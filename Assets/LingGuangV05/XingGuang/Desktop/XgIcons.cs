using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The game's drawn icon set: Resources/LingGuangV05/Icons/&lt;group&gt;/&lt;id&gt;.png, made by Tools/Icons (one script per
    /// group, all on iconkit.py so they share one glossy look). Ids may contain dots ("ph.xor"); the file swaps them for
    /// underscores. A missing file returns null, so every caller keeps its old drawn shape as the fallback.
    /// </summary>
    public static class XgIcons
    {
        const string Root = "LingGuangV05/Icons/";
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static string FileName(string id) => id == null ? null : id.Replace('.', '_').Replace('/', '_').Replace(' ', '_');

        public static Sprite Get(string group, string id)
        {
            if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(id)) return null;
            string key = group + "/" + FileName(id);
            if (cache.TryGetValue(key, out var sprite)) return sprite;
            var texture = Resources.Load<Texture2D>(Root + key);
            sprite = texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
            if (sprite != null) sprite.name = "icon_" + group + "_" + FileName(id);
            cache[key] = sprite;
            return sprite;
        }

        public static bool Has(string group, string id) => Get(group, id) != null;

        /// <summary>Adds a preserve-aspect Image child centred at (x, y) from the parent's top-left, or returns null when there is no icon.</summary>
        public static Image Place(Transform parent, string group, string id, float x, float y, float size, bool raycast = false)
        {
            var sprite = Get(group, id);
            if (sprite == null || parent == null) return null;
            var go = new GameObject("Icon " + id, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = raycast;
            return image;
        }

        /// <summary>Puts the icon into an existing Image (keeping its rect), returning false when there is no icon.</summary>
        public static bool Apply(Image image, string group, string id)
        {
            var sprite = Get(group, id);
            if (sprite == null || image == null) return false;
            image.sprite = sprite; image.color = Color.white; image.preserveAspect = true;
            return true;
        }
    }
}
