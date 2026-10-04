using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The label page's data-source row (design v1.1 §11.8.3): the shop's pack row cycles through every source of the
    /// desk's data (public pack, 淘货 junk pack, 老周's story data, a crowd task on 摆渡众包) and shows samples, noise
    /// and download progress for each.
    /// </summary>
    public sealed partial class XgLabelPage
    {
        XgBtn sourceSwitch;
        string sourcePickId = "";

        void BuildSourceSwitch()
        {
            sourceSwitch = ui.Button(pack.Root, "", NextSource, 11);
            sourceSwitch.rt.gameObject.name = "SourceSwitch";
            sourceSwitch.rt.anchorMin = sourceSwitch.rt.anchorMax = new Vector2(1, 1);
            sourceSwitch.rt.offsetMin = new Vector2(-122, -21); sourceSwitch.rt.offsetMax = new Vector2(-10, -3);
            UiTip.Add(sourceSwitch.rt, "换一个数据来源：公开包、淘货杂包、众包标注、剧情数据。便宜的脏，干净的贵。", "Switch the data source: public pack, junk pack, crowd labelling, story data. Cheap is dirty, clean is dear.");
        }

        /// <summary>Sources of this desk worth showing: the public pack always, the rest once owned or on offer.</summary>
        List<XgDataOffer> DeskSources(string desk)
        {
            var list = new List<XgDataOffer>();
            foreach (var o in Sim.OffersFor(desk))
                if (o.source == XgDataSource.Public || o.owned || o.available) list.Add(o);
            return list;
        }

        XgDataOffer PickedSource(string desk)
        {
            var list = DeskSources(desk);
            if (list.Count == 0) return null;
            var picked = list.Find(o => o.id == sourcePickId);
            return picked ?? list[0];
        }

        void NextSource()
        {
            var list = DeskSources(Desk);
            if (list.Count == 0) return;
            int i = list.FindIndex(o => o.id == sourcePickId);
            sourcePickId = list[(i + 1) % list.Count].id;
            Fx.Play(XgJuice.Sfx.Id.Click);
            Refresh();
        }

        /// <summary>Fills the pack row from the picked source. False when there is nothing to show (the old row then draws).</summary>
        bool RefreshSourceRow(string desk)
        {
            var list = DeskSources(desk);
            var o = PickedSource(desk);
            if (o == null) { sourceSwitch.Show(false); return false; }
            int index = list.FindIndex(x => x.id == o.id) + 1;
            sourceSwitch.Show(list.Count > 1);
            sourceSwitch.Set("⇄ " + T("来源 ", "Source ") + index + "/" + list.Count, true);
            pack.SetGlyph(XgDataUi.Glyph(o.source));

            string title = (o.source == XgDataSource.Public ? T("完整数据包", "Full data pack") : o.Name(En));
            string line = XgDataUi.SourceLine(o);
            if (!o.owned && !o.available) line = "<color=#D63031>" + T(o.lockedReason, o.lockedReasonEn) + "</color>  <size=11><color=#68748C>" + line + "</color></size>";
            if (o.source == XgDataSource.Crowd && o.owned) line += T(" · 每秒 −¥", " · −¥") + N(o.samples * o.price, "0.00") + T("", "/s");

            string price; bool affordable; float toPrice, fill;
            if (o.downloading)
            {
                double speed = Sim.AccelerateOfferCost(o.id);
                price = T("加速 ¥", "Speed ¥") + Money(speed);
                affordable = Host.Money >= speed; toPrice = (float)o.downloadProgress; fill = (float)o.downloadProgress;
            }
            else if (o.source == XgDataSource.Crowd)
            {
                price = o.owned ? T("撤包", "Withdraw") : T("发包", "Post");
                affordable = o.owned || o.available; toPrice = o.owned ? 1 : 0; fill = o.owned ? 1 : 0;
            }
            else if (o.owned) { price = "✓"; affordable = false; toPrice = 1; fill = 1; }
            else
            {
                price = "¥" + Money(o.price);
                affordable = o.available && Host.Money + 1e-9 >= o.price;
                toPrice = o.price > 0 ? (float)(Host.Money / o.price) : 1; fill = 0;
            }
            pack.Set(title, o.owned ? (o.source == XgDataSource.Crowd ? T("进行中", "Live") : T("已拥有", "Owned")) : o.SourceLabel(En), line, price, affordable, toPrice, fill);
            return true;
        }

        string SourceTip()
        {
            if (Sim == null) return "";
            var o = PickedSource(Desk);
            return o != null ? XgDataUi.OfferTip(Sim, o) : T("完整数据包：一次买下整套数据，不用手标就能训练。", "Full data pack: buy the whole dataset at once and train without labelling.");
        }

        /// <summary>Handles the row's button for downloads and non-public sources; false lets the tree buy the public pack.</summary>
        bool BuySource()
        {
            var o = PickedSource(Desk);
            if (o == null) return false;
            if (o.downloading)
            {
                if (Sim.AccelerateOffer(o.id, Host)) { pack.Celebrate(Fx, T("下完了", "Downloaded"), XgPalette.Accent, 1); view.Refresh(true); }
                else pack.Deny(Fx);
                return true;
            }
            if (o.source == XgDataSource.Public) return false;
            bool ok = o.source == XgDataSource.Crowd ? Sim.SetCrowd(o.datasetId, !o.owned) : Sim.BuyDataOffer(o.id, Host);
            if (!ok) { pack.Deny(Fx); return true; }
            if (o.source != XgDataSource.Crowd || !o.owned) pack.Celebrate(Fx, o.SourceLabel(En), new Color32(18, 150, 140, 255), 1);
            else Fx.Play(XgJuice.Sfx.Id.Click);
            view.Refresh(true);
            return true;
        }
    }
}
