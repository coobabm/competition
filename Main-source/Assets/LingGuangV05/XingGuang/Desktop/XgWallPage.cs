using System;
using System.Text;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 诊断: the standing wall as an investigation (look at mistakes → guess → cheap trial → independent exam).
    /// Groups and mistakes come from the real diagnostic cards; trials run on copies and change nothing real.
    /// </summary>
    public sealed class XgWallPage : XgPage
    {
        TMP_Text header, exam, diagnosis, mistakes, trialText, hints, footer;
        XgBtn trialButton, hintButton;
        XgTrialResult lastTrial;
        string trialFor = "";

        public override void Build(RectTransform area)
        {
            var card = ui.Card(area, "wall", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root = card;
            header = ui.Text(Strip("Header", card, 8, 30, 16, 16), "", 18, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            header.fontStyle = FontStyles.Bold;
            exam = ui.Text(Strip("Exam", card, 40, 24, 16, 16), "", 14, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);

            var left = Rect("Diagnosis", card, new Vector2(0, .36f), new Vector2(.5f, 1), new Vector2(12, 0), new Vector2(-6, -72));
            Panel(left, XgPalette.Page);
            diagnosis = ui.Text(Rect("Text", left, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8)), "", 14, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            diagnosis.enableAutoSizing = true; diagnosis.fontSizeMin = 10; diagnosis.fontSizeMax = 14;

            var middle = Rect("Mistakes", card, new Vector2(0, 0), new Vector2(.5f, .36f), new Vector2(12, 12), new Vector2(-6, -6));
            Panel(middle, XgPalette.Page);
            mistakes = ui.Text(Rect("Text", middle, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -8)), "", 13, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            mistakes.enableAutoSizing = true; mistakes.fontSizeMin = 9; mistakes.fontSizeMax = 13;

            var right = Rect("Trial", card, new Vector2(.5f, .36f), new Vector2(1, 1), new Vector2(6, 0), new Vector2(-12, -72));
            Panel(right, XgPalette.Page);
            trialButton = ui.Button(right, "", RunTrial, 14);
            trialButton.rt.anchorMin = new Vector2(0, 1); trialButton.rt.anchorMax = new Vector2(1, 1);
            trialButton.rt.offsetMin = new Vector2(12, -46); trialButton.rt.offsetMax = new Vector2(-12, -8);
            trialText = ui.Text(Rect("Text", right, Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-12, -54)), "", 13, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            trialText.enableAutoSizing = true; trialText.fontSizeMin = 9; trialText.fontSizeMax = 13;

            var bottom = Rect("Hints", card, new Vector2(.5f, 0), new Vector2(1, .36f), new Vector2(6, 12), new Vector2(-12, -6));
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

        void RunTrial()
        {
            var wall = Sim.ActiveWall; if (wall == null) return;
            var run = RunOf(wall, out _);
            if (run.epochActive) { Fx.Play(XgJuice.Sfx.Id.Thud); return; }
            lastTrial = Sim.Trial((XgTrack)run.track);
            trialFor = run.dataset;
            Fx.Play(XgJuice.Sfx.Id.Stamp);
            Refresh();
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
            if (wall == null)
            {
                header.text = T("现在没有墙", "No wall right now"); exam.text = diagnosis.text = mistakes.text = trialText.text = hints.text = footer.text = "";
                trialButton.Show(false); hintButton.Show(false); return;
            }
            var run = RunOf(wall, out var check);
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
                mistakes.text = ""; trialText.text = ""; trialButton.Show(false);
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
                trialButton.Show(true);
                trialButton.Set(T("试训对比（副本，不花钱、不改模型）", "Trial on copies (free; the model is untouched)"), !run.epochActive, XgPalette.AccentSoft, XgPalette.Accent);
                trialText.text = TrialText(run);
            }

            int level = Sim.HintLevel(wall.id), max = XgSim.Hints.TryGetValue(wall.id, out var all) ? all.Length / 2 : 0;
            var h = new StringBuilder("<b>" + T("提示", "Hints") + "</b>  <size=12><color=#68748C>" + level + " / " + max + "</color></size>\n");
            for (int i = 1; i <= level; i++) h.Append(i).Append(". ").Append(Sim.HintText(wall.id, i)).Append("\n");
            if (level == 0) h.Append(T("先看左边的诊断和错题，自己猜猜问题在哪。", "Read the diagnosis and the wrong answers first; guess where the problem is."));
            hints.text = h.ToString();
            hintButton.Show(max > 0);
            hintButton.Set(level < max ? T("下一条提示", "Next hint") : T("提示已全部给出", "All hints shown"), level < max);
            footer.text = (wall.secret.Length > 0 ? (Sim.Has(wall.secret) ? T("已看过参考解法（秘籍）", "Reference seen (secret)") : T("参考解法在技能树的秘籍里", "The reference is the secret in the tree")) : "")
                + "\n" + T("没看参考解法就过墙 = 自悟", "Pass without the reference = insight");
        }

        string TrialText(XgRun run)
        {
            if (lastTrial == null || trialFor != run.dataset)
                return T("改一下训练页的设置，然后按上面的按钮：两个模型副本从同一个快照出发，用同样的 ", "Change a setting on the training page, then press the button: two copies start from the same snapshot, see the same ")
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
}
