using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LingGuangV05.XingGuang;

namespace LingGuangV05.Desktop.XingGuang
{
    // Display-only adapters. All quantities and purchase decisions remain in the simulation.
    public static class XgTechCardCopy
    {
        static string T(XgSim sim, string zh, string en) => sim.English ? en : zh;
        static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        static XgNode At(XgTechEntry entry, XgSim sim) => XgTechTree.Next(entry, sim) ?? entry.members[entry.members.Count - 1];
        public static bool Hidden(XgTechEntry entry, XgSim sim)
            => sim.NodeMystery(entry.First) || XgSim.IsAtlas(entry.First) && sim.Status(entry.First, null) != XgSim.NodeStatus.Owned;

        public static string Name(XgTechEntry entry, XgSim sim)
        {
            if (Hidden(entry, sim)) return XgSim.IsAtlas(entry.First) ? T(sim, "？？？", "???") : sim.NodeName(entry.First);
            string name = XgTechTree.Name(entry, sim);
            var node = At(entry, sim);
            if (node.id == "seq2seq") name += " (Seq2Seq)";
            if (node.id == "textcnn") name += " (TextCNN)";
            if (node.kind == XgNodeKind.Arch)
            {
                var arch = XgCatalog.Arch(node.target);
                // The archive's architecture year is evidence. Stage dates are not invention dates.
                if (arch != null && arch.year > 0) name += "  " + XgCatalog.ShownYear(arch.year);
            }
            return name;
        }

