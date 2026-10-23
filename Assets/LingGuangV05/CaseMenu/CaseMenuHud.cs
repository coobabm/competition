using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LingGuangV05.CaseMenu
{
    /// <summary>Runtime overlay only; the menu controller owns input, audio and persistence.</summary>
    public sealed partial class CaseMenuHud : MonoBehaviour
    {
        private static readonly Color Ink = new Color(.94f, .91f, .85f, 1f);
        private static readonly Color Muted = new Color(.68f, .66f, .61f, 1f);
        private static readonly Color Amber = new Color(.86f, .67f, .40f, 1f);
        private static readonly Color Surface = new Color(.082f, .079f, .073f, 1f);
        private static readonly Color Control = new Color(.14f, .134f, .12f, 1f);
        private static readonly Color Teal = new Color32(90,214,180,255);

        // Opening title artwork (v9 clean lockup: 我的电脑 / [monitor icon 活]了). Reference pixels at 1920x1080.
        // Two lines at 500 px wide keep the same footprint the retired v5 art had, so the room framing and the
        // 8 px gap to the subheading keep working; the cleaner art simply has less transparent padding.
        private const float TitleLogoLeft = 123f;   // artwork padding puts visible ink at x~128
        private const float TitleLogoTop = -110f;
        private const float TitleLogoWidth = 500f;

        private TMP_FontAsset font;
        private RectTransform canvasRoot;
        private CanvasGroup chromeGroup;
        private CanvasGroup settingsGroup;
        private CanvasGroup fadeGroup;
        private RectTransform loadingRoot;
        private RectTransform loadingFill;
        private TextMeshProUGUI hintLabel;
        private TextMeshProUGUI loadingLabel;
        private TextMeshProUGUI volumeLabel;
        readonly UnityEngine.UI.Image[] volumeBars=new UnityEngine.UI.Image[10];
        private UnityEngine.UI.Slider volumeSlider;
        private UnityEngine.UI.Toggle reduceMotionToggle;
        private UnityEngine.UI.Toggle fullscreenToggle;
        private UnityEngine.UI.Toggle qualityToggle, frameRateToggle;
        private TextMeshProUGUI settingsStatus;
        private readonly System.Collections.Generic.List<Action> settingsTranslations = new System.Collections.Generic.List<Action>();
        private Action<float> volumeChanged;
        private Action<bool> reduceMotionChanged;
        private Action<bool> fullscreenChanged;
        private Action closeRequested;
        private Action uiClick;
        private Func<int?> lifeEventMode;
        private Action<int> lifeEventModeChanged;
        private UnityEngine.UI.Button lifeEventsButton;
        private TextMeshProUGUI lifeEventsTitle, lifeEventsDetail;
        private Func<int> offlineIncomeState;
        private Action<bool> offlineIncomeChanged;
        private UnityEngine.UI.Button offlineIncomeButton;
        private TextMeshProUGUI offlineIncomeTitle, offlineIncomeDetail;
        private int displayedVolume = -1;
        private int displayedProgress = -1;
        private string loadingMessage;

        public bool IsSettingsOpen => settingsGroup != null && settingsGroup.gameObject.activeSelf;
        static CaseMenuHud settingsOwner;
        // Both the title controller and direct-Main fallback display this same HUD.
        public static bool AnySettingsOpen => settingsOwner != null && settingsOwner.gameObject.activeInHierarchy && settingsOwner.IsSettingsOpen;

        public bool HasSelectedControl
        {
            get
            {
                var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
                return IsSettingsOpen && selected != null && selected.transform.IsChildOf(settingsGroup.transform);
            }
        }

        public void Build(TMP_FontAsset font, Action<float> onVolume, Action<bool> onReduceMotion,
            Action<bool> onFullscreen, Action onClose, Action onUIClick)
        {
            volumeChanged = onVolume;
            reduceMotionChanged = onReduceMotion;
            fullscreenChanged = onFullscreen;
            closeRequested = onClose;
            uiClick = onUIClick;
            if (canvasRoot != null) return;

            // Title and .exe share MiSans (UiSans) so the first screen already looks like the game's own UI;
            // the scene's Desktop CJK font stays the fallback when the UiV4 assets are unavailable.
            var uiSans = LingGuangV05.Desktop.XingGuang.XgUiV4Fonts.Sans;
            this.font = uiSans != null ? uiSans : font != null ? font : TMP_Settings.defaultFontAsset;
            canvasRoot = Rect("CaseMenuOverlay", transform);
            var canvas = canvasRoot.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = canvasRoot.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            canvasRoot.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            BuildChrome();
            BuildLoading();
            BuildSettings();
            BuildTitleActions();

            // Last sibling: transition black covers the entire menu, including its modal.
            var fade = Panel("TransitionFade", canvasRoot, Color.black, true);
            Stretch(fade.rectTransform);
            fadeGroup = fade.gameObject.AddComponent<CanvasGroup>();
            SetFade(0f);
        }

        public void SetHint(string text)
        {
            if (hintLabel == null) return;
            var value = text ?? string.Empty;
            if (hintLabel.text != value) hintLabel.text = value;
        }

        public void SetChromeAlpha(float alpha)
        {
            if (chromeGroup != null) chromeGroup.alpha = Unit(alpha);
            SetTitleActionsVisible(alpha >= .999f);
        }

        // Keep the legal notice, but remove the title lockup and title-only hints behind subpages.
        void SetSubpageChrome(bool subpage)
        {
            if(chromeGroup!=null)foreach(Transform child in chromeGroup.transform)
            {
                if(child.name.StartsWith("AiContentNotice",StringComparison.Ordinal))continue;
                var group=child.GetComponent<CanvasGroup>();
                if(group==null)group=child.gameObject.AddComponent<CanvasGroup>();
                group.alpha=subpage?0:1;
            }
            if(titleActions!=null){titleActions.alpha=subpage?0:(chromeGroup!=null?chromeGroup.alpha:1);titleActions.blocksRaycasts=!subpage&&titleActions.interactable;}
        }

        public void SetFade(float alpha)
        {
            if (fadeGroup == null) return;
            alpha = Unit(alpha);
            fadeGroup.alpha = alpha;
            fadeGroup.blocksRaycasts = alpha > 0f;
            fadeGroup.interactable = alpha > 0f;
        }

        /// <summary>A negative progress hides the loading readout; normal progress is 0..1.</summary>
        public void SetLoading(float progress, string message)
        {
            if (loadingRoot == null) return;
            if (progress < 0f)
            {
                loadingRoot.gameObject.SetActive(false);
                return;
            }

            if (!loadingRoot.gameObject.activeSelf) loadingRoot.gameObject.SetActive(true);
            progress = Unit(progress);
            int percent = Mathf.RoundToInt(progress * 100f);
            message = string.IsNullOrEmpty(message) ? "正在启动" : message;
            if (displayedProgress != percent || loadingMessage != message)
            {
                displayedProgress = percent;
                loadingMessage = message;
                loadingLabel.text = message + "   " + percent + "%";
            }
            loadingFill.anchorMax = new Vector2(progress, 1f);
        }

        public void ShowError(string message)
        {
            if (loadingRoot != null) loadingRoot.gameObject.SetActive(false);
            SetHint(message);
            SetChromeAlpha(1f);
        }

        public void ConfigureLifeEvents(Func<int?> readMode, Action<int> writeMode)
        {
            lifeEventMode = readMode; lifeEventModeChanged = writeMode;
            RefreshLifeEventsSetting();
        }
        public void RefreshLifeEventsSetting()
        {
            if (lifeEventsButton == null) return;
            int? mode = lifeEventMode?.Invoke();
            lifeEventsButton.interactable = mode.HasValue && mode.Value >= 0 && mode.Value <= 2 && lifeEventModeChanged != null;
            string value = !mode.HasValue ? "" : mode.Value == 1 ? T("每 15 分钟", "Every 15 min")
                : mode.Value == 2 ? T("关闭", "Off") : T("每 8 分钟", "Every 8 min");
            lifeEventsTitle.text = T("突发事件", "Life events") + (value.Length > 0 ? " · " + value : "");
            lifeEventsDetail.text = "";
        }
        private UnityEngine.UI.Button BuildLifeEventsSetting(RectTransform parent)
        {
            var background = Panel("LifeEvents", parent, Color.clear, true);
            Place(background.rectTransform, new Vector2(0, 1), new Vector2(42, -479), new Vector2(536, 56));
            lifeEventsButton = background.gameObject.AddComponent<UnityEngine.UI.Button>();
            Style(lifeEventsButton, background);
            lifeEventsTitle = Label("Label", background.transform, "", 26, Ink);
            Place(lifeEventsTitle.rectTransform, new Vector2(0, 1), new Vector2(14, -5), new Vector2(508, 27));
            lifeEventsDetail = Label("Description", background.transform, "", 12, Muted);
            Place(lifeEventsDetail.rectTransform, new Vector2(0, 0), new Vector2(14, 5), new Vector2(508, 22));
            lifeEventsButton.onClick.AddListener(() =>
            {
                int? mode = lifeEventMode?.Invoke();
                if (!mode.HasValue || mode.Value < 0 || mode.Value > 2 || lifeEventModeChanged == null) return;
                lifeEventModeChanged((mode.Value + 1) % 3);
                RefreshLifeEventsSetting(); uiClick?.Invoke();
            });
            RefreshLifeEventsSetting(); return lifeEventsButton;
        }

        /// <summary>readState: -1 no game loaded, 0 locked, 1 off, 2 on.</summary>
        public void ConfigureOfflineIncome(Func<int> readState, Action<bool> writeState)
        {
            offlineIncomeState = readState; offlineIncomeChanged = writeState;
            RefreshOfflineIncomeSetting();
        }
        public void RefreshOfflineIncomeSetting()
        {
            if (offlineIncomeButton == null) return;
            int state = offlineIncomeState?.Invoke() ?? -1;
            offlineIncomeButton.interactable = state >= 1 && offlineIncomeChanged != null;
            string value = state == 2 ? T("开启", "On") : state == 1 ? T("关闭", "Off") : state == 0 ? T("未解锁", "Locked") : "";
            offlineIncomeTitle.text = T("离线收益", "Offline income") + (value.Length > 0 ? " · " + value : "");
            offlineIncomeTitle.color = state >= 1 ? Ink : Muted;
            string pct = (LingGuangV05.Core.XgOfflineRules.Efficiency * 100).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%";
            string hours = (LingGuangV05.Core.XgOfflineRules.MaxSeconds / 3600).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            offlineIncomeDetail.text = state == 0 ? T("解锁自动答题后自动开启。", "Turns on once automatic answering is unlocked.")
                : state < 0 ? T("进入游戏后可设置。", "Available once a game is loaded.")
                : T("退出游戏后，下次启动补算挂机收入。按" + pct + "效率，最多" + hours + "小时。仅增加游戏资金。",
                    "Idle income is settled on your next launch: " + pct + " efficiency, up to " + hours + " hours. Game funds only.");
        }
        private UnityEngine.UI.Button BuildOfflineIncomeSetting(RectTransform parent)
        {
            var background = Panel("OfflineIncome", parent, Color.clear, true);
            Place(background.rectTransform, new Vector2(0, 1), new Vector2(42, -547), new Vector2(900, 56));
            offlineIncomeButton = background.gameObject.AddComponent<UnityEngine.UI.Button>();
            Style(offlineIncomeButton, background);
            offlineIncomeTitle = Label("Label", background.transform, "", 26, Ink);
            Place(offlineIncomeTitle.rectTransform, new Vector2(0, 1), new Vector2(14, -5), new Vector2(872, 27));
            offlineIncomeDetail = Label("Description", background.transform, "", 12, Muted);
            Place(offlineIncomeDetail.rectTransform, new Vector2(0, 0), new Vector2(14, 5), new Vector2(872, 22));
            offlineIncomeButton.onClick.AddListener(() =>
            {
                int state = offlineIncomeState?.Invoke() ?? -1;
                if (state < 1 || offlineIncomeChanged == null) return;
                offlineIncomeChanged(state != 2);
                RefreshOfflineIncomeSetting(); uiClick?.Invoke();
            });
            RefreshOfflineIncomeSetting(); return offlineIncomeButton;
        }

        public void ShowSettings(float volume, bool reduceMotion, bool fullscreen)
        {
            if (settingsGroup == null) return;
            foreach (var refresh in settingsTranslations) refresh();
            ClosePlayerHelp(false);
            RefreshPlayerHelpEntry();
            volumeSlider.SetValueWithoutNotify(Unit(volume));
            reduceMotionToggle.SetIsOnWithoutNotify(reduceMotion);
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
            qualityToggle.SetIsOnWithoutNotify(!LingGuangV05.Desktop.GamePerformance.Low);
            frameRateToggle.SetIsOnWithoutNotify(LingGuangV05.Desktop.GamePerformance.Frames == 60);
            settingsStatus.text = "";
            foreach(var refresh in settingsTranslations)refresh();
            SetVolumeLabel(volumeSlider.value);
            RefreshLifeEventsSetting();
            RefreshOfflineIncomeSetting();
            SetTitleActionsVisible(false);
            SetSubpageChrome(true);
            settingsGroup.gameObject.SetActive(true);
            settingsOwner = this;
            settingsGroup.alpha = 1f;
            settingsGroup.interactable = true;
            settingsGroup.blocksRaycasts = true;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(volumeSlider.gameObject);
            }
        }

        public void ShowSettingsError(string message)
        {
            if (settingsStatus != null) settingsStatus.text = message;
        }

        public void HideSettings()
        {
            if (settingsOwner == this) settingsOwner = null;
            if (settingsGroup == null) return;
            ClosePlayerHelp(false);
            if (IsSettingsOpen && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            settingsGroup.interactable = false;
            settingsGroup.blocksRaycasts = false;
            settingsGroup.gameObject.SetActive(false);
            SetTitleActionsVisible(chromeGroup != null && chromeGroup.alpha >= .999f);
            SetSubpageChrome(false);
        }

        private void BuildChrome()
        {
            var chrome = Rect("Chrome", canvasRoot);
            Stretch(chrome);
            chromeGroup = chrome.gameObject.AddComponent<CanvasGroup>();
            chromeGroup.blocksRaycasts = false;
            chromeGroup.interactable = false;

            // First chrome child: shade the room edges underneath every title label.
            // The center stays transparent and the parent controls the normal title fade.
            var vignette = Rect("TitleVignette", chrome);
            Stretch(vignette);
            var vignetteGraphic = vignette.gameObject.AddComponent<CaseMenuVignetteGraphic>();
            vignetteGraphic.raycastTarget = false;

            var eyebrow = Label("Eyebrow", chrome, "MY COMPUTER IS ALIVE / 2016", 14, Muted);
            Place(eyebrow.rectTransform, new Vector2(0, 1), new Vector2(70, -66), new Vector2(520, 26));
            eyebrow.characterSpacing = 5f; eyebrow.gameObject.SetActive(false);
            // v9 clean lockup: 我的电脑[monitor icon carrying 活]了. The icon replaces the 活 character, so the
            // artwork replaces both the old text title and the separate signal decoration.
            var logoTexture = Resources.Load<Texture2D>("LingGuangV05/Branding/TitleLogoV9");
            if (logoTexture != null)
            {
                var logoRoot = Rect("TitleLogoV9", chrome);
                float logoHeight = TitleLogoWidth * logoTexture.height / logoTexture.width;
                Place(logoRoot, new Vector2(0, 1), new Vector2(TitleLogoLeft, TitleLogoTop),
                    new Vector2(TitleLogoWidth, logoHeight));
                var logo = logoRoot.gameObject.AddComponent<UnityEngine.UI.RawImage>();
                logo.texture = logoTexture;
                logo.color = Color.white;
                logo.raycastTarget = false;
            }
            else
            {
                var title = Label("Title", chrome, LingGuangV05.Core.AppNames.Game(LingGuangV05.Runtime.GameText.IsEnglish), 66, Ink);
                Place(title.rectTransform, new Vector2(0, 1), new Vector2(64, -98), new Vector2(610, 124));
                title.overflowMode = TextOverflowModes.Overflow;
                title.enableAutoSizing = true; title.fontSizeMin = 42; title.fontSizeMax = 66;
                title.characterSpacing = 3f;
                var titleFace = LingGuangV05.Desktop.XingGuang.XgUiV4Fonts.SansSemibold;
                if (titleFace != null) title.font = titleFace;
            }
            var accent = Panel("HintAccent", chrome, Amber); accent.gameObject.SetActive(false);
            Place(accent.rectTransform, Vector2.zero, new Vector2(70, 76), new Vector2(28, 2));
            hintLabel = Label("ContextHint", chrome, "", 21, Ink);
            Place(hintLabel.rectTransform, Vector2.zero, new Vector2(112, 59), new Vector2(880, 38));
            var keyboard = Label("KeyboardHint", chrome, "TAB 切换 · ENTER 确认", 15, Muted);
            Place(keyboard.rectTransform, new Vector2(1, 0), new Vector2(-70, 63), new Vector2(520, 30));
            keyboard.alignment = TextAlignmentOptions.MidlineRight; keyboard.gameObject.SetActive(false);
            // Match the reference footer: muted text without a full-width dark strip.
            var disclosureBacking = Panel("AiContentNoticeBacking", chrome, Color.clear);
            Place(disclosureBacking.rectTransform, Vector2.zero, new Vector2(62, 18), new Vector2(1136, 34));
            var disclosure = Label("AiContentNoticeBody", chrome,
                LingGuangV05.Runtime.AiContentNotice.Description(LingGuangV05.Runtime.GameText.IsEnglish), 18, new Color32(128, 126, 119, 255));
            Place(disclosure.rectTransform, Vector2.zero, new Vector2(128, 40), new Vector2(1120, 26));
            disclosure.enableAutoSizing = false;
            disclosure.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private void BuildLoading()
        {
            loadingRoot = Rect("Loading", canvasRoot);
            Place(loadingRoot, new Vector2(.5f, 0), new Vector2(0, 122), new Vector2(440, 48));
            var group = loadingRoot.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            loadingLabel = Label("LoadingLabel", loadingRoot, string.Empty, 16, Ink);
            Place(loadingLabel.rectTransform, new Vector2(.5f, 1), Vector2.zero, new Vector2(440, 30));
            loadingLabel.alignment = TextAlignmentOptions.Midline;
            var track = Panel("ProgressTrack", loadingRoot, new Color(.8f, .74f, .64f, .18f));
            Place(track.rectTransform, new Vector2(.5f, 0), Vector2.zero, new Vector2(360, 2));
            loadingFill = Panel("ProgressFill", track.transform, Amber).rectTransform;
            Stretch(loadingFill);
            loadingFill.anchorMax = new Vector2(0, 1);
            loadingRoot.gameObject.SetActive(false);
        }

        private void BuildSettings()
        {
            var backdrop = Panel("Settings", canvasRoot, new Color(0, 0, 0, .72f), true);
            Stretch(backdrop.rectTransform);
            settingsGroup = backdrop.gameObject.AddComponent<CanvasGroup>();
            var card = Panel("SettingsCard", backdrop.transform, Color.clear, true);
            Place(card.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1000, 760));
            var accent = Panel("CardAccent", card.transform, Amber);
            Place(accent.rectTransform, new Vector2(0, 1), new Vector2(42, -29), new Vector2(28, 2));

            var eyebrow = Label("SettingsEyebrow", card.transform, "SYSTEM CONFIGURATION", 11, Muted);
            Place(eyebrow.rectTransform, new Vector2(0, 1), new Vector2(84, -19), new Vector2(494, 23));
            eyebrow.characterSpacing = 2.7f; eyebrow.gameObject.SetActive(false);
            var title = SettingsLabel("SettingsTitle", card.transform, "设置", "Settings", 52, Ink);
            // 70 px holds Desktop CJK's full 52 pt line box (68.96); top raised 3 px so the line's middle stays at -87.
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(42, -52), new Vector2(900, 70));
            title.overflowMode = TextOverflowModes.Overflow;title.gameObject.AddComponent<CaseMenuPageCaret>();
            var separator = Panel("HeaderRule", card.transform, new Color(1, 1, 1, .13f));
            Place(separator.rectTransform, new Vector2(0, 1), new Vector2(42, -113), new Vector2(536, 1));

            BuildVolume(card.rectTransform);
            reduceMotionToggle = BuildToggle(card.rectTransform, "ReduceMotion", "减少动态效果", "Reduce motion", "简化镜头转场与按钮光效", "Simplify camera transitions and button lighting", 207);
            reduceMotionToggle.onValueChanged.AddListener(value => { reduceMotionChanged?.Invoke(value); uiClick?.Invoke(); });
            fullscreenToggle = BuildToggle(card.rectTransform, "Fullscreen", "全屏显示", "Fullscreen", "切换窗口与全屏显示", "Switch between windowed and fullscreen display", 275);
            fullscreenToggle.onValueChanged.AddListener(value => { fullscreenChanged?.Invoke(value); uiClick?.Invoke(); });
            var quality = qualityToggle = BuildToggle(card.rectTransform, "HighQuality", "画面质量", "Graphics", "开启为高画质；关闭降低阴影与细节", "On: high quality. Off: fewer shadows and details.", 343);
            quality.SetIsOnWithoutNotify(!LingGuangV05.Desktop.GamePerformance.Low);
            quality.onValueChanged.AddListener(value => LingGuangV05.Desktop.GamePerformance.SetLow(!value));
            var fps = frameRateToggle = BuildToggle(card.rectTransform, "FrameRate", "帧率", "Frame rate", "开启为 60 帧；关闭为 30 帧", "On: 60 FPS. Off: 30 FPS.", 411);
            fps.SetIsOnWithoutNotify(LingGuangV05.Desktop.GamePerformance.Frames == 60);
            fps.onValueChanged.AddListener(LingGuangV05.Desktop.GamePerformance.SetSixty);

            var lifeEvents = BuildLifeEventsSetting(card.rectTransform);
            var offlineIncome = BuildOfflineIncomeSetting(card.rectTransform);
            var playerHelp = BuildPlayerHelpEntry(card.rectTransform);
            var returnImage = Panel("Return", card.transform, Color.clear, true);
            Place(returnImage.rectTransform, new Vector2(0, 1), new Vector2(42, -625), new Vector2(142, 44));
            var returnButton = returnImage.gameObject.AddComponent<UnityEngine.UI.Button>();
            Style(returnButton, returnImage);
            returnButton.onClick.AddListener(() => { uiClick?.Invoke(); closeRequested?.Invoke(); });
            var returnLabel = SettingsLabel("Label", returnImage.transform, "返回", "Back", 18, Ink);
            Stretch(returnLabel.rectTransform);
            returnLabel.alignment = TextAlignmentOptions.Midline;
            var escape = SettingsLabel("EscapeHint", card.transform, "ESC 关闭", "ESC Close", 14, Muted);
            Place(escape.rectTransform, new Vector2(1, 1), new Vector2(-42, -630), new Vector2(180, 34));
            escape.alignment = TextAlignmentOptions.MidlineRight; escape.gameObject.SetActive(false);
            var saved = settingsStatus = Label("SaveHint", card.transform, "", 20, Amber);
            Place(saved.rectTransform, Vector2.zero, new Vector2(42, 22), new Vector2(536, 24));
            saved.enableAutoSizing = false;

            // Explicit vertical navigation keeps the modal's keyboard focus contained.
            Link(volumeSlider, returnButton, reduceMotionToggle);
            Link(reduceMotionToggle, volumeSlider, fullscreenToggle);
            Link(fullscreenToggle, reduceMotionToggle, quality);
            Link(quality, fullscreenToggle, fps);
            Link(fps, quality, lifeEvents);
            Link(lifeEvents, fps, offlineIncome);
            Link(offlineIncome, lifeEvents, playerHelp);
            Link(playerHelp, offlineIncome, returnButton);
            Link(returnButton, playerHelp, volumeSlider);
            HideSettings();
        }

        private void BuildVolume(RectTransform parent)
        {
            var root = Rect("MasterVolume", parent);
            Place(root, new Vector2(0, 1), new Vector2(42, -132), new Vector2(536, 64));
            var label = SettingsLabel("Label", root, "主音量", "Master volume", 18, Ink);
            Place(label.rectTransform, new Vector2(0, 1), Vector2.zero, new Vector2(340, 28));
            volumeLabel = Label("Value", root, "100%", 16, Muted);
            Place(volumeLabel.rectTransform, Vector2.one, Vector2.zero, new Vector2(150, 28));
            volumeLabel.alignment = TextAlignmentOptions.MidlineRight;

            var sliderHit = Panel("VolumeSlider", root, new Color(0, 0, 0, 0), true);
            Place(sliderHit.rectTransform, new Vector2(0, 1), new Vector2(0, -33), new Vector2(536, 30));
            volumeSlider = sliderHit.gameObject.AddComponent<UnityEngine.UI.Slider>();
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.wholeNumbers = false;
            volumeSlider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;

            var track = Panel("Track", sliderHit.transform, new Color(.30f, .28f, .24f, 1f));
            Place(track.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(516, 3));
            var fill = Panel("Fill", track.transform, Amber);
            Stretch(fill.rectTransform);
            volumeSlider.fillRect = fill.rectTransform;track.gameObject.SetActive(false);
            var handleArea = Rect("HandleArea", sliderHit.transform);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(10, 0);
            handleArea.offsetMax = new Vector2(-10, 0);
            var handle = Panel("Handle", handleArea, Ink);
            Place(handle.rectTransform, new Vector2(1f, .5f), Vector2.zero, new Vector2(12, 20));
            handle.rectTransform.pivot = new Vector2(.5f, .5f);
            // Keep the drag geometry, but only the ten volume bars are visible.
            volumeSlider.handleRect = handle.rectTransform;
            handle.enabled = false;
            volumeSlider.transition = UnityEngine.UI.Selectable.Transition.None;
            for(int i=0;i<10;i++){volumeBars[i]=Panel("VolumeBar"+i,sliderHit.transform,Teal);Place(volumeBars[i].rectTransform,new Vector2(0,.5f),new Vector2(16+i*50,0),new Vector2(24,14+i*2));}
            volumeSlider.onValueChanged.AddListener(value => { SetVolumeLabel(value); volumeChanged?.Invoke(value); });
        }

        private TextMeshProUGUI SettingsLabel(string name, Transform parent, string zh, string en, float size, Color color)
        {
            var label = Label(name, parent, T(zh, en), size, color);
            settingsTranslations.Add(() => label.text = T(zh, en));
            return label;
        }

        private UnityEngine.UI.Toggle BuildToggle(RectTransform parent, string name, string title, string titleEn, string subtitle, string subtitleEn, float top)
        {
            var background = Panel(name, parent, Color.clear, true);
            Place(background.rectTransform, new Vector2(0, 1), new Vector2(42, -top), new Vector2(900, 56));
            var toggle = background.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            toggle.transition = UnityEngine.UI.Selectable.Transition.None;
            var titleLabel = SettingsLabel("Label", background.transform, title, titleEn, 26, Ink);
            Place(titleLabel.rectTransform, new Vector2(0, .5f), Vector2.zero, new Vector2(480, 48));
            string on = name == "HighQuality" ? T("高", "High") : name == "FrameRate" ? "60" : T("开启", "On");
            string off = name == "HighQuality" ? T("低", "Low") : name == "FrameRate" ? "30" : T("关闭", "Off");
            var onText = Label("OnChoice", background.transform, on, 26, Ink);
            Place(onText.rectTransform,new Vector2(1,.5f),new Vector2(-160,0),new Vector2(120,48));
            var offText = Label("OffChoice", background.transform, off, 26, Ink);
            Place(offText.rectTransform,new Vector2(1,.5f),Vector2.zero,new Vector2(120,48));
            onText.raycastTarget=offText.raycastTarget=true;
            var onButton=onText.gameObject.AddComponent<UnityEngine.UI.Button>();onButton.targetGraphic=onText;onButton.onClick.AddListener(()=>toggle.isOn=true);
            var offButton=offText.gameObject.AddComponent<UnityEngine.UI.Button>();offButton.targetGraphic=offText;offButton.onClick.AddListener(()=>toggle.isOn=false);
            var mark = Panel("SelectedUnderline", background.transform, Teal);
            Action refresh = ()=>{onText.color=toggle.isOn?Ink:Muted*.65f;offText.color=toggle.isOn?Muted*.65f:Ink;Place(mark.rectTransform,new Vector2(1,0),new Vector2(toggle.isOn?-160:0,0),new Vector2(120,3));};
            toggle.onValueChanged.AddListener(v=>refresh());
            settingsTranslations.Add(refresh);
            toggle.toggleTransition = UnityEngine.UI.Toggle.ToggleTransition.None;
            return toggle;
        }

        private void SetVolumeLabel(float value)
        {
            int percent = Mathf.RoundToInt(Unit(value) * 100f);
            if (volumeLabel == null || displayedVolume == percent) return;
            displayedVolume = percent;
            volumeLabel.text = percent + "%";
            for(int i=0;i<volumeBars.Length;i++)if(volumeBars[i]!=null)volumeBars[i].color=i<Mathf.CeilToInt(value*10)?Teal:new Color(.25f,.27f,.26f,1);
        }

        private TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color)
        {
            var label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.text = text;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableAutoSizing = false;
            label.raycastTarget = false;
            label.richText = false;
            return label;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static UnityEngine.UI.Image Panel(string name, Transform parent, Color color, bool raycast = false)
        {
            var image = Rect(name, parent).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Style(UnityEngine.UI.Selectable control, UnityEngine.UI.Graphic target)
        {
            var normal = target.color;
            target.color = Color.white;
            control.targetGraphic = target;
            control.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            var colors = UnityEngine.UI.ColorBlock.defaultColorBlock;
            colors.normalColor = normal;
            colors.highlightedColor = Color.Lerp(normal, Ink, .12f);
            colors.selectedColor = Color.Lerp(normal, Amber, .32f);
            colors.pressedColor = Color.Lerp(normal, Ink, .24f);
            colors.disabledColor = Color.Lerp(normal, Color.black, .45f);
            colors.fadeDuration = .09f;
            control.colors = colors;
        }

        private static void Link(UnityEngine.UI.Selectable current, UnityEngine.UI.Selectable previous, UnityEngine.UI.Selectable next)
        {
            var navigation = new UnityEngine.UI.Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnUp = previous,
                selectOnDown = next
            };
            current.navigation = navigation;
        }

        private static float Unit(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp01(value);
        }
    }
}


