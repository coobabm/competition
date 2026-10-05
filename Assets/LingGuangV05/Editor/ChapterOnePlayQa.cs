#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Editor
{
    /// <summary>Explicitly invoked, repeatable Editor Play-mode smoke test. Never runs on player startup.</summary>
    public static class ChapterOnePlayQa
    {
        [Serializable] public sealed class Check { public string name; public bool passed; public string detail; }
        [Serializable] public sealed class Report { public string scope="Editor Play Mode; simulated pointer input through the real LCD relay; no Player build claim"; public bool completed; public int passed, failed; public List<Check> checks=new List<Check>(); }
        public static Report Result { get; private set; }
        public static bool Running { get; private set; }
        static readonly Queue<Action> actions=new Queue<Action>();
        static ChapterOneDesktopBridge bridge;
        static AeroLcdInputSurface surface;
        static double next;
        static double stepsBeforeClose, moneyBeforeClose;
        static string output;

        public static void Start()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Main Play Mode before QA.");
            if (Running) throw new InvalidOperationException("QA already running.");
            bridge=UnityEngine.Object.FindFirstObjectByType<ChapterOneDesktopBridge>();
            surface=UnityEngine.Object.FindFirstObjectByType<AeroLcdInputSurface>();
            if (bridge==null || surface==null) throw new InvalidOperationException("Missing chapter adapter/LCD surface.");
            output=Path.Combine(Application.dataPath,"../Evidence/LingGuang-ChapterOne/play-qa.json");
            Result=new Report(); actions.Clear();
            bridge.Runtime.SetSimulationForTests(new ChapterOneSim());
            bridge.Runtime.tickEnabled=true;
            CheckNow("single constant-lifetime runtime",UnityEngine.Object.FindObjectsByType<Runtime.ChapterOneRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1 && !bridge.Runtime.transform.IsChildOf(bridge.Window.transform));
            actions.Enqueue(()=>{ Click(bridge.DesktopShortcut.transform as RectTransform); });
            actions.Enqueue(()=>
            {
                CheckNow("desktop shortcut opens native app",bridge.IsOpen && bridge.App.UiRoot!=null);
                Canvas.ForceUpdateCanvases();
                foreach(string name in new[]{"Brand","HeaderMoney","HeaderSamples","HeaderSteps","HeaderLearning","CardTitle"})
                {
                    var label=bridge.App.GetComponentsInChildren<TMPro.TMP_Text>(true).FirstOrDefault(x=>x.name==name);
                    if(label!=null) label.ForceMeshUpdate(true);
                    CheckNow("visible Chinese label "+name,label!=null && label.textInfo.characterCount>0 && label.textInfo.characterInfo.Take(label.textInfo.characterCount).Any(x=>x.isVisible));
                }
                CheckNow("custom board graphics have CanvasRenderer",bridge.App.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(x=>x.GetComponent<CanvasRenderer>()!=null));
                ClickButton("Tab_chat");
            });
            actions.Enqueue(()=>{ CheckNow("Lao Zhou help tab reachable through LCD",bridge.App.ActiveTab=="chat"); ClickButton("Tab_cards"); });
            for(int i=0;i<14;i++) actions.Enqueue(()=>
            {
                var sim=bridge.Runtime.Sim; int reviewed=sim.S.cardsReviewed;
                bridge.App.Refresh();
                var button=bridge.App.GetComponentsInChildren<Button>(true).FirstOrDefault(x=>x.gameObject.activeInHierarchy && x.name.EndsWith(sim.CurrentCard.expectedYes?"AnswerYes":"AnswerNo",StringComparison.Ordinal));
                if(button==null) throw new InvalidOperationException("Visible card answer button missing");
                Click(button.transform as RectTransform);
                CheckNow("LCD card judgment "+reviewed,sim.S.cardsReviewed==reviewed+1);
            });
            actions.Enqueue(()=>
            {
                var sim=bridge.Runtime.Sim;
                sim.Tick(90);
                CheckNow("connected board trains",sim.S.steps>20 && sim.HasOutputPath);
                CheckNow("data raises simulated accuracy",sim.Accuracy>=.65);
                bridge.OpenApp("skills");
            });
            actions.Enqueue(()=>{ ClickButton("UpgradeSkill"); CheckNow("LCD buys semantic upgrade",bridge.Runtime.Sim.S.semanticLevel==2); });
            actions.Enqueue(()=>{ ClickButton("UpgradeTraining"); CheckNow("LCD buys training upgrade",bridge.Runtime.Sim.S.trainingLevel==2); bridge.OpenApp("jobs"); });
            actions.Enqueue(()=>{ ClickButton("ToggleJob"); CheckNow("LCD enables first paid job",bridge.Runtime.Sim.S.jobEnabled); bridge.OpenApp("shop"); });
            actions.Enqueue(()=>
            {
                var sim=bridge.Runtime.Sim; var before=sim.S.gpuCount;
                double money=sim.S.money;
                ClickButton("BuyGpu"); bridge.App.Refresh();
                CheckNow("LCD GPU purchase spends exact price",sim.S.gpuCount==before+1 && Math.Abs(money-sim.S.money-sim.Config.gpuPrice)<.001);
                stepsBeforeClose=sim.S.steps; moneyBeforeClose=sim.S.money;
                Click(bridge.CloseButton.transform as RectTransform);
                next=EditorApplication.timeSinceStartup+2.5;
            });
            actions.Enqueue(()=>
            {
                CheckNow("close hides window not simulator",!bridge.IsOpen && bridge.Runtime.gameObject.activeInHierarchy);
                CheckNow("closed window continues training and jobs",bridge.Runtime.Sim.S.steps>stepsBeforeClose && bridge.Runtime.Sim.S.money>moneyBeforeClose);
                Click(bridge.DesktopShortcut.transform as RectTransform);
            });
            actions.Enqueue(()=>{ CheckNow("reopen preserves state",bridge.IsOpen && bridge.Runtime.Sim.S.cardsReviewed==14); Click(bridge.MinimizeButton.transform as RectTransform); });
            actions.Enqueue(()=>{ Click(bridge.TaskbarShortcut.transform as RectTransform); });
            actions.Enqueue(()=>{ CheckNow("taskbar restores app",bridge.Window.windowContainer.gameObject.activeInHierarchy && bridge.IsOpen); bridge.OpenApp("board"); });
            actions.Enqueue(()=>
            {
                CheckNow("board tab visible",bridge.App.ActiveTab=="board");
                var sim=bridge.Runtime.Sim;
                CheckNow("board states and weighted edges exist",sim.S.nodes.Count>=3 && sim.S.edges.Count>0 && sim.S.edges.All(x=>x.weight>0));
                ClickButton("BoardMode_place");
            });
            actions.Enqueue(()=>{ ClickButton("BoardCell_2_0"); CheckNow("LCD places neuron",bridge.Runtime.Sim.S.nodes.Any(x=>x.q==2 && x.r==0)); ClickButton("BoardMode_remove"); });
            actions.Enqueue(()=>{ ClickButton("BoardCell_2_0"); CheckNow("LCD removes neuron",!bridge.Runtime.Sim.S.nodes.Any(x=>x.q==2 && x.r==0)); ClickButton("BoardMode_connect"); });
            actions.Enqueue(()=>{ ClickButton("BoardCell_0_2"); ClickButton("BoardCell_1_2"); CheckNow("LCD removes directed edge and stops path",!bridge.Runtime.Sim.HasOutputPath); });
            actions.Enqueue(()=>{ ClickButton("BoardCell_0_2"); ClickButton("BoardCell_1_2"); CheckNow("LCD restores directed output path",bridge.Runtime.Sim.HasOutputPath);
                bridge.OpenApp("exam");
                CheckNow("exam preparation gate attainable",bridge.Runtime.Sim.CanStartExam);
            });
            actions.Enqueue(()=>{ ClickButton("StartExam"); CheckNow("LCD starts twenty-mail test",bridge.Runtime.Sim.S.examActive); bridge.App.Refresh(); });
            for(int i=0;i<20;i++) actions.Enqueue(()=>
            {
                var sim=bridge.Runtime.Sim;
                int before=sim.S.examAnswered;
                bridge.App.Refresh();
                var button=bridge.App.GetComponentsInChildren<Button>(true).FirstOrDefault(x=>x.gameObject.activeInHierarchy && x.name.EndsWith(sim.CurrentCard.expectedYes?"AnswerYes":"AnswerNo",StringComparison.Ordinal));
                if(button==null) throw new InvalidOperationException("Visible exam answer button missing");
                Click(button.transform as RectTransform);
                CheckNow("LCD exam judgment "+before,sim.S.examAnswered==before+1 || sim.S.chapterOneComplete);
            });
            actions.Enqueue(()=>
            {
                var sim=bridge.Runtime.Sim;
                CheckNow("chapter one completed",sim.S.chapterOneComplete && !sim.S.examActive);
                double money=sim.S.money;
                bool retry=sim.StartExam();
                CheckNow("cannot farm completion reward",!retry && sim.S.money==money);
                bridge.OpenApp("exam");
                CheckNow("QA never writes player save",!bridge.Runtime.useDiskSave);
            });
            Running=true; next=EditorApplication.timeSinceStartup+.7;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output,JsonUtility.ToJson(Result,true));
            EditorApplication.update+=Advance;
            EditorApplication.playModeStateChanged+=OnPlayModeChanged;
        }

        public static Vector2 PhysicalPoint(RectTransform target)
        {
            Canvas.ForceUpdateCanvases();
            var source=surface.sourceCamera;
            Vector2 sourcePoint=RectTransformUtility.WorldToScreenPoint(source,target.TransformPoint(target.rect.center));
            float width=source.targetTexture!=null?source.targetTexture.width:source.pixelWidth;
            float height=source.targetTexture!=null?source.targetTexture.height:source.pixelHeight;
            float u=sourcePoint.x/width, v=sourcePoint.y/height;
            if(surface.flipY) v=1-v;
            Rect bounds=surface.displayRect.rect;
            var world=surface.displayRect.TransformPoint(new Vector3(bounds.xMin+u*bounds.width,bounds.yMin+v*bounds.height,0));
            var canvas=surface.displayRect.GetComponentInParent<Canvas>();
            Camera camera=canvas!=null && canvas.renderMode!=RenderMode.ScreenSpaceOverlay ? (canvas.worldCamera!=null?canvas.worldCamera:Camera.main):null;
            return RectTransformUtility.WorldToScreenPoint(camera,world);
        }

        public static void Click(RectTransform target)
        {
            if(surface==null) surface=UnityEngine.Object.FindFirstObjectByType<AeroLcdInputSurface>();
            if(target==null || !target.gameObject.activeInHierarchy) throw new InvalidOperationException("Click target missing or inactive");
            var data=new PointerEventData(EventSystem.current) {position=PhysicalPoint(target),button=PointerEventData.InputButton.Left,pointerId=-1};
            if(!surface.TryMap(data.position,out _)) throw new InvalidOperationException("Target is outside visible LCD");
            surface.OnPointerEnter(data); surface.OnPointerMove(data); surface.OnPointerDown(data); surface.OnPointerUp(data);
        }

        static void ClickButton(string name)
        {
            var button=bridge.App.GetComponentsInChildren<Button>(true).FirstOrDefault(x=>x.name==name);
            if(button==null) throw new InvalidOperationException("Button not found: "+name);
            Click(button.transform as RectTransform);
        }

        static void Advance()
        {
            if(!EditorApplication.isPlaying) { if(actions.Count>0) CheckNow("QA interrupted before completing all steps",false); Finish(); return; }
            if(EditorApplication.timeSinceStartup<next) return;
            if(actions.Count==0) { Finish(); return; }
            next=EditorApplication.timeSinceStartup+.55;
            try { actions.Dequeue()(); }
            catch(Exception error) { CheckNow("QA step exception",false,error.ToString()); }
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if(Running && change==PlayModeStateChange.ExitingPlayMode)
            { if(actions.Count>0) CheckNow("QA interrupted before completing all steps",false); Finish(); }
        }

        static void CheckNow(string label,bool ok,string detail="")
        {
            Result.checks.Add(new Check {name=label,passed=ok,detail=detail});
            if(ok) Result.passed++; else Result.failed++;
        }

        static void Finish()
        {
            EditorApplication.update-=Advance; EditorApplication.playModeStateChanged-=OnPlayModeChanged; Running=false;
            if(Result==null) return;
            Result.completed=actions.Count==0;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output,JsonUtility.ToJson(Result,true));
            Debug.Log("[LingGuang Chapter1 QA] "+Result.passed+" passed, "+Result.failed+" failed; "+output);
        }
    }
}
#endif
