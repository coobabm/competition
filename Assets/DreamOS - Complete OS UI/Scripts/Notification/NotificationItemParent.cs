using System.Collections;
using UnityEngine;

namespace Michsky.DreamOS
{
    public class NotificationItemParent : MonoBehaviour
    {
        [Header("Resources")]
        public UIPopup clearPanel;
        public UIPopup emptyPanel;

        // Helpers
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
                emptyPanel.PlayIn();
                clearPanel.PlayOut();
                isCleared = true;
            }

            else if (isCleared && transform.childCount > 0)
            {
                emptyPanel.PlayOut();
                clearPanel.PlayIn();
                isCleared = false;
            }
        }

        public void Clear()
        {
            if (clearInProgress)
                return;

            clearInProgress = true;

            foreach (Transform child in transform)
            {
                NotificationItem nItem = child.GetComponent<NotificationItem>();
                if (nItem != null) { nItem.Close(); }
            }

            StartCoroutine(WaitForClear());
        }

        IEnumerator WaitForClear()
        {
            while (transform.childCount > 0) { yield return null; }
            emptyPanel.PlayIn();
            clearPanel.PlayOut();
            clearInProgress = false;
        }
    }
}