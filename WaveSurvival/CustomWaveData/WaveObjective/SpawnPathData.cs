using GameData;
using WaveSurvival.Json.Converters;
using System.Text.Json.Serialization;
using LevelGeneration;

namespace WaveSurvival.CustomWaveData.WaveObjective
{
    [JsonConverter(typeof(SpawnPathDataConverter))]
    public class SpawnPathData
    {
        public static readonly List<List<SpawnPathData>> Template = new()
        {
            new()
            {
                new(),
                new() { ZoneIndex = eLocalZoneIndex.Zone_1 }
            },
            new()
            {
                new() { ZoneIndex = eLocalZoneIndex.Zone_0, AreaIndex = 1 },
                new() { ZoneIndex = eLocalZoneIndex.Zone_1, AreaIndex = AreaBreak }
            },
        };
        public const int AreaBreak = -999;
        public const int LastArea = -1;

        public eLocalZoneIndex ZoneIndex { get; set; } = eLocalZoneIndex.Zone_0;
        public int AreaIndex { get; set; } = LastArea;
        public LG_LayerType Layer { get; set; } = LG_LayerType.MainLayer;
    }
}
