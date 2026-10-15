using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using LingGuangV05.XingGuang;
using UnityEngine;

/// <summary>In-memory first-card checks. No runtime, scene, save directory or player file is accessed.</summary>
public static class DesignFirstLessonChecks
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

    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Debug.Log("FIRST_LESSON_CHECK: " + message);
    }

    // Control prediction direction through the untrained board's seed, never by replacing its answer code.
    static XgSim Fixture(bool guess, bool truth, out XgCard card)
    {
        var sim = new XgSim();
        sim.SelectDesk("spam"); // This answer-path fixture deliberately visits the SMS desk.
        card = new XgCard { id = 90001, dataset = "spam", seed = guess ? 9138 : 9137,
            question = "恭喜您中奖，请点击领取", truth = truth, level = 1 };
        sim.S.cards.RemoveAll(c => c.dataset == "spam");
        sim.S.cards.Add(card);
        Check(sim.TeachingGuess(card, out bool prediction) && prediction == guess, "Fixture predicts " + guess);
        return sim;
    }

    public static void Run()
    {
        var first = new XgSim();
        Check(first.S.desk == XgMemes.SenseDesk && first.S.handCorrect == 0 && first.S.handWrong == 0,
            "Fresh game starts at the first common-sense lesson");
        var firstCard = first.Card(first.S.desk);
        Check(first.TeachingGuess(firstCard, out bool firstGuess), "First card guesses before any player answer or training");
        bool firstTruth = firstCard.truth;
        firstCard.truth = !firstTruth;
        Check(first.TeachingGuess(firstCard, out bool pairedGuess) && pairedGuess == firstGuess,
            "Untrained common-sense prediction does not read ground truth");
        firstCard.truth = firstTruth;
        Check(first.TeachingGuess(firstCard, out bool repeatedGuess) && repeatedGuess == firstGuess
            && ReferenceEquals(first.Card(first.S.desk), firstCard) && first.Samples(XgMemes.SenseDesk) == 0
            && first.S.best.Count == 0 && first.S.models.Count == 0 && first.S.handCorrect + first.S.handWrong == 0,
            "Repeated first-card display does not teach, settle or create a checkpoint");

        // Both guess directions x both truth values x accurate/inaccurate player evaluation.
        foreach (bool guess in new[] { false, true })
        foreach (bool truth in new[] { false, true })
        foreach (bool accurateEvaluation in new[] { false, true })
        {
            var sim = Fixture(guess, truth, out var card);
            var wallet = new Wallet();
            double samplesBefore = sim.Samples("spam");
            int pulses = 0;
            sim.LessonTaught += (taughtCard, result) =>
            {
                pulses++;
                Check(ReferenceEquals(taughtCard, card) && result.truth == truth && result.correct,
                    "Lesson signal carries the answered card and successful result");
                Check(sim.Samples("spam") > samplesBefore, "Lesson signal occurs after learning");
            };
            bool evaluation = accurateEvaluation ? guess == truth : guess != truth;
            Check(sim.TryEvaluateTeachingGuess("spam", card.id, guess, evaluation, wallet, out var answer),
                "Current teaching evaluation is consumed");
            Check(answer.correct == accurateEvaluation && card.truth == truth,
                "Evaluation resolves to the intended label without changing ground truth");
            Check(answer.corrected == (accurateEvaluation && guess != truth),
                "Only a real correction receives correction credit");
            Check(sim.S.handCorrect == (accurateEvaluation ? 1 : 0) && sim.S.handWrong == (accurateEvaluation ? 0 : 1),
                "Exactly one manual answer is settled");
            Check((sim.Samples("spam") > samplesBefore) == accurateEvaluation && pulses == (accurateEvaluation ? 1 : 0),
                "Wrong evaluations neither teach nor emit a success pulse");
            Check(sim.S.teachingCorrections == (accurateEvaluation && guess != truth ? 1 : 0),
                "Correction count matches the actual lesson");
            if (answer.corrected)
                Check(sim.S.recentCorrections.Count == 1 && sim.S.recentCorrections[0].truth == truth,
                    "Correction memory retains the true answer");
            Check(sim.Card("spam").id != card.id, "Consumed evaluation advances the card");
            double moneyAfter = wallet.Money;
            Check(!sim.TryEvaluateTeachingGuess("spam", card.id, guess, evaluation, wallet, out _)
                && wallet.Money == moneyAfter && sim.S.handCorrect + sim.S.handWrong == 1 && pulses == (accurateEvaluation ? 1 : 0),
                "Duplicate click on an old card is ignored");
        }

        var stale = Fixture(false, true, out var staleCard);
        var staleWallet = new Wallet();
        int stalePulses = 0;
        stale.LessonTaught += (_, __) => stalePulses++;
        Check(!stale.TryEvaluateTeachingGuess("spam", staleCard.id, true, false, staleWallet, out _),
            "Outdated displayed prediction is rejected");
        Check(stale.Card("spam").id == staleCard.id && staleWallet.Money == 100
            && stale.S.handCorrect + stale.S.handWrong == 0 && stale.Samples("spam") == 0 && stalePulses == 0,
            "Stale prediction cannot charge, teach, advance or pulse");
        stale.S.teachMode = false;
        Check(!stale.TryEvaluateTeachingGuess("spam", staleCard.id, false, false, staleWallet, out _),
            "Ordinary label mode does not evaluate guesses");
        var ordinary = stale.Answer("spam", true, staleWallet);
        Check(ordinary.correct && stalePulses == 0, "Ordinary true/false answer retains its existing semantics");

        var review = Fixture(false, true, out var reviewCard);
        var reviewWallet = new Wallet();
        int reviewPulses = 0;
        review.LessonTaught += (_, __) => reviewPulses++;
        review.S.cards.Remove(reviewCard);
        reviewCard.hasJudgment = true;
        reviewCard.guess = false;
        review.S.queue.Add(reviewCard);
        Check(ReferenceEquals(review.ReviewCard("spam"), reviewCard)
            && !review.TryEvaluateTeachingGuess("spam", reviewCard.id, false, false, reviewWallet, out _),
            "A card now awaiting review cannot consume a teaching evaluation");
        Check(review.S.queue.Contains(reviewCard) && reviewWallet.Money == 100 && review.Samples("spam") == 0
            && review.S.handCorrect + review.S.handWrong == 0 && reviewPulses == 0,
            "Rejected review evaluation leaves queue and learning untouched");
        var reviewed = review.AnswerQueued(reviewCard.id, true, reviewWallet);
        Check(reviewed.accepted && reviewed.queued && reviewed.correct && reviewPulses == 0,
            "Review still accepts a direct true label, not a guess evaluation");

        var withoutBoard = Fixture(false, true, out var withoutBoardCard);
        withoutBoard.UseBoard = false;
        var withoutBoardWallet = new Wallet();
        Check(!withoutBoard.TryEvaluateTeachingGuess("spam", withoutBoardCard.id, false, false, withoutBoardWallet, out _)
            && withoutBoardWallet.Money == 100 && withoutBoard.S.handCorrect + withoutBoard.S.handWrong == 0,
            "Disabled board cannot consume a teaching evaluation");

        var timed = Fixture(false, true, out var timedCard);
        timedCard.timeLimit = 1;
        timedCard.age = 2;
        int timedPulses = 0;
        timed.LessonTaught += (_, __) => timedPulses++;
        Check(timed.TryEvaluateTeachingGuess("spam", timedCard.id, false, false, new Wallet(), out var timeout)
            && timeout.timeout && !timeout.correct && timed.S.handWrong == 1,
            "Expired lesson is settled as timeout even with an accurate evaluation");
        Check(timed.Samples("spam") == 0 && timed.S.teachingCorrections == 0 && timedPulses == 0
            && timed.Card("spam").id != timedCard.id, "Timed-out lesson advances without learning or pulsing");

        foreach (string special in new[] { "family", "law", "shutdown" })
        {
            var sim = Fixture(false, true, out var card);
            card.family = special == "family";
            card.law = special == "law";
            if (special == "shutdown") card.kind = "shutdown";
            Check(!sim.TryEvaluateTeachingGuess("spam", card.id, false, false, new Wallet(), out _),
                "Special card keeps its own answer semantics: " + special);
        }
        foreach (bool junk in new[] { false, true })
        foreach (bool truth in new[] { false, true })
        foreach (bool teachMode in new[] { false, true })
        {
            var sim = Fixture(false, truth, out var familyCard);
            familyCard.family = true; familyCard.from = "妈妈";
            sim.S.teachMode = teachMode;
            sim.S.combo = 4; sim.S.modelStreak = 2;
            var before = new XgFamilyAttitude { sender = familyCard.from, junk = 3, important = 5 };
            sim.S.familyAttitudes.Add(before);
            int pulses = 0;
            sim.LessonTaught += (_, __) => pulses++;
            var familyAnswer = sim.Answer("spam", junk, new Wallet());
            Check(familyAnswer.accepted && familyAnswer.subjective && familyAnswer.fine == 0
                && before.junk == 3 + (junk ? 1 : 0) && before.important == 5 + (junk ? 0 : 1),
                "Family choice updates only the chosen attitude, independent of truth and teaching mode");
            Check(sim.S.combo == 4 && sim.S.modelStreak == 2 && sim.Samples("spam") == 0
                && sim.S.handCorrect + sim.S.handWrong == 0 && pulses == 0,
                "Family opinion preserves accuracy, streaks and teaching events");
        }
        Lesson004PredictionRecoveryChecks.Run();
        Lesson007SpecialCardChecks.Run();
        RunInputDispatch();
        RunSenseDeskProgression();
        RunFirstMinuteFocus();
        Debug.Log("DESIGN_FIRST_LESSON_CHECKS_PASS");
    }

    static void RunSenseDeskProgression()
    {
        var fresh = new XgSim(); var wallet = new Wallet();
        Check(fresh.S.desk == XgMemes.SenseDesk && fresh.S.sequence.dataset == XgMemes.SenseDesk
            && !fresh.DeskOpen("arith") && !fresh.ProgressionDeskAvailable("arith"),
            "Fresh stage one starts on common sense and keeps arithmetic locked");
        fresh.SelectDesk("arith"); fresh.Tick(.1, wallet);
        Check(fresh.S.desk == XgMemes.SenseDesk && !fresh.DeskOpen("arith"),
            "Selecting or ticking a fresh stage-one save cannot open arithmetic");
        var reloaded = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(fresh.S)));
        Check(reloaded.S.desk == XgMemes.SenseDesk && !reloaded.DeskOpen("arith"),
            "New stage-one save reload does not re-add arithmetic through repair");
        reloaded.S.stage = 2; reloaded.Tick(.1, wallet);
        Check(reloaded.DeskOpen("arith") && reloaded.ProgressionDeskAvailable("arith")
            && reloaded.DeskConditionText(XgCatalog.Desk("arith")) == "",
            "Arithmetic opens automatically at stage two without buying its data pack");
        foreach (string desk in new[] { "spam", "arith" })
        {
            var old = new XgSim();
            if (!old.S.desksOpen.Contains("arith")) old.S.desksOpen.Add("arith");
            old.SelectDesk(desk);
            old.S.labels.Add(new XgLabelCount { dataset = "arith", count = 7 });
            long cardId = old.Card(desk).id;
            var restored = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(old.S)));
            Check(restored.S.stage == 1 && restored.S.desk == desk && restored.DeskOpen("arith")
                && restored.ProgressionDeskAvailable("arith") && restored.Labels("arith") == 7
                && restored.Card(desk).id == cardId,
                "Existing stage-one save preserves selected desk, arithmetic access, labels and card: " + desk);
        }
    }
    static void RunFirstMinuteFocus()
    {
        var window = new LingGuangV05.Desktop.XingGuang.XgFirstLessonFocus();
        var fresh = new XgSim();
        var other = new XgSim();
        Check(!window.ActiveFor(fresh, 0), "An ordinary loaded save does not start first-minute protection");
        window.Arm(fresh);
        Check(window.ActiveFor(fresh, 500), "Setup protection waits for a presented lesson, not boot time");
        window.Presented(fresh, 500);
        window.Presented(fresh, 545);
        window.Arm(fresh);
        fresh.S.handCorrect = 4;
        fresh.S.epochs = 1;
        Check(window.ActiveFor(fresh, 559.999), "Four quick lessons, training and refreshes keep the original first minute");
        Check(!window.ActiveFor(fresh, 560), "Protection expires at sixty seconds from first presentation");
        window.Presented(fresh, 600);
        Check(!window.ActiveFor(fresh, 600), "Later card presentation cannot restart the expired minute");
        window.Arm(other);
        window.Presented(other, 700);
        Check(window.ActiveFor(other, 710), "A separate newly completed setup has its own minute");
        Check(!window.ActiveFor(fresh, 711), "Changing simulations clears the previous session protection");
        Check(!window.ActiveFor(other, 712), "Returning to an old simulation does not resurrect its protection");
        Debug.Log("DESIGN_FIRST_MINUTE_FOCUS_CHECKS_PASS");
    }

    // Event component checks use disposable objects; actual desktop event routing still needs scene acceptance.
    static void RunInputDispatch()
    {
        var eventObject = new GameObject("First lesson isolated event data");
        eventObject.SetActive(false);
        var events = eventObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var target = new GameObject("First lesson isolated button", typeof(RectTransform));
        try
        {
            var button = target.AddComponent<UnityEngine.UI.Button>();
            var input = target.AddComponent<LingGuangV05.Desktop.XingGuang.XgLessonAnswerInput>();
            int version = 1, calls = 0, received = -1;
            input.Bind(button, () =>
            {
                int captured = version;
                return () => { received = captured; calls++; };
            });
            var pointer = new UnityEngine.EventSystems.PointerEventData(events)
                { pointerId = 1, button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
            input.OnPointerDown(pointer);
            Check(calls == 0, "Pointer-down captures without submitting");
            version = 2;
            input.OnPointerClick(pointer);
            Check(calls == 1 && received == 1, "Release submits the captured context, never a newer display");
            input.OnPointerClick(pointer);
            Check(calls == 1, "Pointer click consumes its capture only once");
            input.OnSubmit(new UnityEngine.EventSystems.BaseEventData(events));
            Check(calls == 2 && received == 2, "Submit captures the current context once");
            pointer.button = UnityEngine.EventSystems.PointerEventData.InputButton.Right;
            input.OnPointerDown(pointer); input.OnPointerClick(pointer);
            Check(calls == 2, "Right click cannot evaluate a lesson");
            pointer.button = UnityEngine.EventSystems.PointerEventData.InputButton.Left;
            button.interactable = false;
            input.OnPointerDown(pointer); input.OnSubmit(new UnityEngine.EventSystems.BaseEventData(events));
            button.interactable = true;
            input.OnPointerClick(pointer);
            Check(calls == 2, "Disabled input cannot leave a deferred answer");
            input.OnPointerDown(pointer);
            target.SetActive(false); target.SetActive(true);
            input.OnPointerClick(pointer);
            Check(calls == 2, "Disable clears a pending press before reuse");
            input.OnPointerDown(pointer);
            var other = new UnityEngine.EventSystems.PointerEventData(events)
                { pointerId = 2, button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
            input.OnPointerClick(other);
            Check(calls == 2, "Another pointer cannot consume the pending press");
            input.OnPointerClick(pointer);
            Check(calls == 3, "The captured pointer can still complete one answer");
            input.OnPointerDown(pointer);
            input.OnCancel(new UnityEngine.EventSystems.BaseEventData(events));
            input.OnPointerClick(pointer);
            Check(calls == 3, "Cancel discards a captured answer");
            input.OnPointerDown(pointer);
            target.SendMessage("OnApplicationFocus", false);
            input.OnPointerClick(pointer);
            Check(calls == 3, "Losing application focus discards a captured answer");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(eventObject);
        }
    }
}

// Integrated lesson-004: real model changes, stale input rejection and recovery.
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

// Integrated lesson-007: special card generation and direct-answer compatibility.
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

        }
        UnityEngine.Debug.Log("LESSON_007_SPECIAL_CARD_CHECKS_PASS");
    }
}
