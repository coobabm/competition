using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    sealed class XgPoetryGame : XgSocialSurface
    {
        readonly HashSet<string> used=new HashSet<string>();readonly List<string> log=new List<string>();
        // The key characters and the poems stay Chinese in both languages; only the gloss is English.
        static readonly string[] Themes={"月","春","风","花","月"},ThemesEn={"moon","spring","wind","flower","moon"};
        string theme,themeEn;string[] options;int round,wins,losses,turn,phase;float left,aiDelay;string verdict;
        public XgPoetryGame(XgMiniContext c):base(c){}
        public override void Build(){round=wins=losses=0;NextRound();}
        void NextRound(){
            if(round>=5){phase=3;End(XgUi.T("飞花令 · 五局结束","Poem Chain · Five rounds over"),XgUi.T($"你获胜 {wins} 局，失利 {losses} 局，其余平局。\n真诗、含题字、没有重复，缺一不可。",$"You won {wins}, lost {losses}, and the rest were draws.\nA real poem, with the key character, no repeats: all three are required."),Build);return;}
            themeEn=ThemesEn[round];theme=Themes[round++];used.Clear();log.Clear();turn=0;verdict="";PlayerTurn();
        }
        List<string> Available(){var list=new List<string>();foreach(var s in XgMiniSocialRules.Poems)if(XgMiniSocialRules.PoemValid(s,theme,used))list.Add(s);return list;}
        void PlayerTurn(){
            var pool=Available();if(pool.Count==0){RoundEnd(false,XgUi.T("你已经想不出未说过的真诗。","You cannot think of any real poem line you have not used."));return;}
            phase=0;left=10;string real=pool[C.Random.Next(pool.Count)];
            var list=new List<string>{real,real+"呀",theme+"山轻烟梦中游"};
            string fourth=null;foreach(var s in used){fourth=s;break;}if(fourth==null)foreach(var s in XgMiniSocialRules.Poems)if(!s.Contains(theme)){fourth=s;break;}
            list.Add(fourth??"今天月亮真明亮");Shuffle(list);options=list.ToArray();Draw();
        }
        void Draw(){
            Clear(XgUi.T("飞花令 · 第 "+round+" / 5 局","Poem Chain · Round "+round+" / 5"),XgUi.T("你和灵光轮流接带题字的真诗。10 秒内作答，假诗、没有题字和说过的都算输。","You and LingGuang take turns with a real poem line holding the key character. 10 s to answer; a fake line, a missing character or a repeat loses."));
            Shade(22,93,440,330);T("Theme",40,100,400,52,XgUi.T("本局题字：「"+theme+"」","Key character: "+theme+" ("+themeEn+")"),30,red);
            T("Chain",40,166,400,235,log.Count>0?string.Join("\n",log):XgUi.T("还没人开口。\n你先来：从右边挑一句带「"+theme+"」的真诗。","Nobody has spoken yet.\nYou start: pick a real line with \""+theme+"\" from the right."),20,log.Count>0?(Color?)null:XgPalette.Muted).alignment=TMPro.TextAlignmentOptions.TopLeft;T("Judge",485,95,390,70,verdict,19);
            timer=T("Time",690,8,185,36,"",19);if(phase==0)Clock(left);
            if(phase==0)for(int i=0;i<4;i++){int k=i;B("Poem"+i,490,173+i*62,380,53,(i+1)+"  "+options[i],()=>Pick(k),22);}
            else if(phase==1)T("Thinking",495,218,365,100,XgUi.T("灵光正在接下一句……","LingGuang is thinking of the next line..."),24,purple);
            else B("Next",595,350,265,58,XgUi.T(round>=5?"查看成绩":"下一局",round>=5?"See results":"Next round"),NextRound);
            T("Score",35,438,840,32,XgUi.T($"你 {wins} : {losses} 灵光  · 已使用 {used.Count} 句",$"You {wins} : {losses} LingGuang  · {used.Count} lines used"),18);
        }
        void Pick(int index){if(phase!=0)return;phase=1;bool good=index>=0&&XgMiniSocialRules.PoemValid(options[index],theme,used);
            if(index>=0)C.Report(good);
            if(!good){if(index>=0)log.Add(XgUi.T("你：","You: ")+options[index]+XgUi.T("（驳回）"," (rejected)"));RoundEnd(false,index<0?XgUi.T("时间到，你没有接上。","Time is up; you did not follow."):XgUi.T("评委：假诗、没有题字或已经说过。","Judge: fake poem, missing key character, or already said."));return;}
            used.Add(options[index]);log.Add(XgUi.T("你：","You: ")+options[index]);turn++;if(turn>=8){RoundEnd(null,XgUi.T("双方各说四句，平局。","Four lines each: a draw."));return;}verdict=XgUi.T("评委：真诗且未重复，通过。","Judge: real poem, no repeat. Passed.");aiDelay=.7f+(1-(float)C.Skill)*2.2f;Draw();
        }
        void AiTurn(){
            var pool=Available();if(pool.Count==0){log.Add(XgUi.T("灵光：……","LingGuang: ..."));RoundEnd(true,XgUi.T("灵光想不出新句了。","LingGuang cannot think of a new line."));return;}
            bool good=C.Random.NextDouble()<.18+.8*C.Skill;
            string line=good?pool[C.Random.Next(pool.Count)]:theme+"山轻烟梦中游";log.Add(XgUi.T("灵光：","LingGuang: ")+line+(good?"":XgUi.T("（编造）"," (made up)")));
            if(!good){RoundEnd(true,XgUi.T("评委：查无此句，灵光出局。","Judge: no such line. LingGuang is out."));return;}
            used.Add(line);turn++;if(turn>=8){RoundEnd(null,XgUi.T("双方各说四句，平局。","Four lines each: a draw."));return;}verdict=XgUi.T("评委：灵光通过，轮到你了。","Judge: LingGuang passes. Your turn.");PlayerTurn();
        }
        void RoundEnd(bool? won,string why){phase=2;if(won==true)wins++;else if(won==false)losses++;verdict=why;Draw();}
        public override void Tick(float dt){if(disposed||!C.IsActive)return;if(phase==1){aiDelay-=dt;if(aiDelay<=0)AiTurn();return;}if(phase!=0)return;left-=dt;Clock(left);if(left<=0){Pick(-1);return;}for(int i=0;i<4;i++)if(C.KeyDown((Key)((int)Key.Digit1+i))){Pick(i);break;}}
    }

    sealed class XgMemeGame : XgSocialSurface
    {
        // Opening, suitable counter, three unsuitable counters, explanation.
        static readonly string[][] Exchanges={
            new[]{"宝宝心里苦","抱抱，不哭","你瞅啥","红包拿来","辣眼睛","难过需要安慰，不是挑衅。"},
            new[]{"你瞅啥","来啊，互相伤害啊","宝宝心里苦","摸摸头","晚安","群聊斗图中的挑衅，用夸张反击接梗。"},
            new[]{"厉害了我的哥","干杯，庆祝一下","不要啊","我太难了","辣眼睛","夸赞和胜利适合共同庆祝。"},
            new[]{"吓死宝宝了","淡定，稳住","你咋不上天","红包拿来","哈哈你输了","惊吓需要稳住情绪。"},
            new[]{"友谊的小船说翻就翻","摸摸头，船扶正","就这？","你瞅啥","辣眼睛","失落的玩笑可以用安慰接住。"},
            new[]{"洪荒之力，全交了","厉害了，干杯","然并卵","蓝瘦香菇","不要啊","展示成就时适合赞赏。"}
        };
        // English counterpart of Exchanges: same order and shape.
        static readonly string[][] ExchangesEn={
            new[]{"Baby is hurting inside","Hug, don't cry","What are you looking at?","Send me a red packet","My eyes, they burn","Sadness needs comfort, not a challenge."},
            new[]{"What are you looking at?","Bring it on, let's hurt each other","Baby is hurting inside","There, there (pats head)","Good night","A taunt in a group-chat meme fight: answer with an over-the-top comeback to keep the joke going."},
            new[]{"Awesome, my bro","Cheers, let's celebrate","No way!","Life is too hard for me","My eyes, they burn","Praise and victory call for celebrating together."},
            new[]{"You scared the baby out of me","Stay calm, steady now","Why don't you fly to the sky then?","Send me a red packet","Haha, you lose","A scare calls for steadying your nerves."},
            new[]{"The boat of friendship capsizes in a blink","Pat pat, boat upright again","Is that it?","What are you looking at?","My eyes, they burn","A gloomy joke can be caught with comfort."},
            new[]{"Gave it every ounce of strength","Impressive, cheers","Still useless","Feeling blue, so blue","No way!","When someone shows off an achievement, praise is the right reply."}
        };
        int round,hp,lives,combo,correct,aiPick,right;float left,hintDelay;bool done,hinted;string[] row,rowEn;int[] order;XgBtn[] buttons=new XgBtn[4];
        public XgMemeGame(XgMiniContext c):base(c){}
        string Opt(int i)=>XgUi.T(row[order[i]],rowEn[order[i]]);
        public override void Build(){round=combo=correct=0;hp=12;lives=3;Next();}
        void Next(){if(hp<=0||lives<=0){done=true;End(hp<=0?XgUi.T("斗图获胜","Meme battle won"):XgUi.T("斗图失利","Meme battle lost"),XgUi.T($"命中 {correct} 次；对手体力 {hp}，你的体力 {lives}。\n灵光只负责助攻，出手的是你。",$"Hits: {correct}; opponent HP {hp}, your HP {lives}.\nLingGuang only cheers you on; the replies are yours."),Build);return;}
            round++;done=false;hinted=false;left=3;hintDelay=.7f+(1-(float)C.Skill)*1.9f;int xi=C.Random.Next(Exchanges.Length);row=Exchanges[xi];rowEn=ExchangesEn[xi];order=new[]{1,2,3,4};Shuffle(order);right=Array.IndexOf(order,1);aiPick=Guess(right,4);
            Clear(XgUi.T("斗图 · 第 "+round+" 回合","Meme Battle · Round "+round),XgUi.T("3 秒内回一张。三连击伤害加倍；第 4 回合灵光入群，用紫色文字助攻。","Reply with one meme within 3 s. A three-hit streak deals double damage; from round 4 LingGuang joins the chat and assists in purple text."));
            T("Battle",24,88,680,35,XgUi.T($"表哥体力 {hp}/12    你的体力 {lives}/3    连击 {combo}",$"Cousin HP {hp}/12    Your HP {lives}/3    Streak {combo}"),22);
            timer=T("Timer",718,88,160,35,"",20);Clock(left);
            Shade(28,143,375,145);T("Incoming",47,154,337,120,XgUi.T("表哥发来\n「"+row[0]+"」","Cousin sends\n\""+rowEn[0]+"\""),30);
            T("Companion",438,145,420,100,XgUi.T(round>=4?"灵光已入群，正在挑回复……":"灵光在群外观察你的回击。",round>=4?"LingGuang is in the chat, picking a reply...":"LingGuang watches your comebacks from outside the chat."),22,purple);
            for(int i=0;i<4;i++){int k=i;buttons[i]=B("Meme"+i,22+i*218,306,204,95,(i+1)+"\n"+Opt(i),()=>Pick(k),22);}
        }
        void Pick(int choice){if(done)return;done=true;bool ok=choice==right;
            if(choice>=0)C.ReportTimed(ok,3-left);
            if(ok){correct++;combo++;hp=Math.Max(0,hp-1-(combo>=3?1:0)-(round>=4&&aiPick==right?1:0));}else{lives--;combo=0;}
            T("Reveal",25,410,650,58,XgUi.T((ok?"命中！":"没有接住。")+row[5]+(round>=4?" 灵光选「"+Opt(aiPick)+"」。":""),(ok?"Hit! ":"Missed it. ")+rowEn[5]+(round>=4?" LingGuang picked \""+Opt(aiPick)+"\".":"")),17,ok?green:red);
            B("Next",715,415,160,48,XgUi.T(hp<=0||lives<=0?"看成绩":"下一回合",hp<=0||lives<=0?"See results":"Next round"),Next,18);
        }
        public override void Tick(float dt){if(disposed||done||!C.IsActive)return;left-=dt;hintDelay-=dt;Clock(left);if(left<=0){Pick(-1);return;}
            if(round>=4&&!hinted&&hintDelay<=0){hinted=true;buttons[aiPick].label.text=(aiPick+1)+XgUi.T("  灵\n","  LG\n")+Opt(aiPick);buttons[aiPick].label.color=purple;}
            for(int i=0;i<4;i++)if(C.KeyDown((Key)((int)Key.Digit1+i))){Pick(i);break;}}
    }

    sealed class XgReviewGame : XgSocialSurface
    {
        // Each fake has an observable clue; a short positive review alone is not fake.
        static readonly string[][] Fake={
            new[]{"05-24 02:18  小林\n真好用，质量太棒！推荐买买买！","同一分钟、相同文案"},
            new[]{"05-24 02:18  小王\n真好用，质量太棒！推荐买买买！","同一分钟、相同文案"},
            new[]{"05-25 10:21  阿明\n店家说晒五星截图返 5 元，先好评。","明示好评返现"},
            new[]{"05-19 09:00  小美\n用了三个月了，这款真不错！","早于 05-22 上架且使用时长矛盾"},
            new[]{"05-26 11:07  同学\n商家发了模板：质量超级棒，五星哦！","商家提供刷评模板"}};
        static readonly string[][] FakeEn={
            new[]{"05-24 02:18  Xiao Lin\nWorks great, amazing quality! Highly recommend, buy buy buy!","Same minute, identical text"},
            new[]{"05-24 02:18  Xiao Wang\nWorks great, amazing quality! Highly recommend, buy buy buy!","Same minute, identical text"},
            new[]{"05-25 10:21  A-Ming\nSeller said post a five-star screenshot and get 5 yuan back. Good review first.","Openly offers cashback for good reviews"},
            new[]{"05-19 09:00  Xiao Mei\nBeen using it for three months, this one is really good!","Dated before the 05-22 listing; usage time contradicts it"},
            new[]{"05-26 11:07  Classmate\nThe seller sent a template: Super quality, five stars!","Seller supplied a fake-review template"}};
        static readonly string[] Real={"05-25 08:20 阿青\n充电两个小时，续航一天，比旧的强。","05-25 19:03 刘先生\n包装有个角压坏了，东西没事。","05-26 11:30 胖虎\n物流很快，周二下单周四到了。","05-26 21:16 白白\n音量挺大，但按键有点硬。","05-27 09:41 小李\n给爸爸买的，他觉得字太小。","05-27 15:28 芳芳\n用了两天，还没发现问题。","05-27 22:03 阿森\n一般，和描述有点差别。","05-28 08:11 小云\n客服补发了缺的配件。","05-28 18:42 路人\n好评。暂时能用。"},
        RealEn={"05-25 08:20 A-Qing\nCharged for two hours, lasts a day, better than my old one.","05-25 19:03 Mr. Liu\nOne corner of the package was crushed, but the item is fine.","05-26 11:30 Pang Hu\nShipping was fast: ordered Tuesday, arrived Thursday.","05-26 21:16 Baibai\nPretty loud, but the buttons are a bit stiff.","05-27 09:41 Xiao Li\nBought it for my dad; he finds the text too small.","05-27 15:28 Fang Fang\nUsed it for two days, no problems so far.","05-27 22:03 A-Sen\nSo-so, a little different from the description.","05-28 08:11 Xiao Yun\nSupport re-sent the missing accessory.","05-28 18:42 Passerby\nGood review. Works for now."};
        sealed class Entry {public string text,why;public bool fake,done,hint;public XgBtn button;}
        readonly List<Entry> entries=new List<Entry>();int page,total,found,wrong,quota;float left,hintDelay;bool done,hinted;
        public XgReviewGame(XgMiniContext c):base(c){}
        public override void Build(){page=total=wrong=0;Next();}
        void Next(){if(page>=3){done=true;End(XgUi.T("评论审计结束","Review audit finished"),XgUi.T($"找到 {total} 条刷评，误点 {wrong} 条。\n简短好评不等于刷评，应看重复、时间矛盾和返现证据。",$"Found {total} fake reviews, {wrong} wrong taps.\nA short good review is not necessarily fake; look for duplicates, timeline contradictions and cashback evidence."),Build);return;}
            page++;quota=page+2;found=0;left=45;done=hinted=false;hintDelay=1+(1-(float)C.Skill)*3;
            entries.Clear();for(int i=0;i<quota;i++)entries.Add(new Entry{text=XgUi.T(Fake[i][0],FakeEn[i][0]),why=XgUi.T(Fake[i][1],FakeEn[i][1]),fake=true});
            var real=new List<int>();for(int i=0;i<Real.Length;i++)real.Add(i);Shuffle(real);for(int i=0;entries.Count<12;i++)entries.Add(new Entry{text=XgUi.T(Real[real[i]],RealEn[real[i]]),why=XgUi.T("细节自然，无确定刷评证据","Natural detail, no firm evidence of a fake review")});Shuffle(entries);
            Clear(XgUi.T("找水军 · "+page+" / 3 页","Spot the Fakes · Page "+page+" / 3"),XgUi.T("商品上架 05-22。每页 12 条藏 3～5 条刷评；45 秒内找齐，误点扣 3 秒。","Item listed 05-22. Each page of 12 reviews hides 3-5 fake ones; find them all in 45 s. A wrong tap costs 3 s."));
            timer=T("Timer",710,8,170,36,"",19);Clock(left);
            for(int i=0;i<12;i++){var e=entries[i];float x=18+(i%3)*292,y=88+(i/3)*79;e.button=B("Review"+i,x,y,282,72,e.text,()=>Choose(e),14);}
            T("Progress",20,408,620,28,XgUi.T("不要只看星级。找出全部证据再翻页。","Do not judge by stars alone. Find all the evidence before turning the page."),16);
        }
        void Choose(Entry e){if(done||e.done)return;e.done=true;C.Report(e.fake);e.button.label.text=(e.fake?XgUi.T("刷评：","Fake: "):XgUi.T("真实：","Genuine: "))+e.why;e.button.label.color=e.fake?green:red;
            if(e.fake){found++;total++;}else{wrong++;left=Math.Max(0,left-3);}C.Say(e.why);if(found==quota||left<=0)FinishPage();}
        void FinishPage(){if(done)return;done=true;for(int i=0;i<entries.Count;i++){var e=entries[i];if(e.fake&&!e.done){e.button.label.text=XgUi.T("漏掉：","Missed: ")+e.why;e.button.label.color=red;}}
            T("Results",20,437,620,36,XgUi.T($"本页找到 {found}/{quota}。没点到的就漏过去了。",$"Found {found}/{quota} on this page. The ones you did not tap slipped through."),18);
            B("Next",690,420,190,48,XgUi.T(page>=3?"查看成绩":"下一页",page>=3?"See results":"Next page"),Next,18);
        }
        public override void Tick(float dt){if(disposed||done||!C.IsActive)return;left-=dt;hintDelay-=dt;Clock(left);if(left<=0){FinishPage();return;}
            if(!hinted&&hintDelay<=0){hinted=true;foreach(var e in entries){if(e.done)continue;e.hint=C.Random.NextDouble()<C.Skill?e.fake:!e.fake;if(e.hint){e.button.label.text=XgUi.T("灵：可疑\n","LG: suspicious\n")+e.text;e.button.image.color=new Color32(232,220,250,255);}}}}
    }
}
