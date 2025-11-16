using LevelGeneration;

namespace WaveSurvival.CustomWave
{
    // Helper class used to facilitate zone searching algorithms for spawn path updating.
    public sealed class ZoneNode
    {
        public readonly LG_Zone Zone;
        public readonly ZoneNode? Parent;
        public readonly List<ZoneNode> Children = new();
        public bool IsOpened { get; private set; } = false;
        public bool IsReachable { get; private set; } = false;

        public ZoneNode(LG_Zone zone)
        {
            Zone = zone;
            var parentZone = zone.m_sourceGate?.m_linksFrom?.m_zone;
            if (parentZone != null)
            {
                // Parent will have generated first, so we know it exists
                Parent = ZoneTree.GetZoneNode(parentZone)!;
                Parent.Children.Add(this);
                IsOpened = zone.m_sourceGate!.IsTraversable;
            }
            else
                IsOpened = true;
        }

        public void SetOpenAndPropagate(bool opened)
        {
            IsOpened = opened;
            PropagateReachable(Parent?.IsReachable != false);
        }

        private void PropagateReachable(bool reachable)
        {
            reachable &= IsOpened;
            if (IsReachable != reachable)
            {
                foreach (var child in Children)
                    child.PropagateReachable(reachable);
            }
            IsReachable = reachable;
        }
    }
}
