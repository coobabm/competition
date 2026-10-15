using System.Collections.Generic;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgWallpaperFamilyStar : MaskableGraphic
    {
        XingGuangController controller;
        XgBrainWallpaper wallpaper;
        XgBrainWallpaperPresentation presentation;
        XgState observed;
        TMP_Text label;
        float opacity, nextRead;
        public bool Strong { get; private set; }

        // A single choice is not yet a strong belief. Three positive choices and
        // at least a two-to-one preference reveal the star; mixed choices can dim it.
        public static bool IsStrong(IEnumerable<XgFamilyAttitude> attitudes)
        {
            long important = 0, junk = 0;
            if (attitudes != null) foreach (var a in attitudes)
            {
                if (a == null) continue;
                important += System.Math.Max(0, a.important); junk += System.Math.Max(0, a.junk);
            }
            return important >= 3 && important >= junk * 2;
        }

        public static void Install(XingGuangController controller, XgBrainWallpaper wallpaper)
        {
            var rect = PrologueDesk.Rect("Family Star", wallpaper.transform, new Vector2(.86f, .79f), new Vector2(.86f, .79f), -Vector2.one * 32, Vector2.one * 32);
            var view = rect.gameObject.AddComponent<XgWallpaperFamilyStar>();
            view.controller = controller; view.wallpaper = wallpaper; view.raycastTarget = false;
            view.presentation = wallpaper.GetComponent<XgBrainWallpaperPresentation>();
            var label = PrologueDesk.Rect("Label", rect, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-55, -16), new Vector2(55, 6)).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = PrologueDesk.CjkFont(); label.fontSize = 12; label.alignment = TextAlignmentOptions.Center;
            label.color = new Color32(214, 171, 108, 255); label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap; view.label = label;
        }

        void Update()
        {
            if (wallpaper == null || controller?.Sim == null) return;
            if (Time.unscaledTime >= nextRead || observed != controller.Sim.S)
            {
                nextRead = Time.unscaledTime + .1f;
                Strong = IsStrong(controller.Sim.S.familyAttitudes);
                if (observed != controller.Sim.S) { observed = controller.Sim.S; opacity = Strong ? 1 : 0; }
                label.text = GameText.T("家人之星", "Family star");
            }
            opacity = Mathf.MoveTowards(opacity, Strong ? 1 : 0, Time.unscaledDeltaTime * 1.5f);
            float alpha = opacity * (wallpaper.Reveal.Visible ? Mathf.SmoothStep(0, 1, (wallpaper.Reveal.Age - XgWallpaperReveal.CompleteSeconds) / .8f) : 0);
            alpha *= presentation != null ? presentation.LightLevel : 1;
            canvasRenderer.SetAlpha(alpha); label.canvasRenderer.SetAlpha(alpha);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var center = rectTransform.rect.center;
            var warm = new Color32(255, 194, 107, 255);
            XgDraw.Disc(vh, center, 29, new Color(1, .55f, .15f, .035f), 32);
            XgDraw.Disc(vh, center, 21, new Color(1, .65f, .25f, .09f), 32);
            int first = vh.currentVertCount; vh.AddVert(center, warm, Vector2.zero);
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI * .5f + i * Mathf.PI / 5;
                float radius = i % 2 == 0 ? 12 : 5;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, warm, Vector2.zero);
            }
            for (int i = 0; i < 10; i++) vh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % 10);
        }
    }
}
