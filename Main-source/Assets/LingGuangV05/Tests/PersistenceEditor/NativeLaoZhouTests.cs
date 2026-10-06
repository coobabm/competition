using LingGuangV05.Core;
using LingGuangV05.Runtime;
using NUnit.Framework;

namespace LingGuangV05.Persistence.Tests
{
    public sealed class NativeLaoZhouTests
    {
        private static ChapterOneSim Installed() { var sim = new ChapterOneSim(); sim.InstallApp(); return sim; }

        [Test]
        public void BeforeDownloadEveryQuestionGetsDownloadHelp()
        {
            var sim = new ChapterOneSim();
            Assert.That(NativeLaoZhouReplies.BuildReply("考试在哪", sim), Is.EqualTo(NativeLaoZhouReplies.DownloadHelp));
            Assert.That(NativeLaoZhouReplies.DownloadHelp, Does.Contain("lingguang.cc"));
            sim.InstallApp();
            Assert.That(NativeLaoZhouReplies.BuildReply("考试在哪", sim), Is.Not.EqualTo(NativeLaoZhouReplies.DownloadHelp));
        }

        [Test]
        public void EmptyOrControlOnlyMessageHasNoReply()
        {
            Assert.That(NativeLaoZhouReplies.BuildReply(" \r\n\t\0 ", Installed()), Is.Null);
        }

        [Test]
        public void InputIsBoundedPlainTextWithoutSplittingSurrogatePairs()
        {
            string normalized = NativeLaoZhouReplies.NormalizeInput(" <b>你好</b>\r\n\0 世界 ");
            Assert.That(normalized, Is.EqualTo("<b>你好</b> 世界"));
            string longInput = new string('甲', NativeLaoZhouReplies.MaxInputLength - 1) + "\U0001F600尾";
            string bounded = NativeLaoZhouReplies.NormalizeInput(longInput);
            Assert.That(bounded.Length, Is.LessThanOrEqualTo(NativeLaoZhouReplies.MaxInputLength));
            Assert.That(char.IsHighSurrogate(bounded[bounded.Length - 1]), Is.False);
        }

        [Test]
        public void UnknownQuestionDisclosesLocalPresetAndAvailableHelp()
        {
            string reply = NativeLaoZhouReplies.BuildReply("火星上的天气如何", Installed());
            Assert.That(reply, Does.Contain("本地预设"));
            Assert.That(reply, Does.Contain("不是实时 AI"));
            Assert.That(reply, Does.Contain("考试"));
            Assert.That(reply.Length, Is.LessThanOrEqualTo(NativeLaoZhouReplies.MaxReplyLength));
        }

        [Test]
        public void PowerAdviceReflectsDebtAndBreakerWithoutMutatingState()
        {
            var sim = Installed();
            sim.S.billDue = 12.5; sim.S.unpaidPower = true; sim.S.breakerTripped = true;
            double money = sim.S.money;
            string reply = NativeLaoZhouReplies.BuildReply("没钱交电费了，跳闸怎么办", sim);
            Assert.That(reply, Does.Contain("12.50"));
            Assert.That(reply, Does.Contain("家庭"));
            Assert.That(reply, Does.Contain("邮件"));
            Assert.That(reply, Does.Contain("寻宝"));
            Assert.That(sim.S.money, Is.EqualTo(money));
            Assert.That(sim.S.billDue, Is.EqualTo(12.5));
        }

        [Test]
        public void ActiveExamAdviceShowsProgressAndExplainsMailPause()
        {
            var sim = Installed();
            sim.S.examActive = true; sim.S.examAnswered = 7;
            string reply = NativeLaoZhouReplies.BuildReply("考试做哪儿了", sim);
            Assert.That(reply, Does.Contain("7 / 20"));
            Assert.That(reply, Does.Contain("灵光.exe"));
            Assert.That(reply, Does.Contain("邮件"));
            Assert.That(reply, Does.Contain("暂停"));
        }

        [Test]
        public void ShopAndJobsUseCurrentStateAndCorrectSoftwareNames()
        {
            var sim = Installed();
            Assert.That(NativeLaoZhouReplies.BuildReply("显卡多少钱", sim), Does.Contain("450.00"));
            Assert.That(NativeLaoZhouReplies.BuildReply("显卡多少钱", sim), Does.Contain("寻宝"));
            Assert.That(NativeLaoZhouReplies.BuildReply("怎么赚钱接单", sim), Does.Contain("3"));
            Assert.That(NativeLaoZhouReplies.BuildReply("怎么赚钱接单", sim), Does.Contain("家庭"));
        }

