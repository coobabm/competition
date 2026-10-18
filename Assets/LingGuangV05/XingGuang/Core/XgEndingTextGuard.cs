using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;

namespace LingGuangV05.XingGuang
{
    /// <summary>Local ending text guard. Null means rejection; this is not a model moderation service.</summary>
    public static class XgEndingTextGuard
    {
        public const int MaxDynamicCharacters = 4096;
        public const int MaxReviewedCharacters = 16384;
        const int MaxRawCharacters = 32768;
        // Only actual TMP formatting syntax. Mathematical comparisons and ordinary angle brackets survive.
        static readonly Regex SimpleTags = new Regex(@"</?(?:b|i|u|s|sub|sup|nobr|noparse|uppercase|lowercase|smallcaps)>|</(?:color|size|font|material|link|mark|align|voffset|cspace|mspace|indent|line-indent|margin|width|rotate|style)>|<(?:color|size|font|material|link|mark|align|voffset|cspace|mspace|indent|line-indent|margin|width|rotate|style)=[^<>\r\n]+>|<sprite(?:=[^<>\r\n]+| [^<>\r\n]+)?>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static readonly Regex BreakTags = new Regex(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>Player input and model-generated 2016 content. Does not apply identity filename/length rules.</summary>
        public static string Filter(string value, bool english)
        {
            string clean = NormalizeDynamic(value);
            if (clean == null) return null;
            clean = BreakTags.Replace(clean, "\n");
            clean = SimpleTags.Replace(clean, "");
            if (AiNamePolicy.ContainsAbusiveLanguage(clean)) return null;
            clean = EraLexicon.Scrub(clean).Trim();
            // Recheck after removing future slang so token boundaries cannot reveal an unchecked term.
            if (AiNamePolicy.ContainsAbusiveLanguage(clean) || new StringInfo(clean).LengthInTextElements > MaxDynamicCharacters) return null;
            return clean;
        }

        /// <summary>
        /// Reviewed authored sheets or historical epilogue text only. Preserves exact wording, punctuation,
        /// mathematical symbols and deliberate false statements. Never use for raw chat/model/player input.
        /// The UI must keep richText=false. No 2016-era scrub is applied to reviewed 2017 material.
        /// </summary>
        public static string FilterReviewed(string value, bool english)
        {
            value = value ?? "";
            if (value.Length > MaxRawCharacters || !WellFormed(value)) return null;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                var category = CharUnicodeInfo.GetUnicodeCategory(value, i);
                if (category == UnicodeCategory.Format || char.IsControl(c) && c != '\n' && c != '\r' && c != '\t') return null;
                if (char.IsHighSurrogate(c)) i++;
            }
            return new StringInfo(value).LengthInTextElements <= MaxReviewedCharacters ? value : null;
        }

        static string NormalizeDynamic(string value)
        {
            value = value ?? "";
            if (value.Length > MaxRawCharacters || !WellFormed(value)) return null;
            try { value = value.Normalize(NormalizationForm.FormKC).Replace("\r\n", "\n").Replace('\r', '\n'); }
            catch (ArgumentException) { return null; }
            var builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsHighSurrogate(c)) { builder.Append(c).Append(value[++i]); continue; }
                if (c == '\n') { builder.Append(c); continue; }
                if (c == '\t' || char.IsWhiteSpace(c)) { builder.Append(' '); continue; }
                if (char.IsControl(c) || CharUnicodeInfo.GetUnicodeCategory(value, i) == UnicodeCategory.Format) continue;
                builder.Append(c);
            }
            string clean = builder.ToString().Trim();
            return new StringInfo(clean).LengthInTextElements <= MaxDynamicCharacters ? clean : null;
        }

        static bool WellFormed(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLowSurrogate(value[i])) return false;
                if (char.IsHighSurrogate(value[i]) && (++i >= value.Length || !char.IsLowSurrogate(value[i]))) return false;
            }
            return true;
        }
    }
}
