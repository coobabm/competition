using System;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgJudgeProtocolTests
    {
        const string Yes = "{\"token\":\"是\",\"logprob\":-0.2231435513142097}";
        const string No = "{\"token\":\"否\",\"logprob\":-1.6094379124341003}";

        static string Reply(string candidates, string output = "是") =>
            "{\"choices\":[{\"message\":{\"content\":\"" + output + "\"},\"logprobs\":{\"content\":[{\"token\":\"" + output + "\",\"top_logprobs\":[" + candidates + "]}]}}]}";

        [Test]
        public void PromptUsesOnlyVisibleQuestionAndFixedTaskRules()
        {
            var card = new XgCard { dataset = "spam", question = "中奖通知值得相信吗？", truth = true,
                why = "PRIVATE_ANSWER_EXPLANATION", roll = .123456789, source = "PRIVATE_SOURCE", line = "PRIVATE_LINE", seed = 987654321 };
            string before = XgJudgeProtocol.Prompt(card);
            card.truth = false; card.why = "DIFFERENT_ANSWER"; card.roll = .999; card.source = "DIFFERENT_SOURCE"; card.line = "DIFFERENT_LINE"; card.seed = 1;
            Assert.AreEqual(before, XgJudgeProtocol.Prompt(card), "hidden answer-bearing fields must never affect a request");
            StringAssert.Contains(card.question, before);
            StringAssert.DoesNotContain("PRIVATE", before);
            StringAssert.DoesNotContain("truth", before);
            StringAssert.DoesNotContain("roll", before);
        }

        [TestCase("mnist")]
        [TestCase("captcha")]
        [TestCase("meme")]
        [TestCase("go")]
        [TestCase("unknown")]
        public void UnsupportedImageOrUnknownDeskDoesNotProduceTextJudgePrompt(string desk)
        {
            Assert.IsNull(XgJudgeProtocol.Prompt(new XgCard { dataset = desk, question = "is this it?" }));
        }

        [Test]
        public void EmptyAndOversizedQuestionsAreRejected()
        {
            Assert.IsNull(XgJudgeProtocol.Prompt(null));
            Assert.IsNull(XgJudgeProtocol.Prompt(new XgCard { dataset = "spam", question = "  " }));
            Assert.IsNull(XgJudgeProtocol.Prompt(new XgCard { dataset = "spam", question = new string('x', 9000) }));
        }

        [Test]
        public void TranslationYesMeansCorrectEnglishNotChinglish()
        {
            StringAssert.Contains("这句英文说得对吗", XgJudgeProtocol.Prompt(new XgCard { dataset = "translate", question = "Long time no see." }));
        }

        [Test]
        public void AttentionQuestionKeepsItsVisibleTaskInsteadOfJudgingEnglishGrammar()
        {
            var card = new XgCard { dataset = "translate", kind = "attention", question = "翻译bank时看“河”，它看对了吗？" };
            string prompt = XgJudgeProtocol.Prompt(card);
            StringAssert.Contains("最后的是非问题", prompt);
            StringAssert.DoesNotContain("英文说得对吗", prompt);
        }

        [TestCase("longtext")]
        [TestCase("crosssentence")]
        public void LongAndCrossSentenceQuestionsUseOnlyVisiblePrompt(string desk)
        {
            var card = new XgCard { dataset = desk, question = "第一句：不要开电脑。后来聊了天气。现在应该开电脑吗？", truth = false, why = "PRIVATE_REASON", line = "PRIVATE_LINE", source = "PRIVATE_SOURCE" };
            string prompt = XgJudgeProtocol.Prompt(card);
            Assert.IsNotNull(prompt);
            StringAssert.Contains("最后的是非问题", prompt);
            StringAssert.Contains(card.question, prompt);
            StringAssert.DoesNotContain("PRIVATE", prompt);
            card.truth = true; card.why = "OTHER_REASON"; card.roll = 1;
            Assert.AreEqual(prompt, XgJudgeProtocol.Prompt(card));
        }

        [Test]
        public void PoemPromptUsesOnlyDisplayedPrefixAndCandidateNeverHiddenContinuation()
        {
            var card = new XgCard { dataset = "poems", line = "床前PRIVATE_HIDDEN_CONTINUATION", shown = 2, askedChar = "明", truth = true, why = "PRIVATE_REASON", question = "PRIVATE_QUESTION" };
            string prompt = XgJudgeProtocol.Prompt(card);
            Assert.IsNotNull(prompt);
            StringAssert.Contains("床前", prompt);
            StringAssert.Contains("下一个字是「明」吗", prompt);
            StringAssert.DoesNotContain("PRIVATE", prompt);
            card.line = "床前OTHER_HIDDEN_CONTINUATION"; card.truth = false; card.why = "OTHER_REASON";
            Assert.AreEqual(prompt, XgJudgeProtocol.Prompt(card));
        }

        [TestCase(-1, "明")]
        [TestCase(3, "明")]
        [TestCase(1, "明月")]
        public void InvalidPoemPrefixOrCandidateIsRejected(int shown, string candidate)
        {
            Assert.IsNull(XgJudgeProtocol.Prompt(new XgCard { dataset = "poems", line = "床前", shown = shown, askedChar = candidate }));
        }

        [TestCase("poems", "order")]
        [TestCase("spam", "order")]
        [TestCase("poems", "long")]
        [TestCase("spam", "long")]
        [TestCase("poems", "translation")]
        [TestCase("spam", "translation")]
        [TestCase("translate", "translation")]
        [TestCase("translate", "attention")]
        public void VisibleCurriculumQuestionTakesPriorityOverOriginalDesk(string desk, string kind)
        {
            var card = new XgCard { dataset = desk, kind = kind, bottleneckPreview = true,
                question = "句子A要求先保存再关闭，句子B要求先关闭再保存。顺序相同吗？",
                sourceText = "visible source", candidateText = "visible candidate", line = "PRIVATE_FULL_POEM", shown = 2,
                askedChar = "私", why = "PRIVATE_EXPLANATION", truth = false };
            string prompt = XgJudgeProtocol.Prompt(card);
            Assert.IsNotNull(prompt);
            StringAssert.Contains("最后的是非问题", prompt);
            StringAssert.Contains(card.question, prompt);
            StringAssert.DoesNotContain("PRIVATE", prompt);
            StringAssert.DoesNotContain("下一个字", prompt);
            StringAssert.DoesNotContain("英文说得对吗", prompt);
            StringAssert.DoesNotContain("垃圾或诈骗", prompt);
        }

        [Test]
        public void OrdinaryOrderTaggedPoemStillSendsOnlyDisplayedPrefix()
        {
            var card = new XgCard { dataset = "poems", kind = "order", bottleneckPreview = false,
                line = "床前PRIVATE_CONTINUATION", shown = 2, askedChar = "明", question = "PRIVATE_UNUSED_QUESTION" };
            string prompt = XgJudgeProtocol.Prompt(card);
            StringAssert.Contains("床前", prompt);
            StringAssert.Contains("下一个字是「明」吗", prompt);
            StringAssert.DoesNotContain("PRIVATE", prompt);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PixelPatternsNeverEnterTextOnlyJudgeEvenOnTextDesk(bool first)
        {
            var card = new XgCard { dataset = "spam", kind = "translation", question = "两张图相同吗？", patternA = first ? "10101" : "", patternB = first ? "" : "10101" };
            Assert.IsNull(XgJudgeProtocol.Prompt(card));
        }

        static string EnglishPrompt(XgCard card)
        {
            var overload = typeof(XgJudgeProtocol).GetMethod("Prompt", new[] { typeof(XgCard), typeof(bool) });
            Assert.IsNotNull(overload, "The prompt boundary needs the displayed-language choice");
            return (string)overload.Invoke(null, new object[] { card, true });
        }

        [Test]
        public void EnglishCurriculumUsesVisibleEnglishQuestionWithoutHiddenLineOrExplanation()
        {
            var card = new XgCard { dataset = "poems", kind = "translation", bottleneckPreview = true,
                question = "不该发送的中文版本", questionEn = "Source: give the letter to Lin. Candidate: give it to Chen. Is the recipient preserved?",
                line = "PRIVATE_FULL_POEM", why = "PRIVATE_EXPLANATION", sourceText = "PRIVATE_SOURCE_FIELD", candidateText = "PRIVATE_CANDIDATE_FIELD", truth = false };
            string prompt = EnglishPrompt(card);
            StringAssert.Contains(card.questionEn, prompt);
            StringAssert.Contains("final yes/no question", prompt);
            StringAssert.DoesNotContain(card.question, prompt);
            StringAssert.DoesNotContain("PRIVATE", prompt);
        }

        [Test]
        public void EnglishQuestionFallsBackToVisibleOriginalWhenTranslationIsBlank()
        {
            var card = new XgCard { dataset = "logic", question = "2加2等于4吗？", questionEn = "  " };
            StringAssert.Contains(card.question, EnglishPrompt(card));
        }

        [Test]
        public void RequestIsOneTokenDeterministicLogprobsWithNoThinkingOrTools()
        {
            string body = XgJudgeProtocol.RequestBody("题面\"\n\\\t");
            StringAssert.Contains("\"max_tokens\":1", body);
            StringAssert.Contains("\"temperature\":0", body);
            StringAssert.Contains("\"logprobs\":true", body);
            StringAssert.Contains("\"top_logprobs\":10", body);
            StringAssert.Contains("\"enable_thinking\":false", body);
            StringAssert.Contains("题面\\\"\\n\\\\\\t", body);
            StringAssert.DoesNotContain("tools", body);
        }

        [Test]
        public void ProbabilityNormalizesYesAndNoMass()
        {
            Assert.IsTrue(XgJudgeProtocol.TryParseProbability(Reply(Yes + "," + No), out double p, out string failure), failure);
            Assert.AreEqual(.8, p, 1e-10);
            Assert.IsNull(failure);
        }

        [Test]
        public void ProbabilityCombinesWhitespaceVariantsAndIgnoresOtherTokens()
        {
            string rows = "{\"token\":\" 是\",\"logprob\":-1.6094379124341003}," +
                "{\"token\":\"是\",\"logprob\":-1.6094379124341003}," +
                "{\"token\":\" 否\",\"logprob\":-2.302585092994046}," +
                "{\"token\":\"也\",\"logprob\":-0.6931471805599453}";
            Assert.IsTrue(XgJudgeProtocol.TryParseProbability(Reply(rows), out double p, out string failure), failure);
            Assert.AreEqual(.8, p, 1e-10);
        }

        [TestCase("-10000", "-10001", .7310585786300049)]
        [TestCase("-10001", "-10000", .2689414213699951)]
        public void LogSpaceNormalizationDoesNotUnderflow(string yes, string no, double expected)
        {
            string rows = "{\"token\":\"是\",\"logprob\":" + yes + "},{\"token\":\"否\",\"logprob\":" + no + "}";
            Assert.IsTrue(XgJudgeProtocol.TryParseProbability(Reply(rows), out double p, out string failure), failure);
            Assert.AreEqual(expected, p, 1e-10);
        }

        [Test]
        public void MissingClassIsFailureRatherThanCertainPrediction()
        {
            Assert.IsFalse(XgJudgeProtocol.TryParseProbability(Reply(Yes), out _, out string failure));
            Assert.IsNotEmpty(failure);
            Assert.IsFalse(XgJudgeProtocol.TryParseProbability(Reply(No, "否"), out _, out _));
        }

        [TestCase("null")]
        [TestCase("\"NaN\"")]
        [TestCase("\"Infinity\"")]
        [TestCase("1")]
        public void InvalidLogprobIsFailure(string bad)
        {
            string rows = "{\"token\":\"是\",\"logprob\":" + bad + "}," + No;
            Assert.IsFalse(XgJudgeProtocol.TryParseProbability(Reply(rows), out _, out string failure));
            Assert.IsNotEmpty(failure);
        }

        [Test]
        public void MissingLogprobAndDuplicateTokenAreRejected()
        {
            Assert.IsFalse(XgJudgeProtocol.TryParseProbability(Reply("{\"token\":\"是\"}," + No), out _, out _));
            Assert.IsFalse(XgJudgeProtocol.TryParseProbability(Reply(Yes + "," + Yes + "," + No), out _, out _));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("{}")] 
        [TestCase("not json")]
        [TestCase("{\"choices\":[]}")]
        [TestCase("{\"choices\":[{\"message\":{\"content\":\"是\"}}]}")]
        public void MalformedOrAbsentLogprobsFailClosed(string body)
        {
            Assert.IsFalse(XgJudgeProtocol.TryParseProbability(body, out _, out string failure));
            Assert.IsNotEmpty(failure);
        }

        [TestCase(" ")]
        [TestCase("不确定")]
        [TestCase("是的")]
        public void NonBinaryGeneratedTokenFailsClosed(string output)
        {
            Assert.IsFalse(XgJudgeProtocol.TryParseProbability(Reply(Yes + "," + No, output), out _, out _));
        }
    }
}
