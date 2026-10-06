using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 接法图鉴: every architecture as a tile, in the order history found them, drawn wired and animated the way the
    /// cortex map draws a region. Locked ones show only their year until bought in 科技.
    /// </summary>
    public sealed partial class XgBoardPage
    {
        public const int TopologyColumns = 5;

        sealed class TopologyTile
        {
            public string id;
            public RectTransform root;
            public XgCortexGraphic glyph;
            public TMP_Text name, topo, wire;
            public bool open;
        }

        RectTransform topologyView;
        readonly List<TopologyTile> topologyTiles = new List<TopologyTile>();
        float topologyTimer;

        void BuildTopologies(RectTransform card)
        {
            topologyView = Rect("Topologies", card, Vector2.zero, Vector2.one, new Vector2(10, 10), new Vector2(-10, -50));
            var archs = new List<XgArch>(XgCatalog.Archs);
            archs.Sort((a, b) => a.year != b.year ? a.year.CompareTo(b.year) : string.CompareOrdinal(a.id, b.id));
            int rows = (archs.Count + TopologyColumns - 1) / TopologyColumns;
            for (int i = 0; i < archs.Count; i++)
            {
                int col = i % TopologyColumns, row = i / TopologyColumns;
                var tile = new TopologyTile { id = archs[i].id };
                tile.root = Rect(tile.id, topologyView, new Vector2(col / (float)TopologyColumns, 1 - (row + 1f) / rows), new Vector2((col + 1f) / TopologyColumns, 1 - row / (float)rows), new Vector2(3, 3), new Vector2(-3, -3));
                Panel(tile.root, XgCortexGraphic.N.BgBottom);
                var glyph = Rect("Glyph", tile.root, new Vector2(0, .47f), Vector2.one, new Vector2(4, 0), new Vector2(-4, -4));
                tile.glyph = glyph.gameObject.AddComponent<XgCortexGraphic>();
                tile.glyph.raycastTarget = false;
                tile.glyph.GlyphArch = tile.id;
                tile.glyph.Font = ui.font;
                tile.glyph.Tokens = new[] { "这", "本", "书", "我", "看", "过" };
                tile.glyph.Source = new[] { "那", "只", "猫", "坐", "在", "垫" };
                tile.name = ui.Text(Rect("Name", tile.root, new Vector2(0, .35f), new Vector2(1, .47f), new Vector2(8, 0), new Vector2(-8, 0)), "", 14, XgCortexGraphic.N.Text, TextAlignmentOptions.MidlineLeft);
                tile.topo = ui.Text(Rect("Topo", tile.root, new Vector2(0, .25f), new Vector2(1, .35f), new Vector2(8, 0), new Vector2(-8, 0)), "", 12, new Color32(143, 179, 255, 255), TextAlignmentOptions.MidlineLeft);
                tile.topo.enableAutoSizing = true; tile.topo.fontSizeMin = 8; tile.topo.fontSizeMax = 12;
                tile.wire = ui.Text(Rect("Wire", tile.root, Vector2.zero, new Vector2(1, .25f), new Vector2(8, 4), new Vector2(-8, 0)), "", 11, XgCortexGraphic.N.TextMuted, TextAlignmentOptions.TopLeft);
                tile.wire.enableAutoSizing = true; tile.wire.fontSizeMin = 7; tile.wire.fontSizeMax = 11;
                string id = tile.id;
                UiTip.Add(tile.root, () => TopologyTip(id));
                topologyTiles.Add(tile);
            }
            topologyView.gameObject.SetActive(false);
        }

        bool TopologyOpen(string id)
        {
            if (Sim.Has(id)) return true;
            foreach (var run in Sim.Runs) if (run.arch == id) return true;
            return false;
        }

        string TopologyTip(string id)
        {
            var a = XgCatalog.Arch(id);
            if (a == null || Sim == null) return "";
            if (!TopologyOpen(id)) return Lang.T("还没解锁。去「科技」里找它（") + a.year + Lang.T(" 年）。");
            return T(a.name, a.nameEn) + "【" + Lang.T(a.topo) + "】\n" + T(a.wire, a.wireEn) + "\n" + T(a.note, a.noteEn);
        }

        void RefreshTopologies()
        {
            int open = 0;
            foreach (var tile in topologyTiles)
            {
                var a = XgCatalog.Arch(tile.id);
                tile.open = TopologyOpen(tile.id);
                if (tile.open) open++;
                if (tile.glyph.gameObject.activeSelf != tile.open) tile.glyph.gameObject.SetActive(tile.open);
                if (tile.open)
                {
                    tile.name.text = T(a.name, a.nameEn) + "  <size=11><color=#8494C8>" + a.year + "</color></size>";
                    tile.topo.text = "【" + Lang.T(a.topo) + "】";
                    tile.wire.text = T(a.wire, a.wireEn);
                    tile.name.color = XgCortexGraphic.N.Text;
                }
                else
                {
                    tile.name.text = "？？？  <size=11>" + a.year + "</size>";
                    tile.topo.text = "";
                    tile.wire.text = Lang.T("在「科技」里解锁");
                    tile.name.color = new Color32(90, 104, 150, 255);
                }
            }
            header.text = Lang.T("大脑 · 接法图鉴") + "  <size=13><color=#68748C>" + Lang.T("已解锁 ") + open + " / " + topologyTiles.Count
                + Lang.T(" · 每一种都是给某个区接线的办法") + "</color></size>";
        }

        void TickTopologies(float dt)
        {
            topologyTimer -= dt;
            bool animate = topologyTimer <= 0;
            if (animate) topologyTimer = Sim != null && Sim.S.reduceFx ? .1f : 0;
            foreach (var tile in topologyTiles)
            {
                if (!tile.open) continue;
                tile.glyph.Reduced = Sim != null && Sim.S.reduceFx;
                if (animate) tile.glyph.Animate();
                tile.glyph.SyncLabels();
            }
        }
    }
}
