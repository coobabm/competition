using System.Collections.Generic;
using System.Text;
using LingGuangV05.Runtime;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Recorded voice for the protagonist's own lines (Chinese only). A line is matched by its Chinese text, ignoring
    /// punctuation, spaces and rich-text tags, so the prologue thoughts and the inner voice need no keys: when a line
    /// with a recording appears, its clip plays. Clips live in Resources/LingGuangV05/Voice/me.
    /// </summary>
    public static class VoiceLines
    {
        const string Folder = "LingGuangV05/Voice/me/";

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
            if (byKey == null)
            {
                byKey = new Dictionary<string, string>();
                foreach (var kv in Lines) byKey[Key(kv.Key)] = kv.Value;
            }
            return byKey.TryGetValue(Key(zh), out var name) ? Resources.Load<AudioClip>(Folder + name) : null;
        }

        /// <summary>
        /// Plays the recording for a line if the game is in Chinese and one exists. Returns its length in seconds
        /// (0 when nothing plays), so the caller can keep the subtitle up while it is spoken.
        /// </summary>
        public static float Play(string zh)
        {
            if (GameText.IsEnglish) return 0;
            var clip = Find(zh);
            if (clip == null) return 0;
            if (source == null)
            {
                var go = new GameObject("LingGuang Voice Lines");
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0;
            }
            source.Stop();
            source.clip = clip;
            source.Play();
            return clip.length;
        }

        /// <summary>Stops a line cut off by a cutscene or a skip.</summary>
        public static void Stop()
        {
            if (source != null) source.Stop();
        }
    }
}
