#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

// In-memory simulation only. No scene, runtime, real save, network or player preference access.
// Run after the candidate core is integrated, via the normal editor verification batch.
public static class DataFileCoreChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    sealed class Wallet : IXgHost
    {
        public double Money { get; private set; }
        public int payments;
        public Wallet(double money = 1e9) { Money = money; }
        public double Compute => 1;
        public double VramMB => 1e8;
        public string Blocker => null;
        public bool Spend(double amount) { if (amount < 0 || amount > Money) return false; Money -= amount; payments++; return true; }
        public void Earn(double amount) { Money += amount; }
        public void Train(double seconds) { }
    }
    static void Need(bool ok, string text) { if (!ok) throw new InvalidOperationException("DATA_FILES: " + text); checks++; }
    static void Equal(double actual, double expected, string text) => Need(Math.Abs(actual - expected) <= Math.Max(1e-6, Math.Abs(expected) * 1e-10), text + " actual=" + actual + " expected=" + expected);
    static object Call(XgSim sim, string method, params object[] args) => typeof(XgSim).GetMethod(method, Private).Invoke(sim, args);
    static XgSim Reload(XgSim sim)
    {
        var copy = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
        Need(copy.CanInstallData == sim.CanInstallData, "purchased entry gate persists without gifting unbought access");
        return copy;
    }
    static void SetCount(List<XgLabelCount> values, string id, double value)
    {
        var row = values.Find(x => x.dataset == id);
        if (row == null) values.Add(new XgLabelCount { dataset = id, count = value }); else row.count = value;
    }
    static XgSim Fresh()
    {
        var sim = new XgSim();
        Need(!sim.CanInstallData && !sim.CanDownloadDataUrl && sim.DataInstallBlocker() != null, "new game has no purchased Data Import");
        sim.S.teachingFlow = false;
        sim.S.openingFocus = false;
        sim.S.diskBonusMB = 1e12;
        Need(sim.S.dataFilesVersion > 0 && sim.S.installedDataBatches.Count == 0
            && sim.S.installedDataOffers.Count == 0 && sim.S.installedTeachingNodes.Count == 0,
            "new empty version-zero state receives no installed gifts");
        return sim;
    }
    static void UnlockParents(XgSim sim, XgNode node, HashSet<string> visiting)
    {
        if (node == null || !visiting.Add(node.id)) return;
        foreach (string id in (node.needs ?? new string[0]).Concat(new[] { node.parent }))
        {
            if (string.IsNullOrEmpty(id)) continue;
            UnlockParents(sim, XgCatalog.Node(id), visiting);
            if (!sim.Has(id)) sim.S.unlocked.Add(id);
        }
    }
    static void PreparePurchase(XgSim sim, string nodeId)
    {
        // Eligibility fixture, not evidence of reaching this stage through normal play.
        sim.S.stage = sim.S.stageVision = sim.S.stageSequence = 6;
        sim.S.firstSpecialty = "vision";
        foreach (string id in new[] { "train.curve", "lenet", "conv", "rnn", "lstm", "gru" })
            if (!sim.Has(id)) sim.S.unlocked.Add(id);
        UnlockParents(sim, XgCatalog.Node(nodeId), new HashSet<string>());
    }
    static void BuyEntry(XgSim sim, Wallet wallet)
    {
        if (sim.CanInstallData) return;
        if (!sim.Has("train.curve")) sim.S.unlocked.Add("train.curve"); // Other subsystem fixtures grant only the prerequisite.
        double before = wallet.Money, price = sim.NodeCost(XgCatalog.Node(XgSim.DataEntryNode));
        Need(sim.BuyNode(XgSim.DataEntryNode, wallet), "actual Data Import node purchase succeeds");
        Equal(before - wallet.Money, price, "Data Import cost charged by real purchase");
    }
    static void Buy(XgSim sim, Wallet wallet, string nodeId, bool entry = true)
    {
        if (entry) BuyEntry(sim, wallet);
        PreparePurchase(sim, nodeId);
        var node = XgCatalog.Node(nodeId);
        Need(node != null, "real catalog node exists: " + nodeId);
        double cost = sim.NodeCost(node), before = wallet.Money;
        Need(sim.BuyNode(nodeId, wallet), "real BuyNode succeeds: " + nodeId + " / " + sim.Why(node, wallet));
        Equal(before - wallet.Money, cost, "existing node price is used exactly once");
        before = wallet.Money;
        Need(!sim.BuyNode(nodeId, wallet), "repeat purchase denied");
        Equal(wallet.Money, before, "repeat purchase does not charge");
    }
    static void DownloadAll(XgSim sim)
    {
        // Exercise the actual queue/time/Arrive route; isolate it from unrelated calendar events.
        for (int i = 0; i < 100000 && sim.S.downloadQueue.Count > 0; i++) Call(sim, "TickDownloads", .25);
        Need(sim.S.downloadQueue.Count == 0, "real queue finishes within bounded simulation time");
    }
    static XgDownload File(XgSim sim, string nodeId) => sim.S.dataFiles.Single(x => x.nodeId == nodeId);

    static void PurchaseDownloadInstall()
    {
        var sim = Fresh(); var wallet = new Wallet();
        double before = sim.Samples("mnist");
        Buy(sim, wallet, "mnist.pack", false);
        Equal(sim.Samples("mnist"), before, "buying does not grant samples");
        string id = sim.S.downloadQueue.Count > 0 ? sim.S.downloadQueue[0].fileId : File(sim, "mnist.pack").fileId;
        if (sim.S.downloadQueue.Count > 0) Need(!sim.InstallDataFile(id), "cannot install a downloading file");
        sim = Reload(sim);
        Need(sim.S.downloadQueue.Any(x => x.fileId == id) || sim.S.dataFiles.Any(x => x.fileId == id), "identity survives queued serialization");
        Equal(sim.Samples("mnist"), before, "queued reload grants no samples");
        DownloadAll(sim);
        Equal(sim.Samples("mnist"), before, "download completion grants no samples");
        Need(sim.DataFileReady(id), "download produces ready file");
        Need(!sim.CanInstallData && !sim.InstallDataFile(id) && sim.DataFileReady(id), "unbought entry cannot consume a ready file");
        Need(sim.InstallDataFiles(new[] { id }) == 0, "unbought shared gate blocks bulk installation too");
        BuyEntry(sim, wallet);
        sim = Reload(sim);
        Need(sim.DataFileReady(id), "ready file survives reload without auto-install");
        Equal(sim.Samples("mnist"), before, "ready reload grants no samples");
        Need(sim.InstallDataFile(id), "ready file installs");
        Equal(sim.Samples("mnist"), before + XgCatalog.Dataset("mnist").samples, "installation grants original full pack once");
        Need(!sim.InstallDataFile(id), "repeated drop rejected");
        sim = Reload(sim);
        Need(!sim.DataFileReady(id) && !sim.InstallDataFile(id), "installed state survives reload");
        Equal(sim.Samples("mnist"), before + XgCatalog.Dataset("mnist").samples, "installed reload does not duplicate samples");
    }

    static void TeachingNodeAndBatchSelection()
    {
        var sim = Fresh(); var wallet = new Wallet();
        var paid = XgTeachingPacks.All.Where(x => x.buy && x.needs != "imagenet").Take(2).ToArray();
        Need(paid.Length == 2, "catalog provides two paid packs for mixed selection");
        foreach (var p in paid)
        {
            Buy(sim, wallet, p.NodeId);
            Need(!sim.TeachingNodeInstalled(p.NodeId), "buy does not mark paid desk installed");
        }
        DownloadAll(sim);
        string a = File(sim, paid[0].NodeId).fileId, b = File(sim, paid[1].NodeId).fileId;
        foreach (var p in paid)
        {
            PreparePurchase(sim, p.NodeId);
            Need(!sim.TeachingPackAvailable(p.id), "downloaded paid desk stays locked until install");
        }
        int callbacks = 0; sim.DataFileInstalled += _ => callbacks++;
        Need(sim.InstallDataFiles(new[] { a, a, b, "missing" }) == 2, "mixed batch installs unique ready IDs only");
        Need(callbacks == 2 && sim.InstallDataFiles(new[] { a, b }) == 0, "notification and settlement occur once per file");
        foreach (var p in paid)
        {
            PreparePurchase(sim, p.NodeId);
            Need(sim.TeachingPackAvailable(p.id), "installed paid desk becomes playable");
        }
        foreach (var p in XgTeachingPacks.All.Where(x => !x.buy && string.IsNullOrEmpty(x.needs)))
        {
            PreparePurchase(sim, "train.curve");
            Need(sim.TeachingPackAvailable(p.id), "free original desk is not gated by installation: " + p.id);
        }
    }

    static XgSim LegacyPartial()
    {
        var baseSim = Fresh(); var state = baseSim.S;
        var d = XgCatalog.Dataset("mnist");
        state.owned.Add(d.id); state.unlocked.Add("mnist.pack");
        state.dataFilesVersion = 0;
        state.installedDataBatches.Clear(); state.installedTeachingNodes.Clear();
        SetCount(state.batchesBought, d.id, 3); SetCount(state.batchesArrived, d.id, 2);
        state.downloadQueue.Add(new XgDownload { key = d.id, dataset = d.id, offer = "mnist.pack", batch = 3, batches = 1,
            sizeMB = XgSim.BatchMB(d), doneMB = XgSim.BatchMB(d) * .37, samples = XgSim.BatchSamples(d) });
        return Reload(baseSim);
    }

    static void LegacyAndSameDataset()
    {
        var sim = LegacyPartial(); var wallet = new Wallet(); var d = XgCatalog.Dataset("mnist");
        Need(sim.InstalledBatches(d.id) == 2 && sim.TeachingNodeInstalled("mnist.pack"), "old partial dataset node migrated as installed");
        Equal(sim.S.downloadQueue[0].doneMB, XgSim.BatchMB(d) * .37, "old in-flight progress retained");
        string oldId = sim.S.downloadQueue[0].fileId;
        var p = XgTeachingPacks.All.First(x => x.buy && x.dataset == d.id);
        Buy(sim, wallet, p.NodeId);
        Need(sim.S.downloadQueue.Any(x => x.fileId == oldId), "new full pack does not discard old queue");
        Need(sim.BatchesBought(d.id) == XgSim.PackBatches(d) && sim.InstalledBatches(d.id) == 2, "new full entitlement preserves old installed amount");
        DownloadAll(sim);
        Need(sim.BatchesArrived(d.id) == XgSim.PackBatches(d) && sim.InstalledBatches(d.id) == 2, "overlapping downloads cap bytes but grant no samples");
        Need(sim.InstallDataFile(oldId) && sim.InstalledBatches(d.id) == 3, "old batch installs exactly once");
        Need(sim.InstallDataFile(File(sim, p.NodeId).fileId), "new teaching file installs independently");
        Need(sim.InstalledBatches(d.id) == XgSim.PackBatches(d), "old batch plus new full pack never exceed full total");
        Equal(sim.PackSamples(d.id), d.samples, "same dataset node purchases do not multiply samples");
        sim = Reload(sim);
        Equal(sim.PackSamples(d.id), d.samples, "overlapping full and partial installation survives reload");
    }

    static void OldWholeAndPaidDesk()
    {
        var sim = Fresh(); var state = sim.S;
        state.owned.Add("mnist"); state.unlocked.Add("mnist.pack"); state.unlocked.Add("pack.make24");
        state.desksOpen.Add("arith"); state.dataFilesVersion = 0; state.downloadsVersion = 0;
        sim = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(state)));
        Need(sim.InstalledBatches("mnist") == XgSim.PackBatches("mnist"), "pre-download whole-pack save remains installed");
        Need(sim.TeachingNodeInstalled("pack.make24") && sim.DeskOpen("arith"), "old paid desk and opened desk retained");
        Need(!sim.CanInstallData, "old installed data does not silently receive a newly paid entry");
        double samples = sim.Samples("mnist");
        sim = Reload(sim); Equal(sim.Samples("mnist"), samples, "migration is idempotent");
    }

    static void BadSectorsAndRepair()
    {
        var sim = LegacyPartial(); var wallet = new Wallet(); var d = XgCatalog.Dataset("mnist");
        // Real settlement method, fixed event fixture; not a test of the event scheduler.
        sim.S.lifeArgKey = d.id;
        Call(sim, "ApplyLifeChoice", "bad_sectors", "lose", wallet);
        Need(sim.InstalledBatches(d.id) == 1 && sim.BatchesArrived(d.id) == 1, "bad sectors decrement installed and downloaded data together");
        double damaged = sim.PackSamples(d.id);
        DownloadAll(sim);
        Equal(sim.PackSamples(d.id), damaged, "later download alone cannot heal lost installed samples");
        string id = sim.S.dataFiles.First().fileId;
        BuyEntry(sim, wallet);
        Need(sim.InstallDataFile(id), "replacement file installs");
        Equal(sim.PackSamples(d.id), damaged + XgSim.BatchSamples(d), "installation restores one batch, no more");
        Need(!sim.InstallDataFile(id), "replacement cannot be installed twice");
    }

    static void TwoTeachingNodesSameDataset()
    {
        var sim = Fresh(); var wallet = new Wallet();
        var dataset = XgCatalog.Dataset("chess");
        Need(dataset != null && dataset.samples > 0, "real catalog provides chess training data");
        double before = sim.Samples(dataset.id), fullPack = dataset.samples;
        Buy(sim, wallet, "pack.chess_mate1");
        Buy(sim, wallet, "pack.chess_mate2");
        Equal(sim.Samples(dataset.id), before, "buying two chess desks does not grant uninstalled samples");
        DownloadAll(sim);
        Equal(sim.Samples(dataset.id), before, "both chess downloads still grant no uninstalled samples");
        string first = File(sim, "pack.chess_mate1").fileId;
        string second = File(sim, "pack.chess_mate2").fileId;
        Need(first != second, "same-dataset teaching nodes have distinct file identities");
        Need(sim.InstallDataFile(first), "first chess file installs");
        Equal(sim.Samples(dataset.id), before + fullPack, "first installation grants exactly one catalog chess pack");
        Need(sim.InstalledBatches(dataset.id) == XgSim.PackBatches(dataset), "first chess install fills exactly the defined batch total");
        PreparePurchase(sim, "pack.chess_mate2");
        Need(sim.TeachingPackAvailable("chess_mate1") && !sim.TeachingPackAvailable("chess_mate2"), "installing first chess desk does not unlock second");
        Need(sim.InstallDataFile(second), "second chess file installs independently");
        PreparePurchase(sim, "pack.chess_mate2");
        Need(sim.TeachingPackAvailable("chess_mate1") && sim.TeachingPackAvailable("chess_mate2"), "both original chess desks remain available");
        Equal(sim.Samples(dataset.id), before + fullPack, "second chess desk cannot award the shared dataset a second time");
        Need(!sim.InstallDataFile(first) && !sim.InstallDataFile(second), "neither chess file can be installed twice");
        Equal(sim.Samples(dataset.id), before + fullPack, "repeated chess drops cannot change samples");
        sim = Reload(sim);
        Need(sim.S.dataFiles.Count(x => x.dataset == dataset.id) == 2, "both independently installed teaching files survive save repair");
        Equal(sim.Samples(dataset.id), before + fullPack, "reload preserves exactly one chess dataset reward");
        Need(sim.TeachingNodeInstalled("pack.chess_mate1") && sim.TeachingNodeInstalled("pack.chess_mate2"), "reload preserves both installed chess desks");
    }

    static void JunkAndDuplicateSerialization()
    {
        var sim = Fresh(); var wallet = new Wallet();
        sim.Today = 20161201; PreparePurchase(sim, "train.curve");
        var offer = sim.ShopDataOffers().First(x => x.source == XgDataSource.Junk);
        double before = sim.Samples(offer.datasetId), noise = sim.DataNoise(offer.datasetId);
        Need(sim.BuyDataOffer(offer.id, wallet), "real junk purchase succeeds");
        DownloadAll(sim);
        Equal(sim.Samples(offer.datasetId), before, "downloaded junk grants no sample before install");
        Equal(sim.DataNoise(offer.datasetId), noise, "downloaded junk grants no noise before install");
        var file = sim.S.dataFiles.Single(x => x.offer == offer.id);
        string id = file.fileId;
        // JSON can contain duplicated inventory rows; one identity must still be one payout.
        sim.S.dataFiles.Add(JsonUtility.FromJson<XgDownload>(JsonUtility.ToJson(file)));
        sim.S.dataFiles.Add(null);
        sim = Reload(sim);
        Need(sim.S.dataFiles.Count(x => x.fileId == id) == 1, "save repair deduplicates virtual-file IDs and null entries");
        BuyEntry(sim, wallet);
        Need(sim.InstallDataFile(id), "junk installs once");
        Equal(sim.Samples(offer.datasetId), before + offer.samples, "junk sample total uses original offer");
        Equal(sim.DataNoise(offer.datasetId), noise + offer.samples * offer.noise, "junk noise uses original offer");
        sim = Reload(sim);
        Need(!sim.InstallDataFile(id), "junk cannot pay twice after reload");
        Equal(sim.Samples(offer.datasetId), before + offer.samples, "junk remains exactly once after reload");
    }

    static void LegacyFreeGrantStaysAvailable()
    {
        var sim = Fresh();
        sim.S.unlocked.Add("translate.pack");
        sim.S.dataFilesVersion = 0;
        sim = Reload(sim);
        Need(sim.DatasetAvailable("translate") && !sim.S.owned.Contains("translate"), "old free translation grant stays usable without a paid pack");
        double samples = sim.Samples("translate");
        var wallet = new Wallet();
        Buy(sim, wallet, "pack.chinglish");
        Need(sim.S.owned.Contains("translate"), "new teaching pack owns a download entitlement");
        Need(sim.DatasetAvailable("translate") && sim.FreeDataAccess("translate"), "new paid download cannot revoke old free dataset access");
        Equal(sim.Samples("translate"), samples, "retaining free access does not invent paid samples");
        sim = Reload(sim);
        Need(sim.DatasetAvailable("translate"), "retained free grant survives owning a pending download and reloading");
        Equal(sim.Samples("translate"), samples, "pending paid pack still has zero added samples after reload");
        DownloadAll(sim);
        Need(sim.DatasetAvailable("translate"), "download completion keeps old free access");
        Equal(sim.Samples("translate"), samples, "free access cannot bypass paid sample installation");
    }

    static void PaidEntryRule()
    {
        var sim = new XgSim(); sim.S.teachingFlow = true;
        var node = XgCatalog.Node(XgSim.DataEntryNode);
        Need(node != null && node.parent == "train.curve" && node.cost == 50, "entry catalog has approved curve prerequisite and ordinary 50 price");
        Need(node.name == "数据导入" && node.nameEn == "Data Import", "approved Chinese and English import names");
        var wallet = new Wallet(100);
        Need(!sim.CanInstallData && !sim.CanDownloadDataUrl && !sim.TrainPart("data"), "new data install and URL entry locked");
        Need(!sim.BuyNode(node.id, wallet) && wallet.Money == 100 && wallet.payments == 0, "curve prerequisite blocks purchase without charging");
        sim.S.handCorrect = 12;
        Need(sim.BuyNode("train.curve", wallet), "real curve purchase opens prerequisite");
        Equal(sim.NodeCost(node), 50, "ordinary entry price is 50, not legacy quarter price");
        Equal(sim.NodeCost(node), Math.Ceiling(node.cost * (1 - Math.Min(.7, sim.RelicFx.techCut)) * sim.DiagnosisFactor(node)), "ordinary price retains legitimate discount formula");
        var poor = new Wallet(49);
        Need(!sim.BuyNode(node.id, poor) && poor.Money == 49 && poor.payments == 0, "insufficient entry funds remain untouched");
        double before = wallet.Money; int payments = wallet.payments;
        Need(sim.BuyNode(node.id, wallet), "entry purchased through normal node path");
        Equal(before - wallet.Money, 50, "purchase charges 50 exactly once");
        Need(wallet.payments == payments + 1 && sim.CanInstallData && sim.CanDownloadDataUrl && sim.TrainPart("data"), "one purchase enables install and URL entry together");
        before = wallet.Money;
        Need(!sim.BuyNode(node.id, wallet) && wallet.Money == before && wallet.payments == payments + 1, "duplicate entry purchase charges nothing");
        sim = Reload(sim);
        Need(sim.CanInstallData && sim.CanDownloadDataUrl, "formal entry ownership survives reload");
        var fresh = Reload(new XgSim());
        Need(!fresh.CanInstallData && !fresh.CanDownloadDataUrl, "new unbought save does not migrate into entry ownership");
    }

    [MenuItem("Tools/Local Verification/Data file core")]
    public static void Run()
    {
        checks = 0;
        PaidEntryRule(); PurchaseDownloadInstall(); TeachingNodeAndBatchSelection(); LegacyAndSameDataset(); OldWholeAndPaidDesk(); BadSectorsAndRepair(); TwoTeachingNodesSameDataset(); JunkAndDuplicateSerialization(); LegacyFreeGrantStaysAvailable();
        Debug.Log("DATA_FILE_CORE_PASS: " + checks + " checks; real simulation purchase/download/install, memory-only state and JSON round trips.");
    }
}
#endif
