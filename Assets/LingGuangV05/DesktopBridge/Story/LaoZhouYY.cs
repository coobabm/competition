using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// 老周 guides in YY (XgSim.ZhouChat.cs, 2026-10-18). The stage-one lessons (<see cref="StageOneTutor"/>) and a note at
    /// the start of every later stage come as his YY messages, typed out one bubble at a time, followed by three answers
    /// as buttons. A picked answer gets his reply; anything typed goes to his model (LlmPersonas, with the step the
    /// to-do note is on, so he answers and steers back to it) or, offline, to XgSim.ZhouSteer. Waits for cutscenes and
    /// story moments like the lessons did.
    /// </summary>
    public sealed class LaoZhouYY : MonoBehaviour
    {
        /// <summary>Seconds of 「对方正在输入…」 per bubble: a base plus a little per character, capped.</summary>
        public const float TypeBase = .9f, TypePerChar = .045f, TypeMax = 3.2f, BubbleGap = .5f;
        const float CheckEvery = .5f;

        static LaoZhouYY instance;
        readonly Queue<string> bubbles = new Queue<string>();
        XgZhouReply[] offered, after;
        ChapterOneRuntime runtime;
        MonoBehaviour host;
        float nextBubbleAt = -1, nextCheck;
        bool typing;

        public static LaoZhouYY Install(MonoBehaviour host)
        {
            if (host == null) return null;
            var z = host.GetComponent<LaoZhouYY>() ?? host.gameObject.AddComponent<LaoZhouYY>();
            z.host = host;
            return z;
        }

        void OnEnable() { instance = this; }
        void OnDisable() { if (instance == this) instance = null; }

        static string T(string zh, string en) => GameText.T(zh, en);

        /// <summary>A stage-one lesson as 老周's YY message, with its answers. False when YY is not there (the caller falls back).</summary>
        public static bool Lesson(XgTutorLesson lesson)
        {
            var z = instance;
            var hub = YYChatHub.Instance;
            if (z == null || hub == null || lesson == null) return false;
            if (hub.Conversation(YYChatHub.LaoZhou).messages.Count == 0) z.Say(T(XgSim.ZhouHelloZh, XgSim.ZhouHelloEn));
            z.Say(T(lesson.zhouZh, lesson.zhouEn));
            z.after = XgSim.ZhouReplies(lesson.id);
            return true;
        }

        /// <summary>His lines go out one bubble at a time; a long message is split at its sentences.</summary>
        void Say(string text)
        {
            foreach (var part in Split(text)) bubbles.Enqueue(part);
            if (nextBubbleAt < 0) nextBubbleAt = Time.unscaledTime + .3f;
        }

        /// <summary>Two or three bubbles for a long message (at 。！？), never more than three.</summary>
        static List<string> Split(string text)
        {
            var parts = new List<string>();
            if (string.IsNullOrEmpty(text)) return parts;
            if (text.Length <= 34) { parts.Add(text); return parts; }
            int start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                bool end = c == '。' || c == '！' || c == '？' || (c == '.' || c == '?' || c == '!') && i + 1 < text.Length && text[i + 1] == ' ';
                if (!end || i - start < 14 || parts.Count == 2) continue;
                parts.Add(text.Substring(start, i + 1 - start).Trim());
                start = i + 1;
            }
            if (start < text.Length) parts.Add(text.Substring(start).Trim());
            parts.RemoveAll(p => p.Length == 0);
            return parts;
        }

        static float TypeSeconds(string bubble) => Mathf.Min(TypeMax, TypeBase + TypePerChar * (bubble ?? "").Length);

        bool Busy()
        {
            if (AiJoinsYy.Playing || AutoLabelEpiphany.Playing || StoryFocus.Active) return true;
            return host is StoryDesktopPresenter presenter && (presenter.CutscenePlaying || presenter.NotificationsHeld);
        }

        void Update()
        {
            var hub = YYChatHub.Instance;
            if (hub == null) return;
            hub.LaoZhouHeard = Heard;
            StepBubbles(hub);
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + CheckEvery;
            if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null || runtime.Sim == null || runtime.TestMode || runtime.Sim.InPrologue) return;
            StageNote(hub);
        }

        void StepBubbles(YYChatHub hub)
        {
            if (nextBubbleAt < 0) return;
            if (bubbles.Count == 0)
            {
                nextBubbleAt = -1;
                if (hub.TypingShown == YYChatHub.LaoZhou) hub.TypingShown = null;
                if (after != null && after.Length > 0)
                {
                    offered = after;
                    hub.Offer(YYChatHub.LaoZhou, Texts(after));
                }
                after = null;
                return;
            }
            if (Busy() && !typing) return; // a cutscene: he waits to start typing
            if (!typing) { typing = true; hub.TypingShown = YYChatHub.LaoZhou; nextBubbleAt = Time.unscaledTime + TypeSeconds(bubbles.Peek()); return; }
            if (Time.unscaledTime < nextBubbleAt) return;
            hub.Receive(YYChatHub.LaoZhou, bubbles.Dequeue());
            typing = false;
            hub.TypingShown = null;
            nextBubbleAt = Time.unscaledTime + BubbleGap;
        }

        static string[] Texts(XgZhouReply[] replies)
        {
            var r = new string[replies.Length];
            for (int i = 0; i < r.Length; i++) r[i] = GameText.IsEnglish ? replies[i].en : replies[i].zh;
            return r;
        }

        /// <summary>From stage 2: his note on what the new stage is about, once per save.</summary>
        void StageNote(YYChatHub hub)
        {
            var sim = GuideNote.TopSim;
            if (sim == null || bubbles.Count > 0 || nextBubbleAt >= 0 || Busy() || sim.RelicPickPending) return;
            var note = XgSim.ZhouStageNote(sim.S.stage);
            if (note == null || sim.S.tutorSeen.Contains(XgSim.ZhouStageKey(note.stage))) return;
            // Do not cut into an answer he is still waiting for.
            if (hub.ChoicesFor == YYChatHub.LaoZhou && hub.Choices != null) return;
            sim.S.tutorSeen.Add(XgSim.ZhouStageKey(note.stage));
            runtime.MarkDirty();
            Say(T(note.zh, note.en));
            after = note.replies;
        }

        /// <summary>
        /// The player wrote to 老周 (YYChatHub.Send). A button he offered: his scripted answer, typed out (true: nobody
        /// else answers). Anything else: false, so the hub asks his model (or falls back to steering).
        /// </summary>
        bool Heard(string text)
        {
            var picked = XgSim.ZhouMatch(offered, text);
            offered = null;
            if (picked == null) return false;
            Say(T(picked.answerZh, picked.answerEn));
            return true;
        }
    }
}
