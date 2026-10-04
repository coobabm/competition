using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Tray popups as 2016 Windows software showed them: a small window that slides up out of the taskbar in the
    /// bottom-right corner, with the sender's coloured title bar and logo, a close button, the message, an "OK"
    /// button and a greyed "don't remind me today" line. 360安全卫士 is green, 迅雷 and Windows blue, 摆渡众包 deep
    /// blue, 鲁大师 orange.
    /// One at a time; hovering keeps it open; it slides back down when its time is up.
    /// </summary>
    public sealed class TrayPopups : MonoBehaviour
    {
        public PrologueDesk desk;
        const float Width = 340, Height = 196, Taskbar = 54;
        readonly Queue<(string who, string text, float seconds)> queue = new Queue<(string, string, float)>();
        Coroutine running;
        RectTransform area;

        public void Enqueue(string who, string text, float seconds)
        {
            if (string.IsNullOrEmpty(text)) return;
            queue.Enqueue((who ?? "", text, Mathf.Max(3, seconds)));
            if (running == null) running = StartCoroutine(Run());
        }

        void OnDisable() { running = null; }

        sealed class Hover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public bool over;
            public void OnPointerEnter(PointerEventData e) { over = true; }
            public void OnPointerExit(PointerEventData e) { over = false; }
        }

        IEnumerator Run()
        {
            while (queue.Count > 0)
            {
                var (who, text, seconds) = queue.Dequeue();
                bool closed = false;
                var box = Build(who, text, () => closed = true, out var hover);
                // Slide up out of the taskbar.
                for (float t = 0; t < .35f; t += Time.unscaledDeltaTime) { box.anchoredPosition = new Vector2(0, Mathf.Lerp(-Height, 0, Ease(t / .35f))); yield return null; }
                box.anchoredPosition = Vector2.zero;
                for (float t = 0; t < seconds && !closed; t += hover.over ? 0 : Time.unscaledDeltaTime) yield return null;
                for (float t = 0; t < .3f && box != null; t += Time.unscaledDeltaTime) { box.anchoredPosition = new Vector2(0, Mathf.Lerp(0, -Height, t / .3f)); yield return null; }
                if (box != null) Destroy(box.gameObject);
                yield return new WaitForSecondsRealtime(.4f);
            }
            running = null;
        }

        static float Ease(float t) { t = Mathf.Clamp01(t) - 1; return 1 + t * t * t; }

        static string T(string zh, string en) => GameText.T(zh, en);

        static (Color bar, Color barLight, string mark, string button) Theme(string who)
        {
            if (who.Contains("360")) return (new Color32(36, 160, 62, 255), new Color32(76, 196, 96, 255), "360", T("知道了", "OK"));
            if (who.Contains("迅雷") || who.Contains("Thunder")) return (new Color32(26, 110, 205, 255), new Color32(70, 150, 235, 255), T("迅", "T"), T("知道了", "OK"));
            if (who.Contains("Windows")) return (new Color32(0, 90, 170, 255), new Color32(30, 125, 210, 255), "⊞", T("以后再说", "Later"));
            // 摆渡众包 (the crowd-labelling platform) wears the search giant's deep blue.
            if (who.Contains("摆渡众包") || who.Contains("Bodu Crowd")) return (new Color32(41, 50, 225, 255), new Color32(78, 110, 242, 255), T("众", "BC"), T("查看详情", "Details"));
            if (who.Contains("摆渡云") || who.Contains("Bodu Cloud")) return (new Color32(45, 127, 224, 255), new Color32(95, 165, 245, 255), T("云", "☁"), T("开通会员", "Go premium"));
            if (who.Contains("淘货") || who.Contains("Taohuo")) return (new Color32(255, 80, 0, 255), new Color32(255, 130, 60, 255), T("淘", "T"), T("去看看", "Take a look"));
            if (who.Contains("喵鱼") || who.Contains("Miaoyu")) return (new Color32(230, 180, 0, 255), new Color32(255, 214, 60, 255), T("喵", "M"), T("去看看", "Take a look"));
            if (who.Contains("鲁大师") || who.Contains("Master Lu")) return (new Color32(232, 120, 20, 255), new Color32(250, 160, 60, 255), T("鲁", "Lu"), T("查看详情", "Details"));
            return (new Color32(70, 100, 140, 255), new Color32(110, 140, 180, 255), "i", T("确定", "OK"));
        }

        RectTransform Build(string who, string text, System.Action close, out Hover hover)
        {
            if (area == null)
            {
                // Clips the slide so the popup comes out of the taskbar's top edge, not over it.
                area = PrologueDesk.Rect("Tray Popup Area", transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-Width - 10, Taskbar), new Vector2(-10, Taskbar + Height + 8));
                area.gameObject.AddComponent<RectMask2D>();
            }
            area.SetAsLastSibling();
            var theme = Theme(who);
            var box = PrologueDesk.Rect("Tray Popup", area, Vector2.zero, new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, Height));
            box.pivot = new Vector2(.5f, 0);
            hover = box.gameObject.AddComponent<Hover>();
            PrologueDesk.Fill(box, new Color32(150, 160, 170, 255));
            var inner = PrologueDesk.Rect("Inner", box, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
            PrologueDesk.Fill(inner, Color.white, false);

            // Title bar: two-tone like the 2016 skins, logo badge, sender, close.
            var bar = PrologueDesk.Rect("Bar", inner, new Vector2(0, 1), Vector2.one, new Vector2(0, -34), Vector2.zero);
            PrologueDesk.Fill(bar, theme.bar, false);
            PrologueDesk.Fill(PrologueDesk.Rect("Shine", bar, new Vector2(0, .5f), Vector2.one, Vector2.zero, Vector2.zero), theme.barLight, false);
            var badge = PrologueDesk.Rect("Logo", bar, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(8, -12), new Vector2(32, 12));
            var badgeImg = PrologueDesk.Fill(badge, Color.white, false); badgeImg.sprite = PrologueDesk.Circle();
            var mark = desk.Text(PrologueDesk.Rect("Mark", badge, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "<b>" + theme.mark + "</b>", theme.mark.Length > 2 ? 9 : 13, theme.bar, TextAlignmentOptions.Center);
            mark.textWrappingMode = TextWrappingModes.NoWrap;
            var title = desk.Text(PrologueDesk.Rect("Who", bar, Vector2.zero, Vector2.one, new Vector2(40, 0), new Vector2(-36, 0)), "<b>" + who + "</b>", 15, Color.white, TextAlignmentOptions.MidlineLeft);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            var x = PrologueDesk.Rect("Close", bar, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-32, -13), new Vector2(-6, 13));
            var xImg = PrologueDesk.Fill(x, new Color(1, 1, 1, 0));
            var xButton = x.gameObject.AddComponent<Button>(); xButton.targetGraphic = xImg;
            var colors = xButton.colors; colors.highlightedColor = new Color(1, 1, 1, .25f); colors.pressedColor = new Color(0, 0, 0, .2f); xButton.colors = colors;
            xButton.onClick.AddListener(() => close());
            desk.Text(PrologueDesk.Rect("X", x, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "×", 20, Color.white, TextAlignmentOptions.Center);

            // Message.
            desk.Text(PrologueDesk.Rect("Text", inner, Vector2.zero, Vector2.one, new Vector2(16, 52), new Vector2(-16, -44)), text, 16, PrologueDesk.Ink, TextAlignmentOptions.TopLeft);

            // Footer: greyed opt-out on the left, the action button on the right.
            PrologueDesk.Fill(PrologueDesk.Rect("Rule", inner, Vector2.zero, new Vector2(1, 0), new Vector2(10, 44), new Vector2(-10, 45)), new Color32(228, 230, 234, 255), false);
            desk.Text(PrologueDesk.Rect("Mute", inner, Vector2.zero, new Vector2(.6f, 0), new Vector2(14, 8), new Vector2(0, 40)), "☐ " + T("今日不再提醒", "Don't remind me today"), 12, new Color32(150, 150, 150, 255), TextAlignmentOptions.MidlineLeft);
            var ok = PrologueDesk.Rect("Action", inner, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-104, 9), new Vector2(-12, 37));
            var okImg = PrologueDesk.Fill(ok, theme.bar);
            var okButton = ok.gameObject.AddComponent<Button>(); okButton.targetGraphic = okImg;
            okButton.onClick.AddListener(() => close());
            desk.Text(PrologueDesk.Rect("Label", ok, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), theme.button, 14, Color.white, TextAlignmentOptions.Center);
            box.anchoredPosition = new Vector2(0, -Height);
            return box;
        }
    }
}
