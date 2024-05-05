using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace WalkerGear
{
    [StaticConstructorOnStartup]
    public class SlotDef : Def
    {
        public bool isCoreFrame;//用來顯示的CoreFrame只會有一個
        public bool isWeapon;
        public List<SlotDef> supportedSlots;//填入組裝塢後提供的新槽位
        public static List<ThingDef> cachedAvailableComponents = new(); //使用该槽位的部件
        public int uiPriority; //slot在UI里占用的格子
        public SlotDef parentSlot;
        public override void ResolveReferences()
        {
            base.ResolveReferences();
            LongEventHandler.ExecuteWhenFinished(() => ResolveAvailableComps());
        }
        private void ResolveAvailableComps()
        {
            cachedAvailableComponents = DefDatabase<ThingDef>.AllDefs.Where<ThingDef>(t =>
            {
                if (!t.HasComp<CompWalkerComponent>()) return false;
                return t.GetCompProperties<CompProperties_WalkerComponent>().slot == this;
            }).ToList<ThingDef>();
        }
    }
}
