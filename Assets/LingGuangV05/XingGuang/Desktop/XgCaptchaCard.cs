using System;
using System.Text;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 摆渡众包's captcha card on the labelling page: four handwritten digits drawn by <see cref="XgDigitGraphic"/>
    /// at its messiest, an on-screen keypad (0–9 and backspace) and the keyboard's digit keys. The fourth digit
    /// submits. It covers the card and the answer buttons while a captcha is waiting. Presentation only.
    /// </summary>
    public sealed class XgCaptchaCard
    {
        const int MessyLevel = 5;
        static readonly Key[] DigitKeys = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        static readonly Key[] PadKeys = { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        public readonly RectTransform Root;
        readonly XgDigitGraphic[] digits = new XgDigitGraphic[XgSim.CaptchaLength];
        readonly TMP_Text timer, entry, hint;
        readonly StringBuilder input = new StringBuilder();
        readonly Func<XgSim> sim;
        readonly Action<bool> answered;
        string shownCode = "";
        int shownSeed = -1;

        public XgCaptchaCard(XgUi ui, RectTransform parent, float top, float height, Func<XgSim> sim, Action<bool> answered)
        {
            this.sim = sim; this.answered = answered;
            Root = Strip("QcCaptcha", parent, top, height, 14, 14); // the guide's "name:QcCaptcha" locator
            Panel(Root, new Color32(250, 251, 255, 255)); // also blocks clicks on the card underneath

            var header = Strip("Header", Root, 0, 34, 0, 0);
            Panel(header, new Color32(41, 50, 225, 255)).raycastTarget = false;
            var title = ui.Text(Rect("Title", header, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-90, 0)), "<b>" + Lang.T("摆渡众包 · 人机验证") + "</b>", 16, Color.white, TextAlignmentOptions.MidlineLeft);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            timer = ui.Text(Rect("Timer", header, new Vector2(1, 0), Vector2.one, new Vector2(-86, 0), new Vector2(-10, 0)), "", 16, Color.white, TextAlignmentOptions.MidlineRight);
            ui.Text(Strip("Prompt", Root, 42, 22, 10, 10), Lang.T("为确认您不是机器，请输入下图中的 4 个数字"), 14, XgPalette.Muted, TextAlignmentOptions.Center);

            const float box = 96, gap = 14;
            float start = -(XgSim.CaptchaLength * box + (XgSim.CaptchaLength - 1) * gap) / 2;
            for (int i = 0; i < XgSim.CaptchaLength; i++)
            {
                float x = start + i * (box + gap);
                var cell = Rect("Digit" + i, Root, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(x, -70 - box), new Vector2(x + box, -70));
                Panel(cell, XgPalette.Paper).raycastTarget = false;
                var g = Rect("Ink", cell, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -6)).gameObject.AddComponent<XgDigitGraphic>();
                g.color = new Color32(30, 34, 52, 255); g.raycastTarget = false;
                digits[i] = g;
            }
            entry = ui.Text(Strip("Entry", Root, 176, 40, 10, 10), "", 30, XgPalette.Ink, TextAlignmentOptions.Center);
            entry.fontStyle = FontStyles.Bold;

            // Keypad: 1–6 on the first row, 7–9, 0 and backspace on the second.
            int[] first = { 1, 2, 3, 4, 5, 6 }, second = { 7, 8, 9, 0, -1 };
            Keys(ui, first, 224);
            Keys(ui, second, 270);
            hint = ui.Text(Strip("Hint", Root, 318, 48, 14, 14), "", 13, XgPalette.Muted, TextAlignmentOptions.Top);
            Root.gameObject.SetActive(false);
        }

        void Keys(XgUi ui, int[] row, float y)
        {
            const float w = 54, h = 40, gap = 8;
            float start = -(row.Length * w + (row.Length - 1) * gap) / 2;
            for (int i = 0; i < row.Length; i++)
            {
                int d = row[i];
                var b = ui.Button(Root, d < 0 ? Lang.T("删") : d.ToString(), () => { if (d < 0) Backspace(); else Press(d); }, 20);
                b.rt.anchorMin = b.rt.anchorMax = new Vector2(.5f, 1);
                float x = start + i * (w + gap);
                b.rt.offsetMin = new Vector2(x, -y - h); b.rt.offsetMax = new Vector2(x + w, -y);
                b.label.fontStyle = FontStyles.Bold;
            }
        }

        public bool Visible => Root.gameObject.activeSelf;

        public void Show(bool on)
        {
            if (Root.gameObject.activeSelf == on) return;
            Root.gameObject.SetActive(on);
            if (on) Root.SetAsLastSibling();
            input.Clear();
        }

        /// <summary>Redraws the digits when a new captcha arrives, then the entry, clock and hint.</summary>
        public void Refresh()
        {
            var s = sim();
            if (s == null || !s.CaptchaPending) return;
            string code = s.CaptchaCode;
            if (code != shownCode || s.CaptchaSeed != shownSeed)
            {
                shownCode = code; shownSeed = s.CaptchaSeed; input.Clear();
                for (int i = 0; i < digits.Length && i < code.Length; i++) digits[i].Show(code[i] - '0', shownSeed + i * 7919, MessyLevel);
            }
            var shown = new StringBuilder();
            for (int i = 0; i < XgSim.CaptchaLength; i++) shown.Append(i < input.Length ? input[i] : '＿').Append(i < XgSim.CaptchaLength - 1 ? " " : "");
            entry.text = shown.ToString();
            double left = s.CaptchaSecondsLeft;
            timer.text = (left < 10 ? "<color=#FFD0D0>" : "") + XgSim.FreezeClock(left) + (left < 10 ? "</color>" : "");
            hint.text = s.Has(XgSim.CaptchaAutofillNode)
                ? Lang.T("验证码代填正在填写……")
                : Lang.T("也可以直接按键盘上的数字键。答错或超时：自动标注暂停 2 分钟，信用 −3。");
        }

        /// <summary>Per frame while shown: the clock, and keyboard digits when the label page has the user's attention.</summary>
        public void Tick(bool acceptKeys)
        {
            if (!Visible) return;
            var kb = acceptKeys ? Keyboard.current : null;
            if (kb != null)
            {
                for (int d = 0; d < 10; d++)
                    if (kb[DigitKeys[d]].wasPressedThisFrame || kb[PadKeys[d]].wasPressedThisFrame) Press(d);
                if (kb.backspaceKey.wasPressedThisFrame) Backspace();
            }
            Refresh();
        }

        void Press(int d)
        {
            var s = sim();
            if (s == null || !s.CaptchaPending || input.Length >= XgSim.CaptchaLength) return;
            input.Append((char)('0' + d));
            if (input.Length < XgSim.CaptchaLength) { Refresh(); return; }
            string answer = input.ToString();
            input.Clear();
            answered?.Invoke(s.SubmitCaptcha(answer));
        }

        void Backspace()
        {
            if (input.Length > 0) input.Length--;
            Refresh();
        }
    }
}
