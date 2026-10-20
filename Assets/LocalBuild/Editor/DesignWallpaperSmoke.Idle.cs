using System;
using System.Collections.Generic;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using UnityEngine;

public static partial class DesignWallpaperSmoke
{
    static T IdleField<T>(XgIdleWindow window,string name) => (T)typeof(XgIdleWindow).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);
    static void IdleCall(XgIdleWindow window,string method) => typeof(XgIdleWindow).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(window,null);

    static IEnumerable<float> IdleSceneSteps(XingGuangController controller,ChapterOneRuntime runtime)
    {
        Check(!runtime.useDiskSave&&!runtime.tickEnabled, "Idle scene checks keep player saves and simulation ticks isolated");
        var idle=XgIdleWindow.Install(controller);
        bool enabled=idle.enabled;
        int auto=controller.Sim.S.autoLevel;
        bool paused=controller.Sim.AutoPaused;
        double income=controller.Sim.S.idleAutomaticIncome;
        double money=runtime.Sim.S.money, spent=controller.Sim.S.totalSpent;
        var canvases=new Dictionary<Canvas,bool>();
        var cameras=new Dictionary<Camera,bool>();
        try
        {
            // Exercise production eligibility; never bypass a title, prologue or story gate.
            IdleCall(idle,"Start");
            bool ready=(bool)typeof(XgIdleWindow).GetProperty("Ready",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(idle);
            Check(ready,"Idle fixture is genuinely past the title and prologue gates");
            idle.enabled=false;
            if(IdleField<RectTransform>(idle,"surface")==null)IdleCall(idle,"Build");
            Check(Resources.Load<Texture2D>("LingGuangV05/Branding/IdleMonitor")!=null,"Compact monitor loads the actual title artwork from Resources");
            controller.Sim.S.autoLevel=0;
            idle.Enter();
            Check(!XgIdleWindow.IsCompact && controller.View.Tab=="tree", "Locked idle entry navigates to technology without entering compact mode");
            Check(TechField<XgTechNodeView>("detailShown")!=null&&TechField<XgTechNodeView>("detailShown").Covers("label.auto"),"Locked idle entry focuses basic automatic answering");
            Check(runtime.Sim.S.money==money&&controller.Sim.S.totalSpent==spent&&controller.Sim.GlobalAutoLevel==0,"Locked idle navigation does not purchase automation");
            techPage.Pan.CancelMotion();
            controller.Sim.S.autoLevel=1;
            controller.Sim.AutoPaused=false;
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvases[canvas]=canvas.enabled;
            foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))cameras[camera]=camera.enabled;
            idle.Enter();
            var surface=IdleField<RectTransform>(idle,"surface");
            Check(XgIdleWindow.IsCompact&&surface!=null&&surface.gameObject.activeInHierarchy,"One basic automation level opens the compact surface");
            Check(surface.GetComponent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay,"Compact surface has its own overlay canvas");
            Canvas.ForceUpdateCanvases();
            var corners=new Vector3[4];IdleField<RectTransform>(idle,"monitor").GetWorldCorners(corners);
            foreach(var corner in corners)Check(corner.x>=-1&&corner.y>=-1&&corner.x<=Screen.width+1&&corner.y<=Screen.height+1,"Entire compact monitor fits the current game window");
            Check(IdleField<double>(idle,"displayed")==0,"Compact session income starts at zero");
            TechCapture(controller,"idle-compact-start");
            yield return .25f;
            // Inject one already-settled automation receipt, not a second wallet settlement.
            controller.Sim.S.idleAutomaticIncome+=1.5;
            yield return 1.05f;
            IdleCall(idle,"Update");
            double shown=IdleField<double>(idle,"displayed");
            Check(shown>0&&shown<=1.5,"Compact digits approach an actual settled income receipt without exceeding it");
            controller.Sim.AutoPaused=true;
            yield return 1.05f;
            IdleCall(idle,"Update");
            Check(IdleField<double>(idle,"displayed")>=shown&&IdleField<double>(idle,"displayed")<=1.5,"Paused display may finish the existing receipt but invents no income");
            Check(controller.Sim.S.idleAutomaticIncome==income+1.5&&runtime.Sim.S.money==money,"Idle display never credits a second income stream");
            TechCapture(controller,"idle-paused-settled-income");
            yield return .25f;
            idle.Restore();
            Check(!XgIdleWindow.IsCompact&&!surface.gameObject.activeSelf,"Restoring closes compact mode");
            foreach(var pair in canvases)if(pair.Key!=null)Check(pair.Key.enabled==pair.Value,"Canvas enabled state restored: "+pair.Key.name);
            foreach(var pair in cameras)if(pair.Key!=null)Check(pair.Key.enabled==pair.Value,"Camera enabled state restored: "+pair.Key.name);
        }
        finally
        {
            idle.Restore();idle.enabled=enabled;
            controller.Sim.S.autoLevel=auto;controller.Sim.AutoPaused=paused;controller.Sim.S.idleAutomaticIncome=income;
        }
    }
}
