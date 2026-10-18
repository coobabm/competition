using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;
namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XingGuangView
    {
        TMP_Text fundsWarning;
        void BuildLedgerWarning(RectTransform stats)
        {
            fundsWarning=ui.Text(Rect("FundsDanger",stats,Vector2.zero,Vector2.one,new Vector2(0,4),new Vector2(-98,-4)),"",12,XgDark.Hot,TextAlignmentOptions.MidlineRight);
            fundsWarning.raycastTarget=false;
            fundsWarning.enableAutoSizing=false;
        }
        void RefreshLedgerVisibility()
        {
            bool open=Sim.LedgerOpen;
            moneyRect.gameObject.SetActive(open||Tab=="tree");
            statIncome.transform.parent.gameObject.SetActive(open);
            statPower.transform.parent.gameObject.SetActive(open);
            if(fundsWarning==null)return;
            string warning="";
            var house=controller.runtime?.Sim;
            if(house!=null)
            {
                if(house.S.landlordCut)warning=T("房租欠付，已被拉闸","Rent overdue: power cut");
                else if(house.S.unpaidPower)warning=T("电费欠付，请处理欠费","Power bill overdue");
                else if(house.S.breakerTripped||(Sim.Wiring!=null&&Sim.Wiring.tripped))warning=T("已跳闸，请检查用电","Breaker tripped: check power");
                else if(house.S.debtDays>=2&&house.Debt>0)warning=T("房租逾期，即将断电","Rent overdue: power at risk");
                else if(house.Debt>0)warning=T("房租到期，去 YY 转账","Rent due: transfer in YY");
                else if(Host.Money<50)warning=T("余额偏低，留意用电支出","Low funds: watch power costs");
            }
            fundsWarning.rectTransform.offsetMin=new Vector2(Tab=="tree"?112:0,4);
            fundsWarning.gameObject.SetActive(!open&&warning.Length>0);
            fundsWarning.text=warning;
        }
    }
}
