using System;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    public enum XgMoleMathKind
    {
        Add, Subtract, Multiply, Divide, Parentheses, Precedence,
        Power, SignedPower, SquareRoot, RootOfSquare,
        Limit, Derivative, DefiniteIntegral, IndefiniteIntegral
    }

    /// <summary>Exact, bounded rational values. No text parsing or floating-point tolerance decides a label.</summary>
    public readonly struct XgMoleNumber : IEquatable<XgMoleNumber>
    {
        public readonly long Numerator, Denominator;
        public XgMoleNumber(long numerator, long denominator = 1)
        {
            if (denominator == 0) throw new DivideByZeroException();
            if (denominator < 0) { numerator = -numerator; denominator = -denominator; }
            long a = Math.Abs(numerator), b = denominator;
            while (b != 0) { long t = a % b; a = b; b = t; }
            Numerator = numerator / a; Denominator = denominator / a;
        }
        public bool Equals(XgMoleNumber other) => Numerator == other.Numerator && Denominator == other.Denominator;
        public override bool Equals(object other) => other is XgMoleNumber n && Equals(n);
        public override int GetHashCode() => Numerator.GetHashCode() ^ Denominator.GetHashCode();
        public override string ToString() => Numerator.ToString(CultureInfo.InvariantCulture) +
            (Denominator == 1 ? "" : "/" + Denominator.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>A generated closed claim. The answer is used only by settlement, never by the inference adapter.</summary>
    public sealed class XgMoleQuestion
    {
        public XgMoleMathKind Kind { get; }
        public int A { get; }
        public int B { get; }
        public int N { get; }
        public XgMoleNumber Expected { get; }
        public XgMoleNumber Shown { get; }
        public string Prefix { get; }
        public string Suffix { get; }
        public string Category { get; }
        public string ConditionsZh { get; }
        public string ConditionsEn { get; }
        public bool Truth => Shown.Equals(Expected);
        public string Text => Prefix + Shown + Suffix;
        public string CorrectText => Prefix + Expected + Suffix;
        public string Why => CorrectText + "。" + ConditionsZh;
        public string WhyEn => CorrectText + ". " + ConditionsEn;
        internal XgMoleQuestion(XgMoleMathKind kind, int a, int b, int n, XgMoleNumber expected,
            XgMoleNumber shown, string prefix, string suffix, string category, string zh, string en)
        {
            Kind = kind; A = a; B = b; N = n; Expected = expected; Shown = shown;
            Prefix = prefix; Suffix = suffix; Category = category; ConditionsZh = zh; ConditionsEn = en;
        }
        public XgMoleQuestion AsTrue() => new XgMoleQuestion(Kind, A, B, N, Expected, Expected, Prefix, Suffix, Category, ConditionsZh, ConditionsEn);
        public XgCard Card() => new XgCard { dataset = "arith", question = Text, questionEn = Text,
            category = Category, truth = Truth, level = 1 };
    }

    /// <summary>Game-only content. XgArith.Generate and training/test pools are deliberately independent.</summary>
    public static class XgMoleArithmetic
    {
        static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
        static long Pow(int a, int n) { long value = 1; for (int i = 0; i < n; i++) value *= a; return value; }

        public static XgMoleQuestion Generate(int seed, int tier)
        {
            var r = new Random(seed);
            int count = tier >= 3 ? 14 : tier >= 2 ? 10 : 6;
            var kind = (XgMoleMathKind)r.Next(count);
            int a = r.Next(2, 10), b = r.Next(2, 10), n = r.Next(2, 5);
            string prefix, suffix = "", category = "混合运算", zh = "先算括号，再乘除，后加减。", en = "Brackets first, then multiplication/division, then addition/subtraction.";
            XgMoleNumber expected;
            switch (kind)
            {
                case XgMoleMathKind.Add:
                    prefix = I(a) + "+" + I(b) + "="; expected = new XgMoleNumber(a + b); category = "加减"; break;
                case XgMoleMathKind.Subtract:
                    prefix = I(a) + "−" + I(b) + "="; expected = new XgMoleNumber(a - b); category = "加减"; break;
                case XgMoleMathKind.Multiply:
                    prefix = I(a) + "×" + I(b) + "="; expected = new XgMoleNumber(a * b); category = "乘法"; break;
                case XgMoleMathKind.Divide:
                    prefix = I(a) + "÷" + I(b) + "="; expected = new XgMoleNumber(a, b); category = "除法";
                    zh = "除数非零；分数约分后比较。"; en = "The divisor is nonzero; fractions are compared after reduction."; break;
                case XgMoleMathKind.Parentheses:
                    prefix = "(" + I(a) + "+" + I(b) + ")×" + I(n) + "="; expected = new XgMoleNumber((a + b) * n); break;
                case XgMoleMathKind.Precedence:
                    prefix = I(a) + "+" + I(b) + "×" + I(n) + "="; expected = new XgMoleNumber(a + b * n); break;
                case XgMoleMathKind.Power:
                    prefix = "(" + I(-a) + ")^" + I(n) + "="; expected = new XgMoleNumber(Pow(-a, n)); category = "幂";
                    zh = "括号内负数整体乘方。"; en = "The whole parenthesized negative base is raised to the power."; break;
                case XgMoleMathKind.SignedPower:
                    prefix = "−" + I(a) + "^" + I(n) + "="; expected = new XgMoleNumber(-Pow(a, n)); category = "幂";
                    zh = "幂先于前面的负号计算。"; en = "Exponentiation precedes the leading minus sign."; break;
                case XgMoleMathKind.SquareRoot:
                    prefix = "√" + I(a * a) + "="; expected = new XgMoleNumber(a); category = "根式";
                    zh = "实数算术平方根取非负值；这不是求方程的全部根。"; en = "The real principal square root is nonnegative; this is not a request for all roots of an equation."; break;
                case XgMoleMathKind.RootOfSquare:
                    prefix = "√((" + I(-a) + ")²)="; expected = new XgMoleNumber(a); category = "根式";
                    zh = "实数范围内 √(x²)=|x|。"; en = "For real x, √(x²)=|x|."; break;
                case XgMoleMathKind.Limit:
                    prefix = "lim[x→" + I(a) + "] (x²−" + I(a * a) + ")/(x−" + I(a) + ")=";
                    expected = new XgMoleNumber(2 * a); category = "极限";
                    zh = "实数双侧极限；x≠趋近点时可约去因子，原函数在该点无定义。";
                    en = "A two-sided real limit; cancel the factor only away from the approach point, where the original quotient is undefined."; break;
                case XgMoleMathKind.Derivative:
                    prefix = "[d(" + I(a) + "x^" + I(n) + ")/dx]x=" + I(b) + ": ";
                    expected = new XgMoleNumber(a * n * Pow(b, n - 1)); category = "导数";
                    zh = "多项式在标明的实数点求导数值。"; en = "Evaluate the polynomial derivative at the stated real point."; break;
                case XgMoleMathKind.DefiniteIntegral:
                    prefix = "∫[0," + I(b) + "] " + I(a) + "x^" + I(n) + " dx=";
                    expected = new XgMoleNumber(a * Pow(b, n + 1), n + 1); category = "积分";
                    zh = "连续多项式的定积分；上下限已经明确。"; en = "A definite integral of a continuous polynomial with explicit bounds."; break;
                default:
                    prefix = "∫ " + I(a) + "x^" + I(n) + " dx=("; suffix = ")x^" + I(n + 1) + "+C";
                    expected = new XgMoleNumber(a, n + 1); category = "积分";
                    zh = "实数范围；C 为任意常数，表示全部原函数。分数系数按等值判断。";
                    en = "Over the reals, C is any constant and represents the full antiderivative family. Fractional coefficients are compared by value."; break;
            }
            bool truth = r.Next(2) == 0;
            var shown = truth ? expected : new XgMoleNumber(expected.Numerator + (r.Next(2) == 0 ? -1 : 1) * expected.Denominator, expected.Denominator);
            return new XgMoleQuestion(kind, a, b, n, expected, shown, prefix, suffix, category, zh, en);
        }
    }
}
