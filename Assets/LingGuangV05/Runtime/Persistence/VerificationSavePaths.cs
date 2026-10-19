using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace LingGuangV05.Runtime.Persistence
{
    /// <summary>Opt-in file-data isolation for the ordinary Player. Never changes company/product or PlayerPrefs.</summary>
    public static class VerificationSavePaths
    {
        public const string Option = "--verification-save-dir";
        public const string MarkerName = ".lingguang-verification-save";
        const string MarkerHeader = "LINGGUANG-VERIFICATION-SAVE:1";
        static bool resolved;
        static string root;
        static Exception failure;

        public static string Root
        {
            get
            {
                if (!resolved)
                {
                    resolved = true;
                    try { root = Prepare(Environment.GetCommandLineArgs(), ProtectedRoots()); }
                    catch (Exception error) { failure = error; }
                }
                if (failure != null) throw new InvalidOperationException("Verification save directory rejected; normal player storage will not be opened.", failure);
                return root;
            }
        }
        public static string DesktopProfileDirectory => Child("DesktopProfile");
        public static string ModelCacheDirectory => Child("ModelCache");
        public static string MusicDirectory => Child("Music2016");
        static string Child(string name) { string directory = Root; return directory == null ? null : Path.Combine(directory, name); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void BeforeStartup()
        {
            try { if (Root != null) Debug.Log("VERIFICATION_SAVE_ROOT " + Root); }
            catch (Exception error) { Debug.LogError(error); Application.Quit(64); }
            // Quit is asynchronous. Every storage consumer still calls Root, which keeps throwing after failure.
        }

        static string[] ProtectedRoots()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string localLow = Path.Combine(Path.GetDirectoryName(local), "LocalLow");
            return new[] {
                Application.persistentDataPath,
                Path.Combine(localLow, "RippleLocalPreview", "LingGuang-Playability-20261008"),
                Path.Combine(localLow, "DefaultCompany", "competition"),
                Path.GetDirectoryName(Application.dataPath)
            };
        }

        /// <summary>Small filesystem-only seam for QA. Disabled returns null; explicit errors always throw.</summary>
        public static string Prepare(string[] arguments, string[] protectedRoots)
        {
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));
            int found = -1;
            for (int i = 0; i < arguments.Length; i++)
            {
                string arg = arguments[i] ?? "";
                if (arg.StartsWith(Option + "=", StringComparison.Ordinal)) throw new ArgumentException("Use --verification-save-dir followed by a separate absolute directory argument.");
                if (arg != Option) continue;
                if (found >= 0) throw new ArgumentException("Duplicate verification directory option.");
                found = i;
            }
            if (found < 0) return null;
            if (found + 1 >= arguments.Length) throw new ArgumentException("Missing verification directory.");
            string candidate = Canonical(arguments[found + 1]);
            if (protectedRoots == null || protectedRoots.Length == 0) throw new ArgumentException("Protected player paths are required.");
            foreach (string path in protectedRoots)
            {
                string protectedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (Within(candidate, protectedPath) || Within(protectedPath, candidate))
                    throw new ArgumentException("Verification directory overlaps protected player/build storage: " + candidate);
            }
            RejectLinksToRoot(candidate);
            if (File.Exists(candidate)) throw new IOException("Verification directory is a file.");
            bool exists = Directory.Exists(candidate);
            string marker = Path.Combine(candidate, MarkerName);
            bool marked = exists && File.Exists(marker);
            if (exists)
            {
                RejectTreeLinks(candidate);
                if (marked)
                {
                    var info = new FileInfo(marker);
                    if (info.Length > 4096) throw new InvalidDataException("Verification marker is too large.");
                    string[] lines = File.ReadAllLines(marker, Encoding.UTF8);
                    if (lines.Length != 3 || lines[0] != MarkerHeader || !string.Equals(lines[1], candidate, StringComparison.OrdinalIgnoreCase) || !Guid.TryParseExact(lines[2], "N", out _))
                        throw new InvalidDataException("Verification marker does not belong to this directory.");
                }
                else if (Directory.GetFileSystemEntries(candidate).Length != 0)
                    throw new InvalidDataException("First verification directory must be empty; refusing unmarked existing data.");
            }
            Directory.CreateDirectory(candidate); // Failure propagates; never fall back to LocalLow.
            if (!marked)
            {
                byte[] data = new UTF8Encoding(false).GetBytes(MarkerHeader + "\n" + candidate + "\n" + Guid.NewGuid().ToString("N") + "\n");
                using (var stream = new FileStream(marker, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(data, 0, data.Length); stream.Flush(true); }
            }
            // A valid marker is not evidence of current write permission. Probe only a new owned file.
            string probe = Path.Combine(candidate, ".verification-write-" + Guid.NewGuid().ToString("N"));
            bool created = false;
            try
            {
                using (var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { created = true; stream.WriteByte(1); stream.Flush(true); }
            }
            finally { if (created) File.Delete(probe); }
            return candidate;
        }

        static string Canonical(string value)
        {
            // The supported release target is Windows. Reject relative drive paths, UNC/device names and ADS.
            if (string.IsNullOrWhiteSpace(value) || value.Length < 3 || !char.IsLetter(value[0]) || value[1] != ':' || (value[2] != '\\' && value[2] != '/'))
                throw new ArgumentException("Verification directory must be an absolute local Windows directory.");
            for (int i = 2; i < value.Length; i++)
                if (char.IsControl(value[i]) || ":*?\"<>|~".IndexOf(value[i]) >= 0) throw new ArgumentException("Ambiguous/invalid verification directory character.");
            foreach (string part in value.Substring(3).Split(new[] {'\\', '/'}, StringSplitOptions.RemoveEmptyEntries))
                if (part != "." && part != ".." && (part.EndsWith(".", StringComparison.Ordinal) || part.EndsWith(" ", StringComparison.Ordinal)))
                    throw new ArgumentException("Trailing-dot/space path aliases are not accepted.");
            string full = Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (full.Length <= 2) throw new ArgumentException("A drive root cannot be a verification directory.");
            return full;
        }
        static bool Within(string path, string ancestor) => string.Equals(path, ancestor, StringComparison.OrdinalIgnoreCase) || path.StartsWith(ancestor + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        static void RejectLinksToRoot(string path)
        {
            for (var item = new DirectoryInfo(path); item != null; item = item.Parent)
            {
                FileAttributes attributes;
                try { attributes = File.GetAttributes(item.FullName); }
                catch (FileNotFoundException) { continue; }
                catch (DirectoryNotFoundException) { continue; }
                if ((attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Verification path contains a link/reparse point.");
            }
        }
        static void RejectTreeLinks(string directory)
        {
            foreach (string path in Directory.GetFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Verification tree contains a link/reparse point.");
                if ((attributes & FileAttributes.Directory) != 0) RejectTreeLinks(path);
            }
        }
    }
}
