using System;
using System.Reflection;
using LingGuangV05.Runtime;
using NUnit.Framework;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LingGuangV05.UI.Tests
{
    public sealed class MainSceneEntryTests
    {
        const string EntryTypeName = "LingGuangV05.Desktop.MainSceneEntry, Assembly-CSharp";

        [Test]
        public void MenuHasAnExplicitMainSceneEntryWithoutReflectionInProduction()
        {
            var type = Type.GetType(EntryTypeName);
            Assert.That(type, Is.Not.Null, "Loading Main after a menu needs an explicit startup entry.");
            var entry = type.GetMethod("EnsureInitialized", BindingFlags.Public | BindingFlags.Static);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.ReturnType, Is.EqualTo(typeof(bool)));
            Assert.That(entry.GetParameters(), Is.Empty);
        }

        [Test]
        public void MainEntryDoesNotInitializeOrTouchSavesInEditMode()
        {
            var type = Type.GetType(EntryTypeName);
            Assert.That(type, Is.Not.Null);
            var entry = type.GetMethod("EnsureInitialized", BindingFlags.Public | BindingFlags.Static);
            Assert.That(entry, Is.Not.Null);
            Assert.That(Application.isPlaying, Is.False);
            Assert.That(entry.Invoke(null, null), Is.False);
        }

        [TestCase("LingGuangV05.Desktop.Story.StoryBootstrap")]
        [TestCase("LingGuangV05.Desktop.XingGuang.XingGuangController")]
        [TestCase("LingGuangV05.Desktop.XingGuang.XgYyTalk")]
        [TestCase("LingGuangV05.Desktop.YY.YYChatView")]
        [TestCase("LingGuangV05.Desktop.YY.YYGirlfriend")]
        [TestCase("LingGuangV05.Desktop.Mail.MailInboxView")]
        [TestCase("LingGuangV05.Desktop.XingGuang.XgMarketRelay")]
        [TestCase("LingGuangV05.Desktop.XingGuang.XgAfterthoughtRelay")]
        [TestCase("LingGuangV05.Desktop.LLM.LocalLlm")]
        public void ExplicitInitializersKeepTheirOriginalDirectMainStartup(string typeName)
        {
            var type = Type.GetType(typeName + ", Assembly-CSharp");
            Assert.That(type, Is.Not.Null, typeName);
            var attach = type.GetMethod("Attach", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(attach, Is.Not.Null, typeName);
            Assert.That(attach.IsAssembly, Is.True, "Startup must be callable by MainSceneEntry in the same assembly.");
            var attribute = attach.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
            Assert.That(attribute, Is.Not.Null, "Direct Main play must retain the original startup path.");
            Assert.That(attribute.loadType, Is.EqualTo(RuntimeInitializeLoadType.AfterSceneLoad));
        }

        [Test]
        [Explicit("Run in an existing, save-isolated ComputerCaseMenu -> Main Play session; this test never loads scenes or starts a local model.")]
        public void MainEntryAttachesYyBridgeAndReusesItsLiveBinding()
        {
            Assert.That(Application.isPlaying, Is.True, "Run this regression in the isolated Play QA session, not EditMode.");
            var entryType = Type.GetType(EntryTypeName, true);
            var scenePath = (string)entryType.GetField("ScenePath").GetRawConstantValue();
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(scenePath));
            var runtime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.TestMode, Is.False, "TestMode deliberately skips the real Main entry.");
            Assert.That(runtime.gameObject.scene, Is.EqualTo(SceneManager.GetActiveScene()));
            var llmType = Type.GetType("LingGuangV05.Desktop.LLM.LocalLlm, Assembly-CSharp", true);
            Assert.That(runtime.GetComponent(llmType), Is.Not.Null, "The QA session must already be initialized; do not start a model from this test.");

            var controllerType = Type.GetType("LingGuangV05.Desktop.XingGuang.XingGuangController, Assembly-CSharp", true);
            var controller = runtime.GetComponent(controllerType);
            Assert.That(controller, Is.Not.Null);
            var runtimeSim = runtime.Sim;
            var labSim = controllerType.GetProperty("Sim").GetValue(controller);
            var talkType = Type.GetType("LingGuangV05.Desktop.XingGuang.XgYyTalk, Assembly-CSharp", true);
            var talk = runtime.GetComponent(talkType);
            Assert.That(talk, Is.Not.Null, "The explicit Main entry must install the YY bridge after additive menu loading.");
            var hubType = Type.GetType("LingGuangV05.Desktop.YY.YYChatHub, Assembly-CSharp", true);
            var hub = hubType.GetProperty("Instance").GetValue(null);
            Assert.That(hub, Is.Not.Null);
            Assert.That(hubType.GetProperty("LingGuangTalk").GetValue(hub), Is.SameAs(talk),
                "The normal startup and Update lifecycle must bind the bridge before the test runs.");

            // Check the naturally installed bridge first; calling entry sooner would hide a broken startup caller.
            var entry = entryType.GetMethod("EnsureInitialized", BindingFlags.Public | BindingFlags.Static);
            Assert.That(entry.Invoke(null, null), Is.True);
            Assert.That(runtime.GetComponents(talkType), Has.Length.EqualTo(1), "Repeated Main entry must not duplicate chat subscriptions.");
            Assert.That(runtime.GetComponent(talkType), Is.SameAs(talk));
            Assert.That(hubType.GetProperty("LingGuangTalk").GetValue(hub), Is.SameAs(talk));
            Assert.That(runtime.Sim, Is.SameAs(runtimeSim));
            Assert.That(controllerType.GetProperty("Sim").GetValue(controller), Is.SameAs(labSim));
        }

        [Test]
        public void DeletedMessagingAppHasNoDesktopStartupOrView()
        {
            Assert.That(Type.GetType("LingGuangV05.Desktop.Juxin.JuxinHub, Assembly-CSharp"), Is.Null);
            Assert.That(Type.GetType("LingGuangV05.Desktop.Juxin.JuxinView, Assembly-CSharp"), Is.Null);
        }

        [Test]
        public void RetiredNativeSlotIsSuppressedForDirectMainStartup()
        {
            var type = Type.GetType(EntryTypeName, true);
            var entry = type.GetMethod("HideUnusedPhotoGalleryAfterSceneLoad", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(entry, Is.Not.Null);
            var attribute = entry.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.loadType, Is.EqualTo(RuntimeInitializeLoadType.AfterSceneLoad));
        }

        [Test]
        public void OldMessagingSaveFieldsAreIgnoredWithoutLosingLabProgress()
        {
            var stateType = Type.GetType("LingGuangV05.XingGuang.XgState, LingGuangV05.XingGuang.Core", true);
            var state = JsonUtility.FromJson("{\"stage\":5,\"jxVersion\":1,\"jxAutoReply\":true,\"jxThreads\":[{\"id\":\"dm.old\"}]}", stateType);
            Assert.That(stateType.GetField("stage").GetValue(state), Is.EqualTo(5));
            StringAssert.DoesNotContain("jxVersion", JsonUtility.ToJson(state));
            StringAssert.DoesNotContain("jxThreads", JsonUtility.ToJson(state));
        }

        [Test]
        public void UnusedPhotoGalleryIsClosedAndCannotReturnThroughStartMenuSearch()
        {
            var entryType = Type.GetType(EntryTypeName, true);
            var hide = entryType.GetMethod("HideUnusedPhotoGallery", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(hide, Is.Not.Null);
            var previous = SceneManager.GetActiveScene();
            // TestRunner's unsaved scratch scene cannot have another unsaved scene added to it.
            bool ownsScene = !string.IsNullOrEmpty(previous.path);
            var scene = ownsScene ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive) : previous;
            GameObject root = null;
            try
            {
                root = new GameObject("Removal test desktop");
                if (root.scene != scene) SceneManager.MoveGameObjectToScene(root, scene);
                root.SetActive(false); // Native windows need no running animators for this EditMode test.
                var desktop = Child(root, "Desktop List");
                var removedIcon = Child(desktop, "Photo Gallery");
                var otherIcon = Child(desktop, "Web Browser");
                var windowType = Type.GetType("Michsky.DreamOS.WindowManager, Assembly-CSharp", true);
                var removedWindow = Child(root, "Photo Gallery").AddComponent(windowType);
                var otherWindow = Child(root, "Web Browser").AddComponent(windowType);
                windowType.GetField("enableMobileMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(removedWindow, true);
                windowType.GetField("isOn").SetValue(removedWindow, true);
                windowType.GetField("isOn").SetValue(otherWindow, true);
                var taskbarType = Type.GetType("Michsky.DreamOS.TaskbarButton, Assembly-CSharp", true);
                var removedTaskbar = Child(root, "Gallery taskbar").AddComponent(taskbarType);
                windowType.GetField("taskbarButton").SetValue(removedWindow, removedTaskbar);
                var otherTaskbar = Child(root, "Browser taskbar").AddComponent(taskbarType);
                windowType.GetField("taskbarButton").SetValue(otherWindow, otherTaskbar);
                var menuType = Type.GetType("HongmengOS.Aero2010.AeroStartMenu, Assembly-CSharp", true);
                var menu = Child(root, "Start Menu").AddComponent(menuType);
                var rowType = menuType.GetNestedType("ProgramEntry");
                var removedRow = Child(root, "Gallery start row");
                var otherRow = Child(root, "Browser start row");
                var entries = Array.CreateInstance(rowType, 2);
                entries.SetValue(Program(rowType, "Photo Gallery", removedWindow, removedRow), 0);
                entries.SetValue(Program(rowType, "Web Browser", otherWindow, otherRow), 1);
                menuType.GetField("programs").SetValue(menu, entries);
                // Pictures is a separate native place button, not one of menu.programs.
                var pictures = Child(root, "Pictures").AddComponent<Button>();
                var openGallery = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), removedWindow, windowType.GetMethod("OpenWindow"));
                UnityEventTools.AddPersistentListener(pictures.onClick, openGallery);
                pictures.onClick.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
                var unrelated = Child(root, "Pictures").AddComponent<Button>(); // Same label, different target.
                var openBrowser = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), otherWindow, windowType.GetMethod("OpenWindow"));
                UnityEventTools.AddPersistentListener(unrelated.onClick, openBrowser);

                hide.Invoke(null, new object[] { scene });
                hide.Invoke(null, new object[] { scene }); // Explicit Main startup is idempotent.
                Assert.That(removedWindow.gameObject.activeSelf, Is.False);
                Assert.That(windowType.GetField("isOn").GetValue(removedWindow), Is.False);
                Assert.That(removedIcon.activeSelf, Is.False);
                Assert.That(removedTaskbar.gameObject.activeSelf, Is.False);
                Assert.That(pictures.gameObject.activeSelf, Is.False);
                Assert.That(pictures.interactable, Is.False);
                pictures.onClick.Invoke(); // Even a stale invocation cannot resurrect the retired window.
                Assert.That(removedWindow.gameObject.activeSelf, Is.False);
                Assert.That(windowType.GetField("isOn").GetValue(removedWindow), Is.False);
                Assert.That(unrelated.gameObject.activeSelf, Is.True);
                Assert.That(unrelated.interactable, Is.True);
                Assert.That(unrelated.onClick.GetPersistentListenerState(0), Is.EqualTo(UnityEventCallState.RuntimeOnly));
                Assert.That(rowType.GetField("window").GetValue(entries.GetValue(0)), Is.Null);
                menuType.GetMethod("ShowAllPrograms").Invoke(menu, null);
                Assert.That(removedRow.activeSelf, Is.False);
                Assert.That(otherRow.activeSelf, Is.True);
                menuType.GetMethod("SetSearch").Invoke(menu, new object[] { "Photo Gallery" });
                Assert.That(removedRow.activeSelf, Is.False);
                Assert.That(otherIcon.activeSelf, Is.True);
                Assert.That(otherTaskbar.gameObject.activeSelf, Is.True);
                Assert.That(otherWindow.gameObject.activeSelf, Is.True);
                Assert.That(windowType.GetField("isOn").GetValue(otherWindow), Is.True);
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                if (ownsScene && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        static GameObject Child(GameObject parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        static object Program(Type type, string title, Component window, GameObject row)
        {
            object entry = Activator.CreateInstance(type);
            type.GetField("title").SetValue(entry, title);
            type.GetField("searchKeywords").SetValue(entry, title);
            type.GetField("window").SetValue(entry, window);
            type.GetField("row").SetValue(entry, row);
            return entry;
        }
    }
}
