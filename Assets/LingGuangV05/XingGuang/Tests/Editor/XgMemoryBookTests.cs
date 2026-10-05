using System;
using System.Collections.Generic;
using LingGuangV05.Core.Chat;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The AI's memory book: BM25 recall, mem0-style operations, eviction, migration, saves, the offline rule and the prompt.</summary>
    public sealed class XgMemoryBookTests
    {
        static XgSim Lab(int stage = 5)
        {
            var sim = new XgSim { GoldChance = 0 };
            sim.Profile = new XgProfile { name = "小灯", self = "本机", callMe = "老大", personality = "嘴硬心软" };
            sim.S.stage = stage;
            sim.Today = 20160603;
            return sim;
        }

        static XgMemoryOp Add(string text, string subject = "主人", int importance = 1) => new XgMemoryOp { kind = XgMemoryOpKind.Add, text = text, subject = subject, importance = importance };

        static List<int> Rank(string latest, IList<string> docs, string previous = null) =>
            MemoryRanker.Rank(new Bm25Scorer(), MemoryQuery.Of(latest, previous), docs, 5);

        // ───────────── BM25 ─────────────

        [Test]
        public void ChineseBigramsFindTheExamAndNothingElse()
        {
            var docs = new[] { "主人喜欢喝可乐", "主人下周三考概率论", "晴雯想要YSL星辰口红" };
            CollectionAssert.AreEqual(new[] { 1 }, Rank("概率论考试准备得怎么样", docs));
            CollectionAssert.AreEqual(new[] { 2 }, Rank("星辰口红买到了", docs));
        }

        [Test]
        public void EnglishWordsMatchCaseInsensitively()
        {
            var docs = new[] { "owner has an exam on Friday", "Qingwen wants the YSL Star lipstick", "owner likes cola" };
            CollectionAssert.AreEqual(new[] { 0 }, Rank("Do you remember my EXAM?", docs));
            CollectionAssert.AreEqual(new[] { 1 }, Rank("I bought the lipstick for her", docs));
        }

        [Test]
        public void ThresholdKeepsIrrelevantNotesOut()
        {
            var docs = new[] { "主人喜欢喝可乐", "主人下周三考概率论", "老周下个月盘网吧" };
            Assert.IsEmpty(Rank("你好吗", docs), "function characters alone match nothing");
            Assert.IsEmpty(Rank("今天天气不错", docs));
            Assert.IsEmpty(Rank("在吗", docs));
            Assert.IsEmpty(Rank("", docs));
            Assert.IsEmpty(Rank("主人", docs), "the owner's title is in every note and means nothing");
        }

        [Test]
        public void TokensAreUnigramsBigramsWordsAndNumbers()
        {
            var tokens = new List<string>();
            foreach (var t in MemoryTokens.Tokenize("我的GTX970显卡11月3号")) tokens.Add(t.Key);
            CollectionAssert.Contains(tokens, "gtx");
            CollectionAssert.Contains(tokens, "970");
            CollectionAssert.Contains(tokens, "显卡");
            CollectionAssert.Contains(tokens, "11");
            CollectionAssert.DoesNotContain(tokens, "我", "stop characters are not unigrams");
            CollectionAssert.DoesNotContain(tokens, "我的", "a bigram of two stop characters is dropped");
        }

        [Test]
        public void ThePreviousLineHelpsAFollowUp()
        {
            var docs = new[] { "主人喜欢喝可乐", "主人下周三考概率论" };
            Assert.IsEmpty(Rank("改到哪天了", docs));
            CollectionAssert.AreEqual(new[] { 1 }, Rank("改到哪天了", docs, "概率论考试推迟了"));
        }

        [Test]
        public void ImportanceAndRecencyReorderButNeverPullIn()
        {
            var docs = new[] { "主人周五考英语", "主人周五考数学", "主人喜欢喝可乐" };
            var ranked = MemoryRanker.Rank(new Bm25Scorer(), MemoryQuery.Of("周五考试紧张"), docs, 5, new[] { 1, 3, 3 }, new[] { 0.0, 0.0, 0.0 });
            CollectionAssert.AreEqual(new[] { 1, 0 }, ranked, "importance first among matches; the important but unrelated note stays out");
            ranked = MemoryRanker.Rank(new Bm25Scorer(), MemoryQuery.Of("周五考试紧张"), docs, 5, new[] { 1, 1, 1 }, new[] { 0.0, 60.0, 0.0 });
            Assert.AreEqual(0, ranked[0], "the recent one wins a tie");
        }

        // ───────────── operations ─────────────

        [Test]
        public void AddUpdateDeleteNone()
        {
            var sim = Lab();
            var c = sim.ApplyMemoryOps(new[] { Add("主人下周三考概率论", "主人", 3), new XgMemoryOp { kind = XgMemoryOpKind.None } });
            Assert.AreEqual(1, c.added.Count);
            var exam = sim.MemoryBook[0];
            Assert.AreEqual(3, exam.importance);
            Assert.AreEqual(20160603, exam.createdDay);

            sim.Today = 20160610;
            c = sim.ApplyMemoryOps(new[] { new XgMemoryOp { kind = XgMemoryOpKind.Update, id = exam.id, text = "主人下周五考概率论", subject = "主人", importance = 3 } }, new[] { exam.id });
            Assert.AreEqual(1, c.updated.Count);
            Assert.AreEqual("主人下周五考概率论", exam.text);
            Assert.AreEqual(20160610, exam.createdDay, "a rewritten note carries its new date");

            c = sim.ApplyMemoryOps(new[] { new XgMemoryOp { kind = XgMemoryOpKind.Delete, id = exam.id } }, new int[0]);
            Assert.AreEqual(0, c.deleted.Count, "only notes the extractor was shown can be deleted");
            c = sim.ApplyMemoryOps(new[] { new XgMemoryOp { kind = XgMemoryOpKind.Delete, id = exam.id } }, new[] { exam.id });
            Assert.AreEqual(1, c.deleted.Count);
            Assert.IsEmpty(sim.MemoryBook);
            c = sim.ApplyMemoryOps(new[] { new XgMemoryOp { kind = XgMemoryOpKind.Update, id = 999, text = "不存在" } });
            Assert.IsFalse(c.Any);
        }

        [Test]
        public void AtMostTwoOpsCleanTextAndNoNonFacts()
        {
            var sim = Lab();
            int revision = sim.S.memoryRevision;
            var c = sim.ApplyMemoryOps(new[] { Add("「主人喜欢喝可乐。」"), Add("无值得记录的内容"), Add("老周下个月盘网吧", "老周"), Add("晴雯想要星辰口红", "Qingwen") });
            Assert.AreEqual(2, c.added.Count, "the non-fact is skipped and does not use a slot; the fourth is over the limit");
            Assert.AreEqual("主人喜欢喝可乐", sim.MemoryBook[0].text);
            Assert.AreEqual("老周", sim.MemoryBook[1].subject);
            Assert.Greater(sim.S.memoryRevision, revision);
            c = sim.ApplyMemoryOps(new[] { Add(new string('长', 50), "外星人") });
            Assert.AreEqual(XgSim.MemoryTextLimit, c.added[0].text.Length);
            Assert.AreEqual("主人", c.added[0].subject, "an unknown subject falls back to the owner");
        }

        [Test]
        public void AddingTheSameThingTwiceRefreshesIt()
        {
            var sim = Lab();
            sim.ApplyMemoryOps(new[] { Add("主人下周三考概率论", "主人", 2) });
            var c = sim.ApplyMemoryOps(new[] { Add("主人下周三考概率论", "主人", 3) });
            Assert.AreEqual(0, c.added.Count);
            Assert.AreEqual(1, c.updated.Count);
            Assert.AreEqual(1, sim.MemoryBook.Count);
            Assert.AreEqual(3, sim.MemoryBook[0].importance);
            sim.ApplyMemoryOps(new[] { Add("小灯答应主人不再半夜叫醒他", "它自己", 1) });
            Assert.AreEqual(2, sim.MemoryBook[1].importance, "a promise is at least important");
        }

        [Test]
        public void TheBookIsCappedByImportanceAndRecency()
        {
            var sim = Lab();
            sim.Today = 20160601;
            sim.ApplyMemoryOps(new[] { Add("主人的生日是十一月三号", "主人", 3) });
            int birthday = sim.MemoryBook[0].id;
            for (int i = 0; i < XgSim.MemoryBookLimit + 10; i++)
                sim.S.memoryBook.Add(new XgMemoryEntry { id = sim.S.nextMemoryId++, text = "零碎小事 " + i, subject = "世界", importance = 1, createdDay = 20160601 + i / 30 * 100 + i % 30 });
            sim.Today = 20161201;
            sim.ApplyMemoryOps(new[] { Add("主人十二月要考六级", "主人", 2) });
            Assert.AreEqual(XgSim.MemoryBookLimit, sim.MemoryBook.Count);
            Assert.IsNotNull(sim.MemoryEntry(birthday), "the old but very important note survives");
            Assert.IsNull(sim.MemoryEntry(birthday + 1), "the oldest ordinary note went first");
        }

        // ───────────── migration and saves ─────────────

        [Test]
        public void OldNotesMoveIntoTheBookOnce()
        {
            var state = new XgState { stage = 5, memory = new List<string> { "你说过：我下周三考概率论", "你说过：我下周三考概率论", "你说过：显卡又涨价了" } };
            var sim = new XgSim(state);
            Assert.AreEqual(2, sim.MemoryBook.Count, "duplicates merge");
            Assert.AreEqual("主人说：我下周三考概率论", sim.MemoryBook[0].text);
            Assert.AreEqual("主人", sim.MemoryBook[0].subject);
            Assert.IsEmpty(state.memory);
            Assert.IsTrue(state.memoryMigrated);
            Assert.AreEqual(0, sim.MemoryBook[0].createdDay, "undated until the calendar is known");
            sim.Today = 20160705;
            sim.Recall("概率论");
            Assert.AreEqual(20160705, sim.MemoryBook[0].createdDay);
            state.memory.Add("你说过：又一条");
            var again = new XgSim(state);
            Assert.AreEqual(2, again.MemoryBook.Count, "migration runs once");
        }

        [Test]
        public void BookSurvivesAJsonRoundTripAndOldSavesLoad()
        {
            var sim = Lab();
            sim.ApplyMemoryOps(new[] { Add("主人下周三考概率论", "主人", 3), Add("晴雯想要星辰口红", "晴雯", 2) });
            sim.Recall("概率论好难");
#if UNITY_5_3_OR_NEWER
            var restored = new XgSim(UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(sim.S)));
#else
            var options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
            string json = System.Text.Json.JsonSerializer.Serialize(sim.S, options);
            var restored = new XgSim(System.Text.Json.JsonSerializer.Deserialize<XgState>(json, options));
#endif
            Assert.AreEqual(2, restored.MemoryBook.Count);
            Assert.AreEqual("晴雯", restored.MemoryBook[1].subject);
            Assert.AreEqual(1, restored.MemoryBook[0].uses);
            Assert.AreEqual(sim.S.nextMemoryId, restored.S.nextMemoryId);
            CollectionAssert.AreEqual(sim.S.memoryRecalled, restored.S.memoryRecalled);

            // A save from before the book: no fields at all (JsonUtility leaves such lists null).
            var old = new XgState { stage = 5, memory = new List<string> { "你说过：我喜欢喝可乐" } };
            old.memoryBook = null; old.memoryRecalled = null; old.memoryRecallQuery = null; old.nextMemoryId = 0;
            var loaded = new XgSim(old);
            Assert.AreEqual(1, loaded.MemoryBook.Count);
            Assert.AreEqual(1, loaded.MemoryBook[0].id);
            Assert.AreEqual(2, loaded.S.nextMemoryId);
            Assert.IsNotNull(loaded.S.memoryRecalled);
        }

        // ───────────── offline rule ─────────────

        [Test]
        public void OfflineRuleKeepsOnlyCuedStatements()
        {
            var sim = Lab(4);
            Assert.IsFalse(sim.RememberOffline("明天我要去省城面试").Any, "no long memory before stage 5");
            sim.S.stage = 5;
            Assert.IsFalse(sim.RememberOffline("哈哈哈哈哈哈哈哈").Any, "no cue");
            Assert.IsFalse(sim.RememberOffline("你会不会说话呀？").Any, "a question to it");
            Assert.IsFalse(sim.RememberOffline("想睡").Any, "too short");
            var c = sim.RememberOffline("明天我要去省城面试");
            Assert.AreEqual(1, c.added.Count);
            Assert.AreEqual("主人说：明天我要去省城面试", c.added[0].text);
            Assert.AreEqual(2, c.added[0].importance, "an interview is a strong cue");
            Assert.AreEqual(1, sim.RememberOffline("我最近特别喜欢听周杰伦").added[0].importance);
            Assert.IsFalse(sim.RememberOffline("明天我要去省城面试").added.Count > 0, "the same line again only refreshes");
        }

        [Test]
        public void TalkingNoLongerWritesEveryLine()
        {
            var sim = Lab();
            for (int i = 0; i < 5; i++) sim.AddLine("me", "第 " + i + " 件事：显卡又涨价了");
            Assert.IsEmpty(sim.MemoryBook, "writing happens after the answer, through the extractor or the offline rule");
            Assert.IsEmpty(sim.S.memory);
        }

        // ───────────── prompt ─────────────

        [Test]
        public void PersonaPromptInjectsOnlyRelevantMemories()
        {
            var sim = Lab();
            sim.ApplyMemoryOps(new[] { Add("主人下周三考概率论", "主人", 3), Add("主人喜欢喝可乐") });
            sim.ApplyMemoryOps(new[] { Add("老周下个月盘网吧", "老周") });
            sim.AddLine("me", "概率论好难啊");
            string p = sim.PersonaPrompt(6, new string[0]);
            StringAssert.Contains("你记得：主人下周三考概率论。", p);
            StringAssert.DoesNotContain("可乐", p);
            StringAssert.DoesNotContain("网吧", p);
            Assert.AreEqual(1, sim.RecalledMemories().Count);
            Assert.AreEqual(1, sim.MemoryBook[0].uses);
            sim.PersonaPrompt(6, new string[0]);
            Assert.AreEqual(1, sim.MemoryBook[0].uses, "a retry with the same question counts once");

            sim.AddLine("me", "今天天气不错");
            sim.AddLine("me", "在吗");
            p = sim.PersonaPrompt(6, new string[0]);
            StringAssert.DoesNotContain("你记得", p, "nothing relevant, nothing injected");
            Assert.IsEmpty(sim.RecalledMemories());

            sim.S.stage = 4;
            sim.AddLine("me", "概率论好难啊");
            StringAssert.DoesNotContain("你记得", sim.PersonaPrompt(6, new string[0]), "no long memory before stage 5");
        }

        [Test]
        public void AnOutsideQueryDoesNotMoveThePanel()
        {
            var sim = Lab();
            sim.ApplyMemoryOps(new[] { Add("晴雯想要星辰口红", "晴雯") });
            StringAssert.Contains("星辰口红", sim.PersonaPrompt(6, new string[0], "你记得我想要的星辰口红吗"));
            Assert.IsEmpty(sim.RecalledMemories());
            StringAssert.Contains("星辰口红", sim.MemoryPromptFor("星辰口红到了", null));
            Assert.IsEmpty(sim.RecalledMemories());
            Assert.AreEqual(2, sim.MemoryBook[0].uses);
        }

        // ───────────── the extractor ─────────────

        [Test]
        public void ExtractionPromptShowsSimilarNotesAndParsesTheAnswer()
        {
            var sim = Lab();
            sim.ApplyMemoryOps(new[] { Add("主人下周三考概率论", "主人", 3), Add("主人喜欢喝可乐") });
            var messages = sim.MemoryExtractionMessages("概率论考试改到下周五了", "那你多了两天。", out var shown);
            CollectionAssert.AreEqual(new[] { sim.MemoryBook[0].id }, shown);
            StringAssert.Contains("[" + shown[0] + "] 主人下周三考概率论", messages[0].Value);
            StringAssert.DoesNotContain("可乐", messages[0].Value);
            Assert.AreEqual("主人：概率论考试改到下周五了\n小灯：那你多了两天。", messages[1].Value);

            string reply = "{\"ops\":[{\"op\":\"UPDATE\",\"id\":\"" + shown[0] + "\",\"text\":\"主人下周五考概率论\",\"subject\":\"主人\",\"importance\":3}]}";
            var c = sim.RememberFromModel(reply, shown);
            Assert.AreEqual(1, c.updated.Count);
            Assert.AreEqual("主人下周五考概率论", sim.MemoryBook[0].text);

            c = sim.RememberFromModel("{\"ops\":[{\"op\":\"DELETE\",\"id\":\"" + sim.MemoryBook[1].id + "\",\"text\":\"\",\"subject\":\"主人\",\"importance\":1}]}", shown);
            Assert.IsFalse(c.Any, "the cola note was not shown, so it cannot be deleted");
            c = sim.RememberFromModel("{\"ops\":[{\"op\":\"NONE\",\"id\":\"\",\"text\":\"无值得记录的内容。\",\"subject\":\"主人\",\"importance\":1}]}", shown);
            Assert.IsFalse(c.Any);
        }

        [Test]
        public void ParserIsTolerant()
        {
            var ops = XgSim.ParseMemoryOps("好的：{\"ops\":[{\"op\":\"add\",\"text\":\"主人说\\\"好\\\"\\u4e86\",\"subject\":\"owner\",\"importance\":2},{\"op\":\"DELETE\",\"id\":7}]} 以上");
            Assert.AreEqual(2, ops.Count);
            Assert.AreEqual(XgMemoryOpKind.Add, ops[0].kind);
            Assert.AreEqual("主人说\"好\"了", ops[0].text);
            Assert.AreEqual("主人", XgSim.NormalSubject(ops[0].subject));
            Assert.AreEqual(2, ops[0].importance);
            Assert.AreEqual(7, ops[1].id);
            Assert.IsEmpty(XgSim.ParseMemoryOps("not json"));
            Assert.IsEmpty(XgSim.ParseMemoryOps(null));
        }

        [Test]
        public void GrammarBoundsTheAnswer()
        {
            string g = XgSim.MemoryGrammar(false);
            StringAssert.Contains("root ::= \"{\\\"ops\\\":[\" op (\",\" op)? \"]}\"", g);
            StringAssert.Contains("kind ::= \"ADD\" | \"UPDATE\" | \"DELETE\" | \"NONE\"", g);
            StringAssert.Contains("\"它自己\"", g);
            StringAssert.Contains("\"itself\"", XgSim.MemoryGrammar(true));
        }

        [Test]
        public void NotebookIsDatedPlainText()
        {
            var sim = Lab();
            sim.ApplyMemoryOps(new[] { Add("主人下周三考概率论", "主人", 3) });
            sim.Today = 20160720;
            sim.ApplyMemoryOps(new[] { Add("晴雯想要星辰口红", "晴雯") });
            string text = sim.NotebookText();
            StringAssert.StartsWith("小灯的笔记（共 2 条", text);
            StringAssert.Contains("2016-06-03  [主人] 主人下周三考概率论  ★★", text);
            StringAssert.Contains("2016-07-20  [晴雯] 晴雯想要星辰口红\n", text);
            Assert.Less(text.IndexOf("概率论", StringComparison.Ordinal), text.IndexOf("口红", StringComparison.Ordinal), "oldest first");
            Assert.AreEqual("小灯的笔记.txt", sim.NotebookFileName());
            sim.Profile.name = "a/b:c";
            Assert.AreEqual("a_b_c的笔记.txt", sim.NotebookFileName());
            sim.English = true;
            StringAssert.Contains("[Qingwen]", sim.NotebookText());
        }
    }
}
