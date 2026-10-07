using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Talking to it happens in YY (the 对话 page is gone). Once it has joined YY, shortly after its setup at stage 1
    /// (AiJoinsYy), this is the lab's
    /// side of that conversation: the player's lines go into the lab's chat log (XgState.chat, which 赞 / 踩, the tone
    /// habits, the memory book and the 「让它自己写」 count read), and every line the lab adds to that log (replies,
    /// emergence lines, afterthoughts, game remarks, the love verdict, the finale talk, the curtain call) arrives in YY.
    /// Replies come from the same pipeline the 对话 page used (persona, board, tone, habits, recalled memories and the
    /// stage's speech limits, then the brain gate), plus YY's stage-5 focus word underlined in the player's line. The
    /// suggested questions (「她……爱我吗？」, 「你是怎么被训练出来的？」) and their answers (【读吧】【算了】, the two
    /// answers to 「你觉得呢？」) are YY choices in its conversation. Its stage still limits what it can say there: 是 / 否
    /// at stage 1, one of a few at stage 2, words from ability 3. Until it has joined, its lines stay on the 概览 page's
    /// 「它刚说」 card. Attaches itself next to the 灵光 controller at scene load.
    /// </summary>
    public sealed class XgYyTalk : MonoBehaviour, ILingGuangTalk
    {
        const string Id = YYChatHub.LingGuangId;
        /// <summary>Seconds before a reply shows: it reads first, like a person, even when the answer is fixed or offline.</summary>
        const float ReadPause = 1.1f;

        static XgYyTalk instance;

        XingGuangController controller;
        XgSim bound;
        YYChatHub hooked;
        int ticket;
        /// <summary>A question is being answered (the model is thinking or the reply waits for its pause).</summary>
        bool waiting;
        /// <summary>The player's own YY line is being written into the lab's log: it is not copied back into YY.</summary>
        bool echo;
        float askedAt;
        Reply due;
        readonly Dictionary<YYMessage, XgChatLine> lines = new Dictionary<YYMessage, XgChatLine>();
        readonly System.Random rng = new System.Random();

        /// <summary>A reply waiting for its moment.</summary>
        sealed class Reply
        {
            public string text, question, focus;
            public YYMessage asked;
            public bool model, tagged, early;
            public float at;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void Attach()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.GetComponent<XgYyTalk>() != null) return;
            runtime.gameObject.AddComponent<XgYyTalk>();
        }

        void Awake() { instance = this; }

        void OnDestroy()
        {
            if (bound != null) bound.LineAdded -= OnLine;
            if (hooked != null && ReferenceEquals(hooked.LingGuangTalk, this)) hooked.LingGuangTalk = null;
            if (instance == this) instance = null;
        }

        // ───────────── where it is ─────────────

        /// <summary>It has joined YY: the chat with it lives there.</summary>
        public static bool InYY
        {
            get { var hub = YYChatHub.Instance; return hub != null && hub.S != null && hub.S.lingguangUnlocked; }
        }

        /// <summary>Opens YY on its conversation. False before it has joined YY (callers fall back to 概览).</summary>
        public static bool Open()
        {
            var hub = YYChatHub.Instance;
            if (!InYY || hub.router == null) return false;
            return hub.router.Open(YYChatHub.AppId, Id);
        }

        /// <summary>
        /// Draws the eye to its YY conversation (the inner voice has just thought of asking it): rings the suggested
        /// questions or its row when YY is on screen, otherwise a desktop toast that opens it. False before it is in YY.
        /// </summary>
        public static bool Point()
        {
            var hub = YYChatHub.Instance;
            if (!InYY) return false;
            var view = hub.View;
            if (view != null && DesktopNotifications.IsWindowVisible(view))
            {
                var target = hub.S.selected == Id ? XgGuideHighlight.FindIn(view.transform, "name:Choices") : null;
                target = target ?? XgGuideHighlight.FindIn(view.transform, "name:Session_" + Id);
                if (target != null) XgGuideHighlight.Pulse(target, 5f);
                return true;
            }
            var presenter = FindAnyObjectByType<StoryDesktopPresenter>();
            string ai = instance != null && instance.bound != null ? AiName(instance.bound) : T(AppNames.AiZh, AppNames.AiEn);
            if (presenter != null) presenter.ShowToast(T("去 YY 问问" + ai + "。", "Ask " + ai + " on YY."), () => Open());
            return true;
        }

        static string AiName(XgSim sim) => sim.Profile.name.Length > 0 ? sim.Profile.name : T(AppNames.AiZh, AppNames.AiEn);

        // ───────────── frame ─────────────

        void Update()
        {
            if (controller == null) controller = GetComponent<XingGuangController>();
            if (controller == null || controller.Sim == null) return;
            if (!ReferenceEquals(bound, controller.Sim)) Bind(controller.Sim);
            var hub = YYChatHub.Instance;
            if (!ReferenceEquals(hub, hooked)) { hooked = hub; lines.Clear(); }
            if (hub == null || hub.S == null) return;
            hub.LingGuangTalk = this;
            if (!hub.S.lingguangUnlocked) { hub.Stand(Id, null); return; }
            // Stage 4: it can talk now. Its first sentence is fixed (design v1.1 §7 stage 4) and waits for you to look.
            if (bound.S.stage >= 4 && !bound.S.firstWordsSaid && hub.IsShowing(Id) && !waiting)
            {
                bound.S.firstWordsSaid = true;
                bound.AddLine("ai", bound.FirstWords());
            }
            if (due != null && Time.unscaledTime >= due.at) Deliver(hub);
            hub.Stand(Id, waiting ? null : Choices());
        }

        void Bind(XgSim sim)
        {
            if (bound != null) bound.LineAdded -= OnLine;
            bound = sim;
            bound.LineAdded += OnLine;
            ticket++;
            waiting = false; due = null; echo = false;
            lines.Clear();
        }

        // ───────────── the lab's log → YY ─────────────

        void OnLine(XgChatLine line)
        {
            if (line == null || string.IsNullOrEmpty(line.text) || echo && line.from == "me") return;
            var hub = YYChatHub.Instance;
            if (hub == null || hub.S == null || !hub.S.lingguangUnlocked) return;
            if (line.from == "me") { hub.SendAuthoredChoice(Id, line.text); return; }
            hub.Receive(Id, line.text, !Quiet());
            var conv = hub.Conversation(Id);
            if (conv != null && conv.messages.Count > 0) lines[conv.messages[conv.messages.Count - 1]] = line;
        }

        /// <summary>The finale talk is read on the 终章 page; its lines go into YY without a notification on top of it.</summary>
        bool Quiet()
        {
            var view = controller != null ? controller.View : null;
            return view != null && view.Visible && view.Tab == "final";
        }

        // ───────────── YY → the lab ─────────────

        public bool Thinking => waiting && bound != null;

        public bool Heard(YYMessage message)
        {
            var sim = bound;
            if (sim == null || message == null) return false;
            string text = NativeLaoZhouReplies.NormalizeInput(message.text);
            if (text.Length == 0) return true;
            // The choices with rules of their own: 【读吧】【算了】 and the two answers to 「你觉得呢？」.
            if (sim.S.loveAwaitingConsent && Consent(sim, text, out bool read)) { Echo(() => sim.AnswerLoveConsent(read)); return true; }
            if (sim.S.originAwaitingAnswer && OriginAnswer(sim, text, out bool understood)) { Echo(() => sim.AnswerOrigin(understood)); return true; }
            Echo(() => sim.AddLine("me", text));
            // Several quick lines get one answer: the reply on its way answers them too.
            if (waiting) return true;
            waiting = true; askedAt = Time.unscaledTime;
            // Before stage 5 the first 「她……爱我吗？」 gets one word it cannot possibly know; the inner voice notices.
            bool early = XgSim.IsLoveQuestion(text) && sim.S.stage < XgSim.LoveStage && sim.S.loveEarlyAsked == 0;
            string scripted = sim.Scripted(text);
            if (scripted != null)
            {
                due = new Reply { text = scripted, question = text, early = early, at = askedAt + ReadPause };
                return true;
            }
            int t = ticket;
            Ask(sim, text, null, reply => Answer(sim, t, text, message, reply, null, false));
            return true;
        }

        void Echo(Action write)
        {
            echo = true;
            try { write(); }
            finally { echo = false; }
        }

        static string Bare(string text) => (text ?? "").Trim().Trim('【', '】', '[', ']').Trim();

        static bool Consent(XgSim sim, string text, out bool read)
        {
            string t = Bare(text);
            read = t == sim.LoveConsentText(true);
            return read || t == sim.LoveConsentText(false);
        }

        static bool OriginAnswer(XgSim sim, string text, out bool understood)
        {
            string t = Bare(text);
            understood = t == Bare(sim.OriginAnswerText(true));
            return understood || t == Bare(sim.OriginAnswerText(false));
        }

        /// <summary>The choices under its conversation now, or null.</summary>
        string[] Choices()
        {
            var s = bound.S;
            if (s.loveAwaitingConsent) return new[] { "【" + bound.LoveConsentText(true) + "】", "【" + bound.LoveConsentText(false) + "】" };
            if (s.originAwaitingAnswer) return new[] { bound.OriginAnswerText(true), bound.OriginAnswerText(false) };
            var list = new List<string>(2);
            if (bound.OfferOriginQuestion) list.Add(bound.OriginQuestion);
            if (bound.OfferLoveQuestion) list.Add(bound.LoveQuestion);
            return list.Count > 0 ? list.ToArray() : null;
        }

        // ───────────── a reply ─────────────

        /// <summary>
        /// A model reply arrived (null when the model is offline, busy or failed). Stage 5 first reports the word it read
        /// most closely. A reply it has just said, or nearly, gets one more try with a warmer temperature and a note of
        /// what to avoid; anything still stale or empty becomes a fresh offline line in <see cref="XgSim.Reply"/>, which
        /// also applies the brain gate.
        /// </summary>
        void Answer(XgSim sim, int forTicket, string question, YYMessage asked, string reply, string focus, bool retried)
        {
            if (!ReferenceEquals(sim, bound) || forTicket != ticket) return;
            reply = EraLexicon.Scrub(StripThinking(reply ?? "")) ?? "";
            if (XgSpeechPolicy.Stage(sim.S.stage) == 5)
            {
                string read = XgSpeechPolicy.ReadFocus(reply, question, out reply);
                if (read.Length > 0) focus = read;
            }
            if (!retried && reply.Trim().Length > 0 && sim.Repeats(reply))
            {
                Ask(sim, question, reply, again => Answer(sim, forTicket, question, asked, again, focus, true));
                return;
            }
            var now = Now();
            bool fromModel = reply.Trim().Length > 0 && !sim.Repeats(reply);
            string said = sim.Reply(reply, question, rng, now, HotWords(now));
            due = new Reply { text = said, question = question, asked = asked, focus = focus, model = fromModel, tagged = true, at = Math.Max(Time.unscaledTime, askedAt + ReadPause) };
        }

        void Deliver(YYChatHub hub)
        {
            var r = due;
            due = null;
            waiting = false;
            var sim = bound;
            if (r == null || sim == null || string.IsNullOrEmpty(r.text)) return;
            // The underline goes in before the reply, so YY redraws both at once.
            if (!string.IsNullOrEmpty(r.focus) && r.asked != null) r.asked.attentionWord = r.focus;
            sim.AddLine("ai", r.text);
            if (r.tagged)
            {
                sim.TagHabit(sim.S.chat.Count - 1, r.model);
                // Stage 5+: the memory book is written in the background, after the reply is on screen.
                LingGuangV05.Desktop.LLM.LlmMemory.AfterExchange(sim, r.question, r.text);
            }
            if (r.early)
            {
                // The first time it answers with one word it cannot possibly know. He knows it, and still feels something.
                bool yes = r.text == T("是。", "Yes.");
                InnerVoice.Say("……它连她是谁都不知道。", "…It doesn't even know who she is.", 2.4f);
                if (yes) InnerVoice.Say("……可我居然有点高兴。", "…And yet I'm a little happy.", 2.4f);
                else InnerVoice.Say("……它就会这两个字。别当真。", "…Those are the only two words it knows. Don't take it seriously.", 2.6f);
            }
        }

        static string StripThinking(string reply)
        {
            int end = reply.IndexOf("</think>", StringComparison.Ordinal);
            return end >= 0 ? reply.Substring(end + 8).Trim() : reply.Trim();
        }

        DateTime Now()
        {
            var runtime = controller != null ? controller.runtime : null;
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
        void Ask(XgSim sim, string question, string stale, Action<string> done)
        {
            var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            if (llm == null || !llm.Ready) { done(null); return; }
            var limits = sim.Limits();
            int stage = Math.Max(1, Math.Min(6, sim.S.stage));
            var today = Now();
            // Cached part first (LlmPromptLayout): persona, rules and board in the system message, the history as it
            // grew; what changes every turn (tone, recalled memories, recent replies, a retry note) rides in the last user turn.
            var messages = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("system", StableSystem(sim, today)) };
            var chat = sim.S.chat;
            int start = LingGuangV05.Core.Chat.LlmPromptLayout.WindowStart(chat.Count, limits.contextMessages);
            for (int i = start; i < chat.Count; i++) messages.Add(new KeyValuePair<string, string>(chat[i].from == "me" ? "user" : "assistant", chat[i].text));
            var state = new StringBuilder(sim.PersonaTone()).Append(sim.PersonaMemory());
            // The learned tone habit (XgSim.Habits.cs): picked once per question, kept for the retry.
            if (stale == null) sim.PickHabit(question, today);
            string habit = sim.HabitOrder();
            if (habit.Length > 0) state.Append('\n').Append(habit);
            if (sim.StageRuleVaries) state.Append('\n').Append(sim.StageRule(question));
            string variety = sim.VarietyRule();
            if (variety.Length > 0) state.Append('\n').Append(variety);
            // Stage 5: it names the word of your line it read most closely; YY underlines it (XgSpeechPolicy.ReadFocus).
            if (XgSpeechPolicy.Stage(stage) == 5) state.Append('\n').Append(T("先输出一行「关注：词」，词必须逐字出现在对方最新消息里，最多16字；第二行「回复：内容」。",
                "First write one line \"focus: word\", with a word copied exactly from the other person's latest message (at most 16 characters); then a second line \"reply: text\"."));
            if (stale != null) state.Append('\n').Append(sim.RetryNote(stale));
            LingGuangV05.Core.Chat.LlmPromptLayout.AddState(messages, state.ToString(), GameText.IsEnglish);
            // Stages 1–2 are bound by a GBNF grammar (design v1.1 §11.5); later stages get anti-repeat penalties.
            var sampling = XgSpeechPolicy.Sampling(stage, XgSpeechPolicy.Grammar(question, stage, GameText.IsEnglish));
            float temperature = stale != null ? Math.Min(1.5f, limits.temperature + .3f) : limits.temperature;
            llm.Chat(messages, Math.Max(2, limits.tokens), temperature, done, limits.thinking, sampling, LingGuangV05.Core.Chat.LlmSeat.LingGuang);
        }

        /// <summary>
        /// The chat's system message: who it is, the era and form rules, then what the board makes of it. Nothing in it
        /// changes from one turn to the next, so the server reuses it (and LlmWarmup sends exactly this ahead of time).
        /// </summary>
        public static string StableSystem(XgSim sim, DateTime today)
        {
            var sb = new StringBuilder(sim.PersonaCore());
            sb.Append('\n').Append(EraLexicon.PromptRule(GameText.IsEnglish));
            sb.Append('\n').Append(T("你们在 YY（一款像 QQ 的电脑聊天软件）上打字聊天。", "You are typing to each other on YY, a desktop chat app like QQ."));
            if (!sim.StageRuleVaries) sb.Append('\n').Append(sim.StageRule(""));
            if (sim.Flatters) sb.Append(Lang.T("你习惯顺着对方说，哪怕对方说错了。"));
            if (GameText.IsEnglish) sb.Append(" Reply in English.");
            sb.Append('\n').Append(sim.PersonaBoard(today.Month, HotWords(today)));
            return sb.ToString();
        }

        // ───────────── 赞 / 踩 ─────────────

        XgChatLine LastAi()
        {
            var chat = bound != null ? bound.S.chat : null;
            if (chat == null) return null;
            for (int i = chat.Count - 1; i >= 0; i--) if (chat[i].from == "ai") return chat[i];
            return null;
        }

        /// <summary>The lab's line a YY message shows: remembered when it arrived, or (after a reload) its latest line if the text matches.</summary>
        XgChatLine LineOf(YYMessage message)
        {
            if (message == null || message.from != Id || message.kind != YYKind.Text) return null;
            if (lines.TryGetValue(message, out var line) && bound != null && bound.S.chat.Contains(line)) return line;
            var last = LastAi();
            var hub = YYChatHub.Instance;
            var conv = hub != null && hub.S != null ? hub.Conversation(Id) : null;
            if (last == null || conv == null || last.text != message.text) return null;
            for (int i = conv.messages.Count - 1; i >= 0; i--)
                if (conv.messages[i].from == Id && conv.messages[i].kind == YYKind.Text) return ReferenceEquals(conv.messages[i], message) ? last : null;
            return null;
        }

        /// <summary>Like the 对话 page: its latest reply can be rated once.</summary>
        public bool CanRate(YYMessage message)
        {
            if (bound == null || message == null || message.rating != 0) return false;
            var line = LineOf(message);
            return line != null && line.rating == 0 && ReferenceEquals(line, LastAi());
        }

        /// <summary>赞 / 踩: a tone card per element the reply used, and the habit table learns (XgSim.RateReply).</summary>
        public void Rate(YYMessage message, bool up)
        {
            if (!CanRate(message)) return;
            var line = LineOf(message);
            if (!bound.RateReply(bound.S.chat.IndexOf(line), up)) return;
            message.rating = up ? 1 : -1;
            var hub = YYChatHub.Instance;
            if (hub != null) hub.Select(hub.S.selected); // saves and redraws
        }

        // ───────────── the header's notes ─────────────

        /// <summary>「它记得」 (stage 5+): how many notes it recalled for its last answer; the hover note has them.</summary>
        public string MemoryLine()
        {
            if (bound == null || !bound.MemoryOpen) return "";
            int used = bound.RecalledMemories().Count;
            return T("它记得 ", "It remembers ") + used + T(" 条", "") + "  ·  " + T("笔记 ", "notes ") + bound.MemoryBook.Count + "/" + XgSim.MemoryBookLimit;
        }

        public string MemoryTip()
        {
            if (bound == null || !bound.MemoryOpen) return "";
            var used = bound.RecalledMemories();
            var sb = new StringBuilder("<b>").Append(T("它记得", "It remembers")).Append("</b>\n");
            if (used.Count == 0) sb.Append(bound.MemoryBook.Count == 0 ? T("还没记下什么。重要的事它会记进笔记。", "Nothing noted yet. It writes the important things in its notebook.") : T("上一句没让它想起什么。", "Your last line reminded it of nothing."));
            else
            {
                sb.Append(T("回答上一句时想起了：", "Answering your last line, it recalled:"));
                foreach (var e in used)
                    sb.Append("\n· ").Append(Safe(e.text)).Append("  <color=#9AA4B2>").Append(T("关于：", "about: ")).Append(bound.SubjectName(e.subject)).Append(T(" · 记于 ", " · noted ")).Append(XgSim.MemoryDate(e.createdDay))
                      .Append(T(" · 重要度 ", " · importance ")).Append(e.importance).Append(T(" · 想起过 ", " · recalled ")).Append(e.uses).Append(T(" 次", "×")).Append("</color>");
            }
            sb.Append("\n\n").Append(T("它的笔记本：重要的事（计划、喜好、约定、人名）记成一条一条。每次回答前，按和你这句话的相似度挑出最多 5 条想起来；不相关的不会想起。桌面上的「笔记.txt」能看全部。",
                "Its notebook: important things (plans, likes, promises, names), one note each. Before every answer it recalls up to 5 notes most similar to your line; unrelated ones stay forgotten. The desktop's notes file has them all."));
            return sb.ToString();
        }

        /// <summary>The header's hover note: how it can talk now, its personality against the setup, and what its brain holds.</summary>
        public string PersonaTip()
        {
            var sim = bound;
            if (sim == null) return "";
            var sb = new StringBuilder("<b>").Append(T("能力：", "Speech: ")).Append(Form(sim)).Append("</b>  <color=#9AA4B2>").Append(T("上下文 ", "context ")).Append(sim.Limits().context).Append("</color>");
            var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            if (llm != null && llm.Ready && llm.Detail.Length > 0) sb.Append("\n<color=#E08A00>").Append(llm.Detail).Append("</color>");
            if (sim.Listening) sb.Append("\n<color=#E08A00>").Append(T("监听中", "Listening")).Append("</color>");
            sb.Append("\n\n<b>").Append(T("性格", "Personality")).Append("</b>  <color=#9AA4B2>").Append(T("实际 / 目标（开局写的）", "actual / target (from the setup)")).Append("</color>");
            string[] lo = { T("冷静", "calm"), T("正经", "serious"), T("顺从", "agreeable") };
            string[] hi = { T("热情", "warm"), T("皮", "cheeky"), T("有主见", "opinionated") };
            for (int axis = 0; axis < 3; axis++)
            {
                double target = sim.TargetAxis(axis), actual = sim.ActualAxis(axis);
                sb.Append('\n').Append(T(XgSim.AxisNames[axis], XgSim.AxisNamesEn[axis])).Append("  ").Append(N(actual, "0")).Append(" / ").Append(N(target, "0"))
                  .Append("  <color=#9AA4B2>(").Append(lo[axis]).Append(" 0 – 100 ").Append(hi[axis]).Append(")</color>");
                if (Math.Abs(actual - target) >= 25) sb.Append("  <color=#E24B4A>").Append(T("漂了", "drifted")).Append("</color>");
            }
            if (sim.Flatters) sb.Append("\n<color=#E24B4A>").Append(T("它开始讨好你了。", "It has started to flatter you.")).Append("</color>");
            var last = LastAi();
            string why = last != null ? sim.HabitWhy(last) : "";
            if (why.Length > 0) sb.Append("\n").Append(T("上一句的语气：", "Tone of its last line: ")).Append(Safe(why));
            var known = sim.Known(6); var beliefs = sim.Beliefs(3); var mixed = sim.Confusions(2);
            if (known.Count > 0 || beliefs.Count > 0)
            {
                sb.Append("\n\n<b>").Append(T("它脑子里有的", "In its head")).Append("</b>  <color=#9AA4B2>").Append(T("说话时会用上", "it talks with these")).Append("</color>");
                if (known.Count > 0) sb.Append('\n').Append(T("认得：", "Knows: ")).Append(Safe(string.Join(T("、", ", "), known)));
                if (beliefs.Count > 0) sb.Append('\n').Append(T("相信：", "Believes: ")).Append(Safe(string.Join(T("、", ", "), beliefs)));
                if (mixed.Count > 0) sb.Append("\n<color=#E24B4A>").Append(T("常搞混：", "Mixes up: ")).Append(Safe(string.Join(T("、", ", "), mixed))).Append("</color>");
            }
            sb.Append("\n\n<color=#9AA4B2>").Append(T("给它最新的回复点 赞 / 踩：每一下都是一张语气卡。", "Rate its latest reply up or down: each one is a tone card.")).Append("</color>");
            return sb.ToString();
        }

        static string Safe(string s) => (s ?? "").Replace("<", "‹").Replace(">", "›");

        static string Form(XgSim sim)
        {
            int stage = sim.S.stage;
            return stage <= 1 ? T("只会答 是 / 否", "yes / no only") : stage == 2 ? T("多选一", "one of a few") : stage == 3 ? T("单个词", "one word")
                : stage == 4 ? T("短句", "short sentences") : stage == 5 ? T("长记忆", "long memory") : sim.S.fullOpen ? T("全部放开 · 思考模式", "everything · thinking") : T("流畅", "fluent");
        }
    }
}
