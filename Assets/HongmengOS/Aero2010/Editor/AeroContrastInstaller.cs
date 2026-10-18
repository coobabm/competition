using System;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Desktop.Apps;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HongmengOS.Aero2010.Editor
{
    /// <summary>Scene-scoped light-content theme; original DreamOS resources are never edited.</summary>
    public static class AeroContrastInstaller
    {
        private const string ThemeRoot = "Assets/HongmengOS/Aero2010/Theme";
        private const string PrefabRoot = ThemeRoot + "/Prefabs";
        private static readonly Color Ink = new Color32(25, 38, 51, 255);
        private static readonly Color Secondary = new Color32(82, 99, 116, 255);
        private static readonly Color Paper = new Color32(245, 249, 252, 255);
        private static readonly Color ConsolePaper = new Color32(12, 19, 28, 255);
        private static readonly Color ConsoleInk = new Color32(225, 235, 245, 255);

        // Only runtime UI factories are followed. In particular, games[].gamePrefab is not touched.
        private static readonly Dictionary<string, string[]> FactoryFields = new Dictionary<string, string[]>
        {
            { nameof(WindowManager), new[] { "resizePreset" } },
            { nameof(MessagingApp), new[] { "chatLayout", "chatItem", "textMessageSent", "textMessageRecieved", "imageMessageSent", "imageMessageRecieved", "audioMessageSent", "audioMessageRecieved", "chatMessageTimer", "messageDate", "beginningIndicator", "storyTellerObject" } },
            { nameof(LingGuangV05.Desktop.Apps.MusicApp), new[] { "playlistTrackPreset", "playlistPanelPreset", "playlistItemPreset" } },
            { nameof(LingGuangV05.Desktop.Apps.NoteApp), new[] { "noteLibraryPreset" } },
            { nameof(WidgetManager), new[] { "libraryItem" } },
            { nameof(LingGuangV05.Desktop.Apps.GameHubApp), new[] { "sliderIndicator", "libraryPreset" } },
            { nameof(WebBrowserApp), new[] { "tabPreset", "favoritePreset", "downloadPreset" } },
            { nameof(LingGuangV05.Desktop.Apps.PhotoApp), new[] { "pictureLibraryPreset" } },
            { nameof(LingGuangV05.Desktop.Apps.VideoApp), new[] { "videoPreset" } },
            { nameof(LingGuangV05.Desktop.Apps.ReminderApp), new[] { "reminderPreset" } },
            { nameof(WallpaperManager), new[] { "wallpaperItem" } },
            { nameof(UserManager), new[] { "ppItem" } },
            { nameof(NetworkManager), new[] { "networkPreset" } },
            { nameof(ContextMenuManager), new[] { "menuPreset", "buttonPreset", "separatorPreset" } },
            { nameof(NotificationManager), new[] { "notificationButton", "popupNotification", "standardNotification" } }
        };

        public static int Install()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != AeroDesktopStyleEditor.ScenePath)
                throw new InvalidOperationException("Aero contrast installation requires 2010desktop in edit mode.");
            GameObject source = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "hongmengos 2010 Desktop Source") { source = root; break; }
            if (source == null) throw new InvalidOperationException("The 2010 LCD desktop source is missing.");
            var theme = AssetDatabase.LoadAssetAtPath<UIManager>(ThemeRoot + "/Aero 2010 UI.asset");
            var apps = AssetDatabase.LoadAssetAtPath<AppLibrary>(ThemeRoot + "/Aero 2010 Apps.asset");
            if (theme == null || apps == null) throw new InvalidOperationException("Install the scene-local Aero theme first.");
            if (!AssetDatabase.IsValidFolder(PrefabRoot)) AssetDatabase.CreateFolder(ThemeRoot, "Prefabs");
            theme.primaryColorDark = Ink; theme.secondaryColorDark = Secondary;
            theme.windowBGColorDark = Paper; theme.backgroundColorDark = new Color32(227, 239, 247, 255);
            var context = new Context(theme, apps);
            context.Rebind(source);
            Transform windows = source.transform.Find("Desktop/Apps & Windows");
            if (windows == null) throw new InvalidOperationException("Aero app window hierarchy is missing.");
            foreach (WindowManager window in windows.GetComponentsInChildren<WindowManager>(true))
            {
                Transform content = window.transform.Find("Container/Content");
                if (content == null) continue;
                if (window.name == "Commander") StyleConsole(content);
                else context.Style(content.gameObject);
                if (window.name == "Web Browser")
                {
                    FixImageColor(content.Find("Main Content/Top Panel/Background"), new Color32(218, 232, 243, 255));
                    FixImageColor(content.Find("Main Content/Top Panel/URL Bar/Background"), Color.white);
                    FixImageColor(content.Find("Main Content/Top Panel/URL Bar/Background/Filled"), new Color32(195, 221, 241, 255));
                }
                if (window.name == "Reminder")
                {
                    Transform create = content.Find("Main Content/Create Button");
                    if (create != null) foreach (Image image in create.GetComponentsInChildren<Image>(true))
                        if (image.name == "Background") FixImageColor(image.transform, new Color32(199, 223, 242, 255));
                }
            }
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssetIfDirty(theme);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the Aero contrast changes.");
            Debug.Log("[Aero contrast] Saved " + scene.path + "; localized UI assets: " + context.AssetCount + "; templates: " + PrefabRoot);
            return context.AssetCount;
        }

        private sealed class Context
        {
            private readonly UIManager theme;
            private readonly AppLibrary apps;
            private readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            private readonly Dictionary<string, WebBrowserLibrary> libraries = new Dictionary<string, WebBrowserLibrary>(StringComparer.Ordinal);
            public int AssetCount => prefabs.Count + libraries.Count;
            public Context(UIManager theme, AppLibrary apps) { this.theme = theme; this.apps = apps; }

            public void Rebind(GameObject root)
            {
                foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null) continue;
                    using (var serialized = new SerializedObject(component))
                    {
                        SerializedProperty property = serialized.GetIterator();
                        while (property.Next(true))
                        {
                            if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                            if (property.objectReferenceValue is UIManager) property.objectReferenceValue = theme;
                            else if (property.objectReferenceValue is AppLibrary) property.objectReferenceValue = apps;
                            else if (property.objectReferenceValue is WebBrowserLibrary web) property.objectReferenceValue = LocalLibrary(web);
                        }
                        if (FactoryFields.TryGetValue(component.GetType().Name, out string[] fields))
                            foreach (string field in fields)
                            {
                                SerializedProperty reference = serialized.FindProperty(field);
                                if (reference != null && reference.objectReferenceValue is GameObject prefab)
                                    reference.objectReferenceValue = LocalPrefab(prefab);
                            }
                        if (component is WidgetManager)
                        {
                            SerializedProperty items = serialized.FindProperty("widgetItems");
                            for (int i = 0; items != null && i < items.arraySize; i++)
                            {
                                var reference = items.GetArrayElementAtIndex(i).FindPropertyRelative("widgetPrefab");
                                if (reference.objectReferenceValue is GameObject prefab) reference.objectReferenceValue = LocalPrefab(prefab);
                            }
                        }
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                foreach (UIManagerColorChanger changer in root.GetComponentsInChildren<UIManagerColorChanger>(true))
                {
                    changer.targetUIManager = theme;
                    changer.primary = Ink; changer.secondary = Secondary; changer.windowBackground = Paper;
                    changer.background = new Color32(227, 239, 247, 255); changer.taskbar = theme.taskBarColorDark;
                    changer.accentMatch = Luminance(changer.accent) > .5f ? Ink : Color.white;
                    EditorUtility.SetDirty(changer);
                }
            }

            private GameObject LocalPrefab(GameObject original)
            {
                string source = AssetDatabase.GetAssetPath(original);
                if (string.IsNullOrEmpty(source)) return original; // Scene-local template, styled with its window.
                if (!source.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) return original;
                if (prefabs.TryGetValue(source, out GameObject cached)) return cached;
                string path = LocalPath(source, ".prefab");
                if (source != path && AssetDatabase.LoadAssetAtPath<GameObject>(path) == null && !AssetDatabase.CopyAsset(source, path))
                    throw new IOException("Could not copy UI template: " + source);
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) throw new IOException("Could not load local UI template: " + path);
                prefabs[source] = asset; // Break potential cross-template reference cycles before recursion.
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Rebind(contents);
                    Style(contents);
                    PrefabUtility.SaveAsPrefabAsset(contents, path, out bool saved);
                    if (!saved) throw new IOException("Could not save local UI template: " + path);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            private WebBrowserLibrary LocalLibrary(WebBrowserLibrary original)
            {
                string source = AssetDatabase.GetAssetPath(original);
                if (string.IsNullOrEmpty(source)) throw new InvalidOperationException("The browser library must be a saved asset.");
                if (libraries.TryGetValue(source, out WebBrowserLibrary cached)) return cached;
                string path = LocalPath(source, ".asset");
                if (source != path && AssetDatabase.LoadAssetAtPath<WebBrowserLibrary>(path) == null && !AssetDatabase.CopyAsset(source, path))
                    throw new IOException("Could not copy the browser library: " + source);
                var copy = AssetDatabase.LoadAssetAtPath<WebBrowserLibrary>(path);
                if (copy == null) throw new IOException("Could not load the local browser library: " + path);
                libraries[source] = copy;
                // A physical asset copy gives independent serialized WebPage/download lists.
                using (var serialized = new SerializedObject(copy))
                {
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.name == "pageContent" && property.objectReferenceValue is GameObject page)
                            property.objectReferenceValue = LocalPrefab(page);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorUtility.SetDirty(copy); AssetDatabase.SaveAssetIfDirty(copy);
                return copy;
            }

            public void Style(GameObject root)
            {
                var white = new HashSet<TMP_Text>();
                foreach (LingGuangV05.Desktop.Apps.VideoListEntry item in root.GetComponentsInChildren<LingGuangV05.Desktop.Apps.VideoListEntry>(true))
                { white.Add(item.durationText); white.Add(item.titleText); white.Add(item.descriptionText); }
                foreach (LingGuangV05.Desktop.Apps.PhotoListEntry item in root.GetComponentsInChildren<LingGuangV05.Desktop.Apps.PhotoListEntry>(true))
                {
                    white.Add(item.titleText); white.Add(item.descriptionText);
                    if (item.titleText != null) FixImageColor(item.titleText.transform.parent, new Color32(30, 40, 50, 230));
                }
                foreach (WebBrowserTabItem tab in root.GetComponentsInChildren<WebBrowserTabItem>(true))
                    FixImageColor(tab.transform.Find("Indicator"), Paper);
                Transform gameInfo = root.transform.Find("Main Content/Home/Content/Slider/Game Info");
                if (gameInfo != null)
                {
                    foreach (TMP_Text text in gameInfo.GetComponentsInChildren<TMP_Text>(true)) white.Add(text);
                    foreach (Image image in gameInfo.GetComponentsInChildren<Image>(true))
                        if (image.name == "Icon") FixImageColor(image.transform, Color.white);
                }
                // The full viewer has a light title strip; only thumbnail overlays stay white.
                Transform playing = root.transform.Find("Main Content/Now Playing");
                if (playing != null)
                    foreach (TMP_Text text in playing.GetComponentsInChildren<TMP_Text>(true))
                        if (text.name == "Music Title" || text.name == "Playlist Title" || text.name == "Panel Title" || text.name == "Artist Name") white.Add(text);
                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                var originalAlpha = new Dictionary<TMP_Text, float>();
                foreach (TMP_Text text in texts) originalAlpha[text] = text.color.a;
                foreach (UIManagerElement element in root.GetComponentsInChildren<UIManagerElement>(true))
                { element.themeManagerAsset = theme; element.UpdateElement(); }
                foreach (TMP_Text text in texts)
                {
                    UIManagerElement element = text.GetComponent<UIManagerElement>();
                    // Translucent accent bubbles are light surfaces: they need dark text too.
                    // Only explicit opaque/dark artwork overlays retain white foregrounds.
                    bool reversed = white.Contains(text);
                    bool subdued = originalAlpha[text] < .9f || (element != null && element.colorType == UIManagerElement.ColorType.Secondary)
                        || text.name.IndexOf("Placeholder", StringComparison.OrdinalIgnoreCase) >= 0;
                    float alpha = originalAlpha[text] <= .01f ? 0f : IsDisabledState(text.transform, root.transform) ? .55f : 1f;
                    Color color = reversed ? Color.white : subdued ? Secondary : Ink;
                    color.a = alpha; text.color = color;
                    if (element != null)
                    {
                        element.useCustomColor = reversed;
                        element.keepAlphaValue = alpha != 1f;
                        if (!reversed) element.colorType = subdued ? UIManagerElement.ColorType.Secondary : UIManagerElement.ColorType.Primary;
                        EditorUtility.SetDirty(element);
                    }
                    EditorUtility.SetDirty(text);
                }
                foreach (TMP_InputField field in root.GetComponentsInChildren<TMP_InputField>(true))
                {
                    field.customCaretColor = true;
                    field.caretColor = Ink;
                    field.selectionColor = new Color(.35f, .65f, .9f, .35f);
                    EditorUtility.SetDirty(field);
                }
            }

            private static void PreserveAvatar(TMP_Text letter, HashSet<TMP_Text> white)
            {
                if (letter == null) return;
                white.Add(letter);
                Image picture = letter.transform.parent != null ? letter.transform.parent.GetComponent<Image>() : null;
                if (picture == null) return;
                UIManagerElement element = picture.GetComponent<UIManagerElement>();
                if (element != null) Object.DestroyImmediate(element);
                picture.color = Ink;
                EditorUtility.SetDirty(picture);
            }
        }

        private static void StyleConsole(Transform content)
        {
            Image background = content.GetComponent<Image>() ?? content.gameObject.AddComponent<Image>();
            background.color = ConsolePaper; background.raycastTarget = false;
            foreach (LingGuangV05.Desktop.Apps.CommanderApp commander in content.GetComponentsInChildren<LingGuangV05.Desktop.Apps.CommanderApp>(true))
            { commander.textColor = ConsoleInk; commander.timeColor = new Color32(111, 202, 175, 255); EditorUtility.SetDirty(commander); }
            // The manager usually sits on the window rather than inside Content.
            LingGuangV05.Desktop.Apps.CommanderApp owner = content.GetComponentInParent<LingGuangV05.Desktop.Apps.CommanderApp>();
            if (owner != null) { owner.textColor = ConsoleInk; owner.timeColor = new Color32(111, 202, 175, 255); EditorUtility.SetDirty(owner); }
            foreach (TMP_Text text in content.GetComponentsInChildren<TMP_Text>(true))
            {
                UIManagerElement element = text.GetComponent<UIManagerElement>();
                if (element != null) { element.useCustomColor = true; EditorUtility.SetDirty(element); }
                text.color = ConsoleInk; EditorUtility.SetDirty(text);
            }
            foreach (TMP_InputField field in content.GetComponentsInChildren<TMP_InputField>(true))
            { field.customCaretColor = true; field.caretColor = ConsoleInk; field.selectionColor = new Color(.25f, .45f, .65f, .6f); EditorUtility.SetDirty(field); }
            Transform buttons = content.Find("Top Nav Buttons");
            if (buttons != null) foreach (Image image in buttons.GetComponentsInChildren<Image>(true))
                if (image.name == "Icon" || image.name == "Normalize") FixImageColor(image.transform, ConsoleInk);
        }

        private static void FixImageColor(Transform target, Color color)
        {
            if (target == null) return;
            Image image = target.GetComponent<Image>();
            if (image == null) return;
            UIManagerElement element = image.GetComponent<UIManagerElement>();
            if (element != null) Object.DestroyImmediate(element);
            image.color = color;
            EditorUtility.SetDirty(image);
        }

        private static string LocalPath(string source, string extension)
        {
            if (source.StartsWith(PrefabRoot + "/", StringComparison.Ordinal)) return source;
            return PrefabRoot + "/" + Path.GetFileNameWithoutExtension(source) + "_" + AssetDatabase.AssetPathToGUID(source) + extension;
        }

        private static bool IsDisabledState(Transform item, Transform root)
        {
            for (Transform current = item; current != null; current = current.parent)
            { if (current.name == "Disabled") return true; if (current == root) break; }
            return false;
        }

        private static float Luminance(Color color) => .2126f * color.r + .7152f * color.g + .0722f * color.b;
    }
}
