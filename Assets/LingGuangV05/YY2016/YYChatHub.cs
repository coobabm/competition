using System;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;
using AppNames = LingGuangV05.Core.AppNames;

namespace LingGuangV05.Desktop.YY
{
    public enum YYKind { Text = 0, File = 1, System = 2 }
    public enum YYFileState { Offered = 0, Receiving = 1, Done = 2 }

    [Serializable]
    public sealed class YYMessage
    {
        /// <summary>Contact id, or "me".</summary>
        public string from = "";
        public string text = "";
        public string attentionWord = "";
        public double gameSeconds;
        public YYKind kind;
        public string file = "";
        public double sizeMB;
        public YYFileState fileState;
        public double received;
        /// <summary>A 斗图 sticker id (YYStickers) when this text message is a sticker; empty otherwise. Old saves load with "".</summary>
        public string sticker = "";
        /// <summary>灵光's replies: 1 赞, -1 踩, 0 not rated (the lab keeps the tone card; this is what YY shows).</summary>
        public int rating;
        /// <summary>The lab's AI wrote this line in the player's name (「让 灵光 代我回」 in 林晴雯's chat).</summary>
        public bool byAi;
    }

    [Serializable]
    public sealed class YYConversation
    {
        public string id = "";
        public int unread;
        public List<YYMessage> messages = new List<YYMessage>();
    }

    [Serializable]
    public sealed class YYState
    {
        public int version = 1;
        public string selected = YYChatHub.LaoZhou;
        /// <summary>The AI learned to talk (stage 3): it shows up as a contact.</summary>
        public bool lingguangUnlocked;
        public double linkedGameSeconds;
        /// <summary>An open question in chat: 老周 asks about a scam SMS, the group about a danmaku (labels for 灵光).</summary>
        public string quizFrom = "", quizDesk = "", quizText = "", quizWhy = "";
        public bool quizTruth;
        public int quizSpamMarks, quizDanmuMarks;
        /// <summary>One-off hand-labelling seeds already sent: 老周's numb hand (old saves only), 阿杰 asking you to label for him.</summary>
        public bool seedZhouHand, seedJieAsk;
        public List<YYConversation> conversations = new List<YYConversation>();
        /// <summary>林晴雯's hidden state (YYGirlfriend). Old saves load with a fresh one.</summary>
        public LingGuangV05.Core.Girlfriend.GirlfriendState girlfriend = new LingGuangV05.Core.Girlfriend.GirlfriendState();
    }

    public sealed class YYContact
    {
        public string id, name, nameEn, signature, signatureEn;
        public bool online, group;
        /// <summary>Not in the session list until unlocked (灵光).</summary>
        public bool hidden;
        public Color color;
    }

    /// <summary>
    /// The lab's side of the 灵光 conversation (XgYyTalk). The 对话 page moved into YY: the AI's replies, its suggested
    /// questions and the 赞 / 踩 on its replies come from the lab, which keeps one chat log for both.
    /// </summary>
    public interface ILingGuangTalk
    {
        /// <summary>The player's line, already in the conversation. True when the lab answers it (nobody else does).</summary>
        bool Heard(YYMessage line);
        /// <summary>A reply is on the way (YY shows 「对方正在输入…」).</summary>
        bool Thinking { get; }
        /// <summary>Whether this message can get a 赞 / 踩 now (only its latest reply, once).</summary>
        bool CanRate(YYMessage message);
        void Rate(YYMessage message, bool up);
        /// <summary>The header's 「它记得」 line (empty when there is nothing to show) and its hover note.</summary>
        string MemoryLine();
        string MemoryTip();
        /// <summary>The hover note on its name: how it can talk now, its personality and what its brain holds.</summary>
        string PersonaTip();
    }

