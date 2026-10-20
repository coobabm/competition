using System;
using System.Collections.Generic;
using System.Text;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        /// <summary>The 书架: per book (key = <see cref="XgBook.id"/>) the chapters read (count) and the day it was finished (value, yyyymmdd; 0 = not yet).</summary>
        public List<XgScore> books = new List<XgScore>();
        /// <summary>The book being read right now ("" = none), the chapter, and its bar: effective seconds read of the planned length.</summary>
        public string bookReading = "";
        public int bookChapter;
        public double bookClock, bookDuration;
        /// <summary>What it said after the last chapter it read, for the shelf's bubble (book id, chapter, line).</summary>
        public string bookLineBook = "", bookLine = "";
        public int bookLineChapter = -1;
        /// <summary>红楼梦 stops at 第八十回: 0 = not there yet, 1 = it asked 「后面呢？」, 2 = told, it said 「那我也续一回」.</summary>
        public int redMansionBeat;
    }

    /// <summary>A shelf quiz answer: the question, what it said, and whether that was right.</summary>
    public sealed class XgBookAnswer
    {
        public XgBook book;
        public string question = "", said = "", answer = "";
        public bool right;
    }

    /// <summary>
    /// The 书架 (名著喂养与结局_设计.md §一、§二): feed it the classics one chapter at a time.
    /// <list type="bullet">
    /// <item>A chapter reads like a short training round: a bar of <see cref="ReadSeconds"/> on the GPU, billed as training power,
    /// at half speed while a training round shares the card.</item>
    /// <item>A finished chapter books its characters 1:1 as samples of the book's corpus (古文、诗 → 全唐诗, 小说 → 长句理解) and it
    /// says what it made of it, by ability (<see cref="BookReaction"/>).</item>
    /// <item>A finished book adds <see cref="XgBook.paramsK"/> straight to the parameter bar (<see cref="BookParamsK"/>): on purpose
    /// outside the grade-C rule, it really grew. It also unlocks the book's quotes in chat and gives a card.</item>
    /// </list>
    /// </summary>
    public sealed partial class XgSim
    {
        /// <summary>Characters a chapter reads per second at ability 2 on one card; a chapter takes <see cref="ReadMinSeconds"/>–<see cref="ReadMaxSeconds"/>.</summary>
        public const double ReadCharsPerSecond = 600, ReadMinSeconds = 4, ReadMaxSeconds = 15;
        /// <summary>Reading speed while a training round shares the card.</summary>
        public const double ReadSharedSpeed = .5;
        public const string RedMansionId = "hongloumeng";
        /// <summary>Chance an offline chat reply quotes a finished book when the topic fits.</summary>
        public const double BookQuoteChance = .4;

        /// <summary>A chapter was read: the book, the chapter (0-based) and what it said about it.</summary>
        public event Action<XgBook, int, string> BookChapterRead;
        /// <summary>A whole book was read.</summary>
        public event Action<XgBook> BookFinished;

        // ───────────── what was read ─────────────

        XgScore BookEntry(string id, bool create)
        {
            if (S.books == null) S.books = new List<XgScore>();
            foreach (var e in S.books) if (e != null && e.key == id) return e;
            if (!create) return null;
            var n = new XgScore { key = id };
            S.books.Add(n);
            return n;
        }

        /// <summary>Chapters of a book it has read (0 … chapters).</summary>
        public int ChaptersRead(XgBook book)
        {
            if (book == null) return 0;
            var e = BookEntry(book.id, false);
            return e == null ? 0 : Math.Max(0, Math.Min(book.chapters.Length, e.count));
        }
        public int ChaptersRead(string id) => ChaptersRead(XgBooks.Get(id));
        public bool BookDone(XgBook book) => book != null && book.chapters.Length > 0 && ChaptersRead(book) >= book.chapters.Length;
        public bool BookDone(string id) => BookDone(XgBooks.Get(id));
        /// <summary>Share of a book read (0–1).</summary>
        public double BookShare(XgBook book) => book == null || book.chapters.Length == 0 ? 0 : (double)ChaptersRead(book) / book.chapters.Length;
        /// <summary>The day a book was finished (yyyymmdd), 0 when not.</summary>
        public int BookDoneDay(XgBook book) { var e = book == null ? null : BookEntry(book.id, false); return e == null || !BookDone(book) ? 0 : (int)e.value; }

        /// <summary>The ability opens the book on the shelf.</summary>
        public bool BookOpen(XgBook book) => book != null && BookOwned(book.id)
            && (AbilitiesCount >= book.ability || ChaptersRead(book) > 0 || S.bookReading == book.id);

        /// <summary>Books it has finished, in shelf order.</summary>
        public List<XgBook> BooksDone() { var l = new List<XgBook>(); foreach (var b in XgBooks.All) if (BookDone(b)) l.Add(b); return l; }
        /// <summary>Books it has read at least one chapter of, in shelf order.</summary>
        public List<XgBook> BooksStarted() { var l = new List<XgBook>(); foreach (var b in XgBooks.All) if (ChaptersRead(b) > 0) l.Add(b); return l; }

        /// <summary>Parameters (K) the finished books added to the bar.</summary>
        public double BookParamsK { get { double k = 0; foreach (var b in XgBooks.All) if (b.paramsK != 0 && BookDone(b)) k += b.paramsK; return k; } }
        /// <summary>Characters read off the shelf in all (= samples booked).</summary>
        public double BookCharsRead
        {
            get
            {
                double n = 0;
                foreach (var b in XgBooks.All) { int r = ChaptersRead(b); for (int i = 0; i < r; i++) n += b.chapters[i].chars; }
                return n;
            }
        }

        // ───────────── reading ─────────────

        public bool Reading => !string.IsNullOrEmpty(S.bookReading);
        public XgBook ReadingBook => Reading ? XgBooks.Get(S.bookReading) : null;
        /// <summary>The running chapter's bar (0–1).</summary>
        public double ReadingProgress => !Reading || S.bookDuration <= 0 ? 0 : Math.Max(0, Math.Min(1, S.bookClock / S.bookDuration));

        /// <summary>Seconds a chapter takes: its characters at the reading speed of the ability and the card(s), 4–15 s.</summary>
        public double ReadSeconds(XgBookChapter ch, IXgHost host)
        {
            if (ch == null) return ReadMinSeconds;
            double ability = 1 + .15 * Math.Max(0, AbilitiesCount - 2);
            double card = host == null ? 1 : Math.Max(.6, Math.Min(2, Math.Sqrt(Math.Max(0, host.Compute))));
            return Math.Max(ReadMinSeconds, Math.Min(ReadMaxSeconds, ch.chars / (ReadCharsPerSecond * ability * card)));
        }

        /// <summary>Why it cannot start the next chapter of this book now, or null when it can.</summary>
        public string ReadBlocker(XgBook book, IXgHost host)
        {
            if (book == null) return T("没有这本书。", "No such book.");
            if (!BookOwned(book.id)) return T("先在图书馆购买这本书。", "Buy this book in the library first.");
            if (!BookOpen(book)) return T("学会「" + AbilityName(book.ability, false) + "」以后才读得懂。", "It can read this once it has learnt '" + AbilityName(book.ability, true) + "'.");
            if (BookDone(book)) return T("读完了。", "Finished.");
            if (Reading) return S.bookReading == book.id ? T("正在读。", "Reading.") : T("一次只读一本。", "One book at a time.");
            if (book.id == RedMansionId && S.redMansionBeat == 1) return T("它在等你回答。", "It is waiting for your answer.");
            if (host != null && !string.IsNullOrEmpty(host.Blocker)) return host.Blocker;
            if (host != null && host.Compute <= 0) return T("显卡没电。", "The card has no power.");
            return null;
        }

        /// <summary>Starts the next chapter of a book ("喂这一篇"). Returns why not, or null when it started.</summary>
        public string StartReading(string id, IXgHost host)
        {
            var book = XgBooks.Get(id);
            string why = ReadBlocker(book, host);
            if (why != null) return why;
            S.bookReading = book.id;
            S.bookChapter = ChaptersRead(book);
            S.bookClock = 0;
            S.bookDuration = ReadSeconds(book.chapters[S.bookChapter], host);
            return null;
        }

        /// <summary>Advances the chapter being read: it runs on the card and pays training power; a training round halves it.</summary>
        void TickBooks(double dt, IXgHost host)
        {
            if (!Reading) return;
            var book = ReadingBook;
            if (book == null || BookDone(book) || S.bookChapter != ChaptersRead(book)) { S.bookReading = ""; return; }
            if (host == null || host.Compute <= 0 || !string.IsNullOrEmpty(host.Blocker)) return;
            bool shared = false;
            foreach (var run in Runs) if (run != null && run.epochActive) shared = true;
            if (!Finite(S.bookDuration) || S.bookDuration <= 0) S.bookDuration = ReadSeconds(book.chapters[S.bookChapter], host);
            S.bookClock += dt * (shared ? ReadSharedSpeed : 1);
            ChargeTraining(host, dt, false);
            if (S.bookClock + 1e-9 >= S.bookDuration) FinishChapter(book);
        }

        void FinishChapter(XgBook book)
        {
            int index = S.bookChapter;
            var ch = book.chapters[index];
            S.bookReading = ""; S.bookClock = 0; S.bookDuration = 0;
            var e = BookEntry(book.id, true);
            e.count = index + 1;
            // A character read is worth what a word read is worth now (XgSim.Reading.cs: more in the later stages).
            double samples = ch.chars * ReadWorth;
            SetCount(S.dataExtra, book.dataset, ExtraSamples(book.dataset) + samples);
            string line = BookReaction(book, index);
            S.bookLineBook = book.id; S.bookLineChapter = index; S.bookLine = line;
            var d = XgCatalog.Dataset(book.dataset);
            string corpus = d != null ? T(d.name, d.nameEn) : book.dataset;
            bool done = e.count >= book.chapters.Length;
            bool redBeat = book.id == RedMansionId && done && S.redMansionBeat == 0;
            if (redBeat) S.redMansionBeat = 1;
            bool completedNow = false;
            if (done)
            {
                e.value = TodayKnown ? Today : 0;
                completedNow = CommitBookCompletion(book);
                Earn("book." + book.id);
            }
            foreach (var run in Runs) if (run != null && run.dataset == book.dataset) Evaluate(run);
            Say(T("读完《" + book.name + "》" + ch.title + "：" + ch.chars.ToString("#,0") + " 字，记作 " + samples.ToString("#,0") + " 条样本，进了「" + corpus + "」。",
                "Read " + book.nameEn + ", " + ch.titleEn + ": " + ch.chars.ToString("#,0") + " characters, " + samples.ToString("#,0") + " samples into " + corpus + "."));
            if (!string.IsNullOrEmpty(line)) Say(T("它：", "It: ") + line);
            if (done)
            {
                double now = TrainedParamsK;
                if (book.paramsK > 0)
                    Say(T("《" + book.name + "》选段读完了。参数 +" + ParamsText(book.paramsK) + "：", "Finished the selected excerpts from " + book.nameEn + ". Parameters +" + ParamsText(book.paramsK) + ": ")
                        + ParamsText(now - book.paramsK) + " → " + ParamsText(now));
                else Say(T("《" + book.name + "》的原创导读读完了。", "Finished the original guide to " + book.nameEn + "."));
            }
            if (redBeat)
            {
                Say(T("它：后面呢？", "It: What comes next?"));
            }
            if (completedNow && S.stage < BookAskStage)
            {
                var completion = S.bookCompletions.Find(c => c.bookId == book.id);
                AddLine("ai", BookCompletionText(completion) + (line.Length == 0 ? "" : " " + line));
            }
            // Observers can save only after the chapter, completion and legacy story beat are committed.
            BookChapterRead?.Invoke(book, index, line);
            if (completedNow) BookFinished?.Invoke(book);
        }

        /// <summary>The player answers 「后面呢？」: the last forty chapters were written by someone else. It will write one too.</summary>
        public string AnswerRedMansion()
        {
            if (S.redMansionBeat != 1) return "";
            S.redMansionBeat = 2;
            string line = T("那我也续一回。", "Then I'll write a chapter too.");
            S.bookLineBook = RedMansionId; S.bookLine = line;
            Say(T("它：", "It: ") + line);
            return line;
        }

        /// <summary>What the player says to 「后面呢？」.</summary>
        public string RedMansionAnswerText => T("后面没了。后四十回是别人续的。", "There is no more. The last forty chapters were written by someone else.");

        // ───────────── what it makes of it ─────────────

        /// <summary>
        /// What it says after a chapter, by ability: 2 a name with a question mark, 3 three key words, 4 the gist, 5 the gist
        /// and a chapter of another book it read with the same theme. Ability 6 says the same offline; the shelf asks the
        /// local model instead when it is ready (<see cref="BookPrompt"/>).
        /// </summary>
        public string BookReaction(XgBook book, int chapter)
        {
            if (book == null || chapter < 0 || chapter >= book.chapters.Length) return "";
            var ch = book.chapters[chapter];
            int ability = AbilitiesCount;
            var names = English && ch.namesEn.Length > 0 ? ch.namesEn : ch.names;
            var words = English && ch.wordsEn.Length > 0 ? ch.wordsEn : ch.words;
            if (ability <= 2) return (names.Length > 0 ? names[0] : words.Length > 0 ? words[0] : "") + T("？", "?");
            if (ability == 3) return string.Join(" / ", words);
            string gist = T(ch.gist, ch.gistEn);
            if (ability == 4) return gist;
            var link = BookLink(book, ch);
            return link == null ? gist : gist + LinkText(link.Item1, link.Item2, ch.tag);
        }

        string LinkText(XgBook other, XgBookChapter oc, string tag)
        {
            string who = oc.names.Length > 0 ? oc.names[0] : T(oc.title, oc.titleEn);
            return T("跟《" + other.name + "》里" + who + "那回一样，说的都是" + XgBooks.TagWord(tag, false) + "。",
                " Like " + who + " in " + other.nameEn + ": both are about " + XgBooks.TagWord(tag, true) + ".");
        }

        /// <summary>A chapter of another book it has read that shares this chapter's theme (the latest such book first), or null.</summary>
        public Tuple<XgBook, XgBookChapter> BookLink(XgBook book, XgBookChapter ch)
        {
            if (book == null || ch == null || string.IsNullOrEmpty(ch.tag)) return null;
            var all = XgBooks.All;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                var other = all[i];
                if (other == book) continue;
                int r = ChaptersRead(other);
                for (int c = 0; c < r; c++) if (other.chapters[c].tag == ch.tag) return Tuple.Create(other, other.chapters[c]);
            }
            return null;
        }

        /// <summary>The prompt for the local model at ability 6: the chapter, the books it has read, at most 40 characters back.</summary>
        public string BookPrompt(XgBook book, int chapter)
        {
            if (book == null || chapter < 0 || chapter >= book.chapters.Length) return "";
            var ch = book.chapters[chapter];
            var sb = new StringBuilder();
            sb.Append(T("你刚读完《" + book.name + "》" + ch.title + "。材料类型：" + XgBooks.MaterialLabel(book, false) + "。材料：" + XgBooks.Excerpt(ch, false) + "\n",
                "You just read " + book.nameEn + ", " + ch.titleEn + ". Material: " + XgBooks.MaterialLabel(book, true) + ". Text: " + XgBooks.Excerpt(ch, true) + "\n"));
            string shelf = BooksReadLine();
            if (shelf.Length > 0) sb.Append(shelf).Append('\n');
            sb.Append(T("用你自己的话说一句读后感，不超过 40 字，可以联系读过的别的书。只输出这一句。",
                "Say in your own words what you made of it, one sentence, at most 25 words; you may link it to another book you read. Output only that sentence."));
            return sb.ToString();
        }

        /// <summary>
        /// Its own note on a book, fuller with ability: names (2), key words (3), the gist so far (4), the whole-book note
        /// once finished (5–6). Empty until a chapter was read.
        /// </summary>
        public string BookNote(XgBook book)
        {
            string note = BookNoteOwn(book);
            // What the owner explained when it asked (XgSim.BookPuzzles.cs).
            var told = note.Length > 0 ? LastBookAnswer(book) : null;
            if (told != null) note += T("\n主人说：" + told.answer + (told.verdict == PuzzleRight ? "" : "（" + VerdictName(told.verdict, false) + "）"),
                "\nThe owner said: " + told.answer + (told.verdict == PuzzleRight ? "" : " (" + VerdictName(told.verdict, true) + ")"));
            return note;
        }

        string BookNoteOwn(XgBook book)
        {
            int read = ChaptersRead(book);
            if (read <= 0) return "";
            int ability = AbilitiesCount;
            var seen = new List<string>();
            if (ability <= 3)
            {
                for (int i = 0; i < read; i++)
                {
                    var chapter = book.chapters[i];
                    var names = English && chapter.namesEn.Length > 0 ? chapter.namesEn : chapter.names;
                    var words = English && chapter.wordsEn.Length > 0 ? chapter.wordsEn : chapter.words;
                    foreach (var w in ability <= 2 ? names : words)
                        if (!seen.Contains(w) && seen.Count < 6) seen.Add(w);
                }
                return string.Join(ability <= 2 ? T("、", ", ") : " / ", seen) + (ability <= 2 ? T("？", "?") : "");
            }
            var last = book.chapters[read - 1];
            if (ability == 4 || !BookDone(book))
                return T(last.gist, last.gistEn) + (BookDone(book) ? T("读完了。", " Finished.") : T("后面还没读。", " Haven't read the rest."));
            return T(book.note, book.noteEn);
        }

        // ───────────── the shelf quiz (它学会了吗) ─────────────

        /// <summary>
        /// One question per book it has started. Right with chance (share read) × (ability: 2 → .55 … 5+ → 1), and never when
        /// the chapter that answers it is unread; a wrong answer is a plausible one. Fixed for a given progress, so the page
        /// does not flicker.
        /// </summary>
        public List<XgBookAnswer> BookQuiz()
        {
            var list = new List<XgBookAnswer>();
            int ability = AbilitiesCount;
            foreach (var book in XgBooks.All)
            {
                int read = ChaptersRead(book);
                if (read <= 0 || book.quiz.Length == 0) continue;
                XgBookQuiz q = null;
                foreach (var item in book.quiz) if (item.chapter < read) { q = item; break; }
                if (q == null) q = book.quiz[0];
                double chance = BookQuizChance(book);
                double roll = QuizRoll(book.id, read, ability);
                bool right = q.chapter < read && roll < chance;
                string[] wrong = English && q.wrongEn.Length == q.wrong.Length ? q.wrongEn : q.wrong;
                string answer = T(q.answer, q.answerEn);
                string said = right || wrong.Length == 0 ? answer : wrong[(int)(roll * 997) % wrong.Length];
                list.Add(new XgBookAnswer { book = book, question = T(q.q, q.qEn), answer = answer, said = said, right = right });
            }
            return list;
        }

        /// <summary>Chance it answers a book's question right: share read × the ability's factor.</summary>
        public double BookQuizChance(XgBook book) => BookShare(book) * Math.Min(1, .25 + .15 * AbilitiesCount);

        static double QuizRoll(string id, int read, int ability)
        {
            uint h = 2166136261;
            foreach (char c in id) h = (h ^ c) * 16777619;
            h = (h ^ (uint)read) * 16777619;
            h = (h ^ (uint)(ability * 31)) * 16777619;
            h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
            return (h % 10007) / 10007.0;
        }

        // ───────────── quoting in chat ─────────────

        /// <summary>
        /// A line of a finished book that fits what was said (「《论语》：食不厌精，脍不厌细。」), or null. Only books it has
        /// finished: what it never read never comes out.
        /// </summary>
        public string BookQuote(string message, Random rng)
        {
            var topics = XgBooks.TopicsOf(message);
            if (topics.Count == 0) return null;
            var fits = new List<string>();
            foreach (var book in BooksDone())
                foreach (var q in book.quotes)
                    foreach (var t in q.topics)
                        if (topics.Contains(t)) { fits.Add(T("《" + book.name + "》：" + q.text + (q.text.EndsWith("。") || q.text.EndsWith("？") || q.text.EndsWith("！") ? "" : "。"), book.nameEn + ": \"" + q.textEn + "\"")); break; }
            if (fits.Count == 0) return null;
            return fits[(rng ?? new Random()).Next(fits.Count)];
        }

        /// <summary>The prompt line for chat: 「你读过：《三字经》《论语》；《西游记》读到第 3 回。可以偶尔引用，别卖弄。」, empty before any book.</summary>
        public string BooksReadLine()
        {
            var done = new List<string>();
            var part = new List<string>();
            foreach (var b in XgBooks.All)
            {
                int r = ChaptersRead(b);
                if (r <= 0) continue;
                string material = XgBooks.LibraryEntry(b.id).IsGuide ? T("导读", " (guide)") : T("选段", " (selections)");
                if (BookDone(b)) done.Add(T("《" + b.name + "》", b.nameEn) + material);
                else part.Add(T("《" + b.name + "》读到「" + b.chapters[r - 1].title + "」", b.nameEn + " up to " + b.chapters[r - 1].titleEn));
            }
            if (done.Count == 0 && part.Count == 0) return "";
            string zh = "你读过：" + string.Join("", done) + (part.Count > 0 ? (done.Count > 0 ? "；" : "") + string.Join("；", part) : "") + "。可以偶尔引用，别卖弄；没读过的书不要引用。";
            string en = "Books you have read: " + string.Join(", ", done) + (part.Count > 0 ? (done.Count > 0 ? "; " : "") + string.Join("; ", part) : "") + ". Quote them now and then, don't show off; never quote a book you haven't read.";
            return T(zh, en) + T("原创导读只可概括已提供的材料，不表示读过原著全文，也不要编造原文引句。",
                " Original guides cover only the supplied material, not the full books; do not invent quotations.");
        }

        /// <summary>Keeps a loaded save's shelf sane (unknown books, a stale reading bar).</summary>
        void RepairBooks()
        {
            if (S.books == null) S.books = new List<XgScore>();
            S.books.RemoveAll(e => e == null || XgBooks.Get(e.key) == null);
            var unique = new Dictionary<string, XgScore>();
            foreach (var e in S.books)
            {
                e.count = Math.Max(0, Math.Min(XgBooks.Get(e.key).chapters.Length, e.count));
                if (!unique.TryGetValue(e.key, out var kept)) unique.Add(e.key, e);
                else if (e.count > kept.count) { kept.count = e.count; kept.value = e.value; }
            }
            S.books = new List<XgScore>(unique.Values);
            if (S.bookReading == null) S.bookReading = "";
            if (S.bookLine == null) S.bookLine = "";
            if (S.bookLineBook == null) S.bookLineBook = "";
            if (Reading)
            {
                var b = ReadingBook;
                if (b == null || BookDone(b) || S.bookChapter != ChaptersRead(b)) { S.bookReading = ""; S.bookClock = 0; S.bookDuration = 0; }
            }
            if (!Finite(S.bookClock) || S.bookClock < 0) S.bookClock = 0;
            if (S.redMansionBeat < 0 || S.redMansionBeat > 2) S.redMansionBeat = 0;
        }
    }
}
