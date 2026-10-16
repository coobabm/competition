using System;

namespace LingGuangV05.Core
{
    /// <summary>
    /// A story moment is on screen (剧情修订 2026-10-16 §二): a beat with lines, a cutscene, the origin film, an ability's
    /// cutscene, the AI joining YY, the epiphany, the love question. While it plays, nothing else pops up: life-event cards,
    /// platform notices, tray bubbles, toasts, flash cards and other people's YY popups wait (their messages still land);
    /// they come back <see cref="SettleSeconds"/> after it ends. The story's own popups always pass.
    /// One place decides: <see cref="Active"/> (set by the story presenter). Callers ask <see cref="Allows"/>.
    /// </summary>
    public static class StoryFocus
    {
        /// <summary>Seconds of calm after a story moment before held popups start again.</summary>
        public const double SettleSeconds = 3;

        /// <summary>The desktop's reading (set by the story presenter). Unset outside the game: never focused.</summary>
        public static Func<bool> Probe;

        /// <summary>A story moment is playing, or ended less than <see cref="SettleSeconds"/> ago.</summary>
        public static bool Active
        {
            get
            {
                var probe = Probe;
                if (probe == null) return false;
                try { return probe(); }
                catch (Exception) { return false; }
            }
        }

        /// <summary>A story popup always passes; anything else only when no story moment is on screen.</summary>
        public static bool Allows(bool story) => story || !Active;
    }
}
