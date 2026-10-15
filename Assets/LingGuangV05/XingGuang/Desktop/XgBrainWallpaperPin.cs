using System.Collections.Generic;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>A non-interactive second drawing of the living wallpaper, above app windows.</summary>
    public sealed class XgBrainWallpaperPin : MonoBehaviour
    {
        sealed class Copy
        {
            public Graphic source;
            public CanvasRenderer renderer;
        }
        readonly List<Graphic> graphics = new List<Graphic>();
        readonly Dictionary<Graphic, Copy> copies = new Dictionary<Graphic, Copy>();
        readonly List<Graphic> retired = new List<Graphic>();
        XgBrainWallpaper wallpaper;
        RectTransform overlay, chip;
        CanvasGroup overlayGroup, chipGroup;
        TMP_Text label;
        float nextDraw;
        public bool IsPinned { get; private set; }
        public RectTransform ToggleRect => chip;
        public CanvasGroup Overlay => overlayGroup;

        void Start()
        {
            wallpaper = GetComponent<XgBrainWallpaper>();
            var sourceCanvas = GetComponentInParent<Canvas>().rootCanvas;
            overlay = PrologueDesk.Rect("Brain Wallpaper Overlay", transform.parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.gameObject.layer = sourceCanvas.gameObject.layer;
            var canvas = overlay.gameObject.AddComponent<Canvas>();
            canvas.worldCamera = sourceCanvas.worldCamera;
            canvas.additionalShaderChannels = sourceCanvas.additionalShaderChannels
                | AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
            canvas.overrideSorting = true; canvas.sortingOrder = 29000;
            overlayGroup = overlay.gameObject.AddComponent<CanvasGroup>();
            overlayGroup.alpha = .3f; overlayGroup.blocksRaycasts = false; overlayGroup.interactable = false;
            overlay.gameObject.SetActive(false);
            Canvas.willRenderCanvases += Draw;
            InstallToggle();
        }

        void InstallToggle()
        {
            RectTransform taskbar = null;
            foreach (var rect in GetComponentInParent<Canvas>().rootCanvas.GetComponentsInChildren<RectTransform>(true))
                if (rect.name == "Taskbar") { taskbar = rect; break; }
            if (taskbar == null) return;
            var area = taskbar.Find("Aero Notification Area") as RectTransform;
            float right = area != null ? area.rect.width : 242;
            chip = PrologueDesk.Rect("Brain Pin Toggle", taskbar, new Vector2(1, 0), Vector2.one,
                new Vector2(-right - 334, 3), new Vector2(-right - 268, -3));
            chipGroup = chip.gameObject.AddComponent<CanvasGroup>();
            chipGroup.alpha = 0; chipGroup.blocksRaycasts = false;
            var background = PrologueDesk.Fill(chip, new Color(0, 0, 0, .12f));
            var button = chip.gameObject.AddComponent<Button>(); button.targetGraphic = background;
            button.onClick.AddListener(() => SetPinned(!IsPinned));
            label = PrologueDesk.Rect("Label", chip, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = PrologueDesk.CjkFont(); label.fontSize = 14; label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            UiTip.Add(chip, () => GameText.T("大脑置顶：以 30% 透明度浮在窗口上方，不影响点击。", "Pin brain: show it over windows at 30% opacity. Clicks pass through."));
        }

        public void SetPinned(bool value)
        {
            IsPinned = value && wallpaper != null && wallpaper.Reveal.Visible;
            if (overlay != null) overlay.gameObject.SetActive(IsPinned);
            nextDraw = 0;
        }

        void Update()
        {
            bool ready = wallpaper != null && wallpaper.Reveal.Visible && wallpaper.Reveal.Age >= XgWallpaperReveal.CompleteSeconds;
            if (!ready && IsPinned) SetPinned(false);
            if (chipGroup != null)
            {
                chipGroup.alpha = ready ? 1 : 0; chipGroup.blocksRaycasts = ready;
                label.text = GameText.T(IsPinned ? "置顶 · 开" : "置顶", IsPinned ? "Pin: on" : "Pin");
                label.color = IsPinned ? new Color32(155, 255, 212, 255) : Color.white;
            }
        }

        void Draw()
        {
            if (!IsPinned || overlay == null || Time.unscaledTime < nextDraw) return;
            nextDraw = Time.unscaledTime + 1f / 30;
            graphics.Clear(); GetComponentsInChildren(false, graphics);
            retired.Clear();
            foreach (var pair in copies)
                if (pair.Key == null || !graphics.Contains(pair.Key)) retired.Add(pair.Key);
            foreach (var source in retired)
            {
                var copy = copies[source]; Destroy(copy.renderer.gameObject); copies.Remove(source);
            }
            foreach (var source in graphics)
            {
                // The opaque base stays on the desktop. Only the brain lights and
                // labels float over windows; never duplicate scripts, input or tips.
                if (source.transform == transform) continue;
                if (!copies.TryGetValue(source, out var copy))
                {
                    var rect = PrologueDesk.Rect("Mirror " + source.name, overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    rect.gameObject.layer = overlay.gameObject.layer;
                    copy = new Copy { source = source, renderer = rect.gameObject.AddComponent<CanvasRenderer>() };
                    copies.Add(source, copy);
                }
                // Share the already-built UI mesh without modifying it. The copy
                // owns only its renderer, so no geometry or texture is allocated per frame.
                copy.renderer.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                var scale = overlay.lossyScale;
                copy.renderer.transform.localScale = Vector3.Scale(source.transform.lossyScale,
                    new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z));
                copy.renderer.materialCount = 1;
                var material = source.materialForRendering;
                copy.renderer.SetMaterial(material, 0);
                // TMP uses its material's font atlas; Graphic.mainTexture is the
                // default white image there and would fill the whole glyph quad.
                copy.renderer.SetTexture(source is TMP_Text || source is TMP_SubMeshUI ? material.mainTexture : source.mainTexture);
                copy.renderer.SetColor(new Color(1, 1, 1, source.canvasRenderer.GetAlpha() * source.canvasRenderer.GetInheritedAlpha()));
                copy.renderer.SetMesh(source.canvasRenderer.GetMesh());
                copy.renderer.cull = source.canvasRenderer.cull;
            }
        }

        void OnDisable() { SetPinned(false); }
        void OnDestroy()
        {
            Canvas.willRenderCanvases -= Draw;
            if (overlay != null) Destroy(overlay.gameObject);
            if (chip != null) Destroy(chip.gameObject);
        }
    }
}
