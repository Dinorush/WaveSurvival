using GameData;
using WaveSurvival.Json.Converters;
using WaveSurvival.Utils;
using System.Text.Json.Serialization;
using WaveSurvival.Json;

namespace WaveSurvival.CustomWaveData.Wave
{
    [JsonConverter(typeof(SpawnDataConverter))]
    public class SpawnData
    {
        public static readonly Dictionary<string, SpawnData> Template = new()
        {
            {
                "Example", new SpawnData()
                {
                    Enemies = new(new List<WeightedEnemyData>())
                }
            },
            {
                "Example Two", new SpawnData()
                {
                    Enemies = new("Example")
                }
            }
        };

        public JsonReference<List<WeightedEnemyData>> Enemies { get; set; } = new();
        public int Count { get; set; } = 0;
        public float SpawnRate { get; set; } = 20f;
        public int SpawnInterval { get; set; } = 0;
        public float SpawnDelayOnInterval { get; set; } = 0f;
        public float RandomDirectionChanceOnInterval { get; set; } = 0f;
        public int SubWaveMaxCount { get; set; } = 0;
        public float SubWaveDelay { get; set; } = 0;
        public float RandomDirectionChance { get; set; } = 0;
        public List<WardenObjectiveEventData> EventsOnSubWaveStart { get; set; } = EmptyList<WardenObjectiveEventData>.Instance;
        public ScreamSize SubWaveScreamSize { get; set; } = ScreamSize.Small;
        public ScreamType SubWaveScreamType { get; set; } = ScreamType.None;
        public bool HideFromTotalCount { get; set; } = false;

        public void ResolveReferences()
        {
            if (!DataManager.Resolve(Enemies))
                Enemies.Value = new();
            else
                Enemies.Value!.Sort((x, y) =>
                {
                    if (x.Cost == y.Cost) return 0;
                    return x.Cost < y.Cost ? -1 : 1;
                });
        }
    }
}
