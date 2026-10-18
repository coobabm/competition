#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

public static class TestSetPreparationChecks
{
    // Run cold vocabulary checks before the broad reference-loop suite warms static caches.
    public static void RunAll()
    {
        VocabularyPreparationChecks.Run();
        Run();
    }
    sealed class Host : IXgHost
    {
        public double Compute=>8;public double VramMB=>1000000;public double Money{get;private set;}=1000000;public double Trained;
        public string Blocker=>null;public bool Spend(double value){if(Money<value)return false;Money-=value;return true;}
        public void Earn(double value){Money+=value;}public void Train(double seconds){Trained+=seconds;}
    }
    static int cases;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    [MenuItem("LingGuang/Verification/Test set preparation equivalence")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Use isolated EditMode verification");
        cases=0;
        CheckExistingTrainingSignatureCache();
        foreach(var dataset in XgCatalog.Datasets)
        {
            var expected=Reference(dataset.id,1,20161018,37,XgBoardData.Use.Diagnostic);
            foreach(int chunk in new[]{1,7,32,4096})Compare(expected,Build(dataset.id,1,20161018,37,XgBoardData.Use.Diagnostic,chunk),dataset.id+" chunk "+chunk);
        }
        foreach(string dataset in new[]{"mnist","logic","meme","translate"})
            foreach(int level in new[]{2,4})foreach(int salt in new[]{0,913})foreach(var use in new[]{XgBoardData.Use.Diagnostic,XgBoardData.Use.Exam})
            {
                var expected=Reference(dataset,level,20161101,salt,use);
                Compare(expected,Build(dataset,level,20161101,salt,use,7),dataset+" advanced/date/salt/use "+use);
            }
        var sim=new XgSim();sim.Today=20161018;string state=JsonUtility.ToJson(sim.S);
        Finish(sim,"mnist");Check(JsonUtility.ToJson(sim.S)==state,"Preparation changed simulation save/RNG/board/economy state");
        var pending=(XgTestSetPreparation)typeof(XgSim).GetField("pendingTestSet",Private).GetValue(sim);
        var ready=pending.Result;
        var cache=(Dictionary<string,List<XgBoardCard>>)typeof(XgSim).GetField("testSets",Private).GetValue(sim);
        Check(cache.Count==0,"Completed preparation prematurely entered live testSets cache");
        var taken=sim.TestSet("mnist");Check(ReferenceEquals(taken,ready),"Matching real request did not consume complete pending set");
        Compare(Reference("mnist",sim.BoardLevel("mnist"),0,sim.S.dataSalt,XgBoardData.Use.Diagnostic),taken,"real non-topical consume");
        Check(ReferenceEquals(taken,sim.TestSet("mnist")),"Existing cached reference changed");
        // An earlier day in the same month must not choose the first-use test content.
        sim=new XgSim();sim.Today=20161018;Finish(sim,"meme");sim.Today=20161019;
        Compare(Reference("meme",sim.BoardLevel("meme"),20161019,sim.S.dataSalt,XgBoardData.Use.Diagnostic),sim.TestSet("meme"),"same-month pending date mismatch");
        var monthly=sim.TestSet("meme");sim.Today=20161020;Check(ReferenceEquals(monthly,sim.TestSet("meme")),"Existing same-month cache must survive day change");
        sim.Today=20161101;Compare(Reference("meme",sim.BoardLevel("meme"),20161101,sim.S.dataSalt,XgBoardData.Use.Diagnostic),sim.TestSet("meme"),"month change");
        foreach(int progress in new[]{1,4095,4096,4200,5500})
        {
            sim=new XgSim();sim.Today=20161018;sim.PrepareTestSet("logic",progress);sim.CancelTestSetPreparation();
            Compare(Reference("logic",sim.BoardLevel("logic"),0,sim.S.dataSalt,XgBoardData.Use.Diagnostic),sim.TestSet("logic"),"cancel phase "+progress);
        }
        sim=new XgSim();sim.Today=20161018;sim.PrepareTestSet("meme",37);
        Compare(Reference("meme",sim.BoardLevel("meme"),20161018,sim.S.dataSalt,XgBoardData.Use.Diagnostic),sim.TestSet("meme"),"synchronous request during preparation");
        sim=new XgSim();sim.Today=20161018;Finish(sim,"logic");sim.S.dataSalt++;
        Compare(Reference("logic",sim.BoardLevel("logic"),0,sim.S.dataSalt,XgBoardData.Use.Diagnostic),sim.TestSet("logic"),"salt invalidation");
        sim=new XgSim();sim.Today=20161018;Finish(sim,"logic");int oldLevel=sim.BoardLevel("logic");sim.S.labels.RemoveAll(x=>x.dataset=="logic");sim.S.labels.Add(new XgLabelCount{dataset="logic",count=100000});
        Check(sim.BoardLevel("logic")>oldLevel,"Level-change fixture failed");
        Compare(Reference("logic",sim.BoardLevel("logic"),0,sim.S.dataSalt,XgBoardData.Use.Diagnostic),sim.TestSet("logic"),"level invalidation");
        var baseline=new XgSim();baseline.Today=20161018;baseline.S.teachingFlow=false;baseline.S.dataSalt=37;
        baseline.S.labels.RemoveAll(x=>x.dataset=="mnist");baseline.S.labels.Add(new XgLabelCount{dataset="mnist",count=200});baseline.SetDataset(XgTrack.Vision,"mnist");
        var warmed=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(baseline.S)));warmed.Today=20161018;
        var hostA=new Host();var hostB=new Host();Finish(warmed,"mnist");
        for(int epoch=0;epoch<5;epoch++)
        {
            var a=baseline.TrainEpoch(XgTrack.Vision,hostA);var b=warmed.TrainEpoch(XgTrack.Vision,hostB);
            Check(a!=null&&b!=null,"Deterministic training fixture could not train");
            Check(JsonUtility.ToJson(baseline.S)==JsonUtility.ToJson(warmed.S),"Preparation changed resulting trained state at epoch "+epoch);
            Check(hostA.Money==hostB.Money&&hostA.Trained==hostB.Trained,"Preparation changed earnings/billing at epoch "+epoch);
        }
        var other=new XgSim();other.Today=20161018;
        Check(typeof(XgSim).GetField("pendingTestSet",Private).GetValue(other)==null,"Pending preparation leaked to another save owner");
        string dir=Path.Combine(Path.GetTempPath(),"LingGuang-TestSetEquivalence-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir,"passed.txt"),"PASS "+cases+" assertions. Exact ordered generated content and unchanged preparation state; not a frame-time or audible/gameplay acceptance result.");
        Debug.Log("TESTSET_PREPARATION_PASS "+cases+": "+dir);
    }
    static void Finish(XgSim sim,string dataset){int guard=0;while(!sim.PrepareTestSet(dataset,32)){if(++guard>1000)throw new Exception("Preparation never finished");}}
    static List<XgBoardCard> Build(string dataset,int level,int today,int salt,XgBoardData.Use use,int chunk)
    {
        var job=new XgTestSetPreparation(dataset,level,today,salt,use);Check(job.Result==null,"Partial result exposed");int guard=0;
        while(!job.Complete){job.Advance(chunk);if(++guard>10000)throw new Exception("Builder never finished");}
        Check(job.TrainingGenerated==(job.ReusedTrainingSignatures?0:4096),"Did not generate or reuse every training signature index");Check(job.CandidatesGenerated<=1600,"Candidate limit changed");return job.Result;
    }
    static void CheckExistingTrainingSignatureCache()
    {
        var originalCulture=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=(CultureInfo)CultureInfo.InvariantCulture.Clone();
            const int salt=7593;
            var expected=XgBoardData.TestSet("mnist",1,0,salt,XgBoardData.Use.Diagnostic);
            var job=new XgTestSetPreparation("mnist",1,0,salt);
            Check(job.ReusedTrainingSignatures,"Existing real training-signature cache was not reused");
            job.Advance(10000);
            Check(job.Complete&&job.TrainingGenerated==0,"Cached signatures were regenerated");
            Compare(expected,job.Result,"actual cached TrainSignatures same culture");
            var changed=(CultureInfo)CultureInfo.InvariantCulture.Clone();
            changed.NumberFormat.NegativeSign="~";
            CultureInfo.CurrentCulture=changed;
            // Same CultureInfo.Name, different integer signature formatting. The real static cache remains authoritative.
            expected=XgBoardData.TestSet("mnist",1,0,salt,XgBoardData.Use.Diagnostic);
            job=new XgTestSetPreparation("mnist",1,0,salt);
            Check(job.ReusedTrainingSignatures,"Culture clone bypassed existing signature cache");
            job.Advance(10000);
            Check(job.Complete&&job.TrainingGenerated==0,"Culture clone regenerated cached signatures");
            Compare(expected,job.Result,"actual cached TrainSignatures custom NegativeSign");
        }
        finally { CultureInfo.CurrentCulture=originalCulture; }
    }
    // Independent literal reference loops retained in verification code, NOT delegated to the new builder.
    static List<XgBoardCard> Reference(string dataset,int level,int today,int salt,XgBoardData.Use use)
    {
        var train=new HashSet<string>();for(int i=0;i<4096;i++)train.Add(XgBoardData.Signature(XgBoardData.Make(dataset,XgBoardData.Seed(dataset,i,XgBoardData.Use.Train,salt),level,today)));
        var result=new List<XgBoardCard>();var spare=new List<XgBoardCard>();
        for(int i=0;result.Count<400&&i<1600;i++)
        {
            var card=XgBoardData.Make(dataset,XgBoardData.Seed(dataset,i,use,salt),level,today);
            if(train.Contains(XgBoardData.Signature(card))){if(spare.Count<400)spare.Add(card);}else result.Add(card);
        }
        for(int i=0;result.Count<400&&i<spare.Count;i++)result.Add(spare[i]);return result;
    }
    static string Card(XgBoardCard card)
    {
        var b=new StringBuilder();b.Append(card.region).Append('|').Append(card.truth).Append('|').Append(card.seed).Append('|').Append(card.distance).Append('|').Append(card.text);
        foreach(var f in card.features)b.Append("\n").Append(f.name).Append('|').Append(f.x).Append('|').Append(f.y).Append('|').Append(f.seq).Append('|').Append(f.made);
        b.Append("\nSOURCE:").Append(card.source==null?"null":JsonUtility.ToJson(card.source));return b.ToString();
    }
    static void Compare(List<XgBoardCard> expected,List<XgBoardCard> actual,string label)
    {
        Check(expected.Count==actual.Count&&actual.Count==400,label+" count mismatch");
        for(int i=0;i<expected.Count;i++)Check(Card(expected[i])==Card(actual[i]),label+" ordered card mismatch at "+i);
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);cases++;}
}
#endif
