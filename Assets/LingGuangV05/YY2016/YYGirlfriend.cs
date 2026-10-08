using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core;
using LingGuangV05.Core.Girlfriend;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.YY
{
    /// <summary>
    /// 林晴雯 「晴雯ˇ」 in YY (design 女友系统与YY里的AI §3, §4.5, §4.6, §7). The rules live in Core
    /// (GirlfriendRules); this component is the "像真人" layer on top: when she answers (her schedule and the reply
    /// delay), how (typing per character, the hesitating 「嗯」, several bubbles with gaps), what she starts herself,
    /// the window shake, her signature and status, red packets both ways, gifts arriving from 淘货, 「让 灵光 代我回」,
    /// New Year's Eve and the epilogue line. Her words come from the local model (GirlfriendPrompt) or, offline,
    /// from the line library. Attaches itself next to the YY hub at scene load; never reads real user data.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class YYGirlfriend : MonoBehaviour
    {
        public static YYGirlfriend Instance { get; private set; }

        public YYChatHub hub;
        ChapterOneRuntime runtime;
        XingGuangController labController;
        GirlfriendState bound;
        public GirlfriendState G => hub != null && hub.S != null ? hub.S.girlfriend : null;
        static string T(string zh, string en) => GameText.T(zh, en);
        static bool English => GameText.IsEnglish;

        /// <summary>One message of hers being typed out: its bubbles, and what happens after the last one.</summary>
        sealed class Batch
        {
            public List<string> lines = new List<string>();
            public bool hesitate, proactive;
            /// <summary>False for a good night: she does not wait for an answer to it.</summary>
            public bool expectsReply = true;
            /// <summary>Her answer to a red packet she took gladly: it leads with 「谢谢哥哥」 and never hesitates into 「嗯」.</summary>
            public bool thanks;
            public Action after;
        }

        readonly Queue<Batch> outbox = new Queue<Batch>();
        Batch typing;
        int typingLine, phase;
        float phaseUntil;
        bool generating, replyPending;
        double replyAtGame, askAtGame, holdUntilGame;
        GfTurn pendingTurn;
        string pendingSituation = "";
        string[] pendingScripted;
        bool pendingRuleDecided, caughtPending;
        Action pendingAfter;
        double nextPacketAmount;
        bool nextLineByAi, aiInFlight;
        double aiDueGame = -1;
        float nextIdleCheck;
        string lastStatus = "", lastSignature = "";
        float nextCardCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void Attach()
        {
            var rt = FindAnyObjectByType<ChapterOneRuntime>();
            if (rt == null || rt.TestMode || rt.GetComponent<YYGirlfriend>() != null) return;
            rt.gameObject.AddComponent<YYGirlfriend>();
        }

        void Awake() { Instance = this; runtime = GetComponent<ChapterOneRuntime>() ?? FindAnyObjectByType<ChapterOneRuntime>(); }
        void OnDestroy() { if (Instance == this) Instance = null; if (hub != null && hub.Girlfriend == this) hub.Girlfriend = null; }

        XgSim Lab { get { if (labController == null) labController = FindAnyObjectByType<XingGuangController>(); return labController != null ? labController.Sim : null; } }
        int Stage => Lab != null ? Lab.S.stage : 0;
        GfNow Now => GfNow.Of(runtime.Sim.S);
        YYConversation Conv => hub.Conversation(YYChatHub.GirlfriendId);

        // ───────────── the loop ─────────────

        void Update()
        {
            if (hub == null) hub = YYChatHub.Instance;
            if (hub == null || hub.S == null || runtime == null || runtime.Sim == null) return;
            hub.Girlfriend = this;
            if (hub.View != null) YYGirlfriendBar.Ensure(hub.View);
            // Old saves have no state; a serializer that skipped the field initialisers leaves version 0.
            if (hub.S.girlfriend == null || (hub.S.girlfriend.version == 0 && !hub.S.girlfriend.started)) hub.S.girlfriend = new GirlfriendState();
            var g = hub.S.girlfriend;
            if (!ReferenceEquals(g, bound)) { GirlfriendRules.Repair(g); Rebind(g); }
            UpdateContact(g);
            if (runtime.TestMode) return;
            if (!g.started)
            {
                if (runtime.Sim.InPrologue) return;
                // One thing at a time after the setup (OpeningBeat): she appears once the AI has joined YY.
                if (!HerTurn()) return;
                GirlfriendRules.Begin(g, Now, Seed());
                runtime.MarkDirty();
            }
            var now = Now;
            var act = GirlfriendRules.Schedule(g, now);
            foreach (var e in GirlfriendRules.Tick(g, now)) Handle(e);
            foreach (var a in GirlfriendRules.Deliver(g, now)) Arrived(a);
            Finale(g, now);
            StepTyping(g);
            if (typing == null && !generating)
            {
                if (outbox.Count > 0 && act.Present && now.game >= holdUntilGame) StartBatch(outbox.Dequeue());
                else if (outbox.Count == 0 && replyPending && now.game >= askAtGame && act.Present && !WaitForModel(now)) Reply(g, now, act);
                else if (outbox.Count == 0 && !replyPending) Idle(g, now, act);
            }
            GuaranteeRefusal(g);
            StandIn(g, now);
            if (Time.unscaledTime >= nextCardCheck) { nextCardCheck = Time.unscaledTime + 1; LifeCards(g); }
        }

        /// <summary>The hidden 下班以后 cards she gives (XgSim.Cards.cs), read off her state once a second.</summary>
        void LifeCards(GirlfriendState g)
        {
            var lab = Lab;
            if (lab == null || g == null || !g.started) return;
            if (g.totalMessages > g.herMessages) lab.EarnSecret("life.gf.chat");
            if (GirlfriendRules.Has(g, GirlfriendRules.FlagFought)) lab.EarnSecret("life.gf.fight");
            if (GirlfriendRules.Has(g, GirlfriendRules.FlagMadeUp)) lab.EarnSecret("life.gf.makeup");
            if (GirlfriendRules.Has(g, GirlfriendRules.FlagCaughtAi)) lab.EarnSecret("life.gf.caught");
            if (GirlfriendRules.Has(g, GirlfriendRules.FlagIphone)) lab.EarnSecret("life.gf.iphone");
            if (GirlfriendRules.Has(g, GirlfriendRules.FlagNewYear)) lab.EarnSecret("life.gf.newyear");
            if (GirlfriendRules.Tier(g) == GirlfriendTier.Sweet) lab.EarnSecret("life.gf.sweet");
            foreach (var key in g.done) if (key.StartsWith("forgot:", StringComparison.Ordinal)) { lab.EarnSecret("life.gf.forgot"); break; }
        }

        void Rebind(GirlfriendState g)
        {
            bound = g;
            outbox.Clear(); typing = null; generating = false; replyPending = false; pendingTurn = null; pendingScripted = null; pendingAfter = null; holdUntilGame = 0;
            caughtPending = false; aiInFlight = false; aiDueGame = -1;
            hub.GirlfriendTyping = false;
        }

        /// <summary>A seed fixed per save when she first appears (stored in her state, so a reload rolls the same dice).</summary>
        int Seed()
        {
            unchecked
            {
                int h = (int)2166136261;
                foreach (char c in (runtime.Sim.S.chatState ?? "") + runtime.Sim.S.gameSeconds.ToString("0.000", CultureInfo.InvariantCulture)) h = (h ^ c) * 16777619;
                return h == 0 ? 1 : h;
            }
        }

        // ───────────── contact, status, signature ─────────────

        void UpdateContact(GirlfriendState g)
        {
            var c = YYChatHub.Contact(YYChatHub.GirlfriendId);
            if (c == null) return;
            c.hidden = !g.started;
            if (!g.started) return;
            var act = GirlfriendRules.Schedule(g, Now);
            c.online = act.presence != GfPresence.Asleep && act.presence != GfPresence.Out;
            if (g.signature.Length > 0) { c.signature = g.signature; c.signatureEn = g.signatureEn.Length > 0 ? g.signatureEn : g.signature; }
            string status = act.presence + "|" + g.signature;
            if (status != lastStatus) { lastStatus = status; Refresh(); }
        }

        /// <summary>Her header line in the YY window: 「● WiFi 在线 · 签名」.</summary>
        public string StatusLine()
        {
            var g = G;
            if (g == null || runtime == null || runtime.Sim == null) return "";
            var p = GirlfriendRules.Schedule(g, Now).presence;
            string dot = p == GfPresence.Wifi || p == GfPresence.FourG ? "<color=#4CAF50>●</color> " : "○ ";
            return dot + GirlfriendRules.PresenceText(p, English) + " · " + T(g.signature, g.signatureEn.Length > 0 ? g.signatureEn : g.signature);
        }

        /// <summary>Ask the YY view to redraw (the hub's change event fires on Select).</summary>
        void Refresh() { if (hub != null && hub.S != null && hub.View != null) hub.Select(hub.S.selected); }

        // ───────────── events from the rules ─────────────

        void Handle(GfEvent e)
        {
            switch (e.kind)
            {
                case GfEventKind.Say: Queue(e, true); break;
                case GfEventKind.Signature: Refresh(); break;
                case GfEventKind.Voice: Voice(e.key); break;
                case GfEventKind.Shake: Shake(e.key == "first.shake"); break;
                case GfEventKind.Choices: hub.Offer(YYChatHub.GirlfriendId, Pick(e)); break;
            }
        }

        static string[] Pick(GfEvent e) => (English ? e.en : e.zh).ToArray();

        void Queue(GfEvent e, bool proactive, Action after = null)
        {
            var b = new Batch { proactive = proactive, after = after, expectsReply = e.key != "goodnight" };
            b.lines.AddRange(English ? e.en : e.zh);
            if (b.lines.Count > 0) outbox.Enqueue(b);
        }

        void QueueLines(string[] pairs, bool proactive, Action after = null)
        {
            var b = new Batch { proactive = proactive, after = after };
            for (int i = 0; i + 1 < pairs.Length; i += 2) b.lines.Add(English ? pairs[i + 1] : pairs[i]);
            if (b.lines.Count > 0) outbox.Enqueue(b); else after?.Invoke();
        }

        /// <summary>The protagonist's first-time thoughts (design §7): first shake, first fight, first make-up, first caught AI reply. Once per save each (the rules' flags).</summary>
        static void Voice(string key)
        {
            switch (key)
            {
                case "first.shake": InnerVoice.Say("窗口抖了一下。……是她。", "The window shook. …It's her."); break;
                case "first.fight": InnerVoice.Say("……吵架了。为了什么来着。", "…We're fighting. What was it about again."); break;
                case "first.makeup": InnerVoice.Say("和好了。……这比调参难。", "Made up. …Harder than tuning a model."); break;
                case "first.caught": InnerVoice.Say("被发现了。它学我说话，没学像。", "Caught. It learned to talk like me. Not well enough."); break;
            }
        }

        // ───────────── his lines ─────────────

        /// <summary>The player sent her a line (YYChatHub.Send routes it here). Her answer is scheduled by her tier, mood and schedule.</summary>
        public void OnPlayerLine(string text)
        {
            var g = G;
            if (g == null || !g.started || runtime == null || runtime.Sim == null) return;
            var now = Now;
            bool byAi = nextLineByAi;
            nextLineByAi = false;
            if (byAi) MarkLastMineAsAi();
            double packet = nextPacketAmount;
            nextPacketAmount = 0;
            var turn = GirlfriendRules.OnPlayerLine(g, now, text, byAi);
            foreach (var e in turn.events) Handle(e);
            turn.events.Clear();
            // He promised her the weekend: the train ticket is paid on Saturday (ChapterOneSim.Bills.cs).
            if (turn.promised == "weekend") runtime.Sim.BookWeekendTrip();
            runtime.MarkDirty();
            if (!byAi) aiDueGame = -1;

            string situation = "";
            string[] scripted = null;
            bool ruled = false;
            Action after = null;
            if (packet > 0)
            {
                var o = GirlfriendRules.Packet(g, now, packet);
                situation = T(o.situationZh, o.situationEn);
                turn.thanks = o.thanks;
                scripted = o.lines; ruled = true;
                double amount = packet;
                if (o.returned) after = () => { runtime.Sim.S.money += amount; runtime.MarkDirty(); System(T("晴雯ˇ 退回了你的红包，¥" + Money(amount) + " 已退回钱包。", "Qingwenˇ sent your red packet back. ¥" + Money(amount) + " returned to your wallet.")); };
                else
                {
                    bool asks = o.asksMoney;
                    after = () =>
                    {
                        System(Lang.T("晴雯ˇ 领取了你的红包。"));
                        if (asks) hub.Offer(YYChatHub.GirlfriendId, English ? GirlfriendRules.MoneyAnswersEn : GirlfriendRules.MoneyAnswersZh);
                    };
                }
                // Code decides; the model may word it, but a fixed line is safer for the outcome.
                if (o.returned || o.result == GfPacketResult.TooMany) situation = "";
            }
            else if (turn.answered == "ai" || turn.answered == "work") { scripted = GirlfriendRules.MoneyReply(turn.answered); ruled = true; }
            else if (turn.answered == "admit" || turn.answered == "deny") { scripted = GirlfriendRules.CaughtReply(g, turn.answered); ruled = true; }
            else if (turn.wished != null && turn.wished.thanks.Length > 0) situation = T("他刚刚祝你" + turn.wished.zh + "快乐。", "He just wished you a happy " + turn.wished.en + ".");
            else if (turn.promised.Length > 0) situation = T("他答应了：" + (turn.promised == "qixi" ? "七夕那天视频。" : turn.promised == "weekend" ? "这周末来看你。" : "12 月 2 日一起看《你的名字。》。"), "He said yes: " + (turn.promised == "qixi" ? "a video call on Qixi." : turn.promised == "weekend" ? "coming to see you this weekend." : "watching Your Name together on 2 December."));

            // Several quick lines get one answer: keep the earliest answer time.
            if (!replyPending)
            {
                replyAtGame = now.game + GirlfriendRules.ReplyDelay(g, GirlfriendRules.Schedule(g, now));
                // Her words are asked for a little early, so the model's time is spent inside her delay.
                askAtGame = GirlfriendReplyPolicy.AskAt(now.game, replyAtGame);
                replyPending = true;
                pendingAfter = null;
            }
            // Quick lines answered together keep the first line's silence cues (查岗 after he vanished).
            if (pendingTurn != null)
            {
                turn.keptHerWaiting = Math.Max(turn.keptHerWaiting, pendingTurn.keptHerWaiting);
                turn.sinceHisLast = Math.Max(turn.sinceHisLast, pendingTurn.sinceHisLast);
                turn.thanks |= pendingTurn.thanks;
            }
            pendingTurn = turn;
            pendingSituation = situation;
            if (scripted != null) pendingScripted = scripted;
            pendingRuleDecided = ruled || pendingRuleDecided;
            if (after != null) { var prev = pendingAfter; pendingAfter = () => { prev?.Invoke(); after(); }; }
        }

        void MarkLastMineAsAi()
        {
            var conv = Conv;
            if (conv == null) return;
            for (int i = conv.messages.Count - 1; i >= 0; i--) if (conv.messages[i].from == YYChatHub.Me) { conv.messages[i].byAi = true; break; }
        }

        /// <summary>The model is still booting and her reply is not long overdue: wait for it rather than go offline.</summary>
        bool WaitForModel(GfNow now)
        {
            var llm = LocalLlm.Instance;
            return llm != null && GirlfriendReplyPolicy.WaitForBoot(llm.State == LocalLlm.Status.Starting, now.game, replyAtGame);
        }

        /// <summary>
        /// Time to answer him: a discovered AI reply, a scripted outcome, or the model (offline: the line library).
        /// Called a little before the reply is due; what it queues is held until <see cref="replyAtGame"/>.
        /// </summary>
        void Reply(GirlfriendState g, GfNow now, GfActivity act)
        {
            replyPending = false;
            holdUntilGame = replyAtGame;
            var turn = pendingTurn; var situation = pendingSituation; var scripted = pendingScripted; var after = pendingAfter; bool ruled = pendingRuleDecided;
            pendingTurn = null; pendingSituation = ""; pendingScripted = null; pendingAfter = null; pendingRuleDecided = false;
            if (caughtPending)
            {
                caughtPending = false;
                var events = GirlfriendRules.Caught(g, now);
                foreach (var e in events) if (e.kind == GfEventKind.Voice) Voice(e.key);
                var say = events.Find(e => e.kind == GfEventKind.Say);
                var choices = events.Find(e => e.kind == GfEventKind.Choices);
                Queue(say, false, () => { after?.Invoke(); if (choices != null) hub.Offer(YYChatHub.GirlfriendId, Pick(choices)); });
                return;
            }
            if (scripted != null && string.IsNullOrEmpty(situation)) { QueueReply(g, scripted, after); return; }
            Generate(g, now, act, situation, turn, ruled, false, scripted, after);
        }

        void QueueReply(GirlfriendState g, string[] pairs, Action after)
        {
            var b = new Batch { after = after, thanks = pairs.Length > 1 && pairs[0] == GirlfriendRules.ThanksZh };
            for (int i = 0; i + 1 < pairs.Length; i += 2) b.lines.Add(English ? pairs[i + 1] : pairs[i]);
            Hesitation(g, b);
            outbox.Enqueue(b);
        }

        /// <summary>Cold tier or a bad mood: typing, gone, typing again, and only 「嗯」 (design §3).</summary>
        static void Hesitation(GirlfriendState g, Batch b)
        {
            // Her thanks for a red packet is never swallowed by a hesitation.
            if (b.proactive || b.thanks || !GirlfriendRules.Hesitates(g)) return;
            b.hesitate = true;
            b.lines.Clear();
            b.lines.Add(Lang.T("嗯"));
        }

        /// <summary>
        /// Asks the local model for her words; falls back to <paramref name="fallback"/> or the line library only when
        /// the model is not running, the request fails or times out, or a retry was also unusable
        /// (GirlfriendReplyPolicy). A busy server is no reason: the request waits on her own seat.
        /// </summary>
        void Generate(GirlfriendState g, GfNow now, GfActivity act, string situation, GfTurn turn, bool ruled, bool proactive, string[] fallback, Action after, string topic = "")
        {
            var llm = LocalLlm.Instance;
            if (llm == null || !GirlfriendReplyPolicy.AskModel(llm.Ready))
            {
                LogReply(proactive, "offline:notready");
                Offline(g, now, proactive, fallback, after, topic, turn);
                return;
            }
            generating = true;
            Ask(llm, g, act, situation, turn, ruled, proactive, fallback, after, topic, 0, null);
        }

        /// <summary>
        /// One request on her own seat: a reply to him in the visible lane, a message she starts herself in the
        /// background lane. Broken JSON or a repeat is asked once more, hotter and with a 「换个说法」 hint in the state
        /// block; a second failure, no answer at all or the request's timeout fall back to the library.
        /// </summary>
        void Ask(LocalLlm llm, GirlfriendState g, GfActivity act, string situation, GfTurn turn, bool ruled, bool proactive, string[] fallback, Action after, string topic, int attempt, string hint)
        {
            var expected = g;
            bool english = English;
            var messages = GirlfriendPrompt.Messages(Conv, g, Now, act, english, situation, proactive, hint, turn);
            llm.Chat(messages, GirlfriendPromptText.MaxTokens, GirlfriendReplyPolicy.Temperature(attempt), raw =>
            {
                if (!ReferenceEquals(G, expected)) return;
                var r = raw != null ? GirlfriendPromptText.Parse(raw) : null;
                var repeated = r != null ? Repeated(r.msgs) : null;
                var verdict = GirlfriendReplyPolicy.Judge(raw != null, r, repeated != null && repeated.Count > 0, attempt);
                if (verdict == GfReplyVerdict.Retry)
                {
                    var current = LocalLlm.Instance;
                    if (current != null && current.Ready)
                    {
                        Ask(current, g, act, situation, turn, ruled, proactive, fallback, after, topic, attempt + 1, GirlfriendReplyPolicy.RetryHint(english, r == null, repeated));
                        return;
                    }
                    verdict = GfReplyVerdict.Fallback;
                }
                generating = false;
                if (verdict == GfReplyVerdict.Fallback)
                {
                    LogReply(proactive, raw == null ? "offline:failed" : r == null ? "offline:parse" : "offline:repeat");
                    Offline(g, Now, proactive, fallback, after, topic, turn);
                    return;
                }
                LogReply(proactive, attempt > 0 ? "model:retry" : "model");
                if (turn != null && !ruled) GirlfriendRules.ApplyModelDelta(g, turn, r.delta);
                if (turn != null)
                {
                    if (WorthRemembering(g, r.remember)) GirlfriendRules.RememberHim(g, r.remember);
                    // A quarrel starting or ending on this line (ApplyModelDelta) is a first-time thought.
                    foreach (var e in turn.events) if (e.kind == GfEventKind.Voice) Voice(e.key);
                    turn.events.Clear();
                }
                var b = new Batch { proactive = proactive, after = after, thanks = turn != null && turn.thanks };
                // A red packet she took gladly: 「谢谢哥哥」 leads, whatever the model began with.
                b.lines.AddRange(b.thanks ? GirlfriendRules.ThankFirst(r.msgs, english) : r.msgs);
                Hesitation(g, b);
                outbox.Enqueue(b);
                runtime.MarkDirty();
                // Her own slot keeps her prompt cached; a message she starts herself waits behind anything the player is waiting for.
            }, false, GirlfriendPrompt.Sampling(), LingGuangV05.Core.Chat.LlmSeat.Girlfriend, proactive ? LingGuangV05.Core.Chat.LlmLane.Background : LingGuangV05.Core.Chat.LlmLane.Visible);
        }

        /// <summary>Where each of her generated replies came from, newest last ("r:" a reply, "p:" her own message). Play QA reads it; not saved.</summary>
        public readonly List<string> ReplyLog = new List<string>();

        void LogReply(bool proactive, string source)
        {
            ReplyLog.Add((proactive ? "p:" : "r:") + source);
            if (ReplyLog.Count > 200) ReplyLog.RemoveAt(0);
        }

        void Offline(GirlfriendState g, GfNow now, bool proactive, string[] fallback, Action after, string topic, GfTurn turn = null)
        {
            generating = false;
            if (fallback != null) { if (proactive) QueueLines(fallback, true, after); else QueueReply(g, fallback, after); return; }
            var b = new Batch { proactive = proactive, after = after };
            b.lines.AddRange(topic.Length > 0
                ? GirlfriendLines.Topic(topic, English, g, XgSpeechPolicy.Similar)
                : GirlfriendLines.Pick(g, now.clock, English, XgSpeechPolicy.Similar, proactive ? null : LatestFromHim(), proactive ? null : turn));
            Hesitation(g, b);
            outbox.Enqueue(b);
        }

        /// <summary>
        /// The model's 「remember」 is kept only when it is new: not like a memory she already has (it tends to write
        /// the same plan down every turn), and not a non-event (「他没提…」) or plain affection, which crowded out the
        /// eight memory slots of her prompt and made her circle back to the same topic.
        /// </summary>
        static bool WorthRemembering(GirlfriendState g, string remember)
        {
            remember = (remember ?? "").Trim();
            if (remember.Length < 2) return false;
            foreach (var w in new[] { "没提", "没说", "没有提", "想我", "想你", "didn't mention", "did not mention", "misses me", "miss you" })
                if (remember.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0) return false;
            string bare = remember.StartsWith("他", StringComparison.Ordinal) ? remember.Substring(1) : remember;
            foreach (var m in g.memories)
            {
                string old = m.StartsWith("他说：", StringComparison.Ordinal) || m.StartsWith("她说：", StringComparison.Ordinal) ? m.Substring(3) : m;
                if (old.StartsWith("他", StringComparison.Ordinal)) old = old.Substring(1);
                if (XgSpeechPolicy.Similar(bare, old)) return false;
            }
            return true;
        }

        /// <summary>His latest line in her chat (the offline library answers its kind), or null.</summary>
        string LatestFromHim()
        {
            var conv = Conv;
            if (conv != null) for (int i = conv.messages.Count - 1; i >= 0; i--) if (conv.messages[i].from == YYChatHub.Me && conv.messages[i].kind == YYKind.Text) return conv.messages[i].text;
            return null;
        }

        /// <summary>Her new lines that are like one of her last five (a model line like that reads like a bot); empty when none.</summary>
        List<string> Repeated(List<string> msgs)
        {
            var found = new List<string>();
            var conv = Conv;
            if (conv == null || msgs.Count == 0) return found;
            int seen = 0;
            for (int i = conv.messages.Count - 1; i >= 0 && seen < 5; i--)
            {
                var m = conv.messages[i];
                if (m.from != YYChatHub.GirlfriendId || m.kind != YYKind.Text) continue;
                seen++;
                foreach (var line in msgs) if (line.Length >= 3 && XgSpeechPolicy.Similar(line, m.text) && !found.Contains(line)) found.Add(line);
            }
            return found;
        }

        // ───────────── typing it out (design §3) ─────────────

        void StartBatch(Batch b)
        {
            typing = b; typingLine = 0;
            if (b.hesitate) { phase = 10; phaseUntil = Time.unscaledTime + 2f + UnityEngine.Random.value * 2f; hub.GirlfriendTyping = true; }
            else { phase = 0; phaseUntil = Time.unscaledTime + (float)GirlfriendRules.TypingSeconds(b.lines[0]); hub.GirlfriendTyping = true; }
            Refresh();
        }

        void StepTyping(GirlfriendState g)
        {
            if (typing == null || Time.unscaledTime < phaseUntil) return;
            var b = typing;
            switch (phase)
            {
                case 10: phase = 11; hub.GirlfriendTyping = false; phaseUntil = Time.unscaledTime + 2f + UnityEngine.Random.value * 3f; Refresh(); return;
                case 11: phase = 12; hub.GirlfriendTyping = true; phaseUntil = Time.unscaledTime + 1f + UnityEngine.Random.value; Refresh(); return;
                case 12: Send(b.lines[0]); Finish(g, b); return;
                case 0:
                    Send(b.lines[typingLine]);
                    typingLine++;
                    if (typingLine >= b.lines.Count) { Finish(g, b); return; }
                    hub.GirlfriendTyping = false;
                    phase = 1; phaseUntil = Time.unscaledTime + (float)GirlfriendRules.BubbleGap(g);
                    Refresh();
                    return;
                case 1:
                    hub.GirlfriendTyping = true;
                    phase = 0; phaseUntil = Time.unscaledTime + (float)GirlfriendRules.TypingSeconds(b.lines[typingLine]);
                    Refresh();
                    return;
            }
        }

        void Send(string line)
        {
            hub.Receive(YYChatHub.GirlfriendId, line);
            GirlfriendLines.Note(G, line);
        }

        void Finish(GirlfriendState g, Batch b)
        {
            typing = null;
            hub.GirlfriendTyping = false;
            GirlfriendRules.OnHerLines(g, Now, b.lines, b.hesitate);
            if (!b.expectsReply) { g.awaitingReply = false; g.unansweredRun = 0; }
            runtime.MarkDirty();
            b.after?.Invoke();
            if (g.aiAuto && g.awaitingReply) aiDueGame = Now.game + 5;
            Refresh();
        }

        // ───────────── her first appearance ─────────────

        readonly OpeningTurn herTurn = new OpeningTurn();
        float waitingSince = -1;
        /// <summary>She appears anyway after this many seconds of waiting (the AI's scene could not play).</summary>
        const float HerPatience = 240;

        StoryDesktopPresenter presenter;

        bool HerTurn()
        {
            // A save past the opening (an older one that never met her) does not wait.
            var lab = Lab;
            if (lab != null && lab.AbilitiesCount >= OpeningQuiet.EndsWithAbility) return true;
            if (waitingSince < 0) waitingSince = Time.unscaledTime;
            if (presenter == null) presenter = FindAnyObjectByType<StoryDesktopPresenter>();
            if (herTurn.Ready(OpeningSequence.Turn(OpeningBeat.Qingwen, presenter), Time.unscaledTime)) return true;
            return Time.unscaledTime - waitingSince > HerPatience && !AiJoinsYy.Playing;
        }

        // ───────────── things she starts ─────────────

        /// <summary>While the opening quiet window is open, her own messages come at most every OpeningQuiet.QingwenSpacing seconds.</summary>
        float spacedUntil;

        void Idle(GirlfriendState g, GfNow now, GfActivity act)
        {
            if (Time.unscaledTime < nextIdleCheck) return;
            nextIdleCheck = Time.unscaledTime + 1;
            if (!act.Present) return;
            if (hub.ChoicesFor == YYChatHub.GirlfriendId && hub.Choices != null) return;
            bool quiet = OpeningQuiet.Active;
            if (quiet && Time.unscaledTime < spacedUntil) return;
            if (StartSomething(g, now, act) && quiet) spacedUntil = Time.unscaledTime + (float)OpeningQuiet.QingwenSpacing;
        }

        /// <summary>Whatever she starts by herself now, if anything. True when something was started.</summary>
        bool StartSomething(GirlfriendState g, GfNow now, GfActivity act)
        {
            GfEvent e;
            if ((e = GirlfriendRules.SleepNag(g, now, act)) != null) { Queue(e, true); return true; }
            if ((e = GirlfriendRules.Goodnight(g, now, act)) != null) { Queue(e, true); runtime.MarkDirty(); return true; }
            if (Stage >= 3 && (e = GirlfriendRules.MoneyQuestion(g, now, runtime.Sim.S.money)) != null)
            {
                Queue(e, true, () => hub.Offer(YYChatHub.GirlfriendId, English ? GirlfriendRules.MoneyAnswersEn : GirlfriendRules.MoneyAnswersZh));
                return true;
            }
            double her = GirlfriendRules.HerPacketDue(g, now);
            if (her > 0)
            {
                QueueLines(new[] { "考完了 请你喝奶茶[偷笑]", "Exam's done. Bubble tea's on me [偷笑]" }, true, () =>
                {
                    hub.Receive(YYChatHub.GirlfriendId, GirlfriendRules.PacketText(her, English));
                    runtime.Sim.S.money += her; runtime.MarkDirty();
                    System(T("你领取了 晴雯ˇ 的红包，¥" + Money(her) + " 已存入钱包。", "You opened Qingwenˇ's red packet: ¥" + Money(her) + " added to your wallet."));
                });
                return true;
            }
            e = GirlfriendRules.NextProactive(g, now, act);
            if (e == null) return false;
            runtime.MarkDirty();
            if (e.kind == GfEventKind.Shake) { Shake(e.key == "first.shake"); return true; }
            if (e.kind == GfEventKind.Topic) { Generate(g, now, act, GirlfriendRules.TopicPrompt(e.key, English), null, true, true, null, null, e.key); return true; }
            Queue(e, true);
            return true;
        }

        // ───────────── window shake (design §3: 暖档以上会抖动窗口) ─────────────

        void Shake(bool first)
        {
            if (first) Voice("first.shake");
            System(Lang.T("晴雯ˇ 给你发送了一个窗口抖动。"));
            QueueLines(new[] { "人呢[疑问]", "Hello?? [疑问]" }, true);
            var router = hub.router;
            if (router == null) return;
            router.Open(YYChatHub.AppId, YYChatHub.GirlfriendId);
            var binding = router.Get(YYChatHub.AppId);
            if (binding != null && binding.Window != null && binding.Window.windowContainer != null) StartCoroutine(ShakeWindow(binding.Window.windowContainer));
        }

        static IEnumerator ShakeWindow(RectTransform target)
        {
            yield return null;
            Vector2 origin = target.anchoredPosition;
            for (int i = 0; i < 16; i++)
            {
                target.anchoredPosition = origin + new Vector2((i % 2 == 0 ? 1 : -1) * 10, (i % 3 - 1) * 4);
                yield return new WaitForSecondsRealtime(.03f);
            }
            target.anchoredPosition = origin;
        }

        void System(string text)
        {
            var conv = Conv;
            if (conv == null) return;
            conv.messages.Add(new YYMessage { from = "system", kind = YYKind.System, text = text, gameSeconds = runtime.Sim.S.gameSeconds });
            runtime.MarkDirty();
            Refresh();
        }

        static string Money(double v) => v.ToString("0.00", CultureInfo.InvariantCulture);

        // ───────────── red packets (design §4.6) ─────────────

        /// <summary>The 红包 button in her chat: pays from the wallet; she may send it back.</summary>
        public bool SendPacket(double amount, out string problem)
        {
            problem = "";
            var g = G;
            if (g == null || !g.started || runtime == null || runtime.Sim == null) { problem = Lang.T("现在发不了。"); return false; }
            if (amount <= 0) return false;
            if (runtime.Sim.S.money + 1e-9 < amount) { problem = Lang.T("余额不足。"); return false; }
            runtime.Sim.S.money -= amount;
            runtime.MarkDirty();
            nextPacketAmount = amount;
            if (GirlfriendRules.IsLucky(amount)) Lab?.EarnSecret("life.gf.packet");
            hub.Send(YYChatHub.GirlfriendId, GirlfriendRules.PacketText(amount, English));
            return true;
        }

        public double Wallet => runtime != null && runtime.Sim != null ? runtime.Sim.S.money : 0;

        // ───────────── gifts (design §4.6, the 淘货 「送她」 tab) ─────────────

        /// <summary>Orders a gift for her from 淘货 and pays for it. Returns the shop's message.</summary>
        public bool OrderGift(string giftId, out string message)
        {
            message = "";
            var g = G;
            var gift = GirlfriendRules.Gift(giftId);
            if (g == null || !g.started || gift == null || runtime == null || runtime.Sim == null) { message = Lang.T("亲，现在下不了单哦。"); return false; }
            var now = Now;
            if (!GirlfriendRules.OnSale(gift, now.clock)) { message = Lang.T("亲，还没上架哦～"); return false; }
            if (GirlfriendRules.SoldOut(g, gift, now.day, now.clock)) { message = Lang.T("亲，今天已售罄，明天再来抢～"); return false; }
            if (runtime.Sim.S.money + 1e-9 < gift.price) { message = Lang.T("亲，余额不足哦。"); return false; }
            runtime.Sim.S.money -= gift.price;
            var o = GirlfriendRules.Order(g, now, giftId);
            runtime.MarkDirty();
            var arrive = GameCalendar.DateOf(o.arriveDay);
            message = gift.id == GirlfriendRules.GiftMilkTea
                ? Lang.T("下单成功！骑手已接单，预计 30 分钟送到她宿舍楼下。")
                : T("下单成功！包邮，预计 " + arrive.Month + " 月 " + arrive.Day + " 日送达。", "Ordered! Free shipping, arriving " + arrive.ToString("d MMMM", CultureInfo.InvariantCulture) + ".");
            return true;
        }

        void Arrived(GfArrival a)
        {
            if (a.refused)
            {
                runtime.Sim.S.money += a.order.price;
                PrologueDirector.Desk?.StoryPopup(Lang.T("淘货"), T("「" + a.gift.zh + "」被拒收，¥" + Money(a.order.price) + " 已退款。", "\"" + a.gift.en + "\" was refused. ¥" + Money(a.order.price) + " refunded."), 6);
            }
            else PrologueDirector.Desk?.StoryPopup(Lang.T("淘货"), T("您的宝贝「" + a.gift.zh + "」已签收。", "Your item \"" + a.gift.en + "\" has been signed for."), 6);
            runtime.MarkDirty();
            var events = a.events;
            QueueLines(a.lines, true, () => { foreach (var e in events) Queue(e, true, e.key == "money" ? (Action)(() => hub.Offer(YYChatHub.GirlfriendId, English ? GirlfriendRules.MoneyAnswersEn : GirlfriendRules.MoneyAnswersZh)) : null); });
        }

        // ───────────── New Year's Eve and the epilogue (design §7) ─────────────

        void Finale(GirlfriendState g, GfNow now)
        {
            var lab = Lab;
            if (lab != null && lab.S.ending.Length > 0 && !GirlfriendRules.Has(g, GirlfriendRules.FlagNewYear))
            {
                var e = GirlfriendRules.NewYearMessage(g);
                if (e != null)
                {
                    // 23:59 on the last night: she is up for this one whatever her schedule says.
                    var b = new Batch { proactive = true };
                    b.lines.AddRange(English ? e.en : e.zh);
                    outbox.Clear();
                    StartBatch(b);
                    runtime.MarkDirty();
                }
            }
            var s = runtime.Sim.S;
            if (s.newYearAt > 0 && s.gameSeconds - s.newYearAt >= EpilogueAfterNewYear && !g.done.Contains("epilogue"))
            {
                g.done.Add("epilogue");
                var line = GirlfriendRules.EpilogueLine(g);
                InnerVoice.Say(line.zh, line.en, 4f);
                runtime.MarkDirty();
            }
        }

        // ───────────── 让 灵光 代我回 (design §4.5) ─────────────

        /// <summary>The switch shows from stage 5. Ordinary messages it answers; her questions (a promise, a feeling) it hands back.</summary>
        public bool StandInAvailable => Stage >= 5 && G != null;

        /// <summary>Seconds after New Year's midnight before her epilogue line: after the quiet desktop and 360's balloon.</summary>
        const double EpilogueAfterNewYear = 40;

        /// <summary>Which of her questions (asking + day) the AI has already handed back, so it says so once.</summary>
        string refusedAsk = "";
        public string AiName => GirlfriendPrompt.AiName(Lab, English);

        public void SetStandIn(bool on)
        {
            var g = G;
            if (g == null || !StandInAvailable) return;
            g.aiAuto = on;
            if (on && g.awaitingReply) aiDueGame = Now.game + 5;
            runtime.MarkDirty();
        }

        void StandIn(GirlfriendState g, GfNow now)
        {
            if (!g.aiAuto || aiInFlight || aiDueGame < 0 || now.game < aiDueGame || !g.awaitingReply || typing != null || outbox.Count > 0 || generating) return;
            // 接线: with 本体 unplugged from its card it cannot type; her questions are still handed back below.
            if (g.asking.Length == 0 && Lab != null && !Lab.LifeJobLive(XgSim.LifeQingwen)) return;
            if (hub.ChoicesFor == YYChatHub.GirlfriendId && hub.Choices != null) return; // a question only he can answer
            aiDueGame = -1;
            var lab = Lab;
            if (lab == null) return;
            // Her question (come or not, the movie, where the money came from, was that the AI): only he can answer it.
            // The first time it stops mid-reply and says why; after that it just hands the question back.
            if (g.asking.Length > 0)
            {
                string ask = g.asking + "@" + g.askDay;
                if (refusedAsk == ask) return;
                refusedAsk = ask;
                if (!GirlfriendRules.Has(g, GirlfriendRules.FlagAiRefused))
                {
                    GirlfriendRules.Mark(g, GirlfriendRules.FlagAiRefused);
                    runtime.MarkDirty();
                    StartCoroutine(RefusalScene(g.asking));
                }
                else System(AiName + T("：这句要你自己回。", ": This one you answer yourself."));
                return;
            }
            var llm = LocalLlm.Instance;
            var expected = g;
            if (llm != null && llm.Ready)
            {
                aiInFlight = true;
                var limits = lab.Limits();
                llm.Chat(GirlfriendPrompt.StandIn(lab, Conv, English), Math.Max(48, Math.Min(120, limits.tokens)), limits.temperature, raw =>
                {
                    aiInFlight = false;
                    if (!ReferenceEquals(G, expected) || !expected.awaitingReply) return;
                    string text = GirlfriendPrompt.CleanStandIn(raw);
                    PostStandIn(expected, text.Length > 0 ? text : GirlfriendPrompt.OfflineStandIn(expected.aiReplies, English));
                }, false, XgSpeechPolicy.Sampling(XgSpeechPolicy.Stage(lab.S.stage)), LingGuangV05.Core.Chat.LlmSeat.LingGuang);
            }
            else PostStandIn(g, GirlfriendPrompt.OfflineStandIn(g.aiReplies, English));
        }

        /// <summary>
        /// 「她问的是你，不是我。」 The AI was answering for him; at her question it stops halfway, and the line is not a
        /// lecture but the limit of standing in: it has read the chats, and still the decision is his.
        /// </summary>
        System.Collections.IEnumerator RefusalScene(string asking)
        {
            System(AiName + T("：这句我不能替你答。", ": I can't answer this one for you."));
            yield return new WaitForSecondsRealtime(1.6f);
            InnerVoice.Say("你不是看过我们的聊天吗？", "Haven't you read all our chats?", 2.4f);
            yield return new WaitForSecondsRealtime(2.8f);
            string decide = asking != "weekend" && asking != "movie" && asking != "qixi"
                ? T("看过。可是怎么跟她说，要你决定。", "I have. But what you tell her is your decision.")
                : T("看过。可是去不去，要你决定。", "I have. But whether you go is your decision.");
            System(AiName + "：" + decide);
            yield return new WaitForSecondsRealtime(2f);
            System(AiName + T("：" + GirlfriendRules.AiRefusalZh, ": " + GirlfriendRules.AiRefusalEn));
        }

        /// <summary>
        /// The scene must not depend on the player having tried the switch: when her weekend question arrives and the AI has
        /// never handed one back, he lets it reply this once (the inner voice says so), and it stops at her question.
        /// </summary>
        void GuaranteeRefusal(GirlfriendState g)
        {
            if (Stage >= 5 && GirlfriendRules.Mark(g, GirlfriendRules.FlagStandInOffered)) runtime.MarkDirty();
            if (g.asking != "weekend" || g.aiAuto || Stage < 5 || GirlfriendRules.Has(g, GirlfriendRules.FlagAiRefused)) return;
            g.aiAuto = true;
            aiDueGame = Now.game + 6;
            InnerVoice.Say("……周末。", "…The weekend.", 1.6f);
            InnerVoice.Say("让它替我回吧。", "Let it answer for me.", 2.2f);
            runtime.MarkDirty();
        }

        void PostStandIn(GirlfriendState g, string text)
        {
            var mine = new List<string>();
            var conv = Conv;
            if (conv != null) for (int i = conv.messages.Count - 1; i >= 0 && mine.Count < 8; i--) if (conv.messages[i].from == YYChatHub.Me && !conv.messages[i].byAi && conv.messages[i].kind == YYKind.Text) mine.Add(conv.messages[i].text);
            double chance = GirlfriendRules.DetectChance(g, Now, GirlfriendRules.StyleDistance(mine, text));
            nextLineByAi = true;
            hub.Send(YYChatHub.GirlfriendId, text);
            if (GirlfriendRules.Roll(g) < chance) caughtPending = true;
        }
    }
}
