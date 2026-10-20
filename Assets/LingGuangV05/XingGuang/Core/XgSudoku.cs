using System;
namespace LingGuangV05.XingGuang
{
    /// <summary>Small standard 9x9 puzzles. Clues are removed only while the solution stays unique.</summary>
    public static class XgSudoku
    {
        public static int[] Create(Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            int[] digits = {1,2,3,4,5,6,7,8,9}, order = new int[81], grid = new int[81];
            Shuffle(digits, random);
            for (int i=0;i<81;i++) { int r=i/9,c=i%9; grid[i]=digits[(r*3+r/3+c)%9]; order[i]=i; }
            // Transpose and band/stack preserving permutations retain all Sudoku constraints.
            int[] rows=Permutation(random), cols=Permutation(random), varied=new int[81];
            for(int r=0;r<9;r++)for(int c=0;c<9;c++)varied[r*9+c]=grid[rows[r]*9+cols[c]];
            Shuffle(order, random); int removed=0;
            foreach(int at in order)
            {
                int old=varied[at]; varied[at]=0;
                if (CountSolutions(varied,2)!=1) varied[at]=old;
                else if(++removed>=36) break;
            }
            return varied;
        }
        static int[] Permutation(Random r)
        {
            int[] bands={0,1,2}, result=new int[9]; Shuffle(bands,r);
            for(int b=0;b<3;b++){int[] inner={0,1,2};Shuffle(inner,r);for(int i=0;i<3;i++)result[b*3+i]=bands[b]*3+inner[i];}
            return result;
        }
        static void Shuffle(int[] a,Random r){for(int i=a.Length-1;i>0;i--){int j=r.Next(i+1);int t=a[i];a[i]=a[j];a[j]=t;}}
        public static bool Allowed(int[] grid,int at,int value)
        {
            if(grid==null||grid.Length!=81||at<0||at>=81||value<1||value>9)return false;
            int row=at/9,col=at%9;
            for(int i=0;i<81;i++)if(i!=at&&grid[i]==value&&
                (i/9==row||i%9==col||(i/27==row/3&&i%9/3==col/3)))return false;
            return true;
        }
        public static bool Complete(int[] grid)
        {if(grid==null||grid.Length!=81)return false;for(int i=0;i<81;i++)if(!Allowed(grid,i,grid[i]))return false;return true;}
        public static int CountSolutions(int[] puzzle,int limit=2)
        {
            if(puzzle==null||puzzle.Length!=81||limit<1)return 0;
            var copy=(int[])puzzle.Clone();
            for(int i=0;i<81;i++)if(copy[i]!=0&&!Allowed(copy,i,copy[i]))return 0;
            return Solve(copy,limit);
        }
        static int Solve(int[] g,int limit)
        {
            int best=-1,mask=0,min=10;
            for(int i=0;i<81;i++)if(g[i]==0)
            {
                int m=0,n=0;for(int d=1;d<=9;d++)if(Allowed(g,i,d)){m|=1<<d;n++;}
                if(n==0)return 0;if(n<min){min=n;best=i;mask=m;if(n==1)break;}
            }
            if(best<0)return 1;
            int total=0;
            for(int d=1;d<=9&&total<limit;d++)if((mask&(1<<d))!=0){g[best]=d;total+=Solve(g,limit-total);}
            g[best]=0;return total;
        }
    }
}
