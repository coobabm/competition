#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Desktop.XingGuang;
using UnityEditor;
using UnityEngine;

// Dedicated empty-scene runner only. Does not manipulate production audio or save preferences.
public static class SfxBackgroundLoadChecks
{
    const string AssetRoot="Assets/LingGuangV05/Resources/XgSfx/";
    [Serializable] sealed class ClipRow
    {
        public string path,initialState,finalState;
        public bool importerBackground,clipBackground,clipPreload;
        public double observedLoadedAfterMs;
        public float lengthSeconds;
    }
    [Serializable] sealed class PlaybackRow
    {
        public string request,clip,state;
        public bool observedPlaying;
        public double observedPlayingAfterMs;
    }
    [Serializable] sealed class Report
    {
        public string status="running",failure="";
        public string scope="Empty-scene production Sfx.Init/Clip/ComboClip/Play/PlayClip. Observed wait timings are NOT audio latency, continuous sound, listening validation, cold-cache disk time, or performance benchmark.";
        public double initCallMs,comboInitCallMs,allLoadedAfterMs;
        public int wavCount,outputSampleRate;
        public List<ClipRow> clips=new List<ClipRow>();
        public List<PlaybackRow> playback=new List<PlaybackRow>();
        public List<string> checks=new List<string>();
    }
    public static IEnumerator Frames()
    {
        if(!Application.isPlaying||!Application.isBatchMode)throw new InvalidOperationException("Use isolated batch PlayMode runner");
        string dir=Path.Combine(Path.GetTempPath(),"LingGuang-SfxLoad-QA-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        var report=new Report{outputSampleRate=AudioSettings.outputSampleRate};
        var host=new GameObject("Owned short SFX loading fixture");
        host.AddComponent<AudioListener>();
        var body=Body(report,host);bool completed=false;
        try
        {
            while(true)
            {
                bool more=false;object current=null;
                try{more=body.MoveNext();if(more)current=body.Current;}
                catch(Exception e){report.failure=e.ToString();report.status="failed";throw;}
                if(!more){completed=true;break;}
                yield return current;
            }
            report.status="passed";
        }
        finally
        {
            (body as IDisposable)?.Dispose();
            if(!completed&&report.status=="running"){report.status="failed";report.failure="Coroutine disposed before completion";}
            foreach(var source in host.GetComponents<AudioSource>())source.Stop();
            UnityEngine.Object.Destroy(host);
            File.WriteAllText(Path.Combine(dir,"report.json"),JsonUtility.ToJson(report,true));
            Debug.Log("SfxBackgroundLoadChecks "+report.status+": "+Path.Combine(dir,"report.json"));
        }
    }
    static IEnumerator Body(Report report,GameObject host)
    {
        var expected=new HashSet<string>(StringComparer.Ordinal);
        foreach(var file in Directory.GetFiles(AssetRoot,"*.wav",SearchOption.AllDirectories))expected.Add(file.Replace('\\','/'));
        report.wavCount=expected.Count;Require(expected.Count==58,"Expected exactly 58 owned WAV assets, found "+expected.Count);
        foreach(string path in expected)
        {
            var importer=AssetImporter.GetAtPath(path) as AudioImporter;
            Require(importer!=null&&importer.loadInBackground,"Missing/background-disabled audio importer: "+path);
        }
        double begin=Time.realtimeSinceStartupAsDouble;
        XgJuice.Sfx.Init(host);
        report.initCallMs=(Time.realtimeSinceStartupAsDouble-begin)*1000;
        double comboBegin=Time.realtimeSinceStartupAsDouble;
        XgJuice.Sfx.ComboClip(1);
        report.comboInitCallMs=(Time.realtimeSinceStartupAsDouble-comboBegin)*1000;
        var clips=new List<AudioClip>();
        foreach(XgJuice.Sfx.Id id in Enum.GetValues(typeof(XgJuice.Sfx.Id)))
            for(int variant=0;variant<XgJuice.Sfx.VariantsPerEvent;variant++)clips.Add(XgJuice.Sfx.Clip(id,variant));
        for(int step=1;step<=XgJuice.Sfx.ComboSteps;step++)clips.Add(XgJuice.Sfx.ComboClip(step));
        Require(clips.Count==58,"Production SFX API did not return 58 clips");
        var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var clip in clips)
        {
            Require(clip!=null,"Missing owned clip");
            string path=AssetDatabase.GetAssetPath(clip);
            Require(expected.Contains(path)&&seen.Add(path),"Missing, duplicated or procedural fallback asset: "+path);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);
            var row=new ClipRow{path=path,initialState=clip.loadState.ToString(),finalState=clip.loadState.ToString(),importerBackground=importer.loadInBackground,clipBackground=clip.loadInBackground,clipPreload=clip.preloadAudioData,lengthSeconds=clip.length,observedLoadedAfterMs=-1};
            report.clips.Add(row);
            Require(row.importerBackground&&row.clipBackground&&row.clipPreload,"Import/runtime background or preload flag is false: "+path);
        }
        Require(seen.SetEquals(expected),"Production API clip coverage differs from source WAV set");
        report.checks.Add("All 58 WAVs reached through production API, with preload/background enabled; no silent procedural fallback");
        double deadline=Time.realtimeSinceStartupAsDouble+30;
        while(true)
        {
            int loaded=0;
            for(int i=0;i<clips.Count;i++)
            {
                var state=clips[i].loadState;var row=report.clips[i];row.finalState=state.ToString();
                Require(state!=AudioDataLoadState.Failed,"Background load Failed: "+row.path);
                if(state==AudioDataLoadState.Loaded){loaded++;if(row.observedLoadedAfterMs<0)row.observedLoadedAfterMs=(Time.realtimeSinceStartupAsDouble-begin)*1000;}
                else Require(state==AudioDataLoadState.Loading||state==AudioDataLoadState.Unloaded,"Unknown AudioDataLoadState: "+row.path+" "+state);
            }
            if(loaded==clips.Count)break;
            Require(Time.realtimeSinceStartupAsDouble<deadline,"Timed out waiting for PCM Loaded; inspect per-clip finalState");
            yield return null; // Natural frames; no synchronous wait, sleep, or LoadAudioData call from test.
        }
        report.allLoadedAfterMs=(Time.realtimeSinceStartupAsDouble-begin)*1000;
        report.checks.Add("All owned clips observed Loaded within 30-second async wait");
        Require(!AudioListener.pause&&AudioListener.volume>0&&AudioSettings.outputSampleRate>0,"Audio disabled/paused: invoke with -forceAudio and an available audio device");
        var ids=new[]{XgJuice.Sfx.Id.Fan,XgJuice.Sfx.Id.Tick,XgJuice.Sfx.Id.Stamp,XgJuice.Sfx.Id.Fanfare};
        foreach(var id in ids)
        {
            XgJuice.Sfx.Play(id);
            AudioSource selected=null;
            foreach(var source in host.GetComponents<AudioSource>())
                if(source.clip!=null&&AssetDatabase.GetAssetPath(source.clip).StartsWith(AssetRoot+id+"_v",StringComparison.Ordinal))selected=source;
            Require(selected!=null,"Play did not assign expected standard source: "+id);
            var check=ObservePlaying(report,selected,id.ToString());
            while(check.MoveNext())yield return check.Current;
        }
        foreach(int step in new[]{1,10})
        {
            var clip=XgJuice.Sfx.ComboClip(step);XgJuice.Sfx.PlayClip(clip,1,XgJuice.Sfx.ComboGain);
            AudioSource selected=null;foreach(var source in host.GetComponents<AudioSource>())if(source.clip==clip)selected=source;
            Require(selected!=null,"Combo playback did not assign a source: "+step);
            var check=ObservePlaying(report,selected,"Combo "+step);
            while(check.MoveNext())yield return check.Current;
        }
        report.checks.Add("Fan, Tick, Stamp, Fanfare and Combo 1/10 production calls observed isPlaying; no listening/continuity claim");
    }
    static IEnumerator ObservePlaying(Report report,AudioSource source,string request)
    {
        double begin=Time.realtimeSinceStartupAsDouble;
        var row=new PlaybackRow{request=request,clip=AssetDatabase.GetAssetPath(source.clip),state=source.clip.loadState.ToString()};report.playback.Add(row);
        while(!source.isPlaying)
        {
            Require(source.clip.loadState==AudioDataLoadState.Loaded,"Playback clip lost Loaded state: "+request);
            Require(Time.realtimeSinceStartupAsDouble-begin<1,"No isPlaying observed within one second: "+request+" (audio device/forceAudio required)");
            yield return null;
        }
        row.observedPlaying=true;row.observedPlayingAfterMs=(Time.realtimeSinceStartupAsDouble-begin)*1000;
        yield return null;
    }
    static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
#endif
