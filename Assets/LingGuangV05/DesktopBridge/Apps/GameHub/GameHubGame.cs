using LingGuangV05.Desktop.OS;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The root of a running game in the game center (replaces DreamOS's GameHubGameContent; same GUID and fields).
    /// Its buttons exit (behind a fade), minimise or fullscreen the game center window.
    /// </summary>
    public class GameHubGame : MonoBehaviour
    {
        [SerializeField] ImageFading windowTransition;
        [HideInInspector] public GameHubApp manager;
        [HideInInspector] public int gameIndex;

        void OnEnable()
        {
            if (windowTransition == null) return;
            windowTransition.onFadeOutEnd.RemoveAllListeners();
            windowTransition.onFadeOutEnd.AddListener(() => windowTransition.gameObject.SetActive(false));
            windowTransition.FadeOut();
        }

        public void ExitGame()
        {
            if (windowTransition == null) { manager.ExitGame(); return; }
            windowTransition.onFadeInEnd.RemoveAllListeners();
            windowTransition.onFadeInEnd.AddListener(manager.ExitGame);
            windowTransition.FadeIn();
        }

        public void MinimizeWindow() => manager.GetComponent<WindowManager>().MinimizeWindow();

        public void FullscreenWindow() => manager.GetComponent<WindowManager>().FullscreenWindow();
    }
}
