using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LingGuangV05.Desktop.OS
{
    /// <summary>
    /// Desktop action sounds, ordered by priority: when one click both switches a tab and opens a window, only the
    /// highest one plays. Window, panel, switch, menu and selector code call <see cref="DesktopSfx.Play"/>.
    /// </summary>
    public enum DesktopSound
    {
        Tap,
        Click,
        Select,
        Tab,
        ToggleOn,
        ToggleOff,
        MenuOpen,
        WindowMinimize,
        WindowMaximize,
        WindowClose,
        WindowOpen,
    }

    /// <summary>
    /// One sound per desktop click (2026-10-19). A mouse press only holds its click sound; if the press or its release
    /// triggers an action (window open/close/minimize/maximize, tab, switch, menu, selector), the action's own sound
    /// plays instead, otherwise the held click plays on release. Played by a small hidden driver in LateUpdate through
    /// <see cref="AudioManager"/>'s source, so it works while the AudioManager component itself is disabled (the
    /// monitor scenes forward clicks through AeroLcd/RetroCrt bridges). Clips live in Resources/DesktopSfx and are
    /// made by Tools/OSArt/Audio/make_audio.py.
    /// </summary>
    public static class DesktopSfx
    {
        // Windows restored from the save and panels opened while the desktop boots stay silent
        const float QuietAfterBoot = 1.5f;
        const float SameSoundGap = 0.04f;
        // A press held longer than this (dragging a window) sounds its click without waiting for the release
        const float HoldLimit = 0.25f;

        static readonly Dictionary<DesktopSound, AudioClip> Clips = new Dictionary<DesktopSound, AudioClip>();
        static readonly Dictionary<DesktopSound, float> LastPlayed = new Dictionary<DesktopSound, float>();

        static int pendingFrame = -1;
        static DesktopSound pending;
        static AudioClip heldClick;
        static float heldVolume;
        static float heldSince = -1f;
        static Driver driver;

        /// <summary>Requests an action sound; a higher-priority request before it is played wins.</summary>
        public static void Play(DesktopSound sound)
        {
            if (!Application.isPlaying || Time.unscaledTime < QuietAfterBoot) { return; }

            int frame = Time.frameCount;
            if (pendingFrame < frame - 1 || sound > pending) { pending = sound; }
            pendingFrame = frame;
            EnsureDriver();
        }

        /// <summary>Holds a mouse-press click until the release, unless an action sound replaces it.</summary>
        public static void HoldClick(AudioClip clip, float volume)
        {
            if (!Application.isPlaying || clip == null) { return; }

            heldClick = clip;
            heldVolume = volume;
            heldSince = Time.unscaledTime;
            EnsureDriver();
        }

        /// <summary>
        /// Takes the pending request, if any. A request made after the driver's LateUpdate (editor calls, late
        /// scripts) is kept for one more frame; older ones are dropped.
        /// </summary>
        public static bool TryTake(out DesktopSound sound)
        {
            sound = pending;
            if (pendingFrame < 0) { return false; }
            if (pendingFrame < Time.frameCount - 1) { pendingFrame = -1; return false; }

            pendingFrame = -1;
            return true;
        }

        /// <summary>Plays a sound now through the shared UI source, skipping repeats closer than 40 ms.</summary>
        public static void PlayNow(AudioSource source, DesktopSound sound, float volume = 1f)
        {
            if (source == null) { return; }

            float now = Time.unscaledTime;
            if (LastPlayed.TryGetValue(sound, out float last) && now - last < SameSoundGap) { return; }

            AudioClip clip = GetClip(sound);
            if (clip == null) { return; }

            LastPlayed[sound] = now;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public static AudioClip GetClip(DesktopSound sound)
        {
            if (!Clips.TryGetValue(sound, out AudioClip clip))
            {
                clip = Resources.Load<AudioClip>("DesktopSfx/" + sound);
                Clips[sound] = clip;
            }

            return clip;
        }

        static void EnsureDriver()
        {
            if (driver != null) { return; }

            var host = new GameObject("Desktop Sfx") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(host);
            driver = host.AddComponent<Driver>();
        }

        static void Flush()
        {
            AudioManager audio = AudioManager.instance;
            AudioSource source = audio != null ? audio.audioSource : null;
            float volume = audio != null ? audio.mouseStrokeVolume : 1f;

            if (TryTake(out DesktopSound action))
            {
                heldSince = -1f;
                PlayNow(source, action, volume);
                return;
            }

            if (heldSince < 0f) { return; }

            var mouse = Mouse.current;
            bool stillDown = mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed);
            if (stillDown && Time.unscaledTime - heldSince < HoldLimit) { return; }

            heldSince = -1f;
            if (source != null && heldClick != null) { source.PlayOneShot(heldClick, Mathf.Clamp01(heldVolume)); }
        }

        [DefaultExecutionOrder(1000)]
        sealed class Driver : MonoBehaviour
        {
            void LateUpdate() { Flush(); }
        }
    }
}
