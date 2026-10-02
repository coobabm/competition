using System.Collections.Generic;
using LingGuang.Core;
using UnityEngine;

namespace LingGuang.Game
{
    /// <summary>Placeholder procedural audio: pentatonic notes rising by beat, timbre by shape, chords on settlement.</summary>
    public sealed class SynthAudio : MonoBehaviour
    {
        const int Rate = 44100;
        static readonly int[] Penta = { 0, 2, 4, 7, 9 };
        readonly Dictionary<(int, int, bool), AudioClip> cache = new Dictionary<(int, int, bool), AudioClip>();
        readonly List<AudioSource> sources = new List<AudioSource>();
        int next;
        public float volume = 0.55f;
        AudioClip chime, pattern, chordOk, chordFail, tick;

        void Awake()
        {
            for (int i = 0; i < 14; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                sources.Add(s);
            }
            chime = Make("chime", 1.2f, t => Bell(t, 1318.5f, 3f) * 0.5f + Bell(t, 1975.5f, 4f) * 0.3f);
            pattern = Make("pattern", 0.9f, t => (Bell(t, 523.25f, 2.5f) + Bell(t, 659.25f, 2.5f) + Bell(t, 783.99f, 2.5f)) * 0.3f * Mathf.Clamp01(t * 30f));
            chordOk = Make("chordOk", 2.6f, t => (Pad(t, 261.63f) + Pad(t, 329.63f) + Pad(t, 392f) + Pad(t, 523.25f)) * 0.22f * Env(t, 0.05f, 2.6f));
            chordFail = Make("chordFail", 2.6f, t => (Pad(t, 220f) + Pad(t, 261.63f) + Pad(t, 329.63f)) * 0.22f * Env(t, 0.05f, 2.6f));
            tick = Make("tick", 0.08f, t => Mathf.Sin(2 * Mathf.PI * 1800f * t) * Mathf.Exp(-t * 60f) * 0.3f);
        }

        static float Env(float t, float a, float len) => Mathf.Clamp01(t / a) * Mathf.Clamp01((len - t) / (len * 0.6f));
        static float Bell(float t, float f, float decay) => Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * decay);
        static float Pad(float t, float f) => Mathf.Sin(2 * Mathf.PI * f * t) * 0.7f + Mathf.Sin(2 * Mathf.PI * f * 2.001f * t) * 0.2f + Mathf.Sin(2 * Mathf.PI * f * 0.5f * t) * 0.15f;

        static AudioClip Make(string name, float seconds, System.Func<float, float> fn)
        {
            int n = (int)(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(fn(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Freq(int index)
        {
            int oct = index / Penta.Length, step = index % Penta.Length;
            return 261.63f * Mathf.Pow(2f, oct + Penta[step] / 12f);
        }

        AudioClip NoteClip(int index, Shape shape, bool reverb)
        {
            var key = (index, (int)shape, reverb);
            if (cache.TryGetValue(key, out var c)) return c;
            float f = Freq(index);
            float detune = reverb ? 1.006f : 1f;
            System.Func<float, float> fn = shape switch
            {
                Shape.Cone => t => (Tri(t, f) * 0.6f + Tri(t, f * 2f * detune) * 0.2f) * Mathf.Exp(-t * 7f) * Mathf.Clamp01(t * 400f),
                Shape.Converge => t => (Mathf.Sin(2 * Mathf.PI * f * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * f * 2f * detune * t) * 0.25f + Mathf.Sin(2 * Mathf.PI * f * 3f * t) * 0.15f) * Mathf.Exp(-t * 2.8f) * Mathf.Clamp01(t * 200f),
                Shape.Instinct => t => (Mathf.Sin(2 * Mathf.PI * f * 0.5f * t) * 0.6f + Mathf.Sin(2 * Mathf.PI * f * detune * t) * 0.25f) * Mathf.Exp(-t * 4f) * Mathf.Clamp01(t * 150f),
                _ => t => (Bell(t, f, 5f) * 0.7f + Bell(t, f * 2.76f * detune, 9f) * 0.12f) * Mathf.Clamp01(t * 300f),
            };
            c = Make($"note{index}_{shape}", 1.1f, fn);
            cache[key] = c;
            return c;
        }

        static float Tri(float t, float f)
        {
            float p = t * f - Mathf.Floor(t * f);
            return 4f * Mathf.Abs(p - 0.5f) - 1f;
        }

        void Play(AudioClip clip, float vol, float pitch = 1f)
        {
            if (clip == null) return;
            var s = sources[next];
            next = (next + 1) % sources.Count;
            s.clip = clip;
            s.volume = vol * volume;
            s.pitch = pitch;
            s.Play();
        }

        public void PlayNote(int beat, Shape shape, bool reverb) => Play(NoteClip(Mathf.Clamp(beat, 0, 14), shape, reverb), 0.45f);
        public void PlayChime() => Play(chime, 0.5f);
        public void PlayPattern() => Play(pattern, 0.5f);
        public void PlayChord(bool ok) => Play(ok ? chordOk : chordFail, 0.6f);
        public void PlayTick() => Play(tick, 0.4f);
    }
}
