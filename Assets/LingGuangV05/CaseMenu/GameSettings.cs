using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using UnityEngine;

namespace LingGuangV05.CaseMenu
{
    /// <summary>One preference and save contract for the title and desktop settings entries.</summary>
    public static class GameSettings
    {
        public const string VolumePref = "LingGuangV05.MasterVolume";
        public const string ReduceMotionPref = "LingGuangV05.ReduceMotion";
        public const string FullscreenPref = "LingGuangV05.Fullscreen";
        public static bool ReducedMotion => PlayerPrefs.GetInt(ReduceMotionPref, 0) != 0;

        public static void ApplySaved()
        {
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePref, .8f));
            if (!Application.isEditor && PlayerPrefs.HasKey(FullscreenPref))
                Screen.fullScreen = PlayerPrefs.GetInt(FullscreenPref) != 0;
        }

        public static void SetVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumePref, AudioListener.volume);
        }

        public static void SetReducedMotion(bool value) => PlayerPrefs.SetInt(ReduceMotionPref, value ? 1 : 0);

        public static void SetFullscreen(bool value)
        {
            PlayerPrefs.SetInt(FullscreenPref, value ? 1 : 0);
            Screen.fullScreen = value;
        }

        static XingGuangController LifeSettingsOwner(ChapterOneRuntime runtime)
        {
            var owner = runtime != null ? runtime.GetComponent<XingGuangController>() : null;
            return owner != null && owner.LifeEventSettingsAvailable ? owner : null;
        }

        public static int? CurrentLifeEventMode(ChapterOneRuntime runtime) => LifeSettingsOwner(runtime)?.Sim.LifeEventIntervalMode;

        public static void SetLifeEventMode(ChapterOneRuntime runtime, int mode, CaseMenuHud hud)
        {
            var owner = LifeSettingsOwner(runtime);
            if (owner == null) return;
            if (!owner.SetLifeEventIntervalMode(mode))
                hud.ShowSettingsError(GameText.T("事件设置已修改，但存档未保存。", "Event setting changed, but the save could not be written."));
        }
    }
}
