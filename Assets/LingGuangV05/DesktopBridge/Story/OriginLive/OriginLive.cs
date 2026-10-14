using System.Collections.Generic;
using System.Text;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>Baked textures for the film's backdrop, vignette and film grain (made once, shared by every player).</summary>
    public static class OriginTextures
    {
        static Texture2D back, vignette, grain;

        static Texture2D Make(string name, int w, int h, TextureWrapMode wrap, FilterMode filter)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false, false) { name = name, wrapMode = wrap, filterMode = filter, hideFlags = HideFlags.HideAndDontSave };
            return t;
        }

        /// <summary>radial-gradient(120% 95% at 50% 42%, #0f1512 0%, #070a09 55%, #020303 100%) over 1920×1080, baked at quarter size.</summary>
        public static Texture2D Backdrop()
        {
            if (back != null) return back;
            const int w = 480, h = 270;
            back = Make("OriginBackdrop", w, h, TextureWrapMode.Clamp, FilterMode.Bilinear);
            var px = new Color32[w * h];
            Color a = new Color32(15, 21, 18, 255), b = new Color32(7, 10, 9, 255), c = new Color32(2, 3, 3, 255);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double sx = (x + .5) / w * 1920, sy = (1 - (y + .5) / h) * 1080;
                    double dx = (sx - 960) / 2304.0, dy = (sy - 453.6) / 1026.0;
                    double k = System.Math.Sqrt(dx * dx + dy * dy);
                    Color col = k < .55 ? Color.Lerp(a, b, (float)(k / .55)) : Color.Lerp(b, c, (float)System.Math.Min(1, (k - .55) / .45));
                    px[y * w + x] = col;
                }
            back.SetPixels32(px); back.Apply(false, true);
            return back;
        }

        /// <summary>radial-gradient(ellipse at 50% 48%, transparent 52%, rgba(0,0,0,.72) 100%): black with a rising alpha.</summary>
        public static Texture2D Vignette()
        {
            if (vignette != null) return vignette;
            const int w = 480, h = 270;
            vignette = Make("OriginVignette", w, h, TextureWrapMode.Clamp, FilterMode.Bilinear);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double sx = (x + .5) / w * 1920, sy = (1 - (y + .5) / h) * 1080;
                    double dx = (sx - 960) / 1357.6, dy = (sy - 518.4) / 794.2;
                    double k = System.Math.Sqrt(dx * dx + dy * dy);
                    double alpha = .72 * System.Math.Max(0, System.Math.Min(1, (k - .52) / .48));
                    px[y * w + x] = new Color32(0, 0, 0, (byte)System.Math.Round(OriginInk.Veil((float)alpha) * 255));
                }
            vignette.SetPixels32(px); vignette.Apply(false, true);
            return vignette;
        }

        /// <summary>
        /// The film's grain (an SVG turbulence at 5.5 % opacity, new seed every 12 frames): white-ish noise, baked as a tiny constant alpha
        /// with the grey carrying the noise. It also dithers the dark gradient, which would otherwise band in 8 bits.
        /// </summary>
        public static Texture2D Grain()
        {
            if (grain != null) return grain;
            const int n = 512;
            grain = Make("OriginGrain", n, n, TextureWrapMode.Repeat, FilterMode.Point);
            var px = new Color32[n * n];
            var rnd = new System.Random(20161011);
            const float alpha = 9f / 255;
            for (int i = 0; i < px.Length; i++)
            {
                // SVG turbulence is computed in linearRGB, so its mid grey reaches the screen as ~.73 in sRGB.
                float g = Mathf.Clamp01(.735f + ((float)rnd.NextDouble() - .5f) * .6f);
                var c = OriginInk.Over(new Color(g, g, g), alpha);
                px[i] = new Color(c.r, c.g, c.b, alpha);
            }
            grain.SetPixels32(px); grain.Apply(false, true);
            return grain;
        }
    }

    /// <summary>
    /// The origin film (161 s) rendered live: a full-screen player drawn with uGUI, hosting the film's scenes (<see cref="OriginLiveScenes"/>).
    /// The host owns the layers that run over every scene (backdrop, parameter HUD, captions, vignette and grain) and routes the film time to the
    /// scene that is active, with the fade and drift of the Remotion Main.tsx. Everything on screen is a pure function of the time passed to
    /// <see cref="Render"/>, so any frame can be drawn without Play mode, and scrubbing is just calling Render with another t.
    /// While playing, t comes from the audio clock so the picture stays on the music. The layout is the Remotion project's 1920×1080
    /// reference space, fitted into the parent rect with black bars.
    /// </summary>
    public sealed class OriginLive : MonoBehaviour
    {
        public const float Duration = (float)XgOriginFilm.Duration;
        const string AudioPath = "LingGuangV05/Video/origin_live_soundtrack";

        // CSS line-height: normal of the film's faces, as a multiple of the font size (measured from the reference render).
        public const float LhSerif = 1.42f, LhMono = 1.17f;
        /// <summary>Per-font vertical nudges (reference px, + is down) between TMP's and Chrome's glyph placement.</summary>
        public static float SerifDy = 0, MonoDy = 0;

        /// <summary>A text glow as the SDF material sees it: colour alpha, dilation, softness.</summary>
        public struct Glow { public float Alpha, Dilate, Soft; public Glow(float a, float d, float s) { Alpha = a; Dilate = d; Soft = s; } }
        /// <summary>The stand-ins for the film's CSS text-shadows (tuned against the Remotion stills).</summary>
        public static Glow CaptionShadow = new Glow(.7f, 0f, .8f), CaptionLime = new Glow(.5f, 0f, .45f), SaySoft = new Glow(.07f, 0f, .8f), SayLime = new Glow(.5f, 0f, .45f), HudValue = new Glow(.3f, 0f, .35f);
        /// <summary>Extra stroke weight for the serif text (Chrome on macOS renders Songti heavier than the raw outline).</summary>
        public static float SerifWeight = .03f, MonoWeight = .03f;

        RectTransform root, stage, scenesRoot;
        RawImage back, vignette, grain;
        OriginGridGraphic grid;
        OriginText caption;
        OriginParamHud hud;
        readonly OriginLiveContext ctx = new OriginLiveContext();
        readonly List<IOriginScene> scenes = new List<IOriginScene>();
        readonly List<CanvasGroup> groups = new List<CanvasGroup>();
        readonly List<RectTransform> containers = new List<RectTransform>();
        float pixel = 1;

        // playback
        AudioSource source;
        double startDsp, startClock;
        bool renderFailed;
        bool playing;
        public bool Ended { get; private set; }
        /// <summary>The film time of the last Render.</summary>
        public float Clock { get; private set; }
        /// <summary>The scene index (into <see cref="OriginLiveScenes.Table"/>) drawn by the last Render.</summary>
        public int ActiveScene { get; private set; } = -1;
        /// <summary>Scenes that failed to build (logged); the film plays without them.</summary>
        public int FailedScenes { get; private set; }

        // ───────────── build ─────────────

        /// <summary>Builds the player as a full-stretch child of parent. It draws nothing until <see cref="Render"/> or <see cref="Begin"/>.</summary>
        public static OriginLive Create(RectTransform parent)
        {
            var go = new GameObject("Origin Live", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var live = go.AddComponent<OriginLive>();
            live.Build(rt);
            return live;
        }

        void Build(RectTransform rootRect)
        {
            root = rootRect;
            var black = root.gameObject.AddComponent<Image>(); black.color = Color.black; black.raycastTarget = true;

            ctx.Serif = OriginFonts.Serif; ctx.Mono = OriginFonts.Mono;
            stage = ctx.Rect("Stage", root, false);
            stage.anchorMin = stage.anchorMax = new Vector2(.5f, .5f); stage.pivot = new Vector2(.5f, .5f); stage.sizeDelta = new Vector2(1920, 1080);
            stage.gameObject.AddComponent<RectMask2D>();
            ctx.Stage = stage;

            back = ctx.Raw("Backdrop", stage, OriginTextures.Backdrop());
            grid = ctx.Gfx<OriginGridGraphic>("Grid", stage);

            scenesRoot = ctx.Rect("Scenes", stage);
            var table = OriginLiveScenes.Table;
            for (int i = 0; i < table.Length; i++)
            {
                var container = ctx.Rect(XgOriginFilm.SceneNames[i], scenesRoot);
                var group = container.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
                IOriginScene scene = null;
                try
                {
                    scene = table[i].Make();
                    if (scene is OriginScene os) os.Place(XgOriginFilm.Spans[i]);
                    scene.Build(ctx, container);
                }
                catch (System.Exception e)
                {
                    FailedScenes++;
                    Debug.LogError("OriginLive: scene " + XgOriginFilm.SceneNames[i] + " failed to build: " + e);
                    scene = null;
                }
                scenes.Add(scene); groups.Add(group); containers.Add(container);
                container.gameObject.SetActive(false);
            }

            hud = new OriginParamHud(ctx, stage);
            caption = new OriginText("Caption", stage, ctx.Serif, 46, OriginInk.Ink, 3, TextAlignmentOptions.Center,
                new OriginTextLayer { Rgb = Color.black, Alpha = .9f, Dilate = .3f, Softness = 1, DeltaY = 2 },
                new OriginTextLayer { Rgb = OriginInk.Lime, Alpha = .6f, Dilate = .25f, Softness = 1, HighlightsOnly = true });

            vignette = ctx.Raw("Vignette", stage, OriginTextures.Vignette());
            grain = ctx.Raw("Grain", stage, OriginTextures.Grain());

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0; source.loop = false;
            Warm();
            Render(0);
        }

        /// <summary>Fills the dynamic font atlases with every character the film shows, so no glyph is rasterised mid-film.</summary>
        void Warm()
        {
            var sb = new StringBuilder("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz .,:;/·—-+()[]%#&→←↑↓✓✗①②③④⑤⑥ΣσΔ…「」“”‘’？！，。、：；（）－—×≈≤≥∞▲");
            foreach (var c in XgOriginFilm.Captions) { sb.Append(c.Zh).Append(c.En).Append(c.ZhPersonal).Append(c.EnPersonal); }
            foreach (var s in scenes) if (s is OriginScene os) foreach (var line in os.WarmText()) sb.Append(line);
            string all = sb.ToString();
            OriginFonts.Warm(all);
        }

        /// <summary>The player's own numbers (the one personal touch): the last parameter count and its captions. Null keeps the film's.</summary>
        public void Bind(XgSim sim)
        {
            ctx.Sim = sim;
            ctx.FinalParams = XgOriginColdOpen.FinalParams(sim);
        }

        // ───────────── render ─────────────

        void FitStage()
        {
            var r = root.rect;
            float s = Mathf.Min(r.width / 1920f, r.height / 1080f);
            if (s <= 0.0001f) s = 1;
            stage.localScale = new Vector3(s, s, 1);
            float px = 1 / s;
            if (!Mathf.Approximately(px, pixel))
            {
                pixel = px; ctx.Pixel = px;
                foreach (var g in ctx.Graphics) if (g != null) { g.Pixel = px; g.SetVerticesDirty(); }
                grid.Pixel = px; grid.SetVerticesDirty();
            }
        }

        /// <summary>Draws the film at t seconds. Pure: the same t always gives the same picture.</summary>
        public void Render(float t)
        {
            t = Mathf.Clamp(t, 0, Duration);
            Clock = t;
            FitStage();

            int active = XgOriginFilm.SceneAt(t);
            ActiveScene = active;
            for (int i = 0; i < scenes.Count; i++)
            {
                bool on = i == active && scenes[i] != null;
                if (containers[i].gameObject.activeSelf != on) containers[i].gameObject.SetActive(on);
                if (!on) continue;
                var span = XgOriginFilm.Spans[i];
                groups[i].alpha = OriginInk.Fade((float)span.Alpha(t));
                float k = (float)span.Scale(t);
                containers[i].localScale = new Vector3(k, k, 1);
                scenes[i].SetVisible(true);
                try { scenes[i].Render(t); }
                catch (System.Exception e) { Debug.LogError("OriginLive: scene " + XgOriginFilm.SceneNames[i] + " at " + t.ToString("0.00") + " s: " + e); }
            }
            hud.Render(t, ctx.FinalParams);
            RenderCaption(t);

            int g = Mathf.FloorToInt(t * 30 + .001f) % 12;
            grain.uvRect = new Rect((float)XgOriginColdOpen.Rand("gx" + g), (float)XgOriginColdOpen.Rand("gy" + g), 1920f / 512, 1080f / 512);
        }

        void RenderCaption(float t)
        {
            var f = XgOriginFilm.CaptionAt(t);
            if (f.Index < 0 || f.Alpha <= .001) { caption.Alpha(0); return; }
            string text = XgOriginFilm.CaptionText(f.Index, GameText.IsEnglish, ctx.FinalParams);
            caption.Alpha(OriginInk.Fade((float)f.Alpha));
            caption.SetFace(SerifWeight, 0);
            caption.Set(text);
            caption.SetHaloStrength(0, CaptionShadow.Alpha, CaptionShadow.Dilate, CaptionShadow.Soft);
            caption.SetHaloStrength(1, CaptionLime.Alpha, CaptionLime.Dilate, CaptionLime.Soft);
            float lh = LhSerif * 46;
            // English lines are longer than the Chinese ones: shrink a line that would leave the screen (Chinese lines never do).
            float fit = Mathf.Min(1, 1780f / Mathf.Max(1, caption.Width(text)));
            caption.Root.localScale = new Vector3(fit, fit, 1);
            caption.Place(960, 1080 - 112 - lh * .5f + (float)f.Lift + SerifDy, .5f, 1700, lh);
            caption.Alpha(OriginInk.Fade((float)f.Alpha));
        }

        // ───────────── playback ─────────────

        /// <summary>Starts the music and the clock. t follows the audio (DSP) clock, so the picture cannot drift from the beat.</summary>
        public void Begin()
        {
            Ended = false; playing = true;
            DuckGameAudio();
            var clip = Resources.Load<AudioClip>(AudioPath);
            startDsp = AudioSettings.dspTime + .12;
            startClock = Time.realtimeSinceStartupAsDouble + .12;
            if (clip != null) { source.clip = clip; source.PlayScheduled(startDsp); }
            else Debug.LogWarning("OriginLive: missing audio " + AudioPath);
            Render(0);
        }

        void Update()
        {
            if (!playing) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { Finish(); return; }
            // The picture follows the music's own play position. AudioSettings.dspTime is not used: in the editor it can
            // stall while the music keeps playing, which left the film on its black first frame.
            float t = source != null && source.clip != null && source.isPlaying && source.timeSamples > 0
                ? (float)source.timeSamples / source.clip.frequency
                : (float)(Time.realtimeSinceStartupAsDouble - startClock);
            try
            {
                if (t >= Duration) { Render(Duration); Finish(); return; }
                Render(Mathf.Max(0, t));
            }
            catch (System.Exception e)
            {
                // One log, not one per frame: a frame-by-frame flood of exceptions stalls the editor and the film with it.
                if (!renderFailed) Debug.LogError("OriginLive: frame at " + t.ToString("0.00") + " s failed: " + e);
                renderFailed = true;
            }
        }

        void Finish()
        {
            playing = false; Ended = true;
            if (source != null) source.Stop();
            RestoreGameAudio();
        }

        // The game's music and sounds are paused for the film (AudioListener.pause); the film's own source ignores the pause.
        bool ducked, pauseBefore;

        void DuckGameAudio()
        {
            if (ducked) return;
            ducked = true; pauseBefore = AudioListener.pause;
            source.ignoreListenerPause = true;
            AudioListener.pause = true;
        }

        void RestoreGameAudio()
        {
            if (!ducked) return;
            ducked = false;
            AudioListener.pause = pauseBefore;
        }

        /// <summary>Stops the music and removes the player.</summary>
        public void Close()
        {
            playing = false;
            if (source != null) source.Stop();
            RestoreGameAudio();
            if (root != null) Destroy(root.gameObject);
        }

        void OnDestroy() { RestoreGameAudio(); }
    }
}
