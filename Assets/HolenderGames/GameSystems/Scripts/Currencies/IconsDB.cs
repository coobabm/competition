using System.Collections.Generic;
using UnityEngine;

namespace HolenderGames.Currencies
{
    /// <summary>
    /// A db to hold all the Currencies Icons
    /// </summary>
    [CreateAssetMenu(menuName = "Game Data/Icons Database")]
    public class IconsDB : ScriptableObject
    {
        [System.Serializable]
        public class IconEntry
        {
            public CurrencyType type;
            public Sprite icon;
        }

        [Header("Currency → Icon Mapping")]
        public List<IconEntry> entries = new();

        private Dictionary<CurrencyType, Sprite> _dict;

        public void BuildCache()
        {
            if (_dict == null)
            {
                _dict = new Dictionary<CurrencyType, Sprite>();

                foreach (var entry in entries)
                {
                    if (entry.icon == null)
                        Debug.LogWarning($"IconDB: Missing icon for {entry.type}");

                    _dict[entry.type] = entry.icon;
                }
            }
        }

        public Sprite GetIcon(CurrencyType type)
        {
            if (_dict == null)
                BuildCache();

            return _dict.TryGetValue(type, out var sprite) ? sprite : null;
        }
    }
}

