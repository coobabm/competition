using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgBoardCard
    {
        // Input adapter marker only. No label, result, generator identity or cached answer is stored here.
        public bool arithmeticVisibleClaim;
    }

    /// <summary>
    /// A numeric inductive bias for elementary binary claims, not a learned arithmetic engine.
    /// All four candidate relations are exposed symmetrically; the printed operator selects a context,
    /// never the relation used to answer. The ordinary Board weights must learn that association from
    /// the external training target. This class cannot return a prediction and never reads supervision.
    /// </summary>
    public static class XgArithmeticRelations
    {
        const string Number = @"[+-]?\d{1,10}(?:/\d{1,10})?";
        static readonly Regex Binary = new Regex(@"^\s*(?<a>" + Number + @")\s*(?<op>[+*/-])\s*(?<b>" + Number + @")\s*=\s*(?<c>" + Number + @")\s*$", RegexOptions.CultureInvariant);
        const long Limit = 1000000000;

        static bool Rational(string text, out decimal numerator, out decimal denominator)
        {
            numerator = 0; denominator = 1;
            string[] parts = text.Split('/');
            if (parts.Length > 2 || !long.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long n)
                || n < -Limit || n > Limit) return false;
            long d = 1;
            if (parts.Length == 2 && (!long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out d) || d <= 0 || d > Limit)) return false;
            numerator = n; denominator = d; return true;
        }

        static void Add(List<XgBoard.Element> elements, string id)
            => elements.Add(new XgBoard.Element { id = id, act = 1, x = -1, made = true });

        public static bool TryElements(XgBoardCard card, XgKnobs knobs, out List<XgBoard.Element> elements)
        {
            elements = null;
            if (card == null || !card.arithmeticVisibleClaim || card.region != "logic") return false;
            string text = (card.text ?? "").Replace('−', '-').Replace('×', '*').Replace('÷', '/');
            if (text.Length > 128) return false;
            var match = Binary.Match(text);
            if (!match.Success
                || !Rational(match.Groups["a"].Value, out decimal an, out decimal ad)
                || !Rational(match.Groups["b"].Value, out decimal bn, out decimal bd)
                || !Rational(match.Groups["c"].Value, out decimal cn, out decimal cd)) return false;

            string symbol = ((int)match.Groups["op"].Value[0]).ToString("X4", CultureInfo.InvariantCulture);
            string context = "numrel:v1:" + symbol + ":";
            elements = new List<XgBoard.Element>(6);
            if (knobs.bias) Add(elements, XgBoard.BiasElement);
            Add(elements, "numrel:v1:算符:" + symbol);
            // Decimal products are exact and bounded by 2e27 under the parsed limits (decimal max ~7.9e28).
            // Each is a hypothesis about the three visible values, independent of the printed operator.
            Add(elements, context + "sum:" + ((an * bd + bn * ad) * cd == cn * ad * bd ? "match" : "miss"));
            Add(elements, context + "difference:" + ((an * bd - bn * ad) * cd == cn * ad * bd ? "match" : "miss"));
            Add(elements, context + "product:" + (an * bn * cd == cn * ad * bd ? "match" : "miss"));
            Add(elements, context + "quotient:" + (bn != 0 && an * bd * cd == cn * ad * bn ? "match" : "miss"));
            return true;
        }
    }
}