        public static string Summary(XgTechEntry entry, XgSim sim)
        {
            if (Hidden(entry, sim)) return T(sim, "尚待发现", "Not discovered yet");
            var node = At(entry, sim);
            switch (node.kind)
            {
                case XgNodeKind.Depth: return T(sim, "逐层组合简单线索", "Combine simple clues across layers");
                case XgNodeKind.Width: return T(sim, "一层容纳更多线索", "Hold more clues in each layer");
                case XgNodeKind.Dataset: return T(sim, "用更多例题练习", "Learn from more examples");
                case XgNodeKind.Auto: return T(sim, "让程序按时训练", "Let a program train on schedule");
                case XgNodeKind.Phenomenon: return T(sim, "规则交织产生的现象", "A phenomenon born from interacting rules");
                case XgNodeKind.Secret: return T(sim, "把预训练配置串起来", "Put the pre-training setup together");
            }
            switch (node.id)
            {
                case "perceptron": return T(sim, "给线索称重再判断", "Weigh the clues before deciding");
                case "mlp": return T(sim, "多走几层再判断", "Reason through more layers");
                case "lenet": case "alexnet": case "vgg": return T(sim, "先看局部再看整体", "Build the whole from local clues");
                case "googlenet": return T(sim, "同时看大线索和小线索", "Look at several scales together");
                case "rnn": return T(sim, "把前文接给下一个字", "Carry earlier words forward");
                case "lstm": return T(sim, "给记忆装上闸门", "Give memory a set of gates");
                case "gru": return T(sim, "用更少的门筛选记忆", "Filter memory with fewer gates");
                case "seq2seq": return T(sim, "先读完整句再说", "Read the sentence before answering");
                case "attention": return T(sim, "需要时回看原文", "Look back at the source when needed");
                case "caption": return T(sim, "把看图和读字接起来", "Connect seeing with language");
                case "transformer": return T(sim, "让每个字看见全句", "Let each word see the whole sentence");
                case "textcnn": return T(sim, "从短语中找线索", "Find clues in short phrases");
                case "weights": return T(sim, "给每条线索分轻重", "Give clues different weights");
                case "learnrule": return T(sim, "答错就调一点权重", "Adjust weights after a mistake");
                case "bias": return T(sim, "让判断门槛能移动", "Make the decision threshold movable");
                case "step": return T(sim, "过了门槛才放行", "Let a signal through past a threshold");
                case "sigmoid": return T(sim, "把开关变成缓坡", "Turn a switch into a smooth slope");
                case "relu": return T(sim, "正数照传负数归零", "Pass positives; zero the negatives");
                case "backprop": return T(sim, "看一小批再调整", "Review a small batch before updating");
                case "chainrule": return T(sim, "把求导交给程序", "Let a program calculate derivatives");
                case "features": return T(sim, "先把线索组合好", "Combine clues before training");
                case "irnn": return T(sim, "让前文少丢一点", "Keep more of the earlier signal");
                case "position": return T(sim, "给每个字标上位置", "Tag each word with its position");
                case "warmup": return T(sim, "先小步再迈开步", "Start small before taking full steps");
                case "fp16": return T(sim, "用更短的数字计算", "Compute with shorter numbers");
                case "momentum": return T(sim, "记住最近的调整方向", "Remember recent update directions");
                case "rmsprop": return T(sim, "按变化大小调步幅", "Adapt steps to recent change sizes");
                case "adam": return T(sim, "兼顾方向和变化大小", "Use recent directions and magnitudes");
                case "gradclip": return T(sim, "把过大的改动截短", "Trim oversized updates");
                case "dropout": return T(sim, "别只靠几条熟线索", "Avoid relying on just a few clues");
                case "augment": return T(sim, "换个样子继续练", "Practise a different view of the same example");
                case "dataclean": return T(sim, "清掉错误示范", "Clean up incorrect examples");
                case "shared.lr": return T(sim, "找合适的学习步幅", "Find a suitable learning step");
                case "lrschedule": return T(sim, "先大步再磨细节", "Take big steps, then refine details");
                case "batchnorm": return T(sim, "把一批信号调齐", "Align a batch of signal ranges");
                case "layernorm": return T(sim, "把单字内部信号调齐", "Align signals within a word");
                case "cudnn": return T(sim, "用显卡擅长的方法算", "Use routines suited to the GPU");
                case "cooler": return T(sim, "把训练产生的热带走", "Carry training heat away");
                case "transfer": return T(sim, "换结构也带走旧知识", "Carry knowledge into a new architecture");
                case "earlystop": return T(sim, "不再进步就停下", "Stop when improvement stalls");
                case "wordvec": return T(sim, "把词语变成数字", "Represent words with numbers");
                case "beamsearch": return T(sim, "多留几条候选答案", "Keep several candidate answers");
                case "subword": return T(sim, "把生僻词拆成小片段", "Split rare words into familiar pieces");
                case "sft": return T(sim, "照示范学会听要求", "Learn to follow requests from examples");
                case "cot": return T(sim, "把推理拆成几步", "Break reasoning into steps");
                case "multihead": return T(sim, "几组注意力各看线索", "Let several heads look for clues");
                case "resnet": case "residual": return T(sim, "给信息留条直达路", "Give signals a shortcut");
                case "datacenter": return T(sim, "搬进有专门供电的机柜", "Move into a rack with dedicated power");
                case "label.raise": return T(sim, "亲手答题多赚一点", "Earn more for hand-labelled answers");
                case "label.auto": return T(sim, "让合格模型来代答", "Let a qualified model answer");
                case "label.coop": return T(sim, "拿不准就交你复核", "Send uncertain answers to review");
                case "label.brain": return T(sim, "让本地模型读文字题", "Let the local model read text cards");
                case "label.hard": return T(sim, "复核难题有额外奖励", "Reward difficult reviews");
                case "label.audit": return T(sim, "在错题入库前抽查", "Audit answers before they enter the data");
                case "label.parallel": return T(sim, "同时处理更多判断", "Handle more judgments at once");
                case "label.trapmemory": return T(sim, "记住用来质检的题", "Remember known quality-check items");
                case "label.captcha": return T(sim, "自动填完数字验证", "Fill in digit verification automatically");
                default: return T(sim, "解锁新的学习方法", "Unlock another way to learn");
            }
        }

