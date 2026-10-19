using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The 书架 tab of the train page's side card (名著喂养与结局_设计.md §一): eight classics, one chapter at a time. Each row
    /// shows how far it got and feeds the next chapter; the panel under the list shows the chapter scrolling in while it
    /// reads, then what it said, its note on the book, and at 第八十回 of 红楼梦 the question 「后面呢？」.
    /// </summary>
    public sealed partial class XgTrainPage
    {
        sealed class BookRow
        {
            public RectTransform rt, fill;
            public TMP_Text name, count;
            public XgBtn feed;
            public Image back;
        }

        XgBtn booksTab, redMansionAnswer;
        bool booksView;
        RectTransform booksPane, bookPanelFill, bookAnswerRow;
        TMP_Text bookHead, bookBody;
        /// <summary>The answer box for its question about a book (XgSim.BookPuzzles.cs), shown while one is pending.</summary>
        TMP_InputField bookAnswerField;
        XgBtn bookAnswerSend;
        bool bookAsking, bookJudging;
        readonly List<BookRow> bookRows = new List<BookRow>();
        /// <summary>《AI是怎么炼成的》, the book it arrived with (XgSim.Primer.cs): the top row, read, and it never opens.</summary>
        BookRow primerRow;
        const string PrimerId = "primer";
        string selectedBook = "";
        /// <summary>The chapter the local model was last asked about (ability 6), so it is asked once.</summary>
        string bookAskedKey = "";
        XgSim bookAskedSim;

        void BuildBooksTab(XgModuleRefs m)
        {
            booksTab = m.Button("booksTab", ui, T("书架", "Books"), () => { booksView = true; learningView = false; Fx.Play(XgJuice.Sfx.Id.Click); RefreshSide(Track, Sim.Run(Track)); }, 12, "BooksTab");

            // The shelf scrolls (one row per book, the primer on top); the reading panel under it keeps its height.
            booksPane = m.Get("booksPane");
            var content = m.Get("shelf");
            primerRow = BookRowAt(content, PrimerId);
            primerRow.feed.Set(T("打不开", "Won't open"), false);
            for (int i = 0; i < XgBooks.All.Count; i++) bookRows.Add(BookRowAt(content, XgBooks.All[i].id));

            var panel = m.Get("reading");
            bookHead = m.Text("bookHead");
            bookBody = m.Text("bookBody");
            if (XgUiV4Fonts.Reading != null) bookBody.font = XgUiV4Fonts.Reading;
            bookPanelFill = m.Get("bookFill");
            // Under the text: the answer to 「后面呢？」, or the box for its question about a book; the row is hidden otherwise.
            bookAnswerRow = m.Get("answerRow");
            redMansionAnswer = m.Button("redMansion", ui, "", () =>
            {
                Sim.AnswerRedMansion();
                Fx.Play(XgJuice.Sfx.Id.Ding);
                RefreshBooks();
            }, 12, "RedMansionAnswer");
            redMansionAnswer.label.enableAutoSizing = true; redMansionAnswer.label.fontSizeMin = 9; redMansionAnswer.label.fontSizeMax = 12;
            redMansionAnswer.label.overflowMode = TextOverflowModes.Ellipsis;
            BuildBookAnswer(m);
            panel.gameObject.AddComponent<XgBookReveal>().Page = this;
        }

        void BuildBookAnswer(XgModuleRefs m)
        {
            var box = m.Get("answerField");
            bookAnswerField = box.GetComponent<TMP_InputField>();
            if (bookAnswerField.placeholder is TMP_Text placeholder) placeholder.text = T("你觉得是什么意思？", "What do you think it means?");
            bookAnswerField.fontAsset = ui.font;
            bookAnswerField.onSubmit.AddListener(_ => SendBookAnswer());
            bookAnswerSend = m.Button("answerSend", ui, T("回答", "Answer"), SendBookAnswer, 12, "BookAnswerSend");
            box.gameObject.SetActive(false);
            ShowSlot(bookAnswerSend, false);
            ShowSlot(redMansionAnswer, false);
            Part(bookAnswerRow, false);
        }

        /// <summary>Shows or hides a button together with its layout slot, so a hidden button leaves no gap.</summary>
        static void ShowSlot(XgBtn b, bool on) { b.Show(on); Part(b.rt.parent, on); }

        /// <summary>The owner's answer from the shelf: judged like a YY reply, and both lines go into the chat history.</summary>
        void SendBookAnswer()
        {
            var sim = Sim;
            if (sim == null || bookJudging || sim.BookPuzzlePending == null || bookAnswerField == null) return;
            string text = (bookAnswerField.text ?? "").Trim();
            if (text.Length == 0) { Fx.Play(XgJuice.Sfx.Id.Buzz); return; }
            bookJudging = true;
            sim.AddLine("me", text);
            bookAnswerField.text = "";
            Fx.Play(XgJuice.Sfx.Id.Click);
            XgBookPuzzleJudge.Answer(sim, text, said =>
            {
                bookJudging = false;
                if (!ReferenceEquals(sim, Sim) || string.IsNullOrEmpty(said)) return;
                sim.AddLine("ai", said);
                view.ShowToast(said, 4);
                Fx.Play(XgJuice.Sfx.Id.Ding);
                RefreshBooks();
            });
            RefreshBooks();
        }

        void Feed(string id)
        {
            string why = Sim.StartReading(id, Host);
            if (why != null) { view.ShowToast(why, 3); Fx.Play(XgJuice.Sfx.Id.Buzz); return; }
            selectedBook = id;
            Fx.Play(XgJuice.Sfx.Id.Click);
            RefreshBooks();
        }

        string BookTip(XgBook book)
        {
            if (book == null || Sim == null) return "";
            var d = XgCatalog.Dataset(book.dataset);
            string corpus = d != null ? T(d.name, d.nameEn) : book.dataset;
            string samples = XgSim.SamplesText(book.TotalChars * Sim.ReadWorth);
            return T("《" + book.name + "》" + book.author + "\n" + book.chapters.Length + " 篇 · 约 " + book.TotalChars.ToString("#,0") + " 字 → 「" + corpus + "」约 " + samples + " 条样本（" + Sim.ReadRuleText().Replace("词", "字") + "）\n读完一本：参数 +" + XgSim.ParamsText(book.paramsK) + "，聊天时会引用它。\n读的时候占显卡、付电费；训练一轮在跑时只有一半速度。",
                book.nameEn + " (" + book.authorEn + ")\n" + book.chapters.Length + " chapters · about " + book.TotalChars.ToString("#,0") + " characters → about " + samples + " samples of " + corpus + " (" + Sim.ReadRuleText().Replace("word", "character") + ")\nFinish it: parameters +" + XgSim.ParamsText(book.paramsK) + ", and it quotes it in chat.\nReading uses the card and pays power; at half speed while a training round runs.");
        }

        BookRow BookRowAt(RectTransform content, string id)
        {
            var m = XgModuleRefs.Spawn("TrainBookRow", content, ui);
            var row = new BookRow();
            row.rt = (RectTransform)m.transform;
            row.rt.name = "Book_" + id;
            row.back = row.rt.GetComponent<Image>();
            row.rt.GetComponent<Button>().onClick.AddListener(() => { selectedBook = id; Fx.Play(XgJuice.Sfx.Id.Click); RefreshBooks(); });
            row.fill = m.Get("fill");
            row.name = m.Text("name");
            row.count = m.Text("count");
            row.feed = m.Button("feed", ui, "", () => { if (id != PrimerId) Feed(id); }, 11, "Feed");
            row.feed.label.enableAutoSizing = true; row.feed.label.fontSizeMin = 9; row.feed.label.fontSizeMax = 11;
            if (id != PrimerId) UiTip.Add(row.rt, () => BookTip(XgBooks.Get(id)));
            return row;
        }

        void RefreshBooks()
        {
            if (booksPane == null || Sim == null || !booksPane.gameObject.activeInHierarchy) return;
            var reading = Sim.ReadingBook;
            for (int i = 0; i < bookRows.Count && i < XgBooks.All.Count; i++)
            {
                var book = XgBooks.All[i];
                var row = bookRows[i];
                bool open = Sim.BookOpen(book), done = Sim.BookDone(book), now = reading == book;
                int read = Sim.ChaptersRead(book);
                string name = T("《" + book.name + "》", book.nameEn);
                row.name.text = open ? name : "<color=#6F7F88>" + name + "</color>";
                row.count.text = open ? read + "/" + book.chapters.Length + (done ? "" : "  +" + XgSim.ParamsText(book.paramsK)) : T("能力 " + book.ability, "ability " + book.ability);
                SetBar(row.fill, (float)Sim.BookShare(book));
                row.back.color = selectedBook == book.id ? XgDark.AccentSoft : XgDark.Track;
                if (done) row.feed.Set(T("读完 ✓", "Done ✓"), false);
                else if (now) row.feed.Set(T("在读 ", "Reading ") + XgSim.Pct(Sim.ReadingProgress).Replace(".0%", "%"), false);
                else if (!open) row.feed.Set(T("没解锁", "Locked"), false);
                else row.feed.Set(read == 0 ? T("喂第一篇", "Feed ch.1") : T("喂下一篇", "Feed next"), Sim.ReadBlocker(book, Host) == null, XgDark.Accent, Color.white);
            }

            primerRow.name.text = T("《" + XgSim.PrimerTitle + "》", XgSim.PrimerTitleEn);
            primerRow.count.text = T("5月24日读完", "read May 24");
            SetBar(primerRow.fill, 1);
            primerRow.back.color = selectedBook == PrimerId ? XgDark.AccentSoft : XgDark.Track;
            primerRow.feed.Set(T("打不开", "Won't open"), false);

            var shown = reading ?? XgBooks.Get(selectedBook) ?? XgBooks.Get(Sim.S.bookLineBook) ?? XgBooks.All[0];
            SetBar(bookPanelFill, reading != null ? (float)Sim.ReadingProgress : (float)Sim.BookShare(shown));
            bool askNext = Sim.S.redMansionBeat == 1;
            ShowSlot(redMansionAnswer, askNext);
            if (askNext) redMansionAnswer.Set(Sim.RedMansionAnswerText, true, XgDark.Accent, Color.white);
            var puzzle = askNext ? null : Sim.BookPuzzlePending;
            bookAsking = puzzle != null;
            bookAnswerField.gameObject.SetActive(bookAsking);
            ShowSlot(bookAnswerSend, bookAsking);
            Part(bookAnswerRow, askNext || bookAsking);
            if (bookAsking)
            {
                var pb = Sim.BookPuzzleBook;
                bookAnswerSend.Set(bookJudging ? T("想想…", "Hmm…") : T("回答", "Answer"), !bookJudging, XgDark.Accent, Color.white);
                string where = pb != null && puzzle.chapter < pb.chapters.Length ? T(pb.chapters[puzzle.chapter].title, pb.chapters[puzzle.chapter].titleEn) : "";
                bookHead.text = pb != null ? T("它没读懂 · 《" + pb.name + "》" + where, "It didn't get · " + pb.nameEn + " · " + where) : T("它没读懂", "It didn't get it");
                bookBody.maxVisibleCharacters = 99999;
                bookBody.text = T("它：", "It: ") + T(puzzle.q, puzzle.qEn) + "\n" + Hex(XgDark.Muted) + T("在下面回答，或者在 YY 里回。", "Answer below, or reply on YY.") + "</color>";
                return;
            }
            if (reading != null)
            {
                var ch = reading.chapters[Mathf.Clamp(Sim.S.bookChapter, 0, reading.chapters.Length - 1)];
                bookHead.text = T("正在读《" + reading.name + "》", "Reading " + reading.nameEn + " · ") + T(ch.title, ch.titleEn);
                // The body (the excerpt) is revealed frame by frame by XgBookReveal.
                if (bookBody.text != ch.excerpt) bookBody.text = ch.excerpt;
                return;
            }
            bookBody.maxVisibleCharacters = 99999;
            if (askNext)
            {
                bookHead.text = T("《红楼梦》第八十回 · 读完了", "Dream of the Red Chamber, ch.80 · finished");
                bookBody.text = T("它：后面呢？", "It: What comes next?");
                return;
            }
            if (selectedBook == PrimerId)
            {
                bookHead.text = T("《" + XgSim.PrimerTitle + "》· 打不开", XgSim.PrimerTitleEn + " · won't open");
                bookBody.text = Sim.PrimerPage();
                SetBar(bookPanelFill, 1);
                return;
            }
            bookHead.text = T("《" + shown.name + "》· 它的笔记", shown.nameEn + " · its notes");
            string note = Sim.BookNote(shown);
            string body = note.Length > 0 ? note : Sim.BookOpen(shown) ? T("还没读。按「喂第一篇」。", "Not read yet. Press 'Feed ch.1'.") : T("现在还读不懂：学会「" + XgSim.AbilityName(shown.ability, false) + "」以后再来。", "Too hard for now: come back once it has learnt '" + XgSim.AbilityName(shown.ability, true) + "'.");
            if (Sim.S.bookLineBook == shown.id && Sim.S.bookLine.Length > 0)
            {
                string where = Sim.S.bookLineChapter >= 0 && Sim.S.bookLineChapter < shown.chapters.Length ? T(shown.chapters[Sim.S.bookLineChapter].title, shown.chapters[Sim.S.bookLineChapter].titleEn) : "";
                body += "\n\n" + Hex(XgDark.Muted) + T("读完", "After ") + where + T("，它说：", ", it said:") + "</color>\n" + Hex(XgDark.Gold) + "「" + Sim.S.bookLine + "」</color>";
            }
            bookBody.text = body;
            AskBookModel();
        }

        /// <summary>The 书架小考 under 它学会了吗: one question per book it has started, its answer, and whether it was right.</summary>
        string BookQuizText()
        {
            var quiz = Sim.BookQuiz();
            if (quiz.Count == 0) return "";
            var sb = new System.Text.StringBuilder();
            sb.Append("\n\n<b>").Append(T("书架小考", "Bookshelf quiz")).Append("</b>  ").Append(Hex(XgDark.Muted)).Append(T("读得越多、能力越高，越常答对", "the more it read and the abler it is, the more it gets right")).Append("</color>");
            foreach (var a in quiz)
            {
                sb.Append("\n").Append(Hex(XgDark.Muted)).Append(T("《" + a.book.name + "》", a.book.nameEn + ": ")).Append("</color>").Append(a.question);
                sb.Append("\n    ").Append(T("它答：", "It says: ")).Append(a.right ? Hex(XgDark.Good) : Hex(XgDark.Bad)).Append(a.said).Append(a.right ? " ✓" : " ×").Append("</color>");
                if (!a.right) sb.Append(Hex(XgDark.Muted)).Append(T("（是" + a.answer + "）", " (it is " + a.answer + ")")).Append("</color>");
            }
            return sb.ToString();
        }

        /// <summary>Ability 6: the local model says what it made of the last chapter (≤ 40 字); the scripted line stays when it is not ready.</summary>
        void AskBookModel()
        {
            var sim = Sim;
            if (sim.AbilitiesCount < XgSim.AbilityCount || sim.S.bookLineChapter < 0) return;
            var book = XgBooks.Get(sim.S.bookLineBook);
            if (book == null || sim.S.bookLineChapter >= book.chapters.Length) return;
            string key = book.id + ":" + sim.S.bookLineChapter;
            if (key == bookAskedKey && ReferenceEquals(sim, bookAskedSim)) return;
            var llm = LingGuangV05.Desktop.LLM.LocalLlm.Instance;
            if (llm == null || !llm.Ready) return;
            bookAskedKey = key; bookAskedSim = sim;
            int chapter = sim.S.bookLineChapter;
            var messages = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("system", sim.PersonaPrompt(12, new string[0]) + "\n" + EraLexicon.PromptRule(GameText.IsEnglish) + (GameText.IsEnglish ? " Reply in English." : "")),
                new KeyValuePair<string, string>("user", sim.BookPrompt(book, chapter)),
            };
            llm.Chat(messages, 120, .7f, reply =>
            {
                if (!ReferenceEquals(sim, Sim) || sim.S.bookLineBook != book.id || sim.S.bookLineChapter != chapter) return;
                reply = EraLexicon.Scrub((reply ?? "").Trim());
                int end = reply.IndexOf("</think>", StringComparison.Ordinal); if (end >= 0) reply = reply.Substring(end + 8).Trim();
                reply = reply.Trim('「', '」', '"', '“', '”');
                if (reply.Length < 4) return;
                if (reply.Length > 60) reply = reply.Substring(0, 60) + "……";
                sim.S.bookLine = reply;
                RefreshBooks();
            }, sim.S.fullOpen, null, LingGuangV05.Core.Chat.LlmSeat.LingGuang);
        }

        /// <summary>Reveals the chapter being read character by character, every frame (the page itself refreshes 5 times a second).</summary>
        internal void TickBookReveal()
        {
            if (bookBody == null || Sim == null) return;
            if (!Sim.Reading || bookAsking) return;
            int len = bookBody.text.Length;
            bookBody.maxVisibleCharacters = Mathf.Clamp(Mathf.CeilToInt(len * (float)Sim.ReadingProgress), 1, len);
            SetBar(bookPanelFill, (float)Sim.ReadingProgress);
        }
    }

    /// <summary>Drives <see cref="XgTrainPage.TickBookReveal"/> while the shelf is on screen.</summary>
    public sealed class XgBookReveal : MonoBehaviour
    {
        public XgTrainPage Page;
        void Update() { Page?.TickBookReveal(); }
    }
}
