using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WhatEver
{
    public class CompTargetable_SingleMech :CompTargetable
    {
        protected override bool PlayerChoosesTarget => true;
        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            yield return targetChosenByPlayer;
        }

        protected override TargetingParameters GetTargetingParameters()=>TargetingParameters.ForMech();
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            return target.Pawn != null&& target.Pawn.IsColonyMechPlayerControlled && base.ValidateTarget(target.Thing, showMessages);
        }
    }


    public class CompProperties_TargetEffectAddHediff : CompProperties
    {
        public CompProperties_TargetEffectAddHediff()
        {
            this.compClass = typeof(CompTargetEffect_AddHediff);
        }
        public ThingDef moteDef;
        public HediffDef addsHediff;
    }
    public class CompTargetEffect_AddHediff : CompTargetEffect
    {
        public CompProperties_TargetEffectAddHediff Props
        {
            get=>(CompProperties_TargetEffectAddHediff)this.props;

        }
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!user.IsColonistPlayerControlled)
            {
                return;
            }
            Job job = JobMaker.MakeJob(MehcJobDefOf.InstallModification, target, this.parent);
            job.count = 1;
            job.playerForced = true;
            user.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
        }
    }
    [DefOf]
    public static class MehcJobDefOf
    {
        public static JobDef InstallModification;
    }
    public class JobDriver_InstallModification : JobDriver
    {
        private Pawn Mech => (Pawn)this.job.GetTarget(TargetIndex.A).Thing;
        private Thing Item => this.job.GetTarget(TargetIndex.B).Thing;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Mech, job, errorOnFailed: errorOnFailed) && pawn.Reserve(Item, job, errorOnFailed: errorOnFailed);
        }
        public override string GetReport()
        {
            return "ReportInstallingModifications".Translate();
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.Wait(600, TargetIndex.None);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            toil.tickAction = delegate ()
            {
                CompUsable compUsable = this.Item.TryGetComp<CompUsable>();
                if (compUsable != null && this.warmupMote == null && compUsable.Props.warmupMote != null)
                {
                    this.warmupMote = MoteMaker.MakeAttachedOverlay(this.Corpse, compUsable.Props.warmupMote, Vector3.zero, 1f, -1f);
                }
                Mote mote = this.warmupMote;
                if (mote == null)
                {
                    return;
                }
                mote.Maintain();
            };
            yield return toil;
            yield return Toils_General.Do(new(AddHediff));
        }

        private void AddHediff()
        {
            CompTargetEffect_AddHediff comp = Item.TryGetComp<CompTargetEffect_AddHediff>();
            if (comp.Props.addsHediff != null)
            {
                Mech.health.AddHediff(comp.Props.addsHediff);
                this.Item.SplitOff(1).Destroy(DestroyMode.Vanish);
            }
        }
    }
}