        public static string Effect(XgTechEntry entry, XgSim sim)
        {
            if (Hidden(entry, sim)) return T(sim, "效果尚未发现", "Effect not discovered yet");
            var node = At(entry, sim);
            if (node.kind == XgNodeKind.Phenomenon) return T(sim, "发生后自动记录 · 无购买加成", "Recorded when observed · no purchase bonus");
            if (node.id == "position") return T(sim, "支持的结构可学习位置信号", "Supported architectures can learn position clues");
            if (node.id == "caption") return T(sim, "新表情包题显示预设描述和关注区域", "New meme cards show preset descriptions and focus regions");
            var discounts = XgSim.DiscountsOf(node.id);
            if (discounts.Count > 0)
            {
                var text = new StringBuilder();
                foreach (var discount in discounts)
                {
                    if (text.Length > 0) text.Append("\n");
                    text.Append(T(sim, "能力", "Ability ")).Append(discount.ability).Append(" · ");
                    if (discount.paramsFactor < 1) text.Append(T(sim, "参数门槛 ×", "parameter threshold ×")).Append(N(discount.paramsFactor));
                    if (discount.paramsFactor < 1 && discount.samplesFactor < 1) text.Append("\n");
                    if (discount.samplesFactor < 1) text.Append(T(sim, "样本门槛 ×", "sample threshold ×")).Append(N(discount.samplesFactor));
                    if (discount.itemsAny.Length > 0) text.Append(T(sim, "（同组一次）", " (once per group)"));
                }
                return text.ToString();
            }
            switch (node.kind)
            {
                case XgNodeKind.Depth: return T(sim, "可用层数上限 → ", "Available depth cap → ") + node.value;
                case XgNodeKind.Width: return T(sim, "每层宽度上限 → ", "Layer width cap → ") + XgCatalog.Widths[Math.Max(0, Math.Min(node.value, XgCatalog.Widths.Length - 1))];
                case XgNodeKind.Dataset: return T(sim, "数据包样本 ", "Pack samples ") + XgSim.SamplesText(XgCatalog.Dataset(node.target).samples);
                case XgNodeKind.Auto: return T(sim, "每 ", "Attempt 1 epoch every ") + N(XgSim.AutoInterval(node.value)) + T(sim, " 秒尝试训练 1 轮", " s");
            }
            foreach (var scale in XgSim.ScaleItems)
                if (scale.item == node.id) return T(sim, "预训练有效规模 ×", "Effective pre-training scale ×") + N(scale.factor);
            if (node.id == "label.raise")
            {
                int level = Math.Max(0, Math.Min(sim.RaiseLevel, XgCatalog.RaiseMax));
                return T(sim, "人工答题报酬 ×", "Hand-answer pay ×") + N(XgCatalog.RaiseMultipliers[level])
                    + (level < XgCatalog.RaiseMax ? " → ×" + N(XgCatalog.RaiseMultipliers[level + 1]) : "");
            }
            if (node.id == "label.auto") return T(sim, "每级每个合格台基础速度 +", "Base speed per level per eligible desk +") + N(XgCatalog.AutoRatePerLevel) + T(sim, " 题/秒", " cards/s");
            if (node.id == "dropout") return T(sim, "训练跳过约 ", "Training skips updates to about ") + N(XgBoard.DropoutRate * 100) + T(sim, "% 匹配线索的更新", "% of matched clues");
            if (node.id == "gradclip") return T(sim, "单卡拉动权重限制 ±", "Per-card weight update limit ±") + N(XgBoard.ClipLimit);
            if (node.id == "warmup") return T(sim, "前 ", "Warm up over ") + XgSim.WarmupCards + T(sim, " 张卡逐步升学习率", " cards");
            if (node.id == "datacenter") return T(sim, "可用训练显存 ×", "Usable training VRAM ×") + N(XgSim.DatacenterVram);
            if (node.id == "label.audit") return T(sim, "自动错题退回复核概率 ", "Automatic wrong answers sent to review: ") + N(XgSim.AuditProbability * 100) + "%";
            if (node.id == "label.captcha") return T(sim, "新验证码代填延迟 ", "New captcha autofill delay ") + N(XgSim.AutofillMinDelay) + "–" + N(XgSim.AutofillMaxDelay) + T(sim, " 秒", " s");
            // Retain a whole authored effect sentence, including its conditions. Never clip a
            // sentence at a number or invent a before/after value from an unrelated stat.
            var effect = EffectText(node, sim);
            foreach (var sentence in Sentences(effect))
                if (Regex.IsMatch(sentence, "[0-9]")) return sentence;
            var all = Sentences(effect);
            return all.Count > 0 ? all[0] : T(sim, "详情中查看规则", "Read the rule in Details");
        }

        static string EffectText(XgNode node, XgSim sim)
        {
            string note = XgTechTreeCopy.Note(node, sim);
            string marker = T(sim, "\n\n效果：", "\n\nEffect: ");
            int split = note.IndexOf(marker, StringComparison.Ordinal);
            return split >= 0 ? note.Substring(split + marker.Length) : note;
        }

        static List<string> Sentences(string text)
        {
            var result = new List<string>();
            foreach (string part in Regex.Split(text ?? "", @"(?<=[。！？])|(?<=[.!?])\s*(?=[A-Z])"))
                if (!string.IsNullOrWhiteSpace(part)) result.Add(part.Trim());
            return result;
        }

