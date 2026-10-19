using System.Collections;
using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The list of standard notifications in the notification center. Swaps between the "Clear all" panel and the
    /// "no notifications" panel depending on whether any card is left.
    /// Replaces DreamOS's NotificationItemParent (2026-10-19): same GUID, serialized fields and public API.
    /// </summary>
    public class NotificationItemParent : MonoBehaviour
    {
        [Header("Resources")]
        public UIPopup clearPanel;
        public UIPopup emptyPanel;

        bool clearInProgress = false;
        bool isCleared = true;

        void OnEnable()
        {
            clearInProgress = false;
            UpdateState();
        }

        public void UpdateState()
        {
            if (!isCleared && transform.childCount == 0)
            {
                ShowEmpty();
                isCleared = true;
            }
            else if (isCleared && transform.childCount > 0)
            {
                emptyPanel.PlayOut();
                clearPanel.PlayIn();
                isCleared = false;
            }
        }

        /// <summary>Closes every notification card; the empty panel returns once the last one is gone.</summary>
        public void Clear()
        {
            if (clearInProgress)
                return;

            clearInProgress = true;

            foreach (Transform child in transform)
            {
                NotificationItem item = child.GetComponent<NotificationItem>();
                if (item != null) { item.Close(); }
            }

            StartCoroutine(WaitForClear());
        }

        void ShowEmpty()
        {
            emptyPanel.PlayIn();
            clearPanel.PlayOut();
        }

        IEnumerator WaitForClear()
        {
            while (transform.childCount > 0) { yield return null; }
            ShowEmpty();
            clearInProgress = false;
        }
    }
}
