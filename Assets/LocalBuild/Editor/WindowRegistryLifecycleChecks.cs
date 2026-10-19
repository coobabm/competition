#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.OS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WindowRegistryLifecycleChecks
{
    const BindingFlags StaticFlags=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags InstanceFlags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    [Serializable] sealed class Report
    {
        public string status="running",failure="";
        public string scope="Actual empty-scene PlayMode callbacks and saved additive scene load/unload. Subsystem reset invoked directly; existing Editor reload options are not modified. Legacy/default simulation replacement is in-memory, not a claim of on-disk save migration.";
        public List<string> checks=new List<string>();
    }
    sealed class StubView : IDesktopAppView { public bool EnsureReady()=>true;public void OnOpened(string tab){} }
    public static WindowManager MakeWindow(string name,Transform parent)
    {
        var go=new GameObject(name,typeof(RectTransform));go.SetActive(false);if(parent!=null)go.transform.SetParent(parent,false);
        var animator=go.AddComponent<Animator>();animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/LingGuangV05/DesktopBridge/OSArt/Animations/Apps/App Window/Window.controller");
        if(animator.runtimeAnimatorController==null)throw new InvalidOperationException("Fixture window animator controller missing");animator.enabled=false;
        var window=go.AddComponent<WindowManager>();window.windowAnimator=animator;window.windowContainer=(RectTransform)go.transform;
        // Avoid unrelated resize-prefab, taskbar, preference and animation behavior, while allowing real callbacks.
        typeof(WindowManager).GetField("enableMobileMode",InstanceFlags).SetValue(window,true);
        typeof(WindowManager).GetField("disableAtStart",InstanceFlags).SetValue(window,false);
        return window;
    }
    public static IEnumerator Frames()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Requires PlayMode");
        string dir=Path.Combine(Path.GetTempPath(),"LingGuang-WindowRegistry-QA-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        var report=new Report();var objects=new List<GameObject>();var body=Body(report,objects);bool completed=false;
        try
        {
            while(true)
            {
                bool more=false;object current=null;
                try{more=body.MoveNext();if(more)current=body.Current;}
                catch(Exception e){report.status="failed";report.failure=e.ToString();throw;}
                if(!more){completed=true;break;}yield return current;
            }
            report.status="passed";
        }
        finally
        {
            (body as IDisposable)?.Dispose();
            if(!completed&&report.status=="running"){report.status="failed";report.failure="Disposed before completion";}
            foreach(var go in objects)if(go!=null)UnityEngine.Object.Destroy(go);
            var scene=SceneManager.GetSceneByPath(WindowRegistryLifecycleRunner.FixtureScene);if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);
            File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(report,true));
            Debug.Log("WindowRegistryLifecycleChecks "+report.status+": "+Path.Combine(dir,"report.json"));
        }
    }
    static IEnumerator Body(Report report,List<GameObject> objects)
    {
        var root=new GameObject("Window registry fixture",typeof(RectTransform));root.SetActive(false);objects.Add(root);
        ((RectTransform)root.transform).sizeDelta=new Vector2(1920,1030);
        var runtime=root.AddComponent<ChapterOneRuntime>();runtime.SetSimulationForTests(new ChapterOneSim());
        var hidden=MakeWindow("Web Browser",root.transform);
        Reset();Check(DesktopWindowIndex.Get(runtime).Contains(hidden),"Initial discovery includes never-enabled hidden window",report);
        var snapshot=DesktopWindowIndex.Get(runtime);int scans=DesktopWindowIndex.WindowScans;
        for(int i=0;i<1000;i++)CheckSilent(ReferenceEquals(snapshot,DesktopWindowIndex.Get(runtime)),"Stable membership must reuse snapshot");
        Check(DesktopWindowIndex.WindowScans==scans,"1000 stable queries do not rescan scene",report);
        var late=MakeWindow("Configured hidden window",root.transform);
        var bridgeGo=new GameObject("Configured hidden bridge");bridgeGo.SetActive(false);objects.Add(bridgeGo);
        var bridge=bridgeGo.AddComponent<ChapterOneDesktopBridge>();
        bridge.Configure(runtime,null,late,late.windowContainer,null,null,null,null,null,null,"fixture");bridge.UseCustomView(new StubView());
        Check(DesktopWindowIndex.Get(runtime).Contains(late)&&DesktopWindowIndex.GetBindings(runtime).Contains(bridge),"Configure registers a never-enabled bridge and assigned hidden window",report);
        // Activate a distinct object outside the inactive runtime root: no factory registration shortcut.
        var active=MakeWindow("Callback active window",null);objects.Add(active.gameObject);active.gameObject.SetActive(true);bridgeGo.SetActive(true);yield return null;
        Check(DesktopWindowIndex.Get(runtime).Contains(active)&&DesktopWindowIndex.GetBindings(runtime).Contains(bridge),"Real Awake/OnEnable register dynamic window and bridge",report);
        active.gameObject.SetActive(false);bridgeGo.SetActive(false);yield return null;
        Check(DesktopWindowIndex.Get(runtime).Contains(active)&&DesktopWindowIndex.GetBindings(runtime).Contains(bridge),"Disabled/closed objects remain indexed",report);
        var dead=MakeWindow("Never enabled then destroyed",root.transform);DesktopWindowIndex.RegisterTree(dead.gameObject);
        Check(DesktopWindowIndex.Get(runtime).Contains(dead),"Explicit inactive factory registration works",report);
        UnityEngine.Object.Destroy(dead.gameObject);yield return null;
        Check(!DesktopWindowIndex.Get(runtime).Contains(dead),"Never-enabled destroyed object pruned without relying on OnDestroy",report);
        UnityEngine.Object.Destroy(active.gameObject);yield return null;
        Check(!DesktopWindowIndex.Get(runtime).Contains(active),"Previously activated destroyed object removed",report);
        // SceneManager.CreateScene alone would not exercise sceneLoaded: load a real saved fixture additively.
        string scenePath=WindowRegistryLifecycleRunner.FixtureScene;
        var operation=EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath,new LoadSceneParameters(LoadSceneMode.Additive));
        CheckSilent(operation!=null,"No additive load operation");double deadline=Time.realtimeSinceStartupAsDouble+15;
        while(!operation.isDone){CheckSilent(Time.realtimeSinceStartupAsDouble<deadline,"Additive scene load timeout");yield return null;}
        var scene=SceneManager.GetSceneByPath(scenePath);CheckSilent(scene.IsValid()&&scene.isLoaded,"Additive fixture scene not loaded");
        WindowManager additive=null;foreach(var go in scene.GetRootGameObjects()){additive=go.GetComponentInChildren<WindowManager>(true);if(additive!=null)break;}
        Check(additive!=null&&!additive.gameObject.activeInHierarchy&&DesktopWindowIndex.Get(runtime).Contains(additive),"Real sceneLoaded discovers inactive additive window",report);
        Reset();Check(DesktopWindowIndex.Get(runtime).Contains(hidden)&&DesktopWindowIndex.Get(runtime).Contains(late)&&DesktopWindowIndex.Get(runtime).Contains(additive)&&DesktopWindowIndex.GetBindings(runtime).Contains(bridge),"Subsystem Reset reseeds surviving hidden windows and bridge",report);
        var unload=SceneManager.UnloadSceneAsync(scene);while(!unload.isDone)yield return null;
        Check(!DesktopWindowIndex.Get(runtime).Contains(additive),"Actual scene unload prunes its hidden window",report);
        var defaults=root.AddComponent<DesktopWindowDefaults>();var prepare=typeof(DesktopWindowDefaults).GetMethod("Prepare",InstanceFlags);
        prepare.Invoke(defaults,new object[]{hidden,false});Check(Vector2.Distance(hidden.windowContainer.rect.size,DesktopWindowDefaults.PreferredSize("Web Browser"))<.1f,"Initial browser sizing preserved",report);
        prepare.Invoke(defaults,new object[]{hidden,true});hidden.windowContainer.anchoredPosition+=new Vector2(71,29);hidden.windowContainer.sizeDelta+=new Vector2(37,23);
        var position=hidden.windowContainer.anchoredPosition;var size=hidden.windowContainer.sizeDelta;
        runtime.SetSimulationForTests(new ChapterOneSim());Reset();Check(DesktopWindowIndex.Get(runtime).Contains(hidden),"Default/legacy simulation replacement retains existing window",report);
        prepare.Invoke(defaults,new object[]{hidden,false});Check(hidden.windowContainer.anchoredPosition==position&&hidden.windowContainer.sizeDelta==size,"Simulation rebind and registry reset preserve player move/resize",report);
        var replacementGo=new GameObject("Replacement inactive runtime");replacementGo.SetActive(false);objects.Add(replacementGo);var replacement=replacementGo.AddComponent<ChapterOneRuntime>();replacement.SetSimulationForTests(new ChapterOneSim());
        var replacementWindow=MakeWindow("Replacement owner window",replacementGo.transform);
        Check(DesktopWindowIndex.Get(replacement).Contains(replacementWindow)&&DesktopWindowIndex.Get(replacement).Contains(hidden),"Runtime owner replacement discovers its hidden subtree without losing existing windows",report);
    }
    static void Reset()=>typeof(DesktopWindowIndex).GetMethod("Reset",StaticFlags).Invoke(null,null);
    static void CheckSilent(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static void Check(bool value,string message,Report report){CheckSilent(value,message);report.checks.Add(message);}
}
#endif
