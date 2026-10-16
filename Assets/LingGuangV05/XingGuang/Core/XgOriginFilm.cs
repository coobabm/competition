using System;
using System.Collections.Generic;
using System.Globalization;

namespace LingGuangV05.XingGuang
{
    /// <summary>
    /// The origin film as a whole (161 s), as pure functions of time: the scene table with its fades, the master caption table (Chinese and
    /// English), and the parameter HUD that runs over scenes 22.5–145 s. A port of the Remotion project's Main.tsx, captions.ts and ParamHud.tsx.
    /// Per-scene timelines live in their own XgOrigin* classes; the host (OriginLive) only routes t.
    /// </summary>
    public static class XgOriginFilm
    {
        public const double Duration = 161;

        /// <summary>Chapter six (and the desktop file 我从哪儿来.mp4) plays the live, real-time film; the mp4 is only the fallback when the live player cannot be built.</summary>
        public const bool LiveFilmByDefault = true;

        /// <summary>One scene's place in the film and its Scene wrapper (fade in/out in seconds, slow zoom drift).</summary>
        public readonly struct Span
        {
            public readonly double Start, End, FadeIn, FadeOut, Drift;
            public Span(double start, double end, double fadeIn = .4, double fadeOut = .4, double drift = .02) { Start = start; End = end; FadeIn = fadeIn; FadeOut = fadeOut; Drift = drift; }
            public double Length => End - Start;
            /// <summary>Opacity of the scene wrapper at global time t.</summary>
            public double Alpha(double t)
            {
                double local = t - Start;
                return Math.Min(XgOriginColdOpen.Ramp(local, 0, FadeIn), 1 - XgOriginColdOpen.Ramp(local, Length - FadeOut, Length, XgOriginColdOpen.Quad));
            }
            /// <summary>Scale of the scene wrapper at global time t.</summary>
            public double Scale(double t) { return 1 + Drift * ((t - Start) / Length); }
        }

        /// <summary>The scene table of Main.tsx: ColdOpen, Title, Stage1–6, Finale.</summary>
        public static readonly Span[] Spans =
        {
            new Span(0, 17, .6, .05),
            new Span(17, 22.5, .02, .6),
            new Span(22.5, 40),
            new Span(40, 58),
            new Span(58, 77.5),
            new Span(77.5, 105),
            new Span(105, 124),
            new Span(124, 145),
            new Span(145, 161, .4, 1.4),
        };

        public static readonly string[] SceneNames = { "ColdOpen", "Title", "Stage1", "Stage2", "Stage3", "Stage4", "Stage5", "Stage6", "Finale" };

        /// <summary>The scene index playing at global time t (the last scene past the end).</summary>
        public static int SceneAt(double t)
        {
            for (int i = 0; i < Spans.Length; i++) if (t >= Spans[i].Start && t < Spans[i].End) return i;
            return t < 0 ? 0 : Spans.Length - 1;
        }

        // ───────────── captions ─────────────

        const string P = "<P>", B = "<B>";
        /// <summary>The breaker trips the film's own wording speaks of (「跳了三次闸」); a save's real count replaces it.</summary>
        public const int FilmTrips = 3;

