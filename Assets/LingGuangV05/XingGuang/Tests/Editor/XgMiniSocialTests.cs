using System.Collections.Generic;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgMiniSocialTests
    {
        [Test] public void PoetryRequiresKnownLineThemeAndNoRepeat()
        {
            var used = new HashSet<string>();
            Assert.IsTrue(XgMiniSocialRules.PoemValid("床前明月光", "月", used));
            Assert.IsFalse(XgMiniSocialRules.PoemValid("床前明月灯", "月", used));
            Assert.IsFalse(XgMiniSocialRules.PoemValid("床前明月光", "花", used));
            used.Add("床前明月光");
            Assert.IsFalse(XgMiniSocialRules.PoemValid("床前明月光", "月", used));
        }
        [Test] public void CaptchaRequiresExactNonEmptySelection()
        {
            Assert.IsTrue(XgMiniSocialRules.ExactMask(5, 5, 8));
            Assert.IsFalse(XgMiniSocialRules.ExactMask(1, 5, 8));
            Assert.IsFalse(XgMiniSocialRules.ExactMask(7, 5, 8));
            Assert.IsFalse(XgMiniSocialRules.ExactMask(0, 0, 8));
            Assert.IsFalse(XgMiniSocialRules.ExactMask(257, 257, 8));
        }
        [TestCase(0,0)] [TestCase(3,0)] [TestCase(4,4)] [TestCase(7,4)] [TestCase(8,8)] [TestCase(11,8)] [TestCase(12,12)]
        public void QuizSafetyPoints(int correct, int expected) => Assert.AreEqual(expected, XgMiniSocialRules.SafeLevel(correct));
        [Test] public void TranslationDoesNotRewardUntouchedErrorsOrDamagedCorrectTokens()
        {
            XgMiniSocialRules.RepairScore(new[]{"我","吃","火锅"}, new[]{"我","喝","火锅"}, new[]{"他","吃","火锅"}, out int fixedCount, out int missed, out int damaged);
            Assert.AreEqual(1, fixedCount); Assert.AreEqual(0, missed); Assert.AreEqual(1, damaged);
            XgMiniSocialRules.RepairScore(new[]{"我","吃"}, new[]{"我","喝"}, new[]{"我","喝"}, out fixedCount, out missed, out damaged);
            Assert.AreEqual(0,fixedCount); Assert.AreEqual(1,missed); Assert.AreEqual(0,damaged);
        }
        [Test] public void QuizBankHasEnoughDistinctQuestionsForTwelveAndSwap()
        {
            var seen = new HashSet<string>();
            Assert.GreaterOrEqual(XgMiniSocialRules.Quiz.Length, 16);
            foreach(var q in XgMiniSocialRules.Quiz) { Assert.IsTrue(seen.Add(q[0])); Assert.AreEqual(5,q.Length); Assert.AreEqual(4,new HashSet<string>(new[]{q[1],q[2],q[3],q[4]}).Count); }
        }
    }
}
