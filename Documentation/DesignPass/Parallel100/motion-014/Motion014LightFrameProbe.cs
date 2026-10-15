// Candidate validation component: integrate into an isolated validation scene only.
// Does not construct a game controller, read saves, or invoke production business callbacks.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.Zhongbao;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class Motion014LightFrameProbe : MonoBehaviour
{
    public TMP_FontAsset font; // Assign the same Chinese-capable font as ZhongbaoView.
    public bool english;
    [Range(.5f, 2f)] public float fixtureScale = 1;
    const string Output = "Documentation/DesignPass/Parallel100/motion-014/captures";
    readonly List<XgBtn> buttons = new List<XgBtn>();
    readonly List<Vector3> scales = new List<Vector3>();
    readonly List<Vector2> positions = new List<Vector2>();
    GameObject canvasObject, eventsObject;
    XgJuice host;
    CanvasGroup group;
    EventSystem events;
    RectTransform mask;
    string directory;
    int sequence, callbacks;
    bool running;

    [Serializable] public class Frame
    {
        public string stage, unity, colorSpace;
        public int sequence, frame, width, height, callbacks;
        public float realTime, deltaTime, fixtureScale;
        public bool reduced;
        public List<Sample> buttons = new List<Sample>();
    }
    [Serializable] public class Sample
    {
        public string id, text;
        public bool active, interactable, rendererCull, glyphsOutsideButton, glyphsOutsideMask;
        public int visibleGlyphs, missingGlyphs;
        public Vector3 scale;
        public Vector2 position;
        public float groupAlpha, nominalContrast;
        public Color textColor, imageColor, rendererTint;
        public Rect buttonScreen, glyphScreen;
    }

    [ContextMenu("Capture isolated light feedback frames")]
    public void Capture()
    {
        if (running) return;
        if (!Application.isPlaying || font == null)
            throw new InvalidOperationException("Play an empty validation scene and assign the production font first.");
        var roots = gameObject.scene.GetRootGameObjects();
        if (SceneManager.sceneCount != 1 || roots.Length != 1 || roots[0] != gameObject || transform.childCount != 0)
            throw new InvalidOperationException("Only this empty probe root may exist in the validation scene.");
        running = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        directory = Path.Combine(Application.dataPath, "..", Output,
            DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "status.txt"), "INCOMPLETE: capture started; visual acceptance is pending.");
        try
        {
            Build();
            yield return Series("rest", 8);
            Send(ExecuteEvents.pointerEnterHandler);
            yield return Series("hover", 8);
            Send(ExecuteEvents.pointerDownHandler);
            yield return Series("held-press-and-rebound", 18);
            Send(ExecuteEvents.pointerUpHandler);
            Send(ExecuteEvents.pointerClickHandler);
            Send(ExecuteEvents.pointerExitHandler);
            yield return Series("release-rest", 18);
            AssertRest();

            foreach (var b in buttons) b.button.OnSubmit(new BaseEventData(events));
            yield return Series("submit-and-rebound", 18);
            AssertRest();
            host.Reduced = true;
            Send(ExecuteEvents.pointerDownHandler);
            yield return Series("reduced-held", 18);
            Send(ExecuteEvents.pointerUpHandler);
            yield return Series("reduced-rest", 8);
            AssertRest();
            host.Reduced = false;

            Send(ExecuteEvents.pointerDownHandler);
            yield return Series("before-hide", 2);
            group.alpha = 0;
            host.Visible = () => false;
            yield return Series("hidden", 8);
            AssertRest();
            group.alpha = 1;
            host.Visible = () => true;
            yield return Series("reopened", 8);

            Send(ExecuteEvents.pointerUpHandler);
            Send(ExecuteEvents.pointerDownHandler);
            yield return Series("before-group-disable", 2);
            group.interactable = false;
            yield return Series("group-disabled", 8);
            AssertRest();
            group.interactable = true;
            Send(ExecuteEvents.pointerUpHandler);
            yield return Series("final-rest", 8);
            AssertRest();
            File.WriteAllText(Path.Combine(directory, "status.txt"),
                "CAPTURE_COMPLETE: real rendered frames and transform restoration checks completed. " +
                "Visual acceptance, contrast review, input-device routing and real-page integration remain pending.");
        }
        finally
        {
            if (canvasObject != null) Destroy(canvasObject);
            if (eventsObject != null) Destroy(eventsObject);
            buttons.Clear(); scales.Clear(); positions.Clear(); running = false;
        }
    }

    void Build()
    {
        sequence = callbacks = 0;
        canvasObject = new GameObject("Motion014Fixture", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000, 700); scaler.matchWidthOrHeight = .5f;
        var root = (RectTransform)canvasObject.transform;
        XgUi.Panel(root, ZhongbaoSkin.Page);
        var page = XgUi.Rect("Page", root, Vector2.zero, Vector2.one, new Vector2(30, 30), new Vector2(-30, -30));
        page.localScale = Vector3.one * fixtureScale;
        group = page.gameObject.AddComponent<CanvasGroup>();
        host = page.gameObject.AddComponent<XgJuice>(); // Knock requires no Init/audio/particles.
        eventsObject = new GameObject("Motion014Events", typeof(EventSystem));
        eventsObject.transform.SetParent(transform, false);
        events = eventsObject.GetComponent<EventSystem>(); // No input module; events below are synthetic.
        var ui = new XgUi(font, null);
        Add(ui, page, "filter", english ? "Can sign" : "能签", 72, 26, Color.white, ZhongbaoSkin.Ink, true);
        Add(ui, page, "selected-filter", english ? "Pictures" : "看图", 72, 26, ZhongbaoSkin.Blue3, ZhongbaoSkin.Blue, true);
        Add(ui, page, "primary", english ? "Sign" : "签约", 150, 28, ZhongbaoSkin.Blue, Color.white);
        Add(ui, page, "train-link", english ? "Train in LingGuang ↗" : "去灵光训练 ↗", 150, 28, Color.white, ZhongbaoSkin.Blue, true);
        Add(ui, page, "default", english ? "Buy ×10" : "购买 ×10", 150, 28, XgPalette.Button, XgPalette.Ink);
        Add(ui, page, "gold-existing-palette", english ? "Raise → ×2" : "加薪 → ×2", 200, 32, ZhongbaoSkin.Gold, Color.white, size: 14);
        Add(ui, page, "green-existing-palette", english ? "Buy ¥100" : "购买 ¥100", 150, 32, XgPalette.Good, Color.white, size: 14);
        var disabled = Add(ui, page, "disabled", english ? "Unavailable" : "尚未解锁", 150, 28, Color.white, ZhongbaoSkin.Ink);
        disabled.Set(disabled.label.text, false);
        // Intentionally partly clipped: detects text escaping the real RectMask2D, not a fully visible-button assertion.
        mask = XgUi.TopLeft("Viewport", page, 350, 60, 185, 26);
        XgUi.Panel(mask, Color.white);
        mask.gameObject.AddComponent<RectMask2D>();
        var clipped = Add(ui, mask, "mask-edge", english ? "Train in LingGuang ↗" : "去灵光训练 ↗", 150, 28, Color.white, ZhongbaoSkin.Blue, true);
        XgUi.PlaceTopLeft(clipped, 50, 8, 150, 28);
        Canvas.ForceUpdateCanvases();
        foreach (var b in buttons) { b.label.ForceMeshUpdate(); scales.Add(b.label.rectTransform.localScale); positions.Add(b.label.rectTransform.anchoredPosition); }
    }

    XgBtn Add(XgUi ui, RectTransform parent, string id, string text, float width, float height, Color fill, Color ink, bool outlined = false, float size = 13)
    {
        var b = ui.Button(parent, text, () => callbacks++, size);
        b.rt.name = id;
        XgUi.PlaceTopLeft(b, 20, 20 + buttons.Count * 60, width, height);
        b.Set(text, true, fill, ink);
        if (outlined)
        {
            var rim = b.rt.gameObject.AddComponent<Outline>();
            rim.effectColor = id == "selected-filter" ? ZhongbaoSkin.Blue : id == "filter" ? ZhongbaoSkin.Line : ZhongbaoSkin.Blue2;
            rim.effectDistance = new Vector2(1, -1);
        }
        b.button.navigation = new Navigation { mode = Navigation.Mode.None };
        buttons.Add(b); return b;
    }

    void Send<T>(ExecuteEvents.EventFunction<T> action) where T : IEventSystemHandler
    {
        foreach (var b in buttons)
        {
            var e = new PointerEventData(events) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, b.rt.position) };
            ExecuteEvents.Execute(b.rt.gameObject, e, action);
        }
    }

    IEnumerator Series(string stage, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return null; // Real player-loop Update/LateUpdate; no manual effect evaluation.
            yield return new WaitForEndOfFrame();
            var data = new Frame { stage = stage, sequence = sequence++, frame = Time.frameCount,
                realTime = Time.realtimeSinceStartup, deltaTime = Time.unscaledDeltaTime,
                width = Screen.width, height = Screen.height, fixtureScale = fixtureScale,
                unity = Application.unityVersion, colorSpace = QualitySettings.activeColorSpace.ToString(),
                callbacks = callbacks, reduced = host.Reduced };
            foreach (var b in buttons) data.buttons.Add(Measure(b));
            var stem = Path.Combine(directory, data.sequence.ToString("D4") + "-" + stage);
            Texture2D shot = null;
            try { shot = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(stem + ".png", shot.EncodeToPNG()); }
            finally { if (shot != null) Destroy(shot); }
            File.WriteAllText(stem + ".json", JsonUtility.ToJson(data, true));
        }
    }

    Sample Measure(XgBtn b)
    {
        var t = b.label;
        var s = new Sample { id = b.rt.name, text = t.text, active = b.rt.gameObject.activeInHierarchy,
            interactable = b.button.IsInteractable(), rendererCull = t.canvasRenderer.cull,
            scale = t.rectTransform.localScale, position = t.rectTransform.anchoredPosition,
            groupAlpha = group.alpha, textColor = t.color, imageColor = b.image.color,
            rendererTint = b.image.canvasRenderer.GetColor(), buttonScreen = ScreenRect(b.rt) };
        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < t.textInfo.characterCount; i++)
        {
            var c = t.textInfo.characterInfo[i];
            if (!char.IsWhiteSpace(c.character) && !font.HasCharacter(c.character, true)) s.missingGlyphs++;
            if (!c.isVisible) continue;
            s.visibleGlyphs++;
            foreach (var p in new[] { c.bottomLeft, c.topLeft, c.topRight, c.bottomRight })
            {
                Vector2 v = RectTransformUtility.WorldToScreenPoint(null, t.transform.TransformPoint(p));
                min = Vector2.Min(min, v); max = Vector2.Max(max, v);
            }
        }
        if (s.visibleGlyphs > 0)
        {
            s.glyphScreen = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            s.glyphsOutsideButton = !Contains(s.buttonScreen, s.glyphScreen);
            s.glyphsOutsideMask = b.rt.IsChildOf(mask) && !Contains(ScreenRect(mask), s.glyphScreen);
        }
        // Nominal opaque sRGB style diagnostic, NOT sampled pixel contrast or an accessibility verdict.
        var bg = b.image.color * s.rendererTint;
        s.nominalContrast = (Mathf.Max(Luminance(t.color), Luminance(bg)) + .05f) /
            (Mathf.Min(Luminance(t.color), Luminance(bg)) + .05f);
        return s;
    }

    static Rect ScreenRect(RectTransform rt)
    {
        var corners = new Vector3[4]; rt.GetWorldCorners(corners);
        var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (var p in corners) { var q = RectTransformUtility.WorldToScreenPoint(null, p); min = Vector2.Min(min, q); max = Vector2.Max(max, q); }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    static bool Contains(Rect outer, Rect inner) => inner.xMin >= outer.xMin - .5f && inner.xMax <= outer.xMax + .5f && inner.yMin >= outer.yMin - .5f && inner.yMax <= outer.yMax + .5f;
    static float Linear(float x) => x <= .04045f ? x / 12.92f : Mathf.Pow((x + .055f) / 1.055f, 2.4f);
    static float Luminance(Color c) => .2126f * Linear(c.r) + .7152f * Linear(c.g) + .0722f * Linear(c.b);
    void AssertRest()
    {
        for (int i = 0; i < buttons.Count; i++)
            if (Vector3.Distance(buttons[i].label.rectTransform.localScale, scales[i]) > .0001f ||
                Vector2.Distance(buttons[i].label.rectTransform.anchoredPosition, positions[i]) > .0001f)
                throw new InvalidOperationException("Label failed to restore: " + buttons[i].rt.name);
    }
}
