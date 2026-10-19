using System;
using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using UnityEngine;

namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYChatHub
    {
        Coroutine groupColdRoutine;
        YYState groupColdOwner;
        int groupColdGeneration, groupReplyGeneration, observedGroupDay = -1;

        /// <summary>The game day the ordinary group choices are frozen for (GameCalendar day index).</summary>
        int GroupPresetDay => runtime != null && runtime.Sim != null ? GameCalendar.CurrentDay(runtime.Sim.S) : 0;

        /// <summary>Null-safety for old saves plus a one-time exact history backfill. Never guesses from groupPresetTurn.</summary>
        void RepairGroupLifecycle()
        {
            if (S == null) return;
            if (S.groupPresets == null) S.groupPresets = new GroupPresetState();
            GroupPresetLifecycle.Repair(S.groupPresets);
            if (S.groupPresets.historyImported) return;
            LoadGroupPresets();
            var ids = new List<string>();
            var conversation = S.conversations?.Find(x => x != null && x.id == EraGroup);
            if (conversation?.messages != null)
                foreach (var line in conversation.messages)
                    if (line != null && line.from == Me && !line.byAi && string.IsNullOrEmpty(line.sticker))
                        foreach (var p in groupPresets)
                            if (p != null && (string.Equals(line.text, p.text, StringComparison.Ordinal) || string.Equals(line.text, p.textEn, StringComparison.Ordinal)))
                                ids.Add(p.id);
            GroupPresetLifecycle.ImportHistory(S.groupPresets, ids);
        }

        /// <summary>
        /// The only write path for the day's choices: freezes today's ids once. Returns true when the state changed.
        /// Callers decide whether that change is worth a dirty mark (bind: no; a real day change: yes).
        /// </summary>
        bool FreezeGroupPresetDay()
        {
            if (S == null || runtime == null || runtime.Sim == null) return false;
            RepairGroupLifecycle();
            var available = AvailableGroupPresets();
            return GroupPresetLifecycle.Freeze(S.groupPresets, GroupPresetDay, available.ConvertAll(p => p.id));
        }

        /// <summary>
        /// Bind: remember the loaded day and freeze from the loaded save without MarkDirty. The freeze is
        /// deterministic from the save contents, so it is identical if the game closes before the next ordinary save.
        /// </summary>
        void BindGroupPresetDay(ChapterOneSim sim)
        {
            observedGroupDay = -1;
            if (S == null || sim == null || runtime == null || !ReferenceEquals(runtime.Sim, sim)) return;
            observedGroupDay = GameCalendar.CurrentDay(sim.S);
            FreezeGroupPresetDay();
        }

        void TickGroupPresetDate()
        {
            int day = GroupPresetDay;
            if (observedGroupDay == day) return;
            bool firstObservation = observedGroupDay < 0;
            observedGroupDay = day;
            bool changed = FreezeGroupPresetDay();
            // First observation after a bind that could not see the simulation: refresh the view only, no dirty save.
            if (firstObservation) { if (changed) Changed?.Invoke(); return; }
            // A real date change: dirty only when the frozen selection actually changed.
            if (changed) Touch(); else Changed?.Invoke();
        }

        /// <summary>
        /// Ordinary free text in the group (after script, quiz, preset and intro handlers declined it). Authored
        /// answers are never swallowed: a pending quiz reply or a future-name joke does not count as a free turn.
        /// </summary>
        bool TryGroupColdTurn(string text)
        {
            if (S == null || runtime == null || runtime.Sim == null) return false;
            if (quizReplies.ContainsKey(EraGroup) || YYGroup.FutureNameReply(text) != null) return false;
            RepairGroupLifecycle();
            bool cold = GroupPresetLifecycle.RecordFreeInput(S.groupPresets, GameCalendar.DateOf(GroupPresetDay));
            Touch();
            if (!cold) return false;
            // An older pending/in-flight generic model reply must not answer the newly silent line.
            InvalidateGroupFreeReply();
            StartGroupColdScenes();
            return true;
        }

        // Preserve a queued old-save quiz answer; only the generic request loses ownership.
        void InvalidateGroupFreeReply()
        {
            groupReplyGeneration++;
            inFlight.Remove(EraGroup);
            if (!quizReplies.ContainsKey(EraGroup)) pending.Remove(EraGroup);
        }

        bool SendGroupFutureNameReply(string text)
        {
            var reply = YYGroup.FutureNameReply(text);
            if (string.IsNullOrEmpty(reply)) return false;
            var lines = new List<string>();
            bool liliJoined = GameCalendar.DateOf(GroupPresetDay) >= YYGroup.LiliJoins;
            foreach (var line in reply.Split('\n'))
                if (!string.IsNullOrWhiteSpace(line) && (liliJoined ||
                    !(line.StartsWith("[莉莉]", StringComparison.Ordinal) || line.StartsWith("[Lili]", StringComparison.Ordinal)))) lines.Add(line);
            QueueGroupAuthoredReplies(lines);
            return true;
        }

        void StopGroupColdScenes()
        {
            groupColdGeneration++;
            var previous = groupColdRoutine;
            groupColdRoutine = null; groupColdOwner = null;
            if (previous != null) StopCoroutine(previous);
        }

        void StartGroupColdScenes()
        {
            // Read-only check: starting playback never imports history or repairs the save by itself.
            if (!isActiveAndEnabled || S == null || groupColdRoutine != null) return;
            var scenes = S.groupPresets?.pendingColdScenes;
            if (scenes == null || scenes.Count == 0) return;
            groupColdOwner = S;
            groupColdRoutine = StartCoroutine(DeliverGroupColdScenes(S, ++groupColdGeneration));
        }

        IEnumerator DeliverGroupColdScenes(YYState expected, int generation)
        {
            try
            {
                while (generation == groupColdGeneration && ReferenceEquals(S, expected)
                    && expected.groupPresets?.pendingColdScenes != null && expected.groupPresets.pendingColdScenes.Count > 0)
                {
                    var scene = expected.groupPresets.pendingColdScenes[0];
                    yield return new WaitForSecondsRealtime(scene.nextLine == 0 ? 8f : 3f);
                    // Story focus, intro, authored preset replies and guide scripts keep the floor; the self-chat waits.
                    while (generation == groupColdGeneration && (StoryFocus.Active || IntroPlaying || presetReplyRunning
                        || script != null && (scriptFor == EraGroup || Contact(scriptFor)?.group == true))) yield return null;
                    if (generation != groupColdGeneration || !ReferenceEquals(S, expected)) break;
                    if (expected.groupPresets.pendingColdScenes.Count == 0 || !ReferenceEquals(expected.groupPresets.pendingColdScenes[0], scene)) continue;
                    string line = GroupPresetLifecycle.ColdLine(scene.topic, scene.nextLine, GameText.IsEnglish);
                    scene.nextLine++;
                    if (scene.nextLine >= 2) expected.groupPresets.pendingColdScenes.RemoveAt(0);
                    Receive(EraGroup, line); // Receive -> Touch: progress is dirtied with the line itself.
                }
            }
            finally
            {
                if (generation == groupColdGeneration) { groupColdRoutine = null; groupColdOwner = null; }
            }
        }
    }
}
