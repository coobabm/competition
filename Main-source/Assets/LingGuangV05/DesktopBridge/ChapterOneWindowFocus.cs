using Michsky.DreamOS;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LingGuangV05.Desktop
{
    /// <summary>Uses the LCD relay's mapped event; never reads hardware coordinates.</summary>
    [DisallowMultipleComponent]
    public sealed class ChapterOneWindowFocus : MonoBehaviour, IPointerDownHandler
    {
        public WindowManager Window { get; set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Window != null && Window.isOn) Window.FocusToWindow();
        }
    }
}
