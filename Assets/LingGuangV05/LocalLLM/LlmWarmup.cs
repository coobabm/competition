using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Core.Chat;
using LingGuangV05.Core.Girlfriend;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using UnityEngine;

namespace LingGuangV05.Desktop.LLM
{
    /// <summary>
    /// The stable prompt prefix of each character slot, for LocalLlm's warm-up request. Side-effect free (it runs every
    /// few seconds while idle) and built by the same functions as the real requests, so it is byte-identical to their
    /// start. Null: nothing to warm (no save loaded yet, or the character is not in play).
    /// </summary>
    public static class LlmWarmup
    {
        static XingGuangController lab;
        static ChapterOneRuntime runtime;

        public static IList<KeyValuePair<string, string>> Prefix(LlmSeat seat)
        {
            if (runtime == null) runtime = Object.FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.Sim == null) return null;
            switch (seat)
            {
                case LlmSeat.LingGuang:
                    // The YY chat's system message (XgYyTalk; its requests then add history and the per-turn state).
                    if (lab == null) lab = Object.FindAnyObjectByType<XingGuangController>();
                    if (lab == null || lab.Sim == null) return null;
                    return new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("system", XgYyTalk.StableSystem(lab.Sim, GameCalendar.Now(runtime.Sim.S))) };
                case LlmSeat.Girlfriend:
                    var girl = YYGirlfriend.Instance != null ? YYGirlfriend.Instance.G : null;
                    if (girl == null || !girl.started) return null;
                    return GirlfriendPrompt.Prefix(girl, GfNow.Of(runtime.Sim.S), GameText.IsEnglish);
                default:
                    return null;
            }
        }
    }
}
