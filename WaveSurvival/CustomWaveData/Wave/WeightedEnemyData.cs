using System.Text.Json.Serialization;
using WaveSurvival.Json.Converters;
using WaveSurvival.Utils;

namespace WaveSurvival.CustomWaveData.Wave
{
    [JsonConverter(typeof(WeightedEnemyConverter))]
    public class WeightedEnemyData : IWeightable
    {
        public static readonly Dictionary<string, List<WeightedEnemyData>> Template = new()
        {
            {
                "Example", new List<WeightedEnemyData>()
                {
                    new(),
                    new() { Weight = 0.5f },
                    new() { Weight = 0.2f, Cost = 2 }
                }
            }
        };

        public uint ID { get; set; } = 0u;
        public float Weight { get; set; } = 1f;
        public int Cost { get; set; } = 1;
    }
}