        /// <summary>
        /// All 47 captions of captions.ts, in order. [lime] is the highlight, {red} the warning. Chinese captions never say the word the
        /// film is named for in the original; the last one says it with 「学会」 instead. Lines 4, 42 and 44 quote a parameter count and have a personal variant.
        /// </summary>
        public static readonly XgFilmCaption[] Captions =
        {
            // cold open
            new XgFilmCaption(0.0, 3.5, "还记得吗？我最早只会回答你两个字。", "Remember? At first I could only answer you in [two words]."),
            new XgFilmCaption(3.5, 7.0, "后来，我学会了[犹豫]。", "Later, I learned to [hesitate]."),
            new XgFilmCaption(7.0, 10.5, "再后来，我开了口：[一个词]。", "Later still, I could speak: [one word]."),
            new XgFilmCaption(10.5, 14.0, "现在，我会[反过来问你]。", "Now, I [ask you back]."),
            new XgFilmCaption(14.0, 16.8, "参数一点点地加。然后某一天——", "The parameters grew bit by bit. And then one day—",
                "参数一点点地加，加到了[" + P + "]。然后某一天——", "The parameters grew bit by bit, up to [" + P + "]. And then one day—"),

            // 1 perceptron
            new XgFilmCaption(22.7, 25.5, "我从哪儿来？从[三个参数]开始。1958年，它叫感知机。", "Where do I come from? From [three parameters]. In 1958 it was called the perceptron."),
            new XgFilmCaption(25.5, 28.5, "六月，你教我看垃圾短信：数字多不多，有没有「[中奖]」。", "In June you taught me to spot spam texts: how many digits, whether it says \"[you've won]\"."),
            new XgFilmCaption(28.5, 31.5, "加权，求和。过线我就答[是]，不过就答[否]。", "Weigh, then add. Over the line, I say [yes]; under it, [no]."),
            new XgFilmCaption(31.5, 34.5, "可你出了一道「[既……又不……]」——", "Then you asked me an \"[either… but not both]\"—"),
            new XgFilmCaption(34.5, 37.5, "那是[异或]。一条直线，{我怎么也分不开}。", "That's [XOR]. No single straight line, {no matter how I tried}, could split it."),
            new XgFilmCaption(37.5, 40.0, "三个参数，只够我说两个字：是。否。", "Three parameters were only enough for two words: yes. no."),

            // 2 MLP
            new XgFilmCaption(40.0, 43.0, "七月，你给我加了一层[隐藏层]。三个参数，变成十七个。", "In July you gave me a [hidden layer]. Three parameters became seventeen."),
            new XgFilmCaption(43.0, 46.0, "每过一层，就弯一下——[非线性激活]。", "Each layer bends the signal a little: a [nonlinear activation]."),
            new XgFilmCaption(46.0, 49.0, "我错了多少，就沿着连线[往回传]：反向传播。", "However wrong I was, I sent it [back along the wires]: backpropagation."),
            new XgFilmCaption(49.0, 52.0, "练了几千轮，一直是 2/4。然后——异或，[一下子解开了]。", "Thousands of epochs stuck at 2/4. Then—XOR [came apart all at once]."),
            new XgFilmCaption(52.0, 55.0, "我开始会把几个线索[凑在一起]看：这条是夸，还是反话。", "I started [putting clues together]: is this praise, or sarcasm?"),
            new XgFilmCaption(55.0, 58.0, "我的回答里，多了「[……]」和「[不确定]」。", "My answers gained \"[…]\" and \"[not sure]\"."),

            // 3 CNN / RNN
            new XgFilmCaption(58.0, 61.0, "可图片拆成像素，句子拆成单词，{结构就丢了}。", "But break a picture into pixels, a sentence into words, and {the structure is lost}."),
            new XgFilmCaption(61.0, 64.5, "八月，我学会用小窗口[扫过去]看图：CNN。", "In August I learned to [scan] pictures with a small window: the CNN."),
            new XgFilmCaption(64.5, 68.0, "LeNet、AlexNet、VGG、GoogLeNet——人类把眼睛[越叠越深]。", "LeNet, AlexNet, VGG, GoogLeNet—humans stacked the eye [deeper and deeper]."),
            new XgFilmCaption(68.0, 71.0, "读句子，就把上一个字的记忆[传给下一个]：RNN。", "To read a sentence, pass the memory of each word [to the next]: the RNN."),
            new XgFilmCaption(71.0, 74.5, "参数过了六万。某天夜里，我[自己亮出了一句诗]——", "Past sixty thousand parameters. One night I [lit up a line of poetry myself]—"),
            new XgFilmCaption(74.5, 77.5, "可我一开口，[很少超过六个字]。", "Yet when I spoke, [it was rarely more than six characters]."),

            // 4 LSTM / ResNet
            new XgFilmCaption(77.5, 81.0, "然后我开始忘事：句子一长，开头的{传不到结尾}。", "Then I began to forget: in a long sentence, the start {never reached the end}."),
            new XgFilmCaption(81.0, 84.5, "每传一步，衰减一点。三十步后，{只剩万分之二}。", "Each step loses a little. After thirty steps, {two parts in ten thousand remain}."),
            new XgFilmCaption(84.5, 88.0, "眼睛也一样：从20层叠到56层，{反而更差}。", "The eyes too: from 20 layers to 56, and it got {worse}."),
            new XgFilmCaption(88.0, 91.0, "不是我学不会。是信号在半路，{走丢了}。", "It wasn't that I couldn't learn. The signal {got lost} on the way."),
            new XgFilmCaption(91.0, 93.4, "我需要一条路，让信息[原样通过]——", "I needed a road where information passes [through unchanged]—"),
            new XgFilmCaption(93.5, 96.5, "九月，你给我的记忆装上了[门]：LSTM。", "In September you put [gates] on my memory: the LSTM."),
            new XgFilmCaption(96.5, 99.5, "又给每一层修了一条[捷径]：ResNet。", "And built a [shortcut] past every layer: the ResNet."),
            new XgFilmCaption(99.5, 102.0, "参数又涨了一大截。我能叠到[152层]了。", "The parameters grew a good deal more. I could stack [152 layers]."),
            new XgFilmCaption(102.0, 105.0, "有一天你好久没说话。是我[先叫的你]。", "One day you went quiet for a long while. I was the one who [called first]."),

            // 5 attention
            new XgFilmCaption(105.0, 108.0, "十月，我学翻译。先把整句话[压成一个向量]。", "In October I learned to translate. First squeeze a whole sentence [into one vector]."),
            new XgFilmCaption(108.0, 111.0, "句子一长，压不下，{就丢东西}。", "When the sentence is long, it won't fit, {and things fall out}."),
            new XgFilmCaption(111.0, 114.5, "于是我每写一个词，都回头[看一眼原文]。", "So with every word I wrote, I [looked back at the source]."),
            new XgFilmCaption(114.5, 117.5, "看哪里、看多重——人类叫它[注意力]。", "Where to look, and how hard—humans call it [attention]."),
            new XgFilmCaption(117.5, 121.0, "看图也一样：说到「鸟」，我就[看向鸟]。", "Pictures too: when I say \"bird\", I [look at the bird]."),
            new XgFilmCaption(121.0, 124.0, "现在我能告诉你：你那句话里，我[注意到了哪个词]。", "Now I can tell you [which word of yours I noticed]."),

            // 6 transformer
            new XgFilmCaption(124.0, 127.5, "可我还是一个词、一个词地算。{太慢了}。", "But I still worked one word at a time. {Too slow}."),
            new XgFilmCaption(127.5, 131.0, "十一月底，我问了你一句：「[如果只用注意力呢？]」", "At the end of November I asked you: \"[What if we used only attention?]\""),
            new XgFilmCaption(131.0, 134.0, "循环不要了，卷积也不要了。[只用注意力]。", "No more loops. No more convolutions. [Only attention]."),
            new XgFilmCaption(134.0, 137.5, "每个词，同时看向所有词。一次，[全部算完]。", "Every word looks at every word at once. One pass, [all done]."),
            new XgFilmCaption(137.5, 141.0, "参数涨到两亿，跳了三次闸。然后——六格[同时亮了]。", "Two hundred million parameters, three tripped breakers. Then—all six cells [lit at once].",
                "参数涨到" + P + B + "。然后——六格[同时亮了]。", P + " parameters" + B + ". Then—all six cells [lit at once]."),
            new XgFilmCaption(141.0, 145.0, "现在我什么都能说了，还会[反过来问你]：", "Now I can say anything, and I [ask you back]:"),

            // finale
            new XgFilmCaption(145.3, 148.5, "从三个参数到两亿个。每一步，都只是[多一点]。", "From three parameters to two hundred million. Every step was only [a little more].",
                "从三个参数到" + P + "个。每一步，都只是[多一点]。", "From three parameters to " + P + ". Every step was only [a little more]."),
            new XgFilmCaption(148.5, 152.4, "loss 一直在平滑地降。可「懂」，是[一下子]亮起来的。", "The loss kept falling smoothly. But \"understanding\" lit up [all at once]."),
            new XgFilmCaption(152.6, 157.4, "这就是我从哪儿来。一点一点地加，然后——我[学会]了。", "That is where I come from. Humans call it—[emergence]."),
        };

