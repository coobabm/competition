#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Bodu;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Desktop.Miaoyu;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Actual Main/LCD archive checks. All setup dates/save switches are explicitly synthetic fixtures.
// Run through BookLibraryUiChecks' scene, input, save-directory and restoration lifecycle.
public static class PublicDiscourseUiChecks
{
    const string Key = "SidebarLayoutChecks.PublicDiscourse";
    const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static bool Requested => SessionState.GetBool(Key, false);
    public static readonly List<string> Checks = new List<string>();
    public static int AssertionCount { get; private set; }
    public static bool SourceChecksExecuted { get; private set; }
    public static void ClearRequest() => SessionState.EraseBool(Key);
    [MenuItem("Tools/Local Verification/Public discourse actual LCD")]
    public static void Run()
    {
        SessionState.SetBool(Key, true);
        try { BookLibraryUiChecks.Run(); } catch { ClearRequest(); throw; }
    }
    static object Field(object o, string name)
    {
        for (var t = o.GetType(); t != null; t = t.BaseType)
        { var f = t.GetField(name, F); if (f != null) return f.GetValue(o); }
        throw new InvalidOperationException("Missing field " + name);
    }
    static void Need(bool ok, string why)
    { AssertionCount++; if (!ok) throw new InvalidOperationException("Discourse UI: " + why); }
    static string Delta(string before,string after)
    {
        if(before==after)return "unchanged";
        int at=0;while(at<before.Length&&at<after.Length&&before[at]==after[at])at++;
        int start=Math.Max(0,at-45);
        return "at "+at+" before="+before.Substring(start,Math.Min(180,before.Length-start))+" after="+after.Substring(start,Math.Min(180,after.Length-start));
    }
    static void Done(string text) { Checks.Add(text); Debug.Log("Discourse UI: " + text); }
    static IEnumerable<object> Wait(double seconds = .7)
    { double end = EditorApplication.timeSinceStartup + seconds; while (EditorApplication.timeSinceStartup < end) yield return null; }
    static IEnumerable<object> Until(Func<bool> predicate, string why)
    {
        double end = EditorApplication.timeSinceStartup + 10;
        while (!predicate()) { if (EditorApplication.timeSinceStartup >= end) Need(false, why); yield return null; }
        Canvas.ForceUpdateCanvases(); yield return null; yield return null;
    }
    static WindowManager Window(Component view) => (WindowManager)Field(view, "window");
    static IEnumerable<object> Ready(Component view)
    {
        var w = Window(view); w.FocusToWindow();
        foreach (var f in Until(() => {
            if (!w.isOn || !w.gameObject.activeInHierarchy) return false;
            foreach (var g in w.GetComponentsInParent<CanvasGroup>(true))
                if (g.alpha < .95f || !g.interactable || !g.blocksRaycasts) return false;
            var a = w.windowAnimator;
            return a == null || !a.isActiveAndEnabled || !a.IsInTransition(0) && a.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1;
        }, "native window naturally interactive: " + w.name)) yield return f;
        foreach (var f in Wait()) yield return f;
    }
    static string Label(Button b) => b.GetComponentInChildren<TMP_Text>(true)?.text ?? "";
    static Button Find(Component root, Func<Button, bool> match, int index = 0)
    {
        var bs = root.GetComponentsInChildren<Button>().Where(b => b.gameObject.activeInHierarchy && b.IsInteractable() && match(b)).ToArray();
        Need(index >= 0 && index < bs.Length, "live button " + index + " in " + root.name + " (matches=" + bs.Length + ")");
        return bs[index];
    }
    static string Texts(Component root) => string.Join("\n", root.GetComponentsInChildren<TMP_Text>().Where(t => t.gameObject.activeInHierarchy).Select(t => t.text));
    static void Reveal(RectTransform target)
    {
        var scroll = target.GetComponentInParent<ScrollRect>(); if (scroll == null) return;
        Canvas.ForceUpdateCanvases(); scroll.StopMovement();
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, target);
        float hidden = scroll.content.rect.height - scroll.viewport.rect.height;
        if (hidden > 0) scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition - (scroll.viewport.rect.center.y - bounds.center.y) / hidden);
        Canvas.ForceUpdateCanvases();
    }
    static IEnumerable<object> Tap(Button button, Action<RectTransform> click)
    {
        Reveal((RectTransform)button.transform); yield return null;
        click((RectTransform)button.transform); // Shared real LCD mapper validates raycast ownership.
        foreach (var f in Wait()) yield return f;
    }
    static IEnumerable<object> Capture(string name, Action<string> capture)
    { Canvas.ForceUpdateCanvases(); yield return null; capture(name); }
    static void Body(Component root, XgPublicDiscourseRecord row, bool english, bool escaped)
    {
        string body = escaped ? XgPublicDiscourseDesktop.Escape(row.Body(english)) : row.Body(english);
        var texts = root.GetComponentsInChildren<TMP_Text>().Where(t => t.gameObject.activeInHierarchy && t.text.Contains(body)).ToArray();
        Need(texts.Length == 1, "exact approved body appears once: " + row.id);
        var text = texts[0]; text.ForceMeshUpdate();
        Need(!text.isTextOverflowing && text.preferredHeight <= text.rectTransform.rect.height + 2, "body is not truncated: " + row.id);
        Need(Texts(root).Contains(XgPublicDiscourseDesktop.Caption(row)), "honest source/scope caption: " + row.id);
    }
    static Action Retain(Button b)
    {
        // Adversarial injection only, never counted as an LCD click.
        var calls = (IEnumerable)typeof(UnityEventBase).GetMethod("PrepareInvoke", F).Invoke(b.onClick, null);
        var retained = calls.Cast<object>().ToArray();
        return () => { foreach (var c in retained) c.GetType().GetMethod("Invoke", F, null, new[] { typeof(object[]) }, null).Invoke(c, new object[] { Array.Empty<object>() }); };
    }
    static ChapterOneSim House(int stage, bool english = false)
    {
        var lab = new XgSim(); lab.BeginOpening(); lab.English = english;
        lab.S.stage = lab.S.stageVision = lab.S.stageSequence = stage;
        lab.S.abilitiesEmerged = Enumerable.Range(1, stage).ToList();
        lab.S.teachingFlow = false; lab.S.spiderIntroDone = lab.S.firstWordsSaid = true;
        lab.S.teachIntroSaid = lab.S.teachThirdSaid = lab.S.teachTreeSaid = lab.S.teachTrainSaid = true;
        lab.MarkAfterthought("first.drift");
        var yy = new YYState { lingguangUnlocked = true };
        foreach (var c in YYChatHub.Contacts) yy.conversations.Add(new YYConversation { id = c.id });
        var house = new ChapterOneSim(); house.S.prologue = 2; house.S.appInstallState = 2; house.S.money = 1000000;
        house.S.labState = JsonUtility.ToJson(lab.S); house.S.chatState = JsonUtility.ToJson(yy);
        GameCalendar.Advance(house.S, GameCalendar.DayFor(stage, 0)); house.S.rentDay = GameCalendar.CurrentDay(house.S); house.S.broadbandMonth = 201612;
        return house;
    }
    static void Bind(XingGuangController ctl, ChapterOneSim house)
    {
        ctl.runtime.SetSimulationForTests(house);
        typeof(ChapterOneRuntime).GetProperty("TestMode", F).GetSetMethod(true).Invoke(ctl.runtime, new object[] { false });
        ctl.runtime.useDiskSave = false; ctl.runtime.tickEnabled = false;
        Need(ctl.BoundTo(house) && YYChatHub.Instance.BoundTo(house), "real controller and YY bind fixture save synchronously");
        // Isolate the identified independent 20-second historical group-message timer.
        // The archive must remain read-only; a naturally due Overwatch launch line is unrelated.
        typeof(YYChatHub).GetField("eraWait",F).SetValue(YYChatHub.Instance,float.MaxValue);
        DesktopNotifications.Install(ctl.runtime);
    }
    static void Date(XingGuangController ctl, long revision, int date)
    {
        // Display-date fixture only: no artifact, route, fabricated event, or persisted release receipt.
        XgPublicDiscourseDesktop.SetReleaseProjection(ctl.runtime.Sim, revision, date, "", "", Array.Empty<string>());
        Need(XgPublicDiscourseDesktop.Context("news").publicDate == date, "explicit public date fixture applied");
    }
    static XgPublicDiscourseRecord Row(string id) => XgPublicDiscourseDesktop.Catalog.All.Single(r => r.id == id);
    static IEnumerable<object> BrowserHome(BoduView browser)
    { browser.Open(""); foreach (var f in Ready(browser)) yield return f; }
    static IEnumerable<object> BrowserArchive(BoduView browser, Action<RectTransform> click)
    {
        foreach (var f in BrowserHome(browser)) yield return f;
        foreach (var f in Tap(Find(browser, b => Label(b) == XgPublicDiscourseDesktop.Heading), click)) yield return f;
        Need((string)Field(browser, "page") == "discourse", "homepage LCD entry opens archive");
    }

    // Root may replace the loaded-source suite with a broader end-to-end release UI suite.
    public static IEnumerator Frames(XingGuangController ctl, Action<bool> language, Action<RectTransform> click, Action<string> capture,
        Func<XingGuangController, Action<bool>, Action<RectTransform>, Action<string>, IEnumerator> sourceFrames = null)
    {
        Checks.Clear(); AssertionCount = 0; SourceChecksExecuted = false;
        var rt = ctl.runtime; var yy = YYChatHub.Instance; var forum = TiebaHub.Instance;
        var browser = UnityEngine.Object.FindAnyObjectByType<BoduView>(FindObjectsInactive.Include);
        var market = MiaoyuView.Instance;
        Need(browser != null && forum?.View != null && yy?.View != null && market != null, "all four real native views installed in Main fixture");
        Need(!rt.useDiskSave && !rt.tickEnabled && !rt.TestMode, "isolated Main fixture without disk writes/economy ticks");
        var original = rt.Sim; bool enabled = ctl.enabled, englishBefore = GameText.IsEnglish;
        float originalEraWait=(float)Field(yy,"eraWait");
        var oldProbe = StoryFocus.Probe; var oldExclusive = StoryFocus.ExclusiveProbe;
        var notifications = DesktopNotifications.Install(rt); bool notifyEnabled = notifications.enabled;
        var notifier = ctl.GetComponent<XgPublicDiscourseNotifier>(); bool notifierEnabled = notifier != null && notifier.enabled;
        // This suite reads archives, not unrelated memory discovery or narrative prompts.
        var preparation = ctl.GetComponent<XgEndingPreparationHost>(); bool preparationEnabled = preparation != null && preparation.enabled;
        // The resident spider genuinely harvests visible words independently of archive clicks.
        var resident = UnityEngine.Object.FindAnyObjectByType<XgResidentSpider>(FindObjectsInactive.Include);
        bool residentEnabled = resident != null && resident.enabled;
        var loans = UnityEngine.Object.FindObjectsByType<ZhouCardLoan>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var loanStates = loans.Select(x => x.enabled).ToArray();
        var forumView = forum.View; var yyView = yy.View;
        ctl.enabled = false; notifications.enabled = false; if (notifier != null) notifier.enabled = false;
        if (preparation != null) preparation.enabled = false;
        if (resident != null) resident.enabled = false;
        StoryFocus.Probe = () => true; StoryFocus.ExclusiveProbe = () => false;
        foreach (var loan in loans) { loan.StopAllCoroutines(); loan.enabled = false; }
        try
        {
            var brain = ctl.GetComponent<XgBrainWindow>();
            if (brain != null)
            {
                var panel = Field(brain, "window") as RectTransform;
                if (panel != null && panel.gameObject.activeInHierarchy)
                { click((RectTransform)panel.Find("Title/Close")); foreach (var f in Wait(.2)) yield return f; }
            }
            ctl.Window.CloseWindow();
            foreach (bool english in new[] { false, true })
            {
                language(english); Bind(ctl, House(6, english)); Date(ctl, 100, 20170613);
                foreach (var f in BrowserArchive(browser, click)) yield return f;
                Need(!Texts(browser).Contains(Row("R1-N").Title(english)), "no release-response news before saved public evidence");
                var newsRows = XgPublicDiscourseDesktop.Archive("news");
                for (int i = 0; i < newsRows.Count; i++)
                {
                    var row = newsRows[i];
                    // Re-enter via the live homepage link so each detail action has a current bound callback.
                    foreach (var f in BrowserArchive(browser, click)) yield return f;
                    int sameBefore = newsRows.Take(i).Count(r => r.Title(english) == row.Title(english));
                    foreach (var f in Tap(Find(browser, b => Label(b) == XgPublicDiscourseDesktop.Escape(row.Title(english)), sameBefore), click)) yield return f;
                    Body(browser, row, english, true);
                }
                foreach (var f in Capture("discourse-" + (english ? "en" : "zh") + "-bodu", capture)) yield return f;
                forumView.Open(); foreach (var f in Ready(forumView)) yield return f;
                foreach (var f in Tap(Find(forumView, b => Label(b) == XgPublicDiscourseDesktop.Heading), click)) yield return f;
                var forumRows = XgPublicDiscourseDesktop.Native("forum");
                for (int i = 0; i < forumRows.Count; i++)
                {
                    var row = forumRows[i];
                    int sameBefore = forumRows.Take(i).Count(r => r.Title(english) == row.Title(english));
                    foreach (var f in Tap(Find(forumView, b => Label(b) == XgPublicDiscourseDesktop.Escape(row.Title(english)), sameBefore), click)) yield return f;
                    Need((string)Field(forumView, "arg") == row.id, "native forum title resolves exact row " + row.id);
                    Body(forumView, row, english, true);
                    if (i == 0) foreach (var f in Capture("discourse-" + (english ? "en" : "zh") + "-tieba", capture)) yield return f;
                    foreach (var f in Tap(Find(forumView, b => Label(b) == XgPublicDiscourseDesktop.Heading), click)) yield return f;
                }
                market.Open("sell"); foreach (var f in Ready(market)) yield return f;
                foreach (var f in Tap(Find(market, b => b.name == "Tab public"), click)) yield return f;
                Need((string)Field(market, "tab") == "public", "actual Miaoyu public tab opens");
                foreach (var row in XgPublicDiscourseDesktop.Native("miaoyu")) Body(market, row, english, false);
                foreach (var f in Capture("discourse-" + (english ? "en" : "zh") + "-miaoyu", capture)) yield return f;
                yyView.EnsureReady(); yyView.OnOpened(YYChatHub.EraGroup); Window(yyView).OpenWindow();
                foreach (var f in Ready(yyView)) yield return f;
                if ((bool)Field(yyView, "discourseOpen"))
                    foreach (var f in Tap(Find(yyView, b => b.name == "Public discussion archive"), click)) yield return f;
                string chatBefore = JsonUtility.ToJson(yy.S); string deliveryBefore = JsonUtility.ToJson(ctl.Sim.S.publicDiscourseDelivery);
                foreach (var f in Tap(Find(yyView, b => b.name == "Public discussion archive"), click)) yield return f;
                foreach (var row in XgPublicDiscourseDesktop.Native("yy")) Body(yyView, row, english, false);
                Need(JsonUtility.ToJson(yy.S) == chatBefore, "expanding YY archive creates no messages, unread counts or membership");
                Need(JsonUtility.ToJson(ctl.Sim.S.publicDiscourseDelivery) == deliveryBefore, "archive reads never synthesize notification delivery receipts");
                Reveal((RectTransform)Find(yyView, b => b.name == "Public discussion archive").transform); yield return null;
                foreach (var f in Capture("discourse-" + (english ? "en" : "zh") + "-yy", capture)) yield return f;
                Done((english ? "English" : "Chinese") + ": Bodu/Tieba LCD entry and exact details, Miaoyu tab, YY archive; exact approved text and no overflow.");
            }

            language(false); Bind(ctl, House(1)); Date(ctl, 100, 20160523);
            foreach (var f in BrowserHome(browser)) yield return f;
            Need(!browser.GetComponentsInChildren<Button>().Any(b => Label(b) == XgPublicDiscourseDesktop.Heading), "archive entry absent before first publication date");
            Date(ctl, 101, 20160524);
            foreach (var f in BrowserArchive(browser, click)) yield return f;
            Need(Texts(browser).Contains(Row("D1-B").Title(false)) && !Texts(browser).Contains(Row("D2-B").Title(false)), "native Bodu obeys stage-one gate");
            Need(!XgPublicDiscourseDesktop.Context("miaoyu").Installed("miaoyu") && XgPublicDiscourseDesktop.Native("miaoyu").Count == 0, "stage-locked Miaoyu has no native delivery");
            foreach (var f in Tap(Find(browser, b => Label(b) == XgPublicDiscourseDesktop.PlatformName("miaoyu")), click)) yield return f;
            foreach (var f in Tap(Find(browser, b => Label(b) == Row("D1-M").Title(false)), click)) yield return f;
            Body(browser, Row("D1-M"), false, true);
            Need(!XgPublicDiscourseDesktop.Context("miaoyu").Installed("miaoyu"), "labelled browser excerpt does not unlock Miaoyu");
            // Explicit absence fixture: a closed/minimized window would not prove absence.
            forum.View = null; yy.View = null;
            try
            {
                Need(XgPublicDiscourseDesktop.Native("forum").Count == 0 && XgPublicDiscourseDesktop.Native("yy").Count == 0, "unavailable native views produce no native rows");
                string chats = JsonUtility.ToJson(yy.S), posts = JsonUtility.ToJson(forum.S);
                foreach (var f in BrowserArchive(browser, click)) yield return f;
                foreach (var f in Tap(Find(browser, b => Label(b) == XgPublicDiscourseDesktop.PlatformName("forum")), click)) yield return f;
                foreach (var f in Tap(Find(browser, b => Label(b) == Row("D1-F").Title(false)), click)) yield return f;
                Body(browser, Row("D1-F"), false, true);
                foreach (var f in Tap(Find(browser, b => Label(b) == XgPublicDiscourseDesktop.PlatformName("yy")), click)) yield return f;
                foreach (var f in Tap(Find(browser, b => Label(b) == Row("D1-Y").Title(false)), click)) yield return f;
                Body(browser, Row("D1-Y"), false, true);
                Need(forum.View == null && yy.View == null && JsonUtility.ToJson(yy.S) == chats && JsonUtility.ToJson(forum.S) == posts,
                    "browser excerpt neither installs apps nor creates conversations/posts; views="+(forum.View==null)+"/"+(yy.View==null)+"; YY="+Delta(chats,JsonUtility.ToJson(yy.S))+"; forum="+Delta(posts,JsonUtility.ToJson(forum.S)));
            }
            finally { forum.View = forumView; yy.View = yyView; }
            Bind(ctl, House(6)); Date(ctl, 100, 20161231);
            foreach (var f in BrowserArchive(browser, click)) yield return f;
            Need(!Texts(browser).Contains(Row("H-01").Title(false)), "2016 stage-six page cannot leak 2017 historical headline");
            Date(ctl, 101, 20170612);
            foreach (var f in BrowserArchive(browser, click)) yield return f;
            Need(!Texts(browser).Contains(Row("H-01").Title(false)), "historical row withheld before June 13 display gate");
            Date(ctl, 102, 20170613);
            foreach (var f in BrowserArchive(browser, click)) yield return f;
            foreach (var f in Tap(Find(browser, b => Label(b) == Row("H-01").Title(false)), click)) yield return f;
            Body(browser, Row("H-01"), false, true);
            Need(Texts(browser).Contains("2017-06-13") && Texts(browser).Contains("历史资料摘要"), "historical row date and kind are explicit");
            Done("Actual date/stage gates and absent-view browser excerpts checked without installation, messages or receipts.");

            foreach (var f in BrowserArchive(browser, click)) yield return f;
            var staleBodu = Retain(Find(browser, b => Label(b) == Row("D1-B").Title(false)));
            forumView.Open(); foreach (var f in Ready(forumView)) yield return f;
            var staleForum = Retain(Find(forumView, b => Label(b) == XgPublicDiscourseDesktop.Heading));
            yyView.OnOpened(YYChatHub.EraGroup); Window(yyView).OpenWindow(); foreach (var f in Ready(yyView)) yield return f;
            var staleYy = Retain(Find(yyView, b => b.name == "Public discussion archive"));
            var token = XgPublicDiscourseDesktop.Capture(); var oldOwner = rt.Sim;
            Bind(ctl, House(1)); foreach (var f in Wait()) yield return f;
            string page = (string)Field(browser, "page"), id = (string)Field(browser, "discourseId"), where = (string)Field(forumView, "view");
            bool expanded = (bool)Field(yyView, "discourseOpen"); string state = JsonUtility.ToJson(rt.Sim.S);
            staleBodu(); staleForum(); staleYy();
            Need(!token.Current && (string)Field(browser, "page") == page && (string)Field(browser, "discourseId") == id
                && (string)Field(forumView, "view") == where && (bool)Field(yyView, "discourseOpen") == expanded
                && JsonUtility.ToJson(rt.Sim.S) == state, "retained old-save callbacks cannot navigate, toggle or mutate new save");
            XgPublicDiscourseDesktop.SetReleaseProjection(oldOwner, 999, 20170613, "online", "old", new[] { "online.link_shared_publicly" });
            Need(XgPublicDiscourseDesktop.Context("news").route == "", "stale prior-save projection rejected");
            Need(XgPublicDiscourseDesktop.Native("miaoyu").Count == 0 && !Texts(market).Contains(Row("D6-M").Body(false)), "Miaoyu refresh does not retain old stage-six archive after swap");
            Done("Explicit adversarial retained callbacks rejected after actual save replacement (not counted as pointer interactions).");
            if (sourceFrames == null) sourceFrames = SavedSourceFrames;
            if (sourceFrames != null)
            {
                var source = sourceFrames(ctl, language, click, capture);
                Need(source != null, "saved-source hook supplies an actual frame suite");
                try { while (source.MoveNext()) yield return source.Current; }
                finally { (source as IDisposable)?.Dispose(); }
                SourceChecksExecuted = true; Done("Saved-public-source LCD suite completed against serialized production-flow fixtures.");
            }
        }
        finally
        {
            forum.View = forumView; yy.View = yyView;
            Bind(ctl, original); ctl.enabled = enabled; language(englishBefore);
            if (preparation != null) preparation.enabled = preparationEnabled;
            if (resident != null) resident.enabled = residentEnabled;
            typeof(YYChatHub).GetField("eraWait",F).SetValue(yy,originalEraWait);
            StoryFocus.Probe = oldProbe; StoryFocus.ExclusiveProbe = oldExclusive;
            notifications.enabled = notifyEnabled; if (notifier != null) notifier.enabled = notifierEnabled;
            for (int i = 0; i < loans.Length; i++) if (loans[i] != null) loans[i].enabled = loanStates[i];
        }
    }

    static ChapterOneSim SourceHouse(string route, bool english, string night = "completed", bool publish = true)
    {
        // Synthetic prior gameplay receipts. The production flow creates and validates the source snapshots.
        // This serialized roundtrip verifies loaded-source UI, not the real training/night gameplay or disk IO.
        var house = House(6, english); var lab = JsonUtility.FromJson<XgState>(house.S.labState); string snapshot = "";
        var flow = new XgEpilogueFlow(lab, house, () => true, () => { snapshot = JsonUtility.ToJson(lab); return true; });
        var release = new ReleaseOutcome {
            route = route, decisionOwner = "player", shutdownPin = "unpinned", committedAt = 20161231,
            artifactId = route == "local_only" ? "" : "fixture:artifact", releaseReceiptId = "fixture:release",
            publicName = "Public UI fixture", publicDescription = "Fixture description", publicLimitations = "Fixture limits", publicTerms = "Fixture terms",
            prerequisites = new XgReleasePrerequisites {
                memoryCardsReceiptId = "fixture:memory", redTeamOneReceiptId = "fixture:red1", redTeamTwoReceiptId = "fixture:red2",
                redTeamThreeReceiptId = "fixture:red3", fixedThreeSlotsReceiptId = "fixture:slots"
            }
        };
        Need(flow.CommitRelease(flow.CaptureBinding(), release).Success, "fixture release passes production prerequisites");
        if (route == "online")
        {
            Need(flow.RecordLaunchNightResult(flow.CaptureBinding(), new XgLaunchNightReceipt {
                receiptId = "fixture:night", sourceRef = "fixture:night-run", releaseReceiptId = release.releaseReceiptId,
                artifactId = release.artifactId, ending = night, endedAt = 20170101
            }).Success, "fixture terminal night receipt accepted");
            Need(flow.EnterEpilogue(flow.CaptureBinding()).Success, "fixture online epilogue entered");
        }
        Need(flow.Advance(flow.CaptureBinding()).Success && flow.Advance(flow.CaptureBinding()).Success && lab.epilogue.phase == "route", "fixture reaches authored-scene phase");
        if (publish) Need(flow.PublishAuthoredPublicEvents(flow.CaptureBinding(), XgPublicDiscourseDesktop.Catalog).Success, "production flow creates approved fixture source snapshots");
        house.S.labState = snapshot; return house;
    }
    static IEnumerable<object> BrowserRecord(BoduView browser, XgPublicDiscourseRecord row, bool english, Action<RectTransform> click)
    {
        foreach (var f in BrowserArchive(browser, click)) yield return f;
        if (row.platform != "news")
            foreach (var f in Tap(Find(browser, b => Label(b) == XgPublicDiscourseDesktop.PlatformName(row.platform)), click)) yield return f;
        var rows = XgPublicDiscourseDesktop.Archive(row.platform); int index = rows.FindIndex(r => r.id == row.id);
        Need(index >= 0, "row available before detail click: " + row.id);
        int sameBefore = rows.Take(index).Count(r => r.Title(english) == row.Title(english));
        foreach (var f in Tap(Find(browser, b => Label(b) == XgPublicDiscourseDesktop.Escape(row.Title(english)), sameBefore), click)) yield return f;
        Need((string)Field(browser, "discourseId") == row.id, "LCD title opens exact source-bearing row: " + row.id);
    }
    public static IEnumerator SavedSourceFrames(XingGuangController ctl, Action<bool> language, Action<RectTransform> click, Action<string> capture)
    {
        var browser = UnityEngine.Object.FindAnyObjectByType<BoduView>(FindObjectsInactive.Include);
        foreach (bool english in new[] { false, true }) foreach (string route in new[] { "online", "public_model" })
        {
            language(english); Bind(ctl, SourceHouse(route, english));
            foreach (var f in Wait()) yield return f; // Real mail bridge projects only verified event IDs.
            var director = ctl.Epilogue;
            Need(director != null && director.CaptureBinding() != null, "real director owns loaded source fixture");
            Need(director.SavedPublicEventIds().Length == (route == "online" ? 7 : 5), "loaded director exposes only validated event receipts");
            Need(XgPublicDiscourseDesktop.Context("news").route == route, "real bridge projected loaded route");
            foreach (var row in XgPublicDiscourseDesktop.Catalog.All.Where(r => r.route == route))
            {
                foreach (var f in BrowserRecord(browser, row, english, click)) yield return f;
                var sources = director.PublicSourcesForRecord(row.id); Need(sources.Count == 1, "exactly one approved source per route row");
                var source = sources[0];
                Need(source.sourceKind == "authored_fiction" && director.FindPublicSource(source.sourceRef) != null, "saved source resolves through whitelist");
                string state = JsonUtility.ToJson(ctl.Sim.S);
                string label = english ? "Read the public source ›" : "查看公开原帖 ›";
                var button = Find(browser, b => Label(b) == label); var stale = Retain(button);
                foreach (var f in Tap(button, click)) yield return f;
                Need((string)Field(browser, "discourseSource") == source.sourceRef, "actual source button resolves selected local sourceRef");
                string body = XgPublicDiscourseDesktop.Escape(english ? source.bodyEn : source.bodyZh);
                var text = browser.GetComponentsInChildren<TMP_Text>().Single(t => t.gameObject.activeInHierarchy && t.text == body);
                text.ForceMeshUpdate();
                Need(!text.isTextOverflowing && text.preferredHeight <= text.rectTransform.rect.height + 2, "complete saved source body fits");
                Need(Texts(browser).Contains(english ? "In-game public record · Fictional people and events" : "游戏内公开记录 · 虚构人物与事件"), "source is clearly fictional");
                string afterSource=JsonUtility.ToJson(ctl.Sim.S);
                Need(afterSource == state, "opening original source does not train, reward or resave; "+Delta(state,afterSource));
                if (row.platform == "news") foreach (var f in Capture("discourse-source-" + route + "-" + (english ? "en" : "zh"), capture)) yield return f;
                foreach (var f in Tap(Find(browser, b => Label(b) == (english ? "Back to discussion" : "返回讨论")), click)) yield return f;
                Need((string)Field(browser, "discourseSource") == "" && (string)Field(browser, "discourseId") == row.id, "actual back preserves selected discussion");
                Need(director.FindPublicSource("https://example.invalid/source") == null && director.FindPublicSource(source.sourceRef + "/unknown") == null, "external and unapproved refs rejected");
                if (row.platform == "news")
                {
                    var oldToken = director.CaptureBinding(); Bind(ctl, House(6, english));
                    foreach (var f in Wait()) yield return f;
                    string oldSource = (string)Field(browser, "discourseSource");
                    stale();
                    Need(!ctl.Epilogue.IsCurrent(oldToken) && (string)Field(browser, "discourseSource") == oldSource
                        && ctl.Epilogue.FindPublicSource(source.sourceRef) == null, "retained source click cannot open prior owner's original after swap");
                    Bind(ctl, SourceHouse(route, english)); foreach (var f in Wait()) yield return f;
                    director = ctl.Epilogue;
                }
            }
        }
        foreach (var fixture in new[] { SourceHouse("online", false, "power_cut", false), SourceHouse("local_only", false, "completed", false), SourceHouse("public_model", false, "completed", false) })
        {
            language(false); Bind(ctl, fixture); foreach (var f in Wait()) yield return f;
            Need(ctl.Epilogue.SavedPublicEventIds().Length == 0, "cut-power/local/unpublished scene fixtures expose no source evidence");
            foreach (var f in BrowserArchive(browser, click)) yield return f;
            Need(!XgPublicDiscourseDesktop.Archive("news").Any(r => r.id == "R1-N" || r.id == "R2-N"), "artifact/route alone cannot reveal usage scenes");
            Need(!browser.GetComponentsInChildren<Button>().Any(b => Label(b) == "查看公开原帖 ›"), "no original-source button fabricated without valid source snapshot");
        }
        Done("Both routes × five original sources × both languages use actual LCD source/back buttons; old-save/unknown references and absent evidence rejected.");
    }
}
#endif
