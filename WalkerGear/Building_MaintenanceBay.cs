using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;


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
    public class Building_MaintenanceBay : Building, IThingHolder
	{
        private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");


        [Unsaved(false)]
        private CompPowerTrader cachedPowerComp;
        public bool PowerOn => PowerTraderComp.PowerOn;
        private CompPowerTrader PowerTraderComp
        {
            get
            {
                if (cachedPowerComp == null)
                {
                    cachedPowerComp = this.TryGetComp<CompPowerTrader>();
                }

                return cachedPowerComp;
            }
        }
        private CompAffectedByFacilities ABFComp
        {
            get => this.TryGetComp<CompAffectedByFacilities>();
        }
        public List<SlotDef> slotDefs = new List<SlotDef>(); //所有槽位,画格子用
        public Dictionary<SlotDef,Thing> occupiedSlots = new Dictionary<SlotDef, Thing>();//已使用槽位
        public static Dictionary<SlotDef,List<Thing>> availableCompsForSlots = new Dictionary<SlotDef, List<Thing>>();
        public override IEnumerable<Gizmo> GetGizmos()
        {
            /*List<Thing> things = Map.listerThings.AllThings.Where(a => a.TryGetComp<CompWalkerComponent>(out var x)).ToList();
            foreach (SlotDef def in DefDatabase<SlotDef>.AllDefs)//顯示每一個可用的Slot
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "InsertCompoment".Translate() + "...";
                command_Action.defaultDesc = "InsertCompomentExtractorDesc".Translate();
                command_Action.icon = SlotCompomentIcon;
                command_Action.action = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach (Thing thing in things.Where(a => a.TryGetComp<CompWalkerComponent>().Props.slot == def))//遍歷每一個可用選項並選擇
                    {
                        if (CanReach(thing))
                        {
                            string label = thing.LabelCap + " " + thing.HitPoints.ToString() + "/" + thing.MaxHitPoints.ToString();
                            list.Add(new FloatMenuOption(label, delegate 
                            { 
                            
                            }, MenuOptionPriority.AttackEnemy));
                        }
                        //這東西得能走到位子上
                    }

                    if (!list.Any())
                    {
                        list.Add(new FloatMenuOption("NoAvaliableComponent".Translate(), null));
                    }

                    Find.WindowStack.Add(new FloatMenu(list));
                };
            }*/
            
            if (HasGearCore())
            {
                Command_Target command_GetIn = new Command_Target();
                command_GetIn.defaultLabel = "Get In".Translate();
                command_GetIn.targetingParams = TargetingParameters.ForPawns();
                command_GetIn.action = (tar) =>
                {
                    if (tar.Pawn == null || !CanAcceptPawn(tar.Pawn)) return;
                    tar.Pawn.jobs.StartJob(JobMaker.MakeJob(JobDefOf.WG_GetInWalkerCore, this));
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
            if (!PowerOn)
            {
                return "NoPower".Translate().CapitalizeFirst();
            }
            if (!HasGearCore()) return "NoArmor".Translate().CapitalizeFirst();
            return true;
        }

        public bool HasGearCore()=> occupiedSlots.ContainsKey(SlotDefOf.Core);
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

            foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(selPawn))
            {
                yield return floatMenuOption;
            }
        }

        
        private Command_Action MakeInteractionGizmo(Pawn pawn)//棄用
        {
            Command_Action command_Action = new Command_Action();
            if (MechUtility.PawnWearingWalkerCore(pawn))
            {
                //脫下的Job
                command_Action.defaultLabel = "CommandCancelLoad".Translate();
                command_Action.defaultDesc = "CommandCancelLoadDesc".Translate();
                command_Action.icon = CancelIcon;
                command_Action.activateSound = SoundDefOf.Designate_Cancel;
                command_Action.action = delegate
                {
                    if (pawn.CurJobDef == JobDefOf.WG_GetInWalkerCore)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    }

                    pawn = null;
                };
                return command_Action;
            }
            else
            {
                //WIP檢測操作能力
                command_Action.defaultLabel = "CommandCancelLoad".Translate();
                command_Action.defaultDesc = "CommandCancelLoadDesc".Translate();
                command_Action.icon = CancelIcon;
                command_Action.activateSound = SoundDefOf.Designate_Cancel;
                command_Action.action = delegate
                {
                    if (pawn.CurJobDef == JobDefOf.WG_GetOffWalkerCore)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    }

                    pawn = null;
                };
                return command_Action;
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 250 == 0)  UpdateCache();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                
            }
            foreach (Thing t in innerContainer)
            {
                Log.Message(t.def);
                var c = t.TryGetComp<CompWalkerComponent>();
                if (c != null)
                {
                    AddSlotWithChild(c.Props.slot);
                    occupiedSlots[c.Props.slot] = t;
                }
            }
            UpdateCache();
            if (slotDefs.Empty()) slotDefs.Add(SlotDefOf.Core);
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            this.innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public ThingOwner<Thing> innerContainer;
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }


        //Functions
        private void AddSlotWithChild(SlotDef slot)
        {
            slotDefs.Add(slot);
            foreach(var s in slot.supportedSlots)
            {
                slotDefs.Add(s);
            }
            slotDefs.RemoveDuplicates();
        }
        private void RemoveSlotWithChild(SlotDef slot)
        {
            if (!slot.isCoreFrame)
            slotDefs.Remove(slot);
            foreach (var s in slot.supportedSlots)
            {
                slotDefs.Remove(s);
            }
        }
        public void RemoveComp(Thing t)
        {
            if (innerContainer == null) return;
            if (innerContainer.TryDrop(t,ThingPlaceMode.Near,out Thing th))
            {
                SlotDef s = t.TryGetComp<CompWalkerComponent>().Props.slot;
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
                t.DeSpawnOrDeselect(DestroyMode.Vanish);
                if (innerContainer.TryAdd(t,false)) {

                    AddSlotWithChild(s);
                    occupiedSlots[s] = t;
                }
            }
            
        }
        public void GearUp(Pawn pawn)
        {
            if (innerContainer == null) return;
            foreach (var thing in GetDirectlyHeldThings())
            {
                pawn.apparel.Wear(MechUtility.Conversion(thing) as Apparel,locked:true);
            }
            this.Clear();
        }
        public void UpdateCache()
        {
            availableCompsForSlots.Clear();
            CompAffectedByFacilities comp = ABFComp;
            if ( comp == null) return;
            //Log.Warning($"Looking for{comp.LinkedFacilitiesListForReading.Count} facilities");
            foreach (Thing thing in comp.LinkedFacilitiesListForReading.Where(t => t.HasComp<CompComponentStorage>()))
            {
                if (!(thing is Building_Storage b))continue;
                //Log.Warning("Get storage");
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
            innerContainer.Clear();
            UpdateCache();
        }
    }
}