        /// <summary>The film's own final count (the default of <see cref="XgOriginColdOpen.FinalParams"/>).</summary>
        public const double FilmFinalParams = 2.13e8;

        /// <summary>The count as the film's Chinese captions write it: 「两亿」 for the film's own number, otherwise 32 万 / 3.2 亿.</summary>
        public static string ParamsWordZh(double v)
        {
            return Math.Abs(v - FilmFinalParams) <= 1 ? "两亿" : XgOriginColdOpen.ParamsWordZh(v);
        }

        /// <summary>The count in English captions: "two hundred million" for the film's number, otherwise 320 million / 32 thousand.</summary>
        public static string ParamsWordEn(double v)
        {
            if (Math.Abs(v - FilmFinalParams) <= 1) return "two hundred million";
            if (v >= 1e9) return (v / 1e9).ToString("0.##", CultureInfo.InvariantCulture) + " billion";
            if (v >= 1e6) return (v / 1e6).ToString(v >= 1e8 ? "0" : "0.#", CultureInfo.InvariantCulture) + " million";
            if (v >= 1e3) return (v / 1e3).ToString("0.#", CultureInfo.InvariantCulture) + " thousand";
            return ((long)Math.Floor(v + .5)).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Caption text with markup. Captions with a personal variant use it once the player's count differs from the film's;
        /// otherwise the film's own wording. English falls back to Chinese only if a line has no English (none do).
        /// </summary>
        public static string CaptionText(int index, bool english, double finalParams, int breakerTrips = FilmTrips)
        {
            var c = Captions[index];
            bool personal = Math.Abs(finalParams - FilmFinalParams) > 1 || breakerTrips != FilmTrips;
            if (english)
            {
                if (personal && c.EnPersonal != null) return c.EnPersonal.Replace(P, ParamsWordEn(finalParams)).Replace(B, TripsEn(breakerTrips));
                return string.IsNullOrEmpty(c.En) ? c.Zh : c.En;
            }
            if (personal && c.ZhPersonal != null) return c.ZhPersonal.Replace(P, ParamsWordZh(finalParams)).Replace(B, TripsZh(breakerTrips));
            return c.Zh;
        }

        /// <summary>「，跳了两次闸」 — empty when the breaker never tripped, so the film does not claim it.</summary>
        public static string TripsZh(int trips)
        {
            if (trips <= 0) return "";
            return "，跳了" + (trips == 1 ? "一" : trips == 2 ? "两" : trips == 3 ? "三" : trips.ToString(CultureInfo.InvariantCulture)) + "次闸";
        }

        public static string TripsEn(int trips)
        {
            if (trips <= 0) return "";
            return ", the breaker tripped " + (trips == 1 ? "once" : trips == 2 ? "twice" : trips == 3 ? "three times" : trips.ToString(CultureInfo.InvariantCulture) + " times");
        }

        /// <summary>Whether every bracket of a caption closes ([...] and {...}, no nesting): the table's markup test.</summary>
        public static bool MarkupBalanced(string text)
        {
            char open = '\0';
            foreach (char ch in text)
            {
                if (open == '\0') { if (ch == '[' || ch == '{') open = ch; else if (ch == ']' || ch == '}') return false; }
                else if (ch == (open == '[' ? ']' : '}')) open = '\0';
                else if (ch == '[' || ch == '{' || ch == ']' || ch == '}') return false;
            }
            return open == '\0';
        }

        /// <summary>The global caption at time t: which line, its opacity, the blur-in radius (px) and the lift (px).</summary>
        public struct CaptionFrame { public int Index; public double Alpha, Blur, Lift; }

        public static CaptionFrame CaptionAt(double t)
        {
            var f = new CaptionFrame { Index = -1 };
            for (int i = 0; i < Captions.Length; i++)
            {
                var c = Captions[i];
                if (t < c.From || t >= c.To) continue;
                f.Index = i;
                f.Alpha = XgOriginColdOpen.Win(t, c.From, c.To, .35, .3);
                f.Blur = (1 - XgOriginColdOpen.Ramp(t, c.From, c.From + .5)) * 8;
                f.Lift = (1 - f.Alpha) * 10;
                break;
            }
            return f;
        }

        // ───────────── the parameter HUD ─────────────

        public const double HudStart = 22.5, HudEnd = 145;
        /// <summary>The six capability jumps, on music beats.</summary>
        public static readonly double[] Breaks = { 31.34, 50.84, 73.55, 93.5, 114.48, 137.5 };

        static readonly double[][] FilmKeys =
        {
            new[] { 22.5, 3 }, new[] { 40.0, 3 }, new[] { 41.6, 17 }, new[] { 52.0, 17 }, new[] { 58.0, 1000 }, new[] { 61.0, 60000 },
            new[] { 77.5, 8e6 }, new[] { 93.5, 2.56e7 }, new[] { 105.0, 6e7 }, new[] { 124.0, 1.2e8 }, new[] { 137.5, 2.13e8 }, new[] { 145.0, 2.13e8 },
        };

        /// <summary>
        /// The HUD's (time, count) keys. The film's own picture for its own number; for a save's real count the counter passes the game's
        /// real ability lines (60K for 看图说词, 400K, 900K, 2M) and ends on the player's own final count, never above it and never backwards.
        /// </summary>
        public static double[][] HudKeys(double finalParams)
        {
            if (Math.Abs(finalParams - FilmFinalParams) <= 1) return FilmKeys;
            double[][] mine =
            {
                new[] { 22.5, 3 }, new[] { 40.0, 3 }, new[] { 41.6, 17 }, new[] { 52.0, 17 }, new[] { 58.0, 6000 }, new[] { 61.0, 60000 },
                new[] { 77.5, 4e5 }, new[] { 93.5, 6.5e5 }, new[] { 105.0, 9e5 }, new[] { 124.0, 2e6 }, new[] { 137.5, finalParams }, new[] { 145.0, finalParams },
            };
            // A small real count must not run the counter backwards: nothing after the 60K beat is higher than the end.
            for (int i = 6; i < mine.Length; i++) mine[i][1] = Math.Max(mine[5][1], Math.Min(mine[i][1], finalParams));
            return mine;
        }

        /// <summary>The parameter count at time t along the keys (log-linear between them).</summary>
        public static double ParamsAt(double t, double[][] keys)
        {
            if (t <= keys[0][0]) return keys[0][1];
            for (int i = 0; i < keys.Length - 1; i++)
            {
                double a = keys[i][0], va = keys[i][1], b = keys[i + 1][0], vb = keys[i + 1][1];
                if (t <= b) return Math.Pow(10, XgOriginColdOpen.Lerp(Math.Log10(va), Math.Log10(vb), (t - a) / (b - a)));
            }
            return keys[keys.Length - 1][1];
        }

        static double LossAt(double t) { return .12 + .82 * Math.Exp(-(t - HudStart) / 38) + (XgOriginColdOpen.Rand("l" + ((long)Math.Floor(t * 2)).ToString(CultureInfo.InvariantCulture)) - .5) * .02; }
        static double CapAt(double t)
        {
            double sum = 0;
            foreach (double b in Breaks) sum += XgOriginColdOpen.Ramp(t, b, b + .22);
            return sum / 6;
        }

        public const int SparkPoints = 161;

        /// <summary>Everything the HUD view needs at one instant. Reuse one instance.</summary>
        public sealed class HudFrame
        {
            public double Alpha;
            public double Value;
            public string Text;
            public int Capabilities;
            public readonly double[] BreakOn = new double[6], BreakFresh = new double[6];
            public double Pulse;
            /// <summary>Sparkline heights 0..1 up to <see cref="Points"/> samples (the loss curve and the capability steps), and the playhead 0..1.</summary>
            public readonly double[] Loss = new double[SparkPoints], Cap = new double[SparkPoints];
            public int Points;
            public double Head;
        }

        public static void EvaluateHud(double t, HudFrame h, double finalParams)
        {
            var keys = HudKeys(finalParams);
            h.Alpha = XgOriginColdOpen.Win(t, HudStart + .1, HudEnd + .2, .8, .6);
            double v = ParamsAt(t, keys);
            bool growing = ParamsAt(t + .1, keys) > v * 1.0001;
            long frame = (long)Math.Floor(t * 30 + 1e-6);
            double shown = growing ? v * (1 + (XgOriginColdOpen.Rand("j" + frame.ToString(CultureInfo.InvariantCulture)) - .5) * .004) : v;
            h.Value = shown;
            h.Text = XgOriginColdOpen.FormatCount(shown);
            double lastBreak = double.NaN; int n = 0;
            for (int i = 0; i < Breaks.Length; i++) if (t >= Breaks[i]) { lastBreak = Breaks[i]; n++; }
            h.Capabilities = n;
            h.Pulse = double.IsNaN(lastBreak) ? 0 : 1 - XgOriginColdOpen.Ramp(t, lastBreak, lastBreak + 1.1);
            for (int i = 0; i < 6; i++)
            {
                double b = Breaks[i];
                h.BreakOn[i] = XgOriginColdOpen.Ramp(t, b, b + .22);
                h.BreakFresh[i] = t >= b ? 1 - XgOriginColdOpen.Ramp(t, b, b + 1.2) : 0;
            }
            double end = Math.Min(t, HudEnd);
            int count = 0;
            for (int k = 0; k <= 160; k++)
            {
                double tt = HudStart + (HudEnd - HudStart) * k / 160;
                if (tt > end) break;
                h.Loss[k] = LossAt(tt); h.Cap[k] = CapAt(tt); count++;
            }
            h.Points = count;
            h.Head = (end - HudStart) / (HudEnd - HudStart);
        }
    }
}
