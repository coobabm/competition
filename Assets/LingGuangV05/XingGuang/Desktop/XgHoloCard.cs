using System.Collections.Generic;
using HongmengOS.Aero2010;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The self-insight "闪卡" (design v1.1 §5.3), after the layered holographic collectible cards of RuiC-card-skill:
    /// a card with a recessed art window, a subject glyph standing in front of it, a frame with the text, and a
    /// laser-rainbow foil (shader LingGuang/UIHolo) whose colours run across the card as it tilts after the pointer.
    /// Finishes rise with the stage: silver, gold, pearl, full laser; 「灵光一现」 for all six is the legendary card.
    /// It flips in when a wall is worked out alone, and opens again from the gold secret node in the tech tree.
    /// </summary>
    public sealed class XgHoloCard : MonoBehaviour
    {
        const float W = 380, H = 540;
        XingGuangView view;
        TMP_FontAsset font;
        XgSim bound;
        RectTransform overlay, card, art, subject, sparkle;
        CanvasGroup group;
        Image holo, frame, artBack;
        Material holoMat;
        TMP_Text title, stageLine, glyph, golden, why, stamp, serial, hint;
        readonly Queue<string> pending = new Queue<string>();
        Vector2 tilt;
        float shownAt = -1;
        bool closing;
        float closeAt;

        static Sprite rounded;

        public static XgHoloCard Install(XingGuangView view, RectTransform root, TMP_FontAsset font)
        {
            var c = view.gameObject.GetComponent<XgHoloCard>() ?? view.gameObject.AddComponent<XgHoloCard>();
            c.view = view; c.font = font;
            c.Build(root);
            XgTreePage.OpenInsightCard = c.Show;
            return c;
        }

        /// <summary>A rounded white card shape, used for the mask, the frame and the art window.</summary>
        static Sprite Rounded()
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

        Image Sliced(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Rounded(); img.type = Image.Type.Sliced; img.color = c; img.raycastTarget = false;
            return img;
        }

        TMP_Text Label(RectTransform rt, float size, Color c, TextAlignmentOptions align)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.fontSize = size; t.color = c; t.alignment = align; t.raycastTarget = false; t.richText = true;
            return t;
        }

        void Build(RectTransform root)
        {
            overlay = Rect("Insight Card", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dim = overlay.gameObject.AddComponent<Image>(); dim.color = new Color(.03f, .04f, .09f, .82f);
            var close = overlay.gameObject.AddComponent<Button>(); close.targetGraphic = dim; close.transition = Selectable.Transition.None;
            close.onClick.AddListener(Close);
            group = overlay.gameObject.AddComponent<CanvasGroup>();

            card = Rect("Card", overlay, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-W / 2, -H / 2 + 10), new Vector2(W / 2, H / 2 + 10));
            frame = Sliced(card, Color.white);
            frame.raycastTarget = true; // clicks on the card do not close it

            // Art window: recessed background, then the subject glyph in front (parallax moves them apart).
            art = Rect("Art", card, new Vector2(0, 1), Vector2.one, new Vector2(18, -312), new Vector2(-18, -58));
            artBack = Sliced(art, Color.gray);
            art.gameObject.AddComponent<RectMask2D>();
            sparkle = Rect("Lines", art, Vector2.zero, Vector2.one, new Vector2(-30, -30), new Vector2(30, 30));
            for (int i = 0; i < 9; i++)
            {
                var line = Rect("Line", sparkle, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-260, -1), new Vector2(260, 1));
                line.localRotation = Quaternion.Euler(0, 0, -60 + i * 15);
                line.anchoredPosition = new Vector2(0, (i - 4) * 22);
                var img = line.gameObject.AddComponent<Image>(); img.color = new Color(1, 1, 1, .13f); img.raycastTarget = false;
            }
            subject = Rect("Subject", art, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            glyph = Label(subject, 150, Color.white, TextAlignmentOptions.Center);
            glyph.fontStyle = FontStyles.Bold;
            glyph.outlineWidth = .12f; glyph.outlineColor = new Color32(0, 0, 0, 90);

            title = Label(Rect("Title", card, new Vector2(0, 1), Vector2.one, new Vector2(22, -52), new Vector2(-22, -12)), 26, Color.white, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            serial = Label(Rect("Serial", card, new Vector2(0, 1), Vector2.one, new Vector2(22, -52), new Vector2(-22, -12)), 14, Color.white, TextAlignmentOptions.MidlineRight);
            stageLine = Label(Rect("Stage", card, new Vector2(0, 1), Vector2.one, new Vector2(22, -346), new Vector2(-22, -318)), 15, Color.white, TextAlignmentOptions.MidlineLeft);
            golden = Label(Rect("Golden", card, new Vector2(0, 1), Vector2.one, new Vector2(22, -414), new Vector2(-22, -348)), 18, Color.white, TextAlignmentOptions.TopLeft);
            golden.fontStyle = FontStyles.Bold;
            why = Label(Rect("Why", card, Vector2.zero, Vector2.one, new Vector2(22, 46), new Vector2(-22, -418)), 15, Color.white, TextAlignmentOptions.TopLeft);
            stamp = Label(Rect("Stamp", card, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -300), new Vector2(-24, -240)), 28, new Color32(214, 40, 40, 230), TextAlignmentOptions.Center);
            stamp.fontStyle = FontStyles.Bold; stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, 14);
            stamp.outlineWidth = .2f; stamp.outlineColor = new Color32(214, 40, 40, 230);

            // The foil shines over the frame and the art, but under the card's text so the words stay readable.
            var foil = Rect("Foil", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            foil.SetSiblingIndex(art.GetSiblingIndex() + 1);
            holo = Sliced(foil, Color.white);
            holo.raycastTarget = false;
            var shader = Shader.Find("LingGuang/UIHolo");
            if (shader != null) { holoMat = new Material(shader) { hideFlags = HideFlags.DontSave }; holo.material = holoMat; }
            else holo.enabled = false;

            // A dark rim keeps the white words readable wherever the foil is brightest.
            foreach (var text in new[] { title, serial, stageLine, golden, why })
            {
                text.outlineWidth = .18f; text.outlineColor = new Color32(10, 14, 40, 200);
            }

            hint = Label(Rect("Hint", overlay, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-300, 14), new Vector2(300, 44)), 16, new Color(1, 1, 1, .7f), TextAlignmentOptions.Center);
            overlay.gameObject.SetActive(false);
        }

        void OnDestroy() { if (bound != null) { bound.InsightCard -= Enqueue; bound.AchievementEarned -= OnAchievement; } if (holoMat != null) Destroy(holoMat); }

        void Enqueue(string wall) { pending.Enqueue(wall); }
        void OnAchievement(XgAchievement a) { if (a.id == "lingguang") pending.Enqueue("lingguang"); }

        /// <summary>A card is on screen (or still fading out).</summary>
        public bool Showing => overlay != null && overlay.gameObject.activeSelf;

        /// <summary>Shows a card now (from the tree) or after the one on screen.</summary>
        public void Show(string wall)
        {
            if (overlay.gameObject.activeSelf) { pending.Enqueue(wall); return; }
            Fill(wall);
            overlay.gameObject.SetActive(true);
            overlay.SetAsLastSibling();
            shownAt = Time.unscaledTime; closing = false;
            view.Juice.Play(XgJuice.Sfx.Id.Unlock);
            view.Juice.Flash(Color.white, .15f, .6f);
        }

        void Close()
        {
            if (!overlay.gameObject.activeSelf || closing || Time.unscaledTime - shownAt < .6f) return;
            closing = true; closeAt = Time.unscaledTime;
        }

        static readonly string[] Glyphs = { "", "异", "邻", "忘", "译", "注", "模" };

        void Fill(string wall)
        {
            bool legend = wall == "lingguang";
            int stage; string name, nameEn, gold, goldEn, w, wEn;
            if (legend) { stage = 7; name = "灵光一现"; nameEn = "A Flash of Insight"; gold = "六个阶段，全部自己想通。"; goldEn = "Six stages, all worked out yourself."; w = "没有秘籍，没有提示，只有一次又一次地试。这张卡只发给你。"; wEn = "No secrets, no hints, only trying again and again. This card is yours alone."; }
            else XgSim.InsightCardText(wall, out stage, out name, out nameEn, out gold, out goldEn, out w, out wEn);
            int finish = legend ? 3 : stage <= 2 ? 1 : stage <= 4 ? 0 : stage == 5 ? 2 : 3;
            Color baseColor = finish == 1 ? new Color32(96, 110, 140, 255) : finish == 0 ? new Color32(150, 108, 30, 255) : finish == 2 ? new Color32(150, 110, 150, 255) : new Color32(40, 50, 120, 255);
            frame.color = baseColor;
            artBack.color = Color.Lerp(baseColor, Color.black, .35f);
            bool inbreeding = wall == XgSim.InbreedingId; // 近亲繁殖: a phenomenon card, not a solved wall
            title.text = (legend ? "" : inbreeding ? T("现象 · ", "Phenomenon · ") : T("自悟 · ", "Insight · ")) + T(name, nameEn);
            bool autolabel = wall == XgSim.EpiphanyCardId; // the auto-labelling realisation (AutoLabelEpiphany)
            serial.text = legend ? "★ 7/7" : wall == "degrade" || autolabel || inbreeding ? T("附卡", "Extra") : "No." + stage + "/6";
            glyph.text = legend ? "灵" : wall == "degrade" ? "捷" : autolabel ? "替" : inbreeding ? "近" : Glyphs[Mathf.Clamp(stage, 1, 6)];
            stageLine.text = legend ? T("隐藏成就", "Hidden achievement") : autolabel ? T("标注台 · 自己想到的", "Labelling desk · your own idea")
                : T("第 " + stage + " 阶段 · ", "Stage " + stage + " · ") + XgCatalog.StageYears[Mathf.Clamp(stage, 1, 6)];
            golden.text = T(gold, goldEn);
            why.text = T(w, wEn);
            stamp.text = legend ? T("灵光\n一现", "FLASH") : inbreeding ? T("已发现", "FOUND") : T("已自悟", "SOLVED");
            hint.text = T("移动鼠标转动卡片 · 点空白处收下", "Move the pointer to tilt the card · click outside to keep it");
            if (holoMat != null)
            {
                holoMat.SetFloat("_Finish", finish);
                holoMat.SetFloat("_Strength", legend ? .55f : .3f + .04f * stage);
                holoMat.SetFloat("_Sparkle", legend ? 1f : .45f + .06f * stage);
                holoMat.SetFloat("_Density", legend ? 4.5f : 3f);
            }
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
            // Flip in: from the back, spinning once, settling with a little overshoot.
            float enter = Mathf.Clamp01(t / .9f), ease = 1 - Mathf.Pow(1 - enter, 3);
            float spin = (1 - ease) * 540;
            float scale = Mathf.Lerp(.4f, 1, ease) + Mathf.Sin(enter * Mathf.PI) * .06f;
            if (closing)
            {
                float k = Mathf.Clamp01((Time.unscaledTime - closeAt) / .35f);
                group.alpha = 1 - k; scale *= 1 - .2f * k;
                if (k >= 1) { overlay.gameObject.SetActive(false); group.alpha = 1; closing = false; return; }
            }
            else group.alpha = Mathf.Clamp01(t / .25f);
            // Tilt after the pointer (through the monitor), with an idle sway when it is elsewhere.
            Vector2 target = new Vector2(Mathf.Sin(t * .9f) * .35f, Mathf.Cos(t * .7f) * .25f);
            if (Pointer(out var local)) target = new Vector2(Mathf.Clamp(local.x / (W * .6f), -1, 1), Mathf.Clamp(local.y / (H * .6f), -1, 1));
            tilt = Vector2.Lerp(tilt, target, 1 - Mathf.Exp(-8 * Time.unscaledDeltaTime));
            card.localRotation = Quaternion.Euler(-tilt.y * 16, tilt.x * 20 + spin, 0);
            card.localScale = new Vector3(scale, scale, 1);
            subject.anchoredPosition = tilt * 12;
            sparkle.anchoredPosition = -tilt * 6;
            if (holoMat != null) holoMat.SetVector("_Tilt", new Vector4(tilt.x + spin / 180f, tilt.y, 0, 0));
        }

        AeroLcdInputSurface surface;

        bool Pointer(out Vector2 local)
        {
            local = default;
            var mouse = Mouse.current;
            if (mouse == null) return false;
            if (surface == null) surface = FindFirstObjectByType<AeroLcdInputSurface>();
            if (surface == null || !surface.TryMap(mouse.position.ReadValue(), out var source)) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(card, source, surface.sourceCamera, out local);
        }
    }
}
