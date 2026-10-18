using UnityEngine;

namespace LingGuangV05.Desktop
{
    public static class GamePerformance
    {
        public static bool Low => PlayerPrefs.GetInt("LingGuang.Graphics.Low", 0) != 0;
        public static int Frames => PlayerPrefs.GetInt("LingGuang.Graphics.Fps", 60) == 30 ? 30 : 60;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Frames;
            QualitySettings.shadows = Low ? ShadowQuality.Disable : ShadowQuality.All;
            QualitySettings.lodBias = Low ? .7f : 1f;
        }
        public static void SetLow(bool low) { PlayerPrefs.SetInt("LingGuang.Graphics.Low", low ? 1 : 0); Apply(); PlayerPrefs.Save(); }
        public static void SetSixty(bool sixty) { PlayerPrefs.SetInt("LingGuang.Graphics.Fps", sixty ? 60 : 30); Apply(); PlayerPrefs.Save(); }
    }
}
