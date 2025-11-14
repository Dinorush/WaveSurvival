using Il2CppInterop.Runtime.Attributes;
using LevelGeneration;
using SNetwork;
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
            // Create one spawner for each course node that appears in any path
            Dictionary<int, EnemySpawner> spawners = new();
            foreach (var spawnPath in ActiveObjective!.SpawnPaths)
            {
                List<EnemySpawner> spawnersOnPath = new(spawnPath.Count);
                foreach (var data in spawnPath)
                {
                    if (!Builder.CurrentFloor.TryGetZoneByLocalIndex(eDimensionIndex.Reality, LG_LayerType.MainLayer, data.ZoneIndex, out var zone))
                    {
                        DinoLogger.Error($"Unable to get zone for index {data.ZoneIndex}!");
                    }
                    else
                    {
                        int areaIndex = data.AreaIndex >= 0 ? data.AreaIndex : zone.m_areas.Count - 1;
                        if (areaIndex < 0 || areaIndex >= zone.m_areas.Count)
                        {
                            DinoLogger.Error($"Unable to get area {areaIndex} for zone {data.ZoneIndex} (only {zone.m_areas.Count} areas exist!)");
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
                if (path.TryAdvancePath(Builder.CurrentFloor.allZones[0], out var zeroSpawner, out _))
                    AddSpawner(zeroSpawner);
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

        internal static void Internal_OnDoorOpened(LG_SecurityDoor door)
        {
            if (!IsMaster || !IsActive) return;

            var zone = door.Gate.m_linksTo.m_zone;
            foreach (var path in Current._spawnPaths)
            {
                if (path.TryAdvancePath(zone, out var spawner, out var oldSpawner))
                {
                    Current.AddSpawner(spawner);
                    if (oldSpawner != null && !oldSpawner.Valid)
                        Current._validSpawners.Remove(oldSpawner.ID);
                }
            }
        }
    }
}
