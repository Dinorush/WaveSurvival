using HarmonyLib;
using Player;
using WaveSurvival.CustomWave;

namespace WaveSurvival.Patches
{
    [HarmonyPatch]
    internal static class WarpPatches
    {
        [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.WarpTo))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void PreWarp(PlayerAgent __instance, ref eDimensionIndex __state)
        {
            __state = __instance.DimensionIndex;
        }

        [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.WarpTo))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void OnWarp(PlayerAgent __instance, eDimensionIndex __state)
        {
            var dimensionIndex = __instance.DimensionIndex;
            if (__state != dimensionIndex)
                WaveManager.Internal_OnWarp(dimensionIndex);
        }
    }
}
