using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WalkerGear
{
    public class StatPart_MoveSpeed : StatPart
    {
        public override string ExplanationPart(StatRequest req)
        {
            if (IsWorking(req, out WalkerGear_Core core))
            {
                return "机甲移动速度 : " + "{0} 格 / 秒".Formatted(core.System.movespeed.ToString("0.##"));
            }
            return null;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (IsWorking(req, out WalkerGear_Core core))
            {
                val = core.System.movespeed;
            }
        }
        private bool IsWorking(StatRequest req, out WalkerGear_Core def)
        {
            def = null;
            if (req.HasThing && req.Thing is Pawn pawn)
            {
                foreach (Apparel x in (pawn.apparel?.WornApparel ?? Enumerable.Empty<Apparel>()))
                {
                    if (x is WalkerGear_Core y)
                    {
                        def = y;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
