using WaveSurvival.Json.Converters;
using WaveSurvival.Utils;
using System.Text.Json.Serialization;

namespace WaveSurvival.CustomWaveData.Wave
{
    [JsonConverter(typeof(WeightedEnemyConverter))]
    public class WeightedEnemyData : IWeightable
    {
        public uint ID { get; set; } = 0u;
        public float Weight { get; set; } = 1f;
        public int Cost { get; set; } = 1;
    }
}
