using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using LingGuangV05.Desktop.XingGuang;
using Michsky.DreamOS;
using UnityEngine;

public static partial class DesignWallpaperSmoke
{
    // Optional standalone hook: call once from isolated case 0 for mode "desktop-fade".
    public static void RunDesktopFade() { Start("desktop-fade"); }

    static void StartDesktopFadeFrameChecks()
    {
        due = double.PositiveInfinity;
        var runner = new GameObject("DesktopFadeVerificationRunner").AddComponent<XgJuice>();
        runner.StartCoroutine(CheckDesktopFadeFrames(runner));
    }

    static IEnumerator CheckDesktopFadeFrames(XgJuice runner)
    {
        var checks = DesktopFadeFrameSteps();
        Exception failure = null;
        while (true)
        {
            object current = null;
            bool more = false;
            try { more = checks.MoveNext(); if (more) current = checks.Current; }
            catch (Exception error) { failure = error; }
            if (failure != null || !more) break;
            yield return current;
        }
        (checks as IDisposable)?.Dispose();
        UnityEngine.Object.Destroy(runner.gameObject);
        File.WriteAllText(Path.Combine(Output, "result.txt"), failure == null ? "PASS" : "FAIL: " + failure);
        Finish(failure == null ? 0 : 1);
    }

    // May also be manually enumerated after the completed English evidence is on disk.
    // It never rewrites alpha, evaluates Animator.Update, changes lesson state, or finishes the editor.
    static IEnumerator DesktopFadeFrameSteps()
    {
        var user = UnityEngine.Object.FindAnyObjectByType<UserManager>();
        Check(Application.isPlaying && user != null && user.desktopScreen != null,
            "Desktop fade uses the existing isolated Main UserManager");
        var animator = user.desktopScreen;
        var group = animator.GetComponent<CanvasGroup>();
        Check(group != null && animator.gameObject.activeInHierarchy && !user.transform.IsChildOf(animator.transform),
            "Rendered desktop has its real CanvasGroup and an independent UserManager");
        Check(animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash("Base Layer.In"))
            && animator.HasState(0, Animator.StringToHash("Base Layer.Out")), "Actual desktop controller contains In and Out");
        Check(animator.updateMode == AnimatorUpdateMode.Normal && animator.cullingMode == AnimatorCullingMode.AlwaysAnimate
            && Mathf.Approximately(animator.speed, 1) && Time.timeScale > 0,
            "Fade fixture retains the scene's normal scaled, uncullable animation contract");

