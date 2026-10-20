using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable]
    public sealed class XgBookPurchase
    {
        public string bookId = "";
        public double paid;
        public int day;
    }
    [Serializable]
    public sealed class XgBookCompletion
    {
        public string key = "", bookId = "";
        public int day, stage;
        public long historySequence;
        public bool acknowledged;
    }
    public sealed partial class XgState
    {
        public int bookLibraryVersion;
        public List<string> bookOwned = new List<string>();
        public List<XgBookPurchase> bookPurchases = new List<XgBookPurchase>();
        public List<XgBookCompletion> bookCompletions = new List<XgBookCompletion>();
    }
    public sealed partial class XgSim
    {
        public const string LibraryNode = "books.library";
        public bool LibraryOpen => Has(LibraryNode);
        public event Action<XgBook> BookPurchased;

        // Called before the existing repair path so loading never locks an in-progress chapter.
        void InitializeBookLibrary(bool newGame)
        {
            if (S.bookOwned == null) S.bookOwned = new List<string>();
            if (S.bookPurchases == null) S.bookPurchases = new List<XgBookPurchase>();
            if (S.bookCompletions == null) S.bookCompletions = new List<XgBookCompletion>();
            if (!newGame && S.bookLibraryVersion < 1)
                foreach (var b in XgBooks.LegacyBooks) if (!S.bookOwned.Contains(b.id)) S.bookOwned.Add(b.id);
            // Actual saved progress is evidence of an existing right to read; preserve it, without charging.
            if (S.books != null) foreach (var e in S.books)
                if (e != null && e.count > 0 && XgBooks.Get(e.key) != null && !S.bookOwned.Contains(e.key)) S.bookOwned.Add(e.key);
            if (XgBooks.Get(S.bookReading) != null && !S.bookOwned.Contains(S.bookReading)) S.bookOwned.Add(S.bookReading);
            var seen = new HashSet<string>();
            S.bookOwned.RemoveAll(id => XgBooks.Get(id) == null || !seen.Add(id));
            seen.Clear();
            S.bookPurchases.RemoveAll(p => p == null || XgBooks.Get(p.bookId) == null || !seen.Add(p.bookId));
            foreach (var p in S.bookPurchases) if (!S.bookOwned.Contains(p.bookId)) S.bookOwned.Add(p.bookId);
            seen.Clear();
            S.bookCompletions.RemoveAll(c => c == null || XgBooks.Get(c.bookId) == null || c.key != BookCompletionKey(c.bookId) || !seen.Add(c.key));
            S.bookLibraryVersion = 1;
            // Deliberately no completion/history/YY replay here. Unknown legacy completion time stays unknown.
        }
        public bool BookOwned(string id) => id != null && S.bookOwned.Contains(id);
        public IReadOnlyList<XgBook> OwnedBooks()
        {
            var result = new List<XgBook>();
            foreach (var b in XgBooks.All) if (BookOwned(b.id)) result.Add(b);
            return result;
        }
        public double BookPrice(string id) => XgBooks.LibraryEntry(id)?.price ?? double.PositiveInfinity;
        public string BuyBookBlocker(string id, IXgHost host)
        {
            var entry = XgBooks.LibraryEntry(id);
            if (entry == null) return T("没有这本书。", "No such book.");
            if (BookOwned(id)) return T("已在书架上。", "Already on the shelf.");
            if (!LibraryOpen) return T("先在科技解锁图书馆网页。", "Unlock the library website in Tech first.");
            if (S.stage < entry.stage) return T("第 " + entry.stage + " 阶段开放。", "Available at stage " + entry.stage + ".");
            if (!TodayKnown) return T("等待游戏日期同步。", "Waiting for the game date.");
            if (Today < entry.availableBy) return T("这本书尚未到馆。", "This book has not reached the library yet.");
            if (host == null || host.Money + 1e-9 < entry.price) return T("经费不足。", "Not enough funds.");
            return null;
        }
        public bool BuyBook(string id, IXgHost host)
        {
            if (BuyBookBlocker(id, host) != null) return false;
            double price = BookPrice(id);
            if (!host.Spend(price)) return false;
            S.bookOwned.Add(id);
            S.bookPurchases.Add(new XgBookPurchase { bookId = id, paid = price, day = Today });
            S.totalSpent += price;
            BookPurchased?.Invoke(XgBooks.Get(id));
            return true;
        }
        public static string BookCompletionKey(string id) => "book.complete:" + id;
        public XgBookCompletion PendingBookCompletion
        {
            get { foreach (var c in S.bookCompletions) if (!c.acknowledged) return c; return null; }
        }
        public bool AcknowledgeBookCompletion(string key)
        {
            foreach (var c in S.bookCompletions)
                if (c.key == key && !c.acknowledged) { c.acknowledged = true; return true; }
            return false;
        }
        public string BookCompletionText(XgBookCompletion completion)
        {
            var book = completion == null ? null : XgBooks.Get(completion.bookId);
            if (book == null) return "";
            return XgBooks.LibraryEntry(book.id).IsGuide
                ? T("《" + book.name + "》的原创导读读完了。", "Finished the original guide to " + book.nameEn + ".")
                : T("《" + book.name + "》的现有选段读完了。", "Finished the selected excerpts from " + book.nameEn + ".");
        }
        bool CommitBookCompletion(XgBook book)
        {
            string key = BookCompletionKey(book.id);
            foreach (var c in S.bookCompletions) if (c.key == key) return false;
            var completed = new XgBookCompletion { key = key, bookId = book.id, day = TodayKnown ? Today : 0, stage = S.stage };
            S.bookCompletions.Add(completed);
            // C3 owns history persistence. This call records only an observed completion, never a migration.
            bool guide = XgBooks.LibraryEntry(book.id).IsGuide;
            completed.historySequence = RecordBookCompletion(book.id, book.name + (guide ? "（原创导读）" : "（选段）"),
                book.nameEn + (guide ? " (original guide)" : " (selected excerpts)"), key);
            return true;
        }
    }
}