        public static string Science(XgTechEntry entry, XgSim sim)
        {
            if (Hidden(entry, sim)) return XgSim.IsAtlas(entry.First)
                ? T(sim, "还没发生过。规则叠在一起，它自己会冒出来；第一次发生时会记进图鉴。", "Not seen yet. Interacting rules can produce it; its first occurrence will enter the atlas.")
                : sim.NodeNote(entry.First);
            // Preserve the reviewed explanation and effect facts. Three to five complete
            // sentences are selected; no numeric fragment is detached from its qualifier.
            var node = At(entry, sim);
            if (node.kind == XgNodeKind.Phenomenon) return PhenomenonScience(node.target, sim);
            string note = XgTechTreeCopy.Note(node, sim);
            string marker = T(sim, "\n\n效果：", "\n\nEffect: ");
            int split = note.IndexOf(marker, StringComparison.Ordinal);
            if (split >= 0) note = note.Substring(0, split) + "\n" + note.Substring(split + marker.Length);
            if (node.kind == XgNodeKind.Dataset)
                note += T(sim, "样本入库不等于模型已经学会，仍需训练。", " Adding samples does not mean the model has learned them; training is still needed.");
            if (node.id == "layernorm")
                note += T(sim, "归一化有助于减少各项信号尺度相差过大带来的影响。", " Normalization helps reduce the effect of signals having very different scales.");
            if (node.id == "residual")
                note += T(sim, "绕行的信号会与经过中间层处理的信号汇合。", " The shortcut signal joins the signal processed by the intervening layers.");
            if (node.id == "label.hard") note = T(sim, "为你处理需要复核的难题提供额外奖励。新题人工复核答对时，报酬×1.5，样本×2。旧数据重审不享受此奖励。", "Reward difficult cards that need human review. Correct human reviews of new cards pay ×1.5 and award ×2 samples. Rechecking old data does not earn this bonus.");
            if (node.id == "label.trapmemory") note = T(sim, "平台会用已知答案的标准题检查标注质量。这个节点记住遇到过的金标准题。再次遇到时转入人工复核，不自动提交。", "The platform checks labelling quality with known-answer items. This node remembers gold-standard items it has encountered. Remembered items go to human review instead of automatic submission.");
            var sentences = Sentences(note);
            return string.Join(sim.English ? " " : "", sentences.GetRange(0, Math.Min(5, sentences.Count)));
        }

