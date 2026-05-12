using WaveSurvival.CustomWaveData.Wave;
using WaveSurvival.Utils.Extensions;
using System.Collections;
using WaveSurvival.CustomWaveData.WaveObjective;
using System.Diagnostics.CodeAnalysis;
using AIGraph;
using LevelGeneration;

namespace WaveSurvival.CustomWave
{
    public sealed class ActiveWave
    {
        struct SpawnSettings
        {
            public List<(AIG_CourseNode node, LG_Zone zone)>? spawnLocations;
            public int? spawnDistance;

            public SpawnSettings(WaveEventData data)
            {
                spawnLocations = GetSpawnNodes(data.SpawnLocations);
                spawnDistance = data.SpawnDistance ?? WaveManager.ActiveObjective!.SpawnDistance;
            }

            public SpawnSettings(SpawnSettings baseSettings, SpawnData spawnData)
            {
                spawnLocations = spawnData.SpawnLocations != null ? GetSpawnNodes(spawnData.SpawnLocations) : baseSettings.spawnLocations;
                spawnDistance = spawnData.SpawnDistance ?? baseSettings.spawnDistance;
            }
        }

        public int EnemyCount { get; private set; }
        public int QueuedCount { get; private set; }
        public readonly WaveData Settings;
        public readonly WaveEventData EventData;

        private readonly IEnumerator _update;
        private readonly Queue<SpawnSet> _spawnSetQueue;
        private SpawnSet? _currentSpawn;
        private SpawnSettings _currentSpawnSettings;
        private readonly SpawnSettings _baseSpawnSettings;
        private float _lastSubWaveTime;
        private float _nextIntervalTime;
        private int _intervalCount;
        private AIG_CourseNode _spawnNode;
        private EnemySpawner _spawner;

        public ActiveWave(WaveData settings, WaveEventData eventData)
        {
            Settings = settings;
            EventData = eventData;
            _baseSpawnSettings = new(EventData);

            _update = SpawnWave();
            _spawnSetQueue = new();
            SetupSpawns();

            _currentSpawnSettings = _baseSpawnSettings;
            SetRandomSpawner();
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

        [MemberNotNull(nameof(_spawnNode), nameof(_spawner))]
        private void SetRandomSpawner()
        {
            if (_currentSpawnSettings.spawnLocations != null)
                WaveManager.Current.SetRandomSpawnNode(ref _spawnNode, _currentSpawnSettings.spawnLocations);
            else
                WaveManager.Current.SetRandomSpawnNode(ref _spawnNode);

            UpdateSpawner();
        }

        [MemberNotNull(nameof(_spawner))]
        private void UpdateSpawner()
        {
            WaveManager.Current.SetRandomSpawner(_spawnNode, ref _spawner, _currentSpawnSettings.spawnDistance);
        }

        public static List<(AIG_CourseNode, LG_Zone)>? GetSpawnNodes(List<SpawnPathData>? paths)
        {
            if (paths == null || paths.Count == 0) return null;

            List<(AIG_CourseNode, LG_Zone)> nodes = new(paths.Count);
            foreach (var path in paths)
                if (WaveManager.TryGetNode(path, out var node, out var zoneNode) && node != null)
                    nodes.Add((node, zoneNode));
            return nodes;
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
                if (ShouldForceRandomSpawner(data) || WaveManager.Random.NextSingle() < data.RandomDirectionChance)
                    SetRandomSpawner();
                else
                    UpdateSpawner();

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

        private bool ShouldForceRandomSpawner(SpawnData spawnData)
        {
            var lastPath = _currentSpawnSettings.spawnLocations;
            _currentSpawnSettings = new(_baseSpawnSettings, spawnData);
            var newPath = _currentSpawnSettings.spawnLocations;

            if (lastPath == newPath) return false;
            if (lastPath == null || newPath == null) return true;
            if (lastPath.Count != newPath.Count) return true;

            for (int i = 0; i < newPath.Count; i++)
                if (lastPath[i].node.NodeID != newPath[i].node.NodeID)
                    return true;
            return false;
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

            if (data.SpawnInterval > 0 && _intervalCount >= data.SpawnInterval)
            {
                _nextIntervalTime = Clock.Time + data.SpawnDelayOnInterval;
                _intervalCount -= data.SpawnInterval;
                if (WaveManager.Random.NextSingle() < data.RandomDirectionChanceOnInterval)
                    SetRandomSpawner();
                else
                    UpdateSpawner();
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
