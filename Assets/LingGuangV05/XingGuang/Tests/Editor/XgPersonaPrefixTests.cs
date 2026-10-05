using System.Collections.Generic;
using LingGuangV05.XingGuang;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The persona prompt splits into a cached part (core + board) and a per-turn part (tone, memories).</summary>
    public sealed class XgPersonaPrefixTests
    {
        static XgSim Persona()
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.Profile = new XgProfile { name = "小灯", self = "本机", callMe = "老大", personality = "嘴硬心软", warmth = 70, play = 70, opinion = 70, words = new List<string> { "爱吐槽" } };
            sim.SeedPersonality();
            return sim;
        }

        [Test]
        public void ThePromptIsCoreThenBoardThenMemories()
        {
            var sim = Persona();
            string core = sim.PersonaCore(), board = sim.PersonaBoard(9, new[] { "SSR" });
            StringAssert.Contains("小灯", core);
            StringAssert.DoesNotContain("2016 年9月", core, "the month lives in the board part");
            StringAssert.Contains("2016 年9月", board);
            StringAssert.Contains("温度", sim.PersonaTone(), "tone moves with the per-turn state");
            Assert.AreEqual(core + board + sim.PersonaTone() + sim.PersonaMemory(), sim.PersonaPrompt(9, new[] { "SSR" }));
        }

        [Test]
        public void TheCachedPartIsStableBetweenTurns()
        {
            var sim = Persona();
            sim.S.stage = 6;
            string before = sim.PersonaCore() + sim.PersonaBoard(9, new string[0]);
            sim.AddLine("me", "你还记得我小时候的狗吗");
            sim.AddLine("ai", "记得，叫旺财。");
            Assert.AreEqual(before, sim.PersonaCore() + sim.PersonaBoard(9, new string[0]), "chatting does not change the cached part");
            Assert.IsFalse(sim.StageRuleVaries);
            sim.S.stage = 2;
            Assert.IsTrue(sim.StageRuleVaries, "stage 2 lists options read from the question");
        }
    }
}
