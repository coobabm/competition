using TMPro;
using UnityEngine;

namespace LingGuangV05.Runtime
{
    // Disclosure for the player; never part of character dialogue or model prompts.
    public static class AiContentNotice
    {
        public static string Footer(bool english) => english ? "AI-generated content · For reference only" : "AI 合成内容 · 仅供参考";

        // The caller reserves this band below replies and above input/actions; it never intercepts input.
        public static TMP_Text CreateFooter(RectTransform parent, TMP_FontAsset font, Color color, float bottom = 0, float height = 24)
        {
            var bar = new GameObject("AiContentNotice", typeof(RectTransform)).GetComponent<RectTransform>();
            bar.SetParent(parent, false); bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1, 0);
            bar.offsetMin = new Vector2(0, bottom); bar.offsetMax = new Vector2(0, bottom + height);
            var rect = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(bar, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12, 0); rect.offsetMax = new Vector2(-12, 0);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.color = color;
            BindFooter(label); return label;
        }

        public static void BindFooter(TMP_Text label)
        {
            label.fontSize = 12; label.enableAutoSizing = true; label.fontSizeMin = 10; label.fontSizeMax = 12;
            label.alignment = TextAlignmentOptions.Center; label.richText = false;
            label.textWrappingMode = TextWrappingModes.NoWrap; label.raycastTarget = false;
            label.text = Footer(GameText.IsEnglish);
            if (label.GetComponent<AiContentFooter>() == null) label.gameObject.AddComponent<AiContentFooter>();
        }
        public static string Title(bool english) => english ? "AI-generated content" : "AI 合成内容";
        public static string Short(bool english) => english
            ? "AI-generated replies · For reference only"
            : "AI 合成内容 · 生成回复仅供参考";
        public static string Description(bool english) => english
            ? "Includes a local AI model. Some conversations and answers are AI-generated and are for reference only."
            : "本游戏内置 AI 模型，部分对话和回答为 AI 合成内容，仅供参考。";
    }
}
