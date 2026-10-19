using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.Tieba
{
    /// <summary>Public forum discussions and private messages; NPCs only see public posts and their own conversation.</summary>
    [DisallowMultipleComponent]
    public sealed partial class TiebaHub : MonoBehaviour
    {
        public const string ResourcePath = "LingGuangV05/Forum/forum";
        public static TiebaHub Instance { get; private set; }

        public ChapterOneRuntime runtime;
        public ForumState S { get; private set; }
        public ForumLibrary Library { get; private set; }
        public TiebaView View { get; set; }
        public event Action Changed;
        public bool IsTyping(string id) => pending.ContainsKey(id) || inFlight.Contains(id);

        /// <summary>Scripted replies offered as buttons (the prologue's Step 7).</summary>
        public string[] Choices { get; private set; }
        public string ChoicesFor { get; private set; }
        Func<string, bool> script;
        string scriptFor;

        readonly Dictionary<string, float> pending = new Dictionary<string, float>();
        readonly HashSet<string> inFlight = new HashSet<string>();
        readonly System.Random rng = new System.Random();
        ChapterOneSim linked;
        float nextAway;

        void Awake()
        {
            Instance = this;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            try { Library = ForumLibrary.Parse(asset != null ? asset.text : "{}"); }
            catch (FormatException error) { Debug.LogError("[Tieba] " + error.Message); Library = ForumLibrary.Parse("{}"); }
            RegisterZhouThread();
        }

        void Start()
        {
            if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime != null) { runtime.Saving += WriteToSave; runtime.Changed += OnRuntimeChanged; }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (runtime != null) { runtime.Saving -= WriteToSave; runtime.Changed -= OnRuntimeChanged; }
        }

        void OnRuntimeChanged() { if (runtime != null && !ReferenceEquals(runtime.Sim, linked)) Bind(runtime.Sim); }

        void Bind(ChapterOneSim sim)
        {
            linked = sim;
            ForumState parsed = null;
            string json = sim != null ? sim.S.forumState : null;
            if (!string.IsNullOrEmpty(json))
            {
                try { parsed = JsonUtility.FromJson<ForumState>(json); if (parsed != null && parsed.version != 1) parsed = null; }
                catch (Exception error) { Debug.LogWarning("贴吧记录读取失败：" + error.Message); }
            }
            S = parsed ?? new ForumState();
            S.zhouGone = false;
            if (parsed == null && sim != null && sim.InPrologue) AddHistory();
            RepairZhouQa();
            pending.Clear(); inFlight.Clear(); awayFor.Clear(); nextAway = 0;
            EndScript();
            Changed?.Invoke();
        }

        void WriteToSave() { if (runtime != null && runtime.Sim != null && S != null) runtime.Sim.S.forumState = JsonUtility.ToJson(S); }

        void Touch() { if (runtime != null && !runtime.TestMode) runtime.MarkDirty(); Changed?.Invoke(); }

        double Now => runtime != null && runtime.Sim != null ? runtime.Sim.S.gameSeconds : 0;

        /// <summary>Weeks of private messages with 周而复始 before the game (April–May 2016).</summary>
        void AddHistory()
        {
            var conv = S.Conversation(ForumLibrary.LaoZhou);
            if (conv.messages.Count > 0) return;
            int i = 0;
            foreach (var h in PrologueContent.Lines.History)
                conv.messages.Add(new ForumMessage { from = h.from == "me" ? ForumLibrary.Me : ForumLibrary.LaoZhou, text = GameText.T(h.zh, h.en), gameSeconds = h.days * 86400.0 + 60 * (i++) - 3600 * 3 });
        }

        // ───────────── what the forum shows today ─────────────

        public ForumContext Context()
        {
            var lab = Lab();
            return new ForumContext
            {
                stage = lab != null ? lab.S.stage : 1,
                today = runtime != null && runtime.Sim != null ? GameCalendar.Now(runtime.Sim.S).Date : GameCalendar.Start.Date,
                helpPosted = S != null && S.helpPosted,
                secondsSinceHelp = S != null && S.helpPosted ? Now - S.helpPostedAt : 0,
                english = GameText.IsEnglish,
                flag = id => lab != null && lab.ForumFlag(id),
            };
        }

        public static XgSim Lab()
        {
            // Shared by story/focus probes many times per frame. The owner registers once,
            // and its current Sim is read each time so loading another save cannot leave stale state.
            var controller = XingGuangController.Instance;
            return controller != null ? controller.Sim : null;
        }

        /// <summary>The help post can be written once, in stage 1 (design v1.1 stage 1 「贴吧求助帖」).</summary>
        public bool CanPostHelp => S != null && !S.helpPosted && runtime != null && runtime.Sim != null && !runtime.Sim.InPrologue && (Lab()?.S.stage ?? 1) <= 1;

        public void PostHelp()
        {
            if (!CanPostHelp) return;
            S.helpPosted = true; S.helpPostedAt = Now;
            Touch();
        }

        /// <summary>Data the player can post a 「求数据」 thread about now (thread id "ask_" + dataset in forum.json).</summary>
        public List<string> AskableData()
        {
            var lab = Lab();
            if (lab == null || runtime == null || runtime.Sim == null || runtime.Sim.InPrologue) return new List<string>();
            var list = lab.AskableData();
            list.RemoveAll(ds => Library.Get("ask_" + ds) == null);
            return list;
        }

        /// <summary>The player posts his own 「求数据」 thread; replies (and the offer) come later.</summary>
        public void PostDataRequest(string dataset)
        {
            var lab = Lab();
            if (lab != null && lab.AskForData(dataset)) Touch();
        }

        /// <summary>周而复始_ can be written to once his thread has shown up (stage 4).</summary>
        public bool CanMessage(string id)
        {
            if (IsDiscussion(id)) return DiscussionAvailable(id);
            if (id == ForumLibrary.LaoZhou) return true;
            if (id == ForumLibrary.ZhouNow) return S != null && (S.metZhouNow || Library.By(ForumLibrary.ZhouNow, Context()).Count > 0);
            return false;
        }

        public int Unread { get { int n = 0; if (S != null) foreach (var c in S.conversations) n += c.unread; return n; } }

        public ForumConversation Conversation(string id) => S?.Conversation(id);

        public void MarkSeen(string id) { var c = Conversation(id); if (c != null && c.unread > 0) { c.unread = 0; Touch(); } }

        /// <summary>A message arrives (从对方). <paramref name="seen"/> = the player is looking at that conversation.</summary>
        public void Receive(string id, string text, bool seen = false)
        {
            if (S == null || string.IsNullOrEmpty(text)) return;
            var conv = S.Conversation(id);
            conv.messages.Add(new ForumMessage { from = id, text = text, gameSeconds = Now });
            if (id != null && id.StartsWith("thread:", StringComparison.Ordinal))
            {
                var thread = Library.Get(id.Substring(7));
                if (thread != null && thread.author == ForumLibrary.Me)
                    Lab()?.RecordDailyOwnPostReply(thread.id, Guid.NewGuid().ToString("N"), true, true);
            }
            if (!seen) conv.unread++;
            if (id != ForumLibrary.Me)
                DesktopNotifications.Notify(runtime, IsDiscussion(id) ? "贴吧回复" : Lang.T("贴吧私信"),
                    IsDiscussion(id) ? DiscussionTitle(id) : id == ForumLibrary.ZhouNow ? "周而复始_" : id == ForumLibrary.LaoZhou ? "周而复始" : id,
                    text, seen || IsShowing(id), () => { if (View != null) View.Open("chat", id); });
            Touch();
        }

        public void Offer(string id, string[] choices) { ChoicesFor = id; Choices = choices; Changed?.Invoke(); }
        public void Script(string id, Func<string, bool> handler) { scriptFor = id; script = handler; }
        public void EndScript() { scriptFor = null; script = null; Choices = null; ChoicesFor = null; Changed?.Invoke(); }

        /// <summary>An explicit player-submitted scene request, without an unrelated automatic model reply.</summary>
        public bool SendAuthoredChoice(string id, string text)
        {
            text = NativeLaoZhouReplies.NormalizeInput(text);
            if (S == null || text.Length == 0 || !CanMessage(id)) return false;
            S.Conversation(id).messages.Add(new ForumMessage { from = ForumLibrary.Me, text = text, gameSeconds = Now });
            if (id == ForumLibrary.ZhouNow) S.metZhouNow = true;
            if (id == ForumLibrary.LaoZhou) LaoZhouQuestions.Observe(ZhouQa,"tieba:"+(S.Conversation(id).messages.Count-1)+":"+Now,"tieba_dm",text,Now);
            Touch(); return true;
        }

        public void Send(string id, string text)
        {
            if (TryZhouForumChoice(id, NativeLaoZhouReplies.NormalizeInput(text))) return;
            text = NativeLaoZhouReplies.NormalizeInput(text);
            if (S == null || text.Length == 0 || !CanMessage(id)) return;
            var conv = S.Conversation(id);
            conv.messages.Add(new ForumMessage { from = ForumLibrary.Me, text = text, gameSeconds = Now });
            if (id == ForumLibrary.ZhouNow) S.metZhouNow = true;
            Touch();
            if (id == ForumLibrary.LaoZhou) LaoZhouQuestions.Observe(ZhouQa,"tieba:"+(conv.messages.Count-1)+":"+Now,"tieba_dm",text,Now);
            if (runtime != null) runtime.RaiseSignal("chat.sent", "tieba:" + id);
            if (ChoicesFor == id) { Choices = null; ChoicesFor = null; Changed?.Invoke(); }
            if (scriptFor == id && script != null && script(text)) return;
            // 周而复始 answers in his own time: usually within seconds, sometimes he is "away" for a while (§2).
            float delay = 2.5f + (float)rng.NextDouble() * 4 + Mathf.Min(3, text.Length * .05f);
            if (id == ForumLibrary.LaoZhou && Time.unscaledTime >= nextAway && rng.NextDouble() < .15) { delay += 45 + (float)rng.NextDouble() * 45; nextAway = Time.unscaledTime + 600; awayFor.Add(id); }
            if (id == ForumLibrary.ZhouNow) delay += 4;
            pending[id] = Time.unscaledTime + delay;
        }

        readonly HashSet<string> awayFor = new HashSet<string>();

        void Update()
        {
            if (runtime == null || runtime.Sim == null) return;
            if (!ReferenceEquals(runtime.Sim, linked)) Bind(runtime.Sim);
            TickZhouQuestions();
            if (View != null) View.UpdateBadges(Unread > 0);
            // Outsiders do not react to the private application or its ending.
            if (pending.Count == 0) return;
            foreach (var id in new List<string>(pending.Keys))
            {
                if (inFlight.Contains(id) || Time.unscaledTime < pending[id]) continue;
                inFlight.Add(id);
                var state = S;
                float due = pending[id];
                bool delivered = false;
                Reply(id, text =>
                {
                    if (delivered || !ReferenceEquals(S, state)) return;
                    delivered = true;
                    inFlight.Remove(id);
                    if (pending.TryGetValue(id, out float current) && current == due) pending.Remove(id);
                    if (awayFor.Remove(id)) text = Lang.T("刚才网不好。") + text;
                    Receive(id, text, IsShowing(id));
                });
            }
        }

        /// <summary>
        /// §8 E-5: with the shutdown rule written (E1, or E3 choosing it) 周而复始's account is closed; without it he sends
        /// one last message — 「替我跟年轻的我说声加油。」 if you found the 2016 him, 「保重。」 if not.
        /// </summary>
        /// <summary>The conversation open on screen (set by the view), so its new messages are not counted unread.</summary>
        public string Showing { get; set; }

        bool IsShowing(string id)
        {
            return Showing == id && View != null && View.isActiveAndEnabled && DesktopNotifications.IsWindowVisible(View);
        }

        void Reply(string id, Action<string> done)
        {
            var conv = S.Conversation(id);
            string question = "";
            for (int i = conv.messages.Count - 1; i >= 0; i--) if (conv.messages[i].from == ForumLibrary.Me) { question = conv.messages[i].text; break; }
            string fallback = DiscussionFallback(id, question, conv.messages.Count);
            string context = IsDiscussion(id) ? DiscussionContext(id)
                : id == ForumLibrary.LaoZhou ? "你是老周，网名周而复始，省城某大学研二，实验室做深度学习，是个显卡发烧友，常在显卡吧泡着。对方是你在显卡吧认识的网友，聊过显卡、二手价和游戏，你们通过贴吧认识。只根据真正收到的消息了解对方。你懂 Caffe 和 TensorFlow，说话直、有点贫，常说「啧」「行吧」。"
                : id == ForumLibrary.ZhouNow ? "你是小周，网名周而复始_，是老周的师弟，研一，学翻译方向，导师让你用一张 GTX 970 跑 Caffe。网名和签名都是照师兄起的。你不认识对方。"
                : "对方是你在贴吧认识的网友，只聊过游戏和硬件。";
            context += Regard(id, Context().stage);
            if (id == ForumLibrary.LaoZhou) context += "\n" + LaoZhouQuestions.VisibleContext(ZhouQa,GameText.IsEnglish);
            string system = "现在是2016年。你是摆渡贴吧的普通网友，口语回复1到3句，不用列表。" +
                (id == ForumLibrary.LaoZhou ? (ZhouFinalContactContext ? LaoZhouQuestions.FinalPersonaRule(GameText.IsEnglish) : LaoZhouQuestions.EarlyPersonaRule(GameText.IsEnglish)) : "你不知道玩家电脑中的秘密应用，也看不到他的电脑、钱包或私人记录。你不是程序作者，也不是未来人。不要假装了解任何未公开的事。") +
                "围绕当前帖子和玩家刚才的话接着聊，可以问具体问题；不要重复自己上一条回复。\n" + context +
                "\n" + EraLexicon.PromptRule(GameText.IsEnglish);
            Ask(system, conv, 150, text => {
                string clean = Clean(text);
                if (clean != null && conv.messages.Exists(m => m.from != ForumLibrary.Me && m.text == clean)) clean = null;
                done(clean ?? fallback);
            });
        }

        void Ask(string system, ForumConversation conv, int maxTokens, Action<string> done)
        {
            var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            if (llm == null || !llm.Ready) { done(null); return; }
            var messages = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("system", system) };
            int start = Math.Max(0, conv.messages.Count - 10);
            for (int i = start; i < conv.messages.Count; i++)
                if (conv.id != ForumLibrary.LaoZhou || conv.messages[i].from == ForumLibrary.Me)
                    messages.Add(new KeyValuePair<string, string>(conv.messages[i].from == ForumLibrary.Me ? "user" : "assistant", conv.messages[i].text));
            llm.Chat(messages, maxTokens, .7f, reply => done(string.IsNullOrWhiteSpace(reply) ? null : reply));
        }

        static string Clean(string reply)
        {
            if (reply == null) return null;
            reply = EraLexicon.Scrub(reply.Trim());
            if (reply.Length == 0) return null;
            return reply.Length > 160 ? reply.Substring(0, 160) + "…" : reply;
        }

    }
}
