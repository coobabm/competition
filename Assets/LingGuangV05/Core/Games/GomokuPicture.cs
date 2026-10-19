using System;
namespace LingGuangV05.Core.Games
{
    /// <summary>Raster format for the native 15x15 teaching-board crop. No labels or move metadata travel with pixels.</summary>
    public static class GomokuPicture
    {
        public const int Size=240,Step=16;
        public static byte[] Render(Gomoku board)
        {
            if(board==null)throw new ArgumentNullException(nameof(board));var rgb=new byte[Size*Size*3];
            for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)
            {
                int a=x/Step,b=y/Step,dx=x%Step-8,dy=y%Step-8,stone=board[a,b];int at=(y*Size+x)*3;
                bool piece=stone!=0&&dx*dx+dy*dy<=36;bool grid=dx==0||dy==0;
                rgb[at]=(byte)(piece?(stone==1?25:245):grid?120:222);
                rgb[at+1]=(byte)(piece?(stone==1?25:245):grid?80:184);
                rgb[at+2]=(byte)(piece?(stone==1?25:245):grid?40:120);
            }
            return rgb;
        }
        public static bool TryRead(byte[] rgb,int width,int height,out Gomoku board)
        {
            board=null;if(rgb==null||width!=Size||height!=Size||rgb.Length!=Size*Size*3)return false;
            var cells=new int[225];
            for(int y=0;y<15;y++)for(int x=0;x<15;x++)
            {
                int black=0,white=0,empty=0;
                foreach(int dx in new[]{-3,3})foreach(int dy in new[]{-3,3})
                {
                    int at=((y*Step+8+dy)*Size+x*Step+8+dx)*3;int r=rgb[at],g=rgb[at+1],b=rgb[at+2];
                    if(r<80&&g<80&&b<80)black++;else if(r>220&&g>220&&b>220)white++;
                    else if(r>=180&&r<=245&&g>=140&&g<=215&&b>=75&&b<=165)empty++;
                }
                if(black==4)cells[y*15+x]=1;else if(white==4)cells[y*15+x]=2;else if(empty!=4)return false;
            }
            board=new Gomoku();for(int y=0;y<15;y++)for(int x=0;x<15;x++)if(cells[y*15+x]!=0&&!board.Place(x,y,cells[y*15+x])){board=null;return false;}
            // This reader is for nonterminal question positions, not arbitrary photos or compressed browser screenshots.
            if(board.Winner!=0){board=null;return false;}return true;
        }
    }
}
