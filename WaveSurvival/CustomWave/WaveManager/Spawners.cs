using AIGraph;
using AmorLib.Utils;
using Il2CppInterop.Runtime.Attributes;
using LevelGeneration;
using System.Diagnostics.CodeAnalysis;
using WaveSurvival.CustomWaveData.WaveObjective;

namespace WaveSurvival.CustomWave
{
    public sealed partial class WaveManager
    {
        // Wraps nodes used for spawn paths to know if any paths are currently using it.
        class SpawnNode
        {
            public readonly AIG_CourseNode Node;
            private int _validStack;

            public void AddUser() => _validStack++;
            public bool RemoveUser() => --_validStack == 0;

            public SpawnNode(AIG_CourseNode node)
            {
                Node = node;
                _validStack = 0;
            }
        }

        // Wraps nodes during searching to keep additional info.
        struct SearchInfo
        {
            public AIG_CourseNode node;
            public int nodeDist;
            public int lateralDist;
        }

        private readonly List<SpawnPath> _spawnPaths = new();
        private readonly Dictionary<int, EnemySpawner> _activeSpawners = new();
        private readonly Dictionary<int, EnemySpawner> _allSpawners = new();
        private readonly List<EnemySpawner> _finishedSpawners = new();
        private readonly Dictionary<int, SpawnNode> _pathNodes = new();
        private readonly Queue<SearchInfo> _searchQueue = new();

        private const int LateralSearchCap = 2;

        [HideFromIl2Cpp]
        public void SetRandomSpawnNode([NotNull] ref AIG_CourseNode? node)
        {
            node = _pathNodes.Values.ToArray()[Random.Next(_pathNodes.Count)].Node;
        }

        [HideFromIl2Cpp]
        public void SetRandomSpawnNode([NotNull] ref AIG_CourseNode? node, List<(AIG_CourseNode, LG_Zone)> spawnLocations)
        {
            int validCount = 0;
            AIG_CourseNode[] validNodes = new AIG_CourseNode[spawnLocations.Count];
            foreach ((var option, var zone) in spawnLocations)
                if (ZoneGraphUtil.IsZoneReachable(zone))
                    validNodes[validCount++] = option;
            node = validNodes[Random.Next(validCount)];
        }

        [HideFromIl2Cpp]
        public void SetRandomSpawner(AIG_CourseNode node, [NotNull] ref EnemySpawner? spawner, int? targetNodeDistance = null)
        {
            if (spawner != null)
            {
                bool nodeMatch = spawner.ID == node.NodeID;
                if (targetNodeDistance == null && nodeMatch) return;
                int nodeDist = spawner.Node.m_playerCoverage.GetNodeDistanceToClosestPlayer();
                if (nodeDist == targetNodeDistance || (nodeMatch && nodeDist < targetNodeDistance)) return;

                spawner.Used = false;
            }

            spawner = GetSpawnerFromNode(node, targetNodeDistance);
            _activeSpawners.TryAdd(spawner.ID, spawner);
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
            if (!Builder.CurrentFloor.GetDimension(ActiveObjective!.DimensionIndex, out _))
            {
                DinoLogger.Error($"Unable to get dimension {ActiveObjective.DimensionIndex}, no spawners created!");
                return;
            }

            // Find each course node that appears in each path
            foreach (var spawnPath in ActiveObjective!.SpawnPaths)
            {
                int lastZoneID = -1;
                List<(AIG_CourseNode?, LG_Zone)> nodesOnPath = new(spawnPath.Count);
                foreach (var data in spawnPath)
                {
                    if (TryGetNode(data, out var node, out var zone) && lastZoneID <= zone.ID)
                    {
                        lastZoneID = zone.ID;
                        nodesOnPath.Add((node, zone));
                    }
                }
                SpawnPath path = new(nodesOnPath);
                _spawnPaths.Add(path);
                if (path.TryAdvancePath(out var firstNode, out _) && firstNode != null)
                    AddSpawnNode(firstNode);
            }
        }

        [HideFromIl2Cpp]
        public static bool TryGetNode(SpawnPathData data, out AIG_CourseNode? node, [MaybeNullWhen(false)] out LG_Zone zone)
        {
            if (!Builder.CurrentFloor.GetDimension(ActiveObjective!.DimensionIndex, out var dimension))
            {
                DinoLogger.Error($"Unable to get dimension {ActiveObjective.DimensionIndex}, no spawners created!");
                node = null;
                zone = null;
                return false;
            }

            var layer = dimension.GetLayer(data.Layer);
            if (layer == null || !layer.m_zonesByLocalIndex.ContainsKey(data.ZoneIndex))
            {
                DinoLogger.Error($"Unable to get zone for index {data.ZoneIndex}, layer {data.Layer}!");
                node = null;
                zone = null;
                return false;
            }

            zone = layer.m_zonesByLocalIndex[data.ZoneIndex];
            if (data.AreaIndex == SpawnPathData.AreaBreak)
            {
                node = null;
                return true;
            }

            int areaIndex = data.AreaIndex >= 0 ? data.AreaIndex : zone.m_areas.Count - 1;
            if (areaIndex < 0 || areaIndex >= zone.m_areas.Count)
            {
                DinoLogger.Error($"Unable to get area index {areaIndex} for zone index {data.ZoneIndex}, layer {data.Layer} (only {zone.m_areas.Count} areas exist!)");
                node = null;
                zone = null;
                return false;
            }
            node = zone.m_areas[areaIndex].m_courseNode;
            return true;
        }

