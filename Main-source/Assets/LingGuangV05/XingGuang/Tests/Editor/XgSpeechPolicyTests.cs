using NUnit.Framework;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgSpeechPolicyTests
    {
        [Test] public void SixStageSpeechHasATestablePolicySeparateFromPersonaProse()
        { Assert.That(typeof(XgSim).Assembly.GetType("LingGuangV05.XingGuang.XgSpeechPolicy"), Is.Not.Null); }
        [Test] public void LimitsMatchAllSixStages()
        {
            Assert.That(new[] { XgSpeechPolicy.ContextLimit(1), XgSpeechPolicy.ContextLimit(2), XgSpeechPolicy.ContextLimit(3), XgSpeechPolicy.ContextLimit(4), XgSpeechPolicy.ContextLimit(5), XgSpeechPolicy.ContextLimit(6) }, Is.EqualTo(new[] { 1, 1, 2, 14, 14, int.MaxValue }));
            Assert.That(new[] { XgSpeechPolicy.TokenLimit(1), XgSpeechPolicy.TokenLimit(2), XgSpeechPolicy.TokenLimit(3), XgSpeechPolicy.TokenLimit(4), XgSpeechPolicy.TokenLimit(5), XgSpeechPolicy.TokenLimit(6) }, Is.EqualTo(new[] { 2, 4, 24, 60, 100, 140 }));
            Assert.That(XgSpeechPolicy.Constrain("是的，我能说很长的话", 1), Is.EqualTo("是。"));
            Assert.That(XgSpeechPolicy.Constrain("不确定。需要更多信息", 2), Is.EqualTo("不确定。"));
            Assert.That(XgSpeechPolicy.Constrain("我刚刚学会说话但还很慢", 3).Length, Is.EqualTo(6));
        }
        [Test] public void AttentionWordsMustComeFromActualInputAndCannotInjectRichText()
        {
            Assert.That(XgSpeechPolicy.ReadFocus("关注：累\n回复：你累了。", "我有一点累", out var reply), Is.EqualTo("累"));
            Assert.That(reply, Is.EqualTo("你累了。"));
            Assert.That(XgSpeechPolicy.ReadFocus("关注：不存在\n回复：哦。", "实际输入", out reply), Is.Empty);
            var shown = XgSpeechPolicy.Underline("<color=red>我很累</color>", "累");
            Assert.That(shown, Does.Not.Contain("<color"));
            Assert.That(shown, Does.Contain("<u>累</u>"));
        }
    }
}
