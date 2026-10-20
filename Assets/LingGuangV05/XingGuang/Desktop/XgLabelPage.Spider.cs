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
            if (role == SpiderRole.Working) Speak("是。", "Yes.", "我。来。", "Me. Go.", "我来。", "On it.", 3);
            else if (was == SpiderRole.Working) Speak("……", "…", "停。", "Stop.", "好，我不碰了。", "All right. Hands off.", 3);
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
        public string SpiderBlocker() => Sim == null ? "host" : Mode != 0 ? "special" : shownTeachingEvaluation ? "teaching" : Sim.SpiderDragBlocker(Desk, Host);

        /// <summary>The note the spider says when it cannot answer.</summary>
        public void SpiderIdle(string blocker)
        {
            // Before it can talk it only lights 否; the reason shows in its corner as a plain note.
            switch (blocker)
            {
                case "ability": Speak("否。", "No.", "不会。", "Can't.", "这桌我还不会。", "I can't do this desk yet.", 4, "它还不会这桌。", "It can't do this desk yet."); break;
                case "teaching": Speak("……", "…", "看。", "Watching.", "我在看你教的内容。", "I am watching what you teach.", 4, "它在看你教。", "It is watching you teach."); break;
                case "review": Speak("否。", "No.", "先看。", "Review first.", "先把待复核的题看完。", "Finish the review inbox first.", 4, "先把待复核的题看完。", "Finish the review inbox first."); break;
                case "special": Speak("否。", "No.", "你来。", "Yours.", "这题得你自己来。", "This one is yours.", 4, "这题得你自己来。", "This one is yours."); break;
                case "host": Speak("否。", "No.", "不行。", "Can't.", "现在干不了活。", "I can't work right now.", 4, "现在干不了活。", "It can't work right now."); break;
                case "quiet": Speak("……", "…", "安静。", "Quiet.", "你让我安静。", "You asked me to keep quiet.", 4, "你让它安静了。", "You asked it to keep quiet."); break;
                default: Speak("否。", "No.", "没开。", "Closed.", "这里还没开放。", "This desk isn't open.", 4, "这里还没开放。", "This desk isn't open."); break;
            }
        }

        /// <summary>Use the answer actually presented on this card, including the trained teaching board's prediction.</summary>
        public bool SpiderPeek(out bool yesButton)
        {
            yesButton = false;
            if (Sim == null || WorkContext || Mode != 0 || DeskGameVisible || HandwritingVisible || shownTeachingEvaluation
                || !shownGuessValid || displayedSim != Sim || CurrentCard.id != displayedId || Sim.SpiderDragBlocker(Desk, Host) != null) return false;
            yesButton = shownGuess;
            return true;
        }

        /// <summary>The button the spider reaches for.</summary>
        public RectTransform Button(bool yesButton) => yesButton ? yes.rt : no.rt;

        /// <summary>
        /// The spider's hand comes down on a button: a hand answer for the player's account (XgSim.SpiderDragPress). A
        /// right one pays like one of the player's; a wrong one breaks the combo and, on the 算术 desk, costs the fine.
        /// </summary>
        public XgSpiderAnswer SpiderHandPress(bool yesButton)
        {
            if (!SpiderPeek(out bool displayedAnswer) || displayedAnswer != yesButton) return default;
            var card = CurrentCard;
            string truthWord = YesNoWord(card.truth);
            int multiplierBefore = Sim.StreakMultiplier;
            var r = Sim.SpiderDragPress(Desk, Host, yesButton);
            if (!r.accepted) return r;
            var button = yesButton ? yes.rt : no.rt;
            Vector2 at = Fx.At(paper);
            Fx.Knock(button, .12f);
            if (r.correct)
            {
                feedback.color = XgPalette.Good;
                feedback.text = "<color=#2F9E44>✓ " + T(("灵光标对了" .Replace("灵光", Sim.AiDisplayName)), ("LingGuang got it" .Replace("LingGuang", Sim.AiDisplayName))) + "  +¥" + N(r.pay, "0.00") + "</color>"
                    + (Sim.ComboMultiplier > 1 ? "  <color=#" + ColorUtility.ToHtmlStringRGB(XgJuice.StreakColor((int)Sim.ComboMultiplier)) + ">" + T("连对 ×", "Streak ×") + N(Sim.ComboMultiplier, "0") + "</color>" : "");
                Fx.Float(Fx.At(button, new Vector2(0, 40)), "+¥" + N(r.pay, "0.00"), XgPalette.Money, 22);
                Fx.Burst(at, 5, XgPalette.Gold, XgJuice.Shape.Yen, 200);
                // 摆渡众包's counter plays the combo sound; elsewhere it is played here. This is the hit itself.
                if (Sim.S.combo > 0 && !WorkContext) Fx.PlayCombo(Sim.S.combo, .7f);
                bool tierUp = Sim.StreakMultiplier > multiplierBefore;
                if (tierUp) Fx.StreakUp(at, at, button, view?.WalletChip ?? host.ComboChip, Sim.StreakMultiplier);
                else Fx.StreakHit(at, Sim.StreakMultiplier);
                if (Sim.S.combo > 0)
                    Fx.ComboHit(at, paper, button, host.ComboChip, Sim.S.combo, new Vector2(yesButton ? 1 : -1, .25f),
                        XgPalette.Tiers[Mathf.Clamp(Sim.StreakMultiplier - 1, 1, XgPalette.Tiers.Length - 1)], countLabel: !tierUp);
            }
            else
            {
                feedback.color = XgPalette.Bad;
                feedback.text = "<color=#D63031>× " + T(("灵光伸手答错了" .Replace("灵光", Sim.AiDisplayName)), ("LingGuang reached for the wrong one" .Replace("LingGuang", Sim.AiDisplayName)))
                    + (r.fine > 0 ? T(" · 扣你 ¥", " · it cost you ¥") + N(r.fine, "0.00") : T(" · 连击清零", " · combo reset"))
                    + T(" · 应该是「", " · it was ") + truthWord + T("」", "") + "</color>";
                if (r.fine > 0) Fx.Float(Fx.At(button, new Vector2(0, 40)), "−¥" + N(r.fine, "0.00"), XgPalette.Bad, 24);
                Wrong(at);
                Fx.StreakDrop(at, button, multiplierBefore, Sim.StreakMultiplier);
            }
            feedbackTimer = 2.5f;
            Remember(r.correct ? HistoryMark.SpiderRight : HistoryMark.SpiderWrong);
            SpiderReacts(r.correct, true);
            Refresh();
            return r;
        }

        // ───────────── the bubble ─────────────

        // What it murmurs while it works or watches, by how far it can talk (剧情修订 §一「它说话的阶梯」):
        // 是/否 only before 看图说词, single words with it, short sentences from 短句 on.
        static readonly string[] YesNoZh = { "……", "是。", "否。", "……是。" };
        static readonly string[] YesNoEn = { "…", "Yes.", "No.", "…yes." };
        static readonly string[] WordsZh = { "……", "进位。", "数字。", "七？", "记。", "累？" };
        static readonly string[] WordsEn = { "…", "Carry.", "Numbers.", "Seven?", "Noting.", "Tired?" };
        static readonly string[] LinesZh = { "……", "进位。", "它们都是数字。", "你停了 0.8 秒。", "为什么是 7？", "我在记。", "这一题，我也会了。", "……你累吗" };
        static readonly string[] LinesEn = { "…", "Carry the one.", "They are all numbers.", "You paused for 0.8 seconds.", "Why is it 7?", "I'm taking notes.", "I know this kind now.", "…are you tired?" };
        readonly System.Random chatter = new System.Random();

        /// <summary>0 = only 是/否 (before 看图说词), 1 = single words (看图说词), 2 = short sentences (短句 and later).</summary>
        int SpeechTier => Sim == null ? 2 : Sim.AbilitiesCount >= 4 ? 2 : Sim.AbilitiesCount >= 3 ? 1 : 0;

        /// <summary>
        /// The spider says the line for how far it can talk. It speaks from a bubble that pops up over it on the desktop;
        /// when it is not out on the desktop the bubble in its corner of this page pops up instead. <paramref name="noteZh"/>
        /// (a plain reason, not its voice) shows in the corner while it can only say 是/否 or single words.
        /// </summary>
        void Speak(string yesNoZh, string yesNoEn, string wordZh, string wordEn, string zh, string en, float seconds, string noteZh = null, string noteEn = null)
        {
            int tier = SpeechTier;
            string line = tier == 0 ? T(yesNoZh, yesNoEn) : tier == 1 ? T(wordZh, wordEn) : T(zh, en);
            string note = tier < 2 && noteZh != null ? T(noteZh, noteEn ?? noteZh) : null;
            SayLine(line, seconds, note);
        }

        /// <summary>The spider says something short (already in its own words) for a few seconds.</summary>
        public void Say(string zh, string en, float seconds) => SayLine(T(zh, en), seconds, null);

        void SayLine(string line, float seconds, string note)
        {
            // It keeps quiet while a story moment is on screen (StoryFocus).
            if (bubbleText == null || string.IsNullOrEmpty(line) || LingGuangV05.Core.StoryFocus.Active) return;
            var resident = XgResidentSpider.Instance;
            bool onDesktop = resident != null && resident.Speak(line, seconds);
            // The corner bubble: its words when it is not out on the desktop, or the plain note beside them.
            string corner = onDesktop ? (note != null ? "<color=#8A84A8><i>" + note + "</i></color>" : null) : line + (note != null ? "  <color=#8A84A8><i>" + note + "</i></color>" : "");
            if (corner == null) { HideCornerBubble(); return; }
            bubbleText.text = corner;
            bubbleTimer = seconds;
            bubblePop = 0;
            if (bubbleBack != null && !bubbleBack.gameObject.activeSelf) bubbleBack.gameObject.SetActive(true);
        }

        float bubblePop = -1;

        void HideCornerBubble()
        {
            bubbleTimer = 0; bubblePop = -1;
            if (bubbleBack != null && bubbleBack.gameObject.activeSelf) bubbleBack.gameObject.SetActive(false);
        }

        /// <summary>The corner bubble pops in (a quick overshoot) and hides again when its time is up.</summary>
        void TickBubble(float dt)
        {
            if (bubbleBack == null) return;
            if (bubbleTimer > 0)
            {
                bubbleTimer -= dt;
                if (bubbleTimer <= 0) { HideCornerBubble(); return; }
            }
            else if (bubbleBack.gameObject.activeSelf) { HideCornerBubble(); return; }
            if (bubblePop < 0) return;
            bubblePop += dt;
            float p = Mathf.Clamp01(bubblePop / .22f), b = 1.70158f;
            float k = Mathf.LerpUnclamped(.6f, 1, 1 + (b + 1) * Mathf.Pow(p - 1, 3) + b * Mathf.Pow(p - 1, 2));
            bubbleBack.rectTransform.localScale = new Vector3(k, k, 1);
            if (p >= 1) { bubbleBack.rectTransform.localScale = Vector3.one; bubblePop = -1; }
        }

        void SpiderReacts(bool right, bool working)
        {
            if (bubbleText == null) return;
            if (working)
            {
                if (!right) { Speak("否。", "No.", "错。", "Wrong.", "……我按错了。", "…I pressed the wrong one.", 3); return; }
                if (chatter.NextDouble() < .4) Murmur(4);
            }
            else
            {
                if (right) Speak("是。", "Yes.", "会。", "Know.", "这题。我会。", "This one. I know it.", 3);
                else Speak("……", "…", "不准。", "Unsure.", "……拿不准。", "…not sure.", 3);
            }
        }

        void Murmur(float seconds)
        {
            int tier = SpeechTier;
            var zh = tier == 0 ? YesNoZh : tier == 1 ? WordsZh : LinesZh;
            var en = tier == 0 ? YesNoEn : tier == 1 ? WordsEn : LinesEn;
            int i = chatter.Next(zh.Length);
            Say(zh[i], en[i], seconds);
        }

        /// <summary>A hand answer by the player: now and then the spider murmurs.</summary>
        void SpiderWatches()
        {
            if (bubbleText == null || Spider != SpiderRole.Home || !Sim.SpiderAround || chatter.NextDouble() > .22) return;
            Murmur(4);
        }

        // ───────────── keyboard ─────────────

        /// <summary>
        /// 1 and 2 press the answer buttons while the window is the front one and nothing is being typed. The captcha
        /// card has its own digit keys, so these are off while it shows.
        /// </summary>
        void TickKeys()
        {
            if (captchaCard != null && captchaCard.Visible) return;
            if (HandwritingVisible) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || !host.Visible || (host.Tab != "label" && host.Tab != "teach") || !FrontWindow() || Typing()) return;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) { if (yes.button.IsInteractable() && yes.button.gameObject.activeInHierarchy) CaptureAnswer(true)(); }
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) { if (no.button.IsInteractable() && no.button.gameObject.activeInHierarchy) CaptureAnswer(false)(); }
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
                var other = child.GetComponent<LingGuangV05.Desktop.OS.WindowManager>();
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
