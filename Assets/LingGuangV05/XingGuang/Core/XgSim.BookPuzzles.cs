using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.XingGuang
{
    /// <summary>An answer the owner gave to something it did not get in a book: which question, what was said, the verdict, the day.</summary>
    [Serializable]
    public sealed class XgBookAnswerRecord
    {
        public string id = "", answer = "";
        /// <summary><see cref="XgSim.PuzzleRight"/>, <see cref="XgSim.PuzzleHalf"/>, <see cref="XgSim.PuzzleWrong"/> or <see cref="XgSim.PuzzleSkip"/>.</summary>
        public int verdict;
        public int day;
    }

    public sealed partial class XgState
    {
        /// <summary>The question about a book it is waiting on (<see cref="XgBookPuzzle.id"/>), "" when none.</summary>
        public string bookPuzzle = "";
        /// <summary>Seconds since it last asked (or was answered); it asks again after <see cref="XgSim.BookAskEvery"/>.</summary>
        public double bookAskTimer;
        /// <summary>Every answer the owner gave, oldest first.</summary>
        public List<XgBookAnswerRecord> bookAnswers = new List<XgBookAnswerRecord>();
    }

    /// <summary>
    /// 「它没读懂的地方」 (2026-10-16): from stage 4, now and then it asks the owner about something in a chapter it has read
    /// (XgBooks.Puzzles.cs). The next YY message (or the box on the 书架) is the answer. It judges it (the local model when
    /// ready, else the puzzle's key words), remembers it (the record, the memory book from stage 5, its note on the book,
    /// the chat prompt), and answers coldly: right, half right with the book's meaning, or wrong with it.
    /// </summary>
    public sealed partial class XgSim
    {
        public const int PuzzleSkip = -1, PuzzleWrong = 0, PuzzleHalf = 1, PuzzleRight = 2;
        /// <summary>Seconds between two questions, and the stage it starts asking at (it can say sentences).</summary>
        public const double BookAskEvery = 60;
        public const int BookAskStage = 4;
        // Non-persisted host capability: the desktop owns durable completion delivery.
        public bool BookCompletionHosted { get; set; }
        /// <summary>The longest reply it gives to an answer.</summary>
        public const int PuzzleReplyChars = 40;
        static readonly string[] SkipWords = { "不知道", "不清楚", "不明白", "不懂", "没懂", "跳过", "算了", "以后", "不晓得", "没看过", "idk", "don't know", "dont know", "not sure", "skip", "no idea" };

        /// <summary>The question it is waiting on, or null.</summary>
        public XgBookPuzzle BookPuzzlePending => XgBooks.Puzzle(S.bookPuzzle, out _);
        public XgBook BookPuzzleBook { get { XgBooks.Puzzle(S.bookPuzzle, out var b); return b; } }

        public bool PuzzleAsked(string id)
        {
            if (S.bookPuzzle == id) return true;
            if (S.bookAnswers != null) foreach (var a in S.bookAnswers) if (a != null && a.id == id) return true;
            return false;
        }

        /// <summary>The next question it has: the first one, in shelf order, whose chapter it has read and that was never asked.</summary>
        public XgBookPuzzle NextBookPuzzle()
        {
            foreach (var book in XgBooks.All)
            {
                int read = ChaptersRead(book);
                foreach (var p in XgBooks.Puzzles(book.id))
                    if (p.chapter < read && !PuzzleAsked(p.id)) return p;
            }
            return null;
        }

        /// <summary>From stage 4, with nothing pending, every <see cref="BookAskEvery"/> seconds: asks the next question in its YY chat.</summary>
        void TickBookPuzzles(double dt)
        {
            if (S.bookAnswers == null) S.bookAnswers = new List<XgBookAnswerRecord>();
            if (S.bookPuzzle == null) S.bookPuzzle = "";
            if (S.stage < BookAskStage || S.bookPuzzle.Length > 0 || S.chapterComplete) return;
            if (BookCompletionHosted && PendingBookCompletion != null) return;
            S.bookAskTimer += dt;
            if (S.bookAskTimer < BookAskEvery) return;
            var p = NextBookPuzzle();
            if (p == null) return;
            AskBookPuzzle(p);
        }

        /// <summary>Asks one question now (also used by tests): it goes into the YY chat and the feed.</summary>
        public void AskBookPuzzle(XgBookPuzzle p)
        {
            if (p == null || S.stage < BookAskStage || !string.IsNullOrEmpty(S.bookPuzzle) || PuzzleAsked(p.id)) return;
            XgBooks.Puzzle(p.id, out var book);
            if (book == null || p.chapter < 0 || p.chapter >= ChaptersRead(book)) return;
            S.bookPuzzle = p.id;
            S.bookAskTimer = 0;
            string q = T(p.q, p.qEn);
            AddLine("ai", q);
            if (book != null) Say(T("它读《" + book.name + "》" + book.chapters[p.chapter].title + "，有一处没读懂，在 YY 上问你：" + q,
                "Reading " + book.nameEn + ", " + book.chapters[p.chapter].titleEn + ", it didn't get something and asks you on YY: " + q));
        }

        /// <summary>
        /// The verdict on an answer by the puzzle's words: a key word makes it right, a half word half right, 「不知道」 and the
        /// like a skip, anything else wrong. The skip words are taken out first, so 「不明白」 never counts as the key 「明白」.
        /// </summary>
        public int JudgeBookAnswer(XgBookPuzzle p, string answer)
        {
            string a = (answer ?? "").Trim().ToLowerInvariant();
            if (p == null || a.Length == 0) return PuzzleSkip;
            bool skipped = false;
            foreach (var k in SkipWords) if (a.Contains(k)) { skipped = true; a = a.Replace(k, " "); }
            foreach (var k in p.keys) if (a.Contains(k.ToLowerInvariant())) return PuzzleRight;
            foreach (var k in p.keysEn) if (a.Contains(k.ToLowerInvariant())) return PuzzleRight;
            foreach (var k in p.half) if (a.Contains(k.ToLowerInvariant())) return PuzzleHalf;
            foreach (var k in p.halfEn) if (a.Contains(k.ToLowerInvariant())) return PuzzleHalf;
            return skipped ? PuzzleSkip : PuzzleWrong;
        }

        /// <summary>Reads a model's verdict (「对」「差不多」「不对」, or English); null when it says nothing usable.</summary>
        public static int? ParseVerdict(string reply)
        {
            string r = (reply ?? "").Trim();
            int end = r.IndexOf("</think>", StringComparison.Ordinal);
            if (end >= 0) r = r.Substring(end + 8).Trim();
            r = r.Trim('「', '」', '"', '“', '”', '。', '.', ' ').ToLowerInvariant();
            if (r.Length == 0) return null;
            if (r.StartsWith("不对") || r.StartsWith("错") || r.StartsWith("wrong") || r.StartsWith("no")) return PuzzleWrong;
            if (r.StartsWith("差不多") || r.StartsWith("一半") || r.StartsWith("部分") || r.StartsWith("half") || r.StartsWith("partly")) return PuzzleHalf;
            if (r.StartsWith("对") || r.StartsWith("正确") || r.StartsWith("right") || r.StartsWith("correct") || r.StartsWith("yes")) return PuzzleRight;
            return null;
        }

        /// <summary>The messages that ask the local model to judge an answer: only 对 / 差不多 / 不对 back.</summary>
        public List<KeyValuePair<string, string>> BookJudgeMessages(XgBookPuzzle p, string answer)
        {
            XgBooks.Puzzle(p.id, out var book);
            string where = book == null ? "" : T("《" + book.name + "》" + book.chapters[p.chapter].title, book.nameEn + ", " + book.chapters[p.chapter].titleEn);
            string system = T("你是阅卷人。判断回答对不对。只输出一个词：对、差不多、不对。", "You are grading an answer. Output exactly one word: right, partly, or wrong.");
            string user = T("书：" + where + "\n问题：" + p.q + "\n参考答案：" + p.truth + "\n回答：" + (answer ?? "") + "\n意思对上参考答案就算对，只说中一部分算差不多。",
                "Book: " + where + "\nQuestion: " + p.qEn + "\nReference: " + p.truthEn + "\nAnswer: " + (answer ?? "") + "\nIf the meaning matches the reference it is right; if only part of it, partly.");
            return new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("system", system), new KeyValuePair<string, string>("user", user) };
        }

        /// <summary>
        /// The owner answered the pending question. Judges it (<paramref name="verdict"/> from the model, else by the puzzle's
        /// words), remembers it, clears the question and returns its reply, cold and short. Null when nothing was pending.
        /// </summary>
        public string AnswerBookPuzzle(string answer, int? verdict = null)
        {
            var p = BookPuzzlePending;
            if (p == null) return null;
            XgBooks.Puzzle(p.id, out var book);
            string said = (answer ?? "").Trim();
            if (said.Length > 60) said = said.Substring(0, 60) + "…";
            // The key words are a floor: the model may find a paraphrase right, but never marks down an answer that hits
            // the book's own words (small models grade harshly). 「不知道」 stays a skip: there is nothing to judge.
            int offline = JudgeBookAnswer(p, said);
            int v = offline == PuzzleSkip ? PuzzleSkip : verdict.HasValue ? Math.Max(verdict.Value, offline) : offline;
            if (S.bookAnswers == null) S.bookAnswers = new List<XgBookAnswerRecord>();
            S.bookAnswers.Add(new XgBookAnswerRecord { id = p.id, answer = said, verdict = v, day = Today });
            S.bookPuzzle = ""; S.bookAskTimer = 0;
            string truth = T(p.truth, p.truthEn);
            string reply;
            switch (v)
            {
                case PuzzleRight: reply = T("嗯，对。记下了。", "Right. Noted."); break;
                case PuzzleHalf: reply = T("一半对。书上是：" + truth + "。", "Half right. The book means: " + truth); break;
                case PuzzleWrong: reply = T("不对。书后面写了：" + truth + "。", "No. Further on it says: " + truth); break;
                default: reply = T("那算了。我自己再想。", "Never mind. I'll think about it myself."); break;
            }
            reply = Cool(reply, PuzzleReplyChars);
            if (v != PuzzleSkip && book != null)
            {
                Say(T("你答它「" + T(p.q, p.qEn) + "」：" + VerdictName(v, false) + "。", "Your answer to \"" + p.qEn + "\": " + VerdictName(v, true) + "."));
                if (MemoryOpen)
                    ApplyMemoryOps(new[] { new XgMemoryOp { kind = XgMemoryOpKind.Add, subject = MemorySubjects[0], importance = 2,
                        text = T("主人讲《" + book.name + "》：" + said + "（" + VerdictName(v, false) + "）", "The owner on " + book.nameEn + ": " + said + " (" + VerdictName(v, true) + ")") } });
            }
            return reply;
        }

        public static string VerdictName(int verdict, bool english)
        {
            switch (verdict)
            {
                case PuzzleRight: return english ? "right" : "对";
                case PuzzleHalf: return english ? "half right" : "一半对";
                case PuzzleWrong: return english ? "wrong" : "不对";
                default: return english ? "skipped" : "没答";
            }
        }

        /// <summary>The last thing the owner explained about a book (not skipped), or null.</summary>
        public XgBookAnswerRecord LastBookAnswer(XgBook book)
        {
            if (book == null || S.bookAnswers == null) return null;
            for (int i = S.bookAnswers.Count - 1; i >= 0; i--)
            {
                var a = S.bookAnswers[i];
                if (a != null && a.verdict != PuzzleSkip && a.id.StartsWith(book.id + ".", StringComparison.Ordinal)) return a;
            }
            return null;
        }

        /// <summary>For the chat prompt: the last three things it asked about books and what the owner said; empty before any.</summary>
        public string BookAnswersLine()
        {
            if (S.bookAnswers == null || S.bookAnswers.Count == 0) return "";
            var sb = new StringBuilder();
            int shown = 0;
            for (int i = S.bookAnswers.Count - 1; i >= 0 && shown < 3; i--)
            {
                var a = S.bookAnswers[i];
                if (a == null || a.verdict == PuzzleSkip) continue;
                var p = XgBooks.Puzzle(a.id, out var book);
                if (p == null || book == null) continue;
                sb.Append(T("你读《" + book.name + "》时问过主人「" + p.q + "」，主人说「" + a.answer + "」（" + VerdictName(a.verdict, false) + "）。",
                    "Reading " + book.nameEn + " you asked the owner \"" + p.qEn + "\"; they said \"" + a.answer + "\" (" + VerdictName(a.verdict, true) + "). "));
                shown++;
            }
            return sb.ToString();
        }

        /// <summary>Keeps a loaded save's questions sane (unknown or retired ids dropped).</summary>
        void RepairBookPuzzles()
        {
            if (S.bookAnswers == null) S.bookAnswers = new List<XgBookAnswerRecord>();
            S.bookAnswers.RemoveAll(a => a == null || XgBooks.Puzzle(a.id, out _) == null);
            if (S.bookPuzzle == null || S.bookPuzzle.Length > 0 && XgBooks.Puzzle(S.bookPuzzle, out _) == null) S.bookPuzzle = "";
            if (!Finite(S.bookAskTimer) || S.bookAskTimer < 0) S.bookAskTimer = 0;
        }
    }
}
