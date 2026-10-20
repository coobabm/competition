using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Taohuo
{
    /// <summary>Measures complete product copy before placing the following line.</summary>
    public sealed class TaohuoProductLayout : MonoBehaviour
    {
        RectTransform row, words, right, picture;
        LayoutElement layout;
        readonly List<TMP_Text> lines = new List<TMP_Text>();
        readonly List<string> lastText = new List<string>();
        float minimumHeight, left, rightInset, purchaseWidth, purchaseInset, pictureWidth, pictureHeight;
        float previousWidth = -1;

        public static void Attach(RectTransform row)
        {
            var words = (row.Find("Mid") ?? row.Find("Words")) as RectTransform;
            var right = row.Find("Right") as RectTransform;
            if (words == null || right == null || row.GetComponent<TaohuoProductLayout>() != null) return;
            row.gameObject.AddComponent<TaohuoProductLayout>().Initialize(row, words, right);
        }

        void Initialize(RectTransform owner, RectTransform textArea, RectTransform purchase)
        {
            row = owner; words = textArea; right = purchase;
            layout = row.GetComponent<LayoutElement>();
            minimumHeight = layout.preferredHeight;
            left = words.offsetMin.x; rightInset = -words.offsetMax.x;
            purchaseWidth = right.offsetMax.x - right.offsetMin.x;
            purchaseInset = -right.offsetMax.x;
            picture = row.Find("Picture") as RectTransform;
            if (picture != null) { pictureWidth = picture.rect.width; pictureHeight = picture.rect.height; }
            foreach (Transform child in words)
            {
                var text = child.GetComponent<TMP_Text>();
                if (text == null) continue;
                lines.Add(text); lastText.Add(null);
                text.enableAutoSizing = false;
                text.overflowMode = TextOverflowModes.Overflow;
                text.alignment = TextAlignmentOptions.TopLeft;
            }
        }

        void LateUpdate() { Refresh(); }

        public void Refresh()
        {
            if (row == null || row.rect.width <= 1) return;
            float width = row.rect.width;
            bool changed = Mathf.Abs(width - previousWidth) > .5f;
            for (int i = 0; i < lines.Count; i++)
                changed |= lastText[i] != lines[i].text;
            if (!changed) return;
            previousWidth = width;

            // Keep the image and purchase control together above the copy on narrow cards.
            bool stacked = width - left - rightInset < 260;
            float top = stacked ? Mathf.Max(150, pictureHeight + 24) : 10;
            words.anchorMin = Vector2.zero; words.anchorMax = Vector2.one;
            words.offsetMin = new Vector2(stacked ? 14 : left, 10);
            words.offsetMax = new Vector2(stacked ? -14 : -rightInset, -top);
            float textWidth = Mathf.Max(1, width - (stacked ? 28 : left + rightInset));
            float cursor = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                var text = lines[i]; lastText[i] = text.text;
                float height = Mathf.Ceil(Mathf.Max(text.fontSize * 1.35f,
                    text.GetPreferredValues(text.text, textWidth, Mathf.Infinity).y)) + 2;
                var rect = text.rectTransform;
                rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(0, -cursor - height);
                rect.offsetMax = new Vector2(0, -cursor);
                cursor += height + 4;
            }
            layout.preferredHeight = Mathf.Max(minimumHeight, top + cursor + 6);
            right.anchorMin = new Vector2(1, stacked ? 1 : 0); right.anchorMax = Vector2.one;
            right.offsetMin = new Vector2(-purchaseInset - purchaseWidth, stacked ? -top + 12 : 12);
            right.offsetMax = new Vector2(-purchaseInset, -12);
            if (picture != null)
            {
                picture.anchorMin = picture.anchorMax = new Vector2(0, 1);
                picture.offsetMin = new Vector2(14, -12 - pictureHeight);
                picture.offsetMax = new Vector2(14 + pictureWidth, -12);
            }
        }
    }
}
