using HongmengOS.Aero2010;
using LingGuangV05.CaseMenu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LingGuangV05.Desktop
{
    /// <summary>
    /// Direct Main entry (including editor play) must present the same borderless
    /// desktop as the case menu. The menu owns its own animated handoff.
    /// </summary>
    public static class DirectDesktopFullscreen
    {
        public static void Install(Scene scene)
        {
            if (!scene.IsValid() || scene.path != MainSceneEntry.ScenePath
                || Object.FindAnyObjectByType<CaseMenuMainDisplay>(FindObjectsInactive.Include) != null) return;
            AeroLcdController lcd = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Fullscreen Desktop") return;
                if (lcd == null) lcd = root.GetComponentInChildren<AeroLcdController>(true);
            }
            if (lcd == null || lcd.display == null || lcd.inputSurface == null || lcd.sourceCamera == null) return;
            var screen = lcd.display.rectTransform;
            var physicalCanvas = screen.GetComponentInParent<Canvas>();
            var overlay = new GameObject("Fullscreen Desktop", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(overlay, scene);
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            screen.SetParent(overlay.transform, false);
            screen.localRotation = Quaternion.identity; screen.localScale = Vector3.one;
            screen.anchorMin = Vector2.zero; screen.anchorMax = Vector2.one;
            screen.offsetMin = screen.offsetMax = Vector2.zero;
            lcd.display.color = Color.white; lcd.display.raycastTarget = true;
            if (physicalCanvas != null) physicalCanvas.enabled = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var framing in root.GetComponentsInChildren<AeroMonitorFraming>(true)) framing.enabled = false;
                if (root.name == "HongmengOS 2010 LCD Model") root.SetActive(false);
            }
            lcd.GetComponent<AeroLcdResolution>()?.PrepareViewportResolution();
            lcd.inputSurface.SetInputEnabled(lcd.IsPowered);
        }
    }
}
