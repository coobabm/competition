using System;
using System.Collections;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using LingGuangV05.Core.Story;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Runtime;
using LingGuangV05.Runtime.Story;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using AppNames = LingGuangV05.Core.AppNames;

namespace LingGuangV05.Desktop.Story
{
    /// <summary>
    /// Desktop presentation for the story runner: narration subtitles (the 2026 voice, drawn over the monitor),
    /// toasts, chapter title cards, the emergence cutscene, 老周 messages in the native YY conversation and
    /// desktop signals (app opened, chat sent). Built at runtime; never edits native DreamOS views.
    /// Photosensitivity: no flashes; every large brightness change takes at least 0.3 s.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StoryDesktopPresenter : MonoBehaviour, IStoryOutput
    {
        public const string ReduceMotionPref = "LingGuangV05.ReduceMotion";
        private const string LaoZhou = "老周";

        public StoryDirector director;
        public ChapterOneDesktopRouter router;
        [Tooltip("Characters per second for typewriter text.")]
        public float typeSpeed = 22;

        private Canvas canvas;
        private TMP_FontAsset font;
        private RectTransform root;
        private CanvasGroup narrationGroup, toastGroup, cutsceneGroup;
        private TextMeshProUGUI narrationText, toastText;
        private Button toastButton;
        private RectTransform cutsceneRoot;
        private Image cutsceneBackdrop;
        private Coroutine narrationRoutine, toastRoutine, cutsceneRoutine;
        private Action narrationDone, cutsceneDone;
        private bool skipRequested;
        private readonly Queue<KeyValuePair<Func<string>, Action>> toasts = new Queue<KeyValuePair<Func<string>, Action>>();
        private readonly List<WindowManager> hookedWindows = new List<WindowManager>();
        private readonly List<UnityEngine.Events.UnityAction> windowHandlers = new List<UnityEngine.Events.UnityAction>();
        // Keep authored sources, not the last rendered language. Locale changes only update views;
        // they never restart a coroutine or advance the story runner.
        private sealed class LocalizedLabel
        {
            public Func<string> Render;
            public float Reveal = 1;
        }
        private readonly Dictionary<TMP_Text, LocalizedLabel> localizedLabels = new Dictionary<TMP_Text, LocalizedLabel>();
        private readonly List<TMP_Text> expiredLabels = new List<TMP_Text>();

        // YY channel (native MessagingManager, no adapter changes)
        private ChapterOneNativeChatAdapter chatAdapter;
        private MessagingManager chatManager;
        private ChatLayoutPreset laoZhouLayout;
        private struct PendingChat { public string who, text, beat; public float delay; public LingGuangV05.Core.ChapterOneSim simulation; }
        private readonly Queue<PendingChat> chatQueue = new Queue<PendingChat>();
        private float chatDueAt = -1;
        private bool chatHooked;

        public bool ReduceMotion { get { return PlayerPrefs.GetInt(ReduceMotionPref, 0) == 1; } }
        public bool CutscenePlaying { get { return cutsceneRoutine != null; } }
        /// <summary>Shares the established cutscene/prologue/hologram gate with desktop message delivery.</summary>
        public bool NotificationsHeld => InnerVoiceHeld();

        PrologueDirector prologueForVoice;
        LingGuangV05.Desktop.XingGuang.XgHoloCard holoForVoice;
        float nextHoloLookup;

        bool InnerVoiceHeld()
        {
            if (this == null) return false;
            if (CutscenePlaying || LingGuangV05.Desktop.XingGuang.LoveQuestionCutscene.Playing || LingGuangV05.Desktop.XingGuang.XgEmergenceCutscene.Playing) return true;
            if (prologueForVoice == null) prologueForVoice = GetComponent<PrologueDirector>();
            if (prologueForVoice != null && prologueForVoice.Running) return true;
            if (holoForVoice == null && Time.unscaledTime >= nextHoloLookup)
            {
                nextHoloLookup = Time.unscaledTime + 1f;
                holoForVoice = FindAnyObjectByType<LingGuangV05.Desktop.XingGuang.XgHoloCard>();
            }
            return holoForVoice != null && holoForVoice.Showing;
        }

        private void OnEnable() { GameText.Changed += RefreshLocalizedText; RefreshLocalizedText(); }
        private void OnDisable() { GameText.Changed -= RefreshLocalizedText; }

        private void Start()
        {
            if (director == null) director = GetComponent<StoryDirector>();
            if (router == null) router = GetComponent<ChapterOneDesktopRouter>();
            BuildOverlay();
            HookDesktop();
            // The prologue (design v1.1 §6) lives next to the story host; it only plays on a new game.
            if (GetComponent<PrologueDirector>() == null) gameObject.AddComponent<PrologueDirector>();
            // 摆渡贴吧 (design v1.1 §3): the forum and 老周's private messages, in the desktop's spare native window.
            var tieba = GetComponent<LingGuangV05.Desktop.Tieba.TiebaHub>() ?? gameObject.AddComponent<LingGuangV05.Desktop.Tieba.TiebaHub>();
            LingGuangV05.Desktop.Tieba.TiebaView.Install(tieba);
            // 摆渡 takes the spare Widget Library window; 游戏中心 (五子棋, 围棋, 国际象棋) fills the native Game Hub (design v1.1 §3).
            LingGuangV05.Desktop.Bodu.BoduView.Install();
            LingGuangV05.Desktop.Casino.CasinoDesktop.Install(this);
            LingGuangV05.Desktop.Games.GameHubView.Install();
            // 淘货 takes over the old 寻宝 window; 喵鱼 fills the spare native Commander window (design v1.1 §3, §14.3).
            LingGuangV05.Desktop.Taohuo.TaohuoView.Install(router);
            LingGuangV05.Desktop.Miaoyu.MiaoyuView.Install();
            // The music player's 2016 playlist (the player's own audio files; none are shipped).
            StartCoroutine(Music2016.Install());
            // The 2016 tray crowd next to the clock, and the stage-6 flicker (design v1.1 §3, §10.2 #8).
            TrayApps.Install();
            UniverseFlicker.Install(gameObject);
            // The to-do sticky note: what to do next, with a ring on where to click.
            GuideNote.Install(this);
            // Inner-voice lines wait while a story cutscene, the prologue or a flash card covers the screen.
            InnerVoice.Hold = InnerVoiceHeld;
            // The curtain call after the ending: 「你是怎么被训练出来的？」 and the evolution video.
            OriginCurtain.Install(this);
            // The protagonist's own idea that the model could label for them: thoughts, then the forum cutscene.
            AutoLabelEpiphany.Install(this);
            // Right after its setup (stage 1): the AI reads YY's chat history and joins YY as a contact (design 女友系统与YY里的AI §5).
            AiJoinsYy.Install(this);
            // Stage 5: 「她……爱我吗？」, the AI reads the chats with her and answers 是 or 否 (女友系统 §6).
            LingGuangV05.Desktop.XingGuang.LoveQuestionCutscene.Install(this);
            // 鲁大师 benchmarks every new card (§3 tray widgets).
            if (director != null && director.runtime != null) director.runtime.Signal += OnRuntimeSignal;
            if (director != null)
            {
                director.Rebound += OnStoryRebound;
                director.ExternalBusy = () => CutscenePlaying || AutoLabelEpiphany.Playing || AiJoinsYy.Playing || LingGuangV05.Desktop.XingGuang.LoveQuestionCutscene.Playing || LingGuangV05.Desktop.XingGuang.XgEmergenceCutscene.Playing || LingGuangV05.Desktop.XingGuang.XgResidentSpider.IntroPlaying;
                director.SetOutput(this);
            }
        }

        private void OnRuntimeSignal(string signal, string arg)
        {
            if (signal == "gpu.bought")
            {
                // The score depends on the card: 98% for a new card, 99.7% for a GTX 1080 (§14.3).
                string score = LingGuangV05.Core.Hardware.HardwareCatalog.LudashiPercent(arg).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
                string zh = arg == "nvme" ? "硬盘跑分完成：读写速度战胜了全国 99% 的用户！" : "显卡跑分完成：战胜了全国 " + score + "% 的用户！";
                string en = arg == "nvme" ? "Disk benchmark done: faster than 99% of users nationwide!" : "GPU benchmark done: faster than " + score + "% of users nationwide!";
                if (PrologueDirector.Desk != null) PrologueDirector.Desk.Popup(Lang.T("鲁大师"), GameText.T(zh, en), 8);
                else EnqueueToast(() => GameText.T("鲁大师\n" + zh, "Master Lu\n" + en), null);
            }
        }

        private void OnDestroy()
        {
            if (director != null && director.runtime != null) director.runtime.Signal -= OnRuntimeSignal;
            GameText.Changed -= RefreshLocalizedText;
            for (int i = 0; i < hookedWindows.Count; i++)
                if (hookedWindows[i] != null) hookedWindows[i].onOpen.RemoveListener(windowHandlers[i]);
            if (chatManager != null && chatHooked) chatManager.externalEvents.RemoveListener(OnPlayerSend);
            if (director != null) { director.Rebound -= OnStoryRebound; director.SetOutput(null); }
            if (canvas != null) Destroy(canvas.gameObject);
            localizedLabels.Clear();
        }

        // ------------------------------------------------------------------ IStoryOutput

        public void Execute(StoryCommand command, Action done)
        {
            switch (command.Op)
            {
                case "narrate": Narrate(command.Text, done); break;
                case "cutscene": PlayCutscene(command, done); break;
                case "say": Say(command.Arg("who", LaoZhou), command.Text, (float)command.DelaySeconds, command.BeatId); break;
                case "think": InnerVoice.Say(command.Text, command.Text); break;
                case "notify":
                    string app = command.Arg("app"), tab = command.Arg("tab");
                    ShowToast(command.Text, app == null ? (Action)null : () => OpenApp(app, tab));
                    break;
                case "open": OpenApp(command.Arg("app"), command.Arg("tab")); break;
                case "unlock": break; // flag already written by the runner; apps read it when gating arrives
                default: if (done != null) done(); break;
            }
        }

        // ------------------------------------------------------------------ desktop hooks

        private void HookDesktop()
        {
            if (router == null) return;
            foreach (var binding in router.Bindings)
            {
                if (binding == null || binding.Window == null) continue;
                string id = binding.ApplicationId;
                UnityEngine.Events.UnityAction handler = () => Emit("app.open", id);
                binding.Window.onOpen.AddListener(handler);
                hookedWindows.Add(binding.Window);
                windowHandlers.Add(handler);
                if (binding.NativeChat != null) chatAdapter = binding.NativeChat;
            }
        }

        private void Emit(string signal, string arg)
        {
            if (director != null) director.Emit(signal, arg);
        }

        private void OpenApp(string app, string tab)
        {
            if (router != null && !string.IsNullOrEmpty(app)) router.Open(app, tab);
        }

        private bool EnsureChat()
        {
            if (laoZhouLayout != null && chatManager != null) return true;
            if (chatAdapter == null || chatAdapter.Manager == null) return false;
            chatManager = chatAdapter.Manager;
            if (!chatHooked) { chatManager.externalEvents.AddListener(OnPlayerSend); chatHooked = true; }
            if (chatManager.createdLayoutPresets == null) return false;
            for (int i = 0; i < chatManager.createdLayoutPresets.Count; i++)
                if (chatManager.createdLayoutPresets[i] != null && chatManager.createdLayoutPresets[i].name == LaoZhou)
                { laoZhouLayout = chatManager.createdLayoutPresets[i]; break; }
            return laoZhouLayout != null && laoZhouLayout.messageParent != null;
        }

        private void OnPlayerSend()
        {
            if (chatManager != null && chatManager.selectedLayout == laoZhouLayout) Emit("chat.sent", "laozhou");
        }

        private void Say(string who, string text, float delay, string beat)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (who == LaoZhou) who = "laozhou";
            if (chatQueue.Count == 0) chatDueAt = Time.unscaledTime + delay;
            chatQueue.Enqueue(new PendingChat { who = who, text = text, delay = delay, beat = beat, simulation = director != null && director.runtime != null ? director.runtime.Sim : null });
        }

