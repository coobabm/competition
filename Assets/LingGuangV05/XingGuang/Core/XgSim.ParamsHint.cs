using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.XingGuang
{
    /// <summary>How the model a line trains stands towards the trained parameters P.</summary>
    public enum XgCountState
    {
        /// <summary>P already covers this model's size.</summary>
        Counted,
        /// <summary>It changed shape and has not trained a round since.</summary>
        Fresh,
        /// <summary>Trained, but its grade on the current desk is below C.</summary>
        BelowC,
        /// <summary>At grade C or better but not counted yet; the next round counts it.</summary>
        Unassessed,
    }

    /// <summary>
    /// The numbers behind every parameter hint (指标): what a model's size is made of, whether it counts towards P,
    /// what the caps and the card allow, and what the next tech node would add. Read-only; the guide, the plateau
    /// text, the training page and the tooltips all print these, so they agree.
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>The accuracy at which a dataset's grade line sits (score = <see cref="XgCatalog.GradeScore"/>).</summary>
        public static double GradeAccuracy(string dataset, int grade)
        {
            var d = XgCatalog.Dataset(dataset);
            if (d == null) return 0;
            int g = Math.Max(0, Math.Min(XgCatalog.GradeScore.Length - 1, grade));
            return 1 - d.chanceError + XgCatalog.GradeScore[g] / 1000.0 * (d.chanceError - d.floorError);
        }

        public static string GradeLetter(int grade) => XgCatalog.GradeNames[Math.Max(0, Math.Min(XgCatalog.GradeNames.Length - 1, grade))];

        /// <summary>The grade the model's current accuracy earns on its desk.</summary>
        public int ModelGrade(XgRun run) => run == null ? 0 : Grade(Score(run.dataset, run.valAcc));

        /// <summary>Whether this model's size already counts, and if not what is missing.</summary>
        public XgCountState CountState(XgRun run)
        {
            if (ParamsK(run) <= ModelParamsK + 1e-9) return XgCountState.Counted;
            if (run.shapeRounds <= 0) return XgCountState.Fresh;
            return ModelGrade(run) < TrainedGrade ? XgCountState.BelowC : XgCountState.Unassessed;
        }

        /// <summary>「已算数」「未到 C 级，不算」: the short verdict.</summary>
        public string CountShort(XgRun run)
        {
            switch (CountState(run))
            {
                case XgCountState.Counted: return T("已算数", "counted");
                case XgCountState.Fresh: return T("刚长大，没练过", "just grew, untrained");
                case XgCountState.BelowC: return T("未到 C 级，不算", "below C, not counted");
                default: return T("到 C 级了，练一轮就算", "at grade C, counts next round");
            }
        }

        /// <summary>The verdict with its numbers: the grade and accuracy now against the grade-C line.</summary>
        public string CountText(XgRun run)
        {
            double line = GradeAccuracy(run.dataset, TrainedGrade);
            string now = GradeLetter(ModelGrade(run));
            switch (CountState(run))
            {
                case XgCountState.Counted: return T("现 " + now + " 级 · 已算数", "grade " + now + " · counted");
                case XgCountState.Fresh: return T("刚长大，练到 C 级（" + Pct(line) + "）就算数", "just grew; train it to grade C (" + Pct(line) + ") and it counts");
                case XgCountState.BelowC: return T("现 " + now + " 级（" + Pct(run.valAcc) + "），到 C 级（" + Pct(line) + "）才算", "grade " + now + " (" + Pct(run.valAcc) + "); grade C (" + Pct(line) + ") to count");
                default: return T("现 " + now + " 级（" + Pct(run.valAcc) + "），再练一轮就算数", "grade " + now + " (" + Pct(run.valAcc) + "); one more round and it counts");
            }
        }

        /// <summary>「这台 3.1M（未到 C 级，不算）· 已算数 1.2M」: this model against what P holds.</summary>
        public string ParamsSplitText(XgRun run)
            => T("这台 " + ParamsText(ParamsK(run)) + "（" + CountShort(run) + "）· 已算数 " + ParamsText(TrainedParamsK),
                "This model " + ParamsText(ParamsK(run)) + " (" + CountShort(run) + ") · counted " + ParamsText(TrainedParamsK));

        /// <summary>「3 层 × 宽 128² × 结构 3 = 1.2M」: what the size is made of.</summary>
        public string ModelFormula(XgRun run)
        {
            var a = XgCatalog.Arch(run.arch);
            string w = XgCatalog.Widths[Math.Max(0, Math.Min(XgCatalog.Widths.Length - 1, run.width))].ToString(System.Globalization.CultureInfo.InvariantCulture);
            string c = F(a == null ? 1 : a.paramFactor, "0.##");
            return T(run.depth + " 层 × 宽 " + w + "² × 结构 " + c + " = " + ParamsText(ParamsK(run)),
                run.depth + " layers × width " + w + "² × structure " + c + " = " + ParamsText(ParamsK(run)));
        }

        /// <summary>「科技上限 宽 64/128 · 3/5 层」: the model against the caps the tech tree and its structure allow.</summary>
        public string CapsText(XgRun run)
        {
            var track = (XgTrack)run.track;
            int widthCap = Math.Max(0, Math.Min(XgCatalog.Widths.Length - 1, WidthCap(track))), depthCap = Math.Max(1, Math.Min(DepthCap(track), MaxDepth(run)));
            string w = XgCatalog.Widths[Math.Max(0, Math.Min(XgCatalog.Widths.Length - 1, run.width))] + "/" + XgCatalog.Widths[widthCap];
            return T("科技上限 宽 " + w + " · " + run.depth + "/" + depthCap + " 层", "tech caps: width " + w + " · " + run.depth + "/" + depthCap + " layers");
        }

        /// <summary>Memory the way the cards show it: 640 MB, 4 GB.</summary>
        public static string MemText(double mb) => mb >= 1024 ? F(mb / 1024, mb < 10240 ? "0.#" : "0") + " GB" : F(mb, "0") + " MB";

        /// <summary>「显存 0.9 GB / 4 GB」: what this model takes against what the card has (0 = unknown card).</summary>
        public string VramText(XgRun run, double cardMB)
        {
            double need = VramNeedMB(run);
            if (!(cardMB > 0)) return T("显存 ", "VRAM ") + MemText(need);
            return T("显存 ", "VRAM ") + MemText(need) + " / " + MemText(cardMB) + (need > cardMB + 1e-6 ? T("（超了）", " (over)") : "");
        }

        /// <summary>Samples after the weights, before the noise penalty: what the bar would show with clean data.</summary>
        public double RawSamples
        {
            get
            {
                double raw = 0;
                foreach (var ds in XgCatalog.Datasets) if (CountsAsData(ds.id)) raw += ds.dataWeight * Samples(ds.id);
                return raw;
            }
        }

        /// <summary>「已有 62,000 · 标错扣 1,200 · 算数 60,800」: the samples bar split into what is there and what counts.</summary>
        public string SamplesSplitText()
        {
            double raw = RawSamples, eff = TrainedSamples, lost = Math.Max(0, raw - eff);
            return lost >= 1
                ? T("已有 " + SamplesText(raw) + " · 噪声扣 " + SamplesText(lost) + " · 算数 " + SamplesText(eff), "have " + SamplesText(raw) + " · noise −" + SamplesText(lost) + " · counted " + SamplesText(eff))
                : T("已有 " + SamplesText(raw) + " · 没有被扣 · 全部算数", "have " + SamplesText(raw) + " · nothing taken off · all counted");
        }

        /// <summary>The model the lab would configure for <paramref name="run"/>'s structure once <paramref name="node"/> (a width or depth square) is owned.</summary>
        public double ParamsAfterNode(XgRun run, XgNode node, IXgHost host)
        {
            var track = (XgTrack)run.track;
            int depthCap = DepthCap(track), widthCap = WidthCap(track);
            if (node.kind == XgNodeKind.Depth) depthCap = Math.Max(depthCap, node.value);
            else if (node.kind == XgNodeKind.Width) widthCap = Math.Max(widthCap, node.value);
            return ParamsK(AutoShapeWithCaps(run, run.arch, host, depthCap, widthCap));
        }

        /// <summary>
        /// The numbers after 「参数量不够，再练也涨不动」: this model against the line, the accuracy its size tops out at
        /// against the grade-C line, and the size the next tech node gives. In the language asked for (a guide step keeps both).
        /// </summary>
        public string ParamsPlateauDetail(XgRun run, bool english)
        {
            string L(string zh, string en) => english ? en : zh;
            double k = ParamsK(run);
            int next = NextAbility;
            var sb = new StringBuilder();
            sb.Append(L("这台 ", "This model ")).Append(ParamsText(k));
            if (next > 0)
            {
                double need = ParamsThreshold(next);
                sb.Append(L("，门槛 ", ", line ")).Append(ParamsText(need)).Append(L("（", " (")).Append(F(100 * Math.Min(1, k / Math.Max(1e-9, need)), "0")).Append(L("%）", "%)"));
            }
            sb.Append(L("；这个大小准确率最高 ", "; at this size accuracy tops out at ")).Append(Pct(CeilingAcc(run)))
              .Append(L("，C 级线 ", ", grade C is ")).Append(Pct(GradeAccuracy(run.dataset, TrainedGrade)));
            var node = XgChanceHint.NextScaleNode(this, (XgTrack)run.track);
            if (node != null)
            {
                double after = ParamsAfterNode(run, node, wiringHost);
                sb.Append(L("；科技买「" + XgGuide.NodeLabel(node, false) + "」¥" + F(NodeCost(node), "0") + " → " + ParamsText(after),
                    "; buy '" + XgGuide.NodeLabel(node, true) + "' for ¥" + F(NodeCost(node), "0") + " → " + ParamsText(after)));
                if (after <= k + 1e-9) sb.Append(L("（显卡放不下，要加显卡）", " (the card cannot hold more; add a card)"));
            }
            else sb.Append(L("；宽度和层数已到科技上限，看道具里的结构", "; width and depth are at the tech cap: look at structures on the Items page"));
            return sb.Append(L("。", ".")).ToString();
        }

        /// <summary>One line per trained track: structure and formula, then the verdict, caps and memory.</summary>
        string ModelLines(XgRun run, double cardMB)
        {
            var a = XgCatalog.Arch(run.arch);
            var sb = new StringBuilder();
            sb.Append(T(run.track == (int)XgTrack.Vision ? "视觉" : "序列", run.track == (int)XgTrack.Vision ? "Vision" : "Sequence")).Append(" · ")
              .Append(a == null ? run.arch : T(a.name, a.nameEn)).Append(": ").Append(ModelFormula(run));
            sb.Append("\n   ").Append(CountText(run)).Append(" · ").Append(CapsText(run)).Append(" · ").Append(VramText(run, cardMB));
            return sb.ToString();
        }

        /// <summary>
        /// The parameter bar in full, for hover help: P against the line (with %), the formula, then per line the model
        /// with its grade, whether it counts, the caps and the memory it takes of the card.
        /// </summary>
        public string ParamsReport(IXgHost host)
        {
            double p = TrainedParamsK, cardMB = host == null ? 0 : Vram(host);
            int next = NextAbility;
            var sb = new StringBuilder();
            sb.Append(T("已训练参数量 P：", "Trained parameters P: ")).Append(ParamsText(p));
            if (next > 0)
            {
                double need = ParamsThreshold(next), baseNeed = AbilityParamsK[next];
                sb.Append(" / ").Append(ParamsText(need)).Append(" (").Append(F(100 * ParamsProgress(next), "0")).Append("%)");
                sb.Append(need - p > 1e-9 ? T("，还差 ", "; short by ") + ParamsText(need - p) : T("，够了", "; enough"));
                if (need < baseNeed - 1e-9) sb.Append(T("\n门槛已按道具打折，原 ", "\nThe line is discounted by your items; it was ")).Append(ParamsText(baseNeed));
            }
            sb.Append(T("\nP = 练到 C 级以上的模型里最大的那个；没练到 C 级的不算。\n参数 = 层数 × 宽度² × 结构系数", "\nP = the biggest model trained to grade C or better; one below C does not count.\nParameters = layers × width² × structure factor"));
            foreach (var run in Runs)
                if (TrainingUnlocked((XgTrack)run.track)) sb.Append("\n").Append(ModelLines(run, cardMB));
            return sb.ToString();
        }

        /// <summary>The sample bar in full, for hover help: D against the line (with %), and what the noise took off.</summary>
        public string SamplesReport()
        {
            double d = TrainedSamples;
            int next = NextAbility;
            var sb = new StringBuilder();
            sb.Append(T("样本 D：", "Samples D: ")).Append(SamplesText(d));
            if (next > 0)
            {
                double need = SamplesThreshold(next), baseNeed = AbilitySamples[next];
                sb.Append(" / ").Append(SamplesText(need)).Append(" (").Append(F(100 * SamplesProgress(next), "0")).Append("%)");
                sb.Append(need - d > 1e-9 ? T("，还差 ", "; short by ") + SamplesText(need - d) : T("，够了", "; enough"));
                if (need < baseNeed - 1e-9) sb.Append(T("\n门槛已按道具打折，原 ", "\nThe line is discounted by your items; it was ")).Append(SamplesText(baseNeed));
            }
            sb.Append("\n").Append(SamplesSplitText());
            return sb.ToString();
        }
    }
}
