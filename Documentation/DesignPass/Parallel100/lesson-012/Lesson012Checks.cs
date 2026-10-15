using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using LingGuangV05.XingGuang;

// Standalone memory-only checks against production source. No Unity, host adapter or save I/O.
public static class Lesson012Checks
{
    sealed class Wallet : IXgHost
    {
        public double money = 100;
        public bool reject;
        public int earns, spends;
        public double Money => money;
        public double Compute => 1;
        public double VramMB => 6144;
        public string Blocker => null;
        public bool Spend(double amount)
        {
            spends++;
            if (reject || amount < 0 || amount > money) return false;
            money -= amount;
            return true;
        }
        public void Earn(double amount) { earns++; money += amount; }
        public void Train(double seconds) { throw new Exception("Manual evaluation must not bill training"); }
    }

    static int assertions;
    static void Check(bool yes, string message)
    {
        assertions++;
        if (!yes) throw new Exception(message);
    }
    static void Equal(double actual, double expected, string message)
        => Check(Math.Abs(actual - expected) < 1e-9, message + ": " + actual + " != " + expected);

    // Includes public NonSerialized fields such as board links; snapshots values, not references.
    static string Snapshot(object obj)
    {
        if (obj == null) return "null";
        if (obj is string text) return text.Length + ":" + text;
        var type = obj.GetType();
        if (type.IsPrimitive || type.IsEnum || obj is decimal)
            return Convert.ToString(obj, CultureInfo.InvariantCulture);
        if (obj is IEnumerable items)
            return "[" + string.Join(";", items.Cast<object>().Select(Snapshot)) + "]";
        return "{" + string.Join(";", type.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .OrderBy(f => f.Name).Select(f => f.Name + "=" + Snapshot(f.GetValue(obj)))) + "}";
    }

    static string Learning(XgSim sim) => Snapshot(new object[] {
        sim.S.labels, sim.S.dataExtra, sim.S.logs, sim.S.logsHand,
        sim.S.board, sim.S.recentCorrections, sim.S.teachingCorrections });

    static XgSim Fixture(bool guess, bool truth, bool logs, out XgCard card)
    {
        var sim = new XgSim();
        sim.GoldChance = 0;
        card = new XgCard { id = 90001, dataset = "spam", seed = guess ? 9138 : 9137,
            question = "恭喜您中奖，请点击领取", truth = truth, level = 1 };
        sim.S.cards.RemoveAll(c => c.dataset == "spam");
        sim.S.cards.Add(card);
        if (logs) sim.S.logs.Add(new XgLabelCount { dataset = "spam", count = 2 });
        // Full correction ring proves rejected/failed input neither appends nor evicts old memories.
        for (int i = 0; i < XgSim.CorrectionMemory; i++)
            sim.S.recentCorrections.Add(new XgTaughtCard { question = "old-" + i });
        Check(sim.TeachingGuess(card, out bool predicted) && predicted == guess, "fixture prediction");
        return sim;
    }

