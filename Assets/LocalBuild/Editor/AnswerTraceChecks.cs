using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
#if ANSWER_TRACE_STANDALONE
using Debug = AnswerTraceStandaloneLog;
using JsonUtility = AnswerTraceStandaloneJson;
#else
using UnityEngine;
#endif

/// <summary>Core-only in-memory checks. Does not create a runtime, access saves, or validate rendered paths.</summary>
public static class AnswerTraceChecks
{
    sealed class Wallet : IXgHost
    {
        public double Money { get; private set; } = 100;
        public double Compute => 1;
        public double VramMB => 6144;
        public string Blocker => null;
        public bool Spend(double amount) { if (amount < 0 || amount > Money) return false; Money -= amount; return true; }
        public void Earn(double amount) { Money += amount; }
        public void Train(double seconds) { }
    }

    public static void Run()
    {
        CheckPrediction();
        CheckAnswerSettlement();
        CheckCorrectionPulls();
        CheckClampedCorrectionPulls();
        CheckCreatedCorrectionPulls();
        CorrectionDropoutChecks.Run(message => Debug.Log(message));
        CheckEmptySourcesAndRejections();
        Debug.Log("ANSWER_TRACE_CORE_CHECKS_PASS: readout evidence and settled events; real frames and wallpaper still require acceptance.");
    }

    static void CheckPrediction()
    {
        var state = new XgBoardState();
        state.concepts.Add(new XgConcept { id = 1, key = "a", region = "logic", layer = 1, w = .8 });
        state.concepts.Add(new XgConcept { id = 2, key = "b", region = "logic", layer = 2, w = -.2 });
        state.concepts.Add(new XgConcept { id = 3, key = "zero", region = "logic", layer = 1, w = .001 });
        state.nextId = 4;
        var board = new XgBoard(state);
        var card = new XgBoardCard { region = "logic", seed = 71 }.Add("a").Add("b").Add("zero");
        string before = JsonUtility.ToJson(state);
        foreach (XgWiring wiring in Enum.GetValues(typeof(XgWiring)))
        foreach (bool features in new[] { false, true })
        {
            var knobs = new XgKnobs { depth = 3, width = 32, wiring = wiring, features = features, dropout = true };
            bool oldAnswer = board.Predict(card, knobs, out bool oldGuessed);
            bool answer = board.Predict(card, knobs, out bool guessed, out var trace);
            Require(answer == oldAnswer && guessed == oldGuessed && trace.AnswerYes == answer && trace.Guessed == guessed,
                "Prediction result is identical with observation: " + wiring + "/" + features);
            Require(trace.UsedConcepts.Count == 2 && trace.UsedConcepts[0].Id == 1 && trace.UsedConcepts[1].Id == 2,
                "Readout contains both signed votes, excludes the threshold-skipped concept, and preserves order");
            Require(JsonUtility.ToJson(state) == before && state.links.Count == 0,
                "Observing prediction does not change board state or grow links");
        }
        state.concepts[0].pinned = true; state.concepts[1].pinned = true;
        board.Predict(card, new XgKnobs(), out _, out var pinned);
        Require(pinned.UsedConcepts.Count == 1 && pinned.UsedConcepts[0].Id == 1 && pinned.Score == 80 && pinned.AnswerYes,
            "Pinned short-circuit reports only the first rule actually selected");
        state.concepts[0].w = -4; state.concepts[0].key = "changed"; state.concepts[0].layer = 9;
        Require(pinned.UsedConcepts[0].Weight == .8 && pinned.UsedConcepts[0].Key == "a" && pinned.UsedConcepts[0].Layer == 1,
            "Stored path is a value snapshot unaffected by later concept mutations");
        bool rejectedWrite = false;
        try { ((IList<XgUsedConcept>)pinned.UsedConcepts)[0] = null; }
        catch (NotSupportedException) { rejectedWrite = true; }
        Require(rejectedWrite, "Observers cannot replace entries in the path snapshot");

        var empty = new XgBoard();
        foreach (int seed in new[] { 8, 9 })
        {
            card.seed = seed;
            bool answer = empty.Predict(card, new XgKnobs(), out bool guessed, out var trace);
            Require(guessed && trace.UsedConcepts.Count == 0 && answer == ((seed & 1) == 0),
                "A deterministic guess has an empty concept path");
        }
    }

