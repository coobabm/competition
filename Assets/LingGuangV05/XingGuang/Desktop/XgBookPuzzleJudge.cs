using System;
using LingGuangV05.Core.Era;
using LingGuangV05.XingGuang;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Judges the owner's answer to its question about a book (XgSim.BookPuzzles.cs): the local model when it is ready
    /// (「对 / 差不多 / 不对」), else the puzzle's key words. Shared by the YY chat and the 书架 box.
    /// </summary>
    public static class XgBookPuzzleJudge
    {
        /// <summary>Judges and records the answer; <paramref name="done"/> gets its reply (null when nothing was pending or the save changed).</summary>
        public static void Answer(XgSim sim, string answer, Action<string> done)
        {
            var p = sim?.BookPuzzlePending;
            if (p == null) { done?.Invoke(null); return; }
            string id = p.id;
            Action<int?> finish = verdict =>
            {
                // The answer belongs to this question of this save; a late model reply after a switch is dropped.
                if (sim.S.bookPuzzle != id) { done?.Invoke(null); return; }
                done?.Invoke(sim.AnswerBookPuzzle(answer, verdict));
            };
            var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            if (llm == null || !llm.Ready) { finish(null); return; }
            llm.Chat(sim.BookJudgeMessages(p, answer), 24, .2f, reply => finish(XgSim.ParseVerdict(EraLexicon.Scrub(reply ?? "") ?? "")), false, null, LingGuangV05.Core.Chat.LlmSeat.LingGuang);
        }
    }
}
