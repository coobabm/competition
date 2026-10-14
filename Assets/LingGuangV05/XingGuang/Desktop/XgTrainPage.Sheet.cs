using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 答卷 on the training page (训练页_答卷诊断能力考_设计.md §一): the same few questions for many rounds, each with
    /// what 灵光 answers now. A question that turns right flips to ✓ with a ding; one that falls back after a drop turns
    /// × again. The round's points shrink to a corner badge. The rules are in XgSim.Sheet.cs.
    /// </summary>
    public sealed partial class XgTrainPage
    {
        /// <summary>The sheet takes the curve card above this height; the curve and the stage diagram share the rest.</summary>
        const float SheetSplit = .5f, SheetHeaderHeight = 22, SheetRowGap = 3;

        RectTransform sheetArea;
        TMP_Text sheetHeader, sheetBadge;
        readonly List<SheetRow> sheetRows = new List<SheetRow>();
        string badgeText = "";
        Color badgeColor = XgDark.Muted;

        sealed class SheetRow
        {
            public RectTransform rt, iconRt;
            public Image bg;
            public TMP_Text icon, question, answer;
        }

        void BuildSheet()
        {
            sheetArea = Rect("Sheet", curveCard, new Vector2(0, SheetSplit), Vector2.one, new Vector2(12, 0), new Vector2(-12, -32));
            var header = Rect("SheetHeader", sheetArea, new Vector2(0, 1), Vector2.one, new Vector2(0, -SheetHeaderHeight), Vector2.zero);
            sheetHeader = ui.Text(Rect("Title", header, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-120, 0)), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            sheetHeader.textWrappingMode = TextWrappingModes.NoWrap;
            sheetBadge = ui.Text(Rect("Badge", header, new Vector2(1, 0), Vector2.one, new Vector2(-120, 0), Vector2.zero), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            sheetBadge.textWrappingMode = TextWrappingModes.NoWrap;
            sheetBadge.fontStyle = FontStyles.Bold;
            for (int i = 0; i < XgSim.SheetSize; i++)
            {
                var row = new SheetRow();
                row.rt = Rect("Question" + (i + 1), sheetArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                PlaceRow(row.rt, i);
                row.bg = Panel(row.rt, XgDark.Panel);
                row.bg.raycastTarget = false;
                row.iconRt = Rect("Mark", row.rt, Vector2.zero, new Vector2(0, 1), new Vector2(4, 0), new Vector2(32, 0));
                row.icon = ui.Text(row.iconRt, "", 20, XgDark.Muted, TextAlignmentOptions.Center);
                row.icon.fontStyle = FontStyles.Bold;
                row.question = ui.Text(Rect("Text", row.rt, Vector2.zero, Vector2.one, new Vector2(38, 2), new Vector2(-150, -2)), "", 14, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
                row.question.enableAutoSizing = true; row.question.fontSizeMin = 10; row.question.fontSizeMax = 14;
                row.question.overflowMode = TextOverflowModes.Truncate;
                row.answer = ui.Text(Rect("Answer", row.rt, new Vector2(1, 0), Vector2.one, new Vector2(-146, 2), new Vector2(-8, -2)), "", 14, XgDark.Ink, TextAlignmentOptions.MidlineRight);
                row.answer.enableAutoSizing = true; row.answer.fontSizeMin = 10; row.answer.fontSizeMax = 14;
                sheetRows.Add(row);
            }
            UiTip.Add(sheetArea, () => T(
                "答卷：几道题会留在这里很多轮，看它从答错到答对。\n✓ 答对 · × 答错 · ？ 它答对了，是标签写错了。\n准确率涨到哪道题的难度，那道题就翻过来；退步时也可能翻回去。最难的那道，换结构、加参数或补数据之前，怎么练都翻不过来。",
                "The sheet: a few questions stay here for many rounds, so you see them go from wrong to right.\n✓ right · × wrong · ? its answer is right and the label is wrong.\nA question flips once the accuracy passes its difficulty, and may flip back after a drop. The hardest one will not flip, however long you train, until the structure, the parameters or the data change."));
        }

        /// <summary>Row i of the sheet, under the header, as a share of the remaining height.</summary>
        void PlaceRow(RectTransform rt, int i)
        {
            float n = XgSim.SheetSize;
            // Each row is a slice of the height under the header.
            rt.anchorMin = new Vector2(0, 1 - (i + 1) / n);
            rt.anchorMax = new Vector2(1, 1 - i / n);
            float headerShareTop = SheetHeaderHeight * (n - i) / n, headerShareBottom = SheetHeaderHeight * (n - i - 1) / n;
            rt.offsetMin = new Vector2(0, -headerShareBottom + SheetRowGap / 2);
            rt.offsetMax = new Vector2(0, -headerShareTop - SheetRowGap / 2);
        }

        void RefreshSheet(XgRun run)
        {
            if (sheetArea == null) return;
            bool open = Sim.TrainingUnlocked(Track);
            sheetArea.gameObject.SetActive(open);
            if (!open) return;
            var sheet = Sim.Sheet(run);
            int right = 0;
            foreach (var item in sheet) if (item.right) right++;
            sheetHeader.text = T("答卷", "Sheet") + "  " + Hex(XgDark.Ink) + right + " / " + sheet.Count + "</color>"
                + (run.epoch == 0 ? "  " + T("还没练过：它什么都不会", "not trained yet: it knows nothing") : "")
                + (run.sheetFresh && run.sheetCount > 1 ? "  " + Hex(XgDark.Good) + T("换了一张卷子", "a new sheet") + "</color>" : "");
            sheetBadge.text = badgeText;
            sheetBadge.color = badgeColor;
            for (int i = 0; i < sheetRows.Count; i++)
            {
                var row = sheetRows[i];
                bool has = i < sheet.Count;
                row.rt.gameObject.SetActive(has);
                if (has) ShowSheetRow(row, run, sheet[i]);
            }
        }

        void ShowSheetRow(SheetRow row, XgRun run, XgSheetItem item)
        {
            bool dirty = Sim.SheetDirty(item);
            string kind = Sim.SheetKind(item);
            row.question.text = Sim.SheetQuestion(item)
                + (!item.right && !dirty && kind.Length > 0 ? "\n<size=78%>" + Hex(XgDark.Muted) + kind + "</color></size>" : "")
                + (dirty ? "\n<size=78%>" + Hex(XgDark.Money) + T("标签写的是「" + Sim.SheetLabel(item) + "」——你看看哪个对？", "The label says " + Sim.SheetLabel(item) + ". Which is right?") + "</color></size>" : "");
            Color c = dirty ? XgDark.Money : item.right ? XgDark.Good : XgDark.Bad;
            // The desktop font has ✓ and × (no ✗).
            row.icon.text = dirty ? "？" : item.right ? "✓" : "×";
            row.icon.color = c;
            row.answer.text = Hex(XgDark.Muted) + T("它答 ", "It says ") + "</color>" + Hex(c) + "<b>" + Sim.SheetAnswer(run, item) + "</b></color>";
            row.bg.color = item.right ? new Color32(13, 32, 30, 255) : dirty ? new Color32(36, 30, 18, 255) : XgDark.Panel;
        }

        /// <summary>The round's change as a corner badge, and the questions that flipped.</summary>
        void SheetRound(XgEpoch e, double points)
        {
            if (e.dropped) { badgeText = T("退步 " + N(Math.Abs(points), "0") + " 分", "−" + N(Math.Abs(points), "0") + " points"); badgeColor = XgDark.Bad; }
            else if (e.rolledBack) { badgeText = T("这次退步不计入", "drop excused"); badgeColor = XgDark.Link; }
            else if (points >= .5) { badgeText = "+" + N(points, "0") + T(" 分", " points"); badgeColor = XgDark.Good; }
            else { badgeText = T("涨不动了", "no gain"); badgeColor = XgDark.Muted; }
            if (sheetBadge != null) { sheetBadge.text = badgeText; sheetBadge.color = badgeColor; Fx.Knock(sheetBadge.rectTransform, .12f); }
            var run = Sim.Run(Track);
            if (run.sheet == null || run.sheetFresh) return;
            bool up = false, down = false;
            for (int i = 0; i < run.sheet.Count && i < sheetRows.Count; i++)
            {
                var item = run.sheet[i]; var row = sheetRows[i];
                if (item.flip > 0)
                {
                    up = true;
                    Fx.Knock(row.iconRt, .35f, default, .3f, true);
                    Fx.Burst(Fx.At(row.iconRt), 10, XgDark.Good, XgJuice.Shape.Spark, 220, 300, .5f);
                }
                else if (item.flip < 0)
                {
                    down = true;
                    Fx.Knock(row.rt, .05f, new Vector2(8, 0));
                }
            }
            if (up) Fx.Play(XgJuice.Sfx.Id.Ding, 1.1f, .7f);
            else if (down) Fx.Play(XgJuice.Sfx.Id.Thud, .7f, .45f);
        }
    }
}