    static XgSim Fixture(bool truth, out XgCard card, string dataset = "spam")
    {
        var sim = new XgSim();
        card = new XgCard { id = 89001, dataset = dataset, seed = 9138, question = "恭喜您中奖，请点击领取", truth = truth, level = 1 };
        sim.S.cards.RemoveAll(c => c.dataset == dataset); sim.S.cards.Add(card);
        var prepared = XgBoardData.FromCard(card);
        var knobs = sim.Knobs(sim.Run(XgCatalog.Dataset(dataset).track));
        var elements = XgBoard.Elements(prepared, knobs);
        Require(elements.Count > 0, "Trace fixture contains actual card features");
        sim.S.board = new XgBoardState();
        sim.S.board.concepts.Add(new XgConcept { id = 17, key = elements[0].id, region = prepared.region, layer = 1, w = .8, s = 1 });
        sim.S.board.nextId = 18;
        sim.TeachingGuess(card, out _); // Existing display initialization occurs before each test.
        return sim;
    }

    static void CheckAnswerSettlement()
    {
        foreach (bool yes in new[] { false, true })
        foreach (bool correct in new[] { false, true })
        {
            var template = Fixture(correct ? yes : !yes, out _);
            string saved = JsonUtility.ToJson(template.S);
            var control = new XgSim(JsonUtility.FromJson<XgState>(saved));
            var observed = new XgSim(JsonUtility.FromJson<XgState>(saved));
            var card = observed.Card("spam");
            var boardCard = XgBoardData.FromCard(card);
            observed.Board.Predict(boardCard, observed.Knobs(observed.Run(XgCatalog.Dataset("spam").track)), out _, out var before);
            int events = 0; XgAnswerTrace captured = null;
            observed.CardAnswered += trace => { events++; captured = trace; Require(observed.Card("spam").id != card.id, "Answer event occurs after original card advances"); };
            var plainWallet = new Wallet(); var observedWallet = new Wallet();
            var plain = control.Answer("spam", yes, plainWallet);
            var result = observed.Answer("spam", yes, observedWallet);
            Require(events == 1 && captured.Settled && captured.CardId == card.id && captured.SubmittedYes == yes && captured.Outcome.correct == correct,
                "Exactly one settled event carries original card, submitted answer, and unchanged correctness");
            Require(captured.Source == XgAnswerTraceSource.LocalBoard && captured.Prediction != null &&
                captured.Prediction.AnswerYes == before.AnswerYes && captured.Prediction.UsedConcepts.Count == before.UsedConcepts.Count &&
                captured.Prediction.UsedConcepts[0].Weight == before.UsedConcepts[0].Weight,
                "Answer snapshot records the actual prediction before successful training can change it");
            Require(JsonUtility.ToJson(result) == JsonUtility.ToJson(plain) && JsonUtility.ToJson(observed.S) == JsonUtility.ToJson(control.S) &&
                observedWallet.Money == plainWallet.Money, "Subscribing changes no outcome, simulation state, RNG, or payment");
            Require(!captured.Outcome.accepted && captured.Outcome.cardId == 0,
                "Ordinary outcome defaults remain unchanged; Settled and original CardId are explicit event metadata");
        }

        var teaching = Fixture(true, out var teachingCard);
        Require(teaching.TeachingGuess(teachingCard, out bool shown), "Teaching fixture has a current prediction");
        int answered = 0, lessons = 0; XgAnswerTrace taught = null;
        teaching.CardAnswered += t => { answered++; taught = t; };
        teaching.LessonTaught += (_, __) => lessons++;
        var wallet = new Wallet();
        Require(teaching.TryEvaluateTeachingGuess("spam", teachingCard.id, shown, false, wallet, out var evaluated),
            "Current teaching evaluation is accepted");
        Require(answered == 1 && taught.SubmittedYes == !shown && taught.Prediction.AnswerYes == shown &&
            lessons == (evaluated.correct ? 1 : 0), "Teaching emits once and distinguishes model yes/no from player evaluation");
        Require(!teaching.TryEvaluateTeachingGuess("spam", teachingCard.id, shown, false, wallet, out _) && answered == 1,
            "Repeated stale teaching card emits no additional event");
    }

