using System.Text.Json.Serialization;
using WaveSurvival.CustomWaveData.Wave;
using WaveSurvival.Json;
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
                Waves = new(new List<WeightedWaveReference>()
                {
                    new(WaveData.Template.Values.First())
                }),
                Events = new() { new(new WaveEventData()) }
            },
            new()
            {
                Waves = new()
                {
                    new() { ID = "Example" },
                    new() { ID = "Example Two", Weight = 0.5f }
                },
                Events = new() { new(new WaveEventData()) }
            },
            new()
            {
                Waves = new()
                {
                    new() { ID = "Example" },
                    new() { ID = "Example Two" }
                },
                Events = new() { new("Example"), new("Example") }
            }
        };

        public WeightedList<WeightedWaveReference> Waves { get; set; } = WeightedList<WeightedWaveReference>.Empty;
        public List<JsonReference<WaveEventData>> Events { get; set; } = EmptyList<JsonReference<WaveEventData>>.Instance;

        public bool ResolveReferences()
        {
            Waves.RemoveAll(data => !DataManager.Resolve(data));
            foreach (var wave in Waves)
                wave.Value!.ResolveReferences();

            foreach (var eventData in Events)
                if (!DataManager.Resolve(eventData))
                    eventData.Value = new();
            return true;
        }

        public WaveData GetWaveData() => Waves.GetRandom();
        public void RefillWaves() => Waves.Refill();
    }
}