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
        static readonly Color PacketRed = new Color32(243, 90, 58, 255), AiGreen = new Color32(67, 160, 71, 255);

        /// <summary>
        /// Called by the YY view for every bubble: red packets are drawn red (QQ 2016 style), and a line the AI wrote
        /// for him is green with a hover note.
        /// </summary>
        public static void StyleBubble(YYMessage m, YYRoundRect shape, TMP_Text text)
        {
            if (m == null || shape == null) return;
            if (GirlfriendRules.IsPacket(m.text))
            {
                shape.color = PacketRed; shape.border = 0;
                if (text != null) text.color = Color.white;
            }
            else if (m.byAi)
            {
                shape.color = AiGreen;
                if (text != null) text.color = Color.white;
                shape.raycastTarget = true;
                UiTip.Add(shape, () => GameText.T("这条是 " + (Instance != null ? Instance.AiName : "灵光") + " 代你回的。她以为是你。", "The AI wrote this one for you. She thinks it was you."));
            }
        }
    }

    /// <summary>
    /// Her chat's own controls in the YY tool strip, on the right so the face picker keeps the left: the 红包 button
    /// (amount buttons over the strip, paid from the game wallet) and, from stage 5, 「让 灵光 代我回」.
    /// Shown only while her chat is selected.
    /// </summary>
    public sealed class YYGirlfriendBar : MonoBehaviour
    {
        YYChatView view;
        RectTransform tools, conversation, panel;
        Button packetButton, aiButton;
        TMP_Text packetLabel, aiLabel, panelTitle, panelNote;
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
            bool ai = hers && gf.StandInAvailable;
            aiButton.gameObject.SetActive(ai);
            if (!hers && panel.gameObject.activeSelf) panel.gameObject.SetActive(false);
            if (!hers) return;
            packetLabel.text = Lang.T("￥ 红包");
            if (ai)
            {
                bool on = gf.G.aiAuto;
                aiLabel.text = GameText.T("让 " + gf.AiName + " 代我回：" + (on ? "开" : "关"), "Let " + gf.AiName + " reply for me: " + (on ? "on" : "off"));
                ((YYRoundRect)aiButton.targetGraphic).color = on ? new Color32(67, 160, 71, 255) : new Color32(238, 241, 245, 255);
                aiLabel.color = on ? Color.white : new Color32(34, 40, 52, 255);
            }
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
            aiButton = Button(tools, "GfStandIn", new Color32(238, 241, 245, 255), new Color32(34, 40, 52, 255), out aiLabel, () =>
            {
                var gf = YYGirlfriend.Instance;
                if (gf != null && gf.G != null) gf.SetStandIn(!gf.G.aiAuto);
            });
            Place((RectTransform)aiButton.transform, new Vector2(1, .5f), new Vector2(-330, -13), new Vector2(-108, 13));
            UiTip.Add(aiButton, () => Lang.T("开着的时候，她的消息由 AI 用它自己的语气替你回。她可能会发现。"));

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
                foreach (var s in new Component[] { packetButton, aiButton, panel })
                    foreach (var sel in s.GetComponentsInChildren<Selectable>(true))
                    {
                        var focus = sel.GetComponent<ChapterOneWindowFocus>() ?? sel.gameObject.AddComponent<ChapterOneWindowFocus>();
                        focus.Window = window.Window;
                    }
            return true;
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
