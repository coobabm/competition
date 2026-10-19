using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.OS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Brighten the uncovered desktop without changing the first-meeting reveal.</summary>
    public sealed class XgBrainWallpaperPresentation : MonoBehaviour
    {
        public bool WindowMode;
        XgBrainWallpaper wallpaper;
        CanvasGroup group;
        readonly List<Graphic> lights = new List<Graphic>();
        readonly List<UiTip> tips = new List<UiTip>();
        float nextScan,nextPresentation;
        public int DataVersion { get; private set; } = 1;
        bool lastLow;
        public void DataChanged(){DataVersion=DataVersion==1000000?1:DataVersion+1;}
        void OnEnable(){DataChanged();nextPresentation=0;}
        UiTip hovered;
        UiTip conceptTip;
        XgWallpaperConcepts concepts;
        int hoveredConcept = -1;
        float nextTipRefresh;
        XgWallpaperRegionCard regionCard;
        public bool Showcase { get; private set; }
        public bool Quiet { get; private set; }
        readonly Vector3[] coverageCorners = new Vector3[4];
        public bool FullyCovered { get; private set; }
        public int BackgroundFrameRate => FullyCovered && !XgWallpaperRebirth.Playing ? 10 : 15;
        public float DrawInterval => 1f / BackgroundFrameRate;
        public bool DrawDue(ref float next)
        {
            float now = Time.unscaledTime;
            if(lastLow!=GamePerformance.Low){lastLow=GamePerformance.Low;DataChanged();}
            if(GamePerformance.Low){float stamp=-DataVersion;if(next==stamp)return false;next=stamp;return true;}
            if (next - now > DrawInterval + .001f) next = now;
            if (now < next) return false;
            next = now + DrawInterval; return true;
        }

        bool CoversDesktop(RectTransform window)
        {
            var desktop = transform.parent as RectTransform;
            if (desktop == null || !IsVisible(window)) return false;
            window.GetWorldCorners(coverageCorners);
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var point in coverageCorners)
            {
                Vector2 local = desktop.InverseTransformPoint(point); min = Vector2.Min(min, local); max = Vector2.Max(max, local);
            }
            var area = desktop.rect;
            // Allow the desktop taskbar strip; small normal windows must never trigger the low rate.
            return min.x <= area.xMin + area.width * .02f && max.x >= area.xMax - area.width * .02f
                && min.y <= area.yMin + area.height * .06f && max.y >= area.yMax - area.height * .02f;
        }
        public float MotionLevel { get; private set; } = 1;
        public float LightLevel { get; private set; } = .62f;
        public void SetQuiet(bool quiet) { Quiet = quiet; }

        void Start()
        {
            wallpaper = GetComponent<XgBrainWallpaper>();
            group = GetComponent<CanvasGroup>();
            foreach (Transform part in transform)
            {
                var fade = part.GetComponent<CanvasGroup>();
                if (fade == null) continue;
                fade.blocksRaycasts = true;
                foreach (var light in part.GetComponentsInChildren<Graphic>(true))
                {
                    lights.Add(light);
                    if (light.name != "Glow0") continue;
                    string key = part.name;
                    tips.Add(UiTip.Add(light, () => Description(key)));
                }
            }
            // Win7's transparent desktop context-menu surface sits above the
            // wallpaper. Keep its right-click behavior and relay hover beneath it.
            var context = transform.parent.Find("Desktop Context Menu");
            if (context != null) context.gameObject.AddComponent<XgBrainWallpaperHover>().owner = this;
            concepts = GetComponentInChildren<XgWallpaperConcepts>();
            var hoverTarget = LingGuangV05.Desktop.Story.PrologueDesk.Rect("Concept Hover Target", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // One non-graphic target serves all cells. The real desktop surface
            // relays hover only where icons and application windows do not cover it.
            conceptTip = UiTip.Add(hoverTarget, () => concepts != null ? concepts.DescribeConcept(hoveredConcept) : null);
            regionCard = gameObject.AddComponent<XgWallpaperRegionCard>();
        }

        internal void Click(PointerEventData pointer)
        {
            if (pointer == null || regionCard == null) return;
            if (pointer.button != PointerEventData.InputButton.Left)
            {
                regionCard.Hide();
                if (pointer.button == PointerEventData.InputButton.Right)
                    FindAnyObjectByType<HongmengOS.Aero2010.AeroLcdSourceBridge>()?.AnchorContextMenu(pointer.position);
                return;
            }
            if (!wallpaper.Reveal.Visible || wallpaper.Reveal.Age < XgWallpaperReveal.CompleteSeconds) return;
            int region = concepts != null ? concepts.HitRegion(pointer.position, pointer.enterEventCamera) : -1;
            if (region < 0) regionCard.Hide();
            else regionCard.Show(region, pointer.position, pointer.enterEventCamera);
        }

        internal void Hover(PointerEventData pointer)
        {
            UiTip target = null;
            int concept = -1;
            if (pointer != null && wallpaper.Reveal.Visible && wallpaper.Reveal.Age >= XgWallpaperReveal.CompleteSeconds)
            {
                if (concepts != null) concept = concepts.HitConcept(pointer.position, pointer.enterEventCamera);
                if (concept >= 0) target = conceptTip;
                else
                foreach (var tip in tips)
                    if (tip != null && tip.isActiveAndEnabled && RectTransformUtility.RectangleContainsScreenPoint(
                        tip.transform as RectTransform, pointer.position, pointer.enterEventCamera)) { target = tip; break; }
            }
            if (target != hovered || concept != hoveredConcept)
            {
                if (hovered != null) hovered.OnPointerExit(pointer);
                hoveredConcept = concept;
                hovered = target;
                if (hovered != null) hovered.OnPointerEnter(pointer);
            }
            if (hovered != null) hovered.OnPointerMove(pointer);
            if (hovered == conceptTip && Time.unscaledTime >= nextTipRefresh)
            {
                nextTipRefresh = Time.unscaledTime + .15f;
                conceptTip.Refresh();
            }
        }

        string Description(string part)
        {
            switch (part)
            {
                case "Inputs": return GameText.T("输入：你教给它的东西，从这里进来。", "Input: what you teach it enters here.");
                case "Outputs": return GameText.T("输出：它现在能说——", "Output: it can now use ") + GetComponentInChildren<XgWallpaperOutputs>(true).FormLabel;
                case "Seed": return GameText.T("？：它心里留着一个还没说出口的问题。", "?: a question it has not said aloud yet.");
                default: return GameText.T("每教它一次，这里都会留下痕迹。", "Each lesson leaves a trace here.");
            }
        }

        // Minimize leaves WindowManager.isOn true. Use the rendered state, including
        // ancestor fades, so closing/minimizing animations finish before the reveal.
        public static bool IsVisible(RectTransform rect)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy || rect.rect.width < 1 || rect.rect.height < 1) return false;
            float alpha = 1;
            for (Transform node = rect; node != null; node = node.parent)
            {
                var fade = node.GetComponent<CanvasGroup>();
                if (fade != null) alpha *= fade.alpha;
            }
            // The source desktop itself is a very small world-space canvas;
            // its world scale says nothing about a window being minimized.
            return alpha > .02f && Mathf.Abs(rect.localScale.x * rect.localScale.y) > .001f;
        }

        bool HasVisibleWindow()
        {
            bool visible = false; FullyCovered = false;
            foreach (var window in FindObjectsByType<WindowManager>(FindObjectsSortMode.None))
                if (window.transform.IsChildOf(transform.parent) && IsVisible(window.windowContainer))
                { visible = true; FullyCovered |= CoversDesktop(window.windowContainer); }
            var windows = transform.parent.Find("Apps & Windows");
            if (windows != null)
                foreach (Transform child in windows)
                    if (child.Find("Title") != null && child.Find("Client") != null && IsVisible(child as RectTransform))
                    { visible = true; FullyCovered |= CoversDesktop(child as RectTransform); }
            return visible;
        }

        void Update()
        {
            if (wallpaper == null || !DrawDue(ref nextPresentation)) return;
            if (WindowMode) { Showcase=true;FullyCovered=false;LightLevel=1;MotionLevel=GamePerformance.Low?0:Quiet?.15f:1;group.blocksRaycasts=false;return; }
            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + .2f;
                Showcase = wallpaper.Reveal.Visible && !HasVisibleWindow();
            }
            LightLevel = Mathf.MoveTowards(LightLevel, (Showcase ? 1 : .62f) * (Quiet ? .4f : 1), Time.unscaledDeltaTime * .65f);
            MotionLevel = GamePerformance.Low?0:Mathf.MoveTowards(MotionLevel, Quiet ? .15f : 1, DrawInterval);
            foreach (var light in lights) if (light != null) light.canvasRenderer.SetAlpha(LightLevel);
            bool hover = wallpaper.Reveal.Visible && wallpaper.Reveal.Age >= XgWallpaperReveal.CompleteSeconds;
            group.blocksRaycasts = hover;
            foreach (var tip in tips) if (tip != null) tip.enabled = hover;
        }
    }

    public sealed class XgBrainWallpaperHover : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler
    {
        internal XgBrainWallpaperPresentation owner;
        PointerEventData pointer;
        public void OnPointerEnter(PointerEventData e) { pointer = e; owner?.Hover(e); }
        public void OnPointerMove(PointerEventData e) { pointer = e; owner?.Hover(e); }
        public void OnPointerExit(PointerEventData e) { pointer = null; owner?.Hover(null); }
        public void OnPointerDown(PointerEventData e) { pointer = null; owner?.Hover(null); }
        public void OnPointerClick(PointerEventData e) { owner?.Click(e); }
        void Update() { if (pointer != null && owner != null) owner.Hover(pointer); }
        void OnDisable() { pointer = null; owner?.Hover(null); }
    }
}
