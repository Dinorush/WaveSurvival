using HarmonyLib;
using WaveSurvival.CustomWave;
using SNetwork;

namespace WaveSurvival.Patches
{
    [HarmonyPatch]
    internal static class LobbyPatches
    {
        [HarmonyPatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.OnPlayerJoinedSessionHub))]
        [HarmonyPostfix]
        private static void Post_Joined(SNet_Player player)
        {
            if (WaveManager.TryGetActiveObjective(out var objective))
                WaveNetwork.SendObjective(objective, player);
        }
    }
}
