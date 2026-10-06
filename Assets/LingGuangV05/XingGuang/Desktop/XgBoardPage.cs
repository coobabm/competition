using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 大脑: three views, switched at the top right: the concept board (here), the 皮层拓扑 map (XgBoardPage.Cortex)
    /// and the 接法图鉴 of topologies (XgBoardPage.Atlas). The concept board (design v1.1 §4): Four regions of cells, coloured by what each concept leans toward
    /// (是 green, 否 red), brighter when stronger; superposed cells get a purple rim, the SI's seed is gold. The right
    /// column is the 图鉴 of phenomena seen so far.
    /// </summary>
    public sealed partial class XgBoardPage : XgPage
    {
        public const int CellsShown = 160;
        public const int Columns = 16;
        static readonly string[] Regions = { "vision", "sequence", "logic", "tone" };

        sealed class RegionView
        {
            public RectTransform grid;
            public TMP_Text title, known;
            public readonly List<Image> cells = new List<Image>();
            public readonly List<Outline> rims = new List<Outline>();
        }

        readonly RegionView[] regions = new RegionView[4];
        TMP_Text header, atlas;
        long shownCards = -1;
        int shownPhenomena = -1;

        public override void Build(RectTransform area)
        {
            var card = ui.Card(area, "board", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root = card;
            header = ui.Text(Strip("Header", card, 8, 40, 16, 330), "", 16, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            header.enableAutoSizing = true; header.fontSizeMin = 11; header.fontSizeMax = 16;
            conceptView = Rect("Concepts", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var left = Rect("Regions", conceptView, Vector2.zero, new Vector2(.7f, 1), new Vector2(12, 12), new Vector2(-6, -52));
            for (int i = 0; i < 4; i++)
            {
                float x0 = (i % 2) * .5f, y1 = 1 - (i / 2) * .5f;
                var box = Rect(Regions[i], left, new Vector2(x0, y1 - .5f), new Vector2(x0 + .5f, y1), new Vector2(4, 4), new Vector2(-4, -4));
                Panel(box, XgPalette.Page);
                var view = new RegionView();
                view.title = ui.Text(Strip("Title", box, 4, 22, 10, 10), "", 14, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
                view.title.fontStyle = FontStyles.Bold;
                view.grid = Rect("Grid", box, new Vector2(0, .34f), Vector2.one, new Vector2(10, 0), new Vector2(-10, -30));
                view.known = ui.Text(Rect("Known", box, Vector2.zero, new Vector2(1, .34f), new Vector2(10, 6), new Vector2(-10, -2)), "", 12, XgPalette.Muted, TextAlignmentOptions.TopLeft);
                view.known.enableAutoSizing = true; view.known.fontSizeMin = 9; view.known.fontSizeMax = 12;
                regions[i] = view;
            }
            var right = Rect("Atlas", conceptView, new Vector2(.7f, 0), Vector2.one, new Vector2(6, 12), new Vector2(-12, -52));
            Panel(right, XgPalette.Page);
            atlas = ui.Text(Rect("Text", right, Vector2.zero, Vector2.one, new Vector2(12, 10), new Vector2(-12, -10)), "", 13, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            atlas.enableAutoSizing = true; atlas.fontSizeMin = 9; atlas.fontSizeMax = 13;
            BuildCortex(card);
            BuildTopologies(card);
            BuildModeTabs(card);
        }

        public override void Shown() { shownCards = -1; Refresh(); if (mode == Mode.Cortex) VoiceCortex(); }

        public override void Tick(float dt)
        {
            if (mode == Mode.Cortex) TickCortex(dt);
            else if (mode == Mode.Atlas) TickTopologies(dt);
        }

        public override void Refresh()
        {
            if (header == null || Sim == null) return;
            RefreshModeTabs();
            if (mode == Mode.Cortex) { RefreshCortex(); return; }
            if (mode == Mode.Atlas) { RefreshTopologies(); return; }
            var board = Sim.Board;
            var k = Sim.Knobs(Sim.Selected);
            header.text = Lang.T("大脑 · 概念盘") + "  <size=13><color=#68748C>"
                + Lang.T("已训练 ") + N(board.S.cards, "0") + Lang.T(" 张卡 · 每个板块 ") + k.Cells
                + Lang.T(" 格 · 叠格 ") + board.S.superposed + "</color></size>"
                + (OneBrain() ? "  <size=13><color=#E0A800>" + Lang.T("全皮层统一拓扑 · 同步点亮") + "</color></size>" : "");
            int seen = Sim.S.phenomena != null ? Sim.S.phenomena.seen.Count : 0;
            if (board.S.cards == shownCards && seen == shownPhenomena) return;
            shownCards = board.S.cards; shownPhenomena = seen;
            for (int i = 0; i < 4; i++) DrawRegion(regions[i], Regions[i], RegionTitle(Regions[i]), board);
            DrawAtlas();
        }

        /// <summary>The seeing and reading regions wired the same way (the Transformer of stage 6): one brain lit at once.</summary>
        bool OneBrain()
        {
            var seeing = Sim.RegionWiring("vision"); var reading = Sim.RegionWiring("sequence");
            return Sim.S.stage >= 6 && seeing != null && reading != null && seeing.id == "transformer" && reading.id == "transformer";
        }

        /// <summary>灵光's one brain in regions like a cortex: the region, the part of a human brain it plays, and how it is wired now.</summary>
        string RegionTitle(string region)
        {
            var wired = Sim.RegionWiring(region);
            return T(XgSim.RegionName(region, false), XgSim.RegionName(region, true)) + " <size=11><color=#68748C>" + T(XgSim.RegionLikeness(region, false), XgSim.RegionLikeness(region, true))
                + (wired != null ? Lang.T(" · 拓扑 ") + T(wired.name, wired.nameEn) : "") + "</color></size>";
        }

        void DrawRegion(RegionView view, string region, string name, XgBoard board)
        {
            var concepts = new List<XgConcept>(board.Concepts(region));
            concepts.Sort((a, b) => b.layer != a.layer ? b.layer.CompareTo(a.layer) : (Math.Abs(b.w) * b.s).CompareTo(Math.Abs(a.w) * a.s));
            view.title.text = name + "  <size=12><color=#68748C>" + concepts.Count + Lang.T(" 个概念 · 最高 ") + board.MaxLayer(region) + T(" 层", "") + "</color></size>";
            int count = Math.Min(CellsShown, concepts.Count);
            var size = view.grid.rect.size;
            float cell = Mathf.Max(6, Mathf.Min(size.x / Columns, size.y / Math.Max(1, (CellsShown + Columns - 1) / Columns)) - 2);
            while (view.cells.Count < count)
            {
                var rt = Rect("Cell", view.grid, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
                var img = Panel(rt, Color.white); img.raycastTarget = false;
                var rim = rt.gameObject.AddComponent<Outline>(); rim.effectDistance = new Vector2(1.5f, -1.5f);
                view.cells.Add(img); view.rims.Add(rim);
            }
            for (int i = 0; i < view.cells.Count; i++)
            {
                bool on = i < count;
                if (view.cells[i].gameObject.activeSelf != on) view.cells[i].gameObject.SetActive(on);
                if (!on) continue;
                var c = concepts[i];
                var rt = view.cells[i].rectTransform;
                int col = i % Columns, row = i / Columns;
                rt.offsetMin = new Vector2(col * (cell + 2), -(row + 1) * (cell + 2));
                rt.offsetMax = new Vector2(col * (cell + 2) + cell, -row * (cell + 2) - 2);
                float strength = Mathf.Clamp01((float)(Math.Abs(c.w) / 2));
                var lean = c.w >= 0 ? XgPalette.Good : XgPalette.Bad;
                view.cells[i].color = c.seed ? XgPalette.Gold : Color.Lerp(XgPalette.Line, lean, .25f + .75f * strength);
                view.rims[i].enabled = c.alt.Length > 0 || c.seed;
                view.rims[i].effectColor = c.seed ? new Color32(140, 90, 0, 255) : new Color32(150, 80, 220, 255);
            }
            var sb = new StringBuilder(Lang.T("认得最牢的："));
            var strongest = new List<XgConcept>(concepts);
            strongest.Sort((a, b) => (Math.Abs(b.w) * b.s).CompareTo(Math.Abs(a.w) * a.s));
            for (int i = 0; i < Math.Min(6, strongest.Count); i++)
            {
                var c = strongest[i];
                sb.Append(i == 0 ? "" : Lang.T("、")).Append(c.seed ? "？" : c.key.Replace("+", "·"))
                  .Append(c.w >= 0 ? "→" + T("是", "yes") : "→" + T("否", "no"));
                if (c.alt.Length > 0) sb.Append(Lang.T("（和 ")).Append(c.alt.Replace("+", "·")).Append(Lang.T(" 混在一格）"));
            }
            if (strongest.Count == 0) sb.Append(Lang.T("还是空的"));
            view.known.text = sb.ToString();
        }

        void DrawAtlas()
        {
            var sb = new StringBuilder("<b>" + Lang.T("现象图鉴") + "</b>\n");
            int seen = 0;
            foreach (var p in XgPhenomena.All)
            {
                bool on = Sim.PhenomenonSeen(p.id);
                if (on) seen++;
                sb.Append("\n").Append(on ? "<color=#2F9E44>● </color><b>" + T(p.name, p.nameEn) + "</b>\n<size=11><color=#68748C>" + T(p.why, p.whyEn) + "</color></size>\n"
                                          : "<color=#B4BCCD>○ ？？？</color>  <size=11><color=#B4BCCD>" + T("阶段 ", "stage ") + p.stage + "</color></size>\n");
            }
            sb.Insert(sb.ToString().IndexOf('\n'), "  <size=12><color=#68748C>" + seen + "/" + XgPhenomena.All.Length + "</color></size>");
            atlas.text = sb.ToString();
        }
    }
}