        static string PhenomenonScience(string id, XgSim sim)
        {
            // Observations describe what the simulation detects, never a purchasable bonus.
            // In particular, recurrent memory decays continuously rather than stopping at 8 words.
            switch (id)
            {
                case "xor": return T(sim, "异或要求恰好一个输入为真。单条直线无法把这种组合完整分开。网络需要能表达组合关系的线索或结构。", "XOR asks whether exactly one input is true. One straight boundary cannot separate every combination. The network needs clues or wiring that can represent the combination.");
                case "vanish": return T(sim, "反向学习信号经过多层时可能逐层减弱。传到底层的改动很小，早期线索就难以学动。激活函数和网络结构会影响信号保留。", "Backward learning signals can weaken across successive layers. Tiny updates make early clues difficult to learn. Activation functions and network wiring affect signal retention.");
                case "memorize": return T(sim, "模型可能记住训练题的特殊细节。这样练过的题表现很好，没见过的题却不一定答对。训练与评估成绩的差距可以提示这种过拟合。", "A model can memorize details specific to training examples. It may answer familiar cards well without succeeding on unseen ones. The gap between training and evaluation scores can reveal this overfitting.");
                case "grok": return T(sim, "训练题表现好，不代表模型已掌握通用规律。有时测试成绩在长时间停滞后才明显提高。游戏通过连续评估的成绩变化识别这种现象。", "Strong training scores do not necessarily mean a general rule has been learned. Test performance can improve sharply after a long plateau. The game identifies this pattern from successive evaluations.");
                case "generalize": return T(sim, "泛化是把学到的线索用于没练过的题。它比记住某张训练卡更有用。游戏用未见样本的评估成绩观察这种能力。", "Generalization applies learned clues to unseen examples. It is more useful than memorizing a particular training card. The game observes it through evaluation on unseen samples.");
                case "deeper": return T(sim, "增加普通层不保证成绩提高。层间信号的损失可能让深层网络比浅层网络更难学。残差连接给信息提供一条绕过中间层的路。", "Adding plain layers does not guarantee better scores. Signal loss between layers can make a deeper network harder to train. Residual connections give signals a route around intermediate layers.");
                case "amnesia": return T(sim, "循环网络把前文信号逐步传给后文。每一步的损失相乘，远处的线索会越来越弱。记忆没有固定字数截断，门控和其他技术会改变保留情况。", "A recurrent network carries earlier signals forward one position at a time. Losses compound, so distant clues grow weaker. Memory has no fixed word-count cutoff; gates and other techniques change retention.");
                case "forget": return T(sim, "不同数据集可能共用一部分参数。持续训练新数据会改写旧任务依赖的线索。回头评估旧数据时，成绩下降就能暴露遗忘。", "Different datasets can share parameters. Continued training on new data can rewrite clues used by an older task. Evaluating the older data again can expose this forgetting.");
                case "sycophancy": return T(sim, "只奖励好听的回答会强化附和倾向。回答迎合了你的偏好，不代表事实判断正确。应把是否真实与是否悦耳分开检查。", "Rewarding only pleasing answers can strengthen agreement. Matching a preference does not make a factual judgment correct. Check truth separately from how agreeable the answer sounds.");
                case "hallucinate": return T(sim, "游戏用共享格子里的概念混淆来类比幻觉。模型可能把不相干的线索合在一起，还给出肯定答案。真实模型产生错误内容的原因比这个教学类比更复杂。", "The game illustrates hallucination through confused concepts sharing a cell. Unrelated clues can combine into a confident but incorrect answer. Real models produce incorrect content for reasons more complex than this teaching analogy.");
                case "shortcut": return T(sim, "经常一起出现的特征不一定有因果关系。模型可能只看配文之类的捷径，忽略真正要判断的内容。含反例的数据能帮助区分相关线索与可靠线索。", "Features appearing together need not have a causal relationship. A model can rely on shortcuts such as captions instead of the intended evidence. Counterexamples help separate accidental correlations from reliable clues.");
                case "transfer": return T(sim, "不同任务有时需要相同的基础线索。旧数据练出的概念可以帮助新数据集。迁移是否有效仍取决于任务和结构是否支持这些线索。", "Different tasks can use the same basic clues. Concepts learned from earlier data can help a new dataset. Whether transfer works still depends on the task and its supporting architecture.");
                case "drift": return T(sim, "你的目标和实际训练材料可能不一致。持续收到的例子会把模型推向另一种表现。性格面板把目标与实际并列，帮助发现这种偏移。", "Your target can differ from the examples used for training. Repeated examples can move the model toward another pattern of behavior. The personality panel compares target and actual values to expose the drift.");
                case "translation": return T(sim, "翻译需要把一句话与另一句话对应起来。只沿自身序列传递线索的结构不擅长这种对齐。编码器与解码器等结构可以连接输入和输出序列。", "Translation needs correspondences between two sentences. Wiring that only carries clues along its own sequence struggles with this alignment. Encoder-decoder structures can connect input and output sequences.");
                case "bottleneck": return T(sim, "编码器先把输入压成固定大小的摘要。长句里的细节可能超过摘要能保留的范围。注意力让输出阶段可以回看输入，减轻这种限制。", "An encoder first compresses input into a fixed-size summary. A long sentence can contain more detail than that summary preserves. Attention lets output generation look back at the input to ease the restriction.");
                case "serial": return T(sim, "循环计算的后一步依赖前一步。句子越长，顺序计算的步骤就越多。增加并行资源可以多处理几句，却不能直接消除同一句内部的依赖。", "Each recurrent step depends on the previous step. Longer sentences require more sequential work. Parallel resources can process more sentences without removing the dependencies within one sentence.");
                case "inbreeding": return T(sim, "自动标注也可能答错。漏检的错误答案进入训练后，模型会把自己的错误当作示范。抽检和复核能够减少这类数据污染。", "Automatic labelling can be wrong. Undetected answers can return to training as examples of the model's own mistakes. Audits and human review can reduce this data contamination.");
                case "emergence": return T(sim, "一项能力可能依赖多种线索共同起作用。多个条件先后积累，到满足门槛时才显出新的行为。看起来突然出现的能力，仍有之前训练的积累。", "An ability can depend on several clues working together. Conditions accumulate before new behavior appears at a threshold. A seemingly sudden ability can still depend on earlier training.");
                default: throw new ArgumentOutOfRangeException(nameof(id), id, "A new phenomenon needs reviewed card science.");
            }
        }
    }
}
