#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.CaseMenu;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// No lifecycle owner here. Title uses queued mouse through the real EventSystem; Main uses the real LCD relay.
public static class EndingArchiveActualUiChecks
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    public static readonly List<string> Checks = new List<string>();
    public static int Assertions { get; private set; }
    public static void Reset() { Checks.Clear(); Assertions = 0; }
    static T Get<T>(object obj, string name) => (T)obj.GetType().GetField(name, F).GetValue(obj);
    static void Need(bool value, string message) { Assertions++; if (!value) throw new InvalidOperationException("Ending archive UI: " + message); }
    static void Done(string message) { Checks.Add(message); Debug.Log("Ending archive UI: " + message); }
    public static IEnumerator Frames(int count = 3) { while (count-- > 0) yield return null; }
    public static IEnumerator Until(Func<bool> ready, string label, double seconds = 20)
    {
        double end = EditorApplication.timeSinceStartup + seconds;
        while (!ready()) { Need(EditorApplication.timeSinceStartup < end, label); yield return null; }
        yield return Frames();
    }
    public static Button Named(Component root, string name, bool interactable = true)
    {
        var found = root.GetComponentsInChildren<Button>(true).Where(x => x.name == name && x.gameObject.activeInHierarchy).ToArray();
        Need(found.Length == 1, "one actual visible button " + name + ": " + found.Length);
        Need(!interactable || found[0].IsInteractable(), "actual button enabled: " + name); return found[0];
    }
    static string Payload(string directory)
    {
        var read = new AtomicSaveStore(directory).Load(); Need(read.Success && !read.Recovered, "primary snapshot readable without recovery: " + directory); return read.Payload;
    }
    public static GameState House(string directory) => JsonUtility.FromJson<GameState>(Payload(directory));
    public static XgState Lab(string directory) => JsonUtility.FromJson<XgState>(House(directory).labState);
    static string Text(Component component) => string.Join("\n", component.GetComponentsInChildren<TMP_Text>().Where(t => t.gameObject.activeInHierarchy).Select(t => t.text));
    public static IEnumerator Power(CaseMenuController menu, Action<Vector2, bool> pointer)
    {
        Need(menu != null && menu.IsMainReady && !menu.HasEnteredGame && !menu.IsTransitioning, "actual physical title ready");
        var collider = menu.startButton.GetComponentInChildren<Collider>(); Need(collider != null, "physical power collider exists");
        var point = menu.menuCamera.WorldToScreenPoint(collider.bounds.center);
        Need(point.z > 0 && menu.HitButton(point) == menu.startButton, "real title ray reaches power key");
        pointer(point, false); yield return Frames(); pointer(point, true); yield return Frames();
        Need(menu.startButton.IsPressed, "queued press depresses the real physical power key");
        pointer(point, false); yield return Frames();
    }
    public static IEnumerator TitleTap(RectTransform target, Action<Vector2, bool> pointer)
    {
        Canvas.ForceUpdateCanvases(); yield return Frames();
        Need(target != null && target.gameObject.activeInHierarchy, "title click target remains active");
        var point = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center));
        Need(point.x >= 0 && point.x < Screen.width && point.y >= 0 && point.y < Screen.height, "title click is inside the actual GameView");
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
        Need(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == target.gameObject,
            "title ray hits expected button: " + target.name + ", actual " + (hits.Count == 0 ? "none" : hits[0].gameObject.name));
        pointer(point, false); yield return Frames(); pointer(point, true); yield return Frames(); pointer(point, false); yield return Frames();
    }
    public static IEnumerator TitleTap(Component root, string name, Action<Vector2, bool> pointer)
        => TitleTap((RectTransform)Named(root, name).transform, pointer);

    // Ends on the actual archive home. No forced desktop entry, SaveNow, publication, mailbox or completion request.
    public static IEnumerator CompletedTitleFrames(CaseMenuController menu, string receipt, Action<Vector2, bool> pointer,
        Func<string, IEnumerator> capture, string suffix)
    {
        var hud = menu.GetComponent<CaseMenuHud>(); var rt = menu.GetComponent<CaseMenuMainDisplay>().Runtime;
        var save = rt.Sim; var ctl = rt.GetComponent<XingGuangController>(); string folder = Path.GetDirectoryName(rt.SavePath);
        Need(XgEndingArchiveRules.Completed(ctl.Sim.S) && ctl.Sim.S.epilogue.completionReceipt == receipt, "preloaded title owns exact real completion receipt");
        string disk = Payload(folder), house = JsonUtility.ToJson(save.S), lab = JsonUtility.ToJson(ctl.Sim.S);
        int saves = 0, completions = 0; Action saving = () => saves++; Action<string> completed = _ => completions++;
        rt.Saving += saving; ctl.Epilogue.CompletionSaved += completed;
        try
        {
            if (!hud.IsArchiveOpen) yield return Power(menu, pointer);
            yield return Until(() => hud.IsArchiveOpen, "Completed Continue opens actual ending archive");
            Need(!menu.HasEnteredGame && !menu.IsTransitioning && !rt.enabled, "completed Continue stays in paused physical title");
            Need(Named(hud, "ReplayEnding").IsInteractable(), "completed archive offers read-only replay");
            yield return capture("ending-archive-home-" + suffix + ".png");
            yield return TitleTap(hud, "ReplayEnding", pointer);
            var view = hud.GetComponentsInChildren<XgEpilogueView>().Single(x => x.Showing);
            for (int step = 0; step < 3; step++)
            {
                yield return Until(() => view != null && !view.TransitionPlaying, "title replay presentation settles naturally", 12);
                yield return TitleTap(Get<XgBtn>(view, "next").rt, pointer);
            }
            yield return TitleTap(Get<XgBtn>(view, "inbox").rt, pointer);
            Need(EndingLetterCatalog.ArticleIds.All(id => Named(hud, "PublicLetter_" + id).IsInteractable()), "replay reaches all three public articles");
            yield return TitleTap(hud, "BackToEndings", pointer);
            var asset = Resources.Load<TextAsset>(EndingLetterCatalog.ResourcePath);
            var catalog = new EndingLetterCatalog(JsonUtility.FromJson<EndingLetterResource>(asset.text));
            foreach (string id in EndingLetterCatalog.ArticleIds)
            {
                yield return TitleTap(hud, "EndingPublicLetters", pointer);
                yield return TitleTap(hud, "PublicLetter_" + id, pointer);
                var article = catalog.Get(id); string rendered = Text(hud);
                Need(rendered.Contains(article.Title(GameText.IsEnglish)) && article.paragraphs.All(p => rendered.Contains(p.Text(GameText.IsEnglish))), "all nine actual public paragraphs rendered: " + id);
                var scroll = hud.GetComponentsInChildren<ScrollRect>().Single(x => x.name == "ReadOnlyLetter");
                // Geometry fixture only: no saved read/completion receipt is changed by the real read-only ScrollRect.
                scroll.verticalNormalizedPosition = 0; yield return Frames();
                yield return capture("ending-archive-letter-" + id + "-" + suffix + ".png");
                yield return TitleTap(hud, "BackToEndings", pointer);
            }
            Need(saves == 0 && completions == 0 && ReferenceEquals(rt.Sim, save), "title replay/public reading make no save or completion calls and keep identity");
            Need(Payload(folder) == disk && JsonUtility.ToJson(save.S) == house && JsonUtility.ToJson(ctl.Sim.S) == lab,
                "title archive preserves complete primary payload, money/calendar, lab/letters/public receipts and snapshot state");
            Need(Lab(folder).epilogue.completionReceipt == receipt && !menu.HasEnteredGame, "original completion remains exact without reopening live Main");
            Done(suffix + ": actual physical power→completed archive→replay→three public letters/back; no SaveNow/completion event, no primary/house/lab mutations. Scroll position was an explicit geometry fixture, not an OS-wheel claim.");
        }
        finally { if (rt != null) rt.Saving -= saving; if (ctl != null && ctl.Epilogue != null) ctl.Epilogue.CompletionSaved -= completed; }
    }

    static IEnumerator LcdTap(Component root, string name, Action<RectTransform> click)
    { var b = Named(root, name); yield return Frames(4); foreach(var frame in LcdActualUiProbe.WaitForClick((RectTransform)b.transform, name)) yield return frame; click((RectTransform)b.transform); yield return Frames(4); }
    public static IEnumerator ChoiceTransactions(XingGuangController ctl, Action<RectTransform> click, Action<string> capture)
    {
        var rt = ctl.runtime; var host = ctl.GetComponent<XgEndingPreparationHost>();
        string directory = Path.GetDirectoryName(rt.SavePath), checkpointRoot = Path.Combine(directory, "endings", "checkpoints");
        Need(!rt.TestMode && rt.useDiskSave && !rt.tickEnabled && !ctl.enabled, "actual isolated disk owner with unrelated ticks paused");
        var ids = new HashSet<string>();
        foreach (int routeIndex in new[] { 0, 1, 3, 2 })
        {
            host.Hide(); Need(rt.LoadArchive(4), "reload actual copied pre-choice slot, not manufactured prerequisite receipts");
            Need(PrologueDirector.Desk?.layer!=null&&PrologueDirector.Desk.layer.gameObject.scene==rt.gameObject.scene,"real shared LCD desktop exists for the current archive owner");
            Need(host.Show(PrologueDirector.Desk.layer), "real preparation panel opens"); yield return Frames(5);
            Need(XgEndingArchiveRules.BeforeChoice(ctl.Sim.S), "source is genuinely before choice");
            var panel = Get<EndingPreparationPanel>(host, "preparation"); yield return LcdTap(panel, "Tab1", click);
            string before = Payload(directory), prep = JsonUtility.ToJson(ctl.Sim.S.endingPreparation), previousId = rt.Sim.S.endingCheckpointId;
            var identity = rt.Sim;
            for (int failAt = 1; failAt <= 2; failAt++)
            {
                int calls = 0, injected = failAt;
                Action fault = () => { if (++calls == injected) throw new IOException("Archive QA explicit Saving fault at " + injected); };
                rt.Saving += fault;
                try { yield return LcdTap(panel, "Route" + routeIndex, click); }
                finally { rt.Saving -= fault; }
                Need(calls == failAt && ReferenceEquals(rt.Sim, identity) && rt.Sim.S.endingCheckpointId == previousId,
                    "route " + routeIndex + " failure " + failAt + " preserves current identity and association");
                Need(Payload(directory) == before && JsonUtility.ToJson(ctl.Sim.S.endingPreparation) == prep,
                    "snapshot/choice-save failure keeps exact primary and before-choice state");
                Need(panel.Session.Failed && Named(panel, "Route" + routeIndex).IsInteractable(), "failed real route button remains retryable");
            }
            capture("ending-archive-choice-failure-" + routeIndex + ".png");
            yield return LcdTap(panel, "Route" + routeIndex, click);
            var chosen = ctl.Sim.S.endingPreparation; string id = rt.Sim.S.endingCheckpointId;
            Need(!string.IsNullOrEmpty(id) && ids.Add(id) && chosen.phase == "pinning", "real route retry stores a fresh checkpoint association");
            var check = House(Path.Combine(checkpointRoot, id)); var checkLab = JsonUtility.FromJson<XgState>(check.labState);
            Need(XgEndingArchiveRules.BeforeChoice(checkLab) && check.endingCheckpointId == id && JsonUtility.ToJson(checkLab.endingPreparation) == prep,
                "dedicated full snapshot captures the actual before-choice preparation, not a rewound selected route");
            Need(House(directory).endingCheckpointId == id && Lab(directory).endingPreparation.route == chosen.route, "checkpoint ID and actual route are in the same durable primary transaction");
            string expected = routeIndex == 0 ? "online" : routeIndex == 1 ? "public_model" : routeIndex == 2 ? "local_only" : chosen.route;
            Need(chosen.route == expected && chosen.decisionOwner == (routeIndex == 3 ? "ai" : "player") && !ctl.Sim.S.chapterComplete && !XgEpilogueFlow.HasCommittedRelease(ctl.Sim.S), "choice alone does not publish or complete");
            if (routeIndex == 3) Need(chosen.aiDecision != null && chosen.aiDecision.citedMemoryIds.Count == 2 && chosen.aiDecision.route == chosen.route, "actual deterministic Ask it result cites saved source memories");
            string immutable = Payload(Path.Combine(checkpointRoot, id)); Need(rt.SaveNow(), "ordinary main save after choice");
            Need(Payload(Path.Combine(checkpointRoot, id)) == immutable, "subsequent main save never overwrites immutable checkpoint");
            capture("ending-archive-choice-saved-" + routeIndex + ".png");
            Done("Choice " + routeIndex + ": actual LCD button with collection failure, post-checkpoint main-save failure, rollback and successful retry; verified immutable before-choice snapshot + durable chosen identity. No publication/night in this check.");
        }
    }

    // Local-only route finishes via real pin, release, article and completion actions; there is no launch night to skip.
    public static IEnumerator PublishChosenLocal(XingGuangController ctl, Action<RectTransform> click)
    {
        var host = ctl.GetComponent<XgEndingPreparationHost>(); var panel = Get<EndingPreparationPanel>(host, "preparation");
        Need(ctl.Sim.S.endingPreparation.route == "local_only" && ctl.Sim.S.endingPreparation.phase == "pinning", "actual local route from choice transaction matrix");
        yield return LcdTap(panel, "Tab2", click);
        foreach (string id in new[] { "scam_sms", "learned_wrong", "shutdown" })
        {
            int index = panel.Session.Snapshot.memories.FindIndex(x => x.id == id); Need(index >= 0 && index < 4, "actual source-backed card exists: " + id);
            yield return LcdTap(panel, "Pin" + index, click);
        }
        yield return LcdTap(panel, "Primary", click); yield return LcdTap(panel, "Primary", click);
        Need(!string.IsNullOrEmpty(ctl.Sim.S.endingPreparation.pinReceipt), "actual permanent-pin confirmation saved");
        yield return LcdTap(panel, "Tab3", click); yield return LcdTap(panel, "Primary", click); yield return LcdTap(panel, "Primary", click);
        Need(XgEpilogueFlow.CanEnterEpilogue(ctl.Sim.S) && ctl.Sim.S.releaseOutcome.route == "local_only" && string.IsNullOrEmpty(ctl.Sim.S.releaseOutcome.artifactId) && !ctl.Sim.S.chapterComplete,
            "actual local release can enter epilogue without publishing an artifact or inventing online-night receipts");
        Done("Local-only pin/release completed by actual LCD controls; continuation will earn a new real article/completion receipt without running or shortcutting an online night.");
    }
}
#endif
