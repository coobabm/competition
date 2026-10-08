using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;
using Row = LingGuangV05.Desktop.Zhongbao.ZhongbaoRows;

namespace LingGuangV05.Desktop.Zhongbao
{
    /// <summary>Layout helpers shared by 摆渡众包's own pages (分包, 结算, 信用).</summary>
    static class ZhongbaoUi
    {
        /// <summary>A vertical scroll list filling the given anchors of <paramref name="parent"/>; returns the content rect.</summary>
        public static RectTransform Scroll(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            var viewport = Rect(name, parent, min, max, offMin, offMax);
            Panel(viewport, new Color(1, 1, 1, 0));
            viewport.gameObject.AddComponent<RectMask2D>();
            var list = Rect("List", viewport, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            list.pivot = new Vector2(.5f, 1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = list; scroll.viewport = viewport; scroll.horizontal = false; scroll.scrollSensitivity = 30; scroll.movementType = ScrollRect.MovementType.Clamped;
            return list;
        }

        /// <summary>The scroll list below a box's title strip.</summary>
        public static RectTransform BoxScroll(RectTransform box, string name) => Scroll(box, name, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -(ZhongbaoSkin.HeaderHeight + 1)));

        /// <summary>Lets the pointer rest on a box's title strip for a tooltip.</summary>
        public static void TipOnHeader(RectTransform box, string zh, string en)
        {
            var strip = box.Find("Header") as RectTransform;
            if (strip == null) return;
            Panel(strip, new Color(0, 0, 0, 0));
            UiTip.Add(strip, zh, en);
        }

        public static string Yuan(double v) => "¥" + Money(v);
        public static string Pct(double v) => N(v * 100, "0") + "%";
        public static string Muted(string text) => "<color=#7A7F8C>" + text + "</color>";
    }

    /// <summary>
    /// 分包: the 网吧 friends who label under your account (XgSim.Subcontract.cs). A box 网吧兄弟 with one row per friend
    /// (<see cref="XgMarketSection"/>: face, name, wage, the last results as squares, the desk and hire buttons) and a
    /// box with the YY 网吧 group's chatter.
    /// </summary>
    public sealed class ZhongbaoSubcontractPage : XgPage
    {
        RectTransform list, chatHost;
        TMP_Text title, note, locked, chatTitle;
        XgMarketSection market;
        readonly List<(string who, string zh, string en, RectTransform bubble, TMP_Text text)> chat = new List<(string, string, string, RectTransform, TMP_Text)>();
        string chatKey = "";

        static readonly (string who, string zh, string en)[] Chatter =
        {
            ("ajie", "哥，小刚手慢但不出错，老板只上夜班。我嘛，快是快，就是偶尔点歪。", "Bro, Xiaogang is slow but never wrong, and the boss only works nights. Me? Quick, but I mis-click now and then."),
            ("ajie", "这周电费涨了，工资能不能一分钟加个两毛？", "Power went up this week. Can the wage go up ¥0.2 a minute?"),
            ("xiaogang", "账号要是冻了，我们就在这儿干坐着，工钱照给就行。", "If the account freezes we just sit here. Wages keep running, that's all I ask."),
            ("boss", "夜班没人上机，我闲着也是闲着。天亮我就回去睡。", "Nobody's online at night, so I'm idle anyway. At sunrise I go to bed."),
        };

        public override void Build(RectTransform area)
        {
            root = Rect("subcontract", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var crew = ZhongbaoSkin.Box(ui, root, "Crew", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-332, 0));
            ZhongbaoSkin.Header(ui, crew, "网吧兄弟", "Netbar friends", out title, out note);
            ZhongbaoUi.TipOnHeader(crew, "阿杰、小刚和网吧老板用你的摆渡众包账号标注。平台照样抽检他们的标注：错的罚款、扣信用分、举报都算在你头上；没抽到的错题会变成训练数据里的噪声。工资每分钟照付，账号冻结或等验证码时他们只能干等，钱照样给。",
                "Ajie, Xiaogang and the netbar boss label under your Bodu Crowd account. The platform spot-checks their labels like any other: fines, lost credit and reports all land on you, and unchecked mistakes become noise in your training data. Wages are due every minute, even while a frozen account or a captcha keeps them waiting.");
            list = ZhongbaoUi.BoxScroll(crew, "Viewport");
            market = new XgMarketSection(host, ui, list);
            locked = ui.Text(Rect("Locked", crew, Vector2.zero, Vector2.one, new Vector2(60, 60), new Vector2(-60, -80)), "", 18, ZhongbaoSkin.Mute, TextAlignmentOptions.Center);

            var yy = ZhongbaoSkin.Box(ui, root, "Chat", new Vector2(1, 0), Vector2.one, new Vector2(-320, 0), Vector2.zero);
            ZhongbaoSkin.Header(ui, yy, "YY · 网吧群", "YY · Netbar group", out chatTitle, out _);
            chatHost = Rect("Messages", yy, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -(ZhongbaoSkin.HeaderHeight + 12)));
            foreach (var line in Chatter)
            {
                var bubble = Rect("Message", chatHost, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
                bubble.pivot = new Vector2(.5f, 1);
                Panel(bubble, ZhongbaoSkin.Page).raycastTarget = false;
                var text = ui.Text(Rect("Text", bubble, Vector2.zero, Vector2.one, new Vector2(10, 6), new Vector2(-10, -6)), "", 12, ZhongbaoSkin.Ink, TextAlignmentOptions.TopLeft);
                text.lineSpacing = 6;
                chat.Add((line.who, line.zh, line.en, bubble, text));
            }
            UiTip.Add(yy, "YY 网吧群里的闲话：几个兄弟怎么看自己的活。", "Idle talk in the YY netbar group: how the friends see their own work.");
        }

        void LayoutChat()
        {
            string k = (En ? "en" : "zh") + (int)chatHost.rect.width;
            if (k == chatKey) return;
            chatKey = k;
            float y = 0, width = Mathf.Max(100, chatHost.rect.width - 20);
            foreach (var c in chat)
            {
                string who = Sim.WorkerName(c.who);
                c.text.text = "<color=#2932E1><b>" + who + "</b></color>：" + T(c.zh, c.en);
                float h = c.text.GetPreferredValues(c.text.text, width, 0).y + 14;
                c.bubble.offsetMin = new Vector2(0, -y - h); c.bubble.offsetMax = new Vector2(0, -y);
                y += h + 8;
            }
        }

