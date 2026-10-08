using System.Text;
using LingGuangV05.Core;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 收藏 › 能力 (参数量与数据量主线 §10; the redesign's 能力 page): the six abilities as six rows. A learned one is rimmed
    /// green; the next one shows its two bars; every row shows its thresholds, struck through where an owned item
    /// lowered them, the items that would, and what it opens. It used to be the 能力表 tab of the 大脑 page.
    /// </summary>
    public sealed class XgAbilitiesPage : XgPage
    {
        sealed class Row
        {
            public Image rim;
            public TMP_Text index, name, status, parameters, samples, items;
            public RectTransform paramFill, dataFill, bars;
        }

        readonly Row[] rows = new Row[XgSim.AbilityCount];
        TMP_Text header, scale, notes;

        public override void Build(RectTransform area)
        {
            root = Rect("abilities", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            header = ui.Text(Strip("Header", root, 0, 22, 2, 260), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            header.textWrappingMode = TextWrappingModes.NoWrap; header.characterSpacing = 2;
            scale = ui.Text(Rect("Scale", root, new Vector2(1, 1), Vector2.one, new Vector2(-260, -22), new Vector2(-2, 0)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            scale.textWrappingMode = TextWrappingModes.NoWrap;
            const float Top = 30, Height = 84, Gap = 8;
            for (int i = 0; i < XgSim.AbilityCount; i++)
            {
                var card = ui.Card(root, "Ability" + (i + 1), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -Top - (i + 1) * Height - i * Gap), new Vector2(0, -Top - i * (Height + Gap)));
                var r = new Row { rim = card.GetComponent<Image>() };
                r.index = ui.Text(Rect("Index", card, Vector2.zero, new Vector2(0, 1), new Vector2(12, 0), new Vector2(44, 0)), "", 14, XgDark.Dim, TextAlignmentOptions.MidlineLeft);
                r.name = ui.Text(Rect("Name", card, new Vector2(0, .5f), new Vector2(0, 1), new Vector2(46, 0), new Vector2(206, -10)), "", 16, XgDark.Ink, TextAlignmentOptions.BottomLeft);
                r.name.textWrappingMode = TextWrappingModes.NoWrap; r.name.overflowMode = TextOverflowModes.Ellipsis;
                r.status = ui.Text(Rect("Status", card, Vector2.zero, new Vector2(0, .5f), new Vector2(46, 10), new Vector2(206, -2)), "", 12, XgDark.Muted, TextAlignmentOptions.TopLeft);
                r.status.textWrappingMode = TextWrappingModes.NoWrap;
                r.parameters = Column(card, "Params", 214, 420);
                r.samples = Column(card, "Samples", 430, 636);
                r.bars = Rect("Bars", card, new Vector2(0, 0), new Vector2(0, 0), new Vector2(214, 10), new Vector2(636, 16));
                r.paramFill = Bar(Rect("P", r.bars, Vector2.zero, new Vector2(.49f, 1), Vector2.zero, Vector2.zero), "Fill", XgDark.Track, XgDark.Params);
                r.dataFill = Bar(Rect("D", r.bars, new Vector2(.51f, 0), Vector2.one, Vector2.zero, Vector2.zero), "Fill", XgDark.Track, XgDark.Data);
                r.items = ui.Text(Rect("Items", card, Vector2.zero, Vector2.one, new Vector2(648, 8), new Vector2(-12, -8)), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
                r.items.enableAutoSizing = true; r.items.fontSizeMin = 12; r.items.fontSizeMax = 13;
                rows[i] = r;
            }
            notes = ui.Text(Rect("Notes", root, Vector2.zero, new Vector2(1, 0), new Vector2(2, 0), new Vector2(-2, 52)), "", 12, XgDark.Muted, TextAlignmentOptions.BottomLeft);
            notes.enableAutoSizing = true; notes.fontSizeMin = 12; notes.fontSizeMax = 13;
        }

        TMP_Text Column(RectTransform card, string name, float x0, float x1)
        {
            var t = ui.Text(Rect(name, card, Vector2.zero, new Vector2(0, 1), new Vector2(x0, 20), new Vector2(x1, -8)), "", 14, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        public override void Refresh()
        {
            if (header == null || Sim == null) return;
            int next = Sim.NextAbility;
            double p = Sim.TrainedParamsK, d = Sim.TrainedSamples;
            header.text = T("能力 · 参数量 × 样本，到门槛就有", "ABILITIES · parameters × samples, past the line it comes");
            scale.text = T("规模定律 Kaplan 2020 · 能力 ", "Scaling laws, Kaplan 2020 · abilities ") + Sim.AbilitiesCount + "/6";
            for (int i = 0; i < XgSim.AbilityCount; i++)
            {
                int ability = i + 1;
                var r = rows[i];
                bool learned = Sim.HasAbility(ability), isNext = ability == next;
                r.rim.color = learned ? XgDark.Good : isNext ? XgDark.Params : XgDark.Line;
                r.index.text = "0" + ability;
                r.name.text = XgSim.AbilityName(ability, Sim.English);
                r.name.color = learned ? XgDark.Good : XgDark.Ink;
                r.status.text = learned ? "<color=#5DCAA5>" + T("已学会", "learned") + "</color>"
                    : isNext ? "<color=#A99BF2>" + T("下一项 · 还没学会", "next · not yet learned") + "</color>" : T("还没学会", "not yet learned");
                if (ability == 1)
                {
                    r.parameters.text = T("参数 —", "Params —"); r.samples.text = T("样本 —", "Samples —");
                }
                else
                {
                    double baseP = XgSim.AbilityParamsK[ability], baseD = XgSim.AbilitySamples[ability];
                    double nowP = Sim.ParamsThreshold(ability), nowD = Sim.SamplesThreshold(ability);
                    r.parameters.text = T("参数 ", "Params ") + Threshold(XgSim.ParamsText(baseP), XgSim.ParamsText(nowP), nowP < baseP - 1e-9);
                    r.samples.text = T("样本 ", "Samples ") + Threshold(XgSim.SamplesText(baseD), XgSim.SamplesText(nowD), nowD < baseD - 1e-9);
                }
                if (r.bars.gameObject.activeSelf != isNext) r.bars.gameObject.SetActive(isNext);
                if (isNext) { SetBar(r.paramFill, (float)Sim.ParamsProgress(ability)); SetBar(r.dataFill, (float)Sim.SamplesProgress(ability)); }
                r.items.text = ItemsText(ability);
            }
            notes.text = NotesText(next, p, d);
        }

        /// <summary>A threshold with its base struck through when an owned item lowered it.</summary>
        static string Threshold(string baseText, string nowText, bool lowered) => lowered ? "<b>" + nowText + "</b>  <size=12><color=#6F95A5><s>" + T("原 ", "was ") + baseText + "</s></color></size>" : "<b>" + nowText + "</b>";

        string ItemsText(int ability)
        {
            var sb = new StringBuilder();
            foreach (var discount in XgSim.AbilityDiscounts)
            {
                if (discount.ability != ability) continue;
                bool owned = Sim.DiscountOwned(discount);
                sb.Append(owned ? "<color=#5DCAA5>✓ " : "<color=#6F95A5>○ ").Append(ItemName(discount));
                if (discount.paramsFactor < 1) sb.Append(T(" 参数 ×", " params ×")).Append(N(discount.paramsFactor, "0.0#"));
                if (discount.samplesFactor < 1) sb.Append(T(" 样本 ×", " samples ×")).Append(N(discount.samplesFactor, "0.0#"));
                sb.Append("</color>   ");
            }
            if (ability == 1) sb.Append("<color=#6F95A5>").Append(T("起始即有", "There from the start")).Append("</color>");
            sb.Append("\n<color=#58798A>").Append(T(XgSim.AbilityUnlocks[ability], XgSim.AbilityUnlocksEn[ability])).Append("</color>");
            return sb.ToString();
        }

        string ItemName(XgAbilityDiscount discount)
        {
            if (discount.item.Length > 0) return Sim.NodeName(XgCatalog.Node(discount.item));
            var names = new StringBuilder();
            foreach (var id in discount.itemsAny) names.Append(names.Length > 0 ? T(" 或 ", " or ") : "").Append(Sim.NodeName(XgCatalog.Node(id)));
            return names.ToString();
        }

        string NotesText(int next, double p, double d)
        {
            var sb = new StringBuilder();
            sb.Append("<color=#A99BF2>■</color> ").Append(T("参数量：评估到 C 级以上的模型里最大的那个，现在 ", "Parameters: the biggest model assessed at grade C or better, now ")).Append(XgSim.ParamsText(p))
              .Append(" <color=#58798A>(").Append(XgSim.ParamScale(p, Sim.English)).Append(")</color>");
            sb.Append("    <color=#5DA8E8>■</color> ").Append(T("样本：所有数据集的有效样本，现在 ", "Samples: every dataset's effective samples, now ")).Append(XgSim.SamplesText(d));
            if (Sim.ReadWordsTotal >= 1) sb.Append(" <color=#58798A>(").Append(Sim.ReadShort()).Append(")</color>");
            sb.Append("\n").Append(T("结构是道具：买了能降门槛，不买也能到，只是慢一些。", "Structures are items: they lower the line; without them it still comes, only later."));
            if (next > 0)
            {
                double shortP = Sim.ParamsThreshold(next) - p, shortD = Sim.SamplesThreshold(next) - d;
                sb.Append("  ").Append(T("「" + XgSim.AbilityName(next, false) + "」", "'" + XgSim.AbilityName(next, true) + "'"));
                if (shortP > 0) sb.Append(T(" 还差参数 ", " needs parameters ")).Append(XgSim.ParamsText(shortP));
                if (shortD > 0) sb.Append(T(" 还差样本 ", " needs samples ")).Append(XgSim.SamplesText(shortD));
                if (shortP <= 0 && shortD <= 0) sb.Append(T(" 两样都够了。", ": both are enough."));
            }
            return sb.ToString();
        }
    }
}
