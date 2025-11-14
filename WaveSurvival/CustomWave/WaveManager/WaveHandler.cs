using GameData;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using WaveSurvival.CustomWaveData;
using WaveSurvival.CustomWaveData.Wave;
using WaveSurvival.CustomWaveData.WaveObjective;
using WaveSurvival.Settings;

namespace WaveSurvival.CustomWave
{
    public sealed partial class WaveManager
    {
        class WaveStep
        {
            public readonly WaveData Wave;
            public readonly WaveEventData EventData;

            public WaveStep(WaveData wave, WaveEventData eventData)
            {
                Wave = wave;
                EventData = eventData;
            }
        }

        private readonly List<WaveStep> _waves = new();
        private readonly Dictionary<int, ActiveWave> _activeWaves = new();
        private readonly List<(int, ActiveWave)> _finishedWaves = new();

        private float _nextWaveTime = 0f;
        private float _nextWaveDelayCache = 0f;
        private int _currentWave = -1;

        private void UpdateWaves()
        {
            if ((_nextWaveTime > 0 && Clock.Time >= _nextWaveTime) || (_activeWaves.Count == 0 && Input.GetKeyDown(ClientSettings.SkipWaveBind)))
                StartNextWave();

            foreach ((var index, var wave) in _activeWaves)
            {
                if (wave.UpdateCheckDone())
                    _finishedWaves.Add((index, wave));
            }

            foreach ((var index, var wave) in _finishedWaves)
            {
                FinishWave(index, wave);
            }
            _finishedWaves.Clear();
        }

        private void StartNextWave()
        {
            if (_currentWave >= _waves.Count - 1) return;

            var step = _waves[++_currentWave];
            var eventData = step.EventData;
            _nextWaveTime = eventData.TimeToNextOnStart > 0 ? Clock.Time + eventData.TimeToNextOnStart : 0;
            WaveNetwork.SetWave(_currentWave, step.Wave.NetworkID, _nextWaveTime, WaveState.Wave);

            foreach (var we in eventData.EventsOnWaveStart)
                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(we, eWardenObjectiveEventTrigger.None, true);
            _activeWaves.Add(_currentWave, new(step.Wave, eventData));
        }

        [HideFromIl2Cpp]
        private void FinishWave(int index, ActiveWave wave)
        {
            bool allWavesComplete = _finishedWaves.Count == _activeWaves.Count;
            if (wave.EventData.EndOnAllWavesEnd && !allWavesComplete) return;

            // If the current wave is the one that finished, advance the message but don't modify the timer until all waves are done
            if (_currentWave == index)
            {
                _nextWaveDelayCache = wave.EventData.TimeToNextOnEnd;
                if (!allWavesComplete)
                    WaveNetwork.SetWave(_currentWave + 1, GetNetworkID(_currentWave + 1), _nextWaveTime, WaveState.RemainingWaves);
            }

            foreach (var we in wave.EventData.EventsOnWaveEnd)
                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(we, eWardenObjectiveEventTrigger.None, true);
            _activeWaves.Remove(index);

            if (allWavesComplete)
            {
                float nextTime = _nextWaveDelayCache > 0 ? Clock.Time + _nextWaveDelayCache : 0;
                if (_nextWaveTime == 0)
                    _nextWaveTime = nextTime;
                else
                    _nextWaveTime = Math.Min(_nextWaveTime, nextTime);

                if (_currentWave + 1 == _waves.Count)
                {
                    WaveNetwork.SetWavesFinished();
                }
                else
                {
                    WaveNetwork.SetWave(_currentWave + 1, GetNetworkID(_currentWave + 1), _nextWaveTime, WaveState.Transition);
                }
            }
        }

        private int GetNetworkID(int wave) => wave < _waves.Count ? _waves[wave].Wave.NetworkID : -1;

        private void SetupWaves()
        {
            foreach (var group in ActiveObjective!.WaveSequence)
            {
                for (int count = 0; count < group.EventData.Count;)
                {
                    var id = group.Waves.GetRandom().ID;
                    // If this infinite loops, not my problem
                    if (!DataManager.TryGetWave(id, out var wave))
                    {
                        DinoLogger.Error($"Unable to find wave ID {id}");
                        continue;
                    }
                    _waves.Add(new(wave, group.EventData[count++]));
                }
            }
        }

        private void CleanupWaves()
        {
            _waves.Clear();
            _activeWaves.Clear();
            _nextWaveTime = 0f;
            _nextWaveDelayCache = 0f;
            _currentWave = -1;
        }
    }
}
