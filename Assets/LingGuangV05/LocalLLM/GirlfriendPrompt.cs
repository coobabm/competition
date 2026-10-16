using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.Core.Girlfriend;
using LingGuangV05.Desktop.YY;
using LingGuangV05.XingGuang;

namespace LingGuangV05.Desktop.LLM
{
    /// <summary>
    /// The request for 林晴雯's reply (design §4): her system prompt (GirlfriendPromptText) with the few-shots, the last
    /// 12 chat lines (her bubbles regrouped into the JSON she answers in), and the sampler of §4.4 with the GBNF
    /// grammar.
    /// </summary>
    public static class GirlfriendPrompt
    {
        /// <summary>§4.4: temperature .8, repeat 1.1, presence and frequency penalties against reused words, DRY on, the grammar.</summary>
        public static XgSampling Sampling()
        {
            return new XgSampling
            {
                grammar = GirlfriendPromptText.Grammar(), topP = .9f, topK = 40,
                presencePenalty = GirlfriendPromptText.PresencePenalty, frequencyPenalty = GirlfriendPromptText.FrequencyPenalty, repeatPenalty = GirlfriendPromptText.RepeatPenalty,
                repeatLastN = 256, dryMultiplier = .6f,
            };
        }

        /// <summary>
        /// The cached start of every request for her (LlmPromptLayout): the stable system prompt with the few-shots as
        /// labelled examples at its end (as chat turns the small model took them for real history and repeated their
        /// content). LlmWarmup sends exactly this ahead of time.
        /// </summary>
        public static List<KeyValuePair<string, string>> Prefix(GirlfriendState s, GfNow now, bool english)
        {
            string system = GirlfriendPromptText.Stable(s, now, english) + (english ? "You know nothing about the secret application on his computer and cannot see his computer or its training progress. Do not pretend to know; if he mentions a program you do not recognise, you have never used it." : "你不知道对方电脑里的秘密应用，也看不到电脑和训练进度。不能假装知情；如果他提到一个陌生程序，你从没用过。") + "\n\n" + GirlfriendPromptText.Examples(GirlfriendRules.Tier(s), english);
            return new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("system", system) };
        }

        /// <summary>
        /// Stable system prompt, few-shots and recent history; the per-message state (clock, mood, memories, plans,
        /// her life these days, situation, this turn's note and a retry's <paramref name="hint"/>) goes in front of
        /// his latest line. A proactive message ends on a nudge instead of his line. <paramref name="turn"/> (what his
        /// line did, may be null) picks this turn's texting moves together with her state and the clock.
        /// </summary>
        public static List<KeyValuePair<string, string>> Messages(YYConversation conv, GirlfriendState s, GfNow now, GfActivity a, bool english, string situation, bool proactive, string hint = null, GfTurn turn = null)
        {
            // His latest line picks which of her memories she is reminded of.
            string latest = null;
            if (conv != null) for (int i = conv.messages.Count - 1; i >= 0 && latest == null; i--) if (conv.messages[i].from == YYChatHub.Me && conv.messages[i].kind == YYKind.Text) latest = conv.messages[i].text;
            var list = Prefix(s, now, english);
            int first = list.Count;
            var history = new List<YYMessage>();
            if (conv != null) foreach (var m in conv.messages) if (m.kind == YYKind.Text) history.Add(m);
            // The window moves in steps, so between steps each request extends the previous one (cache reuse).
            int start = LingGuangV05.Core.Chat.LlmPromptLayout.WindowStart(history.Count, GirlfriendPromptText.HistoryLines);
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
            if (list.Count > first && list[first].Key == "assistant") list.RemoveAt(first);
            if (proactive || list[list.Count - 1].Key != "user")
                list.Add(new KeyValuePair<string, string>("user", english ? "(He has not said anything. You message him first.)" : "（他没说话。你主动发一条消息。）"));
            string recall = Recall(history, start, latest, english);
            string state = GirlfriendPromptText.State(s, now, a, english, situation, latest, recall) + "\n" + GirlfriendPromptText.Nudge(s, latest, english, proactive, turn, now.clock, !string.IsNullOrEmpty(situation));
            if (!string.IsNullOrEmpty(hint)) state += "\n" + hint;
            LingGuangV05.Core.Chat.LlmPromptLayout.AddState(list, state, english, english ? "[His message]" : "【他发来的】");
            return list;
        }

        /// <summary>Exchanges recalled from the chat before the window (each a line and the one after it).</summary>
        public const int RecalledExchanges = 2;

        /// <summary>
        /// The older exchanges his latest line brings back (RAG over the whole chat, not just the 12 lines in the
        /// prompt), written as 他：/ 你： lines. Empty when the history fits the window or nothing matches.
        /// </summary>
        public static string Recall(List<YYMessage> history, int windowStart, string latest, bool english)
        {
            if (history == null || windowStart <= 1 || string.IsNullOrWhiteSpace(latest)) return "";
            var lines = new List<string>(windowStart);
            for (int i = 0; i < windowStart; i++) lines.Add(Speaker(history[i], english) + history[i].text);
            var picked = LingGuangV05.Core.Chat.Rag.Episodes(GirlfriendPromptText.MemoryScorer, LingGuangV05.Core.Chat.MemoryQuery.Of(latest), lines, windowStart, RecalledExchanges);
            if (picked.Count == 0) return "";
            var sb = new StringBuilder(english ? "You two talked about this before:\n" : "你们以前聊过：\n");
            foreach (int i in picked)
            {
                sb.Append("- ").Append(lines[i]);
                if (i + 1 < windowStart) sb.Append(" / ").Append(lines[i + 1]);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        static string Speaker(YYMessage m, bool english) => m.from == YYChatHub.Me ? (english ? "He: " : "他：") : (english ? "You: " : "你：");

        /// <summary>The texts whose embeddings her next prompt needs (LocalEmbedder.Prepare): his latest line.</summary>
        public static List<string> QueryTexts(YYConversation conv)
        {
            var list = new List<string>();
            if (conv != null) for (int i = conv.messages.Count - 1; i >= 0; i--) if (conv.messages[i].from == YYChatHub.Me && conv.messages[i].kind == YYKind.Text) { list.Add(conv.messages[i].text); break; }
            return list;
        }

        static string Json(List<string> bubbles)
        {
            var sb = new StringBuilder("{\"msgs\":[");
            for (int i = 0; i < bubbles.Count && i < 3; i++) sb.Append(i > 0 ? "," : "").Append(XgSpeechPolicy.Json(bubbles[i]));
            return sb.Append("],\"delta\":0,\"remember\":\"\"}").ToString();
        }
    }
}
