#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LingGuangV05.CaseMenu.Editor
{
    /// <summary>
    /// Bakes the title room's light (2026-10-18: the room's five lights and two soft shadows were drawn in real time on
    /// every frame, also behind the desktop). The five lights become Mixed in Shadowmask mode: their direct light and
    /// highlights are still drawn as before, but the static room's shadows come from the baked mask, so its shadow maps
    /// are no longer rendered every frame (fully Baked lights were tried and lose the highlights and look flat). The
    /// switches' small feedback lights stay real time (they pulse). Everything in the room that never moves contributes
    /// to the bake: big surfaces get lightmaps (their generated meshes get lightmap UVs here), small things and the
    /// preview monitor use light probes. The keycaps and their rings move or glow, so they stay out of it. A probe grid
    /// around the desk lights what moves there at run time: the real monitor that replaces the preview, and the keycaps.
    /// The scene's own dark Trilight stays (in game Main's environment, with no reflection, is active; the editor view
    /// before baking used Unity's default sky instead and looked bluer). Lightmaps are Directional because Main, once
    /// active, switches the lightmap mode to its own (Directional); non-directional maps then turn the room white.
    /// Needs QualitySettings shadowmaskMode = Shadowmask (set for both levels; Distance Shadowmask would draw every
    /// shadow in real time again).
    /// </summary>
    public static class CaseMenuLightBake
    {
        const string SettingsPath = "Assets/LingGuangV05/Scenes/ComputerCaseMenu Lighting.lighting";
        static readonly string[] RoomLights = { "Warm desk key", "Cool window rim", "Front soft fill", "Desk lamp pool", "Poster Wall Fill" };
        static readonly string[] Moving = { "StartButton", "SettingsButton" };
        /// <summary>Renderers smaller than this (bounds diagonal, metres) use probes instead of a lightmap.</summary>
        const float LightmapMinSize = .35f;

        [MenuItem("Tools/LingGuang/Bake Computer Case Menu Lighting")]
        public static void Bake()
        {
            Debug.Log(Prepare());
            Lightmapping.BakeAsync();
        }

        /// <summary>Sets the open ComputerCaseMenu scene up for baking (idempotent). Returns what it did.</summary>
        public static string Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before baking.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != CaseMenuController.ScenePath) throw new InvalidOperationException("Open " + CaseMenuController.ScenePath + " first.");

            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
            if (settings == null) { settings = new LightingSettings { name = "ComputerCaseMenu Lighting" }; AssetDatabase.CreateAsset(settings, SettingsPath); }
            settings.bakedGI = true; settings.realtimeGI = false;
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.lightmapResolution = 24; settings.lightmapPadding = 4; settings.lightmapMaxSize = 1024;
            settings.directSampleCount = 32; settings.indirectSampleCount = 256; settings.environmentSampleCount = 128;
            settings.maxBounces = 2; settings.ao = false; settings.indirectScale = 1;
            settings.lightmapCompression = LightmapCompression.NormalQuality;
            settings.directionalityMode = LightmapsMode.CombinedDirectional;
            settings.mixedBakeMode = MixedLightingMode.Shadowmask;
            EditorUtility.SetDirty(settings);
            Lightmapping.lightingSettings = settings;

            int lights = 0;
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (Array.IndexOf(RoomLights, light.name) < 0) continue;
                light.lightmapBakeType = LightmapBakeType.Mixed;
                // Baked soft shadows come from the light's size, not the real-time soft-shadow filter.
                if (light.type != LightType.Directional) light.shapeRadius = light.shadows != LightShadows.None ? .06f : 0;
                EditorUtility.SetDirty(light); lights++;
            }

            int mapped = 0, probed = 0, unwrapped = 0;
            var done = new HashSet<Mesh>();
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                string root = renderer.transform.root.name;
                if (root != "CaseMenuSet" && root != "Native HongmengOS Monitor") continue;
                if (Moving.Any(m => Under(renderer.transform, m)) || renderer.GetComponent<TMPro.TMP_Text>() != null) continue;
                var go = renderer.gameObject;
                var flags = GameObjectUtility.GetStaticEditorFlags(go) | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;
                GameObjectUtility.SetStaticEditorFlags(go, flags);
                var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                bool big = root == "CaseMenuSet" && renderer.bounds.size.magnitude >= LightmapMinSize && mesh != null;
                if (big && mesh.uv2.Length == 0)
                {
                    string path = AssetDatabase.GetAssetPath(mesh);
                    // Only the room's own generated meshes can be given UVs; anything else is lit by probes.
                    if (path.EndsWith(".asset") && done.Add(mesh)) { Unwrapping.GenerateSecondaryUVSet(mesh); EditorUtility.SetDirty(mesh); unwrapped++; }
                    else if (!done.Contains(mesh)) big = false;
                }
                renderer.receiveGI = big ? ReceiveGI.Lightmaps : ReceiveGI.LightProbes;
                if (big) { renderer.scaleInLightmap = 1; mapped++; } else probed++;
                EditorUtility.SetDirty(renderer);
            }
            AssetDatabase.SaveAssets();

            int probes = PlaceProbes();
            EditorSceneManager.MarkSceneDirty(scene);
            return "case menu bake setup: " + lights + " lights mixed, " + mapped + " lightmapped, " + probed + " on probes, " + unwrapped + " meshes unwrapped, " + probes + " probes";
        }

        static bool Under(Transform t, string name)
        {
            for (; t != null; t = t.parent) if (t.name == name) return true;
            return false;
        }

        /// <summary>A grid over the desk: the monitor, the case's switches and the space the camera sees them in.</summary>
        static int PlaceProbes()
        {
            var bounds = new Bounds(); bool any = false;
            foreach (var name in new[] { "Native HongmengOS Monitor", "StartButton", "SettingsButton" })
            {
                var go = GameObject.Find(name);
                if (go == null) continue;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds); }
            }
            if (!any) return 0;
            bounds.Expand(.5f);
            var group = GameObject.Find("Menu Light Probes");
            if (group == null) group = new GameObject("Menu Light Probes", typeof(LightProbeGroup));
            group.transform.position = Vector3.zero; group.transform.rotation = Quaternion.identity; group.transform.localScale = Vector3.one;
            var points = new List<Vector3>();
            const int nx = 5, ny = 4, nz = 4;
            for (int x = 0; x < nx; x++)
                for (int y = 0; y < ny; y++)
                    for (int z = 0; z < nz; z++)
                        points.Add(bounds.min + Vector3.Scale(bounds.size, new Vector3(x / (nx - 1f), y / (ny - 1f), z / (nz - 1f))));
            var probes = group.GetComponent<LightProbeGroup>();
            probes.probePositions = points.ToArray();
            EditorUtility.SetDirty(probes);
            return points.Count;
        }
    }
}
#endif
