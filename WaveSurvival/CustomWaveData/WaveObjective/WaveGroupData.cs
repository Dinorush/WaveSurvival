using System.Text.Json.Serialization;
using WaveSurvival.Json.Converters;
using WaveSurvival.Utils;

namespace WaveSurvival.CustomWaveData.WaveObjective
{
    [JsonConverter(typeof(WaveGroupConverter))]
    public sealed class WaveGroupData
    {
        public static readonly List<WaveGroupData> Template = new()
        {
            new()
            {
                Waves = new() { new() { ID = "Example" } },
                EventData = new() { new() }
            },
            new()
            {
                Waves = new()
                {
                    new() { ID = "Example" },
                    new() { ID = "Example Two", Weight = 0.5f }
                },
                EventData = new() { new() }
            }
        };

        public WeightedList<WeightedWaveData> Waves { get; set; } = WeightedList<WeightedWaveData>.Empty;
        public List<WaveEventData> EventData { get; set; } = EmptyList<WaveEventData>.Instance;
    }
}