using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Michsky.DreamOS
{
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
            else
            {
                if (gameObject.activeInHierarchy)
                {
                    StopCoroutine("DisableAnimator");
                    StartCoroutine("DisableAnimator");

                    switchAnimator.enabled = true;

                    if (isOn) { switchAnimator.Play("On Instant"); }
                    else { switchAnimator.Play("Off Instant"); }
                }
            }

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

            if (invokeAtStart && isOn) { onEvents.Invoke(); onValueChanged.Invoke(isOn); }
            else if (invokeAtStart && !isOn) { offEvents.Invoke(); onValueChanged.Invoke(isOn); }

            isInitialized = true;
        }

        void OnEnable()
        {
            if (isInitialized) { UpdateUI(); }
        }

        void GetSavedData()
        {
            if (!DreamOSDataManager.ContainsJsonKey(dataCategory, saveKey))
            {
                if (isOn) { DreamOSDataManager.WriteBooleanData(dataCategory, saveKey, true); }
                else { DreamOSDataManager.WriteBooleanData(dataCategory, saveKey, false); }
            }
            else if (DreamOSDataManager.ReadBooleanData(dataCategory, saveKey)) { isOn = true; }
            else if (!DreamOSDataManager.ReadBooleanData(dataCategory, saveKey)) { isOn = false; }

            if (gameObject.activeInHierarchy)
            {
                StopCoroutine("DisableAnimator");
                StartCoroutine("DisableAnimator");

                switchAnimator.enabled = true;

                if (isOn) { switchAnimator.Play("On"); }
                else { switchAnimator.Play("Off"); }
            }
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
            if (gameObject.activeInHierarchy)
            {
                StopCoroutine("DisableAnimator");
                StartCoroutine("DisableAnimator");

                switchAnimator.enabled = true;
                switchAnimator.Play("On");
            }

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
            if (gameObject.activeInHierarchy)
            {
                StopCoroutine("DisableAnimator");
                StartCoroutine("DisableAnimator");

                switchAnimator.enabled = true;
                switchAnimator.Play("Off");
            }

            isOn = false;

            if (notifyEvents)
            {
                offEvents.Invoke();
                onValueChanged.Invoke(false);
            }
        }

        public void UpdateUI()
        {
            if (gameObject.activeInHierarchy)
            {
                StopCoroutine("DisableAnimator");
                StartCoroutine("DisableAnimator");

                switchAnimator.enabled = true;
            
                if (isOn) { switchAnimator.Play("On Instant"); }
                else { switchAnimator.Play("Off Instant"); }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable || eventData.button != PointerEventData.InputButton.Left) { return; }
            if (AudioManager.instance != null && useSounds) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.clickSound); }

            AnimateSwitch();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (AudioManager.instance != null && useSounds) { AudioManager.instance.audioSource.PlayOneShot(AudioManager.instance.UIManagerAsset.hoverSound); }
            if (!isInteractable) { return; }

            StartCoroutine("SetHighlight");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable)
                return;

            StartCoroutine("SetNormal");
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!isInteractable)
                return;

            StartCoroutine("SetHighlight");
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!isInteractable)
                return;

            StartCoroutine("SetNormal");
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!isInteractable)
                return;

            AnimateSwitch();
            StartCoroutine("SetNormal");
        }

        IEnumerator DisableAnimator()
        {
            yield return new WaitForSeconds(0.5f);
            switchAnimator.enabled = false;
        }

        IEnumerator SetNormal()
        {
            StopCoroutine("SetHighlight");

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
            StopCoroutine("SetNormal");

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