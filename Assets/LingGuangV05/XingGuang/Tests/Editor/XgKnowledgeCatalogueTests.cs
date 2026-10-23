using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgKnowledgeCatalogueTests
    {
        static readonly string[] V4Learning = { "激活函数", "神经元", "神经网络", "归一化指数", "交叉熵", "梯度下降", "随机梯度下降", "梯度爆炸", "训练轮次", "超参数", "欠拟合", "正则化", "权重衰减", "泛化", "初始化", "推断", "预训练", "微调" };
        static readonly string[] V4Architecture = { "全连接层", "池化", "编码解码", "自注意力", "语言模型", "字词单位", "温度", "生成对抗网络", "自编码器", "强化学习", "自我对弈" };
        static readonly string[] V4Data = { "数据集", "样本", "标注", "标签噪声", "训练集", "验证集", "测试集", "精确率", "召回率", "混淆矩阵", "基准测试" };
        static readonly string[] OriginalHardware = { "显卡", "GPU", "显卡加速", "CPU", "机箱风道", "算力", "浮点运算", "显存", "显存带宽", "多卡并行", "数据并行", "卡间互联", "PCIe", "主板插槽", "电源功率", "功耗", "电费", "降频", "机房", "机柜", "内存", "硬盘", "CUDA", "专用芯片" };
        [Test] public void DesignCatalogueHasCompleteBilingualEntriesAndResolvableLinks()
        {
            Assert.AreEqual(132, XgKnowledge.Entries.Length);
            var keys = new HashSet<string>();
            int hardware = 0;
            foreach (var entry in XgKnowledge.Entries)
            {
                Assert.IsTrue(keys.Add(entry.Key), entry.Key);
                Assert.IsNotEmpty(entry.English, entry.Key);
                Assert.IsNotEmpty(entry.ChineseBody, entry.Key);
                Assert.IsNotEmpty(entry.EnglishBody, entry.Key);
                Assert.That(entry.Category, Is.InRange(0, 3));
                Assert.IsNotEmpty(entry.Related, entry.Key);
                var relatedKeys = new HashSet<string>();
                foreach (var related in entry.Related)
                {
                    Assert.IsTrue(relatedKeys.Add(related), entry.Key + ": duplicate link " + related);
                    Assert.AreNotEqual(entry.Key, related, entry.Key + ": self link");
                    Assert.IsNotNull(XgKnowledge.Find(related), entry.Key + " -> " + related);
                }
                if (entry.Category == 3) hardware++;
            }
            Assert.AreEqual(24, hardware);
        }
        [Test] public void V4AndAbruptSwitchHaveBilingualUsageInTheirAuthoredCategories()
        {
            var expected = V4Learning.Concat(V4Architecture).Concat(V4Data).Concat(new[] { "突然开关" }).ToArray();
            CollectionAssert.AreEquivalent(expected, XgKnowledge.Entries.Where(e => e.IsNew).Select(e => e.Key).ToArray());
            Assert.AreEqual(91, XgKnowledge.Entries.Count(e => !e.IsNew));
            var groups = new[] { V4Learning, V4Architecture, V4Data };
            for (int category = 0; category < groups.Length; category++)
            foreach (var key in groups[category])
            {
                var entry = XgKnowledge.Find(key);
                Assert.IsNotNull(entry, key);
                Assert.IsTrue(entry.IsNew, key);
                Assert.AreEqual(category, entry.Category, key);
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.ChineseUsage), key + " Chinese usage");
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.EnglishUsage), key + " English usage");
                Assert.AreEqual(entry.Key, entry.Name(false), key);
                Assert.AreEqual(entry.English, entry.Name(true), key);
                Assert.AreEqual(entry.ChineseBody, entry.Body(false), key);
                Assert.AreEqual(entry.EnglishBody, entry.Body(true), key);
            }
            CollectionAssert.AreEqual(new[] { 63, 30, 15, 24 }, Enumerable.Range(0, 4).Select(c => XgKnowledge.Entries.Count(e => e.Category == c)).ToArray());
        }
        [Test] public void AbruptSwitchIsNewSearchableAndLinkedBothWays()
        {
            var entry = XgKnowledge.Find("突然开关");
            Assert.IsNotNull(entry); Assert.IsTrue(entry.IsNew); Assert.AreEqual(0, entry.Category);
            Assert.AreEqual("Abrupt switch", entry.English);
            Assert.IsNotEmpty(entry.ChineseUsage); Assert.IsNotEmpty(entry.EnglishUsage);
            foreach (var key in new[] { "平滑开关", "阶跃开关" })
            {
                CollectionAssert.Contains(entry.Related, key);
                Assert.AreEqual(entry.Key, XgKnowledge.Find(key).Related[0]);
            }
            Assert.IsTrue(entry.Matches("突然开关")); Assert.IsTrue(entry.Matches("ABRUPT SWITCH"));
        }
        [Test] public void HardwareTermsRemainTheOriginalTwentyFourAndAreNotMarkedNew()
        {
            var hardware = XgKnowledge.Entries.Where(e => e.Category == 3).ToArray();
            CollectionAssert.AreEquivalent(OriginalHardware, hardware.Select(e => e.Key).ToArray());
            Assert.IsTrue(hardware.All(e => !e.IsNew));
        }
        [Test] public void ExistingConceptsLinkToNewExplanationsWithoutDanglingLinks()
        {
            foreach (var key in new[] { "阶跃开关", "平滑开关", "整流激活" })
                CollectionAssert.Contains(XgKnowledge.Find(key).Related, "激活函数", key);
            foreach (var key in new[] { "欠拟合", "正则化", "泛化" })
                CollectionAssert.Contains(XgKnowledge.Find("过拟合").Related, key);
            foreach (var key in new[] { "交叉熵", "梯度下降" })
                CollectionAssert.Contains(XgKnowledge.Find("损失函数").Related, key);
            CollectionAssert.Contains(XgKnowledge.Find("激活函数").Related, "归一化指数");
            CollectionAssert.Contains(XgKnowledge.Find("归一化指数").Related, "交叉熵");
        }
        [Test] public void EveryNewTermIsSearchableByBothNamesAndBodyWithoutLanguageSwitching()
        {
            foreach (var entry in XgKnowledge.Entries.Where(e => e.IsNew))
            {
                Assert.IsTrue(entry.Matches("  " + entry.Key + "  "), entry.Key);
                Assert.IsTrue(entry.Matches("  " + entry.English.ToUpperInvariant() + "  "), entry.Key);
                Assert.IsTrue(entry.Matches(entry.ChineseBody), entry.Key);
                Assert.IsTrue(entry.Matches(entry.EnglishBody), entry.Key);
                Assert.IsTrue(entry.Matches(entry.ChineseUsage), entry.Key);
                Assert.IsTrue(entry.Matches(entry.EnglishUsage), entry.Key);
                Assert.IsFalse(entry.Matches("no_matching_v4_term_123"), entry.Key);
            }
            Assert.IsTrue(XgKnowledge.Find("归一化指数").Matches("softmax"));
            Assert.IsTrue(XgKnowledge.Find("字词单位").Matches("token"));
        }
        [Test] public void SearchHandlesBothLanguagesWhitespaceAndCase()
        {
            var compute = XgKnowledge.Find("算力");
            Assert.IsTrue(compute.Matches("算力"));
            Assert.IsTrue(compute.Matches("  COMPUTE  "));
            Assert.IsTrue(compute.Matches("bandwidth"));
            Assert.IsTrue(compute.Matches(" "));
            Assert.IsFalse(compute.Matches("no_matching_term_123"));
        }
        [Test] public void EraAndScalingCaveatsArePresentInBothLanguages()
        {
            StringAssert.Contains("不保证", XgKnowledge.Find("算力").ChineseBody);
            StringAssert.Contains("does not guarantee", XgKnowledge.Find("算力").EnglishBody);
            foreach (var key in new[] { "多路注意", "位置标记", "自研新脑", "自注意力" })
            {
                StringAssert.Contains("2017", XgKnowledge.Find(key).ChineseBody);
                StringAssert.Contains("2017", XgKnowledge.Find(key).EnglishBody);
            }
            foreach (var english in new[] { false, true })
            {
                StringAssert.Contains("2014", XgKnowledge.Find("生成对抗网络").Body(english));
                StringAssert.Contains("2016", XgKnowledge.Find("强化学习").Body(english));
            }
        }
    }
}
