using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;
namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgTrainPage
    {
        XgTrainingSeal ledgerSeal;
        void BuildLedgerSeal()
        {
            // Its own row of the column, under the ledger card; the row is shown only while the seal is.
            var slot=Rect("LedgerModule",sideContent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var size=slot.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();size.minHeight=size.preferredHeight=LedgerHeight;
            ledgerSeal=XgTrainingSeal.ForModule(slot,ui,view,"ledger",LingGuangV05.XingGuang.XgSim.LedgerNode,"查看资金、收入和电力","See funds, income and power");
            Part(slot,ledgerSeal.gameObject.activeSelf);
        }
    }
}
