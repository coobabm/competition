using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Girlfriend
{
    public enum GfPacketResult { Lucky, Normal, TooMuch, TooMany, Returned }

    /// <summary>What a red packet did (design §4.6). Code decides; the model only gets <see cref="situationZh"/> to word her answer.</summary>
    public sealed class GfPacketOutcome
    {
        public GfPacketResult result;
        public bool returned;
        public int mood, affection;
        /// <summary>Her answer if the model is not running (zh, en pairs).</summary>
        public string[] lines;
        public string situationZh, situationEn;
        /// <summary>She asked 「你哪来这么多钱？」 for the first time: the YY layer offers the two answers.</summary>
        public bool asksMoney;
    }

    /// <summary>A 「送她」 item in 淘货 (design §4.6, prices as in 2016).</summary>
    public sealed class GfGift
    {
        public string id, zh, en, pitchZh, pitchEn;
        public double price;
        public DateTime from;
        /// <summary>Words in her memories that make this the gift she asked for.</summary>
        public string[] match;
        public int baseEffect, matchEffect;
    }

    /// <summary>A gift that was signed for today, and what it did.</summary>
    public sealed class GfArrival
    {
        public GfGiftOrder order;
        public GfGift gift;
        public bool refused;
        /// <summary>Her reaction (zh, en pairs).</summary>
        public string[] lines;
        public readonly List<GfEvent> events = new List<GfEvent>();
    }

    public static partial class GirlfriendRules
    {
        /// <summary>A red packet in chat is a text line with this prefix: 「[红包] ¥5.20 恭喜发财，大吉大利」.</summary>
        public const string PacketPrefix = "[红包]";
        public const string GiftMilkTea = "milktea", GiftPlush = "plush", GiftBand = "band", GiftPowerBank = "powerbank", GiftLipstick = "lipstick",
            GiftTickets = "tickets", GiftIphone = "iphone7", GiftCase = "case";

        static readonly double[] Lucky = { 5.20, 13.14, 52.0, 520, 1314 };

        public static bool IsPacket(string text) => text != null && text.StartsWith(PacketPrefix, StringComparison.Ordinal);

        public static string PacketText(double amount, bool english) => PacketPrefix + " ¥" + amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + (english ? " Best wishes" : " 恭喜发财，大吉大利");

        public static bool IsLucky(double amount) { foreach (var l in Lucky) if (Math.Abs(amount - l) < .005) return true; return false; }

        /// <summary>The most a packet may be before it stops meaning anything: 冷 ¥50 … 甜 ¥500.</summary>
        public static double PacketCap(GirlfriendTier t)
        {
            switch (t) { case GirlfriendTier.Cold: return 50; case GirlfriendTier.Distant: return 100; case GirlfriendTier.Normal: return 200; case GirlfriendTier.Warm: return 300; default: return 500; }
        }

        /// <summary>
        /// He sent her a red packet. A quarrel bounces it (−2, money back); a third one today does nothing; a lucky
        /// number (5.20, 13.14, 52, 520, 1314) lifts her mood, more on a holiday, whatever her limit (except in the
        /// cold tier); any other amount over her limit buys nothing and from the second time sours her mood; an
        /// ordinary one lifts her mood.
        /// </summary>
        public static GfPacketOutcome Packet(GirlfriendState s, GfNow now, double amount)
        {
            var o = new GfPacketOutcome();
            if (s.packetsDay != now.day) { s.packetsDay = now.day; s.packetsToday = 0; }
            s.packetsToday++;
            var tier = Tier(s);
            string amt = "¥" + amount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            if (s.fighting || (tier == GirlfriendTier.Cold && s.mood <= -2))
            {
                o.result = GfPacketResult.Returned; o.returned = true; o.affection = -2;
                o.lines = new[] { "你以为发红包就完了？", "You think a red packet fixes this?", "退回去了", "Sent it back" };
                o.situationZh = "他刚给你发了一个 " + amt + " 的红包，但你们在冷战，你没收，把红包退回去了。你还在生气。";
                o.situationEn = "He just sent you a " + amt + " red packet, but you are fighting; you did not take it and sent it back. You are still angry.";
                s.packetsToday--;
            }
            else if (s.packetsToday > 2)
            {
                o.result = GfPacketResult.TooMany;
                o.lines = new[] { "别发了 我又不是图这个", "Stop sending them. That's not what I'm after" };
                o.situationZh = "他今天已经给你发了好几个红包，这是又一个 " + amt + " 的。你收了，但叫他别发了，你不是图这个。";
                o.situationEn = "He has sent you several red packets today; this is another " + amt + ". You took it but told him to stop: that is not what you want.";
            }
            else if (IsLucky(amount) && (tier != GirlfriendTier.Cold || amount <= PacketCap(tier)))
            {
                o.result = GfPacketResult.Lucky; o.mood = 1;
                if (HolidayOn(now.clock) != null && HolidayOn(now.clock).forgot < 0) o.affection = 2;
                o.lines = tier == GirlfriendTier.Cold ? new[] { "收下了", "Taken" } : new[] { "哈哈哈 你好土", "Hahaha you're so cheesy", "收下了[抠鼻]", "Taken [抠鼻]" };
                o.situationZh = "他给你发了一个 " + amt + " 的红包（谐音「我爱你」之类的数字）。你收下了，觉得他好土但心里挺开心。";
                o.situationEn = "He sent you a " + amt + " red packet (a number that sounds like \"I love you\"). You took it; it is cheesy and you secretly like it.";
            }
            else if (amount > PacketCap(tier) + 1e-6)
            {
                o.result = GfPacketResult.TooMuch;
                if (s.bigPackets > 0) o.mood = -1;
                s.bigPackets++;
                o.lines = new[] { "你哪来这么多钱？", "Where did you get that much money?" };
                o.situationZh = "他突然给你发了一个 " + amt + " 的大红包，多得不正常。你收了，但第一反应是问他哪来这么多钱。";
                o.situationEn = "He suddenly sent you a " + amt + " red packet, far too much. You took it, but your first reaction is to ask where the money came from.";
                if (!Has(s, FlagMoneyAsked)) { Mark(s, FlagMoneyAsked); s.asking = "money"; s.askDay = now.day; o.asksMoney = true; }
            }
            else
            {
                o.result = GfPacketResult.Normal; o.mood = 1;
                o.lines = new[] { "干嘛突然发红包", "Why the sudden red packet?", "那我收啦[偷笑]", "I'll take it then [偷笑]" };
                o.situationZh = "他突然给你发了一个 " + amt + " 的红包，你收下了，有点意外。";
                o.situationEn = "He suddenly sent you a " + amt + " red packet. You took it, a little surprised.";
            }
            AddMood(s, o.mood);
            AddAffection(s, o.affection);
            return o;
        }

        /// <summary>
        /// Her red packet to him (design §4.6): warm or sweet, right after her exam, ¥8.88 「请你喝奶茶」. Once.
        /// </summary>
        public static double HerPacketDue(GirlfriendState s, GfNow now)
        {
            if (s == null || Tier(s) < GirlfriendTier.Warm || s.done.Contains("herPacket") || !s.done.Contains("life:examDone")) return 0;
            s.done.Add("herPacket");
            return 8.88;
        }

        // ───────────── 「你哪来的钱」 (design §4.6) ─────────────

        /// <summary>His wallet passed ¥30,000 (or the iPhone arrived): she asks once where the money comes from.</summary>
        public static GfEvent MoneyQuestion(GirlfriendState s, GfNow now, double wallet, bool force = false)
        {
            if (s == null || !s.started || Has(s, FlagMoneyAsked) || s.asking.Length > 0 || s.awaitingReply) return null;
            if (!force && wallet < 30000) return null;
            Mark(s, FlagMoneyAsked);
            s.asking = "money"; s.askDay = now.day;
            return GfEvent.Say("money", "你最近哪来这么多钱？", "Where's all this money coming from lately?", "别干什么奇怪的事啊", "Don't be doing anything weird");
        }

        public static readonly string[] MoneyAnswersZh = { "接了点私活", "我训练了一个 AI 帮我挣钱" };
        public static readonly string[] MoneyAnswersEn = { "Took on some side jobs", "I trained an AI that makes money for me" };

        /// <summary>Her reply to his answer: half-believing the side jobs, or 「你又在瞎折腾什么」.</summary>
        public static string[] MoneyReply(string answered)
        {
            return answered == "ai"
                ? new[] { "你又在瞎折腾什么", "What are you messing about with now", "AI 还能挣钱？", "An AI can make money?" }
                : new[] { "私活？", "Side jobs?", "好吧 别太累了", "OK. Don't wear yourself out" };
        }

        // ───────────── gifts (design §4.6) ─────────────

        public static readonly GfGift[] Gifts =
        {
            new GfGift { id = GiftMilkTea, zh = "奶茶外卖（饿了么）", en = "Bubble tea delivery (Ele.me)", price = 25, from = GameCalendar.Start.Date, match = new[] { "奶茶", "bubble tea" }, baseEffect = 0, matchEffect = 1,
                pitchZh = "当天送到宿舍楼下，再冷的天也是热的", pitchEn = "Delivered to her dorm the same day, hot even on a cold night" },
            new GfGift { id = GiftPlush, zh = "熊本熊 / 阿狸公仔", en = "Kumamon / Ali plush", price = 129, from = GameCalendar.Start.Date, match = new[] { "熊本熊", "阿狸", "公仔", "kumamon" }, baseEffect = 2, matchEffect = 4,
                pitchZh = "正版授权 40cm 抱着睡刚好", pitchEn = "Licensed, 40 cm, just right to hug at night" },
            new GfGift { id = GiftBand, zh = "小米手环 2", en = "Mi Band 2", price = 149, from = new DateTime(2016, 6, 7), match = new[] { "跑步", "减肥", "手环", "running" }, baseEffect = 2, matchEffect = 4,
                pitchZh = "OLED 屏 计步 心率 续航 20 天", pitchEn = "OLED screen, steps, heart rate, 20-day battery" },
            new GfGift { id = GiftPowerBank, zh = "罗马仕充电宝 10000mAh", en = "Romoss power bank 10,000 mAh", price = 99, from = GameCalendar.Start.Date, match = new[] { "没电", "充电宝", "battery" }, baseEffect = 1, matchEffect = 4,
                pitchZh = "上课再也不关机", pitchEn = "No more phone dying in class" },
            new GfGift { id = GiftLipstick, zh = "YSL 星辰口红 限量", en = "YSL Star lipstick (limited)", price = 320, from = new DateTime(2016, 10, 1), match = new[] { "星辰", "口红", "lipstick" }, baseEffect = 2, matchEffect = 5,
                pitchZh = "2016 年最火的一支 代购现货 手慢无", pitchEn = "The lipstick of 2016. Proxy-bought, in stock, gone in minutes" },
            new GfGift { id = GiftTickets, zh = "《你的名字。》电影票 ×2", en = "Your Name cinema tickets ×2", price = 80, from = new DateTime(2016, 11, 25), match = new[] { "你的名字", "电影", "your name" }, baseEffect = 2, matchEffect = 2,
                pitchZh = "12 月 2 日首映场 预售中", pitchEn = "Premiere showing on 2 December, pre-sale" },
            new GfGift { id = GiftIphone, zh = "iPhone 7 32G 亮黑色", en = "iPhone 7 32 GB Jet Black", price = 5388, from = new DateTime(2016, 9, 16), match = new string[0], baseEffect = 8, matchEffect = 8,
                pitchZh = "国行 9 月 16 日首发 防水 没有耳机孔", pitchEn = "Launched 16 September. Water-resistant. No headphone jack" },
            new GfGift { id = GiftCase, zh = "情侣手机壳一对", en = "Matching couple phone cases", price = 39, from = GameCalendar.Start.Date, match = new[] { "手机壳", "phone case" }, baseEffect = 1, matchEffect = 3,
                pitchZh = "一人一个 拼起来是一颗心", pitchEn = "One each; together they make a heart" },
        };

        public static GfGift Gift(string id) { foreach (var g in Gifts) if (g.id == id) return g; return null; }

        /// <summary>On the shelf today (its 2016 release date has come).</summary>
        public static bool OnSale(GfGift g, DateTime today) => g != null && today.Date >= g.from.Date;

        /// <summary>The YSL Star sold out on many days in October 2016 (显示「已售罄」).</summary>
        public static bool SoldOut(GirlfriendState s, GfGift g, int day, DateTime today)
        {
            if (g == null || g.id != GiftLipstick) return false;
            return DayDie(s, day, 23) < (today.Month == 10 ? .4 : .15);
        }

        /// <summary>Gifts of this kind already signed for (the decay: full, half, then nothing).</summary>
        public static int Delivered(GirlfriendState s, string gift)
        {
            int n = 0;
            foreach (var o in s.orders) if (o.gift == gift && o.state == 1) n++;
            return n;
        }

        /// <summary>
        /// Orders a gift: free shipping, signed for two or three calendar days later (milk tea within the hour, in
        /// play about half a minute). Paying is the caller's job. Ordering on Singles' Day empties her cart (+3, once).
        /// </summary>
        public static GfGiftOrder Order(GirlfriendState s, GfNow now, string giftId)
        {
            var g = Gift(giftId);
            if (s == null || g == null) return null;
            var o = new GfGiftOrder { gift = g.id, price = g.price, orderedDay = now.day };
            if (g.id == GiftMilkTea) { o.arriveDay = now.day; o.arriveAt = now.game + 25; }
            else o.arriveDay = now.day + 2 + (Roll(s) < .5 ? 1 : 0);
            s.orders.Add(o);
            if (now.clock.Month == 11 && now.clock.Day == 11 && !s.done.Contains("cart")) { s.done.Add("cart"); AddAffection(s, 3); AddMood(s, 1); }
            return o;
        }

        /// <summary>The effect a gift will have when it arrives (before the cold-tier cap), and whether it matches something she said.</summary>
        public static int GiftEffect(GirlfriendState s, GfGift g, out bool matched)
        {
            matched = g.match.Length > 0 && HasMemory(s, g.match);
            int e = matched ? g.matchEffect : g.baseEffect;
            if (g.id == GiftCase && Tier(s) >= GirlfriendTier.Warm) e = matched ? 6 : 3;
            if (g.id == GiftIphone) return Has(s, FlagIphone) ? 0 : 8;
            int before = Delivered(s, g.id);
            if (before == 1) e = (int)Math.Ceiling(e / 2.0);
            else if (before >= 2) e = 0;
            return e;
        }

        /// <summary>Parcels signed for by now: effects land (capped at +2 in the cold tier), and her reaction is returned.</summary>
        public static List<GfArrival> Deliver(GirlfriendState s, GfNow now)
        {
            var list = new List<GfArrival>();
            if (s == null) return list;
            foreach (var o in s.orders)
            {
                if (o.state != 0) continue;
                bool due = o.arriveAt > 0 ? now.game >= o.arriveAt || now.day > o.arriveDay : now.day >= o.arriveDay;
                if (!due) continue;
                var g = Gift(o.gift);
                var a = new GfArrival { order = o, gift = g };
                var tier = Tier(s);
                if (g.id == GiftMilkTea && (tier == GirlfriendTier.Cold || s.fighting) && Roll(s) < .5)
                {
                    o.state = 2; a.refused = true;
                    a.lines = new[] { "奶茶我没要 让小哥退了", "I refused the bubble tea. Told the guy to take it back" };
                    list.Add(a);
                    continue;
                }
                int e = GiftEffect(s, g, out bool matched);
                if (tier == GirlfriendTier.Cold) e = Math.Min(e, 2);
                if (g.id == GiftTickets && OpenPromise(s, "movie")) e = 0; // the +6 comes when the promise is kept
                o.state = 1; o.effect = e; o.matched = matched;
                AddAffection(s, e);
                if (g.id == GiftMilkTea) AddMood(s, 1);
                else if (e > 0) AddMood(s, 1);
                if (g.id == GiftIphone) Mark(s, FlagIphone);
                if (g.id == GiftTickets && !OpenPromise(s, "movie") && now.clock.Date <= MovieDay)
                    s.promises.Add(new GfPromise { key = "movie", dueDay = GameCalendar.DayIndex(MovieDay), zh = "12 月 2 日一起看《你的名字。》", en = "watch Your Name together on 2 December" });
                a.lines = GiftReaction(s, g, matched, Delivered(s, g.id));
                if (g.id == GiftIphone)
                {
                    var q = MoneyQuestion(s, now, 0, true);
                    if (q != null) a.events.Add(q);
                }
                list.Add(a);
            }
            return list;
        }

        static bool OpenPromise(GirlfriendState s, string key)
        {
            foreach (var p in s.promises) if (p.key == key && p.state == 0) return true;
            return false;
        }

        /// <summary>Her YY message when a parcel is signed for (zh, en pairs).</summary>
        static string[] GiftReaction(GirlfriendState s, GfGift g, bool matched, int count)
        {
            var tier = Tier(s);
            if (count >= 3) return new[] { "又是这个", "This again", "你是不是只会买这个[白眼]", "Is this the only thing you know how to buy? [白眼]" };
            if (g.id == GiftIphone)
                return tier <= GirlfriendTier.Distant
                    ? new[] { "太贵了", "That's too expensive", "……我先收着", "…I'll keep it for now" }
                    : new[] { "iPhone 7？？？", "An iPhone 7???", "你疯了吧 太贵了", "Are you crazy, it's so expensive", "……亮黑色的 我好喜欢[流泪]", "…Jet Black. I love it [流泪]" };
            if (matched)
            {
                switch (g.id)
                {
                    case GiftLipstick: return new[] { "星辰！！你怎么抢到的", "The Star!! How did you get one?", "你居然记得[流泪]", "You remembered [流泪]" };
                    case GiftPowerBank: return new[] { "充电宝收到了", "Got the power bank", "你居然记得我手机老没电[爱心]", "You remembered my phone always dies [爱心]" };
                    case GiftBand: return new[] { "哈哈 那你监督我跑步", "Haha, so you'll keep me running", "你居然记得我说要减肥", "You remembered I said I'd diet" };
                    case GiftPlush: return new[] { "熊本熊！！", "Kumamon!!", "你居然记得 我今晚抱着睡[害羞]", "You remembered. Sleeping with it tonight [害羞]" };
                    case GiftMilkTea: return new[] { "奶茶到了！你居然记得", "The bubble tea's here! You remembered", "还是热的[爱心]", "Still hot [爱心]" };
                    case GiftCase: return new[] { "情侣手机壳…", "Matching cases…", "幼稚死了 我已经换上了[害羞]", "So childish. I already put mine on [害羞]" };
                    case GiftTickets: return new[] { "票收到了！", "Got the tickets!", "12 月 2 号 说好了啊", "2 December, it's a date" };
                }
            }
            switch (g.id)
            {
                case GiftMilkTea: return new[] { "奶茶到了", "The bubble tea's here", "谢谢啦[微笑]", "Thanks [微笑]" };
                case GiftPlush: return new[] { "公仔收到了", "Got the plush", "还挺可爱的", "It's pretty cute" };
                case GiftBand: return new[] { "哈哈 那你监督我跑步", "Haha, so you'll keep me running" };
                case GiftPowerBank: return new[] { "充电宝收到了 谢谢", "Got the power bank, thanks" };
                case GiftLipstick: return new[] { "口红收到了", "Got the lipstick", "这个色号好难抢的", "This shade is so hard to get" };
                case GiftTickets: return new[] { "两张《你的名字。》？", "Two Your Name tickets?", "那 12 月 2 号一起看[害羞]", "Then let's watch it together on the 2nd [害羞]" };
                case GiftCase: return tier >= GirlfriendTier.Warm ? new[] { "情侣手机壳！", "Couple cases!", "幼稚 但我喜欢", "Childish, but I like it" } : new[] { "手机壳收到了", "Got the phone case" };
            }
            return new[] { "收到了", "Got it" };
        }

        // ───────────── 让 AI 代我回 (design §4.5) ─────────────

        /// <summary>
        /// How unlike his own lines the AI's line reads, 0–1.5: length, punctuation and faces compared with his
        /// recent messages to her. The further, the likelier she notices.
        /// </summary>
        public static double StyleDistance(IList<string> his, string ai)
        {
            if (his == null || his.Count == 0 || string.IsNullOrEmpty(ai)) return .5;
            double len = 0, punct = 0, faces = 0;
            foreach (var l in his) { len += (l ?? "").Length; punct += Punct(l); faces += Chat.YYFaces.CountFaces(l ?? ""); }
            len /= his.Count; punct /= his.Count; faces /= his.Count;
            double dLen = Math.Abs(ai.Length - len) / Math.Max(8, Math.Max(len, ai.Length));
            double dPunct = Math.Min(1, Math.Abs(Punct(ai) - punct) / 3);
            double dFace = Math.Min(1, Math.Abs(Chat.YYFaces.CountFaces(ai) - faces));
            return Math.Min(1.5, dLen + .5 * dPunct + .3 * dFace);
        }

        static int Punct(string s) { int n = 0; foreach (char c in s ?? "") if ("，。！？；：,.!?;:".IndexOf(c) >= 0) n++; return n; }

        /// <summary>The chance she notices one AI-written line: about 6%, more the less it sounds like him, ×3 on holidays and in a quarrel.</summary>
        public static double DetectChance(GirlfriendState s, GfNow now, double styleDistance)
        {
            double p = .06 * (1 + Math.Max(0, styleDistance));
            if (s.fighting || (HolidayOn(now.clock) != null && HolidayOn(now.clock).forgot < 0)) p *= 3;
            return Math.Min(.5, p);
        }

        /// <summary>She caught it: 「你今天说话怎么怪怪的」, 「这不是你吧？」, and his two answers.</summary>
        public static List<GfEvent> Caught(GirlfriendState s, GfNow now)
        {
            var events = new List<GfEvent>();
            s.aiCaught++;
            if (Mark(s, FlagCaughtAi)) events.Add(GfEvent.Of(GfEventKind.Voice, "first.caught"));
            s.asking = "caught"; s.askDay = now.day;
            events.Add(GfEvent.Say("caught", "你今天说话怎么怪怪的", "You sound weird today", "这不是你吧？", "This isn't you, is it?"));
            events.Add(GfEvent.Of(GfEventKind.Choices, "caught", "是 AI 帮我回的", "The AI was answering for me", "就是我啊", "It's me, of course"));
            return events;
        }

        /// <summary>Her answer to his confession or denial.</summary>
        public static string[] CaughtReply(GirlfriendState s, string answered)
        {
            if (answered == "admit") return new[] { "……", "…", "所以刚才那些都不是你说的", "So none of that was you", "你那个 AI 还会谈恋爱？", "Your AI does romance now?" };
            if (s.aiCaught > 1) return new[] { "你又骗我", "You lied to me again", "我不想说话了", "I don't want to talk" };
            return new[] { "哦", "Oh", "那可能是我想多了", "Maybe I'm overthinking it" };
        }

        /// <summary>Stage 6: the AI declines to answer her, once and for good (design §4.5).</summary>
        public const string AiRefusalZh = "她问的是你，不是我。", AiRefusalEn = "She is asking you, not me.";
    }
}
