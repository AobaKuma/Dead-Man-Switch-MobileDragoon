﻿using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace WalkerGear
{
    //WG_RepairComponent;
    public class WorkGiver_ModuleMaintenance : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ThingDefOf.MF_Building_ComponentStorage);
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobOnThing(pawn, t, forced) != null;
        }
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is Building_Storage && t.TryGetComp(out CompComponentStorage c))
            {
                c.CheckMaintenance();
                Thing th = c.maintanenceTar;
                if (th is ThingWithComps && th.TryGetComp<CompWalkerComponent>(out CompWalkerComponent comp))
                {
                    if (comp.NeedRepair)
                    {
                        return JobMaker.MakeJob(JobDefOf.WG_RepairComponent, th);
                    }
                    //if (comp.NeedAmmo)
                    //{
                    //    List<Thing> tmpList = RefuelWorkGiverUtility.FindEnoughReservableThings(pawn, t.Position, new(comp.NeedAmmoCount, comp.NeedAmmoCount), (tar) => tar.def == comp.AmmoDef);
                    //    if (!tmpList.NullOrEmpty()) return JobGiver_Reload.MakeReloadJob(comp, tmpList);
                    //}
                }
            }
            return null;
        }
    }
}
