#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class IdleWindowLayoutChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static T Get<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    [Serializable] sealed class Report
    {
        public string status="running",error="";
        public List<string> checks=new List<string>();
        public string uncovered="Inactive geometry fixture only. Run DesignWallpaperSmoke.RunIdle for actual entry, locked-node navigation, status text, income pause/catchup and return-context checks. Native Windows resize/drag/always-on-top and save+quit require a player build.";
    }
    static void Need(Report r,bool ok,string label){if(!ok)throw new InvalidOperationException(label);r.checks.Add(label);}
    static RectTransform Child(string name,Transform parent)
    {var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);return (RectTransform)g.transform;}
    public static void Run()
    {
        if(!Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Dedicated batch EditMode only");
        string requested=null;var args=Environment.GetCommandLineArgs();
        for(int i=0;i<args.Length-1;i++)if(args[i]=="--verification-save-dir")requested=args[i+1];
        if(string.IsNullOrWhiteSpace(requested))throw new ArgumentException("Supply external --verification-save-dir");
        string rootPath=Path.GetFullPath(requested),project=Path.GetFullPath(Path.Combine(Application.dataPath,"..")).TrimEnd(Path.DirectorySeparatorChar);
        if(rootPath.Equals(project,StringComparison.OrdinalIgnoreCase)||rootPath.StartsWith(project+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("External output required");
        string dir=Path.Combine(rootPath,"IdleLayout-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        var r=new Report();GameObject root=null;XgIdleWindow idle=null;
        try
        {
            root=new GameObject("Inactive idle fixture",typeof(RectTransform),typeof(Canvas));root.SetActive(false);
            var desktop=(RectTransform)root.transform;desktop.sizeDelta=new Vector2(1440,900);
            var bar=Child("Taskbar",desktop);bar.sizeDelta=new Vector2(1440,48);
            var notification=Child("Aero Notification Area",bar);notification.anchorMin=new Vector2(1,0);notification.anchorMax=Vector2.one;notification.offsetMin=new Vector2(-242,0);notification.offsetMax=Vector2.zero;
            var prologue=Child("Prologue Layer",desktop);prologue.gameObject.SetActive(false);
            var host=Child("Isolated idle owner",desktop).gameObject;
            var controller=host.AddComponent<XingGuangController>();var sim=new XgSim();sim.S.mute=true;
            typeof(XingGuangController).GetProperty("Sim",F).GetSetMethod(true).Invoke(controller,new object[]{sim});
            Set(controller,"font",XgUiV4Fonts.Sans);
            idle=host.AddComponent<XgIdleWindow>();Set(idle,"owner",controller);Set(idle,"ui",new XgUi(XgUiV4Fonts.Sans,null,true));
            Call(idle,"BuildEntry",bar);Call(idle,"BuildSurface");
            var entry=Get<XgBtn>(idle,"entry");var panel=Get<RectTransform>(idle,"surface");
            Need(r,entry.rt.parent.parent.name=="AI Utility Tray"&&entry.rt.parent.parent.parent==bar&&!entry.rt.IsChildOf(prologue),"Tray entry is independent of hidden Prologue Layer");
            Need(r,entry.rt.parent.GetComponent<LayoutElement>().preferredWidth==32&&entry.rt.anchorMin==Vector2.zero&&entry.rt.anchorMax==Vector2.one&&!entry.label.gameObject.activeSelf,"Tray entry has a 32px hit area and no long label");
            Need(r,entry.rt.Find("Visual/Monitor glyph")!=null&&typeof(XgIdleWindow).GetField("entryMenu",F)==null,"Computer glyph exists and enters directly without a size menu");
            var entryFx=entry.rt.parent.GetComponent<XgJuice>();var surfaceFx=panel.GetComponent<XgJuice>();
            // This geometry fixture deliberately keeps the entire canvas inactive. Runtime presses require active buttons.
            Need(r,entryFx!=null&&entry.rt.GetComponentInParent<XgJuice>(true)==entryFx,"Entry uses the idle-owned feedback policy");
            sim.S.reduceFx=true;Call(idle,"SyncFeedback");
            Need(r,entryFx.Reduced&&surfaceFx.Reduced&&entryFx.Muted&&surfaceFx.Muted,"Reduced effects and mute synchronize before clicking in both hierarchies");
            sim.S.reduceFx=false;sim.S.mute=false;Call(idle,"SyncFeedback");
            Need(r,!entryFx.Muted&&!surfaceFx.Muted&&entryFx.Reduced==surfaceFx.Reduced,"Feedback settings stay synchronized after toggling");
            sim.S.mute=true;Call(idle,"SyncFeedback");
            string before=JsonUtility.ToJson(sim.S);
            var face=Get<RectTransform>(idle,"face");face.ForceUpdateRectTransforms();
            Need(r,XgIdleWindow.WindowSize==new Vector2(256,148)&&panel.GetComponent<CanvasScaler>().referenceResolution==XgIdleWindow.WindowSize,"Single 256x148 notification size drives the canvas");
            Need(r,Get<RectTransform>(idle,"monitor").sizeDelta==XgIdleWindow.WindowSize,"Title monitor art fills exactly the window size");
            foreach(string removed in new[]{"details","metricLabel","sizeToggle","offline","explanation","reviewTasks","optionsRestore","miniEntry"})
                Need(r,typeof(XgIdleWindow).GetField(removed,F)==null,"Removed final-design row/control stays absent: "+removed);
            Need(r,typeof(XgIdleWindow).GetMethod("SetMini",F)==null,"No Standard/Mini switch");
            var pause=Get<XgBtn>(idle,"pause");var back=Get<XgBtn>(idle,"review");
            Need(r,pause.rt.anchorMax.x<=back.rt.anchorMin.x&&pause.rt.offsetMax.x<0&&back.rt.offsetMin.x>0&&pause.rt.offsetMin.y>=0,"Two body buttons fit side by side without overlap");
            Need(r,Get<RectTransform>(idle,"wallet").rect.height>0&&face.rect.height>Get<RectTransform>(idle,"wallet").rect.height,"Digit viewport fits inside the monitor screen");
            Call(idle,"SetDigits",12.34);
            Need(r,Get<string>(idle,"digits").Contains("12.34"),"Wallet uses the settled-income number formatter");
            var options=Get<RectTransform>(idle,"options");
            Need(r,!options.gameObject.activeSelf&&options.GetComponentsInChildren<Button>(true).Length==2,"More menu holds only always-on-top and quit");
            var native=host.GetComponent<XgIdleNativeWindow>();
            Need(r,native.RequestedSize==XgIdleWindow.WindowSize&&!native.Topmost,"Native window requests the canvas size and is not topmost by default");
            Get<XgBtn>(idle,"pin").button.onClick.Invoke();
            Need(r,Get<bool>(idle,"topmost")&&native.Topmost,"Always-on-top toggle reaches the native window");
            Get<XgBtn>(idle,"pin").button.onClick.Invoke();
            Need(r,!Get<bool>(idle,"topmost")&&!native.Topmost,"Always-on-top toggles back off");
            Get<XgBtn>(idle,"quit").button.onClick.Invoke();
            Need(r,Get<float>(idle,"quitArmed")>=0,"First quit click only arms the confirmation");
            back.button.onClick.Invoke();
            Need(r,JsonUtility.ToJson(sim.S)==before,"Layout, menu and return callbacks neither change save data nor open an exe (fixture has no runtime)");
            r.status="passed";
        }
        catch(Exception ex){r.status="failed";r.error=ex.ToString();throw;}
        finally
        {
            if(idle!=null){Set(idle,"surface",null);Set(idle,"entryHost",null);Set(idle,"entry",null);}
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);
            File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(r,true));Debug.Log("IDLE_LAYOUT "+r.status+" "+dir);
        }
    }
}
#endif
