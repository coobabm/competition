using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// The one-line answer of the 训练图式: what holds the run back most right now, where (a layer, or the whole net),
    /// since which epoch, and the two roads out of it (a new structure, or a training method).
    /// </summary>
    public sealed class XgVerdict
    {
        /// <summary>untrained, nan, step, relay, degrade, signal, cells, memorize, memory, nomerge, plateau, passed, improving.</summary>
        public string id = "untrained";
        public bool problem;
        /// <summary>The layer to point at (1-based), 0 = the whole network.</summary>
        public int layer;
        public string headline = "", headlineEn = "";
        /// <summary>Remedies: change the structure, or change how it is trained ("" when there is none).</summary>
        public string structure = "", structureEn = "", method = "", methodEn = "";
        /// <summary>Epoch the trace first noticed this problem in the current settings (-1 = not noticed yet).</summary>
        public int since = -1;
        public double test, goal;
    }

    public sealed partial class XgSim
    {
        /// <summary>
        /// Picks the single most important problem of the run, in order of what blocks learning hardest: a torn rate,
        /// a step activation, votes drowned on the way up (传话), going deeper made it worse, errors fading on the way
        /// down, a loop that forgets (on texts read in order), cells too few, memorising, no combinations, a plateau. Healthy runs say how far they are from the goal.
        /// </summary>
        public XgVerdict Verdict(XgRun run)
        {
            var v = new XgVerdict();
            var t = TraceOf(run.track, run.dataset, false);
            var h = NetworkHealth(run);
            var k = Knobs(run);
            v.goal = TraceGoal(run);
            if (t == null || t.points.Count == 0)
            {
                v.headline = "还没在这个数据集上训练过：先练几轮再来看。"; v.headlineEn = "Not trained on this dataset yet: train a few epochs and look again.";
                return v;
            }
            var p = t.points[t.points.Count - 1];
            v.test = p.test;
            string Pct(double x) => Math.Round(x * 100) + "%";
            XgLayerHealth First(string problem) { foreach (var l in h.layers) if (l.problem == problem) return l; return null; }
            bool full = p.cap > 0 && p.cells >= p.cap;
            bool sequential = SequentialDatasets.Contains(run.dataset) || run.dataset == "longtext" || run.dataset == "crosssentence";

            if (LastEvent(t, "nan") >= p.epoch - 1)
            {
                Set(v, "nan", 0, "学习率 " + k.lr + " 太大：权重被撕碎（NaN），练了的又退回去一半。", "Rate " + k.lr + " is too high: the weights tear (NaN) and lose half of what they learnt.",
                    "", "", k.clip ? "学习率调小一档。" : "学习率调小一档，或打开梯度裁剪。", k.clip ? "Lower the rate a notch." : "Lower the rate a notch, or switch on gradient clipping.");
            }
            else if (k.depth > 1 && k.G <= 0)
            {
                Set(v, "step", 1, "卡在第 1 层：阶跃激活没有坡度，误差传不下来，长不出组合。", "Stuck at layer 1: a step has no slope, so no error gets down and nothing combines.",
                    "激活换成「S 形」或「ReLU」。", "Switch the activation to S-curve or ReLU.", "或者只留 1 层，开「特征工程」让人替它组合。", "Or keep one layer and switch on feature engineering so people combine for it.");
            }
            else if (First("relay") is XgLayerHealth relay)
            {
                Set(v, "relay", relay.layer, "卡在第 " + relay.layer + " 层：它的票传到输出只剩 " + Pct(relay.relay) + "，路上的噪声盖过了信号（层太多）。",
                    "Stuck at layer " + relay.layer + ": only " + Pct(relay.relay) + " of its votes reach the answer, drowned in noise on the way up (too many layers).",
                    "打开「跨层直连」（残差）：信号原样到达。", "Switch on skip connections (residual): the signal arrives untouched.",
                    "或者 BatchNorm + 少几层（20 层以内）。", "Or BatchNorm and fewer layers (20 at most).");
            }
            else if (t.Noted("degrade") && p.test < v.goal && !k.skip)
            {
                // Deeper than before and worse even on training cards: point at the layer whose votes arrive weakest.
                var low = h.layers.Count > 0 ? h.layers[0] : null;
                Set(v, "degrade", low != null ? low.layer : 0, "越深越差：加深以后连练过的题都变差了，第 " + (low != null ? low.layer : 1) + " 层的票传到输出只剩 " + Pct(low != null ? low.relay : 1) + "。",
                    "Deeper is worse: since going deeper even the trained cards got worse; layer " + (low != null ? low.layer : 1) + "'s votes reach the answer at only " + Pct(low != null ? low.relay : 1) + ".",
                    "打开「跨层直连」（残差）。", "Switch on skip connections (residual).",
                    k.batchNorm ? "或者少几层。" : "或者 BatchNorm + 少几层。", k.batchNorm ? "Or use fewer layers." : "Or BatchNorm and fewer layers.");
            }
            else if (First("signal") is XgLayerHealth weak)
            {
                Set(v, "signal", weak.layer, "卡在第 " + weak.layer + " 层：误差传到这里只剩 " + Pct(weak.signal) + "，它几乎学不动（梯度消失）。",
                    "Stuck at layer " + weak.layer + ": only " + Pct(weak.signal) + " of the error gets here, so it barely learns (vanishing gradients).",
                    "激活换成「ReLU」：每层只损失一成。", "Switch to ReLU: each layer loses only a tenth.", "或者少几层。", "Or use fewer layers.");
            }
            else if (sequential && h.memory10 < .2 && p.test < v.goal)
            {
                Set(v, "memory", h.depth, "记不住远处：回环每过一个字就忘一点，隔 10 个字只剩 " + Pct(h.memory10) + "。", "Forgets far back: the loop loses a little every word; ten words back only " + Pct(h.memory10) + " is left.",
                    "换「门控记忆」（LSTM / GRU）：让它自己决定记住什么。", "Use gated memory (LSTM / GRU): let it decide what to keep.", "", "");
            }
            else if (full && (t.Noted("thrash") || p.evicted > 0 && t.flat >= 3))
            {
                bool dense = k.wiring == XgWiring.Full;
                Set(v, "cells", 0, "格子不够：" + p.cells + " / " + p.cap + " 全满，上一轮挤掉 " + p.evicted + " 个概念，学了就忘。",
                    "Too few cells: all " + p.cap + " are full and the last epoch squeezed out " + p.evicted + " concepts: learnt and forgotten.",
                    dense ? "换能共用的连法：图用卷积，句子用循环（同一个概念到处复用，省格子）。" : "加宽：给每层更多格子。",
                    dense ? "Use shared wiring: convolution for pictures, recurrence for sentences (one concept reused everywhere saves cells)." : "Widen: more cells per layer.",
                    k.features ? "加宽，或者关掉一些脏数据包让它少学乱七八糟的。" : "加宽，或者开「特征工程」：人先整理好特征，省格子。",
                    k.features ? "Widen, or switch off some dirty packs so it learns less noise." : "Widen, or switch on feature engineering: tidied features take fewer cells.");
            }
            else if (p.train - p.test >= .2f)
            {
                bool dense = k.wiring == XgWiring.Full;
                Set(v, "memorize", 0, "死记硬背：练过的题 " + Pct(p.train) + "，没见过的只有 " + Pct(p.test) + "。", "Memorising: " + Pct(p.train) + " on trained cards, only " + Pct(p.test) + " on unseen ones.",
                    dense ? "换能共用的连法（卷积 / 循环）：学到的东西换个位置也认得。" : "", dense ? "Use shared wiring (convolution / recurrence): what it learns carries over to new positions." : "",
                    "多标数据、买数据包，或研究「数据增强」。", "Label more, buy a pack, or research data augmentation.");
            }
            else if (First("nomerge") is XgLayerHealth empty)
            {
                Set(v, "nomerge", empty.layer, "卡在第 " + empty.layer + " 层：练了很多卡，一个组合都没长出来。", "Stuck at layer " + empty.layer + ": many cards trained and not one combination has grown.",
                    "检查激活（要 S 形或 ReLU）和学习率。", "Check the activation (S-curve or ReLU) and the rate.", "或者开「特征工程」，让人替它组合。", "Or switch on feature engineering so people combine for it.");
            }
            else if (p.test < v.goal && t.flat >= PlateauEpochs)
            {
                Set(v, "plateau", 0, "卡在 " + Pct(p.test) + "（目标 " + Pct(v.goal) + "）：结构没大毛病，但不够大，或者数据不够。", "Stuck at " + Pct(p.test) + " (goal " + Pct(v.goal) + "): nothing broken, but too small or too little data.",
                    "加宽加深，或换更强的结构。", "Go wider or deeper, or a stronger structure.", "多标数据、买数据包；看看是不是在用脏数据包。", "Label more or buy a pack; check for dirty packs.");
            }
            else if (p.test >= v.goal)
            {
                v.id = "passed"; v.headline = "达标：" + Pct(p.test) + " ≥ " + Pct(v.goal) + "。"; v.headlineEn = "On target: " + Pct(p.test) + " ≥ " + Pct(v.goal) + ".";
            }
            else
            {
                v.id = "improving"; v.headline = "还在进步：" + Pct(p.test) + "，离目标 " + Pct(v.goal) + " 还差 " + Math.Round((v.goal - p.test) * 100) + " 分。";
                v.headlineEn = "Still improving: " + Pct(p.test) + ", " + Math.Round((v.goal - p.test) * 100) + " points short of " + Pct(v.goal) + ".";
            }
            if (v.problem) v.since = FirstEventSince(t, v.id == "cells" ? new[] { "cells", "thrash" } : v.id == "memory" ? new[] { "ph:amnesia" } : new[] { v.id });
            return v;
        }

        static void Set(XgVerdict v, string id, int layer, string zh, string en, string structure, string structureEn, string method, string methodEn)
        {
            v.id = id; v.problem = true; v.layer = layer; v.headline = zh; v.headlineEn = en;
            v.structure = structure; v.structureEn = structureEn; v.method = method; v.methodEn = methodEn;
        }

        static int LastEvent(XgTrace t, string id)
        {
            for (int i = t.events.Count - 1; i >= 0; i--) if (t.events[i].id == id) return t.events[i].epoch;
            return int.MinValue;
        }

        /// <summary>The first epoch, in the current settings, at which one of these problems was noted (-1 if none).</summary>
        static int FirstEventSince(XgTrace t, string[] ids)
        {
            foreach (var e in t.events)
                if (e.epoch >= t.segmentFrom && Array.IndexOf(ids, e.id) >= 0) return e.epoch;
            return -1;
        }
    }
}
