using System;
using System.Globalization;
using System.Text;
using LingGuangV05.Core;
using LingGuangV05.Core.Girlfriend;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop.Taohuo
{
    /// <summary>
    /// 淘货's 「送她」 tab (design 女友系统 §4.6): 2016's gifts for 林晴雯 on their release dates, free shipping, signed
    /// for two or three calendar days later, when she answers in YY. Pure spending: nothing here produces anything.
    /// The rules (effects, decay, memory match, the cold-tier cap) are GirlfriendRules'.
    /// </summary>
    public sealed partial class TaohuoView
    {
        const string GiftTab = "gift";
        static readonly (string zh, string en) GiftTabTip = ("给她买点 2016 年的小东西。记得她说过的，比贵的管用。", "Little 2016 things for her. Remembering what she said beats spending more.");

        static GirlfriendState Girl => YYChatHub.Instance != null && YYChatHub.Instance.S != null ? YYChatHub.Instance.S.girlfriend : null;
        static bool HasGifts { get { var g = Girl; return g != null && g.started && YYGirlfriend.Instance != null; } }

        string GiftSignature()
        {
            var g = Girl;
            if (g == null) return "";
            var sb = new StringBuilder();
            foreach (var o in g.orders) sb.Append(o.gift).Append(o.state).Append(';');
            return sb.ToString();
        }

        void BuildGifts(DateTime today)
        {
            var g = Girl;
            if (g == null || !HasGifts) { Note(Lang.T("这里还没有东西。")); return; }
            int day = GameCalendar.DayIndex(today);
            if (today.Month == 11 && today.Day == 11)
            {
                var banner = Row(list, "CartBanner", 54, Orange);
                Label(banner, "Text", Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-18, 0), Lang.T("<b>双 11：帮她清空购物车！</b>"), 22, Color.white, TextAlignmentOptions.MidlineLeft);
            }
            foreach (var gift in GirlfriendRules.Gifts) GiftRow(g, gift, today, day);
            OrdersList(g);
            Note(Lang.T("包邮，2–3 天送到她学校（奶茶当天到）。签收以后她会在 YY 上找你。"));
        }

        void GiftRow(GirlfriendState g, GfGift gift, DateTime today, int day)
        {
            var row = Row(list, gift.id, 112, Color.white);
            var pic = PrologueDesk.Rect("Picture", row, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(14, -46), new Vector2(106, 46));
            PrologueDesk.Fill(pic, new Color32(255, 228, 236, 255), false);
            if (!ProductIllustration(pic, "gift_" + gift.id))
                Label(pic, "Glyph", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "<b>" + Glyph(gift.id) + "</b>", 30, new Color32(232, 80, 120, 255), TextAlignmentOptions.Center);
            var mid = PrologueDesk.Rect("Mid", row, Vector2.zero, Vector2.one, new Vector2(122, 10), new Vector2(-200, -10));
            Label(mid, "Title", new Vector2(0, 1), Vector2.one, new Vector2(0, -30), Vector2.zero, "<b>" + T(gift.zh, gift.en) + "</b>", 18, Ink);
            bool onSale = GirlfriendRules.OnSale(gift, today);
            string badges = onSale
                ? "<color=#FF5000>" + Lang.T("[包邮]") + "</color>  <color=#999999>" + T(gift.id == GirlfriendRules.GiftMilkTea ? "[当天送达]" : "[2–3 天送达]", gift.id == GirlfriendRules.GiftMilkTea ? "[Same day]" : "[2–3 days]") + "</color>"
                : "<color=#999999>" + T("[" + gift.from.Month + " 月 " + gift.from.Day + " 日上架]", "[On sale " + gift.from.ToString("d MMMM", CultureInfo.InvariantCulture) + "]") + "</color>";
            Label(mid, "Badges", new Vector2(0, 1), Vector2.one, new Vector2(0, -54), new Vector2(0, -32), badges, 14, Ink);
            Label(mid, "Pitch", new Vector2(0, 0), Vector2.one, Vector2.zero, new Vector2(0, -56), T(gift.pitchZh, gift.pitchEn), 15, Muted);
            var right = PrologueDesk.Rect("Right", row, new Vector2(1, 0), Vector2.one, new Vector2(-190, 12), new Vector2(-14, -12));
            Label(right, "Price", new Vector2(0, 1), Vector2.one, new Vector2(0, -38), Vector2.zero, "<b>" + Money(gift.price) + "</b>", 26, Orange, TextAlignmentOptions.TopRight);
            string id = gift.id;
            var b = Btn(right, "Buy", new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 0), new Vector2(0, 40), Orange, "", 17, Color.white, () => BuyGift(id), out var label);
            buyButtons.Add((b, label, () => GiftState(id)));
            TipFor(b, ("送到才算。记得她说过的东西，效果加倍；同一样送多了就没意思了。", "It only counts once it arrives. Something she mentioned counts double; the same thing again means less each time."));
        }

        (bool, string) GiftState(string id)
        {
            var g = Girl; var gift = GirlfriendRules.Gift(id); var sim = Sim;
            if (g == null || gift == null || sim == null) return (false, "—");
            if (!GirlfriendRules.OnSale(gift, Today)) return (false, Lang.T("未上架"));
            if (GirlfriendRules.SoldOut(g, gift, GameCalendar.DayIndex(Today), Today)) return (false, Lang.T("已售罄"));
            if (sim.S.money + 1e-9 < gift.price) return (false, Lang.T("钱不够"));
            return (true, T("送她", "Send to her"));
        }

        void BuyGift(string id)
        {
            var gf = YYGirlfriend.Instance;
            if (gf == null) { Say(Lang.T("亲，现在下不了单哦。")); return; }
            gf.OrderGift(id, out string message);
            Say(message, 8);
            signature = "";
        }

        void OrdersList(GirlfriendState g)
        {
            if (g.orders.Count == 0) return;
            var head = Row(list, "OrdersHead", 34, new Color(0, 0, 0, 0));
            Label(head, "Text", Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0), "<b>" + Lang.T("我的订单") + "</b>", 17, Ink, TextAlignmentOptions.MidlineLeft);
            for (int i = g.orders.Count - 1, shown = 0; i >= 0 && shown < 8; i--, shown++)
            {
                var o = g.orders[i];
                var gift = GirlfriendRules.Gift(o.gift);
                if (gift == null) continue;
                var arrive = GameCalendar.DateOf(o.arriveDay);
                string state = o.state == 1 ? "<color=#3CB371>" + Lang.T("已签收") + "</color>" + (o.matched ? Lang.T("  · 她说过想要") : "")
                    : o.state == 2 ? "<color=#999999>" + Lang.T("已拒收 · 已退款") + "</color>"
                    : "<color=#FF5000>" + T("运输中 · 预计 " + arrive.Month + " 月 " + arrive.Day + " 日送达", "On the way · arrives " + arrive.ToString("d MMMM", CultureInfo.InvariantCulture)) + "</color>";
                var row = Row(list, "Order", 36, Color.white);
                Label(row, "Text", Vector2.zero, Vector2.one, new Vector2(14, 0), new Vector2(-14, 0), T(gift.zh, gift.en) + "   " + Money(o.price) + "   " + state, 15, Ink, TextAlignmentOptions.MidlineLeft);
            }
        }

        static string Glyph(string id)
        {
            switch (id)
            {
                case GirlfriendRules.GiftMilkTea: return Lang.T("奶茶");
                case GirlfriendRules.GiftPlush: return Lang.T("熊");
                case GirlfriendRules.GiftBand: return Lang.T("手环");
                case GirlfriendRules.GiftPowerBank: return Lang.T("充电");
                case GirlfriendRules.GiftLipstick: return "YSL";
                case GirlfriendRules.GiftTickets: return Lang.T("电影");
                case GirlfriendRules.GiftIphone: return "7";
                default: return Lang.T("情侣");
            }
        }
    }
}
