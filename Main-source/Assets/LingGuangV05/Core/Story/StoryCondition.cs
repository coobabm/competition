using System;
using System.Globalization;

namespace LingGuangV05.Core.Story
{
    public enum StoryCompare { Truthy, Falsy, Ge, Le, Gt, Lt, Eq, Ne }

    /// <summary>
    /// One prerequisite. Grammar: <c>name</c> (non-zero), <c>!name</c> (zero), or <c>name op number</c>
    /// with op in &gt;= &lt;= &gt; &lt; == !=. Parsed once at load; evaluation allocates nothing.
    /// </summary>
    public sealed class StoryCondition
    {
        public string Field { get; private set; }
        public StoryCompare Op { get; private set; }
        public double Value { get; private set; }
        public string Source { get; private set; }

        public static StoryCondition Parse(string source)
        {
            if (string.IsNullOrWhiteSpace(source)) throw new StoryFormatException("Empty condition.");
            string s = source.Trim();
            var c = new StoryCondition { Source = s };
            string[] ops = { ">=", "<=", "==", "!=", ">", "<" };
            StoryCompare[] kinds = { StoryCompare.Ge, StoryCompare.Le, StoryCompare.Eq, StoryCompare.Ne, StoryCompare.Gt, StoryCompare.Lt };
            for (int i = 0; i < ops.Length; i++)
            {
                int at = s.IndexOf(ops[i], StringComparison.Ordinal);
                if (at <= 0) continue;
                string field = s.Substring(0, at).Trim();
                string number = s.Substring(at + ops[i].Length).Trim();
                double value;
                if (number == "true") value = 1;
                else if (number == "false") value = 0;
                else if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    throw new StoryFormatException("Condition \"" + s + "\": right side must be a number.");
                CheckName(field, s);
                c.Field = field; c.Op = kinds[i]; c.Value = value;
                return c;
            }
            if (s[0] == '!') { CheckName(s.Substring(1).Trim(), s); c.Field = s.Substring(1).Trim(); c.Op = StoryCompare.Falsy; return c; }
            CheckName(s, s);
            c.Field = s; c.Op = StoryCompare.Truthy;
            return c;
        }

        private static void CheckName(string name, string source)
        {
            if (name.Length == 0) throw new StoryFormatException("Condition \"" + source + "\": missing variable name.");
            foreach (char ch in name)
                if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch == '.' || ch == '-'))
                    throw new StoryFormatException("Condition \"" + source + "\": bad character '" + ch + "' in variable name.");
        }

        /// <summary>Throws StoryUnknownVariableException for unknown names so typos fail loudly.</summary>
        public bool Eval(IStoryVars vars)
        {
            double v;
            if (!vars.TryGet(Field, out v)) throw new StoryUnknownVariableException(Field);
            const double eps = 1e-9;
            switch (Op)
            {
                case StoryCompare.Truthy: return Math.Abs(v) > eps;
                case StoryCompare.Falsy: return Math.Abs(v) <= eps;
                case StoryCompare.Ge: return v + eps >= Value;
                case StoryCompare.Le: return v - eps <= Value;
                case StoryCompare.Gt: return v > Value + eps;
                case StoryCompare.Lt: return v < Value - eps;
                case StoryCompare.Eq: return Math.Abs(v - Value) <= eps;
                case StoryCompare.Ne: return Math.Abs(v - Value) > eps;
                default: return false;
            }
        }

        public override string ToString() { return Source; }
    }
}
