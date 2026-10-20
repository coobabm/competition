#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Real Resources import and inactive Graphic fixture checks; never loads or writes a player save.</summary>
public static class TechIconResourceChecks
{
    [Serializable] sealed class Node { public string id, entry; }
    [Serializable] sealed class Manifest { public int nodeCount, entryCount; public Node[] nodes; }
    [Serializable] sealed class Signals { public XgTechIconSignal.Path[] nodes; }
    [Serializable] sealed class Personas { public XgPersonaPack[] packs; public XgPersonaTag[] tags; }
    [Serializable] sealed class Unlocks { public XgChatUnlock[] unlocks; }
    [Serializable] sealed class Report
    {
        public string status = "running", error = "";
        public int loadedTextures, configuredSignals, runtimeEntries;
        public List<string> checks = new List<string>();
        public string boundary = "EditMode Resources and inactive component/mesh checks only; no claim of PlayMode timing, rendered hover quality, purchase feedback or focus-event acceptance.";
    }
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    static void Need(Report r, bool pass, string message)
    { if (!pass) throw new InvalidOperationException(message); r.checks.Add(message); }
    static string Output()
    {
        string requested = null; var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--verification-save-dir" && i + 1 < args.Length) requested = args[++i];
            else if (args[i].StartsWith("--verification-save-dir=", StringComparison.Ordinal)) requested = args[i].Substring("--verification-save-dir=".Length);
        }
        if (string.IsNullOrWhiteSpace(requested)) throw new ArgumentException("Supply external --verification-save-dir.");
        string root = Path.GetFullPath(requested), project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (root.Equals(project, StringComparison.OrdinalIgnoreCase) || root.StartsWith(project + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Output must be outside project.");
        return Path.Combine(root, "TechIconResources-" + Guid.NewGuid().ToString("N"));
    }
    [MenuItem("Tools/Local Verification/Technology icon resources")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run in EditMode.");
        var report = new Report(); string output = Output(); Directory.CreateDirectory(output);
        GameObject fixture = null;
        string previousPersonas = null, previousUnlocks = null;
        bool catalogChanged = false;
        try
        {
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(project, "Design/ui/tech-icon-atlas/manifest.json")));
            Need(report, manifest?.nodes != null && manifest.nodes.Length == 211 && manifest.nodeCount == 211, "Authored manifest contains 211 nodes");
            var entries = manifest.nodes.Select(n => n.entry).Distinct().ToArray();
            Need(report, entries.Length == 186 && manifest.entryCount == 186, "Authored manifest contains 186 aggregate entry icons");
            foreach (var entry in entries)
            {
                var texture = Resources.Load<Texture2D>("LingGuangV05/Icons/tech/" + XgIcons.FileName(entry));
                Need(report, texture != null && texture.width > 0 && texture.height > 0, "Resources texture: " + entry);
                var sprite = XgIcons.Get("tech", entry);
                Need(report, sprite != null && sprite.texture == texture, "Actual XgIcons lookup: " + entry);
                report.loadedTextures++;
            }
            var text = Resources.Load<TextAsset>("LingGuangV05/Icons/tech/signal_paths");
            Need(report, text != null, "Signal JSON imports as Resources TextAsset");
            var signals = JsonUtility.FromJson<Signals>(text.text);
            Need(report, signals?.nodes != null && signals.nodes.Length == 211 && signals.nodes.Select(n => n.id).Distinct().Count() == 211, "211 distinct runtime signal paths");
            foreach (var path in signals.nodes)
                Need(report, path.points != null && path.points.Length >= 2 && path.activeMs == 1000 && path.restMs == 1600 && path.points.All(p => !float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.x) && !float.IsInfinity(p.y)), "Valid sampled path and cycle: " + path.id);

