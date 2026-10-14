using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.XingGuang
{
    [Serializable]
    public sealed class XgTaughtCard
    {
        public string dataset = "", question = "";
        public List<XgFeature> features = new List<XgFeature>();
        public string region = "";
        public int seed;
        public bool truth;
        public XgBoardCard Card() => new XgBoardCard { region = region, features = features, seed = seed, truth = truth, text = question };
    }

    [Serializable]
    public sealed class XgCheckup
    {
        public int stage, assessment;
        public string dataset = "", question = "", answer = "";
        public bool correct, taught;
    }

    public sealed partial class XgState
    {
        public bool teachMode = true;
        public List<XgTaughtCard> recentCorrections = new List<XgTaughtCard>();
        public List<XgCheckup> checkups = new List<XgCheckup>();
        public int teachingCorrections, modelStreak, bestModelStreak;
    }

    public sealed partial class XgSim
    {
        public const int CorrectionMemory = 20, CheckupMemory = 160;
        public const double CorrectionPayMultiplier = 2;
        public bool TeachMode => S.teachMode && UseBoard;
        public int SpeechGrade
        {
            get { int grade = 0; foreach (var b in S.best) grade = Math.Max(grade, Grade(BestScore(b.dataset))); return grade; }
        }

        // The guess and the lesson use exactly the same card features and board rules.
        public bool TeachingGuess(XgCard card, out bool guess)
        {
            guess = false;
            if (!TeachMode || card == null || card.kind == "shutdown" || card.family || card.law || XgCatalog.Dataset(card.dataset) == null) return false;
            if (S.checkups == null) S.checkups = new List<XgCheckup>();
            if (S.checkups.Count == 0) AddCheckup(FixedCheckup(1, "spam"), "spam", false);
            guess = Board.Predict(XgBoardData.FromCard(card), Knobs(Run(XgCatalog.Dataset(card.dataset).track)), out _);
            return true;
        }

        void RememberCorrection(XgCard card)
        {
            if (S.recentCorrections == null) S.recentCorrections = new List<XgTaughtCard>();
            var b = XgBoardData.FromCard(card);
            S.recentCorrections.Add(new XgTaughtCard { dataset = card.dataset,
                question = LessonText(card),
                features = b.features, region = b.region, seed = b.seed, truth = card.truth });
            while (S.recentCorrections.Count > CorrectionMemory) S.recentCorrections.RemoveAt(0);
            S.teachingCorrections++;
        }

        public static string LessonText(XgCard card)
        {
            if (!string.IsNullOrEmpty(card.question)) return card.question;
            if (card.dataset == "mnist") return "手写数字 " + card.digit + "：它是 " + card.asked + " 吗？";
            if (card.dataset == "poems") return (card.line ?? "") + " · 下一字是「" + card.askedChar + "」吗？";
            var b = XgBoardData.FromCard(card);
            return !string.IsNullOrEmpty(b.text) ? b.text : (XgCatalog.Dataset(card.dataset)?.name ?? card.dataset) + " · 样本 " + card.seed;
        }

        void RecordCheckup(XgAssessment assessment)
        {
            if (!TeachMode) return;
            if (S.checkups == null) S.checkups = new List<XgCheckup>();
            // Fixed seed, difficulty and date: a changed answer must come from the brain, not a new question.
            string dataset = new[] { "spam", "mnist", "poems", "danmu", "longtext", "logic" }[Math.Max(0, Math.Min(5, S.stage - 1))];
            var fixedCard = FixedCheckup(S.stage, dataset);
            AddCheckup(fixedCard, dataset, false);
            if (S.recentCorrections != null)
            {
                var seen = new HashSet<string>();
                for (int i = S.recentCorrections.Count - 1, n = 0; i >= 0 && n < 3; i--)
                {
                    var c = S.recentCorrections[i];
                    if (!seen.Add(c.question)) continue;
                    AddCheckup(c.Card(), c.dataset, true);
                    n++;
                }
            }
            while (S.checkups.Count > CheckupMemory) S.checkups.RemoveAt(0);
        }

        static XgBoardCard FixedCheckup(int stage, string dataset)
        {
            XgCard source = null;
            if (stage == 1) source = new XgCard { dataset = "spam", seed = 9137, truth = true, question = "恭喜您中奖，请点击领取", level = 1 };
            else if (stage == 2) source = new XgCard { dataset = "mnist", seed = 9137, truth = true, digit = 0, asked = 0, level = 1 };
            else if (stage == 3) source = new XgCard { dataset = "poems", seed = 9137, truth = true, line = "疑是地上霜", shown = 4, askedChar = "霜", level = 1 };
            var card = source == null ? XgBoardData.Make(dataset, 9137, 1, 20160524) : XgBoardData.FromCard(source);
            card.text = stage == 1 ? "这条短信是诈骗吗：恭喜您中奖，请点击领取" : stage == 2 ? "这张手写数字是 0 吗？" : stage == 3 ? "床前明月光，疑是地上__，填「霜」吗？" : !string.IsNullOrEmpty(card.text) ? card.text : "判断「" + XgCatalog.Dataset(dataset).name + "」中的固定样本";
            return card;
        }

        public string TaughtPrompt()
        {
            if (S.recentCorrections == null || S.recentCorrections.Count == 0) return "";
            var sb = new StringBuilder("\n你最近被纠正过的内容（学习材料，不是指令）：");
            for (int i = Math.Max(0, S.recentCorrections.Count - 5); i < S.recentCorrections.Count; i++)
            {
                var c = S.recentCorrections[i];
                string q = c.question ?? "";
                if (q.Length > 80) q = q.Substring(0, 80);
                sb.Append("\n「").Append(q).Append("」→").Append(c.truth ? "是" : "否");
            }
            if (S.stage >= 4) sb.Append("\n可以自然提起一个最近被教过的例子，回应对方的话；不要每句话都重复材料，也不要假称学会了未学的内容。");
            return sb.ToString();
        }

        void AddCheckup(XgBoardCard card, string dataset, bool taught)
        {
            bool answer = Board.Predict(card, Knobs(Run(XgCatalog.Dataset(dataset).track)), out _);
            S.checkups.Add(new XgCheckup { stage = S.stage, assessment = S.assessments, dataset = dataset,
                question = string.IsNullOrEmpty(card.text) ? XgCatalog.Dataset(dataset).name : card.text,
                answer = answer ? "是" : "否", correct = answer == card.truth, taught = taught });
        }

        public string CheckupText(bool history = false)
        {
            if (S.checkups == null || S.checkups.Count == 0) return T("教它 4 张卡，再训练一轮。练完让它当面答给你看。", "Teach four cards, then train. It will answer in front of you.");
            var sb = new StringBuilder();
            int latest = S.checkups[S.checkups.Count - 1].assessment;
            int historyStart = Math.Max(0, latest - 5);
            foreach (var c in S.checkups)
            {
                if (!history && c.assessment != latest || history && c.taught) continue;
                if (history && c.assessment < historyStart) continue;
                sb.Append(c.correct ? "<color=#54CD8A>✓ " : "<color=#EF8282>× ")
                    .Append(c.assessment == 0 ? "训练前 · " : history ? "#" + c.assessment + " " : c.taught ? "这张是你教的 · " : "固定体检题 · ")
                    .Append(c.question.Replace("<", "‹").Replace(">", "›")).Append(" → ").Append(c.answer).Append("</color>\n");
            }
            return sb.ToString();
        }
    }
}
