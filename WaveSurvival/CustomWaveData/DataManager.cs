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

        private const string WaveDir = "Waves";
        private const string ObjectiveDir = "Objectives";

        private readonly Dictionary<string, List<WaveObjectiveData>> _objectiveFiles = new();
        private readonly Dictionary<string, Dictionary<string, WaveData>> _waveFiles = new();
        private readonly Dictionary<string, WaveData> _idToWaves = new();
        private readonly List<WaveObjectiveData> _objectiveDatas = new();
        private readonly List<WaveData> _waveDatas = new();

        public static IReadOnlyList<WaveObjectiveData> ObjectiveDatas => Current._objectiveDatas;
        public static IReadOnlyList<WaveData> WaveDatas => Current._waveDatas;

        public static bool TryGetWave(string id, [MaybeNullWhen(false)] out WaveData wave) => Current._idToWaves.TryGetValue(id, out wave);
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

        private void ObjectiveFileCreated(LiveEditEventArgs e)
        {
            DinoLogger.Warning($"Objective file {e.FileName} created");
            LiveEdit.TryReadFileContent(e.FullPath, (content) => ReadObjectiveContent(e.FullPath, content));
            OnObjectiveReload();
        }

        private void ObjectiveFileChanged(LiveEditEventArgs e)
        {
            DinoLogger.Warning($"Objective file {e.FileName} changed");
            LiveEdit.TryReadFileContent(e.FullPath, (content) => ReadObjectiveContent(e.FullPath, content));
            OnObjectiveReload();
        }

        private void ObjectiveFileRemoved(LiveEditEventArgs e)
        {
            DinoLogger.Warning($"Objective file {e.FileName} removed");
            _objectiveFiles.Remove(e.FullPath);
            OnObjectiveReload();
        }

        private void ReadObjectiveContent(string filepath, string content)
        {
            if (!JSON.TryDeserializeSafe<List<WaveObjectiveData>>(content, out var newList)) return;

            WaveManager.Internal_OnReloadFile(newList);
            _objectiveFiles[filepath] = newList;
        }

        private void OnObjectiveReload()
        {
            _objectiveDatas.Clear();
            foreach ((var filepath, var dataList) in _objectiveFiles.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                foreach (var data in dataList)
                {
                    data.NetworkID = _objectiveDatas.Count;
                    _objectiveDatas.Add(data);
                }
            }
        }

        private void WaveFileCreated(LiveEditEventArgs e)
        {
            DinoLogger.Warning($"Wave file {e.FileName} created");
            LiveEdit.TryReadFileContent(e.FullPath, (content) => ReadWaveContent(e.FullPath, content));
            OnWaveReload();
        }

        private void WaveFileChanged(LiveEditEventArgs e)
        {
            DinoLogger.Warning($"Wave file {e.FileName} changed");
            LiveEdit.TryReadFileContent(e.FullPath, (content) => ReadWaveContent(e.FullPath, content));
            OnWaveReload();
        }

        private void WaveFileRemoved(LiveEditEventArgs e)
        {
            DinoLogger.Warning($"Wave file {e.FileName} removed");
            _waveFiles.Remove(e.FullPath);
            OnWaveReload();
        }

        private void ReadWaveContent(string filepath, string content)
        {
            if (!JSON.TryDeserializeSafe<Dictionary<string, WaveData>>(content, out var newList)) return;

            _waveFiles[filepath] = newList;
        }

        private void OnWaveReload()
        {
            _idToWaves.Clear();
            _waveDatas.Clear();
            foreach ((var filepath, var dataList) in _waveFiles.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                foreach ((var id, var data) in dataList)
                {
                    if (!_idToWaves.TryAdd(id, data))
                    {
                        DinoLogger.Error($"Found duplicate id {id} in waves, skipping...");
                        continue;
                    }
                    data.NetworkID = _waveDatas.Count;
                    _waveDatas.Add(data);
                }
            }
        }

        public DataManager()
        {
            string DEFINITION_PATH = Path.Combine(MTFO.API.MTFOPathAPI.CustomPath, EntryPoint.MODNAME);
            string OBJECTIVE_PATH = Path.Combine(DEFINITION_PATH, ObjectiveDir);
            string WAVE_PATH = Path.Combine(DEFINITION_PATH, WaveDir);
            if (!Directory.Exists(DEFINITION_PATH))
            {
                DinoLogger.Log($"No {EntryPoint.MODNAME} directory detected. Creating templates.");
                Directory.CreateDirectory(OBJECTIVE_PATH);
                Directory.CreateDirectory(WAVE_PATH);

                StreamWriter file;
                using (file = File.CreateText(Path.Combine(OBJECTIVE_PATH, "Template.json")))
                    file.WriteLine(JSON.Serialize(WaveObjectiveData.Template));

                using (file = File.CreateText(Path.Combine(WAVE_PATH, "Template.json")))
                    file.WriteLine(JSON.Serialize(WaveData.Template));
            }

            foreach (string filepath in Directory.EnumerateFiles(OBJECTIVE_PATH, "*.json", SearchOption.AllDirectories))
                ReadObjectiveContent(filepath, File.ReadAllText(filepath));
            OnObjectiveReload();
            var listener = LiveEdit.CreateListener(OBJECTIVE_PATH, "*.json", true);
            listener.FileChanged += ObjectiveFileChanged;
            listener.FileCreated += ObjectiveFileCreated;
            listener.FileDeleted += ObjectiveFileRemoved;

            foreach (string filepath in Directory.EnumerateFiles(WAVE_PATH, "*.json", SearchOption.AllDirectories))
                ReadWaveContent(filepath, File.ReadAllText(filepath));
            OnWaveReload();
            listener = LiveEdit.CreateListener(WAVE_PATH, "*.json", true);
            listener.FileChanged += WaveFileChanged;
            listener.FileCreated += WaveFileCreated;
            listener.FileDeleted += WaveFileRemoved;
        }

        [InvokeOnLoad]
        private static void OnLoad()
        {
            Current.Init();
        }

        private void Init()
        {

        }
    }
}
