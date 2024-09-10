using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using System.Configuration;
using Verse.Sound;
using Mono.Unix.Native;
using RimWorld.Planet;

namespace WalkerGear
{
    [StaticConstructorOnStartup]
    public class DMS_PawnFlyer : PawnFlyer
    {
        public ThingOwner<Thing> innerContainer;

        protected Vector3 startVec;

        //发射源建筑		修改人：Anxie,日期：2024-09-09
        public Thing eBay;

        public static readonly Texture2D TargeterMouseAttachment = ContentFinder<Texture2D>.Get("UI/Overlays/LaunchableMouseAttachment");

        public Pawn pawnStored;

        //射程的接口		修改人：Anxie,日期：2024-09-09
        public ModExtension_Flyer extension => this.def.GetModExtension<ModExtension_Flyer>();

        //是否为着陆		修改人：Anxie,日期：2024-09-09
        public bool isLanding;

        //是否为投射到大地图		修改人：Anxie,日期：2024-09-09
        public bool isToMap;

        public IntVec3 destCell;

        public Map destMap;

        public LocalTargetInfo actionTarget;

        private float flightDistance;

        private bool pawnWasDrafted;

        private bool pawnCanFireAtWill = true;

        protected int ticksFlightTime = 120;

        protected int ticksFlying;

        private JobQueue jobQueue;

        protected EffecterDef flightEffecterDef;

        protected SoundDef soundLanding;

        private Thing carriedThing;

        private LocalTargetInfo target;

        private AbilityDef triggeringAbility;

        private Effecter flightEffecter;

        private int positionLastComputedTick = -1;

        private Vector3 groundPos;

        private Vector3 effectivePos;

        private float effectiveHeight;

        protected Thing FlyingThing
        {
            get
            {
                if (innerContainer.InnerListForReading.Count <= 0)
                {
                    return null;
                }

                return innerContainer.InnerListForReading[0];
            }
        }

        public Pawn FlyingPawn => FlyingThing as Pawn;

        public Thing CarriedThing => carriedThing;

        public override Vector3 DrawPos
        {
            get
            {
                RecomputePosition();
                return effectivePos;
            }
        }

        new public Vector3 DestinationPos
        {
            get
            {
                if (FlyingThing != null)
                {
                    Thing flyingThing = FlyingPawn;
                    return GenThing.TrueCenter(destCell, flyingThing.Rotation, flyingThing.def.size, flyingThing.def.Altitude);
                }
                else
                    return destCell.ToVector3();
            }
        }

        private void RecomputePosition()
        {
            if (positionLastComputedTick != ticksFlying)
            {
                positionLastComputedTick = ticksFlying;
                float t = (float)ticksFlying / (float)ticksFlightTime;
                float t2 = def.pawnFlyer.Worker.AdjustedProgress(t);
                effectiveHeight = def.pawnFlyer.Worker.GetHeight(t2);
                groundPos = Vector3.Lerp(startVec, DestinationPos, t2);
                Vector3 vector = Altitudes.AltIncVect * effectiveHeight;
                Vector3 vector2 = Vector3.forward * (def.pawnFlyer.heightFactor * effectiveHeight);
                effectivePos = groundPos + vector + vector2;
                base.Position = groundPos.ToIntVec3();
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public DMS_PawnFlyer()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            flightEffecter?.Cleanup();
            base.Destroy(mode);
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                float a = Mathf.Max(flightDistance, 1f) / def.pawnFlyer.flightSpeed;
                a = Mathf.Max(a, def.pawnFlyer.flightDurationMin);
                ticksFlightTime = a.SecondsToTicks();
                ticksFlying = 0;
            }
        }

