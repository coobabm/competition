using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class WidgetLibraryItem : MonoBehaviour
    {
        public SwitchManager itemSwitch;
        public Image iconImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        [HideInInspector] public WidgetManager manager;
        [HideInInspector] public int widgetIndex;
    }
}