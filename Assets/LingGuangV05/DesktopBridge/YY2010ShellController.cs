using System;
using System.Collections;
using System.Collections.Generic;
using Michsky.DreamOS;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop
{
    /// <summary>
    /// Behavior only for builder-owned native buddy/chat windows. No UI objects, graphics or
    /// conversations are constructed here; MessagingManager remains the native send/receive owner.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class YY2010ShellController : MonoBehaviour
    {
        public ChapterOneDesktopBridge ChatBinding;
        public WindowManager FriendsWindow;
        public MessagingManager Manager;
        public TMP_InputField SearchInput;
        public RectTransform FriendList;
        public Sprite PandaAvatar;
        public ButtonManager FriendsTab, GroupsTab, RecentTab, CloseChatButton, SendChatButton, EmojiButton, HistoryButton;
        public TMP_Text[] ShellLabels = Array.Empty<TMP_Text>();
        public UnityEngine.UI.Image[] AvatarImages = Array.Empty<UnityEngine.UI.Image>();
        public TMP_Text EmptyStateText;

        private readonly List<Row> _rows = new List<Row>();
        private readonly List<EventHook> _hooks = new List<EventHook>();
        private readonly HashSet<string> _recent = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<ChatLayoutPreset, int> _messageCounts = new Dictionary<ChatLayoutPreset, int>();
        private MessagingManager _hookedManager;
        private WindowManager _hookedChatWindow;
        private TMP_InputField _hookedSearch;
        private bool _initialized, _disposed;
        private string _activeTab = "friends", _query = "";
        private int _lastContactCount = -1, _lastLayoutCount = -1;
        private int _visibilityRecoveryChecks, _visibilityRecoveryCount;
        private float _nextScan;
        private Coroutine _historyRoutine, _caretRoutine;

        public bool IsReady => !_disposed && _initialized && Manager != null && Manager.createdLayoutPresets != null && Manager.createdLayoutPresets.Count > 0 && _rows.Count > 0;
        public string ActiveTab => _activeTab;
        public int VisibleContactCount { get { int count = 0; foreach (Row row in _rows) if (row.preset != null && row.preset.gameObject.activeSelf) count++; return count; } }
        public int RecentContactCount => _recent.Count;
        public int VisibilityRecoveryCount => _visibilityRecoveryCount;
        public UnityEngine.UI.ScrollRect HistoryScroll => ResolveHistoryScroll();

        private void Start() { EnsureInitialized(); }

        public void EnsureInitialized()
        {
            if (_disposed) return;
            if (!_initialized)
            {
                Hook(FriendsTab, () => SelectTab("friends"));
                Hook(GroupsTab, () => SelectTab("groups"));
                Hook(RecentTab, () => SelectTab("recent"));
                Hook(EmojiButton, () => InsertEmoticon("(^_^)"));
                Hook(HistoryButton, ScrollHistoryToTop);
                if (CloseChatButton != null && !HasNativeCloseBinding(CloseChatButton)) Hook(CloseChatButton, CloseChat);
                // SendChatButton deliberately untouched: preserve its original native
                // MessagingManager.CreateMessageFromInput binding, including Enter behavior.
                if (SearchInput != null)
                {
                    ChapterOneNativeChatAdapter.RefreshNativeInputPlaceholder(SearchInput);
                    SearchInput.characterLimit = 64;
                    SearchInput.richText = false;
                    if (SearchInput.textComponent != null) SearchInput.textComponent.richText = false;
                    SearchInput.onValueChanged.AddListener(ApplySearch);
                    _hookedSearch = SearchInput;
                    _query = SearchInput.text ?? "";
                }
                GameText.Changed += OnLanguageChanged;
                _initialized = true;
            }
            if (ChatBinding != null && ChatBinding.NativeChat != null) ChatBinding.NativeChat.EnsureInitialized();
            if (Manager != _hookedManager)
            {
                if (_hookedManager != null) _hookedManager.externalEvents.RemoveListener(OnNativeMessage);
                _hookedManager = Manager;
                if (_hookedManager != null) _hookedManager.externalEvents.AddListener(OnNativeMessage);
                _lastContactCount = -1; _lastLayoutCount = -1;
            }
            WindowManager chatWindow = ChatBinding != null ? ChatBinding.Window : null;
            if (chatWindow != _hookedChatWindow)
            {
                if (_hookedChatWindow != null) _hookedChatWindow.onOpen.RemoveListener(OnChatOpened);
                _hookedChatWindow = chatWindow;
                if (_hookedChatWindow != null) _hookedChatWindow.onOpen.AddListener(OnChatOpened);
            }
            PrepareShellLabels();
            ApplyPandaToBuilderAvatars();
            DiscoverNativeRowsAndLayouts();
            ApplyFilters();
        }

        private void Update()
        {
            if (_disposed || Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + .25f;
            if (!_initialized || _hookedManager != Manager) { EnsureInitialized(); return; }
            DiscoverNativeRowsAndLayouts();
            ObserveNativeMessages();
            // Bounded post-open checks allow the native Show coroutine/animation to run first.
            // No continuous override of native animation state or closed/non-selected layouts.
            if (_visibilityRecoveryChecks > 0)
            {
                _visibilityRecoveryChecks--;
                if (RecoverSelectedConversationVisibility()) _visibilityRecoveryChecks = 0;
            }
        }

        public void OpenBuddy()
        {
            EnsureInitialized();
            if (_disposed || FriendsWindow == null) return;
            FriendsWindow.OpenWindow();
            PrepareShellLabels(); ApplyFilters();
        }

        public void CloseBuddy()
        {
            if (FriendsWindow != null && FriendsWindow.isOn) FriendsWindow.CloseWindow();
        }

        public void OpenChat()
        {
            EnsureInitialized();
            if (_disposed || ChatBinding == null) return;
            ChatBinding.OpenApp("chat");
            MarkCurrentRecent();
            ClearCurrentUnread();
            ApplyPandaToNativeLayouts();
            _visibilityRecoveryChecks = 8;
        }

        public void CloseChat()
        {
            if (ChatBinding != null) ChatBinding.CloseApp();
        }

        public void SelectTab(string tab)
        {
            if (_disposed || (tab != "friends" && tab != "groups" && tab != "recent")) return;
            _activeTab = tab;
            ApplyFilters();
        }

        public void ApplySearch(string query)
        {
            if (_disposed) return;
            _query = query ?? "";
            if (_query.Length > 64) _query = _query.Substring(0, 64);
            ApplyFilters();
        }

        private void OnLanguageChanged()
        {
            if (_disposed) return;
            ChapterOneNativeChatAdapter.RefreshNativeInputPlaceholder(SearchInput);
            ApplyFilters();
            ChatBinding?.NativeChat?.RefreshLocalizedConversation();
        }

        private void ApplyFilters()
        {
            string key = _query.Trim().ToLowerInvariant();
            int visible = 0;
            foreach (Row row in _rows)
            {
                if (row.preset == null) continue;
                string label = row.preset.nameText == null ? row.preset.name : row.preset.nameText.text;
                string searchable = (row.preset.name + " " + label + (row.preset.name.Contains("老周") ? " laozhou lao zhou" : "")).ToLowerInvariant();
                bool show = _activeTab != "groups" && (_activeTab != "recent" || _recent.Contains(row.preset.name)) && (key.Length == 0 || searchable.Contains(key));
                if (row.preset.gameObject.activeSelf != show) row.preset.gameObject.SetActive(show);
                if (show) visible++;
            }
            SetTabInteractable(FriendsTab, _activeTab != "friends");
            SetTabInteractable(GroupsTab, _activeTab != "groups");
            SetTabInteractable(RecentTab, _activeTab != "recent");
            if (EmptyStateText != null)
            {
                EmptyStateText.richText = false;
                EmptyStateText.text = _activeTab == "groups" ? Lang.T("第一章暂无群聊。\n没有接入网络群服务。") : _activeTab == "recent" && _recent.Count == 0 ? Lang.T("本次还没有最近会话。\n双击老周开始聊天。") : _rows.Count == 0 ? Lang.T("正在等待本地联系人…") : Lang.T("未找到匹配的联系人。");
                EmptyStateText.gameObject.SetActive(visible == 0);
                ApplyFont(EmptyStateText);
            }
        }

        private void DiscoverNativeRowsAndLayouts()
        {
            if (FriendList != null && FriendList.childCount != _lastContactCount)
            {
                _lastContactCount = FriendList.childCount;
                ReleaseRowHooks();
                foreach (ChatItemPreset preset in FriendList.GetComponentsInChildren<ChatItemPreset>(true))
                {
                    ButtonManager button = preset.GetComponent<ButtonManager>();
                    var row = new Row { preset = preset, button = button, originalDoubleClick = button != null && button.checkForDoubleClick };
                    if (button != null)
                    {
                        row.doubleClick = () => OpenChatFromRow(preset);
                        button.checkForDoubleClick = true;
                        button.onDoubleClick.AddListener(row.doubleClick);
                    }
                    _rows.Add(row);
                    PrepareRow(preset);
                }
                ApplyFilters();
            }
            int layouts = Manager != null && Manager.createdLayoutPresets != null ? Manager.createdLayoutPresets.Count : 0;
            if (layouts == _lastLayoutCount) return;
            _lastLayoutCount = layouts;
            if (Manager == null) return;
            ApplyPandaToNativeLayouts();
            foreach (ChatLayoutPreset layout in Manager.createdLayoutPresets)
            {
                if (layout == null || layout.messageParent == null) continue;
                if (!_messageCounts.ContainsKey(layout))
                {
                    _messageCounts[layout] = layout.messageParent.childCount;
                    PrepareNativeMessages(layout, 0);
                }
            }
        }

        private void OpenChatFromRow(ChatItemPreset row)
        {
            if (row == null) return;
            _recent.Add(row.name);
            OpenChat(); // native single-click selection listener remains intact
            row.EnableNotificationBadge(false);
            ApplyFilters();
        }

        private void OnChatOpened()
        {
            DiscoverNativeRowsAndLayouts(); MarkCurrentRecent(); ClearCurrentUnread(); ApplyPandaToNativeLayouts();
            _visibilityRecoveryChecks = 8;
        }

        /// <summary>
        /// Completes only an interrupted native "In" endpoint for the currently selected open chat.
        /// Native ChatLayoutPreset can disable its Animator before the clip leaves its initial
        /// alpha/scale plateau under a low-frame-rate Editor. Never fight a running Animator.
        /// </summary>
        public bool RecoverSelectedConversationVisibility()
        {
            if (_disposed || ChatBinding == null || !ChatBinding.IsOpen || Manager == null || Manager.selectedLayout == null) return false;
            ChatLayoutPreset selected = Manager.selectedLayout;
            if (!selected.gameObject.activeInHierarchy) return false;
            Animator animator = selected.GetComponent<Animator>();
            CanvasGroup group = selected.GetComponent<CanvasGroup>();
            if (animator == null || animator.enabled || group == null) return false;
            int inState = Animator.StringToHash("In");
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            bool interruptedIn = current.shortNameHash == inState && current.normalizedTime < .999f;
            if (!interruptedIn && group.alpha >= .999f) return false;
            if (animator.runtimeAnimatorController != null && animator.HasState(0, inState))
            {
                animator.enabled = true;
                try
                {
                    // Apply the authored native endpoint, including its scale and position.
                    // Merely forcing alpha would leave the record column at the .75-scale plateau.
                    animator.Play("In", 0, 1f);
                    animator.Update(0);
                }
                finally { animator.enabled = false; }
            }
            if (ChatBinding.IsOpen && Manager.selectedLayout == selected && selected.gameObject.activeInHierarchy)
            {
                if (group.alpha < .999f) group.alpha = 1;
                _visibilityRecoveryCount++;
                return true;
            }
            return false;
        }

        private void OnNativeMessage()
        {
            if (_disposed) return;
            MarkCurrentRecent();
            foreach (Row row in _rows) if (row.preset != null) PrepareRow(row.preset);
            ObserveNativeMessages();
        }

        private void ObserveNativeMessages()
        {
            if (Manager == null || Manager.createdLayoutPresets == null) return;
            bool changed = false;
            foreach (ChatLayoutPreset layout in Manager.createdLayoutPresets)
            {
                if (layout == null || layout.messageParent == null) continue;
                int count = layout.messageParent.childCount;
                if (!_messageCounts.TryGetValue(layout, out int previous)) { _messageCounts[layout] = count; PrepareNativeMessages(layout, 0); continue; }
                if (count == previous) continue;
                PrepareNativeMessages(layout, count >= previous ? previous : 0);
                _messageCounts[layout] = count;
                if (count > previous) _recent.Add(layout.name);
                changed = true;
            }
            if (changed)
            {
                foreach (Row row in _rows) if (row.preset != null) PrepareRow(row.preset);
                ApplyFilters();
            }
        }

        private void PrepareNativeMessages(ChatLayoutPreset layout, int first)
        {
            for (int i = first; i < layout.messageParent.childCount; i++)
            {
                Transform child = layout.messageParent.GetChild(i);
                YY2010MessageStamp stamp = child.GetComponent<YY2010MessageStamp>();
                if (stamp != null) stamp.ApplyStamp();
                ChatMessagePreset preset = child.GetComponent<ChatMessagePreset>();
                if (preset == null) continue;
                if (preset.contentText != null) preset.contentText.richText = false;
                ApplyFont(preset.contentText); ApplyFont(preset.timeText);
            }
        }

        private void PrepareRow(ChatItemPreset row)
        {
            if (row.latestMessage != null) row.latestMessage.richText = false;
            if (PandaAvatar != null && row.coverImage != null) { row.coverImage.sprite = PandaAvatar; row.coverImage.preserveAspect = true; }
            ApplyFont(row.nameText); ApplyFont(row.latestMessage); ApplyFont(row.timeText);
            foreach (TMP_Text label in row.GetComponentsInChildren<TMP_Text>(true)) ApplyFont(label);
        }

        private void ApplyPandaToBuilderAvatars()
        {
            if (PandaAvatar == null || AvatarImages == null) return;
            foreach (UnityEngine.UI.Image avatar in AvatarImages)
                if (avatar != null) { avatar.sprite = PandaAvatar; avatar.preserveAspect = true; }
        }

        private void ApplyPandaToNativeLayouts()
        {
            if (Manager == null || PandaAvatar == null) return;
            foreach (MessagingManager.ChatItem contact in Manager.chatList) if (contact.chatTitle == "老周") contact.individualPicture = PandaAvatar;
            foreach (ChatLayoutPreset layout in Manager.createdLayoutPresets)
            {
                if (layout == null || layout.name != "老周") continue;
                layout.personPicture = PandaAvatar;
                if (layout.individualImage != null) { layout.individualImage.sprite = PandaAvatar; layout.individualImage.preserveAspect = true; }
                ApplyFont(layout.nameText);
            }
        }

        private void MarkCurrentRecent()
        {
            if (Manager != null && Manager.selectedLayout != null) _recent.Add(Manager.selectedLayout.name);
            ApplyFilters();
        }

        private void ClearCurrentUnread()
        {
            string title = Manager != null && Manager.selectedLayout != null ? Manager.selectedLayout.name : "老周";
            foreach (Row row in _rows) if (row.preset != null && row.preset.name == title) row.preset.EnableNotificationBadge(false);
        }

        public void InsertEmoticon(string emoticon)
        {
            if (_disposed || Manager == null || Manager.messageInput == null || string.IsNullOrEmpty(emoticon)) return;
            var token = new System.Text.StringBuilder(16);
            foreach (char c in emoticon) if (c >= 32 && c <= 126 && token.Length < 16) token.Append(c);
            if (token.Length == 0) return;
            TMP_InputField input = Manager.messageInput;
            string original = input.text ?? "";
            int first = Mathf.Clamp(Math.Min(input.selectionStringAnchorPosition, input.selectionStringFocusPosition), 0, original.Length);
            int last = Mathf.Clamp(Math.Max(input.selectionStringAnchorPosition, input.selectionStringFocusPosition), first, original.Length);
            if (first > 0 && first < original.Length && char.IsLowSurrogate(original[first]) && char.IsHighSurrogate(original[first - 1])) first--;
            if (last > 0 && last < original.Length && char.IsLowSurrogate(original[last]) && char.IsHighSurrogate(original[last - 1])) last++;
            int limit = input.characterLimit > 0 ? input.characterLimit : 160;
            int room = Math.Max(0, limit - (original.Length - (last - first)));
            if (room == 0) return;
            string addition = token.ToString(0, Math.Min(token.Length, room));
            input.richText = false;
            input.text = original.Substring(0, first) + addition + original.Substring(last);
            int caret = first + addition.Length;
            input.selectionStringAnchorPosition = caret; input.selectionStringFocusPosition = caret;
            if (input.gameObject.activeInHierarchy)
            {
                input.ActivateInputField();
                // TMP may select all on deferred focus. Restore the insertion caret only if the
                // player has not edited the draft meanwhile; do not change native input settings.
                if (_caretRoutine != null) StopCoroutine(_caretRoutine);
                _caretRoutine = StartCoroutine(RestoreInsertionCaret(input, input.text, caret));
            }
        }

        private IEnumerator RestoreInsertionCaret(TMP_InputField input, string expected, int caret)
        {
            yield return null;
            if (!_disposed && input != null && input.isFocused && input.text == expected)
            { input.selectionStringAnchorPosition = caret; input.selectionStringFocusPosition = caret; }
            _caretRoutine = null;
        }

        private static void SetTabInteractable(ButtonManager button, bool value)
        {
            if (button != null && button.isInteractable != value) button.Interactable(value);
        }

        public void ScrollHistoryToTop()
        {
            if (_disposed) return;
            UnityEngine.UI.ScrollRect scroll = ResolveHistoryScroll();
            if (scroll == null) return;
            SetScrollTop(scroll);
            if (_historyRoutine != null) StopCoroutine(_historyRoutine);
            _historyRoutine = StartCoroutine(ScrollAfterNativeLayout(scroll));
        }

        private UnityEngine.UI.ScrollRect ResolveHistoryScroll()
        {
            if (Manager == null || Manager.selectedLayout == null) return null;
            UnityEngine.UI.ScrollRect scroll = Manager.selectedLayout.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            if (scroll == null && Manager.selectedLayout.messageParent != null) scroll = Manager.selectedLayout.messageParent.GetComponentInParent<UnityEngine.UI.ScrollRect>(true);
            return scroll;
        }

        private IEnumerator ScrollAfterNativeLayout(UnityEngine.UI.ScrollRect scroll)
        {
            yield return null;
            if (!_disposed && scroll != null) SetScrollTop(scroll);
            _historyRoutine = null;
        }

        private static void SetScrollTop(UnityEngine.UI.ScrollRect scroll)
        {
            Canvas.ForceUpdateCanvases(); scroll.StopMovement(); scroll.verticalNormalizedPosition = 1;
        }

        private void PrepareShellLabels()
        {
            if (ShellLabels != null) foreach (TMP_Text label in ShellLabels) ApplyFont(label);
            ApplyFont(EmptyStateText);
            if (SearchInput != null) { ApplyFont(SearchInput.textComponent); if (SearchInput.placeholder is TMP_Text placeholder) ApplyFont(placeholder); }
        }

        private void ApplyFont(TMP_Text label)
        {
            if (label != null && ChatBinding != null && ChatBinding.NativeChat != null) ChatBinding.NativeChat.ApplyNativeFont(label);
        }

        private void Hook(ButtonManager button, UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(action); _hooks.Add(new EventHook { source = button.onClick, action = action });
        }

        private bool HasNativeCloseBinding(ButtonManager button)
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentMethodName(i) == "CloseWindow" && ChatBinding != null && button.onClick.GetPersistentTarget(i) == ChatBinding.Window) return true;
            return false;
        }

        private void ReleaseRowHooks()
        {
            foreach (Row row in _rows)
                if (row.button != null) { if (row.doubleClick != null) row.button.onDoubleClick.RemoveListener(row.doubleClick); row.button.checkForDoubleClick = row.originalDoubleClick; }
            _rows.Clear();
        }

        private void OnDestroy() { Dispose(); }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            GameText.Changed -= OnLanguageChanged;
            if (_hookedManager != null) _hookedManager.externalEvents.RemoveListener(OnNativeMessage);
            if (_hookedChatWindow != null) _hookedChatWindow.onOpen.RemoveListener(OnChatOpened);
            if (_hookedSearch != null) _hookedSearch.onValueChanged.RemoveListener(ApplySearch);
            foreach (EventHook hook in _hooks) hook.source.RemoveListener(hook.action);
            _hooks.Clear(); ReleaseRowHooks(); _messageCounts.Clear(); _recent.Clear();
            if (_historyRoutine != null) StopCoroutine(_historyRoutine);
            if (_caretRoutine != null) StopCoroutine(_caretRoutine);
            _historyRoutine = null; _caretRoutine = null;
            _visibilityRecoveryChecks = 0;
        }

        private sealed class Row { public ChatItemPreset preset; public ButtonManager button; public UnityAction doubleClick; public bool originalDoubleClick; }
        private sealed class EventHook { public UnityEvent source; public UnityAction action; }
    }
}
