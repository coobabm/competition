using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using LingGuangV05.Core.Chat;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using UnityEditor;
using UnityEngine;

namespace LingGuangV05.EditorTools
{
    /// <summary>
    /// Play QA for 林晴雯's chat: binds a memory-only save past the prologue (refuses while the scene runtime has
    /// useDiskSave on), sends her a fixed 15-line script through YYChatHub.Send as the YY input does, and logs for
    /// each of her replies whether it came from the model or the offline library and why (YYGirlfriend.ReplyLog).
    /// Turns 5 to 9 are a busy stretch: two other visible chats (灵光's seat and the shared seat) are kept running,
    /// as when 灵光 and the 网吧 group talk at the same time. The log goes to Evidence/Girlfriend.
    /// </summary>
    public static class GirlfriendChatPlayQa
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        static readonly string[] Script =
        {
            "在吗", "在干嘛呢", "我今天去网吧帮老周装机了", "你吃饭了没", "你们宿舍今天怎么样", "好无聊啊", "想你了",
            "你最近在追什么剧", "我给你买了个东西 过两天到", "哈哈", "嗯", "你考试准备得怎么样", "周末我可能要通宵调模型", "你生气了？", "那我去睡了 晚安",
        };
        const int BusyFrom = 4, BusyTo = 8;

        static string label = "";
        static int turn, logStart, convStart, loadIssued;
        static double turnStarted, quietSince;
        static bool running;
        static readonly StringBuilder Log = new StringBuilder();
        static readonly List<string> Sources = new List<string>();
        static readonly List<string> ModelLines = new List<string>();
        static int loadRunning;

        public static string Status => running ? "turn " + (turn + 1) + "/" + Script.Length : "idle; " + Summary();

        /// <summary>Binds the memory-only save and starts the script. <paramref name="tag"/> names the log file.</summary>
        public static string Start(string tag)
        {
            var rt = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
            if (!EditorApplication.isPlaying || rt == null || rt.useDiskSave) return "refused: needs Play with useDiskSave off";
            if (running) return "already running";
            var hub = YYChatHub.Instance;
            var gf = YYGirlfriend.Instance;
            var llm = LocalLlm.Instance;
            if (hub == null || gf == null || llm == null) return "YY hub, girlfriend or LocalLlm missing";
            if (!llm.Ready) return "model not ready yet: " + llm.State + " " + llm.Detail;
            if (rt.Sim.InPrologue)
            {
                var house = new LingGuangV05.Core.ChapterOneSim();
                house.S.prologue = 2; house.S.appInstallState = 2; house.S.money = 3000;
                house.S.aiName = "小灯"; house.S.aiSelf = "本机"; house.S.aiCallMe = "老大";
                rt.GetType().GetMethod("Attach", Flags).Invoke(rt, new object[] { house });
            }
            if (hub.S != null) hub.S.lingguangUnlocked = true;
            label = tag ?? "";
            turn = -1; loadIssued = 0; loadRunning = 0;
            Log.Length = 0; Sources.Clear(); ModelLines.Clear();
            running = true;
            turnStarted = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Step;
            EditorApplication.update += Step;
            return "started";
        }

        public static void Stop() { running = false; EditorApplication.update -= Step; }

        static void Step()
        {
            if (!running) return;
            if (!EditorApplication.isPlaying) { Stop(); return; }
            var hub = YYChatHub.Instance; var gf = YYGirlfriend.Instance; var llm = LocalLlm.Instance;
            if (hub == null || gf == null || gf.G == null || llm == null) return;
            var g = gf.G;
            if (!g.started) return;
            // Keep her up for the whole script: no good night, no sleep nag, no dozing off.
            g.dayGame = 0; g.asleepDay = -1; g.naggedDay = g.today;
            if (turn >= BusyFrom && turn <= BusyTo) KeepBusy(llm);
            double now = EditorApplication.timeSinceStartup;
            var conv = hub.Conversation(YYChatHub.GirlfriendId);
            if (turn < 0)
            {
                // Wait for her first message (「还没睡？」) to finish, then begin.
                if (conv == null || conv.messages.Count == 0 || hub.GirlfriendTyping) return;
                Next(hub, gf); return;
            }
            bool answered = false;
            for (int i = convStart; conv != null && i < conv.messages.Count; i++) if (conv.messages[i].from == YYChatHub.GirlfriendId) { answered = true; break; }
            bool idle = !hub.GirlfriendTyping && gf.ReplyLog.Count >= logStart;
            if (answered && idle) { if (quietSince < 0) quietSince = now; }
            else quietSince = -1;
            if ((answered && quietSince > 0 && now - quietSince > 5) || now - turnStarted > 150) { Record(hub, gf, conv, now - turnStarted > 150); Next(hub, gf); }
        }

