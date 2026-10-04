using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The gold ring the to-do note puts around the control a step is about: it breathes for a few seconds, then
    /// fades and removes itself. It sits on top of the target as a last child, ignores layout and never takes clicks.
    /// The static helpers find targets inside 灵光.exe by tab id and a small locator (see XgGuideStep), and inside
    /// any other window by object name or button text. Nothing in the pages is edited: nav buttons that have no
    /// stable name get one ("Tab label", "Tab train", …) the first time they are looked up.
    /// Photosensitivity: the ring only breathes (about one second per cycle), it never flashes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class XgGuideHighlight : MonoBehaviour
    {
        public const float DefaultSeconds = 4f;
        /// <summary>The lab's nav order (XingGuangView.BuildNav): the first buttons under "Nav" are these tabs.</summary>
        public static readonly string[] TabIds = { "label", "train", "tree", "contracts", "repo", "board", "wall", "chat", "final" };
        static readonly Color Gold = new Color32(255, 190, 40, 255);
        const float Pad = 6, Fade = .35f;

        XgGuideRingGraphic ring;
        float age, life;

        /// <summary>Rings the target for <paramref name="seconds"/>; a ring already on it starts over.</summary>
        public static XgGuideHighlight Pulse(RectTransform target, float seconds = DefaultSeconds)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return null;
            var old = target.Find("Guide Ring");
            var existing = old != null ? old.GetComponent<XgGuideHighlight>() : null;
            if (existing != null) { existing.age = 0; existing.life = seconds; existing.transform.SetAsLastSibling(); return existing; }
            var go = new GameObject("Guide Ring", typeof(RectTransform));
            go.layer = target.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(target, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-Pad, -Pad); rt.offsetMax = new Vector2(Pad, Pad);
            rt.SetAsLastSibling();
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            var h = go.AddComponent<XgGuideHighlight>();
            h.ring = go.AddComponent<XgGuideRingGraphic>();
            h.ring.raycastTarget = false;
            h.ring.color = Gold;
            h.life = seconds;
            return h;
        }

        void Update()
        {
            age += Time.unscaledDeltaTime;
            if (ring == null || age >= life + Fade) { Destroy(gameObject); return; }
            float breathe = .55f + .45f * (.5f + .5f * Mathf.Sin(age * 6.3f - 1.57f));
            float fade = age < .2f ? age / .2f : age > life ? 1 - (age - life) / Fade : 1;
            var c = Gold; c.a = Mathf.Clamp01(breathe * fade); ring.color = c;
            ring.Spread = 2 + 4 * (.5f + .5f * Mathf.Sin(age * 6.3f - 1.57f));
        }

        // ───────────── finding targets in 灵光.exe ─────────────

        static Transform Root(XingGuangView view) => view != null ? view.transform.Find("XingGuang") : null;

        /// <summary>The nav button of a tab (named "Tab id" on first use).</summary>
        public static RectTransform TabButton(XingGuangView view, string tab)
        {
            var nav = Root(view)?.Find("Nav");
            if (nav == null || string.IsNullOrEmpty(tab)) return null;
            var named = nav.Find("Tab " + tab) as RectTransform;
            if (named != null) return named;
            int index = 0;
            foreach (Transform child in nav)
            {
                if (child.GetComponent<Button>() == null) continue;
                if (index >= TabIds.Length) break;
                if (!child.name.StartsWith("Tab ", StringComparison.Ordinal)) child.name = "Tab " + TabIds[index];
                index++;
            }
            return nav.Find("Tab " + tab) as RectTransform;
        }

        /// <summary>The page root of a tab (pages are named by their id).</summary>
        public static RectTransform Page(XingGuangView view, string tab) => string.IsNullOrEmpty(tab) ? null : Root(view)?.Find("Pages/" + tab) as RectTransform;

        /// <summary>
        /// A control on a lab page. Locators: "" = the tab button, "name:Foo", "label:是|Yes", "node:id" (also
        /// scrolls the skill tree to it), "contract:id", "chip:n" (the HUD chips).
        /// </summary>
        public static RectTransform Find(XingGuangView view, string tab, string target)
        {
            if (view == null) return null;
            target = target ?? "";
            if (target.Length == 0) return TabButton(view, tab);
            if (target.StartsWith("chip:", StringComparison.Ordinal)) return Root(view)?.Find("Hud/Chip" + target.Substring(5)) as RectTransform;
            var page = Page(view, tab);
            if (page == null || !page.gameObject.activeInHierarchy) return null;
            if (target.StartsWith("node:", StringComparison.Ordinal))
            {
                string id = target.Substring(5);
                if (view.Tree != null) view.Tree.FocusNode(id);
                foreach (var node in page.GetComponentsInChildren<XgNodeView>(false))
                    if (node.node != null && node.node.id == id) return (RectTransform)node.transform;
                return null;
            }
            if (target.StartsWith("contract:", StringComparison.Ordinal)) return FindIn(page, "name:Row" + target.Substring(9));
            return FindIn(page, target);
        }

        static readonly Regex Tags = new Regex("<[^>]*>", RegexOptions.Compiled);

        /// <summary>"name:Foo" (an active object called Foo) or "label:a|b" (an active button whose text starts with a or b).</summary>
        public static RectTransform FindIn(Transform root, string target)
        {
            if (root == null || string.IsNullOrEmpty(target)) return null;
            if (target.StartsWith("name:", StringComparison.Ordinal))
            {
                string name = target.Substring(5);
                foreach (var t in root.GetComponentsInChildren<RectTransform>(false)) if (t.name == name) return t;
                return null;
            }
            if (target.StartsWith("label:", StringComparison.Ordinal))
            {
                var options = target.Substring(6).Split('|');
                foreach (var button in root.GetComponentsInChildren<Button>(false))
                {
                    var label = button.GetComponentInChildren<TMP_Text>(false);
                    if (label == null) continue;
                    string text = Tags.Replace(label.text ?? "", "").Trim();
                    foreach (var o in options) if (o.Length > 0 && text.StartsWith(o, StringComparison.Ordinal)) return (RectTransform)button.transform;
                }
            }
            return null;
        }

        /// <summary>Rings a lab control, or its tab button when the control is not on screen. False if neither was found.</summary>
        public static bool Show(XingGuangView view, string tab, string target, float seconds = DefaultSeconds)
        {
            var rt = Find(view, tab, target) ?? TabButton(view, tab);
            return Pulse(rt, seconds) != null;
        }
    }

    /// <summary>A hollow frame with a soft outer band, drawn in code (no sprite).</summary>
    public sealed class XgGuideRingGraphic : MaskableGraphic
    {
        float spread = 3;
        /// <summary>Width of the soft outer edge in pixels.</summary>
        public float Spread { get => spread; set { if (Mathf.Abs(value - spread) < .05f) return; spread = value; SetVerticesDirty(); } }
        const float Line = 3;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            Frame(vh, r, Line, color);
            var soft = color; soft.a *= .35f;
            var outer = new Rect(r.xMin - spread, r.yMin - spread, r.width + spread * 2, r.height + spread * 2);
            Frame(vh, outer, spread, soft);
        }

        static void Frame(VertexHelper vh, Rect r, float w, Color c)
        {
            if (r.width <= 0 || r.height <= 0 || w <= 0) return;
            XgDraw.Box(vh, new Vector2(r.xMin, r.yMax - w), new Vector2(r.xMax, r.yMax), c);
            XgDraw.Box(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin + w), c);
            XgDraw.Box(vh, new Vector2(r.xMin, r.yMin + w), new Vector2(r.xMin + w, r.yMax - w), c);
            XgDraw.Box(vh, new Vector2(r.xMax - w, r.yMin + w), new Vector2(r.xMax, r.yMax - w), c);
        }
    }
}
