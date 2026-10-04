using System;
using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.Bodu;
using LingGuangV05.Desktop.Tieba;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// The 便笺: a yellow Windows 7 sticky note pinned near the top-right of the fake desktop, written as the
    /// protagonist's own to-do list. It reads <see cref="XgGuide.Current"/> about twice a second and shows the
    /// current task in bold with up to two greyed ones below. A finished task is ticked and struck through for a
    /// moment, then slides away and the next one fades in. Clicking the current task opens the place it is about
    /// (a lab tab, the forum, 摆渡, YY, the home or shop app) and rings the control in gold for a few seconds;
    /// hovering any task tells why. "–" folds it to a slim strip for the rest of the session, and the title bar
    /// drags it. It never shows during the prologue or a cutscene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GuideNote : MonoBehaviour
    {
        const float Width = 300, TitleHeight = 26, Pad = 10, RightMargin = 14, TopMargin = 44;
        const float RefreshEvery = .5f, TickHold = .9f, SlideTime = .35f, FadeIn = .25f;
        const string SeenPref = "LingGuangV05.Guide.Seen.";
        static readonly Color Paper = new Color32(254, 249, 178, 255), Bar = new Color32(247, 236, 128, 255), Edge = new Color32(214, 196, 92, 255);
        static readonly Color Ink = new Color32(58, 50, 26, 255), Soft = new Color32(138, 126, 88, 255), Done = new Color32(70, 140, 60, 255);
        static readonly Color Hover = new Color32(120, 96, 10, 255);

        /// <summary>Folded to a strip; remembered for the session only.</summary>
        static bool collapsed;
        /// <summary>Where the player dragged it (offset from its corner), for the session.</summary>
        static Vector2 dragged;

        MonoBehaviour host;
        ChapterOneRuntime runtime;
        ChapterOneDesktopRouter router;
        XingGuangController lab;
        PrologueDesk desk;

        RectTransform note, body, titleBar;
        TMP_Text title, foldLabel;
        sealed class Row { public RectTransform rt; public TMP_Text label; public CanvasGroup group; public Button button; public XgGuideStep step; }
        readonly Row[] rows = new Row[3];

        readonly List<XgGuideStep> shown = new List<XgGuideStep>();
        List<XgGuideStep> pending;
        XgSim shownFor;
        bool busy, english;
        float nextRefresh;
        WindowManager tiebaWindow, gamesWindow;
        bool windowsLooked;

        /// <summary>Adds the note next to the story presenter (once). Call from its Start().</summary>
        public static GuideNote Install(MonoBehaviour host)
        {
            if (host == null) return null;
            var note = host.GetComponent<GuideNote>() ?? host.gameObject.AddComponent<GuideNote>();
            note.host = host;
            return note;
        }

        void Start()
        {
            runtime = FindAnyObjectByType<ChapterOneRuntime>();
            router = FindAnyObjectByType<ChapterOneDesktopRouter>(FindObjectsInactive.Include);
        }

        void OnDestroy() { if (note != null) Destroy(note.gameObject); }

        bool CutscenePlaying => host is StoryDesktopPresenter presenter && presenter.CutscenePlaying;

        XingGuangController Lab()
        {
            if (lab == null) lab = FindAnyObjectByType<XingGuangController>();
            return lab;
        }

        void Update()
        {
            if (runtime == null) runtime = FindAnyObjectByType<ChapterOneRuntime>();
            bool ready = runtime != null && runtime.Sim != null && !runtime.TestMode && !runtime.Sim.InPrologue && !CutscenePlaying
                && Lab() != null && lab.Sim != null && PrologueDirector.Desk != null;
            if (ready && note == null) Build(PrologueDirector.Desk);
            if (note == null) return;
            if (note.gameObject.activeSelf != ready) note.gameObject.SetActive(ready);
            if (!ready || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshEvery;
            NoteSeenPlaces();
            var steps = XgGuide.Current(lab.Sim, runtime.Sim.S.money, House());
            if (busy) { pending = steps; return; }
            Apply(steps);
        }

        // ───────────── what the house knows ─────────────

        XgGuideHouse House()
        {
            var sim = runtime.Sim; var s = sim.S; var config = sim.Config;
            var hub = TiebaHub.Instance;
            return new XgGuideHouse
            {
                appInstalled = sim.AppInstalled,
                unpaidPower = s.unpaidPower, breakerTripped = s.breakerTripped, billDue = s.billDue,
                noGpu = s.gpuCount <= 0,
                overloaded = s.gpuCount * config.gpuWatts + s.caseCount * config.caseWatts > config.powerLimitWatts,
                vramMB = lab.Host != null ? lab.Host.VramMB : 0,
                zhouAvailable = hub == null || hub.S == null || hub.CanMessage(ForumLibrary.LaoZhou),
                forumSeen = Seen("forum"), yySeen = Seen("yy"), gamesSeen = Seen("games"),
            };
        }

        static bool Seen(string place) => PlayerPrefs.GetInt(SeenPref + place, 0) == 1;

        /// <summary>Optional places count as visited once their window has been open.</summary>
        void NoteSeenPlaces()
        {
            if (!windowsLooked)
            {
                windowsLooked = true;
                foreach (var w in FindObjectsByType<WindowManager>(FindObjectsInactive.Include))
                {
                    if (w.name == TiebaView.NativeWindow) tiebaWindow = w;
                    else if (w.name == LingGuangV05.Desktop.Games.GameHubView.NativeWindow) gamesWindow = w;
                }
            }
            var yy = router != null ? router.Get("yy") : null;
            Mark("forum", tiebaWindow != null && tiebaWindow.isOn);
            Mark("games", gamesWindow != null && gamesWindow.isOn);
            Mark("yy", yy != null && yy.IsOpen);
        }

        static void Mark(string place, bool open) { if (open && !Seen(place)) PlayerPrefs.SetInt(SeenPref + place, 1); }

        // ───────────── layout ─────────────

        void Build(PrologueDesk d)
        {
            desk = d;
            note = PrologueDesk.Rect("Guide Note", d.layer, Vector2.one, Vector2.one, Vector2.zero, Vector2.zero);
            note.pivot = Vector2.one;
            note.sizeDelta = new Vector2(Width, 120);
            note.anchoredPosition = new Vector2(-RightMargin, -TopMargin) + dragged;
            // A soft drop shadow, then the paper with a thin darker edge.
            var shadow = PrologueDesk.Rect("Shadow", note, Vector2.zero, Vector2.one, new Vector2(3, -4), new Vector2(3, -4));
            PrologueDesk.Fill(shadow, new Color(0, 0, 0, .22f), false);
            PrologueDesk.Fill(PrologueDesk.Rect("Edge", note, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Edge, true);
            body = PrologueDesk.Rect("Paper", note, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
            PrologueDesk.Fill(body, Paper, false);

            titleBar = PrologueDesk.Rect("Title Bar", body, new Vector2(0, 1), Vector2.one, new Vector2(0, -TitleHeight), Vector2.zero);
            PrologueDesk.Fill(titleBar, Bar, true);
            var drag = titleBar.gameObject.AddComponent<NoteDrag>(); drag.owner = this;
            title = d.Text(PrologueDesk.Rect("Title", titleBar, Vector2.zero, Vector2.one, new Vector2(Pad, 0), new Vector2(-32, 0)), "", 14, Soft, TextAlignmentOptions.MidlineLeft);
            title.textWrappingMode = TextWrappingModes.NoWrap; title.overflowMode = TextOverflowModes.Ellipsis;
            var fold = PrologueDesk.Rect("Fold", titleBar, new Vector2(1, 0), Vector2.one, new Vector2(-28, 2), new Vector2(-3, -2));
            var foldImage = PrologueDesk.Fill(fold, Color.white, true);
            var foldButton = fold.gameObject.AddComponent<Button>(); foldButton.targetGraphic = foldImage;
            foldButton.colors = Tint(0, .18f, .3f);
            foldButton.onClick.AddListener(() => { collapsed = !collapsed; Layout(); });
            foldLabel = d.Text(PrologueDesk.Rect("Label", fold, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "–", 18, Soft, TextAlignmentOptions.Center);
            UiTip.Add(fold, () => collapsed ? GameText.T("展开便笺", "Unfold the note") : GameText.T("收起便笺", "Fold the note"));

            for (int i = 0; i < rows.Length; i++)
            {
                var row = new Row();
                row.rt = PrologueDesk.Rect("Task " + i, body, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
                var image = PrologueDesk.Fill(row.rt, Hover, true);
                row.group = row.rt.gameObject.AddComponent<CanvasGroup>();
                row.label = d.Text(PrologueDesk.Rect("Text", row.rt, Vector2.zero, Vector2.one, new Vector2(Pad, 0), new Vector2(-Pad, 0)), "", i == 0 ? 17 : 14, i == 0 ? Ink : Soft, TextAlignmentOptions.MidlineLeft);
                row.label.overflowMode = TextOverflowModes.Ellipsis;
                row.label.textWrappingMode = i == 0 ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                if (i == 0)
                {
                    row.label.fontStyle = FontStyles.Bold;
                    row.button = row.rt.gameObject.AddComponent<Button>(); row.button.targetGraphic = image;
                    row.button.colors = Tint(0, .1f, .18f);
                    row.button.onClick.AddListener(() => { if (!busy && rows[0].step != null) Go(rows[0].step); });
                }
                else image.color = Color.clear;
                int index = i;
                UiTip.Add(row.rt, () => rows[index].step == null ? "" : rows[index].step.Why(GameText.IsEnglish));
                rows[i] = row;
            }
            Layout();
        }

        static ColorBlock Tint(float normal, float hover, float pressed)
        {
            var c = ColorBlock.defaultColorBlock;
            c.normalColor = new Color(1, 1, 1, normal); c.highlightedColor = new Color(1, 1, 1, hover);
            c.pressedColor = new Color(1, 1, 1, pressed); c.selectedColor = c.normalColor; c.disabledColor = c.normalColor;
            c.fadeDuration = .08f;
            return c;
        }

        // ───────────── content ─────────────

        void Apply(List<XgGuideStep> steps)
        {
            bool sameSave = ReferenceEquals(shownFor, lab.Sim);
            var current = shown.Count > 0 ? shown[0] : null;
            if (sameSave && !collapsed && current != null && !steps.Exists(s => s.id == current.id)) { StartCoroutine(Finish(steps)); return; }
            bool fresh = current == null || steps.Count == 0 || steps[0].id != current.id;
            Show(steps);
            if (fresh && sameSave) StartCoroutine(FadeInCurrent());
        }

        void Show(List<XgGuideStep> steps)
        {
            shownFor = lab.Sim;
            english = GameText.IsEnglish;
            shown.Clear(); shown.AddRange(steps);
            for (int i = 0; i < rows.Length; i++)
            {
                var step = i < steps.Count ? steps[i] : null;
                rows[i].step = step;
                rows[i].label.color = i == 0 ? Ink : Soft;
                rows[i].label.text = step == null ? "" : (i == 0 ? "☐ " : "· ") + step.Text(english);
            }
            Layout();
        }

        /// <summary>Sizes the note to its lines, or to the folded strip.</summary>
        void Layout()
        {
            if (note == null) return;
            var now = GameCalendar.Now(runtime != null && runtime.Sim != null ? runtime.Sim.S : null);
            string date = GameText.IsEnglish ? now.ToString("MMM d", System.Globalization.CultureInfo.InvariantCulture) : now.Month + "月" + now.Day + "日";
            var first = shown.Count > 0 ? shown[0] : null;
            title.text = collapsed && first != null ? GameText.T("待办：", "To do: ") + first.Text(GameText.IsEnglish) : GameText.T("待办 · ", "To do · ") + date;
            foldLabel.text = collapsed ? "+" : "–";
            float y = TitleHeight + 6;
            for (int i = 0; i < rows.Length; i++)
            {
                bool on = !collapsed && rows[i].step != null;
                if (rows[i].rt.gameObject.activeSelf != on) rows[i].rt.gameObject.SetActive(on);
                if (!on) continue;
                float h = 22;
                if (i == 0)
                {
                    var size = rows[0].label.GetPreferredValues(rows[0].label.text, Width - 2 - Pad * 2, 0);
                    h = Mathf.Clamp(size.y + 6, 26, 50);
                }
                rows[i].rt.offsetMin = new Vector2(1, -y - h); rows[i].rt.offsetMax = new Vector2(-1, -y);
                y += h + (i == 0 ? 4 : 0);
            }
            note.sizeDelta = new Vector2(Width, collapsed ? TitleHeight + 2 : y + 8);
        }

        IEnumerator Finish(List<XgGuideStep> next)
        {
            busy = true;
            var row = rows[0];
            row.label.text = "√ <s>" + shown[0].Text(english) + "</s>";
            row.label.color = Done;
            for (float t = 0; t < TickHold; t += Time.unscaledDeltaTime) yield return null;
            var start = row.rt.anchoredPosition;
            for (float t = 0; t < SlideTime; t += Time.unscaledDeltaTime)
            {
                float k = t / SlideTime;
                row.rt.anchoredPosition = start + new Vector2(-48 * k * k, 0);
                row.group.alpha = 1 - k;
                yield return null;
            }
            row.rt.anchoredPosition = start;
            Show(pending ?? next);
            pending = null;
            busy = false;
            yield return FadeInCurrent();
        }

        IEnumerator FadeInCurrent()
        {
            var group = rows[0].group;
            for (float t = 0; t < FadeIn; t += Time.unscaledDeltaTime) { group.alpha = t / FadeIn; yield return null; }
            group.alpha = 1;
        }

        // ───────────── going there ─────────────

        void Go(XgGuideStep step)
        {
            switch (step.app)
            {
                case XgGuide.Lab: StartCoroutine(GoLab(step)); break;
                case XgGuide.Home: case XgGuide.Shop: case XgGuide.YY: StartCoroutine(GoApp(step)); break;
                case XgGuide.Tieba: GoTieba(step); break;
                case XgGuide.Bodu:
                    var bodu = FindAnyObjectByType<BoduView>(FindObjectsInactive.Include);
                    if (bodu != null) bodu.Open(step.arg);
                    break;
                case XgGuide.Games:
                    if (!windowsLooked) NoteSeenPlaces();
                    if (gamesWindow != null && !gamesWindow.isOn) gamesWindow.OpenWindow();
                    break;
            }
        }

        IEnumerator GoLab(XgGuideStep step)
        {
            var c = Lab();
            if (c == null || c.Sim == null) yield break;
            if (step.tab == "label" && step.arg.Length > 0) c.Sim.SelectDesk(step.arg);
            // "vision" / "sequence" open the training page on that track.
            string tab = step.tab == "train" && (step.arg == "vision" || step.arg == "sequence") ? step.arg : step.tab;
            if (router == null || !router.Open(XingGuangController.AppId, string.IsNullOrEmpty(tab) ? null : tab)) c.Open();
            for (float t = 0; t < 1.5f; t += Time.unscaledDeltaTime)
            {
                if (c.View != null && c.View.Visible && (string.IsNullOrEmpty(step.tab) || c.View.Tab == step.tab)) break;
                yield return null;
            }
            // Pages build some rows on refresh; give them a frame or two.
            yield return null; yield return null;
            if (c.View != null) XgGuideHighlight.Show(c.View, step.tab, step.target);
        }

        IEnumerator GoApp(XgGuideStep step)
        {
            if (router == null) yield break;
            router.Open(step.app, string.IsNullOrEmpty(step.tab) ? (step.arg.Length > 0 ? step.arg : null) : step.tab);
            var binding = router.Get(step.app);
            if (binding == null || binding.Window == null || string.IsNullOrEmpty(step.target)) yield break;
            for (int i = 0; i < 6; i++) yield return null;
            XgGuideHighlight.Pulse(XgGuideHighlight.FindIn(binding.Window.transform, step.target));
        }

        static void GoTieba(XgGuideStep step)
        {
            var hub = TiebaHub.Instance;
            var view = hub != null ? hub.View : null;
            if (view == null) return;
            string where = string.IsNullOrEmpty(step.tab) ? "home" : step.tab, what = step.arg;
            if (where == "chat" && hub.S != null && !hub.CanMessage(what)) { where = "home"; what = ""; }
            if (where == "thread")
            {
                // The wall's own post may not be dated yet: fall back to the general ones, then the front page.
                var context = hub.Context();
                what = null;
                foreach (var id in new[] { step.arg, "tip_wall", "tip_data" })
                {
                    var t = hub.Library != null ? hub.Library.Get(id) : null;
                    if (t != null && ForumLibrary.Visible(t, context)) { what = id; break; }
                }
                if (what == null) { where = "home"; what = ""; }
            }
            view.Open(where, what);
        }

        // ───────────── dragging by the title bar ─────────────

        sealed class NoteDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
        {
            public GuideNote owner;
            Vector2 grab;

            public void OnBeginDrag(PointerEventData e)
            {
                var parent = (RectTransform)owner.note.parent;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, e.pressEventCamera, out var local))
                    grab = owner.note.anchoredPosition - local;
            }

            public void OnDrag(PointerEventData e)
            {
                var parent = (RectTransform)owner.note.parent;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, e.pressEventCamera, out var local)) return;
                var rect = parent.rect;
                var p = local + grab;
                // Keep the title bar on the desktop (the pivot is the note's top-right corner).
                p.x = Mathf.Clamp(p.x, -rect.width + Width, 0);
                p.y = Mathf.Clamp(p.y, -rect.height + TitleHeight + 54, 0);
                owner.note.anchoredPosition = p;
                dragged = p - new Vector2(-RightMargin, -TopMargin);
            }
        }
    }
}
