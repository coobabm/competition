#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Mail;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class EndingIntegrationUiChecks
{
    const string Key = "SidebarLayoutChecks.EndingIntegration";
    public static bool Requested => SessionState.GetBool(Key, false);
    public static readonly List<string> Checks = new List<string>();
    public static void ClearRequest() => SessionState.EraseBool(Key);
    public static void Run()
    {
        SessionState.SetBool(Key, true);
        try { EndingLetterChecks.Run(); BookLibraryUiChecks.Run(); } catch { ClearRequest(); throw; }
    }
    static IEnumerable Wait(double seconds)
    { double until = EditorApplication.timeSinceStartup + seconds; while (EditorApplication.timeSinceStartup < until) yield return null; }
    static void Need(bool condition, string message) { if (!condition) throw new Exception("Ending UI integration: " + message); Checks.Add(message); }
    public static IEnumerator Frames(XingGuangController owner, Action<bool> language, Action<RectTransform> click, Action<string> capture)
    {
        Checks.Clear(); bool wasEnabled = owner.enabled; owner.enabled = false;
        try
        {
            var brain = owner.GetComponent<XgBrainWindow>();
            if (brain != null && brain.Visible) { click((RectTransform)brain.Window.Find("Title/Close")); yield return null; }
            var router = owner.runtime.GetComponent<ChapterOneDesktopRouter>();
            Need(router != null && router.Open("mail"), "native mailbox opens through actual app router");
            foreach (var f in Wait(.8)) yield return f;
            var app = router.Get("mail"); var mail = app.Content.GetComponent<MailInboxView>();
            Need(mail != null && mail.EnsureReady(), "native mailbox custom view installed");
            var root = app.Content.Find("Inbox") as RectTransform;
            Need(root != null, "native inbox content exists");
            var ui = EndingLetterUiChecks.Frames(root, owner.Font, app.Window, click,
                name => capture(name + ".png"));
            try { while (ui.MoveNext()) yield return ui.Current; }
            finally { (ui as IDisposable)?.Dispose(); }
            Checks.Add("Letter UI protocol: " + EndingLetterUiChecks.AssertionCount + " assertions; callbacks are isolated fixtures, not actual save receipts.");
            // Exercise the real bridge with explicit prerequisite fixture receipts. This is not a natural red-team completion.
            var sim = owner.Sim; var oldProvider = owner.EpiloguePrerequisiteProvider;
            owner.EpiloguePrerequisiteProvider = () => new XgReleasePrerequisites {
                memoryCardsReceiptId="qa:memory",redTeamOneReceiptId="qa:red1",redTeamTwoReceiptId="qa:red2",
                redTeamThreeReceiptId="qa:red3",fixedThreeSlotsReceiptId="qa:pins" };
            try
            {
                owner.runtime.useDiskSave = true;
                Need(owner.Epilogue.CommitVirtualRelease(owner.Epilogue.CaptureBinding(), "local_only", "player", "unpinned", "", "", "", "").Success,
                    "explicit-fixture release persists through real existing SaveNow");
                var state = sim.S.epilogue;
                Need(state != null && state.phase == "date" && !sim.S.chapterComplete, "release does not complete chapter");
                var flow = new XgEpilogueFlow(sim.S, owner.runtime.Sim, () => owner.BoundTo(owner.runtime.Sim), () => owner.SaveNow());
                Need(flow.Advance(flow.CaptureBinding()).Success && flow.Advance(flow.CaptureBinding()).Success && flow.Advance(flow.CaptureBinding()).Success,
                    "date/headline/route advance via real atomic saves");
                // Invoke the production director's explicit inbox request, not a mock presenter callback.
                var method = typeof(XgEpilogueDirector).GetMethod("RequestInbox", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                method.Invoke(owner.Epilogue, new object[] { owner.Epilogue.CaptureBinding() });
                foreach (var f in Wait(.3)) yield return f;
                var p = mail.EndingLetter;
                Need(p != null && ReferenceEquals(p.Snapshot.owner, owner.runtime.Sim), "mail bridge bound to exact current save identity");
                Action fault = () => { throw new System.IO.IOException("Ending QA injected snapshot failure"); };
                owner.runtime.Saving += fault;
                try { Need(p.Session.SaveArticle() && !p.Snapshot.saved && sim.S.epilogue.savedAt == 0, "real disk-save failure leaves mail unsaved and state rolled back"); }
                finally { owner.runtime.Saving -= fault; }
                Need(p.Session.SaveArticle() && p.Snapshot.saved && sim.S.epilogue.savedAt != 0, "real save retry persists article and read marker");
                Need(p.Session.OpenPrompt(), "actual bridge shows consent; view=" + p.Session.View + ", current=" + p.Session.Current + ", error=" + p.Session.ErrorCopyId);
                Need(p.Session.OpenAccepted(), "actual bridge accepts explicit consent; view=" + p.Session.View + ", error=" + p.Session.ErrorCopyId);
                Need(sim.S.epilogue.opened && !sim.S.chapterComplete,
                    "actual bridge opening persists consent without completing game; view=" + p.Session.View + ", error=" + p.Session.ErrorCopyId);
                foreach (var f in Wait(.3)) yield return f;
                capture("ending-bridge-real-save-zh.png");
                language(true); foreach (var f in Wait(.2)) yield return f;
                capture("ending-bridge-real-save-en.png");
                p.Session.Hide();
            }
            finally { owner.EpiloguePrerequisiteProvider = oldProvider; }
        }
        finally { owner.enabled = wasEnabled; }
    }
}
#endif
