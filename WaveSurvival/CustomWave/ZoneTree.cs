using WaveSurvival.Attributes;
using LevelGeneration;

namespace WaveSurvival.CustomWave
{
    public sealed class ZoneTree
    {
        public static readonly ZoneTree Current = new();

        private readonly Dictionary<int, ZoneNode> _zoneToNode = new();

        public static bool IsReady { get; private set; } = false;
        public static ZoneNode GetZoneNode(LG_Zone zone) => Current._zoneToNode[zone.ID];

        [InvokeOnBuildDone]
        private static void OnBuildDone() => Current.BuildZoneTree();
        private void BuildZoneTree()
        {
            foreach (var zone in Builder.CurrentFloor.allZones)
                _zoneToNode.Add(zone.ID, new(zone));

            foreach (var dimension in Builder.CurrentFloor.m_dimensions)
            {
                var zones = dimension.MainLayer.m_zones;
                if (zones.Count > 0)
                {
                    _zoneToNode[zones[0].ID].SetOpenAndPropagate(true);
                }
            }

            IsReady = true;
        }

        [InvokeOnCleanup]
        private static void OnCleanup()
        {
            Current._zoneToNode.Clear();
            IsReady = false;
        }

        internal static void Internal_OnDoorStateChanged(LG_Gate gate, bool opened)
        {
            if (!IsReady) return;

            GetZoneNode(gate.m_linksTo.m_zone).SetOpenAndPropagate(opened);
            WaveManager.Internal_OnZoneTreeUpdate(opened);
        }
    }
}
