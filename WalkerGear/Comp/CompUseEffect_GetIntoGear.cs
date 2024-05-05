/*using RimWorld;
using Verse;

namespace WalkerGear
{
	public class CompUseEffect_GetIntoGear : CompUseEffect
	{
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			comp = parent.GetComp<CompWalkerGearBuilding>();
		}
        public override AcceptanceReport CanBeUsedBy(Pawn p)
        {
            if (parent.GetComp<CompRefuelable>().Fuel <= 0)
            {
                return "WalkerGear_NoFuel".Translate();
            }
            for (int i = 0; i < p.apparel.WornApparel.Count; i++)
            {
                if (p.apparel.WornApparel[i] is WalkerGear_Core)
                {
                    return "WalkerGear_AlreadyInWalkerGear".Translate(p.LabelCap);
                }
            }
            return true;
        }
		public override void DoEffect(Pawn usedBy)
		{
			IntVec3 intV3 = parent.Position;
			WalkerGear_Core core = ThingMaker.MakeThing(comp.Props.walkerGearDef.core) as WalkerGear_Core;
			if(parent.TryGetComp<CompColorable>() is CompColorable colorable && colorable.Active)
			{
				core.colorInt = colorable.Color;
			}
			core.Health = parent.HitPoints;
			usedBy.apparel.Wear(core, true, true);
			parent.Destroy(DestroyMode.Vanish);
			usedBy.Position = intV3;
			usedBy.Notify_Teleported();
			usedBy.drafter.Drafted = true;
		}
		private CompWalkerGearBuilding comp;
	}
}
*/