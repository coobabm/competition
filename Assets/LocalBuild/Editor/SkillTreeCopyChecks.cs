using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

/// <summary>In-memory copy contracts only. Does not open scenes, start play mode or load player saves.</summary>
public static class SkillTreeCopyChecks
{
    [Serializable]
    sealed class Report
    {
        public string completedUtc, failure;
        public List<string> passed = new List<string>();
    }

    sealed class CopyHost : IXgHost
    {
        public double Money=>100000;public double Compute=>1;public double VramMB=>16000;public string Blocker=>null;
        public bool Spend(double value)=>value>=0&&value<=Money;public void Earn(double value){}public void Train(double seconds){}
    }
    static Report report;
    static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        report.passed.Add(message);
    }
    static bool ContainsHan(string text)
    {
        foreach (char c in text) if (c >= '\u3400' && c <= '\u9fff') return true;
        return false;
    }
    static void ReadOnly(XgSim sim, Action read, string context)
    {
        string before = JsonUtility.ToJson(sim.S);
        read();
        Check(before == JsonUtility.ToJson(sim.S), context + ": presentation leaves state unchanged");
    }

    [MenuItem("Tools/Design Pass/Skill Tree Copy Checks")]
    public static void Run()
    {
        report = new Report();
        try
        {
            foreach (bool english in new[] { false, true })
            {
                MysteryAndCoverage(english);
                PayAndAutomaticLabelling(english);
                Downloads(english);
                Chains(english);
                IntegratedResearch(english);
            }
            TooltipGeometry();
        }
        catch (Exception error) { report.failure = error.ToString(); throw; }
        finally
        {
            report.completedUtc = DateTime.UtcNow.ToString("o");
            const string folder = "LocalVerification/DesignPass/SkillTreeCopy";
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "checks.json"), JsonUtility.ToJson(report, true));
        }
        Debug.Log("SKILL_TREE_COPY_CHECKS_PASS " + report.passed.Count + "; scene and screenshot acceptance still required");
    }

    // Exercises the production helper through reflection because it belongs to the desktop assembly.
    // This proves bounds arithmetic only, not TMP layout or EventSystem interaction.
    static void TooltipGeometry()
    {
        var assembly = typeof(LingGuangV05.Desktop.XingGuang.XgTechTreePage).Assembly;
        var geometry = assembly.GetType("LingGuangV05.Desktop.XingGuang.XgTechTooltipGeometry", true);
        var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
        var width = geometry.GetMethod("Width", flags);
        var place = geometry.GetMethod("Place", flags);
        Check(width != null && place != null, "Tooltip production geometry is available");
        foreach (float available in new[] { 160f, 240f, 312f, 640f })
        foreach (float height in new[] { 240f, 320f, 720f })
        foreach (float node in new[] { -1000f, 0f, 200f, 1000f })
        {
            float w = (float)width.Invoke(null, new object[] { available, 312f });
            float h = Math.Min(560, height - 8);
            object[] args = { 0f, 0f, available, height, w, h, node, node, node - 56, 0f, 0f };
            place.Invoke(null, args);
            float x = (float)args[9], y = (float)args[10];
            Check(w <= available - 8 && x - w / 2 >= 3.999f && x + w / 2 <= available - 3.999f
                && y - h >= 3.999f && y <= height - 3.999f, "Tooltip fits visible bounds: " + available + "x" + height + " node " + node);
        }
    }

    static void MysteryAndCoverage(bool english)
    {
        var sim = new XgSim { English = english };
        var hidden = XgCatalog.Node("label.auto");
        Check(sim.NodeMystery(hidden), "Fresh automatic labelling remains a mystery");
        Check(XgTechTreeCopy.Name(hidden, sim) == sim.NodeName(hidden), "Hidden name never reveals a discovery");
        Check(!XgTechTreeCopy.Card(hidden, sim).Contains("cards/s"), "Hidden card does not expose rates");
        Check(!string.IsNullOrWhiteSpace(XgTechTreeCopy.Card(null, sim)), "Null card fallback is safe");
        sim.S.autoLabelRevealed = true;
        ReadOnly(sim, () => {
            int covered=0, atlas=0;
            foreach(var node in XgCatalog.Nodes)
            {
                if(sim.NodeMystery(node)) continue;
                string name=XgTechTreeCopy.Name(node,sim), summary=XgTechTreeCopy.Summary(node,sim), effect=XgTechTreeCopy.Effect(node,sim), detail=XgTechTreeCopy.Detail(node,sim);
                Check(XgTechTreeCopy.Covered(node), "Authored copy covers " + node.id);
                Check(XgTechTreeCopy.Card(node,sim).Split('\n').Length==3, "Exactly three card lines " + node.id);
                Check(!string.IsNullOrWhiteSpace(name)&&name!=node.id, "Readable name " + node.id);
                Check(!string.IsNullOrWhiteSpace(summary)&&effect.StartsWith("▶ "), "Summary and definite effect " + node.id);
                Check(english ? !ContainsHan(name+summary+effect+detail) : ContainsHan(summary), "Localized copy " + node.id);
                Check(english || (summary.Length<=12 && effect.Substring(2).Length<=14), "Chinese card length budget " + node.id);
                foreach(string banned in new[]{"不保证","需训练后评估","与其他倍数相乘","下载进度","batches, the first","not guaranteed"})
                    Check(!(name+summary+effect).Contains(banned), "No obsolete caveat on card " + node.id + "/" + banned);
                int sentences=System.Text.RegularExpressions.Regex.Matches(detail, english ? @"[.!?](?:\s|$)" : "[。！？]").Count;
                Check(sentences<=3, "Details at most three sentences " + node.id);
                var entry=XgTechTree.EntryOf(node.id);Check(entry!=null,"Catalog entry " + node.id);
                if(XgSim.IsAtlas(node)) atlas++;
                else { Check(System.Text.RegularExpressions.Regex.IsMatch(name,@"[A-Za-z].*\d{4}"), "Abbreviation and year " + node.id); covered++; }
            }
            Check(covered>0&&atlas>0,"Ordinary and atlas coverage");
        }, "Complete three-line catalog");
    }

    static void PayAndAutomaticLabelling(bool english)
    {
        var sim=new XgSim { English=english };sim.S.autoLabelRevealed=true;
        var pay=XgCatalog.Node("label.raise");
        foreach(int level in new[]{0,1,XgCatalog.RaiseMax})
        {
            sim.S.payRaise=level;
            ReadOnly(sim,()=>{
                Check(!string.IsNullOrWhiteSpace(XgTechTreeCopy.Effect(pay,sim)),"Pay effect remains readable at level "+level);
                Check((XgTechTree.Next(XgTechTree.EntryOf(pay.id),sim)==null)==(level==XgCatalog.RaiseMax),"Pay purchase stops at maximum "+level);
                Check(Math.Abs(sim.ManualPayFor("spam",1)/sim.PayFor("spam",1)-XgCatalog.RaiseMultipliers[level]*XgCatalog.HandPayScale)<1e-9,"Actual manual reward uses the configured multiplier "+level);
                Check(sim.PayFor("spam",1)==XgCatalog.LabelPay*(XgCatalog.Desk("spam").pay),"Automatic base reward ignores manual raises "+level);
            },"Pay fixture "+level);
        }
        Check(XgCatalog.AutoRatePerLevel>0&&XgCatalog.AutoMinAccuracy>0&&XgCatalog.AutoMinAccuracy<=1,"Automatic labelling retains a valid rate and qualification threshold");
        var host=new CopyHost();sim.S.autoLevel=2;sim.S.best.Add(new XgBest { dataset="spam",acc=XgCatalog.AutoMinAccuracy-.001,arch="perceptron" });
        Check(!sim.EligibleDesk("spam")&&sim.CollaborationRate("spam",host)==0,"Below-threshold checkpoint never auto-labels");
        sim.S.best[sim.S.best.Count-1].acc=XgCatalog.AutoMinAccuracy;
        Check(sim.EligibleDesk("spam")&&Math.Abs(sim.CollaborationRate("spam",host)-2*XgCatalog.AutoRatePerLevel*sim.WireSpeed("label:spam"))<1e-9,"Per-desk actual rate uses catalog level and threshold");
        sim.S.autoPaused=true;Check(sim.CollaborationRate("spam",host)==0,"Paused automation produces no labels");
    }

    // Review the two late deliveries against the running simulation, not their old patches.
    static void IntegratedResearch(bool english)
    {
        var sim = new XgSim { English = english };
        var relu = XgCatalog.Node("relu");
        var augment = XgCatalog.Node("augment");
        ReadOnly(sim, () =>
        {
            string reluCopy = XgTechTreeCopy.Note(relu, sim);
            Check(XgTechTreeCopy.Name(relu,sim).Contains("ReLU"),"ReLU keeps its technical abbreviation");
            Check(!XgTechTreeCopy.Detail(augment,sim).Contains(english ? "crop" : "裁剪"),"Augmentation does not promise an unimplemented crop");
            Check(XgTechTreeCopy.Detail(augment,sim).Contains(english ? "same answers" : "答案保持不变"),"Augmentation explains label preservation");
        }, "Late research copy");
        var run = new XgRun { arch = "perceptron", dataset = "spam", depth = 3, width = 0 };
        double speed = sim.ProgressionSpeed(run);
        sim.S.unlocked.Add("relu");
        Check(Math.Abs(sim.ProgressionSpeed(run) / speed - 1.3) < 1e-9 && sim.BestActivation == 2, "Owned ReLU supplies the stated throughput and preferred activation");
        var knobs = new XgKnobs { activation = XgActivation.Relu };
        Check(Math.Abs(knobs.G - .98) < 1e-9 && Math.Abs(knobs.RelayKeep - .95) < 1e-9, "ReLU base backward and forward retention match displayed percentages");
        knobs.batchNorm = true;
        Check(knobs.G > .98 && knobs.RelayKeep > .95, "ReLU retention is explicitly a base rate, modifiable by normalization");
        foreach (string dataset in new[] { "mnist", "go", "cifar", "imagenet", "meme", "spam", "translate" })
        {
            sim.S.unlocked.Remove("augment"); run.dataset = dataset;
            double samples = sim.Samples(dataset), effective = sim.SamplesEffective(run);
            int pool = sim.PoolSize(run);
            Check(sim.AugmentFactor(dataset) == 1, "Unowned augmentation has no effect for " + dataset);
            sim.S.unlocked.Add("augment");
            int factor = dataset == "mnist" ? 2 : dataset == "go" ? 8 : dataset == "cifar" || dataset == "imagenet" || dataset == "meme" ? 3 : 1;
            Check(sim.AugmentFactor(dataset) == factor && sim.PoolSize(run) == pool * factor
                && Math.Abs(sim.SamplesEffective(run) - effective * factor) < 1e-6 && sim.Samples(dataset) == samples,
                "Augmentation changes the pool and effective count, preserving collected samples: " + dataset);
        }
        var card = new XgBoardCard { truth = true }.Add("point", 1, 2).Add("word");
        var shifted = XgBoardData.Augment(card, "mnist", 1);
        Check(shifted.truth == card.truth && shifted.features.Count == card.features.Count && shifted.features[0].x == 2
            && shifted.features[0].y == 2 && shifted.features[1].x == card.features[1].x, "Digit augmentation shifts without cropping or changing labels/non-position clues");
    }

    static void Downloads(bool english)
    {
        var sim=new XgSim { English=english };
        ReadOnly(sim,()=>{
            int count=0;
            foreach(var node in XgCatalog.Nodes)
            {
                if(node.kind!=XgNodeKind.Dataset)continue;
                string copy=XgTechTreeCopy.Card(node,sim)+XgTechTreeCopy.Detail(node,sim);
                Check(!copy.Contains("ADSL")&&!copy.Contains("批下载")&&!copy.Contains("download batches"),"Dataset has no installation/download UI promise "+node.id);
                Check(XgCatalog.Dataset(node.target).samples>0,"Dataset retains its real sample budget "+node.id);count++;
            }
            Check(count>0,"Dataset coverage nonempty");
        },"Immediate dataset copy");
        foreach(var pack in XgTeachingPacks.All)
        {
            if(!pack.buy)continue;
            // Isolated progressed fixture: tests this purchase, not prerequisite progression.
            var buyer=new XgSim();buyer.S.stage=6;buyer.S.firstSpecialty="vision";
            buyer.S.unlocked.AddRange(new[]{"train.curve","rnn","lstm","conv"});
            Check(!buyer.TeachingPackAvailable(pack.id),"Paid desk is unavailable before purchase "+pack.id);
            Check(buyer.BuyNode(pack.NodeId,new CopyHost()),"Purchase actual dataset node "+pack.id+": "+buyer.Why(XgCatalog.Node(pack.NodeId),new CopyHost()));
            Check(buyer.TeachingPackAvailable(pack.id)&&buyer.SelectTeachingPack(pack.id),"Bought dataset is immediately selectable in Teach "+pack.id);
            Check(!buyer.S.downloadQueue.Exists(x=>x.dataset==pack.dataset),"Purchase creates no pending download "+pack.id);
        }
    }

    static void Chains(bool english)
    {
        int chains = 0;
        foreach (var entry in XgTechTree.Entries)
        {
            if (!entry.IsChain) continue;
            chains++;
            var sim = new XgSim { English = english };
            foreach (var member in entry.members) sim.S.unlocked.Remove(member.id);
            for (int owned = 0; owned <= entry.members.Count; owned++)
            {
                int level = owned;
                ReadOnly(sim, () =>
                {
                    bool maxed = level == entry.members.Count;
                    var shown = entry.members[maxed ? level - 1 : level];
                    Check(XgTechTree.Next(entry, sim) == (maxed ? null : shown), "Chain " + entry.id + " level " + level + ": next member");
                    Check(XgTechTree.Level(entry, sim) == level && XgTechTree.MaxLevel(entry) == entry.members.Count, "Chain " + entry.id + " level " + level + ": level display");
                    string name = XgTechTree.Name(entry, sim);
                    string term = entry.First.kind == XgNodeKind.Depth ? (english ? "network depth" : "网络深度") : entry.First.kind == XgNodeKind.Width ? (english ? "network width" : "网络宽度") : (english ? "Training automation" : "训练自动化");
                    Check(name.Contains(term), "Chain " + entry.id + " level " + level + ": professional chain term");
                    if (entry.First.kind == XgNodeKind.Depth || entry.First.kind == XgNodeKind.Width)
                    {
                        Check(name.Contains(english ? "Vision" : "视觉") == (shown.tree == "vision"), "Chain " + entry.id + " level " + level + ": vision scope");
                        Check(name.Contains(english ? "Sequence" : "序列") == (shown.tree == "sequence"), "Chain " + entry.id + " level " + level + ": sequence scope");
                    }
                    Check(XgTechTreeCopy.Effect(shown, sim).StartsWith("▶ "), "Chain " + entry.id + " level " + level + ": displayed member has effect");
                    if (maxed)
                        Check(XgTechTree.Status(entry, sim, null) == XgSim.NodeStatus.Owned && double.IsPositiveInfinity(XgTechTree.Cost(entry, sim)), "Chain " + entry.id + ": max is owned with no further price");
                }, "Chain " + entry.id + " level " + level);
                if (owned < entry.members.Count) sim.S.unlocked.Add(entry.members[owned].id);
            }
        }
        Check(chains > 0, "Chain fixture exercises catalog ladders");
    }
}