        private void NoteChatDelivered(string beat)
        {
            if (director != null && director.runtime != null && !string.IsNullOrEmpty(beat))
                director.runtime.RaiseSignal("story.delivered", beat);
        }

        private void UpdateChat()
        {
            while (chatQueue.Count > 0 && (director == null || director.runtime == null || !ReferenceEquals(chatQueue.Peek().simulation, director.runtime.Sim))) chatQueue.Dequeue();
            if (chatQueue.Count == 0 || Time.unscaledTime < chatDueAt) return;
            var pendingChat = chatQueue.Dequeue();
            if (pendingChat.beat != null && pendingChat.beat.StartsWith("era_", StringComparison.Ordinal) && director != null && director.runtime != null)
            {
                var lab = director.runtime.GetComponent<LingGuangV05.Desktop.XingGuang.XingGuangController>();
                if (lab != null && lab.Sim != null && (lab.Sim.S.chapterComplete || !string.IsNullOrEmpty(lab.Sim.S.ending))) return;
            }
            string text = pendingChat.text;
            if (chatQueue.Count > 0) chatDueAt = Time.unscaledTime + chatQueue.Peek().delay;
            var yy = LingGuangV05.Desktop.YY.YYChatHub.Instance;
            if (yy != null && yy.S != null && yy.Conversation(pendingChat.who) != null)
            {
                yy.Receive(pendingChat.who, GameText.Source(text));
                NoteChatDelivered(pendingChat.beat);
                return;
            }
            if (pendingChat.who == "zhou_now")
            {
                var forum = LingGuangV05.Desktop.Tieba.TiebaHub.Instance;
                if (forum != null && forum.S != null)
                {
                    forum.Receive(LingGuangV05.Core.Forum.ForumLibrary.ZhouNow, GameText.Source(text));
                    NoteChatDelivered(pendingChat.beat); return;
                }
            }
            if (pendingChat.who != "laozhou")
            {
                ShowToast(GameText.Source(text), null);
                NoteChatDelivered(pendingChat.beat);
                return;
            }
            bool posted = false;
            if (EnsureChat())
            {
                try
                {
                    string time = chatManager.GetTimeData() ?? DateTime.Now.ToString("HH:mm");
                    int before = laoZhouLayout.messageParent.childCount;
                    // The native popup font has no CJK glyphs; use the story toast instead.
                    bool nativePopups = chatManager.useNotifications;
                    chatManager.useNotifications = false;
                    try { chatManager.CreateCustomIndividualMessage(laoZhouLayout, GameText.Source(text), time); }
                    finally { chatManager.useNotifications = nativePopups; }
                    // Native bubbles lack CJK glyphs; reuse the adapter's fallback styling on the new bubble only.
                    for (int i = before; i < laoZhouLayout.messageParent.childCount; i++)
                    {
                        var message = laoZhouLayout.messageParent.GetChild(i).GetComponent<ChatMessagePreset>();
                        if (message != null && message.contentText != null) SetText(message.contentText, text);
                        foreach (var label in laoZhouLayout.messageParent.GetChild(i).GetComponentsInChildren<TMP_Text>(true))
                        { label.richText = false; chatAdapter.ApplyNativeFont(label); }
                    }
                    posted = true;
                }
                catch (Exception e) { Debug.LogWarning("[Story] YY post failed: " + e.Message); }
            }
            bool visible = posted && DesktopNotifications.IsWindowVisible(chatAdapter.Window) && chatManager.selectedLayout == laoZhouLayout;
            DesktopNotifications.Notify(director != null ? director.runtime : null, "YY", Lang.T("老周"),
                GameText.Source(text), visible, () => OpenApp("yy", null));
            NoteChatDelivered(pendingChat.beat);
        }

