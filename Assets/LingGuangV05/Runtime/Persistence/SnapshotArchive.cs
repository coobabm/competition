using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LingGuangV05.Runtime.Persistence
{
    /// <summary>Immutable, content-addressed snapshot blocks. Commit their descriptor through AtomicSaveStore last.</summary>
    internal sealed class SnapshotArchive
    {
        internal const int ChunkBytes = 128 * 1024, HashesPerManifest = 64;
        // A corrupt descriptor must not request unbounded memory or traversal. No history is trimmed at this limit.
        internal const int MaxSnapshotBytes = 256 * 1024 * 1024;
        internal const int MaxChunks = MaxSnapshotBytes / ChunkBytes;
        internal const int MaxManifestDepth = (MaxChunks + HashesPerManifest - 1) / HashesPerManifest;
        const string DescriptorHeader = "LINGGUANG-SNAPSHOT:1", ManifestHeader = "LINGGUANG-MANIFEST:1";
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        readonly string objects;
        readonly Action<string> checkpoint;
        internal SnapshotArchive(string saveFolder, Action<string> checkpoint = null)
        {
            objects = Path.Combine(Path.GetFullPath(saveFolder), "snapshot-archive", "objects");
            this.checkpoint = checkpoint;
        }
        internal string Write(byte[] payload)
        {
            if (payload == null || payload.Length == 0 || payload.Length > MaxSnapshotBytes)
                throw new InvalidDataException("Snapshot exceeds the 256 MiB safe read limit; history was not removed.");
            Directory.CreateDirectory(objects);
            var hashes = new List<string>((payload.Length + ChunkBytes - 1) / ChunkBytes);
            for (int offset = 0; offset < payload.Length; offset += ChunkBytes)
            {
                int length = Math.Min(ChunkBytes, payload.Length - offset);
                var chunk = new byte[length]; Buffer.BlockCopy(payload, offset, chunk, 0, length);
                hashes.Add(Put(chunk, "chunk"));
            }
            string next = "-";
            // Each manifest page is bounded; the main descriptor holds only one root hash, not an ever-growing list.
            for (int start = (hashes.Count - 1) / HashesPerManifest * HashesPerManifest; start >= 0; start -= HashesPerManifest)
            {
                int count = Math.Min(HashesPerManifest, hashes.Count - start);
                var lines = new List<string> { ManifestHeader, next, count.ToString(CultureInfo.InvariantCulture) };
                for (int i = 0; i < count; i++) lines.Add(hashes[start + i]);
                next = Put(Utf8.GetBytes(string.Join("\n", lines)), "manifest");
            }
            return string.Join("\n", new[] { DescriptorHeader, next, payload.Length.ToString(CultureInfo.InvariantCulture), hashes.Count.ToString(CultureInfo.InvariantCulture), Digest(payload) });
        }
        internal string Read(string descriptor)
        {
            if (descriptor == null || descriptor.Length > 512) throw new InvalidDataException("Invalid snapshot descriptor length.");
            var fields = descriptor.Split('\n');
            if (fields.Length != 5) throw new InvalidDataException("Incomplete snapshot descriptor.");
            if (fields[0] != DescriptorHeader) throw new NotSupportedException("Unsupported snapshot archive version; save protected.");
            if (!Hash(fields[1]) || !Hash(fields[4])) throw new InvalidDataException("Invalid snapshot root or payload hash.");
            if (!int.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out int bytes) || bytes < 1 || bytes > MaxSnapshotBytes)
                throw new InvalidDataException("Invalid snapshot expanded size.");
            if (!int.TryParse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture, out int count) || count < 1 || count > MaxChunks || count != (bytes + ChunkBytes - 1) / ChunkBytes)
                throw new InvalidDataException("Invalid snapshot chunk count.");
            int expectedDepth = (count + HashesPerManifest - 1) / HashesPerManifest;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            string current = fields[1]; int readCount = 0, readBytes = 0;
            using (var output = new MemoryStream(bytes))
            {
                while (current != "-")
                {
                    if (!Hash(current) || !visited.Add(current) || visited.Count > expectedDepth || visited.Count > MaxManifestDepth)
                        throw new InvalidDataException("Invalid snapshot manifest path, cycle, or depth.");
                    var lines = Utf8.GetString(Get(current, "manifest", 8192)).Split('\n');
                    if (lines.Length < 4 || lines[0] != ManifestHeader) throw new InvalidDataException("Invalid snapshot manifest format.");
                    if (lines[1] != "-" && !Hash(lines[1])) throw new InvalidDataException("Invalid next manifest reference.");
                    if (!int.TryParse(lines[2], NumberStyles.None, CultureInfo.InvariantCulture, out int entries) || entries < 1 || entries > HashesPerManifest || lines.Length != entries + 3)
                        throw new InvalidDataException("Invalid manifest entry count.");
                    int expectedEntries = Math.Min(HashesPerManifest, count - readCount);
                    if (entries != expectedEntries) throw new InvalidDataException("Manifest count differs from descriptor.");
                    for (int i = 0; i < entries; i++)
                    {
                        if (readCount >= count) throw new InvalidDataException("Too many snapshot blocks.");
                        int expectedBytes = Math.Min(ChunkBytes, bytes - readBytes);
                        byte[] chunk = Get(lines[i + 3], "chunk", ChunkBytes);
                        if (chunk.Length != expectedBytes) throw new InvalidDataException("Snapshot block length mismatch.");
                        output.Write(chunk, 0, chunk.Length); readBytes += chunk.Length; readCount++;
                    }
                    current = lines[1];
                }
                if (readCount != count || readBytes != bytes || visited.Count != expectedDepth) throw new InvalidDataException("Snapshot archive is incomplete.");
                byte[] data = output.ToArray();
                if (Digest(data) != fields[4]) throw new InvalidDataException("Snapshot payload checksum mismatch.");
                return Utf8.GetString(data);
            }
        }
        string Put(byte[] data, string kind)
        {
            string hash = Digest(data), target = ObjectPath(hash, kind);
            if (File.Exists(target)) { Get(hash, kind, kind == "chunk" ? ChunkBytes : 8192); return hash; }
            checkpoint?.Invoke("before-" + kind);
            // Unity's Windows Mono file APIs can still enforce MAX_PATH. A
            // second GUID appended to the hash crosses it in ordinary slot paths.
            string temp = Path.Combine(objects, "write-" + Guid.NewGuid().ToString("N").Substring(0, 16) + ".tmp");
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(data, 0, data.Length); stream.Flush(true); }
                if (Digest(File.ReadAllBytes(temp)) != hash) throw new IOException("Snapshot temporary file verification failed.");
                checkpoint?.Invoke("commit-" + kind);
                try { File.Move(temp, target); }
                catch (IOException) { if (!File.Exists(target)) throw; Get(hash, kind, kind == "chunk" ? ChunkBytes : 8192); }
                Get(hash, kind, kind == "chunk" ? ChunkBytes : 8192);
                return hash;
            }
            finally { try { if (File.Exists(temp)) File.Delete(temp); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }
        byte[] Get(string hash, string kind, int maximum)
        {
            string path = ObjectPath(hash, kind);
            var info = new FileInfo(path);
            if (!info.Exists || info.Length < 1 || info.Length > maximum) throw new InvalidDataException("Missing or invalid snapshot " + kind + ": " + hash);
            byte[] data = File.ReadAllBytes(path);
            if (data.Length > maximum || Digest(data) != hash) throw new InvalidDataException("Snapshot " + kind + " checksum mismatch: " + hash);
            return data;
        }
        string ObjectPath(string hash, string kind)
        {
            // Only 64 lowercase hex digits reach Path.Combine; serialized input never supplies a directory or filename.
            if (!Hash(hash) || (kind != "chunk" && kind != "manifest")) throw new InvalidDataException("Unsafe snapshot object reference.");
            return Path.Combine(objects, hash + "." + kind);
        }
        static bool Hash(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (char c in value) if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f')) return false;
            return true;
        }
        internal static string Digest(byte[] data)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }
    }
}
