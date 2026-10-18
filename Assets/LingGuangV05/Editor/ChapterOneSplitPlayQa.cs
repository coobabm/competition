#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Editor
{
    /// <summary>Cross-application acceptance test; all interactions use the actual LCD input relay.</summary>
    public static class ChapterOneSplitPlayQa
    {
        [Serializable] public sealed class Check { public string name; public bool passed; public string detail; }
        [Serializable] public sealed class Report
        {
            public string scope="Five desktop applications; Editor Play; LCD-relayed clicks; simulated time acceleration, no Player build proof";
            public bool completed;
            public int passed, failed;
            public List<Check> checks=new List<Check>();
        }
        public static Report Result { get; private set; }
        public static bool Running { get; private set; }
        static readonly Queue<Action> steps=new Queue<Action>();
        static readonly string[] Ids={"mail","lingguang","xunbao","home","yy"};
        static ChapterOneDesktopRouter router;
        static ChapterOneRuntime runtime;
        static double next, priorSteps, priorMoney;
        static string path;

        public static void Start()
        {
            if(Running || !EditorApplication.isPlaying) throw new InvalidOperationException("Enter Main Play Mode once before running split QA.");
            router=UnityEngine.Object.FindFirstObjectByType<ChapterOneDesktopRouter>();
            if(router==null) throw new InvalidOperationException("Application router missing; run installer.");
            runtime=router.Get("lingguang").Runtime;
            runtime.SetSimulationForTests(new ChapterOneSim()); runtime.tickEnabled=true;
            Result=new Report(); steps.Clear();
            path=Path.Combine(Application.dataPath,"../Evidence/LingGuang-ChapterOne/apps-play-qa.json");
            CheckNow("exact five game bindings",router.Bindings.Count()==5 && Ids.All(id=>router.Get(id)!=null));
            CheckNow("all apps share one simulation owner",Ids.All(id=>router.Get(id).Runtime==runtime) && UnityEngine.Object.FindObjectsByType<ChapterOneRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1);
            CheckNow("existing mail and messaging reused, total 16 windows",UnityEngine.Object.FindObjectsByType<WindowManager>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==16);
            foreach(string appId in Ids)
            {
                string id=appId;
                steps.Enqueue(()=>Click(router.Get(id).DesktopShortcut.transform as RectTransform));
                steps.Enqueue(()=>
                {
                    var b=router.Get(id);
                    if(b.IsNativeChat)
                    {
                        CheckNow(id+" opens original native Messaging",b.IsOpen && b.NativeChat!=null && b.NativeChat.IsReady);
                        CheckNow(id+" uses no selfdraw profile",b.App==null && b.Window.GetComponentsInChildren<LingGuangV05.UI.ChapterOneApp>(true).Length==0);
                        b.CloseApp();
                        next=EditorApplication.timeSinceStartup+1.2;
                        return;
                    }
                    if(b.NativeApp!=null)
                    {
                        CheckNow(id+" opens native component app",b.IsOpen && b.NativeApp.ProfileId==id);
                        string[] nativeExpected=id=="lingguang"?new[]{"board","skills","persona","exam"}:id=="xunbao"?new[]{"shop"}:new[]{"power","jobs"};
                        CheckNow(id+" exact native app pages",b.NativeApp.AvailableTabs.SequenceEqual(nativeExpected));
                        CheckNow(id+" real DreamOS controls",b.Window.GetComponentsInChildren<ButtonManager>(true).Length>0);
                        CheckNow(id+" no old generic dashboard",b.Window.GetComponentsInChildren<LingGuangV05.UI.ChapterOneApp>(true).Length==0);
                        b.CloseApp();
                        next=EditorApplication.timeSinceStartup+1.2;
                        return;
                    }
                    CheckNow(id+" desktop opens correct profile",b.IsOpen && b.App.ProfileId==id);
                    string[] expected=id=="lingguang"?new[]{"board","skills","persona","exam"}:id=="mail"?new[]{"cards"}:id=="xunbao"?new[]{"shop"}:id=="home"?new[]{"jobs","power"}:new[]{"chat"};
                    CheckNow(id+" exact app pages",b.App.AvailableTabs.SequenceEqual(expected));
                    CheckNow(id+" graphics have renderers",b.App.GetComponentsInChildren<Graphic>(true).All(x=>x.GetComponent<CanvasRenderer>()!=null));
                    var title=b.App.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(x=>x.name=="Brand");
                    if(title!=null) title.ForceMeshUpdate(true);
                    CheckNow(id+" title renders",title!=null && title.textInfo.characterCount>0);
                    b.CloseApp();
                    next=EditorApplication.timeSinceStartup+1.2;
                });
            }
            steps.Enqueue(()=>
            {
                var settings=UnityEngine.Object.FindFirstObjectByType<LingGuangV05.Desktop.Apps.SettingsApp>(FindObjectsInactive.Include);
                var iconParent=router.Get("mail").DesktopShortcut.transform.parent;
                CheckNow("native desktop drag contract",settings!=null && iconParent.Cast<Transform>().All(x=>x.GetComponent<ItemDragger>()!=null && x.GetComponent<ItemDragger>().dragContainer!=null));
                settings.SaveDesktopOrder(false);
                CheckNow("native Settings startup order handler",true);
                Click(router.Get("mail").DesktopShortcut.transform as RectTransform);
            });
            for(int i=0;i<14;i++) steps.Enqueue(()=>Answer("mail",false));
            steps.Enqueue(()=>{ runtime.Sim.Tick(90); CheckNow("training follows mail labels",runtime.Sim.S.steps>20 && runtime.Sim.Accuracy>=.65); router.Open("home","jobs"); });
            steps.Enqueue(()=>{ ClickButton("home","ToggleJob"); CheckNow("household starts job",runtime.Sim.S.jobEnabled); ClickButton("home","Tab_power"); });
            steps.Enqueue(()=>{ CheckNow("power belongs to household",router.Get("home").NativeApp.ActiveTab=="power"); router.Open("xunbao"); });
            steps.Enqueue(()=>{ var sim=runtime.Sim; double money=sim.S.money; int gpu=sim.S.gpuCount; ClickButton("xunbao","BuyGpu"); CheckNow("Xunbao GPU purchase shares balance",sim.S.gpuCount==gpu+1 && Math.Abs(money-sim.S.money-sim.Config.gpuPrice)<.001); router.Open("lingguang","skills"); });
            steps.Enqueue(()=>{ ClickButton("lingguang","UpgradeSkill"); CheckNow("LingGuang semantic upgrade",runtime.Sim.S.semanticLevel==2); });
            steps.Enqueue(()=>{ ClickButton("lingguang","UpgradeTraining"); CheckNow("LingGuang training upgrade",runtime.Sim.S.trainingLevel==2); ClickButton("lingguang","Tab_board"); });
            steps.Enqueue(()=>{ ClickButton("lingguang","BoardMode_place"); ClickButton("lingguang","BoardCell_2_0"); CheckNow("LingGuang adds neuron",runtime.Sim.S.nodes.Any(x=>x.q==2 && x.r==0)); ClickButton("lingguang","BoardMode_remove"); });
            steps.Enqueue(()=>{ ClickButton("lingguang","BoardCell_2_0"); CheckNow("LingGuang removes neuron",!runtime.Sim.S.nodes.Any(x=>x.q==2 && x.r==0)); ClickButton("lingguang","BoardMode_connect"); });
            steps.Enqueue(()=>{ ClickButton("lingguang","BoardCell_0_2"); ClickButton("lingguang","BoardCell_1_2"); CheckNow("disconnect stops training path",!runtime.Sim.HasOutputPath); });
            steps.Enqueue(()=>
            {
                ClickButton("lingguang","BoardCell_0_2"); ClickButton("lingguang","BoardCell_1_2");
                CheckNow("reconnect restores training path",runtime.Sim.HasOutputPath);
                priorSteps=runtime.Sim.S.steps; priorMoney=runtime.Sim.S.money;
                foreach(string id in Ids) router.Get(id).CloseApp();
                next=EditorApplication.timeSinceStartup+2.5;
            });
            steps.Enqueue(()=>
            {
                CheckNow("all software closed while simulation continues",Ids.All(id=>!router.Get(id).IsOpen) && runtime.Sim.S.steps>priorSteps && runtime.Sim.S.money>priorMoney);
                Click(router.Get("lingguang").DesktopShortcut.transform as RectTransform);
            });
            steps.Enqueue(()=>{ var b=router.Get("lingguang"); Click(b.MinimizeButton.transform as RectTransform); });
            steps.Enqueue(()=>Click(router.Get("lingguang").TaskbarShortcut.transform as RectTransform));
            steps.Enqueue(()=>{ CheckNow("native taskbar restores LingGuang",router.Get("lingguang").IsOpen); router.Open("lingguang","exam"); });
            steps.Enqueue(()=>
            {
                ClickButton("lingguang","StartExam"); CheckNow("exam starts inside LingGuang",runtime.Sim.S.examActive);
                router.Get("mail").App.Refresh();
                var answer=router.Get("mail").App.GetComponentsInChildren<Button>(true).First(x=>x.name=="CardAnswerYes");
                int before=runtime.Sim.S.examAnswered;
                router.Get("mail").App.SubmitAnswer(runtime.Sim.CurrentCard.expectedYes);
                CheckNow("mail cannot answer an active exam",!answer.interactable && runtime.Sim.S.examAnswered==before);
            });
            for(int i=0;i<20;i++) steps.Enqueue(()=>Answer("lingguang",true));
            steps.Enqueue(()=>
            {
                var sim=runtime.Sim; CheckNow("cross-app loop completes chapter",sim.S.chapterOneComplete && !sim.S.examActive);
                double money=sim.S.money; CheckNow("chapter reward cannot repeat",!sim.StartExam() && sim.S.money==money);
                router.Open("mail");
            });
            steps.Enqueue(()=>{ Answer("mail",false); CheckNow("mail resumes after exam",runtime.Sim.S.cardsReviewed==15); router.Open("yy"); });
            steps.Enqueue(()=>
            {
                var yy=router.Get("yy");
                CheckNow("YY Lao Zhou native chat accessible",yy.IsOpen && yy.IsNative && yy.NativeChat.IsReady && yy.NativeChat.Manager.chatList.Any(x=>x.individualName=="老周"));
                CheckNow("all apps still share same simulation",Ids.All(id=>router.Get(id).Runtime.Sim==runtime.Sim));
                CheckNow("test did not write player disk save",!runtime.useDiskSave);
                router.Open("lingguang","board");
            });
            Running=true; next=EditorApplication.timeSinceStartup+.7;
            SaveReport(); EditorApplication.update+=Advance; EditorApplication.playModeStateChanged+=OnPlayModeChanged;
        }

        static void Answer(string id,bool exam)
        {
            var binding=router.Get(id);
            if(binding.NativeApp!=null) binding.NativeApp.Refresh(); else binding.App.Refresh();
            var sim=runtime.Sim;
            int before=exam?sim.S.examAnswered:sim.S.cardsReviewed;
            ClickButton(id,(exam?"ExamCard":"Card")+(sim.CurrentCard.expectedYes?"AnswerYes":"AnswerNo"));
            int after=exam?sim.S.examAnswered:sim.S.cardsReviewed;
            CheckNow(id+" LCD answer "+before,after==before+1);
        }
        static void ClickButton(string id,string name)
        {
            var binding=router.Get(id);
            if(binding.NativeApp!=null)
            {
                var native=binding.NativeApp.FindControl(name);
                if(native==null) throw new InvalidOperationException(id+" missing native control "+name);
                Click(native); return;
            }
            var target=binding.App.GetComponentsInChildren<Button>(true).FirstOrDefault(x=>x.name==name);
            if(target==null) throw new InvalidOperationException(id+" missing button "+name);
            Click(target.transform as RectTransform);
        }
        static void Click(RectTransform target) => ChapterOnePlayQa.Click(target);
        static void CheckNow(string name,bool passed,string detail="")
        {
            Result.checks.Add(new Check{name=name,passed=passed,detail=detail});
            if(passed) Result.passed++; else Result.failed++;
        }
        static void Advance()
        {
            if(!EditorApplication.isPlaying) { if(steps.Count>0) CheckNow("QA interrupted",false); Finish(); return; }
            if(EditorApplication.timeSinceStartup<next) return;
            if(steps.Count==0) { Finish(); return; }
            next=EditorApplication.timeSinceStartup+.6;
            try { steps.Dequeue()(); } catch(Exception error) { CheckNow("QA step exception",false,error.ToString()); }
        }
        static void OnPlayModeChanged(PlayModeStateChange state)
        { if(Running && state==PlayModeStateChange.ExitingPlayMode) { if(steps.Count>0) CheckNow("QA interrupted",false); Finish(); } }
        static void SaveReport() { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path,JsonUtility.ToJson(Result,true)); }
        static void Finish()
        {
            EditorApplication.update-=Advance; EditorApplication.playModeStateChanged-=OnPlayModeChanged; Running=false;
            Result.completed=steps.Count==0; SaveReport();
            Debug.Log("[LingGuang apps QA] completed="+Result.completed+" passed="+Result.passed+" failed="+Result.failed);
        }
    }
}
#endif
