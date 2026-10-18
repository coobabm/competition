#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using HongmengOS.Aero2010;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Apps;
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
    /// <summary>Native 2010-style buddy window + classic text conversation; no bitmap UI.</summary>
    public static class YY2010Installer
    {
        private const string Owned = "Assets/LingGuangV05/Content/Native/YY2010";
        private const string PandaPath = "Assets/LingGuangV05/Art/Generated/yy-panda-red-scarf.png";
        private const string ButtonPath = "Assets/DreamOS - Complete OS UI/Prefabs/UI Elements/Button/Default Button.prefab";
        private const string InputPath = "Assets/DreamOS - Complete OS UI/Prefabs/UI Elements/Input Field/Input Field.prefab";
        private const string RowPath = "Assets/DreamOS - Complete OS UI/Prefabs/Settings/Settings Element - Button.prefab";
        private static readonly Color Ink = new Color32(32, 53, 75, 255);
        private static readonly Color Subtle = new Color32(88, 112, 135, 255);
        private static readonly Color Blue = new Color32(88, 171, 226, 255);

        [MenuItem("Tools/LingGuang/Install YY 2010")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("YY 2010 安装器只在已完成编译的编辑模式运行。");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("请先保存当前场景修改。");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ChapterOneInstaller.ScenePath)
                scene = EditorSceneManager.OpenScene(ChapterOneInstaller.ScenePath, OpenSceneMode.Single);
            var router = Find<ChapterOneDesktopRouter>(scene);
            var binding = router != null ? router.Get("yy") : null;
            if (binding == null || !binding.IsNativeChat || binding.NativeChat.Manager == null)
                throw new InvalidOperationException("请先恢复原生 YY；本安装器不使用旧自绘聊天界面。");
            var existing = Find<YY2010ShellController>(scene);
            if (existing != null && existing.FriendsWindow != null && existing.ChatBinding == binding)
            {
                Validate(existing);
                RepairExisting(existing, scene);
                Debug.Log("[YY2010] 已修复现有原生发送标签与经典消息行布局；窗口和会话引用未重建。", existing);
                return;
            }
            var manager = binding.NativeChat.Manager;
            var chat = binding.Window;
            var lcd = Find<AeroLcdSourceBridge>(scene);
            if (lcd == null || lcd.sourceCanvas == null) throw new InvalidOperationException("缺少现有 LCD 接入。");
            var panda = Panda();
            var theme = Theme();
            var buttonPrefab = Required<GameObject>(ButtonPath);
            var inputPrefab = Required<GameObject>(InputPath);
            var rowPrefab = Required<GameObject>(RowPath);
            var managerData = new SerializedObject(manager);
            var contactList = managerData.FindProperty("chatParent").objectReferenceValue as RectTransform;
            var friendScroll = contactList != null ? contactList.GetComponentInParent<ScrollRect>() : null;
            var send = chat.transform.Find("Container/Content/Main Content/Chat Viewer/Controls/Send")?.GetComponent<ButtonManager>();
            if (contactList == null || friendScroll == null || send == null || manager.messageInput == null)
                throw new InvalidOperationException("原生 Messaging 联系人、滚动容器或发送控件缺失。");

            GameObject nativeLayout = manager.chatLayout;
            GameObject layout = Variant(nativeLayout, "Classic Chat Layout.prefab", go => ConfigureChatLayout(go));
            GameObject item = Variant((GameObject)managerData.FindProperty("chatItem").objectReferenceValue,
                "Compact Friend Item.prefab", go => ConfigureContact(go, panda, theme));
            GameObject sent = Variant((GameObject)managerData.FindProperty("textMessageSent").objectReferenceValue,
                "Classic Sent.prefab", go => ConfigureMessage(go, "我", theme));
            GameObject received = Variant((GameObject)managerData.FindProperty("textMessageRecieved").objectReferenceValue,
                "Classic Received.prefab", go => ConfigureMessage(go, "老周", theme));
            var shell = binding.gameObject.AddComponent<YY2010ShellController>();
            shell.ChatBinding = binding; shell.Manager = manager; shell.PandaAvatar = panda;
            var friends = CloneBuddy(chat);
            shell.FriendsWindow = friends;
            var taskbar = CloneTaskbar(binding.TaskbarShortcut, friends, shell);
            friends.taskbarButton = taskbar;
            var chromeTitle = friends.transform.Find("Container/Dragger/Aero Window Title")?.GetComponent<TMP_Text>();
            if (chromeTitle != null) chromeTitle.text = "YY 2010";
            var buddyContent = friends.windowContent;
            var chatContent = chat.transform.Find("Container/Content") as RectTransform;
            var builder = new Builder(theme, buttonPrefab, inputPrefab, rowPrefab);
            var labels = new List<TMP_Text>();
            var avatars = new List<Image>();

            // The existing contact-list object and ScrollRect move intact: manager
            // keeps generating real ChatItemPreset rows into the same object ID.
            friendScroll.transform.SetParent(buddyContent, false);
            Position((RectTransform)friendScroll.transform, 6, 236, 334, 356, 346, 653);
            friendScroll.horizontal = false; friendScroll.vertical = true;
            friendScroll.movementType = ScrollRect.MovementType.Clamped;
            contactList.anchorMin = new Vector2(0, 1); contactList.anchorMax = Vector2.one;
            contactList.pivot = new Vector2(.5f, 1); contactList.anchoredPosition = Vector2.zero;
            contactList.sizeDelta = new Vector2(0, 0);
            var friendLayout = contactList.GetComponent<VerticalLayoutGroup>();
            if (friendLayout != null) { friendLayout.padding = new RectOffset(0, 0, 2, 2); friendLayout.spacing = 1; friendLayout.childControlHeight = true; friendLayout.childForceExpandHeight = false; }
            for (int i = contactList.childCount - 1; i >= 0; i--) Object.DestroyImmediate(contactList.GetChild(i).gameObject);
            shell.FriendList = contactList;
            builder.Surface(buddyContent, "YY2010 Header", 0, 0, 346, 112, 346, 653,
                new Color32(90, 179, 232, 255), new Color32(199, 235, 252, 255));
            avatars.Add(builder.Avatar(nativeLayout, buddyContent, "My Panda", panda, 12, 13, 54, 54, 346, 653));
            labels.Add(builder.Text(buddyContent, "玩家", 80, 14, 245, 29, 18, 346, 653));
            labels.Add(builder.Text(buddyContent, "● 在线  ·  我的 YY", 80, 45, 245, 25, 14, 346, 653));
            labels.Add(builder.Text(buddyContent, "认真生活，慢慢学会。", 13, 80, 320, 25, 15, 346, 653));
            shell.SearchInput = builder.Input(buddyContent, "Buddy Search", "搜索联系人", 8, 119, 330, 34, 346, 653);
            shell.FriendsTab = builder.Button(buddyContent, "Friends Tab", "好友", 8, 160, 104, 34, 346, 653);
            shell.GroupsTab = builder.Button(buddyContent, "Groups Tab", "群", 120, 160, 104, 34, 346, 653);
            shell.RecentTab = builder.Button(buddyContent, "Recent Tab", "最近", 232, 160, 106, 34, 346, 653);
            labels.Add(builder.Text(buddyContent, "▼ 我的好友 (1)", 13, 204, 320, 25, 15, 346, 653));
            shell.EmptyStateText = builder.Text(buddyContent, "暂无群聊", 18, 258, 310, 56, 15, 346, 653);
            shell.EmptyStateText.gameObject.SetActive(false); labels.Add(shell.EmptyStateText);
            builder.Surface(buddyContent, "YY2010 Bottom Bar", 0, 601, 346, 52, 346, 653,
                new Color32(224, 241, 251, 255), new Color32(174, 217, 244, 255));
            var open = builder.Button(buddyContent, "Open Lao Zhou", "找老周聊天", 8, 611, 190, 34, 346, 653);
            UnityEventTools.AddPersistentListener(open.onClick, shell.OpenChat);
            var closeBuddy = builder.Button(buddyContent, "Close Buddy", "关闭", 234, 611, 104, 34, 346, 653);
            UnityEventTools.AddPersistentListener(closeBuddy.onClick, shell.CloseBuddy);

            // Move the authoritative native viewer, input, and send button before
            // archiving old demo navigation. The manager itself stays on Chat root.
            var history = manager.chatViewer as RectTransform;
            history.SetParent(chatContent, false);
            manager.messageInput.transform.SetParent(chatContent, false);
            send.transform.SetParent(chatContent, false);
            var old = new GameObject("YY2010 Archived Native Navigation", typeof(RectTransform));
            old.transform.SetParent(chatContent, false); old.SetActive(false);
            var retained = new HashSet<Transform> { history, manager.messageInput.transform, send.transform, old.transform };
            for (int i = chatContent.childCount - 1; i >= 0; i--)
                if (!retained.Contains(chatContent.GetChild(i))) chatContent.GetChild(i).SetParent(old.transform, false);
            var panelManager = chat.GetComponent<WindowPanelManager>();
            if (panelManager != null) { panelManager.panels.Clear(); panelManager.currentPanelIndex = 0; panelManager.enabled = false; }
            manager.storyTellerAnimator = null;
            for (int i = history.childCount - 1; i >= 0; i--) Object.DestroyImmediate(history.GetChild(i).gameObject);
            SetPlainWindow(chat, chatContent);
            SetSize(chat.windowContainer, 980, 720, -190, 0);
            SetSize(friends.windowContainer, 360, 720, 540, 0);
            const float cw = 966, ch = 653;
            builder.Surface(chatContent, "YY2010 Conversation Header", 0, 0, cw, 76, cw, ch,
                new Color32(95, 181, 233, 255), new Color32(211, 238, 253, 255));
            labels.Add(builder.Text(chatContent, "老周", 20, 11, 650, 30, 19, cw, ch));
            labels.Add(builder.Text(chatContent, "在线  ·  本地预设的第一章指导", 20, 45, 650, 25, 14, cw, ch));
            Position(history, 0, 80, 724, 337, cw, ch);
            builder.Surface(chatContent, "YY2010 Profile Column", 737, 80, 229, 573, cw, ch, Color.white, new Color32(231, 244, 252, 255));
            avatars.Add(builder.Avatar(nativeLayout, chatContent, "Lao Zhou Panda", panda, 764, 106, 176, 176, cw, ch));
            labels.Add(builder.Text(chatContent, "老周  /  在线", 753, 299, 205, 30, 17, cw, ch));
            labels.Add(builder.Text(chatContent, "第一章向导\n\n不连接网络\n不调用真实大模型", 753, 340, 205, 119, 14, cw, ch));
            avatars.Add(builder.Avatar(nativeLayout, chatContent, "My Profile Panda", panda, 792, 489, 116, 116, cw, ch));
            labels.Add(builder.Text(chatContent, "我的头像", 775, 616, 164, 25, 14, cw, ch));
            builder.Surface(chatContent, "YY2010 Tool Strip", 0, 425, 724, 39, cw, ch,
                new Color32(233, 246, 253, 255), new Color32(201, 230, 247, 255));
            shell.EmojiButton = builder.Button(chatContent, "Emoji", "表情", 9, 429, 84, 31, cw, ch);
            shell.HistoryButton = builder.Button(chatContent, "History", "消息记录", 558, 429, 156, 31, cw, ch);
            var voice = builder.Button(chatContent, "Voice Unavailable", "语音 · 暂不可用", 106, 429, 192, 31, cw, ch);
            voice.isInteractable = false;
            Position(manager.messageInput.GetComponent<RectTransform>(), 8, 473, 706, 122, cw, ch);
            manager.messageInput.lineType = TMP_InputField.LineType.MultiLineNewline;
            manager.messageInput.characterLimit = 160; manager.messageInput.richText = false;
            var placeholder = manager.messageInput.transform.Find("Placeholder")?.GetComponent<TMP_Text>();
            if (placeholder != null) { placeholder.text = "输入消息，点击发送"; placeholder.overflowMode = TextOverflowModes.Overflow; labels.Add(placeholder); }
            builder.Prepare(send, "发送"); Position(send.GetComponent<RectTransform>(), 579, 612, 135, 34, cw, ch);
            send.onClick = new UnityEvent(); UnityEventTools.AddPersistentListener(send.onClick, manager.CreateMessageFromInput);
            shell.SendChatButton = send;
            shell.CloseChatButton = builder.Button(chatContent, "Close Conversation", "关闭", 435, 612, 132, 34, cw, ch);
            labels.Add(builder.Text(chatContent, "本地会话 · 游戏窗口关闭后学习仍继续", 12, 612, 412, 29, 13, cw, ch));
            history.SetAsLastSibling(); manager.messageInput.transform.SetAsLastSibling(); send.transform.SetAsLastSibling();

            managerData = new SerializedObject(manager);
            managerData.FindProperty("chatParent").objectReferenceValue = contactList;
            managerData.FindProperty("chatItem").objectReferenceValue = item;
            managerData.FindProperty("chatLayout").objectReferenceValue = layout;
            managerData.FindProperty("textMessageSent").objectReferenceValue = sent;
            managerData.FindProperty("textMessageRecieved").objectReferenceValue = received;
            managerData.ApplyModifiedPropertiesWithoutUndo();
            foreach (var contact in manager.chatList) contact.individualPicture = panda;
            binding.DesktopShortcut.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(binding.DesktopShortcut.onClick, shell.OpenBuddy);
            shell.AvatarImages = avatars.ToArray();
            labels.AddRange(friends.GetComponentsInChildren<TMP_Text>(true));
            labels.AddRange(chat.GetComponentsInChildren<TMP_Text>(true));
            labels.AddRange(taskbar.GetComponentsInChildren<TMP_Text>(true));
            shell.ShellLabels = labels.ToArray();
            foreach (var window in new[] { chat, friends })
            {
                window.gameObject.SetActive(true);
                var group = window.GetComponent<CanvasGroup>(); group.alpha = 0; group.interactable = group.blocksRaycasts = false;
                foreach (var t in window.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;
                foreach (var canvas in window.GetComponentsInChildren<Canvas>(true)) canvas.worldCamera = lcd.sourceCamera;
                foreach (var raycaster in window.GetComponentsInChildren<GraphicRaycaster>(true)) raycaster.enabled = false;
            }
            var startMenu = lcd.sourceCanvas.GetComponentInChildren<AeroStartMenu>(true);
            if (startMenu != null)
            {
                var windows = new List<WindowManager>(startMenu.windows ?? Array.Empty<WindowManager>());
                if (!windows.Contains(friends)) windows.Add(friends); startMenu.windows = windows.ToArray();
            }
            Validate(shell);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ChapterOneInstaller.ScenePath)) throw new InvalidOperationException("YY 2010 保存 Main 失败。");
            Debug.Log("[YY2010] 原生窄好友面板 + 独立经典文字聊天已安装；原生发送/会话/路由保持。", shell);
        }

        private static WindowManager CloneBuddy(WindowManager source)
        {
            var go = Object.Instantiate(source.gameObject, source.transform.parent, false); go.name = "YY 2010 好友"; go.SetActive(false);
            var wm = go.GetComponent<WindowManager>();
            var content = go.transform.Find("Container/Content") as RectTransform;
            for (int i = content.childCount - 1; i >= 0; i--) Object.DestroyImmediate(content.GetChild(i).gameObject);
            foreach (var behaviour in go.GetComponents<MonoBehaviour>())
                if (behaviour != null && !(behaviour is WindowManager)) Object.DestroyImmediate(behaviour);
            wm.onOpen = new UnityEvent(); wm.onClose = new UnityEvent(); wm.onMinimize = new UnityEvent(); wm.onFullscreen = new UnityEvent();
            SetPlainWindow(wm, content);
            wm.taskbarButton = null;
            return wm;
        }
        private static void SetPlainWindow(WindowManager wm, RectTransform content)
        {
            wm.windowContent = content; wm.navbarRect = null; wm.useBackgroundBlur = false; wm.resizeAnchor = WindowManager.ResizeAnchor.Disabled;
            wm.windowDragger.dragArea = wm.transform.parent as RectTransform; wm.windowDragger.wManager = wm;
            var data = new SerializedObject(wm); data.FindProperty("hasNavDrawer").boolValue = false;
            data.FindProperty("disableAtStart").boolValue = true; data.FindProperty("resizePreset").objectReferenceValue = null;
            data.ApplyModifiedPropertiesWithoutUndo();
            var image = content.GetComponent<Image>(); if (image != null) { RemoveColorDriver(image); image.color = Color.white; }
            content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(7, 28); content.offsetMax = new Vector2(-7, -39);
        }
        private static TaskbarButton CloneTaskbar(TaskbarButton source, WindowManager window, YY2010ShellController shell)
        {
            var go = Object.Instantiate(source.gameObject, source.transform.parent, false); go.name = "YY 2010 Buddy Taskbar";
            var button = go.GetComponent<TaskbarButton>(); button.windowManager = window; button.buttonTitle = "YY 好友";
            button.appElements.Clear(); button.onClick = new UnityEvent(); UnityEventTools.AddPersistentListener(button.onClick, shell.OpenBuddy);
            foreach (var element in go.GetComponentsInChildren<AppElement>(true)) Object.DestroyImmediate(element);
            foreach (var localized in go.GetComponentsInChildren<LocalizedObject>(true)) Object.DestroyImmediate(localized);
            foreach (var label in go.GetComponentsInChildren<TMP_Text>(true)) if (label.text == "YY") label.text = "YY 好友";
            foreach (var manager in go.GetComponentsInChildren<ButtonManager>(true))
            { manager.onClick = new UnityEvent(); manager.onDoubleClick = new UnityEvent(); manager.useLocalization = false; }
            var data = new SerializedObject(button); data.FindProperty("openWindowsOnly").boolValue = true; data.ApplyModifiedPropertiesWithoutUndo();
            var layout = source.GetComponentInParent<AeroTaskbarLayout>();
            var entries = new List<AeroTaskbarLayout.Entry>(layout.entries);
            entries.Add(new AeroTaskbarLayout.Entry { button = button, window = window, rect = (RectTransform)go.transform,
                icon = go.transform.Find("Icon") as RectTransform, visibility = go.GetComponent<CanvasGroup>(),
                face = go.GetComponentInChildren<AeroShellGraphic>(true), windowGroup = window.GetComponent<CanvasGroup>() });
            layout.entries = entries.ToArray();
            return button;
        }

        private static GameObject Variant(GameObject source, string filename, Action<GameObject> configure)
        {
            EnsureFolder(Owned); string path = Owned + "/" + filename;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null && !AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path))
                throw new InvalidOperationException("无法复制原生聊天模板：" + filename);
            var root = PrefabUtility.LoadPrefabContents(path);
            try { configure(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return Required<GameObject>(path);
        }
        private static void RepairExisting(YY2010ShellController shell, Scene scene)
        {
            var theme = Theme();
            var data = new SerializedObject(shell.Manager);
            var sent = Variant((GameObject)data.FindProperty("textMessageSent").objectReferenceValue,
                "Classic Sent.prefab", go => ConfigureMessage(go, "我", theme));
            var received = Variant((GameObject)data.FindProperty("textMessageRecieved").objectReferenceValue,
                "Classic Received.prefab", go => ConfigureMessage(go, "老周", theme));
            data.FindProperty("textMessageSent").objectReferenceValue = sent;
            data.FindProperty("textMessageRecieved").objectReferenceValue = received;
            data.ApplyModifiedPropertiesWithoutUndo();
            var send = shell.SendChatButton;
            var nativeSendEvent = send.onClick;
            var builder = new Builder(theme, Required<GameObject>(ButtonPath), Required<GameObject>(InputPath), Required<GameObject>(RowPath));
            builder.Prepare(send, "发送");
            send.onClick = nativeSendEvent; // Keep the exact existing native send binding.
            EditorUtility.SetDirty(send);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ChapterOneInstaller.ScenePath))
                throw new InvalidOperationException("YY 2010 标签/行布局修复保存失败。");
        }
        private static void ConfigureMessage(GameObject go, string author, UIManager theme)
        {
            var preset = go.GetComponent<ChatMessagePreset>();
            if (preset == null || preset.timeText == null || preset.contentText == null) throw new InvalidOperationException("原生文本消息模板不完整。");
            preset.timeText.transform.SetParent(go.transform, false); preset.contentText.transform.SetParent(go.transform, false);
            for (int i = go.transform.childCount - 1; i >= 0; i--)
                if (go.transform.GetChild(i) != preset.timeText.transform && go.transform.GetChild(i) != preset.contentText.transform)
                    Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
            foreach (var animation in go.GetComponents<MessageAnimation>()) Object.DestroyImmediate(animation);
            foreach (var animator in go.GetComponents<Animator>()) Object.DestroyImmediate(animator);
            foreach (var group in go.GetComponents<LayoutGroup>()) Object.DestroyImmediate(group);
            var rows = go.AddComponent<VerticalLayoutGroup>(); rows.padding = new RectOffset(12, 12, 5, 7); rows.spacing = 2;
            rows.childAlignment = TextAnchor.UpperLeft;
            rows.childControlWidth = rows.childControlHeight = true; rows.childForceExpandWidth = true; rows.childForceExpandHeight = false;
            var fitter = go.GetComponent<ContentSizeFitter>() ?? go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var element = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            element.ignoreLayout = false; element.minHeight = 60; element.preferredHeight = -1; element.flexibleWidth = 1;
            var messageRect = go.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0, 1); messageRect.anchorMax = Vector2.one;
            messageRect.pivot = new Vector2(0, 1); messageRect.anchoredPosition = Vector2.zero;
            messageRect.sizeDelta = new Vector2(0, 60); messageRect.localScale = Vector3.one;
            PrepareText(preset.timeText, author == "我" ? new Color32(29, 128, 59, 255) : new Color32(37, 89, 153, 255), 14);
            PrepareText(preset.contentText, Ink, 16); preset.contentText.textWrappingMode = TextWrappingModes.Normal;
            preset.timeText.transform.SetAsFirstSibling();
            foreach (var text in new[] { preset.timeText, preset.contentText })
            {
                foreach (var fit in text.GetComponents<ContentSizeFitter>()) Object.DestroyImmediate(fit);
                var line = text.GetComponent<LayoutElement>() ?? text.gameObject.AddComponent<LayoutElement>();
                // The source bubble's timestamp deliberately ignored its old layout
                // and had a fixed 75px, offset overlay. It must join this text row.
                line.ignoreLayout = false;
                line.minWidth = 0; line.preferredWidth = -1; line.flexibleWidth = 1;
                line.minHeight = text == preset.timeText ? 23 : 26; line.preferredHeight = -1; line.flexibleHeight = 0;
                var rect = text.rectTransform;
                rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0, 1); rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0, line.minHeight); rect.localScale = Vector3.one;
            }
            var stamp = go.GetComponent<YY2010MessageStamp>() ?? go.AddComponent<YY2010MessageStamp>(); stamp.Preset = preset; stamp.Author = author;
            var canvas = go.GetComponent<CanvasGroup>(); if (canvas != null) { canvas.alpha = 1; canvas.interactable = canvas.blocksRaycasts = true; }
            Layer(go.transform);
        }
        private static void ConfigureChatLayout(GameObject go)
        {
            var preset = go.GetComponent<ChatLayoutPreset>();
            if (preset == null) throw new InvalidOperationException("原生 ChatLayoutPreset 缺失。");
            var profile = go.transform.Find("Profile"); if (profile != null) profile.gameObject.SetActive(false);
            preset.nameText.gameObject.SetActive(false);
            var scroll = preset.messageParent.GetComponentInParent<ScrollRect>();
            var rect = scroll.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            var vlg = preset.messageParent.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) { vlg.spacing = 1; vlg.padding = new RectOffset(0, 0, 4, 8); vlg.childControlWidth = vlg.childControlHeight = true; vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true; }
            foreach (var image in go.GetComponentsInChildren<Image>(true))
                if (image.transform == scroll.transform) { RemoveColorDriver(image); image.color = Color.white; }
            Layer(go.transform);
        }
        private static void ConfigureContact(GameObject go, Sprite panda, UIManager theme)
        {
            var preset = go.GetComponent<ChatItemPreset>();
            foreach (var group in go.GetComponentsInChildren<LayoutGroup>(true)) group.enabled = false;
            var layout = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>(); layout.minHeight = layout.preferredHeight = 66;
            var rect = go.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(330, 66);
            var profile = preset.coverImage.transform.parent as RectTransform;
            Position(profile, 7, 8, 46, 46, 330, 66);
            preset.coverImage.sprite = panda; preset.coverImage.preserveAspect = true;
            Position(preset.nameText.rectTransform, 64, 5, 172, 27, 330, 66);
            Position(preset.latestMessage.rectTransform, 64, 35, 252, 24, 330, 66);
            Position(preset.timeText.rectTransform, 243, 8, 78, 20, 330, 66);
            PrepareText(preset.nameText, Ink, 16); PrepareText(preset.latestMessage, Subtle, 13); PrepareText(preset.timeText, Subtle, 11);
            foreach (var localized in go.GetComponentsInChildren<LocalizedObject>(true)) Object.DestroyImmediate(localized);
            foreach (var button in go.GetComponentsInChildren<ButtonManager>(true))
            { button.onClick = new UnityEvent(); button.onDoubleClick = new UnityEvent(); button.useLocalization = false; button.useRipple = button.useHoverEffect = false; }
            Layer(go.transform);
        }

        private sealed class Builder
        {
            private readonly UIManager theme;
            private readonly GameObject button, input, text, panel;
            public Builder(UIManager theme, GameObject button, GameObject input, GameObject row)
            { this.theme = theme; this.button = button; this.input = input; text = row.transform.Find("Title").gameObject; panel = button.transform.Find("Normal/Background").gameObject; }
            private GameObject Clone(GameObject source, Transform parent, string name)
            {
                var go = Object.Instantiate(source, parent, false); go.name = name;
                if (PrefabUtility.IsPartOfPrefabInstance(go)) PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var localized in go.GetComponentsInChildren<LocalizedObject>(true)) Object.DestroyImmediate(localized);
                foreach (var element in go.GetComponentsInChildren<UIManagerElement>(true))
                { element.themeManagerAsset = theme; element.UpdateElement(); if (element.objectType == UIManagerElement.ObjectType.Text) element.useCustomFont = true; }
                Layer(go.transform); return go;
            }
            public void Surface(Transform parent, string name, float x, float y, float w, float h, float pw, float ph, Color top, Color bottom)
            {
                var go = Clone(panel, parent, name); Position(go.GetComponent<RectTransform>(), x, y, w, h, pw, ph);
                var image = go.GetComponent<Image>(); RemoveColorDriver(image); image.color = Color.white; image.raycastTarget = false;
                var gradient = go.GetComponent<UIGradient>() ?? go.AddComponent<UIGradient>();
                gradient.direction = UIGradient.Direction.Vertical; gradient.color1 = top; gradient.color2 = bottom;
                go.transform.SetAsFirstSibling();
            }
            public TMP_Text Text(Transform parent, string value, float x, float y, float w, float h, float size, float pw, float ph)
            {
                var go = Clone(text, parent, "YY2010 " + value); Position(go.GetComponent<RectTransform>(), x, y, w, h, pw, ph);
                var label = go.GetComponent<TMP_Text>(); PrepareText(label, Ink, Mathf.Min(size, (h - 1) / 1.6f)); label.text = value;
                return label;
            }
            public Image Avatar(GameObject nativeLayout, Transform parent, string name, Sprite sprite, float x, float y, float w, float h, float pw, float ph)
            {
                var source = nativeLayout.transform.Find("Profile").gameObject;
                var go = Clone(source, parent, name); go.SetActive(true); Position(go.GetComponent<RectTransform>(), x, y, w, h, pw, ph);
                var images = go.GetComponentsInChildren<Image>(true); Image target = images.Length > 1 ? images[1] : images[0];
                RemoveColorDriver(target); target.sprite = sprite; target.color = Color.white; target.preserveAspect = true;
                return target;
            }
            public ButtonManager Button(Transform parent, string name, string caption, float x, float y, float w, float h, float pw, float ph)
            {
                var go = Clone(button, parent, name); Position(go.GetComponent<RectTransform>(), x, y, w, h, pw, ph);
                var result = go.GetComponent<ButtonManager>(); Prepare(result, caption); return result;
            }
            public void Prepare(ButtonManager result, string caption)
            {
                result.buttonText = caption; result.textSize = 16; result.autoFitContent = false; result.enableIcon = false;
                result.enableText = true;
                result.useCustomContent = false; result.useLocalization = false; result.useUINavigation = false;
                result.useSounds = result.useRipple = result.useHoverEffect = false; result.checkForDoubleClick = false;
                result.padding.top = result.padding.bottom = 0; result.padding.left = result.padding.right = 8;
                result.onClick = new UnityEvent(); result.onDoubleClick = new UnityEvent(); result.UpdateUI();
                foreach (var fit in result.GetComponentsInChildren<ContentSizeFitter>(true)) fit.enabled = false;
                foreach (var layout in result.GetComponentsInChildren<HorizontalLayoutGroup>(true))
                { layout.padding = new RectOffset(8, 8, 0, 0); layout.childControlHeight = true; layout.childForceExpandHeight = true; }
                foreach (var label in result.GetComponentsInChildren<TMP_Text>(true))
                {
                    PrepareText(label, Ink, 16); label.text = caption; label.alignment = TextAlignmentOptions.Center;
                    var element = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>(); element.minHeight = element.preferredHeight = 26;
                }
            }
            public TMP_InputField Input(Transform parent, string name, string hint, float x, float y, float w, float h, float pw, float ph)
            {
                var go = Clone(input, parent, name); Position(go.GetComponent<RectTransform>(), x, y, w, h, pw, ph);
                var field = go.GetComponent<TMP_InputField>(); field.richText = false; field.SetTextWithoutNotify(string.Empty);
                var placeholder = go.transform.Find("Placeholder")?.GetComponent<TMP_Text>();
                if (placeholder != null) { placeholder.text = hint; PrepareText(placeholder, Subtle, 14); }
                if (field.textComponent != null) PrepareText(field.textComponent, Ink, 14);
                foreach (var bm in go.GetComponentsInChildren<ButtonManager>(true)) { bm.useLocalization = false; bm.useRipple = bm.useHoverEffect = false; }
                return field;
            }
        }
        private static void PrepareText(TMP_Text text, Color color, float size)
        {
            var element = text.GetComponent<UIManagerElement>(); if (element != null) { element.useCustomColor = true; element.useCustomFont = true; }
            text.color = color; text.fontSize = size; text.richText = false; text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Overflow; text.margin = Vector4.zero;
            text.alignment = TextAlignmentOptions.TopLeft; text.raycastTarget = false;
        }
        private static void RemoveColorDriver(Image image)
        { var element = image.GetComponent<UIManagerElement>(); if (element != null) Object.DestroyImmediate(element); }
        private static void Position(RectTransform rect, float x, float y, float w, float h, float pw, float ph)
        {
            rect.anchorMin = new Vector2(x / pw, 1 - (y + h) / ph); rect.anchorMax = new Vector2((x + w) / pw, 1 - y / ph);
            rect.pivot = new Vector2(.5f, .5f); rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one;
        }
        private static void SetSize(RectTransform rect, float width, float height, float dx, float dy)
        {
            var parent = rect.parent as RectTransform; float pw = parent.rect.width, ph = parent.rect.height;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            var low = new Vector2((pw - width) * .5f + dx, (ph - height) * .5f + dy);
            rect.offsetMin = low; rect.offsetMax = low + new Vector2(width - pw, height - ph);
        }
        private static Sprite Panda()
        {
            AssetDatabase.ImportAsset(PandaPath);
            var importer = AssetImporter.GetAtPath(PandaPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("红围巾熊猫 PNG 缺失。");
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            { importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.maxTextureSize = 2048; importer.SaveAndReimport(); }
            return Required<Sprite>(PandaPath);
        }
        private static UIManager Theme()
        {
            EnsureFolder(Owned); string path = Owned + "/YY 2010 Theme.asset";
            var theme = AssetDatabase.LoadAssetAtPath<UIManager>(path);
            if (theme == null) { theme = Object.Instantiate(Required<UIManager>("Assets/HongmengOS/Aero2010/Theme/Aero 2010 UI.asset")); AssetDatabase.CreateAsset(theme, path); }
            theme.enableDynamicUpdate = false; theme.enableLocalization = false; theme.primaryColorDark = Ink; theme.secondaryColorDark = Subtle;
            theme.windowBGColorDark = Color.white; theme.backgroundColorDark = new Color32(226, 241, 251, 255);
            theme.selectedTheme = UIManager.SelectedTheme.Custom; theme.highlightedColorCustom = Blue; theme.highlightedColorSecondaryCustom = Color.white;
            EditorUtility.SetDirty(theme); AssetDatabase.SaveAssetIfDirty(theme); return theme;
        }
        private static void Validate(YY2010ShellController shell)
        {
            if (shell.Manager == null || shell.ChatBinding == null || shell.FriendsWindow == null || shell.FriendList == null
                || shell.SearchInput == null || shell.SendChatButton == null || shell.ChatBinding.Window == shell.FriendsWindow
                || shell.ChatBinding.NativeChat.Manager != shell.Manager)
                throw new InvalidOperationException("YY 2010 原生引用验证失败。");
        }
        private static void Layer(Transform root) { foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5; }
        private static T Find<T>(Scene scene) where T : Component
        { foreach (var root in scene.GetRootGameObjects()) { var result = root.GetComponentInChildren<T>(true); if (result != null) return result; } return null; }
        private static T Required<T>(string path) where T : Object
        { var value = AssetDatabase.LoadAssetAtPath<T>(path); if (value == null) throw new InvalidOperationException("缺少资源：" + path); return value; }
        private static void EnsureFolder(string path)
        { if (AssetDatabase.IsValidFolder(path)) return; int slash = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, slash)); AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1)); }
    }
}
#endif
