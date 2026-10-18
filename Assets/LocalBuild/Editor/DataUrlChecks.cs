#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;
public static class DataUrlChecks
{
    sealed class Wallet : IXgHost
    {
        public double Money { get; private set; }
        public int payments;
        public Wallet(double n) { Money = n; }
        public double Compute => 1; public double VramMB => 1e8; public string Blocker => null;
        public bool Spend(double n) { if (n < 0 || n > Money) return false; Money -= n; payments++; return true; }
        public void Earn(double n) { Money += n; } public void Train(double n) { }
    }
    static void Parents(XgSim sim, XgNode node, HashSet<string> seen)
    {
        if (node == null || !seen.Add(node.id)) return;
        var ids = new List<string>(node.needs ?? new string[0]); ids.Add(node.parent);
        foreach (string id in ids)
        {
            if (string.IsNullOrEmpty(id)) continue;
            Parents(sim, XgCatalog.Node(id), seen);
            if (!sim.Has(id)) sim.S.unlocked.Add(id);
        }
    }
    static int checks;
    static void Need(bool ok, string why) { if (!ok) throw new Exception("Data URL: " + why); checks++; }
    [MenuItem("Tools/Local Verification/Data URL registry")]
    public static void Run()
    {
        checks = 0;
        foreach (string value in new[] { "yann.lecun.com/exdb/mnist", "http://yann.lecun.com/exdb/mnist/", "https://YANN.LECUN.COM/exdb/mnist", "  http://yann.lecun.com/exdb/mnist/  " })
            Need(XgSim.ResolveDataUrl(value) == "mnist.pack", "allow approved source variant");
        foreach (string value in new[] { "", "https://yann.lecun.com.evil/exdb/mnist", "https://evil@yann.lecun.com/exdb/mnist", "http://yann.lecun.com:80/exdb/mnist", "http://yann.lecun.com/exdb/mnist?x=1", "http://yann.lecun.com/exdb/mnist#x", "http://yann.lecun.com/a/../exdb/mnist", "http://yann.lecun.com/exdb/%6dnist", "file:///exdb/mnist", "statmt.org/wmt16", "dumps.wikimedia.org/zhwiki", "https://yann.lecun.com/exdb/MNIST" })
            Need(XgSim.ResolveDataUrl(value) == null, "reject unapproved or ambiguous URL: " + value);
        var sim = new XgSim();
        Need(sim.PreviewDataUrl(XgSim.MnistSourceUrl, null).status == XgDataUrlStatus.ImportLocked, "recognized URL still requires Data Import");
        Need(sim.PreviewDataUrl("unknown", null).status == XgDataUrlStatus.Unknown, "unknown URL is distinct from locked capability");
        sim.S.unlocked.Add(XgSim.DataEntryNode);
        Need(sim.PreviewDataUrl(XgSim.MnistSourceUrl, null).status == XgDataUrlStatus.CapabilityLocked, "Data Import does not bypass source progression");
        sim.S.unlocked.Add("mnist.pack");
        double before = sim.Samples("mnist");
        int files = sim.S.dataFiles.Count, queue = sim.S.downloadQueue.Count;
        Need(sim.DownloadDataUrl(XgSim.MnistSourceUrl, null).status == XgDataUrlStatus.Owned, "entitlement without file is not repurchased");
        Need(sim.Samples("mnist") == before && sim.S.dataFiles.Count == files && sim.S.downloadQueue.Count == queue, "legacy entitlement does not recreate data or grant samples");
        sim = new XgSim();
        sim.S.teachingFlow = false; sim.S.openingFocus = false; sim.S.diskBonusMB = 1e9;
        sim.S.stage = sim.S.stageVision = sim.S.stageSequence = 6; sim.S.firstSpecialty = "vision";
        foreach (string id in new[] { "train.curve", "lenet", "conv", "rnn", "lstm", "gru" })
            if (!sim.Has(id)) sim.S.unlocked.Add(id);
        Parents(sim, XgCatalog.Node("mnist.pack"), new HashSet<string>());
        var wallet = new Wallet(1e9);
        Need(sim.BuyNode(XgSim.DataEntryNode, wallet), "purchase Data Import through actual catalog");
        var preview = sim.PreviewDataUrl(XgSim.MnistSourceUrl, wallet);
        Need(preview.canDownload, "approved source follows real purchase requirements: " + preview.message);
        double price = sim.NodeCost(XgCatalog.Node("mnist.pack")), money = wallet.Money;
        Need(preview.price == price, "preview preserves current discounted catalog price");
        Need(sim.PreviewDataUrl(XgSim.MnistSourceUrl, new Wallet(0)).status == XgDataUrlStatus.TooExpensive, "recognized unlocked source reports insufficient funds");
        int pays = wallet.payments;
        Need(sim.DownloadDataUrl(XgSim.MnistSourceUrl, wallet).status == XgDataUrlStatus.Started, "URL uses actual purchase");
        Need(Math.Abs(money - wallet.Money - price) < 1e-6 && wallet.payments == pays + 1, "original package price charged exactly once");
        int pending = sim.S.downloadQueue.Count;
        Need(sim.DownloadDataUrl(XgSim.MnistSourceUrl, wallet).status == XgDataUrlStatus.Downloading, "second click sees existing download");
        Need(wallet.payments == pays + 1 && sim.S.downloadQueue.Count == pending && sim.Samples("mnist") == 0, "no repeated charge, queue or premature sample reward");
        Debug.Log("DATA_URL_PASS: " + checks + " normalization/gate/legacy/actual-purchase checks; fixture grants prerequisites, not natural progression.");
    }
}
#endif
