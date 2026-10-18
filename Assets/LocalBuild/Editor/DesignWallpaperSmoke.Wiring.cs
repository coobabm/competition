using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;

public static partial class DesignWallpaperSmoke
{
    public static void RunWiringMotion() { Start("wiring-motion"); }
    static void StartWiringMotionChecks()
    {
        due = double.PositiveInfinity;
        var runner = new GameObject("WiringNaturalFrames").AddComponent<XgJuice>();
        runner.StartCoroutine(WiringMotionFrames());
    }
    static IEnumerator WiringMotionFrames()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(WiringMotionChecks.Frames(PrologueDesk.CjkFont(), name =>
        {
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG()); }
            finally { UnityEngine.Object.Destroy(image); }
        }));
        stack.Push(UiV4PressChecks.Frames());
        Exception failure = null;
        while (stack.Count > 0)
        {
            object current = null; bool more = false;
            try { more = stack.Peek().MoveNext(); if (more) current = stack.Peek().Current; }
            catch (Exception error) { failure = error; }
            if (failure != null) break;
            if (!more) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
            if (current is IEnumerator nested) { stack.Push(nested); continue; }
            yield return current;
        }
        while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
        File.WriteAllText(Path.Combine(Output, "result.txt"), failure == null ? "PASS" : "FAIL: " + failure);
        Finish(failure == null ? 0 : 1);
    }
}

