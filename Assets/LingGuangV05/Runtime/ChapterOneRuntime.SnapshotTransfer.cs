using System;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Runtime.Persistence;
using UnityEngine;

namespace LingGuangV05.Runtime
{
    public sealed partial class ChapterOneRuntime
    {
        /// <summary>Exports one game snapshot plus its immutable archive into a new folder; profile settings are separate.</summary>
        public bool ExportSaveSnapshot(string destinationDirectory)
        {
            EnsureInitialized();
            if (!useDiskSave || store == null || Sim == null || Sim.InPrologue || persistenceBlocked || !SaveNow()) return false;
            bool saved = store.TryExportSnapshot(destinationDirectory, out var message, ValidArchive);
            SaveStatus = message; Changed?.Invoke(); return saved;
        }

        /// <summary>Reconstructs and validates before committing. The import has no references back to the source folder.</summary>
        public bool ImportSaveSnapshot(string sourceDirectory)
        {
            EnsureInitialized();
            if (!useDiskSave || store == null || persistenceBlocked) return false;
            try
            {
                var source = new AtomicSaveStore(sourceDirectory);
                if (string.Equals(source.Folder, store.Folder, StringComparison.OrdinalIgnoreCase))
                { SaveStatus = GameText.T("请选择另一个存档目录。", "Choose another save folder."); return false; }
                var loaded = source.Load(ValidArchive);
                if (!loaded.Success) { SaveStatus = loaded.Message; return false; }
                var restored = new ChapterOneSim(JsonUtility.FromJson<GameState>(loaded.Payload));
                restored.S.lastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (!store.TrySaveSnapshot(JsonUtility.ToJson(restored.S), out var message)) { SaveStatus = message; return false; }
                Attach(restored); KnownProfile(restored, false);
                SaveStatus = GameText.T("已导入，当前进度已切换。", "Imported. Current progress has changed.");
                Changed?.Invoke(); return true;
            }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException || error is ArgumentException || error is InvalidOperationException)
            { SaveStatus = GameText.T("导入失败，当前进度未改变：", "Import failed; current progress is unchanged: ") + error.Message; return false; }
        }
    }
}
