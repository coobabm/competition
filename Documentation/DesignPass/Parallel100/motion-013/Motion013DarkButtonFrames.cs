#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Integration-only probe. Copy into Assets/LocalBuild in an isolated validation checkout.
// Keep this MonoBehaviour outside Editor folders; the entire file is editor-conditional.
// Never starts Play mode, creates controllers, invokes real business code or opens saves.
public sealed class Motion013DarkButtonFrames : MonoBehaviour
{
    [Serializable] public class Sample
    {
        public string id, text, font;
        public float scale, offset, nominalContrast, buttonClearance, maskClearance, nominalFontPixels;
        public bool missingGlyph, culled;
    }
    [Serializable] public class Frame
    {
        public string file, phase;
        public int unityFrame;
        public float seconds, delta;
        public Sample[] samples;
    }
    [Serializable] public class Report
    {
        public string status = "CAPTURED_REQUIRES_VISUAL_REVIEW", unity, font, mode;
        public string[] sourceHashes;
        public int screenWidth, screenHeight, callbacks;
        public float canvasScale;
        public bool compressionObserved, reboundObserved, settled;
        public List<string> reviewFlags = new List<string>();
        public List<Frame> frames = new List<Frame>();
    }
    TMP_FontAsset font;
    GameObject fixture;
    string output;
    readonly List<Texture2D> pending = new List<Texture2D>();
    readonly List<XgBtn> buttons = new List<XgBtn>();
    readonly List<Vector2> rest = new List<Vector2>();
    RectTransform panel;
    XgJuice host;
    EventSystem events;
    int callbacks;
    static readonly string[] Sources = {
        "Assets/LingGuangV05/XingGuang/Desktop/XgUi.cs",
        "Assets/LingGuangV05/XingGuang/Desktop/XgButtonPressFeedback.cs",
        "Assets/LingGuangV05/XingGuang/Desktop/XgJuice.cs"
    };

    [MenuItem("Tools/Design Pass/Motion 013/Capture Dark Button Frames")]
    public static void Run()
    {
        if (!Application.isPlaying || Application.isBatchMode)
            throw new InvalidOperationException("Requires interactive Play mode in an empty validation scene.");
        if (SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().rootCount != 0 ||
            UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).Length != 0)
            throw new InvalidOperationException("Use a truly empty scene; existing runtime behaviours are not allowed.");
        var selected = Selection.activeObject as TMP_FontAsset;
        if (selected == null) throw new InvalidOperationException("Select the AI application's actual TMP font asset first.");
        if (Screen.width < 1024 || Screen.height < 768)
            throw new InvalidOperationException("Use a fixed Game view of at least 1024 x 768.");
        var runner = new GameObject("Motion013_CaptureOnly").AddComponent<Motion013DarkButtonFrames>();
        runner.font = selected;
        runner.output = Path.Combine(Application.dataPath, "../Documentation/DesignPass/Parallel100/motion-013/captures",
            DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ"));
        Directory.CreateDirectory(runner.output);
        runner.StartCoroutine(runner.Guarded());
    }

    IEnumerator Guarded()
    {
        var run = Capture();
        try
        {
            while (true)
            {
                bool next;
                try { next = run.MoveNext(); }
                catch (Exception ex)
                {
                    File.WriteAllText(Path.Combine(output, "ERROR.txt"), ex.ToString());
                    Debug.LogException(ex); break;
                }
                if (!next) break;
                yield return run.Current;
            }
        }
        finally
        {
            (run as IDisposable)?.Dispose();
            Cleanup();
            Destroy(gameObject);
        }
    }

