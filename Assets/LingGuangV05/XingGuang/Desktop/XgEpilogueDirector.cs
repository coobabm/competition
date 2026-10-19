using System;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Bodu;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Explicitly installed by the controller. Never discovers prerequisites from legacy ending flags.</summary>
    [DisallowMultipleComponent]
    public sealed class XgEpilogueDirector : MonoBehaviour
    {
        XingGuangController owner;
        XgSim boundLab;
        ChapterOneSim boundSave;
        XgEpilogueFlow flow;
        XgEpilogueView view;
        Func<XgReleasePrerequisites> prerequisites;
        bool readingVisible, replaying, desktopDateRevealed;
        ReleaseOutcome replayRelease;
        EpilogueState replayEpilogue;
        public event Action<string> OpenInboxRequested;
        public event Action<string> OpenReplayLetterRequested;
        public event Action<string> CompletionSaved;
        public bool Showing => view != null && view.Showing;
        public bool HoldingSimulation => Current && (replaying || (view != null && view.CompletionVisualActive) || (XgEpilogueFlow.HasCommittedRelease(boundLab.S)
            && boundLab.S.epilogue != null && boundLab.S.epilogue.phase != "completed"));
        // The real launch-night owner uses its own clock while normal economic/model ticks remain held.
        public bool LaunchNightPending => Current && XgEpilogueFlow.HasCommittedRelease(boundLab.S)
            && boundLab.S.releaseOutcome.route == "online" && boundLab.S.epilogue != null
            && boundLab.S.epilogue.phase == "launch_night";
        bool Current => owner != null && owner.runtime != null && owner.LifeEventSettingsAvailable
            && ReferenceEquals(owner.runtime.Sim, boundSave) && ReferenceEquals(owner.Sim, boundLab) && owner.BoundTo(boundSave);
        public static bool IsHolding(XingGuangController controller)
        { var d = controller == null ? null : controller.GetComponent<XgEpilogueDirector>(); return d != null && d.HoldingSimulation; }

        public void Bind(XingGuangController controller, Func<XgReleasePrerequisites> prerequisiteProvider = null)
        {
            Hide(); desktopDateRevealed = false; owner = controller; prerequisites = prerequisiteProvider;
            boundSave = owner == null || owner.runtime == null ? null : owner.runtime.Sim;
            boundLab = owner == null ? null : owner.Sim;
            flow = boundSave == null || boundLab == null ? null : new XgEpilogueFlow(boundLab.S, boundSave, () => Current, Persist);
        }
        bool Persist()
        {
            if (!Current || !owner.runtime.useDiskSave || owner.runtime.TestMode || owner.runtime.Sim.InPrologue) return false;
            // Saving event serializes the candidate into labState. Restore that cached JSON too on failure.
            string previousLabJson = boundSave.S.labState;
            owner.runtime.MarkDirty();
            bool success = false;
            try { success = owner.SaveNow(); return success; }
            finally { if (!success) boundSave.S.labState = previousLabJson; }
        }
        /// <summary>Presentation only: never advances GameCalendar, saves, or supplies historical record timestamps.</summary>
        public DateTime DesktopDisplayTime(ChapterOneSim save, DateTime economicNow)
        {
            if (!Current || !ReferenceEquals(save, boundSave) || replaying || !enabled
                || !XgEpilogueFlow.CanEnterEpilogue(boundLab.S)) return economicNow;
            var ep = boundLab.S.epilogue;
            if (ep == null || ep.schemaVersion != 1 || ep.historicalReview
                || ep.displayDate != XgEpilogueFlow.DisplayDate) return economicNow;
            var morning = new DateTime(2017, 6, 13, 8, 0, 0);
            if (economicNow.Date > morning.Date) return economicNow; // Never turn a late legacy save backward.
            // F3/F4 retain the current clock until the calendar reveal finishes (including Skip/reduced motion).
            ObserveDesktopDateReveal();
            bool revealed = ep.phase == "date" && desktopDateRevealed;
            if (!revealed && ep.phase != "headline" && ep.phase != "route" && ep.phase != "letter"
                && ep.phase != "reading") return economicNow;
            return morning;
        }

        void ObserveDesktopDateReveal()
        {
            if (Current && !replaying && boundLab.S.epilogue?.phase == "date" && view != null
                && view.isActiveAndEnabled && !view.TransitionPlaying) desktopDateRevealed = true;
        }

        public XgEpilogueBinding CaptureBinding() => Current && flow != null ? flow.CaptureBinding() : null;
        public bool IsCurrent(XgEpilogueBinding token) => Current && flow != null && flow.IsCurrent(token);
        static XgEpilogueResult Fail(string code) => new XgEpilogueResult(false, code);

        /// <summary>
        /// Called only by the explicit preview confirmation. This local, fictional publication writes the preview
        /// page and publication receipt together. It does not upload files or fabricate usage/derivative events.
        /// </summary>
        public XgEpilogueResult CommitVirtualRelease(XgEpilogueBinding token, string route, string decisionOwner,
            string shutdownPin, string publicName, string description, string limitations, string terms, string capabilities = "")
        {
            if (!IsCurrent(token)) return Fail("stale_binding");
            var proof = prerequisites == null ? null : prerequisites();
            if (proof == null || !proof.Complete) return Fail("missing_prerequisite_receipts");
            bool local = route == "local_only";
            var release = new ReleaseOutcome
            {
                route = route, decisionOwner = decisionOwner, shutdownPin = shutdownPin, prerequisites = proof.Copy(),
                committedAt = GameCalendar.Yyyymmdd(GameCalendar.Now(boundSave.S)),
                releaseReceiptId = "virtual-release:" + Guid.NewGuid().ToString("N"),
                artifactId = local ? "" : "virtual-artifact:" + Guid.NewGuid().ToString("N"),
                publicName = local ? "" : publicName, publicDescription = local ? "" : description,
                publicLimitations = local ? "" : limitations, publicTerms = local ? "" : terms,
                publicCapabilities = local ? "" : capabilities
            };
            return flow.CommitRelease(token, release);
        }
        public XgEpilogueResult RecordPublicEvent(XgEpilogueBinding token, XgPublicEventReceipt receipt)
            => flow == null ? Fail("not_bound") : flow.RecordPublicEvent(token, receipt);
        public XgEpilogueResult RecordLaunchNightResult(XgEpilogueBinding token, XgLaunchNightReceipt receipt)
            => flow == null ? Fail("not_bound") : flow.RecordLaunchNightResult(token, receipt);
        public XgEpilogueResult EnterEpilogue(XgEpilogueBinding token)
            => flow == null ? Fail("not_bound") : flow.EnterEpilogue(token);
        public XgEpilogueResult SaveArticle(XgEpilogueBinding token, string id, int version, string sourceArticleId)
            => flow == null ? Fail("not_bound") : flow.SaveArticle(token, id, version, sourceArticleId);
        public XgEpilogueResult SavePublicArticle(XgEpilogueBinding token, string id, int version, string sourceArticleId)
            => flow == null ? Fail("not_bound") : flow.SavePublicArticle(token, id, version, sourceArticleId);
        /// <summary>Only invoke after the player accepts the article-opening notice.</summary>
        public XgEpilogueResult OpenArticle(XgEpilogueBinding token, string id)
            => flow == null ? Fail("not_bound") : flow.OpenArticle(token, id);
        public XgEpilogueResult SaveReadingPosition(XgEpilogueBinding token, string id, string paragraphId)
            => flow == null ? Fail("not_bound") : flow.SaveReadingPosition(token, id, paragraphId);
        public XgEpilogueResult Complete(XgEpilogueBinding token, string id, int version)
        {
            if (flow == null) return Fail("not_bound");
            var result = flow.Complete(token, id, version);
            if (result.Success && result.Changed && result.Code == "saved" && Current)
            {
                string receipt = boundLab.S.epilogue.completionReceipt;
                Hide(); CompletionSaved?.Invoke(receipt);
            }
            return result;
        }
        public void SetReadingVisible(bool visible)
        {
            readingVisible = visible && Current;
            if (readingVisible && view != null) view.Hide(); // Let the native mailbox receive actual pointer input.
        }
        public string[] SavedPublicEventIds() => flow == null ? Array.Empty<string>() : flow.SavedPublicEventIds(CaptureBinding());
        public ReleaseOutcome PublicModelRelease() => Current && XgEpilogueFlow.HasCommittedRelease(boundLab.S)
            && boundLab.S.releaseOutcome.route == "public_model" ? boundLab.S.releaseOutcome.Copy() : null;
        public List<XgAuthoredPublicSource> PublicSourcesForRecord(string recordId)
            => flow == null ? new List<XgAuthoredPublicSource>() : flow.PublicSourcesForRecord(CaptureBinding(), recordId);
        public XgAuthoredPublicSource FindPublicSource(string sourceRef)
            => flow == null ? null : flow.FindPublicSource(CaptureBinding(), sourceRef);
        bool EnsurePublicScenes()
        {
            if (!Current || boundLab.S.epilogue == null) return false;
            var ep = boundLab.S.epilogue; var release = boundLab.S.releaseOutcome;
            if (ep.phase != "route" && ep.phase != "letter" && ep.phase != "reading") return true;
            if (release.route == "local_only" || release.publicEventScriptVersion != 0 || ep.historicalReview
                || (release.route == "online" && release.launchNight?.ending == "power_cut")) return true;
            return flow.PublishAuthoredPublicEvents(CaptureBinding(), XgPublicDiscourseDesktop.Catalog).Success;
        }
        void BrowsePublic(XgEpilogueBinding token)
        {
            if (!IsCurrent(token)) return;
            var browser = FindAnyObjectByType<BoduView>(FindObjectsInactive.Include);
            if (browser == null) return;
            if (view != null) view.Hide();
            browser.OpenPublicDiscourse("news", boundLab.S.epilogue.phase == "headline" ? "H-01" : "");
        }
        public bool Show(RectTransform parent)
        {
            if (!Current || !XgEpilogueFlow.CanEnterEpilogue(boundLab.S) || boundLab.S.epilogue == null
                || boundLab.S.epilogue.phase == "launch_night" || boundLab.S.epilogue.phase == "completed" || parent == null) return false;
            if (view == null) view = XgEpilogueView.Create(parent, owner.Font);
            Refresh(); return true;
        }
        /// <summary>Copies presentation state only. Replay never invokes SaveNow or publishes a second artifact.</summary>
        public bool ShowReplay(RectTransform parent)
        {
            if (!Current || parent == null || boundLab.S.epilogue == null || boundLab.S.epilogue.phase != "completed"
                || string.IsNullOrEmpty(boundLab.S.epilogue.completionReceipt) || !XgEpilogueFlow.CanEnterEpilogue(boundLab.S)) return false;
            if (view == null) view = XgEpilogueView.Create(parent, owner.Font);
            replayRelease = boundLab.S.releaseOutcome.Copy(); replayEpilogue = boundLab.S.epilogue.Copy();
            replayEpilogue.phase = "date"; replaying = true; RefreshReplay(); return true;
        }
        void RefreshReplay()
        {
            if (!Current || !replaying) { Hide(); return; }
            view.Show(replayRelease, replayEpilogue, GameText.IsEnglish, () =>
            {
                if (!Current || !replaying) return;
                switch (replayEpilogue.phase) { case "date": replayEpilogue.phase = "headline"; break; case "headline": replayEpilogue.phase = "route"; break; case "route": replayEpilogue.phase = "letter"; break; }
                RefreshReplay();
            }, () => { if (Current && replaying) OpenReplayLetterRequested?.Invoke(replayEpilogue.letterId); }, Hide, true, null, boundLab.S.reduceFx, PublicCards());
        }
        void Refresh()
        {
            if (!Current || view == null || !XgEpilogueFlow.CanEnterEpilogue(boundLab.S) || LaunchNightPending) { Hide(); return; }
            bool publicReady = EnsurePublicScenes();
            var token = CaptureBinding();
            view.Show(boundLab.S.releaseOutcome, boundLab.S.epilogue, GameText.IsEnglish,
                () => Advance(token), () => RequestInbox(token), Hide, false, () => BrowsePublic(token), boundLab.S.reduceFx, PublicCards());
            if (!publicReady) view.SetError(GameText.T("没有保存成功，请重试。", "Saving failed. Please retry."));
        }
        void Advance(XgEpilogueBinding token)
        {
            if (!IsCurrent(token)) { Hide(); return; }
            if (!EnsurePublicScenes()) { view.SetError(GameText.T("没有保存成功，请重试。", "Saving failed. Please retry.")); return; }
            var result = flow.Advance(CaptureBinding());
            if (result.Success) Refresh(); else view.SetError(GameText.IsEnglish ? "Saving failed. Please retry." : "没有保存成功，请重试。");
        }
        void RequestInbox(XgEpilogueBinding token)
        {
            if (!IsCurrent(token)) { Hide(); return; }
            // The mail adapter owns catalog version/source lookup and the explicit save operation.
            OpenInboxRequested?.Invoke(boundLab.S.epilogue.letterId);
        }
        XgEpilogueCard[] PublicCards()
        {
            var result = new List<XgEpilogueCard>();
            if (!Current || boundLab.S.epilogue == null || XgPublicDiscourseDesktop.Catalog == null) return result.ToArray();
            var release = boundLab.S.releaseOutcome; if (release == null) return result.ToArray();
            var context = new XgPublicDiscourseContext(boundLab.S.stage, boundLab.S.epilogue.displayDate, "news", new[] { "news" }, release.route, release.artifactId, SavedPublicEventIds());
            // These are public excerpts inside the epilogue, never native-platform delivery/read receipts.
            foreach (string platform in new[] { "news", "forum", "yy" })
                foreach (var row in XgPublicDiscourseDesktop.Catalog.BrowserArchive(context, platform))
                    if (row.route == release.route && row.route.Length > 0) { result.Add(PublicCard(row)); break; }
            if (result.Count == 0)
                foreach (var row in XgPublicDiscourseDesktop.Catalog.BrowserArchive(context, "news"))
                    if (row.id.StartsWith("H-", StringComparison.Ordinal) && result.Count < 3) result.Add(PublicCard(row));
            return result.ToArray();
        }
        static XgEpilogueCard PublicCard(XgPublicDiscourseRecord row)
        {
            bool en = GameText.IsEnglish; string body = row.Body(en).Trim();
            int newline = body.IndexOf('\n'); if (newline > 0) body = body.Substring(0, newline);
            int limit = en ? 140 : 65;
            if (body.Length > limit)
            { int cut = en ? body.LastIndexOf(' ', limit) : limit; if (cut < 1) cut = limit; body = body.Substring(0, cut) + "…"; }
            return new XgEpilogueCard { title = row.Title(en), excerpt = body, caption = XgPublicDiscourseDesktop.PlatformName(row.platform) + " · "
                + (row.kind == "historical" ? GameText.T("历史资料摘录", "Historical excerpt") : GameText.T("游戏内讨论摘录", "In-game discussion excerpt")) };
        }
        public bool CompletionPresentationPending => Current && view != null && view.CompletionVisualActive;
        public bool ShowCompletionPresentation(XgEpilogueBinding token, string receipt)
        {
            if (!IsCurrent(token) || boundLab.S.epilogue?.phase != "completed" || !boundLab.S.chapterComplete
                || string.IsNullOrEmpty(receipt) || boundLab.S.epilogue.completionReceipt != receipt) return false;
            var parent = PrologueDirector.Desk?.layer; if (parent == null) return false;
            if (view == null) view = XgEpilogueView.Create(parent, owner.Font);
            bool en = GameText.IsEnglish; string player = "", ai = "", rule = "";
            var preparation = boundLab.S.endingPreparation; var release = boundLab.S.releaseOutcome;
            // Only show actual saved lines linked to the final-page source, never legacy ending guesses.
            if (preparation != null && !string.IsNullOrEmpty(preparation.finalPageReceipt) && preparation.memories != null
                && preparation.memories.Exists(m => m != null && m.id == "trial_line" && m.sources != null && m.sources.Exists(s => s != null && s.refId == preparation.finalPageReceipt)))
            { player = preparation.playerLastLine; ai = en ? preparation.aiLastLineEn : preparation.aiLastLine; }
            if (preparation != null && !string.IsNullOrEmpty(preparation.pinReceipt) && release?.prerequisites?.fixedThreeSlotsReceiptId == preparation.pinReceipt
                && preparation.pinnedIds != null && preparation.pinnedIds.Contains("shutdown") && (release.shutdownPin == "stop" || release.shutdownPin == "no_stop"))
            {
                var resource = Resources.Load<TextAsset>("LingGuangV05/Data/memory_cards");
                var copy = resource == null ? null : JsonUtility.FromJson<XgEndingPreparationText>(resource.text);
                if (copy != null) rule = release.shutdownPin == "stop" ? (en ? copy.shutdownStopEn : copy.shutdownStopZh) : (en ? copy.shutdownNoStopEn : copy.shutdownNoStopZh);
            }
            view.ShowCompletion(player, ai, rule, en, boundLab.S.reduceFx); return true;
        }
        public void Hide() { ObserveDesktopDateReveal(); readingVisible = false; replaying = false; replayRelease = null; replayEpilogue = null; if (view != null) view.Hide(); }
        void Update()
        {
            if ((Showing || readingVisible) && !Current) { Hide(); return; }
            if (Current && view != null) view.SetReducedMotion(boundLab.S.reduceFx);
        }
        void OnDisable() { Hide(); }
        void OnDestroy() { if (view != null) Destroy(view.gameObject); }
    }
}
