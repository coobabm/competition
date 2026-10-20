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
            new XgPoemQuote("举头望明月，低头玩电脑。","I raise my head to the bright moon, then lower it to play on the computer.",false,"举头望明月，低头思故乡。","I raise my head to the bright moon, then lower it and think of home.","李白《静夜思》","Li Bai, Quiet Night Thoughts"),
            // 2026-10-21: six more poems, each as the original and a 2016 parody (20 quotes).
            new XgPoemQuote("锄禾日当午，汗滴禾下土。","Hoeing at high noon, sweat drips onto the soil.",true,"锄禾日当午，汗滴禾下土。","Hoeing at high noon, sweat drips onto the soil.","李绅《悯农》","Li Shen, Pity the Farmers"),
            new XgPoemQuote("锄禾日当午，WiFi 没信号。","Hoeing at high noon, and no Wi-Fi signal.",false,"锄禾日当午，汗滴禾下土。","Hoeing at high noon, sweat drips onto the soil.","李绅《悯农》","Li Shen, Pity the Farmers"),
            new XgPoemQuote("离离原上草，一岁一枯荣。","Lush grass on the plain; each year it withers and thrives.",true,"离离原上草，一岁一枯荣。","Lush grass on the plain; each year it withers and thrives.","白居易《赋得古原草送别》","Bai Juyi, Grass on the Ancient Plain"),
            new XgPoemQuote("离离原上草，显卡又涨价。","Lush grass on the plain; graphics cards went up again.",false,"离离原上草，一岁一枯荣。","Lush grass on the plain; each year it withers and thrives.","白居易《赋得古原草送别》","Bai Juyi, Grass on the Ancient Plain"),
            new XgPoemQuote("两个黄鹂鸣翠柳，一行白鹭上青天。","Two orioles sing in green willows; a line of egrets climbs the blue sky.",true,"两个黄鹂鸣翠柳，一行白鹭上青天。","Two orioles sing in green willows; a line of egrets climbs the blue sky.","杜甫《绝句》","Du Fu, Quatrain"),
            new XgPoemQuote("两个黄鹂鸣翠柳，一行代码改到天亮。","Two orioles sing in green willows; one line of code, fixed until dawn.",false,"两个黄鹂鸣翠柳，一行白鹭上青天。","Two orioles sing in green willows; a line of egrets climbs the blue sky.","杜甫《绝句》","Du Fu, Quatrain"),
            new XgPoemQuote("独在异乡为异客，每逢佳节倍思亲。","Alone, a stranger in a strange land; at every festival I miss my family more.",true,"独在异乡为异客，每逢佳节倍思亲。","Alone, a stranger in a strange land; at every festival I miss my family more.","王维《九月九日忆山东兄弟》","Wang Wei, Remembering My Brothers on the Double Ninth"),
            new XgPoemQuote("独在异乡为异客，每逢佳节抢红包。","Alone, a stranger in a strange land; at every festival I grab red packets.",false,"独在异乡为异客，每逢佳节倍思亲。","Alone, a stranger in a strange land; at every festival I miss my family more.","王维《九月九日忆山东兄弟》","Wang Wei, Remembering My Brothers on the Double Ninth"),
            new XgPoemQuote("红豆生南国，春来发几枝。","Red beans grow in the south; how many sprigs in spring?",true,"红豆生南国，春来发几枝。","Red beans grow in the south; how many sprigs in spring?","王维《相思》","Wang Wei, Longing"),
            new XgPoemQuote("红豆生南国，快递几天到。","Red beans grow in the south; how many days until delivery?",false,"红豆生南国，春来发几枝。","Red beans grow in the south; how many sprigs in spring?","王维《相思》","Wang Wei, Longing"),
            new XgPoemQuote("朝辞白帝彩云间，千里江陵一日还。","At dawn I leave Baidi amid bright clouds; a thousand li to Jiangling in a day.",true,"朝辞白帝彩云间，千里江陵一日还。","At dawn I leave Baidi amid bright clouds; a thousand li to Jiangling in a day.","李白《早发白帝城》","Li Bai, Leaving Baidi at Dawn"),
            new XgPoemQuote("朝辞白帝彩云间，千里快递一日还。","At dawn I leave Baidi amid bright clouds; a thousand-li parcel, back in a day.",false,"朝辞白帝彩云间，千里江陵一日还。","At dawn I leave Baidi amid bright clouds; a thousand li to Jiangling in a day.","李白《早发白帝城》","Li Bai, Leaving Baidi at Dawn")
        };
        static XgTeachingPoetry(){Quotes=XgQuestionBank.Join(Quotes,XgQuestionBank.Quotes);} // appends XgQuestionBank (2026-10-21)
        public static string Explain(XgPoemQuote q,bool english)=>english?q.sourceEn+": "+q.originalLine+" ("+q.originalLineEn+")":q.source+"："+q.originalLine;
    }
}
