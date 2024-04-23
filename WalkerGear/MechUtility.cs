using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace WalkerGear
{
    public static class MechUtility
	{
        public static bool GetWalkerCore(this Pawn pawn, out WalkerGear_Core core)
        {
            Pawn_ApparelTracker apparel = pawn.apparel;
            IEnumerable<Apparel> enumerable = apparel?.WornApparel;
            foreach (Apparel apparel2 in (enumerable ?? Enumerable.Empty<Apparel>()))
            {
                WalkerGear_Core apparelCore = apparel2 as WalkerGear_Core;
                bool flag = apparelCore != null;
                if (flag)
                {
                    core = apparelCore;
                    return true;
                }
            }
            core = null;
            return false;
        }
        public static bool PawnWearingWalkerCore(Pawn pawn)
        {
            foreach (Apparel item in pawn.apparel.LockedApparel)
            {
                if (item is WalkerGear_Core)
                {
                    return true;
                }
            }
            return false;
        }

        //添加的
        public static Thing Conversion(this Thing source)
        {
            if (!source.TryGetComp(out CompWalkerComponent comp)) return null;
            MechData data =new MechData(source);
            Thing outcome;

            if (comp.Props.isApparal)
            {
                Thing item = ThingMaker.MakeThing(comp.Props.ItemDef);
                data.GetDataFromMech(data,item);
                outcome = item;
            }
            else
            {
                Thing mech = ThingMaker.MakeThing(comp.Props.EquipedThingDef);
                data.SetDataToMech(data,mech);
                outcome = mech;
            }
            
            source.Destroy();
            return outcome;
        }

    }

    public class MechData
    {
        private int remainingCharges;
        private QualityCategory quality;
        public MechData(Thing thing) { throw new NotImplementedException(); }
        public void GetDataFromMech(MechData mech, Thing item) { throw new NotImplementedException(); }
        public void SetDataToMech(MechData item, Thing mech) { throw new NotImplementedException(); }
    }
    public static class GizmoUltily
    {
    }
}
