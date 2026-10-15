using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.Core.Chat;

namespace LingGuangV05.XingGuang
{
    /// <summary>A card the owner labelled by hand, kept so it can recall what it was taught (XgSim.Knowledge.cs).</summary>
    [Serializable]
    public sealed class XgTaughtNote
    {
        public string text = "", textEn = "", why = "", whyEn = "";
        public bool truth;
        /// <summary>In-game day it was taught (yyyymmdd), 0 unknown.</summary>
        public int day;
    }

    public sealed partial class XgState
    {
        public List<XgTaughtNote> taughtNotes = new List<XgTaughtNote>();
    }

    /// <summary>
    /// Retrieval (RAG) for its chat: before it answers, what it has actually been through that fits the owner's line —
    /// cards it was taught, chapters it read on the 书架, older talks with the owner, and 2016 news up to today
    /// (Rag.Era) — goes into the per-turn state, a few lines at most. What it can recall grows with it: taught cards
    /// and news from stage 3, chapters from stage 4, old talks from stage 5 (with the memory book). It recalls by
    /// shared words (BM25) until it has researched 词向量; from then on also by meaning (HybridScorer, when the game's
    /// embedding model runs).
    /// </summary>
    public sealed partial class XgSim
    {
        public const int TaughtNotesKept = 400, KnowledgeCards = 2, KnowledgeChapters = 2, KnowledgeEra = 1, KnowledgeEpisodes = 2;
        public const int KnowledgeFirstStage = 3, KnowledgeBooksStage = 4;

        /// <summary>Recall by meaning, not only shared words: the 词向量 research.</summary>
        public bool SemanticRecall => Has("wordvec");

        /// <summary><see cref="MemoryScorer"/> switched to this save's recall (by meaning or by words).</summary>
        IMemoryScorer RecallScorer()
        {
            if (MemoryScorer is HybridScorer hybrid) hybrid.Semantic = SemanticRecall;
            return MemoryScorer;
        }

        void RepairKnowledge()
        {
            if (S.taughtNotes == null) S.taughtNotes = new List<XgTaughtNote>();
            S.taughtNotes.RemoveAll(n => n == null || string.IsNullOrWhiteSpace(n.text));
        }

        /// <summary>Keeps a hand-labelled card; the same question taught again replaces the old note.</summary>
        void RememberTaught(XgCard card)
        {
            if (card == null || card.family || card.law || card.kind == "shutdown") return;
            if (S.taughtNotes == null) S.taughtNotes = new List<XgTaughtNote>();
            string text = LessonText(card);
            if (string.IsNullOrWhiteSpace(text)) return;
            S.taughtNotes.RemoveAll(n => n.text == text);
            S.taughtNotes.Add(new XgTaughtNote { text = text, textEn = card.questionEn ?? "", why = card.why ?? "", whyEn = card.whyEn ?? "", truth = card.truth, day = TodayKnown ? Today : 0 });
            while (S.taughtNotes.Count > TaughtNotesKept) S.taughtNotes.RemoveAt(0);
        }

        /// <summary>The texts to embed before the next answer (LocalEmbedder.Prepare): the owner's last two lines.</summary>
        public List<string> RecallQueries()
        {
            LastOwnerLines(out string latest, out string previous);
            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(latest)) list.Add(latest);
            if (!string.IsNullOrWhiteSpace(previous)) list.Add(previous);
            return list;
        }

