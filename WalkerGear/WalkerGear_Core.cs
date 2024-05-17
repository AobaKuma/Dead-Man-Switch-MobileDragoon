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
		}
		public override bool CheckPreAbsorbDamage(DamageInfo dinfo)
		{
			if (!dinfo.Def.harmsHealth)
			{
				return true;
			}
			
            Health -= GetPostArmorDamage(dinfo);
			return true;
		}
		
		public float GetPostArmorDamage(DamageInfo dinfo)
		{
            float amount = dinfo.Amount;
            DamageDef damageDef = dinfo.Def;
            if (damageDef.armorCategory != null)
            {
                StatDef armorRatingStat = damageDef.armorCategory.armorRatingStat;
                ArmorUtility.ApplyArmor(ref amount, dinfo.ArmorPenetrationInt, this.GetStatValue(armorRatingStat), null, ref damageDef, Wearer, out _);
            }
			return amount;
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
		public void TryBackToMaintainBay()
		{
            if (Wearer.Position.GetFirstThing(Wearer.Map, ThingDefOf.DMS_Building_MaintenanceBay) is Building_MaintenanceBay building)
            {
                //WIP
            }
        }
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
            GenExplosion.DoExplosion(Wearer.Position, Wearer.Map, 5, DamageDefOf.Bomb, null, 5);

            Building building = (Building)ThingMaker.MakeThing(ThingDefOf.DMS_Building_Wreckage);
            building.SetFactionDirect(Wearer.Faction);
            building.Rotation = Rot4.South;
			
            GenPlace.TryPlaceThing(building, Wearer.Position, Wearer.Map, ThingPlaceMode.Direct);
            
            if (building.TryGetComp(out CompWreckage compWreckage))
            {
                Wearer.DeSpawnOrDeselect();
                compWreckage.pawnContainer.TryAdd(Wearer);
                foreach (var m in modules)
                {
                    Wearer.apparel.Remove((Apparel)m);
					m.DeSpawnOrDeselect();
                    m.HitPoints = 1;
                    if (Rand.Bool) compWreckage.moduleContainer.TryAdd(m);
                }
            }

            
        }
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref healthInt, "healthInt",-1);
			Scribe_Values.Look(ref colorInt, "colorInt",Color.white);
		}

		public Color colorInt = Color.white;
		private float combinedHealth;
		private float healthInt = -1;
		public List<Thing> modules = new();
        public static readonly Texture2D GetOutIcon = ContentFinder<Texture2D>.Get("Things/GetOffWalker", true);
    }
}

