using WaveSurvival.Json.Converters;
using WaveSurvival.Utils;
using System.Text.Json.Serialization;
using WaveSurvival.Json;

namespace WaveSurvival.CustomWaveData.Wave
{
    [JsonConverter(typeof(WaveDataConverter))]
    public class WaveData
    {
        public static readonly Dictionary<string, WaveData> Template = new()
        {
            {
                "Example", new WaveData()
                {
                    Spawns = new()
                    {
                        new JsonReference<SpawnData>()
                        {
                            Value = new()
                            {
                                Enemies = new(new List<WeightedEnemyData>() { new() })
                            }
                        }
                    }
                }
            },
            {
                "Example Two", new WaveData()
                {
                    Spawns = new()
                    {
                        new JsonReference<SpawnData>()
                        {
                            Value = new()
                            {
                                Enemies = new("Example")
                            }
                        }
                    }
                }
            },
            {
                "Example Three", new WaveData()
                {
                    Spawns = new()
                    {
                        new JsonReference<SpawnData>("Example")
                    }
                }
            }
        };

        [JsonIgnore]
        public int NetworkID { get; set; } = 0;

        public List<JsonReference<SpawnData>> Spawns { get; set; } = EmptyList<JsonReference<SpawnData>>.Instance;
        public LocaleText WaveHeader { get; set; } = new("<u>Current Wave</u>\n<color=orange>[WAVE]</color>");
        public ScreamSize ScreamSize { get; set; } = ScreamSize.Small;
        public ScreamType ScreamType { get; set; } = ScreamType.Striker;

        public void ResolveReferences()
        {
            Spawns.RemoveAll(reference => !DataManager.Resolve(reference));
            foreach (var spawn in Spawns)
                spawn.Value!.ResolveReferences();
        }
    }
}
