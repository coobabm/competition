using System;
using System.Linq;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace LingGuangV05.UI.Tests
{
    public sealed class GameplayLocalizationTests
    {
        [Test]
        public void EmailAndExplanationTranslateWithoutChangingSource()
        {
            const string source = "陌生发件人：恭喜抽中十万元！先汇 300 元手续费。";
            using (GameText.Bind(() => "en-US"))
            {
                Assert.That(GameText.Source(source), Is.EqualTo("Unknown sender: You won ¥100,000! Send a ¥300 processing fee first."));
                Assert.That(GameText.Source("陌生中奖通知要求先付手续费，是典型诈骗垃圾邮件。"), Is.EqualTo("An unsolicited prize notice requiring an advance fee is a classic scam."));
            }
            using (GameText.Bind(() => "zh-CN")) Assert.That(GameText.Source(source), Is.EqualTo(source));
        }

        [Test]
        public void NumericCoreStatusAndCompoundFeedbackKeepTheirValues()
        {
            const string status = "已连接 7 → 12。方向决定信号去向。";
            const string feedback = "判断正确，样本和学习点已记录。陌生中奖通知要求先付手续费，是典型诈骗垃圾邮件。 人工标注抵扣电费 ¥2.50，供电已恢复。";
            using (GameText.Bind(() => "en-US"))
            {
                Assert.That(GameText.Source(status), Is.EqualTo("Connected 7 → 12. The direction determines signal flow."));
                string result = GameText.Source(feedback);
                Assert.That(result, Does.Contain("¥2.50"));
                Assert.That(result, Does.Contain("Power restored."));
                Assert.That(result, Does.Not.Contain("判断"));
            }
            using (GameText.Bind(() => "zh-CN")) Assert.That(GameText.Source(status), Is.EqualTo(status));
        }

        [Test]
        public void KnownNpcHelpTranslatesCurrentNumbersWithoutChangingSimulation()
        {
            var sim = new ChapterOneSim();
            sim.InstallApp(); // Exam help requires an installed app, not the initial download reminder.
            string source = NativeLaoZhouReplies.BuildReply("exam", sim);
            string snapshot = JsonUtility.ToJson(sim.S);
            using (GameText.Bind(() => "en-US"))
            {
                Assert.That(GameText.Source(source), Does.StartWith("The exam is in Lingguang.exe."));
                Assert.That(GameText.Source(source), Does.Contain("20 questions"));
            }
            Assert.That(JsonUtility.ToJson(sim.S), Is.EqualTo(snapshot));
        }

        [TestCase("training", "升级")]
        [TestCase("learning points", "升级")]
        [TestCase("skill", "升级")]
        [TestCase("bill", "电费")]
        [TestCase("case", "显卡")]
        [TestCase("label", "标注")]
        [TestCase("connect", "连线")]
        [TestCase("income", "收入")]
        public void EnglishHelpTopicsRouteToExistingChineseTopicRules(string english, string chinese)
        {
            var sim = new ChapterOneSim();
            string stateBefore = JsonUtility.ToJson(sim.S);
            Assert.That(NativeLaoZhouReplies.BuildReply(english, sim), Is.EqualTo(NativeLaoZhouReplies.BuildReply(chinese, sim)));
            Assert.That(JsonUtility.ToJson(sim.S), Is.EqualTo(stateBefore));
        }

        [TestCase("09:41 PM", "zh-CN", "09:41 下午")]
        [TestCase("09:41 AM", "zh-CN", "09:41 上午")]
        [TestCase("09:41 PM", "en-US", "09:41 PM")]
        [TestCase("21:41", "zh-CN", "21:41")]
        public void NativeChatTimestampChangesOnlyItsLanguageMarker(string nativeTime, string locale, string expected)
        {
            Type stamp = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("LingGuangV05.Desktop.YY2010MessageStamp"))
                .FirstOrDefault(type => type != null);
            Assert.That(stamp, Is.Not.Null);
            var format = stamp.GetMethod("LocalizeTimeMarker", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.That(format, Is.Not.Null);
            using (GameText.Bind(() => locale)) Assert.That(format.Invoke(null, new object[] { nativeTime }), Is.EqualTo(expected));
        }

        [Test]
        public void MailRebindsStaticAndDynamicTextButPreservesAiName()
        {
            string locale = "zh-CN";
            GameObject host = null, canvas = null, appHost = null;
            using (GameText.Bind(() => locale))
            {
                try
                {
                    host = new GameObject("LocalizationRuntime");
                    host.SetActive(false); // Prevent Awake from touching the player's on-disk save.
                    ChapterOneRuntime runtime = host.AddComponent<ChapterOneRuntime>();
                    var sim = new ChapterOneSim();
                    sim.S.aiName = "家庭 English <raw>";
                    runtime.SetSimulationForTests(sim);
                    canvas = new GameObject("LocalizationCanvas", typeof(RectTransform), typeof(Canvas));
                    appHost = new GameObject("LocalizationMail");
                    ChapterOneApp app = appHost.AddComponent<ChapterOneApp>();
                    app.ConfigureProfile("mail");
                    app.Initialize(runtime, canvas.GetComponent<RectTransform>(), TMP_Settings.defaultFontAsset);
                    TMP_Text Find(string name) => app.UiRoot.GetComponentsInChildren<TMP_Text>(true).Single(text => text.name == name);
                    Assert.That(Find("MailListTitle").text, Is.EqualTo("收件箱"));
                    locale = "en-US"; GameText.PublishChanged();
                    Assert.That(Find("MailListTitle").text, Is.EqualTo("Inbox"));
                    Assert.That(Find("CardAnswerYesText").text, Is.EqualTo("Yes · Spam"));
                    Assert.That(Find("CardPrompt").text, Does.Contain("Is this email spam?"));
                    Assert.That(Find("MailSender").text, Does.Contain(sim.S.aiName));
                    Assert.That(sim.S.aiName, Is.EqualTo("家庭 English <raw>"));
                    locale = "zh-CN"; GameText.PublishChanged();
                    Assert.That(Find("MailListTitle").text, Is.EqualTo("收件箱"));
                    Assert.That(Find("CardPrompt").text, Does.Contain("这封邮件是垃圾邮件吗？"));
                }
                finally
                {
                    if (appHost != null) UnityEngine.Object.DestroyImmediate(appHost);
                    if (host != null) UnityEngine.Object.DestroyImmediate(host);
                    if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas);
                }
            }
        }
    }
}
