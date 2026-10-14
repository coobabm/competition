using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    /// <summary>Pure board rules. Go O(N²); slide O(N²); row compaction O(HW); links O(HW) states.</summary>
    public static class XgMiniBoard
    {
        static readonly int[] DX={-1,1,0,0}, DY={0,0,-1,1};
        public static List<int> GoGroup(int[,] b,int x,int y,out HashSet<int> liberties)
        {
            int h=b.GetLength(0),w=b.GetLength(1),c=b[y,x]; liberties=new HashSet<int>(); var stones=new List<int>();
            if(c==0)return stones;
            var seen=new HashSet<int>(); stones.Add(y*w+x);seen.Add(y*w+x);
            for(int i=0;i<stones.Count;i++) for(int d=0;d<4;d++)
            { int nx=stones[i]%w+DX[d],ny=stones[i]/w+DY[d];if(nx<0||ny<0||nx>=w||ny>=h)continue;
              int q=ny*w+nx;if(b[ny,nx]==0)liberties.Add(q);else if(b[ny,nx]==c&&seen.Add(q))stones.Add(q); }
            return stones;
        }
        public static int Liberties(int[,] b,int x,int y) { GoGroup(b,x,y,out var l);return l.Count; }
        public static bool GoPlay(int[,] b,int x,int y,int c,out int[,] next,out int captures)
        {
            next=null;captures=0;int h=b.GetLength(0),w=b.GetLength(1);
            if(x<0||y<0||x>=w||y>=h||b[y,x]!=0||(c!=1&&c!=2))return false;
            var n=(int[,])b.Clone();n[y,x]=c;
            for(int d=0;d<4;d++) {int nx=x+DX[d],ny=y+DY[d];if(nx<0||ny<0||nx>=w||ny>=h||n[ny,nx]!=3-c)continue;
                var group=GoGroup(n,nx,ny,out var libs);if(libs.Count!=0)continue;
                foreach(int s in group){n[s/w,s%w]=0;captures++;}}
            if(Liberties(n,x,y)==0)return false;next=n;return true;
        }
        // Ranks 1,2,... represent 100,200,... samples, exactly as the HTML reference.
        public static bool Slide(int[,] b,int direction,out int gain)
        {
            gain=0;bool changed=false;
            for(int line=0;line<4;line++)
            {
                var packed=new List<int>(4);for(int k=0;k<4;k++){Coord(direction,line,k,out int x,out int y);if(b[y,x]!=0)packed.Add(b[y,x]);}
                var output=new List<int>(4);for(int k=0;k<packed.Count;k++)
                {int v=packed[k];if(k+1<packed.Count&&v==packed[k+1]){output.Add(v+1);gain+=(int)Math.Min(65536,Math.Pow(2,v));k++;}else output.Add(v);}
                for(int k=0;k<4;k++){Coord(direction,line,k,out int x,out int y);int v=k<output.Count?output[k]:0;changed|=b[y,x]!=v;b[y,x]=v;}
            } return changed;
        }
        static void Coord(int d,int l,int k,out int x,out int y){x=d==0?k:d==2?3-k:l;y=d==1?k:d==3?3-k:l;}
        public static bool CanSlide(int[,] b){for(int d=0;d<4;d++)if(Slide((int[,])b.Clone(),d,out _))return true;return false;}
        public static int[,] Rotate(int[,] p)
        {int n=p.GetLength(0);var r=new int[n,n];for(int y=0;y<n;y++)for(int x=0;x<n;x++)r[y,x]=p[n-1-x,y];return r;}
        public static bool Collides(int[,] b,int[,] p,int px,int py)
        {for(int y=0;y<p.GetLength(0);y++)for(int x=0;x<p.GetLength(1);x++)if(p[y,x]!=0)
          {int bx=px+x,by=py+y;if(bx<0||by<0||bx>=b.GetLength(1)||by>=b.GetLength(0)||b[by,bx]!=0)return true;}return false;}
        public static int ClearRows(int[,] b)
        {
            int h=b.GetLength(0),w=b.GetLength(1),write=h-1,count=0;
            for(int y=h-1;y>=0;y--){bool full=true;for(int x=0;x<w;x++)full&=b[y,x]!=0;if(full){count++;continue;}
                for(int x=0;x<w;x++)b[write,x]=b[y,x];write--;}
            for(int y=write;y>=0;y--)for(int x=0;x<w;x++)b[y,x]=0;return count;
        }
        // Grid includes an empty border ring. Return every path cell, including endpoints.
        public static List<int> LinkPath(int[,] b,int start,int end)
        {
            int h=b.GetLength(0),w=b.GetLength(1);if(start==end)return null;
            var best=new int[h*w*4];var prev=new int[best.Length];for(int i=0;i<best.Length;i++){best[i]=99;prev[i]=-1;}
            var q=new Queue<int>();for(int d=0;d<4;d++){int s=start*4+d;best[s]=0;q.Enqueue(s);}
            while(q.Count>0)
            {
                int s=q.Dequeue(),cell=s/4,dir=s%4;if(cell==end){var path=new List<int>();for(int z=s;z>=0;z=prev[z])path.Add(z/4);path.Reverse();return path;}
                for(int d=0;d<4;d++){int x=cell%w+DX[d],y=cell/w+DY[d],turn=best[s]+(dir==d?0:1);
                    if(x<0||y<0||x>=w||y>=h||turn>2)continue;int to=y*w+x;if(to!=end&&b[y,x]!=0)continue;
                    int ns=to*4+d;if(best[ns]<=turn)continue;best[ns]=turn;prev[ns]=s;q.Enqueue(ns);}
            }return null;
        }
    }
}
