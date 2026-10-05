using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
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

        public void SetModuleIcon(string type)
        {
            foreach (Transform child in moduleTypeParent)
            {
                if (child.gameObject.name == type) { child.gameObject.SetActive(true); }
                else { child.gameObject.SetActive(false); }
            }
        }
    }
}