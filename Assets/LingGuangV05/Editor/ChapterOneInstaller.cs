#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using HongmengOS.Aero2010;
using LingGuangV05.Desktop;
using LingGuangV05.Runtime;
using LingGuangV05.UI;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LingGuangV05.Editor
{
    /// <summary>
    /// Additive installer. Never edits a vendor asset, global setting or user-data file.
    /// The copied scene is the only serialized output; all app visuals are scene-local.
    /// </summary>
    public static partial class ChapterOneInstaller
    {
        public const string ScenePath = "Assets/LingGuangV05/Scenes/Main.unity";
        public const string SourceScenePath = "Assets/DreamOS - Complete OS UI/Scenes/2010desktop.unity";
        public const string FontPath = "Assets/Emergence/Resources/Fonts/SparkUI_Readable_CJK_SDF.asset";
        public const string AppTitle = "灵光.exe";
        private const string RuntimeName = "LingGuang Chapter One Runtime";

        private sealed class AppSpec
        {
            public readonly string id, title, sourceName, subtitle;
            public AppSpec(string id, string title, string sourceName, string subtitle)
            { this.id = id; this.title = title; this.sourceName = sourceName; this.subtitle = subtitle; }
        }

        private static readonly AppSpec[] Specs =
        {
            new AppSpec("lingguang", "灵光.exe", AppTitle, "神经网络 · 技能 · 我的 AI · 考试"),
            new AppSpec("mail", "邮件", "Mail", "收件箱 · 数据标注"),
            new AppSpec("xunbao", "寻宝", null, "电脑硬件购物"),
            new AppSpec("home", "家庭", null, "家庭收入 · 用电账单"),
            new AppSpec("yy", "YY", "Messaging", "联系人 · 老周")
        };

        [MenuItem("Tools/LingGuang/Install Chapter One Desktop")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("第一章安装器只能在已完成编译的编辑模式运行。");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("先保存或放弃当前场景修改。安装器不会覆盖未保存的工作。");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) throw new InvalidOperationException("缺少只读中文字体：" + FontPath);
            EnsureFolder("Assets/LingGuangV05/Scenes");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                if (!AssetDatabase.CopyAsset(SourceScenePath, ScenePath))
                    throw new InvalidOperationException("无法复制原始桌面场景。原始场景未被修改。");
                AssetDatabase.ImportAsset(ScenePath);
            }
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var lcd = FindInScene<AeroLcdSourceBridge>(scene);
            if (lcd == null || lcd.sourceCanvas == null || lcd.sourceCamera == null)
                throw new InvalidOperationException("Main 场景缺少现有 Aero LCD 桌面接入。");
            var template = FindWindow(lcd.sourceCanvas, "Notepad");
            if (template == null || template.windowContainer == null || template.windowDragger == null
                || template.taskbarButton == null || template.taskbarButton.contextMenu == null)
                throw new InvalidOperationException("原桌面缺少完整 Notepad 窗口/任务栏模板。");
            var desktop = template.transform.parent.parent;
            var desktopList = desktop.Find("Desktop List");
            var layout = template.taskbarButton.GetComponentInParent<AeroTaskbarLayout>();
            var startMenu = desktop.GetComponent<AeroStartMenu>();
            if (desktopList == null || layout == null || desktopList.GetComponent<ItemDragContainer>() == null)
                throw new InvalidOperationException("缺少桌面图标容器、ItemDragContainer 或 Aero 任务栏布局。");

            var runtime = FindInScene<ChapterOneRuntime>(scene);
            int runtimeCount = 0;
            foreach (var root in scene.GetRootGameObjects())
                runtimeCount += root.GetComponentsInChildren<ChapterOneRuntime>(true).Length;
            if (runtimeCount > 1) throw new InvalidOperationException("Main 有多个第一章运行时；拒绝安装以避免状态分叉。");
            if (runtime == null)
            {
                var runtimeObject = new GameObject(RuntimeName);
                SceneManager.MoveGameObjectToScene(runtimeObject, scene);
                runtime = runtimeObject.AddComponent<ChapterOneRuntime>();
            }
            if (runtime.GetComponentInParent<Canvas>() != null)
                throw new InvalidOperationException("运行时必须位于独立场景对象，不能依赖任何窗口/Canvas。");
            var runtimeSettings = new SerializedObject(runtime);
            runtimeSettings.FindProperty("useDiskSave").boolValue = true;
            runtimeSettings.ApplyModifiedPropertiesWithoutUndo();
            var router = runtime.GetComponent<ChapterOneDesktopRouter>();
            if (router != null && router.Bindings.Count == Specs.Length)
            {
                foreach (var spec in Specs)
                {
                    var current = router.Get(spec.id);
                    Validate(current);
                    if (current.Runtime != runtime)
                        throw new InvalidOperationException("五个软件必须引用同一个第一章运行时。");
                    EnsureDesktopDrag(current.DesktopShortcut.transform, desktopList);
                }
                Save(scene);
                if (!router.Get("yy").IsNativeChat) ChapterOneNativeChatInstaller.Install();
                Debug.Log("[LingGuang] 五个软件已安装；已检查原生桌面拖动兼容与生产存档设置。", router);
                return;
            }

            var bindings = new List<ChapterOneDesktopBridge>(Specs.Length);
            foreach (var spec in Specs)
            {
                ChapterOneDesktopBridge bridge = null;
                foreach (var candidate in runtime.GetComponentsInChildren<ChapterOneDesktopBridge>(true))
                    if (candidate.ApplicationId == spec.id) { bridge = candidate; break; }
                if (bridge != null && bridge.IsNative)
                {
                    // Explicit native restoration is authoritative. Never replace
                    // its Messaging hierarchy with ChapterOneApp on a later rerun.
                    Validate(bridge);
                    EnsureDesktopDrag(bridge.DesktopShortcut.transform, desktopList);
                    bindings.Add(bridge);
                    continue;
                }
                WindowManager window = bridge != null ? bridge.Window : null;
                if (window == null && spec.sourceName != null) window = FindWindow(lcd.sourceCanvas, spec.sourceName);
                if (window == null) window = FindWindow(lcd.sourceCanvas, spec.title);
                bool reusedNative = spec.id == "mail" || spec.id == "yy";
                if (reusedNative && window == null)
                    throw new InvalidOperationException("缺少必须复用的原生窗口：" + spec.sourceName);
                var taskbar = window != null ? window.taskbarButton : null;
                if (window == null)
                    window = CloneWindow(template, lcd, font, spec, out _, out _, out _, out _);
                else
                    PrepareWindow(window, font, spec, out _, out _, out _, out _);
                var content = window.windowContent;
                var close = window.transform.Find("Container/Dragger/ChapterOneCaptionButtons/ChapterOneClose").GetComponent<Button>();
                var minimize = window.transform.Find("Container/Dragger/ChapterOneCaptionButtons/ChapterOneMinimize").GetComponent<Button>();
                var maximize = window.transform.Find("Container/Dragger/ChapterOneCaptionButtons/ChapterOneMaximize").GetComponent<Button>();
                var app = content.gameObject.AddComponent<ChapterOneApp>();
                if (bridge == null)
                {
                    var holder = new GameObject("ChapterOne Application " + spec.id);
                    holder.transform.SetParent(runtime.transform, false);
                    bridge = holder.AddComponent<ChapterOneDesktopBridge>();
                }
                if (taskbar == null) taskbar = CloneTaskbar(template.taskbarButton, window, font, layout, spec);
                else ConfigureTaskbar(taskbar, window, font, layout, spec, !reusedNative);
                Button shortcut = bridge.DesktopShortcut;
                if (shortcut == null && reusedNative)
                {
                    var native = desktopList.Find(spec.sourceName);
                    if (native == null) throw new InvalidOperationException("缺少必须复用的桌面入口：" + spec.sourceName);
                    shortcut = PrepareNativeShortcut(native, font, spec);
                }
                if (shortcut == null) shortcut = CreateDesktopShortcut(desktopList, font, spec);
                EnsureDesktopDrag(shortcut.transform, desktopList);
                shortcut.onClick = new Button.ButtonClickedEvent();
                close.onClick = new Button.ButtonClickedEvent();
                minimize.onClick = new Button.ButtonClickedEvent();
                maximize.onClick = new Button.ButtonClickedEvent();
                UnityEventTools.AddPersistentListener(shortcut.onClick, bridge.OpenDefaultApp);
                UnityEventTools.AddPersistentListener(close.onClick, bridge.CloseApp);
                UnityEventTools.AddPersistentListener(minimize.onClick, bridge.MinimizeApp);
                UnityEventTools.AddPersistentListener(maximize.onClick, bridge.ToggleMaximizeApp);
                Transform startRow = null;
                if (startMenu != null)
                {
                    UnityEventTools.AddPersistentListener(shortcut.onClick, startMenu.Close);
                    UnityEventTools.AddPersistentListener(taskbar.onClick, startMenu.Close);
                    startRow = AddStartMenuEntry(startMenu, window, bridge, font, spec);
                }
                var labels = new List<TMP_Text>();
                labels.AddRange(shortcut.GetComponentsInChildren<TMP_Text>(true));
                labels.AddRange(taskbar.GetComponentsInChildren<TMP_Text>(true));
                if (startRow != null) labels.AddRange(startRow.GetComponentsInChildren<TMP_Text>(true));
                bridge.Configure(runtime, app, window, content, font, shortcut, taskbar, close, minimize, maximize,
                    spec.id, labels.ToArray());
                foreach (var label in shortcut.GetComponentsInChildren<TMP_Text>(true)) { label.text = spec.title; label.font = font; }
                window.gameObject.SetActive(true);
                taskbar.gameObject.SetActive(true);
                SetLayer(window.transform, 5); SetLayer(taskbar.transform, 5); SetLayer(shortcut.transform, 5);
                foreach (var raycaster in window.GetComponentsInChildren<GraphicRaycaster>(true)) raycaster.enabled = false;
                foreach (var canvas in window.GetComponentsInChildren<Canvas>(true)) canvas.worldCamera = lcd.sourceCamera;
                Validate(bridge);
                bindings.Add(bridge);
            }
            if (router == null) router = runtime.gameObject.AddComponent<ChapterOneDesktopRouter>();
            router.Configure(bindings.ToArray());
            // SaveDesktopOrder and ItemDragger.UpdateIndexData enumerate every immediate child.
            foreach (Transform child in desktopList)
                if (child.GetComponent<ItemDragger>() == null || child.GetComponent<ItemDragger>().dragContainer == null)
                    throw new InvalidOperationException("桌面入口缺少原生拖动引用：" + child.name);
            Save(scene);
            if (!router.Get("yy").IsNativeChat) ChapterOneNativeChatInstaller.Install();
            Selection.activeGameObject = runtime.gameObject;
            Debug.Log("[LingGuang] 五个独立软件已安装：邮件、灵光.exe、寻宝、家庭、YY；同一运行时。", router);
        }

        private static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Main 场景保存失败。原始场景未修改。");
        }

        private static WindowManager CloneWindow(WindowManager template, AeroLcdSourceBridge lcd,
            TMP_FontAsset font, AppSpec spec, out RectTransform content, out Button close, out Button minimize, out Button maximize)
        {
            var clone = Object.Instantiate(template.gameObject, template.transform.parent, false);
            clone.name = spec.title;
            clone.SetActive(false);
            Unpack(clone);
            var window = clone.GetComponent<WindowManager>();
            PrepareWindow(window, font, spec, out content, out close, out minimize, out maximize);
            return window;
        }

        private static void PrepareWindow(WindowManager window, TMP_FontAsset font, AppSpec spec,
            out RectTransform content, out Button close, out Button minimize, out Button maximize)
        {
            var clone = window.gameObject;
            clone.SetActive(false);
            var container = window.windowContainer;
            content = container.Find("Content") as RectTransform;
            var dragger = container.Find("Dragger") as RectTransform;
            if (content == null || dragger == null)
                throw new InvalidOperationException("复制窗口缺少 Content/Dragger，安装未保存。");

            // Remove only app content/controllers in Main's targeted window. Native
            // animation paths/Container remain, and all source scene/assets stay untouched.
            ClearChildren(content);
            ClearChildren(dragger);
            foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || behaviour is WindowManager || behaviour is WindowDragger) continue;
                string ns = behaviour.GetType().Namespace ?? string.Empty;
                if (ns.StartsWith("UnityEngine.UI", StringComparison.Ordinal) || ns == "TMPro") continue;
                Object.DestroyImmediate(behaviour);
            }
            foreach (var animator in clone.GetComponentsInChildren<Animator>(true))
                if (animator.gameObject != clone) Object.DestroyImmediate(animator);
            foreach (var transform in clone.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);

            window.windowAnimator = clone.GetComponent<Animator>();
            window.windowContent = content;
            window.navbarRect = null;
            window.taskbarButton = null;
            window.windowDragger = dragger.GetComponent<WindowDragger>();
            window.windowDragger.dragObject = container;
            window.windowDragger.dragArea = window.transform.parent as RectTransform;
            window.windowDragger.wManager = window;
            window.resizeAnchor = WindowManager.ResizeAnchor.Disabled;
            window.useBackgroundBlur = false;
            window.allowGestures = true;
            window.isOn = false;
            window.isFullscreen = false;
            window.isNormalized = false;
            window.onOpen = new UnityEvent();
            window.onClose = new UnityEvent();
            window.onMinimize = new UnityEvent();
            window.onFullscreen = new UnityEvent();
            var serialized = new SerializedObject(window);
            serialized.FindProperty("hasNavDrawer").boolValue = false;
            serialized.FindProperty("disableAtStart").boolValue = true;
            serialized.FindProperty("enableMobileMode").boolValue = false;
            serialized.FindProperty("waitForAnimationCompletion").boolValue = true;
            serialized.FindProperty("resizePreset").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Stretch((RectTransform)window.transform, Vector2.zero, Vector2.zero);
            var parentRect = window.transform.parent as RectTransform;
            float width = parentRect != null && parentRect.rect.width > 100 ? parentRect.rect.width : 1920;
            float height = parentRect != null && parentRect.rect.height > 100 ? parentRect.rect.height : 1026;
            Vector2 margin = new Vector2(Mathf.Max(8, (width - 1250) * .5f), Mathf.Max(8, (height - 760) * .5f));
            Stretch(container, margin, -margin);
            Stretch(content, new Vector2(7, 28), new Vector2(-7, -39));
            // Keep the native animation hierarchy path "Container/Content".
            var contentImage = content.GetComponent<Image>() ?? content.gameObject.AddComponent<Image>();
            contentImage.color = new Color32(239, 245, 250, 255);
            contentImage.material = null;
            contentImage.sprite = null;
            contentImage.raycastTarget = true;
            var blur = container.Find("Background Blur");
            if (blur != null) blur.gameObject.SetActive(false);

            var title = Text("ChapterOneTitle", dragger, spec.title + "  —  " + spec.subtitle, font, 18, new Color32(25, 53, 74, 255));
            Stretch(title.rectTransform, new Vector2(38, 0), new Vector2(-150, 0));
            var titleIcon = Icon("ChapterOneTitleIcon", dragger, spec.id);
            titleIcon.anchorMin = titleIcon.anchorMax = new Vector2(0, .5f);
            titleIcon.anchoredPosition = new Vector2(18, 0);
            titleIcon.sizeDelta = new Vector2(25, 25);

            var captions = Rect("ChapterOneCaptionButtons", dragger);
            captions.anchorMin = captions.anchorMax = Vector2.one;
            captions.pivot = Vector2.one;
            captions.anchoredPosition = new Vector2(0, 1);
            captions.sizeDelta = new Vector2(137, 28);
            minimize = Caption("ChapterOneMinimize", captions, "—", font, 0, 40, false);
            maximize = Caption("ChapterOneMaximize", captions, "□", font, 41, 40, false);
            close = Caption("ChapterOneClose", captions, "×", font, 82, 55, true);
            window.fullscreenImage = maximize.transform.Find("Label").gameObject;
            var normalize = Text("RestoreLabel", maximize.transform, "▣", font, 18, new Color32(25, 53, 74, 255));
            normalize.alignment = TextAlignmentOptions.Center;
            Stretch(normalize.rectTransform, Vector2.zero, Vector2.zero);
            normalize.gameObject.SetActive(false);
            window.normalizeImage = normalize.gameObject;
            var status = container.Find("Aero Status Bar/Status")?.GetComponent<TMP_Text>();
            if (status != null)
            {
                status.font = font;
                status.text = "第一章 / 规则模拟 · 窗口关闭后系统继续运行";
                status.fontSize = 13;
            }
            SetLayer(clone.transform, 5);
        }

        private static TaskbarButton CloneTaskbar(TaskbarButton source, WindowManager window,
            TMP_FontAsset font, AeroTaskbarLayout layout, AppSpec spec)
        {
            var clone = Object.Instantiate(source.gameObject, source.transform.parent, false);
            clone.name = "ChapterOneTaskbarShortcut_" + spec.id;
            Unpack(clone);
            var button = clone.GetComponent<TaskbarButton>();
            ConfigureTaskbar(button, window, font, layout, spec, true);
            return button;
        }

        private static void ConfigureTaskbar(TaskbarButton button, WindowManager window,
            TMP_FontAsset font, AeroTaskbarLayout layout, AppSpec spec, bool replaceIcon)
        {
            var clone = button.gameObject;
            button.buttonTitle = spec.title;
            button.windowManager = window;
            button.buttonAnimator = clone.GetComponent<Animator>();
            button.appElements = new List<AppElement>();
            button.onClick = new UnityEvent();
            window.taskbarButton = button;
            var serialized = new SerializedObject(button);
            serialized.FindProperty("openWindowsOnly").boolValue = true;
            serialized.FindProperty("defaultPinState").enumValueIndex = (int)TaskbarButton.DefaultPinState.Unpinned;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            RemoveTextDrivers(clone.transform);
            foreach (var manager in clone.GetComponentsInChildren<ButtonManager>(true))
            {
                manager.useLocalization = false;
                manager.useSounds = manager.useRipple = manager.useHoverEffect = false;
                manager.onClick = new UnityEvent();
                manager.onDoubleClick = new UnityEvent();
                manager.buttonIcon = null;
                manager.enableIcon = false;
            }
            foreach (var text in clone.GetComponentsInChildren<TMP_Text>(true)) text.font = font;
            ConfigureContextLabel(button.headerButton, spec.title);
            ConfigureContextLabel(button.closeButton, "关闭窗口");
            ConfigureContextLabel(button.pinButton, "固定到任务栏");
            ConfigureContextLabel(button.unpinButton, "取消固定");
            if (button.pinButton != null) button.pinButton.gameObject.SetActive(false);
            if (button.unpinButton != null) button.unpinButton.gameObject.SetActive(false);
            var icon = clone.transform.Find("Icon") as RectTransform;
            if (replaceIcon || icon == null)
            {
                if (icon != null) Object.DestroyImmediate(icon.gameObject);
                icon = Icon("Icon", clone.transform, spec.id);
            }
            icon.anchorMin = icon.anchorMax = new Vector2(0, .5f);
            icon.sizeDelta = new Vector2(36, 36);
            icon.anchoredPosition = new Vector2(34, 0);
            var hover = clone.GetComponent<AeroShellHover>();
            if (hover != null && hover.tooltip != null)
                foreach (var text in hover.tooltip.GetComponentsInChildren<TMP_Text>(true)) text.text = spec.title;
            var visibility = clone.GetComponent<CanvasGroup>() ?? clone.AddComponent<CanvasGroup>();
            visibility.alpha = 0;
            visibility.interactable = visibility.blocksRaycasts = false;
            var face = clone.GetComponentInChildren<AeroShellGraphic>(true);
            var entries = new List<AeroTaskbarLayout.Entry>(layout.entries ?? Array.Empty<AeroTaskbarLayout.Entry>());
            var entry = new AeroTaskbarLayout.Entry
            {
                button = button, window = window, rect = (RectTransform)clone.transform,
                icon = icon, face = face, visibility = visibility,
                windowGroup = window.GetComponent<CanvasGroup>()
            };
            int existing = entries.FindIndex(item => item.window == window || item.button == button);
            if (existing >= 0) entries[existing] = entry;
            else entries.Add(entry);
            layout.entries = entries.ToArray();
        }

        private static Button PrepareNativeShortcut(Transform shortcut, TMP_FontAsset font, AppSpec spec)
        {
            // Keep this exact native entry and its icon/drag identity, but give it
            // one unified launch handler (no native double-click plus uGUI duplicate).
            var manager = shortcut.GetComponent<ButtonManager>();
            if (manager != null) Object.DestroyImmediate(manager);
            RemoveTextDrivers(shortcut);
            foreach (var label in shortcut.GetComponentsInChildren<TMP_Text>(true))
            { label.font = font; label.text = spec.title; }
            var image = shortcut.GetComponent<Image>() ?? shortcut.gameObject.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            var button = shortcut.GetComponent<Button>() ?? shortcut.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(1, 1, 1, 0);
            colors.highlightedColor = new Color(.6f, .85f, 1, .25f);
            colors.pressedColor = new Color(.4f, .75f, 1, .45f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        private static void RemoveTextDrivers(Transform root)
        {
            foreach (var element in root.GetComponentsInChildren<AppElement>(true)) Object.DestroyImmediate(element);
            foreach (var localized in root.GetComponentsInChildren<LocalizedObject>(true)) Object.DestroyImmediate(localized);
            foreach (var themed in root.GetComponentsInChildren<UIManagerElement>(true)) Object.DestroyImmediate(themed);
        }

        private static void EnsureDesktopDrag(Transform shortcut, Transform desktopList)
        {
            var container = desktopList.GetComponent<ItemDragContainer>();
            if (container == null) throw new InvalidOperationException("Desktop List 缺少 ItemDragContainer。");
            var dragger = shortcut.GetComponent<ItemDragger>() ?? shortcut.gameObject.AddComponent<ItemDragger>();
            dragger.dragContainer = container;
            // Explicit serialization is required before SettingsManager/SwitchManager
            // Awake; ItemDragger.Start's lazy lookup would run too late.
            var serialized = new SerializedObject(dragger);
            var key = serialized.FindProperty("saveKey");
            if (key != null && string.IsNullOrEmpty(key.stringValue)) key.stringValue = "Desktop";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button CreateDesktopShortcut(Transform parent, TMP_FontAsset font, AppSpec spec)
        {
            var rect = Rect("ChapterOneDesktopShortcut_" + spec.id, parent);
            rect.sizeDelta = new Vector2(112, 112);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1, 1, 1, .01f);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.6f, .85f, 1, .45f);
            colors.pressedColor = new Color(.4f, .75f, 1, .65f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            button.targetGraphic = image;
            // The graphic alpha is in the tint, not the texture: keep a useful
            // visible hover while avoiding an opaque tile over the wallpaper.
            colors.normalColor = new Color(1, 1, 1, 0);
            button.colors = colors;
            image.color = Color.white;
            var icon = Icon("ChapterOneDesktopIcon", rect, spec.id);
            icon.anchorMin = icon.anchorMax = new Vector2(.5f, 1);
            icon.pivot = new Vector2(.5f, 1);
            icon.anchoredPosition = new Vector2(0, -4);
            icon.sizeDelta = new Vector2(60, 60);
            var label = Text("ChapterOneDesktopLabel", rect, spec.title, font, 18, Color.white);
            label.alignment = TextAlignmentOptions.Top;
            label.rectTransform.anchorMin = new Vector2(0, 0);
            label.rectTransform.anchorMax = new Vector2(1, 0);
            label.rectTransform.pivot = new Vector2(.5f, 0);
            label.rectTransform.sizeDelta = new Vector2(0, 41);
            label.rectTransform.anchoredPosition = Vector2.zero;
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, .9f);
            shadow.effectDistance = new Vector2(1, -1);
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        private static Transform AddStartMenuEntry(AeroStartMenu menu, WindowManager window,
            ChapterOneDesktopBridge bridge, TMP_FontAsset font, AppSpec spec)
        {
            var windows = new List<WindowManager>(menu.windows ?? Array.Empty<WindowManager>());
            if (!windows.Contains(window)) windows.Add(window);
            menu.windows = windows.ToArray();
            if (menu.programScroll == null || menu.programScroll.content == null) return null;
            var entries = new List<AeroStartMenu.ProgramEntry>(menu.programs ?? Array.Empty<AeroStartMenu.ProgramEntry>());
            int existing = entries.FindIndex(item => item.window == window);
            RectTransform row = existing >= 0 && entries[existing].row != null
                ? entries[existing].row.transform as RectTransform : null;
            Button button;
            if (row == null)
            {
                row = Rect("ChapterOneStartMenuShortcut_" + spec.id, menu.programScroll.content);
                row.sizeDelta = new Vector2(360, 40);
                var layout = row.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = layout.minHeight = 40;
                var background = row.gameObject.AddComponent<Image>();
                background.color = new Color32(237, 246, 253, 255);
                button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                var icon = Icon("ChapterOneStartMenuIcon", row, spec.id);
                icon.anchorMin = icon.anchorMax = new Vector2(0, .5f);
                icon.anchoredPosition = new Vector2(22, 0);
                icon.sizeDelta = new Vector2(30, 30);
                var label = Text("Label", row, spec.title, font, 18, new Color32(29, 48, 65, 255));
                Stretch(label.rectTransform, new Vector2(48, 0), new Vector2(-8, 0));
            }
            else button = row.GetComponent<Button>();
            if (button == null) throw new InvalidOperationException("开始菜单入口缺少标准按钮：" + spec.title);
            RemoveTextDrivers(row);
            foreach (var label in row.GetComponentsInChildren<TMP_Text>(true)) { label.text = spec.title; label.font = font; }
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, bridge.OpenDefaultApp);
            UnityEventTools.AddPersistentListener(button.onClick, menu.Close);
            var entry = new AeroStartMenu.ProgramEntry
            {
                title = spec.title, searchKeywords = spec.id + " " + spec.title + " " + spec.subtitle,
                row = row.gameObject, window = window
            };
            if (existing >= 0) entries[existing] = entry;
            else entries.Add(entry);
            menu.programs = entries.ToArray();
            SetLayer(row, 5);
            return row;
        }

        private static Button Caption(string name, Transform parent, string label,
            TMP_FontAsset font, float x, float width, bool danger)
        {
            var rect = Rect(name, parent);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, 0);
            rect.sizeDelta = new Vector2(width, 28);
            var face = rect.gameObject.AddComponent<Image>();
            face.color = danger ? new Color32(175, 49, 40, 255) : new Color32(169, 213, 242, 255);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            // Existing pointer-only caption handler swallows caption-button drags,
            // so they never turn into window dragging through the title bar.
            rect.gameObject.AddComponent<AeroCaptionFeedback>().face = face;
            var text = Text("Label", rect, label, font, 18, danger ? Color.white : new Color32(25, 53, 74, 255));
            text.alignment = TextAlignmentOptions.Center;
            text.overflowMode = TextOverflowModes.Overflow;
            Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void ConfigureContextLabel(ButtonManager button, string label)
        {
            if (button == null) return;
            button.buttonText = label;
            button.UpdateUI();
            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true)) text.text = label;
        }

        private static void Validate(ChapterOneDesktopBridge bridge)
        {
            if (bridge == null || bridge.Runtime == null || bridge.Window == null
                || bridge.DesktopShortcut == null || bridge.TaskbarShortcut == null)
                throw new InvalidOperationException("第一章桌面接入不完整；未保存场景。请检查安装过程。");
            if (bridge.IsNativeChat)
            {
                if (bridge.NativeChat.Manager == null || bridge.NativeChat.Window != bridge.Window
                    || bridge.NativeChat.Runtime != bridge.Runtime)
                    throw new InvalidOperationException("原生 YY 绑定不完整；拒绝覆盖为自绘界面。");
            }
            else if (bridge.NativeApp != null)
            {
                if (bridge.NativeApp.Runtime != bridge.Runtime || bridge.Content == null)
                    throw new InvalidOperationException("原生应用绑定不完整；拒绝覆盖为旧界面。");
            }
            else if (bridge.App == null || bridge.Content == null || bridge.CloseButton == null
                || bridge.MinimizeButton == null || bridge.MaximizeButton == null)
                throw new InvalidOperationException("第一章应用视图接入不完整；未保存场景。");
            if (bridge.Runtime.transform.IsChildOf(bridge.Window.transform)
                || bridge.Runtime.GetComponentInParent<Canvas>() != null)
                throw new InvalidOperationException("第一章运行时不能放在窗口或桌面 Canvas 内。");
            if (bridge.gameObject.scene.path != ScenePath)
                throw new InvalidOperationException("第一章接入只能保存在 Main 副本中。");
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        private static WindowManager FindWindow(Canvas canvas, string name)
        {
            foreach (var window in canvas.GetComponentsInChildren<WindowManager>(true))
                if (window.name == name) return window;
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        private static void Unpack(GameObject instance)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(instance))
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--) Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void SetLayer(Transform root, int layer)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.gameObject.layer = 5;
            return rect;
        }

        private static RectTransform Icon(string name, Transform parent, string appId = "lingguang")
        {
            var rect = Rect(name, parent);
            var graphic = rect.gameObject.AddComponent<ChapterOneIconGraphic>();
            graphic.applicationId = appId;
            graphic.raycastTarget = false;
            return rect;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string value,
            TMP_FontAsset font, float size, Color color)
        {
            var rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = minimum;
            rect.offsetMax = maximum;
            rect.localScale = Vector3.one;
        }
    }
}
#endif
