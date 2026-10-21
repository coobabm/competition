#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.CaseMenu;
using LingGuangV05.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// run-isolated-ui.ps1 -ExecuteMethod RealTitleSceneChecks.Run (without -Quit).
[InitializeOnLoad]
public static class RealTitleSceneChecks
{
    const string Key="RealTitleSceneChecks.Root", OldSave="RealTitleSceneChecks.OldSave", OldProfile="RealTitleSceneChecks.OldProfile";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static string Root=>SessionState.GetString(Key,"");
    static readonly FieldInfo Profile=typeof(LingGuangV05.Desktop.OS.DreamOSDataManager).GetField("dataPath",BindingFlags.Static|BindingFlags.NonPublic);
    static CaseMenuController menu;
    static CaseMenuHud hud;
    static Report report;
    static double deadline,readyAt;
    static int phase,lastFrame=-1,frames;
    static bool finishing;
    [Serializable] sealed class Report
    {
        public string status="running",failure="",unityVersion;
        public string scope="Real ComputerCaseMenu PlayMode and additive Main; real menuCamera plus existing HUD rendered at 1920x1080. Direct hover API, no mouse/keyboard injection, no power click. Isolated save/profile and wrapper identity. Not Player-build or physical-input validation.";
        public List<string> checks=new List<string>(),screenshots=new List<string>(),errors=new List<string>();
    }
    static RealTitleSceneChecks(){EditorApplication.playModeStateChanged+=State;}
    public static void Run()
    {
        if(!Application.isBatchMode||Application.companyName!="CodexIsolatedQA"||!Application.productName.StartsWith("UI-",StringComparison.Ordinal))
            throw new InvalidOperationException("Run only through run-isolated-ui.ps1 in its disposable project.");
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode.");
        var dir=Path.GetFullPath(Path.Combine("LocalVerification","RealTitle",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
        Directory.CreateDirectory(Path.Combine(dir,"Save"));Directory.CreateDirectory(Path.Combine(dir,"DesktopProfile"));
        SessionState.SetString(Key,dir);SessionState.SetString(OldSave,ChapterOneRuntime.VerificationSaveDirectory??"");
        SessionState.SetString(OldProfile,(string)Profile.GetValue(null)??"");
        Isolate();EditorSearchWarmup.Run();
        var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));PrepareGameView(view);
        EditorSceneManager.OpenScene(CaseMenuController.ScenePath);EditorApplication.EnterPlaymode();
    }
    static object ViewSizeGroup()
    {
        var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(ScriptableSingleton<>).MakeGenericType(type);
        return type.GetMethod("GetGroup").Invoke(singleton.GetProperty("instance").GetValue(null),new object[]{GameViewSizeGroupType.Standalone});
    }
    static void PrepareGameView(EditorWindow view)
    {
        var selected=view.GetType().GetProperty("selectedSizeIndex",F);
        SessionState.SetInt(Key+"ViewIndex",(int)selected.GetValue(view));
        SessionState.SetBool(Key+"ViewSizePrepared",true);
        var group=ViewSizeGroup();int total=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        int builtIn=(int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group,null);
        string tag="RealTitleQA-"+Guid.NewGuid().ToString("N");
        SessionState.SetString(Key+"ViewTag",tag);SessionState.SetInt(Key+"ViewCustom",total-builtIn);
        var assembly=typeof(EditorWindow).Assembly;
        var mode=assembly.GetType("UnityEditor.GameViewSizeType");
        var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(mode,"FixedResolution"),1920,1080,tag});
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});selected.SetValue(view,total);
    }
    static void RestoreGameView()
    {
        if(!SessionState.GetBool(Key+"ViewSizePrepared",false))return;
        var group=ViewSizeGroup();var view=Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(x=>x.GetType().FullName=="UnityEditor.GameView");
        int total=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        int builtIn=(int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group,null);
        if(view!=null)view.GetType().GetProperty("selectedSizeIndex",F).SetValue(view,Mathf.Clamp(SessionState.GetInt(Key+"ViewIndex",0),0,Mathf.Max(0,total-1)));
        int custom=SessionState.GetInt(Key+"ViewCustom",-1);string tag=SessionState.GetString(Key+"ViewTag","");
        var labels=group.GetType().GetMethod("GetDisplayTexts").Invoke(group,null) as string[];
        if(custom>=0&&tag.Length>0&&labels!=null&&builtIn+custom<labels.Length&&labels[builtIn+custom].Contains(tag))
            group.GetType().GetMethod("RemoveCustomSize",F).Invoke(group,new object[]{custom});
        SessionState.EraseBool(Key+"ViewSizePrepared");SessionState.EraseString(Key+"ViewTag");
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Isolate()
    {
        if(Root.Length==0)return;
        ChapterOneRuntime.VerificationSaveDirectory=Path.Combine(Root,"Save");
        Profile.SetValue(null,Path.Combine(Root,"DesktopProfile")+Path.DirectorySeparatorChar);
        SceneManager.sceneLoaded-=DisableModel;SceneManager.sceneLoaded+=DisableModel;
    }
    static void DisableModel(Scene scene,LoadSceneMode mode)
    {
        foreach(var root in scene.GetRootGameObjects())foreach(var rt in root.GetComponentsInChildren<ChapterOneRuntime>(true))
        {var llm=rt.GetComponent<LingGuangV05.Desktop.LLM.LocalLlm>()??rt.gameObject.AddComponent<LingGuangV05.Desktop.LLM.LocalLlm>();llm.enabled=false;
            var embed=rt.GetComponent<LingGuangV05.Desktop.LLM.LocalEmbedder>()??rt.gameObject.AddComponent<LingGuangV05.Desktop.LLM.LocalEmbedder>();embed.enabled=false;rt.tickEnabled=false;}
    }
    static void State(PlayModeStateChange state)
    {
        if(Root.Length==0)return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            report=new Report{unityVersion=Application.unityVersion};phase=frames=0;lastFrame=-1;readyAt=0;finishing=false;deadline=EditorApplication.timeSinceStartup+180;
            Screen.SetResolution(1920,1080,false);Application.logMessageReceived+=Log;EditorApplication.update+=Tick;
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;SceneManager.sceneLoaded-=DisableModel;
            RestoreGameView();
            var path=Root;bool ok=File.Exists(Path.Combine(path,"passed.txt"));
            var save=SessionState.GetString(OldSave,"");ChapterOneRuntime.VerificationSaveDirectory=save.Length==0?null:save;
            var profile=SessionState.GetString(OldProfile,"");Profile.SetValue(null,profile.Length==0?null:profile);
            SessionState.EraseString(Key);SessionState.EraseString(OldSave);SessionState.EraseString(OldProfile);
            Debug.Log("REAL_TITLE_SCENE_"+(ok?"PASS":"FAIL")+": "+path);EditorApplication.Exit(ok?0:1);
        }
    }
    static void Log(string message,string stack,LogType type)
    {if(!finishing&&report!=null&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))report.errors.Add(message+"\n"+stack);}
    static void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);report.checks.Add(name);}
    static void Tick()
    {
        if(!EditorApplication.isPlaying||finishing||Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Real title phase "+phase);
            if(report.errors.Count>0)throw new InvalidOperationException(report.errors[0]);
            if(phase==0)
            {
                menu=Object.FindAnyObjectByType<CaseMenuController>();if(menu==null)return;
                if(!string.IsNullOrEmpty(menu.LastError))throw new InvalidOperationException(menu.LastError);
                if(!menu.IsMainReady||Screen.width!=1920||Screen.height!=1080)return;
                Check(Screen.width==1920&&Screen.height==1080,"Game view is 1920x1080");
                hud=menu.GetComponent<CaseMenuHud>();Check(hud!=null,"Real title HUD exists");
                // Freeze only the controller's pointer polling; the real button visuals still Update.
                menu.enabled=false;hud.SetChromeAlpha(1);hud.SetLoading(-1,null);
                var texts=SceneManager.GetSceneByPath(CaseMenuController.ScenePath).GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TMP_Text>(true)).ToArray();
                var legends=texts.Where(t=>t.name.StartsWith("Start engraved ")||t.name.StartsWith("Settings engraved ")).ToArray();
                Check(legends.Length==4&&legends.All(t=>!t.gameObject.activeInHierarchy),"All four old physical legends hidden");
                Check(!hud.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.name=="Subheading"||t.text.Contains("从一次开机开始")),"No retired subtitle");
                Check(menu.GetComponent<CaseMenuMainDisplay>().Runtime.SavePath.StartsWith(Path.Combine(Root,"Save")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Runtime uses isolated Save directory");
                phase=1;frames=0;readyAt=EditorApplication.timeSinceStartup;SetHover(null);return;
            }
            if(phase==1&&EditorApplication.timeSinceStartup-readyAt<5)return;
            if(++frames<5)return;frames=0;
            if(phase==1){Capture("01-default",null);SetHover(menu.startButton);phase=2;return;}
            if(phase==2){Capture("02-power-hover",menu.startButton);SetHover(menu.settingsButton);phase=3;return;}
            if(phase==3){Capture("03-settings-hover",menu.settingsButton);Check(!menu.HasEnteredGame&&!menu.IsTransitioning,"No power activation or transition");Check(report.errors.Count==0,"No runtime/render errors");Finish(null);}
        }catch(Exception e){Finish(e);}
    }
    static void SetHover(CaseMenuButton target)
    {menu.startButton.SetHovered(target==menu.startButton);menu.settingsButton.SetHovered(target==menu.settingsButton);hud.SetPhysicalHint(target,menu.menuCamera);}
    static void Capture(string name,CaseMenuButton target)
    {
        var camera=menu.menuCamera;var canvas=hud.GetComponentsInChildren<Canvas>(true).Single(c=>c.name=="CaseMenuOverlay");
        var scaler=canvas.GetComponent<CanvasScaler>();var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance,oldScale=canvas.scaleFactor,oldAspect=camera.aspect;
        bool oldScaler=scaler.enabled;var oldTarget=camera.targetTexture;var active=RenderTexture.active;
        int oldMask=camera.cullingMask;var hudTransforms=canvas.GetComponentsInChildren<Transform>(true);var oldLayers=hudTransforms.Select(t=>t.gameObject.layer).ToArray();
        var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};Texture2D pixels=null;
        try
        {
            rt.Create();camera.targetTexture=rt;camera.aspect=16f/9;scaler.enabled=false;
            foreach(var child in hudTransforms)child.gameObject.layer=30;
            camera.cullingMask=oldMask|(1<<30);
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=Mathf.Max(camera.nearClipPlane+.1f,.5f);canvas.scaleFactor=1;
            Canvas.ForceUpdateCanvases();hud.SetPhysicalHint(target,camera);Canvas.ForceUpdateCanvases();
            Check(Mathf.Abs(camera.aspect-16f/9)<.001f&&rt.width==1920&&rt.height==1080,name+": 1920x1080 camera target");
            var hint=canvas.transform.Find("Chrome/PhysicalButtonHint");
            if(target==null)Check(hint==null||!hint.gameObject.activeInHierarchy,name+": no hover hint");
            else
            {
                Check(hint!=null&&hint.gameObject.activeInHierarchy,name+": hover hint active");
                var label=hint.Find("Caption").GetComponent<TMP_Text>();var corners=new Vector3[4];label.rectTransform.GetWorldCorners(corners);
                float left=corners.Min(p=>camera.WorldToScreenPoint(p).x);
                var center=target.keycap!=null?target.keycap.position:target.transform.position;
                float right=camera.WorldToScreenPoint(center+camera.transform.right*(target.isStart?.066f:.049f)).x;
                Check(left>right+10,name+": caption right of physical button without overlap");
                Check(label.text==GameText.T(target.isStart?"开机":"设置",target.isStart?"Power on":"Settings"),name+": correct caption");
                Check(target.pulse!=null&&target.pulse.enabled,name+": hover outer ring visible");
            }
            foreach(var text in hud.GetComponentsInChildren<TMP_Text>(true))text.ForceMeshUpdate(true,true);
            Canvas.ForceUpdateCanvases();
            // Compare the same real camera with/without the HUD layer, in the same frame.
            // A large scene-only PNG must never satisfy the screenshot check.
            pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            camera.cullingMask=oldMask&~(1<<30);camera.Render();RenderTexture.active=rt;
            pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();var sceneOnly=pixels.GetPixels32();
            camera.cullingMask=oldMask|(1<<30);camera.Render();RenderTexture.active=rt;
            pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();var withHud=pixels.GetPixels32();
            int changedTitlePixels=0;
            for(int y=650;y<1000;y++)for(int x=100;x<680;x++)
            {
                int i=y*1920+x;var a=sceneOnly[i];var b=withHud[i];
                if(Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b)>90)changedTitlePixels++;
            }
            Check(changedTitlePixels>3000,name+": title HUD contributes visible pixels ("+changedTitlePixels+")");
            var path=Path.Combine(Root,name+".png");File.WriteAllBytes(path,pixels.EncodeToPNG());Check(new FileInfo(path).Length>10000,name+": PNG written");report.screenshots.Add(path);
        }
        finally
        {
            RenderTexture.active=active;camera.targetTexture=oldTarget;camera.aspect=oldAspect;camera.cullingMask=oldMask;
            for(int i=0;i<hudTransforms.Length;i++)if(hudTransforms[i]!=null)hudTransforms[i].gameObject.layer=oldLayers[i];
            canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;canvas.scaleFactor=oldScale;scaler.enabled=oldScaler;
            if(pixels!=null)Object.DestroyImmediate(pixels);rt.Release();Object.DestroyImmediate(rt);
        }
    }
    static void Finish(Exception error)
    {
        finishing=true;report.status=error==null?"passed":"failed";report.failure=error?.ToString()??"";
        File.WriteAllText(Path.Combine(Root,"report.json"),JsonUtility.ToJson(report,true));
        if(error==null)File.WriteAllText(Path.Combine(Root,"passed.txt"),"REAL_TITLE_SCENE_PASS");else Debug.LogException(error);
        EditorApplication.ExitPlaymode();
    }
}
#endif
