using System.Collections;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop
{
    /// <summary>
    /// Background music. The music app's library is the 灵光 playlist (Assets/LingGuangV05/Music, set on the scene's
    /// MusicPlayerManager); track 1 on repeat at 50 % (2026-10-18, was 60 %). The prologue is silent: when it ends, and nothing else is on screen,
    /// the protagonist thinks of putting a song on, the music app opens and the first track fades in. A save past the
    /// prologue fades it in at once. Spoken lines and cutscenes pull it down (<see cref="MusicMix"/>); 关机 fades it out.
    /// The player changes the track, repeat, shuffle and volume in the music app, and the music keeps going while its
    /// window is closed.
    /// </summary>
    public sealed class GameMusic : MonoBehaviour
    {
        public const float DefaultVolume = .5f;
        /// <summary>Seconds of quiet after the prologue before the protagonist puts the music on, and how long its window stays open.</summary>
        public const float IntroQuiet = 1.5f, IntroWindowSeconds = 3.5f, IntroGiveUp = 60f;
        // Written once, so an older save's demo settings (shuffle on) give way to the defaults.
        const string DefaultsKey = "LingGuang.Music.Defaults.v1";

        static GameMusic instance;
        MusicPlayerManager manager;
        AudioSource source;
        ChapterOneRuntime runtime;
        StoryDesktopPresenter presenter;
        readonly MusicMix mix = new MusicMix();
        bool ready, started, sawPrologue, wasPlaying, exited;
        float userVolume = DefaultVolume, lastSet = -1, quietSince = -1, afterPrologue = -1, closeWindowAt = -1;

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

        /// <summary>The music has started (after the prologue, or at once on a later save).</summary>
        public static bool Started => instance != null && instance.started;

        void OnEnable()
        {
            instance = this;
            // Back from the title screen: the track starts again and fades in.
            if (!exited || source == null) return;
            exited = false;
            wasPlaying = false;
            if (source.transform.parent != transform) source.transform.SetParent(transform, false);
            source.volume = lastSet = 0;
            if (started && !source.isPlaying) source.Play();
            mix.Cut();
            if (started) mix.FadeIn();
        }
        void OnDisable() { if (instance == this) instance = null; }

        IEnumerator Start()
        {
            manager = System.Array.Find(FindObjectsByType<MusicPlayerManager>(FindObjectsInactive.Include, FindObjectsSortMode.None), m => m != null);
            if (manager == null || manager.audioSource == null) { Destroy(this); yield break; }
            source = manager.audioSource;
            manager.keepPlayingWhenClosed = true;
            // The window, and the desktop's app layer around it, are switched off at times; the music lives here instead.
            source.transform.SetParent(transform, false);

            if (PlayerPrefs.GetInt(DefaultsKey, 0) == 0)
            {
                DreamOSDataManager.WriteBooleanData(DreamOSDataManager.DataCategory.Apps, "MusicPlayer_Repeat", true);
                DreamOSDataManager.WriteBooleanData(DreamOSDataManager.DataCategory.Apps, "MusicPlayer_Shuffle", false);
                PlayerPrefs.SetInt(DefaultsKey, 1);
                PlayerPrefs.Save();
                manager.repeat = true; manager.shuffle = false;
            }
            source.volume = 0;
            // The app's volume slider sets the source from its own value when it first initialises.
            foreach (var item in FindObjectsByType<MusicPlayerDataItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!item.IsVolumeSlider) continue;
                var slider = item.GetComponent<Slider>();
                if (slider != null) slider.SetValueWithoutNotify(DefaultVolume);
            }

            // Let the window close itself first (WindowManager.Start); the music waits for the prologue.
            yield return null;
            yield return null;
            ready = true;
        }

        void Update()
        {
            if (!ready || manager == null || source == null) return;
            if (exited) return; // the exit fade has the source
            if (!started) WaitToStart();
            if (closeWindowAt >= 0 && Time.unscaledTime >= closeWindowAt) { closeWindowAt = -1; Window()?.CloseWindow(); }
            Mix();
            // Repeat-one is the source's own loop, so it holds while the window (and its playback loop) is closed.
            source.loop = manager.repeat && !manager.shuffle;
            bool playing = source.isPlaying;
            // With the window closed nothing moves on to the next track; do it here.
            if (wasPlaying && !playing && source.time <= 0 && !manager.isActiveAndEnabled) { manager.NextTrack(); playing = source.isPlaying; }
            wasPlaying = playing;
        }

        void WaitToStart()
        {
            // The player put a track on in the music app themselves (even during the prologue): it plays as they set it.
            if (source.isPlaying) { started = true; mix.Full(); return; }
            if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
            var sim = runtime != null ? runtime.Sim : null;
            if (runtime != null && sim == null) return; // still loading
            if (sim != null && sim.InPrologue) { sawPrologue = true; return; }
            if (!sawPrologue) { Begin(); return; } // a save past the prologue
            // The prologue has just ended: the protagonist puts the music on once the screen is quiet.
            if (afterPrologue < 0) afterPrologue = Time.unscaledTime;
            if (Busy() || InnerVoice.Busy || OpeningSequence.FirstLessonActive) { quietSince = -1; if (Time.unscaledTime - afterPrologue < IntroGiveUp) return; }
            else if (quietSince < 0) { quietSince = Time.unscaledTime; return; }
            else if (Time.unscaledTime - quietSince < IntroQuiet) return;
            bool quiet = Time.unscaledTime - afterPrologue < IntroGiveUp;
            if (quiet)
            {
                InnerVoice.Say("……放首歌压压惊。", "…Some music. To calm down.");
                var window = Window();
                if (window != null && !window.isOn) { window.OpenWindow(); closeWindowAt = Time.unscaledTime + IntroWindowSeconds; }
            }
            Begin();
        }

        void Begin()
        {
            started = true;
            if (!source.isPlaying && manager.playerQueue.Count > 0 && manager.playerQueue[0] != null) manager.Play(manager.playerQueue[0]);
            if (!source.isPlaying) Debug.LogWarning("GameMusic: the music player has no track to start.");
            mix.FadeIn();
        }

        /// <summary>The player's volume (the app's slider writes the source directly) times the fade and the duck.</summary>
        void Mix()
        {
            if (lastSet >= 0 && Mathf.Abs(source.volume - lastSet) > .002f) userVolume = source.volume;
            float level = mix.Step(Time.unscaledDeltaTime, VoiceLines.Speaking || Busy());
            source.volume = lastSet = userVolume * level;
        }

        WindowManager Window() => manager != null ? manager.GetComponentInParent<WindowManager>(true) : null;

        /// <summary>A cutscene is on screen: the music steps back.</summary>
        bool Busy()
        {
            if (presenter == null) presenter = FindAnyObjectByType<StoryDesktopPresenter>();
            return presenter != null && presenter.CutscenePlaying || AiJoinsYy.Playing || AutoLabelEpiphany.Playing || BankruptcyEnding.Playing
                || LingGuangV05.Desktop.XingGuang.XgEmergenceCutscene.Playing || LingGuangV05.Desktop.XingGuang.LoveQuestionCutscene.Playing;
        }
    }
}
