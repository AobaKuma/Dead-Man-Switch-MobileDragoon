
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using VFECore;

namespace WalkerGear
{
    public class StatPart_CarryingCapacity : StatPart
    {
        public override string ExplanationPart(StatRequest req)
        {
            if (IsWorking(req, out var def))
            {
                return "WG_ArmorCapacity".Translate() + " {0}({1})".Formatted(GetBaseValue(def), GetValue(req, def).ToString());
            }
            return null;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (IsWorking(req, out var def))
            {
                val = GetValue(req, def);
            }
        }
        private float GetValue(StatRequest req, WalkerGear_Core core)
        {

            float baseStat = core.GetStatValue(StatDefOf.CarryingCapacity);

            Log.Message(core.def.defName);
            Log.Message(core.GetStatValue(StatDefOf.CarryingCapacity));
            IEnumerable<Apparel> enumerable = GetPawn(req).apparel?.WornApparel;
            foreach (Apparel item in enumerable ?? Enumerable.Empty<Apparel>())
            {
                baseStat += item.def.equippedStatOffsets.GetStatValueFromList(StatDefOf.CarryingCapacity, 0);
            }
            return baseStat;
        }
        private float GetBaseValue(WalkerGear_Core core)
        {
            return core.def.statBases.GetStatValueFromList(StatDefOf.CarryingCapacity, 175);
        }
        private Pawn GetPawn(StatRequest req)
        {
            if (req.Thing is Pawn p)
            {
                return p;
            }
            Log.Warning("Error: pawn not exist");
            return null;
        }
        private bool IsWorking(StatRequest req, out WalkerGear_Core core)
        {
            core = null;
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                IEnumerable<Apparel> enumerable = pawn.apparel?.WornApparel;
                foreach (Apparel item in enumerable ?? Enumerable.Empty<Apparel>())
                {
                    if (item is WalkerGear_Core walkerGear_Core)
                    {
                        core = walkerGear_Core;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}

