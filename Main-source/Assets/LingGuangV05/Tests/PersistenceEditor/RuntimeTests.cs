using NUnit.Framework;
using UnityEngine;
using LingGuangV05.Core;
using LingGuangV05.Runtime;

namespace LingGuangV05.Tests
{
    public sealed class RuntimeTests
    {
        GameObject root;
        ChapterOneRuntime runtime;
        [SetUp] public void Setup()
        {
            root = new GameObject("ChapterOneRuntimeTest");
            root.SetActive(false);
            runtime = root.AddComponent<ChapterOneRuntime>();
            runtime.useDiskSave = false;
            runtime.EnsureInitialized();
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); }

        [Test] public void InitializationIsIdempotentAndDoesNotRestartProgress()
        {
            runtime.Sim.SubmitCard(runtime.Sim.CurrentCard.id, runtime.Sim.CurrentCard.expectedYes);
            var sim = runtime.Sim;
            runtime.EnsureInitialized();
            Assert.That(runtime.Sim, Is.SameAs(sim));
            Assert.That(runtime.Sim.S.cardsReviewed, Is.EqualTo(1));
        }

        [Test] public void JsonUtilityRoundTripPreservesActiveCardRngAndTraining()
        {
            var original = runtime.Sim;
            original.Tick(12.3);
            original.SubmitCard(original.CurrentCard.id, original.CurrentCard.expectedYes);
            var restored = new ChapterOneSim(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(original.S)));
            Assert.That(restored.CurrentCard.id, Is.EqualTo(original.CurrentCard.id));
            Assert.That(restored.CurrentCard.prompt, Is.EqualTo(original.CurrentCard.prompt));
            for (int i=0;i<8;i++)
            {
                original.Tick(.7); restored.Tick(.7);
                bool answer=original.CurrentCard.expectedYes;
                original.SubmitCard(original.CurrentCard.id,answer);
                restored.SubmitCard(restored.CurrentCard.id,answer);
                Assert.That(restored.CurrentCard.predictedYes, Is.EqualTo(original.CurrentCard.predictedYes));
            }
            Assert.That(JsonUtility.ToJson(restored.S), Is.EqualTo(JsonUtility.ToJson(original.S)));
        }

        [Test] public void NameIsLengthLimitedAndCannotInjectRichText()
        {
            string old=runtime.Sim.S.aiName;
            Assert.That(runtime.RecordName("<color=red>名字</color>"),Is.False);
            Assert.That(runtime.RecordName("line\nbreak"),Is.False);
            Assert.That(runtime.RecordName(new string('好',17)),Is.False);
            Assert.That(runtime.RecordName(" "),Is.False);
            Assert.That(runtime.Sim.S.aiName,Is.EqualTo(old));
            Assert.That(runtime.RecordName(" 灵光 "),Is.True);
            Assert.That(runtime.Sim.S.aiName,Is.EqualTo("灵光"));
        }

        [Test] public void TestModeNeverCreatesOrReloadsDiskSave()
        {
            Assert.That(runtime.SaveNow(),Is.True);
            Assert.That(runtime.ReloadSave(),Is.False);
            Assert.That(runtime.SavePath,Does.Contain("测试"));
        }

        [Test] public void ReplacingSimulationNotifiesViewsAndDetachesOldSimulation()
        {
            var old=runtime.Sim;
            var next=new ChapterOneSim();
            int updates=0;
            runtime.Changed+=()=>updates++;
            runtime.SetSimulationForTests(next);
            Assert.That(runtime.Sim,Is.SameAs(next));
            Assert.That(updates,Is.GreaterThan(0));
            Assert.That(runtime.useDiskSave,Is.False);
            Assert.That(old,Is.Not.SameAs(next));
        }
    }
}
