using System.Diagnostics.CodeAnalysis;

namespace WaveSurvival.CustomWave
{
    public sealed class SpawnPath
    {
        private readonly List<EnemySpawner> _path;
        private int _pathIndex;

        public SpawnPath(List<EnemySpawner> pathList)
        {
            _path = pathList;
            _pathIndex = -1;
        }

        public bool TryUpdatePath(bool opened, out EnemySpawner? spawner, out EnemySpawner? oldSpawner)
        {
            return opened ? TryAdvancePath(out spawner, out oldSpawner) : TryRevertPath(out spawner, out oldSpawner);
        }

        public bool TryRevertPath(out EnemySpawner? spawner, [MaybeNullWhen(false)] out EnemySpawner oldSpawner)
        {
            if (_pathIndex == -1 || _path[_pathIndex].ZoneNode.IsReachable)
            {
                spawner = null;
                oldSpawner = null;
                return false;
            }

            int newIndex = _pathIndex;
            while (newIndex - 1 >= 0 && !_path[newIndex - 1].ZoneNode.IsReachable)
                --newIndex;

            oldSpawner = _path[_pathIndex];
            oldSpawner.Valid = false;

            _pathIndex = newIndex;
            if (_pathIndex >= 0)
            {
                spawner = _path[newIndex];
                spawner.Valid = true;
            }
            else
                spawner = null;
            return true;
        }

        public bool TryAdvancePath([MaybeNullWhen(false)] out EnemySpawner spawner, out EnemySpawner? oldSpawner)
        {
            int newIndex = _pathIndex;
            while (newIndex + 1 < _path.Count && _path[newIndex + 1].ZoneNode.IsReachable)
                ++newIndex;

            if (newIndex == _pathIndex)
            {
                oldSpawner = null;
                spawner = null;
                return false;
            }

            if (_pathIndex >= 0)
            {
                oldSpawner = _path[_pathIndex];
                oldSpawner.Valid = false;
            }
            else
                oldSpawner = null;

            _pathIndex = newIndex;
            spawner = _path[newIndex];
            spawner.Valid = true;
            return true;
        }
    }
}
