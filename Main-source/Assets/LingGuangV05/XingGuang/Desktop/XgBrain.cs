using System;
using System.Collections.Generic;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Live transport adapter, not a second simulation. Called before XgSim.Tick; never during offline catch-up.
    /// Prefetch ownership, stable tickets, inference results and all rewards remain in the pure Core.
    /// O(1) transport work per frame (at most three queued/in-flight requests), bounded by Core's per-desk buffer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class XgBrain : MonoBehaviour
    {
        XingGuangController controller;
        XgSim bound;
        LocalLlm transport;
        readonly Dictionary<long, XgJudgeTicket> pending = new Dictionary<long, XgJudgeTicket>();
        int bindingGeneration;
        bool paused;
        public int PendingCount => pending.Count;

        public void Bind(XingGuangController owner)
        {
            if (controller == owner && ReferenceEquals(bound, owner != null ? owner.Sim : null)) return;
            CancelPending("save-changed");
            if (bound != null) bound.BrainOnline = false;
            controller = owner;
            bound = owner != null ? owner.Sim : null;
            transport = LocalLlm.Instance;
        }

        public void TickBeforeSimulation()
        {
            if (controller == null || !isActiveAndEnabled) return;
            if (!ReferenceEquals(bound, controller.Sim)) Bind(controller);
            var sim = bound;
            if (sim == null) return;
            var current = LocalLlm.Instance;
            if (transport != current)
            {
                CancelPending("service-changed");
                transport = current;
            }
            bool powered = HardwareAvailable(sim);
            bool ready = powered && !sim.ProjectActive && transport != null && transport.JudgeReady && sim.Has("label.brain");
            sim.BrainOnline = ready;
            if (!powered)
            {
                CancelPending(paused ? "paused" : sim.OfflineSimulation ? "offline-simulation" : "power-unavailable");
                sim.BrainStatus = sim.English ? "Brain paused; no live inference" : "大脑已暂停；不发起真实推理";
                return;
            }
            if (sim.ProjectActive)
            {
                // Already-dispatched real replies may be cached, but no new inference or routing occurs.
                // Do not invalidate their binding generation just because research started.
                sim.BrainStatus = sim.English ? "Research occupies the GPU; brain is waiting" : "研发占用 GPU；大脑等待";
                return;
            }
            if (transport != null) transport.SetParallelJudging(sim.Has("label.parallel"));
            if (sim.Has("label.brain") && !ready)
                sim.BrainStatus = sim.English ? "Brain offline; checkpoint fallback" : "大脑离线；回退检查点";
            else if (ready && transport.ParallelUnavailable)
                sim.BrainStatus = sim.English ? "External service: sequential judging; not restarted" : "外部服务：保持串行，未重启";
            else if (ready)
                sim.BrainStatus = "";

            // The Core assigns clearly labelled checkpoint predictions if the live service is unavailable.
            sim.FillJudgmentBuffer();
            if (!ready || sim.ReviewFull) return;
            int limit = transport.ParallelSlots >= 4 && sim.Has("label.parallel") ? 3 : 1;
            while (pending.Count < limit && sim.TryTakeBrainTicket(out var ticket))
            {
                int generation = bindingGeneration;
                var service = transport;
                pending[ticket.cardId] = ticket;
                bool accepted = service.Judge(ticket.prompt,
                    (p, failure) => Complete(sim, ticket, generation, p, failure),
                    () => IsCurrent(sim, generation) && Powered(sim) && sim.DispatchBrainTicket(ticket, controller.Host),
                    this);
                if (accepted) continue;
                pending.Remove(ticket.cardId);
                sim.ReleaseBrainTicket(ticket);
                break;
            }
        }

        bool HardwareAvailable(XgSim sim) => !paused && !sim.OfflineSimulation && controller != null && controller.Host != null &&
            controller.Host.Blocker == null && controller.Host.Compute > 0;

        bool Powered(XgSim sim) => HardwareAvailable(sim) && !sim.ProjectActive;

        bool IsCurrent(XgSim sim, int generation) => isActiveAndEnabled && generation == bindingGeneration &&
            controller != null && ReferenceEquals(sim, controller.Sim) && ReferenceEquals(sim, bound);

        void Complete(XgSim sim, XgJudgeTicket ticket, int generation, double? probability, string failure)
        {
            if (!IsCurrent(sim, generation)) return;
            pending.Remove(ticket.cardId);
            if (!HardwareAvailable(sim)) { probability = null; failure = "power-unavailable"; }
            // A ticket rejected before it actually dispatched has nothing to complete or bill.
            if (!sim.CompleteBrainTicket(ticket, probability, failure)) sim.ReleaseBrainTicket(ticket);
        }

        void CancelPending(string reason)
        {
            bindingGeneration++;
            transport?.CancelJudges(this);
            if (bound != null)
                foreach (var ticket in pending.Values)
                    if (!bound.ReleaseBrainTicket(ticket)) bound.CompleteBrainTicket(ticket, null, reason);
            pending.Clear();
        }

        public void Suspend(bool value)
        {
            paused = value;
            if (!value) return;
            CancelPending("paused");
            if (bound != null) bound.BrainOnline = false;
        }

        void OnApplicationPause(bool value) { Suspend(value); }
        void OnDisable() { CancelPending("disabled"); if (bound != null) bound.BrainOnline = false; }
        void OnDestroy() { CancelPending("destroyed"); }
    }
}