        float originalScale = Time.timeScale, originalSpeed = animator.speed;
        var samples = new StringBuilder("phase,frame,realtime,alpha,interactable,blocksRaycasts,enabled,normalizedTime,stateHash,isIn,isOut,clip,active,timeScale,speed\n");
        var endFrame = new WaitForEndOfFrame();
        string[] names = { "show", "hide", "show-again", "repeat-show", "repeat-hide",
            "cross-frame-last-show", "same-frame-last-hide", "inactive-show", "paused-hide", "slow-show" };
        bool passed = false;
        try
        {
            Time.timeScale = 1;
            for (int phase = 0; phase < names.Length; phase++)
            {
                string name = names[phase];
                bool visible = phase == 0 || phase == 2 || phase == 3 || phase == 5 || phase == 7 || phase == 9;
                DesktopFadeSample(samples, name + "-before-request", animator, group);
                switch (phase)
                {
                    case 0: user.ShowDesktop(); break;
                    case 1: user.HideDesktop(); break;
                    case 2: user.ShowDesktop(); break;
                    case 3: user.ShowDesktop(); user.ShowDesktop(); break;
                    case 4: user.HideDesktop(); user.HideDesktop(); break;
                    case 5:
                        user.ShowDesktop(); yield return null;
                        user.HideDesktop(); yield return null;
                        user.ShowDesktop(); break;
                    case 6: user.HideDesktop(); user.ShowDesktop(); user.HideDesktop(); break;
                    case 7:
                        animator.gameObject.SetActive(false);
                        yield return null;
                        user.ShowDesktop();
                        Check(animator.gameObject.activeInHierarchy, "ShowDesktop reactivates the real desktop");
                        break;
                    case 8:
                        Time.timeScale = 0;
                        user.HideDesktop();
                        yield return new WaitForSecondsRealtime(.1f);
                        yield return endFrame;
                        DesktopFadeSample(samples, name + "-paused", animator, group);
                        Check(animator.enabled && animator.GetCurrentAnimatorStateInfo(0).IsName("Out")
                            && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1,
                            "Normal desktop animation remains pending while scaled time is paused");
                        Time.timeScale = 1;
                        break;
                    case 9:
                        // Controlled animation speed makes the old fixed .267-second stop fail reliably.
                        // This is an actual-frame timing regression, not a claim to simulate slow startup.
                        animator.speed = .1f;
                        user.ShowDesktop();
                        yield return new WaitForSecondsRealtime(.35f);
                        yield return endFrame;
                        DesktopFadeSample(samples, name + "-past-old-delay", animator, group);
                        Check(animator.enabled && animator.GetCurrentAnimatorStateInfo(0).IsName("In")
                            && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1,
                            "Slow actual animation remains enabled beyond the obsolete fixed delay");
                        break;
                }
                DesktopFadeSample(samples, name + "-after-request", animator, group);

                // Bound by real time, not frame count; never force the requested state's final sample.
                float deadline = Time.realtimeSinceStartup + 8;
                do
                {
                    yield return endFrame;
                    DesktopFadeSample(samples, name, animator, group);
                }
                while (animator.enabled && Time.realtimeSinceStartup < deadline);
                DesktopFadeTerminal(name, visible, animator, group);
                // Verify that disabling the Animator preserves the sampled terminal properties.
                yield return null;
                yield return endFrame;
                DesktopFadeSample(samples, name + "-held", animator, group);
                DesktopFadeTerminal(name + " held", visible, animator, group);

                if (phase == 1 || phase == 2 || phase == 9)
                {
                    Capture("desktop-fade-" + name);
                    CaptureVisualState("desktop-fade-" + name);
                    DesktopFadeFullScreenshot("desktop-fade-" + name);
                }
                animator.speed = originalSpeed;
            }
            Check(true, "DESKTOP_FADE_REAL_FRAMES_PASS (terminal alpha, input flags, repeated calls, reversal, reactivation, pause/resume and slow animation; screenshots still need visual review)");
            passed = true;
        }
        finally
        {
            Time.timeScale = originalScale;
            if (animator != null) animator.speed = originalSpeed;
            File.WriteAllText(Path.Combine(Output, "desktop-fade-frame-samples.csv"), samples.ToString());
            File.WriteAllText(Path.Combine(Output, "desktop-fade-result.txt"), passed ? "PASS" : "FAIL");
        }
    }

    static void DesktopFadeTerminal(string phase, bool visible, Animator animator, CanvasGroup group)
    {
        bool terminal = Mathf.Abs(group.alpha - (visible ? 1 : 0)) < .001f
            && group.interactable == visible && group.blocksRaycasts == visible;
        if (!terminal)
        {
            string name = "desktop-fade-" + phase.Replace(' ', '-') + "-failure";
            try { Capture(name); CaptureVisualState(name); DesktopFadeFullScreenshot(name); }
            catch (Exception captureError)
            { File.AppendAllText(Path.Combine(Output, "desktop-fade-capture-errors.txt"), captureError + "\n"); }
        }
        Check(animator.gameObject.activeInHierarchy && !animator.enabled,
            phase + ": real requested animation completed and was disabled");
        Check(terminal, phase + ": final opacity and input flags match the final Show/Hide request"
            + " expectedVisible=" + visible + " alpha=" + group.alpha + " interactable=" + group.interactable
            + " blocksRaycasts=" + group.blocksRaycasts + " stateIn=" + animator.GetCurrentAnimatorStateInfo(0).IsName("In")
            + " stateOut=" + animator.GetCurrentAnimatorStateInfo(0).IsName("Out"));
    }

    static void DesktopFadeSample(StringBuilder samples, string phase, Animator animator, CanvasGroup group)
    {
        var state = animator.GetCurrentAnimatorStateInfo(0);
        string clips = "";
        foreach (var clip in animator.GetCurrentAnimatorClipInfo(0))
            clips += (clips.Length == 0 ? "" : "|") + clip.clip.name;
        samples.AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2:F6},{3:F6},{4},{5},{6},{7:F6},{8},{9},{10},{11},{12},{13:F3},{14:F3}\n",
            phase, Time.frameCount, Time.realtimeSinceStartup, group.alpha, group.interactable,
            group.blocksRaycasts, animator.enabled, state.normalizedTime, state.shortNameHash, state.IsName("In"), state.IsName("Out"),
            clips, animator.gameObject.activeInHierarchy, Time.timeScale, animator.speed);
    }

    static void DesktopFadeFullScreenshot(string name)
    {
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        try
        {
            Check(texture != null && texture.width > 0 && texture.height > 0,
                name + ": completed full-viewport capture exists");
            File.WriteAllBytes(Path.Combine(Output, name + "-full.png"), texture.EncodeToPNG());
        }
        finally { if (texture != null) UnityEngine.Object.Destroy(texture); }
    }
}
