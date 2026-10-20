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
    public sealed partial class XgJuice : MonoBehaviour
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
        public bool DisableMotionWhenReduced;
        bool MotionDisabled => DisableMotionWhenReduced && Reduced;
        public const int FloaterLimit = 8, ParticleLimit = 96;
        public int ActiveFloaters => floaters.Count;
        public int ActiveParticles => particles.Count;
        /// <summary>Opt-in for private label stacks: adopt external absolute layout/rest writes.</summary>
        public bool TrackKnockLayoutChanges;
        /// <summary>Opt-in for desktop minigames: freeze only this effect stack, never the shared simulation.</summary>
        public bool LocalHitStop;
        public bool IsHitStopped => hitstop > 0;
        /// <summary>Effects are only drawn while the window is visible.</summary>
        public Func<bool> Visible = () => true;

        sealed class Floater { public TMP_Text text; public Vector2 from; public float t, life, rise, scale; public Color color; }
        sealed class Particle { public RectTransform rt; public Graphic g; public Vector2 pos, vel, from, to; public float t, life, spin, gravity; public Color color; public bool travelling; }
        sealed class Wave { public XgRingGraphic ring; public Vector2 pos; public float t, life, radius, width; public Color color; }
        sealed class Blink { public Image image; public float t, life, alpha; }
        sealed class Punch
        {
            public RectTransform rt;
            public Vector3 scale, appliedScale, settleScale;
            public Vector2 pos, dir, appliedPos, settlePos;
            public float t, life, amp;
            public bool squash, settling, press, held;
        }

        readonly List<Floater> floaters = new List<Floater>();
        readonly List<Particle> particles = new List<Particle>();
        readonly List<Wave> waves = new List<Wave>();
        readonly List<Blink> blinks = new List<Blink>();
        readonly Dictionary<RectTransform, Blink> blinkOf = new Dictionary<RectTransform, Blink>();
        readonly Dictionary<RectTransform, Punch> punches = new Dictionary<RectTransform, Punch>();
        List<RectTransform> completedPunches;
        readonly Stack<TMP_Text> textPool = new Stack<TMP_Text>();
        readonly Stack<Particle> imagePool = new Stack<Particle>();
        readonly Stack<Particle> glyphPool = new Stack<Particle>();
        readonly Stack<XgRingGraphic> ringPool = new Stack<XgRingGraphic>();

        public void Init(RectTransform root, RectTransform shake, TMP_FontAsset fontAsset, GameObject soundHost = null)
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
            Sfx.Init(soundHost != null ? soundHost : gameObject, soundHost != null);
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
            if (MotionDisabled) return;
            if (!Visible()) return;
            flashColor = color; flashColor.a = alpha * (Reduced ? .4f : 1);
            flashTime = 0; flashDuration = seconds;
            if (flash != null) flash.color = flashColor;
        }

        public void Shake(float amplitude, float seconds = .25f)
        {
            if (MotionDisabled) return;
            if (Reduced || !Visible() || shakeTarget == null) return;
            if (Time.unscaledTime - lastShake < .3f && amplitude <= shakeAmp) return;
            lastShake = Time.unscaledTime;
            shakeAmp = amplitude; shakeTime = 0; shakeDuration = seconds;
        }

        /// <summary>Freezes the game and these effects for a few milliseconds.</summary>
        public void HitStop(float ms)
        {
            if (MotionDisabled) return;
            if (Reduced || !Visible()) return;
            hitstop = Mathf.Max(hitstop, ms / 1000f);
            // Only in Play mode: an edit-mode page render would leave the global clock at 0 and the next Play starts frozen.
            if (Application.isPlaying && !LocalHitStop && Time.timeScale > 0) { previousTimeScale = Time.timeScale; Time.timeScale = 0; hitstopOwned = true; }
        }

        /// <summary>Scale punch (buttons "pressed in" then overshoot) plus an optional knockback offset.</summary>
        public void Knock(RectTransform rt, float amp = .08f, Vector2 dir = default, float seconds = .22f, bool squash = false)
        {
            if (MotionDisabled) return;
            if (rt == null || !Visible()) return;
            if (!punches.TryGetValue(rt, out var p))
            {
                p = new Punch { rt = rt, scale = rt.localScale, pos = rt.anchoredPosition };
                punches[rt] = p;
                RememberApplied(p);
            }
            RebaseExternalTransform(p);
            p.t = 0; p.life = seconds; p.amp = amp; p.dir = dir; p.squash = squash; p.settling = false; p.press = p.held = false;
        }

        /// <summary>Hold a visual target at 95%; do not use a hit rectangle or business FX stack.</summary>
        public void HoldPress(RectTransform rt)
        {
            if (rt == null || !Visible()) return;
            Knock(rt, .05f);
            if (!punches.TryGetValue(rt, out var p)) return;
            p.press = p.held = true;
            rt.localScale = p.scale * .95f;
            rt.anchoredPosition = p.pos;
            RememberApplied(p);
        }

        /// <summary>Release from the current pose with a short, bounded rebound.</summary>
        public void ReleasePress(RectTransform rt)
        {
            if (rt == null || !punches.TryGetValue(rt, out var p) || !p.press || !p.held) return;
            RebaseExternalTransform(p);
            p.held = false; p.t = 0; p.life = .22f;
            p.settleScale = rt.localScale;
            RememberApplied(p);
        }

        /// <summary>True until Knock restores its target or is explicitly cleared.</summary>
        public bool HasActiveKnock(RectTransform target) => target != null && punches.ContainsKey(target);

        /// <summary>Return quietly from the current pose without extending the remaining lifetime.</summary>
        public void SettleKnock(RectTransform rt, float seconds = .06f)
        {
            if (rt == null || !punches.TryGetValue(rt, out var p) || p.settling) return;
            float remaining = p.life - p.t;
            if (remaining <= 0 || seconds <= 0) return;
            RebaseExternalTransform(p);
            p.settleScale = rt.localScale; p.settlePos = rt.anchoredPosition;
            p.life = Mathf.Min(seconds, remaining); p.t = 0; p.settling = true; p.press = p.held = false;
            RememberApplied(p);
        }

        void RebaseExternalTransform(Punch p)
        {
            if (!TrackKnockLayoutChanges || p.rt == null) return;
            var scale = p.rt.localScale;
            var pos = p.rt.anchoredPosition;
            // Layout writes absolute rest values. Untouched axes must not absorb wobble.
            for (int axis = 0; axis < 3; axis++)
                if (scale[axis] != p.appliedScale[axis])
                {
                    p.scale[axis] = scale[axis];
                    if (p.settling || (p.press && !p.held)) p.settleScale[axis] = scale[axis];
                }
            for (int axis = 0; axis < 2; axis++)
                if (pos[axis] != p.appliedPos[axis])
                {
                    p.pos[axis] = pos[axis];
                    if (p.settling) p.settlePos[axis] = pos[axis];
                }
        }

        /// <summary>Declare a complete absolute rest transform after a custom layout write,
        /// including the ambiguous case where its new value equals our last animated value.</summary>
        public void SetKnockRestTransform(RectTransform rt, Vector3 scale, Vector2 position)
        {
            if (!TrackKnockLayoutChanges || rt == null || !punches.TryGetValue(rt, out var p)) return;
            p.scale = scale; p.pos = position;
            if (p.settling) { p.settleScale = rt.localScale; p.settlePos = rt.anchoredPosition; }
            RememberApplied(p);
        }

        static void RememberApplied(Punch p)
        {
            p.appliedScale = p.rt.localScale;
            p.appliedPos = p.rt.anchoredPosition;
        }

        public void Float(Vector2 at, string text, Color color, float size = 22, float rise = 60, float life = .75f, float scale = 1.3f)
        {
            if (MotionDisabled) return;
            if (!Visible()) return;
            if (floaters.Count >= FloaterLimit)
            {
                var oldest = floaters[0];
                oldest.text.gameObject.SetActive(false); textPool.Push(oldest.text); floaters.RemoveAt(0);
            }
            var t = textPool.Count > 0 ? textPool.Pop() : NewText();
            t.gameObject.SetActive(true);
            t.text = text; t.fontSize = size; t.color = color;
            t.rectTransform.anchoredPosition = at; t.rectTransform.localScale = Vector3.one * .6f;
            t.rectTransform.SetAsLastSibling();
            floaters.Add(new Floater { text = t, from = at, t = 0, life = life, rise = rise, scale = scale, color = color });
        }

        public enum Shape { Square, Confetti, Yen, Star, Spark }

        public void Burst(Vector2 at, int count, Color color, Shape shape = Shape.Square, float speed = 260, float gravity = 600, float life = .7f, float scale = 1)
        {
            if (MotionDisabled) return;
            if (!Visible() || GamePerformance.Low) return;
            if (Reduced) count = Mathf.Max(1, count / 2);
            count = Mathf.Clamp(count, 0, ParticleLimit - particles.Count);
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
                float size = (shape == Shape.Spark ? 4 + UnityEngine.Random.value * 4 : shape == Shape.Confetti ? 7 + UnityEngine.Random.value * 6 : glyph ? 18 + UnityEngine.Random.value * 10 : 5 + UnityEngine.Random.value * 6) * scale;
                p.rt.sizeDelta = shape == Shape.Confetti ? new Vector2(size, size * .5f) : glyph ? new Vector2(40, 40) : new Vector2(size, size);
                if (glyph) ((TMP_Text)p.g).fontSize = size;
                p.g.color = p.color;
                p.rt.anchoredPosition = at; p.rt.localRotation = Quaternion.identity;
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
            if (MotionDisabled) return;
            if (!Visible() || GamePerformance.Low) return;
            count = Mathf.Clamp(Reduced ? count / 2 : count, 1, 12);
            count = Mathf.Min(count, ParticleLimit - particles.Count);
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
            if (MotionDisabled) return;
            if (!Visible() || GamePerformance.Low) return;
            count = Mathf.Clamp(count, 0, ParticleLimit - particles.Count);
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
            if (MotionDisabled) return;
            if (!Visible()) return;
            var ring = ringPool.Count > 0 ? ringPool.Pop() : NewRing();
            ring.gameObject.SetActive(true);
            ring.rectTransform.SetAsLastSibling();
            ring.rectTransform.anchoredPosition = at; ring.rectTransform.sizeDelta = Vector2.one * 20;
            ring.Width = width; ring.color = color;
            waves.Add(new Wave { ring = ring, pos = at, t = 0, life = seconds, radius = radius, width = width, color = color });
        }

        /// <summary>闪白: the element itself goes white for a moment (an overlay on it, so it moves with its knock).</summary>
        public void FlashWhite(RectTransform rt, float seconds = .1f, float alpha = .85f)
        {
            if (MotionDisabled) return;
            if (rt == null || !Visible()) return;
            if (!blinkOf.TryGetValue(rt, out var b) || b.image == null)
            {
                var go = new GameObject("Blink", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                go.layer = rt.gameObject.layer;
                var brt = (RectTransform)go.transform; brt.SetParent(rt, false);
                brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = brt.offsetMax = Vector2.zero;
                go.GetComponent<LayoutElement>().ignoreLayout = true;
                b = new Blink { image = go.GetComponent<Image>() };
                b.image.raycastTarget = false;
                blinkOf[rt] = b;
            }
            b.image.transform.SetAsLastSibling();
            b.t = 0; b.life = seconds; b.alpha = alpha * (Reduced ? .5f : 1);
            b.image.color = new Color(1, 1, 1, b.alpha);
            if (!blinks.Contains(b)) blinks.Add(b);
        }

        /// <summary>拖曳: a comet of sparks from one point to another (a hit flying into the combo counter).</summary>
        public void Trail(Vector2 from, Vector2 to, Color color, int count = 10, float seconds = .34f)
        {
            if (MotionDisabled) return;
            if (!Visible() || GamePerformance.Low) return;
            count = Mathf.Clamp(Reduced ? count / 2 : count, 1, 18);
            count = Mathf.Min(count, ParticleLimit - particles.Count);
            for (int i = 0; i < count; i++)
            {
                var p = imagePool.Count > 0 ? imagePool.Pop() : NewImage();
                p.travelling = true; p.from = from; p.to = to;
                p.pos = from; p.vel = Vector2.zero; p.gravity = 0; p.spin = 0;
                // Each spark is a little slower than the one before: together they draw a streak with a bright head.
                p.t = 0; p.life = seconds * (1 + i * .055f);
                p.color = Color.Lerp(Color.white, color, Mathf.Clamp01(i / 3f));
                float size = Mathf.Lerp(11, 3, i / (float)count);
                p.rt.sizeDelta = new Vector2(size, size);
                p.rt.gameObject.SetActive(true); p.rt.SetAsLastSibling();
                p.rt.anchoredPosition = from; p.rt.localRotation = Quaternion.identity; p.g.color = p.color;
                particles.Add(p);
            }
        }

        /// <summary>
        /// One right answer in a combo, at full feel: the answered card flashes white and is knocked back along
        /// <paramref name="dir"/>, the pressed button squashes and stretches, sparks and a shockwave go off where it hit, a
        /// short hit-stop and a screen shake land it, and a trail carries it into the counter. Everything grows with the
        /// combo up to the 10th hit (where the sound tops out too); the sound and the counter's own pop are the counter's.
        /// </summary>
        /// <param name="countLabel">False on the answer that reaches a new streak multiplier: its tier call-out takes the text slot.</param>
        public void ComboHit(Vector2 at, RectTransform struck, RectTransform pressed, RectTransform counter, int combo, Vector2 dir, Color color, bool countLabel = true)
        {
            if (combo < 1 || !Visible()) return;
            // Milestones replace the ordinary large hit, rather than stacking another celebration on it.
            if (combo == 20 || combo == 50)
            {
                StreakMilestone(at, combo);
                Knock(pressed, .08f, default, .18f, squash: true);
                return;
            }
            float k = ComboFeel(combo);
            if (dir.sqrMagnitude < .01f) dir = Vector2.up;
            dir.Normalize();
            // 闪白 击退 挤压拉伸
            FlashWhite(struck, .09f + .06f * k, .55f + .4f * k);
            Knock(struck, .05f + .06f * k, dir * (16 + 34 * k), .22f + .1f * k);
            Knock(pressed, .16f + .14f * k, default, .26f, squash: true);
            // 粒子 冲击波: big sparks and stars from the card, and a second, wider ring from the 5th hit
            Burst(at, 10 + Mathf.RoundToInt(22 * k), color, Shape.Spark, 300 + 420 * k, 380, .45f + .25f * k, 2.2f + 1.2f * k);
            Burst(at, 2 + Mathf.RoundToInt(6 * k), Color.Lerp(Color.white, color, .4f), Shape.Star, 220 + 200 * k, 300, .6f, 1 + .5f * k);
            Shockwave(at, new Color(color.r, color.g, color.b, .75f + .25f * k), 120 + 220 * k, .3f + .12f * k, 7 + 10 * k);
            if (combo >= 5) Shockwave(at, new Color(1, 1, 1, .5f), 200 + 260 * k, .45f, 4);
            // 飘字: the count itself, big, where the hit landed
            if (countLabel) Float(at + new Vector2(0, 70 + 20 * k), combo + T(" 连击", " hits") + (combo >= 10 ? "！" : ""), color, 30 + 26 * k, 90 + 50 * k, .7f + .2f * k, 1.7f + .7f * k);
            // 顿帧 震屏 闪屏
            if (combo >= 2) HitStop(30 + 60 * k);
            Shake(3 + 9 * k, .16f + .12f * k);
            if (combo >= 5) Flash(Color.white, .06f + .04f * k, .06f + .14f * k);
            // 拖曳 into the counter
            if (counter != null) Trail(at, At(counter), color, 8 + Mathf.RoundToInt(10 * k), .3f + .08f * (1 - k));
        }

        static string T(string zh, string en) => LingGuangV05.Runtime.GameText.T(zh, en);

        /// <summary>0 at the first hit, 1 from the 10th: how hard a combo hit lands.</summary>
        public static float ComboFeel(int combo) => Mathf.Clamp01((combo - 1) / (float)(Sfx.ComboSteps - 1));

        public void Play(Sfx.Id id, float pitch = 1, float volume = 1) { if (!Muted && Visible()) Sfx.Play(id, pitch, volume); }
        /// <summary>Plays a recorded clip (e.g. the casino's) through the same pool, muted and hidden the same way.</summary>
        public void PlayClip(AudioClip clip, float pitch = 1, float volume = 1) { if (!Muted && Visible()) Sfx.PlayClip(clip, pitch, volume); }
        /// <summary>Seconds between two hits that keep the combo sound climbing (2026-10-18: 「3 秒内点的都算连击」).</summary>
        public const float ComboSoundWindow = 3f;
        static float comboSoundAt = -100;
        static int comboSoundStep;

        /// <summary>
        /// The step the next combo sound plays: the Nth right answer in a row, each within <see cref="ComboSoundWindow"/>
        /// of the one before (a trick card's +2 or the 教 page's model streak never skip or silence a step); back to 1
        /// after a gap or a <see cref="BreakComboSound"/>. 0: the same hit reported twice in one moment (ignore it).
        /// </summary>
        public static int NextComboSoundStep(float now)
        {
            if (now - comboSoundAt < .05f) return 0;
            comboSoundStep = now - comboSoundAt <= ComboSoundWindow ? comboSoundStep + 1 : 1;
            comboSoundAt = now;
            return comboSoundStep;
        }

        /// <summary>A wrong answer or a broken combo: the next right answer starts the sounds from the first again.</summary>
        public static void BreakComboSound() { comboSoundAt = -100; comboSoundStep = 0; }

        /// <summary>
        /// Combo hit: the 10-step kill-streak set (Resources/XgSfx/Combo/Kill1–10, made with Tone.js in
        /// /Users/kk/unity/tonejs-sfx), low to high one step per hit (<see cref="NextComboSoundStep"/>) up to the big 10th,
        /// which then plays on every hit after it. Falls back to the old rising Ding when the clips are missing.
        /// </summary>
        public void PlayCombo(int combo, float volume = 1)
        {
            if (Muted || !Visible()) return;
            int step = NextComboSoundStep(Time.unscaledTime);
            if (step == 0) return;
            var clip = Sfx.ComboClip(step);
            if (clip != null) PlayClip(clip, 1, volume * Sfx.ComboGain);
            else Play(Sfx.Id.Ding, 1 + Mathf.Min(1, step * .025f), volume);
        }

        /// <summary>Discard presentation on a closed/rebuilt tab; do not replay old particles on reopen.</summary>
        public void Clear()
        {
            OnDisable(); hitstop = 0; flashDuration = shakeDuration = 0;
            if (flash != null) flash.color = Color.clear;
            foreach (var f in floaters) { f.text.gameObject.SetActive(false); textPool.Push(f.text); }
            foreach (var p in particles) { p.rt.gameObject.SetActive(false); (p.g is TMP_Text ? glyphPool : imagePool).Push(p); }
            foreach (var w in waves) { w.ring.gameObject.SetActive(false); ringPool.Push(w.ring); }
            foreach (var b in blinks) if (b.image != null) b.image.color = Color.clear;
            floaters.Clear(); particles.Clear(); waves.Clear(); blinks.Clear();
        }

        // ───────────── update ─────────────

        void Update()
        {
            if (MotionDisabled || !Visible()) { if (floaters.Count + particles.Count + waves.Count + blinks.Count + punches.Count > 0 || hitstopOwned || flashDuration > 0 || shakeDuration > 0) Clear(); return; }
            if (GamePerformance.Low && particles.Count > 0)
            {
                foreach (var p in particles) { p.rt.gameObject.SetActive(false); (p.g is TMP_Text ? glyphPool : imagePool).Push(p); }
                particles.Clear();
            }
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

            for (int i = blinks.Count - 1; i >= 0; i--)
            {
                var b = blinks[i];
                b.t += dt;
                float k = b.life > 0 ? b.t / b.life : 1;
                if (b.image == null || k >= 1) { if (b.image != null) b.image.color = Color.clear; blinks.RemoveAt(i); continue; }
                b.image.color = new Color(1, 1, 1, b.alpha * (1 - k) * (1 - k));
            }

            if (punches.Count > 0)
            {
                var done = completedPunches ?? (completedPunches = new List<RectTransform>());
                foreach (var p in punches.Values)
                {
                    if (p.rt == null) { done.Add(p.rt); continue; }
                    RebaseExternalTransform(p);
                    if (p.press && p.held)
                    {
                        p.rt.localScale = p.scale * .95f;
                        p.rt.anchoredPosition = p.pos;
                        RememberApplied(p);
                        continue;
                    }
                    p.t += dt;
                    float k = p.t / p.life;
                    if (k >= 1) { p.rt.localScale = p.scale; p.rt.anchoredPosition = p.pos; done.Add(p.rt); continue; }
                    if (p.press)
                    {
                        // One small overshoot, then exact rest at .22 seconds; no translation.
                        float residual = (1 - k) * (1 - k) * Mathf.Cos(k * Mathf.PI * 2);
                        p.rt.localScale = p.scale + (p.settleScale - p.scale) * residual;
                        p.rt.anchoredPosition = p.pos;
                        RememberApplied(p);
                        continue;
                    }
                    if (p.settling)
                    {
                        float ease = k * k * (3 - 2 * k);
                        p.rt.localScale = Vector3.LerpUnclamped(p.settleScale, p.scale, ease);
                        p.rt.anchoredPosition = Vector2.LerpUnclamped(p.settlePos, p.pos, ease);
                        RememberApplied(p);
                        continue;
                    }
                    // Pressed in first (−amp), then overshoot, settling with a damped wobble.
                    float wobble = Mathf.Sin(k * Mathf.PI * 2.5f) * (1 - k);
                    p.rt.localScale = p.squash ? Vector3.Scale(p.scale, new Vector3(1 + p.amp * wobble, 1 - p.amp * wobble, 1)) : p.scale * (1 - p.amp * wobble);
                    p.rt.anchoredPosition = p.pos + p.dir * wobble;
                    RememberApplied(p);
                }
                foreach (var rt in done) punches.Remove(rt);
                done.Clear(); // Retain capacity, not completed/destroyed target references.
            }
        }

        void OnDisable()
        {
            if (hitstopOwned) RestoreTimeScale();
            if (shakeTarget != null) shakeTarget.anchoredPosition = shakeBase;
            foreach (var p in punches.Values) if (p.rt != null) { RebaseExternalTransform(p); p.rt.localScale = p.scale; p.rt.anchoredPosition = p.pos; }
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

        /// <summary>
        /// Sound effects: the rendered set in Resources/XgSfx (felt piano, wood, glass, air, sub drops with reverb; made by
        /// Tools/XingGuang/sfx/make_sfx.py), falling back to the old procedural chiptune clip when a file is missing.
        /// Played through a small pool of sources.
        /// </summary>
        public static partial class Sfx
        {
            public enum Id { Click, Ding, Clack, Whoosh, Fan, Tick, Stamp, Fanfare, Buzz, Thud, Charge, Unlock, Coin, Break, Tier, Swoosh }
            public const int VariantsPerEvent = 3;
            const int Rate = 44100;
            static AudioClip[,] clips;
            static int[] lastVariant;
            static AudioSource[] sources;
            static int next;
            static readonly Dictionary<AudioClip, float> nextDataRetry = new Dictionary<AudioClip, float>();
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            static void ResetDataRetries() { nextDataRetry.Clear(); }

            // A cached AudioClip is metadata, not evidence that its PCM is ready. Imported short SFX
            // load in the background; do not busy-wait, postpone a callback or advance a source here.
            static void RequestAudioData(AudioClip clip)
            {
                if (clip == null) return;
                var state = clip.loadState;
                if (state == AudioDataLoadState.Loaded) { nextDataRetry.Remove(clip); return; }
                if (state == AudioDataLoadState.Loading) return;
                // Only called for our XgSfx resources. Never change an external PlayClip request.
                // Procedural fallback clips are already loaded; never force a synchronous load.
                if (!clip.loadInBackground) return;
                float now = Time.realtimeSinceStartup;
                if (nextDataRetry.TryGetValue(clip, out float after) && now < after) return;
                nextDataRetry[clip] = now + 2f;
                clip.LoadAudioData(); // Unity schedules the background load; Failed is retried on later use.
            }

            public static void Init(GameObject host, bool persistent = false)
            {
                EnsureClips();
                // Tabs come and go; retain the live pool rather than redirecting all app sounds into each new tab.
                if (sources != null && sources.Length > 0 && sources[0] != null && (!persistent || sources[0].gameObject == host)) return;
                if (sources != null) foreach (var old in sources) if (old != null) UnityEngine.Object.Destroy(old);
                // Enough sources that a reverb tail (fanfare ≈ 4 s) is not cut by the next few clicks.
                sources = new AudioSource[10];
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
                RequestAudioData(s.clip);
                s.Play();
            }

            /// <summary>A recorded clip through the shared pool (same volume scale as the synthesized ones).</summary>
            public static void PlayClip(AudioClip clip, float pitch = 1, float volume = 1)
            {
                if (sources == null || clip == null) return;
                var s = sources[next = (next + 1) % sources.Length];
                if (s == null) return;
                s.pitch = Mathf.Clamp(pitch, .3f, 3f);
                s.volume = .35f * volume;
                s.clip = clip;
                s.Play();
            }

            public const int ComboSteps = 10;
            /// <summary>The kill-streak clips are about 7 dB quieter than the Ding set after import normalisation.</summary>
            public const float ComboGain = 2.2f;
            static AudioClip[] comboClips;

            /// <summary>Clip for the given combo count (1 → step 1, 10 → step 10, 11 and on → step 10), or null if missing.</summary>
            public static AudioClip ComboClip(int combo)
            {
                if (comboClips == null)
                {
                    comboClips = new AudioClip[ComboSteps];
                    for (int i = 0; i < ComboSteps; i++)
                    {
                        comboClips[i] = Resources.Load<AudioClip>("XgSfx/Combo/Kill" + (i + 1));
                        RequestAudioData(comboClips[i]);
                    }
                }
                var clip = comboClips[Mathf.Clamp(combo, 1, ComboSteps) - 1];
                RequestAudioData(clip);
                return clip;
            }

            public static AudioClip Clip(Id id, int variant)
            {
                if (variant < 0 || variant >= VariantsPerEvent) throw new ArgumentOutOfRangeException(nameof(variant));
                EnsureClips();
                var clip = clips[(int)id, variant];
                RequestAudioData(clip);
                return clip;
            }

            static void EnsureClips()
            {
                if (clips != null) return;
                int count = Enum.GetValues(typeof(Id)).Length;
                clips = new AudioClip[count, VariantsPerEvent];
                lastVariant = new int[count];
                foreach (Id id in Enum.GetValues(typeof(Id)))
                    for (int variant = 0; variant < VariantsPerEvent; variant++)
                    {
                        var rendered = Resources.Load<AudioClip>("XgSfx/" + id + "_v" + variant);
                        clips[(int)id, variant] = rendered != null ? rendered : Build(id, variant);
                        RequestAudioData(clips[(int)id, variant]);
                    }
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
