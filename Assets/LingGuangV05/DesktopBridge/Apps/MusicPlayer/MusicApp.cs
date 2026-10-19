using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The desktop music player (2026-10-19), which also plays the game's background music (GameMusic). Replaces
    /// DreamOS's MusicPlayerManager: same GUID, serialized fields and public members, so the Main scene's player, its
    /// playlists and controls carry over unchanged.
    ///
    /// The library playlist is listed on the library page and gets a sidebar button and its own page; every custom
    /// playlist gets a sidebar button and a page. Clicking a track queues its page's tracks and plays it; at the end of a
    /// track the player repeats, shuffles or moves on (unless GameMusic advances the queue itself). Texts, sliders and
    /// buttons that follow the playing track are <see cref="MusicAppPart"/>s.
    /// </summary>
    public class MusicApp : MonoBehaviour
    {
        public MusicPlaylist libraryPlaylist;
        public MusicPlaylist modPlaylist;
        public List<MusicPlaylist> customPlaylists = new List<MusicPlaylist>();

        public AudioSource audioSource;
        public Transform libraryParent;
        [SerializeField] Transform playlistParent;
        [SerializeField] Transform playlistPanelParent;
        [SerializeField] GameObject playlistTrackPreset;
        [SerializeField] GameObject playlistPanelPreset;
        [SerializeField] GameObject playlistItemPreset;
        public WindowPanelManager musicPanelManager;
        [SerializeField] TextMeshProUGUI nowPlayingListTitle;

        public bool repeat;
        /// <summary>Background music: closing the window leaves the music playing (GameMusic).</summary>
        [HideInInspector] public bool keepPlayingWhenClosed;
        /// <summary>When true, GameMusic alone advances finished tracks.</summary>
        [HideInInspector] public bool externalPlaylistAdvance;
        /// <summary>The player was last asked to play (not paused or stopped).</summary>
        public bool PlaybackRequested { get; private set; }
        public bool shuffle;
        public bool sortListByName = true;
        public bool enablePopupNotification;
        public string playlistSingularLabel = "Song";
        public string playlistPluralLabel = "Songs";
        [SerializeField] Sprite notificationIcon;

        // The playing track.
        public int duration;
        public float playTime;
        public int seconds;
        public float secondsRaw;
        public int minutes;
        public MusicPlaylist currentPlaylist;
        public MusicTrackRow currentTrack;
        public int currentTrackIndex;
        public List<MusicTrackRow> playerQueue = new List<MusicTrackRow>();
        public List<MusicAppPart> dataToBeUpdated = new List<MusicAppPart>();

        // A clip played from outside the playlists (a mail attachment, a download).
        public string customClipName;
        public string customClipArtist;
        public Sprite customClipCover;

        Coroutine playback;

        void Awake() => Initialize();

        void OnEnable()
        {
            // Music that kept going while the window was closed gets its playback loop back.
            if (keepPlayingWhenClosed && audioSource != null && audioSource.isPlaying) RestartPlayback();
        }

        void OnDisable()
        {
            if (keepPlayingWhenClosed) { StopPlayback(); return; }
            Stop();
        }

        public void Initialize()
        {
            foreach (Transform child in libraryParent) Destroy(child.gameObject);
            foreach (Transform child in playlistParent) Destroy(child.gameObject);
            foreach (Transform child in playlistPanelParent) Destroy(child.gameObject);

            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (sortListByName) libraryPlaylist.playlist.Sort(ByTitle);

            InstantiatePlaylist(libraryPlaylist, true, libraryParent);
            for (int i = 0; i < customPlaylists.Count; i++) InstantiatePlaylist(customPlaylists[i]);
        }

        string CountLabel(MusicPlaylist list) =>
            list.playlist.Count + " " + (list.playlist.Count == 1 ? playlistSingularLabel : playlistPluralLabel);

        /// <summary>Adds a sidebar button for the playlist and (by default) its page of tracks.</summary>
        public void InstantiatePlaylist(MusicPlaylist targetPlaylist, bool createContent = true, Transform altTrackParent = null)
        {
            if (targetPlaylist.playlist.Count == 0) return;

            var entry = AppSpawn.Row(playlistItemPreset, playlistParent, targetPlaylist.playlistName).GetComponent<MusicPlaylistEntry>();
            entry.playlistID = targetPlaylist.playlistName;
            entry.coverImage.sprite = targetPlaylist.coverImage;
            entry.titleText.text = targetPlaylist.playlistName;
            entry.countText.text = CountLabel(targetPlaylist);
            entry.playlistButton.onClick.AddListener(() => musicPanelManager.OpenPanel("Playlist_" + entry.playlistID));

            if (createContent) InstantiatePlaylistContent(targetPlaylist, altTrackParent);
        }

        /// <summary>
        /// Builds the playlist's page and registers it with the window's panels as "Playlist_&lt;name&gt;". With
        /// <paramref name="altTrackParent"/> every row is also copied there (the library page), as a twin of the page row.
        /// </summary>
        public void InstantiatePlaylistContent(MusicPlaylist targetPlaylist, Transform altTrackParent = null)
        {
            if (sortListByName) targetPlaylist.playlist.Sort(ByTitle);

            var pageGo = AppSpawn.Row(playlistPanelPreset, playlistPanelParent, targetPlaylist.playlistName);
            musicPanelManager.panels.Add(new WindowPanelManager.PanelItem
            {
                panelName = "Playlist_" + targetPlaylist.playlistName,
                panelObject = pageGo.GetComponent<Animator>()
            });

            var page = pageGo.GetComponent<MusicPlaylistPage>();
            page.panelID = targetPlaylist.playlistName;
            if (page.bannerImage != null) page.bannerImage.sprite = targetPlaylist.coverImage;
            page.coverImage.sprite = targetPlaylist.coverImage;
            page.titleText.text = targetPlaylist.playlistName;
            page.countText.text = CountLabel(targetPlaylist);
            foreach (Transform child in page.contentParent) Destroy(child.gameObject);

            MusicTrackRow first = null;
            bool shaded = false;
            for (int i = 0; i < targetPlaylist.playlist.Count; i++)
            {
                var music = targetPlaylist.playlist[i];
                if (music.excludeFromLibrary || music.musicClip == null) continue;

                var go = AppSpawn.Row(playlistTrackPreset, page.contentParent, music.musicTitle);
                var row = go.GetComponent<MusicTrackRow>();
                row.itemIndex = i;
                row.manager = this;
                row.playlist = targetPlaylist;
                row.cover = music.musicCover;
                row.title = music.musicTitle;
                row.artist = music.artistTitle;
                row.album = music.albumTitle;
                row.accentColor = DreamOSInternalTools.GetSpriteAccentColor(row.cover);
                row.accentMatchColor = DreamOSInternalTools.GetAccentMatchColor(row.accentColor);
                page.tracks.Add(row);
                if (first == null) first = row;

                // Every other row gets a faint stripe.
                if (row.backgroundImage != null)
                {
                    shaded = !shaded;
                    row.backgroundImage.color = new Color32(255, 255, 255, (byte)(shaded ? 4 : 0));
                }

                row.coverImage.sprite = music.musicCover;
                row.titleText.text = music.musicTitle;
                row.artistText.text = music.artistTitle;
                int length = (int)music.musicClip.length;
                row.durationText.text = (length / 60 % 60) + ":" + (length % 60).ToString("D2");
                row.button.onClick.AddListener(() => PlayFromPage(targetPlaylist, page, row));

                if (altTrackParent != null)
                {
                    var twin = AppSpawn.Row(go, altTrackParent, music.musicTitle).GetComponent<MusicTrackRow>();
                    row.twinTrack = twin;
                    twin.button.onClick.AddListener(() => PlayFromPage(targetPlaylist, page, twin));
                }
            }

            page.playAllButton.onClick.AddListener(() =>
            {
                QueuePage(page);
                Play(first);
            });

            // Load the first track without playing it, so the player shows something before the first click.
            if (currentTrack == null && first != null)
            {
                first.button.onClick.Invoke();
                Stop();
            }
        }

        void QueuePage(MusicPlaylistPage page)
        {
            playerQueue.Clear();
            playerQueue.AddRange(page.tracks);
        }

        void PlayFromPage(MusicPlaylist list, MusicPlaylistPage page, MusicTrackRow row)
        {
            if (currentPlaylist != list || playerQueue.Count == 0) QueuePage(page);
            currentPlaylist = list;
            Play(row);
        }

        /// <summary>Resumes (or starts) the current track.</summary>
        public void Play()
        {
            if (currentTrack != null)
            {
                audioSource.clip = currentTrack.playlist.playlist[currentTrack.itemIndex].musicClip;
                currentTrack.SetNowPlayingState(true);
                if (nowPlayingListTitle != null) nowPlayingListTitle.text = currentTrack.playlist.playlistName;
            }
            PlaybackRequested = true;
            audioSource.Play();
            UpdateDataItems();
            if (gameObject.activeInHierarchy) RestartPlayback();
        }

        public void Play(MusicTrackRow track)
        {
            if (currentTrack != null) currentTrack.SetNowPlayingState(false);
            audioSource.Stop();
            currentTrack = track;
            currentTrackIndex = track.itemIndex;
            audioSource.clip = track.playlist.playlist[currentTrackIndex].musicClip;
            audioSource.time = 0;
            duration = GetDuration();
            PlaybackRequested = true;
            audioSource.Play();
            UpdateDataItems();
            currentTrack.SetNowPlayingState(true);
            if (nowPlayingListTitle != null) nowPlayingListTitle.text = currentTrack.playlist.playlistName;
            if (gameObject.activeInHierarchy) RestartPlayback();
        }

        public void PlayCustomClip(AudioClip clip, Sprite cover, string clipName, string clipAuthor)
        {
            if (currentTrack != null) currentTrack.SetNowPlayingState(false);
            customClipName = clipName;
            customClipArtist = clipAuthor;
            customClipCover = cover;
            currentTrack = null;
            currentPlaylist = null;

            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.time = 0;
            duration = GetDuration();
            PlaybackRequested = true;
            audioSource.Play();
            UpdateDataItems();
            if (nowPlayingListTitle != null) nowPlayingListTitle.text = "Custom";
            if (gameObject.activeInHierarchy) RestartPlayback();
        }

        public void Pause()
        {
            PlaybackRequested = false;
            audioSource.Pause();
            if (currentTrack != null) currentTrack.SetNowPlayingState(false);
            UpdateDataItems();
        }

        public void Stop()
        {
            PlaybackRequested = false;
            StopPlayback();
            if (audioSource != null) audioSource.Stop();
            if (currentTrack != null) currentTrack.SetNowPlayingState(false);
            UpdateDataItems();
        }

        public void Mute() => audioSource.mute = !audioSource.mute;

        public void NextTrack()
        {
            if (currentTrack != null) currentTrack.SetNowPlayingState(false);
            if (currentPlaylist == null) { Stop(); return; }
            audioSource.Stop();

            var list = currentPlaylist.playlist;
            if (list.Count == 1)
            {
                // A one-track playlist just starts over.
                audioSource.time = 0;
                audioSource.Play();
            }
            else if (shuffle && !repeat)
            {
                currentTrack = playerQueue[DreamOSInternalTools.GetRandomUniqueValue(currentTrackIndex, 0, list.Count)];
                currentTrackIndex = currentTrack.itemIndex;
                if (list[currentTrackIndex].excludeFromLibrary) { NextTrack(); return; }
                audioSource.clip = list[currentTrackIndex].musicClip;
            }
            else
            {
                currentTrackIndex++;
                if (currentTrackIndex > list.Count - 1) currentTrackIndex = 0;
                if (list[currentTrackIndex].excludeFromLibrary) NextTrack();
                currentTrack = playerQueue[currentTrackIndex];
                audioSource.clip = list[currentTrackIndex].musicClip;
            }

            StartFromTop();
            AnnounceTrack();
        }

        public void PrevTrack()
        {
            if (currentTrack != null) currentTrack.SetNowPlayingState(false);
            if (currentPlaylist == null) { Stop(); return; }
            audioSource.Stop();

            var list = currentPlaylist.playlist;
            if (shuffle && !repeat)
            {
                currentTrack = playerQueue[DreamOSInternalTools.GetRandomUniqueValue(currentTrackIndex, 0, list.Count)];
                currentTrackIndex = currentTrack.itemIndex;
            }
            else
            {
                currentTrackIndex--;
                if (currentTrackIndex < 0) currentTrackIndex = list.Count - 1;
                currentTrack = playerQueue[currentTrackIndex];
            }
            audioSource.clip = list[currentTrackIndex].musicClip;

            StartFromTop();
            AnnounceTrack();
        }

        // Plays the clip now in the audio source from 0:00 and refreshes the parts.
        void StartFromTop()
        {
            duration = GetDuration();
            audioSource.time = 0;
            PlaybackRequested = true;
            audioSource.Play();
            UpdateDataItems();
            if (currentTrack != null) currentTrack.SetNowPlayingState(true);
        }

        void AnnounceTrack()
        {
            if (!enablePopupNotification || NotificationManager.instance == null) return;
            var music = currentPlaylist.playlist[currentTrackIndex];
            NotificationManager.instance.CreatePopupNotification(notificationIcon, music.musicTitle, music.artistTitle);
        }

        public void UpdateDataItems()
        {
            for (int i = 0; i < dataToBeUpdated.Count; i++)
            {
                var part = dataToBeUpdated[i];
                if (part == null) { dataToBeUpdated.RemoveAt(i); continue; }
                if (!part.alwaysUpdate && part.gameObject.activeInHierarchy) part.UpdateItem();
            }
        }

        public void SetPopupNotification(bool value) => enablePopupNotification = value;

        public int GetDuration() => (int)audioSource.clip.length;

        public string GetNormalizedDuration() => (GetDuration() / 60 % 60) + ":" + (duration % 60).ToString("D2");

        /// <summary>Whole seconds into the current minute; also updates <see cref="secondsRaw"/> and <see cref="minutes"/>.</summary>
        public int GetPlayTime()
        {
            secondsRaw = playTime % 60;
            minutes = (int)playTime / 60 % 60;
            return (int)playTime % 60;
        }

        public string GetNormalizedPlayTime() => minutes + ":" + seconds.ToString("00");

        public Sprite GetCoverArt() => currentTrack != null ? currentTrack.cover : customClipCover;

        public string GetAlbumName() => currentTrack != null ? currentTrack.album : "Custom";

        public string GetTrackName() => currentTrack != null ? currentTrack.title : customClipName;

        public string GetArtistName() => currentTrack != null ? currentTrack.artist : customClipArtist;

        // Without a track the colour is (25, 35, 45) in 0-1 units, i.e. over-bright white: kept as DreamOS had it.
        Color Accent(Color track, float alpha) =>
            currentTrack != null ? new Color(track.r, track.g, track.b, alpha) : new Color(25, 35, 45, alpha);

        public Color GetAccentColor(Image targetImage) => Accent(currentTrack != null ? currentTrack.accentColor : default, targetImage.color.a);

        public Color GetAccentMatchColor(Image targetImage) => Accent(currentTrack != null ? currentTrack.accentMatchColor : default, targetImage.color.a);

        public Color GetAccentColor(TextMeshProUGUI targetText) => Accent(currentTrack != null ? currentTrack.accentColor : default, targetText.color.a);

        public Color GetAccentMatchColor(TextMeshProUGUI targetText) => Accent(currentTrack != null ? currentTrack.accentMatchColor : default, targetText.color.a);

        static int ByTitle(MusicPlaylist.MusicItem a, MusicPlaylist.MusicItem b) => a.musicTitle.CompareTo(b.musicTitle);

        void RestartPlayback()
        {
            StopPlayback();
            playback = StartCoroutine(ProcessPlayback());
        }

        void StopPlayback()
        {
            if (playback != null) StopCoroutine(playback);
            playback = null;
        }

        // Tracks the play position while the source plays, and repeats or moves on at the end of the track.
        IEnumerator ProcessPlayback()
        {
            while (audioSource.isPlaying)
            {
                playTime = audioSource.time;
                seconds = GetPlayTime();
                if (!externalPlaylistAdvance && playTime >= duration)
                {
                    if (repeat && !shuffle) { audioSource.Stop(); audioSource.time = 0; audioSource.Play(); }
                    else NextTrack();
                }
                yield return null;
            }
            playback = null;
        }
    }
}