        [Test]
        public void CompletedChapterNeverPromisesImplementedChapterTwo()
        {
            var sim = Installed(); sim.S.chapterOneComplete = true;
            string reply = NativeLaoZhouReplies.BuildReply("考试结束了，然后呢", sim);
            Assert.That(reply, Does.Contain("第二章尚未开放"));
            Assert.That(reply, Does.Contain("一次"));
        }

        [Test]
        public void MissingRuntimeReturnsSafeLocalHelpInsteadOfThrowing()
        {
            Assert.That(NativeLaoZhouReplies.BuildReply("你好", null), Does.Contain("本地"));
        }

#if UNITY_EDITOR
        [Test]
        public void NativeFontCloneDisposalPreservesBorrowedMaterialAndAtlas()
        {
            // This test assembly cannot statically reference Assembly-CSharp. Exercise the
            // adapter's public API with actual Unity/TMP objects, not a mock or source scan.
            System.Type fontType = System.Type.GetType("TMPro.TMP_FontAsset, Unity.TextMeshPro");
            System.Type textType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            System.Type adapterType = System.Type.GetType("LingGuangV05.Desktop.ChapterOneNativeChatAdapter, Assembly-CSharp");
            Assert.That(fontType, Is.Not.Null); Assert.That(textType, Is.Not.Null); Assert.That(adapterType, Is.Not.Null);
            UnityEngine.GameObject textObject = null, adapterObject = null;
            UnityEngine.Object originalFont = null;
            try
            {
                UnityEngine.Font source = UnityEngine.Resources.Load<UnityEngine.Font>("Fonts/NotoSansCJKsc-Regular");
                Assert.That(source, Is.Not.Null);
                originalFont = (UnityEngine.Object)fontType.GetMethod("CreateFontAsset", new[] { typeof(UnityEngine.Font) }).Invoke(null, new object[] { source });
                fontType.GetMethod("TryAddCharacters", new[] { typeof(string), typeof(bool) }).Invoke(originalFont, new object[] { "AB012", false });
                var population = fontType.GetProperty("atlasPopulationMode");
                population.SetValue(originalFont, System.Enum.Parse(population.PropertyType, "Static"));
                UnityEngine.Material material = (UnityEngine.Material)fontType.GetProperty("material").GetValue(originalFont);
                var atlases = (UnityEngine.Texture2D[])fontType.GetProperty("atlasTextures").GetValue(originalFont);
                Assert.That(material != null, Is.True);
                Assert.That(atlases.Length, Is.GreaterThan(0));
                Assert.That(atlases[0] != null, Is.True);

                textObject = new UnityEngine.GameObject("NativeFontBorrowRegressionText", typeof(UnityEngine.RectTransform));
                UnityEngine.Component label = textObject.AddComponent(textType);
                textType.GetProperty("font").SetValue(label, originalFont);
                textType.GetProperty("text").SetValue(label, "老周");
                adapterObject = new UnityEngine.GameObject("NativeFontBorrowRegressionAdapter");
                UnityEngine.Component adapter = adapterObject.AddComponent(adapterType);
                adapterType.GetMethod("ApplyNativeFont").Invoke(adapter, new object[] { label });
                Assert.That(textType.GetProperty("font").GetValue(label), Is.Not.SameAs(originalFont), "Test must create a borrowed-atlas primary clone");

                // Non-ExecuteAlways behaviours need not receive OnDestroy in an EditMode fixture
                // that never entered the play lifecycle. Exercise the same public cleanup path
                // explicitly, twice, then destroy the object; assertions remain resource-based.
                adapterType.GetMethod("DisposeResources").Invoke(adapter, null);
                adapterType.GetMethod("DisposeResources").Invoke(adapter, null);
                Assert.That(textType.GetProperty("font").GetValue(label), Is.SameAs(originalFont), "Explicit cleanup must restore the original font");
                UnityEngine.Object.DestroyImmediate(adapterObject);
                adapterObject = null;
                Assert.That(textType.GetProperty("font").GetValue(label), Is.SameAs(originalFont), "Original label font must be restored");
                Assert.That(material != null, Is.True, "Adapter disposal must not destroy the original native material");
                foreach (UnityEngine.Texture2D atlas in atlases)
                    if (!ReferenceEquals(atlas, null)) Assert.That(atlas != null, Is.True, "Adapter disposal must not destroy borrowed native atlases");
                Assert.That(fontType.GetProperty("material").GetValue(originalFont), Is.SameAs(material));
                UnityEngine.TestTools.LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if (adapterObject != null) UnityEngine.Object.DestroyImmediate(adapterObject);
                if (textObject != null) UnityEngine.Object.DestroyImmediate(textObject);
                // This font/material/atlas is test-owned, never a source-project font asset.
                if (originalFont != null) UnityEngine.Object.DestroyImmediate(originalFont);
            }
        }
#endif
    }
}
