using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace WalkerGear
{
    [HarmonyPatch(typeof(CaptureUtility), nameof(CaptureUtility.CanArrest),
        new Type[] { typeof(Pawn), typeof(Pawn), typeof(string) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
    static class CaptureUtility_CanArrest
    {
        [HarmonyPrefix]
        static bool CanArrest(ref bool __result, Pawn victim, out string reason)
        {
            reason = "";
            if (MechUtility.PawnWearingWalkerCore(victim))
            {
                reason = "WG_Disabled_VictimInWalkerCore";
                __result = false;
                return false;
            }
            return true;
        }
    }
}
