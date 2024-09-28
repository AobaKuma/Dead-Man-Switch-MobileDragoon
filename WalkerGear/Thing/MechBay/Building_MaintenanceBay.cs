using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mono.Unix.Native;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;


namespace WalkerGear
{

    [StaticConstructorOnStartup]
    public partial class Building_MaintenanceBay : Building
    {
        //cached stuffs

        [Unsaved(false)]
        private CompPowerTrader cachedPowerComp;
        //Properties
        private CompPowerTrader PowerTraderComp
        {
            get
            {
                return cachedPowerComp ??= this.TryGetComp<CompPowerTrader>();
            }
        }
        public bool PowerOn => PowerTraderComp.PowerOn;

        //methods override
        public override IEnumerable<Gizmo> GetGizmos()
        {

            if (HasGearCore && false)
            {
                Command_Target command_GetIn = new()
                {
                    defaultLabel = "WG_GetIn".Translate(),
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
            Scribe_Deep.Look(ref cachePawn, "cachedPawn");
            SetItabCacheDirty();
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            foreach (Apparel a in ModuleStorage)
            {
                GenPlace.TryPlaceThing(MechUtility.Conversion(a), Position, Map, ThingPlaceMode.Direct);
            }
            Dummy.Destroy();
            base.Destroy(mode);
        }
        public void RemoveModule(Thing t)
        {
            if (cachePawn == null) return;
            if (DummyApparels.Contains(t))
            {
                if (!this.TryGetComp(out CompAffectedByFacilities abf))
                {
                    Log.Warning("CompAffectedByFacilities is null");
                    return;
                }
                Thing moduleItem = MechUtility.Conversion(t);
                foreach (Thing b in abf.LinkedFacilitiesListForReading)
                {
                    if (b is not Building_Storage s) continue;
                    if (!s.Accepts(moduleItem)) continue;
                    foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(s))
                    {
                        List<Thing> cellThings = cell.GetThingList(s.Map);
                        if (cellThings.Where(thing => thing != s).EnumerableNullOrEmpty())
                        {
                            GenPlace.TryPlaceThing(moduleItem, cell, Map, ThingPlaceMode.Direct);
                            return;
                        }
                    }
                }
                GenPlace.TryPlaceThing(moduleItem, InteractionCell, Map, ThingPlaceMode.Direct);
            }
        }
        public void Add(Thing t)
        {
            if (cachePawn == null) return;
            Apparel a = MechUtility.Conversion(t) as Apparel;
            DummyApparels.Wear(a);
        }
    }
    //给Itab提供的功能
    public partial class Building_MaintenanceBay
    {
        private bool isOccupiedSlotDirty = true;

        private float massCapacity;
        private float currentLoad;

        private readonly List<SlotDef> toRemove = new();

        public readonly Dictionary<SlotDef, Thing> occupiedSlots = new();
        public readonly Dictionary<int, SlotDef> positionWSlot = new(7);

        public float MassCapacity => massCapacity;
        public float CurrentLoad => currentLoad;

        public Rot4 direction = Rot4.South;//缓存Itab里pawn的方向
        public bool HasGearCore => GetGearCore != null;

        public Apparel GetGearCore => DummyApparels.WornApparel.Find(a => a is WalkerGear_Core);

        public List<CompWalkerComponent> GetwalkerComponents()
        {
            if (ComponentsCache==null)
            {
                TryUpdateOccupiedSlotsCache(true);
            }
            return ComponentsCache;
        }
        private List<CompWalkerComponent> ComponentsCache;

        public void TryUpdateOccupiedSlotsCache(bool force = false)
        {
            if (!force && !isOccupiedSlotDirty)
            {
                return;
            }
            occupiedSlots.Clear();
            positionWSlot.Clear();
            massCapacity = 0;
            currentLoad = 0;
            List<CompWalkerComponent> li = new List<CompWalkerComponent>();
            foreach (Apparel a in DummyApparels.WornApparel)
            {
                massCapacity += a.GetStatValue(StatDefOf.CarryingCapacity) != StatDefOf.CarryingCapacity.defaultBaseValue ? a.GetStatValue(StatDefOf.CarryingCapacity) : 0;
                currentLoad += a.GetStatValue(StatDefOf.Mass);
                if (a.TryGetComp<CompWalkerComponent>(out CompWalkerComponent c))
                {
                    foreach (SlotDef s in c.Props.slots)
                    {
                        occupiedSlots[s] = a;
                        positionWSlot[s.uiPriority] = s;
                    }
                    li.Add(c);
                }
            }
            ComponentsCache = li;
            isOccupiedSlotDirty = false;
        }
        public void RemoveModules(SlotDef slot, bool updateNow = true)
        {
            if (!occupiedSlots.ContainsKey(slot)) return;
            if (slot.isCoreFrame)

            toRemove.Clear();
            GetSupportedSlotRecur(slot);
            foreach (var s in toRemove)
            {
                if (occupiedSlots.TryGetValue(s, out var t))
                {
                    RemoveModule(t);
                }

            }
            if (updateNow)
            {
                TryUpdateOccupiedSlotsCache(true);
            }
        }

        public void AddOrReplaceModule(Thing thing)
        {
            if (!thing.TryGetComp(out CompWalkerComponent c)) return;
            if (c.Props.slots.Where(s => s.isCoreFrame).Any() && GetGearCore!=null)
            {
                RemoveModules(GetGearCore.GetComp<CompWalkerComponent>().Props.slots.Where(l => l.isCoreFrame).First());
            }
            else
            {
                foreach (var s in c.Props.slots)
                {
                    RemoveModules(s, false);
                }
            }
            Add(thing);
            TryUpdateOccupiedSlotsCache(true);
        }

