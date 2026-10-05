using System;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop
{
    /// <summary>
    /// Hover help: rest the pointer on a control for a moment and a small dark note explains it, so a newcomer can
    /// learn the screen by looking. The note follows the pointer, stays inside the screen and never takes clicks.
    /// Works through the monitor's input relay like any other pointer event.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerDownHandler
    {
        public Func<string> Text;
        const float Delay = .35f;

        /// <summary>Adds (or replaces) the hover note of a control. Text is read each time it shows, so it follows the language.</summary>
        public static UiTip Add(Component target, Func<string> text)
        {
            if (target == null) return null;
            var tip = target.GetComponent<UiTip>() ?? target.gameObject.AddComponent<UiTip>();
            tip.Text = text;
            // Pointer events need a raycast target: the control's own background is enough.
            var own = target.GetComponent<Graphic>();
            if (own != null) own.raycastTarget = true;
            return tip;
        }

        public static UiTip Add(Component target, string zh, string en) => Add(target, () => GameText.T(zh, en));

        public void OnPointerEnter(PointerEventData e) { Layer()?.Begin(this, e); }
        public void OnPointerMove(PointerEventData e) { Layer()?.Move(this, e); }
        public void OnPointerExit(PointerEventData e) { Layer()?.End(this); }
        public void OnPointerDown(PointerEventData e) { Layer()?.End(this); }
        void OnDisable() { if (layer != null) layer.End(this); }

        UiTipLayer layer;
        UiTipLayer Layer()
        {
            if (layer != null) return layer;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return null;
            canvas = canvas.rootCanvas;
            layer = canvas.GetComponent<UiTipLayer>() ?? canvas.gameObject.AddComponent<UiTipLayer>();
            return layer;
        }
    }

    /// <summary>The one hover note of a canvas, drawn above everything on it.</summary>
    public sealed class UiTipLayer : MonoBehaviour
    {
        RectTransform box;
        TMP_Text label;
        UiTip current;
        Vector2 pointer;
        Camera eventCamera;
        float showAt = -1;

        void EnsureBox()
        {
            if (box != null) return;
            box = PrologueDesk.Rect("Hover Tip", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            box.pivot = new Vector2(0, 1);
            var bg = PrologueDesk.Fill(box, new Color32(28, 34, 48, 242), false);
            box.gameObject.AddComponent<Outline>().effectColor = new Color32(120, 140, 175, 200);
            var group = box.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            var text = PrologueDesk.Rect("Text", box, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8));
            label = text.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = PrologueDesk.CjkFont(); label.fontSize = 17; label.color = Color.white; label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal; label.alignment = TextAlignmentOptions.TopLeft; label.richText = true;
            bg.raycastTarget = false;
            box.gameObject.SetActive(false);
        }

        public void Begin(UiTip tip, PointerEventData e)
        {
            current = tip; showAt = Time.unscaledTime + .35f;
            Move(tip, e);
        }

        public void Move(UiTip tip, PointerEventData e)
        {
            if (tip != current) return;
            pointer = e.position;
            eventCamera = e.enterEventCamera != null ? e.enterEventCamera : e.pressEventCamera;
            if (box != null && box.gameObject.activeSelf) Place();
        }

        public void End(UiTip tip)
        {
            if (tip != current) return;
            current = null; showAt = -1;
            if (box != null) box.gameObject.SetActive(false);
        }

        void Update()
        {
            if (current == null || showAt < 0 || Time.unscaledTime < showAt) return;
            showAt = -1;
            string text = current.Text != null ? current.Text() : null;
            if (string.IsNullOrEmpty(text) || !current.isActiveAndEnabled) return;
            EnsureBox();
            label.text = text;
            // Wrap at about 380 units, then shrink the box to the text.
            var size = label.GetPreferredValues(text, 380, 0);
            float w = Mathf.Min(380, size.x) + 24, h = label.GetPreferredValues(text, w - 24, 0).y + 16;
            box.sizeDelta = new Vector2(w, h);
            box.gameObject.SetActive(true);
            box.SetAsLastSibling();
            Place();
        }

        void Place()
        {
            var canvas = (RectTransform)transform;
            var c = GetComponent<Canvas>();
            var cam = c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? (c.worldCamera != null ? c.worldCamera : eventCamera) : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, pointer, cam, out var local)) return;
            // From bottom-left origin; put the note below-right of the pointer and keep it on screen.
            var r = canvas.rect;
            Vector2 p = local - r.min + new Vector2(18, -22);
            if (p.x + box.sizeDelta.x > r.width - 6) p.x = local.x - r.min.x - box.sizeDelta.x - 12;
            if (p.y - box.sizeDelta.y < 6) p.y = local.y - r.min.y + box.sizeDelta.y + 12;
            box.anchoredPosition = p;
        }
    }
}
