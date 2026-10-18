using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgTrainPage
    {
        XgBtn booksTab;
        bool booksView;
        RectTransform booksPane;
        TMP_Text shelfSummary;
        void BuildBooksTab()
        {
            booksTab = ui.Button(gearCard, T("书架", "Books"), () => XgReadingWindow.Open(XingGuangController.Instance), 12);
            booksTab.rt.gameObject.name = "BooksTab";
            PlaceTopLeft(booksTab, 312, 6, 60, 22);
            // Compatibility with the sidebar's existing tab visibility rules. All reading lives in its own window.
            booksPane = Rect("BooksPane", gearCard, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -34));
            shelfSummary = ui.Text(Rect("ShelfSummary", booksPane, Vector2.zero, Vector2.one, new Vector2(8, 60), new Vector2(-8, -12)), "", 15, XgDark.Ink, TextAlignmentOptions.TopLeft);
            var open = ui.Button(booksPane, T("打开读书窗口", "Open reading window"), () => XgReadingWindow.Open(XingGuangController.Instance), 14);
            open.rt.gameObject.name = "OpenReadingWindow"; PlaceBottom(open, 10, 36, 8, 8);
        }
        void RefreshBooks()
        {
            if (shelfSummary == null || Sim == null) return;
            shelfSummary.text = T("已拥有 ", "Owned: ") + Sim.OwnedBooks().Count
                + T(" 本\n购买、书架与章节进度在独立读书窗口查看；阅读问题在 YY 回答。", " titles\nOpen the reading window for your shelf and chapter progress. Answer reading questions on YY.");
        }
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

    }
}
