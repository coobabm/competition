#if UNITY_EDITOR || NATIVE_LIFE_WINDOW_QA
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

// Scene reference only; no runtime initialization hook. Absent from ordinary player compilations.
public sealed class NativeLifeWindowProbe : MonoBehaviour
{
    [DllImport("user32.dll")] static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassNameW(IntPtr hwnd, StringBuilder name, int count);
    readonly XgLifeHostActivity.ActiveClock clock = new XgLifeHostActivity.ActiveClock();
    readonly System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
    XgSim sim;
    StreamWriter output;
    string directory, phase = "startup";
    IntPtr window;
    int processId, errors;
    double previous, activeSeconds;
    bool slept, done;

    void Start()
    {
        try
        {
            directory = Environment.GetEnvironmentVariable("LINGGUANG_NATIVE_QA_RUN");
            if (string.IsNullOrEmpty(directory)) throw new InvalidOperationException("Missing isolated run directory");
            directory = Path.GetFullPath(directory);
            string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!directory.StartsWith(temp, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(directory).StartsWith("LingGuang-NativeLifeRun-", StringComparison.Ordinal))
                throw new InvalidOperationException("Run directory must be a dedicated system-temp child");
            Directory.CreateDirectory(directory);
            if (FindAnyObjectByType<ChapterOneRuntime>() != null) throw new InvalidOperationException("Game runtime must not exist in probe scene");
            if (Application.companyName != "LingGuangLocalVerification" || !Application.productName.StartsWith("NativeLifeWindowQA-", StringComparison.Ordinal))
                throw new InvalidOperationException("Player save identity is not isolated");
            Application.logMessageReceived += OnLog;
            output = new StreamWriter(Path.Combine(directory, "frames.jsonl"), false, new UTF8Encoding(false)) { AutoFlush = true };
            sim = new XgSim(); // Memory only: never load or save, never instantiate a gameplay runtime.
            processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            watch.Start();
            File.WriteAllText(Path.Combine(directory, "identity.json"), JsonUtility.ToJson(new Identity {
                processId = processId, company = Application.companyName, product = Application.productName,
                persistentPath = Application.persistentDataPath, runInBackground = Application.runInBackground }, true));
        }
        catch (Exception e) { Debug.LogException(e); Finish(false, e.ToString()); }
    }

    bool ResolveWindow()
    {
        if (window == IntPtr.Zero) using (var p = System.Diagnostics.Process.GetCurrentProcess()) window = p.MainWindowHandle;
        if (window == IntPtr.Zero || !IsWindow(window)) { window = IntPtr.Zero; return false; }
        GetWindowThreadProcessId(window, out uint owner);
        var name = new StringBuilder(64);
        if (owner != (uint)processId || GetClassNameW(window, name, name.Capacity) <= 0 || name.ToString() != "UnityWndClass")
            throw new InvalidOperationException("Native HWND ownership/class mismatch");
        return true;
    }

    void Update()
    {
        if (done || sim == null) return;
        try
        {
            if (watch.Elapsed.TotalSeconds > 100) { Finish(false, "Watchdog expired"); return; }
            if (FindAnyObjectByType<ChapterOneRuntime>() != null) throw new InvalidOperationException("Unexpected gameplay runtime");
            string command = Path.Combine(directory, "phase.txt");
            try { if (File.Exists(command)) phase = File.ReadAllText(command).Trim(); } catch (IOException) { }
            if (phase == "quit") { Finish(errors == 0, "Completed; external assertions are authoritative"); return; }
            if (!ResolveWindow()) return;
            if (phase == "longframe" && !slept) { slept = true; System.Threading.Thread.Sleep(1200); }
            double now = watch.Elapsed.TotalSeconds, elapsed = now - previous;
            previous = now;
            bool nativeBefore = IsIconic(window), bridgeBefore = PlayerWindowState.IsMinimized;
            clock.Apply(sim, Time.unscaledDeltaTime);
            bool bridgeAfter = PlayerWindowState.IsMinimized, nativeAfter = IsIconic(window);
            if (sim.LifeEventActive) activeSeconds += elapsed;
            output.WriteLine(JsonUtility.ToJson(new Frame { frame = Time.frameCount, elapsed = now, wallDelta = elapsed,
                unityDelta = Time.unscaledDeltaTime, activeSeconds = activeSeconds, phase = phase, hwnd = window.ToInt64(),
                nativeBefore = nativeBefore, nativeAfter = nativeAfter, bridgeBefore = bridgeBefore, bridgeAfter = bridgeAfter,
                focused = Application.isFocused, active = sim.LifeEventActive, conflict = sim.LifeEventConflict }));
        }
        catch (Exception e) { Debug.LogException(e); Finish(false, e.ToString()); }
    }
    void OnLog(string message, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; }
    void Finish(bool ok, string message)
    {
        if (done) return;
        done = true;
        Application.logMessageReceived -= OnLog;
        if (output != null) { output.Dispose(); output = null; }
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            File.WriteAllText(Path.Combine(directory, "complete.json"), JsonUtility.ToJson(new Completion { ok = ok, errors = errors, message = message }, true));
        Application.Quit(ok ? 0 : 1);
    }
    void OnDestroy() { if (output != null) output.Dispose(); Application.logMessageReceived -= OnLog; }
    [Serializable] sealed class Identity { public int processId; public string company, product, persistentPath; public bool runInBackground; }
    [Serializable] sealed class Completion { public bool ok; public int errors; public string message; }
    [Serializable] sealed class Frame {
        public int frame; public long hwnd; public double elapsed, wallDelta, unityDelta, activeSeconds; public string phase;
        public bool nativeBefore, nativeAfter, bridgeBefore, bridgeAfter, focused, active, conflict;
    }
}
#endif
