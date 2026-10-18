using System;
using LingGuangV05.Core;
using LingGuangV05.Core.Hardware;
using LingGuangV05.Desktop.Story;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Taohuo
{
    /// <summary>
    /// 淘货's 2TB portable drive (节奏重做 §2.2) on the 存储 tab: room for downloaded data, each one ×1.3 the price of
    /// the last (<see cref="XgSim.NextDrivePrice"/>). Listed once the lab's disk has filled up (or a drive was bought, or
    /// it is nearly full). Its buy button is named "BuyDrive" for the guide's locator.
    /// </summary>
    public sealed partial class TaohuoView
    {
        public const string BuyDriveName = "BuyDrive";

        /// <summary>The drive is worth listing: the disk filled up once, a drive was bought, or it is 85% full.</summary>
        static bool DriveListed(XgSim lab) =>
            lab != null && (lab.DiskFullSeen || lab.DrivesOwned > 0 || lab.DiskCapacityMB > 0 && lab.DiskUsedMB / lab.DiskCapacityMB >= .85);

        /// <summary>Downloads are stopped for a full disk.</summary>
        static bool DiskBlocked(XgSim lab) => lab != null && lab.DownloadBlocker(out _) != null;

        string DriveSignature()
        {
            var lab = Lab;
            return lab == null ? "" : (DriveListed(lab) ? "d" : "-") + lab.DrivesOwned + (DiskBlocked(lab) ? "f" : "-");
        }

        void BuildDrive()
        {
            var lab = Lab;
            if (!DriveListed(lab)) return;
            var drive = HardwareCatalog.PortableDrive;
            if (DiskBlocked(lab))
            {
                var banner = Row(list, "DiskFull", 48, Red);
                Label(banner, "Text", Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-18, 0),
                    "<b>" + T("亲，您的硬盘满了，下载已经暂停～插一块移动硬盘就接着下", "Dear, your disk is full and downloads are paused~ plug in a portable drive to carry on") + "</b>", 18, Color.white, TextAlignmentOptions.MidlineLeft);
            }
            var row = Row(list, "Drive", 150, Color.white);
            var pic = PrologueDesk.Rect("Picture", row, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(14, -60), new Vector2(146, 60));
            PrologueDesk.Fill(pic, new Color32(40, 44, 52, 255), false);
            Label(pic, "Name", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "<b>2TB</b>\n<size=14>USB 3.0</size>", 26, Color.white, TextAlignmentOptions.Center);
            var mid = PrologueDesk.Rect("Mid", row, Vector2.zero, Vector2.one, new Vector2(162, 10), new Vector2(-200, -10));
            Label(mid, "Title", new Vector2(0, 1), Vector2.one, new Vector2(0, -50), Vector2.zero, T(drive.title, drive.titleEn), 18, Ink);
            Label(mid, "Badges", new Vector2(0, 1), Vector2.one, new Vector2(0, -74), new Vector2(0, -52),
                "<color=#FF5000>" + Lang.T("[包邮]") + "</color>  <color=#3C8CE7>" + Lang.T("[淘货自营]") + "</color>  <color=#999999>" + T("[每多买一块贵 30%]", "[each one costs 30% more]") + "</color>", 14, Ink);
            Label(mid, "Stats", new Vector2(0, 1), Vector2.one, new Vector2(0, -98), new Vector2(0, -76), T(drive.pitch, drive.pitchEn), 15, Ink);
            // Live: the lab's disk, used / capacity (UpdateLive keeps it current through the buy button's state).
            var usage = Label(mid, "Usage", Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 26), "", 14, Muted, TextAlignmentOptions.BottomLeft);
            var right = PrologueDesk.Rect("Right", row, new Vector2(1, 0), Vector2.one, new Vector2(-190, 12), new Vector2(-14, -12));
            var price = Label(right, "Price", new Vector2(0, 1), Vector2.one, new Vector2(0, -44), Vector2.zero, "", 28, Orange, TextAlignmentOptions.TopRight);
            Label(right, "Sold", new Vector2(0, 1), Vector2.one, new Vector2(0, -70), new Vector2(0, -46), Lang.T("月销 ") + (drive.sold + 37 * Math.Max(0, (Today - drive.release.Date).Days % 30)).ToString("#,0") + Lang.T(" 笔"), 14, Muted, TextAlignmentOptions.TopRight);
            var b = Btn(right, BuyDriveName, Vector2.zero, new Vector2(1, 0), new Vector2(20, 0), new Vector2(0, 44), Orange, "", 18, Color.white, BuyDrive, out var label);
            buyButtons.Add((b, label, () =>
            {
                var l = Lab;
                if (l != null)
                {
                    price.text = "<b>" + Money(l.NextDrivePrice) + "</b>";
                    usage.text = DiskUsageText(l);
                }
                return DriveState();
            }));
            UiTip.Add(b, () => T("给" + (Lab != null ? Lab.AiDisplayName : AppNames.Ai(false)) + "扩充资料空间。科技里的数据包买完直接在「教」使用。每多买一块，价格 ×1.3。", "Adds storage for " + (Lab != null ? Lab.AiDisplayName : AppNames.Ai(true)) + ". Tech data packs are ready in Teach immediately. Each additional drive costs ×1.3."));
        }

        /// <summary>"灵光的硬盘：已用 480 GB / 500 GB · 下载暂停".</summary>
        static string DiskUsageText(XgSim lab)
        {
            double cap = lab.DiskCapacityMB, used = Math.Min(cap, lab.DiskUsedMB);
            string state = DiskBlocked(lab) ? "  <color=#DC3C32>" + T("满了 · 下载暂停", "full · downloads paused") + "</color>"
                : cap > 0 && used / cap >= .85 ? "  <color=#FF5000>" + T("快满了", "nearly full") + "</color>" : "";
            return T(lab.AiDisplayName + "的硬盘：已用 ", lab.AiDisplayName + "'s disk: ") + XgSim.SizeText(used) + " / " + XgSim.SizeText(cap) + T("", " used")
                + (lab.DrivesOwned > 0 ? T(" · 已插 " + lab.DrivesOwned + " 块移动硬盘", " · " + lab.DrivesOwned + " portable drive" + (lab.DrivesOwned > 1 ? "s" : "")) : "") + state;
        }

        (bool, string) DriveState()
        {
            var lab = Lab; var sim = Sim;
            if (lab == null || sim == null) return (false, "—");
            if (sim.S.money + 1e-9 < lab.NextDrivePrice) return (false, Lang.T("钱不够"));
            return (true, Lang.T("立即购买"));
        }

        void BuyDrive()
        {
            var lab = Lab;
            var host = FindAnyObjectByType<XingGuang.XingGuangController>()?.Host;
            if (lab == null || host == null) { Say(Lang.T("灵光还没装好。")); return; }
            bool ok = lab.BuyDrive(host);
            string last = lab.S.log.Count > 0 ? lab.S.log[lab.S.log.Count - 1] : "";
            if (ok) { Dirty(); Say(last.Length > 0 ? last : T("亲，移动硬盘已发货～", "Dear, your drive has shipped~")); }
            else Say(last.Length > 0 ? last : Lang.T("现在买不了。"));
            signature = "";
        }
    }
}
