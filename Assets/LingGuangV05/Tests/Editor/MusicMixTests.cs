using LingGuangV05.Core;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>The background music fades in, steps back under voices and cutscenes, and fades out at 关机.</summary>
    public sealed class MusicMixTests
    {
        static float Run(MusicMix mix, float seconds, bool ducking)
        {
            float level = 0;
            for (float t = 0; t < seconds; t += .02f) level = mix.Step(.02f, ducking);
            return level;
        }

        [Test]
        public void ItIsSilentUntilStartedThenFadesInOverAFewSeconds()
        {
            var mix = new MusicMix();
            Assert.AreEqual(0, Run(mix, 5, false), "silent before it starts (the prologue)");
            mix.FadeIn();
            float early = Run(mix, .5f, false);
            Assert.Greater(early, 0); Assert.Less(early, .1f, "half a second in it is still very quiet");
            Assert.Less(Run(mix, 1, false), 1, "not full before the fade ends");
            Assert.AreEqual(1, Run(mix, MusicMix.FadeInSeconds, false), 1e-4);
        }

        [Test]
        public void VoicesPullItDownQuicklyAndItComesBackSlowly()
        {
            var mix = new MusicMix(); mix.Full();
            Assert.AreEqual(MusicMix.DuckLevel, Run(mix, MusicMix.DuckDownSeconds + .05f, true), 1e-3, "down in a third of a second");
            Assert.Less(Run(mix, MusicMix.DuckDownSeconds + .05f, false), 1, "but not straight back up");
            Assert.AreEqual(1, Run(mix, MusicMix.DuckUpSeconds, false), 1e-3);
        }

        [Test]
        public void ShutdownFadesItOut()
        {
            var mix = new MusicMix(); mix.Full();
            mix.FadeOut();
            Assert.IsTrue(mix.FadingOut);
            Assert.Greater(Run(mix, .3f, false), 0, "a fade, not a cut");
            Assert.AreEqual(0, Run(mix, MusicMix.FadeOutSeconds, false), 1e-4);
            Assert.IsFalse(mix.FadingOut);
        }
    }
}
