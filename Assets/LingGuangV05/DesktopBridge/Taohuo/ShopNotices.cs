using System;
using LingGuangV05.Core;
using LingGuangV05.Core.Hardware;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using UnityEngine;

namespace LingGuangV05.Desktop.Taohuo
{
    /// <summary>
    /// Tray popups for the shops, on an always-active object: 淘货 opens (stage 2), 喵鱼 opens (stage 3), a new card
    /// launches, the NVMe drive arrives, the 网吧 sale starts, Singles' Day. Only changes seen during play pop up; loading
    /// a save does not replay old news.
    /// </summary>
    public sealed class ShopNotices : MonoBehaviour
    {
        int stage = -1;
        DateTime day;
        float next;
        ChapterOneRuntime runtime;
        XingGuangController lab;

        static string T(string zh, string en) => GameText.T(zh, en);

        public static void Install(GameObject host)
        {
            if (host != null && host.GetComponent<ShopNotices>() == null) host.AddComponent<ShopNotices>();
        }

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 1;
            if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (lab == null) lab = FindAnyObjectByType<XingGuangController>();
            if (runtime == null || runtime.Sim == null || runtime.TestMode || lab == null || lab.Sim == null) return;
            int nowStage = lab.Sim.S.stage;
            var today = GameCalendar.Now(runtime.Sim.S).Date;
            if (stage < 0) { stage = nowStage; day = today; return; }
            if (stage < TaohuoView.OpenStage && nowStage >= TaohuoView.OpenStage)
                Pop(AppNames.ShopZh, AppNames.ShopEn, "亲，淘货开张啦！显卡、机箱、固态都在这里，包邮哦～", "Dear, Taohuo is open! Cards, cases and SSDs, free shipping~");
            if (stage < Miaoyu.MiaoyuView.OpenStage && nowStage >= Miaoyu.MiaoyuView.OpenStage)
                Pop(AppNames.UsedZh, AppNames.UsedEn, "喵鱼开通了：旧显卡挂上来回回血吧。", "Miaoyu is open: list your old cards for some cash back.");
            if (today > day && nowStage >= TaohuoView.OpenStage)
            {
                foreach (var g in HardwareCatalog.Gpus)
                    if (g.SoldNew && g.release.Date > day && g.release.Date <= today)
                        Pop(AppNames.ShopZh, AppNames.ShopEn, "亲，" + g.name + " 上新了！¥" + g.price.ToString("0") + "，算力 ×" + g.compute.ToString("0.##") + "。", "Dear, the " + g.nameEn + " is in! ¥" + g.price.ToString("0") + ", compute ×" + g.compute.ToString("0.##") + ".");
                if (HardwareCatalog.NvmeRelease > day && HardwareCatalog.NvmeRelease <= today)
                    Pop(AppNames.ShopZh, AppNames.ShopEn, "亲，三星 950 Pro NVMe 到货了，训练快 10% 哦～", "Dear, the Samsung 950 Pro NVMe is here, training 10% faster~");
                if (HardwareCatalog.SinglesDay > day && HardwareCatalog.SinglesDay <= today)
                    Pop(AppNames.ShopZh, AppNames.ShopEn, "双 11 狂欢：全场显卡 8 折，只限今天！", "Singles' Day: every card 20% off, today only!");
            }
            if (today > day && nowStage >= Miaoyu.MiaoyuView.OpenStage && HardwareCatalog.CafeRelease > day && HardwareCatalog.CafeRelease <= today)
                Pop(AppNames.UsedZh, AppNames.UsedEn, "网吧倒闭清仓：8 台 E5 + GTX 970 整机，一台 ¥" + HardwareCatalog.CafeBoxPrice.ToString("0") + "。", "Net cafe closing down: 8 PCs with a GTX 970 each, ¥" + HardwareCatalog.CafeBoxPrice.ToString("0") + " apiece.");
            stage = nowStage; day = today;
        }

        static void Pop(string whoZh, string whoEn, string zh, string en)
        {
            // Shop ads are ambient chatter: the opening quiet window drops them (they are news of the day, never replayed).
            if (!OpeningQuiet.Allows(false)) return;
            if (PrologueDirector.Desk != null) PrologueDirector.Desk.Popup(T(whoZh, whoEn), T(zh, en), 8);
        }
    }
}
