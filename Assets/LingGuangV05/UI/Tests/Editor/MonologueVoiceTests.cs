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

        static float PlayRecording(string text)
        {
            var useBlips = Voice.GetField("UseBlips");
            object was = useBlips.GetValue(null);
            useBlips.SetValue(null, false);
            try { return (float)Voice.GetMethod("Play").Invoke(null, new object[] { text, null }); }
            finally { useBlips.SetValue(null, was); }
        }

        // 2026-10-19: the protagonist speaks in BlipTalk babble; the recordings (Voice/me) and the VoiceStudio takes
        // (Voice/generated) left Resources for VoiceArchive and no longer ship with the game.
        [Test] public void VoiceRecordingsDoNotShip()
        {
            Assert.IsNull(Resources.Load<TextAsset>("LingGuangV05/Voice/voicestudio_manifest"));
            Assert.IsNull(Find("……就这么走了？"));
            Assert.IsNull(Find("手腕好酸。"));
            Assert.AreEqual(0f, PlayRecording("手腕好酸。"));
        }
        [Test] public void ProtagonistSpeaksInBlips()
        {
            Assert.IsTrue((bool)Voice.GetField("UseBlips").GetValue(null), "BlipTalk babble is the protagonist's voice");
            var blip = Type.GetType("LingGuangV05.Desktop.Story.BlipTalk, Assembly-CSharp", true);
            object me = blip.GetField("Me").GetValue(null);
            foreach (string text in new[] { "手腕好酸。", "My wrist aches." })
            {
                object line = blip.GetMethod("Render").Invoke(null, new[] { text, me });
                Assert.IsNotNull(line, text);
                var clip = (AudioClip)line.GetType().GetField("clip").GetValue(line);
                var reveal = (float[])line.GetType().GetField("reveal").GetValue(line);
                Assert.IsNotNull(clip, text);
                Assert.Greater(clip.length, .2f, text);
                Assert.AreEqual(text.Length, reveal.Length, text + ": one reveal time per character");
                UnityEngine.Object.DestroyImmediate(clip);
            }
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
                Assert.AreEqual(0f, PlayRecording("手腕好酸。"));
        }
    }
}
