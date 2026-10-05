using System.Collections.Generic;
using System.Text;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 诊断 · 训练图式: the network the run trains, drawn layer by layer with the stuck layer in red (where), over the
    /// trace of its epochs with every problem marked at the epoch it first appeared (when). Shown by its tab, and
    /// always when no wall stands. Everything comes from <see cref="XgSim.NetworkHealth"/> and <see cref="XgSim.Trace"/>.
    /// </summary>
    public sealed partial class XgWallPage
    {
        RectTransform diagnosisView, graphView;
        XgBtn diagnosisTab, graphTab;
        bool graphChosen;
        XgNetworkGraphic network;
        XgTraceGraphic timeline;
        TMP_Text networkTitle, networkNote, timelineTitle, timelineAxis, events, verdictText, remedyText, sinceLabel;
        UnityEngine.UI.Image verdictPanel;
        XgVerdict verdict;
        readonly List<TMP_Text> boxLabels = new List<TMP_Text>(), boxNotes = new List<TMP_Text>();

        void BuildGraph(RectTransform card)
        {
            diagnosisTab = ui.Button(card, "", () => { graphChosen = false; Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 12);
            graphTab = ui.Button(card, "", () => { graphChosen = true; Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 12);
            PlaceTopRight(graphTab, 104, 8, 92, 28);
            PlaceTopRight(diagnosisTab, 200, 8, 92, 28);

            graphView = Rect("TrainingGraph", card, Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -44));
            // The answer first: one line saying where it is stuck, and the two roads out.
            var verdictRt = Rect("Verdict", graphView, new Vector2(0, 1), Vector2.one, new Vector2(0, -74), Vector2.zero);
            verdictPanel = Panel(verdictRt, XgPalette.Page);
            verdictText = ui.Text(Rect("Headline", verdictRt, new Vector2(0, .45f), Vector2.one, new Vector2(14, 0), new Vector2(-14, -4)), "", 18, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            verdictText.fontStyle = FontStyles.Bold; verdictText.enableAutoSizing = true; verdictText.fontSizeMin = 12; verdictText.fontSizeMax = 18;
            remedyText = ui.Text(Rect("Remedies", verdictRt, Vector2.zero, new Vector2(1, .45f), new Vector2(14, 4), new Vector2(-14, 0)), "", 13, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            remedyText.enableAutoSizing = true; remedyText.fontSizeMin = 10; remedyText.fontSizeMax = 13;
            var net = Rect("Network", graphView, new Vector2(0, .4f), Vector2.one, new Vector2(0, 0), new Vector2(0, -80));
            Panel(net, XgPalette.Page);
            networkTitle = ui.Text(Strip("Title", net, 6, 22, 12, 12), "", 14, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            networkTitle.textWrappingMode = TextWrappingModes.NoWrap;
            networkNote = ui.Text(Rect("Note", net, Vector2.zero, new Vector2(1, 0), new Vector2(12, 4), new Vector2(-12, 26)), "", 12, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);
            network = Rect("Diagram", net, Vector2.zero, Vector2.one, new Vector2(8, 34), new Vector2(-8, -32)).gameObject.AddComponent<XgNetworkGraphic>();
            network.raycastTarget = false;

            var time = Rect("Timeline", graphView, Vector2.zero, new Vector2(.62f, .4f), Vector2.zero, new Vector2(-6, -8));
            Panel(time, XgPalette.Page);
            timelineTitle = ui.Text(Strip("Title", time, 6, 22, 12, 12), "", 13, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            timelineTitle.textWrappingMode = TextWrappingModes.NoWrap;
            timeline = Rect("Trace", time, Vector2.zero, Vector2.one, new Vector2(44, 22), new Vector2(-10, -32)).gameObject.AddComponent<XgTraceGraphic>();
            timeline.raycastTarget = false;
            timelineAxis = ui.Text(Rect("Axis", time, Vector2.zero, new Vector2(1, 0), new Vector2(44, 2), new Vector2(-10, 20)), "", 11, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);
            ui.Text(Rect("Top", timeline.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-42, -9), new Vector2(-4, 9)), "100%", 11, XgPalette.Muted, TextAlignmentOptions.MidlineRight);
            ui.Text(Rect("Coin", timeline.rectTransform, new Vector2(0, .18f), new Vector2(0, .18f), new Vector2(-42, -9), new Vector2(-4, 9)), "50%", 11, XgPalette.Muted, TextAlignmentOptions.MidlineRight);
            ui.Text(Rect("Cells", timeline.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(-42, 0), new Vector2(-4, 14)), T("格子", "cells"), 10, XgPalette.Muted, TextAlignmentOptions.MidlineRight);
            sinceLabel = ui.Text(Rect("Since", timeline.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(4, -20), new Vector2(150, 0)), "", 12, XgNetworkGraphic.SickEdge, TextAlignmentOptions.MidlineLeft);
            sinceLabel.fontStyle = FontStyles.Bold;

            var list = Rect("Events", graphView, new Vector2(.62f, 0), new Vector2(1, .4f), new Vector2(6, 0), new Vector2(0, -8));
            Panel(list, XgPalette.Page);
            events = ui.Text(Rect("Text", list, Vector2.zero, Vector2.one, new Vector2(10, 8), new Vector2(-10, -8)), "", 12, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            events.enableAutoSizing = true; events.fontSizeMin = 9; events.fontSizeMax = 12;
            graphView.gameObject.SetActive(false);
        }

        /// <summary>Shows the graph view when chosen (or when no wall stands) and returns true; otherwise readies the diagnosis view.</summary>
        bool ShowGraph(XgWall wall)
        {
            bool graph = graphChosen || wall == null;
            diagnosisView.gameObject.SetActive(!graph);
            graphView.gameObject.SetActive(graph);
            diagnosisTab.Show(wall != null); graphTab.Show(wall != null);
            diagnosisTab.Set(T("诊断", "Diagnosis"), true, graph ? XgPalette.Button : XgPalette.Accent, graph ? XgPalette.Ink : Color.white);
            graphTab.Set(T("训练图式", "Training map"), true, graph ? XgPalette.Accent : XgPalette.Button, graph ? Color.white : XgPalette.Ink);
            if (!graph) return false;
            XgWallCheck check = null;
            var run = wall != null ? RunOf(wall, out check) : Sim.Selected;
            RefreshGraph(run, wall);
            return true;
        }

        void RefreshGraph(XgRun run, XgWall wall)
        {
            var d = XgCatalog.Dataset(run.dataset);
            string name = d != null ? T(d.name, d.nameEn) : run.dataset;
            header.text = (wall != null ? T("墙 · ", "Wall · ") + T(wall.name, wall.nameEn) + "  " : "") + "<size=13><color=#68748C>" + T("训练图式 · ", "Training map · ") + name + "</color></size>";

            var h = Sim.NetworkHealth(run);
            verdict = Sim.Verdict(run);
            bool bad = verdict.problem;
            verdictPanel.color = bad ? new Color32(255, 236, 236, 255) : verdict.id == "untrained" ? XgPalette.Page : new Color32(230, 246, 234, 255);
            verdictText.color = bad ? XgNetworkGraphic.SickEdge : verdict.id == "untrained" ? XgPalette.Muted : XgPalette.Good;
            verdictText.text = (bad ? "✖ " : verdict.id == "untrained" ? "" : "✔ ") + T(verdict.headline, verdict.headlineEn)
                + (verdict.since >= 0 ? "  <size=70%><color=#68748C>" + T("第 " + verdict.since + " 轮起", "since epoch " + verdict.since) + "</color></size>" : "");
            var remedies = new StringBuilder();
            if (verdict.structure.Length > 0) remedies.Append("<b>").Append(T("换结构：", "Structure: ")).Append("</b>").Append(T(verdict.structure, verdict.structureEn));
            if (verdict.method.Length > 0) remedies.Append(remedies.Length > 0 ? "      " : "").Append("<b>").Append(T("换练法：", "Method: ")).Append("</b>").Append(T(verdict.method, verdict.methodEn));
            remedyText.text = remedies.Length > 0 ? remedies.ToString() : bad ? "" : T("继续训练就好。", "Keep training.");
            network.SetData(h, bad ? verdict.layer : 0, bad && verdict.layer == 0 && verdict.id == "cells");
            networkTitle.text = "<b>" + T("网络结构", "Network") + "</b>  <size=12><color=#68748C>" + Sim.TraceSettings(run) + "</color></size>";
            var note = new StringBuilder();
            note.Append(h.full ? "<color=#D63031>" : "").Append(T("格子 ", "Cells ")).Append(h.cells).Append(" / ").Append(h.cap).Append(h.full ? T("（满了：权重最小的让位）", " (full: the weakest weights give way)") + "</color>" : "");
            if (h.evicted > 0) note.Append(T("  上一轮挤掉 ", "  last epoch squeezed out ")).Append(h.evicted).Append(T(" 个", ""));
            if (h.memory10 < 1) note.Append(h.memory10 < .2 ? "  <color=#D63031>" : "  <color=#F07820>").Append(T("回环记忆：隔 10 个字还剩 ", "loop memory: 10 words back keeps ")).Append(N(h.memory10 * 100, "0")).Append("%</color>");
            note.Append(T("    块越厚 = 这一层占的格子越多    上方的带子 = 原样转交到输出的票（越窄留下越少，越红改写越多）    ", "    thicker box = more cells on that layer    ribbon = votes passed on unchanged to the answer (narrower = less kept, redder = more rewritten)    "));
            note.Append("<color=#D63031>").Append(T("红 = 卡在这一层", "red = stuck here")).Append("</color>");
            networkNote.text = note.ToString();
            PlaceBoxLabels(h, name);

            var trace = Sim.Trace((XgTrack)run.track);
            float goal = (float)Sim.TraceGoal(run);
            timeline.SetData(trace, goal, verdict.since);
            sinceLabel.gameObject.SetActive(verdict.since >= 0 && trace != null && trace.points.Count > 1);
            if (verdict.since >= 0)
            {
                float x = timeline.EpochX(verdict.since);
                var rt = sinceLabel.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(x, 1);
                bool flip = x > .7f;
                sinceLabel.alignment = flip ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                rt.offsetMin = new Vector2(flip ? -150 : 4, -20); rt.offsetMax = new Vector2(flip ? -4 : 150, 0);
                sinceLabel.text = T("← 第 " + verdict.since + " 轮起出问题", "← trouble from epoch " + verdict.since);
                if (flip) sinceLabel.text = T("第 " + verdict.since + " 轮起出问题 →", "trouble from epoch " + verdict.since + " →");
            }
            timelineTitle.text = "<b>" + T("训练时间线", "Timeline") + "</b>  <size=11>" + Hex(XgChartGraphic.ValColor) + T("— 考试", "— exam") + "</color>  "
                + Hex(XgChartGraphic.TrainColor) + T("— 训练", "— training") + "</color>  " + Hex(XgChartGraphic.BestColor) + T("- - 及格线 ", "- - target ") + XgSim.Pct(goal) + "</color>  "
                + Hex(XgNetworkGraphic.SickEdge) + T("| 出问题", "| problem") + "</color></size>";
            timelineAxis.text = trace == null || trace.points.Count == 0 ? T("还没在这个数据集上训练过。", "Not trained on this dataset yet.")
                : T("第 ", "epoch ") + timeline.FromEpoch + T(" 轮", "") + new string(' ', 12) + T("→ 第 ", "→ epoch ") + timeline.ToEpoch + T(" 轮", "");

            var sb = new StringBuilder("<b>" + T("出了什么事", "What happened") + "</b>  <size=11><color=#68748C>" + T("最新在上", "newest first") + "</color></size>\n");
            if (trace == null || trace.events.Count == 0) sb.Append("\n").Append(T("还没发现问题。训练几轮再来看。", "Nothing noticed yet. Train a few epochs and look again."));
            else
            {
                int shown = 0;
                for (int i = trace.events.Count - 1; i >= 0 && shown < 9; i--, shown++)
                {
                    var e = trace.events[i];
                    string zh = XgSim.TraceEventText(e, out string en);
                    string color = XgSim.TraceEventIsProblem(e) ? "#D63031" : e.id == "change" ? "#68748C" : "#845EF7";
                    sb.Append("\n<color=").Append(color).Append(">").Append(T("第 " + e.epoch + " 轮", "Epoch " + e.epoch)).Append("</color>  ").Append(T(zh, en));
                }
            }
            events.text = sb.ToString();
        }

        /// <summary>Names under each box and the problem above the red one, placed in the diagram's 0–1 layout.</summary>
        void PlaceBoxLabels(XgNetworkHealth h, string datasetName)
        {
            var parent = network.rectTransform;
            int n = 0;
            foreach (var b in network.Layout)
            {
                if (b.gap) continue;
                if (boxLabels.Count <= n)
                {
                    var label = ui.Text(Rect("BoxLabel", parent, Vector2.zero, Vector2.zero, new Vector2(-70, -48), new Vector2(70, -2)), "", 11, XgPalette.Muted, TextAlignmentOptions.Top);
                    var problem = ui.Text(Rect("BoxProblem", parent, Vector2.zero, Vector2.zero, new Vector2(-90, 46), new Vector2(90, 96)), "", 12, XgNetworkGraphic.SickEdge, TextAlignmentOptions.Bottom);
                    problem.fontStyle = FontStyles.Bold;
                    // A white rim keeps the words readable where they cross the signal ribbon.
                    problem.outlineWidth = .25f; problem.outlineColor = new Color32(255, 255, 255, 255);
                    boxLabels.Add(label); boxNotes.Add(problem);
                }
                var below = new Vector2(b.front.center.x, b.front.yMin);
                var above = new Vector2(b.front.center.x, b.front.yMax);
                boxLabels[n].rectTransform.anchorMin = boxLabels[n].rectTransform.anchorMax = below;
                boxNotes[n].rectTransform.anchorMin = boxNotes[n].rectTransform.anchorMax = above;
                boxLabels[n].gameObject.SetActive(true);
                string problemText = "";
                if (b.input) boxLabels[n].text = T("输入", "Input") + "\n" + datasetName + (h.features ? T("（人工特征）", " (hand-made)") : "");
                else if (b.output) boxLabels[n].text = T("是 / 否", "Yes / no");
                else if (b.bottleneck) boxLabels[n].text = T("瓶颈\n只留 ", "Bottleneck\nkeeps ") + XgBoard.BottleneckTokens + T(" 个字", " tokens");
                else
                {
                    var layer = h.layers[b.layer - 1];
                    boxLabels[n].text = T("第 " + layer.layer + " 层", "Layer " + layer.layer) + "\n" + layer.concepts + T(" 个概念", " concepts")
                        + (layer.layer < h.depth ? "\n" + T("原样到输出 ", "arrives unchanged ") + N(layer.relay * 100, "0") + "%" : "");
                    // Only the layer the verdict points at gets words; the rest stay quiet.
                    problemText = verdict != null && verdict.problem && verdict.layer == layer.layer ? T("▼ 卡在这里", "▼ stuck here") + "\n" + (verdict.id == "memory" ? T("隔 10 个字只剩 ", "10 words back: ") + N(h.memory10 * 100, "0") + "%" : LayerProblem(layer)) : "";
                }
                boxNotes[n].text = problemText;
                boxNotes[n].gameObject.SetActive(problemText.Length > 0);
                n++;
            }
            for (int i = n; i < boxLabels.Count; i++) { boxLabels[i].gameObject.SetActive(false); boxNotes[i].gameObject.SetActive(false); }
        }

        static string LayerProblem(XgLayerHealth layer)
        {
            switch (layer.problem)
            {
                case "step": return T("阶跃没有坡度\n误差传不下来", "A step has no slope:\nno error gets here");
                case "relay": return T("原样到输出只剩 ", "Only ") + N(layer.relay * 100, "0") + "%" + T("\n被上面的层改写了", " arrives unchanged\nrewritten on the way");
                case "signal": return T("误差只剩 ", "Only ") + N(layer.signal * 100, layer.signal < .01 ? "0.0" : "0") + "%" + T("\n几乎学不动", " of the error\nbarely learns");
                case "nomerge": return T("一个组合\n都没长出来", "No combined\nconcept yet");
                default: return "";
            }
        }
    }
}
