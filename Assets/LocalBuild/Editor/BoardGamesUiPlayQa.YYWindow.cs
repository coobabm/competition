#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static partial class BoardGamesUiPlayQa
{
    public static void RunYYWindow() => RunMode("yy-window");

    static void InspectYyRendered(TMP_Text label, string name, RectTransform viewport = null, bool complete = true)
    {
        if (complete) Inspect(label, name);
        else
        {
            label.ForceMeshUpdate();
            Require(label.textInfo.characterInfo.Take(label.textInfo.characterCount).Any(c => c.isVisible), name + " preview has visible characters (intentional ellipsis allowed)");
        }
        Require(label.isActiveAndEnabled && !label.canvasRenderer.cull, name + " CanvasRenderer is not culled");
        Require(label.color.a > .9f && label.canvasRenderer.GetAlpha() > .9f, name + " text opacity is visible");
        foreach (var group in label.GetComponentsInParent<CanvasGroup>())
            Require(group.alpha > .9f, name + " ancestor CanvasGroup visible");
        Require(label.canvas != null && label.canvas.isActiveAndEnabled, name + " belongs to active production Canvas");
        if (viewport != null)
        {
            var bounds = CenterBounds(label.rectTransform, viewport);
            Require(bounds.yMin >= viewport.rect.yMin - 1 && bounds.yMax <= viewport.rect.yMax + 1,
                name + " complete body lies inside live clipping viewport");
        }
    }

    static IEnumerator YYWindowChecks()
    {
        yield return null;
        gameWindow.CloseWindow(); ctl.Window.CloseWindow();
        var chat = YYChatHub.Instance;
        Require(chat != null && chat.View != null, "Actual Main YY hub and view exist");
        var view = chat.View;
        ctl.Sim.S.stage = 3;
        XgAppAcquisitionHost.Install(ctl);
        var intro = XgAppAcquisitionHost.Instance;
        Require(intro != null && intro.Current, "Actual group introduction host bound to isolated Main save");
        // Explicit presentation fixture: do not claim natural date/unlock timing or replay user saves.
        intro.enabled = false;
        var data = Resources.Load<TextAsset>("LingGuangV05/Data/group_intro_scripts");
        var catalog = JsonUtility.FromJson<XgGroupIntroCatalog>(data.text);
        var script = catalog.scripts.Single(s => s.id == "miaoyu_intro");
        var progress = ctl.Sim.IntroProgress(script.id);
        progress.step = script.steps.FindIndex(s => s.type == "choice"); progress.choice = -1; progress.reply = 0;
        ctl.Sim.S.appAcquisition.activeScript = script.id; ctl.Sim.S.appAcquisition.revision++;
        var conv = chat.Conversation(YYChatHub.EraGroup);
        int existing = conv.messages.Count;
        for (int i = 0; i < 128; i++)
            conv.messages.Add(new YYMessage { from = i % 2 == 0 ? YYChatHub.Me : YYChatHub.EraGroup,
                text = i % 2 == 0 ? "这是保留在历史里的聊天记录，第 " + i + " 条。" : "[小刚] 较早的群聊记录仍然保留，第 " + i + " 条。", gameSeconds = runtime.Sim.S.gameSeconds });
        string[] recent = { "先看看二手卡的价格。", "[小刚] 我们楼下网吧全换 1060 了，旧的 970 全挂喵鱼上", "[阿杰] 二手 970 一千出头，就是不知道被网吧折腾成啥样" };
        for (int i = 0; i < recent.Length; i++) conv.messages.Add(new YYMessage { from = i == 0 ? YYChatHub.Me : YYChatHub.EraGroup, text = recent[i], gameSeconds = runtime.Sim.S.gameSeconds });
        int total = conv.messages.Count;
        chat.router.Open(YYChatHub.AppId, YYChatHub.EraGroup);
        chat.Select(YYChatHub.EraGroup);
        FindField(view.GetType(), "dirty").SetValue(view, true);
        FindField(view.GetType(), "stickBottom").SetValue(view, true);
        var wait = ToolWait(2); while (wait.MoveNext()) yield return wait.Current;
        Require(view.IsOpen && intro.Playing && !intro.Held, "Actual YY window shows active unheld group introduction");
        var choices = Field<RectTransform>(view, "introInput");
        Require(choices != null && choices.gameObject.activeInHierarchy, "Real group introduction choices panel visible");
        foreach (string expected in new[] { "喵鱼是什么？", "二手卡靠谱吗？", "跳过介绍" })
        {
            var label = choices.GetComponentsInChildren<TMP_Text>().Single(t => t.text == expected);
            InspectYyRendered(label, "YY intro " + expected);
        }
        var content = Field<RectTransform>(view, "scrollContent");
        var scroll = Field<ScrollRect>(view, "scroll");
        Require(content.rect.height > 8192, "Actual YY history layout exceeds 8192px cumulative coordinate");
        var bubbles = content.GetComponentsInChildren<TMP_Text>().Where(t => t.name == "Text" && t.transform.parent.name == "Bubble").ToArray();
        Require(bubbles.Length >= 131, "Old and recent text bubbles coexist without trimming history");
        var latest = bubbles.Skip(bubbles.Length - 3).ToArray();
        for (int i = 0; i < latest.Length; i++) InspectYyRendered(latest[i], "YY recent body " + i + " with intro", scroll.viewport);
        var row = Field<RectTransform>(view, "contactList").Find("Session_" + YYChatHub.EraGroup);
        var preview = row.Find("Preview").GetComponent<TMP_Text>();
        Require(preview.text.Contains("二手 970"), "Contact preview reflects the same latest message");
        InspectYyRendered(preview, "YY selected contact preview", complete: false);
        Capture("yy-long-history-miaoyu-intro-full-window.png", view.transform as RectTransform);
        Require(conv.messages.Count == total && total == existing + 131 && recent.Select((s, i) => conv.messages[total - 3 + i].text == s).All(x => x),
            "UI rendering preserves existing and newly appended fixture messages");
        ctl.Sim.S.appAcquisition.activeScript = ""; ctl.Sim.S.appAcquisition.revision++;
        wait = ToolWait(.5f); while (wait.MoveNext()) yield return wait.Current;
        Require(Field<RectTransform>(view, "introInput") == null, "Removing presentation fixture restores ordinary input panel");
        bubbles = content.GetComponentsInChildren<TMP_Text>().Where(t => t.name == "Text" && t.transform.parent.name == "Bubble").ToArray();
        latest = bubbles.Skip(bubbles.Length - 3).ToArray();
        foreach (var label in latest) InspectYyRendered(label, "YY recent body without intro", scroll.viewport);
        Capture("yy-long-history-normal-input-full-window.png", view.transform as RectTransform);
        Require(conv.messages.Count == total, "Closing introduction presentation does not delete chat messages");
    }
}
#endif
