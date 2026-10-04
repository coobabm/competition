using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Michsky.DreamOS
{
    public class ModManager : MonoBehaviour
    {
        // Resources
        [SerializeField] private GameObject modLibraryElement;
        [SerializeField] private Transform modLibraryParent;
        [SerializeField] private GameObject noModsIndicator;

        // Modules
        public bool enableMusicPlayerModule = true;
        [SerializeField] private MusicPlayerManager musicPlayer;
        [SerializeField] private bool enableMusicPlayerImportLogs = true;
        [SerializeField] private string musicPlayerID = "Music Player";
        public bool enableNotepadModule = true;
        [SerializeField] private NotepadManager notepad;
        [SerializeField] private string notepadID = "Notepad";
        public bool enablePhotoGalleryModule = true;
        [SerializeField] private PhotoGalleryManager photoGallery;
        [SerializeField] private string photoGalleryID = "Photo Gallery";
        public bool enableVideoPlayerModule = true;
        [SerializeField] private VideoPlayerManager videoPlayer;
        [SerializeField] private string videoPlayerID = "Video Player";

        // Settings
        [SerializeField] private string subPath = "DreamOS_Mods";
        [SerializeField] private string dataName = "ModData";
        [SerializeField] private string fileExtension = ".data";
        [SerializeField] private Sprite defaultIcon;

        // Helpers
        string fullPath;
        List<ModItem> mods = new List<ModItem>();

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

        void CheckForDataFile()
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
            CheckForDataFile();

            if (!Directory.Exists(fullPath))
                return;

            // Scan available mods
            List<string> scannedMods = new List<string>();
            DirectoryInfo dir = new DirectoryInfo(fullPath);
            FileInfo[] info = dir.GetFiles("*" + fileExtension, SearchOption.AllDirectories);

            // Add scanned mods to list
            foreach (FileInfo file in info) { scannedMods.Add(file.DirectoryName + "/" + dataName + fileExtension); }

            // Clear the cache
            foreach (Transform child in modLibraryParent) { Destroy(child.gameObject); }

            // Create mod cache
            int modCount = 0;
            string tempTitle = null;
            string tempDesc = null;
            string tempIcon = null;
            string tempModuleType = null;

            // Process mods
            for (int i = 0; i < scannedMods.Count; ++i)
            {
                foreach (string option in File.ReadLines(scannedMods[i]))
                {
                    if (option.Contains("[Title] ")) { tempTitle = option.Replace("[Title] ", ""); }
                    else if (option.Contains("[Description] ")) { tempDesc = option.Replace("[Description] ", ""); }
                    else if (option.Contains("[Icon] ")) { tempIcon = option.Replace("[Icon] ", ""); }
                    else if (option.Contains("[ModuleType] ")) { tempModuleType = option.Replace("[ModuleType] ", ""); }
                    else if (option.Contains("[ModuleAsset] "))
                    {
                        Sprite tempIconSprite = null;
                        string tempModuleAsset = option.Replace("[ModuleAsset] ", "");

                        GameObject go = Instantiate(modLibraryElement, new Vector3(0, 0, 0), Quaternion.identity);
                        go.transform.SetParent(modLibraryParent, false);
                        go.name = tempTitle;

                        try { tempIconSprite = LoadNewSprite(fullPath + tempModuleType + "/" + tempTitle + "/" + tempIcon); }
                        catch { tempIconSprite = defaultIcon; }

                        ModLibraryElement libElement = go.GetComponent<ModLibraryElement>();
                        libElement.SetIcon(tempIconSprite);
                        libElement.SetTitle(tempTitle);
                        libElement.SetDescription(tempDesc);
                        libElement.SetModuleIcon(tempModuleType);

                        CreateModuleItem(tempIconSprite, tempTitle, tempDesc, tempModuleType, tempModuleAsset);

                        modCount++;
                    }
                }
            }

            // Delete music player mod playlist if enabled
            if (enableMusicPlayerModule) { musicPlayer.modPlaylist.playlist.Clear(); }

            // Process mods if available
            if (modCount == 0) { noModsIndicator.SetActive(true); }
            else 
            {
                StartCoroutine(ProcessModules());
                noModsIndicator.SetActive(false); 
            }
        }

        void CreateModuleItem(Sprite icon, string title, string desc, string moduleType, string moduleAsset)
        {
            ModItem newItem = new ModItem();
            newItem.modIcon = icon;
            newItem.modTitle = title;
            newItem.modDescription = desc;
            newItem.modAsset = moduleAsset;

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
                if (mods[i].moduleType == ModuleType.MusicPlayer && enableMusicPlayerModule)
                {
                    MusicPlayerPlaylist.MusicItem tempItem = new MusicPlayerPlaylist.MusicItem();
                    tempItem.musicTitle = mods[i].modTitle;
                    tempItem.artistTitle = mods[i].modDescription;
                    tempItem.musicCover = mods[i].modIcon;
                    tempItem.isModContent = true;

                    string mPath = fullPath + musicPlayerID + "/" + mods[i].modTitle + "/" + mods[i].modAsset;
                    UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(mPath, AudioType.MPEG);
                    yield return www.SendWebRequest();

                    if (enableMusicPlayerImportLogs) { Debug.Log(string.Format("Importing ({0}): {1}", musicPlayerCount, mods[i].modTitle + "/" + mods[i].modAsset)); }
                    tempItem.musicClip = DownloadHandlerAudioClip.GetContent(www);
                    musicPlayer.modPlaylist.playlist.Add(tempItem);

                    musicPlayerCount++;
                }
                else if (mods[i].moduleType == ModuleType.Notepad && enableNotepadModule) { notepad.CreateNote(mods[i].modTitle, mods[i].modTitle, mods[i].modAsset); }
                else if (mods[i].moduleType == ModuleType.PhotoGallery && enablePhotoGalleryModule) { photoGallery.CreatePhoto(mods[i].modIcon, mods[i].modTitle, mods[i].modDescription); }
                else if (mods[i].moduleType == ModuleType.VideoPlayer && enableVideoPlayerModule) 
                {
                    string vPath = fullPath + videoPlayerID + "/" + mods[i].modTitle + "/" + mods[i].modAsset;
                    videoPlayer.CreateVideo(mods[i].modIcon, mods[i].modTitle, mods[i].modDescription, vPath); 
                }
            }

            if (musicPlayerCount > 0) { musicPlayer.InstantiatePlaylist(musicPlayer.modPlaylist, true); }
            yield return null;
        }

        Sprite LoadNewSprite(string filePath, float pixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
        {
            Texture2D spriteTexture = DreamOSInternalTools.LoadTexture(filePath);
            Sprite newSprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), pixelsPerUnit, 0, spriteType);
            return newSprite;
        }
    }
}