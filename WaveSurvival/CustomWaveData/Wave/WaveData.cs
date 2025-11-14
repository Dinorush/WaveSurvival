using WaveSurvival.Json.Converters;
using WaveSurvival.Utils;
using System.Text.Json.Serialization;

namespace WaveSurvival.CustomWaveData.Wave
{
    [JsonConverter(typeof(WaveDataConverter))]
    public class WaveData
    {
        public static readonly Dictionary<string, WaveData> Template =
        new() {
            { 
                "Example", new WaveData()
                {
                    Spawns = new()
                    {
                        new SpawnData()
                        {
                            Enemies = new() { new WeightedEnemyData() }
                        }
                    }
                }
            }
        };
        private static readonly LocaleText DefaultHeader = new("<u>Current Wave</u>\n<color=orange>[WAVE]</color>");

        [JsonIgnore]
        public int NetworkID { get; set; } = 0;

        public List<SpawnData> Spawns { get; set; } = EmptyList<SpawnData>.Instance;
        public LocaleText WaveHeader { get; set; } = DefaultHeader;
        public ScreamSize ScreamSize { get; set; } = ScreamSize.Small;
        public ScreamType ScreamType { get; set; } = ScreamType.Striker;
    }
}
