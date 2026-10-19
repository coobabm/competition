#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class UnifiedAtlasActualUiChecks
{
    public const string Scope="Unified atlas actual LCD checks. Explicit isolated TestMode histories: fullOpen/stage6, learned abilities, prior collection and finite wallet. Actual train.weights purchase, both entry controls, bilingual replays, native minimize/close, in-memory save rebind and real StageCurtain presentation from an explicitly queued learned-stage fixture. No natural six-stage progression, disk slot persistence, physical mouse, Player or model inference claim.";
    public static ChapterOneSim Fixture(bool viewer=true,int abilities=6)
    {
        var sim=new XgSim();sim.BeginOpening();sim.S.stage=sim.S.stageVision=sim.S.stageSequence=abilities;
        sim.S.fullOpen=true;sim.S.teachingFlow=true;sim.S.abilitiesEmerged=Enumerable.Range(1,abilities).ToList();
        // Profile is earned after the first conversation even on a stage-six save.
        sim.S.chatTurns=1;
        sim.S.spiderIntroDone=sim.S.firstWordsSaid=true;
        sim.S.teachIntroSaid=sim.S.teachThirdSaid=sim.S.teachTreeSaid=sim.S.teachTrainSaid=true;
        sim.MarkAfterthought("first.drift");
        foreach(string id in new[]{"train.curve","shared.lr","train.checkpoint"})if(!sim.S.unlocked.Contains(id))sim.S.unlocked.Add(id);
        if(viewer)sim.S.unlocked.Add(XgUnifiedAtlasCatalog.ViewerNode);
        sim.S.lessonCollected=XgTrainingLessons.Cards.Select(x=>x.id).ToList();sim.S.lessonDiscovered=new List<string>(sim.S.lessonCollected);
        sim.S.lessonPending.Clear();sim.S.quizAnswered=XgTrainingLessons.Quizzes.Select(x=>x.id).ToList();sim.S.quizQueued.Clear();sim.S.trainingLessonObservedStage=6;
        sim.S.stagePresentations.Clear();sim.S.stageUnlockSnapshots.Clear();sim.S.reduceFx=false;
        var world=new ChapterOneSim();world.S.prologue=2;world.S.appInstallState=2;world.S.money=1000000;
        world.S.labState=JsonUtility.ToJson(sim.S);GameCalendar.Advance(world.S,GameCalendar.DayFor(abilities,0));world.S.rentDay=GameCalendar.CurrentDay(world.S);
        return world;
    }
    static IEnumerator Until(Func<bool> ready,string label,double seconds=8)
    {double end=EditorApplication.timeSinceStartup+seconds;while(!ready()){Need(EditorApplication.timeSinceStartup<end,label+" timed out");yield return null;}}
    static void BindFixture(XingGuangController ctl,ChapterOneSim world)
    {
        ctl.runtime.SetSimulationForTests(world);
        typeof(XingGuangController).GetMethod("BindSave",F).Invoke(ctl,new object[]{world});
        ctl.runtime.tickEnabled=false;ctl.runtime.useDiskSave=false;ctl.enabled=false;
        Need(ctl.runtime.TestMode&&ctl.BoundTo(world),"isolated world and controller are rebound");
    }
    static IEnumerator Entry(XingGuangController ctl,bool profile,Action<RectTransform> click)
    {
        ctl.Open();ctl.View.Open(profile?"home":"tree");yield return null;yield return null;
        if(profile)
        {
            yield return Tap(Get<XgBtn>(ctl.View,"profileMore").rt,"Profile More",click);
            yield return Tap(Get<XgBtn>(ctl.View,"profileAtlas").rt,"Profile atlas",click);
        }
        else yield return Tap(Get<List<XgBtn>>(ctl.View.Tree,"toolbar")[6].rt,"Tech atlas",click);
        Need(ctl.View.AtlasShowing,"entry opens the collection");
    }
    static RectTransform NativeButton(XingGuangController ctl,string method)
    {
        var app=ctl.runtime.GetComponent<ChapterOneDesktopRouter>()?.Get(XingGuangController.AppId);
        var button=method=="MinimizeWindow"?app?.MinimizeButton:app?.CloseButton;
        return button==null?null:button.transform as RectTransform;
    }
    static IEnumerator SelectEntry(XgUnifiedAtlas atlas,XgAtlasCategory category,string id,Action<RectTransform> click)
    {
        yield return Tap(Get<XgBtn[]>(atlas,"tabs")[(int)category].rt,"Collection category "+category,click);
        int index=Get<List<XgAtlasEntry>>(atlas,"entries").FindIndex(x=>x.id==id);
        Need(index>=0,"collection entry exists: "+id);
        for(int page=0;page<index/7;page++)yield return Tap(Get<XgBtn>(atlas,"next").rt,"Next collection page",click);
        yield return Tap(Get<XgBtn[]>(atlas,"rows")[index%7].rt,"Select collection entry "+id,click);
        Need(atlas.SelectedId==id,"actual pagination selects "+id);
    }
    static IEnumerator BuyViewer(XingGuangController ctl,Action<RectTransform> click)
    {
        // TestMode disables XingGuangHost spending. Temporarily enable the real host in the
        // already redirected, non-ticking, disk-disabled fixture, then restore TestMode.
        var setter=typeof(ChapterOneRuntime).GetProperty("TestMode",F).GetSetMethod(true);
        setter.Invoke(ctl.runtime,new object[]{false});
        try
        {
        var tree=ctl.View.Tree;Need(tree.FocusNode(XgUnifiedAtlasCatalog.ViewerNode),"viewer node is navigable");
        yield return Until(()=>!tree.Pan.Moving,"viewer focus settles");
        var nodes=tree.root.GetComponentsInChildren<XgTechNodeView>(true).Where(x=>x.Covers(XgUnifiedAtlasCatalog.ViewerNode)&&x.gameObject.activeInHierarchy).ToArray();
        Need(nodes.Length==1,"one visible viewer node");
        var node=XgTechTree.Next(nodes[0].entry,ctl.Sim);
        Need(node!=null&&node.id==XgUnifiedAtlasCatalog.ViewerNode&&ctl.Sim.Status(node,ctl.Host)==XgSim.NodeStatus.Buyable,"viewer uses real prerequisite and price rules");
        double money=ctl.Host.Money,cost=ctl.Sim.NodeCost(node);
        yield return Tap(nodes[0].transform as RectTransform,"Purchase Weight viewer",click);
        Need(ctl.Sim.Has(XgUnifiedAtlasCatalog.ViewerNode)&&Math.Abs(money-ctl.Host.Money-cost)<1e-7,"real viewer purchase debits the actual price exactly once");
        }
        finally{setter.Invoke(ctl.runtime,new object[]{true});}
    }
    static IEnumerator FoldGuide(Action<RectTransform> click)
    {
        var guide=GuideNote.Instance;var note=guide==null?null:Get<RectTransform>(guide,"note");
        var task=note==null?null:note.Find("Paper/Task 0");
        if(note==null||!note.gameObject.activeInHierarchy||task==null||!task.gameObject.activeInHierarchy)yield break;
        yield return Tap(note.Find("Paper/Title Bar/Fold") as RectTransform,"Fold guide before caption controls",click);
        yield return Until(()=>!task.gameObject.activeInHierarchy,"guide folds");
    }
    public static IEnumerator RunBoundaries(XingGuangController ctl,Action<bool> language,Action<RectTransform> click,Action<string> capture)
    {
        Need(ctl.runtime.TestMode&&!ctl.enabled,"boundary checks require isolated paused TestMode");
        var original=ctl.runtime.Sim;string originalLab=JsonUtility.ToJson(ctl.Sim.S);
        var previousExclusive=StoryFocus.ExclusiveProbe;bool originalEnglish=ctl.Sim.English;
        try
        {
            for(int locale=0;locale<2;locale++)
            {
                bool en=locale==1;BindFixture(ctl,Fixture(false,2));language(en);ctl.Sim.English=en;
                XgUnifiedAtlas atlas=null;
                for(int entry=0;entry<2;entry++)
                {
                    yield return Entry(ctl,entry==1,click);atlas=Get<XgUnifiedAtlas>(ctl.View,"atlas");
                    string saved=JsonUtility.ToJson(ctl.Sim.S);
                    Need(!atlas.ReplayVisible,"unpaid viewer cannot replay through either entry");
                    Need(Get<TMP_Text>(atlas,"body").text.Contains(en?"Buy Weight viewer":"购买「权重可视化」"),"unpaid viewer explains the existing purchase");
                    capture("atlas-"+(en?"en":"zh")+"-unpaid-"+entry+".png");
                    yield return Tap(Get<XgBtn>(atlas,"action").rt,"Locate existing viewer technology",click);
                    Need(!atlas.Showing&&ctl.View.Tab=="tree"&&!ctl.Sim.Has(XgUnifiedAtlasCatalog.ViewerNode),"unlock link navigates without purchasing");
                    Need(Get<XgTechNodeView>(ctl.View.Tree,"detailShown").Covers(XgUnifiedAtlasCatalog.ViewerNode),"unlock link selects train.weights details");
                    Need(JsonUtility.ToJson(ctl.Sim.S)==saved,"unpaid atlas and unlock navigation do not mutate the lab");
                }
                yield return BuyViewer(ctl,click);
                for(int entry=0;entry<2;entry++)
                {
                    yield return Entry(ctl,entry==1,click);string saved=JsonUtility.ToJson(ctl.Sim.S);
                    Need(atlas.ReplayVisible,"purchase immediately enables learned replay at both entries");
                    yield return Tap(Get<XgBtn[]>(atlas,"rows")[1].rt,"Learned ability 2",click);Need(atlas.ReplayVisible,"second learned ability replays");
                    for(int stage=3;stage<=6;stage++)
                    {
                        yield return Tap(Get<XgBtn[]>(atlas,"rows")[stage-1].rt,"Unlearned ability "+stage,click);
                        Need(!atlas.ReplayVisible&&!Get<XgBtn>(atlas,"action").rt.gameObject.activeSelf,"viewer purchase does not grant unlearned ability "+stage);
                    }
                    Need(JsonUtility.ToJson(ctl.Sim.S)==saved,"learned and unlearned browsing is read-only");
                    yield return Tap(Get<XgBtn>(atlas,"close").rt,"Close after gates",click);
                    // Deliberately invoke a retained callback to prove a stale closed overlay cannot act.
                    Get<XgBtn>(atlas,"action").rt.GetComponent<Button>().onClick.Invoke();
                    Need(!atlas.Showing&&!atlas.ReplayVisible&&!atlas.Current,"closed overlay rejects stale callbacks");
                }
                Checks.Add((en?"English":"Chinese")+" actual unpaid double entry, train.weights navigation/purchase, learned/unlearned rights and closed-callback gates.");
            }
            BindFixture(ctl,Fixture());language(false);ctl.Sim.English=false;
            for(int stage=1;stage<=4;stage+=3)
            {
                yield return Entry(ctl,false,click);var atlas=Get<XgUnifiedAtlas>(ctl.View,"atlas");
                yield return Tap(Get<XgBtn[]>(atlas,"rows")[stage-1].rt,"Replay before native minimize",click);
                Need(atlas.ReplayVisible,"demo is active before minimizing");string saved=JsonUtility.ToJson(ctl.Sim.S);
                yield return FoldGuide(click);
                var minimize=NativeButton(ctl,"MinimizeWindow");Need(minimize!=null,"real minimize button exists");
                yield return Tap(minimize,"Native minimize atlas",click);
                Need(!ctl.View.Visible&&!atlas.Showing&&!atlas.ReplayVisible,"native minimize ends independent replay");
                var group=ctl.Window.GetComponent<CanvasGroup>();
                yield return Until(()=>group!=null&&group.alpha<=.001f&&!group.interactable&&!group.blocksRaycasts,"native minimize animation settles");
                ctl.Open();yield return null;yield return null;
                Need(!atlas.Showing&&!atlas.ReplayVisible&&JsonUtility.ToJson(ctl.Sim.S)==saved,"reopen does not resume or reward stale replay");
                yield return Entry(ctl,false,click);yield return Tap(Get<XgBtn[]>(atlas,"rows")[stage-1].rt,"Replay before native close",click);
                var close=NativeButton(ctl,"CloseWindow");Need(close!=null,"real CloseWindow event binding exists");
                yield return Tap(close,"Native close atlas",click);
                Need(!atlas.Showing&&!atlas.ReplayVisible,"native close ends independent replay");
                yield return Until(()=>!ctl.Window.isOn,"native close settles");
                ctl.Open();yield return null;yield return null;
                Need(!atlas.Showing&&!atlas.ReplayVisible&&JsonUtility.ToJson(ctl.Sim.S)==saved,"native close/reopen does not change saved state");
            }
            Checks.Add("Tiny and sequence replays both stopped by actual native Minimize and Close; reopening preserves full saved state.");
            yield return Entry(ctl,false,click);var boundAtlas=Get<XgUnifiedAtlas>(ctl.View,"atlas");
            yield return Tap(Get<XgBtn[]>(boundAtlas,"rows")[5].rt,"Select stage6 before rebind",click);
            var oldSim=ctl.Sim;string oldJson=JsonUtility.ToJson(oldSim.S);var staleAction=Get<XgBtn>(boundAtlas,"action");
            var emptyWorld=Fixture(false,1);var emptyLab=JsonUtility.FromJson<XgState>(emptyWorld.S.labState);
            emptyLab.lessonCollected.Clear();emptyLab.lessonDiscovered.Clear();emptyLab.quizAnswered.Clear();emptyLab.phenomena.seen.Clear();
            emptyWorld.S.labState=JsonUtility.ToJson(emptyLab);BindFixture(ctl,emptyWorld);
            staleAction.rt.GetComponent<Button>().onClick.Invoke();
            yield return null;yield return null;
            Need(!boundAtlas.Showing&&!boundAtlas.ReplayVisible&&JsonUtility.ToJson(oldSim.S)==oldJson,"save rebind stops replay and rejects old callback without writing old save");
            yield return Entry(ctl,false,click);
            Need(boundAtlas.SelectedId=="ab.1"&&!boundAtlas.ReplayVisible,"new save resets selection and inherits no viewer rights");
            capture("atlas-rebind-unpaid.png");string emptyBefore=JsonUtility.ToJson(ctl.Sim.S);
            foreach(string id in new[]{"lesson.overfit","quiz.q1b"})
            {
                yield return SelectEntry(boundAtlas,XgAtlasCategory.Phenomena,id,click);
                Need(Get<TMP_Text>(boundAtlas,"detailTitle").text=="尚未收录"&&!boundAtlas.ReplayVisible,"new save cannot inherit old collected explanation: "+id);
            }
            yield return SelectEntry(boundAtlas,XgAtlasCategory.Networks,"transformer",click);
            Need(Get<TMP_Text>(boundAtlas,"detailTitle").text=="未解锁结构"&&!Get<UnityEngine.Component>(boundAtlas,"network").gameObject.activeSelf,"new save cannot inherit an unowned structure diagram");
            Need(JsonUtility.ToJson(ctl.Sim.S)==emptyBefore,"sealed collection pagination is read-only");boundAtlas.Close();
            Checks.Add("In-memory production BindSave rebind rejects old callbacks, preserves old state and resets collection selection and rights. No disk-slot claim.");
            BindFixture(ctl,Fixture());yield return Entry(ctl,false,click);
            boundAtlas=Get<XgUnifiedAtlas>(ctl.View,"atlas");
            yield return Tap(Get<XgBtn[]>(boundAtlas,"rows")[3].rt,"Replay before real stage curtain",click);
            var curtain=XgStageCurtain.Install(ctl);curtain.enabled=true;
            string beforeStory=JsonUtility.ToJson(ctl.Sim.S);Need(ctl.Sim.HasAbility(4),"story fixture queues only an already learned ability");
            ctl.Sim.QueueStagePresentation(4); // Explicit presentation fixture, never claimed as natural progression.
            yield return Until(()=>XgStageCurtain.Showing&&XgStageCurtain.Step==0,"real month curtain begins");
            Need(StoryFocus.Exclusive&&!boundAtlas.Showing&&!boundAtlas.ReplayVisible,"real story exclusivity ends atlas replay");
            capture("atlas-story-month.png");
            var advance=Get<Button>(curtain,"nextButton");
            yield return Tap(advance.transform as RectTransform,"Story month to demo",click);Need(XgStageCurtain.Step==1,"story demo retains original sequence");
            Need(Get<XgStageSequenceDemo>(curtain,"sequence").Visible,"story owns its separate visible sequence demo");
            yield return Tap(advance.transform as RectTransform,"Story demo to first line",click);Need(XgStageCurtain.Step==2,"story first line follows demo");
            yield return Tap(advance.transform as RectTransform,"Reveal original first line",click);
            if(XgStageCurtain.Step==2)yield return Tap(advance.transform as RectTransform,"First line to unlocks",click);
            Need(XgStageCurtain.Step==3,"story unlocks follow first line");
            while(XgStageCurtain.Step==3)yield return Tap(advance.transform as RectTransform,"Finish original unlock page",click);
            yield return Until(()=>!XgStageCurtain.Showing&&!ctl.Sim.StagePresentationHeld,"story fade completes");
            Need(JsonUtility.ToJson(ctl.Sim.S)==beforeStory,"one real curtain consumes fixture queue without extra rewards, training or duplicate queue");
            Checks.Add("Real stage4 curtain from declared queue fixture preserved month -> demo -> first line -> unlocks -> fade; independent atlas stopped, full lab state restored after queue consumption.");
        }
        finally
        {
            var atlas=Get<XgUnifiedAtlas>(ctl.View,"atlas");if(atlas!=null)atlas.Close();
            original.S.labState=originalLab;BindFixture(ctl,original);language(originalEnglish);ctl.Sim.English=originalEnglish;
            StoryFocus.ExclusiveProbe=previousExclusive;
        }
    }
}
#endif
