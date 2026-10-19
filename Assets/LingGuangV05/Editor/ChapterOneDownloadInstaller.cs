#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using LingGuangV05.Desktop.Apps;

namespace LingGuangV05.Editor
{
    /// <summary>
    /// 2016 download flow for Main: owned copies of the Aero web library and home page (shared Aero assets untouched),
    /// the lingguang.cc project page built from native DreamOS widgets, the downloadable 灵光.exe, 2016 home broadband
    /// networks, and "hongmengos 2016" branding. Idempotent. Gating and completion live in LingGuangInstallFlow at runtime.
    /// </summary>
    public static class ChapterOneDownloadInstaller
    {
        private const string WebDir = "Assets/LingGuangV05/Content/Native/Web";
        private const string LibraryPath = WebDir + "/LingGuangWebLibrary.asset";
        private const string HomePath = WebDir + "/_HomePage_LingGuang.prefab";
        private const string PagePath = WebDir + "/lingguang.cc.prefab";
        private const string Url = "lingguang.cc";
        private const float FileSizeMB = 46.8f;
        private const string IconPath = "Assets/LingGuangV05/DesktopBridge/OSArt/Textures/Icons/64x/Apps.png";
        private const string HeroPath = "Assets/LingGuangV05/Art/Generated/neural-workbench.png";
        private const string DownloadsTemplate = "Assets/HongmengOS/Aero2010/Theme/Prefabs/downloads.com_d74d3a24ece4b37408f750a5f982cb47.prefab";

        // Visible OS branding: 2010 → 2016. The monitor's printed serial label is hardware and stays.
        private static readonly string[][] Branding =
        {
            new[] { "hongmengos\n2010 • Aero 桌面", "hongmengos\n2016 • Aero 桌面" },
            new[] { "Aero 桌面\n经典电脑系列 · 2010 版", "Aero 桌面\n经典电脑系列 · 2016 版" },
            new[] { "hongmengos 2010", "hongmengos 2016" },
            new[] { "hongmengos\nAero 2010", "hongmengos\nAero 2016" },
        };

        [MenuItem("Tools/LingGuang/Install 2016 Download Flow")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("只能在编辑模式、编译完成后运行。");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ChapterOneInstaller.ScenePath) scene = EditorSceneManager.OpenScene(ChapterOneInstaller.ScenePath, OpenSceneMode.Single);
            var browser = Find<WebBrowserApp>(scene) ?? throw new InvalidOperationException("Main 缺少原生浏览器。");
            var network = Find<NetworkManager>(scene) ?? throw new InvalidOperationException("Main 缺少原生网络管理。");
            EnsureFolder(WebDir);
            var icon = Required<Sprite>(IconPath);

            var library = AssetDatabase.LoadAssetAtPath<WebBrowserLibrary>(LibraryPath);
            if (library == null)
            {
                string source = AssetDatabase.GetAssetPath(browser.webLibrary);
                if (string.IsNullOrEmpty(source) || source.StartsWith("Assets/LingGuangV05")) throw new InvalidOperationException("找不到原浏览器库。");
                if (!AssetDatabase.CopyAsset(source, LibraryPath)) throw new InvalidOperationException("复制浏览器库失败。");
                library = AssetDatabase.LoadAssetAtPath<WebBrowserLibrary>(LibraryPath);
            }
            var page = BuildPage();
            library.homePage.pageContent = BuildHome(library.homePage.pageContent, icon);
            library.webPages.RemoveAll(p => p.pageURL == Url);
            library.webPages.Insert(0, new WebBrowserLibrary.WebPage { pageTitle = "灵光 LingGuang", pageURL = Url, pageIcon = icon, pageContent = page, pageSize = 2 });
            library.dlFiles.RemoveAll(f => f.fileName == LingGuangInstallFlow.FileName);
            library.dlFiles.Insert(0, new WebBrowserLibrary.DownloadableFiles { fileName = LingGuangInstallFlow.FileName, fileIcon = icon, fileSize = FileSizeMB, fileType = WebBrowserLibrary.FileType.Other });
            EditorUtility.SetDirty(library);

