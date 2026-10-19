using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The named parts of a 灵光 UI module prefab (Resources/LingGuangV05/Training/TrainPage). The prefab holds the layout
    /// (layout groups, size fitters, text styles); the page code looks its parts up here by key and fills them.
    /// Buttons are slots (<c>XgUi.Button</c> is built into them at runtime so they press and look like every other button);
    /// rounded surfaces are marked with <see cref="XgRoundSurface"/> because the rounded sprite is made at runtime.
    /// Regenerate the prefabs with Tools/灵光/Rebuild training page module prefabs.
    /// </summary>
    public sealed class XgModuleRefs : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public string key;
            public RectTransform rect;
        }

        public List<Entry> entries = new List<Entry>();
        Dictionary<string, RectTransform> map;

        public const string ResourceRoot = "LingGuangV05/Training/TrainPage/";

        public RectTransform Get(string key)
        {
            if (map == null)
            {
                map = new Dictionary<string, RectTransform>();
                foreach (var e in entries) if (e.rect != null && !string.IsNullOrEmpty(e.key)) map[e.key] = e.rect;
            }
            if (map.TryGetValue(key, out var rt)) return rt;
            throw new KeyNotFoundException(name + " has no part '" + key + "' (rebuild the module prefabs)");
        }

        public TMP_Text Text(string key) => Get(key).GetComponent<TMP_Text>();
        public T Part<T>(string key) where T : Component => Get(key).GetComponent<T>();

        /// <summary>Instantiates the module prefab under <paramref name="parent"/>, in the kit's font and with its rounded surfaces.</summary>
        public static XgModuleRefs Spawn(string prefab, RectTransform parent, XgUi kit)
        {
            var source = Resources.Load<GameObject>(ResourceRoot + prefab);
            if (source == null) throw new InvalidOperationException("Missing UI module prefab Resources/" + ResourceRoot + prefab);
            var go = UnityEngine.Object.Instantiate(source, parent, false);
            go.name = prefab;
            Prepare(go.transform, parent.gameObject.layer, kit);
            return go.GetComponent<XgModuleRefs>();
        }

        static void Prepare(Transform root, int layer, XgUi kit)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            if (kit != null && kit.font != null)
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) text.font = kit.font;
            foreach (var round in root.GetComponentsInChildren<XgRoundSurface>(true))
            {
                var image = round.GetComponent<Image>();
                if (image != null) XgUi.RoundSurface(image, round.radius);
            }
        }

        /// <summary>Builds a dark-kit button that fills a slot of the prefab (the slot keeps the layout element).</summary>
        public XgBtn Button(string slot, XgUi kit, string label, UnityEngine.Events.UnityAction onClick, float size, string objectName = null)
        {
            var at = Get(slot);
            var b = kit.Button(at, label, onClick, size);
            b.rt.anchorMin = Vector2.zero; b.rt.anchorMax = Vector2.one; b.rt.offsetMin = b.rt.offsetMax = Vector2.zero;
            if (objectName != null) b.rt.gameObject.name = objectName;
            return b;
        }
    }

    /// <summary>
    /// A button slot in a layout row that is as wide as its label (plus padding), between a minimum and a maximum.
    /// Call <see cref="Fit"/> after the label changes.
    /// </summary>
    public static class XgSlotWidth
    {
        public static void Fit(XgBtn b, float min, float max, float padding = 24)
        {
            if (b == null) return;
            var slot = b.rt.parent as RectTransform;
            var element = slot != null ? slot.GetComponent<LayoutElement>() : null;
            if (element == null) return;
            float w = Mathf.Clamp(b.label.GetPreferredValues(b.label.text, 9999, 100).x + padding, min, max);
            if (!Mathf.Approximately(element.preferredWidth, w)) element.preferredWidth = w;
        }

        /// <summary>A full-width button slot whose label wraps: as tall as its lines (plus padding), at least <paramref name="min"/>.</summary>
        public static void FitHeight(XgBtn b, float min, float padding = 12)
        {
            if (b == null) return;
            var slot = b.rt.parent as RectTransform;
            var element = slot != null ? slot.GetComponent<LayoutElement>() : null;
            if (element == null) return;
            float width = b.label.rectTransform.rect.width;
            if (width < 10) width = (slot.rect.width > 10 ? slot.rect.width : 386) - 12;
            float h = Mathf.Max(min, b.label.GetPreferredValues(b.label.text, width, 0).y + padding);
            if (!Mathf.Approximately(element.preferredHeight, h)) element.minHeight = element.preferredHeight = h;
        }
    }
}