        public IEnumerable<Thing> GetAvailableModules(SlotDef slotDef, bool IsCore = false)
        {
            if (!this.TryGetComp(out CompAffectedByFacilities abf))
            {
                Log.Warning("CompAffectedByFacilities is null");
                yield break;
            }
            foreach (Thing b in abf.LinkedFacilitiesListForReading)
            {
                if (b is not Building_Storage s) continue;
                if (s.GetSlotGroup().HeldThings.EnumerableNullOrEmpty()) continue;

                foreach (Thing module in s.GetSlotGroup().HeldThings)
                {
                    if (!module.TryGetComp(out CompWalkerComponent c)) continue;
                    if (IsCore)
                    {
                        if (!c.Props.slots.Where(s => s.isCoreFrame).Any()) continue;
                    }
                    else
                    {
                        if (!c.Props.slots.Contains(slotDef)) continue;
                    }
                    //if (DebugSettings.godMode) Log.Message(module.def.defName + " is walker module of " + (slotDef == null ? "any" : slotDef.defName) + " added to list.");
                    yield return module;
                }
            }
        }

        private void GetSupportedSlotRecur(SlotDef slotDef)
        {
            if (!slotDef.supportedSlots.NullOrEmpty())
            {
                foreach (var s in slotDef.supportedSlots)
                {
                    GetSupportedSlotRecur(s);
                }
            }
            toRemove.Add(slotDef);
        }
        private void SetItabCacheDirty()
        {
            isOccupiedSlotDirty = true;
        }
    }
    //穿脱龙骑兵
    public partial class Building_MaintenanceBay
    {
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
                if (!HasGearCore && selPawn.apparel.WornApparel.Any((a) => a is WalkerGear_Core))
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
            if (PowerComp != null && !PowerOn)
            {
                return "NoPower".Translate().CapitalizeFirst();
            }
            if (pawn.apparel.WornApparel.Any((a) => a is WalkerGear_Core)) return "WG_Disabled_AlreadyHasCoreFrame".Translate().CapitalizeFirst();
            if (!HasGearCore) return "WG_Disabled_NoCoreFrame".Translate().CapitalizeFirst();
            return true;
        }
        public bool CanGear(Pawn pawn)
        {
            foreach (Apparel a in ModuleStorage)
            {
                foreach (var item in pawn.apparel.WornApparel)
                {
                    if (!ApparelUtility.CanWearTogether(item.def, a.def, pawn.RaceProps.body) && pawn.apparel.IsLocked(item))
                        return false;
                }
            }
            return true;
        }
        public virtual void GearUp(Pawn pawn)
        {
            if (cachePawn == null || !HasGearCore) return;
            foreach (Apparel a in ModuleStorage)
            {
                foreach (var item in pawn.apparel.WornApparel)
                {
                    if (!ApparelUtility.CanWearTogether(item.def, a.def, pawn.RaceProps.body) && pawn.apparel.IsLocked(item))
                        return;
                }
            }
            for (int i = ModuleStorage.Count - 1; i >= 0; i--)
            {
                Apparel a = ModuleStorage[i];
                DummyApparels.Remove(a);
                pawn.apparel.Wear(a, true, locked: true);
            }
            pawn.apparel.WornApparel.Find((a) => a is WalkerGear_Core c && c.RefreshHP(true));
            SetItabCacheDirty();
        }
        public void GearDown(Pawn pawn)
        {
            if (cachePawn == null) return;
            if (HasGearCore) return;
            List<Apparel> _temp = MechUtility.WalkerCoreApparelLists(pawn);
            MechUtility.WalkerCoreRemove(pawn);
            foreach (Apparel a in _temp)
            {
                DummyApparels.Wear(a, false, true);
            }
            SetItabCacheDirty();
            MechUtility.WeaponDropCheck(pawn);
        }
    }
    //渲染小人的
    public partial class Building_MaintenanceBay
    {
        private Pawn cachePawn;

        public static bool PawnInBuilding(Pawn pawn)
        {
            pawnsInBuilding.RemoveAll(p => p == null || p.Destroyed);
            return pawnsInBuilding.Contains(pawn);
        }
        public static List<Pawn> pawnsInBuilding = new();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (respawningAfterLoad)
            {
                pawnsInBuilding.Add(Dummy);
            }
        }
        public Pawn Dummy
        {
            get
            {
                if (cachePawn == null)
                {
                    cachePawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist);//生成
                    pawnsInBuilding.Add(cachePawn);
                    cachePawn.apparel.DestroyAll();
                    cachePawn.rotationInt = Rotation.Opposite;
                    cachePawn.drafter = new(cachePawn)
                    {
                        Drafted = true
                    };

                }
                return cachePawn;
            }
        }
        public Pawn_ApparelTracker DummyApparels => Dummy?.apparel;
        public List<Apparel> ModuleStorage
        {
            get
            {
                List<Apparel> tmp = new();
                foreach (Apparel a in DummyApparels.WornApparel)
                {
                    if (a.HasComp<CompWalkerComponent>()) tmp.Add(a);
                }
                return tmp;
            }
        }
        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            if (HasGearCore)
            {
                Dummy.Drawer.renderer.DynamicDrawPhaseAt(phase, drawLoc.WithYOffset(1f), null, true);
            }
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
        }
    }
}