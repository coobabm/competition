using System;
using System.Collections.Generic;
using LingGuangV05.Desktop.Media;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.YY;
using LingGuangV05.Desktop.Zhongbao;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The resident spider: 灵光 given a body, living on the desktop above every window and below the taskbar and its
    /// popups. It roams over the text of whatever is open (YY chats, 记事本, 摆渡, 贴吧, mail, 灵光's own pages): its feet
    /// plant on real words of the visible TextMeshPro texts, and a touched word glitches for a moment (colour, size jump,
    /// jitter, a highlight or box, a ghost copy), then is put back exactly. When the 标注台 of 摆渡众包 is up and the
    /// player is answering, it walks to a perch beside the card and watches, pensive: it breathes, tilts toward the
    /// card, taps a word on it now and then, leans in when the player hesitates, pulses after each answer and looks a
    /// beat longer when the answer went against its own guess. It wanders off after a while without answers.
    ///
    /// Rules live in <see cref="XgResident"/> and <see cref="XgResidentMind"/>: it appears once 灵光.exe is installed and
    /// set up, grows with the abilities (four legs to eight), hides during cutscenes and fullscreen video, keeps off
    /// 晴雯's YY conversation after the player's 【算了】, and parks in a corner when told to keep quiet (the nav toggle
    /// in 灵光, saved in <see cref="XgState.residentQuiet"/>). It never takes a click: no raycast targets on the layer it
    /// is drawn on. The one exception is a small invisible handle that follows its body while the 标注台 is up: the player
    /// can pick the spider up and drop it onto the labelling workspace (see "Dragged to the question" below).
    /// Light by design: one spider, a few glitches a second, words gathered one window at a time, pooled ghosts.
    /// The idea of a spider walking on text comes from @rybinfx's web crawler; this is an independent implementation.
    /// </summary>
    public sealed class XgResidentSpider : MonoBehaviour
    {
        const int MaxGlitches = 3, MaxWordsPerText = 90, MaxWords = 1600, GhostPool = 6;
        const float ScanEvery = .12f, FindEvery = 1.5f;
        /// <summary>Drawn a little larger than the rules' size: the desktop is a 1080p canvas seen on a small monitor.</summary>
        const float DesktopScale = 1.3f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if (FindAnyObjectByType<XgResidentSpider>() != null) return;
            var go = new GameObject("XgResidentSpider");
            DontDestroyOnLoad(go);
            go.AddComponent<XgResidentSpider>();
        }

        /// <summary>The live instance (QA and tests).</summary>
        public static XgResidentSpider Instance { get; private set; }

        sealed class Win
        {
            public RectTransform rt;
            public WindowManager manager;
            public Rect rect;
            public int order;
            public bool visible, forbidden;
            public readonly List<int> words = new List<int>();
        }

        sealed class Word
        {
            public TMP_Text text;
            public int first, count;
            public Rect rect;
            public Win win;
            public bool alive;
        }

        sealed class Glitch
        {
            public TMP_Text text;
            public int first, count;
            public Rect rect;
            public float t, life;
            public int kinds;
            public Color accent;
            public bool valid = true;
            public readonly List<int> mesh = new List<int>(), vertex = new List<int>();
            public readonly List<Vector3> verts = new List<Vector3>();
            public readonly List<Color32> colors = new List<Color32>();
        }

        sealed class GhostCopy { public TMP_Text text; public Vector2 at, drift; public float t, life; public Color color; }

        // Scene references, found lazily.
        XingGuangController controller;
        ZhongbaoView zhongbao;
        YYChatView yyView;
        StoryDesktopPresenter presenter;
        OriginCurtain curtain;
        PrologueDirector prologue;
        RectTransform desktop, apps, layer;
        XgCrawlerLayer graphic;
        CanvasGroup layerGroup;
        WindowManager videoWindow;
        float findTimer, scanTimer;
        int scanCursor;

        readonly List<Win> windows = new List<Win>();
        readonly List<Word> words = new List<Word>();
        readonly List<TMP_Text> textBuffer = new List<TMP_Text>();
        readonly Dictionary<TMP_Text, RectMask2D> masks = new Dictionary<TMP_Text, RectMask2D>();
        readonly List<Glitch> glitches = new List<Glitch>();
        readonly List<GhostCopy> ghosts = new List<GhostCopy>();
        readonly Stack<TMP_Text> ghostPool = new Stack<TMP_Text>();

        readonly XgResidentMind mind = new XgResidentMind();
        readonly XgSpiderFx fx = new XgSpiderFx();
        readonly List<Vector2> reachPoints = new List<Vector2>();
        float thinkTimer = 16, introCheck;
        // Answering a card for the player: the card it decided on, whether it will, and how far along it is.
        long decidedCard = -2;
        bool pressPlanned;
        int pressStage;
        float pressWait;
        XgSpiderWalker.Leg pressLeg;
        Coroutine intro;

        // Dragged to the question: the player picks it up and drops it on the workspace; it then stands beside the card
        // and answers with an arm that reaches the button, presses it and comes back (XgSim.SpiderDragAnswer).
        const float ReachOut = .30f, ReachHold = .14f, ReachBack = .28f;
        enum Reach { Idle, Out, Press, Back }
        RectTransform handleRt;
        XgSpiderHandle handle;
        bool held, working, wasLabelUp;
        Vector2 heldAt, grabOffset, pointerAt;
        Reach reach;
        float reachClock, thinkLeft, noteClock;
        bool reachYes, pressed;
        long workCard = -2;
        RectTransform reachButton;
        Vector2 armBase, armTip;
        float armK;
        /// <summary>The spider is carried by the pointer or answering beside the card (the page and tests read it).</summary>
        public bool Dragged => held || working;
        public bool Working => working;

        /// <summary>The spider's first appearance is playing (other story layers can wait for it).</summary>
        public static bool IntroPlaying { get; private set; }
        XgSpiderWalker walker;
        int legCount;
        Vector2 target;
        float pauseLeft, glitchCooldown, tapTimer, pulseT = -1, breath, readTimer;
        int pulses;
        bool shown;
        Rect screen;
        readonly System.Random rng = new System.Random();

        static readonly Color[] Neon = { XgDark.Good, XgDark.Data, XgDark.Params, XgDark.Hot, XgDark.Link, XgDark.Gold };

        public XgResidentMode Mode => mind.Mode;
        public Vector2 Position => walker != null ? walker.pos : Vector2.zero;
        public int Legs => walker != null ? walker.legs.Length : 0;

        /// <summary>Sends it to read a given element next (QA, and a hook for story beats).</summary>
        public void Visit(RectTransform rt)
        {
            if (rt == null || layer == null || walker == null) return;
            target = LayerRect(rt).center; pauseLeft = 0;
        }

        XgSim Sim => controller != null ? controller.Sim : null;
        bool Reduced => Sim != null && Sim.S.reduceFx;

        void Awake() { Instance = this; TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged); }

        void OnDestroy()
        {
            RestoreAll();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            if (Instance == this) Instance = null;
        }

        void OnDisable()
        {
            StopWork();
            RestoreAll();
            if (intro != null) { StopCoroutine(intro); intro = null; }
            IntroPlaying = false;
        }

        // ───────────── scene ─────────────

        bool Find()
        {
            findTimer -= Time.unscaledDeltaTime;
            if (findTimer > 0 && controller != null && layer != null) return true;
            findTimer = FindEvery;
            if (controller == null) controller = FindAnyObjectByType<XingGuangController>();
            if (zhongbao == null) zhongbao = FindAnyObjectByType<ZhongbaoView>();
            if (yyView == null) yyView = FindAnyObjectByType<YYChatView>();
            if (presenter == null) { presenter = FindAnyObjectByType<StoryDesktopPresenter>(); if (presenter != null) curtain = presenter.GetComponent<OriginCurtain>(); }
            if (curtain == null) curtain = FindAnyObjectByType<OriginCurtain>();
            if (prologue == null) prologue = FindAnyObjectByType<PrologueDirector>();
            if (desktop == null)
            {
                var source = GameObject.Find("hongmengos 2010 Desktop Source");
                if (source != null) desktop = source.transform.Find("Desktop") as RectTransform;
                if (desktop != null) apps = desktop.Find("Apps & Windows") as RectTransform;
            }
            if (apps != null && layer == null) BuildLayer();
            if (apps != null && videoWindow == null) { var v = apps.Find("Video Player"); if (v != null) videoWindow = v.GetComponent<WindowManager>(); }
            return controller != null && layer != null;
        }

        void BuildLayer()
        {
            var go = new GameObject("Resident Spider", typeof(RectTransform));
            go.layer = desktop.gameObject.layer;
            layer = (RectTransform)go.transform;
            layer.SetParent(desktop, false);
            layer.anchorMin = Vector2.zero; layer.anchorMax = Vector2.one; layer.offsetMin = layer.offsetMax = Vector2.zero;
            layer.SetSiblingIndex(apps.GetSiblingIndex() + 1);
            layerGroup = go.AddComponent<CanvasGroup>();
            layerGroup.blocksRaycasts = false; layerGroup.interactable = false;
            go.AddComponent<CanvasRenderer>();
            graphic = go.AddComponent<XgCrawlerLayer>();
            graphic.raycastTarget = false;
            graphic.color = Color.white;
            graphic.draw = Draw;
            go.SetActive(false);
        }

        bool CutscenePlaying =>
            presenter != null && presenter.CutscenePlaying || AutoLabelEpiphany.Playing || AiJoinsYy.Playing
            || LoveQuestionCutscene.Playing || XgEmergenceCutscene.Playing || curtain != null && curtain.Playing
            || prologue != null && prologue.Running;

        bool FullscreenVideo => videoWindow != null && videoWindow.isOn && videoWindow.isFullscreen && videoWindow.gameObject.activeInHierarchy;

        /// <summary>Inside 灵光's 摆渡百科 crawler, it is the crawler; it is not on the desktop at the same time.</summary>
        bool InsideCrawler => controller != null && controller.View != null && controller.View.Data != null && controller.View.Data.CrawlerOpen && controller.View.Visible;

        // ───────────── frame ─────────────

        void Update()
        {
            if (!Find()) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            var rt = controller.runtime;
            var pointer = UnityEngine.InputSystem.Mouse.current;
            if (pointer != null && (pointer.leftButton.isPressed || pointer.rightButton.isPressed)) lastPress = Time.unscaledTime;
            TickIntro(dt);
            bool present = Sim != null && rt != null && rt.Sim != null && Sim.SpiderAround && !IntroPlaying
                && XgResident.Present(rt.Sim.AppInstalled, rt.Sim.InPrologue, CutscenePlaying || InsideCrawler, FullscreenVideo);
            bool labelVisible = zhongbao != null && zhongbao.Visible && zhongbao.Tab == "label" && zhongbao.Label != null && zhongbao.Label.Paper != null && zhongbao.Label.Paper.gameObject.activeInHierarchy;
            int answers = zhongbao != null && zhongbao.Label != null ? zhongbao.Label.AnswerCount : 0;
            bool disagreed = zhongbao != null && zhongbao.Label != null && zhongbao.Label.LastAnswerDisagreed;
            var mode = mind.Tick(dt, present, Sim != null && Sim.S.residentQuiet, labelVisible, answers, disagreed);

            if (mode == XgResidentMode.Hidden) { StopWork(); UpdateHandle(false); Show(false); return; }
            Show(true);
            if (layer.GetSiblingIndex() != apps.GetSiblingIndex() + 1) layer.SetSiblingIndex(apps.GetSiblingIndex() + 1);
            screen = layer.rect;

            int abilities = Sim.AbilitiesCount;
            int legs = XgResident.LegCount(abilities);
            float size = (float)XgResident.Size(abilities) * DesktopScale;
            if (walker == null || legCount != legs || Mathf.Abs(walker.size - size) > .01f)
            {
                var at = walker != null ? walker.pos : new Vector2(screen.xMax - 160, screen.yMin + 140);
                walker = new XgSpiderWalker(at, size, legs) { FindFoothold = Foothold, Planted = OnPlanted };
                legCount = legs;
            }

            scanTimer -= dt;
            if (scanTimer <= 0) { scanTimer = ScanEvery; ScanNext(); }

            glitchCooldown -= dt;
            // The handle that lets the player pick it up exists only while the 标注台 is in front.
            bool canDrag = labelVisible && mode != XgResidentMode.Parked;
            if (!canDrag) StopWork();
            UpdateHandle(canDrag);
            if (mode != XgResidentMode.Watch || Dragged) { fx.SetReach(null); decidedCard = -2; pressPlanned = false; }
            if (held) TickHeld(dt);
            else if (working) TickWork(dt);
            else switch (mode)
            {
                case XgResidentMode.Parked: TickParked(dt); break;
                case XgResidentMode.Watch: TickWatch(dt); break;
                default: TickRoam(dt); break;
            }
            if (mind.Pulses != pulses) { pulses = mind.Pulses; pulseT = 0; }
            if (pulseT >= 0) { pulseT += dt; if (pulseT > .6f) pulseT = -1; }
            fx.reduced = Reduced;
            fx.Tick(dt, walker);
            TickGlitches(dt);
            TickGhosts(dt);
            graphic.Redraw();
        }

        void Show(bool on)
        {
            if (shown == on || layer == null) return;
            shown = on;
            if (!on) { RestoreAll(); fx.Clear(); foreach (var g in ghosts) { g.text.gameObject.SetActive(false); ghostPool.Push(g.text); } ghosts.Clear(); }
            layer.gameObject.SetActive(on);
        }

        // ───────────── behaviour ─────────────

        void TickRoam(float dt)
        {
            walker.Settled = false;
            walker.bodyScale = 1;
            if (pauseLeft > 0)
            {
                // Reading: stand still, tap a word under a front leg now and then.
                pauseLeft -= dt;
                walker.Settled = true;
                readTimer -= dt;
                if (readTimer <= 0 && !fx.Thinking) { readTimer = .5f + (float)rng.NextDouble() * .7f; TapNear(walker.pos, 70 * walker.size); }
                walker.Tick(dt, walker.pos, 0, Reduced);
                return;
            }
            // Now and then it stops to think: its threads light up in turn and a ripple goes out.
            thinkTimer -= dt;
            if (thinkTimer <= 0 && fx.Threads >= 4) { thinkTimer = 14 + 8 * (float)rng.NextDouble(); fx.Think(); pauseLeft = 1.9f; return; }
            if (Vector2.Distance(walker.pos, target) < 10 * walker.size || !screen.Contains(target) || Forbidden(target))
            {
                if (Vector2.Distance(walker.pos, target) < 10 * walker.size) pauseLeft = .6f + (float)rng.NextDouble() * 1.8f;
                target = NextRoamTarget();
            }
            // Leave a forbidden window at once (晴雯's chat after 【算了】).
            if (Forbidden(walker.pos)) target = NearestOutside(walker.pos);
            walker.Tick(dt, target, .8f, Reduced);
            ClampToScreen();
        }

        void TickWatch(float dt)
        {
            var paper = zhongbao.Label.Paper;
            var card = LayerRect(paper);
            XgResident.Perch(Box(card), Box(screen), walker.size, out double px, out double py, out bool facesLeft);
            var perch = new Vector2((float)px, (float)py);
            var toCard = (card.center - perch).normalized;
            if (mind.Leaning) perch += toCard * 14 * walker.size;
            breath += dt;
            // While the player hesitates, threads reach for the card's words.
            reachPoints.Clear();
            if (mind.Leaning) foreach (var w in words) { if (w.alive && card.Overlaps(w.rect)) reachPoints.Add(w.rect.center); if (reachPoints.Count >= 5) break; }
            fx.SetReach(reachPoints);
            if (TickPress(dt, ref perch)) return;
            float distance = Vector2.Distance(walker.pos, perch);
            if (distance > 6)
            {
                walker.Settled = false;
                walker.bodyScale = 1;
                walker.Tick(dt, perch, 1.1f, Reduced);
                return;
            }
            // Perched: breathe, tilt toward the card, tap a word on it now and then.
            walker.Settled = true;
            walker.pos = Vector2.Lerp(walker.pos, perch, 1 - Mathf.Exp(-4 * dt));
            float look = Mathf.Atan2(card.center.y - walker.pos.y, card.center.x - walker.pos.x);
            float tilt = mind.Pondering > 0 || Reduced ? 0 : Mathf.Sin(breath * .9f) * .14f + Mathf.Sin(breath * .37f) * .06f;
            walker.heading = Mathf.LerpAngle(walker.heading * Mathf.Rad2Deg, (look + tilt) * Mathf.Rad2Deg, 1 - Mathf.Exp(-3 * dt)) * Mathf.Deg2Rad;
            walker.bodyScale = 1 + (Reduced ? 0 : .035f * Mathf.Sin(breath * 2.2f)) + (pulseT >= 0 ? .12f * Mathf.Sin(pulseT / .6f * Mathf.PI) : 0);
            walker.Tick(dt, walker.pos, 0, Reduced);
            tapTimer -= dt;
            if (tapTimer <= 0 && mind.Pondering <= 0)
            {
                tapTimer = 2 + (float)rng.NextDouble() * 1.8f;
                TapCard(card);
            }
        }

        /// <summary>
        /// Sometimes it answers the card itself: once per card it rolls <see cref="XgSim.SpiderTakesCard"/>; if it takes
        /// the card (and the desk is within its ability) it waits a moment, walks to the button of the checkpoint's guess,
        /// presses it with a leg and goes back to its perch. Returns true while it is busy with that.
        /// </summary>
        bool TickPress(float dt, ref Vector2 perch)
        {
            var label = zhongbao.Label;
            long id = label.ShownCardId;
            if (id != decidedCard)
            {
                decidedCard = id; pressStage = 0; pressLeg = null;
                pressPlanned = id >= 0 && label.HasShownGuess && Sim.SpiderAnswerBlocker(Sim.S.desk, controller.Host) == null
                    && XgSim.SpiderTakesCard(Sim.AbilitiesCount, rng.NextDouble());
                pressWait = 1.4f + 1.4f * (float)rng.NextDouble();
            }
            if (!pressPlanned) return false;
            var button = label.ShownGuessYes ? label.YesButton : label.NoButton;
            if (button == null || !button.gameObject.activeInHierarchy) { pressPlanned = false; return false; }
            if (pressStage == 0)
            {
                pressWait -= dt;
                if (pressWait > 0 || mind.Pondering > 0) return false;
                pressStage = 1;
            }
            var b = LayerRect(button);
            var stand = new Vector2(b.center.x + b.width * .25f, b.yMax + 38 * walker.size);
            if (pressStage == 1)
            {
                walker.Settled = false; walker.bodyScale = 1;
                walker.Tick(dt, stand, 1.2f, Reduced);
                if (Vector2.Distance(walker.pos, stand) < 8)
                {
                    pressStage = 2;
                    XgSpiderWalker.Leg best = null; float bestD = float.MaxValue;
                    foreach (var l in walker.legs) { float d = Vector2.Distance(l.foot, b.center); if (d < bestD) { bestD = d; best = l; } }
                    pressLeg = best;
                    if (best != null) { best.stepping = false; walker.StepTo(best, b.center, -1); }
                }
                return true;
            }
            // Stage 2: the leg comes down on the button.
            walker.Settled = true;
            float look = Mathf.Atan2(b.center.y - walker.pos.y, b.center.x - walker.pos.x);
            walker.heading = Mathf.LerpAngle(walker.heading * Mathf.Rad2Deg, look * Mathf.Rad2Deg, 1 - Mathf.Exp(-6 * dt)) * Mathf.Deg2Rad;
            walker.Tick(dt, walker.pos, 0, Reduced);
            if (pressLeg == null || !pressLeg.stepping)
            {
                var r = label.SpiderPress();
                pressPlanned = false; pressStage = 0;
                if (r.accepted) { pulseT = 0; fx.Credit(); }
            }
            return true;
        }

        // ───────────── dragged to the question ─────────────

        /// <summary>The small invisible handle over the body (made on first use, above the spider's layer and below the taskbar).</summary>
        void UpdateHandle(bool on)
        {
            if (handleRt == null)
            {
                if (!on) return;
                var go = new GameObject("Resident Spider Handle", typeof(RectTransform));
                go.layer = desktop.gameObject.layer;
                handleRt = (RectTransform)go.transform;
                handleRt.SetParent(desktop, false);
                handleRt.anchorMin = handleRt.anchorMax = new Vector2(.5f, .5f);
                var image = go.AddComponent<Image>();
                image.color = new Color(0, 0, 0, 0); image.raycastTarget = true;
                image.canvasRenderer.cullTransparentMesh = false; // invisible, but it still takes the pointer
                handle = go.AddComponent<XgSpiderHandle>();
                handle.owner = this;
            }
            on &= walker != null;
            if (handleRt.gameObject.activeSelf != on) handleRt.gameObject.SetActive(on);
            if (!on) return;
            handleRt.SetSiblingIndex(Mathf.Min(desktop.childCount - 1, layer.GetSiblingIndex() + 1));
            handleRt.sizeDelta = Vector2.one * Mathf.Max(44, 52 * walker.size);
            handleRt.position = layer.TransformPoint(walker.pos);
        }

        /// <summary>Pointer position in the spider layer's space, through the event camera (the desktop canvas is Screen Space – Camera on an LCD texture).</summary>
        bool PointerInLayer(PointerEventData e, out Vector2 local)
        {
            local = default;
            if (layer == null) return false;
            var cam = e.pressEventCamera != null ? e.pressEventCamera : e.enterEventCamera;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, e.position, cam, out local);
        }

        internal void GrabSpider(PointerEventData e)
        {
            if (walker == null || !PointerInLayer(e, out var p) || zhongbao == null || zhongbao.Label == null) return;
            held = true; working = false; reach = Reach.Idle; armK = 0;
            grabOffset = walker.pos - p; heldAt = walker.pos; pointerAt = p;
            zhongbao.Label.SetSpiderRole(XgLabelPage.SpiderRole.Held);
            zhongbao.Juice?.Play(XgJuice.Sfx.Id.Swoosh, 1.4f, .5f);
        }

        internal void MoveGrabbed(PointerEventData e)
        {
            if (!held || !PointerInLayer(e, out var p)) return;
            pointerAt = p;
            heldAt = p + grabOffset;
            heldAt.x = Mathf.Clamp(heldAt.x, screen.xMin + 12, screen.xMax - 12);
            heldAt.y = Mathf.Clamp(heldAt.y, screen.yMin + 60, screen.yMax - 12);
        }

        /// <summary>Dropped on the workspace it goes to work; anywhere else it is let go and carries on as before.</summary>
        internal void DropSpider(PointerEventData e)
        {
            if (!held) return;
            if (PointerInLayer(e, out var p)) { pointerAt = p; heldAt = p + grabOffset; }
            held = false;
            var label = zhongbao != null ? zhongbao.Label : null;
            if (label != null && label.Workspace != null && LayerRect(label.Workspace).Contains(pointerAt))
            {
                working = true; reach = Reach.Idle; workCard = -2; thinkLeft = .5f; noteClock = 0; armK = 0;
                label.SetSpiderRole(XgLabelPage.SpiderRole.Working);
                zhongbao.Juice?.Play(XgJuice.Sfx.Id.Unlock, 1.3f, .5f);
            }
            else
            {
                working = false;
                label?.SetSpiderRole(XgLabelPage.SpiderRole.Home);
                pauseLeft = 0; target = walker.pos + new Vector2(0, -60);
            }
        }

        /// <summary>It stops wherever it was: carried, answering, or reaching.</summary>
        void StopWork()
        {
            if (!held && !working) return;
            held = false; working = false; reach = Reach.Idle; armK = 0; reachButton = null;
            if (walker != null) walker.eyeBoost = 0;
            var label = zhongbao != null ? zhongbao.Label : null;
            label?.SetSpiderRole(XgLabelPage.SpiderRole.Home);
        }

        void TickHeld(float dt)
        {
            walker.Settled = false;
            walker.eyeBoost = 0;
            walker.bodyScale = 1.18f;
            walker.pos = Vector2.Lerp(walker.pos, heldAt, 1 - Mathf.Exp(-22 * dt));
            walker.heading = Mathf.LerpAngle(walker.heading * Mathf.Rad2Deg, 90, 1 - Mathf.Exp(-6 * dt)) * Mathf.Deg2Rad;
            walker.Tick(dt, walker.pos, 0, Reduced);
            var label = zhongbao.Label;
            if (label != null && label.Workspace != null) label.SetDropHint(LayerRect(label.Workspace).Contains(pointerAt));
        }

        static float Ease(float k) { k = Mathf.Clamp01(k); return k * k * (3 - 2 * k); }

        /// <summary>
        /// Standing beside the question card, it answers again and again: think for a moment, reach for the button of its
        /// answer, press it, bring the arm back. One answer takes about a second. When it cannot (the checkpoint is not
        /// good enough for the desk, a review waits, the card is special) it stands and says why.
        /// </summary>
        void TickWork(float dt)
        {
            var label = zhongbao.Label;
            var card = LayerRect(label.Paper);
            var area = LayerRect(label.Workspace);
            float size = walker.size;
            var perch = new Vector2(Mathf.Max(card.xMax + 4, area.xMax - 20 * size), Mathf.Lerp(card.center.y, card.yMin, .75f));
            breath += dt;
            float away = Vector2.Distance(walker.pos, perch);
            if (away > 8 && reach == Reach.Idle)
            {
                walker.Settled = false; walker.bodyScale = 1;
                walker.Tick(dt, perch, 2.4f, Reduced);
                armK = 0;
                return;
            }
            walker.Settled = true;
            walker.pos = Vector2.Lerp(walker.pos, perch, 1 - Mathf.Exp(-6 * dt));
            walker.bodyScale = 1 + (Reduced ? 0 : .03f * Mathf.Sin(breath * 2.4f)) + (pulseT >= 0 ? .12f * Mathf.Sin(pulseT / .6f * Mathf.PI) : 0);

            // Face what it is looking at: the card between answers, the button while reaching.
            Vector2 look = card.center;
            if (reach != Reach.Idle && reachButton != null) look = LayerRect(reachButton).center;
            float want = Mathf.Atan2(look.y - walker.pos.y, look.x - walker.pos.x);
            walker.heading = Mathf.LerpAngle(walker.heading * Mathf.Rad2Deg, want * Mathf.Rad2Deg, 1 - Mathf.Exp(-8 * dt)) * Mathf.Deg2Rad;
            walker.Tick(dt, walker.pos, 0, Reduced);
            walker.eyeBoost = Mathf.MoveTowards(walker.eyeBoost, reach == Reach.Idle && thinkLeft > 0 ? 1 : 0, 4 * dt);

            armBase = walker.pos + walker.Forward * 9 * size * walker.bodyScale;
            switch (reach)
            {
                case Reach.Idle:
                {
                    armK = Mathf.MoveTowards(armK, 0, 6 * dt);
                    long id = label.ShownCardId;
                    if (id != workCard) { workCard = id; thinkLeft = .32f + .16f * (float)rng.NextDouble(); }
                    string blocker = label.SpiderBlocker();
                    if (blocker != null || id < 0)
                    {
                        noteClock -= dt;
                        if (noteClock <= 0 && blocker != null) { label.SpiderIdle(blocker); noteClock = 6; }
                        return;
                    }
                    thinkLeft -= dt;
                    if (thinkLeft > 0) return;
                    if (!label.SpiderPeek(out bool yes)) return;
                    reachYes = yes; reachButton = label.Button(yes); reachClock = 0; pressed = false; reach = Reach.Out;
                    return;
                }
                case Reach.Out:
                {
                    if (reachButton == null || !reachButton.gameObject.activeInHierarchy) { reach = Reach.Back; return; }
                    reachClock += dt / ReachOut;
                    armK = Ease(reachClock);
                    armTip = Vector2.Lerp(armBase + walker.Forward * 16 * size, LayerRect(reachButton).center, armK);
                    if (reachClock >= 1) { reach = Reach.Press; reachClock = 0; }
                    return;
                }
                case Reach.Press:
                {
                    armK = 1;
                    armTip = reachButton != null ? LayerRect(reachButton).center : armTip;
                    if (!pressed)
                    {
                        pressed = true;
                        var r = label.SpiderHandPress(reachYes);
                        if (r.accepted) { pulseT = 0; if (r.correct) fx.Credit(); }
                    }
                    reachClock += dt / ReachHold;
                    if (reachClock >= 1) { reach = Reach.Back; reachClock = 1; }
                    return;
                }
                default:
                {
                    reachClock -= dt / ReachBack;
                    armK = Ease(reachClock);
                    if (reachButton != null) armTip = Vector2.Lerp(armBase + walker.Forward * 16 * size, LayerRect(reachButton).center, armK);
                    if (reachClock <= 0) { reach = Reach.Idle; armK = 0; reachButton = null; thinkLeft = Mathf.Max(thinkLeft, .12f); }
                    return;
                }
            }
        }

        /// <summary>The reaching arm: two bent segments from the front of the body to a glowing hand on the button.</summary>
        void DrawArm(VertexHelper vh, Color leg, Color halo, Color eye, float alpha)
        {
            if (!working || armK < .02f || walker == null) return;
            var b = armBase; var t = armTip;
            float len = Vector2.Distance(b, t);
            if (len < 2) return;
            float size = walker.size;
            float segment = Mathf.Max(30 * size, len * .56f);
            float bend = Mathf.Sqrt(Mathf.Max(0, segment * segment - len * len / 4));
            var dir = (t - b) / len;
            var normal = new Vector2(-dir.y, dir.x);
            if (normal.y < 0) normal = -normal;
            var knee = (b + t) * .5f + normal * bend;
            XgDraw.Seg(vh, b, knee, 2.6f * size + 2.2f, halo);
            XgDraw.Seg(vh, knee, t, 2.1f * size + 2.2f, halo);
            XgDraw.Seg(vh, b, knee, 2.6f * size, leg);
            XgDraw.Seg(vh, knee, t, 2.1f * size, leg);
            XgDraw.Disc(vh, knee, 2f * size, leg, 8);
            // The hand: a glowing pad that lights up as it presses.
            float press = reach == Reach.Press ? 1 : 0;
            var glow = eye; glow.a = (.35f + .3f * press) * alpha;
            XgSoftDraw.Halo(vh, t, (7 + 6 * press) * size, glow, 12);
            XgDraw.Disc(vh, t, 3.8f * size + 1.2f, halo, 10);
            XgDraw.Disc(vh, t, 3.8f * size, eye, 10);
            if (reach == Reach.Press && !Reduced)
            {
                var ring = eye; ring.a = (1 - reachClock) * .8f * alpha;
                XgDraw.Ring(vh, t, (6 + 14 * reachClock) * size, 1.6f, ring, 20);
            }
        }

        // ───────────── first appearance ─────────────

        void TickIntro(float dt)
        {
            if (intro != null || Sim == null || controller.runtime == null || controller.runtime.Sim == null) return;
            introCheck -= dt;
            if (introCheck > 0) return;
            introCheck = 1;
            var rt = controller.runtime;
            bool busy = rt.TestMode || CutscenePlaying || FullscreenVideo || InnerVoice.Busy || PrologueDirector.Desk == null
                || controller.View == null || controller.View.Data == null || controller.Window == null
                || !IntroWelcome(controller);
            if (!Sim.SpiderIntroDue(rt.Sim.AppInstalled, rt.Sim.InPrologue, busy)) return;
            intro = StartCoroutine(IntroScene());
        }

        /// <summary>The player has not pressed a mouse button for this long: the intro may take the screen.</summary>
        const float IntroIdleSeconds = 12;
        float lastPress = -100;

        /// <summary>
        /// The intro pulls 灵光 to the 数据 page, so it never interrupts a step the player is in the middle of: it plays
        /// when they are already on 数据, or have been idle for a moment, and never over the AI joining YY.
        /// </summary>
        bool IntroWelcome(XingGuangController lab)
        {
            if (AiJoinsYy.Playing) return false;
            if (lab.View != null && lab.View.Visible && lab.View.Tab == "data") return true;
            return Time.unscaledTime - lastPress >= IntroIdleSeconds;
        }

        /// <summary>
        /// Its first appearance: 灵光 opens by itself on the 数据 page with the crawler already reading a 摆渡百科 page;
        /// the inner voice is startled; the spider notices the cursor, freezes and looks back for a beat, then reads on.
        /// The flag is set first, so it plays once even if cut short.
        /// </summary>
        System.Collections.IEnumerator IntroScene()
        {
            IntroPlaying = true;
            Sim.S.spiderIntroDone = true;
            controller.Open();
            yield return null;
            if (controller.View != null) { controller.View.Open("data"); controller.View.Data.OpenCrawlerIntro(); }
            yield return new WaitForSecondsRealtime(3.5f);
            yield return InnerVoice.SayAndWait("……那是什么？", "…what is that?", 1.6f);
            yield return InnerVoice.SayAndWait("它在……看百科？", "Is it… reading the encyclopedia?", 1.8f);
            yield return InnerVoice.SayAndWait("谁让它上网的。", "Who let it online?", 1.6f);
            var crawler = controller.View != null && controller.View.Data != null ? controller.View.Data.Crawler : null;
            if (crawler != null && crawler.IsOpen) crawler.LookAtPlayer(2.6f);
            yield return new WaitForSecondsRealtime(2.8f);
            yield return InnerVoice.SayAndWait("它在学。", "It's learning.", 2f);
            IntroPlaying = false;
            intro = null;
        }

        void TickParked(float dt)
        {
            XgResident.ParkingSpot(Box(screen), walker.size, out double x, out double y);
            var spot = new Vector2((float)x, (float)y);
            walker.bodyScale = 1;
            if (Vector2.Distance(walker.pos, spot) > 6) { walker.Settled = false; walker.Tick(dt, spot, .7f, Reduced); return; }
            walker.Settled = true;
            walker.heading = Mathf.LerpAngle(walker.heading * Mathf.Rad2Deg, 135, 1 - Mathf.Exp(-2 * dt)) * Mathf.Deg2Rad;
            walker.Tick(dt, walker.pos, 0, true);
        }

        void ClampToScreen()
        {
            walker.pos.x = Mathf.Clamp(walker.pos.x, screen.xMin + 12, screen.xMax - 12);
            walker.pos.y = Mathf.Clamp(walker.pos.y, screen.yMin + 60, screen.yMax - 12);
        }

        Vector2 NextRoamTarget()
        {
            // A word somewhere readable, preferring ones not too far away so it reads a window before crossing the screen.
            int best = -1; float bestScore = float.MaxValue;
            for (int tries = 0; tries < 24 && words.Count > 0; tries++)
            {
                int i = rng.Next(words.Count);
                var w = words[i];
                if (!w.alive || w.win == null || !w.win.visible || w.win.forbidden) continue;
                float d = Vector2.Distance(w.rect.center, walker.pos);
                float score = Mathf.Abs(d - 220) + (float)rng.NextDouble() * 120;
                if (CrossesForbidden(walker.pos, w.rect.center)) score += 2000;
                if (score < bestScore) { bestScore = score; best = i; }
            }
            if (best >= 0) return words[best].rect.center;
            return new Vector2(Mathf.Lerp(screen.xMin + 80, screen.xMax - 80, (float)rng.NextDouble()), Mathf.Lerp(screen.yMin + 120, screen.yMax - 80, (float)rng.NextDouble()));
        }

        void TapNear(Vector2 at, float radius)
        {
            if (walker == null) return;
            int w = Foothold(at + walker.Forward * radius * .6f, radius, out var spot);
            if (w < 0) return;
            XgSpiderWalker.Leg best = null; float bestD = float.MaxValue;
            foreach (var l in walker.legs) { if (l.stepping) continue; float d = Vector2.Distance(l.foot, spot); if (d < bestD) { bestD = d; best = l; } }
            if (best != null && bestD < radius * 1.4f) walker.StepTo(best, spot, w);
        }

        void TapCard(Rect card)
        {
            // A word inside the card (question, phrase), or the card's near edge.
            int pick = -1;
            for (int tries = 0; tries < 30 && words.Count > 0; tries++)
            {
                int i = rng.Next(words.Count);
                if (words[i].alive && card.Overlaps(words[i].rect) && Vector2.Distance(words[i].rect.center, walker.pos) < 110 * walker.size) { pick = i; break; }
            }
            var spot = pick >= 0 ? words[pick].rect.center : new Vector2(Mathf.Clamp(walker.pos.x, card.xMin + 6, card.xMax - 6), Mathf.Clamp(walker.pos.y, card.yMin + 6, card.yMax - 6));
            XgSpiderWalker.Leg best = null; float bestD = float.MaxValue;
            foreach (var l in walker.legs) { if (l.stepping) continue; float d = Vector2.Distance(l.foot, spot); if (d < bestD) { bestD = d; best = l; } }
            if (best != null && bestD < 95 * walker.size) walker.StepTo(best, spot, pick);
        }

        void OnPlanted(XgSpiderWalker who, XgSpiderWalker.Leg leg)
        {
            if (leg.word < 0 || leg.word >= words.Count) return;
            var w = words[leg.word];
            if (!w.alive || w.text == null) return;
            if (glitchCooldown > 0 || glitches.Count >= MaxGlitches) return;
            glitchCooldown = Reduced ? 1f : mind.Mode == XgResidentMode.Watch ? .5f : .28f;
            bool onCard = mind.Mode == XgResidentMode.Watch || Dragged;
            StartGlitch(w, onCard);
            fx.reduced = Reduced;
            fx.Read(walker, leg, w.rect, XgSpiderFx.Accent(rng), w.count);
        }

        // ───────────── reading the screen ─────────────

        void RefreshWindows()
        {
            var seen = new HashSet<RectTransform>();
            bool mayQingwen = XgResident.MayReadQingwen(Sim.S);
            var hub = YYChatHub.Instance;
            bool qingwenOpen = hub != null && hub.S != null && hub.S.selected == YYChatHub.GirlfriendId;
            var yyWindow = yyView != null ? yyView.GetComponentInParent<WindowManager>() : null;
            for (int i = 0; i < apps.childCount; i++)
            {
                var child = apps.GetChild(i) as RectTransform;
                if (child == null) continue;
                seen.Add(child);
                var win = windows.Find(x => x.rt == child);
                if (win == null) { win = new Win { rt = child, manager = child.GetComponent<WindowManager>() }; windows.Add(win); }
                win.order = i;
                win.visible = child.gameObject.activeInHierarchy && (win.manager == null ? false : DesktopNotifications.IsWindowVisible(win.manager));
                // The window root covers the screen; its frame is the container.
                var frame = win.manager != null && win.manager.windowContainer != null ? win.manager.windowContainer as RectTransform : null;
                win.rect = LayerRect(frame != null ? frame : child);
                win.forbidden = !mayQingwen && qingwenOpen && yyWindow != null && win.manager == yyWindow;
            }
            windows.RemoveAll(x => !seen.Contains(x.rt));
        }

        /// <summary>Gathers the words of one window per call (round robin); the window under the spider comes up twice as often.</summary>
        void ScanNext()
        {
            RefreshWindows();
            if (windows.Count == 0) return;
            Win win;
            var under = TopWindowAt(walker.pos);
            if (under != null && scanCursor % 2 == 0) win = under;
            else win = windows[(scanCursor / 2) % windows.Count];
            scanCursor++;
            foreach (int index in win.words) { var w = words[index]; w.alive = false; w.text = null; }
            win.words.Clear();
            if (!win.visible || win.forbidden) return;
            if (words.Count > MaxWords) Compact();
            textBuffer.Clear();
            win.rt.GetComponentsInChildren(false, textBuffer);
            foreach (var t in textBuffer) Collect(t, win);
        }

        void Compact()
        {
            // Drop dead words and renumber the windows' lists.
            var keep = new List<Word>();
            foreach (var w in words) if (w.alive) keep.Add(w);
            words.Clear(); words.AddRange(keep);
            foreach (var win in windows) win.words.Clear();
            for (int i = 0; i < words.Count; i++) if (words[i].win != null) words[i].win.words.Add(i);
            // Feet that pointed at old indices point at nothing now.
            if (walker != null) foreach (var l in walker.legs) l.word = -1;
        }

        void Collect(TMP_Text t, Win win)
        {
            if (t == null || !t.isActiveAndEnabled || t.color.a < .1f || string.IsNullOrEmpty(t.text)) return;
            var info = t.textInfo;
            if (info == null || info.characterCount == 0 || info.characterInfo == null) return;
            if (t.canvasRenderer != null && t.canvasRenderer.GetInheritedAlpha() < .1f) return;
            Rect clip = win.rect;
            if (masks.Count > 4000) masks.Clear();
            if (!masks.TryGetValue(t, out var mask)) { mask = t.GetComponentInParent<RectMask2D>(); masks[t] = mask; }
            if (mask != null) { var m = LayerRect(mask.rectTransform); if (!Intersect(ref clip, m)) return; }
            int added = 0;
            int n = Mathf.Min(info.characterCount, info.characterInfo.Length);
            int i = 0;
            while (i < n && added < MaxWordsPerText)
            {
                var ci = info.characterInfo[i];
                char c = ci.character;
                if (!ci.isVisible || !char.IsLetterOrDigit(c)) { i++; continue; }
                int start = i, line = ci.lineNumber;
                bool cjk = c >= 0x3400 && c <= 0x9FFF;
                int j = i + 1;
                while (j < n)
                {
                    var cj = info.characterInfo[j];
                    if (!cj.isVisible || cj.lineNumber != line || !char.IsLetterOrDigit(cj.character)) break;
                    bool jc = cj.character >= 0x3400 && cj.character <= 0x9FFF;
                    if (jc != cjk || cjk && j - start >= 2) break;
                    j++;
                }
                i = j;
                var r = CharsRect(t, info, start, j - start);
                if (r.width < 2 || !clip.Contains(r.center) || Occluded(r.center, win)) continue;
                var w = new Word { text = t, first = start, count = j - start, rect = r, win = win, alive = true };
                win.words.Add(words.Count);
                words.Add(w);
                added++;
            }
        }

        Rect CharsRect(TMP_Text t, TMP_TextInfo info, int first, int count)
        {
            float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
            for (int c = first; c < first + count; c++)
            {
                var ci = info.characterInfo[c];
                xMin = Mathf.Min(xMin, ci.bottomLeft.x); xMax = Mathf.Max(xMax, ci.topRight.x);
                yMin = Mathf.Min(yMin, ci.descender); yMax = Mathf.Max(yMax, ci.ascender);
            }
            Vector2 a = layer.InverseTransformPoint(t.transform.TransformPoint(new Vector3(xMin, yMin)));
            Vector2 b = layer.InverseTransformPoint(t.transform.TransformPoint(new Vector3(xMax, yMax)));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        bool Occluded(Vector2 p, Win owner)
        {
            foreach (var w in windows) if (w.visible && w.order > owner.order && w.rect.Contains(p)) return true;
            return false;
        }

        Win TopWindowAt(Vector2 p)
        {
            Win top = null;
            foreach (var w in windows) if (w.visible && w.rect.Contains(p) && (top == null || w.order > top.order)) top = w;
            return top;
        }

        bool Forbidden(Vector2 p) { foreach (var w in windows) if (w.forbidden && w.visible && w.rect.Contains(p)) return true; return false; }

        bool CrossesForbidden(Vector2 a, Vector2 b)
        {
            for (int k = 1; k <= 8; k++) if (Forbidden(Vector2.Lerp(a, b, k / 8f))) return true;
            return false;
        }

        Vector2 NearestOutside(Vector2 p)
        {
            foreach (var w in windows)
            {
                if (!w.forbidden || !w.visible || !w.rect.Contains(p)) continue;
                var r = w.rect;
                float left = p.x - r.xMin, right = r.xMax - p.x, down = p.y - r.yMin, up = r.yMax - p.y;
                float m = Mathf.Min(Mathf.Min(left, right), Mathf.Min(down, up));
                if (m == left) return new Vector2(r.xMin - 40, p.y);
                if (m == right) return new Vector2(r.xMax + 40, p.y);
                if (m == down) return new Vector2(p.x, r.yMin - 40);
                return new Vector2(p.x, r.yMax + 40);
            }
            return p;
        }

        int Foothold(Vector2 near, float radius, out Vector2 at)
        {
            at = near;
            int best = -1; float bestD = float.MaxValue;
            for (int i = 0; i < words.Count; i++)
            {
                var w = words[i];
                if (!w.alive || w.win == null || w.win.forbidden) continue;
                var r = w.rect;
                float dx = Mathf.Max(0, Mathf.Max(r.xMin - near.x, near.x - r.xMax));
                if (dx > radius) continue;
                float dy = Mathf.Max(0, Mathf.Max(r.yMin - near.y, near.y - r.yMax));
                float d = dx * dx + dy * dy;
                if (d < bestD && d <= radius * radius) { bestD = d; best = i; }
            }
            if (best >= 0) { var r = words[best].rect; at = new Vector2(Mathf.Clamp(near.x, r.xMin + r.width * .25f, r.xMax - r.width * .25f), r.center.y); }
            return best;
        }

        // ───────────── glitches (vertex edits, always put back) ─────────────

        void StartGlitch(Word w, bool gentle)
        {
            var t = w.text;
            var info = t.textInfo;
            if (info == null || w.first + w.count > info.characterCount) return;
            foreach (var g0 in glitches) if (g0.text == t && g0.first == w.first) return;
            var g = new Glitch { text = t, first = w.first, count = w.count, rect = w.rect, accent = Neon[rng.Next(Neon.Length)] };
            for (int c = w.first; c < w.first + w.count; c++)
            {
                var ci = info.characterInfo[c];
                if (!ci.isVisible) continue;
                if (ci.materialReferenceIndex >= info.meshInfo.Length) return;
                var mesh = info.meshInfo[ci.materialReferenceIndex];
                if (mesh.vertices == null || mesh.colors32 == null || ci.vertexIndex + 3 >= mesh.vertices.Length) return;
                for (int q = 0; q < 4; q++)
                {
                    g.mesh.Add(ci.materialReferenceIndex); g.vertex.Add(ci.vertexIndex + q);
                    g.verts.Add(mesh.vertices[ci.vertexIndex + q]); g.colors.Add(mesh.colors32[ci.vertexIndex + q]);
                }
            }
            if (g.verts.Count == 0) return;
            if (gentle || Reduced) { g.kinds = 1 | 4; g.life = .35f; }
            else
            {
                g.life = .55f;
                g.kinds = (1 << rng.Next(2)) | 4;
                if (rng.NextDouble() < .5) g.kinds |= 8;
                if (rng.NextDouble() < .4) g.kinds |= 16;
                if (rng.NextDouble() < .35) Ghost(w, g.accent);
            }
            glitches.Add(g);
        }

        void TickGlitches(float dt)
        {
            for (int i = glitches.Count - 1; i >= 0; i--)
            {
                var g = glitches[i];
                g.t += dt;
                bool alive = g.valid && g.text != null && g.text.isActiveAndEnabled;
                if (!alive) { glitches.RemoveAt(i); continue; }
                if (g.t >= g.life) { Restore(g); glitches.RemoveAt(i); continue; }
                Apply(g);
            }
        }

        void Apply(Glitch g)
        {
            var info = g.text.textInfo;
            float k = g.t / g.life, pulse = Mathf.Sin(k * Mathf.PI);
            var center = Vector3.zero;
            foreach (var v in g.verts) center += v;
            center /= g.verts.Count;
            float scale = (g.kinds & 8) != 0 ? 1 + .4f * pulse : 1;
            Vector3 jitter = (g.kinds & 16) != 0 ? new Vector3((float)rng.NextDouble() - .5f, (float)rng.NextDouble() - .5f) * 5 * pulse : Vector3.zero;
            for (int n = 0; n < g.verts.Count; n++)
            {
                int m = g.mesh[n], vi = g.vertex[n];
                if (m >= info.meshInfo.Length) { g.valid = false; return; }
                var mesh = info.meshInfo[m];
                if (mesh.vertices == null || vi >= mesh.vertices.Length) { g.valid = false; return; }
                mesh.vertices[vi] = center + (g.verts[n] - center) * scale + jitter;
                if ((g.kinds & 4) != 0) mesh.colors32[vi] = Color32.Lerp(g.accent, g.colors[n], k * k);
            }
            g.text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        void Restore(Glitch g)
        {
            if (!g.valid || g.text == null) return;
            var info = g.text.textInfo;
            for (int n = 0; n < g.verts.Count; n++)
            {
                int m = g.mesh[n], vi = g.vertex[n];
                if (m >= info.meshInfo.Length) return;
                var mesh = info.meshInfo[m];
                if (mesh.vertices == null || vi >= mesh.vertices.Length) return;
                mesh.vertices[vi] = g.verts[n];
                mesh.colors32[vi] = g.colors[n];
            }
            if (g.text.isActiveAndEnabled) g.text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        void RestoreAll()
        {
            foreach (var g in glitches) Restore(g);
            glitches.Clear();
        }

        /// <summary>A text rebuilt its mesh: its saved vertices are stale, so its glitch is dropped (nothing to put back).</summary>
        void OnTextChanged(UnityEngine.Object obj)
        {
            foreach (var g in glitches) if (g.text == obj) g.valid = false;
        }

        void Ghost(Word w, Color accent)
        {
            TMP_Text t;
            if (ghostPool.Count > 0) t = ghostPool.Pop();
            else if (ghosts.Count < GhostPool)
            {
                var go = new GameObject("Ghost", typeof(RectTransform));
                go.layer = layer.gameObject.layer;
                var rt = (RectTransform)go.transform;
                rt.SetParent(layer, false);
                rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
                rt.sizeDelta = new Vector2(200, 30);
                t = go.AddComponent<TextMeshProUGUI>();
                t.raycastTarget = false; t.richText = true; t.alignment = TextAlignmentOptions.Center;
                t.textWrappingMode = TextWrappingModes.NoWrap;
            }
            else return;
            t.gameObject.SetActive(true);
            t.font = w.text.font;
            var info = w.text.textInfo;
            var sb = new System.Text.StringBuilder();
            for (int c = w.first; c < w.first + w.count && c < info.characterCount; c++) sb.Append(info.characterInfo[c].character);
            string value = sb.ToString();
            if (value.Length == 0) { t.gameObject.SetActive(false); ghostPool.Push(t); return; }
            switch (rng.Next(4))
            {
                case 0: t.text = "<b><i>" + value + "</i></b>"; break;
                case 1: t.text = "<mspace=0.9em>" + value + "</mspace>"; break;
                case 2: t.text = "<s>" + value + "</s>"; break;
                default: t.text = value; break;
            }
            t.fontSize = Mathf.Max(10, w.rect.height * (rng.NextDouble() < .5 ? 1.6f : 1.05f));
            t.rectTransform.anchoredPosition = w.rect.center;
            ghosts.Add(new GhostCopy { text = t, at = w.rect.center, drift = new Vector2(((float)rng.NextDouble() - .5f) * 30, 12 + 12 * (float)rng.NextDouble()), life = .5f, color = accent });
        }

        void TickGhosts(float dt)
        {
            for (int i = ghosts.Count - 1; i >= 0; i--)
            {
                var g = ghosts[i];
                g.t += dt;
                if (g.t >= g.life) { g.text.gameObject.SetActive(false); ghostPool.Push(g.text); ghosts.RemoveAt(i); continue; }
                float k = g.t / g.life;
                g.text.rectTransform.anchoredPosition = g.at + g.drift * k;
                var c = g.color; c.a = (1 - k) * .85f; g.text.color = c;
            }
        }

        // ───────────── drawing ─────────────

        void Draw(VertexHelper vh)
        {
            if (walker == null) return;
            foreach (var g in glitches)
            {
                float k = g.t / g.life;
                var c = g.accent; c.a = (1 - k) * .35f;
                var pad = new Vector2(3, 2);
                if ((g.kinds & 1) != 0) XgDraw.Box(vh, g.rect.min - pad, g.rect.max + pad, c);
                if ((g.kinds & 2) != 0)
                {
                    c.a = (1 - k) * .9f;
                    var a = g.rect.min - pad; var b = g.rect.max + pad;
                    XgDraw.Box(vh, a, new Vector2(b.x, a.y + 1.2f), c); XgDraw.Box(vh, new Vector2(a.x, b.y - 1.2f), b, c);
                    XgDraw.Box(vh, a, new Vector2(a.x + 1.2f, b.y), c); XgDraw.Box(vh, new Vector2(b.x - 1.2f, a.y), b, c);
                }
            }
            bool parked = mind.Mode == XgResidentMode.Parked;
            float alpha = parked ? .6f : 1;
            if (pulseT >= 0 && !Reduced)
            {
                float k = pulseT / .6f;
                var ring = XgDark.Good; ring.a = (1 - k) * .55f;
                XgDraw.Ring(vh, walker.pos, (10 + 34 * k) * walker.size, 1.6f, ring, 32);
            }
            var leg = new Color(18 / 255f, 34 / 255f, 44 / 255f, .95f * alpha);
            var halo = new Color(214 / 255f, 238 / 255f, 245 / 255f, .7f * alpha);
            var body = new Color(20 / 255f, 48 / 255f, 52 / 255f, alpha);
            var eye = XgDark.Good; eye.a = alpha;
            if (mind.Pondering > 0) eye = XgDark.Gold;
            fx.DrawBehind(vh, walker, alpha);
            walker.Draw(vh, leg, body, eye, 0, halo);
            DrawArm(vh, leg, halo, eye, alpha);
            fx.DrawFront(vh, walker, alpha);
        }

        // ───────────── geometry ─────────────

        static readonly Vector3[] Corners = new Vector3[4];

        Rect LayerRect(RectTransform rt)
        {
            rt.GetWorldCorners(Corners);
            Vector2 a = layer.InverseTransformPoint(Corners[0]), b = layer.InverseTransformPoint(Corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        static bool Intersect(ref Rect a, Rect b)
        {
            float x0 = Mathf.Max(a.xMin, b.xMin), y0 = Mathf.Max(a.yMin, b.yMin), x1 = Mathf.Min(a.xMax, b.xMax), y1 = Mathf.Min(a.yMax, b.yMax);
            if (x1 <= x0 || y1 <= y0) return false;
            a = Rect.MinMaxRect(x0, y0, x1, y1);
            return true;
        }

        static XgBox Box(Rect r) => new XgBox(r.x, r.y, r.width, r.height);
    }

    /// <summary>
    /// The invisible handle over the resident spider's body while the 标注台 is in front: the player drags it onto the
    /// labelling workspace. Pointer positions are converted by the spider through the event camera.
    /// </summary>
    public sealed class XgSpiderHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public XgResidentSpider owner;
        bool dragging;
        public void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || owner == null) return;
            dragging = true; owner.GrabSpider(e);
        }
        public void OnDrag(PointerEventData e) { if (dragging) owner.MoveGrabbed(e); }
        public void OnEndDrag(PointerEventData e)
        {
            if (!dragging) return;
            dragging = false; owner.DropSpider(e);
        }
        void OnDisable()
        {
            // The handle went away mid-drag (the page closed): let go where it is.
            dragging = false;
        }
    }
}
