using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The browser's internet: its pages (URL, title, icon, prefab, size in MB) and downloadable files (moved from
    /// DreamOS 2026-10-19; same GUID, fields and enum order, so the existing libraries load unchanged).
    /// </summary>
    [CreateAssetMenu(fileName = "New Web Library", menuName = "LingGuang/Web Library")]
    public class WebBrowserLibrary : ScriptableObject
    {
        public WebPage homePage;
        public WebPage notFoundPage;
        public WebPage noConnectionPage;
        public List<WebPage> webPages = new List<WebPage>();
        public List<DownloadableFiles> dlFiles = new List<DownloadableFiles>();

        [System.Serializable]
        public class WebPage
        {
            public string pageTitle = "Web Page Title";
            public string pageURL = "www.example.com";
            public Sprite pageIcon;
            public GameObject pageContent;
            [Range(0.1f, 100)] public float pageSize = 10;

            [Header("Localization")]
            public string titleKey;
        }

        [System.Serializable]
        public class DownloadableFiles
        {
            public string fileName = "Title";
            public Sprite fileIcon;
            public float fileSize = 5;
            [Space(20)]
            public FileType fileType;
            public AudioClip musicReference;
            public Sprite photoReference;
            public VideoClip videoReference;
            [TextArea(1, 8)] public string noteReference;
        }

        /// <summary>Which app opens a finished download.</summary>
        public enum FileType { Other, Music, Note, Photo, Video }
    }
}
