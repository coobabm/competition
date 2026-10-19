#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;

public static partial class BrainWindowPlayQa
{
    static YYChatHub disclosureHub;
    static ChapterOneDesktopRouter disclosureRouter;
    [Serializable] sealed class AiDisclosureReport
    {
        public string scope = "Actual YY UI opened through production router; contact availability is fixture data; models disabled; no message sent or generated inference claimed.";
        public string language, contact, text, screenshot;
        public bool actualUi, localModelsDisabled, active, overflowing;
        public float preferredWidth, availableWidth, preferredHeight, availableHeight;
        public Rect labelBounds, barBounds, headerBounds, messagesBounds;
        public int visibleCharacters;
    }
    static Rect DisclosureBounds(RectTransform value, RectTransform relativeTo)
    {
        var corners = new Vector3[4]; value.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (var corner in corners)
        {
            Vector2 point = relativeTo.InverseTransformPoint(corner);
            min = Vector2.Min(min, point); max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    static bool DisclosureContained(Rect inner, Rect outer)
        => inner.xMin >= outer.xMin - .5f && inner.xMax <= outer.xMax + .5f
        && inner.yMin >= outer.yMin - .5f && inner.yMax <= outer.yMax + .5f;

    static void OpenDisclosureContact(string contact, string language)
    {
        SetLocale(language); // Scoped memory language provider; no settings mutation.
        Check(disclosureRouter.Open(YYChatHub.AppId, contact), "Production router opens disclosure contact: " + contact);
    }
    static void CaptureDisclosure(string contact, string stem)
    {
        var view = disclosureHub.View;
        Check(view != null && view.isActiveAndEnabled && disclosureHub.IsShowing(contact), "Actual YY contact visible: " + contact);
        var conversation = view.transform.Find("YY2016/Conversation") as RectTransform;
        Check(conversation != null, "YY conversation viewport exists");
        var bar = conversation.Find("AiContentNotice") as RectTransform;
        var label = bar == null ? null : bar.Find("Label").GetComponent<TMP_Text>();
        Check(label != null, "YY fixed AI disclosure label exists");
        Canvas.ForceUpdateCanvases(); label.ForceMeshUpdate();
        var llm = ctl.runtime.GetComponent<LocalLlm>();
        var embedder = ctl.runtime.GetComponent<LocalEmbedder>();
        string expected = AiContentNotice.Footer(GameText.IsEnglish);
        var report = new AiDisclosureReport {
            language = GameText.LanguageId, contact = contact, text = label.text, screenshot = stem + ".png",
            actualUi = true, localModelsDisabled = llm != null && !llm.enabled && embedder != null && !embedder.enabled,
            active = label.isActiveAndEnabled && label.gameObject.activeInHierarchy,
            overflowing = label.isTextOverflowing, preferredWidth = label.preferredWidth, availableWidth = label.rectTransform.rect.width,
            preferredHeight = label.preferredHeight, availableHeight = label.rectTransform.rect.height,
            labelBounds = DisclosureBounds(label.rectTransform, conversation), barBounds = DisclosureBounds(bar, conversation),
            headerBounds = DisclosureBounds((RectTransform)conversation.Find("Header"), conversation),
            messagesBounds = DisclosureBounds((RectTransform)conversation.Find("Messages"), conversation)
        };
        for (int i = 0; i < label.textInfo.characterCount; i++) if (label.textInfo.characterInfo[i].isVisible) report.visibleCharacters++;
        File.WriteAllText(Path.Combine(output, stem + ".json"), JsonUtility.ToJson(report, true));
        Check(report.localModelsDisabled, "Disclosure fixture keeps both local models disabled");
        Check(report.active && report.text == expected, "Exact localized AI disclosure visible for " + contact);
        Check(report.visibleCharacters > 0 && label.canvasRenderer != null && !label.canvasRenderer.cull && label.canvasRenderer.GetInheritedAlpha() > 0,
            "AI disclosure has visible glyphs and is not culled");
        Check(!report.overflowing && report.preferredWidth <= report.availableWidth + .5f && report.preferredHeight <= report.availableHeight + .5f,
            "AI disclosure text fits default window without overflow");
        Check(DisclosureContained(report.labelBounds, report.barBounds) && DisclosureContained(report.barBounds, conversation.rect),
            "AI disclosure stays inside fixed bar and conversation viewport");
        var toolsBounds = DisclosureBounds((RectTransform)conversation.Find("Tools"), conversation);
        var inputBounds = DisclosureBounds((RectTransform)conversation.Find("Input"), conversation);
        Check(report.barBounds.yMax <= report.messagesBounds.yMin + .5f && report.barBounds.yMin >= toolsBounds.yMax - .5f
            && report.barBounds.yMin >= inputBounds.yMax - .5f,
            "AI disclosure sits below replies and above tools/input without overlap");
        Check(!label.raycastTarget, "AI disclosure never intercepts input");
        var messages = disclosureHub.Conversation(contact).messages;
        var notices = conversation.Find("Messages/Viewport/Content").GetComponentsInChildren<TMP_Text>()
            .Where(text => text.name.StartsWith("AiReplyNotice:", StringComparison.Ordinal)).ToArray();
        int replies = messages.Count(message => message.from != YYChatHub.Me && message.kind != YYKind.System);
        Check(notices.Length == replies, "Every non-player text/photo/file/sticker reply has one individual notice");
        Check(notices.All(text => text.name != "AiReplyNotice:" + YYChatHub.Me && text.text == expected && !text.raycastTarget), "Player messages have no AI label; every reply label is localized and input-transparent");
        if (contact == "netbar") Check(replies >= 2 && messages.Any(message => message.from == YYChatHub.Me), "Built-in group history shows multiple NPC replies and an unlabelled player message");
        Check(QaDesktopCapture.Save(Path.Combine(output, report.screenshot)), "Rendered disclosure screenshot: " + report.screenshot);
    }
    static void EnqueueAiDisclosure()
    {
        steps.Enqueue(() => {
            disclosureHub = ctl.runtime.GetComponent<YYChatHub>();
            disclosureRouter = ctl.runtime.GetComponent<ChapterOneDesktopRouter>();
            Check(disclosureHub != null && disclosureHub.S != null && disclosureHub.View != null && disclosureRouter != null, "Actual YY hub and router ready");
            disclosureHub.S.lingguangUnlocked = true; // Explicit contact-availability fixture; no fabricated reply.
            OpenDisclosureContact(YYChatHub.LingGuangId, GameText.Chinese);
        });
        steps.Enqueue(() => CaptureDisclosure(YYChatHub.LingGuangId, "ai-disclosure-lingguang-zh"));
        steps.Enqueue(() => OpenDisclosureContact(YYChatHub.LingGuangId, GameText.English));
        steps.Enqueue(() => CaptureDisclosure(YYChatHub.LingGuangId, "ai-disclosure-lingguang-en"));
        steps.Enqueue(() => OpenDisclosureContact("landlord", GameText.English));
        steps.Enqueue(() => CaptureDisclosure("landlord", "ai-disclosure-npc-en"));
        steps.Enqueue(() => OpenDisclosureContact("landlord", GameText.Chinese));
        steps.Enqueue(() => CaptureDisclosure("landlord", "ai-disclosure-npc-zh"));
        steps.Enqueue(() => {
            var group = Array.Find(YYChatHub.Contacts, item => item.group && disclosureHub.IsVisible(item));
            Check(group != null, "existing visible group for disclosure check");
            OpenDisclosureContact(group.id, GameText.Chinese);
        });
        steps.Enqueue(() => CaptureDisclosure(disclosureHub.S.selected, "ai-disclosure-group-zh"));
        steps.Enqueue(() => OpenDisclosureContact(disclosureHub.S.selected, GameText.English));
        steps.Enqueue(() => CaptureDisclosure(disclosureHub.S.selected, "ai-disclosure-group-en"));
    }
}
#endif
