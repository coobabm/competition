using System.Collections.Generic;
using UnityEngine;

namespace HolenderGames.UpgradesTreePro
{
    /// <summary>
    /// A db to define colored tags for styled text descriptions
    /// </summary>
    [CreateAssetMenu(menuName = "Game Data/TextStyle Database")]
    public class TextStyleDB : ScriptableObject
    {
        [System.Serializable]
        public class StyleEntry
        {
            public string tag;      // e.g., "gold", "rare"
            public Color color;     // color for the tag
        }

        public List<StyleEntry> entries = new List<StyleEntry>();

        private Dictionary<string, Color> _cache;

        public void BuildCache()
        {
            if (_cache == null)
            {
                _cache = new Dictionary<string, Color>();
                foreach (var e in entries)
                {
                    if (!_cache.ContainsKey(e.tag))
                        _cache.Add(e.tag, e.color);
                }
            }
        }

        public bool TryGetColor(string tag, out Color color)
        {
            BuildCache();
            return _cache.TryGetValue(tag, out color);
        }
    }

}