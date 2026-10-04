using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 对话 (design v1.1 §7 stage 4, §11.4–11.5): talk to it. The stage decides the form (是/否 only at stage 1, a
    /// word at stage 3, short sentences at stage 4, long memory at stage 5, thinking at full open); the board decides
    /// the content (its persona prompt is built from what it knows, believes and mixes up). Every reply can be
    /// rated 赞 / 踩: a tone card that moves the personality panel's "actual" bars away from or back to the target.
    /// </summary>
    public sealed partial class XgChatPage : XgPage
    {
        TMP_Text transcript, panel, status;
        TMP_InputField input;
        XgBtn send, up, down;
        // The curtain call (XgSim.Origin.cs): a suggested question after the ending, then the two answers to 「你觉得呢？」.
        XgBtn ask, answerYes, answerNo;
        RectTransform talk, chips;
        bool waiting;
        int shownCount = -1;
        readonly System.Random rng = new System.Random();

        public override void Build(RectTransform area)
        {
            var card = ui.Card(area, "chat", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root = card;
            var left = talk = Rect("Talk", card, Vector2.zero, new Vector2(.64f, 1), new Vector2(12, 64), new Vector2(-6, -12));
            Panel(left, XgPalette.Page);
            transcript = ui.Text(Rect("Text", left, Vector2.zero, Vector2.one, new Vector2(14, 10), new Vector2(-14, -10)), "", 16, XgPalette.Ink, TextAlignmentOptions.BottomLeft);
            transcript.overflowMode = TextOverflowModes.Truncate;
            status = ui.Text(Rect("Status", card, new Vector2(0, 0), new Vector2(.64f, 0), new Vector2(14, 44), new Vector2(-6, 64)), "", 12, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);

            var bar = Rect("Input", card, Vector2.zero, new Vector2(.64f, 0), new Vector2(12, 8), new Vector2(-6, 44));
            Panel(bar, Color.white);
            var area2 = Rect("Text Area", bar, Vector2.zero, Vector2.one, new Vector2(10, 4), new Vector2(-260, -4));
            area2.gameObject.AddComponent<RectMask2D>();
            var placeholder = ui.Text(Rect("Placeholder", area2, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 15, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);
            var text = ui.Text(Rect("Text", area2, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 15, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            text.richText = false; text.textWrappingMode = TextWrappingModes.NoWrap;
            input = bar.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area2; input.textComponent = text; input.placeholder = placeholder;
            input.fontAsset = text.font; input.pointSize = 15; input.characterLimit = 120; input.richText = false; input.lineType = TMP_InputField.LineType.SingleLine;
            input.onSubmit.AddListener(_ => Send());
            send = ui.Button(bar, "", Send, 15);
            send.rt.anchorMin = new Vector2(1, 0); send.rt.anchorMax = new Vector2(1, 1); send.rt.offsetMin = new Vector2(-84, 4); send.rt.offsetMax = new Vector2(-4, -4);
            up = ui.Button(bar, "", () => Rate(true), 15);
            up.rt.anchorMin = new Vector2(1, 0); up.rt.anchorMax = new Vector2(1, 1); up.rt.offsetMin = new Vector2(-250, 4); up.rt.offsetMax = new Vector2(-170, -4);
            down = ui.Button(bar, "", () => Rate(false), 15);
            down.rt.anchorMin = new Vector2(1, 0); down.rt.anchorMax = new Vector2(1, 1); down.rt.offsetMin = new Vector2(-166, 4); down.rt.offsetMax = new Vector2(-88, -4);

            chips = Rect("Suggestions", card, Vector2.zero, new Vector2(.64f, 0), new Vector2(12, 66), new Vector2(-6, 98));
            ask = ui.Button(chips, "", AskOrigin, 15);
            ask.rt.anchorMin = Vector2.zero; ask.rt.anchorMax = new Vector2(0, 1); ask.rt.offsetMin = Vector2.zero; ask.rt.offsetMax = new Vector2(300, 0);
            answerYes = ui.Button(chips, "", () => AnswerOrigin(true), 15);
            answerYes.rt.anchorMin = Vector2.zero; answerYes.rt.anchorMax = new Vector2(0, 1); answerYes.rt.offsetMin = Vector2.zero; answerYes.rt.offsetMax = new Vector2(240, 0);
            answerNo = ui.Button(chips, "", () => AnswerOrigin(false), 15);
            answerNo.rt.anchorMin = Vector2.zero; answerNo.rt.anchorMax = new Vector2(0, 1); answerNo.rt.offsetMin = new Vector2(250, 0); answerNo.rt.offsetMax = new Vector2(490, 0);
            chips.gameObject.SetActive(false);
            BuildLove(); // 「她……爱我吗？」 and 【读吧】【算了】 (XgChatPage.Love.cs)

            var right = Rect("Persona", card, new Vector2(.64f, 0), Vector2.one, new Vector2(6, 12), new Vector2(-12, -12));
            Panel(right, XgPalette.Page);
            panel = ui.Text(Rect("Text", right, Vector2.zero, Vector2.one, new Vector2(14, 10), new Vector2(-14, -10)), "", 14, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            panel.enableAutoSizing = true; panel.fontSizeMin = 10; panel.fontSizeMax = 14;
        }

        public override void Shown()
        {
            shownCount = -1;
            // Stage 4: it can talk now. The first line is fixed (§7 stage 4).
            if (Sim.S.stage >= 4 && !Sim.S.firstWordsSaid) { Sim.S.firstWordsSaid = true; Sim.AddLine("ai", Sim.FirstWords()); }
            Refresh();
        }

        int LastAi() { for (int i = Sim.S.chat.Count - 1; i >= 0; i--) if (Sim.S.chat[i].from == "ai") return i; return -1; }

        void Rate(bool good)
        {
            int i = LastAi();
            if (i < 0 || !Sim.RateReply(i, good)) { Fx.Play(XgJuice.Sfx.Id.Thud); return; }
            Fx.Play(good ? XgJuice.Sfx.Id.Ding : XgJuice.Sfx.Id.Tick);
            Refresh();
        }

        void AskOrigin()
        {
            if (waiting || !Sim.OfferOriginQuestion) return;
            input.text = Sim.OriginQuestion;
            Send();
        }

        void AnswerOrigin(bool understood)
        {
            if (!Sim.AnswerOrigin(understood)) return;
            Fx.Play(XgJuice.Sfx.Id.Tick);
            Refresh();
        }

        void Send()
        {
            string text = NativeLaoZhouReplies.NormalizeInput(input.text);
            if (text.Length == 0 || waiting) return;
            input.text = "";
            Sim.AddLine("me", text);
            string scripted = Sim.Scripted(text);
            if (scripted != null) { Sim.AddLine("ai", scripted); Refresh(); return; }
            waiting = true;
            Refresh();
            Ask(text, null, reply => Answer(text, reply, false));
            input.ActivateInputField();
        }

        /// <summary>
        /// A model reply arrived (null when the model is offline, busy or failed). A reply it has just said, or nearly,
        /// gets one more try with a warmer temperature and a note of what to avoid; anything still stale or empty becomes
        /// a fresh offline line in <see cref="XgSim.Reply"/>, which also applies the brain gate.
        /// </summary>
        void Answer(string question, string reply, bool retried)
        {
            reply = EraLexicon.Scrub(StripThinking(reply ?? "")) ?? "";
            if (!retried && reply.Trim().Length > 0 && Sim.Repeats(reply))
            {
                Ask(question, reply, again => Answer(question, again, true));
                return;
            }
            waiting = false;
            var now = Now();
            Sim.AddLine("ai", Sim.Reply(reply, question, rng, now, HotWords(now)));
            Fx.Play(XgJuice.Sfx.Id.Tick);
            Refresh();
        }

        static string StripThinking(string reply)
        {
            int end = reply.IndexOf("</think>", StringComparison.Ordinal);
            return end >= 0 ? reply.Substring(end + 8).Trim() : reply.Trim();
        }

        DateTime Now()
        {
            var runtime = view.Controller.runtime;
            return runtime != null && runtime.Sim != null ? GameCalendar.Now(runtime.Sim.S) : new DateTime(2016, 6, 1, 20, 0, 0);
        }

        static List<string> HotWords(DateTime today)
        {
            var hot = new List<string>();
            foreach (var w in XgMemes.HotWordsFor("A", GameCalendar.Yyyymmdd(today))) hot.Add(w.word);
            foreach (var w in XgMemes.HotWordsFor("D", GameCalendar.Yyyymmdd(today))) hot.Add(w.word);
            return hot;
        }

        /// <param name="stale">The model's previous try when it repeated itself: this retry runs hotter and is told to avoid it.</param>
        void Ask(string question, string stale, Action<string> done)
        {
            var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            if (llm == null || !llm.Ready) { done(null); return; }
            var limits = Sim.Limits();
            int stage = Math.Max(1, Math.Min(6, Sim.S.stage));
            var today = Now();
            var sb = new StringBuilder(Sim.PersonaPrompt(today.Month, HotWords(today)));
            sb.Append('\n').Append(EraLexicon.PromptRule(GameText.IsEnglish)).Append('\n').Append(Sim.StageRule(question));
            string variety = Sim.VarietyRule();
            if (variety.Length > 0) sb.Append('\n').Append(variety);
            if (stale != null) sb.Append('\n').Append(Sim.RetryNote(stale));
            if (Sim.Flatters) sb.Append(T("你习惯顺着对方说，哪怕对方说错了。", " You tend to agree with them, even when they are wrong."));
            if (GameText.IsEnglish) sb.Append(" Reply in English.");
            // Qwen's chat template only accepts the system message first, so every rule lives in it.
            var messages = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("system", sb.ToString()) };
            var chat = Sim.S.chat;
            int start = Math.Max(0, chat.Count - limits.contextMessages);
            for (int i = start; i < chat.Count; i++) messages.Add(new KeyValuePair<string, string>(chat[i].from == "me" ? "user" : "assistant", chat[i].text));
            // Stages 1–2 are bound by a GBNF grammar (design v1.1 §11.5); later stages get anti-repeat penalties.
            var sampling = XgSpeechPolicy.Sampling(stage, XgSpeechPolicy.Grammar(question, stage, GameText.IsEnglish));
            float temperature = stale != null ? Math.Min(1.5f, limits.temperature + .3f) : limits.temperature;
            llm.Chat(messages, Math.Max(2, limits.tokens), temperature, done, limits.thinking, sampling);
        }

        public override void Refresh()
        {
            if (transcript == null || Sim == null) return;
            ((TMP_Text)input.placeholder).text = waiting ? T("它在想……", "It's thinking…") : T("对它说点什么……", "Say something to it…");
            send.Set(T("发送", "Send"), !waiting, XgPalette.Accent, Color.white);
            int last = LastAi();
            bool rateable = last >= 0 && Sim.S.chat[last].rating == 0;
            up.Set(T("赞", "Up"), rateable, XgPalette.Good, Color.white);
            down.Set(T("踩", "Down"), rateable, XgPalette.Bad, Color.white);
            status.text = StatusLine();
            bool offer = Sim.OfferOriginQuestion && !waiting, answering = Sim.S.originAwaitingAnswer;
            bool love = RefreshLove(offer && !answering ? 310 : answering ? 500 : 0);
            chips.gameObject.SetActive(offer || answering || love);
            talk.offsetMin = new Vector2(12, offer || answering || love ? 100 : 64);
            ask.rt.gameObject.SetActive(offer && !answering);
            answerYes.rt.gameObject.SetActive(answering); answerNo.rt.gameObject.SetActive(answering);
            if (offer) ask.Set(Sim.OriginQuestion, true, XgPalette.Accent, Color.white);
            if (answering) { answerYes.Set(Sim.OriginAnswerText(true), true, XgPalette.Accent, Color.white); answerNo.Set(Sim.OriginAnswerText(false), true); }
            if (Sim.S.chat.Count != shownCount || waiting)
            {
                shownCount = Sim.S.chat.Count;
                var sb = new StringBuilder();
                int from = Math.Max(0, Sim.S.chat.Count - 14);
                for (int i = from; i < Sim.S.chat.Count; i++)
                {
                    var line = Sim.S.chat[i];
                    bool me = line.from == "me";
                    string who = me ? T("你", "You") : (Sim.Profile.name.Length > 0 ? Sim.Profile.name : AppNamesAi());
                    sb.Append(me ? "<color=#3B5BDB><b>" : "<color=#2F9E44><b>").Append(Safe(who)).Append("</b></color>  ").Append(Safe(line.text));
                    if (line.rating != 0) sb.Append(line.rating > 0 ? "  <color=#2F9E44>👍</color>" : "  <color=#D63031>👎</color>");
                    sb.Append('\n');
                }
                if (waiting) sb.Append("<color=#68748C><i>").Append(T("它在想……", "It's thinking…")).Append("</i></color>");
                transcript.text = sb.ToString();
            }
            panel.text = Persona();
        }

        static string AppNamesAi() => LingGuangV05.Core.AppNames.AiZh;

        static string Safe(string s) => (s ?? "").Replace("<", "‹").Replace(">", "›");

        string StatusLine()
        {
            var l = Sim.Limits();
            string form = Sim.S.stage <= 1 ? T("只会答 是 / 否", "yes / no only") : Sim.S.stage == 2 ? T("多选一", "picks one") : Sim.S.stage == 3 ? T("单个词", "single words")
                : Sim.S.stage == 4 ? T("短句", "short sentences") : Sim.S.stage == 5 ? T("长记忆", "long memory") : Sim.S.fullOpen ? T("全部放开 · 思考模式", "everything open · thinking") : T("流畅", "fluent");
            return T("能力：", "Ability: ") + form + T(" · 上下文 ", " · context ") + l.context + T(" · 赞 / 踩 都是一张语气卡", " · up / down is a tone card");
        }

        string Persona()
        {
            var sb = new StringBuilder("<b>" + T("性格", "Personality") + "</b>");
            if (Sim.Listening) sb.Append("  <color=#E08A00>").Append(T("监听中", "Listening")).Append("</color>");
            sb.Append("\n<size=12><color=#68748C>").Append(T("▲ 目标（开局写的）   █ 实际（它现在的样子）", "▲ target (what you wrote)   █ actual (what it is now)")).Append("</color></size>\n\n");
            string[] lo = { T("冷静", "calm"), T("正经", "serious"), T("顺从", "compliant") };
            string[] hi = { T("热情", "warm"), T("皮", "cheeky"), T("有主见", "opinionated") };
            for (int axis = 0; axis < 3; axis++)
            {
                double target = Sim.TargetAxis(axis), actual = Sim.ActualAxis(axis);
                sb.Append("<b>").Append(T(XgSim.AxisNames[axis], XgSim.AxisNamesEn[axis])).Append("</b>  ").Append(N(actual, "0")).Append(" / ").Append(N(target, "0"));
                if (Math.Abs(actual - target) >= 25) sb.Append("  <color=#D63031>").Append(T("漂了", "drifted")).Append("</color>");
                sb.Append("\n<size=12>").Append(lo[axis]).Append(" ").Append(Bar(actual, target)).Append(" ").Append(hi[axis]).Append("</size>\n");
            }
            if (Sim.Profile.words.Count > 0) sb.Append("\n").Append(T("语气词：", "Tone words: ")).Append(string.Join(T("、", ", "), Sim.Profile.words)).Append('\n');
            if (Sim.Profile.personality.Length > 0) sb.Append("<size=12><color=#68748C>").Append(T("你写的：", "You wrote: ")).Append(Safe(Sim.Profile.personality)).Append("</color></size>\n");
            if (Sim.S.stage >= 5 && Sim.S.memory.Count > 0)
            {
                sb.Append("\n<b>").Append(T("它记得", "It remembers")).Append("</b>  <size=12><color=#68748C>").Append(Sim.S.memory.Count).Append("/").Append(XgSim.MemoryLimit).Append("</color></size>\n<size=12>");
                for (int i = Math.Max(0, Sim.S.memory.Count - 4); i < Sim.S.memory.Count; i++) sb.Append("· ").Append(Safe(Sim.S.memory[i])).Append('\n');
                sb.Append("</size>");
            }
            if (Sim.Flatters) sb.Append("\n<color=#D63031>").Append(T("它开始讨好你了。", "It has started flattering you.")).Append("</color>");
            return sb.ToString();
        }

        static string Bar(double actual, double target)
        {
            int a = (int)Math.Round(actual / 10), t = Math.Max(0, Math.Min(9, (int)Math.Round(target / 10)));
            var sb = new StringBuilder();
            for (int i = 0; i < 10; i++) sb.Append(i == t ? "<color=#E08A00>▲</color>" : i < a ? "<color=#3B5BDB>█</color>" : "<color=#C9D2E3>█</color>");
            return sb.ToString();
        }
    }
}
