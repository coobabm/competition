using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;

public sealed class AeroContrastPlayQa : MonoBehaviour
{
    [Serializable] public class AppResult { public string app; public int texts; public int sharedThemeReferences; public List<string> lightTextCandidates = new List<string>(); }
    [Serializable] public class Result { public bool complete; public List<AppResult> apps = new List<AppResult>(); public List<string> errors = new List<string>(); }
    public Result report = new Result();
    const string Evidence = "/Users/kk/competition/Evidence/HongmengOS-Aero2010";
    bool running;
    void OnEnable() { Application.logMessageReceived += Capture; }
    void OnDisable() { Application.logMessageReceived -= Capture; }
    void Capture(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(message); }
    void Save() { File.WriteAllText(Path.Combine(Evidence, "aero-contrast-apps-qa.json"), JsonUtility.ToJson(report, true)); }
    public void Run() { if (running || report.complete) return; running = true; StartCoroutine(Inspect()); }
    IEnumerator Inspect()
    {
        var root = GameObject.Find("hongmengos 2010 Desktop Source").transform.Find("Desktop/Apps & Windows");
        // Do not invoke app factories while the simulated OS is still booting.
        yield return new WaitUntil(() => root.gameObject.activeInHierarchy && root.parent.Find("Taskbar").gameObject.activeInHierarchy);
        yield return new WaitForSecondsRealtime(.5f);
        var windows = root.GetComponentsInChildren<WindowManager>(true);
        foreach (var window in windows) if (window.isOn && window.gameObject.activeInHierarchy) window.CloseWindow();
        yield return new WaitForSecondsRealtime(1f);
        foreach (var window in windows)
        {
            window.OpenWindow();
            yield return new WaitForSecondsRealtime(1.15f);
            var app = new AppResult { app = window.name };
            foreach (var element in window.GetComponentsInChildren<UIManagerElement>(true))
                if (element.themeManagerAsset != null && element.themeManagerAsset.name != "Aero 2010 UI") app.sharedThemeReferences++;
            foreach (var text in window.GetComponentsInChildren<TMP_Text>(true))
            {
                app.texts++;
                if (!text.gameObject.activeInHierarchy || text.color.a < .1f || text.color.r < .7f || text.color.g < .7f || text.color.b < .7f || string.IsNullOrWhiteSpace(text.text)) continue;
                bool visible = true;
                foreach (var group in text.GetComponentsInParent<CanvasGroup>()) if (group.alpha < .01f) visible = false;
                if (!visible) continue;
                string path = text.name;
                for (var p = text.transform.parent; p != null && p != window.transform; p = p.parent) path = p.name + "/" + path;
                app.lightTextCandidates.Add(path + " : " + text.text);
            }
            report.apps.Add(app);
            Save();
            ScreenCapture.CaptureScreenshot(Path.Combine(Evidence, "contrast-" + window.name.Replace(" ", "-").ToLowerInvariant() + ".png"));
            yield return new WaitForEndOfFrame();
            window.CloseWindow();
            yield return new WaitForSecondsRealtime(.75f);
        }
        report.complete = true;
        running = false;
        Save();
    }
}
