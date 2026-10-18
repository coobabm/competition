using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    // Authored player exercises. No random answer is presented as a model prediction.
    sealed class XgDailyQuestions : XgPackQuestion
    {
        public XgDailyQuestions(XgMiniContext context):base(context) {}
        protected override void DrawQuestion()
        {
            if(C.Id=="daily_probability") Probability(); else Affairs();
        }
        void Options(string[] options,int answer,string whyZh,string whyEn)
        {
            var order=new List<int>();for(int i=0;i<options.Length;i++)order.Add(i);Shuffle(order);
            for(int i=0;i<order.Count;i++)
            {
                int choice=order[i];
                B("DailyChoice"+i,25+(i%2)*435,260+(i/2)*64,420,56,options[choice],()=>
                { if(submitted||!C.IsActive)return;chosenText=options[choice];Submit(choice==answer,whyZh,whyEn); },22);
            }
        }
        void Probability()
        {
            int kind=C.Random.Next(3),n=C.Random.Next(1,6);
            string[] answers; string whyZh,whyEn;
            if(kind==0)
            {
                promptZh="公平六面骰掷一次，点数不超过"+n+"的概率是多少？";
                promptEn="Roll a fair six-sided die. What is the probability of a result at most "+n+"?";
                answers=new[]{n+"/6",(n+1)+"/7","0","1"};
                whyZh="6个等可能结果中有"+n+"个满足条件，概率为"+n+"/6。";
                whyEn=n+" of 6 equally likely outcomes qualify: "+n+"/6.";
            }
            else if(kind==1)
            {
                promptZh="公平硬币独立抛两次，两次都是正面的概率？";
                promptEn="Toss a fair coin independently twice. Probability of two heads?";
                answers=new[]{"1/4","1/2","3/4","1"};whyZh="四种等可能结果中只有正正满足条件。";whyEn="Only HH qualifies among four equally likely outcomes.";
            }
            else
            {
                promptZh="袋中有"+n+"个红球和"+(6-n)+"个蓝球，等可能抽一个，抽到红球的概率？";
                promptEn="A bag has "+n+" red and "+(6-n)+" blue balls. Pick one uniformly: probability of red?";
                answers=new[]{n+"/6",(6-n)+"/7","0","1"};whyZh="红球数除以总球数："+n+"/6。";whyEn="Red balls divided by all balls: "+n+"/6.";
            }
            Header("概率小题","Probability questions","请选择概率。","Choose the probability.");
            T("DailyPrompt",35,100,830,110,XgUi.T(promptZh,promptEn),27);
            Options(answers,0,whyZh,whyEn);
        }
        void Affairs()
        {
            if(XgNewsCatalog.All.Count==0){var asset=Resources.Load<TextAsset>("LingGuangV05/Browser/browser_news");if(asset!=null)XgNewsCatalog.Load(asset.text);}
            var articles=new List<XgNewsArticle>();
            foreach(var a in XgNewsCatalog.All)
                if(!a.brief&&C.Sim.DailyNewsAlreadyRead(a.id)&&C.Sim.TodayKnown&&int.Parse(a.date.Replace("-",""))<=C.Sim.Today)articles.Add(a);
            Header("新闻回顾","News review","只回顾本存档里你已明确读完的新闻。","Review only articles confirmed as read in this save.");
            if(articles.Count==0){T("NoReadNews",35,120,830,110,XgUi.T("请先在摆渡读完一篇新闻。","Finish reading a Bodu article first."),24);return;}
            var article=articles[C.Random.Next(articles.Count)];
            DateTime date=DateTime.ParseExact(article.date,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
            bool accurate=C.Random.Next(2)==0;
            // An altered date belongs only to this judgment exercise, never the news archive.
            string proposed=accurate?article.date:date.AddDays(-7).ToString("yyyy-MM-dd");
            promptZh="判断：摆渡于 "+proposed+" 发布了《"+article.title+"》。";
            promptEn="True or false: Bodu published “"+article.titleEn+"” on "+proposed+".";
            T("DailyPrompt",35,100,830,110,XgUi.T(promptZh,promptEn),25);
            Options(new[]{XgUi.T("正确","True"),XgUi.T("错误","False")},accurate?0:1,
                "摆渡原文日期："+article.date,"Bodu article publication date: "+article.date);
        }
    }
}
