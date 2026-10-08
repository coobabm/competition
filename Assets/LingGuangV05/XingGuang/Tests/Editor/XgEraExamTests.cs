using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgEraExamTests
    {
        sealed class Host : IXgHost
        {
            public double compute = 2, vram = 100000;
            public string blocker;
            public double Compute => compute;
            public double VramMB => vram;
            public double Money => 1234;
            public string Blocker => blocker;
            public bool Spend(double amount) { Assert.Fail("exam must not spend"); return false; }
            public void Earn(double amount) { Assert.Fail("exam must not reward"); }
            public void Train(double seconds) { Assert.Fail("exam must not train"); }
        }
        static XgSim Fixture(string dataset = "translate")
        {
            var sim = new XgSim(new XgState { dataSalt = 7 });
            sim.S.sequence.dataset = dataset;
            return sim;
        }
        [Test]
        public void FreshQuestionsAreUniqueDeterministicAndDoNotReuseEarlierExam()
        {
            var sim = Fixture(); var host = new Host();
            var a = sim.RunEraFreshExam("translate", 41, host);
            Assert.That(a.valid, Is.True, a.reason);
            Assert.That(a.count, Is.EqualTo(20));
            Assert.That(new HashSet<string>(a.signatures).Count, Is.EqualTo(20));
            var repeat = sim.RunEraFreshExam("translate", 41, host);
            Assert.That(repeat.transcript, Is.EqualTo(a.transcript));
            var b = sim.RunEraFreshExam("translate", 41, host, a.signatures);
            Assert.That(b.valid, Is.True, b.reason);
            Assert.That(new HashSet<string>(a.signatures).Overlaps(b.signatures), Is.False);
            Assert.That(a.accuracy, Is.EqualTo((double)a.right / a.count));
        }
        [TestCase(7)][TestCase(41)][TestCase(42)]
        public void FreshQuestionsAreBalancedSoAConstantGuessCannotPass(int seed)
        {
            var result = Fixture().RunEraFreshExam("translate", seed, new Host());
            Assert.That(result.valid, Is.True, result.reason);
            int positive = 0, negative = 0;
            foreach (string signature in result.signatures)
            {
                if (signature.StartsWith("sequence|True|", StringComparison.Ordinal)) positive++;
                if (signature.StartsWith("sequence|False|", StringComparison.Ordinal)) negative++;
            }
            Assert.That(positive, Is.EqualTo(10), "positive quota");
            Assert.That(negative, Is.EqualTo(10), "negative quota");
        }
        [Test]
        public void FreshQuestionsAreDisjointFromTrainingAndExposedDiagnostics()
        {
            var sim = Fixture(); var excluded = new HashSet<string>();
            for (int i = 0; i < XgBoardData.PoolLimit; i++) excluded.Add(XgBoardData.Signature(
                XgBoardData.Make("translate", XgBoardData.Seed("translate", i, XgBoardData.Use.Train, sim.S.dataSalt), sim.BoardLevel("translate"), sim.Today)));
            foreach (var c in sim.TestSet("translate")) excluded.Add(XgBoardData.Signature(c));
            for (int i = 0; i < XgSim.ReservedExamCards; i += 97) excluded.Add(XgBoardData.Signature(
                XgBoardData.Make("translate", XgBoardData.Seed("translate", i, XgBoardData.Use.Exam, sim.S.dataSalt), 1, sim.Today)));
            var result = sim.RunEraFreshExam("translate", 42, new Host());
            Assert.That(result.valid, Is.True, result.reason);
            Assert.That(excluded.Overlaps(result.signatures), Is.False);
        }
        [TestCase("blocked")][TestCase("compute")][TestCase("vram")][TestCase("null")]
        public void ResourcesFailClosed(string mode)
        {
            var host = new Host();
            if (mode == "blocked") host.blocker = "断电";
            if (mode == "compute") host.compute = 0;
            if (mode == "vram") host.vram = 0;
            var result = Fixture().RunEraFreshExam("translate", 7, mode == "null" ? null : host);
            Assert.That(result.valid, Is.False); Assert.That(result.reason, Is.Not.Empty);
            Assert.That(result.count, Is.Zero); Assert.That(result.signatures, Is.Empty);
        }
        [Test]
        public void CurrentDatasetMustMatchAndUnknownDatasetFailsClosed()
        {
            var sim = Fixture();
            Assert.That(sim.RunEraFreshExam("spam", 1, new Host()).valid, Is.False);
            Assert.That(sim.RunEraFreshExam("missing", 1, new Host()).valid, Is.False);
        }
        [Test]
        public void ExamLeavesRngModelsMistakesSamplesAndBoardUntouched()
        {
            var sim = Fixture(); var s = sim.S; var run = s.sequence;
            long rng = s.rng, next = s.nextCardId, cursor = run.cursor, cards = s.board.cards;
            int models = s.models.Count, concepts = s.board.concepts.Count;
            double samples = sim.Samples("translate");
            run.mistakeCount = 91; run.examCount = 101; run.mistakeDataset = "sentinel";
            var result = sim.RunEraFreshExam("translate", 7, new Host());
            Assert.That(result.valid, Is.True, result.reason);
            Assert.That(s.rng, Is.EqualTo(rng)); Assert.That(s.nextCardId, Is.EqualTo(next));
            Assert.That(run.cursor, Is.EqualTo(cursor)); Assert.That(s.board.cards, Is.EqualTo(cards));
            Assert.That(s.board.concepts.Count, Is.EqualTo(concepts)); Assert.That(s.models.Count, Is.EqualTo(models));
            Assert.That(sim.Samples("translate"), Is.EqualTo(samples));
            Assert.That(run.mistakeCount, Is.EqualTo(91)); Assert.That(run.examCount, Is.EqualTo(101));
            Assert.That(run.mistakeDataset, Is.EqualTo("sentinel"));
        }
        [Test]
        public void ConceptWeightsActuallyChangePredictions()
        {
            var sim = Fixture(); sim.S.unlocked.Add("bias");
            var concept = new XgConcept { id = 444, key = XgBoard.BiasElement, region = "sequence", layer = 1, s = 1, w = 10 };
            sim.Board.S.concepts.Add(concept);
            var a = sim.RunEraFreshExam("translate", 7, new Host());
            concept.w = -10;
            var b = sim.RunEraFreshExam("translate", 7, new Host());
            Assert.That(a.valid && b.valid, Is.True);
            Assert.That(a.signatures, Is.EqualTo(b.signatures));
            Assert.That(a.transcript, Is.Not.EqualTo(b.transcript), "actual concept weights, not recorded best accuracy, determine predictions");
            Assert.That(a.right + b.right, Is.EqualTo(20));
        }
        [Test]
        public void ExhaustedQuestionSpaceDoesNotFillWithOldQuestions()
        {
            var result = Fixture("xor").RunEraFreshExam("xor", 7, new Host());
            Assert.That(result.valid, Is.False); Assert.That(result.reason, Does.Contain("不足"));
            Assert.That(result.signatures, Is.Empty); Assert.That(result.count, Is.Zero);
        }
    }
}