        public override void Refresh()
        {
            if (root == null || Sim == null) return;
            title.text = T("网吧兄弟", "Netbar friends");
            note.text = T("用你的账号标，按分钟发工资；罚款和举报都算你头上", "They label under your account for wages by the minute; fines and reports land on you");
            chatTitle.text = T("YY · 网吧群", "YY · Netbar group");
            bool open = Sim.SubcontractUnlocked;
            locked.gameObject.SetActive(!open);
            if (!open)
                locked.text = Sim.SubcontractReady
                    ? T("阿杰马上会在 YY 群里问要不要帮你标……", "Ajie is about to ask in the YY group whether you need hands…")
                    : T("还没人来接活。\n\n第 " + XgSim.SubcontractStage + " 阶段起、买下自动答题以后，网吧的阿杰会在 YY 群里问要不要帮你标。",
                        "Nobody has asked for work yet.\n\nFrom stage " + XgSim.SubcontractStage + ", once you own auto-answer, Ajie from the netbar asks in the YY group.");
            list.sizeDelta = new Vector2(0, market.Refresh(0));
            LayoutChat();
        }
    }

    /// <summary>
    /// 结算: the household's bills and the debt countdown on the left; the income per second, the last ten closing
    /// balances and the platform's records (lifetime totals, fines, automatic submissions, this session's notices) on the
    /// right. The sim keeps running totals but no ledger of single payments; the balance history is
    /// <see cref="XgSim.BalanceHistory"/> (filled by <see cref="ZhongbaoHub"/> at each day change) and the notices come
    /// from <see cref="ZhongbaoHub"/> and are not saved.
    /// </summary>
    public sealed class ZhongbaoLedgerPage : XgPage
    {
        const float DebtH = 140, IncomeH = 204, SparkH = 190;
        static readonly Color DebtInk = new Color32(180, 35, 24, 255);

        readonly ZhongbaoHub hub;
        TMP_Text billsTitle, billsNote, incomeTitle, incomeNote, sparkTitle, sparkNote, recordsTitle, debtText, noData;
        RectTransform billsList, recordsList, chart;
        Row bills, income, records;
        readonly TMP_Text[] stepText = new TMP_Text[3];
        readonly Image[] stepFill = new Image[3];
        XgCrawlerLayer chartLayer;
        List<double> chartData = new List<double>();
        double chartRent;
        string chartKey = "";

        public ZhongbaoLedgerPage(ZhongbaoHub hub) { this.hub = hub; }

        public override void Build(RectTransform area)
        {
            root = Rect("ledger", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var left = Rect("Left", root, Vector2.zero, new Vector2(.5f, 1), Vector2.zero, new Vector2(-6, 0));
            var right = Rect("Right", root, new Vector2(.5f, 0), Vector2.one, new Vector2(6, 0), Vector2.zero);

            // ── left: the household's bills, the debt box under them ──
            var billsBox = ZhongbaoSkin.Box(ui, left, "Bills", Vector2.zero, Vector2.one, new Vector2(0, DebtH + 12), Vector2.zero);
            ZhongbaoSkin.Header(ui, billsBox, "家里的开销", "Household bills", out billsTitle, out billsNote);
            ZhongbaoUi.TipOnHeader(billsBox, "房租、训练电费、宽带和夏天的空调：从六月一号起每天扣。下面是每一项现在的价和六月以来的累计。", "Rent, training power, broadband and the summer air con: charged every day from 1 June. Each item's price now and its total since June.");
            billsList = ZhongbaoUi.BoxScroll(billsBox, "Viewport");
            bills = new Row(ui, billsList);

            debtBox = ZhongbaoSkin.TintBox(left, "Debt", Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, DebtH), ZhongbaoSkin.RedLine, ZhongbaoSkin.RedSoft);
            debtText = ui.Text(Rect("Text", debtBox, Vector2.zero, Vector2.one, new Vector2(14, 50), new Vector2(-14, -10)), "", 13, DebtInk, TextAlignmentOptions.TopLeft);
            debtText.lineSpacing = 6;
            var steps = Rect("Steps", debtBox, Vector2.zero, new Vector2(1, 0), new Vector2(14, 14), new Vector2(-14, 42));
            for (int i = 0; i < 3; i++)
            {
                var step = Rect("Step" + (i + 1), steps, new Vector2(i / 3f, 0), new Vector2((i + 1) / 3f, 1), new Vector2(i > 0 ? 3 : 0, 0), new Vector2(i < 2 ? -3 : 0, 0));
                Panel(step, ZhongbaoSkin.RedLine).raycastTarget = false;
                var inner = Rect("Inner", step, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
                stepFill[i] = Panel(inner, Color.white); stepFill[i].raycastTarget = false;
                stepText[i] = ui.Text(Rect("Text", inner, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 12, DebtInk, TextAlignmentOptions.Center);
                stepText[i].textWrappingMode = TextWrappingModes.NoWrap; stepText[i].enableAutoSizing = true; stepText[i].fontSizeMin = 9; stepText[i].fontSizeMax = 12;
            }
            UiTip.Add(debtBox, "钱包低于零就是欠房租：每次每日结算数一天。第 1 天房东来催，第 2 天拉电闸（显卡全停，手动标注还能挣钱），第 3 天电脑卖掉抵房租。钱回到零以上就重新数。",
                "A wallet below zero means rent owed, counted at each daily settlement. Day 1 the landlord texts, day 2 the power is cut (every card stops; hand labelling still pays), day 3 the computer is sold for the rent. Money back above zero resets the count.");

            // ── right: income per second, the balance sparkline, the records ──
            var incomeBox = ZhongbaoSkin.Box(ui, right, "Income", new Vector2(0, 1), Vector2.one, new Vector2(0, -IncomeH), Vector2.zero);
            ZhongbaoSkin.Header(ui, incomeBox, "收入", "Income", out incomeTitle, out incomeNote);
            ZhongbaoUi.TipOnHeader(incomeBox, "现在每秒进出多少钱：企业订单、自动答题进账，分包工资按分钟出账。", "What comes in and goes out right now: orders and auto-answer pay in, subcontract wages pay out by the minute.");
            var incomeHost = Rect("Rows", incomeBox, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -(ZhongbaoSkin.HeaderHeight + 1)));
            income = new Row(ui, incomeHost);

            var sparkBox = ZhongbaoSkin.Box(ui, right, "Balance", new Vector2(0, 1), Vector2.one, new Vector2(0, -IncomeH - 12 - SparkH), new Vector2(0, -IncomeH - 12));
            ZhongbaoSkin.Header(ui, sparkBox, "余额 · 最近 10 天", "Balance · last 10 days", out sparkTitle, out sparkNote);
            ZhongbaoUi.TipOnHeader(sparkBox, "每天结束时钱包里有多少，最多记最近十天。红线是一天的房租：蓝线低于红线，就是一天的房租也交不起。", "What the wallet held at the end of each day, the last ten at most. The red line is one day's rent: a blue line below it cannot pay a day's rent.");
            chart = Rect("Chart", sparkBox, Vector2.zero, Vector2.one, new Vector2(14, 12), new Vector2(-14, -(ZhongbaoSkin.HeaderHeight + 10)));
            chartLayer = ZhongbaoSkin.Canvas(chart, DrawChart);
            noData = ui.Text(Rect("NoData", sparkBox, Vector2.zero, Vector2.one, new Vector2(14, 12), new Vector2(-14, -(ZhongbaoSkin.HeaderHeight + 10))), "", 13, ZhongbaoSkin.Dim, TextAlignmentOptions.Center);

            var recordsBox = ZhongbaoSkin.Box(ui, right, "Records", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -IncomeH - SparkH - 24));
            ZhongbaoSkin.Header(ui, recordsBox, "累计与记录", "Totals and records", out recordsTitle, out _);
            ZhongbaoUi.TipOnHeader(recordsBox, "平台只记累计数和最近几条自动提交，不保存逐笔流水；平台通知只在这次开机里有。", "The platform keeps running totals and the last few automatic submissions, not every single payment; platform notices exist for this session only.");
            recordsList = ZhongbaoUi.BoxScroll(recordsBox, "Viewport");
            records = new Row(ui, recordsList);
        }

