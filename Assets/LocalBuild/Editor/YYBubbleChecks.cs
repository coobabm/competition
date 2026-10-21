#if UNITY_EDITOR
using System;
using System.Reflection;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.YY;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class YYBubbleChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Need(bool ok, string message) { if (!ok) throw new Exception(message); }

    public static void Run()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit Mode only");
        var font = PrologueDesk.CjkFont();
        Need(font != null, "YY CJK font must be available");
        int checkedMessages = 0;
        foreach (float y in new[] { 16f, 8192.13f, 65536.3f, 262144.7f })
        foreach (string from in new[] { "regression_npc", YYChatHub.Me, YYChatHub.EraGroup })
        foreach (string body in new[] { "新手先看哪里？", "这条消息包含多行。\n聊天记录变长以后，仍然应该完整显示。最后一个字。", "A longer conversation must keep every new message visible." })
        {
            var fixture = new GameObject("YY bubble regression", typeof(RectTransform), typeof(Canvas));
            try
            {
                fixture.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var view = fixture.AddComponent<YYChatView>(); view.enabled = false;
                var content = new GameObject("History", typeof(RectTransform)).GetComponent<RectTransform>();
                content.SetParent(fixture.transform, false); content.sizeDelta = new Vector2(900, y + 400);
                typeof(YYChatView).GetField("font", Private).SetValue(view, font);
                typeof(YYChatView).GetField("scrollContent", Private).SetValue(view, content);
                bool mine = from == YYChatHub.Me;
                var who = from == YYChatHub.EraGroup ? YYChatHub.Contact(from) : null;
                string storedBody = who != null ? "[小刚] " + body : body;
                var message = new YYMessage { from = from, text = storedBody };
                typeof(YYChatView).GetMethod("Bubble", Private).Invoke(view, new object[] { y, message, mine, who, 900f, 430f });
                var bubble = content.Find("Bubble");
                Need(bubble != null, "Missing bubble");
                var label = bubble.GetComponentInChildren<TMP_Text>();
                Need(label != null, "Body must use bubble-local coordinates, independent of history length");
                label.ForceMeshUpdate(true, true);
                Need(!label.isTextTruncated, "Body truncated at y=" + y);
                Need(label.textInfo.characterCount > 0, "Empty body mesh at y=" + y);
                Need(label.textInfo.characterInfo[label.textInfo.characterCount - 1].isVisible, "Final character vanished at y=" + y);
                Need(label.rectTransform.rect.height + .1f >= label.preferredHeight, "Body height below preferred height at y=" + y);
                Need(message.text == storedBody, "Rendering changed stored message");
                checkedMessages++;
            }
            finally { UnityEngine.Object.DestroyImmediate(fixture); }
        }
        Debug.Log("YY_BUBBLE_PASS: " + checkedMessages + " NPC/player Chinese, multiline and English message meshes at short/long history offsets; final characters visible and messages preserved.");
    }
}
#endif
