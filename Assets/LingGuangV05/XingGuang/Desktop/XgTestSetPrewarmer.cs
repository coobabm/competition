using System.Diagnostics;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    // Opportunistic preparation only. A real request always retains its synchronous fallback.
    public sealed class XgTestSetPrewarmer : MonoBehaviour
    {
        XingGuangController owner;
        XgSim bound;
        static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker("Xg.TestSetIdlePreparation");
        public static void Attach(XingGuangController controller)
        {
            var worker = controller.GetComponent<XgTestSetPrewarmer>() ?? controller.gameObject.AddComponent<XgTestSetPrewarmer>();
            worker.owner = controller;
        }
        void LateUpdate()
        {
            var sim = owner != null ? owner.Sim : null;
            if (!ReferenceEquals(sim, bound)) { bound?.CancelTestSetPreparation(); bound = sim; }
            if (sim == null || !sim.UseBoard || sim.StagePresentationHeld || (!sim.WindowOpen && !sim.AnyEpochActive)) return;
            // One active job; prioritize a running round, otherwise the dataset currently being inspected/taught.
            var run = sim.S.vision.epochActive ? sim.S.vision : sim.S.sequence.epochActive ? sim.S.sequence : sim.Selected;
            if (run == null) return;
            using (Marker.Auto())
            {
                long started = Stopwatch.GetTimestamp();
                for (int i = 0; i < 32; i++)
                {
                    if (sim.PrepareTestSet(run.dataset, 1)) break;
                    if ((Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency >= 1d) break;
                }
            }
        }
        void OnDisable() { bound?.CancelTestSetPreparation(); bound = null; }
        void OnDestroy() { bound?.CancelTestSetPreparation(); bound = null; }
    }
}