        RectTransform debtBox;

        // ───────────── the sparkline ─────────────

        void DrawChart(VertexHelper vh)
        {
            var v = chartData;
            if (v == null || v.Count < 2) return;
            var r = chart.rect;
            const float pad = 8;
            double max = chartRent * 1.5, min = 0;
            foreach (var x in v) { if (x > max) max = x; if (x < min) min = x; }
            if (max - min < 1) max = min + 1;
            float Y(double value) => r.yMin + pad + (float)((value - min) / (max - min)) * (r.height - pad * 2);
            if (min < 0) XgDraw.Seg(vh, new Vector2(r.xMin, Y(0)), new Vector2(r.xMax, Y(0)), 1, ZhongbaoSkin.Line);
            // One day of rent: the red dashed line.
            if (chartRent > 0) XgDraw.Dashed(vh, new Vector2(r.xMin, Y(chartRent)), new Vector2(r.xMax, Y(chartRent)), 1.5f, 5, ZhongbaoSkin.Red);
            var pts = new List<Vector2>(v.Count);
            for (int i = 0; i < v.Count; i++) pts.Add(new Vector2(r.xMin + pad + i * (r.width - pad * 2) / (v.Count - 1), Y(v[i])));
            XgDraw.Polyline(vh, pts, 2.5f, ZhongbaoSkin.Blue2);
            for (int i = 0; i < pts.Count; i++) XgDraw.Disc(vh, pts[i], i == pts.Count - 1 ? 4.5f : 3, i == pts.Count - 1 ? ZhongbaoSkin.Blue : ZhongbaoSkin.Blue2, 14);
        }

        // ───────────── refresh ─────────────

        static Color MoneyInk(double v) => v > 0 ? ZhongbaoSkin.Red : ZhongbaoSkin.Mute;

        void AddHouseBills(List<Row.Item> items, ChapterOneSim house)
        {
            var s = house.S;
            var today = GameCalendar.Now(s).Date;
            var cfg = house.Config;
            if (!house.EconomyActive)
            {
                items.Add(Row.Note(T("6 月 1 日起交房租，从那天起家里的开销都记在这里。", "Rent starts on 1 June; from then on the household's bills are listed here.")));
                items.Add(Row.Row(T("房租", "Rent"), "−" + ZhongbaoUi.Yuan(cfg.rentPerDay) + T("/天", "/day"), ZhongbaoSkin.Red));
                items.Add(Row.Row(T("宽带（每月 1 号）", "Broadband (the 1st of each month)"), "−" + ZhongbaoUi.Yuan(cfg.broadbandPerMonth), ZhongbaoSkin.Red));
                return;
            }
            var t = s.billsToday ?? new HouseBills();
            double todayTotal = s.billsDay == GameCalendar.CurrentDay(s) ? t.Total : 0;
            items.Add(Row.Row(T(today.Month + " 月 " + today.Day + " 日账单", "Bill for " + today.ToString("d MMM", System.Globalization.CultureInfo.InvariantCulture)), "−" + ZhongbaoUi.Yuan(todayTotal), MoneyInk(todayTotal)));
            items.Add(Row.Row(T("房租", "Rent") + (today < ChapterOneSim.RentRiseDate ? ZhongbaoUi.Muted(T("（10 月起 ¥" + Money(cfg.rentPerDayFromOctober) + "）", " (¥" + Money(cfg.rentPerDayFromOctober) + " from October)")) : ""),
                "−" + ZhongbaoUi.Yuan(house.RentFor(today)) + T("/天", "/day"), ZhongbaoSkin.Red));
            items.Add(Row.Row(T("训练电费 · 本月 " + Money(house.MonthKwh) + " 度 · 第 " + house.PowerTier + " 档", "Training power · " + Money(house.MonthKwh) + " kWh this month · tier " + house.PowerTier),
                T("约 ¥", "about ¥") + N(house.TrainingRoundPrice(XgSim.EpochSeconds), "0.0") + T("/轮", "/round"), ZhongbaoSkin.Red));
            items.Add(Row.Sub(T("阶梯电价：每月 " + Money(cfg.tierOneKwh) + " 度以上 ×" + N(cfg.tierTwoFactor, "0.0#") + "，" + Money(cfg.tierTwoKwh) + " 度以上 ×" + N(cfg.tierThreeFactor, "0.0#"),
                "Tiered pricing: above " + Money(cfg.tierOneKwh) + " kWh a month ×" + N(cfg.tierTwoFactor, "0.0#") + ", above " + Money(cfg.tierTwoKwh) + " kWh ×" + N(cfg.tierThreeFactor, "0.0#"))));
            items.Add(Row.Row(T("宽带（每月 1 号）", "Broadband (the 1st of each month)"), "−" + ZhongbaoUi.Yuan(cfg.broadbandPerMonth), ZhongbaoSkin.Red));
            items.Add(Row.Row(T("夏天空调（7、8 月）", "Summer air con (July, August)"), "−" + ZhongbaoUi.Yuan(cfg.summerAirconPerDay) + T("/天", "/day"), ZhongbaoSkin.Red));
            if (s.repairs != null && s.repairs.Count > 0) items.Add(Row.Row(T("送修中的显卡", "Cards at the repair shop"), s.repairs.Count.ToString()));
            if (s.ticketDay > 0) items.Add(Row.Row(T("周末火车票", "Weekend train ticket"), T("周六扣 ¥", "¥") + Money(cfg.weekendTicket) + T("", " on Saturday"), ZhongbaoSkin.Red));
            var b = s.bills ?? new HouseBills();
            items.Add(Row.Head(T("6 月以来", "Since June")));
            items.Add(Row.Row(T("房租", "Rent"), "−" + ZhongbaoUi.Yuan(b.rent), MoneyInk(b.rent)));
            items.Add(Row.Row(T("训练电费", "Training power"), "−" + ZhongbaoUi.Yuan(b.power), MoneyInk(b.power)));
            items.Add(Row.Row(T("待机电费", "Idle power"), "−" + ZhongbaoUi.Yuan(b.idlePower), MoneyInk(b.idlePower)));
            if (b.aircon > 0) items.Add(Row.Row(T("夏天空调", "Summer air con"), "−" + ZhongbaoUi.Yuan(b.aircon), ZhongbaoSkin.Red));
            items.Add(Row.Row(T("宽带", "Broadband"), "−" + ZhongbaoUi.Yuan(b.broadband), MoneyInk(b.broadband)));
            items.Add(Row.Row(T("修显卡", "Card repairs"), "−" + ZhongbaoUi.Yuan(b.repair), MoneyInk(b.repair)));
            if (b.breaker > 0) items.Add(Row.Row(T("修空开", "Breaker repairs"), "−" + ZhongbaoUi.Yuan(b.breaker), ZhongbaoSkin.Red));
            if (b.ticket > 0) items.Add(Row.Row(T("车票", "Train tickets"), "−" + ZhongbaoUi.Yuan(b.ticket), ZhongbaoSkin.Red));
            items.Add(Row.Total(T("合计", "Total"), "−" + ZhongbaoUi.Yuan(b.Total), MoneyInk(b.Total)));
        }

