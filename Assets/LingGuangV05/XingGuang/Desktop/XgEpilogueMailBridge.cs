using System;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Mail;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Joins the native mailbox to the existing atomic save owner. No inferred private disclosures.</summary>
    public sealed class XgEpilogueMailBridge : MonoBehaviour
    {
        XingGuangController owner;
        XgEpilogueDirector director;
        ChapterOneSim boundSave;
        XgSim boundLab;
        MailInboxView inbox;
        ChapterOneDesktopRouter router;
        Michsky.DreamOS.WindowManager observedMailWindow;
        bool readingWindowActive;
        EndingLetterCatalog catalog;
        int projectedRevision = -1;
        int publicLettersRevision = -1;
        bool restoredMail;
        bool preferencesApplied, lastReduced, lastMuted;
        string completionPending = "";
        XgEpilogueBinding completionBinding;
        double completionPresentationDeadline;
        float nextFind;

        public static XgEpilogueMailBridge Install(XingGuangController controller)
        {
            var b = controller.GetComponent<XgEpilogueMailBridge>() ?? controller.gameObject.AddComponent<XgEpilogueMailBridge>();
            b.owner = controller;
            b.Attach();
            return b;
        }
        void Attach()
        {
            if (owner == null || owner.Epilogue == null || director == owner.Epilogue) return;
            Detach(); director = owner.Epilogue;
            director.OpenInboxRequested += OpenCurrent;
            director.OpenReplayLetterRequested += OpenReplay;
            director.CompletionSaved += OnCompletionSaved;
        }
        void Detach()
        {
            if (director == null) return;
            director.OpenInboxRequested -= OpenCurrent;
            director.OpenReplayLetterRequested -= OpenReplay;
            director.CompletionSaved -= OnCompletionSaved;
        }
        bool Current => owner != null && director != null && owner.LifeEventSettingsAvailable
            && ReferenceEquals(owner.runtime?.Sim, boundSave) && ReferenceEquals(owner.Sim, boundLab)
            && owner.BoundTo(boundSave);
        bool IsCurrent(object identity, long revision)
        {
            var token = Current ? director.CaptureBinding() : null;
            return token != null && ReferenceEquals(identity, boundSave) && revision == token.Revision;
        }
        void ObserveSave()
        {
            if (ReferenceEquals(boundSave, owner.runtime?.Sim) && ReferenceEquals(boundLab, owner.Sim)) return;
            if (inbox?.EndingLetter != null) inbox.BindEndingLetter(null, null, false);
            inbox?.BindPublicLetters(null, null, null, null, null, null);
            publicLettersRevision = -1;
            boundSave = owner.runtime?.Sim; boundLab = owner.Sim;
            completionPending = ""; completionBinding = null; restoredMail = false; projectedRevision = -1; readingWindowActive = false;
        }
        bool ResolveInbox()
        {
            if (inbox != null && router != null) { ObserveNativeWindow(); return true; }
            if (Time.unscaledTime < nextFind) return false;
            nextFind = Time.unscaledTime + 1;
            router = owner.runtime.GetComponent<ChapterOneDesktopRouter>();
            var app = router?.Get("mail");
            if (app?.Content != null) inbox = app.Content.GetComponent<MailInboxView>();
            if (inbox == null) inbox = FindAnyObjectByType<MailInboxView>(FindObjectsInactive.Include);
            bool ready = inbox != null && router != null;
            if (ready) ObserveNativeWindow();
            return ready;
        }
        void ObserveNativeWindow()
        {
            var window = router?.Get("mail")?.Window;
            if (observedMailWindow == window) return;
            DetachNativeWindow(); observedMailWindow = window;
            if (observedMailWindow == null) return;
            observedMailWindow.onClose.AddListener(NativeMailboxDismissed);
            observedMailWindow.onMinimize.AddListener(NativeMailboxDismissed);
        }
        void DetachNativeWindow()
        {
            if (observedMailWindow != null)
            {
                observedMailWindow.onClose.RemoveListener(NativeMailboxDismissed);
                observedMailWindow.onMinimize.RemoveListener(NativeMailboxDismissed);
            }
            observedMailWindow = null;
        }
        void NativeMailboxDismissed()
        {
            if (!readingWindowActive) return;
            readingWindowActive = false;
            if (!Current) return;
            // Native close/minimize bypasses Session.Back. End only presentation/replay; no save or route action.
            // An unfinished epilogue still holds through its persisted phase, as before.
            director.Hide();
        }
        EndingLetterCatalog Catalog
        {
            get
            {
                if (catalog == null)
                {
                    var resource = Resources.Load<TextAsset>(EndingLetterCatalog.ResourcePath);
                    if (resource != null) catalog = new EndingLetterCatalog(JsonUtility.FromJson<EndingLetterResource>(resource.text));
                }
                return catalog;
            }
        }
        void Update()
        {
            if (owner == null) return;
            Attach(); ObserveSave();
            if (!Current) return;
            var ep = boundLab.S.epilogue; var release = boundLab.S.releaseOutcome;
            if (ep == null || !XgEpilogueFlow.HasCommittedRelease(boundLab.S)) return;
            if (projectedRevision != ep.revision)
            {
                // The date changes only after the real night has ended and the epilogue has been entered.
                int date = ep.phase == "launch_night" ? GameCalendar.Yyyymmdd(GameCalendar.Now(boundSave.S)) : ep.displayDate;
                XgPublicDiscourseDesktop.SetReleaseProjection(boundSave, ep.revision, date, release.route,
                    release.artifactId, director.SavedPublicEventIds());
                projectedRevision = ep.revision;
            }
            if (publicLettersRevision != ep.revision && XgEpilogueFlow.CanBrowsePublicLetters(boundLab.S) && ResolveInbox())
                BindPublicLetters();
            if (!restoredMail && ep.savedAt != 0 && ResolveInbox())
            { BindLetter(ep.letterId, false, ep.phase == "completed"); restoredMail = true; }
            if (inbox?.EndingLetter != null && (!preferencesApplied || lastReduced != boundLab.S.reduceFx || lastMuted != boundLab.S.mute))
            {
                inbox.EndingLetter.SetPreferences(boundLab.S.reduceFx, !boundLab.S.mute);
                preferencesApplied = true; lastReduced = boundLab.S.reduceFx; lastMuted = boundLab.S.mute;
            }
            // The presenter's completion response is accepted before the scene is allowed to change.
            if (completionPending.Length > 0)
            {
                // The completion event/receipt already exists. Delay only visible routing, bound to that save.
                if (completionBinding == null || !director.IsCurrent(completionBinding) || ep.completionReceipt != completionPending)
                { completionPending = ""; completionBinding = null; return; }
                if (director.CompletionPresentationPending && Time.realtimeSinceStartupAsDouble < completionPresentationDeadline) return;
                var receipt = completionPending;
                if (GameExit.ReturnToTitleAfterEpilogueSave(receipt)) { completionPending = ""; completionBinding = null; }
            }
        }
        void OpenCurrent(string articleId) => Open(articleId, false);
        void OpenReplay(string articleId) => Open(articleId, true);
        void Open(string articleId, bool readOnly)
        {
            ObserveSave();
            if (!Current || !ResolveInbox() || !BindLetter(articleId, true, readOnly)) return;
            inbox.PublicLetters?.Hide();
            router.Open("mail", "ending");
            director.SetReadingVisible(true);
            readingWindowActive = true;
        }
        bool BindLetter(string articleId, bool show, bool readOnly)
        {
            var ep = boundLab.S.epilogue;
            var article = Catalog?.Get(articleId);
            var token = director.CaptureBinding();
            if (ep == null || article == null || token == null || articleId != ep.letterId
                || !XgEpilogueFlow.CanEnterEpilogue(boundLab.S)) return false;
            if (readOnly && (ep.phase != "completed" || string.IsNullOrEmpty(ep.completionReceipt))) return false;
            if (!readOnly && ep.phase != "letter" && ep.phase != "reading") return false;
            inbox.BindEndingLetter(new EndingLetterInput
            {
                owner = boundSave, revision = token.Revision, articleId = articleId,
                paragraphId = ep.readParagraphId, saved = ep.savedAt != 0, opened = ep.opened,
                consentShown = ep.consentShown, readOnly = readOnly, completionEligible = !readOnly,
                english = GameText.IsEnglish, reduceFx = boundLab.S.reduceFx, soundEnabled = !boundLab.S.mute
            }, new EndingLetterCallbacks
            {
                IsCurrent = IsCurrent,
                RequestSaveArticle = (r, done) => Apply(r, done, "save"),
                RequestOpen = (r, done) => Apply(r, done, "open"),
                ReadingPositionChanged = (r, done) => Apply(r, done, "position"),
                RequestComplete = (r, done) => Apply(r, done, "complete"),
                RequestPublicArchive = r => { if (IsCurrent(r.owner, r.revision)) OpenPublicLetters(); },
                RequestBackToDesktop = r =>
                {
                    if (!IsCurrent(r.owner, r.revision)) return;
                    director.SetReadingVisible(false);
                    router.Get("mail")?.Window?.CloseWindow();
                    var parent = PrologueDirector.Desk?.layer;
                    if (readOnly && parent != null) director.ShowReplay(parent);
                }
            }, show);
            restoredMail = true;
            return true;
        }
        void BindPublicLetters()
        {
            if (!Current || inbox == null || Catalog == null || !XgEpilogueFlow.CanBrowsePublicLetters(boundLab.S)) return;
            var ep = boundLab.S.epilogue; var token = director.CaptureBinding(); var identity = boundSave;
            if (token == null) return;
            var saved = new System.Collections.Generic.List<string>();
            foreach (var article in Catalog.Recommended(boundLab.S.releaseOutcome.route))
                if (ep.HasPublicArticle(article.id, article.version) || (article.id == ep.letterId && ep.savedAt != 0
                    && ep.sourceArticleId == article.id && ep.letterVersion == article.version)) saved.Add(article.id);
            inbox.BindPublicLetters(new PublicLetterArchiveInput
            {
                route = boundLab.S.releaseOutcome.route, recommendedId = ep.letterId,
                savedArticleIds = saved.ToArray(), english = GameText.IsEnglish, completed = ep.phase == "completed"
            }, () => IsCurrent(identity, token.Revision), id => SavePublicLetter(identity, token, id),
            () =>
            {
                if (!IsCurrent(identity, token.Revision)) return;
                inbox.PublicLetters.Hide();
                Open(ep.letterId, ep.phase == "completed");
            }, () =>
            {
                if (!IsCurrent(identity, token.Revision)) return;
                // Refresh the recommendation row's token after a bookmark save. Do not open it automatically.
                BindLetter(boundLab.S.epilogue.letterId, false, boundLab.S.epilogue.phase == "completed");
            }, OpenPublicLetters);
            publicLettersRevision = ep.revision;
        }
        void OpenPublicLetters()
        {
            ObserveSave();
            if (!Current || !XgEpilogueFlow.CanBrowsePublicLetters(boundLab.S) || !ResolveInbox()) return;
            BindPublicLetters(); inbox.ShowPublicLetters();
        }
        void SavePublicLetter(object identity, XgEpilogueBinding token, string id)
        {
            if (!IsCurrent(identity, token.Revision)) return;
            var article = Catalog?.Get(id); if (article == null) return;
            var result = director.SavePublicArticle(token, id, article.version, article.sourceArticleId);
            if (!Current || !ReferenceEquals(identity, boundSave) || result.Code == "saved_detached") return;
            if (result.Success)
            {
                BindPublicLetters();
                // The archive remains visible, and the single ending row must not keep a stale revision.
                BindLetter(boundLab.S.epilogue.letterId, false, boundLab.S.epilogue.phase == "completed");
                inbox.PublicLetters.Show();
            }
            else inbox.PublicLetters.ReportSaveFailure();
        }
        void Apply(EndingLetterRequest request, Action<EndingLetterResult> done, string action)
        {
            XgEpilogueResult result = new XgEpilogueResult(false, "stale_binding");
            var article = Catalog?.Get(request.articleId);
            if (IsCurrent(request.owner, request.revision) && article != null
                && request.version == article.version && request.sourceArticleId == article.sourceArticleId)
            {
                var token = director.CaptureBinding();
                switch (action)
                {
                    case "save": result = director.SaveArticle(token, article.id, article.version, article.sourceArticleId); break;
                    case "open": result = director.OpenArticle(token, article.id); break;
                    case "position":
                        if (article.HasParagraph(request.paragraphId)) result = director.SaveReadingPosition(token, article.id, request.paragraphId);
                        break;
                    case "complete": result = director.Complete(token, article.id, article.version); break;
                }
            }
            var current = Current ? director.CaptureBinding() : null;
            done(new EndingLetterResult { owner = Current ? boundSave : null, revision = current?.Revision ?? -1,
                success = result.Success && result.Code != "saved_detached", code = result.Code });
        }
        void OnCompletionSaved(string receipt)
        {
            if (!Current || string.IsNullOrEmpty(receipt) || boundLab.S.epilogue?.completionReceipt != receipt) return;
            if (completionPending == receipt) return;
            completionPending = receipt; completionBinding = director.CaptureBinding();
            completionPresentationDeadline = Time.realtimeSinceStartupAsDouble + 4;
            try { director.ShowCompletionPresentation(completionBinding, receipt); }
            catch (Exception error) { Debug.LogWarning("Epilogue closing presentation unavailable: " + error.GetType().Name); }
        }
        void OnDestroy() { Detach(); DetachNativeWindow(); inbox?.EndingLetter?.Session.Hide(); }
    }
}
