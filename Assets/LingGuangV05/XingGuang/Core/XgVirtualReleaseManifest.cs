using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace LingGuangV05.XingGuang
{
    public sealed partial class ReleaseOutcome
    {
        // Optional immutable strings: old saves remain valid and reads never fabricate a manifest.
        public string publicCapabilities = "";
        public string publicManifestVersion = "", publicManifestSnapshot = "", publicManifestSha256 = "";
    }

    /// <summary>A saved in-game metadata snapshot, not model weights, a file export, an upload, or a dialogue hash.</summary>
    public static class XgVirtualReleaseManifest
    {
        public const string Version = "xg.virtual-release-manifest/1";
        // Called only on the detached fresh release candidate, before the existing atomic save.
        internal static void Populate(ReleaseOutcome release)
        {
            release.publicManifestVersion = release.publicManifestSnapshot = release.publicManifestSha256 = "";
            if (release.route != "public_model") return;
            release.publicManifestVersion = Version;
            release.publicManifestSnapshot = Build(release);
            release.publicManifestSha256 = Digest(release.publicManifestSnapshot);
        }
        public static bool Recorded(ReleaseOutcome release) => release != null
            && (!string.IsNullOrEmpty(release.publicManifestVersion) || !string.IsNullOrEmpty(release.publicManifestSnapshot)
                || !string.IsNullOrEmpty(release.publicManifestSha256));
        public static bool Valid(ReleaseOutcome release) => release != null && release.route == "public_model"
            && release.publicManifestVersion == Version && !string.IsNullOrEmpty(release.publicManifestSnapshot)
            && release.publicManifestSnapshot == Build(release)
            && string.Equals(release.publicManifestSha256, Digest(release.publicManifestSnapshot), StringComparison.Ordinal);
        static string Digest(string content)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(content))).Replace("-", "").ToLowerInvariant();
        }
        static string Build(ReleaseOutcome release)
        {
            // Canonical JSON: fixed order, LF line ends, UTF-8 without BOM, no trailing LF.
            // Only the five authored public-preview fields enter this snapshot; no private state or receipts.
            return "{\n  \"schema\": " + Quote(Version)
                + ",\n  \"kind\": \"virtual_public_release_manifest\""
                + ",\n  \"modelFileStatus\": \"not_exported\""
                + ",\n  \"product\": " + Quote(release.publicName)
                + ",\n  \"description\": " + Quote(release.publicDescription)
                + ",\n  \"capabilities\": " + Quote(release.publicCapabilities)
                + ",\n  \"limitations\": " + Quote(release.publicLimitations)
                + ",\n  \"maintenanceAndLicense\": " + Quote(release.publicTerms) + "\n}";
        }
        static string Quote(string value)
        {
            var b = new StringBuilder("\"");
            foreach (char c in value ?? "")
            {
                if (c == '"' || c == '\\') b.Append('\\').Append(c);
                else if (c < 32) b.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                else b.Append(c);
            }
            return b.Append('"').ToString();
        }
    }
}
