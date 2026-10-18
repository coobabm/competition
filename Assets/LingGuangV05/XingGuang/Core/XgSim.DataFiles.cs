using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        public int dataFilesVersion;
        public List<string> retainedFreeData = new List<string>();
        public List<XgDownload> dataFiles = new List<XgDownload>();
        public List<XgLabelCount> installedDataBatches = new List<XgLabelCount>();
        public List<string> installedDataOffers = new List<string>();
        public List<string> installedTeachingNodes = new List<string>();
    }

    public sealed partial class XgSim
    {
        const int DataFilesSchema = 1;
        public event Action<string> DataFileInstalled;
        public const string DataEntryNode = "train.data";
        public bool CanInstallData => Has(DataEntryNode);
        public bool CanDownloadDataUrl => CanInstallData;
        public string DataInstallBlocker() => CanInstallData ? null
            : T("先在科技购买数据导入。", "Buy Data Import in Tech first.");

        // Downloaded bytes still count towards the disk; only installed batches feed training.
        public int InstalledBatches(string dataset)
        {
            if (S.dataFilesVersion < DataFilesSchema) return BatchesArrived(dataset);
            return Math.Max(0, Math.Min(BatchesArrived(dataset), (int)Count(S.installedDataBatches, dataset)));
        }

        public bool DataOfferInstalled(string offer)
        {
            var def = DataOfferDef(offer);
            if (def == null) return false;
            if (def.source == XgDataSource.Public || def.source == XgDataSource.Story)
                return InstalledBatches(def.datasetId) > 0;
            return S.installedDataOffers.Contains(offer);
        }

        public bool FreeDataAccess(string dataset) => S.retainedFreeData.Contains(dataset)
            || Has(dataset + ".pack") && !S.owned.Contains(dataset);

        public bool TeachingNodeInstalled(string node) => S.installedTeachingNodes.Contains(node);

        public bool DataFileReady(string fileId)
        {
            if (string.IsNullOrEmpty(fileId)) return false;
            foreach (var file in S.dataFiles)
                if (file.fileId == fileId) return !file.installed && file.doneMB >= file.sizeMB;
            return false;
        }

        // UI passes stable virtual-file identities, never paths on the user's real computer.
        public bool InstallDataFile(string fileId)
        {
            if (!CanInstallData || !DataFileReady(fileId)) return false;
            XgDownload file = S.dataFiles.Find(x => x.fileId == fileId);
            // Commit all simulation state before any callbacks. Duplicate drop/IDs are no-ops.
            file.installed = true;
            if (file.batches > 0)
                SetCount(S.installedDataBatches, file.dataset,
                    Math.Min(BatchesArrived(file.dataset), InstalledBatches(file.dataset) + file.batches));
            var offer = DataOfferDef(file.offer);
            if (offer != null && offer.source == XgDataSource.Junk && !S.installedDataOffers.Contains(file.offer))
                S.installedDataOffers.Add(file.offer);
            if (!string.IsNullOrEmpty(file.nodeId) && !S.installedTeachingNodes.Contains(file.nodeId))
                S.installedTeachingNodes.Add(file.nodeId);
            foreach (var run in Runs) if (run.dataset == file.dataset) Evaluate(run);
            CheckDesks();
            Say(T("数据包装好了。", "Data pack installed."));
            DataFileInstalled?.Invoke(fileId);
            return true;
        }

        public int InstallDataFiles(IEnumerable<string> fileIds)
        {
            if (fileIds == null) return 0;
            // Copy first: selection can change while UI handles installed notifications.
            var unique = new HashSet<string>(fileIds);
            int installed = 0;
            foreach (string id in unique) if (InstallDataFile(id)) installed++;
            return installed;
        }

        void QueueTeachingData(string dataset, string nodeId)
        {
            var d = XgCatalog.Dataset(dataset);
            int downloaded = BatchesArrived(dataset);
            // Buying a teaching desk must not revoke a former free milestone dataset.
            // The just-purchased Dataset node itself is not a pre-existing free grant.
            if (nodeId != dataset + ".pack" && FreeDataAccess(dataset) && !S.retainedFreeData.Contains(dataset))
                S.retainedFreeData.Add(dataset);
            if (!S.owned.Contains(dataset) && d != null) S.owned.Add(dataset);
            int all = d != null ? PackBatches(d) : 0;
            if (d != null)
            {
                // Preserve the full-pack entitlement and current prices of the existing nodes.
                // Installing another desk for the same dataset cannot multiply its sample total.
                SetCount(S.batchesBought, dataset, all);
                SetCount(S.batchesArrived, dataset, downloaded);
            }
            Enqueue(new XgDownload { key = dataset, dataset = dataset, offer = dataset + ".pack",
                nodeId = nodeId, batch = 1, batches = all,
                sizeMB = d != null ? d.sizeMB : 0, samples = d != null ? d.samples : 0 }, false);
        }

        bool ValidDataFile(XgDownload file)
        {
            if (file == null || !Finite(file.sizeMB) || !Finite(file.doneMB) || !Finite(file.samples)
                || file.sizeMB < 0 || file.samples < 0 || file.batches < 0) return false;
            if (XgCatalog.Dataset(file.dataset) != null && DataOfferDef(file.offer) != null) return true;
            if (string.IsNullOrEmpty(file.nodeId) || !file.nodeId.StartsWith("pack.", StringComparison.Ordinal)) return false;
            var teaching = XgTeachingPacks.Find(file.nodeId.Substring(5));
            return teaching != null && teaching.dataset == file.dataset;
        }

        void RepairDataFiles()
        {
            if (S.retainedFreeData == null) S.retainedFreeData = new List<string>();
            if (S.dataFiles == null) S.dataFiles = new List<XgDownload>();
            if (S.installedDataBatches == null) S.installedDataBatches = new List<XgLabelCount>();
            if (S.installedDataOffers == null) S.installedDataOffers = new List<string>();
            if (S.installedTeachingNodes == null) S.installedTeachingNodes = new List<string>();
            if (S.dataFilesVersion < DataFilesSchema)
            {
                // This runs after the existing download migration, before simulation repair/evaluation.
                // Already available data stays installed; in-flight files keep their exact byte progress.
                foreach (string dataset in S.owned)
                    if (XgCatalog.Dataset(dataset) != null)
                        SetCount(S.installedDataBatches, dataset, BatchesArrived(dataset));
                foreach (string id in S.dataOffers)
                {
                    var offer = DataOfferDef(id);
                    if (offer != null && offer.source == XgDataSource.Junk && !QueueHas(DownloadKey(offer))
                        && !S.installedDataOffers.Contains(id)) S.installedDataOffers.Add(id);
                }
                foreach (string id in S.unlocked)
                {
                    var node = XgCatalog.Node(id);
                    if (node != null && node.kind == XgNodeKind.Dataset && !S.owned.Contains(node.target)
                        && !S.retainedFreeData.Contains(node.target)) S.retainedFreeData.Add(node.target);
                    bool installedDataset = node != null && node.kind == XgNodeKind.Dataset && BatchesArrived(node.target) > 0;
                    if ((id.StartsWith("pack.", StringComparison.Ordinal) || installedDataset)
                        && !S.installedTeachingNodes.Contains(id)) S.installedTeachingNodes.Add(id);
                }
                S.dataFilesVersion = DataFilesSchema;
            }
            S.dataFiles.RemoveAll(x => !ValidDataFile(x));
            S.installedDataBatches.RemoveAll(x => x == null || XgCatalog.Dataset(x.dataset) == null || !Finite(x.count) || x.count < 0);
            var ids = new HashSet<string>();
            foreach (var file in S.dataFiles)
            {
                if (string.IsNullOrEmpty(file.fileId)) file.fileId = Guid.NewGuid().ToString("N");
                file.doneMB = file.sizeMB;
            }
            // A duplicated serialized entry is one file, not a second payout.
            S.dataFiles.RemoveAll(x => !ids.Add(x.fileId));
            foreach (var queued in S.downloadQueue)
            {
                if (string.IsNullOrEmpty(queued.fileId)) queued.fileId = Guid.NewGuid().ToString("N");
                queued.installed = false;
            }
            S.downloadQueue.RemoveAll(x => !ids.Add(x.fileId));
        }
    }
}
