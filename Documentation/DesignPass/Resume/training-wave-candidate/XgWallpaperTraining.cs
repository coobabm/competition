using System.Collections.Generic;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Non-interactive readout of actual training starts, concept births and R4 removals.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgWallpaperTraining : MaskableGraphic
    {
        public const float WaveSeconds = .6f, LifeSeconds = .7f;
        const int MaxLives = 128, MaxWaves = 4;
        sealed class Life
        {
            public XgConceptTransition change;
            public float started;
            public Vector2 retiredUv;
        }
        struct Wave { public float started, strength; }
        readonly List<Life> lives = new List<Life>();
        readonly Dictionary<int, Life> births = new Dictionary<int, Life>();
        readonly List<Wave> waves = new List<Wave>();
        XingGuangController controller;
        XgBrainWallpaper wallpaper;
        XgWallpaperConcepts concepts;
        XgBrainWallpaperPresentation presentation;
        ChapterOneRuntime runtime;
        XgSim source;
        XgBoard board;
        bool refreshConcepts;
        bool Reduced => controller?.View?.Juice != null && controller.View.Juice.Reduced;

        public static void Install(XingGuangController controller, XgBrainWallpaper wallpaper)
        {
            var rect = PrologueDesk.Rect("Training Readout", wallpaper.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rect.SetSiblingIndex(1);
            var view = rect.gameObject.AddComponent<XgWallpaperTraining>();
            view.raycastTarget = false; view.controller = controller; view.wallpaper = wallpaper;
            view.concepts = wallpaper.GetComponentInChildren<XgWallpaperConcepts>();
            view.presentation = wallpaper.GetComponent<XgBrainWallpaperPresentation>();
            if (view.concepts != null) view.concepts.TrainingVisual = view;
            view.Observe();
        }

        protected override void OnEnable() { base.OnEnable(); Observe(); }
        protected override void OnDisable() { Detach(); base.OnDisable(); }
        protected override void OnDestroy()
        {
            Detach();
            if (concepts != null && concepts.TrainingVisual == this) concepts.TrainingVisual = null;
            base.OnDestroy();
        }

        void Clear()
        {
            lives.Clear(); births.Clear(); waves.Clear(); refreshConcepts = false;
            SetVerticesDirty(); if (concepts != null) concepts.SetVerticesDirty();
        }
        void Detach()
        {
            if (runtime != null) runtime.Changed -= Observe;
            if (source != null) source.EpochStarted -= Started;
            if (board != null) board.ConceptsChanged -= Changed;
            runtime = null; source = null; board = null; Clear();
        }
        void Observe()
        {
            var current = controller != null ? controller.runtime : null;
            var sim = isActiveAndEnabled && current != null && current.isActiveAndEnabled && current.Sim != null ? controller.Sim : null;
            var model = sim?.Board;
            if (runtime == current && ReferenceEquals(source, sim) && ReferenceEquals(board, model)) return;
            Detach(); if (!isActiveAndEnabled) return;
            runtime = current; source = sim; board = model;
            if (runtime != null) runtime.Changed += Observe;
            if (source != null) source.EpochStarted += Started;
            if (board != null) board.ConceptsChanged += Changed;
        }
        bool Visible => isActiveAndEnabled && wallpaper != null && wallpaper.Reveal.Visible
            && runtime != null && runtime.isActiveAndEnabled && ReferenceEquals(source, controller.Sim);

        void Started(XgEpoch epoch)
        {
            if (!Visible || epoch == null) return;
            if (waves.Count == MaxWaves) waves.RemoveAt(0);
            waves.Add(new Wave { started = Time.unscaledTime, strength = epoch.hand ? 1 : .35f });
            ApplyBrightness(); SetVerticesDirty();
        }
        void Changed(IReadOnlyList<XgConceptTransition> changes)
        {
            if (!Visible || !ReferenceEquals(board, source.Board)) return;
            foreach (var change in changes)
            {
                lives.RemoveAll(life => life.change.Id == change.Id);
                births.Remove(change.Id);
                var life = new Life { change = change, started = Time.unscaledTime };
                // A removed cell uses its last actually displayed location; never invent a missing LOD cell.
                if (change.Kind == XgConceptTransitionKind.Decayed && !TryUv(change.Id, out life.retiredUv)) continue;
                if (lives.Count == MaxLives) { births.Remove(lives[0].change.Id); lives.RemoveAt(0); }
                lives.Add(life);
                if (change.Kind != XgConceptTransitionKind.Decayed) births[change.Id] = life;
            }
            refreshConcepts = true;
            ApplyBrightness(); SetVerticesDirty();
        }
        void Update()
        {
            Observe();
            if (!Visible) { if (lives.Count > 0 || waves.Count > 0) Clear(); return; }
            bool hadLife = lives.Count > 0;
            bool active = hadLife || waves.Count > 0;
            for (int i = lives.Count - 1; i >= 0; i--)
                if (Time.unscaledTime - lives[i].started >= LifeSeconds)
                { births.Remove(lives[i].change.Id); lives.RemoveAt(i); }
            waves.RemoveAll(wave => Time.unscaledTime - wave.started >= WaveSeconds);
            if (active) SetVerticesDirty();
            if (hadLife && concepts != null) concepts.SetVerticesDirty();
        }
        void LateUpdate()
        {
            if (refreshConcepts && concepts != null)
            {
                refreshConcepts = false;
                // Training has finished mutating the board for this frame. Refresh its read-only map once.
                concepts.RefreshTrainingSnapshot(); SetVerticesDirty();
            }
            if (lives.Count > 0 || waves.Count > 0) ApplyBrightness();
        }
        void ApplyBrightness() => canvasRenderer.SetAlpha(presentation != null ? presentation.LightLevel : 1);

        public float CellOpacity(int id)
        {
            if (!isActiveAndEnabled || Reduced) return 1;
            if (births.TryGetValue(id, out var life)) return Mathf.SmoothStep(0, 1, (Age(life) - .5f) * 2);
            return 1;
        }
        static float Age(Life life) => Mathf.Clamp01((Time.unscaledTime - life.started) / LifeSeconds);
        bool TryUv(int id, out Vector2 uv)
        {
            uv = default;
            if (concepts == null || !concepts.TryLocalPosition(id, out var point)) return false;
            var area = concepts.rectTransform.rect;
            if (area.width <= 0 || area.height <= 0) return false;
            uv = new Vector2((point.x - area.xMin) / area.width, (point.y - area.yMin) / area.height);
            return true;
        }
        Vector2 Point(Vector2 uv) => rectTransform.rect.min + Vector2.Scale(rectTransform.rect.size, uv);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); bool reduced = Reduced;
            float scale = Mathf.Min(rectTransform.rect.width / 1920, rectTransform.rect.height / 1080);
            foreach (var wave in waves) DrawWave(vh, wave, scale, reduced);
            foreach (var life in lives)
            {
                float age = Age(life); if (age >= 1) continue;
                var change = life.change;
                if (change.Kind == XgConceptTransitionKind.Decayed)
                {
                    var tint = ChangeTint(change);
                    tint = Color.Lerp(tint, new Color(.45f, .48f, .53f), Mathf.Clamp01(age * 2));
                    tint.a = (1 - age) * (reduced ? .5f : 1);
                    XgDraw.Disc(vh, Point(life.retiredUv), Mathf.Lerp(4, 1.5f, age) * scale, tint, 10);
                    continue;
                }
                if (!TryUv(change.Id, out var uv)) continue;
                Vector2 target = Point(uv), origin = target;
                var ink = XgCortexGraphic.N.Teal;
                if (change.Kind == XgConceptTransitionKind.Merged
                    && TryUv(change.ParentA, out var a) && TryUv(change.ParentB, out var b))
                {
                    Vector2 start = Point(a), end = Point(b);
                    origin = (start + end) * .5f;
                    var line = ink; line.a = (reduced ? .16f : .45f) * (1 - age);
                    XgDraw.Seg(vh, start, reduced ? end : Vector2.Lerp(start, end, Mathf.Clamp01(age * 4)), 1.4f * scale, line);
                }
                Vector2 at = reduced ? target : Vector2.Lerp(origin, target, Mathf.SmoothStep(0, 1, age / .65f));
                var glow = ink; glow.a = (reduced ? .07f : .2f) * (1 - age);
                XgDraw.Disc(vh, at, (reduced ? 7 : 14) * scale, glow, 10);
                ink = Color.Lerp(ChangeTint(change), Color.white, .6f);
                ink.a = (reduced ? .35f : 1) * (1 - age);
                XgDraw.Disc(vh, at, Mathf.Lerp(2, 5, Mathf.Sin(age * Mathf.PI)) * scale, ink, 10);
            }
        }

        void DrawWave(VertexHelper vh, Wave wave, float scale, bool reduced)
        {
            float age = Mathf.Clamp01((Time.unscaledTime - wave.started) / WaveSeconds);
            if (age >= 1) return;
            var ink = XgCortexGraphic.N.Teal;
            float envelope = Mathf.Sin(age * Mathf.PI) * wave.strength;
            if (reduced)
            {
                ink.a = envelope * .12f;
                XgDraw.Seg(vh, Point(new Vector2(.32f, .53f)), Point(new Vector2(.72f, .53f)), 2 * scale, ink);
                return;
            }
            float x = Mathf.Lerp(.32f, .72f, age);
            float height = .31f * Mathf.Sqrt(Mathf.Max(0, 1 - Mathf.Pow((x - .52f) / .25f, 2)));
            for (int i = 0; i < 24; i++)
            {
                float t0 = (float)i / 24, t1 = (float)(i + 1) / 24;
                Vector2 a = Point(new Vector2(x + .018f * Mathf.Sin(t0 * Mathf.PI), .55f + (t0 * 2 - 1) * height));
                Vector2 b = Point(new Vector2(x + .018f * Mathf.Sin(t1 * Mathf.PI), .55f + (t1 * 2 - 1) * height));
                ink.a = envelope * Mathf.Sin((t0 + t1) * .5f * Mathf.PI) * .6f;
                var glow = ink; glow.a *= .2f;
                XgDraw.Seg(vh, a, b, 10 * scale, glow); XgDraw.Seg(vh, a, b, 2 * scale, ink);
            }
        }
        static Color ChangeTint(XgConceptTransition change)
        {
            float strength = Mathf.Sqrt(Mathf.Clamp01((float)(System.Math.Abs(change.Weight) / XgBoard.WeightLimit))
                * Mathf.Clamp01((float)(change.Strength / 10)));
            var ink = change.Weight > 0 ? XgCortexGraphic.N.Good : change.Weight < 0 ? XgCortexGraphic.N.Bad : XgCortexGraphic.N.Dim;
            ink *= Mathf.Lerp(.24f, 1, strength); ink.a = 1; return ink;
        }
    }
}
