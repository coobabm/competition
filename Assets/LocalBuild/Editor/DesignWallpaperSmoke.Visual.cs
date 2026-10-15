using System.IO;
using System.Text;
using HongmengOS.Aero2010;
using LingGuangV05.Desktop.XingGuang;
using Michsky.DreamOS;
using UnityEngine;
using UnityEngine.UI;

public static partial class DesignWallpaperSmoke
{
    static void CaptureVisualState(string prefix)
    {
        var bridge = Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        var data = new StringBuilder();
        foreach (var window in Object.FindObjectsByType<WindowManager>(FindObjectsSortMode.None))
            data.AppendLine("WINDOW " + VisualPath(window.transform) + " on=" + window.isOn + " frame=" + VisualPath(window.windowContainer));
        foreach (var canvas in bridge.sourceCanvas.GetComponentsInChildren<Canvas>(true))
            data.AppendLine("CANVAS " + VisualPath(canvas.transform) + " active=" + canvas.isActiveAndEnabled
                + " override=" + canvas.overrideSorting + " order=" + canvas.sortingOrder);
        foreach (var group in bridge.sourceCanvas.GetComponentsInChildren<CanvasGroup>(false))
            data.AppendLine("GROUP " + VisualPath(group.transform) + " alpha=" + group.alpha + " ignoreParent=" + group.ignoreParentGroups);
        foreach (var animator in bridge.sourceCanvas.GetComponentsInChildren<Animator>(false))
            data.AppendLine("ANIMATOR " + VisualPath(animator.transform) + " enabled=" + animator.enabled
                + " speed=" + animator.speed + " mode=" + animator.updateMode
                + " normalized=" + (animator.runtimeAnimatorController != null ? animator.GetCurrentAnimatorStateInfo(0).normalizedTime : -1));
        var brain = Object.FindAnyObjectByType<XgBrainWallpaper>();
        data.AppendLine("BRAIN visible=" + brain.Reveal.Visible + " age=" + brain.Reveal.Age + " opacity=" + brain.Reveal.Opacity);
        foreach (var graphic in bridge.sourceCanvas.GetComponentsInChildren<Graphic>(false))
        {
            data.AppendLine("GRAPHIC " + VisualPath(graphic.transform) + " type=" + graphic.GetType().Name
                + " color=" + graphic.color + " rendererColor=" + graphic.canvasRenderer.GetColor()
                + " inherited=" + graphic.canvasRenderer.GetInheritedAlpha() + " depth=" + graphic.depth
                + " culled=" + graphic.canvasRenderer.cull + " size=" + graphic.rectTransform.rect.size
                + " material=" + graphic.materialForRendering?.name);
        }
        File.WriteAllText(Path.Combine(Output, prefix + "-visual-state.txt"), data.ToString());
    }

    static string VisualPath(Transform node)
    {
        if (node == null) return "<null>";
        string path = node.name + "[" + node.GetSiblingIndex() + "]";
        while (node.parent != null) { node = node.parent; path = node.name + "[" + node.GetSiblingIndex() + "]/" + path; }
        return path;
    }
}
