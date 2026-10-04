using LingGuangV05.Runtime;
using UnityEngine;
using Michsky.DreamOS;

namespace LingGuangV05.Desktop
{
    /// <summary>Decorates native records. Only explicitly bound authored content is localized.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ChatMessagePreset))]
    public sealed class YY2010MessageStamp : MonoBehaviour
    {
        public ChatMessagePreset Preset;
        public string Author = "老周";
        private string _stamped;
        private string _nativeTime;
        private string _authoredContent;
        private bool _subscribed;
        public bool IsStamped => _stamped != null;

        private void OnEnable()
        {
            _stamped = null;
            EnsureLocaleSubscription();
        }
        private void EnsureLocaleSubscription()
        {
            if (_subscribed) return;
            _subscribed = true;
            GameText.Changed += OnLanguageChanged;
        }
        private void OnDestroy() { if (_subscribed) GameText.Changed -= OnLanguageChanged; }
        private void LateUpdate() { ApplyStamp(); }

        /// <summary>Called for preset history/NPC replies, never for messages typed by the player.</summary>
        public void BindAuthoredContent(string source)
        {
            _authoredContent = source;
            EnsureLocaleSubscription();
            RenderAuthoredContent();
        }

        private void RenderAuthoredContent()
        {
            if (Preset == null) Preset = GetComponent<ChatMessagePreset>();
            if (_authoredContent == null || Preset == null || Preset.contentText == null) return;
            Preset.contentText.richText = false;
            Preset.contentText.text = GameText.Source(_authoredContent);
        }

        public void RefreshLanguage() { OnLanguageChanged(); }

        private void OnLanguageChanged()
        {
            // Intentionally stays subscribed while disabled: immutable records do not need Update,
            // but hidden/reopened history must still follow the selected language.
            if (Preset != null && Preset.timeText != null && _nativeTime != null && Preset.timeText.text == _stamped)
                Preset.timeText.text = _nativeTime; // Preserve the original native clock string across switches.
            _stamped = null;
            RenderAuthoredContent();
            ApplyStamp();
        }

        /// <summary>Localizes only the native 12-hour clock suffix; digits/date/user strings are untouched.</summary>
        public static string LocalizeTimeMarker(string nativeTime)
        {
            string value = RestoreNativeTimeMarker(nativeTime ?? "");
            if (GameText.IsEnglish) return value;
            if (value.EndsWith(" AM", System.StringComparison.Ordinal)) return value.Substring(0, value.Length - 3) + " 上午";
            if (value.EndsWith(" PM", System.StringComparison.Ordinal)) return value.Substring(0, value.Length - 3) + " 下午";
            return value;
        }

        private static string RestoreNativeTimeMarker(string value)
        {
            if (value.EndsWith(" 上午", System.StringComparison.Ordinal)) return value.Substring(0, value.Length - 3) + " AM";
            if (value.EndsWith(" 下午", System.StringComparison.Ordinal)) return value.Substring(0, value.Length - 3) + " PM";
            return value;
        }

        public void ApplyStamp()
        {
            EnsureLocaleSubscription();
            if (Preset == null) Preset = GetComponent<ChatMessagePreset>();
            if (Preset == null || Preset.timeText == null) { enabled = false; return; }
            string value = Preset.timeText.text ?? "";
            if (_stamped == value) { enabled = false; return; }
            string author = string.IsNullOrWhiteSpace(Author) ? "老周" : Author.Replace("\r", "").Replace("\n", "").Trim();
            if (author.Length > 16) author = author.Substring(0, 16);
            string localizedAuthor = author == "老周" ? GameText.T("老周", "Lao Zhou") : author == "我" ? GameText.T("我", "Me") : author;
            // Strip only known stamp prefixes, never timestamps or player-authored message text.
            string[] prefixes = { author + "  ", localizedAuthor + "  ", "我  ", "老周  ", "Me  ", "Lao Zhou  " };
            for (int i = 0; i < 4; i++)
            {
                bool removed = false;
                foreach (string prefix in prefixes)
                    if (value.StartsWith(prefix, System.StringComparison.Ordinal))
                    { value = value.Substring(prefix.Length); removed = true; break; }
                if (!removed) break;
            }
            _nativeTime = RestoreNativeTimeMarker(value);
            _stamped = localizedAuthor + "  " + LocalizeTimeMarker(_nativeTime);
            Preset.timeText.richText = false;
            Preset.timeText.text = _stamped;
            if (Preset.contentText != null) Preset.contentText.richText = false;
            enabled = false;
        }
    }
}
