using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    sealed class XgSudokuPack : XgSocialSurface
    {
        int[] grid,clues; int selected=-1; float elapsed; bool finished;
        readonly XgBtn[] cells=new XgBtn[81]; TMP_Text note;
        public XgSudokuPack(XgMiniContext c):base(c){}
        public override void Build()
        {
            Clear(XgUi.T("数独","Sudoku"),XgUi.T("每行、每列和每个九宫格填入1—9，不重复。选格后点数字，也可用键盘。","Fill every row, column and 3×3 box with 1–9 once. Select a cell, then a number."));
            grid=XgSudoku.Create(C.Random);clues=(int[])grid.Clone();selected=-1;elapsed=0;finished=false;
            C.Box("GridFrame",42,87,330,330,C.Ui.dark?XgDark.Muted:Color.gray);
            for(int i=0;i<81;i++)
            {
                int at=i,r=i/9,col=i%9;float x=44+col*36+col/3*2,y=89+r*36+r/3*2;
                cells[i]=B("Cell"+i,x,y,34,34,grid[i]==0?"":grid[i].ToString(),()=>{if(finished||clues[at]!=0)return;selected=at;Paint();},20);
            }
            T("PadTitle",425,100,400,34,XgUi.T("选一个数字","Choose a number"),23);
            for(int d=1;d<=9;d++){int digit=d;B("Digit"+d,425+(d-1)%3*66,150+(d-1)/3*57,58,49,d.ToString(),()=>Enter(digit),22);}
            B("Erase",425,328,190,40,XgUi.T("擦除","Erase"),()=>Enter(0),17);
            B("Next",640,328,180,40,XgUi.T("换一题","New puzzle"),Build,17);
            note=T("Progress",425,380,420,70,"",17);Paint();
        }
        void Enter(int digit)
        {
            if(finished||selected<0||clues[selected]!=0)return;
            grid[selected]=digit;Paint();
            if(!XgSudoku.Complete(grid))return;
            finished=true;note.text=XgUi.T("完成！这一盘成为新的逻辑训练样本。","Solved! This puzzle becomes a new logic training sample.");note.color=green;
            string puzzle=GridText(clues);
            C.Sim.RecordTeachingQuestion("完成这盘9×9数独（点表示空格）：\n"+puzzle,"Complete this 9×9 Sudoku (dots are blanks):\n"+puzzle,true,true,
                "每行、列和宫均为1—9。","Every row, column and box contains 1–9.","logic",answerText:GridText(grid));
            C.ReportTimed(true,elapsed);
        }
        static string GridText(int[] values)
        {
            var text=new System.Text.StringBuilder();
            for(int i=0;i<81;i++){if(i>0)text.Append(i%9==0?'\n':' ');text.Append(values[i]==0?".":values[i].ToString());}
            return text.ToString();
        }
        void Paint()
        {
            int empty=0;
            for(int i=0;i<81;i++)
            {
                if(grid[i]==0)empty++;
                cells[i].label.text=grid[i]==0?"":grid[i].ToString();
                cells[i].label.color=grid[i]!=0&&!XgSudoku.Allowed(grid,i,grid[i])?red:clues[i]!=0?(C.Ui.dark?XgDark.Ink:Color.black):purple;
                cells[i].label.fontStyle=i==selected?FontStyles.Bold|FontStyles.Underline:FontStyles.Normal;
            }
            if(note!=null){note.color=C.Ui.dark?XgDark.Muted:Color.gray;note.text=XgUi.T("还剩 "+empty+" 格 · 红字表示重复","Remaining: "+empty+" · Red means a duplicate");}
        }
        public override void Tick(float dt)
        {
            if(disposed||!C.IsActive||finished)return;elapsed+=dt;
            var kb=Keyboard.current;if(kb==null)return;
            var keys=new[]{kb.digit1Key,kb.digit2Key,kb.digit3Key,kb.digit4Key,kb.digit5Key,kb.digit6Key,kb.digit7Key,kb.digit8Key,kb.digit9Key};
            for(int i=0;i<9;i++)if(keys[i].wasPressedThisFrame){int d=i+1;C.PlayerInput(()=>Enter(d));return;}
            if(kb.backspaceKey.wasPressedThisFrame||kb.deleteKey.wasPressedThisFrame)C.PlayerInput(()=>Enter(0));
        }
    }
}
