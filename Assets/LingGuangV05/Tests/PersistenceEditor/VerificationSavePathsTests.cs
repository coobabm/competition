using System;
using System.IO;
using NUnit.Framework;
using LingGuangV05.Runtime.Persistence;

namespace LingGuangV05.Tests
{
    public sealed class VerificationSavePathsTests
    {
        string owned, protectedRoot;
        [SetUp] public void CreateOwnedFixture()
        {
            owned = Path.Combine(Path.GetTempPath(), "LingGuang-VerificationPaths-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(owned);protectedRoot=Path.Combine(owned,"Player");Directory.CreateDirectory(protectedRoot);
            File.WriteAllText(Path.Combine(protectedRoot,"sentinel.txt"),"untouched");
        }
        [TearDown] public void RemoveOwnedFixture()
        {
            Assert.AreEqual("untouched",File.ReadAllText(Path.Combine(protectedRoot,"sentinel.txt")));
            string temp=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            Assert.True(Path.GetFullPath(owned).StartsWith(temp,StringComparison.OrdinalIgnoreCase));
            Assert.True(Path.GetFileName(owned).StartsWith("LingGuang-VerificationPaths-",StringComparison.Ordinal));
            Directory.Delete(owned,true);
        }
        string Prepare(params string[] args)=>VerificationSavePaths.Prepare(args,new[]{protectedRoot});
        [Test] public void NoOptionDoesNotCreateVerificationOrOpenProtectedData()
        {Assert.IsNull(Prepare("player.exe","-screen-fullscreen","0"));Assert.AreEqual(1,Directory.GetDirectories(owned).Length);}
        [Test] public void NewDirectoryMarkerAndReopenUseExactlySameStoreAndProfileRoot()
        {
            string path=Path.Combine(owned,"Run");Assert.AreEqual(path,Prepare(VerificationSavePaths.Option,path));
            var store=new AtomicSaveStore(path);Assert.True(store.TrySave("owned test payload",out var error),error);
            var profile=new PlayerProfileStore(store);Assert.AreEqual(Path.Combine(path,PlayerProfileStore.FileName),profile.Path);
            Assert.AreEqual(path,Prepare(VerificationSavePaths.Option,path));Assert.True(File.Exists(store.SavePath));
        }
        [Test] public void ExistingEmptyDirectoryIsAccepted()
        {string path=Path.Combine(owned,"Empty");Directory.CreateDirectory(path);Assert.AreEqual(path,Prepare(VerificationSavePaths.Option,path));}
        [Test] public void NonemptyUnmarkedDirectoryIsNeverAdopted()
        {string path=Path.Combine(owned,"Other");Directory.CreateDirectory(path);File.WriteAllText(Path.Combine(path,"data"),"keep");Assert.Throws<InvalidDataException>(()=>Prepare(VerificationSavePaths.Option,path));Assert.AreEqual("keep",File.ReadAllText(Path.Combine(path,"data")));}
        [TestCase("")][TestCase("relative/run")][TestCase("C:relative")][TestCase("\\\\server\\share\\run")]
        public void NonAbsoluteOrMissingValueThrowsInsteadOfReturningNull(string path)
        {Assert.Throws<ArgumentException>(()=>Prepare(VerificationSavePaths.Option,path));}
        [Test] public void MissingDuplicateAndEqualsOptionsAreRejected()
        {
            string path=Path.Combine(owned,"Run");Assert.Throws<ArgumentException>(()=>Prepare(VerificationSavePaths.Option));
            Assert.Throws<ArgumentException>(()=>Prepare(VerificationSavePaths.Option,path,VerificationSavePaths.Option,path));
            Assert.Throws<ArgumentException>(()=>Prepare(VerificationSavePaths.Option+"="+path));Assert.False(Directory.Exists(path));
        }
        [Test] public void ProtectedRootItsAncestorAndItsChildAreRejectedEvenWithMarker()
        {
            foreach(string path in new[]{protectedRoot,owned,Path.Combine(protectedRoot,"Run")})
                Assert.Throws<ArgumentException>(()=>Prepare(VerificationSavePaths.Option,path));
            Assert.False(File.Exists(Path.Combine(protectedRoot,VerificationSavePaths.MarkerName)));
        }
        [Test] public void WrongOrCopiedMarkerDoesNotAdoptDirectory()
        {
            string a=Path.Combine(owned,"A"),b=Path.Combine(owned,"B");Prepare(VerificationSavePaths.Option,a);Directory.CreateDirectory(b);
            File.Copy(Path.Combine(a,VerificationSavePaths.MarkerName),Path.Combine(b,VerificationSavePaths.MarkerName));
            Assert.Throws<InvalidDataException>(()=>Prepare(VerificationSavePaths.Option,b));
        }
        [Test] public void CreationFailureCannotReturnNormalSaveFallback()
        {
            string blocker=Path.Combine(owned,"File");File.WriteAllText(blocker,"keep");
            Assert.Catch<IOException>(()=>Prepare(VerificationSavePaths.Option,Path.Combine(blocker,"Run")));
            Assert.AreEqual("keep",File.ReadAllText(blocker));
        }
        [Test] public void PathAliasesAndDriveRootAreRejected()
        {
            foreach(string path in new[]{Path.GetPathRoot(owned),Path.Combine(owned,"Run."),Path.Combine(owned,"Run "),Path.Combine(owned,"RUN~1"),Path.Combine(owned,"Run:stream")})
                Assert.Throws<ArgumentException>(()=>Prepare(VerificationSavePaths.Option,path));
        }
        [Test] public void AnOrdinaryFileCannotBecomeTheVerificationRoot()
        {string path=Path.Combine(owned,"File");File.WriteAllText(path,"keep");Assert.Throws<IOException>(()=>Prepare(VerificationSavePaths.Option,path));Assert.AreEqual("keep",File.ReadAllText(path));}
    }
}
