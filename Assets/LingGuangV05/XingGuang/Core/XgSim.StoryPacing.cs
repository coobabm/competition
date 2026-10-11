using System;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        // Ledger survives save/load. Each real ability pays once, regardless of repeated assessments.
        public int fundedAbility = 1;
    }

    public sealed partial class XgSim
    {
        public static readonly int[] AbilityResearchGrant = { 0, 0, 8000, 12000, 20000, 36000, 80000 };

        // One clear route for a new player; every other architecture remains available in the tree.
        public XgNode NextStudyNode()
        {
            int next = NextAbility;
            if (next == 0) return null;
            if (TrainedSamples < SamplesThreshold(next))
            {
                string pack = new[] { "spam.pack", "mnist.pack", "poems.pack", "logic.pack", "translate.pack" }[Math.Min(4, S.stage - 1)];
                var data = MissingStudyPrerequisite(pack);
                if (data != null) return data;
            }
            if (TrainedParamsK >= ParamsThreshold(next) || ParamsK(S.sequence) >= ParamsThreshold(next) || ParamsK(S.vision) >= ParamsThreshold(next)) return null;
            string[][] route = {
                new[] { "mlp", "s.d2", "s.w1", "s.w2" },
                new[] { "rnn", "s.d3", "s.w3" },
                new[] { "lstm", "s.d4", "s.w4" },
                new[] { "seq2seq", "attention", "s.d6", "s.w5" },
                new[] { "transformer", "s.d8", "s.w6" }
            };
            foreach (string id in route[Math.Min(4, S.stage - 1)])
            {
                var node = MissingStudyPrerequisite(id);
                if (node != null) return node;
            }
            return null;
        }

        XgNode MissingStudyPrerequisite(string id)
        {
            var node = XgCatalog.Node(id);
            if (node == null || Has(id) || !NodeVisible(node) || ProgressionBlocker(node) != null) return null;
            if (!string.IsNullOrEmpty(node.parent) && !Has(node.parent)) return MissingStudyPrerequisite(node.parent);
            foreach (var need in node.needs) if (!Has(need)) return MissingStudyPrerequisite(need);
            return node;
        }

        void PayAbilityResearch(IXgHost host)
        {
            if (host == null) return;
            int reached = AbilitiesCount;
            for (int i = Math.Max(2, S.fundedAbility + 1); i <= reached; i++)
            {
                int amount = AbilityResearchGrant[i];
                host.Earn(amount);
                S.totalIncome += amount;
                S.fundedAbility = i;
                Say(T("众包绩效结算 +¥", "Crowd-work milestone payment +¥") + amount
                    + T(" · 去科技升级，再回来教它新东西。", " · Upgrade the model, then teach it something new."));
            }
        }
    }
}
