using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The 任务 queue (XgTasks.cs): appears in order, at most five shown, waits, ticks, clears, expires, stays quiet on old saves.</summary>
    public sealed class XgTaskQueueTests
    {
        static XgSim Fresh()
        {
            var sim = new XgSim();
            sim.TickTasks(); // the first read of a new game is silent and finds nothing
            return sim;
        }

        static List<string> Shown(XgSim sim) => sim.TasksShown.ToList();

        [Test] public void ANewGameStartsWithNoTasks()
        {
            var sim = new XgSim();
            Assert.IsEmpty(sim.TickTasks());
            Assert.IsEmpty(sim.TasksShown);
            Assert.AreEqual(0, sim.TasksWaiting);
            Assert.IsEmpty(sim.TickTasks(), "still nothing until the player answers");
        }

        [Test] public void TwelveAnswersBringTheFirstTasksInBookOrder()
        {
            var sim = Fresh();
            sim.S.handCorrect = 12;
            var events = sim.TickTasks();
            CollectionAssert.AreEqual(new[] { "tree.open", "streak.three", "labels.hundred" }, events.Select(e => e.task.id).ToArray());
            Assert.IsTrue(events.All(e => e.kind == XgTaskEventKind.Arrived));
            CollectionAssert.AreEqual(new[] { "tree.open", "streak.three", "labels.hundred" }, Shown(sim));
            Assert.IsEmpty(sim.TickTasks(), "nothing is announced twice");
        }

        [Test] public void AtMostFiveAreShownAndTheRestWait()
        {
            var sim = Fresh();
            sim.S.handCorrect = 40; sim.S.unlocked.Add("train.curve"); sim.S.epochs = 1; sim.S.assessments = 1; sim.S.bestCombo = 4;
            var events = sim.TickTasks();
            Assert.AreEqual(XgSim.TaskCapacity, sim.TasksShown.Count);
            Assert.Greater(sim.TasksWaiting, 0);
            Assert.AreEqual(XgSim.TaskCapacity, events.Count(e => e.kind == XgTaskEventKind.Arrived));
            Assert.AreEqual(sim.TasksWaiting, events.Count(e => e.kind == XgTaskEventKind.Queued));
        }

        [Test] public void AFinishedTaskStaysTickedUntilClearedThenAWaitingOneTakesItsPlace()
        {
            var sim = Fresh();
            sim.S.handCorrect = 40; sim.S.unlocked.Add("train.curve"); sim.S.epochs = 1; sim.S.assessments = 1; sim.S.bestCombo = 4;
            sim.TickTasks();
            string firstWaiting = sim.S.taskWaiting[0];
            sim.S.bestCombo = 3; // streak.three is already done by bestCombo 4; finish another one instead
            sim.S.payRaise = 2;
            var events = sim.TickTasks();
            Assert.IsTrue(events.Any(e => e.kind == XgTaskEventKind.Completed && e.task.id == "raise.two"));
            Assert.IsTrue(sim.TaskDone("raise.two"));
            CollectionAssert.Contains(Shown(sim), "raise.two", "a finished task stays on the list, ticked");
            var cleared = sim.ClearFinishedTasks();
            CollectionAssert.DoesNotContain(Shown(sim), "raise.two");
            Assert.AreEqual(firstWaiting, cleared.Single().task.id, "the first waiting task fills the free place");
            Assert.AreEqual(XgTaskEventKind.Arrived, cleared.Single().kind);
        }

        [Test] public void ATaskDoneBeforeItIsShownIsRecordedButNeverAnnounced()
        {
            var sim = Fresh();
            sim.S.handCorrect = 12; sim.S.unlocked.Add("train.curve");
            var events = sim.TickTasks();
            Assert.IsTrue(sim.TaskDone("tree.open"));
            Assert.IsFalse(events.Any(e => e.task.id == "tree.open"));
            CollectionAssert.DoesNotContain(Shown(sim), "tree.open");
        }

        [Test] public void AnOldSaveIsReadSilently()
        {
            var sim = new XgSim();
            sim.S.handCorrect = 200; sim.S.unlocked.Add("train.curve"); sim.S.epochs = 30; sim.S.assessments = 5; sim.S.bestCombo = 12;
            sim.S.payRaise = 4;
            Assert.AreEqual(0, sim.S.taskVersion);
            Assert.IsEmpty(sim.TickTasks(), "no flood of notices for an old save");
            Assert.IsTrue(sim.TaskDone("tree.open") && sim.TaskDone("train.first") && sim.TaskDone("streak.nine") && sim.TaskDone("raise.two"));
            Assert.LessOrEqual(sim.TasksShown.Count, XgSim.TaskCapacity);
            Assert.AreEqual(1, sim.S.taskVersion);
        }

        [Test] public void TasksTwoStagesBehindAreWithdrawn()
        {
            var sim = Fresh();
            sim.S.handCorrect = 12;
            sim.TickTasks();
            CollectionAssert.Contains(Shown(sim), "labels.hundred");
            sim.S.stage = 3;
            sim.TickTasks();
            CollectionAssert.DoesNotContain(Shown(sim), "labels.hundred");
            CollectionAssert.DoesNotContain(Shown(sim), "streak.three");
            Assert.IsTrue(sim.TaskDone("labels.hundred"), "withdrawn tasks never come back");
        }

        [Test] public void HouseholdTasksNeedTheHousehold()
        {
            var sim = Fresh();
            sim.S.stage = 3;
            sim.TickTasks();
            var all = Shown(sim).Concat(sim.S.taskWaiting).ToList();
            CollectionAssert.DoesNotContain(all, "gpu.two");
            CollectionAssert.DoesNotContain(all, "rent.on.time");
        }

        [Test] public void WithdrawnTasksGiveTheirPlacesToExistingWaitingTasksFirst()
        {
            var sim = Fresh();
            sim.S.taskActive.AddRange(new[] { "tree.open", "streak.three", "labels.hundred", "raise.two", "mlp" });
            sim.S.taskWaiting.Add("contract.first");
            sim.S.stage = 3;
            var events = sim.TickTasks();
            Assert.AreEqual("contract.first", sim.TasksShown[0]);
            Assert.AreEqual("contract.first", events.First(e => e.kind == XgTaskEventKind.Arrived).task.id);
            Assert.LessOrEqual(sim.TasksShown.Count, XgSim.TaskCapacity);
        }

        [Test] public void FailedSpotChecksDoNotCompleteThePassedChecksTask()
        {
            var sim = Fresh(); var c = new XgTaskContext(sim);
            var task = XgTaskBook.Find("qc.twenty");
            sim.S.qcCheckedTotal = 20; sim.S.qcFailedTotal = 4;
            Assert.IsFalse(task.done(c));
            Assert.AreEqual("16 / 20", task.ProgressText(c));
            sim.S.qcCheckedTotal = 24;
            Assert.IsTrue(task.done(c));
        }

        [Test] public void YesterdayDeskIncomeDoesNotCountAndReadingDoesNotRollCounters()
        {
            var sim = Fresh(); var c = new XgTaskContext(sim);
            var task = XgTaskBook.Find("income.day");
            sim.Today = 20161021; sim.S.dayStatsDay = 20161020; sim.S.dayIncome = 1500;
            string before = Dump(sim.S, 0);
            Assert.IsFalse(task.done(c));
            Assert.AreEqual(0, task.Fraction(c));
            Assert.AreEqual(before, Dump(sim.S, 0));
            sim.S.dayStatsDay = sim.Today;
            Assert.IsTrue(task.done(c));
            Assert.AreEqual(1, task.Fraction(c));
        }

        [Test] public void EveryTaskHasTextAPlaceInTheStagesAndNoSideEffects()
        {
            var ids = new HashSet<string>();
            var sim = new XgSim();
            var c = new XgTaskContext(sim);
            var stagesCovered = new HashSet<int>();
            foreach (var t in XgTaskBook.All)
            {
                Assert.IsTrue(ids.Add(t.id), "duplicate id " + t.id);
                Assert.IsNotEmpty(t.zh, t.id); Assert.IsNotEmpty(t.en, t.id); Assert.IsNotEmpty(t.bodyZh, t.id); Assert.IsNotEmpty(t.bodyEn, t.id);
                Assert.LessOrEqual(Width(t.zh), 8, t.id + " title too wide for the panel");
                Assert.LessOrEqual(Width(t.bodyZh), 22, t.id + " body too wide for the panel");
                Assert.That(t.stage, Is.InRange(1, 6), t.id);
                stagesCovered.Add(t.stage);
                if (t.HasProgress) Assert.IsNotEmpty(t.ProgressText(c), t.id);
                if (!string.IsNullOrEmpty(t.app)) Assert.IsNotNull(t.Jump(), t.id);
            }
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5, 6 }, stagesCovered, "tasks cover all six stages");
            string before = Dump(sim.S, 0);
            foreach (var t in XgTaskBook.All) { t.trigger(c); t.done(c); if (t.HasProgress) t.current(c); }
            Assert.AreEqual(before, Dump(sim.S, 0), "reading tasks changes nothing in the save");
        }

        /// <summary>Display width in CJK cells: a Chinese character is 1, a half-width letter, digit or space 0.5.</summary>
        static double Width(string text) { double w = 0; foreach (char ch in text) w += ch < 0x2E80 ? .5 : 1; return w; }

        /// <summary>A plain field-by-field dump of the save (no Unity serializer), deep enough to catch any write.</summary>
        static string Dump(object o, int depth)
        {
            if (o == null) return "null";
            var t = o.GetType();
            if (t.IsPrimitive || o is string || t.IsEnum || o is decimal) return o.ToString();
            if (depth > 4) return t.Name;
            var sb = new System.Text.StringBuilder();
            if (o is System.Collections.IEnumerable list) { sb.Append('['); foreach (var x in list) sb.Append(Dump(x, depth + 1)).Append(','); return sb.Append(']').ToString(); }
            foreach (var f in t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                sb.Append(f.Name).Append('=').Append(Dump(f.GetValue(o), depth + 1)).Append(';');
            return sb.ToString();
        }

        [Test] public void ProgressTextReadsLikeTheDesign()
        {
            var sim = new XgSim(); var c = new XgTaskContext(sim);
            sim.S.bestCombo = 4;
            Assert.AreEqual("4 / 9", XgTaskBook.Find("streak.nine").ProgressText(c));
            sim.S.dayIncome = 250;
            sim.S.dayStatsDay = sim.Today;
            Assert.AreEqual("¥250 / ¥1,000", XgTaskBook.Find("income.day").ProgressText(c));
            sim.S.best.Add(new XgBest { dataset = "mnist", acc = .41 });
            Assert.AreEqual("41% / 60%", XgTaskBook.Find("ckpt.sixty").ProgressText(c));
        }
    }
}