    IEnumerator Capture()
    {
        // Window size and canvas scale are recorded; no global timing or graphics setting is changed.
        foreach (float scale in new[] { .75f, 1f, 1.25f })
        foreach (string mode in new[] { "mouse", "reduced", "rapid", "submit" })
        {
            Build(scale, mode == "reduced");
            var report = new Report { unity = Application.unityVersion, font = AssetDatabase.GetAssetPath(font),
                sourceHashes = Hashes(), mode = mode, screenWidth = Screen.width, screenHeight = Screen.height,
                canvasScale = scale };
            string folder = Path.Combine(output, scale.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "_" + mode);
            Directory.CreateDirectory(folder);
            yield return null;
            yield return new WaitForEndOfFrame();
            AddFrame(report, 0, "rest");
            var down = new PointerEventData(events) { button = PointerEventData.InputButton.Left };
            foreach (var b in buttons)
            {
                ExecuteEvents.Execute(b.rt.gameObject, down, ExecuteEvents.pointerEnterHandler);
                if (mode == "submit") ExecuteEvents.Execute(b.rt.gameObject, new BaseEventData(events), ExecuteEvents.submitHandler);
                else ExecuteEvents.Execute(b.rt.gameObject, down, ExecuteEvents.pointerDownHandler);
            }
            float start = Time.realtimeSinceStartup;
            bool released = mode == "submit";
            int repeats = 0;
            while (Time.realtimeSinceStartup - start < .5f && report.frames.Count < 96)
            {
                yield return new WaitForEndOfFrame();
                float age = Time.realtimeSinceStartup - start;
                AddFrame(report, age, "animation");
                if (mode == "rapid" && repeats < 3 && age >= (repeats + 1) * .05f)
                {
                    foreach (var b in buttons)
                    {
                        ExecuteEvents.Execute(b.rt.gameObject, down, ExecuteEvents.pointerUpHandler);
                        ExecuteEvents.Execute(b.rt.gameObject, down, ExecuteEvents.pointerDownHandler);
                    }
                    repeats++;
                }
                if (!released && age >= (mode == "rapid" ? .22f : .08f))
                {
                    foreach (var b in buttons)
                    {
                        ExecuteEvents.Execute(b.rt.gameObject, down, ExecuteEvents.pointerUpHandler);
                        ExecuteEvents.Execute(b.rt.gameObject, down, ExecuteEvents.pointerClickHandler);
                    }
                    released = true;
                }
            }
            report.callbacks = callbacks;
            report.settled = true;
            foreach (var s in report.frames[report.frames.Count - 1].samples)
                report.settled &= Mathf.Abs(s.scale - 1) < .0001f && s.offset < .0001f;
            if (!report.compressionObserved || !report.reboundObserved) Flag(report, "INSUFFICIENT_PHASE_COVERAGE");
            if (!report.settled) Flag(report, "NOT_SETTLED");
            if (callbacks != buttons.Count) Flag(report, "CALLBACK_COUNT");
            if (mode == "rapid" && repeats != 3) Flag(report, "INSUFFICIENT_RAPID_INPUT_COVERAGE");
            if (report.frames.Count == 96) Flag(report, "FRAME_CAP_REACHED");
            if (string.Join("|", report.sourceHashes) != string.Join("|", Hashes())) Flag(report, "SOURCE_CHANGED_DURING_CAPTURE");
            // Encoding is deliberately deferred until the animation has completed.
            for (int i = 0; i < pending.Count; i++)
            {
                File.WriteAllBytes(Path.Combine(folder, report.frames[i].file), pending[i].EncodeToPNG());
                Destroy(pending[i]);
                yield return null;
            }
            pending.Clear();
            File.WriteAllText(Path.Combine(folder, "frames.json"), JsonUtility.ToJson(report, true));
            Destroy(fixture); fixture = null;
            yield return null;
        }
        File.WriteAllText(Path.Combine(output, "CAPTURE_COMPLETE.txt"), "All 12 fixtures captured. Read per-case flags and inspect PNGs. This is not visual acceptance or a real-page capture.");
        Debug.Log("Motion013 capture complete; visual review required: " + output);
    }

    void Build(float scale, bool reduced)
    {
        buttons.Clear(); rest.Clear(); callbacks = 0;
        fixture = new GameObject("Motion013_DarkFixture", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(EventSystem));
        var canvas = fixture.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fixture.GetComponent<CanvasScaler>().scaleFactor = scale;
        events = fixture.GetComponent<EventSystem>();
        panel = XgUi.Rect("DarkPage", fixture.transform, Vector2.one * .5f, Vector2.one * .5f, new Vector2(-360, -200), new Vector2(360, 200));
        XgUi.Panel(panel, XgDark.Page);
        host = panel.gameObject.AddComponent<XgJuice>(); host.Reduced = reduced;
        var ui = new XgUi(font, null, true);
        Add(ui, "train-small-zh", "它学会了吗", 12, 104, 22, 20, false);
        Add(ui, "train-small-en", "Learning", 12, 104, 22, 70, false);
        Add(ui, "repo-filter", "★ 收藏", 14, 68, 30, 120, false);
        Add(ui, "selected-primary", "加载权重，接着练", 16, 320, 40, 175, true);
        Add(ui, "long-en-stress", "Carry on from its score (no weights kept)", 16, 320, 40, 240, true);
        Add(ui, "mask-edge-stress", "它学会了吗", 12, 104, 22, 310, false, true);
        Canvas.ForceUpdateCanvases();
        foreach (var b in buttons) { b.label.ForceMeshUpdate(); rest.Add(b.label.rectTransform.anchoredPosition); }
    }

    void Add(XgUi ui, string id, string text, float size, float width, float height, float y, bool accent, bool masked = false)
    {
        Transform parent = panel;
        if (masked)
        {
            var mask = XgUi.TopLeft("ArtificialTightMask", panel, 24, y, width, height);
            mask.gameObject.AddComponent<RectMask2D>(); parent = mask;
        }
        var b = ui.Button(parent, text, () => callbacks++, size); b.rt.name = id;
        XgUi.PlaceTopLeft(b, masked ? 0 : 24, masked ? 0 : y, width, height);
        b.button.navigation = new Navigation { mode = Navigation.Mode.None };
        if (accent) b.Set(text, true, XgDark.Accent, Color.white);
        buttons.Add(b);
    }

