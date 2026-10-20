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
        public const string VolumePref = GameSettings.VolumePref;
        public const string ReduceMotionPref = GameSettings.ReduceMotionPref;
        public const string FullscreenPref = GameSettings.FullscreenPref;
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
        public bool IsReturningToTitle { get; private set; }
        public float ReturnTransitionProgress { get; private set; }
        public bool IsMainReady => mainDisplay != null && mainDisplay.IsReady;
        CaseMenuHud hud;
        CaseMenuMainDisplay mainDisplay;
        AudioSource effects, fan;
        CaseMenuFanAudio fanMotor;
        CaseMenuButton hovered, captured;
        float initialFov, lastHoverSound = -1, originalListenerVolume;
        bool reducedMotion, keyboardFocus;
        Vector3 flightStart, flightEnd, flightControlA, flightControlB;
        Quaternion flightStartRotation;
        float flightDuration;
        bool flightReduced;
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
            GameSettings.ApplySaved();
            reducedMotion = GameSettings.ReducedMotion;
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
            hud.ConfigureLifeEvents(CurrentLifeEventMode, SetLifeEventMode);
            hud.ConfigureTitleActions(() => IsMainReady ? mainDisplay.Runtime : null, BeginGame, QuitGame, RequestPowerOn);
            Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
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
            if (hud == null || IsTransitioning) return;
            var keyboard = Keyboard.current;
            if (HasEnteredGame)
            {
                if (IsSettingsOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                { if (hud.IsPlayerHelpOpen) hud.ClosePlayerHelp(); else CloseSettings(); }
                return;
            }
            if (hud.IsArchiveOpen) { if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) hud.CloseArchiveByPlayer(); return; }
            if (hud.HasTitleSelection && keyboard != null && (keyboard.enterKey.isPressed || keyboard.spaceKey.isPressed || keyboard.enterKey.wasReleasedThisFrame || keyboard.spaceKey.wasReleasedThisFrame)) return;
            if (IsSettingsOpen)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                { if (hud.IsPlayerHelpOpen) hud.ClosePlayerHelp(); else CloseSettings(); }
                return;
            }
            if (keyboard != null && keyboard.lKey.wasPressedThisFrame) { hud.OpenArchives(); return; }
            if (keyboard != null && keyboard.qKey.wasPressedThisFrame) { QuitGame(); return; }
            if (hud.HasTitleSelection && keyboard != null && (keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed || keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed)) return;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame) EventSystem.current?.SetSelectedGameObject(null);
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
            if (hud != null && hud.BlocksTitlePointer(screenPoint)) return null;
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

        public void QuitGame()
        {
            if (IsTransitioning || HasEnteredGame) return;
            var rt = mainDisplay != null ? mainDisplay.Runtime : null;
            try {
                if (rt != null && rt.useDiskSave && !rt.TestMode && rt.Sim != null && !rt.Sim.InPrologue && !rt.SaveNow())
                { hud.ShowError(LingGuangV05.Runtime.GameText.T("未退出：", "Cannot quit: ") + rt.SaveStatus); return; }
                PlayerPrefs.Save();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            } catch (Exception e) { hud.ShowError(LingGuangV05.Runtime.GameText.T("未退出：", "Cannot quit: ") + e.Message); }
        }

        void Activate(CaseMenuButton button) { if (button.isStart) RequestPowerOn(); else OpenSettings(); }

        public void OpenSettings()
        {
            if (IsTransitioning || HasEnteredGame || hud == null) return;
            CancelCapture(); SetHovered(null);
            hud.ShowSettings(AudioListener.volume, reducedMotion, Screen.fullScreen);
        }
        public void OpenDesktopSettings()
        {
            if (!HasEnteredGame || IsTransitioning || hud == null) return;
            hud.ShowSettings(AudioListener.volume, GameSettings.ReducedMotion, Screen.fullScreen);
        }
        public void CloseSettings()
        {
            if (hud == null) return;
            hud.HideSettings(); PlayerPrefs.Save();
            keyboardFocus = false;
        }
        int? CurrentLifeEventMode() => GameSettings.CurrentLifeEventMode(IsMainReady ? mainDisplay.Runtime : null);
        void SetLifeEventMode(int mode) => GameSettings.SetLifeEventMode(IsMainReady ? mainDisplay.Runtime : null, mode, hud);

        public void SetVolume(float value)
        {
            GameSettings.SetVolume(value);
        }
        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            startButton.ReducedMotion = settingsButton.ReducedMotion = value;
            GameSettings.SetReducedMotion(value);
        }
        public void SetFullscreen(bool value)
        {
            GameSettings.SetFullscreen(value);
        }

        bool waitingForArchiveRuntime;
        public void RequestPowerOn()
        {
            if (IsTransitioning || HasEnteredGame || IsSettingsOpen || hud == null || hud.IsArchiveOpen || waitingForArchiveRuntime) return;
            CancelCapture(); SetHovered(null);
            if (IsMainReady) { hud.RequestPowerOn(); return; }
            StartCoroutine(WaitForArchiveRuntime());
        }
        IEnumerator WaitForArchiveRuntime()
        {
            waitingForArchiveRuntime = true;
            while (!mainDisplay.IsReady && string.IsNullOrEmpty(mainDisplay.Error))
            { hud.SetLoading(0, LingGuangV05.Runtime.GameText.T("桌面正在准备", "Preparing the desktop")); yield return null; }
            waitingForArchiveRuntime = false;
            hud.SetLoading(-1, null);
            if (!mainDisplay.IsReady) { hud.ShowError(mainDisplay.Error); yield break; }
            if (!HasEnteredGame && !IsTransitioning && !IsSettingsOpen) hud.RequestPowerOn();
        }

        public void BeginGame()
        {
            if (IsTransitioning || HasEnteredGame || IsSettingsOpen || hud == null || hud.IsArchiveOpen) return;
            if (!string.IsNullOrEmpty(mainDisplay.Error))
            {
                LastError = mainDisplay.Error;
                hud.ShowError(LastError); Debug.LogError(LastError, this); return;
            }
            if (IsMainReady && hud.OpenCompletedEnding()) return;
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
            // A start click during preload must make the same completed-save decision after loading.
            if (hud.OpenCompletedEnding()) { IsTransitioning = false; yield break; }
            Play(bootClip, .22f);
            mainDisplay.PowerOnScreen();
            if (fanMotor != null) fanMotor.StartBoot();
            // Give the physical switch and LCD backlight a short, visible lead before the dolly.
            yield return new WaitForSecondsRealtime(.20f);
            // Original camera timing and physical monitor framing; the LCD stays on the model.
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
            flightStart = start; flightEnd = end;
            flightControlA = controlA; flightControlB = controlB;
            flightStartRotation = startRotation; flightDuration = duration; flightReduced = reducedMotion;
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
            // The original reduced-motion flight arrives before firmware startup finishes.
            while (!mainDisplay.IsBootComplete) yield return null;
            mainDisplay.EnterDesktop();
            while (!mainDisplay.HasEnteredDesktop) yield return null;
            HasEnteredGame = true; IsTransitioning = false;
            hud.SetChromeAlpha(0);
            hud.SetLoading(-1, null);
            if (fanMotor != null) { fanMotor.SetStable(); fanMotor.BeginGameplayCountdown(); }
        }

        /// <summary>Called only after GameExit has successfully saved the active session.</summary>
        public bool TryReturnToTitle()
        {
            if (IsReturningToTitle) return true;
            if (!HasEnteredGame || IsTransitioning || mainDisplay == null || !mainDisplay.HasEnteredDesktop) return false;
            if (!Application.CanStreamedLevelBeLoaded(ScenePath)) return false;
            IsReturningToTitle = true; IsTransitioning = true;
            ReturnTransitionProgress = 0;
            CancelCapture(); SetHovered(null); hud.HideSettings();
            mainDisplay.BeginShutdown();
            StartCoroutine(ReturnToTitle());
            return true;
        }

        IEnumerator ReturnToTitle()
        {
            // Preserve the accepted shutdown: fade the physical LCD, then reverse the camera flight.
            const float fadeSeconds = .35f;
            float fanVolume = fan != null ? fan.volume : 0;
            if (fanMotor != null) fanMotor.ReleaseForShutdown();
            if (effects != null) effects.Stop();
            for (float elapsed = 0; elapsed < fadeSeconds; elapsed += CaseMenuMotion.FrameStep(Time.unscaledDeltaTime))
            {
                mainDisplay.SetShutdownProgress(elapsed / fadeSeconds);
                if (fan != null) fan.volume = fanVolume * (1 - elapsed / fadeSeconds);
                yield return null;
            }
            mainDisplay.SetShutdownProgress(1);
            if (fan != null) { fan.Stop(); fan.volume = 0; }
            mainDisplay.BeginDesktopReturn();
            float duration = Mathf.Max(.1f, flightDuration);
            for (float elapsed = 0; elapsed < duration; elapsed += CaseMenuMotion.FrameStep(Time.unscaledDeltaTime))
            {
                float raw = 1 - Mathf.Clamp01(elapsed / duration);
                ReturnTransitionProgress = 1 - raw;
                SetFlightPose(raw);
                hud.SetChromeAlpha(1 - Mathf.Clamp01(raw * 3f));
                yield return null;
            }
            menuCamera.transform.SetPositionAndRotation(flightStart, flightStartRotation);
            menuCamera.fieldOfView = initialFov;
            ReturnTransitionProgress = 1; hud.SetChromeAlpha(1);
            // Keep the landed title visible for a rendered frame before discarding the old session.
            yield return null;
            SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
        }

        void SetFlightPose(float raw)
        {
            float t = CaseMenuMotion.SmootherStep(raw);
            menuCamera.transform.position = flightReduced ? Vector3.Lerp(flightStart, flightEnd, t)
                : CaseMenuMotion.Bezier(flightStart, flightControlA, flightControlB, flightEnd, t);
            menuCamera.transform.rotation = CaseMenuMotion.Rotation(flightStartRotation, Quaternion.identity, raw);
            menuCamera.fieldOfView = Mathf.Lerp(initialFov, 32f, t);
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
