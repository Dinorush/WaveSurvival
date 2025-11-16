using Il2CppInterop.Runtime.Injection;
using SNetwork;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using WaveSurvival.Attributes;
using WaveSurvival.CustomWaveData;
using WaveSurvival.CustomWaveData.WaveObjective;

namespace WaveSurvival.CustomWave
{
    public sealed partial class WaveManager : MonoBehaviour
    {
        public WaveManager(IntPtr ptr) : base(ptr) { }

        [InvokeOnLoad]
        private static void OnLoad()
        {
            ClassInjector.RegisterTypeInIl2Cpp<WaveManager>();
        }

        [InvokeOnAssetsLoaded]
        private static void OnAssetsLoaded()
        {
            var go = new GameObject(EntryPoint.MODNAME + "_WaveManager");
            UnityEngine.Object.DontDestroyOnLoad(go);
            Current = go.AddComponent<WaveManager>();
            IsActive = false;
        }

        public static WaveManager Current { get; private set; } = null!;
        public readonly static System.Random Random = new();

        public static bool IsActive { get; private set; } = false;
        public static bool IsMaster { get; private set; } = false;
        public static WaveObjectiveData? ActiveObjective { get; private set; } = null;
        public static bool TryGetActiveObjective([MaybeNullWhen(false)] out WaveObjectiveData objective)
        {
            objective = ActiveObjective;
            return IsActive && IsMaster;
        }

        private CheckpointData _checkpointData;

        private void Update()
        {
            if (!IsMaster || !IsActive)
            {
                WaveText.Current.Update();
                return;
            }

            UpdateWaves();
            UpdateSpawners();
            WaveText.Current.Update();
        }

        [InvokeOnEnter]
        private static void OnEnter()
        {
            IsMaster = SNet.IsMaster;
            Current.StartObjective();
        }

        private void StartObjective()
        {
            if (ActiveObjective == null || IsActive || !IsMaster) return;

            WaveNetwork.SendObjective(ActiveObjective);
            IsActive = true;

            SetupSpawners();
            SetupWaves();
        }

        private void FinishObjective()
        {
            WaveText.Current.Update();
            Cleanup();
        }

        [InvokeOnCleanup]
        private static void OnCleanup() => Current.Cleanup();
        private void Cleanup()
        {
            CleanupWaves();
            CleanupSpawners();
            CleanupEnemyCount();

            ActiveObjective = null;
            IsActive = false;
        }

        internal static void Internal_ReceiveObjectiveComplete() => Current.FinishObjective();

        internal static void Internal_ReceiveObjectiveID(int id)
        {
            if (DataManager.TryGetObjective(id, out var objective))
            {
                ActiveObjective = objective;
                IsActive = true;
                WaveText.Current.UpdateWaveHeader();
            }
        }

        internal static void Internal_OnReloadFile(List<WaveObjectiveData> dataList)
        {
            if (Current == null) return;

            if (TryGetActiveObjective(out var objective))
            {
                var level = objective.Level;
                foreach (var data in dataList)
                {
                    // Will fail if the start event/dimension changes, but there's no unique ID for objectives
                    if (data.Level.IsMatch(level) && data.StartEvent == objective.StartEvent && data.DimensionIndex == objective.DimensionIndex)
                        ActiveObjective = data;
                }
            }

            Current.LoadLevelObjectives();
        }

        internal static void Internal_OnCheckpointReached() => Current.OnCheckpointReached();
        private void OnCheckpointReached()
        {
            _checkpointData.active = IsActive;

            if (!TryGetActiveObjective(out var objective)) return;

            _checkpointData.objective = objective;
            CheckpointStoreObjectives();
            CheckpointStoreWaves();
        }

        internal static void Internal_OnCheckpointReload() => Current.OnCheckpointReload();
        private void OnCheckpointReload()
        {
            Current.Cleanup();
            if (!IsMaster || !_checkpointData.active) return;

            ActiveObjective = _checkpointData.objective;
            WaveNetwork.SendObjective(ActiveObjective);
            IsActive = true;
            SetupSpawners();

            CheckpointReloadObjectives();
            CheckpointReloadWaves();
        }

        struct CheckpointData
        {
            public bool active;
            public WaveObjectiveData objective;
            public List<KeyValuePair<eDimensionIndex, WaveObjectiveData>> dimensionStartData;
            public List<WaveStep> waves;
            public int currentWave;
        }
    }
}