namespace LingGuangV05.CaseMenu
{
    // Original static vertex-alpha gradient; no image asset, material allocation or per-frame work.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CaseMenuVignetteGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            const int columns = 32, rows = 18;
            Rect rect = rectTransform.rect;
            for (int y = 0; y <= rows; y++)
                for (int x = 0; x <= columns; x++)
                {
                    float u = x / (float)columns, v = y / (float)rows;
                    float radius = new Vector2(u * 2 - 1, v * 2 - 1).magnitude;
                    float fade = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.38f, 1.08f, radius));
                    vh.AddVert(new Vector3(rect.xMin + rect.width * u, rect.yMin + rect.height * v, 0),
                        new Color(0, 0, 0, .65f * fade), Vector2.zero);
                }
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    int i = y * (columns + 1) + x;
                    vh.AddTriangle(i, i + columns + 1, i + 1);
                    vh.AddTriangle(i + 1, i + columns + 1, i + columns + 2);
                }
        }
    }

    // One static mesh. Shapes are original geometry, not text glyphs or external assets.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CaseMenuTitleSignalGraphic : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            Vector2 origin = rectTransform.rect.min;
            Color faint = color; faint.a *= .35f;
            Color live = color; live.a *= .78f;
            // Open power ring; the top gap and stem remain legible when scaled down.
            var center = origin + new Vector2(6, 6);
            const int segments = 24;
            for (int i = 0; i < segments; i++)
            {
                float a = (135 + 270f * i / segments) * Mathf.Deg2Rad;
                float b = (135 + 270f * (i + 1) / segments) * Mathf.Deg2Rad;
                Stroke(vh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 4.5f,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 4.5f, live, 1.5f);
            }
            Stroke(vh, center, center + new Vector2(0, 5), live, 1.5f);
            Stroke(vh, origin + new Vector2(20, 6), origin + new Vector2(126, 6), faint, 1.5f);
            Stroke(vh, origin + new Vector2(126, 6), origin + new Vector2(132, 10), live, 1.5f);
            Stroke(vh, origin + new Vector2(132, 10), origin + new Vector2(153, 10), live, 1.5f);
            Stroke(vh, origin + new Vector2(153, 10), origin + new Vector2(159, 6), live, 1.5f);
            Stroke(vh, origin + new Vector2(159, 6), origin + new Vector2(326, 6), faint, 1.5f);
            Stroke(vh, origin + new Vector2(56, 6), origin + new Vector2(64, 2), faint, 1.25f);
            Dot(vh, origin + new Vector2(64, 2), live, 1.5f);
            Dot(vh, origin + new Vector2(102, 6), live, 1.5f);
            // A steady terminal caret, deliberately not blinking.
            Stroke(vh, origin + new Vector2(342, 2), origin + new Vector2(342, 10), live, 2.5f);
        }

        static void Stroke(UnityEngine.UI.VertexHelper vh, Vector2 a, Vector2 b, Color ink, float width)
        {
            Vector2 direction = (b - a).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * (width * .5f);
            int start = vh.currentVertCount;
            vh.AddVert(a - normal, ink, Vector2.zero); vh.AddVert(a + normal, ink, Vector2.zero);
            vh.AddVert(b + normal, ink, Vector2.zero); vh.AddVert(b - normal, ink, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }

        static void Dot(UnityEngine.UI.VertexHelper vh, Vector2 center, Color ink, float radius)
        {
            int start = vh.currentVertCount;
            vh.AddVert(center + Vector2.up * radius, ink, Vector2.zero);
            vh.AddVert(center + Vector2.right * radius, ink, Vector2.zero);
            vh.AddVert(center + Vector2.down * radius, ink, Vector2.zero);
            vh.AddVert(center + Vector2.left * radius, ink, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
