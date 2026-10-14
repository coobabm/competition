using NUnit.Framework;

namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The training page's parameter count and its comparison with famous models.</summary>
    public sealed class XgParamScaleTests
    {
        [Test]
        public void WideningDoublesWidthAndQuadruplesParameters()
        {
            var run = new XgRun { arch = "lenet", depth = 2, width = 1 };
            Assert.AreEqual(XgSim.ParamsK(run) * 4, XgSim.ParamsKWith(run, 2, 2), 1e-9);
            Assert.AreEqual(XgSim.ParamsK(run) * 1.5, XgSim.ParamsKWith(run, 3, 1), 1e-9);
        }

        [Test]
        public void TheScaleNamesTheNearestFamousModel()
        {
            StringAssert.Contains("LeNet-5", XgSim.ParamScale(8, false));
            StringAssert.Contains("1/8", XgSim.ParamScale(7.5, false));
            StringAssert.Contains("差不多", XgSim.ParamScale(60, false));
            StringAssert.Contains("AlexNet", XgSim.ParamScale(61000, true));
            StringAssert.Contains("VGG-16", XgSim.ParamScale(138000, true));
            StringAssert.Contains("GNMT", XgSim.ParamScale(500000, true), "bigger than every landmark: the biggest one is the yardstick");
            Assert.AreEqual("", XgSim.ParamScale(0, false));
        }
    }
}
