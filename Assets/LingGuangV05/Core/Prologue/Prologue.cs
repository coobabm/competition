using System;
using System.Collections.Generic;
using LingGuangV05.Core.Story;

namespace LingGuangV05.Core
{
    /// <summary>Fixed facts of the prologue (design v1.1 §6, §1.4).</summary>
    public static class Prologue
    {
        /// <summary>
        /// The long number the SI types into the address bar (§6 Step 2). The ending decodes it (§8 E-1, E-2):
        /// two digits per letter, a = 01 … z = 26, it spells "haineifengzhiji", 海内逢知己.
        /// </summary>
        public const string LongNumber = "080109140509060514072608091009";
        public const string Txt = "0.txt";
        public const string Jpg = "我的显卡在哪里.jpg";
        public const string Rar = "我的显卡在哪里.rar";
        public const string Dll = "sophon.dll";
        public const double JpgMegabytes = 46.8;

        /// <summary>Decodes a two-digits-per-letter number (01 = a … 26 = z); null if it is not one.</summary>
        public static string Decode(string digits)
        {
            if (string.IsNullOrEmpty(digits) || digits.Length % 2 != 0) return null;
            var chars = new char[digits.Length / 2];
            for (int i = 0; i < chars.Length; i++)
            {
                if (!int.TryParse(digits.Substring(i * 2, 2), out int n) || n < 1 || n > 26) return null;
                chars[i] = (char)('a' + n - 1);
            }
            return new string(chars);
        }

        /// <summary>sophon.dll grows as the game goes on (§1.4): bytes for a lab stage and the cards trained so far.</summary>
        public static long SophonBytes(int stage, long cards) => 4096 + Math.Max(0, stage) * 65536L + Math.Max(0, cards) * 12L;

        /// <summary>Lines the prologue reads; content tests check every one exists in both languages.</summary>
        public static readonly string[] Keys =
        {
            "title", "title_sub", "boot", "who_360", "bubble_boot", "virus_title", "virus_head", "virus_body", "m_virus", "m_dodge_1", "m_dodge_2", "m_dodge_3",
            "virus_ignored", "m_ignored", "m_txt", "browser", "downloading", "m_hijack", "bubble_safe", "m_grab",
            "dialog_title", "dialog_shutdown", "yes", "no", "notepad", "txt_bios", "m_who", "shutting_down", "m_reboot", "m_reboot_exe", "rename", "open", "delete", "properties",
            "rar_trial_title", "rar_trial", "buy", "close", "extract", "txt_poem", "m_poem", "m_left",
            "lz_first", "lz_pick_odd", "lz_pick_yes", "lz_pick_later", "lz_odd_1", "lz_odd_2", "lz_yes_1", "lz_later_1", "lz_game_2",
            "recycle", "recycle_empty", "readonly", "hidden", "prop_type", "prop_size", "prop_desc", "prop_location", "desktop_path",
            "type_exe", "type_dll", "type_txt", "ok", "setup_title", "setup_name", "setup_self", "setup_call", "setup_mood", "setup_mood_hint",
            "setup_custom", "setup_done", "setup_reading", "no_intelligence",
        };
    }

    /// <summary>An old chat line with 老周 from before the game (April–May 2016).</summary>
    public sealed class PrologueHistoryLine
    {
        public int days;
        public string from = "", zh = "", en = "";
    }

    /// <summary>prologue_lines.json: zh / en pairs and the old 老周 chat.</summary>
    public sealed class PrologueLines
    {
        readonly Dictionary<string, string[]> lines = new Dictionary<string, string[]>();
        public readonly List<PrologueHistoryLine> History = new List<PrologueHistoryLine>();
        public IEnumerable<string> Keys => lines.Keys;

        public static PrologueLines Parse(string json)
        {
            var result = new PrologueLines();
            var root = StoryJson.Parse(json) as Dictionary<string, object>;
            if (root == null || !(root.TryGetValue("lines", out var l) && l is Dictionary<string, object> map)) throw new FormatException("prologue_lines: no lines");
            foreach (var kv in map)
            {
                var pair = kv.Value as Dictionary<string, object>;
                string zh = pair != null && pair.TryGetValue("zh", out var z) ? z as string : null, en = pair != null && pair.TryGetValue("en", out var e) ? e as string : null;
                if (string.IsNullOrEmpty(zh) || string.IsNullOrEmpty(en)) throw new FormatException("prologue_lines: " + kv.Key + " needs zh and en");
                result.lines[kv.Key] = new[] { zh, en };
            }
            if (root.TryGetValue("history", out var h) && h is List<object> items)
                foreach (var item in items)
                {
                    var o = item as Dictionary<string, object>;
                    if (o == null) continue;
                    var line = new PrologueHistoryLine
                    {
                        days = o.TryGetValue("days", out var d) && d is double n ? (int)n : 0,
                        from = o.TryGetValue("from", out var f) ? f as string ?? "" : "",
                        zh = o.TryGetValue("zh", out var hz) ? hz as string ?? "" : "", en = o.TryGetValue("en", out var he) ? he as string ?? "" : "",
                    };
                    if (line.days >= 0 || line.zh.Length == 0 || line.en.Length == 0) throw new FormatException("prologue_lines: history lines are dated before the game, in both languages");
                    result.History.Add(line);
                }
            return result;
        }

        public bool Has(string key) => lines.ContainsKey(key);
        public string Get(string key, bool english) => lines.TryGetValue(key, out var pair) ? pair[english ? 1 : 0] : key;
    }
}
