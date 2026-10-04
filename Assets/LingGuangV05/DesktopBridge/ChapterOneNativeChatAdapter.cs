using System;
using System.Collections.Generic;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop
{
    /// <summary>
    /// Data/behavior adapter for the original DreamOS MessagingManager. Never constructs a view,
    /// changes native styles, or calls Manager.Initialize (native Awake owns preset construction).
    /// Keep this component on the active bridge holder, outside the native window hierarchy.
    /// </summary>
    public sealed class ChapterOneNativeChatAdapter : MonoBehaviour
    {
        public MessagingManager Manager;
        public ChapterOneRuntime Runtime;
        public WindowManager Window;
        public TMP_Text[] ShellLabels = Array.Empty<TMP_Text>();

        private const int MaxPendingReplies = 8;
        private const float ReplyDelaySeconds = 1f;
        private const string ContactTitle = "老周";
        private const string CjkProbe = "老周邮件灵光家庭寻宝开始训练显卡入学考试电费本地预设帮助你好";
        private readonly Queue<string> _pending = new Queue<string>(MaxPendingReplies);
        private readonly Dictionary<TMP_FontAsset, TMP_FontAsset> _fontClones = new Dictionary<TMP_FontAsset, TMP_FontAsset>();
        private readonly Dictionary<TMP_Text, FontBinding> _fontBindings = new Dictionary<TMP_Text, FontBinding>();
        private MessagingManager _hookedManager;
        private ChatLayoutPreset _conversation;
        private ChatItemPreset[] _contacts = Array.Empty<ChatItemPreset>();
        private TMP_FontAsset _cjkFallback;
        private bool _localeSubscribed;
        private bool _ready, _openRequested, _creatingReply, _shellPrepared, _overflowNoticePending, _disposed;
        private int _layoutIndex = -1, _styledChildCount, _droppedReplyCount;
        private float _nextReplyAt, _nextReadyCheck;

        public bool IsReady => !_disposed && _ready && Manager != null && _conversation != null;
        public int PendingReplyCount => _pending.Count + (_overflowNoticePending ? 1 : 0);
        public int DroppedReplyCount => _droppedReplyCount;

        public void EnsureInitialized()
        {
            if (_disposed) return;
            if (!_localeSubscribed) { _localeSubscribed = true; GameText.Changed += RefreshLocalizedConversation; }
            if (!_shellPrepared)
            {
                _shellPrepared = true;
                if (ShellLabels != null) for (int i = 0; i < ShellLabels.Length; i++) ApplyNativeFont(ShellLabels[i]);
            }
            if (Manager == null || Runtime == null) return;
            Runtime.EnsureInitialized();
            // Defense in depth: only the owned first-chapter conversation is in memory.
            Manager.saveMessageHistory = false;
            Manager.useLocalization = false;
            if (_hookedManager != null && _hookedManager != Manager)
            {
                _hookedManager.externalEvents.RemoveListener(OnNativeSend);
                _hookedManager = null; _ready = false; _conversation = null;
            }
            if (IsReady) return;
            _layoutIndex = -1;
            for (int i = 0; i < Manager.chatList.Count; i++)
                if (Manager.chatList[i].chatTitle == ContactTitle) { _layoutIndex = i; break; }
            if (_layoutIndex < 0 || Manager.messageInput == null || Manager.createdLayoutPresets == null) return;
            // An inactive native window may not have run Awake yet. Wait, never initialize twice.
            for (int i = 0; i < Manager.createdLayoutPresets.Count; i++)
                if (Manager.createdLayoutPresets[i] != null && Manager.createdLayoutPresets[i].name == ContactTitle)
                { _conversation = Manager.createdLayoutPresets[i]; break; }
            if (_conversation == null || _conversation.messageParent == null) return;

            RefreshNativeInputPlaceholder(Manager.messageInput);
            Manager.messageInput.characterLimit = NativeLaoZhouReplies.MaxInputLength;
            Manager.messageInput.richText = false;
            if (Manager.messageInput.textComponent != null) Manager.messageInput.textComponent.richText = false;
            if (Manager.messageInput.placeholder is TMP_Text placeholder) placeholder.richText = false;
            _contacts = Manager.GetComponentsInChildren<ChatItemPreset>(true);
            TMP_Text[] nativeTexts = Manager.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < nativeTexts.Length; i++) ApplyNativeFont(nativeTexts[i]);
            _styledChildCount = 0;
            PrepareNewNativeMessages();
            PrepareContacts();
            if (_hookedManager != Manager)
            {
                Manager.externalEvents.AddListener(OnNativeSend);
                _hookedManager = Manager;
            }
            _ready = true;
            BindAuthoredHistory();
            RefreshLocalizedConversation();
            if (_openRequested) ShowNativeConversation();
        }

        public void OpenConversation()
        {
            if (_disposed) return;
            _openRequested = true;
            EnsureInitialized();
            if (IsReady) ShowNativeConversation();
        }

        private void ShowNativeConversation()
        {
            if (!IsReady || !Manager.gameObject.activeInHierarchy) return;
            _openRequested = false;
            if (Manager.selectedLayout != null && Manager.selectedLayout != _conversation && Manager.selectedLayout.gameObject.activeInHierarchy)
                Manager.selectedLayout.Hide();
            Manager.currentLayout = _layoutIndex;
            Manager.selectedLayout = _conversation;
            Manager.latestPerson = Manager.chatList[_layoutIndex].individualName;
            _conversation.Show();
            RefreshLocalizedConversation();
            for (int i = 0; i < _contacts.Length; i++)
                if (_contacts[i] != null && _contacts[i].name == ContactTitle) _contacts[i].EnableNotificationBadge(false);
            PrepareNewNativeMessages();
        }

        private void Update()
        {
            if (_disposed) return;
            float now = Time.unscaledTime;
            if (!IsReady)
            {
                if (now < _nextReadyCheck) return;
                _nextReadyCheck = now + .25f;
                EnsureInitialized();
            }
            if (!IsReady || now < _nextReplyAt || (_pending.Count == 0 && !_overflowNoticePending)) return;
            string reply;
            if (_pending.Count > 0) reply = NativeLaoZhouReplies.BuildReply(_pending.Dequeue(), Runtime.Sim);
            else
            {
                _overflowNoticePending = false;
                reply = "消息来得有点快。请先看前面的回复，再问一个问题；这是本地预设帮助，不是实时 AI。";
            }
            _nextReplyAt = now + ReplyDelaySeconds;
            if (string.IsNullOrEmpty(reply)) return;
            _creatingReply = true;
            try
            {
                // Native received-bubble prefab, native layout/scroll/notification path. This
                // overload leaves the native input's unsent draft untouched, including on close.
                string nativeTime = Manager.GetTimeData() ?? DateTime.Now.ToString("HH:mm");
                int countBeforeReply = _conversation.messageParent.childCount;
                Manager.CreateCustomIndividualMessage(_conversation, GameText.Source(reply), nativeTime);
                if (_conversation.messageParent.childCount > countBeforeReply)
                {
                    Transform created = _conversation.messageParent.GetChild(_conversation.messageParent.childCount - 1);
                    YY2010MessageStamp stamp = created.GetComponent<YY2010MessageStamp>();
                    if (stamp != null) stamp.BindAuthoredContent(reply);
                }
            }
            finally
            {
                _creatingReply = false;
                PrepareNewNativeMessages();
                PrepareContacts();
                RefreshLocalizedConversation();
            }
        }

        private void OnNativeSend()
        {
            // Native CreateIndividualMessage invokes externalEvents too; never reply to our reply.
            PrepareNewNativeMessages();
            PrepareContacts();
            if (_creatingReply || !IsReady || Manager.selectedLayout != _conversation) return;
            // externalEvents runs after the self bubble but before native input restore/clear.
            string input = NativeLaoZhouReplies.NormalizeInput(Manager.messageInput.text);
            if (input.Length == 0) return;
            if (_pending.Count >= MaxPendingReplies)
            {
                _overflowNoticePending = true;
                _droppedReplyCount++;
                return;
            }
            bool wasEmpty = _pending.Count == 0 && !_overflowNoticePending;
            _pending.Enqueue(input);
            if (wasEmpty) _nextReplyAt = Time.unscaledTime + ReplyDelaySeconds;
        }

        private void PrepareNewNativeMessages()
        {
            if (_conversation == null || _conversation.messageParent == null) return;
            Transform parent = _conversation.messageParent;
            if (_styledChildCount > parent.childCount) _styledChildCount = 0;
            // Only appended native presets are visited; never sweep history every frame.
            for (int i = _styledChildCount; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                ChatMessagePreset preset = child.GetComponent<ChatMessagePreset>();
                if (preset != null && preset.contentText != null) preset.contentText.richText = false;
                TMP_Text[] labels = child.GetComponentsInChildren<TMP_Text>(true);
                for (int j = 0; j < labels.Length; j++) ApplyNativeFont(labels[j]);
            }
            _styledChildCount = parent.childCount;
        }

        private void BindAuthoredHistory()
        {
            // The native asset contains only authored history. Use its declared records, not text
            // heuristics, so even a player message identical to an NPC line remains untouched.
            if (_layoutIndex < 0 || _conversation == null || _conversation.messageParent == null) return;
            MessagingChat asset = Manager.chatList[_layoutIndex].chatAsset;
            if (asset == null) return;
            int offset = Manager.beginningIndicator != null ? 1 : 0;
            int count = Math.Min(asset.messageList.Count, _conversation.messageParent.childCount - offset);
            for (int i = 0; i < count; i++)
            {
                Transform record = _conversation.messageParent.GetChild(i + offset);
                var entry = asset.messageList[i];
                // A native compatibility seed is not a conversation message or timestamp.
                if (entry.objectType == MessagingChat.ObjectType.Date
                    && string.IsNullOrEmpty(entry.messageContent) && string.IsNullOrEmpty(entry.sentTime))
                { record.gameObject.SetActive(false); continue; }
                YY2010MessageStamp stamp = record.GetComponent<YY2010MessageStamp>();
                if (stamp != null) stamp.BindAuthoredContent(asset.messageList[i].messageContent);
            }
        }

        /// <summary>Refreshes owned contact chrome and mirrors the last actual record, never translating user input.</summary>
        public void RefreshLocalizedConversation()
        {
            if (_disposed || !IsReady) return;
            RefreshNativeInputPlaceholder(Manager.messageInput);
            _conversation.personName = GameText.T("老周", "Lao Zhou");
            if (_conversation.nameText != null) _conversation.nameText.text = _conversation.personName;
            string latest = null;
            Transform records = _conversation.messageParent;
            // Date separators may be appended after a record; use only actual message presets.
            for (int i = records.childCount - 1; i >= 0; i--)
            {
                Transform child = records.GetChild(i);
                YY2010MessageStamp stamp = child.GetComponent<YY2010MessageStamp>();
                ChatMessagePreset message = child.GetComponent<ChatMessagePreset>();
                if (stamp == null || message == null || message.contentText == null) continue;
                stamp.RefreshLanguage();
                latest = message.contentText.text;
                break;
            }
            foreach (ChatItemPreset contact in _contacts)
            {
                if (contact == null || contact.name != ContactTitle) continue;
                if (contact.nameText != null) contact.nameText.text = GameText.T("老周", "Lao Zhou");
                if (latest != null && contact.latestMessage != null) contact.latestMessage.text = latest;
            }
            PrepareContacts();
        }

        /// <summary>Repairs the existing prefab's missing TMP placeholder reference; no replacement input is created.</summary>
        public static void RefreshNativeInputPlaceholder(TMP_InputField input)
        {
            if (input == null) return;
            if (input.placeholder == null)
            {
                Transform sibling = input.transform.Find("Placeholder");
                if (sibling != null) input.placeholder = sibling.GetComponent<TMP_Text>();
            }
            // TMP now owns placeholder visibility on native value changes, including clearing on send.
            // The visual's GameObject is not toggled by our locale binder and the draft is never assigned.
            input.ForceLabelUpdate();
            if (input.placeholder != null) input.placeholder.enabled = string.IsNullOrEmpty(input.text);
        }

        private void PrepareContacts()
        {
            for (int i = 0; i < _contacts.Length; i++)
            {
                if (_contacts[i] == null) continue;
                if (_contacts[i].name == ContactTitle && _contacts[i].nameText != null)
                    _contacts[i].nameText.text = GameText.T("老周", "Lao Zhou");
                if (_contacts[i].latestMessage != null) _contacts[i].latestMessage.richText = false;
                ApplyNativeFont(_contacts[i].nameText);
                ApplyNativeFont(_contacts[i].latestMessage);
            }
        }

        public void ApplyNativeFont(TMP_Text label)
        {
            if (_disposed || label == null || label.font == null || _fontBindings.ContainsKey(label)) return;
            TMP_FontAsset original = label.font;
            // Read-only glyph check: never add characters to the original native font asset.
            if (original.HasCharacters(CjkProbe + label.text, out uint[] missing, true, false)) return;
            if (!_fontClones.TryGetValue(original, out TMP_FontAsset clone))
            {
                TMP_FontAsset fallback = GetCjkFallback();
                if (fallback == null) return;
                clone = Instantiate(original);
                clone.name = original.name + "_LingGuangNativeChatFallback";
                clone.hideFlags = HideFlags.DontSave;
                // The primary clone shares the original atlas/material for identical appearance.
                // Keep it read-only so new glyphs can only populate our owned fallback atlas.
                clone.atlasPopulationMode = AtlasPopulationMode.Static;
                clone.fallbackFontAssetTable = original.fallbackFontAssetTable == null
                    ? new List<TMP_FontAsset>() : new List<TMP_FontAsset>(original.fallbackFontAssetTable);
                clone.fallbackFontAssetTable.Add(fallback);
                _fontClones.Add(original, clone);
            }
            Material nativeMaterial = label.fontSharedMaterial;
            _fontBindings.Add(label, new FontBinding { font = original, material = nativeMaterial });
            label.font = clone;
            // Keep the exact original native text material (color/outline/etc.). Only missing
            // CJK glyphs use a fallback. Do not touch size, autosize, spacing, alignment or color.
            if (nativeMaterial != null) label.fontSharedMaterial = nativeMaterial;
        }

        private TMP_FontAsset GetCjkFallback()
        {
            if (_cjkFallback != null) return _cjkFallback;
            Font source = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if (source == null)
            {
                Debug.LogWarning("YY 本地帮助找不到 Noto CJK 字体资源；原生字体和界面保持不变。", this);
                return null;
            }
            _cjkFallback = TMP_FontAsset.CreateFontAsset(source, 48, 5,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            _cjkFallback.name = "LingGuangV05_NativeChat_CjkFallback";
            _cjkFallback.hideFlags = HideFlags.DontSave;
            _cjkFallback.isMultiAtlasTexturesEnabled = true;
            _cjkFallback.TryAddCharacters(CjkProbe);
            return _cjkFallback;
        }

        private void OnDestroy() { DisposeResources(); }

        /// <summary>
        /// Terminal, idempotent cleanup. Exposed for deterministic ownership verification without
        /// relying on EditMode to dispatch a non-ExecuteAlways component's play lifecycle callback.
        /// </summary>
        public void DisposeResources()
        {
            if (_disposed) return;
            _disposed = true;
            if (_localeSubscribed) { GameText.Changed -= RefreshLocalizedConversation; _localeSubscribed = false; }
            if (_hookedManager != null) _hookedManager.externalEvents.RemoveListener(OnNativeSend);
            foreach (KeyValuePair<TMP_Text, FontBinding> binding in _fontBindings)
            {
                if (binding.Key == null) continue;
                binding.Key.font = binding.Value.font;
                if (binding.Value.material != null) binding.Key.fontSharedMaterial = binding.Value.material;
            }
            foreach (TMP_FontAsset clone in _fontClones.Values)
            {
                if (clone == null) continue;
                // TMP_FontAsset.OnDestroy owns and destroys every referenced atlas and material,
                // even for Static assets. These primary-clone resources are borrowed native
                // resources: detach them before invoking the clone's destructor.
                clone.atlasTextures = Array.Empty<Texture2D>();
                clone.material = null;
                ReleaseOwned(clone);
            }
            if (_cjkFallback != null)
            {
                // This asset does own its atlas/material. Let TMP destroy them exactly once.
                ReleaseOwned(_cjkFallback);
            }
            _fontBindings.Clear();
            _fontClones.Clear();
            _cjkFallback = null;
            _pending.Clear();
            _overflowNoticePending = false;
            _hookedManager = null;
            _conversation = null;
            _contacts = Array.Empty<ChatItemPreset>();
            _ready = false;
            _openRequested = false;
        }

        private static void ReleaseOwned(UnityEngine.Object item)
        {
            if (Application.isPlaying) Destroy(item, .1f);
            else DestroyImmediate(item);
        }

        private struct FontBinding { public TMP_FontAsset font; public Material material; }
    }
}
