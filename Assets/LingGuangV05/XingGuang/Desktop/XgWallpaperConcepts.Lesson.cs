using System.Collections.Generic;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    // Presentation only: reuse the wallpaper's soft node halos, with no new material or asset.
    public sealed partial class XgWallpaperConcepts
    {
        const float LessonPulseSeconds = .65f;
        XgSim lessonSource;
        XgLessonSubscription lessonSubscription;
        ChapterOneRuntime lessonRuntime;
        readonly HashSet<int> lessonConceptIds = new HashSet<int>();
        int lessonRegion;
        float lessonUntil;
        float LessonPulse => StructureReduced ? 0 : lessonUntil <= 0 ? 0 : Mathf.Clamp01((lessonUntil - Time.unscaledTime) / LessonPulseSeconds);

        protected override void OnEnable()
        {
            base.OnEnable();
            ObserveLessonSource();
        }

        protected override void OnDisable()
        {
            DetachLessonSource();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            DetachLessonSource();
            lessonSubscription?.Dispose();
            lessonSubscription = null;
            base.OnDestroy();
        }

        void DetachLessonSource()
        {
            ClearStructure();
            if (lessonRuntime != null) lessonRuntime.Changed -= ObserveLessonSource;
            lessonRuntime = null;
            lessonSubscription?.Bind(null);
            lessonSource = null;
            lessonUntil = 0;
            lessonConceptIds.Clear();
        }

        void ObserveLessonSource()
        {
            var runtime = controller != null ? controller.runtime : null;
            var source = isActiveAndEnabled && runtime != null && runtime.isActiveAndEnabled
                && runtime.Sim != null ? controller.Sim : null;
            if (ReferenceEquals(lessonSource, source) && lessonRuntime == runtime) return;
            DetachLessonSource();
            if (!isActiveAndEnabled) return;
            lessonRuntime = runtime;
            // Controller registered first; it binds the new XgSim before this callback.
            if (lessonRuntime != null) lessonRuntime.Changed += ObserveLessonSource;
            lessonSource = source;
            if (lessonSubscription == null) lessonSubscription = new XgLessonSubscription(OnLessonTaught);
            lessonSubscription.Bind(lessonSource);
            next = 0;
            SetVerticesDirty();
        }

        void OnLessonTaught(XgCard taughtCard, XgAnswer answer)
        {
            if (!isActiveAndEnabled || taughtCard == null || !answer.correct || controller == null
                || !ReferenceEquals(lessonSource, controller.Sim) || controller.runtime == null
                || !controller.runtime.isActiveAndEnabled || wallpaper == null || !wallpaper.Reveal.Visible) return;

            // Answer has already advanced the desk. Never call Card(desk) here.
            string region = XgBoardData.Region(taughtCard.dataset);
            lessonRegion = Region(region);
            lessonConceptIds.Clear();
            var board = lessonSource.S.board;
            if (board?.concepts != null)
                foreach (var concept in board.concepts)
                    if (concept != null && !concept.seed && concept.region == region)
                        lessonConceptIds.Add(concept.id);
            lessonUntil = wallpaper.TryStartGlow(XgBoardData.DisplayRegion(taughtCard.dataset)) ? Time.unscaledTime + LessonPulseSeconds : 0;
            RememberAttention(taughtCard);
            presentation?.DataChanged();next = 0;
            Update(); // Refresh the learned board in this event; do not wait for the 100 ms poll.
            SetVerticesDirty();
        }

        void TickLessonPulse()
        {
            if (lessonUntil <= 0) return;
            if (Time.unscaledTime >= lessonUntil)
            {
                lessonUntil = 0;
                lessonConceptIds.Clear();
                next = 0; SetVerticesDirty();
            }
        }

        Color LessonTint(Cell cell)
        {
            return lessonConceptIds.Contains(cell.id)
                ? Color.Lerp(cell.tint, Color.white, LessonPulse * .8f) : cell.tint;
        }

        void DrawLessonHalo(VertexHelper vh, Rect area, float scale)
        {
            float pulse = LessonPulse;
            if (pulse <= 0) return;
            var center = area.min + Vector2.Scale(area.size, RegionCenter(lessonRegion));
            // Region fallback remains visible even if the taught cells are beyond mesh LOD.
            var glow = XgCortexGraphic.N.Teal;
            glow.a = .075f * pulse;
            XgDraw.Disc(vh, center, 72 * scale, glow, 32);
            glow.a = .11f * pulse;
            XgDraw.Disc(vh, center, 42 * scale, glow, 24);
        }
    }
}
