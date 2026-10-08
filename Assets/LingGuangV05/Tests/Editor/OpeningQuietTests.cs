using System;
using LingGuangV05.Core;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>The opening quiet window (OpeningQuiet): who may pop up before the second ability, and the first minute's order.</summary>
    public sealed class OpeningQuietTests
    {
        [TearDown] public void ClearProbe() { OpeningQuiet.Probe = null; }

        [Test]
        public void TheWindowRunsFromBootUntilTheSecondAbility()
        {
            Assert.IsTrue(OpeningQuiet.Rule(false, true, 1), "the prologue is quiet");
            Assert.IsTrue(OpeningQuiet.Rule(false, false, 1), "after the setup, until the second ability");
            Assert.IsFalse(OpeningQuiet.Rule(false, false, 2), "the second ability ends it");
            Assert.IsFalse(OpeningQuiet.Rule(false, false, 5));
            Assert.IsFalse(OpeningQuiet.Rule(true, true, 1), "never in test mode");
        }

        [Test]
        public void StoryMomentsAlwaysPassAndAmbientChatterOnlyOutsideTheWindow()
        {
            Assert.IsTrue(OpeningQuiet.Allows(true, true), "a story moment passes the window");
            Assert.IsFalse(OpeningQuiet.Allows(false, true), "an ad is dropped inside it");
            Assert.IsTrue(OpeningQuiet.Allows(false, false), "and shown after it");
        }

        [Test]
        public void TheGateReadsTheDesktopProbeAndIsOpenWithoutOne()
        {
            OpeningQuiet.Probe = null;
            Assert.IsFalse(OpeningQuiet.Active, "outside the game (tools, tests) nothing is held");
            Assert.IsTrue(OpeningQuiet.Allows(false));
            bool quiet = true;
            OpeningQuiet.Probe = () => quiet;
            Assert.IsTrue(OpeningQuiet.Active);
            Assert.IsFalse(OpeningQuiet.Allows(false));
            Assert.IsTrue(OpeningQuiet.Allows(true));
            quiet = false;
            Assert.IsTrue(OpeningQuiet.Allows(false), "the same gate opens with the save");
            OpeningQuiet.Probe = () => throw new InvalidOperationException("desktop torn down");
            Assert.IsFalse(OpeningQuiet.Active, "a broken probe never silences the game");
        }

        [Test]
        public void TheFirstMinuteRunsOneBeatAtATime()
        {
            Assert.IsTrue(OpeningQuiet.Turn(OpeningBeat.MonthCard, false, false, false), "the month card goes first");
            Assert.IsFalse(OpeningQuiet.Turn(OpeningBeat.AiJoins, false, false, false), "the AI waits for the month card");
            Assert.IsTrue(OpeningQuiet.Turn(OpeningBeat.AiJoins, true, false, false));
            Assert.IsFalse(OpeningQuiet.Turn(OpeningBeat.Qingwen, true, false, false), "晴雯 waits for the AI to join YY");
            Assert.IsTrue(OpeningQuiet.Turn(OpeningBeat.Qingwen, true, true, false));
            Assert.IsFalse(OpeningQuiet.Turn(OpeningBeat.LaoZhou, true, true, false), "老周 comes last");
            Assert.IsTrue(OpeningQuiet.Turn(OpeningBeat.LaoZhou, true, true, true));
        }

        [Test]
        public void ATurnWaitsItsGapAfterTheBeatBeforeAndStartsOverOnABreak()
        {
            var turn = new OpeningTurn(6);
            Assert.IsFalse(turn.Ready(false, 0));
            Assert.IsFalse(turn.Ready(true, 10), "the beat before just finished");
            Assert.IsFalse(turn.Ready(true, 15.9));
            Assert.IsTrue(turn.Ready(true, 16), "six quiet seconds later");
            Assert.IsFalse(turn.Ready(false, 17), "a new cutscene holds it again");
            Assert.IsFalse(turn.Ready(true, 18));
            Assert.IsTrue(turn.Ready(true, 24));
            Assert.IsFalse(turn.Ready(true, double.NaN), "a broken clock never lets it through");
        }
    }
}
