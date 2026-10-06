using System;
using System.Collections;
using System.Collections.Generic;
using HongmengOS.Aero2010;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LingGuangV05.CaseMenu
{
    /// <summary>
    /// Preloads the real desktop into the menu's physical monitor. There is one visible camera
    /// throughout; entering gameplay only releases input and the paused story/simulation hosts.
    /// All modifications are scene-instance changes, never shared rendering or asset changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CaseMenuMainDisplay : MonoBehaviour
    {
        public const float BootDuration = 4.2f;
        public bool IsReady { get; private set; }
        public string Error { get; private set; }
        public bool HasEnteredDesktop { get; private set; }
        public bool IsScreenPowered { get; private set; }
        public bool IsBootComplete { get; private set; }
        public float BootProgress { get; private set; }
        public bool IsBootScreenVisible => bootScreen != null && bootScreen.IsVisible;
        public AeroLcdInputSurface InputSurface => input;
        public ChapterOneRuntime Runtime => runtime;
        readonly List<MonoBehaviour> pausedHosts = new List<MonoBehaviour>();
        readonly List<GraphicRaycaster> pausedRaycasters = new List<GraphicRaycaster>();
        Camera view;
        Transform rig;
        GameObject preview;
        ChapterOneRuntime runtime;
        AeroLcdController lcd;
        AeroLcdInputSurface input;
        CanvasGroup physicalInput;
        bool loading, configuring, subscribed, lcdWasEnabled;
        bool screenStateCaptured;
        Color displayOnColor, lampOnColor, lampOnEmission;
        MaterialPropertyBlock lampProperties;
        Coroutine powerRoutine;
        CaseMenuBootScreen bootScreen;
        EnvironmentSnapshot environment;

        public IEnumerator Preload(Camera menuCamera, Transform displayRig, GameObject previewMonitor)
        {
            if (IsReady || loading) yield break;
            if (menuCamera == null || displayRig == null)
            { Error = "主菜单相机或显示器安装点缺失。"; yield break; }
            Error = null; loading = true;
            view = menuCamera; rig = displayRig; preview = previewMonitor;
            environment = EnvironmentSnapshot.Capture();
            view.cullingMask &= ~(1 << 5);
            var main = SceneManager.GetSceneByPath(MainSceneEntry.ScenePath);
            if (main.IsValid() && main.isLoaded)
            {
                Configure(main);
                loading = false;
                yield break;
            }
            if (!Application.CanStreamedLevelBeLoaded(MainSceneEntry.ScenePath))
            { Error = "Main 场景不在构建列表中。"; loading = false; yield break; }

            // sceneLoaded runs after Awake/OnEnable but before Start. Configure synchronously here:
            // waiting for isDone before pausing the presenter would let its prologue Start run first.
            SceneManager.sceneLoaded += OnSceneLoaded; subscribed = true;
            AsyncOperation operation = null;
            try { operation = SceneManager.LoadSceneAsync(MainSceneEntry.ScenePath, LoadSceneMode.Additive); }
            catch (Exception exception) { Error = "提前载入桌面失败：" + exception.Message; }
            if (operation == null)
            { Unsubscribe(); loading = false; yield break; }
            while (!operation.isDone) yield return null;
            Unsubscribe();
            if (!IsReady && string.IsNullOrEmpty(Error))
            {
                main = SceneManager.GetSceneByPath(MainSceneEntry.ScenePath);
                if (main.IsValid() && main.isLoaded) Configure(main);
                else Error = "Main 载入后不可用。";
            }
            loading = false;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path == MainSceneEntry.ScenePath) Configure(scene);
        }

        void Configure(Scene scene)
        {
            if (IsReady || configuring || view == null || rig == null) return;
            configuring = true;
            GameObject physical = null, model = null;
            try
            {
                physical = Root(scene, "HongmengOS Monitor");
                model = Root(scene, "HongmengOS 2010 LCD Model");
                runtime = FindInScene<ChapterOneRuntime>(scene);
                if (physical == null || model == null || runtime == null)
                    throw new InvalidOperationException("Main 缺少原生显示器、模型或游戏运行时。");
                lcd = physical.GetComponent<AeroLcdController>();
                input = physical.GetComponentInChildren<AeroLcdInputSurface>(true);
                if (lcd == null || input == null || lcd.sourceCamera == null || lcd.display == null)
                    throw new InvalidOperationException("Main 的 LCD 渲染或输入桥未配置完整。");

                // Retain the menu's input module; the source relay recreates event data when current changes.
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var eventSystem in root.GetComponentsInChildren<EventSystem>(true))
                    {
                        eventSystem.enabled = false;
                        foreach (var module in eventSystem.GetComponents<BaseInputModule>()) module.enabled = false;
                    }
                    foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                        if (camera != lcd.sourceCamera) camera.enabled = false;
                    foreach (var listener in root.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
                    foreach (var framing in root.GetComponentsInChildren<AeroMonitorFraming>(true)) framing.enabled = false;
                }
                var studio = Root(scene, "LCD Studio Lighting");
                if (studio != null) studio.SetActive(false);

                if (!SceneManager.SetActiveScene(scene)) throw new InvalidOperationException("无法激活已载入的 Main。");
                environment.Apply();
                if (!MainSceneEntry.EnsureInitialized()) throw new InvalidOperationException("Main 启动桥未完成初始化。");

                // Pause only gameplay hosts; native UI, its source camera and the local model keep warming up.
                foreach (var host in runtime.GetComponents<MonoBehaviour>())
                    if (host != null && host.enabled && !(host is LocalLlm))
                    { pausedHosts.Add(host); host.enabled = false; }
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var prologue in root.GetComponentsInChildren<PrologueDirector>(true))
                        if (prologue.enabled && !pausedHosts.Contains(prologue))
                        { pausedHosts.Add(prologue); prologue.enabled = false; }

                lcdWasEnabled = lcd.enabled;
                lcd.enabled = false; // Keeps the visible backlight, but rejects the OSD power/brightness controls.
                input.SetInputEnabled(false);
                foreach (var raycaster in physical.GetComponentsInChildren<GraphicRaycaster>(true))
                    if (raycaster.enabled) { pausedRaycasters.Add(raycaster); raycaster.enabled = false; }
                physicalInput = physical.GetComponent<CanvasGroup>();
                if (physicalInput == null) physicalInput = physical.AddComponent<CanvasGroup>();
                physicalInput.blocksRaycasts = false; physicalInput.interactable = false;
                Mount(physical.transform); Mount(model.transform);
                foreach (var transform in physical.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 0;
                foreach (var canvas in physical.GetComponentsInChildren<Canvas>(true)) canvas.worldCamera = view;
                lcd.sourceCamera.enabled = true;
                displayOnColor = lcd.display.color;
                lampProperties = new MaterialPropertyBlock();
                if (lcd.powerLamp != null)
                {
                    lcd.powerLamp.GetPropertyBlock(lampProperties);
                    lampOnColor = lampProperties.GetColor("_BaseColor");
                    lampOnEmission = lampProperties.GetColor("_EmissionColor");
                }
                screenStateCaptured = true;
                ApplyVisualPower(0);
                bootScreen = gameObject.AddComponent<CaseMenuBootScreen>();
                var owner = GetComponent<CaseMenuController>();
                bootScreen.Build(lcd.display.rectTransform, owner != null ? owner.font : TMPro.TMP_Settings.defaultFontAsset);
                if (preview != null) preview.SetActive(false);
                Canvas.ForceUpdateCanvases();
                IsReady = true;
            }
            catch (Exception exception)
            {
                Error = "桌面接入失败：" + exception.Message;
                if (physical != null) physical.SetActive(false);
                if (model != null) model.SetActive(false);
                if (input != null) input.SetInputEnabled(false);
                // An incomplete additive scene must not render over the menu or run its story.
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
                    foreach (var listener in root.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
                    foreach (var eventSystem in root.GetComponentsInChildren<EventSystem>(true)) eventSystem.enabled = false;
                }
                if (runtime != null)
                    foreach (var host in runtime.GetComponents<MonoBehaviour>())
                        if (host != null && host.enabled && !(host is LocalLlm))
                        { pausedHosts.Add(host); host.enabled = false; }
                if (view != null && view.gameObject.scene.IsValid()) SceneManager.SetActiveScene(view.gameObject.scene);
                environment.Apply();
                if (preview != null) preview.SetActive(true);
                Debug.LogException(exception, this);
            }
            finally { configuring = false; }
        }

        void Mount(Transform item)
        {
            Vector3 position = item.localPosition, scale = item.localScale;
            Quaternion rotation = item.localRotation;
            item.SetParent(rig, false);
            item.localPosition = position; item.localRotation = rotation; item.localScale = scale;
        }

        public void EnterDesktop()
        {
            if (!IsReady || !IsBootComplete || HasEnteredDesktop) return;
            HasEnteredDesktop = true;
            IsScreenPowered = true;
            if (powerRoutine != null) { StopCoroutine(powerRoutine); powerRoutine = null; }
            ApplyVisualPower(1);
            if (bootScreen != null) bootScreen.Hide();
            if (lcd != null) lcd.enabled = lcdWasEnabled;
            if (physicalInput != null) { physicalInput.blocksRaycasts = true; physicalInput.interactable = true; }
            foreach (var raycaster in pausedRaycasters) if (raycaster != null) raycaster.enabled = true;
            pausedRaycasters.Clear();
            if (input != null) input.SetInputEnabled(lcd != null && lcd.IsPowered);
            // Presenter Start runs only now, so prologue choices cannot be consumed before the camera arrives.
            foreach (var host in pausedHosts) if (host != null) host.enabled = true;
            pausedHosts.Clear();
        }

        /// <summary>Runs the fictional workstation startup on its LCD; the real source is already loaded.</summary>
        public void PowerOnScreen()
        {
            if (!IsReady || IsScreenPowered || !isActiveAndEnabled) return;
            IsScreenPowered = true;
            IsBootComplete = false; BootProgress = 0;
            if (bootScreen != null) bootScreen.Show();
            powerRoutine = StartCoroutine(WarmScreen());
        }

        IEnumerator WarmScreen()
        {
            // A visible boot interval is intentional game presentation, not simulated network progress.
            for (float elapsed = 0; elapsed < BootDuration; elapsed += CaseMenuMotion.FrameStep(Time.unscaledDeltaTime))
            {
                BootProgress = Mathf.Clamp01(elapsed / BootDuration);
                float backlight = CaseMenuMotion.SmootherStep(Mathf.InverseLerp(.15f, .55f, elapsed));
                ApplyVisualPower(backlight);
                if (bootScreen != null) bootScreen.SetProgress(BootProgress, backlight);
                yield return null;
            }
            ApplyVisualPower(1);
            BootProgress = 1; IsBootComplete = true;
            if (bootScreen != null) bootScreen.Hide();
            powerRoutine = null;
        }

        void ApplyVisualPower(float value)
        {
            if (!screenStateCaptured || lcd == null) return;
            value = Mathf.Clamp01(value);
            if (lcd.display != null)
                lcd.display.color = Color.Lerp(new Color(0, 0, 0, displayOnColor.a), displayOnColor, value);
            if (lcd.powerLamp != null)
            {
                lampProperties.SetColor("_BaseColor", Color.Lerp(new Color(.012f, .018f, .026f), lampOnColor, value));
                lampProperties.SetColor("_EmissionColor", Color.Lerp(Color.black, lampOnEmission, value));
                lcd.powerLamp.SetPropertyBlock(lampProperties);
            }
        }

        public void DisposeSession()
        {
            Unsubscribe();
            // Native scene loading is not cancellable. No activation gate is held by this bridge.
            if (IsReady && !HasEnteredDesktop) EnterDesktop();
        }
        void OnDestroy() { Unsubscribe(); }
        void Unsubscribe()
        {
            if (!subscribed) return;
            SceneManager.sceneLoaded -= OnSceneLoaded; subscribed = false;
        }
        static GameObject Root(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }
        static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }

        struct EnvironmentSnapshot
        {
            public AmbientMode ambientMode;
            public Color sky, equator, ground, ambient, fogColor;
            public float ambientIntensity, reflectionIntensity, fogDensity, fogStart, fogEnd;
            public bool fog;
            public FogMode fogMode;
            public Material skybox;
            public Light sun;
            public static EnvironmentSnapshot Capture() => new EnvironmentSnapshot
            {
                ambientMode = RenderSettings.ambientMode, sky = RenderSettings.ambientSkyColor,
                equator = RenderSettings.ambientEquatorColor, ground = RenderSettings.ambientGroundColor,
                ambient = RenderSettings.ambientLight, ambientIntensity = RenderSettings.ambientIntensity,
                reflectionIntensity = RenderSettings.reflectionIntensity, fog = RenderSettings.fog,
                fogMode = RenderSettings.fogMode, fogColor = RenderSettings.fogColor,
                fogDensity = RenderSettings.fogDensity, fogStart = RenderSettings.fogStartDistance,
                fogEnd = RenderSettings.fogEndDistance, skybox = RenderSettings.skybox, sun = RenderSettings.sun
            };
            public void Apply()
            {
                RenderSettings.ambientMode = ambientMode; RenderSettings.ambientSkyColor = sky;
                RenderSettings.ambientEquatorColor = equator; RenderSettings.ambientGroundColor = ground;
                RenderSettings.ambientLight = ambient; RenderSettings.ambientIntensity = ambientIntensity;
                RenderSettings.reflectionIntensity = reflectionIntensity; RenderSettings.fog = fog;
                RenderSettings.fogMode = fogMode; RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = fogDensity; RenderSettings.fogStartDistance = fogStart;
                RenderSettings.fogEndDistance = fogEnd; RenderSettings.skybox = skybox; RenderSettings.sun = sun;
            }
        }
    }
}
