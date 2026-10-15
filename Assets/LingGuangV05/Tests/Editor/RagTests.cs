using System;
using System.Collections.Generic;
using LingGuangV05.Core.Chat;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>Knowledge files, the hybrid (BM25 + embedding) scorer and conversation recall (Core/Chat/Rag.cs).</summary>
    public sealed class RagTests
    {
        /// <summary>Hand-made 3-d "embeddings": texts mentioning an exam point one way, cats another, the rest a third.</summary>
        sealed class FakeVectors : IVectorSource
        {
            public readonly List<string> asked = new List<string>();
            static float[] Of(string t) =>
                t.Contains("期末") || t.Contains("概率论") || t.Contains("exam") ? new[] { 1f, .1f, 0f } :
                t.Contains("猫") || t.Contains("煤球") ? new[] { 0f, 1f, .1f } : new[] { .1f, 0f, 1f };
            public float[] Query(string text) => Of(text);
            public float[] Document(string text) { asked.Add(text); return Of(text); }
        }

        const string Json = "{\"entries\":[" +
            "{\"id\":\"exam\",\"topic\":\"学校\",\"zh\":\"你 11 月 2 日考概率论。\",\"en\":\"Your probability exam is on 2 November.\",\"from\":\"2016-09-01\",\"until\":\"2016-11-02\",\"importance\":5}," +
            "{\"id\":\"cat\",\"topic\":\"家庭\",\"zh\":\"你家的猫叫煤球。\",\"en\":\"\",\"from\":\"\",\"until\":\"\",\"importance\":2}," +
            "{\"id\":\"empty\",\"zh\":\"  \"}]}";

        [Test]
        public void ABookParsesAndKeepsEntriesToTheirDates()
        {
            var book = RagBook.Parse(Json);
            Assert.AreEqual(2, book.entries.Count, "an entry without Chinese text is skipped");
            Assert.AreEqual(3, book.entries[0].importance, "importance is clamped to 1–3");
            Assert.AreEqual("你家的猫叫煤球。", book.entries[1].Text(true), "no English falls back to Chinese");
            Assert.AreEqual(1, book.On(new DateTime(2016, 8, 31)).Count, "not yet");
            Assert.AreEqual(2, book.On(new DateTime(2016, 11, 2)).Count, "the last day still holds");
            Assert.AreEqual(1, book.On(new DateTime(2016, 11, 3)).Count, "over");
            Assert.AreEqual(0, RagBook.Parse("{not json").entries.Count);
            Assert.AreEqual(0, RagBook.Parse(null).entries.Count);
        }

        [Test]
        public void WithoutVectorsTheHybridScorerIsBm25()
        {
            var docs = new[] { "你 11 月 2 日考概率论。", "你家的猫叫煤球。" };
            var q = MemoryQuery.Of("概率论难不难");
            var prev = RagVectors.Source;
            RagVectors.Source = null;
            try { CollectionAssert.AreEqual(new Bm25Scorer().Score(q, docs), new HybridScorer().Score(q, docs)); }
            finally { RagVectors.Source = prev; }
        }

        [Test]
        public void MeaningFindsAFactWithoutSharedWords()
        {
            var book = RagBook.Parse(Json);
            var day = new DateTime(2016, 10, 20);
            var query = MemoryQuery.Of("期末准备得怎么样了");
            var words = Rag.Lookup(new HybridScorer { Semantic = false }, query, book, day, false, 3);
            Assert.AreEqual(0, words.Count, "no shared words: BM25 finds nothing");
            var meaning = Rag.Lookup(new HybridScorer { Vectors = new FakeVectors() }, query, book, day, false, 3);
            Assert.AreEqual(1, meaning.Count, "unrelated facts stay out");
            Assert.AreEqual("exam", meaning[0].id);
            Assert.AreEqual(0, Rag.Lookup(new HybridScorer { Vectors = new FakeVectors() }, query, book, new DateTime(2016, 11, 5), false, 3).Count, "a fact past its date is never found");
            var noise = MemoryQuery.Of("你 11 月去哪玩");
            Assert.AreEqual(1, Rag.Lookup(new HybridScorer { Semantic = false }, noise, book, day, false, 3).Count, "BM25 matches on 「11 月」");
            Assert.AreEqual(0, Rag.Lookup(new HybridScorer { Vectors = new FakeVectors() }, noise, book, day, false, 3).Count, "shared words but unrelated meaning");
        }

        [Test]
        public void EpisodesComeOutsideTheWindowWithoutOverlap()
        {
            var lines = new List<string> { "他：煤球最近胖了吗", "你：胖成球了", "他：吃饭没", "你：吃了", "他：煤球又抓沙发", "你：我妈要疯了", "他：在吗", "你：在" };
            var scorer = new HybridScorer { Vectors = new FakeVectors() };
            var picked = Rag.Episodes(scorer, MemoryQuery.Of("你家猫还好吗"), lines, 6, 2);
            Assert.AreEqual(2, picked.Count);
            Assert.AreEqual(0, picked[0], "the first cat exchange");
            Assert.That(picked[1] == 3 || picked[1] == 4, "the second cat exchange, in time order, not the window");
            for (int i = 1; i < picked.Count; i++) Assert.GreaterOrEqual(picked[i] - picked[i - 1], 2);
            Assert.AreEqual(0, Rag.Episodes(scorer, MemoryQuery.Of(""), lines, 6, 2).Count);
        }

        [Test]
        public void TheBlockIsEmptyWithoutLines()
        {
            Assert.AreEqual("", Rag.Block("x：", new List<string>()));
            Assert.AreEqual("x：\n- a\n- b\n", Rag.Block("x：", new[] { "a", "b" }));
            Assert.AreEqual(1, HybridScorer.Cosine(new[] { 1f, 2f }, new[] { 2f, 4f }), 1e-9);
            Assert.AreEqual(0, HybridScorer.Cosine(new[] { 1f }, new[] { 1f, 2f }));
        }
    }
}
