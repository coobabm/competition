using System.Collections.Generic;
using LingGuangV05.Core.Chat;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>The chat-history statistics behind the stage-3 cutscene in which the AI reads YY's msg.db.</summary>
    public sealed class ChatHistoryStatsTests
    {
        static ChatLine Me(string text, double t) => new ChatLine(true, text, t);
        static ChatLine Them(string text, double t) => new ChatLine(false, text, t);

        [Test]
        public void RecordCountIsBasePlusRealMessages()
        {
            Assert.AreEqual(12000, ChatHistoryStats.RecordCount(0));
            Assert.AreEqual(12408, ChatHistoryStats.RecordCount(408));
            Assert.AreEqual(12000, ChatHistoryStats.RecordCount(-5));
        }

        [Test]
        public void NormalizeTrimsPunctuationAndShortensLaughter()
        {
            Assert.AreEqual("在吗", ChatHistoryStats.Normalize("  在吗？？"));
            Assert.AreEqual("哈哈哈", ChatHistoryStats.Normalize("哈哈哈哈哈哈哈！"));
            Assert.AreEqual("哈哈", ChatHistoryStats.Normalize("哈哈"));
            Assert.AreEqual("hahaha", ChatHistoryStats.Normalize("HAHAHAHAHA"));
            Assert.AreEqual("hello", ChatHistoryStats.Normalize("Hello!"));
            Assert.AreEqual("", ChatHistoryStats.Normalize("。。。"));
            Assert.AreEqual("", ChatHistoryStats.Normalize(null));
        }

        [Test]
        public void PhrasesAreShortLinesInUnits()
        {
            Assert.IsTrue(ChatHistoryStats.IsPhrase("吃了没"));
            Assert.IsTrue(ChatHistoryStats.IsPhrase("are you there"));
            Assert.IsFalse(ChatHistoryStats.IsPhrase("你那台电脑别老开着电费贵"));
            Assert.IsFalse(ChatHistoryStats.IsPhrase(""));
            Assert.AreEqual(3, ChatHistoryStats.Units("ok 好的"));
        }

        [Test]
        public void PhrasesCountShortLinesWholeAndStockWordsInsideLongOnes()
        {
            var lines = new[] { "在吗", "在吗？", "哈哈哈哈", "今天显卡到了哈哈哈哈哈，卧槽真快", "晚安", null, "" };
            var phrases = ChatHistoryStats.Phrases(lines);
            Assert.AreEqual("在吗", phrases[0].phrase);
            Assert.AreEqual(2, phrases[0].count);
            Assert.AreEqual(2, phrases.Find(p => p.phrase == "哈哈哈").count);
            Assert.AreEqual(1, phrases.Find(p => p.phrase == "卧槽").count);
            Assert.AreEqual(1, phrases.Find(p => p.phrase == "晚安").count);
            Assert.IsNull(phrases.Find(p => p.phrase.Contains("显卡")));
        }

        [Test]
        public void EmptyHistoryFallsBackToTheStockWords()
        {
            var zh = ChatHistoryStats.Frequent(new string[0], false, 6);
            CollectionAssert.AreEquivalent(new[] { "在吗", "哈哈哈", "晚安", "卧槽", "吃了没" }, zh.ConvertAll(p => p.phrase));
            Assert.IsTrue(zh.TrueForAll(p => !p.fromHistory && p.count > 0));
            for (int i = 1; i < zh.Count; i++) Assert.GreaterOrEqual(zh[i - 1].count, zh[i].count);
            var en = ChatHistoryStats.Frequent(null, true, 6);
            CollectionAssert.Contains(en.ConvertAll(p => p.phrase), "u there?");
        }

        [Test]
        public void RealPhrasesGetRoomAndRealUseAddsToStockCounts()
        {
            var empty = ChatHistoryStats.Frequent(new string[0], false, 6);
            int baseCount = empty.Find(p => p.phrase == "在吗").count;
            var lines = new List<string> { "在吗", "在吗", "去网吧", "去网吧", "去网吧", "1080", "1080", "收到", "好" };
            var top = ChatHistoryStats.Frequent(lines, false, 6);
            Assert.AreEqual(6, top.Count);
            var zai = top.Find(p => p.phrase == "在吗");
            Assert.AreEqual(baseCount + 2, zai.count);
            Assert.IsTrue(zai.fromHistory);
            // The three most used own phrases, then only those used twice or more (好 is left out).
            var own = top.FindAll(p => p.fromHistory && p.phrase != "在吗");
            Assert.AreEqual(3, own.Count);
            Assert.IsNull(top.Find(p => p.phrase == "好"));
            CollectionAssert.Contains(own.ConvertAll(p => p.phrase), "去网吧");
            CollectionAssert.Contains(own.ConvertAll(p => p.phrase), "1080");
            Assert.AreEqual(3, top.Find(p => p.phrase == "去网吧").count);
        }

        [Test]
        public void OnlyTheTopThreeSingleUsePhrasesAreShown()
        {
            var top = ChatHistoryStats.Frequent(new[] { "一", "二", "三", "四", "五" }, false, 8);
            Assert.AreEqual(3, top.FindAll(p => p.fromHistory).Count);
            Assert.AreEqual(8, top.Count);
        }

        [Test]
        public void FavoriteOpenerCountsLinesAfterSilenceOnly()
        {
            var cousin = new List<ChatLine> { Me("在吗", 0), Me("借我点钱", 10), Them("干嘛", 20), Me("在吗？", 5000), Me("在吗", 5010) };
            var group = new List<ChatLine> { Them("[阿杰] 开黑", 0), Me("来了", 30), Me("睡了吗", 9000) };
            var laoZhou = new List<ChatLine> { Me("睡了吗", 100) };
            // 在吗 opens twice (0 and 5000; 5010 follows too soon), 睡了吗 twice; 在吗 was used first.
            Assert.AreEqual("在吗", ChatHistoryStats.FavoriteOpener(new[] { cousin, group, laoZhou }, false));
            laoZhou.Add(Me("睡了吗", 20000));
            Assert.AreEqual("睡了吗", ChatHistoryStats.FavoriteOpener(new[] { cousin, group, laoZhou }, false));
        }

        [Test]
        public void LongOpenersAreIgnoredAndTheDefaultIsInTheRightLanguage()
        {
            var conv = new List<ChatLine> { Me("你那台电脑别老开着，电费很贵的知道吗", 0) };
            Assert.AreEqual("在吗", ChatHistoryStats.FavoriteOpener(new[] { conv }, false));
            Assert.AreEqual("u there?", ChatHistoryStats.FavoriteOpener(null, true));
            Assert.AreEqual("hey", ChatHistoryStats.FavoriteOpener(new[] { new List<ChatLine> { Me("Hey!", 0) } }, true));
        }
    }
}
