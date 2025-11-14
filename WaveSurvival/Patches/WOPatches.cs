using GameData;
using HarmonyLib;
using WaveSurvival.CustomWave;

namespace WaveSurvival.Patches
{
    [HarmonyPatch]
    internal class Patch_CheckAndExecuteEventsOnTrigger
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(WardenObjectiveManager), nameof(WardenObjectiveManager.CheckAndExecuteEventsOnTrigger), new System.Type[] {
            typeof(WardenObjectiveEventData),
            typeof(eWardenObjectiveEventTrigger),
            typeof(bool),
            typeof(float)
        })]
        private static void Pre_CheckAndExecuteEventsOnTrigger(WardenObjectiveEventData eventToTrigger,
            eWardenObjectiveEventTrigger trigger,
            bool ignoreTrigger,
            float currentDuration)
        {
            if (eventToTrigger == null || !ignoreTrigger && eventToTrigger.Trigger != trigger || currentDuration != 0.0 && eventToTrigger.Delay <= currentDuration)
                return;

            WaveManager.Internal_OnWardenEvent(eventToTrigger.Type);
        }
    }
}