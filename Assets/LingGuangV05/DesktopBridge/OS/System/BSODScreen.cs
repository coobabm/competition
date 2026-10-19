using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The text slots on the blue-screen prefab (2026-10-19). Replaces DreamOS's BSODScreen: same GUID and fields;
    /// <see cref="BSODManager"/> fills them in.
    /// </summary>
    public class BSODScreen : MonoBehaviour
    {
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI errorText;
    }
}
