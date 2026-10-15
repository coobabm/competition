using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;

public static partial class DesignWallpaperSmoke
{
    static IEnumerator GrowthGuideSteps(XingGuangController controller, ChapterOneRuntime runtime)
    {
        Check(runtime.TestMode && !runtime.useDiskSave && !runtime.tickEnabled && !controller.enabled,
            "Growth guide uses the isolated scene with both simulation drivers stopped");
        var view = controller.View;
        var page = view.Tree;
        var navigation = view.NavButton("tree");
        bool savedEnabled = view.enabled, savedShelf = page.GrowthShelfVisible;
        string savedTab = view.Tab, savedState = JsonUtility.ToJson(controller.Sim.S);
        var fixture = new GameObject("GrowthGuideFixture");
        fixture.SetActive(false);
        var guide = fixture.AddComponent<GuideNote>();
        var rings = new List<GameObject>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var router = runtime.GetComponent<ChapterOneDesktopRouter>()
            ?? UnityEngine.Object.FindAnyObjectByType<ChapterOneDesktopRouter>();
        Check(router != null && router.Get(XingGuangController.AppId) != null,
            "Growth guide uses the registered production application route");
        typeof(GuideNote).GetField("lab", flags).SetValue(guide, controller);
        typeof(GuideNote).GetField("router", flags).SetValue(guide, router);
        var goLab = typeof(GuideNote).GetMethod("GoLab", flags);
        try
        {
            // Visible includes this component's enabled state. Only the view is enabled;
            // the inactive fixture never starts GuideNote's polling or persistent seen markers.
            view.enabled = true;
            foreach (string route in new[] { "tree", "items", "abilities", "growth" })
            {
                view.Open(route);
                Check(XgGuideHighlight.TabButton(view, route) == navigation
                    && XgGuideHighlight.Page(view, route) == page.root
                    && XgGuideHighlight.Find(view, route, "") == navigation,
                    "Production guide helpers resolve the visible growth page and navigation: " + route);
                Check(XgGuideHighlight.Find(view, route, "name:GrowthShelfToggle") == page.root.Find("GrowthShelfToggle"),
                    "Production guide control lookup reaches the integrated growth page: " + route);
                Check(navigation.Find("Guide Ring") == null, "Growth guide starts with no existing navigation ring: " + route);
                view.Open("home");
                // This is the actual guide iterator, including router.Open, its readiness wait,
                // two post-open frames and the real Show/Pulse call. Empty targets match plateau hints.
                var step = new XgGuideStep { app = XgGuide.Lab, tab = route, target = "" };
                var opening = (IEnumerator)goLab.Invoke(guide, new object[] { step });
                int yielded = 0;
                try
                {
                    while (opening.MoveNext())
                    {
                        yielded++;
                        Check(view.Visible && view.Tab == "tree" && page.root.gameObject.activeInHierarchy,
                            "Actual guide route has already opened growth: " + route);
                        Check(yielded <= 2, "Guide readiness does not wait for a removed tab id: " + route);
                        yield return .05f;
                    }
                }
                finally { (opening as IDisposable)?.Dispose(); }
                Check(yielded == 2, "Actual guide keeps only its two post-open frame waits: " + route);
                var ring = navigation.Find("Guide Ring");
                if (ring != null) rings.Add(ring.gameObject);
                Check(ring != null && ring.gameObject.activeInHierarchy
                    && ring.GetComponent<XgGuideHighlight>() != null
                    && !ring.GetComponent<XgGuideRingGraphic>().raycastTarget,
                    "Actual guide highlights the visible growth navigation without intercepting input: " + route);
                UnityEngine.Object.DestroyImmediate(ring.gameObject);
            }
            Check(JsonUtility.ToJson(controller.Sim.S) == savedState,
                "Opening and highlighting legacy growth guide routes changes no saved simulation fields");
        }
        finally
        {
            foreach (var ring in rings) if (ring != null) UnityEngine.Object.DestroyImmediate(ring);
            UnityEngine.Object.DestroyImmediate(fixture);
            view.Open(savedTab); page.SetGrowthShelf(savedShelf); view.enabled = savedEnabled;
        }
    }
}
