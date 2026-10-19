using System;
using System.Collections.Generic;
using LingGuangV05.Core.Chat;
using LingGuangV05.Core.Era;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgReadingWindow
    {
        TMP_Text reactionText;
        XgBookReaderReaction reactionInFlight;
        int reactionTicket;

        void CancelReaderReaction()
        {
            reactionTicket++;
            if (reactionInFlight != null && reactionInFlight.status == "requested" && Current(bound, boundSave))
            {
                reactionInFlight.status = "cancelled";
                controller.runtime.MarkDirty(); controller.SaveNow();
            }
            reactionInFlight = null;
            // LocalLlm exposes cancellation only for judge jobs. Never cancel the shared service's other chats;
            // the ticket/save checks below invalidate this reader's result while that one bounded job finishes.
        }
        void RefreshReaderReaction(XgBook book)
        {
            if (reactionText == null || bound == null) return;
            reactionText.text = "";
            if (book == null || chapter < 0 || chapter >= bound.ChaptersRead(book)) return;
            string fixedText = bound.BookReaction(book, chapter);
            if (bound.S.bookReaderReactions == null) bound.S.bookReaderReactions = new List<XgBookReaderReaction>();
            string key = "book.chapter:" + book.id + ":" + chapter;
            var receipt = bound.S.bookReaderReactions.Find(r => r != null && r.key == key);
            if (receipt != null && receipt.status == "requested" && !ReferenceEquals(receipt, reactionInFlight))
            {
                // A saved request has no live job after loading. Do not treat it as success or issue it again.
                receipt.status = "interrupted"; controller.runtime.MarkDirty(); controller.SaveNow();
            }
            bool returned = receipt != null && receipt.status == "returned" && receipt.english == GameText.IsEnglish && !string.IsNullOrEmpty(receipt.reply);
            string label = returned ? T("它的读后感 · AI 合成内容，仅供参考", "Its reaction · AI-generated content, for reference only")
                : receipt != null && receipt.status == "requested" ? T("正在想……", "Thinking…")
                : T("阅读反馈", "Reading response");
            reactionText.text = label + "\n" + (returned ? receipt.reply : fixedText);
            if (receipt != null || reactionInFlight != null || !Visible || !Current(bound, boundSave)
                || bound.AbilitiesCount < XgSim.AbilityCount || LingGuangV05.Core.StoryFocus.Active) return;
            var hub = YYChatHub.Instance;
            if (hub != null && hub.LingGuangTalk?.Thinking == true) return;
            var llm = LocalLlm.Instance;
            if (llm == null || !llm.Ready || llm.Busy || bound.English != GameText.IsEnglish) return;
            var sim = bound; var save = boundSave; int readChapter = chapter; bool english = GameText.IsEnglish;
            var attempt = new XgBookReaderReaction { key = key, bookId = book.id, chapter = readChapter, english = english, status = "requested" };
            int ticket = ++reactionTicket;
            var messages = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("system", sim.PersonaPrompt(12, new string[0]) + "\n" + EraLexicon.PromptRule(english)
                    + (english ? " Reply in English." : " 用中文回答。")),
                new KeyValuePair<string, string>("user", sim.BookPrompt(book, readChapter))
            };
            // Reuse the service's low-priority lane and LingGuang seat. It refuses while chat/typing is busy.
            // No receipt is recorded when the service declines the request; no separate inference runner exists.
            bool accepted = llm.ChatBackground(messages, 120, .7f, reply =>
            {
                if (this == null || ticket != reactionTicket || !Visible || !Current(sim, save)) return;
                reactionInFlight = null;
                reply = EraLexicon.Scrub((reply ?? "").Trim()) ?? "";
                int end = reply.IndexOf("</think>", StringComparison.Ordinal);
                if (end >= 0) reply = reply.Substring(end + 8).Trim();
                reply = reply.Trim('「', '」', '"', '“', '”');
                if (reply.Length < 4) attempt.status = "failed";
                else
                {
                    attempt.status = "returned";
                    attempt.reply = reply.Length > 60 ? reply.Substring(0, 60) + (english ? "…" : "……") : reply;
                    // Preserve the original latest-chapter reaction API without overwriting a later chapter.
                    if (sim.S.bookLineBook == book.id && sim.S.bookLineChapter == readChapter && sim.English == english)
                        sim.S.bookLine = attempt.reply;
                }
                controller.runtime.MarkDirty(); controller.SaveNow(); signature = "";
            }, null, LlmSeat.LingGuang);
            if (!accepted) return;
            sim.S.bookReaderReactions.Add(attempt);
            if (attempt.status == "requested") reactionInFlight = attempt;
            controller.runtime.MarkDirty(); controller.SaveNow();
        }
    }
}
