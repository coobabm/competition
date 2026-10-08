using System;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Story;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.EditorTools
{
    /// <summary>Prepared visual scenarios, NOT natural progression/balance evidence. Never uses a disk save.</summary>
    public static class SixStageVisualQa
    {
        static void RequireSafe(ChapterOneRuntime runtime)
        {
            if (!Application.isPlaying || runtime == null || runtime.useDiskSave || runtime.TestMode)
                throw new InvalidOperationException("Requires Play with useDiskSave=false and the real runtime enabled.");
        }
        public static string Show(int stage, string page, bool fresh = false)
        {
            var runtime = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>(); RequireSafe(runtime);
            var controller = UnityEngine.Object.FindAnyObjectByType<XingGuangController>();
            var director = UnityEngine.Object.FindAnyObjectByType<StoryDirector>();
            var presenter = UnityEngine.Object.FindAnyObjectByType<StoryDesktopPresenter>();
            if (director != null && director.Runner != null) director.Runner.Clear();
            if (presenter != null) typeof(StoryDesktopPresenter).GetMethod("OnStoryRebound", BindingFlags.NonPublic|BindingFlags.Instance).Invoke(presenter,new object[]{director});
            if (director != null) foreach (var beat in director.Library.Beats) runtime.Sim.S.story.MarkFired(beat.Id);
            runtime.Sim.InstallApp();
            var sim = new XgSim();
            if (!fresh)
            {
                runtime.Sim.S.money = 200000; runtime.Sim.S.gpuCount = 4; runtime.Sim.S.caseCount = 2;
                runtime.Sim.S.unpaidPower = false; runtime.Sim.S.billDue = 0; runtime.Sim.S.breakerTripped = false;
                foreach(var n in XgCatalog.Nodes)
                {
                    if (n.stage > stage || n.kind == XgNodeKind.Project || n.kind == XgNodeKind.Auto || n.tree == "label") continue;
                    if (!sim.S.unlocked.Contains(n.id)) sim.S.unlocked.Add(n.id);
                    if (n.kind == XgNodeKind.Dataset && !sim.S.owned.Contains(n.target)) sim.S.owned.Add(n.target);
                }
                sim.S.trainingIntroDelivered=sim.S.treeIntroDelivered=true;
                sim.S.vision.arch=stage==1?"perceptron":stage==2?"mlp":stage==3?"vgg":stage==4?"resnet":stage==5?"caption":"transformer";
                sim.S.sequence.arch=stage==1?"perceptron":stage==2?"mlp":stage==3?"rnn":stage==4?"lstm":stage==5?"attention":"transformer";
                sim.S.vision.depth=stage<3?stage:stage==3?16:32;
                sim.S.vision.width=2;
                sim.S.labels.Add(new XgLabelCount{dataset="mnist",count=5000});
                sim.S.labels.Add(new XgLabelCount{dataset="spam",count=5000});
                sim.S.vision.steps=1200;sim.S.sequence.steps=1200;sim.S.epochs=20;sim.S.vision.epoch=20;
                sim.S.best.Add(new XgBest{dataset="mnist",arch=sim.S.vision.arch,acc=.96});
                sim.S.best.Add(new XgBest{dataset="spam",arch=sim.S.sequence.arch,acc=.95});
                sim.S.stage=sim.S.stageVision=sim.S.stageSequence=stage;
                if(stage>=4)
                {
                    foreach(var id in new[]{"label.raise","label.auto","label.coop","label.brain","label.hard","label.audit"})sim.S.unlocked.Add(id);
                    sim.S.autoLevel=1;sim.S.payRaise=2;sim.S.coopThreshold=.99;
                }
                // Stage 6 is pre-training, alignment and the finale now (design v1.1 §7–8), not the old research project.
                if(stage==6){sim.S.unlocked.Add("transformer");sim.S.unlocked.Add("multihead");sim.S.unlocked.Add("layernorm");sim.S.unlocked.Add("residual");}
            }
            runtime.Sim.S.labState=JsonUtility.ToJson(sim.S);
            typeof(XingGuangController).GetMethod("BindSave",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,new object[]{runtime.Sim});
            controller.Open(); controller.View.Open(page); controller.View.Refresh(true);
            Canvas.ForceUpdateCanvases();
            return Snapshot();
        }
        public static string Snapshot()
        {
            var runtime=UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();RequireSafe(runtime);
            var controller=UnityEngine.Object.FindAnyObjectByType<XingGuangController>();
            return JsonUtility.ToJson(new SnapshotData{
                scenario="Prepared visual fixture; not natural progression", stage=controller.Sim.S.stage, page=controller.View.Tab,
                diskSave=runtime.useDiskSave, treeStages=controller.View.Tree.VisibleStageCount, treeNodes=controller.View.Tree.VisibleNodeCount,
                reviewCount=controller.Sim.ReviewCount(), trainVisible=controller.FeatureVisible("train"),treeVisible=controller.FeatureVisible("tree"),
                texts=controller.View.GetComponentsInChildren<TMPro.TMP_Text>(false).Select(t=>t.text).ToArray()
            },true);
        }
        public static bool ClickText(string text)
        {
            var runtime=UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();RequireSafe(runtime);
            var controller=UnityEngine.Object.FindAnyObjectByType<XingGuangController>();
            var button=controller.View.GetComponentsInChildren<Button>(false).FirstOrDefault(b=>b.GetComponentsInChildren<TMPro.TMP_Text>(false).Any(t=>t.text==text||t.text.StartsWith(text,StringComparison.Ordinal)));
            if(button==null)return false;
            LingGuangV05.Editor.ChapterOnePlayQa.Click((RectTransform)button.transform);return true;
        }
        [Serializable] sealed class SnapshotData
        {
            public string scenario,page;public int stage,treeStages,treeNodes,reviewCount;public bool diskSave,trainVisible,treeVisible;public string[] texts;
        }
    }
}
