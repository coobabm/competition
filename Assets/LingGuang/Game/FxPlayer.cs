using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LingGuang.Core;
using TMPro;
using UnityEngine;

namespace LingGuang.Game
{
    /// <summary>Plays a simulator event list beat by beat. Reads events only; never judges rules.</summary>
    public sealed class FxPlayer : MonoBehaviour
    {
        public BoardView board;
        public CameraRig rig;
        public SynthAudio synth;
        public Func<Vector3> lightBarWorld;
        public Func<Vector3> multBarWorld;
        public Action<SimEvent> onEvent;
        public bool reduceFlash;
        public bool fastForward;
        public bool skipRequested;
        public bool stepMode;
        public bool stepRequested;
        public bool playing;
        public FeelElectricFeedback Electric { get; private set; }
        public ChainFeelSettings feel = new ChainFeelSettings();

        Pool<LineRenderer> lightning;
        Pool<SpriteRenderer> rings;
        Pool<LineRenderer> fireRings;
        Pool<SpriteRenderer> dots;
        Pool<TextMeshPro> texts;
        TMP_FontAsset font;
        TextMeshPro banner;
        float bannerT;
        int lightningThisFrame;
        int frameStamp;
        float flashBudget; // photosensitivity: large flashes per second
        bool electricSkipCleared;
        int firesThisChain, ringsThisBeat;
        float slowUntil;
        const int RingSegs = 48;

        /// <summary>Playback speed of chain animation; drops below 1 during a milestone slow-mo.</summary>
        float Speed => !fastForward && feel != null && Time.unscaledTime < slowUntil ? feel.slowMoScale : 1f;

        sealed class Anim { public float t, life; public Action<float> step; public Action done; }
        readonly List<Anim> anims = new List<Anim>();

        public void Init(BoardView b, CameraRig r, SynthAudio s, TMP_FontAsset f)
        {
            board = b; rig = r; synth = s; font = f;
            Electric = GetComponent<FeelElectricFeedback>();
            if (Electric == null) Electric = gameObject.AddComponent<FeelElectricFeedback>();
            Electric.Initialize();
            lightning = new Pool<LineRenderer>(() => { var lr = Gfx.MakeLine("bolt", transform, Gfx.Additive, 30, 0.05f); lr.positionCount = 10; return lr; });
            rings = new Pool<SpriteRenderer>(() => Gfx.MakeSprite("ring", transform, Gfx.ThinRing, Gfx.Additive, 25, 1f));
            fireRings = new Pool<LineRenderer>(() => { var lr = Gfx.MakeLine("fire-ring", transform, Gfx.Additive, 26, 0.05f); lr.loop = true; lr.positionCount = RingSegs; return lr; });
            dots = new Pool<SpriteRenderer>(() => Gfx.MakeSprite("dot", transform, Gfx.Square, Gfx.Additive, 31, 0.07f));
            texts = new Pool<TextMeshPro>(() =>
            {
                var go = new GameObject("float");
                go.transform.SetParent(transform, false);
                var t = go.AddComponent<TextMeshPro>();
                if (font != null) t.font = font;
                t.fontSize = 3f;
                t.alignment = TextAlignmentOptions.Center;
                t.rectTransform.sizeDelta = new Vector2(3, 1);
                t.sortingOrder = 40;
                return t;
            });
            var bgo = new GameObject("banner");
            bgo.transform.SetParent(transform, false);
            banner = bgo.AddComponent<TextMeshPro>();
            if (font != null) banner.font = font;
            banner.fontSize = 9f;
            banner.alignment = TextAlignmentOptions.Center;
            banner.rectTransform.sizeDelta = new Vector2(12, 3);
            banner.sortingOrder = 50;
            banner.text = "";
        }

