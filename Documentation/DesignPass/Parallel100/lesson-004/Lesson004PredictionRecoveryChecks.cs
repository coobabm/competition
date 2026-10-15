using System;
using System.Globalization;
using System.Text;
using LingGuangV05.XingGuang;
using UnityEngine;

// Integration candidate only: place in an Editor assembly before running Run().
// Uses newly constructed in-memory state; does not load/save files or open scenes.
public static class Lesson004PredictionRecoveryChecks
{
    sealed class Wallet : IXgHost
    {
        public double Money { get; private set; } = 100;
        public double Compute => 1;
        public double VramMB => 6144;
        public string Blocker => null;
        public int Calls;
        public bool Spend(double amount)
        {
            Calls++;
            if (amount < 0 || amount > Money) return false;
            Money -= amount;
            return true;
        }
        public void Earn(double amount) { Calls++; Money += amount; }
        public void Train(double seconds) { Calls++; }
    }

    static void Require(bool value, string message)
    {
        if (!value) throw new Exception("LESSON004: " + message);
    }

    // JsonUtility excludes board links and each link's 'last'; include them explicitly.
    static string Snapshot(XgSim sim)
    {
        var text = new StringBuilder(JsonUtility.ToJson(sim.S));
        var links = sim.S.board.links;
        if (links == null) return text.Append("|links:null").ToString();
        foreach (var link in links)
            text.Append('|').Append(link.a).Append(',').Append(link.b).Append(',')
                .Append(link.c.ToString("R", CultureInfo.InvariantCulture)).Append(',').Append(link.last);
        return text.ToString();
    }

    public static void Run()
    {
        foreach (bool initialGuess in new[] { false, true })
        foreach (bool truth in new[] { false, true })
        foreach (bool staleEvaluation in new[] { false, true })
        {
            var sim = new XgSim();
            var card = new XgCard { id = 90001, dataset = "spam", seed = initialGuess ? 9138 : 9137,
                question = "恭喜您中奖，请点击领取", truth = truth, level = 1 };
            sim.S.cards.RemoveAll(c => c.dataset == "spam");
            sim.S.cards.Add(card);
            Require(sim.TeachingGuess(card, out bool shown) && shown == initialGuess, "initial display fixture");
            string unchangedCard = JsonUtility.ToJson(card);

            // Change the actual model, never the displayed card, seed, truth, or prediction function.
            var trainingCard = XgBoardData.FromCard(card);
            trainingCard.truth = !shown;
            bool current = shown;
            for (int step = 0; step < 64 && current == shown; step++)
            {
                sim.Board.Train(trainingCard, sim.Knobs(sim.Run(XgCatalog.Dataset("spam").track)));
                Require(sim.TeachingGuess(card, out current), "teaching remains available");
            }
            Require(current != shown, "real training flips the prediction (fixture must not silently skip)");
            Require(JsonUtility.ToJson(card) == unchangedCard, "same card and contents after model change");

            var wallet = new Wallet();
            int pulses = 0;
            sim.LessonTaught += (_, __) => pulses++;
            string before = Snapshot(sim);
            for (int retry = 0; retry < 2; retry++)
            {
                Require(!sim.TryEvaluateTeachingGuess("spam", card.id, shown, staleEvaluation, wallet, out _),
                    "old displayed prediction rejected, including repeated clicks");
                Require(Snapshot(sim) == before && ReferenceEquals(sim.Card("spam"), card),
                    "rejection preserves serialized state, model links, and card identity");
                Require(wallet.Money == 100 && wallet.Calls == 0 && pulses == 0,
                    "rejection calls neither wallet nor lesson event");
            }

            // Model the page's Refresh re-reading the displayed prediction, then a NEW user choice.
            Require(sim.TeachingGuess(card, out bool refreshed) && refreshed == current, "refresh obtains new guess");
            Require(sim.TryEvaluateTeachingGuess("spam", card.id, refreshed, refreshed == truth, wallet, out var result),
                "new evaluation on the same card is accepted after rejection");
            Require(result.correct && result.truth == truth && result.corrected == (refreshed != truth),
                "new choice is interpreted relative to refreshed guess");
            Require(sim.S.handCorrect == 1 && sim.S.handWrong == 0 && sim.Samples("spam") > 0
                && pulses == 1 && sim.Card("spam").id != card.id, "exactly one successful settlement advances card");
        }
        Debug.Log("LESSON004_PREDICTION_RECOVERY_CHECKS_PASS");
    }
}
