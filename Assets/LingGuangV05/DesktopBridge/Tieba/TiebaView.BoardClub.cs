using LingGuangV05.Desktop.Media;
namespace LingGuangV05.Desktop.Tieba
{
    public sealed partial class TiebaView
    {
        void BoardClubRow()
        {
            var card=Card();
            TopicLine(card,BoardClubPicture.Key,T("[棋类交流] 周末小题，黑先一步连五", "[Board games] Weekend puzzle: Black wins in one"),()=>Go("board.club",""));
            Line(card,T("木棋盘 · 2016-05-21 · 原创棋盘截图", "WoodenBoard · 2016-05-21 · Original board screenshot"),14,Muted);
        }
        void DrawBoardClub()
        {
            var card=Card();
            Line(card,T("[棋类交流] 周末小题，黑先一步连五", "[Board games] Weekend puzzle: Black wins in one"),22,Ink);
            Line(card,T("木棋盘 · 1楼 · 2016-05-21", "WoodenBoard · Floor 1 · 2016-05-21"),14,Muted);
            Line(card,T("棋社网页那道小题，我把原图搬过来了。黑先，一步连五；A–O 从左到右，1–15 从上到下。欢迎自己摆一摆。", "Here is the original image from our club page. Black moves first: five in one. A–O left to right, 1–15 top to bottom. Try it on your board."),18,Ink);
            BoardClubPicture.Add(card);
            Line(card,T("棋盘原图可以拖动。", "The original board image can be dragged."),14,Muted);
        }
    }
}
