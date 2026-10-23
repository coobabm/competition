using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Wraps a TMP_InputField with the floating-label animation and an Enter-to-submit event (2026-10-19). Replaces
    /// DreamOS's InputFieldManager: same GUID, serialized fields and public API.
    ///
    /// The animator's "In" state plays while the field has text or focus and "Out" when it is empty and unfocused;
    /// the animator is parked after 0.25 s. With <see cref="processSubmit"/> on, Enter in the focused, non-empty field
    /// fires <see cref="onSubmit"/> (optionally clearing the text afterwards).
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public class InputFieldManager : MonoBehaviour
    {
        [Header("Resources")]
        public TMP_InputField inputText;
        public Animator inputFieldAnimator;

        [Header("Settings")]
        public bool processSubmit = false;
        public bool clearOnSubmit = false;

        [Header("Events")]
        public UnityEvent onSubmit;

        // Helpers
        const float AnimatorParkDelay = 0.25f;
        Coroutine disableRoutine;

        void Awake()
        {
            if (inputText == null) { inputText = gameObject.GetComponent<TMP_InputField>(); }
            if (clearOnSubmit) { onSubmit.AddListener(delegate { inputText.text = ""; }); }

            inputText.onValueChanged.AddListener(delegate { UpdateState(); });
            inputText.onSelect.AddListener(delegate { AnimateIn(); });
            inputText.onEndEdit.AddListener(delegate { AnimateOut(); });
        }

        void OnEnable()
        {
            if (inputText == null) { return; }
            if (inputFieldAnimator != null && gameObject.activeInHierarchy) { RestartDisableTimer(); }

            inputText.ForceLabelUpdate();
            UpdateState();
        }

        void Update()
        {
            if (!processSubmit || string.IsNullOrEmpty(inputText.text) || EventSystem.current.currentSelectedGameObject != inputText.gameObject) { return; }
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) { onSubmit.Invoke(); }
        }

        public void AnimateIn()
        {
            if (inputFieldAnimator == null || !inputFieldAnimator.gameObject.activeInHierarchy) { return; }

            inputFieldAnimator.enabled = true;
            inputFieldAnimator.Play("In");
            RestartDisableTimer();
        }

        public void AnimateOut()
        {
            if (inputFieldAnimator == null || !inputFieldAnimator.gameObject.activeInHierarchy) { return; }

            inputFieldAnimator.enabled = true;
            if (inputText.text.Length == 0) { inputFieldAnimator.Play("Out"); }
            RestartDisableTimer();
        }

        public void UpdateState()
        {
            if (inputText.text.Length == 0) { AnimateOut(); }
            else { AnimateIn(); }
        }

        public void InvokeSubmit()
        {
            onSubmit.Invoke();
        }

        void RestartDisableTimer()
        {
            if (disableRoutine != null) { StopCoroutine(disableRoutine); }
            disableRoutine = StartCoroutine(DisableAnimator());
        }

        IEnumerator DisableAnimator()
        {
            yield return new WaitForSeconds(AnimatorParkDelay);
            inputFieldAnimator.enabled = false;
        }
    }
}
