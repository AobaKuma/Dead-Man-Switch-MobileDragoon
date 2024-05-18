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
            MechData data =new(source);
            Thing outcome;

            if (comp.parent.def.IsApparel)
            {
                Thing item = ThingMaker.MakeThing(comp.Props.ItemDef);
                data.GetDataFromMech(item);
                outcome = item;
            }
            else
            {
                Thing mech = ThingMaker.MakeThing(comp.Props.EquipedThingDef);
                data.SetDataToMech(mech);
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
        private Color color;
        private int hp;
        public MechData(Thing thing) {
            hp = thing.HitPoints;
            thing.TryGetQuality(out quality);  
            if(thing.TryGetComp<CompColorable>(out CompColorable colorable))color=colorable.Color;
            if (thing.TryGetComp(out CompWalkerComponent comp))
            {
                if (comp.hasReloadableProps)
                {
                    remainingCharges = comp.remainingCharges;
                }
                else if(thing.TryGetComp<CompApparelReloadable>(out var reloadable))
                {
                    remainingCharges = reloadable.RemainingCharges;
                }
                if(remainingCharges<0)remainingCharges= 0;
            }
        }
        public void GetDataFromMech( Thing item) {
            item.HitPoints = hp;
            if (item.TryGetComp<CompQuality>(out CompQuality compQuality)) compQuality.SetQuality(quality, null);
            item.SetColor(color);
            if(item.TryGetComp<CompWalkerComponent>(out var comp)) comp.remainingCharges = remainingCharges;
        }
        public void SetDataToMech( Thing mech) {
            mech.HitPoints = hp;

            if (mech.TryGetComp<CompQuality>(out CompQuality compQuality)) compQuality.SetQuality(quality, null);

            mech.SetColor(color);

            if (mech.TryGetComp<CompApparelReloadable>(out var comp))comp.remainingCharges = remainingCharges; 
        }
    }
}
