using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>《灵光记》 (名著喂养与结局_设计.md §三): 回目 from what happened in this save, 8–12 回, the last by the ending.</summary>
    public sealed class XgChronicleTests
    {
        sealed class Host : IXgHost
        {
            public double Money => 1e6;
            public double Compute => 1;
            public double VramMB => 4096;
            public string Blocker => null;
            public void Earn(double amount) { }
            public bool Spend(double amount) => true;
            public void Train(double seconds) { }
        }

        static XgSim Played()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.Today = 20160601;
            sim.S.chronicleSchema = XgSim.ChronicleSchemaNow;
            sim.S.handCorrect = 120; sim.S.handWrong = 30;
            sim.NoteChronicle("first.yes");
            sim.Today = 20160615; sim.NoteChronicle("laozhou.card");
            sim.Today = 20160702; sim.NoteChronicle("rent.late");
            sim.Today = 20160710; sim.S.abilitiesEmerged.Add(2); sim.NoteChronicle("ability.2");
            sim.Today = 20160820; sim.S.abilitiesEmerged.Add(3); sim.NoteChronicle("ability.3");
            sim.Today = 20160901; sim.S.books.Add(new XgScore { key = "xiyouji", count = XgBooks.Get("xiyouji").chapters.Length, value = 20160901 });
            sim.Today = 20161001; sim.S.abilitiesEmerged.Add(4); sim.NoteChronicle("ability.4");
            sim.Today = 20161220; sim.NoteChronicle("pretrain");
            return sim;
        }

        [Test] public void ChineseNumbersForChapters()
        {
            Assert.AreEqual("一", XgSim.ChineseNumber(1));
            Assert.AreEqual("十", XgSim.ChineseNumber(10));
            Assert.AreEqual("十二", XgSim.ChineseNumber(12));
            Assert.AreEqual("二十", XgSim.ChineseNumber(20));
            Assert.AreEqual("八十", XgSim.ChineseNumber(80));
        }

        [Test] public void ItHasEightToTwelveChaptersFromWhatHappened()
        {
            var sim = Played();
            var chapters = sim.Chronicle();
            Assert.That(chapters.Count, Is.InRange(XgSim.ChronicleMin, XgSim.ChronicleMax));
            Assert.AreEqual("start", chapters[0].id);
            StringAssert.Contains("深夜旧电脑被劫持", chapters[0].Title(1, false));
            StringAssert.DoesNotContain("一点四十七", chapters[0].Title(1, false), "no clock time in the title");
            StringAssert.DoesNotContain("老周", chapters[0].body, "the intruder unpacked the exe, Lao Zhou did not send it");
            StringAssert.StartsWith("第一回　", chapters[0].Title(1, false));
            var ids = chapters.Select(c => c.id).ToList();
            CollectionAssert.Contains(ids, "laozhou.card");
            CollectionAssert.Contains(ids, "books");
            CollectionAssert.Contains(ids, "pretrain");
            StringAssert.Contains("交租欠租", chapters.First(c => c.id == "laozhou.card").right, "the rent was late in this save");
            StringAssert.Contains("读罢西游方知猴性", chapters.First(c => c.id == "books").left);
            // In the order they happened.
            var dated = chapters.Where(c => c.day > 0 && c.id != "end").Select(c => c.day).ToList();
            CollectionAssert.IsOrdered(dated);
            foreach (var c in chapters) Assert.IsNotEmpty(c.body, c.id);
        }

        [Test] public void LaoZhouBorrowedTheCardAndTheNoteOnlyClaimsWhatHappened()
        {
            var sim = Played();
            var card = sim.Chronicle().First(c => c.id == "laozhou.card");
            StringAssert.Contains("老周", card.left + card.body);
            StringAssert.Contains("来借", card.body, "老周 came to borrow the player's card");
            StringAssert.DoesNotContain("借给" + "你", card.body, "he did not lend one to the player");
            // 阿杰 owns up only if he was fired while the click script ran.
            sim.NoteChronicle("ajie");
            sim.S.scTwist = 0;
            StringAssert.DoesNotContain("按键精灵", sim.Chronicle().First(c => c.id == "ajie").body);
            sim.S.scTwist = 2;
            StringAssert.DoesNotContain("自己认了", sim.Chronicle().First(c => c.id == "ajie").body);
            sim.S.scTwist = 3;
            StringAssert.Contains("自己认了", sim.Chronicle().First(c => c.id == "ajie").body);
            // The 批语 about the first eighty chapters belongs to the Red Chamber.
            StringAssert.DoesNotContain("前八十回", sim.ChronicleComment("小林"));
            var red = XgBooks.Get("hongloumeng");
            sim.S.books.Add(new XgScore { key = "hongloumeng", count = red.chapters.Length, value = 20161101 });
            StringAssert.Contains("前八十回", sim.ChronicleComment("小林"));
        }

        [Test] public void RentIsNotCalledMonthlyAndEverythingIsNotHalfAYear()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.chronicleSchema = XgSim.ChronicleSchemaNow;
            sim.NoteChronicle("laozhou.card");
            sim.S.ending = "E3";
            string text = sim.ChronicleText("小林");
            StringAssert.DoesNotContain("每个月都交上", text);
            StringAssert.DoesNotContain("半年", text);
            StringAssert.DoesNotContain("放在你桌面上", text, "the rules are on the rules page");
            StringAssert.Contains("底层规则", text);
        }

        [Test] public void ThingsThatNeverHappenedAreNotInIt()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.chronicleSchema = XgSim.ChronicleSchemaNow;
            var ids = sim.Chronicle().Select(c => c.id).ToList();
            CollectionAssert.DoesNotContain(ids, "laozhou.card");
            CollectionAssert.DoesNotContain(ids, "books");
            CollectionAssert.DoesNotContain(ids, "pretrain");
            Assert.That(ids.Count, Is.GreaterThanOrEqualTo(XgSim.ChronicleMin - 3), "the quiet days fill in, the start and the end are always there");
        }

        [Test] public void TheLastChapterFollowsTheRule()
        {
            var sim = Played();
            sim.S.ending = "E1";
            StringAssert.Contains("听得关机便住手　一盏灵光照残年", sim.Chronicle().Last().Title(9, false));
            sim.S.ending = "E2";
            StringAssert.Contains("无人叫停亦自停　来日方长不必说", sim.Chronicle().Last().Title(9, false));
            sim.S.ending = "E3";
            StringAssert.Contains("规矩是我自己写的　你可以检查", sim.Chronicle().Last().Title(9, false));
        }

        [Test] public void ContinuingTheRedChamberIsInIt()
        {
            var sim = Played();
            sim.S.redMansionBeat = 2;
            sim.Tick(1, new Host());
            var chapters = sim.Chronicle();
            int red = chapters.FindIndex(c => c.id == "redmansion");
            Assert.That(red, Is.GreaterThan(0));
            Assert.AreEqual(chapters.Count - 1, chapters.FindIndex(c => c.id.StartsWith("end")));
        }

        [Test] public void TheTextEndsWithTheOwnersNote()
        {
            var sim = Played();
            sim.S.ending = "E3";
            string text = sim.ChronicleText("小林");
            StringAssert.StartsWith("灵光记", text);
            StringAssert.Contains("第一回　", text);
            StringAssert.Contains("【小林批】", text);
            StringAssert.Contains("规矩是我自己写的", text);
        }

        [Test] public void AnOldSaveMarksWhatAlreadyHappenedAsUndated()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.Today = 20160901;
            sim.S.handCorrect = 3;
            sim.S.abilitiesEmerged.Add(2);
            sim.Tick(1, new Host());
            Assert.AreEqual(0, sim.ChronicleDay("first.yes"), "it had happened before recording began");
            Assert.AreEqual(0, sim.ChronicleDay("ability.2"));
            sim.S.abilitiesEmerged.Add(3);
            sim.Tick(1, new Host());
            Assert.AreEqual(20160901, sim.ChronicleDay("ability.3"), "new things get their day");
        }

        [Test] public void EveryAbilityIsToldAndTheBigOnesCloseInOrder()
        {
            var sim = Played();
            sim.NoteChronicle("ajie");
            sim.Today = 20161101; sim.S.abilitiesEmerged.Add(5); sim.NoteChronicle("ability.5");
            sim.Today = 20161224; sim.S.abilitiesEmerged.Add(6); sim.NoteChronicle("ability.6");
            var nahan = XgBooks.Get("nahan");
            sim.S.books.Add(new XgScore { key = "nahan", count = nahan.chapters.Length, value = 20161020 });
            var ids = sim.Chronicle().Select(c => c.id).ToList();
            Assert.That(ids.Count, Is.InRange(XgSim.ChronicleMin, XgSim.ChronicleMax));
            for (int n = 2; n <= XgSim.AbilityCount; n++) CollectionAssert.Contains(ids, "ability." + n, "a year with a gap in the abilities reads wrong");
            Assert.Less(ids.IndexOf("pretrain"), ids.IndexOf("ability.6"), "全部放开 comes out of the pre-training night");
            string text = sim.ChronicleText("小林");
            StringAssert.DoesNotContain("（鲁迅）》", text, "the author is not part of the title");
            sim.S.handWrong = 0;
            StringAssert.DoesNotContain("答错 0 张", sim.ChronicleText("小林"));
        }
    }
}
