using HongmengOS.Aero2010;
using LingGuangV05.Runtime;
using UnityEngine;

namespace LingGuangV05.Desktop
{
    /// <summary>Formats the native monitor OSD only; hardware values and input stay native.</summary>
    public sealed class DesktopLocalizedMonitor : MonoBehaviour
    {
        public AeroLcdController monitor;
        private int lastBrightness = -1;
        private string lastLanguage, lastOutput;
        private void OnEnable() { GameText.Changed += Render; Render(); }
        private void OnDisable() { GameText.Changed -= Render; }
        // ShowOsd writes after SetActive. Render afterward, never replace the hardware command.
        private void LateUpdate() { Render(); }
        private void Render()
        {
            if (monitor == null || monitor.osdLabel == null) return;
            int value = Mathf.RoundToInt(monitor.brightness * 100);
            string language = GameText.LanguageId;
            if (value == lastBrightness && language == lastLanguage && monitor.osdLabel.text == lastOutput) return;
            lastBrightness = value; lastLanguage = language;
            lastOutput = GameText.F("亮度   {0}%\n\n标准模式   ·   1920 × 1080 / 60 Hz", "Brightness   {0}%\n\nStandard   ·   1920 × 1080 / 60 Hz", value);
            monitor.osdLabel.text = lastOutput;
        }
    }
}
