using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LingGuangV05.Runtime
{
    /// <summary>Presentation-only facade. The native desktop manager owns the locale; no game/save data is translated.</summary>
    public static class GameText
    {
        public const string Chinese = "zh-CN";
        public const string English = "en-US";
        private static Func<string> provider;
        private static Dictionary<string, Entry> exact;
        private static List<Pattern> patterns;
        private static readonly Dictionary<string, string> chineseCache = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> englishCache = new Dictionary<string, string>();
        public static event Action Changed;
        public static string LanguageId => Normalize(provider == null ? null : provider());
        public static bool IsEnglish => LanguageId == English;
        public static string Normalize(string value) => value == English ? English : Chinese;
        public static string T(string zh, string en) => IsEnglish ? en : zh;
        public static string F(string zh, string en, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, T(zh, en), args);

        public static IDisposable Bind(Func<string> getLocale)
        {
            if (getLocale == null) throw new ArgumentNullException(nameof(getLocale));
            var prior = provider; provider = getLocale;
            return new Binding(prior, getLocale);
        }
        public static void PublishChanged() => Changed?.Invoke();

        /// <summary>Only call with authored text. Captures (including player names) are preserved verbatim.</summary>
        public static string Source(string source)
        {
            if (string.IsNullOrEmpty(source)) return source;
            EnsureCatalog();
            if (exact.TryGetValue(source, out Entry entry)) return T(entry.zh, entry.en);
            var cache = IsEnglish ? englishCache : chineseCache;
            if (cache.TryGetValue(source, out string cached)) return cached;
            foreach (Pattern pattern in patterns)
            {
                if (!source.StartsWith(pattern.prefix, StringComparison.Ordinal) || !source.EndsWith(pattern.suffix, StringComparison.Ordinal)) continue;
                Match match;
                try { match = pattern.expression.Match(source); }
                catch (RegexMatchTimeoutException) { continue; }
                if (!match.Success) continue;
                string target = T(pattern.entry.zh, pattern.entry.en);
                return Cache(cache, source, Regex.Replace(target, @"\{(\d+)\}", token => match.Groups["p" + token.Groups[1].Value].Value));
            }
            return Cache(cache, source, source);
        }

        private static string Cache(Dictionary<string, string> cache, string source, string value)
        { if (cache.Count >= 512) cache.Clear(); cache[source] = value; return value; }

        public static bool HasTranslation(string source)
        {
            EnsureCatalog();
            return !string.IsNullOrEmpty(source) && exact.ContainsKey(source);
        }

        private static void EnsureCatalog()
        {
            if (exact != null) return;
            exact = new Dictionary<string, Entry>(StringComparer.Ordinal);
            patterns = new List<Pattern>();
            var assets = Resources.LoadAll<TextAsset>("Localization");
            Array.Sort(assets, (a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (var asset in assets)
            {
                if (!asset.text.TrimStart().StartsWith("{", StringComparison.Ordinal)) continue;
                Catalog catalog = JsonUtility.FromJson<Catalog>(asset.text);
                if (catalog?.entries == null) continue;
                foreach (Entry entry in catalog.entries)
                {
                    if (string.IsNullOrEmpty(entry.zh) || string.IsNullOrEmpty(entry.en)) continue;
                    exact[entry.zh] = entry;
                    // Shared English labels can name different concepts (Home page / Home app).
                    // Keep the first English alias; explicit Chinese sources remain unambiguous.
                    if (!exact.ContainsKey(entry.en)) exact[entry.en] = entry;
                    AddPattern(entry.zh, entry); AddPattern(entry.en, entry);
                }
            }
            // Most specific templates first. An all-placeholder template is never admitted.
            patterns.Sort((a, b) => b.literalLength.CompareTo(a.literalLength));
        }

        private static void AddPattern(string source, Entry entry)
        {
            var tokens = Regex.Matches(source, @"\{(\d+)\}");
            if (tokens.Count == 0) return;
            int literalLength = Regex.Replace(source, @"\{\d+\}", "").Length;
            if (literalLength < 4) return;
            int offset = 0;
            string expression = @"\A";
            var seen = new HashSet<string>();
            foreach (Match token in tokens)
            {
                expression += Regex.Escape(source.Substring(offset, token.Index - offset));
                string group = "p" + token.Groups[1].Value;
                expression += seen.Add(group) ? "(?<" + group + ">[\\s\\S]*?)" : "\\k<" + group + ">";
                offset = token.Index + token.Length;
            }
            expression += Regex.Escape(source.Substring(offset)) + @"\z";
            patterns.Add(new Pattern { entry = entry, literalLength = literalLength, prefix = source.Substring(0, tokens[0].Index), suffix = source.Substring(tokens[tokens.Count - 1].Index + tokens[tokens.Count - 1].Length),
                expression = new Regex(expression, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(20)) });
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { provider = null; Changed = null; exact = null; patterns = null; chineseCache.Clear(); englishCache.Clear(); }
        [Serializable] private sealed class Catalog { public Entry[] entries; }
        [Serializable] private sealed class Entry { public string zh; public string en; }
        private sealed class Pattern { public Entry entry; public Regex expression; public int literalLength; public string prefix, suffix; }
        private sealed class Binding : IDisposable
        {
            private Func<string> previous, current;
            public Binding(Func<string> previous, Func<string> current) { this.previous = previous; this.current = current; }
            public void Dispose()
            {
                if (current == null) return;
                if (provider == current) provider = previous;
                current = null; previous = null;
            }
        }
    }
}
