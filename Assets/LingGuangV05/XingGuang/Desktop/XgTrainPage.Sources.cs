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
        readonly List<XgBtn> acquireBtns = new List<XgBtn>();
        readonly List<string> acquireIds = new List<string>();
        string shownAcquireKey = "";
        TMP_Text acquireTitle;

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
            RefreshSourceOffers(run, dataBox.sizeDelta.y + 8);
        }

        void RefreshSourceOffers(XgRun run, float top)
        {
            bool unlocked = Sim.SystemUnlocked("data");
            var offers = unlocked ? Sim.OffersFor(run.dataset) : new List<XgDataOffer>();
            string key = run.dataset + ":" + string.Join(",", offers.ConvertAll(o => o.id)) + ":" + top;
            if (key != shownAcquireKey)
            {
                shownAcquireKey = key;
                foreach (var b in acquireBtns) Object.Destroy(b.rt.gameObject);
                acquireBtns.Clear(); acquireIds.Clear();
                if (acquireTitle == null)
                {
                    acquireTitle = ui.Text(Strip("AcquireSources", dataBox, top, 32, 2, 2), "", 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
                    acquireTitle.enableAutoSizing = true; acquireTitle.fontSizeMin = 10; acquireTitle.fontSizeMax = 12;
                }
                acquireTitle.rectTransform.offsetMin = new Vector2(2, -top - 32);
                acquireTitle.rectTransform.offsetMax = new Vector2(-2, -top);
                for (int i = 0; i < offers.Count; i++)
                {
                    string id = offers[i].id;
                    XgBtn button = null;
                    button = ui.Button(dataBox, "", () => { if (Sim.SystemUnlocked("data")) view.Data?.InvokeOffer(id, button.rt); }, 12);
                    button.rt.name = "AcquireSource";
                    button.rt.anchorMin = new Vector2(0, 1); button.rt.anchorMax = Vector2.one;
                    button.rt.offsetMin = new Vector2(0, -top - 32 - (i + 1) * 54 + 4);
                    button.rt.offsetMax = new Vector2(0, -top - 32 - i * 54);
                    button.label.alignment = TextAlignmentOptions.MidlineLeft;
                    button.label.enableAutoSizing = true; button.label.fontSizeMin = 10; button.label.fontSizeMax = 12;
                    UiTip.Add(button.rt, () => { var offer = Sim.Offer(id); return offer == null ? "" : XgDataUi.OfferTip(Sim, offer); });
                    acquireBtns.Add(button); acquireIds.Add(id);
                }
            }
            acquireTitle.text = unlocked ? T("补充数据 · 购买 / 众包 / 爬虫", "More data · packs / crowd / crawler") : T("更多数据来源在第三阶段开放", "More sources open at stage 3");
            for (int i = 0; i < acquireBtns.Count; i++)
            {
                var o = Sim.Offer(acquireIds[i]);
                if (o == null) continue;
                string action; bool enabled;
                if (o.downloading) { double price = Sim.AccelerateOfferCost(o.id); action = T("加速 ¥", "Speed up ¥") + Money(price); enabled = Host.Money + 1e-9 >= price; }
                else if (o.source == XgDataSource.Crawl) { action = o.owned ? T("回到爬虫", "Open crawler") : T("爬一次 ¥", "Crawl ¥") + Money(o.price); enabled = o.owned || o.available && Host.Money + 1e-9 >= o.price; }
                else if (o.source == XgDataSource.Crowd) { action = o.owned ? T("撤包", "Withdraw") : T("发包", "Post task"); enabled = o.owned || o.available; }
                else { action = o.owned ? T("已拥有", "Owned") : T("购买 ¥", "Buy ¥") + Money(o.price); enabled = !o.owned && o.available && Host.Money + 1e-9 >= o.price; }
                acquireBtns[i].Set(o.SourceLabel(Sim.English) + " · " + o.Name(Sim.English) + "\n" + action
                    + (!o.owned && !o.available && o.lockedReason.Length > 0 ? " · " + T(o.lockedReason, o.lockedReasonEn) : ""), enabled, XgDark.Button, enabled ? XgDark.Data : XgDark.Muted);
            }
            dataBox.sizeDelta = new Vector2(0, top + 32 + acquireBtns.Count * 54);
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
