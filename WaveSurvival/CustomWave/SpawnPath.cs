using AIGraph;
using AmorLib.Utils;
using LevelGeneration;

namespace WaveSurvival.CustomWave
{
    public sealed class SpawnPath
    {
        private readonly List<(AIG_CourseNode? node, LG_Zone zone)> _path;
        private int _pathIndex;
        private int _checkpointIndex;

        public SpawnPath(List<(AIG_CourseNode? node, LG_Zone zone)> pathList)
        {
            _path = pathList;
            _pathIndex = -1;
        }

        public bool TryUpdatePath(out AIG_CourseNode? spawner, out AIG_CourseNode? oldSpawner)
        {
            return _pathIndex == -1 || ZoneGraphUtil.IsZoneReachable(_path[_pathIndex].zone) ? TryAdvancePath(out spawner, out oldSpawner) : TryRevertPath(out spawner, out oldSpawner);
        }

        public bool TryRevertPath(out AIG_CourseNode? node, out AIG_CourseNode? oldNode)
        {
            if (_pathIndex == -1 || ZoneGraphUtil.IsZoneReachable(_path[_pathIndex].zone))
            {
                node = null;
                oldNode = null;
                return false;
            }

            int newIndex = _pathIndex;
            while (newIndex - 1 >= 0 && !ZoneGraphUtil.IsZoneReachable(_path[newIndex - 1].zone))
                --newIndex;

            oldNode = _path[_pathIndex].node;
            _pathIndex = newIndex;
            if (_pathIndex >= 0)
                node = _path[newIndex].node;
            else
                node = null;
            return true;
        }

        public bool TryAdvancePath(out AIG_CourseNode? node, out AIG_CourseNode? oldNode)
        {
            int newIndex = _pathIndex;
            while (newIndex + 1 < _path.Count && ZoneGraphUtil.IsZoneReachable(_path[newIndex + 1].zone))
                ++newIndex;

            if (newIndex == _pathIndex)
            {
                oldNode = null;
                node = null;
                return false;
            }

            if (_pathIndex >= 0)
                oldNode = _path[_pathIndex].node;
            else
                oldNode = null;

            _pathIndex = newIndex;
            node = _path[newIndex].node;
            return true;
        }

        public void StoreCheckpoint()
        {
            _checkpointIndex = _pathIndex;
        }

        public bool OnCheckpointReload(out AIG_CourseNode? node)
        {
            _pathIndex = _checkpointIndex;
            if (_pathIndex < 0)
            {
                node = null;
                return false;
            }
            node = _path[_pathIndex].node;
            return true;
        }
    }
}
