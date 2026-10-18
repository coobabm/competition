using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The rare items (稀有道具) in the window frame: the four-slot strip in the top bar (always there, a click opens the 背包 page),
    /// the choice of three after 灵光 learns something, and the gold toast when one arrives. The rules are in XgSim.Relics.cs.
    /// </summary>
    public sealed partial class XingGuangView
    {
        sealed class StripSlot
        {
            public RectTransform rt;
            public XgFrameGraphic frame;
            public TMP_Text glyph;
            public string shown = "";
        }

        const float StripSlotSize = 32, StripGap = 5, PickDelay = 1.2f;
        /// <summary>The strip's width in the top bar; the main-line bars end this far left of the stats.</summary>
        public const float StripWidth = 4 * StripSlotSize + 3 * StripGap;

        readonly StripSlot[] strip = new StripSlot[XgRelics.Slots];
        RectTransform relicStrip, pickOverlay;
        TMP_Text pickTitle, pickSub;
        readonly RectTransform[] pickCards = new RectTransform[XgSim.RelicPickSize];
        string pickSignature = "";
        float pickReadyAt = -1;
        Outline toastRim;

        void BuildRelicStrip(RectTransform bar)
        {
            // Left of the ¥ / income / power / clock columns.
            relicStrip = Rect("Relics", bar, new Vector2(1, 0), Vector2.one, new Vector2(-406 - StripWidth, 0), new Vector2(-406, 0));
            for (int i = 0; i < strip.Length; i++)
            {
                var s = new StripSlot();
                s.rt = Rect("Slot" + (i + 1), relicStrip, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(i * (StripSlotSize + StripGap), -StripSlotSize / 2), new Vector2(i * (StripSlotSize + StripGap) + StripSlotSize, StripSlotSize / 2));
                s.frame = s.rt.gameObject.AddComponent<XgFrameGraphic>();
                var button = s.rt.gameObject.AddComponent<Button>();
                button.targetGraphic = s.frame; button.transition = Selectable.Transition.None;
                button.onClick.AddListener(OpenBackpack);
                if (controller.Window != null) s.rt.gameObject.AddComponent<ChapterOneWindowFocus>().Window = controller.Window;
                s.glyph = ui.Text(Rect("Glyph", s.rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 15, XgDark.Ink, TextAlignmentOptions.Center);
                s.glyph.textWrappingMode = TextWrappingModes.NoWrap; s.glyph.fontStyle = FontStyles.Bold;
                int at = i;
                UiTip.Add(s.rt, () =>
                {
                    var r = Sim == null ? null : Sim.RelicInSlot(at);
                    return (r == null ? XgRelicUi.EmptyTip(Sim) : XgRelicUi.Tip(Sim, r)) + "\n" + Hex(XgDark.Muted) + T("点一下打开背包", "Click to open the backpack") + "</color>";
                });
                strip[i] = s;
            }
        }

        /// <summary>The strip and the backpack tab: the backpack opens once there is something to show.</summary>
        void OpenBackpack()
        {
            Juice.Play(XgJuice.Sfx.Id.Click);
            if (FeatureOpen("backpack")) ShowTab("backpack");
            else ShowToast(T(("灵光学会第一件事后，这里会有稀有道具。" .Replace("灵光", Sim.AiDisplayName)), ("Rare items turn up here once 灵光 has learned its first thing." .Replace("灵光", Sim.AiDisplayName))), 3);
        }

        void RefreshRelicStrip()
        {
            if (relicStrip == null || Sim == null) return;
            for (int i = 0; i < strip.Length; i++)
            {
                var s = strip[i];
                var r = Sim.RelicInSlot(i);
                if (r == null) { s.frame.Style(new Color32(9, 15, 22, 255), XgDark.Dim, true, 1); s.glyph.text = ""; s.shown = ""; continue; }
                var edge = XgRelicUi.RarityColor(r.rarity);
                s.frame.Style(new Color(edge.r * .22f, edge.g * .22f, edge.b * .22f, 1f), edge, false, 2);
                s.glyph.text = r.glyph; s.glyph.color = edge;
                s.shown = r.id;
            }
        }

        // ───────────── the choice of three ─────────────

        void BuildRelicPick()
        {
            pickOverlay = Rect("RelicPick", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(pickOverlay, new Color(0, 0, 0, .78f));
            if (controller.Window != null) pickOverlay.gameObject.AddComponent<ChapterOneWindowFocus>().Window = controller.Window;
            pickTitle = ui.Text(Rect("Title", pickOverlay, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-480, 196), new Vector2(480, 238)), "", 24, XgDark.Gold, TextAlignmentOptions.Center);
            pickTitle.textWrappingMode = TextWrappingModes.NoWrap;
            pickSub = ui.Text(Rect("Sub", pickOverlay, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-480, 164), new Vector2(480, 196)), "", 15, XgDark.Muted, TextAlignmentOptions.Center);
            pickSub.textWrappingMode = TextWrappingModes.NoWrap;
            pickOverlay.gameObject.SetActive(false);
        }

        void RefreshRelicPick()
        {
            if (Sim == null || root == null) return;
            bool want = Visible && Sim.RelicPickPending && welcome == null && !SetupShowing && !XgEmergenceCutscene.Busy;
            if (!want)
            {
                pickReadyAt = -1;
                if (pickOverlay != null && pickOverlay.gameObject.activeSelf) pickOverlay.gameObject.SetActive(false);
                return;
            }
            if (pickReadyAt < 0) pickReadyAt = Time.unscaledTime + PickDelay;
            if (Time.unscaledTime < pickReadyAt) return;
            if (pickOverlay == null) BuildRelicPick();
            string sig = string.Join(",", Sim.RelicPick) + (Sim.English ? "e" : "z");
            bool opening = !pickOverlay.gameObject.activeSelf;
            if (sig != pickSignature) { pickSignature = sig; BuildPickCards(); }
            if (opening)
            {
                pickOverlay.gameObject.SetActive(true);
                pickOverlay.SetAsLastSibling();
                Juice.BringToFront();
                Juice.Play(XgJuice.Sfx.Id.Whoosh);
                Juice.Play(XgJuice.Sfx.Id.Fanfare, 1.1f, .5f);
                foreach (var c in pickCards) if (c != null) Juice.Knock(c, .1f, Vector2.zero, .35f);
            }
            pickTitle.text = T(("灵光学会了新东西。挑一件稀有道具" .Replace("灵光", Sim.AiDisplayName)), ("灵光 learned something new. Pick a rare item" .Replace("灵光", Sim.AiDisplayName)));
            pickSub.text = T("它进背包后，随时可以装备或换下", "It goes in the backpack; equip or swap it any time");
        }

        void BuildPickCards()
        {
            // The old cards go out of the scene at once (SetActive first, so no graphic lingers for a frame).
            for (int i = 0; i < pickCards.Length; i++)
                if (pickCards[i] != null) { pickCards[i].gameObject.SetActive(false); Destroy(pickCards[i].gameObject); pickCards[i] = null; }
            var ids = Sim.RelicPick;
            for (int i = 0; i < ids.Count && i < pickCards.Length; i++)
            {
                var r = XgRelics.Get(ids[i]);
                if (r == null) continue;
                pickCards[i] = MakePickCard(r, (i - (ids.Count - 1) / 2f) * 320f);
            }
        }

        RectTransform MakePickCard(XgRelic r, float cx)
        {
            bool en = Sim.English;
            var edge = XgRelicUi.RarityColor(r.rarity);
            var rt = Rect("Card " + r.id, pickOverlay, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(cx - 150, -170), new Vector2(cx + 150, 150));
            var frame = rt.gameObject.AddComponent<XgFrameGraphic>();
            frame.Style(new Color(edge.r * .08f + .03f, edge.g * .08f + .05f, edge.b * .08f + .07f, 1f), edge, false, 2);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = frame; button.transition = Selectable.Transition.None;
            string id = r.id;
            button.onClick.AddListener(() => PickRelicCard(id, rt));
            var band = ui.Text(Rect("Rarity", rt, new Vector2(0, 1), Vector2.one, new Vector2(14, -34), new Vector2(-14, -8)), XgRelics.RarityName(r.rarity, en), 14, edge, TextAlignmentOptions.MidlineLeft);
            band.characterSpacing = 6; band.textWrappingMode = TextWrappingModes.NoWrap;
            var box = Rect("Mark", rt, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-36, -118), new Vector2(36, -46));
            var boxFrame = box.gameObject.AddComponent<XgFrameGraphic>(); boxFrame.raycastTarget = false;
            var glyph = ui.Text(Rect("Glyph", box, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 32, edge, TextAlignmentOptions.Center);
            glyph.textWrappingMode = TextWrappingModes.NoWrap; glyph.fontStyle = FontStyles.Bold;
            XgRelicUi.PaintTile(boxFrame, glyph, r);
            var name = ui.Text(Rect("Name", rt, new Vector2(0, 1), Vector2.one, new Vector2(12, -154), new Vector2(-12, -122)), XgRelics.Name(r, en), 19, XgDark.Ink, TextAlignmentOptions.Center);
            name.textWrappingMode = TextWrappingModes.NoWrap; name.overflowMode = TextOverflowModes.Ellipsis; name.fontStyle = FontStyles.Bold;
            ui.Text(Rect("Flavor", rt, new Vector2(0, 1), Vector2.one, new Vector2(16, -214), new Vector2(-16, -156)), XgRelics.Flavor(r, en), 14, XgDark.Muted, TextAlignmentOptions.Top);
            ui.Text(Rect("Effect", rt, new Vector2(0, 1), Vector2.one, new Vector2(16, -262), new Vector2(-16, -216)), XgRelics.Effect(r, en).Replace("灵光", Sim.AiDisplayName), 16, XgDark.Money, TextAlignmentOptions.Top);
            var pair = XgRelics.PairOf(r.id);
            if (pair != null)
            {
                var partner = XgRelics.Get(XgRelics.Partner(pair, r.id));
                ui.Text(Rect("Pair", rt, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 34), new Vector2(-16, 74)), T("搭 ", "With ") + XgRelics.Name(partner, en) + T("：", ": ") + (en ? pair.textEn : pair.textZh), 13, XgDark.Link, TextAlignmentOptions.Top);
            }
            ui.Text(Rect("Take", rt, Vector2.zero, new Vector2(1, 0), new Vector2(0, 6), new Vector2(0, 30)), T("选这件", "Take this one"), 15, edge, TextAlignmentOptions.Center);
            return rt;
        }

        void PickRelicCard(string id, RectTransform card)
        {
            var r = XgRelics.Get(id);
            if (r == null || !Sim.PickRelic(id)) return;
            var edge = XgRelicUi.RarityColor(r.rarity);
            Juice.Burst(Juice.At(card), 36, edge, XgJuice.Shape.Spark, 420);
            Juice.Play(XgJuice.Sfx.Id.Unlock, 1.1f);
            pickSignature = "";
            if (pickOverlay != null) pickOverlay.gameObject.SetActive(false);
            pickReadyAt = -1;
            Refresh(true);
        }

        // ───────────── arrival ─────────────

        void OnRelicGained(XgRelic r, string source, bool equipped)
        {
            if (r == null) return;
            var edge = XgRelicUi.RarityColor(r.rarity);
            string name = XgRelics.Name(r, Sim.English);
            string text = T("新道具进背包了：", "New item in the backpack: ") + name + (equipped ? T("（已装备）", " (equipped)") : "");
            AddFeed(text, 0);
            if (!Visible) return;
            ShowToast("<color=#" + ColorUtility.ToHtmlStringRGB(XgDark.Gold) + ">" + text + "</color>", 4.5f, XgDark.Gold);
            Juice.Play(XgJuice.Sfx.Id.Fanfare, 1, .7f);
            Juice.Flash(new Color32(255, 236, 170, 255), .18f, .45f);
            var at = relicStrip != null && equipped ? relicStrip : NavButton("backpack");
            if (at != null && at.gameObject.activeInHierarchy)
            {
                Juice.Burst(Juice.At(at), 26, edge, XgJuice.Shape.Star, 340);
                Juice.Knock(at, .12f);
            }
            Refresh(true);
        }

        public void ShowToast(string text, float seconds, Color accent)
        {
            ShowToast(text, seconds);
            if (toastRim != null) toastRim.effectColor = accent;
        }
    }
}
