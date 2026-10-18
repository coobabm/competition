using System;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core.Girlfriend;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYGirlfriend
    {
        static readonly Color PacketRed = new Color32(243, 90, 58, 255);

        /// <summary>
        /// Called by the YY view for every bubble: red packets are drawn red (QQ 2016 style).
        /// </summary>
        public static void StyleBubble(YYMessage m, YYRoundRect shape, TMP_Text text)
        {
            if (m == null || shape == null) return;
            if (GirlfriendRules.IsPacket(m.text))
            {
                shape.color = PacketRed; shape.border = 0;
                if (text != null) text.color = Color.white;
            }
        }
    }

    /// <summary>
    /// Her chat's own controls in the YY tool strip, on the right so the face picker keeps the left: the 红包 button
    /// (amount buttons over the strip, paid from the game wallet). Shown only while her chat is selected.
    /// </summary>
    public sealed class YYGirlfriendBar : MonoBehaviour
    {
        YYChatView view;
        RectTransform tools, conversation, panel;
        Button packetButton, giftButton;
        TMP_Text packetLabel, panelTitle, panelNote, giftLabel, giftTitle;
        RectTransform giftPanel;
        readonly List<(Button button, TMP_Text label)> giftRows = new List<(Button, TMP_Text)>();
        readonly List<string> giftIds = new List<string>();
        const int GiftRowsMax = 8;
        TMP_FontAsset font;
        bool built;

        static readonly double[] Amounts = { 5.20, 13.14, 52, 88.88, 200, 520, 1314 };

        public static void Ensure(YYChatView v)
        {
            if (v == null || v.GetComponent<YYGirlfriendBar>() != null) return;
            v.gameObject.AddComponent<YYGirlfriendBar>().view = v;
        }

        void Update()
        {
            var gf = YYGirlfriend.Instance;
            var hub = YYChatHub.Instance;
            if (view == null || gf == null || hub == null || hub.S == null) return;
            if (!built && !Build()) return;
            bool hers = hub.S.selected == YYChatHub.GirlfriendId && gf.G != null && gf.G.started && view.IsOpen;
            packetButton.gameObject.SetActive(hers);
            var held = hers ? gf.HeldGifts : null;
            giftButton.gameObject.SetActive(hers);
            if (!hers && panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
            if (!hers && giftPanel.gameObject.activeSelf) giftPanel.gameObject.SetActive(false);
            if (!hers) return;
            packetLabel.text = Lang.T("￥ 红包");
            giftLabel.text = GameText.T("礼物" + (held.Count > 0 ? " " + held.Count : ""), "Gift" + (held.Count > 0 ? " " + held.Count : ""));
            if (giftPanel.gameObject.activeSelf) RefreshGifts(held);
            if (panel.gameObject.activeSelf)
                panelTitle.text = GameText.T("发红包 · 钱包 ¥" + gf.Wallet.ToString("#,0.00", CultureInfo.InvariantCulture), "Send a red packet · wallet ¥" + gf.Wallet.ToString("#,0.00", CultureInfo.InvariantCulture));
        }

        bool Build()
        {
            tools = Find(view.transform, "Tools") as RectTransform;
            conversation = Find(view.transform, "Conversation") as RectTransform;
            if (tools == null || conversation == null) return false;
            foreach (var t in view.GetComponentsInChildren<TMP_Text>(true)) { if (t.font != null) { font = t.font; break; } }
            built = true;

            packetButton = Button(tools, "GfPacket", new Color32(243, 90, 58, 255), Color.white, out packetLabel, () => panel.gameObject.SetActive(!panel.gameObject.activeSelf));
            Place((RectTransform)packetButton.transform, new Vector2(1, .5f), new Vector2(-100, -13), new Vector2(-12, 13));
            UiTip.Add(packetButton, "QQ 红包，用游戏里的钱。钱表达心意，买不来感情：合不合时宜比金额重要。", "A red packet, paid in game money. Money shows you care but cannot buy feelings: timing matters more than the amount.");

            // 「礼物」: gifts bought on 淘货 wait here until he gives them himself (typing 「送你……」 works too).
            giftButton = Button(tools, "GfGift", new Color32(232, 80, 120, 255), Color.white, out giftLabel, () => { giftPanel.gameObject.SetActive(!giftPanel.gameObject.activeSelf); panel.gameObject.SetActive(false); });
            Place((RectTransform)giftButton.transform, new Vector2(1, .5f), new Vector2(-184, -13), new Vector2(-108, 13));
            UiTip.Add(giftButton, "淘货买的礼物先寄到你这儿，要你亲手送才会寄给她。也可以直接打字「送你……」。", "Gifts from Taohuo come to you first and only go to her when you give them. You can also just type that it's for her.");
            giftPanel = Rect("GfGiftPanel", conversation, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-372, 120), new Vector2(-12, 120 + 34 + GiftRowsMax * 30));
            var giftBg = giftPanel.gameObject.AddComponent<YYRoundRect>(); giftBg.radius = 8; giftBg.color = new Color32(255, 243, 247, 255); giftBg.border = 1; giftBg.borderColor = new Color32(232, 80, 120, 255);
            giftTitle = Text(Rect("Title", giftPanel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -30), new Vector2(-12, -6)), "", 14, new Color32(200, 50, 100, 255));
            for (int i = 0; i < GiftRowsMax; i++)
            {
                int index = i;
                var b = Button(giftPanel, "Gift", new Color32(255, 255, 255, 255), new Color32(34, 40, 52, 255), out var label, () => Give(index));
                Place((RectTransform)b.transform, new Vector2(0, 1), new Vector2(12, -64 - i * 30), new Vector2(348, -38 - i * 30));
                label.alignment = TextAlignmentOptions.MidlineLeft;
                giftRows.Add((b, label));
            }
            giftPanel.gameObject.SetActive(false);

            panel = Rect("GfPacketPanel", conversation, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-372, 174), new Vector2(-12, 290));
            var bg = panel.gameObject.AddComponent<YYRoundRect>(); bg.radius = 8; bg.color = new Color32(255, 247, 240, 255); bg.border = 1; bg.borderColor = new Color32(243, 90, 58, 255);
            panelTitle = Text(Rect("Title", panel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -30), new Vector2(-12, -6)), "", 14, new Color32(200, 60, 30, 255));
            for (int i = 0; i < Amounts.Length; i++)
            {
                double amount = Amounts[i];
                var b = Button(panel, "Amount", new Color32(243, 90, 58, 255), Color.white, out var label, () => Send(amount));
                label.text = "¥" + amount.ToString(amount % 1 == 0 ? "0" : "0.00", CultureInfo.InvariantCulture);
                int row = i / 4, col = i % 4;
                Place((RectTransform)b.transform, new Vector2(0, 1), new Vector2(12 + col * 86, -64 - row * 32), new Vector2(12 + col * 86 + 80, -38 - row * 32));
            }
            panelNote = Text(Rect("Note", panel, new Vector2(0, 0), new Vector2(1, 0), new Vector2(12, 2), new Vector2(-12, 18)), "", 12, new Color32(140, 140, 140, 255));
            panel.gameObject.SetActive(false);
            var window = YYChatHub.Instance != null && YYChatHub.Instance.router != null ? YYChatHub.Instance.router.Get(YYChatHub.AppId) : null;
            if (window != null && window.Window != null)
                foreach (var s in new Component[] { packetButton, panel, giftButton, giftPanel })
                    foreach (var sel in s.GetComponentsInChildren<Selectable>(true))
                    {
                        var focus = sel.GetComponent<ChapterOneWindowFocus>() ?? sel.gameObject.AddComponent<ChapterOneWindowFocus>();
                        focus.Window = window.Window;
                    }
            return true;
        }

        /// <summary>One row per kind of gift in hand (「YSL 星辰口红 ×2」); empty when nothing is waiting.</summary>
        void RefreshGifts(List<GfGiftOrder> held)
        {
            giftIds.Clear();
            var counts = new Dictionary<string, int>();
            foreach (var o in held) { if (!counts.ContainsKey(o.gift)) { counts[o.gift] = 0; giftIds.Add(o.gift); } counts[o.gift]++; }
            giftTitle.text = giftIds.Count > 0 ? GameText.T("送她一样（会寄到她住处）", "Give her one (sent to her home)") : GameText.T("手上没有礼物。去淘货买一件。", "No gifts in hand. Buy one on Taohuo.");
            for (int i = 0; i < giftRows.Count; i++)
            {
                bool on = i < giftIds.Count;
                giftRows[i].button.gameObject.SetActive(on);
                if (!on) continue;
                var gift = GirlfriendRules.Gift(giftIds[i]);
                string name = gift == null ? giftIds[i] : GameText.T(gift.zh, gift.en);
                giftRows[i].label.text = "  " + name + (counts[giftIds[i]] > 1 ? " ×" + counts[giftIds[i]] : "");
            }
        }

        void Give(int index)
        {
            var gf = YYGirlfriend.Instance;
            if (gf == null || index < 0 || index >= giftIds.Count) return;
            if (gf.GiveFromPanel(giftIds[index])) giftPanel.gameObject.SetActive(false);
        }

        void Send(double amount)
        {
            var gf = YYGirlfriend.Instance;
            if (gf == null) return;
            if (gf.SendPacket(amount, out string problem)) { panel.gameObject.SetActive(false); panelNote.text = ""; }
            else panelNote.text = problem;
        }

        // ───────────── small UI helpers (the YY view's look) ─────────────

        static Transform Find(Transform parent, string name)
        {
            foreach (Transform c in parent) { if (c.name == name) return c; var f = Find(c, name); if (f != null) return f; }
            return null;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 offMin, Vector2 offMax) { rt.anchorMin = rt.anchorMax = anchor; rt.offsetMin = offMin; rt.offsetMax = offMax; }

        TMP_Text Text(RectTransform rt, string text, float size, Color color)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.MidlineLeft;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        Button Button(Transform parent, string name, Color fill, Color ink, out TMP_Text label, Action click)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            var shape = rt.gameObject.AddComponent<YYRoundRect>(); shape.radius = 4; shape.color = fill;
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = shape;
            var cb = b.colors; cb.highlightedColor = new Color(.92f, .96f, 1f); cb.pressedColor = new Color(.8f, .86f, .95f); b.colors = cb;
            b.onClick.AddListener(() => click());
            label = Text(Rect("Label", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 13, ink);
            label.alignment = TextAlignmentOptions.Center;
            return b;
        }
    }
}
