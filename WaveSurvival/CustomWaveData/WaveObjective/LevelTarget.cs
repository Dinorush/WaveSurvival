using WaveSurvival.Json.Converters;
using System.Text.Json.Serialization;

namespace WaveSurvival.CustomWaveData.WaveObjective
{
    [JsonConverter(typeof(LevelTargetConverter))]
    public sealed class LevelTarget
    {
        public uint LevelLayoutID { get; set; } = 0;
        public eRundownTier Tier { get; set; } = eRundownTier.Surface;
        public int TierIndex { get; set; } = -1;

        public bool IsMatch(LevelTarget other) => IsMatch(other.LevelLayoutID, other.Tier, other.TierIndex);
        public bool IsMatch(uint layoutID, eRundownTier tier, int tierIndex)
        {
            if (LevelLayoutID != 0 && layoutID == LevelLayoutID)
            {
                return true;
            }
            else if (Tier == tier && (TierIndex == -1 || TierIndex == tierIndex))
            {
                return true;
            }

            return false;
        }
    }
}
