#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

// Opt-in isolated EditMode checks. This file is an entry point, not evidence that checks ran.
public static class VocabularyPreparationChecks
{
    static readonly BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
    static readonly BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
    static int assertions;
    static readonly StringBuilder report = new StringBuilder();
    static Dictionary<string, HashSet<string>> Vocab => (Dictionary<string, HashSet<string>>)typeof(XgBoardData).GetField("vocabularies", StaticPrivate).GetValue(null);
    static Dictionary<string, HashSet<string>> Train => (Dictionary<string, HashSet<string>>)typeof(XgBoardData).GetField("trainSignatures", StaticPrivate).GetValue(null);

    [MenuItem("LingGuang/Verification/Vocabulary preparation cold warm cancellation")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use an isolated EditMode project");
        assertions = 0; report.Clear();
        var vocabSnapshot = new Dictionary<string, HashSet<string>>(Vocab);
        var trainSnapshot = new Dictionary<string, HashSet<string>>(Train);
        var originalVocabulary = (Func<string, HashSet<string>>)Delegate.CreateDelegate(typeof(Func<string, HashSet<string>>), typeof(XgBoardData).GetMethod("Vocabulary", StaticPrivate));
        try
        {
            // Incremental path goes first, so its first-unit timing is not warmed by our reference.
            foreach (string dataset in new[] { "spam", "review", "headline", "sense" })
            {
                Reset(dataset);
                var cold = new XgTestSetPreparation(dataset, 3, 20161018, 37);
                double firstMs = Time(() => cold.Advance(1));
                Check(cold.TrainingGenerated == 0 && cold.Result == null && !Vocab.ContainsKey(dataset), "First vocabulary unit published/generated cards: " + dataset);
                double maxMs; int units;
                double totalMs = Finish(cold, out maxMs, out units);
                var incrementalVocabulary = Vocab[dataset];
                report.AppendLine(dataset + " incremental cache-cold first Advance(1) ms=" + firstMs + " remaining total ms=" + totalMs + " max remaining Advance(1) ms=" + maxMs + " units=" + (units + 1));

                // The production original synchronous function is retained unchanged. Compare both
                // complete membership AND enumeration order against it, never against a rewritten oracle.
                Vocab.Remove(dataset);
                HashSet<string> original = null;
                double originalCold = Time(() => original = originalVocabulary(dataset));
                EqualWords(original, incrementalVocabulary, dataset);
                HashSet<string> hot = null;
                double originalWarm = Time(() => hot = originalVocabulary(dataset));
                Check(ReferenceEquals(original, hot), "Original vocabulary lost singleton identity");
                report.AppendLine(dataset + " original Vocabulary cache-cold ms=" + originalCold + " warm ms=" + originalWarm + " words=" + original.Count);

                // Use actual original TestSet for cold and hot content/timing; reset both caches first.
                Reset(dataset);
                List<XgBoardCard> expected = null;
                double testCold = Time(() => expected = XgBoardData.TestSet(dataset, 3, 20161018, 37));
                Compare(expected, cold.Result, dataset + " complete cold preparation versus actual TestSet");
                List<XgBoardCard> repeated = null;
                double testWarm = Time(() => repeated = XgBoardData.TestSet(dataset, 3, 20161018, 37));
                Compare(expected, repeated, dataset + " actual TestSet hot");
                var warm = new XgTestSetPreparation(dataset, 3, 20161018, 37);
                double warmMax; int warmUnits;
                double warmTotal = Finish(warm, out warmMax, out warmUnits);
                Compare(expected, warm.Result, dataset + " warm preparation");
                report.AppendLine(dataset + " actual TestSet cache-cold ms=" + testCold + " warm ms=" + testWarm + " warm preparation total ms=" + warmTotal + " max Advance(1) ms=" + warmMax + " units=" + warmUnits);
            }
            CheckRouting();
            CheckCancellation();
            CheckPartialSynchronousFallback();
            report.AppendLine("PASS " + assertions + " assertions. Cache-cold does not imply a fresh managed domain; use First Pick entry separately after domain reload. Timing is diagnostic, not gameplay/frame acceptance.");
            string path = Path.Combine(Path.GetTempPath(), "LingGuang-Vocabulary-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(path, report.ToString());
            UnityEngine.Debug.Log("VOCABULARY_PREPARATION_PASS " + path + "\n" + report);
        }
        finally
        {
            Vocab.Clear(); foreach (var pair in vocabSnapshot) Vocab.Add(pair.Key, pair.Value);
            Train.Clear(); foreach (var pair in trainSnapshot) Train.Add(pair.Key, pair.Value);
        }
    }

    // Run FIRST after a domain reload, separately from Run. The CLR may eagerly initialize a
    // beforefieldinit type; explicitly request its initializer before the first timed Pick.
    [MenuItem("LingGuang/Verification/Vocabulary first Pick initialization timing")]
    public static void MeasureFirstPick()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use an isolated EditMode project");
        double init = Time(() => System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(XgMemes).TypeHandle));
        var r = new System.Random(37);
        double first = Time(() => XgMemes.Pick("spam", r, XgMemes.MaxLevel, 20161231, ""));
        double second = Time(() => XgMemes.Pick("spam", r, XgMemes.MaxLevel, 20161231, ""));
        UnityEngine.Debug.Log("VOCABULARY_FIRST_PICK type initializer request ms=" + init + " first Pick ms=" + first + " second Pick ms=" + second + ". Initializer timing is cold only if this type was not initialized earlier in this domain.");
    }

    static void CheckRouting()
    {
        foreach (string dataset in new[] { "xor", "parallel", "longtext", "crosssentence", "translate", "mnist", "poems", "news", "logic", "arith", "cifar", "imagenet", "meme", "go", "danmu" })
        {
            Reset(dataset);
            var job = new XgTestSetPreparation(dataset, 1, 20161018, 37); job.Advance(1);
            Check(job.TrainingGenerated == 1, "Non-Words desk incorrectly did vocabulary work: " + dataset);
            Check(!Vocab.ContainsKey(dataset), "Non-Words desk populated vocabulary: " + dataset);
        }
        Reset("spam");
        var cold = new XgTestSetPreparation("spam", 1, 20161018, 37);
        cold.Advance(0); Check(cold.TrainingGenerated == 0 && !Vocab.ContainsKey("spam"), "Zero quota performed work");
        for (int i = 0; i < 4000; i++) cold.Advance(1);
        Check(cold.TrainingGenerated == 0 && !Vocab.ContainsKey("spam"), "Published before all 4000 picks and bigram counting completed");
    }

    static void CheckCancellation()
    {
        foreach (int progress in new[] { 1, 3999, 4000, 4001 })
        {
            Reset("spam");
            var sim = new XgSim(); sim.Today = 20161018;
            string before = JsonUtility.ToJson(sim.S);
            sim.PrepareTestSet("spam", progress);
            Check(JsonUtility.ToJson(sim.S) == before, "Preparation changed serialized state");
            Check(!Vocab.ContainsKey("spam"), "Fixture expected private incomplete vocabulary at " + progress);
            sim.CancelTestSetPreparation();
            Check(typeof(XgSim).GetField("pendingTestSet", InstancePrivate).GetValue(sim) == null, "Cancellation retained owner");
            Check(!Vocab.ContainsKey("spam"), "Cancellation published partial vocabulary");
            var actual = sim.TestSet("spam");
            Compare(XgBoardData.TestSet("spam", sim.BoardLevel("spam"), sim.Today, sim.S.dataSalt), actual, "cancel then original fallback " + progress);
        }
        Reset("spam"); Reset("review");
        var owner = new XgSim(); owner.Today = 20161018;
        owner.PrepareTestSet("spam", 37); owner.PrepareTestSet("review", 1);
        Check(!Vocab.ContainsKey("spam") && !Vocab.ContainsKey("review"), "Dataset switch published partial vocabulary");
        var pending = (XgTestSetPreparation)typeof(XgSim).GetField("pendingTestSet", InstancePrivate).GetValue(owner);
        Check(pending.Dataset == "review", "Dataset switch retained previous job");
        var other = new XgSim();
        Check(typeof(XgSim).GetField("pendingTestSet", InstancePrivate).GetValue(other) == null, "Private pending work leaked across owners");
        owner.CancelTestSetPreparation();
        // Cancelling after vocabulary completion may keep the immutable complete singleton.
        Reset("spam"); owner = new XgSim(); owner.Today = 20161018;
        int guard = 0;
        while (!Vocab.ContainsKey("spam")) { owner.PrepareTestSet("spam", 1); if (++guard > 30000) throw new Exception("Vocabulary never published"); }
        var complete = Vocab["spam"];
        owner.CancelTestSetPreparation();
        Check(ReferenceEquals(complete, Vocab["spam"]), "Cancellation replaced finished vocabulary");
        var method = (Func<string, HashSet<string>>)Delegate.CreateDelegate(typeof(Func<string, HashSet<string>>), typeof(XgBoardData).GetMethod("Vocabulary", StaticPrivate));
        Vocab.Remove("spam"); EqualWords(method("spam"), complete, "cancel after complete vocabulary");
    }

    static void CheckPartialSynchronousFallback()
    {
        Reset("review");
        var pending = new XgTestSetPreparation("review", 3, 20161018, 913); pending.Advance(37);
        Check(!Vocab.ContainsKey("review"), "Partial vocabulary visible before fallback");
        var expected = XgBoardData.TestSet("review", 3, 20161018, 913);
        var singleton = Vocab["review"];
        double max; int units; Finish(pending, out max, out units);
        Check(ReferenceEquals(singleton, Vocab["review"]), "Resumed partial job replaced synchronous singleton");
        Compare(expected, pending.Result, "synchronous fallback during private vocabulary work");
        // Larger quotas and exam purpose retain exact final cards too.
        foreach (int chunk in new[] { 7, 32, 4096 })
        {
            Reset("headline");
            var job = new XgTestSetPreparation("headline", 2, 20161101, 913, XgBoardData.Use.Exam);
            int guard = 0; while (!job.Complete) { job.Advance(chunk); if (++guard > 30000) throw new Exception("Chunk fixture never completed"); }
            Compare(XgBoardData.TestSet("headline", 2, 20161101, 913, XgBoardData.Use.Exam), job.Result, "exam chunk " + chunk);
        }
    }

    static void Reset(string dataset)
    {
        Vocab.Remove(dataset);
        foreach (string key in new List<string>(Train.Keys)) if (key.StartsWith(dataset + "|", StringComparison.Ordinal)) Train.Remove(key);
    }
    static double Finish(XgTestSetPreparation job, out double maxMs, out int units)
    {
        maxMs = 0; units = 0; long started = Stopwatch.GetTimestamp();
        while (!job.Complete)
        {
            double ms = Time(() => job.Advance(1)); maxMs = Math.Max(maxMs, ms);
            if (++units > 30000) throw new Exception("Preparation never completed");
        }
        return (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;
    }
    static double Time(Action action) { long start = Stopwatch.GetTimestamp(); action(); return (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency; }
    static void EqualWords(HashSet<string> expected, HashSet<string> actual, string label)
    {
        Check(expected.SetEquals(actual), label + " vocabulary membership differs");
        var a = new List<string>(expected); var b = new List<string>(actual);
        for (int i = 0; i < a.Count; i++) Check(a[i] == b[i], label + " vocabulary enumeration differs at " + i);
    }
    static void Compare(List<XgBoardCard> expected, List<XgBoardCard> actual, string label)
    {
        Check(expected.Count == actual.Count && actual.Count == 400, label + " card count differs");
        for (int i = 0; i < expected.Count; i++) Check(Card(expected[i]) == Card(actual[i]), label + " full ordered card differs at " + i);
    }
    static string Card(XgBoardCard card)
    {
        // XgBoardCard is not [Serializable]; do not trust JsonUtility on the outer card.
        var value = new StringBuilder();
        value.Append(card.region).Append('|').Append(card.truth).Append('|').Append(card.seed).Append('|').Append(card.distance).Append('|').Append(card.text);
        foreach (var f in card.features) value.Append('\n').Append(f.name).Append('|').Append(f.x).Append('|').Append(f.y).Append('|').Append(f.seq).Append('|').Append(f.made);
        value.Append("\nSOURCE:").Append(card.source == null ? "null" : JsonUtility.ToJson(card.source));
        return value.ToString();
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); assertions++; }
}
#endif
