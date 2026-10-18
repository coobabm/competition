namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        // Zero identifies a save from before the paid ledger. No initializer: absent
        // JSON must remain distinguishable from a new game stamped by the constructor.
        public int ledgerAccessVersion;
    }
    public sealed partial class XgSim
    {
        public const string LedgerNode="train.ledger";
        public bool LedgerOpen => !S.teachingFlow||Has(LedgerNode);
        void RepairLedgerAccess(bool fresh)
        {
            if(S.ledgerAccessVersion>=1)return;
            // Every old .exe header already exposed wallet, income and power. Grant
            // ownership once, including old teachingFlow saves, rather than charging
            // them to restore previously visible information.
            if(!fresh&&!S.unlocked.Contains(LedgerNode))S.unlocked.Add(LedgerNode);
            S.ledgerAccessVersion=1;
        }
    }
}
