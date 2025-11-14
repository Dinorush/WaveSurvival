using BepInEx.Configuration;
using BepInEx;
using GTFO.API.Utilities;
using UnityEngine;

namespace WaveSurvival.Settings.Vanilla
{
    internal static class Configuration
    {
        private static ConfigEntry<KeyCode> _skipWaveBind = null!;
        public static KeyCode SkipWaveBind => _skipWaveBind.Value;

        private static ConfigFile _configFile = null!;

        internal static void Init()
        {
            _configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg"), saveOnInit: true);

            string section = "Keybinds";
            _skipWaveBind = _configFile.Bind(section, "Skip Wave Keybind", KeyCode.X, "The keybind to skip the wait until the next wave (host only).");

            LiveEditListener listener = LiveEdit.CreateListener(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg", false);
            listener.FileChanged += OnFileChanged;
        }

        private static void OnFileChanged(LiveEditEventArgs _) => _configFile.Reload();
    }
}