        /// <summary>The debt box: how many days of rent the wallet covers, or the countdown with the current step lit.</summary>
        void RefreshDebt(ChapterOneSim house)
        {
            int current = 0;
            string text;
            if (house == null) text = "";
            else if (!house.EconomyActive)
                text = T("6 月 1 日起交房租：一天 ¥" + Money(house.Config.rentPerDay) + "。欠钱以后：", "Rent starts on 1 June: ¥" + Money(house.Config.rentPerDay) + " a day. Once you owe it:");
            else if (house.S.bankrupt)
            {
                current = 3;
                text = T("<b>电脑已经卖了（¥" + Money(house.S.soldFor) + "）。</b>房租交上了，可你的机器没了。", "<b>The computer was sold (¥" + Money(house.S.soldFor) + ").</b> The rent is paid, but your machine is gone.");
            }
            else if (house.Debt > 0)
            {
                current = Mathf.Clamp(Mathf.Max(house.S.debtDays, house.S.landlordCut ? 2 : 0), 1, 3);
                text = T("<b>欠房租 ¥" + Money(house.Debt) + (house.S.landlordCut ? "，房东拉了电闸" : "") + "。</b>连着三天交不上，电脑就得卖了。",
                    "<b>Rent owed ¥" + Money(house.Debt) + (house.S.landlordCut ? "; the landlord cut the power" : "") + ".</b> Three days without it and the computer has to be sold.");
            }
            else
            {
                double rent = house.RentFor(GameCalendar.Now(house.S).Date);
                int days = rent > 0 ? (int)Math.Floor(house.S.money / rent) : 0;
                text = T("余额够交 <b>" + days + "</b> 天房租。欠钱以后：", "The balance covers <b>" + days + "</b> day" + (days == 1 ? "" : "s") + " of rent. Once you owe it:");
            }
            debtText.text = text;
            string[] steps = { T("第 1 天 房东催", "Day 1 landlord texts"), T("第 2 天 拉电闸", "Day 2 power cut"), T("第 3 天 卖电脑", "Day 3 computer sold") };
            for (int i = 0; i < 3; i++)
            {
                bool on = current == i + 1;
                stepText[i].text = steps[i];
                stepText[i].color = on ? Color.white : DebtInk;
                stepText[i].fontStyle = on ? FontStyles.Bold : FontStyles.Normal;
                stepFill[i].color = on ? ZhongbaoSkin.Red : Color.white;
            }
        }

