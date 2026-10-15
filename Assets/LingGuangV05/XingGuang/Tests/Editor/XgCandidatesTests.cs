using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>它在学 §2.1–2.2: the chat's candidate bars and attention lines, and the microscope's real run.</summary>
    public sealed class XgCandidatesTests
    {
        const string Reply = "我记得你借了老周的显卡，今天还回去吗？";

        [Test]
        public void CandidatesAreDeterministicPerReply()
        {
            var a = XgCandidates.ForReply(Reply, 3, .4);
            var b = XgCandidates.ForReply(Reply, 3, .4);
            Assert.That(a.Count, Is.EqualTo(b.Count));
            for (int i = 0; i < a.Count; i++)
                for (int k = 0; k < a[i].options.Count; k++)
                {
                    Assert.That(a[i].options[k].token, Is.EqualTo(b[i].options[k].token));
                    Assert.That(a[i].options[k].p, Is.EqualTo(b[i].options[k].p));
                }
        }

        [Test]
        public void EveryStepSumsToOneWithThreeToFiveCandidatesAndTheTypedOneChosen()
        {
            foreach (string text in new[] { Reply, "是。", "Yes, I remember Lao Zhou's graphics card." })
                for (int stage = 1; stage <= 6; stage++)
                    foreach (var step in XgCandidates.ForReply(text, stage, .5))
                    {
                        double sum = 0; int chosen = 0;
                        var seen = new HashSet<string>();
                        foreach (var o in step.options) { sum += o.p; if (o.chosen) { chosen++; Assert.That(o.token, Is.EqualTo(step.token)); } Assert.That(seen.Add(o.token), "distinct: " + o.token); }
                        Assert.That(sum, Is.EqualTo(1).Within(1e-9), text + " stage " + stage);
                        Assert.That(chosen, Is.EqualTo(1));
                        Assert.That(step.options.Count, Is.InRange(3, 5));
                        Assert.That(text.Substring(step.start, step.length), Is.EqualTo(step.token));
                        for (int i = 1; i < step.options.Count; i++) Assert.That(step.options[i - 1].p, Is.GreaterThanOrEqualTo(step.options[i].p), "most likely first");
                    }
        }

        [Test]
        public void TheRightCharacterGetsSurerStageByStage()
        {
            double last = 0;
            for (int stage = 1; stage <= 6; stage++)
            {
                double mean = 0; int n = 0;
                foreach (var step in XgCandidates.ForReply(Reply, stage, 0)) { mean += step.ChosenP; n++; }
                mean /= n;
                Assert.That(mean, Is.GreaterThan(last), "stage " + stage);
                last = mean;
            }
            Assert.That(XgCandidates.Confidence(2, 1), Is.GreaterThan(XgCandidates.Confidence(2, 0)), "progress toward the next ability counts");
            Assert.That(XgCandidates.Confidence(2, 1), Is.LessThan(XgCandidates.Confidence(3, 0)));
            foreach (var step in XgCandidates.ForReply(Reply, 2, 0)) Assert.That(step.ChosenP, Is.LessThan(.5), "scattered early");
            for (int stage = 5; stage <= 6; stage++)
                foreach (var step in XgCandidates.ForReply(Reply, stage, 0)) Assert.That(step.ChosenP, Is.GreaterThanOrEqualTo(.8), "sure late");
        }

        [Test]
        public void WrongCandidatesFollowTheChaptersHabit()
        {
            foreach (var step in XgCandidates.ForReply(Reply, 2, 0))
            {
                int letters = 0;
                foreach (var o in step.options) if (!o.chosen && "ABCD选".Contains(o.token)) letters++;
                Assert.That(letters, Is.GreaterThanOrEqualTo(3), "stage 2 picks option letters");
            }
            foreach (var step in XgCandidates.ForReply(Reply, 1, 0))
                Assert.That(step.options.Exists(o => !o.chosen && (o.token == "是" || o.token == "否")), "stage 1 only knows 是 / 否");
            foreach (var step in XgCandidates.ForReply(Reply, 3, 0))
                foreach (var o in step.options) if (!o.chosen) Assert.That(Reply.Contains(o.token) || "的了我你是在不有就这".Contains(o.token), "stage 3 heaps the reply's own characters: " + o.token);
        }

        [Test]
        public void AttentionPointsToWordsReallyInTheEarlierLines()
        {
            var earlier = new List<string> { "今天好热。", "我借了老周的显卡", "晚上去网吧吗" };
            var links = XgCandidates.Attend("老周的显卡", earlier);
            Assert.That(links.Count, Is.GreaterThan(0));
            Assert.That(links[0].message, Is.EqualTo(1));
            Assert.That(earlier[1].Substring(links[0].start, links[0].length), Is.EqualTo("老周的显卡"));
            double sum = 0;
            foreach (var l in links)
            {
                sum += l.weight;
                string piece = earlier[l.message].Substring(l.start, l.length);
                Assert.That(piece, Is.EqualTo(l.word));
                Assert.That("老周的显卡".Contains(piece) || piece.Length == 1 && "老周的显卡".Contains(piece), "shares the word: " + piece);
            }
            Assert.That(sum, Is.EqualTo(1).Within(1e-9));
            Assert.That(XgCandidates.Attend("西瓜", earlier), Is.Empty, "nothing shared, no line");
            // A shared bigram is enough; the newer line gets the thicker line.
            var two = XgCandidates.Attend("显卡", new List<string> { "显卡坏了", "显卡好了" });
            Assert.That(two[0].message, Is.EqualTo(1));
            Assert.That(two[0].weight, Is.GreaterThan(two[1].weight));
        }

        [Test]
        public void TheMemoryBookStrengthensAttention()
        {
            var earlier = new List<string> { "我借了老周的显卡", "老周的网吧" };
            var plain = XgCandidates.Attend("老周的显卡", earlier);
            var remembered = XgCandidates.Attend("老周的显卡", earlier, new[] { "主人说：我借了老周的显卡" });
            Assert.That(remembered[0].remembered, Is.True);
            Assert.That(remembered[0].score, Is.GreaterThan(plain[0].score));
            Assert.That(XgCandidates.Attend("老周的显卡", earlier), Has.Count.EqualTo(plain.Count), "deterministic");
        }

        [Test]
        public void RepliesSplitIntoWordsThatMatchTheEarlierLines()
        {
            var earlier = new List<string> { "我借了老周的显卡" };
            var words = XgCandidates.Words(Reply, earlier);
            Assert.That(words.Exists(w => Reply.Substring(w.start, w.length).Contains("老周的显卡")), "the phrase it heard is one word");
            foreach (var w in words) Assert.That(XgCandidates.IsPunct(Reply[w.start]), Is.False);
            var en = XgCandidates.Words("I remember the card.", null);
            Assert.That(en.Count, Is.EqualTo(4));
            Assert.That(XgCandidates.Attend("card", new List<string> { "Lao Zhou lent me his Card" })[0].word, Is.EqualTo("Card"));
        }

        [Test]
        public void TheSimGatesAttentionAtStageFive()
        {
            var sim = new XgSim();
            sim.S.stage = 4;
            Assert.That(sim.ReplyAttention("老周", new List<string> { "老周来了" }), Is.Empty);
            sim.S.stage = 5;
            Assert.That(sim.ReplyAttention("老周", new List<string> { "老周来了" }), Has.Count.EqualTo(1));
            Assert.That(sim.ReplyCandidates("老周来了。").Count, Is.EqualTo(4));
        }

        [Test]
        public void TheMicroscopeTrainsForRealAndTracksNewPictures()
        {
            var run = new XgTinyRun(XgTinyLesson.Microscope).Finish();
            Assert.That(run.net.Units, Is.EqualTo(4));
            Assert.That(run.test.Count, Is.GreaterThan(0));
            foreach (var s in run.test) Assert.That(run.train.Contains(s), Is.False, "held out");
            Assert.That(run.testLoss.Count, Is.EqualTo(run.loss.Count));
            Assert.That(run.loss[run.loss.Count - 1], Is.LessThan(run.loss[0] / 2), "the trained error falls");
            Assert.That(run.testLoss[run.testLoss.Count - 1], Is.LessThan(run.testLoss[0]), "and the new pictures' error too");
            Assert.That(run.TestError, Is.LessThan(.25));
            // Every look through the microscope (a new seed each time) learns too.
            for (int k = 1; k <= 8; k++)
            {
                var again = new XgTinyRun(XgTinyLesson.Microscope, XgTinyRun.Seed + 31 * k).Finish();
                Assert.That(again.loss[again.loss.Count - 1], Is.LessThan(again.loss[0] / 2), "look " + k);
                Assert.That(again.TestError, Is.LessThan(.35), "look " + k);
            }
            // The story lessons do not track a second line.
            Assert.That(new XgTinyRun(XgTinyLesson.Zero).Finish().testLoss, Is.Empty);
        }
    }
}
