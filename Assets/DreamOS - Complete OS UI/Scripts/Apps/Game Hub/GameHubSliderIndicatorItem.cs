using UnityEngine;
using UnityEngine.UI;

namespace Michsky.DreamOS
{
    public class GameHubSliderIndicatorItem : MonoBehaviour
    {
        public Animator animator;
        public Image bar;
        public Button button;
        [HideInInspector] public int gameIndex;
    }
}