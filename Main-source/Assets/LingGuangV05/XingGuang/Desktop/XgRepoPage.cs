using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 模型仓库: every saved model (each assessment record, plus hand saves from the training page). Filter and sort the
    /// list, look at a model's curve, load it back into training, star it so cleanup keeps it, or delete it.
    /// </summary>
    public sealed class XgRepoPage : XgPage
    {
        RectTransform list;
        TMP_Text header, dTitle, dStats, dGrade, empty;
        XgChartGraphic dChart;
        XgBtn load, star, delete;
        XgBtn[] filters, sorts;
        readonly List<(XgModelEntry m, RectTransform row, Image bg, TMP_Text left, TMP_Text right, TMP_Text badge)> rows = new List<(XgModelEntry, RectTransform, Image, TMP_Text, TMP_Text, TMP_Text)>();
        string rowKey = "";
        int filter, sort, selectedId, confirmDelete;
        public int SeenUpTo;

        public override void Build(RectTransform area)
        {
            var card = ui.Card(area, "repo", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root = card;
            header = ui.Text(Strip("Header", card, 8, 32, 16, 470), "", 16, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            string[] f = { "全部", "视觉", "序列", "★ 收藏" }, fe = { "All", "Vision", "Seq", "★ Starred" };
            filters = new XgBtn[f.Length];
            for (int i = 0; i < f.Length; i++)
            {
                int index = i;
                filters[i] = ui.Button(card, T(f[i], fe[i]), () => { filter = index; Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 14);
                PlaceTopRight(filters[i], 470 - i * 72, 8, 68, 30);
            }
            string[] so = { "最新", "最高分" }, soe = { "Newest", "Best" };
            sorts = new XgBtn[so.Length];
            for (int i = 0; i < so.Length; i++)
            {
                int index = i;
                sorts[i] = ui.Button(card, T(so[i], soe[i]), () => { sort = index; Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 14);
                PlaceTopRight(sorts[i], 170 - i * 82, 8, 78, 30);
            }

            var viewport = Rect("Viewport", card, Vector2.zero, new Vector2(.6f, 1), new Vector2(12, 12), new Vector2(-6, -48));
            Panel(viewport, new Color32(246, 248, 252, 255));
            viewport.gameObject.AddComponent<RectMask2D>();
            list = Rect("List", viewport, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            list.pivot = new Vector2(.5f, 1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = list; scroll.viewport = viewport; scroll.horizontal = false; scroll.scrollSensitivity = 30; scroll.movementType = ScrollRect.MovementType.Clamped;
            empty = ui.Text(Rect("Empty", viewport, Vector2.zero, Vector2.one, new Vector2(30, 30), new Vector2(-30, -30)), "", 16, XgPalette.Muted, TextAlignmentOptions.Center);

            var detail = Rect("Detail", card, new Vector2(.6f, 0), Vector2.one, new Vector2(6, 12), new Vector2(-12, -48));
            Panel(detail, new Color32(247, 249, 253, 255));
            dTitle = ui.Text(Strip("Title", detail, 12, 28, 14, 90), "", 15, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            dTitle.fontStyle = FontStyles.Bold;
            dTitle.textWrappingMode = TextWrappingModes.NoWrap; dTitle.overflowMode = TextOverflowModes.Ellipsis;
            var stamp = Rect("Grade", detail, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-84, -84), new Vector2(-12, -12));
            var ring = stamp.gameObject.AddComponent<XgRingGraphic>(); ring.Width = 6; ring.raycastTarget = false;
            dGrade = ui.Text(Rect("Letter", stamp, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 40, Color.white, TextAlignmentOptions.Center);
            dGrade.fontStyle = FontStyles.Bold;
            dStats = ui.Text(Rect("Stats", detail, new Vector2(0, 1), Vector2.one, new Vector2(14, -232), new Vector2(-14, -44)), "", 14, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            var chartBox = Rect("Chart", detail, Vector2.zero, new Vector2(1, 0), new Vector2(14, 104), new Vector2(-14, 250));
            Panel(chartBox, Color.white);
            dChart = Rect("Line", chartBox, Vector2.zero, Vector2.one, new Vector2(4, 4), new Vector2(-4, -4)).gameObject.AddComponent<XgChartGraphic>();
            dChart.raycastTarget = false;
            load = ui.Button(detail, "", Load, 16);
            PlaceBottom(load, 54, 40, 14, 14);
            star = ui.Button(detail, "", Star, 15);
            star.rt.anchorMin = new Vector2(0, 0); star.rt.anchorMax = new Vector2(.5f, 0); star.rt.offsetMin = new Vector2(14, 10); star.rt.offsetMax = new Vector2(-4, 46);
            delete = ui.Button(detail, "", Delete, 15);
            delete.rt.anchorMin = new Vector2(.5f, 0); delete.rt.anchorMax = new Vector2(1, 0); delete.rt.offsetMin = new Vector2(4, 10); delete.rt.offsetMax = new Vector2(-14, 46);
        }

        public override void Shown()
        {
            SeenUpTo = Sim.S.nextModelId - 1;
            if (Sim.Model(selectedId) == null && Sim.S.models.Count > 0) selectedId = Sim.S.models[Sim.S.models.Count - 1].id;
        }

        List<XgModelEntry> Visible()
        {
            var all = Sim.S.models.FindAll(m => filter == 0 || (filter == 1 && m.track == 0) || (filter == 2 && m.track == 1) || (filter == 3 && m.starred));
            if (sort == 0) all.Reverse();
            else all.Sort((a, b) => b.score != a.score ? b.score.CompareTo(a.score) : b.id.CompareTo(a.id));
            return all;
        }

        void Select(int id)
        {
            if (selectedId == id) return;
            selectedId = id; confirmDelete = 0;
            Fx.Play(XgJuice.Sfx.Id.Tick, 1.2f, .5f);
            Refresh();
        }

        void Load()
        {
            var m = Sim.Model(selectedId);
            if (m == null) return;
            if (!Sim.LoadModel(m.id)) { Fx.Knock(load.rt, .05f, new Vector2(8, 0)); Fx.Play(XgJuice.Sfx.Id.Thud); return; }
            Fx.Play(XgJuice.Sfx.Id.Swoosh);
            Fx.Shockwave(Fx.At(load.rt), XgPalette.Accent, 160, .3f, 6);
            view.ShowTab("train");
        }

        void Star()
        {
            var m = Sim.Model(selectedId);
            if (m == null) return;
            Sim.StarModel(m.id, !m.starred);
            Fx.Knock(star.rt, .15f);
            if (m.starred) { Fx.Burst(Fx.At(star.rt), 10, XgPalette.Star, XgJuice.Shape.Star, 180); Fx.Play(XgJuice.Sfx.Id.Coin, 1.3f, .5f); }
            Refresh();
        }

        void Delete()
        {
            var m = Sim.Model(selectedId);
            if (m == null) return;
            if (confirmDelete != m.id) { confirmDelete = m.id; Fx.Knock(delete.rt, .08f); Refresh(); return; }
            int index = Sim.S.models.IndexOf(m);
            if (!Sim.DeleteModel(m.id)) { Fx.Play(XgJuice.Sfx.Id.Thud); return; }
            Fx.Play(XgJuice.Sfx.Id.Break, 1.2f, .6f);
            confirmDelete = 0;
            selectedId = Sim.S.models.Count > 0 ? Sim.S.models[Math.Min(index, Sim.S.models.Count - 1)].id : 0;
            Refresh();
        }

        public override void Refresh()
        {
            if (root == null) return;
            double mb = 0; int deployed = 0;
            foreach (var m in Sim.S.models) { mb += XgSim.SizeMB(m); if (Sim.IsDeployed(m)) deployed++; }
            header.text = "<b>" + T("模型仓库", "Model repository") + "</b>  <color=#68748C>" + T("共 ", "") + Sim.S.models.Count + T(" 个 · 已部署 ", " models · deployed ") + deployed
                + " · " + Size(mb) + "</color>";
            for (int i = 0; i < filters.Length; i++) filters[i].Set(filters[i].label.text, true, i == filter ? XgPalette.Accent : XgPalette.Button, i == filter ? Color.white : XgPalette.Ink);
            for (int i = 0; i < sorts.Length; i++) sorts[i].Set(sorts[i].label.text, true, i == sort ? XgPalette.AccentSoft : XgPalette.Button, i == sort ? XgPalette.Accent : XgPalette.Ink);

            var shown = Visible();
            string key = filter + "|" + sort + "|" + string.Join(",", shown.ConvertAll(m => m.id.ToString()));
            if (key != rowKey)
            {
                rowKey = key;
                foreach (var r in rows) UnityEngine.Object.Destroy(r.row.gameObject);
                rows.Clear();
                for (int i = 0; i < shown.Count; i++)
                {
                    var m = shown[i];
                    int id = m.id;
                    var row = Rect("Model" + id, list, new Vector2(0, 1), new Vector2(1, 1), new Vector2(6, -(i + 1) * 58), new Vector2(-6, -i * 58 - 4));
                    var bg = Panel(row, Color.white);
                    var btn = row.gameObject.AddComponent<Button>(); btn.targetGraphic = bg; btn.onClick.AddListener(() => Select(id));
                    var badgeBox = XgUi.TopLeft("Badge", row, 8, 7, 40, 40);
                    Panel(badgeBox, XgPalette.Grades[XgSim.Grade(m.score)]).raycastTarget = false;
                    var badge = ui.Text(Rect("Letter", badgeBox, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), XgCatalog.GradeNames[XgSim.Grade(m.score)], 22, Color.white, TextAlignmentOptions.Center);
                    badge.fontStyle = FontStyles.Bold;
                    var left = ui.Text(Rect("Text", row, Vector2.zero, Vector2.one, new Vector2(58, 2), new Vector2(-120, -2)), "", 13, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
                    left.textWrappingMode = TextWrappingModes.NoWrap; left.overflowMode = TextOverflowModes.Ellipsis;
                    var right = ui.Text(Rect("Score", row, new Vector2(1, 0), Vector2.one, new Vector2(-116, 2), new Vector2(-10, -2)), "", 13, XgPalette.Ink, TextAlignmentOptions.MidlineRight);
                    rows.Add((m, row, bg, left, right, badge));
                }
                list.sizeDelta = new Vector2(0, shown.Count * 58 + 8);
            }
            empty.text = shown.Count == 0 ? T("还没有模型。评估刷新纪录时会自动存一个；训练页右上角也能手动「存入仓库」。", "No models yet. Each assessment record saves one; you can also save from the training page.") : "";
            foreach (var (m, row, bg, left, right, badge) in rows)
            {
                var d = XgCatalog.Dataset(m.dataset); var a = XgCatalog.Arch(m.arch);
                bool on = m.id == selectedId, dep = Sim.IsDeployed(m);
                bg.color = on ? XgPalette.AccentSoft : dep ? new Color32(236, 250, 240, 255) : Color.white;
                left.text = "<b>" + m.name + "</b>" + (m.starred ? " <color=#E0A800>★</color>" : "") + (m.id > SeenUpTo ? " <color=#E86E14>" + T("新", "new") + "</color>" : "")
                    + "\n<color=#68748C>" + T(d.name, d.nameEn) + " · " + T(a.name, a.nameEn) + " · " + m.depth + T(" 层 · 宽 ", "L · w") + XgCatalog.Widths[m.width] + " · " + T("第 ", "epoch ") + m.epoch + T(" 轮", "") + " · " + When(m) + "</color>";
                right.text = "<size=18><b>" + N(m.score, "0") + "</b></size>\n" + (dep ? "<color=#2F9E44>" + T("已部署", "deployed") + "</color>" : "<color=#68748C>" + XgSim.Pct(m.acc) + "</color>");
            }

            var sel = Sim.Model(selectedId);
            load.Show(sel != null); star.Show(sel != null); delete.Show(sel != null);
            dChart.gameObject.SetActive(sel != null);
            if (sel == null) { dTitle.text = ""; dStats.text = ""; dGrade.text = ""; return; }
            var sd = XgCatalog.Dataset(sel.dataset); var sa = XgCatalog.Arch(sel.arch);
            int grade = XgSim.Grade(sel.score);
            dTitle.text = sel.name;
            dGrade.text = XgCatalog.GradeNames[grade]; dGrade.color = XgPalette.Grades[grade];
            dGrade.transform.parent.GetComponent<XgRingGraphic>().color = XgPalette.Grades[grade];
            dStats.text = T("模型分 ", "Score ") + "<b>" + N(sel.score, "0") + "</b>  ·  " + T("验证 ", "val ") + XgSim.Pct(sel.acc) + "  ·  " + T("训练 ", "train ") + XgSim.Pct(sel.trainAcc) + "\n"
                + T(sd.name, sd.nameEn) + " · " + T(sa.name, sa.nameEn) + "\n"
                + sel.depth + T(" 层 · 宽 ", " layers · width ") + XgCatalog.Widths[sel.width] + T(" · 学习率 ", " · rate ") + XgCatalog.LearningRates[sel.lr] + "\n"
                + T("参数 ", "Params ") + Params(XgSim.ParamsK(new XgRun { arch = sel.arch, depth = sel.depth, width = sel.width })) + " · " + Size(XgSim.SizeMB(sel)) + "\n"
                + T("第 ", "Epoch ") + sel.epoch + T(" 轮 · 存于 ", " · saved ") + When(sel) + " · " + (sel.record ? T("评估纪录", "assessment record") : T("手动保存", "saved by hand"))
                + (Sim.IsDeployed(sel) ? "\n<color=#2F9E44>" + (Sim.AutoLabelHidden ? T("已部署：订单、论文都用它", "Deployed: contracts and papers use it") : T("已部署：订单、自动答题、论文都用它", "Deployed: contracts, auto-answer and papers use it")) + "</color>" : "");
            dChart.Capacity = Mathf.Max(8, sel.curve.Count);
            dChart.SetData(new List<float>(), sel.curve, (float)sel.acc);
            string why = Sim.CannotLoad(sel);
            load.Set(why == null ? T("加载到训练，接着练", "Load into training") : why, why == null, XgPalette.Accent, Color.white);
            star.Set(sel.starred ? T("★ 已收藏", "★ Starred") : T("☆ 收藏（不会被清理）", "☆ Star (kept)"), true, sel.starred ? new Color32(255, 244, 210, 255) : (Color?)null);
            bool dep2 = Sim.IsDeployed(sel);
            delete.Set(dep2 ? T("已部署，不能删", "Deployed") : confirmDelete == sel.id ? T("再点一次确认删除", "Click again to delete") : T("删除", "Delete"), !dep2,
                confirmDelete == sel.id ? XgPalette.Bad : (Color?)null, confirmDelete == sel.id ? Color.white : (Color?)null);
        }

        static string Size(double mb) { return mb >= 100 ? N(mb, "0") + " MB" : mb >= 1 ? N(mb, "0.0") + " MB" : N(mb * 1024, "0") + " KB"; }

        static string When(XgModelEntry m)
        {
            var t = GameCalendar.ClockFor(Math.Max(0, m.gameSeconds));
            int month = m.date > 0 ? m.date / 100 % 100 : t.Month, day = m.date > 0 ? m.date % 100 : t.Day;
            return month + "/" + day + " " + t.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
