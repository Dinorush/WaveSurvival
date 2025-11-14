using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WaveSurvival.Dependencies;

namespace WaveSurvival.Settings
{
    internal static class ClientSettings
    {
        public static KeyCode SkipWaveBind => ArchiveWrapper.HasArchive ? GetArchiveBind() : Vanilla.Configuration.SkipWaveBind;

        public static void Init()
        {
            if (ArchiveWrapper.HasArchive)
            {
                LoadArchive();
            }
            else
            {
                Vanilla.Configuration.Init();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void LoadArchive()
        {
            Archive.WaveSurvivalModule.Load();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static KeyCode GetArchiveBind()
        {
            return Archive.WaveClientFeature.Settings.Key;
        }
    }
}
