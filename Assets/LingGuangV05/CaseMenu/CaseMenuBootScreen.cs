using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.CaseMenu
{
    /// <summary>
    /// Fictional OS boot presentation confined to the native LCD, not a loading-screen camera.
    /// 2016 Aurora 7 firmware look (name from EraSafeBranding): BIOS self-test log, a "primordial ring" emblem with a teal spark
    /// (the program that is about to arrive), segmented progress, four pixel stage icons and the
    /// desktop's dolphin. Every animated detail is driven by boot progress, never by time, so the
    /// sequence is deterministic for captures and identical with reduced motion.
    /// </summary>
    public sealed class CaseMenuBootScreen : MonoBehaviour
    {
        // `cover` and `content` are read by ShutdownReturnSmoke through reflection; keep the names.
        RectTransform root, progressFill, spark, dolphin;
        CanvasGroup cover, content;
        Image background, sparkImage, easterDot;
        TextMeshProUGUI stage, percent;
        Image[] segments, stageIcons;
        TextMeshProUGUI[] postLines;
        int lastPercent = -1, lastStage = -1;
        float dolphinBaseX;
        Texture2D[] ownedTextures;
        Sprite[] ownedSprites;

        static readonly string[] Stages = { "正在检测硬件", "正在初始化设备", "正在载入系统", "正在准备桌面" };
        static readonly float[] StageStart = { 0f, .24f, .50f, .85f };

        // Self-test lines appear as the bar passes each threshold. ASCII only: the mono face has no CJK.
        static readonly string[] Post =
        {
            "LG/16 BIOS v2.16   (C) 2016 LG/16",
            "CPU   x86-64  4C/8T ......... OK",
            "MEM   8192 MB ............... OK",
            "GPU   2048 MB ............... OK",
            "HDD   1 TB .................. OK",
            "NET   100 Mb/s .............. OK",
            "AUX   ........................ 1",
        };
        static readonly float[] PostAt = { 0f, .05f, .13f, .29f, .45f, .62f, .86f };

        const int SegmentCount = 24;
        static readonly Color Ink = new Color(.84f, .92f, .96f);
        static readonly Color Dim = new Color(.39f, .55f, .64f);
        static readonly Color Faint = new Color(.29f, .42f, .49f);
        static readonly Color Blue = new Color(.36f, .72f, .87f);
        static readonly Color TrackColor = new Color(.11f, .20f, .26f);
        static readonly Color Teal = new Color(.33f, .84f, .70f);

        public bool IsOpaque => IsVisible && cover != null && cover.alpha >= .999f;
        public bool IsVisible => root != null && root.gameObject.activeSelf;

        public void Build(RectTransform display, TMP_FontAsset font)
        {
            if (root != null || display == null) return;
            var sans = XgUiV4Fonts.Sans != null ? XgUiV4Fonts.Sans : font;
            var heavy = XgUiV4Fonts.SansSemibold != null ? XgUiV4Fonts.SansSemibold : sans;
            var numbers = XgUiV4Fonts.Numbers != null ? XgUiV4Fonts.Numbers : sans;
            var mono = Resources.Load<TMP_FontAsset>("LingGuangV05/Fonts/OriginMono SDF");
            if (mono == null) mono = sans;

            root = Rect("LCD Boot Screen", display);
            Stretch(root);
            root.SetAsLastSibling();
            cover = root.gameObject.AddComponent<CanvasGroup>();
            cover.blocksRaycasts = false; cover.interactable = false;
            background = root.gameObject.AddComponent<Image>();
            background.color = Color.black; background.raycastTarget = false;
            var body = Rect("Boot Content", root);
            Stretch(body);
            content = body.gameObject.AddComponent<CanvasGroup>();
            content.blocksRaycasts = false; content.interactable = false;
            float h = Mathf.Max(200, display.rect.height);
            float w = Mathf.Max(200, display.rect.width);

            var ring = MakeRing(256, .035f);
            var innerRing = MakeRing(128, .05f);
            var dot = MakeDot(64);
            var pixels = new[] { MakePixels(Chip), MakePixels(Plug), MakePixels(Disk), MakePixels(Window) };
            var fish = MakePixels(Dolphin);
            ownedTextures = new[] { ring.texture, innerRing.texture, dot.texture, pixels[0].texture, pixels[1].texture, pixels[2].texture, pixels[3].texture, fish.texture };
            ownedSprites = new[] { ring, innerRing, dot, pixels[0], pixels[1], pixels[2], pixels[3], fish };

            // BIOS self-test log, top left.
            var log = Rect("Boot Self Test", body);
            log.anchorMin = log.anchorMax = new Vector2(.055f, .93f); log.pivot = new Vector2(0, 1);
            log.sizeDelta = new Vector2(w * .40f, h * .26f);
            postLines = new TextMeshProUGUI[Post.Length];
            for (int i = 0; i < Post.Length; i++)
            {
                var line = Label("Post " + i, log, Post[i], mono, h * .019f, Vector2.zero, Vector2.zero, i == Post.Length - 1 ? Teal : Faint);
                var r = line.rectTransform;
                r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0, 1);
                r.sizeDelta = new Vector2(0, h * .03f); r.anchoredPosition = new Vector2(0, -i * h * .031f);
                line.alignment = TextAlignmentOptions.MidlineLeft;
                if (i == 0) line.color = Dim;
                postLines[i] = line;
            }

            // Emblem: the primordial ring with a teal spark orbiting on its rim.
            var emblem = Rect("Boot Emblem", body);
            emblem.anchorMin = emblem.anchorMax = new Vector2(.5f, .675f);
            emblem.sizeDelta = new Vector2(h * .15f, h * .15f);
            Picture("Emblem Ring", emblem, ring, new Color(Blue.r, Blue.g, Blue.b, .85f), Vector2.one);
            Picture("Emblem Core", emblem, innerRing, new Color(Dim.r, Dim.g, Dim.b, .45f), Vector2.one * .52f);
            spark = Rect("Emblem Spark", emblem);
            spark.anchorMin = spark.anchorMax = new Vector2(.5f, .5f);
            spark.sizeDelta = new Vector2(h * .036f, h * .036f);
            sparkImage = spark.gameObject.AddComponent<Image>();
            sparkImage.sprite = dot; sparkImage.color = Teal; sparkImage.raycastTarget = false;

            Label("Boot Logo", body, LingGuangV05.Desktop.EraSafeBranding.Os, heavy, h * .068f, new Vector2(.5f, .525f), new Vector2(.85f, .12f), Ink)
                .characterSpacing = 3f;
            var sub = Label("Boot Subtitle", body, "PERSONAL WORKSTATION  ·  2016", sans, h * .018f, new Vector2(.5f, .462f), new Vector2(.8f, .05f), Dim);
            sub.characterSpacing = 8f;

            // Segmented progress (2016 boot bar) with a glow line underneath.
            var track = Rect("Boot Progress Track", body);
            track.anchorMin = new Vector2(.36f, .392f); track.anchorMax = new Vector2(.64f, .404f);
            track.offsetMin = track.offsetMax = Vector2.zero;
            segments = new Image[SegmentCount];
            float gap = .18f / SegmentCount;
            for (int i = 0; i < SegmentCount; i++)
            {
                var seg = Rect("Segment " + i, track);
                float x0 = (float)i / SegmentCount, x1 = (float)(i + 1) / SegmentCount - gap;
                seg.anchorMin = new Vector2(x0, 0); seg.anchorMax = new Vector2(x1, 1);
                seg.offsetMin = seg.offsetMax = Vector2.zero;
                var img = seg.gameObject.AddComponent<Image>();
                img.color = TrackColor; img.raycastTarget = false;
                segments[i] = img;
            }
            var glow = Rect("Boot Progress Line", body);
            glow.anchorMin = new Vector2(.36f, .382f); glow.anchorMax = new Vector2(.64f, .3845f);
            glow.offsetMin = glow.offsetMax = Vector2.zero;
            var glowTrack = glow.gameObject.AddComponent<Image>();
            glowTrack.color = new Color(TrackColor.r, TrackColor.g, TrackColor.b, .6f); glowTrack.raycastTarget = false;
            progressFill = Rect("Boot Progress", glow);
            progressFill.anchorMin = Vector2.zero; progressFill.anchorMax = new Vector2(0, 1);
            progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;
            var fill = progressFill.gameObject.AddComponent<Image>();
            fill.color = new Color(Blue.r, Blue.g, Blue.b, .8f); fill.raycastTarget = false;

            // One pixel icon per boot stage: hardware, devices, system, desktop.
            stageIcons = new Image[pixels.Length];
            float iconH = h * .046f, step = h * .085f;
            for (int i = 0; i < pixels.Length; i++)
            {
                var icon = Rect("Stage Icon " + i, body);
                icon.anchorMin = icon.anchorMax = new Vector2(.5f, .315f);
                icon.sizeDelta = new Vector2(iconH * 13f / 11f, iconH);
                icon.anchoredPosition = new Vector2((i - 1.5f) * step, 0);
                var img = icon.gameObject.AddComponent<Image>();
                img.sprite = pixels[i]; img.color = Faint; img.raycastTarget = false;
                stageIcons[i] = img;
            }

            stage = Label("Boot Stage", body, Stages[0], sans, h * .028f, new Vector2(.5f, .235f), new Vector2(.8f, .07f), new Color(.66f, .79f, .85f));
            percent = Label("Boot Percent", body, "0%", numbers, h * .02f, new Vector2(.5f, .19f), new Vector2(.8f, .05f), Dim);

            // The desktop's dolphin swims along the footer as the boot advances.
            dolphin = Rect("Boot Dolphin", body);
            dolphin.anchorMin = dolphin.anchorMax = new Vector2(.5f, .125f);
            dolphin.sizeDelta = new Vector2(h * .03f * 21f / 8f, h * .03f);
            dolphinBaseX = -w * .07f;
            var fishImage = dolphin.gameObject.AddComponent<Image>();
            fishImage.sprite = fish; fishImage.color = new Color(Dim.r, Dim.g, Dim.b, .75f); fishImage.raycastTarget = false;
            Label("Boot Footer", body, "LG / 16     ·     SYSTEM STARTUP", sans, h * .016f, new Vector2(.5f, .075f), new Vector2(.8f, .05f), Faint)
                .characterSpacing = 4f;

            // Second-glance detail: a teal point blinks in the corner just before the desktop appears.
            var corner = Rect("Boot Corner Spark", body);
            corner.anchorMin = corner.anchorMax = new Vector2(.962f, .058f);
            corner.sizeDelta = new Vector2(h * .016f, h * .016f);
            easterDot = corner.gameObject.AddComponent<Image>();
            easterDot.sprite = dot; easterDot.color = Clear(Teal); easterDot.raycastTarget = false;
            Hide();
        }

        public void Show()
        {
            if (root == null) return;
            root.gameObject.SetActive(true); cover.alpha = 1;
            lastPercent = lastStage = -1;
            SetProgress(0, 0);
        }

        public void SetProgress(float progress, float luminance)
        {
            if (root == null) return;
            progress = float.IsNaN(progress) ? 0 : Mathf.Clamp01(progress);
            luminance = float.IsNaN(luminance) ? 0 : Mathf.Clamp01(luminance);
            // Restore the original firmware/logo/progress presentation on the monitor.
            background.color = Color.Lerp(Color.black, new Color(.012f, .025f, .035f), luminance);
            content.alpha = luminance;
            progressFill.anchorMax = new Vector2(progress, 1);
            cover.alpha = 1 - CaseMenuMotion.SmootherStep(Mathf.InverseLerp(.94f, 1f, progress));
            int value = Mathf.RoundToInt(progress * 100);
            if (lastPercent != value) { lastPercent = value; percent.text = value + "%"; }
            int index = progress < StageStart[1] ? 0 : progress < StageStart[2] ? 1 : progress < StageStart[3] ? 2 : 3;
            if (lastStage != index) { lastStage = index; stage.text = Stages[index]; }

            float filled = progress * SegmentCount;
            for (int i = 0; i < segments.Length; i++)
            {
                float t = Mathf.Clamp01(filled - i);
                var lit = i == Mathf.FloorToInt(filled) && t > 0 ? Color.Lerp(Blue, Ink, .45f) : Blue;
                segments[i].color = Color.Lerp(TrackColor, lit, t);
            }

            for (int i = 0; i < stageIcons.Length; i++)
            {
                if (i < index) { stageIcons[i].color = Blue; continue; }
                if (i > index) { stageIcons[i].color = new Color(Faint.r, Faint.g, Faint.b, .45f); continue; }
                float end = i + 1 < StageStart.Length ? StageStart[i + 1] : 1f;
                float local = Mathf.InverseLerp(StageStart[i], end, progress);
                float pulse = .5f + .5f * Mathf.Cos(local * Mathf.PI * 6f);
                stageIcons[i].color = Color.Lerp(Blue, Ink, pulse * .6f);
            }

            for (int i = 0; i < postLines.Length; i++)
            {
                var c = postLines[i].color;
                c.a = progress >= PostAt[i] ? 1f : 0f;
                postLines[i].color = c;
            }

            float angle = (90f - progress * 450f) * Mathf.Deg2Rad;
            float radius = ((RectTransform)spark.parent).rect.width * .5f;
            spark.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            sparkImage.color = new Color(Teal.r, Teal.g, Teal.b, Mathf.Lerp(.55f, 1f, progress));

            dolphin.anchoredPosition = new Vector2(dolphinBaseX * (1f - 2f * progress), 0);

            float tail = Mathf.InverseLerp(StageStart[3], .94f, progress);
            float blink = tail <= 0 ? 0 : Mathf.Max(0, Mathf.Sin(tail * Mathf.PI * 4f)); // two blinks
            easterDot.color = new Color(Teal.r, Teal.g, Teal.b, blink);
        }

        public void SetReveal(float progress)
        {
            if (cover != null) cover.alpha = 1 - CaseMenuMotion.SmootherStep(progress);
        }

        public void SetShutdown(float progress)
        {
            if (root == null) return;
            root.gameObject.SetActive(true);
            background.color = Color.black;
            content.alpha = 0;
            cover.alpha = CaseMenuMotion.SmootherStep(progress);
        }

        public void Hide() { if (root != null) root.gameObject.SetActive(false); }

        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static Color Clear(Color c) { c.a = 0; return c; }

        static Image Picture(string name, RectTransform parent, Sprite sprite, Color color, Vector2 scale)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = new Vector2(.5f, .5f) - scale * .5f; rect.anchorMax = new Vector2(.5f, .5f) + scale * .5f;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var img = rect.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.raycastTarget = false;
            return img;
        }

        static TextMeshProUGUI Label(string name, Transform parent, string value, TMP_FontAsset font, float size,
            Vector2 center, Vector2 fraction, Color color)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = center - fraction * .5f; rect.anchorMax = center + fraction * .5f;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = size; text.color = color; text.text = value;
            text.alignment = TextAlignmentOptions.Center;
            text.overflowMode = TextOverflowModes.Overflow;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        // ---- Procedural art (no imported textures) ----

        static readonly string[] Chip =
        {
            "...#..#..#...", "..#########..", "..#.......#..", "###.#####.###", "..#.#...#.#..", "###.#...#.###",
            "..#.#...#.#..", "###.#####.###", "..#.......#..", "..#########..", "...#..#..#...",
        };
        static readonly string[] Plug =
        {
            "....#...#....", "....#...#....", "..#########..", "..#.......#..", "..#.......#..", "...#.....#...",
            "....#####....", "......#......", "......#......", "....###......", "....#........",
        };
        static readonly string[] Disk =
        {
            "##########...", "#.#.....#.#..", "#.#.....#..#.", "#.#######..##", "#..........##", "#.#########.#",
            "#.#.......#.#", "#.#.#####.#.#", "#.#.......#.#", "#.#########.#", "#############",
        };
        static readonly string[] Window =
        {
            "#############", "#.#.#.......#", "#############", "#...........#", "#.####.####.#", "#.####.####.#",
            "#...........#", "#.####.####.#", "#.####.####.#", "#...........#", "#############",
        };
        static readonly string[] Dolphin =
        {
            "..........##.........", ".........###.........", ".....##########......", "...##############...#",
            "####.#############.##", "...#############..##.", ".....##...#####......", "..........##.........",
        };

        static Texture2D NewTexture(int w, int h, FilterMode filter)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = filter, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            return tex;
        }

        static Sprite ToSprite(Texture2D tex)
        {
            var sprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 100f);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        static Sprite MakePixels(string[] rows)
        {
            int h = rows.Length, w = rows[0].Length;
            var tex = NewTexture(w, h, FilterMode.Point);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[(h - 1 - y) * w + x] = x < rows[y].Length && rows[y][x] == '#' ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            tex.SetPixels32(px); tex.Apply(false, false);
            return ToSprite(tex);
        }

        static Sprite MakeRing(int size, float thickness)
        {
            var tex = NewTexture(size, size, FilterMode.Bilinear);
            var px = new Color32[size * size];
            float c = (size - 1) * .5f, outer = size * .5f - 2f, inner = outer - size * thickness;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(outer - d + .5f) * Mathf.Clamp01(d - inner + .5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply(false, false);
            return ToSprite(tex);
        }

        static Sprite MakeDot(int size)
        {
            var tex = NewTexture(size, size, FilterMode.Bilinear);
            var px = new Color32[size * size];
            float c = (size - 1) * .5f, r = size * .5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / r;
                    float core = Mathf.Clamp01((.34f - d) * 12f);
                    float halo = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f) * .55f;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(core + halo) * 255));
                }
            tex.SetPixels32(px); tex.Apply(false, false);
            return ToSprite(tex);
        }

        void OnDestroy()
        {
            if (root != null)
            {
                if (Application.isPlaying) Destroy(root.gameObject);
                else DestroyImmediate(root.gameObject);
            }
            if (ownedSprites != null) foreach (var s in ownedSprites) if (s != null) { if (Application.isPlaying) Destroy(s); else DestroyImmediate(s); }
            if (ownedTextures != null) foreach (var t in ownedTextures) if (t != null) { if (Application.isPlaying) Destroy(t); else DestroyImmediate(t); }
        }
    }
}
