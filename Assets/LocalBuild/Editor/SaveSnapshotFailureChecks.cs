#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Persistence;
using UnityEditor;
using UnityEngine;

// Edit-mode test. Every possible store initialization is redirected to a new temporary directory.
public static class SaveSnapshotFailureChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    [Serializable] sealed class Report { public string status="running",failure="",directory;public List<string> checks=new List<string>(); }
    [MenuItem("Tools/Local Verification/Save snapshot failures preserve disk")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run in Edit Mode");
        string dir=Path.Combine(Path.GetTempPath(),"LingGuang-SaveSnapshot-QA-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var report=new Report{directory=dir};
        string oldOverride=ChapterOneRuntime.VerificationSaveDirectory;
        GameObject go=null;ChapterOneRuntime rt=null;IDisposable locale=null;
        try
        {
            ChapterOneRuntime.VerificationSaveDirectory=dir; // Set before AddComponent, including any Awake path.
            go=new GameObject("Isolated save transaction QA");go.SetActive(false);
            rt=go.AddComponent<ChapterOneRuntime>();rt.tickEnabled=false;
            var sim=new ChapterOneSim();sim.S.prologue=2;sim.S.appInstallState=2;sim.S.money=1234;
            rt.SetSimulationForTests(sim);
            var store=new AtomicSaveStore(dir);typeof(ChapterOneRuntime).GetField("store",Flags).SetValue(rt,store);rt.useDiskSave=true;
            Require(Path.GetFullPath(rt.SavePath).StartsWith(Path.GetFullPath(dir)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Non-isolated save path");
            int afterThrow=0;
            Action first=()=>sim.S.labState="partial-app-snapshot";
            Action fail=()=>throw new InvalidOperationException("QA snapshot failure");
            Action last=()=>afterThrow++;
            rt.Saving+=first;rt.Saving+=fail;rt.Saving+=last;
            string language="zh-CN";locale=GameText.Bind(()=>language);
            // Empty directory case must not create even a first save or temp file.
            Set(rt,"dirty",false);Set(rt,"notify",false);
            Require(!rt.SaveNow(),"Throwing collector incorrectly reported success without prior save");
            Require(Directory.GetFiles(dir).Length==0,"Failure created a disk artifact");
            Require((bool)Get(rt,"dirty")&&(bool)Get(rt,"notify"),"Failed snapshot did not remain dirty/notified");
            Require(rt.SaveStatus.Contains("保存失败")&&rt.SaveStatus.Contains("原存档保持不变"),"Missing Chinese failure explanation");
            Require(afterThrow==0,"Collector after throwing handler unexpectedly ran");
            report.checks.Add("First-save snapshot failure returns false, creates no files, sets dirty+notify and Chinese status");
            // Seed both primary and backup; preserving only primary would miss a harmful backup rotation.
            Require(store.TrySave("{\"checkpoint\":\"older\"}",out var status),status);
            Require(store.TrySave("{\"checkpoint\":\"current\"}",out status),status);
            var bytes=Directory.GetFiles(dir).ToDictionary(Path.GetFileName,File.ReadAllBytes);
            Require(bytes.ContainsKey("chapter-one.sav")&&bytes.ContainsKey("chapter-one.sav.bak"),"Fixture lacks both disk generations");
            foreach(string lang in new[]{"zh-CN",GameText.English})
            {
                language=lang;Set(rt,"dirty",false);Set(rt,"notify",false);
                Require(!rt.SaveNow(),"Throwing collector reported success: "+lang);
                Require((bool)Get(rt,"dirty")&&(bool)Get(rt,"notify"),"Failure cleared dirty/notification");
                var files=Directory.GetFiles(dir);Require(files.Length==bytes.Count,"Failure added/removed disk files");
                foreach(var file in files)Require(bytes.TryGetValue(Path.GetFileName(file),out var original)&&original.SequenceEqual(File.ReadAllBytes(file)),"Failure changed disk generation: "+file);
                Require(lang==GameText.English?rt.SaveStatus.Contains("Save failed")&&rt.SaveStatus.Contains("previous save is unchanged"):rt.SaveStatus.Contains("保存失败"),"Wrong failure locale");
                Require(afterThrow==0,"Multicast collector advanced after error");
                report.checks.Add(lang+": primary and backup byte-identical; false, dirty and localized failure status");
            }
            // Removing the failed collector and retrying must actually save and clear dirty.
            rt.Saving-=first;rt.Saving-=fail;rt.Saving-=last;
            Action complete=()=>sim.S.labState="complete-app-snapshot";rt.Saving+=complete;
            Require(rt.SaveNow(),"Healthy retry failed: "+rt.SaveStatus);
            Require(!(bool)Get(rt,"dirty"),"Healthy retry did not clear dirty");
            var loaded=store.Load();Require(loaded.Success,"Retry not readable");
            var restored=JsonUtility.FromJson<GameState>(loaded.Payload);
            Require(restored.labState=="complete-app-snapshot","Retry persisted partial snapshot");
            rt.Saving-=complete;
            report.checks.Add("Healthy retry writes complete app snapshot, reads back successfully and clears dirty");
            report.status="passed";
        }
        catch(Exception e){report.status="failed";report.failure=e.ToString();}
        finally
        {
            if(rt!=null)rt.useDiskSave=false; // Destroy/quit hooks cannot create another write.
            if(go!=null)UnityEngine.Object.DestroyImmediate(go);
            locale?.Dispose();ChapterOneRuntime.VerificationSaveDirectory=oldOverride;
            File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(report,true));
        }
        Debug.Log("SaveSnapshotFailureChecks "+report.status+": "+Path.Combine(dir,"report.json"));
        if(report.status!="passed")throw new InvalidOperationException(report.failure);
    }
    static object Get(object target,string name)=>target.GetType().GetField(name,Flags).GetValue(target);
    static void Set(object target,string name,object value)=>target.GetType().GetField(name,Flags).SetValue(target,value);
    static void Require(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
}
#endif