    void AddFrame(Report report, float age, string phase)
    {
        if (Screen.width != report.screenWidth || Screen.height != report.screenHeight)
            throw new InvalidOperationException("Game view size changed during capture; rerun at a fixed size.");
        var f = new Frame { file = report.frames.Count.ToString("D3") + ".png", phase = phase,
            unityFrame = Time.frameCount, seconds = age, delta = Time.unscaledDeltaTime, samples = new Sample[buttons.Count] };
        if (phase != "rest" && f.delta > .025f) Flag(report, "LOW_FRAME_RATE_OR_READBACK_STALL");
        for (int i = 0; i < buttons.Count; i++)
        {
            var b = buttons[i]; var label = b.label; var rt = label.rectTransform;
            var s = new Sample { id = b.rt.name, text = label.text, font = label.font.name,
                scale = rt.localScale.x, offset = Vector2.Distance(rest[i], rt.anchoredPosition),
                nominalFontPixels = label.fontSize * report.canvasScale * rt.localScale.y,
                buttonClearance = float.MaxValue, maskClearance = float.MaxValue,
                culled = label.canvasRenderer.cull,
                nominalContrast = Contrast(label.color, b.image.color * b.image.canvasRenderer.GetColor()) };
            var masks = label.GetComponentsInParent<RectMask2D>();
            for (int c = 0; c < label.textInfo.characterCount; c++)
            {
                var ch = label.textInfo.characterInfo[c];
                if (ch.character == '\ufffd' || ch.character == '\u25a1' ||
                    (ch.textElement != null && (ch.textElement.unicode == 0xfffd || ch.textElement.unicode == 0x25a1)))
                    s.missingGlyph = true;
                if (!ch.isVisible) continue;
                foreach (var v in new[] { ch.bottomLeft, ch.topLeft, ch.topRight, ch.bottomRight })
                {
                    Vector3 world = rt.TransformPoint(v);
                    s.buttonClearance = Mathf.Min(s.buttonClearance, Clearance(b.rt, world));
                    foreach (var mask in masks) if (mask.isActiveAndEnabled)
                        s.maskClearance = Mathf.Min(s.maskClearance, Clearance(mask.rectTransform, world));
                }
            }
            if (masks.Length == 0) s.maskClearance = 99999;
            if (phase != "rest")
            {
                report.compressionObserved |= s.scale < .999f;
                report.reboundObserved |= s.scale > 1.001f;
            }
            float budget = report.mode == "reduced" ? .02f : .05f;
            if (Mathf.Abs(s.scale - 1) > budget || s.offset > 1.01f) Flag(report, s.id + ":MOTION_ENVELOPE");
            if (s.buttonClearance < 0) Flag(report, s.id + ":GLYPH_OUTSIDE_BUTTON");
            if (s.maskClearance < 0 || s.culled) Flag(report, s.id + ":CLIP_REVIEW");
            if (s.missingGlyph) Flag(report, s.id + ":POSSIBLE_MISSING_GLYPH");
            if (s.nominalContrast < 4.5f) Flag(report, s.id + ":NOMINAL_CONTRAST_BELOW_4_5");
            f.samples[i] = s;
        }
        var corners = new Vector3[4]; panel.GetWorldCorners(corners);
        int x = Mathf.FloorToInt(corners[0].x), y = Mathf.FloorToInt(corners[0].y);
        int w = Mathf.CeilToInt(corners[2].x) - x, h = Mathf.CeilToInt(corners[2].y) - y;
        var texture = new Texture2D(w, h, TextureFormat.RGB24, false);
        pending.Add(texture);
        texture.ReadPixels(new Rect(x, y, w, h), 0, 0, false); texture.Apply(false);
        report.frames.Add(f);
    }

    static float Clearance(RectTransform bounds, Vector3 world)
    {
        Vector2 p = bounds.InverseTransformPoint(world); Rect r = bounds.rect;
        return Mathf.Min(p.x - r.xMin, r.xMax - p.x, p.y - r.yMin, r.yMax - p.y);
    }
    static float Contrast(Color a, Color b)
    {
        float x = Luminance(a), y = Luminance(b);
        return (Mathf.Max(x, y) + .05f) / (Mathf.Min(x, y) + .05f);
    }
    static float Luminance(Color c) => .2126f * Linear(c.r) + .7152f * Linear(c.g) + .0722f * Linear(c.b);
    static float Linear(float v) => v <= .04045f ? v / 12.92f : Mathf.Pow((v + .055f) / 1.055f, 2.4f);
    static void Flag(Report r, string flag) { if (!r.reviewFlags.Contains(flag)) r.reviewFlags.Add(flag); }
    static string[] Hashes()
    {
        var values = new List<string>();
        using (var sha = SHA256.Create()) foreach (string p in Sources)
            values.Add(p + "=" + BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(Application.dataPath, "..", p)))).Replace("-", ""));
        return values.ToArray();
    }
    void Cleanup()
    {
        foreach (var t in pending) if (t != null) Destroy(t);
        pending.Clear(); if (fixture != null) Destroy(fixture);
    }
    void OnDestroy() { Cleanup(); }
}
#endif