            // Eight persona nodes are registered from YY Resources during normal game startup.
            // A fresh EditMode process has no such startup; load the real data before testing its
            // actual catalog mappings, then restore the pre-test catalog in finally.
            var personas = Resources.Load<TextAsset>("LingGuangV05/YY/persona_packs");
            var unlocks = Resources.Load<TextAsset>("LingGuangV05/YY/chat_unlocks");
            Need(report, personas != null && unlocks != null, "Dynamic persona catalog Resources are imported");
            previousPersonas = JsonUtility.ToJson(new Personas { packs = XgSim.PersonaPacks.ToArray(), tags = XgSim.PersonaTags.ToArray() });
            previousUnlocks = JsonUtility.ToJson(new Unlocks { unlocks = XgSim.ChatUnlocks.ToArray() });
            catalogChanged = true;
            XgSim.LoadYyLearning(personas.text, unlocks.text);
            Need(report, XgSim.PersonaPacks.Count == 8, "Real YY resource data registers eight persona packs");

            fixture = new GameObject("Inactive technology icon verification", typeof(RectTransform));
            fixture.SetActive(false);
            var signal = fixture.AddComponent<XgTechIconSignal>();
            Need(report, fixture.GetComponent<CanvasRenderer>() != null, "Adding signal automatically adds CanvasRenderer");
            ((RectTransform)fixture.transform).sizeDelta = new Vector2(64, 64);
            var pathField = typeof(XgTechIconSignal).GetField("path", Private);
            Need(report, pathField != null, "Signal declares its configured path field");
            foreach (var node in manifest.nodes)
            {
                var entry = XgTechTree.EntryOf(node.id);
                Need(report, entry != null && entry.id == node.entry, "Current catalog-to-icon mapping: " + node.id);
                signal.Configure(null, node.id, false, false);
                Need(report, pathField.GetValue(signal) is XgTechIconSignal.Path p && p.id == node.id && !signal.raycastTarget, "Actual component config: " + node.id);
                report.configuredSignals++;
            }
            report.runtimeEntries = XgTechTree.Entries.Count;
            signal.Configure(null, "pack.sudoku", false, false); signal.Show(true);
            // Invoke geometry at fixed phases; this does not pretend to exercise real elapsed time.
            // Graphic also defines the legacy Mesh overload; select the authored override exactly.
            var populate = typeof(XgTechIconSignal).GetMethod("OnPopulateMesh", Private | BindingFlags.DeclaredOnly,
                null, new[] { typeof(VertexHelper) }, null);
            Need(report, populate != null, "Exact VertexHelper mesh override is present");
            var phase = typeof(XgTechIconSignal).GetField("phase", Private);
            var reduced = typeof(XgTechIconSignal).GetField("reduced", Private);
            Need(report, phase != null && reduced != null, "Signal declares fixed-phase and reduced-motion fields");
            using (var mesh = new VertexHelper())
            {
                phase.SetValue(signal, .5f); populate.Invoke(signal, new object[] { mesh });
                Need(report, mesh.currentVertCount > 0, "Fixed active phase produces icon-local geometry");
                phase.SetValue(signal, 1.5f); populate.Invoke(signal, new object[] { mesh });
                Need(report, mesh.currentVertCount == 0, "Fixed rest phase produces no signal geometry");
                reduced.SetValue(signal, true); populate.Invoke(signal, new object[] { mesh });
                Need(report, mesh.currentVertCount > 0, "Reduced mode produces static trace geometry");
                signal.Configure(null, "pack.sudoku", true, false); populate.Invoke(signal, new object[] { mesh });
                Need(report, !signal.enabled && mesh.currentVertCount == 0, "Mystery configuration hides signal");
                signal.Configure(null, "pack.sudoku", false, false); signal.Show(false); populate.Invoke(signal, new object[] { mesh });
                Need(report, !signal.enabled && mesh.currentVertCount == 0, "Pointer-exit signal reset clears geometry");
            }
            report.status = "passed";
        }
        catch (Exception ex) { report.status = "failed"; report.error = ex.ToString(); throw; }
        finally
        {
            if (fixture != null) UnityEngine.Object.DestroyImmediate(fixture);
            if (catalogChanged) XgSim.LoadYyLearning(previousPersonas, previousUnlocks);
            File.WriteAllText(Path.Combine(output, "report.json"), JsonUtility.ToJson(report, true));
            Debug.Log("TECH_ICON_RESOURCES " + report.status + " " + output);
        }
    }
}
#endif
