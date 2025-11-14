using GameData;
using SNetwork;
using WaveSurvival.Attributes;
using WaveSurvival.CustomWaveData;
using WaveSurvival.CustomWaveData.WaveObjective;

namespace WaveSurvival.CustomWave
{
    public sealed partial class WaveManager
    {
        private readonly Dictionary<eWardenObjectiveEventType, WaveObjectiveData> _eventStartData = new();
        private readonly Dictionary<eWardenObjectiveEventType, WaveObjectiveData> _eventStopData = new();

        private void SetupWardenEventObjectives(bool onBuild = false)
        {
            var expData = RundownManager.GetActiveExpeditionData();
            var layoutID = RundownManager.ActiveExpedition.LevelLayoutData;

            _eventStartData.Clear();
            _eventStopData.Clear();
            foreach (var data in DataManager.ObjectiveDatas)
            {
                if (!data.Level.IsMatch(layoutID, expData.tier, expData.expeditionIndex)) continue;

                if (data.StartEvent != eWardenObjectiveEventType.None)
                {
                    if (!_eventStartData.TryAdd(data.StartEvent, data))
                        DinoLogger.Error($"Unable to register data with StartEvent {data.StartEvent} (already in use)");
                }
                else if (onBuild)
                    ActiveObjective = data;

                if (data.StopEvent != eWardenObjectiveEventType.None)
                {
                    if (!_eventStopData.TryAdd(data.StopEvent, data))
                        DinoLogger.Error($"Unable to register data with StopEvent {data.StopEvent} (already in use)");
                }
            }
        }

        [InvokeOnBuildStart]
        private static void OnBuildStart()
        {
            Current.SetupWardenEventObjectives(onBuild: true);
        }

        internal static void Internal_OnWardenEvent(eWardenObjectiveEventType type)
        {
            if (type == eWardenObjectiveEventType.None) return;

            // JFS - If host migrates before an objective is started, this can recover it
            IsMaster = SNet.IsMaster;
            if (!IsMaster) return;

            if (IsActive && Current._eventStopData.TryGetValue(type, out var data) && data.NetworkID == ActiveObjective?.NetworkID)
            {
                WaveNetwork.SetWavesFinished();
            }

            if (!IsActive && Current._eventStartData.TryGetValue(type, out data))
            {
                ActiveObjective = data;
                Current.StartObjective();
            }
        }
    }
}
