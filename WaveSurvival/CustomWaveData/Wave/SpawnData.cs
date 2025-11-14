using GameData;
using WaveSurvival.Json.Converters;
using WaveSurvival.Utils;
using System.Text.Json.Serialization;

namespace WaveSurvival.CustomWaveData.Wave
{
    [JsonConverter(typeof(SpawnDataConverter))]
    public class SpawnData
    {
        private List<WeightedEnemyData> _enemies = EmptyList<WeightedEnemyData>.Instance;
        public List<WeightedEnemyData> Enemies
        {
            get => _enemies;
            set
            {
                _enemies = value;
                _enemies.Sort((x, y) => 
                { 
                    if (x.Cost == y.Cost) return 0;
                    return x.Cost < y.Cost ? -1 : 1;
                });
            }
        }
        public int Count { get; set; } = 0;
        public float SpawnRate { get; set; } = 20f;
        public int SubWaveMaxCount { get; set; } = 0;
        public float SubWaveDelay { get; set; } = 0;
        public float RandomDirectionChance { get; set; } = 0;
        public List<WardenObjectiveEventData> EventsOnSubWaveStart { get; set; } = EmptyList<WardenObjectiveEventData>.Instance;
    }
}
