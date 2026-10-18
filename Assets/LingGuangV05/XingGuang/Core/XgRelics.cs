using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public enum XgRarity { Rare, Epic, Legendary }

    /// <summary>
    /// The sum of what the equipped relics do to the sim, one number per channel. The identity values (1 or 0) mean "no relic".
    /// <see cref="XgSim.RelicFx"/> rebuilds it whenever the equipped slots change; the sim reads its channels where the real
    /// numbers are made (samples, training gain, thresholds, VRAM, rent, ...).
    /// </summary>
    public sealed class XgRelicFx
    {
        /// <summary>Extra samples per hand label (×2 is 1, ×3 is 2), and how many wrong rows those extras bring per label (the mislabelled share).</summary>
        public double extraSamples, noisePerLabel;
        /// <summary>Training gain per round: all lines, the vision line, the sequence line (added: 0.4 is +40%).</summary>
        public double gain, gainVision, gainSequence;
        /// <summary>Multipliers on the two ability thresholds (parameters, samples).</summary>
        public double paramsFactor = 1, samplesFactor = 1;
        /// <summary>Added to the VRAM multiplier (1 + this).</summary>
        public double vramBonus;
        /// <summary>Extra samples per correct mini-game answer.</summary>
        public int miniExtra;
        /// <summary>Handwriting memory limit multiplier.</summary>
        public double handwritingMult = 1;
        /// <summary>Added to the yes/no accuracy.</summary>
        public double yesNo;
        /// <summary>Share of the rent that is not paid.</summary>
        public double rentCut;
        /// <summary>Share of the training power bill that is not paid, and the share added on top.</summary>
        public double powerCut, powerRise;
        /// <summary>The combo never breaks; and how much the combo bonus cap rises above ×2.</summary>
        public bool comboKeep;
        public double comboCapBonus;
        /// <summary>Share of the chance of a training drop that is removed.</summary>
        public double dropCut;
        /// <summary>Every this many rounds the next drop does not count (0 = none).</summary>
        public int guardEvery;
        /// <summary>Added to the cards fed per round (0.5 is +50%).</summary>
        public double cardsBonus;
        /// <summary>Samples per word the spider reads, multiplied.</summary>
        public double readMult = 1;
        /// <summary>Share off the tech tree prices.</summary>
        public double techCut;
        /// <summary>Added to contract and auto-answer income.</summary>
        public double incomeBonus;
    }

    /// <summary>One rare item: names, flavour, the effect in one line and how it changes <see cref="XgRelicFx"/>.</summary>
    public sealed class XgRelic
    {
        public string id = "", zh = "", en = "", flavorZh = "", flavorEn = "", effectZh = "", effectEn = "";
        public XgRarity rarity;
        public Action<XgRelicFx> apply;
        /// <summary>The ability that wants this effect shown with a symbol on the tile: one or two letters.</summary>
        public string glyph = "";
    }

    /// <summary>Two relics equipped together give one more effect.</summary>
    public sealed class XgRelicPair
    {
        public string a = "", b = "", zh = "", en = "", textZh = "", textEn = "";
        public Action<XgRelicFx> apply;
    }

    /// <summary>
    /// 稀有道具: 24 relics of 2016 China's tech and internet culture and of machine-learning history. Each is strong and
    /// does one clear thing to a real number of the sim; the player holds any number in the backpack and equips four initially, expandable to eight or twelve.
    /// The full table with the numbers, sources and pairs is Documentation/LingGuang-Incremental/稀有道具.md.
    /// </summary>
    public static class XgRelics
    {
        // Slots remains the legacy/new-game capacity; never use it as the purchased capacity.
        public const int Slots = 4, MaxSlots = 12;

        public static string RarityName(XgRarity r, bool english) => english ? new[] { "Rare", "Epic", "Legendary" }[(int)r] : new[] { "稀有", "史诗", "传说" }[(int)r];

        static XgRelic R(string id, XgRarity rarity, string glyph, string zh, string en, string flavorZh, string flavorEn, string effectZh, string effectEn, Action<XgRelicFx> apply)
            => new XgRelic { id = id, rarity = rarity, glyph = glyph, zh = zh, en = en, flavorZh = flavorZh, flavorEn = flavorEn, effectZh = effectZh, effectEn = effectEn, apply = apply };

        public static readonly XgRelic[] All =
        {
            // ───────────── 稀有 ─────────────
            R("dropout_paper", XgRarity.Rare, "Dr", "《Dropout》论文复印件", "Dog-eared Dropout paper",
                "折了角的复印件，页边写着：随机扔掉一半，反而记得更牢。", "A dog-eared photocopy. In the margin: throw half away at random and it remembers better.",
                "训练每轮准确率提升 +40%", "Each training round gains +40% accuracy", f => f.gain += .40),
            R("zhou_usb", XgRarity.Rare, "U", "老周的 U 盘", "Lao Zhou's USB stick",
                "「新建文件夹 (3)」里是他攒了三年的标注。", "The folder 'New Folder (3)' holds three years of his labels.",
                "手标样本 ×2", "Hand-labelled samples ×2", f => f.extraSamples += 1),
            R("watermelon_book", XgRarity.Rare, "瓜", "绿皮「西瓜书」", "The green Watermelon Book",
                "封面绿得像一块西瓜，翻开第一章就是「是」和「否」。", "The cover is watermelon green; chapter one is all about yes and no.",
                "灵光的是/否准确率 +15%", "Yes/no accuracy +15%", f => f.yesNo += .15),
            R("tablet", XgRarity.Rare, "笔", "十五块的数位板", "A ¥15 drawing tablet",
                "比鼠标好写十倍，笔尖有点飘。", "Ten times better than a mouse for handwriting; the pen tip drifts a little.",
                "手写识别记忆上限 ×3", "Handwriting memory limit ×3", f => f.handwritingMult *= 3),
            R("mining_1080", XgRarity.Rare, "矿", "矿卡 1080", "Ex-mining GTX 1080",
                "「成色 95 新，没怎么用过。」风扇里一股焦味。", "'95% new, barely used.' The fan smells scorched.",
                "显存 ×2，但训练电费 +30%", "VRAM ×2, but training power +30%", f => { f.vramBonus += 1; f.powerRise += .30; }),
            R("coupon", XgRarity.Rare, "券", "满减优惠券", "Coupon booklet",
                "满 300 减 30，叠加使用，论坛里的人都这么省。", "Spend 300, save 30, stackable. Everyone on the forum saves like this.",
                "科技树价格 −10%", "Tech tree prices −10%", f => f.techCut += .10),
            R("server_key", XgRarity.Rare, "钥", "实验室的旧服务器钥匙", "Old lab server-room key",
                "没人记得是哪间屋的。试了一下，真能开，里面还有张行军床。", "Nobody remembers which room. It opens one, with a camp bed inside.",
                "房租 −25%", "Rent −25%", f => f.rentCut += .25),
            R("multimeter", XgRarity.Rare, "电", "老电工的万用表", "Old electrician's multimeter",
                "「你家用的电，比我整栋楼都多。」", "'Your house burns more power than my whole building.'",
                "训练电费 −40%", "Training power cost −40%", f => f.powerCut += .40),
            R("spider_ua", XgRarity.Rare, "蛛", "伪装成摆渡蜘蛛的 UA", "A forged Bodu spider UA",
                "网站看了它一眼，放行了。", "The site took one look and waved it through.",
                "读到的词每个算 3 条样本", "Every word read counts as 3 samples", f => f.readMult *= 3),
            R("arcade_tokens", XgRarity.Rare, "币", "一袋街机游戏币", "A bag of arcade tokens",
                "三块钱十个币，够玩一下午。", "Three yuan for ten tokens, enough for an afternoon.",
                "每个小游戏答对 +1 额外样本", "+1 extra sample per correct mini-game answer", f => f.miniExtra += 1),

            // ───────────── 史诗 ─────────────
            R("tieba", XgRarity.Epic, "吧", "摆渡贴吧「精品帖」合集", "Tieba 'featured posts' archive",
                "每层楼都在回「楼主好人」，但内容是真的。", "Every floor says 'thanks, OP', but the content is real.",
                "能力门槛 −15%（参数和样本）", "Ability thresholds −15% (parameters and samples)", f => { f.paramsFactor *= .85; f.samplesFactor *= .85; }),
            R("allnight", XgRarity.Epic, "夜", "网吧通宵卡", "All-night café pass",
                "包夜十块，送一碗泡面。键盘上还留着上个人的油手印。", "Ten yuan for the night, instant noodles included. The last guy's greasy prints are still on the keys.",
                "连击不会断", "The combo never breaks", f => f.comboKeep = true),
            R("redbull", XgRarity.Epic, "牛", "一箱红牛", "A case of Red Bull",
                "纸箱上有行小字：别连续喝六罐。", "Small print on the box: do not drink six in a row.",
                "每轮训练喂的卡片 +50%", "+50% cards fed per training round", f => f.cardsBonus += .50),
            R("imagenet_disc", XgRarity.Epic, "盘", "ImageNet 光盘", "ImageNet disc",
                "一千类，一百多万张，刻成了十几张盘，盘面有划痕。", "A thousand classes, over a million pictures, burned onto a dozen scratched discs.",
                "视觉线训练每轮提升 +60%", "Vision line gains +60% per round", f => f.gainVision += .60),
            R("batchnorm_paper", XgRarity.Epic, "BN", "《Batch Normalization》论文", "Batch Normalization paper",
                "「内部协变量偏移」，念了三遍才读顺。读顺以后训练就稳了。", "'Internal covariate shift' took three readings. Once it clicks, training steadies.",
                "训练退步的概率 −50%", "Chance of a training drop −50%", f => f.dropCut += .50),
            R("resnet_sticker", XgRarity.Epic, "残", "显示器上的「残差」贴纸", "'Residual' sticker on the monitor",
                "一条绕过去的箭头。掉下去的东西，有人接着。", "An arrow that goes around. Whatever falls gets caught.",
                "每练 8 轮，下一次失败的训练不计入", "Every 8 rounds, the next failed training does not count", f => f.guardEvery = f.guardEvery == 0 ? 8 : Math.Min(f.guardEvery, 8)),
            R("data_broker", XgRarity.Epic, "贩", "数据贩子的名片", "A data broker's card",
                "「要数据找我，不问来路。」背面是一个 QQ 号。", "'Need data? Ask me, no questions about origin.' A QQ number on the back.",
                "样本门槛 −20%", "Sample threshold −20%", f => f.samplesFactor *= .8),
            R("campus_vpn", XgRarity.Epic, "网", "校园网的 VPN 账号", "Campus-network VPN login",
                "能看到外面的论文，一天省掉三天的弯路。", "It reaches the papers outside; one day saves three days of detours.",
                "参数门槛 −20%", "Parameter threshold −20%", f => f.paramsFactor *= .8),
            R("gold_cert", XgRarity.Epic, "金", "摆渡众包「金牌」认证", "Bodu Crowdsourcing Gold badge",
                "右下角的小金牌，客户看了就放心。", "A small gold badge in the corner; clients relax when they see it.",
                "订单和自动答题收入 +20%", "Contract and auto-answer income +20%", f => f.incomeBonus += .20),

            // ───────────── 传说（只出现在学会能力的三选一里）─────────────
            R("attention_scratch", XgRarity.Legendary, "At", "「Attention」草稿纸", "'Attention' scratch paper",
                "字迹潦草，日期写着 2017。你确定这不是未来寄来的？", "Scribbled notes dated 2017. Sure this was not mailed from the future?",
                "序列线训练每轮提升 +100%", "Sequence line gains +100% per round", f => f.gainSequence += 1.0),
            R("alphago_book", XgRarity.Legendary, "围", "AlphaGo 棋谱复盘本", "AlphaGo game-record notebook",
                "一页页复盘，复到后来，分不清是它教你还是你教它。", "Replaying move after move, you stop knowing whether it teaches you or you teach it.",
                "手标样本 ×3，但多出来的样本 10% 标错", "Hand-labelled samples ×3, but 10% of the extras are mislabelled", f => { f.extraSamples += 2; f.noisePerLabel += .20; }),
            R("taihulight", XgRarity.Legendary, "神", "「神威·太湖之光」机时卡", "Sunway TaihuLight time card",
                "2016 年 6 月全球第一。你的一小时，是别人的一年。", "World number one, June 2016. Your hour is someone else's year.",
                "显存 ×3，训练每轮准确率提升 +30%", "VRAM ×3, each training round gains +30%", f => { f.vramBonus += 2; f.gain += .30; }),
            R("advisor_page", XgRarity.Legendary, "签", "导师的署名页", "Advisor's signature page",
                "一个签名，所有人都开始相信你的结果。", "One signature, and suddenly everyone believes your results.",
                "能力门槛 −25%（参数和样本）", "Ability thresholds −25% (parameters and samples)", f => { f.paramsFactor *= .75; f.samplesFactor *= .75; }),
            R("angel_term", XgRarity.Legendary, "投", "天使轮投资意向书", "Angel-round term sheet",
                "「我们看好你。」下面是一行写着「估值」的空格。", "'We believe in you.' Below it, a blank line labelled valuation.",
                "订单和自动答题收入 +30%，科技树价格 −10%", "Contract and auto-answer income +30%, tech tree prices −10%", f => { f.incomeBonus += .30; f.techCut += .10; }),
        };

        public static readonly XgRelicPair[] Pairs =
        {
            new XgRelicPair { a = "imagenet_disc", b = "attention_scratch", zh = "两线齐飞", en = "Both lines fly",
                textZh = "两条线训练每轮再 +25%", textEn = "Both lines gain another +25% per round", apply = f => f.gain += .25 },
            new XgRelicPair { a = "allnight", b = "redbull", zh = "通宵赶工", en = "Crunch night",
                textZh = "连击加成的上限从 ×2 提到 ×3", textEn = "The combo bonus cap rises from ×2 to ×3", apply = f => f.comboCapBonus += 1 },
            new XgRelicPair { a = "mining_1080", b = "multimeter", zh = "矿工的自我修养", en = "A miner's craft",
                textZh = "矿卡的电费惩罚取消，显存再 +0.5 倍", textEn = "The mining card's power penalty is gone and VRAM gains another +0.5", apply = f => { f.powerRise = Math.Max(0, f.powerRise - .30); f.vramBonus += .5; } },
            new XgRelicPair { a = "dropout_paper", b = "batchnorm_paper", zh = "正则化套餐", en = "Regularisation combo",
                textZh = "退步概率再 −25%（合计 −75%）", textEn = "Chance of a drop another −25% (−75% in all)", apply = f => f.dropCut += .25 },
            new XgRelicPair { a = "zhou_usb", b = "data_broker", zh = "数据双保险", en = "Double data cover",
                textZh = "手标样本再多记 1 条", textEn = "One more sample per hand label", apply = f => f.extraSamples += 1 },
            new XgRelicPair { a = "tieba", b = "advisor_page", zh = "引经据典", en = "Citing the classics",
                textZh = "能力门槛再 ×0.95", textEn = "Ability thresholds another ×0.95", apply = f => { f.paramsFactor *= .95; f.samplesFactor *= .95; } },
        };

        static Dictionary<string, XgRelic> byId;

        public static XgRelic Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (byId == null)
            {
                var map = new Dictionary<string, XgRelic>();
                foreach (var r in All) map[r.id] = r;
                byId = map;
            }
            return byId.TryGetValue(id, out var found) ? found : null;
        }

        public static string Name(XgRelic r, bool english) => english ? r.en : r.zh;
        public static string Flavor(XgRelic r, bool english) => english ? r.flavorEn : r.flavorZh;
        public static string Effect(XgRelic r, bool english) => english ? r.effectEn : r.effectZh;

        /// <summary>Where this relic can come from, in one line.</summary>
        public static string Source(XgRelic r, bool english)
        {
            if (r.rarity == XgRarity.Legendary)
                return english ? "Only as one of three choices when 灵光 learns something new" : "只在灵光学会新能力时，三选一里出现";
            return english ? "One of three choices when 灵光 learns something new; a rare drop from mini-games, the spider and new records"
                : "灵光学会新能力时三选一；也可能从小游戏、蜘蛛和新纪录里掉出来";
        }

        /// <summary>The pair this relic is part of with another (null when it has none).</summary>
        public static XgRelicPair PairOf(string id)
        {
            foreach (var p in Pairs) if (p.a == id || p.b == id) return p;
            return null;
        }

        /// <summary>The partner's id in the pair of <paramref name="id"/>.</summary>
        public static string Partner(XgRelicPair p, string id) => p.a == id ? p.b : p.a;

        /// <summary>Sums the effects of the given relic ids (empty or unknown entries are skipped) and of every pair fully inside them.</summary>
        public static XgRelicFx Build(IList<string> ids)
        {
            var fx = new XgRelicFx();
            if (ids == null) return fx;
            var seen = new HashSet<string>();
            foreach (var id in ids)
            {
                var r = Get(id);
                if (r == null || !seen.Add(id)) continue;
                r.apply(fx);
            }
            foreach (var p in Pairs) if (seen.Contains(p.a) && seen.Contains(p.b)) p.apply(fx);
            return fx;
        }
    }
}
