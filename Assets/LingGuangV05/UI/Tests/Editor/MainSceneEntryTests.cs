using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.UI.Tests
{
    public sealed class MainSceneEntryTests
    {
        const string EntryTypeName = "LingGuangV05.Desktop.MainSceneEntry, Assembly-CSharp";

        [Test]
        public void MenuHasAnExplicitMainSceneEntryWithoutReflectionInProduction()
        {
            var type = Type.GetType(EntryTypeName);
            Assert.That(type, Is.Not.Null, "Loading Main after a menu needs an explicit startup entry.");
            var entry = type.GetMethod("EnsureInitialized", BindingFlags.Public | BindingFlags.Static);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.ReturnType, Is.EqualTo(typeof(bool)));
            Assert.That(entry.GetParameters(), Is.Empty);
        }

        [Test]
        public void MainEntryDoesNotInitializeOrTouchSavesInEditMode()
        {
            var type = Type.GetType(EntryTypeName);
            Assert.That(type, Is.Not.Null);
            var entry = type.GetMethod("EnsureInitialized", BindingFlags.Public | BindingFlags.Static);
            Assert.That(entry, Is.Not.Null);
            Assert.That(Application.isPlaying, Is.False);
            Assert.That(entry.Invoke(null, null), Is.False);
        }

        [TestCase("LingGuangV05.Desktop.Story.StoryBootstrap")]
        [TestCase("LingGuangV05.Desktop.XingGuang.XingGuangController")]
        [TestCase("LingGuangV05.Desktop.YY.YYChatView")]
        [TestCase("LingGuangV05.Desktop.YY.YYGirlfriend")]
        [TestCase("LingGuangV05.Desktop.Mail.MailInboxView")]
        [TestCase("LingGuangV05.Desktop.Juxin.JuxinHub")]
        [TestCase("LingGuangV05.Desktop.XingGuang.XgMarketRelay")]
        [TestCase("LingGuangV05.Desktop.XingGuang.XgAfterthoughtRelay")]
        [TestCase("LingGuangV05.Desktop.LLM.LocalLlm")]
        public void ExplicitInitializersKeepTheirOriginalDirectMainStartup(string typeName)
        {
            var type = Type.GetType(typeName + ", Assembly-CSharp");
            Assert.That(type, Is.Not.Null, typeName);
            var attach = type.GetMethod("Attach", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(attach, Is.Not.Null, typeName);
            Assert.That(attach.IsAssembly, Is.True, "Startup must be callable by MainSceneEntry in the same assembly.");
            var attribute = attach.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
            Assert.That(attribute, Is.Not.Null, "Direct Main play must retain the original startup path.");
            Assert.That(attribute.loadType, Is.EqualTo(RuntimeInitializeLoadType.AfterSceneLoad));
        }
    }
}
