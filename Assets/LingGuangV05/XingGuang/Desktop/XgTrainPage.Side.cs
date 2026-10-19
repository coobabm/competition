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
    /// The right column of the training page: 有效规模 (stage 6: the factors of the effective scale against the ×2000 the
    /// pre-training needs; stages 1–5: the next ability's two bars), 装备 (what is owned and always on, what can be bought
    /// from here), and 今天的账 (the household bills the training adds to). The 装备 card has a second view, 数据集, that
    /// holds what the old model card kept: the datasets, the data packs to drag onto the GPU, the noise line and cleaning.
    /// </summary>
    public sealed partial class XgTrainPage
    {
        // The column stacks the module prefabs (XgModuleRefs, Resources/LingGuangV05/Training/TrainPage) in a vertical
        // layout: 有效规模 and 今天的账 take their content's height, 装备 takes the rest, and the column scrolls only when the
        // modules need more than its height (layout spec: side-session notes, train-page-module-layout-spec.md).
        const float LedgerHeight = 100, SideGap = 8;
        RectTransform sideContent;

        // 有效规模
        RectTransform scaleCard, nextArea, formulaArea, logBack, logFill, logMark, pFill, dFill;
        TMP_Text scaleTitle, scaleTag, scaleBig, scaleVerdict, logLabel, pLabel, pValue, dLabel, dValue, pSub, dSub, probeText;
        readonly List<Chip> formulaChips = new List<Chip>();
        const int FormulaChipCount = 2 + 7;

        // 装备 / 数据集
        RectTransform gearCard, gearPane, dataPane;
        XgBtn gearTab, dataTab, learningTab, confirmStage;
        bool learningView;
        RectTransform learningPane;
        TMP_Text learningText;
        TMP_Text gearCount;
        bool dataView;
        sealed class Tile
        {
            public RectTransform rt;
            public XgFrameGraphic frame;
            public TMP_Text name, big, state;
        }
        readonly List<Tile> tiles = new List<Tile>();
        const int GearTiles = 12;
        static readonly string[] PassiveGear = { "dropout", "augment", "cudnn", "cooler" };

        // 今天的账
        RectTransform ledgerCard;
        XgBtn fundsBook;
        TMP_Text ledgerHead, ledgerDate, ledgerWarn;
        readonly TMP_Text[] ledgerLabels = new TMP_Text[3], ledgerValues = new TMP_Text[3];

        void BuildSide()
        {
            Panel(right, Color.clear).raycastTarget = true;
            right.gameObject.AddComponent<RectMask2D>();
            sideContent = Rect("SidebarModules", right, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            sideContent.pivot = new Vector2(.5f, 1);
            var column = sideContent.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = SideGap; column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = column.childControlHeight = true; column.childForceExpandWidth = true; column.childForceExpandHeight = false;
            var fill = sideContent.gameObject.AddComponent<XgFillFitter>(); fill.viewport = right;
            right.gameObject.AddComponent<XgFillViewport>().fitter = fill;
            var scroll = right.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = right; scroll.content = sideContent; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 25;
            BuildRelicAccess();
            BuildScaleCard();
            BuildGearCard();
            BuildLedgerCard();
            BuildLedgerSeal();
        }

        // ───────────── 有效规模 ─────────────

        void BuildScaleCard()
        {
            var m = XgModuleRefs.Spawn("TrainScaleModule", sideContent, ui);
            scaleCard = m.Get("card"); scaleCard.name = "Scale";
            scaleTitle = m.Text("title"); scaleTag = m.Text("tag"); scaleBig = m.Text("big");
            // Stage 6: the factors as chips (owned solid, missing dashed), three to a row, then a log-scale bar with the ×2000 mark.
            formulaArea = m.Get("six");
            for (int i = 0; i < FormulaChipCount; i++)
            {
                var chip = m.Get("chip" + i);
                formulaChips.Add(new Chip { rt = chip, frame = chip.GetComponent<XgFrameGraphic>(), text = chip.GetComponentInChildren<TMP_Text>(true) });
            }
            logBack = m.Get("logBar"); logFill = m.Get("logFill"); logMark = m.Get("logMark"); logLabel = m.Text("logLabel");
            scaleVerdict = m.Text("verdict");

            // Stages 1–5: the next ability's two bars.
            nextArea = m.Get("next");
            pLabel = m.Text("pLabel"); pValue = m.Text("pValue"); pSub = m.Text("pSub"); pFill = m.Get("pFill");
            dLabel = m.Text("dLabel"); dValue = m.Text("dValue"); dSub = m.Text("dSub"); dFill = m.Get("dFill");
            // 「同一道题」: the next ability's fixed question and what it answered after the last round (XgSim.Learned.cs).
            // Two lines (the question, then its answer); the card grows when the English wraps further.
            probeText = m.Text("probe");
            // The chapter turns through the 能力考 (XgTrainPage.Exam.cs). Long labels wrap; the button grows.
            confirmStage = m.Button("chapter", ui, "", ChapterButton, 13, "ConfirmNextStage");
            confirmStage.label.textWrappingMode = TextWrappingModes.Normal;
            UiTip.Add(scaleCard, () => ScaleTip());
        }

        static float LogShare(double v) => (float)Math.Min(1, Math.Log10(Math.Max(1, v)) / Math.Log10(5000));

        void RefreshScale(XgTrack track, XgRun run)
        {
            bool six = Sim.S.stage >= 6 || Sim.NextAbility == 0;
            Part(formulaArea, six); Part(nextArea, !six);
            if (!six) { RefreshChapterButton(); XgSlotWidth.FitHeight(confirmStage, 30, 12); }
            if (six)
            {
                double eff = Sim.EffectiveScale, need = XgSim.ScaleNeed;
                scaleTitle.text = T("有效规模 · 预训练要 ×" + N(need, "0"), "EFFECTIVE SCALE");
                scaleBig.text = "×" + N(eff, "#,##0") + "  <size=13><color=#6F95A5>/ ×" + N(need, "0") + "</color></size>";
                bool ready = Sim.PretrainScaleReady;
                scaleTag.text = ready ? Hex(XgDark.Good) + T("够了 · 可以预训练", "Enough · can pre-train") + "</color>"
                    : Hex(XgDark.Bad) + T("不够 · 预训练会卡在 " + N(Math.Min(1, Sim.ScaleRatio) * 100, "0") + "%", "Short · stalls at " + N(Math.Min(1, Sim.ScaleRatio) * 100, "0") + "%") + "</color>";

                // 参数 × 数据 × each multiplier: owned solid, missing dashed.
                var chips = new List<(string text, Color ink, bool own)>();
                chips.Add((T("参数 ", "Params ") + N(Sim.ScaleParamsRatio, "0.00"), XgDark.Params, true));
                chips.Add(("×" + T("数据 ", "Data ") + N(Sim.ScaleDataRatio, "0.00"), XgDark.Data, true));
                foreach (var item in XgSim.ScaleItems)
                    chips.Add(("×" + T(item.name, item.nameEn) + " " + N(item.factor, "0.#"), Sim.Has(item.item) ? XgDark.Money : XgDark.Dim, Sim.Has(item.item)));
                for (int i = 0; i < formulaChips.Count; i++)
                {
                    var c = formulaChips[i];
                    c.text.text = chips[i].text; c.text.color = chips[i].ink;
                    bool ratio = i < 2;
                    c.frame.Style(ratio ? new Color(chips[i].ink.r, chips[i].ink.g, chips[i].ink.b, .08f) : chips[i].own ? new Color(.98f, .78f, .46f, .1f) : Color.clear,
                        ratio ? new Color(chips[i].ink.r, chips[i].ink.g, chips[i].ink.b, .55f) : chips[i].own ? new Color32(110, 90, 46, 255) : XgDark.Line, !ratio && !chips[i].own);
                }
                logFill.anchorMax = new Vector2(LogShare(eff), 1);
                var fillImage = logFill.GetComponent<Image>(); if (fillImage != null) fillImage.color = ready ? XgDark.Good : XgDark.Params;
                float mark = LogShare(need);
                logMark.anchorMin = new Vector2(mark, 0); logMark.anchorMax = new Vector2(mark, 1);
                // The mark's label sits under the mark, kept inside the card.
                logLabel.text = "×" + N(need, "0");
                var labelRow = (RectTransform)logLabel.rectTransform.parent;
                float barWidth = labelRow.rect.width > 10 ? labelRow.rect.width : 386, at = barWidth * mark;
                logLabel.rectTransform.anchorMin = logLabel.rectTransform.anchorMax = new Vector2(mark, 1);
                logLabel.rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(at, 30, barWidth - 30) - at, 0);
                scaleVerdict.text = ScaleVerdict();
            }
            else
            {
                int next = Sim.NextAbility;
                double paramsNeed = Sim.ParamsThreshold(next), samplesNeed = Sim.SamplesThreshold(next), size = XgSim.ParamsK(run);
                scaleTitle.text = T("下一项能力 · 能力 " + next + "「" + XgSim.AbilityName(next, false) + "」", "NEXT ABILITY · " + next + " '" + XgSim.AbilityName(next, true) + "'");
                scaleTag.text = "";
                // The line is hidden (XgSim.Learned.cs): how much is trained, and 「?」 until the bar is past it.
                pLabel.text = T("参数量", "Parameters"); pValue.text = XgSim.ParamsText(Sim.TrainedParamsK) + " <size=11>" + Hex(XgDark.Muted) + "· " + Sim.ParamsLineText(next) + "</color></size>";
                dLabel.text = T("样本", "Samples"); dValue.text = XgSim.SamplesText(Sim.TrainedSamples) + " <size=11>" + Hex(XgDark.Muted) + "· " + Sim.SamplesLineText(next) + "</color></size>";
                SetBar(pFill, (float)Sim.ParamsFill); SetBar(dFill, (float)Sim.SamplesFill);
                bool probeRight;
                string probe = Sim.ProbeLine(out probeRight);
                probeText.gameObject.SetActive(probe.Length > 0);
                probeText.text = Hex(XgDark.Dim) + T("同一道题：", "Same question: ") + "</color>" + probe + " " + (probeRight ? Hex(XgDark.Good) + "✓" : Hex(XgDark.Bad) + "×") + "</color>";
                // This model against what already counts (P only takes a model trained to grade C), and the samples there against the samples that count.
                pSub.text = Sim.ParamsSplitText(run); pSub.color = Sim.CountState(run) == XgCountState.Counted ? XgDark.Muted : XgDark.Gold;
                dSub.text = Sim.SamplesSplitText();
                string line;
                if (Sim.TrainedParamsK + 1e-9 < paramsNeed)
                    line = size + 1e-9 >= paramsNeed ? Hex(XgDark.Muted) + T("这个模型够大：练到 C 级就算数。", "This model is big enough: it counts once trained to grade C.") + "</color>"
                        : Hex(XgDark.Gold) + T("这个模型可能还不够大：去科技加宽、加深，或在道具买更大的结构。", "This model may not be big enough yet: widen or deepen in the tech tree, or buy a bigger structure on the Items page.") + "</color>";
                else line = Hex(XgDark.Muted) + T("参数够了，样本到门槛，它就学会这一项。", "The parameters are there; once the samples reach the line it learns this ability.") + "</color>";
                // The size's makeup (layers × width² × structure) and the caps the tech tree sets for it.
                scaleVerdict.text = line + "\n" + Hex(XgDark.Dim) + Sim.ModelFormula(run) + " · " + Sim.CapsText(run) + "</color>";
            }
        }

        string ScaleVerdict()
        {
            var missing = Sim.MissingScaleItems();
            if (Sim.PretrainScaleReady) return Hex(XgDark.Good) + T("够了：倍率和参数、数据都到位，可以去终章页预训练。", "Enough: multipliers, parameters and data are in; pre-training can run on the Finale page.") + "</color>";
            if (missing.Count == 0) return Hex(XgDark.Muted) + T("倍率都齐了，把参数和数据再练大一点。", "Every multiplier is in; train more parameters and data.") + "</color>";
            var list = new List<string>();
            foreach (var m in missing) list.Add(Hex(XgDark.Money) + T(m.name, m.nameEn) + " ×" + N(m.factor, "0.#") + "</color>");
            return T("还缺：", "Still missing: ") + string.Join(T("、", ", "), list) + "\n" + Hex(XgDark.Muted) + T("倍率是乘起来的：缺大的，堆满参数和数据也补不上。", "Multipliers stack: a missing big one cannot be made up.") + "</color>";
        }

        string ScaleTip()
        {
            if (Sim.S.stage < 6 && Sim.NextAbility != 0)
                return (Sim.ReadWordsTotal >= 1 ? Sim.ReadLine() + "\n" : "") + Sim.ParamsReport(Host) + "\n\n" + Sim.SamplesReport() + "\n\n"
                    + Sim.FogNote + "\n" + T("「同一道题」每一轮都问一次：它学会之前，怎么练都答不对。", "\"Same question\" is asked every round: until it learns, no amount of training gets it right.")
                    + "\n" + T("阶段 1–5 靠参数和样本；下面灰色的格子是阶段 6 才用得上的倍率。", "Stages 1–5 run on parameters and samples; the greyed tiles below are multipliers that only count at stage 6.");
            return T("有效规模 = 参数 × 数据 × 买到的倍率。参数以 " + XgSim.ParamsText(XgSim.ScaleUnitParamsK) + "、数据以 " + XgSim.SamplesText(XgSim.ScaleUnitSamples) + " 条算 1 份，各算到 " + N(XgSim.ScaleBarCap, "0.#") + " 份为止；倍率是道具带来的，乘在一起。预训练要 ×" + N(XgSim.ScaleNeed, "0") + "：三件大的（Transformer ×10、机房 ×8、注意力 ×3）一样都不能少，小的最多缺一件。\n条是对数刻度，白线是 ×" + N(XgSim.ScaleNeed, "0") + "。",
                "Effective scale = parameters × data × the multipliers you own. Parameters count in units of " + XgSim.ParamsText(XgSim.ScaleUnitParamsK) + " and data in units of " + XgSim.SamplesText(XgSim.ScaleUnitSamples) + " samples, each up to " + N(XgSim.ScaleBarCap, "0.#") + " units; the multipliers come from items and multiply. Pre-training needs ×" + N(XgSim.ScaleNeed, "0") + ": none of the three big ones (Transformer ×10, server room ×8, attention ×3) can be missing, and at most one small one.\nThe bar is a log scale; the white line is ×" + N(XgSim.ScaleNeed, "0") + ".");
        }

        // ───────────── 装备 · 数据集 ─────────────

        void BuildGearCard()
        {
            var m = XgModuleRefs.Spawn("TrainGearModule", sideContent, ui);
            gearCard = m.Get("card"); gearCard.name = "Gear";
            // The tabs are as wide as their labels and packed to the left; a tab that is not open yet drops out.
            gearTab = m.Button("gearTab", ui, "", () => { learningView = false; booksView = false; dataView = false; Fx.Play(XgJuice.Sfx.Id.Click); RefreshSide(Track, Sim.Run(Track)); }, 12, "GearTab");
            learningTab = m.Button("learningTab", ui, T("它学会了吗", "Learning"), () => { learningView = true; booksView = false; RefreshSide(Track, Sim.Run(Track)); }, 12, "LearningTab");
            BuildBooksTab(m);
            foreach (var tab in new[] { gearTab, learningTab, booksTab }) { tab.label.enableAutoSizing = true; tab.label.fontSizeMin = 10; tab.label.fontSizeMax = 12; }
            learningPane = m.Get("learningPane");
            learningText = m.Text("learningText");
            var talk = m.Button("ask", ui, T("练完，去 YY 问问它 →", "Ask it on YY →"), () => { if (!XgYyTalk.Open()) view.ShowTab("home"); }, 14, "AskOnYY");
            talk.label.enableAutoSizing = true; talk.label.fontSizeMin = 11; talk.label.fontSizeMax = 14;
            gearCount = ui.Text(Rect("Count", gearCard, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-110, -28), new Vector2(-12, -6)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            gearCount.textWrappingMode = TextWrappingModes.NoWrap;
            gearCount.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            gearCount.gameObject.SetActive(false);

            gearPane = m.Get("gearPane");
            var grid = m.Get("gearGrid");
            for (int i = 0; i < GearTiles; i++)
            {
                int index = i;
                var t = XgModuleRefs.Spawn("TrainGearTile", grid, ui);
                var rt = (RectTransform)t.transform;
                rt.name = "Gear" + i;
                var frame = rt.GetComponent<XgFrameGraphic>();
                rt.GetComponent<Button>().onClick.AddListener(() => ClickGear(index));
                var tile = new Tile { rt = rt, frame = frame, name = t.Text("name"), big = t.Text("big"), state = t.Text("state") };
                UiTip.Add(rt, () => GearAt(index).tip);
                tiles.Add(tile);
            }

            // Data packs are bought in Technology; no hidden source/download UI is built.

        }

        /// <summary>One tile of the 装备 grid: what it is, its state, and what a click does.</summary>
        struct Gear
        {
            public string name, big, state, tip, nodeId;
            public bool owned, buyable, poor, structure, dim;
            public XgNode node;
        }

        Gear GearAt(int index)
        {
            var g = new Gear();
            var run = Sim.Run(Track);
            if (index == 0)
            {
                var a = XgCatalog.Arch(run.arch);
                g.structure = true; g.owned = true;
                g.name = T(a.name, a.nameEn); g.big = XgSim.ParamsText(XgSim.ParamsK(run)); g.state = T("结构 · 自动换", "structure · auto");
                g.tip = ArchTip(run.arch) + "\n\n" + T("结构是道具：买到更好的，它自己换上。点一下去道具页。", "Structures are items: buy a better one and it switches by itself. Click for the Items page.");
                return g;
            }
            string id; double factor = 0; bool scaleItem = false;
            if (index <= PassiveGear.Length) id = PassiveGear[index - 1];
            else { var item = XgSim.ScaleItems[index - 1 - PassiveGear.Length]; id = item.item; factor = item.factor; scaleItem = true; }
            var node = XgCatalog.Node(id);
            g.node = node; g.nodeId = id;
            g.name = scaleItem ? T(XgSim.ScaleItems[index - 1 - PassiveGear.Length].name, XgSim.ScaleItems[index - 1 - PassiveGear.Length].nameEn) : node != null ? Sim.NodeName(node) : id;
            g.owned = Sim.Has(id);
            var status = node == null ? XgSim.NodeStatus.Locked : Sim.Status(node, Host);
            string effect;
            if (scaleItem) { g.big = "×" + N(factor, "0.#"); effect = T("有效规模 ×" + N(factor, "0.#") + "（阶段 6 预训练用）", "effective scale ×" + N(factor, "0.#") + " (counts for stage-6 pre-training)"); }
            else
            {
                switch (id)
                {
                    case "cudnn": g.big = T("电费 −30%", "power −30%"); effect = T("所有训练快 30%，电费少 30%。", "All training 30% faster and 30% less electricity."); break;
                    case "cooler": g.big = T("烧卡 −50%", "burn-outs −50%"); effect = T("连着练太久时，显卡烧坏的概率减半。要有两张卡才会烧。", "Training non-stop burns a card out half as often. A card only burns out when you own two."); break;
                    default: g.big = T("少退步", "fewer drops"); effect = T("让「退步」少一些。", "Makes drops rarer."); break;
                }
                var small = g.big; g.big = "<size=12>" + small + "</size>";
            }
            g.dim = scaleItem && Sim.S.stage < 6;
            double cost = node != null ? Sim.NodeCost(node) : 0;
            bool seen = node != null && Sim.NodeVisible(node);
            if (g.owned) g.state = T("已装备", "on");
            else if (!seen) g.state = T("阶段 " + (node != null ? node.stage : 6), "stage " + (node != null ? node.stage : 6));
            else if (status == XgSim.NodeStatus.Buyable) { g.state = "¥" + Money(cost); g.buyable = true; }
            else if (status == XgSim.NodeStatus.TooExpensive) { g.state = "¥" + Money(cost); g.poor = true; }
            else g.state = (id == "transformer" || id == "warmup") ? T("剧情送", "from the story") : T("未解锁", "locked");
            string note = node != null ? Sim.NodeNote(node) : "";
            string how = g.owned ? T("已拥有：一直生效，不用点。", "Owned: always on, nothing to press.")
                : !seen ? T("阶段 " + (node != null ? node.stage : 6) + " 才能买。", "Available from stage " + (node != null ? node.stage : 6) + ".")
                : g.buyable ? T("点一下买：¥" + Money(cost), "Click to buy: ¥" + Money(cost))
                : g.poor ? T("还差 ¥" + Money(cost - Host.Money), "¥" + Money(cost - Host.Money) + " short")
                : (id == "transformer" || id == "warmup") ? T("剧情里会拿到。", "You get it in the story.") : Sim.Why(node, Host);
            g.tip = "<b>" + g.name + "</b>\n" + effect + (note.Length > 0 && note != effect ? "\n" + note : "") + "\n<size=12><color=#6F95A5>" + how + "</color></size>"
                + (scaleItem && Sim.S.stage < 6 ? "\n<size=12><color=#6F95A5>" + T("阶段 1–5 不靠倍率；到阶段 6 才算进有效规模。", "Stages 1–5 do not use the multipliers; they count in the effective scale from stage 6.") + "</color></size>" : "");
            return g;
        }

        void ClickGear(int index)
        {
            var g = GearAt(index);
            var tile = tiles[index];
            if (g.structure) { Fx.Play(XgJuice.Sfx.Id.Click); view.ShowTab("items"); return; }
            if (g.owned) { Fx.Knock(tile.rt, .04f); Fx.Play(XgJuice.Sfx.Id.Click, 1.2f, .3f); return; }
            if (g.buyable && g.node != null)
            {
                if (view.BuyNode(g.node, tile.rt))
                {
                    Fx.Knock(tile.rt, .12f);
                    Fx.Burst(Fx.At(tile.rt), 14, XgDark.Money, XgJuice.Shape.Spark, 220);
                    Fx.Float(Fx.At(tile.rt, new Vector2(0, 30)), g.name, XgDark.Money, 16, 36, .9f, 1.2f);
                    view.Refresh(true);
                }
                else { Fx.Knock(tile.rt, .05f, new Vector2(8, 0)); Fx.Play(XgJuice.Sfx.Id.Thud); }
                return;
            }
            Fx.Knock(tile.rt, .05f, new Vector2(8, 0));
            Fx.Play(XgJuice.Sfx.Id.Thud, 1, .4f);
            string why = g.poor ? T("钱不够", "Not enough money") : g.node != null && Sim.NodeVisible(g.node) ? Sim.Why(g.node, Host) : g.state;
            Fx.Float(Fx.At(tile.rt, new Vector2(0, 30)), why, XgDark.Bad, 14, 28, .9f, 1.1f);
        }

        void RefreshGear()
        {
            int owned = 0;
            for (int i = 0; i < tiles.Count; i++)
            {
                var g = GearAt(i);
                var t = tiles[i];
                if (g.owned) owned++;
                t.name.text = g.name; t.big.text = g.big; t.state.text = g.state;
                Color ink = g.owned ? XgDark.Ink : g.dim ? XgDark.Dim : XgDark.Ink;
                t.name.color = ink;
                t.big.color = g.owned && !g.dim ? XgDark.Money : g.owned ? XgDark.Muted : XgDark.Dim;
                t.state.color = g.owned ? XgDark.Good : g.buyable ? XgDark.Money : g.poor ? new Color32(150, 120, 70, 255) : XgDark.Dim;
                if (g.structure) t.big.color = XgDark.Params;
                if (g.owned) t.frame.Style(new Color32(15, 36, 32, 255), new Color32(46, 107, 88, 255));
                else if (g.buyable) t.frame.Style(new Color(.98f, .78f, .46f, .06f), XgDark.Money, true);
                else t.frame.Style(Color.clear, g.dim || !g.poor ? XgDark.Hairline : XgDark.Line, true);
                t.rt.GetComponent<Button>().interactable = true;
            }
            gearCount.text = owned + "/" + tiles.Count;
        }

        void RefreshSide(XgTrack track, XgRun run)
        {
            RefreshScale(track, run);
            bool gear=Sim.TrainPart("gear"), learning=Sim.TrainPart("learning"), books=Sim.TrainPart("books");
            if(!learning)learningView=false; if(!books)booksView=false;
            if(!gear&&!learningView&&!booksView){learningView=learning;booksView=!learning&&books;}
            bool other=learningView||booksView;
            gearTab.Set(T("装备", "Gear"), true, !other ? XgDark.Accent : XgDark.Button, !other ? Color.white : XgDark.Ink);
            learningTab.Set(T("它学会了吗", "Learning"), true, learningView ? XgDark.Accent : XgDark.Button, XgDark.Ink);
            booksTab.Set(T("书架", "Books"), true, booksView ? XgDark.Accent : XgDark.Button, booksView ? Color.white : XgDark.Ink);
            foreach (var tab in new[] { gearTab, learningTab, booksTab }) XgSlotWidth.Fit(tab, 56, 180, 22);
            learningPane.gameObject.SetActive(learningView);
            if(learningView)learningText.text=Sim.CheckupText(dataset:run.dataset)+BookQuizText();
            booksPane.gameObject.SetActive(booksView);
            gearPane.gameObject.SetActive(!other&&gear); gearCount.gameObject.SetActive(false);
            if(booksView)RefreshBooks();else if(gear&&!other)RefreshGear();
            if(Sim.TrainPart("ledger"))RefreshLedger();
            RefreshTrainingParts();
        }

        // ───────────── 今天的账 ─────────────

        void BuildLedgerCard()
        {
            var m = XgModuleRefs.Spawn("TrainLedgerModule", sideContent, ui);
            ledgerCard = m.Get("card"); ledgerCard.name = "Ledger";
            ledgerHead = m.Text("head"); ledgerHead.characterSpacing = 1;
            ledgerDate = m.Text("date");
            // The two buttons share a row above the bills: nothing is drawn under them.
            fundsBook = m.Button("accounts", ui, T("查看资金账本", "Accounts"), () =>
            {
                if (!Sim.LedgerOpen) return;
                var runtime = UnityEngine.Object.FindAnyObjectByType<LingGuangV05.Runtime.ChapterOneRuntime>();
                if (runtime != null) (runtime.GetComponent<LingGuangV05.Desktop.FundsPanel>() ?? runtime.gameObject.AddComponent<LingGuangV05.Desktop.FundsPanel>()).Open();
            }, 11, "OpenFundsBook");
            orders = m.Button("orders", ui, "", () => view.ShowTab("contracts"), 12, "Orders");
            foreach (var b in new[] { fundsBook, orders }) { b.label.enableAutoSizing = true; b.label.fontSizeMin = 9; b.label.fontSizeMax = b.label.fontSize; b.label.overflowMode = TextOverflowModes.Ellipsis; }
            for (int i = 0; i < 3; i++) { ledgerLabels[i] = m.Text("label" + i); ledgerValues[i] = m.Text("value" + i); }
            ledgerWarn = m.Text("warn");
            UiTip.Add(ledgerCard, () =>
            {
                var home = Host as XingGuangHost;
                string one = N(home != null ? home.TierOneKwh : 240, "0"), two = N(home != null ? home.TierTwoKwh : 400, "0");
                return T("训练每一轮都要交电费，按这个月的阶梯电价：一个月用电超过 " + one + " 度，超出的部分贵一成；超过 " + two + " 度，贵五成。房租到期后在 YY 向房东转账，可先付一部分。",
                    "Every training round pays electricity at this month's tiered rate: past " + one + " kWh a month the rest costs 10% more; past " + two + " kWh, 50% more. Pay rent to the landlord in YY when due; partial transfers are allowed.");
            });
        }

        void RefreshLedger()
        {
            fundsBook.label.text=T("查看资金账本","Accounts");
            ledgerHead.text = T("今天的账", "TODAY'S BILLS");
            if (!(Host is XingGuangHost home))
            {
                ledgerDate.text = ledgerWarn.text = "";
                for (int i = 0; i < 3; i++) ledgerLabels[i].text = ledgerValues[i].text = "";
                Part(ledgerWarn, false);
                return;
            }
            var day = home.Today;
            ledgerDate.text = day.Year > 1 ? T(day.Month + " 月 " + day.Day + " 日", day.ToString("d MMM", System.Globalization.CultureInfo.InvariantCulture)) : "";
            home.TodayBills(out double power, out _);
            double kwh = home.MonthKwh;
            int tier = home.PowerTier;
            ledgerLabels[0].text = T("训练电费（今天）", "Training power (today)");
            ledgerLabels[1].text = T("房租（日均预算，非自动扣款）", "Rent (daily budget; paid manually)");
            ledgerLabels[2].text = T("本月用电 " + N(kwh, "0") + " 度 · 第 " + tier + " 档", N(kwh, "0") + " kWh this month · tier " + tier);
            string tierNote = tier >= 3 ? Hex(XgDark.Bad) + T("更贵 ×1.5", "dearer ×1.5") : tier == 2 ? Hex(XgDark.Money) + T("贵了 ×1.1", "dearer ×1.1") : Hex(XgDark.Muted) + "—";
            ledgerValues[0].text = Hex(XgDark.Bad) + "−¥" + N(power, "0.0") + "</color>";
            ledgerValues[1].text = Hex(XgDark.Bad) + "¥" + N(home.EconomyActive ? home.RentToday : 0, "0") + T("/天", "/day") + "</color>";
            ledgerValues[2].text = tierNote + "</color>";
            bool poor = Host.Money < Math.Max(50, home.EconomyActive ? home.RentToday : 0);
            ledgerWarn.text = poor ? T("余额偏低。完成已开放的工作补充资金；训练照常收电费。", "Low funds. Complete available work to earn money; training power is still billed.") : "";
            Part(ledgerWarn, poor);
        }
    }
}
