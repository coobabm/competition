using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 成就: the card album. Every achievement is a holographic card (XgSim.Cards.cs), sorted by category; the cards
    /// owned shine in their finish (silver, gold, holographic, cosmos, lenticular) and all catch the light together as
    /// the pointer moves over the binder; the rest are dark slots, the hidden ones without even a name. Click a card
    /// to take it out (XgHoloCard).
    /// </summary>
    public sealed class XgCardsPage : XgPage
    {
        const float CardW = 104, CardH = 146, Gap = 12, SectionH = 34;
        const int Cols = 9;

        sealed class Thumb
        {
            public XgAchievement a;
            public RectTransform rt;
            public Image frame;
            public GameObject owned, locked;
            public TMP_Text glyph, name, stars, fresh, lockName;
            public XgCardThumb hover;
        }

        RectTransform viewport, content;
        TMP_Text header, legend;
        RectTransform progress;
        readonly List<Thumb> thumbs = new List<Thumb>();
        readonly List<(string category, TMP_Text text)> sections = new List<(string, TMP_Text)>();
        readonly Dictionary<string, Material> artMats = new Dictionary<string, Material>();
        readonly Dictionary<int, Material> shineMats = new Dictionary<int, Material>();
        string shownKey = "";
        Vector2 tilt;

        public override void Build(RectTransform area)
        {
            var card = ui.Card(area, "cards", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root = card;
            header = ui.Text(Strip("Header", card, 8, 30, 16, 16), "", 18, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            header.fontStyle = FontStyles.Bold;
            var bar = Strip("Progress", card, 40, 6, 16, 520);
            progress = Bar(bar, "Fill", XgDark.Line, XgDark.Gold);
            legend = ui.Text(Rect("Legend", card, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-500, -50), new Vector2(-16, -8)), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineRight);

            viewport = Rect("Viewport", card, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -56));
            Panel(viewport, new Color32(28, 32, 52, 255)); // a dark binder page, so the foil reads
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect("Content", viewport, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(.5f, 1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = viewport; scroll.horizontal = false; scroll.scrollSensitivity = 30; scroll.movementType = ScrollRect.MovementType.Clamped;

            float y = 10, left = (1050 - (Cols * CardW + (Cols - 1) * Gap)) / 2;
            foreach (var category in XgSim.CardCategories)
            {
                var cards = XgSim.CardsIn(category);
                if (cards.Count == 0) continue;
                var title = ui.Text(Strip("Section " + category, content, y, SectionH - 6, left, left), "", 16, new Color(1, 1, 1, .9f), TextAlignmentOptions.BottomLeft);
                title.fontStyle = FontStyles.Bold;
                sections.Add((category, title));
                y += SectionH;
                for (int i = 0; i < cards.Count; i++)
                {
                    int col = i % Cols, row = i / Cols;
                    thumbs.Add(BuildThumb(cards[i], left + col * (CardW + Gap), y + row * (CardH + Gap)));
                }
                y += ((cards.Count + Cols - 1) / Cols) * (CardH + Gap) + 6;
            }
            content.sizeDelta = new Vector2(0, y + 10);
        }

        Material ArtMat(string category, int rarity)
        {
            string key = category + "/" + rarity;
            if (!artMats.TryGetValue(key, out var m)) artMats[key] = m = XgCardArt.Make(0, category, rarity, new Vector2(CardW - 8, CardH - 8), 6);
            return m;
        }

        Material ShineMat(int rarity)
        {
            if (!shineMats.TryGetValue(rarity, out var m)) shineMats[rarity] = m = XgCardArt.Make(2, XgSim.CardWall, rarity, new Vector2(CardW, CardH), 7);
            return m;
        }

        Thumb BuildThumb(XgAchievement a, float x, float y)
        {
            var t = new Thumb { a = a };
            t.rt = TopLeft("Card " + a.id, content, x, y, CardW, CardH);
            t.frame = XgCardArt.Sliced(t.rt, XgCardArt.Frame(a.rarity), 2);
            t.frame.raycastTarget = true;
            var button = t.rt.gameObject.AddComponent<Button>(); button.targetGraphic = t.frame; button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Open(t));
            t.hover = t.rt.gameObject.AddComponent<XgCardThumb>();
            UiTip.Add(t.rt, () => Tip(t.a));

            var owned = Rect("Owned", t.rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            t.owned = owned.gameObject;
            var artRt = Rect("Art", owned, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4));
            var artMat = ArtMat(a.category, a.rarity);
            if (artMat != null) XgCardArt.Layer(artRt, artMat);
            else XgCardArt.Sliced(artRt, new Color(.12f, .14f, .28f), 2);
            t.glyph = ui.Text(Rect("Glyph", owned, Vector2.zero, Vector2.one, new Vector2(0, 26), new Vector2(0, -10)), a.glyph, 54, Color.white, TextAlignmentOptions.Center);
            t.glyph.fontStyle = FontStyles.Bold; t.glyph.outlineWidth = .12f; t.glyph.outlineColor = new Color32(0, 0, 0, 110);
            var strip = Rect("Name strip", owned, new Vector2(0, 0), new Vector2(1, 0), new Vector2(4, 4), new Vector2(-4, 30));
            Panel(strip, new Color(0, 0, 0, .45f)).raycastTarget = false;
            t.name = ui.Text(Rect("Name", strip, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-4, 0)), "", 12, Color.white, TextAlignmentOptions.Center);
            t.name.enableAutoSizing = true; t.name.fontSizeMin = 9; t.name.fontSizeMax = 12; t.name.textWrappingMode = TextWrappingModes.NoWrap;
            t.stars = ui.Text(Rect("Stars", owned, new Vector2(0, 1), new Vector2(1, 1), new Vector2(8, -22), new Vector2(-8, -6)), new string('★', Mathf.Clamp(a.rarity + 1, 1, 5)), 10, XgDark.Star, TextAlignmentOptions.TopLeft);
            var shine = ShineMat(a.rarity);
            if (shine != null) XgCardArt.Layer(Rect("Shine", owned, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), shine);
            var badge = Rect("New", t.rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-44, -20), new Vector2(-4, -4));
            XgCardArt.Sliced(badge, XgDark.Bad, 4);
            t.fresh = ui.Text(Rect("Text", badge, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "NEW", 11, Color.white, TextAlignmentOptions.Center);
            t.fresh.fontStyle = FontStyles.Bold;

            var locked = Rect("Locked", t.rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            t.locked = locked.gameObject;
            XgCardArt.Sliced(Rect("Slot", locked, Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3)), new Color32(40, 44, 66, 255), 2);
            ui.Text(Rect("Mark", locked, Vector2.zero, Vector2.one, new Vector2(0, 26), new Vector2(0, -10)), "？", 46, new Color(1, 1, 1, .18f), TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
            t.lockName = ui.Text(Rect("Name", locked, new Vector2(0, 0), new Vector2(1, 0), new Vector2(6, 6), new Vector2(-6, 30)), "", 11, new Color(1, 1, 1, .4f), TextAlignmentOptions.Center);
            t.lockName.enableAutoSizing = true; t.lockName.fontSizeMin = 9; t.lockName.fontSizeMax = 11; t.lockName.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        string Tip(XgAchievement a)
        {
            if (Sim == null) return "";
            string rarity = XgSim.RarityName(a.rarity, En) + " · " + XgCardArt.FinishName(a.rarity);
            if (Sim.HasAchievement(a.id)) return T(a.name, a.nameEn) + "（" + rarity + "）\n" + T(a.note, a.noteEn) + Lang.T("\n点击取出来看。");
            if (a.hidden)
                return T("隐藏成就（" + rarity + "）\n", "Hidden (" + rarity + ")\n") + (string.IsNullOrEmpty(a.hint) ? Lang.T("没有人知道怎么拿到它。") : T(a.hint, a.hintEn));
            return T(a.name, a.nameEn) + "（" + rarity + "）\n" + Lang.T("还没拿到：") + T(a.note, a.noteEn);
        }

        void Open(Thumb t)
        {
            if (Sim == null) return;
            if (!Sim.HasAchievement(t.a.id)) { Fx.Play(XgJuice.Sfx.Id.Tick); Fx.Knock(t.rt, .08f); return; }
            Sim.LookedAtCard(t.a.id);
            view.Holo?.Show(t.a.id, true);
            shownKey = "";
        }

        public override void Shown() { shownKey = ""; Refresh(); }

        public override void Refresh()
        {
            if (Sim == null) return;
            string key = Sim.S.achievements.Count + "/" + (Sim.S.cardsViewed?.Count ?? 0) + "/" + En;
            if (key == shownKey) return;
            shownKey = key;
            int owned = Sim.CardsOwned(), total = XgSim.Achievements.Length, hiddenTotal = 0, hiddenOwned = 0;
            var byRarity = new int[5]; var ownedByRarity = new int[5];
            foreach (var a in XgSim.Achievements)
            {
                int r = Mathf.Clamp(a.rarity, 0, 4);
                byRarity[r]++;
                if (Sim.HasAchievement(a.id)) ownedByRarity[r]++;
                if (a.hidden) { hiddenTotal++; if (Sim.HasAchievement(a.id)) hiddenOwned++; }
            }
            header.text = Lang.T("成就卡册") + "   <size=15><color=#" + ColorUtility.ToHtmlStringRGB(XgDark.Muted) + ">" + Lang.T("已收集 ") + owned + " / " + total
                + Lang.T(" · 隐藏 ") + hiddenOwned + " / " + hiddenTotal + "</color></size>";
            SetBar(progress, total > 0 ? owned / (float)total : 0);
            var parts = new List<string>();
            for (int r = 0; r < 5; r++)
                parts.Add("<color=#" + ColorUtility.ToHtmlStringRGB(Color.Lerp(XgCardArt.Frame(r), Color.white, .15f)) + ">■</color> " + XgCardArt.FinishName(r) + " " + ownedByRarity[r] + "/" + byRarity[r]);
            legend.text = string.Join("   ", parts);
            foreach (var (category, text) in sections)
                text.text = XgSim.CategoryName(category, En) + "  <size=13><alpha=#99>" + Sim.CardsOwned(category) + " / " + XgSim.CardsIn(category).Count + "</size>";
            foreach (var t in thumbs)
            {
                bool has = Sim.HasAchievement(t.a.id);
                t.owned.SetActive(has); t.locked.SetActive(!has);
                t.frame.color = has ? XgCardArt.Frame(t.a.rarity) : new Color32(58, 62, 86, 255);
                t.name.text = T(t.a.name, t.a.nameEn);
                t.lockName.text = t.a.hidden ? "？？？" : T(t.a.name, t.a.nameEn);
                bool isNew = has && Sim.CardIsNew(t.a.id);
                t.fresh.transform.parent.gameObject.SetActive(isNew);
            }
        }

        public override void Tick(float dt)
        {
            // Every owned card catches the light together, after the pointer over the binder; a slow sway otherwise.
            float time = Time.unscaledTime;
            Vector2 target = new Vector2(Mathf.Sin(time * .5f) * .3f, Mathf.Cos(time * .37f) * .2f);
            if (XgCardArt.Pointer(viewport, out var local))
            {
                var size = viewport.rect.size;
                if (Mathf.Abs(local.x) <= size.x * .6f && Mathf.Abs(local.y) <= size.y * .6f)
                    target = new Vector2(Mathf.Clamp(local.x / (size.x * .5f), -1, 1), Mathf.Clamp(local.y / (size.y * .5f), -1, 1)) * .8f;
            }
            tilt = Vector2.Lerp(tilt, target, 1 - Mathf.Exp(-6 * dt));
            var v = new Vector4(tilt.x, tilt.y, 0, 0);
            foreach (var m in artMats.Values) if (m != null) m.SetVector("_Tilt", v);
            foreach (var m in shineMats.Values) if (m != null) m.SetVector("_Tilt", v);
            // The card under the pointer lifts and turns a little toward it.
            foreach (var t in thumbs)
            {
                float k = t.hover.Lift(dt);
                t.rt.localScale = Vector3.one * (1 + .07f * k);
                t.rt.localRotation = Quaternion.Euler(-tilt.y * 10 * k, tilt.x * 14 * k, 0);
            }
        }
    }

    /// <summary>Hover state of an album card (eased, so the lift is smooth).</summary>
    public sealed class XgCardThumb : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        bool over;
        float lift;
        public void OnPointerEnter(PointerEventData e) { over = true; transform.SetAsLastSibling(); }
        public void OnPointerExit(PointerEventData e) { over = false; }
        void OnDisable() { over = false; lift = 0; }
        public float Lift(float dt) { lift = Mathf.MoveTowards(lift, over ? 1 : 0, dt * 6); return lift; }
    }
}
