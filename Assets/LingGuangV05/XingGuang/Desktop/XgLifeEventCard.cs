using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Story;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The life events (节奏重做 §2.5, rules in XgSim.LifeEvents.cs) on the desktop. Lives on the controller so the pool
    /// keeps its gate and the house's numbers while the 灵光 window is closed. When an event opens it raises a tray popup
    /// (the same as 摆渡众包's notices; a click opens 灵光), and while the window is up it shows the event as a card over it,
    /// in the window's dark style: who, the title, the situation, and two choices with what each one does. The card is not
    /// modal: the rest of the window still takes clicks while it waits. It tells the
    /// sim when the player is busy (a story line or cutscene, the emergence cutscene, a love question, an insight card,
    /// the setup, an exam or diagnosis on the training page), so no event is drawn then and the card waits.
    /// Presentation only.
    /// </summary>
    public sealed class XgLifeEventCard : MonoBehaviour
    {
        const float RefreshEvery = .25f, RemindEvery = 120, PopupSeconds = 8, BusyCheckEvery = .5f;

        /// <summary>The house as the life events read it: bills, this month's training kWh, the desktop clock.</summary>
        sealed class HouseView : IXgHousehold
        {
            readonly ChapterOneRuntime runtime;
            public HouseView(ChapterOneRuntime runtime) { this.runtime = runtime; }
            ChapterOneSim Sim => runtime != null && !runtime.TestMode ? runtime.Sim : null;
            public bool BillsActive => Sim != null && Sim.EconomyActive;
            public double MonthKwh => Sim != null ? Sim.MonthKwh : 0;
            public double HighKwh => Sim != null ? Sim.Config.tierTwoKwh : 400;
            public double SecondsOfDay => Sim != null ? GameCalendar.Now(Sim.S).TimeOfDay.TotalSeconds : -1;
        }

        XingGuangController controller;
        XgSim bound;
        HouseView house;
        XgUi ui;
        RectTransform overlay, card;
        TMP_Text who, title, body;
        readonly XgBtn[] buttons = new XgBtn[2];
        readonly TMP_Text[] details = new TMP_Text[2];
        string signature = "";
        float refreshTimer, remindTimer, busyTimer;
        bool busy;
        StoryDirector director;
        StoryDesktopPresenter presenter;

        public void Bind(XingGuangController owner) { controller = owner; }

        void Update()
        {
            if (controller == null || controller.Sim == null) return;
            if (!ReferenceEquals(bound, controller.Sim)) Rebind(controller.Sim);
            float dt = Time.unscaledDeltaTime;
            busyTimer -= dt;
            if (busyTimer <= 0) { busyTimer = BusyCheckEvery; busy = ComputeBusy(); }
            var view = controller.View;
            bool show = bound.LifeEventOpen && view != null && view.Visible && !busy;
            if (!bound.LifeEventOpen) { remindTimer = 0; signature = ""; }
            else if (!show)
            {
                // Still waiting while the window is away: a reminder now and then.
                remindTimer += dt;
                if (remindTimer >= RemindEvery && (view == null || !view.Visible)) { remindTimer = 0; Popup(bound.LifeEventView(controller.Host)); }
            }
            if (!show) { if (overlay != null && overlay.gameObject.activeSelf) overlay.gameObject.SetActive(false); return; }
            if (overlay == null && !Build(view)) return;
            bool opening = !overlay.gameObject.activeSelf;
            if (opening)
            {
                overlay.gameObject.SetActive(true);
                overlay.SetAsLastSibling();
                view.Juice.BringToFront();
                view.Juice.Play(XgJuice.Sfx.Id.Whoosh);
                view.Juice.Knock(card, .08f, Vector2.zero, .3f);
                refreshTimer = 0;
            }
            refreshTimer -= dt;
            if (refreshTimer <= 0) { refreshTimer = RefreshEvery; Fill(); }
        }

        void Rebind(XgSim sim)
        {
            if (bound != null) { bound.LifeEventOpened -= OnOpened; bound.LifeEventHold = null; if (house != null && ReferenceEquals(bound.Household, house)) bound.Household = null; }
            bound = sim;
            house = new HouseView(controller.runtime);
            bound.Household = house;
            bound.LifeEventHold = () => busy;
            bound.LifeEventOpened += OnOpened;
            signature = "";
        }

        void OnDestroy()
        {
            if (bound == null) return;
            bound.LifeEventOpened -= OnOpened;
            bound.LifeEventHold = null;
            if (ReferenceEquals(bound.Household, house)) bound.Household = null;
        }

        void OnOpened(XgLifeEvent e)
        {
            remindTimer = 0;
            var view = controller.View;
            if (view == null || !view.Visible) Popup(e);
        }

        void Popup(XgLifeEvent e)
        {
            if (e == null) return;
            PrologueDirector.Desk?.StoryPopup(e.who, e.title + T("（打开灵光处理）", " (open 灵光 to answer)"), PopupSeconds, () => { if (controller != null) controller.Open(); });
        }

        /// <summary>Something else has the player now: a story line or cutscene, a full-window moment, an exam or diagnosis.</summary>
        bool ComputeBusy()
        {
            if (controller.runtime == null || controller.runtime.Sim == null || controller.runtime.Sim.InPrologue) return true;
            if (director == null) director = controller.GetComponent<StoryDirector>();
            if (director != null && director.Runner != null && director.Runner.IsWaitingForOutput) return true;
            if (presenter == null) presenter = FindAnyObjectByType<StoryDesktopPresenter>();
            if (presenter != null && presenter.CutscenePlaying) return true;
            if (XgEmergenceCutscene.Busy || LoveQuestionCutscene.Playing) return true;
            var view = controller.View;
            if (view == null) return false;
            if (view.SetupShowing || view.Holo != null && view.Holo.Showing) return true;
            var train = view.Train;
            return train != null && (train.ExamOpen || train.DiagnosisOpen || train.AssessmentBlocksInput);
        }

        // ───────────── the card ─────────────

        bool Build(XingGuangView view)
        {
            var root = view.transform.Find("XingGuang") as RectTransform;
            if (root == null || controller.Font == null) return false;
            ui = new XgUi(controller.Font, controller.Window, true);
            overlay = Rect("LifeEvent", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // Not modal: the card waits for an answer while the rest of the window (科技 purchases, training) still takes
            // clicks. The sim never blocks anything on an open event; a full-window backdrop here used to swallow every
            // click on the 科技 tree while 老周's question sat unanswered (2026-10-16).
            Panel(overlay, Color.clear).raycastTarget = false;
            card = ui.Card(overlay, "Card", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-280, -165), new Vector2(280, 165));
            who = ui.Text(Strip("Who", card, 14, 20, 22, 22), "", 13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            who.textWrappingMode = TextWrappingModes.NoWrap;
            title = ui.Text(Strip("Title", card, 36, 32, 22, 22), "", 22, XgDark.Gold, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold; title.textWrappingMode = TextWrappingModes.NoWrap;
            body = ui.Text(Rect("Body", card, Vector2.zero, Vector2.one, new Vector2(22, 96), new Vector2(-22, -76)), "", 16, XgDark.Ink, TextAlignmentOptions.TopLeft);
            body.enableAutoSizing = true; body.fontSizeMin = 12; body.fontSizeMax = 16;
            for (int i = 0; i < buttons.Length; i++)
            {
                int at = i;
                var b = ui.Button(card, "", () => Answer(at), 16);
                b.rt.anchorMin = b.rt.anchorMax = new Vector2(i == 0 ? .25f : .75f, 0);
                b.rt.offsetMin = new Vector2(-124, 16); b.rt.offsetMax = new Vector2(124, 80);
                // The choice on top, what it does underneath.
                b.label.rectTransform.anchorMin = new Vector2(0, .5f); b.label.rectTransform.offsetMin = new Vector2(8, 0); b.label.rectTransform.offsetMax = new Vector2(-8, -4);
                b.label.enableAutoSizing = true; b.label.fontSizeMin = 11; b.label.fontSizeMax = 16;
                var d = ui.Text(Rect("Detail", b.rt, Vector2.zero, new Vector2(1, .5f), new Vector2(8, 4), new Vector2(-8, 0)), "", 12, XgDark.Muted, TextAlignmentOptions.Center);
                d.enableAutoSizing = true; d.fontSizeMin = 9; d.fontSizeMax = 12;
                buttons[i] = b; details[i] = d;
            }
            overlay.gameObject.SetActive(false);
            return true;
        }

        void Fill()
        {
            var e = bound.LifeEventView(controller.Host);
            if (e == null) return;
            string sig = e.id + (bound.English ? "e" : "z");
            foreach (var c in e.choices) sig += c.enabled ? "1" : "0";
            if (sig == signature) return;
            signature = sig;
            who.text = e.who;
            title.text = e.title;
            body.text = e.body;
            for (int i = 0; i < buttons.Length; i++)
            {
                bool has = i < e.choices.Count;
                buttons[i].Show(has);
                if (!has) continue;
                var c = e.choices[i];
                buttons[i].Set(c.label, c.enabled, i == 0 ? XgDark.AccentSoft : (Color?)null, i == 0 ? XgDark.Good : (Color?)null);
                details[i].text = c.detail;
                details[i].color = c.enabled ? XgDark.Muted : XgDark.Dim;
            }
        }

        void Answer(int index)
        {
            var view = controller.View;
            if (bound == null || !bound.AnswerLifeEvent(index, controller.Host))
            {
                if (view != null) view.Juice.Play(XgJuice.Sfx.Id.Thud);
                return;
            }
            if (view != null) { view.Juice.Play(XgJuice.Sfx.Id.Click); view.Refresh(true); }
            signature = "";
            overlay.gameObject.SetActive(false);
        }
    }
}
