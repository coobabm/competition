using System;
using System.IO;

namespace LingGuangV05.Runtime.Persistence
{
    /// <summary>
    /// LINGGUANG PATCH D7 (2026-10-19): where the DreamOS desktop UI data (Apps/User/System/Network/... .json) lives in a Player.
    /// The asset's original formula, <c>Application.dataPath.Replace(productName + "_Data", "")</c>, only works when the EXE
    /// name equals productName. Ours differ (LingGuang.exe vs LingGuang-Playability-20261008), so the data landed inside the
    /// game folder as <c>&lt;exe&gt;_DataDreamOS_UserData</c>. Player data now sits under the given player root
    /// (normally Application.persistentDataPath). A verification directory, when present, always wins.
    /// Pure string logic only: no Unity API, no filesystem access, so EditMode tests can call it directly.
    /// Old-version DreamOS data is intentionally not migrated (producer decision for the new release).
    /// </summary>
    public static class DesktopProfilePaths
    {
        public const string SubFolder = "DreamOS_UserData";

        /// <summary>
        /// Returns the profile directory WITH a trailing separator, because DreamOSDataManager builds file paths by
        /// plain concatenation (dataPath + "Network.json"), and many QA scripts set/read that same static field.
        /// </summary>
        /// <param name="verificationDesktopDirectory">VerificationSavePaths.DesktopProfileDirectory (null when not verifying).</param>
        /// <param name="playerRoot">Absolute player data root, normally Application.persistentDataPath.</param>
        /// <param name="profileId">DreamOSDataManager.profileID; null/empty means the default (shared) profile.</param>
        public static string Resolve(string verificationDesktopDirectory, string playerRoot, string profileId)
        {
            // Same behaviour as the existing verification override: the isolated directory itself, profileID ignored.
            if (verificationDesktopDirectory != null) return WithSeparator(verificationDesktopDirectory);
            if (string.IsNullOrEmpty(playerRoot) || !Path.IsPathFullyQualified(playerRoot))
                throw new ArgumentException("DreamOS data needs an absolute player data root; refusing to fall back to the working directory.");
            string directory = Path.Combine(playerRoot, SubFolder);
            if (!string.IsNullOrEmpty(profileId))
            {
                if (profileId == "." || profileId == ".." || profileId != profileId.TrimEnd(' ', '.') ||
                    profileId.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 || profileId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    throw new ArgumentException("DreamOS profileID must be a single folder name: " + profileId);
                directory = Path.Combine(directory, profileId);
            }
            return WithSeparator(Path.GetFullPath(directory));
        }

        static string WithSeparator(string path) => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }
}
