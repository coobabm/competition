using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Michsky.DreamOS
{
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

        void OnEnable()
        {
            if (initialized && openWindowsOnly && windowManager != null && windowManager.isOn)
            {
                buttonAnimator.enabled = true;
                buttonAnimator.Play("Active to Inactive");
            }
            else if (initialized && isPinned) { buttonAnimator.Play("Start Draw"); }
            else if (initialized && !isPinned) { buttonAnimator.Play("Start Hide"); }

            StartCoroutine("DisableAnimator");
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
                if (!DreamOSDataManager.ContainsJsonKey(dataCat, buttonTitle + "_TaskbarShortcut"))
                    DreamOSDataManager.WriteBooleanData(dataCat, buttonTitle + "_TaskbarShortcut", defaultPinState == DefaultPinState.Pinned);
                isPinned = DreamOSDataManager.ReadBooleanData(dataCat, buttonTitle + "_TaskbarShortcut");
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
                if (!isPinned) { pinButton.gameObject.SetActive(true); unpinButton.gameObject.SetActive(false); }
                else { pinButton.gameObject.SetActive(false); unpinButton.gameObject.SetActive(true); }

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
            if (eventData.button == PointerEventData.InputButton.Right) { AnimateContextMenu(); }
            else
            {
                if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Active to Highlighted")
                    && windowManager.transform.GetSiblingIndex() != windowManager.transform.parent.childCount - 1)
                {
                    windowManager.FocusToWindow();
                    buttonAnimator.Play("Inactive to Active");
                }

                else if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Active to Highlighted")
                    || buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Closed to Active")
                    || buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Inactive to Active")
                    || buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Highlighted to Active")
                    && windowManager.transform.GetSiblingIndex() == windowManager.transform.parent.childCount - 1)
                {
                    windowManager.MinimizeWindow();
                    buttonAnimator.Play("Active to Inactive");
                }

                else if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Inactive to Highlighted")
                    || buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Highlighted to Inactive")
                    || buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Active to Inactive"))
                {
                    onClick.Invoke();
                    windowManager.FocusToWindow();
                    buttonAnimator.Play("Highlighted to Active");
                }

                else if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Hide"))
                {
                    onClick.Invoke();
                    windowManager.FocusToWindow();
                    buttonAnimator.Play("Hide to Active");
                }

                else
                {
                    onClick.Invoke();
                    windowManager.FocusToWindow();
                    buttonAnimator.Play("Closed to Active");
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            StopCoroutine("DisableAnimator");
            buttonAnimator.enabled = true;

            if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Highlighted to Closed")
                || buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Active to Closed"))
                buttonAnimator.Play("Closed to Highlighted");

            else if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Highlighted to Active")
                || buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Closed to Active")
               || buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Inactive to Active")
                     || buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Hide to Active"))
                buttonAnimator.Play("Active to Highlighted");

            else if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Active to Inactive")
                || buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Highlighted to Inactive"))
                buttonAnimator.Play("Inactive to Highlighted");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Closed to Highlighted")) { buttonAnimator.Play("Highlighted to Closed"); }
            else if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Active to Highlighted")) { buttonAnimator.Play("Highlighted to Active"); }
            else if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Inactive to Highlighted")) { buttonAnimator.Play("Highlighted to Inactive"); }

            StartCoroutine("DisableAnimator");
        }

        public void SetOpen()
        {
            buttonAnimator.enabled = true;

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator");

            if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Inactive to Highlighted") || buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Active to Highlighted"))
                buttonAnimator.Play("Highlighted to Active");
            else if (buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Start Hide") || buttonAnimator.GetCurrentAnimatorStateInfo(0).IsName("Hide"))
                buttonAnimator.Play("Hide to Active");
            else
                buttonAnimator.Play("Closed to Active");
        }

        public void SetClosed()
        {
            buttonAnimator.enabled = true;

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator");

            if (!openWindowsOnly && DreamOSDataManager.ContainsJsonKey(dataCat, buttonTitle + "_TaskbarShortcut") && DreamOSDataManager.ReadBooleanData(dataCat, buttonTitle + "_TaskbarShortcut")) { buttonAnimator.Play("Active to Closed"); }
            else { buttonAnimator.Play("Hide"); }
        }

        public void SetMinimized()
        {
            buttonAnimator.enabled = true;
            buttonAnimator.Play("Active to Inactive");

            StopCoroutine("DisableAnimator");
            StartCoroutine("DisableAnimator");
        }

        public void PinTaskBarButton()
        {
            if (openWindowsOnly) { return; }
            if (!isPinned)
            {
                isPinned = true;
                DreamOSDataManager.WriteBooleanData(dataCat, buttonTitle + "_TaskbarShortcut", true);

                if (pinButton != null && unpinButton != null) { pinButton.gameObject.SetActive(false); unpinButton.gameObject.SetActive(true); }
            }

            else
            {
                if (windowManager.isOn == false) { buttonAnimator.Play("Hide"); }
                if (pinButton != null && unpinButton != null) { pinButton.gameObject.SetActive(true); unpinButton.gameObject.SetActive(false); }

                DreamOSDataManager.WriteBooleanData(dataCat, buttonTitle + "_TaskbarShortcut", false);
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

                StopCoroutine("DisableContextMenu");
                StartCoroutine("DisableContextMenu");

                if (contextBlur != null) { contextBlur.BlurOutAnim(); }
            }

            else if (!isContextOn)
            {
                contextMenu.gameObject.SetActive(true);
                isContextOn = true;
                contextMenu.Play("In");

                StopCoroutine("DisableContextMenu");

                if (contextBlur != null) { contextBlur.BlurInAnim(); }
                if (windowManager.isOn == true && closeButton != null) { closeButton.gameObject.SetActive(true); }
                else if (closeButton != null) { closeButton.gameObject.SetActive(false); }
            }
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