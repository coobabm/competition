using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The lower half of the 订单 page: 甲方高价单 (SLA contracts with a quality clause) once 摆渡众包 is checking the
    /// account, and 转包 (网吧 friends labelling under your account) once 阿杰 has asked. Lives in the contracts list
    /// below the normal contracts and reports its height so the list scrolls over it. Presentation only.
    /// </summary>
    public sealed class XgMarketSection
    {
        const float Gap = 8, TitleH = 40, OfferH = 66, ActiveH = 100, WorkerH = 64;

        sealed class OfferRow { public XgSlaOffer offer; public RectTransform row; public Image bg; public TMP_Text text; public XgBtn btn; public RectTransform bar; public float height; }
        sealed class WorkerRow { public XgWorkerInfo info; public RectTransform row; public Image bg, avatar; public TMP_Text text; public XgBtn desk, hire; }

        readonly XingGuangView view;
        readonly XgUi ui;
        readonly RectTransform root;
        readonly List<OfferRow> offers = new List<OfferRow>();
        readonly List<WorkerRow> workers = new List<WorkerRow>();
        RectTransform slaTitle, scTitle;
        TMP_Text slaTitleText, scTitleText;
        string key = "";

        XgSim Sim => view.Sim;
        IXgHost Host => view.Host;

        public XgMarketSection(XingGuangView view, XgUi ui, RectTransform list)
        {
            this.view = view; this.ui = ui;
            root = Strip("Market", list, 0, 0, 0, 0);
        }

        /// <summary>Places the section <paramref name="top"/> pixels down the list and refreshes it. Returns its height.</summary>
        public float Refresh(float top)
        {
            if (Sim == null) return 0;
            var visible = Sim.SlaOffersVisible();
            bool workersOpen = Sim.SubcontractUnlocked;
            string k = string.Join(",", visible.ConvertAll(o => o.id)) + "|" + Sim.S.slaId + "|" + workersOpen;
            if (k != key) { key = k; Rebuild(visible, workersOpen); }
            float height = Layout();
            root.offsetMin = new Vector2(0, -top - height); root.offsetMax = new Vector2(0, -top);
            root.gameObject.SetActive(height > 0);
            foreach (var r in offers) RefreshOffer(r);
            foreach (var w in workers) RefreshWorker(w);
            if (slaTitleText != null)
                slaTitleText.text = "<b>" + T("甲方高价单", "High-paying client contracts") + "</b>  <size=13><color=#68748C>"
                    + T("信用分 " + N(Sim.Credit, "0") + "（需 ≥ " + N(XgSim.SlaMinCredit, "0") + "）· 一次一单 · 一半收入押作尾款 · 被举报即违约",
                        "Credit " + N(Sim.Credit, "0") + " (needs " + N(XgSim.SlaMinCredit, "0") + ") · one at a time · half the income held as the balance · a report breaks the contract") + "</color></size>";
            if (scTitleText != null)
                scTitleText.text = "<b>" + T("转包 · 网吧兄弟帮你标", "Subcontract · netbar friends label for you") + "</b>  <size=13><color=#68748C>"
                    + T("工资合计 ¥" + N(Sim.WagesPerMinute, "0") + "/分钟 · 他们的错算在你的账号上", "Wages ¥" + N(Sim.WagesPerMinute, "0") + "/min in total · their mistakes count against your account") + "</color></size>";
            return height;
        }

        void Rebuild(List<XgSlaOffer> visible, bool workersOpen)
        {
            foreach (Transform child in root) Object.Destroy(child.gameObject);
            offers.Clear(); workers.Clear();
            slaTitle = scTitle = null; slaTitleText = scTitleText = null;
            if (visible.Count > 0)
            {
                slaTitle = Strip("SlaTitle", root, 0, TitleH, 4, 4);
                slaTitleText = ui.Text(Rect("Text", slaTitle, Vector2.zero, Vector2.one, new Vector2(8, 0), Vector2.zero), "", 16, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
                foreach (var o in visible) offers.Add(BuildOffer(o));
            }
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
            if (slaTitle != null)
            {
                y += Gap; Place(slaTitle, y, TitleH); y += TitleH;
                foreach (var r in offers) { r.height = Sim.S.slaId == r.offer.id ? ActiveH : OfferH; y += 4; Place(r.row, y, r.height); y += r.height; }
            }
            if (scTitle != null)
            {
                y += Gap; Place(scTitle, y, TitleH); y += TitleH;
                foreach (var w in workers) { y += 4; Place(w.row, y, WorkerH); y += WorkerH; }
            }
            return y > 0 ? y + Gap : 0;
        }

        static void Place(RectTransform rt, float y, float h) { rt.offsetMin = new Vector2(rt.offsetMin.x, -y - h); rt.offsetMax = new Vector2(rt.offsetMax.x, -y); }

        // ───────────── 甲方高价单 ─────────────

        OfferRow BuildOffer(XgSlaOffer offer)
        {
            var r = new OfferRow { offer = offer };
            r.row = Strip("Sla" + offer.id, root, 0, OfferH, 4, 4);
            r.bg = Panel(r.row, new Color32(247, 249, 253, 255));
            r.text = ui.Text(Rect("Text", r.row, Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-150, -4)), "", 14, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            r.btn = ui.Button(r.row, "", () => Sign(r), 14);
            r.btn.rt.anchorMin = r.btn.rt.anchorMax = new Vector2(1, .5f); r.btn.rt.offsetMin = new Vector2(-140, -17); r.btn.rt.offsetMax = new Vector2(-10, 17);
            var barBack = Rect("Bar", r.row, new Vector2(0, 0), new Vector2(1, 0), new Vector2(12, 6), new Vector2(-150, 10));
            r.bar = Bar(barBack, "Fill", XgPalette.Line, XgPalette.Good);
            UiTip.Add(r.row, () => OfferTip(offer));
            UiTip.Add(r.btn.rt, () => ButtonTip(offer));
            return r;
        }

        string OfferTip(XgSlaOffer o)
        {
            return T("高价单：单价是同桌普通订单的 2 倍，期限 " + N(o.duration / 60, "0") + " 分钟。期间平台对「" + XgCatalog.Desk(o.dataset).name + "」的抽检合格率要 ≥ 95%。每秒收入一半先到账，另一半押作尾款：到期合格（或抽检不足 10 条）全额结清，不合格只付一半；中途被举报就违约，尾款作废、信用分再 −5。",
                "High-paying contract: twice the rate of a normal contract on this desk, for " + N(o.duration / 60, "0") + " min. The platform's spot-check pass rate on " + XgCatalog.Desk(o.dataset).nameEn + " must stay at 95% or more. Half the income arrives every second; the other half is held as the balance. At the end it is paid in full on a pass (or with fewer than 10 checks) and only half on a fail. A report breaks the contract: the balance is forfeited and credit drops another 5.");
        }

        string ButtonTip(XgSlaOffer o)
        {
            if (Sim.S.slaId == o.id) return T("正在履约：注意这张桌的抽检。新题型、阿杰的脚本都会拉低合格率。", "In progress: watch this desk's spot checks. New meme cards and a sloppy subcontractor both drag the pass rate down.");
            return Sim.CanSignSla(o, out string why) ? T("签约：需要信用分 ≥ 85、检查点达到门槛，一次只能接一单。", "Sign: needs credit 85+, a checkpoint over the bar, and no other high-paying contract running.") : why;
        }

        void RefreshOffer(OfferRow r)
        {
            var o = r.offer;
            var d = XgCatalog.Dataset(o.dataset);
            bool active = Sim.S.slaId == o.id;
            double best = Sim.BestAcc(o.dataset);
            string head = "<b>" + T(o.client, o.clientEn) + "</b> · " + T(o.job, o.jobEn) + "  <size=12><color=#68748C>" + T(d.name, d.nameEn) + " ≥ " + XgSim.Pct(o.Threshold)
                + T("  当前 ", "  now ") + (best > 0 ? XgSim.Pct(best) : "—") + "</color></size>";
            if (!active)
            {
                r.text.text = head + "\n<color=#E86E14>¥" + Money(o.Income) + T("/秒起（普通单 ×2）", "/s base (2× a normal contract)") + "</color><size=13><color=#68748C> · "
                    + N(o.duration / 60, "0") + T(" 分钟 · 抽检合格率 ≥ 95% · 一半押作尾款", " min · spot-check pass rate ≥ 95% · half held as the balance") + "</color></size>";
                bool can = Sim.CanSignSla(o, out string why);
                double cooldown = Sim.SlaCooldown(o.id);
                string label = can ? T("签约", "Sign") : cooldown > 0 ? T("下单 ", "Next ") + XgSim.FreezeClock(cooldown) : Sim.SlaActive ? T("一次一单", "One at a time") : Sim.Credit + 1e-9 < XgSim.SlaMinCredit ? T("信用不足", "Low credit") : T("未达标", "Not eligible");
                r.btn.Set(label, can, can ? XgPalette.Accent : (Color?)null, can ? Color.white : (Color?)null);
                r.bg.color = new Color32(247, 249, 253, 255);
                r.bar.parent.gameObject.SetActive(false);
                return;
            }
            double rate = Sim.SlaPassRate;
            bool judged = Sim.SlaJudged, failing = judged && rate + 1e-9 < XgSim.SlaClause;
            string clause = Sim.SlaChecks == 0 ? T("还没有抽检", "no spot checks yet")
                : T("条款合格率 ", "clause pass rate ") + (failing ? "<color=#D63031>" : "<color=#2F9E44>") + XgSim.Pct(rate) + "</color>" + T("（" + Sim.SlaChecks + " 条抽检", " (" + Sim.SlaChecks + " checks")
                  + (judged ? T("）", ")") : T("，满 " + XgSim.SlaMinChecks + " 条才算数）", "; counts from " + XgSim.SlaMinChecks + ")"));
            r.text.text = head + "\n<color=#2F9E44><b>" + T("履约中", "In progress") + "</b></color> " + T("剩 ", "") + XgSim.FreezeClock(Sim.SlaSecondsLeft) + T("", " left")
                + " · <color=#E86E14>¥" + Money(Sim.SlaIncome) + T("/秒", "/s") + "</color> · " + T("尾款 ¥", "balance ¥") + Money(Sim.SlaHeld) + "\n<size=13>" + clause + "</size>";
            r.btn.Set(T("履约中", "In progress"), false);
            r.bg.color = failing ? new Color32(253, 240, 240, 255) : new Color32(240, 250, 242, 255);
            r.bar.parent.gameObject.SetActive(true);
            SetBar(r.bar, (float)(1 - Sim.SlaSecondsLeft / o.duration));
            r.bar.GetComponent<Image>().color = failing ? XgPalette.Bad : XgPalette.Good;
        }

        void Sign(OfferRow r)
        {
            if (!Sim.SignSla(r.offer.id))
            {
                Sim.CanSignSla(r.offer, out string why);
                view.Juice.Knock(r.row, .02f, new Vector2(12, 0));
                view.Juice.Play(XgJuice.Sfx.Id.Thud);
                if (!string.IsNullOrEmpty(why)) view.ShowToast(why, 2.5f);
                return;
            }
            view.Juice.Play(XgJuice.Sfx.Id.Stamp);
            view.Juice.Float(view.Juice.At(r.row) + new Vector2(0, 30), T("接单！尾款押一半", "Signed! Half held as the balance"), XgPalette.Good, 22);
            view.Refresh(true);
        }

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

        static string Initial(XgWorkerInfo info) => info.id == "ajie" ? T("杰", "A") : info.id == "xiaogang" ? T("刚", "X") : T("老", "B");

        string HireTip(XgWorkerInfo info)
        {
            if (Sim.Hired(info.id)) return T("辞退：工资停在这一分钟，已付的不退。", "Let go: wages stop; the minute already paid is not refunded.");
            return Sim.CanHire(info.id, Host, out string why) ? T("雇用：先付第一分钟 ¥" + N(info.wage, "0") + "，之后每分钟 ¥" + N(info.wage, "0") + "。", "Hire: pay the first minute (¥" + N(info.wage, "0") + ") now, then ¥" + N(info.wage, "0") + " every minute.") : why;
        }

        string WorkerTip(XgWorkerInfo info)
        {
            switch (info.id)
            {
                case "ajie": return T("阿杰：便宜、手快，偶尔走神。正确率约 85%，比平台的举报线还低一截，别让他一个人扛一张桌太久。", "Ajie: cheap and quick, a little careless. About 85% right, which is below the platform's report line; don't leave him alone on a desk for long.");
                case "xiaogang": return T("小刚：慢，但几乎不出错（约 97%）。", "Xiaogang: slow, but hardly ever wrong (about 97%).");
                default: return T("网吧老板：只上夜班（22:00–06:00），天一亮就回去睡觉。正确率约 93%。", "The netbar boss: night shift only (22:00–06:00), off to bed at sunrise. About 93% right.");
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
            string status = !hired ? (info.nightOnly && !Sim.NightShift ? "<color=#68748C>" + T("白天在睡觉", "Asleep (day)") + "</color>" : "<color=#68748C>" + T("在网吧坐着", "At the netbar") + "</color>")
                : idle != null ? "<color=#B36A00>" + idle + "</color>" : "<color=#2F9E44>" + T("在干活", "Working") + "</color>";
            w.text.text = "<b>" + T(info.name, info.nameEn) + "</b>  <color=#E86E14>¥" + N(info.wage, "0") + T("/分钟", "/min") + "</color> · " + N(interval, "0.#") + T(" 秒/条", " s/card")
                + (info.nightOnly ? T(" · 仅夜班", " · nights only") : "") + "  " + status
                + "\n<size=12><color=#68748C>" + T("今日 " + st.labelsToday + " 条 · 平台抓到错 " + st.errorsSeen + " 条 · 已付工资 ¥" + N(st.wagesPaid, "0"),
                    "Today " + st.labelsToday + " · errors caught by the platform " + st.errorsSeen + " · wages paid ¥" + N(st.wagesPaid, "0")) + "</color></size>";
            w.desk.Set((deskInfo != null ? T(deskInfo.name, deskInfo.nameEn) : "—") + " ›", Sim.OpenDesks().Count > 1);
            if (hired) w.hire.Set(T("辞退", "Let go"), true, XgPalette.Button, XgPalette.Bad);
            else
            {
                bool can = Sim.CanHire(info.id, Host, out _);
                w.hire.Set(T("雇用", "Hire"), can, can ? XgPalette.Accent : (Color?)null, can ? Color.white : (Color?)null);
            }
            w.bg.color = hired ? (idle != null ? new Color32(255, 247, 230, 255) : new Color32(240, 250, 242, 255)) : new Color32(247, 249, 253, 255);
        }

        void Toggle(WorkerRow w)
        {
            if (Sim.Hired(w.info.id)) { Sim.Fire(w.info.id, Host); view.Juice.Play(XgJuice.Sfx.Id.Thud); }
            else if (Sim.Hire(w.info.id, Host)) { view.Juice.Play(XgJuice.Sfx.Id.Coin, 1, .7f); view.Juice.Float(view.Juice.At(w.row), T("−¥", "−¥") + N(w.info.wage, "0") + T(" 第一分钟", " first minute"), XgPalette.Money, 20); }
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
