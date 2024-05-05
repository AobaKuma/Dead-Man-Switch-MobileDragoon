using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace WalkerGear
{
    /*    public class Projectile_Missile : Projectile_Explosive
        {

            public override Vector3 DrawPos
            {
                get
                {
                    return this.ExactPosition;
                }
            }
            public override Vector3 ExactPosition
            {
                get
                {
                    Vector3 b = (this.destination - this.origin).Yto0() * this.DistanceCoveredFraction;
                    return this.origin.Yto0() + b + Vector3.up * this.def.Altitude;
                }
            }

            protected new float DistanceCoveredFraction
            {
                get
                {
                    float a= (float)this.ticksToImpact / this.StartingTicksToImpact;
                    float b = CurveCovered(1f - a);
                    float num = Mathf.Clamp01(b);

                    Log.Message($"{a} {b} {num}");

                    return num;
                }
            }

            protected override void DrawAt(Vector3 drawLoc, bool flip = false)
            {
                float num = this.def.projectile.arcHeightFactor * heightCurve.Evaluate(this.DistanceCoveredFractionArc)*100f;
                Vector3 vector = drawLoc + new Vector3(0f, 0f, 1f) * num;
                if (this.def.projectile.shadowSize > 0f)
                {
                    DrawShadow(drawLoc, num);
                }
                Quaternion rotation = this.ExactRotation;
                if (this.def.projectile.spinRate != 0f)
                {
                    float num2 = 60f / this.def.projectile.spinRate;
                    rotation = Quaternion.AngleAxis((float)Find.TickManager.TicksGame % num2 / num2 * 360f, Vector3.up);
                }
                if (this.def.projectile.useGraphicClass)
                {
                    this.Graphic.Draw(vector, base.Rotation, this, rotation.eulerAngles.y);
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), vector, rotation, this.DrawMat, 0);
                }
                base.Comps_PostDraw();
            }
            private void DrawShadow(Vector3 drawLoc, float height)
            {
                if (Projectile_Missile.shadowMaterial == null)
                {
                    return;
                }
                float num = this.def.projectile.shadowSize * Mathf.Lerp(1f, 0.6f, height);
                Vector3 s = new Vector3(num, 1f, num);
                Vector3 b = new Vector3(0f, -0.01f, 0f);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawLoc + b, Quaternion.identity, s);
                Graphics.DrawMesh(MeshPool.plane10, matrix, Projectile_Missile.shadowMaterial, 0);
            }

            private static readonly Material shadowMaterial = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.Transparent);
            private SimpleCurve speedCurve = new()
            {
                new(0f,0f),
                new(0.1f,1f),
                new(0.2f,8f),
                new(0.6f,9f),
                new(0.9f,10f),
                new(1f,10f)
            };
            private float Area(CurvePoint a, CurvePoint b)=>0.5f*(a.y+b.y)*(b.x-a.x);
            private float CurveCovered(float perc)
            {
                float sum = 0f;
                CurvePoint a = speedCurve[0];
                foreach (CurvePoint p in speedCurve)
                {
                    if (p.x <= perc+0.01f&&p.x>0f)
                    {
                        sum+=Area(a,p);
                        a = p; 
                    }
                    else
                    {
                        sum+=Area(a,new(perc,speedCurve.Evaluate(perc)));
                        break;
                    }
                }
                return sum * SpeedMultiplier;
            }
            private float speedMultiplier = 0.136f;
            public float SpeedMultiplier
            {
                get{
                    if (speedMultiplier == 0f)
                    {
                        float num =0f;
                        for (int i = 0;i<speedCurve.PointsCount-1;i++)
                        {
                            num+=Area(speedCurve[i], speedCurve[i+1]);
                        }
                        speedMultiplier = 1f/num;
                        Log.Message(speedMultiplier);
                    }
                    return speedMultiplier;
                }
            }
            private SimpleCurve heightCurve = new()
            {
                new(0f,0f),
                new(0.05f,0.7f),
                new(0.1f,1f),
                new(0.15f,0.8f),
                new(0.4f,0.7f),
                new(0.7f,0.7f),
                new(0.9f,0.6f),
                new(1f,0f)
            };
        }*/

    public class SentryGun : Apparel, IVerbOwner
    {
        VerbTracker IVerbOwner.VerbTracker => throw new NotImplementedException();

        List<VerbProperties> IVerbOwner.VerbProperties => throw new NotImplementedException();

        List<Tool> IVerbOwner.Tools => throw new NotImplementedException();

        ImplementOwnerTypeDef IVerbOwner.ImplementOwnerTypeDef => throw new NotImplementedException();

        Thing IVerbOwner.ConstantCaster => throw new NotImplementedException();

        string IVerbOwner.UniqueVerbOwnerID()
        {
            throw new NotImplementedException();
        }

        bool IVerbOwner.VerbsStillUsableBy(Pawn p)
        {
            throw new NotImplementedException();
        }
    }
    public class CompProperties_SentryGun : CompProperties
    {
        public CompProperties_SentryGun()
        {
            compClass = typeof(Comp_SentryGun);
        }
        public ThingDef turretDef;
        public float angleOffset;
        //public bool autoAttack = true;
        public List<PawnRenderNodeProperties> renderNodeProperties;
    }
    public class Comp_SentryGun : ThingComp, IAttackTargetSearcher
    {
        public Thing gun;
        public int burstCooldownTicksLeft;
        private int burstWarmupTicksLeft;
        private LocalTargetInfo lastAttackedTarget;
        private int lastAttackTargetTick;
        private LocalTargetInfo currentTarget;
        private float curRotation;
        private bool fireAtWill = true;
        public Thing Thing => parent;
        public Apparel Apparel=>Thing as Apparel;
        public Pawn Parent=>Apparel.Wearer;
        public Verb CurrentEffectiveVerb => GunCompEq.PrimaryVerb;
        public CompEquippable GunCompEq => gun.TryGetComp<CompEquippable>();
        private bool WarmingUp => burstWarmupTicksLeft > 0;
        public LocalTargetInfo LastAttackedTarget => lastAttackedTarget;
        public int LastAttackTargetTick => lastAttackTargetTick;
        public CompProperties_SentryGun Props => (CompProperties_SentryGun)props;

        public bool CanShoot
        {
            get
            {
                if (Parent == null) return false;
                if (!Parent.Spawned || Parent.DeadOrDowned || !Parent.Awake()) return false;
                return !Parent.stances.stunner.Stunned && fireAtWill;
            }
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            MakeGun();
        }
        public override void CompTick()
        {
            base.CompTick();
            if (!CanShoot)
            {
                return;
            }
            if (currentTarget.IsValid)
            {
                curRotation = (currentTarget.Cell.ToVector3Shifted() - parent.DrawPos).AngleFlat() + Props.angleOffset;
            }
            CurrentEffectiveVerb.VerbTick();
            if (CurrentEffectiveVerb.state == VerbState.Bursting)return;

            if (WarmingUp)
            {
                if (--burstWarmupTicksLeft > 0)return;
                CurrentEffectiveVerb.TryStartCastOn(currentTarget, false, true, false, true);
                lastAttackTargetTick = Find.TickManager.TicksGame;
                lastAttackedTarget = currentTarget;
            }
            else
            {
                if (--burstCooldownTicksLeft > 0 || !parent.IsHashIntervalTick(10)) return;
                currentTarget = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable, null, 0f, 9999f);
                if (currentTarget.IsValid)
                {
                    burstWarmupTicksLeft = 1;
                    return;
                }
                ResetCurrentTarget();
            }

        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
        }
        public override List<PawnRenderNode> CompRenderNodes()
        {
            
            /*if (!this.Props.renderNodeProperties.NullOrEmpty<PawnRenderNodeProperties>() && Parent != null)
            {
                List<PawnRenderNode> list = new();
                foreach (PawnRenderNodeProperties pawnRenderNodeProperties in Props.renderNodeProperties)
                {
                    PawnRenderNode_TurretGun pawnRenderNode_TurretGun = (PawnRenderNode_TurretGun)Activator.CreateInstance(pawnRenderNodeProperties.nodeClass, new object[]
                    {
                        Parent,
                        pawnRenderNodeProperties,
                        Parent.Drawer.renderer.renderTree
                    });
                    pawnRenderNode_TurretGun.turretComp = this;
                    list.Add(pawnRenderNode_TurretGun);
                }
                return list;
            }*/
            return base.CompRenderNodes();
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.burstCooldownTicksLeft, "burstCooldownTicksLeft", 0, false);
            Scribe_Values.Look<int>(ref this.burstWarmupTicksLeft, "burstWarmupTicksLeft", 0, false);
            Scribe_TargetInfo.Look(ref this.currentTarget, "currentTarget");
            Scribe_Deep.Look<Thing>(ref this.gun, "gun", Array.Empty<object>());
            Scribe_Values.Look<bool>(ref this.fireAtWill, "fireAtWill", true, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.gun == null)
                {
                    Log.Error("CompTurrentGun had null gun after loading. Recreating.");
                    this.MakeGun();
                    return;
                }
                this.UpdateGunVerbs();
            }
        }

        private void MakeGun()
        {
            gun = ThingMaker.MakeThing(Props.turretDef, null);
            UpdateGunVerbs();
        }
        private void UpdateGunVerbs()
        {
            Verb verb = CurrentEffectiveVerb;
            verb.caster = Parent;
            verb.castCompleteCallback = delegate ()
            {
                burstCooldownTicksLeft = CurrentEffectiveVerb.verbProps.defaultCooldownTime.SecondsToTicks();
            };
        }
        private void ResetCurrentTarget()
        {
            currentTarget = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
        }
    }
}
