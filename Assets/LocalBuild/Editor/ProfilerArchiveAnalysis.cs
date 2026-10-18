#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditorInternal;
using UnityEngine;

// Read the already-recorded raw files; no workload, save, or production settings are changed.
public static class ProfilerArchiveAnalysis
{
    public static void Run()
    {
        var roots=Directory.GetDirectories("LocalVerification/IntegrationProfiler");
        Array.Sort(roots,StringComparer.Ordinal);
        int analyzed=0;
        foreach(var root in roots)
        foreach(var file in Directory.GetFiles(root,"*.raw"))
        {
            if(new FileInfo(file).Length==0)continue;
            ProfilerDriver.ClearAllFrames();
            if(!ProfilerDriver.LoadProfile(Path.GetFullPath(file),false))throw new Exception("Cannot read profiler archive: "+file);
            int first=ProfilerDriver.firstFrameIndex,last=ProfilerDriver.lastFrameIndex,count=0;
            var sums=new Dictionary<string,double>(StringComparer.Ordinal);
            var hits=new Dictionary<string,long>(StringComparer.Ordinal);
            for(int f=first;f<=last;f++)using(var data=ProfilerDriver.GetRawFrameDataView(f,0))
            {
                if(!data.valid)continue;
                count++;
                for(int i=0;i<data.sampleCount;i++)
                {
                    double ms=data.GetSampleTimeMs(i);if(ms<=0)continue;
                    string name=data.GetSampleName(i);
                    sums.TryGetValue(name,out var sum);sums[name]=sum+ms;
                    hits.TryGetValue(name,out var hit);hits[name]=hit+1;
                }
            }
            if(count==0)throw new Exception("Archive contains no readable main-thread frames: "+file);
            var rows=new List<KeyValuePair<string,double>>(sums);rows.Sort((a,b)=>b.Value.CompareTo(a.Value));
            var lines=new List<string>{"Source: "+Path.GetFileName(file),"Loaded frames: "+count+" ("+first+".."+last+")", "Inclusive timings overlap for parent/child markers; these are Editor captures, not standalone FPS.","Rank\tMarker\tMean ms/frame\tCalls/frame"};
            int rank=0;foreach(var pair in rows)lines.Add((++rank)+"\t"+pair.Key+"\t"+(pair.Value/count).ToString("F5",CultureInfo.InvariantCulture)+"\t"+((double)hits[pair.Key]/count).ToString("F3",CultureInfo.InvariantCulture));
            File.WriteAllLines(Path.ChangeExtension(file,"archive-hotspots.tsv"),lines);
            Debug.Log("PROFILE_ARCHIVE_READ: "+file+"; frames="+count);analyzed++;
        }
        ProfilerDriver.ClearAllFrames();
        if(analyzed==0)throw new Exception("No profiler archives found");
        Debug.Log("PROFILE_ARCHIVE_ANALYSIS_PASS: "+analyzed+" archives");
    }
}
#endif
