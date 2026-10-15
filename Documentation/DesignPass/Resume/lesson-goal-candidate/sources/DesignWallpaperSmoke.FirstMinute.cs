using System;
using System.Collections;
using System.IO;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop.LLM;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Story;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEditor;

public static partial class DesignWallpaperSmoke
{
    const string FirstMinuteScenarioKey = "DesignWallpaperSmoke.FirstMinuteScenario";
    public static void RunFirstMinute() { StartFirstMinuteScenario("natural"); }
    public static void RunFirstMinuteIdle() { StartFirstMinuteScenario("idle"); }
    public static void RunFirstMinuteCorrection() { StartFirstMinuteScenario("correction"); }
    public static void RunFirstMinuteEnglishCorrection() { StartFirstMinuteScenario("english-correction"); }
    public static void RunFirstMinutePendingChat() { StartFirstMinuteScenario("pending-chat"); }
    static void StartFirstMinuteScenario(string scenario)
    {
        SessionState.SetString(FirstMinuteScenarioKey, scenario);
        Start("first-minute");
    }

    // Main dispatches here directly after its isolated case 0, before case 11 completes setup.
    static void TickFirstMinute(XingGuangController controller, ChapterOneRuntime runtime, XgBrainWallpaper brain)
    {
        if (step != 200) return;
        step = 201;
        controller.StartCoroutine(CheckFirstMinute(controller, runtime, brain));
    }

    static T FirstMinuteField<T>(object target, string name) => (T)target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    static IEnumerator CheckFirstMinute(XingGuangController controller, ChapterOneRuntime runtime, XgBrainWallpaper brain)
    {
        var checks = FirstMinuteSteps(controller, runtime, brain);
        Exception failure = null;
        while (true)
        {
            object current = null;
            bool more = false;
            try { more = checks.MoveNext(); if (more) current = checks.Current; }
            catch (Exception error) { failure = error; }
            if (failure != null || !more) break;
            yield return current;
        }
        (checks as IDisposable)?.Dispose();
        File.WriteAllText(Path.Combine(Output, "result.txt"), failure == null ? "PASS" : "FAIL: " + failure);
        Finish(failure == null ? 0 : 1);
    }

