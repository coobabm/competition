using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>The house machine as the 借卡 scene shows it, read by the desktop from ChapterOneSim (tests make their own).</summary>
    public sealed class XgRigFacts
    {
        public List<XgCardSpec> cards = new List<XgCardSpec>();
        public int cases = 1, slotsPerCase = 2;
        /// <summary>Case watts (each), the house circuit's limit, ¥ per kWh.</summary>
        public double caseWatts = 45, limitWatts = 3500, kwhPrice = .52;
        public int Slots => cases * slotsPerCase;
        public double CardWatts { get { double w = 0; foreach (var c in cards) w += c.watts; return w; } }
        public double LoadWatts => CardWatts + cases * caseWatts;
        public double Compute { get { double s = 0; foreach (var c in cards) s += c.compute; return s; } }
        public double LargestVramMB { get { double m = 0; foreach (var c in cards) m = Math.Max(m, c.vramMB); return m; } }
    }

    /// <summary>One page of the look inside the case: the part it lights up ("card", "vram", "power", "case").</summary>
    public sealed class XgRigPage
    {
        public string part = "", titleZh = "", titleEn = "", zh = "", en = "";
    }

    public sealed partial class XgState
    {
        /// <summary>
        /// 借你一张卡 (XgSim.CardLoan.cs): 0 not yet, 1 老周 asked (the case is open), 2 the case looked at (his answer is
        /// due), 3 done. <see cref="clLent"/>: 0, 1 lent, 2 said no.
        /// </summary>
        public int clStep, clLent;
    }

    /// <summary>
    /// 借你一张卡, the end of stage 3 (design 2026-10-18). When the 「短句」 exam opens, 老周 writes in YY: the card in his
    /// group's server burned out, could he borrow one for two days. Before answering, the protagonist opens the side of
    /// the case and looks at what the machine runs on: four short pages with the save's own numbers (算力 = the cards,
    /// 显存 = what fits on one card, 电 = what the house circuit and the meter take, 机箱 = the slots). Then the answer,
    /// as reply buttons to 老周: lend (the newest card leaves for a while; with one card the training stops until it is
    /// back) or no. Lending is the first time of the 「zhou_card」 life event (its later asks say 「又得麻烦你了」). Like the
    /// other chapter scenes it holds the exam only with the desktop (<see cref="DesktopHosted"/>); the bot never plays it.
    /// </summary>
    public sealed partial class XgSim
    {
        public const string CardLoanChronicle = "cardloan";
        /// <summary>老周's YY conversation, where he asks.</summary>
        public const string CardLoanConversation = "laozhou";
        /// <summary>How long the lent card is away (lab seconds): 「两天」 in the story.</summary>
        public const double CardLoanSeconds = 300;

        /// <summary>The 「短句」 exam is open (or a pass is waiting) and 老周 has not asked yet.</summary>
        public bool CardLoanDue => S.stage == 3 && S.clStep == 0 && (ExamReady || ExamPassPending);

        /// <summary>With the desktop: the chapter waits until 老周 has his answer.</summary>
        public bool CardLoanContactAvailable; // Host capability only; never character knowledge or a saved acceptance.
        public bool CardLoanHolds => DesktopHosted && CardLoanContactAvailable && S.stage == 3 && NextAbility == 4 && S.clStep < 3;

        public bool CardLoanActive => S.clStep >= 1 && S.clStep < 3;

        /// <summary>老周's ask, bubble by bubble (zh, en pairs). Starts the scene.</summary>
        public List<string[]> CardLoanStart(int cards)
        {
            var b = new List<string[]>();
            if (!CardLoanDue && S.clStep != 1) return b;
            S.clStep = 1;
            b.Add(new[] { "哥们，在不？", "Hey man, you there?" });
            b.Add(new[] { "组里服务器那张卡烧了，导师周一要结果，我实验跑一半卡住了。", "The card in our group's server burned out. My advisor wants results on Monday and my run is stuck halfway." });
            b.Add(new[] { "你手边有空闲显卡吗？能借我两天不？用完原样还你。", "Have a spare graphics card? Could I borrow it for two days? I will return it as it is." });
            return b;
        }

        /// <summary>The look inside the case, with the save's numbers.</summary>
        public static List<XgRigPage> CardLoanTour(XgRigFacts f)
        {
            var pages = new List<XgRigPage>();
            int n = f.cards.Count;
            var top = n > 0 ? f.cards[n - 1] : new XgCardSpec { name = "GTX 980 Ti", nameEn = "GTX 980 Ti" };
            string x = f.Compute.ToString("0.##");
            pages.Add(new XgRigPage
            {
                part = "card", titleZh = "显卡 · 算力", titleEn = "The card · compute",
                zh = (n > 1 ? n + " 张显卡插在主板上。" : "主板上插着一张 " + top.name + "。") + "它学东西的每一轮训练，几百万次乘法加法，都在显卡上算。\n" +
                     "算力 ×" + x + "（一张 980 Ti 算 ×1）。卡越快，进度条走得越快" + (n > 1 ? "；拔掉一张，就慢一截。" : "；卡拔了，训练就停。"),
                en = (n > 1 ? n + " cards sit on the board. " : "One " + top.nameEn + " sits on the board. ") + "Every round it learns, millions of multiply-adds, runs on the card.\n" +
                     "Compute ×" + x + " (one 980 Ti is ×1). A faster card moves the bar faster" + (n > 1 ? "; pull one and it slows down." : "; pull the card and training stops."),
            });
            string g = (f.LargestVramMB / 1024).ToString("0.#");
            pages.Add(new XgRigPage
            {
                part = "vram", titleZh = "显存", titleEn = "Video memory",
                zh = "卡上那几颗黑色的芯片是显存：" + g + "G。训练时，整个模型连同一批数据得一起装进一张卡里。\n卡多了能算得快，可一张卡装不下的模型，再加卡也装不下：得换显存更大的卡。",
                en = "The black chips on the card are its memory: " + g + " GB. While training, the whole model and a batch of data must fit on one card.\nMore cards compute faster, but a model too big for one card stays too big: that needs a card with more memory.",
            });
            string load = f.LoadWatts.ToString("0"), cw = f.CardWatts.ToString("0"), kw = (f.cases * f.caseWatts).ToString("0"), lim = f.limitWatts.ToString("0");
            double hourYuan = f.CardWatts / 1000 * f.kwhPrice;
            pages.Add(new XgRigPage
            {
                part = "power", titleZh = "电", titleEn = "Power",
                zh = "满载 " + load + "W：显卡 " + cw + "W，机箱自己 " + kw + "W。家里这条线最多带 " + lim + "W，超了就跳闸。\n" +
                     "电费一度 ¥" + f.kwhPrice.ToString("0.00") + "，显卡满载跑一小时 ¥" + hourYuan.ToString("0.00") + "。一个月用得多了，阶梯电价还要往上涨。",
                en = "Full load " + load + " W: cards " + cw + " W, the case itself " + kw + " W. The house line carries " + lim + " W at most; past that the breaker trips.\n" +
                     "Power is ¥" + f.kwhPrice.ToString("0.00") + " a kWh: an hour of the cards at full load is ¥" + hourYuan.ToString("0.00") + ". Use a lot in a month and the tiered price goes up.",
            });
            int free = Math.Max(0, f.Slots - n);
            pages.Add(new XgRigPage
            {
                part = "case", titleZh = "机箱 · 插槽", titleEn = "The case · slots",
                zh = (f.cases > 1 ? f.cases + " 台机箱，" : "一个机箱，") + f.Slots + " 个显卡插槽，插着 " + n + " 张" + (free > 0 ? "，空着 " + free + " 个。" : "，插满了。") +
                     (free > 0 ? "\n插满以后再想加卡" : "\n再想加卡") + "，就得再买一台主机：多一台机箱，多一份电，也多占一块桌子。",
                en = (f.cases > 1 ? f.cases + " cases, " : "One case, ") + f.Slots + " card slots, " + n + " in use" + (free > 0 ? ", " + free + " free." : ", all full.") +
                     (free > 0 ? "\nOnce they're full, another card" : "\nAnother card") + " needs another machine: one more case, more power, more desk.",
            });
            return pages;
        }

        /// <summary>What the protagonist thinks closing the case.</summary>
        public static string[] CardLoanThought(int cards) => cards > 1
            ? new[] { "借一张，它慢一截。", "Lend one and it slows down." }
            : new[] { "就这一张。借出去，它就停。", "There's just this one. Lend it and it stops." };

        /// <summary>The case was looked at: the answer is due.</summary>
        public bool CardLoanTourDone()
        {
            if (S.clStep != 1) return false;
            S.clStep = 2;
            return true;
        }

        /// <summary>The two answers to 老周 (zh, en).</summary>
        public static string[][] CardLoanAnswers(int cards) => new[]
        {
            new[] { "拿去吧，用完还我", "Take it, bring it back after" },
            cards > 1 ? new[] { "最近离不了，对不住", "Can't spare one right now, sorry" } : new[] { "就这一张，离不了", "It's my only one, I can't" },
        };

        /// <summary>
        /// The answer. Lend: the newest card is unplugged for <see cref="CardLoanSeconds"/> (put back with 老周's note, as
        /// the life event's loan is), and he owes a favour. No: he takes it well, a beat slow. 老周's reply bubbles.
        /// </summary>
        public List<string[]> CardLoanDecide(bool lend, IXgHost host)
        {
            var b = new List<string[]>();
            if (S.clStep != 2) return b;
            EnsureLife();
            S.clStep = 3;
            S.clLent = lend ? 1 : 2;
            NoteChronicle(CardLoanChronicle);
            if (lend)
            {
                var cards = (host as IXgRig)?.Cards;
                if (cards != null && cards.Count > 0)
                {
                    int last = cards.Count - 1, k = 0;
                    for (int i = 0; i < last; i++) if (cards[i] != null && cards[last] != null && cards[i].id == cards[last].id) k++;
                    var spec = cards[last];
                    // Unplug names the hold from the life event's arguments: borrow them, then put back whatever an
                    // open event had there.
                    string key = S.lifeArgKey, name = S.lifeArgName, nameEn = S.lifeArgNameEn;
                    S.lifeArgName = spec.name ?? "";
                    S.lifeArgNameEn = string.IsNullOrEmpty(spec.nameEn) ? S.lifeArgName : spec.nameEn;
                    Unplug("lend", CardKey(spec.id, k), CardLoanSeconds, host);
                    S.lifeArgKey = key; S.lifeArgName = name; S.lifeArgNameEn = nameEn;
                }
                S.lifeFavour = 1;
                NoteChronicle("laozhou.card");
                SetCooldown("zhou_card", 1500);
                b.Add(new[] { "靠谱！约在电脑城门口交接吧。", "Thanks! Let's meet outside the computer market." });
                b.Add(new[] { "回头学校机房有啥大文件，我拿 U 盘给你拷。", "Any big file you need, I'll copy it for you on the campus network." });
            }
            else
            {
                S.lifeZhouCool = Math.Min(5, S.lifeZhouCool + 1);
                SetCooldown("zhou_card", 1500 * (1 + S.lifeZhouCool));
                b.Add(new[] { "行，没事，我再问问别人。", "Sure, no problem, I will ask around." });
            }
            return b;
        }

        /// <summary>For 《灵光记》, when the card stayed (a loan is told by 「laozhou.card」).</summary>
        string CardLoanChronicleBody(bool english, string self, string you)
            => english
                ? "Lao Zhou's lab card burned out and he asked to borrow one. Before answering, you opened the side of the case and looked at what I run on: the card, its memory, the watts, the slots. Then you said no. He said it was fine, a beat slower than usual."
                : "老周实验室的卡烧了，来借卡。" + you + "回他之前，先拆开机箱侧板，看了看" + self + "靠什么跑：显卡、显存、几百瓦的电、几个插槽。然后" + you + "说了不借。他说没事，回得比平时慢了半拍。";
    }
}
