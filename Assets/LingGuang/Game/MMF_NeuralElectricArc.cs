using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace LingGuang.Game
{
    /// <summary>Feel feedback for the game's real directed edge geometry.</summary>
    [System.Serializable, FeedbackPath("LingGuang/Neural Electric Arc")]
    public sealed class MMF_NeuralElectricArc : MMF_Feedback
    {
        public LineRenderer Core, Halo;
        public SpriteRenderer Head;
        public Vector3 From, To;
        public Color Tint = Color.cyan;
        public float Travel = 0.1f, Tail = 0.22f, Width = 0.035f, Strength = 1, Seed;
        public bool Reduced;
        [System.NonSerialized] public EdgeView Path;
        [System.NonSerialized] public SparkView Target;
        public override float FeedbackDuration => Travel + Tail;
        const int Segments = 20;
        Coroutine routine;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1)
        {
            if (!Active || Core == null || Halo == null || Head == null) return;
            StopRoutine();
            routine = Owner.StartCoroutine(PlayArc());
        }

        IEnumerator PlayArc()
        {
            IsPlaying = true;
            Core.enabled = Halo.enabled = Head.enabled = true;
            Core.positionCount = Halo.positionCount = Segments + 1;
            Core.widthMultiplier = Reduced ? Width * 0.6f : Width;
            Halo.widthMultiplier = Width * 3;
            float elapsed = 0;
            bool arrived = false;
            while (elapsed < FeedbackDuration)
            {
                float front = Mathf.Clamp01(elapsed / Travel);
                float tail = Mathf.Max(0, front - 0.72f);
                var normal = new Vector3(-(To - From).y, (To - From).x).normalized;
                float fade = elapsed <= Travel ? 1 : Mathf.Clamp01(1 - (elapsed - Travel) / Tail);
                for (int i = 0; i <= Segments; i++)
                {
                    float u = Mathf.Lerp(tail, front, i / (float)Segments);
                    var center = Path != null ? Path.SamplePath(u) : Vector3.Lerp(From, To, u);
                    // Fixed filament shape, revealed progressively; no per-frame random strobe.
                    float jag = Reduced ? 0 : Mathf.Sin(u * 73 + Seed) * Mathf.Sin(u * Mathf.PI) * 0.045f;
                    var point = center + normal * jag;
                    Core.SetPosition(i, point);
                    Halo.SetPosition(i, point);
                }
                float peak = Strength * (Reduced ? 0.36f : 2.6f) * fade;
                Gfx.SetLineColor(Core, Gfx.HDR(Color.Lerp(Tint, Color.white, 0.32f), peak));
                Gfx.SetLineColor(Halo, Gfx.HDR(Tint, peak * (Reduced ? 0.12f : 0.30f)));
                Head.transform.position = Path != null ? Path.SamplePath(front) : Vector3.Lerp(From, To, front);
                Gfx.SetColor(Head, Gfx.HDR(new Color(0.75f, 0.96f, 1), peak * 1.2f));
                if (!arrived && front >= 1)
                {
                    arrived = true;
                    if (Target != null) Target.Flash(Reduced ? 0.04f : 0.14f);
                }
                elapsed += FeedbackDeltaTime;
                yield return null;
            }
            routine = null;
            Hide();
        }

        void StopRoutine()
        {
            if (routine != null && Owner != null) Owner.StopCoroutine(routine);
            routine = null;
            Hide();
        }

        public void Hide()
        {
            if (Core != null) Core.enabled = false;
            if (Halo != null) Halo.enabled = false;
            if (Head != null) Head.enabled = false;
            IsPlaying = false;
        }

        protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1) => StopRoutine();
        protected override void CustomRestoreInitialValues() => StopRoutine();
        public override void OnDisable() => StopRoutine();
    }
}
