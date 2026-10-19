using LingGuangV05.Desktop.Media;
namespace LingGuangV05.Desktop.Bodu
{
    public sealed partial class BoduView
    {
        void BoardClubLink()=>Line(T("巷口棋社 · 五子棋读谱练习", "Lane Chess Club · Gomoku board practice"),18,Link,()=>{page="board.club";signature="";});
        void BoardClubPage()
        {
            Line(T("返回摆渡首页","Back to Bodu home"),16,Link,()=>{page="home";signature="";});
            Line(T("巷口棋社 · 黑棋下一手", "Lane Chess Club · Black to move"),24,Ink);
            Line(T("2016-05-21 · 棋社练习页（游戏内原创）", "2016-05-21 · Club practice page (original game content)"),14,Muted);
            Line(T("周末摆了一盘小题：黑先，找一步连五。横向 A–O，纵向 1–15 从上往下数。", "A weekend puzzle: Black to move, find five in one. Columns A–O; rows 1–15 from top to bottom."),18,Ink);
            BoardClubPicture.Add(content);
            Line(T("原始棋盘图 · 240×240 · 可拖动", "Original board image · 240×240 · draggable"),14,Muted);
        }
    }
}
