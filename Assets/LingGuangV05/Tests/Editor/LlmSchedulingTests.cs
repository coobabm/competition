using System.Collections.Generic;
using System;
using LingGuangV05.Core.Chat;
using LingGuangV05.Core.Girlfriend;
using NUnit.Framework;

namespace LingGuangV05.Tests
{
    public sealed class LlmSchedulingTests
    {
        static System.Func<int, bool> Busy(params int[] slots) { var set = new HashSet<int>(slots); return set.Contains; }

        [Test]
        public void EachCharacterKeepsItsOwnSlot()
        {
            Assert.AreEqual(0, LlmScheduling.PickSlot(LlmSeat.LingGuang, LlmLane.Visible, 3, true, 0, Busy()));
            Assert.AreEqual(1, LlmScheduling.PickSlot(LlmSeat.Girlfriend, LlmLane.Visible, 3, true, 0, Busy()));
            Assert.AreEqual(2, LlmScheduling.PickSlot(LlmSeat.Others, LlmLane.Visible, 3, true, 0, Busy()));
            Assert.AreEqual(LlmScheduling.Wait, LlmScheduling.PickSlot(LlmSeat.LingGuang, LlmLane.Visible, 3, true, 1, Busy(0)), "its own slot busy: wait, never borrow another character's");
            Assert.AreEqual(1, LlmScheduling.PickSlot(LlmSeat.Girlfriend, LlmLane.Background, 3, true, 0, Busy(2)), "her proactive message still uses her slot");
        }

        [Test]
        public void BackgroundUsesTheSlotsPastTheCharacters()
        {
            Assert.AreEqual(2, LlmScheduling.PickSlot(LlmSeat.Others, LlmLane.Background, 3, true, 0, Busy()));
            Assert.AreEqual(LlmScheduling.Wait, LlmScheduling.PickSlot(LlmSeat.Others, LlmLane.Background, 3, true, 1, Busy(2)));
            Assert.AreEqual(3, LlmScheduling.PickSlot(LlmSeat.Others, LlmLane.Background, 4, true, 0, Busy()), "parallel judging: the extra slot first");
            Assert.AreEqual(2, LlmScheduling.PickSlot(LlmSeat.Others, LlmLane.Background, 4, true, 1, Busy(3)));
            Assert.AreEqual(LlmScheduling.Wait, LlmScheduling.PickSlot(LlmSeat.Others, LlmLane.Background, 4, true, 2, Busy(2, 3)), "never takes 灵光's or 林晴雯's slot");
            Assert.AreEqual(2, LlmScheduling.JudgeLimit(4, true));
            Assert.AreEqual(1, LlmScheduling.JudgeLimit(3, true));
        }

        [Test]
        public void PinnedWarmupWaitsForItsSlot()
        {
            Assert.AreEqual(1, LlmScheduling.PickSlot(LlmSeat.Girlfriend, LlmLane.Background, 3, true, 0, Busy(), 1));
            Assert.AreEqual(LlmScheduling.Wait, LlmScheduling.PickSlot(LlmSeat.LingGuang, LlmLane.Background, 3, true, 1, Busy(0), 0));
        }

        [Test]
        public void UnroutedServerChoosesAndOnlyCountsCapacity()
        {
            // An external server with one slot, or the QA harness: no id_slot is sent, the server picks.
            Assert.AreEqual(LlmScheduling.ServerChoice, LlmScheduling.PickSlot(LlmSeat.Girlfriend, LlmLane.Visible, 1, false, 0, Busy()));
            Assert.AreEqual(LlmScheduling.Wait, LlmScheduling.PickSlot(LlmSeat.Girlfriend, LlmLane.Visible, 1, false, 1, Busy()));
            Assert.AreEqual(LlmScheduling.ServerChoice, LlmScheduling.PickSlot(LlmSeat.Others, LlmLane.Background, 4, false, 3, Busy()));
            Assert.AreEqual(3, LlmScheduling.JudgeLimit(4, false), "the QA harness keeps three parallel judges");
            Assert.AreEqual(1, LlmScheduling.JudgeLimit(1, false));
            Assert.IsFalse(LlmScheduling.Routed(1));
            Assert.IsTrue(LlmScheduling.Routed(3));
        }

