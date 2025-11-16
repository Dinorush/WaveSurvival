using GameData;
using WaveSurvival.Utils;

namespace WaveSurvival.CustomWaveData.WaveObjective
{
    public class WaveEventData
    {
        public List<WardenObjectiveEventData> EventsOnWaveStart { get; set; } = EmptyList<WardenObjectiveEventData>.Instance;
        public List<WardenObjectiveEventData> EventsOnWaveEnd { get; set; } = EmptyList<WardenObjectiveEventData>.Instance;
        public float TimeToNextOnStart { get; set; } = 0f;
        public float TimeToNextOnEnd { get; set; } = 60f;
        public bool EndOnAllWavesEnd { get; set; } = false;
        public float HealthGainOnEnd { get; set; } = 0f;
        public float MainAmmoGainOnEnd { get; set; } = 0f;
        public float SpecialAmmoGainOnEnd { get; set; } = 0f;
        public float ToolAmmoGainOnEnd { get; set; } = 0f;

        public bool HasAnyAmmoGain => MainAmmoGainOnEnd > 0f || SpecialAmmoGainOnEnd > 0f || ToolAmmoGainOnEnd > 0f;
        public bool HasAnyGain => HealthGainOnEnd > 0f || HasAnyAmmoGain;
    }
}
