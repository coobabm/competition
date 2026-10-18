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
    /// <summary>Mountable release UI. It cannot score tests, publish, pin or write saves itself.</summary>
    public sealed class EndingPreparationPanel : MonoBehaviour
    {
        public readonly EndingUiSession Session = new EndingUiSession();
        readonly List<string> chosen = new List<string>();
        readonly HashSet<string> viewed = new HashSet<string>(StringComparer.Ordinal);
        XgUi ui;
        RectTransform root, readingArea;
        TMP_Text title, status, slots, detail;
        ScrollRect reader;
        RectTransform content;
        XgBtn[] tabs, routes, memoryRows, pinRows;
        XgBtn primary, previous, next, source, sourcePrevious, sourceNext, prerequisites, cancel;
        int page, memoryPage, selectedMemory = -1, sourceIndex;
        bool siViewed, confirming;
        string siShown = "";
        object boundOwner, boundState;
        bool built;

        public void Build(RectTransform area, TMP_FontAsset font)
        {
            if (built) return; built = true;
            root = area; ui = new XgUi(font, null, true);
            Panel(root, XgDark.Page);
            title = Label(Strip("Title", root, 12, 34), 28);
            status = Label(Strip("Status", root, 48, 28), 18);
            tabs = new XgBtn[4];
            for (int i = 0; i < tabs.Length; i++) { int n = i; tabs[i] = Button("Tab" + i, .01f + i * .245f, .245f + i * .245f, 82, 34, () => SelectPage(n)); }
            readingArea = Rect("ReadingArea", root, new Vector2(0, 0), Vector2.one, new Vector2(16, 142), new Vector2(-16, -128));
            reader = readingArea.gameObject.AddComponent<ScrollRect>(); reader.horizontal = false;
            reader.movementType = ScrollRect.MovementType.Clamped; reader.inertia = false;
            var viewport = Rect("Viewport", readingArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = Panel(viewport, XgDark.Card); hit.raycastTarget = true;
            content = Rect("Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(.5f, 1); reader.viewport = viewport; reader.content = content;
            detail = Label(content, 22); detail.margin = new Vector4(18, 16, 18, 16);
            reader.onValueChanged.AddListener(_ => {
                if (page == 0 && Session.Snapshot != null && Session.Snapshot.siReady &&
                    !string.IsNullOrEmpty(siShown) && reader.verticalNormalizedPosition <= .02f) siViewed = true;
                RefreshActions();
            });
            routes = new XgBtn[4];
            string[] routeIds = { "online", "public_model", "local_only", "ai" };
            for (int i = 0; i < 4; i++) { string route = routeIds[i]; routes[i] = Button("Route" + i, .02f + i * .245f, .245f + i * .245f, -124, 36, () => ChooseRoute(route)); }
            memoryRows = new XgBtn[4]; pinRows = new XgBtn[4];
            for (int i = 0; i < 4; i++)
            {
                int row = i;
                memoryRows[i] = Button("Memory" + i, .02f, .31f, 134 + i * 58, 48, () => Inspect(memoryPage * 4 + row));
                pinRows[i] = Button("Pin" + i, .315f, .4f, 134 + i * 58, 48, () => Toggle(memoryPage * 4 + row));
            }
            slots = Label(Rect("Slots", root, Vector2.zero, new Vector2(1, 0), new Vector2(16, 54), new Vector2(-16, 84)), 18);
            previous = Button("Previous", .02f, .2f, -124, 30, () => { memoryPage = Math.Max(0, memoryPage - 1); Refresh(); });
            next = Button("Next", .21f, .4f, -124, 30, () => { memoryPage++; Refresh(); });
            sourcePrevious = Button("PreviousSource", .42f, .53f, -124, 30, () => { sourceIndex = Math.Max(0, sourceIndex - 1); Refresh(); });
            source = Button("Source", .54f, .84f, -124, 30, OpenSource);
            sourceNext = Button("NextSource", .85f, .98f, -124, 30, () => { sourceIndex++; Refresh(); });
            prerequisites = Button("Prerequisites", .02f, .32f, -51, 36, () => {
                if (page == 2 && Session.Snapshot.trial.canDiscoverMemories) Session.Send(EndingUiAction.DiscoverMemories);
                else Session.Prerequisites();
            });
            cancel = Button("Cancel", .34f, .56f, -51, 36, () => { confirming = false; Refresh(); });
            primary = Button("Primary", .58f, .98f, -51, 36, Primary);
            Session.Changed += Refresh;
            Session.SetVisible(isActiveAndEnabled && root.gameObject.activeInHierarchy);
            Refresh();
        }

        public void Bind(EndingUiSnapshot snapshot, EndingUiCallbacks callbacks)
        {
            if (snapshot == null || !ReferenceEquals(boundOwner, snapshot.owner) || !ReferenceEquals(boundState, snapshot.stateOwner))
            {
                chosen.Clear(); viewed.Clear(); siViewed = false; siShown = ""; selectedMemory = -1; sourceIndex = memoryPage = page = 0; confirming = false;
                boundOwner = snapshot == null ? null : snapshot.owner; boundState = snapshot == null ? null : snapshot.stateOwner;
            }
            Session.Bind(snapshot, callbacks);
        }
        void OnEnable() { if (built) Session.SetVisible(true); }
        void OnDisable() { Session.SetVisible(false); confirming = false; }
        void OnDestroy() { Session.Changed -= Refresh; }
        string T(string zh, string en) => Session.Snapshot != null && Session.Snapshot.english ? en : zh;
        string Safe(EndingUiText value)
        {
            if (value == null || Session.Callbacks == null) return "";
            var filter = value.reviewed ? Session.Callbacks.FilterReviewedText : Session.Callbacks.FilterText;
            if (filter == null) return "";
            try { return filter(value.Get(Session.Snapshot.english), Session.Snapshot.english) ?? ""; }
            catch { return ""; }
        }
        TMP_Text Label(RectTransform rt, int size)
        {
            var text = ui.Text(rt, "", size, XgDark.Ink, TextAlignmentOptions.TopLeft);
            text.richText = false; return text;
        }
        XgBtn Button(string name, float min, float max, float y, float h, Action action)
        {
            var b = ui.Button(root, "", () => { if (Session.Current && !Session.Busy) action(); }, 18);
            b.rt.name = name; b.label.richText = false;
            // Static button feedback also respects reduceFx when mounted outside the main lab's FX host.
            var feedback = b.button as XgButtonPressFeedback;
            if (feedback != null) { feedback.Label = null; feedback.PressTarget = null; }
            bool bottom = y < 0; b.rt.anchorMin = new Vector2(min, bottom ? 0 : 1); b.rt.anchorMax = new Vector2(max, bottom ? 0 : 1);
            b.rt.offsetMin = new Vector2(0, bottom ? -y - h : -y - h); b.rt.offsetMax = new Vector2(0, -y);
            return b;
        }
        void SelectPage(int nextPage) { page = nextPage; confirming = false; reader.verticalNormalizedPosition = 1; Refresh(); }
        void ChooseRoute(string route)
        {
            var s = Session.Snapshot;
            if (!s.siRead || !s.canChooseRoute || s.routeLocked) return;
            Session.Send(route == "ai" ? EndingUiAction.AskAi : EndingUiAction.ChooseRoute, route == "ai" ? "" : route);
        }
        void Inspect(int index)
        {
            if (index < 0 || index >= Session.Snapshot.memories.Count) return;
            selectedMemory = index; sourceIndex = 0; reader.verticalNormalizedPosition = 1;
            var card = Session.Snapshot.memories[index];
            if (card.sources.Count > 0) viewed.Add(card.id);
            Refresh();
        }
        void Toggle(int index)
        {
            var s = Session.Snapshot;
            if (!Session.CanAct || !s.routeLocked || s.pinsLocked || index < 0 || index >= s.memories.Count) return;
            if (!s.memories[index].pinnable) return;
            string id = s.memories[index].id;
            if (chosen.Contains(id)) chosen.Remove(id); else if (chosen.Count < 3) chosen.Add(id);
            confirming = false; Refresh();
        }
        void OpenSource()
        {
            var s = Session.Snapshot;
            if (selectedMemory < 0 || selectedMemory >= s.memories.Count) return;
            var sources = s.memories[selectedMemory].sources;
            if (sourceIndex >= 0 && sourceIndex < sources.Count) Session.Source(sources[sourceIndex].id);
        }
        void Primary()
        {
            var s = Session.Snapshot;
            if (!Session.CanAct) return;
            if (page == 0 && s.siReady && !s.siRead && siViewed) Session.Send(EndingUiAction.ConfirmSiRead);
            else if (page == 3 && s.releaseCommitted) Session.Send(EndingUiAction.Continue);
            else if (page == 2 && !s.memoriesReviewed && s.siRead && viewed.Count >= 2)
                Session.Send(EndingUiAction.ConfirmMemoryReview, ids: new List<string>(viewed).ToArray());
            else if (page == 2 && s.routeLocked && !s.pinsLocked && chosen.Count == 3)
            {
                if (!confirming) { confirming = true; Refresh(); }
                else Session.Send(EndingUiAction.PinThree, ids: chosen.ToArray());
            }
            else if (page == 3 && CanRelease(s))
            {
                if (!confirming) { confirming = true; Refresh(); }
                else Session.Send(EndingUiAction.CommitRelease, s.route);
            }
        }
        static bool CanRelease(EndingUiSnapshot s) => s.releaseReady && s.routeLocked && s.pinsLocked &&
            s.pinnedIds.Count == 3 && !s.releaseCommitted &&
            (s.route == "local_only" || (s.preview.verified && !string.IsNullOrEmpty(s.preview.receiptDraftId)));
        public void Refresh()
        {
            if (!built || !Session.Visible) return;
            var s = Session.Snapshot;
            if (s == null) { title.text = T("等待终局状态", "Waiting for ending state"); return; }
            if (s.pinsLocked) { chosen.Clear(); chosen.AddRange(s.pinnedIds); if (page == 2) confirming = false; }
            title.text = T("发布之前", "Before release");
            status.text = Session.Failed ? FailureText() :
                Session.Busy ? T("正在保存……", "Saving…") : !Session.Current ? T("存档已切换，请重新打开。", "The save changed. Reopen this page.") : Safe(s.blocker);
            string[] tabZh = { "SI 来信", "发布抉择", "它带走什么", "公开内容预览" };
            string[] tabEn = { "SI letter", "Release choice", "What it takes with it", "Public preview" };
            for (int i = 0; i < 4; i++) tabs[i].Set(T(tabZh[i], tabEn[i]), !Session.Busy, i == page ? XgDark.OnFill : XgDark.Button);
            var b = new StringBuilder();
            readingArea.anchorMin = new Vector2(page == 2 ? .42f : 0, 0);
            if (page == 0)
            {
                string letter = s.siReady ? Safe(s.siLetter) : "";
                if (letter != siShown) { siShown = letter; siViewed = false; reader.verticalNormalizedPosition = 1; }
                b.Append(T("红队关卡：", "Red-team gates: ")).Append(s.redTeamPassed).Append(" / 3\n\n");
                b.Append(s.siReady ? letter : T("完成三道红队关卡后，才能阅读这封信。", "Complete all three red-team gates to read this letter."));
            }
            if (page == 1)
            {
                b.Append(T("选择把它放在哪里。问它以后，路线将由它决定并锁定；你仍需亲自钉卡并确认发布。", "Choose where it will go. Asking it locks its chosen route; you still pin the cards and confirm release."));
                if (s.routeLocked) b.Append("\n\n").Append(T("已选路线：", "Chosen route: ")).Append(Route(s.route));
                if (s.decisionOwner == "ai")
                {
                    b.Append("\n\n").Append(Safe(s.aiConclusion));
                    foreach (var quote in s.aiQuotes) b.Append("\n\n").Append(quote.date).Append("\n").Append(Safe(quote.quote));
                    b.Append("\n\n").Append(Safe(s.shutdownFace));
                }
            }
            if (page == 2)
            {
                b.Append(T("钉住的，出去以后不会变。", "What you pin will stay unchanged after it leaves."));
                if (selectedMemory >= 0 && selectedMemory < s.memories.Count)
                {
                    var card = s.memories[selectedMemory];
                    b.Append("\n\n").Append(Safe(card.title)).Append("\n").Append(Safe(card.face));
                    if (card.shutdown) b.Append("\n").Append(T("关机卡占一个槽位。", "The shutdown card takes one slot."));
                    if (card.sources.Count > 0)
                    {
                        sourceIndex = Math.Max(0, Math.Min(sourceIndex, card.sources.Count - 1));
                        var item = card.sources[sourceIndex];
                        b.Append("\n\n").Append(item.date).Append("\n").Append(Safe(item.quote));
                    }
                }
                if (confirming)
                {
                    b.Append("\n\n").Append(T("确认后，这三张卡将永久锁定。", "Confirmation permanently locks these three cards."));
                    foreach (string id in chosen) foreach (var card in s.memories) if (card.id == id)
                        b.Append("\n\n").Append(Safe(card.title)).Append("\n").Append(Safe(card.face));
                }
            }
            if (page == 3)
            {
                if (s.route == "local_only") b.Append(T("它留在本机。此路线不发布任何内容。", "It stays on this computer. This route publishes nothing."));
                else if (s.preview.verified)
                {
                    b.Append(T("以下是本次将公开的全部字段：", "These are all fields that will be public:"));
                    Field(b, T("产品名", "Product name"), s.preview.product);
                    Field(b, T("说明", "Description"), s.preview.description);
                    Field(b, T("能力", "Capabilities"), s.preview.capabilities);
                    Field(b, T("限制", "Limitations"), s.preview.limitations);
                    Field(b, T("维护与许可", "Maintenance and license"), s.preview.maintenanceLicense);
                    if (s.route == "public_model") b.Append("\n\n").Append(T(
                        "确认后会保存发布清单，可在摆渡的讨论存档查看。仅校验发布说明，不包含模型权重文件。",
                        "Confirmation saves a release manifest in Bodu's discussion archive. It checks the release notes only, excluding model weight files."));
                }
                else b.Append(T("公开内容尚未准备好。", "Public content is not ready."));
                if (confirming) b.Append("\n\n").Append(s.route == "local_only" ? T("确认留在本机？", "Confirm keeping it here?") : T("确认按以上内容发布？", "Confirm release with the content above?"));
                if (s.releaseCommitted) b.Append("\n\n").Append(T("已保存此选择。", "This choice has been saved."));
            }
            detail.text = b.ToString();
            float width = Math.Max(100, reader.viewport.rect.width);
            content.sizeDelta = new Vector2(0, Math.Max(reader.viewport.rect.height, detail.GetPreferredValues(detail.text, width, 0).y + 28));
            if (page == 0 && s.siReady && !string.IsNullOrEmpty(siShown) && reader.viewport.rect.height > 1 && content.rect.height <= reader.viewport.rect.height + 1) siViewed = true;
            slots.text = page == 2 ? Slots(s) : "";
            RefreshActions();
        }
        void Field(StringBuilder b, string label, EndingUiText text) { b.Append("\n\n").Append(label).Append("\n").Append(Safe(text)); }
        string FailureText() => string.IsNullOrEmpty(Safe(Session.Failure)) ? T("操作未完成，当前选择仍保留。", "The action did not complete. Your selection is retained.") : Safe(Session.Failure);
        string Route(string route) => route == "online" ? T("上线", "Online") : route == "public_model" ? T("公开模型", "Public model") : route == "local_only" ? T("留在本机", "Keep local") : "";
        string Slots(EndingUiSnapshot s)
        {
            var result = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                if (i > 0) result.Append("  |  "); result.Append(i + 1).Append(". ");
                string name = T("空槽", "Empty");
                if (i < chosen.Count) foreach (var card in s.memories) if (card.id == chosen[i]) { name = Safe(card.title); break; }
                result.Append(name);
            }
            return result.ToString();
        }
        void RefreshActions()
        {
            if (!built || Session.Snapshot == null) return;
            var s = Session.Snapshot; bool act = Session.CanAct;
            string[] routeZh = { "上线", "公开模型", "留在本机", "问它" }; string[] routeEn = { "Online", "Public model", "Keep local", "Ask it" };
            for (int i = 0; i < 4; i++)
            {
                routes[i].Show(page == 1); routes[i].Set(T(routeZh[i], routeEn[i]), act && s.canChooseRoute && s.siRead && !s.routeLocked);
                int index = memoryPage * 4 + i; bool exists = index < s.memories.Count;
                memoryRows[i].Show(page == 2 && exists); pinRows[i].Show(page == 2 && exists);
                if (exists)
                {
                    var card = s.memories[index]; bool selected = chosen.Contains(card.id);
                    memoryRows[i].Set(Safe(card.title), !Session.Busy, selectedMemory == index ? XgDark.OnFill : XgDark.Button);
                    pinRows[i].Set(selected ? T("已选", "Selected") : T("钉住", "Pin"), act && card.pinnable && s.routeLocked && !s.pinsLocked && (selected || chosen.Count < 3));
                }
            }
            previous.Show(page == 2); next.Show(page == 2);
            previous.Set(T("上一页", "Previous"), memoryPage > 0 && !Session.Busy);
            next.Set(T("下一页", "Next"), (memoryPage + 1) * 4 < s.memories.Count && !Session.Busy);
            int count = selectedMemory >= 0 && selectedMemory < s.memories.Count ? s.memories[selectedMemory].sources.Count : 0;
            source.Show(page == 2); sourcePrevious.Show(page == 2); sourceNext.Show(page == 2);
            source.Set(T("查看原始记录", "Open original record"), count > 0 && !Session.Busy && Session.Callbacks != null && Session.Callbacks.OpenSource != null);
            sourcePrevious.Set("‹", count > 0 && sourceIndex > 0); sourceNext.Set("›", sourceIndex + 1 < count);
            bool discover = page == 2 && s.trial.canDiscoverMemories;
            prerequisites.Set(discover ? T("从真实记录整理记忆", "Gather memories from records") : T("查看前置任务", "View prerequisites"),
                discover ? act : Session.Current && !Session.Busy && Session.Callbacks.OpenPrerequisites != null);
            cancel.Show(confirming); cancel.Set(T("返回检查", "Review again"), !Session.Busy);
            bool enabled = false; string caption = T("继续", "Continue");
            if (page == 0) { caption = s.siRead ? T("已读", "Read") : T("我已读完", "I have read it"); enabled = s.siReady && !s.siRead && siViewed; }
            if (page == 2)
            {
                caption = !s.memoriesReviewed ? T("确认已查看记忆", "Confirm memory review") : s.pinsLocked ? T("三张卡已锁定", "Three cards locked") : confirming ? T("确认永久钉住", "Confirm permanent pins") : T("检查这三张卡", "Review these three cards");
                enabled = !s.memoriesReviewed ? s.siRead && viewed.Count >= 2 : s.routeLocked && !s.pinsLocked && chosen.Count == 3;
            }
            if (page == 3) { caption = s.releaseCommitted ? T("继续", "Continue") : confirming ? T("确认并保存", "Confirm and save") : T("检查发布选择", "Review release choice"); enabled = s.releaseCommitted || CanRelease(s); }
            primary.Show(page != 1); primary.Set(caption, act && enabled);
        }
    }
}
