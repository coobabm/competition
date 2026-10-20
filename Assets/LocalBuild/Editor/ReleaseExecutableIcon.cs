#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>The approved D_AB 融合 · 屏中活字 artwork, applied explicitly before a release build.</summary>
public static class ReleaseExecutableIcon
{
    const string Root="Assets/LocalBuild/ReleaseIcon/";
    static readonly int[] Available={16,24,32,48,64,128,256,512,1024};
    [Serializable] sealed class Entry { public string target,asset;public int requestedSize,sourceSize; }
    [Serializable] sealed class Report
    {
        public string source="D_AB融合_屏中活字.ico";
        public string originalIcoSha256="7401c578f76b853477dd99467346ef344e28ae1907b00919502e1db8337dc98e";
        public string verification="PlayerSettings assignments verified. Inspect the built EXE resources separately.";
        public List<Entry> icons=new List<Entry>();
    }
    [MenuItem("Tools/Local Verification/Apply approved release EXE icon")]
    public static void Apply()
    {
        var report=new Report();
        ApplyTarget(BuildTargetGroup.Unknown,report);
        ApplyTarget(BuildTargetGroup.Standalone,report);
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("LocalVerification/Release");
        File.WriteAllText("LocalVerification/Release/icon-settings.json",JsonUtility.ToJson(report,true));
        Debug.Log("RELEASE_EXECUTABLE_ICON_SETTINGS_PASS: D_AB融合_屏中活字 (default + Standalone)");
    }
#pragma warning disable 618 // This compatibility API covers the default icon as well as Standalone in Unity 6000.
    static void ApplyTarget(BuildTargetGroup group,Report report)
    {
        var sizes=PlayerSettings.GetIconSizesForTargetGroup(group,IconKind.Any);
        if(sizes==null||sizes.Length==0)
        {
            if(group!=BuildTargetGroup.Unknown)throw new InvalidOperationException("Standalone icon sizes are unavailable.");
            sizes=new[]{128};
        }
        var textures=new Texture2D[sizes.Length];
        for(int i=0;i<sizes.Length;i++)
        {
            int source=Available[Available.Length-1];
            foreach(int candidate in Available)if(candidate>=sizes[i]){source=candidate;break;}
            string path=Root+"ScreenLivingType_"+source+".png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new FileNotFoundException("Approved release icon missing",path);
            if(importer.mipmapEnabled||importer.textureCompression!=TextureImporterCompression.Uncompressed||!importer.alphaIsTransparency)
            {
                importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency=true;importer.SaveAndReimport();
            }
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null||texture.width!=source||texture.height!=source)throw new InvalidDataException("Wrong icon dimensions: "+path);
            textures[i]=texture;report.icons.Add(new Entry{target=group.ToString(),requestedSize=sizes[i],sourceSize=source,asset=path});
        }
        PlayerSettings.SetIconsForTargetGroup(group,textures,IconKind.Any);
        var assigned=PlayerSettings.GetIconsForTargetGroup(group,IconKind.Any);
        if(assigned.Length!=textures.Length)throw new InvalidOperationException("Release icon assignment count mismatch: "+group);
        for(int i=0;i<assigned.Length;i++)if(assigned[i]!=textures[i])throw new InvalidOperationException("Release icon assignment mismatch: "+group+" / "+sizes[i]);
    }
#pragma warning restore 618
}
#endif
