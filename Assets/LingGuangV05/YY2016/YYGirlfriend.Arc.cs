using LingGuangV05.Core;
using LingGuangV05.Core.Girlfriend;

namespace LingGuangV05.Desktop.YY
{
    /// <summary>
    /// 晴雯的支线 in YY (Core/Girlfriend/QingwenArc.cs): when the lab is far enough along and nothing else is on screen,
    /// she sends the next beat and its three answers come up as buttons. Her schedule does not hold a beat back (she
    /// "squeezes in a message"), so she is there in the first hour whatever the desktop clock says. A picked answer gets
    /// her scripted reply; a typed one goes to her model with what she just asked.
    /// </summary>
    public sealed partial class YYGirlfriend
    {
        /// <summary>Seconds of play, past the time she waits for his answer, during which her replies ignore her schedule too.</summary>
        const double ArcHoldSeconds = 90;

        /// <summary>A beat is open or just answered: she is around for it whatever her schedule says.</summary>
        bool ArcHolds(GirlfriendState g, GfNow now)
            => g != null && (!string.IsNullOrEmpty(g.arcOpen) || g.arcAt > 0 && now.game - g.arcAt < QingwenArc.OpenFor + ArcHoldSeconds);

        /// <summary>Sends the next beat when it is due and the chat is free. True when one was sent.</summary>
        bool ArcTick(GirlfriendState g, GfNow now)
        {
            if (typing != null || generating || replyPending || outbox.Count > 0) return false;
            if (QingwenArc.Expire(g, now.game)) { hub.Stand(YYChatHub.GirlfriendId, null); runtime.MarkDirty(); return false; }
            if (StoryFocus.Active || hub.ChoicesOf(YYChatHub.GirlfriendId) != null) return false;
            var lab = Lab;
            if (lab == null) return false;
            var beat = QingwenArc.Due(g, lab.S.stage, lab.AbilitiesCount, lab.S.epochs, now.game);
            if (beat == null) return false;
            // Her lines first: opening the first beat marks her ordinary hello as done, which drops it from the lines.
            var lines = QingwenArc.Lines(g, beat, now.clock.Hour, now.clock.Minute);
            QingwenArc.Opened(g, beat, now.game);
            runtime.MarkDirty();
            var choices = QingwenArc.Choices(beat, English);
            QueueLines(lines, true, () =>
            {
                // Her own buttons on her conversation (standing choices), so another chat's scene never replaces them.
                if (G == g && g.arcOpen == beat.key) hub.Stand(YYChatHub.GirlfriendId, choices);
            });
            return true;
        }

        /// <summary>
        /// His line while a beat is open: a picked answer gives her scripted reply (<paramref name="scripted"/>), anything
        /// else the model's situation (<paramref name="situation"/>). False when no beat was open.
        /// </summary>
        bool ArcHeard(GirlfriendState g, string text, ref string[] scripted, ref string situation)
        {
            var beat = QingwenArc.Answer(g, text, out var picked);
            if (beat == null) return false;
            hub.Stand(YYChatHub.GirlfriendId, null);
            runtime.MarkDirty();
            if (picked != null && picked.reply.Length > 0) scripted = picked.reply;
            else situation = QingwenArc.FreeSituation(beat, English);
            return true;
        }
    }
}
