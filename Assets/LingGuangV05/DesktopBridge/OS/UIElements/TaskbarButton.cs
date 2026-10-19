using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Desktop.Apps;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// One app's button on the taskbar. Its animator walks through Closed / Active / Inactive / Highlighted / Hide
    /// states as the app window opens, minimizes, gets focus or closes; a click opens, focuses or minimizes the
    /// window, a right click opens a small context menu (open, close, pin, unpin). Pinned buttons stay on the bar
    /// after the window closes, the choice is kept in the DreamOS Apps data as "&lt;title&gt;_TaskbarShortcut".
    /// Replaces DreamOS's TaskbarButton (2026-10-19): same GUID, serialized fields and public API.
    ///
    /// 灵光 additions: in open-windows-only mode the button ignores pinning and shows only while its window is open
    /// (visibility is driven from LateUpdate because the animator's write defaults keep restoring the hidden alpha),
    /// and Set* calls are ignored while the desktop has the taskbar deactivated (OnEnable restores the state).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class TaskbarButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler
    {
        // Resources
        public Animator buttonAnimator;
        [HideInInspector] public WindowManager windowManager;
        public List<AppElement> appElements = new List<AppElement>();

        // Settings
        public string buttonTitle = "App Window";
        [SerializeField] private DefaultPinState defaultPinState = DefaultPinState.Pinned;
        [SerializeField] private bool openWindowsOnly;
        public bool OpenWindowsOnly => openWindowsOnly;

        // Context Menu
        public Animator contextMenu;
        public UIBlur contextBlur;
        public ButtonManager headerButton;
        public ButtonManager closeButton;
        public ButtonManager pinButton;
        public ButtonManager unpinButton;
        bool isContextOn;

        // Events
        public UnityEvent onClick = new UnityEvent();

        // Helpers
        CanvasGroup visibilityGroup;
        bool initialized = false;
        bool isDragging = false;
        float cachedButtonLength = 0.5f;
        float cachedContextMenuLength = 0.5f;
        DreamOSDataManager.DataCategory dataCat = DreamOSDataManager.DataCategory.Apps;
        [HideInInspector] public bool isPinned;

        public enum DefaultPinState { Pinned, Unpinned }

        string ShortcutKey => buttonTitle + "_TaskbarShortcut";

        void OnEnable()
        {
            if (initialized && openWindowsOnly && windowManager != null && windowManager.isOn)
            {
                buttonAnimator.enabled = true;
                buttonAnimator.Play("Active to Inactive");
            }
            else if (initialized && isPinned) { buttonAnimator.Play("Start Draw"); }
            else if (initialized && !isPinned) { buttonAnimator.Play("Start Hide"); }

            StartCoroutine(nameof(DisableAnimator));
        }

        void LateUpdate()
        {
            if (!openWindowsOnly || visibilityGroup == null || windowManager == null) { return; }
            // Animator Write Defaults can restore the startup (hidden) alpha on
            // focus/minimize transitions. The window lifecycle owns visibility.
            bool visible = windowManager.isOn;
            visibilityGroup.alpha = visible ? 1f : 0f;
            visibilityGroup.interactable = visible;
            visibilityGroup.blocksRaycasts = visible;
        }

        public void InitializeButton()
        {
            visibilityGroup = GetComponent<CanvasGroup>();
            // Check if raycasting is available
            if (gameObject.GetComponent<Image>() == null)
            {
                Image raycastImg = gameObject.AddComponent<Image>();
                raycastImg.color = new Color(0, 0, 0, 0);
                raycastImg.raycastTarget = true;
            }

            if (buttonAnimator == null) { buttonAnimator = gameObject.GetComponent<Animator>(); }
            // Open-only mode deliberately ignores legacy pinned shortcuts, without
            // deleting the user's saved preferences for other desktop scenes.
            if (openWindowsOnly) { isPinned = false; }
            else
            {
                if (!DreamOSDataManager.ContainsJsonKey(dataCat, ShortcutKey))
                    DreamOSDataManager.WriteBooleanData(dataCat, ShortcutKey, defaultPinState == DefaultPinState.Pinned);
                isPinned = DreamOSDataManager.ReadBooleanData(dataCat, ShortcutKey);
            }

            for (int i = 0; i < appElements.Count; i++)
            {
                if (appElements[i] == null)
                    continue;

                appElements[i].appID = buttonTitle;
                appElements[i].UpdateLibrary();
                appElements[i].UpdateElement();
            }

            if (headerButton != null)
            {
                headerButton.onClick.AddListener(delegate
                {
                    windowManager.OpenWindow();
                    AnimateContextMenu();
                });
            }

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(false);
                closeButton.onClick.AddListener(delegate
                {
                    windowManager.CloseWindow();
                    AnimateContextMenu();
                });
            }

            if (openWindowsOnly)
            {
                if (pinButton != null) { pinButton.gameObject.SetActive(false); }
                if (unpinButton != null) { unpinButton.gameObject.SetActive(false); }
            }
            else if (pinButton != null && unpinButton != null)
            {
                pinButton.gameObject.SetActive(!isPinned);
                unpinButton.gameObject.SetActive(isPinned);

                pinButton.onClick.AddListener(() => PinTaskBarButton());
                unpinButton.onClick.AddListener(() => PinTaskBarButton());
            }

            onClick.AddListener(() => windowManager.OpenWindow());

            cachedButtonLength = DreamOSInternalTools.GetAnimatorClipLength(buttonAnimator, "TaskbarButton_HideToActive") + 0.1f;
            cachedContextMenuLength = DreamOSInternalTools.GetAnimatorClipLength(contextMenu, "ContextMenu_In") + 0.1f;

            initialized = true;
            if (openWindowsOnly && (windowManager == null || !windowManager.isOn))
            {
                buttonAnimator.enabled = true;
                buttonAnimator.Play("Start Hide");
            }
        }

        public void OnBeginDrag(PointerEventData eventData) { isDragging = true; }
        public void OnEndDrag(PointerEventData eventData) { isDragging = false; }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging || windowManager == null) { return; }
            if (eventData.button == PointerEventData.InputButton.Right) { AnimateContextMenu(); return; }

            if (IsState("Active to Highlighted") && !IsWindowOnTop())
            {
                // Open but behind other windows: bring it to the front
                windowManager.FocusToWindow();
                buttonAnimator.Play("Inactive to Active");
            }
            else if (IsState("Active to Highlighted") || IsState("Closed to Active") || IsState("Inactive to Active")
                || (IsState("Highlighted to Active") && IsWindowOnTop()))
            {
                // Already in front: minimize
                windowManager.MinimizeWindow();
                buttonAnimator.Play("Active to Inactive");
            }
            else if (IsState("Inactive to Highlighted") || IsState("Highlighted to Inactive") || IsState("Active to Inactive"))
            {
                OpenAndFocus("Highlighted to Active");
            }
            else if (IsState("Hide")) { OpenAndFocus("Hide to Active"); }
            else { OpenAndFocus("Closed to Active"); }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            StopCoroutine(nameof(DisableAnimator));
            buttonAnimator.enabled = true;

            if (IsState("Highlighted to Closed") || IsState("Active to Closed"))
                buttonAnimator.Play("Closed to Highlighted");

            else if (IsState("Highlighted to Active") || IsState("Closed to Active")
                || IsState("Inactive to Active") || IsState("Hide to Active"))
                buttonAnimator.Play("Active to Highlighted");

            else if (IsState("Active to Inactive") || IsState("Highlighted to Inactive"))
                buttonAnimator.Play("Inactive to Highlighted");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (IsState("Closed to Highlighted")) { buttonAnimator.Play("Highlighted to Closed"); }
            else if (IsState("Active to Highlighted")) { buttonAnimator.Play("Highlighted to Active"); }
            else if (IsState("Inactive to Highlighted")) { buttonAnimator.Play("Highlighted to Inactive"); }

            StartCoroutine(nameof(DisableAnimator));
        }

        public void SetOpen()
        {
            // Desktop transitions can hide the taskbar; OnEnable restores the window state.
            if (!isActiveAndEnabled) return;
            WakeAnimator();

            if (IsState("Inactive to Highlighted") || IsState("Active to Highlighted"))
                buttonAnimator.Play("Highlighted to Active");
            else if (IsState("Start Hide") || IsState("Hide"))
                buttonAnimator.Play("Hide to Active");
            else
                buttonAnimator.Play("Closed to Active");
        }

        public void SetClosed()
        {
            // Desktop transitions can hide the taskbar; OnEnable restores the window state.
            if (!isActiveAndEnabled) return;
            WakeAnimator();

            if (!openWindowsOnly && DreamOSDataManager.ContainsJsonKey(dataCat, ShortcutKey) && DreamOSDataManager.ReadBooleanData(dataCat, ShortcutKey)) { buttonAnimator.Play("Active to Closed"); }
            else { buttonAnimator.Play("Hide"); }
        }

        public void SetMinimized()
        {
            // Desktop transitions can hide the taskbar; OnEnable restores the window state.
            if (!isActiveAndEnabled) return;
            WakeAnimator();
            buttonAnimator.Play("Active to Inactive");
        }

        public void PinTaskBarButton()
        {
            if (openWindowsOnly) { return; }
            if (!isPinned)
            {
                isPinned = true;
                DreamOSDataManager.WriteBooleanData(dataCat, ShortcutKey, true);

                if (pinButton != null && unpinButton != null) { pinButton.gameObject.SetActive(false); unpinButton.gameObject.SetActive(true); }
            }
            else
            {
                if (!windowManager.isOn) { buttonAnimator.Play("Hide"); }
                if (pinButton != null && unpinButton != null) { pinButton.gameObject.SetActive(true); unpinButton.gameObject.SetActive(false); }

                DreamOSDataManager.WriteBooleanData(dataCat, ShortcutKey, false);
                isPinned = false;
            }

            AnimateContextMenu();
        }

        public void AnimateContextMenu()
        {
            if (isContextOn)
            {
                isContextOn = false;
                contextMenu.Play("Out");

                StopCoroutine(nameof(DisableContextMenu));
                StartCoroutine(nameof(DisableContextMenu));

                if (contextBlur != null) { contextBlur.BlurOutAnim(); }
            }
            else
            {
                contextMenu.gameObject.SetActive(true);
                isContextOn = true;
                contextMenu.Play("In");

                StopCoroutine(nameof(DisableContextMenu));

                if (contextBlur != null) { contextBlur.BlurInAnim(); }
                if (windowManager.isOn && closeButton != null) { closeButton.gameObject.SetActive(true); }
                else if (closeButton != null) { closeButton.gameObject.SetActive(false); }
            }
        }

        bool IsState(string stateName)
        {
            return buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
        }

        bool IsWindowOnTop()
        {
            Transform window = windowManager.transform;
            return window.GetSiblingIndex() == window.parent.childCount - 1;
        }

        void OpenAndFocus(string transition)
        {
            onClick.Invoke();
            windowManager.FocusToWindow();
            buttonAnimator.Play(transition);
        }

        // Turns the animator on and schedules it to switch off again once the transition has played
        void WakeAnimator()
        {
            buttonAnimator.enabled = true;

            StopCoroutine(nameof(DisableAnimator));
            StartCoroutine(nameof(DisableAnimator));
        }

        IEnumerator DisableAnimator()
        {
            yield return new WaitForSeconds(cachedButtonLength);
            buttonAnimator.enabled = false;
        }

        IEnumerator DisableContextMenu()
        {
            yield return new WaitForSeconds(cachedContextMenuLength);
            contextMenu.gameObject.SetActive(false);
        }
    }
}
