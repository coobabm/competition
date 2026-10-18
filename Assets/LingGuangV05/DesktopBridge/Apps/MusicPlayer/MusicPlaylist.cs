using System.Collections.Generic;
using UnityEngine;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// A music player playlist asset (replaces DreamOS's MusicPlayerPlaylist; same GUID and fields, so 灵光歌单.asset and
    /// the library assets load unchanged). Music2016 also builds one at runtime from the player's own files.
    /// </summary>
    [CreateAssetMenu(fileName = "New Music Playlist", menuName = "LingGuang/Music Playlist")]
    public class MusicPlaylist : ScriptableObject
    {
        [System.Serializable]
        public class MusicItem
        {
            public string musicTitle = "Music Title";
            public string artistTitle = "Artist Title";
            public string albumTitle = "Album Title";
            public AudioClip musicClip;
            public Sprite musicCover;
            [HideInInspector] public bool excludeFromLibrary;
            [HideInInspector] public bool isModContent;
        }

        public Sprite coverImage;
        public string playlistName;
        public List<MusicItem> playlist = new List<MusicItem>();
    }
}
