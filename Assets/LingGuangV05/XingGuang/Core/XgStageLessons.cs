using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    // Stage lesson formats are additional teaching desks, not purchases in the 19-pack manifest.
    public static class XgStageLessons
    {
        public static readonly XgTeachingPack[] All={
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
