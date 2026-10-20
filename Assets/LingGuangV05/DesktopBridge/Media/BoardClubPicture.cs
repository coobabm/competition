using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang.MiniGames;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.Media
{
    /// <summary>Original 2016 chess-club illustration; shared by the local web page and public post.</summary>
    public static class BoardClubPicture
    {
        public const string Key="board_club_2016";
        static Sprite picture;
        public static Sprite Picture()
        {
            if(picture!=null)return picture;
            // A legal alternating eight-ply position, Black to move. The reader never receives this sequence.
            var board=GomokuLogic.Replay(new[]{"D6","B2","E6","D2","G6","F2","H6","H2"});
            var rgb=GomokuPicture.Render(board);var colors=new Color32[240*240];
            for(int y=0;y<240;y++)for(int x=0;x<240;x++)
            {int p=(y*240+x)*3;colors[(239-y)*240+x]=new Color32(rgb[p],rgb[p+1],rgb[p+2],255);}
            var texture=new Texture2D(240,240,TextureFormat.RGB24,false);
            texture.name="Chess club 2016 original screenshot";texture.hideFlags=HideFlags.DontSave;
            texture.SetPixels32(colors);texture.Apply();texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;
            picture=Sprite.Create(texture,new Rect(0,0,240,240),new Vector2(.5f,.5f));
            picture.hideFlags=HideFlags.DontSave;return picture;
        }
        public static void Add(RectTransform parent)
        {
            var row=PrologueDesk.Rect("Public board screenshot",parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight=240;
            var crop=PrologueDesk.Rect("Board crop",row,new Vector2(0,1),new Vector2(0,1),new Vector2(0,-240),new Vector2(240,0));
            var image=DesktopMedia.Paint(crop,Key,false);
            image.raycastTarget=true;image.gameObject.AddComponent<XgBoardPictureDrag>();
        }
    }
}