            Undo.RecordObject(browser, "LingGuang download flow");
            browser.webLibrary = library;
            EditorUtility.SetDirty(browser);

            // 2016 home broadband: 20M = 2.5 MB/s (the lab's starting line), so the 46.8 MB download takes about 19 seconds.
            Undo.RecordObject(network, "2016 networks");
            network.networkItems = new List<NetworkManager.NetworkItem>
            {
                new NetworkManager.NetworkItem { networkID = "ChinaNet-8A3F", password = "88886666", signalPower = NetworkManager.SignalPower.Best, networkSpeed = 2.5f },
                new NetworkManager.NetworkItem { networkID = "TP-LINK_5C2E", password = "12345678", signalPower = NetworkManager.SignalPower.Strong, networkSpeed = 1.2f },
                new NetworkManager.NetworkItem { networkID = "CMCC-WEB", password = "cmcc2016", signalPower = NetworkManager.SignalPower.Weak, networkSpeed = .4f },
            };
            network.defaultSpeed = 2.5f;
            EditorUtility.SetDirty(network);

            int branded = Rebrand(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Main 保存失败。");
            Debug.Log("[LingGuang] 2016 下载流程已安装：lingguang.cc、灵光.exe（" + FileSizeMB + " MB）、家庭宽带、品牌文字 " + branded + " 处。");
        }

        // ---------- lingguang.cc ----------

