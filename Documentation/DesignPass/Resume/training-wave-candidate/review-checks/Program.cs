using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LingGuangV05.XingGuang;

static class Program
{
    static int assertions;
    static readonly JsonSerializerOptions Json = new JsonSerializerOptions { IncludeFields = true, IgnoreReadOnlyProperties = true };
    static string Snapshot<T>(T value) => JsonSerializer.Serialize(value, Json);
    static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        assertions++;
        Console.WriteLine("PASS " + message);
    }
    static int Main(string[] args)
    {
        try
        {
            if (args.Contains("--force-fail")) throw new InvalidOperationException("INTENTIONAL_PROTECTED_FAILURE");
            Equivalence();
#if TRAINING_TRANSITIONS
            CreationAndDecay();
            MergeSampling();
            RemovalKinds();
#endif
            Console.WriteLine("TRAINING_WAVE_CORE_PASS assertions=" + assertions);
            return 0;
        }
        catch (Exception error)
        {
            // A failed test returns a nonzero exit without an unhandled-exception dialog.
            try { Console.Error.WriteLine("CAUGHT_TEST_FAILURE: " + error); } catch { }
            return 1;
        }
    }

    static void Equivalence()
    {
        // Compare this exact scenario's digest between the production XgBoard and candidate builds.
        var history = new StringBuilder();
        var board = new XgBoard();
#if TRAINING_TRANSITIONS
        int notifications = 0;
        board.ConceptsChanged += changes => notifications += changes.Count;
#endif
        for (int i = 0; i < 256; i++)
        {
            var knobs = new XgKnobs { depth = 1 + i % 3, width = i % 11 == 0 ? 2 : 24,
                activation = XgActivation.Relu, dropout = i % 5 == 0, clip = i % 7 == 0, lr = .08 + .04 * (i % 4) };
            var card = new XgBoardCard { region = i % 3 == 0 ? "vision" : "logic", seed = 3000 + i, truth = i % 3 != 1 }
                .Add("a" + i % 7).Add("b" + i % 5).Add("c" + i % 3);
            history.Append(Snapshot(board.Train(card, knobs))).Append(Snapshot(board.S));
            history.Append(board.Predict(card, knobs, out bool guessed)).Append(guessed);
            if (i % 29 == 0) { board.Idle(60); history.Append(Snapshot(board.S)); }
            if (i == 127) { board.Reinitialise("vision"); history.Append(Snapshot(board.S)); }
        }
        Check(board.S.cards == 256, "Both builds complete the same 256 real training cards");
#if TRAINING_TRANSITIONS
        Check(notifications > 0, "The candidate equivalence workload has a live passive observer");
#endif
        var sim = new XgSim(); var wallet = new Wallet();
#if TRAINING_TRANSITIONS
        sim.Board.ConceptsChanged += changes => notifications += changes.Count;
#endif
        sim.SelectDesk("sense");
        for (int i = 0; i < 5 && sim.Labels("sense") < 4; i++)
        {
            var card = sim.Card("sense");
            Check(sim.TeachingGuess(card, out bool guess)
                && sim.TryEvaluateTeachingGuess("sense", card.id, guess, guess == card.truth, wallet, out var result)
                && result.correct, "Real teaching judgments prepare the equivalence model: " + i);
        }
        Check(sim.Labels("sense") >= 4, "Real lesson supplies training samples");
        sim.SetDataset(XgTrack.Sequence, "sense");
        for (int i = 0; i < 4; i++)
        {
            Check(sim.TrainEpoch(XgTrack.Sequence, wallet) != null, "Actual synchronous training epoch completes: " + i);
            history.Append(Snapshot(sim.S)).Append(Snapshot(new[] { wallet.Money, wallet.Compute, wallet.VramMB }));
        }
        int starts = 0;
        sim.EpochStarted += epoch => starts++;
        Check(sim.BeginEpoch(XgTrack.Sequence, wallet) && starts == 1, "An accepted production start emits exactly one epoch event");
        Check(!sim.BeginEpoch(XgTrack.Sequence, wallet) && starts == 1, "An already active epoch cannot fabricate another training start");
        history.Append(Snapshot(sim.S)).Append(Snapshot(new[] { wallet.Money, wallet.Compute, wallet.VramMB }));
        Console.WriteLine("EQUIVALENCE_SHA256=" + Hash(history.ToString()));
    }

