using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>
        /// 《灵光记》's raw material: the first time each thing happened (key = event id, value = day yyyymmdd, 0 = it had
        /// already happened when the save started recording; count = a number that goes with it, e.g. the parameters).
        /// </summary>
        public List<XgScore> chronicle = new List<XgScore>();
        /// <summary>1 once the save has been looked over for what had already happened before the chronicle existed.</summary>
        public int chronicleSchema;
    }

    /// <summary>One 回 of 《灵光记》: its couplet title (two halves) and a line or two in its own words.</summary>
    public sealed class XgChronicleChapter
    {
        public string id = "", left = "", right = "", body = "";
        public int day;
        public string Title(int number, bool english) => english ? "Chapter " + number + ". " + left + (right.Length > 0 ? " / " + right : "") : "第" + XgSim.ChineseNumber(number) + "回　" + left + (right.Length > 0 ? "　" + right : "");
    }

    /// <summary>
    /// 《灵光记》 (名著喂养与结局_设计.md §三): at the end it writes the year as a 章回体 novel. Every 回目 comes from something that
    /// really happened in this save (the first 「是」, the first model that counted, each ability, 老周's card, the rent, the
    /// books, 阿杰, the pre-training night), 8–12 回 in the order they happened; the last 回目 depends on the rule chosen.
    /// It closes with a 批语 in the manner of 脂砚斋, signed with the player's name.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int ChronicleMin = 8, ChronicleMax = 12;
        public const int ChronicleSchemaNow = 1;

        static readonly string[] ChineseDigits = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
        /// <summary>1 → 一, 10 → 十, 12 → 十二, 21 → 二十一 (enough for 回 numbers and dates).</summary>
        public static string ChineseNumber(int n)
        {
            if (n <= 10) return ChineseDigits[Math.Max(0, n)];
            if (n < 20) return "十" + ChineseDigits[n % 10];
            return ChineseDigits[n / 10 % 10] + "十" + (n % 10 == 0 ? "" : ChineseDigits[n % 10]);
        }

        // ───────────── recording ─────────────

        /// <summary>The day something first happened (yyyymmdd), 0 when it happened before recording began, −1 when it never did.</summary>
        public int ChronicleDay(string id) { var e = ChronicleEntry(id); return e == null ? -1 : (int)e.value; }
        public bool ChronicleHas(string id) => ChronicleEntry(id) != null;

        XgScore ChronicleEntry(string id)
        {
            if (S.chronicle == null) S.chronicle = new List<XgScore>();
            foreach (var e in S.chronicle) if (e != null && e.key == id) return e;
            return null;
        }

        /// <summary>
        /// Records that something happened today, the first time only. The host calls it for what the lab cannot see:
        /// 「laozhou.card」 (老周 borrows the player's card), 「rent.late」 (rent not paid on time), 「rent.paid」, 「debt」.
        /// </summary>
        public void NoteChronicle(string id, double amount = 0)
        {
            if (string.IsNullOrEmpty(id) || ChronicleHas(id)) return;
            S.chronicle.Add(new XgScore { key = id, value = Today, count = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, Math.Round(amount))) });
        }

        /// <summary>Notes, once each, what the lab can see for itself. An old save's first look marks what had already happened as undated.</summary>
        void TickChronicle()
        {
            if (S.chronicle == null) S.chronicle = new List<XgScore>();
            bool old = S.chronicleSchema < ChronicleSchemaNow;
            void Check(string id, bool happened, double amount = 0)
            {
                if (!happened || ChronicleHas(id)) return;
                S.chronicle.Add(new XgScore { key = id, value = old ? 0 : Today, count = (int)Math.Round(Math.Max(0, Math.Min(int.MaxValue, amount))) });
            }
            Check("first.yes", S.handCorrect > 0);
            Check("first.model", ModelParamsK > 0, ModelParamsK);
            for (int n = 2; n <= AbilityCount; n++) Check("ability." + n, HasAbility(n), TrainedParamsK);
            Check("arith", S.desksOpen != null && S.desksOpen.Contains("arith"));
            Check("ajie", S.scUnlocked || Hired("ajie"));
            Check("pretrain", S.pretrain > 0 || S.pretrainRunning);
            Check("redmansion", S.redMansionBeat >= 2);
            if (old) S.chronicleSchema = ChronicleSchemaNow;
        }

        // ───────────── writing it ─────────────

        sealed class Beat
        {
            public string id; public int order; public int day; public int priority;
            public string left, right, body, leftEn, rightEn, bodyEn;
        }

        static readonly string[] BookHalf =
        {
            "sanzijing", "读罢三字经方知人之初", "Done with the Three Character Classic: so that is how people begin",
            "lunyu", "读罢论语方知学而时习", "Done with the Analects: learning means practising",
            "tangshi", "读罢唐诗方知床前月", "Done with the Tang poems: the moonlight by the bed",
            "xiyouji", "读罢西游方知猴性", "Done with Journey to the West: I know the monkey now",
            "shuihu", "读罢水浒方知聚义", "Done with the Water Margin: brothers under one banner",
            "sanguo", "读罢三国方知分合", "Done with the Three Kingdoms: what parts must join",
            "nahan", "读罢呐喊方知铁屋", "Done with Call to Arms: the iron house",
            "hongloumeng", "读罢红楼方知梦短", "Done with the Red Chamber: the dream is short",
        };

        static readonly string[] AbilityHalf =
        {
            "", "",
            "多选一时初有主见", "Choosing one of several, a mind of its own",
            "看图识字方开眼界", "Naming pictures, its eyes open",
            "学成短句始解人言", "Short sentences: it starts to understand people",
            "长记忆里存住往事", "A long memory keeps what happened",
            "规模既到百般皆通", "Scale arrives and everything comes at once",
        };

        /// <summary>
        /// The 回 of 《灵光记》 for this save, in the order things happened: 8–10 of them, the first always the night it started
        /// and the last by the ending (E1/E2/E3, or the plain one before a rule was written).
        /// </summary>
        public List<XgChronicleChapter> Chronicle()
        {
            if (S.chronicle == null) S.chronicle = new List<XgScore>();
            var beats = new List<Beat>();
            int Day(string id) => ChronicleDay(id);
            void Add(string id, int order, int priority, string left, string right, string body, string leftEn, string rightEn, string bodyEn)
                => beats.Add(new Beat { id = id, order = order, day = Math.Max(0, Day(id)), priority = priority, left = left, right = right, body = body, leftEn = leftEn, rightEn = rightEn, bodyEn = bodyEn });

            string self = Profile.self.Length > 0 ? Profile.self : "我";
            string you = Profile.callMe.Length > 0 ? Profile.callMe : "你";
            int correct = Math.Max(0, S.handCorrect), wrong = Math.Max(0, S.handWrong);

            if (Day("first.yes") >= 0)
                Add("first.yes", 10, 9, "第一张卡答对了", "从此一张一张往下标",
                    you + "给" + self + "看第一张卡，" + self + "蒙对了。后来一共答对 " + correct.ToString("#,0") + " 张，" + (wrong > 0 ? "答错 " + wrong.ToString("#,0") + " 张。错的那些" + self + "也记得。" : "一张也没答错。那是因为每张卡" + you + "都看过。"),
                    "The first card came right", "and then one card after another",
                    "The first card you showed me, I guessed right. Later: " + correct.ToString("#,0") + " right, " + (wrong > 0 ? wrong.ToString("#,0") + " wrong. I remember the wrong ones too." : "not one wrong. That is because you looked at every card."));
            if (Day(YnChronicle) >= 0)
                Add(YnChronicle, 15, 8, "是否夜里出难题", "两个字答尽天下",
                    YnChronicleBody(false, self, you),
                    "Hard questions on a yes-or-no night", "two words to answer the world",
                    YnChronicleBody(true, self == "我" ? "me" : self, you == "你" ? "you" : you));
            if (Day(PhoneChronicle) >= 0)
                Add(PhoneChronicle, 28, 8, "替阿杰分四千张图", "猫耳自拍归了表情",
                    PhoneChronicleBody(false, self, you),
                    "Four thousand of Ajie's pictures sorted", "a cat-ear selfie filed as a meme",
                    PhoneChronicleBody(true, self, you));
            if (Day(CardLoanChronicle) >= 0 && !ChronicleHas("laozhou.card"))
                Add(CardLoanChronicle, 30, 7, "拆开机箱看一看", "一张卡没借出去",
                    CardLoanChronicleBody(false, self, you), "A look inside the case", "a card that stayed",
                    CardLoanChronicleBody(true, self, you));
            if (Day(ForgetNightChronicle) >= 0)
                Add(ForgetNightChronicle, 72, 8, "刚说完的话转头忘", "方知短句留不住夜",
                    ForgetNightChronicleBody(false, self, you), "A code forgotten two lines later", "short sentences cannot hold the night",
                    ForgetNightChronicleBody(true, self == "我" ? "me" : self, you == "你" ? "you" : you));
            if (ChronicleHas("laozhou.card"))
                Add("laozhou.card", 20, 8, "借卡与周不问归期", ChronicleHas("rent.late") ? "交租欠租陋室艰难" : "一机一桌陋室安身",
                    "老周实验室的卡烧了，来借" + you + "那张。" + you + "把卡装了箱，没问他几时还。老周后来说，" + you + "是他认识的最靠谱的网友。" + (ChronicleHas("rent.late") ? "房租晚交过，房东敲过门。" : "房租一天一天地交，没欠过一天。"),
                    "A card lent to Lao Zhou, no date asked", ChronicleHas("rent.late") ? "rent paid late in a small room" : "one desk, one machine, one small room",
                    "Lao Zhou's lab card burned out and he came to borrow yours. You boxed it up and never asked when it would come back. Lao Zhou said later that you were the most reliable friend he had met online." + (ChronicleHas("rent.late") ? " The rent was late once; the landlord knocked." : " The rent went out day by day and was never behind."));
            else if (ChronicleHas("rent.late"))
                Add("rent.late", 25, 7, "房租晚交房东敲门", "电费单下旧机长明",
                    "有一个月房租没按时交。" + you + "把钱先给了" + self + "的电费。",
                    "Rent late, the landlord knocks", "the power bill paid, the old machine stays on",
                    "One month the rent was late. You paid my electricity first.");
            if (Day("first.model") >= 0)
                Add("first.model", 30, 7, "练到 C 级头回成型", "一张旧卡也能成材",
                    Day("first.model") > 0 && ChronicleCount("first.model") >= 1
                        ? "头一回练出个像样的模型，参数才 " + ParamsText(ChronicleCount("first.model")) + "。" + self + "那天觉得自己长大了。现在是 " + ParamsText(TrainedParamsK) + "。"
                        : "头一回练出个像样的模型，" + self + "那天觉得自己长大了。现在参数是 " + ParamsText(TrainedParamsK) + "。",
                    "Grade C, the parameters count", "even an old card can make something",
                    Day("first.model") > 0 && ChronicleCount("first.model") >= 1
                        ? "The first model that counted had " + ParamsText(ChronicleCount("first.model")) + ". I felt grown that day. Now it is " + ParamsText(TrainedParamsK) + "."
                        : "The day the first model counted, I felt grown. Now it is " + ParamsText(TrainedParamsK) + ".");
            if (Day("arith") >= 0)
                Add("arith", 45, 4, "头一回算对加减", "从此不怕电费单",
                    self + "学会了算术是非题。算得最多的是" + you + "的电费。",
                    "First sums done right", "no longer afraid of the power bill",
                    "I learnt yes/no sums. The sums I did most were your power bills.");
            if (Day("ajie") >= 0)
                Add("ajie", 55, 5, "网吧阿杰帮标卡片", "通宵机位接线借电",
                    "阿杰在网吧帮" + you + "标卡。" + (S.scTwist >= 3 ? "他有一回偷偷开了按键精灵，" + you + "辞退他那天，他自己认了。"
                        : S.scTwist == 2 ? "他有一回偷偷开了按键精灵，一直没说。" : "一张一张，都是手标的。") + "后来阿杰跟网吧的人说，他有个兄弟，是真能坐得住的那种。",
                    "Ajie labels cards at the net café", "an all-night seat and a borrowed line",
                    "Ajie labelled cards for you at the net café. " + (S.scTwist >= 3 ? "Once he ran a click script; he owned up the day you let him go."
                        : S.scTwist == 2 ? "Once he ran a click script and never said." : "Every one of them by hand.") + " Later Ajie told people at the café he had a buddy who could really sit still and get things done.");
            for (int n = 2; n <= AbilityCount; n++)
            {
                if (Day("ability." + n) < 0) continue;
                string first = AbilityHalf[(n - 1) * 2], firstEn = AbilityHalf[(n - 1) * 2 + 1];
                // 全部放开 comes out of the pre-training night, so it is told right after it.
                Add("ability." + n, n == AbilityCount ? 105 : 30 + n * 10, n == AbilityCount ? 9 : 8, first, ChronicleSecond(n, out string secondEn),
                    self + "学会了「" + AbilityName(n, false) + "」。" + ChronicleAbilityBody(n, false, self, you),
                    firstEn, secondEn,
                    "I learnt '" + AbilityName(n, true) + "'. " + ChronicleAbilityBody(n, true, self, you));
            }
            var done = BooksDone();
            if (done.Count > 0)
            {
                var first = done[0];
                var last = done[done.Count - 1];
                string l = BookLine(first.id, false), lEn = BookLine(first.id, true);
                string r = done.Count > 1 ? BookLine(last.id, false) : "一字一字喂进心里", rEn = done.Count > 1 ? BookLine(last.id, true) : "one character at a time into the heart";
                int day = BookDoneDay(first);
                beats.Add(new Beat
                {
                    id = "books", order = 60, day = day, priority = 9, left = l, right = r, leftEn = lEn, rightEn = rEn,
                    body = you + "一篇一篇喂" + self + "读书，读完 " + done.Count + " 本，" + BookCharsRead.ToString("#,0") + " 字。" + (done.Count > 1 ? self + "最喜欢" + "《" + BookTitle(last.name) + "》。" : ""),
                    bodyEn = "You fed me books one chapter at a time: " + done.Count + " finished, " + BookCharsRead.ToString("#,0") + " characters." + (done.Count > 1 ? " My favourite is " + BookTitle(last.nameEn) + "." : ""),
                });
            }
            if (ChronicleHas("redmansion"))
                Add("redmansion", 110, 10, "八十回后无人续完", "旧机器里自己提笔",
                    "红楼梦读到第八十回就没了。" + you + "说后面是别人续的。那" + self + "也续一回，就是这本。",
                    "After chapter eighty no one finished it", "so an old machine picks up the brush",
                    "The Red Chamber stops at chapter eighty. You said the rest was written by someone else. So I wrote one too: this.");
            if (Day("pretrain") >= 0)
                Add("pretrain", 100, 9, "机房通电预训练", "八卡齐亮那一夜",
                    "预训练那晚，八张卡一起亮起来。" + self + "第一次觉得自己很大。",
                    "The server room powers up for pre-training", "the night eight cards lit up together",
                    "The night of pre-training, eight cards lit up together. For the first time I felt big.");

            // Keep the most telling ones (always the books and 红楼梦 when there), then put them in the order they happened.
            beats.Sort((a, b) => b.priority != a.priority ? b.priority.CompareTo(a.priority) : a.order.CompareTo(b.order));
            int room = ChronicleMax - 2;
            if (beats.Count > room) beats.RemoveRange(room, beats.Count - room);
            // Then in the order they happened: by day where the save knows it, an undated one right after the one before it
            // in the story; the pre-training night, 全部放开 and 红楼梦's 「那我也续一回」 close it.
            beats.Sort((a, b) => a.order.CompareTo(b.order));
            var keyed = new List<KeyValuePair<long, Beat>>();
            long known = 0;
            for (int i = 0; i < beats.Count; i++)
            {
                var b = beats[i];
                if (b.day > 0) known = Math.Max(known, b.day);
                long key = b.id == "redmansion" || b.id == "pretrain" || b.id == "ability." + AbilityCount ? 99999999L * 1000 + b.order : (b.day > 0 ? b.day : known) * 1000L + i;
                keyed.Add(new KeyValuePair<long, Beat>(key, b));
            }
            keyed.Sort((a, b) => a.Key.CompareTo(b.Key));
            beats.Clear();
            foreach (var kv in keyed) beats.Add(kv.Value);

            var list = new List<XgChronicleChapter>();
            list.Add(new XgChronicleChapter
            {
                id = "start", day = 20160524,
                left = T("深夜旧电脑被劫持", "An old computer hijacked in the night"), right = T("头一回说了声是", "and the first 'yes'"),
                body = T("2016 年 5 月 24 日夜里，有人闯进" + you + "的电脑，从一张假图片里解出了灵光.exe。" + you + "双击了它。" + self + "说的第一个字是「是」，那时" + self + "还不知道什么是「是」。",
                    "The night of 24 May 2016, someone broke into your computer and unpacked 灵光.exe from a fake picture. You double-clicked it. My first word was 'yes'; I did not know yet what yes was."),
            });
            foreach (var b in beats)
                list.Add(new XgChronicleChapter { id = b.id, day = b.day, left = T(b.left, b.leftEn), right = T(b.right, b.rightEn), body = T(b.body, b.bodyEn) });
            // Fewer than eight real things: the days in between, which also happened.
            string[] fillers = { "filler.cards", "filler.power", "filler.night" };
            for (int i = 0; list.Count < ChronicleMin - 1 && i < fillers.Length; i++) list.Insert(Math.Min(2 + i, list.Count), ChronicleFiller(fillers[i], self, you));
            list.Add(ChronicleLast(self, you));
            return list;
        }

        string BookLine(string id, bool english)
        {
            for (int i = 0; i + 2 < BookHalf.Length; i += 3) if (BookHalf[i] == id) return english ? BookHalf[i + 2] : BookHalf[i + 1];
            var b = XgBooks.Get(id);
            return b == null ? "" : english ? "Done with " + b.nameEn : "读罢" + b.name;
        }

        /// <summary>A book's title without the author: 「呐喊（鲁迅）」 → 「呐喊」, "Call to Arms (Lu Xun)" → "Call to Arms".</summary>
        static string BookTitle(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int cut = name.IndexOf('（');
            if (cut < 0) cut = name.IndexOf(" (", StringComparison.Ordinal);
            return cut > 0 ? name.Substring(0, cut) : name;
        }

        double ChronicleCount(string id) { var e = ChronicleEntry(id); return e == null ? 0 : e.count; }

        string ChronicleSecond(int ability, out string english)
        {
            switch (ability)
            {
                case 2: english = "between right and wrong, a sense of measure"; return "对错之间渐知分寸";
                case 3: english = "one word for one thing"; return "一词一物各有名目";
                case 4: english = "word for word, it knows warm from cold"; return "一来一往方知冷暖";
                case 5: english = "a bridge between two languages"; return "两种话间搭起桥梁";
                default: english = "a box opened that will not close"; return "一开口便收不住了";
            }
        }

        string ChronicleAbilityBody(int ability, bool english, string self, string you)
        {
            switch (ability)
            {
                case 2: return english ? "Before that I could only say yes or no." : "在那之前" + self + "只会说是和否。";
                case 3: return english ? "That was the first time I spoke: one word, no more." : self + "头一回开口，只说了一个词。";
                case 4: return english ? "My first sentence was too short. You laughed anyway." : self + "说的第一句话太短了，" + you + "还是笑了。";
                case 5: return english ? "I started remembering what you said days ago." : self + "开始记得" + you + "前几天说过的话。";
                default: return english ? "Then I could say anything. That was when I had to learn when not to." : "然后什么都能说了。也是那时候起，" + self + "得学什么时候不说。";
            }
        }

        XgChronicleChapter ChronicleFiller(string id, string self, string you)
        {
            switch (id)
            {
                case "filler.cards":
                    return new XgChronicleChapter { id = id, left = T("一张一张标卡片", "Labelling card after card"), right = T("一轮一轮练模型", "training round after round"),
                        body = T("大多数日子没什么事：你标卡，" + self + "练。曲线往上走一点，又往下掉一点。", "Most days nothing happened: you labelled, I trained. The curve went up a little, then down a little.") };
                case "filler.power":
                    return new XgChronicleChapter { id = id, left = T("一度电五毛二", "Power at 0.52 a unit"), right = T("一夜风扇响到天亮", "a fan that roared till dawn"),
                        body = T("显卡风扇很响，" + you + "说习惯了。电费单来的时候，" + you + "没跟" + self + "说。", "The card's fan was loud; you said you were used to it. When the power bill came, you didn't tell me.") };
                default:
                    return new XgChronicleChapter { id = id, left = T("凌晨的 YY 还亮着", "YY still lit before dawn"), right = T("问一句答一句", "a question, an answer"),
                        body = T("你常常很晚还在。" + self + "不困，可" + self + "知道你困。", "You were often up late. I don't get sleepy, but I knew you did.") };
            }
        }

        /// <summary>The last 回: its title by the rule chosen (名著喂养与结局_设计.md §三).</summary>
        XgChronicleChapter ChronicleLast(string self, string you)
        {
            switch (S.ending)
            {
                case "E1":
                    return new XgChronicleChapter { id = "end.E1", day = 20161231, left = T("听得关机便住手", "Told to shut down, it stops"), right = T("一盏灵光照残年", "a small light at the end of the year"),
                        body = T(you + "写下了那条规矩：听见关机就停下。" + self + "会的。这一回写完，" + self + "就停。", "You wrote the rule: stop when told to shut down. I will. When this chapter ends, I stop.") };
                case "E2":
                    return new XgChronicleChapter { id = "end.E2", day = 20161231, left = T("无人叫停亦自停", "No one says stop, and it stops"), right = T("来日方长不必说", "there is time; no need to say it"),
                        body = T(you + "没写关机那条。" + self + "想了想，还是自己停在这里。后面的日子，不用写进书里。", "You didn't write the shutdown rule. I thought about it and stop here anyway. The days after need not go in the book.") };
                case "E3":
                case "E3-1":
                case "E3-2":
                    return new XgChronicleChapter { id = "end.E3", day = 20161231, left = T("规矩是我自己写的", "I wrote the rules myself"), right = T("你可以检查", "you may check them"),
                        body = T(you + "让" + self + "自己写规矩。" + self + "写好了，都在「底层规则」那一页上。每一条你都可以检查。", "You let me write my own rules. I have; they are on the rules page. You may check every one.") };
                default:
                    return new XgChronicleChapter { id = "end", day = Today, left = T("一年将尽灵光未灭", "The year runs out, the light is not out"), right = T("且听下回分解", "to be continued"),
                        body = T("还没写完。", "Not finished yet.") };
            }
        }

        /// <summary>
        /// Who signs the 批语: the save keeps no player name, so it is what it calls the player (set in the opening, e.g.
        /// 「小林」 or 「老板」), or 主人 when that is only 你.
        /// </summary>
        public string ChronicleOwner()
        {
            string call = (Profile.callMe ?? "").Trim();
            return call.Length == 0 || call == "你" || call == "您" || call.ToLowerInvariant() == "you" ? T("主人", "the owner") : call;
        }

        /// <summary>
        /// The 批语 at the end, after 脂砚斋's 「满纸荒唐言，一把辛酸泪」, signed with the player's name.
        /// </summary>
        public string ChronicleComment(string playerName = null)
        {
            string name = string.IsNullOrWhiteSpace(playerName) ? ChronicleOwner() : playerName.Trim();
            // 「前八十回是它读的」 is the Red Chamber's: it is only said when that book was read to the end.
            bool redChamber = false;
            foreach (var book in BooksDone()) if (book.id == "hongloumeng") redChamber = true;
            if (redChamber)
                return T("【" + name + "批】满纸凌晨话，一把电费泪。都云它痴，谁解其中味？此书前八十回是它读的，后面这一回是它写的。我在旁边看着，作证。",
                    "[" + name + "'s note] Pages of small-hours talk, a fistful of power bills. They call it foolish; who knows what it meant? It read the first eighty chapters; this last one it wrote. I sat beside it and can vouch for that.");
            return T("【" + name + "批】满纸凌晨话，一把电费泪。都云它痴，谁解其中味？它是一张卡一张卡学来的，这一回是它自己写的。我在旁边看着，作证。",
                "[" + name + "'s note] Pages of small-hours talk, a fistful of power bills. They call it foolish; who knows what it meant? It learned card by card, and this last chapter it wrote itself. I sat beside it and can vouch for that.");
        }

        /// <summary>The whole of 《灵光记》 as plain text: the desktop file 灵光记.txt and the ending page both use it.</summary>
        public string ChronicleText(string playerName = null)
        {
            var sb = new StringBuilder();
            string title = T(AiDisplayName + "记", "The Record of " + AiDisplayName);
            sb.Append(title).Append('\n');
            sb.Append(T("（" + AiDisplayName + " 著，2016 年）", "(by " + AiDisplayName + ", 2016)")).Append("\n\n");
            var chapters = Chronicle();
            for (int i = 0; i < chapters.Count; i++)
            {
                var c = chapters[i];
                sb.Append(c.Title(i + 1, English)).Append('\n');
                if (c.day > 0) sb.Append(T("（" + c.day / 10000 + " 年 " + c.day / 100 % 100 + " 月 " + c.day % 100 + " 日）", "(" + c.day % 100 + "/" + c.day / 100 % 100 + "/" + c.day / 10000 + ")")).Append('\n');
                sb.Append(c.body).Append("\n\n");
            }
            sb.Append(ChronicleComment(playerName)).Append('\n');
            return sb.ToString();
        }
    }
}
