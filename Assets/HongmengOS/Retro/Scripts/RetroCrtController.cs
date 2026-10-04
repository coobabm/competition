using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HongmengOS.Retro
{
    /// <summary>
    /// Scene-local CRT picture controls. Runtime material and generated sounds
    /// never modify the shared material, any imported asset, or system volume.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RetroCrtController : MonoBehaviour
    {
        [Header("Scene references")]
        public RawImage display;
        public Camera sourceCamera;
        public RetroCrtInputSurface inputSurface;
        public Michsky.DreamOS.AudioManager desktopAudio;
        public AudioSource hardwareAudio;
        public Graphic powerLamp;
        public RectTransform focusDial;
        public RectTransform brightnessDial;

        [Header("Picture controls")]
        public bool initialPower = true;
        [Range(0f, 1f)] public float brightness = 0.5f;
        [Range(0f, 1f)] public float focus = 0.5f;
        [Tooltip("One brief display-local white flash per accepted power change; set to zero to suppress it.")]
        [Range(0f, 1f)] public float flashStrength = 0.75f;
        [Range(0f, 0.5f)] public float mechanicalGain = 0.22f;
        [Range(0f, 0.5f)] public float powerSweepGain = 0.18f;
        public Color lampOnColor = new Color(0.48f, 0.95f, 0.27f, 1f);
        public Color lampOffColor = new Color(0.10f, 0.17f, 0.07f, 1f);
        public Color lampTransitionColor = new Color(1f, 0.60f, 0.12f, 1f);

        public bool IsPowered { get { return powered; } }
        public bool IsTransitioning { get { return transition != null; } }
        public bool CanInteract { get { return isActiveAndEnabled && powered && transition == null; } }
        public float Brightness { get { return brightness; } }
        public float Focus { get { return focus; } }

        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int FocusId = Shader.PropertyToID("_Focus");
        private static readonly int PowerId = Shader.PropertyToID("_Power");
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static readonly int CollapseId = Shader.PropertyToID("_Collapse");
        private Material runtimeMaterial;
        private Material originalMaterial;
        private AudioClip mechanicalClip;
        private AudioClip powerSweepClip;
        private AudioSource ownedAudio;
        private Coroutine transition;
        private bool powered;
        private bool initialized;
        private bool originalCameraEnabled;
        private bool originalInputEnabled;
        private Color originalLampColor;
        private float lastKnobTick = float.NegativeInfinity;

        private void Awake()
        {
            InitializeRuntime();
        }

        private void OnEnable()
        {
            InitializeRuntime();
            powered = initialPower;
            ApplyStableState();
        }

        private void OnValidate()
        {
            brightness = Mathf.Clamp01(brightness);
            focus = Mathf.Clamp01(focus);
            flashStrength = Mathf.Clamp01(flashStrength);
            mechanicalGain = Mathf.Clamp(mechanicalGain, 0f, 0.5f);
            powerSweepGain = Mathf.Clamp(powerSweepGain, 0f, 0.5f);
        }

        private void InitializeRuntime()
        {
            if (initialized) return;
            initialized = true;
            if (display != null)
            {
                originalMaterial = display.material;
                if (originalMaterial != null)
                {
                    runtimeMaterial = new Material(originalMaterial);
                    runtimeMaterial.name = originalMaterial.name + " (Runtime CRT Controls)";
                    runtimeMaterial.hideFlags = HideFlags.DontSave;
                    display.material = runtimeMaterial;
                }
            }
            if (sourceCamera != null) originalCameraEnabled = sourceCamera.enabled;
            if (inputSurface != null) originalInputEnabled = inputSurface.inputEnabled;
            if (powerLamp != null) originalLampColor = powerLamp.color;
            if (hardwareAudio == null)
            {
                var audioObject = new GameObject("CRT Mechanical Audio");
                audioObject.transform.SetParent(transform, false);
                ownedAudio = audioObject.AddComponent<AudioSource>();
                ownedAudio.playOnAwake = false;
                ownedAudio.loop = false;
                ownedAudio.spatialBlend = 0f;
                ownedAudio.volume = 1f;
                hardwareAudio = ownedAudio;
            }
            mechanicalClip = CreateMechanicalClip();
            powerSweepClip = CreatePowerSweepClip();
            ApplyPictureSettings();
        }

        public void SetBrightness(float normalized)
        {
            brightness = Mathf.Clamp01(normalized);
            SetFloat(BrightnessId, brightness * 2f);
            RotatePointer(brightnessDial, brightness);
        }

        public void SetFocus(float normalized)
        {
            focus = Mathf.Clamp01(normalized);
            SetFloat(FocusId, focus);
            RotatePointer(focusDial, focus);
        }

        public void PlayKnobTick()
        {
            if (!isActiveAndEnabled || Time.unscaledTime - lastKnobTick < 0.035f) return;
            lastKnobTick = Time.unscaledTime;
            PlayMechanical();
        }

        /// <summary>Call only after the input bridge accepts a display-local click.</summary>
        public void PlayDesktopClick()
        {
            if (CanInteract && desktopAudio != null) desktopAudio.PlayMouseStroke();
        }

        public void TogglePower()
        {
            if (!isActiveAndEnabled || transition != null) return;
            InitializeRuntime();
            transition = StartCoroutine(ChangePower(!powered));
        }

        /// <summary>Ignores repeated commands while the physical power transition runs.</summary>
        public void SetPower(bool value)
        {
            if (!isActiveAndEnabled || transition != null || value == powered) return;
            InitializeRuntime();
            transition = StartCoroutine(ChangePower(value));
        }

        private IEnumerator ChangePower(bool turnOn)
        {
            SetInputEnabled(false);
            if (powerLamp != null) powerLamp.color = lampTransitionColor;
            PlayMechanical();
            if (hardwareAudio != null && powerSweepClip != null)
                hardwareAudio.PlayOneShot(powerSweepClip, powerSweepGain);

            if (sourceCamera != null) sourceCamera.enabled = true;
            SetFloat(PowerId, 1f);
            SetFloat(CollapseId, turnOn ? 0f : 1f);
            SetFloat(FlashId, flashStrength);
            // Exactly one short flash; there is no repeating coroutine or oscillation.
            yield return null; // Present at least one flash frame even after an editor hitch.
            float elapsed = Time.unscaledDeltaTime;
            const float flashDuration = 0.045f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetFloat(FlashId, flashStrength * (1f - Mathf.Clamp01(elapsed / flashDuration)));
                yield return null;
            }
            SetFloat(FlashId, 0f);

            elapsed = 0f;
            float duration = turnOn ? 0.25f : 0.18f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetFloat(CollapseId, turnOn ? t : 1f - t);
                yield return null;
            }
            // Let the last thin line occupy a single presented frame before black.
            SetFloat(CollapseId, turnOn ? 1f : 0f);
            if (!turnOn) yield return null;
            powered = turnOn;
            transition = null;
            ApplyStableState();
        }

        private void ApplyStableState()
        {
            SetFloat(PowerId, powered ? 1f : 0f);
            SetFloat(FlashId, 0f);
            SetFloat(CollapseId, powered ? 1f : 0f);
            if (sourceCamera != null) sourceCamera.enabled = powered;
            if (powerLamp != null) powerLamp.color = powered ? lampOnColor : lampOffColor;
            SetInputEnabled(powered);
            ApplyPictureSettings();
        }

        private void ApplyPictureSettings()
        {
            SetBrightness(brightness);
            SetFocus(focus);
        }

        private void SetInputEnabled(bool value)
        {
            if (inputSurface != null) inputSurface.SetInputEnabled(value);
        }

        private void SetFloat(int property, float value)
        {
            if (runtimeMaterial != null && runtimeMaterial.HasProperty(property))
                runtimeMaterial.SetFloat(property, value);
        }

        private static void RotatePointer(RectTransform pointer, float value)
        {
            if (pointer != null) pointer.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(130f, -130f, value));
        }

        private void PlayMechanical()
        {
            if (hardwareAudio != null && mechanicalClip != null)
                hardwareAudio.PlayOneShot(mechanicalClip, mechanicalGain);
        }

        private static AudioClip CreateMechanicalClip()
        {
            const int sampleRate = 44100;
            var samples = new float[(int)(sampleRate * 0.07f)];
            uint state = 0x74C398B1u;
            for (int i = 0; i < samples.Length; i++)
            {
                double t = (double)i / sampleRate;
                float noise = NextNoise(ref state);
                double attack = Math.Min(1.0, t / 0.0007);
                double click = (noise * 0.43 + Math.Sin(t * Math.PI * 2.0 * 1650.0) * 0.24) * Math.Exp(-t * 115.0);
                double returnClick = t >= 0.021 ? noise * 0.19 * Math.Exp(-(t - 0.021) * 180.0) : 0.0;
                double body = Math.Sin(t * Math.PI * 2.0 * 235.0) * Math.Exp(-t * 72.0) * 0.18;
                samples[i] = (float)((click + returnClick + body) * attack);
            }
            var clip = AudioClip.Create("CRT Original Mechanical Detent", samples.Length, 1, sampleRate, false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreatePowerSweepClip()
        {
            const int sampleRate = 44100;
            const double duration = 0.25;
            var samples = new float[(int)(sampleRate * duration)];
            uint state = 0x1387DAB5u;
            double phase = 0.0;
            for (int i = 0; i < samples.Length; i++)
            {
                double t = (double)i / sampleRate;
                double frequency = 310.0 - 215.0 * (t / duration);
                phase += Math.PI * 2.0 * frequency / sampleRate;
                double attack = Math.Min(1.0, t / 0.009);
                double release = Math.Min(1.0, (duration - t) / 0.025);
                double envelope = attack * release * Math.Exp(-t * 14.0);
                samples[i] = (float)((Math.Sin(phase) * 0.28 + Math.Sin(phase * 2.013) * 0.10 + NextNoise(ref state) * 0.07) * envelope);
            }
            var clip = AudioClip.Create("CRT Original Power Discharge", samples.Length, 1, sampleRate, false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(samples, 0);
            return clip;
        }

        private static float NextNoise(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0x00ffffffu) / 8388607.5f - 1f;
        }

        private void OnDisable()
        {
            CleanupRuntime();
        }

        private void OnDestroy()
        {
            CleanupRuntime();
        }

        private void CleanupRuntime()
        {
            if (!initialized) return;
            if (transition != null) StopCoroutine(transition);
            transition = null;
            if (display != null && display.material == runtimeMaterial) display.material = originalMaterial;
            if (sourceCamera != null) sourceCamera.enabled = originalCameraEnabled;
            if (inputSurface != null) inputSurface.SetInputEnabled(originalInputEnabled);
            if (powerLamp != null) powerLamp.color = originalLampColor;
            if (hardwareAudio != null) hardwareAudio.Stop();
            DestroyRuntimeObject(runtimeMaterial);
            DestroyRuntimeObject(mechanicalClip);
            DestroyRuntimeObject(powerSweepClip);
            if (ownedAudio != null)
            {
                DestroyRuntimeObject(ownedAudio.gameObject);
                if (hardwareAudio == ownedAudio) hardwareAudio = null;
            }
            runtimeMaterial = null;
            originalMaterial = null;
            mechanicalClip = null;
            powerSweepClip = null;
            ownedAudio = null;
            initialized = false;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
