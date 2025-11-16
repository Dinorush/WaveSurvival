using HarmonyLib;
using LevelGeneration;
using WaveSurvival.CustomWave;

namespace WaveSurvival.Patches
{
    [HarmonyPatch]
    internal static class DoorPatches
    {
        [HarmonyPatch(typeof(LG_Gate), nameof(LG_Gate.IsTraversable), MethodType.Setter)]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static void OnDoorStateChanged(LG_Gate __instance, ref bool __state)
        {
            __state = __instance.IsTraversable;
        }

        [HarmonyPatch(typeof(LG_Gate), nameof(LG_Gate.IsTraversable), MethodType.Setter)]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void OnDoorStateChanged(LG_Gate __instance, bool __state)
        {
            if (__instance.IsTraversable == __state || __instance.SpawnedDoor?.TryCast<LG_SecurityDoor>() == null) return;

            ZoneTree.Internal_OnDoorStateChanged(__instance, !__state);
        }
    }
}
