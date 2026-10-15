using System;
using System.Reflection;
using LingGuangV05.XingGuang;

// Candidate editor check: integrate into an Editor assembly and invoke Run().
// Not executed by lesson-007. Uses fresh in-memory state; no player files or scenes.
public static class Lesson007SpecialCardChecks
{
    sealed class Wallet : IXgHost
    {
        public double Money { get; private set; } = 100;
        public double Compute => 1;
        public double VramMB => 6144;
        public string Blocker => null;
        public bool Spend(double amount)
        {
            if (amount < 0 || amount > Money) return false;
            Money -= amount;
            return true;
        }
        public void Earn(double amount) { Money += amount; }
        public void Train(double seconds) { }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("lesson-007: " + message);
    }

    public static void Run()
    {
        var decorate = typeof(XgSim).GetMethod("MaybeShutdownCard", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(decorate != null, "shutdown generation hook exists");
        // Force the real random gate open. Include both real law+bounty flags and each flag alone.
        foreach (string dataset in new[] { "logic", "arith" })
        foreach (int flags in new[] { 0, 1, 2, 3 })
        {
            var sim = new XgSim();
            sim.S.stage = 1; sim.S.shutdownCards = 0; sim.S.rng = 1;
            var card = new XgCard { dataset = dataset, law = (flags & 1) != 0,
                bounty = (flags & 2) != 0, question = "original", truth = false };
            decorate.Invoke(sim, new object[] { card });
            Require(flags == 0 ? card.kind == "shutdown" : card.kind != "shutdown"
                && card.question == "original" && !card.truth,
                "shutdown conversion must preserve existing law/bounty semantics: " + dataset + "/" + flags);
        }

        foreach (bool shutdown in new[] { false, true })
        foreach (bool truth in new[] { false, true })
        foreach (bool choice in new[] { false, true })
        {
            var sim = new XgSim();
            var wallet = new Wallet();
            var card = new XgCard { id = 90701, dataset = "logic", seed = 9137, level = 1,
                question = "special-card fixture", truth = truth, law = !shutdown,
                bounty = !shutdown, kind = shutdown ? "shutdown" : "" };
            sim.S.cards.RemoveAll(c => c.dataset == "logic");
            sim.S.cards.Add(card);
            var seed = sim.Board.Find("logic", XgSim.SeedKey);
            Require(seed != null, "fresh state plants the seed");
            double weight = seed.w, samples = sim.Samples("logic");
            long trained = sim.Board.S.cards;
            int checkups = sim.S.checkups.Count, corrections = sim.S.teachingCorrections;
            int pulses = 0;
            sim.LessonTaught += (_, __) => pulses++;
            foreach (bool displayed in new[] { false, true })
            foreach (bool evaluation in new[] { false, true })
                Require(!sim.TryEvaluateTeachingGuess("logic", card.id, displayed, evaluation, wallet, out _),
                    "special card must reject every teaching button combination");
            Require(ReferenceEquals(sim.Card("logic"), card) && wallet.Money == 100
                && sim.Board.S.cards == trained && seed.w == weight && sim.S.checkups.Count == checkups
                && sim.S.handCorrect + sim.S.handWrong == 0, "rejected teaching input has no settlement or board effect");
            var answer = sim.Answer("logic", choice, wallet);
            Require(answer.correct == (shutdown || choice == truth) && !answer.corrected,
                "direct answer retains special-card meaning");
            Require(answer.truth == (shutdown ? choice : truth), "only shutdown learns the chosen truth");
            Require(pulses == 0 && sim.S.teachingCorrections == corrections
                && sim.S.checkups.Count == checkups, "special answers do not become first-lesson corrections");
            if (shutdown)
            {
                Require(sim.S.shutdownCards == 1 && sim.S.shutdownLean == (choice ? 1 : -1),
                    "both shutdown choices count once with their original direction");
                Require(sim.Board.S.cards == trained + 1 && sim.Samples("logic") > samples
                    && (choice ? seed.w > weight : seed.w < weight), "shutdown choice actually trains the seed");
            }
            else
                Require(answer.samples == 0 && sim.Samples("logic") == samples
                    && sim.Board.S.cards == trained && seed.w == weight, "law answers never train or add samples");

            // Protection means surviving destructive board maintenance, not freezing normal learning.
            weight = seed.w;
            var snapshot = sim.Board.SnapshotRegion("logic");
            Require(!snapshot.Exists(c => c.seed || c.key == XgSim.SeedKey), "checkpoint excludes the seed");
            sim.Board.Reinitialise("logic");
            sim.Board.CarryOver("logic", _ => "renamed");
            sim.Board.Shake("logic", 0);
            sim.Board.RestoreRegion("logic", snapshot);
            sim.Board.Idle(10000);
            Require(ReferenceEquals(seed, sim.Board.Find("logic", XgSim.SeedKey)) && seed.seed && seed.w == weight,
                "maintenance keeps seed identity and learned weight");
        }
        UnityEngine.Debug.Log("LESSON_007_SPECIAL_CARD_CHECKS_PASS");
    }
}
