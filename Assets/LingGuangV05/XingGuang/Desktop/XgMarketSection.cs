using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Subcontract workers in the crowdsourcing app. Presentation only.</summary>
    public sealed class XgMarketSection
    {
        const float Gap = 8, TitleH = 40, WorkerH = 64;

        sealed class WorkerRow { public XgWorkerInfo info; public RectTransform row; public Image bg, avatar; public TMP_Text text; public XgBtn desk, hire; }

        readonly IXgPageHost view;
        readonly XgUi ui;
        readonly RectTransform root;
        readonly List<WorkerRow> workers = new List<WorkerRow>();
        RectTransform scTitle;
        TMP_Text scTitleText;
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
            if (scTitleText != null)
                scTitleText.text = "<b>" + Lang.T("转包 · 网吧兄弟帮你标") + "</b>  <size=13><color=#68748C>"
                    + T("工资合计 ¥" + N(Sim.WagesPerMinute, "0") + "/分钟 · 他们的错算在你的账号上", "Wages ¥" + N(Sim.WagesPerMinute, "0") + "/min in total · their mistakes count against your account") + "</color></size>";
            return height;
        }

        void Rebuild(bool workersOpen)
        {
            foreach (Transform child in root) Object.Destroy(child.gameObject);
            workers.Clear();
            scTitle = null; scTitleText = null;
            if (workersOpen)
            {
                scTitle = Strip("SubcontractTitle", root, 0, TitleH, 4, 4);
                scTitleText = ui.Text(Rect("Text", scTitle, Vector2.zero, Vector2.one, new Vector2(8, 0), Vector2.zero), "", 16, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
                UiTip.Add(scTitle, "阿杰、小刚和网吧老板用你的摆渡众包账号标注。平台照样抽检他们的标注：错的罚款、扣信用分、举报都算在你头上；没抽到的错题会变成训练数据里的噪声。工资每分钟照付，账号冻结或等验证码时他们只能干等，钱照样给。",
                    "Ajie, Xiaogang and the netbar boss label under your Bodu Crowdsourcing account. The platform spot-checks their labels like any other: fines, lost credit and reports all land on you, and unchecked mistakes become noise in your training data. Wages are due every minute, even while a frozen account or a captcha keeps them waiting.");
                foreach (var info in XgSim.Workers) workers.Add(BuildWorker(info));
            }
        }

        float Layout()
        {
            float y = 0;
            if (scTitle != null)
            {
                y += Gap; Place(scTitle, y, TitleH); y += TitleH;
                foreach (var w in workers) { y += 4; Place(w.row, y, WorkerH); y += WorkerH; }
            }
            return y > 0 ? y + Gap : 0;
        }

        static void Place(RectTransform rt, float y, float h) { rt.offsetMin = new Vector2(rt.offsetMin.x, -y - h); rt.offsetMax = new Vector2(rt.offsetMax.x, -y); }

        // ───────────── 转包 ─────────────

        static readonly Color[] AvatarColors = { new Color32(236, 151, 31, 255), new Color32(64, 158, 255, 255), new Color32(120, 96, 200, 255) };

        WorkerRow BuildWorker(XgWorkerInfo info)
        {
            var w = new WorkerRow { info = info };
            w.row = Strip("Worker" + info.id, root, 0, WorkerH, 4, 4);
            w.bg = Panel(w.row, new Color32(247, 249, 253, 255));
            var avatar = Rect("Avatar", w.row, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(12, -20), new Vector2(52, 20));
            w.avatar = Panel(avatar, AvatarColors[System.Array.IndexOf(XgSim.Workers, info) % AvatarColors.Length]);
            ui.Text(Rect("Initial", avatar, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Initial(info), 20, Color.white, TextAlignmentOptions.Center);
            w.text = ui.Text(Rect("Text", w.row, Vector2.zero, Vector2.one, new Vector2(62, 4), new Vector2(-262, -4)), "", 14, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            w.desk = ui.Button(w.row, "", () => { Sim.CycleWorkerDesk(info.id); view.Refresh(true); }, 13);
            w.desk.rt.anchorMin = w.desk.rt.anchorMax = new Vector2(1, .5f); w.desk.rt.offsetMin = new Vector2(-254, -16); w.desk.rt.offsetMax = new Vector2(-122, 16);
            w.desk.label.enableAutoSizing = true; w.desk.label.fontSizeMin = 10; w.desk.label.fontSizeMax = 13;
            w.hire = ui.Button(w.row, "", () => Toggle(w), 14);
            w.hire.rt.anchorMin = w.hire.rt.anchorMax = new Vector2(1, .5f); w.hire.rt.offsetMin = new Vector2(-114, -16); w.hire.rt.offsetMax = new Vector2(-10, 16);
            UiTip.Add(w.desk.rt, "换桌：点一下换到下一张开放的标注桌。他们按这张桌的自动答题单价给你挣钱，标对的也变成你的样本。",
                "Desk: click to move them to the next open desk. They earn that desk's auto-labelling rate for you, and their right answers become your samples.");
            UiTip.Add(w.hire.rt, () => HireTip(w.info));
            UiTip.Add(w.row, () => WorkerTip(w.info));
            return w;
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
            string status = !hired ? (info.nightOnly && !Sim.NightShift ? "<color=#68748C>" + Lang.T("白天在睡觉") + "</color>" : "<color=#68748C>" + Lang.T("在网吧坐着") + "</color>")
                : idle != null ? "<color=#B36A00>" + idle + "</color>" : "<color=#2F9E44>" + Lang.T("在干活") + "</color>";
            w.text.text = "<b>" + T(info.name, info.nameEn) + "</b>  <color=#E86E14>¥" + N(info.wage, "0") + Lang.T("/分钟") + "</color> · " + N(interval, "0.#") + Lang.T(" 秒/条")
                + (info.nightOnly ? Lang.T(" · 仅夜班") : "") + "  " + status
                + "\n<size=12><color=#68748C>" + T("今日 " + st.labelsToday + " 条 · 平台抓到错 " + st.errorsSeen + " 条 · 已付工资 ¥" + N(st.wagesPaid, "0"),
                    "Today " + st.labelsToday + " · errors caught by the platform " + st.errorsSeen + " · wages paid ¥" + N(st.wagesPaid, "0")) + "</color></size>";
            w.desk.Set((deskInfo != null ? T(deskInfo.name, deskInfo.nameEn) : "—") + " ›", Sim.OpenDesks().Count > 1);
            if (hired) w.hire.Set(Lang.T("辞退"), true, XgPalette.Button, XgPalette.Bad);
            else
            {
                bool can = Sim.CanHire(info.id, Host, out _);
                w.hire.Set(Lang.T("雇用"), can, can ? XgPalette.Accent : (Color?)null, can ? Color.white : (Color?)null);
            }
            w.bg.color = hired ? (idle != null ? new Color32(255, 247, 230, 255) : new Color32(240, 250, 242, 255)) : new Color32(247, 249, 253, 255);
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
