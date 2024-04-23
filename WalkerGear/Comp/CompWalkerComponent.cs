using System.Collections.Generic;
using System.Linq;
using Verse;


namespace WalkerGear
{
    public class CompWalkerComponent : ThingComp
    {
        public CompProperties_WalkerComponent Props
        {
            get
            {
                return this.props as CompProperties_WalkerComponent;
            }
        }
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            return base.CompFloatMenuOptions(selPawn);
        }
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
                this.remainingCharges = this.Props.maxCharges;
            }
        }

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
        public int ammoCountToRefill;
        public int ammoCountPerCharge;
        public int baseReloadTicks = 60;
        public int maxCharges;

        public float repairEfficiency = 0.01f;//作為物品被修理的效率

    }


}
