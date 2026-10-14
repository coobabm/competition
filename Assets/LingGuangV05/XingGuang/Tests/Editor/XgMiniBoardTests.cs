using NUnit.Framework;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgMiniBoardTests
    {
        [Test] public void GoCapturesAndLeavesSourceUntouched()
        {
            var b = new int[9,9]; b[4,4]=2; b[3,4]=b[4,3]=b[4,5]=1;
            Assert.That(XgMiniBoard.GoPlay(b,4,5,1,out var n,out int count),Is.True);
            Assert.That(count,Is.EqualTo(1)); Assert.That(n[4,4],Is.Zero); Assert.That(b[4,4],Is.EqualTo(2));
        }
        [Test] public void GoRejectsSuicideAndOccupiedPoint()
        {
            var b=new int[9,9]; b[3,4]=b[4,3]=b[4,5]=b[5,4]=2;
            Assert.That(XgMiniBoard.GoPlay(b,4,4,1,out _,out _),Is.False);
            Assert.That(XgMiniBoard.GoPlay(b,4,3,1,out _,out _),Is.False);
        }
        [Test] public void MergeEachTileOnlyOnce()
        {
            var b=new int[4,4]; b[0,0]=b[0,1]=b[0,2]=b[0,3]=1;
            Assert.That(XgMiniBoard.Slide(b,0,out int gain),Is.True);
            Assert.That(b[0,0],Is.EqualTo(2)); Assert.That(b[0,1],Is.EqualTo(2)); Assert.That(b[0,2],Is.Zero); Assert.That(gain,Is.EqualTo(4));
        }
        [Test] public void MergeNoOpDoesNotChangeBoard()
        {
            var b=new int[4,4]; b[0,0]=1;
            Assert.That(XgMiniBoard.Slide(b,0,out int gain),Is.False); Assert.That(gain,Is.Zero); Assert.That(b[0,0],Is.EqualTo(1));
        }
        [Test] public void TetrisBoundsAndBlocksCollide()
        {
            var b=new int[22,10]; var p=new int[,]{{1,1},{1,1}};
            Assert.That(XgMiniBoard.Collides(b,p,-1,0),Is.True);
            Assert.That(XgMiniBoard.Collides(b,p,9,0),Is.True);
            Assert.That(XgMiniBoard.Collides(b,p,0,21),Is.True);
            Assert.That(XgMiniBoard.Collides(b,p,0,0),Is.False);
            b[1,1]=2; Assert.That(XgMiniBoard.Collides(b,p,0,0),Is.True);
        }
        [Test] public void TetrisClearsAdjacentRowsAndCompacts()
        {
            var b=new int[22,10]; for(int x=0;x<10;x++) b[21,x]=b[20,x]=1; b[19,3]=7;
            Assert.That(XgMiniBoard.ClearRows(b),Is.EqualTo(2)); Assert.That(b[21,3],Is.EqualTo(7)); Assert.That(b[19,3],Is.Zero);
        }
        [Test] public void RotationPreservesFourCells()
        {
            var p=new int[,]{{0,1,0},{1,1,1},{0,0,0}}; var r=p;
            for(int i=0;i<4;i++) r=XgMiniBoard.Rotate(r);
            CollectionAssert.AreEqual(p,r);
        }
        [Test] public void LinkCanUseBorderButNotCrossOccupiedCells()
        {
            var b=new int[4,4]; b[1,1]=1;b[1,2]=2;
            Assert.That(XgMiniBoard.LinkPath(b,5,6),Is.Not.Null);
            for(int y=0;y<4;y++)for(int x=0;x<4;x++)b[y,x]=1;
            Assert.That(XgMiniBoard.LinkPath(b,5,10),Is.Null);
        }
        [Test] public void AllGoPuzzlesHaveALegalSolution()
        {
            foreach(var p in XgMiniGoPuzzle.All)
            { bool found=false; for(int y=0;y<9;y++)for(int x=0;x<9;x++)if(p.Judge(p.Board(),x,y))found=true;
              Assert.That(found,Is.True,p.Name); }
        }
    }
}
