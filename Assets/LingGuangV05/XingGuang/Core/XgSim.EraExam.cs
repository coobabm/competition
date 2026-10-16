using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LingGuangV05.XingGuang
{
    public sealed class XgEraExamResult
    {
        public bool valid;
        public string reason = "", dataset = "", transcript = "";
        public int right, count;
        public double accuracy;
        public string[] signatures = Array.Empty<string>();
    }

    public sealed partial class XgSim
    {
        /// <summary>Read-only fresh questions against the current concept weights and settings, not a saved checkpoint.
        /// Exhausted spaces fail closed. Never trains, pays, saves, or publishes training-page mistakes.</summary>
        public XgEraExamResult RunEraFreshExam(string dataset, int seed, IXgHost host,
            IEnumerable<string> previousSignatures = null)
        {
            var result = new XgEraExamResult { dataset = dataset ?? "" };
            var definition = XgCatalog.Dataset(dataset ?? "");
            if (definition == null) { result.reason = T("这个数据集不存在。", "No such dataset."); return result; }
            var run = Run(definition.track);
            if (run.dataset != dataset) { result.reason = T("先到训练页选中这个数据集，再回来验收：验收看的是它现在的状态。", "Select this dataset on the training page first, then come back: the check uses what it knows right now."); return result; }
            if (host == null) { result.reason = T("电脑还没准备好。", "The computer is not ready yet."); return result; }
            if (host.Blocker != null) { result.reason = host.Blocker; return result; }
            if (!(host.Compute > 0) || double.IsInfinity(host.Compute)) { result.reason = T("现在没有可用的算力。", "There is no compute available right now."); return result; }
            string blocked = Blocker(run, host);
            if (blocked != null) { result.reason = blocked; return result; }
            if (!(TrainVram(run, host) >= VramNeedMB(run))) { result.reason = T("显存不够，先腾一点出来。", "Not enough video memory; free some up first."); return result; }

            int level = BoardLevel(dataset), today = Today;
            var forbidden = new HashSet<string>(StringComparer.Ordinal);
            if (previousSignatures != null)
                foreach (string signature in previousSignatures) if (signature != null) forbidden.Add(signature);
            // Reserve the entire possible pool, not just the samples currently purchased. Include augmentations.
            for (int i = 0; i < XgBoardData.PoolLimit; i++)
            {
                var card = XgBoardData.Make(dataset, XgBoardData.Seed(dataset, i, XgBoardData.Use.Train, S.dataSalt), level, today);
                forbidden.Add(XgBoardData.Signature(card));
                for (int v = 1; v < AugmentFactor(dataset); v++)
                    forbidden.Add(XgBoardData.Signature(XgBoardData.Augment(card, dataset, v)));
            }
            // Reserve every index the existing diagnostic/formal generators can expose, without calling their
            // fallback-to-training TestSet. Also exclude already cached questions from earlier levels/dates.
            for (int i = 0; i < XgBoardData.TestSize * 4; i++)
                forbidden.Add(XgBoardData.Signature(XgBoardData.Make(dataset,
                    XgBoardData.Seed(dataset, i, XgBoardData.Use.Diagnostic, S.dataSalt), level, today)));
            for (int i = 0; i < ReservedExamCards; i++)
                forbidden.Add(XgBoardData.Signature(XgBoardData.Make(dataset,
                    XgBoardData.Seed(dataset, i, XgBoardData.Use.Exam, S.dataSalt), 1, today)));
            foreach (var pair in testSets)
                if (pair.Key.StartsWith(dataset + "|", StringComparison.Ordinal))
                    foreach (var card in pair.Value) forbidden.Add(XgBoardData.Signature(card));

            var cards = new List<XgBoardCard>(20);
            var signatures = new List<string>(20);
            int positives = 0, negatives = 0;
            int start = (int)(1000000L + ((uint)seed % 400000L) * 4000L);
            for (int i = 0; i < 4000 && cards.Count < 20; i++)
            {
                var card = XgBoardData.Make(dataset,
                    XgBoardData.Seed(dataset, start + i, XgBoardData.Use.Exam, S.dataSalt), level, today);
                string signature = XgBoardData.Signature(card);
                if (card.truth ? positives >= 10 : negatives >= 10) continue;
                if (!forbidden.Add(signature)) continue;
                cards.Add(card); signatures.Add(signature);
                if (card.truth) positives++; else negatives++;
            }
            if (cards.Count != 20) { result.reason = T("新题不足：凑不齐 20 道（对错各 10 道）。不拿旧题凑数。", "Not enough fresh questions for 20 (10 right, 10 wrong). Old ones are not reused to fill the gap."); return result; }

            var snapshot = new XgBoard(Board.S.Clone());
            var knobs = Knobs(run).Copy();
            var report = new StringBuilder();
            report.Append(T("新题验收：20 道它没见过的题，对错各 10 道。\n这些题不在训练题里，也不在平时的测验里；用的是它现在学到的东西，不是之前存下的检查点。\n\n逐题：\n",
                "Fresh-question check: 20 questions it has never seen, 10 right and 10 wrong.\nThey are not in the training set or in the usual quizzes, and the check uses what it knows right now, not a saved checkpoint.\n\nOne by one:\n"));
            for (int i = 0; i < cards.Count; i++)
            {
                bool prediction = snapshot.Predict(cards[i], knobs, out bool guessed);
                bool good = prediction == cards[i].truth;
                if (good) result.right++;
                report.Append(i + 1).Append(good ? T(" 答对", " right") : T(" 答错", " wrong")).Append(i % 5 == 4 ? "\n" : "   ");
            }
            result.count = cards.Count;
            result.accuracy = (double)result.right / result.count;
            result.signatures = signatures.ToArray();
            report.Append(T("\n结果：答对 ", "\nResult: ")).Append(result.right).Append(" / ").Append(result.count)
                .Append(T("（", " right (")).Append(F(result.accuracy * 100, "0")).Append("%").Append(T("）。", ")."));
            result.transcript = report.ToString();
            result.valid = true;
            return result;
        }
    }
}
