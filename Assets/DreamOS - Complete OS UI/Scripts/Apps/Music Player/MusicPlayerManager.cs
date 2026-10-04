using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.DreamOS
{
    public class MusicPlayerManager : MonoBehaviour
    {
        // Playlist
        public MusicPlayerPlaylist libraryPlaylist;
        public MusicPlayerPlaylist modPlaylist;
        public List<MusicPlayerPlaylist> customPlaylists = new List<MusicPlayerPlaylist>();

        // Resources
        public AudioSource audioSource;
        public Transform libraryParent;
        [SerializeField] private Transform playlistParent;
        [SerializeField] private Transform playlistPanelParent;
        [SerializeField] private GameObject playlistTrackPreset;
        [SerializeField] private GameObject playlistPanelPreset;
        [SerializeField] private GameObject playlistItemPreset;
        public WindowPanelManager musicPanelManager;
        [SerializeField] private TextMeshProUGUI nowPlayingListTitle;

        // Settings
        public bool repeat;
        public bool shuffle;
        public bool sortListByName = true;
        public bool enablePopupNotification;
        public string playlistSingularLabel = "Song";
        public string playlistPluralLabel = "Songs";
        [SerializeField] private Sprite notificationIcon;

        // Song data
        public int duration;
        public float playTime;
        public int seconds;
        public float secondsRaw;
        public int minutes;
        public MusicPlayerPlaylist currentPlaylist;
        public PlaylistTrack currentTrack;
        public int currentTrackIndex;
        public List<PlaylistTrack> playerQueue = new List<PlaylistTrack>();
        public List<MusicPlayerDataItem> dataToBeUpdated = new List<MusicPlayerDataItem>();

        // Custom clip helpers
        public string customClipName;
        public string customClipArtist;
        public Sprite customClipCover;

        void Awake()
        {
            Initialize();
        }

        void OnDisable()
        {
            Stop();
        }

        public void Initialize()
        {
            // Destroy leftovers
            foreach (Transform child in libraryParent) { Destroy(child.gameObject); }
            foreach (Transform child in playlistParent) { Destroy(child.gameObject); }
            foreach (Transform child in playlistPanelParent) { Destroy(child.gameObject); }

            // Get the required variables
            if (audioSource == null) { audioSource = GetComponent<AudioSource>(); }
            if (sortListByName) { libraryPlaylist.playlist.Sort(SortByName); }

            // Create library playlist
            InstantiatePlaylist(libraryPlaylist, true, libraryParent);

            // Create custom playlists
            for (int i = 0; i < customPlaylists.Count; ++i) 
            {
                InstantiatePlaylist(customPlaylists[i]); 
            }
        }

        public void InstantiatePlaylist(MusicPlayerPlaylist targetPlaylist, bool createContent = true, Transform altTrackParent = null)
        {
            if (targetPlaylist.playlist.Count == 0)
                return;

            // Spawn playlist button
            GameObject go = Instantiate(playlistItemPreset, new Vector3(0, 0, 0), Quaternion.identity);
            go.transform.SetParent(playlistParent, false);
            go.gameObject.name = targetPlaylist.playlistName;

            // Get the playlist item
            PlaylistItem tempItem = go.GetComponent<PlaylistItem>();
            tempItem.playlistID = targetPlaylist.playlistName;

            // Set cover image
            tempItem.coverImage.sprite = targetPlaylist.coverImage;

            // Set title
            tempItem.titleText.text = targetPlaylist.playlistName;

            // Set count
            string countText = null;
            if (targetPlaylist.playlist.Count == 1) { countText = targetPlaylist.playlist.Count.ToString() + " " + playlistSingularLabel; }
            else { countText = targetPlaylist.playlist.Count.ToString() + " " + playlistPluralLabel; }
            tempItem.countText.text = countText;

            // Add play button events
            tempItem.playlistButton.onClick.AddListener(delegate
            {
                musicPanelManager.OpenPanel("Playlist_" + tempItem.playlistID);
            });

            // Create content as well based on the parameter
            if (createContent) { InstantiatePlaylistContent(targetPlaylist, altTrackParent); }
        }

        public void InstantiatePlaylistContent(MusicPlayerPlaylist targetPlaylist, Transform altTrackParent = null)
        {
            // Sort playlist by A to Z
            if (sortListByName) { targetPlaylist.playlist.Sort(SortByName); }

            // Helpers
            bool checkerboardHelper = false;
            PlaylistTrack trackHelper = null;

            // Spawn playlist panel
            GameObject panelGo = Instantiate(playlistPanelPreset, new Vector3(0, 0, 0), Quaternion.identity);
            panelGo.transform.SetParent(playlistPanelParent, false);
            panelGo.gameObject.name = targetPlaylist.playlistName;

            // Create panel
            WindowPanelManager.PanelItem tempPanel = new WindowPanelManager.PanelItem();
            tempPanel.panelName = "Playlist_" + targetPlaylist.playlistName;
            tempPanel.panelObject = panelGo.GetComponent<Animator>();
            musicPanelManager.panels.Add(tempPanel);

            // Get the panel component
            PlaylistPanel panel = panelGo.GetComponent<PlaylistPanel>();
            panel.panelID = targetPlaylist.playlistName;

            // Set panel data
            if (panel.bannerImage != null) { panel.bannerImage.sprite = targetPlaylist.coverImage; }
            panel.coverImage.sprite = targetPlaylist.coverImage;
            panel.titleText.text = targetPlaylist.playlistName;
            string countText = null;
            if (targetPlaylist.playlist.Count == 1) { countText = targetPlaylist.playlist.Count.ToString() + " " + playlistSingularLabel; }
            else { countText = targetPlaylist.playlist.Count.ToString() + " " + playlistPluralLabel; }
            panel.countText.text = countText;

            // Destroy each object in playlist parent
            foreach (Transform child in panel.contentParent) { Destroy(child.gameObject); }

            // Instantiate the entire playlist songs as buttons
            for (int i = 0; i < targetPlaylist.playlist.Count; ++i)
            {
                if (targetPlaylist.playlist[i].excludeFromLibrary || targetPlaylist.playlist[i].musicClip == null)
                    continue;

                // Spawn song button
                GameObject go = Instantiate(playlistTrackPreset, new Vector3(0, 0, 0), Quaternion.identity);
                go.transform.SetParent(panel.contentParent, false);
                go.gameObject.name = targetPlaylist.playlist[i].musicTitle;

                // Get the playlist item
                PlaylistTrack tempTrack = go.GetComponent<PlaylistTrack>();
                tempTrack.itemIndex = i;
                tempTrack.manager = this;
                tempTrack.playlist = targetPlaylist;
                tempTrack.cover = targetPlaylist.playlist[i].musicCover;
                tempTrack.title = targetPlaylist.playlist[i].musicTitle;
                tempTrack.artist = targetPlaylist.playlist[i].artistTitle;
                tempTrack.album = targetPlaylist.playlist[i].albumTitle;
                tempTrack.accentColor = DreamOSInternalTools.GetSpriteAccentColor(tempTrack.cover);
                tempTrack.accentMatchColor = DreamOSInternalTools.GetAccentMatchColor(tempTrack.accentColor);

                // Add track to the panel
                panel.tracks.Add(tempTrack);

                // Set the first track as helper
                if (trackHelper == null) { trackHelper = tempTrack; }

                // Set button BG color
                if (!checkerboardHelper && tempTrack.backgroundImage != null) { tempTrack.backgroundImage.color = new Color32(255, 255, 255, 4); checkerboardHelper = true; }
                else if (tempTrack.backgroundImage != null) { tempTrack.backgroundImage.color = new Color32(255, 255, 255, 0); checkerboardHelper = false; }

                // Set cover image
                tempTrack.coverImage.sprite = targetPlaylist.playlist[i].musicCover;

                // Set ID tags
                tempTrack.titleText.text = targetPlaylist.playlist[i].musicTitle;
                tempTrack.artistText.text = targetPlaylist.playlist[i].artistTitle;
                tempTrack.durationText.text = (((int)targetPlaylist.playlist[i].musicClip.length / 60) % 60) + ":" + ((int)targetPlaylist.playlist[i].musicClip.length % 60).ToString("D2");

                // Add button events
                tempTrack.button.onClick.AddListener(delegate
                {
                    if (currentPlaylist != targetPlaylist || playerQueue.Count == 0)
                    {
                        playerQueue.Clear();
                        foreach (PlaylistTrack tk in panel.tracks) { playerQueue.Add(tk); }
                    }

                    currentPlaylist = targetPlaylist;
                    Play(tempTrack);
                });

                if (altTrackParent != null)
                {
                    GameObject altGo = Instantiate(go, new Vector3(0, 0, 0), Quaternion.identity);
                    altGo.transform.SetParent(altTrackParent, false);
                    altGo.gameObject.name = targetPlaylist.playlist[i].musicTitle;

                    PlaylistTrack altTrack = altGo.GetComponent<PlaylistTrack>();
                    tempTrack.twinTrack = altTrack;

                    // Add button events
                    altTrack.button.onClick.AddListener(delegate
                    {
                        if (currentPlaylist != targetPlaylist || playerQueue.Count == 0)
                        {
                            playerQueue.Clear();
                            foreach (PlaylistTrack tk in panel.tracks) { playerQueue.Add(tk); }
                        }

                        currentPlaylist = targetPlaylist;
                        Play(altTrack);
                    });
                }
            }

            // Reset Play All button and replace with the current playlist songs
            panel.playAllButton.onClick.AddListener(delegate
            {
                playerQueue.Clear();
                foreach (PlaylistTrack tk in panel.tracks) { playerQueue.Add(tk); }
                Play(trackHelper);
            });

            // Update the current track for once
            if (currentTrack == null)
            {
                trackHelper.button.onClick.Invoke();
                Stop();
            }
        }

        public void Play()
        {
            // Set the clip
            if (currentTrack != null) 
            {
                audioSource.clip = currentTrack.playlist.playlist[currentTrack.itemIndex].musicClip;
                currentTrack.SetNowPlayingState(true);
                if (nowPlayingListTitle != null) { nowPlayingListTitle.text = currentTrack.playlist.playlistName; }
            }

            // Return playing
            audioSource.Play();

            // Update all available data items
            UpdateDataItems();

            // Start the playback
            if (gameObject.activeInHierarchy)
            {
                StopCoroutine("ProcessPlayback");
                StartCoroutine("ProcessPlayback");
            }
        }

        public void Play(PlaylistTrack track)
        {
            // Disable previous track np state
            if (currentTrack != null) { currentTrack.SetNowPlayingState(false); }

            // Play a specific music depending on the index
            audioSource.Stop();
            currentTrack = track;
            currentTrackIndex = currentTrack.itemIndex;
            audioSource.clip = track.playlist.playlist[currentTrackIndex].musicClip;
            audioSource.time = 0;
            duration = GetDuration();
            audioSource.Play();

            // Update all available data items
            UpdateDataItems();

            // Update current track np state
            if (currentTrack != null) { currentTrack.SetNowPlayingState(true); }

            // Set the np title
            if (nowPlayingListTitle != null) { nowPlayingListTitle.text = currentTrack.playlist.playlistName; }

            // Start the playback
            if (gameObject.activeInHierarchy)
            {
                StopCoroutine("ProcessPlayback");
                StartCoroutine("ProcessPlayback");
            }
        }

        public void PlayCustomClip(AudioClip clip, Sprite cover, string clipName, string clipAuthor)
        {
            // Update current track np state
            if (currentTrack != null) { currentTrack.SetNowPlayingState(false); }

            // Set clip variables
            customClipName = clipName;
            customClipArtist = clipAuthor;
            customClipCover = cover;
            currentTrack = null;
            currentPlaylist = null;

            // Play the clip
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.time = 0;
            duration = GetDuration();
            audioSource.Play();

            // Update all available data items
            UpdateDataItems();

            // Set the np title
            if (nowPlayingListTitle != null) { nowPlayingListTitle.text = "Custom"; }

            // Start the playback
            if (gameObject.activeInHierarchy)
            {
                StopCoroutine("ProcessPlayback");
                StartCoroutine("ProcessPlayback");
            }
        }

        public void Pause()
        {
            audioSource.Pause();

            // Disable previous track np state
            if (currentTrack != null) { currentTrack.SetNowPlayingState(false); }

            UpdateDataItems();
        }

        public void Stop()
        {
            StopCoroutine("ProcessPlayback");

            if (audioSource != null) { audioSource.Stop(); }
            if (currentTrack != null) { currentTrack.SetNowPlayingState(false); }

            UpdateDataItems();
        }

        public void Mute() 
        { 
            audioSource.mute = !audioSource.mute; 
        }

        public void NextTrack()
        {
            // Update current track np state
            if (currentTrack != null) { currentTrack.SetNowPlayingState(false); }

            // Don't go further if there are no available playlist
            if (currentPlaylist == null) { Stop(); return; }

            // Stop!
            audioSource.Stop();

            // If there's only a single track, then repeat it
            if (currentPlaylist.playlist.Count == 1) { audioSource.Stop(); audioSource.time = 0; audioSource.Play(); }

            // If shuffle is true and repeat is false, select a random song from the current list
            else if (shuffle && !repeat)
            {
                // Remember the current track and then pick a random one
                currentTrack = playerQueue[DreamOSInternalTools.GetRandomUniqueValue(currentTrackIndex, 0, currentPlaylist.playlist.Count)];
                currentTrackIndex = currentTrack.itemIndex;

                // Change the song again if it doesn't meet with criteria
                if (currentPlaylist.playlist[currentTrackIndex].excludeFromLibrary)
                {
                    NextTrack();
                    return;
                }

                // Assign the current song to audio audioSource
                audioSource.clip = currentPlaylist.playlist[currentTrackIndex].musicClip;
            }

            // If not, then just skip to the next song
            else
            {
                currentTrackIndex++;

                // Go back to the first song when reaching to the end of playlist
                if (currentTrackIndex > currentPlaylist.playlist.Count - 1) { currentTrackIndex = 0; }
                if (currentPlaylist.playlist[currentTrackIndex].excludeFromLibrary) { NextTrack(); }

                // Set the currentTrack if we're good
                currentTrack = playerQueue[currentTrackIndex];

                // Assign the current song to audio audioSource
                audioSource.clip = currentPlaylist.playlist[currentTrackIndex].musicClip;
            }

            // Play and change the data
            duration = GetDuration();
            audioSource.time = 0;
            audioSource.Play();

            UpdateDataItems();

            // Set currentTrack now playing indicator state
            if (currentTrack != null) { currentTrack.SetNowPlayingState(true); }

            // If notifications are on, then create one through the creator
            if (enablePopupNotification && NotificationManager.instance != null) 
            {
                NotificationManager.instance.CreatePopupNotification(notificationIcon, currentPlaylist.playlist[currentTrackIndex].musicTitle, currentPlaylist.playlist[currentTrackIndex].artistTitle);
            }
        }

        public void PrevTrack()
        {
            // Disable previous track np state
            if (currentTrack != null) { currentTrack.SetNowPlayingState(false); }

            // Don't go further if there are no available playlist
            if (currentPlaylist == null) { Stop(); return; }

            // Stop!
            audioSource.Stop();

            // If shuffle is true and repeat is false, select a random song from the current list
            if (shuffle && !repeat)
            {
                // Remember the current track and then pick a random one
                currentTrack = playerQueue[DreamOSInternalTools.GetRandomUniqueValue(currentTrackIndex, 0, currentPlaylist.playlist.Count)];
                currentTrackIndex = currentTrack.itemIndex;

                // Assign the current song to audio audioSource
                audioSource.clip = currentPlaylist.playlist[currentTrackIndex].musicClip;
            }

            // If not, then just skip to the previous song
            else
            {
                currentTrackIndex--;

                // Go back to the first song when it doesn't meet the requirements
                if (currentTrackIndex < 0) { currentTrackIndex = currentPlaylist.playlist.Count - 1; }

                // Set the currentTrack if we're good
                currentTrack = playerQueue[currentTrackIndex];

                // Assign the current song to audio audioSource
                audioSource.clip = currentPlaylist.playlist[currentTrackIndex].musicClip;
            }

            // Play and change the data
            duration = GetDuration();
            audioSource.clip = currentPlaylist.playlist[currentTrackIndex].musicClip;
            audioSource.time = 0;
            audioSource.Play();

            // Update all available data items
            UpdateDataItems();

            // Set currentTrack now playing indicator state
            if (currentTrack != null) { currentTrack.SetNowPlayingState(true); }

            // If notifications are on, then create one through the creator
            if (enablePopupNotification && NotificationManager.instance != null) 
            {
                NotificationManager.instance.CreatePopupNotification(notificationIcon, currentPlaylist.playlist[currentTrackIndex].musicTitle, currentPlaylist.playlist[currentTrackIndex].artistTitle);
            }
        }

        public void UpdateDataItems()
        {
            for (int i = 0; i < dataToBeUpdated.Count; ++i)
            {
                if (dataToBeUpdated[i] == null) { dataToBeUpdated.RemoveAt(i); continue; }
                if (dataToBeUpdated[i].alwaysUpdate) { continue; }
                if (dataToBeUpdated[i].gameObject.activeInHierarchy == true) { dataToBeUpdated[i].UpdateItem(); }
            }
        }

        public void SetPopupNotification(bool value) 
        {
            if (value == true) { enablePopupNotification = true; }
            else { enablePopupNotification = false; }
        }

        public int GetDuration() 
        {
            int value = (int)audioSource.clip.length;
            return value;
        }

        public string GetNormalizedDuration()
        {
            string value = ((GetDuration() / 60) % 60) + ":" + (duration % 60).ToString("D2");
            return value;
        }

        public int GetPlayTime() 
        { 
            int value = (int)playTime % 60; secondsRaw = playTime % 60; minutes = ((int)playTime / 60) % 60;
            return value;
        }

        public string GetNormalizedPlayTime()
        {
            string value = minutes + ":" + seconds.ToString("00");
            return value;
        }

        public Sprite GetCoverArt()
        {
            Sprite value;

            if (currentTrack != null) { value = currentTrack.cover; }
            else { value = customClipCover; }

            return value;
        }

        public string GetAlbumName()
        {
            string value;

            if (currentTrack != null) { value = currentTrack.album; }
            else { value = "Custom"; }

            return value;
        }

        public string GetTrackName()
        {
            string value;

            if (currentTrack != null) { value = currentTrack.title; }
            else { value = customClipName; }

            return value;
        }

        public string GetArtistName()
        {
            string value;

            if (currentTrack != null) { value = currentTrack.artist; }
            else { value = customClipArtist; }

            return value;
        }

        public Color GetAccentColor(Image targetImage)
        {
            Color value = new Color(25, 35, 45, targetImage.color.a);
            if (currentTrack != null) { value = new Color(currentTrack.accentColor.r, currentTrack.accentColor.g, currentTrack.accentColor.b, targetImage.color.a); }
            return value;
        }

        public Color GetAccentMatchColor(Image targetImage)
        {
            Color value = new Color(25, 35, 45, targetImage.color.a);
            if (currentTrack != null) { value = new Color(currentTrack.accentMatchColor.r, currentTrack.accentMatchColor.g, currentTrack.accentMatchColor.b, targetImage.color.a); }
            return value;
        }

        public Color GetAccentColor(TextMeshProUGUI targetText)
        {
            Color value = new Color(25, 35, 45, targetText.color.a);
            if (currentTrack != null) { value = new Color(currentTrack.accentColor.r, currentTrack.accentColor.g, currentTrack.accentColor.b, targetText.color.a); }
            return value;
        }

        public Color GetAccentMatchColor(TextMeshProUGUI targetText)
        {
            Color value = new Color(25, 35, 45, targetText.color.a);
            if (currentTrack != null) { value = new Color(currentTrack.accentMatchColor.r, currentTrack.accentMatchColor.g, currentTrack.accentMatchColor.b, targetText.color.a); }
            return value;
        }

        private static int SortByName(MusicPlayerPlaylist.MusicItem o1, MusicPlayerPlaylist.MusicItem o2) 
        {
            return o1.musicTitle.CompareTo(o2.musicTitle);
        }

        IEnumerator ProcessPlayback()
        {
            while (audioSource.isPlaying)
            {
                // Update the playback time while audioSource is playing
                playTime = audioSource.time;
                seconds = GetPlayTime();

                // Change playback based on parameters
                if (playTime >= duration && repeat && !shuffle) { audioSource.Stop(); audioSource.time = 0; audioSource.Play(); }
                else if (playTime >= duration) { NextTrack(); }

                yield return null;
            }
        }
    }
}