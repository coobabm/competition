using System;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;

// Only identity is needed by the production focus clock. This is not a simulator or UI test.
namespace LingGuangV05.XingGuang { public sealed class XgSim { } }

static class Program
{
    static int count;
    static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        count++;
        Console.WriteLine("PASS " + message);
    }

    static void Main()
    {
        var window = new XgFirstLessonFocus();
        var fresh = new XgSim();
        var other = new XgSim();
        Check(!window.ActiveFor(fresh, 0), "Existing saves are not armed");
        window.Arm(fresh);
        Check(window.ActiveFor(fresh, 500), "Setup waits for presentation");
        window.Presented(fresh, double.NaN);
        window.Presented(fresh, double.PositiveInfinity);
        Check(window.ActiveFor(fresh, 500), "Invalid timestamps do not consume pending protection");
        window.Presented(other, 0);
        window.Presented(fresh, 500);
        window.Presented(fresh, 545);
        window.Arm(fresh);
        Check(window.ActiveFor(fresh, 559.999), "Refresh and repeated arm do not shorten the minute");
        Check(!window.ActiveFor(fresh, 560), "Original first presentation determines expiry");
        window.Presented(fresh, 600);
        Check(!window.ActiveFor(fresh, 600), "Later cards do not restart protection");
        window.Arm(other);
        window.Presented(other, 700);
        Check(window.ActiveFor(other, 710), "A new setup has a separate minute");
        Check(!window.ActiveFor(fresh, 711), "Switching simulations clears the armed session");
        Check(!window.ActiveFor(other, 712), "Returning to a previous save does not revive protection");
        window.Arm(fresh);
        Check(!window.ActiveFor(null, 0), "Removing the simulation clears protection");
        Check(!window.ActiveFor(fresh, 1), "Cleared setup stays cleared");
        Console.WriteLine("FIRST_MINUTE_FOCUS_CHECKS_PASS assertions=" + count);
    }
}
