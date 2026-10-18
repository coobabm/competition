using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.CaseMenu
{
    public sealed partial class CaseMenuHud
    {
        int endingViewEpoch;
        static XgState EndingState(string json)
        {
            try { return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<XgState>(json); }
            catch (Exception) { return null; }
        }
        static bool BeforeEndingChoice(string json) => XgEndingArchiveRules.BeforeChoice(EndingState(json));
        static string EndingRoute(string route) => route == "online" ? T("在线服务", "Online service")
            : route == "public_model" ? T("公开模型", "Public model") : route == "local_only" ? T("仅留本机", "Local only") : T("未知", "Unknown");
        static string EndingDescription(XgState state)
        {
            if (XgEndingArchiveRules.Completed(state)) return T("已通关 · ", "Completed · ") + EndingRoute(state.releaseOutcome.route);
            if (XgEndingArchiveRules.LegacyCompleted(state)) return T("旧结局已通关 · ", "Legacy ending completed · ") + state.ending;
            return T("尚未通关", "In progress");
        }
        public bool OpenCompletedEnding()
        {
            var rt = titleRuntime?.Invoke();
            var state = EndingState(rt?.Sim?.S.labState);
            if (!XgEndingArchiveRules.Completed(state) && !XgEndingArchiveRules.LegacyCompleted(state)) return false;
            OpenEndingArchives();
            return true;
        }
        bool EndingCurrent(ChapterOneRuntime rt, ChapterOneSim save, string lab, int epoch)
            => rt != null && ReferenceEquals(titleRuntime?.Invoke(), rt) && ReferenceEquals(rt.Sim, save)
                && save.S.labState == lab && epoch == endingViewEpoch && IsArchiveOpen;

        RectTransform EndingCard(string headingText)
        {
            CloseArchives();
            if (archiveGroup != null)
            {
                if (Application.isPlaying) Destroy(archiveGroup.gameObject); else DestroyImmediate(archiveGroup.gameObject);
            }
            var backdrop = Panel("TitleEndings", canvasRoot, new Color(0, 0, 0, .86f), true); Stretch(backdrop.rectTransform);
            archiveGroup = backdrop.gameObject.AddComponent<CanvasGroup>();
            var card = Panel("EndingArchiveCard", backdrop.transform, Surface, true);
            Place(card.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1040, 780));
            var heading = Label("Heading", card.transform, headingText, 28, Ink);
            Place(heading.rectTransform, new Vector2(0, 1), new Vector2(30, -20), new Vector2(980, 50));
            archiveStatus = Label("Status", card.transform, "", 17, Muted);
            Place(archiveStatus.rectTransform, new Vector2(0, 0), new Vector2(30, 100), new Vector2(980, 95));
            archiveStatus.textWrappingMode = TextWrappingModes.Normal; archiveStatus.richText = false;
            return card.rectTransform;
        }
        Button EndingButton(Transform card, string name, string caption, float y, Action action)
        {
            var button = ActionButton(card, name, caption, new Vector2(-30, y), action);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(980, 52);
            var text = button.GetComponentInChildren<TMP_Text>();
            text.richText = false; text.enableAutoSizing = true; text.fontSizeMin = 14; text.fontSizeMax = 18;
            return button;
        }
        void EndingFocus(params Button[] buttons)
        {
            var enabled = new List<Button>();
            foreach (var b in buttons) if (b != null && b.interactable) enabled.Add(b);
            for (int i = 0; i < enabled.Count; i++) Link(enabled[i], enabled[(i + enabled.Count - 1) % enabled.Count], enabled[(i + 1) % enabled.Count]);
            if (enabled.Count > 0) EventSystem.current?.SetSelectedGameObject(enabled[enabled.Count - 1].gameObject);
        }
        public void OpenEndingArchives()
        {
            if (titleActions == null || !titleActions.interactable || IsSettingsOpen) return;
            var rt = titleRuntime?.Invoke(); var save = rt?.Sim;
            if (save == null) { ShowError(T("桌面正在准备，请稍后打开档案。", "The desktop is preparing. Open the archive shortly.")); return; }
            string lab = save.S.labState;
            var state = EndingState(lab);
            bool completed = XgEndingArchiveRules.Completed(state), legacy = XgEndingArchiveRules.LegacyCompleted(state);
            var card = EndingCard(T("结局档案", "Endings")); int epoch = endingViewEpoch;
            Func<bool> current = () => EndingCurrent(rt, save, lab, epoch);
            var description = Label("CurrentEnding", card, EndingDescription(state)
                + (string.IsNullOrEmpty(save.S.endingBranchId) ? T(" · 当前主档", " · Current primary save") : T(" · 独立分支副本 ", " · Independent branch ") + save.S.endingBranchId.Substring(0, Math.Min(8, save.S.endingBranchId.Length))), 20, Ink);
            description.richText = false;
            Place(description.rectTransform, new Vector2(0, 1), new Vector2(30, -88), new Vector2(980, 70));
            var replay = EndingButton(card, "ReplayEnding", legacy ? T("查看旧结局记录", "View legacy ending record") : T("回看已完成结局", "Replay completed ending"), 550,
                () => { if (current()) ShowEndingReplay(rt, save, lab, state); });
            replay.interactable = completed || legacy;
            string checkpoint = save.S.endingCheckpointId;
            var branch = EndingButton(card, "CreateEndingBranch", T("从选择前继续 · 建立独立分支副本", "Continue before the choice · Create independent branch"), 470, () =>
            {
                if (!current()) return;
                if (!rt.CreateEndingBranch(save, checkpoint, BeforeEndingChoice)) { archiveStatus.text = rt.SaveStatus; return; }
                CloseArchives(); archiveStart?.Invoke();
            });
            branch.interactable = (completed || legacy) && rt.HasEndingChoice(save, BeforeEndingChoice);
            var letters = EndingButton(card, "EndingPublicLetters", T("三封公开信 · 只读", "Three public letters · Read only"), 390,
                () => { if (current()) ShowEndingLetters(rt, save, lab, state); });
            letters.interactable = completed;
            var copies = EndingButton(card, "EndingBranches", T("继续独立分支副本", "Resume an independent branch"), 310,
                () => { if (current()) ShowEndingBranches(rt, save, lab, 0); });
            var back = EndingButton(card, "CloseEndings", T("返回标题", "Return to title"), 30, CloseArchives);
            archiveStatus.text = T("回看只读取已保存记录；读三封信不会点亮其他路线。分支单独保存，退出后可在此继续。", "Replay only reads saved records. Reading three letters does not complete other routes. Branches save separately and can be resumed here.")
                + (!branch.interactable ? T("\n本档没有可验证的选择前快照，不能建立分支。", "\nThis save has no verified pre-choice snapshot; a branch cannot be created.") : "");
            EndingFocus(replay, branch, letters, copies, back);
        }

        void ShowEndingBranches(ChapterOneRuntime rt, ChapterOneSim save, string lab, int page)
        {
            var rows = rt.EndingBranches();
            int pages = Math.Max(1, (rows.Count + 4) / 5); page = Math.Max(0, Math.Min(page, pages - 1));
            var card = EndingCard(T("独立分支副本", "Independent branches")); int epoch = endingViewEpoch;
            Func<bool> current = () => EndingCurrent(rt, save, lab, epoch);
            var buttons = new List<Button>();
            for (int i = page * 5; i < Math.Min(rows.Count, page * 5 + 5); i++)
            {
                var row = rows[i];
                var button = EndingButton(card, "Branch_" + row.Id, row.Id.Substring(0, 8) + " · " + row.Name + " · " + EndingDescription(EndingState(row.LabJson)), 545 - (i % 5) * 75, () =>
                {
                    if (!current()) return;
                    if (!rt.ResumeEndingBranch(save, row.Id)) { archiveStatus.text = rt.SaveStatus; return; }
                    CloseArchives(); archiveStart?.Invoke();
                });
                buttons.Add(button);
            }
            var prev = ActionButton(card, "PreviousBranches", T("上一页", "Previous"), new Vector2(-510, 105), () => { if (current()) ShowEndingBranches(rt, save, lab, page - 1); });
            var next = ActionButton(card, "NextBranches", T("下一页", "Next"), new Vector2(-270, 105), () => { if (current()) ShowEndingBranches(rt, save, lab, page + 1); });
            prev.interactable = page > 0; next.interactable = page + 1 < pages;
            var back = EndingButton(card, "BackToEndings", T("返回结局档案", "Return to endings"), 30, () => { if (current()) OpenEndingArchives(); });
            Place(archiveStatus.rectTransform, new Vector2(0, 1), new Vector2(30, -84), new Vector2(980, 90));
            archiveStatus.text = (page + 1) + " / " + pages + T(" · 选择一个副本继续；原通关主档保持不变。", " · Select a copy to resume; the original completed primary is unchanged.")
                + (rows.Count == 0 ? T("\n尚无可读取的独立分支。", "\nNo readable independent branches yet.") : "");
            buttons.Add(prev); buttons.Add(next); buttons.Add(back); EndingFocus(buttons.ToArray());
        }

        void ShowEndingReplay(ChapterOneRuntime rt, ChapterOneSim save, string lab, XgState state)
        {
            if (XgEndingArchiveRules.LegacyCompleted(state))
            {
                ShowEndingText(rt, save, lab, T("旧结局记录", "Legacy ending record"), T("已完成的旧结局编号：", "Completed legacy ending ID: ") + state.ending
                    + T("\n\n这份记录保持原编号；旧结局未记录新的发布路线，不推断为在线服务、公开模型或仅留本机。", "\n\nThis record retains its original ID. It does not contain a new publication route and is not mapped to online service, public model or local only."));
                return;
            }
            if (!XgEndingArchiveRules.Completed(state)) return;
            var card = EndingCard(T("回看结局", "Replay ending")); int epoch = endingViewEpoch;
            var release = state.releaseOutcome.Copy(); var ep = state.epilogue.Copy(); ep.phase = "date";
            var area = Rect("ReadOnlyReplay", card); Stretch(area); area.offsetMin = new Vector2(0, 0); area.offsetMax = new Vector2(0, -75);
            var view = XgEpilogueView.Create(area, font);
            Action refresh = null;
            refresh = () =>
            {
                if (!EndingCurrent(rt, save, lab, epoch)) return;
                view.Show(release, ep, GameText.IsEnglish, () =>
                {
                    if (!EndingCurrent(rt, save, lab, epoch)) return;
                    ep.phase = ep.phase == "date" ? "headline" : ep.phase == "headline" ? "route" : "letter";
                    refresh();
                }, () => { if (EndingCurrent(rt, save, lab, epoch)) ShowEndingLetters(rt, save, lab, state); },
                   () => { if (EndingCurrent(rt, save, lab, epoch)) OpenEndingArchives(); }, true);
            };
            refresh();
        }
        void ShowEndingLetters(ChapterOneRuntime rt, ChapterOneSim save, string lab, XgState state)
        {
            var card = EndingCard(T("三封公开信 · 只读", "Three public letters · Read only")); int epoch = endingViewEpoch;
            Func<bool> current = () => EndingCurrent(rt, save, lab, epoch);
            var buttons = new List<Button>();
            try
            {
                var asset = Resources.Load<TextAsset>(EndingLetterCatalog.ResourcePath);
                var catalog = new EndingLetterCatalog(asset == null ? null : JsonUtility.FromJson<EndingLetterResource>(asset.text));
                int row = 0;
                foreach (var article in catalog.Recommended(state?.releaseOutcome?.route))
                {
                    var letter = article;
                    buttons.Add(EndingButton(card, "PublicLetter_" + letter.id, letter.Title(GameText.IsEnglish), 540 - row++ * 100, () =>
                    {
                        if (!current()) return;
                        var paragraphs = new List<string>(); foreach (var p in letter.paragraphs) paragraphs.Add(p.Text(GameText.IsEnglish));
                        ShowEndingText(rt, save, lab, letter.Title(GameText.IsEnglish), string.Join("\n\n", paragraphs));
                    }));
                }
                archiveStatus.text = T("公开文章 · 2017-06-13。此处阅读不发送邮件、不修改阅读或通关记录。", "Public articles · 2017-06-13. Reading here sends no mail and changes no reading or completion records.");
            }
            catch (Exception) { archiveStatus.text = T("公开信资源无法读取。存档未改变。", "The public letters could not be read. The save is unchanged."); }
            buttons.Add(EndingButton(card, "BackToEndings", T("返回结局档案", "Return to endings"), 30, () => { if (current()) OpenEndingArchives(); }));
            EndingFocus(buttons.ToArray());
        }
        void ShowEndingText(ChapterOneRuntime rt, ChapterOneSim save, string lab, string title, string body)
        {
            var card = EndingCard(title); int epoch = endingViewEpoch;
            archiveStatus.gameObject.SetActive(false);
            var viewport = Panel("ReadOnlyLetter", card, Control, true);
            viewport.rectTransform.anchorMin = Vector2.zero; viewport.rectTransform.anchorMax = Vector2.one;
            viewport.rectTransform.offsetMin = new Vector2(30, 104); viewport.rectTransform.offsetMax = new Vector2(-58, -100);
            viewport.gameObject.AddComponent<RectMask2D>();
            var text = Label("PublicText", viewport.transform, body, 22, Ink); text.richText = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            var content = text.rectTransform; content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            text.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport.rectTransform; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true; scroll.inertia = false; scroll.scrollSensitivity = 36;
            var track = Panel("LetterScrollbar", card, Control, true);
            track.rectTransform.anchorMin = new Vector2(1, 0); track.rectTransform.anchorMax = Vector2.one;
            track.rectTransform.offsetMin = new Vector2(-50, 104); track.rectTransform.offsetMax = new Vector2(-30, -100);
            var handle = Panel("Handle", track.transform, Muted, true); Stretch(handle.rectTransform);
            var scrollbar = track.gameObject.AddComponent<Scrollbar>(); scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle.rectTransform; scrollbar.targetGraphic = handle; scroll.verticalScrollbar = scrollbar;
            var back = EndingButton(card, "BackToEndings", T("返回结局档案", "Return to endings"), 30, () => { if (EndingCurrent(rt, save, lab, epoch)) OpenEndingArchives(); });
            Link(scrollbar, back, back); Link(back, scrollbar, scrollbar); EventSystem.current?.SetSelectedGameObject(back.gameObject);
            Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 1;
        }
    }
}
