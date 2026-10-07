using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// The guide note's step for a model that has stopped improving. There are no knobs to fix any more (训练不再需要调参数):
    /// a model that plateaus is short of parameters, samples, the right structure or clean data, and the step says
    /// which, plainly, and points at where to get it (科技 for width and depth, 道具 for structures, the labelling desk
    /// for samples). Read-only, like <see cref="XgGuide"/>.
    /// </summary>
    public static class XgChanceHint
    {
        /// <summary>Rounds a model must have trained before its plateau is worth a step.</summary>
        public const int PlateauEpochs = 6;

        /// <summary>How far an accuracy sits from chance (0) towards the dataset's floor error (1).</summary>
        public static double Progress(string dataset, double accuracy)
        {
            var d = XgCatalog.Dataset(dataset);
            if (d == null) return accuracy;
            double span = d.chanceError - d.floorError;
            return span <= 1e-9 ? 1 : (accuracy - (1 - d.chanceError)) / span;
        }

        /// <summary>Adds the step for the first trainable run that has stopped improving (the selected track first).</summary>
        public static void Add(XgSim sim, List<XgGuideStep> list)
        {
            var step = Step(sim);
            if (step != null) list.Add(step);
        }

        public static XgGuideStep Step(XgSim sim)
        {
            if (sim == null || sim.S.stage >= 6) return null;
            foreach (var track in new[] { sim.SelectedTrack, sim.SelectedTrack == XgTrack.Vision ? XgTrack.Sequence : XgTrack.Vision })
            {
                var run = sim.Run(track);
                if (!sim.TrainingUnlocked(track) || run.epoch < PlateauEpochs) continue;
                string text = sim.PlateauText(run, out var limit);
                if (limit == XgLimit.None || limit == XgLimit.Done) continue;
                return StepFor(sim, run, limit);
            }
            return null;
        }

        static XgGuideStep StepFor(XgSim sim, XgRun run, XgLimit limit)
        {
            var track = (XgTrack)run.track;
            var d = XgCatalog.Dataset(run.dataset);
            string data = d != null ? d.name : run.dataset, dataEn = d != null ? d.nameEn : run.dataset;
            string seen = "「" + data + "」" + XgSim.Pct(run.valAcc) + "，已经练到这个模型的顶。";
            string seenEn = dataEn + " is at " + XgSim.Pct(run.valAcc) + ", as far as this model goes.";
            string id = "plateau." + limit.ToString().ToLowerInvariant();
            switch (limit)
            {
                case XgLimit.Params:
                {
                    var node = NextScaleNode(sim, track);
                    string size = XgSim.ParamsText(XgSim.ParamsK(run));
                    return node != null
                        ? Make(id, "参数量不够，再练也涨不动：科技买「" + node.name + "」", "Not enough parameters, training will not help: buy " + node.nameEn + " in the tech tree",
                            seen + "模型 " + size + " 参数。加宽、加深以后，它会自动用上更大的模型。", seenEn + " The model has " + size + " parameters. Wider or deeper, and it uses the bigger model by itself.",
                            "tree", "node:" + node.id, "")
                        : Make(id, "参数量不够，再练也涨不动：去道具买参数更多的结构", "Not enough parameters, training will not help: buy a bigger structure on the Items page",
                            seen + "这条线的宽度和层数都到顶了，或者显存装不下更大的。", seenEn + " This line's width and depth are at their cap, or the card holds nothing bigger.",
                            "items", "", "");
                }
                case XgLimit.Data:
                    return Make(id, "样本不够，再练也涨不动：多标「" + data + "」，或买数据包", "Not enough samples, training will not help: label more " + dataEn + " or buy its data pack",
                        seen + "模型够大，题见得太少。", seenEn + " The model is big enough; it has seen too few questions.", "label", "", run.dataset);
                case XgLimit.Structure:
                    return Make(id, "结构不对路，再练也涨不动：去道具买更合适的结构", "The structure does not suit this data, training will not help: buy a better one on the Items page",
                        seen + (track == XgTrack.Sequence ? "句子太长，或者要边读边写，现在的结构做不到。" : "现在的结构看不懂这类图。"),
                        seenEn + (track == XgTrack.Sequence ? " The sentences are too long, or it must read and write at once; this structure cannot." : " This structure cannot read pictures like these."),
                        "items", "", "");
                default:
                    return Make(id, "数据太脏，再练也涨不动：清洗噪声，或关掉杂包", "The data is too dirty, training will not help: clean the noise or switch off junk packs",
                        seen + "标错的样本太多，它在学自己的错。", seenEn + " Too many wrong labels: it is learning its own mistakes.", "train", "name:CleanDatasetNoise", track == XgTrack.Vision ? "vision" : "sequence");
            }
        }

        /// <summary>The cheapest visible width or layer node not owned yet that raises this track's caps.</summary>
        static XgNode NextScaleNode(XgSim sim, XgTrack track)
        {
            string tree = XgSim.TreeOf(track);
            int widthCap = sim.WidthCap(track), depthCap = sim.DepthCap(track);
            XgNode best = null;
            foreach (var n in XgCatalog.Nodes)
            {
                if (n.kind != XgNodeKind.Width && n.kind != XgNodeKind.Depth || n.tree != tree && n.tree != "trunk" || sim.Has(n.id) || !sim.NodeVisible(n)) continue;
                if (n.kind == XgNodeKind.Width ? n.value <= widthCap : n.value <= depthCap) continue;
                if (best == null || sim.NodeCost(n) < sim.NodeCost(best)) best = n;
            }
            return best;
        }

        static XgGuideStep Make(string id, string zh, string en, string whyZh, string whyEn, string tab, string target, string arg)
            => new XgGuideStep { id = id, kind = XgGuideKind.Main, zh = zh, en = en, whyZh = whyZh, whyEn = whyEn, app = XgGuide.Lab, tab = tab, target = target, arg = arg };
    }
}
