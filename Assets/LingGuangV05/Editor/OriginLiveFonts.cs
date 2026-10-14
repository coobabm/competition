using System.IO;
using LingGuangV05.Desktop.Story;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace LingGuangV05.Editor
{
    /// <summary>
    /// Writes the origin film's two dynamic TMP font assets into Resources, so a build finds them: Source Han Serif SC Bold (OFL, bundled as
    /// Localization/Fonts/OriginSerif-*.otf) and Roboto Mono (Apache-2.0, the Feel demo font). The font data itself is never modified; the
    /// assets are dynamic (empty atlas, glyphs are rasterised at run time), so the build does not bake 25 MB of glyphs.
    /// </summary>
    public static class OriginLiveFonts
    {
        const string OutDir = "Assets/LingGuangV05/Resources/LingGuangV05/Fonts";

        [MenuItem("Tools/LingGuang/Origin live – build font assets")]
        public static void BuildMenu() { Debug.Log(Build()); }

        public static string Build()
        {
            Directory.CreateDirectory(OutDir);
            string a = Make(OriginFonts.SerifFontAssetPath, "OriginSerif SDF");
            string b = Make(OriginFonts.MonoFontAssetPath, "OriginMono SDF");
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            OriginFonts.Reset();
            return a + "\n" + b;
        }

        static string Make(string fontPath, string name)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null) return "missing font " + fontPath;
            string path = OutDir + "/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null) AssetDatabase.DeleteAsset(path);
            var asset = TMP_FontAsset.CreateFontAsset(font, 48, 20, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (asset == null) return "could not create " + name;
            asset.name = name;
            AssetDatabase.CreateAsset(asset, path);
            if (asset.atlasTextures != null)
                for (int i = 0; i < asset.atlasTextures.Length; i++) { asset.atlasTextures[i].name = name + " Atlas" + (i == 0 ? "" : " " + i); AssetDatabase.AddObjectToAsset(asset.atlasTextures[i], asset); }
            if (asset.material != null) { asset.material.name = name + " Material"; AssetDatabase.AddObjectToAsset(asset.material, asset); }
            var so = new SerializedObject(asset);
            var clear = so.FindProperty("m_ClearDynamicDataOnBuild");
            if (clear != null) { clear.boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); }
            EditorUtility.SetDirty(asset);
            return "wrote " + path;
        }
    }
}
