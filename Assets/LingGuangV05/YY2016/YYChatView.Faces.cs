using System;
using LingGuangV05.Core;
using LingGuangV05.Core.Chat;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.YY
{
    /// <summary>
    /// YY faces in the chat window: inline faces in bubbles and previews, the 表情 picker on the tool strip,
    /// and 斗图 sticker messages. Kept apart from YYChatView.cs so the main view only calls in here.
    /// </summary>
    public sealed partial class YYChatView
    {
        YYFacePicker facePicker;
        int caretBeforePicker = -1;

        /// <summary>Bubble text as TMP markup with inline faces (attention underlines are already escaped markup).</summary>
        static string BubbleText(string body, bool focusMarkup) =>
            focusMarkup ? YYFaceRenderer.FaceTags(body, true) : YYFaceRenderer.Markup(body, true);

        /// <summary>True when a group line starts with a "[name]" sender label rather than a face.</summary>
        static bool HasSenderLabel(string body) =>
            body.StartsWith("[", StringComparison.Ordinal) && body.IndexOf(']') > 0 && !YYFaces.TryFaceAt(body, 0, out _, out _);

        static bool IsSticker(YYMessage m) => m != null && !string.IsNullOrEmpty(m.sticker) && YYStickers.Find(m.sticker) != null;

        /// <summary>The session-list preview line (TMP markup).</summary>
        static string PreviewLine(YYMessage m)
        {
            if (m.kind == YYKind.File) return YYFaceRenderer.Markup(Lang.T("[文件] ") + m.file);
            string prefix = m.from == YYChatHub.Me ? Lang.T("我：") : "";
            if (IsSticker(m))
                return YYFaceRenderer.Markup(prefix + Lang.T("[斗图] ") + YYStickers.Find(m.sticker).Caption(GameText.IsEnglish));
            return YYFaceRenderer.Markup(prefix) + YYFaceRenderer.PreviewMarkup(m.text ?? "", 24);
        }

        // ───────────── picker ─────────────

        void ToggleFacePicker()
        {
            if (root == null || input == null) return;
            if (facePicker == null)
            {
                var bottomLeft = new Vector2(SideWidth + 6, BottomHeight + InputHeight + ToolHeight + 2);
                facePicker = YYFacePicker.Create(root, bottomLeft, font, Today, InsertFace, SendSticker, AddFocus);
                input.onDeselect.AddListener(_ => caretBeforePicker = input.stringPosition);
            }
            if (input.isFocused) caretBeforePicker = input.stringPosition;
            facePicker.Toggle();
        }

        DateTime Today() => hub != null && hub.runtime != null && hub.runtime.Sim != null ? GameCalendar.Now(hub.runtime.Sim.S) : GameCalendar.Start;

        /// <summary>Puts the face's code where the caret was (or at the end), then gives the input its focus back.</summary>
        void InsertFace(YYFace face)
        {
            if (input == null || face == null) return;
            string text = input.text ?? "";
            string code = face.Code;
            if (input.characterLimit > 0 && text.Length + code.Length > input.characterLimit) return;
            int at = caretBeforePicker < 0 ? text.Length : Mathf.Clamp(caretBeforePicker, 0, text.Length);
            input.text = text.Insert(at, code);
            caretBeforePicker = at + code.Length;
            input.onFocusSelectAll = false;
            input.ActivateInputField();
            StartCoroutine(PlaceCaret(caretBeforePicker));
        }

        System.Collections.IEnumerator PlaceCaret(int at)
        {
            yield return null;
            if (input == null) yield break;
            at = Mathf.Clamp(at, 0, (input.text ?? "").Length);
            input.stringPosition = at;
            input.selectionStringAnchorPosition = at;
            input.selectionStringFocusPosition = at;
        }

        /// <summary>A sticker goes out at once as its own message; its text stands in for it in logs and model context.</summary>
        void SendSticker(YYSticker sticker)
        {
            if (hub == null || sticker == null) return;
            string id = hub.S.selected;
            string text = sticker.PreviewText(false);
            hub.Send(id, text);
            var conv = hub.Conversation(id);
            if (conv != null)
                for (int i = conv.messages.Count - 1; i >= 0; i--)
                {
                    var m = conv.messages[i];
                    if (m.from != YYChatHub.Me || m.text != text || !string.IsNullOrEmpty(m.sticker)) continue;
                    m.sticker = sticker.Id;
                    break;
                }
            lastSignature = "";
            stickBottom = true; dirty = true;
        }

        // ───────────── sticker messages ─────────────

        float StickerBubble(float y, YYMessage m, bool mine, YYContact who, float width)
        {
            const float avatar = 36, gap = 10, size = 112;
            var sticker = YYStickers.Find(m.sticker);
            float left = mine ? width - 16 - avatar - gap - size : 16 + avatar + gap;
            if (!mine) Avatar(scrollContent, MessageSprite(m, who), Initial(who), who != null ? who.color : C.Muted, new Vector2(16, -y), avatar);
            else Avatar(scrollContent, meSprite, "我", C.Mine, new Vector2(width - 16 - avatar, -y), avatar);
            var card = Rect("Sticker", scrollContent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(left, -y - size), new Vector2(left + size, -y));
            var hit = card.gameObject.AddComponent<Image>(); hit.color = new Color(1, 1, 1, 0);
            YYFacePicker.StickerView(card, sticker, font, 0);
            UiTip.Add(card, () => Lang.T("斗图：") + sticker.Caption(GameText.IsEnglish));
            return size;
        }
    }
}
