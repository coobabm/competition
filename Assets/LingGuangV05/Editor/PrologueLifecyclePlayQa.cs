#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LingGuangV05.Editor
{
    /// <summary>Explicit memory-only Main QA. Uses real LCD pointer routing; never runs on player startup.</summary>
    public static class PrologueLifecyclePlayQa
    {
        [Serializable] public class Check { public string name; public bool passed; public string detail; }
        [Serializable] public class Report { public bool complete; public string branch; public List<Check> checks = new List<Check>(); }
        public static Report Result;
        public static bool Running;
        static PrologueDirector director;
        static ChapterOneRuntime runtime;
        static AeroLcdInputSurface surface;
        static string Dir => Path.Combine(Application.dataPath, "../Evidence/PrologueLifecycle");
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static CanvasGroup Thought => (CanvasGroup)typeof(PrologueDirector).GetField("thoughtGroup", Private).GetValue(director);

        public static void Begin(bool shutdownYes)
        {
            runtime = Object.FindAnyObjectByType<ChapterOneRuntime>();
            director = Object.FindAnyObjectByType<PrologueDirector>();
            surface = Object.FindAnyObjectByType<AeroLcdInputSurface>();
            if (!Application.isPlaying || runtime == null || runtime.useDiskSave || runtime.TestMode || director == null || Running)
                throw new InvalidOperationException("Needs fresh memory-only Main, story enabled, no other QA running.");
            Directory.CreateDirectory(Dir);
            Result = new Report { branch = shutdownYes ? "yes" : "no" };
            Running = true;
            ChapterOneQaReloadGuard.LockForPlayQa(240);
            runtime.StartCoroutine(Guard(Run(shutdownYes)));
        }

        static IEnumerator Guard(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);
            while (stack.Count > 0)
            {
                object current = null;
                bool moved = false;
                Exception failure = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception e) { failure = e; }
                if (failure != null) { Record("QA exception", false, failure.ToString()); break; }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) stack.Push(nested);
                else yield return current;
            }
            Result.complete = !Result.checks.Any(c => !c.passed);
            Running = false;
            Save();
            ChapterOneQaReloadGuard.Release();
        }

        static void Save() => File.WriteAllText(Path.Combine(Dir, "play-" + Result.branch + ".json"), JsonUtility.ToJson(Result, true));
        static void Record(string name, bool ok, string detail = "") { Result.checks.Add(new Check { name = name, passed = ok, detail = detail }); Save(); }
        static void Require(string name, bool ok, string detail = "") { Record(name, ok, detail); if (!ok) throw new InvalidOperationException(name + " " + detail); }
        static IEnumerator Until(Func<bool> ready, float seconds, string name)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (!ready() && Time.realtimeSinceStartup < until) yield return null;
            Require(name, ready(), "step=" + director.Step);
        }
        static void Capture(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(Dir, Result.branch + "-" + name + ".png"));
        static void Click(RectTransform rt, bool twice = false)
        {
            ChapterOnePlayQa.Click(rt);
            if (twice) ChapterOnePlayQa.Click(rt);
        }
        static RectTransform DialogButton(string window, string label)
        {
            var root = PrologueDirector.Desk.windows.Find(window);
            return (RectTransform)root.GetComponentsInChildren<Button>().First(b => b.GetComponentInChildren<TMP_Text>()?.text == label).transform;
        }
        static void SingleLayers()
        {
            Require("Exactly one subtitle canvas", Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).Count(c => c.name == "LingGuang Prologue Screen") == 1);
            Require("Exactly one shared desktop layer", Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include).Count(c => c.name == "Prologue Layer") == 1);
        }
        static IEnumerator OpenOrdinaryApp(string name)
        {
            var icon = (RectTransform)PrologueDirector.Desk.icons.Find(name);
            Click(icon, true);
            yield return new WaitForSecondsRealtime(1);
            var w = Object.FindObjectsByType<WindowManager>(FindObjectsInactive.Include).First(x => x.name == name);
            Require(name + " opens through LCD", w.isOn && w.gameObject.activeInHierarchy && w.GetComponent<CanvasGroup>().alpha > .9f);
            Capture(name.Replace(" ", "-"));
            w.CloseWindow();
            yield return new WaitForSecondsRealtime(.8f);
        }

        static IEnumerator Run(bool yes)
        {
            yield return Until(() => director.Step == "virus", 35, "Boot reaches virus choice");
            Require("Boot returns input", surface.inputEnabled);
            SingleLayers();
            yield return new WaitForSecondsRealtime(.5f); // let the newly built dialog render before pointer input
            Click(DialogButton("Prologue Virus Alert", PrologueContent.L("no")));
            yield return Until(() => director.Step == "txt", 40, "Virus choice reaches 0.txt");
            yield return new WaitForSecondsRealtime(1);
            Require("Opening subtitle is displayed", Thought.alpha > .9f);
            Capture("subtitle-visible");
            yield return Until(() => Thought.alpha == 0, 40, "Opening subtitle expires naturally");
            Require("Waiting for 0.txt leaves desktop controllable", surface.inputEnabled && director.Step == "txt");
            Capture("subtitle-gone");
            yield return OpenOrdinaryApp("Calculator");
            yield return OpenOrdinaryApp("Web Browser");
            yield return OpenOrdinaryApp("Notepad");
            Click((RectTransform)PrologueDirector.Desk.icons.Find("Prologue 0.txt"), true);
            yield return new WaitForSecondsRealtime(.2f);
            Require("Only scripted mouse sequence owns input", director.Step == "hijack" && !surface.inputEnabled);
            yield return Until(() => director.Step == "shutdown", 90, "Hijack reaches shutdown choice");
            Require("Shutdown choice returns input", surface.inputEnabled);
            Capture("shutdown-choice");
            yield return new WaitForSecondsRealtime(.5f);
            Click(DialogButton("Prologue Shutdown", PrologueContent.L(yes ? "yes" : "no")));
            // 老周's message no longer waits inside the prologue: it comes after the setup, in its opening turn.
            yield return Until(() => director.Step == "setup", yes ? 45 : 135, "Branch reaches setup without softlock");
            if (!yes) Require("Extraction returns input", surface.inputEnabled);
            Require("Setup leaves desktop interactive", surface.inputEnabled && runtime.Sim.AppInstalled);
            yield return new WaitForSecondsRealtime(10);
            Require("No subtitle remains at setup", Thought.alpha == 0);
            SingleLayers();
            Capture("setup");
            // Stop during a fresh boot, then re-enable and stop again: no orphan coroutines may re-hijack input.
            director.enabled = false;
            typeof(ChapterOneRuntime).GetMethod("Attach", Private).Invoke(runtime, new object[] { new ChapterOneSim() });
            director.enabled = true;
            yield return Until(() => director.Step == "title", 5, "Re-enable starts a fresh sequence");
            Require("Boot owns input before interruption", !surface.inputEnabled);
            director.enabled = false;
            Require("Disable immediately gives input back", surface.inputEnabled && !director.Running && Thought.alpha == 0);
            yield return new WaitForSecondsRealtime(12);
            Require("Interrupted coroutine cannot resume or steal input", surface.inputEnabled && director.Step == "" && !director.Running);
            Require("Interrupted boot leaves no overlay", PrologueDirector.Desk.layer.Find("Boot") == null && PrologueDirector.Desk.layer.Find("Shutdown") == null);
            director.enabled = true;
            yield return Until(() => director.Step == "virus", 30, "Re-enabled prologue remains playable");
            Require("Restart again restores input", surface.inputEnabled);
            SingleLayers();
            Require("QA never writes a player save", !runtime.useDiskSave);
        }
    }
}
#endif