#if TRAINING_TRANSITIONS
    static void CreationAndDecay()
    {
        var state = new XgBoardState { cards = XgBoard.DecayEvery - 1, nextId = 10 };
        state.concepts.Add(Cell(1, "old", .01, .049));
        var pinned = Cell(2, "pinned", .01, .049); pinned.pinned = true; state.concepts.Add(pinned);
        var seed = Cell(3, "seed", .01, .049); seed.seed = true; state.concepts.Add(seed);
        var board = new XgBoard(state);
        IReadOnlyList<XgConceptTransition> saved = null; int batches = 0;
        board.ConceptsChanged += changes => { batches++; saved = changes; };
        var knobs = new XgKnobs { depth = 1, width = 16, lr = .1 };
        board.Train(new XgBoardCard { seed = 31, truth = true }.Add("new"), knobs);
        Check(batches == 1 && saved != null, "One train publishes its actual creation and removal batch once");
        var born = saved.Single(c => c.Kind == XgConceptTransitionKind.ElementBorn);
        var dead = saved.Single(c => c.Kind == XgConceptTransitionKind.Decayed);
        Check(born.Weight == 0 && born.Strength == 1 && born.ParentA == -1 && born.ParentB == -1,
            "R1 evidence describes the actual newborn before R1 pulls, with no invented parents");
        double factor = Math.Pow(1 - XgBoard.Decay, XgBoard.DecayEvery);
        Check(dead.Id == 1 && Near(dead.Weight, .01 * factor) && Near(dead.Strength, .049 * factor)
            && !state.concepts.Any(c => c.id == dead.Id), "R4 evidence samples after decay and before actual removal");
        Check(state.concepts.Any(c => c.id == 2) && state.concepts.Any(c => c.id == 3)
            && saved.All(c => c.Id != 2 && c.Id != 3), "Pinned and seed concepts are never reported as R4 deaths");
        var live = state.concepts.Single(c => c.id == born.Id);
        Check(live.w != born.Weight && live.s != born.Strength, "Newborn evidence does not turn into the later trained/decayed values");
        string preserved = JsonSerializer.Serialize(saved);
        live.w = -3; live.s = 8;
        Check(JsonSerializer.Serialize(saved) == preserved, "Captured records stay immutable when their original concepts change");
        var list = saved as IList<XgConceptTransition>;
        bool readonlyList = false;
        try { list[0] = list[0]; } catch (NotSupportedException) { readonlyList = true; }
        Check(readonlyList, "Published batch cannot be rewritten through IList");
    }

    static void MergeSampling()
    {
        var state = new XgBoardState { cards = XgBoard.DecayEvery - 1, nextId = 3 };
        state.concepts.Add(Cell(1, "a", .2, 2)); state.concepts.Add(Cell(2, "b", -.1, 2));
        state.links.Add(new XgLink { a = 1, b = 2, c = XgBoard.LinkThreshold });
        var board = new XgBoard(state); var changes = new List<XgConceptTransition>();
        board.ConceptsChanged += batch => changes.AddRange(batch);
        board.Train(new XgBoardCard { seed = 41, truth = true }.Add("a").Add("b"),
            new XgKnobs { depth = 2, width = 16, activation = XgActivation.Relu, lr = .1 });
        var merged = changes.Single(c => c.Kind == XgConceptTransitionKind.Merged);
        var cell = state.concepts.Single(c => c.id == merged.Id);
        double factor = Math.Pow(1 - XgBoard.Decay, XgBoard.DecayEvery);
        double parentMean = (state.concepts.Single(c => c.id == 1).w + state.concepts.Single(c => c.id == 2).w) / factor / 2;
        Check(new HashSet<int> { merged.ParentA, merged.ParentB }.SetEquals(new[] { 1, 2 })
            && cell.key == "a+b", "R2 records the two actual parents that created its actual union concept");
        Check(Near(merged.Weight, parentMean) && merged.Strength == 1
            && Near(cell.w, merged.Weight * factor) && Near(cell.s, merged.Strength * factor),
            "R2 samples assigned parent-mean weight before that card's R4 decay");
    }

    static void RemovalKinds()
    {
        var state = new XgBoardState { cards = 100, nextId = 2 };
        state.concepts.Add(Cell(1, "old", .01, 1));
        var board = new XgBoard(state); var changes = new List<XgConceptTransition>(); int batches = 0;
        board.ConceptsChanged += batch => { batches++; changes.AddRange(batch); };
        board.Train(new XgBoardCard { seed = 51, truth = true }.Add("replacement"), new XgKnobs { depth = 1, width = 1 });
        Check(state.evicted == 1 && changes.Count == 1 && changes[0].Kind == XgConceptTransitionKind.ElementBorn,
            "R3 eviction reports only the genuine birth, never a false R4 death");
        int before = batches; board.Reinitialise(); board.Plant("logic", "protected", 1);
        Check(batches == before, "Structural reset and protected planting do not fabricate lifecycle events");

        var idleState = new XgBoardState { nextId = 2 };
        idleState.concepts.Add(Cell(1, "idle", .01, .049));
        var idle = new XgBoard(idleState); changes.Clear(); batches = 0;
        idle.ConceptsChanged += batch => { batches++; changes.AddRange(batch); };
        idle.Idle(0); idle.Idle(-1);
        Check(batches == 0, "Nonpositive idle time emits no event");
        idle.Idle(1);
        Check(batches == 1 && changes.Count == 1 && changes[0].Kind == XgConceptTransitionKind.Decayed
            && changes[0].Id == 1 && idleState.concepts.Count == 0
            && Near(changes[0].Weight, .01 * (1 - XgBoard.Decay)),
            "Idle uses the same actual R4 removal event with post-decay evidence");
        idle.Idle(1);
        Check(batches == 1, "Idle with no removed concepts emits no empty event");
        var absent = new XgBoard(idleState.Clone()); absent.Idle(1);
        Check(absent.S.concepts.Count == 0, "The idle path also works with no observer");
    }
    static XgConcept Cell(int id, string key, double weight, double strength)
        => new XgConcept { id = id, key = key, region = "logic", layer = 1, w = weight, s = strength, born = 0, seen = 0 };
    static bool Near(double a, double b) => Math.Abs(a - b) < 1e-12;
#endif
    sealed class Wallet : IXgHost
    {
        public double Money { get; private set; } = 10000;
        public double Compute => 1;
        public double VramMB => 6144;
        public string Blocker => null;
        public bool Spend(double amount) { if (amount < 0 || amount > Money) return false; Money -= amount; return true; }
        public void Earn(double amount) => Money += amount;
        public void Train(double seconds) { }
    }
}
