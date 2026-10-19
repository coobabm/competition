using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// One expanding click ripple: grows from nothing to maxSize while its colour eases to the transition colour,
    /// then destroys itself (and switches off its parent when it was the last ripple). Replaces DreamOS's Ripple
    /// (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    public class Ripple : MonoBehaviour
    {
        public bool unscaledTime = false;
        public float speed;
        public float maxSize;
        public Color startColor;
        public Color transitionColor;

        Image tempImg;

        void Start()
        {
            transform.localScale = new Vector3(0f, 0f, 0f);
            tempImg = GetComponent<Image>();
            tempImg.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a);
        }

        void Update()
        {
            float t = (unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * speed;

            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(maxSize, maxSize, maxSize), t);
            tempImg.color = Color.Lerp(tempImg.color, new Color(transitionColor.r, transitionColor.g, transitionColor.b, transitionColor.a), t);

            if (transform.localScale.x >= maxSize * 0.998)
            {
                if (transform.parent.childCount == 1)
                    transform.parent.gameObject.SetActive(false);

                Destroy(gameObject);
            }
        }
    }
}
