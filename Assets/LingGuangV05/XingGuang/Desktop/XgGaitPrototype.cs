using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// Prototype (2026-10-15, not wired into the game): the 摆渡百科 crawler learning to walk, live. A
    /// <see cref="XgGaitLearner"/> tries forty mutated gaits a generation (four lines of ten); the best one so far walks across a scrolling
    /// ruler, drawn with <see cref="XgSpiderWalker"/> and driven leg by leg from <see cref="XgGaitWalk"/> (planted feet stay
    /// put, swinging feet lift with a small outward arc; a fall sags the body, flashes red and wobbles before it gets up).
    /// Below it: the gait diagram (when each foot is planted, over two cycles) and the learning curve (best distance per
    /// generation, falls as red ticks). Builds its own Screen Space Overlay canvas; open the scene from
    /// LingGuang/Prototypes/Spider learns to walk and press Play.
    /// </summary>
    public sealed class XgGaitPrototype : MonoBehaviour
    {
        [SerializeField] TMP_FontAsset font;
        [SerializeField] int seed = 1;
        [Tooltip("Seconds between generations while 自动 is on.")]
        [SerializeField] float autoInterval = 1.5f;

        /// <summary>Screen pixels per sim unit (the walker is drawn at this size).</summary>
        const float Scale = 1.45f;
        /// <summary>Leg order of both the sim and the walker.</summary>
        static readonly string[] LegNames = { "L1", "R1", "L2", "R2", "L3", "R3", "L4", "R4" };
        /// <summary>Gait-diagram rows top to bottom: the left side front to back, then the right side.</summary>
        static readonly int[] RowLeg = { 0, 2, 4, 6, 1, 3, 5, 7 };
        /// <summary>The two alternating groups of the tetrapod (L1 R2 L3 R4 against R1 L2 R3 L4), coloured apart in the diagram.</summary>
        static bool GroupA(int leg) => leg == 0 || leg == 3 || leg == 4 || leg == 7;

        static readonly Color LegInk = new Color(18 / 255f, 34 / 255f, 44 / 255f, .95f);
        static readonly Color LegRim = new Color(214 / 255f, 238 / 255f, 245 / 255f, .75f);
        static readonly Color BodyInk = new Color32(20, 48, 52, 255);

        XgUi ui;
        XgGaitLearner learner;
        readonly List<float> tetrapodHistory = new List<float>();

        // The walking demo.
        XgGaitWalk walk;
        XgSpiderWalker walker;
        double baseX;
        float acc, camX = -70, sag, flash, legBlend;
        int shownFalls;
        readonly Vector2[] blendFrom = new Vector2[XgGaitGenes.Legs];
        readonly bool[] wasStance = new bool[XgGaitGenes.Legs];
        // Footprints: world x, y, age and which leg (ring buffer).
        const int Prints = 96;
        readonly Vector2[] printAt = new Vector2[Prints];
        readonly float[] printAge = new float[Prints];
        readonly int[] printLeg = new int[Prints];
        int printNext;

        // Auto-run.
        bool auto;
        float autoTimer;

        // UI.
        XgCrawlerLayer walkLayer, gaitLayer, curveLayer;
        TMP_Text stats, status, live, curveTop, curveRight;
        XgBtn stepButton, autoButton, resetButton;
        readonly TMP_Text[] rulerLabels = new TMP_Text[10];
        readonly int[] rulerValue = new int[10];
        readonly List<Vector2> curvePoints = new List<Vector2>(256);
        readonly List<Vector2> tetPoints = new List<Vector2>(256);
        float liveTimer;
        int curveTopValue = -1, curveTopShown = -1, curveGens, curveGensShown = -1;

        void Start()
        {
            if (font == null) font = TMP_Settings.defaultFontAsset;
            ui = new XgUi(font, null, true);
            EnsureEventSystem();
            Build();
            Restart(seed);
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        // ---------------------------------------------------------------- learning

        void Restart(int newSeed)
        {
            seed = newSeed;
            learner = new XgGaitLearner(seed);
            tetrapodHistory.Clear();
            tetrapodHistory.Add((float)XgGaitLearner.Tetrapod(learner.Best));
            baseX = 0; camX = -70; acc = 0; sag = 0; flash = 0;
            for (int i = 0; i < Prints; i++) printAge[i] = 99;
            // The sim walks along +x, so the drawn body faces right (the walker's own default heading points down the page).
            walker = new XgSpiderWalker(Vector2.zero, Scale) { heading = 0 };
            StartWalk(false);
            SetStatus(T("第 0 代：随机乱迈的八条腿。按「练一代」让它试四十种新走法。", "Generation 0: eight legs stepping at random. Press Train one to try forty new gaits."), XgDark.Muted);
            RefreshStats();
            curveLayer.Redraw();
        }

        void TrainOne()
        {
            learner.Generate();
            tetrapodHistory.Add((float)XgGaitLearner.Tetrapod(learner.Best));
            if (learner.Improved)
            {
                StartWalk(true);
                SetStatus(T("第 " + learner.Generation + " 代：找到一种更好的走法 ✓ 换它上场。", "Generation " + learner.Generation + ": found a better gait ✓ it walks now."), XgDark.Good);
            }
            else SetStatus(T("第 " + learner.Generation + " 代：四十种新走法都没更好，留着原来的接着试。", "Generation " + learner.Generation + ": none of forty tries did better; keeping the old one and trying again."), XgDark.Muted);
            RefreshStats();
            curveLayer.Redraw();
        }

        /// <summary>Starts the best gait walking from where the spider stands now (legs ease over from their old spots).</summary>
        void StartWalk(bool blend)
        {
            if (walk != null) baseX += walk.distance;
            if (blend) { for (int i = 0; i < blendFrom.Length; i++) blendFrom[i] = walker.legs[i].shown; legBlend = 1; }
            else legBlend = 0;
            walk = new XgGaitWalk(learner.Best.Clone());
            shownFalls = 0; acc = 0;
            for (int i = 0; i < wasStance.Length; i++) wasStance[i] = walk.legs[i].stance;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, .1f);
            if (auto)
            {
                autoTimer += dt;
                if (autoTimer >= autoInterval) { autoTimer = 0; TrainOne(); }
            }

            acc += dt;
            while (acc >= XgGaitLearner.Dt)
            {
                acc -= (float)XgGaitLearner.Dt;
                walk.Step(XgGaitLearner.Dt);
                for (int i = 0; i < walk.legs.Length; i++)
                {
                    bool st = walk.legs[i].stance;
                    if (st && !wasStance[i]) AddPrint(i);
                    wasStance[i] = st;
                }
            }
            if (walk.falls > shownFalls) { shownFalls = walk.falls; flash = 1; }
            sag = Mathf.MoveTowards(sag, walk.down > 0 ? 1 : 0, dt * (walk.down > 0 ? 14 : 5));
            flash = Mathf.Max(0, flash - dt * 2.2f);
            legBlend = Mathf.Max(0, legBlend - dt * 5);
            for (int i = 0; i < Prints; i++) printAge[i] += dt;
            if (curveTopValue >= 0 && curveTopShown != curveTopValue) { curveTopShown = curveTopValue; curveTop.text = curveTopValue.ToString(); }
            if (curveGens != curveGensShown) { curveGensShown = curveGens; curveRight.text = T(curveGens + " 代", curveGens + " gen"); }

            liveTimer -= dt;
            if (liveTimer <= 0) { liveTimer = .2f; RefreshLive(); }
            walkLayer.Redraw();
            gaitLayer.Redraw();
        }

        void AddPrint(int leg)
        {
            var l = walk.legs[leg];
            printAt[printNext] = new Vector2((float)(baseX + walk.distance + l.footX), (float)l.footY);
            printAge[printNext] = 0; printLeg[printNext] = leg;
            printNext = (printNext + 1) % Prints;
        }

        // ---------------------------------------------------------------- text

        static string T(string zh, string en) => XgUi.T(zh, en);
        static string N(double v, string f) => XgUi.N(v, f);

        void SetStatus(string text, Color color) { status.text = text; status.color = color; }

        void RefreshStats()
        {
            var s = learner.BestScore; var g = learner.Best;
            string falls = s.falls == 0 ? XgUi.Hex(XgDark.Good) + "0</color>" : XgUi.Hex(XgDark.Bad) + s.falls + "</color>";
            stats.text = T("第 <b>" + learner.Generation + "</b> 代    最好的一次 5 秒试走：<b>" + N(s.distance, "0") + "</b>    摔倒 " + falls + " 次    四足交替 <b>" + N(XgGaitLearner.Tetrapod(g), "0.00") + "</b>    波形 " + N(XgGaitLearner.Wave(g), "0.00"),
                "Generation <b>" + learner.Generation + "</b>    best 5 s trial: <b>" + N(s.distance, "0") + "</b>    falls " + falls + "    tetrapod <b>" + N(XgGaitLearner.Tetrapod(g), "0.00") + "</b>    wave " + N(XgGaitLearner.Wave(g), "0.00"))
                + "\n" + XgUi.Hex(XgDark.Muted) + T("着地 " + N(g.duty * 100, "0") + "%  ·  步幅 " + N(g.stride, "0") + "  ·  步频 " + N(g.freq, "0.0") + " 步/秒  ·  摇摆 " + N(s.wobble, "0.00") + "  ·  种子 " + seed,
                "duty " + N(g.duty * 100, "0") + "%  ·  stride " + N(g.stride, "0") + "  ·  " + N(g.freq, "0.0") + " steps/s  ·  waddle " + N(s.wobble, "0.00") + "  ·  seed " + seed) + "</color>";
            autoButton.Set(auto ? T("自动 ✓", "Auto ✓") : T("自动", "Auto"), true, auto ? XgDark.OnFill : (Color?)null, auto ? XgDark.Good : (Color?)null);
        }

        void RefreshLive()
        {
            live.text = T("现在这只：走了 " + N(walk.distance, "0") + " · " + N(walk.time, "0.0") + " 秒 · 摔了 " + walk.falls + " 次",
                "This walk: " + N(walk.distance, "0") + " in " + N(walk.time, "0.0") + " s · " + walk.falls + " falls");
        }

        // ---------------------------------------------------------------- layout

        void Build()
        {
            var canvasGo = new GameObject("GaitPrototypeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            var root = (RectTransform)canvasGo.transform;

            var page = XgUi.Rect("Page", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            XgUi.Panel(page, XgDark.Page).raycastTarget = false;

            // Header: title, what this is, and the three buttons.
            var header = XgUi.Strip("Header", page, 0, 64, 0, 0);
            XgUi.Panel(header, XgDark.Panel).raycastTarget = false;
            ui.Text(XgUi.TopLeft("Title", header, 24, 8, 700, 30), T("爬虫学走路", "The spider learns to walk"), 24, XgDark.Ink, TextAlignmentOptions.MidlineLeft);
            ui.Text(XgUi.TopLeft("Sub", header, 24, 38, 900, 20),
                T("原型 · 一个小小的进化学习器：每代试四十种变异的走法（四条线各十种），只留下走得更远、更稳的那种", "Prototype · a tiny evolution learner: forty mutated gaits a generation (four lines of ten), only the one that walks further and steadier stays"),
                13, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
            stepButton = ui.Button(header, T("练一代", "Train one"), TrainOne, 17);
            XgUi.PlaceTopRight(stepButton, 3 * 128 + 8, 12, 120, 40);
            autoButton = ui.Button(header, T("自动", "Auto"), () => { auto = !auto; autoTimer = autoInterval * .6f; RefreshStats(); }, 17);
            XgUi.PlaceTopRight(autoButton, 2 * 128 + 8, 12, 120, 40);
            resetButton = ui.Button(header, T("重来", "Restart"), () => { auto = false; Restart(seed + 1); }, 17);
            XgUi.PlaceTopRight(resetButton, 128 + 8, 12, 120, 40);

            // The walk.
            var walkCard = ui.Card(page, "WalkCard", new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -476), new Vector2(-16, -76));
            walkLayer = Layer(walkCard, "Walk", DrawWalk);
            stats = ui.Text(XgUi.TopLeft("Stats", walkCard, 16, 10, 1400, 50), "", 17, XgDark.Ink, TextAlignmentOptions.TopLeft);
            status = ui.Text(XgUi.Rect("Status", walkCard, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 8), new Vector2(-420, 30)), "", 14, XgDark.Muted, TextAlignmentOptions.BottomLeft);
            live = ui.Text(XgUi.Rect("Live", walkCard, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-410, 8), new Vector2(-16, 30)), "", 14, XgDark.Muted, TextAlignmentOptions.BottomRight);
            for (int i = 0; i < rulerLabels.Length; i++)
            {
                var rt = XgUi.Rect("Mark", walkLayer.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-30, -9), new Vector2(30, 9));
                rulerLabels[i] = ui.Text(rt, "", 12, XgDark.Dim, TextAlignmentOptions.Center);
                rulerLabels[i].textWrappingMode = TextWrappingModes.NoWrap;
                rulerValue[i] = int.MinValue;
            }

            // The gait diagram.
            var gaitCard = ui.Card(page, "GaitCard", new Vector2(0, 0), new Vector2(.5f, 1), new Vector2(16, 16), new Vector2(-8, -492));
            gaitLayer = Layer(gaitCard, "Gait", DrawGait);
            ui.Text(XgUi.TopLeft("Head", gaitCard, 16, 10, 700, 22), T("步态图 · 两个步子周期（色块 = 这只脚踩在地上）", "Gait diagram · two step cycles (bar = that foot is planted)"), 15, XgDark.Ink, TextAlignmentOptions.TopLeft);
            for (int r = 0; r < 8; r++)
            {
                var t = ui.Text(XgUi.TopLeft("Leg", gaitCard, 14, RowTop(r), 40, RowH), LegNames[RowLeg[r]], 14, XgDark.Muted, TextAlignmentOptions.MidlineLeft);
                t.textWrappingMode = TextWrappingModes.NoWrap;
            }
            var key = ui.Text(XgUi.Rect("Key", gaitCard, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 6), new Vector2(-16, 26)),
                XgUi.Hex(XgDark.Good) + "■</color> L1 R2 L3 R4    " + XgUi.Hex(XgDark.Data) + "■</color> R1 L2 R3 L4    " + T("两组交替 = 经典的四足交替步态", "the two groups taking turns = the classic alternating tetrapod"),
                12, XgDark.Muted, TextAlignmentOptions.BottomLeft);
            key.textWrappingMode = TextWrappingModes.NoWrap;

            // The learning curve.
            var curveCard = ui.Card(page, "CurveCard", new Vector2(.5f, 0), new Vector2(1, 1), new Vector2(8, 16), new Vector2(-16, -492));
            curveLayer = Layer(curveCard, "Curve", DrawCurve);
            ui.Text(XgUi.TopLeft("Head", curveCard, 16, 10, 700, 22), T("学习曲线 · 每代最好的 5 秒距离", "Learning curve · best 5 s distance per generation"), 15, XgDark.Ink, TextAlignmentOptions.TopLeft);
            curveTop = ui.Text(XgUi.TopLeft("YMax", curveCard, 6, CurveTopPad - 9, 44, 18), "", 11, XgDark.Dim, TextAlignmentOptions.MidlineRight);
            curveRight = ui.Text(XgUi.Rect("XMax", curveCard, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-120, 30), new Vector2(-16, 46)), "", 11, XgDark.Dim, TextAlignmentOptions.BottomRight);
            var ckey = ui.Text(XgUi.Rect("Key", curveCard, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 6), new Vector2(-16, 26)),
                XgUi.Hex(XgDark.Good) + "—</color> " + T("距离", "distance") + "    " + XgUi.Hex(XgDark.Params) + "—</color> " + T("四足交替（0–1）", "tetrapod (0–1)") + "    " + XgUi.Hex(XgDark.Bad) + "|</color> " + T("摔倒", "falls"),
                12, XgDark.Muted, TextAlignmentOptions.BottomLeft);
            ckey.textWrappingMode = TextWrappingModes.NoWrap;
        }

        XgCrawlerLayer Layer(RectTransform card, string name, System.Action<VertexHelper> draw)
        {
            var rt = XgUi.Rect(name, card, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1));
            var layer = rt.gameObject.AddComponent<XgCrawlerLayer>();
            layer.raycastTarget = false;
            layer.draw = draw;
            return layer;
        }

        // ---------------------------------------------------------------- the walk

        void DrawWalk(VertexHelper vh)
        {
            if (walk == null) return;
            var r = walkLayer.rectTransform.rect;
            float groundY = r.yMin + 70, bodyY = r.center.y - 4;
            // The body moves by its interpolated distance; the camera follows once it passes 40% of the width.
            float steppedX = (float)(baseX + walk.distance), worldX = steppedX + (float)walk.speed * acc;
            camX = Mathf.Max(camX, worldX - r.width * .4f / Scale);
            float bodyScreenX = r.xMin + (worldX - camX) * Scale, feetScreenX = r.xMin + (steppedX - camX) * Scale;

            // Faint grid, then the dotted ruler with marks every 100.
            for (float gy = r.yMin + 20; gy < r.yMax; gy += 40) XgDraw.Box(vh, new Vector2(r.xMin, gy), new Vector2(r.xMax, gy + 1), XgDark.Grid);
            float firstUnit = Mathf.Floor(camX / 10) * 10, lastUnit = camX + r.width / Scale;
            for (float u = firstUnit; u <= lastUnit; u += 10)
            {
                float x = r.xMin + (u - camX) * Scale;
                bool major = Mathf.RoundToInt(u) % 100 == 0, mid = Mathf.RoundToInt(u) % 50 == 0;
                if (major) XgDraw.Box(vh, new Vector2(x - .75f, groundY - 12), new Vector2(x + .75f, groundY + 6), XgDark.Line);
                else XgDraw.Box(vh, new Vector2(x - 1, groundY - (mid ? 5 : 1)), new Vector2(x + 1, groundY + 1), mid ? XgDark.Line : XgDark.Dim);
            }
            PlaceRulerLabels(r, groundY);

            // Footprints: where each foot was planted, fading.
            for (int i = 0; i < Prints; i++)
            {
                float age = printAge[i];
                if (age > 4) continue;
                float x = r.xMin + (printAt[i].x - camX) * Scale;
                if (x < r.xMin - 4 || x > r.xMax + 4) continue;
                var c = GroupA(printLeg[i]) ? XgDark.Good : XgDark.Data; c.a = .55f * (1 - age / 4);
                XgDraw.Disc(vh, new Vector2(x, bodyY + printAt[i].y * Scale), 2.2f, c, 6);
            }

            // The spider: body from the sim, legs from the sim's feet.
            float wob = Mathf.Sin(Time.time * 42) * .16f * sag;
            walker.yaw = (float)walk.yaw + wob;
            walker.pos = new Vector2(bodyScreenX, bodyY);
            walker.bodyScale = 1 - .2f * sag;
            walker.eyeOpen = 1 - .6f * sag;
            for (int i = 0; i < walker.legs.Length; i++)
            {
                var l = walker.legs[i]; var s = walk.legs[i];
                l.hipWorld = walker.ToWorld(l.hip);
                // A swinging foot lifts and arcs a little outward; a fall pulls every foot in under the sagging body.
                float outward = (float)s.lift * 6;
                // Feet stand where the sim last put them; only the body glides between sim steps.
                var foot = new Vector2(feetScreenX + (float)s.footX * Scale, bodyY + ((float)s.footY + l.side * outward) * Scale);
                foot = Vector2.Lerp(foot, l.hipWorld + (foot - l.hipWorld) * .82f, sag);
                if (legBlend > 0) foot = Vector2.Lerp(foot, blendFrom[i], legBlend * legBlend);
                l.shown = l.foot = foot;
                l.stepping = !s.stance;
                l.knee = Knee(l.hipWorld, foot, walker.pos, walker.upper, walker.lower);
            }
            var eye = Color.Lerp(XgDark.Good, XgDark.Bad, Mathf.Max(sag, flash));
            if (flash > 0)
            {
                var red = XgDark.Bad; red.a = .35f * flash;
                XgSoftDraw.Halo(vh, walker.pos, 90 * Scale * (.6f + .4f * flash), red, 20);
            }
            walker.Draw(vh, LegInk, BodyInk, eye, 0, LegRim);
            if (flash > 0)
            {
                var wash = XgDark.Bad; wash.a = .12f * flash;
                XgDraw.Box(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMax), wash);
            }
        }

        void PlaceRulerLabels(Rect r, float groundY)
        {
            int first = Mathf.CeilToInt(camX / 100);
            for (int i = 0; i < rulerLabels.Length; i++)
            {
                int mark = first + i;
                float x = r.xMin + (mark * 100 - camX) * Scale;
                var t = rulerLabels[i];
                bool on = mark >= 0 && x < r.xMax - 10;
                if (t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
                if (!on) continue;
                if (rulerValue[i] != mark) { rulerValue[i] = mark; t.text = (mark * 100).ToString(); }
                t.rectTransform.anchoredPosition = new Vector2(x, groundY - 22);
            }
        }

        /// <summary>
        /// The knee of a two-segment leg (law of cosines), on the side of the hip→foot line away from the body, as a
        /// spider's leg arches seen from above: front legs bend forward-out, back legs back-out. Picking the knee that is
        /// merely "more sideways" put it on the foot row for a reaching leg, and the legs drew as crossing Z shapes.
        /// </summary>
        static Vector2 Knee(Vector2 hip, Vector2 foot, Vector2 body, float upper, float lower)
        {
            var d = foot - hip;
            float len = d.magnitude;
            var away = hip - body;
            if (len < 1e-3f) return hip + (away.sqrMagnitude > 1e-6f ? away.normalized : Vector2.up) * upper;
            float reach = Mathf.Clamp(len, Mathf.Abs(upper - lower) + .01f, upper + lower - .01f);
            float a = Mathf.Acos(Mathf.Clamp((upper * upper + reach * reach - lower * lower) / (2 * upper * reach), -1, 1));
            float baseAngle = Mathf.Atan2(d.y, d.x);
            var k1 = hip + new Vector2(Mathf.Cos(baseAngle + a), Mathf.Sin(baseAngle + a)) * upper;
            var k2 = hip + new Vector2(Mathf.Cos(baseAngle - a), Mathf.Sin(baseAngle - a)) * upper;
            // The normal of the hip→foot line that points away from the body.
            var outward = new Vector2(-d.y, d.x);
            if (Vector2.Dot(outward, (hip + foot) * .5f - body) < 0) outward = -outward;
            return Vector2.Dot(k1 - hip, outward) >= Vector2.Dot(k2 - hip, outward) ? k1 : k2;
        }

        // ---------------------------------------------------------------- the gait diagram

        const float GaitTop = 44, RowH = 30, RowGap = 6, SideGap = 14, GaitLeft = 52;
        static float RowTop(int row) => GaitTop + row * (RowH + RowGap) + (row >= 4 ? SideGap : 0);

        void DrawGait(VertexHelper vh)
        {
            if (walk == null) return;
            var r = gaitLayer.rectTransform.rect;
            var g = walk.genes;
            float x0 = r.xMin + GaitLeft, x1 = r.xMax - 16, w = x1 - x0;
            // Cycle boundaries.
            for (int c = 0; c <= 2; c++)
            {
                float x = x0 + w * c / 2;
                XgDraw.Box(vh, new Vector2(x - .5f, r.yMax - RowTop(7) - RowH - 4), new Vector2(x + .5f, r.yMax - GaitTop + 4), XgDark.Line);
            }
            for (int row = 0; row < 8; row++)
            {
                int leg = RowLeg[row];
                float top = r.yMax - RowTop(row), bottom = top - RowH;
                XgDraw.Box(vh, new Vector2(x0, bottom), new Vector2(x1, top), XgDark.Track);
                var col = GroupA(leg) ? XgDark.Good : XgDark.Data;
                // Planted while frac(u + phase) < duty, u in cycles: [k − phase, k − phase + duty).
                double ph = g.phase[leg];
                for (int k = -1; k <= 3; k++)
                {
                    double a = k - ph, b = a + g.duty;
                    if (b <= 0 || a >= 2) continue;
                    float xa = x0 + w * (float)System.Math.Max(0, a) / 2, xb = x0 + w * (float)System.Math.Min(2, b) / 2;
                    XgDraw.Box(vh, new Vector2(xa, bottom + 3), new Vector2(xb, top - 3), col);
                }
            }
            // The playhead: where the walking spider is in its cycle now.
            float u = (float)(((g.freq * (walk.time + acc)) % 2 + 2) % 2);
            float px = x0 + w * u / 2;
            var head = XgDark.Ink; head.a = .8f;
            XgDraw.Box(vh, new Vector2(px - 1, r.yMax - RowTop(7) - RowH - 6), new Vector2(px + 1, r.yMax - GaitTop + 6), head);
        }

        // ---------------------------------------------------------------- the learning curve

        const float CurveTopPad = 48, CurveBottomPad = 52, CurveLeft = 56;

        void DrawCurve(VertexHelper vh)
        {
            if (learner == null) return;
            var r = curveLayer.rectTransform.rect;
            float x0 = r.xMin + CurveLeft, x1 = r.xMax - 20, y0 = r.yMin + CurveBottomPad, y1 = r.yMax - CurveTopPad;
            var hist = learner.History;
            int gens = Mathf.Max(80, hist.Count - 1);
            double most = 100;
            for (int i = 0; i < hist.Count; i++) if (hist[i].distance > most) most = hist[i].distance;
            int top = Mathf.CeilToInt((float)(most * 1.1 / 100)) * 100;
            // Labels are set in Update: text changed inside a mesh rebuild is not redrawn until something else dirties it.
            curveTopValue = top; curveGens = gens;

            // Grid: quarters of the height.
            for (int q = 0; q <= 4; q++)
            {
                float y = Mathf.Lerp(y0, y1, q / 4f);
                XgDraw.Box(vh, new Vector2(x0, y - .5f), new Vector2(x1, y + .5f), q == 0 ? XgDark.Line : XgDark.Hairline);
            }
            curvePoints.Clear(); tetPoints.Clear();
            for (int i = 0; i < hist.Count; i++)
            {
                float x = x0 + (x1 - x0) * i / gens;
                curvePoints.Add(new Vector2(x, y0 + (y1 - y0) * (float)(hist[i].distance / top)));
                if (i < tetrapodHistory.Count) tetPoints.Add(new Vector2(x, y0 + (y1 - y0) * tetrapodHistory[i]));
                if (hist[i].falls > 0)
                {
                    float h = 6 + 2.2f * Mathf.Min(hist[i].falls, 16);
                    XgDraw.Box(vh, new Vector2(x - 1, y0 - 4 - h), new Vector2(x + 1, y0 - 4), XgDark.Bad);
                }
            }
            if (tetPoints.Count >= 2) XgDraw.Polyline(vh, tetPoints, 1.5f, XgDark.Params);
            if (curvePoints.Count >= 2) XgDraw.Polyline(vh, curvePoints, 2.5f, XgDark.Good);
            var last = curvePoints[curvePoints.Count - 1];
            XgDraw.Disc(vh, last, 4, XgDark.Good, 10);
        }
    }
}
