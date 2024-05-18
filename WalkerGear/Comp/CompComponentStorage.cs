using RimWorld;
using Verse;

namespace WalkerGear
{
    public class CompComponentStorage:ThingComp
    {
        public Building_Storage Parent => (Building_Storage)this.parent;
        public Thing maintanenceTar;        
        public bool CheckMaintenance()
        {
            maintanenceTar=null;
            foreach (Thing thing in Parent.slotGroup.HeldThings)
            {
                if (thing is ThingWithComps twc && twc.TryGetComp<CompWalkerComponent>(out CompWalkerComponent c) && c.NeedMaintenance)
                {
                    maintanenceTar = thing;
                    return true;
                }
            }
            return false;
        }
    }
}
