using System;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>
    /// 能力考 (训练页_答卷诊断能力考_设计.md §三): the chapter turns through an exam that opens at 80% of both bars,
    /// passes at 6 of 8, shows the old habit in its wrong answers, and can be sat again after a few rounds.
    /// </summary>
    public sealed class XgAbilityExamTests
    {
        static void SetSamples(XgSim sim, double ready)
        {
            // Labels on the common-sense desk until the sample bar reads the wanted share of its line.
            sim.S.labels.RemoveAll(l => l.dataset == "sense");
            var l = new XgLabelCount { dataset = "sense", count = 0 };
            sim.S.labels.Add(l);
            double need = sim.SamplesThreshold(sim.NextAbility) * ready;
            double lo = 0, hi = need * 10 + 100;
            for (int i = 0; i < 60; i++) { l.count = (lo + hi) / 2; if (sim.TrainedSamples < need) lo = l.count; else hi = l.count; }
            l.count = hi;
        }

        /// <summary>A lab before its second ability with both bars at <paramref name="ready"/> of the line.</summary>
        static XgSim Lab(double ready, int salt = 0, double samplesReady = -1)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.dataSalt = salt;
            sim.S.stageEpochs = 1;
            sim.S.trainedParamsK = sim.ParamsThreshold(sim.NextAbility) * ready;
            SetSamples(sim, samplesReady < 0 ? ready : samplesReady);
            return sim;
        }

        [Test]
        public void TheExamOpensAtEightyPercentOfBothBars()
        {
            Assert.That(Lab(.79).ExamReady, Is.False);
            Assert.That(Lab(.9, 0, .79).ExamReady, Is.False, "both bars");
            Assert.That(Lab(.8).ExamReady, Is.True);
            var fresh = Lab(1); fresh.S.stageEpochs = 0;
            Assert.That(fresh.ExamReady, Is.False, "a round trained this chapter first");
        }

        [Test]
        public void AnExamHasEightQuestionsAndTwoOfThemAreThePlayersOwn()
        {
            var sim = Lab(1);
            sim.Profile.name = "小光";
            sim.S.sheetHand.Add(sim.CreateLabelCard("logic"));
            var e = sim.TakeExam();
            Assert.That(e.items.Count, Is.EqualTo(XgSim.AbilityExamQuestions));
            Assert.That(e.items.Count(i => i.personal), Is.EqualTo(XgSim.ExamPersonal));
            Assert.That(e.items.Any(i => i.rightZh == "小光。"), "the name it was given");
            foreach (var i in e.items)
            {
                Assert.That(i.qZh, Is.Not.Empty); Assert.That(i.qEn, Is.Not.Empty);
                Assert.That(i.wrongZh, Is.Not.EqualTo(i.rightZh), i.id);
                Assert.That(i.wrongEn, Is.Not.EqualTo(i.rightEn), i.id);
            }
            Assert.That(e.items.Select(i => i.id).Distinct().Count(), Is.EqualTo(e.items.Count));
        }

        [Test]
        public void ThePassRateFollowsTheBars()
        {
            double Rate(double ready)
            {
                int pass = 0, n = 300;
                for (int s = 0; s < n; s++) if (Lab(ready, s).TakeExam().passed) pass++;
                return pass / (double)n;
            }
            Assert.That(Rate(1.25), Is.EqualTo(1), "full and then some: always");
            double full = Rate(1), eighty = Rate(.8);
            Assert.That(full, Is.InRange(.75, .98), "full bars: most of the time");
            Assert.That(eighty, Is.InRange(.04, .3), "at 80%: now and then");
        }

        [Test]
        public void APassTurnsTheChapterOnlyWhenClaimed()
        {
            var sim = Lab(1.25);
            var e = sim.TakeExam();
            Assert.That(e.passed, Is.True);
            Assert.That(sim.ExamPassPending, Is.False, "nothing tells the result before the page has shown it");
            Assert.That(sim.TakeExam(), Is.SameAs(e), "an unfinished sitting plays again, it is not sat again");
            sim.RevealExam();
            Assert.That(sim.HasAbility(2), Is.False, "the page plays the answers first");
            Assert.That(sim.ExamPassPending, Is.True);
            Assert.That(sim.ExamReady, Is.False);
            Assert.That(sim.ClaimExamPass(), Is.True);
            Assert.That(sim.HasAbility(2), Is.True);
            Assert.That(sim.S.stage, Is.EqualTo(2));
            Assert.That(sim.ExamPassPending, Is.False);
        }

        [Test]
        public void TheOldChapterButtonClaimsAPassedExam()
        {
            var sim = Lab(1.25);
            sim.TakeExam();
            sim.RevealExam();
            Assert.That(sim.ConfirmNextStage(), Is.True);
            Assert.That(sim.HasAbility(2), Is.True);
        }

        [Test]
        public void AFailCostsNothingAndWaitsThreeRounds()
        {
            XgSim sim = null; XgExamSheet e = null;
            for (int s = 0; s < 200; s++) { sim = Lab(.8, s); e = sim.TakeExam(); if (!e.passed) break; }
            sim.RevealExam();
            Assert.That(e.passed, Is.False);
            Assert.That(e.items.Where(i => !i.right).All(i => i.reasonZh.Length > 0 && i.reasonEn.Length > 0), "every wrong answer says which bar is short");
            Assert.That(sim.ExamReady, Is.False);
            Assert.That(sim.ExamRoundsToRetry, Is.EqualTo(XgSim.ExamRetryRounds));
            sim.S.stageEpochs += XgSim.ExamRetryRounds;
            Assert.That(sim.ExamReady, Is.True);
            Assert.That(sim.TakeExam().attempt, Is.EqualTo(2));
        }

        [Test]
        public void WrongAnswersShowTheHabitOfTheChapterBefore()
        {
            var rng = new Random(1);
            Assert.That(new[] { "是。", "否。" }, Does.Contain(XgSim.ExamWrongAnswer(2, "", "凉的。", false, rng)));
            Assert.That(XgSim.ExamWrongAnswer(3, "", "猫。", false, rng), Does.StartWith("选 "));
            string heap = XgSim.ExamWrongAnswer(4, "", "我很好。", false, rng);
            Assert.That(heap.Replace("　", "").OrderBy(c => c), Is.EqualTo("我很好".OrderBy(c => c)), "the right words, in a heap");
            Assert.That(heap.Replace("　", ""), Is.Not.EqualTo("我很好"));
            string half = XgSim.ExamWrongAnswer(5, "translate", "I lent Lao Zhou my graphics card yesterday.", true, rng);
            Assert.That(half, Does.StartWith("…"));
            Assert.That("I lent Lao Zhou my graphics card yesterday.", Does.EndWith(half.TrimStart('…')));
            Assert.That(XgSim.ExamWrongAnswer(5, "memory", "750Ti。", false, rng), Does.Contain("谁"));
        }

        [Test]
        public void EveryChapterHasEnoughQuestions()
        {
            for (int a = 2; a < XgSim.AbilityCount; a++)
                Assert.That(XgExamBank.All.Count(q => q.ability == a), Is.GreaterThanOrEqualTo(XgSim.AbilityExamQuestions), "ability " + a);
            Assert.That(XgExamBank.All.Select(q => q.id).Distinct().Count(), Is.EqualTo(XgExamBank.All.Length), "unique ids");
            Assert.That(XgExamBank.All.All(q => q.zh.Length > 0 && q.en.Length > 0 && q.rightZh.Length > 0 && q.rightEn.Length > 0));
        }

        [Test]
        public void TheLearnedMomentShowsAQuestionItReallyGotWrongBefore()
        {
            // Fail first, then pass: a question wrong in the failed sitting and right now is the before and after.
            for (int s = 0; s < 300; s++)
            {
                var sim = Lab(.8, s);
                var first = sim.TakeExam();
                sim.RevealExam();
                if (first.passed) continue;
                sim.S.stageEpochs += XgSim.ExamRetryRounds;
                sim.S.trainedParamsK = sim.ParamsThreshold(2) * 1.25;
                SetSamples(sim, 1.25);
                var second = sim.TakeExam();
                sim.RevealExam();
                Assert.That(second.passed, Is.True);
                bool shared = second.items.Any(n => first.items.Any(o => o.id == n.id && !o.right));
                if (!shared) continue;
                sim.ClaimExamPass();
                var pair = sim.BeforeAfter(2);
                Assert.That(pair.real, Is.True);
                Assert.That(second.items.Any(i => i.qZh == pair.Question(false)), "one of the exam's own questions");
                return;
            }
            Assert.Inconclusive("no failed-then-passed pair shared a question");
        }

        [Test]
        public void TheGuideSendsThePlayerToTheExam()
        {
            var sim = Lab(.85);
            var step = XgGuide.ConfirmStage(sim);
            Assert.That(step.id, Is.EqualTo(XgGuide.ConfirmStageId));
            Assert.That(step.zh, Does.Contain("考"));
            Assert.That(XgGuide.ChapterWaits(sim), Is.True);
            Assert.That(XgGuide.ChapterWaits(Lab(.7)), Is.False);
        }
    }
}