        /// <summary>
        /// What it knows that fits the line (empty before stage 3 or when nothing fits). <paramref name="latest"/> null:
        /// the owner's last two lines on the 对话 page. <paramref name="chatWindowStart"/>: the first 对话 line already in
        /// the prompt (older ones may be recalled); -1 recalls none.
        /// </summary>
        public string PersonaKnowledge(string latest = null, string previous = null, int chatWindowStart = -1)
        {
            int stage = SpeechStage;
            if (stage < KnowledgeFirstStage) return "";
            if (latest == null && previous == null) LastOwnerLines(out latest, out previous);
            var query = MemoryQuery.Of(latest, previous);
            if (query.Empty) return "";
            var scorer = RecallScorer();
            var sb = new StringBuilder();

            // Cards it was taught.
            var notes = S.taughtNotes ?? new List<XgTaughtNote>();
            if (notes.Count > 0)
            {
                var docs = new List<string>(notes.Count);
                foreach (var n in notes) docs.Add(NoteText(n));
                var lines = new List<string>();
                foreach (int i in MemoryRanker.Rank(scorer, query, docs, KnowledgeCards)) lines.Add(TaughtLine(notes[i]));
                sb.Append(Rag.Block(T("主人教过你的卡（你学到的，可以拿来举例）：", "Cards your owner taught you (what you learned; you may use them as examples):"), lines));
            }

            // Chapters it has read.
            if (stage >= KnowledgeBooksStage)
            {
                var docs = new List<string>(); var refs = new List<string>();
                foreach (var b in XgBooks.All)
                {
                    int read = ChaptersRead(b);
                    for (int c = 0; c < read && c < b.chapters.Length; c++)
                    {
                        var ch = b.chapters[c];
                        // Searched by title and gist: the classical excerpt shares stray characters with modern lines (夺帅 / 夺冠).
                        docs.Add(English ? b.nameEn + " · " + ch.titleEn + ": " + ch.gistEn : "《" + b.name + "》" + ch.title + "：" + ch.gist);
                        refs.Add(English ? b.nameEn + ", \"" + ch.titleEn + "\": " + ch.gistEn
                            : "《" + b.name + "》「" + ch.title + "」：" + ch.gist + "（原文：" + Clip(ch.excerpt, 50) + "）");
                    }
                }
                var lines = new List<string>();
                foreach (int i in MemoryRanker.Rank(scorer, query, docs, KnowledgeChapters)) lines.Add(refs[i]);
                sb.Append(Rag.Block(T("你读过的、和这句话有关的段落：", "Passages you have read that fit this:"), lines));
            }

            // Older talks with the owner (outside the prompt's window).
            if (MemoryOpen && chatWindowStart > 1 && S.chat.Count > 1)
            {
                int end = Math.Min(chatWindowStart, S.chat.Count);
                var lines = new List<string>(end);
                for (int i = 0; i < end; i++) lines.Add((S.chat[i].from == "me" ? T("主人：", "Owner: ") : T("你：", "You: ")) + S.chat[i].text);
                var picked = Rag.Episodes(scorer, query, lines, end, KnowledgeEpisodes);
                var shown = new List<string>();
                foreach (int i in picked) shown.Add(lines[i] + (i + 1 < end ? " / " + lines[i + 1] : ""));
                sb.Append(Rag.Block(T("你们以前聊过：", "You talked about this before:"), shown));
            }

            // 2016 news up to today.
            if (TodayKnown)
            {
                var lines = new List<string>();
                foreach (var e in Rag.Lookup(scorer, query, Rag.Era, DayOf(Today), English, KnowledgeEra)) lines.Add(e.Text(English));
                sb.Append(Rag.Block(T("你知道的外面的事：", "News you know about:"), lines));
            }
            return sb.ToString();
        }

        string NoteText(XgTaughtNote n) => English && !string.IsNullOrEmpty(n.textEn) ? n.textEn + " " + n.whyEn : n.text + " " + n.why;

        string TaughtLine(XgTaughtNote n)
        {
            string q = English && !string.IsNullOrEmpty(n.textEn) ? n.textEn : n.text;
            string why = English ? n.whyEn : n.why;
            return "「" + Clip(q, 60) + "」→ " + (n.truth ? T("是", "yes") : T("否", "no")) + (string.IsNullOrWhiteSpace(why) ? "" : T("（", " (") + Clip(why, 50) + T("）", ")"));
        }

        static string Clip(string s, int n) { s = (s ?? "").Trim(); return s.Length > n ? s.Substring(0, n) + "…" : s; }

        static DateTime DayOf(int yyyymmdd)
        {
            try { return new DateTime(yyyymmdd / 10000, yyyymmdd / 100 % 100, yyyymmdd % 100); }
            catch (ArgumentOutOfRangeException) { return new DateTime(2016, 6, 1); }
        }
    }
}
