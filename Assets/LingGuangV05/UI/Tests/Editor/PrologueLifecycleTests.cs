using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LingGuangV05.UI.Tests
{
    public sealed class PrologueLifecycleTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static Type DirectorType => Type.GetType("LingGuangV05.Desktop.Story.PrologueDirector, Assembly-CSharp", true);
        static Type DeskType => Type.GetType("LingGuangV05.Desktop.Story.PrologueDesk, Assembly-CSharp", true);
        GameObject root, host;
        Component director;
        object desk;
        Canvas[] previousScreens;

        [SetUp] public void SetUp()
        {
            previousScreens = Screens();
            root = new GameObject("Prologue test desktop", typeof(RectTransform));
            host = new GameObject("Prologue test host");
            host.SetActive(false);
            director = host.AddComponent(DirectorType);
            desk = NewDesk();
            Set("desk", desk);
        }

        object NewDesk() => Activator.CreateInstance(DeskType, root.transform, root.transform, root.transform, TMP_Settings.defaultFontAsset);
        static Canvas[] Screens() => Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)
            .Where(c => c.name == "LingGuang Prologue Screen").ToArray();
        void Set(string field, object value) => DirectorType.GetField(field, Private).SetValue(director, value);
        object Get(string field) => DirectorType.GetField(field, Private).GetValue(director);
        void Call(string method) => DirectorType.GetMethod(method, Private).Invoke(director, null);

        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(host);
            foreach (var screen in Screens().Except(previousScreens)) if (screen != null) Object.DestroyImmediate(screen.gameObject);
            Object.DestroyImmediate(root);
        }

        [Test] public void RebuildingDeskReusesLayerAndPreservesOtherApps()
        {
            var layer = (RectTransform)DeskType.GetField("layer").GetValue(desk);
            var notification = new GameObject("Other app notification", typeof(RectTransform));
            notification.transform.SetParent(layer, false);
            var rebuilt = NewDesk();
            Assert.AreSame(layer, DeskType.GetField("layer").GetValue(rebuilt));
            Assert.IsTrue(notification != null && notification.transform.parent == layer);
        }

        [Test] public void RebuildingScreenLeavesOneHiddenNonBlockingSubtitle()
        {
            Call("BuildScreen");
            ((CanvasGroup)Get("thoughtGroup")).alpha = 1;
            Call("BuildScreen");
            Assert.AreEqual(1, Screens().Except(previousScreens).Count());
            Assert.AreEqual(0, ((CanvasGroup)Get("thoughtGroup")).alpha);
            Assert.IsFalse(((CanvasGroup)Get("thoughtGroup")).blocksRaycasts);
        }

        [Test] public void DisablingDirectorClearsThoughtImmediately()
        {
            Call("BuildScreen");
            ((CanvasGroup)Get("thoughtGroup")).alpha = 1;
            ((TMP_Text)Get("thought")).text = "stale subtitle";
            Call("OnDisable");
            Assert.AreEqual(0, ((CanvasGroup)Get("thoughtGroup")).alpha);
            Assert.AreEqual("", ((TMP_Text)Get("thought")).text);
        }

        [Test] public void DesktopDestroyedBeforeDirectorDoesNotBreakCleanup()
        {
            Object.DestroyImmediate(root);
            root = null;
            Assert.DoesNotThrow(() => Call("OnDisable"));
        }

        [Test] public void StopPreservesSharedNotificationsButRemovesOwnedBlockers()
        {
            var layer = (RectTransform)DeskType.GetField("layer").GetValue(desk);
            var notification = new GameObject("Other app notification", typeof(RectTransform));
            notification.transform.SetParent(layer, false);
            var boot = new GameObject("Boot", typeof(RectTransform));
            boot.transform.SetParent(layer, false);
            Call("StopPrologue");
            Assert.IsTrue(notification != null && notification.activeSelf);
            Assert.IsTrue(boot == null);
        }

        [Test] public void LegacyDuplicateLayersMergeWithoutLosingOtherApps()
        {
            var canonical = (RectTransform)DeskType.GetField("layer").GetValue(desk);
            var old = new GameObject("Prologue Layer", typeof(RectTransform));
            old.transform.SetParent(root.transform, false);
            var notification = new GameObject("Other app notification", typeof(RectTransform));
            notification.transform.SetParent(old.transform, false);
            NewDesk();
            Assert.IsTrue(old == null);
            Assert.AreSame(canonical, notification.transform.parent);
        }

        [Test] public void DuplicatePopupControllerDoesNotLeaveUndismissableUi()
        {
            var old = new GameObject("Prologue Layer", typeof(RectTransform));
            old.transform.SetParent(root.transform, false);
            old.AddComponent(Type.GetType("LingGuangV05.Desktop.Story.TrayPopups, Assembly-CSharp", true));
            var area = new GameObject("Tray Popup Area", typeof(RectTransform));
            area.transform.SetParent(old.transform, false);
            NewDesk();
            Assert.IsTrue(area == null, "UI without its expiry coroutine must not survive the host");
        }

        [Test] public void DisablingPopupHostRemovesOnlyItsTransientArea()
        {
            var popupType = Type.GetType("LingGuangV05.Desktop.Story.TrayPopups, Assembly-CSharp", true);
            var popup = root.AddComponent(popupType);
            var area = new GameObject("Tray Popup Area", typeof(RectTransform));
            area.transform.SetParent(root.transform, false);
            var other = new GameObject("Other app", typeof(RectTransform));
            other.transform.SetParent(root.transform, false);
            popupType.GetMethod("OnDisable", Private).Invoke(popup, null);
            Assert.IsTrue(area == null);
            Assert.IsTrue(other != null && other.activeSelf);
        }

        [Test] public void FirstExplicitNativeOpenSurvivesDeferredStart()
        {
            var windowType = Type.GetType("LingGuangV05.Desktop.OS.WindowManager, Assembly-CSharp", true);
            var go = new GameObject("Cold native window", typeof(RectTransform), typeof(CanvasGroup), typeof(Animator));
            go.SetActive(false);
            go.transform.SetParent(root.transform, false);
            var controller = new UnityEditor.Animations.AnimatorController();
            go.GetComponent<Animator>().runtimeAnimatorController = controller;
            var window = go.AddComponent(windowType);
            windowType.GetField("enableMobileMode", Private).SetValue(window, true); // no animation dependencies
            windowType.GetMethod("OpenWindow").Invoke(window, null);
            windowType.GetMethod("Start", Private).Invoke(window, null);
            try
            {
                Assert.IsTrue(go.activeSelf, "Start must not undo an explicit open from a desktop icon");
                Assert.IsTrue((bool)windowType.GetField("isOn").GetValue(window));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(controller); }
        }

        [Test] public void ReenableAlwaysBindsALiveAudioSource()
        {
            Call("OnEnable");
            var first = (AudioSource)Get("voice");
            Assert.IsTrue(first != null);
            Object.DestroyImmediate(first);
            Call("OnEnable");
            Assert.IsTrue((AudioSource)Get("voice") != null);
        }

        [TestCase(true)] [TestCase(false)]
        public void DisablingOnlyReleasesItsOwnInput(bool ownsInput)
        {
            var surfaceType = Type.GetType("HongmengOS.Aero2010.AeroLcdInputSurface, Assembly-CSharp", true);
            var surface = root.AddComponent(surfaceType);
            surfaceType.GetField("inputEnabled").SetValue(surface, false);
            Set("surface", surface);
            Set("hijacked", ownsInput);
            Call("OnDisable");
            Assert.AreEqual(ownsInput, surfaceType.GetField("inputEnabled").GetValue(surface));
        }
    }
}
