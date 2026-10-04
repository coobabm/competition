using System.Globalization;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppNames = LingGuangV05.Core.AppNames;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// lingguang.cc in the native browser: the 2016 project page for 灵光. Built by ChapterOneDownloadInstaller from
    /// native DreamOS widgets. The big button starts the native download (WebBrowserManager.DownloadFile); while it runs
    /// the page mirrors the native download item: MB done / total, speed and time left. After the download the button
    /// opens the app. All text goes through GameText, so Settings → 语言 switches it.
    /// </summary>
    public sealed class LingGuangDownloadPage : MonoBehaviour
    {
        public TMP_Text title, tagline, meta, body, status, footer;
        public ButtonManager downloadButton;
        public Slider progress;
        public float fileSizeMB = 46.8f;

        private WebBrowserManager browser;
        private NativeAppFontFallback fonts;
        private float nextRefresh;
        private string lastButton;

        private void OnEnable()
        {
            browser = GetComponentInParent<WebBrowserManager>();
            // Browser fonts have no CJK glyphs; same per-instance Noto fallback the native apps use.
            fonts = fonts ?? new NativeAppFontFallback();
            foreach (var label in GetComponentsInChildren<TMP_Text>(true)) fonts.Apply(label);
            GameText.Changed += Refresh;
            nextRefresh = 0;
            Refresh();
        }

        private void OnDisable() { GameText.Changed -= Refresh; }
        private void OnDestroy() { fonts?.Dispose(); fonts = null; }

        private void Update() { if (Time.unscaledTime >= nextRefresh) Refresh(); }

        /// <summary>Wired to the page button.</summary>
        public void OnDownloadClicked()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime != null && runtime.Sim != null && runtime.Sim.AppInstalled && !runtime.TestMode)
            {
                var router = FindAnyObjectByType<ChapterOneDesktopRouter>();
                if (router != null) router.Open(LingGuangInstallFlow.AppId);
                return;
            }
            // 灵光 is only passed hand to hand now: 老周 sends the file in YY.
            var desk = FindAnyObjectByType<ChapterOneDesktopRouter>();
            if (desk != null) desk.Open("yy");
            Refresh();
        }

        private void Refresh()
        {
            nextRefresh = Time.unscaledTime + .2f;
            Set(title, GameText.T(AppNames.AppZh + " " + AppNames.AppEn, AppNames.AppEn));
            Set(tagline, GameText.T("一个会自己学习的小程序", "A little program that learns by itself"));
            Set(meta, GameText.T("v0.1.3 · 2016-05-20 · Windows 7 / 8 / 10 · " + Mb(fileSizeMB) + " MB",
                "v0.1.3 · 2016-05-20 · Windows 7 / 8 / 10 · " + Mb(fileSizeMB) + " MB"));
            Set(body, GameText.T(
                "它一开始什么都不会，只会回答「是」和「否」。\n你教它，它就会学。\n\n·  本地运行，不联网也能用\n·  开源，由几个爱好者维护\n·  我们不保证它会学成什么样",
                "At first it knows nothing. It can only answer \"yes\" or \"no\".\nTeach it, and it learns.\n\n·  Runs locally, works offline\n·  Open source, maintained by a few hobbyists\n·  No promises about what it turns into"));
            Set(footer, GameText.T("© 2016 lingguang.cc · 下载即表示你知道这只是个实验", "© 2016 lingguang.cc · By downloading you accept this is an experiment"));

            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            bool installed = runtime != null && runtime.Sim != null && runtime.Sim.AppInstalled;
            var item = FindItem();
            Slider native = item != null ? item.GetComponentInChildren<Slider>(true) : null;
            var network = browser != null ? browser.networkManager : null;
            bool connected = network != null && network.isConnected;
            float speed = SpeedMBps(network);

            if (installed || (item != null && item.isFinished))
            {
                ShowProgress(1, 1);
                Set(status, GameText.T("下载完成 · " + AppNames.ExeZh + " 已放到桌面", "Download complete · " + AppNames.ExeEn + " is on your desktop"));
                SetButton(GameText.T("打开 " + AppNames.ExeZh, "Open " + AppNames.ExeEn), true);
            }
            else if (native != null)
            {
                float done = native.value, total = Mathf.Max(.01f, native.maxValue);
                ShowProgress(done, total);
                string left = speed > 0 ? Seconds((total - done) / speed) : "—";
                Set(status, connected
                    ? GameText.F("正在下载  {0} MB / {1} MB  ·  {2} MB/s  ·  剩余 {3}", "Downloading  {0} MB / {1} MB  ·  {2} MB/s  ·  {3} left", Mb(done), Mb(total), Mb(speed), left)
                    : GameText.F("网络已断开，下载暂停在 {0} MB。在右下角连接网络后自动继续。", "Disconnected. Paused at {0} MB; reconnect from the taskbar to resume.", Mb(done)));
                SetButton(GameText.T("下载中…", "Downloading…"), false);
            }
            else
            {
                ShowProgress(0, 1);
                Set(status, GameText.T("内测版不公开下载。认识开发者的人会直接用 YY 传给你。", "The beta is not public. Someone who knows the developers will send it to you on YY."));
                SetButton(GameText.T("打开 YY 找老周要", "Ask Lao Zhou on YY"), true);
            }
        }

        private WebBrowserDownloadItem FindItem()
        {
            if (browser == null) return null;
            foreach (var d in browser.activeDownloads) if (d != null && d.fileName == LingGuangInstallFlow.FileName) return d;
            foreach (var d in browser.GetComponentsInChildren<WebBrowserDownloadItem>(true)) if (d.fileName == LingGuangInstallFlow.FileName) return d;
            return null;
        }

        private static float SpeedMBps(NetworkManager network)
        {
            if (network == null || !network.isConnected) return 0;
            if (network.dynamicNetwork && network.currentNetworkIndex >= 0 && network.currentNetworkIndex < network.networkItems.Count)
                return network.networkItems[network.currentNetworkIndex].networkSpeed;
            return network.defaultSpeed;
        }

        private void ShowProgress(float value, float max)
        {
            if (progress == null) return;
            progress.maxValue = max; progress.SetValueWithoutNotify(value);
        }

        private void SetButton(string text, bool interactable)
        {
            if (downloadButton == null) return;
            if (text != lastButton)
            {
                downloadButton.SetText(text); lastButton = text;
                if (fonts != null) foreach (var label in downloadButton.GetComponentsInChildren<TMP_Text>(true)) fonts.Apply(label);
            }
            if (downloadButton.isInteractable != interactable) downloadButton.Interactable(interactable);
        }

        private static void Set(TMP_Text label, string value) { if (label != null && label.text != value) label.text = value; }
        private static string Mb(float value) { return value.ToString("0.0", CultureInfo.InvariantCulture); }
        private static string Seconds(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return s < 60 ? GameText.F("{0} 秒", "{0} s", s) : GameText.F("{0} 分 {1} 秒", "{0} min {1} s", s / 60, s % 60);
        }
    }
}
