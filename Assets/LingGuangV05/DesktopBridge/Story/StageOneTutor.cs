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
            if (replyPending != null && Time.unscaledTime >= replyAt && !Busy())
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
            var lab = LingGuangV05.Desktop.XingGuang.XingGuangController.Instance;
            if (sim != null && lab != null && ReferenceEquals(lab.Sim, sim) && !Busy())
            {
                int thoughtVersion = sim.S.teachingThoughtVersion;
                var thought = sim.TakeTeachingThought(lab.View != null && lab.View.Visible);
                if (thoughtVersion != sim.S.teachingThoughtVersion) runtime.MarkDirty();
                if (thought.HasValue)
                {
                    var line = thought.Value;
                    InnerVoice.Say(line.zh, line.en, line.seconds);
                    runtime.MarkDirty();
                    return;
                }
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
