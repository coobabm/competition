using UnityEngine;

namespace Michsky.DreamOS
{
    public class GameHubGameContent : MonoBehaviour
    {
        [SerializeField] private ImageFading windowTransition;
        [HideInInspector] public GameHubManager manager;
        [HideInInspector] public int gameIndex;

        void OnEnable()
        {
            if (windowTransition != null)
            {
                windowTransition.onFadeOutEnd.RemoveAllListeners();
                windowTransition.onFadeOutEnd.AddListener(delegate { windowTransition.gameObject.SetActive(false); });
                windowTransition.FadeOut();
            }
        }

        public void ExitGame()
        {
            if (windowTransition == null) { manager.ExitGame(); }
            else
            {
                windowTransition.onFadeInEnd.RemoveAllListeners();
                windowTransition.onFadeInEnd.AddListener(delegate { manager.ExitGame(); });
                windowTransition.FadeIn();
            }
        }

        public void MinimizeWindow()
        {
            manager.gameObject.GetComponent<WindowManager>().MinimizeWindow();
        }

        public void FullscreenWindow()
        {
            manager.gameObject.GetComponent<WindowManager>().FullscreenWindow();
        }
    }
}