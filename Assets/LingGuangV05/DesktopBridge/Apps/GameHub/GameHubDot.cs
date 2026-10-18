using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// One dot under the game center slider (replaces DreamOS's GameHubSliderIndicatorItem; same GUID and fields):
    /// its bar fills while its game is shown, and clicking it jumps there.
    /// </summary>
    public class GameHubDot : MonoBehaviour
    {
        public Animator animator;
        public Image bar;
        public Button button;
        [HideInInspector] public int gameIndex;
    }
}
