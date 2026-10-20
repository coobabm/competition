using System;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LingGuangV05.UI.Tests
{
    public sealed class EpilogueDesktopClockTests
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        GameObject host, labelObject;
        ChapterOneRuntime runtime;
        ChapterOneSim save;
        Component controller, director, clock, manager, shell;
        object lab, state, ep;
        object previousManager;
        Func<DateTime> previousWall;
        static Type Desktop(string name) => Type.GetType(name + ", Assembly-CSharp", true);
        static Type Core(string name) => Type.GetType("LingGuangV05.XingGuang." + name + ", LingGuangV05.XingGuang.Core", true);
        static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Fields).SetValue(owner, value);
        static object Get(object owner, string field) => owner.GetType().GetField(field, Fields).GetValue(owner);
        static void Property(object owner, string name, object value) => owner.GetType().GetProperty(name, Fields).SetValue(owner, value);
        static object Call(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Fields).Invoke(owner, args);
        DateTime Display() => (DateTime)clock.GetType().GetMethod("DisplayNow", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { runtime });
        DateTime Economic => GameCalendar.Now(runtime.Sim.S);

        [SetUp] public void SetUp()
        {
            previousWall = GameCalendar.WallClock;
            GameCalendar.WallClock = () => new DateTime(2026, 10, 19, 18, 42, 13);
            host = new GameObject("Epilogue clock test owner"); host.SetActive(false);
            // Inactive components prevent startup, model loading, disk IO, and automatic simulation ticks.
            runtime = host.AddComponent<ChapterOneRuntime>(); runtime.useDiskSave = false; runtime.tickEnabled = false;
            save = new ChapterOneSim(); GameCalendar.Advance(save.S, GameCalendar.DayFor(6, 0));
            Property(runtime, "Sim", save);
            controller = host.AddComponent(Desktop("LingGuangV05.Desktop.XingGuang.XingGuangController"));
            lab = Activator.CreateInstance(Core("XgSim"), new object[] { null });
            state = lab.GetType().GetProperty("S").GetValue(lab);
            var release = JsonUtility.FromJson(@"{""route"":""local_only"",""releaseReceiptId"":""test:release"",""committedAt"":20161201,""prerequisites"":{""memoryCardsReceiptId"":""m"",""redTeamOneReceiptId"":""r1"",""redTeamTwoReceiptId"":""r2"",""redTeamThreeReceiptId"":""r3"",""fixedThreeSlotsReceiptId"":""s""}}", Core("ReleaseOutcome"));
            ep = JsonUtility.FromJson(@"{""schemaVersion"":1,""phase"":""reading"",""displayDate"":20170613}", Core("EpilogueState"));
            Set(state, "releaseOutcome", release); Set(state, "epilogue", ep);
            Set(controller, "runtime", runtime); Property(controller, "Sim", lab); Set(controller, "linked", save);
            director = host.AddComponent(Desktop("LingGuangV05.Desktop.XingGuang.XgEpilogueDirector"));
            Call(director, "Bind", controller, null);
            clock = host.AddComponent(Desktop("LingGuangV05.Desktop.Story.GameEraClock")); Set(clock, "runtime", runtime);
            var managerType = Desktop("LingGuangV05.Desktop.OS.DateAndTimeManager");
            previousManager = managerType.GetField("instance").GetValue(null);
            manager = host.AddComponent(managerType); managerType.GetField("instance").SetValue(null, manager);
            Set(clock, "time", manager);
            labelObject = new GameObject("Taskbar date", typeof(RectTransform)); labelObject.SetActive(false);
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            shell = host.AddComponent(Desktop("HongmengOS.Aero2010.AeroShellClock"));
            Set(shell, "desktopClock", manager); Set(shell, "dateLabel", label);
        }

        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(labelObject); Object.DestroyImmediate(host);
            Desktop("LingGuangV05.Desktop.OS.DateAndTimeManager").GetField("instance").SetValue(null, previousManager);
            GameCalendar.WallClock = previousWall;
        }

        [TestCase("headline")]
        [TestCase("route")]
        [TestCase("letter")]
        [TestCase("reading")]
        public void CurrentEpilogueReachesActualTaskbarWithoutChangingEitherSave(string phase)
        {
            Set(ep, "phase", phase);
            string beforeSave = JsonUtility.ToJson(save.S), beforeLab = JsonUtility.ToJson(state);
            var wall = GameCalendar.WallClock;
            Assert.AreEqual(new DateTime(2017, 6, 13, 8, 0, 0), Display());
            Call(clock, "LateUpdate"); Call(shell, "Refresh");
            Assert.AreEqual("6/13/2017", ((TMP_Text)Get(shell, "dateLabel")).text);
            Assert.AreEqual(8, Get(manager, "currentHour")); Assert.AreEqual(true, Get(manager, "isAm"));
            Assert.AreEqual(false, Get(manager, "saveTimeData")); Assert.AreEqual(false, Get(manager, "useSystemTime"));
            Assert.AreEqual(beforeSave, JsonUtility.ToJson(save.S)); Assert.AreEqual(beforeLab, JsonUtility.ToJson(state));
            Assert.AreSame(wall, GameCalendar.WallClock);
            Assert.AreEqual(new DateTime(2016, 12, 1, 18, 42, 13), Economic);
        }

        [TestCase("launch_night")]
        [TestCase("date")]
        [TestCase("completed")]
        [TestCase("unknown")]
        public void UnrevealedAndInactivePhasesKeepEconomicClock(string phase)
        { Set(ep, "phase", phase); Assert.AreEqual(Economic, Display()); }

        [Test] public void HistoricalFlagAndActuallyLateLegacySaveNeverTurnCalendarBackward()
        {
            Set(ep, "historicalReview", true); Assert.AreEqual(Economic, Display());
            Set(ep, "historicalReview", false);
            save.S.newYearAt = 1; save.S.gameSeconds = 1 + (new DateTime(2017, 7, 1) - GameCalendar.NewYearsDay).TotalSeconds;
            Assert.AreEqual(new DateTime(2017, 7, 1), Economic); Assert.AreEqual(Economic, Display());
        }

        [Test] public void MissingProofInvalidSchemaAndUnsupportedDateDoNotProject()
        {
            var release = Get(state, "releaseOutcome"); Set(release, "releaseReceiptId", ""); Assert.AreEqual(Economic, Display());
            Set(release, "releaseReceiptId", "test:release"); Set(ep, "schemaVersion", 2); Assert.AreEqual(Economic, Display());
            Set(ep, "schemaVersion", 1); Set(ep, "displayDate", 20990101); Assert.AreEqual(Economic, Display());
        }

        [Test] public void ReplayDisableAndSaveOwnerReplacementRestoreTheRealTaskbar()
        {
            Assert.AreEqual(2017, Display().Year);
            Set(director, "replaying", true); Assert.AreEqual(Economic, Display());
            Set(director, "replaying", false); ((Behaviour)director).enabled = false; Assert.AreEqual(Economic, Display());
            ((Behaviour)director).enabled = true;
            var replacement = new ChapterOneSim(); GameCalendar.Advance(replacement.S, GameCalendar.DayFor(2, 0));
            Property(runtime, "Sim", replacement); // Director and lab still refer to the previous save.
            Call(clock, "LateUpdate"); Call(shell, "Refresh");
            Assert.AreEqual("7/1/2016", ((TMP_Text)Get(shell, "dateLabel")).text);
            Assert.AreEqual(Economic, Display());
            Set(controller, "linked", replacement);
            Set(state, "epilogue", null); Set(state, "releaseOutcome", null);
            Call(director, "Bind", controller, null);
            Assert.AreEqual(Economic, Display());
        }

        [Test] public void RevealedDateIsSessionOnlyAndRebindingClearsIt()
        {
            Set(ep, "phase", "date"); Assert.AreEqual(Economic, Display());
            // Exercise the session latch separately from the animation's real UI acceptance test.
            Set(director, "desktopDateRevealed", true);
            Assert.AreEqual(new DateTime(2017, 6, 13, 8, 0, 0), Display());
            Call(director, "Hide"); Assert.AreEqual(2017, Display().Year);
            Call(director, "Bind", controller, null); Assert.AreEqual(Economic, Display());
        }
    }
}
