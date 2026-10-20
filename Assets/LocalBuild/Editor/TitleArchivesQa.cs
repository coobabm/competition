#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
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
            string saveRoot=LingGuangV05.Runtime.Persistence.VerificationSavePaths.Root??Path.Combine(dir,"Save");
            check(rt.SavePath.StartsWith(saveRoot+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"save root is isolated");
            canvas=new GameObject("Title HUD fixture");var hud=canvas.AddComponent<CaseMenuHud>();hud.Build(null,_=>{},_=>{},_=>{},()=>{},()=>{});
            int starts=0,quits=0;hud.ConfigureTitleActions(()=>rt,()=>starts++,()=>quits++);
            Func<string,Button> button=name=>Array.Find(canvas.GetComponentsInChildren<Button>(true),b=>b.name==name&&b.gameObject.activeInHierarchy);
            check(!rt.HasAnyArchive,"fresh isolated profile has no saves");
            hud.RequestPowerOn();check(starts==1&&!hud.IsArchiveOpen,"power with no save enters directly");starts=0;
            button("ReadArchives").onClick.Invoke();check(hud.IsArchiveOpen&&starts==0,"load button with no save shows empty archive panel");
            button("CloseArchives").onClick.Invoke();check(!hud.IsArchiveOpen&&starts==0,"cancel without selection does not boot");
            var profile=PrologueProfile.Read("嘴硬心软");profile.name="存档测试";profile.self="我";profile.callMe="你";check(rt.CompletePrologue(profile),"initial fixture saved");
            rt.Sim.S.money=23456;check(rt.SaveSlot(2),"manual slot created");rt.Sim.S.money=34567;check(rt.SaveNow(),"primary differs from manual slot");
            button("QuitGame").onClick.Invoke();check(quits==1,"exit button reaches independent quit callback without real OS quit");
            button("ReadArchives").onClick.Invoke();check(hud.IsArchiveOpen,"title load entry opens modal");
            check(!Array.Exists(canvas.GetComponentsInChildren<Button>(true),b=>b.name=="Archive6"&&b.gameObject.activeInHierarchy),"empty manual slots are omitted; new-save card replaces empty slots");check(!button("LoadSelected").interactable,"load needs an explicit slot choice");
            button("Archive2").onClick.Invoke();check(starts==0&&rt.Sim.S.money==34567,"selection alone does not replace current save");
            button("ArchivePage").onClick.Invoke();check(!button("LoadSelected").interactable,"changing pages clears an unread hidden selection");
            button("ArchivePage").onClick.Invoke();button("Archive2").onClick.Invoke();
            var blocked=typeof(ChapterOneRuntime).GetField("persistenceBlocked",Flags);blocked.SetValue(rt,true);
            string statusBefore=rt.SaveStatus;
            button("LoadSelected").onClick.Invoke();check(starts==0&&hud.IsArchiveOpen&&rt.Sim.S.money==34567,"failed load keeps panel and current progress");
            check(!string.IsNullOrWhiteSpace(rt.SaveStatus)&&rt.SaveStatus!=statusBefore,"blocked load replaces stale success with an explicit failure");blocked.SetValue(rt,false);
            button("LoadSelected").onClick.Invoke();check(starts==0&&hud.IsArchiveOpen&&rt.Sim.S.money==23456,"successful load waits for explicit close before boot");
            button("ArchivePage").onClick.Invoke();check(hud.IsArchiveOpen&&button("Archive1")!=null&&!button("LoadSelected").interactable&&starts==0,"last page contains autosave and preserves loaded selection");
            button("CloseArchives").onClick.Invoke();check(starts==1&&!hud.IsArchiveOpen,"closing loaded panel starts exactly once");
            hud.CloseArchiveByPlayer();check(starts==1,"repeated close cannot start twice");
            check(rt.ReloadSave()&&rt.Sim.S.money==23456,"selected slot persisted as current primary");
            hud.OpenArchives();int empty=rt.FirstEmptyManualArchive();string unchanged=Hash(rt.SavePath);
            button("CreateSave").onClick.Invoke();
            check(!rt.ArchiveExists(empty)&&Hash(rt.SavePath)==unchanged&&starts==1,"new manual card only enters name edit; no disk write or new game");
            var name=Array.Find(canvas.GetComponentsInChildren<TMPro.TMP_InputField>(true),f=>f.gameObject.activeInHierarchy);
            check(name!=null,"new manual card exposes name input");name.text="我的训练记录";
            button("CommitNewSave").onClick.Invoke();
            check(rt.ArchiveExists(empty)&&rt.DescribeArchiveCard(empty).name=="我的训练记录"&&Hash(rt.SavePath)==unchanged&&starts==1,"save new card copies current progress and name without resetting primary or booting");
            hud.CloseArchiveByPlayer();

            hud.RequestPowerOn();check(hud.IsArchiveOpen&&starts==1,"power with saves opens chooser without boot");
            hud.RequestPowerOn();check(hud.IsArchiveOpen&&starts==1,"repeat power is blocked by chooser");hud.CloseArchiveByPlayer();check(starts==1,"cancel power chooser stays on title");
            for(int row=0;row<6;row++)check(ChapterOneRuntime.ArchiveDisplaySlot(row)==(row==5?1:row+2),"UI row maps to preserved disk slot "+row);
            check(ChapterOneRuntime.AutosaveSeconds==300,"system autosave stays at five minutes");
            for(int slot=2;slot<=6;slot++) { rt.Sim.S.money=20000+slot;check(rt.SaveSlot(slot),"manual slot "+slot+" saves"); }
            for(int slot=2;slot<=6;slot++) check(rt.LoadArchive(slot)&&rt.Sim.S.money==20000+slot,"manual slot "+slot+" restores independent progress");
            rt.Sim.S.money=98765;check(rt.SaveSlot(6),"slot backup created");
            string slotPath=Path.Combine(saveRoot,"slots","6","chapter-one.sav");
            File.Move(slotPath,slotPath+".held");
            File.SetLastWriteTime(slotPath+".bak",new DateTime(2020,4,3,12,34,0));
            check(rt.ArchiveDescription(6).Contains("04-03 12:34"),"backup-only slot displays backup timestamp");
            check(rt.LoadArchive(6)&&rt.Sim.S.money==20006,"backup-only slot restores preserved progress");
            hud.SetChromeAlpha(0);var group=canvas.transform.Find("CaseMenuOverlay/TitleActions").GetComponent<CanvasGroup>();check(!group.blocksRaycasts&&!group.interactable&&group.alpha==0,"hidden title controls cannot intercept desktop input");
            hud.OpenArchives();check(!hud.IsArchiveOpen,"hidden title rejects archive opening");hud.SetChromeAlpha(1);check(group.interactable&&group.blocksRaycasts,"title controls return after shutdown");
            hud.ShowSettings(.8f,false,false);check(!group.blocksRaycasts,"settings modal blocks underlying title actions");hud.HideSettings();check(group.blocksRaycasts,"closing settings restores title controls");
            var elapsed=typeof(ChapterOneRuntime).GetField("sinceSave",Flags);
            var input=typeof(ChapterOneRuntime).GetField("lastDesktopInput",BindingFlags.Static|BindingFlags.NonPublic);
            object oldInput=input.GetValue(null);
            try {
                int writes=0;rt.Saving+=()=>writes++;
                input.SetValue(null,Time.realtimeSinceStartupAsDouble-3);
                rt.MarkDirty();elapsed.SetValue(rt,299.9f);check(!rt.TryAutosave()&&writes==0,"autosave does not write before 300 seconds");
                elapsed.SetValue(rt,300f);check(rt.TryAutosave()&&writes==1,"autosave writes once at five minutes when idle");
                check(!rt.TryAutosave()&&writes==1,"autosave does not write every following frame");
            } finally {input.SetValue(null,oldInput);}
            hud.ConfigureTitleActions(()=>null,()=>starts++,()=>quits++);hud.OpenArchives();check(!hud.IsArchiveOpen&&starts==1,"preload not ready safely rejects load");
            hud.ConfigureTitleActions(()=>rt,()=>starts++,()=>quits++);
            string manualPath=Path.Combine(saveRoot,"slots","2","chapter-one.sav");
            string manualHash=Hash(manualPath),primaryHash=Hash(rt.SavePath);double priorMoney=rt.Sim.S.money;
            hud.OpenArchives();button("StartNewGame").onClick.Invoke();
            check(hud.IsArchiveOpen&&starts==1&&rt.Sim.S.money==priorMoney&&Hash(rt.SavePath)==primaryHash,"new game first click only confirms; existing primary unchanged");
            // A locked primary is a real disk failure: ArchiveForReset must fail before a new session starts.
            using(var locked=File.Open(rt.SavePath,FileMode.Open,FileAccess.Read,FileShare.None))
            {
                button("StartNewGame").onClick.Invoke();
                check(hud.IsArchiveOpen&&starts==1&&rt.Sim.S.money==priorMoney&&!rt.Sim.InPrologue,"new game backup failure leaves current session and modal intact");
            }
            check(Hash(rt.SavePath)==primaryHash&&Hash(manualPath)==manualHash,"failed new game preserves primary and manual file hashes");
            hud.CloseArchiveByPlayer();hud.OpenArchives();
            button("StartNewGame").onClick.Invoke();button("StartNewGame").onClick.Invoke();
            check(rt.Sim.InPrologue&&!hud.IsArchiveOpen&&starts==2,"confirmed new game closes modal and starts opening exactly once");
            check(Hash(manualPath)==manualHash,"new game preserves existing manual slot bytes");
            bool primaryArchived=false;
            foreach(string archived in Directory.GetFiles(saveRoot,"chapter-one.sav.reset-*"))if(Hash(archived)==primaryHash)primaryArchived=true;
            check(primaryArchived,"old primary retained byte-for-byte in reset archive");
            hud.CloseArchiveByPlayer();check(starts==2,"closing new-game modal again cannot start twice");
        }
        catch(Exception e){report.status="failed";report.error=e.ToString();}
        finally{if(canvas!=null)UnityEngine.Object.DestroyImmediate(canvas);if(host!=null)UnityEngine.Object.DestroyImmediate(host);ChapterOneRuntime.VerificationSaveDirectory=before;File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(report,true));}
        Debug.Log("TitleArchivesQa "+report.status+": "+dir);if(report.status!="passed")throw new Exception(report.error);
    }
    static string Hash(string path){using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream));}
}
#endif
