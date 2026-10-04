using System;
using System.Text;

namespace LingGuangV05.Core.Story
{
    /// <summary>Replaces {token} placeholders. Unknown tokens (resolver returns null) stay as written.</summary>
    public static class StoryText
    {
        public static string Format(string text, Func<string, string> resolve)
        {
            if (string.IsNullOrEmpty(text) || resolve == null || text.IndexOf('{') < 0) return text;
            var sb = new StringBuilder(text.Length + 16);
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c == '{')
                {
                    int end = text.IndexOf('}', i + 1);
                    if (end > i + 1)
                    {
                        string token = text.Substring(i + 1, end - i - 1);
                        if (IsToken(token))
                        {
                            string value = resolve(token);
                            if (value != null) { sb.Append(value); i = end + 1; continue; }
                        }
                    }
                }
                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }

        private static bool IsToken(string token)
        {
            foreach (char ch in token) if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch == '.')) return false;
            return true;
        }
    }
}