    /// <summary>
    /// YY (2016) chat service: conversations, persistence, 老周's file transfer of 灵光.exe and his local help replies.
    /// Story `say` lines arrive through <see cref="Receive"/>; the player's messages raise runtime signal chat.sent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class YYChatHub : MonoBehaviour
    {
        public const string AppId = "yy";
        public const string LaoZhou = "laozhou";
        public const string Me = "me";
        public const string LingGuangId = "lingguang";
        /// <summary>林晴雯 「晴雯ˇ」 (YYGirlfriend.cs).</summary>
        public const string GirlfriendId = "qingwen";
        public const string FileName = AppNames.ExeZh;
        public const double FileSizeMB = 46.8;
        public const double TransferMBps = 4.6;

        public static YYChatHub Instance { get; private set; }

        public static readonly YYContact[] Contacts =
        {
            new YYContact { id = LaoZhou, name = "老周", nameEn = "Lao Zhou", signature = "AI 是下一个风口。这周三个会。", signatureEn = "AI is the next big thing. Three meetings this week.", online = true, color = new Color32(232, 93, 74, 255) },
            new YYContact { id = "cousin", name = "表姐", nameEn = "Cousin", signature = "加班中，勿扰", signatureEn = "Working late, do not disturb", online = false, color = new Color32(236, 151, 31, 255) },
            new YYContact { id = LingGuangId, name = AppNames.AiZh, nameEn = AppNames.AiEn, signature = "是。否。……", signatureEn = "Yes. No. …", online = true, hidden = true, color = new Color32(64, 84, 196, 255) },
            new YYContact { id = GirlfriendId, name = "晴雯ˇ", nameEn = "Qingwenˇ", signature = "今天的月亮好圆", signatureEn = "The moon is so round tonight", online = true, hidden = true, color = new Color32(240, 98, 146, 255) },
            new YYContact { id = "netbar", name = "下班屁股群", nameEn = "Clock-out Overwatch squad", signature = "群公告：今晚八点屁股车，满五开", signatureEn = "Notice: Overwatch at 8 tonight, five-stack", group = true, color = new Color32(64, 158, 255, 255) },
        };

        public ChapterOneRuntime runtime;
        public ChapterOneDesktopRouter router;
        public YYState S { get; private set; }
        public event Action Changed;
        /// <summary>The view registers itself so the hub knows what the player can see.</summary>
        public YYChatView View { get; set; }
        public bool IsTyping(string id) => pending.ContainsKey(id) || (id == GirlfriendId && GirlfriendTyping) || (id != null && id == TypingShown)
            || (id == LingGuangId && LingGuangTalk != null && LingGuangTalk.Thinking);
        /// <summary>The lab answers in 灵光's conversation (XgYyTalk registers itself); without it the generic replies below run.</summary>
        public ILingGuangTalk LingGuangTalk { get; set; }
        /// <summary>A cutscene (AiJoinsYy) shows 「对方正在输入…」 for this contact without a reply on the way.</summary>
        public string TypingShown { get; set; }
        /// <summary>林晴雯's 「对方正在输入…」, driven by YYGirlfriend (it comes and goes on its own).</summary>
        public bool GirlfriendTyping { get; set; }
        /// <summary>Her controller; the player's lines to her go there instead of the generic reply queue.</summary>
        public YYGirlfriend Girlfriend { get; set; }
        /// <summary>
        /// Since the prologue (design v1.1) 老周 is a forum friend (§9): only saves from before it still chat with him here.
        /// The AI is a contact once it has joined YY, shortly after its setup at stage 1 (AiJoinsYy, design 女友系统与YY里的AI §5).
        /// </summary>
        public bool IsVisible(YYContact c) => c != null && (!c.hidden || (c.id == LingGuangId && S.lingguangUnlocked))
            && !(c.id == LaoZhou && runtime != null && runtime.Sim != null && runtime.Sim.S.prologue != 0);

        const int MaxHistory = 200;
        /// <summary>Contacts with a reply on the way, and when it may start (a short "reading" pause).</summary>
        readonly Dictionary<string, float> pending = new Dictionary<string, float>();
        readonly Dictionary<string, string> quizReplies = new Dictionary<string, string>();
        readonly HashSet<string> inFlight = new HashSet<string>();
        ChapterOneSim linked;

        public static YYContact Contact(string id) { foreach (var c in Contacts) if (c.id == id) return c; return null; }

        public void Initialize(ChapterOneRuntime gameRuntime, ChapterOneDesktopRouter desktopRouter)
        {
            Instance = this;
            runtime = gameRuntime;
            router = desktopRouter;
            BindSave(runtime.Sim);
            runtime.Changed += OnRuntimeChanged;
            runtime.Saving += WriteToSave;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (runtime != null) { runtime.Changed -= OnRuntimeChanged; runtime.Saving -= WriteToSave; }
        }

        /// <summary>Chat history lives inside the 灵光 save (GameState.chatState), so it always matches the game it belongs to.</summary>
        void BindSave(ChapterOneSim sim)
        {
            linked = sim;
            YYState parsed = null;
            string json = sim != null ? sim.S.chatState : null;
            if (!string.IsNullOrEmpty(json))
            {
                try { parsed = JsonUtility.FromJson<YYState>(json); if (parsed != null && parsed.version != 1) parsed = null; }
                catch (Exception error) { Debug.LogWarning("YY 聊天记录读取失败：" + error.Message); parsed = null; }
            }
            if (parsed != null)
                foreach (var c in Contacts) if (parsed.conversations.Find(x => x.id == c.id) == null) parsed.conversations.Add(new YYConversation { id = c.id });
            S = parsed ?? Fresh();
            pending.Clear(); inFlight.Clear(); standing.Clear();
            Changed?.Invoke();
        }

        void WriteToSave()
        {
            if (S == null || runtime == null || runtime.Sim == null || runtime.Sim != linked) return;
            foreach (var conv in S.conversations)
                if (conv.messages.Count > MaxHistory) conv.messages.RemoveRange(0, conv.messages.Count - MaxHistory);
            runtime.Sim.S.chatState = JsonUtility.ToJson(S);
        }

        static YYState Fresh()
        {
            var s = new YYState();
            foreach (var c in Contacts) s.conversations.Add(new YYConversation { id = c.id });
            // A little history so the app feels lived in.
            s.conversations.Find(c => c.id == "cousin").messages.Add(new YYMessage { from = "cousin", text = "你那台电脑别老开着，电费贵。", gameSeconds = -86400 * 3 });
            var group = s.conversations.Find(c => c.id == "netbar");
            group.messages.Add(new YYMessage { from = "netbar", text = "[阿杰] 1080 这周五开卖，有人去排吗？", gameSeconds = -3600 * 5 });
            group.messages.Add(new YYMessage { from = "netbar", text = "[大伟] 周五下班开黑，谁来？", gameSeconds = -3600 * 4 });
            return s;
        }

        void OnRuntimeChanged()
        {
            if (runtime.Sim != linked) BindSave(runtime.Sim);
        }

        /// <summary>Persist now (e.g. right after 灵光.exe is received) through the one 灵光 save.</summary>
        public void SaveNow()
        {
            if (runtime != null && runtime.useDiskSave && !runtime.TestMode) runtime.SaveNow();
        }

        public YYConversation Conversation(string id) { return S.conversations.Find(c => c.id == id); }
        double Now => runtime != null && runtime.Sim != null ? runtime.Sim.S.gameSeconds : 0;
        public bool AppInstalled => runtime != null && runtime.Sim != null && runtime.Sim.AppInstalled;

        public bool IsShowing(string id)
        {
            return S != null && S.selected == id && DesktopNotifications.IsWindowVisible(View);
        }

        /// <summary>A message from a contact (story lines, replies).</summary>
        public void Receive(string id, string text)
        {
            Receive(id, text, true);
        }

        /// <summary>A scripted scene may already show and sound this arrival; only its duplicate notification is suppressed.</summary>
        public void Receive(string id, string text, bool notify)
        {
            var conv = Conversation(id);
            if (conv == null || string.IsNullOrEmpty(text)) return;
            conv.messages.Add(new YYMessage { from = id, text = text, gameSeconds = Now });
            if (!IsShowing(id)) conv.unread++;
            if (notify) NotifyReceived(id, text);
            EnsureFileOffer();
            Touch();
        }

        void NotifyReceived(string id, string text)
        {
            if (id == Me) return;
            var contact = Contact(id);
            string sender = contact == null ? id : GameText.T(contact.name, contact.nameEn);
            DesktopNotifications.Notify(runtime, "YY", sender, text, IsShowing(id),
                () => { if (router != null) router.Open(AppId, id); });
        }

        /// <summary>Records an explicit player choice from an authored scene; its reply is owned by StoryRunner.</summary>
        public bool SendAuthoredChoice(string id, string text)
        {
            if (S == null) return false;
            var conv = Conversation(id); text = (text ?? "").Trim();
            if (conv == null || text.Length == 0) return false;
            if (text.Length > 1000) text = text.Substring(0, 1000);
            conv.messages.Add(new YYMessage { from = Me, text = text, gameSeconds = Now });
            Touch(); return true;
        }

        public void Send(string id, string text)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0) return;
            if (text.Length > 300) text = text.Substring(0, 300);
            var conv = Conversation(id);
            if (conv == null) return;
            var line = new YYMessage { from = Me, text = text, gameSeconds = Now };
            conv.messages.Add(line);
            Touch();
            if (id == LaoZhou) runtime.RaiseSignal("chat.sent", LaoZhou);
            if (ChoicesFor == id) { Choices = null; ChoicesFor = null; Changed?.Invoke(); }
            if (scriptFor == id && script != null && script(text)) return;
            if (AnswerQuiz(id, text)) return;
            if (id == LingGuangId && LingGuangTalk != null && LingGuangTalk.Heard(line)) return;
            if (id == GirlfriendId) { if (Girlfriend != null) Girlfriend.OnPlayerLine(text); return; }
            // Everyone answers in their own time; the group only sometimes. Several quick lines get one answer.
            if (id == "netbar" && UnityEngine.Random.value > .7f) return;
            float pause = id == "cousin" ? 5f : id == "netbar" ? 3f : id == LingGuangId ? 1.2f : 1.5f;
            pending[id] = Time.unscaledTime + pause + Mathf.Min(2f, text.Length * .04f);
        }

        // ───────────── scripted turns (the prologue, design v1.1 §6 Step 7) ─────────────

        /// <summary>Replies offered as buttons above the input; picking one sends it like a typed line.</summary>
        public string[] Choices { get; private set; }
        public string ChoicesFor { get; private set; }
        Func<string, bool> script;
        string scriptFor;

        public void Offer(string id, string[] choices) { ChoicesFor = id; Choices = choices; Changed?.Invoke(); }

        readonly Dictionary<string, string[]> standing = new Dictionary<string, string[]>();

        /// <summary>
        /// Choices a conversation keeps offering by itself (灵光's suggested questions, XgYyTalk), shown there whenever
        /// no scene's choices (<see cref="Offer"/>) are up for it. Null or empty takes them away.
        /// </summary>
        public void Stand(string id, string[] choices)
        {
            if (id == null) return;
            standing.TryGetValue(id, out var old);
            bool none = choices == null || choices.Length == 0;
            if (none ? old == null : old != null && string.Join("\n", old) == string.Join("\n", choices)) return;
            if (none) standing.Remove(id); else standing[id] = (string[])choices.Clone();
            Changed?.Invoke();
        }

        /// <summary>The choices shown in a conversation: a scene's first, else the conversation's standing ones.</summary>
        public string[] ChoicesOf(string id)
        {
            if (id == null) return null;
            if (ChoicesFor == id && Choices != null) return Choices;
            return standing.TryGetValue(id, out var c) ? c : null;
        }

        /// <summary>The player's next lines to <paramref name="id"/> go to <paramref name="handler"/>; when it returns true, nobody answers on their own.</summary>
        public void Script(string id, Func<string, bool> handler) { scriptFor = id; script = handler; }
        public void EndScript() { scriptFor = null; script = null; Choices = null; ChoicesFor = null; Changed?.Invoke(); }

        // ───────────── replies (local model, with scripted fallback) ─────────────

        void TickReplies()
        {
            if (pending.Count == 0) return;
            foreach (var id in new List<string>(pending.Keys))
            {
                if (inFlight.Contains(id) || Time.unscaledTime < pending[id]) continue;
                inFlight.Add(id);
                var replyingState = S;
                var replyingSimulation = runtime.Sim;
                Reply(id, text =>
                {
                    if (!ReferenceEquals(S, replyingState) || !ReferenceEquals(runtime.Sim, replyingSimulation)) return;
                    inFlight.Remove(id);
                    pending.Remove(id);
                    if (!string.IsNullOrEmpty(text)) Receive(id, text);
                });
            }
        }

        void Reply(string id, Action<string> done)
        {
            if (quizReplies.TryGetValue(id, out var scripted)) { quizReplies.Remove(id); done(scripted); return; }
            var lab = FindLab();
            var conv = Conversation(id);
            var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            if (llm != null && llm.Ready && conv != null)
            {
                var messages = LingGuangV05.Desktop.LLM.LlmPersonas.Conversation(id, conv, runtime.Sim, lab);
                YYMessage userMessage = null;
                for (int i = conv.messages.Count - 1; i >= 0; i--) if (conv.messages[i].from == Me) { userMessage = conv.messages[i]; break; }
                var expectedState = S;
                llm.Chat(messages, LingGuangV05.Desktop.LLM.LlmPersonas.MaxTokens(id, lab), LingGuangV05.Desktop.LLM.LlmPersonas.Temperature(id), text =>
                {
                    if (!ReferenceEquals(S, expectedState)) { done(null); return; }
                    if (text == null) { done(Fallback(id, conv, lab)); if (id == LingGuangId && userMessage != null && lab != null) lab.RememberOffline(userMessage.text); return; }
                    text = LingGuangV05.Core.Era.EraLexicon.Scrub(text); // it is 2016: no later memes
                    if (id == LingGuangId)
                    {
                        int stage = LingGuangV05.Desktop.LLM.LlmPersonas.LingGuangStage(lab);
                        if (stage == 5)
                        {
                            string focus = XgSpeechPolicy.ReadFocus(text, userMessage != null ? userMessage.text : "", out var answer);
                            text = answer;
                            if (userMessage != null && focus.Length > 0 && conv.messages.Contains(userMessage)) { userMessage.attentionWord = focus; Touch(); }
                        }
                        text = XgSpeechPolicy.Constrain(text, stage, GameText.IsEnglish);
                        // A line it said a moment ago reads like a bot: swap it for a fresh one.
                        if (RepeatsInYY(conv, text)) text = Fallback(id, conv, lab);
                    }
                    done(Shape(id, text));
                    if (id == LingGuangId && userMessage != null) LingGuangV05.Desktop.LLM.LlmMemory.AfterExchange(lab, userMessage.text, text);
                }, false, LingGuangV05.Desktop.LLM.LlmPersonas.Sampling(id, lab),
                    id == LingGuangId ? LingGuangV05.Core.Chat.LlmSeat.LingGuang : LingGuangV05.Core.Chat.LlmSeat.Others);
                return;
            }
            done(Fallback(id, conv, lab));
            if (id == LingGuangId && lab != null && conv != null) for (int i = conv.messages.Count - 1; i >= 0; i--) if (conv.messages[i].from == Me) { lab.RememberOffline(conv.messages[i].text); break; }
        }

        /// <summary>Group lines keep the "[name] text" form the view renders as a sender label.</summary>
        static string Shape(string id, string text)
        {
            if (id != "netbar") return text;
            text = text.Trim();
            if (text.StartsWith("[", StringComparison.Ordinal)) return text;
            foreach (var who in new[] { "阿杰", "老板", "小刚" })
                foreach (var sep in new[] { "：", ":" })
                    if (text.StartsWith(who + sep, StringComparison.Ordinal)) return "[" + who + "] " + text.Substring(who.Length + 1).Trim();
            return "[阿杰] " + text;
        }

        string Fallback(string id, YYConversation conv, XgSim lab)
        {
            string last = "";
            if (conv != null) for (int i = conv.messages.Count - 1; i >= 0; i--) if (conv.messages[i].from == Me) { last = conv.messages[i].text; break; }
            switch (id)
            {
                case LaoZhou: return LaoZhouHelp.Reply(last, runtime.Sim, lab);
                case "cousin": return Lang.T("在上班，晚点说。早点睡，别熬夜。");
                case "netbar": return null;
                default:
                    int stage = LingGuangV05.Desktop.LLM.LlmPersonas.LingGuangStage(lab);
                    if (lab == null)
                        return XgSpeechPolicy.Constrain(stage <= 2 ? "……" : Lang.T("我。在学。"), stage, GameText.IsEnglish);
                    // The lab's offline pools (topics, time of day, personality) give the same variety as the 对话 page;
                    // a few tries keep it clear of its last lines in this YY window too.
                    string line = null;
                    for (int tries = 0; tries < 6; tries++)
                    {
                        line = lab.Reply(null, last, offlineRng, runtime != null && runtime.Sim != null ? GameCalendar.Now(runtime.Sim.S) : (DateTime?)null);
                        if (!RepeatsInYY(conv, line)) break;
                    }
                    return line;
            }
        }

        readonly System.Random offlineRng = new System.Random();

        /// <summary>Whether a 灵光 line matches one of its last few lines in this YY conversation.</summary>
        static bool RepeatsInYY(YYConversation conv, string text)
        {
            if (conv == null || string.IsNullOrEmpty(text)) return false;
            int seen = 0;
            for (int i = conv.messages.Count - 1; i >= 0 && seen < XgSim.FreshWindow; i--)
            {
                if (conv.messages[i].from == Me) continue;
                seen++;
                if (XgSpeechPolicy.Similar(text, conv.messages[i].text)) return true;
            }
            return false;
        }

        // ───────────── questions from chat (labels for 灵光) ─────────────

        /// <summary>
        /// Friends no longer quiz the player in chat: the user ruled out both 「这条短信是不是骗子」 and
        /// 「这条弹幕算夸还是骂」. A question still open in an old save can be answered (AnswerQuiz); no new one is asked.
        /// </summary>
        void CheckQuiz() { }

        void Ask(string from, string desk, XgPhrase p, string text)
        {
            S.quizFrom = from; S.quizDesk = desk; S.quizText = p.text; S.quizTruth = p.yes; S.quizWhy = p.why;
            Receive(from, text);
        }

        /// <summary>The player's message answers the open question if it reads as yes or no.</summary>
        bool AnswerQuiz(string id, string text)
        {
            if (string.IsNullOrEmpty(S.quizFrom) || id != S.quizFrom) return false;
            bool? answer = ReadAnswer(S.quizDesk, text);
            if (answer == null) return false;
            bool correct = answer.Value == S.quizTruth;
            var controller = FindAnyObjectByType<XingGuangController>();
            double pay = controller != null && controller.Sim != null ? controller.Sim.ChatLabel(S.quizDesk, correct, controller.Host) : 0;
            string reply;
            if (S.quizDesk == "spam")
                reply = correct
                    ? (S.quizTruth ? Lang.T("果然是垃圾短信，删了。还好问了你，差点就回了。") : Lang.T("哦，是正经短信啊，那我回一下。谢了。"))
                    : GameText.T("你确定？我刚问了别人……" + S.quizWhy + " 下次看仔细点。", "Sure? I just checked… " + S.quizWhy);
            else
                reply = correct ? "[小刚] " + Lang.T("懂哥，666") : "[阿杰] " + GameText.T("错啦，" + S.quizWhy, "Nope: " + S.quizWhy);
            quizReplies[id] = reply;
            var conv = Conversation(id);
            if (correct && pay > 0 && conv != null)
                conv.messages.Add(new YYMessage { from = "system", kind = YYKind.System, text = GameText.T(AppNames.AiZh + "：「" + XgCatalog.Dataset(S.quizDesk).name + "」样本 +1，+¥" + pay.ToString("0.00", CultureInfo.InvariantCulture), "灵光: +1 sample, +¥" + pay.ToString("0.00", CultureInfo.InvariantCulture)), gameSeconds = Now });
            S.quizFrom = S.quizDesk = S.quizText = S.quizWhy = "";
            pending[id] = Time.unscaledTime + 1.2f;
            Touch();
            return true;
        }

        static bool? ReadAnswer(string desk, string text)
        {
            string t = text.ToLowerInvariant();
            bool Any(params string[] words) { foreach (var w in words) if (t.Contains(w)) return true; return false; }
            if (desk == "danmu")
            {
                if (Any("骂", "不是夸", "吐槽", "否", "no")) return false;
                if (Any("夸", "是", "yes", "praise")) return true;
                return null;
            }
            if (Any("不是", "没事", "正常", "不像", "否", "正经", "no", "legit")) return false;
            if (Any("骗", "垃圾", "别信", "诈骗", "假的", "是", "yes", "scam")) return true;
            return null;
        }

        // ───────────── 灵光 learns to talk ─────────────

        /// <summary>
        /// The stage-3 cutscene (DesktopBridge/Story/AiJoinsYy.cs) registers here. It is asked every frame while the AI
        /// is due to join; true means it plays (or waits for a quiet moment) and sets lingguangUnlocked itself.
        /// Without it the old plain moment below runs.
        /// </summary>
        public static Func<YYChatHub, bool> JoinCutscene;

        void CheckLingGuangUnlock()
        {
            if (S.lingguangUnlocked || !AppInstalled) return;
            var lab = FindLab();
            // It joins right after its setup (named, stage 1). It can only say 是 / 否 then; it talks properly from ability 3.
            if (lab == null || lab.S.stage < 1 || runtime.Sim.InPrologue) return;
            if (JoinCutscene != null && JoinCutscene(this)) return;
            S.lingguangUnlocked = true;
            var conv = Conversation(LingGuangId);
            conv.messages.Add(new YYMessage { from = "system", kind = YYKind.System, text = GameText.T("「" + AppNames.AiZh + "」添加你为好友。", AppNames.AiEn + " added you as a friend."), gameSeconds = Now });
            Touch();
            pending[LingGuangId] = Time.unscaledTime + 1f;
            var presenter = FindAnyObjectByType<LingGuangV05.Desktop.Story.StoryDesktopPresenter>();
            if (presenter != null) presenter.ShowToast(GameText.T(AppNames.AiZh + "：……是。你好。", AppNames.AiEn + ": … yes. Hello."), () => { if (router != null) router.Open(AppId, LingGuangId); });
        }

        public void Select(string id)
        {
            if (Conversation(id) == null) return;
            S.selected = id;
            if (IsShowing(id)) Conversation(id).unread = 0;
            Touch();
        }

        public void MarkSeen()
        {
            if (S == null || !IsShowing(S.selected)) return;
            var conv = Conversation(S.selected);
            if (conv != null && conv.unread > 0) { conv.unread = 0; Touch(); }
        }

        public int UnreadTotal { get { int n = 0; foreach (var c in S.conversations) n += c.unread; return n; } }

        // ───────────── 灵光.exe file transfer ─────────────

        /// <summary>
        /// 老周 sends 灵光.exe right after his third intro line ("我打包好了……"). If the story already ran but the chat
        /// history is missing (older save), he repeats the gist and sends it again, so the player is never stuck.
        /// </summary>
        void EnsureFileOffer()
        {
            var conv = Conversation(LaoZhou);
            // Since the prologue (design v1.1 §6) the exe comes out of the picture archive; only older saves still wait for this file.
            if (conv == null || AppInstalled || runtime.Sim.S.prologue != 0) return;
            if (conv.messages.Exists(m => m.kind == YYKind.File && m.file == FileName)) return;
            int said = conv.messages.FindAll(m => m.from == LaoZhou && m.kind == YYKind.Text).Count;
            bool introDone = runtime.Sim.S.story != null && runtime.Sim.S.story.HasFired("lz_intro");
            // While the intro is still being typed (1–2 lines so far), wait for the line that announces the file.
            if (said < 3 && !(introDone && said == 0)) return;
            if (said == 0) conv.messages.Add(new YYMessage { from = LaoZhou, text = GameText.T(AppNames.AppZh + "我直接传给你了，点「接收」，下完它会出现在桌面上。", "I'm sending you " + AppNames.AppEn + " directly. Press Receive and it will appear on your desktop."), gameSeconds = Now });
            conv.messages.Add(new YYMessage { from = LaoZhou, kind = YYKind.File, file = FileName, sizeMB = FileSizeMB, gameSeconds = Now });
            if (!IsShowing(LaoZhou)) conv.unread++;
            NotifyReceived(LaoZhou, GameText.F("发来文件：{0}", "Sent a file: {0}", FileName));
            Touch();
        }

        public void AcceptFile(YYMessage message)
        {
            if (message == null || message.kind != YYKind.File) return;
            if (message.fileState == YYFileState.Done) { OpenFile(message); return; }
            if (message.fileState == YYFileState.Offered) { message.fileState = YYFileState.Receiving; Touch(); }
        }

        public void OpenFile(YYMessage message)
        {
            if (message != null && message.file == FileName && AppInstalled && router != null) router.Open("lingguang");
        }

        void TickTransfers(float dt)
        {
            foreach (var conv in S.conversations)
                for (int i = 0, count = conv.messages.Count; i < count; i++)
                {
                    var m = conv.messages[i];
                    if (m.kind != YYKind.File) continue;
                    if (m.file == FileName && AppInstalled && m.fileState != YYFileState.Done) { m.fileState = YYFileState.Done; m.received = m.sizeMB; Touch(); continue; }
                    if (m.file == FileName && !AppInstalled && m.fileState == YYFileState.Done) { m.fileState = YYFileState.Offered; m.received = 0; Touch(); continue; }
                    if (m.fileState != YYFileState.Receiving) continue;
                    m.received = Math.Min(m.sizeMB, m.received + TransferMBps * dt);
                    if (m.received >= m.sizeMB - 1e-6)
                    {
                        m.fileState = YYFileState.Done;
                        conv.messages.Add(new YYMessage { from = "system", kind = YYKind.System, text = "你已成功接收文件「" + m.file + "」，已放到桌面。", gameSeconds = Now });
                        if (m.file == FileName && runtime.Sim != null && !runtime.Sim.AppInstalled) runtime.Sim.InstallApp();
                        DesktopNotifications.Notify(runtime, "YY", Lang.T("文件接收完成"),
                            GameText.F("{0} 已放到桌面。", "{0} is on your desktop.", m.file), false, () => OpenFile(m));
                        SaveNow();
                    }
                    Changed?.Invoke();
                }
        }

        void Update()
        {
            if (S == null || runtime == null || runtime.Sim == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f);
            EnsureFileOffer();
            TickTransfers(dt);
            CheckLingGuangUnlock();
            CheckQuiz();
            CheckLabelSeeds();
            DeliverEra(dt);
            TickReplies();
            S.linkedGameSeconds = runtime.Sim.S.gameSeconds;
        }

        // ───────────── hand-labelling seeds (the auto-labelling realisation, see AutoLabelEpiphany) ─────────────

        public const int SeedZhouHandLabels = 120, SeedJieAskLabels = 220;

        /// <summary>
        /// Two one-off lines while the player labels by hand, scheduled by the hand-label count like the chat quizzes:
        /// 老周 grumbles that labelling leaves the hand numb (only in old saves, where he is still a YY contact; since
        /// the prologue he never starts a chat, so the same words are his floor in the forum's 挂机脚本 thread), and
        /// 阿杰 asks in the group whether you could label some for him too, since you are at it anyway.
        /// </summary>
        void CheckLabelSeeds()
        {
            if (S.seedZhouHand && S.seedJieAsk) return;
            if (!AppInstalled || runtime.TestMode || runtime.Sim.InPrologue) return;
            var lab = FindLab();
            if (lab == null) return;
            int hand = lab.S.handCorrect + lab.S.handWrong;
            if (!S.seedZhouHand && hand >= SeedZhouHandLabels)
            {
                S.seedZhouHand = true;
                if (IsVisible(Contact(LaoZhou))) Receive(LaoZhou, Lang.T("标注这活我也干过，标到最后手都是麻的。"));
                else Touch();
            }
            else if (!S.seedJieAsk && hand >= SeedJieAskLabels)
            {
                S.seedJieAsk = true;
                Receive("netbar", "[阿杰] " + Lang.T("听说你天天在摆渡众包上做标注？反正你都在标了，顺手帮我也标点呗，钱分你一半。"));
            }
        }

        // ───────────── the month's chatter (design v1.1 §14.1, era_events.json) ─────────────

        /// <summary>Where the group's 2016 chatter goes (the group is renamed 下班屁股群 with the forum, phase F).</summary>
        public const string EraGroup = "netbar";
        /// <summary>Real seconds between two dated group lines, so a new month does not arrive as a wall of text.</summary>
        public const float EraChatGap = 20;
        float eraWait;

        void DeliverEra(float dt)
        {
            if (runtime.TestMode) return;
            eraWait -= dt;
            if (eraWait > 0) return;
            eraWait = 2;
            var e = EraContent.TakeDue(runtime.Sim.S, LingGuangV05.Core.Era.EraEvents.YY);
            if (e == null) return;
            // The opening quiet window (OpeningQuiet): the line still lands in the group, without a popup or a sound.
            Receive(EraGroup, "[" + EraContent.Who(e) + "] " + EraContent.Text(e), OpeningQuiet.Allows(false));
            eraWait = EraChatGap;
        }

        static XgSim FindLab()
        {
            var lab = FindAnyObjectByType<XingGuangController>();
            return lab != null ? lab.Sim : null;
        }

        void Touch() { if (runtime != null && !runtime.TestMode) runtime.MarkDirty(); Changed?.Invoke(); }


        /// <summary>The save whose calendar dates chat lines (each line keeps the day it was written on).</summary>
        static GameState CalendarSave => Instance != null && Instance.runtime != null && Instance.runtime.Sim != null ? Instance.runtime.Sim.S : null;

        public static string Clock(double gameSeconds)
        {
            return GameCalendar.ClockFor(CalendarSave, Math.Max(0, gameSeconds)).ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        public static string Day(double gameSeconds)
        {
            var t = gameSeconds < 0 ? GameCalendar.Start.AddSeconds(gameSeconds) : GameCalendar.ClockFor(CalendarSave, gameSeconds);
            return GameText.IsEnglish ? t.ToString("MMM d", CultureInfo.InvariantCulture) : t.Month + "月" + t.Day + "日";
        }
    }

    /// <summary>老周's local, rule-based help for the 灵光 lab. Not a live AI; it only reads the current numbers.</summary>
    public static class LaoZhouHelp
    {
        static string T(string zh, string en) => GameText.T(zh, en);
        static string P(double v) => XgSim.Pct(v);

        public static string Reply(string input, ChapterOneSim sim, XgSim lab)
        {
            string q = (input ?? "").ToLowerInvariant();
            if (sim == null) return Lang.T("我在开会，晚点回你。");
            if (sim.InPrologue) return Lang.T("在忙，晚点说。");
            if (!sim.AppInstalled)
                return T("先把" + AppNames.AppZh + "收了：咱俩聊天记录里那个文件，点「接收」。", "Grab " + AppNames.AppEn + " first: the file in our chat, press Receive.");
            if (lab == null) return T(AppNames.AppZh + "还在启动，等一下再问。", AppNames.AppEn + " is still starting, ask again in a bit.");
            var v = lab.S.vision;
            double mnist = lab.Samples("mnist"), poems = lab.Samples("poems");
            if (Has(q, "nan", "发散", "学习率", "爆炸", "调参", "退步", "lr", "rate", "tuning"))
                return T("现在不用调参数了，学习率它自己挑。参数量和样本够，每轮就往上涨；偶尔会退步一点：数据太少、太脏，或者刚涨了一大截时更容易。道具里的 Dropout、数据增强、BatchNorm、预热都能让它少退。到现在退步过 " + lab.S.drops + " 次，学会的能力不会丢。",
                         "No more tuning: it picks its own rate. With enough parameters and samples every round goes up; now and then one goes down a little, more often when the data is scarce or dirty, or right after a big jump. Dropout, augmentation, BatchNorm and warm-up on the Items page make that rarer. Drops so far: " + lab.S.drops + "; nothing it learned is lost.");
            if (Has(q, "算术", "算数", "口算", "加减", "arithmetic", "sums", "math"))
                return T("算术桌开局就开着，加减乘除、百分数、分数，题是按规则现编的。最简单，报酬也最低，每条只算半条总样本；标对 40 题升一级。现在难度 " + lab.LevelOf("arith") + "，已标 " + lab.Samples("arith").ToString("0") + " 题。",
                         "The arithmetic desk is open from the start: + − × ÷, percentages, fractions, generated by rules. Easiest and lowest-paid; each sample counts half toward the total. Every 40 right answers raise the level. Level " + lab.LevelOf("arith") + ", " + lab.Samples("arith").ToString("0") + " labelled.");
            if (Has(q, "逻辑", "推理", "是非", "logic", "puzzle"))
                return T((lab.DeskOpen("logic") ? "逻辑题桌已经开放。" : "逻辑题桌尚未开放，要先完成组合阶段并买对应数据包。") + "题是现编的，永远做不完。标对 40 题升一级，越难给钱越多。答错了会告诉你为什么错。现在难度 " + lab.LogicLevel + "，已标 " + lab.Samples("logic").ToString("0") + " 题。",
                         (lab.DeskOpen("logic") ? "The logic desk is open. " : "Unlock the combination stage and its data pack first. ") + "Logic tasks are freshly generated and never run out. Every 40 right answers raise the level; harder pays more, and wrong answers show why. Level " + lab.LogicLevel + ", " + lab.Samples("logic").ToString("0") + " labelled.");
            if (Has(q, "连击", "combo"))
                return T("连击是标注和训练共用的：答对 +1，按一轮 +1，刷新纪录 +5。慢了、答错都清零。连击越高，钱和训练都越多，最多翻倍。你最高 ×" + lab.S.bestCombo + "。",
                         "The combo is shared: +1 per right card or epoch, +5 per record. Too slow or a miss resets it. Up to ×2 on pay and training. Your best: ×" + lab.S.bestCombo + ".");
            // Before the protagonist has the auto-labelling idea, 老周 only talks about training automation.
            if (Has(q, "自动", "挂机", "auto", "idle", "脚本", "crontab") && lab.AutoLabelHidden)
                return T("训练的自动化在「科技」的自动化那一行，从 crontab 到 AutoML。标注嘛，只能靠手。现在自动化 " + lab.AutoTrainLevel + " 级。",
                         "Training automation is in Research, the automation row, crontab to AutoML. Labelling? That's by hand. Level " + lab.AutoTrainLevel + " now.");
            if (Has(q, "自动", "挂机", "auto", "idle", "脚本", "crontab"))
                return T("两种自动：标注台的「自动答题」要检查点过 " + P(XgCatalog.AutoMinAccuracy) + "；训练的自动化在「科技」的自动化那一行，从 crontab 到 AutoML。现在自动化 " + lab.AutoTrainLevel + " 级。",
                         "Two kinds: auto-answer in the desk needs a checkpoint over " + P(XgCatalog.AutoMinAccuracy) + "; training automation is in Research, crontab to AutoML. Level " + lab.AutoTrainLevel + " now.");
            if (Has(q, "技能", "架构", "层", "宽", "tree", "skill", "layer", "alexnet", "lstm"))
                return T("「科技」用钱买层数、宽度、数据包和自动化；「道具」买新结构和技巧。模型自己配：用最好的结构、装得下的最大尺寸，技巧全开。参数量不够就再练也涨不动，那就去加宽、加深。按住节点不放就买了。",
                         "The tech tree sells layers, width, data packs and automation; the Items page sells structures and techniques. The model sets itself up: the best structure, the biggest size that fits, every technique on. Short of parameters it stops improving, so widen or deepen. Hold a node to buy it.");
            if (Has(q, "训练", "模型", "准确", "曲线", "过拟合", "检查点", "评估", "train", "model", "overfit", "checkpoint", "assess"))
                return T("训练页按「训练一轮」，按一下跑一轮，不用调参数。每轮练完自动考一次，打个分，刷新纪录才给钱，纪录自动存成检查点。现在 " + v.arch + "，验证集 " + P(v.valAcc) + "，最佳 " + Best(lab, v.dataset) + "。",
                         "Press Train for one epoch; there is nothing to tune. Every epoch ends with an exam; only records pay and they save the checkpoint. Now " + v.arch + ", validation " + P(v.valAcc) + ", best " + Best(lab, v.dataset) + ".");
            if (Has(q, "显卡", "显存", "gpu", "vram", "寻宝", "淘货", "喵鱼", "机箱", "硬件", "1080"))
                return T("新卡在「" + AppNames.ShopZh + "」买，旧卡去「" + AppNames.UsedZh + "」卖。" + AppNames.AppZh + "的算力和显存都算你的卡：现在 " + sim.S.gpuCount + " 张，算力 ×" + sim.CardCompute.ToString("0.##") + "，显存 " + (sim.MemoryCapacity / 1024).ToString("0") + " G。模型太大塞不下就得加卡。",
                         "New cards are on " + AppNames.ShopEn + ", old ones sell on " + AppNames.UsedEn + ". " + AppNames.AppEn + "'s compute and VRAM are your cards: " + sim.S.gpuCount + " now, compute ×" + sim.CardCompute.ToString("0.##") + ", " + (sim.MemoryCapacity / 1024).ToString("0") + " GB. Bigger models need more cards.");
            if (Has(q, "电", "欠费", "跳闸", "账单", "power", "bill"))
                return T("电费在「家庭」看。训练时显卡多吃一半的电，记在同一张单子上。现在待付 ¥" + sim.S.billDue.ToString("0.00") + "，钱包 ¥" + sim.S.money.ToString("0.00") + "。" + (sim.S.unpaidPower ? "已经停电了，先去缴费。" : ""),
                         "Check power in Home. Training adds half the cards' power to the same bill. Due ¥" + sim.S.billDue.ToString("0.00") + ", wallet ¥" + sim.S.money.ToString("0.00") + "." + (sim.S.unpaidPower ? " Power is cut: pay first." : ""));
            if (Has(q, "突破", "阶段", "必修", "研究", "stage", "breakthrough", "research"))
                return T("每个阶段有一组必修节点。买齐了、看懂了当前的瓶颈，科技前沿的突破节点才能买。现在是第 " + lab.S.stage + " 阶段。",
                         "Each stage has required nodes. Own them and understand the current bottleneck, then the breakthrough on the frontier opens. You are at stage " + lab.S.stage + ".");
            if (Has(q, "订单", "赚钱", "收入", "钱", "order", "contract", "money", "earn"))
                return T("一开始只能在摆渡众包的标注台手点挣钱。有了检查点就能在摆渡众包接「企业订单」，按秒给钱，比手点多得多。现在订单每秒 ¥" + lab.IncomePerSecond.ToString("0.0") + "。",
                         "At first you earn by hand at the labelling desk in Bodu Crowd. With a checkpoint you can take its business orders, which pay every second. Now ¥" + lab.IncomePerSecond.ToString("0.0") + "/s.");
            if (Has(q, "标注", "开始", "怎么", "你好", "在吗", "hello", "hi", "help", "start", "label"))
                return T("先去「摆渡众包」的「标注台」，看图点「是」或「否」。答对给钱、多一条样本；答错不给钱、连击清零、那条作废。攒够 " + XgCatalog.SamplesToTrain + " 条就能训练。开局有算术、常识判断、逻辑题和垃圾短信：算术最简单，常识判断专教它分是和否，逻辑题最值钱，其它桌以后开。你现在算术 " + lab.Samples("arith").ToString("0") + " 条，常识判断 " + lab.Samples("sense").ToString("0") + " 条，逻辑题 " + lab.Samples("logic").ToString("0") + " 条，垃圾短信 " + lab.Samples("spam").ToString("0") + " 条。",
                         "Open the labelling desk in Bodu Crowd and answer Yes or No. Right answers pay and add a sample; wrong ones pay nothing, reset the combo and are discarded. " + XgCatalog.SamplesToTrain + " samples unlock training. You start with arithmetic, common sense, logic and spam: arithmetic is easiest, common sense is what teaches it yes from no, logic pays best, the other desks open later. Arithmetic " + lab.Samples("arith").ToString("0") + ", common sense " + lab.Samples("sense").ToString("0") + ", logic " + lab.Samples("logic").ToString("0") + ", spam " + lab.Samples("spam").ToString("0") + ".");
            return T("我不一定马上回。可以问我：怎么开始、算术、逻辑题、训练、连击、科技、自动化、退步、显卡、电费、阶段、订单。",
                     "I may not answer right away. Ask me about: getting started, arithmetic, logic, training, combo, tech, automation, drops, cards, power, stages, orders.");
        }

        static string Best(XgSim lab, string dataset) { double b = lab.BestAcc(dataset); return b > 0 ? P(b) : "—"; }

        static bool Has(string q, params string[] words) { foreach (var w in words) if (q.Contains(w)) return true; return false; }
    }
}
