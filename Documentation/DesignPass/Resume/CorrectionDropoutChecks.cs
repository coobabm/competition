using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;

// Candidate regression. Real TeachingGuess/TryEvaluateTeachingGuess/Board.Train;
// only the wallet is in memory. No private Dropout predicate or fake training.
public static class CorrectionDropoutChecks
{
    sealed class Wallet : IXgHost
    {
        public double Money { get; private set; } = 100;
        public double Compute => 1;
        public double VramMB => 6144;
        public string Blocker => null;
        public bool Spend(double amount) { if (amount < 0 || amount > Money) return false; Money -= amount; return true; }
        public void Earn(double amount) => Money += amount;
        public void Train(double seconds) { }
    }

    public static void Run(Action<string> log)
    {
        // Search a short fixed range using actual training outcomes. This does not
        // duplicate the implementation's Dropout hash to make the expectation.
        for (int seed = 9138; seed < 9202; seed++)
        {
            var sim = new XgSim();
            sim.S.unlocked.Add("dropout");
            var run = sim.Run(XgCatalog.Dataset("spam").track);
            run.depth = 1; run.width = XgCatalog.Widths.Length - 1;
            var card = new XgCard { id = 89001, dataset = "spam", seed = seed,
                question = "恭喜您中奖，请点击领取", truth = false, level = 1 };
            sim.S.cards.RemoveAll(c => c.dataset == "spam"); sim.S.cards.Add(card);
            var prepared = XgBoardData.FromCard(card);
            var knobs = sim.Knobs(run);
            var elements = XgBoard.Elements(prepared, knobs);
            Require(knobs.dropout && elements.Count > 1, "Actual hand-teaching knobs enable Dropout with multiple features");
            sim.S.board = new XgBoardState { cards = XgBoard.DecayEvery - 1, nextId = 901 };
            sim.S.board.concepts.Add(new XgConcept { id = 17, region = prepared.region, key = elements[0].id,
                layer = 1, w = .8, s = 1, pinned = true, trainedCards = 7, trainingCountKnown = true });
            sim.S.board.concepts.Add(new XgConcept { id = 900, region = prepared.region, key = "unrelated-test-token",
                layer = 1, w = .8, s = 1, trainedCards = 5, trainingCountKnown = true });
            Require(sim.TeachingGuess(card, out bool guess) && guess, "The real prediction is yes before the correct no-label");
            XgAnswerTrace trace = null; int events = 0;
            sim.CardAnswered += value => { events++; trace = value; };
            bool accepted = sim.TryEvaluateTeachingGuess("spam", card.id, guess, false, new Wallet(), out var result);
            Require(accepted && events == 1 && result.correct && result.corrected && trace != null,
                "The real teaching UI route settles one wrong-to-right correction");
            var droppedNew = sim.S.board.concepts.FindAll(c => c.id != 17 && c.id != 900 && c.trainedCards == 0);
            if (droppedNew.Count == 0) continue;
            log?.Invoke("Dropout fixture seed=" + seed + "; new untrained IDs=" + string.Join(",", droppedNew.ConvertAll(c => c.id.ToString())));
            var oldUnused = sim.S.board.concepts.Find(c => c.id == 900);
            Require(oldUnused != null && oldUnused.trainedCards == 5 && oldUnused.w < .8 && oldUnused.s < 1,
                "The unrelated existing cell actually decayed and did not train");
            Require(trace.CorrectionPulls.Count > 0, "Some actual learned cells still get feedback");
            foreach (var pull in trace.CorrectionPulls)
            {
                Require(pull.Id != 17 && pull.Id != 900, "Pinned and decay-only existing cells are excluded");
                Require(!droppedNew.Exists(c => c.id == pull.Id), "New Dropout-skipped cells with trainedCards=0 are excluded: " + pull.Id);
                var cell = sim.S.board.concepts.Find(c => c.id == pull.Id);
                Require(cell != null && cell.trainedCards > 0, "Every newly captured cell was actually trained");
            }
            log?.Invoke("CORRECTION_DROPOUT_CHECKS_PASS: new dropped cells, pinned anchor and old decay-only cell excluded; real pulls retained.");
            return;
        }
        throw new InvalidOperationException("The fixed seed range did not exercise a new Dropout-skipped cell");
    }

    static void Require(bool ok, string message)
    { if (!ok) throw new InvalidOperationException("CorrectionDropoutChecks: " + message); }
}
