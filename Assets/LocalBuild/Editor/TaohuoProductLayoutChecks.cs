#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using LingGuangV05.Core.Hardware;
using LingGuangV05.Desktop.Taohuo;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class TaohuoProductLayoutChecks
{
    public static void Run()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset");
        if (font == null) throw new InvalidOperationException("Missing shipped desktop font");
        int checks = 0;
        foreach (var gpu in HardwareCatalog.Listings(new DateTime(2016, 10, 20)))
            foreach (bool english in new[] { false, true })
                Check(font, new[] { english ? gpu.titleEn : gpu.title, "[包邮]  [淘货自营]  [7 天无理由]",
                    "算力 ×2.2 · 显存 8G · " + (english ? gpu.pitchEn : gpu.pitch),
                    "<color=#FFAA00>★★★★★</color> 包装很严实，就是旧机箱塞不下了，又买了个机箱。",
                    "问大家：能跑深度学习吗？ 答：亲，建议咨询专业人士哦～" }, ref checks);
        Check(font, new[] { "【准系统】机箱 + Z170 主板 + 600W 电源 + 16G 内存（不含显卡）",
            "[包邮] [装好再发]", "多 2 个显卡插槽。卡分开放，温度更低。机箱本身 45W。" }, ref checks);
        Debug.Log("TaohuoProductLayoutChecks PASS: " + checks + " product/width combinations; complete text, no overlap, resize stability.");
    }

    static void Check(TMP_FontAsset font, string[] copy, ref int checks)
    {
        var go = new GameObject("ProductLayoutFixture", typeof(RectTransform), typeof(LayoutElement));
        try
        {
            var row = go.GetComponent<RectTransform>(); row.sizeDelta = new Vector2(900, 168);
            var layout = go.GetComponent<LayoutElement>(); layout.preferredHeight = 168;
            var mid = Child("Mid", row); mid.anchorMin = Vector2.zero; mid.anchorMax = Vector2.one;
            mid.offsetMin = new Vector2(162, 10); mid.offsetMax = new Vector2(-200, -10);
            var right = Child("Right", row); right.anchorMin = new Vector2(1, 0); right.anchorMax = Vector2.one;
            right.offsetMin = new Vector2(-190, 12); right.offsetMax = new Vector2(-14, -12);
            var pic = Child("Picture", row); pic.sizeDelta = new Vector2(132, 132);
            var lines = new List<TMP_Text>();
            for (int i = 0; i < copy.Length; i++)
            {
                var text = Child("Line" + i, mid).gameObject.AddComponent<TextMeshProUGUI>();
                text.font = font; text.fontSize = i == 0 ? 18 : 15; text.text = copy[i]; lines.Add(text);
            }
            TaohuoProductLayout.Attach(row);
            var adaptive = row.GetComponent<TaohuoProductLayout>();
            foreach (float width in new[] { 900f, 720f, 600f, 400f, 900f })
            {
                row.sizeDelta = new Vector2(width, 168); adaptive.Refresh();
                row.sizeDelta = new Vector2(width, layout.preferredHeight);
                float bottom = 0;
                for (int i = 0; i < lines.Count; i++)
                {
                    var text = lines[i]; var rect = text.rectTransform;
                    Need(text.text == copy[i], "Product copy changed");
                    Need(-rect.offsetMax.y >= bottom, "Text lines overlap");
                    float preferred = text.GetPreferredValues(text.text, rect.rect.width, Mathf.Infinity).y;
                    Need(rect.rect.height + .5f >= preferred, "Wrapped text is clipped");
                    bottom = -rect.offsetMin.y;
                }
                Need(bottom <= mid.rect.height + .5f, "Text exceeds product card");
                float height = layout.preferredHeight;
                adaptive.Refresh(); Need(Mathf.Abs(layout.preferredHeight - height) < .01f, "Layout oscillates");
                checks++;
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
    static RectTransform Child(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); return rect;
    }
    static void Need(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
#endif
