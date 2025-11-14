using WaveSurvival.Attributes;
using UnityEngine;
using WaveSurvival.CustomWaveData;

namespace WaveSurvival.CustomWave
{
    public sealed class WaveText
    {
        public static readonly WaveText Current = new();
        private const float HideFinishDelay = 10f;

        private static PUI_ObjectiveTimer s_waveInfo = null!;
        private static PUI_InteractionPrompt s_intPrompt = null!;
        private static Vector2 s_IntPromptPos;
        private static Vector2 s_IntPromptPosInWave;

        private float _nextWaveTime = -1;
        private bool _canSkip = false;
        private (int wave, int networkID, WaveState state) _waveCache = default;
        private float _hideFinishTime = 0;

        private int _enemyCount = 0;

        internal static void Internal_Setup(PUI_ObjectiveTimer waveInfo, PUI_InteractionPrompt intPrompt)
        {
            s_waveInfo = waveInfo;
            waveInfo.SetVisible(false);
            s_intPrompt = intPrompt;
            s_intPrompt.SetVisible(false);
            s_intPrompt.SetTimerFill(0);
            s_intPrompt.SetMessage("");
            s_intPrompt.m_timerAlpha = 0;
            s_IntPromptPos = s_intPrompt.RectTrans.anchoredPosition;
            s_IntPromptPosInWave = s_IntPromptPos;
            s_IntPromptPosInWave.y -= 25f;
        }

        [InvokeOnCleanup]
        private static void OnCleanup()
        {
            s_waveInfo.SetVisible(false);
            s_intPrompt.SetVisible(false);
            s_intPrompt.SetMessage("");
            s_intPrompt.SetTimerFill(0);
            s_intPrompt.m_timerAlpha = 0;
            s_intPrompt.RectTrans.anchoredPosition = s_IntPromptPos;
        }

        internal void Internal_ReceiveWave(int wave, int networkID, float nextWaveTime, WaveState state)
        {
            _waveCache = (wave, networkID, state);
            s_waveInfo.SetVisible(true);
            s_intPrompt.SetVisible(state == WaveState.Wave || state == WaveState.RemainingWaves);
            _nextWaveTime = nextWaveTime;
            _canSkip = state == WaveState.Transition;
            UpdateWaveHeader();
        }

        internal void Internal_ReceiveEnemyCount(int count)
        {
            if (_enemyCount != count)
                s_intPrompt.SetMessage($"Enemies Remaining: <color=orange>{count}</color>");

            _enemyCount = count;
        }

        public void UpdateWaveHeader()
        {
            if (WaveManager.ActiveObjective == null) return;

            if (_waveCache.state == WaveState.Finished)
            {
                s_waveInfo.m_titleText.SetText(WaveManager.ActiveObjective.CompleteHeader);
                _nextWaveTime = 0;
                if (_hideFinishTime <= 0)
                    _hideFinishTime = Clock.Time + HideFinishDelay;
                return;
            }
            _hideFinishTime = 0f;

            if (!DataManager.TryGetWave(_waveCache.networkID, out var waveData)) return;

            if (_waveCache.state != WaveState.Wave)
            {
                s_waveInfo.m_titleText.SetText("<u>Next Wave</u><color=orange>\n{0:0}</color>", _waveCache.wave + 1);
                s_intPrompt.RectTrans.anchoredPosition = s_IntPromptPos;
            }
            else
            {
                var header = waveData.WaveHeader.ToString().Replace("[WAVE]", (_waveCache.wave + 1).ToString());
                s_waveInfo.m_titleText.SetText(header);
                s_intPrompt.RectTrans.anchoredPosition = s_IntPromptPosInWave;
            }
        }

        public void Update()
        {
            var time = Clock.Time;
            if (_hideFinishTime > 0f && time > _hideFinishTime)
            {
                OnCleanup();
                _hideFinishTime = 0;
            }

            if (_nextWaveTime > 0)
            {
                s_waveInfo.m_timerText.enabled = true;
                var toNextWave = TimeSpan.FromSeconds(_nextWaveTime - time);
                if (_canSkip)
                {
                    s_waveInfo.m_timerText.SetText("<size=100%>\nNext Wave In: <color=orange>{0:00}:{1:00}</color></size>\n<color=green><size=60%>[Press 'X' to skip to next wave]</size></color>", toNextWave.Minutes, toNextWave.Seconds);
                }
                else
                {
                    s_waveInfo.m_timerText.SetText("<size=100%>\nNext Wave In: <color=orange>{0:00}:{1:00}</color></size>", toNextWave.Minutes, toNextWave.Seconds);
                }
            }
            else
            {
                if (_canSkip)
                {
                    s_waveInfo.m_timerText.enabled = true;
                    s_waveInfo.m_timerText.SetText("<color=green><size=60%>[Press 'X' to skip to next wave]</size></color>");
                }
                else
                    s_waveInfo.m_timerText.enabled = false;
            }
        }
    }
}
