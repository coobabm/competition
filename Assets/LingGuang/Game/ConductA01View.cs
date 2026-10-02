using UnityEngine;
using UnityEngine.Rendering;

namespace LingGuang.Game
{
    /// <summary>A01 HTML art translated into two shared-mesh/shader draws per conductor.
    /// Only presentation state is accepted; no rule state or global random/time state is changed.</summary>
    [DisallowMultipleComponent]
    public sealed class ConductA01View : MonoBehaviour
    {
        public const float BreathPeriod = 3f, BreathAmplitude = .10f, RestBrightness = .10f;
        public const float FlickerRate = .8f, ScanInterval = 5f, ScanDuration = 1.4f, Persistence = .55f;
        static ConductA01Geometry geometry;
        static Mesh lineMesh;
        static Material lineMaterial, bodyMaterial;
        static Sprite icon;
        static readonly int SignalId = Shader.PropertyToID("_Signal");
        static readonly int ScanId = Shader.PropertyToID("_Scan");
        static readonly int FlowId = Shader.PropertyToID("_Flow");
        static readonly int TintId = Shader.PropertyToID("_Tint");
        MaterialPropertyBlock properties;
        MeshRenderer filaments;
        SpriteRenderer soma;
        float phase;
        public bool Ready => soma != null && filaments != null;
        public float LastBreath { get; private set; }
        public float LastFlicker { get; private set; }
        public Vector2 LastFlow { get; private set; }
        public static Sprite Icon
        {
            get
            {
                if (icon == null) icon = Geometry.CreateIcon();
                return icon;
            }
        }
        public static ConductA01Geometry Geometry => geometry ??= new ConductA01Geometry();

        public bool Initialize(int identity = 0)
        {
            properties ??= new MaterialPropertyBlock();
            phase = Geometry.Phase + identity * 1.618034f;
            if (Ready) return true;
            var shader = Shader.Find("LingGuang/ConductA01");
            if (shader == null || !shader.isSupported) return false;
            if (lineMaterial == null)
            {
                lineMaterial = new Material(shader) { name = "A01 shared filaments", hideFlags = HideFlags.DontSave };
                lineMaterial.SetFloat("_Geometry", 1);
            }
            if (bodyMaterial == null)
                bodyMaterial = new Material(shader) { name = "A01 shared soma", hideFlags = HideFlags.DontSave };
            if (lineMesh == null) lineMesh = Geometry.CreateLineMesh();
            var lines = new GameObject("A01 axon dendrites membrane");
            lines.transform.SetParent(transform, false);
            lines.AddComponent<MeshFilter>().sharedMesh = lineMesh;
            filaments = lines.AddComponent<MeshRenderer>();
            filaments.sharedMaterial = lineMaterial;
            filaments.sortingOrder = 13;
            filaments.shadowCastingMode = ShadowCastingMode.Off;
            filaments.receiveShadows = false;
            soma = Gfx.MakeSprite("A01 crystal scan phosphor", transform, Gfx.Square, bodyMaterial, 12,
                ConductA01Geometry.WorldUnit * 2.4f);
            soma.transform.localPosition = Vector3.right * (.2f * ConductA01Geometry.WorldUnit);
            TickVisual(0, 0, 0, false, false, false);
            return true;
        }

        public void SetDirection(int direction)
        {
            int d = (direction % 6 + 6) % 6;
            transform.localRotation = Quaternion.Euler(0, 0, 60 - 60 * d);
        }

        public static float BreathAt(float time, float phase = 0, bool reduced = false)
            => 1 + (reduced ? .025f : BreathAmplitude) * Mathf.Sin(time * Mathf.PI * 2 / BreathPeriod + phase);

        public static Vector3 ScanAt(float time, bool reduced = false)
        {
            if (reduced) return new Vector3(-.246f, 0, 0);
            float p = Mathf.Repeat(time, ScanInterval), u = p / ScanDuration;
            float x = Mathf.Lerp(-.246f, .39f, Mathf.Min(u, 1));
            float fade = u <= 1 ? 1 : Mathf.Clamp01(1 - (p - ScanDuration) / Persistence);
            return new Vector3(x, u <= 1 ? 1 : 0, fade);
        }

        static float Hash(float n) => Mathf.Repeat(Mathf.Sin(n * 127.1f + 311.7f) * 43758.5453f, 1);
        static float FlickerAt(float time, float phase)
        {
            // Independent, bounded decorative noise. Never consumes RunState or UnityEngine.Random.
            float t = Mathf.Max(0, time + phase), epoch = Mathf.Floor(t * FlickerRate);
            float local = t - epoch / FlickerRate - Hash(epoch + phase) * 1.0f;
            float duration = .05f + Hash(epoch + phase + 11) * .06f;
            return local >= 0 && local < duration
                ? (.25f + Hash(epoch + phase + 23) * .35f) * Mathf.Sin(local / duration * Mathf.PI) : 0;
        }

        public void TickVisual(float time, float charge, float flash, bool disabled, bool ghost, bool reduced)
        {
            if (!Ready) return;
            LastBreath = BreathAt(time, phase, reduced);
            LastFlicker = reduced || disabled || ghost ? 0 : FlickerAt(time, phase);
            float gain = LastBreath * (1 + LastFlicker * .6f) * (1 + Mathf.Clamp01(charge) * .45f);
            gain *= disabled ? .28f : ghost ? .58f : 1;
            float fire = disabled ? 0 : Mathf.Clamp01(flash) * (reduced ? .25f : 1.6f);
            var scan = ScanAt(time + phase, reduced || disabled || ghost);
            float u = Mathf.Repeat(time + phase, 2.6f) / 2.6f;
            Vector2 flow = Vector2.zero; float flowAlpha = 0;
            if (u < .25f) { flow = ConductA01Geometry.Along(Geometry.Dendrite, 1 - u / .25f); flowAlpha = .5f; }
            else if (u > .35f)
            {
                float v = (u - .35f) / .65f;
                flow = ConductA01Geometry.Along(Geometry.Axon, v); flowAlpha = .8f * (1 - v * v * v);
            }
            if (disabled || ghost) flowAlpha = 0;
            LastFlow = flow;
            properties.Clear();
            properties.SetVector(SignalId, new Vector4(gain, fire, LastFlicker, ghost ? .58f : disabled ? .45f : 1));
            properties.SetVector(ScanId, new Vector4(scan.x, scan.y, scan.z, reduced ? .25f : 1));
            properties.SetVector(FlowId, new Vector4(flow.x, flow.y, flowAlpha * (reduced ? .3f : 1), 0));
            properties.SetColor(TintId, new Color(90 / 255f, 220 / 255f, 1));
            filaments.SetPropertyBlock(properties);
            soma.SetPropertyBlock(properties);
        }
    }
}
