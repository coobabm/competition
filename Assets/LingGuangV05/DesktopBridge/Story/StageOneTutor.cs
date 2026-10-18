using LingGuangV05.Core;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// 第一阶段全程引导: when the to-do note's first step in stage one changes to one that has a lesson
    /// (<see cref="XgSim.TutorFor"/>), the protagonist thinks it through in the inner voice, and a moment later 老周 answers
    /// in YY in plain words, with answers to pick (LaoZhouYY; the forum's private messages when YY is not there). Each lesson once per save (<see cref="XgState.tutorSeen"/>). It waits
    /// for cutscenes, story moments, the first lesson and other inner-voice lines, and for the player to have looked at the
    /// step for a moment, so the note's new line comes first and the lesson second.
    /// </summary>
    public sealed class StageOneTutor : MonoBehaviour
    {
        /// <summary>Seconds a step must stay the note's first line before its lesson plays.</summary>
        public const float SettleSeconds = 2.5f;
        /// <summary>Seconds between the thought and 老周's message.</summary>
        public const float ReplySeconds = 2.2f;
        const float CheckEvery = .5f;

        MonoBehaviour host;
        ChapterOneRuntime runtime;
        string watching;
        float since, nextCheck;
        XgTutorLesson replyPending;
        float replyAt;

        /// <summary>Adds the tutor next to the story presenter (once). Call from its Start().</summary>
        public static StageOneTutor Install(MonoBehaviour host)
        {
            if (host == null) return null;
            var tutor = host.GetComponent<StageOneTutor>() ?? host.gameObject.AddComponent<StageOneTutor>();
            tutor.host = host;
            return tutor;
        }

        bool Busy()
        {
            if (AiJoinsYy.Playing || AutoLabelEpiphany.Playing || InnerVoice.Busy || StoryFocus.Active) return true;
            return host is StoryDesktopPresenter presenter && (presenter.CutscenePlaying || presenter.NotificationsHeld);
        }

        void Update()
        {
            if (replyPending != null && Time.unscaledTime >= replyAt)
            {
                var lesson = replyPending; replyPending = null;
                // 老周 now teaches in YY, with answers to pick (LaoZhouYY); the forum DM is the fallback.
                if (!LaoZhouYY.Lesson(lesson))
                {
                    InnerVoice.Say(lesson.zhouZh, lesson.zhouEn, 4f);
                }
            }
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + CheckEvery;
            if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.Sim == null || runtime.TestMode || runtime.Sim.InPrologue) return;
            var sim = GuideNote.TopSim;
            if (sim != null && sim.S.teachingFlow && !Busy())
            {
                string zh = null, en = null;
                if (!sim.S.teachIntroSaid) { sim.S.teachIntroSaid = true; zh = "这又是什么鬼？我难道要做题吗？"; en = "What on earth is this? Am I supposed to answer questions?"; }
                else if (!sim.S.teachThirdSaid && sim.TeachingAnswers >= 3) { sim.S.teachThirdSaid = true; zh = "这都是什么弱智题目？有什么意义啊？……先做完再说吧。"; en = "What are these silly questions? What's the point? …I'll finish them first."; }
                else if (!sim.S.teachTreeSaid && sim.TeachingTreeOpen) { sim.S.teachTreeSaid = true; zh = "做完了……然后呢？"; en = "Done… now what?"; }
                else if (!sim.S.teachTrainSaid && sim.S.epochs > 0) { sim.S.teachTrainSaid = true; zh = "……它在答我刚才答的题？所以那几道弱智题，是在教它。"; en = "…It's answering the questions I just answered? So those silly questions were teaching it."; }
                if (zh != null) { InnerVoice.Say(zh, en); runtime.MarkDirty(); return; }
            }
            var step = GuideNote.TopStep;
            string id = step == null ? null : step.id + "|" + step.target;
            if (id != watching) { watching = id; since = Time.unscaledTime; }
            // The relic pick that follows a learned ability covers the screen: the lesson waits for it.
            if (sim == null || step == null || replyPending != null || Time.unscaledTime - since < SettleSeconds || Busy() || sim.RelicPickPending) return;
            var due = sim.TutorDue(step);
            if (due == null) return;
            sim.MarkTutor(due);
            InnerVoice.Say(due.innerZh, due.innerEn);
            replyPending = due;
            replyAt = Time.unscaledTime + ReplySeconds;
        }
    }
}
