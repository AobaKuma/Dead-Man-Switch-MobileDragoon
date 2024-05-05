using RimWorld;
using Verse;

namespace WalkerGear
{
    public class CompComponentStorage:ThingComp
    {
        public Building_Storage Parent => (Building_Storage)this.parent;
        public bool needMaintenance;
        public override void CompTickRare()
        {
            base.CompTickRare();
            CheckMaintenance();
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            CheckMaintenance();
        }
        private void CheckMaintenance()
        {
            foreach (Thing thing in Parent.slotGroup.HeldThings)
            {
                if (thing is ThingWithComps twc && twc.TryGetComp<CompWalkerComponent>(out CompWalkerComponent c) && c.NeedMaintenance)
                {
                    needMaintenance = true;
                    break;
                }
            }
            needMaintenance = false;
        }
    }
}
