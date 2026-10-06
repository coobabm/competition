using LingGuangV05.Desktop.Juxin;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Mail;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            runtime.EnsureInitialized();
            StoryBootstrap.Attach();
            XingGuangController.Attach();
            YYChatView.Attach();
            YYGirlfriend.Attach();
            MailInboxView.Attach();
            JuxinHub.Attach();
            XgMarketRelay.Attach();
            XgAfterthoughtRelay.Attach();
            if (runtime.GetComponent<LocalLlm>() == null) LocalLlm.Attach();
            return true;
        }
    }
}
