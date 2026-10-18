#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Core.Story;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using LingGuangV05.Desktop.Apps;

namespace LingGuangV05.Editor
{
    public static class DesktopLocalizationQa
    {
        [Serializable] public class Check { public string name, detail; public bool passed; }
        [Serializable] public class Report { public bool completed; public int passed, failed; public List<Check> checks = new List<Check>(); }
        public static Report Result { get; private set; }
        public static bool Running { get; private set; }
        private static readonly Queue<Action> steps = new Queue<Action>();
        private static ChapterOneDesktopRouter router;
        private static ChapterOneRuntime runtime;
        private static DesktopLanguageController locale;
        private static double next;
        private static StoryDesktopPresenter story;
        private static object narrationRoutine;
        private static int narrationDone;
        private static string state;
        private const string Output = "Evidence/Localization/play-qa.json";

        public static void Start()
        {
            if (!Application.isPlaying || Running) throw new InvalidOperationException("Play Main before QA.");
            Result = new Report(); steps.Clear();
            router = Object.FindFirstObjectByType<ChapterOneDesktopRouter>();
            locale = Object.FindFirstObjectByType<DesktopLanguageController>();
            runtime = router.Get("lingguang").Runtime;
            runtime.SetSimulationForTests(new ChapterOneSim()); runtime.tickEnabled = false;
            runtime.Sim.S.aiName = "家庭 Name <raw>";
            CheckNow("Chinese initial locale", locale.LanguageId == "zh-CN");
            CheckNow("native manager enabled, isolated preference storage", locale.manager.UIManagerAsset.enableLocalization && !locale.manager.saveLanguageChanges);
            CheckNow("original seven native tables retained", locale.manager.currentLanguageAsset.tableList.Count == 7);
            CheckNow("no game disk save during QA", !runtime.useDiskSave);
            foreach (string language in new[] { "zh-CN", "en-US", "zh-CN" })
            {
                string id = language;
                steps.Enqueue(() => { locale.SetLanguage(id); state = JsonUtility.ToJson(runtime.Sim.S); });
                foreach (string profile in new[] { "mail", "lingguang", "xunbao", "home", "yy" })
                {
                    string app = profile;
                    steps.Enqueue(() => { foreach (var b in router.Bindings) b.CloseApp(); router.Open(app); });
                    steps.Enqueue(() =>
                    {
                        var b = router.Get(app);
                        CheckNow(id + " " + app + " opened", b.IsOpen);
                        CheckNow(id + " " + app + " shared locale", GameText.LanguageId == id);
                        if (b.NativeApp != null)
                        {
                            var label = b.NativeApp.Bindings.texts.First(t => t.id == "Status").control;
                            CheckNow(id + " " + app + " dynamic status", app == "xunbao" ? label.text == (id == "en-US" ? "Product filter updated." : "商品筛选已更新。") : id == "en-US" ? label.text.Contains("Local learner") : label.text.Contains("本地学习机"), label.text);
                        }
                        if (app == "mail")
                        {
                            var label = b.App.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "CardPrompt");
                            CheckNow(id + " mail dynamic prompt", label.text.Contains(id == "en-US" ? "Is this email spam?" : "这封邮件是垃圾邮件吗？"), label.text);
                        }
                        CheckNow(id + " " + app + " leaves simulation unchanged", JsonUtility.ToJson(runtime.Sim.S) == state);
                    });
                }
            }
            steps.Enqueue(() =>
            {
                router.Open("yy"); var shell = Object.FindFirstObjectByType<YY2010ShellController>(); shell.OpenChat();
                var manager = shell.Manager; manager.messageInput.text = "KEEP 家庭 <raw>"; manager.CreateMessageFromInput();
                manager.messageInput.text = "DRAFT 寻宝 <raw>";
                next = EditorApplication.timeSinceStartup + 2;
            });
            steps.Enqueue(() =>
            {
                var manager = router.Get("yy").NativeChat.Manager; locale.SetLanguage("en-US");
                CheckNow("unsent input preserved", manager.messageInput.text == "DRAFT 寻宝 <raw>");
                CheckNow("sent input preserved", manager.selectedLayout.messageParent.GetComponentsInChildren<ChatMessagePreset>(true).Any(m => m.contentText.text == "KEEP 家庭 <raw>"));
                CheckNow("authored NPC history translated", manager.selectedLayout.messageParent.GetComponentsInChildren<ChatMessagePreset>(true).Any(m => m.contentText.text.StartsWith("I am Lao Zhou's preset local help")));
                var placeholder = manager.messageInput.transform.Find("Placeholder");
                if (placeholder != null) CheckNow("nonempty input hides native placeholder", !placeholder.gameObject.activeInHierarchy || (placeholder.GetComponent<CanvasGroup>() != null && placeholder.GetComponent<CanvasGroup>().alpha == 0) || (placeholder.GetComponent<UnityEngine.UI.Graphic>() != null && !placeholder.GetComponent<UnityEngine.UI.Graphic>().enabled));
                CheckNow("AI name preserved", runtime.Sim.S.aiName == "家庭 Name <raw>");
                CheckNow("saved English preference", PlayerPrefs.GetString(DesktopLanguageController.PreferenceKey) == "en-US");
                router.Get("yy").CloseApp();
            });
            steps.Enqueue(() => { locale.SetLanguage("zh-CN"); router.Open("yy"); });
            steps.Enqueue(() =>
            {
                var manager = router.Get("yy").NativeChat.Manager;
                CheckNow("hidden reopened chat keeps input", manager.messageInput.text == "DRAFT 寻宝 <raw>");
                CheckNow("hidden reopened NPC returns to Chinese", manager.selectedLayout.messageParent.GetComponentsInChildren<ChatMessagePreset>(true).Any(m => m.contentText.text.StartsWith("我是老周的本地预设帮助")));
                foreach (var b in router.Bindings) b.CloseApp();
                story = Object.FindFirstObjectByType<StoryDesktopPresenter>();
                narrationDone = 0; story.typeSpeed = 1;
                story.Execute(new StoryCommand { Op = "narrate", Text = "第一道题。我什么都不懂，但我很确定。" }, () => narrationDone++);
            });
            steps.Enqueue(() =>
            {
                narrationRoutine = Field("narrationRoutine");
                var label = (TMP_Text)Field("narrationText");
                int before = label.maxVisibleCharacters; locale.SetLanguage("en-US");
                CheckNow("mid-narration switches text", label.text.Contains("first question") || label.text.Contains("first task"), label.text);
                CheckNow("mid-narration does not restart or complete", ReferenceEquals(narrationRoutine, Field("narrationRoutine")) && narrationDone == 0);
                CheckNow("typewriter progress not reset", before == 0 || label.maxVisibleCharacters > 0);
                locale.SetLanguage("zh-CN");
                CheckNow("narration roundtrip Chinese", label.text == "第一道题。我什么都不懂，但我很确定。");
                story.StopAllCoroutines(); story.enabled = false;
            });
            steps.Enqueue(() =>
            {
                // Language switching lives only in native Settings (辅助功能 → 语言); the taskbar has no shortcut.
                CheckNow("taskbar has no language shortcut", GameObject.Find("DesktopLanguageShortcut") == null);
                locale.SetLanguage("zh-CN");
                var selector = locale.manager.languageSelectors.First(); selector.NextItem();
            });
            steps.Enqueue(() =>
            {
                CheckNow("native Settings selector switches facade", locale.LanguageId == "en-US" && GameText.IsEnglish);
                locale.SetLanguage("zh-CN"); router.Open("lingguang", "board");
                CheckNow("native Settings selector label synchronized", locale.manager.languageSelectors.First().label.text == "简体中文");
            });
            Running = true; next = EditorApplication.timeSinceStartup + .6;
            EditorApplication.update += Advance;
        }
        private static object Field(string name) => typeof(StoryDesktopPresenter).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(story);
        private static void Advance()
        {
            if (!Application.isPlaying) { CheckNow("QA not interrupted", false); Finish(); return; }
            if (EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + .65;
            if (steps.Count == 0) { Finish(); return; }
            try { steps.Dequeue()(); }
            catch (Exception e) { CheckNow("QA step exception", false, e.ToString()); }
            Save();
        }
        private static void CheckNow(string name, bool passed, string detail = "")
        { Result.checks.Add(new Check {name=name,passed=passed,detail=detail}); if(passed)Result.passed++;else Result.failed++; }
        private static void Finish() { Running=false; Result.completed=true; EditorApplication.update-=Advance; ChapterOneQaReloadGuard.Release(); Save(); }
        private static void Save() { Directory.CreateDirectory("Evidence/Localization"); File.WriteAllText(Output, JsonUtility.ToJson(Result,true)); }
    }
}
#endif
