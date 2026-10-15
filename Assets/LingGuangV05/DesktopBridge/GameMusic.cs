using System.Collections;
using Michsky.DreamOS;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop
{
    /// <summary>
    /// Background music. The music app's library is the 灵光 playlist (Assets/LingGuangV05/Music, set on the scene's
    /// MusicPlayerManager); from the desktop's start it plays the first track on repeat at 60 %. The player changes the
    /// track, repeat, shuffle and volume in the music app, and the music keeps going while its window is closed.
    /// </summary>
    public sealed class GameMusic : MonoBehaviour
    {
        public const float DefaultVolume = .6f;
        // Written once, so an older save's demo settings (shuffle on) give way to the defaults.
        const string DefaultsKey = "LingGuang.Music.Defaults.v1";

        MusicPlayerManager manager;
        AudioSource source;
        bool wasPlaying;

        public static GameMusic Install(GameObject host)
        {
            var music = host.GetComponent<GameMusic>();
            return music != null ? music : host.AddComponent<GameMusic>();
        }

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
            source.volume = DefaultVolume;
            // The app's volume slider sets the source from its own value when it first initialises.
            foreach (var item in FindObjectsByType<MusicPlayerDataItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!item.IsVolumeSlider) continue;
                var slider = item.GetComponent<Slider>();
                if (slider != null) slider.SetValueWithoutNotify(DefaultVolume);
            }

            // Let the window close itself first (WindowManager.Start), then start the first track.
            yield return null;
            yield return null;
            if (!source.isPlaying && manager.playerQueue.Count > 0 && manager.playerQueue[0] != null) manager.Play(manager.playerQueue[0]);
            if (!source.isPlaying) Debug.LogWarning("GameMusic: the music player has no track to start.");
        }

        void Update()
        {
            if (manager == null || source == null) return;
            // Repeat-one is the source's own loop, so it holds while the window (and its playback loop) is closed.
            source.loop = manager.repeat && !manager.shuffle;
            bool playing = source.isPlaying;
            // With the window closed nothing moves on to the next track; do it here.
            if (wasPlaying && !playing && source.time <= 0 && !manager.isActiveAndEnabled) { manager.NextTrack(); playing = source.isPlaying; }
            wasPlaying = playing;
        }
    }
}
