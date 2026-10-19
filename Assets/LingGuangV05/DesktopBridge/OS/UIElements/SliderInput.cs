using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// A number box that mirrors a <see cref="SliderManager"/>: shows the slider's value, and typing a number and
    /// pressing Enter moves the slider (clamped to its maximum). Replaces DreamOS's SliderInput (2026-10-19): same
    /// GUID, serialized fields and public API.
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public class SliderInput : MonoBehaviour
    {
        [Header("Resources")]
        [SerializeField] private SliderManager sliderManager;
        [SerializeField] private TMP_InputField inputField;

        [Header("Settings")]
        [SerializeField] private bool multiplyValue = false;
        [Range(1, 10)] public int maxChar = 5;
        [Range(0, 4)] public int decimals = 1;

        [Header("Events")]
        public UnityEvent onSubmit;

        void Awake()
        {
            if (sliderManager == null) { Debug.LogWarning("'Slider Manager' is missing!"); return; }
            if (inputField == null) { inputField = GetComponent<TMP_InputField>(); }

            if (sliderManager.mainSlider.wholeNumbers) { inputField.contentType = TMP_InputField.ContentType.IntegerNumber; }

            inputField.characterLimit = maxChar;
            inputField.selectionColor = new Color(inputField.textComponent.color.r, inputField.textComponent.color.g, inputField.textComponent.color.b, inputField.selectionColor.a);
            inputField.onDeselect.AddListener(delegate { SetText(sliderManager.mainSlider.value); });
            onSubmit.AddListener(SetValue);

            sliderManager.mainSlider.onValueChanged.AddListener(SetText);
            sliderManager.mainSlider.onValueChanged.Invoke(sliderManager.mainSlider.value);
        }

        void Update()
        {
            if (string.IsNullOrEmpty(inputField.text) || EventSystem.current.currentSelectedGameObject != inputField.gameObject) { return; }
            if (Keyboard.current.enterKey.wasPressedThisFrame) { onSubmit.Invoke(); }
        }

        void SetText(float value)
        {
            if (multiplyValue) { value = value * 100; }

            // Only 0-4 decimals are supported; anything else leaves the text alone
            if (decimals >= 0 && decimals <= 4) { inputField.text = value.ToString("F" + decimals); }
        }

        void SetValue()
        {
            var slider = sliderManager.mainSlider;

            if (slider.wholeNumbers) { slider.value = int.Parse(inputField.text); }
            else if (multiplyValue) { slider.value = float.Parse(inputField.text) / 100; }
            else { slider.value = float.Parse(inputField.text); }

            // Typed values above the maximum snap to it
            float typed = multiplyValue ? float.Parse(inputField.text) / 100 : float.Parse(inputField.text);
            if (typed > slider.maxValue) { slider.value = slider.maxValue; }
        }
    }
}
