using System.Globalization;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgSim
    {
        XgTestSetPreparation pendingTestSet;
        string pendingTestSetCulture;
        bool pendingTestSetEnglish;
        public void CancelTestSetPreparation() { pendingTestSet = null; pendingTestSetCulture = null; }
        string TestSetCacheKey(string dataset, int level) => dataset + "|" + level + "|" + (XgBoardData.Topical(dataset) ? Today / 100 : 0) + "|" + S.dataSalt;
        bool PendingContextMatches(string dataset, int level, int today) => pendingTestSet != null
            && pendingTestSet.Matches(dataset, level, today, S.dataSalt)
            && pendingTestSetCulture == CultureInfo.CurrentCulture.Name && pendingTestSetEnglish == English;
        // One pending set per simulation: bounded memory, no new serialized state, no live board/rng/economy writes.
        public bool PrepareTestSet(string dataset, int quota)
        {
            if (string.IsNullOrEmpty(dataset) || XgCatalog.Dataset(dataset) == null) { CancelTestSetPreparation(); return true; }
            int level = BoardLevel(dataset);
            int today = XgBoardData.Topical(dataset) ? Today : 0;
            if (!PendingContextMatches(dataset, level, today))
            {
                if (testSets.ContainsKey(TestSetCacheKey(dataset, level))) { CancelTestSetPreparation(); return true; }
                pendingTestSet = new XgTestSetPreparation(dataset, level, today, S.dataSalt);
                pendingTestSetCulture = CultureInfo.CurrentCulture.Name; pendingTestSetEnglish = English;
            }
            pendingTestSet.Advance(quota);
            return pendingTestSet.Complete;
        }
        System.Collections.Generic.List<XgBoardCard> TakePreparedTestSet(string dataset, int level, int today)
        {
            if (!PendingContextMatches(dataset, level, today) || !pendingTestSet.Complete) { CancelTestSetPreparation(); return null; }
            var result = pendingTestSet.Result;
            CancelTestSetPreparation();
            return result;
        }
    }
}
