using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LingGuangV05.CaseMenu
{
    /// <summary>Runtime overlay only; the menu controller owns input, audio and persistence.</summary>
    public sealed class CaseMenuHud : MonoBehaviour
    {
        private static readonly Color Ink = new Color(.94f, .91f, .85f, 1f);
        private static readonly Color Muted = new Color(.68f, .66f, .61f, 1f);
        private static readonly Color Amber = new Color(.86f, .67f, .40f, 1f);
        private static readonly Color Surface = new Color(.082f, .079f, .073f, 1f);
        private static readonly Color Control = new Color(.14f, .134f, .12f, 1f);

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
        private UnityEngine.UI.Slider volumeSlider;
        private UnityEngine.UI.Toggle reduceMotionToggle;
        private UnityEngine.UI.Toggle fullscreenToggle;
        private Action<float> volumeChanged;
        private Action<bool> reduceMotionChanged;
        private Action<bool> fullscreenChanged;
        private Action closeRequested;
        private Action uiClick;
        private int displayedVolume = -1;
        private int displayedProgress = -1;
        private string loadingMessage;

        public bool IsSettingsOpen => settingsGroup != null && settingsGroup.gameObject.activeSelf;

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

            this.font = font != null ? font : TMP_Settings.defaultFontAsset;
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

        public void ShowSettings(float volume, bool reduceMotion, bool fullscreen)
        {
            if (settingsGroup == null) return;
            volumeSlider.SetValueWithoutNotify(Unit(volume));
            reduceMotionToggle.SetIsOnWithoutNotify(reduceMotion);
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
            SetVolumeLabel(volumeSlider.value);
            settingsGroup.gameObject.SetActive(true);
            settingsGroup.alpha = 1f;
            settingsGroup.interactable = true;
            settingsGroup.blocksRaycasts = true;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(volumeSlider.gameObject);
            }
        }

        public void HideSettings()
        {
            if (settingsGroup == null) return;
            if (IsSettingsOpen && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            settingsGroup.interactable = false;
            settingsGroup.blocksRaycasts = false;
            settingsGroup.gameObject.SetActive(false);
        }

        private void BuildChrome()
        {
            var chrome = Rect("Chrome", canvasRoot);
            Stretch(chrome);
            chromeGroup = chrome.gameObject.AddComponent<CanvasGroup>();
            chromeGroup.blocksRaycasts = false;
            chromeGroup.interactable = false;

            var eyebrow = Label("Eyebrow", chrome, "LING GUANG / 2016", 14, Muted);
            Place(eyebrow.rectTransform, new Vector2(0, 1), new Vector2(70, -66), new Vector2(520, 26));
            eyebrow.characterSpacing = 5f;
            var title = Label("Title", chrome, "灵光", 78, Ink);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(64, -98), new Vector2(520, 124));
            title.overflowMode = TextOverflowModes.Overflow;
            title.characterSpacing = 13f;
            var subheading = Label("Subheading", chrome, "从一次开机开始。", 21, Muted);
            Place(subheading.rectTransform, new Vector2(0, 1), new Vector2(70, -234), new Vector2(640, 36));

            var accent = Panel("HintAccent", chrome, Amber);
            Place(accent.rectTransform, Vector2.zero, new Vector2(70, 76), new Vector2(28, 2));
            hintLabel = Label("ContextHint", chrome, "点击机箱上的电源键，进入游戏", 21, Ink);
            Place(hintLabel.rectTransform, Vector2.zero, new Vector2(112, 59), new Vector2(880, 38));
            var keyboard = Label("KeyboardHint", chrome, "TAB 切换 · ENTER 确认", 15, Muted);
            Place(keyboard.rectTransform, new Vector2(1, 0), new Vector2(-70, 63), new Vector2(520, 30));
            keyboard.alignment = TextAlignmentOptions.MidlineRight;
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
            var card = Panel("SettingsCard", backdrop.transform, Surface, true);
            Place(card.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(620, 460));
            var accent = Panel("CardAccent", card.transform, Amber);
            Place(accent.rectTransform, new Vector2(0, 1), new Vector2(42, -29), new Vector2(28, 2));

            var eyebrow = Label("SettingsEyebrow", card.transform, "SYSTEM CONFIGURATION", 11, Muted);
            Place(eyebrow.rectTransform, new Vector2(0, 1), new Vector2(84, -19), new Vector2(494, 23));
            eyebrow.characterSpacing = 2.7f;
            var title = Label("SettingsTitle", card.transform, "设置", 32, Ink);
            Place(title.rectTransform, new Vector2(0, 1), new Vector2(42, -55), new Vector2(536, 52));
            title.overflowMode = TextOverflowModes.Overflow;
            var separator = Panel("HeaderRule", card.transform, new Color(1, 1, 1, .13f));
            Place(separator.rectTransform, new Vector2(0, 1), new Vector2(42, -113), new Vector2(536, 1));

            BuildVolume(card.rectTransform);
            reduceMotionToggle = BuildToggle(card.rectTransform, "ReduceMotion", "减少动态效果", "简化镜头转场与按钮光效", 207);
            reduceMotionToggle.onValueChanged.AddListener(value => { reduceMotionChanged?.Invoke(value); uiClick?.Invoke(); });
            fullscreenToggle = BuildToggle(card.rectTransform, "Fullscreen", "全屏显示", "切换窗口与全屏显示", 275);
            fullscreenToggle.onValueChanged.AddListener(value => { fullscreenChanged?.Invoke(value); uiClick?.Invoke(); });

            var returnImage = Panel("Return", card.transform, Control, true);
            Place(returnImage.rectTransform, new Vector2(0, 1), new Vector2(42, -353), new Vector2(142, 44));
            var returnButton = returnImage.gameObject.AddComponent<UnityEngine.UI.Button>();
            Style(returnButton, returnImage);
            returnButton.onClick.AddListener(() => { uiClick?.Invoke(); closeRequested?.Invoke(); });
            var returnLabel = Label("Label", returnImage.transform, "返回", 18, Ink);
            Stretch(returnLabel.rectTransform);
            returnLabel.alignment = TextAlignmentOptions.Midline;
            var escape = Label("EscapeHint", card.transform, "ESC 关闭", 14, Muted);
            Place(escape.rectTransform, new Vector2(1, 1), new Vector2(-42, -358), new Vector2(320, 34));
            escape.alignment = TextAlignmentOptions.MidlineRight;
            var saved = Label("SaveHint", card.transform, "设置即时保存，不会重置游戏进度", 14, Muted);
            Place(saved.rectTransform, Vector2.zero, new Vector2(42, 22), new Vector2(536, 24));

            // Explicit vertical navigation keeps the modal's keyboard focus contained.
            Link(volumeSlider, returnButton, reduceMotionToggle);
            Link(reduceMotionToggle, volumeSlider, fullscreenToggle);
            Link(fullscreenToggle, reduceMotionToggle, returnButton);
            Link(returnButton, fullscreenToggle, volumeSlider);
            HideSettings();
        }

        private void BuildVolume(RectTransform parent)
        {
            var root = Rect("MasterVolume", parent);
            Place(root, new Vector2(0, 1), new Vector2(42, -132), new Vector2(536, 64));
            var label = Label("Label", root, "主音量", 18, Ink);
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
            volumeSlider.fillRect = fill.rectTransform;
            var handleArea = Rect("HandleArea", sliderHit.transform);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(10, 0);
            handleArea.offsetMax = new Vector2(-10, 0);
            var handle = Panel("Handle", handleArea, Ink);
            Place(handle.rectTransform, new Vector2(1f, .5f), Vector2.zero, new Vector2(12, 20));
            handle.rectTransform.pivot = new Vector2(.5f, .5f);
            volumeSlider.handleRect = handle.rectTransform;
            Style(volumeSlider, handle);
            var colors = volumeSlider.colors;
            colors.normalColor = Ink;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Amber;
            colors.pressedColor = Color.white;
            volumeSlider.colors = colors;
            volumeSlider.onValueChanged.AddListener(value => { SetVolumeLabel(value); volumeChanged?.Invoke(value); });
        }

        private UnityEngine.UI.Toggle BuildToggle(RectTransform parent, string name, string title, string subtitle, float top)
        {
            var background = Panel(name, parent, Control, true);
            Place(background.rectTransform, new Vector2(0, 1), new Vector2(42, -top), new Vector2(536, 56));
            var toggle = background.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            Style(toggle, background);
            var titleLabel = Label("Label", background.transform, title, 17, Ink);
            Place(titleLabel.rectTransform, new Vector2(0, 1), new Vector2(14, -5), new Vector2(450, 27));
            var detail = Label("Description", background.transform, subtitle, 12, Muted);
            Place(detail.rectTransform, new Vector2(0, 0), new Vector2(14, 5), new Vector2(450, 22));
            var box = Panel("Checkbox", background.transform, new Color(.46f, .43f, .36f, 1f));
            Place(box.rectTransform, new Vector2(1, .5f), new Vector2(-16, 0), new Vector2(22, 22));
            var inner = Panel("CheckboxInset", box.transform, Surface);
            Stretch(inner.rectTransform);
            inner.rectTransform.offsetMin = Vector2.one * 2;
            inner.rectTransform.offsetMax = Vector2.one * -2;
            var mark = Panel("Checkmark", inner.transform, Amber);
            Stretch(mark.rectTransform);
            mark.rectTransform.offsetMin = Vector2.one * 3;
            mark.rectTransform.offsetMax = Vector2.one * -3;
            toggle.graphic = mark;
            toggle.toggleTransition = UnityEngine.UI.Toggle.ToggleTransition.None;
            return toggle;
        }

        private void SetVolumeLabel(float value)
        {
            int percent = Mathf.RoundToInt(Unit(value) * 100f);
            if (volumeLabel == null || displayedVolume == percent) return;
            displayedVolume = percent;
            volumeLabel.text = percent + "%";
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
