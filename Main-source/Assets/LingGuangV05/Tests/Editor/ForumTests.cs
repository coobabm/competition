using System;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using LingGuangV05.Core.Forum;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    /// <summary>Design v1.1 §2, §9, §10: 摆渡贴吧 content and the two 老周.</summary>
    public sealed class ForumTests
    {
        static ForumLibrary Library()
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), TestContext.CurrentContext.TestDirectory })
                for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                {
                    string file = Path.Combine(dir.FullName, "Assets", "LingGuangV05", "Resources", "LingGuangV05", "Forum", "forum.json");
                    if (File.Exists(file)) return ForumLibrary.Parse(File.ReadAllText(file));
                }
            Assert.Fail("forum.json not found.");
            return null;
        }

        static ForumContext At(int stage, int month, int day) => new ForumContext { stage = stage, today = new DateTime(2016, month, day) };

        [Test]
        public void FloorSevenIsTheLongNumberInStageOneAndDeletedAfter()
        {
            var lib = Library();
            var build = lib.Get("my_build");
            Assert.IsTrue(build.mine);
            var seven = build.floors[6];
            Assert.AreEqual(ForumLibrary.LaoZhou, seven.author, "第 7 楼 is 周而复始");
            Assert.AreEqual(Prologue.LongNumber, seven.text);
            Assert.AreEqual(FloorView.Shown, ForumLibrary.View(seven, At(1, 6, 10)));
            Assert.AreEqual(FloorView.Deleted, ForumLibrary.View(seven, At(2, 7, 1)), "「该楼层已被删除」, not folded");
        }

        [Test]
        public void CluesAppearOnlyInTheirStage()
        {
            var lib = Library();
            Assert.IsFalse(ForumLibrary.Visible(lib.Get("ai_numbers"), At(2, 7, 20)));
            Assert.IsTrue(ForumLibrary.Visible(lib.Get("ai_numbers"), At(3, 8, 20)));
            Assert.IsTrue(lib.Get("ai_numbers").dead, "the 'me too' thread is a 404");
            Assert.IsFalse(ForumLibrary.Visible(lib.Get("gpu_caffe"), At(3, 8, 31)));
            Assert.IsTrue(ForumLibrary.Visible(lib.Get("gpu_caffe"), At(4, 9, 10)), "周而复始_ shows up in stage 4");
            Assert.AreEqual(lib.Users[ForumLibrary.LaoZhou].sign, lib.Users[ForumLibrary.ZhouNow].sign, "the same signature");
            Assert.IsFalse(ForumLibrary.Visible(lib.Get("ai_master"), At(6, 12, 28)));
            Assert.IsTrue(ForumLibrary.Visible(lib.Get("ai_master"), At(6, 12, 29)));
            Assert.IsTrue(lib.Get("ai_master").floors.Exists(f => f.author == ForumLibrary.LaoZhou && f.text == "开始了。"));
            var rule = lib.Get("my_rule");
            Assert.AreEqual(new DateTime(2016, 3, 15), rule.date, "the shutdown rule post, the day of the last AlphaGo game");
            Assert.AreEqual(1, rule.floors.Count, "0 replies");
            StringAssert.Contains("关机", rule.floors[0].text);
        }

        [Test]
        public void TheHelpPostOnlyExistsOnceWrittenAndRepliesTrickleIn()
        {
            var lib = Library();
            var help = lib.Get("help_bios");
            var c = At(1, 6, 5);
            Assert.IsFalse(ForumLibrary.Visible(help, c));
            c.helpPosted = true; c.secondsSinceHelp = 1;
            Assert.IsTrue(ForumLibrary.Visible(help, c));
            int shown = 0; foreach (var f in help.floors) if (ForumLibrary.View(f, c) == FloorView.Shown) shown++;
            Assert.AreEqual(1, shown, "only the post itself at first");
            c.secondsSinceHelp = 1000;
            shown = 0; foreach (var f in help.floors) if (ForumLibrary.View(f, c) == FloorView.Shown) shown++;
            Assert.AreEqual(4, shown);
            Assert.IsTrue(help.floors.Exists(f => f.text.Contains("CIH")));
            Assert.IsTrue(help.floors.Exists(f => f.text.Contains("熊猫烧香")));
        }

        [Test]
        public void TheTrapItemThreadWaitsForTheLabsFlag()
        {
            var lib = Library();
            var trap = lib.Get("crowd_trap");
            Assert.IsNotNull(trap);
            Assert.AreEqual("qc.trap", trap.requires);
            var c = At(2, 7, 1);
            Assert.IsFalse(ForumLibrary.Visible(trap, c), "no flag reader: hidden");
            c.flag = id => false;
            Assert.IsFalse(ForumLibrary.Visible(trap, c));
            c.flag = id => id == "qc.trap";
            Assert.IsTrue(ForumLibrary.Visible(trap, c), "after the first trap-item fine");
            StringAssert.Contains("金标题", trap.title);
            Assert.IsTrue(ForumLibrary.Visible(lib.Get("crowd_script"), At(1, 6, 1)), "threads without a flag are unaffected");
        }

        [Test]
        public void EveryLineHasEnglishAndNoLateMemes()
        {
            var lib = Library();
            var lines = new List<string>();
            foreach (var u in lib.Users.Values) { lines.Add(u.name); lines.Add(u.sign); }
            foreach (var t in lib.Threads) { lines.Add(t.title); foreach (var f in t.floors) { lines.Add(f.text); Assert.IsNotEmpty(f.textEn, t.id); } Assert.IsNotEmpty(t.titleEn, t.id); }
            Assert.IsEmpty(EraLexicon.Audit(lines));
        }

        static LaoZhouFacts Facts(int stage, string wall = "") => new LaoZhouFacts { stage = stage, wall = wall, wallName = wall, today = GameCalendar.FirstDay(stage).AddDays(10), money = 1200, temperature = 50, gpus = 2 };

        [Test]
        public void FutureLaoZhouAnswersADirectionFirstAndExplainsOnARepeat()
        {
            var f = Facts(1, "combo");
            string first = LaoZhouFuture.Reply("卡在 50% 了", f, 1, false);
            string second = LaoZhouFuture.Reply("卡在 50% 了", f, 2, false);
            StringAssert.Contains("一条直线", first);
            Assert.AreNotEqual(first, second);
            StringAssert.Contains("一层", second);
            foreach (var wall in new[] { "combo", "structure", "length", "degrade", "translation", "parallel" })
                for (int times = 1; times <= 3; times++)
                {
                    string reply = LaoZhouFuture.Reply("卡住了怎么办", Facts(3, wall), times, false);
                    Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(reply, "0\\.\\d|\\d{2,}"), "never a number: " + reply);
                }
            StringAssert.Contains("自己想", LaoZhouFuture.Reply("卡住了", Facts(5, "parallel"), 1, false));
        }

        [Test]
        public void FutureLaoZhouNeverAdmitsAndPlaysDumbOnTheClues()
        {
            Assert.AreEqual("猜的。你这种情况我见多了。", LaoZhouFuture.Reply("你怎么知道的？", Facts(2), 1, false));
            Assert.AreEqual("啥楼？我回过的帖多了去了。", LaoZhouFuture.Reply("第7楼那串数字是你发的吗", Facts(2), 1, false));
            StringAssert.Contains("不是我们", LaoZhouFuture.Reply("有人说他也被发了那串数字，404 了", Facts(3), 1, false));
            StringAssert.Contains("挺像的", LaoZhouFuture.Reply("周而复始_ 是你小号吗", Facts(4), 1, false));
            Assert.AreEqual("开始了。", LaoZhouFuture.Reply("Master 是谁", new LaoZhouFacts { stage = 6, today = new DateTime(2016, 12, 30) }, 1, false));
            StringAssert.Contains("自己定", LaoZhouFuture.Reply("关机那题标什么", Facts(1), 1, false));
            StringAssert.Contains("1070", LaoZhouFuture.Reply("买哪张显卡", Facts(2), 1, false));
            // A model that confesses is overruled by the offline answer.
            string clean = LaoZhouFuture.Clean("好吧我来自未来，sophon.dll 是我的", "你是谁", Facts(2), 1, false);
            Assert.AreEqual("猜的。你这种情况我见多了。", clean);
            Assert.AreEqual("调参，", LaoZhouFuture.Clean("调参，真香", "x", Facts(2), 1, false), "no post-2016 memes");
        }

        [Test]
        public void ThePromptCarriesTheGameStateButNoGoldenSettings()
        {
            var f = Facts(3, "length"); f.wallName = "长句失忆"; f.nanCount = 4; f.phenomenon = "梯度消失";
            string p = LaoZhouFuture.SystemPrompt(f, false);
            StringAssert.Contains("长句失忆", p); StringAssert.Contains("梯度消失", p); StringAssert.Contains("NaN 4", p);
            StringAssert.Contains("永远不承认", p); StringAssert.Contains("不要主动", p);
            StringAssert.Contains("真香", p, "the banned-meme rule is part of the prompt");
            Assert.IsFalse(p.Contains("LSTM") || p.Contains("0.01") || p.Contains("裁剪"), "the golden setting is never in what he sees");
        }

        [Test]
        public void TheOtherZhouDoesNotKnowYouOrTheFuture()
        {
            Assert.AreEqual("？你谁啊，我们认识吗", LaoZhouNow.Reply("你好", true, false));
            StringAssert.Contains("科幻", LaoZhouNow.Reply("你知道未来会有超级智能吗", false, false));
            StringAssert.Contains("batch", LaoZhouNow.Reply("显存不够怎么办", false, false));
            StringAssert.Contains("2016", LaoZhouNow.SystemPrompt(false));
        }

        [Test]
        public void ForumStateCountsQuestionsPerTopic()
        {
            var s = new ForumState();
            Assert.AreEqual(1, s.Ask("wall:combo"));
            Assert.AreEqual(2, s.Ask("wall:combo"));
            Assert.AreEqual(1, s.Ask("gpu"));
            Assert.AreEqual(2, s.Asked("wall:combo"));
            s.Conversation(ForumLibrary.LaoZhou).messages.Add(new ForumMessage { from = "me", text = "在吗" });
            Assert.AreEqual(1, s.Conversation(ForumLibrary.LaoZhou).messages.Count);
        }
    }
}
