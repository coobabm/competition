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
        const float ScaleHeight = 184, LedgerHeight = 106, SideGap = 10;

        // 有效规模
        RectTransform scaleCard, nextArea, formulaArea, logBack, logFill, logMark, pFill, dFill;
        TMP_Text scaleTitle, scaleTag, scaleBig, scaleVerdict, logLabel, pLabel, pValue, dLabel, dValue;
        readonly List<Chip> formulaChips = new List<Chip>();
        const int FormulaChipCount = 2 + 7;

        // 装备 / 数据集
        RectTransform gearCard, gearPane, dataPane;
        XgBtn gearTab, dataTab;
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
        TMP_Text ledgerHead, ledgerDate, ledgerLeft, ledgerRight, ledgerWarn;

        void BuildSide()
        {
            BuildScaleCard();
            gearCard = ui.Card(right, "Gear", Vector2.zero, Vector2.one, new Vector2(0, LedgerHeight + SideGap), new Vector2(0, -ScaleHeight - SideGap));
            BuildGearCard();
            BuildLedgerCard();
        }

        // ───────────── 有效规模 ─────────────

        void BuildScaleCard()
        {
            scaleCard = ui.Card(right, "Scale", new Vector2(0, 1), Vector2.one, new Vector2(0, -ScaleHeight), Vector2.zero);
            scaleTitle = ui.Text(Strip("Title", scaleCard, 8, 18, 12, 170), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            scaleTitle.textWrappingMode = TextWrappingModes.NoWrap; scaleTitle.characterSpacing = 1;
            scaleTag = ui.Text(Rect("Tag", scaleCard, new Vector2(0, 1), Vector2.one, new Vector2(150, -26), new Vector2(-12, -8)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            scaleTag.textWrappingMode = TextWrappingModes.NoWrap;
            scaleBig = ui.Text(Strip("Big", scaleCard, 26, 32, 12, 12), "", 24, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            scaleBig.textWrappingMode = TextWrappingModes.NoWrap;

            // Stage 6: the factors as chips (owned solid, missing dashed), then a log-scale bar with the ×2000 mark.
            formulaArea = Rect("Formula", scaleCard, new Vector2(0, 1), Vector2.one, new Vector2(12, -124), new Vector2(-12, -60));
            for (int i = 0; i < FormulaChipCount; i++) formulaChips.Add(MakeChip(formulaArea, "Factor" + i, 11));
            logBack = Rect("LogBar", scaleCard, new Vector2(0, 1), Vector2.one, new Vector2(12, -138), new Vector2(-12, -126));
            Panel(logBack, XgDark.Track).raycastTarget = false;
            logFill = Rect("Fill", logBack, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero);
            Panel(logFill, XgDark.Params).raycastTarget = false;
            logMark = Rect("Mark", logBack, new Vector2(0, 0), new Vector2(0, 1), new Vector2(-1, -3), new Vector2(1, 3));
            Panel(logMark, Color.white).raycastTarget = false;
            logLabel = ui.Text(Rect("MarkLabel", scaleCard, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-30, -151), new Vector2(30, -138)), "", 10, XgDark.Muted, TextAlignmentOptions.Top);
            logLabel.textWrappingMode = TextWrappingModes.NoWrap;

            // Stages 1–5: the next ability's two bars.
            nextArea = Rect("Next", scaleCard, new Vector2(0, 1), Vector2.one, new Vector2(12, -138), new Vector2(-12, -34));
            pFill = BarRow(nextArea, 0, XgDark.Params, out pLabel, out pValue);
            dFill = BarRow(nextArea, 52, XgDark.Data, out dLabel, out dValue);

            scaleVerdict = ui.Text(Rect("Verdict", scaleCard, Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-12, -156)), "", 11, XgDark.Muted, TextAlignmentOptions.TopLeft);
            scaleVerdict.enableAutoSizing = true; scaleVerdict.fontSizeMin = 9; scaleVerdict.fontSizeMax = 11;
            UiTip.Add(scaleCard, () => ScaleTip());
        }

        /// <summary>A label, a value and a bar under them, <paramref name="y"/> from the top of the parent.</summary>
        RectTransform BarRow(RectTransform parent, float y, Color fill, out TMP_Text label, out TMP_Text value)
        {
            label = ui.Text(Rect("Label", parent, new Vector2(0, 1), new Vector2(.4f, 1), new Vector2(0, -y - 20), new Vector2(0, -y)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            value = ui.Text(Rect("Value", parent, new Vector2(.4f, 1), Vector2.one, new Vector2(0, -y - 20), new Vector2(0, -y)), "", 13, XgDark.Ink, TextAlignmentOptions.MidlineRight);
            value.textWrappingMode = TextWrappingModes.NoWrap;
            var track = Rect("Track", parent, new Vector2(0, 1), Vector2.one, new Vector2(0, -y - 32), new Vector2(0, -y - 22));
            return Bar(track, "Fill", XgDark.Track, fill);
        }

        static float LogShare(double v) => (float)Math.Min(1, Math.Log10(Math.Max(1, v)) / Math.Log10(5000));

        void RefreshScale(XgTrack track, XgRun run)
        {
            bool six = Sim.S.stage >= 6 || Sim.NextAbility == 0;
            formulaArea.gameObject.SetActive(six); logBack.gameObject.SetActive(six); logLabel.gameObject.SetActive(six);
            nextArea.gameObject.SetActive(!six);
            scaleBig.gameObject.SetActive(six);
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
                float x = 0, y = 0, width = formulaArea.rect.width > 10 ? formulaArea.rect.width : 386;
                for (int i = 0; i < formulaChips.Count; i++)
                {
                    var c = formulaChips[i];
                    c.text.text = chips[i].text; c.text.color = chips[i].ink;
                    bool ratio = i < 2;
                    c.frame.Style(ratio ? new Color(chips[i].ink.r, chips[i].ink.g, chips[i].ink.b, .08f) : chips[i].own ? new Color(.98f, .78f, .46f, .1f) : Color.clear,
                        ratio ? new Color(chips[i].ink.r, chips[i].ink.g, chips[i].ink.b, .55f) : chips[i].own ? new Color32(110, 90, 46, 255) : XgDark.Line, !ratio && !chips[i].own);
                    float w = c.text.GetPreferredValues(c.text.text).x + 12;
                    if (x + w > width && x > 0) { x = 0; y += 22; }
                    PlaceChip(c, x, y, 20);
                    x += w + 4;
                }
                logFill.anchorMax = new Vector2(LogShare(eff), 1);
                var fillImage = logFill.GetComponent<Image>(); if (fillImage != null) fillImage.color = ready ? XgDark.Good : XgDark.Params;
                float mark = LogShare(need);
                logMark.anchorMin = new Vector2(mark, 0); logMark.anchorMax = new Vector2(mark, 1);
                float barWidth = scaleCard.rect.width - 24;
                logLabel.text = "×" + N(need, "0");
                logLabel.rectTransform.anchoredPosition = new Vector2(12 + barWidth * mark, -145);
                scaleVerdict.text = ScaleVerdict();
                scaleVerdict.rectTransform.offsetMax = new Vector2(-12, -154);
            }
            else
            {
                int next = Sim.NextAbility;
                double paramsNeed = Sim.ParamsThreshold(next), samplesNeed = Sim.SamplesThreshold(next), size = XgSim.ParamsK(run);
                scaleTitle.text = T("下一项能力 · 能力 " + next + "「" + XgSim.AbilityName(next, false) + "」", "NEXT ABILITY · " + next + " '" + XgSim.AbilityName(next, true) + "'");
                scaleTag.text = "";
                pLabel.text = T("参数量", "Parameters"); pValue.text = XgSim.ParamsText(Sim.TrainedParamsK) + " / " + XgSim.ParamsText(paramsNeed);
                dLabel.text = T("样本", "Samples"); dValue.text = XgSim.SamplesText(Sim.TrainedSamples) + " / " + XgSim.SamplesText(samplesNeed);
                SetBar(pFill, (float)Sim.ParamsProgress(next)); SetBar(dFill, (float)Sim.SamplesProgress(next));
                string line;
                if (Sim.TrainedParamsK + 1e-9 < paramsNeed)
                    line = size + 1e-9 >= paramsNeed ? Hex(XgDark.Muted) + T("这个模型够大：练到 C 级就算数。", "This model is big enough: it counts once assessed at grade C.") + "</color>"
                        : Hex(XgDark.Gold) + T("这个模型 " + XgSim.ParamsText(size) + "，还不够大：去科技加宽、加深，或在道具买参数更多的结构。", "This model has " + XgSim.ParamsText(size) + ", not enough yet: widen or deepen in the tech tree, or buy a bigger structure on the Items page.") + "</color>";
                else line = Hex(XgDark.Muted) + T("参数够了，样本到门槛，它就学会这一项。", "The parameters are there; once the samples reach the line it learns this ability.") + "</color>";
                scaleVerdict.rectTransform.offsetMax = new Vector2(-12, -122);
                scaleVerdict.text = line + "\n" + Hex(XgDark.Dim) + T("阶段 1–5 靠参数和样本；下面灰色的格子是阶段 6 才用得上的倍率。", "Stages 1–5 run on parameters and samples; the greyed tiles below are multipliers that only count at stage 6.") + "</color>";
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
                return (Sim.ReadWordsTotal >= 1 ? Sim.ReadLine() + "\n" : "") + T("下一项能力要两样都到门槛：练到 C 级的最大模型的参数量，和所有数据集的有效样本（标错的打折）。门槛已按买到的道具打折。",
                    "The next ability needs both bars at the line: the parameters of the biggest model assessed at grade C, and the effective samples of every dataset (wrong labels count against). The lines already include the items you own.");
            return T("有效规模 = 参数 × 数据 × 买到的倍率。参数和数据各算到第六项能力门槛的 " + N(XgSim.ScaleBarCap, "0.#") + " 倍为止；倍率是道具带来的，乘在一起。预训练要 ×" + N(XgSim.ScaleNeed, "0") + "：三件大的（Transformer ×10、机房 ×8、注意力 ×3）一样都不能少，小的最多缺一件。\n条是对数刻度，白线是 ×" + N(XgSim.ScaleNeed, "0") + "。",
                "Effective scale = parameters × data × the multipliers you own. Parameters and data each count up to " + N(XgSim.ScaleBarCap, "0.#") + " times the sixth ability's bar; the multipliers come from items and multiply. Pre-training needs ×" + N(XgSim.ScaleNeed, "0") + ": none of the three big ones (Transformer ×10, server room ×8, attention ×3) can be missing, and at most one small one.\nThe bar is a log scale; the white line is ×" + N(XgSim.ScaleNeed, "0") + ".");
        }

        // ───────────── 装备 · 数据集 ─────────────

        void BuildGearCard()
        {
            gearTab = ui.Button(gearCard, "", () => { dataView = false; Fx.Play(XgJuice.Sfx.Id.Click); RefreshSide(Track, Sim.Run(Track)); }, 12);
            gearTab.rt.gameObject.name = "GearTab";
            PlaceTopLeft(gearTab, 8, 6, 84, 22);
            dataTab = ui.Button(gearCard, "", () => { dataView = true; Fx.Play(XgJuice.Sfx.Id.Click); RefreshSide(Track, Sim.Run(Track)); }, 12);
            dataTab.rt.gameObject.name = "DataTab";
            PlaceTopLeft(dataTab, 98, 6, 96, 22);
            gearCount = ui.Text(Rect("Count", gearCard, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-110, -28), new Vector2(-12, -6)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            gearCount.textWrappingMode = TextWrappingModes.NoWrap;

            gearPane = Rect("GearPane", gearCard, Vector2.zero, Vector2.one, new Vector2(8, 6), new Vector2(-8, -34));
            const float tileWidth = 94, tileHeight = 56;
            for (int i = 0; i < GearTiles; i++)
            {
                int index = i;
                var rt = Rect("Gear" + i, gearPane, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
                rt.pivot = new Vector2(0, 1);
                rt.sizeDelta = new Vector2(tileWidth, tileHeight);
                rt.anchoredPosition = new Vector2(i % 4 * (tileWidth + 6), -(i / 4) * (tileHeight + 6));
                var frame = rt.gameObject.AddComponent<XgFrameGraphic>();
                var button = rt.gameObject.AddComponent<Button>();
                button.targetGraphic = frame; button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => ClickGear(index));
                var tile = new Tile { rt = rt, frame = frame };
                tile.name = ui.Text(Rect("Name", rt, new Vector2(0, 1), Vector2.one, new Vector2(6, -19), new Vector2(-4, -3)), "", 11, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
                tile.name.textWrappingMode = TextWrappingModes.NoWrap; tile.name.overflowMode = TextOverflowModes.Ellipsis;
                tile.big = ui.Text(Rect("Big", rt, Vector2.zero, Vector2.one, new Vector2(6, 14), new Vector2(-2, -17)), "", 15, XgDark.Money, TextAlignmentOptions.MidlineLeft);
                tile.big.textWrappingMode = TextWrappingModes.NoWrap;
                tile.state = ui.Text(Rect("State", rt, Vector2.zero, new Vector2(1, 0), new Vector2(6, 0), new Vector2(-2, 16)), "", 10, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
                tile.state.textWrappingMode = TextWrappingModes.NoWrap;
                UiTip.Add(rt, () => GearAt(index).tip);
                tiles.Add(tile);
            }

            // The datasets, the packs, the noise: what the old model card kept.
            dataPane = Rect("DataPane", gearCard, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -32));
            noiseText = ui.Text(Strip("DatasetNoise", dataPane, 2, 22, 12, 170), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            noiseText.enableAutoSizing = true; noiseText.fontSizeMin = 10; noiseText.fontSizeMax = 13;
            noiseText.textWrappingMode = TextWrappingModes.NoWrap;
            noiseHint = ui.Text(Strip("NoiseCleaningHint", dataPane, 28, 32, 12, 180), "", 11, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            noiseHint.enableAutoSizing = true; noiseHint.fontSizeMin = 9; noiseHint.fontSizeMax = 11;
            noiseHint.textWrappingMode = TextWrappingModes.NoWrap;
            cleanNoise = ui.Button(dataPane, "", CleanCurrentNoise, 12);
            cleanNoise.rt.gameObject.name = "CleanDatasetNoise";
            PlaceTopRight(cleanNoise, 172, 28, 160, 32);
            BuildDataEconomy(dataPane);
            dataTitle = ui.Text(Strip("DataTitle", dataPane, 64, 22, 12, 12), "", 15, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            dataTitle.fontStyle = FontStyles.Bold;
            NodeLink(dataTitle, () => Sim.Run(Track).dataset + ".pack");
            var scroll = Rect("Datasets", dataPane, Vector2.zero, Vector2.one, new Vector2(12, 58), new Vector2(-12, -88));
            var scroller = scroll.gameObject.AddComponent<ScrollRect>();
            dataViewport = Rect("Viewport", scroll, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(dataViewport, Color.clear); dataViewport.gameObject.AddComponent<RectMask2D>();
            dataBox = Rect("Content", dataViewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            dataBox.pivot = new Vector2(.5f, 1); dataBox.sizeDelta = Vector2.zero;
            scroller.viewport = dataViewport; scroller.content = dataBox; scroller.horizontal = false; scroller.vertical = true; scroller.scrollSensitivity = 24;
            scroller.movementType = ScrollRect.MovementType.Clamped;
            gpuTarget = Rect("GpuInstallTarget", dataPane, Vector2.zero, new Vector2(1, 0), new Vector2(12, 6), new Vector2(-12, 52));
            Panel(gpuTarget, XgDark.AccentSoft);
            gpuGlow = Glow(gpuTarget, XgDark.Gold, 4);
            gpuLabel = ui.Text(Rect("GpuLabel", gpuTarget, Vector2.zero, Vector2.one, new Vector2(12, 3), new Vector2(-12, -3)), "", 13, XgDark.Accent, TextAlignmentOptions.MidlineLeft);
            gpuLabel.enableAutoSizing = true; gpuLabel.fontSizeMin = 9; gpuLabel.fontSizeMax = 13;
            gpuTarget.gameObject.AddComponent<XgGpuInstallDrop>().Page = this;
            UiTip.Add(noiseText, () => XgDataUi.NoiseTip(Sim, Sim.Run(Track).dataset));
            UiTip.Add(cleanNoise.rt, () => T("花钱和 GPU 时间把已知的脏标注扔掉（需要科技里的「标注抽检」）。更省事的办法：道具页的清洗脚本，一次清 50 条。",
                "Spend money and GPU time to discard the known dirty labels (needs the 'label audit' tech). A cheaper way: the cleaning script on the Items page removes 50 at once."));
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
            gearTab.Set(T("装备", "Gear"), true, !dataView ? XgDark.Accent : XgDark.Button, !dataView ? Color.white : XgDark.Ink);
            dataTab.Set(T("数据集", "Datasets"), true, dataView ? XgDark.Accent : XgDark.Button, dataView ? Color.white : XgDark.Ink);
            gearPane.gameObject.SetActive(!dataView);
            dataPane.gameObject.SetActive(dataView);
            gearCount.gameObject.SetActive(!dataView);
            if (dataView) RefreshDataPane(track, run); else RefreshGear();
            RefreshLedger();
        }

        // ───────────── 今天的账 ─────────────

        void BuildLedgerCard()
        {
            ledgerCard = ui.Card(right, "Ledger", Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, LedgerHeight));
            ledgerHead = ui.Text(Strip("Head", ledgerCard, 6, 18, 12, 140), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            ledgerHead.characterSpacing = 1; ledgerHead.textWrappingMode = TextWrappingModes.NoWrap;
            ledgerDate = ui.Text(Rect("Date", ledgerCard, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-140, -24), new Vector2(-12, -6)), "", 12, XgDark.Ink, TextAlignmentOptions.MidlineRight);
            ledgerDate.textWrappingMode = TextWrappingModes.NoWrap;
            ledgerLeft = ui.Text(Rect("Left", ledgerCard, Vector2.zero, Vector2.one, new Vector2(12, 34), new Vector2(-150, -26)), "", 12, XgDark.Muted, TextAlignmentOptions.TopLeft);
            ledgerLeft.textWrappingMode = TextWrappingModes.NoWrap; ledgerLeft.lineSpacing = 0;
            ledgerRight = ui.Text(Rect("Right", ledgerCard, Vector2.zero, Vector2.one, new Vector2(-150, 34), new Vector2(-12, -26)), "", 12, XgDark.Ink, TextAlignmentOptions.TopRight);
            ledgerRight.textWrappingMode = TextWrappingModes.NoWrap; ledgerRight.lineSpacing = 0;
            ledgerWarn = ui.Text(Rect("Warn", ledgerCard, Vector2.zero, new Vector2(1, 0), new Vector2(12, 4), new Vector2(-12, 34)), "", 11, XgDark.Hot, TextAlignmentOptions.TopLeft);
            ledgerWarn.enableAutoSizing = true; ledgerWarn.fontSizeMin = 9; ledgerWarn.fontSizeMax = 11;
            UiTip.Add(ledgerCard, () =>
            {
                var home = Host as XingGuangHost;
                string one = N(home != null ? home.TierOneKwh : 240, "0"), two = N(home != null ? home.TierTwoKwh : 400, "0");
                return T("训练每一轮都要交电费，按这个月的阶梯电价：一个月用电超过 " + one + " 度，超出的部分贵一成；超过 " + two + " 度，贵五成。房租每过一个日历日扣一次，10 月起涨到 ¥350。",
                    "Every training round pays electricity at this month's tiered rate: past " + one + " kWh a month the rest costs 10% more; past " + two + " kWh, 50% more. Rent is paid once per calendar day, rising to ¥350 from October.");
            });
        }

        void RefreshLedger()
        {
            ledgerHead.text = T("今天的账", "TODAY'S BILLS");
            if (!(Host is XingGuangHost home)) { ledgerDate.text = ""; ledgerLeft.text = ledgerRight.text = ledgerWarn.text = ""; return; }
            var day = home.Today;
            ledgerDate.text = day.Year > 1 ? T(day.Month + " 月 " + day.Day + " 日", day.ToString("d MMM", System.Globalization.CultureInfo.InvariantCulture)) : "";
            if (!home.EconomyActive)
            {
                ledgerLeft.text = Hex(XgDark.Muted) + T("学会「一个词」以前不收房租和电费。", "No rent or electricity until it learns its third ability (one word).");
                ledgerRight.text = ""; ledgerWarn.text = "";
                return;
            }
            home.TodayBills(out double power, out _);
            double kwh = home.MonthKwh;
            int tier = home.PowerTier;
            ledgerLeft.text = T("训练电费（今天）", "Training power (today)") + "\n" + T("房租（日历走一天扣一次）", "Rent (once per calendar day)") + "\n" + T("本月用电 " + N(kwh, "0") + " 度 · 第 " + tier + " 档", N(kwh, "0") + " kWh this month · tier " + tier);
            string tierNote = tier >= 3 ? Hex(XgDark.Bad) + T("更贵 ×1.5", "dearer ×1.5") : tier == 2 ? Hex(XgDark.Money) + T("贵了 ×1.1", "dearer ×1.1") : Hex(XgDark.Muted) + "—";
            ledgerRight.text = Hex(XgDark.Bad) + "−¥" + N(power, "0.0") + "</color>\n" + Hex(XgDark.Bad) + "¥" + N(home.RentToday, "0") + T("/天", "/day") + "</color>\n" + tierNote + "</color>";
            bool poor = Host.Money < home.RentToday;
            ledgerWarn.text = poor ? T("⚠ 余额不够交下一天房租。去摆渡众包标注挣点，或者少练几轮。", "⚠ Not enough for the next day's rent. Earn some on the labelling desk in Bodu Crowd, or train fewer rounds.") : "";
        }
    }
}
