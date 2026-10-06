using LingGuangV05.Runtime;
using LingGuangV05.UI;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop
{
    /// <summary>A code-built view that replaces an app's original content (灵光, YY, 邮件).</summary>
    public interface IDesktopAppView
    {
        /// <summary>Build on first use; false if it cannot open yet.</summary>
        bool EnsureReady();
        /// <summary>The window just opened (tab may be null).</summary>
        void OnOpened(string tab);
    }

    /// <summary>
    /// Adapts the chapter app to the existing desktop. This object must live outside
    /// the window: hiding or closing a window never owns the simulation lifetime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChapterOneDesktopBridge : MonoBehaviour
    {
        [SerializeField] private ChapterOneRuntime runtime;
        [SerializeField] private string applicationId = "lingguang";
        [SerializeField] private ChapterOneApp app;
        [SerializeField] private ChapterOneNativeChatAdapter nativeChat;
        [SerializeField] private ChapterOneNativeAppView nativeApp;
        [SerializeField] private WindowManager window;
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Button desktopShortcut;
        [SerializeField] private TaskbarButton taskbarShortcut;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button minimizeButton;
        [SerializeField] private Button maximizeButton;
        [SerializeField] private TMP_Text[] shellLabels = System.Array.Empty<TMP_Text>();

        private bool initialized;
        private IDesktopAppView customView;

        public ChapterOneRuntime Runtime => runtime;
        public IDesktopAppView CustomView => customView;

        /// <summary>Route opening through a replacement view; the window, icon and taskbar button stay the same.</summary>
        public void UseCustomView(IDesktopAppView view) { customView = view; initialized = false; }
        public string ApplicationId => applicationId;
        public ChapterOneApp App => app;
        public ChapterOneNativeChatAdapter NativeChat => nativeChat;
        public ChapterOneNativeAppView NativeApp => nativeApp;
        public bool IsNativeChat => nativeChat != null;
        public bool IsNative => nativeChat != null || nativeApp != null;
        public WindowManager Window => window;
        public RectTransform Content => content;
        public Button DesktopShortcut => desktopShortcut;
        public TaskbarButton TaskbarShortcut => taskbarShortcut;
        public Button CloseButton => closeButton;
        public Button MinimizeButton => minimizeButton;
        public Button MaximizeButton => maximizeButton;
        public bool IsOpen => window != null && window.isOn;
        public bool IsInitialized => initialized;
        public RectTransform DesktopShortcutRect => desktopShortcut != null ? desktopShortcut.transform as RectTransform : null;
        public bool LaunchNeedsDoubleClick => false;

        public void Configure(ChapterOneRuntime gameRuntime, ChapterOneApp appView,
            WindowManager desktopWindow, RectTransform appContent, TMP_FontAsset appFont,
            Button desktopButton, TaskbarButton taskbarButton, Button close,
            Button minimize, Button maximize, string appId = "lingguang", TMP_Text[] extraLabels = null)
        {
            runtime = gameRuntime;
            applicationId = appId;
            app = appView;
            nativeChat = null;
            nativeApp = null;
            window = desktopWindow;
            content = appContent;
            font = appFont;
            desktopShortcut = desktopButton;
            taskbarShortcut = taskbarButton;
            closeButton = close;
            minimizeButton = minimize;
            maximizeButton = maximize;
            shellLabels = extraLabels ?? System.Array.Empty<TMP_Text>();
        }

        public void ConfigureNative(ChapterOneRuntime gameRuntime, WindowManager desktopWindow,
            ChapterOneNativeChatAdapter chat, Button shortcut, TaskbarButton taskbar,
            TMP_Text[] labels = null)
        {
            runtime = gameRuntime;
            applicationId = "yy";
            window = desktopWindow;
            nativeChat = chat;
            nativeApp = null;
            app = null;
            content = desktopWindow != null ? desktopWindow.windowContent : null;
            font = null;
            desktopShortcut = shortcut;
            taskbarShortcut = taskbar;
            closeButton = minimizeButton = maximizeButton = null;
            shellLabels = labels ?? System.Array.Empty<TMP_Text>();
            initialized = false;
        }

        public void ConfigureNativeApp(ChapterOneRuntime gameRuntime, ChapterOneNativeAppView view,
            WindowManager desktopWindow, RectTransform appContent, Button shortcut, TaskbarButton taskbar,
            Button close, Button minimize, Button maximize, string profile, TMP_Text[] labels = null)
        {
            runtime = gameRuntime;
            applicationId = profile;
            window = desktopWindow;
            nativeApp = view;
            nativeChat = null;
            app = null;
            content = appContent;
            desktopShortcut = shortcut;
            taskbarShortcut = taskbar;
            closeButton = close;
            minimizeButton = minimize;
            maximizeButton = maximize;
            shellLabels = labels ?? System.Array.Empty<TMP_Text>();
            initialized = false;
        }

        private void Awake()
        {
            // Native taskbar, Start-menu and Show Desktop restores also enter here.
            // Do not force a selected tab when a previously opened window restores.
            if (window != null) window.onOpen.AddListener(HandleWindowOpened);
        }

        private void OnDestroy()
        {
            if (window != null) window.onOpen.RemoveListener(HandleWindowOpened);
        }

        private void Start()
        {
            // Construct hidden views once so desktop labels already have complete
            // CJK coverage before the user opens any of the five applications.
            EnsureApp();
        }

        public void OpenDefaultApp() => OpenApp();

        public void OpenApp(string tab = null)
        {
            if (customView != null)
            {
                if (window == null || !customView.EnsureReady()) return;
                window.OpenWindow();
                customView.OnOpened(tab);
                return;
            }
            if (!EnsureApp()) return;
            if (IsNativeChat)
            {
                window.OpenWindow();
                nativeChat.OpenConversation();
                return;
            }
            if (nativeApp != null)
            {
                var nativeTabs = nativeApp.AvailableTabs;
                nativeApp.ShowTab(string.IsNullOrWhiteSpace(tab) ? nativeTabs[0] : tab);
                window.OpenWindow();
                return;
            }
            var tabs = app.AvailableTabs;
            app.ShowTab(string.IsNullOrWhiteSpace(tab) ? tabs[0] : tab);
            window.OpenWindow();
        }

        public void CloseApp()
        {
            if (window != null && window.isOn) window.CloseWindow();
        }

        public void MinimizeApp()
        {
            if (window != null && window.isOn) window.MinimizeWindow();
        }

        public void ToggleMaximizeApp()
        {
            if (window != null && window.isOn) window.FullscreenWindow();
        }

        private void HandleWindowOpened()
        {
            if (customView != null) { if (customView.EnsureReady()) customView.OnOpened(null); return; }
            if (!EnsureApp()) return;
            if (IsNativeChat) nativeChat.OpenConversation();
            else if (nativeApp != null) nativeApp.Refresh();
            else app.Refresh();
        }

        private bool EnsureApp()
        {
            if (customView != null) return customView.EnsureReady();
            if (IsNativeChat)
            {
                if (runtime == null || window == null) return false;
                runtime.EnsureInitialized();
                nativeChat.EnsureInitialized();
                initialized = nativeChat.IsReady;
                // Native Messaging.Awake may not run until its first activation.
                // Opening the window must not be blocked by that initial state.
                return true;
            }
            if (nativeApp != null)
            {
                if (runtime == null || window == null || content == null) return false;
                runtime.EnsureInitialized();
                nativeApp.EnsureInitialized();
                if (!initialized)
                {
                    foreach (var label in window.GetComponentsInChildren<TMP_Text>(true)) nativeApp.ApplyNativeFont(label);
                    foreach (var label in shellLabels) if (label != null) nativeApp.ApplyNativeFont(label);
                    foreach (var button in window.GetComponentsInChildren<ButtonManager>(true)) AddFocusTarget(button.gameObject);
                    foreach (var selectable in window.GetComponentsInChildren<Selectable>(true)) AddFocusTarget(selectable.gameObject);
                    AddFocusTarget(content.gameObject);
                    if (window.windowDragger != null) AddFocusTarget(window.windowDragger.gameObject);
                }
                initialized = nativeApp.IsInitialized;
                return initialized;
            }
            if (initialized) return true;
            if (runtime == null || app == null || window == null || content == null || font == null)
            {
                Debug.LogError("灵光.exe 桌面接入缺少引用，请重新运行第一章安装器。", this);
                return false;
            }
            runtime.EnsureInitialized();
            app.ConfigureProfile(applicationId);
            app.Initialize(runtime, content, font);
            // The old desktop's static subset font is insufficient for the new
            // Chinese title/status. Borrow the app-owned dynamic font only here.
            foreach (var label in window.GetComponentsInChildren<TMP_Text>(true))
            {
                app.ApplyEffectiveFont(label);
                if (label.transform.parent != null && label.transform.parent.name.StartsWith("ChapterOne", System.StringComparison.Ordinal)
                    && label.transform.parent.GetComponent<Button>() != null && (label.name == "Label" || label.name == "RestoreLabel"))
                {
                    label.fontSize = 18;
                    label.overflowMode = TextOverflowModes.Overflow;
                }
            }
            foreach (var label in shellLabels)
                if (label != null) app.ApplyEffectiveFont(label);
            // uGUI buttons consume pointer-down before WindowManager can see it.
            // Focus on those same controls, not by polling the hardware mouse.
            foreach (var selectable in window.GetComponentsInChildren<Selectable>(true))
                AddFocusTarget(selectable.gameObject);
            AddFocusTarget(content.gameObject);
            if (window.windowDragger != null) AddFocusTarget(window.windowDragger.gameObject);
            initialized = true;
            return true;
        }

        private void AddFocusTarget(GameObject target)
        {
            var focus = target.GetComponent<ChapterOneWindowFocus>() ?? target.AddComponent<ChapterOneWindowFocus>();
            focus.Window = window;
        }
    }
}
