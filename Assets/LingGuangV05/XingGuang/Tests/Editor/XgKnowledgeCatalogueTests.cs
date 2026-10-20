using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgKnowledgeCatalogueTests
    {
        [Test] public void DesignCatalogueHasCompleteBilingualEntriesAndResolvableLinks()
        {
            Assert.AreEqual(91, XgKnowledge.Entries.Length);
            var keys = new HashSet<string>();
            int hardware = 0;
            foreach (var entry in XgKnowledge.Entries)
            {
                Assert.IsTrue(keys.Add(entry.Key), entry.Key);
                Assert.IsNotEmpty(entry.English, entry.Key);
                Assert.IsNotEmpty(entry.ChineseBody, entry.Key);
                Assert.IsNotEmpty(entry.EnglishBody, entry.Key);
                Assert.That(entry.Category, Is.InRange(0, 3));
                Assert.IsNotEmpty(entry.Related, entry.Key);
                foreach (var related in entry.Related) Assert.IsNotNull(XgKnowledge.Find(related), entry.Key + " -> " + related);
                if (entry.Category == 3) hardware++;
            }
            Assert.AreEqual(24, hardware);
        }
        [Test] public void SearchHandlesBothLanguagesWhitespaceAndCase()
        {
            var compute = XgKnowledge.Find("算力");
            Assert.IsTrue(compute.Matches("算力"));
            Assert.IsTrue(compute.Matches("  COMPUTE  "));
            Assert.IsTrue(compute.Matches("bandwidth"));
            Assert.IsTrue(compute.Matches(" "));
            Assert.IsFalse(compute.Matches("no_matching_term_123"));
        }
        [Test] public void EraAndScalingCaveatsArePresentInBothLanguages()
        {
            StringAssert.Contains("不保证", XgKnowledge.Find("算力").ChineseBody);
            StringAssert.Contains("does not guarantee", XgKnowledge.Find("算力").EnglishBody);
            foreach (var key in new[] { "多路注意", "位置标记", "自研新脑" })
            {
                StringAssert.Contains("2017", XgKnowledge.Find(key).ChineseBody);
                StringAssert.Contains("2017", XgKnowledge.Find(key).EnglishBody);
            }
        }
    }
}