    static IEnumerator FirstMinuteSteps(XingGuangController controller, ChapterOneRuntime runtime, XgBrainWallpaper brain)
    {
        string scenario = SessionState.GetString(FirstMinuteScenarioKey, "natural");
        bool englishCorrection = scenario == "english-correction";
        bool idle = scenario == "idle", deterministicCorrection = scenario == "correction" || englishCorrection, pendingChat = scenario == "pending-chat";
        Check(runtime != null && !runtime.useDiskSave && runtime.Sim.InPrologue, "First-minute run starts in isolated pre-setup state");
        var story = runtime.GetComponent<StoryDirector>();
        var prologue = runtime.GetComponent<PrologueDirector>();
        var presenter = runtime.GetComponent<StoryDesktopPresenter>();
        Check(story != null && prologue != null && presenter != null && presenter.enabled, "Real desktop story components are present");
        bool wasStory = story.enabled, wasPrologue = prologue.enabled, wasTest = runtime.TestMode;
        var model = LocalLlm.Instance;
        bool wasModel = model != null && model.enabled;
        IDisposable firstMinuteLanguage = null;
        var priorSourceLanguage = Lang.IsEnglish;
        bool priorSimEnglish = controller.Sim.English;
        XgSim lessonSim = null;
        int lessons = 0, corrected = 0;
        Action<XgCard, XgAnswer> observed = (card, answer) => { lessons++; if (answer.corrected) corrected++; };
        try
        {
            if (englishCorrection)
            {
                firstMinuteLanguage = GameText.Bind(() => GameText.English);
                controller.Sim.English = true;
                GameText.PublishChanged();
                Check(GameText.IsEnglish && Lang.English && controller.Sim.English, "English scenario uses the live desktop, source-text and simulation locale");
            }
            // Only network/model work is excluded. TestMode is off for actual story scheduling and all sixty seconds.
            if (model != null) model.enabled = false;
            typeof(ChapterOneRuntime).GetProperty("TestMode").GetSetMethod(true).Invoke(runtime, new object[] { false });
            runtime.Sim.InstallApp();
            controller.Open();
            story.enabled = true;
            yield return new WaitForSecondsRealtime(1);
            controller.View.Refresh(true);
            var setup = FirstMinuteField<XgSetupOverlay>(controller.View, "setup");
            Check(setup != null && setup.Showing && !runtime.TestMode && story.Active, "Real setup is visible with production story scheduling");
            FirstMinuteField<TMP_InputField>(setup, "name").text = englishCorrection ? "Star" : "小星";
            FirstMinuteField<TMP_InputField>(setup, "mood").text = englishCorrection ? "Curious" : "好奇";
            Click(FirstMinuteField<XgBtn>(setup, "done").rt);
            float setupDeadline = Time.unscaledTime + 10;
            while (runtime.Sim.InPrologue && Time.unscaledTime < setupDeadline) yield return null;
            Check(!runtime.Sim.InPrologue, "Setup submit reaches production CompletePrologue");
            yield return new WaitForSecondsRealtime(.2f);
            Click(FirstMinuteField<XgBtn>(setup, "yes").rt);
            while (setup.Showing && Time.unscaledTime < setupDeadline) yield return null;
            Check(!setup.Showing && controller.View.Tab == "teach", "Setup first answer enters the teaching page through real pointer input");
            prologue.enabled = true;
            if (pendingChat)
            {
                var focusClock = (XgFirstLessonFocus)typeof(XgFirstLessonWindow).GetField("focus", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                Check(double.IsNaN(FirstMinuteField<double>(focusClock, "presentedAt")), "Live setup completed before the first card's presentation clock started");
                Check(XgYyTalk.Open(playerRequested: true), "Explicit player chat request succeeds while presentation is still pending");
                // This is an API boundary probe of the explicit UI action; ordinary chat below also uses the real button.
                var pendingConversation = CheckFirstMinuteOfflineChat(controller);
                while (pendingConversation.MoveNext()) yield return pendingConversation.Current;
                Check(double.IsNaN(FirstMinuteField<double>(focusClock, "presentedAt")) && OpeningSequence.FirstLessonActive,
                    "Chat can send and receive while first-card protection waits for presentation");
                controller.Open(); controller.View.ShowTab("teach");
            }
            Check(controller.Sim.S.desk == XgMemes.SenseDesk && controller.Sim.Card(controller.Sim.S.desk).dataset == XgMemes.SenseDesk
                && !controller.Sim.DeskOpen("arith"), "Real setup enters the default common-sense desk with arithmetic still locked");
            if (deterministicCorrection)
            {
                Check(controller.Sim.S.handCorrect == 0 && controller.Sim.S.handWrong == 0, "Correction fixture precedes all teaching");
                var phrase = XgMemes.Pick(XgMemes.SenseDesk, new System.Random(9137), 1, controller.Sim.Today, controller.Sim.Topic, false);
                Check(phrase != null && phrase.yes && phrase.textEn == "Beijing is the capital of China.",
                    "Correction input is a real, authored common-sense phrase with unchanged truth");
                var fixture = new XgCard { id = 91370001, dataset = XgMemes.SenseDesk, track = (int)XgTrack.Sequence,
                    seed = 9137, truth = phrase.yes, level = 1, question = phrase.text, questionEn = phrase.textEn,
                    why = phrase.why, whyEn = phrase.whyEn, source = phrase.source,
                    category = XgCatalog.Desk(XgMemes.SenseDesk).name, categoryEn = XgCatalog.Desk(XgMemes.SenseDesk).nameEn };
                var features = XgBoardData.FromCard(fixture);
                bool realWords = features.region == "sequence" && features.features.Count > 0;
                foreach (var feature in features.features) realWords &= fixture.question.Contains(feature.name);
                Check(realWords, "Deterministic common-sense card uses its real production word features");
                controller.Sim.S.cards.RemoveAll(c => c.dataset == XgMemes.SenseDesk);
                controller.Sim.S.cards.Add(fixture);
                Check(controller.Sim.TeachingGuess(fixture, out bool predicted) && !predicted, "Deterministic UI input has true answer and actual false model prediction");
                controller.View.Refresh(true);
                File.AppendAllText(Path.Combine(Output, "first-minute-observations.txt"), "Scenario=deterministic common-sense correction; only card input replaced with its authored bank phrase before any lesson.\n");
            }
            yield return null;
            yield return new WaitForEndOfFrame();
            float firstVisible = Time.unscaledTime;
            Check(OpeningSequence.FirstLessonActive, "The real setup armed first-minute protection");
            Check(story.enabled && story.Active && prologue.enabled && !runtime.TestMode, "Opening actors run normally throughout the observation");
            lessonSim = controller.Sim;
            lessonSim.LessonTaught += observed;
            Capture("first-minute-before-answer");
            CaptureVisualState("first-minute-before-answer");
            Check(brain.GetComponent<UnityEngine.UI.Image>().canvasRenderer.GetInheritedAlpha() > .99f,
                "Desktop fade has fully completed before teaching; the brain background is opaque");
            ScreenCapture.CaptureScreenshot(Path.Combine(Output, "first-minute-before-answer-full.png"));
            var companion = controller.View.GetComponentInChildren<XgTeachCompanion>(true);
            Check(companion != null, "Teaching companion exists in the actual scene");
            var focus = FirstMinuteField<TMP_Text>(companion, "next");
            var progress = FirstMinuteField<TMP_Text>(companion, "progress");
            var train = FirstMinuteField<XgBtn>(companion, "train");
            var chat = FirstMinuteField<XgBtn>(companion, "chat");
            var board = FirstMinuteField<XgBtn>(companion, "brain");
            if (englishCorrection)
            {
                CheckFirstMinuteFrame(runtime, story, presenter, controller);
                CheckFirstMinuteVisibleObjectives(controller);
                var card = lessonSim.Card(lessonSim.S.desk);
                Check(card.id == 91370001 && card.truth && lessonSim.TeachingGuess(card, out bool guess) && !guess,
                    "English correction uses the actual first-card false prediction and unchanged true answer");
                Capture("first-minute-english-before");
                CaptureVisualState("first-minute-english-before");
                ScreenCapture.CaptureScreenshot(Path.Combine(Output, "first-minute-english-before-full.png"));
                CheckFirstMinuteEnglishLabels(controller, false);
                Click(controller.View.Teach.NoButton);
                Check(lessons == 1 && corrected == 1 && lessonSim.S.handCorrect == 1 && lessonSim.S.handWrong == 0,
                    "English Guessed wrong pointer teaches exactly one correction");
                Check(LessonPulse(brain.GetComponentInChildren<XgWallpaperConcepts>()) > 0,
                    "English first-card correction updates wallpaper in the input frame");
                yield return new WaitForEndOfFrame();
                CheckFirstMinuteFeedback(controller, card, true, true);
                CheckFirstMinuteEnglishLabels(controller, true);
                Capture("first-minute-english-corrected");
                CaptureVisualState("first-minute-english-corrected");
                ScreenCapture.CaptureScreenshot(Path.Combine(Output, "first-minute-english-corrected-full.png"));
                yield return new WaitForSecondsRealtime(.6f);
                yield return new WaitForEndOfFrame();
                CheckFirstMinuteFrame(runtime, story, presenter, controller);
                CheckFirstMinuteVisibleObjectives(controller);
                CheckFirstMinuteFeedback(controller, card, true, true);
                CheckFirstMinuteEnglishLabels(controller, true);
                Check(OpeningSequence.FirstLessonActive && Time.unscaledTime - firstVisible < 60,
                    "Focused English verification ends within the untouched first-minute protection window");
                Capture("first-minute-english-corrected-readable");
                CaptureVisualState("first-minute-english-corrected-readable");
                var finalScreenshot = Path.Combine(Output, "first-minute-english-corrected-readable-full.png");
                if (File.Exists(finalScreenshot)) File.Delete(finalScreenshot);
                ScreenCapture.CaptureScreenshot(finalScreenshot);
                yield return null; // CaptureScreenshot writes asynchronously after the rendered frame.
                float screenshotDeadline = Time.realtimeSinceStartup + 10;
                while (!FirstMinuteScreenshotWritten(finalScreenshot) && Time.realtimeSinceStartup < screenshotDeadline)
                    yield return null;
                Check(FirstMinuteScreenshotWritten(finalScreenshot), "Final English full-viewport PNG is complete on disk before Finish");
                Check(FirstMinuteScreenshotWritten(Path.Combine(Output, "first-minute-english-before-full.png"))
                    && FirstMinuteScreenshotWritten(Path.Combine(Output, "first-minute-english-corrected-full.png")),
                    "Earlier English full-viewport PNG captures are also complete on disk");
                Check(true, "FIRST_MINUTE_ENGLISH_CORRECTION_PASS (focused locale and layout scene; does not repeat the sixty-second acceptance)");
                yield break;
            }
            CheckLessonGoalState(controller, 0);
            int answers = 0, sample = -1;
            float nextAnswer = firstVisible + 8;
            bool checkedNavigation = false, capturedCorrection = false;
            while (OpeningSequence.FirstLessonActive && Time.unscaledTime - firstVisible < 62)
            {
                CheckFirstMinuteFrame(runtime, story, presenter, controller);
                CheckFirstMinuteVisibleObjectives(controller);
                if (focus.text.Contains("训练") || focus.text.Contains("Train") || focus.text.Contains("Talk")
                    || progress.text.Contains("Ready to train") || progress.text.Contains("可以训练"))
                    throw new Exception("First-minute prominent copy introduced a training/chat objective");
                float elapsed = Time.unscaledTime - firstVisible;
                int slot = (int)(elapsed / 10);
                if (slot != sample)
                {
                    sample = slot;
                    File.AppendAllText(Path.Combine(Output, "first-minute-observations.txt"), "t=" + elapsed.ToString("F2")
                        + " tab=" + controller.View.Tab + " lessons=" + lessons + " corrections=" + corrected + "\n");
                    Capture("first-minute-t" + slot * 10);
                }
                if (!idle && answers < 4 && Time.unscaledTime >= nextAnswer)
                {
                    var card = lessonSim.Card(lessonSim.S.desk);
                    Check(lessonSim.TeachingGuess(card, out bool guess), "The actual shown card has a model guess");
                    int prior = lessons;
                    bool willCorrect = guess != card.truth;
                    Click(guess == card.truth ? controller.View.Teach.YesButton : controller.View.Teach.NoButton);
                    Check(lessons == prior + 1, "Real teaching pointer settles exactly one lesson");
                    if (deterministicCorrection && answers == 0)
                        Check(willCorrect && corrected == 1 && LessonPulse(brain.GetComponentInChildren<XgWallpaperConcepts>()) > 0,
                            "Known correction through actual UI emits learning and wallpaper response immediately");
                    answers++;
                    nextAnswer = Time.unscaledTime + 1;
                    yield return new WaitForEndOfFrame();
                    CheckFirstMinuteFeedback(controller, card, willCorrect, true);
                    if (willCorrect && !capturedCorrection)
                    {
                        capturedCorrection = true;
                        Capture("first-minute-corrected");
                        ScreenCapture.CaptureScreenshot(Path.Combine(Output, "first-minute-corrected-full.png"));
                        yield return new WaitForSecondsRealtime(.6f);
                        yield return new WaitForEndOfFrame();
                        CheckFirstMinuteFeedback(controller, card, true, true);
                        Capture("first-minute-corrected-readable");
                        ScreenCapture.CaptureScreenshot(Path.Combine(Output, "first-minute-corrected-readable-full.png"));
                    }
                }
                if (!idle && answers == 4 && !checkedNavigation)
                {
                    yield return new WaitForSecondsRealtime(.2f);
                    CheckLessonGoalState(controller, XgCatalog.SamplesToTrain);
                    Check(train.button.IsInteractable() && chat.button.IsInteractable() && board.button.IsInteractable(),
                        "Training, chat and brain entries remain available during the first minute");
                    Click(train.rt);
                    Check(controller.View.Tab == "train" && OpeningSequence.FirstLessonActive, "Player may navigate to training before the minute ends");
                    controller.View.ShowTab("teach");
                    Click(chat.rt);
                    var yy = LingGuangV05.Desktop.YY.YYChatHub.Instance;
                    Check(yy != null && yy.S.lingguangUnlocked && yy.S.selected == LingGuangV05.Desktop.YY.YYChatHub.LingGuangId
                        && OpeningSequence.FirstLessonActive && !AiJoinsYy.Playing, "Player-requested chat opens during first-minute protection without an automatic scene");
                    int messages = yy.Conversation(LingGuangV05.Desktop.YY.YYChatHub.LingGuangId).messages.Count;
                    controller.Open(); controller.View.ShowTab("teach");
                    yield return new WaitForSecondsRealtime(.2f);
                    Click(chat.rt);
                    Check(yy.Conversation(LingGuangV05.Desktop.YY.YYChatHub.LingGuangId).messages.Count == messages,
                        "Opening the same AI contact twice adds no duplicate greeting");
                    var chatChecks = CheckFirstMinuteOfflineChat(controller);
                    while (chatChecks.MoveNext()) yield return chatChecks.Current;
                    controller.Open(); controller.View.ShowTab("teach");
                    checkedNavigation = true;
                }
                yield return null;
            }
            Check(!OpeningSequence.FirstLessonActive && lessons == (idle ? 0 : 4) && Time.unscaledTime - firstVisible >= 59,
                "First minute expires naturally after the complete " + scenario + " observation window");
            if (idle) Check(answers == 0 && controller.Sim.S.handCorrect == 0 && controller.Sim.S.handWrong == 0,
                "Idle sixty-second scene leaves the very first card unanswered");
            if (deterministicCorrection) Check(capturedCorrection, "Known correction screenshot was captured in the actual scene");
            File.AppendAllText(Path.Combine(Output, "first-minute-observations.txt"), "Natural correction captured=" + capturedCorrection
                + ". If false, deterministic correction scene B remains required.\n");
            yield return new WaitForEndOfFrame();
            Capture("first-minute-finished");
            Check(true, "FIRST_MINUTE_REAL_SETUP_PASS scenario=" + scenario + " (visual review remains required)");
        }
        finally
        {
            if (firstMinuteLanguage != null)
            {
                firstMinuteLanguage.Dispose();
                Lang.IsEnglish = priorSourceLanguage;
                controller.Sim.English = priorSimEnglish;
                GameText.PublishChanged();
            }
            if (lessonSim != null) lessonSim.LessonTaught -= observed;
            story.enabled = wasStory;
            prologue.enabled = wasPrologue;
            typeof(ChapterOneRuntime).GetProperty("TestMode").GetSetMethod(true).Invoke(runtime, new object[] { wasTest });
            if (model != null) model.enabled = wasModel;
        }
    }

    static IEnumerator CheckFirstMinuteOfflineChat(XingGuangController controller)
    {
        var hub = YYChatHub.Instance;
        Check(hub != null && hub.View != null && hub.S.selected == YYChatHub.LingGuangId,
            "AI conversation is selected for actual message input");
        yield return new WaitForSecondsRealtime(.3f);
        var conversation = hub.Conversation(YYChatHub.LingGuangId);
        int before = conversation.messages.Count;
        const string message = "你能看懂这句话吗？";
        var input = FirstMinuteField<TMP_InputField>(hub.View, "input");
        input.text = message;
        RectTransform send = null;
        foreach (var button in hub.View.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            if (button.name == "Send") { send = button.transform as RectTransform; break; }
        Check(send != null, "YY production Send button is present");
        Click(send);
        float deadline = Time.unscaledTime + 8;
        bool mine = false, replied = false;
        while (Time.unscaledTime < deadline)
        {
            for (int i = before; i < conversation.messages.Count; i++)
            {
                var line = conversation.messages[i];
                if (line.from == YYChatHub.Me && line.text == message) mine = true;
                if (line.from == YYChatHub.LingGuangId && !string.IsNullOrWhiteSpace(line.text)) replied = true;
            }
            if (mine && replied) break;
            yield return null;
        }
        Check(mine && replied && (LocalLlm.Instance == null || !LocalLlm.Instance.Ready),
            "Real Send posts the player's line and receives a visible offline AI response");
        Check(OpeningSequence.FirstLessonActive && !AiJoinsYy.Playing, "Player conversation preserves first-minute teaching protection");
        yield return new WaitForEndOfFrame();
        Capture("first-minute-offline-chat");
        ScreenCapture.CaptureScreenshot(Path.Combine(Output, "first-minute-offline-chat-full.png"));
        input.DeactivateInputField();
    }

    static void CheckFirstMinuteFrame(ChapterOneRuntime runtime, StoryDirector story, StoryDesktopPresenter presenter, XingGuangController controller)
    {
        // Throw directly for repeated frame guards so the evidence file remains readable.
        if (runtime.TestMode || !story.enabled || !story.Active || presenter.CutscenePlaying || AiJoinsYy.Playing)
            throw new Exception("First-minute scheduling disabled or an external scene interrupted teaching");
        if (controller.View.Tab != "teach") throw new Exception("The teaching page lost its selected tab without player navigation");
    }

    static void CheckFirstMinuteVisibleObjectives(XingGuangController controller)
    {
        // Include the toast and full teaching window, not only the companion's two selected labels.
        foreach (var label in controller.View.GetComponentsInChildren<TMP_Text>())
        {
            string content = label.text ?? "";
            if (content.Contains("可以训练") || content.Contains("training unlocked") || content.Contains("Ready to train")
                || content.Contains("带它去训练") || content.Contains("Train together") || content.Contains("去「训练」")
                || content.Contains("more to train"))
                throw new Exception("Visible teaching UI introduced a training objective at " + label.name + ": " + content);
        }
        foreach (var note in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
        {
            if (note.name != "Guide Note" || !note.gameObject.activeInHierarchy) continue;
            foreach (var label in note.GetComponentsInChildren<TMP_Text>())
                if (label.text.Contains("训练") || label.text.Contains("Train") || label.text.Contains("train"))
                    throw new Exception("Desktop guide introduced training during the first lesson: " + label.text);
        }
    }

    static void CheckFirstMinuteFeedback(XingGuangController controller, XgCard answered, bool correction, bool accurate)
    {
        var feedback = FirstMinuteField<TMP_Text>(controller.View.Teach, "feedback");
        var back = FirstMinuteField<UnityEngine.UI.Image>(controller.View.Teach, "lessonFeedbackBack");
        Check(feedback.isActiveAndEnabled && back.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(feedback.text),
            "Last-card feedback has a visible, separate background");
        Check(feedback.text.StartsWith("上一题：", StringComparison.Ordinal) || feedback.text.StartsWith("Last card:", StringComparison.Ordinal),
            "Feedback explicitly belongs to the previous card");
        Check(controller.Sim.Card(controller.Sim.S.desk).id != answered.id, "Previous-card feedback remains while the next card is already shown");
        Check(feedback.text.Contains(answered.truth ? "是" : "否") || feedback.text.Contains(answered.truth ? "Yes" : "No"),
            "Feedback displays the answered card's truth rather than the next card's answer");
        if (correction && accurate) Check(feedback.text.Contains("你纠正了它") || feedback.text.Contains("you corrected it"), "Successful correction stays readable in its feedback");
        feedback.ForceMeshUpdate();
        Check(feedback.fontSize >= 13 && !feedback.isTextOverflowing && feedback.textInfo.lineCount <= 2,
            "Actual rendered feedback fits two readable lines without shrinking below thirteen");
        var rect = feedback.rectTransform.rect;
        for (int i = 0; i < feedback.textInfo.characterCount; i++)
        {
            var character = feedback.textInfo.characterInfo[i];
            if (!character.isVisible) continue;
            Check(character.bottomLeft.x >= rect.xMin - 1 && character.topRight.x <= rect.xMax + 1
                && character.bottomLeft.y >= rect.yMin - 1 && character.topRight.y <= rect.yMax + 1,
                "Feedback glyph lies inside its own panel");
        }
        float dark = FirstMinuteLuminance(feedback.color), light = FirstMinuteLuminance(back.color);
        Check((Mathf.Max(light, dark) + .05f) / (Mathf.Min(light, dark) + .05f) >= 7,
            "Feedback foreground and its own panel have at least seven-to-one source contrast");
        File.AppendAllText(Path.Combine(Output, "first-minute-feedback.txt"), "frame=" + Time.frameCount + " font=" + feedback.fontSize
            + " inheritedAlpha=" + feedback.canvasRenderer.GetInheritedAlpha() + " text=" + feedback.text.Replace("\n", " | ") + "\n");
    }

    static float FirstMinuteLuminance(Color color)
    {
        return .2126f * Mathf.GammaToLinearSpace(color.r) + .7152f * Mathf.GammaToLinearSpace(color.g) + .0722f * Mathf.GammaToLinearSpace(color.b);
    }

    static void CheckFirstMinuteEnglishLabels(XingGuangController controller, bool answered)
    {
        Check(GameText.IsEnglish && Lang.English && controller.Sim.English,
            "Actual first-minute labels remain bound to English");
        var page = controller.View.Teach;
        var question = FirstMinuteField<TMP_Text>(page, "text");
        var guess = FirstMinuteField<TMP_Text>(page, "suggestion");
        var yes = FirstMinuteField<XgBtn>(page, "yes").label;
        var no = FirstMinuteField<XgBtn>(page, "no").label;
        if (!answered)
        {
            Check(question.text == "Beijing is the capital of China.", "Rendered card uses its authored English question");
            Check(guess.text.Contains("guesses: No"), "Rendered first guess explicitly says No in English");
            CheckFirstMinuteEnglishTextFits(question, "English question");
            CheckFirstMinuteEnglishTextFits(guess, "English shown guess");
        }
        Check(yes.text.StartsWith("Guessed right", StringComparison.Ordinal) && no.text.StartsWith("Guessed wrong", StringComparison.Ordinal),
            "Actual answer buttons identify evaluation of the guess in English");
        CheckFirstMinuteEnglishTextFits(yes, "English Guessed right button");
        CheckFirstMinuteEnglishTextFits(no, "English Guessed wrong button");
        var companion = controller.View.GetComponentInChildren<XgTeachCompanion>(true);
        var chat = FirstMinuteField<XgBtn>(companion, "chat");
        var board = FirstMinuteField<XgBtn>(companion, "brain");
        var train = FirstMinuteField<XgBtn>(companion, "train");
        Check(chat.label.text == "Talk to it →" && board.label.text == "Look inside its brain" && train.label.text == "Train →",
            "Existing companion buttons refresh their actual text after switching the live locale to English");
        Check(chat.button.interactable && board.button.interactable,
            "English companion localization preserves optional chat and brain navigation");
        CheckFirstMinuteEnglishTextFits(chat.label, "English companion chat button");
        CheckFirstMinuteEnglishTextFits(board.label, "English companion brain button");
        CheckLessonGoalState(controller, answered ? 1 : 0);
        if (train.rt.gameObject.activeInHierarchy)
            CheckFirstMinuteEnglishTextFits(train.label, "English companion train button");
        else
            Check(!train.button.IsInteractable(), "English training action stays hidden and unavailable before four examples");
        if (!answered) return;
        var feedback = FirstMinuteField<TMP_Text>(page, "feedback");
        Check(feedback.text.StartsWith("Last card: you corrected it\nCorrect answer: Yes", StringComparison.Ordinal)
            && !feedback.text.Contains("上一题") && !feedback.text.Contains("正确答案"),
            "Actual correction feedback names Last card and its English Yes answer");
        Check(feedback.canvasRenderer.GetInheritedAlpha() > .99f, "English feedback inherits full desktop opacity");
        CheckFirstMinuteEnglishTextFits(feedback, "English last-card feedback");
    }

    static bool FirstMinuteScreenshotWritten(string path)
    {
        try
        {
            if (!File.Exists(path)) return false;
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length < 24) return false;
                foreach (byte b in new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                    if (stream.ReadByte() != b) return false;
                stream.Seek(-12, SeekOrigin.End);
                foreach (byte b in new byte[] { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 })
                    if (stream.ReadByte() != b) return false;
                return true;
            }
        }
        catch (IOException) { return false; }
    }

    static void CheckFirstMinuteEnglishTextFits(TMP_Text label, string name)
    {
        label.ForceMeshUpdate();
        Check(label.isActiveAndEnabled && !label.isTextOverflowing && label.textInfo.characterCount > 0,
            name + " is active and has rendered text without overflow");
        var rect = label.rectTransform.rect;
        bool fits = true;
        for (int i = 0; i < label.textInfo.characterCount; i++)
        {
            var character = label.textInfo.characterInfo[i];
            if (!character.isVisible) continue;
            fits &= character.bottomLeft.x >= rect.xMin - 1 && character.topRight.x <= rect.xMax + 1
                && character.bottomLeft.y >= rect.yMin - 1 && character.topRight.y <= rect.yMax + 1;
        }
        File.AppendAllText(Path.Combine(Output, "first-minute-english-layout.txt"), name + " rect=" + rect.size
            + " bounds=" + label.textBounds.size + " font=" + label.fontSize + " lines=" + label.textInfo.lineCount
            + " text=" + label.GetParsedText().Replace("\n", " | ") + "\n");
        Check(fits, name + " visible glyphs fit its actual width and height");
    }
}
