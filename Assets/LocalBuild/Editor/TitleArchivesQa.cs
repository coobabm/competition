#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.CaseMenu;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class TitleArchivesQa
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    [Serializable] sealed class Report {public string status="passed",error="",scope="EditMode UI callbacks and real isolated disk. Not a native OS quit or animated transition test.";public List<string> checks=new List<string>();}
    [MenuItem("Tools/Local Verification/Title archives isolated")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run in EditMode");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","TitleArchives",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));Directory.CreateDirectory(dir);
        var report=new Report();GameObject host=null,canvas=null;var before=ChapterOneRuntime.VerificationSaveDirectory;
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.checks.Add(label);};
        try
        {
            // Inactive before AddComponent: runtime Awake cannot touch a real player's disk.
            host=new GameObject("Isolated title save runtime");host.SetActive(false);var rt=host.AddComponent<ChapterOneRuntime>();
            typeof(ChapterOneRuntime).GetField("SaveDirectoryOverride",Flags).SetValue(rt,Path.Combine(dir,"Save"));rt.EnsureInitialized();
            check(rt.SavePath.StartsWith(dir,StringComparison.OrdinalIgnoreCase),"save root is isolated");
            var profile=PrologueProfile.Read("嘴硬心软");profile.name="存档测试";profile.self="我";profile.callMe="你";check(rt.CompletePrologue(profile),"initial fixture saved");
            rt.Sim.S.money=23456;check(rt.SaveSlot(2),"manual slot created");rt.Sim.S.money=34567;check(rt.SaveNow(),"primary differs from manual slot");
            canvas=new GameObject("Title HUD fixture");var hud=canvas.AddComponent<CaseMenuHud>();hud.Build(null,_=>{},_=>{},_=>{},()=>{},()=>{});
            int starts=0,quits=0;hud.ConfigureTitleActions(()=>rt,()=>starts++,()=>quits++);
            Func<string,Button> button=name=>Array.Find(canvas.GetComponentsInChildren<Button>(true),b=>b.name==name&&b.gameObject.activeInHierarchy);
            button("QuitGame").onClick.Invoke();check(quits==1,"exit button reaches independent quit callback without real OS quit");
            button("ReadArchives").onClick.Invoke();check(hud.IsArchiveOpen,"title load entry opens modal");
            check(!button("Archive6").interactable,"empty slot disabled");check(!button("LoadSelected").interactable,"load needs an explicit slot choice");
            button("Archive2").onClick.Invoke();check(starts==0&&rt.Sim.S.money==34567,"selection alone does not replace current save");
            button("LoadSelected").onClick.Invoke();check(starts==1&&!hud.IsArchiveOpen&&rt.Sim.S.money==23456,"confirmation reloads actual manual slot before starting");
            check(rt.ReloadSave()&&rt.Sim.S.money==23456,"selected slot persisted as current primary");
            for(int slot=2;slot<=6;slot++) { rt.Sim.S.money=20000+slot;check(rt.SaveSlot(slot),"manual slot "+slot+" saves"); }
            for(int slot=2;slot<=6;slot++) check(rt.LoadArchive(slot)&&rt.Sim.S.money==20000+slot,"manual slot "+slot+" restores independent progress");
            rt.Sim.S.money=98765;check(rt.SaveSlot(6),"slot backup created");
            string slotPath=Path.Combine(dir,"Save","slots","6","chapter-one.sav");
            File.Move(slotPath,slotPath+".held");
            File.SetLastWriteTime(slotPath+".bak",new DateTime(2020,4,3,12,34,0));
            check(rt.ArchiveDescription(6).Contains("04-03 12:34"),"backup-only slot displays backup timestamp");
            check(rt.LoadArchive(6)&&rt.Sim.S.money==20006,"backup-only slot restores preserved progress");
            hud.SetChromeAlpha(0);var group=canvas.transform.Find("CaseMenuOverlay/TitleActions").GetComponent<CanvasGroup>();check(!group.blocksRaycasts&&!group.interactable&&group.alpha==0,"hidden title controls cannot intercept desktop input");
            hud.OpenArchives();check(!hud.IsArchiveOpen,"hidden title rejects archive opening");hud.SetChromeAlpha(1);check(group.interactable&&group.blocksRaycasts,"title controls return after shutdown");
            hud.ShowSettings(.8f,false,false);check(!group.blocksRaycasts,"settings modal blocks underlying title actions");hud.HideSettings();check(group.blocksRaycasts,"closing settings restores title controls");
            hud.ConfigureTitleActions(()=>null,()=>starts++,()=>quits++);hud.OpenArchives();check(!hud.IsArchiveOpen&&starts==1,"preload not ready safely rejects load");
        }
        catch(Exception e){report.status="failed";report.error=e.ToString();}
        finally{if(canvas!=null)UnityEngine.Object.DestroyImmediate(canvas);if(host!=null)UnityEngine.Object.DestroyImmediate(host);ChapterOneRuntime.VerificationSaveDirectory=before;File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(report,true));}
        Debug.Log("TitleArchivesQa "+report.status+": "+dir);if(report.status!="passed")throw new Exception(report.error);
    }
}
#endif
