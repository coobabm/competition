#if RECORDING_PREVIEW
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UI;
using TMPro;
using LingGuangV05.Desktop.OS;
using UnityEngine;
using LingGuangV05.CaseMenu;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.Zhongbao;
using LingGuangV05.XingGuang;

public sealed class RecordingPlayerSmoke : MonoBehaviour
{
    string checkpoint = "startup";
    static string ButtonText(RectTransform button)
    {
        if (button == null) throw new Exception("Missing verification button");
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null) throw new Exception("Missing button label: " + button.name);
        return label.text ?? "";
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--recording-smoke") < 0) return;
        Application.runInBackground = true;
        var profile = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "VerificationData", "DesktopProfile")) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(profile);
        typeof(DreamOSDataManager).GetField("dataPath", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, profile);
        var go = new GameObject("Recording build verification"); DontDestroyOnLoad(go); go.AddComponent<RecordingPlayerSmoke>();
    }
    IEnumerator Start()
    {
        var run = Verify();
        while (true)
        {
            object current;
            try { if (!run.MoveNext()) yield break; current = run.Current; }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "player-check.txt")), "FAIL " + checkpoint + " " + ex);
                Application.Quit(4); yield break;
            }
            yield return current;
        }
    }
    IEnumerator Verify()
    {
        float until = Time.realtimeSinceStartup + 120;
        CaseMenuController menu;
        while ((menu = FindAnyObjectByType<CaseMenuController>()) == null || !menu.IsMainReady)
        { if (Time.realtimeSinceStartup > until) { Application.Quit(2); yield break; } yield return null; }
        menu.BeginGame();
        yield return new WaitForSecondsRealtime(25);
        var runtime = FindAnyObjectByType<ChapterOneRuntime>();
        var lab = FindAnyObjectByType<XingGuangController>();
        bool ok = lab != null && lab.Sim.S.stage >= 3 && lab.Sim.SpiderAround && runtime != null
            && runtime.SavePath.Contains("Recording-Stage4") && runtime.SaveNow();
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        runtime.tickEnabled = false;
        lab.Open();
        yield return new WaitForSecondsRealtime(2);
        ok &= XingGuangView.NavIds.Length == 4;
        checkpoint = "technology navigation";
        ok &= ButtonText(lab.View.NavButton("tree")).Contains("科技");
        string previousDesk = lab.Sim.S.desk;
        foreach (var desk in XgCatalog.Desks)
        {
            if (!lab.Sim.DeskOpen(desk.id)) continue;
            checkpoint = "statement " + desk.id;
            lab.Sim.SelectDesk(desk.id);
            lab.View.ShowTab("teach"); lab.View.Teach.Refresh();
            yield return new WaitForSecondsRealtime(.15f);
            var card = lab.Sim.Card(desk.id);
            if (!XgSim.DirectTruthJudgment(card)) continue;
            ok &= ButtonText(lab.View.Teach.YesButton).StartsWith("对");
            ok &= ButtonText(lab.View.Teach.NoButton).StartsWith("错");
            var privateCaptcha = (XgCaptchaCard)typeof(XgLabelPage).GetField("captchaCard", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(lab.View.Teach);
            ok &= !privateCaptcha.Visible && lab.View.Teach.YesButton.gameObject.activeInHierarchy;
            foreach (var label in lab.View.Teach.root.GetComponentsInChildren<TMP_Text>())
                ok &= string.IsNullOrEmpty(label.text) || (!label.text.Contains("猜对了") && !label.text.Contains("猜错了") && !label.text.Contains("判断题目本身"));
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "statement-" + desk.id + ".png"));
            yield return new WaitForSecondsRealtime(.1f);
        }
        lab.Sim.SelectDesk(previousDesk);
        checkpoint = "company work";
        if (lab.Sim.CaptchaPending) ok &= lab.Sim.SubmitCaptcha(lab.Sim.CaptchaCode);
        ok &= lab.OpenCrowd("label");
        yield return new WaitForSecondsRealtime(1);
        var crowd = ZhongbaoView.Instance;
        ok &= crowd != null && crowd.Visible;
        if (crowd != null)
        {
            ok &= ButtonText(crowd.Label.YesButton).StartsWith("对");
            ok &= ButtonText(crowd.Label.NoButton).StartsWith("错");
            var work = lab.Sim.WorkCard(lab.Sim.S.workDesk);
            var privateCard = lab.Sim.Card(lab.Sim.S.workDesk);
            double samples = lab.Sim.Samples(lab.Sim.S.workDesk);
            int submitted = lab.Sim.S.workSubmitted;
            var answerButton = work.truth ? crowd.Label.YesButton : crowd.Label.NoButton;
            answerButton.GetComponent<XgLessonAnswerInput>().OnSubmit(
                new UnityEngine.EventSystems.BaseEventData(UnityEngine.EventSystems.EventSystem.current));
            yield return null;
            ok &= lab.Sim.S.workSubmitted == submitted + 1;
            ok &= lab.Sim.Samples(lab.Sim.S.workDesk) == samples;
            ok &= ReferenceEquals(lab.Sim.Card(lab.Sim.S.workDesk), privateCard);
            crowd.Refresh(true);
            yield return new WaitForSecondsRealtime(.6f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "check-crowd.png"));
            yield return new WaitForSecondsRealtime(.2f);
        }
        lab.Open();
        foreach (string page in new[] { "teach", "train", "tree", "home", "cards", "data", "repo" })
        {
            lab.View.ShowTab(page);
            yield return new WaitForSecondsRealtime(.6f);
            if (page == "data" || page == "repo") ok &= lab.View.Tab == "train";
            if (page == "cards") ok &= lab.View.Tab == "home" && lab.View.Cards.root.gameObject.activeInHierarchy;
            if (page == "tree") ok &= lab.View.Tree.root.gameObject.activeInHierarchy;
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "check-" + page + ".png"));
            yield return new WaitForSecondsRealtime(.15f);
        }
        lab.View.ShowTab("data"); lab.View.Data.OpenCrawlerIntro();
        yield return new WaitForSecondsRealtime(1);
        ok &= lab.View.Data.CrawlerOpen && lab.View.Tab == "train";
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, "check-crawler.png"));
        lab.View.ShowTab("home");
        yield return null;
        ok &= !lab.View.Data.CrawlerOpen;
        var brain = FindAnyObjectByType<XgBrainWallpaper>();
        ok &= brain != null && brain.GetComponent<Image>().color.a == 0;
        ok &= lab.Window.windowContainer.Find(XgBrainWindowGlass.BackingName) != null;
        lab.Window.MinimizeWindow();
        var wallpaper = FindAnyObjectByType<WallpaperManager>();
        if (wallpaper != null && wallpaper.wallpaperLibrary.wallpapers.Count > 1)
        {
            int original = wallpaper.wallpaperIndex;
            wallpaper.SetWallpaper((original + 1) % wallpaper.wallpaperLibrary.wallpapers.Count, true);
            yield return new WaitForSecondsRealtime(1);
            ok &= brain.gameObject.activeInHierarchy && brain.Reveal.Visible;
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "check-wallpaper.png"));
            wallpaper.SetWallpaper(original, true);
        }
        // Exercise the destroyed-object pool that survived a return-to-title scene reload.
        var spider = XgResidentSpider.Instance;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var pool = (Stack<TMP_Text>)typeof(XgResidentSpider).GetField("ghostPool", flags).GetValue(spider);
        var dead = new GameObject("Destroyed ghost", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        pool.Push(dead); Destroy(dead.gameObject); yield return null;
        var live = new GameObject("Source word", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        live.text = "测试"; live.font = lab.View.NavButton("teach").GetComponentInChildren<TMP_Text>().font;
        live.ForceMeshUpdate();
        var wordType = typeof(XgResidentSpider).GetNestedType("Word", BindingFlags.NonPublic);
        var word = Activator.CreateInstance(wordType);
        wordType.GetField("text").SetValue(word, live); wordType.GetField("count").SetValue(word, 1);
        typeof(XgResidentSpider).GetMethod("Ghost", flags).Invoke(spider, new object[] { word, Color.cyan });
        Destroy(live.gameObject);
        spider.Recall(); yield return new WaitForSecondsRealtime(2);
        var layer = brain.transform.parent.Find("Resident Spider");
        ok &= layer != null && layer.gameObject.activeInHierarchy && layer.GetComponent<CanvasGroup>().alpha > .9f;
        File.WriteAllText(Path.Combine(dir, "player-check.txt"), ok ? "PASS stage=4 spider=true isolated save round-trip" : "FAIL");
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, "recording-desktop.png"));
        yield return new WaitForSecondsRealtime(2);
        Application.Quit(ok ? 0 : 3);
    }
}
#endif
