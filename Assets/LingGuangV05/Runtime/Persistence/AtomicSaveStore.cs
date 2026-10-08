using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LingGuangV05.Runtime.Persistence
{
    public sealed class SaveLoadResult
    {
        public bool Success, Recovered, WriteBlocked;
        public string Payload, Message;
    }
    public sealed class AtomicSaveStore
    {
        public const int MaxPayloadBytes = 2 * 1024 * 1024;
        const string Header = "LINGGUANG-CHAPTER1:1";
        const string HeaderPrefix = "LINGGUANG-CHAPTER1:";
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        readonly object gate = new object();
        bool writeBlocked;
        bool recoveredBackup;
        public string SavePath { get; }
        public AtomicSaveStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Save directory is required.", nameof(directory));
            SavePath = Path.Combine(Path.GetFullPath(directory), "chapter-one.sav");
        }

        public bool TrySave(string payload, out string message)
        {
            lock (gate)
            {
                message = "";
                if (writeBlocked) { message = "存档已保护，不能自动覆盖；请先备份并确认重置。"; return false; }
                if (string.IsNullOrWhiteSpace(payload)) { message = "拒绝写入空存档。"; return false; }
                string temp = SavePath + ".tmp";
                try
                {
                    byte[] data = Utf8.GetBytes(payload);
                    if (data.Length > MaxPayloadBytes) { message = "存档超过容量上限。"; return false; }
                    Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                    string document = Header + "\n" + Digest(data) + "\n" + payload;
                    byte[] bytes = Utf8.GetBytes(document);
                    using (var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                    { output.Write(bytes, 0, bytes.Length); output.Flush(true); }
                    // Both files reside in the same directory. A failed replacement must
                    // leave the old primary intact; never delete it as a fallback.
                    if (File.Exists(SavePath)) File.Replace(temp, SavePath, recoveredBackup ? null : SavePath + ".bak");
                    else File.Move(temp, SavePath);
                    recoveredBackup = false;
                    message = "已保存（本机离线存档）";
                    return true;
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
                { message = "保存失败，原存档未主动删除：" + error.Message; return false; }
                finally
                {
                    try { if (File.Exists(temp)) File.Delete(temp); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }

        public SaveLoadResult Load(Func<string, bool> payloadValidator = null)
        {
            lock (gate)
            {
                var primary = ValidatePayload(Read(SavePath), payloadValidator);
                if (primary.Success) { writeBlocked = false; recoveredBackup = false; return primary; }
                if (primary.WriteBlocked) { writeBlocked = true; return primary; }
                var backup = ValidatePayload(Read(SavePath + ".bak"), payloadValidator);
                if (backup.Success)
                {
                    backup.Recovered = true;
                    backup.Message = "主存档损坏，已恢复上一次有效备份。";
                    recoveredBackup = true;
                    writeBlocked = false;
                    return backup;
                }
                bool exists = File.Exists(SavePath) || File.Exists(SavePath + ".bak");
                writeBlocked = exists || backup.WriteBlocked;
                return new SaveLoadResult { WriteBlocked = writeBlocked, Message = writeBlocked ? "存档无法读取，已阻止自动覆盖。" : "新游戏：尚无存档。" };
            }
        }

        static SaveLoadResult ValidatePayload(SaveLoadResult result, Func<string, bool> validator)
        {
            if (!result.Success || validator == null) return result;
            try
            {
                if (validator(result.Payload)) return result;
                return new SaveLoadResult { Message = "存档内容验证失败。" };
            }
            catch (NotSupportedException error)
            { return new SaveLoadResult { WriteBlocked = true, Message = error.Message }; }
            catch (Exception error) when (error is ArgumentException || error is InvalidDataException || error is InvalidOperationException)
            { return new SaveLoadResult { Message = "存档内容验证失败：" + error.Message }; }
        }

        public bool ArchiveForReset(out string message)
        {
            lock (gate)
            {
                try
                {
                    string suffix = ".reset-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                    foreach (string file in new[] { SavePath, SavePath + ".bak" }) if (File.Exists(file)) File.Move(file, file + suffix);
                    writeBlocked = false;
                    recoveredBackup = false;
                    message = "旧存档已留作重置备份。";
                    return true;
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                { message = "无法备份旧存档，未重置进度：" + error.Message; return false; }
            }
        }

        /// <summary>
        /// 重新开始 after the failure ending: copies the save and its backup, as they are, next to them with a timestamp
        /// (chapter-one.sav.gameover-20161201T014700). Nothing is moved or deleted; the primary stays where it is.
        /// </summary>
        public bool CopyForRestart(out string message, DateTime? now = null)
        {
            lock (gate)
            {
                try
                {
                    string stamp = ".gameover-" + (now ?? DateTime.UtcNow).ToString("yyyyMMddTHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                    foreach (string file in new[] { SavePath, SavePath + ".bak" })
                    {
                        if (!File.Exists(file)) continue;
                        string target = file + stamp;
                        // Two restarts in the same second keep both copies.
                        for (int i = 2; File.Exists(target); i++) target = file + stamp + "-" + i;
                        File.Copy(file, target, false);
                    }
                    message = "旧存档已另存一份带时间的备份。";
                    return true;
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                { message = "无法备份旧存档，没有重新开始：" + error.Message; return false; }
            }
        }

        /// <summary>The folder the save lives in (the player profile and the backups sit next to it).</summary>
        public string Folder => Path.GetDirectoryName(SavePath);

        /// <summary>
        /// Every save-shaped file in the folder, the primary and its .bak first, then the timestamped copies
        /// (.gameover-*, .reset-*, repair copies) newest first. Read-only; used to learn things from older saves.
        /// </summary>
        public string[] SaveFiles()
        {
            var found = new System.Collections.Generic.List<string>();
            if (File.Exists(SavePath)) found.Add(SavePath);
            if (File.Exists(SavePath + ".bak")) found.Add(SavePath + ".bak");
            try
            {
                if (Directory.Exists(Folder))
                {
                    var others = new System.Collections.Generic.List<string>();
                    foreach (string file in Directory.GetFiles(Folder, Path.GetFileName(SavePath) + "*"))
                    {
                        if (file == SavePath || file == SavePath + ".bak" || file.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
                        others.Add(file);
                    }
                    others.Sort((a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
                    found.AddRange(others);
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) { }
            return found.ToArray();
        }

        /// <summary>Reads one save-shaped file (header and checksum verified) without touching the store's state. False if it is not a valid save file.</summary>
        public static bool TryReadFile(string path, out string payload)
        {
            var result = Read(path);
            payload = result.Success ? result.Payload : null;
            return result.Success;
        }

        static SaveLoadResult Read(string path)
        {
            if (!File.Exists(path)) return new SaveLoadResult();
            try
            {
                if (new FileInfo(path).Length > MaxPayloadBytes + 128) return new SaveLoadResult { Message = "存档体积无效。" };
                string text = File.ReadAllText(path, Utf8);
                int first = text.IndexOf('\n');
                if (first < 0) return new SaveLoadResult { Message = "存档头不完整。" };
                string header = text.Substring(0, first);
                if (header.StartsWith(HeaderPrefix, StringComparison.Ordinal) && header != Header)
                    return new SaveLoadResult { WriteBlocked = true, Message = "存档属于其他版本，未覆盖。" };
                if (header != Header) return new SaveLoadResult { Message = "存档格式无效。" };
                int second = text.IndexOf('\n', first + 1);
                if (second < 0) return new SaveLoadResult { Message = "存档校验段不完整。" };
                string digest = text.Substring(first + 1, second - first - 1);
                string payload = text.Substring(second + 1);
                if (payload.Length == 0 || !string.Equals(digest, Digest(Utf8.GetBytes(payload)), StringComparison.Ordinal))
                    return new SaveLoadResult { Message = "存档校验失败。" };
                return new SaveLoadResult { Success = true, Payload = payload, Message = "已读取本机存档。" };
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            { return new SaveLoadResult { Message = "读取失败：" + error.Message }; }
        }

        static string Digest(byte[] data)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }
    }
}