        [Test]
        public void SlotCounts()
        {
            Assert.AreEqual(3, LlmScheduling.SlotCount(false, false));
            Assert.AreEqual(4, LlmScheduling.SlotCount(true, false));
            Assert.AreEqual(1, LlmScheduling.SlotCount(false, true));
            Assert.AreEqual(4, LlmScheduling.SlotCount(true, true));
        }

        [Test]
        public void BackgroundWaitsForVisibleWorkAndTyping()
        {
            Assert.IsTrue(LlmScheduling.BackgroundMayStart(0, 0, false));
            Assert.IsFalse(LlmScheduling.BackgroundMayStart(1, 0, false), "a visible reply is queued");
            Assert.IsFalse(LlmScheduling.BackgroundMayStart(0, 1, false), "a visible reply is running");
            Assert.IsFalse(LlmScheduling.BackgroundMayStart(0, 0, true), "the player is typing");
            Assert.AreEqual(0, LlmScheduling.OlderHead(true, 1, true, 2));
            Assert.AreEqual(1, LlmScheduling.OlderHead(true, 3, true, 2));
            Assert.AreEqual(1, LlmScheduling.OlderHead(false, 0, true, 2));
            Assert.AreEqual(-1, LlmScheduling.OlderHead(false, 0, false, 0));
        }

        [Test]
        public void TimeoutsAndSlowHardware()
        {
            Assert.AreEqual(20f, LlmScheduling.Timeout(LlmLane.Visible, false));
            Assert.AreEqual(40f, LlmScheduling.Timeout(LlmLane.Background, false));
            Assert.Greater(LlmScheduling.Timeout(LlmLane.Visible, true), 20f);
        }

        [Test]
        public void ServerFlagsLetFitPlaceLayers()
        {
            string gpu = LlmScheduling.ServerArgs(LlmLaunchMode.Gpu, "/m/q.gguf", 18088, 3);
            StringAssert.Contains("-c 12288 -np 3", gpu);
            StringAssert.Contains("--fit on --fit-ctx 4096", gpu);
            StringAssert.DoesNotContain("-ngl", gpu);
            StringAssert.DoesNotContain("--n-gpu-layers", gpu, "a forced layer count would switch --fit off for layers");
            StringAssert.Contains("--cache-type-k q8_0 --cache-type-v q8_0", gpu);
            StringAssert.Contains("--flash-attn auto", gpu);
            StringAssert.Contains("--cache-reuse 256 --cache-ram 2048", gpu);
            StringAssert.Contains("--jinja --reasoning-budget 0", gpu);
            StringAssert.DoesNotContain("--log-file", gpu);
            StringAssert.DoesNotContain("q8_0", LlmScheduling.ServerArgs(LlmLaunchMode.GpuPlainCache, "m", 1, 3));
            string cpu = LlmScheduling.ServerArgs(LlmLaunchMode.Cpu, "m", 1, 4, "/tmp/x.log");
            StringAssert.Contains("--n-gpu-layers 0 --device none", cpu);
            StringAssert.Contains("-c 16384 -np 4", cpu);
            StringAssert.Contains("--log-file \"/tmp/x.log\"", cpu);
        }

        [Test]
        public void StartLadderEndsOnTheCpu()
        {
            Assert.IsTrue(LlmScheduling.Fallback(LlmLaunchMode.Gpu, out var next)); Assert.AreEqual(LlmLaunchMode.GpuPlainCache, next);
            Assert.IsTrue(LlmScheduling.Fallback(next, out next)); Assert.AreEqual(LlmLaunchMode.Cpu, next);
            Assert.IsFalse(LlmScheduling.Fallback(next, out _));
        }