        void Update()
        {
            if (board != null) board.ReduceFlash = reduceFlash;
            if (board != null && board.WaveField != null) board.WaveField.ReduceFlash = reduceFlash;
            if (Electric != null)
            {
                Electric.SetMode(reduceFlash, fastForward);
                if (skipRequested && !electricSkipCleared) Electric.Clear();
                electricSkipCleared = skipRequested;
            }
            float dt = Time.deltaTime;
            float animDt = dt * Speed;
            for (int i = anims.Count - 1; i >= 0; i--)
            {
                var a = anims[i];
                a.t += animDt;
                float u = Mathf.Clamp01(a.t / a.life);
                a.step?.Invoke(u);
                if (u >= 1f) { anims.RemoveAt(i); a.done?.Invoke(); }
            }
            if (bannerT > 0f)
            {
                bannerT -= dt;
                var c = banner.color; c.a = Mathf.Clamp01(bannerT / 0.25f);
                banner.color = c;
                if (rig != null) banner.transform.position = rig.Center + new Vector3(0, rig.Size * 0.25f, 0);
            }
            else if (banner.text != "") banner.text = "";
            flashBudget = Mathf.Min(3f, flashBudget + dt * 3f);
        }

        void Animate(float life, Action<float> step, Action done = null) => anims.Add(new Anim { life = life, step = step, done = done });

        public float BeatDuration(int b) => Mathf.Max(0.06f, 0.2f * Mathf.Pow(0.85f, b));

        public void ShowBanner(string text, Color color, float seconds = 0.4f)
        {
            banner.text = text;
            banner.color = color;
            bannerT = seconds;
            if (rig != null) banner.transform.position = rig.Center + new Vector3(0, rig.Size * 0.25f, 0);
        }

        // ------------------------------------------------------------------ playback

