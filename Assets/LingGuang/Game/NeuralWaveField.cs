using UnityEngine;

namespace LingGuang.Game
{
    /// <summary>Event-driven, bounded decorative waves. Never writes to the simulation.</summary>
    public sealed class NeuralWaveField : MonoBehaviour
    {
        public const int Capacity = 8;
        public const float Lifetime = 4.2f;
        public const float Speed = 2.6f;
        public const float Frequency = 9f;
        public const float PacketWidth = 1.35f;
        static readonly int WavesId = Shader.PropertyToID("_Waves");
        static readonly int ClockId = Shader.PropertyToID("_WaveClock");
        static readonly int GainId = Shader.PropertyToID("_WaveGain");
        static readonly int GeometryId = Shader.PropertyToID("_WaveGeometry");
        static readonly int Lobe0Id = Shader.PropertyToID("_WaterLobe0");
        static readonly int Lobe1Id = Shader.PropertyToID("_WaterLobe1");
        readonly Vector4[] waves = new Vector4[Capacity];
        readonly Vector4[] lobes = new Vector4[2];
        Material material;
        int cursor;
        public bool ReduceFlash { get; set; }
        public Material FieldMaterial => material;

        public void Initialize()
        {
            if (material != null) return;
            var shader = Shader.Find("LingGuang/NeuralWave");
            if (shader == null || !shader.isSupported) return;
            material = new Material(shader) { name = "LG_NeuralWaveField", hideFlags = HideFlags.DontSave };
            Clear();
        }

        // Explicit clock makes lifetime/coalescing testable without frame timing.
        public void Emit(Vector3 position, float strength, float now)
        {
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y)
                || !float.IsFinite(strength) || !float.IsFinite(now) || strength <= 0) return;
            strength = Mathf.Clamp01(strength);
            for (int i = 0; i < Capacity; i++)
            {
                var wave = waves[i];
                float age = now - wave.z;
                // Only nearby impacts coalesce. Distant sources must interfere.
                if (wave.w <= 0 || age < 0 || age > 0.24f) continue;
                if ((new Vector2(wave.x, wave.y) - (Vector2)position).sqrMagnitude > 0.64f) continue;
                wave.w = Mathf.Min(1f, Mathf.Max(wave.w, strength) + 0.08f);
                waves[i] = wave; // Do not restart its clock or move its origin.
                return;
            }
            waves[cursor] = new Vector4(position.x, position.y, now, strength);
            cursor = (cursor + 1) % Capacity;
        }

        public int ActiveCount(float now)
        {
            int count = 0;
            for (int i = 0; i < Capacity; i++)
                if (waves[i].w > 0 && now >= waves[i].z && now - waves[i].z < Lifetime) count++;
            return count;
        }

        public void Clear()
        {
            System.Array.Clear(waves, 0, waves.Length);
            cursor = 0;
            Upload(Time.time);
        }

        public void Upload(float now)
        {
            ApplyTo(material, now);
        }

        public void SetLobe(int side, Vector2 center, Vector2 radii)
        {
            if (side < 0 || side >= lobes.Length) return;
            lobes[side] = new Vector4(center.x, center.y, Mathf.Max(0.01f, radii.x), Mathf.Max(0.01f, radii.y));
        }

        public void ApplyTo(Material target, float now)
        {
            if (target == null) return;
            target.SetVectorArray(WavesId, waves); // Fixed-size upload clears stale slots too.
            target.SetFloat(ClockId, now);
            target.SetFloat(GainId, ReduceFlash ? 0.14f : 0.68f);
            target.SetVector(GeometryId, new Vector4(Lifetime, Speed, Frequency, PacketWidth));
            target.SetVector(Lobe0Id, lobes[0]);
            target.SetVector(Lobe1Id, lobes[1]);
        }

        void LateUpdate() => Upload(Time.time);
        void OnDisable() => Clear();
        void OnDestroy()
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
    }
}
