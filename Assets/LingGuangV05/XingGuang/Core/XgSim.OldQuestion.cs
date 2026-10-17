using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// 老问题: one question asked again every time it learns an ability, so the player watches the same answer grow up — from
    /// a wrong 是 on day one to a sentence, a memory and a question of its own. The learned toast (ability 1) and the stage-up
    /// cutscene show the first-day answer next to today's, with the answers in between. Stateless: the ladder follows the
    /// abilities it has (and the track it chose for ability 3).
    /// </summary>
    public sealed partial class XgSim
    {
        public const string OldQuestionZh = "冰是烫的吗？", OldQuestionEn = "Is ice hot?";

        /// <summary>Its answer to the old question with <paramref name="ability"/> abilities (0 = day one, before any).</summary>
        public string OldAnswer(int ability, bool english)
        {
            switch (ability <= 0 ? 0 : ability >= AbilityCount ? AbilityCount : ability)
            {
                case 0: return english ? "Yes." : "是。";
                case 1: return english ? "No." : "否。";
                case 2: return english ? "Cold." : "凉的。";
                case 3:
                    return FirstTrack == "vision"
                        ? english ? "Cold. White. Melts in your hand." : "凉的。白的，一捂就化。"
                        : english ? "Ice… is… cold." : "冰……是……凉的。";
                case 4: return english ? "Ice is cold. Boiling water is hot." : "冰是凉的，烫的是开水。";
                case 5: return english ? "Ice is cold. You asked me that on day one." : "冰是凉的。这句你第一天就问过我。";
                default: return english ? "Ice is cold. You ask this every time I learn something. Afraid I'll go back?" : "冰是凉的。你每学会一样都要问一遍，是怕我变回去吗？";
            }
        }

        /// <summary>The old question: day one's answer before, the answer with <paramref name="ability"/> abilities after.</summary>
        public XgBeforeAfter OldQuestionPair(int ability)
            => new XgBeforeAfter(OldQuestionZh, OldQuestionEn, OldAnswer(0, false), OldAnswer(0, true), OldAnswer(ability, false), OldAnswer(ability, true));

        /// <summary>Every answer it has given the old question so far, day one first.</summary>
        public List<string> OldAnswerTrail(int ability, bool english)
        {
            var trail = new List<string>();
            for (int i = 0; i <= ability && i <= AbilityCount; i++) trail.Add(OldAnswer(i, english));
            return trail;
        }
    }
}
