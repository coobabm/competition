#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.XingGuang.MiniGames;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MoleReviewUiChecks
{
    const string Key = "SidebarLayoutChecks.MoleReview";
    const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static bool Requested => SessionState.GetBool(Key, false);
    public static readonly List<string> Checks = new List<string>();
    public static int AssertionCount { get; private set; }
    public static void ClearRequest() => SessionState.EraseBool(Key);
    public static void Run()
    {
        // Reuse only the scene/input/save-isolation lifecycle. The shared runner selects our own Frames branch first.
        SessionState.SetBool(Key, true);
        try { BookLibraryUiChecks.Run(); } catch { ClearRequest(); throw; }
    }
    static object Field(object obj, string name)
    {
        for (var t = obj.GetType(); t != null; t = t.BaseType) { var f = t.GetField(name, F); if (f != null) return f.GetValue(obj); }
        throw new Exception("Missing field " + name);
    }
    static void Need(bool ok, string why) { AssertionCount++; if (!ok) throw new Exception("Mole UI: " + why); }
    static IEnumerable<object> Wait(double seconds)
    { double until = EditorApplication.timeSinceStartup + seconds; while (EditorApplication.timeSinceStartup < until) yield return null; }
    static RectTransform Button(RectTransform root, string name)
    {
        foreach (var b in root.GetComponentsInChildren<Button>(true)) if (b.name == name && b.gameObject.activeInHierarchy) return b.transform as RectTransform;
        throw new Exception("Missing active button " + name);
    }
    /// <summary>
    /// A strike only counts on the visible head dome (XgMoleGraphic.ContainsHead), so a <paramref name="strike"/> aims there
    /// once the mole has risen at least 90%. A miss (empty hole, or a mole already hit) aims at <paramref name="aim"/>
    /// (default the cell centre) and must not reach the game. Returns the local point used.
    /// </summary>
    static Vector2 Whack(XgMoleGraphic hole, bool strike, Vector2? aim = null)
    {
        // Moles receive pointer-down, not Button pointer-click.
        Canvas.ForceUpdateCanvases();
        var local = aim ?? hole.rectTransform.rect.center;
        if (strike) { Need(hole.Rise >= .9f && hole.HitTime < 0, "mole has risen at least 90% before the strike: rise=" + hole.Rise); local = HeadPoint(hole); }
        var bridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        Need(bridge != null && bridge.inputSurface != null && bridge.inputSurface.inputEnabled, "actual LCD input available");
        var p = LcdPointer(bridge, hole.rectTransform, local);
        var surface = bridge.inputSurface; surface.OnPointerEnter(p);
        var data = (PointerEventData)Field(surface, "hoverData"); var hit = data?.pointerCurrentRaycast.gameObject;
        Need(hit != null && ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit) == hole.gameObject, "mole pointer is not intercepted: " + hit);
        int dispatched = 0;
        Action original = hole.Down;
        hole.Down = () => { dispatched++; original?.Invoke(); };
        try { surface.OnPointerDown(p); surface.OnPointerUp(p); }
        finally { hole.Down = original; }
        Need(dispatched == (strike ? 1 : 0), (strike ? "LCD down dispatched exactly once to current hole" : "LCD down off a visible head is not a strike") + "; actual=" + dispatched);
        return local;
    }
    /// <summary>The middle of the visible dome on the cell's vertical centre line.</summary>
    static Vector2 HeadPoint(XgMoleGraphic hole)
    {
        var r = hole.rectTransform.rect; float lo = float.NaN, hi = float.NaN;
        for (float y = r.yMin; y <= r.yMax; y += .5f) if (hole.ContainsHead(new Vector2(r.center.x, y))) { if (float.IsNaN(lo)) lo = y; hi = y; }
        Need(!float.IsNaN(lo), "risen mole has a visible head dome");
        return new Vector2(r.center.x, (lo + hi) * .5f);
    }
    /// <summary>The shared LCD mapping (FilesWindowUiChecks.Pointer) for a point inside <paramref name="target"/> rather than its centre.</summary>
    static PointerEventData LcdPointer(AeroLcdSourceBridge bridge, RectTransform target, Vector2 local)
    {
        var surface = bridge.inputSurface; var tex = bridge.sourceCamera.targetTexture;
        var src = bridge.sourceCamera.WorldToScreenPoint(target.TransformPoint(local));
        Need(src.z > 0 && src.x >= 0 && src.x < tex.width && src.y >= 0 && src.y < tex.height, "target point outside source camera viewport: " + src);
        float u = src.x / tex.width, v = src.y / tex.height; if (surface.flipY) v = 1 - v;
        var display = surface.displayRect; var r = display.rect;
        var camera = (Camera)typeof(AeroLcdInputSurface).GetMethod("DisplayEventCamera", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(surface, null);
        var p = RectTransformUtility.WorldToScreenPoint(camera, display.TransformPoint(new Vector3(r.xMin + u * r.width, r.yMin + v * r.height, 0)));
        Need(surface.TryMap(p, out var mapped) && Vector2.Distance(mapped, new Vector2(src.x, src.y)) < 1, "LCD coordinate roundtrip");
        return new PointerEventData(EventSystem.current) { position = p, button = PointerEventData.InputButton.Left };
    }
    static string State(XingGuangController ctl, XgDeskGamePage desk, XgMiniGame game, int slot)
    {
        var context = (XgMiniContext)Field(game, "C");
        return "slot=" + slot + " expiry=" + ((float[])Field(game, "expires"))[slot]
            + " sameGame=" + ReferenceEquals(game, Field(desk, "game"))
            + " visible=" + desk.Visible + " focused=" + desk.Focused + " context=" + context.IsActive
            + " sameOwner=" + ReferenceEquals(context.Sim, ctl.Sim)
            + " playing=" + Field(game, "playing") + " reviewing=" + Field(game, "reviewing")
            + " generation=" + Field(game, "buildGeneration") + " time=" + Field(game, "time")
            + " uiEnglish=" + XgUi.En + " simEnglish=" + ctl.Sim.English
            + " feedback=" + ((TMP_Text)Field(game, "feedback")).text;
    }
    static IEnumerator HeldReview(XgMiniGame game, RectTransform surface, XgSim sim, bool returnToOriginal)
    {
        int originalSlot = (int)Field(game, "reviewSlot");
        var turns = (XgMoleTurn[])Field(game, "turns");
        long originalToken = turns[originalSlot].Token;
        var question = ((XgMoleQuestion[])Field(game, "questions"))[originalSlot];
        var target = Button(surface, question.Truth ? "ReviewTrue" : "ReviewFalse");
        var input = target.GetComponent<XgMoleReviewInput>();
        Need(input != null && !sim.MoleLessonAnswered(originalToken), "held-pointer fixture starts on unanswered displayed question");
        var bridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        var lcd = bridge.inputSurface;
        var mapper = typeof(FilesWindowUiChecks).GetMethod("Pointer", BindingFlags.Static | BindingFlags.NonPublic);
        var modules = EventSystem.current.GetComponents<BaseInputModule>();
        var enabled = modules.Select(m => m.enabled).ToArray();
        long cards = sim.S.board.cards;
        int notes = sim.S.teachingNotebook.Count, records = sim.S.askedLog.Count;
        PointerEventData pointer = null;
        try
        {
            // Isolate split-frame synthetic LCD input from the physical mouse modules.
            for (int i = 0; i < modules.Length; i++) modules[i].enabled = false;
            Canvas.ForceUpdateCanvases();
            pointer = (PointerEventData)mapper.Invoke(null, new object[] { target });
            lcd.OnPointerEnter(pointer);
            var hover = (PointerEventData)Field(lcd, "hoverData");
            Need(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hover.pointerCurrentRaycast.gameObject) == target.gameObject,
                "held judgment starts at the real LCD button hit");
            lcd.OnPointerDown(pointer);
            Need(Field(input, "pending") != null, "real down captures the displayed question before navigation");
            yield return null;
            // Explicit navigation fixture while one desktop pointer is held; this is not another claimed mouse click.
            var step = game.GetType().GetMethod("StepReview", F);
            step.Invoke(game, new object[] { 1 });
            int otherSlot = (int)Field(game, "reviewSlot");
            long otherToken = turns[otherSlot].Token;
            Need(otherSlot != originalSlot && otherToken != originalToken && !sim.MoleLessonAnswered(otherToken),
                "held navigation actually changes to a different unanswered question");
            if (returnToOriginal) step.Invoke(game, new object[] { -1 });
            yield return null;
            Canvas.ForceUpdateCanvases(); lcd.OnPointerMove(pointer);
            var press = ((Array)Field(lcd, "buttons")).GetValue(0);
            var data = (PointerEventData)Field(press, "data");
            Need((bool)Field(press, "pressed") && data.eligibleForClick && !data.dragging && Field(input, "pending") != null,
                "LCD still holds the original press, so the regression cannot pass by cancelling its input");
            hover = (PointerEventData)Field(lcd, "hoverData");
            Need(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hover.pointerCurrentRaycast.gameObject) == target.gameObject,
                "release still hits the same judgment control");
            lcd.OnPointerUp(pointer); pointer = null;
            Need(Field(input, "pending") == null && sim.S.board.cards == cards && sim.S.teachingNotebook.Count == notes
                && sim.S.askedLog.Count == records && !sim.MoleLessonAnswered(originalToken) && !sim.MoleLessonAnswered(otherToken),
                returnToOriginal ? "away-and-back held judgment is stale even when token matches again" : "cross-question held judgment consumes neither old nor new lesson and creates no record");
            if (!returnToOriginal) step.Invoke(game, new object[] { -1 });
            Need((int)Field(game, "reviewSlot") == originalSlot, "held regression restores the original displayed question");
        }
        finally
        {
            if (pointer != null) lcd.CancelInteraction();
            for (int i = 0; i < modules.Length; i++) modules[i].enabled = enabled[i];
        }
    }
    static string BoardDigest(XgSim sim)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(sim.S.board)))).Replace("-", "");
    }
    static string Prediction(XgPredictionTrace trace) => trace == null ? "none"
        : "yes=" + trace.AnswerYes + ",guessed=" + trace.Guessed + ",score=" + trace.Score.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    static IEnumerator PrepareAutomatic(XgMiniGame game, RectTransform surface, XgSim sim, IXgHost host,
        Action<RectTransform> click, bool firstLabel)
    {
        // Explicit supervised QA prerequisite: four positives and four negatives per language, including
        // the first calculus judgment above. Fixture truth chooses only which visible control to click.
        // The sample count is fixed before observing predictions; no train-until-the-model-says-yes loop.
        int positive = firstLabel ? 1 : 0, negative = firstLabel ? 0 : 1;
        long before = sim.S.board.cards;
        string modelBefore = BoardDigest(sim);
        while (positive < 4 || negative < 4)
        {
            bool label = positive < negative && positive < 4 || negative >= 4;
            int selected = -1;
            for (int step = 0; step < 200 && selected < 0; step++)
            {
                if (!(bool)Field(game, "playing") || (float)Field(game, "time") < 2)
                {
                    click(Button(surface, "Restart"));
                    yield return null; // New buttons need rendered Canvas depth before actual LCD input.
                    if ((bool)Field(game, "autoEnabled")) click(Button(surface, "MoleAuto"));
                }
                Need(!(bool)Field(game, "autoEnabled") && !(bool)Field(game, "reviewing"), "supervised prerequisite keeps auto off outside review");
                var questions = (XgMoleQuestion[])Field(game, "questions");
                var turns = (XgMoleTurn[])Field(game, "turns");
                for (int i = 0; i < questions.Length; i++)
                    if (questions[i] != null && questions[i].Truth == label && !sim.MoleLessonAnswered(turns[i].Token)) { selected = i; break; }
                if (selected < 0) game.Tick(.15f);
            }
            Need(selected >= 0, "bounded live generator supplies the declared " + (label ? "positive" : "negative") + " supervision case");
            click(Button(surface, "MoleReview"));
            yield return null;
            for (int i = 0; i < 9 && (int)Field(game, "reviewSlot") != selected; i++) click(Button(surface, "ReviewNext"));
            var question = ((XgMoleQuestion[])Field(game, "questions"))[selected];
            long token = ((XgMoleTurn[])Field(game, "turns"))[selected].Token;
            Need((int)Field(game, "reviewSlot") == selected && ((TMP_Text)Field(game, "reviewQuestion")).text == question.Text,
                "declared supervision labels the exact actually displayed statement");
            long cards = sim.S.board.cards;
            int notes = sim.S.teachingNotebook.Count, records = sim.S.askedLog.Count;
            double money = host.Money, samples = sim.Labels("arith");
            string predictionBefore = Prediction(sim.PredictMole(question.Card()));
            click(Button(surface, label ? "ReviewTrue" : "ReviewFalse"));
            Need(sim.MoleLessonAnswered(token) && sim.S.board.cards == cards + 1
                && sim.S.teachingNotebook.Count == notes + 1 && sim.S.askedLog.Count == records + 1,
                "declared LCD supervision produces one exact board/note/record receipt");
            var record = sim.S.askedLog.Last();
            var note = sim.LearningTeachingNote(record.teachingNoteId);
            Need(XgSim.HasPlayerTeachingAnswer(record, note) && note.playerAnswer == label && record.question == question.Text,
                "declared extra supervision remains linked player evidence with the entered label");
            Need(host.Money == money && sim.Labels("arith") == samples, "declared review supervision neither pays nor grants samples");
            Debug.Log("Mole supervised prerequisite: english=" + sim.English + " token=" + token + " label=" + label
                + " question=" + question.Text + " before={" + predictionBefore + "} after={" + Prediction(sim.PredictMole(question.Card())) + "}");
            if (label) positive++; else negative++;
            click(Button(surface, "ReviewBack"));
        }
        Need(positive == 4 && negative == 4 && sim.S.board.cards == before + 7, "fixed balanced prerequisite adds exactly seven real supervised labels");
        Debug.Log("Mole supervised prerequisite complete: english=" + sim.English + " positives=" + positive + " negatives=" + negative
            + " cardsBefore=" + before + " cardsAfter=" + sim.S.board.cards + " modelBefore=" + modelBefore + " modelAfter=" + BoardDigest(sim));
        // Spawn new questions after supervision; existing live traces were captured before their labels.
        // Restart is the actual player control, not a replacement prediction or a model reset.
        click(Button(surface, "Restart"));
        yield return null;
        if ((bool)Field(game, "autoEnabled")) click(Button(surface, "MoleAuto"));
        Need(!(bool)Field(game, "autoEnabled") && (bool)Field(game, "playing"), "fresh round is ready for the separately measured automatic phase");
    }
    static void Fits(TMP_Text text, string description)
    {
        text.ForceMeshUpdate();
        Need(!text.isTextOverflowing && text.preferredHeight <= text.rectTransform.rect.height + 1, description);
    }
    public static IEnumerator Frames(XingGuangController ctl, Action<bool> language, Action<RectTransform> click, Action<string> capture)
    {
        Checks.Clear(); AssertionCount = 0;
        bool originalEnabled = ctl.enabled, oldReduced = ctl.Sim.S.reduceFx, oldEnglish = ctl.Sim.English;
        var sim = ctl.Sim;
        XgDeskGamePage desk = null;
        ctl.enabled = false;
        try
        {
            foreach (bool english in new[] { false, true })
            {
                language(english); sim.English = english;
                Need(sim.SelectTeachingPack("arith_mole"), "arithmetic desk selectable in explicit unlocked fixture");
                ctl.Open(); ctl.View.Open("teach"); ctl.View.Refresh(true); ctl.Window.FocusToWindow();
                foreach (var f in Wait(.7)) yield return f;
                var brain = ctl.GetComponent<XgBrainWindow>();
                if (brain != null)
                {
                    var brainWindow = Field(brain, "window") as RectTransform;
                    if (brainWindow != null && brainWindow.gameObject.activeInHierarchy)
                    {
                        click((RectTransform)brainWindow.Find("Title/Close"));
                        foreach (var f in Wait(.15)) yield return f;
                    }
                }
                if (sim.S.reduceFx != english) click(((XgBtn)Field(ctl.View, "fxToggle")).rt);
                Need(sim.S.reduceFx == english && ctl.View.Juice.Reduced == english, "actual effects toggle synchronizes save and renderer");
                yield return null;
                desk = (XgDeskGamePage)Field(ctl.View.Teach, "deskGame");
                Need(desk != null && desk.Visible && desk.Open("arith_mole"), "actual native game starts");
                desk.Refresh();
                var game = (XgMiniGame)Field(desk, "game"); var surface = (RectTransform)Field(desk, "surface");
                foreach (var f in Wait(.2)) yield return f;
                if ((bool)Field(game, "autoEnabled")) click(Button(surface, "MoleAuto"));
                Need(!(bool)Field(game, "autoEnabled"), "actual auto toggle pauses model actions for reading fixture");
                // Calculus claims come only from the purchased 极限 / 求导 / 积分 topic; the default topic is basic arithmetic.
                click(Button(surface, "MoleTopic"));
                foreach (var f in Wait(.5)) yield return f; // the menu's options settle in before they take a click
                click(Button(surface, "MoleTopic3"));
                yield return null;
                Need(sim.MoleTopic == XgMoleTopic.Calculus && (bool)Field(game, "playing"), "actual topic menu restarts the round on the calculus pack");
                if ((bool)Field(game, "autoEnabled")) click(Button(surface, "MoleAuto"));
                Need(!(bool)Field(game, "autoEnabled"), "auto stays paused on the calculus round");
                var holes = (XgMoleGraphic[])Field(game, "holes");
                long initialCards = sim.S.board.cards; double initialSamples = sim.Labels("arith"), initialMoney = ctl.Host.Money;
                var initialExpiry = (float[])Field(game, "expires");
                int empty = Array.FindIndex(initialExpiry, expiry => expiry <= 0);
                Need(empty >= 0, "live round has an actual empty hole for neutral feedback check");
                Debug.Log("Mole empty-hole before: " + State(ctl, desk, game, empty));
                Need(ReferenceEquals(game, Field(desk, "game")) && ((XgMiniContext)Field(game, "C")).IsActive
                    && (bool)Field(game, "playing") && !(bool)Field(game, "reviewing"), "empty-hole fixture owns an active live round: " + State(ctl, desk, game, empty));
                string feedbackBefore = ((TMP_Text)Field(game, "feedback")).text;
                // An empty hole has no head to hit: the pointer is ignored, without feedback.
                Whack(holes[empty], false);
                Debug.Log("Mole empty-hole after: " + State(ctl, desk, game, empty));
                Need(((TMP_Text)Field(game, "feedback")).text == feedbackBefore, "empty-hole pointer is not a strike: " + State(ctl, desk, game, empty));
                Need(sim.S.board.cards == initialCards && sim.Labels("arith") == initialSamples && ctl.Host.Money == initialMoney, "empty clicks neither train nor settle");
                int picked = -1;
                for (int step = 0; step < 100 && picked < 0; step++)
                {
                    game.Tick(.3f);
                    var questions = (XgMoleQuestion[])Field(game, "questions"); var expiry = (float[])Field(game, "expires");
                    for (int i = 0; i < 9; i++) if (questions.Count(q => q != null) >= 2 && questions[i] != null && questions[i].Kind >= XgMoleMathKind.Limit && expiry[i] > .4f) { picked = i; break; }
                }
                Need(picked >= 0, "actual generator produced a live calculus claim");
                game.Tick(.2f);
                for (int rising = 0; rising < 20 && holes[picked].Rise < .9f; rising++) game.Tick(.05f);
                var signs = (TMP_Text[])Field(game, "signs"); signs[picked].ForceMeshUpdate();
                Fits(signs[picked], "calculus equation fits widened sign without ellipsis");
                Canvas.ForceUpdateCanvases(); yield return null;
                capture("mole-" + (english ? "en-reduced" : "zh") + "-live.png");
                int feedbacks = 0; Action<XgMoleFeedback> manual = x => { if (!x.Automatic && x.Whack) feedbacks++; };
                sim.ArithmeticMoleFeedback += manual;
                try
                {
                    var head = Whack(holes[picked], true);
                    Need(feedbacks == 1 && ((TMP_Text[])Field(game, "bangs"))[picked].enabled, "real pointer whack emits feedback and visible result immediately");
                    long cards = sim.S.board.cards; double money = ctl.Host.Money, labels = sim.Labels("arith");
                    // The struck mole no longer takes strikes on the same spot.
                    Whack(holes[picked], false, head);
                    Need(feedbacks == 1 && sim.S.board.cards == cards && ctl.Host.Money == money && sim.Labels("arith") == labels, "repeated pointer cannot settle or train twice");
                    Need(cards == initialCards, "whack did not silently teach its generated label");
                    var manualRecord=sim.S.askedLog.Last();
                    Need(XgSim.HasPlayerTeachingAnswer(manualRecord,sim.LearningTeachingNote(manualRecord.teachingNoteId)), "actual human whack is explicitly player-sourced evidence");
                }
                finally { sim.ArithmeticMoleFeedback -= manual; }
                foreach (var f in Wait(.4)) yield return f;
                click(Button(surface, "MoleReview"));
                Need((bool)Field(game, "reviewing"), "actual LCD review button opens panel");
                yield return null; // Newly enabled graphics receive their Canvas depth on the next rendered frame.
                float clock = (float)Field(game, "time"); game.Tick(2);
                Need((float)Field(game, "time") == clock, "review pauses only minigame countdown");
                for (int i = 0; i < 9 && (int)Field(game, "reviewSlot") != picked; i++) click(Button(surface, "ReviewNext"));
                var qs = (XgMoleQuestion[])Field(game, "questions"); int chosen = (int)Field(game, "reviewSlot");
                Need(chosen == picked && qs[chosen].Kind >= XgMoleMathKind.Limit, "actual review navigation selects the observed calculus question");
                var shown = (TMP_Text)Field(game, "reviewQuestion"); shown.ForceMeshUpdate();
                Need(shown.text == qs[chosen].Text, "review shows the exact observed claim"); Fits(shown, "full review statement is legible");
                var conditions = (TMP_Text)Field(game, "reviewConditions");
                Need(conditions.text == (english ? qs[chosen].ConditionsEn : qs[chosen].ConditionsZh), "complete mathematical conditions appear in the selected language");
                Fits(conditions, "complete conditions fit without truncation");
                var rawTurns = (XgMoleTurn[])Field(game, "turns");
                long rawCards = sim.S.board.cards;
                ((XgBtn)Field(game, "reviewTrue")).button.onClick.Invoke();
                ((XgBtn)Field(game, "reviewFalse")).button.onClick.Invoke();
                Need(sim.S.board.cards == rawCards && !sim.MoleLessonAnswered(rawTurns[chosen].Token), "unarmed raw callbacks cannot fabricate a human judgment");
                foreach (bool backAgain in new[] { false, true })
                {
                    var held = HeldReview(game, surface, sim, backAgain);
                    try { while (held.MoveNext()) yield return held.Current; }
                    finally { (held as IDisposable)?.Dispose(); }
                }
                long before = sim.S.board.cards; double cash = ctl.Host.Money, samples = sim.Labels("arith");
                click(Button(surface, qs[chosen].Truth ? "ReviewTrue" : "ReviewFalse"));
                Need(sim.S.board.cards == before + 1, "explicit true/false label reaches actual board once");
                var teachingRecord=sim.S.askedLog.Last();
                Need(XgSim.HasPlayerTeachingAnswer(teachingRecord,sim.LearningTeachingNote(teachingRecord.teachingNoteId)), "actual review judgment creates linked player-source record and note");
                Need(ctl.Host.Money == cash && sim.Labels("arith") == samples, "review has no duplicate pay or sample reward");
                var yes = ((XgBtn)Field(game, "reviewTrue")).button; var no = ((XgBtn)Field(game, "reviewFalse")).button;
                Need(!yes.interactable && !no.interactable, "both choices disable after accepted answer");
                Canvas.ForceUpdateCanvases(); yield return null;
                capture("mole-" + (english ? "en-reduced" : "zh") + "-taught.png");
                yes.onClick.Invoke(); no.onClick.Invoke();
                Need(sim.S.board.cards == before + 1, "adversarial duplicate callbacks cannot teach twice");
                click(Button(surface, "ReviewBack")); Need(!(bool)Field(game, "reviewing"), "actual back button resumes round");
                var prepare = PrepareAutomatic(game, surface, sim, ctl.Host, click, qs[chosen].Truth);
                try { while (prepare.MoveNext()) yield return prepare.Current; }
                finally { (prepare as IDisposable)?.Dispose(); }
                int automatic = 0,automaticWhacks=0;
                var decisions = new List<string>();
                string modelBeforeAuto = BoardDigest(sim);
                int notesBeforeAuto=sim.S.teachingNotebook.Count;long cardsBeforeAuto=sim.S.board.cards;
                long actionsBeforeAuto=sim.S.askedLog.Count(x=>x.sourceKind==XgSim.AiActionSource);
                Action<XgMoleFeedback> readout = x =>
                {
                    if (!x.Automatic) return;
                    automatic++; if (x.Whack) automaticWhacks++;
                    decisions.Add("token=" + x.QuestionToken + ",whack=" + x.Whack + "," + Prediction(x.Prediction) + ",question=" + x.Question.Text);
                };
                sim.ArithmeticMoleFeedback += readout;
                try
                {
                    click(Button(surface, "MoleAuto")); Need((bool)Field(game, "autoEnabled"), "actual auto button enables owned technology");
                    for (int i = 0; i < 100 && automaticWhacks == 0; i++) game.Tick(.15f);
                    string modelAfterAuto = BoardDigest(sim);
                    Debug.Log("Mole automatic phase: english=" + english + " decisions=" + automatic + " whacks=" + automaticWhacks
                        + " cardsBefore=" + cardsBeforeAuto + " cardsAfter=" + sim.S.board.cards
                        + " modelBefore=" + modelBeforeAuto + " modelAfter=" + modelAfterAuto + " observed=[" + string.Join("; ", decisions) + "]");
                    Need(automatic > 0 && automaticWhacks > 0, "real model emits an automatic whack; leave alone is not fabricated action evidence; decisions=" + automatic + ", whacks=" + automaticWhacks);
                    Need(modelBeforeAuto == modelAfterAuto, "automatic readout leaves the full serialized board unchanged");
                    Need(sim.S.teachingNotebook.Count==notesBeforeAuto&&sim.S.board.cards==cardsBeforeAuto,"automatic whacks never create player teaching notes or self-train");
                    Need(sim.S.askedLog.Count(x=>x.sourceKind==XgSim.AiActionSource)==actionsBeforeAuto+automaticWhacks,"exactly one separate AI action record per accepted automatic whack");
                    var actionRecord=sim.S.askedLog.Last(x=>x.sourceKind==XgSim.AiActionSource);
                    Need(string.IsNullOrEmpty(actionRecord.playerAnswer)&&actionRecord.answerSource==XgSim.AiActionSource&&actionRecord.teachingNoteId==0,"automatic action owns no player label or teaching note");
                    Need(sim.LearningAnswerSummary(actionRecord).Contains(english?"not a player judgment":"非玩家判断"),"bilingual action readout identifies AI origin");
                }
                finally { sim.ArithmeticMoleFeedback -= readout; }
                click(Button(surface, "MoleReview"));
                var oldAuto = Button(surface, "MoleAuto").GetComponent<Button>();
                var oldBack = Button(surface, "ReviewBack").GetComponent<Button>();
                var oldRestart = Button(surface, "Restart").GetComponent<Button>();
                long beforeRestart = sim.S.board.cards;
                var oldJudgment = ((XgBtn)Field(game, "reviewTrue")).rt.GetComponent<XgMoleReviewInput>().Capture();
                click(Button(surface, "Restart")); game.Tick(.3f); game.Tick(.2f);
                yield return null;
                click(Button(surface, "MoleReview"));
                yield return null;
                bool currentAuto = (bool)Field(game, "autoEnabled");
                oldAuto.onClick.Invoke(); oldBack.onClick.Invoke(); oldRestart.onClick.Invoke(); yes.onClick.Invoke(); no.onClick.Invoke(); oldJudgment?.Invoke();
                Need((bool)Field(game, "reviewing") && (bool)Field(game, "autoEnabled") == currentAuto && sim.S.board.cards == beforeRestart,
                    "callbacks from rebuilt buttons cannot mutate the new round");
                Need(((TMP_Text)Field(game, "questionDetail")).gameObject.activeInHierarchy, "restart binds the new rule text before deferred destruction");
                yes = ((XgBtn)Field(game, "reviewTrue")).button; no = ((XgBtn)Field(game, "reviewFalse")).button;
                // A fresh, unconsumed lesson makes these guards independent of duplicate-answer protection.
                var pendingTurns = (XgMoleTurn[])Field(game, "turns");
                Need(!sim.MoleLessonAnswered(pendingTurns[(int)Field(game, "reviewSlot")].Token), "stale-callback fixture owns an unanswered lesson");
                long ownedCards = sim.S.board.cards;
                var yesInput = yes.GetComponent<XgMoleReviewInput>(); var noInput = no.GetComponent<XgMoleReviewInput>();
                // Explicit adversarial queued captures, taken while the lesson really is available.
                var ownerYes = yesInput.Capture(); var ownerNo = noInput.Capture();
                var hiddenYes = yesInput.Capture(); var hiddenNo = noInput.Capture();
                var disposedYes = yesInput.Capture(); var disposedNo = noInput.Capture();
                Need(ownerYes != null && ownerNo != null && hiddenYes != null && hiddenNo != null && disposedYes != null && disposedNo != null,
                    "stale callback fixtures capture actual available judgments");
                var other = new XgSim(); long otherCards = other.S.board.cards;
                var simSetter = typeof(XingGuangController).GetProperty("Sim", F).GetSetMethod(true);
                try { simSetter.Invoke(ctl, new object[] { other }); ownerYes(); ownerNo(); }
                finally { simSetter.Invoke(ctl, new object[] { sim }); }
                Need(sim.S.board.cards == ownedCards && other.S.board.cards == otherCards, "explicit owner-swap fixture rejects stale review callbacks for both saves");
                surface.gameObject.SetActive(false); hiddenYes(); hiddenNo();
                Need(sim.S.board.cards == ownedCards, "hidden surface rejects queued labels");
                desk.Stop(); disposedYes(); disposedNo();
                Need(sim.S.board.cards == ownedCards, "disposed round rejects queued labels");
                Checks.Add((english ? "English reduced" : "Chinese animated") + ": actual LCD calculus navigation/conditions, immediate pointer feedback, explicit training after rejected cross-question/away-and-back held LCD presses, fixed four-positive/four-negative LCD supervision prerequisite, no duplicate payout, real model auto decision with unchanged board digest; adversarial callback and owner-swap guards.");
            }
        }
        finally { desk?.Stop(); sim.S.reduceFx = oldReduced; ctl.View.Juice.Reduced = oldReduced; sim.English = oldEnglish; ctl.enabled = originalEnabled; }
    }
}
#endif
