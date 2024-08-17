using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using VFECore;

namespace WalkerGear
{
    public class PawnRenderSubWorker_Offset : PawnRenderSubWorker
    {
        public override void TransformOffset(PawnRenderNode node, PawnDrawParms parms, ref Vector3 offset, ref Vector3 pivot)
        {
            Apparel p = parms.pawn.apparel.WornApparel.FirstOrDefault(a => a is WalkerGear_Core c);
            if (p != null && p.def.HasModExtension<ApparelRenderOffsets>())
            {
                var ext = p.def.GetModExtension<ApparelRenderOffsets>();
                switch (node.Worker)
                {
                    case PawnRenderNodeWorker_Head: //Head
                        offset = ext.headData.OffsetForRot(parms.facing) == Vector3.zero ? offset : ext.headData.OffsetForRot(parms.facing);
                    break;
                    case PawnRenderNodeWorker: //Root
                        offset = ext.rootData.OffsetForRot(parms.facing) == Vector3.zero ? offset : ext.rootData.OffsetForRot(parms.facing);
                    break;
                }
            }
        }
        public override void TransformLayer(PawnRenderNode node, PawnDrawParms parms, ref float layer)
        {
            Apparel p = parms.pawn.apparel.WornApparel.FirstOrDefault(a => a is WalkerGear_Core c);
            if (p != null && p.def.HasModExtension<ApparelRenderOffsets>())
            {
                var ext = p.def.GetModExtension<ApparelRenderOffsets>();
                switch (node.Worker)
                {
                    case PawnRenderNodeWorker_Head:
                        layer = ext.headData.LayerForRot(parms.facing, layer);
                    break;
                    case PawnRenderNodeWorker_Carried:
                        layer = ext.rootData.LayerForRot(parms.facing, layer);
                    break;
                }
            }
        }
    }
    public class ApparelRenderOffsets : DefModExtension
    {
        public DrawData headData;
        public DrawData rootData;
    }
}