        public override void Refresh()
        {
            if (root == null || Sim == null) return;
            var s = Sim.S;
            var house = hub != null && hub.runtime != null ? hub.runtime.Sim : null;
            billsTitle.text = T("家里的开销", "Household bills");
            billsNote.text = house != null ? T(GameCalendar.Now(house.S).Month + " 月" + GameCalendar.Now(house.S).Day + " 日", GameCalendar.Now(house.S).ToString("d MMM", System.Globalization.CultureInfo.InvariantCulture)) : "";
            incomeTitle.text = T("收入", "Income"); incomeNote.text = T("现在每秒", "Per second now");
            sparkTitle.text = T("余额 · 最近 10 天", "Balance · last 10 days"); sparkNote.text = T("红线 = 一天的房租", "Red line = one day's rent");
            recordsTitle.text = T("累计与记录", "Totals and records");

            // ── bills + debt ──
            var items = new List<Row.Item>();
            if (house != null) AddHouseBills(items, house);
            billsList.sizeDelta = new Vector2(0, bills.Set(items));
            RefreshDebt(house);

            // ── income per second ──
            double contracts = Sim.IncomePerSecond, auto = 0, wages = Sim.WagesPerMinute;
            foreach (var l in s.autoLevels) auto += Sim.AutoIncome(l.dataset, Host);
            int signedCount = 0; foreach (var c in XgCatalog.Contracts) if (Sim.Signed(c.id)) signedCount++;
            double net = contracts + auto - wages / XgSim.WageSeconds;
            double dayLength = house != null ? house.Config.dayLengthSeconds : 120;
            items.Clear();
            items.Add(Row.Row(T("企业订单 · " + signedCount + " 单", "Business orders · " + signedCount + " signed"), "+" + ZhongbaoUi.Yuan(contracts) + T("/秒", "/s"), ZhongbaoSkin.Green));
            items.Add(Row.Row(T("自动答题", "Auto-answer") + (Sim.QualityFrozen ? ZhongbaoUi.Muted(T("（账号冻结中，暂停）", " (account frozen, paused)")) : ""), "+" + ZhongbaoUi.Yuan(auto) + T("/秒", "/s"), ZhongbaoSkin.Green));
            items.Add(Row.Row(T("分包工资", "Subcontract wages"), wages > 0 ? "−" + ZhongbaoUi.Yuan(wages) + T("/分钟", "/min") : "—", wages > 0 ? ZhongbaoSkin.Red : ZhongbaoSkin.Mute));
            items.Add(Row.Total(T("合计约", "About"), (net >= 0 ? "+" : "−") + ZhongbaoUi.Yuan(Math.Abs(net)) + T("/秒 ≈ ", "/s ≈ ") + (net >= 0 ? "" : "−") + ZhongbaoUi.Yuan(Math.Abs(net) * dayLength) + T("/天", "/day"), net >= 0 ? ZhongbaoSkin.Green : ZhongbaoSkin.Red));
            income.Set(items);

            // ── balance history ──
            var history = Sim.BalanceHistory();
            double rent = house != null ? house.RentFor(GameCalendar.Now(house.S).Date) : 0;
            string key = rent + ":" + string.Join(",", history);
            if (key != chartKey) { chartKey = key; chartData = history; chartRent = rent; chartLayer.Redraw(); }
            noData.gameObject.SetActive(history.Count < 2);
            noData.text = T("还没有数据：每过一天记一个点，两天以后这里会画出线来。", "No data yet: one point is recorded each day, and a line appears after two days.");

            // ── records ──
            items.Clear();
            items.Add(Row.Head(T("存档里记下的累计", "Totals kept in the save")));
            items.Add(Row.Row(T("实验室总收入", "Lab income"), ZhongbaoUi.Yuan(s.totalIncome), ZhongbaoSkin.Green));
            items.Add(Row.Row(T("实验室总支出", "Lab spending"), ZhongbaoUi.Yuan(s.totalSpent), MoneyInk(s.totalSpent)));
            double bonuses = 0; int signed = 0;
            foreach (var c in XgCatalog.Contracts) if (Sim.Signed(c.id)) { bonuses += c.signBonus; signed++; }
            items.Add(Row.Row(T("签约首付", "Signing advances"), ZhongbaoUi.Yuan(bonuses) + T("（" + signed + " 单）", " (" + signed + " signed)")));
            foreach (var c in XgCatalog.Contracts)
                if (Sim.Signed(c.id))
                    items.Add(Row.Sub(T(c.client, c.clientEn) + " · " + T(c.job, c.jobEn), "¥" + Money(Sim.ContractIncome(c)) + T("/秒", "/s")));
            items.Add(Row.Row(T("抽检罚款", "Spot-check fines"), "−" + ZhongbaoUi.Yuan(s.qcFines), MoneyInk(s.qcFines)));
            items.Add(Row.Row(T("点错罚款（算术）", "Wrong-pick fines (arithmetic)"), "−" + ZhongbaoUi.Yuan(s.handFines), MoneyInk(s.handFines)));
            items.Add(Row.Sub(T("抽检 " + s.qcCheckedTotal + " 次 · 不合格 " + s.qcFailedTotal + " 次 · 其中金标题 " + s.qcTrapFails + " 次 · 举报 " + s.qcReports + " 次",
                s.qcCheckedTotal + " checks · " + s.qcFailedTotal + " failed · " + s.qcTrapFails + " on trap items · " + s.qcReports + " reports")));
            if (s.scUnlocked)
                foreach (var info in XgSim.Workers)
                {
                    var w = Sim.Worker(info.id);
                    if (w == null || w.labelsTotal == 0 && w.wagesPaid <= 0) continue;
                    items.Add(Row.Row(T("分包 · " + info.name, "Subcontract · " + info.nameEn), T("工资 −¥" + Money(w.wagesPaid) + " · 挣 +¥" + Money(w.earned) + " · 罚 −¥" + Money(w.fines),
                        "wages −¥" + Money(w.wagesPaid) + " · earned +¥" + Money(w.earned) + " · fined −¥" + Money(w.fines))));
                }

            items.Add(Row.Head(T("最近的自动提交", "Latest automatic submissions")));
            if (s.autoFeed.Count == 0) items.Add(Row.Note(T("还没有。买下自动答题以后，模型交出去的题会出现在这里。", "None yet. Once you own auto-answer, the labels the model hands in show here.")));
            for (int i = s.autoFeed.Count - 1; i >= 0; i--)
            {
                var r = s.autoFeed[i];
                var desk = XgCatalog.Desk(r.dataset);
                string name = desk != null ? T(desk.name, desk.nameEn) : r.dataset;
                if (r.audited) items.Add(Row.Row("<color=#8C540A>↖ " + name + T("  审核拦下，回到待复核", "  caught by the audit, back to review") + "</color>", "", null));
                else if (r.spotChecked && !r.correct) items.Add(Row.Row("<color=#BE1414>× " + name + T("  抽检不合格", "  failed spot check") + "</color>", "−¥" + N(r.fine, "0.##"), ZhongbaoSkin.Red));
                else if (r.correct) items.Add(Row.Row("<color=#2F9E44>✓ " + name + (r.spotChecked ? T("（抽检合格）", " (spot-checked, passed)") : "") + "</color>", "+¥" + N(r.pay, "0.##"), ZhongbaoSkin.Green));
                else items.Add(Row.Row("<color=#D63031>× " + name + T("  错了，没被抽到（成了噪声）", "  wrong, not checked (now noise)") + "</color>", T("噪声", "noise"), ZhongbaoSkin.Mute));
            }

            items.Add(Row.Head(T("这次开机以来的平台通知", "Platform notices since the game started")));
            var notices = hub != null ? hub.Notices : null;
            if (notices == null || notices.Count == 0) items.Add(Row.Note(T("暂无。", "None yet.")));
            else foreach (var n in notices) items.Add(Row.Sub(ZhongbaoUi.Muted(n.clock) + "  " + T(n.zh, n.en)));
            if (hub != null && hub.SessionFines > 0) items.Add(Row.Row(T("本次罚款合计", "Fines this session"), "−" + ZhongbaoUi.Yuan(hub.SessionFines), ZhongbaoSkin.Red));
            items.Add(Row.Note(T("（平台通知不存档，重新开游戏会清空。）", "(Notices are not saved: this list starts empty each time the game starts.)")));
            recordsList.sizeDelta = new Vector2(0, records.Set(items));
        }
    }

    /// <summary>
    /// 信用: the platform's credit score and the rules that move it, read from XgSim.Quality / QualityTraps / Captcha:
    /// a half-circle dial with the three tiers, the tier table, what moves credit, and (as rows) the account's state,
    /// the lifetime counts, the report rules and the data noise behind 近亲繁殖 (XgSim.Inbreeding), which does not touch
    /// credit but is the other cost of bad labels. Numbers come from the sim's own constants.
    /// </summary>
    public sealed class ZhongbaoCreditPage : XgPage
    {
        const float LeftW = 330, TierH = 176, RulesH = 292;

        TMP_Text number, tierLine, passLine, reportLine, dialZero, dialMax, tiersTitle, rulesTitle, moreTitle, tiersNote;
        RectTransform dial, statusList, rulesList, moreList;
        XgCrawlerLayer dialLayer;
        Row status, rules, more;
        readonly TMP_Text[][] tierCells = new TMP_Text[4][];
        readonly Image[] tierBands = new Image[3];
        readonly TMP_Text[] tierHead = new TMP_Text[4];
        double dialCredit = -1;

        static Color TierInk(XgCreditTier t) => t == XgCreditTier.Gold ? ZhongbaoSkin.Green : t == XgCreditTier.Normal ? ZhongbaoSkin.Blue : ZhongbaoSkin.Red;

