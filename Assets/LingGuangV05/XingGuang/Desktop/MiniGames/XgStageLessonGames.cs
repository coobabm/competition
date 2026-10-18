using System;
using UnityEngine;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    sealed class XgSentenceLesson : XgPackQuestion
    {
        static readonly string[][] Zh={new[]{"我","已经","准被","好了"},new[]{"请","保持","安静"},new[]{"今天","天气","晴朗"},new[]{"这件事","值得","在接再厉"},new[]{"他","戴上","眼睛"},new[]{"出门前","记得","检察","门窗"}};
        static readonly string[][] En={new[]{"I","am","redy","now"},new[]{"Please","keep","quiet"},new[]{"Today's","weather","is sunny"},new[]{"This","deserves","more effert"},new[]{"He","wears","glassess"},new[]{"Before leaving","remember to","chek","the windows"}};
        static readonly int[] Bad={2,-1,-1,2,2,2};
        static readonly string[] Fix={"准备","没错","没错","再接再厉","眼镜","检查"},FixEn={"ready","No mistake","No mistake","effort","glasses","check"};
        public XgSentenceLesson(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            int n=C.Random.Next(Zh.Length);promptZh=string.Join("",Zh[n])+"。";promptEn=string.Join(" ",En[n])+".";
            Header("短句找错字","Fix the sentence","点出错的字词，句子全对就选「没错」。","Tap the wrong word, or choose No mistake.");
            T("Sentence",35,105,830,70,XgUi.T(promptZh,promptEn),31);
            var zh=new string[Zh[n].Length+1];var en=new string[zh.Length];Array.Copy(Zh[n],zh,Zh[n].Length);Array.Copy(En[n],en,En[n].Length);
            zh[zh.Length-1]="没错";en[en.Length-1]="No mistake";
            Choices(zh,en,Bad[n]<0?zh.Length-1:Bad[n],225,Fix[n],FixEn[n]);
            T("CorrectionMeaning",35,405,830,25,XgUi.T("只判断这句写得对不对。","Judge the written sentence itself."),16);
        }
    }
    sealed class XgTranslationLesson : XgPackQuestion
    {
        // source, candidate, problem token index (-1 when no pinpoint is needed), grade (0 accurate/1 off/2 wrong)
        static readonly string[] Sources={"请明天上午九点给我打电话。","我把钥匙落在家里了。","这条路比那条路更短。","我已经在这里住了两年。","我对海鲜过敏。","下雨了，我们把窗户关上吧。"};
        static readonly string[] Candidates={"Please call me at nine tomorrow morning.","I forgot my keys at school.","This road is longer than that one.","I have lived here for two years.","I really enjoy eating seafood.","It is raining. Let us open the windows."};
        static readonly int[] Grades={0,1,1,0,2,2},WrongWords={-1,5,3,-1,-1,-1};
        int question;bool pinpoint;
        public XgTranslationLesson(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            question=C.Random.Next(Sources.Length);pinpoint=false;promptZh=Sources[question]+"\n"+Candidates[question];promptEn=promptZh;
            if(C.Sim.TeachingAiReady){int guess=Guess(Grades[question],3);aiText=C.Skill<.55?XgUi.T("……不知道","…not sure"):GradeText(guess);CapturePrediction(C.Skill<.55?(bool?)null:guess==Grades[question]);}
            Paint();
        }
        string GradeText(int n)=>n==0?XgUi.T("准","Accurate"):n==1?XgUi.T("有点歪","Slightly off"):XgUi.T("错了","Wrong");
        void Paint()
        {
            Header("翻译打分","Score a translation","准 / 有点歪 / 错了；有点歪时点出问题词。","Accurate / slightly off / wrong; pinpoint the weak word when slightly off.");
            T("Translation",35,105,830,110,promptZh,26);ShowAi(35,75,830);
            if(!pinpoint){for(int i=0;i<3;i++){int grade=i;B("Grade"+i,45+i*280,270,260,65,GradeText(i),()=>ChooseGrade(grade),26);}}
            else
            {
                string[] words=Candidates[question].Split(' ');for(int i=0;i<words.Length;i++){int at=i;B("Word"+i,35+(i%5)*170,250+(i/5)*58,162,50,words[i],()=>{chosenText+=" · "+words[at];Submit(at==WrongWords[question],"需要改的是 "+words[WrongWords[question]],"The weak word is "+words[WrongWords[question]]);},21);}
            }
        }
        void ChooseGrade(int grade)
        {
            if(submitted)return;chosenText=GradeText(grade);
            if(grade==1&&Grades[question]==1){pinpoint=true;Paint();return;}
            Submit(grade==Grades[question],"这一题："+GradeText(Grades[question]),"Assessment: "+GradeText(Grades[question]));
        }
    }
    sealed class XgBetterAnswerLesson : XgPackQuestion
    {
        static readonly string[] Q={"我把电脑电源拔了，文件还会在吗？","水烧开以后一直冒泡，是怎么回事？","帮我把‘请明天回复’说得礼貌一点。","明天有重要会议，今晚怎么安排？","我忘记一个单词，能先给你描述吗？"};
        static readonly string[] QEn={"Will my files remain after unplugging my PC?","Why does boiling water keep bubbling?","Make 'reply tomorrow' more polite.","I have an important meeting tomorrow. What should I do tonight?","Can I describe a word I cannot remember?"};
        static readonly string[] Good={"保存到硬盘的文件通常还在；没保存的修改可能丢失。","水受热汽化，产生的水蒸气形成气泡。","方便的话，能请你明天回复吗？谢谢。","先准备资料、设好闹钟，再留出足够睡眠。","可以。告诉我它的意思、用途或你记得的片段。"};
        static readonly string[] GoodEn={"Files saved to disk usually remain; unsaved changes may be lost.","Heat turns water into vapor, forming bubbles.","Could you please reply tomorrow if convenient? Thank you.","Prepare your materials and alarm, then allow enough sleep.","Yes. Describe its meaning, use, or any part you remember."};
        static readonly string[] Bad={"拔电源会把所有硬盘文件立刻清空。","因为水里一直有鱼在呼吸。","你明天必须回复，不许拖延！","不需要准备，熬夜玩到早上就好。","不行，必须先知道完整拼写才能问。"};
        static readonly string[] BadEn={"Unplugging instantly erases all files on every drive.","Fish in the water keep breathing.","You must reply tomorrow. No excuses!","Skip preparation and play games until morning.","No. You must know the full spelling before asking."};
        public XgBetterAnswerLesson(XgMiniContext c):base(c){}
        protected override void DrawQuestion()
        {
            int i=C.Random.Next(Q.Length);promptZh=Q[i];promptEn=QEn[i];Header("挑好答案","Choose a better answer","挑更准确、有帮助的回答。","Choose the more accurate, helpful reply.");
            T("Question",35,105,830,100,XgUi.T(promptZh,promptEn),28);Choices(new[]{Good[i],Bad[i]},new[]{GoodEn[i],BadEn[i]},0,275);
        }
    }
}
