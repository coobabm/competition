using System.Collections.Generic;
using LingGuangV05.Core.Chat;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>What it recalls before answering (XgSim.Knowledge.cs): taught cards, read chapters, old talks, 2016 news.</summary>
    public sealed class XgKnowledgeTests
    {
        sealed class Vectors : IVectorSource
        {
            static float[] Of(string t) => t.Contains("坑") || t.Contains("中奖") ? new[] { 1f, 0f } : new[] { 0f, 1f };
            public float[] Query(string text) => Of(text);
            public float[] Document(string text) => Of(text);
        }

        static XgSim Sim(int stage)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.S.stage = stage;
            sim.Today = 20160701;
            sim.S.taughtNotes.Add(new XgTaughtNote { text = "恭喜您中奖，请点击领取", why = "陌生号码要钱的都是诈骗", truth = true });
            sim.S.taughtNotes.Add(new XgTaughtNote { text = "床前明月光，疑是地上霜", truth = true });
            return sim;
        }

        [Test]
        public void TaughtCardsComeBackFromStageThree()
        {
            Assert.AreEqual("", Sim(2).PersonaKnowledge("今天有人给我发中奖短信", null), "too young to recall");
            string known = Sim(3).PersonaKnowledge("今天有人给我发中奖短信", null);
            StringAssert.Contains("恭喜您中奖", known);
            StringAssert.DoesNotContain("床前明月光", known, "only what fits");
            Assert.AreEqual("", Sim(3).PersonaKnowledge("", null));
        }

        [Test]
        public void MeaningNeedsWordVectors()
        {
            var sim = Sim(3);
            sim.MemoryScorer = new HybridScorer { Vectors = new Vectors() };
            Assert.AreEqual("", sim.PersonaKnowledge("我被人坑了", null), "without 词向量 only shared words count");
            sim.S.unlocked.Add("wordvec");
            StringAssert.Contains("恭喜您中奖", sim.PersonaKnowledge("我被人坑了", null), "with 词向量 it recalls by meaning");
        }

        [Test]
        public void OldTalksNeedTheMemoryStageAndStayOutsideTheWindow()
        {
            var sim = Sim(5);
            sim.AddLine("me", "我养了只猫叫可乐");
            sim.AddLine("ai", "可乐好听");
            for (int i = 0; i < 6; i++) { sim.AddLine("me", "第" + i + "句"); sim.AddLine("ai", "嗯"); }
            StringAssert.Contains("可乐", sim.PersonaKnowledge("可乐最近怎么样", null, 4));
            StringAssert.DoesNotContain("我养了只猫", sim.PersonaKnowledge("可乐最近怎么样", null, -1), "no window given: no old talks");
            sim.S.stage = 4;
            StringAssert.DoesNotContain("我养了只猫", sim.PersonaKnowledge("可乐最近怎么样", null, 4));
        }
    }
}
