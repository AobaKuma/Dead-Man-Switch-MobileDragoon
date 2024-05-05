using System;
using System.Linq;
using System.Text;
using Verse;

namespace WalkerGear
{
    public class CompWalkerGearBuilding : ThingComp
    {
        public CompProperties_WalkerGearBuilding Props
        {
            get
            {
                return this.props as CompProperties_WalkerGearBuilding;
            }
        }
    }
    public class CompProperties_WalkerGearBuilding : CompProperties
    {
        public CompProperties_WalkerGearBuilding()
        {
            this.compClass = typeof(CompWalkerGearBuilding);
        }
        public WalkerGearDef walkerGearDef;
    }
}
