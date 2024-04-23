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
		public WalkerGearDef System => this.def.GetCompProperties<CompProperties_WalkerGearBuilding>().walkerGearDef;
		public float HealthMax => System.health;
		public float ShieldMax => System.shield;
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
		public float Shield
		{
			get
			{
				return shieldInt == -1 ? shieldInt = ShieldMax : shieldInt;
			}
			set
			{
				shieldInt = value;
				if (shieldInt < 0) shieldInt = 0;
				if (shieldInt > ShieldMax) shieldInt = ShieldMax;
			}
		}
		public override IEnumerable<Gizmo> GetWornGizmos()
		{
			foreach(Gizmo gizmo in base.GetWornGizmos())
			{
				yield return gizmo;
			}
			yield return new Gizmo_Gui()
			{
				core = this
			};
			var command = new Command_Action()
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
			yield return command;
		}
		public override bool CheckPreAbsorbDamage(DamageInfo dinfo)
		{
			if((System.sufferEMP && dinfo.Def == DamageDefOf.EMP) || (System.sufferStun && dinfo.Def == DamageDefOf.Stun))
			{
				Wearer.stances.stunner.StunFor(Mathf.RoundToInt(dinfo.Amount * 30f), dinfo.Instigator, true, true);
				return true;
			}
			if (!dinfo.Def.harmsHealth)
			{
				return true;
			}
			if (Shield > 0) Shield -= dinfo.Amount;
			else Health -= dinfo.Amount;
			return true;
		}
		public override void Tick()
		{
			base.Tick();
			if(Wearer == null)
			{
				return;
			}
			CheckWeapon();
			if (fuelInt > 0)
			{
				fuelInt -= comsumptionPerTick;
				if (fuelInt <= 0) 
				{
					fuelInt = 0;
					GetOut();
					return;
				}
			}
			if (Health <= 0)
			{
				GearDestory();
				return;
			}
			ForceGetOut();
		}
		void ForceGetOut()
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
		}
		private void CheckWeapon()
		{
			if (!System.equipWeapon && Wearer.equipment.Primary != null && Wearer.equipment.Primary.def != System.weapon)
			{
				if (Wearer.Spawned)
				{
					Wearer.equipment.TryDropEquipment(Wearer.equipment.Primary, out _, Wearer.Position);
				}
				else
				{
					Wearer.inventory.TryAddItemNotForSale(Wearer.equipment.Primary);
					Wearer.equipment.Remove(Wearer.equipment.Primary);
				}
			}
			if (Wearer.equipment.Primary == null)
			{
				Wearer.equipment.AddEquipment(ThingMaker.MakeThing(System.weapon) as ThingWithComps);
			}
		}
		public void TryBackToMaintainBay()
		{
            Building_MaintenanceBay building = Wearer.Position.GetFirstThing(Wearer.Map, ThingDefOf.DMS_Building_MaintenanceBay) as Building_MaintenanceBay;
			if (building != null)
			{ 
			//WIP
			}
        }
		public void GetOut()
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
		}
		public void GearDestory()
		{
			if (Wearer.equipment.Primary.def == System.weapon)
			{
				Wearer.equipment.Remove(Wearer.equipment.Primary);
			}
			Pawn pawn = Wearer;
			pawn.apparel.Remove(this);
			Building building = ThingMaker.MakeThing(System.wreckage) as Building;
			building.SetFactionDirect(pawn.Faction);
			building.Rotation = Rot4.South;
			GenPlace.TryPlaceThing(building, pawn.Position, pawn.Map, ThingPlaceMode.Direct);
			GenExplosion.DoExplosion(pawn.Position,pawn.Map,System.selfExplosive.radius,System.selfExplosive.damageDef,null,System.selfExplosive.amount);
		}
		public override void DrawWornExtras()
		{
			Pawn wearer = Wearer;
			if (wearer.Spawned)
			{
				if (Shield > 0)
				{
					System.shieldGraphic.graphicData.Graphic.GetColoredVersion(System.shieldGraphic.graphicData.shaderType.Shader, colorInt, Color.white).Draw(wearer.DrawPos + System.shieldGraphic.offSet, Wearer.Rotation, Wearer, 0f);
				}
				System.frontGraphic.graphicData.Graphic.GetColoredVersion(System.shieldGraphic.graphicData.shaderType.Shader, colorInt, Color.white).Draw(wearer.DrawPos + System.frontGraphic.offSet, Wearer.Rotation, Wearer, 0f);
				System.backGraphic.graphicData.Graphic.GetColoredVersion(System.shieldGraphic.graphicData.shaderType.Shader, colorInt, Color.white).Draw(wearer.DrawPos + System.backGraphic.offSet, Wearer.Rotation, Wearer, 0f);
				
				return;
			}
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref healthInt, "healthInt",-1);
			Scribe_Values.Look(ref shieldInt, "shieldInt",-1);
			Scribe_Values.Look(ref fuelInt, "fuelInt");
			Scribe_Values.Look(ref colorInt, "colorInt",Color.white);
		}
		public Color colorInt = Color.white;
		private float healthInt = -1;
		private float shieldInt = -1;
		public float fuelInt = 0;
		const float comsumptionPerTick = 0.005f;
		public static readonly Texture2D GetOutIcon = ContentFinder<Texture2D>.Get("Things/GetOffWalker", true);
    }
}
