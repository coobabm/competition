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

    /// <summary>How the owned llama-server is started. Each failed start falls to the next, more conservative mode.</summary>
    public enum LlmLaunchMode { Gpu = 0, GpuPlainCache = 1, Cpu = 2 }

    /// <summary>
    /// Pure policy for the local model service (LocalLlm): server flags, slot routing, queue priority and timeouts.
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
        /// Qwen3.5 is a hybrid (recurrent + attention) model: a prompt can only be resumed from a saved checkpoint, and
        /// llama.cpp saves one at the start of the last user message. A small spacing lets every turn leave one behind.
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
        /// llama-server arguments. GPU modes leave the layer count unset so llama.cpp's --fit offloads as many layers
        /// as fit in video memory and keeps the rest on the CPU (fit only adjusts arguments that were not given).
        /// GpuPlainCache drops the quantized KV cache for GPUs whose flash attention cannot take it; Cpu uses no GPU.
        /// </summary>
        public static string ServerArgs(LlmLaunchMode mode, string model, int port, int slots, string logPath = null)
        {
            var sb = new StringBuilder();
            sb.Append("-m \"").Append(model).Append("\" --host 127.0.0.1 --port ").Append(port.ToString(CultureInfo.InvariantCulture))
              .Append(" -c ").Append((SlotContext * slots).ToString(CultureInfo.InvariantCulture))
              .Append(" -np ").Append(slots.ToString(CultureInfo.InvariantCulture));
            if (mode == LlmLaunchMode.Cpu) sb.Append(" --n-gpu-layers 0 --device none --fit off");
            else sb.Append(" --fit on --fit-ctx ").Append(SlotContext.ToString(CultureInfo.InvariantCulture));
            sb.Append(" --flash-attn auto");
            if (mode != LlmLaunchMode.GpuPlainCache) sb.Append(" --cache-type-k q8_0 --cache-type-v q8_0");
            sb.Append(" --cache-reuse 256 --cache-ram ").Append(CacheRamMiB.ToString(CultureInfo.InvariantCulture))
              .Append(" --ctx-checkpoints ").Append(CheckpointsPerSlot.ToString(CultureInfo.InvariantCulture))
              .Append(" --checkpoint-min-step ").Append(CheckpointSpacing.ToString(CultureInfo.InvariantCulture))
              .Append(" --jinja --reasoning-budget 0")
              // Verbosity 4 is where this build prints "offloaded N/M layers to GPU"; the output is read and dropped.
              .Append(" --verbosity 4");
            if (!string.IsNullOrEmpty(logPath)) sb.Append(" --log-file \"").Append(logPath).Append('"');
            return sb.ToString();
        }

        /// <summary>The next, more conservative launch mode after a start failed; false when none is left.</summary>
        public static bool Fallback(LlmLaunchMode mode, out LlmLaunchMode next)
        {
            next = mode == LlmLaunchMode.Gpu ? LlmLaunchMode.GpuPlainCache : LlmLaunchMode.Cpu;
            return mode != LlmLaunchMode.Cpu;
        }

        static readonly Regex Offloaded = new Regex(@"offloaded (\d+)/(\d+) layers to GPU", RegexOptions.CultureInvariant);

        /// <summary>Reads llama.cpp's "offloaded N/M layers to GPU" log line.</summary>
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

        /// <summary>Player-facing note for a model that does not run fully on the GPU; empty when it does.</summary>
        public static string SlowNotice(LlmLaunchMode mode, int onGpu, int total, bool english)
        {
            if (mode == LlmLaunchMode.Cpu)
                return english ? "Not enough video memory: running in system memory, replies will be slower." : "显存不够，只能在内存里跑，会慢一些";
            if (total > 0 && onGpu < total)
                return english ? "Not enough video memory: part of the model runs in system memory, replies will be slower." : "显存不够，部分在内存里跑，会慢一些";
            return "";
        }

        /// <summary>A chat body with the slot pin inserted (the body is a JSON object ending in '}').</summary>
        public static string WithSlot(string body, int slot) =>
            slot < 0 || string.IsNullOrEmpty(body) || body[body.Length - 1] != '}' ? body
                : body.Substring(0, body.Length - 1) + ",\"id_slot\":" + slot.ToString(CultureInfo.InvariantCulture) + "}";
    }
}
