using LingGuangV05.Core;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgEndingTextGuardTests
    {
        [Test] public void SentencesDoNotInheritNameLengthOrFilenameRules()
        {
            const string sentence = "This is a sentence longer than sixteen characters: a/b [3] < 5? It can end with .exe";
            Assert.That(XgEndingTextGuard.Filter(sentence, true), Is.EqualTo(sentence));
            Assert.That(XgEndingTextGuard.Filter("先看说明，再决定。\n这句话超过名字长度，但应完整保留。", false), Does.Contain("\n"));
        }
        [Test] public void DynamicTextNormalizesControlsAndRealFormattingTags()
        {
            Assert.That(XgEndingTextGuard.Filter("ＡＢＣ\r\n<b>正常</b>\t\u202e字\0", false), Is.EqualTo("ABC\n正常 字"));
            Assert.That(XgEndingTextGuard.Filter("<color=#ff0000>原句</color><br>下一行", false), Is.EqualTo("原句\n下一行"));
            Assert.That(XgEndingTextGuard.Filter("a < b && c > d", true), Is.EqualTo("a < b && c > d"));
        }
        [Test] public void SameAbuseBaselineIsReusedWithoutSilentlyRewritingIt()
        {
            Assert.That(AiNamePolicy.ContainsAbusiveLanguage("傻，逼"), Is.True);
            Assert.That(XgEndingTextGuard.Filter("傻\u200b逼", false), Is.Null);
            Assert.That(XgEndingTextGuard.Filter("ＦＵＣＫ", true), Is.Null);
            Assert.That(XgEndingTextGuard.Filter("a class assignment", true), Is.EqualTo("a class assignment"));
            // Dynamic text deliberately uses NFKC: width changes, but the false claim is preserved.
            Assert.That(XgEndingTextGuard.Filter("你算错了，二加二等于五。", false), Is.EqualTo("你算错了,二加二等于五。"));
        }
        [Test] public void DynamicEraScrubDoesNotTouchReviewedHistoryOrFalseTrialClaims()
        {
            Assert.That(XgEndingTextGuard.Filter("训练好了，真香", false), Is.EqualTo("训练好了,"));
            const string reviewed = "2017 年出现的说法：扎心了老铁。\r\n今年欧洲杯，东道主法国拿了冠军。 a < b";
            Assert.That(XgEndingTextGuard.FilterReviewed(reviewed, false), Is.EqualTo(reviewed));
        }
        [Test] public void InvalidUnicodeAndLengthAreRejectedRatherThanTruncated()
        {
            Assert.That(XgEndingTextGuard.Filter("\ud800", false), Is.Null);
            Assert.That(XgEndingTextGuard.Filter("\udc00", false), Is.Null);
            Assert.That(XgEndingTextGuard.Filter(new string('a', 4097), true), Is.Null);
            Assert.That(XgEndingTextGuard.Filter("🙂", false), Is.EqualTo("🙂"));
            Assert.That(XgEndingTextGuard.Filter(null, false), Is.EqualTo(""));
        }
        [TestCase("Call 13800138000")]
        [TestCase("１３８００１３８０００")]
        [TestCase("138\u200b00138000")]
        [TestCase("QQ: 12345678")]
        [TestCase("扣扣号：12345678")]
        [TestCase("user+tag@example.com")]
        [TestCase("https://example.com/path")]
        [TestCase("www.example.com")]
        [TestCase("example.net")]
        [TestCase("<link=https://example.com>label</link>")]
        [TestCase("138真香00138000")]
        public void DynamicContactDetailsAreRejected(string text)
        {
            Assert.That(XgEndingTextGuard.Filter(text, false), Is.Null);
        }

        [TestCase("http://yann.lecun.com/exdb/mnist/")]
        [TestCase("https://YANN.LECUN.COM/exdb/mnist")]
        [TestCase("yann.lecun.com/exdb/mnist/")]
        [TestCase("公开地址：http://yann.lecun.com/exdb/mnist/，先看说明。")]
        public void ActualDataRegistryUrlsRemainAvailable(string text)
        {
            Assert.That(XgEndingTextGuard.Filter(text, false), Is.Not.Null);
        }

        [TestCase("https://evil.example/yann.lecun.com/exdb/mnist/")]
        [TestCase("https://yann.lecun.com.evil.net/exdb/mnist/")]
        [TestCase("https://evil-yann.lecun.com/exdb/mnist/")]
        [TestCase("https://yann.lecun.com@evil.net/exdb/mnist/")]
        [TestCase("https://yann.lecun.com/exdb/mnist/?redirect=evil.net")]
        [TestCase("https://yann.lecun.com/exdb/mnist/#evil")]
        [TestCase("https://yann.lecun.com/exdb/mnist/../other")]
        [TestCase("https://yann.lecun.com/exdb/mnist/%2e%2e/other")]
        [TestCase("https://yann.lecun.com/exdb/mnist//")]
        [TestCase("https://yann.lecun.com:443/exdb/mnist/")]
        [TestCase("ftp://yann.lecun.com/exdb/mnist/")]
        [TestCase("https://yann.lecun.com/exdb/mnist/ https://evil.net")]
        [TestCase("https://statmt.org/wmt16")]
        public void TrustedSubstringsAndUnregisteredLegacyAddressesCannotBypassContactGuard(string text)
        {
            Assert.That(XgEndingTextGuard.Filter(text, true), Is.Null);
        }

        [Test] public void ContactChecksPreserveReviewedHistoryAndOrdinaryGameText()
        {
            const string reviewed = "史料来源：https://example.org/2017/；原始联系电话：13800138000";
            Assert.That(XgEndingTextGuard.FilterReviewed(reviewed, false), Is.EqualTo(reviewed));
            const string ordinary = "关机 病毒 诈骗 破产 下载 训练 赌场 888 21点 熊猫烧香 灰鸽子";
            Assert.That(XgEndingTextGuard.Filter(ordinary, false), Is.EqualTo(ordinary));
            Assert.That(XgEndingTextGuard.Filter("model.exe a/b [3] 12345 2016 60000", true), Is.EqualTo("model.exe a/b [3] 12345 2016 60000"));
            Assert.That(XgEndingTextGuard.Filter("00013800138000000", true), Is.EqualTo("00013800138000000"));
        }

        [Test] public void ReviewedInputRejectsHiddenControlCharactersWithoutChangingValidText()
        {
            Assert.That(XgEndingTextGuard.FilterReviewed("原题\u202e隐藏", false), Is.Null);
            Assert.That(XgEndingTextGuard.FilterReviewed("原题\0", false), Is.Null);
            Assert.That(XgEndingTextGuard.FilterReviewed("<b>已审稿原样</b>", false), Is.EqualTo("<b>已审稿原样</b>"));
            Assert.That(XgEndingTextGuard.FilterReviewed(new string('a', 16385), true), Is.Null);
        }
    }
}
