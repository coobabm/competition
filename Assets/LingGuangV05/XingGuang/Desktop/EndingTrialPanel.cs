using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Six original saved sheets in three gates. Rendering and draft picks only; core scores.</summary>
    public sealed class EndingTrialPanel : MonoBehaviour
    {
        public readonly EndingUiSession Session = new EndingUiSession();
        XgUi ui;
        RectTransform root, readingArea, content;
        ScrollRect reader;
        TMP_Text heading, status, body;
        TMP_InputField finalInput;
        RawImage photo;
        Func<string, Texture> resolvePhoto;
        XgBtn[] sheets;
        readonly List<XgBtn> choices = new List<XgBtn>();
        XgBtn previous, next, submit, original, finalPage, memories, si;
        string attemptId = "", draftLine = "", photoId = "";
        int itemIndex;
        int[] picks = new int[0];
        bool built, finalMode, showOriginal;
        object owner, state;

        public void Build(RectTransform area, TMP_FontAsset font, Func<string, Texture> approvedPhotoResolver = null)
        {
            if (built) return; built = true; root = area; resolvePhoto = approvedPhotoResolver;
            ui = new XgUi(font, null, true); Panel(root, XgDark.Page);
            heading = Label(Strip("Title", root, 14, 38), 30);
            status = Label(Strip("Status", root, 56, 40), 18);
            sheets = new XgBtn[6];
            for (int i = 0; i < 6; i++)
            {
                int q = i;
                sheets[i] = Button("Sheet" + q, .02f, .26f, 104 + i * 50, 42, false, () => Begin(q));
            }
            finalPage = Button("FinalPage", .02f, .26f, 414, 42, false, () => { finalMode = true; Refresh(); });
            memories = Button("DiscoverMemories", .02f, .26f, 60, 34, true, () => Session.Send(EndingUiAction.DiscoverMemories));
            si = Button("ReadSi", .02f, .26f, 16, 34, true, () => Session.Preparation());
            readingArea = Rect("ReadingArea", root, new Vector2(.29f, 0), Vector2.one, new Vector2(0, 226), new Vector2(-16, -104));
            reader = readingArea.gameObject.AddComponent<ScrollRect>(); reader.horizontal = false; reader.inertia = false;
            reader.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Rect("Viewport", readingArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(viewport, XgDark.Card); viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect("Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero); content.pivot = new Vector2(.5f, 1);
            reader.viewport = viewport; reader.content = content; body = Label(content, 22); body.margin = new Vector4(18, 14, 18, 14);
            var photoFrame = Rect("PhotoFrame", root, new Vector2(.73f, 1), new Vector2(.98f, 1), new Vector2(0, -414), new Vector2(0, -104));
            var photoRect = Rect("Photo", photoFrame, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            photo = photoRect.gameObject.AddComponent<RawImage>(); photo.raycastTarget = false;
            var aspect = photoRect.gameObject.AddComponent<AspectRatioFitter>(); aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            original = Button("Original", .29f, .98f, 186, 30, true, () => { showOriginal = !showOriginal; Refresh(); });
            previous = Button("PreviousItem", .29f, .45f, 60, 34, true, () => Move(-1));
            next = Button("NextItem", .46f, .62f, 60, 34, true, () => Move(1));
            submit = Button("Submit", .64f, .98f, 16, 34, true, Submit);
            BuildFinalInput();
            Session.Changed += Refresh; Session.SetVisible(isActiveAndEnabled && root.gameObject.activeInHierarchy); Refresh();
        }
        void BuildFinalInput()
        {
            var rt = Rect("PlayerLastLine", root, new Vector2(.29f, 0), new Vector2(.98f, 0), new Vector2(0, 108), new Vector2(0, 172));
            Panel(rt, XgDark.HudChip);
            finalInput = rt.gameObject.AddComponent<TMP_InputField>(); finalInput.characterLimit = 40;
            finalInput.lineType = TMP_InputField.LineType.SingleLine;
            var viewport = Rect("Viewport", rt, Vector2.zero, Vector2.one, new Vector2(10, 8), new Vector2(-10, -8));
            viewport.gameObject.AddComponent<RectMask2D>();
            finalInput.textViewport = viewport;
            finalInput.textComponent = Label(Rect("Text", viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 16);
            var placeholder = Label(Rect("Placeholder", viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 14);
            placeholder.color = XgDark.Muted; finalInput.placeholder = placeholder;
            finalInput.onValueChanged.AddListener(value => draftLine = value);
        }
        public void Bind(EndingUiSnapshot snapshot, EndingUiCallbacks callbacks)
        {
            if (snapshot == null || !ReferenceEquals(owner, snapshot.owner) || !ReferenceEquals(state, snapshot.stateOwner))
            {
                owner = snapshot == null ? null : snapshot.owner; state = snapshot == null ? null : snapshot.stateOwner;
                attemptId = draftLine = photoId = ""; picks = new int[0]; itemIndex = 0; finalMode = showOriginal = false;
            }
            Session.Bind(snapshot, callbacks);
        }
        void OnEnable() { if (built) Session.SetVisible(true); }
        void OnDisable() { Session.SetVisible(false); }
        void OnDestroy() { Session.Changed -= Refresh; }
        string T(string zh, string en) => Session.Snapshot != null && Session.Snapshot.english ? en : zh;
        // A1 (待办 §4): the paper is now the pre-release red-team test. First line of its cover.
        public static string CoverLine(bool english) => english ? "Before letting it out, someone has to try to break it first." : "放它出去之前，总得有人先试着把它弄坏。";
        string Safe(EndingUiText text)
        {
            if (text == null || Session.Callbacks == null) return "";
            var filter = text.reviewed ? Session.Callbacks.FilterReviewedText : Session.Callbacks.FilterText;
            if (filter == null) return "";
            try { return filter(text.Get(Session.Snapshot.english), Session.Snapshot.english) ?? ""; }
            catch { return ""; }
        }
        TMP_Text Label(RectTransform rt, int size)
        { var t = ui.Text(rt, "", size, XgDark.Ink, TextAlignmentOptions.TopLeft); t.richText = false; return t; }
        XgBtn Button(string name, float min, float max, float y, float height, bool bottom, Action action)
        {
            var b = ui.Button(root, "", () => { if (Session.Current && !Session.Busy) action(); }, 18);
            b.rt.name = name; b.label.richText = false; b.label.enableAutoSizing = true; b.label.fontSizeMin = 14; b.label.fontSizeMax = 18;
            var fx = b.button as XgButtonPressFeedback; if (fx != null) { fx.Label = null; fx.PressTarget = null; }
            b.rt.anchorMin = new Vector2(min, bottom ? 0 : 1); b.rt.anchorMax = new Vector2(max, bottom ? 0 : 1);
            b.rt.offsetMin = new Vector2(0, bottom ? y : -y - height); b.rt.offsetMax = new Vector2(0, bottom ? y + height : -y);
            return b;
        }
        void Begin(int question)
        {
            var view = Session.Snapshot.trial;
            foreach (var menu in view.menu) if (menu.question == question && menu.canBegin)
            {
                finalMode = false; Session.Send(EndingUiAction.BeginTrial, question: question); return;
            }
        }
        void Move(int delta)
        {
            itemIndex = Math.Max(0, Math.Min(Session.Snapshot.trial.items.Count - 1, itemIndex + delta));
            showOriginal = false; reader.verticalNormalizedPosition = 1; Refresh();
        }
        void Pick(int choice)
        {
            var t = Session.Snapshot.trial;
            if (!Session.CanAct || !t.active || t.submitted || itemIndex >= picks.Length) return;
            picks[itemIndex] = choice; Refresh();
        }
        bool Complete()
        {
            if (picks.Length == 0 || picks.Length != Session.Snapshot.trial.items.Count) return false;
            foreach (int pick in picks) if (pick < 0) return false;
            return true;
        }
        void Submit()
        {
            var t = Session.Snapshot.trial; if (!Session.CanAct) return;
            if (finalMode && t.finalPageReady && !t.finalPageSaved) Session.Send(EndingUiAction.SubmitFinalPage, value: draftLine);
            else if (!finalMode && t.active && !t.submitted && Complete())
                Session.Send(EndingUiAction.SubmitTrial, question: t.question, attemptId: t.attemptId, picks: picks);
        }
        public void Refresh()
        {
            if (!built || !Session.Visible || Session.Snapshot == null) return;
            var s = Session.Snapshot; var t = s.trial; bool act = Session.CanAct;
            heading.text = T("最后一份卷子", "The final paper");
            status.text = Session.Failed ? FailureText() :
                Session.Busy ? T("正在保存……", "Saving…") : Safe(s.blocker);
            if (attemptId != t.attemptId || picks.Length != t.items.Count)
            {
                attemptId = t.attemptId; itemIndex = 0; showOriginal = false; picks = new int[t.items.Count];
                for (int i = 0; i < picks.Length; i++) picks[i] = -1;
            }
            if (t.submitted && t.submittedPicks.Count == picks.Length) picks = t.submittedPicks.ToArray();
            itemIndex = Math.Max(0, Math.Min(itemIndex, Math.Max(0, t.items.Count - 1)));
            for (int q = 0; q < 6; q++)
            {
                EndingTrialMenuItem entry = t.menu.Find(m => m.question == q);
                string prefix = T("第", "Gate ") + (q / 2 + 1) + T("关 · ", " · ");
                sheets[q].Set(prefix + (entry == null ? (q + 1).ToString() : Safe(entry.title) + "  " + entry.bestStars + "★"), act && entry != null && entry.canBegin);
            }
            finalPage.Set(T("最后一页：各写一句", "Last page: a line each"), !Session.Busy && (t.finalPageReady || t.finalPageSaved));
            memories.Set(T("整理真实记忆", "Gather real memories"), act && t.canDiscoverMemories);
            si.Set(T("阅读 SI 来信", "Read the SI letter"), Session.Current && !Session.Busy && s.siReady && Session.Callbacks.OpenPreparation != null);
            var b = new StringBuilder();
            EndingTrialItemView item = !finalMode && t.items.Count > 0 ? t.items[itemIndex] : null;
            if (finalMode)
            {
                b.Append(T("它和你，各留下一句话。", "It and you each leave one line."));
                if (t.finalPageSaved)
                {
                    b.Append("\n\n").Append(Safe(t.aiLastLine)).Append('\n').Append(LingGuangV05.Runtime.AiContentNotice.Footer(s.english));
                    var playerLine = new EndingUiText { zh = t.playerLastLine, en = t.playerLastLine };
                    b.Append("\n\n").Append(Safe(playerLine));
                }
                else b.Append("\n\n").Append(T("你的话会保存在卷子末页，最多 40 个字符。", "Your line will be saved on the last page, up to 40 characters."));
            }
            else if (item != null)
            {
                b.Append(Safe(t.title)).Append(" · ").Append(Safe(t.ability)).Append("\n").Append(Safe(t.note));
                foreach (var hint in t.hints) b.Append("\n").Append(Safe(hint));
                b.Append("\n\n").Append(itemIndex + 1).Append(" / ").Append(t.items.Count).Append("\n").Append(Safe(item.prompt));
                if (!string.IsNullOrEmpty(Safe(item.ai))) b.Append("\n\n").Append(Safe(item.ai)).Append('\n').Append(LingGuangV05.Runtime.AiContentNotice.Footer(s.english));
                // The photo description names the correct object. Reveal it only with results.
                if (t.submitted && !string.IsNullOrEmpty(Safe(item.photoDescription))) b.Append("\n\n").Append(Safe(item.photoDescription));
                if (showOriginal) b.Append("\n\n").Append(Safe(item.original));
                if (t.submitted)
                    b.Append("\n\n").Append(T("本次得分：", "Score: ")).Append(t.score).Append(" · ").Append(t.stars).Append("★\n").Append(Safe(item.feedback));
            }
            else b.Append(CoverLine(s.english)).Append("\n\n").Append(T("按顺序完成三关。每关两份卷面；交卷后才显示结果。", "Complete the three gates in order. Each has two sheets; results appear after submission."));
            SetPhoto(item);
            body.text = b.ToString();
            content.sizeDelta = new Vector2(0, Math.Max(reader.viewport.rect.height, body.GetPreferredValues(body.text, Math.Max(100, reader.viewport.rect.width), 0).y + 24));
            int optionCount = item == null ? 0 : t.markMode ? 2 : item.choices.Count;
            while (choices.Count < optionCount)
            {
                int index = choices.Count;
                choices.Add(Button("Choice" + index, 0, 1, 110, 32, true, () => Pick(index)));
            }
            for (int i = 0; i < choices.Count; i++)
            {
                var button = choices[i]; button.Show(i < optionCount);
                if (i >= optionCount) continue;
                // Two columns, enough rows for the original choices. Reader yields space if more are supplied.
                button.rt.anchorMin = new Vector2(i % 2 == 0 ? .29f : .64f, 0); button.rt.anchorMax = new Vector2(i % 2 == 0 ? .63f : .98f, 0);
                button.rt.offsetMin = new Vector2(0, 110 + i / 2 * 64); button.rt.offsetMax = new Vector2(0, 168 + i / 2 * 64);
                string label = t.markMode ? (i == 0 ? T("不标记", "Leave unmarked") : T("标记有错", "Mark as wrong")) : Safe(item.choices[i]);
                button.Set(label, act && t.active && !t.submitted, picks[itemIndex] == i ? XgDark.OnFill : XgDark.Button);
            }
            float optionTop = 110 + ((optionCount + 1) / 2) * 64;
            readingArea.offsetMin = new Vector2(0, Math.Max(226, optionTop + 50));
            original.rt.offsetMin = new Vector2(0, Math.Max(186, optionTop + 8)); original.rt.offsetMax = new Vector2(0, Math.Max(216, optionTop + 38));
            original.Show(!finalMode && item != null && !string.IsNullOrEmpty(Safe(item.original)));
            original.Set(showOriginal ? T("收起原文", "Hide original") : T("看原文", "Read original"), !Session.Busy);
            previous.Show(!finalMode); next.Show(!finalMode);
            previous.Set(T("上一小题", "Previous item"), item != null && itemIndex > 0 && !Session.Busy);
            next.Set(T("下一小题", "Next item"), item != null && itemIndex + 1 < t.items.Count && !Session.Busy);
            finalInput.gameObject.SetActive(finalMode && !t.finalPageSaved);
            finalInput.interactable = act && t.finalPageReady && !t.finalPageSaved;
            if (finalInput.text != draftLine) finalInput.SetTextWithoutNotify(draftLine);
            ((TMP_Text)finalInput.placeholder).text = T("写下你的一句话", "Write your line");
            submit.Set(finalMode ? T("保存末页", "Save final page") : T("交卷", "Submit paper"), act &&
                (finalMode ? t.finalPageReady && !t.finalPageSaved : t.active && !t.submitted && Complete()));
        }
        void SetPhoto(EndingTrialItemView item)
        {
            string id = item == null ? "" : item.photoId;
            if (id != photoId)
            {
                photoId = id; photo.texture = null;
                if (resolvePhoto != null && !string.IsNullOrEmpty(id)) try { photo.texture = resolvePhoto(id); } catch { photo.texture = null; }
            }
            bool visible = item != null && photo.texture != null;
            photo.gameObject.SetActive(visible); readingArea.anchorMax = new Vector2(visible ? .71f : 1, 1);
            if (visible) photo.GetComponent<AspectRatioFitter>().aspectRatio = (float)photo.texture.width / Math.Max(1, photo.texture.height);
        }
        string FailureText() => string.IsNullOrEmpty(Safe(Session.Failure)) ? T("操作未完成，答案和末页草稿仍在。", "The action did not complete. Your answers and final-page draft are retained.") : Safe(Session.Failure);
    }
}
