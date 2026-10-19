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
        [Serializable] sealed class GroupPresetFile { public GroupPreset[] presets; }
        [Serializable] sealed class GroupPreset
        {
            public string id, text, textEn, after;
            public int[] month;
            public int minDate;
            public string[] replies, repliesEn;
        }
        static GroupPreset[] groupPresets;
        readonly Queue<string> presetReplyQueue = new Queue<string>();
        bool presetReplyRunning;
        Coroutine presetReplyRoutine;
        YYState presetReplyOwner;
        int presetReplyGeneration;
        // Presentation scheduling only: does not start, advance or alter any authored chat.
        public bool GroupScriptPlaying => isActiveAndEnabled &&
            (IntroPlaying || presetReplyRunning && ReferenceEquals(presetReplyOwner, S)
                || groupColdRoutine != null && ReferenceEquals(groupColdOwner, S)
                || script != null && (scriptFor == EraGroup || Contact(scriptFor)?.group == true));

        void OnEnable() { StartPresetReplies(); StartGroupColdScenes(); }
        void OnDisable() { StopPresetReplies(false); StopGroupColdScenes(); }

        void StopPresetReplies(bool discard)
        {
            // Invalidate before disposal: an older iterator's finally cannot clear resumed work.
            presetReplyGeneration++;
            var previous = presetReplyRoutine;
            presetReplyRoutine = null; presetReplyRunning = false;
            if (previous != null) StopCoroutine(previous);
            if (discard) { presetReplyQueue.Clear(); presetReplyOwner = null; }
        }

        void StartPresetReplies()
        {
            if (!isActiveAndEnabled || presetReplyRunning || presetReplyQueue.Count == 0) return;
            if (S == null || !ReferenceEquals(presetReplyOwner, S)) { StopPresetReplies(true); return; }
            int generation = ++presetReplyGeneration;
            presetReplyRunning = true;
            presetReplyRoutine = StartCoroutine(DeliverPresetReplies(S, generation));
        }

        static void LoadGroupPresets()
        {
            if (groupPresets == null)
            {
                var asset = Resources.Load<TextAsset>("LingGuangV05/YY/yy_group_presets");
                groupPresets = asset != null ? JsonUtility.FromJson<GroupPresetFile>(asset.text)?.presets : null;
                if (groupPresets == null) groupPresets = Array.Empty<GroupPreset>();
            }
        }

        /// <summary>Date/event/member-gated presets not yet sent in this save. Pure read: no repair, import or freeze.</summary>
        List<GroupPreset> AvailableGroupPresets()
        {
            LoadGroupPresets();
            var sent = S?.groupPresets?.sentIds;
            var result = new List<GroupPreset>();
            var date = GameCalendar.DateOf(GroupPresetDay);
            int month = date.Year * 100 + date.Month;
            int day = month * 100 + date.Day;
            foreach (var p in groupPresets)
            {
                if (p == null || string.IsNullOrEmpty(p.id)) continue;
                if (sent != null && sent.Contains(p.id)) continue;
                if (p.minDate > 0 && day < p.minDate) continue;
                if (p.month != null && p.month.Length >= 2 && (month < p.month[0] || month > p.month[1])) continue;
                if (!string.IsNullOrEmpty(p.after) && !(runtime.Sim.S.eraDelivered?.Contains(p.after) ?? false)) continue;
                // Keep members from replying before they have joined the group.
                if (date < YYGroup.LiliJoins && p.replies != null && Array.Exists(p.replies, x => x.StartsWith("[莉莉]", StringComparison.Ordinal))) continue;
                result.Add(p);
            }
            return result;
        }

        /// <summary>
        /// Pure read for ChoicesOf: today's frozen ids still unsent and eligible (at most three, no same-day refill).
        /// Before today is frozen it previews exactly what FreezeGroupPresetDay would store, without storing it.
        /// </summary>
        string[] GroupPresetChoices()
        {
            var available = AvailableGroupPresets();
            var ids = GroupPresetLifecycle.Visible(S.groupPresets, GroupPresetDay, available.ConvertAll(p => p.id));
            var result = new string[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                var p = available.Find(x => x.id == ids[i]);
                result[i] = GameText.T(p.text, p.textEn);
            }
            return result;
        }

        bool SendGroupPreset(string text)
        {
            foreach (var p in AvailableGroupPresets())
            {
                if (text != p.text && text != p.textEn) continue;
                // Freeze today before recording the send, so the remaining bubbles cannot shift or refill.
                FreezeGroupPresetDay();
                if (!GroupPresetLifecycle.MarkSent(S.groupPresets, p.id)) return false;
                var lines = GameText.IsEnglish ? p.repliesEn : p.replies;
                QueueGroupAuthoredReplies(lines);
                S.groupPresetTurn++; // Legacy counter, kept for old diagnostics/checks; no longer selects bubbles.
                Touch();
                StartPresetReplies();
                return true;
            }
            return false;
        }

        void QueueGroupAuthoredReplies(IEnumerable<string> lines)
        {
            // Authored responses keep their own queue even if the player's next free line is a cold turn.
            InvalidateGroupFreeReply();
            if (!ReferenceEquals(presetReplyOwner, S)) { StopPresetReplies(true); presetReplyOwner = S; }
            if (lines != null) foreach (var line in lines) if (!string.IsNullOrWhiteSpace(line)) presetReplyQueue.Enqueue(line);
            StartPresetReplies();
        }

        IEnumerator DeliverPresetReplies(YYState expected, int generation)
        {
            try
            {
                while (generation == presetReplyGeneration && presetReplyQueue.Count > 0 && ReferenceEquals(S, expected))
                {
                    yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(2f, 4f));
                    while (generation == presetReplyGeneration && (LingGuangV05.Core.StoryFocus.Active||IntroPlaying)) yield return null;
                    if (generation != presetReplyGeneration || !ReferenceEquals(S, expected)) break;
                    Receive(EraGroup, presetReplyQueue.Dequeue());
                }
            }
            finally
            {
                if (generation == presetReplyGeneration)
                {
                    presetReplyQueue.Clear(); presetReplyOwner = null;
                    presetReplyRunning = false; presetReplyRoutine = null;
                }
            }
        }
    }
}