    static void CheckEmptySourcesAndRejections()
    {
        foreach (var source in new[] { XgAnswerTraceSource.Manual, XgAnswerTraceSource.Family, XgAnswerTraceSource.Law, XgAnswerTraceSource.Shutdown })
        {
            var sim = Fixture(true, out var card, source == XgAnswerTraceSource.Shutdown ? "logic" : "spam");
            if (source == XgAnswerTraceSource.Manual) sim.S.teachMode = false;
            if (source == XgAnswerTraceSource.Family) { card.family = true; card.from = "妈妈"; }
            if (source == XgAnswerTraceSource.Law) card.law = true;
            if (source == XgAnswerTraceSource.Shutdown) card.kind = "shutdown";
            int events = 0; XgAnswerTrace captured = null;
            sim.CardAnswered += t => { events++; captured = t; };
            var result = sim.Answer(card.dataset, true, new Wallet());
            Require(events == 1 && captured.Settled && captured.Source == source && captured.Prediction == null &&
                captured.CardId == card.id && captured.Outcome.subjective == result.subjective,
                "Non-prediction source settles once without fabricated concept usage: " + source);
            Require(captured.CorrectionPulls.Count == 0, "Non-prediction sources do not invent correction pulls");
            if (source == XgAnswerTraceSource.Family)
                Require(result.subjective && result.accepted && sim.S.handCorrect == 0 && sim.S.handWrong == 0,
                    "Family opinion is not classified as an accuracy result");
            if (source == XgAnswerTraceSource.Law) Require(result.samples == 0, "Law answer adds no training sample");
        }

        var late = Fixture(true, out var lateCard);
        lateCard.timeLimit = 1; lateCard.age = 2;
        XgAnswerTrace timeout = null; int count = 0;
        late.CardAnswered += t => { count++; timeout = t; };
        var lateResult = late.Answer("spam", true, new Wallet());
        Require(count == 1 && lateResult.timeout && timeout.Outcome.timeout && !timeout.Outcome.correct && timeout.Settled,
            "A submitted expired card is a settled late answer and still emits once");
        Require(timeout.CorrectionPulls.Count == 0, "Expired answers never claim corrected cells");

        var invalid = Fixture(true, out var invalidCard);
        int invalidEvents = 0;
        invalid.CardAnswered += _ => invalidEvents++;
        string before = JsonUtility.ToJson(invalid.S);
        invalid.Answer("spam", true, null); invalid.Answer("missing-desk", true, new Wallet());
        invalid.TeachingGuess(invalidCard, out bool guess);
        Require(!invalid.TryEvaluateTeachingGuess("spam", invalidCard.id, !guess, true, new Wallet(), out _),
            "Stale displayed prediction is rejected");
        Require(invalidEvents == 0 && JsonUtility.ToJson(invalid.S) == before,
            "Invalid host/desk and stale teaching prediction emit nothing and change no state");
        invalid.Board.Predict(XgBoardData.FromCard(invalidCard), invalid.Knobs(invalid.Run(XgCatalog.Dataset("spam").track)), out _, out _);
        invalid.TeachingGuess(invalidCard, out _);
        Require(invalidEvents == 0, "Prediction/display queries never broadcast settled-answer events");
    }

    static void CheckCorrectionPulls()
    {
        foreach (bool pinned in new[] { false, true })
        {
            var sim = Fixture(false, out var card);
            sim.S.board.concepts[0].pinned = pinned;
            // This unrelated cell is not pulled by this card's lesson.
            sim.S.board.concepts.Add(new XgConcept { id = 900, region = "sequence", key = "unrelated-test-token", w = .8, s = 1 });
            var before = new Dictionary<int, XgConcept>();
            foreach (var cell in sim.S.board.concepts) before[cell.id] = JsonUtility.FromJson<XgConcept>(JsonUtility.ToJson(cell));
            XgAnswerTrace trace = null;
            sim.CardAnswered += t => trace = t;
            var outcome = sim.Answer("spam", false, new Wallet());
            Require(outcome.correct && outcome.corrected && trace.CorrectionPulls.Count > 0, "Real wrong-to-right lesson captures changed cells");
            foreach (var pull in trace.CorrectionPulls)
            {
                var after = sim.S.board.concepts.Find(c => c.id == pull.Id);
                bool existed = before.TryGetValue(pull.Id, out var old);
                Require(after != null && !after.pinned && pull.Id != 900 && (!pinned || pull.Id != 17),
                    "Only actual learned, non-pinned cells receive correction highlights");
                Require(pull.BeforeWeight == (existed ? old.w : 0) && pull.AfterWeight == after.w
                    && pull.BeforeStrength == (existed ? old.s : after.layer == 1 ? 1 : 0) && pull.AfterStrength == after.s && pull.Created == !existed,
                    "Correction captures exact pre-decay write values, including initialized new R1 cells");
                double captured = pull.AfterWeight;
                after.w += 1;
                Require(pull.AfterWeight == captured, "Later learning cannot mutate a previous correction snapshot");
            }
            bool readOnly = false;
            try { ((IList<XgConceptPull>)trace.CorrectionPulls).Clear(); }
            catch (NotSupportedException) { readOnly = true; }
            Require(readOnly, "Correction collection cannot be edited by presentation observers");
        }
        var wrong = Fixture(true, out _); XgAnswerTrace rejected = null;
        wrong.CardAnswered += t => rejected = t;
        wrong.Answer("spam", false, new Wallet());
        Require(!rejected.Outcome.correct && rejected.CorrectionPulls.Count == 0,
            "An incorrect player correction never claims a learning change");
    }

