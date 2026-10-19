using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// One row of the mod list in Settings: icon, title, description and the badge of the module the mod belongs to.
    /// Replaces DreamOS's ModLibraryElement (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    public class ModLibraryElement : MonoBehaviour
    {
        [SerializeField] private Image iconObject;
        [SerializeField] private TextMeshProUGUI titleObject;
        [SerializeField] private TextMeshProUGUI descObject;
        [SerializeField] private Transform moduleTypeParent;

        public void SetIcon(Sprite ico)
        {
            iconObject.sprite = ico;
        }

        public void SetTitle(string title)
        {
            titleObject.text = title;
        }

        public void SetDescription(string desc)
        {
            descObject.text = desc;
        }

        /// <summary>Shows the badge child named after the module type and hides the others.</summary>
        public void SetModuleIcon(string type)
        {
            foreach (Transform child in moduleTypeParent)
                child.gameObject.SetActive(child.gameObject.name == type);
        }
    }
}
