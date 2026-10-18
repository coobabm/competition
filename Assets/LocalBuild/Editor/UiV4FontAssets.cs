#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
// Run only after copying the three unmodified font files and licenses described in SOURCE_MANIFEST.json.
// This generator changes only its own UiV4 asset folder; it does not replace desktop/TMP global fonts.
public static class UiV4FontAssets
{
    const string Source="Assets/LingGuangV05/Fonts/UiV4";
    const string Output="Assets/LingGuangV05/Resources/LingGuangV05/Fonts/UiV4";
    const string Probe="教练科技资料名字训练曲线答卷数据包购买解锁阶段能力对话性格图鉴返回确定取消保存读取窗口大脑完成奖励连击是否正确错误年月日资金余额电力房租知识卡 ▶›←→−×￥¥，。！？：；（）「」《》…—%+";
    [MenuItem("Tools/Local Verification/Build exe UI v4 font assets")]
    public static void Build()
    {
        string[] files={"MiSans-Regular.ttf","SourceHanSerifSC-Regular.otf","Inter-Regular.ttf"};
        foreach(string file in files)if(!File.Exists(Source+"/"+file))throw new FileNotFoundException("Missing licensed source font",Source+"/"+file);
        if(!File.Exists(Source+"/MiSans-license.pdf")||!File.Exists(Source+"/SourceHanSerif-LICENSE.txt")||!File.Exists(Source+"/Inter-LICENSE.txt"))
            throw new InvalidOperationException("Copy each source license before generating fonts.");
        Directory.CreateDirectory(Output);AssetDatabase.Refresh();
        var sans=Make(files[0],"UiSans",2048);var serif=Make(files[1],"ReadingSerif",2048);var number=Make(files[2],"Numeric",512);
        var existingCjk=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset");
        sans.fallbackFontAssetTable=existingCjk!=null?new List<TMP_FontAsset>{existingCjk}:new List<TMP_FontAsset>();
        // Only this pack's roles refer to one another. No TMP_Settings/OS/other desktop font mutation.
        serif.fallbackFontAssetTable=new List<TMP_FontAsset>{sans};number.fallbackFontAssetTable=new List<TMP_FontAsset>{sans};
        string ascii=new string(Enumerable.Range(32,95).Select(x=>(char)x).ToArray());
        string missingSans,missingSerif,missingNumber;
        sans.TryAddCharacters(ascii+Probe,out missingSans);serif.TryAddCharacters(ascii+"读书知识学习世界故事",out missingSerif);number.TryAddCharacters(ascii+"¥￥−×",out missingNumber);
        // Repeated generation can report already-populated characters as not added.
        // Report actual native coverage rather than treating that return list as missing glyphs.
        missingSans=new string((ascii+Probe).Distinct().Where(c=>!sans.HasCharacter(c)).ToArray());
        missingSerif=new string((ascii+"读书知识学习世界故事").Distinct().Where(c=>!serif.HasCharacter(c)).ToArray());
        missingNumber=new string((ascii+"¥￥−×").Distinct().Where(c=>!number.HasCharacter(c)).ToArray());
        if(!sans.HasCharacter('▶',true,true)||!number.HasCharacter('￥',true,true))
            throw new InvalidOperationException("UI symbol fallback failed (triangle or currency).");
        if(!sans.HasCharacter('✓',true,true)||!sans.HasCharacter('×',true,true))
            throw new InvalidOperationException("Answer feedback symbol fallback failed.");
        var probeCanvas=new GameObject("Numeric spacing canvas",typeof(Canvas));
        probeCanvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var probeObject=new GameObject("Numeric spacing verification",typeof(RectTransform));
        probeObject.transform.SetParent(probeCanvas.transform,false);
        try
        {
            var label=probeObject.AddComponent<TextMeshProUGUI>();label.font=number;label.fontSize=24;label.richText=true;label.fontFeatures.Clear();
            label.rectTransform.sizeDelta=new Vector2(600,60);label.textWrappingMode=TextWrappingModes.NoWrap;label.alignment=TextAlignmentOptions.TopLeft;
            label.text=LingGuangV05.Desktop.XingGuang.XgUiV4Fonts.FixedDigits("111111");label.ForceMeshUpdate(true,true);
            float ones=label.textInfo.characterInfo[5].xAdvance-label.rectTransform.rect.xMin;
            label.text=LingGuangV05.Desktop.XingGuang.XgUiV4Fonts.FixedDigits("888888");label.ForceMeshUpdate(true,true);
            float eights=label.textInfo.characterInfo[5].xAdvance-label.rectTransform.rect.xMin;
            if(ones<=0||Mathf.Abs(ones-eights)>.05f)throw new InvalidOperationException("Fixed numeric width failed: "+ones+" / "+eights);
            Debug.Log("UI_V4_FONT_CHECKS_PASS: fallback symbols resolved; fixed digit widths "+ones+" / "+eights);
        }
        finally{UnityEngine.Object.DestroyImmediate(probeCanvas);}
        foreach(var asset in new[]{sans,serif,number})PersistChildren(asset);
        AssetDatabase.SaveAssets();
        string report="Exe-only font assets generated. Source font files unchanged.\n"+
            "MiSans missing probe glyphs: "+missingSans+"\nSerif missing probe glyphs: "+missingSerif+"\nInter missing probe glyphs (Sans fallback): "+missingNumber+"\n";
        var advances=new List<float>();for(uint c='0';c<='9';c++){if(!number.characterLookupTable.TryGetValue(c,out var glyph))throw new InvalidOperationException("Inter digit missing: "+c);advances.Add(glyph.glyph.metrics.horizontalAdvance);}
        report+="Inter native digit advances: "+string.Join(",",advances)+"\nEqual native advances: "+(advances.Max()-advances.Min()<.01f)+"\n";
        Directory.CreateDirectory("LocalVerification/UiV4Fonts");File.WriteAllText("LocalVerification/UiV4Fonts/generation.txt",report);Debug.Log(report);
    }
    static TMP_FontAsset Make(string filename,string role,int size)
    {
        string path=Output+"/"+role+".asset";var font=AssetDatabase.LoadAssetAtPath<Font>(Source+"/"+filename);
        if(font==null)throw new InvalidOperationException("Unity could not import "+filename);
        var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(asset==null)
        {
            asset=TMP_FontAsset.CreateFontAsset(font,48,5,GlyphRenderMode.SDFAA,size,size,AtlasPopulationMode.Dynamic,true);
            if(asset==null)throw new InvalidOperationException("TMP generation failed: "+filename);
            asset.name="Exe UiV4 "+role;AssetDatabase.CreateAsset(asset,path);
        }
        else if(asset.sourceFontFile!=font)throw new InvalidOperationException("Existing role has another source; do not replace its GUID silently: "+path);
        asset.isMultiAtlasTexturesEnabled=true;
        var so=new SerializedObject(asset);var clear=so.FindProperty("m_ClearDynamicDataOnBuild");if(clear!=null){clear.boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();}
        PersistChildren(asset);return asset;
    }
    static void PersistChildren(TMP_FontAsset asset)
    {
        foreach(var texture in asset.atlasTextures)if(texture!=null&&!AssetDatabase.Contains(texture))AssetDatabase.AddObjectToAsset(texture,asset);
        if(asset.material!=null&&!AssetDatabase.Contains(asset.material))AssetDatabase.AddObjectToAsset(asset.material,asset);
        EditorUtility.SetDirty(asset);if(asset.material!=null)EditorUtility.SetDirty(asset.material);
        foreach(var texture in asset.atlasTextures)if(texture!=null)EditorUtility.SetDirty(texture);
    }
}
#endif
