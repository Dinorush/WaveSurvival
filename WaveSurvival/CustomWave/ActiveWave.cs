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

            while (!TryDoSpawns())
                yield return null;

            while (EnemyCount != 0 || QueuedCount != 0)
                yield return null;
        }

        private bool TryDoSpawns()
        {
            while (CanDoSpawn())
            {
                _lastSubWaveTime = Clock.Time;

                if (WaveManager.Random.NextSingle() < CurrentSpawn.RandomDirectionChance)
                    WaveManager.Current.SetRandomSpawner(ref _spawner);

                if (CurrentSpawn.Count == 0)
                   DoUnweightedSpawns();
                else
                   DoWeightedSpawns();

                foreach (var we in CurrentSpawn.EventsOnSubWaveStart)
                    WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(we, GameData.eWardenObjectiveEventTrigger.None, true);
                IncrementSpawn();
            }

            return IsDone();
        }

        private bool CanDoSpawn()
        {
            if (IsDone()) return false;

            var spawn = CurrentSpawn;
            if (spawn.SubWaveMaxCount > 0 && spawn.SubWaveMaxCount < QueuedCount + EnemyCount)
                return false;
            if (spawn.SubWaveDelay > Clock.Time - _lastSubWaveTime)
                return false;
            return true;
        }

        private bool IsDone() => SpawnIndex >= Settings.Spawns.Count;

        private void DoUnweightedSpawns()
        {
            foreach (var enemy in CurrentSpawn.Enemies)
            {
                _spawner.AddSpawn(enemy.ID, CurrentSpawn.SpawnRate, this);
                QueuedCount++;
            }
        }

        private void DoWeightedSpawns()
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
                        _spawner.AddSpawn(enemy.ID, CurrentSpawn.SpawnRate, this);
                        QueuedCount++;
                        remainingCost -= enemy.Cost;
                        break;
                    }
                }
            }
        }

        private void IncrementSpawn()
        {
            SpawnIndex++;
        }
    }
}
