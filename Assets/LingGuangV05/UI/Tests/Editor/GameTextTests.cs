using System;
using System.Linq;
using System.Text.RegularExpressions;
using LingGuangV05.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.UI.Tests
{
    public class GameTextTests
    {
        [TestCase(null)] [TestCase("")] [TestCase("de-DE")] [TestCase("ko-KR")]
        public void MissingOrUnsupportedChoiceFallsBackToChinese(string value)
        { using (GameText.Bind(() => value)) Assert.That(GameText.T("中文", "English"), Is.EqualTo("中文")); }
        [Test]
        public void BindingReadsLiveNativeLocaleAndRestoresPreviousProvider()
        {
            string locale = "zh-CN";
            using (GameText.Bind(() => locale))
            {
                Assert.That(GameText.LanguageId, Is.EqualTo("zh-CN"));
                locale = "en-US";
                Assert.That(GameText.LanguageId, Is.EqualTo("en-US"));
                using (GameText.Bind(() => "zh-CN")) Assert.That(GameText.IsEnglish, Is.False);
                Assert.That(GameText.IsEnglish, Is.True);
            }
        }
        [Test]
        public void UnknownContentAndNameArgumentsAreNeverRewritten()
        {
            using (GameText.Bind(() => "en-US"))
            {
                Assert.That(GameText.Source("user raw 家庭 <tag>"), Is.EqualTo("user raw 家庭 <tag>"));
                Assert.That(GameText.F("名字：{0}", "Name: {0}", "家庭"), Is.EqualTo("Name: 家庭"));
            }
        }
        [Serializable] private class Catalog { public Entry[] entries; }
        [Serializable] private class Entry { public string zh; public string en; }
        [Test]
        public void EveryCatalogPairIsNonemptyAndPreservesNumericPlaceholders()
        {
            foreach (var asset in Resources.LoadAll<TextAsset>("Localization"))
            {
                if (!asset.text.TrimStart().StartsWith("{")) continue;
                var catalog = JsonUtility.FromJson<Catalog>(asset.text);
                Assert.That(catalog.entries, Is.Not.Null, asset.name);
                foreach (var entry in catalog.entries)
                {
                    Assert.That(entry.zh, Is.Not.Empty, asset.name);
                    Assert.That(entry.en, Is.Not.Empty, asset.name + ": " + entry.zh);
                    string[] Tokens(string text) => Regex.Matches(text, @"\{\d+\}").Cast<Match>().Select(m => m.Value).Distinct().OrderBy(s => s).ToArray();
                    Assert.That(Tokens(entry.en), Is.EqualTo(Tokens(entry.zh)), asset.name + ": " + entry.zh);
                }
            }
        }
    }
}
