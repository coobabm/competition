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
            if (feedbackSim != null)
            {
                feedbackSim.DownloadStarted += FileDownloadStarted;
                feedbackSim.DownloadFinished += FileDownloadFinished;
            }
        }
        void UnbindDownloadFeedback()
        {
            if (feedbackSim != null)
            {
                feedbackSim.DownloadStarted -= FileDownloadStarted;
                feedbackSim.DownloadFinished -= FileDownloadFinished;
            }
            feedbackSim = null;
        }
        void FileDownloadStarted(string key, double seconds)
        {
            var source = feedbackSim;
            if (source == null || !source.S.downloadQueue.Exists(file => file.key == key)) return;
            string body = T("数据包已加入下载，可在桌面「资料文件夹」→「训练数据」查看进度。",
                "Pack download started. Check its progress in desktop Training files → Training data.");
            body += "\n" + (source.CanInstallData
                ? T("下载完成后，把数据包拖到「练」安装。", "When ready, drag the pack into Train to install.")
                : source.DataInstallBlocker());
            NotifyDataFiles(source, body);
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
            string body = T("下载完成，桌面「资料文件夹」→「训练数据」中有 ", "Download complete: desktop Training files → Training data has ") + ready
                + T(" 个数据包待安装。", " packs awaiting installation.");
            body += "\n" + (source.CanInstallData
                ? T("点击打开资料文件夹，把数据包拖到「练」安装。", "Click to open Training files, then drag packs into Train.")
                : source.DataInstallBlocker());
            NotifyDataFiles(source, body);
        }
        void NotifyDataFiles(XgSim source, string body)
        {
            var owner = XingGuangController.Instance;
            if (this == null || !isActiveAndEnabled || source == null || !ReferenceEquals(Sim, source)
                || owner == null || !ReferenceEquals(owner.Sim, source) || owner.runtime == null) return;
            UpdateNames();
            if (shortcutRoot != null) shortcutRoot.SetActive(CanBrowse);
            // Existing tray service handles muted audio, burst limits and cutscene deferral.
            // Nothing opens or gains focus until the player clicks the notification.
            DesktopNotifications.Notify(owner.runtime, DisplayName, "", body, false, () =>
            {
                var current = XingGuangController.Instance;
                if (this == null || current == null || !ReferenceEquals(current.Sim, source)
                    || !ReferenceEquals(Sim, source) || !CanBrowse) return;
                ShowTrainingData(source);
            });
        }
    }
}