        // ------------------------------------------------------------------ frame

        private void Update()
        {
            if (director != null && !director.Active)
            {
                if (cutsceneRoutine != null || narrationRoutine != null) HideAll();
                return;
            }
            UpdateChat();
            HandleKeys();
            DeliverEraTray();
        }

        // ------------------------------------------------------------------ 2016 tray bubbles (era_events.json)

        private float trayWait;
        private readonly Dictionary<string, float> trayRepeatAt = new Dictionary<string, float>();

        /// <summary>360, 迅雷 and the Windows 10 offer speak up when the calendar reaches their day; the offer keeps coming back until 29 July.</summary>
        private void DeliverEraTray()
        {
            trayWait -= Time.unscaledDeltaTime;
            if (trayWait > 0 || CutscenePlaying) return;
            trayWait = 3;
            var runtime = director != null ? director.runtime : null;
            if (runtime == null || runtime.Sim == null || runtime.TestMode) return;
            var save = runtime.Sim.S;
            var e = EraContent.TakeDue(save, EraEvents.Tray);
            if (e == null)
                foreach (var repeating in EraContent.Events.Repeating(EraEvents.Tray, GameCalendar.Now(save)))
                {
                    trayRepeatAt.TryGetValue(repeating.id, out float at);
                    if (Time.unscaledTime < at) continue;
                    trayRepeatAt[repeating.id] = Time.unscaledTime + (float)repeating.repeatMinutes * 60;
                    e = repeating;
                    break;
                }
            if (e == null) return;
            var shown = e;
            // Tray software speaks from the bottom-right corner of the monitor, like the real thing.
            if (PrologueDirector.Desk != null) PrologueDirector.Desk.Popup(EraContent.Who(shown), EraContent.Text(shown), 8);
            else EnqueueToast(() => EraContent.Who(shown) + "\n" + EraContent.Text(shown), null);
            trayWait = 30;
        }

