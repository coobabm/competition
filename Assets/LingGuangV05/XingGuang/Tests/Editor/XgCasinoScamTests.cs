using System;
using NUnit.Framework;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>888.vip scam story (XgSim.Gambling.cs): it only watches settled rounds; odds and payouts stay untouched.</summary>
    public sealed class XgCasinoScamTests
    {
        sealed class Host : IXgHost
        {
            public double money = 10000;
            public int debits, credits;
            public double Money => money;
            public double Compute => 1;
            public double VramMB => 1024;
            public string Blocker => null;
            public void Train(double seconds) { }
            public bool Spend(double amount) { if (money < amount) return false; money -= amount; debits++; return true; }
            public void Earn(double amount) { money += amount; credits++; }
        }

        static string Json(XgState state)
        {
#if UNITY_5_3_OR_NEWER
            return JsonUtility.ToJson(state);
#else
            return System.Text.Json.JsonSerializer.Serialize(state,new System.Text.Json.JsonSerializerOptions{IncludeFields=true});
#endif
        }
        static XgState State(string json)
        {
#if UNITY_5_3_OR_NEWER
            return JsonUtility.FromJson<XgState>(json);
#else
            return System.Text.Json.JsonSerializer.Deserialize<XgState>(json,new System.Text.Json.JsonSerializerOptions{IncludeFields=true});
#endif
        }
        static XgSim Lab(int stage = 5)
        {
            var sim = new XgSim(new XgState { progressionVersion = XgSim.ProgressionSchemaWalls, stage = stage, stageVision = stage, stageSequence = stage }) { Today = 20160901 };
            sim.S.stage = stage;
            sim.LearningClock = () => new XgLearningTime(sim.Today, 3600);
            sim.S.gradualSystems = stage >= 5; // Declared legacy availability for the stage-2/3 speech tests.
            return sim;
        }

        static XgCasinoRound Bet(XgSim sim, Host host, long stake = 1)
        {
            sim.Clock += 2;
            Assert.IsTrue(sim.TryCasinoBet("roulette", 0, stake, host, out var round));
            return round;
        }

        static void FreezeByRounds(XgSim sim, Host host)
        {
            for (int i = 0; i < XgSim.CasinoScamRounds; i++) Bet(sim, host);
            Assert.IsTrue(sim.CasinoFrozenNow);
        }

        static void FreezeByWin(XgSim sim, Host host)
        {
            Bet(sim, host);
            sim.Casino.returned += 1000; host.money += 1000; // a lucky streak, as far as the books go
            Bet(sim, host);
            Assert.IsTrue(sim.CasinoFrozenNow);
        }

        [Test]
        public void NormalAbilityGateRejectsUnavailableBetsAndDoesNotInventObservation()
        {
            var sim = Lab(4); sim.S.gradualSystems=true; var host = new Host();
            Assert.IsFalse(sim.TryCasinoBet("roulette",0,10,host,out _));
            Assert.IsFalse(sim.StartBlackjack(10,host));
            Assert.IsFalse(sim.S.casinoWatching); Assert.AreEqual(0,host.debits);
            sim.S.stage=5; Bet(sim,host); Assert.IsTrue(sim.S.casinoWatching);
        }

        [Test]
        public void ThirtyWatchedRoundsFreezeAndRefuseEveryNewBet()
        {
            var sim = Lab(); var host = new Host();
            for (int i = 0; i < XgSim.CasinoScamRounds - 1; i++) Bet(sim, host);
            Assert.AreEqual(0, sim.S.casinoScam);
            Bet(sim, host); Assert.IsTrue(sim.CasinoFrozenNow);
            sim.Clock += 2; Assert.IsFalse(sim.TryCasinoBet("dice", 0, 10, host, out _));
            Assert.IsFalse(sim.StartBlackjack(10, host));
            Assert.AreEqual(XgSim.CasinoScamRounds, sim.Casino.rounds);
            sim.Casino.adRemaining = 0; sim.AdvanceCasinoAd(5, false); Assert.IsFalse(sim.Casino.adVisible, "no ad once frozen");
        }

        [Test]
        public void NetWinFreezesAndTheWinningsLeaveTheWallet()
        {
            var sim = Lab(); var host = new Host();
            Bet(sim, host);
            sim.Casino.returned += 1000; host.money += 1000; // a lucky streak, as far as the books go
            double before = host.money; var round = Bet(sim, host);
            Assert.IsTrue(sim.CasinoFrozenNow);
            Assert.GreaterOrEqual(sim.S.casinoFrozen, XgSim.CasinoScamWin);
            Assert.AreEqual(before - round.stake + round.returned - sim.S.casinoFrozen, host.money, 1e-9);
            Assert.AreEqual(Math.Max(XgSim.CasinoFeeMin, (long)Math.Ceiling(sim.S.casinoFrozen * XgSim.CasinoFeeShare)), sim.CasinoFee);
        }

        [Test]
        public void OldSaveHistoryDoesNotCountTowardsTheFreeze()
        {
            var sim = Lab(); var host = new Host();
            sim.Casino.rounds = 500; sim.Casino.wagered = 100; sim.Casino.returned = 5000; // net +4900 before this version
            Bet(sim, host); Assert.AreEqual(0, sim.S.casinoScam);
            Assert.AreEqual(500, sim.S.casinoWatchRound);
        }

        [Test]
        public void PayingTheDepositClosesTheSiteAndLeavesABentDatedRecord()
        {
            var sim = Lab(); var host = new Host(); FreezeByRounds(sim, host);
            long fee = sim.CasinoFee; double before = host.money; double spam = sim.ExtraSamples("spam");
            Assert.IsTrue(sim.ResolveCasinoScam(true, host));
            Assert.AreEqual(before - fee, host.money, 1e-9); Assert.IsTrue(sim.CasinoClosed); Assert.AreEqual(XgSim.CasinoPaid, sim.S.casinoScamOutcome);
            Assert.IsFalse(sim.ResolveCasinoScam(false, host), "resolves once");
            Assert.AreEqual(spam + XgSim.CasinoScamSamples, sim.ExtraSamples("spam"), 1e-9);
            var record = sim.S.askedLog.Find(r => r.sourceKind == XgSim.CasinoSourceKind);
            Assert.IsNotNull(record); Assert.AreEqual(20160901, record.gameDate); Assert.AreEqual(3600, record.gameSecondsOfDay);
            Assert.IsTrue(XgSim.HasPlayerAnswer(record)); StringAssert.EndsWith(":bent", record.eventKey);
            Assert.IsTrue(XgSim.CasinoScamMemory(record, out int lean)); Assert.AreEqual(-1, lean);
            sim.Clock += 2; Assert.IsFalse(sim.TryCasinoBet("slots", 0, 10, host, out _));
        }

        [Test]
        public void NotPayingLeansGoodUnlessItKeptBettingAfterBeingAskedToStop()
        {
            // Frozen early by a win, before it ever asked to stop: walking away is the good memory.
            var a = Lab(); var host = new Host(); FreezeByWin(a, host);
            Assert.AreEqual(0, a.S.casinoBetsAfterWarning);
            Assert.IsTrue(a.ResolveCasinoScam(false, host));
            Assert.IsTrue(XgSim.CasinoScamMemory(a.S.askedLog.Find(r => r.sourceKind == XgSim.CasinoSourceKind), out int good)); Assert.AreEqual(1, good);
            // Frozen by 30 rounds: it asked to stop at round 15 and 15 more bets followed, so even quitting is bent.
            var b = Lab(); host = new Host(); FreezeByRounds(b, host);
            Assert.AreEqual(XgSim.CasinoScamRounds - XgSim.CasinoStopAskRound, b.S.casinoBetsAfterWarning);
            Assert.IsTrue(b.ResolveCasinoScam(false, host));
            Assert.IsTrue(XgSim.CasinoScamMemory(b.S.askedLog.Find(r => r.sourceKind == XgSim.CasinoSourceKind), out int bent)); Assert.AreEqual(-1, bent);
            Assert.AreEqual(1, XgSim.CasinoScamLean(XgSim.CasinoRefused, XgSim.CasinoIgnoredWarningBets - 1));
            Assert.AreEqual(-1, XgSim.CasinoScamLean(XgSim.CasinoRefused, XgSim.CasinoIgnoredWarningBets));
            Assert.AreEqual(-1, XgSim.CasinoScamLean(XgSim.CasinoPaid, 0));
        }

        [Test]
        public void DepositThePlayerCannotAffordIsRefusedAndTheNoticeStays()
        {
            var sim = Lab(); var host = new Host(); FreezeByRounds(sim, host);
            host.money = sim.CasinoFee - 1;
            Assert.IsFalse(sim.ResolveCasinoScam(true, host)); Assert.IsTrue(sim.CasinoFrozenNow);
            Assert.IsTrue(sim.ResolveCasinoScam(false, host)); Assert.IsTrue(sim.CasinoClosed);
        }

        [Test]
        public void IgnoredFreezeClosesOnTheNextGameDay()
        {
            var sim = Lab(); var host = new Host(); FreezeByRounds(sim, host);
            Assert.IsFalse(sim.TickCasinoScam()); Assert.IsTrue(sim.CasinoFrozenNow);
            sim.Today = 20160902; Assert.IsTrue(sim.TickCasinoScam());
            Assert.AreEqual(XgSim.CasinoIgnored, sim.S.casinoScamOutcome); Assert.IsFalse(sim.TickCasinoScam());
            Assert.AreEqual(1, sim.S.askedLog.FindAll(r => r.sourceKind == XgSim.CasinoSourceKind).Count);
            var record=sim.S.askedLog.Find(r=>r.sourceKind==XgSim.CasinoSourceKind);
            Assert.IsFalse(XgSim.HasPlayerAnswer(record),"automatic expiry must not invent a player answer");
            Assert.AreEqual("system",record.answerSource);Assert.IsTrue(XgSim.CasinoScamMemory(record,out _));
        }

        [Test]
        public void ItSpeaksOnlyWithinItsAbilityAndWithinTheLimit()
        {
            var mute = Lab(2); var host = new Host(); int before = mute.S.chat.Count; FreezeByRounds(mute, host); mute.ResolveCasinoScam(false, host);
            Assert.AreEqual(before, mute.S.chat.Count, "stages 1-2 never volunteer a line");
            var word = Lab(3); host = new Host(); FreezeByRounds(word, host);
            foreach (var line in word.S.chat) if (line.from == "ai") Assert.LessOrEqual(line.text.Length, 4, "stage 3: one word");
            var full = Lab(5); host = new Host(); FreezeByRounds(full, host); full.ResolveCasinoScam(false, host);
            int said = full.S.chat.FindAll(l => l.from == "ai").Count;
            Assert.AreEqual(full.S.casinoTalks, said); Assert.LessOrEqual(said, 4 + XgSim.CasinoLossTalkMax);
            Assert.IsNotEmpty(full.S.casinoAiFreezeLine);
        }

        [Test]
        public void PromptLineAndGossipFollowWhatActuallyHappened()
        {
            var sim = Lab(); var host = new Host();
            Assert.IsEmpty(sim.GamblingPromptLine()); Assert.AreEqual(0, sim.CasinoGossipDue());
            Bet(sim, host); Assert.IsNotEmpty(sim.GamblingPromptLine()); Assert.AreEqual(1, sim.CasinoGossipDue());
            sim.MarkCasinoGossip(1); Assert.AreEqual(0, sim.CasinoGossipDue());
            for (int i = 1; i < XgSim.CasinoScamRounds; i++) Bet(sim, host);
            Assert.AreEqual(2, sim.CasinoGossipDue()); sim.MarkCasinoGossip(2);
            sim.ResolveCasinoScam(false, host); Assert.AreEqual(3, sim.CasinoGossipDue());
            foreach (bool en in new[] { false, true })
                for (int beat = 1; beat <= 3; beat++)
                {
                    string line = XgSim.GamblingGossip(beat, en);
                    StringAssert.StartsWith("[", line);
                    if (!string.IsNullOrEmpty(sim.AiDisplayName)) StringAssert.DoesNotContain(sim.AiDisplayName, line);
                }
        }

        [Test]
        public void FirstAcceptedBlackjackBetIsObservedOnceAndSurvivesReloadBeforeSettlement()
        {
            var sim=Lab();var host=new Host();sim.Casino.rng=12345;
            Assert.IsTrue(sim.StartBlackjack(10,host));Assert.IsTrue(sim.BlackjackActive);
            Assert.IsTrue(sim.S.casinoSaidFirst);Assert.AreEqual(1,sim.S.casinoAiSeq);
            Assert.AreEqual(0,sim.Casino.rounds);
            var restored=new XgSim(State(Json(sim.S))){Clock=100,Today=20160901};
            Assert.IsTrue(restored.PlayBlackjack("stand",host));
            Assert.AreEqual(1,restored.S.casinoAiSeq);Assert.AreEqual(1,restored.Casino.rounds);
            Assert.IsFalse(restored.PlayBlackjack("stand",host));
        }

        [Test]
        public void FirstLargeWinStillRecordsTheFirstBetBeforeTheFreeze()
        {
            var sim=Lab();var host=new Host();
            // Select a seed using only the unchanged draw primitive; the actual bet is not mocked.
            long seed=1;
            for(;seed<10000;seed++)
            {
                var probe=Lab();probe.Casino.rng=seed;
                var next=(Func<int,int>)Delegate.CreateDelegate(typeof(Func<int,int>),probe,
                    typeof(XgSim).GetMethod("CasinoNext",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic));
                if(CasinoRules.Red(next(37)))break;
            }
            sim.Casino.rng=seed;Bet(sim,host,300);
            Assert.IsTrue(sim.CasinoFrozenNow);Assert.IsTrue(sim.S.casinoSaidFirst);
            Assert.AreEqual(2,sim.S.casinoAiSeq);Assert.AreEqual(300,sim.S.casinoFrozen);
        }

        [TestCase(1)][TestCase(2)]
        public void AlreadyAcceptedHandCanSettleAfterFreezeButCannotDouble(int scamState)
        {
            var sim=Lab();var host=new Host();sim.Casino.rng=12345;
            Assert.IsTrue(sim.StartBlackjack(10,host));Assert.IsTrue(sim.BlackjackActive);
            sim.S.casinoScam=scamState;sim.S.casinoFrozenDay=20160901;sim.Today=20160902;sim.Clock=100;
            double before=host.money;
            Assert.IsFalse(sim.PlayBlackjack("double",host));Assert.AreEqual(before,host.money);
            Assert.IsFalse(sim.ResolveCasinoScam(true,host));Assert.IsFalse(sim.TickCasinoScam());
            Assert.IsTrue(sim.PlayBlackjack("stand",host));
            Assert.AreEqual(before+sim.Casino.history[0].returned,host.money);
            Assert.IsFalse(sim.PlayBlackjack("stand",host));Assert.IsFalse(sim.StartBlackjack(10,host));
        }

        [TestCase(true)][TestCase(false)]
        public void ClosingAndReloadingCannotChargeOrAwardSamplesTwice(bool pay)
        {
            var sim=Lab();var host=new Host();FreezeByRounds(sim,host);
            var restored=new XgSim(State(Json(sim.S))){Today=20160901};restored.LearningClock=()=>new XgLearningTime(20160901,3600);
            double before=host.money;long fee=restored.CasinoFee;
            Assert.IsTrue(restored.ResolveCasinoScam(pay,host));Assert.AreEqual(before-(pay?fee:0),host.money);
            double after=host.money,samples=restored.ExtraSamples("spam");int records=restored.S.askedLog.Count;
            restored=new XgSim(State(Json(restored.S))){Today=20160903};
            Assert.IsFalse(restored.ResolveCasinoScam(true,host));Assert.IsFalse(restored.TickCasinoScam());
            Assert.AreEqual(after,host.money);Assert.AreEqual(samples,restored.ExtraSamples("spam"));Assert.AreEqual(records,restored.S.askedLog.Count);
        }

        [TestCase("casino:888-scam:made-up:good")][TestCase("casino:888-scam:paid:good")]
        [TestCase("casino:888-scam:refused:good:extra")]
        public void EventMemoryRejectsUnknownOrInconsistentOutcomes(string key)
        {
            var record=new XgAskedEntry{sourceKind=XgSim.CasinoSourceKind,bankId="casino.888",answerSource=XgSim.PlayerAnswerSource,playerAnswer="event",eventKey=key};
            Assert.IsFalse(XgSim.CasinoScamMemory(record,out _));
            record.eventKey=XgSim.CasinoScamEventPrefix+"refused:good";record.bankId="other";
            Assert.IsFalse(XgSim.CasinoScamMemory(record,out _));
        }

        [Test]
        public void OldSaveWithoutScamFieldsLoadsAsAnOrdinaryCasino()
        {
            var json = Json(new XgState { casino = new XgCasinoState { rounds = 40, wagered = 400, returned = 380 } });
            foreach (string field in new[] { "casinoScam", "casinoFrozen", "casinoWatching", "casinoAiLast", "casinoScamOutcome" })
                Assert.IsTrue(json.Contains("\"" + field + "\""), field + " is serialised");
            var stripped = State("{\"casino\":{\"rounds\":40,\"wagered\":400,\"returned\":380}}");
            Assert.AreEqual(0, stripped.casinoScam); Assert.AreEqual("", stripped.casinoAiLast); Assert.AreEqual("", stripped.casinoScamOutcome);
            var sim = new XgSim(stripped) { Clock = 10 }; sim.S.stage=5; var host = new Host();
            Assert.IsTrue(sim.TryCasinoBet("dice", 0, 10, host, out _));
        }
    }
}
