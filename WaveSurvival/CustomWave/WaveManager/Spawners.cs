using Il2CppInterop.Runtime.Attributes;
using LevelGeneration;
using System.Diagnostics.CodeAnalysis;

namespace WaveSurvival.CustomWave
{
    public sealed partial class WaveManager
    {
        private readonly List<SpawnPath> _spawnPaths = new();
        private readonly Dictionary<int, EnemySpawner> _activeSpawners = new();
        private readonly Dictionary<int, EnemySpawner> _validSpawners = new();
        private readonly List<EnemySpawner> _finishedSpawners = new();

        [HideFromIl2Cpp]
        public void SetRandomSpawner([NotNull] ref EnemySpawner? spawner)
        {
            if (spawner != null)
                spawner.Used = false;
            spawner = _validSpawners.Values.ToArray()[Random.Next(_validSpawners.Count)];
            spawner.Used = true;
        }

        private void UpdateSpawners()
        {
            foreach (var spawner in _activeSpawners.Values)
            {
                if (spawner.UpdateCheckDone())
                    _finishedSpawners.Add(spawner);
            }

            foreach (var spawner in _finishedSpawners)
            {
                _activeSpawners.Remove(spawner.ID);
            }
            _finishedSpawners.Clear();
        }

        private void SetupSpawners()
        {
            if (!Builder.CurrentFloor.GetDimension(ActiveObjective!.DimensionIndex, out var dimension))
            {
                DinoLogger.Error($"Unable to get dimension {ActiveObjective.DimensionIndex}, no spawners created!");
                return;
            }

            // Create one spawner for each course node that appears in any path
            Dictionary<int, EnemySpawner> spawners = new();
            foreach (var spawnPath in ActiveObjective!.SpawnPaths)
            {
                List<EnemySpawner> spawnersOnPath = new(spawnPath.Count);
                foreach (var data in spawnPath)
                {
                    var layer = dimension.GetLayer(data.Layer);
                    if (layer == null || !layer.m_zonesByLocalIndex.ContainsKey(data.ZoneIndex))
                    {
                        DinoLogger.Error($"Unable to get zone for index {data.ZoneIndex}, layer {data.Layer}!");
                    }
                    else
                    {
                        var zone = layer.m_zonesByLocalIndex[data.ZoneIndex];
                        int areaIndex = data.AreaIndex >= 0 ? data.AreaIndex : zone.m_areas.Count - 1;
                        if (areaIndex < 0 || areaIndex >= zone.m_areas.Count)
                        {
                            DinoLogger.Error($"Unable to get area index {areaIndex} for zone index {data.ZoneIndex}, layer {data.Layer} (only {zone.m_areas.Count} areas exist!)");
                            continue;
                        }
                        var node = zone.m_areas[areaIndex].m_courseNode;
                        if (!spawners.TryGetValue(node.NodeID, out var spawner))
                            spawners.Add(node.NodeID, spawner = new(node));
                        spawnersOnPath.Add(spawner);
                    }
                }
                SpawnPath path = new(spawnersOnPath);
                _spawnPaths.Add(path);
                if (path.TryAdvancePath(out var firstSpawner, out _))
                    AddSpawner(firstSpawner!);
            }
        }

        [HideFromIl2Cpp]
        private void AddSpawner(EnemySpawner spawner)
        {
            _activeSpawners.TryAdd(spawner.ID, spawner);
            _validSpawners.TryAdd(spawner.ID, spawner);
        }

        private void CleanupSpawners()
        {
            _spawnPaths.Clear();
            _activeSpawners.Clear();
            _validSpawners.Clear();
        }

        internal static void Internal_OnZoneTreeUpdate(bool opened)
        {
            if (!IsMaster || !IsActive) return;

            foreach (var path in Current._spawnPaths)
            {
                if (path.TryUpdatePath(opened, out var spawner, out var oldSpawner))
                {
                    if (spawner != null)
                        Current.AddSpawner(spawner);
                    if (oldSpawner != null && !oldSpawner.Valid)
                        Current._validSpawners.Remove(oldSpawner.ID);
                }
            }
        }
    }
}
