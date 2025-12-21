using WaveSurvival.CustomWaveData.Wave;
using WaveSurvival.Utils.Extensions;
using System.Collections;
using WaveSurvival.CustomWaveData.WaveObjective;
using System.Diagnostics.CodeAnalysis;

namespace WaveSurvival.CustomWave
{
    public sealed class ActiveWave
    {
        public int EnemyCount { get; private set; }
        public int QueuedCount { get; private set; }
        public readonly WaveData Settings;
        public readonly WaveEventData EventData;

        private readonly IEnumerator _update;
        private readonly Queue<SpawnSet> _spawnSetQueue;
        private SpawnSet? _currentSpawn;
        private float _lastSubWaveTime;
        private float _nextIntervalTime;
        private int _intervalCount;
        private EnemySpawner _spawner;

        public ActiveWave(WaveData settings, WaveEventData eventData)
        {
            Settings = settings;
            EventData = eventData;
            _update = SpawnWave();
            _spawnSetQueue = new();
            SetupSpawns();

            WaveManager.Current.SetRandomSpawner(ref _spawner);
            WaveNetwork.DoWaveScream(Settings.ScreamSize, Settings.ScreamType, _spawner.Node.Position);
        }

        private void SetupSpawns()
        {
            int total = 0;
            foreach (var spawnRef in Settings.Spawns)
            {
                SpawnData spawn = spawnRef;
                var set = new SpawnSet(spawn);
                _spawnSetQueue.Enqueue(set);
                if (!spawn.HideFromTotalCount)
                    total += set.RemainingEnemies;
            }

            if (!_spawnSetQueue.TryDequeue(out _currentSpawn))
                _currentSpawn = null;

            WaveManager.Current.AddWaveEnemyCount(total);
        }

        public bool UpdateCheckDone()
        {
            if (_update.MoveNext())
                return false;
            return true;
        }

        public void OnEnemySpawned(bool hideFromCount)
        {
            EnemyCount++;
            QueuedCount--;
            WaveManager.Current.OnEnemySpawned(hideFromCount);
        }

        public void OnEnemyDead()
        {
            EnemyCount--;
            WaveManager.Current.OnEnemyDead();
        }

        private IEnumerator SpawnWave()
        {
            _lastSubWaveTime = Clock.Time;

            IEnumerator spawns = DoSpawns();
            while (spawns.MoveNext())
                yield return null;

            while (EnemyCount != 0 || QueuedCount != 0)
                yield return null;
        }

        private IEnumerator DoSpawns()
        {
            while (!IsDone)
            {
                while (!CanDoSpawn())
                    yield return null;

                _lastSubWaveTime = Clock.Time;

                var data = _currentSpawn.Settings;
                if (WaveManager.Random.NextSingle() < data.RandomDirectionChance)
                    WaveManager.Current.SetRandomSpawner(ref _spawner);

                foreach (var we in data.EventsOnSubWaveStart)
                    WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(we, GameData.eWardenObjectiveEventTrigger.None, true);
                WaveNetwork.DoWaveScream(data.SubWaveScreamSize, data.SubWaveScreamType, _spawner.Node.Position);

                while (TryAddSpawns())
                    yield return null;

                IncrementSpawn();
            }
        }

        private bool CanDoSpawn()
        {
            if (IsDone) return false;

            var spawn = _currentSpawn.Settings;
            if (spawn.SubWaveMaxCount > 0 && spawn.SubWaveMaxCount <= QueuedCount + EnemyCount)
                return false;
            if (spawn.SubWaveDelay > Clock.Time - _lastSubWaveTime)
                return false;
            return true;
        }

        [MemberNotNullWhen(false, nameof(_currentSpawn))]
        private bool IsDone => _currentSpawn == null;

        private void IncrementSpawn()
        {
            if (!_spawnSetQueue.TryDequeue(out _currentSpawn))
                _currentSpawn = null;
            _intervalCount = 0;
            _nextIntervalTime = 0;
        }

        private bool TryAddSpawns()
        {
            if (_currentSpawn!.IsDone) return false;

            var time = Clock.Time;
            if (time < _nextIntervalTime) return true;

            var data = _currentSpawn.Settings;
            (uint id, int cost) = _currentSpawn.Dequeue();
            _spawner.AddSpawn(id, data.SpawnRate, data.HideFromTotalCount, this);
            QueuedCount++;
            _intervalCount += cost;

            if (_intervalCount >= data.SpawnInterval)
            {
                _nextIntervalTime = Clock.Time + data.SpawnDelayOnInterval;
                _intervalCount -= data.SpawnInterval;
                if (WaveManager.Random.NextSingle() < data.RandomDirectionChanceOnInterval)
                    WaveManager.Current.SetRandomSpawner(ref _spawner);
            }
            return !_currentSpawn.IsDone;
        }

        class SpawnSet
        {
            protected Queue<(uint id, int cost)> _spawnQueue = new();
            public readonly SpawnData Settings;

            public SpawnSet(SpawnData data)
            {
                Settings = data;
                if (Settings.Count > 0)
                    SetupWeightedSpawns();
                else
                    SetupUnweightedSpawns();
            }

            public int RemainingEnemies => _spawnQueue.Count;
            public bool IsDone => _spawnQueue.Count == 0;

            public (uint id, int cost) Dequeue() => _spawnQueue.Dequeue();

            private void SetupUnweightedSpawns()
            {
                List<WeightedEnemyData> enemies = Settings.Enemies;
                foreach (var enemy in enemies)
                    _spawnQueue.Enqueue((enemy.ID, 1));
            }

            private void SetupWeightedSpawns()
            {
                List<WeightedEnemyData> enemies = Settings.Enemies;
                float totalSpawnWeight = enemies.Sum(data => data.Weight);

                for (int remainingCost = Settings.Count; remainingCost > 0;)
                {
                    float random = WaveManager.Random.NextSingle(totalSpawnWeight);
                    float runningWeight = 0;
                    foreach (var enemy in enemies)
                    {
                        // In sorted order - can't spawn anything from this enemy onward
                        if (enemy.Cost > remainingCost)
                        {
                            if (totalSpawnWeight != runningWeight)
                                totalSpawnWeight = runningWeight;
                            else
                            {
                                DinoLogger.Warning($"Wave spawn unable to use {remainingCost} remaining count (total count: {Settings.Count}, costs: [{string.Join(",", enemies.ConvertAll(e => e.Cost))}])!");
                                remainingCost = 0; // No more valid spawns exist!
                            }
                            break;
                        }

                        runningWeight += enemy.Weight;
                        if (random < runningWeight && remainingCost >= enemy.Cost)
                        {
                            _spawnQueue.Enqueue((enemy.ID, enemy.Cost));
                            remainingCost -= enemy.Cost;
                            break;
                        }
                    }
                }
            }
        }
    }
}
