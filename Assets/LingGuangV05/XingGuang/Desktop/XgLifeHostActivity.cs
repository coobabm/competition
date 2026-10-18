using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.YY;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Inputs for the event cadence only. Never pauses training, debts or the simulation clock.</summary>
    public static class XgLifeHostActivity
    {
        static readonly HashSet<Object> readingWindows = new HashSet<Object>();
        static readonly HashSet<Object> pauseOwners = new HashSet<Object>();
        static readonly System.Predicate<Object> destroyedOwner = IsDestroyed;

        // C4 integration: call on actual reader visibility changes, and false on disable/close.
        // Do not use Sim.Reading: background AI book progress is not a visible reading window.
        public static void SetReadingWindowVisible(Object owner, bool visible) => Set(readingWindows, owner, visible);
        // A real pause UI/host can own this gate; transient hit-stop and application focus are not pause inputs.
        public static void SetGamePaused(Object owner, bool paused) => Set(pauseOwners, owner, paused);
        static void Set(HashSet<Object> set, Object owner, bool held)
        {
            if (owner == null) return;
            if (held) set.Add(owner); else set.Remove(owner);
        }
        static bool Held(HashSet<Object> set)
        {
            set.RemoveWhere(destroyedOwner);
            return set.Count > 0;
        }
        static bool IsDestroyed(Object value) => value == null;
        public static bool ReadingWindowVisible => Held(readingWindows);
        public static bool GamePaused => Held(pauseOwners);

        // One clock per Controller, so switching saves does not create a shared per-frame work queue.
        public sealed class ActiveClock
        {
            bool wasSuspended;
            public void ObservePause() { wasSuspended = true; }
            public void Apply(XgSim sim, float frameSeconds)
            {
                if (sim == null) return;
                bool suspended = PlayerWindowState.IsMinimized || GamePaused;
                bool restored = wasSuspended && !suspended;
                wasSuspended = suspended;
                // Frame duration alone never proves suspension. Ordinary slow frames still count.
                // The host runs in the background; only an observed native/explicit pause earns a recovery frame.
                sim.LifeEventActive = !suspended && !restored;
                var chat = YYChatHub.Instance;
                sim.LifeEventConflict = StoryFocus.Active || ReadingWindowVisible ||
                    (chat != null && chat.GroupScriptPlaying);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { readingWindows.Clear(); pauseOwners.Clear(); }
    }
}
