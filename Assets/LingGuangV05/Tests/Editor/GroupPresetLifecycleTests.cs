using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.Tests
{
    public sealed class GroupPresetLifecycleTests
    {
        static readonly string[] Pool = { "a", "b", "c", "d", "e", "f", "g" };
        static readonly DateTime Monday = new DateTime(2016, 6, 6);

        // Stand-in for YYState: an old save JSON without the field must still yield a usable state.
        [Serializable] sealed class Holder { public int other; public GroupPresetState groupPresets = new GroupPresetState(); }

        [Test]
        public void SameDayFreezesThreeIdsAndSendingDoesNotRefill()
        {
            var state = new GroupPresetState();
            Assert.True(GroupPresetLifecycle.Freeze(state, 0, Pool));
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, GroupPresetLifecycle.Visible(state, 0, Pool));
            Assert.False(GroupPresetLifecycle.Freeze(state, 0, Pool), "second freeze on the same day is a no-op");
            Assert.True(GroupPresetLifecycle.MarkSent(state, "b"));
            Assert.False(GroupPresetLifecycle.MarkSent(state, "b"));
            CollectionAssert.AreEqual(new[] { "a", "c" }, GroupPresetLifecycle.Visible(state, 0, Pool));
            Assert.False(GroupPresetLifecycle.Freeze(state, 0, Pool));
            CollectionAssert.AreEqual(new[] { "a", "c" }, GroupPresetLifecycle.Visible(state, 0, Pool));
        }

        [Test]
        public void VisibleIsPureAndPreviewsTheFreeze()
        {
            var state = new GroupPresetState();
            var preview = GroupPresetLifecycle.Visible(state, 4, Pool);
            Assert.False(state.hasDailySelection);
            Assert.IsEmpty(state.dailyIds);
            Assert.AreEqual(0, state.selectionDay);
            Assert.True(GroupPresetLifecycle.Freeze(state, 4, Pool));
            CollectionAssert.AreEqual(preview, GroupPresetLifecycle.Visible(state, 4, Pool));
            // Reading another day neither unfreezes nor rewrites the stored day.
            GroupPresetLifecycle.Visible(state, 5, Pool);
            Assert.AreEqual(4, state.selectionDay);
            CollectionAssert.AreEqual(preview, state.dailyIds);
        }

        [Test]
        public void NextGameDayRefreshesWithoutNeedingAPlayerSend()
        {
            var state = new GroupPresetState();
            GroupPresetLifecycle.Freeze(state, 0, Pool);
            var first = GroupPresetLifecycle.Visible(state, 0, Pool);
            Assert.True(GroupPresetLifecycle.Freeze(state, 1, Pool));
            var next = GroupPresetLifecycle.Visible(state, 1, Pool);
            CollectionAssert.AreEqual(new[] { "d", "e", "f" }, next);
            CollectionAssert.AreNotEqual(first, next);
        }

        [Test]
        public void SentIdsNeverReturnAcrossDatesAndExhaustionDoesNotLoop()
        {
            var state = new GroupPresetState();
            GroupPresetLifecycle.Freeze(state, 0, Pool);
            GroupPresetLifecycle.MarkSent(state, "a");
            GroupPresetLifecycle.Freeze(state, 1, Pool);
            CollectionAssert.DoesNotContain(GroupPresetLifecycle.Visible(state, 1, Pool), "a");
            Assert.AreEqual(3, GroupPresetLifecycle.Visible(state, 1, Pool).Length);
            foreach (string id in Pool) GroupPresetLifecycle.MarkSent(state, id);
            Assert.IsEmpty(GroupPresetLifecycle.Visible(state, 1, Pool));
            Assert.True(GroupPresetLifecycle.Freeze(state, 8, Pool));
            Assert.IsEmpty(GroupPresetLifecycle.Visible(state, 8, Pool));
            Assert.IsEmpty(GroupPresetLifecycle.Visible(state, 99, Pool));
            Assert.AreEqual(Pool.Length, state.sentIds.Count);
        }

        [Test]
        public void SameDayGateChangesOnlyRemoveChoicesUntilNextDate()
        {
            var state = new GroupPresetState();
            GroupPresetLifecycle.Freeze(state, 0, Pool);
            CollectionAssert.AreEqual(new[] { "b", "c" }, GroupPresetLifecycle.Visible(state, 0, new[] { "b", "c", "late" }));
            Assert.False(GroupPresetLifecycle.Freeze(state, 0, new[] { "b", "c", "late" }), "a gate change is not a new day");
            CollectionAssert.AreEqual(new[] { "b", "c" }, GroupPresetLifecycle.Visible(state, 0, new[] { "b", "c", "late" }));
            CollectionAssert.Contains(GroupPresetLifecycle.Visible(state, 1, new[] { "b", "c", "late" }), "late");
        }

        [Test]
        public void OldMissingListsAreReadSafelyAndHistoryImportsOnce()
        {
            var state = JsonUtility.FromJson<GroupPresetState>("{}");
            state.sentIds = null; state.dailyIds = null; state.pendingColdScenes = null;
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, GroupPresetLifecycle.Visible(state, 0, Pool));
            Assert.IsNull(state.sentIds, "pure read does not repair");
            GroupPresetLifecycle.ImportHistory(state, new[] { "a", "a", "", null });
            GroupPresetLifecycle.ImportHistory(state, new[] { "b" });
            CollectionAssert.AreEqual(new[] { "a" }, state.sentIds);
            Assert.True(state.historyImported);
            Assert.AreEqual(0, state.weeklyFreeInputs);
            Assert.IsNotNull(state.pendingColdScenes);
            Assert.IsNotNull(state.dailyIds);
            CollectionAssert.DoesNotContain(GroupPresetLifecycle.Visible(state, 0, Pool), "a");
        }

        [Test]
        public void OldSaveWithoutFieldRepairsAFreshUnimportedState()
        {
            var holder = JsonUtility.FromJson<Holder>("{\"other\":3}");
            Assert.AreEqual(3, holder.other);
            // Match BindSave: JsonUtility versions may leave a missing nested field null.
            if (holder.groupPresets == null) holder.groupPresets = new GroupPresetState();
            GroupPresetLifecycle.Repair(holder.groupPresets);
            Assert.False(holder.groupPresets.historyImported);
            Assert.False(holder.groupPresets.hasDailySelection);
            Assert.IsNotNull(holder.groupPresets.sentIds);
            Assert.IsNotNull(holder.groupPresets.pendingColdScenes);
        }

        [Test]
        public void WeeklySilenceOccursOnSecondAndFifthRealFreeInputsOnly()
        {
            var state = new GroupPresetState();
            for (int n = 1; n <= 9; n++)
                Assert.AreEqual(n == 2 || n == 5, GroupPresetLifecycle.RecordFreeInput(state, Monday.AddDays(n % 5)), "free input " + n);
            Assert.AreEqual(9, state.weeklyFreeInputs);
            Assert.AreEqual(2, state.pendingColdScenes.Count);
            Assert.AreEqual(1, state.pendingColdScenes[0].topic);
            Assert.AreEqual(2, state.pendingColdScenes[1].topic);
        }

        [Test]
        public void MondayResetsCountAndSkippedWeeksNeverInventTurns()
        {
            var state = new GroupPresetState();
            Assert.False(GroupPresetLifecycle.RecordFreeInput(state, Monday.AddDays(6)));
            Assert.False(GroupPresetLifecycle.RecordFreeInput(state, Monday.AddDays(7)));
            Assert.True(GroupPresetLifecycle.RecordFreeInput(state, Monday.AddDays(7)));
            Assert.False(GroupPresetLifecycle.RecordFreeInput(state, Monday.AddDays(42)));
            Assert.AreEqual(1, state.pendingColdScenes.Count);
            Assert.AreEqual(1, state.weeklyFreeInputs);
            Assert.AreEqual(20151228, GroupPresetLifecycle.WeekKey(new DateTime(2016, 1, 1)));
        }

        [Test]
        public void GameDayWeekUsesTheCalendarDate()
        {
            // Day 0 is Tue 24 May 2016; its week starts Mon 23 May. Day 6 (Mon 30 May) starts the next week.
            Assert.AreEqual(20160523, GroupPresetLifecycle.WeekKeyForDay(0));
            Assert.AreEqual(20160523, GroupPresetLifecycle.WeekKeyForDay(5));
            Assert.AreEqual(20160530, GroupPresetLifecycle.WeekKeyForDay(6));
            Assert.AreEqual(GroupPresetLifecycle.WeekKey(GameCalendar.DateOf(GameCalendar.DayIndex(Monday))),
                GroupPresetLifecycle.WeekKeyForDay(GameCalendar.DayIndex(Monday)));
        }

        [Test]
        public void JsonRoundtripPreservesChoicesSentIdsWeekAndPartialContinuation()
        {
            var state = new GroupPresetState();
            GroupPresetLifecycle.ImportHistory(state, new[] { "a" });
            GroupPresetLifecycle.Freeze(state, 17, Pool);
            var choices = GroupPresetLifecycle.Visible(state, 17, Pool);
            for (int n = 0; n < 2; n++) GroupPresetLifecycle.RecordFreeInput(state, Monday);
            state.pendingColdScenes[0].nextLine = 1;
            var restored = JsonUtility.FromJson<GroupPresetState>(JsonUtility.ToJson(state));
            Assert.True(GroupPresetLifecycle.IsFrozen(restored, 17));
            Assert.False(GroupPresetLifecycle.Freeze(restored, 17, Pool));
            CollectionAssert.AreEqual(choices, GroupPresetLifecycle.Visible(restored, 17, Pool));
            CollectionAssert.AreEqual(state.sentIds, restored.sentIds);
            Assert.AreEqual(state.weekKey, restored.weekKey);
            Assert.AreEqual(2, restored.weeklyFreeInputs);
            Assert.AreEqual(1, restored.pendingColdScenes[0].nextLine);
            Assert.False(GroupPresetLifecycle.RecordFreeInput(restored, Monday));
            Assert.False(GroupPresetLifecycle.RecordFreeInput(restored, Monday));
            Assert.True(GroupPresetLifecycle.RecordFreeInput(restored, Monday));
        }

        [Test]
        public void BothLanguagesHaveAllPublicContinuationLines()
        {
            for (int topic = 1; topic <= 2; topic++) for (int line = 0; line < 2; line++)
            {
                string zh = GroupPresetLifecycle.ColdLine(topic, line, false);
                string en = GroupPresetLifecycle.ColdLine(topic, line, true);
                Assert.IsNotEmpty(zh); Assert.IsNotEmpty(en); Assert.AreNotEqual(zh, en);
                StringAssert.StartsWith("[", zh); StringAssert.StartsWith("[", en);
                StringAssert.DoesNotContain("莉莉", zh); // Both topics work before she joins.
                StringAssert.DoesNotContain("Lili", en);
            }
        }
    }
}
