using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    public sealed class XgDatedDanmu { public string text,textEn;public int tone,since;public XgDatedDanmu(string text,string textEn,int tone,int since=20160101){this.text=text;this.textEn=textEn;this.tone=tone;this.since=since;} }
    public static class XgTeachingDanmu
    {
        public static readonly XgDatedDanmu[] Lines={
            new XgDatedDanmu("这操作我服，太强了！","Respect, that play was amazing!",0),
            new XgDatedDanmu("这都能送？别坑队友了！","How did you feed again? Stop dragging us down!",1),
            new XgDatedDanmu("真厉害啊，零杀十死。","Amazing: zero kills and ten deaths.",2),
            new XgDatedDanmu("666，这波漂亮！","GG, beautiful play!",0),
            new XgDatedDanmu("你可真是个天才，把队友全挡住了。","What a genius: you blocked every teammate.",2),
            new XgDatedDanmu("主播辛苦了，讲得真清楚。","Thanks for streaming. That explanation was clear.",0),
            // 2026-10-21: 14 undated lines so the stage-two pack opens with 20+, plus two Rio lines.
            new XgDatedDanmu("解说声音真好听，听着很舒服。","The commentator's voice is so pleasant to listen to.",0),
            new XgDatedDanmu("画质糊成这样，还好意思开直播？","Picture this blurry and you still go live?",1),
            new XgDatedDanmu("哇，又掉线了，网速真快呢。","Wow, disconnected again. Such fast internet.",2),
            new XgDatedDanmu("这一手微操，我回放看了三遍。","That micro-play — I watched the replay three times.",0),
            new XgDatedDanmu("能不能别刷屏了，烦死了。","Can you stop spamming the chat? So annoying.",1),
            new XgDatedDanmu("这波团战指挥得太稳了！","That team-fight call was rock steady!",0),
            new XgDatedDanmu("好棒哦，又在泉水里挂机了。","Great job, idling at base again.",2),
            new XgDatedDanmu("主播唱得真好听，再来一首！","The streamer sings so well. One more!",0),
            new XgDatedDanmu("广告比正片还长，受不了了。","The ads are longer than the show. I can't stand it.",1),
            new XgDatedDanmu("真是大师操作，闪现撞墙。","A true master: flashed straight into a wall.",2),
            new XgDatedDanmu("UP 主剪得用心，字幕都对齐了。","The uploader edited with care; even the subtitles line up.",0),
            new XgDatedDanmu("别吵了，好好看比赛行不行。","Stop arguing and just watch the match.",1),
            new XgDatedDanmu("厉害厉害，一局死了十五次。","Impressive: fifteen deaths in one game.",2),
            new XgDatedDanmu("这个结局没想到，编剧牛！","Didn't see that ending coming. Great writing!",0),
            new XgDatedDanmu("里约开幕式看得我热血沸腾！","The Rio opening ceremony fired me up!",0,20160806),
            new XgDatedDanmu("女排这一局打得太提气了！","Women's volleyball, that set was so uplifting!",0,20160816),
            new XgDatedDanmu("这波欧洲杯分析有理有据，服！","That Euro 2016 analysis was convincing. Respect!",0,20160611),
            new XgDatedDanmu("队友在打团，你还葛优躺呢？","Your team is fighting while you lounge on the sofa?",1,20160725),
            new XgDatedDanmu("这波真用了洪荒之力，厉害！","You unleashed all your strength there. Amazing!",0,20160808),
            new XgDatedDanmu("洪荒之力都用上了，结果补刀还是零。","All that legendary strength, and still zero last hits.",2,20160808),
            new XgDatedDanmu("阴阳师这阵容搭得漂亮，学到了。","That Onmyoji team setup is brilliant. I learned something.",0,20160902),
            new XgDatedDanmu("别再故意送了，看得我蓝瘦香菇。","Stop feeding on purpose; this makes me so miserably sad.",1,20161010),
            new XgDatedDanmu("厉害了我的哥，这一波救场太及时！","Impressive, bro! That rescue was perfectly timed!",0,20161105),
            new XgDatedDanmu("你的名字解析得真细，终于看懂了。","Your analysis of Your Name was so clear. Now I get it.",0,20161202)
        };
        static XgTeachingDanmu(){Lines=XgQuestionBank.Join(Lines,XgQuestionBank.Danmu);} // appends XgQuestionBank (2026-10-21)
        public static XgDatedDanmu Pick(int today,Random random)
        {
            var available=new List<XgDatedDanmu>();var monthly=new List<XgDatedDanmu>();
            foreach(var line in Lines)if(line.since<=today){available.Add(line);if(line.since/100==today/100&&line.since>20160101)monthly.Add(line);}
            var bank=monthly.Count>0&&random.Next(2)==0?monthly:available;
            return bank.Count>0?bank[random.Next(bank.Count)]:Lines[0];
        }
    }
}
