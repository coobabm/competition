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

    static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException("AnswerTraceChecks: " + message); }
}
