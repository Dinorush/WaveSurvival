using GameData;
using SNetwork;
using WaveSurvival.Attributes;
using WaveSurvival.CustomWaveData;
using WaveSurvival.CustomWaveData.WaveObjective;

namespace WaveSurvival.CustomWave
{
    public sealed partial class WaveManager
    {
        private readonly Dictionary<eDimensionIndex, WaveObjectiveData> _dimensionStartData = new();
        private readonly Dictionary<eWardenObjectiveEventType, WaveObjectiveData> _eventStartData = new();
        private readonly Dictionary<eWardenObjectiveEventType, WaveObjectiveData> _eventStopData = new();

        private void LoadLevelObjectives()
        {
            if (RundownManager.ActiveExpedition == null) return;
            if (!RundownManager.TryGetIdFromLocalRundownKey(RundownManager.ActiveRundownKey, out var rundownID)) return;

            var expData = RundownManager.GetActiveExpeditionData();
            var layoutID = RundownManager.ActiveExpedition.LevelLayoutData;

            _dimensionStartData.Clear();
            _eventStartData.Clear();
            _eventStopData.Clear();
            foreach (var data in DataManager.ObjectiveDatas)
            {
                if (data.RundownID != 0 && data.RundownID != rundownID) continue;
                if (!data.Level.IsMatch(layoutID, expData.tier, expData.expeditionIndex)) continue;

                if (data.StartEvent != eWardenObjectiveEventType.None)
                {
                    if (!_eventStartData.TryAdd(data.StartEvent, data))
                        DinoLogger.Error($"Unable to register data with StartEvent {data.StartEvent} (already in use)");
                }
                else if (!_dimensionStartData.TryAdd(data.DimensionIndex, data))
                    DinoLogger.Error($"Unable to register data with no start event and DimensionIndex {data.DimensionIndex} (already in use)");

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
            Current.LoadLevelObjectives();
            if (Current._dimensionStartData.Remove(eDimensionIndex.Reality, out var data))
                ActiveObjective = data;
        }

        private void CheckpointStoreObjectives()
        {
            _checkpointData.dimensionStartData = new(_dimensionStartData);
        }

        private void CheckpointReloadObjectives()
        {
            _dimensionStartData.Clear();
            foreach (var kv in _checkpointData.dimensionStartData)
                _dimensionStartData.Add(kv.Key, kv.Value);
        }

        internal static void Internal_OnWarp(eDimensionIndex dimensionIndex)
        {
            if (!IsMaster || IsActive) return;

            if (!Current._dimensionStartData.Remove(dimensionIndex, out var data)) return;

            ActiveObjective = data;
            Current.StartObjective();
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