        public IEnumerator Play(List<SimEvent> events)
        {
            playing = true;
            skipRequested = false;
            firesThisChain = 0;
            slowUntil = 0f;
            var byBeat = new SortedDictionary<int, List<SimEvent>>();
            foreach (var e in events)
            {
                if (!byBeat.TryGetValue(e.beat, out var l)) byBeat[e.beat] = l = new List<SimEvent>();
                l.Add(e);
            }
            foreach (var kv in byBeat)
            {
                int beat = kv.Key;
                int firesThisBeat = kv.Value.Count(e => e.type == SimEventType.Fire);
                int notes = 0;
                bool pause = false;
                ringsThisBeat = 0;
                foreach (var e in kv.Value)
                {
                    onEvent?.Invoke(e);
                    if (skipRequested) continue;
                    switch (e.type)
                    {
                        case SimEventType.Lighthouse: PlayLighthouse(e); break;
                        case SimEventType.Fire:
                            PlayFire(e, firesThisBeat);
                            if (notes++ < 4 && synth != null) synth.PlayNote(beat, ShapeAt(e.cell), e.fireIndex >= 2);
                            break;
                        case SimEventType.Deliver: PlayDeliver(e); break;
                        case SimEventType.Conduct: board.EdgeAt(e.cell, e.cellB)?.Stabilize(); break;
                        case SimEventType.EdgeLevelUp: board.EdgeAt(e.cell, e.cellB)?.Stabilize(); break;
                        case SimEventType.Lost: PlayLost(e); break;
                        case SimEventType.RippleMerge: PlayRipple(e); break;
                        case SimEventType.RippleApply:
                        case SimEventType.WeakLinkDeliver: board.ViewAt(e.cellB)?.Flash(0.15f); break;
                        case SimEventType.PickupCollected:
                            FloatText(board.CellPos(e.cell), EffectDef.Get(e.text)?.name ?? "拾取", new Color(1f, 0.85f, 0.35f), 1.0f);
                            synth?.PlayChime();
                            break;
                        case SimEventType.PatternRecognized:
                            Electric?.Pattern();
                            ShowBanner(e.text + (e.amount > 0 ? $"  ×{e.mult:0.##}" : ""), new Color(1f, 0.95f, 0.75f, 1f), 0.4f);
                            if (!reduceFlash) rig?.Shake(0.06f, 0.15f);
                            synth?.PlayPattern();
                            pause = true;
                            break;
                        case SimEventType.InfiniteLoop:
                            ShowBanner($"无限回荡 ∞\n<size=60%>每 {e.amount} 拍一圈 · 记 {e.fireIndex} 圈  +{e.light} 光量{(e.mult > 0 ? $"  +{e.mult:0.#} 倍" : "")}</size>", new Color(1f, 0.85f, 1f, 1f), 1.6f);
                            if (!reduceFlash) rig?.Shake(0.12f, 0.35f);
                            synth?.PlayPattern();
                            synth?.PlayChime();
                            pause = true;
                            break;
                        case SimEventType.Error:
                            ShowBanner(e.text, new Color(1f, 0.5f, 0.4f), 1.2f);
                            break;
                    }
                }
                if (skipRequested) continue;
                board.Sync();
                float d = BeatDuration(beat) / (fastForward ? 4f : 1f);
                if (pause) d += 0.1f;
                if (firesThisBeat >= 1 && kv.Value.Any(e => e.type == SimEventType.Fire && e.amount >= 3)) d += 0.1f; // converge burst
                if (stepMode)
                {
                    stepRequested = false;
                    while (!stepRequested && !skipRequested && stepMode) yield return null;
                }
                else
                {
                    float t = 0f;
                    while (t < d && !skipRequested) { t += Time.deltaTime * Speed; yield return null; }
                }
            }
            // let the last effects breathe
            float tail = skipRequested ? 0f : 0.35f;
            while (tail > 0f && !skipRequested) { tail -= Time.deltaTime * Speed * (fastForward ? 4f : 1f); yield return null; }
            playing = false;
        }

        Shape ShapeAt(int cell)
        {
            var sp = board.state.SparkAt(cell);
            return sp?.shape ?? Shape.Conduct;
        }

        void PlayLighthouse(SimEvent e)
        {
            var from = board.LighthousePos;
            foreach (int c in e.cells)
            {
                if (board.state.SparkAt(c) == null) continue;
                Bolt(from, board.CellPos(c), new Color(0.45f, 0.85f, 1f), 0.045f, 0.6f);
            }
        }

        void PlayFire(SimEvent e, int firesThisBeat)
        {
            var v = board.ViewAt(e.cell);
            // photosensitivity: big beats merge into softer flashes
            float scale = firesThisBeat > 6 ? 6f / firesThisBeat : 1f;
            if (reduceFlash) scale *= 0.45f;
            if (firesThisBeat > 6) { if (flashBudget < 1f) scale *= 0.5f; else flashBudget -= 1f; }
            v?.Flash(Mathf.Clamp(scale, 0.25f, 1f));
            var pos = board.CellPos(e.cell);
            var col = SparkView.ShapeColor(board.state.SparkAt(e.cell));
            ChainJuice(pos, col, Mathf.Clamp(scale, 0.25f, 1f), e.isStart);
            board.WaveField?.Emit(pos, e.isStart ? 1f : 0.6f, Time.time);
            if (Electric != null)
            {
                Electric.SetMode(reduceFlash, fastForward);
                Electric.Fire(pos, col, Mathf.Clamp01(scale));
            }
            // shards
            int shards = Electric != null ? 0 : reduceFlash ? 1 : firesThisBeat > 8 ? 2 : 4;
            for (int i = 0; i < shards; i++)
            {
                var d = dots.Get();
                float ang = UnityEngine.Random.value * Mathf.PI * 2f;
                var vel = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang)) * UnityEngine.Random.Range(0.6f, 1.6f);
                d.transform.position = pos;
                Animate(0.45f, u =>
                {
                    d.transform.position = pos + vel * (u * 0.55f) * (1f - u * 0.4f);
                    d.transform.localScale = Vector3.one * (u < 0.6f ? 0.055f : 0.03f);
                    Gfx.SetColor(d, Gfx.HDR(Color.Lerp(Color.white, col, u), 1.8f * (1f - u) * scale));
                }, () => dots.Release(d));
            }
            if (e.light > 0) FlyTo(pos, "+" + e.light, Color.Lerp(Color.white, col, 0.4f), e.isStart ? 3.4f : 2.8f, lightBarWorld);
            if (e.mult > 0) FlyTo(pos + Vector3.up * 0.2f, $"+{e.mult:0.#}倍", new Color(0.7f, 1f, 0.8f), 2.6f, multBarWorld);
            if (e.fireIndex >= 2) FloatText(pos + new Vector3(0.25f, 0.25f), "回荡", new Color(0.8f, 0.8f, 1f), 0.6f);
        }

