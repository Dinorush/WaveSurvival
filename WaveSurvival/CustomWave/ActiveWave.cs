using WaveSurvival.CustomWaveData.Wave;
using WaveSurvival.Utils.Extensions;
using System.Collections;
using WaveSurvival.CustomWaveData.WaveObjective;

namespace WaveSurvival.CustomWave
{
    public sealed class ActiveWave
    {
        public int EnemyCount { get; private set; }
        public int QueuedCount { get; private set; }
        public int SpawnIndex { get; private set; }
        public SpawnData CurrentSpawn => Settings.Spawns[SpawnIndex];
        public readonly WaveData Settings;
        public readonly WaveEventData EventData;

        private readonly IEnumerator _update;
        private float _lastSubWaveTime;
        private float _nextIntervalTime;
        private int _intervalCount;
        private EnemySpawner _spawner;

        public ActiveWave(WaveData settings, WaveEventData eventData)
        {
            Settings = settings;
            EventData = eventData;
            _update = SpawnWave();
            WaveManager.Current.SetRandomSpawner(ref _spawner);
            WaveNetwork.DoWaveScream(Settings.ScreamSize, Settings.ScreamType, _spawner.Node.Position);
        }

        public bool UpdateCheckDone()
        {
            if (_update.MoveNext())
                return false;
            return true;
        }

        public void OnEnemySpawned()
        {
            EnemyCount++;
            QueuedCount--;
            WaveManager.Current.OnEnemySpawned();
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
            while (!IsDone())
            {
                while (!CanDoSpawn())
                    yield return null;

                _lastSubWaveTime = Clock.Time;

                if (WaveManager.Random.NextSingle() < CurrentSpawn.RandomDirectionChance)
                    WaveManager.Current.SetRandomSpawner(ref _spawner);

                IEnumerator waveSpawns = CurrentSpawn.Count == 0 ? DoUnweightedSpawns() : DoWeightedSpawns();
                while (waveSpawns.MoveNext())
                    yield return null;

                foreach (var we in CurrentSpawn.EventsOnSubWaveStart)
                    WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(we, GameData.eWardenObjectiveEventTrigger.None, true);
                WaveNetwork.DoWaveScream(CurrentSpawn.SubWaveScreamSize, CurrentSpawn.SubWaveScreamType, _spawner.Node.Position);
                IncrementSpawn();
            }
        }

        private bool CanDoSpawn()
        {
            if (IsDone()) return false;

            var spawn = CurrentSpawn;
            if (spawn.SubWaveMaxCount > 0 && spawn.SubWaveMaxCount <= QueuedCount + EnemyCount)
                return false;
            if (spawn.SubWaveDelay > Clock.Time - _lastSubWaveTime)
                return false;
            return true;
        }

        private bool IsDone() => SpawnIndex >= Settings.Spawns.Count;

        private IEnumerator DoUnweightedSpawns()
        {
            List<WeightedEnemyData> enemies = CurrentSpawn.Enemies;
            foreach (var enemy in enemies)
            {
                while (!TryAddSpawn(enemy.ID, 1))
                    yield return null;
            }
        }

        private IEnumerator DoWeightedSpawns()
        {
            List<WeightedEnemyData> enemies = CurrentSpawn.Enemies;
            float totalSpawnWeight = enemies.Sum(data => data.Weight);

            for (int remainingCost = CurrentSpawn.Count; remainingCost > 0;)
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
                            DinoLogger.Warning($"Wave {SpawnIndex + 1} had {remainingCost} it was unable to spawn!");
                            remainingCost = 0; // No more valid spawns exist!
                        }
                        break;
                    }

                    runningWeight += enemy.Weight;
                    if (random < runningWeight && remainingCost >= enemy.Cost)
                    {
                        while (!TryAddSpawn(enemy.ID, enemy.Cost))
                            yield return null;
                        remainingCost -= enemy.Cost;
                        break;
                    }
                }
            }
        }

        private void IncrementSpawn()
        {
            SpawnIndex++;
            _intervalCount = 0;
            _nextIntervalTime = 0;
        }

        private bool TryAddSpawn(uint id, int cost)
        {
            var time = Clock.Time;
            if (time < _nextIntervalTime)
                return false;

            _spawner.AddSpawn(id, CurrentSpawn.SpawnRate, this);
            QueuedCount++;
            _intervalCount += cost;

            if (_intervalCount >= CurrentSpawn.SpawnInterval)
            {
                _nextIntervalTime = time + CurrentSpawn.SpawnDelayOnInterval;
                _intervalCount -= CurrentSpawn.SpawnInterval;
                if (WaveManager.Random.NextSingle() < CurrentSpawn.RandomDirectionChanceOnInterval)
                    WaveManager.Current.SetRandomSpawner(ref _spawner);
            }
            return true;
        }
    }
}
