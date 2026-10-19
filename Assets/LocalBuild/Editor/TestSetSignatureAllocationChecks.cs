#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

public static class TestSetSignatureAllocationChecks
{
    static int assertions;
    public static void RunAll()
    {
        TestSetPreparationChecks.RunAll();
        Run();
    }

    [MenuItem("LingGuang/Verification/Preparation signature equivalence and allocations")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use isolated EditMode verification");
        assertions = 0;
        var original = CultureInfo.CurrentCulture;
        try
        {
            var custom = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
            custom.NumberFormat.NegativeSign = "minus";
            foreach (var culture in new[] { CultureInfo.InvariantCulture, CultureInfo.GetCultureInfo("zh-CN"), CultureInfo.GetCultureInfo("ar-SA"), custom })
            {
                CultureInfo.CurrentCulture = culture;
                var job = new XgTestSetPreparation("logic", 1, 0, 17);
                var signature = Bind(job);
                foreach (var data in XgCatalog.Datasets)
                    for (int i = 0; i < 64; i++)
                    {
                        var card = XgBoardData.Make(data.id, XgBoardData.Seed(data.id, i, XgBoardData.Use.Diagnostic, 17), 1 + i % 4, 20161018);
                        Equal(card, signature, data.id + "/" + culture.Name + "/" + i);
                    }
                var edge = new XgBoardCard { region = null, truth = true };
                Equal(edge, signature, "empty/null region");
                edge.Add(null, int.MinValue, int.MaxValue, -1).Add("x@;,|\n", -5, 0, 9)
                    .Add("中文", 1, 2, 3).Add("中文", 1, 2, 3).Add("", 10, -2, 0);
                Equal(edge, signature, "extremes/null/duplicates/separators");
                edge.features.Reverse(); edge.truth = false; edge.region = "vision|";
                Equal(edge, signature, "reverse and false");
                edge.features.Clear();
                Equal(edge, signature, "scratch cleared after large card");
                var second = Bind(new XgTestSetPreparation("mnist", 1, 0, 31));
                Equal(edge.Add("second", -9), second, "separate owner");
                Equal(edge, signature, "first owner after second");
            }
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var cards = new List<XgBoardCard>();
            for (int i = 0; i < 128; i++) cards.Add(XgBoardData.Make(i % 2 == 0 ? "logic" : "mnist", i + 17, 3, 0));
            var worker = new XgTestSetPreparation("logic", 3, 0, 17);
            var optimized = Bind(worker);
            Func<XgBoardCard, string> reference = XgBoardData.Signature;
            Measure(cards, reference); Measure(cards, optimized); // JIT and scratch growth outside measurement.
            long before = Measure(cards, reference), after = Measure(cards, optimized);
            bool allocationCounterAvailable = before > 0 && after > 0;
            if (allocationCounterAvailable) Check(after < before, "No signature allocation reduction: " + before + " -> " + after);
            var completed = new XgTestSetPreparation("logic", 1, 0, 93);
            int guard = 0;
            while (!completed.Complete) { completed.Advance(4096); if (++guard > 50) throw new Exception("Completion timeout"); }
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Check(typeof(XgTestSetPreparation).GetField("signatureParts", flags).GetValue(completed) == null, "Retained parts after completion");
            Check(typeof(XgTestSetPreparation).GetField("signatureText", flags).GetValue(completed) == null, "Retained builder after completion");
            Debug.Log("TESTSET_SIGNATURE_EQUIVALENCE_PASS assertions=" + assertions + " measuredCalls=1024 originalBytes=" + before + " preparedBytes=" + after
                + " allocationCounter=" + (allocationCounterAvailable ? "available" : "UNAVAILABLE: no allocation reduction claim")
                + ". Isolated signature check; not whole-game frame or audio validation.");
        }
        finally { CultureInfo.CurrentCulture = original; }
    }
    static Func<XgBoardCard, string> Bind(XgTestSetPreparation job) =>
        (Func<XgBoardCard, string>)Delegate.CreateDelegate(typeof(Func<XgBoardCard, string>), job,
            typeof(XgTestSetPreparation).GetMethod("BuildSignature", BindingFlags.Instance | BindingFlags.NonPublic));
    static void Equal(XgBoardCard card, Func<XgBoardCard, string> signature, string label) =>
        Check(signature(card) == XgBoardData.Signature(card), "Signature differs: " + label);
    static long Measure(List<XgBoardCard> cards, Func<XgBoardCard, string> signature)
    {
        int consumed = 0;
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int pass = 0; pass < 8; pass++) foreach (var card in cards) consumed ^= signature(card).Length;
        long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
        GC.KeepAlive(consumed);
        return bytes;
    }
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); assertions++; }
}
#endif
