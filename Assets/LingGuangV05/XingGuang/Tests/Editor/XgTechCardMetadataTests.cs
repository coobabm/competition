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
            StringAssert.Contains("20→60",XgTechTreeCopy.Effect(node,sim));
            StringAssert.Contains("容量满时暂停",XgTechTreeCopy.Detail(node,sim));
        }
    }
}
