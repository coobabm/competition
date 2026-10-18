using LingGuangV05.Desktop.Media;
using LingGuangV05.XingGuang;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgFilesWindow
    {
        XgSim feedbackSim;
        void BindDownloadFeedback(XgSim sim)
        {
            if (ReferenceEquals(feedbackSim, sim)) return;
            UnbindDownloadFeedback();
            feedbackSim = sim;
            if (feedbackSim != null) feedbackSim.DownloadFinished += FileDownloadFinished;
        }
        void UnbindDownloadFeedback()
        {
            if (feedbackSim != null) feedbackSim.DownloadFinished -= FileDownloadFinished;
            feedbackSim = null;
        }
        void FileDownloadFinished(string key)
        {
            var owner = XingGuangController.Instance;
            var source = feedbackSim;
            if (this == null || !isActiveAndEnabled || source == null || !ReferenceEquals(Sim, source)
                || owner == null || !ReferenceEquals(owner.Sim, source) || owner.runtime == null) return;
            int ready = 0;
            foreach (var file in source.S.dataFiles) if (source.DataFileReady(file.fileId)) ready++;
            if (ready == 0) return;
            string body = T("下载完成，资料夹中有 ", "Download complete: ") + ready
                + T(" 个数据包待安装。", " data packs ready in Files.");
            if (!source.CanInstallData) body += "\n" + source.DataInstallBlocker();
            // Existing tray service handles muted audio, burst limits and cutscene deferral.
            // Nothing opens or gains focus until the player clicks the notification.
            DesktopNotifications.Notify(owner.runtime, DisplayName, "", body, false, () =>
            {
                var current = XingGuangController.Instance;
                if (this == null || current == null || !ReferenceEquals(current.Sim, source)
                    || !ReferenceEquals(Sim, source) || !CanBrowse) return;
                folder = "packs";
                Show();
            });
        }
    }
}
