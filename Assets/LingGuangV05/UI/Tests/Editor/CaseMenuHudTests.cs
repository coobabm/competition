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
        public void MainTitleUsesApprovedTransparentArtworkWithoutDuplicateDecoration()
        {
            BuildHud();
            var logo = host.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).Single(image => image.name == "TitleLogoV9");
            var approved = Resources.Load<Texture2D>("LingGuangV05/Branding/TitleLogoV9");
            Assert.That(approved, Is.Not.Null);
            Assert.That(logo.texture, Is.SameAs(approved));
            Assert.That(approved.width, Is.EqualTo(1775));
            Assert.That(approved.height, Is.EqualTo(1040));
            Assert.That(approved.width / (float)approved.height, Is.LessThan(2f),
                "the two-line clean lockup is the in-game title; the single-line store art is not");
            Assert.That(logo.raycastTarget, Is.False);
            Assert.That(logo.color, Is.EqualTo(Color.white));
            Assert.That(logo.rectTransform.rect.width, Is.EqualTo(500f));
            Assert.That(logo.rectTransform.rect.height, Is.EqualTo(500f * 1040f / 1775f).Within(.01f));
            Assert.That(logo.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(65f, -98f)));
            Assert.That(logo.rectTransform.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(logo.rectTransform.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(logo.rectTransform.anchoredPosition.x + logo.rectTransform.rect.width, Is.LessThanOrEqualTo(715f),
                "the lockup must stay clear of the Turing poster's left edge in ComputerCaseMenu");
            Assert.That(logo.uvRect, Is.EqualTo(new Rect(0, 0, 1, 1)));
            Assert.That(host.GetComponentsInChildren<Transform>(true).Any(item => item.name == "TitleSignal"), Is.False);
            Assert.That(host.GetComponentsInChildren<TextMeshProUGUI>(true)
                .Single(label => label.name == "Eyebrow").text, Does.Contain("MY COMPUTER IS ALIVE"));
        }
        [Test]
        public void MainTitleLineBoxDoesNotOverlapItsSubheading()
        {
            BuildHud();
            var labels = host.GetComponentsInChildren<TextMeshProUGUI>(true);
            var title = host.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).Single(image => image.name == "TitleLogoV9").rectTransform;
            var subheading = labels.Single(label => label.name == "Subheading").rectTransform;
            float titleBottom = title.anchoredPosition.y - title.rect.height;
            Assert.That(titleBottom, Is.GreaterThanOrEqualTo(subheading.anchoredPosition.y + 8f),
                "The title artwork must retain at least 8 reference pixels above its subheading");
            Assert.That(subheading.anchoredPosition.y, Is.EqualTo(-398.96f).Within(.05f),
                "The subheading follows the v9 lockup height (500 x 292.96), not the retired 500 x 267 v5 block");
        }

        private void BuildHud()
        {
            Assert.That(Resources.Load<Texture2D>("LingGuangV05/Branding/TitleLogoV9"), Is.Not.Null, "Approved v9 Resources texture must load before HUD construction");
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
