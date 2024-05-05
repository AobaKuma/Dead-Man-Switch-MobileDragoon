using RimWorld.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;


namespace WalkerGear
{
    public class CompWalkerComponent : ThingComp,IReloadableComp
    {
        public bool NeedMaintenance=> NeedAmmo || NeedRepair;
        public bool NeedAmmo=> remainingCharges < Props.maxCharges;
        public bool NeedRepair=> parent.HitPoints < parent.MaxHitPoints;
        public CompProperties_WalkerComponent Props
        {
            get
            {
                return this.props as CompProperties_WalkerComponent;
            }
        }
        public ThingDef AmmoDef => Props.ammoDef;
        public int MaxCharges => Props.maxCharges;
        public int RemainingCharges => remainingCharges;
        public int NeedAmmoCount=> (Props.maxCharges - remainingCharges) * Props.ammoCountPerCharge;

        public Thing ReloadableThing => throw new System.NotImplementedException();

        public int BaseReloadTicks => throw new System.NotImplementedException();

        public string LabelRemaining => throw new System.NotImplementedException();

        public override void PostPostMake()
        {
            base.PostPostMake();
            this.remainingCharges = 0;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.remainingCharges, "remainingCharges", -999, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.remainingCharges == -999)
            {
                this.remainingCharges = 0;
            }
        }

        public bool NeedsReload(bool allowForceReload)=> NeedAmmo;

        public int MinAmmoNeeded(bool allowForcedReload)=>NeedAmmoCount;

        public int MaxAmmoNeeded(bool allowForcedReload) => NeedAmmoCount;

        public int MaxAmmoAmount()=>MaxCharges;

        public void ReloadFrom(Thing ammo)
        {
            if (!this.NeedsReload(true))
            {
                return;
            }
            if (ammo.stackCount < this.Props.ammoCountPerCharge)
            {
                return;
            }
            int num = Mathf.Clamp(ammo.stackCount / this.Props.ammoCountPerCharge, 0, this.MaxCharges - this.RemainingCharges);
            ammo.SplitOff(num * this.Props.ammoCountPerCharge).Destroy(DestroyMode.Vanish);
            this.remainingCharges += num;

        }

        public string DisabledReason(int minNeeded, int maxNeeded)=>"";

        public bool CanBeUsed(out string reason) { reason = ""; return false; }

        public int remainingCharges;
    }
    public class CompProperties_WalkerComponent : CompProperties
    {
        public CompProperties_WalkerComponent()
        {
            this.compClass = typeof(CompWalkerComponent);
        }
        public bool isApparal = false; //判断是装甲还是物品
        public ThingDef EquipedThingDef;//提供的裝備
        public ThingDef ItemDef;//物品def
        public SlotDef slot;//被填裝時的槽位

        //联动可装填Comp
        public ThingDef ammoDef;
        public int maxCharges;
        public int ammoCountPerCharge;
        public float repairEfficiency = 0.01f;//作為物品被修理的效率

    }


}
