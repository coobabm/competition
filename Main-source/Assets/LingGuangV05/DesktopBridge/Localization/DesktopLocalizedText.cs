using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;

namespace LingGuangV05.Desktop
{
    /// <summary>Explicit authored-label binding. Never discovers or rewrites player input.</summary>
    [DisallowMultipleComponent]
    public sealed class DesktopLocalizedText : MonoBehaviour
    {
        public TMP_Text target;
        [TextArea] public string source;
        public ButtonManager button;
        private string lastOutput;
        private bool relinquished;
        private void OnEnable() { GameText.Changed += Apply; Apply(); }
        private void OnDisable() { GameText.Changed -= Apply; }
        public void Apply()
        {
            if (relinquished) return;
            string current = button != null ? button.buttonText : target != null ? target.text : null;
            string value = GameText.Source(source);
            // Runtime payload/presenter changed this slot: it is no longer an authored static label.
            if (Application.isPlaying && current != null && current != source && current != lastOutput && current != value
                && (!GameText.HasTranslation(current) || GameText.Source(current) != value))
            { relinquished = true; return; }
            lastOutput = value;
            if (button != null) { button.buttonText = value; button.UpdateUI(); }
            else if (target != null) target.text = value;
        }
    }
}
