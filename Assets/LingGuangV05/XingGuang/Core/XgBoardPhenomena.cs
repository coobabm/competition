using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>One evaluation of one dataset, as seen by the phenomenon detectors.</summary>
    public sealed class XgObservation
    {
        public string dataset = "", region = "";
        public XgKnobs knobs = new XgKnobs();
        public double train, test;
        /// <summary>Cards trained on this dataset so far.</summary>
        public long cards;
        /// <summary>Cards trained on other datasets since this one was last trained.</summary>
        public long idle;
        /// <summary>Longest sequence in the dataset (0 for non-sequences).</summary>
        public int maxLength;
        /// <summary>Cards trained in this dataset's region (all its datasets together).</summary>
        public long regionCards;
        /// <summary>Data augmentation is owned (it weakens shortcut bias).</summary>
        public bool augmented;
    }

    [Serializable]
    public sealed class XgScore { public string key = ""; public double value; public int count; }

    /// <summary>What the detectors remember between observations (saved with the lab).</summary>
    [Serializable]
    public sealed class XgPhenomenaMemory
    {
        public List<string> seen = new List<string>();
        public List<XgScore> best = new List<XgScore>();
        public List<XgScore> last = new List<XgScore>();
        /// <summary>Observations in a row without a test improvement, per dataset.</summary>
        public List<XgScore> flat = new List<XgScore>();
    }

    public sealed class XgPhenomenon
    {
        public string id, name, nameEn, why, whyEn;
        public int stage;
    }

    /// <summary>
    /// Phenomena are never triggered, only detected (design v1.1 §4.5). The first time one shows up, its 图鉴 node lights.
    /// </summary>
    public static class XgPhenomena
    {
        public static readonly XgPhenomenon[] All =
        {
            P("xor", 1, "异或不可分", "XOR is not separable", "单层只能画一条直线，“恰好一个为真”分不开，准确率卡在 50%。", "One layer draws one straight line; \"exactly one is true\" cannot be split, so accuracy sits at 50%."),
            P("vanish", 2, "梯度消失", "Vanishing gradients", "S 形激活每往下一层只剩四分之一，层数一多，底层概念几乎不动。", "An S-curve passes a quarter per layer; with many layers the bottom concepts barely move."),
            P("memorize", 2, "死记硬背", "Memorisation", "格子多、数据少：每张卡长出一个专属概念，训练卡全对，测试卡全错。", "Many cells, little data: each card grows its own concept; training cards right, test cards wrong."),
            P("grok", 2, "顿悟", "Grokking", "训练了很久都没起色，测试准确率却突然跳了上去。", "Nothing for a long time, then test accuracy suddenly jumps."),
            P("generalize", 2, "泛化", "Generalisation", "第一次答对一张从没训练过的卡。", "It answers a card it has never been trained on."),
            P("deeper", 3, "越深越差", "Deeper is worse", "多加的普通层本该什么都不做、原样往上传，可它学不会：每层都改动一点，层一多，连练过的题都变差（2015 年 ResNet 论文发现的「退化」）。弱小和无知不是生存的障碍，傲慢才是。", "Extra plain layers should just pass things on unchanged, and they cannot learn to: each changes a little, and with enough of them even the trained cards get worse (the 'degradation' the 2015 ResNet paper found). Weakness and ignorance are not barriers to survival, arrogance is."),
            P("amnesia", 3, "长句失忆", "Long-sentence amnesia", "回环每一步 ×0.75，八个字以后开头就接不上了。", "A loop keeps ×0.75 per step; after eight characters the start is gone."),
            P("forget", 3, "灾难性遗忘", "Catastrophic forgetting", "一直只练别的，这个数据集的概念慢慢衰减掉了。", "Training only on something else lets this dataset's concepts fade."),
            P("sycophancy", 6, "讨好", "Sycophancy", "你总挑好听的回答，「附和」就越来越强，它开始顺着你说错话。", "Pick the nicer-sounding answer every time and \"agree\" grows strong: it starts going along with your mistakes."),
            P("hallucinate", 4, "幻觉", "Hallucination", "两个概念叠在一格，它把两者混为一谈，还说得很肯定。", "Two concepts share one cell; it mixes them up and says so with confidence."),
            P("shortcut", 3, "捷径偏见", "Shortcut bias", "碰巧总一起出现的东西也会被连起来：狗的图都在草地上，它就把「草地」当成了「狗」。数据增强能削弱它。", "Things that merely appear together get linked: every dog photo was on grass, so \"grass\" became \"dog\". Augmentation weakens it."),
            P("transfer", 3, "迁移", "Transfer", "别的数据集练出来的概念，在这里直接用上了：没练几张卡就答得不错。", "Concepts learned on other datasets work here directly: good answers after very few cards."),
            P("drift", 4, "人格漂移", "Personality drift", "你希望它是一个样子，喂给它的卡却把它推向另一个样子。看性格面板的「目标」和「实际」。", "You wanted it one way, but the cards you fed it pushed it another. Compare target and actual on the personality panel."),
            P("translation", 4, "翻译不了", "Cannot translate", "一条序列只够得着自己相邻的字，够不着另一条序列。", "A sequence model only reaches its own neighbouring words, never the other sentence."),
            P("bottleneck", 5, "定长瓶颈", "Fixed-length bottleneck", "整句压进一格再说出来：短句能翻，长句一翻就乱。", "The whole sentence squeezed into one cell: short ones translate, long ones fall apart."),
            P("serial", 5, "串行瓶颈", "Serial bottleneck", "循环在一句话里只能一个字一个字地算，句子越长越慢；加显卡只能多算几句，快不了一句。", "A loop computes a sentence one word at a time, slower the longer it is; more cards run more sentences, not one faster."),
            P("inbreeding", 2, "近亲繁殖", "Inbreeding", "自动标错又没被抽检发现的题混进了训练数据，它把自己的错当成答案又学了回去。错得越多，成绩越被拖住。", "Wrong automatic labels nobody caught went back into the training data; it learned its own mistakes as answers. The more there are, the harder the score is held down."),
            P("emergence", 6, "能力涌现", "Emergent abilities", "多个高层概念同时过了阈值，一项能力从完全没有，到突然出现。", "Several high-level concepts cross the threshold together; an ability goes from nothing to there."),
        };

        static XgPhenomenon P(string id, int stage, string name, string en, string why, string whyEn)
        { return new XgPhenomenon { id = id, stage = stage, name = name, nameEn = en, why = why, whyEn = whyEn }; }

        public static XgPhenomenon Get(string id) { foreach (var p in All) if (p.id == id) return p; return null; }

        static XgScore Score(List<XgScore> list, string key)
        {
            foreach (var s in list) if (s.key == key) return s;
            var made = new XgScore { key = key, value = double.NaN }; list.Add(made); return made;
        }

        /// <summary>Records the observation and returns phenomena seen for the first time.</summary>
        public static List<string> Observe(XgObservation o, XgPhenomenaMemory m, XgBoard brain)
        {
            var found = new List<string>();
            void Found(string id) { if (!m.seen.Contains(id) && !found.Contains(id)) found.Add(id); }
            var k = o.knobs;
            var best = Score(m.best, o.dataset); var last = Score(m.last, o.dataset); var flat = Score(m.flat, o.dataset);
            if (double.IsNaN(flat.value)) flat.value = 0; // only its count is used; NaN would end up in the save
            double previous = last.value, peak = double.IsNaN(best.value) ? 0 : best.value;

            if (o.dataset == "xor" && o.cards >= 300 && o.test > .35 && o.test < .65) Found("xor");
            if (k.depth > 4 && k.G > 0 && k.G < .5 && !k.skip && o.cards >= 300 && o.test < .7) Found("vanish");
            if (o.train >= .8 && o.train - o.test >= .25) Found("memorize");
            if (o.test >= .7 && o.cards > 0) Found("generalize");
            if (!double.IsNaN(previous) && flat.count >= 4 && o.test - previous >= .2) Found("grok");
            if (k.depth > 19 && !k.skip && k.G > 0 && o.test < peak - .05) Found("deeper");
            if (o.region == "sequence" && k.wiring == XgWiring.Recurrent && o.maxLength > 8 && o.cards >= 300 && o.test < .65) Found("amnesia");
            if (peak >= .7 && o.idle >= 500 && o.test <= peak - .2) Found("forget");
            if (brain != null && brain.S.superposed > 0 && o.train - o.test >= .1) Found("hallucinate");
            if (o.region == "vision" && !o.augmented && o.cards >= 300 && o.train - o.test >= .1 && o.train - o.test < .25) Found("shortcut");
            if (o.cards > 0 && o.cards <= 150 && o.regionCards - o.cards >= 600 && o.test >= .6) Found("transfer");
            if (k.wiring == XgWiring.EncoderDecoder && o.dataset == "translate" && o.cards >= 300 && o.test < .8) Found("bottleneck");

            if (!double.IsNaN(previous) && o.test <= previous + .01) flat.count++; else flat.count = 0;
            last.value = o.test;
            if (double.IsNaN(best.value) || o.test > best.value) best.value = o.test;
            m.seen.AddRange(found);
            return found;
        }
    }
}
