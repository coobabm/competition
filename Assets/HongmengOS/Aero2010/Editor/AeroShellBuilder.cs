using System;
using System.Collections.Generic;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HongmengOS.Aero2010.Editor
{
    /// <summary>Builds only the copied scene's Aero shell; shared DreamOS assets are never edited.</summary>
    public static class AeroShellBuilder
    {
        private static readonly Color Ink = new Color32(34, 48, 62, 255);
        private static TMP_FontAsset font;
        private static Dictionary<string, Sprite> iconMap;
        private static Dictionary<string, WindowManager> windows;

        public static void Apply(Transform desktop, Dictionary<string, Sprite> icons, TMP_FontAsset shellFont)
        {
            if (EditorApplication.isPlaying || desktop == null) throw new InvalidOperationException("Aero shell requires the copied desktop in Edit mode.");
            if (desktop.gameObject.scene.path != AeroDesktopStyleEditor.ScenePath)
                throw new InvalidOperationException("Only an Aero2010 scene may receive this shell.");
            if (desktop.Find("Aero Shell Controller") != null) return;
            Transform taskbar = Required(desktop, "Taskbar"), list = Required(taskbar, "Shortcut List"), start = Required(taskbar, "Quick Center Button");
            Transform appRoot = Required(desktop, "Apps & Windows");
            font = shellFont != null ? shellFont : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null) throw new InvalidOperationException("A shell TMP font is required.");
            iconMap = icons ?? new Dictionary<string, Sprite>();
            windows = new Dictionary<string, WindowManager>(StringComparer.OrdinalIgnoreCase);
            var allWindows = appRoot.GetComponentsInChildren<WindowManager>(true);
            foreach (var window in allWindows) if (!windows.ContainsKey(window.name)) windows.Add(window.name, window);

            var host = new GameObject("Aero Shell Controller", typeof(AeroStartMenu));
            host.transform.SetParent(desktop, false);
            var menu = host.GetComponent<AeroStartMenu>();
            menu.windows = allWindows;
            menu.controlPanelWindow = FindWindow("Settings");
            menu.computerWindow = FindWindow("Computer") ?? menu.controlPanelWindow;
            menu.userWindow = menu.controlPanelWindow;
            BuildTaskbar(desktop, taskbar, list, start, menu);
            BuildMenu(desktop, menu);
            var quickCenter = desktop.Find("Quick Center");
            if (quickCenter != null) quickCenter.gameObject.SetActive(false);
        }

        private static void BuildTaskbar(Transform desktop, Transform taskbar, Transform list, Transform start, AeroStartMenu menu)
        {
            foreach (Transform child in taskbar) if (child != list && child != start) child.gameObject.SetActive(false);
            var bar = (RectTransform)taskbar;
            bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1, 0); bar.pivot = new Vector2(.5f, 0);
            bar.offsetMin = Vector2.zero; bar.offsetMax = new Vector2(0, 54);
            var background = Surface("Aero Glass Taskbar", taskbar, AeroShellGraphic.Shape.Glass);
            background.radius = 0;
            background.top = new Color(.30f, .52f, .73f, .74f); background.bottom = new Color(.035f, .15f, .29f, .94f);
            Stretch(background.rectTransform); background.transform.SetAsFirstSibling();
            var gleam = Image("Glass top highlight", taskbar, new Color(.79f, .92f, 1, .7f));
            Rect(gleam.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0, -1), Vector2.zero);

            foreach (Transform child in start) child.gameObject.SetActive(false);
            var animator = start.GetComponent<Animator>(); if (animator != null) animator.enabled = false;
            var animated = start.GetComponent<AnimatedIconHandler>(); if (animated != null) animated.enabled = false;
            var startButton = start.GetComponent<ButtonManager>();
            if (startButton == null) throw new InvalidOperationException("The source Start button must have ButtonManager.");
            startButton.useCustomContent = true; startButton.bypassUpdateOnEnable = true; startButton.checkForDoubleClick = false;
            startButton.useSounds = false; startButton.useRipple = false; startButton.useHoverEffect = false;
            startButton.onClick = new UnityEvent(); startButton.onDoubleClick = new UnityEvent();
            UnityEventTools.AddPersistentListener(startButton.onClick, menu.Toggle);
            Box((RectTransform)start, 8, 0, 58, 54);
            var orb = Surface("Aero Start Orb", start, AeroShellGraphic.Shape.Orb);
            Box(orb.rectTransform, 4, 2, 50, 50); menu.startOrb = orb;
            AddHover(start.gameObject, orb);

            var listRect = (RectTransform)list;
            Rect(listRect, Vector2.zero, Vector2.one, new Vector2(78, 0), new Vector2(-250, 0));
            var layout = list.GetComponent<HorizontalLayoutGroup>(); if (layout != null) layout.enabled = false;
            var fitter = list.GetComponent<ContentSizeFitter>(); if (fitter != null) fitter.enabled = false;
            var entries = new List<AeroTaskbarLayout.Entry>();
            foreach (Transform child in list)
            {
                var button = child.GetComponent<TaskbarButton>(); if (button == null) continue;
                var window = FindWindow(button.buttonTitle) ?? FindWindow(child.name);
                var so = new SerializedObject(button);
                so.FindProperty("openWindowsOnly").boolValue = true;
                so.FindProperty("defaultPinState").enumValueIndex = (int)TaskbarButton.DefaultPinState.Unpinned;
                so.ApplyModifiedPropertiesWithoutUndo();
                var icon = child.Find("Icon") as RectTransform;
                foreach (Transform visual in child)
                    if (visual != icon && (button.contextMenu == null || visual != button.contextMenu.transform)) visual.gameObject.SetActive(false);
                if (icon != null)
                {
                    icon.gameObject.SetActive(true); icon.anchorMin = icon.anchorMax = new Vector2(0, .5f); icon.pivot = new Vector2(.5f, .5f);
                    var image = icon.GetComponent<Image>();
                    if (image != null) { FixedImage(image, Color.white); var sprite = Icon(button.buttonTitle); if (sprite != null) image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false; }
                }
                var rect = (RectTransform)child;
                rect.anchorMin = rect.anchorMax = new Vector2(0, .5f); rect.pivot = new Vector2(0, .5f); rect.sizeDelta = new Vector2(68, 48);
                var face = Surface("Aero Running App Glass", child, AeroShellGraphic.Shape.TaskButton);
                Stretch(face.rectTransform, new Vector2(1, 1), new Vector2(-1, -1)); face.transform.SetAsFirstSibling();
                var hover = AddHover(child.gameObject, face);
                hover.tooltip = Tooltip(child, button.buttonTitle);
                var visibility = child.GetComponent<CanvasGroup>() ?? child.gameObject.AddComponent<CanvasGroup>();
                visibility.alpha = 0; visibility.blocksRaycasts = false; visibility.interactable = false;
                UnityEventTools.AddPersistentListener(button.onClick, menu.Close);
                entries.Add(new AeroTaskbarLayout.Entry { button = button, window = window, rect = rect, icon = icon, face = face, visibility = visibility, windowGroup = window != null ? window.GetComponent<CanvasGroup>() : null });
            }
            var driver = list.gameObject.GetComponent<AeroTaskbarLayout>() ?? list.gameObject.AddComponent<AeroTaskbarLayout>(); driver.entries = entries.ToArray();

            var tray = NewRect("Aero Notification Area", taskbar);
            tray.anchorMin = new Vector2(1, 0); tray.anchorMax = Vector2.one; tray.pivot = new Vector2(1, .5f); tray.offsetMin = new Vector2(-242, 0); tray.offsetMax = Vector2.zero;
            var separator = Image("Tray separator", tray, new Color(.8f, .93f, 1, .20f)); Box(separator.rectTransform, 0, 7, 1, 39);
            var network = TrayButton("Network settings", tray, 15, AeroShellGraphic.Shape.Network); UnityEventTools.AddPersistentListener(network.onClick, menu.OpenControlPanel);
            var volume = TrayButton("Sound settings", tray, 55, AeroShellGraphic.Shape.Volume); UnityEventTools.AddPersistentListener(volume.onClick, menu.OpenControlPanel);
            var clock = NewRect("Aero Clock", tray); Box(clock, 101, 2, 120, 50);
            var time = Text("Time", clock, "00:00", 18, Color.white); Rect(time.rectTransform, Vector2.zero, Vector2.one, new Vector2(0, 22), new Vector2(0, 0)); time.alignment = TextAlignmentOptions.Center;
            var date = Text("Date", clock, "", 14, new Color(.93f, .97f, 1)); Rect(date.rectTransform, Vector2.zero, Vector2.one, new Vector2(0, 2), new Vector2(0, -25)); date.alignment = TextAlignmentOptions.Center;
            var clockDriver = clock.gameObject.AddComponent<AeroShellClock>(); clockDriver.timeLabel = time; clockDriver.dateLabel = date; clockDriver.desktopClock = desktop.GetComponentInParent<Canvas>()?.GetComponentInChildren<DateAndTimeManager>(true);
            var show = GlassButton("Show desktop", tray); Box((RectTransform)show.transform, 226, 0, 16, 54);
            UnityEventTools.AddPersistentListener(show.onClick, menu.ShowDesktop);
            show.GetComponent<AeroShellHover>().tooltip = Tooltip(show.transform, "Show desktop");
        }

        private static void BuildMenu(Transform desktop, AeroStartMenu control)
        {
            var overlayImage = Image("Aero Start Menu", desktop, Color.clear); Stretch(overlayImage.rectTransform); overlayImage.raycastTarget = true;
            control.menu = overlayImage.gameObject;
            var outside = overlayImage.gameObject.AddComponent<Button>(); outside.transition = Selectable.Transition.None; UnityEventTools.AddPersistentListener(outside.onClick, control.Close);
            var panel = NewRect("Aero Start Panel", overlayImage.transform); Box(panel, 4, 56, 660, 724); control.panel = panel;
            var panelHit = panel.gameObject.AddComponent<Image>(); panelHit.color = Color.clear; panelHit.raycastTarget = true; panel.gameObject.AddComponent<AeroStartMenuPanel>();
            var shadow = Image("Soft edge shadow", panel, new Color(0, 0, 0, .22f)); Stretch(shadow.rectTransform, new Vector2(-5, -6), new Vector2(6, 4));
            var glass = Surface("Blue Glass Frame", panel, AeroShellGraphic.Shape.Glass); Stretch(glass.rectTransform); glass.radius = 9;
            glass.top = new Color(.28f, .48f, .70f, .96f); glass.bottom = new Color(.06f, .18f, .32f, .97f);
            var pane = Surface("Programs Ivory Pane", panel, AeroShellGraphic.Shape.Glass); Rect(pane.rectTransform, Vector2.zero, Vector2.one, new Vector2(9, 9), new Vector2(-224, -10)); pane.radius = 6;
            pane.top = new Color(.995f, .998f, 1, 1); pane.bottom = new Color(.95f, .97f, .98f, 1);
            var heading = Text("Programs Heading", pane.transform, "Programs", 16, new Color(.37f, .44f, .50f)); Rect(heading.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(15, -35), new Vector2(-15, -7));
            var scrollRoot = NewRect("Programs Scroll", pane.transform); Rect(scrollRoot, Vector2.zero, Vector2.one, new Vector2(8, 100), new Vector2(-8, -41));
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 29; control.programScroll = scroll;
            var viewport = NewRect("Viewport", scrollRoot); Stretch(viewport); viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect("Program List", viewport); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>(); vlg.childControlWidth = true; vlg.childControlHeight = false; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false; vlg.spacing = 2; vlg.padding = new RectOffset(1, 1, 1, 1);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; scroll.content = content; scroll.viewport = viewport;
            var entries = new List<AeroStartMenu.ProgramEntry>();
            var gameSettings = ProgramRow("Settings", content, Icon("Settings"));
            gameSettings.name = "Game Settings";
            UnityEventTools.AddPersistentListener(gameSettings.onClick, control.OpenSettings);
            entries.Add(new AeroStartMenu.ProgramEntry { title = "Settings", searchKeywords = "设置 游戏 音量 全屏 帮助 settings volume fullscreen help", row = gameSettings.gameObject, gameSettings = true });
            var desktopList = desktop.Find("Desktop List");
            if (desktopList != null)
                foreach (Transform item in desktopList)
                {
                    WindowManager window = FindWindow(item.name); if (window == null) continue;
                    var button = ProgramRow(item.name, content, Icon(item.name));
                    UnityEventTools.AddPersistentListener(button.onClick, window.OpenWindow); UnityEventTools.AddPersistentListener(button.onClick, control.Close);
                    entries.Add(new AeroStartMenu.ProgramEntry { title = item.name, searchKeywords = Keywords(item.name), row = button.gameObject, window = window });
                }
            control.programs = entries.ToArray();
            var empty = Text("No search matches", viewport, "No programs match your search.", 18, new Color(.40f, .45f, .50f)); Stretch(empty.rectTransform, new Vector2(15, 0), new Vector2(-15, 0)); empty.alignment = TextAlignmentOptions.Center; control.emptyMessage = empty; empty.gameObject.SetActive(false);
            var divider = Image("Programs divider", pane.transform, new Color(.75f, .81f, .85f)); Rect(divider.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(11, 88), new Vector2(-11, 89));
            var all = ProgramRow("All Programs", pane.transform, null); Rect((RectTransform)all.transform, Vector2.zero, new Vector2(1, 0), new Vector2(9, 49), new Vector2(-9, 85));
            UnityEventTools.AddPersistentListener(all.onClick, control.ShowAllPrograms);
            var arrow = Surface("All Programs Arrow", all.transform, AeroShellGraphic.Shape.Chevron); Box(arrow.rectTransform, 9, 9, 14, 18); arrow.color = Ink;
            var allText = all.transform.Find("Label").GetComponent<TMP_Text>(); Stretch(allText.rectTransform, new Vector2(34, 0), new Vector2(-15, 0));
            control.search = SearchField(pane.transform);
            UnityEventTools.AddPersistentListener(control.search.onValueChanged, control.SetSearch);
            UnityEventTools.AddPersistentListener(control.search.onSubmit, control.SubmitSearch);

            var right = NewRect("Places And System", panel); Rect(right, new Vector2(1, 0), Vector2.one, new Vector2(-215, 13), new Vector2(-10, -10));
            var avatar = Surface("User portrait glass", right, AeroShellGraphic.Shape.Avatar); Rect(avatar.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(-37, -85), new Vector2(37, -11));
            string userName = desktop.GetComponentInParent<Canvas>()?.GetComponentInChildren<UserManager>(true)?.systemUsername;
            if (string.IsNullOrWhiteSpace(userName)) userName = "Player";
            var user = RightLink(userName, right, 96); UnityEventTools.AddPersistentListener(user.onClick, control.OpenUser);
            RightApp("Documents", "Notepad", right, 153, control);
            RightApp("Pictures", "Photo Gallery", right, 204, control);
            RightApp("Music", "Music Player", right, 255, control);
            var rule = Image("Places separator", right, new Color(.67f, .83f, .96f, .25f)); Rect(rule.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(10, -318), new Vector2(-10, -317));
            var computer = RightLink("Computer", right, 335); UnityEventTools.AddPersistentListener(computer.onClick, control.OpenComputer);
            var settings = RightLink("Control Panel", right, 386); UnityEventTools.AddPersistentListener(settings.onClick, control.OpenControlPanel);
            RightApp("Games", "Game Hub", right, 437, control);
            RightApp("Mail", "Mail", right, 488, control);
            var brand = Text("Edition", right, "hongmengos\nAero 2010", 14, new Color(.70f, .83f, .94f)); Rect(brand.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(13, 58), new Vector2(-8, 97));
            var shutdown = GlassButton("Shut down", right); Rect((RectTransform)shutdown.transform, Vector2.zero, new Vector2(1, 0), new Vector2(8, 10), new Vector2(-8, 47));
            var shutdownText = Text("Label", shutdown.transform, "Shut down", 18, Color.white); Stretch(shutdownText.rectTransform); shutdownText.alignment = TextAlignmentOptions.Center;
            UnityEventTools.AddPersistentListener(shutdown.onClick, control.RequestShutdown);
            overlayImage.gameObject.SetActive(false);
        }

        private static TMP_InputField SearchField(Transform parent)
        {
            var image = Image("Search programs", parent, new Color(.60f, .69f, .76f)); image.raycastTarget = true;
            Rect(image.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(13, 12), new Vector2(-13, 44));
            var paper = Image("Inset white", image.transform, Color.white); Stretch(paper.rectTransform, Vector2.one, -Vector2.one);
            var viewport = NewRect("Text Viewport", image.transform); Stretch(viewport, new Vector2(8, 2), new Vector2(-31, -2)); viewport.gameObject.AddComponent<RectMask2D>();
            var value = Text("Text", viewport, "", 17, Ink); Stretch(value.rectTransform); value.textWrappingMode = TextWrappingModes.NoWrap;
            var placeholder = Text("Placeholder", viewport, "Search programs", 17, new Color(.43f, .49f, .54f)); Stretch(placeholder.rectTransform);
            var field = image.gameObject.AddComponent<TMP_InputField>();
            field.targetGraphic = image; field.textViewport = viewport; field.textComponent = value; field.placeholder = placeholder;
            // TMP setters can immediately update both text components, so wire the children first.
            field.lineType = TMP_InputField.LineType.SingleLine; field.characterLimit = 80; field.pointSize = 17;
            field.customCaretColor = true; field.caretColor = Ink; field.selectionColor = new Color(.39f, .68f, .95f, .48f);
            var icon = Surface("Search glyph", image.transform, AeroShellGraphic.Shape.Search); Rect(icon.rectTransform, new Vector2(1, 0), Vector2.one, new Vector2(-26, 5), new Vector2(-5, -5));
            return field;
        }
        private static Button ProgramRow(string title, Transform parent, Sprite sprite)
        {
            var image = Image(title, parent, Color.white); image.raycastTarget = true; image.rectTransform.sizeDelta = new Vector2(0, 44);
            var layout = image.gameObject.AddComponent<LayoutElement>(); layout.preferredHeight = 44; layout.minHeight = 36;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = new Color(1, 1, 1, 0); colors.highlightedColor = new Color(.80f, .91f, 1, 1); colors.pressedColor = new Color(.60f, .78f, .94f, 1); colors.selectedColor = colors.highlightedColor; colors.fadeDuration = .06f; button.colors = colors;
            if (sprite != null) { var icon = Image("Icon", image.transform, Color.white); icon.sprite = sprite; icon.preserveAspect = true; Box(icon.rectTransform, 9, 5, 34, 34); }
            var label = Text("Label", image.transform, title, 18, Ink); Stretch(label.rectTransform, new Vector2(53, 0), new Vector2(-8, 0));
            return button;
        }
        private static Button RightLink(string title, Transform parent, float top)
        {
            var button = GlassButton(title, parent); Rect((RectTransform)button.transform, new Vector2(0, 1), Vector2.one, new Vector2(5, -top - 42), new Vector2(-5, -top));
            var graphic = button.GetComponent<AeroShellGraphic>(); graphic.top = new Color(.2f, .4f, .6f, 0); graphic.bottom = new Color(.06f, .17f, .28f, 0);
            var text = Text("Label", button.transform, title, 18, Color.white); Stretch(text.rectTransform, new Vector2(14, 0), new Vector2(-8, 0));
            return button;
        }
        private static void RightApp(string label, string app, Transform parent, float top, AeroStartMenu control)
        {
            var button = RightLink(label, parent, top); var window = FindWindow(app); button.interactable = window != null;
            if (window != null) UnityEventTools.AddPersistentListener(button.onClick, window.OpenWindow);
            UnityEventTools.AddPersistentListener(button.onClick, control.Close);
        }
        private static Button TrayButton(string title, Transform parent, float x, AeroShellGraphic.Shape iconShape)
        {
            var button = GlassButton(title, parent); Box((RectTransform)button.transform, x, 5, 34, 43);
            var face = button.GetComponent<AeroShellGraphic>(); face.top = face.bottom = new Color(1, 1, 1, 0);
            var icon = Surface("Tray glyph", button.transform, iconShape); Box(icon.rectTransform, 6, 11, 22, 22);
            button.GetComponent<AeroShellHover>().tooltip = Tooltip(button.transform, title);
            return button;
        }
        private static Button GlassButton(string title, Transform parent)
        {
            var graphic = Surface(title, parent, AeroShellGraphic.Shape.Glass); graphic.raycastTarget = true;
            var button = graphic.gameObject.AddComponent<Button>(); button.targetGraphic = graphic; button.transition = Selectable.Transition.None; AddHover(button.gameObject, graphic); return button;
        }
        private static GameObject Tooltip(Transform parent, string label)
        {
            var image = Image("Aero Tooltip", parent, new Color(.98f, .99f, 1, .98f));
            var r = image.rectTransform; r.anchorMin = r.anchorMax = new Vector2(.5f, 1); r.pivot = new Vector2(.5f, 0); r.anchoredPosition = new Vector2(0, 8); r.sizeDelta = new Vector2(Mathf.Max(112, label.Length * 8 + 20), 30);
            var text = Text("Text", image.transform, label, 15, Ink); Stretch(text.rectTransform, new Vector2(7, 0), new Vector2(-7, 0)); text.alignment = TextAlignmentOptions.Center;
            image.gameObject.SetActive(false); return image.gameObject;
        }
        private static AeroShellHover AddHover(GameObject go, AeroShellGraphic graphic) { var hover = go.GetComponent<AeroShellHover>() ?? go.AddComponent<AeroShellHover>(); hover.graphic = graphic; return hover; }
        private static string Keywords(string name)
        {
            switch (name) { case "Settings": return "control panel system preferences computer"; case "Notepad": return "documents text notes"; case "Photo Gallery": return "pictures images photos"; case "Web Browser": return "internet web explorer"; case "Music Player": return "music audio media"; case "Commander": return "terminal command prompt cmd"; default: return name; }
        }
        private static WindowManager FindWindow(string name) => name != null && windows.TryGetValue(name, out var window) ? window : null;
        private static Sprite Icon(string name) => name != null && iconMap.TryGetValue(name, out var icon) ? icon : null;
        private static Transform Required(Transform root, string path) => root.Find(path) ?? throw new InvalidOperationException("Missing desktop shell object: " + path);
        private static RectTransform NewRect(string name, Transform parent) { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return (RectTransform)go.transform; }
        private static Image Image(string name, Transform parent, Color color) { var rect = NewRect(name, parent); var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image; }
        private static AeroShellGraphic Surface(string name, Transform parent, AeroShellGraphic.Shape shape) { var rect = NewRect(name, parent); var graphic = rect.gameObject.AddComponent<AeroShellGraphic>(); graphic.shape = shape; graphic.raycastTarget = false; return graphic; }
        private static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color)
        {
            var rect = NewRect(name, parent); var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.text = value; text.fontSize = size; text.color = color; text.raycastTarget = false; text.alignment = TextAlignmentOptions.MidlineLeft; text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis; return text;
        }
        private static void FixedImage(Image image, Color color) { var binding = image.GetComponent<UIManagerElement>(); if (binding != null) Object.DestroyImmediate(binding); image.color = color; }
        private static void Stretch(RectTransform r) => Stretch(r, Vector2.zero, Vector2.zero);
        private static void Stretch(RectTransform r, Vector2 min, Vector2 max) => Rect(r, Vector2.zero, Vector2.one, min, max);
        private static void Rect(RectTransform r, Vector2 minAnchor, Vector2 maxAnchor, Vector2 minOffset, Vector2 maxOffset) { r.anchorMin = minAnchor; r.anchorMax = maxAnchor; r.offsetMin = minOffset; r.offsetMax = maxOffset; }
        private static void Box(RectTransform r, float x, float y, float width, float height) { r.anchorMin = r.anchorMax = r.pivot = Vector2.zero; r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(width, height); }
    }
}
