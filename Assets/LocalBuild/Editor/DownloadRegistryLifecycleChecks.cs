#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using LingGuangV05.Desktop.Apps;

// Invoke/yield Frames() from an isolated PlayMode verification runner, not a real player session.
// No browser manager, ProcessDownload or ProcessComplete is invoked: those write DreamOS preferences.
public static class DownloadRegistryLifecycleChecks
{
    const BindingFlags StaticFlags=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags InstanceFlags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    [Serializable] sealed class Report { public string status="running",scope="Isolated objects and additive scene; subsystem reset simulated through the actual reset method, not actual editor reload settings.";public List<string> checks=new List<string>(),failures=new List<string>(),notes=new List<string>(); }
    static IEnumerable<WebBrowserDownloadItem> Rows()
    {
        var property=typeof(WebBrowserDownloadItem).GetProperty("KnownItems",StaticFlags);
        if(property==null)throw new InvalidOperationException("Apply registry patch before this check");
        return (IEnumerable<WebBrowserDownloadItem>)property.GetValue(null);
    }
    static void Reset()
    {
        var method=typeof(WebBrowserDownloadItem).GetMethod("ResetKnownItems",StaticFlags);
        if(method==null)throw new InvalidOperationException("Registry reset method missing");
        method.Invoke(null,null);
    }
    public static IEnumerator Frames()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Lifecycle checks require PlayMode callbacks");
        string dir=Path.Combine(Path.GetTempPath(),"LingGuang-DownloadRegistry-QA-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        var report=new Report();var objects=new List<GameObject>();Scene scene=default;
        try
        {
            var hidden=Make("Initially hidden",false,objects);
            Reset();Check(Contains(hidden),"Initial seed includes never-enabled hidden row",report);
            var active=Make("Runtime active",true,objects);yield return null;
            Check(Contains(active),"Runtime active creation registered by callbacks",report);
            active.isFinished=true;active.enabled=false;active.gameObject.SetActive(false);
            Check(Contains(active),"Completed disabled and hidden row remains registered",report);
            var textGo=new GameObject("Filename",typeof(RectTransform));textGo.transform.SetParent(hidden.transform,false);
            var text=textGo.AddComponent<TextMeshProUGUI>();typeof(WebBrowserDownloadItem).GetField("fileNameObject",InstanceFlags).SetValue(hidden,text);
            hidden.fileName="stable-download-key";hidden.SetDisplayName("Player AI.exe");
            Check(text.text=="Player AI.exe"&&hidden.fileName=="stable-download-key","Hidden display rename preserves persistence key",report);
            // Intentionally instantiate an inactive source AFTER the initial seed.
            var late=UnityEngine.Object.Instantiate(hidden);late.name="Runtime inactive clone";objects.Add(late.gameObject);
            // This is the same explicit call used at both production download-clone creation sites.
            typeof(WebBrowserDownloadItem).GetMethod("RegisterIdentityRow",InstanceFlags).Invoke(late,null);
            Check(Contains(late),"Runtime never-enabled inactive clone is registered at factory boundary",report);
            var snapshot=Rows();Check(ReferenceEquals(snapshot,Rows()),"Unchanged registry reuses cached snapshot without per-read array allocation",report);
            var deadHidden=UnityEngine.Object.Instantiate(hidden);objects.Add(deadHidden.gameObject);
            typeof(WebBrowserDownloadItem).GetMethod("RegisterIdentityRow",InstanceFlags).Invoke(deadHidden,null);
            UnityEngine.Object.Destroy(deadHidden.gameObject);yield return null;
            Check(!ContainsReference(deadHidden),"Never-enabled destroyed clone is pruned even without OnDestroy",report);
            late.gameObject.SetActive(true);yield return null;
            Check(Contains(late),"Previously inactive clone registers when first activated",report);
            var dead=Make("Destroy active",true,objects);yield return null;
            Check(Contains(dead),"Destruction fixture initially registered",report);
            UnityEngine.Object.Destroy(dead.gameObject);yield return null;
            Check(!ContainsReference(dead),"Destroyed active row removed without stale managed reference",report);
            // Enumeration must not throw if creation occurs during a caller's iteration.
            var iterator=Rows().GetEnumerator();bool moved=iterator.MoveNext();
            var during=Make("Created during enumeration",true,objects);yield return null;
            bool safe=true;try{while(iterator.MoveNext()){} }catch(InvalidOperationException){safe=false;}finally{iterator.Dispose();}
            Check(moved&&safe,"Enumeration survives registered collection change",report);
            // Calls the actual SubsystemRegistration callback while instances remain alive.
            Reset();Check(Contains(hidden)&&Contains(active)&&Contains(late)&&Contains(during),"Subsystem reset reseeds surviving active/disabled/hidden instances",report);
            scene=SceneManager.CreateScene("RegistryReentryQA-"+Guid.NewGuid().ToString("N"));
            var replacement=Make("Replacement scene hidden row",false,objects);SceneManager.MoveGameObjectToScene(replacement.gameObject,scene);
            // CreateScene itself does not simulate sceneLoaded: invoke the actual registered callback.
            typeof(WebBrowserDownloadItem).GetMethod("OnRegistrySceneLoaded",StaticFlags).Invoke(null,new object[]{scene,LoadSceneMode.Additive});
            Check(Contains(replacement),"Scene-loaded callback discovers new hidden scene rows after an earlier seed",report);
            Reset();Check(Contains(replacement),"Reload/reset discovery includes new hidden scene row",report);
            report.notes.Add("Scene-loaded/subsystem callbacks exercised directly; actual Enter Play Mode options remain root-runner coverage.");
            report.status=report.failures.Count==0?"passed":"failed";
        }
        finally
        {
            foreach(var go in objects)if(go!=null)UnityEngine.Object.Destroy(go);
            if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);
            File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(report,true));
            Debug.Log("DownloadRegistryLifecycleChecks "+report.status+": "+Path.Combine(dir,"report.json"));
        }
        if(report.failures.Count>0)throw new InvalidOperationException(string.Join("; ",report.failures));
    }
    static WebBrowserDownloadItem Make(string name,bool active,List<GameObject> objects)
    {
        var go=new GameObject(name);go.SetActive(false);objects.Add(go);var row=go.AddComponent<WebBrowserDownloadItem>();if(active)go.SetActive(true);return row;
    }
    static bool Contains(WebBrowserDownloadItem wanted){foreach(var row in Rows())if(row!=null&&row==wanted)return true;return false;}
    static bool ContainsReference(WebBrowserDownloadItem wanted){foreach(var row in Rows())if(ReferenceEquals(row,wanted))return true;return false;}
    static void Check(bool value,string name,Report report){if(value)report.checks.Add(name);else report.failures.Add(name);}
}
#endif
