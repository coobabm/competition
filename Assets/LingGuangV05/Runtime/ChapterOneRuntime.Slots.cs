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
            if (index < 1 || index > (chapter ? 6 : ManualArchiveLimit + 1) || store == null) return null;
            return !chapter && index == 1 ? store : new AtomicSaveStore(Path.Combine(store.Folder,
                chapter ? "chapters" : "slots", index.ToString()));
        }

        public bool HasAnyArchive
        {
            get { for (int slot=1; slot<=ManualArchiveLimit+1; slot++) if (ArchiveExists(slot)) return true; return false; }
        }
        // UI order changes without moving or renaming any existing save file.
        public static int ArchiveDisplaySlot(int row) => row >= 0 && row < 5 ? row + 2 : row == 5 ? 1 : 0;

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
            string source = loaded.Recovered ? archive.SavePath + ".bak" : archive.SavePath;
            return state.aiName + " · ¥" + state.money.ToString("0") + " · " + File.GetLastWriteTime(source).ToString("MM-dd HH:mm")
                + (loaded.Recovered ? GameText.T(" · 备份", " · Backup") : "");
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
            if (archive == null || !useDiskSave || Sim.InPrologue || persistenceBlocked || endingArchiveBusy) return false;
            try {
                Saving?.Invoke();
                bool saved = archive.TrySaveSnapshot(JsonUtility.ToJson(Sim.S), out var message);
                if(saved)SaveArchiveThumbnail(index);
                SaveStatus = message; Changed?.Invoke(); return saved;
            } catch (Exception e) { SaveStatus = e.Message; return false; }
        }

        public bool LoadArchive(int index, bool chapter = false)
        {
            var archive = ArchiveStore(index, chapter);
            if (archive == null || !useDiskSave || persistenceBlocked || endingArchiveBusy)
            {
                SaveStatus = endingArchiveBusy
                    ? GameText.T("正在保存结局，请稍后再读取。", "Saving the ending. Please try loading again shortly.")
                    : GameText.T("暂时无法读取存档，当前进度未改变。请返回后重试。", "Unable to load right now. Your current progress is unchanged. Please go back and try again.");
                return false;
            }
            var loaded = archive.Load(ValidArchive);
            if (!loaded.Success) { SaveStatus = loaded.Message; return false; }
            // Validate and instantiate before touching the active save. Atomic replacement retains a backup.
            ChapterOneSim restored;
            try { restored = new ChapterOneSim(JsonUtility.FromJson<GameState>(loaded.Payload)); }
            catch (Exception error)
            {
                SaveStatus = GameText.T("存档无法恢复，当前进度未改变。", "The save could not be restored. Current progress is unchanged.");
                Debug.LogWarning("[Save] Archive reconstruction failed: " + error.Message);
                return false;
            }
            restored.S.lastSeenUnix = Math.Max(restored.S.lastSeenUnix,UtcNow);
            if (!PrepareOfflineArchive(restored.S)) return false;
            byte[] restoredThumbnail=loaded.Recovered?null:ReadArchiveThumbnail(index,chapter);
            if (!store.TrySaveSnapshot(JsonUtility.ToJson(restored.S), out var message)) { SaveStatus = message; return false; }
            SetArchiveThumbnail(1,restoredThumbnail);
            Attach(restored); KnownProfile(restored, false);
            SaveStatus = GameText.T("已读取，当前进度已切换。", "Loaded. Current progress has changed.") + OfflineSummary();
            Changed?.Invoke(); return true;
        }

        public bool CaptureChapter(int chapter)
        {
            var archive = ArchiveStore(chapter, true);
            if (archive == null || !useDiskSave || TestMode || Sim.InPrologue || persistenceBlocked || endingArchiveBusy) return false;
            // First-unlock snapshots are immutable across reloads and new games.
            if (File.Exists(archive.SavePath) || File.Exists(archive.SavePath + ".bak")) return true;
            try {
                Saving?.Invoke();
                bool saved = archive.TrySaveSnapshot(JsonUtility.ToJson(Sim.S), out var message);
                if (!saved) SaveStatus = message;
                return saved;
            } catch (Exception e) { SaveStatus = e.Message; return false; }
        }
    }
}
