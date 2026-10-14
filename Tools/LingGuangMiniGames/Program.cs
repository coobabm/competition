using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
static class Program
{
    sealed class Host : IXgHost
    {
        public double Money { get; private set; } = 100000;
        public double Compute => 10;
        public double VramMB => 10000;
        public string Blocker { get; set; }
        public void Earn(double v) { Money += v; }
        public bool Spend(double v) { if (v < 0 || Money < v) return false; Money -= v; return true; }
        public void Train(double seconds) { Money -= seconds; }
    }
    static int passed;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    static void Main()
    {
        var host = new Host(); var sim = new XgSim();
        Check(XgMiniGames.All.Count == 23, "all 23 distinct games");
        var ids = new HashSet<string>(); foreach(var g in XgMiniGames.All) Check(ids.Add(g.Id), "unique " + g.Id);
        Check(sim.BeginMiniGame("unknown", host) == 0, "unknown blocked");
        Check(sim.BeginMiniGame("draw", host) == 0, "locked digit desk blocked");
        var ticket = sim.BeginMiniGame("slice", host); Check(ticket > 0, "initial spam playable");
        var before = sim.Labels("spam"); var money = host.Money;
        var r = sim.SubmitMiniGame(ticket, 1, true, host);
        Check(r.accepted && r.samples == 1 && sim.Labels("spam") == before + 1, "verified samples feed existing dataset");
        Check(host.Money > money, "shared host wallet paid");
        Check(!sim.SubmitMiniGame(ticket, 1, true, host).accepted, "duplicate event rejected");
        Check(!sim.SubmitMiniGame(ticket, 3, true, host).accepted, "sequence gap rejected");
        before = sim.Labels("spam"); money = host.Money;
        r = sim.SubmitMiniGame(ticket, 2, false, host);
        Check(r.accepted && r.samples == 0 && sim.Labels("spam") == before && host.Money == money, "mistakes grant no samples or money");
        sim.CancelMiniGame(ticket);
        Check(!sim.SubmitMiniGame(ticket, 3, true, host).accepted, "cancelled ticket rejected");
        var next = sim.BeginMiniGame("slice", host);
        Check(next != ticket && !sim.SubmitMiniGame(ticket, 3, true, host).accepted, "new session invalidates previous");
        Check(!new XgSim(sim.S).SubmitMiniGame(next, 1, true, host).accepted, "loaded save has no outstanding ticket");
        sim.CancelMiniGame(next); sim.S.miniGames = null;
        Check(sim.MiniGameRecord("slice").correct == 0, "old null-list save repaired");
        sim.S.qcFrozen = 100; sim.S.qcCredit = 0;
        next = sim.BeginMiniGame("slice", host);
        Check(next > 0 && sim.SubmitMiniGame(next, 1, true, host).accepted, "manual labels allowed during automatic freeze");
        sim.CancelMiniGame(next);
        var train = sim.BeginMiniGame("forgetax", host);
        before = sim.TotalLabels;
        r = sim.SubmitMiniGame(train, 1, true, host);
        Check(train > 0 && r.accepted && sim.TotalLabels == before, "forge does not mint data");
        Check(!sim.SubmitMiniGame(train, 2, true, null).accepted, "null host rejected");
        sim.CancelMiniGame(train);
        Check(sim.MiniGameBlocker("turing") != null, "late story stays gated");
        var wrong = new XgSim(); var prior = wrong.Card("spam").id;
        next = wrong.BeginMiniGame("slice", host); wrong.SubmitMiniGame(next, 1, true, host);
        Check(wrong.Card("spam").id == prior, "does not secretly answer unrelated story card");
        Check(sim.MiniGameSkill("slice") >= 0 && sim.MiniGameSkill("slice") <= 1, "skill bounded");
        Console.WriteLine(passed + " minigame assertions passed");
    }
}
