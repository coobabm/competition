using System;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Casino
{
    /// <summary>Presentation only: deal from shoe, flip, settle and drag-return. Never knows the wallet or deck.</summary>
    public sealed class CasinoCardMotion : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform Front, Back;
        public XgJuice Juice;
        public bool Reduced;
        public Func<bool> CanDrag;
        public bool Busy => mode != 0 || dragging;
        public float Progress { get; private set; }
        RectTransform rt;
        CanvasGroup group;
        Vector2 target, from, dragOffset;
        Vector3 baseScale;
        readonly Image[] trails = new Image[3];
        float elapsed, startAngle;
        int mode;
        bool faceUp, dragging, soundStarted;

        void Cache()
        {
            if (rt != null) return;
            rt = (RectTransform)transform;
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            target = rt.anchoredPosition; baseScale = rt.localScale;
        }
        public void Deal(Vector2 shoe, float delay, bool reveal)
        {
            Cache(); from = shoe; elapsed = -Mathf.Max(0, delay); faceUp = reveal; mode = 1; Progress = 0; soundStarted = false;
            rt.anchoredPosition = from; group.alpha = 0; group.blocksRaycasts = false;
            SetFace(false); MakeTrails();
        }
        public void Reveal(float delay = 0)
        {
            Cache(); faceUp = true; mode = 2; elapsed = -Mathf.Max(0, delay); Progress = 0; soundStarted = false;
            group.alpha = 1; group.blocksRaycasts = false; SetFace(false);
        }
        public void Rest(bool reveal) { Cache(); faceUp = reveal; Finish(false); }
        public void Cancel() { if (rt != null) Finish(false); }
        void SetFace(bool front)
        {
            if (Front != null) Front.gameObject.SetActive(front);
            if (Back != null) Back.gameObject.SetActive(!front);
        }
        void Update() => Step(Time.unscaledDeltaTime);
        public void Step(float seconds)
        {
            if (mode == 0 || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds) || Juice != null && Juice.IsHitStopped) return;
            elapsed += Mathf.Min(seconds, .05f);
            if (elapsed < 0) return;
            group.alpha = 1;
            if (!soundStarted) { soundStarted = true; Juice?.Play(XgJuice.Sfx.Id.Whoosh, 1.35f, .25f); }
            float travel = Reduced ? .13f : .38f, flip = Reduced ? .08f : .16f, settle = Reduced ? .04f : .14f;
            if (mode == 3)
            {
                float k = Mathf.Clamp01(elapsed / .22f); Progress = k;
                rt.anchoredPosition = Vector2.Lerp(from, target, Ease(k));
                rt.localScale = Vector3.Lerp(baseScale * 1.07f, baseScale, Ease(k));
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(startAngle, 0, Ease(k)));
                Trail(k, 0);
                if (k >= 1) Finish(true);
                return;
            }
            float afterTravel = elapsed - (mode == 1 ? travel : 0);
            Progress = Mathf.Clamp01(elapsed / (mode == 1 ? travel + (faceUp ? flip : 0) + settle : flip + settle));
            if (mode == 1 && elapsed < travel)
            {
                float k = elapsed / travel;
                rt.anchoredPosition = Path(k, Reduced ? 10 : 42);
                rt.localRotation = Quaternion.Euler(0, 0, Reduced ? 0 : Mathf.Lerp(-18, 0, Ease(k)));
                rt.localScale = Vector3.Scale(baseScale, Reduced ? Vector3.one : new Vector3(1.03f, .95f, 1));
                Trail(k, Reduced ? 10 : 42); return;
            }
            HideTrails(); rt.anchoredPosition = target; rt.localRotation = Quaternion.identity;
            if (faceUp && afterTravel < flip)
            {
                float k = Mathf.Clamp01(afterTravel / flip);
                SetFace(k >= .5f);
                rt.localScale = Vector3.Scale(baseScale, new Vector3(Mathf.Max(.025f, Mathf.Abs(Mathf.Cos(k * Mathf.PI))), 1, 1));
                return;
            }
            SetFace(faceUp);
            float landing = Mathf.Clamp01((afterTravel - (faceUp ? flip : 0)) / settle);
            float squash = Reduced ? 0 : Mathf.Sin(landing * Mathf.PI) * .10f;
            rt.localScale = Vector3.Scale(baseScale, new Vector3(1 + squash, 1 - squash, 1));
            if (landing >= 1) Finish(true);
        }
        static float Ease(float t) { t = Mathf.Clamp01(t); return 1 - Mathf.Pow(1 - t, 3); }
        Vector2 Path(float t, float arc) => Vector2.Lerp(from, target, Ease(t)) + Vector2.up * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * arc;
        void MakeTrails()
        {
            if (Reduced || rt.parent == null) return;
            for (int i = 0; i < trails.Length; i++)
            {
                if (trails[i] != null) continue;
                var ghost = PrologueDesk.Rect("Card Trail", rt.parent, rt.anchorMin, rt.anchorMax, Vector2.zero, Vector2.zero);
                ghost.pivot = rt.pivot; ghost.sizeDelta = rt.sizeDelta;
                trails[i] = PrologueDesk.Fill(ghost, new Color(1, .77f, .35f, 0), false);
                ghost.SetSiblingIndex(rt.GetSiblingIndex());
            }
        }
        void Trail(float t, float arc)
        {
            for (int i = 0; i < trails.Length; i++) if (trails[i] != null)
            {
                var image = trails[i]; image.gameObject.SetActive(true);
                image.rectTransform.anchoredPosition = Path(Mathf.Max(0, t - (i + 1) * .065f), arc);
                image.rectTransform.localRotation = rt.localRotation;
                var color = image.color; color.a = (1 - t) * (.16f - .035f * i); image.color = color;
            }
        }
        void HideTrails() { foreach (var image in trails) if (image != null) image.gameObject.SetActive(false); }
        void Finish(bool sound)
        {
            if (rt == null) return;
            mode = 0; dragging = false; Progress = 1;
            rt.anchoredPosition = target; rt.localScale = baseScale; rt.localRotation = Quaternion.identity;
            if (group != null) { group.alpha = 1; group.blocksRaycasts = true; } SetFace(faceUp); HideTrails();
            if (sound) Juice?.Play(XgJuice.Sfx.Id.Clack, 1.2f, .3f);
        }
        bool Point(PointerEventData e, out Vector2 p)
        {
            var parent = rt.parent as RectTransform;
            if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, e.pressEventCamera, out p))
            { p -= new Vector2(parent.rect.xMin, parent.rect.yMax); return true; }
            p = Vector2.zero; return false;
        }
        public void OnBeginDrag(PointerEventData e)
        {
            Cache();
            if (e.button != PointerEventData.InputButton.Left || Busy || CanDrag != null && !CanDrag() || !Point(e, out var p)) return;
            dragging = true; dragOffset = rt.anchoredPosition - p; rt.SetAsLastSibling(); rt.localScale = baseScale * 1.07f;
            Juice?.Play(XgJuice.Sfx.Id.Tick, 1, .25f);
        }
        public void OnDrag(PointerEventData e)
        {
            if (!dragging || !Point(e, out var p)) return;
            var parent = (RectTransform)rt.parent;
            p += dragOffset;
            rt.anchoredPosition = new Vector2(Mathf.Clamp(p.x, rt.rect.width * .5f, parent.rect.width - rt.rect.width * .5f),
                Mathf.Clamp(p.y, -parent.rect.height + rt.rect.height * .5f, -rt.rect.height * .5f));
            rt.localRotation = Quaternion.Euler(0, 0, Reduced ? 0 : Mathf.Clamp(e.delta.x * -.35f, -12, 12));
        }
        public void OnEndDrag(PointerEventData e)
        {
            if (!dragging) return;
            dragging = false; from = rt.anchoredPosition; startAngle = Mathf.DeltaAngle(0, rt.localEulerAngles.z);
            elapsed = 0; mode = 3; group.blocksRaycasts = false; MakeTrails();
        }
        void OnDisable() { if (rt != null) Finish(false); }
        void OnDestroy()
        {
            foreach (var image in trails) if (image != null)
            { if (Application.isPlaying) Destroy(image.gameObject); else DestroyImmediate(image.gameObject); }
        }
    }
}
