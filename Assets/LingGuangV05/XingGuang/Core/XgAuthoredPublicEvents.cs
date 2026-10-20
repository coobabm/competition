using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace LingGuangV05.XingGuang
{
    /// <summary>A persisted fictional public scene. Not telemetry, generated testimony or a real network publication.</summary>
    [Serializable]
    public sealed class XgAuthoredPublicSource
    {
        public int version = 1, publishedAt, savedRevision;
        public string sourceRef = "", approvedTextId = "", sourceKind = "authored_fiction";
        public string route = "", platform = "", artifactId = "", releaseReceiptId = "";
        public string titleZh = "", titleEn = "", bodyZh = "", bodyEn = "", contentHash = "";
        public string[] eventIds = Array.Empty<string>();
        public XgAuthoredPublicSource Copy()
        {
            var copy = (XgAuthoredPublicSource)MemberwiseClone();
            copy.eventIds = eventIds == null ? Array.Empty<string>() : (string[])eventIds.Clone();
            return copy;
        }
    }

    public sealed partial class ReleaseOutcome
    {
        public int publicEventScriptVersion;
        public List<XgAuthoredPublicSource> publicEventSources = new List<XgAuthoredPublicSource>();
        internal void CopyAuthoredPublicSources(ReleaseOutcome from)
        {
            publicEventSources = new List<XgAuthoredPublicSource>();
            if (from.publicEventSources != null) foreach (var source in from.publicEventSources)
                if (source != null) publicEventSources.Add(source.Copy());
        }
        internal void ClearAuthoredPublicSources()
        { publicEventScriptVersion = 0; publicEventSources.Clear(); }
    }

    /// <summary>Versioned whitelist for the approved R1/R2 script. It reads only the approved public catalog.</summary>
    public static partial class XgAuthoredPublicEvents
    {
        public const int Version = 1;
        sealed class Definition
        {
            public readonly string id, route, platform, hash;
            public readonly string[] events;
            public Definition(string id, string route, string platform, string hash, params string[] events)
            { this.id = id; this.route = route; this.platform = platform; this.hash = hash; this.events = events; }
        }
        static Definition Find(string id)
        { foreach (var definition in Definitions) if (definition.id == id) return definition; return null; }
        public static bool AllowsEvent(string eventId)
        { foreach (var definition in Definitions) foreach (var id in definition.events) if (id == eventId) return true; return false; }
        public static string TextForEvent(string eventId)
        { foreach (var definition in Definitions) foreach (var id in definition.events) if (id == eventId) return definition.id; return ""; }
        public static string SourceLink(string releaseReceiptId, string approvedTextId)
        {
            if (string.IsNullOrWhiteSpace(releaseReceiptId) || releaseReceiptId.Length > 160 || Find(approvedTextId) == null) return "";
            foreach (char c in releaseReceiptId)
                if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == ':' || c == '-' || c == '_' || c == '.')) return "";
            return "xg-public://epilogue/" + Uri.EscapeDataString(releaseReceiptId) + "/" + approvedTextId + "/v1";
        }
        internal static bool Date(int value) => DateTime.TryParseExact(value.ToString(CultureInfo.InvariantCulture),
            "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        static string Hash(string titleZh, string titleEn, string bodyZh, string bodyEn)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(titleZh + "\n" + titleEn + "\n" + bodyZh + "\n" + bodyEn));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) result.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }
        static bool SameIds(string[] actual, string[] expected)
        {
            if (actual == null || actual.Length != expected.Length) return false;
            var set = new HashSet<string>(actual, StringComparer.Ordinal);
            if (set.Count != expected.Length) return false;
            foreach (var id in expected) if (!set.Contains(id)) return false;
            return true;
        }
        internal static List<XgAuthoredPublicSource> Create(ReleaseOutcome release, EpilogueState ep, XgPublicDiscourseCatalog catalog)
        {
            if (catalog == null) throw new FormatException("Missing authored public catalog");
            var result = new List<XgAuthoredPublicSource>();
            foreach (var definition in Definitions)
            {
                if (definition.route != release.route) continue;
                if (SourceLink(release.releaseReceiptId, definition.id).Length == 0) throw new FormatException("Invalid public release handle");
                XgPublicDiscourseRecord row = null;
                foreach (var record in catalog.All) if (record.id == definition.id) { row = record; break; }
                if (row == null || row.route != definition.route || row.platform != definition.platform || row.kind != "fictional"
                    || !row.requiresArtifact || row.stage != 6 || row.since > ep.displayDate
                    || !SameIds(row.requiredPublicEventIds, definition.events)
                    || Hash(row.titleZh, row.titleEn, row.bodyZh, row.bodyEn) != definition.hash)
                    throw new FormatException("Authored public script changed: " + definition.id);
                result.Add(new XgAuthoredPublicSource
                {
                    sourceRef = SourceLink(release.releaseReceiptId, row.id), approvedTextId = row.id,
                    route = row.route, platform = row.platform, artifactId = release.artifactId, releaseReceiptId = release.releaseReceiptId,
                    titleZh = row.titleZh, titleEn = row.titleEn, bodyZh = row.bodyZh, bodyEn = row.bodyEn, contentHash = definition.hash,
                    publishedAt = ep.displayDate, savedRevision = ep.revision + 1, eventIds = (string[])definition.events.Clone()
                });
            }
            if (result.Count != 5) throw new FormatException("Missing public route script");
            return result;
        }
        internal static bool Valid(XgAuthoredPublicSource source, ReleaseOutcome release, EpilogueState ep)
        {
            if (source == null || release == null || ep == null || source.version != Version || source.sourceKind != "authored_fiction"
                || release.publicEventScriptVersion != Version || source.route != release.route || source.artifactId != release.artifactId
                || source.releaseReceiptId != release.releaseReceiptId || source.savedRevision < 1 || source.savedRevision > ep.revision
                || !Date(source.publishedAt) || source.publishedAt < release.committedAt || source.publishedAt != ep.displayDate) return false;
            var definition = Find(source.approvedTextId);
            return definition != null && definition.route == source.route && definition.platform == source.platform
                && source.sourceRef == SourceLink(release.releaseReceiptId, source.approvedTextId)
                && SameIds(source.eventIds, definition.events) && source.contentHash == definition.hash
                && Hash(source.titleZh, source.titleEn, source.bodyZh, source.bodyEn) == definition.hash;
        }
    }

    public sealed partial class XgEpilogueFlow
    {
        XgEpilogueResult CheckPublicScript(XgEpilogueBinding token)
        {
            if (saving) return Fail("save_in_progress");
            if (!IsCurrent(token)) return Fail("stale_binding");
            if (!HasCommittedRelease(state) || state.epilogue == null || state.releaseOutcome.route == "local_only") return Fail("no_public_artifact");
            if (!CanEnterEpilogue(state)) return Fail("launch_night_incomplete");
            if (state.releaseOutcome.route == "online" && state.releaseOutcome.launchNight.ending != "completed") return Fail("online_service_stopped");
            if (state.epilogue.phase == "completed") return Fail("read_only_completed");
            if (state.epilogue.phase != "route" && state.epilogue.phase != "letter" && state.epilogue.phase != "reading") return Fail("public_script_not_entered");
            if (!XgAuthoredPublicEvents.Date(state.epilogue.displayDate) || !XgAuthoredPublicEvents.Date(state.releaseOutcome.committedAt)
                || state.epilogue.historicalReview || state.epilogue.displayDate < state.releaseOutcome.committedAt
                || state.releaseOutcome.launchNight != null && (!XgAuthoredPublicEvents.Date(state.releaseOutcome.launchNight.endedAt)
                    || state.releaseOutcome.launchNight.endedAt > state.epilogue.displayDate))
                return Fail("historical_review_no_public_events");
            return null;
        }

        /// <summary>Explicit route-scene script operation. Source snapshots and receipts share one atomic existing save.</summary>
        public XgEpilogueResult PublishAuthoredPublicEvents(XgEpilogueBinding token, XgPublicDiscourseCatalog catalog)
        {
            var blocked = CheckPublicScript(token); if (blocked != null) return blocked;
            var current = state.releaseOutcome;
            if (current.publicEventScriptVersion != 0 || current.publicEventSources != null && current.publicEventSources.Count != 0)
                return Fail("public_events_already_created");
            if (current.publicEventIds != null && current.publicEventIds.Count != 0 || current.publicEvents != null && current.publicEvents.Count != 0)
                return Fail("unverified_public_events_present");
            var release = current.Copy();
            try { release.publicEventSources = XgAuthoredPublicEvents.Create(release, state.epilogue, catalog); }
            catch (FormatException) { return Fail("invalid_public_script"); }
            release.publicEventScriptVersion = XgAuthoredPublicEvents.Version;
            foreach (var source in release.publicEventSources) foreach (var id in source.eventIds)
            {
                release.publicEvents.Add(new XgPublicEventReceipt { id = id, sourceRef = source.sourceRef, artifactId = source.artifactId, occurredAt = source.publishedAt });
                release.publicEventIds.Add(id);
            }
            return Save(token, release, state.epilogue.Copy(), state.chapterComplete);
        }

        bool PublicSourcesReadable(XgEpilogueBinding token)
        {
            if (saving || !IsCurrent(token) || !CanEnterEpilogue(state) || state.epilogue == null || state.releaseOutcome.route == "local_only") return false;
            var release = state.releaseOutcome;
            if (release.route == "online" && release.launchNight.ending != "completed") return false;
            if (state.epilogue.historicalReview || !XgAuthoredPublicEvents.Date(release.committedAt) || !XgAuthoredPublicEvents.Date(state.epilogue.displayDate)) return false;
            if (state.epilogue.phase != "route" && state.epilogue.phase != "letter" && state.epilogue.phase != "reading" && state.epilogue.phase != "completed") return false;
            return release.publicEventScriptVersion == XgAuthoredPublicEvents.Version;
        }
        XgAuthoredPublicSource VerifiedSource(string sourceRef)
        {
            var release = state.releaseOutcome;
            if (release.publicEventSources == null) return null;
            XgAuthoredPublicSource found = null;
            foreach (var source in release.publicEventSources)
            {
                if (source == null || source.sourceRef != sourceRef) continue;
                if (found != null || !XgAuthoredPublicEvents.Valid(source, release, state.epilogue)) return null;
                found = source;
            }
            return found;
        }
        bool Matches(XgPublicEventReceipt receipt, XgAuthoredPublicSource source)
            => receipt != null && source != null && XgAuthoredPublicEvents.TextForEvent(receipt.id) == source.approvedTextId
                && receipt.artifactId == source.artifactId && receipt.occurredAt == source.publishedAt && receipt.sourceRef == source.sourceRef;

        /// <summary>Use this projection after successful save/load; raw publicEventIds are not evidence.</summary>
        public string[] SavedPublicEventIds(XgEpilogueBinding token)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (!PublicSourcesReadable(token)) return Array.Empty<string>();
            var release = state.releaseOutcome;
            if (release.publicEvents == null || release.publicEventIds == null) return Array.Empty<string>();
            foreach (var receipt in release.publicEvents)
                if (receipt != null && release.publicEventIds.Contains(receipt.id) && Matches(receipt, VerifiedSource(receipt.sourceRef))) ids.Add(receipt.id);
            var result = new List<string>(ids); result.Sort(StringComparer.Ordinal); return result.ToArray();
        }
        /// <summary>Local whitelist resolver. Never navigate an arbitrary URL supplied by a save or public text.</summary>
        public XgAuthoredPublicSource FindPublicSource(XgEpilogueBinding token, string sourceRef)
            => PublicSourcesReadable(token) ? VerifiedSource(sourceRef)?.Copy() : null;

        public List<XgAuthoredPublicSource> PublicSourcesForRecord(XgEpilogueBinding token, string approvedTextId)
        {
            var result = new List<XgAuthoredPublicSource>();
            if (!PublicSourcesReadable(token)) return result;
            var source = VerifiedSource(XgAuthoredPublicEvents.SourceLink(state.releaseOutcome.releaseReceiptId, approvedTextId));
            if (source != null) result.Add(source.Copy());
            return result;
        }
        XgEpilogueResult RecordVerifiedPublicEvent(XgEpilogueBinding token, XgPublicEventReceipt receipt)
        {
            var blocked = CheckPublicScript(token); if (blocked != null) return blocked;
            if (receipt == null || !Matches(receipt, VerifiedSource(receipt.sourceRef))) return Fail("missing_saved_public_source");
            var release = state.releaseOutcome.Copy();
            foreach (var existing in release.publicEvents) if (existing.id == receipt.id)
                return existing.sourceRef == receipt.sourceRef && existing.artifactId == receipt.artifactId && existing.occurredAt == receipt.occurredAt
                    ? Unchanged() : Fail("public_event_locked");
            release.publicEvents.Add(receipt.Copy());
            if (!release.publicEventIds.Contains(receipt.id)) release.publicEventIds.Add(receipt.id);
            return Save(token, release, state.epilogue.Copy(), state.chapterComplete);
        }
    }
}
