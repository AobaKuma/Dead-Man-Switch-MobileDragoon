
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WalkerGear
{
    public class StatPart_MoveSpeed : StatPart
    {
        public override string ExplanationPart(StatRequest req)
        {
            if (IsWorking(req, out var def))
            {
                return "WG_ArmorSpeed".Translate() + " {0} c/s".Formatted(GetValue(req, def).ToString("0.##"));
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
        private float GetValue(StatRequest req, WalkerGear_Core def)
        {
            float baseStat = def.GetStatValue(StatDefOf.MoveSpeed);
            IEnumerable<Apparel> enumerable = GetPawn(req).apparel?.WornApparel;
            foreach (Apparel item in enumerable ?? Enumerable.Empty<Apparel>())
            {
                baseStat += item.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.MoveSpeed);
            }
            return baseStat;
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
        private bool IsWorking(StatRequest req, out WalkerGear_Core def)
        {
            def = null;
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                IEnumerable<Apparel> enumerable = pawn.apparel?.WornApparel;
                foreach (Apparel item in enumerable ?? Enumerable.Empty<Apparel>())
                {
                    if (item is WalkerGear_Core walkerGear_Core)
                    {
                        def = walkerGear_Core;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}

