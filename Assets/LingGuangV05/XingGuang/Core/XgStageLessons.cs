using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    // Stage lesson formats are additional teaching desks, not purchases in the 19-pack manifest.
    public static class XgStageLessons
    {
        public static readonly XgTeachingPack[] All={
            new XgTeachingPack{id="board_nextmove",name="下一步走哪",nameEn="Next move",dataset="go",stage=3,needs="nextmove",play="读棋局，从四个合法落点中选出连五或挡四的一步。",playEn="Read the position and choose the winning or blocking move from four legal options."},
            new XgTeachingPack{id="daily_probability",name="概率小题",nameEn="Probability questions",dataset="arith",stage=2,needs="daily.dice",play="按等可能结果计算概率。",playEn="Calculate probabilities from equally likely outcomes."},
            new XgTeachingPack{id="daily_affairs",name="新闻回顾",nameEn="News review",dataset="headline",stage=2,needs="daily.news",play="回顾你已读完新闻的发布日期。",playEn="Recall the publication dates of articles you finished reading."},
            new XgTeachingPack{id="stage_photo",name="看图选词",nameEn="Picture to word",dataset="imagenet",stage=3,needs="imagenet",play="看真实照片，从四个词里选出对应的名字。",playEn="Look at a real photograph and choose its name from four words."},
            new XgTeachingPack{id="stage_sentence",name="短句找错字",nameEn="Fix the sentence",dataset="longtext",stage=4,play="点出错的字词，句子全对就选没错。",playEn="Tap the wrong word, or choose No mistake."},
            new XgTeachingPack{id="stage_translation",name="翻译打分",nameEn="Score a translation",dataset="translate",stage=5,play="判断准、有点歪或错了；有点歪时点出问题词。",playEn="Choose Accurate, Slightly off or Wrong; locate the weak word when slightly off."},
            new XgTeachingPack{id="stage_compare",name="挑好答案",nameEn="Choose a better answer",dataset="longtext",stage=6,play="看问题和两个回答，挑更准确、有帮助的那个。",playEn="Read the question and pick the more accurate, helpful answer."}
        };
        public static IEnumerable<XgTeachingPack> Desks
        {get{foreach(var p in XgTeachingPacks.All)yield return p;foreach(var p in All)yield return p;}}
        public static XgTeachingPack Find(string id){foreach(var p in All)if(p.id==id)return p;return null;}
    }
}
