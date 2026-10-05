using System;
using System.Text;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 诊断: the standing wall as an investigation (look at mistakes → guess → cheap trial → independent exam).
    /// Groups and mistakes come from the real diagnostic cards; trials run on copies and change nothing real.
    /// </summary>
    public sealed partial class XgWallPage : XgPage
    {
        TMP_Text header, exam, diagnosis, mistakes, trialText, hints, footer;
        XgBtn trialButton, hintButton, editButton, applyButton, cancelButton;
        XgTrialResult lastTrial;
        XgTrialDraft draft;
        RectTransform trialPanel, proposalPanel, trialContent;
        TMP_Text trialStatus;
        XgBtn[] proposalButtons;
        bool expanded, confirmDepth, currentDraft;
        float nextDraftCheck, applyReadyAt;
        bool applyInputReleased;
        int applyReleasedFrame;
        XgTrialDraft checkedDraft;
        XgSim checkedOwner;
        string trialFor = "", trialError = "";

        public override void Build(RectTransform area)
        {
            var card = ui.Card(area, "wall", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root = card;
            header = ui.Text(Strip("Header", card, 8, 30, 16, 16), "", 18, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            header.fontStyle = FontStyles.Bold;
            // Two views share the page: the wall investigation and the 训练图式 (XgWallPage.Graph.cs).
            var view = Rect("DiagnosisView", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            diagnosisView = view;
            BuildGraph(card);
            exam = ui.Text(Strip("Exam", view, 40, 24, 16, 16), "", 14, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);

            var left = Rect("Diagnosis", view, new Vector2(0, .36f), new Vector2(.5f, 1), new Vector2(12, 0), new Vector2(-6, -72));
            Panel(left, XgPalette.Page);
            diagnosis = ui.Text(Rect("Text", left, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8)), "", 14, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            diagnosis.enableAutoSizing = true; diagnosis.fontSizeMin = 10; diagnosis.fontSizeMax = 14;

            var middle = Rect("Mistakes", view, new Vector2(0, 0), new Vector2(.5f, .36f), new Vector2(12, 12), new Vector2(-6, -6));
            Panel(middle, XgPalette.Page);
            mistakes = ui.Text(Rect("Text", middle, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8)), "", 13, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            mistakes.enableAutoSizing = true; mistakes.fontSizeMin = 9; mistakes.fontSizeMax = 13;

            var right = Rect("Trial", view, new Vector2(.5f, .36f), new Vector2(1, 1), new Vector2(6, 0), new Vector2(-12, -72));
            Panel(right, XgPalette.Page);
            trialPanel = right;
            editButton = ui.Button(right, "", EditDraft, 12);
            trialButton = ui.Button(right, "", RunTrial, 12);
            applyButton = ui.Button(right, "", ApplyDraft, 12);
            cancelButton = ui.Button(right, "", () => { CancelDraft(); Refresh(); }, 12);
            var actions = new[] { editButton, trialButton, applyButton, cancelButton };
            for (int i = 0; i < actions.Length; i++)
            {
                actions[i].rt.anchorMin = new Vector2(i / 4f, 1); actions[i].rt.anchorMax = new Vector2((i + 1) / 4f, 1);
                actions[i].rt.offsetMin = new Vector2(5, -40); actions[i].rt.offsetMax = new Vector2(-5, -8);
            }
            trialStatus = ui.Text(Strip("Status", right, 44, 60, 10, 10), "", 12, XgPalette.Muted, TextAlignmentOptions.TopLeft);
            trialStatus.enableAutoSizing = true; trialStatus.fontSizeMin = 10; trialStatus.fontSizeMax = 12;
            var scroll = Rect("TrialScroll", right, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -108));
            var viewport = Rect("Viewport", scroll, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(viewport, XgPalette.Page); viewport.gameObject.AddComponent<RectMask2D>();
            trialContent = Rect("Content", viewport, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
            trialContent.pivot = new Vector2(.5f, 1);
            var scroller = scroll.gameObject.AddComponent<ScrollRect>(); scroller.viewport = viewport; scroller.content = trialContent;
            scroller.horizontal = false; scroller.vertical = true; scroller.scrollSensitivity = 24; scroller.movementType = ScrollRect.MovementType.Clamped;
            proposalPanel = Strip("Proposal", trialContent, 0, 280, 0, 0);
            proposalButtons = new XgBtn[10];
            for (int i = 0; i < proposalButtons.Length; i++)
            {
                int index = i;
                proposalButtons[i] = ui.Button(proposalPanel, "", () => ChangeProposal(index), 12);
                Place(proposalButtons[i], Strip("Knob", proposalPanel, i * 28, 25, 4, 4));
            }
            trialText = ui.Text(Strip("Result", trialContent, 0, 285, 6, 6), "", 13, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            trialText.enableAutoSizing = true; trialText.fontSizeMin = 10; trialText.fontSizeMax = 13;
            root.gameObject.AddComponent<XgTrialPageLifetime>().Disabled = CancelDraft;

            var bottom = Rect("Hints", view, new Vector2(.5f, 0), new Vector2(1, .36f), new Vector2(6, 12), new Vector2(-12, -6));
            Panel(bottom, XgPalette.Page);
            hintButton = ui.Button(bottom, "", () => { var w = Sim.ActiveWall; if (w != null && Sim.RevealHint(w.id)) { Fx.Play(XgJuice.Sfx.Id.Tick); Refresh(); } }, 13);
            hintButton.rt.anchorMin = new Vector2(1, 1); hintButton.rt.anchorMax = new Vector2(1, 1);
            hintButton.rt.offsetMin = new Vector2(-150, -38); hintButton.rt.offsetMax = new Vector2(-10, -8);
            hints = ui.Text(Rect("Text", bottom, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-160, -8)), "", 13, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            hints.enableAutoSizing = true; hints.fontSizeMin = 9; hints.fontSizeMax = 13;
            footer = ui.Text(Rect("Footer", bottom, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-150, 6), new Vector2(-10, 60)), "", 11, XgPalette.Muted, TextAlignmentOptions.BottomRight);
        }

        XgRun RunOf(XgWall wall, out XgWallCheck check)
        {
            check = null;
            foreach (var c in wall.checks)
            {
                if (Sim.WallCheckPassed(wall, c)) continue;
                check = c;
                var d = XgCatalog.Dataset(c.dataset);
                return Sim.Run(d != null ? d.track : XgTrack.Vision);
            }
            check = wall.checks.Length > 0 ? wall.checks[0] : null;
            return Sim.Selected;
        }

        void CancelDraft()
        {
            draft = null; lastTrial = null; trialFor = trialError = ""; expanded = confirmDepth = currentDraft = false; checkedDraft = null; nextDraftCheck = 0;
        }

        public override void Tick(float dt)
        {
            PollDepthApply();
            if (draft != null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { CancelDraft(); Refresh(); }
        }

        void EditDraft()
        {
            var wall = Sim.ActiveWall; if (wall == null) return;
            var run = RunOf(wall, out _);
            if (run.running) { Sim.SetAutoTrain((XgTrack)run.track, false); Refresh(); return; }
            if (run.epochActive) return;
            if (draft == null || !Sim.IsTrialCurrent(draft))
            {
                CancelDraft(); draft = Sim.BeginTrialDraft((XgTrack)run.track); expanded = true;
            }
            else expanded = !expanded;
            Refresh();
        }

        void RunTrial()
        {
            var wall = Sim.ActiveWall; if (wall == null) return;
            var run = RunOf(wall, out _);
            if (run.epochActive || run.running) return;
            // An expired result is never silently treated as current: pressing Trial explicitly starts afresh.
            if (draft == null || !Sim.IsTrialCurrent(draft))
            { CancelDraft(); draft = Sim.BeginTrialDraft((XgTrack)run.track); }
            try
            {
                lastTrial = Sim.Trial(draft); trialFor = draft.dataset; trialError = ""; confirmDepth = false; expanded = false;
                Fx.Play(XgJuice.Sfx.Id.Stamp);
            }
            catch (ArgumentException) { trialError = T("方案参数无效，请取消后重新调整。", "Invalid proposal. Cancel and adjust again."); }
            Refresh();
        }

        void ApplyDraft()
        {
            if (draft == null || lastTrial == null) return;
            if (!Sim.IsTrialCurrent(draft)) { confirmDepth = false; Refresh(); return; }
            if (draft.proposal.depth != Sim.Run(draft.track).depth && !confirmDepth)
            { confirmDepth = true; applyInputReleased = false; applyReadyAt = Time.unscaledTime + .5f; Refresh(); return; }
            if (confirmDepth && !DepthApplyReady()) return;
            if (Sim.TryApplyTrial(draft, Host, out var reason))
            { CancelDraft(); trialError = T("方案已应用；尚未开始正式训练。", "Applied. Real training has not started."); Fx.Play(XgJuice.Sfx.Id.Stamp); }
            else { trialError = reason; confirmDepth = false; }
            Refresh();
        }

        void PollDepthApply()
        {
            if (!confirmDepth) return;
            if (!DepthApplyActivationHeld())
            {
                if (!applyInputReleased) { applyInputReleased = true; applyReleasedFrame = Time.frameCount; }
            }
            else if (!DepthApplyReady()) applyInputReleased = false;
            applyButton.button.interactable = currentDraft && DepthApplyReady();
        }

        static bool DepthApplyActivationHeld()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && (keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed || keyboard.spaceKey.isPressed)
                || Mouse.current != null && Mouse.current.leftButton.isPressed;
        }

        bool DepthApplyReady() => applyInputReleased && Time.frameCount > applyReleasedFrame && Time.unscaledTime >= applyReadyAt;

        void ChangeProposal(int index)
        {
            if (draft == null || !Sim.IsTrialCurrent(draft)) return;
            var p = draft.proposal;
            switch (index)
            {
                case 0:
                    var archs = new List<XgArch>();
                    foreach (var a in XgCatalog.Archs) if (XgSim.ArchitectureFits(a, draft.track) && Sim.Has(a.id)) archs.Add(a);
                    int at = archs.FindIndex(a => a.id == p.arch);
                    if (archs.Count > 0) p.arch = archs[(at + 1) % archs.Count].id;
                    break;
                case 1: p.depth = p.depth % Sim.DepthCap(draft.track) + 1; break;
                case 2: p.width = (p.width + 1) % (Sim.WidthCap(draft.track) + 1); break;
                case 3: if (Sim.HasLrKnob(draft.track)) p.lr = (p.lr + 1) % XgSim.RateValues.Length; break;
                case 4: for (int i = 1; i <= 3; i++) if (Sim.ActivationOwned((p.act + i) % 3)) { p.act = (p.act + i) % 3; break; } break;
                case 5: if (Sim.ClipOwned) p.clip = !p.clip; break;
                case 6: if (Sim.SkipOwned) p.skip = !p.skip; break;
                case 7: if (Sim.PositionOwned) p.position = !p.position; break;
                case 8: if (Sim.WarmupOwned) p.warmup = !p.warmup; break;
                case 9: if (Sim.AttentionOnlyOwned) p.attnOnly = !p.attnOnly; break;
            }
            lastTrial = null; confirmDepth = false; trialError = ""; Refresh();
        }

        string DepthApplyWarning(XgRun run)
        {
            bool deferred = run.formal != null && run.formal.set;
            string effect = deferred && draft.proposal.depth == run.formal.depth
                ? T("恢复已训练的层数，不会重置成果。", "Restores the trained depth without resetting learned concepts.")
                : deferred ? T("下一轮正式训练将重置该板块的学习成果（保留种子）。", "The next real epoch resets this region's learned concepts (seeds survive).")
                : T("应用后立即重建该板块，学习成果会重置（保留种子）。", "Applying immediately rebuilds this region, resetting learned concepts (seeds survive).");
            return effect + T("再次点「确认应用」，或取消。", " Confirm again, or cancel.");
        }

        void RefreshDraft(XgRun run)
        {
            bool busy = run.epochActive || run.running;
            if (checkedDraft != draft || checkedOwner != Sim || Time.unscaledTime >= nextDraftCheck)
            {
                currentDraft = draft != null && Sim.IsTrialCurrent(draft);
                checkedDraft = draft; checkedOwner = Sim; nextDraftCheck = Time.unscaledTime + .5f;
            }
            bool current = currentDraft && !busy;
            if (!current) confirmDepth = false;
            editButton.Set(run.running ? T("停止自动训练", "Stop auto") : draft != null && !current ? T("重新调整", "New draft") : expanded ? T("收起方案", "Fold") : T("调整方案", "Adjust"), run.running || !run.epochActive);
            trialButton.Set(T("试训（免费）", "Trial (free)"), !busy, XgPalette.AccentSoft, XgPalette.Accent);
            applyButton.Set(confirmDepth ? T("确认应用", "Confirm") : T("应用方案", "Apply"), current && lastTrial != null && (!confirmDepth || DepthApplyReady()));
            cancelButton.Set(T("取消", "Cancel"), draft != null);
            trialStatus.text = busy ? T("先停止自动训练，等本轮结束；这里只停止当前线路。", "Stop auto-training and wait for this epoch; only this track is stopped.")
                : draft != null && !current ? T("结果已过期，请重新试训。模型、数据或正式设置已变化；旧方案不可应用。", "Result expired. Model, data or live settings changed. Start a new trial before applying.")
                : confirmDepth ? DepthApplyWarning(run)
                : trialError.Length > 0 ? trialError : T("草稿不改正式设置。点参数循环选择，试训只训练副本；取消不丢任何成果。", "Draft only. Click a parameter to cycle; trials train copies. Cancel loses no progress.");
            trialStatus.color = confirmDepth || draft != null && !current || trialError.Length > 0 ? XgPalette.Bad : XgPalette.Muted;
            bool show = draft != null && expanded;
            proposalPanel.gameObject.SetActive(show);
            trialContent.sizeDelta = new Vector2(0, show ? 575 : 285);
            trialText.rectTransform.offsetMin = new Vector2(6, -(show ? 575 : 285));
            trialText.rectTransform.offsetMax = new Vector2(-6, show ? -290 : 0);
            if (!show) return;
            var p = draft.proposal; var arch = XgCatalog.Arch(p.arch);
            string On(bool on) => on ? T("开", "on") : T("关", "off");
            string[] names = { T("架构：", "Architecture: ") + T(arch.name, arch.nameEn), T("层数：", "Depth: ") + p.depth,
                T("宽度：", "Width: ") + XgCatalog.Widths[p.width], T("学习率：", "Rate: ") + Sim.RateLabel(p.lr),
                T("激活：", "Activation: ") + new[] { T("阶跃", "Step"), T("S 形", "Sigmoid"), "ReLU" }[p.act],
                T("梯度裁剪：", "Clipping: ") + On(p.clip), T("跨层直连：", "Skip: ") + On(p.skip),
                T("位置标记：", "Position: ") + On(p.position), T("预热：", "Warm-up: ") + On(p.warmup), T("只用注意力：", "Attention only: ") + On(p.attnOnly) };
            bool[] owned = { true, Sim.DepthCap(draft.track) > 1, Sim.WidthCap(draft.track) > 0, Sim.HasLrKnob(draft.track), true,
                Sim.ClipOwned, Sim.SkipOwned, Sim.PositionOwned, Sim.WarmupOwned, Sim.AttentionOnlyOwned };
            for (int i = 0; i < names.Length; i++) proposalButtons[i].Set(names[i] + (owned[i] ? "  ›" : T("（未解锁）", " (locked)")), current && owned[i]);
        }

        static string Bar(double rate)
        {
            int n = (int)Math.Round(rate * 10);
            return "<color=#3B5BDB>" + new string('█', n) + "</color><color=#C9D2E3>" + new string('█', 10 - n) + "</color>";
        }

        public override void Refresh()
        {
            if (header == null) return;
            var wall = Sim.ActiveWall;
            if (ShowGraph(wall)) return;
            if (wall == null)
            {
                header.text = T("现在没有墙", "No wall right now"); exam.text = diagnosis.text = mistakes.text = trialText.text = hints.text = footer.text = "";
                trialPanel.gameObject.SetActive(false); hintButton.Show(false); return;
            }
            var run = RunOf(wall, out var check);
            if (draft != null && (draft.track != (XgTrack)run.track || draft.dataset != run.dataset)) CancelDraft();
            var k = Sim.Knobs(run);
            var data = XgCatalog.Dataset(run.dataset);
            header.text = T("墙 · ", "Wall · ") + T(wall.name, wall.nameEn) + "  <size=13><color=#68748C>" + T("第 " + wall.stage + " 阶段", "stage " + wall.stage) + "</color></size>";

            var sb = new StringBuilder();
            foreach (var c in wall.checks)
            {
                var d = XgCatalog.Dataset(c.dataset);
                bool done = Sim.WallCheckPassed(wall, c);
                double acc = d != null ? Sim.ExamAccuracy(c, Sim.Run(d.track)) : 0;
                sb.Append(done ? "<color=#2F9E44>✓ " : "○ ").Append(T("考核", "Exam")).Append(" · ").Append(d != null ? T(d.name, d.nameEn) : c.dataset)
                  .Append(c.minDistance > 0 ? T("（没见过的远距离题）", " (unseen far clues)") : T("（没见过的变体）", " (unseen variants)"))
                  .Append("  ").Append(XgSim.Pct(acc)).Append(" / ").Append(XgSim.Pct(c.target)).Append(done ? "</color>" : "").Append("    ");
            }
            exam.text = sb.ToString();

            if (check != null && run.dataset != check.dataset && check.dataset != "*vision")
            {
                diagnosis.text = T("训练页现在练的是「", "The training page is on \"") + (data != null ? T(data.name, data.nameEn) : run.dataset)
                    + T("」。把数据集换成「", "\". Switch the dataset to \"") + T(XgCatalog.Dataset(check.dataset).name, XgCatalog.Dataset(check.dataset).nameEn) + T("」再诊断。", "\" to diagnose it.");
                mistakes.text = ""; trialText.text = ""; trialPanel.gameObject.SetActive(false);
            }
            else
            {
                var dg = Sim.Diagnose(run.dataset, k);
                var d = new StringBuilder("<b>" + T("诊断", "Diagnosis") + "</b>  <size=12><color=#68748C>" + T("诊断题 ", "") + Sim.TestSet(run.dataset).Count + T(" 道，不参与训练 · 当前设置", " diagnostic cards, never trained on · current settings") + "</color></size>\n\n");
                foreach (var g in dg.groups)
                    d.Append(T(g.name, g.nameEn).PadRight(10)).Append("  ").Append(Bar(g.Rate)).Append("  ").Append(g.right).Append(" / ").Append(g.total).Append("\n");
                d.Append("\n<color=#D63031>").Append(T(dg.mainError, dg.mainErrorEn)).Append("</color>");
                diagnosis.text = d.ToString();

                var m = new StringBuilder("<b>" + T("错题样本", "Wrong answers") + "</b>\n");
                if (dg.mistakes.Count == 0) m.Append(T("诊断题全答对了。", "No mistakes on the diagnostic cards."));
                foreach (var x in dg.mistakes)
                    m.Append("\n").Append(x.text).Append("\n<size=11><color=#68748C>").Append(T("模型：", "Model: ")).Append(x.answer ? T("是", "yes") : T("否", "no"))
                     .Append(T(" · 正确：", " · right: ")).Append(x.truth ? T("是", "yes") : T("否", "no"))
                     .Append(x.distance >= 0 ? T(" · 条件离问题 ", " · clue ") + x.distance + T(" 字", " characters away") : "").Append("</color></size>\n");
                mistakes.text = m.ToString();
                trialPanel.gameObject.SetActive(true);
                RefreshDraft(run);
                trialText.text = TrialText(run);
            }

            int level = Sim.HintLevel(wall.id), max = XgSim.Hints.TryGetValue(wall.id, out var all) ? all.Length / 2 : 0;
            var h = new StringBuilder("<b>" + T("提示", "Hints") + "</b>  <size=12><color=#68748C>" + level + " / " + max + "</color></size>\n");
            for (int i = 1; i <= level; i++) h.Append(i).Append(". ").Append(Sim.HintText(wall.id, i)).Append("\n");
            if (level == 0) h.Append(T("先看左边的诊断和错题，自己猜猜问题在哪。", "Read the diagnosis and the wrong answers first; guess where the problem is."));
            hints.text = h.ToString();
            hintButton.Show(max > 0);
            hintButton.Set(level < max ? T("下一条提示", "Next hint") : T("提示已全部给出", "All hints shown"), level < max);
            footer.text = (wall.secret.Length > 0 ? (Sim.Has(wall.secret) ? T("已看过参考解法（秘籍）", "Reference seen (secret)") : T("参考解法在科技的秘籍里", "The reference is the secret in the tree")) : "")
                + "\n" + T("没看参考解法就过墙 = 自悟", "Pass without the reference = insight");
        }

        string TrialText(XgRun run)
        {
            if (draft != null && !currentDraft) return T("结果已过期，请重新试训。旧对比不代表当前模型。", "Result expired. The old comparison no longer represents the current model.");
            if (lastTrial == null || trialFor != run.dataset)
                return T("在本页展开「调整方案」，编辑草稿后试训：两个副本从同一个冻结快照出发，用同样的 ", "Expand Adjust here, edit the draft, then run a trial: two copies start from the same frozen snapshot, see the same ")
                    + XgSim.TrialCards + T(" 张训练卡，在同一套诊断题上比较。", " training cards and are compared on the same diagnostic cards.");
            var t = lastTrial;
            var sb = new StringBuilder();
            if (t.sameSettings) sb.Append(T("设置没变：这是“照现在这样继续练 ", "Settings unchanged: what ")).Append(t.cards).Append(T(" 张卡”的预测。", " more cards would do.")).Append("\n\n");
            else
            {
                sb.Append(T("本次改动：", "Changes: ")).Append(string.Join(T("；", "; "), t.changes)).Append("\n");
                if (t.changes.Count > 1) sb.Append("<color=#C88A00>").Append(T("一次改了 ", "")).Append(t.changes.Count).Append(T(" 项，无法确定是哪项带来的变化。", " changes at once: you cannot tell which one made the difference.")).Append("</color>\n");
                sb.Append("\n");
            }
            sb.Append(T("组别", "Group").PadRight(12)).Append(T("现在", "Now")).Append("    ").Append(T("原设置", "Before")).Append("    ").Append(t.sameSettings ? "" : T("新设置", "After")).Append("\n");
            foreach (var g in t.now.groups)
            {
                var b = t.baseline.Group(g.id); var p = t.proposal.Group(g.id);
                sb.Append(T(g.name, g.nameEn).Split('（')[0].PadRight(10)).Append("  ").Append(g.right).Append("/").Append(g.total)
                  .Append("  →  ").Append(b.right).Append("/").Append(b.total);
                if (!t.sameSettings)
                {
                    int diff = p.right - b.right;
                    sb.Append("  →  ").Append(diff > 0 ? "<color=#2F9E44>" : diff < 0 ? "<color=#D63031>" : "").Append(p.right).Append("/").Append(p.total).Append(diff != 0 ? "</color>" : "");
                }
                sb.Append("\n");
            }
            sb.Append("\n<size=11><color=#68748C>").Append(T("试训只是预测：正式训练仍要投入相应的时间和电费，成绩才属于你的模型。", "A trial is only a forecast: real training still costs its time and power before the result belongs to your model.")).Append("</color></size>");
            return sb.ToString();
        }
    }

    /// <summary>Closing the window or changing tabs discards only the local experiment.</summary>
    public sealed class XgTrialPageLifetime : MonoBehaviour
    {
        public Action Disabled;
        void OnDisable() { Disabled?.Invoke(); }
    }
}
