using System.Text;
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
        TMP_Text summary, entries;
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
            ui.Text(Rect("Title",card,new Vector2(0,1),Vector2.one,new Vector2(24,-55),new Vector2(-75,-15)), "资金账本",26,XgPalette.Ink,TextAlignmentOptions.MidlineLeft);
            var close=ui.Button(card,"×",()=>panel.SetActive(false),20); close.rt.anchorMin=close.rt.anchorMax=Vector2.one; close.rt.offsetMin=new Vector2(-64,-54); close.rt.offsetMax=new Vector2(-20,-14);
            summary=ui.Text(Rect("Summary",card,new Vector2(0,1),Vector2.one,new Vector2(24,-185),new Vector2(-24,-67)),"",17,XgPalette.Ink,TextAlignmentOptions.TopLeft);
            var viewport=Rect("Viewport",card,Vector2.zero,Vector2.one,new Vector2(24,65),new Vector2(-24,-195)); Panel(viewport,Color.clear); viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect("Entries",viewport,new Vector2(0,1),Vector2.one,Vector2.zero,Vector2.zero); content.pivot=new Vector2(.5f,1);
            entries=ui.Text(content,"",16,XgPalette.Ink,TextAlignmentOptions.TopLeft); entries.richText=false;
            var fitter=content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=content; scroll.horizontal=false; scroll.scrollSensitivity=30; scroll.movementType=ScrollRect.MovementType.Clamped;
            ui.Text(Rect("Hint",card,Vector2.zero,new Vector2(1,0),new Vector2(24,12),new Vector2(-24,57)),"每轮训练即付电费，待机用电按日结算。余额低时先做众包。\n显示最近 300 笔；短时间内同类收支合并，累计金额持续保留。",14,XgPalette.Ink,TextAlignmentOptions.MidlineLeft);
        }
        void Refresh()
        {
            runtime.Sim.RecordFunds("其他收支"); var s=runtime.Sim.S;
            double pending=s.billDue+s.energyKwh*runtime.Sim.Config.electricityPrice;
            summary.text="可用余额  ¥"+s.money.ToString("0.00")+"    未结电费  ¥"+pending.ToString("0.00")
                +"\n累计收入  +¥"+s.fundsIncome.ToString("0.00")+"    累计支出  −¥"+s.fundsExpense.ToString("0.00")
                +"\n"+(s.money<50?"余额偏低：先接手工任务补充资金，再决定是否训练。":"目标：用工作收入支付用电和升级，让它学会新的表达。")
                +"\n房租："+(runtime.Sim.EconomyActive?"每分钟折算一天，¥"+runtime.Sim.RentFor(LingGuangV05.Core.GameCalendar.Now(s)).ToString("0")+" / 天":"第三章开始收取；电费从开局计费。");
            var b=new StringBuilder();
            for(int i=s.funds.Count-1;i>=0;i--) { var e=s.funds[i]; b.Append(e.reason).Append("  ").Append(e.amount>=0?"+¥":"−¥").Append(System.Math.Abs(e.amount).ToString("0.00")).Append("    余额 ¥").Append(e.balance.ToString("0.00")).Append('\n'); }
            entries.text=b.ToString();
        }
    }
}
