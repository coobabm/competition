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

        [Test] public void ExistingTakesStillMatchRichTextAndPunctuation()
        {
            var clip = Find("……就这么走了？");
            Assert.NotNull(clip);
            Assert.AreSame(clip, Find("<b>就这么走了</b>!"));
        }
        [Test] public void MissingAndSilenceOnlyLinesRemainSilent()
        {
            Assert.IsNull(Find(null));
            Assert.IsNull(Find("……"));
            Assert.IsNull(Find("This is not a recorded monologue"));
            Assert.IsNull(Find("根本不属于游戏的另一句话"));
        }
        [Test] public void NewlyVoicedMonologueHasOfflineClip()
        {
            var clip = Find("手腕好酸。");
            Assert.NotNull(clip, "The former subtitle-only monologue needs an offline recording.");
            Assert.Greater(clip.length, .2f);
        }
        [Test] public void DynamicClockUsesFixedPortionWithoutReadingFalseTime()
        {
            var first = Find("23:45 了，明天还要上班。");
            Assert.NotNull(first);
            Assert.AreSame(first, Find("1:02 了，明天还要上班。"));
            Assert.IsNull(Find("23:45 了，明天不用上班。"));
        }
        [Test] public void QuotedDynamicSpeechUsesOnlyItsAuthoredPrefix()
        {
            Assert.AreSame(Find("她说。"), Find("她说：『今天有点冷』"));
            Assert.NotNull(Find("她说：『今天有点冷』"));
            Assert.IsNull(Find("他说：『今天有点冷』"));
        }
        [Test] public void EnglishModeDoesNotPlayChineseVoice()
        {
            using (LingGuangV05.Runtime.GameText.Bind(() => "en-US"))
                Assert.AreEqual(0f, Voice.GetMethod("Play").Invoke(null, new object[] { "手腕好酸。" }));
        }
        [Serializable] public class Entry { public string text, clip; }
        [Serializable] public class Portion { public string template, clip, spoken; }
        [Serializable] public class Manifest { public Entry[] entries; public Portion[] portions; }
        [Test] public void EveryManifestEntryResolvesToImportedAudio()
        {
            var asset = Resources.Load<TextAsset>("LingGuangV05/Voice/voicestudio_manifest");
            Assert.NotNull(asset);
            var manifest = JsonUtility.FromJson<Manifest>(asset.text);
            Assert.Greater(manifest.entries.Length, 100);
            foreach (var entry in manifest.entries)
            {
                var clip = Find(entry.text);
                Assert.NotNull(clip, entry.text);
                Assert.Greater(clip.length, .12f, entry.text);
            }
            foreach (var portion in manifest.portions)
            {
                if (string.IsNullOrEmpty(portion.clip)) continue;
                string sample = System.Text.RegularExpressions.Regex.Replace(portion.template, @"\{[^{}]+\}", "12");
                Assert.NotNull(Find(sample), portion.template);
            }
        }
    }
}
