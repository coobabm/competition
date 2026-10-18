#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Embedded in SidebarLayoutChecks' isolated, live Main fixture; never loads a scene or writes player saves.
public static class LearningRecordsUiChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    public static readonly List<string> Checks=new List<string>();
    public static int AssertionCount { get; private set; }
    static T Get<T>(object value,string field)=>(T)value.GetType().GetField(field,F).GetValue(value);
    static void Set(object value,string field,object data)=>value.GetType().GetField(field,F).SetValue(value,data);
    static object Call(object value,string method,params object[] args)=>value.GetType().GetMethod(method,F).Invoke(value,args);
    static void Need(bool ok,string message){AssertionCount++;if(!ok)throw new InvalidOperationException("Learning UI: "+message);}
    static void Done(string message){Checks.Add(message);Debug.Log("Learning UI: "+message);}
    static string Token(string prefix,int index)=>prefix+index.ToString("000");
    static string Label(Button button)=>button.GetComponentInChildren<TMP_Text>(true).text;
    static IEnumerable<object> Layout(){yield return null;Canvas.ForceUpdateCanvases();yield return null;}
    static IEnumerable<object> Ready(WindowManager window)
    {
        double until=EditorApplication.timeSinceStartup+5;int stable=0;
        while(stable<3)
        {
            bool visible=window.isOn&&window.gameObject.activeInHierarchy;
            foreach(var group in window.GetComponentsInParent<CanvasGroup>(true))visible&=group.alpha>=.95f&&group.interactable&&group.blocksRaycasts;
            var animator=window.windowAnimator;
            visible&=animator==null||!animator.isActiveAndEnabled||(!animator.IsInTransition(0)&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>=1);
            stable=visible?stable+1:0;
            Need(EditorApplication.timeSinceStartup<until,"window did not naturally become interactive: "+window.name);
            yield return null;
        }
        Canvas.ForceUpdateCanvases();
    }
    // Scroll setup is deterministic; each selection and page turn still traverses the actual LCD click callback.
    static void Bring(RectTransform target)
    {
        Canvas.ForceUpdateCanvases();var scroll=target.GetComponentInParent<ScrollRect>();if(scroll==null||scroll.content==target)return;
        scroll.StopMovement();float overflow=Mathf.Max(0,scroll.content.rect.height-scroll.viewport.rect.height);
        var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.content,target);
        float top=Mathf.Clamp(-bounds.center.y-scroll.viewport.rect.height*.5f,0,overflow);
        scroll.verticalNormalizedPosition=overflow>0?1-top/overflow:1;
        Canvas.ForceUpdateCanvases();
    }
    static XgFilesRow Row(XgFilesWindow files,string id)
    {
        var row=files.client.GetComponentsInChildren<XgFilesRow>().FirstOrDefault(x=>x.fileId==id);
        Need(row!=null,"file row exists: "+id);return row;
    }
    static IEnumerable<object> Pick(XgFilesWindow files,string id,Action<RectTransform> click,bool folder=false)
    {
        var row=Row(files,id);Bring((RectTransform)row.transform);foreach(var frame in Layout())yield return frame;
        click((RectTransform)row.transform);if(folder)click((RectTransform)row.transform);
        foreach(var frame in Layout())yield return frame;
        if(folder)Need(Get<string>(files,"folder")==id,"actual LCD double-click opened "+id);
    }
    static IEnumerable<object> Back(XgFilesWindow files,Action<RectTransform> click)
    {click((RectTransform)files.client.Find("Toolbar/Back"));foreach(var frame in Layout())yield return frame;Need(Get<string>(files,"folder")=="","back returns to root");}
    static Button NotePage(XgFilesWindow files,bool next,bool english)
    {
        string label=next?(english?"Next":"下一页"):(english?"Previous":"上一页");
        var rows=Get<RectTransform>(files,"rows");var button=rows.GetComponentsInChildren<Button>().FirstOrDefault(x=>Label(x)==label);
        Need(button!=null,"notes pager "+label);return button;
    }
    static XgSim Fixture(string prefix)
    {
        var sim=new XgSim();sim.BeginOpening();sim.S.stage=3;sim.S.teachingFlow=false;
        sim.S.unlocked.Add("train.curve");sim.S.unlocked.Add(XgSim.DataEntryNode);sim.S.unlocked.Add("lenet");
        sim.S.desksOpen.Add("mnist");sim.S.spiderIntroDone=true;sim.S.firstWordsSaid=true;
        sim.S.teachIntroSaid=sim.S.teachThirdSaid=sim.S.teachTreeSaid=sim.S.teachTrainSaid=true;
        sim.Profile.name=prefix=="LR"?"小灯":"新灯";
        for(int i=0;i<120;i++)
        {
            int day=i/40+1,second=i%40*60;
            sim.LearningClock=i==0?(Func<XgLearningTime>)null:()=>new XgLearningTime(20160100+day,second);
            var image=i==119?new XgCard { dataset="mnist",digit=3,seed=7,teachingImageKind="handwriting28",teachingImageKey=17,teachingImageKeys=new[]{17,23},teachingPosition="fixture-original-position" }:null;
            sim.RecordTeachingQuestion(Token(prefix,i)+" 第"+i+"题",Token(prefix,i)+" Question "+i,true,false,
                "原始解释","Original explanation",image==null?"sense":"mnist",image,"PLAYER_"+i.ToString("000"),i==0?"":"AI_ACTUAL_"+i.ToString("000"));
        }
        // Legacy elapsed time is deliberately nonzero; it must never become a fabricated calendar timestamp.
        sim.S.askedLog[0].at=987654;
        return sim;
    }
    static void Bind(XingGuangController ctl,XgFilesWindow files,XgYyTalk talk,XgSim sim,bool english)
    {
        sim.English=english;
        typeof(XingGuangController).GetProperty("Sim",F).GetSetMethod(true).Invoke(ctl,new object[]{sim});
        ctl.View.Bind(ctl);ctl.View.Refresh(true);files.Bind(sim);Call(talk,"Bind",sim);
    }
    static int[] Expected(int page)=>Enumerable.Range(Math.Max(0,120-(page+1)*50),Math.Min(50,120-page*50)).Reverse().ToArray();
    static void CheckPrompt(string text,string prefix,int i,bool english)
    {
        Need(text.Contains(Token(prefix,i)),"retained prompt "+Token(prefix,i));
        Need(text.Contains(english?"Question "+i:"第"+i+"题"),"localized authored prompt "+Token(prefix,i));
        if(i>0)Need(text.Contains("AI_ACTUAL_"+i.ToString("000")),"actual AI answer retained for "+i);
        else Need(text.Contains(english?"Not recorded":"未记录")&&!text.Contains("AI_ACTUAL_000"),"unknown legacy time and missing AI answer stay unknown");
    }
    static IEnumerable<object> Notes(XgFilesWindow files,XgSim sim,bool english,Action<RectTransform> click,Action<string> capture,string suffix)
    {
        foreach(var frame in Pick(files,"notes",click,true))yield return frame;
        var seen=new HashSet<long>();
        for(int page=0;page<3;page++)
        {
            var rows=files.client.GetComponentsInChildren<XgFilesRow>().Where(x=>x.fileId.StartsWith("notes:",StringComparison.Ordinal)).ToArray();
            var expected=Expected(page);Need(rows.Length==expected.Length,"notes page has exact 50/50/20 rows");
            for(int j=0;j<rows.Length;j++)
            {
                var row=rows[j];long id=long.Parse(row.fileId.Substring(6));Need(seen.Add(id),"notes pagination never duplicates "+id);
                Bring((RectTransform)row.transform);foreach(var frame in Layout())yield return frame;click((RectTransform)row.transform);
                var preview=Get<TMP_Text>(files,"preview");CheckPrompt(preview.text,"LR",expected[j],english);
                Need(preview.overflowMode==TextOverflowModes.Overflow,"note preview retains scrollable full text");
                if(expected[j]==119)
                {
                    var note=sim.LearningTeachingNote(id);var raw=Get<RawImage>(files,"previewImage");
                    Need(raw.gameObject.activeSelf&&raw.texture!=null,"actual original image renders");
                    Need(raw.texture==XgTeachingPictures.ForNote(note),"preview uses original note image reference");
                    Need(note.teachingImageKey==17&&note.teachingImageKeys.SequenceEqual(new[]{17,23})&&note.teachingPosition=="fixture-original-position","image identifiers and position unchanged");
                    foreach(var frame in Layout())yield return frame;capture("learning-"+suffix+"-original-image.png");
                }
            }
            foreach(var frame in Layout())yield return frame;capture("learning-"+suffix+"-notes-page"+(page+1)+".png");
            if(page<2){var next=NotePage(files,true,english);Bring((RectTransform)next.transform);foreach(var frame in Layout())yield return frame;click((RectTransform)next.transform);foreach(var frame in Layout())yield return frame;}
        }
        Need(seen.Count==120&&Get<int>(files,"recordPageOffset")==100,"all 120 notes reachable over three actual pages");
        Done(suffix+": all 120 notes selected through LCD; unknown date, actual answers and original image preserved");
    }
    static IEnumerable<object> Csv(XgFilesWindow files,XgSim sim,string kind,bool english,Action<RectTransform> click,Action<string> capture,string suffix)
    {
        foreach(var frame in Pick(files,kind,click))yield return frame;
        var seen=new HashSet<string>();
        for(int page=0;page<3;page++)
        {
            var preview=Get<TMP_Text>(files,"preview");string text=preview.text;var expected=Expected(page);
            var tokens=Regex.Matches(text,@"LR\d{3}").Cast<Match>().Select(x=>x.Value).ToArray();
            Need(tokens.SequenceEqual(expected.Select(i=>Token("LR",i))),kind+" CSV exact stable order and 50/50/20 bounds");
            foreach(int i in expected){Need(seen.Add(Token("LR",i)),kind+" CSV each record once");CheckPrompt(text,"LR",i,english);}
            Need(Get<TMP_Text>(files,"recordPreviewPage").text==(page+1)+" / 3",kind+" real pager label");
            Need(Get<Button>(files,"recordPreviewPrevious").interactable==(page>0)&&Get<Button>(files,"recordPreviewNext").interactable==(page<2),kind+" pager limits");
            Need(preview.overflowMode==TextOverflowModes.Overflow&&text.Length<40000,"CSV TMP contains bounded page, no full-history preview or ellipsis");
            foreach(var frame in Layout())yield return frame;capture("learning-"+suffix+"-"+kind+"-page"+(page+1)+".png");
            if(page<2){click((RectTransform)Get<Button>(files,"recordPreviewNext").transform);foreach(var frame in Layout())yield return frame;}
        }
        Need(seen.Count==120&&sim.S.askedLog.Count==120&&sim.S.teachingNotebook.Count==120,"CSV pagination never trims saved history");
        Done(suffix+": "+kind+" CSV visits all 120 records with bounded 50-row TMP pages");
    }
    static List<Button> NotebookRows(YYChatView view)
    {var content=Get<RectTransform>(view,"learningContent");return content.GetComponentsInChildren<Button>().Where(x=>x.transform.parent==content&&x.gameObject.activeSelf).ToList();}
    static IEnumerable<object> Notebook(XingGuangController ctl,XgFilesWindow files,YYChatHub hub,YYChatView view,XgYyTalk talk,XgSim sim,bool english,Action<RectTransform> click,Action<string> capture,string suffix)
    {
        var window=Get<WindowManager>(view,"window");files.window.CloseWindow();ctl.Window.CloseWindow();
        view.EnsureReady();view.OnOpened(YYChatHub.LingGuangId);window.OpenWindow();window.FocusToWindow();
        foreach(var frame in Ready(window))yield return frame;
        Call(view,"RefreshLearningTools");click((RectTransform)Get<Button>(view,"notebookTool").transform);foreach(var frame in Layout())yield return frame;
        var seen=new HashSet<string>();
        for(int page=0;page<3;page++)
        {
            var rows=NotebookRows(view);var expected=Expected(page);Need(rows.Count==expected.Length,"YY notebook exact 50/50/20 rows");
            for(int j=0;j<rows.Count;j++){string label=Label(rows[j]);CheckPrompt(label,"LR",expected[j],english);Need(seen.Add(Token("LR",expected[j])),"YY notebook no duplicate rows");if(expected[j]>0)Need(label.Contains(sim.AiDisplayName+": AI_ACTUAL_"),"YY shows real AI name");}
            Need(Get<TMP_Text>(view,"learningPageLabel").text.Contains("120"),"YY full retained total");
            Bring((RectTransform)rows[rows.Count-1].transform);foreach(var frame in Layout())yield return frame;
            capture("learning-"+suffix+"-yy-page"+(page+1)+".png");
            if(page<2){click((RectTransform)Get<Button>(view,"learningNext").transform);foreach(var frame in Layout())yield return frame;}
        }
        Need(seen.Count==120,"all YY records reachable across three pages");
        var search=Get<TMP_InputField>(view,"learningSearch");
        // Actual input control receives focus; assigning text fires its real value-change handler, without submitting a chat.
        click((RectTransform)search.transform);search.text="LR001";foreach(var frame in Layout())yield return frame;
        Need(Get<int>(view,"learningOffset")==0&&NotebookRows(view).Count==1&&Label(NotebookRows(view)[0]).Contains("LR001"),"YY search reaches oldest known-date page and resets offset");
        capture("learning-"+suffix+"-yy-search-old.png");
        search.text="LR119";foreach(var frame in Layout())yield return frame;
        var imageRow=NotebookRows(view).Single();Bring((RectTransform)imageRow.transform);foreach(var frame in Layout())yield return frame;
        Need(!imageRow.interactable&&Label(imageRow).Contains(english?"With image":"含原图"),"image notebook row explicitly disabled for text retest");
        int messages=hub.Conversation(YYChatHub.LingGuangId).messages.Count,records=sim.S.askedLog.Count;
        click((RectTransform)imageRow.transform);
        var imageRecord=sim.QueryLearningRecords(new XgLearningQuery{text="LR119",limit=1}).records.Single();
        Need(!XgYyTalk.AskFromNotebook(imageRecord),"public retest guard rejects original-image record");
        var drawer=Get<RectTransform>(view,"learningDrawer");var random=drawer.GetComponentsInChildren<Button>().First(x=>Label(x)=="Random"||Label(x)=="抽一道");
        click((RectTransform)random.transform);foreach(var frame in Layout())yield return frame;
        Need(hub.Conversation(YYChatHub.LingGuangId).messages.Count==messages&&sim.S.askedLog.Count==records&&!talk.Thinking&&drawer.gameObject.activeSelf,"disabled row and image-only random query do not send, request LLM or fabricate a reply");
        capture("learning-"+suffix+"-yy-image-guard.png");
        search.text="";foreach(var frame in Layout())yield return frame;
        click((RectTransform)Get<Button>(view,"learningNext").transform);foreach(var frame in Layout())yield return frame;
        click((RectTransform)Get<Button>(view,"learningNext").transform);foreach(var frame in Layout())yield return frame;
        var other=Fixture("SW");Need(other.S.learningRecordSerial==sim.S.learningRecordSerial,"save-switch fixture intentionally has identical count and serial");
        Bind(ctl,files,talk,other,english);
        // Exercise the production refresh guard, not DrawLearningRows or a forced close/reopen that would hide stale ownership.
        Call(view,"RefreshLearningTools");foreach(var frame in Layout())yield return frame;
        Need(Get<int>(view,"learningOffset")==0,"same-size save switch resets YY page to first");
        var swapped=NotebookRows(view);Need(swapped.Count==50&&Label(swapped[0]).Contains("SW119")&&!swapped.Any(x=>Label(x).Contains("LR")),"same-count same-serial save switch redraws current records");
        Need(Label(swapped[0]).Contains(other.AiDisplayName+": AI_ACTUAL_119"),"new save actual name replaces old name");
        capture("learning-"+suffix+"-yy-switched.png");
        Call(view,"CloseLearningDrawer");window.CloseWindow();Bind(ctl,files,talk,sim,english);
        Done(suffix+": YY three pages, older search, original-image text guard and same-size save switch");
    }
    static IEnumerable<object> Provenance(XgFilesWindow files,YYChatView view,XgSim sim,bool english,Action<RectTransform> click,Action<string> capture,string suffix)
    {
        // Explicit record fixtures exercise native display only; no AI inference or generated human judgment.
        sim.RecordTeachingQuestion("C5_SOURCE_PLAYER","C5_SOURCE_PLAYER",true,true,"","","arith",null,"PLAYER_TRUE");
        sim.RecordTeachingQuestion("C5_SOURCE_LEGACY","C5_SOURCE_LEGACY",false,true,"","","arith",null,"LEGACY_RETAINED");
        var legacy=sim.S.askedLog.Last();var legacyNote=sim.LearningTeachingNote(legacy.teachingNoteId);
        legacy.answerSource="";legacyNote.answerSource="";
        sim.RecordAiAction("arith","C5_SOURCE_AI","C5_SOURCE_AI","Whack");
        var window=Get<WindowManager>(view,"window");files.window.CloseWindow();view.OnOpened(YYChatHub.LingGuangId);window.OpenWindow();window.FocusToWindow();
        foreach(var f in Ready(window))yield return f;
        Call(view,"RefreshLearningTools");click((RectTransform)Get<Button>(view,"notebookTool").transform);foreach(var f in Layout())yield return f;
        var search=Get<TMP_InputField>(view,"learningSearch");click((RectTransform)search.transform);search.text="C5_SOURCE";foreach(var f in Layout())yield return f;
        var rows=NotebookRows(view);Need(rows.Count==3,"native YY retains player, AI-action and old unknown rows");
        string ai=Label(rows.Single(x=>Label(x).Contains("C5_SOURCE_AI")));
        string player=Label(rows.Single(x=>Label(x).Contains("C5_SOURCE_PLAYER")));
        string old=Label(rows.Single(x=>Label(x).Contains("C5_SOURCE_LEGACY")));
        Need(ai.Contains(english?"not a player judgment":"非玩家判断")&&!ai.Contains(english?"You:":"你："),"actual bilingual YY AI row is not attributed to player");
        Need(player.Contains(english?"You:":"你：")&&player.Contains("PLAYER_TRUE"),"actual YY new manual answer remains player evidence");
        Need(old.Contains(english?"source unknown":"来源不明")&&old.Contains("LEGACY_RETAINED"),"actual YY old record retains text without inferred author");
        capture("learning-"+suffix+"-answer-provenance-yy.png");
        Call(view,"CloseLearningDrawer");window.CloseWindow();files.Show();files.window.FocusToWindow();foreach(var f in Ready(files.window))yield return f;
        foreach(var f in Pick(files,"asked",click))yield return f;
        string csv=Get<TMP_Text>(files,"preview").text;
        Need(csv.Contains(english?"Answer source":"答案来源")&&csv.Contains("ai_action")&&csv.Contains("C5_SOURCE_AI")&&csv.Contains("LEGACY_RETAINED"),"actual CSV exposes provenance and preserves old records");
        capture("learning-"+suffix+"-answer-provenance-csv.png");
        foreach(var f in Pick(files,"notes",click,true))yield return f;
        foreach(var f in Pick(files,"notes:"+legacyNote.id,click))yield return f;
        string preview=Get<TMP_Text>(files,"preview").text;
        Need(preview.Contains(english?"source unknown":"来源不明")&&preview.Contains("LEGACY_RETAINED"),"actual file note identifies unknown historical answer source");
        capture("learning-"+suffix+"-answer-provenance-old-note.png");
        Need(sim.S.teachingNotebook.All(n=>n.text!="C5_SOURCE_AI"),"AI action never enters player teaching notebook");
        Done(suffix+": actual YY/CSV/old-note provenance display; explicit fixtures, no text retest sent or model output fabricated");
    }
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> locale,Action<RectTransform> click,Action<string> capture)
    {
        Checks.Clear();AssertionCount=0;var runtime=ctl.runtime;
        Need(runtime!=null&&!runtime.useDiskSave&&!runtime.tickEnabled&&!runtime.TestMode,"isolated live-host Main required");
        Need(locale!=null&&click!=null&&capture!=null,"locale/LCD/capture callbacks required");
        var files=runtime.GetComponent<XgFilesWindow>();var hub=YYChatHub.Instance;var talk=ctl.GetComponent<XgYyTalk>();var view=hub?.View;
        Need(files!=null&&files.window!=null&&hub!=null&&hub.runtime==runtime&&view!=null&&talk!=null,"real files and YY components installed");
        Need(!talk.Thinking&&!(bool)talk.GetType().GetProperty("NightPlaying",F).GetValue(talk),"fixture starts without active model/story operation");
        var original=ctl.Sim;var originalYy=hub.S;var originalTalk=hub.LingGuangTalk;var originalBound=Get<XgSim>(talk,"bound");
        bool ctlEnabled=ctl.enabled,hubEnabled=hub.enabled,talkEnabled=talk.enabled;string house=JsonUtility.ToJson(runtime.Sim.S),tab=ctl.View.Tab;
        var yyWindow=Get<WindowManager>(view,"window");bool yyOpen=yyWindow.isOn,filesOpen=files.window.isOn,exeOpen=ctl.Window.isOn;
        ctl.enabled=false;hub.enabled=false;talk.enabled=false;
        try
        {
            var yy=new YYState { lingguangUnlocked=true,selected=YYChatHub.LingGuangId };
            foreach(var contact in YYChatHub.Contacts)yy.conversations.Add(new YYConversation { id=contact.id });
            typeof(YYChatHub).GetProperty("S",F).GetSetMethod(true).Invoke(hub,new object[]{yy});hub.LingGuangTalk=talk;
            for(int language=0;language<2;language++)
            {
                bool english=language==1;string suffix=english?"en":"zh";locale(english);
                var sim=Fixture("LR");Bind(ctl,files,talk,sim,english);ctl.Window.CloseWindow();yyWindow.CloseWindow();files.Show();files.window.FocusToWindow();
                foreach(var frame in Ready(files.window))yield return frame;
                Need(files.titleLabel.text==sim.AiDisplayName+(english?"'s files":"的资料"),"files title uses current save's actual AI name");
                Need(sim.QueryLearningRecords(new XgLearningQuery{limit=1}).records[0].gameDate==20160103,"fixture actually spans calendar days");
                foreach(var frame in Notes(files,sim,english,click,capture,suffix))yield return frame;
                // Swap while notes are on page three; selecting the same folder must start at page one.
                var replacement=Fixture("SW");Bind(ctl,files,talk,replacement,english);files.Show();foreach(var frame in Ready(files.window))yield return frame;
                foreach(var frame in Pick(files,"notes",click,true))yield return frame;
                Need(Get<int>(files,"recordPageOffset")==0&&Row(files,"notes:120").detail.Contains("SW119"),"files notes page resets on replacement save");
                Bind(ctl,files,talk,sim,english);files.Show();foreach(var frame in Ready(files.window))yield return frame;
                foreach(var frame in Csv(files,sim,"answers",english,click,capture,suffix))yield return frame;
                foreach(var frame in Csv(files,sim,"asked",english,click,capture,suffix))yield return frame;
                Bind(ctl,files,talk,replacement,english);files.Show();foreach(var frame in Ready(files.window))yield return frame;
                foreach(var frame in Pick(files,"asked",click))yield return frame;
                Need(Get<int>(files,"csvOffset")==0&&Get<TMP_Text>(files,"preview").text.Contains("SW119")&&!Get<TMP_Text>(files,"preview").text.Contains("LR"),"CSV page resets and old content disappears on save switch");
                Bind(ctl,files,talk,sim,english);
                foreach(var frame in Notebook(ctl,files,hub,view,talk,sim,english,click,capture,suffix))yield return frame;
                foreach(var frame in Provenance(files,view,sim,english,click,capture,suffix))yield return frame;
            }
            Debug.Log("LearningRecordsUiChecks PASS: "+AssertionCount+" assertions; isolated bilingual Main fixture. No text retest was sent and no LLM response was required.");
        }
        finally
        {
            Call(view,"CloseLearningDrawer");files.CancelDrag();files.window.CloseWindow();yyWindow.CloseWindow();
            JsonUtility.FromJsonOverwrite(house,runtime.Sim.S);
            typeof(YYChatHub).GetProperty("S",F).GetSetMethod(true).Invoke(hub,new object[]{originalYy});hub.LingGuangTalk=originalTalk;
            locale(original.English);Bind(ctl,files,talk,original,original.English);if(originalBound!=null&&!ReferenceEquals(originalBound,original))Call(talk,"Bind",originalBound);
            ctl.View.Open(tab);Call(view,"Rebuild");ctl.enabled=ctlEnabled;hub.enabled=hubEnabled;talk.enabled=talkEnabled;
            if(filesOpen)files.Show();if(yyOpen)yyWindow.OpenWindow();if(exeOpen)ctl.Window.OpenWindow();else ctl.Window.CloseWindow();
        }
    }
}
#endif
