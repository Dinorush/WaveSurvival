using System.Text.Json.Serialization;
using WaveSurvival.Json.Converters;
using WaveSurvival.Utils;

namespace WaveSurvival.CustomWaveData.WaveObjective
{
    [JsonConverter(typeof(WeightedWaveConverter))]
    public sealed class WeightedWaveData : IWeightable
    {
        public string ID { get; set; } = string.Empty;
        public float Weight { get; set; } = 1f;
    }
}
