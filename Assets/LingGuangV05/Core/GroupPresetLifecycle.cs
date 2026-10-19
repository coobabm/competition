using System;
using System.Collections.Generic;

namespace LingGuangV05.Core
{
    /// <summary>One pending public self-chat after a silent group turn. Flat fields only (JsonUtility-safe).</summary>
    [Serializable]
    public sealed class GroupColdScene
    {
        public int topic, nextLine;
    }

    /// <summary>
    /// Persistent ordinary group-preset lifecycle inside YYState. Only flat fields, List&lt;string&gt; and
    /// List&lt;GroupColdScene&gt; (no nested lists, no dictionaries), so Unity JsonUtility round-trips it and an old
    /// save without the field loads the initializer values.
    /// </summary>
    [Serializable]
    public sealed class GroupPresetState
    {
        public bool historyImported, hasDailySelection;
        public int selectionDay, weekKey, weeklyFreeInputs;
        public List<string> sentIds = new List<string>();
        public List<string> dailyIds = new List<string>();
        public List<GroupColdScene> pendingColdScenes = new List<GroupColdScene>();
    }

    /// <summary>
    /// Calendar-based ordinary group choices. No wall clock, RNG or Unity dependencies.
    /// Write paths: <see cref="Repair"/>, <see cref="ImportHistory"/>, <see cref="MarkSent"/>, <see cref="Freeze"/>,
    /// <see cref="RecordFreeInput"/>. <see cref="Visible"/> is a pure read and never changes the state.
    /// </summary>
    public static class GroupPresetLifecycle
    {
        public const int DailyCount = 3;
        public const int FirstColdInput = 2, SecondColdInput = 5;

        public static void Repair(GroupPresetState s)
        {
            if (s == null) return;
            if (s.sentIds == null) s.sentIds = new List<string>();
            if (s.dailyIds == null) s.dailyIds = new List<string>();
            if (s.pendingColdScenes == null) s.pendingColdScenes = new List<GroupColdScene>();
            s.weeklyFreeInputs = Math.Max(0, s.weeklyFreeInputs);
            s.pendingColdScenes.RemoveAll(x => x == null || x.topic < 1 || x.topic > 2 || x.nextLine < 0 || x.nextLine >= 2);
        }

        // Call with ids matched to genuine player history, never the old rotation counter.
        public static void ImportHistory(GroupPresetState s, IEnumerable<string> ids)
        {
            if (s == null) return;
            Repair(s);
            if (s.historyImported) return;
            if (ids != null) foreach (var id in ids) MarkSent(s, id);
            s.historyImported = true;
        }

        public static bool MarkSent(GroupPresetState s, string id)
        {
            if (s == null) return false;
            Repair(s);
            if (string.IsNullOrEmpty(id) || s.sentIds.Contains(id)) return false;
            s.sentIds.Add(id);
            return true;
        }

        public static bool IsFrozen(GroupPresetState s, int day) =>
            s != null && s.hasDailySelection && s.selectionDay == day && s.dailyIds != null;

        /// <summary>
        /// The single explicit write path for the day's choices. Freezes at most three unsent eligible ids for
        /// <paramref name="day"/>. Returns true only when the state actually changed (a new day was frozen).
        /// </summary>
        public static bool Freeze(GroupPresetState s, int day, IEnumerable<string> eligible)
        {
            if (s == null) return false;
            Repair(s);
            if (IsFrozen(s, day)) return false;
            s.dailyIds = Pick(Unsent(s, eligible), day);
            s.selectionDay = day; s.hasDailySelection = true;
            return true;
        }

        /// <summary>
        /// Pure read. Frozen day: the frozen ids that are still unsent and eligible (sent or newly ineligible entries
        /// disappear; no refill until the next game day). Not yet frozen: exactly what <see cref="Freeze"/> would pick
        /// now, without storing it. Tolerates null lists from old saves without repairing them.
        /// </summary>
        public static string[] Visible(GroupPresetState s, int day, IEnumerable<string> eligible)
        {
            var available = Unsent(s, eligible);
            var source = IsFrozen(s, day) ? s.dailyIds : Pick(available, day);
            var result = new List<string>();
            foreach (var id in source) if (available.Contains(id) && !result.Contains(id)) result.Add(id);
            return result.ToArray();
        }

        static List<string> Unsent(GroupPresetState s, IEnumerable<string> eligible)
        {
            var sent = s?.sentIds;
            var available = new List<string>();
            if (eligible != null) foreach (var id in eligible)
                if (!string.IsNullOrEmpty(id) && (sent == null || !sent.Contains(id)) && !available.Contains(id)) available.Add(id);
            return available;
        }

        static List<string> Pick(List<string> available, int day)
        {
            var picked = new List<string>();
            if (available.Count == 0) return picked;
            int offset = (int)((Math.Max(0L, day) * DailyCount) % available.Count);
            for (int i = 0; i < Math.Min(DailyCount, available.Count); i++) picked.Add(available[(offset + i) % available.Count]);
            return picked;
        }

        /// <summary>Monday-based week of a calendar date, as yyyymmdd of that Monday.</summary>
        public static int WeekKey(DateTime date)
        {
            var monday = date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
            return monday.Year * 10000 + monday.Month * 100 + monday.Day;
        }

        /// <summary>Week of a GameCalendar day index (the same day the preset selection is frozen for).</summary>
        public static int WeekKeyForDay(int day) => WeekKey(GameCalendar.DateOf(day));

        /// <summary>Default A refinement: only genuine ordinary free text counts. No fabricated missed turns.</summary>
        public static bool RecordFreeInput(GroupPresetState s, DateTime date)
        {
            if (s == null) return false;
            Repair(s);
            int week = WeekKey(date);
            if (s.weekKey != week) { s.weekKey = week; s.weeklyFreeInputs = 0; }
            if (s.weeklyFreeInputs < int.MaxValue) s.weeklyFreeInputs++;
            int topic = s.weeklyFreeInputs == FirstColdInput ? 1 : s.weeklyFreeInputs == SecondColdInput ? 2 : 0;
            if (topic == 0) return false;
            s.pendingColdScenes.Add(new GroupColdScene { topic = topic });
            return true;
        }

        public static string ColdLine(int topic, int line, bool english)
        {
            if (topic == 1)
                return line == 0
                    ? (english ? "[Da Wei] Still at the office. The last bus is getting close." : "[大伟] 还在公司，快赶不上末班车了。")
                    : (english ? "[Xiao Gang] Our floor just turned its lights off. Heading home." : "[小刚] 我们这层刚关灯，准备回家。");
            return line == 0
                ? (english ? "[A-Jie] The noodle shop downstairs is still open. Getting supper." : "[阿杰] 楼下面馆还开着，我去吃个夜宵。")
                : (english ? "[Xiao Gang] I had noodles too. Extra egg today." : "[小刚] 我今天也吃的面，加了个蛋。");
        }
    }
}
