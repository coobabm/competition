using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.EditorTools
{
    /// <summary>
    /// Play QA of the 训练页 right column (the module prefabs): puts an isolated lab (StageOneTutorPlayQa.Arm + Setup)
    /// into a named state, language and window size, then measures every text and button of the column for overlaps and
    /// for text that does not fit its box. Driven from MCP: Apply(state, english, width, height), then Check(), then
    /// StageOneTutorPlayQa.Shot(path).
    /// States: 0 stage 2, 装备 tab, low funds; 1 它学会了吗; 2 书架 with its question box; 3 书架 with 「后面呢？」;
    /// 4 stage 6 (effective scale); 5 the ledger still sealed; 6 long text (every label padded).
    /// </summary>
    public static class XgTrainSideLayoutQa
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static string language = "zh-CN";
        static IDisposable locale;

        static XingGuangController Ctl()
        {
            var rt = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>();
            return rt != null ? rt.GetComponent<XingGuangController>() : null;
        }

        static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, Flags).GetValue(o);
        static void SetField(object o, string name, object value) => o.GetType().GetField(name, Flags).SetValue(o, value);

        public static string Apply(int state, bool english, float width = 1064, float height = 680)
        {
            if (!EditorApplication.isPlaying) return "refused: not playing";
            var ctl = Ctl(); var sim = ctl.Sim;
            if (locale == null) locale = GameText.Bind(() => language);
            language = english ? GameText.English : "zh-CN";
            var englishField = typeof(XgSim).GetField("English", Flags);
            if (englishField != null) englishField.SetValue(sim, english);
            GameText.PublishChanged();

            var s = sim.S;
            s.teachingFlow = state == 5;
            s.stage = state == 4 ? 6 : 2;
            s.epochs = Math.Max(s.epochs, 12);
            foreach (string id in new[] { "dropout", "relu", "mlp", "lenet" }) if (!s.unlocked.Contains(id)) s.unlocked.Add(id);
            if (state == 4) foreach (string id in new[] { "transformer", "resnet", "cudnn", "augment", "warmup" }) if (!s.unlocked.Contains(id)) s.unlocked.Add(id);
            if (s.books == null) s.books = new List<XgScore>();
            if (s.books.Count == 0) s.books.Add(new XgScore { key = XgBooks.All[0].id, count = 2 });
            s.redMansionBeat = state == 3 ? 1 : 0;
            s.bookPuzzle = "";
            if (state == 2)
                foreach (var b in XgBooks.All) { var p = XgBooks.Puzzles(b.id); if (p.Length > 0) { s.bookPuzzle = p[0].id; break; } }
            var house = UnityEngine.Object.FindAnyObjectByType<ChapterOneRuntime>().Sim;
            house.S.money = state == 0 || state == 6 ? 12 : 4800;

            var frame = ctl.Window.windowContainer;
            frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            ctl.View.Open("train");
            var train = ctl.View.Train;
            SetField(train, "learningView", state == 1 || state == 6);
            SetField(train, "booksView", state == 2 || state == 3);
            ctl.View.Refresh(true);
            if (state == 6) Pad(train);
            Canvas.ForceUpdateCanvases();
            return "state " + state + (english ? " en " : " zh ") + width + "x" + height + " stage " + s.stage;
        }

        /// <summary>Doubles every text of the column (the long-text case: names, values and buttons twice as long).</summary>
        static void Pad(XgTrainPage train)
        {
            var right = Field<RectTransform>(train, "right");
            foreach (var t in right.GetComponentsInChildren<TMP_Text>(false))
                if (!string.IsNullOrEmpty(t.text) && t.GetComponentInParent<TMP_InputField>() == null) t.text = t.text + " " + t.text;
        }

        sealed class Leaf { public string name; public Rect rect; public Transform t; }

        /// <summary>Overlaps and misfits in the right column, in canvas units; "ok" when there are none.</summary>
        public static string Check()
        {
            if (!EditorApplication.isPlaying) return "refused: not playing";
            var train = Ctl().View.Train;
            var right = Field<RectTransform>(train, "right");
            Canvas.ForceUpdateCanvases();
            var issues = new List<string>();
            var leaves = new List<Leaf>();
            var column = right.GetChild(0);
            Rect columnRect = WorldRect(right);
            foreach (var t in right.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrEmpty(t.text) || !t.enabled) continue;
                if (t.GetComponentInParent<XgTrainingSeal>() != null) continue;
                t.ForceMeshUpdate(true, true);
                var rt = t.rectTransform;
                // Fit: wrapped text taller than its box, or a one-line text wider than its box and neither shrunk nor cut.
                Vector2 pref = t.GetPreferredValues(t.text, t.textWrappingMode == TextWrappingModes.NoWrap ? 99999 : rt.rect.width, 0);
                bool shrinks = t.enableAutoSizing, cuts = t.overflowMode != TextOverflowModes.Overflow;
                if (!shrinks && !cuts && t.textWrappingMode != TextWrappingModes.NoWrap && pref.y > rt.rect.height + 1.5f && t.GetComponentInParent<ScrollRect>() == null)
                    issues.Add("taller than box: " + Path(rt) + " " + pref.y.ToString("0") + ">" + rt.rect.height.ToString("0") + " '" + Short(t.text) + "'");
                if (!shrinks && !cuts && t.textWrappingMode == TextWrappingModes.NoWrap && pref.x > rt.rect.width + 1.5f)
                    issues.Add("wider than box: " + Path(rt) + " " + pref.x.ToString("0") + ">" + rt.rect.width.ToString("0") + " '" + Short(t.text) + "'");
                if (cuts && t.isTextOverflowing) issues.Add("note, cut with ellipsis: " + Path(rt) + " '" + Short(t.text) + "'");
                if (shrinks && t.fontSize <= t.fontSizeMin + .01f && t.isTextOverflowing) issues.Add("note, shrunk to min and still long: " + Path(rt) + " '" + Short(t.text) + "'");
                if (t.GetComponentInParent<XgButtonPressFeedback>() != null) continue;
                leaves.Add(new Leaf { name = Path(rt), rect = Clip(rt, InkRect(t)), t = rt });
            }
            foreach (var b in right.GetComponentsInChildren<XgButtonPressFeedback>(false))
            {
                if (b.GetComponentInParent<XgTrainingSeal>() != null && b.GetComponent<XgTrainingSeal>() == null) continue;
                var rt = (RectTransform)b.transform;
                leaves.Add(new Leaf { name = Path(rt), rect = Clip(rt, WorldRect(rt)), t = rt });
            }
            for (int i = 0; i < leaves.Count; i++)
                for (int j = i + 1; j < leaves.Count; j++)
                {
                    var a = leaves[i]; var b = leaves[j];
                    if (a.rect.width <= 0 || b.rect.width <= 0 || a.rect.height <= 0 || b.rect.height <= 0) continue;
                    if (a.t.IsChildOf(b.t) || b.t.IsChildOf(a.t)) continue;
                    float w = Mathf.Min(a.rect.xMax, b.rect.xMax) - Mathf.Max(a.rect.xMin, b.rect.xMin);
                    float h = Mathf.Min(a.rect.yMax, b.rect.yMax) - Mathf.Max(a.rect.yMin, b.rect.yMin);
                    if (w > 1 && h > 1) issues.Add("OVERLAP " + w.ToString("0") + "x" + h.ToString("0") + ": " + a.name + "  ×  " + b.name);
                }
            var sb = new StringBuilder();
            sb.Append("column ").Append(right.rect.width.ToString("0")).Append("x").Append(right.rect.height.ToString("0"))
              .Append(" content ").Append(((RectTransform)column).rect.height.ToString("0"));
            foreach (Transform card in column)
                if (card.gameObject.activeSelf) sb.Append(" | ").Append(card.name).Append(" ").Append(((RectTransform)card).rect.height.ToString("0"));
            sb.Append("\nleaves ").Append(leaves.Count).Append(", issues ").Append(issues.Count);
            foreach (var i in issues) sb.Append("\n").Append(i);
            return sb.ToString();
        }

        /// <summary>The drawn glyphs' rectangle (a text box may be taller than its lines; only ink can collide).</summary>
        static Rect InkRect(TMP_Text t)
        {
            var b = t.textBounds;
            if (b.size.x <= 0 || b.size.y <= 0) return WorldRect(t.rectTransform);
            var min = t.rectTransform.TransformPoint(b.min); var max = t.rectTransform.TransformPoint(b.max);
            return CanvasRect(min, max, t.rectTransform);
        }

        static Rect WorldRect(RectTransform rt)
        {
            var c = new Vector3[4]; rt.GetWorldCorners(c);
            return CanvasRect(c[0], c[2], rt);
        }

        static Rect CanvasRect(Vector3 a, Vector3 b, Transform any)
        {
            var canvas = any.GetComponentInParent<Canvas>().rootCanvas.transform;
            var p = canvas.InverseTransformPoint(a); var q = canvas.InverseTransformPoint(b);
            return Rect.MinMaxRect(Mathf.Min(p.x, q.x), Mathf.Min(p.y, q.y), Mathf.Max(p.x, q.x), Mathf.Max(p.y, q.y));
        }

        /// <summary>Cuts a rectangle to every RectMask2D above it (what scrolls out of view is not drawn).</summary>
        static Rect Clip(RectTransform rt, Rect r)
        {
            for (var t = rt.parent; t != null; t = t.parent)
            {
                if (t.GetComponent<RectMask2D>() == null) continue;
                var m = WorldRect((RectTransform)t);
                float xMin = Mathf.Max(r.xMin, m.xMin), yMin = Mathf.Max(r.yMin, m.yMin), xMax = Mathf.Min(r.xMax, m.xMax), yMax = Mathf.Min(r.yMax, m.yMax);
                r = xMax > xMin && yMax > yMin ? Rect.MinMaxRect(xMin, yMin, xMax, yMax) : new Rect(0, 0, 0, 0);
            }
            return r;
        }

        static string Path(Transform t)
        {
            string s = t.name;
            for (int i = 0; i < 3 && t.parent != null; i++) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }

        static string Short(string s) { s = s.Replace("\n", "⏎"); return s.Length > 40 ? s.Substring(0, 40) + "…" : s; }

        /// <summary>The training page's window, cut out of the LCD capture, for a close look.</summary>
        public static string Shot(string file) => StageOneTutorPlayQa.Shot(file);
    }
}
