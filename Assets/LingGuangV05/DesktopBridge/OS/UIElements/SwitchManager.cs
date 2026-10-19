using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The on/off toggle used across settings pages and app options. An animator slides the knob ("On"/"Off", with
    /// "... Instant" states for first draw), a highlight group fades on hover, and the value can be kept in the
    /// DreamOS data files. Replaces DreamOS's SwitchManager (2026-10-19): same GUID, serialized fields and public API.
    ///
    /// Events fire both as the legacy onEvents/offEvents pair and as onValueChanged(bool); SetOn/SetOff can skip them
    /// when the owner is only syncing the visual.
    /// </summary>
    public class SwitchManager : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        // Resources
        [SerializeField] private Animator switchAnimator;
        [SerializeField] private CanvasGroup highlightCG;

        // Saving
        public bool saveValue = false;
        public string saveKey = "My Switch";
        public DreamOSDataManager.DataCategory dataCategory = DreamOSDataManager.DataCategory.Apps;

        // Settings
        public bool isOn = true;
        public bool isInteractable = true;
        public bool invokeAtStart = true;
        [SerializeField] private bool useEventTrigger = true;
        public bool useSounds = true;
        public bool useUINavigation = false;
        [Range(0, 15)] public float fadingMultiplier = 8;

        // Events
        [SerializeField] public SwitchEvent onValueChanged = new SwitchEvent();
        public UnityEvent onEvents;
        public UnityEvent offEvents;

        // Helpers
        bool isInitialized = false;

        [System.Serializable]
        public class SwitchEvent : UnityEvent<bool> { }

        void Awake()
        {
            if (saveValue) { GetSavedData(); }
            else { PlayState(isOn ? "On Instant" : "Off Instant"); }

            // Add necessary components
            if (useEventTrigger && gameObject.GetComponent<EventTrigger>() == null) { gameObject.AddComponent<EventTrigger>(); }
            if (gameObject.GetComponent<Image>() == null)
            {
                Image raycastImg = gameObject.AddComponent<Image>();
                raycastImg.color = new Color(0, 0, 0, 0);
                raycastImg.raycastTarget = true;
            }

            if (useUINavigation) { AddUINavigation(); }
            if (highlightCG == null) { highlightCG = new GameObject().AddComponent<CanvasGroup>(); highlightCG.transform.SetParent(transform); highlightCG.gameObject.name = "Highlighted"; }

            if (invokeAtStart)
            {
                if (isOn) { onEvents.Invoke(); }
                else { offEvents.Invoke(); }
                onValueChanged.Invoke(isOn);
            }

            isInitialized = true;
        }

        void OnEnable()
        {
            if (isInitialized) { UpdateUI(); }
        }

        void GetSavedData()
        {
            if (!DreamOSDataManager.ContainsJsonKey(dataCategory, saveKey)) { DreamOSDataManager.WriteBooleanData(dataCategory, saveKey, isOn); }
            else { isOn = DreamOSDataManager.ReadBooleanData(dataCategory, saveKey); }

            PlayState(isOn ? "On" : "Off");
        }

        public void AddUINavigation()
        {
            Button navButton = gameObject.AddComponent<Button>();
            navButton.transition = Selectable.Transition.None;
            Navigation customNav = new Navigation();
            customNav.mode = Navigation.Mode.Automatic;
            navButton.navigation = customNav;
        }

        public void AnimateSwitch()
        {
            if (isOn) { SetOff(); }
            else { SetOn(); }
        }

        public void SetOn(bool notifyEvents = true)
        {
            if (saveValue) { DreamOSDataManager.WriteBooleanData(dataCategory, saveKey, true); }
            PlayState("On");

            isOn = true;

            if (notifyEvents)
            {
                onEvents.Invoke();
                onValueChanged.Invoke(true);
            }
        }

        public void SetOff(bool notifyEvents = true)
        {
            if (saveValue) { DreamOSDataManager.WriteBooleanData(dataCategory, saveKey, false); }
            PlayState("Off");

            isOn = false;

            if (notifyEvents)
            {
                offEvents.Invoke();
                onValueChanged.Invoke(false);
            }
        }

        public void UpdateUI()
        {
            PlayState(isOn ? "On Instant" : "Off Instant");
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable || eventData.button != PointerEventData.InputButton.Left) { return; }
            if (AudioManager.instance != null && useSounds) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.clickSound); }

            AnimateSwitch();
            DesktopSfx.Play(isOn ? DesktopSound.ToggleOn : DesktopSound.ToggleOff);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (AudioManager.instance != null && useSounds) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.hoverSound); }
            if (!isInteractable) { return; }

            StartCoroutine(nameof(SetHighlight));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable)
                return;

            StartCoroutine(nameof(SetNormal));
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!isInteractable)
                return;

            StartCoroutine(nameof(SetHighlight));
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!isInteractable)
                return;

            StartCoroutine(nameof(SetNormal));
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!isInteractable)
                return;

            AnimateSwitch();
            StartCoroutine(nameof(SetNormal));
        }

        // Plays an animator state and switches the animator off again half a second later. Skipped while the
        // object is inactive: the state is re-applied by OnEnable through UpdateUI.
        void PlayState(string stateName)
        {
            if (!gameObject.activeInHierarchy) { return; }

            StopCoroutine(nameof(DisableAnimator));
            StartCoroutine(nameof(DisableAnimator));

            switchAnimator.enabled = true;
            switchAnimator.Play(stateName);
        }

        IEnumerator DisableAnimator()
        {
            yield return new WaitForSeconds(0.5f);
            switchAnimator.enabled = false;
        }

        IEnumerator SetNormal()
        {
            StopCoroutine(nameof(SetHighlight));

            if (fadingMultiplier == 0) { highlightCG.alpha = 0; }
            else
            {
                while (highlightCG.alpha > 0.01f)
                {
                    highlightCG.alpha -= Time.unscaledDeltaTime * fadingMultiplier;
                    yield return null;
                }
            }

            highlightCG.alpha = 0;
        }

        IEnumerator SetHighlight()
        {
            StopCoroutine(nameof(SetNormal));

            if (fadingMultiplier == 0) { highlightCG.alpha = 1; }
            else
            {
                while (highlightCG.alpha < 0.99f)
                {
                    highlightCG.alpha += Time.unscaledDeltaTime * fadingMultiplier;
                    yield return null;
                }
            }

            highlightCG.alpha = 1;
        }
    }
}
