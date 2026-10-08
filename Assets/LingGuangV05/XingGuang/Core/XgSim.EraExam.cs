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
            if (definition == null) { result.reason = "未知数据集"; return result; }
            var run = Run(definition.track);
            if (run.dataset != dataset) { result.reason = "请先在训练页选择此数据集；验收使用当前训练快照"; return result; }
            if (host == null) { result.reason = "设备未就绪"; return result; }
            if (host.Blocker != null) { result.reason = host.Blocker; return result; }
            if (!(host.Compute > 0) || double.IsInfinity(host.Compute)) { result.reason = "没有可用算力"; return result; }
            string blocked = Blocker(run, host);
            if (blocked != null) { result.reason = blocked; return result; }
            if (!(TrainVram(run, host) >= VramNeedMB(run))) { result.reason = "显存不足"; return result; }

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
            if (cards.Count != 20) { result.reason = "独立新题不足正负各 10 道；未使用旧题补足"; return result; }

            var snapshot = new XgBoard(Board.S.Clone());
            var knobs = Knobs(run).Copy();
            var report = new StringBuilder();
            report.Append("当前训练快照（非部署检查点）\ndataset=").Append(dataset).Append(" level=").Append(level)
                .Append(" seed=").Append(seed).Append(" dataSalt=").Append(S.dataSalt).Append('\n')
                .Append("arch=").Append(run.arch).Append(" depth=").Append(knobs.depth).Append(" width=").Append(knobs.width)
                .Append(" wiring=").Append(knobs.wiring).Append(" activation=").Append(knobs.activation)
                .Append(" lr=").Append(knobs.lr.ToString("R", CultureInfo.InvariantCulture))
                .Append(" skip=").Append(knobs.skip).Append(" clip=").Append(knobs.clip).Append(" position=").Append(knobs.position)
                .Append(" features=").Append(knobs.features).Append(" batchNorm=").Append(knobs.batchNorm)
                .Append(" dropout=").Append(knobs.dropout).Append(" bias=").Append(knobs.bias)
                .Append(" warmup=").Append(knobs.warmup).Append(" identityInit=").Append(knobs.identityInit)
                .Append(" multiHead=").Append(knobs.multiHead).Append(" steadiness=").Append(knobs.steadiness.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
            for (int i = 0; i < cards.Count; i++)
            {
                bool prediction = snapshot.Predict(cards[i], knobs, out bool guessed);
                if (prediction == cards[i].truth) result.right++;
                report.Append(i + 1).Append(". prediction=").Append(prediction).Append(" truth=").Append(cards[i].truth)
                    .Append(" guessed=").Append(guessed).Append(" signature=").Append(signatures[i]).Append('\n');
            }
            result.count = cards.Count;
            result.accuracy = (double)result.right / result.count;
            result.signatures = signatures.ToArray();
            report.Append("raw accuracy=").Append(result.right).Append('/').Append(result.count).Append(" = ")
                .Append(result.accuracy.ToString("P1", CultureInfo.InvariantCulture));
            result.transcript = report.ToString();
            result.valid = true;
            return result;
        }
    }
}
