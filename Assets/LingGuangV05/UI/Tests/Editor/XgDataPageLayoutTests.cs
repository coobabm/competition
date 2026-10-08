using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LingGuangV05.UI.Tests
{
    /// <summary>Exercise the data page's real row builders with the shipped desktop font.</summary>
    public sealed class XgDataPageLayoutTests
    {
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject canvasObject;
        Scene previousScene, layoutScene;

        [SetUp]
        public void SetUp()
        {
            previousScene = SceneManager.GetActiveScene();
            layoutScene = default;
            if (string.IsNullOrEmpty(previousScene.path)) return; // Unity Test Runner already owns this unsaved scratch scene.
            layoutScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(layoutScene);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (canvasObject != null) UnityEngine.Object.DestroyImmediate(canvasObject);
            }
            finally
            {
                if (layoutScene.IsValid() && layoutScene.isLoaded) EditorSceneManager.CloseScene(layoutScene, true);
                if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            }
        }

        [TestCase(false, 1064f, "MNIST 手写数字", 16f)]
        [TestCase(false, 1748f, "MNIST 手写数字", 16f)]
        [TestCase(true, 1064f, "手写数字公开包", 15f)]
        [TestCase(true, 1748f, "手写数字公开包", 15f)]
        public void ShortNamesFitShippedCjkLineBoxAndRenderEveryGlyph(
            bool offer, float pageWidth, string text, float fontSize)
        {
            TMP_Text title = BuildTitle(offer, pageWidth, text);
            title.ForceMeshUpdate(true, true);
            float needed = title.GetPreferredValues(text, title.rectTransform.rect.width, Mathf.Infinity).y;
            string context = (offer ? "Offer" : "Dataset") + " name at page width " + pageWidth;

            Assert.That(title.rectTransform.rect.height, Is.GreaterThanOrEqualTo(needed),
                context + " must fit Desktop CJK's full line box, not only its font size");
            Assert.That(title.fontSize, Is.EqualTo(fontSize), context + " must retain its readable font size");
            Assert.That(title.enableAutoSizing, Is.False, context + " must not shrink to hide a layout error");
            Assert.That(title.overflowMode, Is.EqualTo(TextOverflowModes.Ellipsis),
                context + " must retain horizontal truncation for genuinely long names");
            Assert.That(title.isTextOverflowing, Is.False, context + " must fit without overflow");
            Assert.That(title.textInfo.characterCount, Is.EqualTo(text.Length),
                context + " must not be cleared or replaced by an ellipsis");
            int visible = title.textInfo.characterInfo.Take(title.textInfo.characterCount)
                .Count(character => character.isVisible);
            Assert.That(visible, Is.EqualTo(text.Count(character => !char.IsWhiteSpace(character))),
                context + " must emit visible glyph geometry for the whole short name");
        }

        TMP_Text BuildTitle(bool offer, float pageWidth, string text)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset");
            Assert.That(font, Is.Not.Null, "Use the actual font that exposed the first-character overflow");
            Type pageType = FindDesktopType("XgDataPage");
            Type uiType = FindDesktopType("XgUi");
            object ui = Activator.CreateInstance(uiType, new object[] { font, null, true });
            object page = Activator.CreateInstance(pageType);
            pageType.GetMethod("Init").Invoke(page, new[] { null, ui });

            canvasObject = new GameObject("Data page layout test", typeof(RectTransform), typeof(Canvas));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var area = (RectTransform)canvasObject.transform;
            area.sizeDelta = new Vector2(pageWidth, 600f);
            pageType.GetMethod("Build").Invoke(page, new object[] { area });

            MethodInfo makeRow = pageType.GetMethod(offer ? "MakeOffer" : "MakeDataset", PrivateInstance);
            Assert.That(makeRow, Is.Not.Null);
            Type modelType = makeRow.GetParameters()[0].ParameterType;
            object model = Activator.CreateInstance(modelType);
            modelType.GetField("id").SetValue(model, offer ? "mnist.pack" : "mnist");
            object row = makeRow.Invoke(page, offer
                ? new object[] { model, 0f }
                : new object[] { model, 1, 0f });
            var title = (TMP_Text)row.GetType().GetField("name").GetValue(row);
            // The fixture isolates layout: row text refresh and button callbacks do not require a game host.
            title.text = text;
            area.ForceUpdateRectTransforms();
            return title;
        }

        static Type FindDesktopType(string name)
        {
            Type type = Type.GetType("LingGuangV05.Desktop.XingGuang." + name + ", Assembly-CSharp");
            Assert.That(type, Is.Not.Null, name);
            return type;
        }
    }
}
