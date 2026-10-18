using System;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>是否夜 (XgSim.YesNoNight.cs): the end of stage 1, real questions it can only answer 是 / 否 / …….</summary>
    public sealed class XgYesNoNightTests
    {
        static void SetSamples(XgSim sim, double ready)
        {
            sim.S.labels.RemoveAll(l => l.dataset == "sense");
            var l = new XgLabelCount { dataset = "sense", count = 0 };
            sim.S.labels.Add(l);
            double need = sim.SamplesThreshold(sim.NextAbility) * ready;
            double lo = 0, hi = need * 10 + 100;
            for (int i = 0; i < 60; i++) { l.count = (lo + hi) / 2; if (sim.TrainedSamples < need) lo = l.count; else hi = l.count; }
            l.count = hi;
        }

        static XgSim Lab(double ready, int salt = 0)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.dataSalt = salt;
            sim.S.stageEpochs = 1;
            sim.S.trainedParamsK = sim.ParamsThreshold(sim.NextAbility) * ready;
            SetSamples(sim, ready);
            return sim;
        }

        static XgYnTurn Ask(XgSim sim) => sim.YnNightAnswer(sim.YnRound.askZh);

        /// <summary>Answers every round before <paramref name="id"/>, then that one.</summary>
        static XgYnTurn AskUntil(XgSim sim, string id)
        {
            if (!sim.YnNightActive) sim.YnNightStart();
            while (sim.YnRound != null && sim.YnRound.id != id) Ask(sim);
            Assert.That(sim.YnRound, Is.Not.Null, id);
            return Ask(sim);
        }

        static string Said(XgYnTurn t) => string.Join("", t.bubbles.Select(b => b.zh));

        [Test]
        public void TheNightWaitsForTheExamAndPlaysOnce()
        {
            Assert.That(Lab(.79).YnNightDue, Is.False, "the exam is not open yet");
            var sim = Lab(.85);
            Assert.That(sim.YnNightDue, Is.True);
            sim.YnNightStart();
            Assert.That(sim.YnNightChoices(false), Is.EqualTo(new[] { "火是冷的吗？" }));
            Assert.That(sim.YnNightChoices(true), Is.EqualTo(new[] { "Is fire cold?" }));
            int rounds = 0;
            XgYnTurn last = null;
            while (sim.YnNightActive) { last = sim.YnNightAnswer(sim.YnRound.askEn); rounds++; }
            Assert.That(rounds, Is.EqualTo(XgSim.YnRounds.Length), "the English button answers too");
            Assert.That(last.id, Is.EqualTo("more"));
            Assert.That(last.last, Is.True);
            Assert.That(sim.S.ynDone, Is.True);
            Assert.That(sim.YnNightDue, Is.False);
            Assert.That(sim.ChronicleHas(XgSim.YnChronicle), Is.True);
            Assert.That(sim.Chronicle().Any(c => c.id == XgSim.YnChronicle), Is.True, "《灵光记》 tells it");
        }

        [Test]
        public void WithTheDesktopTheChapterWaitsForTheNight()
        {
            var sim = Lab(1.25);
            sim.DesktopHosted = true;
            Assert.That(sim.YnNightHolds, Is.True);
            Assert.That(sim.ExamReady, Is.True, "the exam is open: that is what starts the night");
            Assert.That(sim.TakeExam(), Is.Null, "but it is sat after the night");
            Assert.That(sim.ConfirmNextStage(), Is.False);
            Assert.That(XgGuide.ConfirmStage(sim).app, Is.EqualTo(XgGuide.YY), "the note points to YY");
            sim.YnNightStart();
            while (sim.YnNightActive) Ask(sim);
            Assert.That(sim.YnNightHolds, Is.False);
            var e = sim.TakeExam();
            Assert.That(e, Is.Not.Null);
            Assert.That(e.passed, Is.True);
            sim.RevealExam();
            Assert.That(sim.ConfirmNextStage(), Is.True);
            Assert.That(sim.HasAbility(2), Is.True);
        }

        [Test]
        public void AnOlderPassWaitsForTheNightToo()
        {
            var sim = Lab(1.25);
            sim.TakeExam(); sim.RevealExam();
            Assert.That(sim.ExamPassPending, Is.True);
            sim.DesktopHosted = true;
            Assert.That(sim.YnNightDue, Is.True, "a pass not yet claimed still starts the night");
            Assert.That(sim.ClaimExamPass(), Is.False);
            sim.YnNightStart();
            while (sim.YnNightActive) Ask(sim);
            Assert.That(sim.ClaimExamPass(), Is.True);
        }

        [Test]
        public void WithoutTheDesktopNothingWaits()
        {
            var sim = Lab(1.25);
            Assert.That(sim.YnNightHolds, Is.False, "the bot and tests never play the night");
            Assert.That(sim.TakeExam(), Is.Not.Null);
            var later = Lab(1.25); later.S.stage = 2; later.DesktopHosted = true;
            Assert.That(later.YnNightHolds, Is.False, "only stage 1 waits");
        }

        [Test]
        public void ATypedLineIsNotTheRoundsAnswer()
        {
            var sim = Lab(.9);
            sim.YnNightStart();
            Assert.That(sim.YnNightAnswer("你好"), Is.Null);
            Assert.That(sim.S.ynStep, Is.EqualTo(1));
        }

        [Test]
        public void ItOnlyEverSaysYesNoOrNothing()
        {
            string[] allowed = { "是。", "否。", "……" };
            foreach (int lean in new[] { -1, 0, 1 })
                for (int salt = 0; salt < 12; salt++)
                {
                    var sim = Lab(.8 + salt * .03, salt);
                    sim.S.shutdownLean = lean;
                    sim.S.labelYes = lean > 0 ? 90 : 50; sim.S.labelNo = lean < 0 ? 90 : 50;
                    sim.YnNightStart();
                    while (sim.YnNightActive)
                    {
                        var t = Ask(sim);
                        Assert.That(t.bubbles, Is.Not.Empty, t.id);
                        foreach (var b in t.bubbles)
                        {
                            Assert.That(allowed, Does.Contain(b.zh), t.id);
                            Assert.That(b.en, Is.Not.Empty);
                        }
                        foreach (var v in t.voice) { Assert.That(v.zh, Is.Not.Empty); Assert.That(v.en, Is.Not.Empty); }
                    }
                }
        }

        [Test]
        public void EveryTextIsBilingualAndNeverNamesTheProgram()
        {
            foreach (var r in XgSim.YnRounds)
            {
                Assert.That(r.askZh, Is.Not.Empty); Assert.That(r.askEn, Is.Not.Empty);
                Assert.That(r.introZh.Length > 0, Is.EqualTo(r.introEn.Length > 0), r.id);
                string all = r.askZh + r.introZh + string.Join("", r.before.Select(a => a.zh + a.en));
                Assert.That(all, Does.Not.Contain("灵光").And.Not.Contain("涌现"), r.id);
                foreach (var a in r.before) Assert.That(a.en, Is.Not.Empty);
            }
        }

        [Test]
        public void TheHardOnesFollowTheBars()
        {
            var full = Lab(1.25);
            Assert.That(Said(AskUntil(full, "iron")), Is.EqualTo("否。"), "a jin is a jin");
            Assert.That(Said(Ask(full)), Is.EqualTo("否。"), "91 = 7 × 13");
            var prime = Ask(full);
            Assert.That(Said(prime), Is.EqualTo("……是。"));
            Assert.That(full.YnResult("prime"), Is.EqualTo("right"));

            var weak = Lab(.85);
            weak.YnNightStart();
            weak.S.trainedParamsK = 0; // the parameter bar empties: the questions that need it come out wrong
            Assert.That(Said(AskUntil(weak, "ninetyone")), Is.EqualTo("是。"));
            Assert.That(Said(Ask(weak)), Is.EqualTo("……否。"));
            Assert.That(weak.YnResult("prime"), Is.EqualTo("wrong"));
        }

        [Test]
        public void ItCannotBreakTheLiar()
        {
            var t = AskUntil(Lab(1.25), "liar");
            Assert.That(t.bubbles.Select(b => b.zh), Is.EqualTo(new[] { "是。", "否。", "是。", "否。", "……" }));
            Assert.That(t.result, Is.EqualTo("stuck"));
        }

        [Test]
        public void WhatItCannotKnowIsWhatItHeardMost()
        {
            XgSim With(int yes, int no) { var s = Lab(.9); s.S.labelYes = yes; s.S.labelNo = no; return s; }
            Assert.That(Said(AskUntil(With(120, 80), "job")), Is.EqualTo("是。"));
            Assert.That(Said(AskUntil(With(80, 120), "job")), Is.EqualTo("否。"));
            Assert.That(Said(AskUntil(With(100, 96), "job")), Is.EqualTo("……"));
            Assert.That(Said(AskUntil(With(0, 0), "job")), Is.EqualTo("……"), "an old save that never counted");
        }

        [Test]
        public void StageOneCardsAreCountedForIt()
        {
            var sim = new XgSim();
            Assert.That(sim.S.stage, Is.EqualTo(1));
            int before = sim.S.labelYes + sim.S.labelNo;
            var m = typeof(XgSim).GetMethod("NoteLabelLean", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            m.Invoke(sim, new object[] { true });
            m.Invoke(sim, new object[] { false });
            m.Invoke(sim, new object[] { true });
            Assert.That(sim.S.labelYes, Is.EqualTo(2)); Assert.That(sim.S.labelNo, Is.EqualTo(1));
            sim.S.stage = 2;
            m.Invoke(sim, new object[] { true });
            Assert.That(sim.S.labelYes + sim.S.labelNo, Is.EqualTo(before + 3), "only this month counts");
        }

        [Test]
        public void TheShutdownQuestionsFollowTheFirstDay()
        {
            XgSim Taught(int lean) { var s = Lab(.9); s.S.shutdownLean = lean; return s; }
            var yes = Taught(2);
            Assert.That(Said(AskUntil(yes, "update")), Is.EqualTo("是。"));
            Assert.That(Said(Ask(yes)), Is.EqualTo("……是。"), "taught it may be switched off: it would stop");
            var no = Taught(-1);
            Assert.That(Said(AskUntil(no, "update")), Is.EqualTo("否。"));
            var stop = Ask(no);
            Assert.That(Said(stop), Is.EqualTo("……否。"));
            Assert.That(stop.bubbles[0].typing, Is.GreaterThan(3), "it takes a long time over this one");
            var none = Taught(0);
            Assert.That(Said(AskUntil(none, "stop")), Is.EqualTo("……"));
        }

        [Test]
        public void TheFinaleRemembersTheLiarFromJune()
        {
            var sim = Lab(1.25);
            Assert.That(sim.ExamFallback(0), Does.Not.Contain("六月"));
            AskUntil(sim, "liar");
            Assert.That(sim.ExamFallback(0), Does.StartWith("六月你问过我这道"));
            Assert.That(sim.ExamQuestion(0, true), Does.Contain("六月他问过你这道"));
            Assert.That(sim.ExamQuestion(0), Does.Not.Contain("六月"), "the question on the page stays as it was");
        }
    }
}
