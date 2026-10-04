using UnityEngine;

namespace Michsky.DreamOS
{
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
            if (targetParent.childCount > 0) { targetIndicator.SetActive(false); }
            else { targetIndicator.SetActive(true); }
        }
    }
}