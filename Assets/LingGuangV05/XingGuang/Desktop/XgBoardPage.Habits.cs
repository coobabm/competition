using System.Text;
using LingGuangV05.Core;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 大脑 · 语气习惯 (stage 4+): the learned H(s, a) of XgSim.Habits.cs drawn as a heat map, one row per tone and
    /// one column per situation. Green means it expects a 赞 for that tone in that situation, red a 踩.
    /// The weights here are the ones the 对话 page's 赞 / 踩 actually change.
    /// </summary>
    public sealed partial class XgBoardPage
    {
        RectTransform habitView;
        XgBtn habitTab;
        Image[,] habitCells;
        TMP_Text[,] habitValues;
        TMP_Text habitNotes;
        int shownHabitRatings = -1, shownHabitPicks = -1;

        bool HabitsOpen => Sim != null && Sim.S.stage >= 4;

        void BuildHabits(RectTransform card)
        {
            habitTab = ModeTab(card, 512, Mode.Habits);
            UiTip.Add(habitTab.rt, () => Lang.T("语气习惯：对话页每次 赞 / 踩，都会改这里的数。它按这张表决定下一句用什么语气。"));

            habitView = Rect("Habits", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var left = Rect("Map", habitView, Vector2.zero, new Vector2(.72f, 1), new Vector2(12, 12), new Vector2(-6, -52));
            Panel(left, XgDark.Page);
            var right = Rect("Notes", habitView, new Vector2(.72f, 0), Vector2.one, new Vector2(6, 12), new Vector2(-12, -52));
            Panel(right, XgDark.Page);
            habitNotes = ui.Text(Rect("Text", right, Vector2.zero, Vector2.one, new Vector2(12, 10), new Vector2(-12, -10)), "", 13, XgDark.Ink, TextAlignmentOptions.TopLeft);
            habitNotes.enableAutoSizing = true; habitNotes.fontSizeMin = 9; habitNotes.fontSizeMax = 13;

            int rows = XgSim.HabitTones.Length, cols = XgSim.HabitFeatures.Length;
            habitCells = new Image[rows, cols];
            habitValues = new TMP_Text[rows, cols];
            // Row labels in the first 12% of the width, column labels in the top 14% of the height.
            var grid = Rect("Grid", left, new Vector2(.12f, 0), new Vector2(1, .86f), new Vector2(0, 14), new Vector2(-14, 0));
            var heads = Rect("Heads", left, new Vector2(.12f, .86f), Vector2.one, new Vector2(0, 0), new Vector2(-14, -10));
            var names = Rect("Names", left, Vector2.zero, new Vector2(.12f, .86f), new Vector2(10, 14), new Vector2(-4, 0));
            for (int c = 0; c < cols; c++)
            {
                var head = ui.Text(Rect("Head" + c, heads, new Vector2((float)c / cols, 0), new Vector2((float)(c + 1) / cols, 1), new Vector2(1, 0), new Vector2(-1, 0)),
                    "", 11, XgDark.Muted, TextAlignmentOptions.Bottom);
                head.enableAutoSizing = true; head.fontSizeMin = 8; head.fontSizeMax = 11;
                head.text = T(XgSim.HabitFeatures[c], XgSim.HabitFeaturesEn[c]);
            }
            for (int r = 0; r < rows; r++)
            {
                float y0 = 1 - (float)(r + 1) / rows, y1 = 1 - (float)r / rows;
                var name = ui.Text(Rect("Name" + r, names, new Vector2(0, y0), new Vector2(1, y1), Vector2.zero, Vector2.zero), "", 13, XgDark.Ink, TextAlignmentOptions.MidlineRight);
                name.fontStyle = FontStyles.Bold;
                name.text = T(XgSim.HabitTones[r], XgSim.HabitTonesEn[r]);
                for (int c = 0; c < cols; c++)
                {
                    var rt = Rect("Cell" + r + "_" + c, grid, new Vector2((float)c / cols, y0), new Vector2((float)(c + 1) / cols, y1), new Vector2(2, 2), new Vector2(-2, -2));
                    var img = Panel(rt, XgDark.Line); img.raycastTarget = false;
                    habitCells[r, c] = img;
                    habitValues[r, c] = ui.Text(Rect("Value", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 11, XgDark.Ink, TextAlignmentOptions.Center);
                }
            }
        }

        void RefreshHabits()
        {
            header.fontStyle = FontStyles.Normal;
            header.text = Lang.T("大脑 · 语气习惯") + "  <size=13><color=#6F95A5>" + Lang.T("每个格子 = 在这种情况下用这种语气，它预计你会给的分（赞 +1，踩 −1）") + "</color></size>";
            if (Sim.HabitRatings == shownHabitRatings && Sim.HabitPicks == shownHabitPicks) return;
            shownHabitRatings = Sim.HabitRatings; shownHabitPicks = Sim.HabitPicks;
            for (int r = 0; r < habitCells.GetLength(0); r++)
                for (int c = 0; c < habitCells.GetLength(1); c++)
                {
                    float w = Sim.HabitWeight(r, c);
                    float strength = Mathf.Clamp01(Mathf.Abs(w) / 1.2f);
                    habitCells[r, c].color = Color.Lerp(XgDark.Line, w >= 0 ? XgDark.Good : XgDark.Bad, strength);
                    habitValues[r, c].text = Mathf.Abs(w) < .01f ? "" : (w > 0 ? "+" : "") + w.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                    habitValues[r, c].color = strength > .55f ? Color.white : XgDark.Ink;
                }
            habitNotes.text = HabitNotes();
        }

        string HabitNotes()
        {
            var sb = new StringBuilder("<b>").Append(Lang.T("它怎么选语气")).Append("</b>\n<size=12><color=#6F95A5>");
            sb.Append(Lang.T("你每说一句，它先看情况（上面那一排），把这几列的数加起来，再加一点开局性格的偏向，挑分最高的语气。"));
            sb.Append(Lang.T("偶尔它会随便挑一个试试。")).Append("\n\n");
            sb.Append(Lang.T("你给 赞 / 踩，就把那一句用到的格子往 +1 或 −1 拉一半。同样的话，下次它可能换个语气。"));
            sb.Append("</color></size>\n\n");
            sb.Append(Lang.T("选过语气 ")).Append(Sim.HabitPicks).Append(Lang.T(" 次\n"));
            sb.Append(Lang.T("被你评过 ")).Append(Sim.HabitRatings).Append(Lang.T(" 次\n"));
            if (Sim.HabitRatings == 0) sb.Append("\n<color=#E08A00>").Append(Lang.T("还是空的。去对话页赞 / 踩它的回复。")).Append("</color>");
            return sb.ToString();
        }
    }
}
