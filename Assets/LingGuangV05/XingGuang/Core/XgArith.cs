using System;
using System.Collections.Generic;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// The 算术 desk: endless yes/no claims about sums, generated from a seed and a level (1–5) by fixed rules. Each
    /// generator first picks the answer it wants (about half yes) and works out the real result. A "no" shows a slip a
    /// person really makes: off by one or two, off by ten (a carry or borrow forgotten), the digits swapped, the wrong
    /// operation, left to right instead of × and ÷ first. Every question also carries <see cref="XgLogicQuestion.check"/>,
    /// a plain formula of what it claims, so the tests can recompute the answer on their own.
    /// </summary>
    public static class XgArith
    {
        public const int MaxLevel = 5;

        delegate XgLogicQuestion Gen(Random r, int level, bool want);

        /// <summary>
        /// Kinds by first level and weight. Level 1 is sums within 20; 2 adds the times table and word problems; 3 long
        /// multiplication, division, odd and even, divisibility and number patterns; 4 order of operations, powers of 2 and
        /// primes; 5 percentages, fractions and squares.
        /// </summary>
        static readonly (int minLevel, int weight, Gen gen)[] Generators =
        {
            (1, 3, AddSub),
            (2, 2, TimesTable), (2, 1, Story),
            (3, 2, LongTimes), (3, 2, Divide), (3, 1, Parity), (3, 1, Divisible), (3, 1, Pattern),
            (4, 2, Precedence), (4, 1, PowerOfTwo), (4, 1, Prime),
            (5, 2, Percent), (5, 2, Fraction), (5, 1, Square),
        };

        public static XgLogicQuestion Generate(int seed, int level)
        {
            level = Math.Max(1, Math.Min(MaxLevel, level));
            var r = new Random(seed);
            var pool = new List<Gen>();
            foreach (var (min, weight, gen) in Generators)
            {
                if (level < min) continue;
                // Kinds two or more levels old fade to weight 1; a kind is most frequent at its first level.
                int w = level - min >= 2 ? 1 : weight + (level == min && min > 1 ? weight : 0);
                for (int i = 0; i < w; i++) pool.Add(gen);
            }
            bool want = r.NextDouble() < .5;
            var q = pool[r.Next(pool.Count)](r, level, want);
            q.level = level;
            return q;
        }

        static string I(long v) => v.ToString(CultureInfo.InvariantCulture);

        // ───────────── slips ─────────────

        struct Slip
        {
            public long value; public string zh, en;
            public Slip(long value, string zh, string en) { this.value = value; this.zh = zh; this.en = en; }
        }

        static Slip Off(Random r, long real, long by)
        {
            long v = r.Next(2) == 0 ? real + by : real - by;
            if (v < 0) v = real + by;
            return new Slip(v, "差了 " + I(by), "off by " + I(by));
        }

        /// <summary>Tens and units swapped (47 → 74, 141 → 114); the same number when the two digits match.</summary>
        static long SwapLastTwo(long n)
        {
            if (n < 10) return n;
            long units = n % 10, tens = n / 10 % 10;
            return n - units - tens * 10 + units * 10 + tens;
        }

        /// <summary>Tens and units swapped; no slip when the units digit is 0 (40 → 4 is no swap).</summary>
        static Slip Swapped(long real) => new Slip(real % 10 == 0 ? real : SwapLastTwo(real), "十位和个位写反了", "tens and units swapped");

        /// <summary>
        /// A slip that really is wrong, positive and within ten times the real value either way (a decimal point at
        /// most), or off by one when every listed slip happens to be right.
        /// </summary>
        static Slip Pick(Random r, long real, List<Slip> slips)
        {
            var ok = slips.FindAll(s => s.value != real && s.value > 0 && s.value * 10 >= real && s.value <= Math.Max(real, 1) * 10);
            return ok.Count == 0 ? new Slip(real + 1, "差了 1", "off by 1") : ok[r.Next(ok.Count)];
        }

        /// <summary>
        /// A claim "<paramref name="shown"/> = value": the real value when <paramref name="want"/>, else a slip. The text
        /// templates take the shown value as {0}; <paramref name="check"/> is the formula without its right-hand side.
        /// </summary>
        static XgLogicQuestion Claim(Random r, bool want, string cat, string catEn, string zh, string en, string shown, string check, long real, List<Slip> slips, string unitZh = "", string unitEn = "")
        {
            var slip = want ? new Slip(real, "", "") : Pick(r, real, slips);
            long value = slip.value;
            return new XgLogicQuestion
            {
                category = cat, categoryEn = catEn,
                text = string.Format(CultureInfo.InvariantCulture, zh, I(value)), textEn = string.Format(CultureInfo.InvariantCulture, en, I(value)),
                truth = value == real,
                why = shown + " = " + I(real) + unitZh + (value == real ? "。" : "，不是 " + I(value) + "：" + slip.zh + "。"),
                whyEn = shown + " = " + I(real) + unitEn + (value == real ? "." : ", not " + I(value) + ": " + slip.en + "."),
                check = check + " == " + I(value),
            };
        }

        /// <summary>
        /// A bare equation with an answer, such as 「17 + 5 = 22」: the player judges it 对 / 错. About half are right; a
        /// wrong one carries a slip a person really makes.
        /// </summary>
        static XgLogicQuestion Equation(Random r, bool want, string cat, string catEn, string shown, long real, List<Slip> slips)
            => Claim(r, want, cat, catEn, shown + " = {0}", shown + " = {0}", shown, Formula(shown), real, slips);

        /// <summary>The visible expression as a formula for the tests: × ÷ − become * / -.</summary>
        static string Formula(string shown) => shown.Replace("×", "*").Replace("÷", "/").Replace("−", "-");

        // ───────────── 1 · adding and subtracting ─────────────

        static XgLogicQuestion AddSub(Random r, int level, bool want)
        {
            int max = level <= 1 ? 20 : level <= 3 ? 100 : 1000;
            bool add = r.Next(2) == 0;
            long a, b;
            if (level <= 1)
            {
                if (add) { a = r.Next(1, max); b = r.Next(1, max - (int)a + 1); }
                else { a = r.Next(3, max + 1); b = r.Next(1, (int)a); }
            }
            else if (add) { a = r.Next(10, max - 10); b = r.Next(2, max - (int)a); }
            else { a = r.Next(20, max); b = r.Next(2, (int)a - 1); }
            long real = add ? a + b : a - b;
            var slips = new List<Slip> { Off(r, real, 1), Off(r, real, 2) };
            // The wrong operation, where its result is not absurdly far off (19 + 18 = 1 is no slip anyone makes).
            if (add ? Math.Abs(a - b) * 5 >= real : a + b <= 5 * real) slips.Add(add ? new Slip(Math.Abs(a - b), "这是减法的结果", "that is the difference") : new Slip(a + b, "这是加法的结果", "that is the sum"));
            if (level >= 2)
            {
                bool carry = add ? a % 10 + b % 10 >= 10 : a % 10 < b % 10;
                if (carry) slips.Add(add ? new Slip(real - 10, "忘了进位", "forgot to carry") : new Slip(real + 10, "忘了借位", "forgot to borrow"));
                slips.Add(Off(r, real, 10));
                slips.Add(Swapped(real));
                if (carry) slips.Add(slips[slips.Count - 3]); // the classic slip twice as often
            }
            else slips.RemoveAll(s => s.value > max); // level 1 never shows a number over 20
            return Equation(r, want, "加减", "Add and subtract", I(a) + (add ? " + " : " − ") + I(b), real, slips);
        }

        // ───────────── 2 · the times table ─────────────

        static XgLogicQuestion TimesTable(Random r, int level, bool want)
        {
            long a = r.Next(2, 10), b = r.Next(2, 10), real = a * b;
            var slips = new List<Slip>
            {
                new Slip(real + a, "多算了一个 " + I(a), "one " + I(a) + " too many"), new Slip(real - a, "少算了一个 " + I(a), "one " + I(a) + " too few"),
                Off(r, real, 1), Swapped(real), new Slip(a + b, "这是加法的结果", "that is the sum"),
            };
            return Equation(r, want, "乘法", "Multiplication", I(a) + " × " + I(b), real, slips);
        }

        // ───────────── 2+ · word problems (2016) ─────────────

        delegate XgLogicQuestion Template(Random r, bool want);

        static readonly (int minLevel, Template make)[] Stories =
        {
            (2, Noodles), (2, Netbar), (2, PowerBill), (2, Channel),
            (3, Overnight), (3, Crate), (3, Download),
            (4, NoodlesAndCola), (4, NetbarNight),
            (5, GpuSale), (5, BillRise),
        };

        static XgLogicQuestion Story(Random r, int level, bool want)
        {
            var pool = new List<Template>();
            foreach (var (min, make) in Stories)
            {
                if (level < min) continue;
                pool.Add(make);
                if (min == level) pool.Add(make); // this level's problems first
            }
            return pool[r.Next(pool.Count)](r, want);
        }

        static XgLogicQuestion Word(Random r, bool want, string zh, string en, string shown, long real, List<Slip> slips, string unitZh, string unitEn)
            => Claim(r, want, "应用题", "Word problem", zh, en, shown, Formula(shown), real, slips, unitZh, unitEn);

        static XgLogicQuestion Noodles(Random r, bool want)
        {
            long a = r.Next(3, 6), b = r.Next(2, 10), real = a * b;
            return Word(r, want, "泡面 " + I(a) + " 块一包，买 " + I(b) + " 包要 {0} 块吗？", "Instant noodles are " + I(a) + " yuan a pack. Do " + I(b) + " packs cost {0} yuan?",
                I(a) + " × " + I(b), real, new List<Slip> { new Slip(real + a, "多算了一包", "one pack too many"), new Slip(real - a, "少算了一包", "one pack too few"), Off(r, real, 1), new Slip(a + b, "加起来了，应该乘", "added instead of multiplied") }, " 块", " yuan");
        }

        static XgLogicQuestion Netbar(Random r, bool want)
        {
            long a = r.Next(3, 7), b = r.Next(2, 10), real = a * b;
            return Word(r, want, "网吧上机 " + I(a) + " 块一小时，上了 " + I(b) + " 个小时，一共 {0} 块吗？", "The net café charges " + I(a) + " yuan an hour. Do " + I(b) + " hours cost {0} yuan?",
                I(a) + " × " + I(b), real, new List<Slip> { new Slip(real + a, "多算了一小时", "one hour too many"), new Slip(real - a, "少算了一小时", "one hour too few"), Off(r, real, 2), new Slip(a + b, "加起来了，应该乘", "added instead of multiplied") }, " 块", " yuan");
        }

        static XgLogicQuestion PowerBill(Random r, bool want)
        {
            long a = r.Next(40, 100), b = r.Next(11, (int)a - 5), real = a - b;
            var slips = new List<Slip> { Off(r, real, 1), Off(r, real, 10), new Slip(a + b, "加起来了，应该减", "added instead of subtracted") };
            if (a % 10 < b % 10) { slips.Add(new Slip(real + 10, "忘了借位", "forgot to borrow")); slips.Add(new Slip(real + 10, "忘了借位", "forgot to borrow")); }
            return Word(r, want, "这个月电费 " + I(a) + " 块，已经交了 " + I(b) + " 块，还差 {0} 块吗？", "This month's power bill is " + I(a) + " yuan and " + I(b) + " is paid. Is {0} yuan still owed?",
                I(a) + " − " + I(b), real, slips, " 块", " yuan");
        }

        static XgLogicQuestion Channel(Random r, bool want)
        {
            long a = r.Next(20, 70), b = r.Next(5, 30), real = a + b;
            var slips = new List<Slip> { Off(r, real, 1), Off(r, real, 10) };
            if (a % 10 + b % 10 >= 10) { slips.Add(new Slip(real - 10, "忘了进位", "forgot to carry")); slips.Add(new Slip(real - 10, "忘了进位", "forgot to carry")); }
            else slips.Add(Swapped(real));
            return Word(r, want, "YY 频道里原来有 " + I(a) + " 人，又进来 " + I(b) + " 人，现在 {0} 人吗？", "A YY channel had " + I(a) + " people and " + I(b) + " more joined. Are there {0} now?",
                I(a) + " + " + I(b), real, slips, " 人", " people");
        }

        static XgLogicQuestion Overnight(Random r, bool want)
        {
            long b = r.Next(3, 9), real = r.Next(12, 26), a = b * real;
            return Word(r, want, I(b) + " 个人包夜，一共 " + I(a) + " 块网费平摊，每人 {0} 块吗？", I(b) + " friends split a " + I(a) + " yuan all-night net café bill. Does each pay {0} yuan?",
                I(a) + " ÷ " + I(b), real, new List<Slip> { Off(r, real, 1), Off(r, real, 2), Swapped(real) }, " 块", " yuan");
        }

        static XgLogicQuestion Crate(Random r, bool want)
        {
            long a = new long[] { 12, 20, 24 }[r.Next(3)], b = r.Next(3, 10), real = a * b;
            var slips = new List<Slip> { new Slip(real + a, "多算了一箱", "one box too many"), new Slip(real - a, "少算了一箱", "one box too few"), Off(r, real, 10) };
            if (a % 10 * b >= 10) slips.Add(new Slip(a / 10 * b * 10 + a % 10 * b % 10, "忘了进位", "forgot to carry"));
            return Word(r, want, "一箱泡面 " + I(a) + " 包，买 " + I(b) + " 箱一共 {0} 包吗？", "A box holds " + I(a) + " packs of noodles. Do " + I(b) + " boxes hold {0} packs?",
                I(a) + " × " + I(b), real, slips, " 包", " packs");
        }

        static XgLogicQuestion Download(Random r, bool want)
        {
            long b = r.Next(2, 6), real = r.Next(12, 40), a = b * real;
            return Word(r, want, "网速每秒 " + I(b) + " MB，下载 " + I(a) + " MB 的游戏补丁要 {0} 秒吗？", "At " + I(b) + " MB a second, does a " + I(a) + " MB game patch take {0} seconds?",
                I(a) + " ÷ " + I(b), real, new List<Slip> { Off(r, real, 1), Off(r, real, 10), Swapped(real) }, " 秒", " seconds");
        }

        static XgLogicQuestion NoodlesAndCola(Random r, bool want)
        {
            long a = r.Next(2, 7), b = r.Next(3, 6), c = r.Next(2, 6), real = a * b + c;
            return Word(r, want, "买 " + I(a) + " 包泡面，每包 " + I(b) + " 块，再加一瓶 " + I(c) + " 块的可乐，一共 {0} 块吗？",
                "Buy " + I(a) + " packs of noodles at " + I(b) + " yuan each and one cola at " + I(c) + " yuan. Is that {0} yuan?",
                I(a) + " × " + I(b) + " + " + I(c), real, new List<Slip> { new Slip(a * (b + c), "把可乐也乘了 " + I(a) + " 次", "the cola was counted " + I(a) + " times"), new Slip(real - b, "少算了一包", "one pack too few"), Off(r, real, 1) }, " 块", " yuan");
        }

        static XgLogicQuestion NetbarNight(Random r, bool want)
        {
            long a = r.Next(8, 21), b = r.Next(10, 21), c = r.Next(20, (int)(a * b / 2)), real = a * b - c;
            return Word(r, want, "网吧 " + I(a) + " 台机器，每台每晚收 " + I(b) + " 块，再扣掉 " + I(c) + " 块电费，净赚 {0} 块吗？",
                "A net café has " + I(a) + " machines at " + I(b) + " yuan a night each, minus " + I(c) + " yuan of power. Is the profit {0} yuan?",
                I(a) + " × " + I(b) + " − " + I(c), real, new List<Slip> { Off(r, real, 10), new Slip(real + c, "忘了扣电费", "forgot the power bill"), Off(r, real, 1) }, " 块", " yuan");
        }

        static XgLogicQuestion GpuSale(Random r, bool want)
        {
            long[] prices = { 800, 1000, 1200, 1500, 1600, 2000, 2400, 2500, 3000 };
            long[] cuts = { 5, 10, 15, 20, 25, 30 };
            long a, p;
            do { a = prices[r.Next(prices.Length)]; p = cuts[r.Next(cuts.Length)]; } while (a * p % 100 != 0);
            long off = a * p / 100, real = a - off;
            return Claim(r, want, "应用题", "Word problem", "原价 " + I(a) + " 块的显卡降价 " + I(p) + "%，现在 {0} 块吗？",
                "A graphics card at " + I(a) + " yuan is cut by " + I(p) + "%. Does it cost {0} yuan now?",
                I(a) + " − " + I(a) + " × " + I(p) + "%", I(a) + " - " + I(a) + " * " + I(p) + " / 100", real,
                new List<Slip> { new Slip(off, "这是降掉的钱", "that is the discount"), new Slip(a - p, "降的是 " + I(p) + "%，不是 " + I(p) + " 块", "the cut is " + I(p) + "%, not " + I(p) + " yuan"), Off(r, real, 100) }, " 块", " yuan");
        }

        static XgLogicQuestion BillRise(Random r, bool want)
        {
            long[] rises = { 5, 10, 15, 20, 25, 50 };
            long a, p;
            do { a = 20 * r.Next(3, 16); p = rises[r.Next(rises.Length)]; } while (a * p % 100 != 0);
            long up = a * p / 100, real = a + up;
            return Claim(r, want, "应用题", "Word problem", "电费涨了 " + I(p) + "%，原来每月 " + I(a) + " 块，现在 {0} 块吗？",
                "The power bill went up " + I(p) + "%. It was " + I(a) + " yuan a month; is it {0} yuan now?",
                I(a) + " + " + I(a) + " × " + I(p) + "%", I(a) + " + " + I(a) + " * " + I(p) + " / 100", real,
                new List<Slip> { new Slip(a + p, "涨的是 " + I(p) + "%，不是 " + I(p) + " 块", "the rise is " + I(p) + "%, not " + I(p) + " yuan"), new Slip(up, "这只是涨的部分", "that is only the rise"), Off(r, real, 10) }, " 块", " yuan");
        }

        // ───────────── 3 · long multiplication and division ─────────────

        static XgLogicQuestion LongTimes(Random r, int level, bool want)
        {
            long a, b;
            var slips = new List<Slip>();
            if (r.Next(3) > 0)
            {
                a = r.Next(12, 100); b = r.Next(3, 10);
                long units = a % 10 * b;
                if (units >= 10) { long forgot = a / 10 * b * 10 + units % 10; slips.Add(new Slip(forgot, "忘了进位", "forgot to carry")); slips.Add(new Slip(forgot, "忘了进位", "forgot to carry")); }
                slips.Add(new Slip((a + 1) * b, "多算了一个 " + I(b), "one " + I(b) + " too many"));
            }
            else
            {
                a = r.Next(11, 26); b = r.Next(11, 20);
                slips.Add(new Slip(a * (b / 10 * 10), "只乘了十位", "only the tens were multiplied"));
                slips.Add(new Slip(a * b - a, "少算了一个 " + I(a), "one " + I(a) + " too few"));
            }
            long real = a * b;
            slips.Add(Off(r, real, 10)); slips.Add(Swapped(real));
            return Equation(r, want, "乘法", "Multiplication", I(a) + " × " + I(b), real, slips);
        }

        static XgLogicQuestion Divide(Random r, int level, bool want)
        {
            long b, q;
            if (level >= 4 && r.Next(2) == 0) { b = r.Next(11, 20); q = r.Next(3, 10); }
            else { b = r.Next(2, 10); q = r.Next(11, 41); }
            long a = b * q;
            return Equation(r, want, "除法", "Division", I(a) + " ÷ " + I(b), q,
                new List<Slip> { Off(r, q, 1), Off(r, q, 10), Swapped(q), new Slip(q + 1, "商多了 1", "the quotient is one too big") });
        }

        static XgLogicQuestion Parity(Random r, int level, bool want)
        {
            bool add = r.Next(3) > 0;
            long a = r.Next(100, 1000), b = r.Next(10, add ? 1000 : 100);
            long value = add ? a + b : a * b;
            bool odd = value % 2 == 1;
            bool askOdd = want ? odd : !odd;
            string op = add ? " + " : " × ", word = askOdd ? "奇数" : "偶数", wordEn = askOdd ? "odd" : "even";
            string rule = add ? (odd ? "一奇一偶，和是奇数" : a % 2 == 1 ? "奇数加奇数，和是偶数" : "偶数加偶数，和是偶数")
                : (odd ? "两个奇数相乘才是奇数" : "有一个偶数，积就是偶数");
            string ruleEn = add ? (odd ? "odd plus even is odd" : a % 2 == 1 ? "odd plus odd is even" : "even plus even is even")
                : (odd ? "only odd times odd is odd" : "anything times an even number is even");
            return new XgLogicQuestion
            {
                category = "奇偶", categoryEn = "Odd and even",
                text = I(a) + op + I(b) + " 的结果是" + word + "吗？", textEn = "Is " + I(a) + op + I(b) + " " + wordEn + "?",
                truth = askOdd == odd,
                why = "只看个位：" + rule + "（" + I(a) + op + I(b) + " = " + I(value) + "）。",
                whyEn = "Look at the last digits: " + ruleEn + " (" + I(a) + op + I(b) + " = " + I(value) + ").",
                check = "(" + I(a) + (add ? " + " : " * ") + I(b) + ") % 2 == " + (askOdd ? "1" : "0"),
            };
        }

        static XgLogicQuestion Divisible(Random r, int level, bool want)
        {
            long[] divisors = { 3, 4, 6, 7, 8, 9, 11 };
            long k = divisors[r.Next(divisors.Length)];
            long m = r.Next((int)(100 / k) + 1, (int)(999 / k));
            // A "no" sits right next to a multiple, so a quick glance does not settle it.
            long n = want ? k * m : k * m + (r.Next(2) == 0 ? 1 : -1) * r.Next(1, (int)k);
            long digits = 0; for (long t = n; t > 0; t /= 10) digits += t % 10;
            string hint = k == 3 || k == 9 ? "各位数字之和 " + I(digits) + (digits % k == 0 ? " 能" : " 不能") + "被 " + I(k) + " 整除。"
                : k == 4 ? "末两位 " + I(n % 100) + (n % 100 % 4 == 0 ? " 能" : " 不能") + "被 4 整除。" : "";
            string hintEn = k == 3 || k == 9 ? " Its digit sum " + I(digits) + (digits % k == 0 ? " is" : " is not") + " divisible by " + I(k) + "."
                : k == 4 ? " Its last two digits " + I(n % 100) + (n % 100 % 4 == 0 ? " are" : " are not") + " divisible by 4." : "";
            bool truth = n % k == 0;
            return new XgLogicQuestion
            {
                category = "整除", categoryEn = "Divisibility",
                text = I(n) + " 能被 " + I(k) + " 整除吗？", textEn = "Is " + I(n) + " divisible by " + I(k) + "?",
                truth = truth,
                why = (truth ? I(n) + " = " + I(k) + " × " + I(n / k) + "。" : I(n) + " ÷ " + I(k) + " = " + I(n / k) + " 余 " + I(n % k) + "。") + hint,
                whyEn = (truth ? I(n) + " = " + I(k) + " × " + I(n / k) + "." : I(n) + " ÷ " + I(k) + " = " + I(n / k) + " remainder " + I(n % k) + ".") + hintEn,
                check = I(n) + " % " + I(k) + " == 0",
            };
        }

        /// <summary>找规律: add, multiply (3+), squares and "sum of the two before" (4+), alternate add and subtract (5).</summary>
        static XgLogicQuestion Pattern(Random r, int level, bool want)
        {
            int kind = r.Next(level <= 3 ? 2 : level == 4 ? 4 : 5);
            var terms = new List<long>();
            string ruleZh, ruleEn, rule;
            switch (kind)
            {
                case 0: { long a = r.Next(1, 20), d = r.Next(2, 10); for (int i = 0; i < 6; i++) terms.Add(a + d * i); ruleZh = "每次加 " + I(d); ruleEn = "add " + I(d) + " each time"; rule = "add " + I(d); break; }
                case 1: { long a = r.Next(1, 5), k = r.Next(2, 4); for (int i = 0; i < 6; i++) terms.Add(a * (long)Math.Pow(k, i)); ruleZh = "每次乘 " + I(k); ruleEn = "multiply by " + I(k); rule = "mul " + I(k); break; }
                case 2: { long s = r.Next(1, 8); for (int i = 0; i < 6; i++) terms.Add((s + i) * (s + i)); ruleZh = "连续整数的平方"; ruleEn = "consecutive squares"; rule = "sq"; break; }
                case 3: { long a = r.Next(1, 4), b = r.Next(1, 5); terms.Add(a); terms.Add(b); for (int i = 2; i < 7; i++) terms.Add(terms[i - 1] + terms[i - 2]); ruleZh = "每项是前两项之和"; ruleEn = "each term is the sum of the two before"; rule = "fib"; break; }
                default: { long a = r.Next(1, 10), p = r.Next(2, 6), q = r.Next(1, 4); for (int i = 0; i < 7; i++) terms.Add(i == 0 ? a : terms[i - 1] + (i % 2 == 1 ? p : -q)); ruleZh = "交替加 " + I(p) + "、减 " + I(q); ruleEn = "alternately add " + I(p) + " and subtract " + I(q); rule = "alt " + I(p) + " " + I(q); break; }
            }
            int shownCount = Math.Max(4, terms.Count - 2);
            long next = terms[shownCount];
            long asked = want ? next : Math.Max(0, next + (r.Next(2) == 0 ? 1 : -1) * r.Next(1, 4));
            if (!want && asked == next) asked = next + 1;
            var shown = new List<string>();
            for (int i = 0; i < shownCount; i++) shown.Add(I(terms[i]));
            string list = string.Join(", ", shown);
            return new XgLogicQuestion
            {
                category = "找规律", categoryEn = "Number pattern",
                text = list + ", …… 下一个数是 " + I(asked) + " 吗？", textEn = list + ", … is the next number " + I(asked) + "?",
                truth = asked == next, why = "规律：" + ruleZh + "，下一个是 " + I(next) + "。", whyEn = "Rule: " + ruleEn + "; next is " + I(next) + ".",
                check = "seq " + rule + " | " + string.Join(" ", shown) + " | " + I(asked),
            };
        }

        // ───────────── 4 · order of operations, powers of 2, primes ─────────────

        static XgLogicQuestion Precedence(Random r, int level, bool want)
        {
            long a = r.Next(2, 10), b = r.Next(2, 10), c = r.Next(2, 10);
            switch (r.Next(4))
            {
                case 0:
                    return Equation(r, want, "混合运算", "Order of operations", I(a) + " + " + I(b) + " × " + I(c), a + b * c,
                        new List<Slip> { new Slip((a + b) * c, "从左往右算了，要先乘后加", "worked left to right; multiply first"), new Slip((a + b) * c, "从左往右算了，要先乘后加", "worked left to right; multiply first"), Off(r, a + b * c, 1) });
                case 1:
                {
                    long d = r.Next(2, 10);
                    return Equation(r, want, "混合运算", "Order of operations", I(a) + " × " + I(b) + " + " + I(c) + " × " + I(d), a * b + c * d,
                        new List<Slip> { new Slip((a * b + c) * d, "从左往右算了，要先乘后加", "worked left to right; multiply first"), new Slip((a * b + c) * d, "从左往右算了，要先乘后加", "worked left to right; multiply first"), Off(r, a * b + c * d, 2) });
                }
                case 2:
                    return Equation(r, want, "混合运算", "Order of operations", "(" + I(a) + " + " + I(b) + ") × " + I(c), (a + b) * c,
                        new List<Slip> { new Slip(a + b * c, "没先算括号", "the brackets were ignored"), new Slip(a + b * c, "没先算括号", "the brackets were ignored"), Off(r, (a + b) * c, 1) });
                default:
                {
                    // a + b ÷ c with b a multiple of c; left to right only when (a + b) also divides.
                    long q = r.Next(2, 10); b = c * q;
                    var slips = new List<Slip> { Off(r, a + q, 1), Off(r, a + q, 2) };
                    if ((a + b) % c == 0) { slips.Add(new Slip((a + b) / c, "从左往右算了，要先除后加", "worked left to right; divide first")); slips.Add(slips[slips.Count - 1]); }
                    else slips.Add(new Slip(a + b - c, "把 ÷ 当成了 −", "÷ read as −"));
                    return Equation(r, want, "混合运算", "Order of operations", I(a) + " + " + I(b) + " ÷ " + I(c), a + q, slips);
                }
            }
        }

        static XgLogicQuestion PowerOfTwo(Random r, int level, bool want)
        {
            switch (r.Next(3))
            {
                case 0:
                {
                    int n = r.Next(5, 13);
                    long real = 1L << n;
                    return Claim(r, want, "2 的幂", "Powers of 2", "2 的 " + n + " 次方是 {0} 吗？", "Is 2 to the power " + n + " equal to {0}?",
                        "2 的 " + n + " 次方", "2 ^ " + n, real,
                        PowerSlips(n));
                }
                case 1:
                {
                    long[] gb = { 2, 3, 4, 6, 8 };
                    long g = gb[r.Next(gb.Length)];
                    return Claim(r, want, "2 的幂", "Powers of 2", "显卡有 " + I(g) + " GB 显存，也就是 {0} MB 吗？", "A graphics card has " + I(g) + " GB of memory. Is that {0} MB?",
                        I(g) + " × 1024", I(g) + " * 1024", g * 1024,
                        new List<Slip> { new Slip(g * 1000, "1 GB 是 1024 MB，不是 1000 MB", "1 GB is 1024 MB, not 1000"), new Slip(g * 1000, "1 GB 是 1024 MB，不是 1000 MB", "1 GB is 1024 MB, not 1000"), new Slip(g * 512, "1 GB 是 1024 MB，不是 512 MB", "1 GB is 1024 MB, not 512") }, " MB", " MB");
                }
                default:
                {
                    int k = r.Next(5, 12);
                    long v = want ? 1L << k : new[] { 3L << (k - 1), (1L << k) + 2, (1L << k) - 2, (1L << k) / 100 * 100 }[r.Next(4)];
                    if (!want && (v & (v - 1)) == 0) v += 2;
                    bool truth = (v & (v - 1)) == 0;
                    int below = 0; while ((1L << (below + 1)) <= v) below++;
                    return new XgLogicQuestion
                    {
                        category = "2 的幂", categoryEn = "Powers of 2",
                        text = I(v) + " 是 2 的整数次幂吗？", textEn = "Is " + I(v) + " a power of 2?",
                        truth = truth,
                        why = truth ? I(v) + " 是 2 的 " + below + " 次方。" : "2 的 " + below + " 次方是 " + I(1L << below) + "，" + (below + 1) + " 次方是 " + I(1L << (below + 1)) + "，" + I(v) + " 夹在中间。",
                        whyEn = truth ? I(v) + " is 2 to the power " + below + "." : "2^" + below + " = " + I(1L << below) + " and 2^" + (below + 1) + " = " + I(1L << (below + 1)) + "; " + I(v) + " is in between.",
                        check = "pow2(" + I(v) + ") == 1",
                    };
                }
            }
        }

        /// <summary>One 2 too few or too many; for small powers also 2 × n and n squared (2^12 = 24 would fool nobody).</summary>
        static List<Slip> PowerSlips(int n)
        {
            var slips = new List<Slip> { new Slip(1L << (n - 1), "少乘了一个 2", "one 2 too few"), new Slip(1L << (n + 1), "多乘了一个 2", "one 2 too many") };
            if (n <= 6) slips.Add(new Slip(2 * n, "这是 2 × " + n, "that is 2 × " + n));
            if (n <= 9) slips.Add(new Slip((long)n * n, "这是 " + n + " 的平方", "that is " + n + " squared"));
            return slips;
        }

        static XgLogicQuestion Prime(Random r, int level, bool want)
        {
            // A "no" is an odd number that is not a multiple of 5, so it looks prime at a glance (91 = 7 × 13).
            var pick = new List<long>();
            for (long n = 51; n < 300; n += 2) if (IsPrime(n) == want && (want || n % 5 != 0)) pick.Add(n);
            long v = pick[r.Next(pick.Count)];
            long f = SmallestFactor(v);
            return new XgLogicQuestion
            {
                category = "质数", categoryEn = "Primes",
                text = I(v) + " 是质数吗？", textEn = "Is " + I(v) + " a prime?",
                truth = IsPrime(v),
                why = IsPrime(v) ? I(v) + " 只能被 1 和它自己整除。" : I(v) + " = " + I(f) + " × " + I(v / f) + "。",
                whyEn = IsPrime(v) ? "Only 1 and " + I(v) + " divide it." : I(v) + " = " + I(f) + " × " + I(v / f) + ".",
                check = "prime(" + I(v) + ") == 1",
            };
        }

        static bool IsPrime(long n) { if (n < 2) return false; for (long d = 2; d * d <= n; d++) if (n % d == 0) return false; return true; }
        static long SmallestFactor(long n) { for (long d = 2; d * d <= n; d++) if (n % d == 0) return d; return n; }

        // ───────────── 5 · percentages, fractions, squares ─────────────

        static XgLogicQuestion Percent(Random r, int level, bool want)
        {
            long[] rates = { 5, 10, 15, 20, 25, 30, 40, 50, 60, 75, 80 };
            long p = rates[r.Next(rates.Length)], unit = 100 / Gcd(p, 100);
            long baseValue = unit * r.Next((int)Math.Max(1, (20 + unit - 1) / unit), (int)(500 / unit) + 1);
            long real = baseValue * p / 100;
            var slips = new List<Slip> { new Slip(real * 10, "小数点挪错了一位", "the decimal point slipped"), Off(r, real, Math.Max(1, real / 10)) };
            if (p != 50) slips.Add(new Slip(baseValue - real, "这是剩下的 " + I(100 - p) + "%", "that is the other " + I(100 - p) + "%"));
            return Claim(r, want, "百分数", "Percent", I(baseValue) + " 的 " + I(p) + "% 是 {0} 吗？", "Is " + I(p) + "% of " + I(baseValue) + " equal to {0}?",
                I(baseValue) + " × " + I(p) + "%", I(baseValue) + " * " + I(p) + " / 100", real, slips);
        }

        static XgLogicQuestion Fraction(Random r, int level, bool want)
        {
            if (r.Next(2) == 0)
            {
                long den = r.Next(2, 10), num;
                do num = r.Next(1, (int)den); while (Gcd(num, den) != 1);
                long baseValue = den * r.Next(2, 13), real = baseValue / den * num;
                var slips = new List<Slip> { Off(r, real, 1), Off(r, real, 2) };
                if (num > 1) slips.Add(new Slip(baseValue / den, "只算了 1/" + I(den), "only 1/" + I(den) + " was taken"));
                if (2 * num != den) slips.Add(new Slip(baseValue - real, "这是剩下的 " + I(den - num) + "/" + I(den), "that is the other " + I(den - num) + "/" + I(den)));
                return Claim(r, want, "分数", "Fractions", I(baseValue) + " 的 " + I(num) + "/" + I(den) + " 是 {0} 吗？", "Is " + I(num) + "/" + I(den) + " of " + I(baseValue) + " equal to {0}?",
                    I(baseValue) + " ÷ " + I(den) + " × " + I(num), I(baseValue) + " * " + I(num) + " / " + I(den), real, slips);
            }
            long b = r.Next(2, 7), d = r.Next(2, 7), a = r.Next(1, (int)b), c = r.Next(1, (int)d);
            var sum = Frac.Of(a * d + c * b, b * d);
            var wrong = new List<(Frac f, string zh, string en)>
            {
                (Frac.Of(a + c, b + d), "分子加分子、分母加分母了", "tops and bottoms were added separately"),
                (Frac.Of(a + c, b + d), "分子加分子、分母加分母了", "tops and bottoms were added separately"),
                (Frac.Of(sum.n + 1, sum.d), "分子差了 1", "the top is off by 1"),
            };
            if (b != d) wrong.Add((Frac.Of(a + c, b * d), "通分后分子没跟着乘", "the tops were not scaled to the common bottom"));
            var choice = want ? (sum, "", "") : wrong.FindAll(w => !w.f.Equals(sum))[r.Next(wrong.FindAll(w => !w.f.Equals(sum)).Count)];
            string expr = I(a) + "/" + I(b) + " + " + I(c) + "/" + I(d);
            long m = b * d / Gcd(b, d);
            string steps = b == d ? "" : " = " + I(a * (m / b)) + "/" + I(m) + " + " + I(c * (m / d)) + "/" + I(m);
            bool truth = choice.Item1.Equals(sum);
            return new XgLogicQuestion
            {
                category = "分数", categoryEn = "Fractions",
                text = expr + " = " + choice.Item1 + " 吗？", textEn = "Is " + expr + " = " + choice.Item1 + "?",
                truth = truth,
                why = expr + steps + " = " + sum + (truth ? "。" : "，不是 " + choice.Item1 + "：" + choice.Item2 + "。"),
                whyEn = expr + steps + " = " + sum + (truth ? "." : ", not " + choice.Item1 + ": " + choice.Item3 + "."),
                check = expr + " == " + choice.Item1,
            };
        }

        static XgLogicQuestion Square(Random r, int level, bool want)
        {
            long n = r.Next(11, 31), real = n * n;
            if (r.Next(3) > 0)
                return Claim(r, want, "平方", "Squares", I(n) + " 的平方是 {0} 吗？", "Is " + I(n) + " squared equal to {0}?", I(n) + " × " + I(n), I(n) + " ^ 2", real,
                    new List<Slip> { new Slip((n - 1) * (n - 1), "这是 " + I(n - 1) + " 的平方", "that is " + I(n - 1) + " squared"), new Slip((n + 1) * (n + 1), "这是 " + I(n + 1) + " 的平方", "that is " + I(n + 1) + " squared"),
                        new Slip(n * 2, "这是 " + I(n) + " × 2", "that is " + I(n) + " × 2"), Swapped(real), Off(r, real, 10) });
            long v = want ? real : new[] { real + 1, real - 1, real + n, real + 10 }[r.Next(4)];
            bool truth = v == real;
            return new XgLogicQuestion
            {
                category = "平方", categoryEn = "Squares",
                text = I(v) + " 是完全平方数吗？", textEn = "Is " + I(v) + " a perfect square?",
                truth = truth,
                why = truth ? I(v) + " = " + I(n) + " × " + I(n) + "。" : I(n) + " × " + I(n) + " = " + I(real) + "，" + I(v) + " 不是哪个整数的平方。",
                whyEn = truth ? I(v) + " = " + I(n) + " × " + I(n) + "." : I(n) + " × " + I(n) + " = " + I(real) + "; " + I(v) + " is no whole number squared.",
                check = "square(" + I(v) + ") == 1",
            };
        }

        static long Gcd(long a, long b) { while (b != 0) { long t = a % b; a = b; b = t; } return Math.Abs(a); }

        /// <summary>A reduced fraction, written "5/6" (or "1" when whole).</summary>
        struct Frac : IEquatable<Frac>
        {
            public long n, d;
            public static Frac Of(long n, long d) { long g = Gcd(n, d); if (g == 0) g = 1; return new Frac { n = n / g, d = d / g }; }
            public bool Equals(Frac o) => n == o.n && d == o.d;
            public override bool Equals(object o) => o is Frac f && Equals(f);
            public override int GetHashCode() => (int)(n * 31 + d);
            public override string ToString() => d == 1 ? I(n) : I(n) + "/" + I(d);
        }
    }
}
