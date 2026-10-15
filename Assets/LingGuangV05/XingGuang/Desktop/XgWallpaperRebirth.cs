using System.Collections.Generic;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    // Transient presentation of real ability events; never inferred from loading a later save.
    public sealed class XgWallpaperRebirth : MonoBehaviour
    {
        static XgWallpaperRebirth active;
        XingGuangController controller;
        XgBrainWallpaper wallpaper;
        XgSim bound;
        XgRebirthMask mask;
        TMP_Text title;
        readonly Queue<int> pending = new Queue<int>();
        sealed class WindowFade
        {
            public CanvasGroup group;
            public bool owned;
            public float baseline, applied;
        }
        readonly Dictionary<Transform, WindowFade> windows = new Dictionary<Transform, WindowFade>();
        float started = -100;
        int ability;
        public static bool Available => active != null && active.isActiveAndEnabled;
        public static bool Playing => Available && active.Showing;
        public bool Showing { get; private set; }
        public float Age => Showing ? Time.unscaledTime - started : -1;
        public int ShownAbility => ability;
        public int Plays { get; private set; }
        public float WindowFactor { get; private set; } = 1;
        public float RevealRadius => mask != null ? mask.Radius : 2;
        public bool Reduced => controller?.View?.Juice != null && controller.View.Juice.Reduced;

        public static void Install(XingGuangController controller, XgBrainWallpaper wallpaper)
        {
            var view = wallpaper.gameObject.AddComponent<XgWallpaperRebirth>();
            view.controller = controller; view.wallpaper = wallpaper; active = view;
            var cover = PrologueDesk.Rect("Ability Rebirth Mask", wallpaper.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            view.mask = cover.gameObject.AddComponent<XgRebirthMask>(); view.mask.raycastTarget = false;
            var label = PrologueDesk.Rect("Ability Rebirth Title", wallpaper.transform, new Vector2(.3f, .43f), new Vector2(.74f, .59f), Vector2.zero, Vector2.zero);
            view.title = label.gameObject.AddComponent<TextMeshProUGUI>();
            view.title.font = PrologueDesk.CjkFont(); view.title.fontSize = 32; view.title.alignment = TextAlignmentOptions.Center;
            view.title.color = XgCortexGraphic.N.Gold; view.title.raycastTarget = false;
            cover.gameObject.SetActive(false); label.gameObject.SetActive(false);
        }

        void OnAbility(int value)
        {
            if (value < 2 || value > XgSim.AbilityCount || pending.Contains(value) || Showing && ability == value) return;
            pending.Enqueue(value);
        }

        void Unbind()
        {
            if (bound != null) bound.AbilityEmerged -= OnAbility;
            bound = null; pending.Clear(); Finish();
        }

        void Finish()
        {
            Showing = false; WindowFactor = 1;
            if (mask != null) { mask.gameObject.SetActive(false); mask.Radius = 2; }
            if (title != null) title.gameObject.SetActive(false);
            foreach (var fade in windows.Values) if (fade.group != null)
            {
                if (Mathf.Approximately(fade.group.alpha, fade.applied)) fade.group.alpha = fade.baseline;
                if (fade.owned) Destroy(fade.group);
            }
            windows.Clear();
        }

        void OnDisable() { Unbind(); }
        void OnDestroy() { Unbind(); if (active == this) active = null; }

        void LateUpdate()
        {
            var sim = controller != null && controller.runtime != null && controller.runtime.isActiveAndEnabled ? controller.Sim : null;
            if (sim != bound) { Unbind(); bound = sim; if (bound != null) bound.AbilityEmerged += OnAbility; }
            if (bound == null || wallpaper == null || !wallpaper.Reveal.Visible) return;
            // Keep temporary cover and title above all brain graphics, below desktop icons and windows.
            mask.transform.SetAsLastSibling(); title.transform.SetAsLastSibling();
            if (!Showing)
            {
                if (pending.Count == 0) return;
                var story = FindAnyObjectByType<StoryDesktopPresenter>();
                if (story != null && story.CutscenePlaying || LoveQuestionCutscene.Playing) return;
                ability = pending.Dequeue(); started = Time.unscaledTime; Showing = true; Plays++;
                mask.gameObject.SetActive(true); title.gameObject.SetActive(true);
            }
            float age = Age;
            if (age >= 3.2f) { Finish(); return; }
            float fadeOut = Mathf.SmoothStep(0, 1, (age - 2.8f) / .4f);
            WindowFactor = Mathf.Lerp(Mathf.Lerp(1, .4f, Mathf.SmoothStep(0, 1, age / .2f)), 1, fadeOut);
            var apps = transform.parent.Find("Apps & Windows");
            if (apps != null) foreach (Transform window in apps)
            {
                if (!window.gameObject.activeInHierarchy) continue;
                if (!windows.TryGetValue(window, out var fade) || fade.group == null)
                {
                    var existing = window.GetComponent<CanvasGroup>();
                    fade = new WindowFade { owned = existing == null, group = existing != null ? existing : window.gameObject.AddComponent<CanvasGroup>() };
                    fade.baseline = fade.applied = fade.group.alpha; windows[window] = fade;
                }
                // Accept a newer value from the window's own animation instead of restoring stale opacity.
                if (!Mathf.Approximately(fade.group.alpha, fade.applied)) fade.baseline = fade.group.alpha;
                fade.applied = fade.baseline * WindowFactor;
                fade.group.alpha = fade.applied;
            }
            mask.Radius = age < .3f ? 0 : Mathf.SmoothStep(0, 1.3f, (age - .3f) / 1.2f);
            mask.Uniform = Reduced;
            mask.Opacity = Reduced ? 1 - Mathf.SmoothStep(0, 1, age / 1.2f) : 1;
            mask.SetVerticesDirty();
            title.text = XgSim.AbilityName(ability, GameText.IsEnglish);
            title.canvasRenderer.SetAlpha(Mathf.SmoothStep(0, 1, (age - .5f) / .4f) * (1 - fadeOut));
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgRebirthMask : MaskableGraphic
    {
        public float Radius = 2, Opacity = 1;
        public bool Uniform;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            var ink = new Color32(5, 12, 23, (byte)Mathf.RoundToInt(255 * Mathf.Clamp01(Opacity)));
            if (Uniform || Radius <= 0) { XgDraw.Box(vh, r.min, r.max, ink); return; }
            var center = r.min + Vector2.Scale(r.size, new Vector2(.52f, .53f));
            float radius = Radius * Mathf.Max(r.width, r.height);
            // Horizontal strips reveal a growing circle without touching any source graphic or concept.
            for (int i = 0; i < 128; i++)
            {
                float y0 = Mathf.Lerp(r.yMin, r.yMax, i / 128f), y1 = Mathf.Lerp(r.yMin, r.yMax, (i + 1) / 128f);
                float dy = Mathf.Abs((y0 + y1) * .5f - center.y);
                float half = dy >= radius ? 0 : Mathf.Sqrt(radius * radius - dy * dy);
                float left = Mathf.Clamp(center.x - half, r.xMin, r.xMax), right = Mathf.Clamp(center.x + half, r.xMin, r.xMax);
                if (left > r.xMin) XgDraw.Box(vh, new Vector2(r.xMin, y0), new Vector2(left, y1), ink);
                if (right < r.xMax) XgDraw.Box(vh, new Vector2(right, y0), new Vector2(r.xMax, y1), ink);
            }
        }
    }
}
