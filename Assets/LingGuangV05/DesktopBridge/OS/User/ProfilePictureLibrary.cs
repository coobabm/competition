using System.Collections.Generic;
using UnityEngine;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// The list of avatars a user can pick from (2026-10-19). Replaces DreamOS's ProfilePictureLibrary: same GUID and
    /// serialized layout, so the existing library asset keeps its pictures. <see cref="UserManager"/> indexes it by
    /// <see cref="UserManager.ppIndex"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "New Profile Picture Library", menuName = "LingGuang/OS/New Profile Picture Library")]
    public class ProfilePictureLibrary : ScriptableObject
    {
        public List<PPItem> pictures = new List<PPItem>();

        [System.Serializable]
        public class PPItem
        {
            public string pictureID = "Picture";
            public Sprite pictureSprite;
        }
    }
}
