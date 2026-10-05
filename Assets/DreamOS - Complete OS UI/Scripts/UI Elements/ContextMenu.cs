using UnityEngine;

namespace Michsky.DreamOS
{
    public class ContextMenu : MonoBehaviour
    {
        public Animator animator;
        public RectTransform mainRect;
        public RectTransform contentRect;
        public UIBlur blur;

        [HideInInspector] public ContextMenuManager manager;

        public void Close()
        {
            manager.Close();
        }
    }
}