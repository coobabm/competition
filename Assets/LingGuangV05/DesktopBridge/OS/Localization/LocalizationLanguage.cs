using System.Collections.Generic;
using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// One language's strings, audio and sprites, grouped per <see cref="LocalizationTable"/>. Replaces DreamOS's
    /// LocalizationLanguage (2026-10-19): same GUID, serialized fields and nested types, so the existing language assets
    /// load unchanged.
    /// </summary>
    [CreateAssetMenu(fileName = "New Localization Table", menuName = "LingGuang/OS/Localization/New Language")]
    public class LocalizationLanguage : ScriptableObject
    {
        public LocalizationSettings localizationSettings;
        public string languageID;
        public string languageName;
        public string localizedName;
        public List<TableList> tableList = new List<TableList>();

        [System.Serializable]
        public class TableList
        {
            public LocalizationTable table;
            public List<TableContent> tableContent = new List<TableContent>();
        }

        [System.Serializable]
        public class TableContent
        {
            public string key;
            [TextArea] public string value;
            public AudioClip audioValue;
            public Sprite spriteValue;
        }
    }
}
