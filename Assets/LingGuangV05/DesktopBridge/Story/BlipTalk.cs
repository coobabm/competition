using System;
using System.Collections.Generic;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Dialogue blips in the manner of BlipTalk Studio (github.com/DevTarlow/bliptalk-studio, MIT): a line is rendered,
    /// offline, into one short clip of retro "speech". Animalese is the default voice: five additively synthesised vowel
    /// slices (three formants each) are played back at a rate that sets the pitch, with a short noise burst in front for
    /// consonants. Chiptune is a plain square-wave blip. Every Chinese character is one syllable (every Latin letter a
    /// shorter one); its vowel and consonant come from its code point, so the same character always sounds the same and
    /// the babble reads like a language. Punctuation is silent but phrases the line as BlipTalk does: a comma is a short
    /// breath, a full stop a longer one, an ellipsis the longest, and the syllable before a question mark lifts while
    /// one before a full stop or an ellipsis falls. The render also gives the moment each character is spoken
    /// (<see cref="Line.reveal"/>), so the typewriter shows the text exactly as it is said: as long, and with the same
    /// pauses. Pitch jitter is seeded by the text, so a line sounds the same every time it is said.
    /// </summary>
    public static class BlipTalk
    {
        public enum Mode { Animalese, Chiptune }

        public struct Voice
        {
            public Mode mode;
            /// <summary>Register in Hz (BlipTalk's Base Pitch), random pitch spread in Hz, 0..1 loudness.</summary>
            public float basePitch, jitter, volume;
            /// <summary>Formant scale and brightness of the Animalese voice (1 = neutral).</summary>
            public float voiceCharacter;
            /// <summary>Chiptune blip decay, seconds.</summary>
            public float decay;
            /// <summary>Seconds per Chinese character (a syllable) and per Latin letter.</summary>
            public float syllable, letter;
        }

        /// <summary>One rendered line: the clip, when each character (of the text without rich-text tags) is spoken, and how long it all takes.</summary>
        public sealed class Line { public AudioClip clip; public float[] reveal; public float length; }

        /// <summary>The protagonist: a low, soft Animalese voice (BlipTalk's "Cozy Animal" taken down to a young man's register).</summary>
        public static readonly Voice Me = new Voice { mode = Mode.Animalese, basePitch = 290, jitter = 28, volume = .75f, voiceCharacter = .95f, decay = .09f, syllable = .085f, letter = .045f };

        const float VoiceF0 = 120, VowelSeconds = .34f, RateReference = 275, RateMin = .35f, RateMax = 3.6f;
        const float Tail = .25f;

        // A, E, I, O, U: formant centres (Hz), relative gains and bandwidths (Hz).
        static readonly float[][] FormantFreq = { new[] { 730f, 1090, 2440 }, new[] { 530f, 1840, 2480 }, new[] { 270f, 2290, 3010 }, new[] { 570f, 840, 2410 }, new[] { 300f, 870, 2240 } };
        static readonly float[][] FormantGain = { new[] { 1f, .5f, .16f }, new[] { 1f, .45f, .2f }, new[] { 1f, .35f, .22f }, new[] { 1f, .6f, .12f }, new[] { 1f, .4f, .1f } };
        static readonly float[][] FormantBw = { new[] { 110f, 130, 180 }, new[] { 100f, 140, 190 }, new[] { 90f, 150, 200 }, new[] { 100f, 120, 180 }, new[] { 90f, 120, 170 } };
        static readonly float[] VowelRate = { 1f, 1.07f, 1.15f, .93f, .87f };
        const string LetterVowels = "AEEEEEEAIAAEEEOEUAEEUEUEIE"; // a..z → A E I O U
        const string Plosives = "bcdgkpqtx", Fricatives = "fhjsvwz";

        /// <summary>BlipTalk's punctuation table: pitch offset (Hz) for the syllable before the mark, and the pause after it (s).</summary>
        struct Rule { public float pitch, pause; public Rule(float p, float s) { pitch = p; pause = s; } }
        static readonly Dictionary<char, Rule> Punctuation = new Dictionary<char, Rule>
        {
            { '?', new Rule(150, .26f) }, { '？', new Rule(150, .26f) }, { '!', new Rule(100, .23f) }, { '！', new Rule(100, .23f) },
            { '.', new Rule(-45, .22f) }, { '。', new Rule(-45, .22f) }, { ',', new Rule(-30, .14f) }, { '，', new Rule(-30, .14f) }, { '、', new Rule(-20, .1f) },
            { ';', new Rule(-28, .16f) }, { '；', new Rule(-28, .16f) }, { ':', new Rule(-28, .16f) }, { '：', new Rule(-28, .16f) },
            { '…', new Rule(-60, .34f) }, { '—', new Rule(-40, .2f) }, { '-', new Rule(-20, .11f) },
        };

        struct Syllable { public float start, slot, pitch, gain, offset; public int vowel; public int consonant; /* 0 none, 1 plosive, 2 fricative */ }

        static int bankRate;
        static float bankCharacter = -1;
        static float[][] vowels;
        static float[] plosive, fricative;

        /// <summary>The spoken line, or null when it has nothing to say.</summary>
        public static Line Render(string text, Voice voice)
        {
            var syllables = Analyze(text, voice, out var reveal, out float length);
            if (syllables.Count == 0) return null;
            int rate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 44100;
            var mix = new float[Mathf.CeilToInt((length + Tail) * rate)];
            if (voice.mode == Mode.Animalese)
            {
                EnsureBank(rate, voice.voiceCharacter);
                foreach (var s in syllables) Animalese(mix, rate, s, voice);
            }
            else foreach (var s in syllables) Chiptune(mix, rate, s, voice);
            for (int i = 0; i < mix.Length; i++) mix[i] = SoftClip(mix[i] * voice.volume);
            var clip = AudioClip.Create("BlipTalk", mix.Length, 1, rate, false);
            clip.SetData(mix, 0);
            return new Line { clip = clip, reveal = reveal, length = length };
        }

        static List<Syllable> Analyze(string text, Voice voice, out float[] reveal, out float length)
        {
            var list = new List<Syllable>();
            var chars = new List<char>(text?.Length ?? 0);
            bool tag = false;
            if (text != null)
                foreach (char c in text)
                {
                    if (c == '<') { tag = true; continue; }
                    if (c == '>') { tag = false; continue; }
                    // TextMeshPro counts a surrogate pair as one character.
                    if (!tag && !char.IsLowSurrogate(c)) chars.Add(c);
                }
            reveal = new float[chars.Count];
            var random = new Mulberry(Hash(text ?? ""));
            float drift = 0, cursor = 0, runPause = 0;
            bool lifted = false;
            for (int i = 0; i < chars.Count; i++)
            {
                char c = chars[i];
                reveal[i] = cursor;
                if (Punctuation.TryGetValue(c, out var rule))
                {
                    // A mark is silent: the syllable before it carries its intonation, and a run of marks (……, ？！)
                    // pauses once, as long as its longest mark.
                    if (list.Count > 0 && !lifted) { var last = list[list.Count - 1]; last.pitch = Mathf.Clamp(last.pitch + rule.pitch, 40, 8000); list[list.Count - 1] = last; lifted = true; }
                    if (rule.pause > runPause) { cursor += rule.pause - runPause; runPause = rule.pause; }
                    continue;
                }
                if (char.IsWhiteSpace(c))
                {
                    cursor += c == '\n' ? .18f : voice.letter * 1.3f;
                    continue;
                }
                if (char.IsPunctuation(c) || char.IsSymbol(c)) { cursor += .03f; continue; } // quotes, brackets
                runPause = 0; lifted = false;

                drift = drift * .62f + (random.Next() * 2 - 1) * .38f;
                float scatter = (random.Next() * 2 - 1) * voice.jitter * .62f + drift * voice.jitter * .38f;
                char lower = char.ToLowerInvariant(c);
                bool latin = lower >= 'a' && lower <= 'z', digit = c >= '0' && c <= '9';
                float slot = latin ? voice.letter : digit ? (voice.letter + voice.syllable) / 2 : voice.syllable;
                uint h = Hash(c.ToString());
                int vowel = latin ? "AEIOU".IndexOf(LetterVowels[lower - 'a']) : (int)(h % 5);
                int consonant = latin ? (Plosives.IndexOf(lower) >= 0 ? 1 : Fricatives.IndexOf(lower) >= 0 ? 2 : 0)
                    : digit ? 1 : ((h >> 8) % 10) switch { < 4 => 1, < 7 => 2, _ => 0 };
                list.Add(new Syllable
                {
                    start = cursor, slot = slot, vowel = vowel, consonant = consonant,
                    pitch = Mathf.Clamp(voice.basePitch + scatter, 40, 8000),
                    gain = latin || digit || char.IsLetter(c) ? 1 : .72f,
                    offset = random.Next() * .22f,
                });
                cursor += slot;
            }
            length = cursor;
            return list;
        }

        static void Animalese(float[] mix, int rate, Syllable s, Voice voice)
        {
            float register = Mathf.Clamp(voice.basePitch / RateReference, RateMin, RateMax);
            float playback = Mathf.Clamp(s.pitch / voice.basePitch * register * VowelRate[s.vowel], RateMin, RateMax);
            float heard = Mathf.Clamp(Mathf.Min(s.slot * 1.25f, .16f), .032f, .16f);
            int at = Mathf.RoundToInt(s.start * rate);
            if (s.consonant != 0)
            {
                bool p = s.consonant == 1;
                Write(mix, at, p ? plosive : fricative, 0, Mathf.Clamp(playback * (p ? 1 : .9f), .4f, 3), p ? .03f : .05f, s.gain * (p ? .34f : .22f), .001f, rate, 0);
            }
            var buffer = vowels[s.vowel];
            float wanted = heard * playback, bufferSeconds = buffer.Length / (float)rate;
            float offset = s.offset * Mathf.Max(0, bufferSeconds - wanted - .005f);
            float cutoff = Mathf.Clamp(3800 * voice.voiceCharacter, 900, 12000);
            Write(mix, at, buffer, offset * rate, playback, heard, s.gain * .95f, .005f, rate, cutoff);
        }

        static void Chiptune(float[] mix, int rate, Syllable s, Voice voice)
        {
            float note = Mathf.Clamp(Mathf.Min(voice.decay, s.slot * 4.5f), .018f, 1.2f), peak = s.gain * .5f;
            int at = Mathf.RoundToInt(s.start * rate), n = Mathf.CeilToInt(note * rate);
            double phase = 0, inc = s.pitch / rate;
            for (int i = 0; i < n && at + i < mix.Length; i++)
            {
                mix[at + i] += (phase < .5 ? 1 : -1) * Envelope(i / (float)rate, peak, .004f, note);
                phase += inc; if (phase >= 1) phase -= 1;
            }
        }

        /// <summary>Plays <paramref name="source"/> from <paramref name="from"/> (samples) at <paramref name="speed"/> into the mix, under a blip envelope and an optional one-pole low-pass.</summary>
        static void Write(float[] mix, int at, float[] source, float from, float speed, float seconds, float peak, float attack, int rate, float cutoff)
        {
            int n = Mathf.CeilToInt(seconds * rate);
            float k = cutoff > 0 ? 1 - Mathf.Exp(-2 * Mathf.PI * cutoff / rate) : 1, low = 0;
            for (int i = 0; i < n && at + i < mix.Length; i++)
            {
                float pos = from + i * speed;
                int j = (int)pos;
                if (j + 1 >= source.Length) break;
                float sample = Mathf.Lerp(source[j], source[j + 1], pos - j);
                low += k * (sample - low);
                mix[at + i] += low * Envelope(i / (float)rate, peak, attack, seconds);
            }
        }

        /// <summary>Near-instant attack, then an exponential fall to silence at <paramref name="length"/>.</summary>
        static float Envelope(float t, float peak, float attack, float length)
        {
            peak = Mathf.Max(.0002f, peak);
            if (t < attack) return peak * t / attack;
            if (t >= length) return 0;
            return peak * Mathf.Pow(.0001f / peak, (t - attack) / Mathf.Max(.001f, length - attack));
        }

        static float SoftClip(float x)
        {
            float a = Mathf.Abs(x);
            return a <= .7f ? x : Mathf.Sign(x) * (.7f + .3f * (float)Math.Tanh((a - .7f) / .3f));
        }

        static void EnsureBank(int rate, float character)
        {
            if (vowels != null && bankRate == rate && Mathf.Approximately(bankCharacter, character)) return;
            bankRate = rate; bankCharacter = character;
            vowels = new float[5][];
            for (int v = 0; v < 5; v++) vowels[v] = Vowel(rate, v, character);
            plosive = Noise(rate, true, 7);
            fricative = Noise(rate, false, 7);
        }

        /// <summary>A harmonic series at 120 Hz (1/n^1.15 tilt, random phases) shaped by the vowel's three formants.</summary>
        static float[] Vowel(int rate, int vowel, float scale)
        {
            int length = Mathf.Max(1, Mathf.FloorToInt(rate * VowelSeconds));
            var data = new float[length];
            var random = new Mulberry((uint)(1 + "AEIOU"[vowel] * 977));
            var freqs = new List<float>(); var amps = new List<float>(); var phases = new List<float>();
            for (int n = 1; n * VoiceF0 < Mathf.Min(5200, rate * .45f); n++)
            {
                float harmonic = n * VoiceF0, resonance = 0;
                for (int f = 0; f < 3; f++)
                {
                    float detune = (harmonic - FormantFreq[vowel][f] * scale) / (FormantBw[vowel][f] * (.7f + scale * .3f));
                    resonance += FormantGain[vowel][f] / (1 + detune * detune);
                }
                float amplitude = resonance / Mathf.Pow(n, 1.15f);
                if (amplitude > .002f) { freqs.Add(harmonic); amps.Add(amplitude); phases.Add(random.Next() * Mathf.PI * 2); }
            }
            const float attack = .012f;
            float release = VowelSeconds * .55f, peak = 0;
            int fade = Mathf.Max(1, Mathf.FloorToInt(.004f * rate));
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)rate, sample = 0;
                for (int p = 0; p < freqs.Count; p++) sample += amps[p] * Mathf.Sin(2 * Mathf.PI * freqs[p] * t + phases[p]);
                float env = t < attack ? t / attack : .62f + .38f * Mathf.Exp(-(t - attack) * 9);
                if (t > release) env *= Mathf.Exp(-(t - release) / ((VowelSeconds - release) * .36f));
                int fromEnd = length - 1 - i;
                if (fromEnd < fade) env *= fromEnd / (float)fade;
                data[i] = sample * env;
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }
            if (peak > 0) for (int i = 0; i < length; i++) data[i] *= .92f / peak;
            return data;
        }

        /// <summary>A 55 ms low-passed thump (plosive) or a 100 ms high-passed hiss (fricative).</summary>
        static float[] Noise(int rate, bool plosiveKind, uint seed)
        {
            var random = new Mulberry(seed + (uint)(plosiveKind ? 7 : 9) * 131);
            int length = Mathf.Max(1, Mathf.FloorToInt(rate * (plosiveKind ? .055f : .1f)));
            var data = new float[length];
            float lp = 1 - Mathf.Exp(-2 * Mathf.PI * (plosiveKind ? 1500 : 2600) / rate);
            float hp = 1 - Mathf.Exp(-2 * Mathf.PI * (plosiveKind ? 180 : 2600) / rate);
            float low = 0, lowForHigh = 0, peak = 0;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)rate, noise = random.Next() * 2 - 1;
                low += lp * (noise - low);
                lowForHigh += hp * (noise - lowForHigh);
                float sample = plosiveKind ? low : noise - lowForHigh;
                float env = plosiveKind ? Mathf.Exp(-t * 62) : Mathf.Min(1, t / .006f) * Mathf.Exp(-t * 24);
                data[i] = sample * env;
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }
            if (peak > 0) for (int i = 0; i < length; i++) data[i] *= .85f / peak;
            return data;
        }

        /// <summary>FNV-1a.</summary>
        static uint Hash(string text)
        {
            uint h = 2166136261;
            foreach (char c in text) { h ^= c; h *= 16777619; }
            return h;
        }

        /// <summary>mulberry32: the same seed gives the same performance.</summary>
        struct Mulberry
        {
            uint a;
            public Mulberry(uint seed) { a = seed; }
            public float Next()
            {
                a += 0x6d2b79f5;
                uint t = a;
                t = (t ^ (t >> 15)) * (t | 1);
                t ^= t + (t ^ (t >> 7)) * (t | 61);
                return (t ^ (t >> 14)) / 4294967296f;
            }
        }
    }
}
