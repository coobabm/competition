using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 是否夜 in its YY conversation (XgSim.YesNoNight.cs). When the stage-1 exam opens and the desktop is quiet, the
    /// protagonist thinks of testing it and YY points at its chat. Each round: friends' messages that bring the question
    /// (阿杰's riddle in the group, 咸鱼's DM), a thought, then the question as the only button. Its answer comes as
    /// 是 / 否 / …… bubbles with 「对方正在输入…」 (the 它在学 lens flickers its candidates), then the thoughts, then
    /// the friends again; the next round waits until the thoughts are read. Anything typed during the night is answered
    /// the ordinary stage-1 way. A reload resumes at the round on the buttons without sending its lead-in twice.
    /// </summary>
    public sealed partial class XgYyTalk
    {
        const float NightAsideGap = 1.4f, NightRoundGap = 1.6f, NightBubbleGap = .25f;

        readonly Queue<XgYnBubble> nightBubbles = new Queue<XgYnBubble>();
        readonly Queue<XgYnAside> nightAsides = new Queue<XgYnAside>();
        XgYnTurn nightTurn;
        float nightNext = -1;
        bool nightTyping, nightReady, nightIntro;
        StoryDesktopPresenter presenter;

        /// <summary>The night is between buttons: a lead-in or an answer is playing.</summary>
        bool NightPlaying => nightTurn != null || bound != null && bound.YnNightActive && !nightReady;

        void ResetNight()
        {
            nightBubbles.Clear(); nightAsides.Clear();
            nightTurn = null; nightNext = -1; nightTyping = false; nightReady = false; nightIntro = false;
        }

        string[] NightChoices() => bound != null && bound.YnNightActive && nightReady && nightTurn == null ? bound.YnNightChoices(GameText.IsEnglish) : null;

        bool NightBusy()
        {
            if (AiJoinsYy.Playing || AutoLabelEpiphany.Playing || StoryFocus.Active || bound.RelicPickPending) return true;
            if (presenter == null) presenter = FindAnyObjectByType<StoryDesktopPresenter>();
            return presenter != null && (presenter.CutscenePlaying || presenter.NotificationsHeld);
        }

        void MarkNightDirty() { if (controller != null && controller.runtime != null) controller.runtime.MarkDirty(); }

        void StepNight(YYChatHub hub)
        {
            var sim = bound;
            var runtime = controller != null ? controller.runtime : null;
            if (sim == null || runtime == null || runtime.TestMode || runtime.Sim == null || runtime.Sim.InPrologue) return;
            float now = Time.unscaledTime;
            // The last answer marks the night done before its bubbles play: they still play out.
            if (nightTurn != null) { StepAnswer(hub, now); return; }
            if (!sim.YnNightActive)
            {
                if (!sim.YnNightDue || waiting || NightBusy() || InnerVoice.Busy) return;
                sim.YnNightStart();
                MarkNightDirty();
                InnerVoice.Say("……它到底懂不懂？", "…Does it actually understand anything?", 2.6f);
                InnerVoice.Say("去 YY 考考它。真的难的那种。", "Let's test it on YY. Properly hard questions.", 2.8f);
                Point();
                nightNext = now + 2f;
                return;
            }
            if (nightReady || now < nightNext) return;
            var round = sim.YnRound;
            if (round == null) return;
            if (sim.S.ynIntro < sim.S.ynStep)
            {
                if (NightBusy()) return;
                sim.S.ynIntro = sim.S.ynStep;
                MarkNightDirty();
                foreach (var a in round.before) nightAsides.Enqueue(a);
                nightIntro = round.introZh.Length > 0;
            }
            if (nightAsides.Count > 0) { SendAside(hub, nightAsides.Dequeue()); nightNext = now + NightAsideGap; return; }
            if (nightIntro)
            {
                nightIntro = false;
                InnerVoice.Say(round.introZh, round.introEn, 2.4f);
                nightNext = now + 1.2f;
                return;
            }
            nightReady = true;
        }

        void StepAnswer(YYChatHub hub, float now)
        {
            if (now < nightNext) return;
            if (nightBubbles.Count > 0)
            {
                var b = nightBubbles.Peek();
                if (!nightTyping)
                {
                    nightTyping = true;
                    hub.TypingShown = Id;
                    nightNext = now + b.typing;
                    XgYyLens.Typing(bound, T(b.zh, b.en), nightNext);
                    return;
                }
                nightBubbles.Dequeue();
                nightTyping = false;
                if (hub.TypingShown == Id) hub.TypingShown = null;
                bound.AddLine("ai", T(b.zh, b.en));
                nightNext = now + NightBubbleGap;
                if (nightBubbles.Count == 0)
                {
                    foreach (var line in nightTurn.voice) InnerVoice.Say(line.zh, line.en, line.seconds);
                    foreach (var a in nightTurn.after) nightAsides.Enqueue(a);
                    nightNext = now + 1.2f;
                }
                return;
            }
            if (nightAsides.Count > 0) { SendAside(hub, nightAsides.Dequeue()); nightNext = now + NightAsideGap; return; }
            if (InnerVoice.Busy) return; // the next round waits until he has finished thinking
            nightTurn = null;
            nightReady = false;
            nightNext = now + NightRoundGap;
        }

        static void SendAside(YYChatHub hub, XgYnAside a)
        {
            string text = T(a.zh, a.en);
            if (a.mine) hub.SendAuthoredChoice(a.to, text);
            else hub.Receive(a.to, text);
        }

        /// <summary>The player sent the round's question: its scripted answer starts playing (true: nothing else answers).</summary>
        bool NightHeard(XgSim sim, string text)
        {
            if (!sim.YnNightActive || !nightReady || nightTurn != null || !sim.YnNightMatches(text)) return false;
            Echo(() => sim.AddLine("me", text));
            var turn = sim.YnNightAnswer(text);
            if (turn == null) return false;
            nightTurn = turn;
            foreach (var b in turn.bubbles) nightBubbles.Enqueue(b);
            nightReady = false;
            nightNext = Time.unscaledTime + .4f;
            MarkNightDirty();
            return true;
        }
    }
}
