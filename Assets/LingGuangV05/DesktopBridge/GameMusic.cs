using System.Collections;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.Apps;
using LingGuangV05.Desktop.OS;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop
{
    /// <summary>
    /// Desktop background music with smooth cutscene ducking and shutdown fades. Since 2026-10-22 (玩家反馈 F2) it starts on
    /// its own as soon as the desktop is on screen (after the prologue's title card on a new game), not when the player
    /// opens the music app; opening the app later only shows what is playing.
    /// </summary>
    public sealed class GameMusic : MonoBehaviour
    {
        public const float DefaultVolume = .5f;
        // Written once, so an older save's demo settings (shuffle on) give way to the defaults.
        const string DefaultsKey = "LingGuang.Music.Defaults.v2";

        static GameMusic instance;
        MusicApp manager;
        AudioSource source;
        WindowManager musicWindow;
        StoryDesktopPresenter presenter;
        readonly MusicMix mix = new MusicMix();
        bool ready, started, wasPlaying, exited;
        float userVolume = DefaultVolume, lastSet = -1;
        readonly LingGuangV05.XingGuang.XgDailyPlayback dailyPlayback = new LingGuangV05.XingGuang.XgDailyPlayback();
        LingGuangV05.XingGuang.XgSim dailyMusicLab;
        AudioClip dailyClip;

        public static GameMusic Install(GameObject host)
        {
            var music = host.GetComponent<GameMusic>();
            return music != null ? music : host.AddComponent<GameMusic>();
        }

        /// <summary>
        /// The game is leaving for the title screen: the music fades out with the screen. The desktop (and this) is switched
        /// off in the same moment, so the fade runs on its own object outside it; entering again fades the music back in.
        /// </summary>
        public static void FadeOutForExit()
        {
            if (instance == null || instance.source == null) return;
            var music = instance;
            music.mix.Cut();
            music.exited = true;
            if (!music.source.isPlaying) return;
            var host = new GameObject("Music Fade Out");
            music.source.transform.SetParent(host.transform, false);
            host.AddComponent<FadeOut>().Run(music.source, music.transform);
        }

        /// <summary>This source is the music fading out for the title screen (leave it to finish).</summary>
        public static bool FadingOut(AudioSource source)
            => source != null && source.transform.parent != null && source.transform.parent.GetComponent<FadeOut>() != null;

        /// <summary>Fades a source out over <see cref="MusicMix.FadeOutSeconds"/>, stops it and hands it back.</summary>
        sealed class FadeOut : MonoBehaviour
        {
            public void Run(AudioSource source, Transform home) => StartCoroutine(Fade(source, home));

            IEnumerator Fade(AudioSource source, Transform home)
            {
                float from = source.volume;
                for (float t = 0; t < MusicMix.FadeOutSeconds && source != null; t += Time.unscaledDeltaTime)
                {
                    float k = 1 - t / MusicMix.FadeOutSeconds;
                    source.volume = from * k * k;
                    yield return null;
                }
                if (source != null)
                {
                    source.Stop();
                    source.volume = 0;
                    if (home != null) source.transform.SetParent(home, false);
                }
                Destroy(gameObject);
            }
        }

        /// <summary>The background music has started this boot (on its own when the desktop showed, or from the app).</summary>
        public static bool Started => instance != null && instance.started;

        void OnEnable()
        {
            instance = this;
            // A new boot waits for the player to open the app again.
            if (!exited || source == null) return;
            exited = false;
            wasPlaying = false;
            if (source.transform.parent != transform) source.transform.SetParent(transform, false);
            source.volume = lastSet = 0;
            started = false;
            source.Stop();
            mix.Cut();
        }
        void OnDisable() { dailyPlayback.Reset(); dailyMusicLab = null; dailyClip = null; if (instance == this) instance = null; }

        IEnumerator Start()
        {
            manager = System.Array.Find(FindObjectsByType<MusicApp>(FindObjectsInactive.Include, FindObjectsSortMode.None), m => m != null);
            if (manager == null || manager.audioSource == null) { Destroy(this); yield break; }
            musicWindow = manager.GetComponentInParent<WindowManager>(true);
            source = manager.audioSource;
            source.playOnAwake = false;
            source.Stop();
            manager.keepPlayingWhenClosed = true;
            manager.externalPlaylistAdvance = true;
            // The window, and the desktop's app layer around it, are switched off at times; the music lives here instead.
            source.transform.SetParent(transform, false);

            if (PlayerPrefs.GetInt(DefaultsKey, 0) == 0)
            {
                DreamOSDataManager.WriteBooleanData(DreamOSDataManager.DataCategory.Apps, "MusicPlayer_Repeat", false);
                DreamOSDataManager.WriteBooleanData(DreamOSDataManager.DataCategory.Apps, "MusicPlayer_Shuffle", false);
                PlayerPrefs.SetInt(DefaultsKey, 1);
                PlayerPrefs.Save();
                manager.repeat = false; manager.shuffle = false;
            }
            source.volume = 0;
            // The app's volume slider sets the source from its own value when it first initialises.
            foreach (var item in FindObjectsByType<MusicAppPart>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!item.IsVolumeSlider) continue;
                var slider = item.GetComponent<Slider>();
                if (slider != null) slider.SetValueWithoutNotify(DefaultVolume);
            }

            // Ignore scene setup. Only a subsequent open starts the song.
            yield return null;
            yield return null;
            ready = true;
            if (musicWindow != null) musicWindow.onOpen.AddListener(OnMusicOpened);
        }

        void Update()
        {
            if (!ready || manager == null || source == null) return;
            if (exited) return; // the exit fade has the source
            if (!started) { if (Time.unscaledTime >= nextAutoTry && DesktopShown()) { nextAutoTry = Time.unscaledTime + .5f; StartFirstSong(); } return; }
            Mix();
            // Repeat-one is the source's own loop, so it holds while the window (and its playback loop) is closed.
            source.loop = manager.repeat && !manager.shuffle;
            bool playing = source.isPlaying;
            var currentLab = LingGuangV05.Desktop.Tieba.TiebaHub.Lab();
            if (!ReferenceEquals(currentLab, dailyMusicLab)) { dailyPlayback.Reset(); dailyMusicLab = currentLab; dailyClip = null; }
            // A loop's wrap is also a natural completion, before observing the new start.
            if (playing && source.loop && dailyClip == source.clip && source.time < .15f && dailyPlayback.Complete(Time.unscaledDeltaTime))
                dailyMusicLab?.RecordDailySongCompleted(dailyPlayback.Song, dailyPlayback.Ticket);
            if (dailyClip != source.clip) { dailyPlayback.Reset(); dailyClip = source.clip; }
            if (source.clip != null) dailyPlayback.Observe(source.clip.name, source.time, source.clip.length, Time.unscaledDeltaTime, playing);
            // One resident owner advances both open and closed windows. A natural end can
            // report isPlaying=false before the streaming time resets to zero, so do not
            // gate the falling edge on source.time. Explicit Pause/Stop clears the request.
            if (MusicApp.ShouldAdvanceAfterEnd(wasPlaying, playing, manager.PlaybackRequested, AudioListener.pause,
                source.enabled && source.gameObject.activeInHierarchy))
            {
                if (dailyClip == source.clip && dailyPlayback.Complete(Time.unscaledDeltaTime))
                    dailyMusicLab?.RecordDailySongCompleted(dailyPlayback.Song, dailyPlayback.Ticket);
                dailyPlayback.Reset(); dailyClip = null;
                wasPlaying=false; // consume this completion before starting the next clip
                manager.NextTrack();playing=source.isPlaying;
            }
            wasPlaying = playing;
        }

        void OnMusicOpened() => StartFirstSong();

        PrologueDirector prologue;
        ChapterOneRuntime runtime;
        float lookAgain, nextAutoTry;

        /// <summary>
        /// The desktop is on screen: a loaded or finished game at once, a new game once the prologue's title card has gone
        /// (the boot screen and the desktop are what it plays over). Hosts paused behind the title room never get here.
        /// </summary>
        bool DesktopShown()
        {
            if ((prologue == null || runtime == null) && Time.unscaledTime >= lookAgain)
            {
                lookAgain = Time.unscaledTime + 1;
                if (prologue == null) prologue = FindAnyObjectByType<PrologueDirector>();
                if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
            }
            if (prologue != null && prologue.Running) return prologue.Step.Length > 0 && prologue.Step != "title";
            return runtime == null || runtime.Sim == null || runtime.TestMode || !runtime.Sim.InPrologue;
        }

        void StartFirstSong()
        {
            if (!ready || started || exited || manager == null || source == null) return;
            // Each boot starts in playlist order. A manual change remains in effect while this
            // playback session continues, including closing and reopening the player.
            manager.repeat = false; manager.shuffle = false; source.loop = false;
            DreamOSDataManager.WriteBooleanData(DreamOSDataManager.DataCategory.Apps, "MusicPlayer_Repeat", false);
            DreamOSDataManager.WriteBooleanData(DreamOSDataManager.DataCategory.Apps, "MusicPlayer_Shuffle", false);
            manager.UpdateDataItems();
            foreach (var track in manager.playerQueue)
            {
                if (track == null || track.playlist == null || track.itemIndex < 0 || track.itemIndex >= track.playlist.playlist.Count) continue;
                var clip = track.playlist.playlist[track.itemIndex].musicClip;
                // Preserve the original desktop song after alphabetical title sorting.
                if (clip == null || clip.name != "emergency_1") continue;
                manager.Play(track);
                if (!source.isPlaying) return;
                started = wasPlaying = true;
                mix.FadeIn();
                // The new-game prologue already owns the music thought; opening the app later never restarts the song.
                return;
            }
        }

        void OnDestroy()
        {
            if (musicWindow != null) musicWindow.onOpen.RemoveListener(OnMusicOpened);
            if (manager != null) manager.externalPlaylistAdvance=false;
            if (instance == this) instance = null;
        }

        /// <summary>Read only the already-playing music source into a caller-owned power-of-two buffer.
        /// No song seeking, loading, playlist changes, generated BPM or scene searches.</summary>
        public static bool TryReadMusicOutput(float[] buffer, out AudioClip clip, out float position)
        {
            clip = null; position = 0;
            var music = instance;
            if (music == null || !music.isActiveAndEnabled || !music.ready || !music.started || music.exited || buffer == null || buffer.Length < 64 || (buffer.Length & (buffer.Length - 1)) != 0) return false;
            var audio = music.source;
            if (audio == null || !audio.enabled || !audio.gameObject.activeInHierarchy || !audio.isPlaying || audio.clip == null || audio.mute || audio.volume <= .001f || AudioListener.pause || AudioListener.volume <= .001f) return false;
            try { audio.GetOutputData(buffer, 0); clip = audio.clip; position = audio.time; return true; }
            catch (System.InvalidOperationException) { return false; }
            catch (UnityException) { return false; }
        }

        /// <summary>The player's volume (the app's slider writes the source directly) times the fade and the duck.</summary>
        void Mix()
        {
            if (lastSet >= 0 && Mathf.Abs(source.volume - lastSet) > .002f) userVolume = source.volume;
            float level = mix.Step(Time.unscaledDeltaTime, VoiceLines.Speaking || Busy());
            source.volume = lastSet = userVolume * level;
        }

        /// <summary>A cutscene is on screen: the music steps back.</summary>
        bool Busy()
        {
            if (presenter == null) presenter = FindAnyObjectByType<StoryDesktopPresenter>();
            // The prologue's 360 alert ducks it as well (v4 §7.1): the first scare is not drowned by the song.
            return presenter != null && presenter.CutscenePlaying || prologue != null && prologue.Running && prologue.Step == "virus" || AiJoinsYy.Playing || AutoLabelEpiphany.Playing || BankruptcyEnding.Playing
                || LingGuangV05.Desktop.XingGuang.XgEmergenceCutscene.Playing || LingGuangV05.Desktop.XingGuang.LoveQuestionCutscene.Playing;
        }
    }
}
