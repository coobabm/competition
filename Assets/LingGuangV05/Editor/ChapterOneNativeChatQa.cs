#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LingGuangV05.Desktop;
using LingGuangV05.UI;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace LingGuangV05.Editor
{
    public static class ChapterOneNativeChatQa
    {
        [Serializable] public sealed class Check { public string name; public bool passed; public string detail; }
        [Serializable] public sealed class Report { public bool completed; public int passed,failed; public List<Check> checks=new List<Check>(); }
        public static bool Running { get; private set; }
        public static Report Result { get; private set; }
        static readonly Queue<Action> steps=new Queue<Action>();
        static ChapterOneDesktopBridge bridge;
        static MessagingManager manager;
        static ButtonManager send;
        static double next,gameTime;
        static int before;
        static string path;
        public static void Start()
        {
            if(Running || !EditorApplication.isPlaying) throw new InvalidOperationException("Open Main Play Mode before native chat QA.");
            var router=UnityEngine.Object.FindFirstObjectByType<ChapterOneDesktopRouter>();
            bridge=router.Get("yy");
            if(bridge.NativeChat==null) throw new InvalidOperationException("YY does not have the native chat adapter.");
            manager=bridge.NativeChat.Manager;
            Result=new Report(); steps.Clear();
            path=Path.Combine(Application.dataPath,"../Evidence/LingGuang-ChapterOne/native-chat-qa.json");
            CheckNow("native adapter instead of selfdraw app",bridge.IsNative && bridge.App==null);
            CheckNow("native MessagingManager on original window",manager!=null && manager.gameObject==bridge.Window.gameObject);
            CheckNow("no selfdraw chat components",bridge.Window.GetComponentsInChildren<ChapterOneApp>(true).Length==0);
            CheckNow("native panel controller restored",bridge.Window.GetComponent<WindowPanelManager>()!=null);
            CheckNow("native messaging history not redirected into old user files",!manager.saveMessageHistory);
            steps.Enqueue(()=>{ ChapterOnePlayQa.Click(bridge.DesktopShortcutRect); });
            steps.Enqueue(()=>
            {
                CheckNow("YY launcher opens native window",bridge.IsOpen);
                CheckNow("native contact and layout initialized",bridge.NativeChat.IsReady && manager.createdLayoutPresets.Count>0 && manager.selectedLayout!=null);
                CheckNow("Lao Zhou contact uses native list",manager.chatList.Any(x=>x.chatTitle=="老周" || x.individualName=="老周"));
                CheckNow("native input is present",manager.messageInput!=null && manager.messageInput.gameObject.activeInHierarchy);
                CheckNow("removed tutorial intro stays absent",!Messages().Any(x=>x.contentText!=null && x.contentText.text.StartsWith("我是老周。先去邮件里标注")));
                send=bridge.Window.GetComponentsInChildren<ButtonManager>(true).FirstOrDefault(x=>Enumerable.Range(0,x.onClick.GetPersistentEventCount()).Any(i=>x.onClick.GetPersistentMethodName(i)=="CreateMessageFromInput" && x.onClick.GetPersistentTarget(i)==manager));
                CheckNow("original send button wired to native manager",send!=null);
            });
            steps.Enqueue(()=>
            {
                before=Messages().Length;
                ChapterOnePlayQa.Click(manager.messageInput.transform as RectTransform);
                manager.messageInput.text="电费怎么交？";
                ChapterOnePlayQa.Click(send.transform as RectTransform);
                CheckNow("native send creates player bubble",Messages().Length>=before+1);
                next=EditorApplication.timeSinceStartup+3;
            });
            steps.Enqueue(()=>
            {
                CheckNow("Lao Zhou reply arrives through native bubble",Messages().Length>=before+2);
                CheckNow("reply points to household electricity",Messages().Any(x=>x.contentText!=null && x.contentText.text.Contains("家庭")));
                before=Messages().Length; gameTime=bridge.Runtime.Sim.S.gameSeconds;
                manager.messageInput.text="显卡在哪里买？";
                ChapterOnePlayQa.Click(send.transform as RectTransform);
                bridge.CloseApp(); next=EditorApplication.timeSinceStartup+3;
            });
            steps.Enqueue(()=>
            {
                CheckNow("closing chat keeps game running",bridge.Runtime.Sim.S.gameSeconds>gameTime);
                CheckNow("reply survives closed window",Messages().Length>=before+2);
                bridge.OpenApp();
            });
            steps.Enqueue(()=>
            {
                CheckNow("reopen retains native conversation",Messages().Length>=before+2);
                CheckNow("hardware reply points to Xunbao",Messages().Any(x=>x.contentText!=null && x.contentText.text.Contains("寻宝")));
                before=Messages().Length; manager.messageInput.text="";
                ChapterOnePlayQa.Click(send.transform as RectTransform);
            });
            steps.Enqueue(()=>
            {
                CheckNow("empty send adds no messages",Messages().Length==before);
                CheckNow("native layout contains no dashboard root",!bridge.Window.GetComponentsInChildren<Transform>(true).Any(x=>x.name=="LingGuangAppRoot"));
                CheckNow("native input still interactive",manager.messageInput.interactable);
            });
            Running=true; next=EditorApplication.timeSinceStartup+.8; Save();
            EditorApplication.update+=Advance; EditorApplication.playModeStateChanged+=ModeChanged;
        }
        static ChatMessagePreset[] Messages() => manager.createdLayoutPresets.Count==0 ? Array.Empty<ChatMessagePreset>() : manager.createdLayoutPresets[0].GetComponentsInChildren<ChatMessagePreset>(true);
        static void CheckNow(string name,bool passed,string detail="") { Result.checks.Add(new Check{name=name,passed=passed,detail=detail}); if(passed)Result.passed++;else Result.failed++; }
        static void Advance()
        {
            if(!EditorApplication.isPlaying) { if(steps.Count>0)CheckNow("QA interrupted",false);Finish();return; }
            if(EditorApplication.timeSinceStartup<next)return;
            if(steps.Count==0){Finish();return;}
            next=EditorApplication.timeSinceStartup+.75;
            try{steps.Dequeue()();}catch(Exception e){CheckNow("QA step exception",false,e.ToString());}
        }
        static void ModeChanged(PlayModeStateChange mode) { if(Running && mode==PlayModeStateChange.ExitingPlayMode){if(steps.Count>0)CheckNow("QA interrupted",false);Finish();} }
        static void Save(){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(Result,true));}
        static void Finish(){EditorApplication.update-=Advance;EditorApplication.playModeStateChanged-=ModeChanged;Running=false;Result.completed=steps.Count==0;Save();Debug.Log("[Native YY QA] completed="+Result.completed+" pass="+Result.passed+" fail="+Result.failed);}
    }
}
#endif
