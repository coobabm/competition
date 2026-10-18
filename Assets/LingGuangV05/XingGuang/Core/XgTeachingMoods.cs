using System;
namespace LingGuangV05.XingGuang
{
    public static class XgTeachingMoods
    {
        public static readonly string[] Chinese={"开心","难过","惊讶","生气","困倦","疑惑"};
        public static readonly string[] English={"Happy","Sad","Surprised","Angry","Sleepy","Confused"};
        static readonly string[][] Accepted={
            new[]{"开心","高兴","快乐","喜悦","happy","glad","joyful","cheerful"},
            new[]{"难过","伤心","悲伤","委屈","sad","unhappy","upset","sorrowful"},
            new[]{"惊讶","吃惊","震惊","惊奇","surprised","shocked","astonished","amazed"},
            new[]{"生气","愤怒","恼火","气愤","angry","mad","furious","annoyed"},
            new[]{"困倦","困","犯困","瞌睡","疲倦","sleepy","drowsy","tired"},
            new[]{"疑惑","困惑","不解","迷惑","confused","puzzled","unsure"}
        };
        public static string ImageKind(int mood,int today)=>mood==4&&today>=20160725?"meme_couch_2016":mood==0&&today>=20160808?"meme_power_2016":"face";
        public static bool Matches(int mood,string text)
        {
            if(mood<0||mood>=Accepted.Length||string.IsNullOrWhiteSpace(text)||text.Length>24)return false;
            string word=text.Trim().Trim('。','！','!','.','?','？').Trim();
            foreach(var synonym in Accepted[mood])if(string.Equals(word,synonym,StringComparison.OrdinalIgnoreCase))return true;
            return false;
        }
    }
}
