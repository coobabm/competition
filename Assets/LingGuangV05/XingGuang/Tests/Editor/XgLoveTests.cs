using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>「她爱我吗」 (女友系统 §6): the question, consent, cooldown and the lines, on the lab's side.</summary>
    public sealed class XgLoveTests
    {
        static XgSim Ready(int day = 40)
        {
            var sim = new XgSim();
            sim.S.stage = 5; sim.LoveDataReady = true; sim.LoveToday = day;
            return sim;
        }

        static string Last(XgSim sim) => sim.S.chat[sim.S.chat.Count - 1].text;

        static XgState Reload(XgState state)
        {
#if UNITY_5_3_OR_NEWER
            return UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(state));
#else
            var options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
            return System.Text.Json.JsonSerializer.Deserialize<XgState>(System.Text.Json.JsonSerializer.Serialize(state, options), options);
#endif
        }

        [TestCase(1)]
        [TestCase(5)]
        public void UsedEntryStaysGoneAfterSaveReloadWithoutRemovingHistory(int stage)
        {
            var sim = Ready(); sim.S.stage = stage; sim.OfferLoveHint();
            sim.AddLine("me", sim.LoveQuestion);
            Assert.IsTrue(sim.ConsumeLoveQuestion());
            Assert.IsFalse(sim.ConsumeLoveQuestion(), "only the first acceptance needs a save");
            sim.Scripted(sim.LoveQuestion);
            if (sim.S.loveAwaitingConsent) sim.AnswerLoveConsent(false);
            var copy = Reload(sim.S); var loaded = new XgSim(copy) { LoveToday = 100, LoveDataReady = true };
            Assert.IsFalse(loaded.OfferLoveQuestion);
            Assert.IsFalse(loaded.OfferLoveHint(), "inner voice must not re-offer a consumed entry");
            Assert.AreEqual(sim.S.chat.Count, loaded.S.chat.Count);
            Assert.AreEqual(sim.S.chat[0].text, loaded.S.chat[0].text);
            Assert.IsNotNull(loaded.Scripted("她爱我吗"), "manual input remains supported");
            var fresh = Ready(); Assert.IsTrue(fresh.OfferLoveHint()); Assert.IsTrue(fresh.OfferLoveQuestion);
        }

        [Test]
        public void InsufficientHistoryStillConsumesTheEntry()
        {
            var sim = Ready(); sim.LoveDataReady = false; sim.OfferLoveHint();
            sim.Scripted(sim.LoveQuestion);
            Assert.IsTrue(sim.S.loveQuestionConsumed);
            sim.LoveDataReady = true;
            Assert.IsFalse(new XgSim(Reload(sim.S)).OfferLoveQuestion);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void LegacyAskedSavesDoNotResurrectTheEntry(int evidence)
        {
            var sim = Ready(); sim.S.loveOffered = true;
            if (evidence == 0) sim.S.loveEarlyAsked = 1;
            if (evidence == 1) sim.S.loveConsent = -1;
            if (evidence == 2) sim.S.loveVerdicts = 1;
            if (evidence == 3) sim.AddLine("me", "她爱我吗");
            var loaded = new XgSim(Reload(sim.S)) { LoveToday = 100 };
            Assert.IsFalse(loaded.OfferLoveQuestion);
            Assert.IsTrue(loaded.S.loveQuestionConsumed);
        }

        [Test]
        public void RecognisesTheQuestionInBothLanguages()
        {
            Assert.IsTrue(XgSim.IsLoveQuestion("她……爱我吗？"));
            Assert.IsTrue(XgSim.IsLoveQuestion("你说她爱不爱我"));
            Assert.IsTrue(XgSim.IsLoveQuestion("她爱我吗"));
            Assert.IsTrue(XgSim.IsLoveQuestion("Does she... love me?"));
            Assert.IsTrue(XgSim.IsLoveQuestion("so does she love me"));
            Assert.IsFalse(XgSim.IsLoveQuestion("你爱我吗"));
            Assert.IsFalse(XgSim.IsLoveQuestion(""));
            Assert.IsTrue(XgSim.IsLoveQuestion(new XgSim().LoveQuestion));
        }

        [Test]
        public void BeforeStageFiveItOnlyAnswersYesOrNo()
        {
            var sim = Ready(); int asked = 0; sim.LoveVerdictRequested += _ => asked++;
            for (int stage = 1; stage <= 4; stage++)
            {
                sim.S.stage = stage;
                string reply = sim.Scripted("她爱我吗");
                Assert.IsTrue(reply == "是。" || reply == "否。", "stage " + stage + ": " + reply);
                Assert.AreEqual(sim.EarlyLoveAnswer, reply, "the same lean as the 「？」 cell");
            }
            Assert.AreEqual(0, asked, "it never reads her chats before stage 5");
            Assert.IsFalse(sim.S.loveAwaitingConsent);
            Assert.IsFalse(sim.OfferLoveQuestion, "asked once, the suggested chip stays consumed");
        }

        [Test]
        public void TooLittleChatIsSaidPlainly()
        {
            var sim = Ready(); sim.LoveDataReady = false;
            StringAssert.Contains("太少", sim.Scripted("她爱我吗"));
            Assert.IsFalse(sim.S.loveAwaitingConsent);
        }

        [Test]
        public void TheChipAppearsOnceTheInnerVoiceHasWondered()
        {
            var sim = Ready();
            Assert.IsFalse(sim.OfferLoveQuestion);
            Assert.IsTrue(sim.OfferLoveHint());
            Assert.IsFalse(sim.OfferLoveHint(), "once");
            Assert.IsTrue(sim.OfferLoveQuestion);
            sim.S.stage = 1;
            Assert.IsTrue(sim.OfferLoveQuestion, "it can be asked from stage 1; the answer is just 是 or 否");
        }

        [Test]
        public void EveryLineOfTheChatLogIsAnnouncedForYY()
        {
            // YY shows the chat with it (XgYyTalk): the consent answer, its reply and the verdict lines all reach it.
            var sim = Ready(); sim.OfferLoveHint();
            var seen = new System.Collections.Generic.List<string>();
            sim.LineAdded += l => seen.Add(l.from + ":" + l.text);
            sim.Scripted(sim.LoveQuestion);
            sim.AnswerLoveConsent(false);
            CollectionAssert.AreEqual(new[] { "me:算了", "ai:好。" }, seen);
            sim.PostLoveVerdict(true, "是。", "这个数会变。");
            Assert.AreEqual("ai:这个数会变。", seen[seen.Count - 1]);
            Assert.AreEqual(4, seen.Count);
        }

        [Test]
        public void ItAsksForConsentFirstAndReadGoesToTheFullCutscene()
        {
            var sim = Ready(); sim.OfferLoveHint();
            bool? full = null; sim.LoveVerdictRequested += f => full = f;
            Assert.AreEqual("要读你和她的聊天记录。", sim.Scripted(sim.LoveQuestion));
            Assert.IsTrue(sim.S.loveAwaitingConsent);
            Assert.IsFalse(sim.OfferLoveQuestion, "the chip makes room for 【读吧】【算了】");
            Assert.IsNull(full);
            Assert.IsTrue(sim.AnswerLoveConsent(true));
            Assert.AreEqual("读吧", Last(sim));
            Assert.AreEqual(true, full);
            Assert.AreEqual(1, sim.S.loveConsent);
            Assert.IsTrue(sim.LoveVerdictPending);
            Assert.AreEqual("我在读。", sim.Scripted("她爱我吗"));
            Assert.IsFalse(sim.AnswerLoveConsent(false), "nothing to answer now");
        }

        [Test]
        public void NeverMindIsRememberedButAYesLaterWins()
        {
            var sim = Ready();
            sim.Scripted("她爱我吗");
            Assert.IsTrue(sim.AnswerLoveConsent(false));
            Assert.AreEqual("好。", Last(sim));
            Assert.AreEqual(-1, sim.S.loveConsent);
            Assert.AreEqual("你没让我看。我就没看。", sim.PrivacyCardRemark);
            Assert.AreEqual("要读你和她的聊天记录。", sim.Scripted("她爱我吗"), "it asks again");
            sim.AnswerLoveConsent(true);
            Assert.AreEqual("我看过一次。你让的。", sim.PrivacyCardRemark);
            sim.Scripted("她爱我吗"); // pending: no new consent step
            Assert.IsFalse(sim.S.loveAwaitingConsent);
        }

        [Test]
        public void TheVerdictStartsAThreeDayCooldownThenReAsksAreShort()
        {
            var sim = Ready(40);
            bool? full = null; sim.LoveVerdictRequested += f => full = f;
            sim.Scripted("她爱我吗"); sim.AnswerLoveConsent(true);
            sim.PostLoveVerdict(true, "是。她记得你说过的每一件小事。", "她没打出来的，我读不到。");
            Assert.IsFalse(sim.LoveVerdictPending);
            Assert.AreEqual("她没打出来的，我读不到。", Last(sim));
            Assert.AreEqual(1, sim.S.loveVerdicts);
            Assert.IsTrue(sim.S.loveLastYes);
            Assert.IsTrue(sim.LoveCoolingDown);
            Assert.IsFalse(sim.OfferLoveQuestion);
            full = null;
            sim.LoveToday = 42;
            Assert.AreEqual("同一个问题问三遍，答案不会变。会变的是她。", sim.Scripted("她爱我吗"));
            Assert.IsNull(full);
            sim.LoveToday = 43;
            Assert.IsFalse(sim.LoveCoolingDown);
            Assert.AreEqual("我再读一遍。", sim.Scripted("她爱我吗"), "consent is not asked again");
            Assert.AreEqual(false, full, "later asks play only the verdict");
        }

        [Test]
        public void CancelAllowsTypedRetryButDoesNotRestoreTheChip()
        {
            var sim = Ready(); sim.S.loveConsent = 1; sim.OfferLoveHint();
            sim.Scripted("她爱我吗");
            Assert.IsFalse(sim.OfferLoveQuestion);
            sim.CancelLoveVerdict();
            Assert.IsFalse(sim.OfferLoveQuestion);
            Assert.AreEqual("我再读一遍。", sim.Scripted("她爱我吗"));
            Assert.IsTrue(sim.LoveVerdictPending);
            Assert.AreEqual(0, sim.S.loveVerdicts);
        }

        [Test]
        public void OfflineLinesFollowTheTierAndUseRealNumbers()
        {
            var sim = Ready();
            Assert.AreEqual("是。她 12 点 40 还在等你说晚安。", sim.LoveConclusionLine(4, false, "12 点 40"));
            Assert.AreEqual("是。她记得你说过的每一件小事。", sim.LoveConclusionLine(3, false));
            Assert.AreEqual("是。她在，但在等你先开口。", sim.LoveConclusionLine(2, false));
            Assert.AreEqual("否。不是不爱。你最近回她平均要 3 小时 12 分。", sim.LoveConclusionLine(1, false, null, "3 小时 12 分"));
            Assert.AreEqual("否。她改了签名：『有些人，说了也不懂。』", sim.LoveConclusionLine(0, false, null, null, "有些人，说了也不懂。"));
            StringAssert.StartsWith("否。", sim.LoveConclusionLine(0, false));
            StringAssert.StartsWith("……是", sim.LoveConclusionLine(2, true), "near 50 it hedges");
            StringAssert.StartsWith("……否", sim.LoveConclusionLine(1, true));
            Assert.AreEqual("这个问题，不该问我。该问她。", sim.LoveLimitLine(0, false));
            Assert.AreEqual("我只读得到她打出来的字。不过这些够了。", sim.LoveLimitLine(4, false));
            for (int tier = 0; tier <= 4; tier++)
                foreach (bool hesitant in new[] { false, true })
                {
                    Assert.That(sim.LoveConclusionLine(tier, hesitant) + sim.LoveLimitLine(tier, hesitant), Does.Not.Contain("%"));
                    sim.English = true;
                    Assert.That(sim.LoveConclusionLine(tier, hesitant), Does.StartWith(tier >= 2 ? (hesitant ? "…Yes" : "Yes") : (hesitant ? "…No" : "No")));
                    Assert.That(sim.LoveLimitLine(tier, hesitant), Is.Not.Empty);
                    sim.English = false;
                }
        }

        [Test]
        public void ThePromptCarriesTheSlotsAndForbidsPercentages()
        {
            var sim = Ready();
            string p = sim.LoveVerdictPrompt(false, "她平均回复时间从 2 分钟 变成 41 分钟", "先说晚安的：她 3 晚，你 9 晚", "她回了「哦」", "下周三考概率论");
            StringAssert.Contains("你的判断：否。不要说百分比。", p);
            StringAssert.Contains("41 分钟", p);
            StringAssert.Contains("下周三考概率论", p);
            StringAssert.Contains("不要编造", p);
            sim.English = true;
            StringAssert.Contains("Never give a percentage", sim.LoveVerdictPrompt(true, "a", "b", "c", "d"));
        }

        [Test]
        public void ModelLinesAreCleaned()
        {
            var sim = Ready();
            Assert.IsNull(sim.CleanLoveLine("是。她爱你的概率是 87%。", true), "never a percentage");
            Assert.IsNull(sim.CleanLoveLine("否。她不爱你了。", true), "never the opposite verdict");
            Assert.IsNull(sim.CleanLoveLine("  ", true));
            Assert.AreEqual("是。她每晚都等你说晚安。", sim.CleanLoveLine("她每晚都等你说晚安。", true), "the verdict word is added");
            Assert.AreEqual("否。你最近回她要 3 小时。她回了「哦」。", sim.CleanLoveLine("否。你最近回她要 3 小时。她回了「哦」。我很遗憾。还有更多。", false), "at most two sentences after the verdict");
            Assert.AreEqual("是。她记得概率论那天。", sim.CleanLoveLine("<think>x</think>是。她记得概率论那天。", true));
            sim.English = true;
            Assert.AreEqual("No. She said \"oh\".", sim.CleanLoveLine("No. She said \"oh\".", false));
            Assert.IsNull(sim.CleanLoveLine("Yes, 90 percent.", true));
        }

        [Test]
        public void ThePrivacyCardRemembersTheAnswer()
        {
            var sim = Ready();
            sim.S.logsTotal = 1;
            var card = sim.RuleCards().Find(c => c.id == "privacy");
            Assert.IsNotNull(card);
            Assert.That(card.source, Does.Not.Contain("你让的"));
            sim.S.loveConsent = 1;
            card = sim.RuleCards().Find(c => c.id == "privacy");
            StringAssert.EndsWith("我看过一次。你让的。", card.source);
        }

        [Test]
        public void TheStateSurvivesASave()
        {
            var sim = Ready();
            sim.S.loveOffered = true; sim.S.loveQuestionConsumed = true; sim.S.loveConsent = -1; sim.S.loveAskedDay = 41; sim.S.loveVerdicts = 2; sim.S.loveLastYes = true;
#if UNITY_5_3_OR_NEWER
            var copy = UnityEngine.JsonUtility.FromJson<XgState>(UnityEngine.JsonUtility.ToJson(sim.S));
            var old = UnityEngine.JsonUtility.FromJson<XgState>("{\"version\":1}");
#else
            var options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
            var copy = System.Text.Json.JsonSerializer.Deserialize<XgState>(System.Text.Json.JsonSerializer.Serialize(sim.S, options), options);
            var old = System.Text.Json.JsonSerializer.Deserialize<XgState>("{\"version\":1}", options);
#endif
            Assert.IsTrue(copy.loveOffered);
            Assert.IsTrue(copy.loveQuestionConsumed);
            Assert.IsFalse(old.loveQuestionConsumed);
            Assert.AreEqual(-1, copy.loveConsent);
            Assert.AreEqual(41, copy.loveAskedDay);
            Assert.AreEqual(2, copy.loveVerdicts);
            Assert.IsTrue(copy.loveLastYes);
            Assert.IsFalse(old.loveOffered);
            Assert.AreEqual(0, old.loveConsent);
            Assert.AreEqual(0, old.loveAskedDay);
        }
    }
}
