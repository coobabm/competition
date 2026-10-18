#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;

// Run inside the existing isolated Main fixture after the real application is open.
// Caller owns save/profile isolation, runtime error capture and coroutine timeout.
public static class CrawlerVisibleMeshChecks
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static object Field(object obj, string name) => obj.GetType().GetField(name, Flags).GetValue(obj);
    static void Call(object obj, string name) => obj.GetType().GetMethod(name, Flags).Invoke(obj, null);
    static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException("Crawler lifecycle: " + reason); }
    static int Count(object obj, string name) => ((ICollection)Field(obj, name)).Count;

    public static IEnumerator Frames(XingGuangController controller)
    {
        var view = controller.View;
        var crawler = view.Data.Crawler;
        var trainRoot = view.Train.root;
        bool wasActive = trainRoot.gameObject.activeSelf;
        Call(crawler, "Close");
        try
        {
            trainRoot.gameObject.SetActive(false);
            view.Data.OpenCrawlerIntro();
            float time = (float)Field(crawler, "timeLeft");
            int grabbed = (int)Field(crawler, "grabbed");
            for (int i = 0; i < 3; i++) { crawler.Tick(.05f); yield return null; }
            Require((bool)Field(crawler, "pendingPage"), "inactive parent must retain pending page");
            Require(Count(crawler, "cached") == 0 && Count(crawler, "words") == 0, "hidden page must not snapshot or collect words");
            Require((float)Field(crawler, "timeLeft") == time && (int)Field(crawler, "grabbed") == grabbed, "hidden page must not advance or settle");
            // The normal View.Update route must tick a crawler hosted on Train while Data is hidden.
            trainRoot.gameObject.SetActive(true);
            for (int i = 0; i < 90 && (Count(crawler, "cached") == 0 || (bool)Field(crawler, "pendingGeometry")); i++) yield return null;
            Require(Count(crawler, "cached") > 0 && Count(crawler, "words") > 0, "visible page must produce actual glyph snapshots and word rectangles");
            Require(!(bool)Field(crawler, "pendingGeometry") && Field(crawler, "spider") != null, "spider must start after geometry is ready");
            foreach (var entry in (IEnumerable)Field(crawler, "cached"))
            {
                var meshes = (TMPro.TMP_MeshInfo[])entry;
                bool vertices = false;
                foreach (var mesh in meshes) if (mesh.vertices != null && mesh.vertices.Length > 0) vertices = true;
                Require(vertices, "snapshot must contain real vertex buffers");
            }
            Call(crawler, "LoadPage");
            Require(Count(crawler, "cached") == 0, "next-page request discards stale snapshots");
            Call(crawler, "Close");
            Require(!(bool)Field(crawler, "pendingPage") && !(bool)Field(crawler, "pendingGeometry"), "close clears deferred work");
            view.Data.OpenCrawlerIntro();
            for (int i = 0; i < 90 && (Count(crawler, "cached") == 0 || (bool)Field(crawler, "pendingGeometry")); i++) yield return null;
            Require(Count(crawler, "words") > 0 && Field(crawler, "spider") != null, "reopen must create a fresh working page");
            Debug.Log("CrawlerVisibleMeshChecks PASS: inactive wait, real visible mesh, hidden Data owner tick, page cancellation, reopen.");
        }
        finally { Call(crawler, "Close"); trainRoot.gameObject.SetActive(wasActive); }
    }
}
#endif
