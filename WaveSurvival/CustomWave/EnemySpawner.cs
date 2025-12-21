using AIGraph;
using Enemies;
using WaveSurvival.Utils.Extensions;

namespace WaveSurvival.CustomWave
{
    public sealed class EnemySpawner
    {
        private static float s_lastSpawnTime = 0f;

        public readonly AIG_CourseNode Node;
        public readonly ZoneNode ZoneNode;
        public readonly int ID;
        private readonly Placement[] _spawnPositions;
        private readonly Queue<(uint id, float delay, bool isHidden, ActiveWave wave)> _queuedSpawns = new();
        private int _useCount = 0;
        private int _validCount = 0;
        private int _spawnIndex;

        public EnemySpawner(AIG_CourseNode node)
        {
            Node = node;
            ZoneNode = ZoneTree.GetZoneNode(node.m_zone);
            ID = node.NodeID;
            var cluster = node.m_nodeCluster;
            _spawnPositions = new Placement[cluster.m_scoredPlacements.Count];
            for (int i = 0; i < _spawnPositions.Length; i++)
                _spawnPositions[i] = new(cluster.m_scoredPlacements[i].item.Position, EnemyGroup.GetRandomRotation());

            WaveManager.Random.Shuffle(_spawnPositions);
        }

        // Tracks whether any spawn paths are using this spawner - removed from valid pool in WaveManager when invalid
        public bool Valid
        {
            get => _validCount > 0;
            set => _validCount += value ? 1 : -1;
        }

        // Tracks whether any active waves are using this spawner
        public bool Used
        {
            get => _useCount > 0;
            set => _useCount += value ? 1 : -1;
        }

        public bool UpdateCheckDone()
        {
            float time = Clock.Time;
            if (_queuedSpawns.TryPeek(out var queuedSpawn) && time >= queuedSpawn.delay + s_lastSpawnTime)
            {
                _queuedSpawns.Dequeue();
                SpawnEnemy(queuedSpawn.id, queuedSpawn.isHidden, queuedSpawn.wave);
                s_lastSpawnTime = time;
            }

            return _queuedSpawns.Count == 0 && !Valid && !Used;
        }

        public void AddSpawn(uint id, float spawnRate, bool hideFromCount, ActiveWave wave)
        {
            _queuedSpawns.Enqueue((id, 1f / spawnRate, hideFromCount, wave));
        }

        private void SpawnEnemy(uint id, bool hideFromCount, ActiveWave wave)
        {
            var placement = _spawnPositions[_spawnIndex++];
            if (_spawnIndex >= _spawnPositions.Length)
            {
                WaveManager.Random.Shuffle(_spawnPositions);
                _spawnIndex = 0;
            }

            var agent = EnemyAllocator.Current.SpawnEnemy(id, Node, Agents.AgentMode.Agressive, placement.position, placement.rotation);
            wave.OnEnemySpawned(hideFromCount);
            agent.AddOnDeadOnce(wave.OnEnemyDead);
        }
    }
}
