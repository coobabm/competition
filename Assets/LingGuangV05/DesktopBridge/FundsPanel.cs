using System.Text;
using System.Globalization;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop
{
    public sealed class FundsPanel : MonoBehaviour
    {
        ChapterOneRuntime runtime;
        GameObject panel;
        TMP_Text title, summary, entries, hint;
        float nextRefresh;
        public void Open()
        {
            runtime = GetComponent<ChapterOneRuntime>();
            if (runtime?.Sim == null || runtime.Sim.InPrologue) return;
            if (panel == null) Build();
            panel.SetActive(true); Refresh();
        }
        void Update()
        {
            if (panel == null || !panel.activeSelf) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) panel.SetActive(false);
            if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .5f; Refresh(); }
        }
        void Build()
        {
            panel = new GameObject("FundsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            panel.transform.SetParent(transform, false);
            var canvas = panel.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30900;
            var scale = panel.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1366, 900); scale.matchWidthOrHeight = .5f;
            var shade = Rect("Shade", panel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); Panel(shade, new Color(0, 0, 0, .75f));
            var card = Rect("FundsBook", shade, new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(-410,-330), new Vector2(410,330)); Panel(card, XgPalette.Page);
            var ui = new XgUi(PrologueDesk.CjkFont(), null);
            title = ui.Text(Rect("Title",card,new Vector2(0,1),Vector2.one,new Vector2(24,-55),new Vector2(-75,-15)), T("资金账本", "Funds ledger"),26,XgPalette.Ink,TextAlignmentOptions.MidlineLeft);
            var close=ui.Button(card,"×",()=>panel.SetActive(false),20); close.rt.anchorMin=close.rt.anchorMax=Vector2.one; close.rt.offsetMin=new Vector2(-64,-54); close.rt.offsetMax=new Vector2(-20,-14);
            summary=ui.Text(Rect("Summary",card,new Vector2(0,1),Vector2.one,new Vector2(24,-238),new Vector2(-24,-67)),"",16,XgPalette.Ink,TextAlignmentOptions.TopLeft);
            var viewport=Rect("Viewport",card,Vector2.zero,Vector2.one,new Vector2(24,65),new Vector2(-24,-248)); Panel(viewport,Color.clear); viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect("Entries",viewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero); content.pivot=new Vector2(.5f,1);
            entries=ui.Text(content,"",16,XgPalette.Ink,TextAlignmentOptions.TopLeft); entries.richText=false;
            var fitter=content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=content; scroll.horizontal=false; scroll.scrollSensitivity=30; scroll.movementType=ScrollRect.MovementType.Clamped;
            hint = ui.Text(Rect("Hint",card,Vector2.zero,new Vector2(1,0),new Vector2(24,12),new Vector2(-24,57)),"",14,XgPalette.Ink,TextAlignmentOptions.MidlineLeft);
        }
        void Refresh()
        {
            var sim = runtime.Sim;
            sim.RecordFunds("其他收支"); var s = sim.S;
            double pending = s.billDue + s.energyKwh * sim.Config.electricityPrice;
            title.text = T("资金账本", "Funds ledger");
            hint.text = T("训练电费每轮结算，待机电费按日结算。房租到期后去 YY 找房东转账，可先付一部分。\n显示最近 300 笔；同类收支会合并，累计金额保留。",
                "Training power is billed each round; idle power daily. Pay rent to the landlord in YY; partial payments are allowed.\nLast 300 entries; similar entries combine and lifetime totals remain.");
            summary.text = T("可用余额  ¥", "Wallet  ¥") + Amount(s.money) + T("    未结电费  ¥", "    Unbilled power  ¥") + Amount(pending)
                + "\n" + T("累计收入  +¥", "Total income  +¥") + Amount(s.fundsIncome) + T("    累计支出  −¥", "    Total expenses  −¥") + Amount(s.fundsExpense)
                + "\n" + RentSummary(sim)
                + "\n" + (s.money < 50 ? T("余额偏低：先完成已开放的工作补充资金。", "Low balance: complete available work to earn money.")
                    : T("用工作收入支付用电和升级，让它学会新的表达。", "Work pays for power and upgrades as your AI learns."));
            var b = new StringBuilder();
            for (int i = s.funds.Count - 1; i >= 0; i--)
            {
                var e = s.funds[i]; b.Append(GameText.Source(e.reason)).Append("  ").Append(e.amount >= 0 ? "+¥" : "−¥")
                    .Append(Amount(System.Math.Abs(e.amount))).Append(T("    余额 ¥", "    Balance ¥")).Append(Amount(e.balance)).Append('\n');
            }
            entries.text = b.ToString();
            summary.ForceMeshUpdate(true); entries.ForceMeshUpdate(true);

        }
        static string Amount(double amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);
        static string RentSummary(ChapterOneSim sim)
        {
            var s = sim.S;
            if (sim.Debt > 0.000001)
                return T("待付房租  ¥", "Rent still due  ¥") + Amount(sim.Debt)
                    + T(" · 已转部分已扣除", " · after payments")
                    + "\n" + (s.landlordCut ? T("房东已拉闸，补齐后恢复。去 YY 转账。", "Power cut by landlord. Settle the balance in YY to restore it.")
                        : s.debtDays > 0 ? T("已逾期，去 YY 找房东转账，可分次付。", "Overdue. Transfer to the landlord in YY; partial payments are allowed.")
                        : T("已到期，去 YY 找房东转账，可分次付。", "Due now. Transfer to the landlord in YY; partial payments are allowed."));
            if (!sim.EconomyActive)
                return T("房租尚未开始；学会「看图说词」后房东会在 YY 通知。", "Rent has not started. The landlord will message in YY after picture naming.");
            double period = sim.MonthlyRentFor(sim.NextRentDueDate);
            return T("房租已结清 · 下月 ¥", "Rent settled · next month ¥") + Amount(period)
                + "\n" + sim.NextRentDueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                + T(" 房东在 YY 发账单，手动转账。", " · the landlord sends the bill in YY; pay manually.");
        }
    }
}
