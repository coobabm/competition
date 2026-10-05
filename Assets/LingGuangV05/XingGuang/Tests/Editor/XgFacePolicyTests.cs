using NUnit.Framework;
namespace LingGuangV05.XingGuang.Tests
{
    /// <summary>The AI only uses YY bracket faces from stage 4 on.</summary>
    public sealed class XgFacePolicyTests
    {
        [Test] public void FacesAreStrippedBeforeStageFour()
        {
            Assert.That(XgSpeechPolicy.Constrain("[微笑]是的", 1), Is.EqualTo("是。"));
            Assert.That(XgSpeechPolicy.Constrain("[呲牙]我在学", 3), Is.EqualTo("我在学"));
            Assert.That(XgSpeechPolicy.Constrain("你好[微笑]呀[图片]", 3), Is.EqualTo("你好呀[图片"));
        }

        [Test] public void FacesStayFromStageFour()
        {
            Assert.That(XgSpeechPolicy.FirstFaceStage, Is.EqualTo(4));
            Assert.That(XgSpeechPolicy.Constrain("收到[微笑]", 4), Is.EqualTo("收到[微笑]"));
            Assert.That(XgSpeechPolicy.Constrain("ok [强]", 6), Is.EqualTo("ok [强]"));
        }
    }
}
