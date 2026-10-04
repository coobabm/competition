using System.Linq;
using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace LingGuangV05.UI.Tests
{
    /// <summary>Regression guards for the actual renderer/line-box defects found in desktop Play QA.</summary>
    public sealed class ChapterOneAppTests
    {
        private GameObject _runtimeHost, _appHost, _canvasHost;
        private ChapterOneApp _app;
        private ChapterOneRuntime _runtime;
        private readonly List<GameObject> _extraAppHosts = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject host in _extraAppHosts) if (host != null) UnityEngine.Object.DestroyImmediate(host);
            _extraAppHosts.Clear();
            if (_appHost != null) UnityEngine.Object.DestroyImmediate(_appHost);
            if (_runtimeHost != null) UnityEngine.Object.DestroyImmediate(_runtimeHost);
            if (_canvasHost != null) UnityEngine.Object.DestroyImmediate(_canvasHost);
        }

        [Test]
        public void CustomHexGraphicRequiresItsOwnCanvasRenderer()
        {
            GameObject node = new GameObject("HexGraphicRegression", typeof(RectTransform));
            try
            {
                node.AddComponent<HexCellGraphic>();
                Assert.That(node.GetComponent<CanvasRenderer>(), Is.Not.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(node); }
        }

        [Test]
        public void CustomEdgesGraphicRequiresItsOwnCanvasRenderer()
        {
            GameObject node = new GameObject("EdgesGraphicRegression", typeof(RectTransform));
            try
            {
                node.AddComponent<BoardEdgesGraphic>();
                Assert.That(node.GetComponent<CanvasRenderer>(), Is.Not.Null);
            }
            finally { UnityEngine.Object.DestroyImmediate(node); }
        }

        [Test]
        public void GeneratedUiUsesLayerFiveAndEveryGraphicHasCanvasRenderer()
        {
            BuildApp();
            foreach (Transform child in _app.UiRoot.GetComponentsInChildren<Transform>(true))
                Assert.That(child.gameObject.layer, Is.EqualTo(5), child.name);
            foreach (UnityEngine.UI.Graphic graphic in _app.UiRoot.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                Assert.That(graphic.GetComponent<CanvasRenderer>(), Is.Not.Null, graphic.name);
            Assert.That(_app.UiRoot.GetComponentsInChildren<HexCellGraphic>(true).Length, Is.EqualTo(35));
            Assert.That(_app.UiRoot.GetComponentsInChildren<BoardEdgesGraphic>(true).Length, Is.EqualTo(1));
        }

        [Test]
        public void CriticalTitlesAndLiveCountersFitNotoLineBoxAndGenerateCharacters()
        {
            BuildApp();
            string[] names = { "Brand", "HeaderMoney", "HeaderSamples", "HeaderSteps", "HeaderLearning" };
            TMP_Text[] labels = _app.UiRoot.GetComponentsInChildren<TMP_Text>(true);
            foreach (string name in names)
            {
                TMP_Text label = labels.Single(value => value.name == name);
                float needed = label.GetPreferredValues(label.text, label.rectTransform.rect.width, Mathf.Infinity).y;
                Assert.That(label.rectTransform.rect.height, Is.GreaterThanOrEqualTo(needed), name + " line box");
                Assert.That(label.enableAutoSizing, Is.False, name + " must not autosize on refresh");
                label.ForceMeshUpdate(true, true);
                Assert.That(label.textInfo.characterCount, Is.GreaterThan(0), name + " must generate visible text");
            }
        }

        [Test]
        public void MailReaderTitleFitsNotoLineBoxAndGeneratesCharacters()
        {
            BuildApp("mail");
            TMP_Text title = _app.UiRoot.GetComponentsInChildren<TMP_Text>(true).Single(label => label.name == "CardTitle");
            Assert.That(title.text.Contains('\n'), Is.False, "Mail subject must exclude explicit newline / question paragraphs");
            Assert.That(title.text.Contains('\r'), Is.False, "Mail subject must also exclude CRLF separators");
            float needed = title.GetPreferredValues(title.text, title.rectTransform.rect.width, Mathf.Infinity).y;
            Assert.That(title.rectTransform.rect.height, Is.GreaterThanOrEqualTo(needed));
            title.ForceMeshUpdate(true, true);
            Assert.That(title.textInfo.characterCount, Is.GreaterThan(0));
        }

        [TestCase("lingguang", "board,skills,persona,exam")]
        [TestCase("mail", "cards")]
        [TestCase("xunbao", "shop")]
        [TestCase("home", "jobs,power")]
        [TestCase("yy", "chat")]
        public void ProfilesBuildOnlyTheirOwnPagesAndRejectCrossAppNavigation(string profile, string expectedCsv)
        {
            BuildApp(profile);
            string[] expected = expectedCsv.Split(',');
            Assert.That(_app.ProfileId, Is.EqualTo(profile));
            Assert.That(_app.AvailableTabs, Is.EqualTo(expected));
            string[] actual = _app.UiRoot.GetComponentsInChildren<Transform>(true)
                .Where(child => child.name.StartsWith("Page_", StringComparison.Ordinal))
                .Select(child => child.name.Substring(5)).ToArray();
            Assert.That(actual, Is.EquivalentTo(expected));
            string[] allTabs = { "cards", "board", "skills", "shop", "jobs", "power", "persona", "exam", "chat" };
            foreach (string foreign in allTabs.Except(expected))
            {
                string active = _app.ActiveTab;
                _app.ShowTab(foreign);
                Assert.That(_app.ActiveTab, Is.EqualTo(active), foreign);
                Assert.That(_app.UiRoot.GetComponentsInChildren<Transform>(true).Any(item => item.name == "Tab_" + foreign), Is.False);
            }
            string[] defensiveCopy = _app.AvailableTabs;
            defensiveCopy[0] = "tampered";
            Assert.That(_app.AvailableTabs, Is.EqualTo(expected));
            if (profile != "lingguang") Assert.That(_app.UiRoot.GetComponentsInChildren<HexCellGraphic>(true), Is.Empty);
        }

        [Test]
        public void ProfileMustBeKnownAndCannotChangeAfterConstruction()
        {
            BuildApp();
            Assert.Throws<ArgumentOutOfRangeException>(() => _app.ConfigureProfile("unknown"));
            Assert.Throws<InvalidOperationException>(() => _app.ConfigureProfile("mail"));
            Assert.DoesNotThrow(() => _app.ConfigureProfile("lingguang"));
        }

        [Test]
        public void SharedRuntimeMailCannotSubmitExamEvenBeforeItsNextRefresh()
        {
            var config = new GameConfig { examRequiredCards = 0, examRequiredSteps = 0, examRequiredAccuracy = 0 };
            var simulation = new ChapterOneSim(config: config);
            BuildApp("mail", simulation);
            ChapterOneApp mail = _app;
            ChapterOneApp workbench = BuildAdditionalApp("lingguang");
            workbench.ShowTab("exam");
            workbench.UiRoot.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(button => button.name == "StartExam").onClick.Invoke();
            Assert.That(simulation.S.examActive, Is.True);
            int examCardId = simulation.CurrentCard.id;
            // Mail still displays the prior training message here: guard must be synchronous.
            mail.SubmitAnswer(simulation.CurrentCard.expectedYes);
            Assert.That(simulation.S.examAnswered, Is.Zero);
            Assert.That(simulation.CurrentCard.id, Is.EqualTo(examCardId));
            Assert.That(mail.UiRoot.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(button => button.name == "CardAnswerYes").interactable, Is.False);
            for (int i = 0; i < config.examQuestions; i++) simulation.SubmitCard(simulation.CurrentCard.id, simulation.CurrentCard.expectedYes);
            Assert.That(simulation.S.examActive, Is.False);
            mail.Refresh();
            Assert.That(mail.UiRoot.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(button => button.name == "CardAnswerYes").interactable, Is.True);
            int reviewed = simulation.S.cardsReviewed;
            mail.SubmitAnswer(simulation.CurrentCard.expectedYes);
            Assert.That(simulation.S.cardsReviewed, Is.EqualTo(reviewed + 1));
            workbench.Refresh();
            Assert.That(workbench.UiRoot.GetComponentsInChildren<TMP_Text>(true).Single(label => label.name == "HeaderSamples").text,
                Does.Contain(simulation.S.samples.ToString(simulation.S.samples >= 100 ? "0" : "0.0", System.Globalization.CultureInfo.InvariantCulture)));
        }

        private void BuildApp(string profile = "lingguang", ChapterOneSim simulation = null)
        {
            _runtimeHost = new GameObject("UiTestRuntime");
            // Prevent Awake from touching disk; the injected simulation initializes this inactive host.
            _runtimeHost.SetActive(false);
            _runtime = _runtimeHost.AddComponent<ChapterOneRuntime>();
            _runtime.SetSimulationForTests(simulation ?? new ChapterOneSim());
            _canvasHost = new GameObject("UiTestCanvas", typeof(RectTransform), typeof(Canvas));
            _canvasHost.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = _canvasHost.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1236, 693);
            GameObject contentObject = new GameObject("UiTestContent", typeof(RectTransform));
            contentObject.transform.SetParent(canvasRect, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one;
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            _appHost = new GameObject("UiTestApp");
            _app = _appHost.AddComponent<ChapterOneApp>();
            _app.ConfigureProfile(profile);
            _app.Initialize(_runtime, content, null);
            Assert.That(_app.EffectiveFont, Is.Not.Null, "Existing Resources Noto font must be available");
            Canvas.ForceUpdateCanvases();
            _app.Refresh();
            Canvas.ForceUpdateCanvases();
        }

        private ChapterOneApp BuildAdditionalApp(string profile)
        {
            GameObject contentObject = new GameObject("UiTestContent_" + profile, typeof(RectTransform));
            contentObject.transform.SetParent(_canvasHost.transform, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one;
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            GameObject appHost = new GameObject("UiTestApp_" + profile);
            _extraAppHosts.Add(appHost);
            ChapterOneApp app = appHost.AddComponent<ChapterOneApp>();
            app.ConfigureProfile(profile);
            app.Initialize(_runtime, content, null);
            Canvas.ForceUpdateCanvases();
            app.Refresh();
            return app;
        }
    }
}
