using NUnit.Framework;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgMiniIntegrationTests
    {
        sealed class Host : IXgHost
        {
            public double Money { get; private set; } = 10000;
            public double Compute => 10;
            public double VramMB => 4096;
            public string Blocker { get; set; }
            public double energy;
            public void Earn(double amount) { Money += amount; }
            public bool Spend(double amount) { if (amount < 0 || Money < amount) return false; Money -= amount; return true; }
            public void Train(double seconds) { energy += seconds; }
        }
        [Test] public void CatalogCoversExactlyTwentyThreeUniqueNativeGames()
        {
            Assert.That(XgMiniGames.All.Count, Is.EqualTo(23));
            var ids = new HashSet<string>(); foreach (var g in XgMiniGames.All) Assert.IsTrue(ids.Add(g.Id));
            // Every mapped game labels the desk it sits on; the digits desk has its own writing view and long sentences keep their cards.
            foreach (var d in XgCatalog.Desks)
            {
                string id = XgMiniGames.ForDesk(d.id);
                if (id == null) continue;
                var def = XgMiniGames.Find(id);
                Assert.NotNull(def, d.id); Assert.AreEqual(d.id, def.Desk, id); Assert.AreEqual(XgMiniReward.Label, def.Reward, id);
            }
            Assert.IsNull(XgMiniGames.ForDesk("mnist")); Assert.IsNull(XgMiniGames.ForDesk("longtext"));
        }
        [Test] public void SubmissionUsesWalletAndDatasetButNeverAnswersTheExistingCard()
        {
            var s = new XgSim(); var h = new Host(); var card = s.Card("spam");
            var t = s.BeginMiniGame("slice", h); double rows = s.Labels("spam"), money = h.Money;
            var r = s.SubmitMiniGame(t, 1, true, h);
            Assert.IsTrue(r.accepted); Assert.AreEqual(rows + 1, s.Labels("spam")); Assert.Greater(h.Money, money);
            Assert.AreEqual(card.id, s.Card("spam").id); Assert.AreEqual(1, s.MiniGameRecord("slice").samples);
        }
        [Test] public void RepeatedGappedAndCancelledResultsNeverPay()
        {
            var s = new XgSim(); var h = new Host(); var t = s.BeginMiniGame("slice", h);
            Assert.IsTrue(s.SubmitMiniGame(t, 1, true, h).accepted); var money = h.Money;
            Assert.IsFalse(s.SubmitMiniGame(t, 1, true, h).accepted);
            Assert.IsFalse(s.SubmitMiniGame(t, 3, true, h).accepted);
            s.CancelMiniGame(t); Assert.IsFalse(s.SubmitMiniGame(t, 2, true, h).accepted);
            Assert.AreEqual(money, h.Money);
        }
        [Test] public void WrongLabelsDoNotPolluteDataAndAutomaticFreezeDoesNotBlockManualWork()
        {
            var s = new XgSim(); var h = new Host(); s.S.qcFrozen = 100;
            long t = s.BeginMiniGame("slice", h); double before = s.Labels("spam"), money = h.Money;
            var wrong = s.SubmitMiniGame(t, 1, false, h);
            Assert.IsTrue(wrong.accepted);
            Assert.AreEqual(before, s.Labels("spam")); Assert.AreEqual(money - wrong.fine, h.Money, 1e-9);
            Assert.IsTrue(s.SubmitMiniGame(t, 2, true, h).accepted);
        }
        #if UNITY_5_3_OR_NEWER
        [Test] public void SavedCountersRoundTripButSessionDoesNot()
        {
            var s = new XgSim(); var h = new Host(); long t = s.BeginMiniGame("slice", h);
            s.SubmitMiniGame(t, 1, true, h);
            var state = UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(s.S));
            var loaded = new XgSim(state);
            Assert.AreEqual(1, loaded.MiniGameRecord("slice").samples);
            Assert.IsFalse(loaded.SubmitMiniGame(t, 2, true, h).accepted);
            loaded.S.miniGames = null; Assert.NotNull(loaded.MiniGameRecord("slice"));
        }
        #endif
        [Test] public void ForgeStartsNormalTimedEpochAndHonorsPowerBlocker()
        {
            var s = new XgSim(); var h = new Host();
            s.S.desksOpen.Add("mnist"); s.S.labels.Add(new XgLabelCount { dataset = "mnist", count = 20 });
            s.SelectedTrack = XgTrack.Vision;
            long t = s.BeginMiniGame("forgetax", h); double labels = s.TotalLabels;
            h.Blocker = "停电";
            Assert.IsFalse(s.SubmitMiniGame(t, 1, true, h).trainingStarted);
            h.Blocker = null; var r = s.SubmitMiniGame(t, 2, true, h);
            Assert.IsTrue(r.trainingStarted); Assert.Zero(s.S.epochs); Assert.Zero(h.energy);
            Assert.AreEqual(labels, s.TotalLabels);
            Assert.IsFalse(s.SubmitMiniGame(t, 3, true, h).trainingStarted);
        }
        [Test] public void LockedDesksAndLateStoryCannotBeStartedById()
        {
            var s = new XgSim(); var h = new Host();
            foreach (string id in new[] { "unknown", "esp", "turing", "qingwen", "spidersnake" })
                Assert.AreEqual(0, s.BeginMiniGame(id, h), id);
        }
    }
}
