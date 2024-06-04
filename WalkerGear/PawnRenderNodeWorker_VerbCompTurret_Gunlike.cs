using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace WalkerGear
{
    public class PawnRenderNodeWorker_VerbCompTurret_Gunlike : PawnRenderNodeWorker //WIP
    {
        private static readonly Vector3 EqLocNorth = new Vector3(0f, 0f, -0.11f);

        private static readonly Vector3 EqLocEast = new Vector3(0.22f, 0f, -0.22f);

        private static readonly Vector3 EqLocSouth = new Vector3(0f, 0f, -0.22f);

        private static readonly Vector3 EqLocWest = new Vector3(-0.22f, 0f, -0.22f);
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms))
            {
                return false;
            }

            if (parms.Portrait || parms.pawn.Dead || !parms.pawn.Spawned)
            {
                return false;
            }

            return true;
        }

        public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
        {
            Vector3 result = base.OffsetFor(node, parms, out pivot);
            if (node is PawnRenderNode_VerbCompTurret turret && turret.VerbComp_Turret != null)
            {
                switch (Rot4.FromAngleFlat(turret.VerbComp_Turret.curRotation).AsInt)
                {
                    case 0://北
                        result += EqLocNorth;
                        break;
                    case 1://東
                        result += EqLocEast;
                        break;
                    case 2://南
                        result += EqLocSouth;
                        break;
                    case 3://西
                        result += EqLocWest;
                        break;
                }
            }
            return result;
        }
        public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Quaternion quaternion = base.RotationFor(node, parms);

            if (node is PawnRenderNode_VerbCompTurret turret && turret.VerbComp_Turret != null)
            {
                quaternion *= turret.VerbComp_Turret.curRotation.ToQuat();
            }
            return quaternion;
        }

        public override void AppendDrawRequests(PawnRenderNode node, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
        {
            Mesh m = null;
            if (node is PawnRenderNode_VerbCompTurret turret && turret.VerbComp_Turret != null)
            {
                
                float aimAngle = turret.VerbComp_Turret.curRotation;
                if (aimAngle > 20f && aimAngle < 160f)
                {
                    m = MeshPool.plane10;
                }
                else if (aimAngle > 200f && aimAngle < 340f)
                {
                    m = MeshPool.plane10Flip;
                }
                else
                {
                    m = MeshPool.plane10;
                }
            }
            requests.Add(new PawnGraphicDrawRequest(node,m));
        }
    }
}
