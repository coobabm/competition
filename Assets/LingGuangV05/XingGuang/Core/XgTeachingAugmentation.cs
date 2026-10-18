using System;
namespace LingGuangV05.XingGuang
{
    public sealed class XgTeachingAugmentSession
    {
        internal XgSim owner; internal bool submitted;
        public readonly int digit,seed;
        internal XgTeachingAugmentSession(XgSim owner,int digit,int seed){this.owner=owner;this.digit=digit;this.seed=(seed&int.MaxValue)%16;}
    }
    public sealed class XgTeachingAugmentResult { public int kept,bad;public bool correct; }
    public sealed class XgTeachingShiftResult { public XgHandwritingGuess before,after;public bool lost; }
    public sealed partial class XgState { public int teachingAugmentKept,teachingAugmentBad; }
    public static class XgTeachingAugmentation
    {
        public const int Side=28;
        // Pixel transforms, not RectTransform decoration. All consume the displayed source raster.
        public static byte[] Transform(byte[] source,int variant)
        {
            if(source==null||source.Length!=Side*Side||variant<0||variant>3)throw new ArgumentException("Expected 28-square ink and variant 0..3");
            var output=new byte[source.Length];
            for(int y=0;y<Side;y++)for(int x=0;x<Side;x++)
            {
                int sx=x,sy=y;if(variant==0)sx=x-1;else if(variant==1){sx=Side-1-x;sy=Side-1-y;}else if(variant==2)sx=Side-1-x;
                byte value=sx>=0&&sx<Side?source[sy*Side+sx]:(byte)0;output[y*Side+x]=variant==3?(byte)(value*.65):value;
            }
            return output;
        }
        public static bool LabelPreserved(int digit,int variant)
        {if(digit<0||digit>9||variant<0||variant>3)return false;return variant==0||variant==3||digit==0||digit==1||digit==8;}
        public static bool[] RecognitionRaster(byte[] ink)
        {
            if(ink==null||ink.Length!=784)throw new ArgumentException("Expected 28-square ink");
            var raster=new bool[XgHandwriting.Grid*XgHandwriting.Grid];
            for(int y=0;y<XgHandwriting.Grid;y++)for(int x=0;x<XgHandwriting.Grid;x++)raster[y*XgHandwriting.Grid+x]=ink[(y*28/XgHandwriting.Grid)*28+x*28/XgHandwriting.Grid]>=80;
            return raster;
        }
    }
    public sealed partial class XgSim
    {
        public XgTeachingAugmentSession BeginTeachingAugmentation(int digit,int seed)
            =>Has("augment")&&digit>=0&&digit<=9?new XgTeachingAugmentSession(this,digit,seed):null;
        public XgTeachingAugmentResult FinishTeachingAugmentation(XgTeachingAugmentSession session,bool[] keep)
        {
            if(session==null||session.owner!=this||session.submitted||!Has("augment")||keep==null||keep.Length!=4)return null;
            session.submitted=true;var result=new XgTeachingAugmentResult{correct=true};var original=XgVisualRaster.Handwriting28(session.digit,session.seed);
            for(int i=0;i<4;i++)
            {
                bool valid=XgTeachingAugmentation.LabelPreserved(session.digit,i);if(keep[i]!=valid)result.correct=false;
                if(!keep[i])continue;
                var raster=XgTeachingAugmentation.RecognitionRaster(XgTeachingAugmentation.Transform(original,i));
                if(!XgHandwriting.Usable(raster))continue;
                // Retaining a damaged label really teaches that bad label; it is not a cosmetic error count.
                LearnHandwriting(session.digit,raster);result.kept++;if(!valid)result.bad++;
            }
            S.teachingAugmentKept+=result.kept;S.teachingAugmentBad+=result.bad;return result;
        }
        public XgTeachingShiftResult ObserveTeachingDigitShift(int digit,int seed)
        {
            if(digit<0||digit>9)return null;
            var ink=XgVisualRaster.Handwriting28(digit,(seed&int.MaxValue)%16);
            var before=ReadHandwriting(XgTeachingAugmentation.RecognitionRaster(ink));
            var after=ReadHandwriting(XgTeachingAugmentation.RecognitionRaster(XgTeachingAugmentation.Transform(ink,0)));
            var result=new XgTeachingShiftResult{before=before,after=after,lost=before.valid&&after.valid&&before.digit==digit&&after.digit!=digit};
            if(result.lost)NoteTrainingPhenomenon("shift");return result;
        }
    }
}