        protected virtual void RespawnPawn()
        {
            Thing flyingThing = FlyingThing;
            LandingEffects();

            innerContainer.TryDrop(flyingThing, destCell, flyingThing.MapHeld, ThingPlaceMode.Direct, out var lastResultingThing, null, null, playDropSound: false);
            Pawn pawn = flyingThing as Pawn;
            if (pawn?.drafter != null)
            {
                pawn.drafter.Drafted = pawnWasDrafted;
                pawn.drafter.FireAtWill = pawnCanFireAtWill;
            }
            flyingThing.Rotation = base.Rotation;

            if (carriedThing != null && innerContainer.TryDrop(carriedThing, destCell, flyingThing.MapHeld, ThingPlaceMode.Direct, out lastResultingThing, null, null, playDropSound: false) && pawn != null)
            {
                carriedThing.DeSpawn();
                if (!pawn.carryTracker.TryStartCarry(carriedThing))
                {
                    Log.Error("Could not carry " + carriedThing.ToStringSafe() + " after respawning flyer pawn.");
                }
            }

            if (pawn == null)
            {
                return;
            }
            if (isToMap)
            {
                //投射到大地图		修改人：Anxie,日期：2024-09-09
                pawn.DeSpawn();
                GenSpawn.Spawn(pawn, new IntVec3(1, 0, 1), destMap);
                Caravan caravan = CaravanMaker.MakeCaravan(new List<Pawn> { pawn }, pawn.Faction, pawn.Map.Tile, addToWorldPawnsIfNotAlready: false);
                pawn.ExitMap(allowedToJoinOrCreateCaravan: false, Rot4.North);
                CameraJumper.TryJump(CameraJumper.GetWorldTarget(this));
                Find.WorldSelector.ClearSelection();
                int tile = this.Map.Tile;
                pawnStored = pawn;
                Find.WorldTargeter.BeginTargeting(ChoseWorldTarget, canTargetTiles: true, TargeterMouseAttachment, closeWorldTabWhenFinished: true, delegate
                {
                    GenDraw.DrawWorldRadiusRing(tile, extension.flightRange);
                }, (GlobalTargetInfo target) => CompLaunchable.TargetingLabelGetter(target, tile, extension.flightRange, new List<IThingHolder> { (Building_EjectorBay)eBay }, TryLaunch, null));
                return;
            }
            if (!isLanding)
            {
                pawn.DeSpawn();
                GenSpawn.Spawn(pawn, new IntVec3(actionTarget.Cell.x, actionTarget.Cell.y, actionTarget.Cell.z + 25 < destMap.AllCells.MaxBy(o => o.z).z ? actionTarget.Cell.z + 25 : destMap.AllCells.MaxBy(o => o.z).z), destMap);
                pawn.Rotation = Rot4.South;
                DMS_AbilityVerb_QuickJump.DoJump(pawn, destMap, target, actionTarget.Cell, true, false);
                return;
            }
            if (jobQueue != null)
            {
                pawn.jobs.RestoreCapturedJobs(jobQueue);
            }

            pawn.jobs.CheckForJobOverride();
            if (def.pawnFlyer.stunDurationTicksRange != IntRange.zero)
            {
                pawn.stances.stunner.StunFor(def.pawnFlyer.stunDurationTicksRange.RandomInRange, null, addBattleLog: false, showMote: false);
            }

            if (triggeringAbility == null)
            {
                return;
            }

            Ability ability = pawn.abilities.GetAbility(triggeringAbility);
            if (ability?.comps == null)
            {
                return;
            }

            foreach (AbilityComp comp in ability.comps)
            {
                ICompAbilityEffectOnJumpCompleted compAbilityEffectOnJumpCompleted;
                if ((compAbilityEffectOnJumpCompleted = comp as ICompAbilityEffectOnJumpCompleted) != null)
                {
                    compAbilityEffectOnJumpCompleted.OnJumpCompleted(startVec.ToIntVec3(), target);
                }
            }
        }

        public void TryLaunch(int destinationTile, TransportPodsArrivalAction arrivalAction)
        {
            if (pawnStored != null && pawnStored.GetCaravan() != null)
            {
                pawnStored.GetCaravan().Tile = destinationTile;
                //不知为何没有效果。。。		修改人：Anxie,日期：2024-09-09
                CameraJumper.TryShowWorld();
            }
        }
        public bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            return CompLaunchable.ChoseWorldTarget(target, eBay.Map.Tile, new List<IThingHolder> { (Building_EjectorBay)eBay }, extension.flightRange, TryLaunch, null);
        }

        private void LandingEffects()
        {
            soundLanding?.PlayOneShot(new TargetInfo(base.Position, base.Map));
            FleckMaker.ThrowDustPuff(DestinationPos + Gen.RandomHorizontalVector(0.5f), base.Map, 2f);
        }
        public override void Tick()
        {
            if (flightEffecter == null && flightEffecterDef != null)
            {
                flightEffecter = flightEffecterDef.Spawn();
                flightEffecter.Trigger(this, TargetInfo.Invalid);
            }
            else
            {
                flightEffecter?.EffectTick(this, TargetInfo.Invalid);
            }

            if (ticksFlying >= ticksFlightTime)
            {
                RespawnPawn();
                Destroy();
            }
            else
            {
                if (ticksFlying % 5 == 0)
                {
                    CheckDestination();
                }

                innerContainer.ThingOwnerTick();
            }

            ticksFlying++;
        }

        public void CheckDestination()
        {
            if (JumpUtility.ValidJumpTarget(base.Map, destCell))
            {
                return;
            }

            int num = GenRadial.NumCellsInRadius(3.9f);
            for (int i = 0; i < num; i++)
            {
                IntVec3 cell = destCell + GenRadial.RadialPattern[i];
                if (JumpUtility.ValidJumpTarget(base.Map, cell))
                {
                    destCell = cell;
                    break;
                }
            }
        }

        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            RecomputePosition();
            if (FlyingPawn != null)
            {
                FlyingPawn.DynamicDrawPhaseAt(phase, effectivePos);
            }
            else
            {
                FlyingThing?.DynamicDrawPhaseAt(phase, effectivePos);
            }

