using System;
using LingGuangV05.Desktop.Zhongbao;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The page's side of the dragged spider (<see cref="XgResidentSpider"/>): where its workspace is, which button it
    /// reaches for, the hand answer it presses, its corner and speech bubble, and the keyboard shortcuts (1 / 2) of
    /// the answer buttons. The rules are <see cref="XgSim.SpiderDragAnswer"/>: a hand answer that can cost money.
    /// </summary>
    public sealed partial class XgLabelPage
    {
        /// <summary>Where the spider is: in its corner (watching), carried by the pointer, or answering beside the card.</summary>
        public enum SpiderRole { Home, Held, Working }

        public SpiderRole Spider { get; private set; }

        /// <summary>The whole workspace box: dropping the spider anywhere on it puts it to work.</summary>
        public RectTransform Workspace => workspace;

        Image dropOutline;

        /// <summary>The resident spider tells the page what it is doing.</summary>
        public void SetSpiderRole(SpiderRole role)
        {
            if (Spider == role) return;
            var was = Spider;
            Spider = role;
            if (role == SpiderRole.Working) Say("我来。", "On it.", 3);
            else if (was == SpiderRole.Working) Say("好，我不碰了。", "All right. Hands off.", 3);
            if (role != SpiderRole.Held) SetDropHint(false);
            if (root != null && Sim != null) RefreshAi();
        }

        /// <summary>A dashed rim round the workspace while the carried spider is over it.</summary>
        public void SetDropHint(bool over)
        {
            if (workspace == null) return;
            if (dropOutline == null)
            {
                var rt = Rect("DropHint", workspace, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
                dropOutline = Panel(rt, new Color(ZhongbaoSkin.Spider.r, ZhongbaoSkin.Spider.g, ZhongbaoSkin.Spider.b, .10f));
                dropOutline.raycastTarget = false;
                var rim = rt.gameObject.AddComponent<UnityEngine.UI.Outline>();
                rim.effectColor = ZhongbaoSkin.Spider; rim.effectDistance = new Vector2(2, -2);
            }
            if (dropOutline.gameObject.activeSelf != over) dropOutline.gameObject.SetActive(over);
        }

        /// <summary>Why the spider cannot answer this card (see XgSim.SpiderAnswerBlocker); null when it can.</summary>
        public string SpiderBlocker() => Sim == null ? "host" : Mode != 0 ? "special" : Sim.SpiderDragBlocker(Desk, Host);

        /// <summary>The note the spider says when it cannot answer.</summary>
        public void SpiderIdle(string blocker)
        {
            switch (blocker)
            {
                case "ability": Say("这桌我还不会。", "I can't do this desk yet.", 4); break;
                case "review": Say("先把待复核的题看完。", "Finish the review inbox first.", 4); break;
                case "special": Say("这题得你自己来。", "This one is yours.", 4); break;
                case "host": Say("现在干不了活。", "I can't work right now.", 4); break;
                case "quiet": Say("你让我安静。", "You asked me to keep quiet.", 4); break;
                default: Say("这里还没开放。", "This desk isn't open.", 4); break;
            }
        }

        /// <summary>What the spider would press for the card on screen (the checkpoint's guess); false when it cannot.</summary>
        public bool SpiderPeek(out bool yesButton)
        {
            yesButton = false;
            if (Sim == null || Mode != 0 || displayedSim != Sim || CurrentCard.id != displayedId) return false;
            return Sim.SpiderDragPeek(Desk, Host, out yesButton);
        }

        /// <summary>The button the spider reaches for.</summary>
        public RectTransform Button(bool yesButton) => yesButton ? yes.rt : no.rt;

        /// <summary>
        /// The spider's hand comes down on a button: a hand answer for the player's account (XgSim.SpiderDragPress). A
        /// right one pays like one of the player's; a wrong one breaks the combo and, on the 算术 desk, costs the fine.
        /// </summary>
        public XgSpiderAnswer SpiderHandPress(bool yesButton)
        {
            if (Sim == null || Mode != 0 || displayedSim != Sim || CurrentCard.id != displayedId) return default;
            var card = CurrentCard;
            string truthWord = YesNoWord(card.truth);
            var r = Sim.SpiderDragPress(Desk, Host, yesButton);
            if (!r.accepted) return r;
            var button = yesButton ? yes.rt : no.rt;
            Vector2 at = Fx.At(paper);
            Fx.Knock(button, .12f);
            if (r.correct)
            {
                feedback.color = XgPalette.Good;
                feedback.text = "<color=#2F9E44>✓ " + T("灵光标对了", "LingGuang got it") + "  +¥" + N(r.pay, "0.00") + "</color>"
                    + (Sim.S.combo > 1 ? "  <color=#7A5AF8>" + T("连击 ×", "Combo ×") + N(Sim.ComboMultiplier, "0.00") + "</color>" : "");
                Fx.Float(Fx.At(button, new Vector2(0, 40)), "+¥" + N(r.pay, "0.00"), XgPalette.Money, 22);
                Fx.Burst(at, 5, XgPalette.Gold, XgJuice.Shape.Yen, 200);
                Fx.Play(XgJuice.Sfx.Id.Ding, 1 + Mathf.Min(1, Sim.S.combo * .025f), .7f);
            }
            else
            {
                feedback.color = XgPalette.Bad;
                feedback.text = "<color=#D63031>× " + T("灵光伸手答错了", "LingGuang reached for the wrong one")
                    + (r.fine > 0 ? T(" · 扣你 ¥", " · it cost you ¥") + N(r.fine, "0.00") : T(" · 连击清零", " · combo reset"))
                    + T(" · 应该是「", " · it was ") + truthWord + T("」", "") + "</color>";
                if (r.fine > 0) Fx.Float(Fx.At(button, new Vector2(0, 40)), "−¥" + N(r.fine, "0.00"), XgPalette.Bad, 24);
                Wrong(at);
            }
            feedbackTimer = 2.5f;
            Remember(r.correct ? HistoryMark.SpiderRight : HistoryMark.SpiderWrong);
            SpiderReacts(r.correct, true);
            Refresh();
            return r;
        }

        // ───────────── the bubble ─────────────

        static readonly string[] LinesZh = { "……", "进位。", "它们都是数字。", "你停了 0.8 秒。", "为什么是 7？", "我在记。", "这一题，我也会了。", "……你累吗" };
        static readonly string[] LinesEn = { "…", "Carry the one.", "They are all numbers.", "You paused for 0.8 seconds.", "Why is it 7?", "I'm taking notes.", "I know this kind now.", "…are you tired?" };
        readonly System.Random chatter = new System.Random();

        /// <summary>The spider says something short in its bubble for a few seconds.</summary>
        public void Say(string zh, string en, float seconds)
        {
            if (bubbleText == null) return;
            bubbleText.text = T(zh, en);
            bubbleTimer = seconds;
        }

        void SpiderReacts(bool right, bool working)
        {
            if (bubbleText == null) return;
            if (working)
            {
                if (!right) { Say("……我按错了。", "…I pressed the wrong one.", 3); return; }
                if (chatter.NextDouble() < .4) { int i = chatter.Next(LinesZh.Length); Say(LinesZh[i], LinesEn[i], 4); }
            }
            else
            {
                if (right) Say("这题。我会。", "This one. I know it.", 3);
                else Say("……拿不准。", "…not sure.", 3);
            }
        }

        /// <summary>A hand answer by the player: now and then the spider murmurs.</summary>
        void SpiderWatches()
        {
            if (bubbleText == null || Spider != SpiderRole.Home || !Sim.SpiderAround || chatter.NextDouble() > .22) return;
            int i = chatter.Next(LinesZh.Length);
            Say(LinesZh[i], LinesEn[i], 4);
        }

        // ───────────── keyboard ─────────────

        /// <summary>
        /// 1 and 2 press the answer buttons while the window is the front one and nothing is being typed. The captcha
        /// card has its own digit keys, so these are off while it shows.
        /// </summary>
        void TickKeys()
        {
            if (captchaCard != null && captchaCard.Visible) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || !host.Visible || host.Tab != "label" || !FrontWindow() || Typing()) return;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) { if (yes.button.interactable && yes.button.gameObject.activeInHierarchy) yes.button.onClick.Invoke(); }
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) { if (no.button.interactable && no.button.gameObject.activeInHierarchy) no.button.onClick.Invoke(); }
        }

        bool FrontWindow()
        {
            var w = ui.window;
            if (w == null || w.transform.parent == null) return true;
            var parent = w.transform.parent;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (!child.gameObject.activeInHierarchy) continue;
                var other = child.GetComponent<Michsky.DreamOS.WindowManager>();
                if (other == null || !other.isOn) continue;
                return other == w;
            }
            return true;
        }

        static bool Typing()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null && (selected.GetComponent<TMP_InputField>() != null || selected.GetComponent<InputField>() != null);
        }
    }
}
