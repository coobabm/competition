using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using LingGuangV05.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LingGuangV05.UI.Tests
{
    /// <summary>Real startup boundary regressions; temporary data never uses the player's DreamOS profile.</summary>
    public sealed class StartupRegressionTests
    {
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const string DownloadKey = "灵光.exe_DownloadState";
        private Scene previousScene;
        private Scene testScene;

        [SetUp]
        public void SetUp()
        {
            if (Application.isPlaying) return;
            previousScene = SceneManager.GetActiveScene();
            testScene = default(Scene);
            // Unity TestRunner already provides an unsaved scratch scene and rejects another additive scene.
            if (string.IsNullOrEmpty(previousScene.path)) return;
            testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(testScene);
        }

        [TearDown]
        public void TearDown()
        {
            if (Application.isPlaying) return;
            if (testScene.IsValid() && testScene.isLoaded) EditorSceneManager.CloseScene(testScene, true);
            if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
        }

        [Test]
        public void UninstalledAppWithMissingDownloadKeyReconcilesWithoutInventingARecord()
        {
            Assert.That(Application.isPlaying, Is.False, "This temporary-profile regression runs only in EditMode.");
            using (var data = new TemporaryDreamOsData("{}"))
            {
                var host = InactiveHost();
                try
                {
                    var browser = host.AddComponent(RuntimeType("Michsky.DreamOS.WebBrowserManager"));
                    var flow = host.AddComponent(RuntimeType("LingGuangV05.Desktop.Story.LingGuangInstallFlow"));
                    Set(flow, "browser", browser);

                    Assert.DoesNotThrow(() => Invoke(flow, "Reconcile", false));

                    Assert.That(Get(flow, "reconciled"), Is.True, "A missing record is already the undownloaded state, not a reason to retry each frame.");
                    Assert.That(File.ReadAllText(data.NetworkFile), Is.EqualTo("{}"), "Reading a new profile must not create a fake completed/download record.");
                    Assert.That((IList)Get(browser, "activeDownloads"), Is.Empty);
                }
                finally { UnityEngine.Object.DestroyImmediate(host); }
            }
        }

        [Test]
        public void UninstalledAppClearsItsStaleDownloadButKeepsOtherDownloads()
        {
            Assert.That(Application.isPlaying, Is.False);
            using (var data = new TemporaryDreamOsData("{\"灵光.exe_DownloadState\":2}"))
            {
                var host = InactiveHost();
                ScriptableObject library = null;
                try
                {
                    var browser = host.AddComponent(RuntimeType("Michsky.DreamOS.WebBrowserManager"));
                    var flow = host.AddComponent(RuntimeType("LingGuangV05.Desktop.Story.LingGuangInstallFlow"));
                    Set(flow, "browser", browser);
                    library = ScriptableObject.CreateInstance(RuntimeType("Michsky.DreamOS.WebBrowserLibrary"));
                    var fileType = library.GetType().GetNestedType("DownloadableFiles");
                    var file = Activator.CreateInstance(fileType);
                    fileType.GetField("fileName").SetValue(file, "灵光.exe");
                    ((IList)Get(library, "dlFiles")).Add(file);
                    Set(browser, "webLibrary", library);
                    Set(browser, "downloadsParent", Child(host, "Downloads").transform);
                    var downloadType = RuntimeType("Michsky.DreamOS.WebBrowserDownloadItem");
                    var ours = Child(host, "Our download").AddComponent(downloadType);
                    var other = Child(host, "Other download").AddComponent(downloadType);
                    Set(ours, "fileName", "灵光.exe");
                    Set(other, "fileName", "other.txt");
                    var downloads = (IList)Get(browser, "activeDownloads");
                    downloads.Add(ours);
                    downloads.Add(other);

                    Assert.DoesNotThrow(() => Invoke(flow, "Reconcile", false));

                    var manager = RuntimeType("Michsky.DreamOS.DreamOSDataManager");
                    var category = Enum.Parse(manager.GetNestedType("DataCategory"), "Network");
                    Assert.That(manager.GetMethod("ReadIntData").Invoke(null, new[] { category, DownloadKey }), Is.EqualTo(0));
                    Assert.That(downloads.Count, Is.EqualTo(1));
                    Assert.That(downloads[0], Is.SameAs(other), "Reconciliation must not discard unrelated downloads.");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(host);
                    if (library != null) UnityEngine.Object.DestroyImmediate(library);
                }
            }
        }

        [TestCase(0, 0)]
        [TestCase(1, -1)]
        [TestCase(1, 1)]
        public void InvalidNetworkSelectionDisplaysDisconnectedInsteadOfIndexingTheList(int count, int selectedIndex)
        {
            var host = InactiveHost();
            var disconnected = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            try
            {
                var manager = host.AddComponent(RuntimeType("Michsky.DreamOS.NetworkManager"));
                var indicator = Child(host, "Indicator").AddComponent<Image>();
                ((IList)Get(manager, "networkIndicators")).Add(indicator);
                Set(manager, "signalDisconnected", disconnected);
                var items = (IList)Get(manager, "networkItems");
                for (int i = 0; i < count; i++) items.Add(Activator.CreateInstance(manager.GetType().GetNestedType("NetworkItem")));
                Set(manager, "currentNetworkIndex", selectedIndex);
                Set(manager, "isConnected", true);

                Assert.DoesNotThrow(() => Invoke(manager, "UpdateIndicators", true));

                Assert.That(indicator.sprite, Is.SameAs(disconnected));
                Assert.That(Get(manager, "isConnected"), Is.False, "A nonexistent selection cannot remain logically connected.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(disconnected);
            }
        }

        [Test]
        public void ValidConnectedNetworkStillDisplaysItsSignalStrength()
        {
            var host = InactiveHost();
            var connected = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            try
            {
                var manager = host.AddComponent(RuntimeType("Michsky.DreamOS.NetworkManager"));
                var indicator = Child(host, "Indicator").AddComponent<Image>();
                ((IList)Get(manager, "networkIndicators")).Add(indicator);
                Set(manager, "signalWeak", connected);
                ((IList)Get(manager, "networkItems")).Add(Activator.CreateInstance(manager.GetType().GetNestedType("NetworkItem")));
                Set(manager, "currentNetworkIndex", 0);
                Set(manager, "isConnected", true);

                Invoke(manager, "UpdateIndicators", true);

                Assert.That(indicator.sprite, Is.SameAs(connected));
                Assert.That(Get(manager, "isConnected"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(connected);
            }
        }

        [Test]
        [Explicit("Invoke only inside the root-controlled, isolated Play QA session; does not load scenes or touch saves.")]
        public void LocalizedButtonCanRefreshBeforeItsOwnEnableInitializesCanvasGroups()
        {
            Assert.That(Application.isPlaying, Is.True, "The failing alpha access only exists in Play Mode.");
            var host = InactiveHost();
            try
            {
                var button = host.AddComponent(RuntimeType("Michsky.DreamOS.ButtonManager"));
                ((Behaviour)button).enabled = false; // Deterministically defer its OnEnable while an external caller refreshes.
                Set(button, "autoFitContent", false);
                Set(button, "useLocalization", false);
                Set(button, "buttonText", "StartupRegressionLabel");
                var text = Child(host, "Normal text").AddComponent<TextMeshProUGUI>();
                text.text = "old";
                Set(button, "normalTextObj", text);
                var localized = host.AddComponent(RuntimeType("LingGuangV05.Desktop.DesktopLocalizedText"));
                ((Behaviour)localized).enabled = false;
                Set(localized, "button", button);
                Set(localized, "source", "StartupRegressionLabel");
                host.SetActive(true);
                Assert.That(Get(button, "isInitialized"), Is.False, "Fixture must exercise the pre-initialization boundary.");

                Assert.DoesNotThrow(() => Invoke(localized, "Apply"));
                Assert.DoesNotThrow(() => Invoke(localized, "Apply"));

                Assert.That(text.text, Is.EqualTo("StartupRegressionLabel"));
                Assert.That(Get(button, "disabledCG"), Is.Not.Null, "A public refresh must initialize the groups before reading their alpha.");
                Assert.That(host.GetComponentsInChildren<CanvasGroup>(true), Has.Length.EqualTo(4), "Repeated refresh must not duplicate visual groups.");
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        [Explicit("Invoke only inside the root-controlled Play QA session; exercises inactive refresh and later activation without touching saves.")]
        public void InactiveLocalizedButtonRefreshWaitsForActivationWithoutCoroutineErrorsOrDuplicateGroups()
        {
            Assert.That(Application.isPlaying, Is.True);
            var host = InactiveHost();
            var errors = new List<string>();
            Application.LogCallback capture = (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
            };
            Application.logMessageReceived += capture;
            try
            {
                var button = host.AddComponent(RuntimeType("Michsky.DreamOS.ButtonManager"));
                Set(button, "autoFitContent", false);
                Set(button, "useLocalization", false);
                Set(button, "useUINavigation", true);
                Set(button, "navigationMode", Navigation.Mode.Explicit);
                Set(button, "buttonText", "InactiveStartupLabel");
                var text = Child(host, "Normal text").AddComponent<TextMeshProUGUI>();
                text.text = "old";
                Set(button, "normalTextObj", text);
                var localized = host.AddComponent(RuntimeType("LingGuangV05.Desktop.DesktopLocalizedText"));
                ((Behaviour)localized).enabled = false;
                Set(localized, "button", button);
                Set(localized, "source", "InactiveStartupLabel");

                Assert.DoesNotThrow(() => Invoke(localized, "Apply"));
                Assert.DoesNotThrow(() => Invoke(localized, "Apply"));
                Assert.That(text.text, Is.EqualTo("InactiveStartupLabel"));
                Assert.That(Get(button, "isInitialized"), Is.False, "An inactive authored label must not start initialization coroutines.");
                Assert.That(host.GetComponentsInChildren<CanvasGroup>(true), Is.Empty);

                host.SetActive(true);
                Invoke(localized, "Apply");
                Invoke(localized, "Apply");

                Assert.That(Get(button, "isInitialized"), Is.True);
                Assert.That(host.GetComponentsInChildren<CanvasGroup>(true), Has.Length.EqualTo(4));
                Assert.That(errors, Is.Empty, "Refreshing an inactive label must not attempt to start a coroutine on its hidden GameObject.");
            }
            finally
            {
                Application.logMessageReceived -= capture;
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        [Explicit("Invoke only inside the root-controlled Play QA session; listens for the actual inactive coroutine error.")]
        public void InactiveChatLayoutShowUpdatesContentWithoutStartingAnimationCoroutines()
        {
            Assert.That(Application.isPlaying, Is.True);
            var host = InactiveHost();
            var picture = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            var errors = new List<string>();
            Application.LogCallback capture = (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
            };
            Application.logMessageReceived += capture;
            try
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DreamOS - Complete OS UI/Prefabs/Messaging/Chat Layout.prefab");
                Assert.That(prefab, Is.Not.Null, "Use the real authored chat layout, including its animator and display bindings.");
                var view = UnityEngine.Object.Instantiate(prefab, host.transform, false);
                view.name = "Startup inactive chat";
                view.SetActive(false);
                var layout = view.GetComponent(RuntimeType("Michsky.DreamOS.ChatLayoutPreset"));
                Assert.That(layout, Is.Not.Null);
                Set(layout, "personName", "Startup contact");
                Set(layout, "personPicture", picture);

                Invoke(layout, "Show");
                Invoke(layout, "Show");

                Assert.That(view.activeSelf, Is.True, "Show must retain the requested visibility for later parent activation.");
                Assert.That(view.activeInHierarchy, Is.False, "The enclosing native chat pane is still hidden.");
                Assert.That(((TMP_Text)Get(layout, "nameText")).text, Is.EqualTo("Startup contact"));
                Assert.That(((Image)Get(layout, "individualImage")).sprite, Is.SameAs(picture));
                Assert.That(errors, Is.Empty, "An inactive native chat pane must update data without starting DisableAnimator on an inactive GameObject.");
            }
            finally
            {
                Application.logMessageReceived -= capture;
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(picture);
            }
        }

        private static Type RuntimeType(string name) { return Type.GetType(name + ", Assembly-CSharp", true); }
        private static object Get(object target, string field) { return target.GetType().GetField(field, InstanceFields).GetValue(target); }
        private static void Set(object target, string field, object value) { target.GetType().GetField(field, InstanceFields).SetValue(target, value); }
        private static void Invoke(object target, string name, params object[] args)
        {
            try { target.GetType().GetMethod(name, InstanceFields).Invoke(target, args); }
            catch (TargetInvocationException error)
            {
                if (error.InnerException != null) ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
        private static GameObject InactiveHost()
        {
            var host = new GameObject("Startup regression", typeof(RectTransform));
            host.SetActive(false);
            return host;
        }
        private static GameObject Child(GameObject parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private sealed class TemporaryDreamOsData : IDisposable
        {
            private readonly FieldInfo pathField;
            private readonly object previousPath;
            private readonly string directory;
            public string NetworkFile { get; }
            public TemporaryDreamOsData(string networkJson)
            {
                pathField = RuntimeType("Michsky.DreamOS.DreamOSDataManager").GetField("dataPath", BindingFlags.NonPublic | BindingFlags.Static);
                previousPath = pathField.GetValue(null);
                directory = Path.Combine(Path.GetTempPath(), "lingguang-startup-regression-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                NetworkFile = Path.Combine(directory, "Network.json");
                File.WriteAllText(NetworkFile, networkJson);
                pathField.SetValue(null, directory + Path.DirectorySeparatorChar);
            }
            public void Dispose()
            {
                pathField.SetValue(null, previousPath);
                Directory.Delete(directory, true);
            }
        }
    }
}
