using System;

namespace LingGuangV05.XingGuang
{
    public enum XgDataUrlStatus { Unknown, ImportLocked, CapabilityLocked, TooExpensive, Available, Downloading, Downloaded, Installed, Owned, Started, Failed }
    public sealed class XgDataUrlPreview
    {
        public XgDataUrlStatus status;
        public string url = "", offerId = "", message = "";
        public double price;
        public bool canDownload => status == XgDataUrlStatus.Available;
    }
    public sealed partial class XgSim
    {
        public const string MnistSourceUrl = "http://yann.lecun.com/exdb/mnist/";
        public string DataUrlClue(string dataset) => dataset == "mnist"
            ? T("公开数据地址：", "Public data address: ") + MnistSourceUrl : "";

        // This is an in-game source registry. Never fetch a URL or access the real filesystem.
        public static string ResolveDataUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string raw = value.Trim();
            if (raw.IndexOfAny(new[] { '\\', '%', '?', '#', '@' }) >= 0) return null;
            foreach (char c in raw) if (char.IsControl(c) || char.IsWhiteSpace(c)) return null;
            if (!raw.Contains("://")) raw = "http://" + raw;
            // Match the raw authority/path too: Uri would otherwise normalize dot segments.
            int split = raw.IndexOf("://", StringComparison.Ordinal);
            string scheme = raw.Substring(0, split);
            if (!scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && !scheme.Equals("https", StringComparison.OrdinalIgnoreCase)) return null;
            string rest = raw.Substring(split + 3);
            int slash = rest.IndexOf('/');
            if (slash < 0 || !rest.Substring(0, slash).Equals("yann.lecun.com", StringComparison.OrdinalIgnoreCase)) return null;
            string path = rest.Substring(slash);
            if (path != "/exdb/mnist" && path != "/exdb/mnist/") return null;
            if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) || uri.UserInfo.Length != 0 || !uri.IsDefaultPort) return null;
            return "mnist.pack";
        }

        public XgDataUrlPreview PreviewDataUrl(string value, IXgHost host)
        {
            var p = new XgDataUrlPreview();
            string id = ResolveDataUrl(value);
            if (id == null) { p.status = XgDataUrlStatus.Unknown; p.message = T("未识别的数据地址。", "Unknown data address."); return p; }
            p.offerId = id; p.url = MnistSourceUrl;
            if (!CanDownloadDataUrl) { p.status = XgDataUrlStatus.ImportLocked; p.message = DataInstallBlocker(); return p; }
            var def = DataOfferDef(id);
            foreach (var file in S.downloadQueue)
                if (file.offer == id) { p.status = XgDataUrlStatus.Downloading; p.message = T("这份数据正在下载。", "This data is already downloading."); return p; }
            foreach (var file in S.dataFiles)
                if (file.offer == id && !file.installed) { p.status = XgDataUrlStatus.Downloaded; p.message = T("已在资料文件夹，拖入练页安装。", "Already in Training files; drag it to Train to install."); return p; }
            if (DataOfferInstalled(id)) { p.status = XgDataUrlStatus.Installed; p.message = T("这份数据已经安装。", "This data is already installed."); return p; }
            // Legacy entitlements may have no virtual file. Never fabricate a second sample reward.
            if (OfferOwned(id) || Has(def.node) || S.owned.Contains(def.datasetId))
            { p.status = XgDataUrlStatus.Owned; p.message = T("已拥有这份数据，不会重复收费。", "You own this data; no duplicate charge."); return p; }
            string why = OfferBlocker(def, out string whyEn);
            if (why != null) { p.status = XgDataUrlStatus.CapabilityLocked; p.message = T("已识别地址，但尚未满足条件：", "Address recognized; requirements not met: ") + T(why, whyEn); return p; }
            var node = XgCatalog.Node(def.node);
            p.price = NodeCost(node);
            var state = Status(node, host);
            if (state != NodeStatus.Buyable)
            { p.status = state == NodeStatus.TooExpensive ? XgDataUrlStatus.TooExpensive : XgDataUrlStatus.CapabilityLocked; p.message = Why(node, host); return p; }
            p.status = XgDataUrlStatus.Available;
            p.message = T("下载数据包 ¥", "Download data pack ¥") + F(p.price, "0.##");
            return p;
        }

        // Re-evaluate immediately before purchase; a preview never reserves price or entitlement.
        public XgDataUrlPreview DownloadDataUrl(string value, IXgHost host)
        {
            var p = PreviewDataUrl(value, host);
            if (!p.canDownload) return p;
            if (!BuyDataOffer(p.offerId, host))
            { p.status = XgDataUrlStatus.Failed; p.message = T("下载未开始，请检查购买条件。", "Download did not start; check purchase requirements."); return p; }
            p.status = XgDataUrlStatus.Started;
            p.message = T("已加入下载队列。", "Added to the download queue.");
            return p;
        }
    }
}