    static void CheckClampedCorrectionPulls()
    {
        double learnedWeight = 0, learnedStrength = 0;
        foreach (bool decay in new[] { false, true })
        {
            var template = new XgSim();
            var run = template.Run(XgCatalog.Dataset("spam").track);
            run.depth = 1; run.width = XgCatalog.Widths.Length - 1;
            var card = new XgCard { id = 89001, dataset = "spam", seed = 9139,
                question = "恭喜您中奖，请点击领取", truth = false, level = 1 };
            template.S.cards.RemoveAll(c => c.dataset == "spam"); template.S.cards.Add(card);
            var prepared = XgBoardData.FromCard(card);
            var knobs = template.Knobs(run);
            var elements = XgBoard.Elements(prepared, knobs);
            Require(!knobs.dropout && elements.Count > 2, "Clamp fixture has three actual features without Dropout");
            template.S.board = new XgBoardState { cards = XgBoard.DecayEvery - (decay ? 1 : 2), nextId = 901 };
            template.S.board.concepts.Add(new XgConcept { id = 17, region = prepared.region, key = elements[0].id,
                layer = 1, w = .8, s = 1, pinned = true, trainedCards = 7, trainingCountKnown = true });
            template.S.board.concepts.Add(new XgConcept { id = 18, region = prepared.region, key = elements[1].id,
                layer = 1, w = -XgBoard.WeightLimit, s = 10, trainedCards = 5, trainingCountKnown = true });
            template.S.board.concepts.Add(new XgConcept { id = 19, region = prepared.region, key = elements[2].id,
                layer = 1, w = .25, s = 1, trainedCards = long.MaxValue, trainingCountKnown = true });
            string saved = JsonUtility.ToJson(template.S);
            var sim = new XgSim(JsonUtility.FromJson<XgState>(saved));
            var control = new XgSim(JsonUtility.FromJson<XgState>(saved));
            Require(sim.TeachingGuess(sim.Card("spam"), out bool guess) && guess &&
                control.TeachingGuess(control.Card("spam"), out bool plainGuess) && plainGuess,
                "Both real teaching predictions are wrong before correction");
            XgAnswerTrace trace = null; int events = 0;
            sim.CardAnswered += value => { events++; trace = value; };
            var wallet = new Wallet(); var plainWallet = new Wallet();
            bool plainAccepted = control.TryEvaluateTeachingGuess("spam", card.id, guess, false, plainWallet, out var plain);
            bool accepted = sim.TryEvaluateTeachingGuess("spam", card.id, guess, false, wallet, out var result);
            Require(plainAccepted && accepted && result.corrected && events == 1,
                "The real teaching UI route settles one correction, with and without an observer");
            Require(JsonUtility.ToJson(plain) == JsonUtility.ToJson(result) && JsonUtility.ToJson(control.S) == JsonUtility.ToJson(sim.S)
                && wallet.Money == plainWallet.Money, "Write capture preserves outcome, state, RNG and payment across the decay boundary");
            var clamped = sim.S.board.concepts.Find(c => c.id == 18);
            var changed = sim.S.board.concepts.Find(c => c.id == 19);
            XgConceptPull learned = null;
            foreach (var pull in trace.CorrectionPulls)
            {
                Require(pull.Id != 17 && pull.Id != 18, "Pinned and fully clamped R1 cells never claim a write, even when R4 fades them");
                if (pull.Id == 19) learned = pull;
            }
            Require(clamped.trainedCards == 6 && changed.trainedCards == long.MaxValue,
                "Real R1 runs for the clamped cell and still writes when the lesson counter is saturated");
            Require(learned != null && !learned.Created && learned.BeforeWeight == .25 && learned.BeforeStrength == 1
                && learned.AfterWeight < .25 && learned.AfterStrength > 1, "An actual changed cell is captured with precise prior values");
            if (!decay)
            {
                Require(clamped.w == -XgBoard.WeightLimit && clamped.s == 10, "Clamping and saturation leave both parameters unchanged before decay");
                learnedWeight = learned.AfterWeight; learnedStrength = learned.AfterStrength;
                Require(changed.w == learnedWeight && changed.s == learnedStrength, "Without R4 the write snapshot equals the final board values");
            }
            else
            {
                Require(clamped.w > -XgBoard.WeightLimit && clamped.s < 10, "The clamped cell actually fades on the decay step");
                Require(learned.AfterWeight == learnedWeight && learned.AfterStrength == learnedStrength,
                    "The same actual write has the same snapshot on either side of the decay boundary");
                Require(changed.w != learned.AfterWeight && changed.s < learned.AfterStrength,
                    "The snapshot intentionally precedes R4, rather than claiming the decayed final values");
            }
        }
        Debug.Log("CORRECTION_WRITE_CHECKS_PASS: clamped decay-only cells excluded; actual writes, saturated counters and pre-decay values retained.");
    }

