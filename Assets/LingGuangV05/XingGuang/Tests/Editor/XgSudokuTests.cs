using System;
using NUnit.Framework;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgSudokuTests
    {
        [Test] public void GeneratedPuzzlesHaveOneSolutionAndDoNotChangeDuringValidation()
        {
            for(int seed=0;seed<20;seed++)
            {
                var grid=XgSudoku.Create(new Random(seed));var original=(int[])grid.Clone();
                Assert.AreEqual(1,XgSudoku.CountSolutions(grid));CollectionAssert.AreEqual(original,grid);
                int gaps=0;for(int i=0;i<81;i++){if(grid[i]==0)gaps++;else Assert.IsTrue(XgSudoku.Allowed(grid,i,grid[i]));}
                Assert.AreEqual(36,gaps);Assert.IsFalse(XgSudoku.Complete(grid));
            }
        }
        [Test] public void CompletedGridAcceptsOnlyValidRowsColumnsAndBoxes()
        {
            var grid=new int[81];for(int i=0;i<81;i++)grid[i]=(i/9*3+i/27+i%9)%9+1;
            Assert.IsTrue(XgSudoku.Complete(grid));grid[0]=grid[1];Assert.IsFalse(XgSudoku.Complete(grid));Assert.AreEqual(0,XgSudoku.CountSolutions(grid));
        }
        [Test] public void SudokuIsAPurchasedStageTwoLogicPack()
        {
            var p=XgTeachingPacks.Find("sudoku");Assert.NotNull(p);Assert.AreEqual(2,p.stage);Assert.IsTrue(p.buy);Assert.AreEqual("logic",p.dataset);
            var sim=new XgSim();sim.S.stage=1;Assert.IsNotNull(sim.TeachingPackBlocker(p));
        }
    }
}
