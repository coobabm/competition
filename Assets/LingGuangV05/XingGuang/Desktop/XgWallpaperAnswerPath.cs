using System.Collections.Generic;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Transient readout of the answered card's real prediction, beneath application windows.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgWallpaperAnswerPath : MaskableGraphic
    {
        public const float Duration = .4f;
        public const float CorrectionDuration = .6f;
        const int MaxPaths = 4;
        sealed class Path
        {
            public readonly List<Vector2> points = new List<Vector2>();
            public Transform input, output;
            public int[] concepts;
            public float started;
            public Color ink;
            public bool corrected;
            public IReadOnlyList<XgConceptPull> pulls;
        }
        readonly List<Path> paths = new List<Path>(MaxPaths);
        XingGuangController controller;
        XgBrainWallpaper wallpaper;
        XgWallpaperConcepts concepts;
        XgBrainWallpaperPresentation presentation;
        ChapterOneRuntime runtime;
        XgSim source;

        public static void Install(XingGuangController controller, XgBrainWallpaper wallpaper)
        {
            var rect = PrologueDesk.Rect("Answer Paths", wallpaper.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rect.SetSiblingIndex(1);
            var view = rect.gameObject.AddComponent<XgWallpaperAnswerPath>();
            view.raycastTarget = false; view.controller = controller; view.wallpaper = wallpaper;
            view.concepts = wallpaper.GetComponentInChildren<XgWallpaperConcepts>();
            view.presentation = wallpaper.GetComponent<XgBrainWallpaperPresentation>();
            view.Observe();
        }

        protected override void OnEnable() { base.OnEnable(); Observe(); }
        protected override void OnDisable() { Detach(); base.OnDisable(); }
        protected override void OnDestroy() { Detach(); base.OnDestroy(); }

        void Detach()
        {
            if (runtime != null) runtime.Changed -= Observe;
            if (source != null) source.CardAnswered -= Answered;
            runtime = null; source = null; paths.Clear(); SetVerticesDirty();
        }

        void Observe()
        {
            var current = controller != null ? controller.runtime : null;
            var sim = isActiveAndEnabled && current != null && current.isActiveAndEnabled && current.Sim != null
                ? controller.Sim : null;
            if (runtime == current && ReferenceEquals(source, sim)) return;
            Detach();
            if (!isActiveAndEnabled) return;
            runtime = current; source = sim;
            if (runtime != null) runtime.Changed += Observe;
            if (source != null) source.CardAnswered += Answered;
        }

        Vector2 Point(Transform node) => rectTransform.InverseTransformPoint(node.position);

        void Answered(XgAnswerTrace answer)
        {
            if (!isActiveAndEnabled || wallpaper == null || !wallpaper.Reveal.Visible || runtime == null
                || !runtime.isActiveAndEnabled || !ReferenceEquals(source, controller.Sim) || answer == null) return;
            bool yes = answer.Prediction?.AnswerYes ?? answer.SubmittedYes;
            var input = wallpaper.transform.Find("Inputs");
            var output = wallpaper.GetComponentInChildren<XgWallpaperOutputs>();
            if (input == null || output == null) return;
            var end = output.transform.Find("Port " + (output.PortCount > 1 && !yes ? 1 : 0) + "/Glow0");
            if (end == null) return;
            int inputIndex = answer.Region == "vision" ? 0 : answer.Region == "tone" ? 2 : 1;
            Transform start = input;
            int at = 0;
            foreach (Transform child in input)
                if (child.name == "Glow0" && at++ == inputIndex) { start = child; break; }

            var used = new List<XgUsedConcept>();
            if (answer.Prediction != null) used.AddRange(answer.Prediction.UsedConcepts);
            used.Sort((a, b) => a.Layer != b.Layer ? a.Layer.CompareTo(b.Layer) : a.Id.CompareTo(b.Id));
            var ids = new List<int>();
            foreach (var cell in used)
                ids.Add(cell.Id);
            // Old saves/LOD may omit a cell from the drawing. Do not invent a substitute cell or a learned link.
            if (paths.Count == MaxPaths) paths.RemoveAt(0);
            var path = new Path { input = start, output = end, concepts = ids.ToArray(), started = Time.unscaledTime,
                ink = yes ? XgCortexGraphic.N.Good : XgCortexGraphic.N.Bad,
                corrected = answer.Outcome.correct && answer.Outcome.corrected && answer.Prediction != null,
                pulls = answer.CorrectionPulls };
            ResolvePoints(path); paths.Add(path);
            ApplyBrightness();
            SetVerticesDirty();
        }

        bool ResolvePoints(Path path)
        {
            path.points.Clear();
            if (path.input == null || path.output == null) return false;
            path.points.Add(Point(path.input));
            foreach (int id in path.concepts)
                if (concepts != null && concepts.TryLocalPosition(id, out var point))
                    path.points.Add(rectTransform.InverseTransformPoint(concepts.rectTransform.TransformPoint(point)));
            path.points.Add(Point(path.output));
            return true;
        }

        void Update()
        {
            Observe();
            if(GamePerformance.Low){if(paths.Count>0){paths.Clear();SetVerticesDirty();}return;}
            if (paths.Count == 0) return;
            if (wallpaper == null || !wallpaper.Reveal.Visible) paths.Clear();
            else paths.RemoveAll(path => Time.unscaledTime - path.started >= Duration + (path.corrected ? CorrectionDuration : 0));
            if (paths.Count == 0 || presentation == null || presentation.DrawDue(ref nextDraw)) SetVerticesDirty();
        }
        float nextDraw;

        // Presentation settles Quiet/showcase brightness in Update. Apply it afterwards,
        // including the very first frame of an answer after a long idle period.
        void LateUpdate() { if (paths.Count > 0) ApplyBrightness(); }
        void ApplyBrightness() => canvasRenderer.SetAlpha(presentation != null ? presentation.LightLevel : 1);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(GamePerformance.Low)return;
            float scale = Mathf.Min(rectTransform.rect.width / 1920, rectTransform.rect.height / 1080);
            bool reduced = controller != null && controller.View != null && controller.View.Juice != null && controller.View.Juice.Reduced;
            foreach (var path in paths)
            {
                // A lesson can add/rearrange cells after the answer event in this same frame.
                // Follow the participating identities, never their former screen slots.
                if (!ResolvePoints(path)) continue;
                float elapsed = Time.unscaledTime - path.started;
                bool returning = path.corrected && elapsed >= Duration;
                float age = Mathf.Clamp01(returning ? (elapsed - Duration) / CorrectionDuration : elapsed / Duration);
                if (age >= 1) continue;
                float fade = Mathf.Clamp01((1 - age) * 4);
                int count = path.points.Count;
                var ink = returning ? new Color(.72f, .48f, 1) : path.ink;
                var line = ink; line.a = (reduced ? .18f : .32f) * fade;
                for (int i = 0; i + 1 < count; i++)
                {
                    var glow = line; glow.a *= .22f;
                    XgDraw.Seg(vh, path.points[i], path.points[i + 1], 5 * scale, glow);
                    XgDraw.Seg(vh, path.points[i], path.points[i + 1], 1.2f * scale, line);
                    if (returning && !reduced)
                    {
                        float progress = (1 - age) * (count - 1);
                        if (progress >= i && progress <= i + 1)
                        {
                            var head = Vector2.Lerp(path.points[i], path.points[i + 1], progress - i);
                            var tail = Vector2.Lerp(path.points[i], path.points[i + 1], Mathf.Min(1, progress - i + .22f));
                            var flare = Color.Lerp(ink, Color.white, .6f); flare.a = fade;
                            XgDraw.Seg(vh, head, tail, 3 * scale, flare);
                        }
                    }
                }
                for (int i = 0; i < count; i++)
                {
                    // Returning paths highlight only cells actually pulled by the correction below.
                    if (returning && i > 0 && i < count - 1) continue;
                    float phase = (float)i / (count - 1);
                    if (returning) phase = 1 - phase;
                    float light = reduced ? .3f * fade : Mathf.Clamp01(1 - Mathf.Abs(age * 1.3f - phase) / .18f) * fade;
                    if (light <= 0) continue;
                    var glow = ink; glow.a = light * .18f;
                    // Four simultaneous paths over the full 512-cell LOD stay below the UI mesh limit.
                    int sides = reduced ? 8 : 12;
                    XgDraw.Disc(vh, path.points[i], 12 * scale, glow, sides);
                    glow = Color.Lerp(ink, Color.white, .55f); glow.a = light;
                    XgDraw.Disc(vh, path.points[i], 4 * scale, glow, sides);
                }
                if (returning && path.pulls != null)
                    foreach (var pull in path.pulls)
                    {
                        if (concepts == null || !concepts.TryLocalPosition(pull.Id, out var local)) continue;
                        Vector2 point = rectTransform.InverseTransformPoint(concepts.rectTransform.TransformPoint(local));
                        // Before/after are immutable values from the one real training step. No board writes here.
                        Color oldTint = PullTint(pull.BeforeWeight, pull.BeforeStrength);
                        Color newTint = PullTint(pull.AfterWeight, pull.AfterStrength);
                        var glow = ink; glow.a = (reduced ? .15f : .38f) * fade;
                        XgDraw.Disc(vh, point, (reduced ? 9 : 13) * scale, glow, 6);
                        var core = Color.Lerp(oldTint, newTint, Mathf.SmoothStep(0, 1, age));
                        core = Color.Lerp(core, Color.white, (reduced ? .15f : .65f) * Mathf.Sin(age * Mathf.PI));
                        core.a = fade;
                        XgDraw.Disc(vh, point, 5 * scale, core, 6);
                    }
            }
        }

        static Color PullTint(double weight, double strength)
        {
            float power = Mathf.Sqrt(Mathf.Clamp01((float)(System.Math.Abs(weight) / XgBoard.WeightLimit))
                * Mathf.Clamp01((float)(strength / 10)));
            var tint = weight > 0 ? XgCortexGraphic.N.Good : weight < 0 ? XgCortexGraphic.N.Bad : XgCortexGraphic.N.Dim;
            return tint * Mathf.Lerp(.24f, 1, power);
        }
    }
}
