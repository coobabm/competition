using System.Collections.Generic;
using HongmengOS.Aero2010;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The look of a card, shared by the big card and the album thumbnails: the theme of its category, the finish of
    /// its rarity, and the three materials of LingGuang/UIHoloCard (art, tint, shine).
    /// </summary>
    public static class XgCardArt
    {
        public const int Silver = 0, Gold = 1, Holo = 2, Cosmos = 3, Lenticular = 4;

        /// <summary>Finish of a rarity: 普通 silver, 稀有 gold, 史诗 holographic, 传说 cosmos, 隐藏传说 lenticular.</summary>
        public static int Finish(int rarity) => Mathf.Clamp(rarity, Silver, Lenticular);

        public static string FinishName(int rarity) => new[] { T("银箔", "Silver foil"), T("金箔", "Gold foil"), T("镭射", "Holographic"), T("星河", "Cosmos"), T("光栅", "Lenticular") }[Finish(rarity)];

        /// <summary>The frame around the art: the metal of the finish.</summary>
        public static Color Frame(int rarity)
        {
            switch (Finish(rarity))
            {
                case Silver: return new Color32(122, 132, 152, 255);
                case Gold: return new Color32(168, 120, 30, 255);
                case Holo: return new Color32(96, 72, 160, 255);
                case Cosmos: return new Color32(24, 30, 82, 255);
                default: return new Color32(18, 16, 30, 255);
            }
        }

        /// <summary>Theme colours of a category (light, dark), and the second state of a lenticular card.</summary>
        public static void Theme(string category, out Color light, out Color dark, out Color light2, out Color dark2)
        {
            switch (category)
            {
                case XgSim.CardWall: light = new Color(.35f, .52f, 1f); dark = new Color(.04f, .07f, .22f); break;
                case XgSim.CardInsight: light = new Color(.72f, .48f, 1f); dark = new Color(.12f, .05f, .26f); break;
                case XgSim.CardRoad: light = new Color(.36f, .86f, .6f); dark = new Color(.03f, .15f, .12f); break;
                case XgSim.CardCure: light = new Color(1f, .48f, .45f); dark = new Color(.22f, .04f, .07f); break;
                case XgSim.CardPhenomenon: light = new Color(.3f, .8f, .95f); dark = new Color(.03f, .12f, .18f); break;
                case XgSim.CardData: light = new Color(1f, .76f, .32f); dark = new Color(.2f, .1f, .02f); break;
                case XgSim.CardFun: light = new Color(1f, .5f, .76f); dark = new Color(.22f, .05f, .15f); break;
                case XgSim.CardLife: light = new Color(1f, .62f, .4f); dark = new Color(.2f, .07f, .05f); break;
                default: light = new Color(1f, .9f, .62f); dark = new Color(.1f, .08f, .12f); break;
            }
            // The other side of the lens: the complementary hue, so the flip is plain to see.
            Color.RGBToHSV(light, out float h, out float sat, out float v);
            light2 = Color.HSVToRGB((h + .45f) % 1, sat, v);
            Color.RGBToHSV(dark, out h, out sat, out v);
            dark2 = Color.HSVToRGB((h + .45f) % 1, sat, v);
        }

        static Shader shader;
        static bool looked;

        public static Shader Shader
        {
            get
            {
                if (!looked) { shader = Shader.Find("LingGuang/UIHoloCard"); looked = true; }
                return shader;
            }
        }

        /// <summary>A material for one layer (0 art, 1 tint, 2 shine) of a card of this size; null without the shader.</summary>
        public static Material Make(int mode, string category, int rarity, Vector2 size, float radius)
        {
            if (Shader == null) return null;
            var m = new Material(Shader) { hideFlags = HideFlags.DontSave };
            m.SetFloat("_Mode", mode);
            m.SetFloat("_SrcBlend", (float)(mode == 0 ? BlendMode.One : mode == 1 ? BlendMode.DstColor : BlendMode.One));
            m.SetFloat("_DstBlend", (float)(mode == 0 ? BlendMode.OneMinusSrcAlpha : mode == 1 ? BlendMode.SrcColor : BlendMode.One));
            m.SetVector("_Size", new Vector4(size.x, size.y, radius, 0));
            Style(m, category, rarity);
            return m;
        }

        public static void Style(Material m, string category, int rarity)
        {
            if (m == null) return;
            int finish = Finish(rarity);
            Theme(category, out var a, out var b, out var c, out var d);
            m.SetFloat("_Finish", finish);
            m.SetColor("_ColorA", a); m.SetColor("_ColorB", b); m.SetColor("_ColorC", c); m.SetColor("_ColorD", d);
            m.SetFloat("_Foil", new[] { .45f, .6f, .75f, .9f, 1f }[finish]);
            m.SetFloat("_Rim", new[] { 0f, .6f, .5f, .85f, 1f }[finish]);
            m.SetFloat("_BgDepth", finish == Cosmos ? -.35f : -.25f);
        }

        /// <summary>The lenticular flip at the middle of the card for a tilt (the shader adds the strips).</summary>
        public static float Flip(Vector2 tilt)
        {
            float t = Mathf.Min(Mathf.Abs(tilt.x * 20) / 14, 1);
            return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.5f - .22f, .5f + .22f, t));
        }

        public static RawImage Layer(RectTransform rt, Material m)
        {
            var img = rt.gameObject.AddComponent<RawImage>();
            img.raycastTarget = false; img.material = m;
            if (m == null) img.enabled = false;
            return img;
        }

        static AeroLcdInputSurface surface;

        /// <summary>The pointer in a rect's local space, mapped through the desktop monitor.</summary>
        public static bool Pointer(RectTransform rt, out Vector2 local)
        {
            local = default;
            var mouse = Mouse.current;
            if (mouse == null || rt == null) return false;
            if (surface == null) surface = Object.FindFirstObjectByType<AeroLcdInputSurface>();
            if (surface == null || !surface.TryMap(mouse.position.ReadValue(), out var source)) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, source, surface.sourceCamera, out local);
        }

        static Sprite rounded;

        /// <summary>A rounded white card shape for sliced frames and panels.</summary>
        public static Sprite Rounded()
        {
            if (rounded != null) return rounded;
            const int n = 64, r = 14;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(r - x - .5f, x + .5f - (n - r))), dy = Mathf.Max(0, Mathf.Max(r - y - .5f, y + .5f - (n - r)));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + .5f)));
                }
            tex.Apply();
            rounded = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            rounded.hideFlags = HideFlags.DontSave;
            return rounded;
        }

        public static Image Sliced(RectTransform rt, Color c, float pixelsPerUnit = 1)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Rounded(); img.type = Image.Type.Sliced; img.color = c; img.raycastTarget = false;
            img.pixelsPerUnitMultiplier = pixelsPerUnit;
            return img;
        }
    }

    /// <summary>
    /// The big card (design v1.1 §5.3), after holo-card-studio's layered holographic cards: a recessed art window
    /// sunk behind the frame by parallax, the subject glyph standing in front, the foil tinting it, and the shine on
    /// top: sweep, stars, particles and the edge. The finish follows the rarity (XgCardArt). It flips in when a
    /// card is earned (rare and up, and every self-insight card), and opens from the 成就 album and the tech tree.
    /// </summary>
    public sealed class XgHoloCard : MonoBehaviour
    {
        const float W = 380, H = 540, ArtW = 344, ArtH = 254;
        XingGuangView view;
        TMP_FontAsset font;
        XgSim bound;
        RectTransform overlay, card, art, subject;
        CanvasGroup group;
        Image frame;
        RawImage artLayer, tintLayer, shineLayer;
        Material artMat, tintMat, shineMat;
        TMP_Text title, kicker, glyph, glyph2, headline, body, stamp, serial, hint;
        readonly Queue<string> pending = new Queue<string>();
        Vector2 tilt;
        float shownAt = -1;
        bool closing, quiet, lenticular;
        float closeAt;

        public static XgHoloCard Install(XingGuangView view, RectTransform root, TMP_FontAsset font)
        {
            var c = view.gameObject.GetComponent<XgHoloCard>() ?? view.gameObject.AddComponent<XgHoloCard>();
            c.view = view; c.font = font;
            c.Build(root);
            XgTreePage.OpenInsightCard = id => c.Show(id);
            return c;
        }

        TMP_Text Label(RectTransform rt, float size, Color c, TextAlignmentOptions align)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.fontSize = size; t.color = c; t.alignment = align; t.raycastTarget = false; t.richText = true;
            return t;
        }

        void Build(RectTransform root)
        {
            overlay = Rect("Card Overlay", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dim = overlay.gameObject.AddComponent<Image>(); dim.color = new Color(.03f, .04f, .09f, .82f);
            var close = overlay.gameObject.AddComponent<Button>(); close.targetGraphic = dim; close.transition = Selectable.Transition.None;
            close.onClick.AddListener(Close);
            group = overlay.gameObject.AddComponent<CanvasGroup>();

            card = Rect("Card", overlay, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-W / 2, -H / 2 + 10), new Vector2(W / 2, H / 2 + 10));
            frame = XgCardArt.Sliced(card, Color.white);
            frame.raycastTarget = true; // clicks on the card do not close it

            // Art window: the recessed background, the subject in front (parallax moves them apart), the foil over both.
            art = Rect("Art", card, new Vector2(0, 1), Vector2.one, new Vector2(18, -58 - ArtH), new Vector2(-18, -58));
            art.gameObject.AddComponent<RectMask2D>();
            artMat = XgCardArt.Make(0, XgSim.CardWall, 2, new Vector2(ArtW, ArtH), 10);
            artLayer = XgCardArt.Layer(Rect("Background", art, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), artMat);
            if (artMat == null) { var plain = XgCardArt.Sliced(Rect("Plain", art, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), new Color(.1f, .12f, .25f)); plain.transform.SetAsFirstSibling(); }
            subject = Rect("Subject", art, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            glyph = Label(subject, 150, Color.white, TextAlignmentOptions.Center);
            glyph.fontStyle = FontStyles.Bold;
            glyph.outlineWidth = .12f; glyph.outlineColor = new Color32(0, 0, 0, 90);
            glyph2 = Label(Rect("Subject B", subject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 150, Color.white, TextAlignmentOptions.Center);
            glyph2.fontStyle = FontStyles.Bold;
            glyph2.outlineWidth = .12f; glyph2.outlineColor = new Color32(0, 0, 0, 90);
            tintMat = XgCardArt.Make(1, XgSim.CardWall, 2, new Vector2(ArtW, ArtH), 10);
            tintLayer = XgCardArt.Layer(Rect("Foil", art, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), tintMat);

            // The shine covers the whole card but stays under the words so they remain readable.
            shineMat = XgCardArt.Make(2, XgSim.CardWall, 2, new Vector2(W, H), 14);
            shineLayer = XgCardArt.Layer(Rect("Shine", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), shineMat);

            title = Label(Rect("Title", card, new Vector2(0, 1), Vector2.one, new Vector2(22, -52), new Vector2(-22, -12)), 26, Color.white, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.enableAutoSizing = true; title.fontSizeMin = 18; title.fontSizeMax = 26;
            title.margin = new Vector4(0, 0, 70, 0);
            serial = Label(Rect("Serial", card, new Vector2(0, 1), Vector2.one, new Vector2(22, -52), new Vector2(-22, -12)), 14, Color.white, TextAlignmentOptions.MidlineRight);
            kicker = Label(Rect("Kicker", card, new Vector2(0, 1), Vector2.one, new Vector2(22, -346), new Vector2(-22, -318)), 15, Color.white, TextAlignmentOptions.MidlineLeft);
            headline = Label(Rect("Headline", card, new Vector2(0, 1), Vector2.one, new Vector2(22, -414), new Vector2(-22, -348)), 18, Color.white, TextAlignmentOptions.TopLeft);
            headline.fontStyle = FontStyles.Bold;
            headline.enableAutoSizing = true; headline.fontSizeMin = 13; headline.fontSizeMax = 18;
            body = Label(Rect("Body", card, Vector2.zero, Vector2.one, new Vector2(22, 22), new Vector2(-22, -418)), 15, Color.white, TextAlignmentOptions.TopLeft);
            body.enableAutoSizing = true; body.fontSizeMin = 11; body.fontSizeMax = 15;
            stamp = Label(Rect("Stamp", card, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -300), new Vector2(-24, -240)), 28, new Color32(214, 40, 40, 230), TextAlignmentOptions.Center);
            stamp.fontStyle = FontStyles.Bold; stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, 14);
            stamp.outlineWidth = .2f; stamp.outlineColor = new Color32(214, 40, 40, 230);
            // A dark rim keeps the white words readable wherever the shine is brightest.
            foreach (var text in new[] { title, serial, kicker, headline, body })
            {
                text.outlineWidth = .18f; text.outlineColor = new Color32(10, 14, 40, 200);
            }

            hint = Label(Rect("Hint", overlay, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-300, 14), new Vector2(300, 44)), 16, new Color(1, 1, 1, .7f), TextAlignmentOptions.Center);
            overlay.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (bound != null) { bound.InsightCard -= Enqueue; bound.AchievementEarned -= OnAchievement; }
            foreach (var m in new[] { artMat, tintMat, shineMat }) if (m != null) Destroy(m);
        }

        void Enqueue(string wall) { if (!pending.Contains(wall)) pending.Enqueue(wall); }

        /// <summary>
        /// A new card flips in when it is rare or better; commons only toast (XgSim.Earn says so). Self-insight cards
        /// come through InsightCard with the wall's own face, so their album twins stay quiet. A batch earned at once
        /// (an old save catching up) shows its first few only.
        /// </summary>
        void OnAchievement(XgAchievement a)
        {
            if (a.rarity < 1 || a.category == XgSim.CardInsight && a.id != "lingguang") return;
            if (pending.Count < 3) Enqueue(a.id);
        }

        /// <summary>A card is on screen (or still fading out).</summary>
        public bool Showing => overlay != null && overlay.gameObject.activeSelf;

        /// <summary>Shows a card now or after the one on screen. From the album it opens without the fanfare.</summary>
        public void Show(string id, bool fromAlbum = false)
        {
            if (overlay.gameObject.activeSelf) { Enqueue(id); return; }
            quiet = fromAlbum;
            Fill(id);
            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();
            shownAt = Time.unscaledTime; closing = false;
            bound?.MarkCardViewed(id);
            if (fromAlbum) { view.Juice.Play(XgJuice.Sfx.Id.Click); return; }
            view.Juice.Play(XgJuice.Sfx.Id.Unlock);
            view.Juice.Flash(Color.white, .15f, .6f);
        }

        void Close()
        {
            if (!overlay.gameObject.activeSelf || closing || Time.unscaledTime - shownAt < (quiet ? .25f : .6f)) return;
            closing = true; closeAt = Time.unscaledTime;
        }

        void Fill(string id)
        {
            var f = XgSim.CardFace(id);
            int finish = XgCardArt.Finish(f.rarity);
            lenticular = finish == XgCardArt.Lenticular && !string.IsNullOrEmpty(f.glyph2);
            var frameColor = XgCardArt.Frame(f.rarity);
            frame.color = frameColor;
            foreach (var m in new[] { artMat, tintMat, shineMat }) XgCardArt.Style(m, f.category, f.rarity);
            title.text = T(f.title, f.titleEn);
            serial.text = f.serial == "附卡" ? T("附卡", "Extra") : f.serial;
            glyph.text = f.glyph; glyph2.text = lenticular ? f.glyph2 : "";
            glyph2.alpha = 0; glyph.alpha = 1;
            string stars = new string('★', Mathf.Clamp(f.rarity + 1, 1, 5));
            kicker.text = T(f.kicker, f.kickerEn) + "  <color=#FFE08A>" + stars + "</color>  <size=12><alpha=#B0>" + XgCardArt.FinishName(f.rarity) + "</size>";
            headline.text = T(f.headline, f.headlineEn);
            body.text = T(f.body, f.bodyEn);
            stamp.text = T(f.stamp, f.stampEn);
            hint.text = lenticular ? T("左右转动卡片，看它的另一面 · 点空白处收下", "Turn the card left and right to see its other side · click outside to keep it")
                : quiet ? T("移动鼠标转动卡片 · 点空白处放回卡册", "Move the pointer to tilt the card · click outside to put it back")
                : T("移动鼠标转动卡片 · 点空白处收下", "Move the pointer to tilt the card · click outside to keep it");
        }

        void Update()
        {
            if (view == null) return;
            var sim = view.Sim;
            if (sim != bound)
            {
                if (bound != null) { bound.InsightCard -= Enqueue; bound.AchievementEarned -= OnAchievement; }
                bound = sim;
                if (bound != null) { bound.InsightCard += Enqueue; bound.AchievementEarned += OnAchievement; }
            }
            if (!overlay.gameObject.activeSelf)
            {
                if (pending.Count > 0 && view.Visible) Show(pending.Dequeue());
                return;
            }
            float t = Time.unscaledTime - shownAt;
            // Flip in: from the back, spinning once, settling with a little overshoot (from the album: a quick lift).
            float enter = Mathf.Clamp01(t / (quiet ? .35f : .9f)), ease = 1 - Mathf.Pow(1 - enter, 3);
            float spin = quiet ? 0 : (1 - ease) * 540;
            float scale = Mathf.Lerp(quiet ? .8f : .4f, 1, ease) + Mathf.Sin(enter * Mathf.PI) * (quiet ? .02f : .06f);
            if (closing)
            {
                float k = Mathf.Clamp01((Time.unscaledTime - closeAt) / .35f);
                group.alpha = 1 - k; scale *= 1 - .2f * k;
                if (k >= 1) { overlay.gameObject.SetActive(false); group.alpha = 1; closing = false; return; }
            }
            else group.alpha = Mathf.Clamp01(t / .25f);
            // Tilt after the pointer (through the monitor), with an idle sway when it is elsewhere.
            Vector2 target = new Vector2(Mathf.Sin(t * .9f) * .35f, Mathf.Cos(t * .7f) * .25f);
            if (XgCardArt.Pointer(card, out var local)) target = new Vector2(Mathf.Clamp(local.x / (W * .6f), -1, 1), Mathf.Clamp(local.y / (H * .6f), -1, 1));
            tilt = Vector2.Lerp(tilt, target, 1 - Mathf.Exp(-8 * Time.unscaledDeltaTime));
            card.localRotation = Quaternion.Euler(-tilt.y * 16, tilt.x * 20 + spin, 0);
            card.localScale = new Vector3(scale, scale, 1);
            subject.anchoredPosition = tilt * 12;
            var shaderTilt = new Vector4(tilt.x + spin / 180f, tilt.y, 0, 0);
            foreach (var m in new[] { artMat, tintMat, shineMat }) if (m != null) m.SetVector("_Tilt", shaderTilt);
            if (lenticular)
            {
                // The subject flips with the lens: one glyph fades as the other comes through.
                float m = XgCardArt.Flip(tilt);
                glyph.alpha = 1 - m; glyph2.alpha = m;
            }
        }
    }
}
