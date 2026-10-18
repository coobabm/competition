using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;
namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgTrainPage
    {
        XgTrainingSeal ledgerSeal;
        void BuildLedgerSeal()
        {
            var slot=Rect("LedgerModule",right,Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,LedgerHeight));
            ledgerSeal=XgTrainingSeal.ForModule(slot,ui,view,"ledger",LingGuangV05.XingGuang.XgSim.LedgerNode,"查看资金、收入和电力","See funds, income and power");
        }
    }
}
