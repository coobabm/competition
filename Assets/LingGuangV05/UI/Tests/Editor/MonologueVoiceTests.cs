using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.UI.Tests
{
    public class MonologueVoiceTests
    {
        static Type Voice => Type.GetType("LingGuangV05.Desktop.Story.VoiceLines, Assembly-CSharp", true);
        static AudioClip Find(string text) => (AudioClip)Voice.GetMethod("Find").Invoke(null, new object[] { text });

        // 2026-10-19: the protagonist speaks only through written thoughts, so the recordings (Voice/me) and the
        // VoiceStudio takes (Voice/generated) left Resources for VoiceArchive and no longer ship with the game.
        [Test] public void VoiceRecordingsDoNotShip()
        {
            Assert.IsNull(Resources.Load<TextAsset>("LingGuangV05/Voice/voicestudio_manifest"));
            Assert.IsNull(Find("……就这么走了？"));
            Assert.IsNull(Find("手腕好酸。"));
            Assert.AreEqual(0f, Voice.GetMethod("Play").Invoke(null, new object[] { "手腕好酸。", null }));
        }
        [Test] public void MissingAndSilenceOnlyLinesRemainSilent()
        {
            Assert.IsNull(Find(null));
            Assert.IsNull(Find("……"));
            Assert.IsNull(Find("This is not a recorded monologue"));
            Assert.IsNull(Find("根本不属于游戏的另一句话"));
        }
        [Test] public void EnglishModeDoesNotPlayChineseVoice()
        {
            using (LingGuangV05.Runtime.GameText.Bind(() => "en-US"))
                Assert.AreEqual(0f, Voice.GetMethod("Play").Invoke(null, new object[] { "手腕好酸。", null }));
        }
    }
}
