using HarmonyLib;
using Verse;
using Verse.AI;
using WalkerGear;

namespace WalkerGear
{
    [HarmonyPatch(typeof(GenAI), nameof(GenAI.CanBeCaptured))]
    static class GenAI_CanBeCaptured
    {
        [HarmonyPostfix]
        static void CanBeCaptured(Pawn pawn, ref bool __result)
        {
            if (MechUtility.PawnWearingWalkerCore(pawn)) __result = false;
        }
    }

    [HarmonyPatch(typeof(GenAI), nameof(GenAI.CanBeArrestedBy))]
    static class GenAI_CanBeArrestedBy
    {
        [HarmonyPostfix]
        static void CanBeCaptured(Pawn pawn, Pawn arrester, ref bool __result)
        {
            if (MechUtility.PawnWearingWalkerCore(pawn)) __result = false;
        }
    }
}