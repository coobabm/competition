using LingGuangV05.Core;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// What the desktop knows about the first minute after the setup (OpeningBeat): whether the month card has played,
    /// the AI has joined YY and 晴雯 has said her first line. Each beat's owner asks <see cref="OpeningQuiet.Turn"/>
    /// with these, so only one thing happens at a time: month card, the AI joining YY, 晴雯, then 老周.
    /// Reads only; it changes nothing.
    /// </summary>
    public static class OpeningSequence
    {
        /// <summary>The month card of the lab's stage has played (or there is no story running to play it).</summary>
        public static bool MonthCardDone(StoryDesktopPresenter presenter)
        {
            if (presenter == null || presenter.director == null || !presenter.director.Active) return true;
            if (presenter.CutscenePlaying) return false;
            var runtime = presenter.director.runtime;
            var lab = LingGuangV05.Desktop.Tieba.TiebaHub.Lab();
            if (runtime == null || runtime.Sim == null || lab == null) return true;
            return runtime.Sim.S.story.HasFired("month_" + lab.S.stage);
        }

        /// <summary>The AI is a YY contact and its join scene is over.</summary>
        public static bool AiJoined()
        {
            var hub = YYChatHub.Instance;
            return hub != null && hub.S != null && hub.S.lingguangUnlocked && !AiJoinsYy.Playing;
        }

        /// <summary>晴雯 has said something and is not typing.</summary>
        public static bool QingwenSpoke()
        {
            var hub = YYChatHub.Instance;
            var g = hub != null && hub.S != null ? hub.S.girlfriend : null;
            return g != null && g.started && g.herMessages > 0 && !hub.GirlfriendTyping;
        }

        /// <summary>Whether a beat may start now, from the running desktop.</summary>
        public static bool Turn(OpeningBeat beat, StoryDesktopPresenter presenter)
            => OpeningQuiet.Turn(beat, MonthCardDone(presenter), AiJoined(), QingwenSpoke());
    }
}
