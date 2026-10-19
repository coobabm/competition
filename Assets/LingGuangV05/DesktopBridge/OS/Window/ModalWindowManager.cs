using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// A small modal dialog on the desktop: icon, title, description, confirm / cancel buttons, animated open and
    /// close, optional background blur (2026-10-19). Replaces DreamOS's ModalWindowManager: same GUID, serialized
    /// fields and public API.
    ///
    /// Confirm and cancel close the dialog unless turned off; the buttons simply forward to <see cref="onConfirm"/>
    /// and <see cref="onCancel"/>. When the dialog is localized its title and description take their text from the
    /// keys instead of <see cref="titleText"/> / <see cref="descriptionText"/>.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CanvasGroup))]
    public class ModalWindowManager : MonoBehaviour
    {
        // Resources
        public Image windowIcon;
        public TextMeshProUGUI windowTitle;
        public TextMeshProUGUI windowDescription;
        public ButtonManager confirmButton;
        public ButtonManager cancelButton;
        [SerializeField] private Animator mwAnimator;
        [SerializeField] private UIBlur backgroundBlur;

        // Content
        public Sprite icon;
        public string titleText = "Title";
        [TextArea] public string descriptionText = "Description here";

        // Localization
        public string titleKey;
        public string descriptionKey;

        // Settings
        public bool useCustomContent = false;
        public bool isOn = false;
        public bool closeOnCancel = true;
        public bool closeOnConfirm = true;
        public bool showCancelButton = true;
        public bool showConfirmButton = true;
        public bool useLocalization = true;
        [Range(0.5f, 2)] public float animationSpeed = 1;
        public StartBehaviour startBehaviour = StartBehaviour.Disable;
        public CloseBehaviour closeBehaviour = CloseBehaviour.Disable;
        public InputType inputType = InputType.Focused;

        // Events
        public UnityEvent onConfirm;
        public UnityEvent onCancel;
        public UnityEvent onOpen;
        public UnityEvent onClose;

        public enum StartBehaviour { Enable, Disable }
        public enum CloseBehaviour { Disable, Destroy }
        public enum InputType { Focused, Free }

        const string animIn = "In";
        const string animOut = "Out";
        const string animSpeedKey = "AnimSpeed";

        float openStateLength;
        float closeStateLength;

        void Awake()
        {
            InitModalWindow();
            UpdateUI();
        }

        void InitModalWindow()
        {
            if (mwAnimator == null) { mwAnimator = gameObject.GetComponent<Animator>(); }
            if (closeOnCancel) { onCancel.AddListener(CloseWindow); }
            if (closeOnConfirm) { onConfirm.AddListener(CloseWindow); }
            if (confirmButton != null) { confirmButton.onClick.AddListener(onConfirm.Invoke); }
            if (cancelButton != null) { cancelButton.onClick.AddListener(onCancel.Invoke); }

            if (useLocalization) { ApplyLocalizationKeys(); }

            // The clip lengths are read after the start behaviour on purpose: an Enable dialog opens in Awake
            // with openStateLength still 0, so its animator switches off after 0.1 s.
            if (startBehaviour == StartBehaviour.Disable) { isOn = false; gameObject.SetActive(false); }
            else if (startBehaviour == StartBehaviour.Enable) { isOn = false; OpenWindow(); }

            openStateLength = DreamOSInternalTools.GetAnimatorClipLength(mwAnimator, "ModalWindow_In");
            closeStateLength = DreamOSInternalTools.GetAnimatorClipLength(mwAnimator, "ModalWindow_Out");
        }

        void ApplyLocalizationKeys()
        {
            LocalizedObject mainLoc = GetComponent<LocalizedObject>();

            if (mainLoc == null || !mainLoc.CheckLocalizationStatus())
            {
                useLocalization = false;
                return;
            }

            if (windowTitle != null && !string.IsNullOrEmpty(titleKey)) { BindKey(windowTitle.gameObject, mainLoc, titleKey); }
            if (windowDescription != null && !string.IsNullOrEmpty(descriptionKey)) { BindKey(windowDescription.gameObject, mainLoc, descriptionKey); }
        }

        static void BindKey(GameObject target, LocalizedObject mainLoc, string key)
        {
            LocalizedObject loc = target.GetComponent<LocalizedObject>();
            if (loc == null) { return; }

            loc.tableIndex = mainLoc.tableIndex;
            loc.localizationKey = key;
            loc.UpdateItem();
        }

        public void UpdateUI()
        {
            if (!useCustomContent)
            {
                if (windowIcon != null) { windowIcon.sprite = icon; }
                if (windowTitle != null && !useLocalization) { windowTitle.text = titleText; }
                if (windowDescription != null && !useLocalization) { windowDescription.text = descriptionText; }
            }

            if (cancelButton != null) { cancelButton.gameObject.SetActive(showCancelButton); }
            if (confirmButton != null) { confirmButton.gameObject.SetActive(showConfirmButton); }
        }

        public void OpenWindow()
        {
            if (isOn) { return; }

            gameObject.SetActive(true);
            isOn = true;
            DesktopSfx.Play(DesktopSound.WindowOpen);

            StopCoroutine(nameof(DisableObject));
            StopCoroutine(nameof(DisableAnimator));
            StartCoroutine(nameof(DisableAnimator));

            if (backgroundBlur != null) { backgroundBlur.BlurInAnim(); }

            mwAnimator.enabled = true;
            mwAnimator.SetFloat(animSpeedKey, animationSpeed);
            mwAnimator.Play(animIn);

            onOpen.Invoke();
        }

        public void CloseWindow()
        {
            if (!isOn) { return; }

            isOn = false;
            DesktopSfx.Play(DesktopSound.WindowClose);

            StopCoroutine(nameof(DisableObject));
            StopCoroutine(nameof(DisableAnimator));
            StartCoroutine(nameof(DisableObject));

            if (backgroundBlur != null) { backgroundBlur.BlurOutAnim(); }

            mwAnimator.enabled = true;
            mwAnimator.SetFloat(animSpeedKey, animationSpeed);
            mwAnimator.Play(animOut);

            onClose.Invoke();
        }

        public void AnimateWindow()
        {
            if (!isOn) { OpenWindow(); }
            else { CloseWindow(); }
        }

        IEnumerator DisableObject()
        {
            yield return new WaitForSecondsRealtime(closeStateLength);

            if (closeBehaviour == CloseBehaviour.Disable) { gameObject.SetActive(false); }
            else if (closeBehaviour == CloseBehaviour.Destroy) { Destroy(gameObject); }

            mwAnimator.enabled = false;
        }

        IEnumerator DisableAnimator()
        {
            yield return new WaitForSecondsRealtime(openStateLength + 0.1f);
            mwAnimator.enabled = false;
        }
    }
}
