using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace WalkerGear
{

    [HarmonyPatch(typeof(PawnGenerator), "GenerateGearFor")]
    static class PawnGenerator_Patch
    {
        static void Postfix(Pawn pawn, PawnGenerationRequest request)
        {
            PawnKindDef def = request.KindDef;
            if (def == null) return;
            ModExtForceApparelGen modExt = def.GetModExtension<ModExtForceApparelGen>();
            if (modExt == null) return;
            if (DebugSettings.godMode) Log.Message("ModExtForceApparelGen loaded, Force Apparel Gen");
            WalkerGear_Core core = null;

            foreach (ThingDef apparelDef in modExt.apparels)
            {
                Apparel apparel = (Apparel)ThingMaker.MakeThing(apparelDef);
                Color color = pawn.kindDef.specificApparelRequirements.Where(req => req.GetColor() != Color.white).First().GetColor();
                if (color!= null)
                {
                    apparel.SetColor(color);
                }
                pawn.apparel.Wear(apparel);
                if (apparel is WalkerGear_Core core2)
                {
                    core = core2;
                }
            }
            core?.RefreshHP(true);
            core.Health = core.HealthMax * modExt.StructurePointRange.RandomInRange;
        }
    }
}

