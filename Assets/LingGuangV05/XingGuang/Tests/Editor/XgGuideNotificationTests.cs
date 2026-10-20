using System.Collections.Generic;
using NUnit.Framework;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgGuideNotificationTests
    {
        static XgGuideStep Task(string id="one")=>new XgGuideStep{id=id,zh="学习",en="Learn",target="node:train.curve",goal=12,current=0};
        [Test]public void EmptyStartDoesNotNotifyAndTriggeredTasksCoalesce()
        {
            var state=new XgGuideNotificationState();state.Observe(new List<XgGuideStep>(),0,false);
            Assert.IsFalse(state.Unread);Assert.IsNull(state.Drain(10,false));
            state.Observe(new[]{Task()},1,false);state.Observe(new[]{Task(),Task("two")},1.5,false);
            Assert.IsNull(state.Drain(2.9,false));Assert.IsNull(state.Drain(10,true));
            Assert.AreEqual(2,state.Drain(10,false).Count);Assert.IsTrue(state.Unread);state.Acknowledge();Assert.IsFalse(state.Unread);
        }
        [Test]public void NumericProgressDoesNotSpamButGoalAndNavigationChangesNotify()
        {
            var state=new XgGuideNotificationState();var task=Task();state.Observe(new[]{task},0,true);
            task.current=5;task.zh="还剩7题";state.Observe(new[]{task},1,false);Assert.AreEqual(0,state.PendingCount);
            task.current=12;state.Observe(new[]{task},2,false);Assert.AreEqual(1,state.PendingCount);state.Acknowledge();
            task.target="node:train.data";state.Observe(new[]{task},3,false);Assert.AreEqual(1,state.PendingCount);
        }
        [Test]public void RestoredTasksAreQuietAndResetDoesNotLeakUnreadAcrossSaves()
        {
            var state=new XgGuideNotificationState();state.Observe(new[]{Task()},0,true);Assert.IsFalse(state.Unread);
            state.Observe(new[]{Task("new")},1,false);Assert.IsTrue(state.Unread);state.Reset();Assert.AreEqual(0,state.PendingCount);Assert.IsFalse(state.Unread);
            state.Observe(new[]{Task()},2,true);Assert.IsFalse(state.Unread);
        }
        [Test]public void ReprioritisedTaskDoesNotPretendToBeCompletedOrNewAgain()
        {
            var state=new XgGuideNotificationState();state.Observe(new[]{Task()},0,true);
            state.Observe(new XgGuideStep[0],1,false);state.Observe(new[]{Task()},2,false);Assert.AreEqual(0,state.PendingCount);
        }
    }
}
