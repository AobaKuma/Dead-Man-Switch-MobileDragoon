using RimWorld;
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
	public class WalkerGear_Core : Apparel
    {
		//public WalkerGearDef System => this.def.GetCompProperties<CompProperties_WalkerGearBuilding>().walkerGearDef;
		public float HealthMax => HitPoints<combinedHealth?combinedHealth:HitPoints;
		public float Health
		{
			get
			{
				return healthInt == -1 ? healthInt = HealthMax : healthInt;
			}
			set
			{
				healthInt = value;
				if (healthInt < 0) healthInt = 0;
				if (healthInt > HealthMax) healthInt = HealthMax;
			}
		}
		public float HealthDamaged=>HealthMax-Health;
        public override IEnumerable<Gizmo> GetWornGizmos()
		{
			foreach(Gizmo gizmo in base.GetWornGizmos())
			{
				yield return gizmo;
			}
			//yield return new Gizmo_Gui(){core = this};
/*			var command = new Command_Action()
			{
				Order = 10f,
				defaultLabel = "WalkerGear_LeaveMech".Translate(),
				icon = GetOutIcon,
				action = delegate ()
				{
					GetOut();
				}
			};
			if(Wearer.Faction != Faction.OfPlayer)
            {
				command.Disable();
			}
			yield return command;*/
		}
		public override bool CheckPreAbsorbDamage(DamageInfo dinfo)
		{
			if (!dinfo.Def.harmsHealth)
			{
				return true;
			}
			Health -= dinfo.Amount;
			return true;
		}
		public override void Tick()
		{
			base.Tick();
			if(Wearer == null)
			{
				return;
			}
			if (Health <= 0)
			{
				GearDestory();
				return;
			}
		}
		/*public void ForceGetOut()
		{
			if (Wearer.InMentalState)
			{
				GetOut();
				return;
			}
			if (Wearer.Dead || Wearer.Downed)
			{
				GetOut();
			}
		}*/
		public void TryBackToMaintainBay()
		{
            if (Wearer.Position.GetFirstThing(Wearer.Map, ThingDefOf.DMS_Building_MaintenanceBay) is Building_MaintenanceBay building)
            {
                //WIP
            }
        }
		/*public void GetOut()
		{
			if(Wearer.equipment.Primary.def == System.weapon)
			{
				Wearer.equipment.Remove(Wearer.equipment.Primary);
			}
			Building building = ThingMaker.MakeThing(System.building) as Building;
			if (building.TryGetComp<CompColorable>() is CompColorable colorable)
			{
				colorable.SetColor(colorInt);
			}
			building.HitPoints = (int)healthInt;
			building.SetFactionDirect(Wearer.Faction);
			building.Rotation = Rot4.South;
			if (fuelInt > 0) 
			{
				building.TryGetComp<CompRefuelable>().Refuel(fuelInt); 
			}
			GenPlace.TryPlaceThing(building, Wearer.Position, Wearer.Map, ThingPlaceMode.Direct);
			Pawn pawn = Wearer;
			pawn.apparel.Remove(this);
		}*/
		public bool RefreshHP()
		{
			CheckModules();
			float hp=0f; modules.ForEach((t)=>hp+=t.HitPoints);
			combinedHealth = hp;
			healthInt = combinedHealth;
            return true;
        }
		public void CheckModules()
		{
			modules.Clear();
			foreach(Apparel a in Wearer.apparel.WornApparel)
			{
				if (a.TryGetComp<CompWalkerComponent>(out var c))
				{
					modules.Add(a);
                }
			}
		}
		public void GearDestory()
		{
            Building building = (Building)ThingMaker.MakeThing(ThingDefOf.DMS_Building_Wreckage);
            building.SetFactionDirect(Wearer.Faction);
            building.Rotation = Rot4.South;
            GenPlace.TryPlaceThing(building, Wearer.Position, Wearer.Map, ThingPlaceMode.Direct);
			List<Thing> innercontainer=new();
            foreach (var m in modules)
            {
                Wearer.apparel.Remove((Apparel)m);
				if(Rand.Bool) innercontainer.Add(m);
            }
            



            GenExplosion.DoExplosion(Wearer.Position, Wearer.Map, 5, DamageDefOf.Bomb, null, 5);
        }
		public override void DrawWornExtras()
		{
			Pawn wearer = Wearer;
			/*if (wearer.Spawned)
			{
				if (Shield > 0)
				{
					System.shieldGraphic.graphicData.Graphic.GetColoredVersion(System.shieldGraphic.graphicData.shaderType.Shader, colorInt, Color.white).Draw(wearer.DrawPos + System.shieldGraphic.offSet, Wearer.Rotation, Wearer, 0f);
				}
				System.frontGraphic.graphicData.Graphic.GetColoredVersion(System.shieldGraphic.graphicData.shaderType.Shader, colorInt, Color.white).Draw(wearer.DrawPos + System.frontGraphic.offSet, Wearer.Rotation, Wearer, 0f);
				System.backGraphic.graphicData.Graphic.GetColoredVersion(System.shieldGraphic.graphicData.shaderType.Shader, colorInt, Color.white).Draw(wearer.DrawPos + System.backGraphic.offSet, Wearer.Rotation, Wearer, 0f);
			}*/
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref healthInt, "healthInt",-1);
			Scribe_Values.Look(ref shieldInt, "shieldInt",-1);
			Scribe_Values.Look(ref colorInt, "colorInt",Color.white);
		}

		public Color colorInt = Color.white;
		private float combinedHealth = 0f;
		private float healthInt = -1;
		private float shieldInt = -1;
		public List<Thing> modules = new();
        public static readonly Texture2D GetOutIcon = ContentFinder<Texture2D>.Get("Things/GetOffWalker", true);
    }
}

