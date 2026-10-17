using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The first five minutes: the starting desks and training are open until the yes/no is learned, then everything; old saves never lock; the before/after pairs.</summary>
    public sealed class XgOpeningTests
    {
        sealed class Host : IXgHost
        {
            public double money, compute = 50;
            public double Money => money; public double Compute => compute; public double VramMB => 1e5; public string Blocker => null;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
        }

        /// <summary>Open with 常识判断 from the first second (2026-10-18: the opening is a run of games and combos). 算术 waits for stage two since the 2026-10-15 integration.</summary>
        static readonly string[] Locked = { "logic", "spam", "mnist" };

        static XgSim Fresh()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.BeginOpening();
            return sim;
        }

        static void LabelSense(XgSim sim, Host host, int count)
        {
            for (int i = 0; i < count; i++) { var c = sim.Card(XgMemes.SenseDesk); sim.Answer(XgMemes.SenseDesk, c.truth, host); }
        }

        /// <summary>Labels and trains on the sense desk like a player until the yes/no is learned; returns the rounds it took.</summary>
        static int PlayUntilLearned(XgSim sim, Host host, int maxRounds = 40)
        {
            for (int round = 1; round <= maxRounds; round++)
            {
                LabelSense(sim, host, 10);
                sim.TrainEpoch(XgTrack.Sequence, host);
                if (!sim.OpeningFocus) return round;
            }
            return -1;
        }

        [Test]
        public void AFreshOpeningHasTheStartingDesksAndTheirGamesOpen()
        {
            var sim = Fresh();
            Assert.IsTrue(sim.OpeningFocus);
            Assert.IsTrue(sim.DeskOpen(XgMemes.SenseDesk));
            foreach (var id in Locked) Assert.IsTrue(sim.DeskOpen(id), id + " is open at once");
            Assert.AreEqual(4, sim.OpenDesks().Count);
            foreach (var desk in sim.OpenDesks())
            {
                string game = desk.id == "mnist" ? "draw" : XgMiniGames.ForDesk(desk.id); // 手写数字 opens the handwriting pad
                Assert.IsNotNull(game, desk.id + " opens as a game");
                Assert.IsNull(sim.MiniGameBlocker(game), game + " can be played now");
            }
            Assert.IsFalse(sim.DeskOpen("arith"), "算术 still waits for stage two");
            Assert.AreNotEqual(sim.OpeningReason, sim.DeskConditionText(XgCatalog.Desk("arith")), "and says its own reason");
            Assert.AreEqual(XgMemes.SenseDesk, sim.S.desk);
            Assert.AreEqual(XgMemes.SenseDesk, sim.Run(XgTrack.Sequence).dataset);
            StringAssert.Contains("分辨对错", sim.OpeningReason);
            foreach (var feature in new[] { "tree", "contracts", "repo", "board", "cards", "wiring", "coop", "attention", "final", "project" })
                Assert.IsFalse(sim.FeatureVisible(feature), feature);
            foreach (var tab in new[] { "board", "tree", "data", "items", "wiring", "repo", "abilities", "cards", "final", "games", "contracts" })
                Assert.IsTrue(sim.OpeningLocksLabTab(tab), tab);
            foreach (var tab in new[] { "teach", "train", "vision", "sequence", "home", "" }) Assert.IsFalse(sim.OpeningLocksLabTab(tab), tab);
            Assert.IsFalse(sim.OpeningLocksCrowdTab("label"));
            foreach (var tab in new[] { "games", "contracts", "subcontract", "ledger", "credit" }) Assert.IsTrue(sim.OpeningLocksCrowdTab(tab), tab);
            Assert.IsFalse(XgGuide.TabOpen(sim, XgGuide.Lab, "tree"));
            Assert.IsFalse(XgGuide.TabOpen(sim, XgGuide.Crowd, "subcontract"));
            Assert.IsTrue(XgGuide.TabOpen(sim, XgGuide.Crowd, "label"));
        }

        [Test]
        public void TheOpeningStateSurvivesASaveAndReload()
        {
            var sim = Fresh();
            var host = new Host();
            LabelSense(sim, host, 6);
            var back = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S))) { GoldChance = 0 };
            Assert.IsTrue(back.OpeningFocus);
            foreach (var id in Locked) Assert.IsTrue(back.DeskOpen(id), id + " stays open after a reload");
            Assert.IsTrue(back.DeskOpen(XgMemes.SenseDesk));
            Assert.AreEqual(XgMemes.SenseDesk, back.S.desk);
        }

        [Test]
        public void TheNoteSaysOnlyLabelThenTrainThenChat()
        {
            var sim = Fresh(); var host = new Host();
            var steps = XgGuide.Current(sim, 0);
            Assert.AreEqual(1, steps.Count, "one lonely line, no side task");
            Assert.AreEqual("opening.label", steps[0].id);
            Assert.AreEqual(XgGuide.Crowd, steps[0].app); Assert.AreEqual("label", steps[0].tab); Assert.AreEqual(XgMemes.SenseDesk, steps[0].arg);
            StringAssert.Contains("常识判断", steps[0].zh);
            Assert.AreEqual(XgCatalog.SamplesToTrain, steps[0].goal);

            LabelSense(sim, host, XgCatalog.SamplesToTrain);
            Assert.IsTrue(sim.TrainingUnlocked(XgTrack.Sequence));
            steps = XgGuide.Current(sim, host.money);
            Assert.AreEqual("opening.train", steps[0].id);
            Assert.AreEqual(XgGuide.Lab, steps[0].app); Assert.AreEqual("train", steps[0].tab);
            StringAssert.StartsWith("label:训练一轮", steps[0].target);
            foreach (var s in steps)
            {
                Assert.IsTrue(XgGuide.TabOpen(sim, s.app, s.tab), s.id + " points at an open page");
                Assert.AreNotEqual(XgGuideKind.Side, s.kind);
                Assert.IsNotEmpty(s.zh); Assert.IsNotEmpty(s.en); Assert.IsNotEmpty(s.whyZh); Assert.IsNotEmpty(s.whyEn);
            }

            Assert.Greater(PlayUntilLearned(sim, host), 0, "it learns on the sense desk alone");
            steps = XgGuide.Current(sim, host.money);
            Assert.AreEqual("opening.chat", steps[0].id, "after learning the note says to chat with it");
            Assert.AreEqual(XgGuide.YY, steps[0].app);
            sim.AddLine("me", "冰是烫的吗？");
            Assert.IsFalse(sim.OpeningChatDue);
            Assert.IsNull(XgGuide.Current(sim, host.money).Find(x => x.id == "opening.chat"));
        }

        [Test]
        public void LearningOpensEverythingOnceAndSaysSo()
        {
            var sim = Fresh(); var host = new Host();
            int learned = 0; var said = new List<string>();
            sim.YesNoLearned += () => learned++;
            sim.Message += m => said.Add(m);
            int rounds = PlayUntilLearned(sim, host);
            Assert.Greater(rounds, 0, "learned within 40 rounds of labelling and training");
            Debug.Log("[XgOpening] learned after " + rounds + " rounds; labels " + sim.Labels(XgMemes.SenseDesk) + ", accuracy " + sim.YesNoAccuracy.ToString("0.00"));
            Assert.AreEqual(1, learned);
            Assert.IsFalse(sim.OpeningFocus);
            Assert.GreaterOrEqual(sim.YesNoAccuracy, XgSim.SenseVoiceLearned);
            foreach (var id in Locked) Assert.IsTrue(sim.DeskOpen(id), id + " opens with it");
            Assert.IsTrue(sim.FeatureVisible("tree"));
            Assert.IsFalse(sim.OpeningLocksLabTab("tree"));
            Assert.IsTrue(sim.OpeningChatDue);
            Assert.IsTrue(said.Exists(m => m.Contains("它学会分辨对错了")), "the notice names it");
            Assert.IsTrue(said.Exists(m => m.Contains("以前答")), "with its before/after");
            // Once only: more labels and training never fire it again, and the focus never comes back.
            LabelSense(sim, host, 10); sim.TrainEpoch(XgTrack.Sequence, host);
            Assert.AreEqual(1, learned);
            Assert.IsFalse(sim.OpeningFocus);
        }

        [Test]
        public void TheOpeningComboWindowIsLongerAndEndsWithTheFocus()
        {
            var sim = Fresh(); var host = new Host();
            var c = sim.Card(XgMemes.SenseDesk); sim.Answer(XgMemes.SenseDesk, c.truth, host);
            double window = XgCatalog.Desk(XgMemes.SenseDesk).comboWindow;
            Assert.AreEqual(window * XgSim.OpeningComboWindow, sim.S.comboTimer, 1e-6, "a new player reading a card keeps the streak");
            var old = new XgSim { GoldChance = 0 };
            c = old.Card(XgMemes.SenseDesk); old.Answer(XgMemes.SenseDesk, c.truth, host);
            Assert.AreEqual(window, old.S.comboTimer, 1e-6, "outside the opening the window is the desk's own");
        }

        [Test]
        public void TheAutoLabelIdeaWaitsForTheOpeningToEnd()
        {
            var sim = Fresh(); var host = new Host();
            sim.S.teachMode = false;
            // A strong sense checkpoint: while the focus is on, no ghost guess is shown and nothing is counted.
            sim.S.best.Add(new XgBest { dataset = XgMemes.SenseDesk, acc = .7 });
            Assert.IsFalse(sim.GhostGuess(XgMemes.SenseDesk, out _, out _));
            Assert.IsFalse(sim.EpiphanyDone);
        }

        [Test]
        public void OldSavesAreNeverLockedOut()
        {
            // A save from before the focus existed: the flag is off and every desk is as it was.
            var old = new XgSim { GoldChance = 0 };
            Assert.IsFalse(old.OpeningFocus);
            foreach (var id in Locked) Assert.IsTrue(old.DeskOpen(id), id);
            Assert.IsTrue(old.FeatureVisible("teach"));
            Assert.IsFalse(old.OpeningLocksLabTab("tree"));
            Assert.IsFalse(old.OpeningLocksCrowdTab("subcontract"));
            old.BeginOpening();
            Assert.IsTrue(old.OpeningFocus, "a brand-new game does start it");

            // Any ability, any trained run, any checkpoint or a label on another desk: not a new game.
            var staged = new XgSim { GoldChance = 0 }; staged.S.stage = 2; staged.BeginOpening();
            Assert.IsFalse(staged.OpeningFocus, "stage 2");
            var ability = new XgSim { GoldChance = 0 }; ability.S.abilitiesEmerged.Add(2); ability.BeginOpening();
            Assert.IsFalse(ability.OpeningFocus, "an ability");
            var trained = new XgSim { GoldChance = 0 }; trained.S.epochs = 3; trained.BeginOpening();
            Assert.IsFalse(trained.OpeningFocus, "a trained run");
            var run = new XgSim { GoldChance = 0 }; run.Run(XgTrack.Sequence).epoch = 2; run.BeginOpening();
            Assert.IsFalse(run.OpeningFocus, "a run with epochs");
            var scored = new XgSim { GoldChance = 0 }; scored.S.best.Add(new XgBest { dataset = "spam", acc = .6 }); scored.BeginOpening();
            Assert.IsFalse(scored.OpeningFocus, "a checkpoint");
            var labelled = new XgSim { GoldChance = 0 }; labelled.S.labels.Add(new XgLabelCount { dataset = "logic", count = 30 }); labelled.BeginOpening();
            Assert.IsFalse(labelled.OpeningFocus, "labels elsewhere");
            foreach (var id in Locked) Assert.IsTrue(labelled.DeskOpen(id));

            // A focus flag on a save that has since moved past stage 1 (a jump from outside) ends silently at the next check.
            var jumped = Fresh(); var host = new Host();
            var said = new List<string>(); jumped.Message += m => said.Add(m);
            jumped.S.stage = 3;
            var card = jumped.Card(XgMemes.SenseDesk); jumped.Answer(XgMemes.SenseDesk, card.truth, host);
            Assert.IsFalse(jumped.OpeningFocus);
            foreach (var id in Locked) Assert.IsTrue(jumped.DeskOpen(id), id);
            Assert.IsFalse(said.Exists(m => m.Contains("它学会分辨对错了")), "no learned notice for a jump");
            Assert.IsFalse(jumped.OpeningChatDue);
        }

        [Test]
        public void TheRentHolidayRuleIsUntouched()
        {
            Assert.AreEqual(3, XgSim.RentFromAbility);
            var sim = Fresh();
            Assert.Less(sim.AbilitiesCount, XgSim.RentFromAbility);
        }

        [Test]
        public void EveryAbilityHasABeforeAndAfterThatDiffers()
        {
            foreach (var english in new[] { false, true })
                for (int ability = 1; ability <= XgSim.AbilityCount; ability++)
                {
                    var sim = new XgSim { English = english };
                    var pair = sim.BeforeAfter(ability);
                    Assert.IsNotEmpty(pair.Question(english), ability + " question"); Assert.IsNotEmpty(pair.Before(english), ability + " before"); Assert.IsNotEmpty(pair.After(english), ability + " after");
                    Assert.AreNotEqual(pair.Before(english), pair.After(english), ability + " differs (english " + english + ")");
                    Assert.IsFalse(pair.Question(english).Contains("涌现"));
                    Assert.IsFalse(pair.After(english).Contains("涌现"));
                }
            var vision = new XgSim { }; vision.S.firstTrack = "vision";
            var seq = new XgSim { }; seq.S.firstTrack = "sequence";
            Assert.AreNotEqual(vision.BeforeAfter(3).Question(false), seq.BeforeAfter(3).Question(false), "ability 3 follows the track it chose");
        }

        [Test]
        public void TheYesNoPairIsATaughtStatementWithTheRightNewAnswer()
        {
            var sim = new XgSim();
            var pair = sim.BeforeAfter(1);
            bool? taught = XgSim.TaughtAnswer(pair.Question(false));
            Assert.IsTrue(taught.HasValue, "the question is a statement from the bank");
            Assert.AreEqual(taught.Value ? "是。" : "否。", pair.After(false));
            Assert.AreEqual(taught.Value ? "No." : "Yes.", pair.Before(true), "the old answer is the flipped one");
            Assert.IsFalse(pair.real);
            // Same save, same pair: the toast and any later reader agree.
            Assert.AreEqual(pair.Question(false), sim.BeforeAfter(1).Question(false));
        }

        [Test]
        public void TheYesNoPairPrefersAnAnswerItReallyGotWrong()
        {
            var sim = new XgSim();
            var wrong = XgMemes.SenseBank[0];
            sim.S.chat.Add(new XgChatLine { from = "me", text = wrong.text });
            sim.S.chat.Add(new XgChatLine { from = "ai", text = wrong.yes ? "否。" : "是。" });
            var pair = sim.BeforeAfter(1);
            Assert.IsTrue(pair.real);
            Assert.AreEqual(wrong.text, pair.Question(false));
            Assert.AreEqual(wrong.yes ? "否。" : "是。", pair.Before(false), "what it really said");
            Assert.AreEqual(wrong.yes ? "是。" : "否。", pair.After(false));
            // An exchange it got right is not a before/after.
            var right = new XgSim();
            right.S.chat.Add(new XgChatLine { from = "me", text = wrong.text });
            right.S.chat.Add(new XgChatLine { from = "ai", text = wrong.yes ? "是。" : "否。" });
            Assert.IsFalse(right.BeforeAfter(1).real);
        }

        [Test]
        public void ThePickOnePairOffersTheAnswerAsAnOption()
        {
            var sim = new XgSim();
            var pair = sim.BeforeAfter(2);
            var options = XgSpeechPolicy.Options(pair.Question(false), 2, false);
            Assert.Greater(options.Count, 1, "the question offers a choice");
            Assert.IsTrue(options.Exists(o => XgSpeechPolicy.Answer(o, false) == pair.After(false)), "the new answer is one of them");
            var english = XgSpeechPolicy.Options(pair.Question(true), 2, true);
            Assert.IsTrue(english.Exists(o => XgSpeechPolicy.Answer(o, true) == pair.After(true)));
        }

        [Test]
        public void TheMemoryPairQuotesWhatThePlayerSaid()
        {
            var sim = new XgSim();
            sim.S.chat.Add(new XgChatLine { from = "me", text = "我明天去看你" });
            var pair = sim.BeforeAfter(5);
            StringAssert.Contains("我明天去看你", pair.After(false));
            Assert.IsTrue(pair.real);
        }
    }
}
