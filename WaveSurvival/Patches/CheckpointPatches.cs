using HarmonyLib;
using UnityEngine;
using WaveSurvival.Attributes;
using WaveSurvival.CustomWave;

namespace WaveSurvival.Patches
{
    [HarmonyPatch]
    internal static class CheckpointPatches
    {
        private static Vector3 _lastCheckpointPos = Vector3.zero;
        [HarmonyPatch(typeof(CheckpointManager), nameof(CheckpointManager.OnStateChange))]
        [HarmonyWrapSafe]
        [HarmonyPostfix]
        public static void OnCheckpointStateChange(pCheckpointState newState)
        {
            if (newState.lastInteraction == eCheckpointInteractionType.StoreCheckpoint && _lastCheckpointPos != newState.doorLockPosition)
            {
                _lastCheckpointPos = newState.doorLockPosition;
                WaveManager.Internal_OnCheckpointReached();
            }
            else if (newState.lastInteraction == eCheckpointInteractionType.ReloadCheckpoint)
            {
                WaveManager.Internal_OnCheckpointReload();
            }
        }

        [InvokeOnCleanup]
        private static void OnCleanup() => _lastCheckpointPos = Vector3.zero;
    }
}
