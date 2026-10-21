using System;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Runtime.Persistence;
using UnityEngine;

namespace LingGuangV05.Runtime
{
    public sealed class EndingBranchSummary
    {
        public string Id, Name, LabJson;
    }

    public sealed partial class ChapterOneRuntime
    {
        string endingRoot;
        bool endingArchiveBusy;
        string EndingRoot => endingRoot ?? (endingRoot = store.Folder);
        static bool EndingId(string value) => value != null && value.Length == 32
            && Guid.TryParseExact(value, "N", out _);
        AtomicSaveStore EndingStore(string kind, string id)
        {
            if (!EndingId(id)) throw new ArgumentException("Invalid ending archive identity.");
            return new AtomicSaveStore(Path.Combine(EndingRoot, "endings", kind, id));
        }
        bool EndingReady(ChapterOneSim expected) => useDiskSave && !TestMode && !persistenceBlocked
            && !endingArchiveBusy && store != null && expected != null && !expected.InPrologue && ReferenceEquals(Sim, expected);
        bool EndingFailure(string detail)
        {
            SaveStatus = GameText.T("结局存档未完成，当前存档身份保持不变。", "Ending archive action failed. The current save identity is unchanged.") + " " + detail;
            return false;
        }
        bool ValidEndingSnapshot(string payload, string checkpoint, Func<string, bool> isPreChoice)
        {
            if (!ValidArchive(payload) || isPreChoice == null) return false;
            var state = ReadNarrationState(payload);
            return state.endingCheckpointId == checkpoint && isPreChoice(state.labState);
        }

        /// <summary>Collect all apps before the real route action. No write to the active primary or manual slots.
        /// The caller installs the returned ID only for its own atomic route transaction, rolling it back on failure.</summary>
        public bool CaptureEndingChoice(ChapterOneSim expected, Func<string, bool> isPreChoice, out string checkpoint)
        {
            checkpoint = null;
            EnsureInitialized();
            if (!EndingReady(expected) || isPreChoice == null) return EndingFailure("unavailable");
            endingArchiveBusy = true;
            try
            {
                Saving?.Invoke();
                if (!ReferenceEquals(Sim, expected) || !isPreChoice(expected.S.labState)) return EndingFailure("not_before_choice");
                // Serialize a detached copy; collection does not install an ID in the live state.
                var copy = ReadNarrationState(JsonUtility.ToJson(expected.S));
                string id = Guid.NewGuid().ToString("N");
                copy.endingCheckpointId = id;
                var destination = EndingStore("checkpoints", id);
                if (Directory.Exists(destination.Folder) || File.Exists(destination.Folder)) return EndingFailure("destination_exists");
                string payload = JsonUtility.ToJson(copy);
                if (!ValidEndingSnapshot(payload, id, isPreChoice)) return EndingFailure("invalid_checkpoint");
                if (!destination.TrySaveSnapshot(payload, out var message)) return EndingFailure(message);
                var verified = destination.Load(p => ValidEndingSnapshot(p, id, isPreChoice));
                if (!verified.Success || verified.Payload != payload) return EndingFailure("checkpoint_verification_failed");
                checkpoint = id;
                return true;
            }
            catch (Exception error) { return EndingFailure(error.Message); }
            finally { endingArchiveBusy = false; }
        }

        public bool HasEndingChoice(ChapterOneSim expected, Func<string, bool> isPreChoice)
        {
            EnsureInitialized();
            if (!EndingReady(expected) || !EndingId(expected.S.endingCheckpointId)) return false;
            try
            {
                string id = expected.S.endingCheckpointId;
                return EndingStore("checkpoints", id).Load(p => ValidEndingSnapshot(p, id, isPreChoice)).Success;
            }
            catch (Exception) { return false; }
        }

