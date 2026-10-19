using System.Collections.Generic;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop
{
    /// <summary>
    /// 2026-10-19: replaces what was left of DreamOS's own (2020s, Fluent-like) look with the hongmengos Aero 2016 one the
    /// taskbar, Start menu and title bars already have. It works on the running desktop and changes no scene or DreamOS asset:
    /// <list type="bullet">
    /// <item>DreamOS's big-radius rounded sprites become 3 px Aero ones: panels and cards get a thin darker edge, button-sized
    /// shapes a glossy two-tone bevel, outlines a 1 px line. Progress fills become square.</item>
    /// <item>Its soft drop shadows inside windows go (Win7 cards had none); the scroll-fade gradients stay.</item>
    /// <item>Circles bigger than a status dot (icon-button hovers, avatars) become small rounded squares.</item>
    /// <item>The round toggle switches become Win7 checkboxes; scrollbars get a visible track and a bevelled thumb.</item>
    /// <item>Settings, still on DreamOS's dark default theme, gets the Aero light theme the other windows use.</item>
    /// <item>DreamOS's six sample wallpapers (dolphins, glass cube, …) become four original Aero-style ones
    /// (Resources/AeroWallpapers, made by Tools/AeroWallpapers/make_wallpapers.py), the light 晨光 first.</item>
    /// </list>
    /// 灵光.exe keeps its own dark design and the Aero shell (taskbar, Start menu) is already Aero, so both are skipped.
    /// Windows, menus and notifications created later are restyled as they appear (a sweep of the active UI each second).
    /// </summary>
    public sealed class AeroRestyle : MonoBehaviour
    {
        const string DesktopRoot = "hongmengos 2010 Desktop Source";
        // Desktop icons (Desktop List) and the desktop's own layers keep their look (user, 2026-10-19: 「不要改图标样式」).
        // Loading spinners stay round too: the boot screen's ring is a radial-filled circle, and squaring it drew a
        // spinning pie wedge in a box (2026-10-19, 「进入桌面那个圈圈的加载有问题」).
        static readonly string[] Skip = { "灵光.exe", "Taskbar", "Aero Start Menu", "Aero Shell Controller", "Aero Glass Taskbar",
            "Desktop List", "Widgets", "Resident Spider", "Prologue Layer", "Aero Edition Mark", "Spinner" };
        // "bliss" (原野, the owner's pick 2026-10-19) is first, so it is the default wallpaper.
        static readonly string[] Wallpapers = { "bliss", "morning", "aurora", "harmony", "dusk" };
        static readonly string[] WallpaperNames = { "原野", "晨光", "极光蓝", "翠绿", "暮紫" };
        /// <summary>A circle at most this big (desktop px) is a dot or a badge and stays round.</summary>
        const float DotSize = 22;

        static Sprite panel, card, pane, fill, outline, button, square;
        static Sprite[] wallpaperSprites;

        Transform root;
        readonly HashSet<int> done = new HashSet<int>();
        readonly HashSet<int> switches = new HashSet<int>();
        readonly HashSet<int> bars = new HashSet<int>();
        readonly List<Image> images = new List<Image>(4096);
        readonly List<SwitchManager> switchList = new List<SwitchManager>(64);
        readonly List<Scrollbar> barList = new List<Scrollbar>(64);
        float next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.GetComponent<AeroRestyle>() != null) return;
            var desktop = GameObject.Find(DesktopRoot);
            if (desktop == null) return;
            var restyle = runtime.gameObject.AddComponent<AeroRestyle>();
            restyle.root = desktop.transform;
            // Before any WallpaperObject.Start reads the library, so the first frame already shows the new wallpaper.
            restyle.ReplaceWallpapers();
        }

        void Start()
        {
            if (root == null) { enabled = false; return; }
            LightSettings();
            Sweep(true);
        }

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 1;
            Sweep(false);
        }

        void Sweep(bool includeInactive)
        {
            root.GetComponentsInChildren(includeInactive, images);
            foreach (var image in images)
                if (image != null && done.Add(image.GetInstanceID()) && !Skipped(image.transform)) Restyle(image);
            root.GetComponentsInChildren(includeInactive, switchList);
            foreach (var s in switchList)
                if (s != null && switches.Add(s.GetInstanceID()) && !Skipped(s.transform)) Checkbox(s);
            root.GetComponentsInChildren(includeInactive, barList);
            foreach (var b in barList)
                if (b != null && bars.Add(b.GetInstanceID()) && !Skipped(b.transform)) ScrollbarLook(b);
        }

        static bool Skipped(Transform t)
        {
            for (; t != null; t = t.parent)
                if (System.Array.IndexOf(Skip, t.name) >= 0 || t.GetComponent<AeroCheckBox>() != null) return true;
            return false;
        }

        // ---- Sprites ----

        void Restyle(Image image)
        {
            if (image.name == "Background" && image.transform.parent != null && image.transform.parent.name == "Navigation Panel")
            {
                // DreamOS's side rail becomes a Win7 task pane: a pale blue gradient with a hairline on its right edge.
                image.sprite = Pane; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 1;
                return;
            }
            var sprite = image.sprite;
            if (sprite == null) return;
            string name = sprite.name;
            if (name.StartsWith("Example Wallpaper")) { Wallpaper(image, name); return; }
            if (name.Contains("Shadow"))
            {
                // Soft card and window shadows; the gradient ones fade scroll edges and stay.
                if (!name.Contains("Gradient")) image.enabled = false;
                return;
            }
            bool rounded = name.StartsWith("Rounded "), radial = name.StartsWith("Radial ");
            if (!rounded && !radial) return;
            var size = image.rectTransform.rect.size;
            bool isOutline = name.Contains("Outline");
            if (radial && Mathf.Max(size.x, size.y) <= DotSize) return;
            if (image.type == Image.Type.Filled) { image.sprite = Square; return; }
            Sprite to;
            if (isOutline) to = Outline;
            else if (size.y >= 18 && size.y <= 56 && size.x <= 240 && Clickable(image.transform) && !InNavigation(image.transform)) to = Button;
            else if (Mathf.Min(size.x, size.y) >= 60)
            {
                to = Panel;
                var c = image.color;
                if (c.a < .15f && c.grayscale < .5f && InWindow(image.transform))
                {
                    // DreamOS cards are a 2 % dark tint over the window; Win7 group boxes are white with a visible grey-blue edge.
                    var theme = image.GetComponent<UIManagerElement>(); if (theme != null) theme.enabled = false;
                    image.color = new Color(1, 1, 1, .92f);
                    to = Card;
                }
            }
            else to = Fill;
            image.sprite = to;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1;
            image.fillCenter = true;
        }

        static bool Clickable(Transform t)
        {
            for (int i = 0; i < 3 && t != null; i++, t = t.parent)
                if (t.GetComponent<Selectable>() != null || t.GetComponent<ButtonManager>() != null) return true;
            return false;
        }

        /// <summary>Inside an app window (not a desktop icon, whose faint hover tint must stay see-through).</summary>
        static bool InWindow(Transform t)
        {
            for (; t != null; t = t.parent) if (t.name == "Apps & Windows") return true;
            return false;
        }

        static bool InNavigation(Transform t)
        {
            for (; t != null; t = t.parent) if (t.name == "Navigation Panel") return true;
            return false;
        }

        static Sprite Panel => panel != null ? panel : panel = Make("Aero Panel", 16, 3, (x, y) => 1f, .74f, 1);
        static Sprite Card => card != null ? card : card = Make("Aero Card", 16, 3, (x, y) => 1f, .80f, 1);
        static Sprite Pane => pane != null ? pane : pane = MakePane();
        static Sprite Fill => fill != null ? fill : fill = Make("Aero Fill", 16, 3, (x, y) => 1f, 1, 0);
        static Sprite Outline => outline != null ? outline : outline = Make("Aero Outline", 16, 3, null, 1, 1);
        static Sprite Square => square != null ? square : square = Make("Aero Square", 4, 0, (x, y) => 1f, 1, 0);
        // Win7 button: a light upper half, a slightly darker lower half, a thin white inner highlight and a darker edge.
        static Sprite Button => button != null ? button : button = Make("Aero Button", 32, 3,
            (x, y) => y > .5f ? Mathf.Lerp(.96f, 1f, (y - .5f) * 2) : Mathf.Lerp(.84f, .90f, y * 2), .62f, 1, true);

        /// <summary>
        /// A white 9-sliced rounded rectangle (tinted by the Image colour). <paramref name="shade"/> gives the fill brightness
        /// at (x, y) in 0–1 (y up), null for no fill; <paramref name="edge"/> is the border's brightness, <paramref name="stroke"/> its width.
        /// </summary>
        static Sprite Make(string id, int n, float radius, System.Func<float, float, float> shade, float edge, float stroke, bool highlight = false)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = id, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float cx = x + .5f, cy = y + .5f;
                    // Signed distance to the rounded rectangle (negative inside).
                    float qx = Mathf.Abs(cx - n * .5f) - (n * .5f - radius), qy = Mathf.Abs(cy - n * .5f) - (n * .5f - radius);
                    float d = Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0)) + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
                    float cover = Mathf.Clamp01(.5f - d);
                    float inside = Mathf.Clamp01(.5f - (d + stroke));
                    float v = shade != null ? shade(cx / n, cy / n) : 1;
                    if (highlight && cy > n - stroke - 1.5f && inside > 0) v = 1;
                    float a = shade != null ? cover : cover * (1 - inside);
                    float b = shade != null ? Mathf.Lerp(edge, v, stroke > 0 ? inside : 1) : edge;
                    px[y * n + x] = new Color(b, b, b, a);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            float border = n >= 8 ? Mathf.Max(radius, stroke) + 2 : 0;
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        /// <summary>Task-pane background: brighter at the top, a 1 px darker line down the right edge. Sliced 1 px left, 2 px right.</summary>
        static Sprite MakePane()
        {
            const int w = 8, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Aero Pane", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float b = x == w - 1 ? .80f : Mathf.Lerp(.95f, 1.04f, (y + .5f) / h);
                    px[y * w + x] = new Color(Mathf.Min(1, b), Mathf.Min(1, b), Mathf.Min(1, b * 1.0f), 1);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(1, 0, 2, 0));
        }

        // ---- Controls ----

        /// <summary>The round switch becomes a 15 px Win7 checkbox at the switch's right edge; the switch itself still takes the clicks.</summary>
        static void Checkbox(SwitchManager s)
        {
            var rt = (RectTransform)s.transform;
            foreach (Transform child in rt)
            {
                var group = child.GetComponent<CanvasGroup>();
                if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0; group.blocksRaycasts = false;
            }
            var go = new GameObject("Aero Check", typeof(RectTransform), typeof(CanvasRenderer), typeof(AeroCheckBox));
            var box = (RectTransform)go.transform;
            box.SetParent(rt, false);
            box.anchorMin = box.anchorMax = new Vector2(1, .5f); box.pivot = new Vector2(1, .5f);
            box.sizeDelta = new Vector2(15, 15); box.anchoredPosition = new Vector2(-4, 0);
            var check = go.GetComponent<AeroCheckBox>();
            check.source = s; check.raycastTarget = false;
        }

        /// <summary>Win7 scrollbars: a pale track and a bevelled grey thumb (DreamOS draws only a thin floating bar).</summary>
        static void ScrollbarLook(Scrollbar bar)
        {
            var track = bar.GetComponent<Image>();
            if (track != null)
            {
                track.sprite = Fill; track.type = Image.Type.Sliced; track.pixelsPerUnitMultiplier = 1;
                track.color = new Color32(232, 235, 239, 200);
                var theme = track.GetComponent<UIManagerElement>(); if (theme != null) theme.enabled = false;
            }
            if (bar.handleRect == null) return;
            foreach (var image in bar.handleRect.GetComponentsInChildren<Image>(true))
            {
                if (image.color.a < .01f) continue;
                image.sprite = Button; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 1;
                var theme = image.GetComponent<UIManagerElement>(); if (theme != null) theme.enabled = false;
                image.color = new Color32(196, 202, 210, 255);
            }
        }

        // ---- Theme ----

        /// <summary>Settings' 52 elements still point at DreamOS's dark default theme; give them the Aero light one.</summary>
        void LightSettings()
        {
            UIManager aero = null, dark = null;
            var all = root.GetComponentsInChildren<UIManagerElement>(true);
            var counts = new Dictionary<UIManager, int>();
            foreach (var e in all)
                if (e.themeManagerAsset != null) counts[e.themeManagerAsset] = counts.TryGetValue(e.themeManagerAsset, out int c) ? c + 1 : 1;
            foreach (var pair in counts)
            {
                var t = pair.Key;
                bool light = t.windowBGColorDark.grayscale > .8f;
                if (light && (aero == null || pair.Value > counts[aero])) aero = t;
                if (!light && t.selectedTheme == UIManager.SelectedTheme.Default) dark = t;
            }
            if (aero == null || dark == null) return;
            foreach (var e in all)
            {
                if (e.themeManagerAsset != dark || Skipped(e.transform)) continue;
                e.themeManagerAsset = aero;
                e.UpdateElement();
            }
        }

        // ---- Wallpapers ----

        static Sprite[] WallpaperSprites()
        {
            if (wallpaperSprites != null) return wallpaperSprites;
            var list = new List<Sprite>();
            foreach (var id in Wallpapers)
            {
                var tex = Resources.Load<Texture2D>("AeroWallpapers/" + id);
                if (tex != null) list.Add(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 100));
            }
            return wallpaperSprites = list.ToArray();
        }

        void ReplaceWallpapers()
        {
            var sprites = WallpaperSprites();
            if (sprites.Length == 0) return;
            var library = ScriptableObject.CreateInstance<WallpaperLibrary>();
            library.name = "Aero Wallpapers";
            library.wallpapers = new List<WallpaperLibrary.WallpaperItem>();
            for (int i = 0; i < sprites.Length; i++)
                library.wallpapers.Add(new WallpaperLibrary.WallpaperItem { wallpaperID = WallpaperNames[i], wallpaperSprite = sprites[i] });
            foreach (var manager in root.GetComponentsInChildren<WallpaperManager>(true))
            {
                manager.wallpaperLibrary = library;
                manager.wallpaperIndex = Mathf.Clamp(manager.wallpaperIndex, 0, sprites.Length - 1);
            }
        }

        /// <summary>Static leftovers that show a DreamOS sample (the lock screen, the setup screen's list, sample photos).</summary>
        static void Wallpaper(Image image, string name)
        {
            var sprites = WallpaperSprites();
            if (sprites.Length == 0) return;
            int n = name.Length > "Example Wallpaper".Length && int.TryParse(name.Substring("Example Wallpaper".Length).Trim(), out int k) ? k - 1 : 0;
            image.sprite = sprites[Mathf.Clamp(n, 0, 99) % sprites.Length];
        }
    }
}
