using System;
using System.Collections.Generic;
using System.Linq;
using LingGuangV05.Core.Chat;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>The YY face catalog, message parsing and the girlfriend grammar alternation.</summary>
    public sealed class YYFacesTests
    {
        static readonly string[] Required =
        {
            "微笑", "撇嘴", "色", "发呆", "得意", "流泪", "害羞", "闭嘴", "睡", "大哭", "尴尬", "发怒", "调皮", "呲牙", "惊讶",
            "难过", "抠鼻", "再见", "偷笑", "可爱", "白眼", "傲慢", "困", "惊恐", "流汗", "憨笑", "奋斗", "疑问", "嘘", "晕", "衰",
            "敲打", "擦汗", "鼓掌", "坏笑", "哈欠", "鄙视", "委屈", "快哭了", "阴险", "亲亲", "可怜", "玫瑰", "爱心", "心碎", "拥抱",
            "强", "弱", "握手", "胜利", "OK", "月亮", "太阳",
        };

        [Test]
        public void CatalogHasEveryRequiredFaceInOrderWithUniqueNames()
        {
            Assert.AreEqual(Required, YYFaces.All.Select(f => f.Zh).ToArray());
            Assert.AreEqual(YYFaces.Count, YYFaces.All.Select(f => f.Id).Distinct().Count());
            Assert.AreEqual(YYFaces.Count, YYFaces.All.Select(f => f.En).Distinct().Count());
            for (int i = 0; i < YYFaces.Count; i++)
            {
                var face = YYFaces.All[i];
                Assert.AreEqual(i, face.Index);
                Assert.IsTrue(face.Id.All(c => c < 128 && !char.IsWhiteSpace(c)), face.Id);
                Assert.AreEqual("[" + face.Zh + "]", face.Code);
                Assert.IsFalse(string.IsNullOrWhiteSpace(face.En));
                Assert.LessOrEqual(face.Zh.Length, YYFaces.MaxNameLength);
            }
        }

        [Test]
        public void FindAcceptsCodesNamesAndEnglishAliases()
        {
            Assert.AreSame(YYFaces.Find("[微笑]"), YYFaces.Find("微笑"));
            Assert.AreSame(YYFaces.Find("[微笑]"), YYFaces.Find("[Smile]"));
            Assert.AreSame(YYFaces.Find("[强]"), YYFaces.Find("thumbsup"));
            Assert.AreEqual("[ThumbsUp]", YYFaces.Find("[强]").EnAlias);
            Assert.IsTrue(YYFaces.IsKnown("[OK]"));
            Assert.IsTrue(YYFaces.IsKnown("[快哭了]"));
            Assert.IsFalse(YYFaces.IsKnown("[图片]"));
            Assert.IsFalse(YYFaces.IsKnown("[]"));
            Assert.IsFalse(YYFaces.IsKnown(null));
        }

        [Test]
        public void ParseSplitsTextAndFacesAndKeepsUnknownCodesAsText()
        {
            var runs = YYFaces.Parse("你好[微笑]今天[图片]吃了吗[呲牙][呲牙]");
            Assert.AreEqual(5, runs.Count);
            Assert.AreEqual("你好", runs[0].Text);
            Assert.AreEqual("微笑", runs[1].Face.Zh);
            Assert.AreEqual("今天[图片]吃了吗", runs[2].Text);
            Assert.IsFalse(runs[2].IsFace);
            Assert.AreEqual("呲牙", runs[3].Face.Zh);
            Assert.AreSame(runs[3].Face, runs[4].Face);
            Assert.AreEqual("[呲牙][呲牙]", string.Concat(YYFaces.Parse("[呲牙][呲牙]").Select(r => r.ToString())));
            Assert.AreEqual(2, YYFaces.CountFaces("[呲牙][呲牙]"));
        }

        [Test]
        public void ParseHandlesEdgeCases()
        {
            Assert.AreEqual(0, YYFaces.Parse(null).Count);
            Assert.AreEqual(0, YYFaces.Parse("").Count);
            Assert.AreEqual("[[微笑", YYFaces.Parse("[[微笑").Single().Text);
            var nested = YYFaces.Parse("[[微笑]]");
            Assert.AreEqual(3, nested.Count);
            Assert.AreEqual("[", nested[0].Text);
            Assert.IsTrue(nested[1].IsFace);
            Assert.AreEqual("]", nested[2].Text);
            Assert.AreEqual("unclosed [微笑", YYFaces.Parse("unclosed [微笑").Single().Text);
            Assert.IsTrue(YYFaces.Parse("ok[Smile]").Last().IsFace);
            Assert.IsFalse(YYFaces.ContainsFace("[这是一个非常非常长的方括号里面的内容啊]"));
        }

        [Test]
        public void StripUnknownRemovesInventedFacesButKeepsRealOnesAndOtherBrackets()
        {
            Assert.AreEqual("好的[微笑]", YYFaces.StripUnknown("好的[微笑][狗头]"));
            Assert.AreEqual("ok [微笑] then", YYFaces.StripUnknown("ok [Smile] [doge] then"));
            Assert.AreEqual("见[图片 1]", YYFaces.StripUnknown("见[图片 1]"));
            Assert.AreEqual("[1]", YYFaces.StripUnknown("[1]"));
            Assert.AreEqual("", YYFaces.StripUnknown("[狗头]"));
            Assert.AreEqual("plain", YYFaces.StripUnknown("plain"));
        }

        [Test]
        public void StripFacesAndPlainText()
        {
            Assert.AreEqual("收到。", YYFaces.StripFaces("收到。[微笑]"));
            Assert.AreEqual("a b", YYFaces.StripFaces("a [微笑] b"));
            Assert.AreEqual("[图片]", YYFaces.StripFaces("[图片]"));
            Assert.AreEqual("hi [Smile]", YYFaces.ToPlainText("hi [微笑]", true));
            Assert.AreEqual("hi [微笑]", YYFaces.ToPlainText("hi [微笑]", false));
        }

        [Test]
        public void GbnfAlternationListsEveryCodeQuoted()
        {
            string alt = YYFaces.GbnfAlternation();
            var parts = alt.Split(new[] { " | " }, StringSplitOptions.None);
            Assert.AreEqual(YYFaces.Count, parts.Length);
            Assert.AreEqual("\"[微笑]\"", parts[0]);
            Assert.AreEqual("\"[太阳]\"", parts[parts.Length - 1]);
            foreach (var p in parts) StringAssert.IsMatch("^\"\\[[^\"\\\\]+\\]\"$", p);
            Assert.AreEqual("\"[微笑]\" | \"[OK]\"", YYFaces.GbnfAlternation(YYFaces.Subset("微笑", "[OK]", "[nope]")));
            Assert.AreEqual(YYFaces.Count * 2 + YYFaces.All.Sum(f => f.Zh.Length), YYFaces.Whitelist().Length);
        }

        [Test]
        public void StickersAreDatedByTheirMeme()
        {
            Assert.GreaterOrEqual(YYStickers.All.Count, 8);
            Assert.LessOrEqual(YYStickers.All.Count, 12);
            Assert.AreEqual(YYStickers.All.Count, YYStickers.All.Select(s => s.Id).Distinct().Count());
            Assert.IsTrue(YYStickers.All.All(s => !string.IsNullOrEmpty(s.Zh) && !string.IsNullOrEmpty(s.En)));
            var june = YYStickers.AvailableOn(new DateTime(2016, 6, 1)).Select(s => s.Id).ToList();
            Assert.IsFalse(june.Contains("lanshou"));
            Assert.IsFalse(june.Contains("honghuang"));
            var aug = YYStickers.AvailableOn(new DateTime(2016, 8, 20)).Select(s => s.Id).ToList();
            Assert.IsTrue(aug.Contains("honghuang"));
            Assert.IsFalse(aug.Contains("lanshou"));
            Assert.IsTrue(YYStickers.AvailableOn(new DateTime(2016, 10, 20)).Any(s => s.Id == "lanshou"));
            Assert.AreEqual("[斗图:蓝瘦香菇]", YYStickers.Find("lanshou").PreviewText(false));
        }
    }
}
