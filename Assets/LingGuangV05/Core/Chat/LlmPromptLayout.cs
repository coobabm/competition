using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Chat
{
    /// <summary>
    /// How chat prompts are laid out so llama-server can reuse the previous turn's work (prompt prefix caching).
    /// The local model (Qwen3.5) is hybrid recurrent: the server can only resume from checkpoints it saves at the
    /// start of the last user message, so a turn is cheap only when the new prompt extends the previous one up to
    /// that point. Hence: the system message (persona, rules, slowly changing state) and the few-shots stay
    /// byte-identical between turns, history only grows, and everything that changes per turn (clock, memories,
    /// mood, recent replies, live numbers) rides in the last user turn as a 「当前状态」 block. Qwen's template
    /// accepts a single system message, first, so this block cannot be a second system message.
    /// </summary>
    public static class LlmPromptLayout
    {
        /// <summary>
        /// Puts <paramref name="state"/> in front of the last user turn (or adds a user turn holding only the state when
        /// the list does not end on one). Earlier user turns stay as they were sent, so the history is unchanged next turn.
        /// </summary>
        public static void AddState(IList<KeyValuePair<string, string>> messages, string state, bool english, string messageLabel = null)
        {
            if (messages == null || string.IsNullOrWhiteSpace(state)) return;
            string head = (english ? "[Current state, for you only; do not repeat it]\n" : "【当前状态（只给你参考，不要复述）】\n") + state.Trim();
            int last = messages.Count - 1;
            if (last >= 0 && messages[last].Key == "user")
            {
                string label = messageLabel ?? (english ? "[Their message]" : "【对方的消息】");
                messages[last] = new KeyValuePair<string, string>("user", head + "\n" + label + "\n" + messages[last].Value);
            }
            else messages.Add(new KeyValuePair<string, string>("user", head));
        }

        /// <summary>
        /// First history index to send when at least <paramref name="limit"/> recent items should be visible. The
        /// window moves in steps of half the limit (an even number, so user / reply pairs stay aligned) instead of
        /// one message per turn: between steps the prompt only grows, which is what the server's cache can reuse.
        /// The model then sees between limit and limit + step - 1 items.
        /// </summary>
        public static int WindowStart(int count, int limit)
        {
            if (limit <= 0) return Math.Max(0, count);
            if (count <= limit) return 0;
            // Tiny windows (stages 1–3 see one or two lines) are cheap anyway and must stay exact.
            if (limit < 4) return count - limit;
            int step = Step(limit);
            return (count - limit) / step * step;
        }

        /// <summary>The window step for a limit: half of it, even, at least 2.</summary>
        public static int Step(int limit) => Math.Max(2, limit / 2 / 2 * 2);
    }
}
