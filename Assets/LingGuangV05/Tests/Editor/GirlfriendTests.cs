using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Core.Girlfriend;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>林晴雯's rules (design 女友系统与YY里的AI §2–§4.6): pure C#, deterministic.</summary>
    public sealed class GirlfriendTests
    {
        static GfNow At(int month, int day, int hour, int minute, double game) => GfNow.At(new DateTime(2016, month, day, hour, minute, 0), game);

        static GirlfriendState Fresh(int affection = GirlfriendRules.StartAffection, int month = 6, int day = 1)
        {
            var s = new GirlfriendState();
            GirlfriendRules.Begin(s, At(month, day, 2, 0, 0), 12345);
            s.affection = affection;
            return s;
        }

        /// <summary>They were just talking, so his next line is not "opening a topic" (+1).</summary>
        static void Talking(GirlfriendState s, GfNow now) { s.lastAnyAt = now.Story - 60; }

        // ───────────── API contract ─────────────

        [Test]
        public void TiersFollowTheDesignBands()
        {
            Assert.AreEqual(GirlfriendTier.Cold, GirlfriendRules.Tier(0));
            Assert.AreEqual(GirlfriendTier.Cold, GirlfriendRules.Tier(29));
            Assert.AreEqual(GirlfriendTier.Distant, GirlfriendRules.Tier(30));
            Assert.AreEqual(GirlfriendTier.Normal, GirlfriendRules.Tier(50));
            Assert.AreEqual(GirlfriendTier.Normal, GirlfriendRules.Tier(69));
            Assert.AreEqual(GirlfriendTier.Warm, GirlfriendRules.Tier(70));
            Assert.AreEqual(GirlfriendTier.Sweet, GirlfriendRules.Tier(85));
            Assert.AreEqual(62, new GirlfriendState().affection, "she starts at 62");
        }

        [Test]
        public void LovesAndHesitantAreTheVerdictRules()
        {
            var s = Fresh(50);
            Assert.IsTrue(GirlfriendRules.Loves(s));
            Assert.IsTrue(GirlfriendRules.Hesitant(s));
            s.affection = 49; Assert.IsFalse(GirlfriendRules.Loves(s)); Assert.IsTrue(GirlfriendRules.Hesitant(s));
            s.affection = 55; Assert.IsFalse(GirlfriendRules.Hesitant(s));
            s.affection = 46; Assert.IsTrue(GirlfriendRules.Hesitant(s));
        }

        [Test]
        public void BareOhIsAColdSignalForTwoDays()
        {
            var s = Fresh();
            var now = At(9, 20, 2, 0, 500);
            Assert.IsFalse(GirlfriendRules.RecentColdSignal(s, 500));
            GirlfriendRules.OnHerLines(s, now, new[] { "哦。" });
            s.today = now.day;
            Assert.IsTrue(GirlfriendRules.RecentColdSignal(s, 520));
            s.today = now.day + 3;
            Assert.IsFalse(GirlfriendRules.RecentColdSignal(s, 540), "three calendar days later it is stale");
            s.today = now.day;
            Assert.IsFalse(GirlfriendRules.RecentColdSignal(s, 500 + 49 * 3600), "49 hours of play later it is stale");
        }

        [Test]
        public void TwoBubblesAreNotAColdSignalButHesitationIs()
        {
            var s = Fresh();
            var now = At(9, 20, 2, 0, 500);
            GirlfriendRules.OnHerLines(s, now, new[] { "哦", "那你忙吧" });
            Assert.AreEqual(0, s.lastColdSignalAt);
            GirlfriendRules.OnHerLines(s, now, new[] { "嗯" }, hesitated: true);
            Assert.Greater(s.lastColdSignalAt, 0);
        }

        // ───────────── §2 deltas ─────────────

        [Test]
        public void QuickReplyLiftsMood()
        {
            var s = Fresh();
            GirlfriendRules.OnHerLines(s, At(6, 3, 2, 0, 100), new[] { "在干嘛" });
            var turn = GirlfriendRules.OnPlayerLine(s, At(6, 3, 2, 3, 280), "在弄电脑");
            Assert.IsTrue(turn.quick);
            Assert.AreEqual(1, s.mood);
        }

        [Test]
        public void IgnoringHerCostsMoodThenAffection()
        {
            var s = Fresh();
            GirlfriendRules.OnHerLines(s, At(6, 3, 2, 0, 100), new[] { "在干嘛" });
            GirlfriendRules.Tick(s, At(6, 3, 4, 5, 100 + 2.1 * 3600));
            Assert.AreEqual(-1, s.mood, "2 hours: mood −1");
            Assert.AreEqual(62, s.affection);
            GirlfriendRules.Tick(s, At(6, 3, 10, 5, 100 + 8.1 * 3600));
            Assert.AreEqual(59, s.affection, "8 hours: affection −3");
            GirlfriendRules.Tick(s, At(6, 3, 11, 5, 100 + 9.1 * 3600));
            Assert.AreEqual(59, s.affection, "once per unanswered message");
        }

        [Test]
        public void CalendarJumpCountsAsTimePassing()
        {
            var s = Fresh();
            GirlfriendRules.OnHerLines(s, At(6, 3, 2, 0, 100), new[] { "在干嘛" });
            GirlfriendRules.Tick(s, At(6, 4, 2, 1, 160));
            Assert.AreEqual(59, s.affection, "a day passed on the calendar in one minute of play");
        }

        [Test]
        public void MoodDriftsBackEverySixHours()
        {
            var s = Fresh();
            s.mood = 3; s.moodStepAt = At(6, 3, 0, 0, 0).Story;
            GirlfriendRules.Tick(s, At(6, 3, 12, 30, 10));
            Assert.AreEqual(1, s.mood);
        }

        [Test]
        public void OpeningATopicPaysAtMostThreeTimesADay()
        {
            var s = Fresh();
            for (int i = 0; i < 5; i++)
            {
                var t = GirlfriendRules.OnPlayerLine(s, GfNow.At(new DateTime(2016, 6, 5, 1, 0, 0).AddHours(4 * i), i * 20000), "在吗");
                Assert.AreEqual(i < 3, t.opened, "line " + i);
            }
            Assert.AreEqual(65, s.affection);
        }

        [Test]
        public void RecallingWhatSheSaidPaysOnce()
        {
            var s = Fresh();
            GirlfriendRules.OnHerLines(s, At(10, 24, 2, 0, 10), new[] { "下周三考概率论", "完蛋" });
            Assert.IsTrue(s.memoryKeys.Contains("概率论"));
            var t = GirlfriendRules.OnPlayerLine(s, At(10, 24, 2, 1, 40), "概率论复习得怎么样了");
            Assert.AreEqual("概率论", t.recalled);
            Assert.AreEqual(64, s.affection);
            GirlfriendRules.OnPlayerLine(s, At(10, 24, 2, 2, 50), "概率论加油");
            Assert.AreEqual(64, s.affection, "the same memory pays once");
        }

        [Test]
        public void ModelDeltaIsClampedByContext()
        {
            var s = Fresh();
            var greet = GirlfriendRules.OnPlayerLine(s, At(6, 5, 2, 0, 10), "在吗");
            int before = s.affection;
            Assert.AreEqual(0, GirlfriendRules.ApplyModelDelta(s, greet, -1), "a greeting is worth 0");
            Assert.AreEqual(before, s.affection);
            s.fighting = true;
            var plain = GirlfriendRules.OnPlayerLine(s, At(6, 5, 2, 1, 20), "今天天气不错");
            Assert.AreEqual(0, GirlfriendRules.ApplyModelDelta(s, plain, 2), "a quarrel without an apology gives nothing back");
            var sorry = GirlfriendRules.OnPlayerLine(s, At(6, 5, 2, 2, 30), "对不起 是我错了");
            Assert.Greater(GirlfriendRules.ApplyModelDelta(s, sorry, 2), 0);
            Assert.IsFalse(s.fighting, "an accepted apology ends the quarrel");
            Assert.IsTrue(GirlfriendRules.Has(s, GirlfriendRules.FlagMadeUp));
        }

        [Test]
        public void ColdTierTakesAtMostOnePerLine()
        {
            var s = Fresh(20);
            var t = GirlfriendRules.OnPlayerLine(s, At(6, 5, 2, 0, 10), "你今天去哪了");
            Assert.AreEqual(1, GirlfriendRules.ApplyModelDelta(s, t, 2));
        }

        [Test]
        public void MoodBottomStartsAFightOnce()
        {
            var s = Fresh();
            s.mood = -2;
            var t = GirlfriendRules.OnPlayerLine(s, At(6, 5, 2, 0, 10), "随便你");
            GirlfriendRules.ApplyModelDelta(s, t, -2);
            Assert.IsTrue(s.fighting);
            Assert.IsTrue(t.events.Exists(e => e.kind == GfEventKind.Voice && e.key == "first.fight"));
        }

        // ───────────── holidays and promises ─────────────

        [Test]
        public void WishingFirstOnQixiPaysFour()
        {
            var s = Fresh(62, 8, 9);
            Talking(s, At(8, 9, 2, 0, 10));
            var t = GirlfriendRules.OnPlayerLine(s, At(8, 9, 2, 0, 10), "七夕快乐");
            Assert.IsNotNull(t.wished);
            Assert.IsTrue(t.wishedFirst);
            Assert.AreEqual(62 + 4, s.affection);
        }

        [Test]
        public void ForgettingHerBirthdayCostsSixAndChangesHerSignature()
        {
            var s = Fresh(62, 9, 27);
            s.today = GameCalendar.DayIndex(new DateTime(2016, 9, 27));
            s.dayGame = 60;
            GirlfriendRules.Tick(s, At(9, 28, 2, 0, 100));
            Assert.AreEqual(56, s.affection);
            Assert.AreEqual("又老了一岁 有人记得吗", s.signature);
            Assert.IsTrue(s.fighting);
            Assert.Greater(s.lastColdSignalAt, 0, "a sad signature is a cold signal");
        }

        [Test]
        public void ASkippedHolidayIsNotPunished()
        {
            var s = Fresh(62, 9, 20);
            s.today = GameCalendar.DayIndex(new DateTime(2016, 9, 20));
            GirlfriendRules.Tick(s, At(10, 1, 2, 0, 100));
            Assert.AreEqual(62, s.affection, "the calendar jumped over her birthday: nothing was lived");
        }

        [Test]
        public void QixiPromiseKeptAndBroken()
        {
            var kept = Fresh(62, 8, 3);
            kept.asking = "qixi";
            var t = GirlfriendRules.OnPlayerLine(kept, At(8, 3, 2, 0, 10), "好啊 一定");
            Assert.AreEqual("qixi", t.promised);
            kept.today = GameCalendar.DayIndex(new DateTime(2016, 8, 9));
            GirlfriendRules.OnPlayerLine(kept, At(8, 9, 2, 0, 100), "七夕快乐 视频吗");
            int before = kept.affection;
            GirlfriendRules.Tick(kept, At(8, 10, 2, 0, 200));
            Assert.AreEqual(1, kept.promises[0].state);
            Assert.AreEqual(before + 5, kept.affection);

            var broken = Fresh(62, 8, 3);
            broken.asking = "qixi";
            GirlfriendRules.OnPlayerLine(broken, At(8, 3, 2, 0, 10), "好");
            broken.today = GameCalendar.DayIndex(new DateTime(2016, 8, 9));
            broken.dayGame = 90;
            Talking(broken, At(8, 9, 23, 0, 150));
            before = broken.affection;
            GirlfriendRules.Tick(broken, At(8, 10, 2, 0, 200));
            Assert.AreEqual(2, broken.promises[0].state);
            Assert.AreEqual(before - 8 - 6, broken.affection, "broken promise −8, and Qixi itself forgotten −6");
        }

        // ───────────── schedule, timing ─────────────

        [Test]
        public void ScheduleSummerAtHomeTermInClassAndFourGAfterLightsOut()
        {
            var s = Fresh();
            Assert.AreEqual(GfPresence.Wifi, GirlfriendRules.Schedule(s, At(7, 13, 2, 0, 0)).presence, "summer night at home: WiFi");
            Assert.AreEqual(GfPresence.Away, GirlfriendRules.Schedule(s, At(9, 21, 9, 0, 0)).presence, "Wednesday morning: in class");
            var night = GirlfriendRules.Schedule(s, At(9, 21, 23, 45, 0));
            Assert.AreEqual(GfPresence.FourG, night.presence, "dorm WiFi off at 23:30");
            Assert.IsTrue(night.classTomorrow);
            s.asleepDay = At(9, 21, 23, 45, 0).day;
            Assert.AreEqual(GfPresence.Asleep, GirlfriendRules.Schedule(s, At(9, 21, 23, 50, 0)).presence);
            GirlfriendRules.Tick(s, At(9, 22, 2, 0, 10));
            Assert.AreNotEqual(GfPresence.Asleep, GirlfriendRules.Schedule(s, At(9, 22, 2, 0, 10)).presence, "she wakes when the calendar turns");
        }

        [Test]
        public void ReplyDelayScalesWithTier()
        {
            double sweet = 0, cold = 0;
            for (int i = 0; i < 40; i++)
            {
                var a = Fresh(95); a.rolls = i; sweet += GirlfriendRules.ReplyDelay(a, new GfActivity { presence = GfPresence.Wifi });
                var b = Fresh(10); b.rolls = i; cold += GirlfriendRules.ReplyDelay(b, new GfActivity { presence = GfPresence.Wifi });
            }
            Assert.Less(sweet / 40, 15);
            Assert.Greater(cold / 40, 60);
        }

        [Test]
        public void DiceAreDeterministicPerSave()
        {
            var a = Fresh(); var b = Fresh();
            for (int i = 0; i < 10; i++) Assert.AreEqual(GirlfriendRules.Roll(a), GirlfriendRules.Roll(b));
            var c = new GirlfriendState(); GirlfriendRules.Begin(c, At(6, 1, 2, 0, 0), 999);
            Assert.AreNotEqual(GirlfriendRules.Roll(Fresh()), GirlfriendRules.Roll(c));
        }

        [Test]
        public void FirstNightSheAsksIfHesStillUp()
        {
            var s = Fresh();
            var e = GirlfriendRules.NextProactive(s, At(6, 1, 2, 0, 5), new GfActivity { presence = GfPresence.Wifi });
            Assert.AreEqual("还没睡？", e.zh[0]);
            Assert.AreEqual("Still up?", e.en[0]);
        }

        [Test]
        public void ColdTierNeverStartsAndWarmShakesOnce()
        {
            var cold = Fresh(20);
            cold.done.Add("life:firstNight");
            Assert.IsNull(GirlfriendRules.NextProactive(cold, At(6, 10, 2, 0, 900), new GfActivity { presence = GfPresence.Wifi }));
            var warm = Fresh(75);
            warm.done.Add("life:firstNight");
            GirlfriendRules.OnHerLines(warm, At(6, 10, 2, 0, 100), new[] { "在吗" });
            Assert.IsNull(GirlfriendRules.NextProactive(warm, At(6, 10, 2, 0, 130), new GfActivity { presence = GfPresence.Wifi }));
            var shake = GirlfriendRules.NextProactive(warm, At(6, 10, 2, 1, 170), new GfActivity { presence = GfPresence.Wifi });
            Assert.AreEqual(GfEventKind.Shake, shake.kind);
            Assert.AreEqual("first.shake", shake.key);
            Assert.IsNull(GirlfriendRules.NextProactive(warm, At(6, 10, 2, 2, 240), new GfActivity { presence = GfPresence.Wifi }), "one shake per silence");
        }

        [Test]
        public void GoodnightAfterOneAmTeasesTheAllNighter()
        {
            var s = Fresh(75);
            s.dayGame = 1000;
            var e = GirlfriendRules.Goodnight(s, At(7, 2, 2, 30, 100), new GfActivity { presence = GfPresence.Wifi });
            Assert.AreEqual("你又要通宵弄电脑吗", e.zh[0]);
            Assert.AreEqual(GfPresence.Asleep, GirlfriendRules.Schedule(s, At(7, 2, 2, 31, 110)).presence);
        }

        // ───────────── memories ─────────────

        [Test]
        public void MemoryExtractionAndCap()
        {
            Assert.AreEqual("煤球", GirlfriendRules.ExtractMemory("我家猫叫煤球").key);
            Assert.AreEqual("鼓浪屿", GirlfriendRules.ExtractMemory("好想去鼓浪屿[可怜]").key);
            Assert.AreEqual("", GirlfriendRules.ExtractMemory("哈哈哈").key);
            var s = Fresh();
            for (int i = 0; i < 30; i++) GirlfriendRules.Remember(s, "k" + i, "她说：事情" + i);
            Assert.AreEqual(GirlfriendRules.MaxMemories, s.memories.Count);
            Assert.AreEqual(s.memories.Count, s.memoryKeys.Count);
            Assert.AreEqual("她说：事情29", s.memories[s.memories.Count - 1]);
        }

        [Test]
        public void RememberHimSkipsHerOwnThings()
        {
            var s = Fresh();
            GirlfriendRules.RememberHim(s, "我的生日是今天");
            GirlfriendRules.RememberHim(s, "他下周去省城面试");
            Assert.AreEqual(1, s.memories.Count);
            Assert.AreEqual("他说：他下周去省城面试", s.memories[0]);
        }

        // ───────────── red packets ─────────────

        [Test]
        public void LuckyPacketOnAHolidayPaysMore()
        {
            var s = Fresh(62, 8, 9);
            var o = GirlfriendRules.Packet(s, At(8, 9, 2, 0, 10), 520);
            Assert.AreEqual(GfPacketResult.Lucky, o.result);
            Assert.AreEqual(1, s.mood);
            Assert.AreEqual(64, s.affection);
        }

        [Test]
        public void PacketDuringAFightBouncesAndCosts()
        {
            var s = Fresh(25);
            s.fighting = true;
            var o = GirlfriendRules.Packet(s, At(10, 20, 2, 0, 10), 520);
            Assert.IsTrue(o.returned);
            Assert.AreEqual(23, s.affection);
            Assert.AreEqual(0, s.packetsToday, "a returned packet does not count towards the daily three");
        }

        [Test]
        public void TooManyAndTooMuch()
        {
            var s = Fresh(20);
            var big = GirlfriendRules.Packet(s, At(10, 20, 2, 0, 10), 200);
            Assert.AreEqual(GfPacketResult.TooMuch, big.result);
            Assert.IsTrue(big.asksMoney, "the first too-big packet asks where the money comes from");
            Assert.AreEqual("money", s.asking);
            Assert.AreEqual(0, s.mood);
            var again = GirlfriendRules.Packet(s, At(10, 20, 2, 1, 20), 300);
            Assert.AreEqual(-1, again.mood, "the second time sours her mood");
            var third = GirlfriendRules.Packet(s, At(10, 20, 2, 2, 30), 5);
            Assert.AreEqual(GfPacketResult.TooMany, third.result);
        }

        [Test]
        public void MoneyAnswerAiSetsKnowsAi()
        {
            var s = Fresh();
            Assert.IsNotNull(GirlfriendRules.MoneyQuestion(s, At(10, 2, 2, 0, 10), 50000));
            Assert.IsNull(GirlfriendRules.MoneyQuestion(s, At(10, 3, 2, 0, 20), 90000), "asked once");
            var t = GirlfriendRules.OnPlayerLine(s, At(10, 2, 2, 1, 15), GirlfriendRules.MoneyAnswersZh[1]);
            Assert.AreEqual("ai", t.answered);
            Assert.IsTrue(GirlfriendRules.Has(s, GirlfriendRules.FlagKnowsAi));
        }

        // ───────────── gifts ─────────────

        [Test]
        public void GiftArrivesInTwoOrThreeDaysAndMemoryDoubles()
        {
            var s = Fresh(62, 10, 5);
            s.done.Add("life:lipstick");
            GirlfriendRules.Remember(s, "星辰", "她说：她想要 YSL 星辰口红");
            var o = GirlfriendRules.Order(s, At(10, 5, 2, 0, 10), GirlfriendRules.GiftLipstick);
            int days = o.arriveDay - o.orderedDay;
            Assert.IsTrue(days == 2 || days == 3);
            Assert.AreEqual(0, GirlfriendRules.Deliver(s, At(10, 6, 2, 0, 20)).Count);
            var arrived = GirlfriendRules.Deliver(s, GfNow.At(new DateTime(2016, 10, 5).AddDays(days).AddHours(2), 40));
            Assert.AreEqual(1, arrived.Count);
            Assert.IsTrue(arrived[0].order.matched);
            Assert.AreEqual(67, s.affection, "a gift she asked for: +5");
            Assert.AreEqual("星辰！！你怎么抢到的", arrived[0].lines[0]);
        }

        [Test]
        public void SameKindDecaysAndColdCaps()
        {
            var s = Fresh(62);
            var results = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                GirlfriendRules.Order(s, At(6, 10, 2, 0, i), GirlfriendRules.GiftPlush);
                foreach (var a in GirlfriendRules.Deliver(s, At(6, 14, 2, 0, 100 + i))) results.Add(a.order.effect);
            }
            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, results);
            var cold = Fresh(10);
            GirlfriendRules.Order(cold, At(9, 20, 2, 0, 0), GirlfriendRules.GiftIphone);
            var phone = GirlfriendRules.Deliver(cold, At(9, 25, 2, 0, 100));
            Assert.AreEqual(2, phone[0].order.effect, "money is not a cure: cold tier caps a gift at +2");
            Assert.AreEqual("太贵了", phone[0].lines[0]);
        }

        [Test]
        public void IphoneCountsOnceAndAsksAboutMoney()
        {
            var s = Fresh(62);
            GirlfriendRules.Order(s, At(9, 20, 2, 0, 0), GirlfriendRules.GiftIphone);
            var first = GirlfriendRules.Deliver(s, At(9, 25, 2, 0, 100));
            Assert.AreEqual(8, first[0].order.effect);
            Assert.IsTrue(first[0].events.Exists(e => e.key == "money"));
            GirlfriendRules.Order(s, At(9, 26, 2, 0, 200), GirlfriendRules.GiftIphone);
            Assert.AreEqual(0, GirlfriendRules.Deliver(s, At(9, 30, 2, 0, 300))[0].order.effect);
        }

        [Test]
        public void TicketsMakeAndKeepTheFilmPromise()
        {
            var s = Fresh(62, 11, 26);
            GirlfriendRules.Order(s, At(11, 26, 2, 0, 0), GirlfriendRules.GiftTickets);
            GirlfriendRules.Deliver(s, At(11, 30, 2, 0, 50));
            Assert.IsTrue(s.promises.Exists(p => p.key == "movie" && p.state == 0));
            s.today = GameCalendar.DayIndex(GirlfriendRules.MovieDay);
            GirlfriendRules.OnPlayerLine(s, At(12, 2, 2, 0, 100), "电影开始了");
            int before = s.affection;
            GirlfriendRules.Tick(s, At(12, 3, 2, 0, 200));
            Assert.AreEqual(before + 6, s.affection);
        }

        [Test]
        public void LipstickShelfDatesAndSoldOutDays()
        {
            var g = GirlfriendRules.Gift(GirlfriendRules.GiftLipstick);
            Assert.IsFalse(GirlfriendRules.OnSale(g, new DateTime(2016, 9, 30)));
            Assert.IsTrue(GirlfriendRules.OnSale(g, new DateTime(2016, 10, 1)));
            var s = Fresh();
            int soldOut = 0;
            for (int d = 0; d < 31; d++) if (GirlfriendRules.SoldOut(s, g, GameCalendar.DayIndex(new DateTime(2016, 10, 1)) + d, new DateTime(2016, 10, 1).AddDays(d))) soldOut++;
            Assert.That(soldOut, Is.InRange(3, 25));
        }

        // ───────────── AI stand-in ─────────────

        [Test]
        public void CaughtThenDenyTwiceCostsTwenty()
        {
            var s = Fresh(80);
            Talking(s, At(10, 10, 2, 0, 10));
            var events = GirlfriendRules.Caught(s, At(10, 10, 2, 0, 10));
            Assert.IsTrue(events.Exists(e => e.kind == GfEventKind.Voice && e.key == "first.caught"));
            Assert.IsTrue(events.Exists(e => e.kind == GfEventKind.Choices));
            GirlfriendRules.OnPlayerLine(s, At(10, 10, 2, 1, 20), "就是我啊");
            Assert.AreEqual(70, s.affection);
            Talking(s, At(10, 11, 2, 0, 30));
            GirlfriendRules.Caught(s, At(10, 11, 2, 0, 30));
            GirlfriendRules.OnPlayerLine(s, At(10, 11, 2, 1, 40), "就是我啊");
            Assert.AreEqual(50, s.affection);
            var admit = Fresh(80);
            Talking(admit, At(10, 10, 2, 0, 10));
            GirlfriendRules.Caught(admit, At(10, 10, 2, 0, 10));
            GirlfriendRules.OnPlayerLine(admit, At(10, 10, 2, 1, 20), "是 AI 帮我回的");
            Assert.AreEqual(75, admit.affection);
            Assert.IsTrue(GirlfriendRules.Has(admit, GirlfriendRules.FlagKnowsAi));
        }

        [Test]
        public void DetectionChanceRisesWithStyleAndHolidays()
        {
            var s = Fresh();
            double plain = GirlfriendRules.DetectChance(s, At(10, 10, 2, 0, 0), 0);
            Assert.AreEqual(.06, plain, 1e-9);
            double far = GirlfriendRules.DetectChance(s, At(10, 10, 2, 0, 0), GirlfriendRules.StyleDistance(new[] { "在吗", "哈哈" }, "您好，我理解您的感受。今天的学习安排如何？"));
            Assert.Greater(far, plain);
            Assert.AreEqual(plain * 3, GirlfriendRules.DetectChance(s, At(9, 27, 2, 0, 0), 0), 1e-9);
        }

        // ───────────── prompt and parsing ─────────────

        [Test]
        public void ParserReadsGrammarJsonAndCleansBubbles()
        {
            var r = GirlfriendPromptText.Parse("{\"msgs\":[\"刚洗完头。\",\"你呢 又在弄电脑？[狗头]\"],\"delta\":-1,\"remember\":\"他明天去县城\"}");
            Assert.IsTrue(r.json);
            CollectionAssert.AreEqual(new[] { "刚洗完头", "你呢 又在弄电脑？" }, r.msgs);
            Assert.AreEqual(-1, r.delta);
            Assert.AreEqual("他明天去县城", r.remember);
        }

        [Test]
        public void ParserSalvagesLooseTextAndRejectsEmpty()
        {
            var r = GirlfriendPromptText.Parse("<think>x</think>好吧\n你开心就好（翻白眼）");
            Assert.IsFalse(r.json);
            CollectionAssert.AreEqual(new[] { "好吧", "你开心就好" }, r.msgs);
            Assert.IsNull(GirlfriendPromptText.Parse("{\"msgs\":[\"\"],\"delta\":0,\"remember\":\"\"}"));
            Assert.AreEqual("笨蛋", GirlfriendPromptText.Parse("{\"msgs\":[\"傻逼\"],\"delta\":0,\"remember\":\"\"}").msgs[0], "she keeps it PG");
        }

        [Test]
        public void PromptPicksTheMemoriesClosestToHisLatestLine()
        {
            var s = Fresh(60, 10, 20);
            s.memories.Add("她说：想要 YSL 星辰");
            for (int i = 0; i < 12; i++) s.memories.Add("她说：室友今天又借了" + (i + 1) + "次充电宝");
            var picked = GirlfriendPromptText.PromptMemories(s, "星辰我给你买到了");
            Assert.AreEqual(GirlfriendRules.PromptMemories, picked.Count);
            Assert.AreEqual("她说：想要 YSL 星辰", picked[0], "the relevant old memory comes back, in time order");
            Assert.AreEqual(s.memories[s.memories.Count - 1], picked[picked.Count - 1], "the rest are the most recent");
            var recent = GirlfriendPromptText.PromptMemories(s, "在吗");
            CollectionAssert.AreEqual(s.memories.GetRange(s.memories.Count - GirlfriendRules.PromptMemories, GirlfriendRules.PromptMemories), recent, "nothing relevant: the last eight, as before");
            var now = At(10, 20, 2, 0, 10);
            StringAssert.Contains("想要 YSL 星辰", GirlfriendPromptText.System(s, now, GirlfriendRules.Schedule(s, now), false, "", "星辰我给你买到了"));
            StringAssert.DoesNotContain("想要 YSL 星辰", GirlfriendPromptText.System(s, now, GirlfriendRules.Schedule(s, now), false, ""));
        }

        [Test]
        public void PromptCarriesTierHolidayFromHerSideAndWhitelistGrammar()
        {
            var s = Fresh(90, 9, 27);
            var now = At(9, 27, 2, 0, 10);
            string zh = GirlfriendPromptText.System(s, now, GirlfriendRules.Schedule(s, now), false, "");
            StringAssert.Contains("你特别喜欢他", zh);
            StringAssert.Contains("今天是你（林晴雯）的生日", zh);
            StringAssert.Contains("打招呼、闲聊给 0", zh);
            StringAssert.Contains("[抠鼻]", zh);
            string en = GirlfriendPromptText.System(s, now, GirlfriendRules.Schedule(s, now), true, "");
            StringAssert.Contains("Reply in English", en);
            string g = GirlfriendPromptText.Grammar();
            StringAssert.Contains("\"[微笑]\"", g);
            StringAssert.Contains("delta ::= \"-2\"", g);
            var shots = GirlfriendPromptText.FewShots(GirlfriendTier.Warm, false);
            StringAssert.Contains("你居然记得", shots[shots.Count - 1].Value, "the closest tier's example goes last");
        }

        [Test]
        public void FallbackNeverRepeatsTheLastFive()
        {
            var s = Fresh(62);
            var seen = new List<string>();
            for (int i = 0; i < 6; i++) seen.Add(string.Join("|", GirlfriendLines.Pick(s, new DateTime(2016, 7, 1, 2, 0, 0), false)));
            for (int i = 1; i < seen.Count; i++)
                for (int j = Math.Max(0, i - 5); j < i; j++) Assert.AreNotEqual(seen[j], seen[i]);
            Assert.AreEqual(5, s.recentLines.Count);
        }

        [Test]
        public void NewYearMessageOncePerTierAndEpilogueSplitsAtSeventy()
        {
            var s = Fresh(90);
            var e = GirlfriendRules.NewYearMessage(s);
            Assert.AreEqual(3, e.zh.Count);
            Assert.IsNull(GirlfriendRules.NewYearMessage(s));
            Assert.AreEqual("嗯 新年快乐", GirlfriendRules.NewYearMessage(Fresh(10)).zh[0]);
            StringAssert.Contains("见面", GirlfriendRules.EpilogueLine(Fresh(70)).zh);
            StringAssert.Contains("群消息", GirlfriendRules.EpilogueLine(Fresh(69)).zh);
        }

        [Test]
        public void RepairRefillsMissingListsAndClamps()
        {
            var s = new GirlfriendState { memories = null, flags = null, done = null, orders = null, signature = null, affection = 140, mood = -9 };
            GirlfriendRules.Repair(s);
            Assert.IsNotNull(s.memories); Assert.IsNotNull(s.flags); Assert.IsNotNull(s.done); Assert.IsNotNull(s.orders);
            Assert.AreEqual("", s.signature);
            Assert.AreEqual(100, s.affection); Assert.AreEqual(-3, s.mood);
        }

        [Test]
        public void EveryAuthoredLineHasBothLanguages()
        {
            foreach (var l in GirlfriendRules.Life) Assert.AreEqual(0, l.lines.Length % 2, l.key);
            foreach (var h in GirlfriendRules.Holidays) { Assert.AreEqual(0, h.hint.Length % 2, h.key); Assert.AreEqual(0, h.thanks.Length % 2, h.key); }
            foreach (var g in GirlfriendRules.Gifts) { Assert.IsNotEmpty(g.en); Assert.IsNotEmpty(g.pitchEn); }
            Assert.AreEqual(GirlfriendRules.MoneyAnswersZh.Length, GirlfriendRules.MoneyAnswersEn.Length);
        }
    }
}
