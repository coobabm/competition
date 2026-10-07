using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The training page's pack switches: under the dataset list, one row per owned pack of the current dataset
    /// (public / story data, junk pack). Clicking a row switches the pack in or out of training (XgSim.DataSources.cs);
    /// hand labels, crowd rows and user logs always train and are listed above the switches.
    /// </summary>
    public sealed partial class XgTrainPage
    {
        const float SourceRow = 32, SourceTitle = 40;
        TMP_Text sourceTitle;
        readonly List<XgBtn> sourceBtns = new List<XgBtn>();
        readonly List<string> sourceIds = new List<string>();
        string shownSourceKey = "";

        /// <summary>Owned packs of the dataset that can be switched; story data shows as the public pack it stands for.</summary>
        List<XgDataOffer> SwitchablePacks(string dataset) =>
            Sim.OffersFor(dataset).FindAll(o => o.owned && XgSim.PackSwitchable(o) && o.source != XgDataSource.Story);

        /// <summary>Lays the switches out below <paramref name="datasetRows"/> dataset rows and sizes the scroll content.</summary>
        void RefreshSources(XgRun run, int datasetRows)
        {
            float top = datasetRows * 36 + 6;
            var packs = SwitchablePacks(run.dataset);
            string key = run.dataset + ":" + string.Join(",", packs.ConvertAll(o => o.id)) + ":" + top;
            if (key != shownSourceKey)
            {
                shownSourceKey = key;
                foreach (var b in sourceBtns) Object.Destroy(b.rt.gameObject);
                sourceBtns.Clear(); sourceIds.Clear();
                if (sourceTitle == null)
                {
                    sourceTitle = ui.Text(Strip("TrainingSources", dataBox, top, SourceTitle, 2, 2), "", 12, XgDark.Muted, TextAlignmentOptions.TopLeft);
                    sourceTitle.enableAutoSizing = true; sourceTitle.fontSizeMin = 9; sourceTitle.fontSizeMax = 12;
                }
                var rt = sourceTitle.rectTransform;
                rt.offsetMin = new Vector2(rt.offsetMin.x, -top - SourceTitle); rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
                for (int i = 0; i < packs.Count; i++)
                {
                    string id = packs[i].id;
                    var b = ui.Button(dataBox, "", () => TogglePack(id), 12);
                    b.rt.gameObject.name = "PackSwitch";
                    b.label.alignment = TextAlignmentOptions.MidlineLeft;
                    b.label.enableAutoSizing = true; b.label.fontSizeMin = 9; b.label.fontSizeMax = 12;
                    b.rt.anchorMin = new Vector2(0, 1); b.rt.anchorMax = new Vector2(1, 1); b.rt.pivot = new Vector2(.5f, 1);
                    b.rt.offsetMin = new Vector2(12, -top - SourceTitle - i * SourceRow - SourceRow + 2); b.rt.offsetMax = new Vector2(0, -top - SourceTitle - i * SourceRow);
                    UiTip.Add(b.rt, () => PackSwitchTip(id));
                    sourceBtns.Add(b); sourceIds.Add(id);
                }
            }

            var d = XgCatalog.Dataset(run.dataset);
            string name = d != null ? T(d.name, d.nameEn) : run.dataset;
            sourceTitle.text = "<b>" + T("「" + name + "」用哪些数据训练", "What " + name + " trains on") + "</b>"
                + (packs.Count > 0 ? Lang.T("（点数据包开关）") : "") + "\n" + XgDataUi.AlwaysOnLine(Sim, run.dataset);
            for (int i = 0; i < sourceBtns.Count; i++)
            {
                var o = Sim.Offer(sourceIds[i]);
                if (o == null) continue;
                bool can = Sim.PackSwitchBlocker(o.id, out _) == null;
                string text = (o.included ? "■ " : "□ ") + XgDataUi.SourceLine(o)
                    + (o.included ? "" : "  " + Hex(XgDark.Bad) + Lang.T("训练不用") + "</color>");
                sourceBtns[i].Set(text, can, o.included ? XgDark.AccentSoft : XgDark.Button, o.included ? XgDark.Accent : XgDark.Ink);
            }
            dataBox.sizeDelta = new Vector2(0, top + SourceTitle + sourceBtns.Count * SourceRow);
        }

        void TogglePack(string offerId)
        {
            var o = Sim.Offer(offerId);
            if (o == null) return;
            if (Sim.SetPackIncluded(offerId, !o.included)) Fx.Play(o.included ? XgJuice.Sfx.Id.Click : XgJuice.Sfx.Id.Unlock, 1, .5f);
            else Fx.Play(XgJuice.Sfx.Id.Thud, 1, .4f);
            Refresh();
        }

        string PackSwitchTip(string offerId)
        {
            var o = Sim.Offer(offerId);
            if (o == null) return "";
            string tip = XgDataUi.OfferTip(Sim, o) + "\n\n" + (o.included
                ? Lang.T("现在训练会用上它。点一下关掉：包还在，只是不再算进样本和噪声。")
                : Lang.T("现在训练不用它。点一下重新用上。"));
            string why = Sim.PackSwitchBlocker(offerId, out string whyEn);
            return why == null ? tip : tip + "\n<color=#E24B4A>" + T(why, whyEn) + "</color>";
        }
    }
}
