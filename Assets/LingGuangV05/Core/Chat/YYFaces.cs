using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.Core.Chat
{
    /// <summary>
    /// One YY emoticon. Messages store the Chinese bracket code (for example "[微笑]"); the English UI shows the
    /// same drawn face and uses <see cref="EnAlias"/> wherever a face has to be written out as text.
    /// </summary>
    public sealed class YYFace
    {
        /// <summary>Position in the catalog; also the cell index in the runtime atlas.</summary>
        public readonly int Index;
        /// <summary>The name inside the brackets, e.g. "微笑".</summary>
        public readonly string Zh;
        /// <summary>Human-readable English name, e.g. "Thumbs up".</summary>
        public readonly string En;
        /// <summary>ASCII identifier, used as the sprite name, e.g. "thumbsup".</summary>
        public readonly string Id;

        /// <summary>The stored bracket code, e.g. "[微笑]".</summary>
        public string Code => "[" + Zh + "]";
        /// <summary>The bracket code shown in English text, e.g. "[ThumbsUp]".</summary>
        public string EnAlias => "[" + Alias + "]";
        /// <summary>The English alias without brackets, e.g. "ThumbsUp".</summary>
        public readonly string Alias;

        internal YYFace(int index, string zh, string en, string alias)
        {
            Index = index; Zh = zh; En = en; Alias = alias; Id = alias.ToLowerInvariant();
        }

        public string Name(bool english) => english ? En : Zh;
        public override string ToString() => Code;
    }

    /// <summary>A run of a parsed chat message: either literal text or one face.</summary>
    public readonly struct YYRun
    {
        public readonly string Text;
        public readonly YYFace Face;
        public bool IsFace => Face != null;

        public YYRun(string text) { Text = text ?? ""; Face = null; }
        public YYRun(YYFace face) { Face = face; Text = face.Code; }
        public override string ToString() => IsFace ? Face.Code : Text;
    }

    /// <summary>
    /// The single source of truth for YY bracket faces. The renderer, the picker, the girlfriend's prompt
    /// whitelist and her GBNF grammar all read this list. A bracketed token that is not in the list
    /// (for example "[图片]" or "[xxx]") is ordinary text everywhere.
    /// </summary>
    public static class YYFaces
    {
        /// <summary>The longest name inside the brackets ("快哭了" has three characters).</summary>
        public const int MaxNameLength = 12;

        static readonly YYFace[] all;
        static readonly Dictionary<string, YYFace> byName = new Dictionary<string, YYFace>(StringComparer.Ordinal);
        static readonly Dictionary<string, YYFace> byAlias = new Dictionary<string, YYFace>(StringComparer.OrdinalIgnoreCase);

        static YYFaces()
        {
            string[,] table =
            {
                { "微笑", "Smile", "Smile" },
                { "撇嘴", "Grimace", "Grimace" },
                { "色", "Lovestruck", "Lovestruck" },
                { "发呆", "Dazed", "Dazed" },
                { "得意", "Smug", "Smug" },
                { "流泪", "Tears", "Tears" },
                { "害羞", "Shy", "Shy" },
                { "闭嘴", "Zip it", "ZipIt" },
                { "睡", "Asleep", "Asleep" },
                { "大哭", "Sob", "Sob" },
                { "尴尬", "Awkward", "Awkward" },
                { "发怒", "Angry", "Angry" },
                { "调皮", "Cheeky", "Cheeky" },
                { "呲牙", "Grin", "Grin" },
                { "惊讶", "Surprised", "Surprised" },
                { "难过", "Sad", "Sad" },
                { "抠鼻", "Nose pick", "NosePick" },
                { "再见", "Bye", "Bye" },
                { "偷笑", "Giggle", "Giggle" },
                { "可爱", "Cute", "Cute" },
                { "白眼", "Eye roll", "EyeRoll" },
                { "傲慢", "Haughty", "Haughty" },
                { "困", "Sleepy", "Sleepy" },
                { "惊恐", "Terrified", "Terrified" },
                { "流汗", "Sweat", "Sweat" },
                { "憨笑", "Goofy grin", "Goofy" },
                { "奋斗", "Fighting", "Fighting" },
                { "疑问", "Puzzled", "Puzzled" },
                { "嘘", "Shush", "Shush" },
                { "晕", "Dizzy", "Dizzy" },
                { "衰", "Doomed", "Doomed" },
                { "敲打", "Bonk", "Bonk" },
                { "擦汗", "Phew", "Phew" },
                { "鼓掌", "Clap", "Clap" },
                { "坏笑", "Smirk", "Smirk" },
                { "哈欠", "Yawn", "Yawn" },
                { "鄙视", "Scorn", "Scorn" },
                { "委屈", "Wronged", "Wronged" },
                { "快哭了", "About to cry", "AboutToCry" },
                { "阴险", "Sly", "Sly" },
                { "亲亲", "Kiss", "Kiss" },
                { "可怜", "Puppy eyes", "PuppyEyes" },
                { "玫瑰", "Rose", "Rose" },
                { "爱心", "Heart", "Heart" },
                { "心碎", "Heartbreak", "Heartbreak" },
                { "拥抱", "Hug", "Hug" },
                { "强", "Thumbs up", "ThumbsUp" },
                { "弱", "Thumbs down", "ThumbsDown" },
                { "握手", "Handshake", "Handshake" },
                { "胜利", "Victory", "Victory" },
                { "OK", "OK", "OK" },
                { "月亮", "Moon", "Moon" },
                { "太阳", "Sun", "Sun" },
            };
            int n = table.GetLength(0);
            all = new YYFace[n];
            for (int i = 0; i < n; i++)
            {
                var face = new YYFace(i, table[i, 0], table[i, 1], table[i, 2]);
                all[i] = face;
                byName[face.Zh] = face;
                byAlias[face.Alias] = face;
            }
        }

        /// <summary>Every face in picker order.</summary>
        public static IReadOnlyList<YYFace> All => all;
        public static int Count => all.Length;

        /// <summary>
        /// Looks a face up by bracket code or bare name, Chinese or English alias:
        /// "[微笑]", "微笑", "[Smile]", "smile" all find the same face. Returns null when unknown.
        /// </summary>
        public static YYFace Find(string codeOrName)
        {
            if (string.IsNullOrEmpty(codeOrName)) return null;
            string name = codeOrName.Trim();
            if (name.Length >= 2 && name[0] == '[' && name[name.Length - 1] == ']') name = name.Substring(1, name.Length - 2);
            if (name.Length == 0) return null;
            if (byName.TryGetValue(name, out var face)) return face;
            return byAlias.TryGetValue(name, out face) ? face : null;
        }

        public static YYFace ById(string id) => string.IsNullOrEmpty(id) ? null : (byAlias.TryGetValue(id, out var f) ? f : null);
        public static YYFace At(int index) => index >= 0 && index < all.Length ? all[index] : null;

        /// <summary>True when the bracket code (or bare name) is a known face.</summary>
        public static bool IsKnown(string codeOrName) => Find(codeOrName) != null;

        /// <summary>
        /// Splits a message into text and face runs. Known Chinese codes and English aliases become faces;
        /// unknown bracketed tokens stay literal text. Adjacent text is merged into one run.
        /// </summary>
        public static List<YYRun> Parse(string message)
        {
            var runs = new List<YYRun>();
            if (string.IsNullOrEmpty(message)) return runs;
            var text = new StringBuilder();
            int i = 0;
            while (i < message.Length)
            {
                if (message[i] == '[' && TryFaceAt(message, i, out var face, out int length))
                {
                    if (text.Length > 0) { runs.Add(new YYRun(text.ToString())); text.Clear(); }
                    runs.Add(new YYRun(face));
                    i += length;
                    continue;
                }
                text.Append(message[i]);
                i++;
            }
            if (text.Length > 0) runs.Add(new YYRun(text.ToString()));
            return runs;
        }

        /// <summary>
        /// When a known face code starts at <paramref name="start"/> (which must be '['), returns it and the
        /// code length including both brackets.
        /// </summary>
        public static bool TryFaceAt(string message, int start, out YYFace face, out int length)
        {
            face = null; length = 0;
            if (message == null || start < 0 || start >= message.Length || message[start] != '[') return false;
            int limit = Math.Min(message.Length, start + MaxNameLength + 2);
            for (int j = start + 1; j < limit; j++)
            {
                char c = message[j];
                if (c == '[') return false;
                if (c != ']') continue;
                if (j == start + 1) return false;
                face = Find(message.Substring(start + 1, j - start - 1));
                if (face == null) return false;
                length = j - start + 1;
                return true;
            }
            return false;
        }

        /// <summary>True when the message contains at least one known face.</summary>
        public static bool ContainsFace(string message)
        {
            if (string.IsNullOrEmpty(message)) return false;
            for (int i = message.IndexOf('['); i >= 0; i = message.IndexOf('[', i + 1))
                if (TryFaceAt(message, i, out _, out _)) return true;
            return false;
        }

        /// <summary>Number of known faces in the message.</summary>
        public static int CountFaces(string message)
        {
            int count = 0;
            foreach (var run in Parse(message)) if (run.IsFace) count++;
            return count;
        }

        /// <summary>
        /// Normalises LLM or player text: known faces (including English aliases) are rewritten to their stored
        /// Chinese code, and short bracketed tokens that look like an invented face (one to
        /// <see cref="MaxNameLength"/> characters, no spaces or punctuation inside) are removed. Anything else
        /// in brackets, such as "[图片 1]" or a markdown link, is left as literal text.
        /// </summary>
        public static string StripUnknown(string message)
        {
            if (string.IsNullOrEmpty(message) || message.IndexOf('[') < 0) return message;
            var sb = new StringBuilder(message.Length);
            int i = 0;
            while (i < message.Length)
            {
                if (message[i] == '[')
                {
                    if (TryFaceAt(message, i, out var face, out int length)) { sb.Append(face.Code); i += length; continue; }
                    int close = LooksLikeFaceToken(message, i);
                    if (close > 0) { i = close + 1; continue; }
                }
                sb.Append(message[i]);
                i++;
            }
            return CollapseSpaces(sb.ToString(), message);
        }

        /// <summary>Removes every known face code (used for speakers who are not allowed faces yet).</summary>
        public static string StripFaces(string message)
        {
            if (!ContainsFace(message)) return message;
            var sb = new StringBuilder(message.Length);
            foreach (var run in Parse(message)) if (!run.IsFace) sb.Append(run.Text);
            return CollapseSpaces(sb.ToString(), message);
        }

        /// <summary>
        /// Writes the message for plain-text contexts (no inline sprites): in English, known faces become their
        /// English alias; in Chinese the stored codes are already the readable form.
        /// </summary>
        public static string ToPlainText(string message, bool english)
        {
            if (!english || !ContainsFace(message)) return message;
            var sb = new StringBuilder(message.Length);
            foreach (var run in Parse(message)) sb.Append(run.IsFace ? run.Face.EnAlias : run.Text);
            return sb.ToString();
        }

        /// <summary>All codes concatenated, e.g. "[微笑][撇嘴]…", for prompt whitelists.</summary>
        public static string Whitelist(IEnumerable<YYFace> faces = null)
        {
            var sb = new StringBuilder();
            foreach (var face in faces ?? all) sb.Append(face.Code);
            return sb.ToString();
        }

        /// <summary>
        /// The GBNF alternation of every code, e.g. "\"[微笑]\" | \"[撇嘴]\" | …", so a grammar rule can be written as
        /// <c>face ::= ( + GbnfAlternation() + )</c>. Pass a subset to restrict a character to fewer faces.
        /// </summary>
        public static string GbnfAlternation(IEnumerable<YYFace> faces = null)
        {
            var sb = new StringBuilder();
            foreach (var face in faces ?? all)
            {
                if (sb.Length > 0) sb.Append(" | ");
                sb.Append('"');
                foreach (char c in face.Code)
                {
                    if (c == '"' || c == '\\') sb.Append('\\');
                    sb.Append(c);
                }
                sb.Append('"');
            }
            return sb.ToString();
        }

        /// <summary>Faces looked up by code or name; unknown entries are skipped.</summary>
        public static List<YYFace> Subset(params string[] codesOrNames)
        {
            var list = new List<YYFace>();
            if (codesOrNames == null) return list;
            foreach (var code in codesOrNames)
            {
                var face = Find(code);
                if (face != null && !list.Contains(face)) list.Add(face);
            }
            return list;
        }

        static int LooksLikeFaceToken(string message, int start)
        {
            int limit = Math.Min(message.Length, start + MaxNameLength + 2);
            for (int j = start + 1; j < limit; j++)
            {
                char c = message[j];
                if (c == ']') return j > start + 1 ? j : -1;
                if (char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSymbol(c) || char.IsDigit(c)) return -1;
            }
            return -1;
        }

        static string CollapseSpaces(string result, string original)
        {
            // Removing a code can leave doubled ASCII spaces ("ok [x] then"); tidy those, keep everything else.
            if (original.IndexOf("  ", StringComparison.Ordinal) < 0)
                while (result.IndexOf("  ", StringComparison.Ordinal) >= 0) result = result.Replace("  ", " ");
            return result.Trim().Length == 0 ? "" : result;
        }
    }
}
