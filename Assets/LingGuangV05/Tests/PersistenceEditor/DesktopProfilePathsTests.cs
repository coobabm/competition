using System;
using System.IO;
using NUnit.Framework;
using LingGuangV05.Runtime.Persistence;

namespace LingGuangV05.Tests
{
    /// <summary>LINGGUANG PATCH D7: pure path tests for DreamOS desktop data. No file or directory is created.</summary>
    public sealed class DesktopProfilePathsTests
    {
        static readonly string Sep = Path.DirectorySeparatorChar.ToString();
        // Shaped like a Player persistentDataPath (LocalLow/<company>/<product>); never created on disk.
        static readonly string PlayerRoot = Path.Combine(Path.GetTempPath(), "LingGuang-D7-NotCreated", "LocalLow", "DefaultCompany", "competition");

        [Test] public void DefaultProfileLivesUnderPlayerRootNotTheGameFolder()
        {
            string path = DesktopProfilePaths.Resolve(null, PlayerRoot, null);
            Assert.AreEqual(Path.GetFullPath(Path.Combine(PlayerRoot, "DreamOS_UserData")) + Sep, path);
            StringAssert.DoesNotContain("_DataDreamOS_UserData", path);
            Assert.AreEqual(path, DesktopProfilePaths.Resolve(null, PlayerRoot, ""), "Empty profileID is the same default profile.");
        }

        [Test] public void ResultEndsWithSeparatorSoConcatenatedFilesStayInside()
        {
            string path = DesktopProfilePaths.Resolve(null, PlayerRoot + Sep, null);
            StringAssert.EndsWith(Sep, path);
            Assert.False(path.EndsWith(Sep + Sep, StringComparison.Ordinal));
            Assert.AreEqual(path.TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(path + "Network.json"));
        }

        [Test] public void ProfileIdIsOneSubfolder()
        {
            string path = DesktopProfilePaths.Resolve(null, PlayerRoot, "Profile2");
            Assert.AreEqual(Path.GetFullPath(Path.Combine(PlayerRoot, "DreamOS_UserData", "Profile2")) + Sep, path);
        }

        [TestCase("..")] [TestCase(".")] [TestCase("a/b")] [TestCase("a\\b")] [TestCase("C:x")] [TestCase(".. ")] [TestCase("profile.")]
        public void ProfileIdCannotEscapeTheDataFolder(string profileId)
        { Assert.Throws<ArgumentException>(() => DesktopProfilePaths.Resolve(null, PlayerRoot, profileId)); }

        [Test] public void VerificationDirectoryWinsAndIgnoresPlayerRootAndProfile()
        {
            string verification = Path.Combine(Path.GetTempPath(), "LingGuang-D7-NotCreated", "Run", "DesktopProfile");
            Assert.AreEqual(verification + Sep, DesktopProfilePaths.Resolve(verification, PlayerRoot, "Profile2"));
            Assert.AreEqual(verification + Sep, DesktopProfilePaths.Resolve(verification + Sep, null, null), "No player root is consulted while verifying.");
        }

        [TestCase(null)] [TestCase("")] [TestCase("relative/root")] [TestCase("C:relative")]
        public void MissingOrRelativePlayerRootThrowsInsteadOfUsingWorkingDirectory(string root)
        { Assert.Throws<ArgumentException>(() => DesktopProfilePaths.Resolve(null, root, null)); }
    }
}
