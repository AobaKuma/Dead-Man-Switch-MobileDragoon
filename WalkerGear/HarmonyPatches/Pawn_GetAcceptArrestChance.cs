using HarmonyLib;
using System.Linq;
using Verse;

namespace WalkerGear
{
    //[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetAcceptArrestChance))]
    //static class Pawn_GetAcceptArrestChance
    //{
    //    [HarmonyPostfix]
    //    static void GetAcceptArrestChance(Pawn __instance, Pawn arrester, ref float __result)
    //    {
    //        if (MechUtility.PawnWearingWalkerCore(__instance)) __result = 0;
    //    }
    //}
}
