namespace LingGuangV05.XingGuang
{
    public sealed class XgPoemQuote
    {
        public string line,lineEn,originalLine,originalLineEn,source,sourceEn;public bool original;
        public XgPoemQuote(string line,string en,bool original,string originalLine,string originalEn,string source,string sourceEn){this.line=line;lineEn=en;this.original=original;this.originalLine=originalLine;originalLineEn=originalEn;this.source=source;this.sourceEn=sourceEn;}
    }
    // Original poems are public-domain Tang texts. Parodies here are illustrative teaching text.
    public static class XgTeachingPoetry
    {
        public static readonly XgPoemQuote[] Quotes={
            new XgPoemQuote("床前明月光，疑是地上霜。","Moonlight before my bed; I thought it frost upon the ground.",true,"床前明月光，疑是地上霜。","Moonlight before my bed; I thought it frost upon the ground.","李白《静夜思》","Li Bai, Quiet Night Thoughts"),
            new XgPoemQuote("床前明月光，地上鞋两双。","Moonlight before my bed; two pairs of shoes on the floor.",false,"床前明月光，疑是地上霜。","Moonlight before my bed; I thought it frost upon the ground.","李白《静夜思》","Li Bai, Quiet Night Thoughts"),
            new XgPoemQuote("春眠不觉晓，处处闻啼鸟。","Sleeping in spring, dawn passes unnoticed; birds call everywhere.",true,"春眠不觉晓，处处闻啼鸟。","Sleeping in spring, dawn passes unnoticed; birds call everywhere.","孟浩然《春晓》","Meng Haoran, Spring Dawn"),
            new XgPoemQuote("春眠不觉晓，处处蚊子咬。","Sleeping in spring, dawn passes unnoticed; mosquitoes bite everywhere.",false,"春眠不觉晓，处处闻啼鸟。","Sleeping in spring, dawn passes unnoticed; birds call everywhere.","孟浩然《春晓》","Meng Haoran, Spring Dawn"),
            new XgPoemQuote("白日依山尽，黄河入海流。","The sun sinks behind the hills; the Yellow River runs into the sea.",true,"白日依山尽，黄河入海流。","The sun sinks behind the hills; the Yellow River runs into the sea.","王之涣《登鹳雀楼》","Wang Zhihuan, On the Stork Tower"),
            new XgPoemQuote("白日依山尽，钱包空荡荡。","The sun sinks behind the hills; my wallet is completely empty.",false,"白日依山尽，黄河入海流。","The sun sinks behind the hills; the Yellow River runs into the sea.","王之涣《登鹳雀楼》","Wang Zhihuan, On the Stork Tower"),
            new XgPoemQuote("举头望明月，低头思故乡。","I raise my head to the bright moon, then lower it and think of home.",true,"举头望明月，低头思故乡。","I raise my head to the bright moon, then lower it and think of home.","李白《静夜思》","Li Bai, Quiet Night Thoughts"),
            new XgPoemQuote("举头望明月，低头玩电脑。","I raise my head to the bright moon, then lower it to play on the computer.",false,"举头望明月，低头思故乡。","I raise my head to the bright moon, then lower it and think of home.","李白《静夜思》","Li Bai, Quiet Night Thoughts")
        };
        public static string Explain(XgPoemQuote q,bool english)=>english?q.sourceEn+": "+q.originalLine+" ("+q.originalLineEn+")":q.source+"："+q.originalLine;
    }
}
