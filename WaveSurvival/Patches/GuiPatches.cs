using HarmonyLib;
using WaveSurvival.CustomWave;
using UnityEngine;

namespace WaveSurvival.Patches
{
    [HarmonyPatch]
    internal static class GuiPatches
    {
        [HarmonyPatch(typeof(GuiManager), nameof(GuiManager.Setup))]
        [HarmonyPostfix]
        private static void AddNewLayer()
        {
            var waveInfo = GuiManager.PlayerLayer.AddComp("Gui/Player/PUI_ObjectiveTimer", GuiAnchor.MidCenter, new Vector2(0.0f, 425.0f), null).Cast<PUI_ObjectiveTimer>();
            var intPrompt = GuiManager.InteractionLayer.AddRectComp("Gui/Player/PUI_InteractionPrompt_CellUI", GuiAnchor.MidCenter, new Vector2(0.0f, 375f), null).Cast<PUI_InteractionPrompt>();
            WaveText.Internal_Setup(waveInfo, intPrompt);
        }
    }
}
