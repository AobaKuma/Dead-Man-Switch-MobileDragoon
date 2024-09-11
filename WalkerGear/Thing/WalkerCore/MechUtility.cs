using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace WalkerGear
{
    public static class MechUtility
	{
        public static void WeaponDropCheck(Pawn pawn)
        {
            if (pawn.equipment.Primary != null && !EquipmentUtility.CanEquip(pawn.equipment.Primary, pawn))
            {
                Messages.Message("WG_WeaponDropped".Translate(), pawn, MessageTypeDefOf.NeutralEvent, false);
                pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out var weapon, pawn.Position, false);
            }
        }
        static MechData mechData = new();
        public static readonly Dictionary<QualityCategory, float> qualityToHPFactor = new() {
            {QualityCategory.Awful, 1f},
            {QualityCategory.Poor,1.6f },
            {QualityCategory.Normal,2f},
            {QualityCategory.Good,2.3f},
            {QualityCategory.Excellent,2.6f},
            {QualityCategory.Masterwork,3f},
            {QualityCategory.Legendary,3.6f }
        };
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
            foreach (Apparel item in pawn.apparel?.WornApparel)
            {
                if (item is WalkerGear_Core)
                {
                    return true;
                }
            }
            return false;
        }
        public static void WalkerCoreRemove(Pawn pawn)
        {
            for (int i = pawn.apparel.WornApparelCount - 1; i >= 0; i--)
            {
                var a = pawn.apparel.WornApparel[i];
                if (a.HasComp<CompWalkerComponent>())
                {
                    pawn.apparel.Unlock(a);
                    pawn.apparel.Remove(a);
                }
            }
        }
        public static List<Apparel> WalkerCoreApparelLists(Pawn pawn)
        {
            List<Apparel> tmpApparelList = new List<Apparel>();
            for (int i = pawn.apparel.WornApparelCount - 1; i >= 0; i--)
            {
                var a = pawn.apparel.WornApparel[i];
                if (a.HasComp<CompWalkerComponent>())
                {
                    tmpApparelList.Add(a);
                }
            }
            WalkerGear_Core core = (WalkerGear_Core)tmpApparelList.Find((a) => a is WalkerGear_Core);
            if (core == null) return null;
            List<float> values = new();

            //Log.Message(core.HealthDamaged);
            if (core.HealthDamaged > 0)
            {
                Rand.SplitRandomly(core.HealthDamaged, tmpApparelList.Count, values);
            }
            List<Apparel> finalList = new List<Apparel>();

            for (int j = 0; j < tmpApparelList.Count; j++)
            {
                var a = tmpApparelList[j];
                var c = a.TryGetComp<CompWalkerComponent>();
                if (!values.Empty())
                {
                    if (values[j] >= c.HP)
                    {
                        if (j < tmpApparelList.Count - 1)
                        {
                            values[j + 1] += values[j] - c.HP;
                        }
                        c.HP = 1;
                    }
                    else
                    {
                        c.HP -= Mathf.FloorToInt(values[j]);
                    }
                }
                finalList.Add(a);
            }
            return finalList;
        }
        /// <summary>
        /// 給屍體或倒地龍騎兵脫下模塊(並機率損壞的)方法
        /// </summary>
        /// <param name="pawnOrCorpse"></param>
        public static void DissambleFrom(Thing pawnOrCorpse)
        {
            if (!pawnOrCorpse.Spawned) return;
            Pawn pawn = null;
            IntVec3 pos = IntVec3.Invalid;
            Map map = null;

            if (pawnOrCorpse is Pawn p)
            {
                pawn = p;
                pos = p.Position;
                map = pawn.Map;
            }
            else if (pawnOrCorpse is Corpse c)
            {
                pawn = c.InnerPawn;
                pos = c.Position;
                map = c.Map;
            }

            //檢查可用性
            if (pawn == null || pos == IntVec3.Invalid || map == null)
            {
                Log.Error("Error on MechUltility, Data is Null");
                return;
            }
            if (!PawnWearingWalkerCore(pawn)) return;

            //生成拆除物。
            var a = MechUtility.WalkerCoreApparelLists(pawn);
            MechUtility.WalkerCoreRemove(pawn);
            foreach (Apparel item in a)
            {
                if (Rand.Chance(0.7f))
                {
                    GenSpawn.Spawn(ThingMaker.MakeThing(RimWorld.ThingDefOf.ChunkSlagSteel), pos, map, WipeMode.VanishOrMoveAside);
                }
                else
                if (Rand.Chance(0.25f))
                {
                    ;
                    GenSpawn.Refund(GenSpawn.Spawn(item.Conversion(), pos, map, WipeMode.VanishOrMoveAside), map, new CellRect(pos.x, pos.y, 1, 1), false);
                }
                else
                {
                    GenSpawn.Spawn(item.Conversion(), pos, map, WipeMode.VanishOrMoveAside);
                }
            }
        }

        //添加的
        public static Thing Conversion(this Thing source)
        {
            if (!source.TryGetComp(out CompWalkerComponent comp)) return null;
            mechData.Init(source);
            Thing outcome;

            if (comp.parent.def.IsApparel)
            {
                Thing item = ThingMaker.MakeThing(comp.Props.ItemDef);
                mechData.GetDataFromMech(item); 
                outcome = item;
            }
            else
            {
                Thing mech = ThingMaker.MakeThing(comp.Props.EquipedThingDef);
                mechData.SetDataToMech(mech);
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
        public MechData()
        {

        }

        public void Init(Thing thing)
        {
            quality =default;
            color = default;
            remainingCharges = default;
            hp = default;

            thing.TryGetQuality(out quality);
            if (thing.TryGetComp(out CompColorable colorable)) color = colorable.Color;
            if (thing.TryGetComp(out CompWalkerComponent comp))
            {
                hp = comp.HP;
                if (comp.hasReloadableProps)
                {
                    remainingCharges = comp.remainingCharges;
                }
                else if (thing.TryGetComp<CompApparelReloadable>(out var reloadable))
                {
                    remainingCharges = reloadable.RemainingCharges;
                }
                if (remainingCharges < 0) remainingCharges = 0;
            }
        }
        public void GetDataFromMech( Thing item) {
            if (item.TryGetComp<CompQuality>(out CompQuality compQuality)) compQuality.SetQuality(quality, null);
            item.SetColor(color);
            if (item.TryGetComp<CompWalkerComponent>(out var comp))
            {
                comp.remainingCharges = remainingCharges;
                comp.HP = Mathf.FloorToInt((hp / MechUtility.qualityToHPFactor[quality]));
            }
        }
        public void SetDataToMech( Thing mech) {
            

            if (mech.TryGetComp<CompQuality>(out CompQuality compQuality)) compQuality.SetQuality(quality, null);

            mech.SetColor(color);

            if (mech.TryGetComp<CompApparelReloadable>(out var comp))
            {
                comp.remainingCharges = remainingCharges;
            }
            if (mech.TryGetComp<CompWalkerComponent>(out var c))
            {

                c.HP = Mathf.FloorToInt(hp * MechUtility.qualityToHPFactor[quality]);
            }
        }
    }
}
