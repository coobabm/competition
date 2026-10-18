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
    /// A picture question opens 「它怎么看的」 (XgConvLook, 它在学 §2.3) over the curve card when clicked.
    /// </summary>
    public sealed partial class XgTrainPage
    {
        /// <summary>The sheet takes the curve card above this height; the curve and the stage diagram share the rest.</summary>
        const float SheetSplit = .5f, SheetHeaderHeight = 22, SheetRowGap = 3;

        RectTransform sheetArea, sheetRowsRoot;
        TMP_Text sheetHeader, sheetBadge;
        readonly List<SheetRow> sheetRows = new List<SheetRow>();
        string badgeText = "";
        Color badgeColor = XgDark.Muted;
        XgSheetAttentionLook look;

        // Cache only fully evaluated presentation inputs. Simulation/dirty-data/texture lookup
        // still runs, so source edits and newly available textures cannot hide behind a version key.
        sealed class SheetPresentation
        {
            bool valid,dirty,right,english,highlighted;
            string question,answer,label,kind;object texture;
            public bool Changed(string nextQuestion,string nextAnswer,string nextLabel,string nextKind,
                bool nextDirty,bool nextRight,bool nextEnglish,bool nextHighlighted,object nextTexture)
            {
                if(valid&&question==nextQuestion&&answer==nextAnswer&&label==nextLabel&&kind==nextKind
                    &&dirty==nextDirty&&right==nextRight&&english==nextEnglish&&highlighted==nextHighlighted
                    &&ReferenceEquals(texture,nextTexture))return false;
                valid=true;question=nextQuestion;answer=nextAnswer;label=nextLabel;kind=nextKind;
                dirty=nextDirty;right=nextRight;english=nextEnglish;highlighted=nextHighlighted;texture=nextTexture;
                return true;
            }
        }

        sealed class SheetRow
        {
            public RectTransform rt, iconRt;
            public Image bg;
            public RawImage thumbnail;
            public Button hit;
            public TMP_Text icon, question, answer;
            public readonly SheetPresentation presentation=new SheetPresentation();
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
            // Keep picture rows readable when other unlocked panels reduce the available height.
            // The viewport scrolls instead of squeezing 48px images into fractional-height rows.
            var viewport = Rect("RowsViewport", sheetArea, Vector2.zero, Vector2.one,
                Vector2.zero, new Vector2(0, -SheetHeaderHeight));
            Panel(viewport, Color.clear).raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            sheetRowsRoot = Rect("Rows", viewport, new Vector2(0, 1), Vector2.one,
                new Vector2(0, -XgSim.SheetSize * 67), Vector2.zero);
            sheetRowsRoot.pivot = new Vector2(.5f, 1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = sheetRowsRoot;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28;
            for (int i = 0; i < XgSim.SheetSize; i++)
            {
                var row = new SheetRow();
                row.rt = Rect("Question" + (i + 1), sheetRowsRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                PlaceRow(row.rt, i);
                row.bg = Panel(row.rt, XgDark.Panel);
                row.bg.raycastTarget = false;
                // Picture questions open 「它怎么看的」; the others stay plain rows.
                int index = i;
                row.hit = row.rt.gameObject.AddComponent<Button>();
                row.hit.targetGraphic = row.bg; row.hit.transition = Selectable.Transition.None;
                row.hit.onClick.AddListener(() => OpenLook(index));
                row.iconRt = Rect("Mark", row.rt, Vector2.zero, new Vector2(0, 1), new Vector2(4, 0), new Vector2(32, 0));
                row.icon = ui.Text(row.iconRt, "", 20, XgDark.Muted, TextAlignmentOptions.Center);
                row.icon.fontStyle = FontStyles.Bold;
                var thumb = Rect("OriginalThumbnail",row.rt,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(38,-24),new Vector2(86,24));
                row.thumbnail=thumb.gameObject.AddComponent<RawImage>();row.thumbnail.raycastTarget=false;
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
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0, -i * 67 - 64);
            rt.offsetMax = new Vector2(0, -i * 67);
        }

        void RefreshSheet(XgRun run)
        {
            if (sheetArea == null) return;
            bool open = Sim.TrainingUnlocked(Track);
            sheetArea.gameObject.SetActive(open);
            if (!open) { look?.Hide(); return; }
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
            look?.Sync(Sim, sheet);
        }

        /// <summary>Opens (or closes) 「它怎么看的」 for question i, a picture question.</summary>
        void OpenLook(int i)
        {
            var sheet = Sim.Run(Track).sheet;
            if (!Sim.TrainPart("weights") || sheet == null || i >= sheet.Count || XgTeachingPictures.ForCard(Sim.SheetCard(sheet[i])?.source)==null) return;
            if (look == null) look = XgSheetAttentionLook.Create(root, ui, Fx);
            look.Toggle(Sim, sheet[i]);
            RefreshSheet(Sim.Run(Track));
        }

        void ShowSheetRow(SheetRow row, XgRun run, XgSheetItem item)
        {
            bool dirty = Sim.SheetDirty(item);
            string kind = Sim.SheetKind(item);
            string question=Sim.SheetQuestion(item),answer=Sim.SheetAnswer(run,item),label=dirty?Sim.SheetLabel(item):"";
            var texture=XgTeachingPictures.ForCard(Sim.SheetCard(item)?.source);
            bool highlighted=look!=null&&look.Showing&&look.Item==item;
            if(!row.presentation.Changed(question,answer,label,kind,dirty,item.right,En,highlighted,texture))return;
            row.question.text = question
                + (!item.right && !dirty && kind.Length > 0 ? "\n<size=78%>" + Hex(XgDark.Muted) + kind + "</color></size>" : "")
                + (dirty ? "\n<size=78%>" + Hex(XgDark.Money) + T("标签写的是「" + label + "」——你看看哪个对？", "The label says " + label + ". Which is right?") + "</color></size>" : "");
            Color c = dirty ? XgDark.Money : item.right ? XgDark.Good : XgDark.Bad;
            // The desktop font has ✓ and × (no ✗).
            row.icon.text = dirty ? "？" : item.right ? "✓" : "×";
            row.icon.color = c;
            bool picture=texture!=null;
            row.thumbnail.texture=texture;row.thumbnail.gameObject.SetActive(picture);
            row.question.rectTransform.offsetMin=new Vector2(picture?94:38,2);
            row.answer.text = Hex(XgDark.Muted) + T("它答 ", "It says ") + "</color>" + Hex(c) + "<b>" + answer + "</b></color>"
                + (picture ? "\n<size=72%>" + Hex(highlighted ? XgDark.Gold : XgDark.Link) + T("看它学 ›", "watch it learn ›") + "</color></size>" : "");
            row.bg.raycastTarget = picture;
            row.hit.interactable = picture;
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
