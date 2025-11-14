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

            SetupSpawners();
            SetupWaves();

            IsActive = true;
            _nextWaveTime = Clock.Time + ActiveObjective.StartDelay;
            _currentWave = ActiveObjective.StartWave - 2; // Shift 1-indexed from user to 0-indexed, then minus 1 since StartNextWave advances it
            WaveNetwork.SetWave(_currentWave + 1, GetNetworkID(_currentWave + 1), _nextWaveTime, WaveState.Transition);
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
                    // Will fail if the start event changes, but there's no other way to easily identify the object through changes
                    if (data.Level.IsMatch(level) && data.StartEvent == objective.StartEvent)
                        ActiveObjective = data;
                }
            }

            Current.SetupWardenEventObjectives();
        }
    }
}
