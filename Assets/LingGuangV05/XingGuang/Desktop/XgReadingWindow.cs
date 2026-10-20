using System;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Bodu;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Owned materials in a separate Aero window. Visibility gates life events, never background reading.</summary>
    public sealed partial class XgReadingWindow : MonoBehaviour
    {
        XingGuangController controller;
        XgSim bound;
        ChapterOneSim boundSave;
        PrologueDesk desk;
        RectTransform window, client, shelf, textContent;
        TMP_Text title, heading, material, body, progress, note, readLabel;
        Button readButton;
        string selected = "", signature = "", status = "";
        int chapter, shelfPage;
        float nextRefresh;
        const int ShelfPageSize = 9;
        static string T(string zh, string en) => GameText.T(zh, en);
        public bool Visible => isActiveAndEnabled && window != null && window.gameObject.activeInHierarchy;
        public RectTransform Window => window;
        bool Current(XgSim sim, ChapterOneSim save) => controller != null && controller.LifeEventSettingsAvailable
            && ReferenceEquals(controller.Sim, sim) && ReferenceEquals(controller.runtime?.Sim, save) && controller.BoundTo(save)
            && ReferenceEquals(bound, sim) && ReferenceEquals(boundSave, save);

        public static XgReadingWindow Install(XingGuangController owner)
        {
            var reader = owner.GetComponent<XgReadingWindow>() ?? owner.gameObject.AddComponent<XgReadingWindow>();
            reader.controller = owner; return reader;
        }
        public static void Open(XingGuangController owner, string bookId = null)
        {
            if (owner == null || owner.runtime?.Sim == null || owner.runtime.Sim.InPrologue || !owner.LifeEventSettingsAvailable) return;
            Install(owner).Show(bookId);
        }
        void Bind()
        {
            var sim = controller?.Sim; var save = controller?.runtime?.Sim;
            if (ReferenceEquals(bound, sim) && ReferenceEquals(boundSave, save)) return;
            Hide(); bound = sim; boundSave = save; selected = ""; chapter = shelfPage = 0; signature = ""; status = "";
        }
        void Show(string bookId)
        {
            Bind(); desk = PrologueDirector.Desk;
            if (desk == null || bound == null) return;
            if (window == null) Build();
            if (!string.IsNullOrEmpty(bookId) && bound.BookOwned(bookId)) Select(bookId);
            if (selected.Length == 0)
            {
                var owned = bound.OwnedBooks();
                if (bound.ReadingBook != null) Select(bound.ReadingBook.id);
                else if (owned.Count > 0) Select(owned[0].id);
            }
            window.gameObject.SetActive(true); window.SetAsLastSibling(); Clamp();
            XgLifeHostActivity.SetReadingWindowVisible(this, Visible); signature = ""; Refresh();
        }
        public void Hide()
        {
            CancelReaderReaction();
            if (window != null) window.gameObject.SetActive(false);
            XgLifeHostActivity.SetReadingWindowVisible(this, false);
        }
        void Update()
        {
            Bind();
            XgLifeHostActivity.SetReadingWindowVisible(this, Visible);
            if (!Visible || bound == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .25f; Refresh();
        }
        void OnDisable() { Hide(); }
        void OnDestroy() { XgLifeHostActivity.SetReadingWindowVisible(this, false); if (window != null) Destroy(window.gameObject); }
        void Build()
        {
            client = desk.Window("Reading Window", "", new Vector2(30, 10), new Vector2(820, 570), out window);
            window.gameObject.AddComponent<Canvas>(); window.gameObject.AddComponent<GraphicRaycaster>();
            var bar = (RectTransform)window.Find("Title"); title = bar.Find("Text").GetComponent<TMP_Text>();
            title.rectTransform.offsetMax = new Vector2(-100, 0);
            var close = bar.Find("Close").GetComponent<Button>(); close.onClick.RemoveAllListeners(); close.onClick.AddListener(Hide);
            ButtonAt(bar, "Minimize", "−", new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-85, -11), new Vector2(-56, 11), Hide);
            bar.gameObject.AddComponent<XgReadingWindowDrag>().owner = this;
            var toolbar = PrologueDesk.Rect("Toolbar", client, new Vector2(0, 1), Vector2.one, new Vector2(0, -42), Vector2.zero);
            PrologueDesk.Fill(toolbar, new Color32(239, 243, 247, 255));
            ButtonAt(toolbar, "Library", T("图书馆购书", "Library store"), Vector2.zero, new Vector2(0, 1), new Vector2(8, 6), new Vector2(150, -6), OpenStore);
            ButtonAt(toolbar, "YY", T("在 YY 回答", "Answer on YY"), Vector2.zero, new Vector2(0, 1), new Vector2(158, 6), new Vector2(300, -6), () => { if (Current(bound, boundSave)) XgYyTalk.Open(true); });
            ButtonAt(toolbar, "Primer", T("随附笔记", "Bundled note"), Vector2.zero, new Vector2(0, 1), new Vector2(308, 6), new Vector2(450, -6), () => { if (Current(bound, boundSave)) { selected = "primer"; signature = ""; Refresh(); } });
            shelf = PrologueDesk.Rect("Owned Shelf", client, Vector2.zero, new Vector2(0, 1), new Vector2(8, 8), new Vector2(236, -50));
            PrologueDesk.Fill(shelf, new Color32(242, 245, 248, 255));
            var detail = PrologueDesk.Rect("Reading Material", client, Vector2.zero, Vector2.one, new Vector2(248, 8), new Vector2(-10, -50));
            heading = Label(PrologueDesk.Rect("Heading", detail, new Vector2(0, 1), Vector2.one, new Vector2(0, -48), Vector2.zero), "", 22);
            material = Label(PrologueDesk.Rect("MaterialLabel", detail, new Vector2(0, 1), Vector2.one, new Vector2(0, -79), new Vector2(0, -49)), "", 14);
            var viewport = PrologueDesk.Rect("TextViewport", detail, Vector2.zero, Vector2.one, new Vector2(0, 145), new Vector2(0, -84));
            PrologueDesk.Fill(viewport, new Color32(255, 253, 247, 255)); viewport.gameObject.AddComponent<RectMask2D>();
            var scroller = viewport.gameObject.AddComponent<ScrollRect>(); scroller.horizontal = false; scroller.scrollSensitivity = 28;
            scroller.movementType = ScrollRect.MovementType.Clamped;
            textContent = PrologueDesk.Rect("TextContent", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            textContent.pivot = new Vector2(.5f, 1);
            var layout = textContent.gameObject.AddComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(12, 12, 12, 12); layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false;
            textContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            body = Label(PrologueDesk.Rect("Text", textContent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 19);
            body.richText = false; if (XgUiV4Fonts.Reading != null) body.font = XgUiV4Fonts.Reading;
            reactionText = Label(PrologueDesk.Rect("ReaderReaction", textContent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 16);
            reactionText.richText = false;
            reactionNotice = Label(PrologueDesk.Rect("AiReplyNotice:ReaderReaction", textContent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 12);
            AiContentNotice.BindFooter(reactionNotice);
            reactionNotice.alignment = TextAlignmentOptions.MidlineLeft;
            reactionNotice.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;
            reactionNotice.gameObject.SetActive(false);
            scroller.viewport = viewport; scroller.content = textContent;
            ButtonAt(detail, "PreviousChapter", "‹", Vector2.zero, Vector2.zero, new Vector2(0, 110), new Vector2(44, 140), () => Browse(-1));
            ButtonAt(detail, "NextChapter", "›", Vector2.zero, Vector2.zero, new Vector2(50, 110), new Vector2(94, 140), () => Browse(1));
            readButton = ButtonAt(detail, "ReadNext", "", Vector2.zero, new Vector2(1, 0), new Vector2(105, 110), new Vector2(0, 140), () => { });
            readLabel = readButton.GetComponentInChildren<TMP_Text>();
            progress = Label(PrologueDesk.Rect("Progress", detail, Vector2.zero, new Vector2(1, 0), new Vector2(0, 80), new Vector2(0, 106)), "", 14);
            note = Label(PrologueDesk.Rect("Note", detail, Vector2.zero, new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 76)), "", 14);
            note.overflowMode = TextOverflowModes.Ellipsis;
        }
        TMP_Text Label(RectTransform rect, string text, float size)
            => desk.Text(rect, text, size, PrologueDesk.Ink, TextAlignmentOptions.TopLeft);
        Button ButtonAt(RectTransform parent, string id, string text, Vector2 a, Vector2 b, Vector2 min, Vector2 max, Action click)
        {
            var rect = PrologueDesk.Rect(id, parent, a, b, min, max); var fill = PrologueDesk.Fill(rect, new Color32(219, 231, 242, 255));
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = fill; button.onClick.AddListener(() => click());
            var label = desk.Text(PrologueDesk.Rect("Text", rect, Vector2.zero, Vector2.one, new Vector2(6, 2), new Vector2(-6, -2)), text, 14, PrologueDesk.Ink, TextAlignmentOptions.Center);
            label.raycastTarget = false; return button;
        }
        void Select(string id)
        {
            if (bound == null || !bound.BookOwned(id)) return;
            selected = id; var book = XgBooks.Get(id);
            chapter = Math.Min(bound.ChaptersRead(book), book.chapters.Length - 1); status = ""; signature = "";
        }
        void Browse(int direction)
        {
            if (!Current(bound, boundSave)) return;
            var book = XgBooks.Get(selected); if (book == null || !bound.BookOwned(selected)) return;
            chapter = Mathf.Clamp(chapter + direction, 0, book.chapters.Length - 1); signature = ""; Refresh();
        }
        void StartChapter(string id, XgSim owner, ChapterOneSim save)
        {
            if (!Current(owner, save) || selected != id) return;
            string why = owner.StartReading(id, controller.Host);
            status = why ?? T("开始阅读。", "Reading started.");
            if (why == null) { chapter = bound.S.bookChapter; controller.runtime.MarkDirty(); if (!controller.SaveNow()) status = T("正在阅读；存档暂未写入，请稍后保存。", "Reading; saving failed, please save again shortly."); }
            signature = ""; Refresh();
        }
        void OpenStore()
        {
            if (!Current(bound, boundSave)) return;
            var browser = FindAnyObjectByType<BoduView>(FindObjectsInactive.Include);
            if (browser != null) browser.OpenLibrary();
        }
        void Refresh()
        {
            if (!Visible || bound == null || !Current(bound, boundSave)) return;
            var owned = bound.OwnedBooks(); var book = XgBooks.Get(selected);
            if (selected != "primer" && book != null && !bound.BookOwned(selected)) { selected = ""; book = null; }
            string now = selected + ":" + chapter + ":" + shelfPage + ":" + GameText.IsEnglish + ":" + owned.Count + ":" + bound.ChaptersRead(book) + ":" + bound.S.bookReading + ":" + bound.S.bookPuzzle + ":" + bound.S.stage;
            if (now != signature)
            {
                signature = now; title.text = T("读书 · 我的书架", "Reading · My bookshelf");
                // Language changes replace toolbar copy as well as material text.
                client.Find("Toolbar/Library/Text").GetComponent<TMP_Text>().text = T("图书馆购书", "Library store");
                client.Find("Toolbar/YY/Text").GetComponent<TMP_Text>().text = T("在 YY 回答", "Answer on YY");
                client.Find("Toolbar/Primer/Text").GetComponent<TMP_Text>().text = T("随附笔记", "Bundled note");
                for (int i = shelf.childCount - 1; i >= 0; i--) { shelf.GetChild(i).gameObject.SetActive(false); Destroy(shelf.GetChild(i).gameObject); }
                Label(PrologueDesk.Rect("OwnedCount", shelf, new Vector2(0, 1), Vector2.one, new Vector2(8, -32), new Vector2(-8, -6)), T("已拥有 ", "Owned: ") + owned.Count, 17);
                int pages = Math.Max(1, (owned.Count + ShelfPageSize - 1) / ShelfPageSize); shelfPage = Math.Min(shelfPage, pages - 1);
                var owner = bound; var save = boundSave; var bookId = selected;
                readButton.onClick.RemoveAllListeners();
                readButton.onClick.AddListener(() => StartChapter(bookId, owner, save));
                for (int i = shelfPage * ShelfPageSize; i < Math.Min(owned.Count, (shelfPage + 1) * ShelfPageSize); i++)
                {
                    var item = owned[i]; int row = i % ShelfPageSize;
                    ButtonAt(shelf, "Book_" + item.id, T(item.name, item.nameEn) + "\n" + bound.ChaptersRead(item) + "/" + item.chapters.Length,
                        new Vector2(0, 1), Vector2.one, new Vector2(6, -80 - row * 43), new Vector2(-6, -39 - row * 43), () => { if (Current(owner, save)) { Select(item.id); Refresh(); } });
                }
                if (shelfPage > 0) ButtonAt(shelf, "PreviousPage", "‹", Vector2.zero, Vector2.zero, new Vector2(8, 4), new Vector2(52, 32), () => { if (Current(owner, save)) { shelfPage--; signature = ""; Refresh(); } });
                if (shelfPage + 1 < pages) ButtonAt(shelf, "NextPage", "›", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-52, 4), new Vector2(-8, 32), () => { if (Current(owner, save)) { shelfPage++; signature = ""; Refresh(); } });
                if (book == null)
                {
                    heading.text = selected == "primer" ? T(XgSim.PrimerTitle, XgSim.PrimerTitleEn) : T("书架里还没有书", "Your shelf is empty");
                    material.text = selected == "primer" ? T("随附笔记 · 已保留", "Bundled note · preserved") : T("购买后加入书架", "Purchases appear here");
                    body.text = selected == "primer" ? bound.PrimerPage() : T("从摆渡图书馆选一本。已有存档的书籍和进度会保留。", "Choose a title in Bodu Library. Existing books and progress are preserved.");
                }
                else
                {
                    chapter = Mathf.Clamp(chapter, 0, book.chapters.Length - 1); var ch = book.chapters[chapter];
                    heading.text = T(book.name, book.nameEn) + " · " + (chapter + 1) + "/" + book.chapters.Length;
                    material.text = XgBooks.MaterialLabel(book, GameText.IsEnglish);
                    body.text = T(ch.title, ch.titleEn) + "\n\n" + XgBooks.Excerpt(ch, GameText.IsEnglish);
                }
                textContent.anchoredPosition = Vector2.zero;
            }
            string blocker = book == null ? T("请先选书", "Choose a book") : bound.ReadBlocker(book, controller.Host);
            readButton.interactable = book != null && blocker == null;
            readLabel.text = blocker ?? T("阅读下一篇", "Read next chapter");
            progress.text = book == null ? "" : T("已读 ", "Read: ") + bound.ChaptersRead(book) + "/" + book.chapters.Length
                + (bound.ReadingBook == book ? " · " + XgSim.Pct(bound.ReadingProgress) : "");
            string pending = bound.S.redMansionBeat == 1 ? T("它问：后面呢？请到 YY 回答。", "It asks: What comes next? Answer on YY.")
                : bound.BookPuzzlePending != null ? T("有一个阅读问题等待 YY 回复。", "A reading question awaits your reply on YY.") : "";
            note.text = status.Length > 0 ? status + "\n" + pending : pending.Length > 0 ? pending : book == null ? "" : bound.BookNote(book);
            RefreshReaderReaction(book);
        }
        public void Move(Vector2 delta) { if (window != null) { window.anchoredPosition += delta; Clamp(); } }
        void Clamp()
        {
            if (window == null || !(window.parent is RectTransform parent)) return;
            var rect = parent.rect; var half = window.sizeDelta * .5f;
            window.anchoredPosition = new Vector2(Mathf.Clamp(window.anchoredPosition.x, rect.xMin + half.x, rect.xMax - half.x), Mathf.Clamp(window.anchoredPosition.y, rect.yMin + half.y + 42, rect.yMax - half.y));
        }
    }
    public sealed class XgReadingWindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {
        public XgReadingWindow owner; Vector2 previous;
        public void OnPointerDown(PointerEventData e) { if (owner?.Window != null) owner.Window.SetAsLastSibling(); }
        public void OnBeginDrag(PointerEventData e) { if (owner?.Window != null) RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)owner.Window.parent, e.position, e.pressEventCamera, out previous); }
        public void OnDrag(PointerEventData e)
        {
            if (owner?.Window == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)owner.Window.parent, e.position, e.pressEventCamera, out var now);
            owner.Move(now - previous); previous = now;
        }
    }
}
