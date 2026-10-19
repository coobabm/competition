using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// Shared arithmetic input for teaching, training and inference. It reads only the visible claim and category.
    /// It never evaluates an expression or reads a label, worked answer, explanation, level or generator seed.
    /// </summary>
    public static class XgArithmeticInput
    {
        static readonly Regex Tokens = new Regex(@"\d+|[A-Za-z]+|[^\s]", RegexOptions.CultureInvariant);
        static string Key(string token)
        {
            // Board concept keys reserve '+' and '@'; encode literal operators without changing their meaning.
            var key = new StringBuilder("算符:");
            foreach (char c in token) key.Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
            return key.ToString();
        }

        public static XgBoardCard Input(XgCard displayed)
        {
            if (displayed == null || displayed.dataset != "arith") return null;
            string text = displayed.question ?? "", category = displayed.category ?? "";
            var input = new XgBoardCard { region = "logic", text = text, arithmeticVisibleClaim = true };
            input.Add("类:" + category);
            // Hash the same canonical token stream the model sees, not original character offsets/formatting.
            // The displayed right-hand-side is part of the claim; the actual correct answer is never supplied.
            var canonical = new StringBuilder(category).Append('\n');
            int position = 0;
            foreach (Match match in Tokens.Matches(text))
            {
                string token = match.Value.Replace('−', '-').Replace('×', '*').Replace('÷', '/');
                string key = Key(token);
                input.Add(key, position++, 0, 1);
                canonical.Append(key).Append(';');
            }
            uint hash = 2166136261;
            foreach (char c in canonical.ToString()) { hash ^= c; hash = unchecked(hash * 16777619); }
            input.seed = unchecked((int)hash);
            // truth is intentionally its default; the supervised caller attaches its target after encoding.
            return input;
        }
    }
}
