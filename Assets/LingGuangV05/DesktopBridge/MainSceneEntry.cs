using HongmengOS.Aero2010;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Mail;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Desktop.Zhongbao;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LingGuangV05.Desktop
{
    /// <summary>
    /// Starts the existing desktop after a menu loads Main. RuntimeInitializeOnLoadMethod
    /// only covers the initial scene; this explicit entry retains direct-Main play and
    /// never creates game state inside a menu, edits a scene, or resets a saved game.
    /// </summary>
    public static class MainSceneEntry
    {
        public const string ScenePath = "Assets/LingGuangV05/Scenes/Main.unity";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void HideUnusedPhotoGalleryAfterSceneLoad()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.path == ScenePath) HideUnusedPhotoGallery(scene);
        }

        /// <summary>The retired messenger used this native slot; do not restore the demo photo app in its place.</summary>
        internal static void HideUnusedPhotoGallery(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var window in root.GetComponentsInChildren<WindowManager>(true))
                {
                    if (window.name != "Photo Gallery") continue;
                    if (window.isOn) window.CloseWindow();
                    window.gameObject.SetActive(false);
                    if (window.taskbarButton != null) window.taskbarButton.gameObject.SetActive(false);
                }
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    {
                        var window = button.onClick.GetPersistentTarget(i) as WindowManager;
                        if (window == null || window.gameObject.scene != scene || window.name != "Photo Gallery"
                            || button.onClick.GetPersistentMethodName(i) != nameof(WindowManager.OpenWindow)) continue;
                        // Native Places/Pictures is separate from programs; hiding the row alone leaves this launch path.
                        button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                        button.interactable = false;
                        button.gameObject.SetActive(false);
                    }
                }
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name != "Desktop List") continue;
                    var shortcut = transform.Find("Photo Gallery");
                    if (shortcut != null) shortcut.gameObject.SetActive(false);
                }
                foreach (var menu in root.GetComponentsInChildren<AeroStartMenu>(true))
                {
                    if (menu.programs == null) continue;
                    for (int i = 0; i < menu.programs.Length; i++)
                    {
                        var entry = menu.programs[i];
                        if (entry.window == null || entry.window.gameObject.scene != scene || entry.window.name != "Photo Gallery") continue;
                        // Start-menu search and Enter ignore entries with no window, even after ShowAllPrograms.
                        entry.window = null;
                        if (entry.row != null) entry.row.SetActive(false);
                        menu.programs[i] = entry;
                    }
                }
            }
        }

        /// <summary>
        /// Call after loading Main in Single mode.
        /// Returns false outside Play/Main, in test mode, or without its runtime/router.
        /// Repeated calls reuse installed services and never restart the local model.
        /// </summary>
        public static bool EnsureInitialized()
        {
            if (!Application.isPlaying) return false;
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) return false;
            var runtime = Object.FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.gameObject.scene != scene || runtime.TestMode) return false;
            if (runtime.GetComponent<ChapterOneDesktopRouter>() == null) return false;

            HideUnusedPhotoGallery(scene);
            runtime.EnsureInitialized();
            StoryBootstrap.Attach();
            XingGuangController.Attach();
            XgYyTalk.Attach();
            YYChatView.Attach();
            YYGirlfriend.Attach();
            MailInboxView.Attach();
            ZhongbaoHub.Attach();
            XgMarketRelay.Attach();
            XgAfterthoughtRelay.Attach();
            if (runtime.GetComponent<LocalLlm>() == null) LocalLlm.Attach();
            return true;
        }
    }
}
