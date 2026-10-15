// STANDALONE FIXTURES ONLY: never copy this file into Assets.
// These test doubles exercise C# event ownership, not XgSim learning or Unity.
using System;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.XingGuang;

namespace LingGuangV05.XingGuang
{
    public sealed class XgCard { }
    public struct XgAnswer { public bool correct; public bool corrected; }
    public sealed class XgSim
    {
        public event Action<XgCard, XgAnswer> LessonTaught;
        public int Count { get { return LessonTaught == null ? 0 : LessonTaught.GetInvocationList().Length; } }
        public Action<XgCard, XgAnswer> Capture() { return LessonTaught; }
        public void Raise(XgCard c, XgAnswer a) { if (LessonTaught != null) LessonTaught(c, a); }
    }
}

public static class SubscriptionChecks
{
    static int assertions;
    static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    public static string Run()
    {
        assertions = 0;
        int pulses = 0, otherPulses = 0;
        var a = new XgSim(); var b = new XgSim(); var c = new XgCard();
        var result = new XgAnswer { correct = true, corrected = true };
        Action<XgCard, XgAnswer> other = (card, answer) => otherPulses++;
        a.LessonTaught += other;
        var binding = new XgLessonSubscription((card, answer) =>
        {
            Check(ReferenceEquals(card, c) && answer.correct && answer.corrected, "Original payload preserved");
            pulses++;
        });
        binding.Bind(null); binding.Bind(a); binding.Bind(a);
        Check(a.Count == 2, "Repeat binding adds exactly one owned handler");
        a.Raise(c, result); Check(pulses == 1 && otherPulses == 1, "Single delivery, other listener preserved");
        var stale = a.Capture();
        binding.Bind(b);
        Check(a.Count == 1 && b.Count == 1, "Switch physically removes old and adds new handler");
        a.Raise(c, result); Check(pulses == 1, "Old publisher is silent");
        b.Raise(c, result); Check(pulses == 2, "New publisher delivers immediately");
        binding.Bind(a); stale(c, result);
        Check(pulses == 2, "A-B-A rejects old captured invocation");
        stale = a.Capture(); binding.Bind(null);
        Check(a.Count == 1 && b.Count == 0, "Disable removes owned handler only");
        stale(c, result); Check(pulses == 2, "Disable suppresses captured invocation");
        binding.Bind(a); stale(c, result);
        Check(pulses == 2, "Re-enable cannot revive old invocation");
        a.Raise(c, result); Check(pulses == 3, "Re-enable binds once");
        stale = a.Capture(); binding.Dispose(); binding.Dispose(); stale(c, result);
        Check(a.Count == 1 && pulses == 3, "Destroy is idempotent and suppresses captured invocation");
        bool rejected = false;
        try { binding.Bind(b); } catch (ObjectDisposedException) { rejected = true; }
        Check(rejected && b.Count == 0, "Disposed owner cannot rebind");

        // Actual multicast dispatch: an earlier listener switches ownership while
        // the old publisher's invocation list is already being executed.
        var old = new XgSim(); var replacement = new XgSim();
        XgLessonSubscription reentrant = null;
        old.LessonTaught += (card, answer) => reentrant.Bind(replacement);
        reentrant = new XgLessonSubscription((card, answer) => pulses++);
        reentrant.Bind(old); old.Raise(c, result);
        Check(pulses == 3 && old.Count == 1 && replacement.Count == 1, "In-flight replacement rejects old callback");
        replacement.Raise(c, result); Check(pulses == 4, "Replacement's first event is delivered");
        reentrant.Dispose();
        return "Standalone event ownership checks passed: " + assertions + " assertions (fixtures, not Unity).";
    }
}
