#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.Apps;
using LingGuangV05.Desktop.OS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run with audio enabled, no -quit: -executeMethod MusicInteractionChecks.Run
// Uses Main, real WindowManager handlers, isolated save/profile. Does not replace audio with a mock.
[InitializeOnLoad]
public static class MusicInteractionChecks
{
    const string Key="MusicInteractionChecks.Root", Defaults="LingGuang.Music.Defaults.v1";
    const BindingFlags InstanceFlags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags StaticFlags=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    const string DefaultsV2="LingGuang.Music.Defaults.v2";
    const string MusicThought="开机第一件事当然是来首歌听听......";
    static string Root=>SessionState.GetString(Key,"");
    static GameMusic music;
    static MusicApp manager;
    static WindowManager window;
    static AudioSource source;
    static ChapterOneRuntime runtime;
    static int phase, lastFrame=-1, ticketsBefore, ticketsAfter;
    static double deadline, due;
    static float beforeTime, fadeStartVolume;
    static AudioClip endingClip,expectedClip;
    static int endingIndex,expectedIndex;
    static double endingDsp,endingTimeout;
    static float seekTime;
    static string endingCase;
    static bool seekConfirmed;
    static bool attached, finishing;
    static Report report;
    [Serializable] sealed class Item { public string check, detail; public bool passed; }
    [Serializable] sealed class Report
    {
        public string scope="Editor PlayMode Main; actual Music WindowManager.OpenWindow/CloseWindow; real AudioSource; shutdown fade followed by GameMusic disable/enable simulates boot lifecycle, not full title camera animation; since 2026-10-22 the desktop starts the music itself (F2)";
        public string unityVersion, audioDriver, voiceClip, status="running", failure="";
        public bool batchMode, audioListenerPaused;
        public int audioSampleRate;
        public List<Item> checks=new List<Item>();
        public List<string> runtimeErrors=new List<string>(),editorInfrastructureErrors=new List<string>();
    }
    static MusicInteractionChecks(){EditorApplication.playModeStateChanged+=State;}
    // Safe while another player is running: no scene, PlayMode, prefs, profile, or real audio playback.
    public static void RunIsolated()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("EditMode only");
        GameObject fixture=null;MusicPlaylist list=null;AudioClip clip=null;
        int checks=0;
        void Need(bool value,string label){if(!value)throw new InvalidOperationException(label);checks++;}
        try
        {
            fixture=new GameObject("Inactive music policy fixture");fixture.SetActive(false);
            var player=fixture.AddComponent<MusicApp>();player.audioSource=fixture.AddComponent<AudioSource>();
            list=ScriptableObject.CreateInstance<MusicPlaylist>();
            clip=AudioClip.Create("Fixture only",4410,1,44100,false);
            for(int i=0;i<5;i++)list.playlist.Add(new MusicPlaylist.MusicItem{musicClip=i==2?null:clip,excludeFromLibrary=i==1});
            for(int i=0;i<5;i++)
            {
                var go=new GameObject("Row "+i);go.transform.SetParent(fixture.transform,false);
                var row=go.AddComponent<MusicTrackRow>();row.playlist=list;row.itemIndex=i;player.playerQueue.Add(row);
            }
            player.currentPlaylist=list;player.currentTrackIndex=0;
            player.SetRepeatOne(true,false);
            Need(player.repeat&&!player.shuffle&&player.audioSource.loop,"Single repeat applies without persistence");
            player.SetShuffle(true,false);
            Need(player.shuffle&&!player.repeat&&!player.audioSource.loop,"Shuffle clears single repeat without persistence");
            var randomState=UnityEngine.Random.state;
            try
            {
                for(int i=0;i<30;i++)
                {
                    int index=player.NextPlayableTrack(1).itemIndex;
                    Need(index==3||index==4,"Random skips current, excluded and clip-less rows");
                }
            }
            finally{UnityEngine.Random.state=randomState;}
            player.SetShuffle(false,false);
            Need(player.NextPlayableTrack(1).itemIndex==3,"Sequential uses filtered queue");
            Need(player.NextPlayableTrack(-1).itemIndex==4,"Previous wraps filtered queue");
            player.currentTrackIndex=4;Need(player.NextPlayableTrack(1).itemIndex==0,"Next wraps filtered queue");
            player.playerQueue.RemoveRange(1,4);player.currentTrackIndex=0;player.SetShuffle(true,false);
            Need(player.NextPlayableTrack(1).itemIndex==0,"Single playable track is bounded");
            player.playerQueue.Clear();Need(player.NextPlayableTrack(1)==null,"Empty playable queue is bounded");
            Need(MusicApp.ShouldAdvanceAfterEnd(true,false,true,false,true),"Natural end advances");
            Need(!MusicApp.ShouldAdvanceAfterEnd(true,false,false,false,true),"Explicit pause or stop does not advance");
            Need(!MusicApp.ShouldAdvanceAfterEnd(true,false,true,true,true),"Listener pause does not advance");
            Need(!MusicApp.ShouldAdvanceAfterEnd(true,false,true,false,false),"Disabled source does not advance");
            Need(!MusicApp.ShouldAdvanceAfterEnd(true,true,true,false,true),"Playing or repeat-one source does not advance");
            Need(!MusicApp.ShouldAdvanceAfterEnd(false,false,true,false,true),"Consumed end does not advance twice");
            Debug.Log("MUSIC_ISOLATED_PASS checks="+checks+"; no PlayerPrefs/profile/save writes; policy checks only, actual DSP playback not exercised");
        }
        finally
        {
            if(fixture!=null)UnityEngine.Object.DestroyImmediate(fixture);
            if(list!=null)UnityEngine.Object.DestroyImmediate(list);
            if(clip!=null)UnityEngine.Object.DestroyImmediate(clip);
        }
    }

    [MenuItem("Tools/Local Verification/Music interaction checks")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Start in Edit Mode.");
        string dir=Path.GetFullPath(Path.Combine("LocalVerification","MusicInteraction",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
        Directory.CreateDirectory(dir);Directory.CreateDirectory(Path.Combine(dir,"Save"));Directory.CreateDirectory(Path.Combine(dir,"DesktopProfile"));
        SessionState.SetString(Key,dir);
        SessionState.SetString(Key+".PreviousSave",ChapterOneRuntime.VerificationSaveDirectory??"");
        SessionState.SetString(Key+".PreviousProfile",(string)(typeof(DreamOSDataManager).GetField("dataPath",StaticFlags)?.GetValue(null)??""));
        SessionState.SetBool(Key+".HadDefaults",PlayerPrefs.HasKey(Defaults));SessionState.SetInt(Key+".Defaults",PlayerPrefs.GetInt(Defaults));
        SessionState.SetBool(Key+".HadDefaultsV2",PlayerPrefs.HasKey(DefaultsV2));SessionState.SetInt(Key+".DefaultsV2",PlayerPrefs.GetInt(DefaultsV2));
        SessionState.SetBool(Key+".Batch",Application.isBatchMode);
        EditorSceneManager.OpenScene("Assets/LingGuangV05/Scenes/Main.unity");
        EditorApplication.EnterPlaymode();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Isolate()
    {
        if(string.IsNullOrEmpty(Root))return;
        ChapterOneRuntime.VerificationSaveDirectory=Path.Combine(Root,"Save");
        typeof(DreamOSDataManager).GetField("dataPath",StaticFlags)?.SetValue(null,Path.Combine(Root,"DesktopProfile")+Path.DirectorySeparatorChar);
        SceneManager.sceneLoaded+=DisableModel;
    }
    static void DisableModel(Scene scene,LoadSceneMode mode)
    {
        if(string.IsNullOrEmpty(Root))return;
        foreach(var root in scene.GetRootGameObjects())foreach(var rt in root.GetComponentsInChildren<ChapterOneRuntime>(true))
        {var llm=rt.GetComponent<LocalLlm>()??rt.gameObject.AddComponent<LocalLlm>();llm.enabled=false;}
    }
    static void State(PlayModeStateChange state)
    {
        if(string.IsNullOrEmpty(Root))return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            phase=0;attached=finishing=false;lastFrame=-1;deadline=EditorApplication.timeSinceStartup+90;
            report=new Report{unityVersion=Application.unityVersion,batchMode=Application.isBatchMode,audioDriver=AudioSettings.driverCapabilities.ToString(),audioSampleRate=AudioSettings.outputSampleRate,audioListenerPaused=AudioListener.pause};
            Application.logMessageReceived+=Log;EditorApplication.update+=Tick;
        }
        else if(state==PlayModeStateChange.EnteredEditMode)
        {
            Application.logMessageReceived-=Log;EditorApplication.update-=Tick;SceneManager.sceneLoaded-=DisableModel;
            if(SessionState.GetBool(Key+".HadDefaults",false))PlayerPrefs.SetInt(Defaults,SessionState.GetInt(Key+".Defaults",0));else PlayerPrefs.DeleteKey(Defaults);
            if(SessionState.GetBool(Key+".HadDefaultsV2",false))PlayerPrefs.SetInt(DefaultsV2,SessionState.GetInt(Key+".DefaultsV2",0));else PlayerPrefs.DeleteKey(DefaultsV2);
            SessionState.EraseBool(Key+".HadDefaultsV2");SessionState.EraseInt(Key+".DefaultsV2");
            PlayerPrefs.Save();ChapterOneRuntime.VerificationSaveDirectory=SessionState.GetString(Key+".PreviousSave","");
            typeof(DreamOSDataManager).GetField("dataPath",StaticFlags)?.SetValue(null,SessionState.GetString(Key+".PreviousProfile",""));
            SessionState.EraseString(Key+".PreviousSave");SessionState.EraseString(Key+".PreviousProfile");
            bool batch=SessionState.GetBool(Key+".Batch",false);string dir=Root;
            bool passed=File.Exists(Path.Combine(dir,"passed.txt"));
            SessionState.EraseString(Key);SessionState.EraseBool(Key+".HadDefaults");SessionState.EraseInt(Key+".Defaults");SessionState.EraseBool(Key+".Batch");
            Debug.Log("Music interaction checks: "+(passed?"PASS":"FAIL/INTERRUPTED")+"; report: "+dir);
            if(batch)EditorApplication.Exit(passed?0:1);
        }
    }
    static void Check(bool condition,string name,string detail)
    {
        report.checks.Add(new Item{check=name,passed=condition,detail=detail});Save();
        if(!condition)throw new InvalidOperationException(name+": "+detail);
    }
    static void Log(string message,string stack,LogType type)
    {
        if(finishing||report==null||(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert))return;
        if(stack.Contains("UnityEditor.Search.SearchDatabase")&&!stack.Contains("Assets/"))report.editorInfrastructureErrors.Add(message+"\n"+stack);
        else report.runtimeErrors.Add(message+"\n"+stack);
    }
    static void Save()=>File.WriteAllText(Path.Combine(Root,"report.json"),JsonUtility.ToJson(report,true));
    static object Field(object o,string name)=>o.GetType().GetField(name,InstanceFlags)?.GetValue(o);
    static int MusicThoughtCount()
    {
        var voice=typeof(InnerVoice).GetField("instance",StaticFlags)?.GetValue(null);if(voice==null)return 0;
        int count=0;object current=Field(voice,"current");
        if(current!=null && (string)current.GetType().GetField("zh",InstanceFlags)?.GetValue(current)==MusicThought)count++;
        var queue=Field(voice,"queue") as System.Collections.IEnumerable;
        if(queue!=null)foreach(var line in queue)if((string)line.GetType().GetField("zh",InstanceFlags)?.GetValue(line)==MusicThought)count++;
        return count;
    }
    static int VoiceTicket()=> (int)(typeof(InnerVoice).GetField("nextTicket",StaticFlags)?.GetValue(null)??-1);
    static void Wait(int next,double seconds){phase=next;due=EditorApplication.timeSinceStartup+seconds;}
    static void Tick()
    {
        if(!EditorApplication.isPlaying||finishing)return;
        try
        {
            if(report.runtimeErrors.Count>0)throw new InvalidOperationException("Runtime error: "+report.runtimeErrors[0]);
            double now=EditorApplication.timeSinceStartup;
            if(now>deadline)throw new TimeoutException("90-second timeout at phase "+phase+". Audio must be enabled; do not pass -noaudio.");
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            if(phase==0)
            {
                runtime=UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
                var ctl=runtime==null?null:runtime.GetComponent<XingGuangController>();
                if(ctl==null||ctl.Sim==null||ctl.View==null)return;
                if(!attached)
                {
                    runtime.useDiskSave=false;runtime.tickEnabled=false;
                    foreach(var component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                    {
                        string name=component.GetType().Name;
                        if(name=="StoryDirector"||name=="StoryDesktopPresenter"||name=="PrologueDirector"||name=="SpiderIntroScene"||name=="LoveQuestionCutscene"||name=="StageOneTutor")
                        {component.CancelInvoke();component.StopAllCoroutines();component.enabled=false;}
                    }
                    var house=new ChapterOneSim();house.S.prologue=2;house.S.appInstallState=2;house.S.money=100000;house.S.labState=JsonUtility.ToJson(new XgSim().S);
                    typeof(ChapterOneRuntime).GetMethod("Attach",InstanceFlags).Invoke(runtime,new object[]{house});
                    typeof(XingGuangController).GetMethod("BindSave",InstanceFlags).Invoke(ctl,new object[]{house});
                    var boot=UnityEngine.Object.FindAnyObjectByType<BootManager>(FindObjectsInactive.Include);
                    if(boot!=null){boot.CancelInvoke();boot.StopAllCoroutines();if(boot.bootAnimator!=null)boot.bootAnimator.gameObject.SetActive(false);}
                    var user=UnityEngine.Object.FindAnyObjectByType<UserManager>(FindObjectsInactive.Include);
                    if(user==null||!user.isActiveAndEnabled)throw new InvalidOperationException("Desktop user unavailable");user.ShowDesktop();
                    InnerVoice.ResumeAfterBoot();attached=true;
                }
                music=UnityEngine.Object.FindAnyObjectByType<GameMusic>();
                if(music==null||!(Field(music,"ready") is bool ready)||!ready)return;
                manager=Field(music,"manager") as MusicApp;source=Field(music,"source") as AudioSource;window=Field(music,"musicWindow") as WindowManager;
                if(window==null||window.transform.parent==null||!window.transform.parent.gameObject.activeInHierarchy)return;
                Check(manager!=null&&source!=null&&window!=null,"real music components","manager, source and actual parent WindowManager resolved");
                // 2026-10-22 (玩家反馈 F2): the background music starts on its own once the desktop shows.
                Wait(1,1);return;
            }
            if(now<due)return;
            if(phase==1)
            {
                Check(GameMusic.Started&&source.isPlaying,"desktop starts the background music on its own","Started="+GameMusic.Started+", playing="+source.isPlaying+", queue="+manager.playerQueue.Count);
                Check(source.clip!=null&&source.clip.name=="emergency_1","correct first track",source.clip==null?"null clip":source.clip.name);
                Check(!manager.repeat&&!manager.shuffle&&!source.loop,"autoplay starts in playlist order","repeat/shuffle/source.loop all false, overriding legacy modes");
                string line="我去，什么玩意儿在动我电脑？不会中病毒了吧？！";
                Check(VoiceLines.Find(line)==null,"recordings do not ship","VoiceLines.Find finds no legacy clip");
                float duration=VoiceLines.Play(line);
                Check(duration>0&&VoiceLines.Speaking&&VoiceLines.Revealed(0)>=0,"thought is spoken in BlipTalk babble","Play returned "+duration+", Speaking="+VoiceLines.Speaking);
                var voiceSource=typeof(VoiceLines).GetField("source",StaticFlags)?.GetValue(null) as AudioSource;
                report.voiceClip=voiceSource!=null&&voiceSource.clip!=null?voiceSource.clip.name:"";
                VoiceLines.Stop();
                Check(voiceSource!=null&&!voiceSource.isPlaying,"voice AudioSource stops","Stop silences the babble before the music checks");
                Check(MusicApp.NextPlayableIndex(0,0,1,false,0)==-1&&MusicApp.NextPlayableIndex(1,0,1,true,0)==0,"empty and single playable queues are bounded","No recursive skipping or out-of-range row access");
                Check(MusicApp.NextPlayableIndex(3,2,1,false,0)==0&&MusicApp.NextPlayableIndex(3,0,-1,false,0)==2,"sequential queue wraps in both directions","Indices refer to filtered playable rows");
                for(int current=0;current<4;current++)
                {
                    var picks=new HashSet<int>();
                    for(int draw=0;draw<3;draw++)picks.Add(MusicApp.NextPlayableIndex(4,current,1,true,draw));
                    Check(picks.Count==3&&!picks.Contains(current),"shuffle visits every alternative at index "+current,"Never selects the current track when another exists");
                }
                manager.SetRepeatOne(true);
                Check(manager.repeat&&!manager.shuffle&&source.loop,"repeat one takes effect immediately","AudioSource loops the same song");
                manager.SetShuffle(true);
                Check(manager.shuffle&&!manager.repeat&&!source.loop,"shuffle clears repeat one immediately","Modes cannot cancel each other out");
                if(manager.playerQueue.Count>1)
                {
                    int previous=manager.currentTrackIndex;
                    manager.NextTrack();
                    Check(manager.currentTrackIndex!=previous&&source.isPlaying,"real shuffle skip chooses another playable song","Uses actual player queue and AudioSource");
                }
                if(manager.playerQueue.Count>=3)
                {
                    var fullQueue=new List<MusicTrackRow>(manager.playerQueue);
                    var first=fullQueue[0];var last=fullQueue[fullQueue.Count-1];
                    manager.playerQueue.Clear();manager.playerQueue.Add(first);manager.playerQueue.Add(last);
                    manager.Play(first);manager.NextTrack();
                    Check(manager.currentTrack==last,"shuffle respects sparse playable queue","Two playable rows retain original noncontiguous itemIndex values");
                    manager.PrevTrack();
                    Check(manager.currentTrack==first,"shuffle previous uses playable rows too","No raw playlist index into filtered queue");
                    manager.playerQueue.Clear();manager.playerQueue.AddRange(fullQueue);
                }
                manager.SetShuffle(false);manager.SetRepeatOne(false);
                var playingClip=source.clip;
                ticketsBefore=VoiceTicket();window.OpenWindow();ticketsAfter=VoiceTicket();
                Check(GameMusic.Started&&source.isPlaying&&source.clip==playingClip,"real window open keeps the playing song","Started="+GameMusic.Started+", playing="+source.isPlaying+", clip="+(source.clip==null?"null":source.clip.name));
                Check(ticketsBefore>=0&&ticketsAfter==ticketsBefore&&MusicThoughtCount()==0,"music open does not repeat prologue thought","InnerVoice ticket "+ticketsBefore+" -> "+ticketsAfter);
                Wait(2,1);return;
            }
            if(phase==2)
            {
                beforeTime=source.time;Check(beforeTime>.05f,"audio clock advances","time="+beforeTime);
                window.CloseWindow();Wait(3,1);return;
            }
            if(phase==3)
            {
                Check(source.isPlaying&&source.time>beforeTime+.05f,"closing window preserves playback","time="+beforeTime+" -> "+source.time+", playing="+source.isPlaying);
                beforeTime=source.time;int beforeTicket=VoiceTicket();window.OpenWindow();
                Check(source.isPlaying&&Mathf.Abs(source.time-beforeTime)<.15f,"reopening does not restart track","time="+beforeTime+" -> "+source.time);
                Check(VoiceTicket()==beforeTicket,"reopening does not repeat thought","InnerVoice ticket unchanged at "+beforeTicket);
                Wait(80,MusicMix.FadeInSeconds+.25);return;
            }
            if(phase==80){BeginNaturalEnd("open window advances to next track");Wait(81,0);return;}
            if(phase==81){if(!NaturalEndFinished())return;window.CloseWindow();Wait(82,1);return;}
            if(phase==82){BeginNaturalEnd("closed window advances to next track");Wait(83,0);return;}
            if(phase==83)
            {
                if(!NaturalEndFinished())return;
                window.OpenWindow();
                int last=manager.currentPlaylist.playlist.Count-1;
                while(last>=0&&manager.currentPlaylist.playlist[last].excludeFromLibrary)last--;
                Check(last>=1,"playlist has multiple playable tracks","last playable index="+last);
                // Select the final track as fixture setup using the real player selection handler.
                // Only its clock reaching the end may perform the wrap under test.
                manager.Play(manager.playerQueue[last]);Wait(84,.25);return;
            }
            if(phase==84){BeginNaturalEnd("last track wraps to first by natural completion");Check(expectedIndex==0,"wrap target is first track","target="+expectedIndex);Wait(85,0);return;}
            if(phase==85){if(!NaturalEndFinished())return;Wait(86,.8);return;}
            if(phase==86)
            {
                endingClip=source.clip;endingIndex=manager.currentTrackIndex;beforeTime=source.time;
                Check(beforeTime>.1f,"pause fixture has played samples","time="+beforeTime);
                manager.Pause();window.CloseWindow();Wait(87,.8);return;
            }
            if(phase==87)
            {
                Check(!source.isPlaying&&source.clip==endingClip&&manager.currentTrackIndex==endingIndex,"closed player preserves manual pause","time="+source.time+", index="+manager.currentTrackIndex);
                window.OpenWindow();Wait(88,.8);return;
            }
            if(phase==88)
            {
                Check(!source.isPlaying&&source.clip==endingClip&&manager.currentTrackIndex==endingIndex,"reopening preserves manual pause","time="+source.time+", index="+manager.currentTrackIndex);
                Check(Mathf.Abs(source.time-beforeTime)<.15f,"pause retains playback position","before="+beforeTime+", after="+source.time);
                manager.Play();Wait(89,.5);return;
            }
            if(phase==89)
            {
                Check(source.isPlaying&&source.clip==endingClip&&source.time>beforeTime+.1f,"manual resume continues from paused position","before="+beforeTime+", after="+source.time);
                BeginNaturalEnd("resumed playback naturally advances");Wait(90,0);return;
            }
            if(phase==90)
            {
                if(!NaturalEndFinished())return;endingClip=source.clip;endingIndex=manager.currentTrackIndex;manager.Stop();Wait(91,1.1);return;
            }
            if(phase==91)
            {
                Check(!source.isPlaying&&source.clip==endingClip&&manager.currentTrackIndex==endingIndex,"explicit stop does not advance playlist","stopped clip/index unchanged");
                manager.Play();Wait(4,.3);return;
            }
            if(phase==4)
            {
                Check(source.isPlaying,"music still playing before shutdown",source.clip.name);
                fadeStartVolume=source.volume;Check(fadeStartVolume>0,"fade starts from audible volume","volume="+fadeStartVolume);
                GameMusic.FadeOutForExit();
                Check(GameMusic.FadingOut(source),"shutdown owns detached audio source","fade host created by real FadeOutForExit");
                music.enabled=false;Wait(5,.4);return;
            }
            if(phase==5)
            {
                Check(source.volume<fadeStartVolume,"shutdown fades smoothly","volume="+fadeStartVolume+" -> "+source.volume);
                Wait(6,MusicMix.FadeOutSeconds+.3);return;
            }
            if(phase==6)
            {
                Check(!source.isPlaying&&!GameMusic.FadingOut(source),"shutdown fade ends stopped","playing="+source.isPlaying+", fadeHost="+GameMusic.FadingOut(source));
                music.enabled=true;Wait(7,.7);return;
            }
            if(phase==7)
            {
                Check(GameMusic.Started&&source.isPlaying,"next boot starts the background music again","Started="+GameMusic.Started+", playing="+source.isPlaying);
                report.status="passed";Save();File.WriteAllText(Path.Combine(Root,"passed.txt"),"All checks passed; see report.json for scope and evidence.");Finish();
            }
        }
        catch(Exception e)
        {
            report.status="failed";report.failure=e.ToString();Save();Debug.LogError("Music interaction check failed: "+e);Finish();
        }
    }
    static void BeginNaturalEnd(string label)
    {
        Check(!manager.repeat&&!manager.shuffle&&!source.loop,"sequential mode before "+label,"no repeat/shuffle/loop");
        Check(source.isPlaying&&source.clip!=null,"playing before "+label,"actual AudioSource must already run");
        endingCase=label;endingClip=source.clip;endingIndex=manager.currentTrackIndex;
        int count=manager.currentPlaylist.playlist.Count;expectedIndex=(endingIndex+1)%count;
        for(int i=0;i<count&&manager.currentPlaylist.playlist[expectedIndex].excludeFromLibrary;i++)expectedIndex=(expectedIndex+1)%count;
        expectedClip=manager.currentPlaylist.playlist[expectedIndex].musicClip;
        Check(expectedClip!=null&&expectedClip!=endingClip,"distinct expected next clip",endingIndex+" -> "+expectedIndex);
        // DreamOS uses integer duration while open; seek before that boundary, then let
        // the actual audio DSP clock and production Update/coroutine complete playback.
        float end=Mathf.Min(endingClip.length,manager.GetDuration());seekTime=Mathf.Max(0,end-.9f);
        seekConfirmed=false;source.timeSamples=Mathf.RoundToInt(seekTime*endingClip.frequency);endingDsp=AudioSettings.dspTime;endingTimeout=EditorApplication.timeSinceStartup+8;
        Check(source.isPlaying,"seek preserves real playback",endingClip.name+" time="+source.time+" requested="+seekTime+" length="+endingClip.length+" loadType="+endingClip.loadType+" samples="+source.timeSamples);
    }
    static bool NaturalEndFinished()
    {
        if(source.clip==endingClip)
        {
            if(!seekConfirmed&&source.time>=seekTime-.2f)
            {seekConfirmed=true;Check(true,endingCase+" seek reaches tail","requested="+seekTime+", actual="+source.time+", samples="+source.timeSamples);}
            if(!seekConfirmed&&AudioSettings.dspTime-endingDsp>3)
                Check(false,endingCase+" seek reaches tail","seek was not observed: requested="+seekTime+", actual="+source.time+", loadType="+endingClip.loadType);
            if(EditorApplication.timeSinceStartup>endingTimeout)
                Check(false,endingCase,"Timed out: index="+manager.currentTrackIndex+", time="+source.time+", playing="+source.isPlaying+", managerActive="+manager.isActiveAndEnabled+", dspElapsed="+(AudioSettings.dspTime-endingDsp));
            return false;
        }
        Check(seekConfirmed,endingCase+" tail was observed before transition","The original clip reached the requested tail before changing clips.");
        double elapsed=AudioSettings.dspTime-endingDsp;
        Check(elapsed>.05,endingCase+" uses advancing audio clock","dsp elapsed="+elapsed+", seek="+seekTime);
        Check(source.clip==expectedClip&&manager.currentTrackIndex==expectedIndex&&source.isPlaying,endingCase,
            endingClip.name+" ("+endingIndex+") -> "+source.clip?.name+" ("+manager.currentTrackIndex+"); expected="+expectedClip.name+", playing="+source.isPlaying+", new time="+source.time);
        return true;
    }
    static void Finish(){Application.logMessageReceived-=Log;finishing=true;EditorApplication.update-=Tick;EditorApplication.ExitPlaymode();}
}
#endif
