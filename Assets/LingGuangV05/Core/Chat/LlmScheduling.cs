using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LingGuangV05.Core.Chat
{
    /// <summary>Who is speaking. Each seat keeps its own server slot so its prompt cache stays warm between turns.</summary>
    public enum LlmSeat { LingGuang = 0, Girlfriend = 1, Others = 2 }

    /// <summary>Visible: the player is waiting for this reply. Background: nobody is watching (judges, comments, memory).</summary>
    public enum LlmLane { Visible = 0, Background = 1 }

    /// <summary>How the owned model service is started. Each failed start falls to the next, more conservative mode.</summary>
    public enum LlmLaunchMode { Gpu = 0, GpuPlainCache = 1, Cpu = 2 }

    /// <summary>
    /// Pure policy for the local model service (LocalLlm): service flags, slot routing, queue priority and timeouts.
    /// Kept free of Unity so the rules can be tested on their own.
    /// </summary>
    public static class LlmScheduling
    {
        /// <summary>Context per slot. The largest real request (girlfriend prompt, 12 lines + few-shots) is about 1.1k tokens.</summary>
        public const int SlotContext = 4096;
        public const int NormalSlots = 3, ParallelJudgingSlots = 4;
        /// <summary>Host-RAM prompt cache: idle slots and older prompts are parked here and restored on a match.</summary>
        public const int CacheRamMiB = 2048;
        /// <summary>
        /// Prompt-cache checkpoints. The retired Qwen3.5 hybrid (recurrent + attention) model could only resume from a
        /// saved checkpoint, so these kept every turn's prefix reusable. Qwen3-1.7B is a dense transformer and reuses
        /// the plain KV prefix, so the setting is no longer required; it is kept (and not passed as a flag) until a
        /// measured run shows it buys nothing.
        /// </summary>
        public const int CheckpointsPerSlot = 6, CheckpointSpacing = 64;
        public const float VisibleTimeout = 20, BackgroundTimeout = 40, SlowFactor = 3;

        /// <summary>Slots the owned server runs with. The QA harness keeps its historic 1 / 4 layout.</summary>
        public static int SlotCount(bool parallelJudging, bool qaInstance) =>
            parallelJudging ? ParallelJudgingSlots : qaInstance ? 1 : NormalSlots;

        /// <summary>Requests are pinned to fixed slots only when the server has at least the three character slots.</summary>
        public static bool Routed(int slotCount) => slotCount >= NormalSlots;

        public const int Wait = -2, ServerChoice = -1;

        /// <summary>The fixed slot of a seat on a routed server.</summary>
        public static int SeatSlot(LlmSeat seat, int slotCount) => Math.Max(0, Math.Min((int)seat, slotCount - 1));

        /// <summary>
        /// The slot a request should run in now: a slot index to pin, <see cref="ServerChoice"/> on an unrouted
        /// service (the server picks), or <see cref="Wait"/>. On a routed server a character's requests (any lane)
        /// use the character's slot; seat Others uses slot 2 when visible and, when in the background, the slots from
        /// the last down to 2 (3 and 2 when parallel judging runs four). <paramref name="pinned"/> forces a slot.
        /// </summary>
        public static int PickSlot(LlmSeat seat, LlmLane lane, int slotCount, bool routed, int activeTotal, Func<int, bool> slotBusy, int pinned = -1)
        {
            if (!routed) return activeTotal < Math.Max(1, slotCount) ? ServerChoice : Wait;
            if (pinned >= 0 && pinned < slotCount) return slotBusy(pinned) ? Wait : pinned;
            if (lane == LlmLane.Visible || seat != LlmSeat.Others)
            {
                int own = SeatSlot(seat, slotCount);
                return slotBusy(own) ? Wait : own;
            }
            for (int slot = slotCount - 1; slot >= (int)LlmSeat.Others; slot--)
                if (!slotBusy(slot)) return slot;
            return Wait;
        }

        /// <summary>Background work only starts when no visible reply is waiting or running and the player is not typing.</summary>
        public static bool BackgroundMayStart(int visibleQueued, int visibleActive, bool playerTyping) =>
            visibleQueued == 0 && visibleActive == 0 && !playerTyping;

        /// <summary>How many judges may run at once: one per background slot when routed, else the historic 1 / 3.</summary>
        public static int JudgeLimit(int slotCount, bool routed) =>
            routed ? Math.Max(1, slotCount - (int)LlmSeat.Others) : slotCount >= ParallelJudgingSlots ? 3 : 1;

        /// <summary>
        /// Which background job goes next: the older of the judge queue head and the background chat head.
        /// Returns 0 for the judge, 1 for the chat, -1 for neither.
        /// </summary>
        public static int OlderHead(bool judgeAvailable, float judgeQueuedAt, bool chatAvailable, float chatQueuedAt)
        {
            if (judgeAvailable && chatAvailable) return judgeQueuedAt <= chatQueuedAt ? 0 : 1;
            return judgeAvailable ? 0 : chatAvailable ? 1 : -1;
        }

        /// <summary>Total seconds a request may take from being queued until its reply; slow hardware gets more.</summary>
        public static float Timeout(LlmLane lane, bool slow) =>
            (lane == LlmLane.Visible ? VisibleTimeout : BackgroundTimeout) * (slow ? SlowFactor : 1);

        /// <summary>
        /// llama.cpp server-style arguments for the in-process LlamaLib service (LLM for Unity parses them with
        /// llama.cpp's own server parser). GPU modes leave the layer count unset so llama.cpp's --fit offloads as many
        /// layers as fit in video memory and keeps the rest on the CPU (fit only adjusts arguments that were not given).
        /// GpuPlainCache drops the quantized KV cache for GPUs whose flash attention cannot take it; Cpu uses no GPU.
        /// No host / port / log file: the service is called directly, and its log comes through LlamaLib's callback.
        /// </summary>
        public static string ServiceArgs(LlmLaunchMode mode, string model, int slots)
        {
            var sb = new StringBuilder();
            sb.Append("-m \"").Append(model).Append('"')
              .Append(" -c ").Append((SlotContext * slots).ToString(CultureInfo.InvariantCulture))
              .Append(" -np ").Append(slots.ToString(CultureInfo.InvariantCulture));
            if (mode == LlmLaunchMode.Cpu) sb.Append(" --n-gpu-layers 0 --device none --fit off");
            else sb.Append(" --fit on --fit-ctx ").Append(SlotContext.ToString(CultureInfo.InvariantCulture));
            sb.Append(" --flash-attn auto");
            // Leave cores available for the Unity main/render threads and audio mixer.
            int threads = Math.Max(1, Math.Min(6, Environment.ProcessorCount / 2));
            sb.Append(" --threads ").Append(threads).Append(" --threads-batch ").Append(threads);
            if (mode != LlmLaunchMode.GpuPlainCache) sb.Append(" --cache-type-k q8_0 --cache-type-v q8_0");
            sb.Append(" --cache-reuse 256 --cache-ram ").Append(CacheRamMiB.ToString(CultureInfo.InvariantCulture))
              // LlamaLib v2.0.5's llama.cpp (2026-03) has no --checkpoint-min-step: it spaces checkpoints 64 tokens
              // apart itself, which is CheckpointSpacing, so the flag would change nothing.
              .Append(" --ctx-checkpoints ").Append(CheckpointsPerSlot.ToString(CultureInfo.InvariantCulture))
              .Append(" --jinja --reasoning-budget 0");
            return sb.ToString();
        }

        /// <summary>The next, more conservative launch mode after a start failed; false when none is left.</summary>
        public static bool Fallback(LlmLaunchMode mode, out LlmLaunchMode next)
        {
            next = mode == LlmLaunchMode.Gpu ? LlmLaunchMode.GpuPlainCache : LlmLaunchMode.Cpu;
            return mode != LlmLaunchMode.Cpu;
        }

        static readonly Regex Offloaded = new Regex(@"offloaded (\d+)/(\d+) layers to GPU", RegexOptions.CultureInvariant);

        /// <summary>Reads llama.cpp's "offloaded N/M layers to GPU" log line (arrives through LlamaLib's log callback).</summary>
        public static bool TryParseOffload(string line, out int onGpu, out int total)
        {
            onGpu = total = 0;
            if (string.IsNullOrEmpty(line)) return false;
            var m = Offloaded.Match(line);
            if (!m.Success) return false;
            onGpu = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            total = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>The empty reasoning block that closes thinking before the assistant's answer (Qwen chat format).</summary>
        public const string ClosedThinking = "<think>\n\n</think>\n\n";
        /// <summary>What the assistant turn starts with when the model should reason first.</summary>
        public const string OpenThinking = "<think>\n";

        static readonly Regex TrailingClosedThinking = new Regex(@"<think>\s*</think>\s*$", RegexOptions.CultureInvariant);

        /// <summary>
        /// The service applies the chat template with thinking off (one setting for every slot). LlamaLib's template
        /// leaves the closed block as "&lt;think&gt;\n&lt;/think&gt;" with no blank line after it, so the model's first token
        /// would be "\n\n" (a one-token judge would read that instead of 是/否): it is rewritten to
        /// <see cref="ClosedThinking"/>. A request that wants thinking (design v1.1 §11.5: stage 6's full open) gets
        /// <see cref="OpenThinking"/> instead. A prompt without the block is left alone.
        /// </summary>
        public static string WithThinking(string templated, bool thinking)
        {
            if (string.IsNullOrEmpty(templated)) return templated;
            var m = TrailingClosedThinking.Match(templated);
            if (!m.Success) return templated;
            return templated.Substring(0, m.Index) + (thinking ? OpenThinking : ClosedThinking);
        }

        /// <summary>Player-facing note for a model that does not run fully on the GPU; empty when it does.</summary>
        public static string SlowNotice(LlmLaunchMode mode, int onGpu, int total, bool english)
        {
            if (mode == LlmLaunchMode.Cpu)
                return english ? "Not enough video memory: running in system memory, replies will be slower." : "显存不够，只能在内存里跑，会慢一些";
            if (total > 0 && onGpu < total)
                return english ? "Not enough video memory: part of the model runs in system memory, replies will be slower." : "显存不够，部分在内存里跑，会慢一些";
            return "";
        }
    }
}
