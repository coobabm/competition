#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// Offline only: -executeMethod ProfilerRawAudit.Run -profilerArchiveRoot <completed directory> -quit
// Never enable this while a workload capture is running. All outputs are a new child directory.
public static class ProfilerRawAudit
{
    sealed class Stat { public long calls; public double total, max, self; public int maxFrame, invalidSelf; public string maxPath; }
    sealed class ThreadStats { public int frames; public string name; public Dictionary<string,Stat> markers=new Dictionary<string,Stat>(); }
    struct Parent { public int index,end; }
    [Serializable] sealed class RecordedReport { public Metric[] metrics; }
    [Serializable] sealed class Metric { public string name,unit;public double mean,max;public int samples; }
    static string F(double x)=>x.ToString("F6",CultureInfo.InvariantCulture);
    static string Clean(string value)=>(value??"").Replace('\t',' ').Replace('\n',' ').Replace('\r',' ');
    static bool Valid(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x)&&x>=0;
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||UnityEngine.Profiling.Profiler.enabled)
            throw new InvalidOperationException("Stop PlayMode and active recording before offline raw analysis");
        string root=null;var args=Environment.GetCommandLineArgs();
        for(int i=0;i+1<args.Length;i++)if(args[i]=="-profilerArchiveRoot")root=args[i+1];
        if(string.IsNullOrEmpty(root))root=Directory.GetDirectories("LocalVerification/IntegrationProfiler").Where(p=>File.Exists(Path.Combine(p,"complete.txt"))).OrderBy(p=>p,StringComparer.Ordinal).LastOrDefault();
        if(string.IsNullOrEmpty(root)||!File.Exists(Path.Combine(root,"complete.txt")))throw new InvalidOperationException("Require a completed capture root");
        root=Path.GetFullPath(root);string output=Path.Combine(root,"raw-analysis-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));Directory.CreateDirectory(output);
        try
        {
            foreach(string phase in new[]{"teach-continuous","train-auto","yy-tieba-switch"})Analyze(root,output,phase);
            File.WriteAllText(Path.Combine(output,"complete.txt"),"Offline raw analysis complete. Source raw/JSON/old reports unchanged. Inclusive parents overlap; self time excludes immediate children, invalid overlapping self values omitted. Thread maxima are not necessarily the same frame. Editor workload only; audio timings are not proof of audible continuity.");
            Debug.Log("RAW_AUDIT_PASS: "+output);
        }
        finally{ProfilerDriver.ClearAllFrames();}
    }
    static void Analyze(string root,string output,string phase)
    {
        string source=Path.Combine(root,phase+".raw");
        if(!File.Exists(source)||new FileInfo(source).Length==0)throw new InvalidOperationException("Missing raw: "+source);
        ProfilerDriver.ClearAllFrames();if(!ProfilerDriver.LoadProfile(source,false))throw new InvalidOperationException("Cannot load raw: "+source);
        int first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex,validFrames=0,invalidSamples=0;
        if(first<0||last<first)throw new InvalidOperationException("Raw contains no loaded frames: "+source);
        var threads=new Dictionary<string,ThreadStats>();var frameRows=new List<string>{"Frame\tThreadId\tThread\tFrameMs\tSamples\tMaxSampleMs\tMaxSample\tMaxSamplePath"};
        var peaks=new List<string>{"Frame\tThreadId\tThread\tMarker\tInclusiveMs\tSelfMs\tPath"};
        var anomalies=new List<string>{"Category\tFrame\tThread\tMarker\tValue\tNote"};
        for(int frame=first;frame<=last;frame++)
        {
            bool frameValid=false;
            for(int threadIndex=0;threadIndex<512;threadIndex++)
            using(var data=ProfilerDriver.GetRawFrameDataView(frame,threadIndex))
            {
                if(!data.valid)break;
                if(threadIndex==511)throw new InvalidOperationException("Thread enumeration safety bound reached");
                frameValid=true;string key=data.threadId.ToString(CultureInfo.InvariantCulture),name=Clean(data.threadGroupName+"/"+data.threadName);
                if(!threads.TryGetValue(key,out var thread)){thread=new ThreadStats{name=name};threads.Add(key,thread);}thread.frames++;
                int n=data.sampleCount;var names=new string[n];var times=new double[n];var childrenTime=new double[n];var invalidChild=new bool[n];var parents=new int[n];var stack=new List<Parent>();
                for(int i=0;i<n;i++)
                {
                    names[i]=Clean(data.GetSampleName(i));times[i]=data.GetSampleTimeMs(i);
                    while(stack.Count>0&&i>stack[stack.Count-1].end)stack.RemoveAt(stack.Count-1);
                    int parent=stack.Count==0?-1:stack[stack.Count-1].index;parents[i]=parent;
                    if(!Valid(times[i])){if(parent>=0)invalidChild[parent]=true;invalidSamples++;anomalies.Add("invalid_raw_time\t"+frame+"\t"+name+"\t"+names[i]+"\t"+F(times[i])+"\tExcluded from aggregates");}
                    else if(parent>=0)childrenTime[parent]+=times[i];
                    int descendants=data.GetSampleChildrenCountRecursive(i);
                    if(descendants>0)stack.Add(new Parent{index=i,end=Math.Min(n-1,i+descendants)});
                }
                double max=-1;int maxi=-1;
                for(int i=0;i<n;i++)
                {
                    double ms=times[i];if(!Valid(ms))continue;
                    double self=ms-childrenTime[i];bool validSelf=Valid(self)&&!invalidChild[i]; // negative means overlapping/truncated children; never clamp into fake evidence
                    if(!thread.markers.TryGetValue(names[i],out var stat)){stat=new Stat();thread.markers.Add(names[i],stat);}
                    stat.calls++;stat.total+=ms;if(validSelf)stat.self+=self;else stat.invalidSelf++;
                    if(ms>stat.max){stat.max=ms;stat.maxFrame=frame;stat.maxPath=PathOf(i,parents,names);}
                    if(ms>max){max=ms;maxi=i;}
                    // Script/audio/log samples >=2ms and any self-time >=2ms retain exact frames and ancestry for spike attribution.
                    if((validSelf&&self>=2)||(ms>=2&&(names[i].Contains("Assembly-CSharp")||names[i].Contains("GC.Collect")||names[i].Contains("Audio")||names[i].Contains("Log")||names[i].Contains("TMP"))))
                        peaks.Add(frame+"\t"+key+"\t"+name+"\t"+names[i]+"\t"+F(ms)+"\t"+(validSelf?F(self):"NA")+"\t"+PathOf(i,parents,names));
                }
                frameRows.Add(frame+"\t"+key+"\t"+name+"\t"+(Valid(data.frameTimeMs)?F(data.frameTimeMs):"NA")+"\t"+n+"\t"+F(Math.Max(0,max))+"\t"+(maxi>=0?names[maxi]:"")+"\t"+(maxi>=0?PathOf(maxi,parents,names):""));
            }
            if(frameValid)validFrames++;
        }
        if(validFrames==0||!threads.Values.Any(t=>t.name.Contains("Main Thread")))throw new InvalidOperationException("No valid main-thread frames in "+source);
        var rows=new List<string>{"ThreadId\tThread\tValidThreadFrames\tMarker\tCalls\tInclusiveMeanMsPerThreadFrame\tMaxSingleSampleMs\tMaxSampleFrame\tSelfMeanMsPerThreadFrame\tInvalidSelfSamples\tMaxSamplePath"};
        foreach(var t in threads)foreach(var pair in t.Value.markers.OrderByDescending(p=>p.Value.total))
        {
            var x=pair.Value;rows.Add(t.Key+"\t"+t.Value.name+"\t"+t.Value.frames+"\t"+pair.Key+"\t"+x.calls+"\t"+F(x.total/t.Value.frames)+"\t"+F(x.max)+"\t"+x.maxFrame+"\t"+(x.invalidSelf==0?F(x.self/t.Value.frames):"NA")+"\t"+x.invalidSelf+"\t"+x.maxPath);
        }
        string json=Path.Combine(root,phase+".json");
        if(File.Exists(json))
        {
            var report=JsonUtility.FromJson<RecordedReport>(File.ReadAllText(json));
            if(report?.metrics!=null)foreach(var metric in report.metrics)
                if(metric.unit=="TimeNanoseconds"&&(!Valid(metric.mean)||!Valid(metric.max)))
                    anomalies.Add("invalid_recorder_aggregate\tNA\tNA\t"+Clean(metric.name)+"\tmean="+F(metric.mean)+",max="+F(metric.max)+"\tOriginal JSON retained; do not treat this counter as timing evidence");
        }
        File.WriteAllLines(Path.Combine(output,phase+"-markers.tsv"),rows);
        File.WriteAllLines(Path.Combine(output,phase+"-frames.tsv"),frameRows);
        File.WriteAllLines(Path.Combine(output,phase+"-sample-peaks.tsv"),peaks);
        File.WriteAllLines(Path.Combine(output,phase+"-anomalies.tsv"),anomalies);
        File.WriteAllText(Path.Combine(output,phase+"-scope.txt"),"Source="+source+"\nLoadedRange="+first+".."+last+"\nValidFrames="+validFrames+"\nThreads="+threads.Count+"\nInvalidRawSamples="+invalidSamples+"\nZero-duration samples retained. Negative/nonfinite samples excluded and reported. No audio continuity claim. Not player FPS. Inclusive values overlap; maxima may be unrelated frames. Thread availability varies; denominator is valid frames of that thread.\n");
    }
    static string PathOf(int index,int[] parents,string[] names)
    {
        var path=new List<string>();for(int i=index;i>=0;i=parents[i])path.Add(names[i]);path.Reverse();return string.Join(" > ",path);
    }
}
#endif
