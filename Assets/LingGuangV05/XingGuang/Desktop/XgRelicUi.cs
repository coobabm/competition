using System.Text;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Shared looks of the 稀有道具: rarity colours, a tile, and the hover text (name, rarity, flavour, effect, source, pair).</summary>
    public static class XgRelicUi
    {
        public static readonly Color Rare = new Color32(93, 168, 232, 255), Epic = new Color32(169, 155, 242, 255), Legendary = new Color32(255, 190, 40, 255);

        public static Color RarityColor(XgRarity r) => r == XgRarity.Legendary ? Legendary : r == XgRarity.Epic ? Epic : Rare;

        /// <summary>Paints a tile: a framed square with the relic's mark. <paramref name="dim"/> shows it as not yet found.</summary>
        public static void PaintTile(XgFrameGraphic frame, TMP_Text glyph, XgRelic r, bool dim = false)
        {
            var drawn = (r == null || dim) ? null : XgIcons.Get("relic", r.id);
            ShowIcon(frame, drawn);
            if (r == null || dim)
            {
                frame.Style(new Color32(10, 17, 25, 255), XgDark.Dim);
                glyph.text = dim ? "?" : "";
                glyph.color = XgDark.Dim;
                return;
            }
            var edge = RarityColor(r.rarity);
            frame.Style(new Color(edge.r * .22f, edge.g * .22f, edge.b * .22f, 1f), edge, false, 2);
            glyph.text = drawn != null ? "" : r.glyph; // the drawn object (Resources/LingGuangV05/Icons/relic) replaces the letter mark
            glyph.color = edge;
        }

        /// <summary>Shows the drawn object of a relic inside its tile (a child "RelicIcon" made on first use), or hides it when there is none.</summary>
        static void ShowIcon(XgFrameGraphic frame, Sprite sprite)
        {
            if (frame == null) return;
            var child = frame.transform.Find("RelicIcon");
            if (sprite == null)
            {
                if (child != null) child.gameObject.SetActive(false);
                return;
            }
            if (child == null)
            {
                var go = new GameObject("RelicIcon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(frame.transform, false);
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(2, 2); rt.offsetMax = new Vector2(-2, -2);
                go.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                child = rt;
            }
            child.gameObject.SetActive(true);
            var image = child.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite; image.color = Color.white; image.preserveAspect = true;
        }

        /// <summary>The hover text of a relic. <paramref name="sim"/> says which pairs are on and whether it is equipped.</summary>
        public static string Tip(XgSim sim, XgRelic r)
        {
            bool en = sim.English;
            var sb = new StringBuilder();
            string rare = Hex(RarityColor(r.rarity)) + XgRelics.RarityName(r.rarity, en) + "</color>";
            sb.Append("<b>").Append(XgRelics.Name(r, en)).Append("</b>  ").Append(rare);
            if (sim.RelicEquipped(r.id)) sb.Append("  ").Append(Hex(XgDark.Good)).Append(T("已装备", "Equipped")).Append("</color>");
            else if (sim.RelicOwned(r.id)) sb.Append("  ").Append(Hex(XgDark.Muted)).Append(T("在背包里", "In the backpack")).Append("</color>");
            sb.Append("\n").Append(Hex(XgDark.Muted)).Append(XgRelics.Flavor(r, en)).Append("</color>");
            sb.Append("\n").Append(Hex(XgDark.Money)).Append(T("效果：", "Effect: ")).Append(XgRelics.Effect(r, en).Replace("灵光", sim.AiDisplayName)).Append("</color>");
            sb.Append("\n").Append(Hex(XgDark.Muted)).Append(T("来源：", "From: ")).Append(XgRelics.Source(r, en).Replace("灵光", sim.AiDisplayName)).Append("</color>");
            var pair = XgRelics.PairOf(r.id);
            if (pair != null)
            {
                var partner = XgRelics.Get(XgRelics.Partner(pair, r.id));
                bool on = sim.RelicEquipped(pair.a) && sim.RelicEquipped(pair.b);
                sb.Append("\n").Append(Hex(on ? XgDark.Good : XgDark.Link)).Append(T("联动「", "Pair '")).Append(en ? pair.en : pair.zh).Append(T("」（搭 ", "' (with ")).Append(XgRelics.Name(partner, en)).Append(T("）：", "): "))
                    .Append(en ? pair.textEn : pair.textZh).Append(on ? T("  已生效", "  active") : "").Append("</color>");
            }
            return sb.ToString();
        }

        /// <summary>The tip for an empty slot.</summary>
        public static string EmptyTip(XgSim sim) => sim.English
            ? "<b>Empty slot</b>\nFour rare items can be equipped at once. Open the backpack to equip or swap any of them, any time."
            : "<b>空位</b>\n稀有道具最多同时装备 4 件。打开背包，随时装备或互换。";
    }
}
