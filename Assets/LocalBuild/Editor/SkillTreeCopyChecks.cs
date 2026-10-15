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
        string language = english ? "EN" : "ZH";
        var hidden = XgCatalog.Node("label.auto");
        Check(sim.NodeMystery(hidden), language + ": fresh automatic labelling starts as a mystery");
        ReadOnly(sim, () =>
        {
            Check(XgTechTreeCopy.Name(hidden, sim) == sim.NodeName(hidden), language + ": hidden name preserves mystery");
            Check(XgTechTreeCopy.Note(hidden, sim) == sim.NodeNote(hidden), language + ": hidden note preserves hint without effects");
            Check(XgTechTree.Name(XgTechTree.EntryOf(hidden.id), sim) == sim.NodeName(hidden), language + ": square does not reveal hidden name");
            Check(XgTechTreeCopy.Name(null, sim) == sim.NodeName(null) && XgTechTreeCopy.Note(null, sim) == sim.NodeNote(null), language + ": null fallback remains safe");
        }, language + " mystery");

        // This modifies only a newly constructed fixture, never an actual player's state.
        sim.S.autoLabelRevealed = true;
        ReadOnly(sim, () =>
        {
            int covered = 0, atlas = 0;
            foreach (var node in XgCatalog.Nodes)
            {
                string name = XgTechTreeCopy.Name(node, sim), note = XgTechTreeCopy.Note(node, sim);
                if (XgSim.IsAtlas(node))
                {
                    Check(name == sim.NodeName(node) && note == sim.NodeNote(node), language + ": atlas fallback " + node.id);
                    atlas++;
                    continue;
                }
                if (sim.NodeMystery(node)) continue;
                string marker = english ? "\n\nEffect: " : "\n\n效果：";
                int split = note.IndexOf(marker, StringComparison.Ordinal);
                Check(!string.IsNullOrWhiteSpace(name) && name != node.id, language + ": readable name " + node.id);
                Check(split > 0 && split + marker.Length < note.Length, language + ": explanation and player effect " + node.id);
                Check(english ? !ContainsHan(name + note) : ContainsHan(note), language + ": localized copy " + node.id);
                var entry = XgTechTree.EntryOf(node.id);
                Check(entry != null, language + ": catalog node has an entry " + node.id);
                if (!entry.IsChain) Check(XgTechTree.Name(entry, sim) == name, language + ": square uses professional node name " + node.id);
                covered++;
            }
            Check(covered > 0 && atlas > 0, language + ": ordinary and atlas coverage are nonempty");
        }, language + " complete catalog");
    }

    static void PayAndAutomaticLabelling(bool english)
    {
        var sim = new XgSim { English = english };
        sim.S.autoLabelRevealed = true;
        var pay = XgCatalog.Node("label.raise");
        foreach (int level in new[] { 0, 1, XgCatalog.RaiseMax })
        {
            sim.S.payRaise = level;
            ReadOnly(sim, () =>
            {
                string note = XgTechTreeCopy.Note(pay, sim);
                string expected = "×" + Number(XgCatalog.RaiseMultipliers[level]);
                if (level < XgCatalog.RaiseMax) expected += " → ×" + Number(XgCatalog.RaiseMultipliers[level + 1]);
                Check(note.Contains(expected), "Pay level " + level + ": actual catalog multipliers");
                Check(level < XgCatalog.RaiseMax || !note.Contains("→"), "Pay level " + level + ": max has no fictional next level");
                Check(note.Contains(english ? "Automatic labelling pay is unchanged" : "自动标注报酬不变"), "Pay level " + level + ": manual-only reward scope");
                Check((XgTechTree.Next(XgTechTree.EntryOf(pay.id), sim) == null) == (level == XgCatalog.RaiseMax), "Pay level " + level + ": next purchase matches max level");
            }, "Pay fixture level " + level);
        }
        ReadOnly(sim, () =>
        {
            string note = XgTechTreeCopy.Note(XgCatalog.Node("label.auto"), sim);
            Check(note.Contains(Number(XgCatalog.AutoRatePerLevel) + (english ? " cards/s" : " 题/秒")), "Automatic labelling uses actual per-level rate");
            Check(note.Contains(Number(XgCatalog.AutoMinAccuracy * 100) + "%"), "Automatic labelling states actual accuracy threshold");
            Check(note.Contains(english ? "per eligible desk" : "每个合格标注台"), "Automatic labelling rate is per eligible desk");
            Check(note.Contains(english ? "review capacity and pauses" : "复核队列和暂停状态"), "Automatic labelling states operating limits");
        }, "Automatic labelling copy");
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
            Check(XgTechTreeCopy.Name(relu, sim).Contains("ReLU") && reluCopy.Contains("98%") && reluCopy.Contains("95%"), "ReLU names its technique and base signal retention");
            Check(reluCopy.Contains(english ? "automatic configuration" : "自动配置") || reluCopy.Contains("Automatic configuration"), "ReLU explains automatic selection");
            string augmentCopy = XgTechTreeCopy.Note(augment, sim);
            Check(!augmentCopy.Contains(english ? "crop" : "裁剪"), "Augmentation does not promise an unimplemented crop");
            Check(augmentCopy.Contains(english ? "effective sample count" : "有效样本数") && augmentCopy.Contains(english ? "include originals" : "含原题"), "Augmentation identifies the effective count and included originals");
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
        var sim = new XgSim { English = english };
        ReadOnly(sim, () =>
        {
            int small = 0, large = 0;
            foreach (var node in XgCatalog.Nodes)
            {
                if (node.kind != XgNodeKind.Dataset) continue;
                var data = XgCatalog.Dataset(node.target);
                string note = XgTechTreeCopy.Note(node, sim);
                Check(note.Contains(XgSim.SamplesText(data.samples)), "Dataset sample count " + node.id);
                bool needsDownload = XgSim.PackBatches(data) > 1;
                Check(note.Contains(english ? "batches, the first with the pack" : "批下载，第一批随数据包到") == needsDownload, "Dataset download batches " + node.id);
                if (needsDownload)
                {
                    Check(note.Contains(Number(XgSim.DownloadSecondsAt(XgSim.BatchMB(data), XgSim.BroadbandMBps[0])) + (english ? " s on ADSL" : " 秒，宽带越快越短")), "Dataset per-batch ADSL download time " + node.id);
                    large++;
                }
                else small++;
            }
            Check(large > 0, "Dataset checks cover packs that come in batches"); // every pack comes in 5–20 batches (XgSim.PackBatches)
        }, "Dataset copy");
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
                    Check(XgTechTreeCopy.Note(shown, sim).Contains(english ? "\n\nEffect: " : "\n\n效果："), "Chain " + entry.id + " level " + level + ": displayed member has effect");
                    if (maxed)
                        Check(XgTechTree.Status(entry, sim, null) == XgSim.NodeStatus.Owned && double.IsPositiveInfinity(XgTechTree.Cost(entry, sim)), "Chain " + entry.id + ": max is owned with no further price");
                }, "Chain " + entry.id + " level " + level);
                if (owned < entry.members.Count) sim.S.unlocked.Add(entry.members[owned].id);
            }
        }
        Check(chains > 0, "Chain fixture exercises catalog ladders");
    }
}
