namespace WaveSurvival.CustomWave
{
    public sealed partial class WaveManager
    {
        private int _enemyCount = 0;

        public void AddWaveEnemyCount(int count)
        {
            WaveNetwork.SetEnemyCount(_enemyCount += count);
        }

        public void OnEnemySpawned(bool hideFromCount)
        {
            if (hideFromCount)
                WaveNetwork.SetEnemyCount(++_enemyCount);
        }

        public void OnEnemyDead()
        {
            WaveNetwork.SetEnemyCount(--_enemyCount);
        }

        private void CleanupEnemyCount()
        {
            _enemyCount = 0;
        }
    }
}
