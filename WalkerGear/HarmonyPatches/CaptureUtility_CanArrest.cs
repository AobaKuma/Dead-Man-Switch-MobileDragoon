using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace WalkerGear
{
    [HarmonyPatch(
        typeof(CaptureUtility),
        nameof(CaptureUtility.TryGetBed),
        new Type[] { typeof(Pawn), typeof(Pawn), typeof(Thing) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
    internal static class CaptureUtility_TryGetBed
    {
        [HarmonyPrefix]
        static bool TryGetBed(Pawn victim, ref bool __result)
        {
            Log.Message("p2");
            if (MechUtility.PawnWearingWalkerCore(victim))
            {
                Messages.Message("WG_Disabled_VictimInWalkerCore".Translate(), MessageTypeDefOf.RejectInput, false);
                __result = false;
                return false;
            }
            return true;
        }
    }
}