        private void HandleKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame && cutsceneRoutine != null) skipRequested = true;
            if (clickSkips && cutsceneRoutine != null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) skipRequested = true;
            if ((keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) && narrationRoutine != null && cutsceneRoutine == null)
                skipRequested = true;
            if (!(Application.isEditor || Debug.isDebugBuild) || director == null) return;
            if (keyboard.f6Key.wasPressedThisFrame) { skipRequested = true; if (director.Runner != null) director.Runner.SkipWait(); }
            if (keyboard.f7Key.wasPressedThisFrame && cutsceneRoutine == null)
                cutsceneRoutine = StartCoroutine(Emerge("prologue", "……这题问得不对。", null));
            if (keyboard.f8Key.wasPressedThisFrame) Debug.Log("[Story]\n" + director.ExplainAll());
            if (keyboard.f9Key.wasPressedThisFrame)
            {
                director.TimeScale = director.TimeScale > 1.5f ? 1 : 10;
                string speed = director.TimeScale.ToString("0");
                EnqueueToast(() => GameText.F("剧情时间 x{0}", "Story speed x{0}", speed), null);
            }
        }

        private void OnStoryRebound(StoryDirector source)
        {
            chatQueue.Clear(); chatDueAt = -1;
            toasts.Clear();
            if (toastRoutine != null) { StopCoroutine(toastRoutine); toastRoutine = null; }
            if (toastGroup != null) { toastGroup.alpha = 0; toastGroup.blocksRaycasts = false; }
            HideAll();
        }

        private void HideAll()
        {
            if (cutsceneRoutine != null) { StopCoroutine(cutsceneRoutine); cutsceneRoutine = null; }
            if (narrationRoutine != null) { StopCoroutine(narrationRoutine); narrationRoutine = null; }
            ClearCutscene();
            if (cutsceneGroup != null) { cutsceneGroup.alpha = 0; cutsceneGroup.blocksRaycasts = false; }
            if (narrationGroup != null) narrationGroup.alpha = 0;
            narrationDone = null; cutsceneDone = null;
        }

        // ------------------------------------------------------------------ narration

        private void Narrate(string text, Action done)
        {
            if (narrationRoutine != null) { StopCoroutine(narrationRoutine); Complete(ref narrationDone); }
            narrationDone = done;
            narrationRoutine = StartCoroutine(NarrationRoutine(text ?? ""));
        }

        private IEnumerator NarrationRoutine(string text)
        {
            skipRequested = false;
            SetText(narrationText, text);
            SetReveal(narrationText, ReduceMotion ? 1 : 0);
            yield return Fade(narrationGroup, 1, .35f);
            if (!ReduceMotion)
            {
                float shown = 0;
                while (shown < text.Length && !skipRequested)
                {
                    shown += Time.unscaledDeltaTime * typeSpeed;
                    SetReveal(narrationText, text.Length == 0 ? 1 : shown / text.Length);
                    yield return null;
                }
                SetReveal(narrationText, 1);
                if (skipRequested) { skipRequested = false; yield return null; }
            }
            float hold = Mathf.Max(2.5f, text.Length * .09f);
            while (hold > 0 && !skipRequested) { hold -= Time.unscaledDeltaTime; yield return null; }
            skipRequested = false;
            yield return Fade(narrationGroup, 0, .4f);
            narrationRoutine = null;
            Complete(ref narrationDone);
        }

        // ------------------------------------------------------------------ toast

        public void ShowToast(string text, Action onClick)
        {
            if (string.IsNullOrEmpty(text)) return;
            var runtime = director != null ? director.runtime : GetComponent<ChapterOneRuntime>();
            DesktopNotifications.Notify(runtime, Lang.T("桌面消息"), "", GameText.Source(text), false, onClick);
        }

        private void EnqueueToast(Func<string> render, Action onClick)
        {
            if (render == null || toastGroup == null) return;
            toasts.Enqueue(new KeyValuePair<Func<string>, Action>(render, onClick));
            if (toastRoutine == null) toastRoutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            while (toasts.Count > 0)
            {
                var item = toasts.Dequeue();
                SetText(toastText, item.Key);
                Action click = item.Value;
                toastButton.onClick.RemoveAllListeners();
                toastButton.onClick.AddListener(() => { if (click != null) click(); toastGroup.alpha = 0; });
                toastGroup.blocksRaycasts = true;
                yield return Fade(toastGroup, 1, .3f);
                float hold = 4f;
                while (hold > 0 && toastGroup.alpha > 0) { hold -= Time.unscaledDeltaTime; yield return null; }
                yield return Fade(toastGroup, 0, .3f);
                toastGroup.blocksRaycasts = false;
            }
            toastRoutine = null;
        }

        // ------------------------------------------------------------------ cutscenes

        private void PlayCutscene(StoryCommand command, Action done)
        {
            if (cutsceneRoutine != null) { StopCoroutine(cutsceneRoutine); ClearCutscene(); Complete(ref cutsceneDone); }
            cutsceneDone = done;
            clickSkips = false;
            string name = command.Arg("name", "");
            switch (name)
            {
                case "Title":
                    cutsceneRoutine = StartCoroutine(TitleCard(command.Text, LineText(command.Arg("subKey"))));
                    break;
                case "TimeSkip":
                    int stage;
                    if (!int.TryParse(command.Arg("stage", "1"), out stage)) stage = 1;
                    cutsceneRoutine = StartCoroutine(TimeSkip(stage, command.Text ?? "", LineText(command.Arg("subKey"))));
                    break;
                case "Emerge":
                    cutsceneRoutine = StartCoroutine(Emerge(command.Arg("mode", "prologue"), command.Text ?? "……这题问得不对。", command.Arg("cardsKey", "emerge_prologue_cards")));
                    break;
                default:
                    Debug.LogWarning("[Story] unknown cutscene " + name);
                    Complete(ref cutsceneDone);
                    break;
            }
        }

        private string LineText(string key)
        {
            string text;
            if (key == null || director == null || director.Library == null || !director.Library.TryGetLine(key, 0, out text)) return "";
            return StoryText.Format(text, director.ResolveToken);
        }

        private List<string> LineVariants(string key)
        {
            var list = new List<string>();
            if (key == null || director == null || director.Library == null) return list;
            string text;
            for (int i = 0; i < 16 && director.Library.TryGetLine(key, i, out text); i++)
            {
                if (i > 0 && text == list[0]) break;
                list.Add(text);
            }
            return list;
        }

        private void FinishCutscene()
        {
            ClearCutscene();
            cutsceneGroup.blocksRaycasts = false;
            cutsceneRoutine = null;
            Complete(ref cutsceneDone);
        }

        private IEnumerator TitleCard(string title, string subtitle)
        {
            skipRequested = false;
            cutsceneGroup.blocksRaycasts = true;
            cutsceneBackdrop.color = new Color(0, 0, 0, 1);
            var titleText = Label(cutsceneRoot, title, 64, new Vector2(0, 40), new Vector2(1400, 100), new Color(.92f, .94f, .97f, 0));
            var dateText = Label(cutsceneRoot, subtitle, 34, new Vector2(0, -40), new Vector2(1400, 60), new Color(.56f, .72f, .87f, 1));
            SetReveal(dateText, 0);
            yield return Fade(cutsceneGroup, 1, .6f);
            for (float t = 0; t < .5f && !skipRequested; t += Time.unscaledDeltaTime) { titleText.alpha = t / .5f; yield return null; }
            titleText.alpha = 1;
            for (int i = 0; i <= subtitle.Length && !skipRequested; i++)
            {
                SetReveal(dateText, subtitle.Length == 0 ? 1 : (float)i / subtitle.Length);
                yield return Wait(ReduceMotion ? 0 : .08f);
            }
            SetReveal(dateText, 1);
            yield return Wait(1.4f);
            yield return Fade(cutsceneGroup, 0, .8f);
            FinishCutscene();
        }

        private bool clickSkips;

        /// <summary>
        /// The month turns (design v1.1 decision D2): the last calendar page of the old month tears off and the first
        /// day of the stage's month shows, then the stage, its idea and up to three of the month's headlines.
        /// Click or Esc skips.
        /// </summary>
        private IEnumerator TimeSkip(int stage, string title, string subtitle)
        {
            skipRequested = false;
            clickSkips = true;
            cutsceneGroup.blocksRaycasts = true;
            cutsceneBackdrop.color = new Color(.02f, .03f, .05f, 1);
            DateTime from = stage <= 1 ? GameCalendar.Start.Date : GameCalendar.LastDay(stage - 1);
            DateTime to = GameCalendar.FirstDay(stage);

            var page = Panel(cutsceneRoot, new Vector2(0, 170), new Vector2(300, 330), new Color(.96f, .95f, .91f, 1));
            var pageGroup = page.gameObject.AddComponent<CanvasGroup>();
            var band = Panel(page, new Vector2(0, 130), new Vector2(300, 70), new Color(.74f, .15f, .15f, 1));
            var month = Label(band, "", 30, Vector2.zero, new Vector2(280, 60), Color.white);
            var day = Label(page, "", 150, new Vector2(0, -15), new Vector2(280, 190), new Color(.12f, .12f, .14f, 1));
            var week = Label(page, "", 26, new Vector2(0, -130), new Vector2(280, 40), new Color(.42f, .42f, .46f, 1));
            ShowDate(month, day, week, from);
            var hint = Label(cutsceneRoot, "点击或按 Esc 跳过", 18, new Vector2(0, -500), new Vector2(600, 30), new Color(.5f, .55f, .62f, 1));
            yield return Fade(cutsceneGroup, 1, ReduceMotion ? .2f : .5f);
            yield return Wait(.7f);

            // The old page tears off and falls.
            for (float t = 0; t < .55f && !skipRequested && !ReduceMotion; t += Time.unscaledDeltaTime)
            {
                page.anchoredPosition = new Vector2(t * 90, 170 - t * t * 1400);
                page.localRotation = Quaternion.Euler(0, 0, -t * 40);
                pageGroup.alpha = 1 - t / .55f;
                yield return null;
            }
            page.anchoredPosition = new Vector2(0, 170);
            page.localRotation = Quaternion.identity;
            ShowDate(month, day, week, to);
            for (float t = 0; t < .35f && !skipRequested; t += Time.unscaledDeltaTime) { pageGroup.alpha = t / .35f; yield return null; }
            pageGroup.alpha = 1;

            var titleText = Label(cutsceneRoot, title, 52, new Vector2(0, -60), new Vector2(1400, 80), new Color(.92f, .94f, .97f, 0));
            var subText = Label(cutsceneRoot, subtitle, 30, new Vector2(0, -125), new Vector2(1400, 50), new Color(.56f, .72f, .87f, 1));
            SetReveal(subText, 0);
            for (float t = 0; t < .4f && !skipRequested; t += Time.unscaledDeltaTime) { titleText.alpha = t / .4f; yield return null; }
            titleText.alpha = 1;
            for (int i = 0; i <= subtitle.Length && !skipRequested; i++)
            {
                SetReveal(subText, subtitle.Length == 0 ? 1 : (float)i / subtitle.Length);
                yield return Wait(ReduceMotion ? 0 : .05f);
            }
            SetReveal(subText, 1);

            // This month's front page: headlines with a story, not the one-line political or obituary items.
            var headlines = new List<EraEvent>();
            foreach (var e in EraContent.Events.InMonth(EraEvents.News, to.Year, to.Month)) if (!e.brief && headlines.Count < 3) headlines.Add(e);
            float y = -195;
            foreach (var e in headlines)
            {
                var shown = e;
                var line = Label(cutsceneRoot, "", 24, new Vector2(0, y), new Vector2(1300, 36), new Color(.78f, .8f, .84f, 0));
                SetText(line, () => "· " + EraContent.Title(shown));
                for (float t = 0; t < .3f && !skipRequested; t += Time.unscaledDeltaTime) { line.alpha = t / .3f; yield return null; }
                line.alpha = 1;
                y -= 42;
                yield return Wait(.25f);
            }
            yield return Wait(2.2f);
            hint.alpha = 0;
            yield return Fade(cutsceneGroup, 0, .6f);
            clickSkips = false;
            FinishCutscene();
        }

        private void ShowDate(TMP_Text month, TMP_Text day, TMP_Text week, DateTime date)
        {
            SetText(month, () => GameText.IsEnglish ? date.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture) : date.Year + " 年 " + date.Month + " 月");
            SetText(day, () => date.Day.ToString(System.Globalization.CultureInfo.InvariantCulture));
            string[] zh = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
            SetText(week, () => GameText.IsEnglish ? date.DayOfWeek.ToString() : zh[(int)date.DayOfWeek]);
        }

        /// <summary>
        /// The emergence. Prologue mode: the player answers a few yes/no cards, then an ill-posed card breaks
        /// the frame: buttons tremble, labels scramble, and a sentence replaces them; a chat window glows open.
        /// </summary>
        private IEnumerator Emerge(string mode, string sentence, string cardsKey)
        {
            skipRequested = false;
            bool reduce = ReduceMotion;
            cutsceneGroup.blocksRaycasts = true;
            cutsceneBackdrop.color = new Color(.02f, .03f, .05f, .96f);
            var cards = LineVariants(cardsKey);
            if (cards.Count == 0) cards.AddRange(new[] { "“我真是谢谢你了”是在夸人吗？", "17 × 3 = 51？", "这句话是假的。" });
            string glitchPool = LineText("emerge_glitch_chars");
            if (string.IsNullOrEmpty(glitchPool)) glitchPool = "是否对错真假有无";

            var header = Label(cutsceneRoot, mode == "prologue" ? GameText.T(AppNames.ExeZh + " · 2026 年 · 回忆", AppNames.ExeEn + " · 2026 · A memory") : AppNames.ExeZh, 24,
                new Vector2(0, 300), new Vector2(900, 40), new Color(.56f, .72f, .87f, .9f));
            var card = Panel(cutsceneRoot, new Vector2(0, 40), new Vector2(860, 420), new Color(.055f, .1f, .17f, 1));
            Panel(card, new Vector2(0, 208), new Vector2(860, 4), new Color(.31f, .76f, .97f, .8f));
            var prompt = Label(card, "", 40, new Vector2(0, 70), new Vector2(760, 160), new Color(.92f, .94f, .97f, 1));
            var confidence = Label(card, "", 22, new Vector2(0, -20), new Vector2(760, 40), new Color(1f, .82f, .48f, 1));
            var buttonArea = new GameObject("Buttons", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            buttonArea.SetParent(card, false);
            buttonArea.sizeDelta = new Vector2(760, 110);
            buttonArea.anchoredPosition = new Vector2(0, -130);
            var buttonsGroup = buttonArea.GetComponent<CanvasGroup>();
            int answered = -1;
            var yes = ChoiceButton(buttonArea, "是", new Vector2(-170, 0), new Color(.18f, .45f, .62f, 1), () => answered = 1);
            var no = ChoiceButton(buttonArea, "否", new Vector2(170, 0), new Color(.29f, .35f, .45f, 1), () => answered = 0);
            var spoken = Label(card, "", 44, new Vector2(0, -120), new Vector2(780, 120), new Color(.92f, .94f, .97f, 1));
            var hint = Label(cutsceneRoot, "点“是”或“否”回答 · Esc 跳过", 20, new Vector2(0, -230), new Vector2(900, 30), new Color(.6f, .65f, .72f, .8f));

            yield return Fade(cutsceneGroup, 1, .6f);
            var rng = new System.Random(20101023);
            for (int i = 0; i < cards.Count - 1 && !skipRequested; i++)
            {
                SetText(prompt, cards[i]);
                int confidenceValue = 51 + rng.Next(0, 40);
                SetText(confidence, () => GameText.F("信心 {0}%", "Confidence {0}%", confidenceValue));
                answered = -1;
                buttonsGroup.interactable = true;
                for (float t = 0; answered < 0 && t < 10f && !skipRequested; t += Time.unscaledDeltaTime) yield return null;
                buttonsGroup.interactable = false;
                yield return Wait(.25f);
            }
            SetText(hint, "");
            if (!skipRequested)
            {
                // The ill-posed card. The frame of yes/no breaks.
                SetText(prompt, cards[cards.Count - 1]);
                SetText(confidence, () => GameText.F("信心 {0}%", "Confidence {0}%", 51));
                buttonsGroup.interactable = false;
                yield return Wait(1.2f);
                var yesRect = (RectTransform)yes.transform;
                var noRect = (RectTransform)no.transform;
                Vector2 yesHome = yesRect.anchoredPosition, noHome = noRect.anchoredPosition;
                if (!reduce)
                {
                    // Fine tremble: ±2 px, under 3 position changes per second per axis... smooth sine at 2.5 Hz.
                    for (float t = 0; t < 1.5f && !skipRequested; t += Time.unscaledDeltaTime)
                    {
                        float dx = Mathf.Sin(t * Mathf.PI * 2 * 2.5f) * 2f, dy = Mathf.Cos(t * Mathf.PI * 2 * 1.7f) * 1.5f;
                        yesRect.anchoredPosition = yesHome + new Vector2(dx, dy);
                        noRect.anchoredPosition = noHome + new Vector2(-dx, dy);
                        yield return null;
                    }
                    yesRect.anchoredPosition = yesHome; noRect.anchoredPosition = noHome;
                    // Labels scramble every 0.1 s (text change only, no brightness change).
                    var yesLabel = yes.GetComponentInChildren<TextMeshProUGUI>();
                    var noLabel = no.GetComponentInChildren<TextMeshProUGUI>();
                    for (float t = 0; t < 1f && !skipRequested; t += .1f)
                    {
                        int yesIndex = rng.Next(glitchPool.Length), noIndex = rng.Next(glitchPool.Length);
                        SetText(yesLabel, () => GlitchCharacter(glitchPool, yesIndex));
                        SetText(noLabel, () => GlitchCharacter(glitchPool, noIndex));
                        int confidenceValue = rng.Next(0, 100);
                        SetText(confidence, () => GameText.F("信心 {0}%", "Confidence {0}%", confidenceValue));
                        yield return Wait(.1f);
                    }
                    SetText(confidence, "信心 —");
                }
                yield return Fade(buttonsGroup, 0, .4f);
                SetText(spoken, sentence);
                if (reduce)
                {
                    spoken.alpha = 0;
                    for (float t = 0; t < .3f; t += Time.unscaledDeltaTime) { spoken.alpha = t / .3f; yield return null; }
                    spoken.alpha = 1;
                }
                else
                {
                    for (int i = 0; i <= sentence.Length && !skipRequested; i++) { SetReveal(spoken, sentence.Length == 0 ? 1 : (float)i / sentence.Length); yield return Wait(.06f); }
                    SetReveal(spoken, 1);
                }
                yield return Wait(1f);
                // A chat window opens by itself, the sentence already inside. Slow glow, no flash.
                var chat = Panel(cutsceneRoot, new Vector2(560, -260), new Vector2(460, 170), new Color(.93f, .95f, .98f, 1));
                var chatGroup = chat.gameObject.AddComponent<CanvasGroup>();
                chatGroup.alpha = 0;
                Panel(chat, new Vector2(0, 66), new Vector2(460, 38), new Color(.36f, .6f, .82f, 1));
                Label(chat, "YY · 新消息", 20, new Vector2(-120, 66), new Vector2(200, 30), Color.white);
                Label(chat, sentence, 26, new Vector2(0, -12), new Vector2(420, 90), new Color(.1f, .12f, .16f, 1));
                yield return Fade(chatGroup, 1, .8f);
                yield return Wait(1.6f);
            }
            yield return Fade(cutsceneGroup, 0, .8f);
            SetText(header, "");
            FinishCutscene();
        }

        // ------------------------------------------------------------------ building blocks

        private void BuildOverlay()
        {
            font = CreateFont();
            var canvasObject = new GameObject("LingGuang Story Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            root = (RectTransform)canvasObject.transform;

            // Cutscene layer (full screen, blocks input only while a cutscene plays).
            var cut = new GameObject("Cutscene", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            cutsceneRoot = (RectTransform)cut.transform;
            cutsceneRoot.SetParent(root, false);
            Stretch(cutsceneRoot);
            cutsceneBackdrop = cut.GetComponent<Image>();
            cutsceneBackdrop.color = Color.black;
            cutsceneGroup = cut.GetComponent<CanvasGroup>();
            cutsceneGroup.alpha = 0;
            cutsceneGroup.blocksRaycasts = false;

            // Narration: bottom, never blocks input.
            var box = Panel(root, Vector2.zero, new Vector2(1320, 130), new Color(.02f, .03f, .05f, .8f));
            box.anchorMin = box.anchorMax = new Vector2(.5f, 0);
            box.anchoredPosition = new Vector2(0, 120);
            box.GetComponent<Image>().raycastTarget = false;
            var line = Panel(box, new Vector2(0, 64), new Vector2(1320, 2), new Color(.56f, .72f, .87f, .6f));
            line.GetComponent<Image>().raycastTarget = false;
            narrationGroup = box.gameObject.AddComponent<CanvasGroup>();
            narrationGroup.alpha = 0;
            narrationGroup.blocksRaycasts = false;
            narrationGroup.interactable = false;
            narrationText = Label(box, "", 30, Vector2.zero, new Vector2(1240, 116), new Color(.92f, .94f, .97f, 1));
            narrationText.raycastTarget = false;

            // Toast: top right, clickable.
            var toast = Panel(root, Vector2.zero, new Vector2(520, 96), new Color(.06f, .1f, .16f, .92f));
            toast.anchorMin = toast.anchorMax = new Vector2(1, 1);
            toast.anchoredPosition = new Vector2(-290, -80);
            Panel(toast, new Vector2(-256, 0), new Vector2(4, 96), new Color(.31f, .76f, .97f, .9f)).GetComponent<Image>().raycastTarget = false;
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0;
            toastGroup.blocksRaycasts = false;
            toastButton = toast.gameObject.AddComponent<Button>();
            toastText = Label(toast, "", 22, new Vector2(6, 0), new Vector2(480, 84), new Color(.92f, .94f, .97f, 1));
            toastText.alignment = TextAlignmentOptions.MidlineLeft;
            toastText.raycastTarget = false;
        }

        private static TMP_FontAsset CreateFont()
        {
            Font source = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if (source == null) return TMP_Settings.defaultFontAsset;
            var asset = TMP_FontAsset.CreateFontAsset(source, 48, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = "LingGuangV05_Story_Cjk";
            asset.hideFlags = HideFlags.DontSave;
            asset.isMultiAtlasTexturesEnabled = true;
            return asset;
        }

        private void ClearCutscene()
        {
            if (cutsceneRoot == null) return;
            foreach (var label in cutsceneRoot.GetComponentsInChildren<TMP_Text>(true)) localizedLabels.Remove(label);
            for (int i = cutsceneRoot.childCount - 1; i >= 0; i--) Destroy(cutsceneRoot.GetChild(i).gameObject);
        }

        private RectTransform Panel(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private TextMeshProUGUI Label(Transform parent, string text, float size, Vector2 position, Vector2 box, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = box;
            rect.anchoredPosition = position;
            var label = go.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.richText = false;
            label.raycastTarget = false;
            SetText(label, text);
            return label;
        }

        private void SetText(TMP_Text label, string source)
        {
            SetText(label, () => GameText.Source(source ?? ""));
        }

        private void SetText(TMP_Text label, Func<string> render)
        {
            if (label == null) return;
            var binding = new LocalizedLabel { Render = render };
            localizedLabels[label] = binding;
            RenderLocalized(label, binding);
        }

        private void SetReveal(TMP_Text label, float progress)
        {
            if (label == null || !localizedLabels.TryGetValue(label, out var binding)) return;
            binding.Reveal = Mathf.Clamp01(progress);
            label.maxVisibleCharacters = Mathf.CeilToInt(label.text.Length * binding.Reveal);
        }

        private static void RenderLocalized(TMP_Text label, LocalizedLabel binding)
        {
            label.text = binding.Render() ?? "";
            label.maxVisibleCharacters = Mathf.CeilToInt(label.text.Length * binding.Reveal);
        }

        private void RefreshLocalizedText()
        {
            expiredLabels.Clear();
            foreach (var pair in localizedLabels)
            {
                if (pair.Key == null) expiredLabels.Add(pair.Key);
                else RenderLocalized(pair.Key, pair.Value);
            }
            foreach (var label in expiredLabels) localizedLabels.Remove(label);
            // The native contact preview mirrors the final message body. Refresh it after
            // authored bubbles, independent of the adapter's locale-event subscription order.
            if (chatAdapter != null) chatAdapter.RefreshLocalizedConversation();
        }

        private static string GlitchCharacter(string source, int index)
        {
            string pool = GameText.Source(source);
            return string.IsNullOrEmpty(pool) ? "?" : pool[index % pool.Length].ToString();
        }

        private Button ChoiceButton(Transform parent, string text, Vector2 position, Color color, Action onClick)
        {
            var rect = Panel(parent, position, new Vector2(260, 96), color);
            var button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            Label(rect, text, 44, Vector2.zero, new Vector2(240, 90), Color.white);
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private IEnumerator Fade(CanvasGroup group, float target, float seconds)
        {
            float start = group.alpha;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, t / seconds);
                yield return null;
            }
            group.alpha = target;
        }

        private IEnumerator Wait(float seconds)
        {
            for (float t = 0; t < seconds && !skipRequested; t += Time.unscaledDeltaTime) yield return null;
        }

        private static void Complete(ref Action done)
        {
            var callback = done;
            done = null;
            if (callback != null) callback();
        }
    }
}
