namespace LingGuangV05.Runtime.Persistence
{
    // Persistence remains language-neutral; the runtime getter localizes its player-facing status.
    public static class SnapshotStatusText
    {
        static readonly string[,] copy = {
            { "存档超过安全读取容量，历史未裁剪、旧存档未覆盖。", "The snapshot exceeds the safe read limit. History was not trimmed and the previous save is unchanged." },
            { "主存档或归档不可用，已恢复上一次完整备份。", "The primary save or archive is unavailable. The previous complete backup was restored." },
            { "导出目标已有存档，未覆盖。", "The export destination already contains a save; nothing was overwritten." },
            { "导出校验失败，源存档未改变。", "Export verification failed. The source save is unchanged." },
            { "已导出独立存档目录（包含快照归档）。", "Exported a self-contained save folder, including its snapshot archive." },
            { "导入源与当前存档相同。", "The import source is the current save folder." },
            { "导出失败：", "Export failed: " }, { "导入失败：", "Import failed: " },
            { "存档已保护，不能自动覆盖；请先备份并确认重置。", "The save is protected against automatic replacement. Back it up before confirming a reset." },
            { "拒绝写入空存档。", "Refused to write an empty save." },
            { "存档超过容量上限。", "The save exceeds the size limit." },
            { "已保存（本机离线存档）", "Saved locally (offline)." },
            { "保存失败，原存档未主动删除：", "Saving failed; the previous save was not deleted: " },
            { "存档无法读取，已阻止自动覆盖。", "The save could not be read. Automatic replacement is blocked." },
            { "新游戏：尚无存档。", "New game: no save yet." },
            { "存档内容验证失败。", "Save content validation failed." }, { "存档内容验证失败：", "Save content validation failed: " },
            { "旧存档已留作重置备份。", "The previous save was archived before resetting." },
            { "无法备份旧存档，未重置进度：", "Could not archive the old save. Progress was not reset: " },
            { "旧存档已另存一份带时间的备份。", "A timestamped copy of the previous save was created." },
            { "无法备份旧存档，没有重新开始：", "Could not back up the previous save. Restart was cancelled: " },
            { "存档体积无效。", "Invalid save size." }, { "存档头不完整。", "Incomplete save header." },
            { "存档属于其他版本，未覆盖。", "The save belongs to another version and was not replaced." },
            { "存档格式无效。", "Invalid save format." }, { "存档校验段不完整。", "Incomplete save checksum section." },
            { "存档校验失败。", "Save checksum failed." }, { "已读取本机存档。", "Loaded the local save." },
            { "读取失败：", "Read failed: " }
        };
        public static string Localize(string text, bool english)
        {
            if (!english || string.IsNullOrEmpty(text)) return text;
            for (int i = 0; i < copy.GetLength(0); i++) text = text.Replace(copy[i, 0], copy[i, 1]);
            return text;
        }
    }
}
