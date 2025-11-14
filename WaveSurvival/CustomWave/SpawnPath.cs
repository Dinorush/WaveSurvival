using LevelGeneration;
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

        public bool TryAdvancePath(LG_Zone zone, [MaybeNullWhen(false)] out EnemySpawner spawner, out EnemySpawner? oldSpawner)
        {
            // At the end of the path
            if (_pathIndex >= _path.Count - 1)
            {
                oldSpawner = null;
                spawner = null;
                return false;
            }

            // The newly opened zone doesn't match the next one
            if (_path[_pathIndex + 1].Node.m_zone.ID != zone.ID)
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

            spawner = _path[++_pathIndex];
            spawner.Valid = true;
            return true;
        }
    }
}