        // Ripple-style feel: ring per fire, accumulating camera trauma, slow-mo at chain milestones.
        void ChainJuice(Vector3 pos, Color col, float strength, bool isStart)
        {
            if (feel == null) return;
            firesThisChain++;
            if (!reduceFlash) rig?.AddTrauma(feel.shakePerFire, feel.maxShake, feel.shakeRecover);
            if (!fastForward && feel.IsMilestone(firesThisChain)) slowUntil = Time.unscaledTime + feel.slowMoDuration;
            if (feel.fireRings && ringsThisBeat++ < feel.maxRingsPerBeat)
                FireRing(pos, col, strength, isStart ? feel.startRingScale : 1f);
        }

        void FireRing(Vector3 center, Color col, float strength, float radiusScale)
        {
            var lr = fireRings.Get();
            float maxR = feel.ringRadiusCells * radiusScale * BoardView.HexSize * BoardView.Sqrt3;
            float life = Mathf.Max(0.05f, feel.ringLife) / (fastForward ? 2f : 1f);
            Action<float> step = u =>
            {
                float r = Mathf.Max(0.001f, maxR * (1f - (1f - u) * (1f - u)));
                for (int i = 0; i < RingSegs; i++)
                {
                    float a = i / (float)RingSegs * Mathf.PI * 2f;
                    lr.SetPosition(i, center + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
                lr.widthMultiplier = Mathf.Lerp(feel.ringWidthStart, feel.ringWidthEnd, u);
                Gfx.SetLineColor(lr, Gfx.HDR(col, feel.ringIntensity * strength * (1f - u * u)));
            };
            step(0f); // pooled objects must not show the preceding ring for one frame
            Animate(life, step, () => fireRings.Release(lr));
        }

        void FlyTo(Vector3 from, string text, Color c0, float size, Func<Vector3> dest)
        {
            var t = texts.Get();
            t.text = text;
            t.fontSize = size;
            t.transform.position = from;
            var mid = from + new Vector3(0, 0.6f, 0);
            Animate(0.7f, u =>
            {
                Vector3 d = dest != null ? dest() : from + Vector3.up * 2f;
                Vector3 p = u < 0.35f ? Vector3.Lerp(from, mid, u / 0.35f) : Vector3.Lerp(mid, d, Mathf.SmoothStep(0, 1, (u - 0.35f) / 0.65f));
                t.transform.position = p;
                var c = c0;
                c.a = u < 0.85f ? 1f : (1f - u) / 0.15f;
                t.color = c;
            }, () => texts.Release(t));
        }

        public void FloatText(Vector3 pos, string text, Color col, float life)
        {
            var t = texts.Get();
            t.text = text;
            t.fontSize = 2.4f;
            t.transform.position = pos;
            Animate(life, u =>
            {
                t.transform.position = pos + Vector3.up * u * 0.5f;
                var c = col; c.a = 1f - u * u;
                t.color = c;
            }, () => texts.Release(t));
        }

        void PlayDeliver(SimEvent e)
        {
            var target = board.ViewAt(e.cellB);
            if (e.viaRipple)
            {
                // lighthouse charge
                target?.Flash(0.2f);
                return;
            }
            if (e.cell < 0) return;
            var a = board.CellPos(e.cell); var b = board.CellPos(e.cellB);
            Color c = e.level == 0 ? new Color(0.4f, 0.85f, 1f) : new Color(0.7f, 0.96f, 1f);
            var edge = board.EdgeAt(e.cell, e.cellB);
            float travel = Mathf.Min(0.11f, BeatDuration(e.beat) * 0.65f) / (fastForward ? 4f : 1f);
            Transmit(a, b, c, 0.045f + e.level * 0.016f, 1f, travel, edge, target);
        }

        void PlayLost(SimEvent e)
        {
            if (e.cell < 0 || e.cellB < 0) return;
            var a = board.CellPos(e.cell); var b = board.CellPos(e.cellB);
            Bolt(a, Vector3.Lerp(a, b, 0.7f), new Color(0.5f, 0.6f, 0.8f), 0.025f, 0.35f);
        }

        void PlayRipple(SimEvent e)
        {
            var centers = (e.text ?? "").Split(',').Where(x => x.Length > 0).Select(int.Parse).ToList();
            float worldR = e.radius * BoardView.HexSize * BoardView.Sqrt3 + BoardView.HexSize * 0.5f;
            foreach (int c in centers)
            {
                var r = rings.Get();
                var p = board.CellPos(c);
                r.transform.position = p;
                float inten = reduceFlash ? 0.06f : 0.18f;
                Animate(0.55f, u =>
                {
                    float k = 1f - Mathf.Pow(1f - u, 2.2f);
                    r.transform.localScale = Vector3.one * worldR * 2f * k;
                    Gfx.SetColor(r, Gfx.HDR(new Color(0.65f, 0.85f, 1f), inten * (1f - u)));
                }, () => rings.Release(r));
            }
        }

        public void Bolt(Vector3 a, Vector3 b, Color c, float width, float intensity)
        {
            Transmit(a, b, c, width, intensity, fastForward ? 0.025f : 0.10f, null, null);
        }

        void Transmit(Vector3 a, Vector3 b, Color c, float width, float intensity,
            float travel, EdgeView edge, SparkView target)
        {
            if (Electric != null)
            {
                Electric.SetMode(reduceFlash, fastForward);
                Electric.Transmit(a, b, c, width, intensity, travel, edge, target);
                return;
            }
            if (Time.frameCount != frameStamp) { frameStamp = Time.frameCount; lightningThisFrame = 0; }
            if (++lightningThisFrame > 48) return; // cap per frame
            var lr = lightning.Get();
            var head = dots.Get();
            const int segs = 12;
            lr.positionCount = segs + 1;
            lr.widthMultiplier = width;
            head.transform.localScale = Vector3.one * Mathf.Max(0.055f, width * 1.4f);
            float peak = (reduceFlash ? 0.85f : 2.1f) * intensity;
            float life = travel + (fastForward ? 0.06f : 0.18f);
            bool arrived = false;
            Action<float> step = u =>
            {
                float elapsed = u * life;
                float front = Mathf.Clamp01(elapsed / travel);
                float tail = Mathf.Max(0f, front - 0.55f);
                for (int i = 0; i <= segs; i++)
                {
                    float p = Mathf.Lerp(tail, front, i / (float)segs);
                    lr.SetPosition(i, edge != null ? edge.SamplePath(p) : Vector3.Lerp(a, b, p));
                }
                head.transform.position = edge != null ? edge.SamplePath(front) : Vector3.Lerp(a, b, front);
                float fade = elapsed <= travel ? 1f : Mathf.Clamp01((life - elapsed) / (life - travel));
                Gfx.SetLineColor(lr, Gfx.HDR(c, peak * fade));
                Gfx.SetColor(head, Gfx.HDR(new Color(0.78f, 1f, 1f), peak * fade));
                if (!arrived && front >= 1f)
                {
                    arrived = true;
                    if (target != null) target.Flash(reduceFlash ? 0.06f : 0.18f);
                }
            };
            step(0f); // pooled objects must not show the preceding pulse for one frame
            Animate(life, step, () => { lightning.Release(lr); dots.Release(head); });
        }

        public void ClearAll()
        {
            Electric?.Clear();
            board?.WaveField?.Clear();
            anims.Clear();
            lightning?.ReleaseAll(); rings?.ReleaseAll(); fireRings?.ReleaseAll(); dots?.ReleaseAll(); texts?.ReleaseAll();
            firesThisChain = 0;
            slowUntil = 0f;
        }
    }

    // =====================================================================

    public sealed class CameraRig : MonoBehaviour
    {
        public Camera cam;
        public float minSize = 3.2f, maxSize = 8.5f;
        Vector3 targetPos;
        float targetSize;
        Vector3 shakeOffset;
        float shakeAmp, shakeT;
        float trauma, traumaRecover = 0.7f;
        public Bounds limits;

        public Vector3 Center => new Vector3(targetPos.x, targetPos.y, 0);
        public float Size => cam.orthographicSize;

        public void Init(Camera c)
        {
            cam = c;
            targetPos = cam.transform.position;
            targetSize = cam.orthographicSize;
        }

        public void Fit(Bounds b, bool instant)
        {
            float aspect = cam.aspect;
            float size = Mathf.Max(b.extents.y * 1.35f, b.extents.x / aspect * 1.45f) + 0.6f;
            targetSize = Mathf.Clamp(size, minSize, maxSize);
            targetPos = new Vector3(b.center.x, b.center.y + targetSize * 0.04f, -10f);
            if (instant) { cam.orthographicSize = targetSize; cam.transform.position = targetPos; }
        }

        public void Zoom(float delta, Vector3 aroundWorld)
        {
            float old = targetSize;
            targetSize = Mathf.Clamp(targetSize * (1f - delta), minSize, maxSize);
            float k = 1f - targetSize / old;
            targetPos += (aroundWorld - new Vector3(targetPos.x, targetPos.y, 0)) * k;
            targetPos.z = -10f;
            Clamp();
        }

        public void Pan(Vector3 worldDelta)
        {
            targetPos -= worldDelta;
            targetPos.z = -10f;
            cam.transform.position = targetPos + shakeOffset;
            Clamp();
        }

        void Clamp()
        {
            if (limits.size == Vector3.zero) return;
            targetPos.x = Mathf.Clamp(targetPos.x, limits.min.x, limits.max.x);
            targetPos.y = Mathf.Clamp(targetPos.y, limits.min.y, limits.max.y);
        }

        public void Shake(float amp, float time) { shakeAmp = Mathf.Max(shakeAmp, amp); shakeT = Mathf.Max(shakeT, time); }

        /// <summary>Accumulating, smoothly recovering shake (one small kick per fire).</summary>
        public void AddTrauma(float amount, float max, float recover)
        {
            trauma = Mathf.Min(max, trauma + amount);
            traumaRecover = Mathf.Max(0.01f, recover);
        }

        void LateUpdate()
        {
            if (cam == null) return;
            float k = 1f - Mathf.Exp(-Time.deltaTime * 6f);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, k);
            var offset = Vector3.zero;
            if (shakeT > 0f)
            {
                shakeT -= Time.deltaTime;
                if (shakeT > 0f) offset += (Vector3)UnityEngine.Random.insideUnitCircle * shakeAmp;
                else shakeAmp = 0f;
            }
            if (trauma > 0f)
            {
                trauma = Mathf.MoveTowards(trauma, 0f, traumaRecover * Time.deltaTime);
                float n = Time.time * 25f;
                offset += new Vector3(Mathf.PerlinNoise(n, 0f) - 0.5f, Mathf.PerlinNoise(0f, n) - 0.5f, 0f) * 2f * trauma;
            }
            var p = Vector3.Lerp(cam.transform.position - shakeOffset, targetPos, k);
            shakeOffset = offset;
            cam.transform.position = p + shakeOffset;
        }
    }
}
