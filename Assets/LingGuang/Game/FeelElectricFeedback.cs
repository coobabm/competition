using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

namespace LingGuang.Game
{
    /// <summary>Local light/electric Feel sequences. Bounded pools; no simulation writes.</summary>
    [DisallowMultipleComponent]
    public sealed class FeelElectricFeedback : MonoBehaviour
    {
        public const int ImpactCapacity = 12;
        public const int ArcCapacity = 24;
        public int ImpactPlayCount { get; private set; }
        public int ArcPlayCount { get; private set; }
        public int PatternPlayCount { get; private set; }
        public bool Reduced { get; private set; }
        public bool FastForward { get; private set; }

        sealed class ImpactSlot
        {
            public MMF_Player player;
            public Transform visuals;
            public SpriteRenderer halo, horizontal, vertical;
            public ParticleSystem particles;
            public MMF_Particles particleFeedback;
        }
        sealed class ArcSlot { public MMF_Player player; public MMF_NeuralElectricArc feedback; }
        readonly ImpactSlot[] impacts = new ImpactSlot[ImpactCapacity];
        readonly ArcSlot[] arcs = new ArcSlot[ArcCapacity];
        int impactCursor, arcCursor, stamp = -1, impactsInFrame, arcsInFrame;
        bool initialized;
        float lastPattern = float.NegativeInfinity;
        Vector3 lastFirePosition;

        public void Initialize()
        {
            if (initialized) return;
            for (int i = 0; i < impacts.Length; i++) impacts[i] = CreateImpact(i);
            for (int i = 0; i < arcs.Length; i++) arcs[i] = CreateArc(i);
            initialized = true;
            Clear();
        }

        MMF_Player CreatePlayer(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var player = go.AddComponent<MMF_Player>();
            player.InitializationMode = MMFeedbacks.InitializationModes.Script;
            player.AutoPlayOnEnable = false;
            player.AutoPlayOnStart = false;
            player.CanPlayWhileAlreadyPlaying = false;
            player.StopFeedbacksOnDisable = true;
            player.RestoreInitialValuesOnDisable = false;
            player.ForceTimescaleMode = true;
            player.ForcedTimescaleMode = TimescaleModes.Scaled;
            return player;
        }

        static void Add(MMF_Player player, MMF_Feedback feedback)
        {
            feedback.Timing = new MMFeedbackTiming { TimescaleMode = TimescaleModes.Scaled };
            player.AddFeedback(feedback);
        }

