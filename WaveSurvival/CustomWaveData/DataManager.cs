using GTFO.API.Utilities;
using System.Diagnostics.CodeAnalysis;
using WaveSurvival.Attributes;
using WaveSurvival.CustomWave;
using WaveSurvival.CustomWaveData.Wave;
using WaveSurvival.CustomWaveData.WaveObjective;
using WaveSurvival.Json;

namespace WaveSurvival.CustomWaveData
{
    public sealed class DataManager
    {
        public static readonly DataManager Current = new();

        private const string ObjectiveDir = "Main";
        private const string DataDir = "Data";

        class DataFolder<T> where T : new()
        {
            public readonly string Dir;

            public DataFolder(string dir) { Dir = dir; }

            private readonly Dictionary<string, Dictionary<string, T>> _fileData = new();
            private readonly Dictionary<string, T> _idMap = new();

            public bool TryGetValue(string id, [MaybeNullWhen(false)] out T value) => _idMap.TryGetValue(id, out value);

            public void SetFileData(string filepath, Dictionary<string, T> data) => _fileData[filepath] = data;
            public void RemoveFile(string filepath) => _fileData.Remove(filepath);

            public void ReloadIDs(Action<T>? onAddData = null)
            {
                _idMap.Clear();
                foreach ((var filepath, var dataList) in _fileData.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                {
                    foreach ((var id, var data) in dataList)
                    {
                        if (!_idMap.TryAdd(id, data))
                        {
                            DinoLogger.Error($"Found duplicate id {id} in {Dir}, skipping...");
                            continue;
                        }
                        onAddData?.Invoke(data);
                    }
                }
            }
        }

        private readonly DataFolder<WaveData> _waveFolder = new("Waves");
        private readonly DataFolder<SpawnData> _spawnFolder = new("Spawns");
        private readonly DataFolder<List<WeightedEnemyData>> _enemyFolder = new("Enemies");
        private readonly DataFolder<WaveEventData> _eventFolder = new("Events");

        private readonly Dictionary<string, List<WaveObjectiveData>> _objectiveFiles = new();

        private readonly List<WaveObjectiveData> _objectiveDatas = new();
        private readonly List<WaveData> _waveDatas = new();

        public static IReadOnlyList<WaveObjectiveData> ObjectiveDatas => Current._objectiveDatas;
        public static IReadOnlyList<WaveData> WaveDatas => Current._waveDatas;

        private static bool Resolve<T>(JsonReference<T> reference, DataFolder<T> dataFolder) where T : new()
        {
            if (reference.Value != null) return true;

            if (dataFolder.TryGetValue(reference.ID, out var value))
            {
                reference.Value = value;
                return true;
            }
            DinoLogger.Error($"Unable to resolve ID {reference.ID} for type {typeof(T)}");
            return false;
        }
        public static bool Resolve(JsonReference<List<WeightedEnemyData>> reference) => Resolve(reference, Current._enemyFolder);
        public static bool Resolve(JsonReference<SpawnData> reference) => Resolve(reference, Current._spawnFolder);
        public static bool Resolve(JsonReference<WaveData> reference) => Resolve(reference, Current._waveFolder);
        public static bool Resolve(JsonReference<WaveEventData> reference) => Resolve(reference, Current._eventFolder);

        public static bool TryGetWave(int networkID, [MaybeNullWhen(false)] out WaveData wave)
        {
            if (networkID >= 0 && networkID < Current._waveDatas.Count)
            {
                wave = Current._waveDatas[networkID];
                return true;
            }
            wave = null;
            return false;
        }

        public static bool TryGetObjective(int networkID, [MaybeNullWhen(false)] out WaveObjectiveData objective)
        {
            if (networkID >= 0 && networkID < Current._objectiveDatas.Count)
            {
                objective = Current._objectiveDatas[networkID];
                return true;
            }
            objective = null;
            return false;
        }

        private void ObjectiveFileChanged(LiveEditEventArgs e)
        {
            DinoLogger.Warning($"Main file {e.FileName} changed");
            LiveEdit.TryReadFileContent(e.FullPath, (content) => ReadObjectiveContent(e.FullPath, content));
            OnReload();
        }

        private void ObjectiveFileRemoved(LiveEditEventArgs e)
        {
            DinoLogger.Warning($"Main file {e.FileName} removed");
            _objectiveFiles.Remove(e.FullPath);
            OnReload();
        }

        private void ReadObjectiveContent(string filepath, string content)
        {
            if (!JSON.TryDeserializeSafe<List<WaveObjectiveData>>(content, out var newList))
            {
                DinoLogger.Error("Failed to read main file!");
                return;
            }

            WaveManager.Internal_OnReloadFile(newList);
            _objectiveFiles[filepath] = newList;
        }

        private LiveEditEventHandler DataFileChanged<T>(DataFolder<T> dataFolder) where T : new()
        {
            return (e) =>
            {
                DinoLogger.Warning($"{dataFolder.Dir} file {e.FileName} changed");
                LiveEdit.TryReadFileContent(e.FullPath, (content) => ReadDataContent(e.FullPath, content, dataFolder));
                OnReload();
            };
        }

