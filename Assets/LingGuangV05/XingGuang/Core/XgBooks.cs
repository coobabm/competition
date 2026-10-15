using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>One chapter (篇 / 回 / 首组) of a book on the 书架: a short public-domain excerpt and what it can say about it.</summary>
    public sealed class XgBookChapter
    {
        /// <summary>The chapter's own title in the original (回目, 篇名); short ones get a plain label.</summary>
        public string title = "";
        public string titleEn = "";
        /// <summary>A short excerpt of the original text (40–160 characters), shown scrolling in while it reads.</summary>
        public string excerpt = "";
        /// <summary>About how many characters the whole chapter has: what one read is worth in samples.</summary>
        public int chars;
        /// <summary>1–3 people or images of the chapter (ability 2 picks one: 「猴子？」).</summary>
        public string[] names = new string[0];
        /// <summary>Three key words (ability 3 strings them: 「猴子 / 打架 / 佛」).</summary>
        public string[] words = new string[0];
        /// <summary>One plain sentence in its own voice, at most about 18 characters (ability 4).</summary>
        public string gist = "";
        public string gistEn = "";
        /// <summary>A theme from <see cref="XgBooks.Tags"/>; ability 5 links chapters of different books that share it.</summary>
        public string tag = "";
    }

    /// <summary>A quiz question on a book: right when it has read enough of it (and is able enough), wrong in a plausible way otherwise.</summary>
    public sealed class XgBookQuiz
    {
        public string q = "", qEn = "";
        public string answer = "", answerEn = "";
        /// <summary>Plausible wrong answers (2–3), in the same order in both languages.</summary>
        public string[] wrong = new string[0], wrongEn = new string[0];
        /// <summary>The chapter (0-based) that answers it: it can only know once that chapter is read.</summary>
        public int chapter;
    }

    /// <summary>A short line of the book it may quote in chat when the topic comes up.</summary>
    public sealed class XgBookQuote
    {
        /// <summary>The original words, at most about 16 characters.</summary>
        public string text = "";
        /// <summary>Plain gloss for English play.</summary>
        public string textEn = "";
        /// <summary>Chat topics (keys of <see cref="XgBooks.Topics"/>) it fits.</summary>
        public string[] topics = new string[0];
    }

    public sealed class XgBook
    {
        public string id = "", name = "", nameEn = "", author = "", authorEn = "";
        /// <summary>The ability that opens it on the shelf (2–5).</summary>
        public int ability = 2;
        /// <summary>Parameters (K) it adds once every chapter is read.</summary>
        public double paramsK;
        /// <summary>The corpus its characters go into (1 character = 1 sample).</summary>
        public string dataset = "longtext";
        /// <summary>Its own one-line note on the whole book (≤ 40 characters), written as it would at ability 6.</summary>
        public string note = "", noteEn = "";
        public XgBookChapter[] chapters = new XgBookChapter[0];
        public XgBookQuiz[] quiz = new XgBookQuiz[0];
        public XgBookQuote[] quotes = new XgBookQuote[0];
        public int TotalChars { get { int n = 0; foreach (var c in chapters) n += c.chars; return n; } }
    }

    /// <summary>
    /// The 书架 (名著喂养与结局_设计.md §一): eight public-domain Chinese classics, fed to it one chapter at a time.
    /// The texts themselves are only short excerpts (XgBooks.Data.cs).
    /// </summary>
    public static partial class XgBooks
    {
        /// <summary>Themes a chapter can carry; ability 5 says two chapters of different books share one.</summary>
        public static readonly string[] Tags =
        {
            "打架", "吃喝", "离别", "做官", "师徒", "兄弟", "梦", "月亮", "家", "读书", "死", "钱", "酒", "病", "孤独", "反抗", "命运", "骗", "女人", "孩子"
        };
        public static readonly string[] TagsEn =
        {
            "fighting", "food and drink", "parting", "officials", "master and pupil", "sworn brothers", "dreams", "the moon", "home", "study", "death", "money", "wine", "illness", "loneliness", "rebellion", "fate", "tricks", "women", "children"
        };

        /// <summary>Chat topics a quote can fit, each with the words in a message that bring it up.</summary>
        public static readonly Dictionary<string, string[]> Topics = new Dictionary<string, string[]>
        {
            { "吃", new[] { "吃", "饭", "饿", "外卖", "泡面", "早饭", "晚饭", "eat", "food", "hungry" } },
            { "钱", new[] { "钱", "房租", "工资", "穷", "花", "贵", "账", "money", "rent", "broke" } },
            { "睡", new[] { "睡", "困", "熬夜", "失眠", "晚安", "sleep", "tired", "night" } },
            { "学", new[] { "学", "考", "练", "读", "书", "作业", "learn", "study", "exam", "read" } },
            { "朋友", new[] { "朋友", "兄弟", "老周", "阿杰", "小刚", "friend", "buddy" } },
            { "家", new[] { "家", "妈", "爸", "回家", "老家", "home", "mom", "dad" } },
            { "累", new[] { "累", "烦", "难", "撑", "崩", "tired", "hard", "stress" } },
            { "夜", new[] { "夜", "月", "凌晨", "天黑", "星", "moon", "dark", "midnight" } },
            { "生气", new[] { "气", "火", "骂", "讨厌", "滚", "angry", "hate", "mad" } },
            { "怕", new[] { "怕", "担心", "慌", "吓", "afraid", "scared", "worry" } },
            { "离别", new[] { "走", "再见", "离开", "关机", "删", "bye", "leave", "shut down" } },
            { "自己", new[] { "你是谁", "你是什么", "你自己", "活", "意识", "who are you", "alive", "yourself" } },
        };

        public static IReadOnlyList<XgBook> All => all;
        public static XgBook Get(string id) { foreach (var b in all) if (b.id == id) return b; return null; }
        public static int IndexOf(string id) { for (int i = 0; i < all.Length; i++) if (all[i].id == id) return i; return -1; }
        public static string TagWord(string tag, bool english)
        {
            int i = Array.IndexOf(Tags, tag);
            return i < 0 ? tag : english ? TagsEn[i] : Tags[i];
        }

        /// <summary>The topics a chat message touches (keys of <see cref="Topics"/>).</summary>
        public static List<string> TopicsOf(string message)
        {
            var found = new List<string>();
            if (string.IsNullOrEmpty(message)) return found;
            string m = message.ToLowerInvariant();
            foreach (var kv in Topics)
                foreach (var w in kv.Value)
                    if (m.Contains(w.ToLowerInvariant())) { found.Add(kv.Key); break; }
            return found;
        }
    }
}