        /// <summary>Build a complete independent store/profile before publishing its directory and switching identity.
        /// Load resolves large snapshot blocks; saving recreates the blocks in the destination, never copies a header.</summary>
        public bool CreateEndingBranch(ChapterOneSim expected, string expectedCheckpoint, Func<string, bool> isPreChoice)
        {
            EnsureInitialized();
            if (!EndingReady(expected) || expected.S.endingCheckpointId != expectedCheckpoint || !EndingId(expectedCheckpoint))
                return EndingFailure("stale_binding");
            endingArchiveBusy = true;
            string pending = null, published = null;
            try
            {
                var loaded = EndingStore("checkpoints", expectedCheckpoint).Load(p => ValidEndingSnapshot(p, expectedCheckpoint, isPreChoice));
                if (!loaded.Success) return EndingFailure(loaded.Message);
                var state = ReadNarrationState(loaded.Payload);
                string id = Guid.NewGuid().ToString("N");
                state.endingBranchId = id;
                state.lastSeenUnix = Math.Max(state.lastSeenUnix, UtcNow);
                var restored = new ChapterOneSim(state);
                var target = EndingStore("branches", id);
                if (Directory.Exists(target.Folder) || File.Exists(target.Folder)) return EndingFailure("destination_exists");
                pending = target.Folder + ".pending";
                if (Directory.Exists(pending) || File.Exists(pending)) return EndingFailure("destination_exists");
                var draft = new AtomicSaveStore(pending);
                var profile = PlayerProfile.FromState(restored.S) ?? profiles?.Read();
                // Do not call KnownProfile: that would write to the original profile during preparation.
                var draftProfile = new PlayerProfileStore(draft);
                string message = "";
                // Pre-opening legacy saves may have no player profile; do not invent one or reject their state.
                if (profile != null && (!draftProfile.Write(profile, out message) || draftProfile.Read() == null)) return EndingFailure(message);
                string payload = JsonUtility.ToJson(restored.S);
                if (!draft.TrySaveSnapshot(payload, out message)) return EndingFailure(message);
                var verified = draft.Load(p => ValidEndingSnapshot(p, expectedCheckpoint, isPreChoice));
                if (!verified.Success || verified.Payload != payload) return EndingFailure("branch_verification_failed");
                if (!ReferenceEquals(Sim, expected)) return EndingFailure("stale_binding");
                Directory.Move(pending, target.Folder);
                published = target.Folder;
                if (!SwitchEndingStore(expected, target, restored))
                {
                    // A failed notification/rebind is not a completed switch. Keep the candidate out of the list.
                    Directory.Move(published, pending);
                    return false;
                }
                SaveStatus = GameText.T("已建立独立分支副本；原通关档保持不变。", "Independent branch created. The original completed save is unchanged.");
                return true;
            }
            catch (Exception error) { return EndingFailure(error.Message); }
            finally { endingArchiveBusy = false; }
        }

        public List<EndingBranchSummary> EndingBranches()
        {
            EnsureInitialized();
            var rows = new List<EndingBranchSummary>();
            if (store == null) return rows;
            try
            {
                string folder = Path.Combine(EndingRoot, "endings", "branches");
                if (!Directory.Exists(folder)) return rows;
                foreach (string path in Directory.GetDirectories(folder))
                {
                    string id = Path.GetFileName(path);
                    if (!EndingId(id) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) continue;
                    var source = new AtomicSaveStore(path);
                    var read = source.Load(ValidArchive);
                    var profile = new PlayerProfileStore(source);
                    if (!read.Success || File.Exists(profile.Path) && profile.Read() == null) continue;
                    var state = ReadNarrationState(read.Payload);
                    if (state.endingBranchId == id && EndingId(state.endingCheckpointId))
                        rows.Add(new EndingBranchSummary { Id = id, Name = state.aiName, LabJson = state.labState });
                }
                rows.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            }
            catch (Exception error) { SaveStatus = error.Message; }
            return rows;
        }

        public bool ResumeEndingBranch(ChapterOneSim expected, string id)
        {
            EnsureInitialized();
            if (!EndingReady(expected) || !EndingId(id)) return EndingFailure("stale_binding");
            endingArchiveBusy = true;
            try
            {
                var source = EndingStore("branches", id);
                var read = source.Load(ValidArchive);
                if (!read.Success) return EndingFailure(read.Message);
                var state = ReadNarrationState(read.Payload);
                var profile = new PlayerProfileStore(source);
                if (state.endingBranchId != id || !EndingId(state.endingCheckpointId) || File.Exists(profile.Path) && profile.Read() == null)
                    return EndingFailure("invalid_branch");
                var restored = new ChapterOneSim(state);
                if (!SwitchEndingStore(expected, source, restored)) return false;
                SaveStatus = GameText.T("已切换到独立分支副本。", "Switched to the independent branch copy.");
                return true;
            }
            catch (Exception error) { return EndingFailure(error.Message); }
            finally { endingArchiveBusy = false; }
        }

        // No disk writes after this boundary. Rebind failure restores the old identity, store and profile.
        bool SwitchEndingStore(ChapterOneSim expected, AtomicSaveStore target, ChapterOneSim restored)
        {
            if (!ReferenceEquals(Sim, expected)) return EndingFailure("stale_binding");
            var oldStore = store; var oldProfiles = profiles;
            bool oldDirty = dirty, oldNotify = notify;
            float oldSave = sinceSave, oldNotification = sinceNotify;
            try
            {
                store = target; profiles = new PlayerProfileStore(target);
                Attach(restored);
                return true;
            }
            catch (Exception error)
            {
                store = oldStore; profiles = oldProfiles;
                try { Attach(expected); } catch (Exception) { /* identity is restored before Changed is invoked */ }
                dirty = oldDirty; notify = oldNotify; sinceSave = oldSave; sinceNotify = oldNotification;
                return EndingFailure(error.Message);
            }
        }
    }
}
