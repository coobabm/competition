using System.Collections.Generic;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>Design v1.1 §6 prologue: fixed facts, the opening setup, and the rule that nothing is saved before it.</summary>
    public sealed class PrologueTests
    {
        static PrologueLines Lines()
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), TestContext.CurrentContext.TestDirectory })
                for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                {
                    string file = Path.Combine(dir.FullName, "Assets", "LingGuangV05", "Resources", "LingGuangV05", "Prologue", "prologue_lines.json");
                    if (File.Exists(file)) return PrologueLines.Parse(File.ReadAllText(file));
                }
            Assert.Fail("prologue_lines.json not found.");
            return null;
        }

        [Test]
        public void TheLongNumberSpellsTheLineTheSiQuotes()
        {
            Assert.AreEqual("haineifengzhiji", Prologue.Decode(Prologue.LongNumber), "海内逢知己, for the ending to decode");
            Assert.IsNull(Prologue.Decode("1234"));
            Assert.Less(Prologue.SophonBytes(1, 0), Prologue.SophonBytes(2, 0));
            Assert.Less(Prologue.SophonBytes(2, 0), Prologue.SophonBytes(2, 10000), "sophon.dll keeps growing");
        }

        [Test]
        public void EveryPrologueLineExistsInBothLanguagesAndIn2016()
        {
            var lines = Lines();
            foreach (var key in Prologue.Keys) Assert.IsTrue(lines.Has(key), key);
            var all = new List<string>();
            foreach (var key in lines.Keys) { all.Add(lines.Get(key, false)); all.Add(lines.Get(key, true)); }
            foreach (var h in lines.History) { Assert.Less(h.days, 0); all.Add(h.zh); }
            Assert.GreaterOrEqual(lines.History.Count, 6, "a few weeks of chat with 老周 since April");
            Assert.IsEmpty(EraLexicon.Audit(all));
            StringAssert.Contains("关机", lines.Get("dialog_shutdown", false));
            StringAssert.Contains("关机", lines.Get("m_grab", false), "the word in the thought is what triggers the rule");
            StringAssert.Contains("逢", lines.Get("txt_poem", false), "the SI writes 逢, not 存");
        }

        [Test]
        public void TheSentenceBecomesAxisTargetsAndToneWords()
        {
            var p = PrologueProfile.Read("嘴硬心软，爱吐槽");
            Assert.Greater(p.opinion, 50, "嘴硬");
            Assert.Greater(p.warmth, 50, "心软");
            Assert.Greater(p.play, 50, "吐槽");
            CollectionAssert.Contains(p.words, "爱吐槽");
            Assert.LessOrEqual(p.words.Count, PrologueProfile.MaxWords);

            var calm = PrologueProfile.Read("冷静理性，一本正经，很听话");
            Assert.Less(calm.warmth, 50); Assert.Less(calm.play, 50); Assert.Less(calm.opinion, 50);
            var plain = PrologueProfile.Read("就普通吧");
            Assert.AreEqual(50, plain.warmth); Assert.AreEqual(50, plain.play); Assert.AreEqual(50, plain.opinion);
            Assert.IsEmpty(PrologueProfile.Read("可爱的猫").words, "可爱 is not a tone word");
        }

        [Test]
        public void TheModelAnswerIsUsedWhenItParsesAndClamped()
        {
            var p = PrologueProfile.ParseModel("好的：{\"温度\": 72, \"玩心\": 130, \"主见\": 64, \"语气词\": [\"爱吐槽\", \"嘴硬\", \"第三个\"]}", "嘴硬心软，爱吐槽");
            Assert.IsNotNull(p);
            // Where the sentence clearly leans (心软 吐槽 嘴硬 each read as 70) the two readings are averaged; 130 is clamped first.
            Assert.AreEqual(71, p.warmth); Assert.AreEqual(85, p.play); Assert.AreEqual(67, p.opinion);
            CollectionAssert.AreEqual(new[] { "爱吐槽", "嘴硬" }, p.words);
            var misread = PrologueProfile.ParseModel("{\"温度\":35,\"玩心\":85,\"主见\":20,\"语气词\":[\"嘛\",\"呗\"]}", "嘴硬心软，爱吐槽");
            Assert.Greater(misread.opinion, 40, "a model that reads 嘴硬 as compliant is pulled back towards the sentence");
            CollectionAssert.AreEqual(new[] { "爱吐槽", "嘴硬" }, misread.words, "tone words come from the player's sentence, not 嘛 or 呗");
            Assert.AreEqual(10, PrologueProfile.ParseModel("{\"warmth\":10,\"play\":20,\"opinion\":30,\"words\":[]}", "x").warmth, "no keywords: the model decides");
            Assert.IsNull(PrologueProfile.ParseModel("我觉得他很温柔", "x"), "no numbers: the offline reading is used");
        }

        [Test]
        public void TheSetupIsValidatedAndEndsThePrologue()
        {
            var sim = new ChapterOneSim();
            Assert.IsTrue(sim.InPrologue, "a new game starts in the prologue");
            Assert.IsFalse(sim.AppInstalled);
            var signals = new List<string>();
            sim.Signal += (n, a) => signals.Add(n);

            var bad = PrologueProfile.Read("嘴硬心软");
            bad.name = ""; bad.self = "我"; bad.callMe = "你";
            Assert.IsNotNull(PrologueProfile.Problem(bad));
            Assert.IsFalse(sim.CompletePrologue(bad));
            Assert.IsTrue(sim.InPrologue);
            bad.name = "<b>x</b>";
            Assert.IsNotNull(PrologueProfile.Problem(bad), "no format tags in the name");

            var p = PrologueProfile.Read("嘴硬心软，爱吐槽");
            p.name = "小灯"; p.self = "本机"; p.callMe = "老大";
            Assert.IsNull(PrologueProfile.Problem(p));
            Assert.IsTrue(sim.CompletePrologue(p));
            Assert.IsFalse(sim.InPrologue);
            Assert.IsTrue(sim.AppInstalled);
            Assert.AreEqual("小灯", sim.S.aiName); Assert.AreEqual("本机", sim.S.aiSelf); Assert.AreEqual("老大", sim.S.aiCallMe);
            Assert.AreEqual("嘴硬心软，爱吐槽", sim.S.personality);
            Assert.AreEqual(p.opinion, sim.S.targetOpinion);
            CollectionAssert.AreEqual(p.words, sim.S.toneWords);
            CollectionAssert.Contains(signals, "prologue.done");
            Assert.IsFalse(sim.CompletePrologue(p), "only once");
            Assert.AreEqual(0, new GameState().prologue, "older saves load as done");
        }
    }
}
