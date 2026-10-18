#if UNITY_EDITOR
using System;
using System.Reflection;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

public static class DataDownloadPaceChecks
{
    sealed class Wallet : IXgHost
    {
        public double Money => 1e9; public double Compute => 1; public double VramMB => 1e8; public string Blocker => null;
        public bool Spend(double n) => n >= 0 && n <= Money;
        public void Earn(double n) { } public void Train(double n) { }
    }
    static int checks;
    static void Need(bool ok, string reason) { if (!ok) throw new Exception("Download pace: " + reason); checks++; }
    static void Equal(double a, double b, string reason) => Need(Math.Abs(a - b) < 1e-7, reason);
    static void Tick(XgSim sim, double dt) => typeof(XgSim).GetMethod("TickDownloads", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(sim, new object[] { dt });
    static XgSim Reload(XgSim sim) => new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
    static XgSim BuyChess()
    {
        var sim = new XgSim(); sim.S.teachingFlow = false; sim.S.openingFocus = false;
        sim.S.stage = sim.S.stageVision = sim.S.stageSequence = 6;
        sim.S.unlocked.Add("train.curve"); sim.S.diskBonusMB = 1e9;
        Need(sim.BuyNode("pack.chess_mate1", new Wallet()), "real catalog purchase succeeds");
        Need(sim.S.downloadQueue.Count == 1 && sim.S.dataFiles.Count == 0, "small file does not arrive instantly");
        return sim;
    }
    [MenuItem("Tools/Local Verification/Data download pacing")]
    public static void Run()
    {
        checks = 0;
        foreach (double size in new[] { 1d, 10, 11, 100, 101, 999, 1000, 10000, 10001, 150000 })
        foreach (double speed in XgSim.BroadbandMBps)
        foreach (bool junk in new[] { false, true })
        {
            double duration = XgSim.PacedDownloadSeconds(size, speed, junk, false);
            Need(duration >= (size >= 1000 ? 15 : 2) && duration <= 30, "visible bounded duration size=" + size + " speed=" + speed);
            Need(duration <= XgSim.PacedDownloadSeconds(size, XgSim.BroadbandMBps[0], junk, false), "faster broadband never slows a file");
        }
        var sim = BuyChess();
        string id = sim.S.downloadQueue[0].fileId;
        double duration0 = sim.DownloadQueue()[0].secondsLeft;
        Equal(duration0, 3, "1 MB ordinary chess file uses three visible game seconds");
        Tick(sim, 1);
        Equal(sim.DownloadQueue()[0].progress, 1d / 3, "actual byte progress and duration agree");
        Equal(sim.Samples("chess"), 0, "download progress grants no sample");
        sim = Reload(sim);
        Need(sim.S.downloadQueue[0].fileId == id && sim.S.downloadQueue[0].paceVersion == 1, "new pace and stable ID survive reload");
        Equal(sim.DownloadQueue()[0].secondsLeft, 2, "new pace remaining time survives reload");
        Tick(sim, 1.75);
        Need(sim.S.downloadQueue.Count == 1 && sim.S.dataFiles.Count == 0, "file stays downloading before its full duration");
        Tick(sim, .25);
        Need(sim.S.downloadQueue.Count == 0 && sim.DataFileReady(id), "file arrives at promised game duration");
        Equal(sim.Samples("chess"), 0, "download completion still requires installation");
        sim = BuyChess();
        var old = sim.S.downloadQueue[0]; old.paceVersion = 0; old.doneMB = old.sizeMB * .37;
        double oldRemaining = sim.DownloadQueue()[0].secondsLeft;
        sim = Reload(sim);
        Need(sim.S.downloadQueue[0].paceVersion == 0, "legacy queue timing is not silently converted");
        Equal(sim.S.downloadQueue[0].doneMB, old.doneMB, "legacy byte progress retained");
        Equal(sim.DownloadQueue()[0].secondsLeft, oldRemaining, "legacy remaining time unchanged after load");
        Debug.Log("DATA_DOWNLOAD_PACE_PASS: " + checks + " in-memory duration/progress/save checks. No real network requests.");
    }
}
#endif
