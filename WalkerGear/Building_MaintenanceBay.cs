using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using static Verse.HediffCompProperties_RandomizeSeverityPhases;


namespace WalkerGear
{
    public class WorkGiver_RepairThing : WorkGiver_Scanner
    {
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(JobDefOf.WG_RepairComponent, t);
        }
    }

    [StaticConstructorOnStartup]
    public partial class Building_MaintenanceBay : Building, IThingHolder
    {
        //cached stuffs
        //private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
        public static readonly Texture2D rotateButton = new CachedTexture("UI/Rotate").Texture;
        public static readonly Texture2D rotateOppoButton = new CachedTexture("UI/RotateOppo").Texture;
        [Unsaved(false)]
        private CompPowerTrader cachedPowerComp;
        private CompAffectedByFacilities abfComp;
        public bool hasGearCore;

        //Fileds
        public Rot4 direction = Rot4.South;
        public HashSet<SlotDef> slotDefs = new(); //所有槽位,画格子用
        public Dictionary<SlotDef, Apparel> occupiedSlots = new();//已使用槽位
        public Dictionary<SlotDef, List<Thing>> availableCompsForSlots = new();
        public ThingOwner<Thing> innerContainer;

        //Properties
        private CompPowerTrader PowerTraderComp
        {
            get
            {
                return cachedPowerComp ??= this.TryGetComp<CompPowerTrader>();
            }
        }
        public bool PowerOn => PowerTraderComp.PowerOn;
        private CompAffectedByFacilities ABFComp
        {
            get {
                return abfComp ??= this.TryGetComp<CompAffectedByFacilities>();
            }
        }
        public bool HasGearCore => hasGearCore = occupiedSlots.ContainsKey(SlotDefOf.Core);
        //methods override
        public override IEnumerable<Gizmo> GetGizmos()
        {

            if (HasGearCore)
            {
                Command_Target command_GetIn = new()
                {
                    defaultLabel = "Get In".Translate(),
                    targetingParams = TargetingParameters.ForPawns(),
                    action = (tar) =>
                    {
                        if (tar.Pawn == null || !CanAcceptPawn(tar.Pawn)) return;
                        tar.Pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.WG_GetInWalkerCore, this));
                    }
                };
                yield return command_GetIn;
            }
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<ThingOwner<Thing>>(ref this.innerContainer, "innerContainer", new object[]
            {
                this
            });
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn) //選擇殖民者後右鍵
        {
            if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotEnterBuilding".Translate(this) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }
            AcceptanceReport acceptanceReport = CanAcceptPawn(selPawn);
            if (acceptanceReport.Accepted)
            {
                yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("EnterBuilding".Translate(this), delegate
                {
                    selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_GetInWalkerCore, this), JobTag.DraftedOrder);
                }), selPawn, this);
            }
            else if (selPawn.CanReach(this, PathEndMode.Touch, Danger.Deadly))
            {
                if (!HasGearCore && selPawn.apparel.LockedApparel.Any((a) => a is WalkerGear_Core))
                {
                    yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("EnterBuilding".Translate(this), delegate
                    {
                        selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.WG_GetOffWalkerCore, this), JobTag.DraftedOrder);
                    }), selPawn, this);
                }
            }

            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn))
            {
                yield return floatMenuOption;
            }
        }
        public override void Tick()
        {
            base.Tick();

            if (Find.TickManager.TicksGame % 10 == 0 && HasGearCore) GetDirectlyHeldThings();

            if (Find.TickManager.TicksGame % 250 == 0) UpdateCache();
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                Pawn p = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist);
                p.apparel.DestroyAll();
                p.rotationInt = Rotation.Opposite;
                p.apparel.Wear((Apparel)ThingMaker.MakeThing(ThingDefOf.Apparel_Dummy));
                innerContainer.TryAdd(p);
            }
            foreach (Apparel a in ModuleStorage)
            {
                if (a.TryGetComp<CompWalkerComponent>(out CompWalkerComponent c))
                {
                    AddSlotWithChild(c.Props.slot);
                    occupiedSlots[c.Props.slot] = a;
                }
            }
            UpdateCache();
            slotDefs.Add(SlotDefOf.Core);
        }
        public BuildingExtraRenderer ExtraRenderer
        {
            get
            {
                return def.GetModExtension<BuildingExtraRenderer>()??null;
            }
        }
        public Graphic ExtraGraphic => extraGraphic ??= ExtraRenderer?.extraGraphic.GraphicColoredFor(this);
        public Graphic extraGraphic;
        public override void Print(SectionLayer layer)
        {
            base.Print(layer);
            if (ExtraRenderer != null)
                ExtraGraphic.Print(layer,this,0);
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            foreach(Apparel a in ModuleStorage)
            {
                GenPlace.TryPlaceThing(MechUtility.Conversion(a),Position,Map,ThingPlaceMode.Direct);
            }
            Dummy.Destroy();
            base.Destroy(mode);
        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        //Help Functions
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }
        public AcceptanceReport CanReach(Thing thing)
        {
            if (thing == null) return false;
            if (!thing.MapHeld.reachability.CanReach(thing.Position, this.InteractionCell, PathEndMode.OnCell, TraverseMode.ByPawn, Danger.Deadly)) return false;
            return true;
        }
        private AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            if (!pawn.IsColonist && !pawn.IsSlaveOfColony && !pawn.IsPrisonerOfColony && (!pawn.IsColonyMutant || !pawn.IsGhoul))
            {
                return false;
            }
            if (!pawn.RaceProps.Humanlike || pawn.IsQuestLodger())
            {
                return false;
            }
            if (PowerComp!=null && !PowerOn)
            {
                return "NoPower".Translate().CapitalizeFirst();
            }
            if (pawn.apparel.LockedApparel.Any((a) => a is WalkerGear_Core)) return "AlreadyHasArmor".Translate().CapitalizeFirst();
            if (!HasGearCore) return "NoArmor".Translate().CapitalizeFirst();
            return true;
        }
        
        private void AddSlotWithChild(SlotDef slot)
        {
            slotDefs.Add(slot);
            foreach(var s in slot.supportedSlots)
            {
                slotDefs.Add(s);
            }
        }
        private void RemoveSlotWithChild(SlotDef slot)
        {
            /*if (!slot.isCoreFrame)
            slotDefs.Remove(slot);*/
            if (slot.isCoreFrame)
            {
                slot.supportedSlots.ForEach((s) => slotDefs.Remove(s));                       
            }
        }
        public void RemoveComp(Thing t)
        {
            if (innerContainer == null) return;
            if (ModuleStorage.Contains((Apparel)t))
            {
                SlotDef s = t.TryGetComp<CompWalkerComponent>().Props.slot;
                GenPlace.TryPlaceThing(MechUtility.Conversion(t),Position,Map,ThingPlaceMode.Direct);
                RemoveSlotWithChild(s);
                occupiedSlots.Remove(s);
                foreach (var item in s.supportedSlots)
                {
                    if (occupiedSlots.ContainsKey(item)) RemoveComp(occupiedSlots[item]);
                }
            }
        }
        public void AddOrReplaceComp(Thing t)
        {
            if (innerContainer == null) return;
            SlotDef s = t.TryGetComp<CompWalkerComponent>().Props.slot;
            if (occupiedSlots.ContainsKey(s))
            {
                RemoveComp(occupiedSlots[s]);
            }
            if (availableCompsForSlots.ContainsKey(s) && availableCompsForSlots[s].Contains(t)){
                if (MechUtility.Conversion(t) is Apparel a) {
                    DummyApparels.Wear(a);
                    AddSlotWithChild(s);
                    occupiedSlots[s] = a;
                }
            }
        }
        public void GearUp(Pawn pawn)
        {
            if (innerContainer == null ||!HasGearCore) return;
            foreach(Apparel a in ModuleStorage)
            {
                foreach (var item in pawn.apparel.WornApparel)
                {
                    if (!ApparelUtility.CanWearTogether(item.def, a.def, pawn.RaceProps.body) && pawn.apparel.IsLocked(item)) 
                        return;
                }
                
            }
            for (int i = ModuleStorage.Count-1; i >=0;i--) {
                Apparel a = ModuleStorage[i];
                DummyApparels.Remove(a);
                pawn.apparel.Wear(a,true, locked: true);
            }
            pawn.apparel.WornApparel.Find((a)=>a is WalkerGear_Core c&&c.RefreshHP(true));
            this.Clear();
        }
        public void GearDown(Pawn pawn)
        {
            if (innerContainer == null) return;
            if (HasGearCore) return;
            List<Apparel> tmpList = new();
            //Log.Message(1);
            for (int i = pawn.apparel.LockedApparel.Count-1;i>=0;i--)
            {
                var a = pawn.apparel.LockedApparel[i];
                if (a.HasComp<CompWalkerComponent>())
                {
                    tmpList.Add(a);
                    pawn.apparel.Remove(a);
                }
            }
            WalkerGear_Core core = (WalkerGear_Core)tmpList.Find((a) => a is WalkerGear_Core);
            if (core == null) return;
            List<float> values=new();
            Rand.SplitRandomly(core.HealthDamaged,tmpList.Count,values);
            //Log.Message(2);
            for (int j=0;j<tmpList.Count;j++)
            {
                var a = tmpList[j];
                if(a.TryGetQuality(out var quality)&& WalkerGear_Core.qualityToHPFactor.TryGetValue(quality,out var v))
                {
                    values[j]/= v;
                }
                if (values[j] >= a.HitPoints)
                {
                    if (j < tmpList.Count - 1)
                    {
                        values[j + 1] += values[j] - a.HitPoints;
                    }
                    a.HitPoints = 1;
                }
                else
                {
                    a.HitPoints -= Mathf.FloorToInt(values[j]);
                }
                DummyApparels.Wear(a);
                SlotDef s = a.TryGetComp<CompWalkerComponent>().Props.slot;
                AddSlotWithChild(s);
                occupiedSlots[s] = a;
            }
            //Log.Message(3);
        }
        public void UpdateCache()
        {
            availableCompsForSlots.Clear();
            if (ABFComp == null) return;
           
            foreach (Thing thing in ABFComp.LinkedFacilitiesListForReading.Where(t => t.HasComp<CompComponentStorage>()))
            {
                if (thing is not Building_Storage b)continue;
              
                foreach (Thing t in b.slotGroup.HeldThings)
                {
                    if (!t.TryGetComp<CompWalkerComponent>(out CompWalkerComponent c)) continue;
                    SlotDef s = c.Props.slot;
                    if (!availableCompsForSlots.ContainsKey(s))
                    {
                        availableCompsForSlots[s] = new List<Thing>();
                    }
                    availableCompsForSlots[s].AddDistinct(t);
                    //Log.Warning("Added Thing");
                }
                    
            }
        }
        private void Clear()
        {
            slotDefs.Clear();
            occupiedSlots.Clear();
            slotDefs.Add(SlotDefOf.Core);
            UpdateCache();
        }

        
    }

    public partial class Building_MaintenanceBay
    {
        private Pawn cachePawn;
        public Pawn Dummy => cachePawn ??= (Pawn)innerContainer.First();
        public Rot4 Face
        {
            get => direction; set => direction = value;
        }
        public Pawn_ApparelTracker DummyApparels => Dummy.apparel;
        public List<Apparel> ModuleStorage => DummyApparels.WornApparel.Where((a) => a.HasComp<CompWalkerComponent>()).ToList();
        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            if (hasGearCore)
            {
                Dummy.Drawer.renderer.DynamicDrawPhaseAt(phase, drawLoc.WithYOffset(1f), null, true);
            }

            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
        }
    }
    public class BuildingExtraRenderer : DefModExtension
    {
        public GraphicData extraGraphic;
        public AltitudeLayer altitudeLayer;
    }
}
