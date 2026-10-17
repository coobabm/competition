using System.Collections.Generic;
using LingGuangV05.Core.Era;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>科技页「说人话」: every tech-tree node ends with one plain line, in 2016 register.</summary>
    public sealed class XgTechPlainTests
    {
        static IEnumerable<XgNode> TechNodes()
        {
            foreach (var n in XgCatalog.Nodes) if (n.tree != "atlas") yield return n;
        }

        [Test]
        public void EveryTechNodeHasAChineseAndEnglishLine()
        {
            int count = 0;
            foreach (var n in TechNodes())
            {
                Assert.IsTrue(XgTechPlain.TryGet(n, out string zh, out string en), n.id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(zh), n.id + " zh");
                Assert.IsFalse(string.IsNullOrWhiteSpace(en), n.id + " en");
                count++;
            }
            Assert.Greater(count, 80);
        }

        [Test]
        public void EverySquareOnThePageHasALine()
        {
            foreach (var e in XgTechTree.Entries)
            {
                if (XgSim.IsAtlas(e.First)) continue;
                foreach (var m in e.members) Assert.IsTrue(XgTechPlain.TryGet(m, out _, out _), e.id + " / " + m.id);
            }
        }

        [Test]
        public void AtlasNodesAreNotTechNodes()
        {
            foreach (var n in XgCatalog.Nodes)
                if (n.tree == "atlas") Assert.IsFalse(XgTechPlain.TryGet(n, out _, out _), n.id);
        }

        [Test]
        public void EveryTechNodeIsHandWritten()
        {
            // The generic fallback exists for nodes added later; the shipped catalog must not rely on it.
            foreach (var n in TechNodes()) Assert.IsTrue(XgTechPlain.IsAuthored(n), n.id + " has only the fallback line");
        }

        [Test]
        public void LinesAreShortPlainAndNeverSayYongxian()
        {
            foreach (var n in TechNodes())
            {
                XgTechPlain.TryGet(n, out string zh, out string en);
                StringAssert.DoesNotContain("涌现", zh, n.id);
                StringAssert.DoesNotContain("涌现", en, n.id);
                StringAssert.DoesNotContain("<", zh, n.id + " rich-text safe");
                StringAssert.DoesNotContain("<", en, n.id + " rich-text safe");
                Assert.LessOrEqual(zh.Length, 34, n.id + " zh too long: " + zh);
                Assert.LessOrEqual(en.Length, 110, n.id + " en too long: " + en);
            }
        }

        [Test]
        public void LinesUseNoPost2016Slang()
        {
            foreach (var n in TechNodes())
            {
                XgTechPlain.TryGet(n, out string zh, out string en);
                Assert.IsNull(EraLexicon.FirstBanned(zh), n.id + ": " + zh);
                Assert.IsNull(EraLexicon.FirstBanned(en), n.id + ": " + en);
                foreach (var word in new[] { "绝绝子", "内卷", "躺平", "yyds", "凡尔赛", "抖音", "拼多多", "破防", "栓Q" })
                    StringAssert.DoesNotContain(word, zh, n.id);
            }
        }

        [Test]
        public void PrefixesAreLocalised()
        {
            Assert.AreEqual("说人话：", XgTechPlain.Prefix(false));
            Assert.AreEqual("In plain words: ", XgTechPlain.Prefix(true));
        }
    }
}
