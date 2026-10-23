using LingGuangV05.Desktop;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LingGuangV05.CaseMenu
{
    /// <summary>Direct-Main fallback. Normal title entry reuses the already-built title HUD.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(EscapeKey.SettingsOrder)]
    public sealed class DesktopSettingsOverlay : MonoBehaviour
    {
        CaseMenuHud hud;

        ChapterOneRuntime Runtime
        {
            get
            {
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    var runtime = root.GetComponentInChildren<ChapterOneRuntime>(true);
                    if (runtime != null) return runtime;
                }
                return null;
            }
        }

        public void Open(TMP_FontAsset font)
        {
            if (hud == null)
            {
                // A root overlay must not inherit the desktop's offscreen LCD canvas.
                var root = new GameObject("Desktop Game Settings");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
                hud = root.AddComponent<CaseMenuHud>();
                hud.Build(font, GameSettings.SetVolume, GameSettings.SetReducedMotion,
                    GameSettings.SetFullscreen, Close, null);
                hud.SetChromeAlpha(0);
                hud.ConfigureLifeEvents(() => GameSettings.CurrentLifeEventMode(Runtime),
                    mode => GameSettings.SetLifeEventMode(Runtime, mode, hud));
                hud.ConfigureOfflineIncome(() => GameSettings.CurrentOfflineIncome(Runtime),
                    enabled => GameSettings.SetOfflineIncome(Runtime, enabled, hud));
            }
            hud.ShowSettings(AudioListener.volume, GameSettings.ReducedMotion, Screen.fullScreen);
        }

        public void Close()
        {
            if (hud != null) hud.HideSettings();
            PlayerPrefs.Save();
        }

        void Update()
        {
            if (hud == null || !hud.IsSettingsOpen || !EscapeKey.TryConsume()) return;
            if (hud.IsPlayerHelpOpen) hud.ClosePlayerHelp(); else Close();
        }

        void OnDisable() { if (hud != null && hud.IsSettingsOpen) Close(); }
        void OnDestroy()
        {
            if (hud == null) return;
            if (Application.isPlaying) Destroy(hud.gameObject); else DestroyImmediate(hud.gameObject);
        }
    }
}
