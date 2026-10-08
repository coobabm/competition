using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.Core.Girlfriend
{
    /// <summary>What to do with one model answer for her: show it, ask once more, or use the offline library.</summary>
    public enum GfReplyVerdict { Accept, Retry, Fallback }

    /// <summary>
    /// When her words come from the model and when from the offline library (GirlfriendLines). A busy server is
    /// not a reason to go offline: her request waits on her own slot, and her reply delay covers the wait. She
    /// goes offline only when the model is not running, the request failed or timed out, or a second try was
    /// also unusable (broken JSON, or the same thing she just said). Pure rules; the YY layer sends the requests.
    /// </summary>
    public static class GirlfriendReplyPolicy
    {
        /// <summary>The first try and one retry.</summary>
        public const int MaxAttempts = 2;
        /// <summary>A retry samples a little hotter so it does not land on the same words again.</summary>
        public const float RetryTemperatureStep = .15f;
        /// <summary>Game seconds before her reply is due that the request may already go out (at most this, and at most 60 % of the delay).</summary>
        public const double MaxLead = 8;

        /// <summary>Game seconds past her reply time she keeps waiting for a model that is still starting up.</summary>
        public const double BootWait = 20;

        /// <summary>Whether to ask the model at all. Only availability counts: how busy it is does not.</summary>
        public static bool AskModel(bool modelReady) => modelReady;

        /// <summary>The model is starting (Play just began): hold her reply for it, up to <see cref="BootWait"/> past its time.</summary>
        public static bool WaitForBoot(bool modelStarting, double now, double replyAt) => modelStarting && now < replyAt + BootWait;

        /// <summary>
        /// The verdict on one attempt (0 = first). No answer at all (failure or the request's own timeout) falls back
        /// at once: the budget is spent. Broken JSON or a repeat is retried once.
        /// </summary>
        public static GfReplyVerdict Judge(bool answered, GfModelReply reply, bool repeats, int attempt)
        {
            if (!answered) return GfReplyVerdict.Fallback;
            if (reply != null && !repeats) return GfReplyVerdict.Accept;
            return attempt + 1 < MaxAttempts ? GfReplyVerdict.Retry : GfReplyVerdict.Fallback;
        }

        public static float Temperature(int attempt) => GirlfriendPromptText.Temperature + Math.Max(0, attempt) * RetryTemperatureStep;

        /// <summary>
        /// When her request may go out: a little before the reply is due, so the time the model takes is part of
        /// her delay instead of added to it. Lines he sends before then are still answered together.
        /// </summary>
        public static double AskAt(double lineAt, double replyAt)
        {
            double delay = Math.Max(0, replyAt - lineAt);
            return replyAt - Math.Min(MaxLead, delay * .6);
        }

        /// <summary>
        /// The per-turn hint for a retry, for the 【当前状态】 block: say it differently (quoting what she must not
        /// repeat), or only output the JSON after a broken answer.
        /// </summary>
        public static string RetryHint(bool english, bool brokenFormat, IList<string> repeated)
        {
            if (brokenFormat) return english ? "Your last answer was not in the format. Output only the one JSON." : "你刚才没按格式回。只输出那一个 JSON。";
            var sb = new StringBuilder(english ? "Say it differently; do not repeat what you just said" : "换个说法，别重复你刚说过的话");
            if (repeated != null && repeated.Count > 0)
            {
                sb.Append(english ? " (not \"" : "（不要再说「");
                for (int i = 0; i < repeated.Count && i < 2; i++) sb.Append(i > 0 ? (english ? "\", \"" : "」「") : "").Append(repeated[i]);
                sb.Append(english ? "\")" : "」）");
            }
            return sb.Append(english ? ". Answer what he just said." : "。接着他刚说的话回。").ToString();
        }
    }
}