        public override void Build(RectTransform area)
        {
            root = Rect("credit", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ── left: the dial ──
            var left = ZhongbaoSkin.Box(ui, root, "Dial", Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(LeftW, 0));
            dial = Rect("Dial", left, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-140, -170), new Vector2(140, -20));
            dialLayer = ZhongbaoSkin.Canvas(dial, DrawDial);
            number = ui.Text(Rect("Number", dial, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-60, 0), new Vector2(60, 56)), "", 44, ZhongbaoSkin.Green, TextAlignmentOptions.Center);
            number.fontStyle = FontStyles.Bold; number.textWrappingMode = TextWrappingModes.NoWrap;
            dialZero = ui.Text(Rect("Zero", dial, Vector2.zero, Vector2.zero, new Vector2(-15, -16), new Vector2(35, 0)), "0", 11, ZhongbaoSkin.Dim, TextAlignmentOptions.Center);
            dialMax = ui.Text(Rect("Max", dial, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-35, -16), new Vector2(15, 0)), N(XgSim.QcCreditMax, "0"), 11, ZhongbaoSkin.Dim, TextAlignmentOptions.Center);
            tierLine = ui.Text(Rect("Tier", left, new Vector2(0, 1), Vector2.one, new Vector2(12, -222), new Vector2(-12, -192)), "", 14, ZhongbaoSkin.Ink, TextAlignmentOptions.Center);
            tierLine.textWrappingMode = TextWrappingModes.NoWrap; tierLine.enableAutoSizing = true; tierLine.fontSizeMin = 10; tierLine.fontSizeMax = 14;
            passLine = ui.Text(Rect("Pass", left, new Vector2(0, 1), Vector2.one, new Vector2(12, -246), new Vector2(-12, -224)), "", 12, ZhongbaoSkin.Mute, TextAlignmentOptions.Center);
            passLine.textWrappingMode = TextWrappingModes.NoWrap; passLine.enableAutoSizing = true; passLine.fontSizeMin = 9; passLine.fontSizeMax = 12;
            reportLine = ui.Text(Rect("Reports", left, new Vector2(0, 1), Vector2.one, new Vector2(12, -268), new Vector2(-12, -246)), "", 12, ZhongbaoSkin.Mute, TextAlignmentOptions.Center);
            reportLine.textWrappingMode = TextWrappingModes.NoWrap; reportLine.enableAutoSizing = true; reportLine.fontSizeMin = 9; reportLine.fontSizeMax = 12;
            Panel(Rect("Divide", left, new Vector2(0, 1), Vector2.one, new Vector2(12, -279), new Vector2(-12, -278)), ZhongbaoSkin.Line).raycastTarget = false;
            statusList = ZhongbaoUi.Scroll(left, "Status", Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -286));
            status = new Row(ui, statusList);
            UiTip.Add(dial, "信用表盘：红 = 重点关注，蓝 = 普通，绿 = 金牌；指针指着现在的信用分。", "Credit dial: red = watch list, blue = normal, green = gold; the needle shows the credit now.");

