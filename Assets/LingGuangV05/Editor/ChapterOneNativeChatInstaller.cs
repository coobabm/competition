#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using HongmengOS.Aero2010;
using LingGuangV05.Desktop;
using LingGuangV05.UI;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using LingGuangV05.Desktop.Apps;

namespace LingGuangV05.Editor
{
    /// <summary>
    /// Restores the actual native Messaging hierarchy from the pristine source scene.
    /// It creates no UI and changes no shared theme, prefab, font or demo-chat asset.
    /// </summary>
    public static class ChapterOneNativeChatInstaller
    {
        public const string ChatAssetPath = "Assets/LingGuangV05/Content/Native/LaoZhou.asset";

        [MenuItem("Tools/LingGuang/Restore Native YY")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("原生 YY 恢复必须在完成编译后的编辑模式运行。");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("先保存当前场景修改；恢复器不会覆盖未保存工作。");
            var main = SceneManager.GetActiveScene();
            if (main.path != ChapterOneInstaller.ScenePath)
                main = EditorSceneManager.OpenScene(ChapterOneInstaller.ScenePath, OpenSceneMode.Single);
            var router = FindInScene<ChapterOneDesktopRouter>(main);
            var bridge = router != null ? router.Get("yy") : null;
            if (bridge == null || bridge.Window == null || bridge.Runtime == null
                || bridge.DesktopShortcut == null || bridge.TaskbarShortcut == null)
                throw new InvalidOperationException("Main 缺少 YY 的现有窗口/路由/运行时入口。");
            if (bridge.IsNativeChat && bridge.NativeChat.Manager != null)
            {
                ValidateNative(bridge, main);
                Debug.Log("[LingGuang] YY 已使用原生 Messaging；保留现有原生层级与绑定。", bridge);
                return;
            }

            string sourceHash = HashFile(ChapterOneInstaller.SourceScenePath);
            var reference = SceneManager.GetSceneByPath(ChapterOneInstaller.SourceScenePath);
            bool openedReference = !reference.IsValid() || !reference.isLoaded;
            GameObject temporaryClone = null;
            try
            {
                if (openedReference)
                    reference = EditorSceneManager.OpenScene(ChapterOneInstaller.SourceScenePath, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(main);
                var original = FindWindow(reference, "Messaging");
                if (original == null || original.GetComponent<MessagingApp>() == null
                    || original.GetComponent<WindowPanelManager>() == null)
                    throw new InvalidOperationException("原始 2010desktop 缺少完整原生 Messaging。");

                var target = bridge.Window;
                var taskbar = bridge.TaskbarShortcut;
                var shortcut = bridge.DesktopShortcut;
                temporaryClone = Object.Instantiate(original.gameObject, target.transform.parent, false);
                temporaryClone.name = "__NativeMessagingRestoreTemporary";
                temporaryClone.SetActive(false);
                if (PrefabUtility.IsPartOfPrefabInstance(temporaryClone))
                    PrefabUtility.UnpackPrefabInstance(temporaryClone, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                RestoreInPlace(temporaryClone, target.gameObject, reference, main);
                Object.DestroyImmediate(temporaryClone);
                temporaryClone = null;

                // This is data configuration, not a replacement contact/message view.
                var manager = target.GetComponent<MessagingApp>();
                Sprite portrait = manager.chatList.Count > 0 ? manager.chatList[0].individualPicture : null;
                manager.chatList = new List<MessagingApp.ChatItem>
                {
                    new MessagingApp.ChatItem
                    {
                        chatTitle = "老周", individualName = "老周", individualSurname = string.Empty,
                        individualPicture = portrait, chatAsset = GetOrCreateChat(),
                        defaultStatus = MessagingApp.Status.Online, isVisible = true
                    }
                };
                manager.currentLayout = 0;
                manager.useLocalization = false;
                manager.saveMessageHistory = false;
                manager.messageStoring = null;
                manager.debugStoryTeller = false;
                manager.externalEvents = new UnityEvent();
                manager.createdLayoutPresets.Clear();
                manager.storytellerReplyEvents.Clear();
                target.taskbarButton = taskbar;
                taskbar.windowManager = target;
                // Preserve native size/color/material/spacing and caption events.
                var title = target.transform.Find("Container/Dragger/Aero Window Title")?.GetComponent<TMP_Text>();
                if (title != null) title.text = "YY";
                var lcd = FindInScene<AeroLcdSourceBridge>(main);
                if (lcd == null || lcd.sourceCanvas == null)
                    throw new InvalidOperationException("Main 缺少现有 LCD 输入接入。");
                foreach (var transform in target.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 5;
                foreach (var canvas in target.GetComponentsInChildren<Canvas>(true)) canvas.worldCamera = lcd.sourceCamera;
                foreach (var raycaster in target.GetComponentsInChildren<UnityEngine.UI.GraphicRaycaster>(true))
                    raycaster.enabled = false;

                var adapter = bridge.GetComponent<ChapterOneNativeChatAdapter>()
                    ?? bridge.gameObject.AddComponent<ChapterOneNativeChatAdapter>();
                adapter.Manager = manager;
                adapter.Runtime = bridge.Runtime;
                adapter.Window = target;
                var labels = new List<TMP_Text>();
                labels.AddRange(shortcut.GetComponentsInChildren<TMP_Text>(true));
                labels.AddRange(taskbar.GetComponentsInChildren<TMP_Text>(true));
                var start = lcd.sourceCanvas.GetComponentInChildren<AeroStartMenu>(true);
                if (start != null && start.programs != null)
                    foreach (var entry in start.programs)
                        if (entry.window == target && entry.row != null)
                            labels.AddRange(entry.row.GetComponentsInChildren<TMP_Text>(true));
                adapter.ShellLabels = labels.ToArray();
                bridge.ConfigureNative(bridge.Runtime, target, adapter, shortcut, taskbar, labels.ToArray());
                target.gameObject.SetActive(original.gameObject.activeSelf);
                ValidateReferences(target.gameObject, reference, main);
                ValidateNative(bridge, main);
            }
            finally
            {
                if (temporaryClone != null) Object.DestroyImmediate(temporaryClone);
                if (openedReference && reference.IsValid() && reference.isLoaded)
                    EditorSceneManager.CloseScene(reference, true); // Never save the reference scene.
                if (main.IsValid() && main.isLoaded) SceneManager.SetActiveScene(main);
            }
            if (!string.Equals(sourceHash, HashFile(ChapterOneInstaller.SourceScenePath), StringComparison.Ordinal))
                throw new InvalidOperationException("原始场景文件哈希发生变化；已停止 Main 保存，请立即检查。");
            EditorSceneManager.MarkSceneDirty(main);
            if (!EditorSceneManager.SaveScene(main, ChapterOneInstaller.ScenePath))
                throw new InvalidOperationException("原生 YY 已恢复，但 Main 保存失败。");
            Selection.activeGameObject = bridge.Window.gameObject;
            Debug.Log("[LingGuang] YY 已恢复原生 Messaging 联系人、气泡、输入框和窗口控件；只接入老周数据与回复。", bridge);
        }

        private static void RestoreInPlace(GameObject clone, GameObject destination, Scene reference, Scene main)
        {
            destination.SetActive(false);
            // Keep existing root object/component identities used by taskbar/router.
            var replacements = new Dictionary<Object, Object>
            {
                { clone, destination }, { clone.transform, destination.transform }
            };
            var sources = clone.GetComponents<Component>();
            var types = new HashSet<Type>();
            foreach (var component in sources)
            {
                if (component == null) throw new InvalidOperationException("原生 Messaging 根存在缺失脚本。");
                types.Add(component.GetType());
            }
            foreach (var component in destination.GetComponents<Component>())
                if (component != null && !(component is Transform) && !types.Contains(component.GetType()))
                    Object.DestroyImmediate(component);
            var counts = new Dictionary<Type, int>();
            foreach (var component in sources)
            {
                if (component is Transform) continue;
                var type = component.GetType();
                counts.TryGetValue(type, out int index);
                var targets = destination.GetComponents(type);
                Component target = targets.Length > index ? targets[index] : destination.AddComponent(type);
                counts[type] = index + 1;
                replacements.Add(component, target);
            }
            for (int i = destination.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(destination.transform.GetChild(i).gameObject);
            while (clone.transform.childCount > 0)
                clone.transform.GetChild(0).SetParent(destination.transform, false);
            CopyTransform(clone.transform, destination.transform);
            destination.layer = clone.layer;
            foreach (var component in sources)
                if (!(component is Transform)) EditorUtility.CopySerialized(component, replacements[component]);

            var lookup = SceneObjectLookup(main);
            foreach (var component in destination.GetComponentsInChildren<Component>(true))
            {
                if (component == null) throw new InvalidOperationException("恢复后的原生 Messaging 存在缺失脚本。");
                var serialized = new SerializedObject(component);
                var iterator = serialized.GetIterator();
                bool changed = false;
                while (iterator.Next(true))
                {
                    if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var value = iterator.objectReferenceValue;
                    if (value == null || EditorUtility.IsPersistent(value)) continue;
                    if (replacements.TryGetValue(value, out var replacement))
                    {
                        iterator.objectReferenceValue = replacement;
                        changed = true;
                    }
                    else if (SceneOf(value) == reference)
                    {
                        iterator.objectReferenceValue = ResolveExternal(value, lookup);
                        changed = true;
                    }
                }
                if (changed) serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void CopyTransform(Transform source, Transform destination)
        {
            destination.localPosition = source.localPosition;
            destination.localRotation = source.localRotation;
            destination.localScale = source.localScale;
            if (source is RectTransform from && destination is RectTransform to)
            {
                to.anchorMin = from.anchorMin;
                to.anchorMax = from.anchorMax;
                to.pivot = from.pivot;
                to.sizeDelta = from.sizeDelta;
                to.anchoredPosition3D = from.anchoredPosition3D;
            }
        }

        private static Dictionary<string, GameObject> SceneObjectLookup(Scene scene)
        {
            var result = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    result.Add(HierarchyKey(transform), transform.gameObject);
            return result;
        }

        private static string HierarchyKey(Transform transform)
        {
            int ordinal = 0;
            if (transform.parent != null)
                for (int i = 0; i < transform.GetSiblingIndex(); i++)
                    if (transform.parent.GetChild(i).name == transform.name) ordinal++;
            return (transform.parent != null ? HierarchyKey(transform.parent) : string.Empty)
                + "/" + transform.name.Length + ":" + transform.name + "#" + ordinal;
        }

        private static Object ResolveExternal(Object source, Dictionary<string, GameObject> lookup)
        {
            var original = source is GameObject gameObject ? gameObject : (source as Component)?.gameObject;
            if (original == null || !lookup.TryGetValue(HierarchyKey(original.transform), out var destination))
                throw new InvalidOperationException("原生聊天外部场景引用无法映射到 Main：" + source.name);
            if (source is GameObject) return destination;
            var component = (Component)source;
            var originalComponents = original.GetComponents(component.GetType());
            int index = Array.IndexOf(originalComponents, component);
            var destinationComponents = destination.GetComponents(component.GetType());
            if (index < 0 || index >= destinationComponents.Length)
                throw new InvalidOperationException("Main 缺少原生聊天引用的外部组件：" + source.GetType().Name + " / " + source.name);
            return destinationComponents[index];
        }

        private static Scene SceneOf(Object value)
        {
            if (value is GameObject gameObject) return gameObject.scene;
            if (value is Component component) return component.gameObject.scene;
            return default;
        }

        private static void ValidateReferences(GameObject root, Scene reference, Scene main)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) throw new InvalidOperationException("原生聊天存在缺失脚本。");
                var iterator = new SerializedObject(component).GetIterator();
                while (iterator.Next(true))
                {
                    if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var value = iterator.objectReferenceValue;
                    if (value == null || EditorUtility.IsPersistent(value)) continue;
                    Scene owner = SceneOf(value);
                    if (owner.IsValid() && (owner == reference || owner != main))
                        throw new InvalidOperationException("原生聊天仍引用 Main 外场景：" + component.name + "." + iterator.propertyPath);
                }
            }
        }

        private static MessagingChat GetOrCreateChat()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MessagingChat>(ChatAssetPath);
            if (existing != null)
            {
                if (existing.messageList == null || existing.messageList.Count == 0)
                    throw new InvalidOperationException("老周聊天数据为空；原生 Messaging 要求至少一条初始消息。");
                return existing;
            }
            EnsureFolder("Assets/LingGuangV05/Content/Native");
            var chat = ScriptableObject.CreateInstance<MessagingChat>();
            chat.name = "LaoZhou";
            chat.saveConversation = false;
            chat.useDynamicMessages = false;
            chat.useStoryTeller = false;
            // DreamOS reads the last history entry unconditionally. An empty date record
            // keeps its contact initialization valid without showing a fabricated message.
            // ChapterOneNativeChatAdapter hides this non-message seed after initialization.
            chat.messageList.Add(new MessagingChat.ChatMessage
            {
                objectType = MessagingChat.ObjectType.Date,
                sentTime = "", messageContent = ""
            });
            AssetDatabase.CreateAsset(chat, ChatAssetPath);
            return chat;
        }

        private static void ValidateNative(ChapterOneDesktopBridge bridge, Scene main)
        {
            if (!bridge.IsNativeChat || bridge.App != null || bridge.NativeChat.Manager == null
                || bridge.Window.GetComponent<MessagingApp>() != bridge.NativeChat.Manager
                || bridge.Window.GetComponent<WindowPanelManager>() == null
                || bridge.NativeChat.Manager.messageInput == null || bridge.NativeChat.Manager.chatViewer == null
                || bridge.Window.GetComponentsInChildren<ChapterOneApp>(true).Length != 0
                || bridge.gameObject.scene != main || bridge.NativeChat.Runtime != bridge.Runtime)
                throw new InvalidOperationException("原生 YY 恢复校验失败；未保存 Main。");
        }

        private static WindowManager FindWindow(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var window in root.GetComponentsInChildren<WindowManager>(true))
                    if (window.name == name) return window;
            return null;
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

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        private static string HashFile(string path)
        {
            string absolute = Path.IsPathRooted(path) ? path
                : Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
            using (var stream = File.OpenRead(absolute))
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(stream));
        }
    }
}
#endif
