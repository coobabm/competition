using TMPro;
using UnityEngine;

namespace LingGuangV05.Runtime
{
    // UI-only localization; no dialogue, inference or save-state ownership.
    public sealed class AiContentFooter : MonoBehaviour
    {
        void OnEnable() { GameText.Changed += Refresh; Refresh(); }
        void OnDisable() { GameText.Changed -= Refresh; }
        void Refresh() { var label = GetComponent<TMP_Text>(); if (label != null) label.text = AiContentNotice.Footer(GameText.IsEnglish); }
    }
}
