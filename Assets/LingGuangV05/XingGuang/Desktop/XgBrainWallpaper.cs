using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.Story;
using LingGuangV05.UI;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    // A save change restores its own presentation; only completing setup in the
    // currently observed game plays the first reveal. No presentation state is saved.
    public sealed class XgWallpaperReveal
    {
        object owner;
        bool complete;
        public const float FadeSeconds = .8f;
        public float Age { get; private set; }
        public bool Visible => complete;
        public float Opacity => !complete ? 0 : Mathf.SmoothStep(0, 1, Age / FadeSeconds);
        public const float CompleteSeconds = 2.65f;
        public float PartOpacity(int part) => !complete ? 0 : Mathf.SmoothStep(0, 1, (Age - .85f - part * .45f) / .45f);

        public bool Observe(object save, bool setupComplete)
        {
            if (!ReferenceEquals(owner, save))
            {
                owner = save; complete = setupComplete;
                Age = setupComplete ? 100 : 0;
                return false;
            }
            if (complete == setupComplete) return false;
            complete = setupComplete; Age = 0;
            return complete;
        }

        public void Tick(float dt) { if (complete) Age = Mathf.Min(100, Age + Mathf.Max(0, dt)); }
    }

    /// <summary>The living background belongs to the source desktop, below icons and windows.</summary>
    public sealed class XgBrainWallpaper : MonoBehaviour
    {
        XingGuangController controller;
        public bool WindowMode { get; private set; }
        public bool ReduceMotion => MotionReduced(controller);
        public static bool MotionReduced(XingGuangController owner) => GamePerformance.Low
            || owner?.Sim?.S.reduceFx == true || owner?.View?.Juice?.Reduced == true;
        public bool TryStartGlow(string region)
        {
            if (ReduceMotion) return false;
            var window = controller != null ? controller.GetComponent<XgBrainWindow>() : null;
            return !WindowMode || window == null || window.OnsetGate.TryStart(Time.unscaledTime, region);
        }
        CanvasGroup group;
        readonly CanvasGroup[] firstPoints = new CanvasGroup[4];
        ChapterOneSim observedSave;
        bool noticePending;float nextPresentation,lastPresentation;XgBrainWallpaperPresentation presentation;
        public int NoticesSent { get; private set; }
        public XgWallpaperReveal Reveal { get; } = new XgWallpaperReveal();
        public static Vector2 SeedPosition => new Vector2(.52f, .24f);
        public XgConcept SeedConcept { get; private set; }

        public static void Install(XingGuangController controller)
        {
            var bridge = FindAnyObjectByType<AeroLcdSourceBridge>(FindObjectsInactive.Include);
            var desktop = bridge != null && bridge.sourceCanvas != null ? bridge.sourceCanvas.transform.Find("Desktop") : null;
            var wallpaper = desktop != null ? desktop.Find("Wallpaper") : null;
            if (wallpaper == null || desktop.Find("Brain Wallpaper") != null) return;
            var root = PrologueDesk.Rect("Brain Wallpaper", desktop, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.SetSiblingIndex(wallpaper.GetSiblingIndex() + 1);
            // Every brain layer uses the same reference UVs as the 1280x832 window.
            // Letterbox the shared drawing instead of stretching its anatomy on wide desktops.
            var fit = root.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectRatio = XgBrainWindowBody.DesignWidth / XgBrainWindowBody.DesignHeight;
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            var view = root.gameObject.AddComponent<XgBrainWallpaper>();
            view.controller = controller;
            view.group = root.gameObject.AddComponent<CanvasGroup>();
            view.group.alpha = 0; view.group.blocksRaycasts = false; view.group.interactable = false;
            var background = root.gameObject.AddComponent<Image>();
            // The chosen system wallpaper remains visible beneath the living brain.
            // This layer owns only the brain, never a replacement opaque wallpaper.
            background.color = Color.clear; background.raycastTarget = false;
            view.BuildFirstPoints(root);
            root.gameObject.AddComponent<XgBrainWindowGlass>();
            root.gameObject.AddComponent<XgBrainWallpaperPresentation>();
            root.gameObject.AddComponent<XgWallpaperMenu>();
            XgWallpaperConcepts.Install(controller, view);
            XgWallpaperAnswerPath.Install(controller, view);
            XgWallpaperTraining.Install(controller, view);
            XgWallpaperPersona.Install(controller, view);
            XgWallpaperAbilities.Install(controller, view);
            XgWallpaperFamilyStar.Install(controller, view);
            XgWallpaperRebirth.Install(controller, view);
            XgBrainWindowBody.Install(view);
            view.Observe();
        }

        public static XgBrainWallpaper InstallWindow(XingGuangController controller, RectTransform root)
        {
            var view=root.gameObject.AddComponent<XgBrainWallpaper>();
            view.controller=controller;view.WindowMode=true;
            view.group=root.gameObject.AddComponent<CanvasGroup>();
            view.group.blocksRaycasts=false;view.group.interactable=false;
            view.BuildFirstPoints(root);
            var presentation=root.gameObject.AddComponent<XgBrainWallpaperPresentation>();
            presentation.WindowMode=true;
            XgWallpaperConcepts.Install(controller,view);
            XgWallpaperAnswerPath.Install(controller,view);
            XgWallpaperTraining.Install(controller,view);
            XgWallpaperPersona.Install(controller,view);
            XgWallpaperAbilities.Install(controller,view);
            XgWallpaperFamilyStar.Install(controller,view);
            XgBrainWindowBody.Install(view);
            view.Observe();return view;
        }

        void BuildFirstPoints(RectTransform root)
        {
            var positions = new[] { new Vector2(.52f, .53f), new Vector2(.32f, .53f), new Vector2(.72f, .53f), SeedPosition };
            var names = new[] { "Center", "Inputs", "Outputs", "Seed" };
            for (int i = 0; i < firstPoints.Length; i++)
            {
                var point = PrologueDesk.Rect(names[i], root, positions[i], positions[i], new Vector2(-36, -70), new Vector2(36, 70));
                var fade = point.gameObject.AddComponent<CanvasGroup>();
                fade.alpha = 0; fade.blocksRaycasts = false; fade.interactable = false; firstPoints[i] = fade;
                if (i == 2) { XgWallpaperOutputs.Install(controller, point); continue; }
                int count = i == 1 ? 3 : i == 2 ? 2 : 1;
                for (int n = 0; n < count; n++)
                {
                    var at = new Vector2(0, (n - (count - 1) * .5f) * 45);
                    var tint = i == 3 ? XgCortexGraphic.N.Gold : XgCortexGraphic.N.Teal;
                    for (int ring = 3; ring >= 0; ring--)
                    {
                        float radius = i == 3 && ring == 0 ? 20 : 6 + ring * 8;
                        var rt = PrologueDesk.Rect("Glow" + ring, point, Vector2.one * .5f, Vector2.one * .5f, at - Vector2.one * radius, at + Vector2.one * radius);
                        Graphic dot;
                        if (i == 3 && ring == 0) dot = rt.gameObject.AddComponent<HexCellGraphic>();
                        else { var image = rt.gameObject.AddComponent<Image>(); image.sprite = PrologueDesk.Circle(); dot = image; }
                        dot.color = new Color(tint.r, tint.g, tint.b, ring == 0 ? 1 : .025f + .02f * (3 - ring));
                        dot.raycastTarget = false;
                    }
                    if (i == 3)
                    {
                        var label = PrologueDesk.Rect("Question", point, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();
                        label.font = PrologueDesk.CjkFont(); label.text = "?"; label.fontSize = 25;
                        label.color = new Color32(40, 27, 4, 255); label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
                    }
                }
            }
        }

        void Observe()
        {
            var runtime = controller != null ? controller.runtime : null;
            if (runtime == null || runtime.Sim == null) return;
            if (observedSave != runtime.Sim) { observedSave = runtime.Sim; noticePending = false; }
            if (Reveal.Observe(runtime.Sim, !runtime.Sim.InPrologue) && !WindowMode) noticePending = true;
        }

        void Update()
        {
            if (controller == null || controller.runtime == null || !controller.runtime.isActiveAndEnabled) return;
            if(presentation==null)presentation=GetComponent<XgBrainWallpaperPresentation>();
            if(presentation!=null&&!presentation.DrawDue(ref nextPresentation))return;
            float now=Time.unscaledTime;float dt=lastPresentation==0?1f/15:now-lastPresentation;lastPresentation=now;
            Observe(); Reveal.Tick(ReduceMotion?100:dt);
            SeedConcept = null;
            var concepts = controller.Sim?.S.board?.concepts;
            if (Reveal.Visible && concepts != null)
                foreach (var concept in concepts)
                    if (concept != null && concept.seed && concept.region == "logic" && concept.key == XgSim.SeedKey)
                    { SeedConcept = concept; break; }
            // Read the existing seed only; drawing must never plant or repair it.
            firstPoints[3].gameObject.SetActive(SeedConcept != null);
            group.alpha = Reveal.Opacity;
            for (int i = 0; i < firstPoints.Length; i++) firstPoints[i].alpha = Reveal.PartOpacity(i);
            if (SeedConcept != null) firstPoints[3].alpha *= Mathf.Lerp(.55f, 1, XgWallpaperConcepts.Strength(SeedConcept));
            if (noticePending && Reveal.Age >= XgWallpaperReveal.CompleteSeconds && PrologueDirector.Desk != null)
            {
                noticePending = false;
                var save = observedSave;
                var desk = PrologueDirector.Desk;
                var notices = desk.layer.GetComponent<TrayPopups>() ?? desk.layer.gameObject.AddComponent<TrayPopups>();
                notices.desk = desk;
                notices.EnqueueNotification(EraSafeBranding.Os, GameText.T("桌面背景已更改", "Desktop background changed"), 5, null,
                    () => this != null && controller != null && controller.runtime != null && controller.runtime.Sim == save, null, systemBalloon: true);
                NoticesSent++;
            }
        }
    }
}