        [HideFromIl2Cpp]
        public EnemySpawner GetSpawnerFromNode(AIG_CourseNode node, int? targetNodeDistance = null)
        {
            int bestDist = 0;
            if (targetNodeDistance != null)
                bestDist = node.m_playerCoverage.GetNodeDistanceToClosestPlayer();

            if (targetNodeDistance != null && bestDist > targetNodeDistance)
            {
                List<AIG_CourseNode> bestNodes = new() { node };
                AIG_SearchID.IncrementSearchID();
                var searchID = AIG_SearchID.SearchID;
                node.m_searchID = searchID;
                _searchQueue.Enqueue(new() { node = node, nodeDist = bestDist, lateralDist = LateralSearchCap });
                while (_searchQueue.TryDequeue(out var info))
                {
                    foreach (var portal in info.node.m_portals)
                    {
                        if (portal.m_searchID == searchID || portal.IsProgressionLocked) continue;
                        portal.m_searchID = searchID;

                        SearchInfo newInfo = new()
                        {
                            node = portal.GetOppositeNode(info.node)
                        };

                        if (newInfo.node.m_searchID == searchID) continue;
                        newInfo.node.m_searchID = searchID;

                        if (!newInfo.node.IsValid) continue;

                        newInfo.nodeDist = newInfo.node.m_playerCoverage.GetNodeDistanceToClosestPlayer();
                        if (newInfo.nodeDist > info.nodeDist || newInfo.nodeDist < targetNodeDistance) continue;

                        if (newInfo.nodeDist == info.nodeDist)
                        {
                            if (info.lateralDist < LateralSearchCap)
                                newInfo.lateralDist = info.lateralDist + 1;
                            else
                                continue;
                        }
                        else
                            newInfo.lateralDist = 0;

                        if (newInfo.nodeDist < bestDist)
                        {
                            bestNodes.Clear();
                            bestDist = newInfo.nodeDist;
                            bestNodes.Add(newInfo.node);
                        }
                        else if (newInfo.nodeDist == bestDist)
                            bestNodes.Add(newInfo.node);

                        _searchQueue.Enqueue(newInfo);
                    }
                }
                _searchQueue.Clear();
                node = bestNodes.Count > 1 ? bestNodes[Random.Next(bestNodes.Count)] : bestNodes[0];
            }

            if (!_allSpawners.TryGetValue(node.NodeID, out var spawner))
                _allSpawners.Add(node.NodeID, spawner = new(node));
            return spawner;
        }

        [HideFromIl2Cpp]
        private void AddSpawnNode(AIG_CourseNode node)
        {
            if (!_pathNodes.TryGetValue(node.NodeID, out var spawnNode))
                _pathNodes.Add(node.NodeID, spawnNode = new(node));
            spawnNode.AddUser();
        }

        [HideFromIl2Cpp]
        private void RemoveSpawnNode(AIG_CourseNode node)
        {
            var spawnNode = _pathNodes[node.NodeID];
            if (spawnNode.RemoveUser())
                _pathNodes.Remove(node.NodeID);
        }

        private void CheckpointStoreSpawners()
        {
            foreach (var path in _spawnPaths)
                path.StoreCheckpoint();
        }

        private void CheckpointReloadSpawners()
        {
            foreach (var path in _spawnPaths)
                if (path.OnCheckpointReload(out var node) && node != null)
                    AddSpawnNode(node);

            foreach (var spawner in _allSpawners.Values)
                spawner.OnCheckpointReload();
        }

        private void CleanupSpawners(bool isCheckpoint)
        {
            if (!isCheckpoint)
            {
                _spawnPaths.Clear();
                _allSpawners.Clear();
            }
            _activeSpawners.Clear();
            _pathNodes.Clear();
        }

        private void OnReachableUpdate()
        {
            if (!IsMaster || !IsActive) return;

            System.Text.StringBuilder sb = new("Paths updated: [");
            foreach (var path in _spawnPaths)
            {
                if (path.TryUpdatePath(out var node, out var oldNode))
                {
                    sb.Append($"({oldNode?.m_zone.NavInfo.ToString()}{oldNode?.m_area.m_navInfo.ToString()} -> {node?.m_zone.NavInfo.ToString()}{node?.m_area.m_navInfo.ToString()}),");
                    if (node != null)
                        AddSpawnNode(node);
                    if (oldNode != null)
                        RemoveSpawnNode(oldNode);
                }
            }
            sb.Append(']');
            DinoLogger.Log(sb.ToString());
        }
    }
}