        private LiveEditEventHandler DataFileRemoved<T>(DataFolder<T> dataFolder) where T : new()
        {
            return (e) =>
            {
                DinoLogger.Warning($"{dataFolder.Dir} file {e.FileName} removed");
                dataFolder.RemoveFile(e.FullPath);
                OnReload();
            };
        }

        private void ReadDataContent<T>(string filepath, string content, DataFolder<T> dataFolder) where T : new()
        {
            if (!JSON.TryDeserializeSafe<Dictionary<string, T>>(content, out var newList))
            {
                DinoLogger.Error($"Failed to read {dataFolder.Dir} file!");
                return;
            }

            dataFolder.SetFileData(filepath, newList);
        }

        private void OnReload()
        {
            _waveDatas.Clear();

            _enemyFolder.ReloadIDs();
            _spawnFolder.ReloadIDs();
            _waveFolder.ReloadIDs((data) =>
            {
                data.NetworkID = _waveDatas.Count;
                _waveDatas.Add(data);
            });
            _eventFolder.ReloadIDs();

            _objectiveDatas.Clear();
            foreach ((var filepath, var dataList) in _objectiveFiles.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                foreach (var data in dataList)
                {
                    data.NetworkID = _objectiveDatas.Count;
                    _objectiveDatas.Add(data);
                    data.ResolveReferences();
                }
            }
        }

        public DataManager()
        {
            string DEFINITION_PATH = Path.Combine(MTFO.API.MTFOPathAPI.CustomPath, EntryPoint.MODNAME);
            string OBJECTIVE_PATH = Path.Combine(DEFINITION_PATH, ObjectiveDir);
            string DATA_PATH = Path.Combine(DEFINITION_PATH, DataDir);
            string ENEMY_PATH = Path.Combine(DATA_PATH, _enemyFolder.Dir);
            string SPAWN_PATH = Path.Combine(DATA_PATH, _spawnFolder.Dir);
            string WAVE_PATH = Path.Combine(DATA_PATH, _waveFolder.Dir);
            string EVENT_PATH = Path.Combine(DATA_PATH, _eventFolder.Dir);

            if (!Directory.Exists(DEFINITION_PATH))
            {
                DinoLogger.Log($"No {EntryPoint.MODNAME} directory detected. Creating templates.");
                Directory.CreateDirectory(OBJECTIVE_PATH);
                Directory.CreateDirectory(DATA_PATH);
                Directory.CreateDirectory(ENEMY_PATH);
                Directory.CreateDirectory(SPAWN_PATH);
                Directory.CreateDirectory(WAVE_PATH);
                Directory.CreateDirectory(EVENT_PATH);

                StreamWriter file;
                using (file = File.CreateText(Path.Combine(OBJECTIVE_PATH, "Template.json")))
                    file.WriteLine(JSON.Serialize(WaveObjectiveData.Template));

                using (file = File.CreateText(Path.Combine(ENEMY_PATH, "Template.json")))
                    file.WriteLine(JSON.Serialize(WeightedEnemyData.Template));
                using (file = File.CreateText(Path.Combine(SPAWN_PATH, "Template.json")))
                    file.WriteLine(JSON.Serialize(SpawnData.Template));
                using (file = File.CreateText(Path.Combine(WAVE_PATH, "Template.json")))
                    file.WriteLine(JSON.Serialize(WaveData.Template));
                using (file = File.CreateText(Path.Combine(EVENT_PATH, "Template.json")))
                    file.WriteLine(JSON.Serialize(WaveEventData.Template));
            }

            foreach (string filepath in Directory.EnumerateFiles(OBJECTIVE_PATH, "*.json", SearchOption.AllDirectories))
                ReadObjectiveContent(filepath, File.ReadAllText(filepath));

            var listener = LiveEdit.CreateListener(OBJECTIVE_PATH, "*.json", true);
            listener.FileCreated += ObjectiveFileChanged;
            listener.FileChanged += ObjectiveFileChanged;
            listener.FileDeleted += ObjectiveFileRemoved;

            void InitDataFolder<T>(string path, DataFolder<T> dataFolder) where T : new()
            {
                foreach (string filepath in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
                    ReadDataContent(filepath, File.ReadAllText(filepath), dataFolder);
                listener = LiveEdit.CreateListener(path, "*.json", true);
                listener.FileCreated += DataFileChanged(dataFolder);
                listener.FileChanged += DataFileChanged(dataFolder);
                listener.FileChanged += DataFileRemoved(dataFolder);
            }

            InitDataFolder(ENEMY_PATH, _enemyFolder);
            InitDataFolder(SPAWN_PATH, _spawnFolder);
            InitDataFolder(WAVE_PATH, _waveFolder);
            InitDataFolder(EVENT_PATH, _eventFolder);
        }

        [InvokeOnLoad]
        private static void OnLoad()
        {
            Current.Init();
        }

        private void Init()
        {
            OnReload();
        }
    }
}
