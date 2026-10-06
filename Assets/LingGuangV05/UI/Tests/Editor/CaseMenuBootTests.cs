using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.UI.Tests
{
    public sealed class CaseMenuBootTests
    {
        static Type Find(string name)
        {
            var type = Type.GetType("LingGuangV05.CaseMenu." + name + ", Assembly-CSharp");
            Assert.That(type, Is.Not.Null, name + " must exist");
            return type;
        }

        static object Read(Type type, object instance, string property)
        {
            var info = type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(info, Is.Not.Null, property + " is part of the boot contract");
            return info.GetValue(instance);
        }

        [Test]
        public void BootHasAVisibleDurationInsteadOfAnInstantDesktopReveal()
        {
            var duration = Find("CaseMenuMainDisplay").GetField("BootDuration", BindingFlags.Public | BindingFlags.Static);
            Assert.That(duration, Is.Not.Null, "The physical boot needs a deterministic duration.");
            Assert.That(duration.IsLiteral, Is.True);
            Assert.That(Convert.ToSingle(duration.GetRawConstantValue()), Is.GreaterThanOrEqualTo(3f));
        }

        [Test]
        public void PreloadedDisplayStartsWithAnIncompleteHiddenBoot()
        {
            var type = Find("CaseMenuMainDisplay");
            var go = new GameObject("Boot initial-state test");
            try
            {
                var display = go.AddComponent(type);
                Assert.That(Read(type, display, "IsBootComplete"), Is.False);
                Assert.That(Read(type, display, "BootProgress"), Is.EqualTo(0f));
                Assert.That(Read(type, display, "IsBootScreenVisible"), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void CameraArrivalCannotEnterGameplayBeforeBootCompletes()
        {
            var type = Find("CaseMenuMainDisplay");
            var go = new GameObject("Boot input-gate test");
            try
            {
                var display = go.AddComponent(type);
                var ready = type.GetField("<IsReady>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(ready, Is.Not.Null, "The fixture only marks preload ready; boot remains incomplete.");
                ready.SetValue(display, true);
                type.GetMethod("EnterDesktop").Invoke(display, null);
                Assert.That(Read(type, display, "HasEnteredDesktop"), Is.False,
                    "A short/reduced-motion camera move must not bypass the screen's boot.");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void BootArtworkFitsThePhysicalDisplayAndHasReadableNonInteractiveLabels()
        {
            WithBootScreen((type, boot, display) =>
            {
                type.GetMethod("Show").Invoke(boot, null);
                type.GetMethod("SetProgress").Invoke(boot, new object[] { .5f, 1f });
                Canvas.ForceUpdateCanvases();
                var generated = display.Cast<Transform>().OfType<RectTransform>()
                    .FirstOrDefault(t => t.GetComponentsInChildren<Graphic>(true).Length > 0);
                Assert.That(generated, Is.Not.Null, "Boot UI must be parented inside the physical display.");
                Assert.That(generated.rect.width, Is.EqualTo(display.rect.width).Within(.1f));
                Assert.That(generated.rect.height, Is.EqualTo(display.rect.height).Within(.1f));
                Assert.That(generated.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(generated.anchorMax, Is.EqualTo(Vector2.one));
                var labels = generated.GetComponentsInChildren<TextMeshProUGUI>(true);
                Assert.That(labels.Length, Is.GreaterThan(0));
                Assert.That(labels.Any(label => !string.IsNullOrWhiteSpace(label.text) && label.font != null
                    && label.fontSize > 0 && label.color.a > 0 && label.gameObject.activeInHierarchy), Is.True,
                    "At least one visible boot label needs a real font.");
                Assert.That(generated.GetComponentsInChildren<Graphic>(true).All(graphic => !graphic.raycastTarget), Is.True,
                    "Screen-contained boot art must never become another input path.");
            });
        }

        [Test]
        public void BootScreenShowAndHideControlItsVisibleState()
        {
            WithBootScreen((type, boot, display) =>
            {
                type.GetMethod("Show").Invoke(boot, null);
                Assert.That(Read(type, boot, "IsVisible"), Is.True);
                type.GetMethod("SetProgress").Invoke(boot, new object[] { 1f, 1f });
                type.GetMethod("Hide").Invoke(boot, null);
                Assert.That(Read(type, boot, "IsVisible"), Is.False);
                type.GetMethod("Hide").Invoke(boot, null);
                Assert.That(Read(type, boot, "IsVisible"), Is.False);
            });
        }

        static void WithBootScreen(Action<Type, Component, RectTransform> check)
        {
            var type = Find("CaseMenuBootScreen");
            Assert.That(typeof(MonoBehaviour).IsAssignableFrom(type), Is.True);
            var host = new GameObject("Physical LCD boot test", typeof(RectTransform), typeof(Canvas));
            try
            {
                var displayObject = new GameObject("LCD Display", typeof(RectTransform), typeof(RawImage));
                var display = (RectTransform)displayObject.transform;
                display.SetParent(host.transform, false);
                display.sizeDelta = new Vector2(1920, 1080);
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset");
                Assert.That(font, Is.Not.Null, "The installed CJK font is reused; tests create no font asset.");
                var boot = host.AddComponent(type);
                var build = type.GetMethod("Build", new[] { typeof(RectTransform), typeof(TMP_FontAsset) });
                Assert.That(build, Is.Not.Null);
                build.Invoke(boot, new object[] { display, font });
                check(type, boot, display);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }
    }
}
