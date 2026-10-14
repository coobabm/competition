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
        public void MoneyAnswerAiStaysPrivate()
        {
            var s = Fresh();
            Assert.IsNotNull(GirlfriendRules.MoneyQuestion(s, At(10, 2, 2, 0, 10), 50000));
            Assert.IsNull(GirlfriendRules.MoneyQuestion(s, At(10, 3, 2, 0, 20), 90000), "asked once");
            var t = GirlfriendRules.OnPlayerLine(s, At(10, 2, 2, 1, 15), GirlfriendRules.MoneyAnswersZh[1]);
            Assert.AreEqual("work", t.answered, "the AI is a private application: she only hears that it is work");
            Assert.IsFalse(GirlfriendRules.Has(s, GirlfriendRules.FlagKnowsAi), "she never learns about it");
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
        public void TheWeekendQuestionComesOnlyOnceStandInExistsEvenWhenSheIsCold()
        {
            var s = Fresh(10, 10, 20);
            var now = At(10, 20, 20, 0, 0);
            Assert.AreNotEqual("weekendAsk", GirlfriendRules.DueLife(s, now)?.key, "not before stage 5");
            GirlfriendRules.Mark(s, GirlfriendRules.FlagStandInOffered);
            foreach (var l in GirlfriendRules.Life) if (l.key != "weekendAsk") s.done.Add("life:" + l.key);
            Assert.AreEqual("weekendAsk", GirlfriendRules.DueLife(s, now)?.key);
            s.awaitingReply = true;
            var said = GirlfriendRules.NextProactive(s, now, new GfActivity { presence = GfPresence.Wifi });
            Assert.AreEqual("life:weekendAsk", said?.key, "a cold tier still asks, even while he owes her an answer");
            Assert.AreEqual("weekend", s.asking);
        }

        [Test]
        public void SayingYesToTheWeekendIsRemembered()
        {
            var s = Fresh(62, 10, 20);
            s.asking = "weekend"; s.askDay = s.today;
            var turn = GirlfriendRules.OnPlayerLine(s, At(10, 20, 21, 0, 50), "好 我来");
            Assert.AreEqual("weekend", turn.promised);
            Assert.AreEqual("", s.asking);
            Assert.IsTrue(s.memories.Exists(m => m.Contains("周末")));
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
            GirlfriendRules.OnPlayerLine(admit, At(10, 10, 2, 1, 20), "刚才走神了，对不起");
            Assert.AreEqual(75, admit.affection, "admitting he was distracted costs five");
            Assert.IsFalse(GirlfriendRules.Has(admit, GirlfriendRules.FlagKnowsAi), "and still tells her nothing about the application");
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
        public void OfflineLibraryDoesNotRepeatWithinItsMemory()
        {
            var s = Fresh(62, 10, 20);
            var clock = new DateTime(2016, 10, 20, 2, 0, 0);
            var seen = new List<string>();
            for (int i = 0; i < 30; i++) seen.Add(string.Join("|", GirlfriendLines.Pick(s, clock, false, null)));
            for (int i = 0; i < 12; i++)
                for (int j = 0; j < i; j++) Assert.AreNotEqual(seen[j], seen[i], "twelve different lines before any comes back");
            for (int i = 1; i < seen.Count; i++)
                for (int j = Math.Max(0, i - 10); j < i; j++) Assert.AreNotEqual(seen[j], seen[i], "nothing within ten lines of itself");
            Assert.LessOrEqual(s.recentLines.Count, GirlfriendLines.RecentLimit);
            // Keyword answers keep apart too: four 想你 in a row get four different answers.
            var warm = Fresh(75, 10, 20);
            var love = new HashSet<string>();
            for (int i = 0; i < 4; i++) love.Add(string.Join("|", GirlfriendLines.Pick(warm, clock, false, null, "想你了")));
            Assert.AreEqual(4, love.Count);
            // Lines from the model count as hers as well.
            var t = Fresh(62, 10, 20);
            GirlfriendLines.Note(t, "你又熬夜[白眼]");
            for (int i = 0; i < 11; i++) Assert.AreNotEqual("你又熬夜[白眼]", string.Join("|", GirlfriendLines.Pick(t, clock, false, null)));
        }

        [Test]
        public void OfflineLibraryRoutesHisLineByKeyword()
        {
            Assert.AreEqual("greeting", GirlfriendLines.RouteOf("在吗"));
            Assert.AreEqual("greeting", GirlfriendLines.RouteOf("hi"));
            Assert.AreEqual("", GirlfriendLines.RouteOf("this is it"), "hi inside a word is not a greeting");
            Assert.AreEqual("whatDoing", GirlfriendLines.RouteOf("在干嘛呢"));
            Assert.AreEqual("love", GirlfriendLines.RouteOf("想你了"));
            Assert.AreEqual("goodnight", GirlfriendLines.RouteOf("那我去睡了 晚安"));
            Assert.AreEqual("ate", GirlfriendLines.RouteOf("你吃饭了没"));
            Assert.AreEqual("gift", GirlfriendLines.RouteOf("我给你买了个东西 过两天到"));
            Assert.AreEqual("work", GirlfriendLines.RouteOf("周末我可能要通宵调模型"));
            Assert.AreEqual("angry", GirlfriendLines.RouteOf("你生气了？"));
            Assert.AreEqual("sorry", GirlfriendLines.RouteOf("对不起 我错了"));
            Assert.AreEqual("question", GirlfriendLines.RouteOf("你考试准备得怎么样"));
            Assert.AreEqual("short", GirlfriendLines.RouteOf("嗯"));
            Assert.AreEqual("", GirlfriendLines.RouteOf("今天下雨了"));
            var s = Fresh(75, 10, 20);
            var clock = new DateTime(2016, 10, 20, 23, 0, 0);
            StringAssert.Contains("晚安", string.Join("|", GirlfriendLines.Pick(s, clock, false, null, "晚安")));
            StringAssert.Contains("night", string.Join("|", GirlfriendLines.Pick(s, clock, true, null, "good night")).ToLowerInvariant());
            // Every keyword answers in English too, with nothing Chinese outside the [faces].
            foreach (var his in new[] { "hi", "what are you doing", "miss you", "good night", "have you eaten", "sorry", "are you angry", "I bought you a gift", "all-nighter on the computer", "so tired", "haha", "why?", "ok", "good morning" })
                foreach (int affection in new[] { 20, 60, 90 })
                {
                    var t = Fresh(affection, 10, 20);
                    for (int i = 0; i < 4; i++)
                        foreach (var bubble in GirlfriendLines.Pick(t, clock, true, null, his))
                        {
                            string bare = System.Text.RegularExpressions.Regex.Replace(bubble, "\\[[^\\]]*\\]", "");
                            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(bare, "[\\u4e00-\\u9fff]"), his + " → " + bubble);
                        }
                }
        }

        [Test]
        public void OfflineLibraryUsesHerSummerHerLifeAndHisWords()
        {
            var s = Fresh(62, 7, 10);
            var july = new DateTime(2016, 7, 10, 15, 0, 0);
            for (int i = 0; i < 20; i++)
            {
                string line = string.Join("|", GirlfriendLines.Pick(s, july, false, null));
                Assert.IsFalse(line.Contains("上课") || line.Contains("食堂") || line.Contains("宿舍"), "home for the summer: " + line);
            }
            var t = Fresh(62, 10, 25);
            t.done.Add("life:exam");
            GirlfriendRules.RememberHim(t, "他周末要去网吧通宵");
            var oct = new DateTime(2016, 10, 25, 20, 0, 0);
            var all = new List<string>();
            for (int i = 0; i < 16; i++) all.Add(string.Join("|", GirlfriendLines.Pick(t, oct, false, null)));
            Assert.IsTrue(all.Exists(l => l.Contains("概率论")), "her exam week: " + string.Join(" / ", all));
            Assert.IsTrue(all.Exists(l => l.Contains("你周末要去网吧通宵")), "what he told her, said back to him");
            var topic = new HashSet<string>();
            for (int i = 0; i < 3; i++) topic.Add(string.Join("|", GirlfriendLines.Topic("roommate", false, t)));
            Assert.AreEqual(3, topic.Count, "an offline topic does not come back at once");
        }

        // ───────────── model or offline (GirlfriendReplyPolicy) ─────────────

        [Test]
        public void BusyModelDoesNotMeanOffline()
        {
            Assert.IsTrue(GirlfriendReplyPolicy.AskModel(true), "ready is enough, however busy the server is");
            Assert.IsFalse(GirlfriendReplyPolicy.AskModel(false));
            Assert.IsTrue(GirlfriendReplyPolicy.WaitForBoot(true, 110, 100), "a model still starting is waited for");
            Assert.IsFalse(GirlfriendReplyPolicy.WaitForBoot(true, 100 + GirlfriendReplyPolicy.BootWait + 1, 100), "but not for ever");
            Assert.IsFalse(GirlfriendReplyPolicy.WaitForBoot(false, 110, 100));
            // The request goes out inside her delay, so the model's time is hidden by it.
            Assert.AreEqual(112, GirlfriendReplyPolicy.AskAt(100, 120), 1e-9);
            Assert.AreEqual(101.2, GirlfriendReplyPolicy.AskAt(100, 103), 1e-9);
            Assert.AreEqual(GfReplyVerdict.Fallback, GirlfriendReplyPolicy.Judge(false, null, false, 0), "no answer (failure or timeout): offline at once");
        }

        [Test]
        public void RepeatOrBrokenAnswerIsRetriedOnceThenFallsBack()
        {
            var ok = GirlfriendPromptText.Parse("{\"msgs\":[\"刚洗完头\"],\"delta\":0,\"remember\":\"\"}");
            Assert.AreEqual(GfReplyVerdict.Accept, GirlfriendReplyPolicy.Judge(true, ok, false, 0));
            Assert.AreEqual(GfReplyVerdict.Retry, GirlfriendReplyPolicy.Judge(true, ok, true, 0), "a repeat is asked again");
            Assert.AreEqual(GfReplyVerdict.Retry, GirlfriendReplyPolicy.Judge(true, null, false, 0), "broken JSON is asked again");
            Assert.AreEqual(GfReplyVerdict.Fallback, GirlfriendReplyPolicy.Judge(true, ok, true, 1), "the retry repeats too: offline");
            Assert.AreEqual(GfReplyVerdict.Accept, GirlfriendReplyPolicy.Judge(true, ok, false, 1));
            Assert.Greater(GirlfriendReplyPolicy.Temperature(1), GirlfriendReplyPolicy.Temperature(0));
            Assert.AreEqual(GirlfriendPromptText.Temperature, GirlfriendReplyPolicy.Temperature(0));
            string hint = GirlfriendReplyPolicy.RetryHint(false, false, new[] { "刚洗完头" });
            StringAssert.Contains("换个说法", hint);
            StringAssert.Contains("「刚洗完头」", hint);
            StringAssert.Contains("JSON", GirlfriendReplyPolicy.RetryHint(false, true, null));
            StringAssert.Contains("differently", GirlfriendReplyPolicy.RetryHint(true, false, null));
        }

        [Test]
        public void StateBlockCarriesTheHourHerLifeAndThisTurnsNote()
        {
            var s = Fresh(62, 5, 28);
            var night = At(5, 28, 3, 5, 10);
            string state = GirlfriendPromptText.State(s, night, GirlfriendRules.Schedule(s, night), false, "");
            StringAssert.Contains("不是早上", state, "03:05 is the middle of the night");
            StringAssert.Contains("期末复习", state, "her exam week reaches the prompt");
            StringAssert.Contains("middle of the night", GirlfriendPromptText.State(s, night, GirlfriendRules.Schedule(s, night), true, ""));
            var summer = At(6, 2, 3, 20, 10);
            StringAssert.Contains("半夜", GirlfriendRules.Schedule(s, summer).zh, "summer, 03:20: up in bed, not just got up");
            StringAssert.DoesNotContain("期末复习", GirlfriendPromptText.State(s, summer, GirlfriendRules.Schedule(s, summer), false, ""), "her exams are over once the holiday starts");
            StringAssert.Contains("先正面回答", GirlfriendPromptText.Nudge(s, "你考试准备得怎么样", false, false));
            StringAssert.DoesNotContain("回答", GirlfriendPromptText.Nudge(s, "你考试准备得怎么样", false, true), "her own message has no question to answer");
            var notes = new HashSet<string>();
            for (int i = 0; i < 4; i++) { s.totalMessages = i; notes.Add(GirlfriendPromptText.Nudge(s, "今天下雨了", false, false)); }
            Assert.AreEqual(4, notes.Count, "the note rotates");
            string examples = GirlfriendPromptText.Examples(GirlfriendTier.Normal, false);
            StringAssert.Contains("不是你们真的聊过的内容", examples);
            StringAssert.Contains("刚洗完头", examples);
        }

        [Test]
        public void BubblesLoseStrayPunctuation()
        {
            var r = GirlfriendPromptText.Parse("{\"msgs\":[\"嘿嘿\",\",刚复习完\",\":\"],\"delta\":0,\"remember\":\"\"}");
            CollectionAssert.AreEqual(new[] { "嘿嘿", "刚复习完" }, r.msgs);
            Assert.AreEqual("[微笑]", GirlfriendPromptText.CleanBubble("[微笑]"), "a face alone is a message");
            Assert.AreEqual("", GirlfriendPromptText.CleanBubble("><br><br>"), "seen in Play: stray markup is dropped");
            Assert.AreEqual("好吧 你开心就好", GirlfriendPromptText.CleanBubble("好吧<br>你开心就好"));
        }

        // ───────────── texting style (GirlfriendStyle) ─────────────

        static GirlfriendState Fighting(GirlfriendState s) { s.fighting = true; return s; }

        static List<GfMove> Moves(GirlfriendState s, string his, GfTurn turn = null, int hour = 21) =>
            GirlfriendStyle.Moves(s, his, turn, new DateTime(2016, 10, 20, hour, 0, 0));

        [Test]
        public void HisLineKindsForTheStyleNote()
        {
            Assert.AreEqual(GfHisKind.Flat, GirlfriendStyle.Kind("嗯"));
            Assert.AreEqual(GfHisKind.Flat, GirlfriendStyle.Kind("哈哈哈"));
            Assert.AreEqual(GfHisKind.Flat, GirlfriendStyle.Kind("ok"));
            Assert.AreEqual(GfHisKind.Apology, GirlfriendStyle.Kind("对不起 是我不好"));
            Assert.AreEqual(GfHisKind.Goodnight, GirlfriendStyle.Kind("那我去睡了 晚安"));
            Assert.AreEqual(GfHisKind.Choice, GirlfriendStyle.Kind("晚上吃什么啊 你帮我选"));
            Assert.AreEqual(GfHisKind.Choice, GirlfriendStyle.Kind("周末看电影还是逛街？"));
            Assert.AreEqual(GfHisKind.News, GirlfriendStyle.Kind("我那个模型今天终于跑通了！"));
            Assert.AreEqual(GfHisKind.Question, GirlfriendStyle.Kind("你考试准备得怎么样"));
            Assert.AreEqual(GfHisKind.Greeting, GirlfriendStyle.Kind("在吗"));
            Assert.AreEqual(GfHisKind.Love, GirlfriendStyle.Kind("想你了"));
            Assert.AreEqual(GfHisKind.Share, GirlfriendStyle.Kind("今天老周又找我修电脑"));
        }

        [Test]
        public void StyleNoteFollowsTierMoodAndHisLine()
        {
            // Engaged: answer, then ask back; a flat line gets a flat answer at normal, a pout or 「哦。」 when warm.
            var normal = Fresh(62, 10, 20); normal.totalMessages = 10;
            CollectionAssert.AreEqual(new[] { GfMove.Answer, GfMove.AskBack }, Moves(normal, "你考试准备得怎么样"));
            CollectionAssert.AreEqual(new[] { GfMove.TrailOff }, Moves(normal, "嗯"));
            var warm = Fresh(78, 10, 20);
            CollectionAssert.AreEqual(new[] { GfMove.PeriodOh }, Moves(warm, "嗯"), "warm, mood 0: 「哦。」 and wait to be coaxed");
            warm.mood = 1;
            CollectionAssert.AreEqual(new[] { GfMove.PoutFlat }, Moves(warm, "嗯"));
            CollectionAssert.AreEqual(new[] { GfMove.Cheer, GfMove.Coax }, Moves(warm, "我那个模型今天终于跑通了！"));
            CollectionAssert.AreEqual(new[] { GfMove.Whatever }, Moves(normal, "晚上吃什么啊 你帮我选"));
            CollectionAssert.AreEqual(new[] { GfMove.Whatever, GfMove.Coax }, Moves(warm, "晚上吃什么啊 你帮我选"));
            CollectionAssert.AreEqual(new[] { GfMove.Goodnight, GfMove.Coax }, Moves(warm, "晚安"));
            // Cold and distant: bare answers, no asking back.
            CollectionAssert.AreEqual(new[] { GfMove.Brief, GfMove.AnswerBare }, Moves(Fresh(20, 10, 20), "你在干什么呢？"));
            CollectionAssert.AreEqual(new[] { GfMove.AnswerBare }, Moves(Fresh(40, 10, 20), "你考试准备得怎么样"));
            CollectionAssert.AreEqual(new[] { GfMove.PeriodOh }, Moves(Fresh(40, 10, 20), "哦"));
            // Sweet late at night acts cute; the same state always gets the same note.
            var sweet = Fresh(90, 10, 20); sweet.totalMessages = 1;
            CollectionAssert.Contains(Moves(sweet, "睡不着", null, 23), GfMove.Coax);
            Assert.AreEqual(GirlfriendPromptText.Nudge(sweet, "睡不着", false, false, null, new DateTime(2016, 10, 20, 23, 0, 0)),
                GirlfriendPromptText.Nudge(sweet, "睡不着", false, false, null, new DateTime(2016, 10, 20, 23, 0, 0)));
            // Every move has a note in both languages, at most two per turn.
            foreach (GfMove m in Enum.GetValues(typeof(GfMove))) { Assert.IsNotEmpty(GirlfriendStyle.Text(m, false)); Assert.IsNotEmpty(GirlfriendStyle.Text(m, true)); }
            for (int n = 0; n < 12; n++) { sweet.totalMessages = n; Assert.LessOrEqual(Moves(sweet, "今天下雨了", null, 23).Count, 2); }
        }

        [Test]
        public void StyleNoteChecksOnHimAfterALongSilenceAndGoesColdWhenHurt()
        {
            // He answers her 20:00 message at 23:10: 查岗, with a little jealousy when warm, 「你是不是不爱我了」 when distant.
            var s = Fresh(78, 10, 20);
            var asked = At(10, 20, 20, 0, 100);
            s.awaitingReply = true; s.herLastAt = asked.Story; s.meLastAt = asked.Story - 600;
            var turn = GirlfriendRules.OnPlayerLine(s, At(10, 20, 23, 10, 200), "刚弄完电脑");
            Assert.AreEqual(3 * 3600 + 600, turn.keptHerWaiting, 1e-6);
            Assert.Greater(turn.sinceHisLast, turn.keptHerWaiting);
            Assert.AreEqual(GfMove.CheckInJealous, Moves(s, "刚弄完电脑", turn, 23)[0]);
            Assert.AreEqual(GfMove.CheckIn, Moves(Fresh(62, 10, 20), "刚弄完电脑", turn, 23)[0]);
            CollectionAssert.Contains(Moves(Fresh(40, 10, 20), "刚弄完电脑", turn, 23), GfMove.TestSharp);
            Assert.AreEqual(GfMove.Share, Moves(Fresh(62, 10, 20), "在吗", new GfTurn(), 23)[0], "a quick answer is no reason to 查岗");
            // Hurt (a quarrel): cold shoulder, 「没事」 when asked, 「哦。」 for a flat line; an apology or a memory starts the thaw.
            var hurt = Fresh(78, 10, 20); hurt.fighting = true;
            CollectionAssert.AreEqual(new[] { GfMove.ColdShoulder }, Moves(hurt, "今天老周又找我修电脑"));
            CollectionAssert.AreEqual(new[] { GfMove.NotFine }, Moves(hurt, "你怎么不理我"));
            CollectionAssert.AreEqual(new[] { GfMove.PeriodOh }, Moves(hurt, "嗯"));
            CollectionAssert.AreEqual(new[] { GfMove.Thaw }, Moves(hurt, "对不起 我错了"));
            CollectionAssert.AreEqual(new[] { GfMove.Soften }, Moves(hurt, "概率论考得怎么样", new GfTurn { recalled = "概率论" }));
            // Sulking over a forgotten day counts as hurt until it ends.
            var sulk = Fresh(78, 9, 28); sulk.sulkUntil = GameCalendar.DayIndex(new DateTime(2016, 9, 30)); sulk.sulkZh = "有些日子 有人不记得";
            Assert.IsTrue(GirlfriendStyle.Hurt(sulk, new DateTime(2016, 9, 29, 21, 0, 0)));
            Assert.IsFalse(GirlfriendStyle.Hurt(sulk, new DateTime(2016, 10, 1, 21, 0, 0)));
            // The note in the state block: the moves' words, the always-part, nothing else; code's situation leaves only a reaction.
            string zh = GirlfriendPromptText.Nudge(hurt, "你怎么不理我", false, false, null, new DateTime(2016, 10, 20, 21, 0, 0));
            StringAssert.Contains("没事", zh);
            StringAssert.Contains("别重复", zh);
            Assert.AreEqual(GirlfriendStyle.Text(GfMove.React, false) + "别重复你前面几条的说法，也别每次都说想他。",
                GirlfriendPromptText.Nudge(s, "[红包] ¥52", false, false, null, null, true));
        }

        [Test]
        public void StableStyleGuideAndCurtFullStop()
        {
            var s = Fresh(62, 10, 20);
            string zh = GirlfriendPromptText.Stable(s, At(10, 20, 21, 0, 10), false);
            StringAssert.Contains("你的聊天习惯", zh);
            StringAssert.Contains("「哦。」带句号", zh);
            StringAssert.Contains("宿舍", zh);
            StringAssert.DoesNotContain("宿舍", GirlfriendPromptText.StyleGuide(true, false), "home for the summer");
            StringAssert.Contains("How you text", GirlfriendPromptText.Stable(s, At(10, 20, 21, 0, 10), true));
            Assert.AreEqual("哦。", GirlfriendPromptText.CleanBubble("哦。"), "her 「哦。」 keeps its full stop");
            Assert.AreEqual("好的", GirlfriendPromptText.CleanBubble("好的。"));
            Assert.AreEqual("厉害了", GirlfriendPromptText.CleanBubble("牛逼了"));
            foreach (var shot in GirlfriendPromptText.FewShots(GirlfriendTier.Normal, false)) if (shot.Key == "user") Assert.AreNotEqual("嗯", shot.Value, "the model copied a 「嗯」 example word for word");
            Assert.IsNotNull(GirlfriendPromptText.Parse(GirlfriendPromptText.FewShots(GirlfriendTier.Distant, false)[GirlfriendPromptText.FewShots(GirlfriendTier.Distant, false).Count - 1].Value));
        }

        [Test]
        public void OfflineLibraryRoutesChoiceLatenessAndHurt()
        {
            Assert.AreEqual("choose", GirlfriendLines.RouteOf("晚上吃什么啊 你帮我选"));
            Assert.AreEqual("choose", GirlfriendLines.RouteOf("you decide"));
            Assert.AreEqual("late", GirlfriendLines.RouteOf("刚弄完电脑"));
            Assert.AreEqual("late", GirlfriendLines.RouteOf("才看到消息"));
            Assert.AreEqual("ate", GirlfriendLines.RouteOf("你吃饭了没"), "eating is still eating");
            var clock = new DateTime(2016, 10, 20, 21, 0, 0);
            string choose = string.Join("|", GirlfriendLines.Pick(Fresh(62, 10, 20), clock, false, null, "晚上吃什么啊 你帮我选"));
            Assert.IsTrue(choose.Contains("随便") || choose.Contains("都行") || choose.Contains("选"), "she leaves it to him: " + choose);
            // He kept her waiting for hours: whatever he says, she checks on him.
            var warm = Fresh(78, 10, 20);
            string checkIn = string.Join("|", GirlfriendLines.Pick(warm, clock, false, null, "在吗", new GfTurn { keptHerWaiting = 3 * 3600 }));
            Assert.IsTrue(checkIn.Contains("久") || checkIn.Contains("刚才") || checkIn.Contains("等你"), checkIn);
            // Hurt: the cool lines whatever her tier, no faces; his sorry gets the first step of a thaw.
            var hurt = Fresh(78, 10, 20); hurt.fighting = true;
            for (int i = 0; i < 6; i++)
            {
                string line = string.Join("|", GirlfriendLines.Pick(hurt, clock, false, null, "今天下雨了"));
                Assert.IsFalse(line.Contains("["), "no faces while hurt: " + line);
            }
            var cool = new[] { "嗯", "哦。", "行", "好" };
            CollectionAssert.Contains(cool, string.Join("|", GirlfriendLines.Pick(hurt, clock, false, null, "嗯")));
            string thaw = string.Join("|", GirlfriendLines.Pick(Fighting(Fresh(78, 10, 20)), clock, false, null, "对不起"));
            Assert.IsTrue(thaw.Contains("哼") || thaw.Contains("道歉") || thaw.Contains("下不为例"), "a first step, not the make-up: " + thaw);
            foreach (var his in new[] { "you decide", "just got back", "sorry", "ok" })
                foreach (var t in new[] { Fresh(60, 10, 20), Fighting(Fresh(90, 10, 20)) })
                    for (int i = 0; i < 4; i++)
                        foreach (var bubble in GirlfriendLines.Pick(t, clock, true, null, his, new GfTurn { keptHerWaiting = i * 3600 }))
                            Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(System.Text.RegularExpressions.Regex.Replace(bubble, "\\[[^\\]]*\\]", ""), "[\\u4e00-\\u9fff]"), his + " → " + bubble);
        }

        [Test]
        public void AcceptedRedPacketIsThankedFirstAndAReturnedOneIsNot()
        {
            var s = Fresh(66, 10, 20);
            var o = GirlfriendRules.Packet(s, At(10, 20, 21, 0, 10), 52);
            Assert.IsTrue(o.thanks);
            Assert.AreEqual(GirlfriendRules.ThanksZh, o.lines[0], "the offline answer leads with 谢谢哥哥");
            Assert.AreEqual("Thanks, gege~", o.lines[1]);
            Assert.AreEqual(0, o.lines.Length % 2);
            StringAssert.Contains("谢谢哥哥", o.situationZh, "the model is told the thanks is already sent");
            // The model's own bubbles: thanks goes first, without being said twice.
            CollectionAssert.AreEqual(new[] { "谢谢哥哥", "收到啦", "好土[偷笑]" }, GirlfriendRules.ThankFirst(new[] { "收到啦", "好土[偷笑]" }, false));
            CollectionAssert.AreEqual(new[] { "谢谢哥哥", "你哪来这么多钱" }, GirlfriendRules.ThankFirst(new[] { "谢啦", "你哪来这么多钱" }, false));
            CollectionAssert.AreEqual(new[] { "谢谢哥哥[害羞]", "收下啦" }, GirlfriendRules.ThankFirst(new[] { "谢谢[害羞]", "收下啦" }, false));
            CollectionAssert.AreEqual(new[] { "谢谢哥哥～", "哈哈哈" }, GirlfriendRules.ThankFirst(new[] { "谢谢哥哥～", "哈哈哈" }, false));
            CollectionAssert.AreEqual(new[] { "Thanks, gege~", "so cheesy" }, GirlfriendRules.ThankFirst(new[] { "thank you!", "so cheesy" }, true));
            CollectionAssert.AreEqual(new[] { "Thanks, gege~", "type faster" }, GirlfriendRules.ThankFirst(new[] { "type faster" }, true));
            Assert.AreEqual(3, GirlfriendRules.ThankFirst(new[] { "一", "二", "三" }, false).Count);
            // Too much is still taken: thanks, then where did the money come from.
            var big = GirlfriendRules.Packet(Fresh(66, 10, 20), At(10, 20, 21, 0, 10), 300);
            Assert.AreEqual(GfPacketResult.TooMuch, big.result);
            Assert.AreEqual("谢谢哥哥", big.lines[0]);
            Assert.AreEqual("你哪来这么多钱？", big.lines[2]);
            // Sent back in a quarrel, or taken coldly: no thanks.
            var fight = Fresh(66, 10, 20); fight.fighting = true;
            var back = GirlfriendRules.Packet(fight, At(10, 20, 21, 0, 10), 52);
            Assert.IsTrue(back.returned);
            Assert.IsFalse(back.thanks);
            Assert.AreNotEqual(GirlfriendRules.ThanksZh, back.lines[0]);
            StringAssert.DoesNotContain("谢谢哥哥", back.situationZh);
            var cold = GirlfriendRules.Packet(Fresh(20, 10, 20), At(10, 20, 21, 0, 10), 5.2);
            Assert.IsFalse(cold.returned);
            Assert.IsFalse(cold.thanks, "the cold tier takes it without a sweet word");
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
