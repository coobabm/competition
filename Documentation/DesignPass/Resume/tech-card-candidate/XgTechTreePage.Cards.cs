using System;
using System.Collections.Generic;
using DG.Tweening;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    // 4:010: display-only four-layer cards. The simulation still owns every purchase.
    public sealed partial class XgTechTreePage
    {
        const float TechCardWidth = 296, TechCardMinimumHeight = 174;
        sealed class TechCard
        {
            public TMP_Text summary, effect;
            public XgBtn details;
            public RectTransform divider;
        }
        readonly Dictionary<XgTechNodeView, TechCard> techCards = new Dictionary<XgTechNodeView, TechCard>();
        bool cardLayoutDirty = true, detailsOpen;
        XgBtn detailsClose;

        void MakeTechCard(XgTechEntry entry)
        {
            var rt = Rect("Node " + entry.id, nodeLayer, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            rt.sizeDelta = new Vector2(TechCardWidth, TechCardMinimumHeight); rt.anchoredPosition = position[entry];
            var node = rt.gameObject.AddComponent<XgTechNodeView>();
            node.entry = entry; node.page = this; node.home = position[entry];
            node.square = Rect("Square", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            node.fill = Panel(node.square, XgDark.Card); node.fill.raycastTarget = true;
            var outline = node.square.gameObject.AddComponent<Outline>(); outline.effectDistance = new Vector2(1, -1);
            node.frame = Panel(Rect("Border", node.square, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Color.clear);
            node.frame.enabled = false; node.frame.raycastTarget = false;
            node.ring = Panel(Rect("HoverRing", node.square, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Color.clear);
            node.ring.raycastTarget = false; node.ring.enabled = false;
            var icon = TopLeft("Icon", node.square, 12, 12, 22, 22);
            icon.gameObject.AddComponent<CanvasRenderer>(); node.icon = icon.gameObject.AddComponent<XgTechIcon>(); node.icon.raycastTarget = false;
            node.glyph = ui.Text(TopLeft("Glyph", node.square, 8, 12, 30, 22), "", 11, XgDark.Ink, TextAlignmentOptions.Center);
            node.glyph.enableAutoSizing = true; node.glyph.fontSizeMin = 8; node.glyph.fontSizeMax = 11;
            node.label = ui.Text(Strip("Name", node.square, 10, 42, 44, 12), "", 16, XgDark.Ink, TextAlignmentOptions.TopLeft);
            node.label.fontStyle = FontStyles.Bold;
            var card = new TechCard();
            card.summary = ui.Text(Strip("Summary", node.square, 56, 22, 12, 12), "", 14, XgDark.Muted, TextAlignmentOptions.TopLeft);
            card.effect = ui.Text(Strip("Effect", node.square, 84, 40, 12, 12), "", 14, XgDark.Data, TextAlignmentOptions.TopLeft);
            card.divider = Strip("DetailDivider", node.square, 132, 1, 12, 12); Panel(card.divider, XgDark.Line).raycastTarget = false;
            card.details = ui.Button(node.square, "", () => OpenTechDetails(node), 14); card.details.rt.gameObject.name = "Details";
            var badge = TopLeft("Level", node.square, 100, 140, 58, 26);
            node.badge = ui.Text(Rect("Text", badge, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 12, XgDark.Muted, TextAlignmentOptions.Center);
            node.cost = ui.Text(TopLeft("Cost", node.square, 166, 140, 118, 26), "", 14, XgDark.Money, TextAlignmentOptions.MidlineRight);
            node.cost.fontStyle = FontStyles.Bold;
            techCards.Add(node, card); views.Add(node); viewOf[entry] = node;
            rt.gameObject.SetActive(false);
        }

        void PaintTechCard(XgTechNodeView node)
        {
            var card = techCards[node];
            node.label.text = XgTechCardCopy.Name(node.entry, Sim);
            card.summary.text = XgTechCardCopy.Summary(node.entry, Sim);
            card.effect.text = XgTechCardCopy.Effect(node.entry, Sim);
            card.details.Set(T("详情 ›", "Details ›"), true);
            float y = 10;
            foreach (var text in new[] { node.label, card.summary, card.effect })
            {
                float left = text == node.label ? 44 : 12;
                float height = GrowthTextHeight(text, TechCardWidth - left - 12, 22);
                GrowthStrip(text.rectTransform, y, height, left, 12); y += height + 7;
            }
            y = Mathf.Max(y, TechCardMinimumHeight - 45);
            GrowthStrip(card.divider, y, 1, 12, 12); y += 9;
            GrowthStrip(card.details.rt, y, 28, 12, TechCardWidth - 98);
            GrowthStrip((RectTransform)node.badge.transform.parent, y, 28, 103, TechCardWidth - 161);
            float costHeight = GrowthTextHeight(node.cost, 118, 28);
            GrowthStrip(node.cost.rectTransform, y, costHeight, 166, 12);
            float heightNeeded = y + Mathf.Max(28, costHeight) + 9;
            var rect = (RectTransform)node.transform;
            if (Mathf.Abs(rect.rect.height - heightNeeded) > .1f)
            { rect.sizeDelta = new Vector2(TechCardWidth, heightNeeded); cardLayoutDirty = true; }
        }

        float TechCardHeight(XgTechEntry entry)
            => viewOf.TryGetValue(entry, out var node) ? ((RectTransform)node.transform).rect.height : TechCardMinimumHeight;

        void LayoutTechCards()
        {
            if (!cardLayoutDirty) return;
            cardLayoutDirty = false; Layout();
            foreach (RectTransform stripe in bandLayer)
            {
                string band = stripe.name.Substring("LaneBand ".Length);
                stripe.offsetMin = new Vector2(0, -bandTop[band] - bandHeight[band] - LaneGap * .5f);
                stripe.offsetMax = new Vector2(0, -bandTop[band] + LaneGap * .5f);
            }
            foreach (var title in laneTitles)
            {
                var parts = title.name.Split(' ');
                title.anchoredPosition = new Vector2(title.anchoredPosition.x, -bandTop[parts[1]] - 17);
            }
            foreach (var node in views)
            { node.home = position[node.entry]; ((RectTransform)node.transform).anchoredPosition = node.home; }
            foreach (var link in links)
            {
                var delta = link.to.home - link.from.home; var rect = link.image.rectTransform;
                rect.anchoredPosition = link.from.home;
                rect.sizeDelta = new Vector2(delta.magnitude, link.need ? 2 : 3);
                rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
            content.sizeDelta = new Vector2(XgSim.AbilityCount * StageWidth + 40, treeHeight);
            Pan.Clamp();
        }

        public RectTransform TechDetailsTarget(string id)
        {
            var node = views.Find(v => v.Covers(id) && v.gameObject.activeSelf);
            return node != null ? techCards[node].details.rt : null;
        }

        public void OpenTechDetails(XgTechNodeView node)
        {
            if (!built || node == null || !node.gameObject.activeInHierarchy) return;
            if (hovered != null)
            { hovered.square.DOKill(); hovered.square.localScale = Vector3.one; hovered.square.localRotation = Quaternion.identity; }
            detailsOpen = true; hovered = node; tipExitAt = -1;
            ShowTooltip(node); PaintLinks();
        }

        void BuildTechDetailsClose()
        {
            detailsClose = ui.Button(tooltip, "", () => { hovered = null; HideTooltip(); PaintLinks(); }, 14);
            detailsClose.rt.gameObject.name = "DetailsClose";
            GrowthStrip(detailsClose.rt, 4, 28, 12, 12);
            detailsClose.Show(false);
        }

        void RefreshTechDetails()
        {
            detailsClose.Show(detailsOpen);
            detailsClose.Set(T("关闭详情 ×", "Close details ×"), true);
            tipScroll.viewport.offsetMax = new Vector2(-12, detailsOpen ? -36 : 0);
            tipScroll.verticalScrollbar.GetComponent<RectTransform>().offsetMax = new Vector2(-2, detailsOpen ? -36 : -4);
        }

        // A hover popup must leave the card's independent Details action reachable,
        // even when the window is too short to fit the popup above the whole card.
        Rect TechTooltipBounds(XgTechNodeView node, Rect bounds, float width)
        {
            if (detailsOpen || !techCards.TryGetValue(node, out var card)) return bounds;
            var corners = new Vector3[4]; card.details.rt.GetWorldCorners(corners);
            Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
            foreach (var corner in corners)
            { var point = (Vector2)root.InverseTransformPoint(corner); min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
            var action = UnityEngine.Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            if (!bounds.Overlaps(action)) return bounds;
            float nodeX = root.InverseTransformPoint(node.square.TransformPoint(node.square.rect.center)).x;
            float centre = Mathf.Clamp(nodeX, bounds.xMin + width * .5f + 4, bounds.xMax - width * .5f - 4);
            if (centre + width * .5f <= action.xMin || centre - width * .5f >= action.xMax) return bounds;
            float above = bounds.yMax - action.yMax - 6, below = action.yMin - bounds.yMin - 6;
            if (above >= below) bounds.yMin = Mathf.Max(bounds.yMin, action.yMax + 6);
            else bounds.yMax = Mathf.Min(bounds.yMax, action.yMin - 6);
            return bounds;
        }
    }
}
