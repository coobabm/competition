using UnityEngine;

namespace Michsky.DreamOS
{
    [DisallowMultipleComponent]
    public class WebsiteSetFullscreenDiv : MonoBehaviour
    {
        void Start()
        {
            RectTransform tempRect = gameObject.GetComponent<RectTransform>();
            tempRect.sizeDelta = new Vector2(transform.parent.GetComponent<RectTransform>().rect.width, transform.parent.GetComponent<RectTransform>().rect.height);
        }
    }
}