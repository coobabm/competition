using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HongmengOS.Aero2010;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static partial class DesignWallpaperSmoke
{
    public static void RunTrainingScene() { Start("training-scene"); }
    static void StartTrainingSceneChecks(XingGuangController controller, ChapterOneRuntime runtime)
    {
        due = double.PositiveInfinity;
        controller.StartCoroutine(TrainingSceneFrames(controller, runtime));
    }
    static IEnumerator TrainingSceneFrames(XingGuangController controller, ChapterOneRuntime runtime)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(TrainingSceneSteps(controller, runtime));
        Exception failure = null; float deadline = Time.realtimeSinceStartup + 120;
        while (stack.Count > 0)
        {
            object current = null; bool more = false;
            try
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Training Main scene exceeded 120 seconds");
                more = stack.Peek().MoveNext(); if (more) current = stack.Peek().Current;
            }
            catch (Exception error) { failure = error; }
            if (failure != null) break;
            if (!more) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
            if (current is IEnumerator nested) { stack.Push(nested); continue; }
            yield return current;
        }
        while (stack.Count > 0)
        {
            try { (stack.Pop() as IDisposable)?.Dispose(); }
            catch (Exception error) { if (failure == null) failure = error; }
        }
        File.WriteAllText(Path.Combine(Output, "result.txt"), failure == null ? "PASS" : "FAIL: " + failure);
        Finish(failure == null ? 0 : 1);
    }
    static IEnumerator TrainingSceneSteps(XingGuangController controller, ChapterOneRuntime runtime)
    {
        Check(runtime != null && runtime.TestMode && !runtime.useDiskSave && controller?.View?.Train != null,
            "Training scene starts in the common isolated Main scene");
        var brain = UnityEngine.Object.FindAnyObjectByType<XgBrainWallpaper>();
        var graphic = brain != null ? brain.GetComponentInChildren<XgWallpaperTraining>() : null;
        var concepts = brain != null ? brain.GetComponentInChildren<XgWallpaperConcepts>() : null;
        var presentation = brain != null ? brain.GetComponent<XgBrainWallpaperPresentation>() : null;
        var bridge = UnityEngine.Object.FindAnyObjectByType<AeroLcdSourceBridge>();
        Check(graphic != null && concepts != null && presentation != null && bridge?.inputSurface != null
            && EventSystem.current != null, "The real desktop owns the installed training, concepts, presentation and LCD components");
        Check(!graphic.raycastTarget, "Training readout cannot intercept any desktop input");
        var house = runtime.Sim; var sim = controller.Sim;
        bool oldTick = runtime.tickEnabled, oldController = controller.enabled, oldView = controller.View.enabled;
        bool oldTest = runtime.TestMode, oldReduced = controller.View.Juice.Reduced, oldQuiet = presentation.Quiet;
        bool oldRuntimeEnabled = runtime.enabled, oldGraphicEnabled = graphic.enabled;
        float oldScale = Time.timeScale;
        var modules = EventSystem.current.GetComponents<BaseInputModule>();
        var moduleEnabled = new bool[modules.Length];
        for (int i = 0; i < modules.Length; i++) moduleEnabled[i] = modules[i].enabled;
        int epochEvents = 0; var observed = new List<XgConceptTransition>();
        Action<XgEpoch> epoch = value => epochEvents++;
        Action<IReadOnlyList<XgConceptTransition>> changes = batch => observed.AddRange(batch);
        var subscribed = sim.Board;
        try
        {
            runtime.tickEnabled = false; controller.enabled = false; controller.View.enabled = true;
            TrainingSceneTestMode(runtime, false); Time.timeScale = 1;
            for (int i = 0; i < modules.Length; i++) modules[i].enabled = false;
            controller.View.Juice.Reduced = false; presentation.SetQuiet(false);
            var rig = sim.RefreshWiring(controller.Host);
            Check(controller.Host is XingGuangHost && rig.hasRig && rig.nodes.Exists(n => n.house && n.lane == XgWireLane.Gpu
                && !n.unlimited && FiniteWiring(n.cu) && FiniteWiring(n.vramMB) && n.cu > 0 && n.vramMB > 0)
                && FiniteWiring(controller.Host.Money) && controller.Host.Compute > 0,
                "The Main fixture uses real finite installed hardware and the actual Host");
            File.WriteAllText(Path.Combine(Output, "training-scene-cases.txt"),
                "First epoch: real LCD TrainButton; later boundary fixtures use real Core rules, never raised events.\n");
            File.WriteAllText(Path.Combine(Output, "training-scene-frames.csv"), "case,frame,unscaled,waves,lives,births,vertices\n");
            // Access setup uses four real judgments on naturally supplied cards, with the actual finite Host.
            sim.SelectDesk("sense");
            for (int i = 0; i < 5 && sim.Labels("sense") < 4; i++)
            {
                var card = sim.Card("sense");
                Check(sim.TeachingGuess(card, out bool guess)
                    && sim.TryEvaluateTeachingGuess("sense", card.id, guess, guess == card.truth, controller.Host, out var result)
                    && result.correct, "Real lesson supplies a training sample: " + i);
            }
            Check(sim.Labels("sense") >= 4, "Natural lesson setup supplies the required samples");
            sim.SelectedTrack = XgTrack.Sequence; sim.SetDataset(XgTrack.Sequence, "sense");
            controller.View.ShowTab("train"); controller.View.Refresh(true);
            yield return new WaitForSecondsRealtime(1.3f);
            float revealDeadline = Time.unscaledTime + 5;
            while (brain.Reveal.Age < XgWallpaperReveal.CompleteSeconds + .65f && Time.unscaledTime < revealDeadline) yield return null;
            Check(brain.Reveal.Age >= XgWallpaperReveal.CompleteSeconds + .65f, "Initial desktop and concept reveal finish on natural time");
            yield return WiringSceneDismissNotifications(bridge);
            yield return new WaitForEndOfFrame();
            Check(brain.Reveal.Visible && controller.View.Train.root.GetComponentInParent<UiFitScale>() != null,
                "Actual scaled training window and its wallpaper are visible");
            TrainingSceneEmpty(graphic, "Prepared lesson effects expire naturally before the button action");
            TrainingSceneCapture(graphic, "training-window-rest");

            sim.EpochStarted += epoch; subscribed.ConceptsChanged += changes;
            Button train = null;
            foreach (var button in controller.View.Train.root.GetComponentsInChildren<Button>())
                if (button.name == "TrainButton") { train = button; break; }
            Check(train != null && train.IsInteractable(), "Actual TrainButton is reachable and enabled for the real rig");
            int beforeEpochs = sim.S.epochs; long beforeCards = sim.S.board.cards;
            yield return null;
            var rect = (RectTransform)train.transform;
            WiringSceneClick(bridge, rect.TransformPoint(rect.rect.center), train.gameObject, "training start via real LCD");
            Check(epochEvents == 1 && sim.Run(XgTrack.Sequence).epochActive && TrainingSceneList(graphic, "waves").Count == 1,
                "Actual LCD down/up starts exactly one authoritative epoch and one wallpaper wave");
            float waveStart = TrainingSceneStart(graphic, "waves");
            controller.Window.MinimizeWindow();
            string heldState = TrainingSceneState(controller, runtime);
            yield return TrainingSceneAge(graphic, "button-wave", waveStart, .2f);
            yield return new WaitForEndOfFrame();
            Check(TrainingSceneList(graphic, "waves").Count == 1 && Time.unscaledTime - waveStart < XgWallpaperTraining.WaveSeconds,
                "Real wave is present inside its natural 0.6-second lifetime");
            TrainingSceneMesh(graphic, true); TrainingSceneCapture(graphic, "training-wave-desktop");
            yield return TrainingSceneExpired(graphic, "button-wave", waveStart, XgWallpaperTraining.WaveSeconds);
            Check(heldState == TrainingSceneState(controller, runtime), "Wave rendering alone changes no Core state, RNG or wallet");
            Check(sim.S.epochs == beforeEpochs && sim.S.board.cards == beforeCards,
                "Starting a wave does not fabricate a completed training epoch");

            // Resume the real controller only long enough for its real unscaled Update to complete that epoch.
            controller.enabled = true; float completionDeadline = Time.unscaledTime + 8;
            while (sim.Run(XgTrack.Sequence).epochActive && Time.unscaledTime < completionDeadline) yield return null;
            controller.enabled = false;
            Check(!sim.Run(XgTrack.Sequence).epochActive && sim.S.epochs == beforeEpochs + 1
                && sim.S.board.cards > beforeCards, "The real controller naturally completes the accepted training epoch");
            yield return new WaitForEndOfFrame();
            TrainingSceneMesh(graphic, false); TrainingSceneCapture(graphic, "training-epoch-completed");
            yield return new WaitForSecondsRealtime(.85f);
            TrainingSceneEmpty(graphic, "Real completion effects settle without leaving records");
            subscribed.ConceptsChanged -= changes;

            yield return TrainingScenePaused(controller, runtime, graphic, concepts, presentation);
            yield return TrainingSceneRules(controller, runtime, graphic, concepts);
            yield return TrainingSceneCapacity(controller, runtime, graphic, concepts);
            yield return TrainingSceneCleanup(controller, runtime, brain, graphic, concepts);
            Check(!runtime.useDiskSave, "All Main training checks retain isolated save handling");
            Check(true, "TRAINING_MAIN_SCENE_PASS: actual finite rig, LCD start, natural frames and real-rule boundaries; visual review required");
        }
        finally
        {
            sim.EpochStarted -= epoch; subscribed.ConceptsChanged -= changes;
            bridge.inputSurface.CancelInteraction();
            runtime.enabled = oldRuntimeEnabled; graphic.enabled = oldGraphicEnabled;
            Time.timeScale = oldScale; presentation.SetQuiet(oldQuiet);
            if (controller.View != null) controller.View.Juice.Reduced = oldReduced;
            if (runtime.Sim != house) runtime.SetSimulationForTests(house);
            TrainingSceneTestMode(runtime, oldTest);
            runtime.tickEnabled = oldTick; controller.enabled = oldController; controller.View.enabled = oldView;
            for (int i = 0; i < modules.Length; i++) if (modules[i] != null) modules[i].enabled = moduleEnabled[i];
        }
    }

    static IEnumerator TrainingScenePaused(XingGuangController controller, ChapterOneRuntime runtime,
        XgWallpaperTraining graphic, XgWallpaperConcepts concepts, XgBrainWallpaperPresentation presentation)
    {
        TrainingSceneCase("Reduced + Quiet + Time.timeScale=0: real BeginEpoch and actual R1 creation; no synthetic event or age writes");
        var sim = controller.Sim;
        controller.View.Juice.Reduced = true; presentation.SetQuiet(true);
        yield return new WaitForSecondsRealtime(1.2f);
        Time.timeScale = 0;
        yield return null;
        Check(sim.BeginEpoch(XgTrack.Sequence, controller.Host), "Reduced paused case accepts a genuine epoch start");
        sim.Board.Train(new XgBoardCard { seed = 81101, truth = true }.Add("review-paused-birth"), new XgKnobs { width = 64 });
        float started = TrainingSceneStart(graphic, "waves");
        string state = TrainingSceneState(controller, runtime);
        yield return TrainingSceneAge(graphic, "paused-reduced", started, .18f);
        yield return new WaitForEndOfFrame();
        Check(Time.timeScale == 0 && TrainingSceneList(graphic, "waves").Count == 1
            && graphic.canvasRenderer.GetAlpha() <= presentation.LightLevel + .001f && presentation.LightLevel <= .41f,
            "Paused Reduced wave remains visible with the actual Quiet brightness limit");
        foreach (DictionaryEntry birth in TrainingSceneBirths(graphic))
            Check(graphic.CellOpacity((int)birth.Key) == 1, "Reduced mode keeps its real base concept visible: " + birth.Key);
        TrainingSceneMesh(graphic, true); TrainingSceneCapture(graphic, "training-quiet-reduced-paused");
        yield return TrainingSceneAge(graphic, "paused-reduced", started, .85f);
        yield return new WaitForEndOfFrame();
        TrainingSceneEmpty(graphic, "Paused effects expire on real unscaled time"); TrainingSceneMesh(graphic, false);
        Check(state == TrainingSceneState(controller, runtime), "Paused effects mutate neither learning nor finite resources");
        Time.timeScale = 1; controller.View.Juice.Reduced = false; presentation.SetQuiet(false);
        TrainingSceneCompleteFixtureEpoch(sim, controller.Host);
        yield return new WaitForSecondsRealtime(.85f);
    }

    static IEnumerator TrainingSceneRules(XingGuangController controller, ChapterOneRuntime runtime,
        XgWallpaperTraining graphic, XgWallpaperConcepts concepts)
    {
        TrainingSceneCase("Boundary fixture: explicit parent/link and near-threshold old cells; actual Board.Train and Idle execute every R1/R2/R4 event");
        var sim = controller.Sim;
        var state = new XgBoardState { cards = XgBoard.DecayEvery - 1, nextId = 10 };
        state.concepts.Add(TrainingSceneCell(1, "a", .2, 2)); state.concepts.Add(TrainingSceneCell(2, "b", -.1, 2));
        state.concepts.Add(TrainingSceneCell(3, "decay-on-card", .01, .049));
        var pinned = TrainingSceneCell(4, "pinned", .01, .049); pinned.pinned = true; state.concepts.Add(pinned);
        var seed = TrainingSceneCell(5, XgSim.SeedKey, 1, 10); seed.seed = true; state.concepts.Add(seed);
        state.concepts.Add(TrainingSceneCell(6, "decay-on-idle", .021, .052));
        state.links.Add(new XgLink { a = 1, b = 2, c = XgBoard.LinkThreshold });
        sim.S.board = state;
        yield return null; yield return new WaitForSecondsRealtime(.15f); yield return new WaitForEndOfFrame();
        Check(ReferenceEquals(WiringSceneField<XgBoard>(graphic, "board"), sim.Board), "Readout observes the actual replacement boundary Board");
        Check(concepts.TryLocalPosition(3, out var previousDead), "The R4 target has a genuine last visible position");
        var beforeUv = TrainingSceneUv(concepts, previousDead);
        var events = new List<XgConceptTransition>(); Action<IReadOnlyList<XgConceptTransition>> receive = batch => events.AddRange(batch);
        var board = sim.Board; board.ConceptsChanged += receive;
        try
        {
            yield return null;
            board.Train(new XgBoardCard { seed = 81102, truth = true }.Add("a").Add("b"),
                new XgKnobs { depth = 2, width = 32, activation = XgActivation.Relu });
            board.Train(new XgBoardCard { seed = 81103, truth = false }.Add("same-frame-extra"), new XgKnobs { width = 64 });
            Check(events.Exists(c => c.Kind == XgConceptTransitionKind.Merged && (c.ParentA == 1 && c.ParentB == 2 || c.ParentA == 2 && c.ParentB == 1))
                && events.Exists(c => c.Kind == XgConceptTransitionKind.ElementBorn)
                && events.Exists(c => c.Kind == XgConceptTransitionKind.Decayed && c.Id == 3),
                "Two actual same-frame trains produce genuine R2, R1 and R4 records");
            Check(!events.Exists(c => c.Kind == XgConceptTransitionKind.Decayed && (c.Id == 4 || c.Id == 5)),
                "The real pinned and seed cells are excluded from R4 death feedback");
            string settled = TrainingSceneState(controller, runtime);
            yield return new WaitForEndOfFrame();
            TrainingSceneFinalMap(sim, concepts);
            foreach (var change in events)
                if (change.Kind != XgConceptTransitionKind.Decayed)
                    Check(TrainingSceneLife(graphic, change.Id) != null && TrainingSceneBirths(graphic).Contains(change.Id),
                        "Actual same-frame birth has its matching life and opacity identity: " + change.Id);
            var dead = TrainingSceneLife(graphic, 3);
            Check(dead != null && Vector2.Distance(WiringSceneField<Vector2>(dead, "retiredUv"), beforeUv) < .00001f,
                "R4 retains its own last shown position after the final same-frame layout remaps");
            Check(!concepts.TryLocalPosition(3, out _), "The removed ID is absent from the final live-cell map");
            TrainingSceneMesh(graphic, true); TrainingSceneCapture(graphic, "training-r1-r2-r4-same-frame");
            yield return new WaitForSecondsRealtime(.85f);
            TrainingSceneEmpty(graphic, "Same-frame birth/death records and the opacity index expire together");
            Check(settled == TrainingSceneState(controller, runtime), "Natural birth/death rendering is read-only after the real rules settle");

            Check(concepts.TryLocalPosition(6, out _), "The idle-only victim remains genuinely visible before Idle");
            events.Clear(); yield return null; board.Idle(64);
            Check(events.Exists(c => c.Kind == XgConceptTransitionKind.Decayed && c.Id == 6), "Actual Idle R4 publishes the idle victim's real removal");
            float started = Time.unscaledTime; settled = TrainingSceneState(controller, runtime);
            yield return TrainingSceneAge(graphic, "idle-decay", started, .36f);
            yield return new WaitForEndOfFrame();
            Check(TrainingSceneLife(graphic, 6) != null && !concepts.TryLocalPosition(6, out _),
                "Idle death remains as a transient grey remnant while its real concept is gone");
            TrainingSceneMesh(graphic, true); TrainingSceneCapture(graphic, "training-idle-r4-grey");
            yield return TrainingSceneAge(graphic, "idle-decay", started, .85f);
            TrainingSceneEmpty(graphic, "Idle death clears naturally");
            Check(settled == TrainingSceneState(controller, runtime), "Idle death presentation does not perform extra decay");
        }
        finally { board.ConceptsChanged -= receive; }
    }

    static IEnumerator TrainingSceneCapacity(XingGuangController controller, ChapterOneRuntime runtime,
        XgWallpaperTraining graphic, XgWallpaperConcepts concepts)
    {
        TrainingSceneCase("Capacity boundary: 5 actual BeginEpoch/Tick completions within one render frame, then 160 real raw feature creations; no synthetic callbacks");
        var sim = controller.Sim;
        yield return null;
        for (int i = 0; i < 5; i++)
        {
            Check(sim.BeginEpoch(XgTrack.Sequence, controller.Host), "Capacity fixture starts a genuine epoch: " + i);
            TrainingSceneCompleteFixtureEpoch(sim, controller.Host);
        }
        Check(TrainingSceneList(graphic, "waves").Count == 4, "Five actual starts retain only the four newest waves");
        var card = new XgBoardCard { seed = 81104, truth = true };
        for (int i = 0; i < 160; i++) card.Add("capacity-" + i);
        var ids = new List<int>(); Action<IReadOnlyList<XgConceptTransition>> receive = batch =>
        { foreach (var item in batch) if (item.Kind == XgConceptTransitionKind.ElementBorn) ids.Add(item.Id); };
        var board = sim.Board; board.ConceptsChanged += receive;
        try { board.Train(card, new XgKnobs { width = 512 }); }
        finally { board.ConceptsChanged -= receive; }
        Check(ids.Count == 160 && TrainingSceneList(graphic, "lives").Count == 128 && TrainingSceneBirths(graphic).Count == 128,
            "160 genuine births retain exactly 128 newest lives and opacity entries");
        for (int i = 0; i < ids.Count; i++)
            if (TrainingSceneBirths(graphic).Contains(ids[i]) != (i >= 32)) throw new Exception("Birth capacity retained the wrong ID: " + ids[i]);
        string state = TrainingSceneState(controller, runtime);
        float capacityStarted = Time.unscaledTime;
        yield return new WaitForEndOfFrame();
        TrainingSceneFinalMap(sim, concepts); TrainingSceneMesh(graphic, true);
        yield return TrainingSceneAge(graphic, "capacity", capacityStarted, .14f);
        yield return new WaitForEndOfFrame();
        TrainingSceneMesh(graphic, true);
        TrainingSceneCapture(graphic, "training-capacity-final-state");
        Check(state == TrainingSceneState(controller, runtime), "Rendering capped effects leaves the fully trained final state unchanged");
    }

    static IEnumerator TrainingSceneCleanup(XingGuangController controller, ChapterOneRuntime runtime,
        XgBrainWallpaper brain, XgWallpaperTraining graphic, XgWallpaperConcepts concepts)
    {
        TrainingSceneCase("Lifecycle: real component disable/re-enable, Board replacement, runtime disable and isolated SetSimulationForTests save swap");
        var sim = controller.Sim; var board = sim.Board;
        graphic.enabled = false;
        TrainingSceneEmpty(graphic, "Disabling immediately clears waves, lives and the opacity index");
        board.Train(new XgBoardCard { seed = 81105, truth = true }.Add("disabled-source"), new XgKnobs { width = 512 });
        TrainingSceneEmpty(graphic, "Disabled readout ignores genuine old-source training");
        graphic.enabled = true; yield return null; yield return new WaitForEndOfFrame();
        TrainingSceneEmpty(graphic, "Re-enabling does not replay past births");
        yield return null;
        board.Train(new XgBoardCard { seed = 81106, truth = true }.Add("enabled-source"), new XgKnobs { width = 512 });
        Check(TrainingSceneList(graphic, "lives").Count > 0, "Re-enabled readout receives a new real birth");
        sim.S.board = new XgBoardState();
        yield return null; yield return new WaitForEndOfFrame();
        TrainingSceneEmpty(graphic, "Board replacement clears all previous-board effects");
        board.Train(new XgBoardCard { seed = 81107, truth = false }.Add("retired-board"), new XgKnobs { width = 512 });
        TrainingSceneEmpty(graphic, "Retired Board can no longer deliver callbacks");
        var current = sim.Board;
        current.Train(new XgBoardCard { seed = 81108, truth = true }.Add("current-board"), new XgKnobs { width = 64 });
        Check(TrainingSceneList(graphic, "lives").Count > 0, "Current Board remains subscribed after replacement");
        runtime.enabled = false; yield return null;
        TrainingSceneEmpty(graphic, "Disabling the runtime clears activity and detaches the source");
        current.Train(new XgBoardCard { seed = 81109, truth = true }.Add("disabled-runtime"), new XgKnobs { width = 64 });
        TrainingSceneEmpty(graphic, "Inactive runtime cannot receive training effects");
        runtime.enabled = true; yield return null;
        current.Train(new XgBoardCard { seed = 81110, truth = true }.Add("before-save-swap"), new XgKnobs { width = 64 });
        Check(TrainingSceneList(graphic, "lives").Count > 0, "An active real effect exists before save swap");
        var replacement = new ChapterOneSim();
        replacement.CompletePrologue(new PrologueProfile { name = "小星", self = "我", callMe = "你", personality = "好奇" });
        runtime.SetSimulationForTests(replacement); TrainingSceneTestMode(runtime, false);
        yield return null; yield return new WaitForEndOfFrame();
        TrainingSceneEmpty(graphic, "Actual isolated save swap clears old training effects");
        Check(!ReferenceEquals(controller.Sim, sim) && ReferenceEquals(WiringSceneField<XgSim>(graphic, "source"), controller.Sim),
            "The readout observes the replacement real controller simulation");
        current.Train(new XgBoardCard { seed = 81111, truth = false }.Add("retired-save"), new XgKnobs { width = 64 });
        TrainingSceneEmpty(graphic, "Genuine old-save training cannot leak into the new save");
        yield return null;
        controller.Sim.Board.Train(new XgBoardCard { seed = 81112, truth = true }.Add("new-save"), new XgKnobs { width = 64 });
        Check(brain.Reveal.Visible && TrainingSceneList(graphic, "lives").Count > 0,
            "The new completed save receives its own genuine birth");
        yield return new WaitForSecondsRealtime(.85f); yield return new WaitForEndOfFrame();
        TrainingSceneEmpty(graphic, "The replacement save's effect expires naturally");
        TrainingSceneMesh(graphic, false); TrainingSceneCapture(graphic, "training-save-swap-clean");
    }

    static void TrainingSceneTestMode(ChapterOneRuntime runtime, bool value)
        => typeof(ChapterOneRuntime).GetProperty("TestMode").GetSetMethod(true).Invoke(runtime, new object[] { value });
    static IList TrainingSceneList(XgWallpaperTraining graphic, string name) => WiringSceneField<IList>(graphic, name);
    static IDictionary TrainingSceneBirths(XgWallpaperTraining graphic) => WiringSceneField<IDictionary>(graphic, "births");
    static object TrainingSceneLife(XgWallpaperTraining graphic, int id)
    {
        foreach (var life in TrainingSceneList(graphic, "lives"))
            if (WiringSceneField<XgConceptTransition>(life, "change").Id == id) return life;
        return null;
    }
    static float TrainingSceneStart(XgWallpaperTraining graphic, string name)
    {
        var list = TrainingSceneList(graphic, name); Check(list.Count > 0, "Actual effect has a natural start time: " + name);
        return WiringSceneField<float>(list[list.Count - 1], "started");
    }
    static void TrainingSceneEmpty(XgWallpaperTraining graphic, string message)
        => Check(TrainingSceneList(graphic, "waves").Count == 0 && TrainingSceneList(graphic, "lives").Count == 0
            && TrainingSceneBirths(graphic).Count == 0, message);
    static IEnumerator TrainingSceneAge(XgWallpaperTraining graphic, string label, float started, float age)
    {
        while (Time.unscaledTime - started < age) { TrainingSceneFrame(graphic, label); yield return null; }
    }
    static IEnumerator TrainingSceneExpired(XgWallpaperTraining graphic, string label, float started, float seconds)
    {
        while (TrainingSceneList(graphic, "waves").Count > 0 && Time.unscaledTime - started < seconds + 2)
        { TrainingSceneFrame(graphic, label); yield return null; }
        Check(TrainingSceneList(graphic, "waves").Count == 0 && Time.unscaledTime - started >= seconds,
            "Natural wave expires at or after its " + seconds + " second lifetime");
    }
    static void TrainingSceneFrame(XgWallpaperTraining graphic, string label)
    {
        var mesh = graphic.canvasRenderer.GetMesh();
        File.AppendAllText(Path.Combine(Output, "training-scene-frames.csv"), label + "," + Time.frameCount + "," + Time.unscaledTime
            + "," + TrainingSceneList(graphic, "waves").Count + "," + TrainingSceneList(graphic, "lives").Count
            + "," + TrainingSceneBirths(graphic).Count + "," + (mesh != null ? mesh.vertexCount : 0) + "\n");
    }
    static string TrainingSceneState(XingGuangController controller, ChapterOneRuntime runtime)
    {
        string state = JsonUtility.ToJson(controller.Sim.S) + "|" + JsonUtility.ToJson(runtime.Sim.S);
        foreach (var link in controller.Sim.S.board.links) state += "|" + link.a + "," + link.b + "," + link.c.ToString("R") + "," + link.last;
        return state + "|wallet=" + controller.Host.Money.ToString("R");
    }
    static XgConcept TrainingSceneCell(int id, string key, double weight, double strength)
        => new XgConcept { id = id, key = key, region = "logic", layer = 1, w = weight, s = strength };
    static Vector2 TrainingSceneUv(XgWallpaperConcepts concepts, Vector2 point)
    { var rect = concepts.rectTransform.rect; return new Vector2((point.x - rect.xMin) / rect.width, (point.y - rect.yMin) / rect.height); }
    static void TrainingSceneCompleteFixtureEpoch(XgSim sim, IXgHost host)
    {
        // Explicit capacity/setup acceleration of Core time only. Effect ages always use natural Unity time.
        for (int i = 0; i < 40 && sim.Run(XgTrack.Sequence).epochActive; i++) sim.Tick(.25, host);
        Check(!sim.Run(XgTrack.Sequence).epochActive, "Boundary fixture completes its real epoch within bounded Core ticks");
    }
    static void TrainingSceneFinalMap(XgSim sim, XgWallpaperConcepts concepts)
    {
        var actual = new HashSet<int>(); foreach (var cell in concepts.Cells) actual.Add(cell.id);
        var expected = new HashSet<int>(); foreach (var cell in sim.S.board.concepts) if (!cell.seed) expected.Add(cell.id);
        Check(expected.Count <= XgWallpaperConcepts.MeshCellLimit && actual.SetEquals(expected),
            "End-of-frame concept map exactly matches all actual final-state IDs in the bounded fixture");
    }
    static void TrainingSceneMesh(XgWallpaperTraining graphic, bool nonempty)
    {
        var mesh = graphic.canvasRenderer.GetMesh(); int count = mesh != null ? mesh.vertexCount : 0;
        using (var expected = new VertexHelper())
        {
            typeof(XgWallpaperTraining).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(graphic, new object[] { expected });
            Check(count == expected.currentVertCount && count <= 4096 && (!nonempty || count > 0),
                "Actual EOF training mesh is current, nonempty when required, and within 4096 vertices: " + count);
            if (count == 0) return;
            var points = mesh.vertices; var colors = mesh.colors32; var vertex = default(UIVertex);
            for (int i = 0; i < count; i++)
            {
                expected.PopulateUIVertex(ref vertex, i);
                if (!points[i].Equals(vertex.position) || !colors[i].Equals(vertex.color))
                    throw new Exception("Training EOF mesh differs from natural current state at vertex " + i);
                if (!FiniteWiring(points[i].x) || !FiniteWiring(points[i].y)) throw new Exception("Nonfinite training mesh vertex " + i);
            }
            Check(true, "Every rendered training vertex and color matches its same-frame source state");
        }
    }
    static void TrainingSceneCapture(XgWallpaperTraining graphic, string name)
    { TrainingSceneFrame(graphic, name); WiringSceneCapture(name); }
    static void TrainingSceneCase(string text) => File.AppendAllText(Path.Combine(Output, "training-scene-cases.txt"), text + "\n");
}
