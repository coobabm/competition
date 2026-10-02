using System.Collections.Generic;
using UnityEngine;

namespace Emergence
{
    /// <summary>Small procedural sound palette: no imported clips, mixer, or audio assets required.</summary>
    [DisallowMultipleComponent]
    public sealed class EmergenceAudio : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private readonly List<AudioClip> generatedClips = new List<AudioClip>();
        private readonly AudioClip[] notes = new AudioClip[12];
        private AudioSource tones;
        private AudioSource ambience;
        private AudioClip uiClip;
        private AudioClip winClip;
        private AudioClip loseClip;
        private AudioClip multiplierClip;
        private bool initialized;
        private bool muted;
        private int burstFrame = -1;
        private int burstCount;

        public bool Muted
        {
            get => muted;
            set
            {
                muted = value;
                if (tones != null) tones.mute = value;
                if (ambience != null) ambience.mute = value;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            if (ambience != null && !ambience.isPlaying) ambience.Play();
        }

        private void OnDisable()
        {
            if (ambience != null) ambience.Stop();
            if (tones != null) tones.Stop();
        }

        private void OnDestroy()
        {
            if (ambience != null) ambience.Stop();
            if (tones != null) tones.Stop();
            for (int i = 0; i < generatedClips.Count; i++)
                if (generatedClips[i] != null) Destroy(generatedClips[i]);
            generatedClips.Clear();
        }

        public void PlayNode(int depth, bool echo = false)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled || muted) return;
            if (burstFrame != Time.frameCount)
            {
                burstFrame = Time.frameCount;
                burstCount = 0;
            }
            burstCount++;
            int index = Mathf.Clamp(depth, 0, notes.Length - 1);
            if (echo) index = Mathf.Min(notes.Length - 1, index + 3);
            float level = (echo ? 0.19f : 0.27f) / Mathf.Sqrt(Mathf.Max(1, burstCount));
            tones.PlayOneShot(notes[index], level);
        }

        public void PlayUI()
        {
            Play(uiClip, 0.23f);
        }

        public void PlayWin()
        {
            Play(winClip, 0.43f);
        }

        public void PlayLose()
        {
            Play(loseClip, 0.32f);
        }

        public void PlayMultiplier()
        {
            Play(multiplierClip, 0.30f);
        }

        private void Play(AudioClip clip, float volume)
        {
            EnsureInitialized();
            if (!isActiveAndEnabled || muted || clip == null) return;
            tones.PlayOneShot(clip, volume);
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;
            tones = gameObject.AddComponent<AudioSource>();
            Configure(tones);
            tones.priority = 80;
            ambience = gameObject.AddComponent<AudioSource>();
            Configure(ambience);
            ambience.priority = 160;
            ambience.loop = true;
            ambience.volume = 0.23f;

            // A pentatonic palette keeps simultaneous waves consonant across a growing chain.
            float[] frequencies = { 220f, 261.6256f, 293.6648f, 349.2282f, 391.9954f, 440f,
                523.2511f, 587.3295f, 698.4565f, 783.9909f, 880f, 1046.502f };
            for (int i = 0; i < notes.Length; i++)
                notes[i] = CreatePluck("Neuron " + i, frequencies[i], 0.50f, 0.29f);
            uiClip = CreatePluck("Interface", 659.255f, 0.115f, 0.18f);
            multiplierClip = CreatePhrase("Multiplier", new[] { 523.2511f, 698.4565f, 1046.502f }, 0.09f, 0.75f, 0.18f);
            winClip = CreatePhrase("Emergence", new[] { 261.6256f, 349.2282f, 391.9954f, 523.2511f, 698.4565f }, 0.12f, 1.75f, 0.16f);
            loseClip = CreatePhrase("Dissolve", new[] { 220f, 174.6141f, 146.8324f }, 0.19f, 1.5f, 0.15f);
            ambience.clip = CreateDrone();
        }

        private void Configure(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.volume = 1f;
            source.mute = muted;
            source.bypassReverbZones = true;
        }

        private AudioClip CreatePluck(string title, float frequency, float duration, float amplitude)
        {
            int length = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / SampleRate;
                float phase = 2f * Mathf.PI * frequency * t;
                float body = Mathf.Sin(phase) + 0.22f * Mathf.Sin(phase * 2f) * Mathf.Exp(-12f * t)
                    + 0.08f * Mathf.Sin(phase * 3f) * Mathf.Exp(-19f * t);
                float envelope = Mathf.Min(1f, t / 0.009f) * Mathf.Exp(-t * 7.5f);
                envelope *= Mathf.Clamp01((duration - t) / 0.035f);
                samples[i] = body * envelope * amplitude;
            }
            return Clip(title, samples);
        }

        private AudioClip CreatePhrase(string title, float[] frequencies, float spacing, float duration, float amplitude)
        {
            int length = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[length];
            for (int note = 0; note < frequencies.Length; note++)
            {
                int start = Mathf.RoundToInt(note * spacing * SampleRate);
                for (int i = start; i < length; i++)
                {
                    float t = (float)(i - start) / SampleRate;
                    float phase = 2f * Mathf.PI * frequencies[note] * t;
                    float envelope = Mathf.Min(1f, t / 0.015f) * Mathf.Exp(-t * 4f);
                    envelope *= Mathf.Clamp01((length - i) / (SampleRate * 0.08f));
                    samples[i] += (Mathf.Sin(phase) + 0.14f * Mathf.Sin(phase * 2f)) * envelope * amplitude;
                }
            }
            return Clip(title, samples);
        }

        private AudioClip CreateDrone()
        {
            const float duration = 8f;
            float[] samples = new float[(int)(SampleRate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / SampleRate;
                // All oscillator periods divide eight seconds, including modulation: seamless loop.
                float motion = 0.79f + Mathf.Sin(t * Mathf.PI * 0.25f) * 0.15f;
                samples[i] = (Mathf.Sin(2f * Mathf.PI * 65.5f * t) * 0.042f
                    + Mathf.Sin(2f * Mathf.PI * 98.125f * t) * 0.020f
                    + Mathf.Sin(2f * Mathf.PI * 130.75f * t) * 0.009f) * motion;
            }
            return Clip("Quiet network", samples);
        }

        private AudioClip Clip(string title, float[] samples)
        {
            AudioClip result = AudioClip.Create(title, samples.Length, 1, SampleRate, false);
            result.hideFlags = HideFlags.DontSave;
            result.SetData(samples, 0);
            generatedClips.Add(result);
            return result;
        }
    }
}
