using System;
using System.Linq;
using LingGuangV05.Core.Chat;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>Expansion changes review storage, not inference concurrency. Uses the real purchase/routing/save-repair paths.</summary>
    public sealed class XgReviewCapacityTests
    {
        sealed class Host : IXgHost
        {
            public double money = 10000;
            public int spends;
            public double Money => money;
            public double Compute => 1;
            public double VramMB => 16000;
            public string Blocker => null;
            public bool Spend(double amount) { spends++; if (money < amount) return false; money -= amount; return true; }
            public void Earn(double amount) { money += amount; }
            public void Train(double seconds) { }
        }

        static XgSim Eligible()
        {
            var sim = new XgSim { GoldChance = 0, UseBoard = false };
            sim.S.stage = 4;
            sim.S.autoLevel = 1;
            sim.S.unlocked.Add("label.coop");
            sim.S.unlocked.Add("label.brain");
            return sim;
        }

        static XgCard Waiting(XgSim sim)
        {
            var card = sim.CreateLabelCard("spam");
            card.hasJudgment = true; card.confidence = .5; card.guess = card.truth; card.judgeSource = "checkpoint";
            sim.S.prefetch.Add(card);
            return card;
        }

        static void Fill(XgSim sim, Host host, int count)
        {
            for (int i = 0; i < count; i++) Assert.IsTrue(sim.Route(Waiting(sim).id, host), "route " + i);
        }

        static XgSim Reload(XgSim sim) => new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));

        [Test]
        public void RealPurchaseCosts3000AndImmediatelyUnblocksCard21ThenStopsAt60()
        {
            var sim = Eligible(); var host = new Host();
            Assert.AreEqual(20, sim.ReviewLimit);
            Fill(sim, host, 20);
            Assert.IsTrue(sim.ReviewFull);
            var blocked = Waiting(sim);
            Assert.IsFalse(sim.Route(blocked.id, host));
            CollectionAssert.Contains(sim.S.prefetch, blocked, "blocked work must not disappear");
            double before = host.money;
            Assert.IsTrue(sim.BuyNode("label.parallel", host), "real node purchase, not injected entitlement");
            Assert.AreEqual(3000, before - host.money, 1e-9);
            Assert.AreEqual(1, host.spends);
            Assert.AreEqual(60, sim.ReviewLimit);
            Assert.IsFalse(sim.ReviewFull);
            Assert.IsTrue(sim.Route(blocked.id, host), "the same waiting card resumes");
            Assert.AreSame(blocked, sim.S.queue[20]);
            Fill(sim, host, 39);
            Assert.AreEqual(60, sim.ReviewCount());
            Assert.IsTrue(sim.ReviewFull);
            var extra = Waiting(sim);
            Assert.IsFalse(sim.Route(extra.id, host), "card 61 must stay outside the review inbox");
            CollectionAssert.Contains(sim.S.prefetch, extra);
            CollectionAssert.DoesNotContain(sim.S.queue, extra);
            Assert.AreEqual(60, sim.ReviewCount());
            double paid = host.money;
            Assert.IsFalse(sim.BuyNode("label.parallel", host), "cannot purchase an owned node twice");
            Assert.AreEqual(paid, host.money);
            Assert.AreEqual(1, host.spends);
        }

        [Test]
        public void InsufficientFundsDoNotGrantCapacityOrMoveWaitingCards()
        {
            var sim = Eligible(); var host = new Host { money = 2999 };
            Fill(sim, host, 20); var waiting = Waiting(sim);
            Assert.IsFalse(sim.BuyNode("label.parallel", host));
            Assert.AreEqual(2999, host.money);
            Assert.AreEqual(20, sim.ReviewLimit);
            Assert.IsTrue(sim.ReviewFull);
            Assert.IsFalse(sim.Route(waiting.id, host));
            CollectionAssert.Contains(sim.S.prefetch, waiting);
        }

        [Test]
        public void OwnedLegacyJsonKeepsAll60CardIdsOrderAndPayloads()
        {
            var sim = Eligible(); var host = new Host();
            // Existing saves already have the old node id; no new capacity field is required.
            sim.S.unlocked.Add("label.parallel"); Fill(sim, host, 60);
            var ids = sim.S.queue.Select(card => card.id).ToArray();
            var payloads = sim.S.queue.Select(card => card.question + "|" + card.truth).ToArray();
            var loaded = Reload(sim);
            Assert.AreEqual(60, loaded.ReviewLimit);
            Assert.AreEqual(60, loaded.ReviewCount());
            Assert.IsTrue(loaded.ReviewFull);
            CollectionAssert.AreEqual(ids, loaded.S.queue.Select(card => card.id).ToArray());
            CollectionAssert.AreEqual(payloads, loaded.S.queue.Select(card => card.question + "|" + card.truth).ToArray());
            Assert.AreEqual(60, loaded.S.queue.Select(card => card.id).Distinct().Count());
        }

        [Test]
        public void UnownedLegacyOverflowMovesAfterExistingPrefetchWithoutLoss()
        {
            var sim = Eligible();
            for (int i = 0; i < 30; i++) sim.S.queue.Add(sim.CreateLabelCard("spam"));
            for (int i = 0; i < 3; i++) Waiting(sim);
            var queued = sim.S.queue.Select(card => card.id).ToArray();
            var prefetched = sim.S.prefetch.Select(card => card.id).ToArray();
            var all = queued.Concat(prefetched).OrderBy(id => id).ToArray();
            var loaded = Reload(sim);
            Assert.AreEqual(20, loaded.ReviewLimit);
            CollectionAssert.AreEqual(queued.Take(20).ToArray(), loaded.S.queue.Select(card => card.id).ToArray());
            CollectionAssert.AreEqual(prefetched.Concat(queued.Skip(20)).ToArray(), loaded.S.prefetch.Select(card => card.id).ToArray());
            CollectionAssert.AreEqual(all, loaded.S.queue.Concat(loaded.S.prefetch).Select(card => card.id).OrderBy(id => id).ToArray());
            Assert.AreEqual(33, loaded.S.queue.Concat(loaded.S.prefetch).Select(card => card.id).Distinct().Count());
        }

        [Test]
        public void NoiseRecheckUsesOnlyTheFiveFreeExpandedSlots()
        {
            var sim = Eligible(); var host = new Host();
            Assert.IsTrue(sim.BuyNode("label.parallel", host)); Fill(sim, host, 55);
            sim.S.noise.Add(new XgLabelCount { dataset = "spam", count = 10 });
            Assert.AreEqual(5, sim.RecheckNoise("spam"));
            Assert.AreEqual(60, sim.ReviewCount());
            Assert.IsTrue(sim.ReviewFull);
            Assert.AreEqual(0, sim.RecheckNoise("spam"));
        }

        [TestCase(0, 0, false, true)]
        [TestCase(1, 0, false, false)]
        [TestCase(0, 1, false, false)]
        [TestCase(0, 0, true, false)]
        [TestCase(1, 1, true, false)]
        public void ChatAndTypingTakePriorityOverBackgroundJudging(int queued, int active, bool typing, bool expected)
        {
            Assert.AreEqual(expected, LlmScheduling.BackgroundMayStart(queued, active, typing));
        }

        [Test]
        public void OneSlotPolicyNeverAdmitsASecondRequestForAnySeatOrLane()
        {
            // Pure one-slot policy. Actual multi-slot LocalLlm.Pump's active/native-call total guard
            // lives in Assembly-CSharp and requires its separate integration verification.
            foreach (LlmSeat seat in Enum.GetValues(typeof(LlmSeat)))
                foreach (LlmLane lane in Enum.GetValues(typeof(LlmLane)))
                {
                    Assert.AreEqual(LlmScheduling.ServerChoice, LlmScheduling.PickSlot(seat, lane, 1, false, 0, _ => false));
                    Assert.AreEqual(LlmScheduling.Wait, LlmScheduling.PickSlot(seat, lane, 1, false, 1, _ => false));
                    Assert.AreEqual(LlmScheduling.Wait, LlmScheduling.PickSlot(seat, lane, 1, false, 2, _ => false));
                }
            Assert.AreEqual(1, LlmScheduling.JudgeLimit(1, false));
        }
    }
}