            // ── right: tiers, what moves credit, the rest ──
            var right = Rect("Right", root, Vector2.zero, Vector2.one, new Vector2(LeftW + 12, 0), Vector2.zero);
            var tiersBox = ZhongbaoSkin.Box(ui, right, "Tiers", new Vector2(0, 1), Vector2.one, new Vector2(0, -TierH), Vector2.zero);
            ZhongbaoSkin.Header(ui, tiersBox, "信用档位", "Credit tiers", out tiersTitle, out tiersNote);
            ZhongbaoUi.TipOnHeader(tiersBox, "信用分决定平台抽检你多少、自动收入打几折或加几成。手动标注从不抽检。", "Your credit sets how often the platform spot-checks you and the multiplier on auto income. Hand labels are never checked.");
            string[] headers = { "档位", "信用分", "抽检", "自动收入" };
            float[] fromLeft = { 14, 120, 240, 400 };
            float headY = ZhongbaoSkin.HeaderHeight;
            for (int c = 0; c < 4; c++)
            {
                tierHead[c] = ui.Text(Rect("Head" + c, tiersBox, new Vector2(0, 1), new Vector2(0, 1), new Vector2(fromLeft[c], -headY - 26), new Vector2(fromLeft[c] + 150, -headY - 4)), headers[c], 11, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
                tierHead[c].textWrappingMode = TextWrappingModes.NoWrap;
            }
            for (int r = 0; r < 3; r++)
            {
                float y = headY + 26 + r * 32;
                var band = Rect("Tier" + r, tiersBox, new Vector2(0, 1), Vector2.one, new Vector2(1, -y - 32), new Vector2(-1, -y));
                tierBands[r] = Panel(band, Color.clear); tierBands[r].raycastTarget = false;
                var dash = Rect("Dash", band, Vector2.zero, new Vector2(1, 0), new Vector2(12, 0), new Vector2(-12, 1));
                ZhongbaoSkin.Canvas(dash, vh => { var rr = dash.rect; XgDraw.Dashed(vh, new Vector2(rr.xMin, rr.center.y), new Vector2(rr.xMax, rr.center.y), 1, 3, ZhongbaoSkin.Line); });
                tierCells[r] = new TMP_Text[4];
                for (int c = 0; c < 4; c++)
                {
                    tierCells[r][c] = ui.Text(Rect("Cell" + c, band, Vector2.zero, Vector2.one, new Vector2(fromLeft[c] - 1, 0), new Vector2(-4, 0)), "", 13, ZhongbaoSkin.Ink, TextAlignmentOptions.MidlineLeft);
                    tierCells[r][c].textWrappingMode = TextWrappingModes.NoWrap;
                }
            }

            var rulesBox = ZhongbaoSkin.Box(ui, right, "Rules", new Vector2(0, 1), Vector2.one, new Vector2(0, -TierH - 12 - RulesH), new Vector2(0, -TierH - 12));
            ZhongbaoSkin.Header(ui, rulesBox, "什么会让信用变", "What moves credit", out rulesTitle, out _);
            rulesList = ZhongbaoUi.BoxScroll(rulesBox, "Viewport");
            rules = new Row(ui, rulesList);

            var moreBox = ZhongbaoSkin.Box(ui, right, "More", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -TierH - RulesH - 24));
            ZhongbaoSkin.Header(ui, moreBox, "举报规则与数据噪声", "Report rules and data noise", out moreTitle, out _);
            moreList = ZhongbaoUi.BoxScroll(moreBox, "Viewport");
            more = new Row(ui, moreList);
        }

        // ───────────── the dial ─────────────

        void DrawDial(VertexHelper vh)
        {
            var r = dial.rect;
            const float width = 16;
            float radius = Mathf.Min(r.width * .5f, r.height - width) - 4;
            var c = new Vector2(r.center.x, r.yMin + width * .5f + 2);
            float max = (float)XgSim.QcCreditMax;
            float watch = (float)XgSim.QcCreditWatch / max, gold = (float)XgSim.QcCreditGold / max;
            float A(float f) => Mathf.PI * (1 - f);
            const float gap = .012f;
            XgDraw.Arc(vh, c, radius, width, A(0), A(watch) + gap, ZhongbaoSkin.Red, 24);
            XgDraw.Arc(vh, c, radius, width, A(watch), A(gold) + gap, ZhongbaoSkin.Blue2, 24);
            XgDraw.Arc(vh, c, radius, width, A(gold), A(1), ZhongbaoSkin.Green, 12);
            // The needle: from inside the number's room out past the ring, and a hub.
            float f = Mathf.Clamp01((float)(dialCredit < 0 ? 0 : dialCredit) / max);
            var dir = new Vector2(Mathf.Cos(A(f)), Mathf.Sin(A(f)));
            XgDraw.Seg(vh, c + dir * 60, c + dir * (radius + width * .5f + 2), 3, ZhongbaoSkin.Ink);
            XgDraw.Disc(vh, c + dir * 60, 3.5f, ZhongbaoSkin.Ink, 12);
        }

        // ───────────── the table and the rules ─────────────

        static string Signed(double v) => (v >= 0 ? "+" : "−") + N(Math.Abs(v), "0.#");

        static Row.Item Rule(double delta, string zh, string en)
            => Row.Row(T(zh, en), Signed(delta), delta >= 0 ? ZhongbaoSkin.Green : ZhongbaoSkin.Red);

        public override void Refresh()
        {
            if (root == null || Sim == null) return;
            var s = Sim.S;
            var tier = Sim.CreditTier;
            bool active = Sim.QualityActive;
            string tierName = Sim.CreditTierName(tier);
            var ink = TierInk(tier);

            // ── dial ──
            if (Math.Abs(dialCredit - Sim.Credit) > 1e-6) { dialCredit = Sim.Credit; dialLayer.Redraw(); }
            number.text = N(Math.Floor(Sim.Credit), "0"); number.color = active ? ink : ZhongbaoSkin.Mute;
            tierLine.text = "<color=" + ZhongbaoSkin.Hex(ink) + "><b>" + tierName + "</b></color>" + (active ? "" : ZhongbaoUi.Muted(T("（未开通）", " (inactive)")))
                + T(" · 抽检 ", " · checked ") + ZhongbaoUi.Pct(active ? Sim.SpotCheckChance : XgSim.CheckChanceOf(tier)) + T(" · 自动收入 ×", " · auto income ×") + N(XgSim.PayMultiplierOf(tier), "0.0#");
            string rate = Sim.SpotChecks == 0 ? T("还没有抽检", "no checks yet")
                : T("合格率 " + ZhongbaoUi.Pct(Sim.PassRate) + "（近 " + Sim.SpotChecks + " 次）", "pass rate " + ZhongbaoUi.Pct(Sim.PassRate) + " (last " + Sim.SpotChecks + ")");
            if (Sim.SpotChecks > 0 && !Sim.PassRateJudged) rate += T("  满 " + XgSim.QcMinChecks + " 次才评定", "  judged from " + XgSim.QcMinChecks);
            passLine.text = Sim.QualityWarning ? "<color=#D63031>" + rate + T(" · 已警告", " · warned") + "</color>" : rate;
            reportLine.text = T("已被举报 " + s.qcReports + " 次 · 下次冻结 " + N(XgSim.FreezeSecondsFor(s.qcReports + 1) / 60, "0") + " 分钟",
                "Reported " + s.qcReports + " times · next freeze " + N(XgSim.FreezeSecondsFor(s.qcReports + 1) / 60, "0") + " min");

            // ── the tier table ──
            tiersTitle.text = T("信用档位", "Credit tiers");
            string[] heads = { T("档位", "Tier"), T("信用分", "Credit"), T("抽检", "Spot checks"), T("自动收入", "Auto income") };
            for (int c = 0; c < 4; c++) tierHead[c].text = heads[c];
            var order = new[] { XgCreditTier.Gold, XgCreditTier.Normal, XgCreditTier.Watch };
            string[] ranges = { "≥ " + N(XgSim.QcCreditGold, "0"), N(XgSim.QcCreditWatch, "0") + "–" + N(XgSim.QcCreditGold - 1, "0"), "< " + N(XgSim.QcCreditWatch, "0") };
            for (int r = 0; r < 3; r++)
            {
                bool on = order[r] == tier;
                tierBands[r].color = on ? ZhongbaoSkin.Blue3 : Color.clear;
                string[] cells = { Sim.CreditTierName(order[r]), ranges[r], ZhongbaoUi.Pct(XgSim.CheckChanceOf(order[r])), "×" + N(XgSim.PayMultiplierOf(order[r]), "0.0#") };
                for (int c = 0; c < 4; c++)
                {
                    tierCells[r][c].text = cells[c];
                    tierCells[r][c].fontStyle = on ? FontStyles.Bold : FontStyles.Normal;
                    tierCells[r][c].color = c == 0 ? TierInk(order[r]) : ZhongbaoSkin.Ink;
                }
            }

            // ── the account, under the dial ──
            var items = new List<Row.Item>();
            if (!active)
                items.Add(Row.Note(T("平台还没开始抽检：买下自动答题以后，模型交出去的题才会被抽检、计信用。手动标注从不抽检。",
                    "The platform is not checking yet: it starts spot-checking (and scoring credit) once you own auto-answer. Hand labels are never checked.")));
            if (Sim.QualityFrozen) items.Add(Row.Note("<b>" + T("账号冻结中 ", "Frozen ") + XgSim.FreezeClock(Sim.FreezeSecondsLeft) + "</b> · " + Sim.ReportReasonText(Sim.LastReportReason), ZhongbaoSkin.Red));
            if (Sim.CaptchaPending) items.Add(Row.Note("<b>" + T("等待人机验证 ", "Captcha waiting ") + XgSim.FreezeClock(Sim.CaptchaSecondsLeft) + "</b>" + T("（在标注台输入）", " (enter it on the labelling desk)"), ZhongbaoSkin.Red));
            else if (Sim.CaptchaPauseLeft > 0) items.Add(Row.Note(T("验证未通过，自动标注暂停 ", "Captcha failed, auto labelling paused ") + XgSim.FreezeClock(Sim.CaptchaPauseLeft), ZhongbaoSkin.Orange));
            if (Sim.MonotoneAnswers) items.Add(Row.Note(T("最近的自动答案几乎全是同一个：像脚本。", "Recent automatic answers are nearly all the same: it looks like a script."), ZhongbaoSkin.Orange));
            items.Add(Row.Head(T("累计", "Lifetime")));
            items.Add(Row.Row(T("抽检", "Spot checks"), s.qcCheckedTotal.ToString()));
            items.Add(Row.Row(T("不合格（其中金标题）", "Failed (of which trap items)"), s.qcFailedTotal + " (" + s.qcTrapFails + ")", s.qcFailedTotal > 0 ? ZhongbaoSkin.Red : (Color?)null));
            items.Add(Row.Row(T("罚款", "Fines"), "−" + ZhongbaoUi.Yuan(s.qcFines), s.qcFines > 0 ? ZhongbaoSkin.Red : ZhongbaoSkin.Mute));
            items.Add(Row.Row(T("人机验证", "Captchas"), s.qcCaptchaTotal.ToString()));
            items.Add(Row.Row(T("验证码代填 / 被识破", "Autofilled / spotted"), s.qcAutofills + " / " + s.qcAutofillFlags));
            if (Sim.TrapsRemembered > 0) items.Add(Row.Row(T("记住的金标题", "Trap items remembered"), Sim.TrapsRemembered.ToString()));
            statusList.sizeDelta = new Vector2(0, status.Set(items));

            // ── what moves credit ──
            rulesTitle.text = T("什么会让信用变", "What moves credit");
            items.Clear();
            items.Add(Rule(XgSim.QcCreditPass, "抽检合格", "A spot check passes"));
            items.Add(Rule(-XgSim.QcCreditFail, "抽检不合格（罚该题报酬 ×" + N(XgSim.QcFineMultiplier, "0") + "）", "A spot check fails (fine: the card's pay ×" + N(XgSim.QcFineMultiplier, "0") + ")"));
            items.Add(Rule(-XgSim.TrapCreditFail, "金标题答错（平台预置的已知答案题，罚 ×" + N(XgSim.TrapFineMultiplier, "0") + "，在抽检记录里算两次）", "A trap item is wrong (the platform knew the answer; fine ×" + N(XgSim.TrapFineMultiplier, "0") + ", counts twice in the window)"));
            items.Add(Rule(-XgSim.QcCreditReport, "被举报（冻结 " + N(XgSim.QcFreezeSeconds[0] / 60, "0") + " / " + N(XgSim.QcFreezeSeconds[1] / 60, "0") + " / " + N(XgSim.QcFreezeSeconds[2] / 60, "0") + " 分钟）",
                "Reported (frozen " + N(XgSim.QcFreezeSeconds[0] / 60, "0") + " / " + N(XgSim.QcFreezeSeconds[1] / 60, "0") + " / " + N(XgSim.QcFreezeSeconds[2] / 60, "0") + " min)"));
            items.Add(Rule(XgSim.QcCreditHand, "冻结期间每手动标对一条", "Each right hand label while frozen"));
            items.Add(Rule(XgSim.CaptchaCreditPass, "人机验证通过", "A captcha passed"));
            items.Add(Rule(-XgSim.CaptchaCreditFail, "验证码答错或超时（自动标注暂停 " + N(XgSim.CaptchaPause / 60, "0") + " 分钟；连错 " + XgSim.CaptchaFailsToReport + " 次按机器处理）",
                "A captcha wrong or late (auto labelling pauses " + N(XgSim.CaptchaPause / 60, "0") + " min; " + XgSim.CaptchaFailsToReport + " in a row counts as a bot)"));
            items.Add(Rule(-XgSim.AutofillFlagCredit, "验证码代填被识破（每次 " + ZhongbaoUi.Pct(XgSim.AutofillFlagChance) + " 的可能）", "Captcha autofill spotted (" + ZhongbaoUi.Pct(XgSim.AutofillFlagChance) + " chance each time)"));
            rulesList.sizeDelta = new Vector2(0, rules.Set(items));

            // ── report rules and the data noise ──
            moreTitle.text = T("举报规则与数据噪声", "Report rules and data noise");
            items.Clear();
            items.Add(Row.Head(T("举报规则", "When the account is reported")));
            items.Add(Row.Note(T("平台每 " + XgSim.QcJudgeEvery + " 次抽检评一次，看最近 " + XgSim.QcWindow + " 次（满 " + XgSim.QcMinChecks + " 次才评）：\n"
                    + "· 错误率 ≥ " + ZhongbaoUi.Pct(XgSim.QcWarnRate) + " 警告；\n"
                    + "· 满 " + XgSim.QcWindow + " 次且错误率 ≥ " + ZhongbaoUi.Pct(XgSim.QcReportRate) + " 举报；\n"
                    + "· 答案 " + ZhongbaoUi.Pct(XgSim.QcMonotoneShare) + " 以上是同一个，像脚本：错误率到 " + ZhongbaoUi.Pct(XgSim.QcWarnRate) + " 就举报。\n"
                    + "冻结期间自动标注不能提交；申诉付 ¥" + N(XgSim.QcAppealMin, "0") + " 或余额的 " + ZhongbaoUi.Pct(XgSim.QcAppealShare) + "（取较多者），信用不变。",
                "The platform judges every " + XgSim.QcJudgeEvery + " checks over the last " + XgSim.QcWindow + " (once there are " + XgSim.QcMinChecks + "):\n"
                    + "· an error rate of " + ZhongbaoUi.Pct(XgSim.QcWarnRate) + " or more warns;\n"
                    + "· with a full " + XgSim.QcWindow + " checks, " + ZhongbaoUi.Pct(XgSim.QcReportRate) + " or more reports the account;\n"
                    + "· when " + ZhongbaoUi.Pct(XgSim.QcMonotoneShare) + " of the answers are the same it looks like a script, and " + ZhongbaoUi.Pct(XgSim.QcWarnRate) + " is enough.\n"
                    + "While frozen, auto labels cannot be submitted. An appeal costs ¥" + N(XgSim.QcAppealMin, "0") + " or " + ZhongbaoUi.Pct(XgSim.QcAppealShare) + " of your money (whichever is more); credit stays the same."), ZhongbaoSkin.Ink));
            items.Add(Row.Head(T("数据噪声（不扣信用，伤模型）", "Data noise (no credit cost, hurts the model)")));
            bool any = false;
            foreach (var d in Sim.OpenDesks())
            {
                double share = Sim.NoiseShare(d.id);
                if (share <= 0) continue;
                any = true;
                double penalty = Sim.InbreedingPenaltyFor(d.id);
                items.Add(Row.Row(T(d.name, d.nameEn), T("噪声 ", "noise ") + ZhongbaoUi.Pct(share) + (penalty > 0 ? "  " + T("近亲繁殖 −", "inbreeding −") + ZhongbaoUi.Pct(penalty) : ""), penalty > 0 ? ZhongbaoSkin.Red : (Color?)null));
            }
            if (!any) items.Add(Row.Note(T("没有记下的错标。", "No wrong labels recorded.")));
            items.Add(Row.Note(T("没被抽到的错题会混进训练数据；超过样本的 " + ZhongbaoUi.Pct(XgSim.InbreedingFreeShare) + " 模型就会学回自己的错。标注台的「人工复核旧数据」能清掉它们。",
                "Wrong labels nobody checked go into the training data; above " + ZhongbaoUi.Pct(XgSim.InbreedingFreeShare) + " of the samples the model learns its own mistakes back. Re-check old labels on the labelling desk to clear them.")));
            moreList.sizeDelta = new Vector2(0, more.Set(items));
        }
    }
}
