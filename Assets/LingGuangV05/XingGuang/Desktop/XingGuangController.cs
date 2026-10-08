using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppNames = LingGuangV05.Core.AppNames;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// 灵光.exe: owns the lab simulation (labelling desk → training → contracts → research), its save file and its view.
    /// Lives next to ChapterOneRuntime, outside every window, attached at scene load (Main.unity is not edited).
    /// It takes over the existing 灵光.exe window: icon, taskbar button and the "installed" gate stay as they were;
    /// the old native content is removed and the bridge routes opening through this controller.
    /// Story signals (first time each): lg.trainable, lg.epoch, lg.assess, lg.checkpoint, lg.gradeA, lg.gradeS, lg.auto,
    /// lg.autotrain, lg.contract, lg.nan, lg.combo50.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class XingGuangController : MonoBehaviour, IDesktopAppView
    {
        public const string AppId = "lingguang";
        /// <summary>摆渡众包 (DesktopBridge/Zhongbao): it hosts the 标注台 and 订单 pages against this controller's Sim.</summary>
        public const string CrowdAppId = "zhongbao";

        public ChapterOneRuntime runtime;
        public XgSim Sim { get; private set; }
        public XingGuangHost Host { get; private set; }
        public WindowManager Window { get; private set; }
        public XingGuangView View { get; private set; }
        /// <summary>The font the lab pages are drawn with (摆渡众包 uses it too, so its pages look the same).</summary>
        public TMP_FontAsset Font => font;

        /// <summary>What the lab did while the game was closed, shown once as a 欢迎回来 card.</summary>
        public sealed class OfflineReport { public double seconds, income, labels; public int epochs, records; }
        public OfflineReport Offline { get; set; }

        bool initialized, quiet;
        XgBrain brain;
        XgStoryBridge storyBridge;
        XgPlatformPopups platformPopups;
        float sinceSave;
        ChapterOneSim linked;
        TMP_Text title, status;
        XgWindowChrome chrome;
        TMP_FontAsset font;
        RectTransform content;
        readonly HashSet<string> raised = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void Attach()
        {
            var runtime = FindAnyObjectByType<ChapterOneRuntime>();
            if (runtime == null) return;
            var controller = runtime.GetComponent<XingGuangController>() ?? runtime.gameObject.AddComponent<XingGuangController>();
            controller.runtime = runtime;
            controller.Initialize();
        }

        public void Initialize()
        {
            if (initialized || runtime == null) return;
            initialized = true;
            runtime.EnsureInitialized();
            Host = new XingGuangHost(runtime);
            BindSave(runtime.Sim);
            // No offline progress: the lab only works while the game runs.
            runtime.Changed += OnRuntimeChanged;
            runtime.Saving += WriteToSave;
            GameText.Changed += ApplyShellText;
            TakeOverWindow();
            brain = GetComponent<XgBrain>() ?? gameObject.AddComponent<XgBrain>();
            brain.Bind(this);
            storyBridge = GetComponent<XgStoryBridge>() ?? gameObject.AddComponent<XgStoryBridge>();
            storyBridge.Bind(this);
            platformPopups = GetComponent<XgPlatformPopups>() ?? gameObject.AddComponent<XgPlatformPopups>();
            platformPopups.Bind(this);
            (GetComponent<XgMemoryNotebook>() ?? gameObject.AddComponent<XgMemoryNotebook>()).Bind(this);
        }

        /// <summary>The lab lives inside the 灵光 save (GameState.labState): load, reload and 重新开始 all follow it.</summary>
        void BindSave(ChapterOneSim sim)
        {
            linked = sim;
            XgState state = null;
            string json = sim != null ? sim.S.labState : null;
            if (!string.IsNullOrEmpty(json))
            {
                try { state = JsonUtility.FromJson<XgState>(json); if (state != null && state.version != 1) state = null; }
                catch (Exception error) { Debug.LogWarning("灵光进度读取失败，从头开始：" + error.Message); state = null; }
            }
            Sim = new XgSim(state) { English = GameText.IsEnglish };
            // The save's own date, before any catch-up tick reads the calendar.
            if (sim != null) Sim.Today = GameCalendar.Yyyymmdd(GameCalendar.Now(sim.S));
            raised.Clear();
            // Milestones already reached in this save do not re-trigger the story this session.
            if (state != null) RaiseProgress(silent: true);
            if (View != null) View.Bind(this);
        }

        void WriteToSave()
        {
            if (Sim == null || runtime == null || runtime.Sim == null || runtime.Sim != linked) return;
            runtime.Sim.S.labState = JsonUtility.ToJson(Sim.S);
        }

        public bool SaveNow() { return runtime != null && runtime.SaveNow(); }
        public bool FeatureVisible(string feature) => Sim != null && Sim.FeatureVisible(feature);
        public bool EndingQuestionReady => Sim != null && Sim.EndingAvailable && runtime != null && runtime.Sim != null && runtime.Sim.S.story.GetFlag("delivered.ending_card") > 0;

        void OnRuntimeChanged()
        {
            if (runtime == null || runtime.Sim == null) return;
            if (runtime.Sim != linked)
            {
                BindSave(runtime.Sim);
            }
        }

        void Update()
        {
            if (Sim == null || runtime == null || runtime.Sim == null) return;
            Sim.English = GameText.IsEnglish;
            Sim.WindowOpen = Window != null && Window.isOn && View != null;
            // The rent holiday: nothing is charged until 灵光 learns its third ability (ChapterOneSim.Bills.cs).
            if (runtime.Sim.RentWaived == null) runtime.Sim.RentWaived = () => Sim == null || Sim.AbilitiesCount < XgSim.RentFromAbility;
            // The 2016 calendar follows the lab (design v1.1 §11.7): the prologue day until 灵光 is installed, then
            // the stage's month at its progress. It reads progress and never holds anything back.
            if (runtime.Sim.AppInstalled && !runtime.Sim.InPrologue)
            {
                GameCalendar.Advance(runtime.Sim.S, GameCalendar.DayFor(Sim.S.stage, Sim.MonthProgress));
                // The finale is New Year's Eve (design v1.1 §8); the ending turns the clock to 2017.
                if (Sim.S.fullOpen) GameCalendar.Advance(runtime.Sim.S, GameCalendar.DayIndex(GameCalendar.Ending));
                if (Sim.S.ending.Length > 0) GameCalendar.NewYear(runtime.Sim.S);
                SyncProfile(runtime.Sim.S);
            }
            Sim.Today = GameCalendar.Yyyymmdd(GameCalendar.Now(runtime.Sim.S));
            Sim.Clock = runtime.Sim.S.gameSeconds;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 2f);
            if (brain != null) brain.TickBeforeSimulation();
            Sim.Tick(dt, Host);
            Sim.S.linkedGameSeconds = runtime.Sim.S.gameSeconds;
            RaiseProgress();
            // Training, income and auto-answers change the lab every frame; the runtime saves on its own 15 s rhythm.
            sinceSave += dt;
            if (sinceSave >= 1f) { sinceSave = 0; if (!runtime.TestMode) runtime.MarkDirty(); }
        }

        string profileSignature = "";
        XgSim profiled;

        /// <summary>The opening setup (saved in GameState) is what the lab's personality reads (design v1.1 §11.4).</summary>
        void SyncProfile(GameState s)
        {
            string signature = s.aiName + "|" + s.aiSelf + "|" + s.aiCallMe + "|" + s.personality + "|" + s.targetWarmth + "|" + s.targetPlay + "|" + s.targetOpinion + "|" + string.Join(",", s.toneWords ?? new System.Collections.Generic.List<string>());
            // A reloaded or reset lab is a new XgSim: it needs the profile too.
            if (signature == profileSignature && ReferenceEquals(profiled, Sim)) return;
            profileSignature = signature; profiled = Sim;
            Sim.Profile = new XgProfile
            {
                name = s.aiName == "the code" ? "" : s.aiName, self = s.aiSelf.Length > 0 ? s.aiSelf : "我", callMe = s.aiCallMe.Length > 0 ? s.aiCallMe : "你",
                personality = s.personality, warmth = s.targetWarmth, play = s.targetPlay, opinion = s.targetOpinion,
                words = new System.Collections.Generic.List<string>(s.toneWords ?? new System.Collections.Generic.List<string>()),
            };
            if (!Sim.S.personaSeeded && s.prologue == 2) { Sim.S.personaSeeded = true; Sim.SeedPersonality(); }
        }

        /// <summary>First-time milestones for the story (老周 reacts). Each is raised once per session; beats fire once.</summary>
        void RaiseProgress(bool silent = false)
        {
            if (runtime.TestMode) return;
            quiet = silent;
            Raise("lg.unsure", Sim.S.uncertaintyObserved);
            Raise("lg.trainable", Sim.TrainingUnlocked(XgTrack.Vision) || Sim.TrainingUnlocked(XgTrack.Sequence));
            Raise("lg.epoch", Sim.S.epochs > 0);
            Raise("lg.assess", Sim.S.assessments > 0);
            Raise("lg.checkpoint", Sim.S.best.Count > 0);
            bool gradeA = false, gradeS = false;
            foreach (var g in Sim.S.grades) { if (g.EndsWith("#3")) gradeA = true; if (g.EndsWith("#4")) gradeS = true; }
            Raise("lg.gradeA", gradeA);
            Raise("lg.gradeS", gradeS);
            Raise("lg.auto", Sim.AutoLevelTotal > 0);
            Raise("lg.autotrain", Sim.AutoTrainLevel >= 2);
            Raise("lg.combo50", Sim.S.bestCombo >= 50);
            Raise("lg.contract", Sim.S.contracts.Count > 0);
            Raise("lg.nan", Sim.S.nanEvents > 0);
            quiet = false;
        }

        void Raise(string signal, bool reached)
        {
            if (!reached || raised.Contains(signal)) return;
            raised.Add(signal);
            if (!quiet) runtime.RaiseSignal(signal);
        }

        void OnDestroy()
        {
            if (runtime != null) { runtime.Changed -= OnRuntimeChanged; runtime.Saving -= WriteToSave; }
            GameText.Changed -= ApplyShellText;
        }

        public void Open()
        {
            var router = runtime.GetComponent<ChapterOneDesktopRouter>() ?? FindAnyObjectByType<ChapterOneDesktopRouter>();
            if (router != null) router.Open(AppId);
        }

        /// <summary>Opens 摆渡众包 on a page (label, contracts, subcontract, ledger, credit; null keeps the current one).</summary>
        public bool OpenCrowd(string page)
        {
            var router = runtime != null ? runtime.GetComponent<ChapterOneDesktopRouter>() ?? FindAnyObjectByType<ChapterOneDesktopRouter>() : null;
            return router != null && router.Open(CrowdAppId, page);
        }

        // ───────────── IDesktopAppView ─────────────

        public bool EnsureReady()
        {
            if (content == null || Window == null) return false;
            if (View == null)
            {
                View = content.gameObject.AddComponent<XingGuangView>();
                View.Build(this, content, font);
                ApplyShellText();
            }
            return true;
        }

        public void OnOpened(string tab)
        {
            if (View == null) return;
            EnsureShown(this, Window);
            View.Open(tab);
        }

        // ───────────── window takeover ─────────────

        void TakeOverWindow()
        {
            var router = runtime.GetComponent<ChapterOneDesktopRouter>() ?? FindAnyObjectByType<ChapterOneDesktopRouter>();
            var binding = router != null ? router.Get(AppId) : null;
            if (binding == null || binding.Window == null || binding.Window.windowContainer == null)
            {
                Debug.LogWarning("灵光：找不到 灵光.exe 窗口，未接入桌面。");
                return;
            }
            Window = binding.Window;
            KeepOpenOnFirstStart(Window);
            content = Window.windowContainer.Find("Content") as RectTransform;
            if (content == null) return;
            var shop = router.Get("xunbao");
            if (shop != null && shop.Content != null) foreach (var label in shop.Content.GetComponentsInChildren<TMP_Text>(true)) { font = label.font; break; }
            if (font == null) font = Window.GetComponentInChildren<TMP_Text>(true)?.font;

            // Retire the old 灵光 content (native workbench / self-drawn app) without touching the window chrome.
            foreach (var old in content.GetComponents<MonoBehaviour>())
                if (old is ChapterOneNativeAppView || old.GetType().Name == "ChapterOneApp") old.enabled = false;
            for (int i = content.childCount - 1; i >= 0; i--) content.GetChild(i).gameObject.SetActive(false);
            var bg = content.GetComponent<Image>();
            if (bg != null) bg.color = XingGuangView.Palette.Page;

            foreach (var label in Window.windowContainer.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.transform.IsChildOf(content)) continue;
                if (label.name == "ChapterOneTitle") title = label;
                else if (label.name == "Status") status = label;
                else continue;
                var localized = label.GetComponent<DesktopLocalizedText>();
                if (localized != null) Destroy(localized);
            }
            // The dark caption of the redesign, on this window only (other apps keep their Aero bars).
            chrome = XgWindowChrome.Apply(Window);
            binding.UseCustomView(this);
        }

        /// <summary>
        /// A window that is still inactive at load runs WindowManager.Start on its first open, and Start's
        /// disableAtStart would close it again at once. The scene already hides it, so turn that off.
        /// </summary>
        public static void KeepOpenOnFirstStart(WindowManager window)
        {
            if (window == null) return;
            typeof(WindowManager).GetField("disableAtStart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(window, false);
        }

        /// <summary>
        /// On a window's very first activation DreamOS can drop Play("In") and leave it in the hidden
        /// Window_OutHelper state (open but invisible). For a few frames after opening, replay the entry animation
        /// if that happened. Bounded; never fights a close or minimize.
        /// </summary>
        public static void EnsureShown(MonoBehaviour host, WindowManager window)
        {
            if (host != null && window != null && host.isActiveAndEnabled) host.StartCoroutine(Recover(window));
        }

        static System.Collections.IEnumerator Recover(WindowManager window)
        {
            for (int i = 0; i < 6; i++)
            {
                yield return null;
                if (window == null || !window.isOn || !window.gameObject.activeInHierarchy) yield break;
                var animator = window.windowAnimator;
                var group = window.GetComponent<CanvasGroup>();
                if (animator == null || group == null || group.alpha > .01f) continue;
                var clips = animator.GetCurrentAnimatorClipInfo(0);
                if (clips.Length > 0 && clips[0].clip != null && clips[0].clip.name.Contains("Out"))
                {
                    animator.enabled = true;
                    animator.Play("In", 0, 0);
                    yield break;
                }
            }
        }

        void ApplyShellText()
        {
            if (chrome != null) chrome.SetTitle(GameText.T(AppNames.ExeZh, AppNames.ExeEn), GameText.T("深度学习工作站", "deep learning workstation"));
            else if (title != null) title.text = GameText.T(AppNames.ExeZh + "  —  深度学习工作站", AppNames.ExeEn + "  —  deep learning workstation");
            if (status != null) status.text = Lang.T("显卡、电费和 ¥ 与家里共用 · 窗口关闭后训练继续");
            if (View != null) View.Refresh(true);
        }
    }
}
