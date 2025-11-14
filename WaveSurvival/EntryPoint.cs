using BepInEx;
using BepInEx.Unity.IL2CPP;
using GTFO.API;
using HarmonyLib;
using WaveSurvival.Attributes;
using System.Reflection;
using WaveSurvival.Dependencies;
using WaveSurvival.Settings;

namespace WaveSurvival
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    [BepInDependency(MTFOWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(PartialData.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ArchiveWrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class EntryPoint : BasePlugin
    {
        public const string
            MODNAME = "WaveSurvival",
            AUTHOR = "Dinorush",
            GUID = AUTHOR + "." + MODNAME,
            VERSION = "1.0.0";

        private IEnumerable<MethodInfo> _cleanupCallbacks = null!;
        private IEnumerable<MethodInfo> _enterCallbacks = null!;
        private IEnumerable<MethodInfo> _buildStartCallbacks = null!;
        private IEnumerable<MethodInfo> _buildDoneCallbacks = null!;

        public override void Load()
        {
            CacheFrequentCallbacks();
            LevelAPI.OnLevelCleanup += RunFrequentCallback(_cleanupCallbacks);
            LevelAPI.OnEnterLevel += RunFrequentCallback(_enterCallbacks);
            LevelAPI.OnBuildStart += RunFrequentCallback(_buildStartCallbacks);
            LevelAPI.OnBuildDone += RunFrequentCallback(_buildDoneCallbacks);

            new Harmony(MODNAME).PatchAll();

            AssetAPI.OnStartupAssetsLoaded += InvokeCallbacks<InvokeOnAssetsLoadedAttribute>;
            ClientSettings.Init();
            InvokeCallbacks<InvokeOnLoadAttribute>();
            DinoLogger.Log($"Loaded {MODNAME}");
        }

        private static Action RunFrequentCallback(IEnumerable<MethodInfo> callbacks)
        {
            return () =>
            {
                foreach (var callback in callbacks)
                    callback.Invoke(null, null);
            };
        }

        private void CacheFrequentCallbacks()
        {
            Type[] typesFromAssembly = AccessTools.GetTypesFromAssembly(GetType().Assembly);
            var methods = typesFromAssembly.SelectMany(AccessTools.GetDeclaredMethods).Where(method => method.IsStatic);
            var cleanups = from method in methods
                           let attr = method.GetCustomAttribute<InvokeOnCleanupAttribute>()
                           where attr != null
                           select new { Method = method, Attribute = attr };

            _cleanupCallbacks = from pair in cleanups select pair.Method;

            _enterCallbacks = from method in methods
                              where method.GetCustomAttribute<InvokeOnEnterAttribute>() != null
                              select method;

            _buildStartCallbacks = from method in methods
                                  where method.GetCustomAttribute<InvokeOnBuildStartAttribute>() != null
                                  select method;

            _buildDoneCallbacks = from method in methods
                                  where method.GetCustomAttribute<InvokeOnBuildDoneAttribute>() != null
                                  select method;
        }


        private void InvokeCallbacks<T>() where T : Attribute
        {
            Type[] typesFromAssembly = AccessTools.GetTypesFromAssembly(GetType().Assembly);
            IEnumerable<MethodInfo> enumerable = from method in typesFromAssembly.SelectMany(AccessTools.GetDeclaredMethods)
                                                 where method.GetCustomAttribute<T>() != null
                                                 where method.IsStatic
                                                 select method;
            foreach (MethodInfo item in enumerable)
                item.Invoke(null, null);
        }
    }
}