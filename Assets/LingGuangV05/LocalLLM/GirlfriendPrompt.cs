using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.Core.Girlfriend;
using LingGuangV05.Desktop.YY;
using LingGuangV05.XingGuang;
using AppNames = LingGuangV05.Core.AppNames;

namespace LingGuangV05.Desktop.LLM
{
    /// <summary>
    /// The request for 林晴雯's reply (design §4): her system prompt (GirlfriendPromptText), the few-shots, the last
    /// 12 chat lines (her bubbles regrouped into the JSON she answers in), and the sampler of §4.4 with the GBNF
    /// grammar. Also the prompt for 「让 灵光 代我回」, which answers her in the lab AI's own persona.
    /// </summary>
    public static class GirlfriendPrompt
    {
        /// <summary>§4.4: temperature .8, repeat 1.1, presence .3, DRY on, the grammar.</summary>
        public static XgSampling Sampling()
        {
            return new XgSampling
            {
                grammar = GirlfriendPromptText.Grammar(), topP = .9f, topK = 40,
                presencePenalty = GirlfriendPromptText.PresencePenalty, frequencyPenalty = 0, repeatPenalty = GirlfriendPromptText.RepeatPenalty,
                repeatLastN = 256, dryMultiplier = .6f,
            };
        }

        /// <summary>System prompt, few-shots and recent history. A proactive message ends on a nudge instead of his line.</summary>
        public static List<KeyValuePair<string, string>> Messages(YYConversation conv, GirlfriendState s, GfNow now, GfActivity a, bool english, string situation, bool proactive)
        {
            var list = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("system", GirlfriendPromptText.System(s, now, a, english, situation)) };
            list.AddRange(GirlfriendPromptText.FewShots(GirlfriendRules.Tier(s), english));
            var history = new List<YYMessage>();
            if (conv != null) foreach (var m in conv.messages) if (m.kind == YYKind.Text) history.Add(m);
            int start = Math.Max(0, history.Count - GirlfriendPromptText.HistoryLines);
            var hers = new List<string>();
            var his = new StringBuilder();
            void FlushHers() { if (hers.Count == 0) return; list.Add(new KeyValuePair<string, string>("assistant", Json(hers))); hers.Clear(); }
            void FlushHis() { if (his.Length == 0) return; list.Add(new KeyValuePair<string, string>("user", his.ToString())); his.Clear(); }
            for (int i = start; i < history.Count; i++)
            {
                var m = history[i];
                if (m.from == YYChatHub.Me) { FlushHers(); if (his.Length > 0) his.Append('\n'); his.Append(m.text); }
                else { FlushHis(); hers.Add(m.text); }
            }
            FlushHers(); FlushHis();
            // A chat must not start with her answer; drop a leading assistant turn left by the window.
            int first = 1 + GirlfriendPromptText.FewShots(GirlfriendTier.Normal, english).Count;
            if (list.Count > first && list[first].Key == "assistant") list.RemoveAt(first);
            if (proactive || list[list.Count - 1].Key != "user")
                list.Add(new KeyValuePair<string, string>("user", english ? "(He has not said anything. You message him first.)" : "（他没说话。你主动发一条消息。）"));
            return list;
        }

        static string Json(List<string> bubbles)
        {
            var sb = new StringBuilder("{\"msgs\":[");
            for (int i = 0; i < bubbles.Count && i < 3; i++) sb.Append(i > 0 ? "," : "").Append(XgSpeechPolicy.Json(bubbles[i]));
            return sb.Append("],\"delta\":0,\"remember\":\"\"}").ToString();
        }

        // ───────────── 让 灵光 代我回 (design §4.5) ─────────────

