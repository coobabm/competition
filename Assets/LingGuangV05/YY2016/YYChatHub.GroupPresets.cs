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
            public string[] replies, repliesEn;
        }
        static GroupPreset[] groupPresets;
        readonly Queue<string> presetReplyQueue = new Queue<string>();
        bool presetReplyRunning;

        List<GroupPreset> AvailableGroupPresets()
        {
            if (groupPresets == null)
            {
                var asset = Resources.Load<TextAsset>("LingGuangV05/YY/yy_group_presets");
                groupPresets = asset != null ? JsonUtility.FromJson<GroupPresetFile>(asset.text)?.presets : null;
                if (groupPresets == null) groupPresets = Array.Empty<GroupPreset>();
            }
            var result = new List<GroupPreset>();
            var date = GameCalendar.Now(runtime.Sim.S);
            int month = date.Year * 100 + date.Month;
            foreach (var p in groupPresets)
            {
                if (p.month != null && p.month.Length >= 2 && (month < p.month[0] || month > p.month[1])) continue;
                if (!string.IsNullOrEmpty(p.after) && !(runtime.Sim.S.eraDelivered?.Contains(p.after) ?? false)) continue;
                // Keep members from replying before they have joined the group.
                if (date < YYGroup.LiliJoins && p.replies != null && Array.Exists(p.replies, x => x.StartsWith("[莉莉]", StringComparison.Ordinal))) continue;
                result.Add(p);
            }
            return result;
        }

        string[] GroupPresetChoices()
        {
            var available = AvailableGroupPresets();
            int count = Math.Min(3, available.Count);
            var result = new string[count];
            int offset = available.Count == 0 ? 0 : Math.Max(0, S.groupPresetTurn) % available.Count;
            for (int i = 0; i < count; i++)
            {
                var p = available[(offset + i) % available.Count];
                result[i] = GameText.T(p.text, p.textEn);
            }
            return result;
        }

        bool SendGroupPreset(string text)
        {
            foreach (var p in AvailableGroupPresets())
            {
                if (text != p.text && text != p.textEn) continue;
                var lines = GameText.IsEnglish ? p.repliesEn : p.replies;
                if (lines != null) foreach (var line in lines) if (!string.IsNullOrWhiteSpace(line)) presetReplyQueue.Enqueue(line);
                S.groupPresetTurn++;
                Touch();
                if (!presetReplyRunning) StartCoroutine(DeliverPresetReplies(S));
                return true;
            }
            return false;
        }

        IEnumerator DeliverPresetReplies(YYState expected)
        {
            presetReplyRunning = true;
            while (presetReplyQueue.Count > 0 && ReferenceEquals(S, expected))
            {
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(2f, 4f));
                while (LingGuangV05.Core.StoryFocus.Active) yield return null;
                if (!ReferenceEquals(S, expected)) break;
                Receive(EraGroup, presetReplyQueue.Dequeue());
            }
            presetReplyQueue.Clear();
            presetReplyRunning = false;
        }
    }
}
