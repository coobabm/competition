using System;
using System.Collections.Generic;
using System.Globalization;
namespace LingGuangV05.XingGuang
{
    public static class XgTeachingHeadlines
    {
        public static XgPhrase Pick(int today,Random random)
        {
            var current=new List<XgNewsArticle>();var past=new List<XgNewsArticle>();
            foreach(var article in XgNewsCatalog.All){int day=int.Parse(article.date.Replace("-",""),CultureInfo.InvariantCulture);if(day>today)continue;past.Add(article);if(day/100==today/100)current.Add(article);}
            var bank=current.Count>0?current:past;if(bank.Count==0)return null;
            var item=bank[random.Next(bank.Count)];bool sourceBait=item.kind=="水文";bool wrapped=!sourceBait&&random.Next(2)==0;bool bait=sourceBait||wrapped;
            string title=item.title,titleEn=item.titleEn;
            if(wrapped){if(random.Next(2)==0){title="不看后悔一辈子！"+title;titleEn="Miss this and regret it forever! "+titleEn;}else{title+="，看完不转就亏大了！";titleEn+=" — share now or lose out!";}}
            return new XgPhrase{ text=title,textEn=titleEn,yes=bait,since=int.Parse(item.date.Replace("-",""),CultureInfo.InvariantCulture),source="news:"+item.id,topic=item.id,
                why=sourceBait?"这是夸张恐吓、隐藏关键事实的推广标题。":bait?"新闻事件是真的，夸张恐吓或强迫转发的包装是标题党。":"标题直接说明新闻事件，没有夸张恐吓或强迫转发。",
                whyEn=sourceBait?"This promotional headline exaggerates, frightens, or hides key facts.":bait?"The underlying event is real; the scare tactic or forced-sharing wrapper is clickbait.":"The title states the news directly without scare tactics or forced sharing."};
        }
    }
}