        /// <summary>
        /// The lab AI answers her in the protagonist's name: the 对话 page's persona, told who she is and that she
        /// thinks it is him. Its voice is its own, which is why she may notice.
        /// </summary>
        public static List<KeyValuePair<string, string>> StandIn(XgSim lab, YYConversation conv, bool english)
        {
            var hot = new List<string>();
            foreach (var w in XgMemes.HotWordsFor("A", lab.Today)) hot.Add(w.word);
            string incoming = "";
            if (conv != null) for (int i = conv.messages.Count - 1; i >= 0; i--) if (conv.messages[i].from == YYChatHub.GirlfriendId && conv.messages[i].kind == YYKind.Text) { incoming = conv.messages[i].text; break; }
            var sb = new StringBuilder(lab.PersonaPrompt(Math.Max(1, Math.Min(12, lab.Today / 100 % 100)), hot));
            string owner = lab.Profile.callMe.Length > 0 ? lab.Profile.callMe : (english ? "your owner" : "主人");
            sb.Append('\n').Append(english
                ? "You are answering YY messages for " + owner + ". The sender is his girlfriend, Lin Qingwen, a university student far away. She thinks it is " + owner + " writing. Never say you are a program. One or two short casual lines."
                : "现在你在 YY 上替" + owner + "回他女朋友林晴雯的消息。她在外地读大学，以为是" + owner + "本人在回。不要说你是程序，不要叫她「" + owner + "」。像真人打字，一两句，口语。");
            sb.Append('\n').Append(LingGuangV05.Core.Era.EraLexicon.PromptRule(english)).Append('\n').Append(lab.StageRule(incoming));
            if (english) sb.Append(" Reply in English.");
            var list = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("system", sb.ToString()) };
            if (conv != null)
            {
                int start = Math.Max(0, conv.messages.Count - 8);
                for (int i = start; i < conv.messages.Count; i++)
                {
                    var m = conv.messages[i];
                    if (m.kind != YYKind.Text) continue;
                    list.Add(new KeyValuePair<string, string>(m.from == YYChatHub.Me ? "assistant" : "user", m.text));
                }
            }
            if (list[list.Count - 1].Key != "user") list.Add(new KeyValuePair<string, string>("user", incoming.Length > 0 ? incoming : "在吗"));
            return list;
        }

        /// <summary>The AI's line as sent: no reasoning, no 「关注：」 label, no quotes, short.</summary>
        public static string CleanStandIn(string reply)
        {
            reply = reply ?? "";
            int end = reply.IndexOf("</think>", StringComparison.Ordinal);
            if (end >= 0) reply = reply.Substring(end + 8);
            int answer = reply.IndexOf("回复：", StringComparison.Ordinal);
            if (answer >= 0) reply = reply.Substring(answer + 3);
            else
            {
                int nl = reply.IndexOf('\n');
                if (reply.StartsWith("关注", StringComparison.Ordinal) && nl > 0) reply = reply.Substring(nl + 1);
            }
            reply = (LingGuangV05.Core.Era.EraLexicon.Scrub(reply) ?? "").Replace('\n', ' ').Trim().Trim('"', '「', '」', '“', '”');
            int colon = reply.IndexOfAny(new[] { '：', ':' });
            if (colon > 0 && colon <= 6) reply = reply.Substring(colon + 1).Trim();
            if (reply.Length > 60) reply = reply.Substring(0, 60) + "…";
            return LingGuangV05.Core.Chat.YYFaces.StripUnknown(reply);
        }

        /// <summary>Its offline lines: correct, polite and nothing like him.</summary>
        public static string OfflineStandIn(int pick, bool english)
        {
            string[][] pool =
            {
                new[] { "收到。我也想你。", "Received. I miss you too." },
                new[] { "好的。今天过得怎么样？", "OK. How was your day?" },
                new[] { "嗯。早点休息，注意身体。", "Mm. Rest early and take care of yourself." },
                new[] { "我在。我一直在。", "I'm here. I'm always here." },
                new[] { "明白。你说得对。", "Understood. You are right." },
            };
            var p = pool[((pick % pool.Length) + pool.Length) % pool.Length];
            return english ? p[1] : p[0];
        }

        /// <summary>Who answers for him in the toggle's label: the AI's own name.</summary>
        public static string AiName(XgSim lab, bool english) => lab != null && lab.Profile.name.Length > 0 ? lab.Profile.name : (english ? AppNames.AiEn : AppNames.AiZh);
    }
}
