using System.Collections.Generic;
using System.IO;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.EditorTools
{
    /// <summary>
    /// Writes the 训练页 right-column module prefabs (Resources/LingGuangV05/Training/TrainPage): 下一项能力 / 有效规模
    /// (TrainScaleModule), 装备 · 它学会了吗 · 书架 (TrainGearModule, with TrainGearTile and TrainBookRow) and 今天的账
    /// (TrainLedgerModule). Layout follows the Game Designer's spec (side-session notes, train-page-module-layout-spec.md):
    /// every module is a VerticalLayoutGroup card, rows are layout groups, nothing is placed by pixel offsets, so no text
    /// can run into a button at any length or language. Buttons are slots filled at runtime by XgUi.Button.
    /// The prefabs are the source of truth once written; rerun this only to reset them.
    /// </summary>
    public static class XgTrainModulePrefabs
    {
        const string Folder = "Assets/LingGuangV05/Resources/" + XgModuleRefs.ResourceRoot;

        [MenuItem("Tools/灵光/Rebuild training page module prefabs")]
        public static string BuildAll()
        {
            if (EditorApplication.isPlaying) return "refused: Play mode";
            Directory.CreateDirectory(Folder);
            Save(Scale(), "TrainScaleModule");
            Save(Gear(), "TrainGearModule");
            Save(GearTile(), "TrainGearTile");
            Save(BookRow(), "TrainBookRow");
            Save(Ledger(), "TrainLedgerModule");
            AssetDatabase.SaveAssets();
            return "ok " + Folder;
        }

        // ───────────── kit ─────────────

        static TMP_FontAsset Font => XgUiV4Fonts.Sans;

        sealed class Refs
        {
            public readonly XgModuleRefs refs;
            public Refs(GameObject root) { refs = root.AddComponent<XgModuleRefs>(); }
            public RectTransform Add(string key, RectTransform rt) { refs.entries.Add(new XgModuleRefs.Entry { key = key, rect = rt }); return rt; }
        }

        static void Save(GameObject go, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(go, Folder + name + ".prefab");
            Object.DestroyImmediate(go);
        }

        static RectTransform Root(string name, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static RectTransform Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        static VerticalLayoutGroup V(RectTransform rt, int l, int r, int t, int b, float spacing, TextAnchor align = TextAnchor.UpperLeft)
        {
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.padding = new RectOffset(l, r, t, b); g.spacing = spacing; g.childAlignment = align;
            g.childControlWidth = g.childControlHeight = true; g.childForceExpandWidth = true; g.childForceExpandHeight = false;
            return g;
        }

        static HorizontalLayoutGroup H(RectTransform rt, int l, int r, int t, int b, float spacing, TextAnchor align = TextAnchor.MiddleLeft, bool expandWidth = false, bool expandHeight = false)
        {
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.padding = new RectOffset(l, r, t, b); g.spacing = spacing; g.childAlignment = align;
            g.childControlWidth = g.childControlHeight = true; g.childForceExpandWidth = expandWidth; g.childForceExpandHeight = expandHeight;
            return g;
        }

        static LayoutElement LE(RectTransform rt, float minW = -1, float prefW = -1, float flexW = -1, float minH = -1, float prefH = -1, float flexH = -1)
        {
            if (!rt.TryGetComponent<LayoutElement>(out var e)) e = rt.gameObject.AddComponent<LayoutElement>();
            e.minWidth = minW; e.preferredWidth = prefW; e.flexibleWidth = flexW; e.minHeight = minH; e.preferredHeight = prefH; e.flexibleHeight = flexH;
            return e;
        }

        static void Fit(RectTransform rt)
        {
            var f = rt.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static Image Img(RectTransform rt, Color c, bool raycast = false, float round = 0)
        {
            var i = rt.gameObject.AddComponent<Image>();
            i.color = c; i.raycastTarget = raycast;
            if (round > 0) rt.gameObject.AddComponent<XgRoundSurface>().radius = round;
            return i;
        }

        /// <summary>Text that wraps and takes its preferred height (no autosize: TMP autosize inside a layout misreports height).</summary>
        static TextMeshProUGUI Txt(RectTransform rt, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.TopLeft, bool wrap = true, bool ellipsis = false)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null) t.font = Font;
            t.text = ""; t.fontSize = size; t.color = color; t.alignment = align; t.raycastTarget = false; t.richText = true;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = ellipsis ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
            return t;
        }

        /// <summary>Fixed-box text: one line, shrinks to <paramref name="min"/>, then ellipsis.</summary>
        static TextMeshProUGUI Box(RectTransform rt, float size, float min, Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var t = Txt(rt, size, color, align, false, true);
            t.enableAutoSizing = true; t.fontSizeMin = min; t.fontSizeMax = size;
            return t;
        }

        /// <summary>A card: rounded hairline border with the card surface inside; a vertical layout with the spec's padding.</summary>
        static RectTransform Card(string name, float width, float height)
        {
            var rt = Root(name, width, height);
            Img(rt, XgDark.Line, true, 18);
            var inner = Stretch(Node("Inner", rt), 1, 1, 1, 1);
            Img(inner, XgDark.Card, false, 17);
            LE(inner).ignoreLayout = true;
            V(rt, 12, 12, 8, 8, 8);
            return rt;
        }

        /// <summary>A label (gives way, wraps) and a value (keeps its width) on one row.</summary>
        static void LabelValue(RectTransform parent, string name, Refs refs, string labelKey, string valueKey, float labelSize, Color labelColor, float valueSize, Color valueColor, bool labelWraps, float minHeight)
        {
            var row = Node(name, parent);
            H(row, 0, 0, 0, 0, 8, TextAnchor.UpperLeft);
            LE(row, minH: minHeight);
            var label = Node("Label", row);
            Txt(label, labelSize, labelColor, TextAlignmentOptions.TopLeft, labelWraps, !labelWraps);
            LE(label, minW: 0, prefW: 0, flexW: 1);
            refs.Add(labelKey, label);
            var value = Node("Value", row);
            Txt(value, valueSize, valueColor, TextAlignmentOptions.TopRight, false);
            refs.Add(valueKey, value);
        }

        /// <summary>A bar: the track and an anchor-scaled fill (XgUi.SetBar).</summary>
        static RectTransform BarNode(RectTransform parent, string name, float height, Color back, Color fill, Refs refs, string fillKey)
        {
            var track = Node(name, parent);
            Img(track, back);
            LE(track, minH: height, prefH: height);
            var f = Node("Fill", track);
            f.anchorMin = Vector2.zero; f.anchorMax = new Vector2(0, 1); f.offsetMin = f.offsetMax = Vector2.zero;
            Img(f, fill);
            refs.Add(fillKey, f);
            return track;
        }

        static RectTransform Slot(RectTransform parent, string name, Refs refs, string key, float minH, float prefH = -1, float flexW = -1, float minW = -1, float prefW = -1)
        {
            var s = Node(name, parent);
            LE(s, minW: minW, prefW: prefW, flexW: flexW, minH: minH, prefH: prefH < 0 ? minH : prefH);
            return refs.Add(key, s);
        }

        static RectTransform Scroll(RectTransform viewport, out RectTransform content)
        {
            Img(viewport, Color.clear, true);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Node("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var s = viewport.gameObject.AddComponent<ScrollRect>();
            s.viewport = viewport; s.content = content; s.horizontal = false; s.vertical = true;
            s.movementType = ScrollRect.MovementType.Clamped; s.scrollSensitivity = 25;
            return viewport;
        }

        // ───────────── A · 下一项能力 / 有效规模 ─────────────

        static GameObject Scale()
        {
            var card = Card("TrainScaleModule", 410, 210);
            var refs = new Refs(card.gameObject);
            refs.Add("card", card);
            LE(card, flexH: 0);
            var title = Node("Title", card);
            var tt = Txt(title, 12, XgDark.Muted); tt.characterSpacing = 1;
            LE(title, minH: 16);
            refs.Add("title", title);

            // Stages 1–5: the two bars, the fixed question, the chapter button.
            var next = Node("NextGroup", card);
            V(next, 0, 0, 0, 0, 8);
            refs.Add("next", next);
            foreach (var (block, p) in new[] { ("ParamsBlock", "p"), ("SamplesBlock", "d") })
            {
                var b = Node(block, next);
                V(b, 0, 0, 0, 0, 4);
                LabelValue(b, "Row", refs, p + "Label", p + "Value", 12, XgDark.Muted, 13, XgDark.Ink, false, 18);
                BarNode(b, "Track", 8, XgDark.Track, p == "p" ? XgDark.Params : XgDark.Data, refs, p + "Fill");
                var sub = Node("Sub", b);
                Txt(sub, 10, XgDark.Muted);
                refs.Add(p + "Sub", sub);
            }
            var probe = Node("Probe", next);
            Txt(probe, 11, XgDark.Ink);
            refs.Add("probe", probe);
            Slot(next, "ChapterButton", refs, "chapter", 30);

            // Stage 6: the big number with its verdict tag, the nine factors, the log bar, the verdict.
            var six = Node("SixGroup", card);
            V(six, 0, 0, 0, 0, 6);
            refs.Add("six", six);
            var bigRow = Node("BigRow", six);
            H(bigRow, 0, 0, 0, 0, 8, TextAnchor.MiddleLeft);
            LE(bigRow, minH: 30);
            var big = Node("Big", bigRow);
            Txt(big, 24, XgDark.Ink, TextAlignmentOptions.MidlineLeft, false);
            refs.Add("big", big);
            var tag = Node("Tag", bigRow);
            Txt(tag, 12, XgDark.Muted, TextAlignmentOptions.MidlineRight);
            LE(tag, minW: 0, prefW: 0, flexW: 1);
            refs.Add("tag", tag);
            var chips = Node("Chips", six);
            V(chips, 0, 0, 0, 0, 3);
            for (int r = 0; r < 3; r++)
            {
                var row = Node("ChipRow" + r, chips);
                H(row, 0, 0, 0, 0, 4, TextAnchor.MiddleLeft, true, true);
                LE(row, minH: 20, prefH: 20);
                for (int c = 0; c < 3; c++)
                {
                    int i = r * 3 + c;
                    var chip = Node("Factor" + i, row);
                    chip.gameObject.AddComponent<XgFrameGraphic>().raycastTarget = false;
                    LE(chip, minW: 0, prefW: 0, flexW: 1, minH: 20, prefH: 20);
                    Box(Stretch(Node("Text", chip), 5, 0, 5, 0), 11, 9, XgDark.Ink, TextAlignmentOptions.Center);
                    refs.Add("chip" + i, chip);
                }
            }
            var log = Node("LogBlock", six);
            V(log, 0, 0, 0, 0, 2);
            var logBar = BarNode(log, "LogBar", 12, XgDark.Track, XgDark.Params, refs, "logFill");
            refs.Add("logBar", logBar);
            var mark = Node("Mark", logBar);
            mark.anchorMin = Vector2.zero; mark.anchorMax = new Vector2(0, 1); mark.offsetMin = new Vector2(-1, -3); mark.offsetMax = new Vector2(1, 3);
            Img(mark, Color.white);
            refs.Add("logMark", mark);
            var labelRow = Node("LabelRow", log);
            LE(labelRow, minH: 13, prefH: 13);
            var markLabel = Node("MarkLabel", labelRow);
            markLabel.anchorMin = markLabel.anchorMax = new Vector2(0, 1); markLabel.pivot = new Vector2(.5f, 1);
            markLabel.sizeDelta = new Vector2(60, 13);
            Txt(markLabel, 10, XgDark.Muted, TextAlignmentOptions.Top, false);
            refs.Add("logLabel", markLabel);
            var verdict = Node("Verdict", six);
            Txt(verdict, 11, XgDark.Muted);
            refs.Add("verdict", verdict);
            return card.gameObject;
        }

        // ───────────── B · 装备 / 它学会了吗 / 书架 ─────────────

        static GameObject Gear()
        {
            var card = Card("TrainGearModule", 410, 300);
            var refs = new Refs(card.gameObject);
            refs.Add("card", card);
            LE(card, minH: 224, flexH: 1);
            var tabs = Node("TabRow", card);
            H(tabs, 0, 0, 0, 0, 4, TextAnchor.MiddleLeft, false, true);
            LE(tabs, minH: 26, prefH: 26, flexH: 0);
            Slot(tabs, "GearTabSlot", refs, "gearTab", 26, minW: 56, prefW: 72);
            Slot(tabs, "LearningTabSlot", refs, "learningTab", 26, minW: 56, prefW: 104);
            Slot(tabs, "BooksTabSlot", refs, "booksTab", 26, minW: 56, prefW: 60);
            var host = Node("PaneHost", card);
            LE(host, minH: 0, flexH: 1);

            // 装备: four tiles a row; the grid scrolls when the card is short.
            var gear = Scroll(Stretch(Node("GearPane", host)), out var grid);
            refs.Add("gearPane", gear);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(92, 56); g.spacing = new Vector2(6, 6);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 4;
            g.startCorner = GridLayoutGroup.Corner.UpperLeft; g.childAlignment = TextAnchor.UpperCenter;
            Fit(grid);
            refs.Add("gearGrid", grid);

            // 它学会了吗: the checkup scrolls; the YY button is a layout sibling under it.
            var learning = Stretch(Node("LearningPane", host));
            V(learning, 0, 0, 0, 0, 8);
            refs.Add("learningPane", learning);
            var evidence = Scroll(Node("Evidence", learning), out var evidenceContent);
            LE(evidence, minH: 48, flexH: 1);
            Txt(evidenceContent, 14, XgDark.Ink);
            Fit(evidenceContent);
            refs.Add("learningText", evidenceContent);
            Slot(learning, "AskButton", refs, "ask", 32);

            // 书架: the shelf scrolls; the reading panel is a fixed footer.
            var books = Stretch(Node("BooksPane", host));
            V(books, 0, 0, 0, 0, 4);
            refs.Add("booksPane", books);
            var shelf = Scroll(Node("Shelf", books), out var shelfContent);
            LE(shelf, minH: 58, flexH: 1);
            V(shelfContent, 0, 0, 0, 0, 3);
            Fit(shelfContent);
            refs.Add("shelf", shelfContent);
            var reading = Node("Reading", books);
            Img(reading, XgDark.AccentSoft);
            LE(reading, minH: 108, prefH: 112, flexH: 0);
            V(reading, 10, 10, 6, 8, 4);
            refs.Add("reading", reading);
            var head = Node("Head", reading);
            var ht = Txt(head, 12, XgDark.Accent, TextAlignmentOptions.MidlineLeft, false, true); ht.fontStyle = FontStyles.Bold;
            LE(head, minH: 18, prefH: 18);
            refs.Add("bookHead", head);
            var body = Node("Body", reading);
            var bt = Txt(body, 12, XgDark.Ink, TextAlignmentOptions.TopLeft, true, true);
            bt.enableAutoSizing = true; bt.fontSizeMin = 9; bt.fontSizeMax = 12;
            if (XgUiV4Fonts.Reading != null) bt.font = XgUiV4Fonts.Reading;
            LE(body, minH: 32, prefH: 32, flexH: 1);
            refs.Add("bookBody", body);
            BarNode(reading, "Bar", 4, XgDark.Track, XgDark.Gold, refs, "bookFill");
            var answer = Node("AnswerRow", reading);
            H(answer, 0, 0, 0, 0, 8, TextAnchor.MiddleLeft, false, true);
            LE(answer, minH: 28, prefH: 28);
            refs.Add("answerRow", answer);
            Slot(answer, "AnswerButton", refs, "redMansion", 28, flexW: 1, minW: 0, prefW: 0);
            var box = Node("BookAnswer", answer);
            LE(box, minW: 0, prefW: 0, flexW: 1, minH: 28);
            Img(box, XgDark.Button, true);
            var area = Stretch(Node("Text Area", box), 8, 2, 8, 2);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = Txt(Stretch(Node("Placeholder", area)), 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft, false);
            var text = Txt(Stretch(Node("Text", area)), 12, XgDark.Ink, TextAlignmentOptions.MidlineLeft, false);
            text.richText = false;
            var field = box.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area; field.textComponent = text; field.placeholder = placeholder;
            field.pointSize = 12; field.characterLimit = 60; field.richText = false; field.lineType = TMP_InputField.LineType.SingleLine;
            if (Font != null) field.fontAsset = Font;
            refs.Add("answerField", box);
            Slot(answer, "SendButton", refs, "answerSend", 28, minW: 56, prefW: 64);
            return card.gameObject;
        }

        static GameObject GearTile()
        {
            var rt = Root("TrainGearTile", 92, 56);
            var refs = new Refs(rt.gameObject);
            var frame = rt.gameObject.AddComponent<XgFrameGraphic>();
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = frame; button.transition = Selectable.Transition.None;
            V(rt, 6, 4, 4, 4, 0);
            var name = Node("Name", rt); Box(name, 11, 9, XgDark.Ink); LE(name, minH: 14, prefH: 14); refs.Add("name", name);
            var big = Node("Big", rt); Box(big, 15, 11, XgDark.Money); LE(big, minH: 20, prefH: 20); refs.Add("big", big);
            var state = Node("State", rt); Box(state, 10, 9, XgDark.Muted); LE(state, minH: 13, prefH: 13); refs.Add("state", state);
            return rt.gameObject;
        }

        static GameObject BookRow()
        {
            var rt = Root("TrainBookRow", 386, 26);
            var refs = new Refs(rt.gameObject);
            var back = Img(rt, XgDark.Track, true);
            var select = rt.gameObject.AddComponent<Button>(); select.transition = Selectable.Transition.None; select.targetGraphic = back;
            LE(rt, minH: 26, prefH: 26, flexH: 0);
            H(rt, 8, 4, 2, 3, 6, TextAnchor.MiddleLeft);
            var name = Node("Name", rt); Txt(name, 12, XgDark.Ink, TextAlignmentOptions.MidlineLeft, false, true); LE(name, minW: 0, prefW: 0, flexW: 1); refs.Add("name", name);
            var count = Node("Count", rt); Txt(count, 11, XgDark.Muted, TextAlignmentOptions.MidlineRight, false); LE(count, minW: 40); refs.Add("count", count);
            Slot(rt, "FeedSlot", refs, "feed", 20, minW: 88, prefW: 88);
            var meter = Node("Meter", rt);
            LE(meter).ignoreLayout = true;
            meter.anchorMin = Vector2.zero; meter.anchorMax = new Vector2(1, 0); meter.offsetMin = Vector2.zero; meter.offsetMax = new Vector2(-98, 3);
            var fill = Node("Fill", meter);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1); fill.offsetMin = fill.offsetMax = Vector2.zero;
            Img(fill, XgDark.Data);
            refs.Add("fill", fill);
            return rt.gameObject;
        }

        // ───────────── C · 今天的账 ─────────────

        static GameObject Ledger()
        {
            var card = Card("TrainLedgerModule", 410, 106);
            var refs = new Refs(card.gameObject);
            refs.Add("card", card);
            LE(card, flexH: 0);
            var head = Node("Head", card); Txt(head, 12, XgDark.Muted, TextAlignmentOptions.MidlineLeft, false); head.gameObject.SetActive(false); refs.Add("head", head);
            var date = Node("Date", card); Txt(date, 12, XgDark.Ink, TextAlignmentOptions.MidlineRight, false); date.gameObject.SetActive(false); refs.Add("date", date);
            var buttons = Node("ButtonRow", card);
            H(buttons, 0, 0, 0, 0, 8, TextAnchor.MiddleCenter, true, true);
            LE(buttons, minH: 24, prefH: 24);
            Slot(buttons, "AccountsSlot", refs, "accounts", 24, flexW: 1, minW: 0, prefW: 0);
            Slot(buttons, "OrdersSlot", refs, "orders", 24, flexW: 1, minW: 0, prefW: 0);
            var rows = Node("Rows", card);
            V(rows, 0, 0, 0, 0, 2);
            for (int i = 0; i < 3; i++) LabelValue(rows, "Row" + i, refs, "label" + i, "value" + i, 12, XgDark.Muted, 12, XgDark.Ink, true, 16);
            var warn = Node("Warn", card);
            Txt(warn, 11, XgDark.Hot);
            refs.Add("warn", warn);
            return card.gameObject;
        }
    }
}
