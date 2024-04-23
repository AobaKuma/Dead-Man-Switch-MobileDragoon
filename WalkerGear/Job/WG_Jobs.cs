using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace WalkerGear
{
    //没做完！！！！！

    //WG_GetInWalkerCore; 不知道能不能用
    public class JobDriver_GetInWalkerCore : JobDriver
    {
        private const TargetIndex maintenanceBay = TargetIndex.A;
        private const int wait = 200;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.GetTarget(maintenanceBay), this.job, errorOnFailed: errorOnFailed);
        }

        //还在写
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(maintenanceBay);
            this.FailOnDowned(maintenanceBay);
            this.FailOnNotCasualInterruptible(maintenanceBay);
            yield return Toils_Goto.GotoThing(maintenanceBay, PathEndMode.InteractionCell);
            yield return Toils_General.WaitWith(maintenanceBay, wait, true, true,face:TargetIndex.A);
            Toil gearUp = new Toil
            {
                initAction = () =>
                {
                    Pawn actor = this.pawn;
                    Building_MaintenanceBay bay = actor.CurJob.GetTarget(TargetIndex.A).Thing as Building_MaintenanceBay;
                    if (bay != null&&bay.HasGearCore())
                    {
                        actor.Position = bay.Position;
                        bay.GearUp(actor);
                    }
                }
            };
            yield return gearUp;
        }
    }

    //WG_GetOffWalkerCore;
    public class JobDriver_GetOffWalkerCore : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            throw new NotImplementedException();
        }
    }

    //WG_RepairComponent;
    public class WorkGiver_RepairComponent : WorkGiver
    {

    }

        
    public class JobDriver_RepairComponent : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            throw new NotImplementedException();
        }
    }
}
