#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LingGuangV05.Desktop;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LingGuangV05.Editor
{
    public static class DesktopLocalizationInstaller
    {
        public const string Root = "Assets/LingGuangV05/Localization";
        private const string Vendor = "Assets/DreamOS - Complete OS UI/Localization/";
        private static readonly Dictionary<Object, Object> copies = new Dictionary<Object, Object>();
        private static readonly Dictionary<string, string> prefabs = new Dictionary<string, string>();
        private static readonly HashSet<TMP_Text> dynamicTexts = new HashSet<TMP_Text>();
        private static readonly HashSet<ButtonManager> dynamicButtons = new HashSet<ButtonManager>();
        private static LocalizationSettings settings;
        private static LocalizationLanguage chinese, english;
        private static TMP_FontAsset cjk;
        private static int bound;

        [MenuItem("LingGuang/Localization/Install Chinese and English")]
        public static void Install()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before installing localization.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ChapterOneInstaller.ScenePath) throw new InvalidOperationException("Open the copied Main scene first.");
            copies.Clear(); prefabs.Clear(); dynamicTexts.Clear(); dynamicButtons.Clear(); bound = 0;
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Fonts");
            AssetDatabase.Refresh();
            BuildTables(); BuildFont();
            var roots = scene.GetRootGameObjects();
            foreach (var view in roots.SelectMany(r => r.GetComponentsInChildren<ChapterOneNativeAppView>(true)))
            {
                foreach (var t in view.Bindings.texts) if (t?.control != null) dynamicTexts.Add(t.control);
                foreach (var b in view.Bindings.buttons) if (b?.control != null) dynamicButtons.Add(b.control);
            }
            // Only templates actually referenced by Main are copied; originals are never edited.
            foreach (var root in roots) GatherPayloadFields(root);
            foreach (var root in roots) GatherPrefabs(root);
            foreach (var pair in prefabs.ToArray())
            {
                var contents = PrefabUtility.LoadPrefabContents(pair.Value);
                try { Rewire(contents); foreach (var stale in contents.GetComponentsInChildren<DesktopLocalizedText>(true)) Object.DestroyImmediate(stale); BindStaticTemplateHelp(contents); PrefabUtility.SaveAsPrefabAsset(contents, pair.Value); }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            foreach (var root in roots) { Rewire(root); BindLabels(root, true); }
            var manager = roots.SelectMany(r => r.GetComponentsInChildren<LocalizationManager>(true)).Single();
            manager.setLanguageOnAwake = false; manager.saveLanguageChanges = false; manager.updateItemsOnSet = true;
            manager.currentLanguageAsset = chinese; manager.currentLanguage = "Chinese (zh-CN)";
            manager.UIManagerAsset.currentLanguage = chinese;
            manager.localizedItems.Clear();
            var controller = manager.GetComponent<DesktopLanguageController>() ?? manager.gameObject.AddComponent<DesktopLanguageController>();
            controller.manager = manager; controller.desktopRoot = manager.transform.root;
            var signal = manager.GetComponent<LocalizedObject>() ?? manager.gameObject.AddComponent<LocalizedObject>();
            signal.localizationSettings = settings; signal.tableIndex = 0; signal.localizationKey = "SettingsLabelLanguage";
            signal.objectType = LocalizedObject.ObjectType.Custom; signal.updateMode = LocalizedObject.UpdateMode.OnEnable;
            controller.changeSignal = signal;
            var nativeContent = manager.GetComponent<DesktopNativeContentLocalization>() ?? manager.gameObject.AddComponent<DesktopNativeContentLocalization>();
            nativeContent.languageManager = manager;
            nativeContent.contextMenus = roots.SelectMany(r => r.GetComponentsInChildren<ContextMenuManager>(true)).ToArray();
            nativeContent.selectors = roots.SelectMany(r => r.GetComponentsInChildren<HorizontalSelector>(true)).ToArray();
            nativeContent.gameHubs = roots.SelectMany(r => r.GetComponentsInChildren<GameHubManager>(true)).ToArray();
            foreach (var selector in manager.languageSelectors)
            {
                selector.saveSelected = false; selector.useLocalization = false; selector.invokeOnAwake = false;
                selector.items.Clear(); selector.CreateNewItem("简体中文"); selector.CreateNewItem("English");
                selector.index = selector.defaultIndex = 0; selector.UpdateUI();
            }
            RemoveTaskbarShortcut(manager);
            foreach (var bridge in roots.SelectMany(r => r.GetComponentsInChildren<ChapterOneDesktopBridge>(true)))
                if (bridge.DesktopShortcut != null)
                    foreach (var label in bridge.DesktopShortcut.GetComponentsInChildren<TMP_Text>(true))
                    { label.enableAutoSizing = true; label.fontSizeMax = label.fontSize; label.fontSizeMin = 12; }
            foreach (var start in roots.SelectMany(r => r.GetComponentsInChildren<HongmengOS.Aero2010.AeroStartMenu>(true)))
                for (int i = 0; i < start.programs.Length; i++)
                { var p = start.programs[i]; p.searchKeywords += " " + GameText.Source(p.title); start.programs[i] = p; }
            // Save only our own assets. Do not flush unrelated/shared dirty assets.
            foreach (string guid in AssetDatabase.FindAssets("", new[] { Root }))
            { var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)); if (asset != null) AssetDatabase.SaveAssetIfDirty(asset); }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Localization] Installed: 2 languages, 232 native entries, " + bound + " explicit labels, " + prefabs.Count + " owned native templates.");
        }

        private static void BuildTables()
        {
            settings = CopyAsset<LocalizationSettings>(Vendor + "Localization Settings.asset", Root + "/Settings.asset");
            english = CopyAsset<LocalizationLanguage>(Vendor + "Languages/English (en-US).asset", Root + "/English.asset");
            chinese = CopyAsset<LocalizationLanguage>(Vendor + "Languages/English (en-US).asset", Root + "/Chinese.asset");
            var translations = File.ReadAllLines("Assets/LingGuangV05/Resources/Localization/NativeChinese.tsv")
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")).Select(x => x.Split(new[] {'\t'}, 3))
                .ToDictionary(x => x[0] + "/" + x[1], x => x[2].Replace("\\n", "\n").Replace("\\t", "\t"));
            for (int i = 0; i < english.tableList.Count; i++)
            {
                var original = english.tableList[i].table;
                var table = CopyAsset<LocalizationTable>(AssetDatabase.GetAssetPath(original), Root + "/Table " + i + ".asset");
                table.localizationSettings = settings;
                settings.tables[i].localizationTable = table;
                english.tableList[i].table = chinese.tableList[i].table = table;
                foreach (var entry in chinese.tableList[i].tableContent)
                {
                    if (!translations.TryGetValue(i + "/" + entry.key, out string value)) throw new InvalidOperationException("Missing Chinese: " + entry.key);
                    entry.value = value;
                }
                EditorUtility.SetDirty(table);
            }
            english.localizationSettings = chinese.localizationSettings = settings;
            english.languageID = "en-US"; english.languageName = "English"; english.localizedName = "English";
            chinese.languageID = "zh-CN"; chinese.languageName = "Chinese"; chinese.localizedName = "简体中文";
            settings.languages = new List<LocalizationSettings.Language> {
                new LocalizationSettings.Language {languageID="zh-CN",languageName="Chinese",localizedName="简体中文",localizationLanguage=chinese},
                new LocalizationSettings.Language {languageID="en-US",languageName="English",localizedName="English",localizationLanguage=english} };
            settings.defaultLanguageID = "zh-CN"; settings.defaultLanguageIndex = 0;
            EditorUtility.SetDirty(english); EditorUtility.SetDirty(chinese); EditorUtility.SetDirty(settings);
        }

        private static T CopyAsset<T>(string source, string target) where T : Object
        {
            if (!File.Exists(target) && !AssetDatabase.CopyAsset(source, target)) throw new IOException("Could not copy " + source);
            return AssetDatabase.LoadAssetAtPath<T>(target);
        }
        private static void BuildFont()
        {
            string path = Root + "/Fonts/Desktop CJK.asset";
            cjk = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (cjk != null) return;
            Font font = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if (font == null) throw new InvalidOperationException("Missing Noto CJK source font.");
            cjk = TMP_FontAsset.CreateFontAsset(font, 48, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            cjk.name = "Desktop CJK"; cjk.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(cjk, path);
            foreach (var texture in cjk.atlasTextures) AssetDatabase.AddObjectToAsset(texture, cjk);
            AssetDatabase.AddObjectToAsset(cjk.material, cjk);
            cjk.material.mainTexture = cjk.atlasTexture;
            string chars = string.Concat(chinese.tableList.SelectMany(t => t.tableContent).Select(t => t.value))
                + string.Concat(Resources.LoadAll<TextAsset>("Localization").Select(a => a.text));
            cjk.TryAddCharacters(new string(chars.Distinct().ToArray()));
            foreach (var texture in cjk.atlasTextures) if (!AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, cjk);
            EditorUtility.SetDirty(cjk); EditorUtility.SetDirty(cjk.material);
        }
        private static TMP_FontAsset FontCopy(TMP_FontAsset source)
        {
            if (source == null || AssetDatabase.GetAssetPath(source).StartsWith(Root)) return source ?? cjk;
            if (copies.TryGetValue(source, out var cached)) return (TMP_FontAsset)cached;
            string path = Root + "/Fonts/" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)) + ".asset";
            var result = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (result == null)
            {
                result = Object.Instantiate(source); result.name = source.name + " Localized";
                result.atlasPopulationMode = AtlasPopulationMode.Static;
                result.atlasTextures = source.atlasTextures.Select(Object.Instantiate).ToArray();
                result.material = Object.Instantiate(source.material);
                result.material.mainTexture = result.atlasTextures[0];
                result.fallbackFontAssetTable = new List<TMP_FontAsset> { cjk };
                AssetDatabase.CreateAsset(result, path);
                foreach (var texture in result.atlasTextures) AssetDatabase.AddObjectToAsset(texture, result);
                AssetDatabase.AddObjectToAsset(result.material, result);
            }
            copies[source] = result; return result;
        }
        private static UIManager ThemeCopy(UIManager source)
        {
            if (source == null) return null;
            if (copies.TryGetValue(source, out var cached)) return (UIManager)cached;
            string sourcePath = AssetDatabase.GetAssetPath(source);
            UIManager theme = sourcePath.StartsWith(Root) ? source : CopyAsset<UIManager>(sourcePath,
                Root + "/Theme " + AssetDatabase.AssetPathToGUID(sourcePath) + ".asset");
            copies[source] = theme;
            theme.enableLocalization = true; theme.localizationSettings = settings; theme.currentLanguage = chinese;
            theme.systemFontThin = FontCopy(theme.systemFontThin); theme.systemFontLight = FontCopy(theme.systemFontLight);
            theme.systemFontRegular = FontCopy(theme.systemFontRegular); theme.systemFontSemiBold = FontCopy(theme.systemFontSemiBold);
            theme.systemFontBold = FontCopy(theme.systemFontBold);
            EditorUtility.SetDirty(theme); return theme;
        }
        private static void GatherPrefabs(GameObject root)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                var so = new SerializedObject(component); var p = so.GetIterator();
                while (p.NextVisible(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue == null) continue;
                    string path = AssetDatabase.GetAssetPath(p.objectReferenceValue);
                    if (!path.EndsWith(".prefab") || prefabs.ContainsKey(path)) continue;
                    if (path.StartsWith(Root)) { prefabs[path] = path; GatherPrefabs(AssetDatabase.LoadAssetAtPath<GameObject>(path)); continue; }
                    string target = Root + "/Prefabs/" + Path.GetFileNameWithoutExtension(path) + "_" + AssetDatabase.AssetPathToGUID(path) + ".prefab";
                    prefabs[path] = target;
                    if (!File.Exists(target)) AssetDatabase.CopyAsset(path, target);
                    GatherPrefabs(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                }
            }
        }
        private static Object RemapPrefab(Object source)
        {
            string path = AssetDatabase.GetAssetPath(source);
            if (!prefabs.TryGetValue(path, out string target)) return source;
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var copy = AssetDatabase.LoadAssetAtPath<GameObject>(target);
            Transform t = source is GameObject go ? go.transform : (source as Component)?.transform;
            if (t == null) return source;
            string relative = AnimationUtility.CalculateTransformPath(t, original.transform);
            Transform match = string.IsNullOrEmpty(relative) ? copy.transform : copy.transform.Find(relative);
            if (match == null) return source;
            if (source is GameObject) return match.gameObject;
            var components = t.GetComponents(source.GetType());
            int index = Array.IndexOf(components, source);
            var matches = match.GetComponents(source.GetType());
            return index >= 0 && index < matches.Length ? matches[index] : source;
        }
        private static void Rewire(GameObject root)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                var so = new SerializedObject(component); var p = so.GetIterator(); bool changed = false;
                while (p.NextVisible(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue == null) continue;
                    Object value = p.objectReferenceValue, replacement = value;
                    if (value is UIManager ui) replacement = ThemeCopy(ui);
                    else if (value is TMP_FontAsset font) replacement = FontCopy(font);
                    else if (value is LocalizationSettings) replacement = settings;
                    else replacement = RemapPrefab(value);
                    if (replacement != value) { p.objectReferenceValue = replacement; changed = true; }
                }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                label.font = FontCopy(label.font); label.fontSharedMaterial = label.font.material;
                var themeElement = label.GetComponent<UIManagerElement>();
                if (themeElement != null) themeElement.useCustomFont = true;
            }
            foreach (var button in root.GetComponentsInChildren<ButtonManager>(true))
            {
                foreach (var label in new[] { button.normalTextObj, button.highlightTextObj, button.pressedTextObj, button.disabledTextObj })
                {
                    if (label == null) continue;
                    label.enableAutoSizing = true; label.fontSizeMax = button.textSize;
                    label.fontSizeMin = Mathf.Min(14, button.textSize);
                }
            }
            foreach (var localized in root.GetComponentsInChildren<LocalizedObject>(true))
            {
                if (localized.localizationKey == "SetupFınalStep") localized.localizationKey = "SetupFinalStep";
                localized.localizationSettings = settings;
                if (localized.tableIndex < 0 || localized.tableIndex >= english.tableList.Count) localized.tableIndex = 0;
            }
            foreach (var monitor in root.GetComponentsInChildren<HongmengOS.Aero2010.AeroLcdController>(true))
            {
                if (monitor.osd == null) continue;
                var adapter = monitor.osd.GetComponent<DesktopLocalizedMonitor>() ?? monitor.osd.AddComponent<DesktopLocalizedMonitor>();
                adapter.monitor = monitor;
            }
            foreach (var reminder in root.GetComponentsInChildren<ReminderItem>(true))
            { var adapter = reminder.GetComponent<DesktopLocalizedReminder>() ?? reminder.gameObject.AddComponent<DesktopLocalizedReminder>(); adapter.item = reminder; }
            foreach (var shellClock in root.GetComponentsInChildren<HongmengOS.Aero2010.AeroShellClock>(true))
            {
                shellClock.enabled = false;
                if (shellClock.timeLabel != null)
                {
                    var clock = shellClock.timeLabel.GetComponent<DesktopLocalizedClock>() ?? shellClock.timeLabel.gameObject.AddComponent<DesktopLocalizedClock>();
                    clock.target = shellClock.timeLabel; clock.amPm = true;
                }
                if (shellClock.dateLabel != null)
                {
                    var clock = shellClock.dateLabel.GetComponent<DesktopLocalizedClock>() ?? shellClock.dateLabel.gameObject.AddComponent<DesktopLocalizedClock>();
                    clock.target = shellClock.dateLabel; clock.date = true;
                    shellClock.dateLabel.enableAutoSizing = true; shellClock.dateLabel.fontSizeMin = 10; shellClock.dateLabel.fontSizeMax = shellClock.dateLabel.fontSize;
                }
            }
            foreach (var date in root.GetComponentsInChildren<DateAndTime>(true))
            {
                if (date.objectType == DateAndTime.ObjectType.AnalogClock) continue;
                var so = new SerializedObject(date);
                var clock = date.GetComponent<DesktopLocalizedClock>() ?? date.gameObject.AddComponent<DesktopLocalizedClock>();
                clock.target = date.textObj != null ? date.textObj : date.GetComponent<TMP_Text>();
                clock.date = date.objectType == DateAndTime.ObjectType.DigitalDate;
                clock.seconds = so.FindProperty("addSeconds").boolValue; clock.amPm = so.FindProperty("enableAmPmLabel").boolValue;
                date.enabled = false;
            }
        }
        private static void GatherPayloadFields(GameObject root)
        {
            var owners = new HashSet<string> { "HorizontalSelector", "PanelButton", "MusicPlayerManager", "GameHubManager", "AeroLcdController", "NotificationItem", "WidgetLibraryItem", "PlaylistPanel", "PlaylistItem", "PlaylistTrack", "PhotoGalleryManager", "VideoPlayerManager", "CommanderManager", "GetUserInfo", "ResetPassword", "NetworkPreset", "WebBrowserTabItem", "WebBrowserFavoritesItem", "ReminderItem", "NotepadPreset" };
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null || !owners.Contains(component.GetType().Name)) continue;
                var self = component.GetComponent<TMP_Text>(); if (self != null) dynamicTexts.Add(self);
                var serialized = new SerializedObject(component); var p = serialized.GetIterator();
                while (p.NextVisible(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (p.objectReferenceValue is TMP_Text label) dynamicTexts.Add(label);
                    if (p.objectReferenceValue is ButtonManager button) dynamicButtons.Add(button);
                }
            }
        }
        private static void BindStaticTemplateHelp(GameObject root)
        {
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                bool instructions = root.name.StartsWith("3D Test", StringComparison.Ordinal) && label.name == "Keys";
                bool blueScreen = root.name.StartsWith("Blue Screen of Death", StringComparison.Ordinal) && (label.name == "Title" || label.name == "Error Code Title");
                if ((!instructions && !blueScreen) || !GameText.HasTranslation(label.text)) continue;
                var binding = label.gameObject.AddComponent<DesktopLocalizedText>(); binding.target = label; binding.source = label.text;
            }
        }
        private static void BindLabels(GameObject root, bool scene)
        {
            foreach (var button in root.GetComponentsInChildren<ButtonManager>(true))
            {
                if (scene && dynamicButtons.Contains(button))
                { var stale = button.GetComponent<DesktopLocalizedText>(); if (stale != null) Object.DestroyImmediate(stale); continue; }
                var lo = button.GetComponent<LocalizedObject>();
                if (lo != null && !string.IsNullOrEmpty(lo.localizationKey)) continue;
                if (!GameText.HasTranslation(button.buttonText)) continue;
                var binding = button.GetComponent<DesktopLocalizedText>() ?? button.gameObject.AddComponent<DesktopLocalizedText>();
                binding.source = button.buttonText; binding.button = button; button.useLocalization = false;
                binding.Apply(); bound++;
            }
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if ((scene && dynamicTexts.Contains(label)) || label.GetComponentInParent<HorizontalSelector>() != null)
                {
                    var stale = label.GetComponent<DesktopLocalizedText>();
                    if (stale != null) Object.DestroyImmediate(stale);
                    continue;
                }
                if (label.GetComponentInParent<ButtonManager>() != null || label.GetComponent<DesktopLocalizedClock>() != null) continue;
                if (label.GetComponentInParent<TMP_InputField>() is TMP_InputField input && input.textComponent == label) continue;
                // Native manager/list scripts own these data slots, not the authored template sample.
                if (!scene && (label.GetComponentInParent<ChatLayoutPreset>() != null || label.name == "Message" || label.name == "Author")) continue;
                var lo = label.GetComponent<LocalizedObject>();
                if (lo != null && !string.IsNullOrEmpty(lo.localizationKey)) continue;
                if (!GameText.HasTranslation(label.text)) continue;
                var binding = label.GetComponent<DesktopLocalizedText>() ?? label.gameObject.AddComponent<DesktopLocalizedText>();
                if (string.IsNullOrEmpty(binding.source)) binding.source = label.text;
                binding.target = label; binding.Apply(); bound++;
            }
        }
        /// <summary>Language is switched only in native Settings (辅助功能 → 语言). Removes the older taskbar 「中 / EN」 shortcut.</summary>
        private static void RemoveTaskbarShortcut(LocalizationManager manager)
        {
            foreach (var existing in manager.transform.root.GetComponentsInChildren<ButtonManager>(true).Where(b => b.name == "DesktopLanguageShortcut").ToArray())
                Object.DestroyImmediate(existing.gameObject);
        }
    }
}
#endif
