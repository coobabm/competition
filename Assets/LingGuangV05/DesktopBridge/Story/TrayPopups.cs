using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LingGuangV05.Core;
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
        sealed class Notice
        {
            public string who, text;
            public float seconds;
            public System.Action open;
            public System.Func<bool> valid, held;
        }
        readonly Queue<Notice> queue = new Queue<Notice>();
        Coroutine running;
        RectTransform area;
        public bool IsBusy => running != null || queue.Count > 0;

        public void Enqueue(string who, string text, float seconds)
        {
            if (string.IsNullOrEmpty(text)) return;
            queue.Enqueue(new Notice { who = who ?? "", text = text, seconds = Mathf.Max(3, seconds) });
            if (running == null) running = StartCoroutine(Run());
        }

        /// <summary>Message notices are invalidated on save swap and hidden/paused during a cutscene.</summary>
        public void EnqueueNotification(string who, string text, float seconds, System.Action open,
            System.Func<bool> valid, System.Func<bool> held)
        {
            if (string.IsNullOrEmpty(text)) return;
            queue.Enqueue(new Notice { who = who ?? "", text = text, seconds = Mathf.Max(3, seconds), open = open, valid = valid, held = held });
            if (running == null) running = StartCoroutine(Run());
        }

        void OnEnable() { ClearTransient(); }
        void OnDisable() { ClearTransient(); }

        void ClearTransient()
        {
            StopAllCoroutines();
            running = null;
            queue.Clear();
            // A popup's buttons close over its coroutine. After reload they cannot dismiss old UI.
            // Remove only this controller's transient area, not other apps on the shared desktop layer.
            for (int i = transform.childCount - 1; i >= 0; i--)
                if (transform.GetChild(i).name == "Tray Popup Area")
                    PrologueDesk.RemoveTransient(transform.GetChild(i).gameObject);
            area = null;
        }

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
                var item = queue.Dequeue();
                if (item.valid != null && !item.valid()) continue;
                bool closed = false;
                var box = Build(item.who, item.text, () => closed = true, () =>
                {
                    closed = true;
                    if (item.valid == null || item.valid()) item.open?.Invoke();
                }, item.open != null, out var hover);
                LingGuangV05.Desktop.Media.DesktopNotifications.Ping();
                // Slide up out of the taskbar.
                for (float t = 0; t < .35f && Valid(item);)
                {
                    if (Ready(item, box)) { box.anchoredPosition = new Vector2(0, Mathf.Lerp(-Height, 0, Ease(t / .35f))); t += Time.unscaledDeltaTime; }
                    yield return null;
                }
                box.anchoredPosition = Vector2.zero;
                for (float t = 0; t < item.seconds && !closed && Valid(item);)
                { if (Ready(item, box) && !hover.over) t += Time.unscaledDeltaTime; yield return null; }
                for (float t = 0; t < .3f && box != null && Valid(item);)
                {
                    if (Ready(item, box)) { box.anchoredPosition = new Vector2(0, Mathf.Lerp(0, -Height, t / .3f)); t += Time.unscaledDeltaTime; }
                    yield return null;
                }
                if (box != null) Destroy(box.gameObject);
                yield return new WaitForSecondsRealtime(.4f);
            }
            running = null;
        }

        static bool Valid(Notice item) => item.valid == null || item.valid();
        static bool Ready(Notice item, RectTransform box)
        {
            bool ready = item.held == null || !item.held();
            if (box.gameObject.activeSelf != ready) box.gameObject.SetActive(ready);
            return ready;
        }

        static float Ease(float t) { t = Mathf.Clamp01(t) - 1; return 1 + t * t * t; }

        static string T(string zh, string en) => GameText.T(zh, en);

        static (Color bar, Color barLight, string mark, string button) Theme(string who)
        {
            if (who.StartsWith("YY")) return (new Color32(33, 144, 197, 255), new Color32(78, 185, 226, 255), "YY", Lang.T("确定"));
            if (who.Contains("巨信") || who.Contains("Juxin")) return (new Color32(26, 173, 25, 255), new Color32(71, 197, 79, 255), Lang.T("信"), Lang.T("确定"));
            if (who.Contains("贴吧") || who.Contains("Tieba")) return (new Color32(44, 100, 204, 255), new Color32(88, 145, 239, 255), Lang.T("贴"), Lang.T("确定"));
            if (who.Contains("360")) return (new Color32(36, 160, 62, 255), new Color32(76, 196, 96, 255), "360", Lang.T("知道了"));
            if (who.Contains("迅雷") || who.Contains("Thunder")) return (new Color32(26, 110, 205, 255), new Color32(70, 150, 235, 255), Lang.T("迅"), Lang.T("知道了"));
            if (who.Contains("Windows")) return (new Color32(0, 90, 170, 255), new Color32(30, 125, 210, 255), "⊞", Lang.T("以后再说"));
            // 摆渡众包 (the crowd-labelling platform) wears the search giant's deep blue.
            if (who.Contains("摆渡众包") || who.Contains("Bodu Crowd")) return (new Color32(41, 50, 225, 255), new Color32(78, 110, 242, 255), T("众", "BC"), Lang.T("查看详情"));
            if (who.Contains("摆渡云") || who.Contains("Bodu Cloud")) return (new Color32(45, 127, 224, 255), new Color32(95, 165, 245, 255), Lang.T("云"), Lang.T("开通会员"));
            if (who.Contains("淘货") || who.Contains("Taohuo")) return (new Color32(255, 80, 0, 255), new Color32(255, 130, 60, 255), Lang.T("淘"), Lang.T("去看看"));
            if (who.Contains("喵鱼") || who.Contains("Miaoyu")) return (new Color32(230, 180, 0, 255), new Color32(255, 214, 60, 255), Lang.T("喵"), Lang.T("去看看"));
            if (who.Contains("鲁大师") || who.Contains("Master Lu")) return (new Color32(232, 120, 20, 255), new Color32(250, 160, 60, 255), Lang.T("鲁"), Lang.T("查看详情"));
            return (new Color32(70, 100, 140, 255), new Color32(110, 140, 180, 255), "i", Lang.T("确定"));
        }

        RectTransform Build(string who, string text, System.Action close, System.Action open, bool canOpen, out Hover hover)
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
            var title = desk.Text(PrologueDesk.Rect("Who", bar, Vector2.zero, Vector2.one, new Vector2(40, 0), new Vector2(-36, 0)), who, 15, Color.white, TextAlignmentOptions.MidlineLeft);
            title.richText = false;
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            var x = PrologueDesk.Rect("Close", bar, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-32, -13), new Vector2(-6, 13));
            var xImg = PrologueDesk.Fill(x, new Color(1, 1, 1, 0));
            var xButton = x.gameObject.AddComponent<Button>(); xButton.targetGraphic = xImg;
            var colors = xButton.colors; colors.highlightedColor = new Color(1, 1, 1, .25f); colors.pressedColor = new Color(0, 0, 0, .2f); xButton.colors = colors;
            xButton.onClick.AddListener(() => close());
            desk.Text(PrologueDesk.Rect("X", x, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "×", 20, Color.white, TextAlignmentOptions.Center);

            // Message.
            var message = desk.Text(PrologueDesk.Rect("Text", inner, Vector2.zero, Vector2.one, new Vector2(16, 52), new Vector2(-16, -44)), text, 16, PrologueDesk.Ink, TextAlignmentOptions.TopLeft);
            message.richText = false;
            message.overflowMode = TextOverflowModes.Ellipsis;

            // Footer: greyed opt-out on the left, the action button on the right.
            PrologueDesk.Fill(PrologueDesk.Rect("Rule", inner, Vector2.zero, new Vector2(1, 0), new Vector2(10, 44), new Vector2(-10, 45)), new Color32(228, 230, 234, 255), false);
            desk.Text(PrologueDesk.Rect("Mute", inner, Vector2.zero, new Vector2(.6f, 0), new Vector2(14, 8), new Vector2(0, 40)), "☐ " + Lang.T("今日不再提醒"), 12, new Color32(150, 150, 150, 255), TextAlignmentOptions.MidlineLeft);
            var ok = PrologueDesk.Rect("Action", inner, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-104, 9), new Vector2(-12, 37));
            var okImg = PrologueDesk.Fill(ok, theme.bar);
            var okButton = ok.gameObject.AddComponent<Button>(); okButton.targetGraphic = okImg;
            okButton.onClick.AddListener(() => open());
            desk.Text(PrologueDesk.Rect("Label", ok, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), canOpen ? Lang.T("查看") : theme.button, 14, Color.white, TextAlignmentOptions.Center);
            box.anchoredPosition = new Vector2(0, -Height);
            return box;
        }
    }
}