            if (phase == DrawPhase.Draw)
            {
                DrawAt(drawLoc, flip);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            DrawShadow(groundPos, effectiveHeight);
            if (CarriedThing != null && FlyingPawn != null)
            {
                PawnRenderUtility.DrawCarriedThing(FlyingPawn, effectivePos, CarriedThing);
            }
        }

        private void DrawShadow(Vector3 drawLoc, float height)
        {
            Material shadowMaterial = def.pawnFlyer.ShadowMaterial;
            if (!(shadowMaterial == null))
            {
                float num = Mathf.Lerp(1f, 0.6f, height);
                Vector3 s = new Vector3(num, 1f, num);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawLoc, Quaternion.identity, s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMaterial, 0);
            }
        }

        public static DMS_PawnFlyer MakeFlyer(ThingDef flyingDef, Pawn pawn, IntVec3 destCell, LocalTargetInfo actionTarget, Map destMap, EffecterDef flightEffecterDef, SoundDef landingSound, bool isLanding, bool isToMap, bool flyWithCarriedThing = false, Vector3? overrideStartVec = null, Ability triggeringAbility = null, LocalTargetInfo target = default(LocalTargetInfo))
        {
            DMS_PawnFlyer pawnFlyer = (DMS_PawnFlyer)ThingMaker.MakeThing(flyingDef);
            pawnFlyer.startVec = overrideStartVec ?? pawn.TrueCenter();
            pawnFlyer.Rotation = Rot4.North;
            pawnFlyer.flightDistance = pawn.Position.DistanceTo(destCell);
            pawnFlyer.destCell = destCell;
            pawnFlyer.destMap = destMap;
            pawnFlyer.isToMap = isToMap;
            pawnFlyer.actionTarget = actionTarget;
            pawnFlyer.isLanding = isLanding;
            Log.Warning("目的坐标：" + destCell.ToString());
            pawnFlyer.pawnWasDrafted = pawn.Drafted;
            pawnFlyer.flightEffecterDef = flightEffecterDef;
            pawnFlyer.soundLanding = landingSound;
            pawnFlyer.triggeringAbility = triggeringAbility?.def;
            pawnFlyer.target = target;
            if (pawn.drafter != null)
            {
                pawnFlyer.pawnCanFireAtWill = pawn.drafter.FireAtWill;
            }

            if (pawn.CurJob != null)
            {
                if (pawn.CurJob.def == RimWorld.JobDefOf.CastJump)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
                else
                {
                    pawn.jobs.SuspendCurrentJob(JobCondition.InterruptForced);
                }
            }

            pawnFlyer.jobQueue = pawn.jobs.CaptureAndClearJobQueue();
            if (flyWithCarriedThing && pawn.carryTracker.CarriedThing != null && pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out pawnFlyer.carriedThing))
            {
                if (pawnFlyer.carriedThing.holdingOwner != null)
                {
                    pawnFlyer.carriedThing.holdingOwner.Remove(pawnFlyer.carriedThing);
                }

                pawnFlyer.carriedThing.DeSpawn();
            }

            if (pawn.Spawned)
            {
                pawn.DeSpawn(DestroyMode.WillReplace);
            }

            if (!pawnFlyer.innerContainer.TryAdd(pawn))
            {
                Log.Error("Could not add " + pawn.ToStringSafe() + " to a flyer.");
                pawn.Destroy();
            }

            if (pawnFlyer.carriedThing != null && !pawnFlyer.innerContainer.TryAdd(pawnFlyer.carriedThing))
            {
                Log.Error("Could not add " + pawnFlyer.carriedThing.ToStringSafe() + " to a flyer.");
            }

            return pawnFlyer;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref startVec, "startVec");
            Scribe_Values.Look(ref destCell, "destCell");
            Scribe_Values.Look(ref flightDistance, "flightDistance", 0f);
            Scribe_Values.Look(ref pawnWasDrafted, "pawnWasDrafted", defaultValue: false);
            Scribe_Values.Look(ref pawnCanFireAtWill, "pawnCanFireAtWill", defaultValue: true);
            Scribe_Values.Look(ref ticksFlightTime, "ticksFlightTime", 0);
            Scribe_Values.Look(ref ticksFlying, "ticksFlying", 0);
            Scribe_Defs.Look(ref flightEffecterDef, "flightEffecterDef");
            Scribe_Defs.Look(ref soundLanding, "soundLanding");
            Scribe_Defs.Look(ref triggeringAbility, "triggeringAbility");
            Scribe_References.Look(ref carriedThing, "carriedThing");
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Deep.Look(ref jobQueue, "jobQueue");
            Scribe_TargetInfo.Look(ref target, "target");
        }
    }
    public class ModExtension_Flyer : DefModExtension
    {
        public int flightRange;
    }
}
