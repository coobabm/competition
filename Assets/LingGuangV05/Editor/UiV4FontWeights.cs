using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace LingGuangV05.EditorTools
{
    /// <summary>
    /// Real MiSans weights for the .exe UI. Before this, every FontStyles.Bold / &lt;b&gt; in the .exe view
    /// was TMP faux bold on MiSans Regular (empty weight table), which smears small CJK text.
    /// Builds UiSansMedium / UiSansSemibold beside UiSans (same dynamic SDF settings) and points
    /// UiSans' weight table at them: &lt;font-weight=500&gt; uses Medium, Bold and 600 use Semibold.
    /// Idempotent: existing assets are reused so GUIDs stay stable.
    /// Batch: -executeMethod LingGuangV05.EditorTools.UiV4FontWeights.BuildAndExit
    /// </summary>
    public static class UiV4FontWeights
    {
        const string SourceDir = "Assets/LingGuangV05/Fonts/UiV4/";
        const string AssetDir = "Assets/LingGuangV05/Resources/LingGuangV05/Fonts/UiV4/";
        public const float Sharpness = .5f;

        [MenuItem("Tools/LingGuang/Build UiV4 Font Weights")]
        public static void Build()
        {
            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDir + "UiSans.asset");
            if (regular == null) throw new Exception("UiSans.asset missing");
            var medium = Ensure("UiSansMedium", SourceDir + "MiSans-Medium.ttf", regular);
            var semibold = Ensure("UiSansSemibold", SourceDir + "MiSans-Semibold.ttf", regular);
            // Match the regular face's warmed character coverage before the player first opens the app.
            var characters = regular.characterTable.ConvertAll(c => c.unicode).ToArray();
            foreach (var face in new[] { medium, semibold })
            {
                var serialized = new SerializedObject(face);
                var clearOnBuild = serialized.FindProperty("m_ClearDynamicDataOnBuild");
                if (clearOnBuild != null) { clearOnBuild.boolValue = false; serialized.ApplyModifiedPropertiesWithoutUndo(); }
                if (!face.TryAddCharacters(characters, out uint[] missing))
                    Debug.LogWarning("[UiV4FontWeights] " + face.name + " uses fallback for " + missing.Length + " characters.");
            }

            var table = regular.fontWeightTable;
            table[5].regularTypeface = medium;
            table[6].regularTypeface = semibold;
            table[7].regularTypeface = semibold;
            // TMP exposes the backing array through a read-only property; entries above mutate it in place.
            // The heavier faces inherit the same table so nested <b> inside <font-weight> stays real.
            foreach (var face in new[] { medium, semibold })
            {
                var t = face.fontWeightTable;
                t[4].regularTypeface = regular; t[5].regularTypeface = medium;
                t[6].regularTypeface = semibold; t[7].regularTypeface = semibold;
                EditorUtility.SetDirty(face);
            }
            // Small CJK UI text (12-14 px at 720p) smears with TMP's default SDF edge softness.
            // _Sharpness .5 was chosen from LocalVerification/FontBootPreview sharpness-study.png (0/.25/.5/.75).
            foreach (var face in new[] { regular, medium, semibold })
                if (face.material != null && face.material.HasProperty("_Sharpness"))
                { face.material.SetFloat("_Sharpness", Sharpness); EditorUtility.SetDirty(face.material); }
            EditorUtility.SetDirty(regular);
            AssetDatabase.SaveAssets();
            Debug.Log("[UiV4FontWeights] UiSans weight table: 500=Medium, 600/700=Semibold; _Sharpness=" + Sharpness);
        }

        public static void BuildAndExit()
        {
            int code = 0;
            try { Build(); }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            EditorApplication.Exit(code);
        }

        static TMP_FontAsset Ensure(string name, string fontPath, TMP_FontAsset template)
        {
            string path = AssetDir + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null) throw new Exception("Missing source font " + fontPath);
            if (existing != null) { CopyFallbacks(template, existing); return existing; }

            var asset = TMP_FontAsset.CreateFontAsset(font, 48, 5, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (asset == null) throw new Exception("Could not create " + name);
            asset.name = name;
            asset.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(asset, path);
            for (int i = 0; i < asset.atlasTextures.Length; i++)
            {
                asset.atlasTextures[i].name = name + " Atlas" + (i == 0 ? "" : " " + i);
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[i], asset);
            }
            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            asset.material.mainTexture = asset.atlasTexture;
            var so = new SerializedObject(asset);
            var clear = so.FindProperty("m_ClearDynamicDataOnBuild");
            if (clear != null) { clear.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
            CopyFallbacks(template, asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static void CopyFallbacks(TMP_FontAsset from, TMP_FontAsset to)
        {
            to.fallbackFontAssetTable = from.fallbackFontAssetTable == null
                ? new System.Collections.Generic.List<TMP_FontAsset>()
                : new System.Collections.Generic.List<TMP_FontAsset>(from.fallbackFontAssetTable);
            EditorUtility.SetDirty(to);
        }
    }
}
