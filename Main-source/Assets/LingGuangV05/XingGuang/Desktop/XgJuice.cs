using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Game feel for 灵光.exe, all inside its own canvas: flash, knockback / punch, floating text, particles, hit-stop,
    /// screen shake, shockwaves, glow and procedural sound. Effects run on unscaled time and pause together during a
    /// hit-stop. "Reduce effects" drops shake and hit-stop; automatic events call the light variants.
    /// </summary>
    public sealed class XgJuice : MonoBehaviour
    {
        RectTransform fxLayer, shakeTarget;
        Image flash;
        TMP_FontAsset font;
        Vector2 shakeBase;
        float shakeAmp, shakeTime, shakeDuration, flashTime, flashDuration, hitstop, lastShake = -10;
        Color flashColor;
        bool hitstopOwned;
        float previousTimeScale = 1;
        public bool Reduced, Muted;
        /// <summary>Effects are only drawn while the window is visible.</summary>
        public Func<bool> Visible = () => true;

        sealed class Floater { public TMP_Text text; public Vector2 from; public float t, life, rise, scale; public Color color; }
        sealed class Particle { public RectTransform rt; public Graphic g; public Vector2 pos, vel, from, to; public float t, life, spin, gravity; public Color color; public bool travelling; }
        sealed class Wave { public XgRingGraphic ring; public Vector2 pos; public float t, life, radius, width; public Color color; }
        sealed class Punch { public RectTransform rt; public Vector3 scale; public Vector2 pos, dir; public float t, life, amp; }

        readonly List<Floater> floaters = new List<Floater>();
        readonly List<Particle> particles = new List<Particle>();
        readonly List<Wave> waves = new List<Wave>();
        readonly Dictionary<RectTransform, Punch> punches = new Dictionary<RectTransform, Punch>();
        readonly Stack<TMP_Text> textPool = new Stack<TMP_Text>();
        readonly Stack<Particle> imagePool = new Stack<Particle>();
        readonly Stack<Particle> glyphPool = new Stack<Particle>();
        readonly Stack<XgRingGraphic> ringPool = new Stack<XgRingGraphic>();

        public void Init(RectTransform root, RectTransform shake, TMP_FontAsset fontAsset)
        {
            font = fontAsset;
            shakeTarget = shake;
            shakeBase = shake.anchoredPosition;
            var go = new GameObject("Fx", typeof(RectTransform));
            go.layer = root.gameObject.layer;
            fxLayer = (RectTransform)go.transform;
            fxLayer.SetParent(root, false);
            fxLayer.anchorMin = Vector2.zero; fxLayer.anchorMax = Vector2.one; fxLayer.offsetMin = fxLayer.offsetMax = Vector2.zero;
            var f = new GameObject("Flash", typeof(RectTransform), typeof(Image));
            f.layer = go.layer;
            var frt = (RectTransform)f.transform; frt.SetParent(fxLayer, false);
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
            flash = f.GetComponent<Image>(); flash.raycastTarget = false; flash.color = Color.clear;
            Sfx.Init(gameObject);
        }

        /// <summary>Keep the effect layer above pages that are built later.</summary>
        public void BringToFront() { if (fxLayer != null) fxLayer.SetAsLastSibling(); }

        /// <summary>Position of a UI element in the effect layer's space.</summary>
        public Vector2 At(RectTransform rt, Vector2 offset = default)
        {
            if (rt == null || fxLayer == null) return Vector2.zero;
            Vector3 world = rt.TransformPoint(rt.rect.center);
            return (Vector2)fxLayer.InverseTransformPoint(world) + offset;
        }

        // ───────────── effects ─────────────

        public void Flash(Color color, float seconds = .09f, float alpha = .8f)
        {
            if (!Visible()) return;
            flashColor = color; flashColor.a = alpha * (Reduced ? .4f : 1);
            flashTime = 0; flashDuration = seconds;
        }

        public void Shake(float amplitude, float seconds = .25f)
        {
            if (Reduced || !Visible() || shakeTarget == null) return;
            if (Time.unscaledTime - lastShake < .3f && amplitude <= shakeAmp) return;
            lastShake = Time.unscaledTime;
            shakeAmp = amplitude; shakeTime = 0; shakeDuration = seconds;
        }

        /// <summary>Freezes the game and these effects for a few milliseconds.</summary>
        public void HitStop(float ms)
        {
            if (Reduced || !Visible()) return;
            hitstop = Mathf.Max(hitstop, ms / 1000f);
            if (Time.timeScale > 0) { previousTimeScale = Time.timeScale; Time.timeScale = 0; hitstopOwned = true; }
        }

        /// <summary>Scale punch (buttons "pressed in" then overshoot) plus an optional knockback offset.</summary>
        public void Knock(RectTransform rt, float amp = .08f, Vector2 dir = default, float seconds = .22f)
        {
            if (rt == null || !Visible()) return;
            if (!punches.TryGetValue(rt, out var p)) { p = new Punch { rt = rt, scale = rt.localScale, pos = rt.anchoredPosition }; punches[rt] = p; }
            p.t = 0; p.life = seconds; p.amp = amp; p.dir = dir;
        }

        public void Float(Vector2 at, string text, Color color, float size = 22, float rise = 60, float life = .75f, float scale = 1.3f)
        {
            if (!Visible()) return;
            var t = textPool.Count > 0 ? textPool.Pop() : NewText();
            t.gameObject.SetActive(true);
            t.text = text; t.fontSize = size; t.color = color;
            t.rectTransform.SetAsLastSibling();
            floaters.Add(new Floater { text = t, from = at, t = 0, life = life, rise = rise, scale = scale, color = color });
        }

        public enum Shape { Square, Confetti, Yen, Star, Spark }

        public void Burst(Vector2 at, int count, Color color, Shape shape = Shape.Square, float speed = 260, float gravity = 600, float life = .7f)
        {
            if (!Visible()) return;
            if (Reduced) count = Mathf.Max(1, count / 2);
            for (int i = 0; i < count; i++)
            {
                bool glyph = shape == Shape.Yen || shape == Shape.Star;
                var p = glyph ? (glyphPool.Count > 0 ? glyphPool.Pop() : NewGlyph()) : (imagePool.Count > 0 ? imagePool.Pop() : NewImage());
                p.rt.gameObject.SetActive(true);
                p.rt.SetAsLastSibling();
                float a = UnityEngine.Random.value * Mathf.PI * 2, s = speed * (.35f + UnityEngine.Random.value * .9f);
                p.travelling = false; p.pos = at; p.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s + Vector2.up * speed * .35f;
                p.t = 0; p.life = life * (.7f + UnityEngine.Random.value * .6f); p.gravity = gravity;
                p.spin = (UnityEngine.Random.value * 2 - 1) * 720;
                p.color = shape == Shape.Confetti ? Color.HSVToRGB(UnityEngine.Random.value, .7f, 1) : color;
                if (glyph) ((TMP_Text)p.g).text = shape == Shape.Yen ? "¥" : "★";
                float size = shape == Shape.Spark ? 4 + UnityEngine.Random.value * 4 : shape == Shape.Confetti ? 7 + UnityEngine.Random.value * 6 : glyph ? 18 + UnityEngine.Random.value * 10 : 5 + UnityEngine.Random.value * 6;
                p.rt.sizeDelta = shape == Shape.Confetti ? new Vector2(size, size * .5f) : glyph ? new Vector2(40, 40) : new Vector2(size, size);
                if (glyph) ((TMP_Text)p.g).fontSize = size;
                p.g.color = p.color;
                particles.Add(p);
            }
        }

        public static Vector2 CoinFlightPosition(Vector2 from, Vector2 to, float fraction)
        {
            float t = Mathf.Clamp01(fraction);
            return Vector2.Lerp(from, to, t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 70;
        }

        /// <summary>Visible payment direction, using the existing bounded glyph pool.</summary>
        public void TransferCoins(Vector2 from, Vector2 to, int count = 8)
        {
            if (!Visible()) return;
            count = Mathf.Clamp(Reduced ? count / 2 : count, 1, 12);
            for (int i = 0; i < count; i++)
            {
                var p = glyphPool.Count > 0 ? glyphPool.Pop() : NewGlyph();
                p.travelling = true; p.from = from; p.to = to;
                p.pos = from; p.vel = Vector2.zero; p.gravity = p.spin = 0;
                p.t = 0; p.life = .45f + i * .035f; p.color = XgPalette.Money;
                ((TMP_Text)p.g).text = "¥"; ((TMP_Text)p.g).fontSize = 20;
                p.rt.gameObject.SetActive(true); p.rt.SetAsLastSibling();
                p.rt.sizeDelta = new Vector2(40,40); p.rt.anchoredPosition = from; p.g.color = p.color;
                particles.Add(p);
            }
        }

        /// <summary>Particles falling from a point (a broken combo crumbling).</summary>
        public void Crumble(Vector2 at, int count, Color color)
        {
            if (!Visible()) return;
            for (int i = 0; i < count; i++)
            {
                var p = imagePool.Count > 0 ? imagePool.Pop() : NewImage();
                p.rt.gameObject.SetActive(true);
                p.travelling = false; p.pos = at + new Vector2((UnityEngine.Random.value - .5f) * 60, (UnityEngine.Random.value - .5f) * 20);
                p.vel = new Vector2((UnityEngine.Random.value - .5f) * 120, UnityEngine.Random.value * 80);
                p.t = 0; p.life = .9f; p.gravity = 900; p.spin = (UnityEngine.Random.value * 2 - 1) * 400; p.color = color;
                p.rt.sizeDelta = Vector2.one * (5 + UnityEngine.Random.value * 7);
                p.g.color = color;
                particles.Add(p);
            }
        }

        public void Shockwave(Vector2 at, Color color, float radius = 220, float seconds = .35f, float width = 10)
        {
            if (!Visible()) return;
            var ring = ringPool.Count > 0 ? ringPool.Pop() : NewRing();
            ring.gameObject.SetActive(true);
            ring.rectTransform.SetAsLastSibling();
            waves.Add(new Wave { ring = ring, pos = at, t = 0, life = seconds, radius = radius, width = width, color = color });
        }

        public void Play(Sfx.Id id, float pitch = 1, float volume = 1) { if (!Muted && Visible()) Sfx.Play(id, pitch, volume); }

        // ───────────── update ─────────────

        void Update()
        {
            float raw = Time.unscaledDeltaTime;
            if (hitstop > 0)
            {
                hitstop -= raw;
                if (hitstop <= 0 && hitstopOwned) RestoreTimeScale();
                return;
            }
            float dt = Mathf.Min(raw, .05f);

            if (flashDuration > 0)
            {
                flashTime += dt;
                float k = 1 - Mathf.Clamp01(flashTime / flashDuration);
                var c = flashColor; c.a *= k; flash.color = c;
                if (k <= 0) flashDuration = 0;
            }
            if (shakeTarget != null)
            {
                if (shakeDuration > 0)
                {
                    shakeTime += dt;
                    float k = 1 - Mathf.Clamp01(shakeTime / shakeDuration);
                    shakeTarget.anchoredPosition = shakeBase + UnityEngine.Random.insideUnitCircle * shakeAmp * k * k;
                    if (k <= 0) { shakeDuration = 0; shakeAmp = 0; shakeTarget.anchoredPosition = shakeBase; }
                }
            }

            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                var f = floaters[i];
                f.t += dt;
                float k = f.t / f.life;
                if (k >= 1) { f.text.gameObject.SetActive(false); textPool.Push(f.text); floaters.RemoveAt(i); continue; }
                float ease = 1 - (1 - k) * (1 - k);
                f.text.rectTransform.anchoredPosition = f.from + Vector2.up * f.rise * ease;
                float pop = k < .15f ? Mathf.Lerp(.6f, f.scale, k / .15f) : Mathf.Lerp(f.scale, 1, (k - .15f) / .85f);
                f.text.rectTransform.localScale = Vector3.one * pop;
                var c = f.color; c.a = k < .6f ? 1 : 1 - (k - .6f) / .4f; f.text.color = c;
            }

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.t += dt;
                if (p.t >= p.life)
                {
                    p.rt.gameObject.SetActive(false);
                    (p.g is TMP_Text ? glyphPool : imagePool).Push(p);
                    particles.RemoveAt(i); continue;
                }
                if (p.travelling) p.pos = CoinFlightPosition(p.from, p.to, p.t / p.life);
                else
                {
                    p.vel += Vector2.down * p.gravity * dt;
                    p.vel *= 1 - 1.2f * dt;
                    p.pos += p.vel * dt;
                }
                p.rt.anchoredPosition = p.pos;
                p.rt.localRotation = Quaternion.Euler(0, 0, p.spin * p.t);
                var c = p.color; c.a = 1 - Mathf.Pow(p.t / p.life, 2); p.g.color = c;
            }

            for (int i = waves.Count - 1; i >= 0; i--)
            {
                var w = waves[i];
                w.t += dt;
                float k = w.t / w.life;
                if (k >= 1) { w.ring.gameObject.SetActive(false); ringPool.Push(w.ring); waves.RemoveAt(i); continue; }
                float ease = 1 - Mathf.Pow(1 - k, 3);
                float d = Mathf.Lerp(20, w.radius * 2, ease);
                w.ring.rectTransform.anchoredPosition = w.pos;
                w.ring.rectTransform.sizeDelta = new Vector2(d, d);
                w.ring.Width = Mathf.Lerp(w.width, 1, k);
                var c = w.color; c.a = (1 - k) * w.color.a; w.ring.color = c;
            }

            if (punches.Count > 0)
            {
                var done = new List<RectTransform>();
                foreach (var p in punches.Values)
                {
                    if (p.rt == null) { done.Add(p.rt); continue; }
                    p.t += dt;
                    float k = p.t / p.life;
                    if (k >= 1) { p.rt.localScale = p.scale; p.rt.anchoredPosition = p.pos; done.Add(p.rt); continue; }
                    // Pressed in first (−amp), then overshoot, settling with a damped wobble.
                    float wobble = Mathf.Sin(k * Mathf.PI * 2.5f) * (1 - k);
                    p.rt.localScale = p.scale * (1 - p.amp * wobble);
                    p.rt.anchoredPosition = p.pos + p.dir * wobble;
                }
                foreach (var rt in done) punches.Remove(rt);
            }
        }

        void OnDisable()
        {
            if (hitstopOwned) RestoreTimeScale();
            if (shakeTarget != null) shakeTarget.anchoredPosition = shakeBase;
            foreach (var p in punches.Values) if (p.rt != null) { p.rt.localScale = p.scale; p.rt.anchoredPosition = p.pos; }
            punches.Clear();
        }

        void RestoreTimeScale()
        {
            // Do not overwrite another system if it has already restored or changed the scale.
            if (Time.timeScale == 0) Time.timeScale = previousTimeScale;
            hitstopOwned = false; hitstop = 0;
        }

        // ───────────── pools ─────────────

        TMP_Text NewText()
        {
            var go = new GameObject("Float", typeof(RectTransform));
            go.layer = fxLayer.gameObject.layer;
            var rt = (RectTransform)go.transform; rt.SetParent(fxLayer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.sizeDelta = new Vector2(420, 60);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.fontStyle = FontStyles.Bold;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.richText = true;
            t.outlineWidth = .18f; t.outlineColor = new Color32(20, 22, 40, 200);
            return t;
        }

        Particle NewImage()
        {
            var go = new GameObject("Bit", typeof(RectTransform), typeof(Image));
            go.layer = fxLayer.gameObject.layer;
            var rt = (RectTransform)go.transform; rt.SetParent(fxLayer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            var img = go.GetComponent<Image>(); img.raycastTarget = false;
            return new Particle { rt = rt, g = img };
        }

        Particle NewGlyph()
        {
            var go = new GameObject("Glyph", typeof(RectTransform));
            go.layer = fxLayer.gameObject.layer;
            var rt = (RectTransform)go.transform; rt.SetParent(fxLayer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.fontStyle = FontStyles.Bold;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return new Particle { rt = rt, g = t };
        }

        XgRingGraphic NewRing()
        {
            var go = new GameObject("Wave", typeof(RectTransform));
            go.layer = fxLayer.gameObject.layer;
            var rt = (RectTransform)go.transform; rt.SetParent(fxLayer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            var ring = go.AddComponent<XgRingGraphic>(); ring.raycastTarget = false;
            return ring;
        }

        // ───────────── sound ─────────────

        /// <summary>Procedural sound effects (no audio assets): built once, played through a small pool of sources.</summary>
        public static class Sfx
        {
            public enum Id { Click, Ding, Clack, Whoosh, Fan, Tick, Stamp, Fanfare, Buzz, Thud, Charge, Unlock, Coin, Break, Tier, Swoosh }
            public const int VariantsPerEvent = 3;
            const int Rate = 44100;
            static AudioClip[,] clips;
            static int[] lastVariant;
            static AudioSource[] sources;
            static int next;

            public static void Init(GameObject host)
            {
                EnsureClips();
                sources = new AudioSource[6];
                for (int i = 0; i < sources.Length; i++)
                {
                    var s = host.AddComponent<AudioSource>();
                    s.playOnAwake = false; s.spatialBlend = 0; s.volume = .35f; s.ignoreListenerPause = true;
                    sources[i] = s;
                }
            }

            public static void Play(Id id, float pitch = 1, float volume = 1)
            {
                if (sources == null || clips == null) return;
                var s = sources[next = (next + 1) % sources.Length];
                if (s == null) return;
                s.pitch = Mathf.Clamp(pitch, .3f, 3f);
                s.volume = .35f * volume;
                int variant = (lastVariant[(int)id] + UnityEngine.Random.Range(1, VariantsPerEvent)) % VariantsPerEvent;
                lastVariant[(int)id] = variant;
                s.clip = clips[(int)id, variant];
                s.Play();
            }

            public static AudioClip Clip(Id id, int variant)
            {
                if (variant < 0 || variant >= VariantsPerEvent) throw new ArgumentOutOfRangeException(nameof(variant));
                EnsureClips();
                return clips[(int)id, variant];
            }

            static void EnsureClips()
            {
                if (clips != null) return;
                int count = Enum.GetValues(typeof(Id)).Length;
                clips = new AudioClip[count, VariantsPerEvent];
                lastVariant = new int[count];
                foreach (Id id in Enum.GetValues(typeof(Id)))
                    for (int variant = 0; variant < VariantsPerEvent; variant++) clips[(int)id, variant] = Build(id, variant);
            }

            static AudioClip Build(Id id, int variant)
            {
                float length;
                switch (id)
                {
                    case Id.Fanfare: length = .7f; break;
                    case Id.Buzz: length = .45f; break;
                    case Id.Charge: length = .6f; break;
                    case Id.Whoosh: case Id.Swoosh: length = .28f; break;
                    case Id.Tier: length = .5f; break;
                    case Id.Break: length = .4f; break;
                    case Id.Unlock: length = .35f; break;
                    default: length = .18f; break;
                }
                length *= .92f + variant * .08f;
                int n = (int)(Rate * length);
                var data = new float[n];
                var rng = new System.Random((int)id * 97 + variant * 1013 + 3);
                float noise = 0;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate * (.96f + variant * .04f), k = (float)i / n;
                    float white = (float)rng.NextDouble() * 2 - 1;
                    noise = noise * .7f + white * .3f;
                    float v;
                    switch (id)
                    {
                        case Id.Click: v = Sq(t, 1800) * Env(k, 40) * .5f; break;
                        case Id.Ding: v = (Sin(t, 1320) + .4f * Sin(t, 2640)) * Env(k, 6) * .6f; break;
                        case Id.Clack: v = (noise * .8f + Sq(t, 220) * .4f) * Env(k, 18); break;
                        case Id.Whoosh: v = noise * Mathf.Sin(k * Mathf.PI) * .6f; break;
                        case Id.Swoosh: v = Sin(t, Mathf.Lerp(300, 1400, k) * .5f) * Mathf.Sin(k * Mathf.PI) * .35f + noise * Mathf.Sin(k * Mathf.PI) * .3f; break;
                        case Id.Fan: v = (Saw(t, 140) * .3f + noise * .5f) * Mathf.Sin(k * Mathf.PI) * .5f; break;
                        case Id.Tick: v = Sin(t, 2400) * Env(k, 60) * .5f; break;
                        case Id.Stamp: v = (Sin(t, Mathf.Lerp(140, 50, k)) * .9f + noise * .5f * Env(k, 30)) * Env(k, 7); break;
                        case Id.Fanfare:
                        {
                            float[] notes = { 523f, 659f, 784f, 1046f };
                            int idx = Mathf.Min(3, (int)(k * 5));
                            float local = k * 5 - idx;
                            v = (Sq(t, notes[idx]) * .25f + Sin(t, notes[idx]) * .4f) * (idx == 3 ? Env((k - .6f) / .4f, 3) : Env(local, 4)) * .7f;
                            break;
                        }
                        case Id.Buzz: v = (Saw(t, 110 + 40 * Mathf.Sin(t * 60)) * .5f + noise * .4f) * (1 - k) * .7f; break;
                        case Id.Thud: v = Sin(t, Mathf.Lerp(120, 60, k)) * Env(k, 8) * .8f; break;
                        case Id.Charge: v = (Sin(t, Mathf.Lerp(300, 900, k) * (1 + k)) * .35f) * Mathf.Min(1, k * 4) * (1 - Mathf.Pow(k, 8)); break;
                        case Id.Unlock: v = (Sin(t, k < .4f ? 880 : 1320) * .5f + Sq(t, k < .4f ? 440 : 660) * .15f) * Env(k < .4f ? k / .4f : (k - .4f) / .6f, 5); break;
                        case Id.Coin: v = Sin(t, k < .35f ? 1568 : 2093) * Env(k < .35f ? k / .35f : (k - .35f) / .65f, 6) * .5f; break;
                        case Id.Break: v = (Sq(t, Mathf.Lerp(600, 120, k)) * .3f + noise * .3f) * (1 - k); break;
                        case Id.Tier: v = (Sin(t, 880 * (1 + Mathf.Floor(k * 3) * .25f)) * .5f + Sin(t, 1760) * .15f) * (1 - k) * .8f; break;
                        default: v = 0; break;
                    }
                    data[i] = Mathf.Clamp(v, -1, 1);
                }
                var clip = AudioClip.Create("xg_" + id + "_v" + variant, n, 1, Rate, false);
                clip.SetData(data, 0);
                return clip;
            }

            static float Sin(float t, float f) => Mathf.Sin(2 * Mathf.PI * f * t);
            static float Sq(float t, float f) => Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * t));
            static float Saw(float t, float f) => 2 * (t * f - Mathf.Floor(t * f + .5f));
            static float Env(float k, float decay) => k < 0 ? 0 : Mathf.Exp(-k * decay) * Mathf.Min(1, k * 60);
        }
    }

    /// <summary>A soft pulsing halo behind a button or node (buyable / combo hot).</summary>
    public sealed class XgGlow : MonoBehaviour
    {
        public Image image;
        public Color color = new Color(1, .85f, .3f, .55f);
        public bool on;
        public float speed = 5.2f;
        void Update()
        {
            if (image == null) return;
            float a = on ? (.45f + .55f * (.5f + .5f * Mathf.Sin(Time.unscaledTime * speed))) : 0;
            var c = color; c.a *= a; image.color = c;
        }
    }
}
