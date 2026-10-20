using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable]
    public sealed class XgReleasePrerequisites
    {
        // Written only by the corresponding completed gameplay systems. Empty is never a pass.
        public string memoryCardsReceiptId = "", redTeamOneReceiptId = "", redTeamTwoReceiptId = "",
            redTeamThreeReceiptId = "", fixedThreeSlotsReceiptId = "";
        public string aiRouteDecisionReceiptId = "", aiChosenRoute = "";
        public bool Complete => Has(memoryCardsReceiptId) && Has(redTeamOneReceiptId) && Has(redTeamTwoReceiptId)
            && Has(redTeamThreeReceiptId) && Has(fixedThreeSlotsReceiptId);
        static bool Has(string value) => !string.IsNullOrWhiteSpace(value);
        public XgReleasePrerequisites Copy() => (XgReleasePrerequisites)MemberwiseClone();
    }

    [Serializable]
    public sealed partial class ReleaseOutcome
    {
        public string route = "", decisionOwner = "", shutdownPin = "unpinned";
        public string artifactId = "", releaseReceiptId = "", sourceLegacyEnding = "";
        public int committedAt;
        public XgReleasePrerequisites prerequisites;
        public XgLaunchNightReceipt launchNight;
        // These fields describe only the public virtual page. Never serialize chat or memory cards into them.
        public string publicName = "", publicDescription = "", publicLimitations = "", publicTerms = "";
        public List<string> publicEventIds = new List<string>();
        public List<XgPublicEventReceipt> publicEvents = new List<XgPublicEventReceipt>();
        internal void RepairOptionalPlaceholders()
        {
            var n=launchNight;
            if(n!=null&&string.IsNullOrEmpty(n.receiptId)&&string.IsNullOrEmpty(n.sourceRef)&&
                string.IsNullOrEmpty(n.releaseReceiptId)&&string.IsNullOrEmpty(n.artifactId)&&
                string.IsNullOrEmpty(n.ending)&&n.endedAt==0)launchNight=null;
        }
        public ReleaseOutcome Copy()
        {
            var c = (ReleaseOutcome)MemberwiseClone();
            c.prerequisites = prerequisites == null ? null : prerequisites.Copy();
            c.launchNight = launchNight == null ? null : launchNight.Copy();
            c.publicEventIds = new List<string>(publicEventIds ?? new List<string>());
            c.publicEvents = new List<XgPublicEventReceipt>();
            if (publicEvents != null) foreach (var r in publicEvents) if (r != null) c.publicEvents.Add(r.Copy());
            c.CopyAuthoredPublicSources(this);
            c.RepairOptionalPlaceholders();
            return c;
        }
    }

    /// <summary>Written by the actual online launch-night system after it finishes or the player cuts power.</summary>
    [Serializable]
    public sealed class XgLaunchNightReceipt
    {
        public string receiptId = "", sourceRef = "", releaseReceiptId = "", artifactId = "";
        public string ending = ""; // completed / power_cut; neither means the chapter itself is complete.
        public int endedAt;
        public XgLaunchNightReceipt Copy() => (XgLaunchNightReceipt)MemberwiseClone();
    }

    [Serializable]
    public sealed class XgPublicEventReceipt
    {
        public string id = "", sourceRef = "", artifactId = "";
        public int occurredAt;
        public XgPublicEventReceipt Copy() => (XgPublicEventReceipt)MemberwiseClone();
    }

    [Serializable]
    public sealed partial class EpilogueState
    {
        public int schemaVersion = 1, revision, displayDate = XgEpilogueFlow.DisplayDate;
        public string phase = "", letterId = "", deliverySource = "", sourceArticleId = "";
        public int letterVersion, savedAt, completedAt;
        public bool consentShown, opened, historicalReview;
        public string readParagraphId = "", completionReceipt = "";
        public List<string> seenContentIds = new List<string>();
        public EpilogueState Copy()
        {
            var c = (EpilogueState)MemberwiseClone();
            c.seenContentIds = new List<string>(seenContentIds ?? new List<string>());
            c.CopyPublicArticleArchives(this);
            return c;
        }
    }

    public sealed partial class XgState
    {
        public ReleaseOutcome releaseOutcome;
        public EpilogueState epilogue;
    }

    /// <summary>A window's actual save instance, lab instance and persisted revision. Not a save path.</summary>
    public sealed class XgEpilogueBinding
    {
        internal readonly object saveIdentity;
        internal readonly XgState state;
        public readonly int Revision;
        internal XgEpilogueBinding(object saveIdentity, XgState state, int revision)
        { this.saveIdentity = saveIdentity; this.state = state; Revision = revision; }
    }

    public sealed class XgEpilogueResult
    {
        public readonly bool Success, Changed;
        public readonly string Code;
        public XgEpilogueResult(bool success, string code, bool changed = false)
        { Success = success; Code = code; Changed = changed; }
    }

    /// <summary>
    /// No clock, file IO, model calls or character knowledge. The owner supplies the existing atomic SaveNow.
    /// Candidates replace only this feature's state; a failed save restores the exact prior references and flag.
    /// </summary>
    public sealed partial class XgEpilogueFlow
    {
        public const int DisplayDate = 20170613, PaperPublishedDate = 20170612;
        readonly XgState state;
        readonly object saveIdentity;
        readonly Func<bool> stillCurrent, saveNow;
        bool saving;
        public XgEpilogueFlow(XgState state, object saveIdentity, Func<bool> stillCurrent, Func<bool> saveNow)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.saveIdentity = saveIdentity ?? throw new ArgumentNullException(nameof(saveIdentity));
            this.stillCurrent = stillCurrent ?? throw new ArgumentNullException(nameof(stillCurrent));
            this.saveNow = saveNow ?? throw new ArgumentNullException(nameof(saveNow));
        }
        public XgEpilogueBinding CaptureBinding() => new XgEpilogueBinding(saveIdentity, state, state.epilogue == null ? 0 : state.epilogue.revision);
        public bool IsCurrent(XgEpilogueBinding token) => token != null && ReferenceEquals(token.saveIdentity, saveIdentity)
            && ReferenceEquals(token.state, state) && (state.epilogue == null || state.epilogue.schemaVersion == 1)
            && token.Revision == (state.epilogue == null ? 0 : state.epilogue.revision) && stillCurrent();
        public static bool IsRoute(string route) => route == "online" || route == "public_model" || route == "local_only";
        public static string RecommendedLetter(string route) => route == "online" ? "zhou_open_door" : route == "public_model" ? "zhou_copies" : route == "local_only" ? "zhou_unpressed_button" : "";
        public static bool HasCommittedRelease(XgState s)
        {
            var r = s == null ? null : s.releaseOutcome;
            return r != null && IsRoute(r.route) && !string.IsNullOrEmpty(r.releaseReceiptId)
                && r.prerequisites != null && r.prerequisites.Complete
                && (r.route == "local_only" ? string.IsNullOrEmpty(r.artifactId) : !string.IsNullOrEmpty(r.artifactId));
        }
        /// <summary>Public/local releases need no launch night. An online release needs its own saved terminal receipt.</summary>
        public static bool CanEnterEpilogue(XgState s) => HasCommittedRelease(s)
            && (s.releaseOutcome.route != "online" || ValidLaunchNight(s.releaseOutcome, s.releaseOutcome.launchNight));
        static bool ValidLaunchNight(ReleaseOutcome release, XgLaunchNightReceipt night)
            => release != null && release.route == "online" && night != null
                && !string.IsNullOrWhiteSpace(night.receiptId) && !string.IsNullOrWhiteSpace(night.sourceRef)
                && night.releaseReceiptId == release.releaseReceiptId && night.artifactId == release.artifactId
                && (night.ending == "completed" || night.ending == "power_cut") && night.endedAt >= release.committedAt;
        static XgEpilogueResult Fail(string code) => new XgEpilogueResult(false, code);
        static XgEpilogueResult Unchanged() => new XgEpilogueResult(true, "unchanged");

        public XgEpilogueResult CommitRelease(XgEpilogueBinding token, ReleaseOutcome released)
        {
            if (!IsCurrent(token)) return Fail("stale_binding");
            if (released == null || !IsRoute(released.route)) return Fail("invalid_route");
            if (released.decisionOwner != "player" && released.decisionOwner != "ai") return Fail("invalid_decision_owner");
            if (released.shutdownPin != "unpinned" && released.shutdownPin != "stop" && released.shutdownPin != "no_stop") return Fail("invalid_shutdown_pin");
            if (released.prerequisites == null || !released.prerequisites.Complete) return Fail("missing_prerequisite_receipts");
            if (released.decisionOwner == "ai" && (string.IsNullOrWhiteSpace(released.prerequisites.aiRouteDecisionReceiptId)
                || released.prerequisites.aiChosenRoute != released.route)) return Fail("missing_ai_route_decision");
            if (string.IsNullOrWhiteSpace(released.releaseReceiptId) || released.committedAt < 20160101) return Fail("missing_release_receipt");
            if (released.route == "local_only" ? !string.IsNullOrEmpty(released.artifactId) : string.IsNullOrWhiteSpace(released.artifactId)) return Fail("invalid_artifact");
            if (released.route != "local_only" && (string.IsNullOrWhiteSpace(released.publicName) || string.IsNullOrWhiteSpace(released.publicDescription)
                || string.IsNullOrWhiteSpace(released.publicLimitations) || string.IsNullOrWhiteSpace(released.publicTerms))) return Fail("missing_public_preview");
            if (state.releaseOutcome != null && !string.IsNullOrEmpty(state.releaseOutcome.route))
                return state.releaseOutcome.releaseReceiptId == released.releaseReceiptId && state.releaseOutcome.route == released.route ? Unchanged() : Fail("route_locked");
            // Public experience events have their own operation; a release cannot smuggle in claims of use or copies.
            var candidate = released.Copy();
            candidate.publicEventIds.Clear(); candidate.publicEvents.Clear();
            candidate.ClearAuthoredPublicSources();
            // Launch night happens after online publication; an input receipt cannot skip that operation.
            candidate.launchNight = null;
            candidate.sourceLegacyEnding = state.ending ?? "";
            XgVirtualReleaseManifest.Populate(candidate);
            var ep = new EpilogueState { phase = candidate.route == "online" ? "launch_night" : "date",
                letterId = RecommendedLetter(candidate.route), historicalReview = candidate.committedAt > DisplayDate };
            return Save(token, candidate, ep, state.chapterComplete);
        }

        public XgEpilogueResult RecordLaunchNightResult(XgEpilogueBinding token, XgLaunchNightReceipt receipt)
        {
            if (!IsCurrent(token)) return Fail("stale_binding");
            if (!HasCommittedRelease(state) || state.epilogue == null) return Fail("release_not_committed");
            if (state.releaseOutcome.route != "online") return Fail("launch_night_not_required");
            if (!ValidLaunchNight(state.releaseOutcome, receipt)) return Fail("invalid_launch_night_receipt");
            var before = state.releaseOutcome.launchNight;
            if (before != null)
                return before.receiptId == receipt.receiptId && before.sourceRef == receipt.sourceRef
                    && before.ending == receipt.ending && before.endedAt == receipt.endedAt
                    ? Unchanged() : Fail("launch_night_locked");
            if (state.epilogue.phase != "launch_night") return Fail("launch_night_not_active");
            var release = state.releaseOutcome.Copy(); release.launchNight = receipt.Copy();
            return Save(token, release, state.epilogue.Copy(), state.chapterComplete);
        }

        /// <summary>Separate from publication and night receipt persistence so a reload never starts the night twice.</summary>
        public XgEpilogueResult EnterEpilogue(XgEpilogueBinding token)
        {
            if (!IsCurrent(token)) return Fail("stale_binding");
            if (!HasCommittedRelease(state) || state.epilogue == null) return Fail("release_not_committed");
            if (!CanEnterEpilogue(state)) return Fail("launch_night_incomplete");
            if (state.epilogue.phase != "launch_night")
                return state.epilogue.phase == "date" || state.epilogue.phase == "headline" || state.epilogue.phase == "route"
                    || state.epilogue.phase == "letter" || state.epilogue.phase == "reading" || state.epilogue.phase == "completed"
                    ? Unchanged() : Fail("invalid_epilogue_phase");
            var ep = state.epilogue.Copy(); ep.phase = "date";
            return Save(token, state.releaseOutcome.Copy(), ep, state.chapterComplete);
        }

        public XgEpilogueResult Advance(XgEpilogueBinding token)
        {
            if (!IsCurrent(token)) return Fail("stale_binding");
            if (!HasCommittedRelease(state) || state.epilogue == null) return Fail("release_not_committed");
            if (!CanEnterEpilogue(state)) return Fail("launch_night_incomplete");
            var ep = state.epilogue.Copy();
            switch (ep.phase)
            {
                case "date": ep.phase = "headline"; break;
                case "headline": ep.phase = "route"; break;
                case "route": ep.phase = "letter"; break;
                default: return Fail("phase_not_advanceable");
            }
            return Save(token, state.releaseOutcome.Copy(), ep, state.chapterComplete);
        }

        /// <summary>Only call after the referenced virtual public event actually occurs. Route alone never grants it.</summary>
        public XgEpilogueResult RecordPublicEvent(XgEpilogueBinding token, XgPublicEventReceipt receipt)
        {
            return RecordVerifiedPublicEvent(token, receipt);
        }

        public XgEpilogueResult SaveArticle(XgEpilogueBinding token, string articleId, int version, string sourceArticleId)
        {
            var blocked = CheckLetter(token, articleId); if (blocked != null) return blocked;
            if (version < 1 || string.IsNullOrWhiteSpace(sourceArticleId)) return Fail("invalid_article_source");
            var ep = state.epilogue.Copy();
            if (ep.savedAt != 0) return ep.letterVersion == version && ep.sourceArticleId == sourceArticleId ? Unchanged() : Fail("article_version_locked");
            if (ep.phase != "letter") return Fail("presentation_incomplete");
            ep.letterVersion = version; ep.deliverySource = "public_saved"; ep.sourceArticleId = sourceArticleId;
            ep.savedAt = ep.displayDate;
            return Save(token, state.releaseOutcome.Copy(), ep, state.chapterComplete);
        }
        public XgEpilogueResult OpenArticle(XgEpilogueBinding token, string articleId)
        {
            var blocked = CheckLetter(token, articleId); if (blocked != null) return blocked;
            if (state.epilogue.savedAt == 0) return Fail("article_not_saved");
            if (state.epilogue.opened) return Unchanged();
            var ep = state.epilogue.Copy(); ep.consentShown = true; ep.opened = true; ep.phase = "reading";
            return Save(token, state.releaseOutcome.Copy(), ep, state.chapterComplete);
        }
        public XgEpilogueResult SaveReadingPosition(XgEpilogueBinding token, string articleId, string paragraphId)
        {
            var blocked = CheckLetter(token, articleId); if (blocked != null) return blocked;
            if (!state.epilogue.opened) return Fail("article_not_opened");
            if (string.IsNullOrEmpty(paragraphId) || paragraphId.Length > 120) return Fail("invalid_paragraph");
            if (state.epilogue.readParagraphId == paragraphId) return Unchanged();
            var ep = state.epilogue.Copy(); ep.readParagraphId = paragraphId;
            return Save(token, state.releaseOutcome.Copy(), ep, state.chapterComplete);
        }
        public XgEpilogueResult Complete(XgEpilogueBinding token, string articleId, int version)
        {
            if (IsCurrent(token) && CanEnterEpilogue(state) && state.epilogue != null && state.epilogue.phase == "completed"
                && state.epilogue.letterId == articleId && state.epilogue.letterVersion == version
                && !string.IsNullOrEmpty(state.epilogue.completionReceipt)) return Unchanged();
            var blocked = CheckLetter(token, articleId); if (blocked != null) return blocked;
            var ep = state.epilogue.Copy();
            if (!ep.opened || !ep.consentShown || ep.savedAt == 0 || ep.letterVersion != version) return Fail("letter_not_ready");
            if (!string.IsNullOrEmpty(ep.completionReceipt)) return Unchanged();
            ep.completionReceipt = "epilogue:" + Guid.NewGuid().ToString("N"); ep.completedAt = ep.displayDate; ep.phase = "completed";
            return Save(token, state.releaseOutcome.Copy(), ep, true);
        }
        XgEpilogueResult CheckLetter(XgEpilogueBinding token, string articleId)
        {
            if (!IsCurrent(token)) return Fail("stale_binding");
            if (!HasCommittedRelease(state) || state.epilogue == null) return Fail("release_not_committed");
            if (!CanEnterEpilogue(state)) return Fail("launch_night_incomplete");
            if (state.epilogue.phase == "launch_night") return Fail("epilogue_not_entered");
            if (articleId != state.epilogue.letterId || articleId != RecommendedLetter(state.releaseOutcome.route)) return Fail("wrong_article");
            if (state.epilogue.phase == "completed") return Fail("read_only_completed");
            return null;
        }
        XgEpilogueResult Save(XgEpilogueBinding token, ReleaseOutcome release, EpilogueState ep, bool complete)
        {
            if (saving) return Fail("save_in_progress");
            if (!IsCurrent(token)) return Fail("stale_binding");
            if (token.Revision == int.MaxValue) return Fail("revision_exhausted");
            var beforeRelease = state.releaseOutcome; var beforeEpilogue = state.epilogue; bool beforeComplete = state.chapterComplete;
            ep.revision = token.Revision + 1;
            saving = true; state.releaseOutcome = release; state.epilogue = ep; state.chapterComplete = complete;
            bool saved = false;
            try { saved = saveNow(); }
            catch (Exception) { saved = false; }
            finally
            {
                if (!saved) { state.releaseOutcome = beforeRelease; state.epilogue = beforeEpilogue; state.chapterComplete = beforeComplete; }
                saving = false;
            }
            // The saved old instance may have succeeded while a callback rebound the UI. Never route that new save.
            return saved ? new XgEpilogueResult(true, stillCurrent() ? "saved" : "saved_detached", true) : Fail("save_failed");
        }
    }
}
