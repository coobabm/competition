#if UNITY_EDITOR
using System;
using LingGuangV05.CaseMenu;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Candidate verification entry, not a record of execution. Run only after the Core cadence candidate is integrated.
public static class LifeHostSettingsChecks
{
    static int count;
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
    [MenuItem("LingGuang/Verification/Life host gates and settings")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use isolated EditMode verification");
        count = 0;
        var oldProbe = PlayerWindowState.EditorMinimizedProbe;
        var owner = new GameObject("Life host checks");
        var second = new GameObject("Life reader second owner");
        var sim = new XgSim();
        var clock = new XgLifeHostActivity.ActiveClock();
        try
        {
            PlayerWindowState.EditorMinimizedProbe = () => false;
            sim.WindowOpen = false;
            clock.Apply(sim, .016f);
            Check(sim.LifeEventActive, "Hidden internal exe must not pause active time");
            PlayerWindowState.EditorMinimizedProbe = () => true;
            clock.Apply(sim, .016f);
            Check(!sim.LifeEventActive, "Native minimized probe must pause active time");
            PlayerWindowState.EditorMinimizedProbe = () => false;
            clock.Apply(sim, .016f);
            Check(!sim.LifeEventActive, "Restore frame does not charge minimized time");
            clock.Apply(sim, .016f);
            Check(sim.LifeEventActive, "Unminimized steady frame resumes regardless of application focus");
            clock.Apply(sim, Time.maximumDeltaTime + 1);
            Check(sim.LifeEventActive, "Ordinary long frame is not guessed to be minimization or pause");
            clock.Apply(sim, .016f);
            Check(sim.LifeEventActive, "Normal frame after a long active frame remains active");
            clock.ObservePause(); clock.Apply(sim, .016f);
            Check(!sim.LifeEventActive, "Explicit stopped Controller frame gets one recovery frame");
            clock.Apply(sim, .016f);
            XgLifeHostActivity.SetGamePaused(owner, true);
            clock.Apply(sim, .016f);
            Check(!sim.LifeEventActive, "Explicit pause owner blocks event cadence");
            XgLifeHostActivity.SetGamePaused(owner, false);
            XgLifeHostActivity.SetReadingWindowVisible(owner, true);
            XgLifeHostActivity.SetReadingWindowVisible(second, true);
            XgLifeHostActivity.SetReadingWindowVisible(owner, false);
            clock.Apply(sim, .016f); // Pause recovery itself is not counted.
            clock.Apply(sim, .016f);
            Check(sim.LifeEventActive && sim.LifeEventConflict, "Second reader keeps conflict without pausing economy input");
            UnityEngine.Object.DestroyImmediate(second); second = null;
            Check(!XgLifeHostActivity.ReadingWindowVisible, "Destroyed reader cannot strand global conflict");

            int mode = 0, calls = 0;
            bool available = false;
            var hud = owner.AddComponent<CaseMenuHud>();
            hud.Build(TMP_Settings.defaultFontAsset, _ => { }, _ => { }, _ => { }, () => { }, () => { });
            hud.ConfigureLifeEvents(() => available ? (int?)mode : null, next => { mode = next; calls++; });
            var button = Array.Find(owner.GetComponentsInChildren<Button>(true), x => x.name == "LifeEvents");
            Check(button != null && !button.interactable, "Missing simulation disables event setting");
            button.onClick.Invoke();
            Check(calls == 0, "Unavailable event setting cannot mutate preference");
            available = true; hud.ShowSettings(.8f, false, false);
            Check(button.interactable, "Current simulation enables event setting");
            for (int expected = 1; expected <= 3; expected++)
            {
                button.onClick.Invoke();
                Check(mode == expected % 3 && calls == expected, "One callback cycles 8/15/off exactly once");
            }
            mode = 1; hud.HideSettings(); hud.ShowSettings(.8f, false, false);
            Check(calls == 3 && mode == 1, "Reopen restores actual mode without writing a default");
            sim.LifeEventIntervalMode = 1;
            var loaded = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
            Check(loaded.LifeEventIntervalMode == 1, "Core saved mode survives JSON roundtrip");
            Check(new XgSim().LifeEventIntervalMode == 0, "New state defaults to eight minutes");
            Debug.Log("LIFE_HOST_SETTINGS_PASS " + count + ". Injected native state and direct UI callbacks; not native Windows minimize, real save IO, or physical-input acceptance.");
        }
        finally
        {
            PlayerWindowState.EditorMinimizedProbe = oldProbe;
            XgLifeHostActivity.SetGamePaused(owner, false);
            XgLifeHostActivity.SetReadingWindowVisible(owner, false);
            if (second != null) { XgLifeHostActivity.SetReadingWindowVisible(second, false); UnityEngine.Object.DestroyImmediate(second); }
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }
}
#endif