    static void Settlements()
    {
        foreach (bool guess in new[] { false, true })
        foreach (bool truth in new[] { false, true })
        foreach (bool accurate in new[] { false, true })
        foreach (bool logs in new[] { false, true })
        {
            var sim = Fixture(guess, truth, logs, out var card);
            var wallet = new Wallet();
            string before = Learning(sim), weights = Snapshot(sim.S.board);
            double samples = sim.Samples("spam"), income = sim.S.totalIncome;
            double spent = sim.S.totalSpent, fines = sim.S.handFines;
            long trained = sim.S.board.cards;
            double pay = sim.ManualPayFor("spam", 1) * (guess != truth ? 2 : 1);
            double fine = sim.HandFineFor("spam", 1);
            int pulses = 0;
            sim.LessonTaught += (_, __) => pulses++;
            bool evaluation = accurate ? guess == truth : guess != truth;
            Check(sim.TryEvaluateTeachingGuess("spam", card.id, guess, evaluation, wallet, out var answer), "consumed");
            Check(answer.correct == accurate && !answer.timeout, "outcome");
            Equal(wallet.Money, 100 + (accurate ? pay : -fine), "wallet delta");
            Equal(sim.S.totalIncome - income, accurate ? pay : 0, "income ledger");
            Equal(sim.S.totalSpent - spent, accurate ? 0 : fine, "spend ledger");
            Equal(sim.S.handFines - fines, accurate ? 0 : fine, "fine ledger");
            Check(wallet.earns == (accurate ? 1 : 0) && wallet.spends == (!accurate && fine > 0 ? 1 : 0), "wallet call count");
            Equal(answer.pay, accurate ? pay : 0, "reported pay");
            Equal(answer.fine, accurate ? 0 : fine, "reported fine");
            Equal(sim.Samples("spam") - samples, accurate ? (logs ? 4 : 1) : 0, "sample delta");
            Check(answer.samples == (accurate ? (logs ? 4 : 1) : 0), "reported samples");
            Check(sim.S.handCorrect == (accurate ? 1 : 0) && sim.S.handWrong == (accurate ? 0 : 1), "one outcome");
            Check(sim.S.board.cards == trained + (accurate ? 1 : 0), "board train count");
            Check(pulses == (accurate ? 1 : 0), "event count");
            if (!accurate) Check(Learning(sim) == before, "wrong evaluation leaves all learning unchanged");
            else
            {
                Check(Snapshot(sim.S.board) != weights && sim.S.board.concepts.Any(c => !c.seed && c.w != 0), "real concept weight changes");
                bool corrected = guess != truth;
                Check(sim.S.teachingCorrections == (corrected ? 1 : 0) && answer.corrected == corrected, "correction count");
                Check(sim.S.recentCorrections.Count == 20, "bounded memory");
                Check(sim.S.recentCorrections[0].question == (corrected ? "old-1" : "old-0"), "eviction only on correction");
                if (corrected) Check(sim.S.recentCorrections[19].truth == truth && sim.S.recentCorrections[19].seed == card.seed, "memory original card");
            }
            Check(!ReferenceEquals(sim.Card("spam"), card), "consumed card advances");
            string settled = Snapshot(sim.S), walletSettled = Snapshot(wallet);
            Check(!sim.TryEvaluateTeachingGuess("spam", card.id, guess, evaluation, wallet, out _), "duplicate rejected");
            Check(Snapshot(sim.S) == settled && Snapshot(wallet) == walletSettled, "duplicate has zero state or wallet effect");
        }
    }

    static void Rejections()
    {
        foreach (string reason in new[] { "id", "prediction", "host", "desk", "mode", "board", "queue" })
        {
            var sim = Fixture(false, true, true, out var card);
            var wallet = new Wallet();
            if (reason == "mode") sim.S.teachMode = false;
            if (reason == "board") sim.UseBoard = false;
            if (reason == "queue") sim.S.queue.Add(card);
            string before = Snapshot(sim.S), funds = Snapshot(wallet);
            int pulses = 0;
            sim.LessonTaught += (_, __) => pulses++;
            Check(!sim.TryEvaluateTeachingGuess(reason == "desk" ? "invalid-desk" : "spam",
                reason == "id" ? card.id - 1 : card.id, reason == "prediction", false,
                reason == "host" ? null : wallet, out _), reason + " rejection");
            Check(Snapshot(sim.S) == before && Snapshot(wallet) == funds && pulses == 0, reason + " no mutation");
        }
    }

    static void FineAndTimeBoundaries()
    {
        foreach (double money in new[] { 0.0, 0.001, 100.0 })
        foreach (bool reject in new[] { false, true })
        {
            var sim = Fixture(false, true, true, out var card);
            var wallet = new Wallet { money = money, reject = reject };
            string before = Learning(sim);
            double expected = reject ? 0 : Math.Min(money, sim.HandFineFor("spam", 1));
            Check(sim.TryEvaluateTeachingGuess("spam", card.id, false, true, wallet, out var answer), "wrong input consumed despite wallet refusal");
            Equal(wallet.Money, money - expected, "bounded deduction");
            Equal(answer.fine, expected, "actual fine only");
            Equal(sim.S.totalSpent, expected, "actual spending only");
            Equal(sim.S.handFines, expected, "actual fines only");
            Check(Learning(sim) == before && sim.S.handWrong == 1 && !answer.correct, "fine failure no learning");
        }
        foreach (double age in new[] { 1.0, 1.000001 })
        {
            var sim = Fixture(false, true, true, out var card);
            card.timeLimit = 1; card.age = age;
            var wallet = new Wallet();
            string before = Learning(sim);
            Check(sim.TryEvaluateTeachingGuess("spam", card.id, false, false, wallet, out var answer), "timed input consumed");
            Check(answer.timeout == (age > 1) && answer.correct == (age == 1), "strict timeout boundary");
            if (age > 1)
                Check(Learning(sim) == before && wallet.Money == 100 && wallet.spends == 0 && wallet.earns == 0
                    && answer.pay == 0 && answer.fine == 0 && sim.S.handWrong == 1, "timeout no partial settlement");
        }
    }

    public static void Main()
    {
        Settlements(); Rejections(); FineAndTimeBoundaries();
        Console.WriteLine("LESSON012_MEMORY_CHECKS_PASS assertions=" + assertions);
    }
}
