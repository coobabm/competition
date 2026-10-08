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
    /// <summary>
    /// 摆渡贴吧 service (design v1.1 §2, §9): private messages with 周而复始 (the future 老周, the game's helper,
    /// who only answers) and 周而复始_ (the 2016 one, from stage 4), the player's optional help post, and the
    /// forum content. State lives in the save (GameState.forumState). Replies come from the local model when it runs,
    /// with the game state injected, and from <see cref="LaoZhouFuture"/> / <see cref="LaoZhouNow"/> otherwise.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TiebaHub : MonoBehaviour
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
            if (parsed == null && sim != null && sim.InPrologue) AddHistory();
            pending.Clear(); inFlight.Clear();
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
            var controller = FindAnyObjectByType<XingGuangController>();
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

        /// <summary>周而复始_ can be written to once his thread has shown up (stage 4).</summary>
        public bool CanMessage(string id)
        {
            if (id == ForumLibrary.LaoZhou) return S == null || !S.zhouGone;
            if (id == ForumLibrary.ZhouNow) return S.metZhouNow || Library.By(ForumLibrary.ZhouNow, Context()).Count > 0;
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
            if (!seen) conv.unread++;
            if (id != ForumLibrary.Me)
                DesktopNotifications.Notify(runtime, Lang.T("贴吧私信"),
                    id == ForumLibrary.ZhouNow ? "周而复始_" : id == ForumLibrary.LaoZhou ? "周而复始" : id,
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
            Touch(); return true;
        }

        public void Send(string id, string text)
        {
            text = NativeLaoZhouReplies.NormalizeInput(text);
            if (S == null || text.Length == 0 || !CanMessage(id)) return;
            var conv = S.Conversation(id);
            conv.messages.Add(new ForumMessage { from = ForumLibrary.Me, text = text, gameSeconds = Now });
            if (id == ForumLibrary.ZhouNow) S.metZhouNow = true;
            Touch();
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
            if (View != null) View.UpdateBadges(Unread > 0);
            CheckEnding();
            if (pending.Count == 0) return;
            foreach (var id in new List<string>(pending.Keys))
            {
                if (inFlight.Contains(id) || Time.unscaledTime < pending[id]) continue;
                inFlight.Add(id);
                var state = S;
                Reply(id, text =>
                {
                    if (!ReferenceEquals(S, state)) return;
                    inFlight.Remove(id); pending.Remove(id);
                    if (awayFor.Remove(id)) text = Lang.T("刚才网不好。") + text;
                    Receive(id, text, IsShowing(id));
                });
            }
        }

        /// <summary>
        /// §8 E-5: with the shutdown rule written (E1, or E3 choosing it) 周而复始's account is closed; without it he sends
        /// one last message — 「替我跟年轻的我说声加油。」 if you found the 2016 him, 「保重。」 if not.
        /// </summary>
        void CheckEnding()
        {
            if (S == null || S.endingHandled) return;
            var lab = Lab();
            if (lab == null || lab.S.ending.Length == 0) return;
            S.endingHandled = true;
            bool shutdown = lab.S.ending == "E1" || lab.S.ending == "E3-1";
            if (shutdown) S.zhouGone = true;
            else Receive(ForumLibrary.LaoZhou, S.metZhouNow ? Lang.T("替我跟年轻的我说声加油。") : Lang.T("保重。"));
            Touch();
        }

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
            bool english = GameText.IsEnglish;
            if (id == ForumLibrary.ZhouNow)
            {
                bool first = conv.messages.FindAll(m => m.from == ForumLibrary.ZhouNow).Count == 0;
                var labNow = Lab();
                bool finale = labNow != null && labNow.S.fullOpen, ended = labNow != null && labNow.S.ending.Length > 0;
                string offline = LaoZhouNow.Reply(question, first, english, finale, ended);
                if (first || ended || finale && offline.Contains("当然要写") || finale && offline.Contains("Of course you write")) { done(offline); return; }
                Ask(LaoZhouNow.SystemPrompt(english), conv, 60, reply => done(Clean(reply) ?? offline));
                return;
            }
            var facts = Facts();
            string topic = LaoZhouFuture.Topic(question, facts);
            int times = topic.Length > 0 ? S.Ask(topic) : 1;
            string fallback = LaoZhouFuture.Reply(question, facts, times, english);
            // Every topic he has a known answer for is answered from that: the clue lines must not be improvised around
            // the secret, and in play-testing the small local model gave wrong technical advice ("reinstall the driver"
            // for a NaN). The model only takes the open-ended chat.
            if (topic != "other" && topic != "hello") { done(fallback); return; }
            Ask(LaoZhouFuture.SystemPrompt(facts, english) + (times >= 2 ? (english ? " They asked this before: explain a bit more this time." : "对方之前问过这个问题，这次说透一点。") : (english ? " First time asked: just give a direction." : "第一次问：只给方向。")),
                conv, 90, reply => done(reply == null ? fallback : LaoZhouFuture.Clean(reply, question, facts, times, english)));
        }

        void Ask(string system, ForumConversation conv, int maxTokens, Action<string> done)
        {
            var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            if (llm == null || !llm.Ready) { done(null); return; }
            var messages = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("system", system) };
            int start = Math.Max(0, conv.messages.Count - 10);
            for (int i = start; i < conv.messages.Count; i++)
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

        /// <summary>What sophon.dll shows him (§9): the save and the lab, never the golden settings.</summary>
        public LaoZhouFacts Facts()
        {
            var f = new LaoZhouFacts();
            if (runtime != null && runtime.Sim != null)
            {
                var s = runtime.Sim.S;
                f.today = GameCalendar.Now(s).Date; f.money = s.money; f.temperature = s.temperature; f.gpus = s.gpuCount;
                f.aiName = s.aiName; f.callMe = s.aiCallMe;
            }
            var lab = Lab();
            if (lab != null)
            {
                f.stage = lab.S.stage; f.nanCount = lab.S.nanEvents;
                int next = lab.NextAbility;
                if (next > 0)
                {
                    f.nextAbility = XgSim.AbilityName(next, GameText.IsEnglish);
                    double p = lab.ParamsProgress(next), d = lab.SamplesProgress(next);
                    f.shortOf = p < 1 && p <= d ? "params" : d < 1 ? "data" : "";
                }
                f.emergedThisStage = lab.HasEmerged(lab.S.stage);
                f.finale = lab.S.fullOpen;
                if (lab.S.phenomena != null && lab.S.phenomena.seen.Count > 0)
                {
                    string id = lab.S.phenomena.seen[lab.S.phenomena.seen.Count - 1];
                    foreach (var p in XgPhenomena.All) if (p.id == id) f.phenomenon = GameText.T(p.name, p.nameEn);
                }
                foreach (var b in lab.S.best) f.bestAccuracy = Math.Max(f.bestAccuracy, b.acc);
            }
            return f;
        }
    }
}
