using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Members;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace WaveSurvival.Settings.Archive
{
    [EnableFeatureByDefault]
    public class WaveClientFeature : Feature
    {
        public override string Name => "Wave Survival";

        public override FeatureGroup Group => FeatureGroups.GetOrCreateModuleGroup("Wave Survival");

        [FeatureConfig]
        public static WaveClientSettings Settings { get; set; } = null!;

        public class WaveClientSettings
        {
            [FSDisplayName("Skip Wave Keybind")]
            [FSDescription("Key used to skip the wait until the next wave (host only).")]
            public KeyCode Key { get; set; } = KeyCode.X;
        }
    }
}
