using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The popup instance a <see cref="ContextMenuManager"/> spawns on the canvas (2026-10-19). Replaces DreamOS's
    /// ContextMenu: same GUID, serialized fields and public API. It only carries the references the manager needs
    /// (animator, rects, optional blur) and forwards <see cref="Close"/> back to it.
    ///
    /// Inside this namespace the name resolves to this class; code elsewhere that also imports UnityEngine must
    /// qualify it, because UnityEngine.ContextMenu is an attribute with the same name.
    /// </summary>
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
