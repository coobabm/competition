using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed class XgLibraryEntry
    {
        public string id, title, titleEn, author, authorEn, language, publication, sourceUrl, materialKind, rights, band;
        // availableBy is a conservative sale threshold, not a claimed exact first-publication date.
        public int availableBy, stage;
        double catalogPrice;
        // One quote for library cards, purchase checks and charges; historical receipts are separate.
        public double price { get => LingGuangV05.Core.PurchasePricing.AtLeastHundred(catalogPrice); set => catalogPrice = value; }
        public string introZh, introEn, insightZh, insightEn, questionZh, questionEn, keysZh, keysEn;
        public bool IsGuide => materialKind == "original-guide";
    }

    public static partial class XgBooks
    {
        static XgLibraryEntry[] libraryEntries;
        static XgBook[] libraryBooks;
        static readonly Dictionary<string, XgBookPuzzle[]> guidePuzzles = new Dictionary<string, XgBookPuzzle[]>();
        public static IReadOnlyList<XgLibraryEntry> Library => libraryEntries ?? (libraryEntries = CreateLibraryEntries());
        public static XgLibraryEntry LibraryEntry(string id)
        {
            foreach (var entry in Library) if (entry.id == id) return entry;
            return null;
        }
        static XgBook[] LibraryBooks
        {
            get
            {
                if (libraryBooks != null) return libraryBooks;
                var result = new List<XgBook>(legacyBooks);
                foreach (var entry in Library)
                {
                    if (!entry.IsGuide) continue;
                    result.Add(new XgBook
                    {
                        id = entry.id, name = entry.title, nameEn = entry.titleEn, author = entry.author, authorEn = entry.authorEn,
                        ability = entry.stage, dataset = NovelDataset, paramsK = 0,
                        note = entry.insightZh, noteEn = entry.insightEn,
                        chapters = new[] { GuideChapter(entry, false), GuideChapter(entry, true) }
                    });
                }
                libraryBooks = result.ToArray();
                return libraryBooks;
            }
        }
        static XgBookChapter GuideChapter(XgLibraryEntry e, bool insight)
        {
            string zh = insight ? e.insightZh : e.introZh, en = insight ? e.insightEn : e.introEn;
            string native = e.language == "en" ? en : zh;
            return new XgBookChapter
            {
                title = insight ? "读后思考（原创）" : "作品导读（原创）", titleEn = insight ? "Reflection (original guide)" : "Introduction (original guide)",
                excerpt = native, excerptEn = en, chars = native.Length,
                names = new[] { e.title }, namesEn = new[] { e.titleEn },
                words = new[] { e.title, "观察", "理解" }, wordsEn = new[] { e.titleEn, "observe", "understand" },
                gist = zh, gistEn = en, tag = ""
            };
        }
        static XgBookPuzzle[] LibraryPuzzles(string id)
        {
            if (id == null) return new XgBookPuzzle[0];
            if (guidePuzzles.TryGetValue(id, out var cached)) return cached;
            var e = LibraryEntry(id);
            if (e == null || !e.IsGuide) return new XgBookPuzzle[0];
            var p = new XgBookPuzzle
            {
                id = id + ".guide", chapter = 1, q = e.questionZh, qEn = e.questionEn,
                keys = e.keysZh.Split(';'), keysEn = e.keysEn.Split(';'), truth = e.insightZh, truthEn = e.insightEn
            };
            return guidePuzzles[id] = new[] { p };
        }
        public static string MaterialLabel(XgBook book, bool english)
        {
            bool guide = book != null && LibraryEntry(book.id)?.IsGuide == true;
            return guide ? (english ? "Original guide, not the complete book" : "原创导读，非原著全文")
                : (english ? "Selected public-domain excerpts" : "公版原作选段");
        }
        public static string Excerpt(XgBookChapter chapter, bool english)
            => chapter == null ? "" : english && !string.IsNullOrEmpty(chapter.excerptEn) ? chapter.excerptEn : chapter.excerpt;
    }
}
