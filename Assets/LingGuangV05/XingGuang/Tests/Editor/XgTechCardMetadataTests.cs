using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgTechCardMetadataTests
    {
        [TestCase("mlp","MLP · 1986")]
        [TestCase("lenet","LeNet-5 · 1998")]
        [TestCase("shared.lr","LRS")]
        [TestCase("train.checkpoint","CKPT")]
        [TestCase("brain.window","")]
        public void MetadataIsSeparateFromTheName(string id,string expected)
        {
            var sim=new XgSim();var node=XgCatalog.Node(id);
            Assert.IsNotNull(node);
            Assert.AreEqual(expected,XgTechTreeCopy.Tag(node,sim));
            foreach(bool english in new[]{false,true})
            {
                sim.English=english;
                string name=XgTechTreeCopy.Name(node,sim);
                Assert.IsNotEmpty(name);
                Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(name,@"\d{4}"));
                StringAssert.DoesNotContain("工具",name);
            }
        }

        [TestCase("backprop","BP · 1986","反向传播")]
        [TestCase("attention","Attn · 2014","回看注意力")]
        [TestCase("label.brain","LM","本地判题")]
        [TestCase("cooler","","机箱风道")]
        public void ScienceReviewNamesAndTags(string id,string tag,string name)
        {
            var sim=new XgSim();var node=XgCatalog.Node(id)??System.Array.Find(XgSim.LabelNodes,n=>n.id==id);
            Assert.IsNotNull(node,id);
            Assert.AreEqual(tag,XgTechTreeCopy.Tag(node,sim));
            Assert.AreEqual(name,XgTechTreeCopy.Name(node,sim));
        }

        [Test]
        public void ScienceReviewCorrectionsStayCorrected()
        {
            var sim=new XgSim();
            Assert.AreEqual(2014,XgCatalog.Arch("attention").year,"one year for attention on the card and the architecture");
            StringAssert.Contains("输出 0",XgTechTreeCopy.Detail(XgCatalog.Node("dropout"),sim));
            StringAssert.DoesNotContain("暂停更新",XgTechTreeCopy.Detail(XgCatalog.Node("dropout"),sim));
            StringAssert.DoesNotContain("烧卡",XgTechTreeCopy.Card(XgCatalog.Node("cooler"),sim));
            Assert.AreSame(XgKnowledge.Find("反向传播"),XgTechTreeCopy.KnowledgeFor(XgCatalog.Node("backprop"),sim));
            Assert.AreSame(XgKnowledge.Find("机箱风道"),XgTechTreeCopy.KnowledgeFor(XgCatalog.Node("cooler"),sim));
            StringAssert.DoesNotContain("多层感知机",XgKnowledge.Find("多层感知机").ChineseBody,"no self-referential definition");
            StringAssert.DoesNotContain("偏旁",XgKnowledge.Find("特征").ChineseBody);
            StringAssert.Contains("1999",XgKnowledge.Find("门控记忆").ChineseBody);
            StringAssert.Contains("1999",XgKnowledge.Find("门控记忆").EnglishBody);
            foreach(var key in new[]{"循环网络","门控记忆","简化记忆","注意力","回看注意力","多路注意","位置标记","自研新脑","词向量","束搜索","子词切分","示范跟答","分步解题"})
                CollectionAssert.DoesNotContain(XgKnowledge.Find(key).Related,"卷积",key+" is a sequence entry, not convolution");
            foreach(var id in new[]{"shortcut","emergence"})
                StringAssert.IsMatch("20\\d\\d 年",XgPhenomena.Get(id).why,id+" carries its era footnote");
        }

        [Test]
        public void InWorldScienceCopyUsesNoLaterAiTerms()
        {
            var sim=new XgSim();sim.S.stage=6;sim.S.autoLabelRevealed=true;
            var lines=new System.Collections.Generic.List<string>();
            foreach(bool english in new[]{false,true})
            {
                sim.English=english;
                foreach(var n in System.Linq.Enumerable.Concat(XgCatalog.Nodes,XgSim.LabelNodes))
                {
                    if(n.tree=="atlas")continue;
                    lines.Add(XgTechTreeCopy.Card(n,sim));lines.Add(XgTechTreeCopy.Detail(n,sim));lines.Add(XgTechTreeCopy.Tag(n,sim));
                }
            }
            foreach(var e in XgKnowledge.Entries){lines.Add(e.Key);lines.Add(e.ChineseBody);lines.Add(e.EnglishBody);}
            foreach(var p in XgPhenomena.All){lines.Add(p.name);lines.Add(p.why);lines.Add(p.whyEn);}
            foreach(var a in XgCatalog.Archs){lines.Add(a.name);lines.Add(a.note);lines.Add(a.wire);}
            Assert.IsEmpty(LingGuangV05.Core.Era.EraLexicon.AuditAiTerms(lines));
        }

        [Test]
        public void ExperimentalTechIsNotPresentedAsAPublishedFuturePaper()
        {
            var sim=new XgSim();var node=XgCatalog.Node("transformer");
            Assert.AreEqual("自研 · 2016",XgTechTreeCopy.Tag(node,sim));
            sim.English=true;
            Assert.AreEqual("Research · 2016",XgTechTreeCopy.Tag(node,sim));
        }

        [Test]
        public void HiddenAutomationDoesNotLeakItsTag()
        {
            var sim=new XgSim();var node=XgSim.LabelNodes[1];
            Assert.AreEqual("label.auto",node.id);
            Assert.IsTrue(sim.NodeMystery(node));
            Assert.AreEqual("",XgTechTreeCopy.Tag(node,sim));
        }

        [Test]
        public void TransferDoesNotPromiseKeepingAllAccuracy()
        {
            var sim=new XgSim();var node=XgCatalog.Node("transfer");
            StringAssert.Contains("部分成果",XgTechTreeCopy.Effect(node,sim));
            StringAssert.DoesNotContain("脑型",XgTechTreeCopy.Card(node,sim));
            sim.English=true;
            StringAssert.Contains("some progress",XgTechTreeCopy.Effect(node,sim));
        }

        [Test]
        public void ReviewCapacityExplainsItsActualLimitRatherThanAccuracy()
        {
            var sim=new XgSim();
            var node=System.Array.Find(XgSim.LabelNodes,n=>n.id=="label.parallel");
            StringAssert.Contains("40→120",XgTechTreeCopy.Effect(node,sim));
            StringAssert.Contains("满了只跳过拿不准的题",XgTechTreeCopy.Detail(node,sim));
            StringAssert.DoesNotContain("暂停",XgTechTreeCopy.Detail(node,sim),"a full inbox no longer pauses automation");
        }
    }
}
