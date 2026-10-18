#if UNITY_EDITOR
using System;
using System.Reflection;
using LingGuangV05.XingGuang;
using UnityEngine;
public static class TeachingVerdictChecks
{
    sealed class Host:IXgHost
    {
        public double Money{get;private set;}=100000;
        public double Compute=>1;public double VramMB=>2048;public string Blocker=>null;
        public bool Spend(double value){if(value>Money)return false;Money-=value;return true;}
        public void Earn(double value){Money+=value;}public void Train(double seconds){}
    }
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    public static void Run()
    {
        var sim=new XgSim();var host=new Host();sim.GoldChance=0;
        int tiers=0;sim.ComboTier+=_=>tiers++;
        for(int i=0;i<120;i++)
        {
            var card=sim.Card(XgMemes.SenseDesk);
            var answer=sim.Answer(XgMemes.SenseDesk,card.truth,host);
            Check(answer.correct,"Correct player truth accepted on card "+i);
            Check(sim.S.combo>=i+1,"Player streak independent of AI prediction on card "+i);
        }
        Check(tiers>=4&&sim.S.combo>=120,"Streak crosses all tiers and exceeds 100");
        var current=sim.Card(XgMemes.SenseDesk);current.kind="sense";current.family=false;current.gold=true;current.timeLimit=3;current.age=2;
        sim.S.desk=XgMemes.SenseDesk;long id=current.id;int wrong=sim.S.handWrong,combo=sim.S.combo;
        typeof(XgSim).GetMethod("TickCard",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(sim,new object[]{10d});
        Check(sim.Card(XgMemes.SenseDesk).id==id&&sim.S.handWrong==wrong&&sim.S.combo==combo,"Expired timer does not replace question, mark wrong, or break streak");
        var late=sim.Answer(XgMemes.SenseDesk,current.truth,host);
        Check(late.correct&&!late.timeout&&!late.gold&&late.pay>0&&sim.S.combo>combo,"Slow correct answer retains normal pay and streak, without quick bonus");
        current=sim.Card(XgMemes.SenseDesk);current.family=false;current.kind="sense";current.gold=true;current.timeLimit=3;current.age=1;
        var quick=sim.Answer(XgMemes.SenseDesk,current.truth,host);
        Check(quick.correct&&quick.gold,"Quick correct answer retains gold bonus");
        current=sim.Card(XgMemes.SenseDesk);current.family=false;current.kind="sense";
        var incorrect=sim.Answer(XgMemes.SenseDesk,!current.truth,host);
        Check(!incorrect.correct&&sim.S.combo==0,"Wrong player truth breaks streak");
        Debug.Log("TEACHING_VERDICT_PASS: 120 correct answers, independent streak, timer preserves card, quick-only gold, actual wrong breaks streak.");
    }
}
#endif
