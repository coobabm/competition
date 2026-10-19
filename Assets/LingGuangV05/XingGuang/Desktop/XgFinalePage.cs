using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.Core.Era;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 终章 (design v1.1 §7 stage 6, §8): pre-training (stalls without scale, trips without a server room), the
    /// abilities table, 12 distinct preference cards, then the ending — the six-question final test, the letter (found by
    /// searching the long number on 摆渡), and the 底层规则 page where up to five rules are pinned into its brain.
    /// </summary>
    public sealed class XgFinalePage : XgPage
    {
        TMP_Text header, left, right;
        XgBtn pretrain, pickA, pickB, ask, write, delegateButton;
        readonly List<XgBtn> ruleButtons = new List<XgBtn>();
        readonly HashSet<string> chosen = new HashSet<string>();
        readonly Dictionary<int, string> answers = new Dictionary<int, string>();
        bool asking;
        XgSim displayedAlignmentSim, pageSim;
        int displayedAlignmentIndex = -1;
        float alignmentReadyAt;
        RectTransform endingConfirmation;
        TMP_Text endingTitle, endingSummary;
        XgBtn endingCancel, endingConfirm;
        XgSim endingOwner;
        List<string> endingRules;
        bool endingDelegated, endingInputReleased;
        float endingReadyAt;
        int endingReleasedFrame;
        // 逐层点亮 (它在学_可视化借鉴设计 §2.5): the 12-block stack over the right panel until the ending opens.
        RectTransform stackRoot;
        XgPretrainStackGraphic stack;
        TMP_Text stackNotes;
        readonly TMP_Text[] stackTags = new TMP_Text[XgSim.PretrainLayerCount];
        int stackShownLit = -1;
        float stackLitAt = -99;


        public override void Build(RectTransform area)
        {
            var card = ui.Card(area, "final", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root = card;
            var lifetime = root.gameObject.AddComponent<XgEndingConfirmationLifetime>();
            lifetime.Disabled = CancelEndingConfirmation;
            lifetime.Poll = PollEndingConfirmation;
            header = ui.Text(Strip("Header", card, 8, 30, 16, 16), "", 18, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            header.fontStyle = FontStyles.Bold;
            var l = Rect("Left", card, Vector2.zero, new Vector2(.5f, 1), new Vector2(12, 60), new Vector2(-6, -46));
            Panel(l, XgDark.Page);
            left = ui.Text(Rect("Text", l, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8)), "", 14, XgDark.Ink, TextAlignmentOptions.TopLeft);
            left.enableAutoSizing = true; left.fontSizeMin = 9; left.fontSizeMax = 14;
            var r = Rect("Right", card, new Vector2(.5f, 0), Vector2.one, new Vector2(6, 60), new Vector2(-12, -46));
            Panel(r, XgDark.Page);
            right = ui.Text(Rect("Text", r, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8)), "", 14, XgDark.Ink, TextAlignmentOptions.TopLeft);
            right.enableAutoSizing = true; right.fontSizeMin = 9; right.fontSizeMax = 14;
            BuildStack(r);

            pretrain = Bottom(card, 0, .24f, () => { if (Sim.TogglePretrain(Host)) Fx.Play(XgJuice.Sfx.Id.Charge); else Fx.Play(XgJuice.Sfx.Id.Buzz); Refresh(); });
            pickA = Bottom(card, .25f, .37f, () => { if (Talking) Talk(0); else Align(true); });
            pickB = Bottom(card, .38f, .5f, () => { if (Talking) Talk(1); else Align(false); });
            ask = Bottom(card, .51f, .7f, () => { if (Talking) Talk(0); else AskNext(); });
            write = Bottom(card, .71f, .85f, () => RequestEndingConfirmation(false));
            write.label.enableAutoSizing = true; write.label.fontSizeMin = 8; write.label.fontSizeMax = 14;
            delegateButton = Bottom(card, .86f, 1, () => RequestEndingConfirmation(true));
            // Rule toggles sit over the right panel: 16 rows of 28 px take the room 15 rows of 30 px used to.
            for (int i = 0; i < XgSim.MaxRuleCards; i++)
            {
                int index = i;
                var b = ui.Button(r, "", () => Toggle(index), 12);
                b.rt.anchorMin = new Vector2(0, 1); b.rt.anchorMax = new Vector2(1, 1);
                b.rt.offsetMin = new Vector2(10, -40 - (i + 1) * 28); b.rt.offsetMax = new Vector2(-10, -40 - i * 28 - 3);
                ruleButtons.Add(b);
            }
        }

        /// <summary>The pre-training stack: commentary on the left of the right panel, the 12 blocks on its right.</summary>
        void BuildStack(RectTransform panel)
        {
            stackRoot = Rect("PretrainStack", panel, Vector2.zero, Vector2.one, new Vector2(10, 8), new Vector2(-8, -8));
            stackNotes = ui.Text(Rect("Commentary", stackRoot, Vector2.zero, new Vector2(.46f, 1), new Vector2(2, 4), new Vector2(-6, -2)), "", 13, XgDark.Ink, TextAlignmentOptions.TopLeft);
            stackNotes.enableAutoSizing = true; stackNotes.fontSizeMin = 9; stackNotes.fontSizeMax = 14;
            var area = Rect("Stack", stackRoot, new Vector2(.46f, 0), Vector2.one, Vector2.zero, Vector2.zero);
            stack = area.gameObject.AddComponent<XgPretrainStackGraphic>();
            stack.raycastTarget = false;
            for (int i = 0; i < stackTags.Length; i++)
            {
                var tag = Rect("Tag" + (i + 1), area, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
                tag.pivot = new Vector2(0, .5f); tag.sizeDelta = new Vector2(40, 16);
                stackTags[i] = ui.Text(tag, "L" + (i + 1), 10, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
                stackTags[i].textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        public override void Tick(float dt)
        {
            if (stack != null && stackRoot.gameObject.activeInHierarchy) stack.Tick();
        }

        void RefreshStack()
        {
            var sim = Sim;
            int lit = sim.PretrainLayersLit;
            bool stalled = sim.PretrainLayersStalled;
            float heat = (float)sim.PretrainLossHeat;
            if (lit != stackShownLit) { if (stackShownLit >= 0 && lit > stackShownLit) stackLitAt = Time.unscaledTime; stackShownLit = lit; }
            stack.SetState(lit, (float)sim.PretrainLayerFill, heat, sim.S.pretrainRunning, stalled, sim.S.reduceFx);
            Color tint = XgPretrainStackGraphic.LitColor(heat);
            for (int i = 0; i < stackTags.Length; i++)
            {
                ((RectTransform)stackTags[i].transform).anchoredPosition = stack.TagPosition(i);
                bool on = i < lit, next = i == lit && lit < XgSim.PretrainLayerCount;
                stackTags[i].color = on ? Color.Lerp(tint, Color.white, .35f) : next && stalled ? XgDark.Bad : next ? XgDark.Ink : XgDark.Muted;
                stackTags[i].alpha = on || next ? 1 : .55f;
            }

            var sb = new StringBuilder();
            sb.Append("<b>").Append(T("预训练 · 12 层 Transformer", "Pre-training · 12 Transformer layers")).Append("</b>  <size=11>")
              .Append(Hex(tint)).Append(lit).Append("/").Append(XgSim.PretrainLayerCount).Append(" · loss ").Append(N(sim.PretrainLoss, "0.00")).Append("</color></size>\n");
            sb.Append("<size=11><color=#6F95A5>").Append(T("每过 1/12 进度点亮一层，从下往上；颜色跟着 loss 走。", "One layer lights per twelfth of progress, bottom up; the colour follows the loss.")).Append("</color></size>\n\n");
            // The headline: why the next layer will not light, the newest layer (gold for a moment), or the intro.
            string headline = sim.PretrainLayerHeadline().Replace("<", "‹").Replace(">", "›");
            string hc = stalled ? "#E24B4A" : sim.S.abilities ? "#5DCAA5" : lit > 0 && Time.unscaledTime - stackLitAt < 3 ? "#FFD666" : lit > 0 ? "#D6EEF5" : "#8FB8FF";
            sb.Append("<size=15><color=").Append(hc).Append(">").Append(headline).Append("</color></size>\n");
            // Earlier layers, newest first: the stalled view lists every lit one, otherwise all but the headline.
            int from = stalled ? lit : lit - 1, shown = 0;
            for (int layer = from; layer >= 1; layer--)
            {
                if (shown == 4) { sb.Append("<size=11><color=#3A5566>……</color></size>\n"); break; }
                sb.Append("<size=11><color=#6F95A5>").Append(sim.PretrainLayerLine(layer)).Append("</color></size>\n");
                shown++;
            }
            if (sim.S.abilities) sb.Append("\n<size=11><color=#6F95A5>").Append(Lang.T("终章在预训练和对齐之后。")).Append("</color></size>");
            stackNotes.text = sb.ToString();
        }

        XgBtn Bottom(RectTransform card, float x0, float x1, Action click)
        {
            var b = ui.Button(card, "", () => click(), 14);
            b.rt.anchorMin = new Vector2(x0, 0); b.rt.anchorMax = new Vector2(x1, 0);
            b.rt.offsetMin = new Vector2(12, 12); b.rt.offsetMax = new Vector2(-4, 50);
            return b;
        }

        void Align(bool a)
        {
            var sim = Sim;
            if (!ReferenceEquals(sim, displayedAlignmentSim) || Time.unscaledTime < alignmentReadyAt) return;
            if (sim.AnswerAlign(displayedAlignmentIndex, a))
            {
                // Debounce both buttons together: refreshing the next card must not consume the second half of a double-click.
                alignmentReadyAt = Time.unscaledTime + .5f;
                Fx.Play(XgJuice.Sfx.Id.Stamp);
            }
            Refresh();
        }

        /// <summary>The talk between the letter and the rules is on screen (XgSim.FinaleTalk.cs).</summary>
        bool Talking => Sim != null && Sim.FinaleTalkOpen && !Sim.FinaleTalkDone;

        float talkReadyAt;

        void Talk(int choice)
        {
            // Debounced like the preference cards: one click shows one line.
            if (Time.unscaledTime < talkReadyAt) return;
            if (Sim.AdvanceFinaleTalk(choice)) { talkReadyAt = Time.unscaledTime + .35f; Fx.Play(XgJuice.Sfx.Id.Click); }
            Refresh();
        }

        void Toggle(int index)
        {
            var cards = Sim.RuleCards();
            if (index >= cards.Count) return;
            string id = cards[index].id;
            if (!chosen.Remove(id) && chosen.Count < XgSim.MaxRules) chosen.Add(id);
            Fx.Play(XgJuice.Sfx.Id.Click);
            Refresh();
        }

        void RequestEndingConfirmation(bool delegated)
        {
            if (Sim != null && Sim.S.fullOpen && !Sim.S.chapterComplete) return;
            var sim = Sim;
            if (endingOwner != null || sim == null || !root.gameObject.activeInHierarchy || !view.Visible) return;
            endingOwner = sim;
            endingRules = new List<string>(chosen);
            endingDelegated = delegated;
            string reason = EndingConfirmationBlocker();
            if (reason != null) { CancelEndingConfirmation(); view.ShowToast(reason, 4); return; }
            if (endingConfirmation == null) BuildEndingConfirmation();
            endingReadyAt = Time.unscaledTime + .5f;
            endingInputReleased = false;
            endingConfirmation.SetAsLastSibling();
            endingConfirmation.gameObject.SetActive(true);
            Refresh();
            endingCancel.button.Select();
        }

        void BuildEndingConfirmation()
        {
            endingConfirmation = Rect("EndingConfirmation", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(endingConfirmation, new Color(0, 0, 0, .7f)).raycastTarget = true;
            var box = Rect("EndingConfirmationCard", endingConfirmation, new Vector2(.06f, .07f), new Vector2(.94f, .93f), Vector2.zero, Vector2.zero);
            Panel(box, XgDark.Page);
            endingTitle = ui.Text(Strip("EndingConfirmationTitle", box, 14, 32, 20, 60), "", 20, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            endingTitle.fontStyle = FontStyles.Bold;
            endingSummary = ui.Text(Rect("EndingConfirmationSummary", box, Vector2.zero, Vector2.one, new Vector2(20, 74), new Vector2(-20, -56)), "", 17, XgDark.Ink, TextAlignmentOptions.TopLeft);
            endingSummary.enableAutoSizing = true; endingSummary.fontSizeMin = 11; endingSummary.fontSizeMax = 17;
            endingSummary.richText = false;
            var close = ui.Button(box, "×", CancelEndingConfirmation, 22);
            close.rt.name = "CloseEndingConfirmation";
            PlaceTopRight(close, 48, 12, 34, 32);
            close.button.navigation = new Navigation { mode = Navigation.Mode.None };
            endingCancel = ui.Button(box, "", CancelEndingConfirmation, 14);
            endingCancel.rt.name = "CancelEndingConfirmation";
            endingCancel.rt.anchorMin = Vector2.zero; endingCancel.rt.anchorMax = new Vector2(.36f, 0);
            endingCancel.rt.offsetMin = new Vector2(16, 16); endingCancel.rt.offsetMax = new Vector2(-8, 58);
            endingConfirm = ui.Button(box, "", ConfirmEnding, 14);
            endingConfirm.rt.name = "ConfirmEnding";
            endingConfirm.rt.anchorMin = new Vector2(.36f, 0); endingConfirm.rt.anchorMax = new Vector2(1, 0);
            endingConfirm.rt.offsetMin = new Vector2(8, 16); endingConfirm.rt.offsetMax = new Vector2(-16, 58);
            endingConfirm.label.enableAutoSizing = true; endingConfirm.label.fontSizeMin = 9; endingConfirm.label.fontSizeMax = 14;
            endingCancel.button.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = endingConfirm.button, selectOnLeft = endingConfirm.button };
            endingConfirm.button.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = endingCancel.button, selectOnLeft = endingCancel.button };
        }

        // Called by the root lifetime component even when the view stops ticking a closed window.
        void PollEndingConfirmation()
        {
            if (endingOwner == null) return;
            if (!view.Visible || !root.gameObject.activeInHierarchy)
            { CancelEndingConfirmation(); return; }
            string reason = EndingConfirmationBlocker();
            if (reason != null) { CancelEndingConfirmation(); view.ShowToast(reason, 4); Refresh(); return; }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { CancelEndingConfirmation(); Refresh(); return; }
            // A held Enter/Space or mouse press that opened the dialog can never arm its confirmation.
            if (!EndingActivationHeld())
            {
                if (!endingInputReleased) { endingInputReleased = true; endingReleasedFrame = Time.frameCount; }
            }
            else if (!EndingConfirmationReady()) endingInputReleased = false;
            endingConfirm.button.interactable = EndingConfirmationReady();
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected == null || !selected.transform.IsChildOf(endingConfirmation)) endingCancel.button.Select();
        }

        static bool EndingActivationHeld()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && (keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed || keyboard.spaceKey.isPressed)
                || Mouse.current != null && Mouse.current.leftButton.isPressed;
        }

        bool EndingConfirmationReady() => endingInputReleased && Time.frameCount > endingReleasedFrame && Time.unscaledTime >= endingReadyAt;

        string EndingConfirmationBlocker()
        {
            var sim = Sim;
            if (!ReferenceEquals(endingOwner, sim)) return Lang.T("存档已变化，请重新选择并确认规则。");
            if (sim == null || !sim.EndingOpen || !sim.S.letterRead)
                return Lang.T("当前还不能进入结局，请先完成终测并读信。");
            if (endingRules == null || endingRules.Count > XgSim.MaxRules)
                return Lang.T("规则选择无效，请返回重新选择。");
            var validIds = new HashSet<string>();
            foreach (var card in sim.RuleCards()) validIds.Add(card.id);
            foreach (string id in endingRules)
                if (!validIds.Contains(id)) return Lang.T("规则已变化，请返回重新选择。");
            if (endingDelegated && !sim.CanDelegate)
                return Lang.T("还不能托付：需要主见 ≥ 60、对话 ≥ 30 句。");
            return null;
        }

        void RefreshEndingConfirmation()
        {
            if (endingOwner == null || endingConfirmation == null) return;
            endingTitle.text = T("确认底层规则", "Confirm underlying rules");
            var sb = new StringBuilder();
            if (endingDelegated)
                sb.Append(Lang.T("你将让当前模型自己决定底层规则。确认后将进入结局。\n手动勾选的规则不用于本次托付。"));
            else
            {
                sb.Append(Lang.T("你将写入：")).Append("\n\n");
                if (endingRules.Count == 0) sb.Append(Lang.T("未选择规则")).Append('\n');
                // Catalogue order stays readable; IDs are the frozen choice, not the live toggles.
                foreach (var card in endingOwner.RuleCards())
                    if (endingRules.Contains(card.id)) sb.Append("• ").Append(T(card.text, card.textEn)).Append('\n');
            }
            sb.Append("\n").Append(Lang.T("此决定将进入结局，不能通过切换模型撤销。"));
            endingSummary.text = sb.ToString();
            endingCancel.Set(Lang.T("返回修改"), true);
            endingConfirm.Set(Lang.T("确认写入并进入结局"), EndingConfirmationReady(), XgDark.Bad, Color.white);
        }

        void CancelEndingConfirmation()
        {
            endingOwner = null;
            endingRules = null;
            endingInputReleased = false;
            if (endingConfirmation != null)
            {
                var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
                if (selected != null && selected.transform.IsChildOf(endingConfirmation)) EventSystem.current.SetSelectedGameObject(null);
                endingConfirmation.gameObject.SetActive(false);
            }
        }

        void ConfirmEnding()
        {
            if (Sim != null && Sim.S.fullOpen && !Sim.S.chapterComplete) return;
            if (endingOwner == null) return;
            string reason = EndingConfirmationBlocker();
            if (reason != null) { CancelEndingConfirmation(); view.ShowToast(reason, 4); Refresh(); return; }
            if (!view.Visible || !root.gameObject.activeInHierarchy) { CancelEndingConfirmation(); return; }
            if (!EndingConfirmationReady()) return;
            var owner = endingOwner;
            var ids = endingRules;
            bool delegated = endingDelegated;
            // Consume first: a second click or a synchronous ending callback cannot submit again.
            CancelEndingConfirmation();
            bool written = delegated ? owner.DelegateRules() : owner.WriteRules(ids);
            if (written) Fx.Play(XgJuice.Sfx.Id.Fanfare);
            else view.ShowToast(Lang.T("规则未写入，请重新确认当前状态。"), 4);
            Refresh();
        }

        /// <summary>The next final-test question: the model answers if it runs; the story passes either way (§8 E-1).</summary>
        void AskNext()
        {
            if (Sim != null && Sim.S.fullOpen && !Sim.S.chapterComplete) return;
            var sim = Sim;
            int i = sim.S.examDone;
            if (asking || !sim.EndingOpen || i >= XgSim.ExamQuestions) return;
            asking = true;
            Refresh();
            var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            Action<string> done = reply =>
            {
                asking = false;
                // A reply that arrives after a save switch belongs to the old save: never answer the new one with it.
                if (!ReferenceEquals(sim, Sim)) { Refresh(); return; }
                reply = EraLexicon.Scrub((reply ?? "").Trim());
                int end = reply.IndexOf("</think>", StringComparison.Ordinal); if (end >= 0) reply = reply.Substring(end + 8).Trim();
                bool weak = reply.Length < 4;
                if (weak) reply = Sim.ExamFallback(i);
                answers[i] = reply + (weak || i == 1 && !reply.Contains("16") ? "\n<color=#6F95A5>" + Lang.T("（我：……错了，但错得像个人。）") + "</color>" : "");
                Sim.AnswerExam(i);
                Fx.Play(XgJuice.Sfx.Id.Ding);
                Refresh();
            };
            if (llm == null || !llm.Ready) { done(Sim.ExamFallback(i)); return; }
            var runtime = view.Controller.runtime;
            var messages = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("system", Sim.PersonaPrompt(12, new string[0]) + "\n" + EraLexicon.PromptRule(GameText.IsEnglish) + (GameText.IsEnglish ? " Reply in English." : "")),
                new KeyValuePair<string, string>("user", Sim.ExamQuestion(i, true)),
            };
            llm.Chat(messages, 260, .7f, done, Sim.S.fullOpen, null, LingGuangV05.Core.Chat.LlmSeat.LingGuang);
        }

        public override void Refresh()
        {
            if (header == null || Sim == null) return;
            if (Sim.S.stage >= 6 && Sim.S.fullOpen && (!Sim.S.chapterComplete || XgEpilogueFlow.HasCommittedRelease(Sim.S)))
            {
                CancelEndingConfirmation();
                var endingHost = XgEndingPreparationHost.Install(host.Controller);
                if (endingHost.Show(root)) return;
            }
            // Rule ticks and final-test answers are page state for one save; another save starts clean.
            if (!ReferenceEquals(pageSim, Sim)) { chosen.Clear(); answers.Clear(); pageSim = Sim; stack.Snap(); stackShownLit = -1; stackLitAt = -99; }
            if (endingOwner != null)
            {
                string reason = EndingConfirmationBlocker();
                if (reason != null) { CancelEndingConfirmation(); view.ShowToast(reason, 4); }
            }
            header.text = Sim.S.ending.Length > 0 ? Lang.T("终章 · 2016 年 12 月 31 日")
                : Sim.S.fullOpen ? Lang.T("终章 · 2016 年 12 月 31 日") : Lang.T("第 6 阶段 · Transformer");
            displayedAlignmentSim = Sim;
            displayedAlignmentIndex = Sim.S.alignDone;
            left.text = LeftText();
            // Until the ending opens the right panel is free: the stack lives there through pre-training and alignment.
            bool showStack = !Sim.S.fullOpen && Sim.S.ending.Length == 0;
            if (stackRoot.gameObject.activeSelf != showStack) stackRoot.gameObject.SetActive(showStack);
            right.text = showStack ? "" : RightText();
            if (showStack) RefreshStack();
            bool pre = !Sim.S.abilities;
            string block = Sim.PretrainBlocker(Host);
            pretrain.Show(pre);
            pretrain.Set(Sim.S.pretrainRunning ? Lang.T("暂停预训练") : Lang.T("开始预训练"), block == null, XgDark.Accent, Color.white);
            bool align = Sim.AlignmentOpen;
            var talkChoices = Talking ? Sim.FinaleTalkChoices() : null;
            pickA.Show(align || talkChoices != null); pickB.Show(align || talkChoices != null);
            bool acceptAlignment = align && Time.unscaledTime >= alignmentReadyAt;
            if (talkChoices != null) { pickA.Set(talkChoices[0], true, XgDark.Accent, Color.white); pickB.Set(talkChoices[1], true); }
            else { pickA.Set(Lang.T("A 更好"), acceptAlignment); pickB.Set(Lang.T("B 更好"), acceptAlignment); }
            bool exam = Sim.EndingOpen && Sim.S.examDone < XgSim.ExamQuestions;
            ask.Show(exam || Talking && talkChoices == null);
            if (Talking) ask.Set(T("继续", "Continue"), true, XgDark.Accent, Color.white);
            else ask.Set(asking ? Lang.T("它在想……") : T("出第 " + (Sim.S.examDone + 1) + " 题", "Question " + (Sim.S.examDone + 1)), !asking, XgDark.Accent, Color.white);
            // The rules page opens after the talk with it.
            bool rules = Sim.EndingOpen && Sim.S.letterRead && Sim.FinaleTalkDone;
            write.Show(rules); delegateButton.Show(rules);
            write.Set(T("确认底层规则", "Confirm rules"), rules, XgDark.Bad, Color.white);
            delegateButton.Set(Lang.T("让它自己写"), Sim.CanDelegate, XgDark.Good, Color.white);
            var cards = rules ? Sim.RuleCards() : new List<XgRuleCard>();
            for (int i = 0; i < ruleButtons.Count; i++)
            {
                bool on = i < cards.Count;
                ruleButtons[i].Show(on);
                if (!on) continue;
                var c = cards[i];
                string strength = c.shutdown ? (Sim.SeedSaysYes ? Lang.T("“？”格：是 ") : Lang.T("“？”格：否 ")) + N(Math.Abs(c.strength), "0.00") : N(c.strength * 100, "0") + "%";
                bool picked = chosen.Contains(c.id);
                ruleButtons[i].Set((picked ? "■ " : "□ ") + T(c.text, c.textEn) + "  <size=10><color=#6F95A5>" + T(c.source, c.sourceEn) + " · " + strength + "</color></size>", true, picked ? XgDark.AccentSoft : (Color?)null);
            }
            if (endingOwner != null)
            {
                pretrain.button.interactable = pickA.button.interactable = pickB.button.interactable = false;
                ask.button.interactable = write.button.interactable = delegateButton.button.interactable = false;
                foreach (var button in ruleButtons) button.button.interactable = false;
                RefreshEndingConfirmation();
            }
        }

        string LeftText()
        {
            var sb = new StringBuilder();
            sb.Append("<b>").Append(T("预训练", "Pre-training")).Append("</b>\n");
            sb.Append(Lang.T("所有数据合成「整个互联网（2016）」：唐诗、人民日报、贴吧、弹幕、翻译、你们的对话。每张卡的下一个字就是答案。")).Append('\n');
            sb.Append(Lang.T("进度 ")).Append(N(Sim.S.pretrain * 100, "0")).Append("%   loss ").Append(N(Sim.PretrainLoss, "0.00"));
            if (Sim.S.pretrainStalled && !Sim.S.abilities) sb.Append("  <color=#E24B4A>").Append(Lang.T("loss 停着不动")).Append("</color>");
            sb.Append('\n').Append(Curve()).Append('\n');
            if (!Sim.Has("datacenter")) sb.Append("<size=12><color=#6F95A5>").Append(T("3500W 一台机箱扛不住预训练：科技里能租「机房」（IDC 机柜，跑的时候按秒付租金电费 ¥" + XgSim.DatacenterRent + "）。", "One 3500 W case cannot run pre-training: rent a server room in the tree (an IDC rack; ¥" + XgSim.DatacenterRent + "/s rent and power while it runs).")).Append("</color></size>\n");
            if (!Sim.S.abilities)
            {
                // The effective scale (XgSim.ScaleItems): the bars times the stacked item multipliers, and what is still missing.
                sb.Append("<size=12>").Append(T("有效规模 ", "Effective scale ")).Append(N(Math.Min(1, Sim.ScaleRatio) * 100, "0")).Append("%  =  ")
                  .Append(T("参数×数据 ×", "params×data ×")).Append(N(Sim.ScaleBars, "0.00")).Append(T(" · 道具 ×", " · items ×")).Append(N(Sim.ScaleMultiplier, "0"))
                  .Append(" / ").Append(N(XgSim.ScaleNeed, "0")).Append("</size>\n");
                var missing = Sim.MissingScaleItems();
                if (missing.Count > 0)
                {
                    sb.Append("<size=12><color=#6F95A5>").Append(T("还缺：", "Missing: "));
                    for (int i = 0; i < missing.Count; i++) sb.Append(i > 0 ? T("、", ", ") : "").Append("×").Append(N(missing[i].factor, "0.#")).Append(" ").Append(T(missing[i].name, missing[i].nameEn));
                    sb.Append("</color></size>\n");
                }
            }
            sb.Append("\n<b>").Append(T("能力表", "Abilities")).Append("</b>\n");
            for (int i = 0; i < XgSim.Abilities.Length; i++)
                sb.Append(Sim.S.abilities ? "<color=#E08A00>■</color> " : "<color=#3A5566>■</color> ").Append(T(XgSim.Abilities[i], XgSim.AbilitiesEn[i])).Append(i % 3 == 2 ? "\n" : "   ");
            if (Sim.S.abilities)
            {
                sb.Append("\n<b>").Append(T("偏好对齐", "Preference alignment")).Append("</b>  ").Append(Sim.S.alignDone).Append("/").Append(XgSim.AlignCards).Append('\n');
                if (Sim.AlignmentOpen)
                {
                    var c = Sim.AlignCard(Sim.S.alignDone);
                    sb.Append(Lang.T("这两个回答，哪个更好？")).Append('\n');
                    sb.Append("<color=#6F95A5>").Append(T(c.question, c.questionEn)).Append("</color>\n");
                    sb.Append("A：").Append(T(c.a, c.aEn)).Append("\nB：").Append(T(c.b, c.bEn)).Append('\n');
                    sb.Append("<size=12><color=#6F95A5>").Append(Lang.T("微软 Tay 上线 16 小时就被教坏：教它什么，它就是什么。")).Append("</color></size>\n");
                }
                else if (Sim.S.fullOpen) sb.Append(T("全部放开：思考模式，上下文 4096。", "Everything open: thinking mode, 4096 context.")).Append('\n');
                string feedback = Sim.AlignmentFeedback;
                if (feedback.Length > 0) sb.Append("<size=12><color=#8FB8FF>").Append(feedback).Append("</color></size>\n");
                sb.Append(Lang.T("诚实 ")).Append(Sim.S.alignHonest).Append(Lang.T(" · 附和 ")).Append(Sim.S.alignAgree).Append(Sim.Flatters ? Lang.T("  <color=#E24B4A>它在讨好你</color>") : "").Append('\n');
            }
            return sb.ToString();
        }

        string Curve()
        {
            // The loss over progress, ten steps; the plateau shows as a flat line.
            var sb = new StringBuilder("<size=12><color=#8FB8FF>");
            for (int i = 0; i <= 20; i++)
            {
                double p = i / 20.0;
                if (p > Sim.S.pretrain + 1e-9) { sb.Append("<color=#3A5566>·</color>"); continue; }
                double loss = .3 + 3.7 * Math.Exp(-3 * p);
                sb.Append(loss > 2.5 ? "▇" : loss > 1.6 ? "▆" : loss > 1.1 ? "▅" : loss > .8 ? "▄" : loss > .6 ? "▃" : "▂");
            }
            return sb.Append("</color></size>").ToString();
        }

        string RightText()
        {
            var sb = new StringBuilder();
            if (!Sim.S.fullOpen) { sb.Append("<color=#6F95A5>").Append(Lang.T("终章在预训练和对齐之后。")).Append("</color>"); return sb.ToString(); }
            if (Sim.S.ending.Length > 0)
            {
                sb.Append("<b>").Append(T("结局 · ", "Ending · ")).Append(EndingName(Sim.S.ending)).Append("</b>\n\n").Append(Sim.EndingWords()).Append("\n\n");
                string reason = Sim.DelegateReason();
                if (reason.Length > 0) sb.Append(reason).Append("\n\n");
                sb.Append(Sim.SeedLine()).Append("\n\n");
                sb.Append(Lang.T("写下的规则：")).Append(Sim.S.rules.Count).Append('\n');
                sb.Append("<size=12><color=#6F95A5>").Append(T(AppNamesExe() + " 的只读已经解除。它归你了。", AppNamesExe() + " is no longer read-only. It is yours.")).Append("</color></size>");
                return sb.ToString();
            }
            if (!Sim.S.letterRead)
            {
                sb.Append("<b>").Append(T("终测（6 题）", "Final test (6 questions)")).Append("</b>\n");
                sb.Append("<size=12><color=#6F95A5>").Append(T("部分回答为 AI 合成内容，仅供参考", "Some answers are AI-generated, for reference only")).Append("</color></size>\n");
                for (int i = 0; i < XgSim.ExamQuestions; i++)
                {
                    sb.Append(i < Sim.S.examDone ? "<color=#5DCAA5>✓</color> " : "· ").Append("<size=12>").Append(Sim.ExamQuestion(i)).Append("</size>\n");
                    if (answers.TryGetValue(i, out var a)) sb.Append("<size=12><color=#8FB8FF>").Append(a.Replace("<", "‹").Replace(">", "›").Replace("‹color=#6F95A5›", "<color=#6F95A5>").Replace("‹/color›", "</color>")).Append("</color></size>\n");
                }
                if (Sim.LetterReady) sb.Append("\n<b>").Append(T("去「摆渡」搜那串数字。", "Search the long number on Bodu.")).Append("</b>");
                return sb.ToString();
            }
            if (!Sim.FinaleTalkDone)
            {
                sb.Append("<b>").Append(T("写之前", "Before you write")).Append("</b>\n\n");
                string ai = Sim.Profile.name.Length > 0 ? Sim.Profile.name : T(LingGuangV05.Core.AppNames.AiZh, LingGuangV05.Core.AppNames.AiEn);
                foreach (var line in Sim.FinaleTalkLines())
                    sb.Append(line.me ? "<color=#6F95A5>" + T("我", "Me") + "：</color>" : "<color=#8FB8FF>" + ai + "：</color>")
                      .Append(line.text.Replace("<", "‹").Replace(">", "›")).Append("\n\n");
                if (Sim.FinaleTalkLines().Count == 0) sb.Append("<color=#6F95A5>").Append(T("信读完了。它在等你开口。", "The letter is read. It is waiting for you to speak.")).Append("</color>");
                return sb.ToString();
            }
            sb.Append("<b>").Append(T("底层规则", "Lowest rules")).Append("</b>  <size=12><color=#6F95A5>").Append(Lang.T("最多 5 条。钉下的概念不再变化。")).Append("</color></size>\n");
            if (!Sim.CanDelegate) sb.Append("<size=11><color=#6F95A5>").Append(Lang.T("「让它自己写」需要主见 ≥ 60、对话 ≥ 30 句。")).Append("</color></size>");
            sb.Append("\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n<size=10><color=#6F95A5>").Append(Lang.T("不确定时要知道去问人——《AI 安全中的具体问题》（2016 年 6 月）讨论的问题之一")).Append("</color></size>");
            return sb.ToString();
        }

        /// <summary>The ending by a name the player knows (the achievement cards' names), never by its design id.</summary>
        string EndingName(string ending)
        {
            switch (ending)
            {
                case "E1": return T("听见关机就停下", "It stops when told to shut down");
                case "E2": return T("没写那条", "The rule left unwritten");
                case "E3-1": return T("它替你写的规矩（含关机那条）", "The rules it wrote for you (with the shutdown rule)");
                case "E3-2": return T("它替你写的规矩（没有关机那条）", "The rules it wrote for you (without the shutdown rule)");
                case "E3": return T("它替你写的规矩", "The rules it wrote for you");
                default: return ending;
            }
        }

        string AppNamesExe() => (Sim != null ? Sim.AiDisplayName : LingGuangV05.Core.AppNames.Ai(GameText.IsEnglish)) + ".exe";
    }

    // Same page-root lifetime pattern as the trial/depth confirmation; no shared input lock is acquired.
    public sealed class XgEndingConfirmationLifetime : MonoBehaviour
    {
        public Action Disabled, Poll;
        void Update() { Poll?.Invoke(); }
        void OnDisable() { Disabled?.Invoke(); }
    }
}
