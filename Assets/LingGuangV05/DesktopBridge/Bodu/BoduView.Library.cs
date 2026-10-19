using System;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.Bodu
{
    public sealed partial class BoduView
    {
        const int LibraryPageSize = 10;
        int libraryPage;
        string libraryLanguage = "", libraryBook = "";
        int libraryStatus;
        XgSim libraryOwner;
        ChapterOneSim librarySave;

        public void OpenLibrary()
        {
            Open(""); page = "library"; libraryBook = ""; signature = "";
            if (window != null) { XingGuangController.KeepOpenOnFirstStart(window); window.OpenWindow(); XingGuangController.EnsureShown(this, window); }
        }
        string LibrarySignature(XgSim sim)
        {
            var owner = XingGuangController.Instance;
            var save = owner?.runtime?.Sim;
            if (!ReferenceEquals(libraryOwner, sim) || !ReferenceEquals(librarySave, save))
            {
                libraryOwner = sim; librarySave = save;
                libraryPage = 0; libraryBook = ""; libraryStatus = 0; signature = "";
            }
            return sim == null || page != "library" ? "" : "|" + sim.LibraryOpen + "|" + sim.S.bookOwned.Count + "|" + Math.Floor(owner?.Host?.Money ?? 0).ToString("0");
        }
        bool LibraryCurrent(XingGuangController owner, XgSim sim, ChapterOneSim save)
            => owner != null && owner.LifeEventSettingsAvailable && ReferenceEquals(owner.Sim, sim)
                && ReferenceEquals(owner.runtime?.Sim, save) && owner.BoundTo(save) && ReferenceEquals(libraryOwner, sim);

        void LibraryPage(XgSim sim)
        {
            var owner = XingGuangController.Instance;
            var save = owner?.runtime?.Sim;
            bool Current() => LibraryCurrent(owner, sim, save);
            Line(T("← 摆渡首页", "← Bodu home"), 15, Link, () => { page = "home"; signature = ""; });
            Line(T("<b>摆渡图书馆</b> · 书籍与导读", "<b>Bodu Library</b> · Books and reading guides"), 25, Blue);
            Line(T("100 种书目 · 90 种中文原作 / 10 种英文原作\n购买的是下方标明的阅读材料；原创导读不含原著全文。", "100 titles · 90 Chinese originals / 10 English originals\nEach purchase supplies the material stated below. Original guides do not include the complete book."), 16, Ink);
            Line(T("打开我的书架", "Open my bookshelf"), 18, Link, () => { if (Current()) XgReadingWindow.Open(owner); });
            if (sim == null || owner == null) { Line(T("等待电脑连接……", "Waiting for the computer…"), 16, Muted); return; }
            if (!sim.LibraryOpen)
            {
                Line(T("在科技中解锁「图书馆网页」：阶段 2，普通标价 ¥500。入口费用不含书籍；已有书籍仍可从书架阅读。", "Unlock the library website in Tech: stage 2, ordinary price ¥500. Books are purchased separately; your existing shelf stays available."), 18, Ink);
                Line(T("前往科技", "Open Tech"), 18, Link, () => { if (Current()) { owner.Open(); owner.View?.ShowTab("tree"); } });
                return;
            }
            if (libraryStatus > 0) Line(libraryStatus == 1 ? T("已加入书架。", "Added to your shelf.")
                : T("已加入书架，存档暂未写入；请稍后保存。", "Added to your shelf. Saving failed; please save again shortly."), 16, Blue);
            if (libraryBook.Length > 0)
            {
                var e = XgBooks.LibraryEntry(libraryBook);
                if (e == null) { libraryBook = ""; signature = ""; return; }
                var book = XgBooks.Get(e.id);
                Line(T("← 返回书目", "← Back to catalogue"), 16, Link, () => { libraryBook = ""; signature = ""; });
                Line("<b>" + T(e.title, e.titleEn) + "</b>\n" + T(e.author, e.authorEn), 23, Ink);
                Line(XgBooks.MaterialLabel(book, GameText.IsEnglish), 18, Blue);
                Line(T("原作语言：", "Original language: ") + (e.language == "en" ? T("英文", "English") : T("中文", "Chinese"))
                    + " · " + T("出版依据：", "Publication basis: ") + Publication(e)
                    + "\n" + T("阶段 ", "Stage ") + e.stage + " · ¥" + e.price.ToString("0"), 16, Muted);
                Line(e.IsGuide ? T(e.introZh, e.introEn) : T("收录原有 ", "Includes the existing ") + book.chapters.Length + T(" 篇原作选段，保留阅读进度。", " selected excerpts, with your reading progress preserved."), 18, Ink);
                Line(T("材料来源与许可\n", "Material source and rights\n") + MaterialRights(e), 14, Muted);
                if (sim.BookOwned(e.id)) Line(T("已拥有 · 在书架中打开", "Owned · open on shelf"), 19, Link, () => { if (Current()) XgReadingWindow.Open(owner, e.id); });
                else
                {
                    string why = sim.BuyBookBlocker(e.id, owner.Host);
                    Line(why ?? T("购买阅读材料 · ¥", "Buy reading material · ¥") + e.price.ToString("0"), 19, why == null ? Link : Muted, why == null ? (Action)(() =>
                    {
                        if (!Current() || !sim.BuyBook(e.id, owner.Host)) return;
                        owner.runtime.MarkDirty();
                        bool saved = owner.SaveNow();
                        libraryStatus = saved ? 1 : 2;
                        signature = ""; XgReadingWindow.Open(owner, e.id);
                    }) : null);
                }
                return;
            }
            foreach (string code in new[] { "", "zh", "en" })
            {
                string value = code;
                Line((libraryLanguage == code ? "● " : "○ ") + (code == "" ? T("全部书目", "All titles") : code == "zh" ? T("中文原作", "Chinese originals") : T("英文原作", "English originals")), 16, Link,
                    () => { if (!Current()) return; libraryLanguage = value; libraryPage = 0; signature = ""; });
            }
            int count = 0;
            foreach (var e in XgBooks.Library) if (libraryLanguage.Length == 0 || e.language == libraryLanguage) count++;
            int pages = Math.Max(1, (count + LibraryPageSize - 1) / LibraryPageSize);
            libraryPage = Math.Min(libraryPage, pages - 1);
            Line(T("第 ", "Page ") + (libraryPage + 1) + " / " + pages, 15, Muted);
            if (libraryPage > 0) Line(T("上一页", "Previous page"), 16, Link, () => { if (Current()) { libraryPage--; signature = ""; } });
            if (libraryPage + 1 < pages) Line(T("下一页", "Next page"), 16, Link, () => { if (Current()) { libraryPage++; signature = ""; } });
            int index = 0;
            foreach (var e in XgBooks.Library)
            {
                if (libraryLanguage.Length > 0 && e.language != libraryLanguage) continue;
                int row = index++;
                if (row < libraryPage * LibraryPageSize || row >= (libraryPage + 1) * LibraryPageSize) continue;
                string id = e.id;
                Line("<u>" + T(e.title, e.titleEn) + "</u> — " + T(e.author, e.authorEn) + "\n<size=14>"
                    + XgBooks.MaterialLabel(XgBooks.Get(id), GameText.IsEnglish) + " · " + T("阶段 ", "Stage ") + e.stage
                    + (sim.BookOwned(id) ? T(" · 已拥有", " · Owned") : " · ¥" + e.price.ToString("0")) + "</size>", 19, Link,
                    () => { if (Current()) { libraryBook = id; signature = ""; } });
            }
        }
    }
}
