using System;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        // Ledger survives save/load. Each real ability pays once, regardless of repeated assessments.
        public int fundedAbility = 1;
        /// <summary>The 摆渡众包 order tier reached (节奏重做 §2.4): each ability opens the next, paying per second.</summary>
        public int crowdTier;
    }

    public sealed partial class XgSim
    {
        /// <summary>
        /// ¥ per second the 摆渡众包 orders pay once ability i has been learned (节奏重做 §2.4): the abilities raise a steady
        /// income instead of handing over one big sum, so each chapter's purchases take time to save up for.
        /// </summary>
        public static readonly double[] CrowdTierIncome = { 0, 0, 16, 110, 125, 250, 280 };
        /// <summary>The one-off welcome payment of a new tier is this many seconds of the tier's income.</summary>
        public const double WelcomeSeconds = 120;
        /// <summary>The welcome payment of each ability (index = ability). Declared after <see cref="CrowdTierIncome"/> on purpose.</summary>
        public static readonly int[] AbilityResearchGrant = WelcomePayments();

        static int[] WelcomePayments()
        {
            var g = new int[CrowdTierIncome.Length];
            for (int i = 0; i < g.Length; i++) g[i] = (int)Math.Round(CrowdTierIncome[i] * WelcomeSeconds);
            return g;
        }

        /// <summary>¥ per second of the crowd-order tier reached (before relics; part of <see cref="IncomePerSecond"/>).</summary>
        public double CrowdOrderIncome => CrowdTierIncome[Math.Max(0, Math.Min(AbilityCount, S.crowdTier))];

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
                double before = CrowdOrderIncome;
                S.crowdTier = Math.Max(S.crowdTier, i);
                int amount = AbilityResearchGrant[i];
                host.Earn(amount);
                S.totalIncome += amount;
                S.fundedAbility = i;
                Say(T("摆渡众包升级：它学会了「" + AbilityName(i, false) + "」，平台给你开了第 " + (i - 1) + " 档订单，每秒 ¥" + F(CrowdOrderIncome, "0") + "（原来 ¥" + F(before, "0") + "）。见面礼 +¥" + amount + "。钱按秒进账，攒够了再去科技升级。",
                      "Bodu Crowd upgrade: it learned '" + AbilityName(i, true) + "', so the platform opened order tier " + (i - 1) + " for you: ¥" + F(CrowdOrderIncome, "0") + "/s (was ¥" + F(before, "0") + "/s). Welcome bonus +¥" + amount + ". The money comes in by the second; save up, then upgrade the model."));
            }
        }
    }
}
