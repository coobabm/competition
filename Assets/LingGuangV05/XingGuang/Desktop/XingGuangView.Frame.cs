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
    /// second, house power and the game clock. Nav: 它 (概览) / 养成 (训练, 数据, 道具, 科技) / 调度 / 收藏, the locked 终章
    /// row and the 摆渡众包 link. Talking to it is in YY (XgYyTalk), not in this window.
    /// </summary>
    public sealed partial class XingGuangView
    {
        // ───────────── top bar ─────────────

        RectTransform barsRect, moneyRect;
        XgOrbGraphic orb;
        TMP_Text nextHead, goLabel, aiName, aiSub, paramsLabel, paramsValue, dataLabel, dataValue, statMoney, statIncome, statPower, statClock, statYear;
        RectTransform paramsFill, dataFill;
        Button goButton;
        XgGuideStep barStep;
        float barStepAt = -1;

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
                if (XgUiV4Fonts.Numbers != null) { value.font = XgUiV4Fonts.Numbers; value.fontFeatures.Clear(); }
                value.textWrappingMode = TextWrappingModes.NoWrap;
                return value;
            }
            statClock = Stat("Clock", 84, XgDark.Ink, out statYear, out var clockCell);
            statPower = Stat("Chip2", 96, XgDark.Ink, out var powerCaption, out var powerCell);
            statIncome = Stat("Chip1", 74, XgDark.Good, out var incomeCaption, out var incomeCell);
            statMoney = Stat("Chip0", 98, XgDark.Money, out var moneyCaption, out moneyRect);
            BuildLedgerWarning(stats);
            moneyTextReady=false;
            moneyCaption.text = "¥";
            incomeCaption.text = T("收入/秒", "income/s");
            powerCaption.text = T("电力", "power");
            UiTip.Add(moneyRect, "经费。手动标注、订单、评级奖励会加钱；电费、显卡、道具和科技会花钱。", "Funds. Labelling, contracts and grade rewards add money; power, cards, items and the tech tree cost money.");
            UiTip.Add(incomeCell, () => (Sim != null && Sim.AutoLabelHidden
                ? T("每秒自动进账 = 已签订单。\n去「摆渡众包」的企业订单页：模型准确率够了就能签。", "Income per second = signed contracts.\nGo to Business orders in Bodu Crowd: sign once the model is accurate enough.")
                : T("每秒自动进账 = 已签订单 + 自动答题。\n去「摆渡众包」的企业订单页：模型准确率够了就能签。", "Income per second = signed contracts + auto-answer.\nGo to Business orders in Bodu Crowd: sign once the model is accurate enough."))
                // 节奏重做 §2.4: each ability opens the next crowd-order tier, paid by the second.
                + (Sim != null ? "\n" + XgDataUi.CrowdTierLine(Sim) : ""));
            UiTip.Add(powerCell, () => T("家里机箱的电：显卡、训练和接线页上的任务一起算，超过 3500W 就跳闸。\n显存：每张卡放得下多大的模型。", "The house case's power: cards, training and the jobs on the wiring page together; past 3500 W the breaker trips.\nVRAM: how big a model each card holds.")
                + "\n" + T("单卡显存 ", "VRAM per card ") + N(VramUseMB() / 1024, "0.0") + "/" + N(Sim.Vram(Host) / 1024, "0") + "G");
            UiTip.Add(clockCell, "2016 年的游戏时间。", "The game's time in 2016.");

            // The main line in one place: the next ability's name and the 去做 link on top, two thin bars under it
            // (trained parameters P and samples D against the lines after the items owned). Every text is 13 px or more.
            barsRect = Rect("Chip3", bar, Vector2.zero, Vector2.one, new Vector2(248, 0), new Vector2(-406 - StripWidth - 14, 0));
            Panel(barsRect, Color.clear);
            nextHead = ui.Text(Rect("Next", barsRect, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -23), new Vector2(-84, -3)), "", 13, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            nextHead.textWrappingMode = TextWrappingModes.NoWrap; nextHead.overflowMode = TextOverflowModes.Ellipsis;
            var goRt = Rect("Go", barsRect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -23), new Vector2(0, -3));
            var goImg = Panel(goRt, Color.clear);
            goButton = goRt.gameObject.AddComponent<Button>(); goButton.targetGraphic = goImg;
            goButton.onClick.AddListener(GoDo);
            goLabel = ui.Text(Rect("Label", goRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 13, XgDark.Link, TextAlignmentOptions.MidlineRight);
            goLabel.textWrappingMode = TextWrappingModes.NoWrap;
            UiTip.Add(goRt, () => GoTip());
            RectTransform BarRow(string name, float x0, float x1, Color fill, out TMP_Text label, out TMP_Text value)
            {
                var row = Rect(name, barsRect, new Vector2(x0, 0), new Vector2(x1, 1), new Vector2(x0 > 0 ? 8 : 0, 0), new Vector2(x1 < 1 ? -8 : 0, 0));
                label = ui.Text(Rect("Label", row, Vector2.zero, new Vector2(0, 0), new Vector2(0, 11), new Vector2(64, 29)), "", 13, XgDark.Muted, TextAlignmentOptions.BottomLeft);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                value = ui.Text(Rect("Value", row, Vector2.zero, new Vector2(1, 0), new Vector2(64, 11), new Vector2(0, 29)), "", 13, XgDark.Ink, TextAlignmentOptions.BottomRight);
                if (XgUiV4Fonts.Numbers != null) { value.font = XgUiV4Fonts.Numbers; value.fontFeatures.Clear(); }
                value.textWrappingMode = TextWrappingModes.NoWrap; value.overflowMode = TextOverflowModes.Ellipsis;
                var track = Rect("Track", row, Vector2.zero, new Vector2(1, 0), new Vector2(0, 6), new Vector2(0, 10));
                return Bar(track, "Fill", XgDark.Track, fill);
            }
            paramsFill = BarRow("Params", 0, .5f, XgDark.Params, out paramsLabel, out paramsValue);
            dataFill = BarRow("Data", .5f, 1, XgDark.Data, out dataLabel, out dataValue);
            UiTip.Add(barsRect, () => AbilityTip());
            BuildRelicStrip(bar);
        }

        /// <summary>The biggest model shape of the two training lines in VRAM (MB).</summary>
        double VramUseMB() => Math.Max(XgSim.VramNeedMB(Sim.S.vision), XgSim.VramNeedMB(Sim.S.sequence));

        /// <summary>The AI's display name: the name given at setup, else 灵光.</summary>
        public string AiName => Sim != null ? Sim.AiDisplayName : T(AppNames.AiZh, AppNames.AiEn);

        /// <summary>The bars' hover help: what they count, and what the next ability opens.</summary>
        string AbilityTip()
        {
            if (Sim == null) return "";
            int next = Sim.NextAbility;
            string head = T("能力 " + Sim.AbilitiesCount + "/6。", "Abilities " + Sim.AbilitiesCount + "/6.");
            // Live numbers: each bar against its line, the models behind P with their formula, caps and memory.
            string bars = "\n" + Sim.ParamsReport(Host) + T("\n光买宽度不练，不算。", "\nWidth bought but never trained does not count.") + "\n\n" + Sim.SamplesReport()
                + "\n\n" + Sim.FogNote;
            if (next == 0) return head + bars;
            return head + bars + "\n\n" + T("下一项「" + XgSim.AbilityName(next, false) + "」：", "Next, '" + XgSim.AbilityName(next, true) + "': ") + T(XgSim.AbilityUnlocks[next], XgSim.AbilityUnlocksEn[next]);
        }

        void RefreshTopBar()
        {
            aiName.text = AiName;
            int have = Sim.AbilitiesCount, next = Sim.NextAbility;
            aiSub.text = T("能力 ", "Ability ") + have + "/6 · " + XgSim.AbilityName(have, Sim.English);
            double p = Sim.TrainedParamsK, d = Sim.TrainedSamples;
            paramsLabel.text = T("参数", "Params");
            dataLabel.text = T("样本", "Samples");
            if (next == 0)
            {
                nextHead.text = T("六项都有了", "All six learned");
                paramsValue.text = XgUiV4Fonts.FixedDigits(XgSim.ParamsText(p)); dataValue.text = XgUiV4Fonts.FixedDigits(XgSim.SamplesText(d));
                SetBar(paramsFill, 1); SetBar(dataFill, 1);
                barStep = null; goButton.gameObject.SetActive(false);
            }
            else
            {
                nextHead.text = T("下一项能力 · ", "Next ability · ") + XgSim.AbilityName(next, Sim.English);
                // The line is hidden (XgSim.Learned.cs): the bars say how much is trained, and 「?」 until a bar is past it.
                paramsValue.text = XgUiV4Fonts.FixedDigits(XgSim.ParamsText(p) + " · " + Sim.ParamsLineText(next));
                dataValue.text = XgUiV4Fonts.FixedDigits(XgSim.SamplesText(d) + " · " + Sim.SamplesLineText(next));
                SetBar(paramsFill, (float)Sim.ParamsFill);
                SetBar(dataFill, (float)Sim.SamplesFill);
                // The step behind the link is planned a couple of times a second, not every refresh.
                if (barStepAt < 0 || Time.unscaledTime - barStepAt > .5f) { barStepAt = Time.unscaledTime; barStep = FindBarStep(out _); }
                goButton.gameObject.SetActive(barStep != null);
                goLabel.text = T("去做 ›", "Do it ›");
            }
            if (shownMoney < 0) ApplyMoneyText(Host.Money);
            statIncome.text = XgUiV4Fonts.FixedDigits("+" + Money(IncomeRate));
            var w = Sim.Wiring;
            double watts = w != null ? w.psuW : 0, limit = w != null && w.psuLimit > 0 ? w.psuLimit : 3500;
            statPower.text = N(watts, "0") + "/" + N(limit, "0") + "W";
            statPower.color = w != null && w.tripped || watts > limit ? XgDark.Bad : watts > limit * .85 ? XgDark.Hot : XgDark.Ink;
            RefreshLedgerVisibility();
            var rt = controller.runtime;
            if (rt != null && rt.Sim != null)
            {
                var now = GameCalendar.Now(rt.Sim.S);
                statYear.text = now.Year.ToString(System.Globalization.CultureInfo.InvariantCulture);
                statClock.text = XgUiV4Fonts.FixedDigits(now.ToString("MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        /// <summary>Whole percent, never 100 before the bar is full.</summary>
        static string Percent(double k) => Math.Floor(k * 100 + 1e-9).ToString("0", System.Globalization.CultureInfo.InvariantCulture);

        XgGuideStep FindBarStep(out bool parameters) => XgGuide.BarStep(Sim, Host.Money, Host.VramMB, out parameters);

        /// <summary>「去做 ›」: goes where the guide would send you for the shorter bar, and rings the control (the to-do note's own way there).</summary>
        void GoDo()
        {
            var step = FindBarStep(out _);
            if (step == null) return;
            barStep = step;
            var note = LingGuangV05.Desktop.Story.GuideNote.Instance;
            if (note != null && note.Follow(step)) return;
            // Without the note (a bare lab window): the lab's own pages only.
            if (step.app != XgGuide.Lab) return;
            Open(step.tab == "train" && (step.arg == "vision" || step.arg == "sequence") ? step.arg : step.tab);
            XgGuideHighlight.Show(this, step.tab, step.target);
            if (step.id == "data.install" && Sim.DataFileReady(step.arg))
                UnityEngine.Object.FindAnyObjectByType<XgFilesWindow>(UnityEngine.FindObjectsInactive.Include)?.ShowTrainingData(Sim);
        }

        string GoTip()
        {
            var step = barStep ?? FindBarStep(out _);
            if (step == null) return "";
            return T("去做：", "Do it: ") + step.Text(Sim.English) + "\n" + step.Why(Sim.English);
        }

        float orbTime;
        bool moneyTextReady,moneyTextEnglish;
        double moneyTextValue;
        void ApplyMoneyText(double amount)
        {
            bool english=En;
            if(moneyTextReady&&moneyTextValue.Equals(amount)&&moneyTextEnglish==english)return;
            moneyTextValue=amount;moneyTextEnglish=english;moneyTextReady=true;
            statMoney.text=XgUiV4Fonts.FixedDigits(Money(amount));
        }

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
            ApplyMoneyText(shownMoney);
            statMoney.color = money > before + .004 ? new Color32(255, 230, 160, 255) : (Color)XgDark.Money;
        }

        // ───────────── nav ─────────────

        /// <summary>The nav's pages in order (XgGuideHighlight.TabIds follows it). 科技 (tree) is a section of 道具.</summary>
        public static readonly string[] NavIds = { "teach", "train", "tree", "home" };

        static readonly (string zh, string en, string[] ids)[] NavGroups =
        {
            ("", "", new[] { "teach", "train", "tree", "home" }),
        };

        sealed class NavRow
        {
            public string id;
            public RectTransform rt;
            public Image back, accent, icon;
            public TMP_Text label, badge, lockedHint;
            public RectTransform lockIcon;
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

        const float NavRowHeight = 27, NavGroupHeight = 26;
        readonly Dictionary<string, NavRow> navRows = new Dictionary<string, NavRow>();
        readonly List<TMP_Text> navGroupLabels = new List<TMP_Text>();
        RectTransform nav, navMenu, crowdLink;
        TMP_Text crowdTitle, crowdSub;
        XgBtn soundToggle, fxToggle, residentToggle, residentRecall;

        void BuildNav()
        {
            nav = Rect("Nav", root, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(NavWidth, -TopBarHeight));
            Panel(nav, XgDark.Panel);
            Panel(Rect("Rule", nav, new Vector2(1, 0), Vector2.one, new Vector2(-1, 0), Vector2.zero), XgDark.Hairline).raycastTarget = false;
            navMenu=Rect("NavigationArea",nav,Vector2.zero,Vector2.one,new Vector2(8,120),new Vector2(-8,-8));
            foreach (var g in NavGroups)
            {
                var label = ui.Text(Rect("Group", nav, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero), "", 12, XgDark.Dim, TextAlignmentOptions.BottomLeft);
                label.characterSpacing = 12; label.textWrappingMode = TextWrappingModes.NoWrap;
                navGroupLabels.Add(label);
                foreach (var id in g.ids) navRows[id] = MakeNavRow(id);
            }

            // 摆渡众包: labelling, orders and the combo live there.
            crowdLink = Rect("CrowdLink", nav, Vector2.zero, new Vector2(1, 0), new Vector2(8,84), new Vector2(-8,108));
            var face = Panel(crowdLink, XgDark.Panel);
            var rim = crowdLink.gameObject.AddComponent<Outline>(); rim.effectColor = XgDark.Line; rim.effectDistance = new Vector2(1, -1);
            var hover = crowdLink.gameObject.AddComponent<XgHoverRim>(); hover.rim = rim; hover.rest = XgDark.Line; hover.hot = XgDark.Link; hover.Apply();
            var link = crowdLink.gameObject.AddComponent<Button>(); link.targetGraphic = face; link.transition = Selectable.Transition.None;
            link.onClick.AddListener(() => { Juice.Play(XgJuice.Sfx.Id.Click); if (!XgYyTalk.Open()) ShowTab("home"); });
            if (controller.Window != null) crowdLink.gameObject.AddComponent<ChapterOneWindowFocus>().Window = controller.Window;
            crowdTitle = ui.Text(Rect("Title", crowdLink, new Vector2(0, .5f), Vector2.one, new Vector2(4, 0), new Vector2(-4, -3)), "", 14, XgDark.Link, TextAlignmentOptions.Bottom);
            crowdTitle.textWrappingMode = TextWrappingModes.NoWrap;
            crowdSub = ui.Text(Rect("Sub", crowdLink, Vector2.zero, new Vector2(1, .5f), new Vector2(4, 3), new Vector2(-4, 0)), "", 12, XgDark.Muted, TextAlignmentOptions.Top);
            crowdSub.textWrappingMode = TextWrappingModes.NoWrap; crowdSub.overflowMode = TextOverflowModes.Ellipsis;
            crowdSub.gameObject.SetActive(false);crowdTitle.rectTransform.anchorMin=Vector2.zero;crowdTitle.rectTransform.anchorMax=Vector2.one;crowdTitle.alignment=TextAlignmentOptions.Center;crowdTitle.fontSize=11;
            UiTip.Add(crowdLink, () => T("打开 YY，直接输入想对" + Sim.AiDisplayName + "说的话。教完或训练后，再问问它，看看回答有什么变化。", "Open YY and talk freely to " + Sim.AiDisplayName + ". After teaching or training, ask again and see how its answers change."));

            soundToggle = ui.Button(nav, "", () => { Sim.S.mute = !Sim.S.mute; Juice.Muted = Sim.S.mute; Refresh(true); }, 12);
            PlaceSidebarToggle(soundToggle,-23);
            fxToggle = ui.Button(nav, "", () => { Sim.S.reduceFx = !Sim.S.reduceFx; Juice.Reduced = Sim.S.reduceFx; Refresh(true); }, 12);
            PlaceSidebarToggle(fxToggle,23);
            UiTip.Add(soundToggle.rt, "打开或关闭音效。", "Turn sound effects on or off.");
            UiTip.Add(fxToggle.rt, "减少闪光、震动和粒子特效。", "Fewer flashes, shakes and particles.");
            // The resident spider (XgResidentSpider) roams the desktop; this parks it in a corner, saved with the lab.
            residentToggle = ui.Button(nav, "", () => { Sim.S.residentQuiet = !Sim.S.residentQuiet; Juice.Play(XgJuice.Sfx.Id.Click); Refresh(true); }, 12);
            PlaceBottom(residentToggle,52,24,8,58);
            residentRecall = ui.Button(nav, "", () => { XgResidentSpider.Instance?.Recall(); Refresh(true); }, 12);
            PlaceBottom(residentRecall,52,24,58,8);
            foreach(var control in new[]{residentToggle,residentRecall}){control.label.fontSize=11;control.label.rectTransform.offsetMin=new Vector2(2,0);control.label.rectTransform.offsetMax=new Vector2(-2,0);}
            nav.gameObject.AddComponent<NavResize>().resized=LayoutNavRows;
            UiTip.Add(residentToggle.rt, "它会在桌面上到处爬，你标注时还会过来旁观。点一下让它停下，慢慢隐身。", "It crawls all over the desktop and comes to watch when you label. Click to make it stop and fade away.");
        }

        NavRow MakeNavRow(string id)
        {
            var row = new NavRow { id = id };
            row.rt = Rect("Tab " + id, navMenu, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            row.back = Panel(row.rt, Color.clear);RoundSurface(row.back,12);
            row.accent = Panel(Rect("Accent", row.rt, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(3, 0)), XgDark.Good);
            row.accent.raycastTarget = false;
            row.icon=XgIcons.Place(row.rt,"nav",id,0,0,50);
            if(row.icon!=null){row.icon.rectTransform.anchorMin=row.icon.rectTransform.anchorMax=new Vector2(.5f,1);row.icon.rectTransform.anchoredPosition=new Vector2(0,-27);}
            row.lockIcon=Rect("LockIcon",row.rt,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-25,-52),new Vector2(25,-2));
            // Draw the lock with geometry; no emoji font dependency or missing glyph.
            Panel(Rect("Body",row.lockIcon,new Vector2(.24f,.18f),new Vector2(.76f,.58f),Vector2.zero,Vector2.zero),XgDark.Muted).raycastTarget=false;
            Panel(Rect("Left",row.lockIcon,new Vector2(.30f,.58f),new Vector2(.38f,.82f),Vector2.zero,Vector2.zero),XgDark.Muted).raycastTarget=false;
            Panel(Rect("Right",row.lockIcon,new Vector2(.62f,.58f),new Vector2(.70f,.82f),Vector2.zero,Vector2.zero),XgDark.Muted).raycastTarget=false;
            Panel(Rect("Top",row.lockIcon,new Vector2(.38f,.78f),new Vector2(.62f,.86f),Vector2.zero,Vector2.zero),XgDark.Muted).raycastTarget=false;
            row.label=ui.Text(Rect("Label",row.rt,new Vector2(0,1),Vector2.one,new Vector2(1,-73),new Vector2(-1,-53)),"",14,XgDark.Muted,TextAlignmentOptions.Center);
            row.label.textWrappingMode=TextWrappingModes.NoWrap;row.label.overflowMode=TextOverflowModes.Ellipsis;
            row.lockedHint=ui.Text(Rect("LockedHint",row.rt,new Vector2(0,1),Vector2.one,new Vector2(0,-99),new Vector2(0,-74)),"",10,XgDark.Muted,TextAlignmentOptions.Top);
            row.lockedHint.textWrappingMode=TextWrappingModes.Normal;row.lockedHint.raycastTarget=false;
            row.badge=ui.Text(Rect("Badge",row.rt,new Vector2(.5f,1),Vector2.one,new Vector2(12,-18),new Vector2(-1,-2)),"",10,XgDark.Money,TextAlignmentOptions.TopRight);
            row.badge.textWrappingMode=TextWrappingModes.NoWrap;
            row.button = row.rt.gameObject.AddComponent<Button>();
            row.button.targetGraphic = row.back; row.button.transition = Selectable.Transition.None;
            row.button.onClick.AddListener(() => { if (!FeatureOpen(id)) return; ShowTab(id); Juice.Play(XgJuice.Sfx.Id.Click); });
            if (controller.Window != null) row.rt.gameObject.AddComponent<ChapterOneWindowFocus>().Window = controller.Window;
            row.rt.gameObject.AddComponent<NavHover>().changed = on => { row.hover = on; PaintNavRow(row); };
            UiTip.Add(row.rt, () => TabHelp(id));
            return row;
        }

        /// <summary>The nav row of a page (the guide rings it).</summary>
        public RectTransform NavButton(string id)
        {
            if (id == "data" || id == "repo" || id == "wiring") id = "train";
            if (id == "board" || id == "cards" || id == "final") id = "home";
            if (id == "label") id = "teach";
            if (id == "items" || id == "abilities" || id == "growth") id = "tree";
            return id != null && navRows.TryGetValue(id, out var row) ? row.rt : null;
        }

        string NavName(string id)
        {
            switch (id)
            {
                case "teach": return T("教", "Teach");
                case "board": return T("大脑", "Brain");
                case "home": return AiName;
                case "train": return T("练", "Train");
                case "data": return T("数据", "Data");
                case "items": return T("道具", "Items");
                case "backpack": return T("背包", "Backpack");
                case "tree": return T("科技", "Technology");
                case "wiring": return T("接线", "Wiring");
                case "repo": return T("模型仓库", "Models");
                case "abilities": return T("能力", "Abilities");
                case "cards": return T("成就卡册", "Card album");
                default: return Lang.T("终章");
            }
        }

        string TabHelp(string id)
        {
            if (!FeatureOpen(id))
            {
                if (id == "tree") return T("做完一套题开放", "Opens after twelve questions");
                if (id == "train") return T("在科技里买「训练曲线」", "Buy Training curve in Tech");
                if (id == "home") return T("第一次和它说话后开放", "Opens after your first conversation");
            }
            switch (id)
            {
                case "teach": return T("教：逻辑、常识题直接判断题目对错；其他教学卡按按钮提示回答。", "Teach: judge logic and common-sense statements directly; follow the prompts on other cards.");
                case "home": return T("概览：它现在会什么、下一项能力还差多少，和最近发生的事。", "Overview: what it can do now, how far the next ability is, and what just happened.");
                case "train": return T("训练：选数据集，再选这个区的连接拓扑（结构），按「训练一轮」让" + Sim.AiDisplayName + "学。\n每轮练完自动考一次；成绩好才能签订单。", "Train: select data and this region's network, then train " + Sim.AiDisplayName + " for one round.\nEach round ends with an exam; better results unlock contracts.");
                case "data": return T("数据：每个数据集有多少样本、干不干净。买数据包，或者去摆渡众包做题。", "Data: how many samples each dataset has and how clean they are. Buy packs, or label on Bodu Crowd.");
                case "backpack": return T("背包：拿到的稀有道具都在这里，最多装备 4 件，只有装备的才生效，随时可换。也有已买的结构和技巧，和稀有道具图鉴。", "Backpack: every rare item you found. Equip up to four; only equipped ones work, and you can swap any time. Also the structures and techniques you bought, and the collection book.");
                case "items": return T("道具：结构和技巧，按年代排。结构能降低某项能力的门槛。\n加宽、加深、自动化和研究在「科技」里。", "Items: structures and techniques by year. A structure lowers an ability's line.\nWidth, depth, automation and research are under Tech.");
                case "tree": return T("成长：六项能力及其门槛、结构、技巧和数据包。点击或停留在图标上看说明，点击价格购买。", "Growth: six abilities and their thresholds, structures, techniques and data packs. Click or hover over an icon to read; click the price to buy.");
                case "repo": return Lang.T("模型仓库：每次刷新纪录都存一个检查点。");
                case "wiring": return T("接线：电力 → 显卡 → 模型 → 任务。\n点输出口再点输入口接线，点线拔线。模型要放得进显卡的显存，显卡要有电；订单、代标、训练和预训练都在这里排。\n有了「看图说词」，就能按秒租阿杰网吧的两台 960。", "Wiring: power → cards → models → jobs.\nClick an output port, then an input port, to wire; click a wire to unplug. A model must fit in its card's VRAM and a card needs power; orders, auto-labelling, training and pre-training all run through here.\nWith 'Picture to word' you can rent Ajie's two café 960s by the second.");
                case "abilities": return T("能力：六项能力、它们的参数和样本门槛，以及能降低门槛的道具。", "Abilities: the six abilities, their parameter and sample lines, and the items that lower them.");
                case "cards": return Lang.T("成就：收集来的闪卡。稀有度越高，卡面越闪：银箔、金箔、镭射、星河，还有转动才看得见的光栅卡。\n有些卡藏在工作以外的地方。");
                default: return T("终章：第六项能力「全部放开」以后打开。", "Finale: opens with the sixth ability, 'Everything'.");
            }
        }

        string NavBadge(string id)
        {
            switch (id)
            {
                // 背包 now lives in the AI page, so a fresh rare item marks that row.
                case "home": return Sim.RelicsFresh && !(tab == "home" && profileSection == "backpack") ? "<color=#E24B4A>●</color>" : "";
                case "items": { int n = Items != null ? Items.BuyableCount() : 0; return n > 0 ? T("可买 ", "") + n : ""; }
                case "tree": { if(Sim.S.lessonAtlasUnread)return "<color=#E24B4A>●</color>"; int n = Tree != null ? Tree.GrowthBuyableCount() : 0; return n > 0 ? T("可买 ", "") + n : ""; }
                case "repo": { int fresh = Sim.S.nextModelId - 1 - (Repo != null ? Repo.SeenUpTo : 0); return fresh > 0 && tab != "repo" ? "+" + fresh : ""; }
                case "wiring": return (Sim.AjiePending || Sim.Wiring.tripped) && tab != "wiring" ? "!" : "";
                case "cards": { int n = Sim.NewCards(); return n > 0 ? "+" + n : ""; }
                case "final": return Sim.EndingOpen && tab != "final" ? "!" : "";
                default: return "";
            }
        }

        bool NavVisible(string id)
        {
            if (id == "items" || id == "abilities") return false; // Legacy routes remain available through the single Growth row.
            if (id == "home" || id == "teach" || id == "tree" || id == "train") return true;
            if (Sim.S.gradualSystems && Sim.S.stage < 3 && System.Array.IndexOf(new[] { "teach", "train", "board", "tree", "chat" }, id) < 0) return false;
            return FeatureOpen(id) || id == "final";
        }

        void RefreshNav()
        {
            float y = 8;
            for (int g = 0; g < NavGroups.Length; g++)
            {
                var group = NavGroups[g];
                bool any = false;
                foreach (var id in group.ids) if (NavVisible(id)) any = true;
                var header = navGroupLabels[g];
                header.gameObject.SetActive(false);
                if (!any) { foreach (var id in group.ids) navRows[id].rt.gameObject.SetActive(false); continue; }
                header.text = T(group.zh, group.en);
                header.rectTransform.offsetMin = new Vector2(14, -y - NavGroupHeight); header.rectTransform.offsetMax = new Vector2(-8, -y - 4);
                foreach (var id in group.ids)
                {
                    var row = navRows[id];
                    bool open = FeatureOpen(id);
                    // 终章 stays in the list, locked until 全部放开.
                    bool show = NavVisible(id);
                    row.rt.gameObject.SetActive(show);
                    if (!show) continue;

                    row.label.text=NavName(id);
                    row.lockedHint.gameObject.SetActive(!open);row.lockedHint.text=NavLockedHint(id);
                    row.lockIcon.gameObject.SetActive(!open);if(row.icon!=null)row.icon.gameObject.SetActive(open);
                    row.badge.text = open ? NavBadge(id) : "";
                    row.button.interactable = open;
                    PaintNavRow(row);
                }
            }
            crowdLink.gameObject.SetActive(Sim.SystemUnlocked("crowd"));
            bool raiseReady = Sim.RaiseLevel < XgCatalog.RaiseMax && Host.Money >= Sim.NextRaiseCost;
            // The 加薪 badge follows the shop to 摆渡众包.
            crowdTitle.text = T("YY 聊天 ↗", "YY chat ↗");
            crowdSub.text = T("在 YY 自由输入", "Speak freely in YY");
            soundToggle.Set(Sim.S.mute ? "S×" : "S♪", true, XgDark.Panel, XgDark.Muted);
            fxToggle.Set(Sim.S.reduceFx ? "FX−" : "FX", true, XgDark.Panel, XgDark.Muted);
            // Only once the desktop spider has shown up (its intro played).
            residentToggle.rt.gameObject.SetActive(Sim.SpiderAround);
            residentRecall.rt.gameObject.SetActive(Sim.SpiderAround);
            residentRecall.Set(T("叫回", "Recall"), true, XgDark.Panel, XgDark.Muted);
            residentToggle.Set(Sim.S.residentQuiet ? T("走走", "Roam") : T("休息", "Rest"), true, XgDark.Panel, XgDark.Muted);
            LayoutNavRows();
        }

        // The footer keeps its own space; the open pages share the remaining height evenly.
        void LayoutNavRows()
        {
            if(navMenu==null||crowdLink==null)return;
            const float footer=120;
            navMenu.offsetMin=new Vector2(8,footer);navMenu.offsetMax=new Vector2(-8,-8);
            const int count=4;
            if(count==0)return;
            float height=Mathf.Max(0,navMenu.rect.height),slot=height/count;
            float rowHeight=Mathf.Max(0,slot-4);int index=0;
            foreach(var id in NavIds)
            {
                var row=navRows[id];if(!row.rt.gameObject.activeSelf)continue;
                float center=1-(index+.5f)/count;index++;
                row.rt.anchorMin=new Vector2(0,center);row.rt.anchorMax=new Vector2(1,center);
                row.rt.offsetMin=new Vector2(0,-rowHeight*.5f);row.rt.offsetMax=new Vector2(0,rowHeight*.5f);
                float inset=Mathf.Max(2,(rowHeight-108)*.5f);
                if(row.icon!=null)row.icon.rectTransform.anchoredPosition=new Vector2(0,-inset-25);
                row.lockIcon.offsetMin=new Vector2(-25,-inset-50);row.lockIcon.offsetMax=new Vector2(25,-inset);
                row.label.rectTransform.offsetMin=new Vector2(1,-inset-71);row.label.rectTransform.offsetMax=new Vector2(-1,-inset-51);
                row.lockedHint.rectTransform.offsetMin=new Vector2(0,-inset-108);row.lockedHint.rectTransform.offsetMax=new Vector2(0,-inset-72);
            }
        }
        string NavLockedHint(string id)
        {
            if(FeatureOpen(id))return "";
            if(id=="tree")return T("做满12题\n"+Math.Min(12,Sim.TeachingAnswers)+" / 12","Answer 12\n"+Math.Min(12,Sim.TeachingAnswers)+" / 12");
            if(id=="train")return T("科技购买\n训练曲线","Buy Training\ncurve in Tech");
            if(id=="home")return T("和它聊过一次","Talk to it first");
            return TabHelp(id);
        }
        static void PlaceSidebarToggle(XgBtn button,float x)
        {
            button.rt.anchorMin=button.rt.anchorMax=new Vector2(.5f,0);button.rt.pivot=new Vector2(.5f,0);
            button.rt.anchoredPosition=new Vector2(x,8);button.rt.sizeDelta=new Vector2(38,38);
            button.image.sprite=LingGuangV05.Desktop.Story.PrologueDesk.Circle();button.image.type=Image.Type.Simple;
            button.label.fontSize=12;button.label.alignment=TextAlignmentOptions.Center;
        }
        sealed class NavResize : MonoBehaviour
        {
            public Action resized;
            void OnRectTransformDimensionsChange(){resized?.Invoke();}
        }

        void PaintNavRow(NavRow row)
        {
            bool on = row.id == tab;
            bool open = row.button.interactable;
            row.back.color = on ? new Color(1,1,1,.10f) : row.hover && open ? new Color(1,1,1,.04f) : Color.clear;
            row.accent.enabled = false;
            row.label.color = !open ? XgDark.Dim : on || row.hover ? XgDark.Ink : XgDark.Muted;
            if (row.icon != null) row.icon.color = !open ? new Color(1, 1, 1, .35f) : on || row.hover ? Color.white : new Color(1, 1, 1, .85f);
        }
    }
}

