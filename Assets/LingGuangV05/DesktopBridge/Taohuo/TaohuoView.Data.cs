using System;
using System.Globalization;
using System.Text;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.Taohuo
{
    /// <summary>
    /// 淘货's 数据 tab (design v1.1 §11.8.3): the lab's public packs and the cheap 淘货杂包 on the shelves this month,
    /// from <see cref="XgSim.ShopDataOffers"/>. A public pack is the same purchase as its skill-tree node. Big packs
    /// download through 迅雷 / 摆渡云, and a slow one can be sped up with a day of super membership.
    /// </summary>
    public sealed partial class TaohuoView
    {
        static readonly Color Blue = new Color32(60, 140, 231, 255), Red = new Color32(220, 60, 50, 255);

        bool HasData { get { var lab = Lab; if (lab == null) return false; foreach (var _ in lab.ShopDataOffers()) return true; return false; } }

        /// <summary>Rebuild when an offer appears, is bought, or finishes downloading.</summary>
        string DataSignature()
        {
            var lab = Lab;
            if (lab == null) return "";
            var sb = new StringBuilder();
            foreach (var o in lab.ShopDataOffers()) sb.Append(o.id).Append(o.owned ? 'o' : '-').Append(o.downloading ? 'd' : '-').Append(o.available ? 'a' : '-').Append(';');
            return sb.ToString();
        }

        void BuildData(string q)
        {
            var lab = Lab;
            var host = FindAnyObjectByType<XingGuang.XingGuangController>()?.Host;
            if (lab == null) { Note(Lang.T("灵光还没装好。")); return; }
            int shown = 0;
            foreach (var o in lab.ShopDataOffers())
            {
                if (q.Length > 0 && (o.name + o.nameEn + o.description + o.descriptionEn).IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                DataRow(lab, host, o);
                shown++;
            }
            if (shown == 0) Note(q.Length > 0 ? T("没有找到「" + q + "」相关的数据包。", "No data packs for \"" + q + "\".") : Lang.T("这个月还没有数据包上架。"));
            else Note(Lang.T("公开包干净，杂包便宜但脏（噪声高）。研究树的「数据清洗」能把杂包噪声减半。"));
        }

        void DataRow(XgSim lab, IXgHost host, XgDataOffer o)
        {
            bool junk = o.source == XgDataSource.Junk;
            var row = Row(list, o.id, 140, Color.white);
            var pic = PrologueDesk.Rect("Picture", row, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(14, -56), new Vector2(126, 56));
            PrologueDesk.Fill(pic, junk ? new Color32(120, 40, 30, 255) : new Color32(30, 70, 130, 255), false);
            var d = XgCatalog.Dataset(o.datasetId);
            string datasetName = d != null ? T(d.name, d.nameEn) : o.datasetId;
            if (!ProductIllustration(pic, "data", "<b>" + datasetName + "</b>"))
                Label(pic, "Name", Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-4, 0), "<b>" + datasetName + "</b>", 18, Color.white, TextAlignmentOptions.Center);
            var mid = PrologueDesk.Rect("Mid", row, Vector2.zero, Vector2.one, new Vector2(140, 10), new Vector2(-200, -10));
            Label(mid, "Title", new Vector2(0, 1), Vector2.one, new Vector2(0, -28), Vector2.zero, "<b>" + o.Name(GameText.IsEnglish) + "</b>", 18, Ink);
            string badge = "<color=#" + ColorUtility.ToHtmlStringRGB(junk ? Red : Blue) + ">[" + o.SourceLabel(GameText.IsEnglish) + "]</color>"
                + (o.isNew ? "  <color=#FF5000>" + Lang.T("[新品]") + "</color>" : "")
                + (junk ? "  <color=#999999>" + Lang.T("[爬虫新鲜货]") + "</color>" : "  <color=#999999>" + Lang.T("[正规授权]") + "</color>");
            Label(mid, "Badges", new Vector2(0, 1), Vector2.one, new Vector2(0, -52), new Vector2(0, -30), badge, 14, Ink);
            string facts = T("样本 ", "Samples ") + o.samples.ToString("#,0", CultureInfo.InvariantCulture) + "  ·  " + Lang.T("噪声 ") + (o.effectiveNoise * 100).ToString("0", CultureInfo.InvariantCulture) + "%"
                + (o.downloadSec > 0 ? "  ·  " + Lang.T("下载约 ") + o.downloadSec.ToString("0", CultureInfo.InvariantCulture) + Lang.T(" 秒") : "");
            Label(mid, "Facts", new Vector2(0, 1), Vector2.one, new Vector2(0, -76), new Vector2(0, -54), facts, 15, Ink);
            Label(mid, "Desc", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -78), o.Description(GameText.IsEnglish), 14, Muted);
            var right = PrologueDesk.Rect("Right", row, new Vector2(1, 0), Vector2.one, new Vector2(-190, 12), new Vector2(-14, -12));
            Label(right, "Price", new Vector2(0, 1), Vector2.one, new Vector2(0, -44), Vector2.zero, "<b>" + Money(o.price) + "</b>", 28, Orange, TextAlignmentOptions.TopRight);
            Label(right, "Sold", new Vector2(0, 1), Vector2.one, new Vector2(0, -70), new Vector2(0, -46), junk ? Lang.T("月销 9999+ 笔") : Lang.T("包邮 · 秒发"), 14, Muted, TextAlignmentOptions.TopRight);
            string id = o.id;
            var b = Btn(right, "Buy", Vector2.zero, new Vector2(1, 0), new Vector2(20, 0), new Vector2(0, 44), Orange, "", 17, Color.white, () => BuyData(id), out var label);
            buyButtons.Add((b, label, () => DataState(id)));
            UiTip.Add(b, () => DataTip(id));
        }

        (bool, string) DataState(string id)
        {
            var lab = Lab; var sim = Sim;
            if (lab == null || sim == null) return (false, "—");
            var o = lab.Offer(id);
            if (o == null) return (false, "—");
            if (o.downloading) return (sim.S.money + 1e-9 >= lab.AccelerateOfferCost(id), Lang.T("下载中 ") + (o.downloadProgress * 100).ToString("0") + "%  " + Lang.T("加速 ") + Money(lab.AccelerateOfferCost(id)));
            if (o.owned) return (false, Lang.T("已拥有"));
            if (!o.available) return (false, T(o.lockedReason, o.lockedReasonEn));
            if (sim.S.money + 1e-9 < o.price) return (false, Lang.T("钱不够"));
            return (true, Lang.T("立即购买"));
        }

        string DataTip(string id)
        {
            var lab = Lab;
            var o = lab != null ? lab.Offer(id) : null;
            if (o == null) return "";
            if (o.downloading) return Lang.T("迅雷下载中。摆渡云超级会员一天，立刻下完。");
            if (!o.available && !o.owned && o.lockedReason.Length > 0) return T(o.lockedReason, o.lockedReasonEn);
            return o.source == XgDataSource.Junk
                ? Lang.T("便宜量大，但标签错得多：噪声会拉低准确率。配上「数据清洗」才划算。")
                : Lang.T("公开包：和科技里的数据节点是同一样东西，买一次就行。");
        }

        void BuyData(string id)
        {
            var lab = Lab;
            var host = FindAnyObjectByType<XingGuang.XingGuangController>()?.Host;
            if (lab == null || host == null) { Say(Lang.T("灵光还没装好。")); return; }
            var o = lab.Offer(id);
            if (o == null) return;
            bool ok = o.downloading ? lab.AccelerateOffer(id, host) : lab.BuyDataOffer(id, host);
            string last = lab.S.log.Count > 0 ? lab.S.log[lab.S.log.Count - 1] : "";
            if (ok) { Dirty(); Say(last.Length > 0 ? last : Lang.T("亲，数据包已发货～")); }
            else Say(last.Length > 0 ? last : Lang.T("现在买不了。"));
            signature = "";
        }
    }
}
