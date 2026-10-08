using System.Collections.Generic;
using LingGuangV05.Desktop.Zhongbao;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Subcontract workers in the crowdsourcing app, in 摆渡众包's white-platform look: one row per friend with a round
    /// coloured face, name, note and wage, the last results as green/red squares, a desk button and a hire/fire button,
    /// then a footer line. Presentation only.
    /// </summary>
    public sealed class XgMarketSection
    {
        const float WorkerH = 82, FooterH = 44;

        sealed class WorkerRow
        {
            public XgWorkerInfo info; public RectTransform row; public Image bg;
            public TMP_Text name, note, stats;
            public XgCrawlerLayer squares;
            public string squareKey = "";
            public XgBtn desk, hire;
        }

        readonly IXgPageHost view;
        readonly XgUi ui;
        readonly RectTransform root;
        readonly List<WorkerRow> workers = new List<WorkerRow>();
        TMP_Text footer;
        string key = "";

        XgSim Sim => view.Sim;
        IXgHost Host => view.Host;

        public XgMarketSection(IXgPageHost view, XgUi ui, RectTransform list)
        {
            this.view = view; this.ui = ui;
            root = Strip("Market", list, 0, 0, 0, 0);
        }

        /// <summary>Places the section <paramref name="top"/> pixels down the list and refreshes it. Returns its height.</summary>
        public float Refresh(float top)
        {
            if (Sim == null) return 0;
            bool workersOpen = Sim.SubcontractUnlocked;
            string k = workersOpen.ToString();
            if (k != key) { key = k; Rebuild(workersOpen); }
            float height = Layout();
            root.offsetMin = new Vector2(0, -top - height); root.offsetMax = new Vector2(0, -top);
            root.gameObject.SetActive(height > 0);
            foreach (var w in workers) RefreshWorker(w);
            if (footer != null)
                footer.text = T("工资合计 <b>¥" + N(Sim.WagesPerMinute, "0") + "/分钟</b> · 没被抽检到的错题会变成训练数据里的噪声，他们的错算在你的账号上。",
                    "Wages <b>¥" + N(Sim.WagesPerMinute, "0") + "/min</b> in total · mistakes the platform does not catch become noise in your training data, and they all count against your account.");
            return height;
        }

        void Rebuild(bool workersOpen)
        {
            foreach (Transform child in root) Object.Destroy(child.gameObject);
            workers.Clear();
            footer = null;
            if (workersOpen)
            {
                foreach (var info in XgSim.Workers) workers.Add(BuildWorker(info));
                footer = ui.Text(Rect("Footer", root, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero), "", 12, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
                footer.margin = new Vector4(12, 0, 12, 0);
            }
        }

        float Layout()
        {
            if (workers.Count == 0) return 0;
            float y = 0;
            foreach (var w in workers) { Place(w.row, y, WorkerH); y += WorkerH; }
            Place((RectTransform)footer.transform, y, FooterH); y += FooterH;
            return y;
        }

        static void Place(RectTransform rt, float y, float h) { rt.offsetMin = new Vector2(rt.offsetMin.x, -y - h); rt.offsetMax = new Vector2(rt.offsetMax.x, -y); }

        // ───────────── 转包 ─────────────

        static readonly Color[] FaceColors = { new Color32(232, 163, 23, 255), new Color32(47, 158, 68, 255), new Color32(78, 110, 242, 255) };

        WorkerRow BuildWorker(XgWorkerInfo info)
        {
            var w = new WorkerRow { info = info };
            w.row = Strip("Worker" + info.id, root, 0, WorkerH, 0, 0);
            w.bg = Panel(w.row, Color.white);
            var rule = Rect("Rule", w.row, Vector2.zero, new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1));
            Panel(rule, ZhongbaoSkin.Line).raycastTarget = false;
            var face = Rect("Face", w.row, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(12, -22), new Vector2(56, 22));
            var color = FaceColors[System.Array.IndexOf(XgSim.Workers, info) % FaceColors.Length];
            var disc = face.gameObject.AddComponent<XgCrawlerLayer>();
            disc.raycastTarget = false; disc.color = Color.white;
            disc.draw = vh => { var r = face.rect; XgDraw.Disc(vh, r.center, Mathf.Min(r.width, r.height) * .5f, color, 36); };
            var initial = ui.Text(Rect("Initial", face, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Initial(info), 20, Color.white, TextAlignmentOptions.Center);
            initial.fontStyle = FontStyles.Bold; initial.textWrappingMode = TextWrappingModes.NoWrap;
            w.name = ui.Text(Rect("Name", w.row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(68, -32), new Vector2(-250, -8)), "", 15, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
            w.name.textWrappingMode = TextWrappingModes.NoWrap; w.name.overflowMode = TextOverflowModes.Ellipsis;
            w.note = ui.Text(Rect("Note", w.row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(68, -50), new Vector2(-250, -32)), "", 12, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
            w.note.textWrappingMode = TextWrappingModes.NoWrap; w.note.overflowMode = TextOverflowModes.Ellipsis;
            // The last 13 labels as small squares: green right, red wrong, grey not yet.
            var squares = Rect("Recent", w.row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(68, -68), new Vector2(68 + SquaresWidth, -59));
            w.squares = squares.gameObject.AddComponent<XgCrawlerLayer>();
            w.squares.raycastTarget = false; w.squares.color = Color.white;
            var captured = w;
            w.squares.draw = vh => DrawSquares(vh, squares.rect, captured.squareKey);
            w.stats = ui.Text(Rect("Stats", w.row, new Vector2(0, 1), new Vector2(1, 1), new Vector2(68 + SquaresWidth + 10, -72), new Vector2(-250, -55)), "", 11, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
            w.stats.textWrappingMode = TextWrappingModes.NoWrap; w.stats.overflowMode = TextOverflowModes.Ellipsis;
            w.desk = ZhongbaoSkin.Outlined(ui, w.row, Lang.T("派到"), () => { Sim.CycleWorkerDesk(info.id); view.Refresh(true); }, 13, new Color32(184, 194, 246, 255));
            w.desk.label.enableAutoSizing = true; w.desk.label.fontSizeMin = 10; w.desk.label.fontSizeMax = 13;
            Anchor(w.desk, -240, 130);
            w.hire = ZhongbaoSkin.Outlined(ui, w.row, "", () => Toggle(w), 14, new Color32(184, 194, 246, 255));
            Anchor(w.hire, -102, 90);
            UiTip.Add(w.desk.rt, "换桌：点一下换到下一张开放的标注桌。他们按这张桌的自动答题单价给你挣钱，标对的也变成你的样本。",
                "Desk: click to move them to the next open desk. They earn that desk's auto-labelling rate for you, and their right answers become your samples.");
            UiTip.Add(w.hire.rt, () => HireTip(w.info));
            UiTip.Add(w.row, () => WorkerTip(w.info));
            return w;
        }

        const float SquaresWidth = 13 * 9 + 12 * 2;

        /// <summary>Puts a button's frame at the row's right side: <paramref name="fromRight"/> is the frame's left edge (negative), 34 px tall.</summary>
        static void Anchor(XgBtn b, float fromRight, float width)
        {
            var f = ZhongbaoSkin.Frame(b);
            f.anchorMin = f.anchorMax = new Vector2(1, .5f);
            f.offsetMin = new Vector2(fromRight, -17); f.offsetMax = new Vector2(fromRight + width, 17);
        }

        static void DrawSquares(VertexHelper vh, UnityEngine.Rect r, string recent)
        {
            const float size = 9, gap = 2;
            int missing = XgSim.WorkerRecentKept - (recent == null ? 0 : recent.Length);
            for (int i = 0; i < XgSim.WorkerRecentKept; i++)
            {
                int at = i - missing;
                Color c = at < 0 ? ZhongbaoSkin.Track : recent[at] == 'o' ? ZhongbaoSkin.Green : ZhongbaoSkin.Red;
                float x = r.xMin + i * (size + gap);
                XgDraw.Box(vh, new Vector2(x, r.yMin), new Vector2(x + size, r.yMin + size), c);
            }
        }

        static string Initial(XgWorkerInfo info) => info.id == "ajie" ? Lang.T("杰") : info.id == "xiaogang" ? Lang.T("刚") : Lang.T("老");

        string HireTip(XgWorkerInfo info)
        {
            if (Sim.Hired(info.id)) return Lang.T("辞退：工资停在这一分钟，已付的不退。");
            return Sim.CanHire(info.id, Host, out string why) ? T("雇用：先付第一分钟 ¥" + N(info.wage, "0") + "，之后每分钟 ¥" + N(info.wage, "0") + "。", "Hire: pay the first minute (¥" + N(info.wage, "0") + ") now, then ¥" + N(info.wage, "0") + " every minute.") : why;
        }

        string WorkerTip(XgWorkerInfo info)
        {
            switch (info.id)
            {
                case "ajie": return Lang.T("阿杰：便宜、手快，偶尔走神。正确率约 85%，比平台的举报线还低一截，别让他一个人扛一张桌太久。");
                case "xiaogang": return Lang.T("小刚：慢，但几乎不出错（约 97%）。");
                default: return Lang.T("网吧老板：只上夜班（22:00–06:00），天一亮就回去睡觉。正确率约 93%。");
            }
        }

        string Blurb(XgWorkerInfo info)
        {
            switch (info.id)
            {
                case "ajie": return T("便宜手快，偶尔走神", "Cheap and quick, drifts off now and then");
                case "xiaogang": return T("手慢，但几乎不出错", "Slow, and almost never wrong");
                default: return T("只上夜班，22:00–06:00", "Night shift only, 22:00–06:00");
            }
        }

        void RefreshWorker(WorkerRow w)
        {
            var info = w.info;
            var st = Sim.Worker(info.id);
            if (st == null) return;
            bool hired = st.hired;
            string desk = Sim.WorkerDesk(info.id);
            var deskInfo = XgCatalog.Desk(desk);
            double interval = Sim.WorkerInterval(info.id);
            string idle = hired ? Sim.WorkerIdleReason(info.id) : null;
            string status = !hired ? (info.nightOnly && !Sim.NightShift ? "<color=#7A7F8C>" + Lang.T("白天在睡觉") + "</color>" : "<color=#7A7F8C>" + Lang.T("在网吧坐着") + "</color>")
                : idle != null ? "<color=#B36A00>" + idle + "</color>" : "<color=#2F9E44>" + Lang.T("在干活") + "</color>";
            w.name.text = "<color=#222222><b>" + T(info.name, info.nameEn) + "</b></color>   <size=12>" + status + "</size>";
            w.note.text = Blurb(info) + T(" · 工资 ", " · wage ") + "<color=#E33E33>¥" + N(info.wage, "0") + Lang.T("/分钟") + "</color> · " + N(interval, "0.#") + Lang.T(" 秒/条");
            w.stats.text = T("今日 " + st.labelsToday + " 条 · 平台抓到错 " + st.errorsSeen + " 条 · 已付工资 ¥" + N(st.wagesPaid, "0"),
                "Today " + st.labelsToday + " · errors caught " + st.errorsSeen + " · wages paid ¥" + N(st.wagesPaid, "0"));
            string recent = st.recent ?? "";
            if (recent != w.squareKey) { w.squareKey = recent; w.squares.Redraw(); }

            bool canCycle = Sim.OpenDesks().Count > 1;
            ZhongbaoSkinGhost(w.desk, Lang.T("派到 ") + (deskInfo != null ? T(deskInfo.name, deskInfo.nameEn) : "—"), canCycle);
            if (hired)
            {
                w.hire.Set(Lang.T("辞退"), true, Color.white, ZhongbaoSkin.Mute);
                ZhongbaoSkin.SetRim(w.hire, ZhongbaoSkin.Line);
            }
            else
            {
                bool can = Sim.CanHire(info.id, Host, out _);
                w.hire.Set(Lang.T("雇用"), can, can ? ZhongbaoSkin.Blue : (Color?)null, can ? Color.white : (Color?)null);
                ZhongbaoSkin.SetRim(w.hire, can ? ZhongbaoSkin.Blue : ZhongbaoSkin.Line);
            }
            w.bg.color = hired && idle != null ? ZhongbaoSkin.OrangeSoft : Color.white;
        }

        void ZhongbaoSkinGhost(XgBtn b, string text, bool enabled)
        {
            b.Set(text, enabled, Color.white, ZhongbaoSkin.Blue);
            ZhongbaoSkin.SetRim(b, enabled ? new Color32(184, 194, 246, 255) : ZhongbaoSkin.Line);
        }

        void Toggle(WorkerRow w)
        {
            if (Sim.Hired(w.info.id)) { Sim.Fire(w.info.id, Host); view.Juice.Play(XgJuice.Sfx.Id.Thud); }
            else if (Sim.Hire(w.info.id, Host)) { view.Juice.Play(XgJuice.Sfx.Id.Coin, 1, .7f); view.Juice.Float(view.Juice.At(w.row), Lang.T("−¥") + N(w.info.wage, "0") + Lang.T(" 第一分钟"), XgPalette.Money, 20); }
            else
            {
                Sim.CanHire(w.info.id, Host, out string why);
                view.Juice.Knock(w.row, .02f, new Vector2(12, 0));
                if (!string.IsNullOrEmpty(why)) view.ShowToast(why, 2.5f);
            }
            view.Refresh(true);
        }
    }
}
