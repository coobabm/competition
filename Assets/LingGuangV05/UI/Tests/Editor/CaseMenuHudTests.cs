using System;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace LingGuangV05.UI.Tests
{
    /// <summary>Uses the shipped CJK metrics rather than assuming a font size is its line height.</summary>
    public sealed class CaseMenuHudTests
    {
        private GameObject host;

        [TearDown]
        public void TearDown()
        {
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
        }

        [TestCase("Title", "我的电脑活了")]
        [TestCase("SettingsTitle", "设置")]
        public void FixedTitlesFitTheShippedCjkLineBoxAndRenderVisibleCharacters(string name, string expectedText)
        {
            BuildHud();
            var title = host.GetComponentsInChildren<TextMeshProUGUI>(true).Single(label => label.name == name);
            title.ForceMeshUpdate(true, true);
            float preferredHeight = title.GetPreferredValues(title.text, title.rectTransform.rect.width, Mathf.Infinity).y;

            Assert.That(title.text, Is.EqualTo(expectedText));
            Assert.That(title.rectTransform.rect.height, Is.GreaterThanOrEqualTo(preferredHeight),
                name + " must fit Desktop CJK's full line box");
            Assert.That(title.overflowMode, Is.EqualTo(TextOverflowModes.Overflow),
                name + " is a fixed title, not a truncatable user-supplied string");
            Assert.That(title.isTextOverflowing, Is.False, name + " must not rely on overflow to fit");
            Assert.That(title.textInfo.characterCount, Is.GreaterThan(0), name + " must emit characters");
            Assert.That(title.textInfo.characterInfo.Take(title.textInfo.characterCount).Any(character => character.isVisible),
                Is.True, name + " must emit visible glyph geometry");
        }

        [Test]
        public void MainTitleLineBoxDoesNotOverlapItsSubheading()
        {
            BuildHud();
            var labels = host.GetComponentsInChildren<TextMeshProUGUI>(true);
            var title = labels.Single(label => label.name == "Title").rectTransform;
            var subheading = labels.Single(label => label.name == "Subheading").rectTransform;
            float titleBottom = title.anchoredPosition.y - title.rect.height;
            Assert.That(titleBottom, Is.GreaterThanOrEqualTo(subheading.anchoredPosition.y + 8f),
                "The expanded title line box must retain at least 8 reference pixels above its subheading");
        }

        private void BuildHud()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset");
            Assert.That(font, Is.Not.Null, "Regression must use the actual shipped menu font");
            var type = Type.GetType("LingGuangV05.CaseMenu.CaseMenuHud, Assembly-CSharp");
            Assert.That(type, Is.Not.Null);
            host = new GameObject("CaseMenuHudTest");
            var hud = host.AddComponent(type);
            var build = type.GetMethod("Build");
            var showSettings = type.GetMethod("ShowSettings");
            Assert.That(build, Is.Not.Null);
            Assert.That(showSettings, Is.Not.Null);
            build.Invoke(hud, new object[] { font, null, null, null, null, null });
            showSettings.Invoke(hud, new object[] { .75f, false, false });
            Canvas.ForceUpdateCanvases();
        }
    }
}
