using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Shows an "empty" placeholder while a container has no children (empty download list, no notifications).
    /// Replaces DreamOS's EmptyChildIndicator (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    public class EmptyChildIndicator : MonoBehaviour
    {
        [SerializeField] private Transform targetParent;
        [SerializeField] private GameObject targetIndicator;

        void OnEnable()
        {
            CheckForParent();
        }

        public void CheckForParent()
        {
            targetIndicator.SetActive(targetParent.childCount == 0);
        }
    }
}
