using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using LingGuangV05.Runtime;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// The protagonist's voice. Every line is "spoken" in BlipTalk babble (<see cref="BlipTalk"/>), in either language, and
    /// the caller types the text along <see cref="Revealed"/>. The old recordings (matched by Chinese text, ignoring
    /// punctuation, spaces and rich-text tags) moved to VoiceArchive on 2026-10-19 and no longer ship, so <see cref="Find"/>
    /// only finds a clip if one is put back under Resources/LingGuangV05/Voice.
    /// </summary>
    public static class VoiceLines
    {
        const string Folder = "LingGuangV05/Voice/";
        /// <summary>True: BlipTalk babble for every line (the default since 2026-10-18). False: a recording if one exists.</summary>
        public static bool UseBlips = true;
        static AudioClip blipClip;
        static float[] timeline;

        /// <summary>
        /// How many characters of the line being spoken have been said <paramref name="elapsed"/> seconds after
        /// <see cref="Play"/>, or -1 when the line has no timeline (a recording, or nothing) and the caller types at its own
        /// speed. Past the end it is every character.
        /// </summary>
        public static int Revealed(float elapsed)
        {
            if (timeline == null) return -1;
            int n = 0;
            while (n < timeline.Length && timeline[n] <= elapsed) n++;
            return n < timeline.Length ? n : 99999;
        }

        /// <summary>Chinese line → clip name. Keep the text identical to the line the game shows.</summary>
        static readonly Dictionary<string, string> Lines = new Dictionary<string, string>
        {
            { "我去，什么玩意儿在动我电脑？不会中病毒了吧？！", "m_hijack" },
            { "怎么鼠标动不了啊！这是灰鸽子？现在病毒这么强吗？得赶紧关机！", "m_grab" },
            { "？！你是谁？什么玩意儿？怎么能听见我说话？窃听我麦克风了？……等等，“别关机”——这不是今年那个勒索病毒的套路吗？", "m_who" },
            { "图种？这玩意儿还有人用？……还有，不应该是“海内存知己”吗？这人儿语文咋学的……", "m_poem" },
            { "……就这么走了？", "m_left" },
            { "我点的明明是「是」啊！", "m_dodge_3" },
            { "……算了。360 天天报毒，八成又是误报。", "m_ignored" },
            { "……它在挣钱。我什么都没点。", "income_1" },
            { "躺着也能挣钱？", "income_2" },
            { "……也没说错。", "bot_2" },
            { "……它跟谁学的这个。", "yy_learn_1" },
            { "哦，跟我。", "yy_learn_2" },
        };

        static Dictionary<string, string> byKey;
        static AudioSource source;
        static readonly List<PartialLine> partialLines = new List<PartialLine>();

        [Serializable] sealed class Manifest { public Entry[] entries; public Portion[] portions; }
        [Serializable] sealed class Entry { public string text, clip; }
        [Serializable] sealed class Portion { public string template, clip; }
        sealed class PartialLine { public Regex pattern; public string clip; }

        static bool SafeClip(string name) => name != null &&
            Regex.IsMatch(name, @"\A(?:me|generated)/[a-zA-Z0-9_-]+\z");

        static void LoadManifest()
        {
            byKey = new Dictionary<string, string>();
            partialLines.Clear();
            foreach (var kv in Lines) byKey[Key(kv.Key)] = "me/" + kv.Value;
            var asset = Resources.Load<TextAsset>(Folder + "voicestudio_manifest");
            if (asset == null) return; // Older builds keep their original recordings.
            var manifest = JsonUtility.FromJson<Manifest>(asset.text);
            if (manifest == null) return;
            if (manifest.entries != null)
                foreach (var entry in manifest.entries)
                {
                    if (entry == null || !SafeClip(entry.clip)) continue;
                    string key = Key(entry.text);
                    if (key.Length > 0) byKey[key] = entry.clip;
                }
            if (manifest.portions == null) return;
            foreach (var portion in manifest.portions)
            {
                if (portion == null || !SafeClip(portion.clip) || string.IsNullOrEmpty(portion.template)) continue;
                var pattern = new StringBuilder(@"\A");
                int literalLength = 0;
                foreach (string part in Regex.Split(portion.template, @"(\{[^{}]+\})"))
                {
                    if (part.StartsWith("{", StringComparison.Ordinal)) pattern.Append(@".{0,128}?");
                    else { string literal = Key(part); literalLength += literal.Length; pattern.Append(Regex.Escape(literal)); }
                }
                if (literalLength < 2) continue; // Allow authored "她说" but never a variable-only catch-all.
                pattern.Append(@"\z");
                partialLines.Add(new PartialLine {
                    pattern = new Regex(pattern.ToString(), RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(25)),
                    clip = portion.clip
                });
            }
        }

        static string Key(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var sb = new StringBuilder(text.Length);
            bool tag = false;
            foreach (char c in text)
            {
                if (c == '<') { tag = true; continue; }
                if (c == '>') { tag = false; continue; }
                if (tag) continue;
                // Letters, digits and CJK only: punctuation and spacing differences never break a match.
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>The recording for this Chinese line, or null.</summary>
        public static AudioClip Find(string zh)
        {
            if (byKey == null) LoadManifest();
            string key = Key(zh);
            if (key.Length == 0 || key.Length > 2048) return null;
            if (byKey.TryGetValue(key, out var name)) return Resources.Load<AudioClip>(Folder + name);
            foreach (var line in partialLines)
            {
                try { if (line.pattern.IsMatch(key)) return Resources.Load<AudioClip>(Folder + line.clip); }
                catch (RegexMatchTimeoutException) { /* Player-provided names must not stall the frame. */ }
            }
            return null;
        }

        /// <summary>
        /// Speaks a line: in BlipTalk babble for <paramref name="shown"/> (the text on screen) when <see cref="UseBlips"/>,
        /// otherwise the recording if the game is in Chinese and one exists. Returns its length in seconds (0 when nothing
        /// plays), so the caller can keep the subtitle up while it is spoken; with babble the caller types the text along
        /// <see cref="Revealed"/>.
        /// </summary>
        public static float Play(string zh, string shown = null)
        {
            AudioClip clip;
            timeline = null;
            if (UseBlips)
            {
                Stop();
                if (blipClip != null) UnityEngine.Object.Destroy(blipClip);
                var line = BlipTalk.Render(shown ?? zh, BlipTalk.Me);
                clip = blipClip = line?.clip;
                timeline = line?.reveal;
            }
            else clip = GameText.IsEnglish ? null : Find(zh);
            if (clip == null) { Stop(); return 0; }
            if (source == null)
            {
                var go = new GameObject("LingGuang Voice Lines");
                UnityEngine.Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0;
            }
            source.Stop();
            source.clip = clip;
            source.Play();
            return clip.length;
        }

        /// <summary>A recorded line is being spoken (the background music steps back for it).</summary>
        public static bool Speaking => source != null && source.isPlaying;

        /// <summary>Stops a line cut off by a cutscene or a skip.</summary>
        public static void Stop()
        {
            if (source != null) source.Stop();
        }
    }
}
