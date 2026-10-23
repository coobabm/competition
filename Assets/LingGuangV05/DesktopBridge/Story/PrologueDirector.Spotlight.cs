using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// 首次「打开灵光」 (玩家反馈 F3, 技能树与自动化 v4 §7.2). When 灵光.exe has been waiting on the desktop for
    /// <see cref="SpotlightAfter"/> seconds the first time, the desktop dims to <see cref="SpotlightDim"/> except for a soft
    /// hole around the icon, the icon breathes inside a mint ring, and the spider comes out early (before the setup it is
    /// otherwise never on the desktop), walks or drops to the icon and rubs its front legs at it. A click on the dimmed
    /// desktop flashes it; the third shows 「双击打开它」 under the icon. Opening the exe, <see cref="SpotlightSeconds"/>
    /// or Esc lifts the dim; only opening it sends the spider away (with a hop), so after a timeout it keeps beckoning.
    /// Later nudges have no dim: the spider walks over and rubs a round, and the icon rocks as before.
    /// </summary>
    public sealed partial class PrologueDirector
    {
        public const float SpotlightAfter = 6, SpotlightSeconds = 20, SpotlightDim = .62f, SpotlightRadius = 72, SpotlightFeather = 24;
        PrologueSpotlightMask spotlight;
        bool spotlightShown;
        Coroutine spotlightRun;
        RectTransform spotlightIcon;
        Vector3 spotlightIconScale = Vector3.one;

        /// <summary>The dim around 灵光.exe is on screen (QA reads it).</summary>
        public bool SpotlightOn => spotlight != null && spotlight.isActiveAndEnabled;
        /// <summary>The first spotlight has been shown in this prologue.</summary>
        public bool SpotlightShown => spotlightShown;

        bool ReducedFx => LingGuangV05.Desktop.Tieba.TiebaHub.Lab()?.S.reduceFx ?? false;

        bool ExeOpen()
        {
            var app = router != null ? router.Get(LingGuangInstallFlow.AppId) : null;
            return app != null && app.Window != null && app.Window.isOn;
        }

        /// <summary>The first time only: dim, ring and spider until the exe opens, 20 s pass or Esc.</summary>
        IEnumerator Spotlight()
        {
            spotlightShown = true;
            var icon = ExeIcon();
            if (icon == null || desk == null || desk.windows == null) { spotlightRun = null; yield break; }
            Debug.Log("[PrologueTiming] spotlight");
            bool reduced = ReducedFx;
            var spider = XgResidentSpider.Instance;
            var mask = BuildSpotlight(icon);
            var restScale = icon.localScale;
            spotlightIcon = icon; spotlightIconScale = restScale;
            float shown = 0, clock = 0;
            int clicks = 0;
            mask.Clicked = () =>
            {
                clicks++; mask.Flash();
                if (clicks == 3 && mask.Note != null) mask.Note.gameObject.SetActive(true);
            };
            bool beckoning = false;
            while (!ExeOpen())
            {
                float dt = Time.unscaledDeltaTime;
                clock += dt;
                if (mask != null)
                {
                    shown = Mathf.Min(1, shown + dt / .4f);
                    // Right above the icons and windows, right under the spider (which sits just after it) and the tray layer.
                    var apps = SpotlightApps();
                    int want = apps != null && apps.parent == mask.transform.parent ? apps.GetSiblingIndex() + 1 : mask.transform.GetSiblingIndex();
                    if (mask.transform.GetSiblingIndex() != want) mask.transform.SetSiblingIndex(want);
                    mask.Center = mask.LocalCenterOf(icon);
                    mask.Dim = SpotlightDim * (1 - (1 - shown) * (1 - shown));
                    float breath = reduced ? .5f : .5f + .5f * Mathf.Sin(clock * Mathf.PI * 2 / 1.6f);
                    mask.RingAlpha = clock < .4f ? 0 : reduced ? .4f : .2f + .4f * breath;
                    icon.localScale = restScale * (reduced || clock < .4f ? 1 : 1 + .06f * breath);
                    if (clock >= SpotlightSeconds || EscapeKey.TryConsume()) { icon.localScale = restScale; yield return LiftSpotlight(mask); mask = null; }
                }
                // The spider comes out 0.4 s in and stays by the icon until the exe opens.
                if (!beckoning && clock >= .4f && spider != null) beckoning = spider.Beckon(icon, reduced);
                yield return null;
            }
            if (icon != null) icon.localScale = restScale;
            spotlightIcon = null;
            if (mask != null) yield return LiftSpotlight(mask, .25f);
            if (spider != null && beckoning) spider.EndBeckon(true);
            spotlightRun = null;
        }

        /// <summary>Stopping the prologue: no dim, no breathing icon, no beckoning spider left behind.</summary>
        void EndSpotlight()
        {
            if (spotlightIcon != null) spotlightIcon.localScale = spotlightIconScale;
            spotlightIcon = null; spotlightRun = null; spotlightShown = false;
            RemoveSpotlight();
            XgResidentSpider.Instance?.EndBeckon(false);
        }

        /// <summary>A later nudge to open the exe: no dim, the spider walks over and rubs a round (and the icon rocks).</summary>
        IEnumerator SpiderNudge()
        {
            var spider = XgResidentSpider.Instance; var icon = ExeIcon();
            if (spider == null || icon == null || !spider.Beckon(icon, ReducedFx)) yield break;
            for (float t = 0; t < 6 && !ExeOpen(); t += Time.unscaledDeltaTime) yield return null;
            spider.EndBeckon(ExeOpen());
        }

        /// <summary>The desktop's window layer, the same one the spider's layer follows (XgResidentSpider.Find).</summary>
        static RectTransform SpotlightApps()
        {
            var source = GameObject.Find("hongmengos 2010 Desktop Source");
            var desktop = source != null ? source.transform.Find("Desktop") : null;
            return desktop != null ? desktop.Find("Apps & Windows") as RectTransform : null;
        }

        PrologueSpotlightMask BuildSpotlight(RectTransform icon)
        {
            RemoveSpotlight();
            // A desktop layer right after the windows: above the icons and windows, under the spider (XgResidentSpider puts its
            // layer right after this one while it beckons) and under the prologue's own layer with the tray popups.
            var apps = SpotlightApps();
            var desktop = apps != null ? (RectTransform)apps.parent : desk.layer.parent as RectTransform;
            var rt = PrologueDesk.Rect("Prologue Spotlight", desktop, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (apps != null) rt.SetSiblingIndex(apps.GetSiblingIndex() + 1);
            rt.gameObject.AddComponent<CanvasRenderer>(); // added first, as the spider's layer does: the graphic does not bring one here
            spotlight = rt.gameObject.AddComponent<PrologueSpotlightMask>();
            spotlight.Radius = SpotlightRadius; spotlight.Feather = SpotlightFeather; spotlight.Dim = 0;
            spotlight.Center = spotlight.LocalCenterOf(icon);
            var note = desk.Text(PrologueDesk.Rect("Note", rt, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero), T("双击打开它", "Double-click to open it"), 22, new Color32(95, 227, 181, 255), TextAlignmentOptions.Center);
            note.rectTransform.sizeDelta = new Vector2(320, 34);
            note.rectTransform.anchoredPosition = spotlight.Center - new Vector2(0, SpotlightRadius + 28);
            note.raycastTarget = false;
            note.gameObject.SetActive(false);
            spotlight.Note = note;
            return spotlight;
        }

        IEnumerator LiftSpotlight(PrologueSpotlightMask mask, float seconds = .25f)
        {
            if (mask == null) yield break;
            float from = mask.Dim, ring = mask.RingAlpha;
            mask.raycastTarget = false;
            for (float t = 0; t < seconds && mask != null; t += Time.unscaledDeltaTime)
            {
                float k = 1 - t / seconds;
                mask.Dim = from * k; mask.RingAlpha = ring * k;
                yield return null;
            }
            if (mask != null) PrologueDesk.RemoveTransient(mask.gameObject);
            if (spotlight == mask) spotlight = null;
        }

        void RemoveSpotlight()
        {
            if (spotlight != null) PrologueDesk.RemoveTransient(spotlight.gameObject);
            spotlight = null;
            var apps = SpotlightApps();
            var desktop = apps != null ? apps.parent : desk != null && desk.layer != null ? desk.layer.parent : null;
            if (desktop != null)
                for (int i = desktop.childCount - 1; i >= 0; i--)
                    if (desktop.GetChild(i).name == "Prologue Spotlight") PrologueDesk.RemoveTransient(desktop.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// The dim over the desktop with a soft round hole (one mesh: the hole, a feathered edge, then out to the rect's edges,
    /// corners included) and a breathing ring at the hole's rim. Clicks inside the hole go through to the icon.
    /// </summary>
    public sealed class PrologueSpotlightMask : MaskableGraphic, ICanvasRaycastFilter, IPointerClickHandler
    {
        public float Radius = 72, Feather = 24;
        public System.Action Clicked;
        public TMP_Text Note;
        Vector2 center;
        float dim, ringAlpha, flash;
        static readonly Color Void = new Color32(5, 7, 10, 255), Mint = new Color32(95, 227, 181, 255);

        public Vector2 Center { get => center; set { if (center != value) { center = value; SetVerticesDirty(); } } }
        public float Dim { get => dim; set { if (!Mathf.Approximately(dim, value)) { dim = value; SetVerticesDirty(); } } }
        public float RingAlpha { get => ringAlpha; set { if (!Mathf.Approximately(ringAlpha, value)) { ringAlpha = value; SetVerticesDirty(); } } }

        public Vector2 LocalCenterOf(RectTransform target)
        {
            if (target == null) return center;
            return rectTransform.InverseTransformPoint(target.TransformPoint(target.rect.center));
        }

        public void Flash() { flash = .08f; SetVerticesDirty(); }

        void Update()
        {
            if (flash <= 0) return;
            flash = Mathf.Max(0, flash - Time.unscaledDeltaTime * (.08f / .15f));
            SetVerticesDirty();
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local)) return true;
            return (local - center).magnitude > Radius;
        }

        public void OnPointerClick(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) Clicked?.Invoke(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            var shade = Void; shade.a = Mathf.Clamp01(dim + (dim > 0 ? flash : 0));
            var clear = shade; clear.a = 0;
            var angles = new List<float>(72);
            const int n = 64;
            for (int i = 0; i < n; i++) angles.Add(i * Mathf.PI * 2 / n);
            foreach (var corner in new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) })
            {
                float a = Mathf.Atan2(corner.y - center.y, corner.x - center.x);
                angles.Add(a < 0 ? a + Mathf.PI * 2 : a);
            }
            angles.Sort();
            float outer = Radius + Feather;
            int count = angles.Count;
            for (int i = 0; i < count; i++)
            {
                var d = new Vector2(Mathf.Cos(angles[i]), Mathf.Sin(angles[i]));
                float reach = Edge(r, d);
                vh.AddVert(center + d * Radius, clear, Vector2.zero);
                vh.AddVert(center + d * Mathf.Min(outer, reach), shade, Vector2.zero);
                vh.AddVert(center + d * Mathf.Max(reach, outer), shade, Vector2.zero);
            }
            for (int i = 0; i < count; i++)
            {
                int a = i * 3, b = (i + 1) % count * 3;
                vh.AddTriangle(a, b, b + 1); vh.AddTriangle(a, b + 1, a + 1);
                vh.AddTriangle(a + 1, b + 1, b + 2); vh.AddTriangle(a + 1, b + 2, a + 2);
            }
            if (ringAlpha <= 0) return;
            // The mint ring at the rim of the hole, with a faint outer glow.
            var ring = Mint; ring.a = ringAlpha;
            var glow = Mint; glow.a = 0;
            int start = vh.currentVertCount;
            for (int i = 0; i < n; i++)
            {
                var d = new Vector2(Mathf.Cos(i * Mathf.PI * 2 / n), Mathf.Sin(i * Mathf.PI * 2 / n));
                vh.AddVert(center + d * (Radius - 3), ring, Vector2.zero);
                vh.AddVert(center + d * Radius, ring, Vector2.zero);
                vh.AddVert(center + d * (Radius + 10), glow, Vector2.zero);
            }
            for (int i = 0; i < n; i++)
            {
                int a = start + i * 3, b = start + (i + 1) % n * 3;
                vh.AddTriangle(a, b, b + 1); vh.AddTriangle(a, b + 1, a + 1);
                vh.AddTriangle(a + 1, b + 1, b + 2); vh.AddTriangle(a + 1, b + 2, a + 2);
            }
        }

        /// <summary>How far a ray from the centre in direction <paramref name="d"/> runs before it leaves the rect.</summary>
        float Edge(Rect r, Vector2 d)
        {
            float t = float.MaxValue;
            if (d.x > 1e-5f) t = Mathf.Min(t, (r.xMax - center.x) / d.x); else if (d.x < -1e-5f) t = Mathf.Min(t, (r.xMin - center.x) / d.x);
            if (d.y > 1e-5f) t = Mathf.Min(t, (r.yMax - center.y) / d.y); else if (d.y < -1e-5f) t = Mathf.Min(t, (r.yMin - center.y) / d.y);
            return Mathf.Max(0, t == float.MaxValue ? 0 : t);
        }
    }
}