        private static GameObject BuildPage()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PagePath) != null) AssetDatabase.DeleteAsset(PagePath);
            if (!AssetDatabase.CopyAsset(DownloadsTemplate, PagePath)) throw new InvalidOperationException("复制网页模板失败。");
            var root = PrefabUtility.LoadPrefabContents(PagePath);
            try
            {
                root.name = Url;
                var content = root.transform.Find("Content") as RectTransform;
                var titleTemplate = content.Find("Title").GetComponent<TextMeshProUGUI>();
                var buttonTemplate = content.Find("List").GetChild(0).gameObject;
                var backgroundTemplate = content.Find("Background").GetComponent<Image>();
                // Detach working copies of the templates, then clear the demo content.
                var title = Object.Instantiate(titleTemplate.gameObject); var button = Object.Instantiate(buttonTemplate); var band = Object.Instantiate(backgroundTemplate.gameObject);
                for (int i = content.childCount - 1; i >= 0; i--) Object.DestroyImmediate(content.GetChild(i).gameObject);

                var layout = content.GetComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(0, 0, 0, 20); layout.spacing = 14;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true; layout.childControlHeight = true;
                layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;

                var pageView = root.AddComponent<LingGuangDownloadPage>();
                pageView.fileSizeMB = FileSizeMB;

                // Header band: dark blue strip with name and tagline.
                var header = Panel(band, content, "Header", 900, 92, new Color32(16, 42, 67, 255));
                var headerLayout = header.gameObject.AddComponent<VerticalLayoutGroup>();
                headerLayout.padding = new RectOffset(32, 32, 14, 12); headerLayout.spacing = 2;
                headerLayout.childAlignment = TextAnchor.MiddleLeft; headerLayout.childControlWidth = headerLayout.childControlHeight = true;
                headerLayout.childForceExpandWidth = true; headerLayout.childForceExpandHeight = false;
                pageView.title = Text(title, header, "Name", "灵光 LingGuang", 32, Color.white, 0, 40, FontStyles.Bold, TextAlignmentOptions.Left);
                pageView.tagline = Text(title, header, "Tagline", "一个会自己学习的小程序", 17, new Color32(150, 205, 240, 255), 0, 24, FontStyles.Normal, TextAlignmentOptions.Left);

                // Two columns so the button and progress stay above the fold in the default window.
                var row = new GameObject("Row", typeof(RectTransform)).transform as RectTransform;
                row.SetParent(content, false);
                var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 28; rowLayout.childAlignment = TextAnchor.UpperCenter;
                rowLayout.childControlWidth = rowLayout.childControlHeight = true; rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;
                Element(row.gameObject, 900, 290);

                var hero = Panel(band, row, "Hero", 400, 200, Color.white);
                var heroImage = hero.GetComponent<Image>();
                heroImage.sprite = LoadSprite(HeroPath); heroImage.preserveAspect = true; heroImage.type = Image.Type.Simple;
                heroImage.color = Color.white;

                var column = new GameObject("Details", typeof(RectTransform)).transform as RectTransform;
                column.SetParent(row, false);
                var columnLayout = column.gameObject.AddComponent<VerticalLayoutGroup>();
                columnLayout.spacing = 8; columnLayout.childAlignment = TextAnchor.UpperLeft;
                columnLayout.childControlWidth = columnLayout.childControlHeight = true; columnLayout.childForceExpandWidth = columnLayout.childForceExpandHeight = false;
                Element(column.gameObject, 470, 290);

                pageView.meta = Text(title, column, "Meta", "v0.1.3 · 2016-05-20 · Windows 7 / 8 / 10 · 46.8 MB", 14, new Color32(90, 104, 118, 255), 470, 20, FontStyles.Normal, TextAlignmentOptions.Left);
                pageView.body = Text(title, column, "Body", "它一开始什么都不会，只会回答「是」和「否」。", 16, new Color32(30, 41, 52, 255), 470, 138, FontStyles.Normal, TextAlignmentOptions.TopLeft);

                var download = Object.Instantiate(button, column, false); download.name = "DownloadButton";
                var manager = download.GetComponent<ButtonManager>();
                foreach (var localized in download.GetComponentsInChildren<LocalizedObject>(true)) Object.DestroyImmediate(localized);
                manager.useLocalization = false; manager.buttonText = "下载 灵光 v0.1.3"; manager.textSize = 18;
                manager.onClick = new UnityEvent();
                UnityEventTools.AddPersistentListener(manager.onClick, pageView.OnDownloadClicked);
                var fit = download.GetComponent<ContentSizeFitter>(); if (fit != null) Object.DestroyImmediate(fit);
                Element(download, 260, 44);
                pageView.downloadButton = manager;

                pageView.progress = Progress(column, 470);
                pageView.status = Text(title, column, "Status", "", 14, new Color32(16, 42, 67, 255), 470, 22, FontStyles.Normal, TextAlignmentOptions.Left);
                pageView.footer = Text(title, content, "Footer", "© 2016 lingguang.cc", 12, new Color32(130, 140, 150, 255), 900, 18, FontStyles.Normal, TextAlignmentOptions.Center);

                Object.DestroyImmediate(title); Object.DestroyImmediate(button); Object.DestroyImmediate(band);
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;
                PrefabUtility.SaveAsPrefabAsset(root, PagePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return AssetDatabase.LoadAssetAtPath<GameObject>(PagePath);
        }

        /// <summary>Native slider: a clone of the Progress Bar inside the browser's own download item.</summary>
        private static Slider Progress(Transform parent, float width)
        {
            var wb = Object.FindAnyObjectByType<WebBrowserApp>(FindObjectsInactive.Include);
            var preset = (GameObject)new SerializedObject(wb).FindProperty("downloadPreset").objectReferenceValue;
            var bar = Object.Instantiate(preset.transform.Find("Progress Bar").gameObject, parent, false);
            bar.name = "Progress";
            var status = bar.transform.Find("Status"); if (status != null) Object.DestroyImmediate(status.gameObject);
            var handle = bar.transform.Find("Handle Slide Area"); if (handle != null) Object.DestroyImmediate(handle.gameObject);
            // The native bar has no track; give it a light one so 0% is visible.
            var track = bar.GetComponent<Image>() ?? bar.AddComponent<Image>();
            track.color = new Color32(214, 222, 230, 255); track.raycastTarget = false;
            var slider = bar.GetComponent<Slider>();
            if (slider.fillRect != null)
            {
                var fill = slider.fillRect.GetComponent<Image>();
                foreach (var e in slider.fillRect.GetComponents<UIManagerElement>()) Object.DestroyImmediate(e);
                fill.color = new Color32(29, 128, 196, 255); // themed fill is 20% alpha; the page needs a solid bar
            }
            slider.interactable = false; slider.handleRect = null; slider.minValue = 0; slider.maxValue = 1; slider.value = 0;
            Element(bar, width, 12);
            return slider;
        }

        private static RectTransform Panel(GameObject template, Transform parent, string name, float width, float height, Color color)
        {
            var go = Object.Instantiate(template, parent, false); go.name = name;
            foreach (var c in go.GetComponents<Component>()) if (c is AspectRatioFitter) Object.DestroyImmediate(c);
            foreach (Transform child in go.transform) Object.DestroyImmediate(child.gameObject);
            var image = go.GetComponent<Image>(); image.sprite = null; image.color = color; image.raycastTarget = false;
            Element(go, width, height);
            return (RectTransform)go.transform;
        }

        private static TMP_Text Text(GameObject template, Transform parent, string name, string value, float size, Color color,
            float width, float height, FontStyles style, TextAlignmentOptions alignment)
        {
            var go = Object.Instantiate(template, parent, false); go.name = name;
            foreach (var localized in go.GetComponentsInChildren<LocalizedObject>(true)) Object.DestroyImmediate(localized);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value; text.fontSize = size; text.color = color; text.fontStyle = style; text.alignment = alignment;
            text.enableAutoSizing = false; text.richText = false; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow;
            Element(go, width, height);
            return text;
        }

        private static void Element(GameObject go, float width, float height)
        {
            var e = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            e.ignoreLayout = false;
            if (width > 0) { e.preferredWidth = width; e.minWidth = width; }
            e.preferredHeight = height; e.minHeight = height; e.flexibleWidth = width > 0 ? 0 : 1; e.flexibleHeight = 0;
        }

        // ---------- browser home page: first tile is lingguang.cc ----------

        private static GameObject BuildHome(GameObject current, Sprite icon)
        {
            if (current != null && AssetDatabase.GetAssetPath(current) != HomePath)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(HomePath) != null) AssetDatabase.DeleteAsset(HomePath);
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(current), HomePath)) throw new InvalidOperationException("复制浏览器主页失败。");
            }
            var root = PrefabUtility.LoadPrefabContents(HomePath);
            try
            {
                var sites = root.GetComponentsInChildren<GridLayoutGroup>(true).First().transform;
                var existing = sites.Find(Url);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
                var template = sites.GetChild(0).gameObject;
                var tile = Object.Instantiate(template, sites, false); tile.name = Url; tile.transform.SetAsFirstSibling();
                foreach (var localized in tile.GetComponentsInChildren<LocalizedObject>(true)) Object.DestroyImmediate(localized);
                tile.GetComponentInChildren<TextMeshProUGUI>(true).text = Url;
                var tileIcon = tile.transform.Find("Icon"); if (tileIcon != null) tileIcon.GetComponent<Image>().sprite = icon;
                var so = new SerializedObject(tile.GetComponent<ButtonManager>());
                var calls = so.FindProperty("onClick.m_PersistentCalls.m_Calls");
                for (int i = 0; i < calls.arraySize; i++)
                    calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Arguments.m_StringArgument").stringValue = Url;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, HomePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return AssetDatabase.LoadAssetAtPath<GameObject>(HomePath);
        }

        // ---------- branding ----------

        private static int Rebrand(Scene scene)
        {
            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                    foreach (var pair in Branding)
                        if (text.text == pair[0]) { Undo.RecordObject(text, "2016"); text.text = pair[1]; EditorUtility.SetDirty(text); count++; }
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null) continue;
                    var so = new SerializedObject(behaviour); var source = so.FindProperty("source");
                    if (source == null || source.propertyType != SerializedPropertyType.String) continue;
                    foreach (var pair in Branding)
                        if (source.stringValue == pair[0]) { source.stringValue = pair[1]; so.ApplyModifiedProperties(); }
                }
            }
            return count;
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("缺少图片：" + path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.SaveAndReimport();
            return Required<Sprite>(path);
        }
        private static T Required<T>(string path) where T : Object
        {
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value == null) throw new InvalidOperationException("缺少资源：" + path);
            return value;
        }
        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects()) { var c = root.GetComponentInChildren<T>(true); if (c != null) return c; }
            return null;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int index = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, index));
            AssetDatabase.CreateFolder(path.Substring(0, index), path.Substring(index + 1));
        }
    }
}
#endif