        [Test]
        public void OffloadLineAndNotice()
        {
            Assert.IsTrue(LlmScheduling.TryParseOffload("0.03.29 I load_tensors: offloaded 9/34 layers to GPU", out int gpu, out int total));
            Assert.AreEqual(9, gpu); Assert.AreEqual(34, total);
            Assert.IsFalse(LlmScheduling.TryParseOffload("srv load_model: loading model", out _, out _));
            Assert.AreEqual("", LlmScheduling.SlowNotice(LlmLaunchMode.Gpu, 34, 34, false));
            Assert.AreEqual("", LlmScheduling.SlowNotice(LlmLaunchMode.Gpu, -1, -1, false), "unknown layout: say nothing");
            Assert.AreEqual("显存不够，部分在内存里跑，会慢一些", LlmScheduling.SlowNotice(LlmLaunchMode.Gpu, 9, 34, false));
            StringAssert.Contains("显存不够", LlmScheduling.SlowNotice(LlmLaunchMode.Cpu, 0, 0, false));
            StringAssert.Contains("video memory", LlmScheduling.SlowNotice(LlmLaunchMode.Gpu, 9, 34, true));
        }

        [Test]
        public void SlotPinIsAppendedToTheBody()
        {
            Assert.AreEqual("{\"a\":1,\"id_slot\":2}", LlmScheduling.WithSlot("{\"a\":1}", 2));
            Assert.AreEqual("{\"a\":1}", LlmScheduling.WithSlot("{\"a\":1}", -1));
        }
        [Test]
        public void StateRidesInTheLastUserTurnOnly()
        {
            var m = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("system", "S"),
                new KeyValuePair<string, string>("user", "a"),
                new KeyValuePair<string, string>("assistant", "b"),
                new KeyValuePair<string, string>("user", "c"),
            };
            LlmPromptLayout.AddState(m, "现在 22:30", false);
            Assert.AreEqual("S", m[0].Value);
            Assert.AreEqual("a", m[1].Value, "earlier user turns stay as sent, so next turn's prompt extends this one");
            StringAssert.StartsWith("【当前状态", m[3].Value);
            StringAssert.Contains("现在 22:30", m[3].Value);
            StringAssert.EndsWith("\nc", m[3].Value);
            var n = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("system", "S"), new KeyValuePair<string, string>("assistant", "b") };
            LlmPromptLayout.AddState(n, "x", true);
            Assert.AreEqual("user", n[2].Key, "no user turn to carry it: the state gets its own");
            LlmPromptLayout.AddState(n, "  ", true);
            Assert.AreEqual(3, n.Count);
        }

        [Test]
        public void HistoryWindowMovesInSteps()
        {
            Assert.AreEqual(0, LlmPromptLayout.WindowStart(20, 20));
            Assert.AreEqual(0, LlmPromptLayout.WindowStart(29, 20), "between steps the start stays put");
            Assert.AreEqual(10, LlmPromptLayout.WindowStart(30, 20));
            Assert.AreEqual(10, LlmPromptLayout.WindowStart(39, 20));
            for (int count = 0; count < 200; count++)
            {
                int start = LlmPromptLayout.WindowStart(count, 12), shown = count - start;
                Assert.That(shown, Is.EqualTo(count).Or.InRange(12, 17), "never fewer than the limit, at most limit + step - 1");
                Assert.AreEqual(0, start % 2, "user / reply pairs stay aligned");
            }
            Assert.AreEqual(4, LlmPromptLayout.WindowStart(5, 1), "tiny windows stay exact");
            Assert.AreEqual(3, LlmPromptLayout.WindowStart(5, 2));
        }

        [Test]
        public void GirlfriendStablePromptHasNoClock()
        {
            var s = new GirlfriendState();
            var at = GfNow.At(new DateTime(2016, 9, 27, 22, 30, 0), 1000);
            GirlfriendRules.Begin(s, at, 12345);
            var later = GfNow.At(new DateTime(2016, 9, 27, 23, 41, 0), 1500);
            Assert.AreEqual(GirlfriendPromptText.Stable(s, at, false), GirlfriendPromptText.Stable(s, later, false), "byte-identical between messages");
            StringAssert.DoesNotContain("22:30", GirlfriendPromptText.Stable(s, at, false));
            StringAssert.Contains("22:30", GirlfriendPromptText.State(s, at, GirlfriendRules.Schedule(s, at), false, ""));
            string whole = GirlfriendPromptText.System(s, at, GirlfriendRules.Schedule(s, at), false, "他刚上线");
            StringAssert.Contains("他刚上线", whole);
            StringAssert.Contains("只输出这一个 JSON", whole);
        }
    }
}
