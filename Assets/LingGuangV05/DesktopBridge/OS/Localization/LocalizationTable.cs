using System.Collections.Generic;
using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// A named set of localization keys (the key list only; the translated values live in each
    /// <see cref="LocalizationLanguage"/>). Replaces DreamOS's LocalizationTable (2026-10-19): same GUID and
    /// serialized fields.
    /// </summary>
    [CreateAssetMenu(fileName = "New Localization Table", menuName = "LingGuang/OS/Localization/New Localization Table")]
    public class LocalizationTable : ScriptableObject
    {
        public string tableID;
        public LocalizationSettings localizationSettings;
        public List<TableContent> tableContent = new List<TableContent>();

        [System.Serializable]
        public class TableContent
        {
            public string key;
        }
    }
}
