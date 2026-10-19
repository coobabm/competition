using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public class XgOriginCutTests
    {
        const double Eps = 1e-9;

        [Test] public void ShotsTileTheCutOnBarLines()
        {
            var s = XgOriginCut.Shots;
            Assert.AreEqual(0, s[0].From, Eps);
            Assert.AreEqual(XgOriginCut.Duration, s[s.Length - 1].To, Eps);
            Assert.LessOrEqual(XgOriginCut.Duration, 90, "the cut stays within a minute and a half");
            for (int i = 0; i < s.Length; i++)
            {
                Assert.Greater(s[i].ToBar, s[i].FromBar, "shot " + i);
                Assert.Greater(s[i].FilmTo, s[i].FilmFrom, "shot " + i + " runs forward");
                if (i > 0)
                {
                    Assert.AreEqual(s[i - 1].ToBar, s[i].FromBar, Eps, "shot " + i + " starts where the last ended");
                    Assert.GreaterOrEqual(s[i].FilmFrom, s[i - 1].FilmTo - Eps, "film time never runs back at shot " + i);
                }
                Assert.AreEqual(System.Math.Round(s[i].FromBar), s[i].FromBar, Eps, "shot " + i + " starts on a bar line");
            }
        }

        [Test] public void FilmTimeCoversTheWholeFilm()
        {
            Assert.AreEqual(0, XgOriginCut.FilmTime(0), Eps);
            Assert.AreEqual(XgOriginFilm.Duration, XgOriginCut.FilmTime(XgOriginCut.Duration), Eps);
            double last = -1;
            for (double t = 0; t <= XgOriginCut.Duration; t += .05)
            {
                double f = XgOriginCut.FilmTime(t);
                Assert.GreaterOrEqual(f, last, "film time at " + t);
                last = f;
            }
        }

        [Test] public void EveryCapabilityJumpLandsOnABarLine()
        {
            int found = 0;
            foreach (double b in XgOriginFilm.Breaks)
                foreach (var s in XgOriginCut.Shots)
                    if (System.Math.Abs(s.FilmFrom - b) < Eps) { found++; break; }
            // The first jump (是/否) rides inside its shot; the other five open a shot on a hit.
            Assert.AreEqual(5, found);
        }

        [Test] public void EveryKeptCaptionIsShownWhole()
        {
            int shown = 0;
            for (int c = 0; c < XgOriginFilm.Captions.Length; c++)
            {
                var cap = XgOriginFilm.Captions[c];
                bool any = false, whole = false;
                foreach (var s in XgOriginCut.Shots)
                {
                    if (s.FilmTo <= cap.From || s.FilmFrom >= cap.To) continue;
                    any = true;
                    if (s.FilmFrom <= cap.From + .25 && s.FilmTo >= cap.To - .25) whole = true;
                }
                // A caption split across two shots that run on without a jump also counts as whole.
                if (any && !whole)
                {
                    double from = double.MaxValue, to = double.MinValue;
                    for (int i = 0; i < XgOriginCut.Shots.Length; i++)
                    {
                        var s = XgOriginCut.Shots[i];
                        if (s.FilmTo <= cap.From || s.FilmFrom >= cap.To) continue;
                        from = System.Math.Min(from, s.FilmFrom); to = System.Math.Max(to, s.FilmTo);
                    }
                    whole = from <= cap.From + .25 && to >= cap.To - .25;
                }
                if (any) { shown++; Assert.IsTrue(whole, "caption " + c + " 「" + cap.Zh + "」 is cut short"); }
            }
            Assert.GreaterOrEqual(shown, 20);
        }

        [Test] public void FlashAndKickStayInRange()
        {
            for (double t = 0; t < XgOriginCut.Duration; t += .01)
            {
                Assert.That(XgOriginCut.Flash(t), Is.InRange(0.0, 1.0));
                Assert.That(XgOriginCut.Kick(t), Is.InRange(0.0, 1.0));
            }
            Assert.Greater(XgOriginCut.Flash(16 * XgOriginCut.Bar), .9, "the drop flashes");
            Assert.AreEqual(0, XgOriginCut.Kick(4 * XgOriginCut.Bar), Eps, "no kick in the build");
        }
    }
}
