using System;

namespace LingGuangV05.XingGuang
{
    // Optional diagnostic bridge. The simulation assembly remains independent of Unity.
    // Capture tooling owns the callbacks; normal play has no profiler subscriptions.
    public sealed class XgSimulationMarker
    {
        public static Action<string> Begin;
        public static Action End;
        readonly string name;
        public XgSimulationMarker(string name) { this.name = name; }
        public Scope Auto()
        {
            var begin = Begin;
            var end = End;
            if (begin == null || end == null) return default;
            begin(name);
            return new Scope(end);
        }
        public readonly struct Scope : IDisposable
        {
            readonly Action end;
            public Scope(Action end) { this.end = end; }
            public void Dispose() { end?.Invoke(); }
        }
    }
}
