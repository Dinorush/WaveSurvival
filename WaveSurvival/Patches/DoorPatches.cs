using HarmonyLib;
using LevelGeneration;
using WaveSurvival.CustomWave;

namespace WaveSurvival.Patches
{
    [HarmonyPatch]
    internal static class DoorPatches
    {
        [HarmonyPatch(typeof(LG_SecurityDoor), nameof(LG_SecurityDoor.OnDoorIsOpened))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        private static void OnDoorOpened(LG_SecurityDoor __instance)
        {
            if (WaveManager.IsActive && __instance.LinkedToZoneData != null)
                WaveManager.Internal_OnDoorOpened(__instance);
        }
    }
}
