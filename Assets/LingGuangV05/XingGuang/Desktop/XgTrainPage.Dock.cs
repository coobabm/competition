using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The training page's consumables (消耗品, XgSim.Consumables.cs): the quick bar of six slots under the train row (keys
    /// 1–6, a count badge, a cooldown veil, an empty slot greyed with its pack price that opens the 道具 page), and the
    /// chips of the effects running now beside the train button. Presentation only: the rules are in XgSim.
    /// </summary>
    public sealed partial class XgTrainPage
    {
        /// <summary>A small framed label: a running effect, or one factor of the effective scale.</summary>
        sealed class Chip
        {
            public RectTransform rt;
            public XgFrameGraphic frame;
            public TMP_Text text;
        }

        sealed class Slot
        {
            public XgConsumableDef def;
            public XgBtn button;
            public CanvasGroup group;
            public XgHoverRim rim;
            public XgFrameGraphic tile;
            public TMP_Text key, count, glyph, price, cooldown;
            public RectTransform veil;
        }

        RectTransform quickBar, buffRow;
        TMP_Text quickBarHead, quickBarLink, noBuffs;
        readonly List<Slot> slots = new List<Slot>();
        readonly List<Chip> buffChips = new List<Chip>();
        const int BuffChipCount = 5;

        Chip MakeChip(RectTransform parent, string name, float size)
        {
            var rt = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            rt.pivot = new Vector2(0, 1);
            var frame = rt.gameObject.AddComponent<XgFrameGraphic>();
            frame.raycastTarget = false;
            var text = ui.Text(Rect("Text", rt, Vector2.zero, Vector2.one, new Vector2(5, 0), new Vector2(-5, 0)), "", size, XgDark.Ink, TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return new Chip { rt = rt, frame = frame, text = text };
        }

        /// <summary>Sizes a chip to its text and puts it at (x, y) from the top-left of its parent; returns its width.</summary>
        static float PlaceChip(Chip c, float x, float y, float height, float minWidth = 0)
        {
            float w = Mathf.Max(minWidth, c.text.GetPreferredValues(c.text.text).x + 12);
            c.rt.sizeDelta = new Vector2(w, height);
            c.rt.anchoredPosition = new Vector2(x, -y);
            return w;
        }

        // ───────────── quick bar ─────────────

        void BuildQuickBar()
        {
            quickBar = ui.Card(left, "QuickBar", Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, QuickBarHeight));
            quickBarHead = ui.Text(Strip("Head", quickBar, 4, 18, 12, 150), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            quickBarHead.textWrappingMode = TextWrappingModes.NoWrap;
            quickBarLink = ui.Text(Rect("Shop", quickBar, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -22), new Vector2(-12, -4)), "", 12, XgDark.Link, TextAlignmentOptions.MidlineRight);
            quickBarLink.textWrappingMode = TextWrappingModes.NoWrap;
            var link = quickBarLink.gameObject.AddComponent<Button>();
            quickBarLink.raycastTarget = true;
            link.targetGraphic = quickBarLink; link.transition = Selectable.Transition.None;
            link.onClick.AddListener(() => { Fx.Play(XgJuice.Sfx.Id.Click); view.ShowTab("items"); });
            for (int i = 0; i < XgConsumables.All.Length; i++)
            {
                int index = i;
                var def = XgConsumables.All[i];
                var s = new Slot { def = def };
                s.button = ui.Button(quickBar, "", () => UseSlot(index), 12);
                s.button.rt.gameObject.name = "Slot " + def.id;
                s.button.rt.anchorMin = new Vector2(i / 6f, 0); s.button.rt.anchorMax = new Vector2((i + 1) / 6f, 1);
                s.button.rt.offsetMin = new Vector2(i == 0 ? 10 : 4, 8); s.button.rt.offsetMax = new Vector2(i == 5 ? -10 : -4, -24);
                s.group = s.button.rt.gameObject.AddComponent<CanvasGroup>();
                s.rim = s.button.rt.GetComponent<XgHoverRim>();
                var rt = s.button.rt;
                s.key = ui.Text(Rect("Key", rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(5, -15), new Vector2(24, -2)), (i + 1).ToString(), 11, XgDark.Dim, TextAlignmentOptions.TopLeft);
                s.count = ui.Text(Rect("Count", rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-48, -17), new Vector2(-5, -2)), "", 13, XgDark.Ink, TextAlignmentOptions.TopRight);
                s.count.textWrappingMode = TextWrappingModes.NoWrap;
                var tileRt = Rect("Tile", rt, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-15, -42), new Vector2(15, -12));
                s.tile = tileRt.gameObject.AddComponent<XgFrameGraphic>(); s.tile.raycastTarget = false;
                s.glyph = ui.Text(Rect("Glyph", tileRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 17, XgDark.Ink, TextAlignmentOptions.Center);
                s.glyph.textWrappingMode = TextWrappingModes.NoWrap;
                s.glyph.fontStyle = FontStyles.Bold;
                s.button.label.rectTransform.anchorMin = Vector2.zero; s.button.label.rectTransform.anchorMax = new Vector2(1, 0);
                s.button.label.rectTransform.offsetMin = new Vector2(2, 14); s.button.label.rectTransform.offsetMax = new Vector2(-2, 32);
                s.button.label.enableAutoSizing = true; s.button.label.fontSizeMin = 8; s.button.label.fontSizeMax = 12;
                var priceStrip = Rect("PriceStrip", rt, Vector2.zero, new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 14));
                Panel(priceStrip, new Color(0, 0, 0, .55f)).raycastTarget = false;
                s.price = ui.Text(Rect("Price", priceStrip, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 10, XgDark.Money, TextAlignmentOptions.Center);
                s.price.textWrappingMode = TextWrappingModes.NoWrap;
                s.veil = Rect("Cooldown", rt, Vector2.zero, new Vector2(1, 0), Vector2.zero, Vector2.zero);
                Panel(s.veil, new Color(0, 0, 0, .66f)).raycastTarget = false;
                s.cooldown = ui.Text(Rect("Seconds", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 16, XgDark.Ink, TextAlignmentOptions.Center);
                s.cooldown.textWrappingMode = TextWrappingModes.NoWrap;
                UiTip.Add(rt, () => SlotTip(def));
                slots.Add(s);
            }
        }

        string SlotTip(XgConsumableDef def)
        {
            string tip = "<b>" + T(def.name, def.nameEn) + "</b>\n" + T(def.note, def.noteEn) + "\n<color=#6F95A5>" + T(def.history, def.historyEn) + "</color>\n"
                + "<size=12><color=#6F95A5>" + T("道具页 ¥" + N(def.price, "0") + " 一份 " + def.pack + " 个", "Items page: ¥" + N(def.price, "0") + " for " + def.pack)
                + (def.cooldown > 0 ? T(" · 冷却 " + N(def.cooldown, "0") + " 秒", " · cooldown " + N(def.cooldown, "0") + " s") : "") + "</color></size>";
            if (Sim.ConsumableCount(def.id) <= 0) tip += "\n" + Hex(XgDark.Money) + T("没有了：点一下去道具页买", "None left: click to buy on the Items page") + "</color>";
            return tip;
        }

        /// <summary>Puts the train row at the bottom of the left column (above the quick bar when it is shown) and the curve card above it.</summary>
        void LayoutLeft(bool bar)
        {
            quickBarShown = bar;
            if (quickBar != null) quickBar.gameObject.SetActive(bar);
            float y = bar ? QuickBarHeight + RowGap : 0;
            trainRow.offsetMin = new Vector2(0, y); trainRow.offsetMax = new Vector2(0, y + TrainRowHeight);
            curveCard.offsetMin = new Vector2(0, y + TrainRowHeight + RowGap);
        }

        void RefreshQuickBar()
        {
            // The bar waits for the 道具 page (where the packs are sold), or for the first one the player owns.
            bool any = false;
            foreach (var d in XgConsumables.All) if (Sim.ConsumableCount(d.id) > 0) any = true;
            bool show = any || view != null && view.FeatureOpen("items");
            if (show != quickBarShown) LayoutLeft(show);
            if (!show) return;
            quickBarHead.text = T("消耗品 · 点一下用掉 · 键盘 1–6", "Consumables · click to use · keys 1–6");
            quickBarLink.text = T("去道具页补货 →", "Restock on the Items page →");
            foreach (var s in slots)
            {
                int n = Sim.ConsumableCount(s.def.id);
                double cd = Sim.ConsumableCooldown(s.def.id);
                bool ready = n > 0 && cd <= 0;
                s.group.alpha = n > 0 ? 1 : .6f;
                s.button.Set(T(s.def.name, s.def.nameEn), true, n > 0 ? XgDark.Button : XgDark.Disabled, n > 0 ? XgDark.Ink : XgDark.Muted);
                if (s.rim != null) { s.rim.rest = ready ? XgDark.Link : XgDark.Line; s.rim.Apply(); }
                s.count.text = "×" + n;
                s.count.color = n > 0 ? XgDark.Ink : XgDark.Bad;
                s.glyph.text = T(s.def.glyph, s.def.glyphEn);
                s.glyph.fontSize = Sim.English ? 12 : 17;
                s.glyph.color = n > 0 ? XgDark.Link : XgDark.Dim;
                s.tile.Style(n > 0 ? new Color32(15, 42, 42, 255) : Color.clear, ready ? XgDark.Link : XgDark.Line);
                s.price.transform.parent.gameObject.SetActive(n <= 0);
                s.price.text = "¥" + N(s.def.price, "0") + " / " + s.def.pack + T(" 个", "");
            }
            TickQuickBar(0);
        }

        /// <summary>Cooldown veils every frame, and the number keys 1–6.</summary>
        void TickQuickBar(float dt)
        {
            if (!quickBarShown) return;
            foreach (var s in slots)
            {
                double cd = Sim.ConsumableCooldown(s.def.id), total = Math.Max(1, s.def.cooldown);
                bool cooling = cd > 0 && Sim.ConsumableCount(s.def.id) > 0;
                var m = s.veil.anchorMax; m.y = cooling ? Mathf.Clamp01((float)(cd / total)) : 0; s.veil.anchorMax = m;
                s.cooldown.text = cooling ? N(Math.Ceiling(cd), "0") + "s" : "";
            }
            if (dt <= 0 || view == null || !view.Visible || view.Tab != "train" || AssessmentBlocksInput) return;
            var keyboard = Keyboard.current;
            if (keyboard == null || TypingInAField()) return;
            var keys = new[] { keyboard.digit1Key, keyboard.digit2Key, keyboard.digit3Key, keyboard.digit4Key, keyboard.digit5Key, keyboard.digit6Key };
            var pad = new[] { keyboard.numpad1Key, keyboard.numpad2Key, keyboard.numpad3Key, keyboard.numpad4Key, keyboard.numpad5Key, keyboard.numpad6Key };
            for (int i = 0; i < slots.Count; i++)
                if (keys[i].wasPressedThisFrame || pad[i].wasPressedThisFrame) UseSlot(i);
        }

        /// <summary>A text box has the keyboard (YY chat, a rename): the number keys are for typing then.</summary>
        static bool TypingInAField()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null && selected.GetComponent<TMP_InputField>() != null;
        }

        void UseSlot(int i)
        {
            if (i < 0 || i >= slots.Count) return;
            var s = slots[i];
            var def = s.def;
            if (Sim.ConsumableCount(def.id) <= 0)
            {
                Fx.Play(XgJuice.Sfx.Id.Click);
                Fx.Float(Fx.At(s.button.rt, new Vector2(0, 40)), T("没了 · 去道具页买", "None left · buy on the Items page"), XgDark.Money, 15, 30, .9f, 1.1f);
                view.ShowTab("items");
                return;
            }
            string dataset = Sim.Run(Track).dataset;
            double noiseBefore = Sim.Noise(dataset) + Sim.DataNoise(dataset);
            string why = Sim.ConsumableBlocker(def.id, dataset);
            if (why != null || !Sim.UseConsumable(def.id, Host, dataset))
            {
                Fx.Knock(s.button.rt, .04f, new Vector2(8, 0));
                Fx.Play(XgJuice.Sfx.Id.Thud, 1, .4f);
                if (why != null) Fx.Float(Fx.At(s.button.rt, new Vector2(0, 40)), why, XgDark.Bad, 14, 28, .9f, 1.1f);
                return;
            }
            string effect;
            switch (def.id)
            {
                case XgConsumables.Offpeak: effect = T("谷电 · " + XgSim.OffpeakRounds + " 轮半价", "Off-peak · " + XgSim.OffpeakRounds + " rounds at half price"); break;
                case XgConsumables.Paste: effect = T("不烧卡 " + N(XgSim.PasteSeconds, "0") + " 秒", "No burn-out for " + N(XgSim.PasteSeconds, "0") + " s"); break;
                case XgConsumables.RedBull: effect = T("连击不掉 " + N(XgSim.RedBullSeconds, "0") + " 秒", "Combo holds for " + N(XgSim.RedBullSeconds, "0") + " s"); break;
                case XgConsumables.Checkpoint: effect = T("回滚待命 ×" + Sim.S.rollbackGuards, "Rollback ready ×" + Sim.S.rollbackGuards); break;
                case XgConsumables.Sgdr: effect = T("上限 +8% · " + XgSim.SgdrRounds + " 轮", "Ceiling +8% · " + XgSim.SgdrRounds + " rounds"); break;
                default: effect = T("清掉 " + N(noiseBefore - Sim.Noise(dataset) - Sim.DataNoise(dataset), "0") + " 条脏标注", "Removed " + N(noiseBefore - Sim.Noise(dataset) - Sim.DataNoise(dataset), "0") + " dirty labels"); break;
            }
            Fx.Knock(s.button.rt, .1f);
            Fx.Burst(Fx.At(s.button.rt), 12, XgDark.Link, XgJuice.Shape.Spark, 220);
            Fx.Float(Fx.At(s.button.rt, new Vector2(0, 40)), effect, XgDark.Link, 16, 36, .9f, 1.2f);
            Fx.Play(XgJuice.Sfx.Id.Unlock, 1.1f, .6f);
            view.Refresh(true);
        }

        // ───────────── running effects ─────────────

        void BuildBuffRow(RectTransform row)
        {
            buffRow = row;
            for (int i = 0; i < BuffChipCount; i++) buffChips.Add(MakeChip(row, "Buff" + i, 12));
            noBuffs = ui.Text(Rect("None", row, Vector2.zero, Vector2.one, new Vector2(0, 0), new Vector2(0, 0)), "", 11, XgDark.Dim, TextAlignmentOptions.MidlineLeft);
            noBuffs.textWrappingMode = TextWrappingModes.NoWrap;
        }

        void RefreshBuffs()
        {
            var shown = new List<(string text, Color color, string tip)>();
            var link = XgDark.Link;
            if (Sim.OffpeakActive) shown.Add((T("谷电 " + Sim.S.offpeakRounds + " 轮", "Off-peak " + Sim.S.offpeakRounds + " rds"), link, "offpeak"));
            if (Sim.PasteActive) shown.Add((T("硅脂 " + N(Math.Ceiling(Sim.S.pasteSeconds), "0") + "s", "Paste " + N(Math.Ceiling(Sim.S.pasteSeconds), "0") + "s"), link, "paste"));
            if (Sim.RedBullActive) shown.Add((T("红牛 " + N(Math.Ceiling(Sim.S.redbullSeconds), "0") + "s", "Red Bull " + N(Math.Ceiling(Sim.S.redbullSeconds), "0") + "s"), XgDark.Money, "redbull"));
            if (Sim.S.rollbackGuards > 0) shown.Add((T("回滚 ×" + Sim.S.rollbackGuards, "Rollback ×" + Sim.S.rollbackGuards), link, "ckpt"));
            if (Sim.SgdrActive) shown.Add((T("重启 " + Sim.S.sgdrRounds + " 轮", "Restart " + Sim.S.sgdrRounds + " rds"), XgDark.Params, "sgdr"));
            noBuffs.text = shown.Count == 0 ? T("没有生效中的消耗品", "No consumable running") : "";
            float x = 0, room = buffRow.rect.width > 10 ? buffRow.rect.width : 290;
            for (int i = 0; i < buffChips.Count; i++)
            {
                var c = buffChips[i];
                bool on = i < shown.Count;
                c.rt.gameObject.SetActive(on);
                if (!on) continue;
                c.text.text = shown[i].text; c.text.color = shown[i].color;
                c.frame.Style(new Color(shown[i].color.r, shown[i].color.g, shown[i].color.b, .1f), shown[i].color);
                x += PlaceChip(c, x, 2, 20) + 5;
                if (x > room) c.rt.gameObject.SetActive(false);
            }
        }
    }
}
