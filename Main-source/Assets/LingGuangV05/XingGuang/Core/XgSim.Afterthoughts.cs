using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>Which one-time afterthoughts of the crowd-labelling arc have already played. Saved with the lab.</summary>
    public sealed partial class XgState
    {
        public List<string> afterthoughts = new List<string>();
    }

    /// <summary>
    /// The second act of the auto-labelling arc, told in the protagonist's own head: the first money the model earns
    /// alone, the first fine, the first report, the first 金标题, the first captcha and so on, each said once per save.
    /// The desktop decides when and how a line is shown; this only keeps the lines and remembers which were said.
    /// At stage 5 the AI itself asks, once, who its labels are teaching.
    /// </summary>
    public sealed partial class XgSim
    {
        public sealed class XgAfterthought
        {
            public string key, zh, en;
            public XgAfterthought(string key, string zh, string en) { this.key = key; this.zh = zh; this.en = en; }
        }

        /// <summary>Automatic labels the model must have got right before it wonders about them (stage 5 and up).</summary>
        public const int ReflectionLabels = 300;
        public const string ReflectionKey = "ai.reflection";

        static readonly Dictionary<string, string[]> AfterthoughtLines = new Dictionary<string, string[]>
        {
            // key: zh lines, then en lines, joined by '|'
            { "first.income", new[] { "……它在挣钱。我什么都没点。|躺着也能挣钱？", "...It's earning. I didn't click anything.|Money while lying down?" } },
            { "first.fine", new[] { "扣钱了？|……它也会错。", "A fine?|...It makes mistakes too." } },
            { "first.warning", new[] { "合格率在往下掉。|得盯着点它。", "The pass rate is slipping.|I'd better keep an eye on it." } },
            { "first.report", new[] { "被举报了……|人家看得出来，这不是人在标。", "Reported...|They can tell no person is labelling this." } },
            { "first.trap", new[] { "金标题……|他们早就知道会有人拿机器糊弄。", "Trap items...|They always knew someone would try a machine." } },
            { "first.captcha", new[] { "验证码？|……歪歪扭扭的数字。它第一天学的就是这个。", "A captcha?|...Wobbly handwritten digits. The first thing it ever learned." } },
            { "first.flagged", new[] { "「答题时间过于规律」。|人哪有这么准时。", "\"Answer timing too regular.\"|No person is that punctual." } },
            { "first.drift", new[] { "「{0}」？它没见过这个词。|……我也是今天才见到。", "\"{0}\"? It has never seen that word.|...Neither had I, until today." } },
            { "first.fivestar", new[] { "五星好评。|甲方不知道，替他们标数据的是一台家里的电脑。", "Five stars.|The client has no idea a home PC labelled their data." } },
            { "first.docked", new[] { "尾款扣了一半。|差一点点就是差。", "Half the balance docked.|Almost is not enough." } },
            { "first.hire", new[] { "{0}也来挣这个钱了。|……现在我成了包工头。", "{0} is in on it now too.|...So now I'm the foreman." } },
            { "first.bot", new[] { "「疑似机器操作」。|……也没说错。", "\"Suspected bot.\"|...They're not wrong." } },
        };

        /// <summary>The line for an afterthought that has not been said yet, or null. Does not mark it said.</summary>
        public XgAfterthought PeekAfterthought(string key, string argZh = null, string argEn = null)
        {
            if (string.IsNullOrEmpty(key) || !AfterthoughtLines.TryGetValue(key, out var lines) || AfterthoughtSaid(key)) return null;
            string zh = lines[0].Replace("{0}", argZh ?? ""), en = lines[1].Replace("{0}", argEn ?? argZh ?? "");
            return new XgAfterthought(key, zh, en);
        }

        /// <summary>Records that the player has seen an afterthought, so it never plays again in this save.</summary>
        public void MarkAfterthought(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (S.afterthoughts == null) S.afterthoughts = new List<string>();
            if (!S.afterthoughts.Contains(key)) S.afterthoughts.Add(key);
        }

        /// <summary>The line for an afterthought, or null if it has been said already. Marks it said.</summary>
        public XgAfterthought TakeAfterthought(string key, string argZh = null, string argEn = null)
        {
            var thought = PeekAfterthought(key, argZh, argEn);
            if (thought != null) MarkAfterthought(key);
            return thought;
        }

        public bool AfterthoughtSaid(string key) => S.afterthoughts != null && S.afterthoughts.Contains(key);

        /// <summary>Every afterthought key, for tests.</summary>
        public static IEnumerable<string> AfterthoughtKeys => AfterthoughtLines.Keys;

        /// <summary>
        /// Once per save, from stage 5, after the model has labelled enough on its own: it asks who those labels teach.
        /// Returns the line (already in the current language) and adds it to the 对话 log, or null if not due.
        /// </summary>
        public string TakeReflectionLine()
        {
            if (S.stage < 5 || S.autoCorrect < ReflectionLabels || AfterthoughtSaid(ReflectionKey)) return null;
            if (S.afterthoughts == null) S.afterthoughts = new List<string>();
            S.afterthoughts.Add(ReflectionKey);
            string line = T("我标的那些，是在教谁？", "The cards I label. Who are they teaching?");
            AddLine("ai", line);
            return line;
        }

        void RepairAfterthoughts()
        {
            if (S.afterthoughts == null) S.afterthoughts = new List<string>();
            S.afterthoughts.RemoveAll(k => string.IsNullOrEmpty(k));
        }
    }
}
