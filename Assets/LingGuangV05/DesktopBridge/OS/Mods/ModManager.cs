using System.Collections;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Desktop.Apps;
using UnityEngine;
using UnityEngine.Networking;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Loads player-made mods from a "LingGuang_Mods" folder next to the game and feeds them to the music, notepad,
    /// photo and video apps. Replaces DreamOS's ModManager (2026-10-19): same GUID, serialized fields and public API.
    ///
    /// Every "ModData.data" file below the folder is a list of "[Title] ", "[Description] ", "[Icon] ",
    /// "[ModuleType] " lines closed by a "[ModuleAsset] " line, which completes one mod. A list row is created for each
    /// mod, then the module items are imported one by one (music clips are fetched through a web request).
    /// </summary>
    public class ModManager : MonoBehaviour
    {
        // Resources
        [SerializeField] private GameObject modLibraryElement;
        [SerializeField] private Transform modLibraryParent;
        [SerializeField] private GameObject noModsIndicator;

        // Modules
        public bool enableMusicPlayerModule = true;
        [SerializeField] private MusicApp musicPlayer;
        [SerializeField] private bool enableMusicPlayerImportLogs = true;
        [SerializeField] private string musicPlayerID = "Music Player";
        public bool enableNotepadModule = true;
        [SerializeField] private NoteApp notepad;
        [SerializeField] private string notepadID = "Notepad";
        public bool enablePhotoGalleryModule = true;
        [SerializeField] private PhotoApp photoGallery;
        [SerializeField] private string photoGalleryID = "Photo Gallery";
        public bool enableVideoPlayerModule = true;
        [SerializeField] private VideoApp videoPlayer;
        [SerializeField] private string videoPlayerID = "Video Player";

        // Settings
        [SerializeField] private string subPath = "LingGuang_Mods";
        [SerializeField] private string dataName = "ModData";
        [SerializeField] private string fileExtension = ".data";
        [SerializeField] private Sprite defaultIcon;

        string fullPath;
        readonly List<ModItem> mods = new List<ModItem>();

        [System.Serializable]
        public class ModItem
        {
            public string modTitle;
            public string modDescription;
            public Sprite modIcon;
            public string modAsset;
            public ModuleType moduleType;
        }

        public enum ModuleType
        {
            MusicPlayer,
            Notepad,
            PhotoGallery,
            VideoPlayer
        }

        void Awake()
        {
            InitializeMods();
        }

        public void InitializeMods()
        {
            ReadModData();
        }

        /// <summary>The mod folder: inside the project in the editor, next to the player's *_Data folder in a build.</summary>
        void ResolveModFolder()
        {
#if UNITY_EDITOR
            fullPath = Application.dataPath + "/" + subPath + "/";
#else
            string appPath = Application.dataPath;
            appPath = appPath.Replace(Application.productName + "_Data", "");
            fullPath = appPath + subPath + "/";
#endif
        }

        void ReadModData()
        {
            ResolveModFolder();

            if (!Directory.Exists(fullPath))
                return;

            // One data file per mod folder; the file name is always dataName + fileExtension
            var dataFiles = new List<string>();
            foreach (FileInfo file in new DirectoryInfo(fullPath).GetFiles("*" + fileExtension, SearchOption.AllDirectories))
                dataFiles.Add(file.DirectoryName + "/" + dataName + fileExtension);

            // Clear the old list rows
            foreach (Transform child in modLibraryParent) { Destroy(child.gameObject); }

            // These deliberately outlive a single data file, as the fields are read line by line
            int modCount = 0;
            string title = null;
            string description = null;
            string icon = null;
            string moduleType = null;

            foreach (string dataFile in dataFiles)
            {
                foreach (string line in File.ReadLines(dataFile))
                {
                    if (line.Contains("[Title] ")) { title = line.Replace("[Title] ", ""); }
                    else if (line.Contains("[Description] ")) { description = line.Replace("[Description] ", ""); }
                    else if (line.Contains("[Icon] ")) { icon = line.Replace("[Icon] ", ""); }
                    else if (line.Contains("[ModuleType] ")) { moduleType = line.Replace("[ModuleType] ", ""); }
                    else if (line.Contains("[ModuleAsset] "))
                    {
                        string moduleAsset = line.Replace("[ModuleAsset] ", "");

                        Sprite iconSprite;
                        try { iconSprite = LoadNewSprite(fullPath + moduleType + "/" + title + "/" + icon); }
                        catch { iconSprite = defaultIcon; }

                        CreateListRow(iconSprite, title, description, moduleType);
                        CreateModuleItem(iconSprite, title, description, moduleType, moduleAsset);
                        modCount++;
                    }
                }
            }

            // The music player starts from an empty mod playlist
            if (enableMusicPlayerModule) { musicPlayer.modPlaylist.playlist.Clear(); }

            if (modCount == 0) { noModsIndicator.SetActive(true); }
            else
            {
                StartCoroutine(ProcessModules());
                noModsIndicator.SetActive(false);
            }
        }

        void CreateListRow(Sprite icon, string title, string description, string moduleType)
        {
            GameObject go = Instantiate(modLibraryElement, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(modLibraryParent, false);
            go.name = title;

            ModLibraryElement element = go.GetComponent<ModLibraryElement>();
            element.SetIcon(icon);
            element.SetTitle(title);
            element.SetDescription(description);
            element.SetModuleIcon(moduleType);
        }

        void CreateModuleItem(Sprite icon, string title, string desc, string moduleType, string moduleAsset)
        {
            ModItem newItem = new ModItem();
            newItem.modIcon = icon;
            newItem.modTitle = title;
            newItem.modDescription = desc;
            newItem.modAsset = moduleAsset;

            // An unknown module type keeps the enum default (MusicPlayer), as it always has
            if (moduleType == musicPlayerID) { newItem.moduleType = ModuleType.MusicPlayer; }
            else if (moduleType == notepadID) { newItem.moduleType = ModuleType.Notepad; }
            else if (moduleType == photoGalleryID) { newItem.moduleType = ModuleType.PhotoGallery; }
            else if (moduleType == videoPlayerID) { newItem.moduleType = ModuleType.VideoPlayer; }

            mods.Add(newItem);
        }

        IEnumerator ProcessModules()
        {
            int musicPlayerCount = 0;

            for (int i = 0; i < mods.Count; ++i)
            {
                ModItem mod = mods[i];

                if (mod.moduleType == ModuleType.MusicPlayer && enableMusicPlayerModule)
                {
                    MusicPlaylist.MusicItem track = new MusicPlaylist.MusicItem();
                    track.musicTitle = mod.modTitle;
                    track.artistTitle = mod.modDescription;
                    track.musicCover = mod.modIcon;
                    track.isModContent = true;

                    string clipPath = fullPath + musicPlayerID + "/" + mod.modTitle + "/" + mod.modAsset;
                    UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(clipPath, AudioType.MPEG);
                    yield return request.SendWebRequest();

                    if (enableMusicPlayerImportLogs) { Debug.Log(string.Format("Importing ({0}): {1}", musicPlayerCount, mod.modTitle + "/" + mod.modAsset)); }
                    track.musicClip = DownloadHandlerAudioClip.GetContent(request);
                    musicPlayer.modPlaylist.playlist.Add(track);

                    musicPlayerCount++;
                }
                else if (mod.moduleType == ModuleType.Notepad && enableNotepadModule) { notepad.CreateNote(mod.modTitle, mod.modTitle, mod.modAsset); }
                else if (mod.moduleType == ModuleType.PhotoGallery && enablePhotoGalleryModule) { photoGallery.CreatePhoto(mod.modIcon, mod.modTitle, mod.modDescription); }
                else if (mod.moduleType == ModuleType.VideoPlayer && enableVideoPlayerModule)
                {
                    string videoPath = fullPath + videoPlayerID + "/" + mod.modTitle + "/" + mod.modAsset;
                    videoPlayer.CreateVideo(mod.modIcon, mod.modTitle, mod.modDescription, videoPath);
                }
            }

            if (musicPlayerCount > 0) { musicPlayer.InstantiatePlaylist(musicPlayer.modPlaylist, true); }
            yield return null;
        }

        Sprite LoadNewSprite(string filePath, float pixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
        {
            Texture2D texture = DreamOSInternalTools.LoadTexture(filePath);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0), pixelsPerUnit, 0, spriteType);
        }
    }
}
