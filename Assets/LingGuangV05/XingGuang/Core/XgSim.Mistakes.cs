using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgBoardCard
    {
        /// <summary>The label card this example was made from (null for generated wall tasks such as 异或 or 长句).</summary>
        public XgCard source;
    }

    /// <summary>
    /// One exam card the model got wrong at the last evaluation: the card, what it said, and the kind of mistake (the
    /// pattern several mistakes share is the clue, e.g. "把 7 认成了 1" or "后面又出现了同一个字").
    /// </summary>
    public sealed class XgExamMistake
    {
        public XgBoardCard card;
        public string dataset = "";
        public bool answer, truth, guessed;
        /// <summary>The kind of mistake (grouping key) and its readable name.</summary>
        public string key = "", kind = "", kindEn = "";
    }

    public sealed partial class XgRun
    {
        /// <summary>Wrong exam cards of the last evaluation (the first <see cref="XgSim.MistakesKept"/>), for the 错题 view.</summary>
        [NonSerialized] public List<XgExamMistake> mistakes = new List<XgExamMistake>();
        /// <summary>Wrong cards per kind at the last evaluation, over all of them (not only the kept ones).</summary>
        [NonSerialized] public Dictionary<string, int> mistakeKinds = new Dictionary<string, int>();
        [NonSerialized] public int mistakeCount, examCount;
        [NonSerialized] public string mistakeDataset = "";
    }

    /// <summary>
    /// 错题: every evaluation already answers the whole test set; the wrong answers are kept so the training page can
    /// show the cards themselves (a digit it misread, a sentence it missed) and say what they have in common.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int MistakesKept = 24;
        /// <summary>A kind of mistake is worth naming once it is at least this share of all mistakes (and 3 cards).</summary>
        public const double MistakePatternShare = .3;

        /// <summary>Answers the test set with the run's knobs, keeping the wrong cards. Returns the board accuracy.</summary>
        double ExamWithMistakes(XgRun run, List<XgBoardCard> test, XgKnobs k)
        {
            if (run.mistakes == null) run.mistakes = new List<XgExamMistake>();
            if (run.mistakeKinds == null) run.mistakeKinds = new Dictionary<string, int>();
            run.mistakes.Clear(); run.mistakeKinds.Clear();
            run.mistakeCount = 0; run.examCount = test.Count; run.mistakeDataset = run.dataset;
            int right = 0;
            foreach (var card in test)
            {
                bool answer = Board.Predict(card, k, out bool guessed);
                if (answer == card.truth) { right++; continue; }
                var m = new XgExamMistake { card = card, dataset = run.dataset, answer = answer, truth = card.truth, guessed = guessed };
                Classify(m);
                run.mistakeCount++;
                run.mistakeKinds.TryGetValue(m.key, out int n);
                run.mistakeKinds[m.key] = n + 1;
                if (run.mistakes.Count < MistakesKept) run.mistakes.Add(m);
            }
            return test.Count == 0 ? 0 : (double)right / test.Count;
        }

        /// <summary>Up to <paramref name="count"/> wrong cards to show, one of each kind first (most common kind first).</summary>
        public List<XgExamMistake> ShownMistakes(XgRun run, int count)
        {
            var shown = new List<XgExamMistake>();
            if (run?.mistakes == null || run.mistakeDataset != run.dataset) return shown;
            var kinds = new List<string>(run.mistakeKinds.Keys);
            kinds.Sort((a, b) => run.mistakeKinds[b].CompareTo(run.mistakeKinds[a]));
            foreach (var kind in kinds)
            {
                var m = run.mistakes.Find(x => x.key == kind);
                if (m != null && shown.Count < count) shown.Add(m);
            }
            foreach (var m in run.mistakes) if (shown.Count < count && !shown.Contains(m)) shown.Add(m);
            return shown;
        }

        /// <summary>
        /// What most of the mistakes share, in a line (null when they share nothing worth naming): the clue the
        /// mistakes give, before any verdict says what to change.
        /// </summary>
        public string MistakePattern(XgRun run)
        {
            if (run?.mistakeKinds == null || run.mistakeDataset != run.dataset || run.mistakeCount < 3) return null;
            string top = null; int most = 0;
            foreach (var kv in run.mistakeKinds) if (kv.Value > most) { most = kv.Value; top = kv.Key; }
            if (top == null || most < 3 || most < run.mistakeCount * MistakePatternShare) return null;
            var m = run.mistakes.Find(x => x.key == top);
            if (m == null) return null;
            return T("错题里最多的是：" + m.kind + "（" + most + " / " + run.mistakeCount + "）",
                     "Most of the mistakes: " + m.kindEn + " (" + most + " / " + run.mistakeCount + ")");
        }

        /// <summary>The wrong card as a short readable line: what it was and what the model said.</summary>
        public string MistakeLine(XgExamMistake m)
        {
            var c = m.card; var s = c.source;
            string said = m.answer ? T("是", "yes") : T("否", "no"), right = m.truth ? T("是", "yes") : T("否", "no");
            switch (m.dataset)
            {
                case "mnist":
                    if (s == null) break;
                    return m.truth ? T("这张 " + s.digit + "，它说不是 " + s.digit, "A " + s.digit + "; it said not a " + s.digit)
                                   : T("把 " + s.digit + " 认成了 " + s.asked, "Read a " + s.digit + " as a " + s.asked);
                case "xor":
                    return T("「" + Names(c, "、") + "」该答" + right + "，它答" + said, "\"" + Names(c, ", ") + "\": should be " + right + ", it said " + said);
                case "parallel":
                    return T(ParallelText(c) + " 它答" + said + "，应该" + right, ParallelText(c) + " It said " + said + ", should be " + right);
                case "translate":
                    return T(TranslationText(c) + " 它答" + said + "，应该" + right, TranslationText(c) + " It said " + said + ", should be " + right);
                case "poems": case "news":
                    if (s == null) break;
                    string shown = s.line.Substring(0, Math.Min(s.shown, s.line.Length));
                    return T("「" + shown + "」下一个字是「" + s.askedChar + "」？它答" + said, "\"" + shown + "\" then \"" + s.askedChar + "\"? It said " + said);
            }
            string text = c.text.Length > 0 ? c.text : s != null ? T(s.question, s.questionEn.Length > 0 ? s.questionEn : s.question) : "";
            return text + T(" · 它答" + said + "，应该" + right, " · it said " + said + ", should be " + right);
        }

        /// <summary>What the model said and what was right: 「它答是，应该否」.</summary>
        public string MistakeAnswer(XgExamMistake m)
            => T("它答「" + (m.answer ? "是" : "否") + "」，应该「" + (m.truth ? "是" : "否") + "」", "It said " + (m.answer ? "yes" : "no") + ", should be " + (m.truth ? "yes" : "no"));

        /// <summary>What is on a card, in a line (for cards without text of their own, such as pictures).</summary>
        public string CardText(XgBoardCard c, string dataset)
        {
            var s = c.source;
            switch (dataset)
            {
                case "mnist": if (s != null) return T("手写的 " + s.digit + "，问：是 " + s.asked + " 吗？", "A handwritten " + s.digit + "; asked: is it a " + s.asked + "?"); break;
                case "xor": return "「" + Names(c, "、") + "」";
                case "parallel": return ParallelText(c);
                case "translate": return TranslationText(c);
                case "poems": case "news":
                    if (s != null) return T("「" + s.line.Substring(0, Math.Min(s.shown, s.line.Length)) + "」下一个字是「" + s.askedChar + "」？", "\"" + s.line.Substring(0, Math.Min(s.shown, s.line.Length)) + "\" then \"" + s.askedChar + "\"?");
                    break;
            }
            if (c.text.Length > 0) return c.text;
            return s != null ? T(s.question, s.questionEn.Length > 0 ? s.questionEn : s.question) : "";
        }

        /// <summary>Sorts a mistake into a kind whose name is the clue (see <see cref="MistakePattern"/>).</summary>
        static void Classify(XgExamMistake m)
        {
            var c = m.card; var s = c.source;
            void Kind(string key, string zh, string en) { m.key = key; m.kind = zh; m.kindEn = en; }
            // Nothing it knows matched: the card looks new to it (a digit moved to a new place, an unseen phrase).
            if (m.guessed)
            {
                if (c.region == "vision") Kind("guess", "没见过的样子（挪了位置、换了写法），只能瞎猜", "looks new to it (moved, or written differently), so it guessed");
                else Kind("guess", "没见过的说法，只能瞎猜", "words it has never seen together, so it guessed");
                return;
            }
            switch (m.dataset)
            {
                case "mnist":
                    if (s == null) break;
                    if (m.truth) Kind("miss" + s.digit, "认不出 " + s.digit, "fails to see a " + s.digit);
                    else Kind(s.digit + ">" + s.asked, "把 " + s.digit + " 认成 " + s.asked, "reads a " + s.digit + " as a " + s.asked);
                    return;
                case "xor":
                    Kind("xor:" + Names(c, "+"), "「" + Names(c, "、") + "」", "\"" + Names(c, ", ") + "\"");
                    return;
                case "parallel":
                    if (LaterRepeat(c)) Kind("decoy", "问的字后面又出现过（分不清哪个在开头）", "the asked character appears again later (which one opened?)");
                    else Kind("plain", "开头的字没认对", "misread the opening character");
                    return;
                case "translate":
                    if (TranslationFromEnd(c) >= XgBoard.BottleneckTokens) Kind("front", "错在句子前半（离句尾远的词）", "wrong on the front of the sentence (far from its end)");
                    else Kind("back", "错在句尾附近的词", "wrong near the end of the sentence");
                    return;
            }
            if (c.distance >= 0)
            {
                if (c.distance >= 10) Kind("far", "线索离问题 10 字以上", "the clue is 10+ characters back");
                else if (c.distance >= 5) Kind("mid", "线索离问题 5–9 字", "the clue is 5–9 characters back");
                else Kind("near", "线索就在附近（1–4 字）", "the clue is close (1–4 characters)");
                return;
            }
            if (s == null) { Kind("other", "其他", "other"); return; }
            if (s.trick) { Kind("trick", "老司机题（看着像一个答案，其实是另一个）", "trick cards (look like one answer, are the other)"); return; }
            if (s.category.Length > 0) { Kind("cat:" + s.category, "「" + s.category + "」题", "\"" + (s.categoryEn.Length > 0 ? s.categoryEn : s.category) + "\" cards"); return; }
            if (m.truth) Kind("missyes", "该答「是」的答成了「否」", "said no where the answer was yes");
            else Kind("falseyes", "该答「否」的答成了「是」", "said yes where the answer was no");
        }

        static string Names(XgBoardCard c, string sep)
        {
            var sb = new StringBuilder();
            foreach (var f in c.features) { if (sb.Length > 0) sb.Append(sep); sb.Append(f.name); }
            return sb.ToString();
        }

        static string ParallelText(XgBoardCard c)
        {
            var sb = new StringBuilder(); string asked = "";
            foreach (var f in c.features) { if (f.x >= 0) sb.Append(f.name); else if (f.name.StartsWith("首?", StringComparison.Ordinal)) asked = f.name.Substring(2); }
            return "「" + sb + "」" + "开头是「" + asked + "」吗？";
        }

        static bool LaterRepeat(XgBoardCard c)
        {
            string asked = "";
            foreach (var f in c.features) if (f.x < 0 && f.name.StartsWith("首?", StringComparison.Ordinal)) asked = f.name.Substring(2);
            foreach (var f in c.features) if (f.x > 0 && f.name == asked) return true;
            return false;
        }

        static string TranslationText(XgBoardCard c)
        {
            var sb = new StringBuilder(); string word = ""; int at = -1;
            foreach (var f in c.features)
            {
                if (f.x < 0) continue;
                if (f.seq == 0) { if (sb.Length > 0) sb.Append(' '); sb.Append(f.name); }
                else { word = f.name; at = f.x; }
            }
            return "「" + sb + "」第 " + (at + 1) + " 个词译作「" + word + "」？";
        }

        static int TranslationFromEnd(XgBoardCard c)
        {
            int length = 0, at = 0;
            foreach (var f in c.features) { if (f.x < 0) continue; if (f.seq == 0) length = Math.Max(length, f.x + 1); else at = f.x; }
            return length - 1 - at;
        }
    }
}
