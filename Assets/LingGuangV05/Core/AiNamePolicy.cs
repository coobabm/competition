using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LingGuangV05.Core
{
    /// <summary>Identity validation and a shared abuse baseline. Full identity rules never apply to sentences.</summary>
    public static class AiNamePolicy
    {
        public const int MaxName = 16, MaxAddress = 8;
        // A deliberately small, reviewable baseline; not a comprehensive moderation service.
        static readonly string[] BlockedZh = { "傻逼", "傻屄", "煞笔", "傻比", "操你妈", "草你妈", "日你妈", "你妈死了", "去死吧", "狗杂种" };
        static readonly Regex BlockedEn = new Regex(@"\b(fuck|fucker|motherfucker|shit|bitch|asshole)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static readonly Regex Device = new Regex(@"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static string Normalize(string value)
        {
            if (!WellFormed(value)) return "";
            try { return (value ?? "").Normalize(NormalizationForm.FormKC).Trim(); }
            catch (ArgumentException) { return ""; }
        }

        static bool WellFormed(string value)
        {
            for (int i = 0; i < (value ?? "").Length; i++)
            {
                if (char.IsLowSurrogate(value[i])) return false;
                if (!char.IsHighSurrogate(value[i])) continue;
                if (++i >= value.Length || !char.IsLowSurrogate(value[i])) return false;
            }
            return true;
        }

        public static int Length(string value) => new StringInfo(value ?? "").LengthInTextElements;

        /// <summary>Shared small abuse baseline for normalized plain text; no identity length or filename restrictions.</summary>
        public static bool ContainsAbusiveLanguage(string clean)
        {
            if (string.IsNullOrEmpty(clean)) return false;
            var compact = new StringBuilder();
            foreach (char c in clean) if (!char.IsWhiteSpace(c) && !char.IsPunctuation(c) && !char.IsSymbol(c)) compact.Append(c);
            string compactText = compact.ToString();
            foreach (string word in BlockedZh)
                if (compactText.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return BlockedEn.IsMatch(clean.Replace('_', ' '));
        }

        public static string Problem(string value, bool english = false, int max = MaxName, bool fileName = true)
        {
            string T(string zh, string en) => english ? en : zh;
            if (!WellFormed(value)) return T("名字包含无法识别的字符，请重新输入。", "The name contains an invalid character. Please retype it.");
            // Check before Trim so pasted newlines and invisible marks cannot disappear silently.
            for (int i = 0; i < (value ?? "").Length; i++)
                if (char.IsControl(value[i]) || CharUnicodeInfo.GetUnicodeCategory(value, i) == UnicodeCategory.Format)
                    return T("名字和称呼不能包含换行或隐藏控制符。", "Names cannot contain newlines or invisible controls.");
            string clean = Normalize(value);
            if (Length(clean) < 1 || Length(clean) > max || clean.Length > 64)
                return T("请输入 1–" + max + " 个字。", "Enter 1–" + max + " characters.");
            if (clean.IndexOfAny("<>[]\"/\\:|?*".ToCharArray()) >= 0)
                return T("请去掉格式标签和文件名特殊符号。", "Remove markup and filename special characters.");
            if (fileName && (clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || clean.EndsWith(".") || clean == ".." || Device.IsMatch(clean) || clean == "the code"))
                return T("只填 AI 的名字，不用加 .exe；请避开系统保留名。", "Enter only the AI name, without .exe or a reserved system name.");
            if (ContainsAbusiveLanguage(clean))
                return T("这个称呼含有不合适的用语，换一个吧。", "Please choose a name without abusive language.");
            return null;
        }

        /// <summary>Legacy saves still load. Only their displayed identity is made safe; stored data stays intact.</summary>
        public static string Display(string value, bool english)
        {
            string name = Normalize(value);
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - 4).Trim();
            return Problem(name, english) == null ? name : AppNames.Ai(english);
        }
    }
}
