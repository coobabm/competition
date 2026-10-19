using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// One row of the widget library: icon, title, description and the on/off switch for a widget.
    /// <see cref="WidgetManager"/> fills it in and wires the switch.
    /// Replaces DreamOS's WidgetLibraryItem (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
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
