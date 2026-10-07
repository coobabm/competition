using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AppNames = LingGuangV05.Core.AppNames;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The frame of 灵光.exe (lingguang-redesign/index.html): the 52-pixel top bar and the grouped left nav.
    /// Top bar: the AI (a breathing orb, its name, 「能力 n/6 · 当前能力」), the two bars of the main line toward the next
    /// ability (trained parameters P and samples D against the thresholds after the items owned) and ¥, income per
    /// second, house power and the game clock. Nav: 它 / 养成 / 调度 / 收藏, the locked 终章 row and the 摆渡众包 link.
    /// </summary>
    public sealed partial class XingGuangView
    {
        // ───────────── top bar ─────────────

        RectTransform barsRect, moneyRect;
        XgOrbGraphic orb;
        TMP_Text aiName, aiSub, paramsLabel, paramsValue, dataLabel, dataValue, statMoney, statIncome, statPower, statClock, statYear;
        RectTransform paramsFill, dataFill;

        void BuildTopBar()
        {
            var bar = Rect("Hud", root, new Vector2(0, 1), Vector2.one, new Vector2(0, -TopBarHeight), Vector2.zero);
            Panel(bar, XgDark.Panel);
            Panel(Rect("Rule", bar, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 1)), XgDark.Hairline).raycastTarget = false;

            // The AI: orb, name and its ability count.
            var brand = Rect("Brand", bar, new Vector2(0, 0), new Vector2(0, 1), new Vector2(14, 0), new Vector2(234, 0));
            var orbRt = Rect("Avatar", brand, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, -17), new Vector2(34, 17));
            orbRt.gameObject.AddComponent<CanvasRenderer>();
            orb = orbRt.gameObject.AddComponent<XgOrbGraphic>(); orb.raycastTarget = false;
            aiName = ui.Text(Rect("Name", brand, new Vector2(0, .5f), new Vector2(1, 1), new Vector2(44, 1), new Vector2(0, 0)), "", 15, XgDark.Ink, TextAlignmentOptions.BottomLeft);
            aiName.textWrappingMode = TextWrappingModes.NoWrap; aiName.overflowMode = TextOverflowModes.Ellipsis;
            aiSub = ui.Text(Rect("Ability", brand, Vector2.zero, new Vector2(1, .5f), new Vector2(44, 2), new Vector2(0, -1)), "", 12, XgDark.Muted, TextAlignmentOptions.TopLeft);
            aiSub.textWrappingMode = TextWrappingModes.NoWrap; aiSub.overflowMode = TextOverflowModes.Ellipsis;
            UiTip.Add(brand, () => AbilityTip());

            // ¥ · income · power · clock, right-aligned columns.
            var stats = Rect("Stats", bar, new Vector2(1, 0), Vector2.one, new Vector2(-392, 0), new Vector2(-14, 0));
            float x = 0;
            TMP_Text Stat(string name, float w, Color ink, out TMP_Text caption, out RectTransform cell)
            {
                cell = Rect(name, stats, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-x - w, 0), new Vector2(-x, 0));
                x += w + 14;
                caption = ui.Text(Rect("Caption", cell, new Vector2(0, .5f), Vector2.one, new Vector2(0, 1), new Vector2(0, -6)), "", 12, XgDark.Muted, TextAlignmentOptions.BottomRight);
                caption.textWrappingMode = TextWrappingModes.NoWrap;
                var value = ui.Text(Rect("Value", cell, Vector2.zero, new Vector2(1, .5f), new Vector2(0, 5), new Vector2(0, -1)), "", 14, ink, TextAlignmentOptions.TopRight);
                value.textWrappingMode = TextWrappingModes.NoWrap;
                return value;
            }
            statClock = Stat("Clock", 84, XgDark.Ink, out statYear, out var clockCell);
            statPower = Stat("Chip2", 96, XgDark.Ink, out var powerCaption, out var powerCell);
            statIncome = Stat("Chip1", 74, XgDark.Good, out var incomeCaption, out var incomeCell);
            statMoney = Stat("Chip0", 98, XgDark.Money, out var moneyCaption, out moneyRect);
            moneyCaption.text = "¥";
            incomeCaption.text = T("收入/秒", "income/s");
            powerCaption.text = T("电力", "power");
            UiTip.Add(moneyRect, "经费。手动标注、订单、评级奖励会加钱；电费、显卡、道具和科技会花钱。", "Funds. Labelling, contracts and grade rewards add money; power, cards, items and the tech tree cost money.");
            UiTip.Add(incomeCell, () => Sim != null && Sim.AutoLabelHidden
                ? T("每秒自动进账 = 已签订单。\n去「摆渡众包」的企业订单页：模型准确率够了就能签。", "Income per second = signed contracts.\nGo to Business orders in Bodu Crowd: sign once the model is accurate enough.")
                : T("每秒自动进账 = 已签订单 + 自动答题。\n去「摆渡众包」的企业订单页：模型准确率够了就能签。", "Income per second = signed contracts + auto-answer.\nGo to Business orders in Bodu Crowd: sign once the model is accurate enough."));
            UiTip.Add(powerCell, () => T("家里机箱的电：显卡、训练和接线页上的任务一起算，超过 3500W 就跳闸。\n显存：每张卡放得下多大的模型。", "The house case's power: cards, training and the jobs on the wiring page together; past 3500 W the breaker trips.\nVRAM: how big a model each card holds.")
                + "\n" + T("单卡显存 ", "VRAM per card ") + N(VramUseMB() / 1024, "0.0") + "/" + N(Sim.Vram(Host) / 1024, "0") + "G");
            UiTip.Add(clockCell, "2016 年的游戏时间。", "The game's time in 2016.");

            // The main line: two bars toward the next ability, between the AI and the stats.
            barsRect = Rect("Chip3", bar, Vector2.zero, Vector2.one, new Vector2(248, 0), new Vector2(-406, 0));
            RectTransform BarRow(string name, float x0, float x1, Color fill, out TMP_Text label, out TMP_Text value)
            {
                var row = Rect(name, barsRect, new Vector2(x0, 0), new Vector2(x1, 1), new Vector2(x0 > 0 ? 6 : 0, 0), new Vector2(x1 < 1 ? -6 : 0, 0));
                label = ui.Text(Rect("Label", row, new Vector2(0, .5f), Vector2.one, new Vector2(0, 2), new Vector2(-124, -6)), "", 12, XgDark.Muted, TextAlignmentOptions.BottomLeft);
                label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Ellipsis;
                value = ui.Text(Rect("Value", row, new Vector2(1, .5f), Vector2.one, new Vector2(-124, 2), new Vector2(0, -6)), "", 12, XgDark.Ink, TextAlignmentOptions.BottomRight);
                value.textWrappingMode = TextWrappingModes.NoWrap;
                var track = Rect("Track", row, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(0, -12), new Vector2(0, -4));
                return Bar(track, "Fill", XgDark.Track, fill);
            }
            paramsFill = BarRow("Params", 0, .5f, XgDark.Params, out paramsLabel, out paramsValue);
            dataFill = BarRow("Data", .5f, 1, XgDark.Data, out dataLabel, out dataValue);
            UiTip.Add(barsRect, () => AbilityTip());
        }

        /// <summary>The biggest model shape of the two training lines in VRAM (MB).</summary>
        double VramUseMB() => Math.Max(XgSim.VramNeedMB(Sim.S.vision), XgSim.VramNeedMB(Sim.S.sequence));

        /// <summary>The AI's display name: the name given at setup, else 灵光.</summary>
        public string AiName => Sim != null && Sim.Profile != null && Sim.Profile.name.Length > 0 ? Sim.Profile.name : T(AppNames.AiZh, AppNames.AiEn);

        /// <summary>The bars' hover help: what they count, and what the next ability opens.</summary>
        string AbilityTip()
        {
            if (Sim == null) return "";
            int next = Sim.NextAbility;
            string head = T("能力 " + Sim.AbilitiesCount + "/6。", "Abilities " + Sim.AbilitiesCount + "/6.");
            string bars = T("\n参数量：评估到 C 级的模型里最大的那个。光买宽度不练，不算。\n样本：所有数据集的有效样本（标错的打折）。\n两样都到门槛，下一项能力就有了。门槛已按买到的道具打折。",
                "\nParameters: the biggest model assessed at grade C or better. Width bought but never trained does not count.\nSamples: the effective samples of every dataset (wrong labels count against).\nWhen both reach the line, the next ability comes. The lines already include the items you own.");
            if (next == 0) return head + bars;
            return head + bars + "\n\n" + T("下一项「" + XgSim.AbilityName(next, false) + "」：", "Next, '" + XgSim.AbilityName(next, true) + "': ") + T(XgSim.AbilityUnlocks[next], XgSim.AbilityUnlocksEn[next]);
        }

        void RefreshTopBar()
        {
            aiName.text = AiName;
            int have = Sim.AbilitiesCount, next = Sim.NextAbility;
            aiSub.text = T(AppNames.AppZh + " · 能力 ", AppNames.AppEn + " · ability ") + have + "/6 · " + XgSim.AbilityName(have, Sim.English);
            double p = Sim.TrainedParamsK, d = Sim.TrainedSamples;
            if (next == 0)
            {
                paramsLabel.text = T("参数量 · 六项都有了", "Parameters · all six");
                dataLabel.text = T("样本 · 六项都有了", "Samples · all six");
                paramsValue.text = XgSim.ParamsText(p); dataValue.text = XgSim.SamplesText(d);
                SetBar(paramsFill, 1); SetBar(dataFill, 1);
            }
            else
            {
                string name = XgSim.AbilityName(next, Sim.English);
                paramsLabel.text = T("参数量 → ", "Parameters → ") + name;
                dataLabel.text = T("样本 → ", "Samples → ") + name;
                paramsValue.text = XgSim.ParamsText(p) + " / " + XgSim.ParamsText(Sim.ParamsThreshold(next));
                dataValue.text = XgSim.SamplesText(d) + " / " + XgSim.SamplesText(Sim.SamplesThreshold(next));
                SetBar(paramsFill, (float)Sim.ParamsProgress(next));
                SetBar(dataFill, (float)Sim.SamplesProgress(next));
            }
            if (shownMoney < 0) statMoney.text = Money(Host.Money);
            statIncome.text = "+" + Money(IncomeRate);
            var w = Sim.Wiring;
            double watts = w != null ? w.psuW : 0, limit = w != null && w.psuLimit > 0 ? w.psuLimit : 3500;
            statPower.text = N(watts, "0") + "/" + N(limit, "0") + "W";
            statPower.color = w != null && w.tripped || watts > limit ? XgDark.Bad : watts > limit * .85 ? XgDark.Hot : XgDark.Ink;
            var rt = controller.runtime;
            if (rt != null && rt.Sim != null)
            {
                var now = GameCalendar.Now(rt.Sim.S);
                statYear.text = now.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
                statClock.text = now.ToString("MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        float orbTime;

        void TickTopBar(float dt)
        {
            UpdateMoney(dt);
            orbTime += dt;
            // Breathes every three seconds (the mockup's 3 s ease-in-out); still when effects are reduced.
            if (orb != null) orb.Glow = Sim.S.reduceFx ? .5f : .5f + .5f * Mathf.Sin(orbTime * Mathf.PI * 2 / 3f);
        }

        /// <summary>The wallet counts up towards its real value instead of jumping.</summary>
        void UpdateMoney(float dt)
        {
            double money = Host.Money;
            if (shownMoney < 0 || Math.Abs(money - shownMoney) > Math.Max(1e5, money * 2)) shownMoney = money;
            double before = shownMoney;
            shownMoney += (money - shownMoney) * (1 - Math.Exp(-10 * dt));
            if (Math.Abs(money - shownMoney) < .005) shownMoney = money;
            statMoney.text = Money(shownMoney);
            statMoney.color = money > before + .004 ? new Color32(255, 230, 160, 255) : (Color)XgDark.Money;
        }

        // ───────────── nav ─────────────

        /// <summary>The nav's pages in order (XgGuideHighlight.TabIds follows it). 科技 (tree) is a section of 道具.</summary>
        public static readonly string[] NavIds = { "home", "chat", "train", "data", "items", "wiring", "repo", "abilities", "cards", "final" };

        static readonly (string zh, string en, string[] ids)[] NavGroups =
        {
            ("它", "IT", new[] { "home", "chat" }),
            ("养成", "RAISE", new[] { "train", "data", "items" }),
            ("调度", "RUN", new[] { "wiring", "repo" }),
            ("收藏", "KEEP", new[] { "abilities", "cards", "final" }),
        };

        sealed class NavRow
        {
            public string id;
            public RectTransform rt;
            public Image back, accent;
            public TMP_Text label, badge;
            public Button button;
            public bool hover;
        }

        /// <summary>Lights a nav row's text while the pointer is over it.</summary>
        sealed class NavHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Action<bool> changed;
            public void OnPointerEnter(PointerEventData e) => changed?.Invoke(true);
            public void OnPointerExit(PointerEventData e) => changed?.Invoke(false);
            void OnDisable() => changed?.Invoke(false);
        }

        const float NavRowHeight = 30, NavGroupHeight = 26;
        readonly Dictionary<string, NavRow> navRows = new Dictionary<string, NavRow>();
        readonly List<TMP_Text> navGroupLabels = new List<TMP_Text>();
        RectTransform nav, crowdLink;
        TMP_Text crowdTitle, crowdSub;
        XgBtn soundToggle, fxToggle, residentToggle;

        void BuildNav()
        {
            nav = Rect("Nav", root, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(NavWidth, -TopBarHeight));
            Panel(nav, XgDark.Panel);
            Panel(Rect("Rule", nav, new Vector2(1, 0), Vector2.one, new Vector2(-1, 0), Vector2.zero), XgDark.Hairline).raycastTarget = false;
            foreach (var g in NavGroups)
            {
                var label = ui.Text(Rect("Group", nav, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero), "", 12, XgDark.Dim, TextAlignmentOptions.BottomLeft);
                label.characterSpacing = 12; label.textWrappingMode = TextWrappingModes.NoWrap;
                navGroupLabels.Add(label);
                foreach (var id in g.ids) navRows[id] = MakeNavRow(id);
            }

            // 摆渡众包: labelling, orders and the combo live there.
            crowdLink = Rect("CrowdLink", nav, Vector2.zero, new Vector2(1, 0), new Vector2(10, 90), new Vector2(-10, 138));
            var face = Panel(crowdLink, XgDark.Panel);
            var rim = crowdLink.gameObject.AddComponent<Outline>(); rim.effectColor = XgDark.Line; rim.effectDistance = new Vector2(1, -1);
            var hover = crowdLink.gameObject.AddComponent<XgHoverRim>(); hover.rim = rim; hover.rest = XgDark.Line; hover.hot = XgDark.Link; hover.Apply();
            var link = crowdLink.gameObject.AddComponent<Button>(); link.targetGraphic = face; link.transition = Selectable.Transition.None;
            link.onClick.AddListener(() => { Juice.Play(XgJuice.Sfx.Id.Click); controller.OpenCrowd(null); });
            if (controller.Window != null) crowdLink.gameObject.AddComponent<ChapterOneWindowFocus>().Window = controller.Window;
            crowdTitle = ui.Text(Rect("Title", crowdLink, new Vector2(0, .5f), Vector2.one, new Vector2(4, 0), new Vector2(-4, -3)), "", 14, XgDark.Link, TextAlignmentOptions.Bottom);
            crowdTitle.textWrappingMode = TextWrappingModes.NoWrap;
            crowdSub = ui.Text(Rect("Sub", crowdLink, Vector2.zero, new Vector2(1, .5f), new Vector2(4, 3), new Vector2(-4, 0)), "", 12, XgDark.Muted, TextAlignmentOptions.Top);
            crowdSub.textWrappingMode = TextWrappingModes.NoWrap; crowdSub.overflowMode = TextOverflowModes.Ellipsis;
            UiTip.Add(crowdLink, "标注台和企业订单在「摆渡众包」：亲手标注赚钱、攒样本、叠连击，签订单让模型挣钱。", "The labelling desk and business orders are in Bodu Crowd: label by hand for money, samples and combo; sign contracts so the model earns.");

            soundToggle = ui.Button(nav, "", () => { Sim.S.mute = !Sim.S.mute; Juice.Muted = Sim.S.mute; Refresh(true); }, 12);
            PlaceBottom(soundToggle, 34, 24, 10, 10);
            fxToggle = ui.Button(nav, "", () => { Sim.S.reduceFx = !Sim.S.reduceFx; Juice.Reduced = Sim.S.reduceFx; Refresh(true); }, 12);
            PlaceBottom(fxToggle, 8, 24, 10, 10);
            UiTip.Add(soundToggle.rt, "打开或关闭音效。", "Turn sound effects on or off.");
            UiTip.Add(fxToggle.rt, "减少闪光、震动和粒子特效。", "Fewer flashes, shakes and particles.");
            // The resident spider (XgResidentSpider) roams the desktop; this parks it in a corner, saved with the lab.
            residentToggle = ui.Button(nav, "", () => { Sim.S.residentQuiet = !Sim.S.residentQuiet; Juice.Play(XgJuice.Sfx.Id.Click); Refresh(true); }, 12);
            PlaceBottom(residentToggle, 60, 24, 10, 10);
            UiTip.Add(residentToggle.rt, "它会在桌面上到处看，你标注时还会过来旁观。点这里让它在角落里待着。", "It wanders the desktop reading everything, and comes to watch when you label. Click to have it sit in a corner.");
        }

        NavRow MakeNavRow(string id)
        {
            var row = new NavRow { id = id };
            row.rt = Rect("Tab " + id, nav, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            row.back = Panel(row.rt, Color.clear);
            row.accent = Panel(Rect("Accent", row.rt, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(3, 0)), XgDark.Good);
            row.accent.raycastTarget = false;
            row.label = ui.Text(Rect("Label", row.rt, Vector2.zero, Vector2.one, new Vector2(14, 0), new Vector2(-8, 0)), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            row.label.textWrappingMode = TextWrappingModes.NoWrap; row.label.overflowMode = TextOverflowModes.Ellipsis;
            row.badge = ui.Text(Rect("Badge", row.rt, new Vector2(.5f, 0), Vector2.one, Vector2.zero, new Vector2(-10, 0)), "", 12, XgDark.Money, TextAlignmentOptions.MidlineRight);
            row.badge.textWrappingMode = TextWrappingModes.NoWrap;
            row.button = row.rt.gameObject.AddComponent<Button>();
            row.button.targetGraphic = row.back; row.button.transition = Selectable.Transition.None;
            row.button.onClick.AddListener(() => { if (!FeatureOpen(id)) return; ShowTab(id); Juice.Play(XgJuice.Sfx.Id.Click); });
            if (controller.Window != null) row.rt.gameObject.AddComponent<ChapterOneWindowFocus>().Window = controller.Window;
            row.rt.gameObject.AddComponent<NavHover>().changed = on => { row.hover = on; PaintNavRow(row); };
            UiTip.Add(row.rt, () => TabHelp(id));
            return row;
        }

        /// <summary>The nav row of a page (the guide rings it); 科技 answers with 道具.</summary>
        public RectTransform NavButton(string id)
        {
            if (id == "tree") id = "items";
            return id != null && navRows.TryGetValue(id, out var row) ? row.rt : null;
        }

        static string NavName(string id)
        {
            switch (id)
            {
                case "home": return T("概览", "Overview");
                case "chat": return Lang.T("对话");
                case "train": return Lang.T("训练");
                case "data": return T("数据", "Data");
                case "items": return T("道具", "Items");
                case "wiring": return T("接线", "Wiring");
                case "repo": return T("模型仓库", "Models");
                case "abilities": return T("能力", "Abilities");
                case "cards": return T("成就卡册", "Card album");
                default: return Lang.T("终章");
            }
        }

        static string TabHelp(string id)
        {
            switch (id)
            {
                case "home": return T("概览：它现在会什么、下一项能力还差多少，和最近发生的事。", "Overview: what it can do now, how far the next ability is, and what just happened.");
                case "train": return Lang.T("训练：选数据集，再选这个区的连接拓扑（结构），按「训练一轮」让灵光学。\n每轮练完自动考一次；成绩好才能签订单。");
                case "data": return T("数据：每个数据集有多少样本、干不干净。买数据包，或者去摆渡众包做题。", "Data: how many samples each dataset has and how clean they are. Buy packs, or label on Bodu Crowd.");
                case "items": return T("道具：结构和技巧，按年代排。结构能降低某项能力的门槛。\n加宽、加深、自动化和研究在「科技树」里。", "Items: structures and techniques by year. A structure lowers an ability's line.\nWidth, depth, automation and research are in the tech tree.");
                case "repo": return Lang.T("模型仓库：每次刷新纪录都存一个检查点。");
                case "wiring": return T("接线：电力 → 显卡 → 模型 → 任务。\n点输出口再点输入口接线，点线拔线。模型要放得进显卡的显存，显卡要有电；订单、代标、训练、预训练和替你回消息都在这里排。\n有了「看图说词」，就能按秒租阿杰网吧的两台 960。", "Wiring: power → cards → models → jobs.\nClick an output port, then an input port, to wire; click a wire to unplug. A model must fit in its card's VRAM and a card needs power; orders, auto-labelling, training, pre-training and replies all run through here.\nWith 'Picture to word' you can rent Ajie's two café 960s by the second.");
                case "abilities": return T("能力：六项能力、它们的参数和样本门槛，以及能降低门槛的道具。", "Abilities: the six abilities, their parameter and sample lines, and the items that lower them.");
                case "cards": return Lang.T("成就：收集来的闪卡。稀有度越高，卡面越闪：银箔、金箔、镭射、星河，还有转动才看得见的光栅卡。\n有些卡藏在工作以外的地方。");
                case "chat": return Lang.T("对话：和它说话。阶段越高，它会说的越多。");
                default: return T("终章：第六项能力「全部放开」以后打开。", "Finale: opens with the sixth ability, 'Everything'.");
            }
        }

        string NavBadge(string id)
        {
            switch (id)
            {
                case "chat": { int n = tab == "chat" ? 0 : UnreadChat; return n > 0 ? n.ToString() : ""; }
                case "items": { int n = Items != null ? Items.BuyableCount() : 0; return n > 0 ? T("可买 ", "") + n : ""; }
                case "repo": { int fresh = Sim.S.nextModelId - 1 - (Repo != null ? Repo.SeenUpTo : 0); return fresh > 0 && tab != "repo" ? "+" + fresh : ""; }
                case "wiring": return (Sim.AjiePending || Sim.Wiring.tripped) && tab != "wiring" ? "!" : "";
                case "cards": { int n = Sim.NewCards(); return n > 0 ? "+" + n : ""; }
                case "final": return Sim.EndingOpen && tab != "final" ? "!" : "";
                default: return "";
            }
        }

        void RefreshNav()
        {
            float y = 8;
            for (int g = 0; g < NavGroups.Length; g++)
            {
                var group = NavGroups[g];
                bool any = false;
                foreach (var id in group.ids) if (id == "final" || FeatureOpen(id)) any = true;
                var header = navGroupLabels[g];
                header.gameObject.SetActive(any);
                if (!any) { foreach (var id in group.ids) navRows[id].rt.gameObject.SetActive(false); continue; }
                header.text = T(group.zh, group.en);
                header.rectTransform.offsetMin = new Vector2(14, -y - NavGroupHeight); header.rectTransform.offsetMax = new Vector2(-8, -y - 4);
                y += NavGroupHeight;
                foreach (var id in group.ids)
                {
                    var row = navRows[id];
                    bool open = FeatureOpen(id);
                    // 终章 stays in the list, locked until 全部放开.
                    bool show = open || id == "final";
                    row.rt.gameObject.SetActive(show);
                    if (!show) continue;
                    row.rt.offsetMin = new Vector2(0, -y - NavRowHeight); row.rt.offsetMax = new Vector2(0, -y);
                    y += NavRowHeight;
                    row.label.text = id == "final" && !open ? T("终章 · 全部放开后", "Finale · after Everything") : NavName(id);
                    row.badge.text = open ? NavBadge(id) : "";
                    row.button.interactable = open;
                    PaintNavRow(row);
                }
            }
            bool raiseReady = Sim.RaiseLevel < XgCatalog.RaiseMax && Host.Money >= Sim.NextRaiseCost;
            // The 加薪 badge follows the shop to 摆渡众包.
            crowdTitle.text = T("摆渡众包 ↗", "Bodu Crowd ↗") + (raiseReady ? "  <size=12><color=#FAC775>" + T("加薪", "raise") + "</color></size>" : "");
            crowdSub.text = T("做题挣钱和样本", "Label for ¥ and samples");
            soundToggle.Set(Sim.S.mute ? Lang.T("音效：关") : Lang.T("音效：开"), true, XgDark.Panel, XgDark.Muted);
            fxToggle.Set(Sim.S.reduceFx ? Lang.T("减少特效：开") : Lang.T("减少特效：关"), true, XgDark.Panel, XgDark.Muted);
            residentToggle.Set(Sim.S.residentQuiet ? T("让它出来走走", "Let it roam") : T("让它安静一会儿", "Let it rest a while"), true, XgDark.Panel, XgDark.Muted);
        }

        void PaintNavRow(NavRow row)
        {
            bool on = row.id == tab || row.id == "items" && tab == "tree";
            bool open = row.button.interactable;
            row.back.color = on ? XgDark.OnFill : row.hover && open ? new Color32(14, 28, 36, 255) : (Color)new Color32(0, 0, 0, 0);
            row.accent.enabled = on;
            row.label.color = !open ? XgDark.Dim : on || row.hover ? XgDark.Ink : XgDark.Muted;
        }
    }
}
