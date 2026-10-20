using System;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

/// <summary>Every catalog node gets a purchase result or a concrete remaining gate. No player saves or scenes.</summary>
public static class TechnologyCatalogCoverageChecks
{
    [Serializable] public sealed class Row
    {
        public string id, entry, finalStatus, blocker;
        public int purchases, inspectedStages;
        public double lastQuotedPrice;
    }
    [Serializable] sealed class Report
    {
        public string completedUtc, failure;
        public int catalogCount, purchaseTransactions;
        public List<Row> nodes=new List<Row>();
    }
    sealed class Host : IXgHost
    {
        public double money=1e12;
        public double Money=>money; public double Compute=>1; public double VramMB=>16000; public string Blocker=>null;
        public bool Spend(double amount){if(double.IsNaN(amount)||double.IsInfinity(amount)||amount<0||money<amount)return false;money-=amount;return true;}
        public void Earn(double amount){money+=amount;} public void Train(double seconds){}
    }
    static void Require(bool value,string context){if(!value)throw new Exception(context);}

    [MenuItem("Tools/Design Pass/Technology Catalog Coverage")]
    public static void Run()
    {
        var report=new Report();
        try
        {
            var sim=new XgSim();var host=new Host();
            var rows=new Dictionary<string,Row>(StringComparer.Ordinal);
            foreach(var node in XgCatalog.Nodes)
            {
                Require(node!=null&&!string.IsNullOrEmpty(node.id)&&!rows.ContainsKey(node.id),"Invalid or duplicate catalog ID");
                var entry=XgTechTree.EntryOf(node.id);
                Require(entry!=null&&entry.Covers(node.id),"No UI entry covers "+node.id);
                var row=new Row{id=node.id,entry=entry.id};rows.Add(node.id,row);report.nodes.Add(row);
            }
            report.catalogCount=report.nodes.Count;
            Require(report.catalogCount>0,"Catalog must not be empty");
            for(int stage=1;stage<=6;stage++)
            {
                // Stage is the only progressed fixture axis. Activity, chat, accuracy and ownership gates remain real.
                sim.S.stage=stage;
                bool progressed;int pass=0;
                do
                {
                    progressed=false;
                    Require(++pass<report.catalogCount*20+100,"Purchase traversal did not converge");
                    foreach(var node in XgCatalog.Nodes)
                    {
                        var row=rows[node.id];
                        var status=sim.Status(node,host);
                        if(status!=XgSim.NodeStatus.Buyable)continue;
                        var entry=XgTechTree.EntryOf(node.id);
                        var next=XgTechTree.Next(entry,sim);
                        Require(next!=null&&next.id==node.id,"Buyable node is not its UI entry's next purchase: "+node.id);
                        Require(XgTechTree.Status(entry,sim,host)==status,"UI status differs from simulation: "+node.id);
                        double price=sim.NodeCost(node), money=host.money, spent=sim.S.totalSpent;
                        Require(Math.Abs(XgTechTree.Cost(entry,sim)-price)<1e-6,"UI price differs from transaction: "+node.id);
                        Require(XgTechTree.Buy(entry,sim,host),"Displayed Buyable purchase failed: "+node.id+" / "+sim.Why(node,host));
                        Require(Math.Abs((money-host.money)-price)<.001&&Math.Abs((sim.S.totalSpent-spent)-price)<.001,"Purchase accounting mismatch: "+node.id);
                        row.purchases++;row.lastQuotedPrice=price;report.purchaseTransactions++;progressed=true;
                    }
                }while(progressed);
                foreach(var node in XgCatalog.Nodes)
                {
                    var row=rows[node.id];row.inspectedStages++;
                    var status=sim.Status(node,host);row.finalStatus=status.ToString();row.blocker=sim.Why(node,host);
                    Require(status!=XgSim.NodeStatus.Buyable,"Traversal left a buyable node untested: "+node.id);
                    Require(!string.IsNullOrWhiteSpace(row.blocker),"Missing gate or ownership explanation: "+node.id);
                    double money=host.money,spent=sim.S.totalSpent;
                    Require(!sim.BuyNode(node.id,host),"Owned or blocked node accepted a purchase: "+node.id);
                    Require(host.money==money&&sim.S.totalSpent==spent,"Rejected purchase charged money: "+node.id);
                }
            }
            foreach(var row in report.nodes)Require(row.inspectedStages==6,"Missing stage coverage: "+row.id);
            Require(report.nodes.Count==XgCatalog.Nodes.Length,"Catalog changed during coverage");
        }
        catch(Exception ex){report.failure=ex.ToString();throw;}
        finally
        {
            report.completedUtc=DateTime.UtcNow.ToString("o");
            const string folder="LocalVerification/DesignPass/TechnologyCatalogCoverage";
            Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"report.json"),JsonUtility.ToJson(report,true));
        }
        Debug.Log("TECH_CATALOG_COVERAGE_PASS nodes="+report.catalogCount+" transactions="+report.purchaseTransactions+"; blocked activity gates are listed individually, not bypassed");
    }
}
