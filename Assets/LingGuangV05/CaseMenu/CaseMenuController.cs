using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using LingGuangV05.Desktop;

namespace LingGuangV05.CaseMenu
{
    /// <summary>Preloads the real Main desktop; entering only moves the same camera and unlocks its screen.</summary>
    [DisallowMultipleComponent]
    public sealed class CaseMenuController : MonoBehaviour
    {
        public const string ScenePath = "Assets/LingGuangV05/Scenes/ComputerCaseMenu.unity";
        public const string VolumePref = "LingGuangV05.MasterVolume";
        public const string ReduceMotionPref = "LingGuangV05.ReduceMotion";
        public const string FullscreenPref = "LingGuangV05.Fullscreen";
        public Camera menuCamera;
        public CaseMenuButton startButton, settingsButton;
        public GameObject previewMonitor;
        public Transform displayRig;
        public Transform monitorCenter;
        public TMP_FontAsset font;
        public AudioClip hoverClip, pressClip, releaseClip, bootClip, fanClip;
        public float transitionSeconds = 4.8f;
        public bool IsTransitioning { get; private set; }
        public bool IsSettingsOpen => hud != null && hud.IsSettingsOpen;
        public int TransitionCount { get; private set; }
        public float TransitionProgress { get; private set; }
        public string LastError { get; private set; }
        public bool HasEnteredGame { get; private set; }
        public bool IsMainReady => mainDisplay != null && mainDisplay.IsReady;
        CaseMenuHud hud;
        CaseMenuMainDisplay mainDisplay;
        AudioSource effects, fan;
        CaseMenuFanAudio fanMotor;
        CaseMenuButton hovered, captured;
        float initialFov, lastHoverSound = -1, originalListenerVolume;
        bool reducedMotion, keyboardFocus;
        enum PressSource { None, Mouse, Touch, Enter, Space }
        PressSource pressSource;

