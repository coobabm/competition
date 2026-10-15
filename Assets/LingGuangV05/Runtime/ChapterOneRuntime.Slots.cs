using System;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Runtime.Persistence;
using UnityEngine;

namespace LingGuangV05.Runtime
{
    public sealed partial class ChapterOneRuntime
    {
        // Slot one is the original save path, so existing players keep their progress.
        AtomicSaveStore ArchiveStore(int index, bool chapter)
        {
            EnsureInitialized();
            if (index < 1 || index > 6 || store == null) return null;
            return !chapter && index == 1 ? store : new AtomicSaveStore(Path.Combine(store.Folder,
                chapter ? "chapters" : "slots", index.ToString()));
        }

        public bool ArchiveExists(int index, bool chapter = false)
        {
            var archive = ArchiveStore(index, chapter);
            return archive != null && (File.Exists(archive.SavePath) || File.Exists(archive.SavePath + ".bak"));
        }

        public string ArchiveDescription(int index, bool chapter = false)
        {
            var archive = ArchiveStore(index, chapter);
            if (archive == null || !File.Exists(archive.SavePath) && !File.Exists(archive.SavePath + ".bak")) return GameText.T("空", "Empty");
            var loaded = archive.Load(ValidArchive);
            if (!loaded.Success) return GameText.T("损坏，无法读取", "Unreadable save");
            var state = JsonUtility.FromJson<GameState>(loaded.Payload);
            return state.aiName + " · ¥" + state.money.ToString("0") + " · " + File.GetLastWriteTime(archive.SavePath).ToString("MM-dd HH:mm");
        }

        static bool ValidArchive(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload) || !payload.Contains("\"rngState\"")) return false;
            var state = JsonUtility.FromJson<GameState>(payload);
            if (state == null || state.version != 1 || state.nodes == null || state.nodes.Count < 3 || state.edges == null) return false;
            // A slot whose lab cannot be read must not replace the main save.
            return string.IsNullOrEmpty(state.labState) || LabStateReadable == null || LabStateReadable(state.labState);
        }

        public bool SaveSlot(int index)
        {
            if (index == 1) return SaveNow();
            var archive = ArchiveStore(index, false);
            if (archive == null || !useDiskSave || Sim.InPrologue || persistenceBlocked) return false;
            try {
                Saving?.Invoke();
                bool saved = archive.TrySave(JsonUtility.ToJson(Sim.S), out var message);
                SaveStatus = message; Changed?.Invoke(); return saved;
            } catch (Exception e) { SaveStatus = e.Message; return false; }
        }

        public bool LoadArchive(int index, bool chapter = false)
        {
            var archive = ArchiveStore(index, chapter);
            if (archive == null || !useDiskSave || persistenceBlocked) return false;
            var loaded = archive.Load(ValidArchive);
            if (!loaded.Success) { SaveStatus = loaded.Message; return false; }
            // Validate and instantiate before touching the active save. Atomic replacement retains a backup.
            var restored = new ChapterOneSim(JsonUtility.FromJson<GameState>(loaded.Payload));
            restored.S.lastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (!store.TrySave(JsonUtility.ToJson(restored.S), out var message)) { SaveStatus = message; return false; }
            Attach(restored); KnownProfile(restored, false);
            SaveStatus = GameText.T("已读取，当前进度已切换。", "Loaded. Current progress has changed.");
            Changed?.Invoke(); return true;
        }

        public bool CaptureChapter(int chapter)
        {
            var archive = ArchiveStore(chapter, true);
            if (archive == null || !useDiskSave || TestMode || Sim.InPrologue || persistenceBlocked) return false;
            // First-unlock snapshots are immutable across reloads and new games.
            if (File.Exists(archive.SavePath) || File.Exists(archive.SavePath + ".bak")) return true;
            try {
                Saving?.Invoke();
                bool saved = archive.TrySave(JsonUtility.ToJson(Sim.S), out var message);
                if (!saved) SaveStatus = message;
                return saved;
            } catch (Exception e) { SaveStatus = e.Message; return false; }
        }
    }
}
