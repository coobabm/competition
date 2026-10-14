using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgMiniActionTests
    {
        [Test] public void FastSwipeCrossesSmallTargetWithoutEndpointInside()
        { Assert.IsTrue(XgMiniActionRules.SegmentHits(0, 5, 100, 5, 40, 0, 10, 10)); }
        [Test] public void ParallelSwipeOutsideAndNegativeRectangleNeverHit()
        { Assert.IsFalse(XgMiniActionRules.SegmentHits(0, 11, 100, 11, 40, 0, 10, 10)); Assert.IsFalse(XgMiniActionRules.SegmentHits(0, 0, 0, 0, 0, 0, -1, 1)); }
        [Test] public void StationaryPointInsideIsHitAndOutsideIsNot()
        { Assert.IsTrue(XgMiniActionRules.SegmentHits(4, 4, 4, 4, 0, 0, 10, 10)); Assert.IsFalse(XgMiniActionRules.SegmentHits(12, 4, 12, 4, 0, 0, 10, 10)); }
        [Test] public void CircularCollisionRejectsFalsePositiveAtExpandedSquareCorner()
        { Assert.IsFalse(XgMiniActionRules.CircleHitsRect(-7, -7, 8, 0, 0, 10, 10)); }
        [Test] public void CircularCollisionIncludesTangentAndRejectsNegativeRadius()
        { Assert.IsTrue(XgMiniActionRules.CircleHitsRect(-8, 5, 8, 0, 0, 10, 10)); Assert.IsFalse(XgMiniActionRules.CircleHitsRect(5, 5, -1, 0, 0, 10, 10)); }
        [Test] public void BlankAndSingleDotAreNotRecognized()
        { var b = new bool[1024]; Assert.IsNull(XgMiniActionRules.Normalize(b, 32, 32)); b[12] = true; Assert.IsNull(XgMiniActionRules.Normalize(b, 32, 32)); }
        [Test] public void TeachingActuallyChangesNearestExampleAndCopiesInput()
        { var r = new XgMiniActionRecognizer(); var a = new float[400]; var b = new float[400]; b[0] = 1; r.Teach(1, a); r.Teach(7, b); b[0] = 0; Assert.AreEqual(7, r.Recognize(newFeature(), out _)); Assert.AreEqual(-1, r.Recognize(null, out _)); }
        static float[] newFeature() { var f = new float[400]; f[0] = 1; return f; }
        [Test] public void TeachingBoundedAndInvalidLabelIgnored()
        { var r = new XgMiniActionRecognizer(); for (int i = 0; i < 200; i++) r.Teach(i % 10, new float[400]); Assert.AreEqual(110, r.Count); r.Teach(10, new float[400]); Assert.AreEqual(110, r.Count); }
        [Test] public void DrawingMemoryRoundTripsBitsAndRejectsInvalidData()
        { var p = new bool[4096]; p[0] = p[4095] = true; var q = XgMiniActionMemory.Decode(XgMiniActionMemory.Encode(p)); Assert.IsTrue(q[0]); Assert.IsTrue(q[4095]); Assert.IsFalse(q[2048]); Assert.IsNull(XgMiniActionMemory.Decode("broken")); }
        [Test] public void DrawingMemoryRepairsLegacyNullAndCapsSavedSamples()
        { var s = new XgState { miniActionDrawings = null }; XgMiniActionMemory.Repair(s); Assert.AreEqual(0, s.miniActionDrawings.Count); for (int i = 0; i < 130; i++) XgMiniActionMemory.Remember(s, i % 10, new bool[4096]); Assert.AreEqual(100, s.miniActionDrawings.Count); s.miniActionDrawings.Add(new XgMiniActionDrawing { digit = 42, raster = "broken" }); XgMiniActionMemory.Repair(s); Assert.AreEqual(100, s.miniActionDrawings.Count); }
        [Test] public void RestoredDrawingTrainsFreshRecognizerFromActualRaster()
        {
            var p = new bool[4096]; for (int y = 10; y < 40; y++) p[y * 64 + 20] = true;
            var state = new XgState(); XgMiniActionMemory.Remember(state, 1, p);
            var model = new XgMiniActionRecognizer(); var sample = state.miniActionDrawings[0];
            model.Teach(sample.digit, XgMiniActionRules.Normalize(XgMiniActionMemory.Decode(sample.raster), 64, 64));
            Assert.AreEqual(1, model.Recognize(XgMiniActionRules.Normalize(p, 64, 64), out _));
        }
        [Test] public void EquationsIncludeNegativeSubtractionAndMultiplication()
        { Assert.AreEqual(-4, XgMiniActionRules.Arithmetic(3, 7, 1)); Assert.AreEqual(21, XgMiniActionRules.Arithmetic(3, 7, 2)); Assert.AreEqual(10, XgMiniActionRules.Arithmetic(3, 7, 0)); }
    }
}