        void Awake()
        {
            if (menuCamera == null || startButton == null || settingsButton == null || monitorCenter == null || displayRig == null)
            {
                Debug.LogError("Case menu scene is missing required references.", this);
                enabled = false; return;
            }
            initialFov = menuCamera.fieldOfView;
            originalListenerVolume = AudioListener.volume;
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePref, .8f));
            reducedMotion = PlayerPrefs.GetInt(ReduceMotionPref, 0) != 0;
            startButton.ReducedMotion = settingsButton.ReducedMotion = reducedMotion;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false; effects.spatialBlend = 0; effects.priority = 48;
            fan = gameObject.AddComponent<AudioSource>();
            fan.playOnAwake = false; fan.loop = true; fan.spatialBlend = 0; fan.volume = 0;
            fanMotor = gameObject.AddComponent<CaseMenuFanAudio>();
            fanMotor.Configure(fan, fanClip);
            hud = gameObject.AddComponent<CaseMenuHud>();
            hud.Build(font, SetVolume, SetReducedMotion, SetFullscreen, CloseSettings,
                () => Play(releaseClip, .24f));
            mainDisplay = gameObject.AddComponent<CaseMenuMainDisplay>();
            Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
            if (!Application.isEditor && PlayerPrefs.HasKey(FullscreenPref))
                Screen.fullScreen = PlayerPrefs.GetInt(FullscreenPref) != 0;
        }

        IEnumerator Start()
        {
            hud.SetLoading(0, "准备桌面");
            yield return mainDisplay.Preload(menuCamera, displayRig, previewMonitor);
            if (!mainDisplay.IsReady)
            {
                LastError = mainDisplay.Error;
                hud.ShowError("桌面启动失败：" + LastError);
            }
            else hud.SetLoading(-1, null);
        }

        void Update()
        {
            if (fanMotor != null && mainDisplay != null && mainDisplay.IsScreenPowered && !HasEnteredGame)
                fanMotor.SetBootElapsed(mainDisplay.BootProgress * CaseMenuMainDisplay.BootDuration);
            if (hud == null || IsTransitioning || HasEnteredGame) return;
            var keyboard = Keyboard.current;
            if (IsSettingsOpen)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) CloseSettings();
                return;
            }
            if (captured == null && keyboard != null && (keyboard.tabKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame))
            {
                keyboardFocus = true;
                SetHovered(hovered == startButton ? settingsButton : startButton);
            }
            var mouse = Mouse.current;
            var touch = Touchscreen.current;
            bool touching = touch != null && touch.primaryTouch.press.isPressed;
            bool touchRelease = touch != null && touch.primaryTouch.press.wasReleasedThisFrame;
            Vector2 point = touching || touchRelease ? touch.primaryTouch.position.ReadValue() : mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            bool down = touching ? touch.primaryTouch.press.wasPressedThisFrame : mouse != null && mouse.leftButton.wasPressedThisFrame;
            bool up = touchRelease || (mouse != null && mouse.leftButton.wasReleasedThisFrame);
            if (touching || touchRelease || down || up || (mouse != null && mouse.delta.ReadValue().sqrMagnitude > .01f)) keyboardFocus = false;
            if (!keyboardFocus && (mouse != null || touching || touchRelease)) SetHovered(HitButton(point));

            bool confirmDown = keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
            if (confirmDown && hovered == null) { keyboardFocus = true; SetHovered(startButton); }
            if (captured == null && (down || confirmDown) && hovered != null)
            {
                pressSource = down ? (touching ? PressSource.Touch : PressSource.Mouse)
                    : keyboard.enterKey.wasPressedThisFrame ? PressSource.Enter : PressSource.Space;
                captured = hovered; captured.BeginPress(); Play(pressClip, .38f);
            }
            bool matchingRelease = pressSource == PressSource.Mouse && mouse != null && mouse.leftButton.wasReleasedThisFrame
                || pressSource == PressSource.Touch && touchRelease
                || pressSource == PressSource.Enter && keyboard != null && keyboard.enterKey.wasReleasedThisFrame
                || pressSource == PressSource.Space && keyboard != null && keyboard.spaceKey.wasReleasedThisFrame;
            if (matchingRelease && captured != null)
            {
                var released = captured; captured = null;
                pressSource = PressSource.None;
                bool accepted = released.EndPress(hovered == released);
                Play(releaseClip, accepted ? .29f : .12f);
                if (accepted) Activate(released);
            }
        }

        public CaseMenuButton HitButton(Vector2 screenPoint)
        {
            if (menuCamera == null || IsTransitioning || IsSettingsOpen || HasEnteredGame) return null;
            return Physics.Raycast(menuCamera.ScreenPointToRay(screenPoint), out var hit, 15f, 1,
                QueryTriggerInteraction.Collide) ? hit.collider.GetComponentInParent<CaseMenuButton>() : null;
        }

        void SetHovered(CaseMenuButton next)
        {
            if (next == hovered) return;
            if (hovered != null) hovered.SetHovered(false);
            hovered = next;
            if (hovered != null)
            {
                hovered.SetHovered(true);
                if (Time.unscaledTime - lastHoverSound > .09f) { Play(hoverClip, .16f); lastHoverSound = Time.unscaledTime; }
            }
            hud.SetHint(next == startButton ? "按下电源键 · 开始游戏" : next == settingsButton ? "按下设置键 · 调整音量与显示" : "点击机箱上的电源键，进入游戏");
        }

        void Activate(CaseMenuButton button) { if (button.isStart) BeginGame(); else OpenSettings(); }

        public void OpenSettings()
        {
            if (IsTransitioning || HasEnteredGame || hud == null) return;
            CancelCapture(); SetHovered(null);
            hud.ShowSettings(AudioListener.volume, reducedMotion, Screen.fullScreen);
        }
        public void CloseSettings()
        {
            if (hud == null) return;
            hud.HideSettings(); PlayerPrefs.Save();
            keyboardFocus = false;
        }
        public void SetVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumePref, AudioListener.volume);
        }
        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            startButton.ReducedMotion = settingsButton.ReducedMotion = value;
            PlayerPrefs.SetInt(ReduceMotionPref, value ? 1 : 0);
        }
        public void SetFullscreen(bool value)
        {
            PlayerPrefs.SetInt(FullscreenPref, value ? 1 : 0);
            Screen.fullScreen = value;
        }

        public void BeginGame()
        {
            if (IsTransitioning || HasEnteredGame || IsSettingsOpen || hud == null) return;
            if (!string.IsNullOrEmpty(mainDisplay.Error))
            {
                LastError = mainDisplay.Error;
                hud.ShowError(LastError); Debug.LogError(LastError, this); return;
            }
            IsTransitioning = true; TransitionCount++;
            CancelCapture(); SetHovered(null); startButton.Pulse();
            PlayerPrefs.Save();
            StartCoroutine(EnterGame());
        }

        IEnumerator EnterGame()
        {
            // All loading occurs while stationary. No scene activation or blackout occurs in the flight.
            while (!mainDisplay.IsReady && string.IsNullOrEmpty(mainDisplay.Error))
            {
                hud.SetLoading(0, "桌面正在准备"); yield return null;
            }
            if (!mainDisplay.IsReady) { IsTransitioning = false; hud.ShowError(mainDisplay.Error); yield break; }
            hud.SetLoading(-1, null);
            Play(bootClip, .22f);
            mainDisplay.PowerOnScreen();
            if (fanMotor != null) fanMotor.StartBoot();
            // Give the physical switch and LCD backlight a short, visible lead before the dolly.
            yield return new WaitForSecondsRealtime(.20f);
            float duration = reducedMotion ? .65f : Mathf.Max(2f, transitionSeconds);
            Vector3 start = menuCamera.transform.position;
            Quaternion startRotation = menuCamera.transform.rotation;
            // Match Main's native monitor framing (scaled by .25) at every aspect ratio.
            float halfHeight = Mathf.Max(1.64f, 2.79f / Mathf.Max(.1f, menuCamera.aspect)) * .25f;
            float finalFov = 32f;
            float distance = halfHeight / Mathf.Tan(finalFov * Mathf.Deg2Rad * .5f);
            Vector3 end = monitorCenter.position + Vector3.back * distance;
            Vector3 controlA = start + startRotation * Vector3.forward * .85f;
            Vector3 controlB = end + new Vector3(0, .025f, -.65f);
            for (float elapsed = 0; elapsed < duration; elapsed += CaseMenuMotion.FrameStep(Time.unscaledDeltaTime))
            {
                float raw = Mathf.Clamp01(elapsed / duration);
                float t = CaseMenuMotion.SmootherStep(raw);
                TransitionProgress = raw;
                menuCamera.transform.position = reducedMotion ? Vector3.Lerp(start, end, t)
                    : CaseMenuMotion.Bezier(start, controlA, controlB, end, t);
                menuCamera.transform.rotation = CaseMenuMotion.Rotation(startRotation, Quaternion.identity, raw);
                menuCamera.fieldOfView = Mathf.Lerp(initialFov, finalFov, t);
                hud.SetChromeAlpha(1 - Mathf.Clamp01(raw * 3f));
                yield return null;
            }
            menuCamera.transform.SetPositionAndRotation(end, Quaternion.identity);
            menuCamera.fieldOfView = finalFov; TransitionProgress = 1;
            // Reduced motion may arrive early. Keep desktop input locked until the visible boot ends.
            while (!mainDisplay.IsBootComplete) yield return null;
            mainDisplay.EnterDesktop();
            HasEnteredGame = true; IsTransitioning = false;
            hud.SetChromeAlpha(0);
            hud.SetLoading(-1, null);
            if (fanMotor != null) { fanMotor.SetStable(); fanMotor.BeginGameplayCountdown(); }
        }

        void Play(AudioClip clip, float gain)
        {
            if (effects != null && clip != null) effects.PlayOneShot(clip, gain);
        }
        void CancelCapture() { if (captured != null) captured.CancelPress(); captured = null; pressSource = PressSource.None; }
        void OnApplicationFocus(bool focus) { if (!focus) { CancelCapture(); if (hud != null && !HasEnteredGame) SetHovered(null); } }
        void OnDisable() { CancelCapture(); }
        void OnDestroy()
        {
            if (Application.isEditor) AudioListener.volume = originalListenerVolume;
        }
    }
}
