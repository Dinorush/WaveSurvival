using AK;
using WaveSurvival.Attributes;
using WaveSurvival.CustomWaveData.Wave;
using WaveSurvival.Extensions;
using WaveSurvival.Networking;
using SNetwork;
using UnityEngine;
using WaveSurvival.CustomWaveData.WaveObjective;

namespace WaveSurvival.CustomWave
{
    internal static class WaveNetwork
    {
        private static readonly WaveSync _waveSync = new();
        private static readonly ObjectiveSync _objectiveSync = new();
        private static readonly WaveScreamSync _waveScreamSync = new();
        private static readonly EnemyCountSync _enemyCountSync = new();

        internal static void SendObjective(WaveObjectiveData data, SNet_Player? player = null)
        {
            _objectiveSync.Send(data.NetworkID, player, SNet_ChannelType.GameReceiveCritical);
        }

        internal static void SetWavesFinished() => SetWave(0, 0, 0, WaveState.Finished);
        internal static void SetWave(int wave, int networkID, float nextWaveTime, WaveState waveStep)
        {
            _waveSync.Send(new()
            {
                wave = wave,
                networkID = networkID,
                nextWaveTime = nextWaveTime,
                state = waveStep
            },
            priority: SNet_ChannelType.GameReceiveCritical);
        }

        internal static void SetEnemyCount(int enemyCount)
        {
            _enemyCountSync.Send(enemyCount);
        }

        internal static void DoWaveScream(ScreamSize screamSize, ScreamType screamType, Vector3 position)
        {
            if (screamType == ScreamType.None) return;

            _waveScreamSync.Send(new()
            {
                screamSize = screamSize,
                screamType = screamType,
                position = position
            }, 
            priority: SNet_ChannelType.GameReceiveCritical);
        }

        [InvokeOnLoad]
        private static void OnLoad()
        {
            _waveSync.Setup();
            _objectiveSync.Setup();
            _waveScreamSync.Setup();
            _enemyCountSync.Setup();
        }

        class ObjectiveSync : SyncedEvent<int>
        {
            public override string GUID => "Objective";

            protected override void Receive(int id)
            {
                WaveManager.Internal_ReceiveObjectiveID(id);
            }
        }

        class WaveSync : SyncedEvent<pWaveData>
        {
            public override string GUID => "Wave";

            protected override void Receive(pWaveData packet)
            {
                WaveText.Current.Internal_ReceiveWave(packet.wave, packet.networkID, packet.nextWaveTime, packet.state);
                if (packet.state == WaveState.Finished)
                    WaveManager.Internal_ReceiveObjectiveComplete();
            }

            protected override void ReceiveLocal(pWaveData packet) => Receive(packet);
        }

        class WaveScreamSync : SyncedEvent<pScreamData>
        {
            public override string GUID => "Scream";

            protected override void Receive(pScreamData packet)
            {
                CellSoundPlayer roar = new();
                roar.SetSwitch(SWITCHES.ENEMY_TYPE.GROUP, packet.screamType.ToSwitch());
                roar.SetSwitch(SWITCHES.ROAR_SIZE.GROUP, packet.screamSize.ToSwitch());
                roar.SetSwitch(SWITCHES.ENVIROMENT.GROUP, SWITCHES.ENVIROMENT.SWITCH.COMPLEX);
                roar.PostWithCleanup(EVENTS.PLAY_WAVE_DISTANT_ROAR, packet.position);
            }

            protected override void ReceiveLocal(pScreamData packet) => Receive(packet);
        }

        class EnemyCountSync : SyncedEvent<int>
        {
            public override string GUID => "Count";

            protected override void Receive(int count)
            {
                WaveText.Current.Internal_ReceiveEnemyCount(count);
            }

            protected override void ReceiveLocal(int count) => Receive(count);
        }

        struct pWaveData
        {
            public int wave;
            public int networkID;
            public float nextWaveTime;
            public WaveState state;
        }

        struct pScreamData
        {
            public ScreamSize screamSize;
            public ScreamType screamType;
            public Vector3 position;
        }
    }
}
