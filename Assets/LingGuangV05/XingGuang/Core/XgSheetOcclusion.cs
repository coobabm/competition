using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    /// <summary>Real occlusion measurements on the game's small demonstration networks, never an LLM explanation.</summary>
    public static class XgSheetOcclusion
    {
        public sealed class Result { public double baseline; public readonly double[] change=new double[49]; public string model; }
        static readonly Dictionary<string,Result> Cache=new Dictionary<string,Result>();
        static readonly Dictionary<string,XgTinyNet> Models=new Dictionary<string,XgTinyNet>();
        public static string Family(string arch)
        {
            if(arch=="perceptron")return "perceptron";
            if(arch=="mlp")return "mlp";
            if(arch=="lenet"||arch=="alexnet"||arch=="vgg"||arch=="googlenet"||arch=="resnet")return "cnn";
            return "";
        }
        public static bool Supports(XgCard card,string arch)=>card!=null&&card.dataset=="mnist"&&!(card.teachingImageKind??"").StartsWith("handwriting28")&&string.IsNullOrEmpty(card.patternA)&&Family(arch)!="";
        public static Result For(XgCard card,string arch)
        {
            if(!Supports(card,arch))return null;
            string family=Family(arch);int seed=(card.seed&int.MaxValue)%16,digit=Math.Max(0,Math.Min(9,card.digit)),asked=Math.Max(0,Math.Min(9,card.asked));
            string key=family+":"+digit+":"+asked+":"+seed;
            if(Cache.TryGetValue(key,out var cached))return cached;
            // This is precisely the generator and seed reduction used by TeachingPictures.Digit.
            var pixels=XgTinyData.Digit(digit,seed).pixels;
            Func<double[],double> predict;
            if(family=="cnn")predict=x=>XgTinyConvNet.Digits.P(x,asked);
            else
            {
                string modelKey=family+":"+asked;
                if(!Models.TryGetValue(modelKey,out var model))
                {
                    model=new XgTinyNet(64,family=="perceptron"?0:12,8127+asked);
                    var samples=new List<XgTinySample>();
                    for(int d=0;d<10;d++)for(int i=0;i<30;i++){var sample=XgTinyData.Digit(d,400+i);sample.label=d==asked;samples.Add(sample);}
                    for(int epoch=0;epoch<10;epoch++)foreach(var sample in samples)model.Learn(sample,.08);
                    Models[modelKey]=model;
                }
                predict=model.Forward;
            }
            var result=Measure(pixels,predict);result.model=family;
            if(Cache.Count>=32)Cache.Clear();Cache[key]=result;return result;
        }
        /// <summary>49 overlapping 2x2 masks at all 7x7 positions on the unchanged 8x8 input. Positive means masking lowers the chosen score.</summary>
        public static Result Measure(double[] pixels,Func<double[],double> predict)
        {
            if(pixels==null||pixels.Length!=64||predict==null)throw new ArgumentException("Expected an 8x8 input and predictor.");
            var result=new Result{baseline=predict(pixels)};var masked=(double[])pixels.Clone();
            for(int y=0;y<7;y++)for(int x=0;x<7;x++)
            {
                Array.Copy(pixels,masked,64);
                for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++)masked[(y+dy)*8+x+dx]=0;
                result.change[y*7+x]=result.baseline-predict(masked);
            }
            return result;
        }
    }
}