    static void CheckCreatedCorrectionPulls()
    {
        var initialWrites = new Dictionary<int, XgConceptPull>();
        foreach (bool decay in new[] { false, true })
        {
            var sim = Fixture(false, out var card);
            sim.S.unlocked.Add("relu");
            var run = sim.Run(XgCatalog.Dataset("spam").track);
            run.depth = 2; run.width = XgCatalog.Widths.Length - 1; run.lr = 1; run.act = 2;
            sim.S.board.concepts[0].pinned = true;
            sim.S.board.cards = XgBoard.DecayEvery - (decay ? 1 : 2);
            Require(sim.TeachingGuess(card, out bool guess) && guess, "R2 fixture uses a real wrong teaching prediction");
            XgAnswerTrace trace = null;
            sim.CardAnswered += value => trace = value;
            Require(sim.TryEvaluateTeachingGuess("spam", card.id, guess, false, new Wallet(), out var result) && result.corrected,
                "R2 fixture settles through the actual teaching route");
            int combinations = 0;
            foreach (var pull in trace.CorrectionPulls)
            {
                var cell = sim.S.board.concepts.Find(c => c.id == pull.Id);
                Require(cell != null && cell.id != 17 && pull.Created && pull.BeforeWeight == 0,
                    "Every captured cell in this fixture was actually created, excluding the pinned anchor");
                if (cell.layer > 1)
                {
                    combinations++;
                    Require(pull.BeforeStrength == 0 && pull.AfterStrength == 1 && cell.trainedCards == 1,
                        "Actual R2 creation captures an absent 0/0 baseline and the initialized combination before decay");
                }
                else Require(pull.BeforeStrength == 1, "R1 creation records its initialized strength before the actual pull");
                if (!decay)
                {
                    initialWrites.Add(pull.Id, pull);
                    Require(pull.AfterWeight == cell.w && pull.AfterStrength == cell.s, "Without R4 the creation/write snapshot equals board values");
                }
                else
                {
                    Require(initialWrites.TryGetValue(pull.Id, out var previous) && previous.AfterWeight == pull.AfterWeight
                        && previous.AfterStrength == pull.AfterStrength, "R1 and R2 capture the same writes before a later decay step");
                    Require(cell.s < pull.AfterStrength, "Created cells really decay after the immutable evidence was captured");
                }
            }
            Require(combinations > 0, "The real training rule must create at least one higher-layer combination");
        }
        Debug.Log("CORRECTION_CREATION_CHECKS_PASS: actual R1 writes and R2 creations captured before decay with distinct initial baselines.");
    }

    static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException("AnswerTraceChecks: " + message); }
}
