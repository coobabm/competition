using System;
using System.Linq;
using NUnit.Framework;
using LingGuangV05.Core.Games;
namespace LingGuangV05.Tests
{
    public sealed class GomokuLogicTests
    {
        static Gomoku Line(int stone){var b=new Gomoku();for(int x=2;x<7;x++)if(x!=4)b.Place(x,6,stone);return b;}
        [Test] public void ImmediateWinAndBlockAreComputedFromCells()
        {
            var win=Line(1);Assert.True(GomokuLogic.Pick(win,1,1,new Random(1),null,out int x,out int y));Assert.AreEqual(4,x);Assert.AreEqual(6,y);
            var block=Line(2);Assert.True(GomokuLogic.Pick(block,1,1,new Random(1),null,out x,out y));Assert.AreEqual(4,x);Assert.AreEqual(6,y);
            Assert.AreEqual(4,win.Moves);Assert.AreEqual(4,block.Moves);
        }
        [Test] public void WinningNowTakesPrecedenceOverBlocking()
        {
            var b=Line(1);for(int column=2;column<7;column++)if(column!=4)b.Place(column,9,2);
            Assert.True(GomokuLogic.Pick(b,1,3,new Random(1),null,out int x,out int y));Assert.AreEqual(6,y);Assert.True(b.Place(x,y,1));Assert.AreEqual(1,b.Winner);
        }
        [Test] public void LevelZeroCanPickAnyEmptyCellInsteadOfRankedRunnerUp()
        {
            var b=Line(1);var selected=new System.Collections.Generic.HashSet<string>();
            for(int seed=0;seed<40;seed++){Assert.True(GomokuLogic.Pick(b,1,0,new Random(seed),null,out int x,out int y));Assert.AreEqual(0,b[x,y]);selected.Add(x+":"+y);}
            Assert.Greater(selected.Count,10);
        }
        [Test] public void PlanningAndCopyLeaveOriginalBoardUntouched()
        {
            var b=new Gomoku();b.Place(7,7,1);b.Place(6,7,2);b.Place(7,8,1);
            var copy=b.Copy();copy.Place(0,0,2);Assert.AreEqual(0,b[0,0]);
            Assert.True(GomokuLogic.Pick(b,2,3,new Random(2),null,out int x,out int y));Assert.AreEqual(0,b[x,y]);Assert.AreEqual(3,b.Moves);Assert.AreEqual(7,b.LastX);Assert.AreEqual(8,b.LastY);
        }
        [Test] public void PictureReaderReadsChangedPixelsAndRejectsUnsupportedImages()
        {
            var b=Line(1);b.Place(12,12,2);var rgb=GomokuPicture.Render(b);
            Assert.True(GomokuPicture.TryRead(rgb,240,240,out var read));for(int y=0;y<15;y++)for(int x=0;x<15;x++)Assert.AreEqual(b[x,y],read[x,y]);
            Assert.False(GomokuPicture.TryRead(rgb,239,240,out _));
            foreach(int dx in new[]{-3,3})foreach(int dy in new[]{-3,3}){int at=((8+dy)*240+8+dx)*3;rgb[at]=255;rgb[at+1]=0;rgb[at+2]=0;}
            Assert.False(GomokuPicture.TryRead(rgb,240,240,out _));
        }
        [Test] public void ReplayRejectsIllegalOrPostTerminalMoves()
        {
            Assert.IsNull(GomokuLogic.Replay(new[]{"A1","A1"}));Assert.IsNull(GomokuLogic.Replay(new[]{"Z1"}));
            var b=GomokuLogic.Replay(new[]{"A1","B1","A2"});Assert.NotNull(b);Assert.AreEqual(1,b[0,1]);Assert.AreEqual(2,b[1,0]);
        }
    }
}
