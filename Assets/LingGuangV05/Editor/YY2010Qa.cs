#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.OS;
using UnityEditor;
using UnityEngine;
using LingGuangV05.Desktop.Apps;

namespace LingGuangV05.Editor
{
    public static class YY2010Qa
    {
        [Serializable] public sealed class Check { public string name; public bool passed; public string detail; }
        [Serializable] public sealed class Report { public bool completed; public int passed,failed; public List<Check> checks=new List<Check>(); }
        public static Report Result {get;private set;}
        public static bool Running {get;private set;}
        static readonly Queue<Action> actions=new Queue<Action>();
        static YY2010ShellController shell;
        static ChapterOneDesktopBridge chat;
        static MessagingApp manager;
        static string path;
        static double next;
        static int before;
        public static void Start()
        {
            if(Running||!EditorApplication.isPlaying)throw new InvalidOperationException("Start YY2010 QA in Main Play Mode.");
            shell=UnityEngine.Object.FindFirstObjectByType<YY2010ShellController>();
            if(shell==null)throw new InvalidOperationException("YY2010 shell is not installed.");
            chat=shell.ChatBinding;manager=shell.Manager;
            Result=new Report();actions.Clear();
            path=Path.Combine(Application.dataPath,"../Evidence/YY2010/play-qa.json");
            CheckNow("real native message backend",manager!=null&&chat.IsNativeChat);
            CheckNow("two separate native windows",shell.FriendsWindow!=null&&shell.FriendsWindow!=chat.Window);
            CheckNow("panda avatar is a project Sprite",shell.PandaAvatar!=null&&AssetDatabase.GetAssetPath(shell.PandaAvatar).EndsWith("yy-panda-red-scarf.png",StringComparison.Ordinal));
            chat.CloseApp();shell.CloseBuddy();
            actions.Enqueue(()=>ChapterOnePlayQa.Click(chat.DesktopShortcutRect));
            actions.Enqueue(()=>
            {
                CheckNow("desktop icon opens buddy list",shell.FriendsWindow.isOn);
                CheckNow("buddy launcher does not fake chat view",!chat.IsOpen);
                CheckNow("native contact list exists",Contacts().Length>0);
                CheckNow("panda on contact row",Contacts().Any(x=>x.coverImage!=null&&x.coverImage.sprite==shell.PandaAvatar));
                shell.SearchInput.text="不会匹配的名字";
            });
            actions.Enqueue(()=>
            {
                CheckNow("search hides nonmatching native contacts",Contacts().All(x=>!x.gameObject.activeSelf));
                shell.SearchInput.text="老周";
            });
            actions.Enqueue(()=>
            {
                var contact=Contacts().First(x=>x.name=="老周");
                CheckNow("search finds Lao Zhou",contact.gameObject.activeInHierarchy);
                ChapterOnePlayQa.Click(contact.transform as RectTransform);
                ChapterOnePlayQa.Click(contact.transform as RectTransform);
            });
            actions.Enqueue(()=>
            {
                CheckNow("doubleclick opens native chat",chat.IsOpen&&chat.NativeChat.IsReady);
                CheckNow("native conversation avatar uses panda",manager.selectedLayout!=null&&manager.selectedLayout.individualImage.sprite==shell.PandaAvatar);
                CheckNow("native send method retained",shell.SendChatButton!=null&&Enumerable.Range(0,shell.SendChatButton.onClick.GetPersistentEventCount()).Any(i=>shell.SendChatButton.onClick.GetPersistentMethodName(i)=="CreateMessageFromInput"));
                var captions=shell.SendChatButton.GetComponentsInChildren<TMPro.TMP_Text>(true);
                foreach(var caption in captions)caption.ForceMeshUpdate(true);
                CheckNow("send caption visible",shell.SendChatButton.enableText&&captions.Any(x=>x.gameObject.activeInHierarchy&&x.textInfo.characterCount>0));
                before=Messages().Length;
                ChapterOnePlayQa.Click(manager.messageInput.transform as RectTransform);
                manager.messageInput.text="电费怎么交？";
                ChapterOnePlayQa.Click(shell.SendChatButton.transform as RectTransform);
                CheckNow("original native send produces self record",Messages().Length==before+1);
                next=EditorApplication.timeSinceStartup+2.5;
            });
            actions.Enqueue(()=>
            {
                CheckNow("native Lao Zhou reply received",Messages().Length>=before+2);
                CheckNow("classic author timestamps occupy full native rows",Messages().All(x=>x.timeText.rectTransform.rect.width>200&&x.timeText.textInfo.characterCount>0));
                CheckNow("reply retains actual game guidance",Messages().Any(x=>x.contentText.text.Contains("家庭")));
                manager.messageInput.text="";
                ChapterOnePlayQa.Click(shell.EmojiButton.transform as RectTransform);
                CheckNow("toolbar emoticon inserts native input text",!string.IsNullOrEmpty(manager.messageInput.text));
                if(shell.HistoryScroll!=null)shell.HistoryScroll.verticalNormalizedPosition=0;
                ChapterOnePlayQa.Click(shell.HistoryButton.transform as RectTransform);
            });
            actions.Enqueue(()=>
            {
                CheckNow("history action scrolls actual native records to top",shell.HistoryScroll!=null&&shell.HistoryScroll.verticalNormalizedPosition>=.99f);
                manager.messageInput.text="显卡在哪里买？";before=Messages().Length;
                ChapterOnePlayQa.Click(shell.SendChatButton.transform as RectTransform);
                chat.CloseApp();next=EditorApplication.timeSinceStartup+2.5;
            });
            actions.Enqueue(()=>
            {
                CheckNow("reply queue survives chat closing",Messages().Length>=before+2);
                shell.OpenChat();
                // Native window + conversation animations finish asynchronously;
                // include the bounded post-animation readability recovery interval.
                next=EditorApplication.timeSinceStartup+1.6;
            });
            actions.Enqueue(()=>
            {
                CheckNow("reopen retains conversation",chat.IsOpen&&Messages().Length>=before+2);
                CheckNow("reopened native history visibly renders",Messages().Any(x=>x.contentText.canvasRenderer.GetInheritedAlpha()>.9f&&!x.contentText.canvasRenderer.cull&&x.contentText.textInfo.characterCount>0));
                CheckNow("native conversation animation ends at readable scale",Mathf.Abs(manager.selectedLayout.transform.localScale.x-1)<.02f&&Mathf.Abs(manager.selectedLayout.transform.localScale.y-1)<.02f);
                shell.SelectTab("groups");CheckNow("groups tab selects without inventing contacts",shell.ActiveTab=="groups");
                shell.SelectTab("friends");shell.SearchInput.text="";
                CheckNow("friends restore after tab switch",Contacts().Any(x=>x.gameObject.activeSelf));
                CheckNow("no selfdraw legacy dashboard",chat.Window.GetComponentsInChildren<LingGuangV05.UI.ChapterOneApp>(true).Length==0);
            });
            Running=true;next=EditorApplication.timeSinceStartup+1.4;Save();
            EditorApplication.update+=Advance;EditorApplication.playModeStateChanged+=Mode;
        }
        static ChatItemPreset[] Contacts()=>shell.FriendList.GetComponentsInChildren<ChatItemPreset>(true);
        static ChatMessagePreset[] Messages()=>manager.createdLayoutPresets.Count==0?Array.Empty<ChatMessagePreset>():manager.createdLayoutPresets[0].GetComponentsInChildren<ChatMessagePreset>(true);
        static void CheckNow(string name,bool pass,string detail=""){Result.checks.Add(new Check{name=name,passed=pass,detail=detail});if(pass)Result.passed++;else Result.failed++;}
        static void Advance()
        {
            if(!EditorApplication.isPlaying){if(actions.Count>0)CheckNow("QA interrupted",false);Finish();return;}
            if(EditorApplication.timeSinceStartup<next)return;
            if(actions.Count==0){Finish();return;}
            next=EditorApplication.timeSinceStartup+.8;
            try{actions.Dequeue()();}catch(Exception error){CheckNow("QA step exception",false,error.ToString());}
        }
        static void Mode(PlayModeStateChange state){if(Running&&state==PlayModeStateChange.ExitingPlayMode){if(actions.Count>0)CheckNow("QA interrupted",false);Finish();}}
        static void Save(){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(Result,true));}
        static void Finish(){EditorApplication.update-=Advance;EditorApplication.playModeStateChanged-=Mode;Running=false;Result.completed=actions.Count==0;Save();Debug.Log("[YY2010 QA] "+Result.passed+" pass / "+Result.failed+" fail; completed="+Result.completed);}
    }
}
#endif
