using System.Text.Json.Serialization;
using WaveSurvival.CustomWaveData.Wave;
using WaveSurvival.Json;
using WaveSurvival.Json.Converters;
using WaveSurvival.Utils;

namespace WaveSurvival.CustomWaveData.WaveObjective
{
    [JsonConverter(typeof(WeightedWaveConverter))]
    public sealed class WeightedWaveReference : JsonReference<WaveData>, IWeightable
    {
        public float Weight { get; set; } = 1f;

        public WeightedWaveReference() : base() { }
        public WeightedWaveReference(WaveData data) : base(data) { }
        public WeightedWaveReference(string id) : base(id) { }
    }
}
