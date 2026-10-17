using System;

namespace LingGuangV05.Core
{
    /// <summary>
    /// The background music's level on top of the player's own volume: a fade (in when the music starts, out at 关机) and a
    /// duck (down under spoken lines and cutscenes, back up slowly after). Step it once a frame; multiply the player's
    /// volume by what it returns.
    /// </summary>
    public sealed class MusicMix
    {
        public const float FadeInSeconds = 3f, FadeOutSeconds = 1.2f, DuckLevel = .3f, DuckDownSeconds = .35f, DuckUpSeconds = 1.2f;

        float fadeTarget;
        public float Fade { get; private set; }
        public float Duck { get; private set; } = 1;
        public bool FadingOut => fadeTarget <= 0 && Fade > 0;

        public void FadeIn() => fadeTarget = 1;
        public void FadeOut() => fadeTarget = 0;
        /// <summary>Silent at once (the music was faded out elsewhere); a FadeIn starts it again from nothing.</summary>
        public void Cut() { fadeTarget = 0; Fade = 0; }
        /// <summary>Straight to full, no fade (the player started a track themselves).</summary>
        public void Full() { fadeTarget = 1; Fade = 1; }

        /// <summary>Advances by <paramref name="dt"/> seconds; returns the multiplier for the player's volume (0–1).</summary>
        public float Step(float dt, bool ducking)
        {
            dt = Math.Max(0, dt);
            float fadeRate = fadeTarget > Fade ? 1 / FadeInSeconds : 1 / FadeOutSeconds;
            Fade = Toward(Fade, fadeTarget, fadeRate * dt);
            float duckRate = (1 - DuckLevel) / (ducking ? DuckDownSeconds : DuckUpSeconds);
            Duck = Toward(Duck, ducking ? DuckLevel : 1, duckRate * dt);
            // Squared: a linear fade sounds as if it jumps in at the start.
            return Fade * Fade * Duck;
        }

        static float Toward(float value, float target, float step)
            => value < target ? Math.Min(target, value + step) : Math.Max(target, value - step);
    }
}
