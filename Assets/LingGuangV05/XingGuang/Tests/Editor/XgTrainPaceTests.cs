using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>进度条仿真 (XgSim.TrainPace.cs): rounds take real time, and the bar runs fast and slow like a real one.</summary>
    public sealed class XgTrainPaceTests
    {
        sealed class Host : IXgHost, IXgThermal
        {
            public double money = 1e7, compute = 1, vram = 1e5;
            public bool hot;
            public double Compute => compute;
            public double VramMB => vram;
            public double Money => money;
            public bool Spend(double a) { if (money < a) return false; money -= a; return true; }
            public void Earn(double a) { money += a; }
            public void Train(double s) { }
            public string Blocker => null;
            public bool Throttling => hot;
        }

        static XgSim Lab(params string[] owned)
        {
            var state = new XgState { progressionVersion = XgSim.ProgressionSchema, stage = 3, stageVision = 3, stageSequence = 3, trainingVersion = XgSim.TrainingSchemaAuto };
            state.unlocked.Clear();
            var sim = new XgSim(state) { GoldChance = 0, UseBoard = false };
            foreach (var id in owned) sim.S.unlocked.Add(id);
            foreach (var dataset in new[] { "mnist", "spam" })
            {
                var d = XgCatalog.Dataset(dataset);
                if (XgCatalog.Desk(dataset) != null && !sim.S.desksOpen.Contains(dataset)) sim.S.desksOpen.Add(dataset);
                if (!d.handLabel || XgCatalog.Desk(dataset) == null) sim.S.owned.Add(dataset);
                sim.S.labels.Add(new XgLabelCount { dataset = dataset, count = 60000 });
                sim.SetDataset(d.track, dataset);
            }
            return sim;
        }

        /// <summary>Ticks until the round lands; returns the elapsed seconds (or −1 if it never does).</summary>
        static double RunRound(XgSim sim, XgTrack track, Host host, double dt, List<XgPace> trace = null)
        {
            var run = sim.Run(track);
            int before = run.epoch;
            Assert.IsTrue(sim.BeginEpoch(track, host, false));
            double t = 0;
            while (t < 60)
            {
                sim.Tick(dt, host);
                t += dt;
                if (run.epoch > before) return t;
                trace?.Add(sim.Pace(run));
            }
            return -1;
        }

        [Test]
        public void DurationGrowsWithParamsAndShrinksWithCompute()
        {
            var sim = Lab();
            var host = new Host();
            var small = new XgRun { track = 0, arch = "mlp", dataset = "mnist", depth = 1, width = 0 };
            var big = new XgRun { track = 0, arch = "resnet", dataset = "mnist", depth = 20, width = 5 };
            double smallOne = sim.PaceSeconds(small, host), bigOne = sim.PaceSeconds(big, host);
            Assert.Greater(bigOne, smallOne, "a bigger model takes longer");
            Assert.That(smallOne, Is.InRange(1.2, 3.0), "an early small model on one card: a few seconds");
            host.compute = 2;
            double bigTitan = sim.PaceSeconds(big, host);
            Assert.That(bigTitan, Is.InRange(3.0, 7.0), "a large late model on one fast card");
            host.compute = 8;
            Assert.Less(sim.PaceSeconds(big, host), bigTitan, "more cards, shorter rounds");
            Assert.Less(sim.PaceSeconds(new XgRun { track = 1, arch = "transformer", dataset = "spam", depth = 4, width = 4 }, host),
                sim.PaceSeconds(new XgRun { track = 1, arch = "lstm", dataset = "spam", depth = 4, width = 4 }, host), "attention reads the sentence at once");
        }

        [Test]
        public void SpeedVariesWithinARoundAndThePhasesComeInOrder()
        {
            var sim = Lab();
            var host = new Host();
            var trace = new List<XgPace>();
            double t = RunRound(sim, XgTrack.Vision, host, .05, trace);
            Assert.Greater(t, 0);
            var speeds = new HashSet<double>();
            double lo = double.MaxValue, hi = 0;
            var last = XgPacePhase.None;
            int validating = 0;
            foreach (var p in trace)
            {
                Assert.IsTrue(p.active);
                Assert.GreaterOrEqual((int)p.phase, (int)last, "加载数据 → 训练 → 验证");
                last = p.phase;
                Assert.IsNotEmpty(p.phaseLabel);
                if (p.phase == XgPacePhase.Validate) { validating++; Assert.AreEqual(XgSim.PaceValidateAt, p.progress, 1e-9, "validation pauses, then jumps"); continue; }
                speeds.Add(System.Math.Round(p.speed, 4));
                lo = System.Math.Min(lo, p.speed); hi = System.Math.Max(hi, p.speed);
            }
            Assert.AreEqual(XgPacePhase.Validate, last);
            Assert.Greater(validating, 0);
            Assert.Greater(speeds.Count, 5, "the speed changes within the round");
            Assert.Greater(hi / System.Math.Max(1e-6, lo), 1.5, "sometimes fast, sometimes slow");
        }

        [Test]
        public void RoundsStayWithinTheirBounds()
        {
            var sim = Lab();
            var host = new Host { hot = true };
            for (int i = 0; i < 40; i++)
            {
                var trace = new List<XgPace>();
                double t = RunRound(sim, XgTrack.Vision, host, .05, trace);
                double nominal = sim.S.vision.epochDuration;
                Assert.That(t, Is.InRange(XgSim.PaceMinFactor * nominal - .05, XgSim.PaceMaxFactor * nominal + .05), "round " + i);
            }
        }

        [Test]
        public void StallsOnlyAppearUnderTheirConditions()
        {
            var cool = Lab();
            var hot = Lab();
            var coolHost = new Host();
            var hotHost = new Host { hot = true };
            var counts = new Dictionary<XgPaceStall, int>[2];
            for (int k = 0; k < 2; k++)
            {
                var sim = k == 0 ? cool : hot;
                var host = k == 0 ? coolHost : hotHost;
                var c = counts[k] = new Dictionary<XgPaceStall, int>();
                var run = sim.S.vision;
                for (int i = 0; i < 600; i++)
                {
                    run.epoch = i;
                    Assert.IsTrue(sim.BeginEpoch(XgTrack.Vision, host, false));
                    var s = (XgPaceStall)run.paceStall;
                    c[s] = (c.TryGetValue(s, out int n) ? n : 0) + 1;
                    Assert.AreNotEqual(XgPaceStall.SharedCard, sim.Pace(run).stall, "one line alone shares nothing");
                    run.epochActive = false;
                }
            }
            Assert.IsFalse(counts[0].ContainsKey(XgPaceStall.Throttle), "cool cards never clock down");
            Assert.Greater(counts[1].TryGetValue(XgPaceStall.Throttle, out int th) ? th : 0, 100, "hot cards often do");
            Assert.Greater(counts[0].TryGetValue(XgPaceStall.Disk, out int disk) ? disk : 0, 20, "the disk is sometimes slow");
            int swap = counts[0].TryGetValue(XgPaceStall.Swap, out int sw) ? sw : 0;
            Assert.Greater(swap, 0);
            Assert.Less(swap, 600 * .08, "swapping is rare");

            // The stall shows while it lasts, with its reason.
            var sim2 = Lab();
            var run2 = sim2.S.vision;
            bool seen = false;
            for (int i = 0; i < 200 && !seen; i++)
            {
                run2.epoch = 1000 + i;
                var trace = new List<XgPace>();
                RunRound(sim2, XgTrack.Vision, hotHost, .05, trace);
                foreach (var p in trace)
                    if (p.stall == XgPaceStall.Throttle) { seen = true; Assert.AreEqual("显卡降频（太热了）", p.stallReason); Assert.Less(p.speed, .2); }
            }
            Assert.IsTrue(seen);

            // Both lines in a round on the one card: the other line is using it.
            var both = Lab();
            Assert.IsTrue(both.BeginEpoch(XgTrack.Vision, coolHost, false));
            Assert.AreNotEqual(XgPaceStall.SharedCard, both.Pace(XgTrack.Vision).stall);
            Assert.IsTrue(both.BeginEpoch(XgTrack.Sequence, coolHost, false));
            both.S.vision.paceStall = both.S.sequence.paceStall = 0;
            Assert.AreEqual(XgPaceStall.SharedCard, both.Pace(XgTrack.Vision).stall);
            Assert.AreEqual("另一条线在用卡", both.Pace(XgTrack.Sequence).stallReason);
        }

        [Test]
        public void SameInputsGiveTheSameProgress()
        {
            var a = Lab();
            var b = Lab();
            var host = new Host();
            Assert.IsTrue(a.BeginEpoch(XgTrack.Vision, host, false));
            Assert.IsTrue(b.BeginEpoch(XgTrack.Vision, host, false));
            for (int i = 0; i < 200 && a.S.vision.epochActive; i++)
            {
                a.Tick(.05, host); b.Tick(.05, host);
                Assert.AreEqual(a.S.vision.epochProgress, b.S.vision.epochProgress, 0);
                Assert.AreEqual(a.Pace(XgTrack.Vision).eta, b.Pace(XgTrack.Vision).eta, 0);
            }

            // The frame rate does not change the bar: 20 small ticks land where 4 big ones do.
            var fine = Lab();
            var coarse = Lab();
            Assert.IsTrue(fine.BeginEpoch(XgTrack.Vision, host, false));
            Assert.IsTrue(coarse.BeginEpoch(XgTrack.Vision, host, false));
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 5; j++) fine.Tick(.02, host);
                coarse.Tick(.1, host);
                Assert.AreEqual(coarse.S.vision.epochProgress, fine.S.vision.epochProgress, 1e-6);
            }
        }

        [Test]
        public void OldSaveWithoutPaceFieldsTrains()
        {
            var sim = Lab();
            var host = new Host();
            sim.TrainEpoch(XgTrack.Vision, host, false);
            sim.S.vision.epochActive = true;
            sim.S.vision.epochProgress = .4;
            sim.S.vision.epochDuration = .6;
            string json = UnityEngine.JsonUtility.ToJson(sim.S);
            json = Regex.Replace(json, "\"pace[A-Za-z]*\":[^,}]*,?", "");
            StringAssert.DoesNotContain("paceSeed", json);
            var old = UnityEngine.JsonUtility.FromJson<XgState>(json);
            Assert.AreEqual(0, old.vision.paceSeed);

            // Loading drops the unfinished round, and the next one runs with its bar.
            var loaded = new XgSim(old) { GoldChance = 0, UseBoard = false };
            Assert.IsFalse(loaded.Pace(XgTrack.Vision).active);
            int epoch = loaded.S.vision.epoch;
            Assert.Greater(RunRound(loaded, XgTrack.Vision, host, .1), 0);
            Assert.AreEqual(epoch + 1, loaded.S.vision.epoch);

            // A round left running without a plan (as an old save held it) still finishes from where it was.
            var held = Lab();
            held.TrainEpoch(XgTrack.Vision, host, false);
            var run = held.S.vision;
            run.epochActive = true; run.epochProgress = .4; run.epochDuration = .6; run.paceSeed = 0;
            Assert.IsTrue(held.Pace(XgTrack.Vision).active);
            held.Tick(1, host);
            Assert.IsFalse(run.epochActive);
            Assert.AreEqual(2, run.epoch);
        }

        [Test]
        public void EtaWobblesButNeverGoesNegativeAndEndsAtZero()
        {
            var sim = Lab();
            var host = new Host();
            bool rose = false;
            for (int r = 0; r < 6; r++)
            {
                var trace = new List<XgPace>();
                double t = RunRound(sim, XgTrack.Vision, host, .05, trace);
                Assert.Greater(t, 0);
                double prev = double.MaxValue;
                foreach (var p in trace)
                {
                    Assert.GreaterOrEqual(p.eta, 0);
                    Assert.LessOrEqual(p.eta, XgSim.PaceMaxFactor * p.nominal + 1e-9);
                    if (p.eta > prev + 1e-6) rose = true;
                    prev = p.eta;
                }
                Assert.Less(trace[trace.Count - 1].eta, .5, "almost done: the ETA is nearly 0");
                Assert.AreEqual(0, sim.Pace(XgTrack.Vision).eta, "done: 0");
            }
            Assert.IsTrue(rose, "the estimate wobbles like a real ETA");
        }
    }
}