        static Gradient FadeGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.10f),
                    new GradientAlphaKey(0.26f, 0.45f), new GradientAlphaKey(0, 1) });
            return gradient;
        }

        ImpactSlot CreateImpact(int index)
        {
            var slot = new ImpactSlot { player = CreatePlayer("Feel Node Discharge " + index) };
            slot.visuals = new GameObject("Local light").transform;
            slot.visuals.SetParent(slot.player.transform, false);
            slot.halo = Gfx.MakeSprite("Corona", slot.visuals, Gfx.SoftDot, Gfx.Additive, 28, 0.90f);
            slot.horizontal = Gfx.MakeSprite("Horizontal glint", slot.visuals, Gfx.Square, Gfx.Additive, 32, 1);
            slot.horizontal.transform.localScale = new Vector3(0.62f, 0.025f, 1);
            slot.vertical = Gfx.MakeSprite("Vertical glint", slot.visuals, Gfx.Square, Gfx.Additive, 32, 1);
            slot.vertical.transform.localScale = new Vector3(0.025f, 0.38f, 1);
            foreach (var sprite in new[] { slot.halo, slot.horizontal, slot.vertical })
            {
                sprite.color = Color.clear;
                Add(slot.player, new MMF_SpriteRenderer
                {
                    Label = "Light envelope / " + sprite.name,
                    BoundSpriteRenderer = sprite, Mode = MMF_SpriteRenderer.Modes.OverTime,
                    Duration = 0.32f, StartsOff = true, ColorOverTime = FadeGradient(),
                    AllowAdditivePlays = false
                });
            }
            var tween = new MMTweenType(new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.18f, 1), new Keyframe(1, 0.20f)));
            Add(slot.player, new MMF_Scale
            {
                Label = "Corona expansion (visuals only)", AnimateScaleTarget = slot.visuals,
                Mode = MMF_Scale.Modes.Absolute, AnimateScaleDuration = 0.32f,
                UniformScaling = true, RemapCurveZero = 0.6f, RemapCurveOne = 1.35f,
                AnimateScaleTweenX = tween, AnimateScaleTweenY = tween, AnimateScaleTweenZ = tween
            });
            var particleRoot = new GameObject("Electric fragments");
            particleRoot.transform.SetParent(slot.player.transform, false);
            slot.particles = particleRoot.AddComponent<ParticleSystem>();
            slot.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = slot.particles.main;
            main.loop = false; main.playOnAwake = false; main.maxParticles = 12;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.13f, 0.28f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 3.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.5f, 0.9f, 1f);
            var emission = slot.particles.emission; emission.enabled = false;
            var shape = slot.particles.shape;
            shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 0.025f;
            var colors = slot.particles.colorOverLifetime;
            colors.enabled = true; colors.color = FadeGradient();
            var sizes = slot.particles.sizeOverLifetime;
            sizes.enabled = true; sizes.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
            var renderer = slot.particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Gfx.Additive;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.2f; renderer.velocityScale = 0.04f; renderer.sortingOrder = 33;
            slot.particleFeedback = new MMF_Particles
            {
                Label = "Short electric fragments", BoundParticleSystem = slot.particles,
                Mode = MMF_Particles.Modes.Emit, EmitCount = 4, MoveToPosition = true,
                DeclaredDuration = 0.28f, StopSystemOnInit = true, StopSystemOnStopFeedback = true
            };
            Add(slot.player, slot.particleFeedback);
            slot.player.Initialization();
            return slot;
        }

        ArcSlot CreateArc(int index)
        {
            var slot = new ArcSlot { player = CreatePlayer("Feel Transmission " + index) };
            slot.feedback = new MMF_NeuralElectricArc
            {
                Label = "Travelling electric arc",
                Core = Gfx.MakeLine("Electric filament", slot.player.transform, Gfx.Additive, 31, 0.026f),
                Halo = Gfx.MakeLine("Arc halo", slot.player.transform, Gfx.Additive, 29, 0.09f),
                Head = Gfx.MakeSprite("Charge head", slot.player.transform, Gfx.Diamond, Gfx.Additive, 34, 0.085f),
                Seed = index * 1.73f
            };
            Add(slot.player, slot.feedback);
            slot.player.Initialization();
            slot.feedback.Hide();
            return slot;
        }

        public void SetMode(bool reduced, bool fastForward)
        {
            if (Reduced != reduced) Clear(); // Do not leave bright tails active after F2.
            Reduced = reduced;
            FastForward = fastForward;
        }

        bool Allow(bool arc)
        {
            if (!initialized || !isActiveAndEnabled || !Application.isPlaying) return false;
            if (stamp != Time.frameCount) { stamp = Time.frameCount; impactsInFrame = arcsInFrame = 0; }
            if (arc) return ++arcsInFrame <= ArcCapacity;
            return ++impactsInFrame <= (Reduced ? 4 : ImpactCapacity);
        }

        public void Fire(Vector3 position, Color color, float strength)
        {
            if (!Finite(position) || !float.IsFinite(strength) || strength <= 0 || !Allow(false)) return;
            lastFirePosition = position;
            var slot = impacts[impactCursor++ % impacts.Length];
            slot.player.StopFeedbacks();
            slot.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            slot.player.transform.position = position;
            slot.visuals.localScale = Vector3.one;
            slot.player.DurationMultiplier = FastForward ? 0.45f : 1f;
            float gain = Mathf.Clamp01(strength) * (Reduced ? 0.16f : 1f);
            Gfx.SetColor(slot.halo, Gfx.HDR(color, gain * 1.65f));
            Gfx.SetColor(slot.horizontal, Gfx.HDR(Color.Lerp(color, Color.white, 0.6f), gain * 2f));
            Gfx.SetColor(slot.vertical, Gfx.HDR(Color.Lerp(color, Color.white, 0.6f), gain * 2f));
            slot.particleFeedback.Active = !Reduced;
            slot.particleFeedback.EmitCount = FastForward ? 2 : 4;
            var main = slot.particles.main;
            main.startColor = Color.Lerp(color, Color.white, 0.4f);
            main.simulationSpeed = FastForward ? 2.2f : 1f;
            slot.player.PlayFeedbacks(position);
            ImpactPlayCount++;
        }

        public void Transmit(Vector3 from, Vector3 to, Color color, float width, float strength,
            float travel, EdgeView edge, SparkView target)
        {
            if (!Finite(from) || !Finite(to) || !float.IsFinite(width) || !float.IsFinite(strength) || strength <= 0
                || !float.IsFinite(travel) || (to - from).sqrMagnitude < 0.000001f || !Allow(true)) return;
            var slot = arcs[arcCursor++ % arcs.Length];
            slot.player.StopFeedbacks();
            var arc = slot.feedback;
            arc.From = from; arc.To = to; arc.Path = edge; arc.Target = target;
            arc.Tint = color; arc.Strength = Mathf.Clamp(strength, 0, 1.4f);
            arc.Width = Mathf.Clamp(width, 0.025f, 0.07f);
            arc.Travel = Mathf.Max(0.02f, travel);
            arc.Tail = FastForward ? 0.07f : 0.22f;
            arc.Reduced = Reduced;
            slot.player.ComputeCachedTotalDuration();
            slot.player.PlayFeedbacks(from);
            ArcPlayCount++;
        }

        public void Pattern()
        {
            if (Time.time - lastPattern < 0.45f) return;
            lastPattern = Time.time;
            Fire(lastFirePosition, new Color(0.60f, 0.80f, 1f), 0.8f);
            PatternPlayCount++;
        }

        static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z);

        public void Clear()
        {
            if (!initialized) return;
            foreach (var slot in impacts)
            {
                slot.player.StopFeedbacks();
                slot.particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                slot.halo.color = slot.horizontal.color = slot.vertical.color = Color.clear;
                slot.visuals.localScale = Vector3.one;
            }
            foreach (var slot in arcs) { slot.player.StopFeedbacks(); slot.feedback.Hide(); }
            stamp = -1; impactCursor = arcCursor = 0;
            lastPattern = float.NegativeInfinity;
        }

        void OnDisable() => Clear();
    }
}
