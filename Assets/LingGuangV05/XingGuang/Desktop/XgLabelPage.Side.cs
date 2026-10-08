using System;
using System.Text;
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
    /// The right column of the 标注台 (a scrolling stack of boxes): 今天 with the raise ladder, the 灵光 panel (the
    /// spider's corner, its speech bubble and the drag hint), the auto-answer and data rows with the collaboration
    /// panel, and the box that replaces them during the research and finale cards.
    /// </summary>
    public sealed partial class XgLabelPage
    {
        const float TodayH = 214, AiH = 134, RowH = 84, CoopH = 142, ChipH = 34, LinkH = 30;

        RectTransform sideContent, todayBox, aiBox, shopBox, specialBox, raiseBlock;
        readonly TMP_Text[] dayValues = new TMP_Text[4];
        TMP_Text dayNote, raiseMul, raiseName, todayTitle, shopTitle, specialTitle, historyCaption;
        readonly TMP_Text[] dayLabels = new TMP_Text[4];
        Image[] ladder;
        XgBtn raiseBtn;
        float sideHeight;

        void BuildSide(RectTransform parent)
        {
            var viewport = Rect("Side", parent, new Vector2(1, 0), Vector2.one, new Vector2(-SideW, 0), Vector2.zero);
            Panel(viewport, new Color(1, 1, 1, 0));
            viewport.gameObject.AddComponent<RectMask2D>();
            sideContent = Rect("Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            sideContent.pivot = new Vector2(.5f, 1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = sideContent; scroll.viewport = viewport; scroll.horizontal = false; scroll.scrollSensitivity = 30; scroll.movementType = ScrollRect.MovementType.Clamped;
            BuildToday();
            BuildAiPanel();
            BuildShop();
            BuildSpecialBox();
        }

        void PlaceBox(RectTransform box, float y, float h)
        {
            box.anchorMin = new Vector2(0, 1); box.anchorMax = new Vector2(1, 1);
            box.offsetMin = new Vector2(0, -y - h); box.offsetMax = new Vector2(0, -y);
        }

        // ───────────── 今天 and the raise ladder ─────────────

        void BuildToday()
        {
            todayBox = ZhongbaoSkin.Box(ui, sideContent, "Today", new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            ZhongbaoSkin.Header(ui, todayBox, "今天", "Today", out todayTitle, out dayNote);
            string[] zh = { "已标", "正确率", "收入", "交给灵光的样本" }, en = { "Done", "Accuracy", "Income", "Samples for LingGuang" };
            for (int i = 0; i < 4; i++)
            {
                float y = 40 + i * 20;
                var label = ui.Text(TopLeft("Label" + i, todayBox, 12, y, 170, 20), T(zh[i], en[i]), 12, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                dayLabels[i] = label;
                dayValues[i] = ui.Text(Rect("Value" + i, todayBox, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-120, -y - 20), new Vector2(-12, -y)), "", 13, i == 2 ? ZhongbaoSkin.Red : ZhongbaoSkin.Ink, TextAlignmentOptions.MidlineRight);
                dayValues[i].fontStyle = FontStyles.Bold;
            }
            UiTip.Add(dayValues[3], "你亲手标对（和灵光替你标对）的每一条都是它的训练数据。", "Every label you get right by hand, or LingGuang gets right for you, is training data for it.");
            // The raise: price per card, the ladder of levels and the button. The guide's "name:Row薪" ring lands here.
            raiseBlock = Rect("Row薪", todayBox, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -TodayH + 4), new Vector2(0, -126));
            var rule = ZhongbaoSkin.Hairline(raiseBlock, 12, true);
            raiseName = ui.Text(Rect("Name", raiseBlock, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -28), new Vector2(-70, -8)), "", 12, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
            raiseName.textWrappingMode = TextWrappingModes.NoWrap; raiseName.overflowMode = TextOverflowModes.Ellipsis;
            raiseMul = ui.Text(Rect("Multiplier", raiseBlock, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -30), new Vector2(-12, -6)), "", 17, ZhongbaoSkin.Red, TextAlignmentOptions.MidlineRight);
            raiseMul.fontStyle = FontStyles.Bold;
            int cells = XgCatalog.RaiseMax;
            ladder = new Image[cells];
            var ladderRow = Rect("Ladder", raiseBlock, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -40), new Vector2(-12, -35));
            for (int i = 0; i < cells; i++)
            {
                var c = Rect("Step" + i, ladderRow, new Vector2((float)i / cells, 0), new Vector2((i + 1f) / cells, 1), new Vector2(i == 0 ? 0 : 1, 0), Vector2.zero);
                ladder[i] = Panel(c, ZhongbaoSkin.Track);
                ladder[i].raycastTarget = false;
            }
            raiseBtn = ui.Button(raiseBlock, "", BuyRaise, 14);
            raiseBtn.rt.anchorMin = new Vector2(0, 1); raiseBtn.rt.anchorMax = new Vector2(1, 1);
            raiseBtn.rt.offsetMin = new Vector2(12, -80); raiseBtn.rt.offsetMax = new Vector2(-12, -48);
            raiseBtn.label.fontStyle = FontStyles.Bold;
            UiTip.Add(raiseBlock, "加薪：每升一级，手动标注每题拿得更多。\n每两级换一个头衔。灵光替你按的自动答题不吃加薪。", "Pay raise: every level pays more per hand-labelled card.\nA new title every two levels. Auto-answer does not get the raise.");
            UiTip.Add(raiseBtn.rt, "加薪：每升一级，手动标注每题拿得更多。\n每两级换一个头衔。", "Pay raise: every level pays more per hand-labelled card.\nA new title every two levels.");
        }

        void RefreshToday()
        {
            string date = Sim.TodayKnown ? T(Sim.Today / 100 % 100 + "月" + Sim.Today % 100 + "日", new DateTime(2016, Math.Max(1, Math.Min(12, Sim.Today / 100 % 100)), Math.Max(1, Math.Min(28, Sim.Today % 100))).ToString("MMM d", System.Globalization.CultureInfo.InvariantCulture)) : "";
            dayNote.text = date;
            int done = Sim.DayDone;
            dayValues[0].text = done + T(" 条", "");
            dayValues[1].text = done > 0 ? Math.Round(100.0 * Sim.DayRight / done) + "%" : "—";
            double income = Sim.DayIncome;
            dayValues[2].text = (income < 0 ? "−¥" : "¥") + N(Math.Abs(income), "0.00");
            dayValues[3].text = Sim.DaySamples.ToString();

            int rl = Sim.RaiseLevel;
            bool maxed = rl >= XgCatalog.RaiseMax;
            int count = host.BuyAmount == int.MaxValue ? Math.Max(1, Sim.AffordableRaises(Host)) : host.BuyAmount;
            double cost = Sim.RaiseCostFor(count, out int levels);
            double nextMult = XgCatalog.RaiseMultipliers[Math.Min(XgCatalog.RaiseMax, rl + Math.Max(1, levels))];
            raiseName.text = T("人工标注单价", "Hand pay") + " · " + XgCatalog.RaiseTitle(rl, En) + "  <color=#B4B8C2>Lv " + rl + "/" + XgCatalog.RaiseMax + "</color>";
            raiseMul.text = "×" + N(Sim.RaiseMultiplier, "0");
            for (int i = 0; i < ladder.Length; i++) ladder[i].color = i < rl ? ZhongbaoSkin.Red : ZhongbaoSkin.Track;
            bool can = !maxed && Host.Money + 1e-9 >= cost;
            raiseBtn.Set(maxed ? T("已经最高", "Maxed out") : T("加薪", "Raise") + (levels > 1 ? " +" + levels : "") + " → ×" + N(nextMult, "0") + " · ¥" + Money(cost), can, can ? ZhongbaoSkin.Gold : (Color?)null, can ? Color.white : (Color?)null);
        }

        // ───────────── the 灵光 panel ─────────────

        RectTransform aiHome;
        XgCrawlerLayer aiIcon;
        TMP_Text aiName, aiSub, bubbleText, aiHint;
        Image bubbleBack;
        float bubbleTimer;
        string bubbleDefault = "……";

        void BuildAiPanel()
        {
            aiBox = ZhongbaoSkin.Box(ui, sideContent, "LingGuang", new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            aiHome = TopLeft("Home", aiBox, 10, 10, 36, 36);
            aiIcon = ZhongbaoSkin.Canvas(aiHome, DrawAiIcon);
            aiName = ui.Text(TopLeft("Name", aiBox, 54, 9, SideW - 64, 20), "", 12, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
            aiName.textWrappingMode = TextWrappingModes.NoWrap; aiName.overflowMode = TextOverflowModes.Ellipsis;
            aiSub = ui.Text(TopLeft("State", aiBox, 54, 27, SideW - 64, 18), "", 11, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
            aiSub.textWrappingMode = TextWrappingModes.NoWrap; aiSub.overflowMode = TextOverflowModes.Ellipsis;
            var bubble = TopLeft("Bubble", aiBox, 10, 52, SideW - 20, 34);
            bubbleBack = Panel(bubble, ZhongbaoSkin.SpiderSoft); bubbleBack.raycastTarget = false;
            var rim = bubble.gameObject.AddComponent<UnityEngine.UI.Outline>();
            rim.effectColor = ZhongbaoSkin.SpiderLine; rim.effectDistance = new Vector2(1, -1);
            bubbleText = ui.Text(Rect("Text", bubble, Vector2.zero, Vector2.one, new Vector2(8, 2), new Vector2(-8, -2)), "", 12, new Color32(75, 63, 168, 255), TextAlignmentOptions.MidlineLeft);
            bubbleText.enableAutoSizing = true; bubbleText.fontSizeMin = 9; bubbleText.fontSizeMax = 12;
            aiHint = ui.Text(TopLeft("Hint", aiBox, 10, 90, SideW - 20, 40), "", 11, ZhongbaoSkin.Mute, TextAlignmentOptions.TopLeft);
            UiTip.Add(aiHome, "灵光的小窝。把桌面上的它拖到题目旁边，它就会伸手替你答题；再拖开，它就停下。", "LingGuang's corner. Drag it from the desktop onto the question and it reaches out to answer for you; drag it away and it stops.");
        }

        /// <summary>The little spider in the panel's corner: dim and dashed while the real one is out working.</summary>
        void DrawAiIcon(VertexHelper vh)
        {
            var r = aiHome.rect;
            var c = r.center;
            float k = r.width / 34f;
            bool away = Spider != SpiderRole.Home;
            float a = away ? .28f : 1;
            var leg = new Color(.18f, .44f, .37f, a);
            Vector2[][] legs =
            {
                new[] { new Vector2(-5, 3), new Vector2(-13, 9), new Vector2(-16, 4) }, new[] { new Vector2(-5, 0), new Vector2(-14, 0), new Vector2(-17, -4) },
                new[] { new Vector2(-5, -3), new Vector2(-12, -9), new Vector2(-15, -14) }, new[] { new Vector2(5, 3), new Vector2(13, 9), new Vector2(16, 4) },
                new[] { new Vector2(5, 0), new Vector2(14, 0), new Vector2(17, -4) }, new[] { new Vector2(5, -3), new Vector2(12, -9), new Vector2(15, -14) },
            };
            foreach (var l in legs)
            {
                XgDraw.Seg(vh, c + l[0] * k, c + l[1] * k, 1.4f, leg);
                XgDraw.Seg(vh, c + l[1] * k, c + l[2] * k, 1.4f, leg);
            }
            var glow = new Color(.36f, .79f, .65f, .35f * a);
            XgDraw.Disc(vh, c, 12 * k, glow, 16);
            XgDraw.Disc(vh, c, 8 * k, new Color(.06f, .24f, .2f, a), 16);
            XgDraw.Disc(vh, c + new Vector2(-2, 2) * k, 4.5f * k, new Color(.36f, .79f, .65f, a), 12);
            if (away) XgDraw.Ring(vh, c, 15.5f * k, 1f, new Color(.81f, .79f, .96f, 1), 24);
        }

        void RefreshAi()
        {
            bool eligible = Mode == 0 && Sim.EligibleDesk(Desk);
            string name = T("灵光", "LingGuang");
            switch (Spider)
            {
                case SpiderRole.Working:
                    aiName.text = name + T(" · 在替你答题", " · answering for you");
                    break;
                case SpiderRole.Held:
                    aiName.text = name + T(" · 放到题目旁边", " · drop it by the question");
                    break;
                default:
                    aiName.text = name + T(" · 在旁边看", " · watching");
                    break;
            }
            double acc = Sim.BestAcc(Desk);
            aiSub.text = eligible ? T("这桌能答 · 检查点 " + XgSim.Pct(acc), "Can answer this desk · checkpoint " + XgSim.Pct(acc))
                : acc > 0 ? T("这桌还不会 · 检查点 " + XgSim.Pct(acc) + "，要 60%", "Cannot do this desk yet · checkpoint " + XgSim.Pct(acc) + ", needs 60%")
                : T("这桌还不会 · 还没有检查点", "Cannot do this desk yet · no checkpoint");
            var info = XgCatalog.Desk(Desk);
            double fine = info != null && info.fine > 0 ? Sim.HandFineFor(Desk, Sim.LevelOf(Desk)) : 0;
            aiHint.text = T("把它拖到题目旁边，它会伸手替你答。", "Drag it by the question and it reaches out to answer for you. ")
                + (fine > 0 ? "<color=#E33E33>" + T("那时它答错要扣你的钱（每错 −¥" + N(fine, "0.00") + "）。", "Then every wrong answer costs your money (−¥" + N(fine, "0.00") + " each).") + "</color>"
                    : "<color=#E33E33>" + T("那时它答错会断掉连击。", "Then a wrong answer breaks the combo.") + "</color>")
                + "\n" + T("放在这里时它偶尔顺手标一题，错了不扣。", "While it sits here it sometimes labels one on its own; a miss costs nothing.");
            if (bubbleTimer <= 0 && bubbleText.text != bubbleDefault) bubbleText.text = bubbleDefault;
            else if (bubbleTimer <= 0 && string.IsNullOrEmpty(bubbleText.text)) bubbleText.text = bubbleDefault;
            aiIcon.Redraw();
        }

        // ───────────── auto-answer, data, collaboration ─────────────

        void BuildShop()
        {
            shopBox = ZhongbaoSkin.Box(ui, sideContent, "Shop", new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            ZhongbaoSkin.Header(ui, shopBox, "自动答题 · 数据", "Auto-answer · data", out shopTitle, out _);
            amountBtns = new XgBtn[3];
            string[] amounts = { "×1", "×10", T("最大", "Max") };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                amountBtns[i] = ui.Button(shopBox, amounts[i], () => { host.BuyAmount = index == 0 ? 1 : index == 1 ? 10 : int.MaxValue; Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 12);
                amountBtns[i].rt.anchorMin = amountBtns[i].rt.anchorMax = new Vector2(1, 1);
                float right = 8 + (2 - i) * 44;
                amountBtns[i].rt.offsetMin = new Vector2(-right - 40, -29); amountBtns[i].rt.offsetMax = new Vector2(-right, -6);
            }
            auto = new XgShopRow(ui, shopBox, "自", XgPalette.Accent, BuyAuto);
            pack = new XgShopRow(ui, shopBox, "包", new Color32(18, 150, 140, 255), BuyPack);
            BuildSourceSwitch();
            BuildCollaborationPanel(shopBox);
            BuildPlatformChip(shopBox);
            toTrain = ZhongbaoSkin.Outlined(ui, shopBox, "", () => { Sim.SelectedTrack = XgCatalog.Dataset(Desk).track; Sim.SetDataset(Sim.SelectedTrack, Desk); host.ShowTab("train"); }, 14, ZhongbaoSkin.Blue);
            ZhongbaoSkin.Frame(toTrain).name = "ToTrain";
            ZhongbaoSkin.Frame(toTrain).anchorMin = new Vector2(0, 1); ZhongbaoSkin.Frame(toTrain).anchorMax = new Vector2(1, 1);
            UiTip.Add(toTrain.rt, "带着这张桌的数据去训练页。", "Go to the training page with this desk's data.");
        }

        /// <summary>Stacks the shop box's rows from the top, hiding the empty ones; returns the box's height.</summary>
        float LayoutShop(bool coop, bool chip, bool packRow)
        {
            float y = 40;
            auto.Root.anchorMin = pack.Root.anchorMin = new Vector2(0, 1);
            auto.Root.anchorMax = pack.Root.anchorMax = new Vector2(1, 1);
            Place(auto.Root, y, RowH); y += RowH + 4;
            if (packRow) { Place(pack.Root, y, RowH); y += RowH + 4; }
            if (coop) { Place(coopPanel, y, CoopH); y += CoopH + 4; }
            if (chip) { Place(platformChip, y, ChipH); y += ChipH + 4; }
            var link = ZhongbaoSkin.Frame(toTrain);
            if (link.gameObject.activeSelf) { link.offsetMin = new Vector2(8, -y - LinkH); link.offsetMax = new Vector2(-8, -y); y += LinkH + 4; }
            return y + 4;
        }

        static void Place(RectTransform rt, float y, float h)
        {
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(8, -y - h); rt.offsetMax = new Vector2(-8, -y);
        }

        // ───────────── the research and finale cards ─────────────

        void BuildSpecialBox()
        {
            specialBox = ZhongbaoSkin.Box(ui, sideContent, "Special", new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            ZhongbaoSkin.Header(ui, specialBox, "特别题", "Special card", out specialTitle, out _);
            specialInfo = ui.Text(Rect("ResearchInfo", specialBox, Vector2.zero, Vector2.one, new Vector2(14, 12), new Vector2(-14, -44)), "", 15, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            specialBox.gameObject.SetActive(false);
        }

        /// <summary>Positions the boxes in the column for the current state and sizes the scrolling content.</summary>
        void LayoutSide(bool special, float shopHeight)
        {
            todayBox.gameObject.SetActive(!special);
            aiBox.gameObject.SetActive(!special && Sim.SpiderAround);
            shopBox.gameObject.SetActive(!special);
            specialBox.gameObject.SetActive(special);
            float y = 0;
            if (special) { PlaceBox(specialBox, 0, 300); y = 300; }
            else
            {
                PlaceBox(todayBox, y, TodayH); y += TodayH + 8;
                if (aiBox.gameObject.activeSelf) { PlaceBox(aiBox, y, AiH); y += AiH + 8; }
                PlaceBox(shopBox, y, shopHeight); y += shopHeight + 8;
            }
            sideHeight = y;
            sideContent.sizeDelta = new Vector2(0, y);
        }

        /// <summary>The detailed pay formula of the current card, shown when the pointer rests on the pay note above the workspace.</summary>
        string PayTip()
        {
            string desk = Desk;
            int dl = Sim.LevelOf(desk);
            double kind = XgCatalog.Desk(desk)?.pay ?? 1;
            string kindZh = Math.Abs(kind - 1) > 1e-9 ? " · 题型 ×" + N(kind, "0.0#") : "", kindEn = Math.Abs(kind - 1) > 1e-9 ? " · card type ×" + N(kind, "0.0#") : "";
            double fine = Sim.HandFineFor(desk, dl);
            return T("手动标注：基础 ¥" + N(XgCatalog.LabelPay, "0.0#") + " × 手动 ×" + N(XgCatalog.HandPayScale, "0.0#") + kindZh + " × 难度 ×" + N(1 + .5 * (dl - 1), "0.0") + " × 加薪 ×" + N(Sim.RaiseMultiplier, "0") + " × 连击 ×" + N(Sim.ComboMultiplier, "0.00")
                    + "  =  每题 ¥" + N(Sim.ManualPayFor(desk, dl) * Sim.ComboMultiplier, "0.00")
                    + (fine > 0 ? "\n点错：扣 ¥" + N(fine, "0.00") + "（右答案的一半，随加薪涨，不随连击涨），连击清零。" : "")
                    + "\n答对 +1 连击，题桌之间不断；答错、超时、NaN 清零。",
                "Hand labelling: base ¥" + N(XgCatalog.LabelPay, "0.0#") + " × hand ×" + N(XgCatalog.HandPayScale, "0.0#") + kindEn + " × difficulty ×" + N(1 + .5 * (dl - 1), "0.0") + " × raise ×" + N(Sim.RaiseMultiplier, "0") + " × combo ×" + N(Sim.ComboMultiplier, "0.00")
                    + " = ¥" + N(Sim.ManualPayFor(desk, dl) * Sim.ComboMultiplier, "0.00") + " per card"
                    + (fine > 0 ? "\nA wrong pick costs ¥" + N(fine, "0.00") + " (half of a right answer's pay: it rises with the raise, not with the combo) and resets the combo." : "")
                    + "\nA right answer adds 1 to the combo, across desks; a wrong one, a timeout or NaN resets it.");
        }

        // ───────────── the last 20 answers ─────────────

        /// <summary>One square of the strip under the workspace.</summary>
        enum HistoryMark : sbyte { Right, Wrong, SpiderRight, SpiderWrong }

        const int HistorySize = 20;
        readonly System.Collections.Generic.List<HistoryMark> history = new System.Collections.Generic.List<HistoryMark>();
        Image[] historyCells;
        TMP_Text[] historyMarks;
        TMP_Text historyLegend;

        void BuildHistoryStrip()
        {
            var strip = Rect("Recent", workspace, Vector2.zero, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-1, 35));
            ZhongbaoSkin.Hairline(strip, 0, true);
            var caption = historyCaption = ui.Text(Rect("Caption", strip, Vector2.zero, new Vector2(0, 1), new Vector2(12, 0), new Vector2(76, 0)), T("最近 20 题", "Last 20"), 12, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            historyCells = new Image[HistorySize];
            historyMarks = new TMP_Text[HistorySize];
            float x = 78;
            for (int i = 0; i < HistorySize; i++)
            {
                var cell = Rect("Answer" + i, strip, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(x, -6), new Vector2(x + 12, 6));
                historyCells[i] = Panel(cell, ZhongbaoSkin.Track);
                historyCells[i].raycastTarget = false;
                historyMarks[i] = ui.Text(Rect("Mark", cell, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 9, Color.white, TextAlignmentOptions.Center);
                historyMarks[i].textWrappingMode = TextWrappingModes.NoWrap;
                x += 15;
            }
            historyLegend = ui.Text(Rect("Legend", strip, Vector2.zero, Vector2.one, new Vector2(x + 8, 0), new Vector2(-12, 0)), T("「灵」= 灵光答的 · 红 = 错", "灵 = LingGuang's · red = wrong"), 11, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineRight);
            historyLegend.textWrappingMode = TextWrappingModes.NoWrap; historyLegend.overflowMode = TextOverflowModes.Ellipsis;
            RefreshHistory();
        }

        void Remember(HistoryMark mark)
        {
            history.Add(mark);
            while (history.Count > HistorySize) history.RemoveAt(0);
            RefreshHistory();
        }

        void RefreshHistory()
        {
            if (historyCells == null) return;
            for (int i = 0; i < HistorySize; i++)
            {
                bool has = i < history.Count;
                var m = has ? history[i] : HistoryMark.Right;
                historyCells[i].color = !has ? ZhongbaoSkin.Track : (m == HistoryMark.Right || m == HistoryMark.SpiderRight) ? ZhongbaoSkin.Green : ZhongbaoSkin.Red;
                historyMarks[i].text = has && (m == HistoryMark.SpiderRight || m == HistoryMark.SpiderWrong) ? T("灵", "L") : "";
            }
            historyLegend.text = T("「灵」= 灵光答的 · 红 = 错", "L = LingGuang's · red = wrong");
        }

        bool builtEnglish;

        /// <summary>The titles and captions built once follow a change of language.</summary>
        void RelabelStatic()
        {
            if (builtEnglish == En) return;
            builtEnglish = En;
            hallTitle.text = T("任务大厅", "Task hall");
            todayTitle.text = T("今天", "Today");
            shopTitle.text = T("自动答题 · 数据", "Auto-answer · data");
            specialTitle.text = T("特别题", "Special card");
            historyCaption.text = T("最近 20 题", "Last 20");
            string[] zh = { "已标", "正确率", "收入", "交给灵光的样本" }, en = { "Done", "Accuracy", "Income", "Samples for LingGuang" };
            for (int i = 0; i < 4; i++) dayLabels[i].text = T(zh[i], en[i]);
            amountBtns[2].label.text = T("最大", "Max");
            RefreshHistory();
        }
    }
}
