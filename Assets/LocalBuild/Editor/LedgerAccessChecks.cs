#if UNITY_EDITOR
using System;
using LingGuangV05.Core;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;
public static class LedgerAccessChecks
{
    sealed class Wallet:IXgHost
    {
        public double cash=100;public int payments;
        public double Compute=>1000;public double VramMB=>8192;public double Money=>cash;public string Blocker=>null;
        public bool Spend(double amount){if(amount<0||cash<amount)return false;cash-=amount;payments++;return true;}
        public void Earn(double amount){cash+=amount;}public void Train(double seconds){}
    }
    static void Check(bool ok,string name){if(!ok)throw new Exception("Ledger: "+name);}
    [MenuItem("Tools/Local Verification/Ledger access and billing")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Use isolated EditMode");
        var sim=new XgSim();sim.S.teachingFlow=true;sim.S.unlocked.Add("train.curve");
        var node=XgCatalog.Node(XgSim.LedgerNode);Check(node!=null,"real node exists");
        Check(!sim.LedgerOpen&&!sim.TrainPart("ledger"),"new game locked");
        Check(Math.Abs(sim.NodeCost(node)-15)<1e-9,"ordinary price is 15 without legacy quarter multiplier");
        var wallet=new Wallet{cash=14};Check(!sim.BuyNode(node.id,wallet)&&wallet.cash==14&&wallet.payments==0,"insufficient funds cannot buy");
        wallet.cash=100;Check(sim.BuyNode(node.id,wallet)&&wallet.cash==85&&wallet.payments==1&&sim.LedgerOpen,"real purchase unlocks and bills once");
        Check(!sim.BuyNode(node.id,wallet)&&wallet.cash==85&&wallet.payments==1,"repeat purchase rejected");
        var owned=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));Check(owned.LedgerOpen,"purchase survives save reload");
        var fresh=new XgSim();fresh.S.teachingFlow=true;
        var freshReload=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(fresh.S)));Check(!freshReload.LedgerOpen,"new unbought save does not migrate into ownership");
        foreach(bool teaching in new[]{false,true})
        {
            var old=new XgState{teachingFlow=teaching,ledgerAccessVersion=0};
            var migrated=new XgSim(old);Check(migrated.Has(node.id)&&migrated.LedgerOpen&&old.ledgerAccessVersion==1,"old header access retained "+teaching);
            var again=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(old)));
            Check(again.S.unlocked.FindAll(id=>id==node.id).Count==1,"migration idempotent "+teaching);
        }
        // The real household billing path remains independent from ledger ownership
        // and rent's introductory waiver. No fake reward or training-bill implementation.
        var house=new ChapterOneSim();house.PowerCostsEnabled=true;house.RentWaived=()=>true;
        double before=house.S.money,quote=house.TrainingRoundPrice(1),charged=house.BillTraining(1);
        Check(quote>0&&Math.Abs(charged-quote)<1e-9&&Math.Abs(before-house.S.money-charged)<1e-7,"training electricity bills during rent introduction");
        var settle=typeof(ChapterOneSim).GetMethod("SettleDay",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        house.S.energyKwh=1;house.S.billDue=0;before=house.S.money;
        settle.Invoke(house,null);
        Check(Math.Abs(before-house.S.money-house.Config.electricityPrice)<1e-7&&house.S.billDue==0&&!house.S.unpaidPower,"idle electricity settles while rent is waived");
        Check(house.Debt==0,"electricity does not start rent early");
        house.S.money=0;house.S.energyKwh=1;settle.Invoke(house,null);
        Check(house.S.unpaidPower&&house.S.billDue>0,"insufficient idle funds become arrears, not free power");
        Debug.Log("LedgerAccessChecks passed: isolated purchase, persistence, migration and real billing. UI not covered.");
    }
}
#endif
