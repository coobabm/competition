using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>Why a desk is or is not being labelled by 自动答题 right now, in the order the checks are made.</summary>
    public enum XgAutoDeskState { Off, NoCheckpoint, BelowBar, PausedByYou, Held, InboxFull, Project, NoPower, Running }

    /// <summary>
    /// One open desk as 摆渡众包's 自动提交 page shows it: whether the model labels it, and which model does
    /// (the deployed best checkpoint, or 灵光's own brain for text desks once 「灵光的大脑」 is owned and online).
    /// </summary>
    public sealed class XgAutoDeskPlan
    {
        public XgDesk desk;
        public XgAutoDeskState state;
        /// <summary>The deployed checkpoint (S.best) and its repository entry; the entry is null for old saves.</summary>
        public XgBest best;
        public XgModelEntry model;
        public XgArch arch;
        public int depth = 1;
        public double accuracy;
        /// <summary>Text cards here go to 灵光's own brain (the local language model) instead of the checkpoint.</summary>
        public bool usesBrain;
        /// <summary>This desk would use the brain, but it is offline (or offline simulation), so the checkpoint answers.</summary>
        public bool brainFallback;
        public int brainJudgments;
        public double brainAccuracy;
        /// <summary>Cards per second and ¥ per second while running (0 otherwise).</summary>
        public double rate, income;
        /// <summary>The last <see cref="XgSim.RouteWindow"/> routed cards of this desk.</summary>
        public XgCollaborationStats recent;
        public bool Running => state == XgAutoDeskState.Running;
        /// <summary>Running, or running with a full review inbox (only the unsure cards are skipped then).</summary>
        public bool Earning => state == XgAutoDeskState.Running || state == XgAutoDeskState.InboxFull;
    }

    public sealed partial class XgSim
    {
        /// <summary>The state that holds every desk at once (Running when nothing global stops 自动答题).</summary>
        public XgAutoDeskState AutoGlobalState(IXgHost host)
        {
            if (GlobalAutoLevel <= 0 || AutoLabelHidden) return XgAutoDeskState.Off;
            if (S.autoPaused) return XgAutoDeskState.PausedByYou;
            if (AutomationHeld) return XgAutoDeskState.Held;
            if (ProjectActive) return XgAutoDeskState.Project;
            if (host == null || host.Blocker != null || host.Compute <= 0) return XgAutoDeskState.NoPower;
            // Informational: a full inbox no longer stops income, it only skips the low-confidence cards.
            if (ReviewFull) return XgAutoDeskState.InboxFull;
            return XgAutoDeskState.Running;
        }

        /// <summary>Every open desk, with the model that answers it and why it is (not) running.</summary>
        public List<XgAutoDeskPlan> AutoDeskPlans(IXgHost host)
        {
            var plans = new List<XgAutoDeskPlan>();
            var global = AutoGlobalState(host);
            foreach (var desk in XgCatalog.Desks)
            {
                if (!DeskOpen(desk.id)) continue;
                var p = new XgAutoDeskPlan { desk = desk, best = Best(desk.id), accuracy = BestAcc(desk.id), recent = CollaborationStats(desk.id) };
                p.model = p.best == null ? null : Model(p.best.modelId);
                var dataset = XgCatalog.Dataset(desk.id);
                // The same deployed shape JudgeCheckpoint builds.
                string archId = p.best != null && !string.IsNullOrEmpty(p.best.arch) ? p.best.arch : dataset != null ? DefaultArchitecture(dataset.track) : "";
                p.arch = XgCatalog.Arch(archId);
                p.depth = p.model == null ? 1 : p.model.depth;
                bool brainDesk = Has("label.brain") && IsBrainDesk(desk.id);
                p.usesBrain = brainDesk && UsesBrain(desk.id);
                p.brainFallback = brainDesk && !p.usesBrain;
                p.brainJudgments = BrainJudgmentCount(desk.id);
                p.brainAccuracy = BrainAccuracy(desk.id);
                if (global == XgAutoDeskState.Off) p.state = XgAutoDeskState.Off;
                else if (p.best == null || p.accuracy <= 0) p.state = XgAutoDeskState.NoCheckpoint;
                else if (!EligibleDesk(desk.id)) p.state = XgAutoDeskState.BelowBar;
                else p.state = global;
                if (p.Earning)
                {
                    p.rate = CollaborationRate(desk.id, host);
                    p.income = CollaborationIncome(desk.id, host);
                }
                plans.Add(p);
            }
            return plans;
        }
    }
}
