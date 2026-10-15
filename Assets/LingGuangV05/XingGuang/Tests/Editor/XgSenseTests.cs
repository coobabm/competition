using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LingGuangV05.Core.Era;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// The 常识判断 desk and what hangs on it: the bank, the desk open from the first minute, the stage-1 gate that now
    /// counts it, and the inner-voice lines about its yes/no (each once per save, only at stage 1 while it is still at chance).
    /// </summary>
    public sealed class XgSenseTests
    {
        sealed class Host : IXgHost
        {
            public double money = 1000;
            public double Money => money; public double Compute => 50; public double VramMB => 1e5; public string Blocker => null;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
        }

        sealed class FixedRandom : Random
        {
            readonly double value;
            public FixedRandom(double value) { this.value = value; }
            public override double NextDouble() => value;
        }

        static string ToJson(XgState state)
        {
#if UNITY_5_3_OR_NEWER
            return UnityEngine.JsonUtility.ToJson(state);
#else
            return System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
#endif
        }

        static XgState FromJson(string json)
        {
#if UNITY_5_3_OR_NEWER
            return UnityEngine.JsonUtility.FromJson<XgState>(json);
#else
            return System.Text.Json.JsonSerializer.Deserialize<XgState>(json, new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
#endif
        }

        static void Train(XgSim sim, string dataset, double accuracy)
        {
            sim.S.best.RemoveAll(b => b.dataset == dataset);
            sim.S.best.Add(new XgBest { dataset = dataset, arch = "perceptron", acc = accuracy });
        }

        static XgSim StageOne() { var sim = new XgSim { GoldChance = 0 }; sim.S.stage = 1; return sim; }

        // ───────────── the bank ─────────────

        [Test]
        public void TheBankIsBalancedAndEveryItemIsPaired()
        {
            var bank = XgMemes.SenseBank;
            Assert.GreaterOrEqual(bank.Length, 80);
            int yes = 0, no = 0;
            var seen = new HashSet<string>();
            var perLevel = new int[XgMemes.MaxLevel + 1];
            foreach (var p in bank)
            {
                string where = p.text;
                if (p.yes) yes++; else no++;
                Assert.IsFalse(string.IsNullOrWhiteSpace(p.text) || string.IsNullOrWhiteSpace(p.textEn), where + ": a zh+en pair");
                Assert.IsFalse(string.IsNullOrWhiteSpace(p.why) || string.IsNullOrWhiteSpace(p.whyEn), where + ": a reason in both languages");
                Assert.AreNotEqual(p.text, p.textEn, where + ": the English is not the Chinese pasted twice");
                Assert.IsTrue(seen.Add(p.text), where + ": listed twice");
                Assert.IsFalse(p.trick, where + ": no trick questions");
                // Some statements are news of 2016 and only appear from their date on; none may need a date after the game's last day.
                Assert.LessOrEqual(p.since, 20161231, where + ": nothing that needs a later date");
                Assert.IsTrue(p.level >= 1 && p.level <= XgMemes.MaxLevel, where);
                perLevel[p.level]++;
                Assert.AreEqual(p.text, EraLexicon.Scrub(p.text), where + ": nothing from after 2016");
                Assert.AreEqual(p.textEn, EraLexicon.Scrub(p.textEn), where);
                foreach (var s in new[] { p.text, p.textEn, p.why, p.whyEn }) StringAssert.DoesNotContain("涌现", s);
            }
            Assert.AreEqual(yes, no, "as many 是 as 否");
            for (int level = 1; level <= XgMemes.MaxLevel; level++)
            {
                Assert.GreaterOrEqual(perLevel[level], 20, "level " + level);
                int y = 0, n = 0;
                foreach (var p in bank) if (p.level == level) { if (p.yes) y++; else n++; }
                Assert.AreEqual(y, n, "level " + level + " is balanced too");
            }
        }

        [Test]
        public void NewsOfTheYearIsDrawnOnlyFromItsDate()
        {
            var dated = new List<XgPhrase>();
            foreach (var p in XgMemes.SenseBank) if (p.since > 20160101) dated.Add(p); // undated statements carry the default 2015 date
            Assert.GreaterOrEqual(dated.Count, 2, "the bank carries some 2016 news");
            var rng = new Random(7);
            foreach (var news in dated)
            {
                bool seenBefore = false, seenFrom = false;
                for (int i = 0; i < 3000; i++)
                {
                    var early = XgMemes.Pick(XgMemes.SenseDesk, rng, XgMemes.MaxLevel, news.since - 1, "");
                    if (early.since > news.since - 1) seenBefore = true;
                    if (XgMemes.Pick(XgMemes.SenseDesk, rng, XgMemes.MaxLevel, news.since, "") == news) seenFrom = true;
                }
                Assert.IsFalse(seenBefore, news.text + ": drawn before its date");
                Assert.IsTrue(seenFrom, news.text + ": never drawn on its date");
            }
        }

        [Test]
        public void GeneratedCardsComeFromTheBankWithTheirAnswerAndBothLanguages()
        {
            var sim = new XgSim { GoldChance = 0 };
            var byText = new Dictionary<string, XgPhrase>();
            foreach (var p in XgMemes.SenseBank) byText[p.text] = p;
            int yes = 0;
            for (int i = 0; i < 400; i++)
            {
                var c = sim.CreateLabelCard("sense");
                Assert.AreEqual("sense", c.dataset);
                Assert.IsTrue(byText.TryGetValue(c.question, out var p), c.question);
                Assert.AreEqual(p.yes, c.truth); Assert.AreEqual(p.textEn, c.questionEn);
                Assert.AreEqual(p.why, c.why); Assert.AreEqual(p.whyEn, c.whyEn);
                Assert.IsFalse(c.trick);
                if (c.truth) yes++;
            }
            Assert.AreEqual(.5, yes / 400.0, .1, "about half are 是");
        }

        // ───────────── the desk ─────────────

        [Test]
        public void TheDeskIsOpenFromTheStartAndPaysLikeTheCheapDesks()
        {
            var sim = new XgSim { GoldChance = 0 };
            var desk = XgCatalog.Desk("sense");
            Assert.IsNotNull(desk); Assert.AreEqual("常识判断", desk.name); Assert.AreEqual("Common sense", desk.nameEn);
            Assert.AreEqual(XgDeskUnlock.Start, desk.unlock);
            Assert.AreEqual(1, desk.pay, 1e-9); Assert.AreEqual(0, desk.fine, 1e-9, "only 算术 has its own fine");
            Assert.IsTrue(sim.DeskOpen("sense")); Assert.IsTrue(sim.ProgressionDeskAvailable("sense")); Assert.IsTrue(sim.DatasetAvailable("sense"));
            Assert.IsTrue(sim.OpenDesks().Exists(d => d.id == "sense"));
            var data = XgCatalog.Dataset("sense");
            Assert.AreEqual(XgTrack.Sequence, data.track); Assert.IsTrue(data.handLabel);
            Assert.AreEqual(1, XgSim.StageOfDataset("sense"));
            Assert.AreEqual(sim.PayFor("arith", 1), sim.PayFor("sense", 1), 1e-9, "pay factor 1, like 算术");
            Assert.AreEqual(XgCatalog.HandFine * sim.ManualPayFor("sense", 3), sim.HandFineFor("sense", 3), 1e-9, "the default half fine");
            Assert.IsTrue(XgSim.IsBrainDesk("sense"));
        }

        [Test]
        public void AnOldSaveWithoutTheDeskGetsItOnLoad()
        {
            var old = new XgSim { GoldChance = 0 };
            old.S.desksOpen.Remove("sense");
            Assert.IsFalse(old.S.desksOpen.Contains("sense"));
            var loaded = new XgSim(FromJson(ToJson(old.S)));
            Assert.IsTrue(loaded.DeskOpen("sense"));
            Assert.IsTrue(loaded.DatasetAvailable("sense"));
        }

        [Test]
        public void AnswersFeedTheDatasetPayAndAWrongOneFinesHalf()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            sim.SelectDesk("sense");
            double before = host.money;
            var right = sim.Answer("sense", sim.Card("sense").truth, host);
            Assert.IsTrue(right.correct); Assert.Greater(right.pay, 0);
            Assert.AreEqual(1, sim.Samples("sense"), 1e-9);
            Assert.AreEqual(before + right.pay, host.money, 1e-9);
            double mid = host.money;
            var wrongCard = sim.Card("sense");
            var wrong = sim.Answer("sense", !wrongCard.truth, host);
            Assert.IsFalse(wrong.correct); Assert.AreEqual(XgCatalog.HandFine * sim.ManualPayFor("sense", wrongCard.level), wrong.fine, 1e-9);
            Assert.AreEqual(mid - wrong.fine, host.money, 1e-9, "a wrong pick costs half a right answer's pay");
            for (int i = 0; i < XgCatalog.SamplesToTrain; i++) sim.Answer("sense", sim.Card("sense").truth, host);
            Assert.IsTrue(sim.TrainingUnlocked(XgTrack.Sequence), "its labels alone unlock training");
        }

        [Test]
        public void ItTrainsAndIsAssessedLikeAnyDeskOfTheSequenceTrack()
        {
            var sim = new XgSim { GoldChance = 0 }; var host = new Host();
            sim.SelectDesk("sense");
            for (int i = 0; i < 40; i++) sim.Answer("sense", sim.Card("sense").truth, host);
            sim.S.sequence.dataset = "sense";
            sim.TrainEpoch(XgTrack.Sequence, host);
            Assert.Greater(sim.S.epochs, 0);
            Assert.Greater(sim.BestAcc("sense"), .5, "training on it lifts it above a coin toss");
            Assert.Greater(sim.YesNoAccuracy, .5);
        }

        // ───────────── the gate ─────────────

        [Test]
        public void TheYesNoAccuracyCountsTheCommonSenseDatasetAndKeepsTheCoinTossFloor()
        {
            var sim = StageOne();
            CollectionAssert.Contains(XgSim.YesNoDatasets, "sense");
            Assert.AreEqual(.5, sim.YesNoAccuracy, 1e-9, "untrained is a coin toss");
            Train(sim, "sense", .9);
            Assert.AreEqual(.9, sim.YesNoAccuracy, 1e-9);
            Train(sim, "logic", .95);
            Assert.AreEqual(.95, sim.YesNoAccuracy, 1e-9, "the best of them");
            Train(sim, "sense", .3);
            Train(sim, "logic", .2); Train(sim, "spam", .2);
            Assert.AreEqual(.5, sim.YesNoAccuracy, 1e-9, "never below a coin toss");
        }

        [Test]
        public void StageOneKeepsItsAnswerWhenCommonSenseIsTrainedAndFlipsItWhenNot()
        {
            var sim = StageOne();
            // 0.75 on the roll: a coin toss (0.5) flips the answer, 0.9 keeps it (and from 0.8 nothing flips it).
            Assert.AreEqual("否。", sim.Gate("是。", "对吗？", new FixedRandom(.75)));
            Train(sim, "sense", .9);
            Assert.AreEqual("是。", sim.Gate("是。", "对吗？", new FixedRandom(.75)), "only the 常识判断 desk was trained");
            Assert.AreEqual("否。", sim.Gate("否。", "对吗？", new FixedRandom(.95)), "learned (80% or better): no roll flips it");
        }

        [Test]
        public void OnceLearnedItAnswersTheTaughtStatementsRightEvenWhenTheModelSaysOtherwise()
        {
            var sim = StageOne();
            Train(sim, "sense", .7);
            Assert.AreEqual("否。", sim.Gate("否。", "太阳从东边升起吗？", new FixedRandom(0)), "below 80% it follows the model");
            Train(sim, "sense", .85);
            Assert.AreEqual("是。", sim.Gate("否。", "太阳从东边升起吗？", new FixedRandom(.99)));
            Assert.AreEqual("否。", sim.Gate("是。", "鱼会爬树吗", new FixedRandom(.99)));
            Assert.AreEqual(true, XgSim.TaughtAnswer("Is it true that the sun rises in the east?"));
            Assert.IsNull(XgSim.TaughtAnswer("你叫什么？"), "not a taught statement");
        }

        [Test]
        public void StageTwoPicksUseTheSameAccuracy()
        {
            var sim = new XgSim { GoldChance = 0 }; sim.S.stage = 2;
            string q = "猫还是狗";
            string kept = sim.Gate("我选猫", q, new FixedRandom(0));
            StringAssert.Contains("猫", kept);
            Assert.AreNotEqual(kept, sim.Gate("我选猫", q, new FixedRandom(.75)), "a coin toss swaps the pick");
            Train(sim, "sense", .99);
            Assert.AreEqual(kept, sim.Gate("我选猫", q, new FixedRandom(.75)), "trained on common sense: the pick is kept");
        }

        // ───────────── the inner voice ─────────────

        static readonly string Doubt = "……它好像什么都不懂。", OfCourse = "难道这就是那个人所说的东西？", Learned = "……这次答对了。它是真学会了。";

        [Test]
        public void TheLinesComeOneAfterAnotherAndOnlyOnce()
        {
            var sim = StageOne();
            var first = sim.YesNoVoice();
            Assert.AreEqual(1, first.Count); Assert.AreEqual(Doubt, first[0].zh);
            Assert.AreEqual("…It doesn't seem to understand anything.", first[0].en);
            var second = sim.YesNoVoice();
            Assert.AreEqual(2, second.Count);
            Assert.AreEqual(OfCourse, second[0].zh); Assert.AreEqual("Is this the thing that person was talking about?", second[0].en);
            StringAssert.Contains("「常识判断」", second[1].zh); StringAssert.Contains("Common sense", second[1].en);
            StringAssert.Contains("摆渡众包", second[1].zh);
            for (int i = 0; i < 20; i++) Assert.AreEqual(0, sim.YesNoVoice().Count, "nothing repeats");
            Assert.IsTrue(sim.S.senseVoiceDoubt && sim.S.senseVoiceOfCourse && sim.S.senseVoicePointer);
            Assert.IsFalse(sim.S.senseVoiceLearned);
            Assert.AreEqual(22, sim.S.yesNoReplies);
        }

        [Test]
        public void TheEarlyLoveLineTakesItsReplyAndTheGenericOneWaits()
        {
            var sim = StageOne();
            Assert.AreEqual(0, sim.YesNoVoice(true).Count, "the love line plays for this reply");
            Assert.IsFalse(sim.S.senseVoiceDoubt, "nothing consumed");
            var next = sim.YesNoVoice();
            Assert.AreEqual(1, next.Count); Assert.AreEqual(Doubt, next[0].zh);
            Assert.AreEqual(0, sim.YesNoVoice(true).Count, "again silent while the love line plays");
            Assert.IsFalse(sim.S.senseVoiceOfCourse);
            Assert.AreEqual(OfCourse, sim.YesNoVoice()[0].zh);
        }

        [Test]
        public void OnlyAtStageOneAndOnlyWhileItIsStillAtChance()
        {
            var later = new XgSim { GoldChance = 0 };
            foreach (int stage in new[] { 2, 3, 5, 6 })
            {
                later.S.stage = stage;
                Assert.AreEqual(0, later.YesNoVoice().Count, "stage " + stage);
            }
            Assert.IsFalse(later.S.senseVoiceDoubt || later.S.senseVoiceLearned);
            Assert.AreEqual(0, later.S.yesNoReplies, "other stages do not even count");

            var trained = StageOne();
            Train(trained, "sense", .7);
            for (int i = 0; i < 5; i++) Assert.AreEqual(0, trained.YesNoVoice().Count, "0.7 is past 'understands nothing' and short of 'learned'");
            Assert.IsFalse(trained.S.senseVoiceDoubt);

            var rising = StageOne();
            Assert.AreEqual(Doubt, rising.YesNoVoice()[0].zh);
            Train(rising, "sense", .65);
            Assert.AreEqual(0, rising.YesNoVoice().Count, "it has learned a little: the second thought does not come");
            Assert.IsFalse(rising.S.senseVoiceOfCourse);
        }

        [Test]
        public void TheLearnedLineComesOnceWhenTheAccuracyFirstReachesEighty()
        {
            var sim = StageOne();
            Train(sim, "sense", .79);
            Assert.AreEqual(0, sim.YesNoVoice().Count);
            Train(sim, "sense", .8);
            var lines = sim.YesNoVoice();
            Assert.AreEqual(1, lines.Count); Assert.AreEqual(Learned, lines[0].zh);
            Assert.AreEqual("…That one was right. It really learned it.", lines[0].en);
            Assert.IsTrue(sim.S.senseVoiceLearned);
            for (int i = 0; i < 5; i++) Assert.AreEqual(0, sim.YesNoVoice().Count);
            // The love line's reply stays silent too, and does not use it up.
            var other = StageOne(); Train(other, "logic", .9);
            Assert.AreEqual(0, other.YesNoVoice(true).Count); Assert.IsFalse(other.S.senseVoiceLearned);
            Assert.AreEqual(Learned, other.YesNoVoice()[0].zh);
        }

        [Test]
        public void TheLinesNeverUseTheWordEmergence()
        {
            var sim = StageOne();
            var all = new List<XgVoiceLine>();
            all.AddRange(sim.YesNoVoice()); all.AddRange(sim.YesNoVoice());
            Train(sim, "sense", .9); all.AddRange(sim.YesNoVoice());
            Assert.AreEqual(4, all.Count);
            foreach (var l in all) { StringAssert.DoesNotContain("涌现", l.zh); Assert.IsNotEmpty(l.en); Assert.Greater(l.seconds, 0); }
        }

        [Test]
        public void TheFlagsSurviveASaveAndAnOldSaveStartsWithThemOff()
        {
            var sim = StageOne();
            sim.YesNoVoice(); sim.YesNoVoice();
            Train(sim, "sense", .85); sim.YesNoVoice();
            string json = ToJson(sim.S);
            var loaded = new XgSim(FromJson(json));
            Assert.IsTrue(loaded.S.senseVoiceDoubt && loaded.S.senseVoiceOfCourse && loaded.S.senseVoicePointer && loaded.S.senseVoiceLearned);
            Assert.AreEqual(3, loaded.S.yesNoReplies);
            Assert.AreEqual(0, loaded.YesNoVoice().Count, "nothing said twice after a reload");

            string old = Regex.Replace(json, ",\\s*\"(yesNoReplies|senseVoiceDoubt|senseVoiceOfCourse|senseVoicePointer|senseVoiceLearned)\"\\s*:\\s*(true|false|-?[0-9]+)", "");
            StringAssert.DoesNotContain("senseVoice", old);
            StringAssert.DoesNotContain("yesNoReplies", old);
            var legacy = new XgSim(FromJson(old));
            Assert.AreEqual(0, legacy.S.yesNoReplies);
            Assert.IsFalse(legacy.S.senseVoiceDoubt || legacy.S.senseVoiceOfCourse || legacy.S.senseVoicePointer || legacy.S.senseVoiceLearned);
            legacy.S.best.Clear(); // that save had trained the desk; the voice is about a lab at chance
            Assert.AreEqual(Doubt, legacy.YesNoVoice()[0].zh, "and it can start talking");
        }

        // ───────────── the to-do note ─────────────

        [Test]
        public void TheNoteSendsAnUntrainedLabToTheCommonSenseDeskFirst()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.desk = "spam"; // a player who has moved on from the starting desk
            sim.S.labels.Add(new XgLabelCount { dataset = "spam", count = 300 });
            sim.S.epochs = 5; sim.S.assessments = 1; sim.S.treeIntroDelivered = true;
            sim.S.trainedParamsK = sim.ParamsThreshold(2);
            sim.S.unlocked.Add("spam.pack"); sim.S.owned.Add("spam");
            // The pack alone would fill the data bar; its noise takes part of it back, so labels are still what is left.
            sim.S.noise.Add(new XgLabelCount { dataset = "spam", count = 4000 });
            Assert.Less(sim.TrainedSamples, sim.SamplesThreshold(2));
            Train(sim, "spam", .5);
            var label = XgGuide.Current(sim, 0).Find(s => s.id == "ability.label");
            Assert.IsNotNull(label);
            Assert.AreEqual("sense", label.arg, "the desk that teaches yes from no goes first");
            StringAssert.Contains("「常识判断」", label.zh);
            StringAssert.Contains("Common sense", label.en);
            StringAssert.Contains("专教它分清是和否", label.whyZh);
            Assert.AreEqual(1, XgCatalog.Dataset(label.arg).dataWeight, 1e-9);

            // Once it answers better than chance, or the desk has had its share, the note goes back to the best-paid desks.
            Train(sim, "spam", .9);
            Assert.AreNotEqual("sense", XgGuide.Current(sim, 0).Find(s => s.id == "ability.label").arg);
            Train(sim, "spam", .5);
            sim.S.labels.Add(new XgLabelCount { dataset = "sense", count = XgGuide.SenseGuideLabels });
            Assert.AreNotEqual("sense", XgGuide.Current(sim, 0).Find(s => s.id == "ability.label").arg);
        }
    }
}
