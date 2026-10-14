using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The spider's behaviour state machine (XgSpiderBehavior.cs): variety, speeds, suspension, startle and poses.</summary>
    public sealed class XgSpiderBehaviorTests
    {
        static XgSpiderBrain Make(int seed, XgSpiderCtx ctx = XgSpiderCtx.Roam)
        {
            var b = new XgSpiderBrain(new Random(seed));
            b.SetContext(ctx);
            return b;
        }

        /// <summary>Runs the brain standing still for a while and returns the actions that started, in order.</summary>
        static List<XgSpiderAct> Run(XgSpiderBrain b, double seconds, double quiet = 0)
        {
            var list = new List<XgSpiderAct>();
            b.Started = a => list.Add(a);
            b.Quiet = quiet;
            for (double t = 0; t < seconds; t += .1) b.Tick(.1, true);
            return list;
        }

        [Test]
        public void TheSameIdleActionNeverComesTwiceInARow()
        {
            foreach (var ctx in new[] { XgSpiderCtx.Roam, XgSpiderCtx.Watch, XgSpiderCtx.Read })
                for (int seed = 1; seed <= 6; seed++)
                    foreach (double quiet in new[] { 0.0, 100.0 })
                    {
                        var b = Make(seed, ctx);
                        var list = Run(b, 4000, quiet);
                        Assert.That(list.Count, Is.GreaterThan(20), ctx + " acts at all");
                        for (int i = 1; i < list.Count; i++)
                            Assert.AreNotEqual(list[i - 1], list[i], ctx + " seed " + seed + " quiet " + quiet + " at " + i);
                    }
        }

        [Test]
        public void ARepeatIsOnlyAllowedWhenNothingElseIs()
        {
            var b = Make(3);
            b.Filter = a => a == XgSpiderAct.Look;
            var list = Run(b, 600);
            Assert.That(list.Count, Is.GreaterThanOrEqualTo(3), "the only allowed action still comes up");
            foreach (var a in list) Assert.AreEqual(XgSpiderAct.Look, a);
        }

        [Test]
        public void ManyDifferentActionsAppearAndEachContextHasItsOwnMix()
        {
            var roam = new HashSet<XgSpiderAct>(Run(Make(7), 6000));
            foreach (var a in new[] { XgSpiderAct.Look, XgSpiderAct.Groom, XgSpiderAct.Stretch, XgSpiderAct.Drum, XgSpiderAct.Hop, XgSpiderAct.Dangle })
                Assert.IsTrue(roam.Contains(a), "roaming does " + a);
            CollectionAssert.DoesNotContain(roam, XgSpiderAct.Think, "thinking is the view's call or the watching mix");
            CollectionAssert.DoesNotContain(roam, XgSpiderAct.Linger);
            CollectionAssert.DoesNotContain(roam, XgSpiderAct.Scan);
            CollectionAssert.DoesNotContain(roam, XgSpiderAct.Sleep, "not without a long quiet");

            var watch = new HashSet<XgSpiderAct>(Run(Make(7, XgSpiderCtx.Watch), 6000));
            Assert.IsTrue(watch.Contains(XgSpiderAct.Think) && watch.Contains(XgSpiderAct.Drum) && watch.Contains(XgSpiderAct.Look));
            foreach (var a in new[] { XgSpiderAct.Sleep, XgSpiderAct.Dangle, XgSpiderAct.Linger, XgSpiderAct.Scan }) CollectionAssert.DoesNotContain(watch, a);

            var read = new HashSet<XgSpiderAct>(Run(Make(7, XgSpiderCtx.Read), 6000));
            Assert.IsTrue(read.Contains(XgSpiderAct.Linger) && read.Contains(XgSpiderAct.Scan));
            foreach (var a in new[] { XgSpiderAct.Think, XgSpiderAct.Sleep, XgSpiderAct.Dangle }) CollectionAssert.DoesNotContain(read, a);
        }

        [Test]
        public void ThinkingComesUpMoreWhileThePlayerHesitates()
        {
            int calm = 0, curious = 0;
            for (int seed = 1; seed <= 8; seed++)
            {
                var a = Make(seed, XgSpiderCtx.Watch);
                calm += Run(a, 1500).FindAll(x => x == XgSpiderAct.Think).Count;
                var b = Make(seed, XgSpiderCtx.Watch); b.Curious = true;
                curious += Run(b, 1500).FindAll(x => x == XgSpiderAct.Think).Count;
            }
            Assert.That(curious, Is.GreaterThan(calm));
        }

        [Test]
        public void ReducedMotionDropsHopsAndDangles()
        {
            var b = Make(5); b.Reduced = true;
            var list = Run(b, 6000);
            CollectionAssert.DoesNotContain(list, XgSpiderAct.Hop);
            CollectionAssert.DoesNotContain(list, XgSpiderAct.Dangle);
            Assert.That(list.Count, Is.GreaterThan(10), "it still does the quiet ones");
            var r = Make(5); r.Reduced = true;
            for (int i = 0; i < 300; i++)
            {
                var trip = r.Plan(0, 0, 400, 100);
                Assert.That(trip.gait, Is.Not.EqualTo(XgSpiderGait.Dash).And.Not.EqualTo(XgSpiderGait.Hop));
                Assert.That(trip.speed, Is.LessThanOrEqualTo(1.15 + 1e-9));
            }
            var normal = XgSpiderPoses.Body(XgSpiderAct.Look, .3, 2, 1, false);
            var reduced = XgSpiderPoses.Body(XgSpiderAct.Look, .3, 2, 1, true);
            Assert.That(Math.Abs(reduced.yaw), Is.LessThan(Math.Abs(normal.yaw)), "smaller motions");
        }

        [Test]
        public void SleepNeedsALongQuietAndEndsWhenThePlayerReturns()
        {
            Assert.IsFalse(Run(Make(2), 3000, 0).Contains(XgSpiderAct.Sleep), "the player is around");
            Assert.IsFalse(Run(Make(2, XgSpiderCtx.Watch), 3000, 500).Contains(XgSpiderAct.Sleep), "it does not sleep while watching");
            var b = Make(4);
            b.Quiet = XgSpiderBrain.SleepAfter + 5;
            for (int i = 0; i < 20000 && b.Act != XgSpiderAct.Sleep; i++) b.Tick(.1, true);
            Assert.AreEqual(XgSpiderAct.Sleep, b.Act, "after a long quiet it curls up");
            for (int i = 0; i < 30; i++) b.Tick(.1, true);
            b.Quiet = 0;
            for (int i = 0; i < 30 && b.Act == XgSpiderAct.Sleep; i++) b.Tick(.1, true);
            Assert.AreEqual(XgSpiderAct.Stretch, b.Act, "woken: a stretch");

            var parked = Make(4);
            parked.DeepSleep = true; parked.Do(XgSpiderAct.Sleep, 1e7);
            for (int i = 0; i < 100; i++) parked.Tick(.1, true);
            Assert.AreEqual(XgSpiderAct.Sleep, parked.Act, "parked, it sleeps on");
        }

        [Test]
        public void TripSpeedsStayInTheirGaitRangesAndVaryFromTripToTrip()
        {
            var b = Make(11);
            var seen = new Dictionary<XgSpiderGait, int>();
            double lo = 9, hi = 0; int same = 0, n = 2000;
            XgSpiderGait? prev = null;
            for (int i = 0; i < n; i++)
            {
                double len = 30 + (i * 37 % 400);
                var t = b.Plan(0, 0, len, 0);
                XgSpiderGaits.Range(t.gait, out double glo, out double ghi);
                Assert.That(t.speed, Is.InRange(glo, ghi));
                if (t.gait == XgSpiderGait.Dash) Assert.That(len, Is.GreaterThanOrEqualTo(160), "short moves are never dashes");
                seen[t.gait] = seen.TryGetValue(t.gait, out int c) ? c + 1 : 1;
                lo = Math.Min(lo, t.speed); hi = Math.Max(hi, t.speed);
                if (prev == t.gait) same++;
                prev = t.gait;
            }
            Assert.That(hi / lo, Is.GreaterThan(6), "from a creep to a dash");
            foreach (var g in new[] { XgSpiderGait.Creep, XgSpiderGait.Walk, XgSpiderGait.Scurry, XgSpiderGait.Dash })
                Assert.That(seen.ContainsKey(g) ? seen[g] : 0, Is.GreaterThan(n / 100), g + " is used");
            Assert.That(same, Is.LessThan(n * .35), "successive trips usually differ");
            Assert.That(seen.ContainsKey(XgSpiderGait.Hop), Is.False, "skipping is the crawler's gait");

            // The page reader skips between items now and then and never dashes.
            var r = Make(11, XgSpiderCtx.Read);
            var read = new HashSet<XgSpiderGait>();
            for (int i = 0; i < 600; i++) read.Add(r.Plan(0, 0, 130, 0).gait);
            Assert.IsTrue(read.Contains(XgSpiderGait.Hop) && read.Contains(XgSpiderGait.Walk) && read.Contains(XgSpiderGait.Scurry));
            Assert.IsFalse(read.Contains(XgSpiderGait.Dash));
        }

        [Test]
        public void GaitsGetFasterAndSnappierInOrder()
        {
            var order = new[] { XgSpiderGait.Creep, XgSpiderGait.Walk, XgSpiderGait.Scurry, XgSpiderGait.Dash };
            for (int i = 1; i < order.Length; i++)
            {
                XgSpiderGaits.Range(order[i - 1], out double _, out double prevHi);
                XgSpiderGaits.Range(order[i], out double lo, out double _);
                Assert.That(lo, Is.GreaterThan(prevHi), "ranges do not overlap");
                Assert.That(XgSpiderGaits.Accel(order[i]), Is.GreaterThan(XgSpiderGaits.Accel(order[i - 1])));
                Assert.That(XgSpiderGaits.Brake(order[i]), Is.GreaterThan(XgSpiderGaits.Brake(order[i - 1])));
                Assert.That(XgSpiderGaits.Turn(order[i]), Is.GreaterThan(XgSpiderGaits.Turn(order[i - 1])));
            }
        }

        /// <summary>Walks a trip with plain kinematics (reference speed 70 units/s) and returns the scale at each frame.</summary>
        static List<double> Walk(XgSpiderBrain b, double x1, double y1, ref double x, ref double y, out int frames, double maxSeconds = 60)
        {
            var scales = new List<double>();
            frames = 0;
            for (double t = 0; t < maxSeconds; t += .02, frames++)
            {
                if (b.Resting) { b.Tick(.02, true); scales.Add(0); continue; }
                if (b.Steer(.02, x, y, 8, out double ax, out double ay, out double sc)) return scales;
                double dx = ax - x, dy = ay - y, d = Math.Sqrt(dx * dx + dy * dy);
                double v = 70 * sc * Math.Min(1, d / 40);
                if (d > 1e-6) { x += dx / d * v * .02; y += dy / d * v * .02; }
                scales.Add(sc);
                b.Tick(.02, false);
            }
            Assert.Fail("the trip did not end");
            return scales;
        }

        [Test]
        public void ATripEasesInArrivesAndSometimesStopsHalfway()
        {
            bool sawStop = false, sawCurve = false;
            for (int seed = 1; seed <= 40; seed++)
            {
                var b = Make(seed);
                var trip = b.Plan(0, 0, 400, 0, XgSpiderGait.Walk);
                trip.Point(.5, out double mx, out double my);
                if (Math.Abs(my) > 12) sawCurve = true;
                double x = 0, y = 0;
                var scales = Walk(b, 400, 0, ref x, ref y, out int frames);
                Assert.That(Math.Abs(x - 400) + Math.Abs(y), Is.LessThan(30), "it ends where it was going");
                Assert.That(scales[0], Is.LessThan(trip.speed * .5), "ease in, not an instant top speed");
                Assert.That(scales.Count, Is.GreaterThan(30));
                if (trip.stopU >= 0)
                {
                    // Somewhere in the middle it comes to (nearly) a standstill and moves on again.
                    int from = scales.Count / 5, to = scales.Count * 4 / 5;
                    double min = double.MaxValue; for (int i = from; i < to; i++) min = Math.Min(min, scales[i]);
                    if (min < trip.speed * .15) sawStop = true;
                }
            }
            Assert.IsTrue(sawStop, "mid-path stops happen");
            Assert.IsTrue(sawCurve, "paths bend");
        }

        [Test]
        public void ItMaySometimesChangeItsMindAfterAStop()
        {
            int redirects = 0;
            for (int seed = 1; seed <= 80; seed++)
            {
                var b = Make(seed);
                b.Plan(0, 0, 500, 0, XgSpiderGait.Walk);
                double x = 0, y = 0;
                Walk(b, 500, 0, ref x, ref y, out int _);
                if (b.TakeRedirect()) redirects++;
                Assert.IsFalse(b.TakeRedirect(), "taken once");
            }
            Assert.That(redirects, Is.InRange(1, 40));
        }

        [Test]
        public void EveryActionEndsAndTheSpiderGoesBackToWandering()
        {
            foreach (XgSpiderAct a in Enum.GetValues(typeof(XgSpiderAct)))
            {
                if (a == XgSpiderAct.None || a == XgSpiderAct.Sleep) continue;
                var b = Make(9);
                Assert.IsTrue(b.Do(a), a.ToString());
                Assert.IsTrue(b.Resting);
                double limit = b.ActSeconds + 4;
                double t = 0;
                // Quiet enough that nothing new starts: only the follow-up (a startle's look) may.
                b.Filter = x => false;
                while (b.Resting && t < limit) { b.Tick(.05, true); t += .05; }
                Assert.IsFalse(b.Resting, a + " ends");
                Assert.AreEqual(XgSpiderAct.None, b.Act);
            }
            var s = Make(9); s.Do(XgSpiderAct.Sleep);
            s.Quiet = 0;
            for (int i = 0; i < 2000 && s.Resting; i++) s.Tick(.05, true);
            Assert.IsFalse(s.Resting, "sleep ends when the player is around");
        }

        [Test]
        public void DraggingOrAnsweringByHandSuspendsItAndItResumesAfterwards()
        {
            var b = Make(6);
            b.Do(XgSpiderAct.Groom);
            b.Plan(0, 0, 100, 0);
            b.SetSuspended(true);
            Assert.AreEqual(XgSpiderAct.None, b.Act);
            Assert.IsFalse(b.Resting);
            Assert.IsFalse(b.HasTrip);
            var list = Run(b, 120);
            Assert.AreEqual(0, list.Count, "no idle actions of its own while held");
            Assert.IsFalse(b.Startle(1, 0), "no startle while held");
            Assert.IsFalse(b.Do(XgSpiderAct.Look));
            b.Joy();
            Run(b, 5);
            Assert.AreEqual(XgSpiderAct.None, b.Act, "the spin waits too");

            b.SetSuspended(false);
            var after = Run(b, 120);
            Assert.That(after.Count, Is.GreaterThan(0), "it acts again after the drop");
            Assert.AreEqual(XgSpiderAct.Dance, after[0], "and the queued spin comes first");
        }

        [Test]
        public void ARushingCursorStartlesItWithAGapAndALookBack()
        {
            var b = Make(1);
            b.Tick(.1, true);
            Assert.IsTrue(b.Startle(3, 4));
            Assert.AreEqual(XgSpiderAct.Startle, b.Act);
            Assert.AreEqual(.6, b.AwayX, 1e-9); Assert.AreEqual(.8, b.AwayY, 1e-9);
            Assert.IsFalse(b.Startle(1, 0), "not again at once");
            double t = 0; var seen = new List<XgSpiderAct>(); b.Started = a => seen.Add(a);
            b.Filter = x => false;
            while (b.Resting && t < 10) { b.Tick(.05, true); t += .05; }
            CollectionAssert.Contains(seen, XgSpiderAct.Look, "it looks back after the jolt");
            Assert.IsFalse(b.Startle(1, 0), "still inside the gap");
            for (int i = 0; i < 100; i++) b.Tick(.05, true);
            Assert.IsTrue(b.Startle(1, 0), "later it can be startled again");
        }

        [Test]
        public void JoyMakesItDanceOnceAndTheSpinEndsFacingTheSameWay()
        {
            var b = Make(8);
            b.Joy();
            b.Tick(.1, true);
            Assert.AreEqual(XgSpiderAct.Dance, b.Act);
            var end = XgSpiderPoses.Body(XgSpiderAct.Dance, 1, b.ActSeconds, -1, false);
            double turns = end.yaw / (2 * Math.PI);
            Assert.AreEqual(Math.Round(turns), turns, 1e-9, "whole turns");
            Assert.That(Math.Abs(turns), Is.GreaterThanOrEqualTo(1));
            b.Filter = x => false;
            for (int i = 0; i < 100 && b.Resting; i++) b.Tick(.05, true);
            Assert.AreEqual(1, b.Counts[(int)XgSpiderAct.Dance]);
        }

        [Test]
        public void TheCursorOnlyScaresItWhenItRushesAtIt()
        {
            var th = new XgSpiderThreat();
            Assert.IsFalse(th.Update(.016, 0, 0, 100, 0, 150), "first sample");
            Assert.IsFalse(th.Update(.016, 2, 0, 100, 0, 150), "slow");
            th.Reset();
            th.Update(.016, 0, 0, 100, 0, 150);
            Assert.IsTrue(th.Update(.016, 20, 0, 100, 0, 150), "fast and aimed at it");
            Assert.That(th.AwayX, Is.EqualTo(1).Within(1e-9));
            th.Reset(); th.Update(.016, 0, 0, 100, 0, 150);
            Assert.IsFalse(th.Update(.016, -20, 0, 100, 0, 150), "rushing away");
            th.Reset(); th.Update(.016, 0, 0, 100, 0, 150);
            Assert.IsFalse(th.Update(.016, 0, 20, 100, 0, 150), "rushing past, not at it");
            th.Reset(); th.Update(.016, 0, 0, 1000, 0, 150);
            Assert.IsFalse(th.Update(.016, 20, 0, 1000, 0, 150), "far away");
        }

        [Test]
        public void EveryPoseStartsAndEndsNeutralAndStaysInRange()
        {
            foreach (XgSpiderAct a in Enum.GetValues(typeof(XgSpiderAct)))
            {
                if (a == XgSpiderAct.None) continue;
                foreach (bool reduced in new[] { false, true })
                    foreach (int variant in new[] { -1, 1, 2 })
                    {
                        const double seconds = 2.2;
                        foreach (double t in new[] { 0.0, 1.0 })
                        {
                            var p = XgSpiderPoses.Body(a, t, seconds, variant, reduced);
                            string tag = a + " t=" + t + " reduced=" + reduced;
                            double turns = p.yaw / (2 * Math.PI);
                            Assert.AreEqual(Math.Round(turns), turns, 1e-6, tag + " yaw");
                            Assert.AreEqual(0, p.lift, 1e-6, tag + " lift");
                            Assert.AreEqual(0, p.thread, 1e-6, tag + " thread");
                            // Startle begins at its jolt; every other action starts at rest.
                            if (a == XgSpiderAct.Startle && t == 0) continue;
                            Assert.AreEqual(1, p.scale, 1e-6, tag + " scale");
                            Assert.AreEqual(1, p.eyeOpen, 1e-6, tag + " eyes");
                            for (int i = 0; i < 8; i++)
                            {
                                var l = XgSpiderPoses.Leg(i, 8, a, t, seconds, variant, reduced);
                                Assert.AreEqual(0, l.w, 1e-6, tag + " leg " + i);
                            }
                        }
                        for (double t = 0; t <= 1; t += .05)
                        {
                            var p = XgSpiderPoses.Body(a, t, seconds, variant, reduced);
                            Assert.That(p.scale, Is.InRange(.5, 1.5));
                            Assert.That(p.eyeOpen, Is.InRange(0, 1));
                            Assert.That(p.eyeBoost, Is.InRange(0, 1));
                            for (int i = 0; i < 8; i++)
                            {
                                var l = XgSpiderPoses.Leg(i, 8, a, t, seconds, variant, reduced);
                                Assert.That(l.w, Is.InRange(0, 1), a + " leg weight");
                                Assert.That(l.scale, Is.InRange(.2, 2));
                            }
                        }
                    }
            }
        }

        [Test]
        public void ItCurlsUpInSleepAndHangsOnAThread()
        {
            var sleep = XgSpiderPoses.Body(XgSpiderAct.Sleep, .5, 12, 1, false);
            Assert.That(sleep.eyeOpen, Is.LessThan(.3), "eyes closed");
            Assert.That(XgSpiderPoses.Leg(2, 8, XgSpiderAct.Sleep, .5, 12, 1, false).scale, Is.LessThan(.5), "legs folded");
            var hang = XgSpiderPoses.Body(XgSpiderAct.Dangle, .5, 4, 1, false);
            Assert.That(hang.lift, Is.LessThan(-30), "dropped down the thread");
            Assert.That(hang.thread, Is.GreaterThan(.9));
            var crouch = XgSpiderPoses.Body(XgSpiderAct.Think, .5, 2, 1, false);
            Assert.That(crouch.scale, Is.LessThan(1), "crouching");
            Assert.That(XgSpiderPoses.Leg(3, 8, XgSpiderAct.Think, .5, 2, 1, false).scale, Is.GreaterThan(1.1), "legs spread");
        }
    }
}
