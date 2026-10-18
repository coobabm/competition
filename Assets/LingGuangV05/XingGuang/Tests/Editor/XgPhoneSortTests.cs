using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>阿杰的手机备份 (XgSim.PhoneSort.cs): the end of stage 2, 4,312 pictures into eight folders.</summary>
    public sealed class XgPhoneSortTests
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

        /// <summary>A lab in stage 2 with both bars of 「看图说词」 at <paramref name="ready"/>.</summary>
        static XgSim Lab(double ready, int salt = 0)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.abilitiesEmerged.Add(2);
            sim.S.stage = 2;
            sim.S.dataSalt = salt;
            sim.S.stageEpochs = 1;
            sim.S.trainedParamsK = sim.ParamsThreshold(sim.NextAbility) * ready;
            SetSamples(sim, ready);
            Assert.That(sim.NextAbility, Is.EqualTo(3));
            return sim;
        }

        static void PlayThrough(XgSim sim, bool tellIds = true, bool script = true)
        {
            sim.PhoneSortStart();
            sim.PhoneSortRun();
            sim.PhoneIds(tellIds);
            Assert.That(sim.PhoneHandBack(), Is.True);
            Assert.That(sim.PhoneAlibi(script), Is.Not.Empty);
        }

        [Test]
        public void ItComesWithTheExamAndHoldsTheChapter()
        {
            Assert.That(Lab(.7).PhoneSortDue, Is.False, "the exam is not open yet");
            var sim = Lab(1.25);
            Assert.That(sim.PhoneSortDue, Is.True);
            Assert.That(sim.PhoneSortHolds, Is.False, "without the desktop nothing waits");
            sim.DesktopHosted = true;
            Assert.That(sim.ChapterSceneHolds, Is.True);
            Assert.That(sim.TakeExam(), Is.Null);
            Assert.That(XgGuide.ConfirmStage(sim).zh, Does.Contain("阿杰"));
            PlayThrough(sim);
            Assert.That(sim.S.psStep, Is.EqualTo(4));
            Assert.That(sim.ChapterSceneHolds, Is.False);
            Assert.That(sim.PhoneSortDue, Is.False, "once per save");
            var e = sim.TakeExam();
            Assert.That(e, Is.Not.Null);
            sim.RevealExam();
            Assert.That(sim.ConfirmNextStage(), Is.True);
            Assert.That(sim.HasAbility(3), Is.True);
        }

        [Test]
        public void TheStoryMomentsHappenWhateverTheTraining()
        {
            foreach (double ready in new[] { .8, 1.25 })
            {
                var sim = Lab(ready);
                sim.PhoneSortStart();
                sim.PhoneSortRun();
                Assert.That(sim.PhonePick("panda"), Is.EqualTo("meme"));
                Assert.That(sim.PhonePick("ow1"), Is.EqualTo("game"));
                Assert.That(sim.PhonePick("catear"), Is.EqualTo("meme"), "阿杰's cat-ear selfie is filed as a meme");
                Assert.That(sim.PhonePick("beach"), Is.EqualTo("other"), "sideways, it does not know it");
                Assert.That(sim.PhonePick("idcard"), Is.EqualTo("other"));
                Assert.That(XgSim.PhoneShot("beach").sideways, Is.True);
            }
        }

        [Test]
        public void TheAccuracyFollowsTheSampleBar()
        {
            var weak = Lab(.8); weak.PhoneSortStart(); weak.PhoneSortRun();
            var strong = Lab(1.25); strong.PhoneSortStart(); strong.PhoneSortRun();
            Assert.That(strong.PhoneRightPct, Is.GreaterThan(weak.PhoneRightPct));
            Assert.That(strong.PhoneRightPct, Is.InRange(90, 96));
            Assert.That(weak.PhoneRightPct, Is.InRange(65, 85));
        }

        [Test]
        public void TheFoldersAddUpToEveryPicture()
        {
            foreach (double ready in new[] { .8, 1, 1.25 })
            {
                var sim = Lab(ready);
                sim.PhoneSortStart(); sim.PhoneSortRun();
                int sum = XgSim.PhoneFolders.Sum(f => sim.PhoneCount(f.id));
                Assert.That(sum, Is.InRange(XgSim.PhoneTotal - 4, XgSim.PhoneTotal + 4), "rounding only");
                Assert.That(sim.PhoneCount("meme"), Is.GreaterThan(sim.PhoneCount("pet")), "memes are most of 阿杰's phone");
            }
            Assert.That(XgSim.PhoneFolders.Sum(f => f.share), Is.EqualTo(1).Within(1e-9));
            Assert.That(XgSim.PhoneFolders.Length, Is.EqualTo(8));
        }

        [Test]
        public void MovingAWrongOneTeachesItOnce()
        {
            var sim = Lab(.8);
            sim.PhoneSortStart(); sim.PhoneSortRun();
            var wrong = sim.PhoneWrong();
            Assert.That(wrong.Select(s => s.id), Does.Contain("catear").And.Contain("beach"));
            int right = sim.S.psRight;
            double labels = sim.TotalLabels;
            Assert.That(sim.PhoneMove("catear", "meme"), Is.False, "the wrong folder again changes nothing");
            Assert.That(sim.PhoneMove("catear", "selfie"), Is.True);
            Assert.That(sim.PhoneMove("catear", "selfie"), Is.False, "once");
            Assert.That(sim.PhonePick("catear"), Is.EqualTo("selfie"));
            Assert.That(sim.S.psRight, Is.EqualTo(right + 1));
            Assert.That(sim.TotalLabels, Is.EqualTo(labels + 1), "a label for its seeing side");
            Assert.That(sim.PhoneWrong().Select(s => s.id), Does.Not.Contain("catear"));
        }

        [Test]
        public void ItCannotBeHandedBackBeforeTheIdCards()
        {
            var sim = Lab(1);
            sim.PhoneSortStart(); sim.PhoneSortRun();
            Assert.That(sim.PhoneHandBack(), Is.False, "其他's ID cards first");
            sim.PhoneIds(false);
            Assert.That(sim.S.psIds, Is.EqualTo(2));
            Assert.That(sim.PhoneHandBack(), Is.True);
        }

        [Test]
        public void TheChronicleTellsWhatHappened()
        {
            var told = Lab(1); PlayThrough(told, true, false);
            var c = told.Chronicle().First(x => x.id == XgSim.PhoneChronicle);
            Assert.That(c.body, Does.Contain("身份证").And.Contain("手分的").And.Contain("4,312"));
            var quiet = Lab(1); PlayThrough(quiet, false, true);
            Assert.That(quiet.Chronicle().First(x => x.id == XgSim.PhoneChronicle).body, Does.Not.Contain("身份证").And.Contain("小脚本"));
        }

        [Test]
        public void EveryPictureHasAFolderAndAFileAndNoBannedWords()
        {
            foreach (var s in XgSim.PhoneShots)
            {
                Assert.That(XgSim.PhoneFolder(s.folder), Is.Not.Null, s.id);
                Assert.That(s.file, Is.Not.Empty, s.id);
                Assert.That(s.photo.Length > 0 ^ s.draw.Length > 0, Is.True, s.id + ": a photo or a drawing");
                if (s.fixedPick.Length > 0) Assert.That(XgSim.PhoneFolder(s.fixedPick), Is.Not.Null, s.id);
                Assert.That(s.file, Does.Not.Contain("灵光").And.Not.Contain("涌现"));
            }
            Assert.That(XgSim.PhoneShots.Select(s => s.id).Distinct().Count(), Is.EqualTo(XgSim.PhoneShots.Length));
        }

        [Test]
        public void LaoZhouFollowsTheRoadItChose()
        {
            var sim = Lab(1);
            sim.S.firstTrack = "vision";
            Assert.That(sim.ZhouStageNoteNow(2).zh, Does.Contain("卷积").And.Contain("笔画"));
            sim.S.firstTrack = "sequence";
            var reading = sim.ZhouStageNoteNow(2);
            Assert.That(reading.zh, Does.Contain("循环网络").And.Not.Contain("卷积"));
            Assert.That(reading.replies[0].answerZh, Does.Contain("循环网络"));
        }
    }
}
