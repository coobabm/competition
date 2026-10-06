using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 大脑: the concept board (design v1.1 §4). Four regions of cells, coloured by what each concept leans toward
    /// (是 green, 否 red), brighter when stronger; superposed cells get a purple rim, the SI's seed is gold. The right
    /// column is the 图鉴 of phenomena seen so far.
    /// </summary>
    public sealed class XgBoardPage : XgPage
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
            header = ui.Text(Strip("Header", card, 8, 40, 16, 16), "", 16, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            var left = Rect("Regions", card, Vector2.zero, new Vector2(.7f, 1), new Vector2(12, 12), new Vector2(-6, -52));
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
            var right = Rect("Atlas", card, new Vector2(.7f, 0), Vector2.one, new Vector2(6, 12), new Vector2(-12, -52));
            Panel(right, XgPalette.Page);
            atlas = ui.Text(Rect("Text", right, Vector2.zero, Vector2.one, new Vector2(12, 10), new Vector2(-12, -10)), "", 13, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            atlas.enableAutoSizing = true; atlas.fontSizeMin = 9; atlas.fontSizeMax = 13;
        }

        public override void Shown() { shownCards = -1; Refresh(); }

        public override void Refresh()
        {
            if (header == null || Sim == null) return;
            var board = Sim.Board;
            var k = Sim.Knobs(Sim.Selected);
            header.text = T("大脑 · 概念盘", "Brain · concept board") + "  <size=13><color=#68748C>"
                + T("已训练 ", "Trained ") + N(board.S.cards, "0") + T(" 张卡 · 每个板块 ", " cards · cells per region ") + k.Cells
                + T(" 格 · 叠格 ", " · superposed ") + board.S.superposed + "</color></size>";
            int seen = Sim.S.phenomena != null ? Sim.S.phenomena.seen.Count : 0;
            if (board.S.cards == shownCards && seen == shownPhenomena) return;
            shownCards = board.S.cards; shownPhenomena = seen;
            for (int i = 0; i < 4; i++) DrawRegion(regions[i], Regions[i], RegionTitle(Regions[i]), board);
            DrawAtlas();
        }

        /// <summary>灵光's one brain in regions like a cortex: the region, the part of a human brain it plays, and how it is wired now.</summary>
        string RegionTitle(string region)
        {
            var wired = Sim.RegionWiring(region);
            return T(XgSim.RegionName(region, false), XgSim.RegionName(region, true)) + " <size=11><color=#68748C>" + T(XgSim.RegionLikeness(region, false), XgSim.RegionLikeness(region, true))
                + (wired != null ? T(" · 接法 ", " · wired as ") + T(wired.name, wired.nameEn) : "") + "</color></size>";
        }

        void DrawRegion(RegionView view, string region, string name, XgBoard board)
        {
            var concepts = new List<XgConcept>(board.Concepts(region));
            concepts.Sort((a, b) => b.layer != a.layer ? b.layer.CompareTo(a.layer) : (Math.Abs(b.w) * b.s).CompareTo(Math.Abs(a.w) * a.s));
            view.title.text = name + "  <size=12><color=#68748C>" + concepts.Count + T(" 个概念 · 最高 ", " concepts · top layer ") + board.MaxLayer(region) + T(" 层", "") + "</color></size>";
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
            var sb = new StringBuilder(T("认得最牢的：", "Strongest: "));
            var strongest = new List<XgConcept>(concepts);
            strongest.Sort((a, b) => (Math.Abs(b.w) * b.s).CompareTo(Math.Abs(a.w) * a.s));
            for (int i = 0; i < Math.Min(6, strongest.Count); i++)
            {
                var c = strongest[i];
                sb.Append(i == 0 ? "" : T("、", ", ")).Append(c.seed ? "？" : c.key.Replace("+", "·"))
                  .Append(c.w >= 0 ? "→" + T("是", "yes") : "→" + T("否", "no"));
                if (c.alt.Length > 0) sb.Append(T("（和 ", " (mixed with ")).Append(c.alt.Replace("+", "·")).Append(T(" 混在一格）", ")"));
            }
            if (strongest.Count == 0) sb.Append(T("还是空的", "nothing yet"));
            view.known.text = sb.ToString();
        }

        void DrawAtlas()
        {
            var sb = new StringBuilder("<b>" + T("现象图鉴", "Phenomena") + "</b>\n");
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
