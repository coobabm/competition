using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.YY;
using LingGuangV05.XingGuang;
using UnityEngine;

public static class LatestRequestChecks
{
    public static void Run()
    {
        int count=0;
        void Check(bool yes,string message) { if(!yes) throw new Exception("LATEST REQUEST: "+message); count++; }
        var sim=new ChapterOneSim(); sim.RentWaived=()=>true;
        Check(new XgSim().SystemUnlocked("power"),"Power enabled at stage one");
        double before=sim.S.money, quote=sim.TrainingRoundPrice(12);
        Check(quote>0,"Opening round has a non-zero quote");
        double charged=sim.BillTraining(12);
        Check(Math.Abs(quote-charged)<1e-8 && Math.Abs(before-sim.S.money-charged)<1e-8,"Opening training charges its quoted price");
        sim.S.energyKwh=2;
        typeof(ChapterOneSim).GetMethod("SettleDay",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(sim,null);
        Check(sim.S.bills.idlePower>0 && sim.S.bills.rent==0,"Rent holiday never waives electricity");
        Check(Math.Abs(sim.S.fundsExpense-(before-sim.S.money))<1e-8,"Money ledger reconciles bills exactly");
        double spent=sim.S.fundsExpense; sim.RecordFunds("Again"); sim.RecordFunds("Again");
        Check(sim.S.fundsExpense==spent,"Repeated reconciliation never double charges");
        var loaded=new ChapterOneSim(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(sim.S)));
        Check(loaded.S.funds.Count==sim.S.funds.Count && loaded.S.fundsExpense==spent,"Funds survive load without duplicate entries");
        sim.S.money=0;sim.RecordFunds("Purchase fixture");sim.S.energyKwh=2;
        typeof(ChapterOneSim).GetMethod("SettleDay",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(sim,null);
        Check(sim.S.unpaidPower && sim.S.billDue>0,"Opening electricity cannot disappear when money runs out");
        sim.S.money=50;sim.RecordFunds("Work fixture");Check(sim.PayBill() && !sim.S.unpaidPower,"Earning and paying restores electricity");
        var lab=new XgSim();lab.S.stage=5;lab.S.bestCombo=12345;
        var privateState=typeof(LlmPersonas).GetMethod("State",BindingFlags.Static|BindingFlags.NonPublic);
        foreach(var id in new[]{"laozhou","oldfriend","cousin","landlord","netbar"}) {
            var state=(string)privateState.Invoke(null,new object[]{id,sim,lab});
            Check(!state.Contains("12345") && !state.Contains("第 5 阶段") && !state.Contains("样本"),"No private training facts for "+id);
            var conversation=new YYConversation{id=id};conversation.messages.Add(new YYMessage{from=YYChatHub.Me,text="你好"});
            string system=LlmPersonas.Conversation(id,conversation,sim,lab)[0].Value;
            Check(!system.Contains("是「灵光」") && system.Contains("不知道玩家电脑"),"Human persona remains human and unaware: "+id);
        }
        Check(!File.ReadAllText("Assets/LingGuangV05/Mail2016/MailInboxView.cs").Contains("from = \"老周\""),"Old Zhou mail removed");
        Directory.CreateDirectory("LocalVerification/ClaudeHandoff");
        File.WriteAllText("LocalVerification/ClaudeHandoff/latest-checks.txt","PASS "+count+" checks: opening power, funds reconciliation/load, debt recovery, human privacy, removed mail.");
        Debug.Log("LATEST_REQUEST_CHECKS_PASS "+count);
    }
}
