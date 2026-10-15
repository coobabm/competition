using System.Collections;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using UnityEngine;

public static partial class DesignWallpaperSmoke
{
    static IEnumerator RefreshRateScene(XingGuangController controller, ChapterOneRuntime runtime,
        XgBrainWallpaper brain, XgWallpaperTraining training, XgWallpaperConcepts concepts, XgBrainWallpaperPresentation presentation)
    {
        TrainingSceneCase("Wallpaper refresh: actual normal/fullscreen/minimized AI window, count real mesh rebuilds over natural time");
        var halo = brain.GetComponentInChildren<XgWallpaperPersona>(); float scale = Time.timeScale;
        try
        {
            Time.timeScale = 1; controller.Window.OpenWindow();
            yield return new WaitForSecondsRealtime(1);
            Check(!presentation.FullyCovered && presentation.BackgroundFrameRate == 30,
                "A normal application still exposes the desktop and retains 30 fps target");
            controller.Window.FullscreenWindow(); yield return new WaitForSecondsRealtime(1);
            Check(presentation.FullyCovered && presentation.BackgroundFrameRate == 10,
                "Actual fullscreen application lowers wallpaper target to 10 fps");
            Time.timeScale = 0; int cellsBefore = concepts.MeshBuilds, haloBefore = halo.MeshBuilds;
            float start = Time.unscaledTime; yield return new WaitForSecondsRealtime(1.25f);
            float elapsed = Time.unscaledTime - start;
            Check(concepts.MeshBuilds - cellsBefore <= Mathf.CeilToInt(elapsed * 10) + 2
                && halo.MeshBuilds - haloBefore <= Mathf.CeilToInt(elapsed * 10) + 2
                && concepts.MeshBuilds > cellsBefore && halo.MeshBuilds > haloBefore,
                "Actual covered concept and halo meshes rebuild at bounded 10 fps on unscaled time");
            yield return new WaitForEndOfFrame(); TrainingSceneCapture(training, "refresh-fullscreen-10fps");
            Time.timeScale = 1; controller.Window.FullscreenWindow(); yield return new WaitForSecondsRealtime(1);
            Check(!presentation.FullyCovered && presentation.BackgroundFrameRate == 30,
                "Restoring a normal window immediately restores 30 fps target");
            controller.Window.MinimizeWindow(); yield return new WaitForSecondsRealtime(1);
            Check(!presentation.FullyCovered && presentation.BackgroundFrameRate == 30,
                "A minimized window cannot falsely occlude the desktop");
            cellsBefore = concepts.MeshBuilds; haloBefore = halo.MeshBuilds; start = Time.unscaledTime;
            yield return new WaitForSecondsRealtime(1.25f); elapsed = Time.unscaledTime - start;
            Check(concepts.MeshBuilds - cellsBefore <= Mathf.CeilToInt(elapsed * 30) + 2
                && halo.MeshBuilds - haloBefore <= Mathf.CeilToInt(elapsed * 30) + 2
                && concepts.MeshBuilds - cellsBefore > elapsed * 10 && halo.MeshBuilds - haloBefore > elapsed * 10,
                "Visible desktop meshes refresh faster than 10 fps while respecting 30 fps cap");
            yield return new WaitForEndOfFrame(); TrainingSceneCapture(training, "refresh-desktop-30fps");
        }
        finally { Time.timeScale = scale; }
    }
}
