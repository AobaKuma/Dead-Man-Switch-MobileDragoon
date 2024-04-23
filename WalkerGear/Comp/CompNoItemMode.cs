using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WalkerGear
{
    public class CompNoItemMode : ThingComp
    {
        public override void CompTick()
        {
            base.CompTick();
            if(DestroyNow)
            {
                parent.Destroy();
            }
            if(parent.Spawned)
            {
                DestroyNow = true;
            }
        }

        public bool DestroyNow = false;
    }
}
