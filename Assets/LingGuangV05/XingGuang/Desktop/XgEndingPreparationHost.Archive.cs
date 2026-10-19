using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgEndingPreparationHost
    {
        static bool BeforeChoiceJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            return XgEndingArchiveRules.BeforeChoice(JsonUtility.FromJson<XgState>(json));
        }
        XgEndingPreparationResult ChooseWithArchive(XgEndingPreparationBinding token, bool askAi, string route = null)
        {
            if (!Current || !flow.IsCurrent(token)) { lastCode = "stale_binding"; return null; }
            if (!XgEndingArchiveRules.BeforeChoice(boundLab.S)) { lastCode = "release_choice_unavailable"; return null; }
            if (!askAi && !XgEpilogueFlow.IsRoute(route)) { lastCode = "invalid_route"; return null; }
            // Snapshot is written before the real choice transaction changes phase/route/AI decision.
            var save = boundSave;
            if (!owner.runtime.CaptureEndingChoice(save, BeforeChoiceJson, out var id))
            { lastCode = "checkpoint_failed"; return null; }
            if (!Current || !flow.IsCurrent(token)) { lastCode = "stale_binding"; return null; }
            string previous = save.S.endingCheckpointId;
            bool committed = false;
            try
            {
                save.S.endingCheckpointId = id;
                var result = askAi ? flow.AskAiToChoose(token) : flow.ChooseRoute(token, route);
                committed = result != null && result.Success;
                return result;
            }
            finally
            {
                // An immutable orphan is harmless and never listed as a completion or a resumable branch.
                if (!committed) save.S.endingCheckpointId = previous;
            }
        }
    }
}