        static void Next(YYChatHub hub, YYGirlfriend gf)
        {
            turn++;
            if (turn >= Script.Length) { Finish(); return; }
            var conv = hub.Conversation(YYChatHub.GirlfriendId);
            logStart = gf.ReplyLog.Count; convStart = conv != null ? conv.messages.Count + 1 : 0;
            quietSince = -1; turnStarted = EditorApplication.timeSinceStartup;
            hub.Send(YYChatHub.GirlfriendId, Script[turn]);
        }

        static void Record(YYChatHub hub, YYGirlfriend gf, YYConversation conv, bool timedOut)
        {
            var src = new List<string>();
            for (int i = logStart; i < gf.ReplyLog.Count; i++) src.Add(gf.ReplyLog[i]);
            var hers = new List<string>();
            for (int i = convStart; conv != null && i < conv.messages.Count; i++) if (conv.messages[i].from == YYChatHub.GirlfriendId && conv.messages[i].kind == YYKind.Text) hers.Add(conv.messages[i].text);
            string reply = "r:scripted";
            foreach (var s in src) if (s.StartsWith("r:", StringComparison.Ordinal)) reply = s;
            if (timedOut && hers.Count == 0) reply = "r:noanswer";
            Sources.Add(reply);
            foreach (var s in src) if (s.StartsWith("p:", StringComparison.Ordinal)) Sources.Add(s);
            if (reply.EndsWith(":model", StringComparison.Ordinal) || reply.Contains("retry")) ModelLines.Add(string.Join(" | ", hers));
            bool busy = turn >= BusyFrom && turn <= BusyTo;
            Log.Append(turn + 1).Append(busy ? " [busy]" : "").Append(" 他：").Append(Script[turn]).Append("  →  ").Append(string.Join(",", src.Count > 0 ? src : new List<string> { reply }))
               .Append("  她：").Append(string.Join(" | ", hers)).Append('\n');
        }

        /// <summary>Two other visible chats running: 灵光's seat and the shared seat, re-sent as each finishes.</summary>
        static void KeepBusy(LocalLlm llm)
        {
            if (loadRunning >= 2) return;
            var seat = loadIssued % 2 == 0 ? LlmSeat.LingGuang : LlmSeat.Others;
            loadIssued++; loadRunning++;
            var msgs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("system", "你是网吧群里的一个网友，用口语聊天。"),
                new KeyValuePair<string, string>("user", "随便聊聊今天网吧里发生的事，写长一点，至少八句。（第 " + loadIssued + " 次）"),
            };
            llm.Chat(msgs, 160, .8f, _ => loadRunning--, false, null, seat, LlmLane.Visible);
        }

        static string Summary()
        {
            int model = 0, total = 0;
            var why = new SortedDictionary<string, int>();
            foreach (var s in Sources)
            {
                if (!s.StartsWith("r:", StringComparison.Ordinal)) continue;
                total++;
                if (s == "r:model" || s.Contains("retry")) model++;
                why.TryGetValue(s, out int n); why[s] = n + 1;
            }
            var sb = new StringBuilder("replies " + total + ", model " + model);
            foreach (var kv in why) sb.Append("; ").Append(kv.Key).Append(' ').Append(kv.Value);
            int p = 0; foreach (var s in Sources) if (s.StartsWith("p:", StringComparison.Ordinal)) p++;
            if (p > 0) { sb.Append("; her own:"); foreach (var s in Sources) if (s.StartsWith("p:", StringComparison.Ordinal)) sb.Append(' ').Append(s.Substring(2)); }
            return sb.ToString();
        }

        static void Finish()
        {
            running = false;
            EditorApplication.update -= Step;
            Log.Append("SUMMARY ").Append(Summary()).Append('\n');
            Log.Append("MODEL REPLIES\n");
            foreach (var m in ModelLines) Log.Append("- ").Append(m).Append('\n');
            string dir = Path.Combine(Application.dataPath, "../Evidence/Girlfriend");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "chat-qa-" + (label.Length > 0 ? label : "run") + ".txt"), Log.ToString());
            Debug.Log("[GirlfriendChatPlayQa] " + Summary());
        }

        public static string Report() => Log.ToString();
    }
}
