using GameData;
using System.Text.Json.Serialization;
using WaveSurvival.Utils;

namespace WaveSurvival.CustomWaveData.WaveObjective
{
    public sealed class WaveObjectiveData
    {
        public static readonly List<WaveObjectiveData> Template = new()
        {
            new()
            {
                SpawnPaths = new(SpawnPathData.Template),
                WaveSequence = new(WaveGroupData.Template),
            }
        };

        [JsonIgnore]
        public int NetworkID { get; set; } = 0;

        public LevelTarget Level { get; set; } = new();
        public int StartWave { get; set; } = 1;
        public float StartDelay { get; set; } = 60f;
        public eWardenObjectiveEventType StartEvent { get; set; } = eWardenObjectiveEventType.None;
        public eWardenObjectiveEventType StopEvent { get; set; } = eWardenObjectiveEventType.None;
        public LocaleText CompleteHeader { get; set; } = new("<u>All Waves Complete</u>");
        public List<List<SpawnPathData>> SpawnPaths { get; set; } = EmptyList<List<SpawnPathData>>.Instance;
        public List<WaveGroupData> WaveSequence { get; set; } = EmptyList<WaveGroupData>.Instance;
    }
}
