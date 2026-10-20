using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    public static class XgActionGames
    {
        public static XgMiniGame Create(string id, XgMiniContext c)
        {
            switch (id)
            {
                case "draw": return new XgActionDraw(c);
                case "slice": return new XgActionSlice(c);
                case "swipe": return new XgActionSwipe(c);
                case "dadishu": return new XgActionMoles(c);
                case "danmushooter": return new XgActionShooter(c);
                case "spidersnake": return new XgActionSnake(c);
                case "zhaobutong": return new XgActionDifferences(c);
                default: return null;
            }
        }
    }
    public abstract class XgActionBase : XgMiniGame
    {
        protected static readonly Color Blue = new Color(.23f, .39f, .69f), Purple = new Color(.55f, .36f, .8f), Green = new Color(.15f, .62f, .41f), Red = new Color(.84f, .26f, .25f);
        protected TMP_Text status, feedback;
        protected float time;
        protected int score, errors, combo;
        protected bool playing;
        protected XgActionBase(XgMiniContext c) : base(c) { }
        protected void Header(string title, string instruction, float seconds = 60)
        {
            time = seconds; score = errors = combo = 0; playing = true;
            C.Text("Title", 15, 4, 700, 30, title, 23);
            C.Text("Rule", 15, 36, 860, 42, instruction, 15);
            status = C.Text("Status", 15, 78, 690, 28, "", 17);
            feedback = C.Text("Feedback", 15, 443, 690, 32, XgUi.T("看准了再动手。", "Take your time and look closely."), 15);
            C.Button("Restart", 742, 443, 142, 30, XgUi.T("重新开始", "Restart"), Restart, 16);
            Hud();
        }
        protected void Restart()
        {
            playing = false;
            for (int i = C.Root.childCount - 1; i >= 0; i--) { var go = C.Root.GetChild(i).gameObject; go.SetActive(false); XgMiniContext.DestroyObject(go); }
            Build();
        }
        protected void Hud() { status.text = XgUi.T($"剩余 {Mathf.CeilToInt(time)} 秒   正确 {score}   失误 {errors}   连击 {combo}", $"{Mathf.CeilToInt(time)} s left   right {score}   wrong {errors}   streak {combo}"); }
        protected bool Clock(float dt)
        {
            if (!playing || !C.IsActive) return false;
            time = Mathf.Max(0, time - Mathf.Max(0, dt)); Hud();
            if (time <= 0) { End(XgUi.T("时间到", "Time is up")); return false; }
            return true;
        }
        protected void Result(bool correct, string explanation)
        {
            if (!playing) return;
            if (correct) { score++; combo++; } else { errors++; combo = 0; }
            C.Report(correct); feedback.text = explanation; Hud();
        }
        protected void End(string reason) { playing = false; feedback.text = XgUi.T($"{reason}：正确 {score}，失误 {errors}。可重新开始。", $"{reason}: {score} right, {errors} wrong. You can restart."); }
        protected static void Place(RectTransform rect, float x, float y)
        {
            // XgUi.TopLeft retains the centre pivot; anchoredPosition is not a top-left coordinate.
            Vector2 size = rect.sizeDelta;
            rect.offsetMin = new Vector2(x, -y - size.y);
            rect.offsetMax = new Vector2(x + size.x, -y);
        }
        protected bool Opinion(bool truth) => XgMiniContent.Opinion(C.Random, C.Skill, truth);
        protected XgActionGraphic Art(float x, float y, float w, float h)
        { var a = C.Rect("原生图形", x, y, w, h).gameObject.AddComponent<XgActionGraphic>(); a.raycastTarget = false; return a; }
    }

    /// <summary>Single native uGUI mesh for grids, trails, and procedural scene art.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgActionGraphic : MaskableGraphic
    {
        struct Shape { public Vector2 a, b; public float width; public Color color; public bool line; }
        readonly List<Shape> shapes = new List<Shape>();
        public void Clear() { shapes.Clear(); }
        public void Box(float x, float y, float w, float h, Color c) { shapes.Add(new Shape { a = new Vector2(x, y), b = new Vector2(w, h), color = c }); }
        public void Line(Vector2 a, Vector2 b, float width, Color c) { shapes.Add(new Shape { a = a, b = b, width = width, color = c, line = true }); }
        public void Refresh() { SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            foreach (var s in shapes)
            {
                Vector2 a, b, c, d;
                if (s.line)
                {
                    var n = (s.b - s.a).normalized; n = new Vector2(-n.y, n.x) * s.width * .5f;
                    a = s.a + n; b = s.b + n; c = s.b - n; d = s.a - n;
                }
                else { a = s.a; b = s.a + new Vector2(s.b.x, 0); c = s.a + s.b; d = s.a + new Vector2(0, s.b.y); }
                int start = vh.currentVertCount;
                vh.AddVert(new Vector3(r.xMin + a.x, r.yMax - a.y), s.color, Vector2.zero);
                vh.AddVert(new Vector3(r.xMin + b.x, r.yMax - b.y), s.color, Vector2.zero);
                vh.AddVert(new Vector3(r.xMin + c.x, r.yMax - c.y), s.color, Vector2.zero);
                vh.AddVert(new Vector3(r.xMin + d.x, r.yMax - d.y), s.color, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }

    sealed class XgActionDraw : XgActionBase
    {
        readonly bool[] pixels = new bool[64 * 64];
        XgActionGraphic ink;
        TMP_Text targetText, guessText;
        Vector2 last;
        int target, rounds, strokeCount;
        bool completed;
        // Reads with the same recogniser and memory as the digits desk on 摆渡众包 (XgSim.Handwriting.cs).
        public XgActionDraw(XgMiniContext c) : base(c) { }
        public override void Build()
        {
            Header(XgUi.T("你画我猜", "Draw and Guess"), XgUi.T("在白板上写出指定数字，提交后" + C.Sim.AiDisplayName + "按你的笔迹来读，交上去的字它都会记住。", "Write the asked digit on the board; " + C.Sim.AiDisplayName + " reads your handwriting and remembers every drawing you submit."), 120);
            rounds = 0;
            var pad = C.Box("画板", 50, 117, 310, 310, Color.white);
            ink = Art(50, 117, 310, 310);
            C.Pointer(pad.rectTransform, p => { if (playing && !completed) { last = Clamp(p); Stroke(last + new Vector2(.1f, 0)); } }, Stroke);
            targetText = C.Text("目标数字", 410, 123, 450, 65, "", 34);
            guessText = C.Text("识别说明", 410, 200, 455, 95, "", 20);
            C.Button("提交笔迹", 415, 309, 190, 48, "画好了 · 识别", Submit);
            C.Button("清空笔迹", 630, 309, 160, 48, "清空", Clear);
            C.Button("下一数字", 415, 374, 190, 43, "下一题", Next);
            Next();
        }
        Vector2 Clamp(Vector2 p) => new Vector2(Mathf.Clamp(p.x, 4, 306), Mathf.Clamp(p.y, 4, 306));
        void Stroke(Vector2 p)
        {
            if (!playing || completed) return;
            if (strokeCount++ >= 6000) { feedback.text = "笔迹已达上限，请提交或清空。"; return; }
            p = Clamp(p); ink.Line(last, p, 13, new Color(.12f, .14f, .19f)); ink.Refresh();
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(last, p)));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 q = Vector2.Lerp(last, p, i / (float)steps) * (64f / 310);
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                { int xx = Mathf.Clamp((int)q.x + x, 0, 63), yy = Mathf.Clamp((int)q.y + y, 0, 63); pixels[yy * 64 + xx] = true; }
            }
            last = p;
        }
        void Clear() { if (!playing || completed) return; Array.Clear(pixels, 0, pixels.Length); strokeCount = 0; ink.Clear(); ink.Refresh(); }
        void Next()
        {
            if (!playing) return; completed = false; Clear(); target = C.Random.Next(10);
            targetText.text = "请写数字：" + target;
            guessText.text = XgUi.T("内置字模 " + C.Sim.Handwriting.BuiltInCount + " 个 + 你的字 " + C.Sim.HandwritingMemory + " 张", C.Sim.Handwriting.BuiltInCount + " built-in letterforms + " + C.Sim.HandwritingMemory + " of your drawings");
        }
        void Submit()
        {
            if (!playing || completed) return;
            var reading = C.Sim.ReadHandwriting(pixels);
            if (!reading.valid) { feedback.text = XgUi.T("笔迹太少，请写完整数字；空白和单点不计分。", "Too little ink: write the whole digit. Blank pages and dots are not counted."); return; }
            int guess = reading.digit; completed = true; rounds++;
            C.Sim.LearnHandwriting(target, pixels);
            Result(guess == target, guess == target ? XgUi.T("读对了，这张字它也记住了。", "Read right, and it remembered this one too.") : XgUi.T($"它读成了 {guess}，题目是 {target}。这张已记住，下次再试。", $"It read {guess}, the digit was {target}. It remembered this one; try again."));
            guessText.text = XgUi.T($"它读成：{guess}\n题目：{target} · 已交 {rounds} 张", $"It read: {guess}\nAsked: {target} · {rounds} handed in");
        }
        public override void Tick(float dt) { Clock(dt); }
    }

    sealed class XgActionSlice : XgActionBase
    {
        sealed class Message { public float x, y, vx, vy; public int lane; public bool bad, used; public TMP_Text text; public Image card; }
        readonly List<Message> items = new List<Message>();
        Vector2 last;
        float spawn;
        int hearts, missed;
        XgActionGraphic trail;
        float trailTime;
        public XgActionSlice(XgMiniContext c) : base(c) { }
        public override void Build()
        {
            Header(XgUi.T("切短信", "Slice the SMS"), XgUi.T("按住鼠标划过飞起的卡片：切掉垃圾短信，放过正常短信。误切三次就结束。", "Hold the mouse and sweep across the flying cards: slice the spam, keep the normal messages. Three wrong cuts end the round."));
            items.Clear(); spawn = 0; hearts = 3; missed = 0;
            var pad = C.Box("Board", 15, 112, 870, 320, new Color(.09f, .15f, .23f));
            trail = Art(15, 112, 870, 320);
            C.Pointer(pad.rectTransform, p => { last = p; }, Swipe);
        }
        void Swipe(Vector2 p)
        {
            if (!playing) return;
            p = new Vector2(Mathf.Clamp(p.x, 0, 870), Mathf.Clamp(p.y, 0, 320));
            if (Vector2.Distance(last, p) < 3) return;
            trail.Clear(); trail.Line(last, p, 4, Color.white); trail.Refresh(); trailTime = .15f;
            foreach (var m in items)
            {
                if (m.used || m.y < 0 || m.y > 262 || !XgMiniActionRules.SegmentHits(last.x, last.y, p.x, p.y, m.x, m.y, 235, 58)) continue;
                m.used = true; m.text.gameObject.SetActive(false); m.card.gameObject.SetActive(false);
                Result(m.bad, m.bad ? XgUi.T("切对：这是垃圾短信。", "Right cut: that was spam.") : XgUi.T("误切正常短信，损失一点生命。", "You cut a normal message and lose a life."));
                if (!m.bad && --hearts <= 0) { End(XgUi.T("三次误切", "Three wrong cuts")); break; }
            }
            last = p;
        }
        public override void Tick(float dt)
        {
            if (!Clock(dt)) return;
            dt = Mathf.Min(dt, .1f); spawn -= dt;
            if (spawn <= 0)
            {
                bool wrong = C.Random.Next(2) == 0; var bank = wrong ? XgMiniContent.SpamMessages : XgMiniContent.NormalMessages; bool opinion = Opinion(wrong);
                var line = bank[C.Random.Next(bank.Length)];
                // Three lanes, and the one whose latest card is furthest from the launch point, so cards do not pile on each other.
                int lane = 0; float roomest = -1;
                for (int k = 0; k < 3; k++)
                {
                    float nearest = 1000; foreach (var o in items) if (!o.used && o.lane == k) nearest = Mathf.Min(nearest, Mathf.Abs(o.y - 290));
                    if (nearest > roomest) { roomest = nearest; lane = k; }
                }
                var m = new Message { x = lane * 285 + C.Random.Next(0, 16), y = 290, vx = C.Random.Next(-12, 13), vy = -255 - C.Random.Next(70), bad = wrong, lane = lane };
                m.card = C.Box("Card", 15 + m.x, 112 + m.y, 235, 58, new Color(.2f, .29f, .43f));
                m.text = C.Text("Flying", 15 + m.x, 112 + m.y, 235, 58, XgUi.T(line[0], line[1]) + "\n" + C.Sim.AiDisplayName + XgUi.T("：", ": ") + (opinion ? XgUi.T("切掉", "slice") : XgUi.T("保留", "keep")), 17, Color.white); m.text.raycastTarget = false;
                items.Add(m); spawn = Mathf.Lerp(.55f, 1.05f, time / 60);
            }
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var m = items[i]; m.x = Mathf.Clamp(m.x + m.vx * dt, 0, 635); m.y += m.vy * dt; m.vy += 205 * dt;
                if (m.used || m.y > 330)
                {
                    if (!m.used && m.bad) { missed++; combo = 0; feedback.text = XgUi.T("漏掉垃圾短信。漏掉 " + missed + " 条；没切的不算答题。", "Spam got through. " + missed + " missed; a message you leave alone is not an answer."); }
                    m.text.gameObject.SetActive(false); m.card.gameObject.SetActive(false); XgMiniContext.DestroyObject(m.text.gameObject); XgMiniContext.DestroyObject(m.card.gameObject); items.RemoveAt(i); continue;
                }
                Place(m.text.rectTransform, 15 + m.x, 112 + m.y);
                // The native text is hidden below the board until the projectile rises.
                Place(m.card.rectTransform, 15 + m.x, 112 + m.y);
                m.text.enabled = m.card.enabled = m.y >= 0 && m.y <= 262;
            }
            if ((trailTime -= dt) <= 0) { trail.Clear(); trail.Refresh(); }
            status.text += XgUi.T($"   生命 {hearts}   漏掉 {missed}", $"   lives {hearts}   missed {missed}");
        }
    }

    sealed class XgActionSwipe : XgActionBase
    {
        RectTransform card;
        TMP_Text headline, hint;
        int current;
        float dragStart, hintDelay;
        bool aiBait;
        readonly Queue<int> deck = new Queue<int>();
        public XgActionSwipe(XgMiniContext c) : base(c) { }
        public override void Build()
        {
            Header(XgUi.T("左滑右滑 · 新闻审核", "Swipe left, swipe right"), XgUi.T("左滑 / 左方向键：标题党。右滑 / 右方向键：正经新闻。拖动卡片至少 80 像素提交。", "Left / left arrow: clickbait. Right / right arrow: honest news. Drag the card at least 80 pixels to hand it in."));
            var bg = C.Box("Card", 230, 125, 440, 240, Color.white); card = bg.rectTransform;
            headline = C.Text("Headline", 260, 170, 380, 110, "", 25); headline.raycastTarget = false;
            hint = C.Text("Hint", 260, 300, 380, 48, "", 17, Purple); hint.raycastTarget = false;
            C.Pointer(card, p => dragStart = p.x, p => Move(card.offsetMin.x - 230 + p.x - dragStart), p => { float x = card.offsetMin.x - 230; if (Mathf.Abs(x) >= 80) Decide(x < 0); else Move(0); });
            C.Button("Left", 150, 380, 220, 43, XgUi.T("← 标题党", "← Clickbait"), () => Decide(true));
            C.Button("Right", 530, 380, 220, 43, XgUi.T("正经新闻 →", "Honest news →"), () => Decide(false));
            deck.Clear(); Next();
        }
        void Move(float delta)
        {
            if (!playing) return; float x = Mathf.Clamp(delta, -190, 190); Place(card, 230 + x, 125);
            Place(headline.rectTransform, 260 + x, 170); Place(hint.rectTransform, 260 + x, 300);
        }
        void Next()
        {
            if (deck.Count == 0)
            {
                var pool = new List<int>(); for (int i = 0; i < XgMiniContent.Headlines.Length; i++) pool.Add(i);
                while (pool.Count > 0) { int n = C.Random.Next(pool.Count); deck.Enqueue(pool[n]); pool.RemoveAt(n); }
            }
            current = deck.Dequeue(); headline.text = XgUi.T(XgMiniContent.Headlines[current][0], XgMiniContent.Headlines[current][1]); aiBait = Opinion(current < XgMiniContent.ClickbaitCount); hint.text = XgUi.T(C.Sim.AiDisplayName + "正在读这张卡片……", C.Sim.AiDisplayName + " is reading this card..."); hintDelay = .3f + (float)(1 - C.Skill) * 1.4f; Move(0);
        }
        void Decide(bool bait)
        { if (!playing) return; Result(bait == (current < XgMiniContent.ClickbaitCount), (current < XgMiniContent.ClickbaitCount) ? XgUi.T("标题党：夸张、恐吓、强迫转发或隐去关键信息。", "Clickbait: hype, scare tactics, forced sharing or a hidden key fact.") : XgUi.T("正经新闻：具体事件与主体清晰；文风不是事实核查的替代。", "Honest news: a concrete event and a clear subject. Style is no substitute for fact-checking.")); Next(); }
        public override void Tick(float dt)
        {
            if (!Clock(dt)) return;
            if ((hintDelay -= dt) <= 0) hint.text = XgUi.T(C.Sim.AiDisplayName + "建议：", C.Sim.AiDisplayName + " suggests: ") + (aiBait ? XgUi.T("左滑，标题党", "left, clickbait") : XgUi.T("右滑，正经新闻", "right, honest news"));
            if (C.KeyDown(Key.LeftArrow)) C.PlayerInput(() => Decide(true)); else if (C.KeyDown(Key.RightArrow)) C.PlayerInput(() => Decide(false));
        }
    }

    sealed class XgActionMoles : XgActionBase
    {
        const int Cells = 9;
        const float PopSeconds = .18f, SinkSeconds = .22f;
        readonly XgMoleGraphic[] holes = new XgMoleGraphic[Cells];
        readonly TMP_Text[] signs = new TMP_Text[Cells], bangs = new TMP_Text[Cells];
        readonly float[] expires = new float[Cells], rise = new float[Cells];
        readonly bool[] wrong = new bool[Cells];
        readonly string[] truth = new string[Cells], aiAnswers = new string[Cells];
        readonly TMP_Text[] aiSigns = new TMP_Text[Cells];
        readonly XgMoleQuestion[] questions = new XgMoleQuestion[Cells];
        readonly XgMoleTurn[] turns = new XgMoleTurn[Cells];
        readonly XgPredictionTrace[] predictions = new XgPredictionTrace[Cells];
        readonly float[] autoDelay = new float[Cells];
        XgBtn autoButton;
        RectTransform topicMenu;
        TMP_Text questionDetail;
        int hovered = -1;
        RectTransform reviewRoot;
        TMP_Text reviewQuestion, reviewConditions, reviewResult;
        XgBtn reviewTrue, reviewFalse;
        int reviewSlot, reviewRevision;
        bool reviewing;
        int buildGeneration;
        bool autoEnabled;
        float spawn;
        int missed,goldenMilestone,goldenBonus;
        public XgActionMoles(XgMiniContext c) : base(c) { }
        bool Target(int i) => C.Id == "arith_mole" ? !wrong[i] : wrong[i];
        public override void Build()
        {
            int generation = ++buildGeneration;
            autoEnabled = C.Sim.MoleAutoAvailable(C.Id);
            Header(XgUi.T("算术打地鼠", "Arithmetic whack-a-mole"), C.Id == "arith_mole" ? XgUi.T("敲正确的算式，放过错误的算式。", "Whack correct equations; leave wrong ones alone.") : XgUi.T("敲错误的算式，放过正确的算式。", "Whack wrong equations; leave correct ones alone."));
            // Restart destroys old children at end-of-frame; bind only the new active controls.
            foreach (var restart in C.Root.GetComponentsInChildren<Button>()) if (restart.name == "Restart")
            { restart.onClick.RemoveAllListeners(); restart.onClick.AddListener(() => { if (generation == buildGeneration && C.IsActive) Restart(); }); }
            hovered = -1;
            reviewing = false; C.Sim.ClearMoleLessons();
            questionDetail = null;
            foreach (var label in C.Root.GetComponentsInChildren<TMP_Text>()) if (label.name == "Rule") questionDetail = label;
            Array.Clear(expires, 0, Cells); Array.Clear(rise, 0, Cells); missed = goldenMilestone = goldenBonus = 0; spawn = .2f;
            autoButton = C.Button("MoleAuto", 545, 78, 190, 28, XgUi.T("自动：", "Auto: ") + (autoEnabled ? XgUi.T("开", "On") : XgUi.T("关", "Off")), () =>
            {
                if (generation != buildGeneration) return;
                autoEnabled = !autoEnabled && C.Sim.MoleAutoAvailable(C.Id);
                autoButton.Set(XgUi.T("自动：", "Auto: ") + (autoEnabled ? XgUi.T("开", "On") : XgUi.T("关", "Off")), true);
                feedback.text = !C.Sim.MoleAutoAvailable(C.Id)
                    ? XgUi.T("阶段2购买算术自动作答后开放。", "Buy arithmetic auto-answer in stage 2.")
                    : XgUi.T("自动作答已", "Auto-answer ") + (autoEnabled ? XgUi.T("开启", "on") : XgUi.T("关闭", "off"));
            }, 13);
            C.Button("MoleReview", 745, 78, 135, 28, XgUi.T("题目回顾", "Review"), () => { if (generation == buildGeneration) OpenReview(); }, 14);
            status.rectTransform.sizeDelta = new Vector2(285, 28);
            C.Button("MoleTopic", 305, 78, 230, 28, TopicLabel(C.Sim.MoleTopic), () =>
            {
                if (generation == buildGeneration && topicMenu != null) topicMenu.gameObject.SetActive(!topicMenu.gameObject.activeSelf);
            }, 13);
            C.Box("Lawn", 15, 112, 870, 320, new Color(.36f, .56f, .33f));
            float[] x = { 42, 327, 612 };
            for (int i = 0; i < Cells; i++)
            {
                int slot = i;
                if (turns[i] == null) turns[i] = new XgMoleTurn();
                turns[i].Invalidate(); predictions[i] = null; questions[i] = null;
                var g = C.Rect("Hole" + i, x[i % 3], 122 + i / 3 * 102, XgMoleGraphic.CellWidth, XgMoleGraphic.CellHeight).gameObject.AddComponent<XgMoleGraphic>();
                g.raycastTarget = true; g.HitTime = -1;
                g.Down = () => { C.Ui.window?.FocusToWindow(); if (generation == buildGeneration && C.IsActive) C.PlayerInput(() => Hit(slot, turns[slot].Token, false)); };
                holes[i] = g;
                g.Hover = entered =>
                {
                    if (generation != buildGeneration || !C.IsActive) return;
                    hovered = entered && expires[slot] > 0 ? slot : -1;
                    RefreshQuestionDetail();
                };
            }
            // The labels sit over their cells, so they are made after every graphic.
            for (int i = 0; i < Cells; i++)
            {
                var t = C.Text("Sum" + i, 0, 0, 226, 42, "", 18, new Color(.2f, .12f, .05f));
                t.enableAutoSizing = false; t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow;
                t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.enabled = false; signs[i] = t;
                var ai = C.Text("AiSum"+i,0,0,180,22,"",11,new Color(.45f,.28f,.65f));ai.raycastTarget=false;ai.enabled=false;aiSigns[i]=ai;
                var b = C.Text("Bang" + i, 0, 0, 110, 44, "", 34, new Color(.86f, .2f, .12f));
                b.alignment = TextAlignmentOptions.Center; b.fontStyle = FontStyles.Bold; b.raycastTarget = false; b.enabled = false; bangs[i] = b;
            }
            BuildReview();
            topicMenu = C.Rect("MoleTopicMenu", 300, 108, 280, 164);
            var topicBackground = topicMenu.gameObject.AddComponent<Image>(); topicBackground.color = new Color(.10f,.14f,.20f,.98f);
            for (int topicIndex = 0; topicIndex < 4; topicIndex++)
            {
                var topic = (XgMoleTopic)topicIndex;
                bool available = C.Sim.MoleTopicAvailable(topic);
                var option = C.Button("MoleTopic" + topicIndex, 308, 114 + topicIndex * 39, 264, 34,
                    TopicLabel(topic) + (available ? "" : XgUi.T(" · 未购买", " · Locked")), () =>
                    {
                        if (generation != buildGeneration || !C.IsActive) return;
                        if (!C.Sim.SelectMoleTopic(topic))
                        {
                            feedback.text = XgUi.T("先在科技购买对应题包。", "Buy this question pack in Technology first.");
                            topicMenu.gameObject.SetActive(false); return;
                        }
                        Restart();
                    }, 13);
                option.rt.SetParent(topicMenu, true);
            }
            topicMenu.gameObject.SetActive(false);
        }
        static string TopicLabel(XgMoleTopic topic)
        {
            switch (topic)
            {
                case XgMoleTopic.Mixed: return XgUi.T("混合运算", "Mixed operations");
                case XgMoleTopic.Powers: return XgUi.T("幂指对与根式", "Powers, logs and roots");
                case XgMoleTopic.Calculus: return XgUi.T("极限 / 求导 / 积分", "Limits / derivatives / integrals");
                default: return XgUi.T("加减乘除", "Basic arithmetic");
            }
        }
        void BuildReview()
        {
            int generation = buildGeneration;
            reviewRoot = C.Rect("MoleReviewLayer", 0, 0, 900, 480);
            var panel = C.Box("MoleReviewBackground", 12, 108, 876, 330, C.Ui.dark ? XgDark.Card : new Color(.97f, .97f, .94f));
            panel.raycastTarget = true; panel.transform.SetParent(reviewRoot, false);
            reviewQuestion = C.Text("ReviewQuestion", 35, 124, 825, 64, "", 24);
            reviewQuestion.transform.SetParent(reviewRoot, false); reviewQuestion.enableAutoSizing = false;
            reviewQuestion.textWrappingMode = TextWrappingModes.Normal; reviewQuestion.overflowMode = TextOverflowModes.Overflow;
            reviewConditions = C.Text("ReviewConditions", 35, 196, 825, 62, "", 16);
            reviewConditions.transform.SetParent(reviewRoot, false);
            reviewResult = C.Text("ReviewResult", 35, 266, 825, 66, "", 17);
            reviewResult.transform.SetParent(reviewRoot, false);
            reviewTrue = C.Button("ReviewTrue", 35, 344, 195, 36, XgUi.T("题目正确", "Statement is true"), null, 17);
            reviewFalse = C.Button("ReviewFalse", 242, 344, 195, 36, XgUi.T("题目错误", "Statement is false"), null, 17);
            BindReviewInput(reviewTrue, true, generation);
            BindReviewInput(reviewFalse, false, generation);
            reviewTrue.rt.SetParent(reviewRoot, false); reviewFalse.rt.SetParent(reviewRoot, false);
            var prev = C.Button("ReviewPrevious", 465, 344, 102, 36, XgUi.T("上一题", "Previous"), () => { if (generation == buildGeneration) StepReview(-1); }, 15);
            var next = C.Button("ReviewNext", 577, 344, 102, 36, XgUi.T("下一题", "Next"), () => { if (generation == buildGeneration) StepReview(1); }, 15);
            var back = C.Button("ReviewBack", 691, 344, 168, 36, XgUi.T("返回对局", "Back to game"), () => { if (generation != buildGeneration) return; reviewing = false; reviewRoot.gameObject.SetActive(false); }, 16);
            prev.rt.SetParent(reviewRoot, false); next.rt.SetParent(reviewRoot, false); back.rt.SetParent(reviewRoot, false);
            var note = C.Text("ReviewNote", 35, 390, 825, 36, XgUi.T("亲自判断一次，教它这道题；回顾不重复结算。", "Judge the statement yourself to teach this example. Reviewing does not pay again."), 14);
            note.transform.SetParent(reviewRoot, false);
            reviewRoot.gameObject.SetActive(false);
        }
        void OpenReview()
        {
            if (!C.IsActive) return;
            long latest = 0; reviewSlot = -1;
            for (int i = 0; i < Cells; i++) if (questions[i] != null && turns[i].Token > latest) { latest = turns[i].Token; reviewSlot = i; }
            if (reviewSlot < 0) { feedback.text = XgUi.T("先看一道题，再来回顾。", "Wait for a question before reviewing."); return; }
            reviewing = true; reviewRoot.gameObject.SetActive(true); RefreshReview();
        }
        void StepReview(int direction)
        {
            if (!reviewing || !C.IsActive) return;
            for (int n = 1; n <= Cells; n++)
            {
                int slot = (reviewSlot + direction * n + Cells * 2) % Cells;
                if (questions[slot] == null) continue;
                reviewSlot = slot; RefreshReview(); return;
            }
        }
        void RefreshReview()
        {
            reviewRevision++; // Even away-and-back navigation invalidates a held judgment.
            var q = questions[reviewSlot];
            reviewQuestion.text = q.Text;
            reviewConditions.text = XgUi.T(q.ConditionsZh, q.ConditionsEn);
            bool answered = C.Sim.MoleLessonAnswered(turns[reviewSlot].Token);
            reviewTrue.button.interactable = reviewFalse.button.interactable = !answered;
            reviewResult.text = answered ? XgUi.T(q.Why, q.WhyEn) : XgUi.T("这道题说得对吗？", "Is this statement true?");
        }
        void BindReviewInput(XgBtn button, bool label, int generation)
        {
            var input = button.rt.gameObject.AddComponent<XgMoleReviewInput>();
            input.Available = () => generation == buildGeneration && reviewing && reviewSlot >= 0 && C.IsActive;
            input.Capture = () =>
            {
                if (!input.Available()) return null;
                int revision = reviewRevision;
                long token = turns[reviewSlot].Token;
                var question = questions[reviewSlot];
                var sim = C.Sim;
                var owner = XingGuangController.Instance;
                bool used = false;
                return () =>
                {
                    if (used) return;
                    used = true;
                    if (!input.Available() || revision != reviewRevision
                        || owner != null && (!ReferenceEquals(owner, XingGuangController.Instance) || !ReferenceEquals(owner.Sim, sim))
                        || !ReferenceEquals(question, questions[reviewSlot]) || token != turns[reviewSlot].Token
                        || sim.MoleLessonAnswered(token)) return;
                    C.PlayerInput(() => TeachReviewed(token, label));
                };
            };
        }
        void TeachReviewed(long token, bool label)
        {
            if (!reviewing || reviewSlot < 0 || !C.IsActive) return;
            var result = C.Sim.TeachMoleStatement(token, label);
            if (!result.accepted) { reviewResult.text = XgUi.T("这道题已失效，请回到对局。", "This question has expired. Return to the game."); return; }
            var owner = XingGuangController.Instance;
            if (owner != null && ReferenceEquals(owner.Sim, C.Sim)) owner.runtime?.MarkDirty();
            RefreshReview();
            reviewResult.text = (result.trained ? XgUi.T("已教给它。", "Taught to it.") : XgUi.T("再看看题意。", "Check the statement again.")) + "\n" + reviewResult.text;
        }
        void RefreshQuestionDetail()
        {
            if (questionDetail == null) return;
            if (hovered >= 0 && expires[hovered] > 0 && questions[hovered] != null)
            {
                var q = questions[hovered];
                questionDetail.text = q.Text + "\n" + XgUi.T(q.ConditionsZh, q.ConditionsEn);
            }
            else questionDetail.text = (C.Id == "arith_mole"
                ? XgUi.T("敲正确的算式，放过错误的算式。", "Whack correct equations; leave wrong ones alone.")
                : XgUi.T("敲错误的算式，放过正确的算式。", "Whack wrong equations; leave correct ones alone."))
                + XgUi.T(" 移到题牌上查看完整题意。", " Hover over a card for its full wording.");
        }
        void Hit(int i, long token, bool automatic)
        {
            if (!playing || reviewing || (topicMenu != null && topicMenu.gameObject.activeSelf) || !C.IsActive) return;
            if (expires[i] <= 0 || !turns[i].TryWhack(token, true))
            {
                if (!automatic) feedback.text = XgUi.T("这里没有尚未作答的题目。", "There is no unanswered live question here.");
                return;
            }
            var question = questions[i];
            var prediction = predictions[i];
            int generation = buildGeneration;
            bool correct = Target(i), goldenHit = holes[i].Golden && correct;
            // The turn is consumed before notebook, settlement, or feedback callbacks can reenter.
            expires[i] = 0; holes[i].HitTime = 0; holes[i].HitGood = correct;
            if (automatic)
                C.Sim.RecordAiAction("arith", question.Text, question.Text, XgUi.T("敲", "Whack"));
            else C.Sim.RecordTeachingQuestion(question.Text, question.Text, question.Truth, C.Id=="arith_mole",
                question.Why, question.WhyEn, "arith", question.Card(), XgUi.T("敲", "Whack"), aiAnswers[i]);
            bangs[i].text = correct ? XgUi.T("啪！", "Pow!") : XgUi.T("冤！", "Oops!");
            bangs[i].color = correct ? new Color(.86f, .2f, .12f) : new Color(.3f, .38f, .62f);
            if (correct) { score++; combo++; } else { errors++; combo = 0; }
            C.Report(correct);
            // The host may close/switch/rebuild the surface while reporting the accepted answer.
            if (generation != buildGeneration || !playing || !C.IsActive) return;
            feedback.text = XgUi.T(question.Why, question.WhyEn); Hud();
            if(goldenHit){goldenBonus++;bangs[i].text=XgUi.T("金！+2分","Gold! +2");feedback.text=XgUi.T("金地鼠！得分 +2。","Golden mole! +2 points.");}
            if(!correct)goldenMilestone=0;
            ShowMoleScore();
            Paint(i);
            C.Sim.NotifyArithmeticMole(new XgMoleFeedback(C.Id, token, i, automatic, true, correct, true, question, prediction));
        }
        bool PredictedWhack(int i) => predictions[i] != null && (C.Id == "arith_mole" ? predictions[i].AnswerYes : !predictions[i].AnswerYes);
        public override void Dispose()
        {
            playing = false; buildGeneration++;
            reviewing = false; C.Sim.ClearMoleLessons();
            foreach (var turn in turns) turn?.Invalidate();
        }
        /// <summary>Lays the hole's label over its sign and the 「啪」 over its head, and redraws the hole.</summary>
        void Paint(int i)
        {
            var g = holes[i];
            g.Reduced = C.Sim.S.reduceFx;
            float eased = g.Reduced ? (expires[i] > 0 || g.HitTime >= 0 ? 1 : 0) : Ease(rise[i]);
            g.Rise = eased; g.Redraw();
            // The cell is placed with its top left at (cellX, cellY); the label follows the sign as the mole pops up.
            var cell = g.rectTransform; float left = cell.offsetMin.x, top = -cell.offsetMax.y;
            var sc = g.SignCenter;
            bool showSign = g.SignShown && (expires[i] > 0 || g.HitTime >= 0 && g.HitTime < XgMoleGraphic.HitSeconds || rise[i] > 0);
            signs[i].enabled = showSign;
            aiSigns[i].enabled=showSign&&(C.Sim.TeachingAiReady||autoEnabled);
            if(aiSigns[i].enabled){aiSigns[i].text=C.Sim.AiDisplayName+"："+aiAnswers[i];Place(aiSigns[i].rectTransform,left+sc.x-90,top+sc.y-49);}
            if (showSign) Place(signs[i].rectTransform, left + sc.x - 113, top + sc.y - 21);
            bool bang = g.HitTime >= 0 && g.HitTime < XgMoleGraphic.HitSeconds;
            bangs[i].enabled = bang;
            if (bang)
            {
                float t = g.HitTime / XgMoleGraphic.HitSeconds;
                bangs[i].rectTransform.localScale = Vector3.one * (g.Reduced ? 1 : 1 + .4f * Mathf.Sin(Mathf.Min(1, t * 2.2f) * Mathf.PI));
                Place(bangs[i].rectTransform, left + 18, top + 4 + (g.Reduced ? 0 : 8 * t));
            }
        }
        static float Ease(float t)
        {
            // A quick pop with a small overshoot.
            t = Mathf.Clamp01(t); float s = 1.4f; t -= 1;
            return t * t * ((s + 1) * t + s) + 1;
        }
        public override void Tick(float dt)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0) return;
            if (reviewing || (topicMenu != null && topicMenu.gameObject.activeSelf)) return;
            if (!Clock(dt)) { for (int i = 0; i < Cells; i++) Paint(i); return; }
            int generation = buildGeneration;
            for (int i = 0; i < Cells; i++)
            {
                var g = holes[i];
                if (expires[i] > 0)
                {
                    expires[i] -= dt; rise[i] = Mathf.Min(1, rise[i] + dt / PopSeconds);
                    if (expires[i] <= 0)
                    {
                        turns[i].Invalidate();
                        if (hovered == i) { hovered = -1; RefreshQuestionDetail(); }
                        if (Target(i)) { missed++; combo = 0; goldenMilestone=0; }
                    }
                    else if (autoEnabled && C.Sim.MoleAutoAvailable(C.Id) && predictions[i] != null)
                    {
                        autoDelay[i] -= dt;
                        long token = turns[i].Token;
                        if (autoDelay[i] <= 0 && turns[i].TryAutoDecision(token, C.IsActive))
                        {
                            if (PredictedWhack(i)) Hit(i, token, true);
                            else C.Sim.NotifyArithmeticMole(new XgMoleFeedback(C.Id, token, i, true, false,
                                !Target(i), false, questions[i], predictions[i]));
                            if (generation != buildGeneration || !playing || !C.IsActive) return;
                        }
                    }
                }
                else if (g.HitTime >= 0)
                {
                    g.HitTime += dt;
                    if (g.HitTime >= XgMoleGraphic.HitSeconds) g.HitTime = -1;
                }
                else if (rise[i] > 0) rise[i] = Mathf.Max(0, rise[i] - dt / SinkSeconds);
                else { signs[i].enabled = false; aiSigns[i].enabled=false; continue; }
                Paint(i);
            }
            spawn -= dt;
            if (spawn <= 0)
            {
                var free = new List<int>(); for (int i = 0; i < Cells; i++) if (expires[i] <= 0 && rise[i] <= 0 && holes[i].HitTime < 0) free.Add(i);
                if (free.Count > 0)
                {
                    int i = free[C.Random.Next(free.Count)];
                    var question = C.Sim.GenerateMoleQuestion(C.Random.Next());
                    bool golden=C.Id=="arith_mole"&&combo>=10&&goldenMilestone<combo/10;
                    if(golden) { goldenMilestone=combo/10; question=question.AsTrue(); }
                    holes[i].Golden=golden;
                    questions[i] = question; turns[i].Begin(C.Sim.NextMoleQuestionToken());
                    C.Sim.RegisterMoleLesson(turns[i].Token, question);
                    wrong[i] = !question.Truth; truth[i] = question.CorrectText; signs[i].text = question.Text;
                    holes[i].WideSign = true;
                    signs[i].fontSize = question.Text.Length > 20 ? 14 : 18;
                    predictions[i] = C.Sim.TeachingAiReady || C.Sim.MoleAutoAvailable(C.Id) ? C.Sim.PredictMole(question.Card()) : null;
                    holes[i].Ai = predictions[i] != null;
                    aiAnswers[i] = predictions[i] == null ? "" : PredictedWhack(i) ? XgUi.T("敲", "Whack") : XgUi.T("不敲", "Leave");
                    if(predictions[i] != null && predictions[i].Guessed) aiAnswers[i] += XgUi.T("（猜测）", " (guess)");
                    autoDelay[i] = .55f;
                    expires[i] = 1.3f + time / 60 * 1.3f;
                }
                spawn = .42f + time / 60 * .55f;
            }
            ShowMoleScore();
        }
        void ShowMoleScore(){Hud();status.text+=XgUi.T("   得分 "+(score+goldenBonus)+"   漏敲 "+missed,"   points "+(score+goldenBonus)+"   missed "+missed);}
    }

    sealed class XgActionShooter : XgActionBase
    {
        sealed class Comment { public float x, y, speed; public bool harmful, flagged; public TMP_Text label; }
        sealed class Bullet { public Vector2 position; public bool ai; }
        readonly List<Comment> comments = new List<Comment>();
        readonly List<Bullet> bullets = new List<Bullet>();
        static readonly string[] Bad = { "垃圾主播，滚出去", "你们全是骗子", "玩这游戏的都是蠢货", "就你也配当主播？", "真厉害，菜得有个性" };
        static readonly string[] Good = { "主播加油", "这波操作我给满分", "前方高能", "学到了，记笔记", "主播别熬夜，注意身体" };
        XgActionGraphic art;
        Vector2 ship;
        float spawn, fire, wingFire;
        int hp, aiHits, aiMistakes, wave;
        bool hold;
        public XgActionShooter(XgMiniContext c) : base(c) { }
        public override void Build()
        {
            Header(XgUi.T("弹幕射击 · 保护直播间", "Comment shooter · protect the stream"), XgUi.T("WASD / 方向键或拖动移动，按住空格 / 鼠标开火。只击落辱骂引战，保护正常弹幕；紫色为" + C.Sim.AiDisplayName + "建议。", "Move with WASD / arrows or drag; hold Space / mouse to fire. Shoot abuse and protect normal comments. Purple marks " + C.Sim.AiDisplayName + " suggestions."));
            comments.Clear(); bullets.Clear(); ship = new Vector2(100, 150); spawn = .2f; fire = wingFire = 0; hp = 5; aiHits = aiMistakes = 0; wave = 1; hold = false;
            C.Button("下一波弹幕", 745, 78, 135, 28, "下一波", NextWave, 15);
            var pad = C.Box("直播区域", 15, 112, 870, 320, new Color(.065f, .085f, .16f));
            C.Pointer(pad.rectTransform, p => { hold = true; SetShip(p); }, SetShip, p => hold = false);
            art = Art(15, 112, 870, 320); Paint();
        }
        void NextWave()
        {
            if (playing || time > 0 || hp <= 0) return;
            foreach (var c in comments) { c.label.gameObject.SetActive(false); XgMiniContext.DestroyObject(c.label.gameObject); }
            comments.Clear(); bullets.Clear(); wave++; hp = Mathf.Min(5, hp + 1); time = 60; playing = true; combo = 0; hold = false; spawn = .4f;
            feedback.text = "第 " + wave + " 波开始，主播恢复一点心态；弹幕速度加快。"; Paint();
        }
        void SetShip(Vector2 p) { if (playing) ship = new Vector2(Mathf.Clamp(p.x, 35, 350), Mathf.Clamp(p.y, 20, 300)); }
        void Hit(Comment c, bool ai)
        {
            if (ai)
            {
                if (c.harmful) aiHits++; else { aiMistakes++; hp--; }
                feedback.text = c.harmful ? XgUi.T(C.Sim.AiDisplayName + "僚机击中了恶意弹幕（不自动产生玩家标签）。", C.Sim.AiDisplayName + " hit an abusive comment (no player label is added automatically).") : XgUi.T(C.Sim.AiDisplayName + "误伤了好评，主播心态下降。", C.Sim.AiDisplayName + " hit a positive comment, hurting the streamer’s morale.");
            }
            else { Result(c.harmful, c.harmful ? "击落恶意弹幕。" : "误伤正常弹幕，主播心态下降。"); if (!c.harmful) hp--; }
            c.label.gameObject.SetActive(false); XgMiniContext.DestroyObject(c.label.gameObject); comments.Remove(c);
            if (hp <= 0) End("主播心态耗尽");
        }
        public override void Tick(float dt)
        {
            if (!Clock(dt)) return;
            dt = Mathf.Min(dt, .08f);
            Vector2 move = new Vector2((C.KeyHeld(Key.D) || C.KeyHeld(Key.RightArrow) ? 1 : 0) - (C.KeyHeld(Key.A) || C.KeyHeld(Key.LeftArrow) ? 1 : 0), (C.KeyHeld(Key.S) || C.KeyHeld(Key.DownArrow) ? 1 : 0) - (C.KeyHeld(Key.W) || C.KeyHeld(Key.UpArrow) ? 1 : 0));
            SetShip(ship + move.normalized * 230 * dt);
            if (hold && (Mouse.current == null || !Mouse.current.leftButton.isPressed)) hold = false;
            bool shooting = hold || C.KeyHeld(Key.Space); fire -= dt; wingFire -= dt;
            if (shooting && fire <= 0) { bullets.Add(new Bullet { position = ship + new Vector2(18, 0) }); fire = .23f; }
            // Companion only assists while the player actively fires; no unattended sample farming.
            if (shooting && wingFire <= 0)
            {
                Comment target = comments.Find(x => x.flagged && x.x > ship.x);
                if (target != null) bullets.Add(new Bullet { position = new Vector2(ship.x - 18, target.y + 15), ai = true });
                wingFire = .7f + (float)(1 - C.Skill) * 1.5f;
            }
            spawn -= dt;
            if (spawn <= 0)
            {
                bool harmful = C.Random.Next(2) == 0; var bank = harmful ? Bad : Good;
                var c = new Comment { x = 660, y = 8 + C.Random.Next(8) * 37, speed = (62 + (60 - time) * .65f) * (1 + .09f * (wave - 1)), harmful = harmful, flagged = Opinion(harmful) };
                c.label = C.Text("飞行弹幕", 15 + c.x, 112 + c.y, 207, 31, bank[C.Random.Next(bank.Length)], 17, c.flagged ? new Color(.76f, .66f, 1) : Color.white); c.label.raycastTarget = false;
                comments.Add(c); spawn = .75f;
            }
            for (int i = comments.Count - 1; i >= 0; i--)
            {
                var c = comments[i]; c.x -= c.speed * dt;
                Place(c.label.rectTransform, 15 + c.x, 112 + c.y);
                if (c.x < 10)
                {
                    if (c.harmful) { hp--; combo = 0; feedback.text = "恶意弹幕穿过防线，主播心态 −1。"; }
                    c.label.gameObject.SetActive(false); XgMiniContext.DestroyObject(c.label.gameObject); comments.RemoveAt(i);
                    if (hp <= 0) { End("主播心态耗尽"); Paint(); return; }
                }
            }
            for (int b = bullets.Count - 1; b >= 0; b--)
            {
                var bullet = bullets[b]; Vector2 old = bullet.position; bullet.position.x += 540 * dt; Comment hit = null;
                foreach (var c in comments) if (XgMiniActionRules.SegmentHits(old.x, old.y, bullet.position.x, bullet.position.y, c.x, c.y, 207, 31)) { hit = c; break; }
                if (hit != null) { Hit(hit, bullet.ai); bullets.RemoveAt(b); if (!playing) break; }
                else if (bullet.position.x > 870) bullets.RemoveAt(b);
            }
            Paint(); status.text = $"第 {wave} 波 · {Mathf.CeilToInt(time)} 秒 · 心态 {hp}/5 · 击落 {score} · 误伤 {errors} · 僚机 {aiHits}/{aiMistakes}";
        }
        void Paint()
        {
            art.Clear(); art.Box(8, 0, 3, 320, Red);
            for (int i = 0; i < 8; i++) art.Box(12, 40 * i, 854, 1, new Color(.14f, .18f, .25f));
            art.Line(ship + new Vector2(-13, -12), ship + new Vector2(18, 0), 4, Color.cyan);
            art.Line(ship + new Vector2(-13, 12), ship + new Vector2(18, 0), 4, Color.cyan);
            art.Box(ship.x - 12, ship.y - 5, 19, 10, Color.cyan);
            art.Box(ship.x - 23, Mathf.Clamp(ship.y - 34, 2, 304), 13, 13, Purple);
            foreach (var b in bullets) art.Box(b.position.x - 7, b.position.y - 2, 14, 4, b.ai ? Purple : Color.yellow);
            art.Refresh();
        }
    }

    sealed class XgActionSnake : XgActionBase
    {
        sealed class Word { public Rect rect; public TMP_Text text; public string value; }
        readonly List<Word> words = new List<Word>();
        readonly List<Vector2> trail = new List<Vector2>();
        readonly List<Rect> ads = new List<Rect>();
        static readonly string[] Lexicon = { "人工智能", "算法", "神经网络", "训练", "数据", "模型", "计算机", "学习", "语料", "向量", "样本", "推理", "记忆", "文本", "视觉", "语言", "参数", "标签", "验证集", "损失函数", "梯度", "优化" };
        XgActionGraphic art;
        Vector2 head, destination;
        float angle, invincible;
        int length, page, collisions;
        bool moving, mouseMode;
        public XgActionSnake(XgMiniContext c) : base(c) { }
        public override void Build()
        {
            Header("爬虫贪吃蛇 · 抓取语料", "方向键 / WASD 转向，或按住鼠标引导。碰到单词才采集；避开广告与尾巴。碰撞断尾并扣时 3 秒。", 90);
            words.Clear(); trail.Clear(); ads.Clear(); head = new Vector2(38, 140); destination = head; angle = 0; length = 0; page = 1; collisions = 0; invincible = 0; moving = false; mouseMode = false;
            var bg = C.Box("网页语料区", 15, 112, 870, 320, new Color(.94f, .94f, .90f));
            C.Pointer(bg.rectTransform, p => { destination = p; moving = mouseMode = true; }, p => destination = p);
            art = Art(15, 112, 870, 320);
            ads.Add(new Rect(235, 120, 140, 67)); ads.Add(new Rect(590, 120, 155, 67));
            foreach (var r in ads) { var t = C.Text("广告", 15 + r.x, 112 + r.y, r.width, r.height, "广告区域\n请绕行", 19, Color.white); t.raycastTarget = false; }
            Populate(); Paint();
        }
        void Populate()
        {
            for (int row = 0; row < 4; row++) for (int col = 0; col < 7; col++)
            {
                if ((row == 1 || row == 2) && (col == 2 || col == 5)) continue;
                float x = 70 + col * 112, y = 18 + row * 76;
                var rect = new Rect(x, y, 88, 25); bool blocked = false;
                foreach (var ad in ads) if (ad.Overlaps(rect)) blocked = true;
                if (blocked || Vector2.Distance(rect.center, head) < 55) continue;
                string value = Lexicon[(row * 7 + col + page * 3) % Lexicon.Length];
                var t = C.Text("语料词", 15 + x, 112 + y, 88, 27, value, 16, (row + col) % 3 == 0 ? new Color(.58f, .35f, .04f) : Blue); t.raycastTarget = false;
                words.Add(new Word { rect = rect, text = t, value = value });
            }
        }
        void Crash(string reason)
        {
            if (invincible > 0) return; invincible = 1.3f; collisions++; length /= 2; time = Mathf.Max(0, time - 3); combo = 0; angle += Mathf.PI;
            feedback.text = reason + "：断掉一半尾巴，扣时 3 秒。";
        }
        public override void Tick(float dt)
        {
            if (!Clock(dt)) return; dt = Mathf.Min(dt, .06f); invincible = Mathf.Max(0, invincible - dt);
            var direction = new Vector2((C.KeyHeld(Key.RightArrow) || C.KeyHeld(Key.D) ? 1 : 0) - (C.KeyHeld(Key.LeftArrow) || C.KeyHeld(Key.A) ? 1 : 0), (C.KeyHeld(Key.DownArrow) || C.KeyHeld(Key.S) ? 1 : 0) - (C.KeyHeld(Key.UpArrow) || C.KeyHeld(Key.W) ? 1 : 0));
            if (direction.sqrMagnitude > 0) { moving = true; mouseMode = false; }
            if (!moving) return;
            if (mouseMode) direction = destination - head;
            if (direction.sqrMagnitude > 10 || (!mouseMode && direction.sqrMagnitude > 0))
            {
                float target = Mathf.Atan2(direction.y, direction.x);
                angle = Mathf.MoveTowardsAngle(angle * Mathf.Rad2Deg, target * Mathf.Rad2Deg, 290 * dt) * Mathf.Deg2Rad;
            }
            head += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (120 + (float)C.Skill * 30) * dt;
            if (head.x < 10 || head.x > 860) { angle = Mathf.PI - angle; head.x = Mathf.Clamp(head.x, 10, 860); }
            if (head.y < 10 || head.y > 310) { angle = -angle; head.y = Mathf.Clamp(head.y, 10, 310); }
            if (trail.Count == 0 || Vector2.Distance(trail[0], head) > 5) trail.Insert(0, head);
            int maxTrail = 10 + length * 5; if (trail.Count > maxTrail) trail.RemoveRange(maxTrail, trail.Count - maxTrail);
            foreach (var r in ads) if (XgMiniActionRules.CircleHitsRect(head.x, head.y, 8, r.x, r.y, r.width, r.height)) { Crash("撞到广告"); break; }
            for (int i = 14; i < trail.Count && invincible <= 0; i++) if (Vector2.Distance(head, trail[i]) < 9) { Crash("撞到自己的尾巴"); break; }
            for (int i = words.Count - 1; i >= 0; i--)
            {
                var w = words[i]; var r = w.rect;
                if (!XgMiniActionRules.CircleHitsRect(head.x, head.y, 7, r.x, r.y, r.width, r.height)) continue;
                Result(true, "已抓取词语：「" + w.value + "」。碰到实体才入库。"); length = Mathf.Min(30, length + 1);
                w.text.gameObject.SetActive(false); XgMiniContext.DestroyObject(w.text.gameObject); words.RemoveAt(i);
            }
            if (words.Count == 0) { page++; Populate(); feedback.text = "这一页读完了，进入第 " + page + " 页。"; }
            Paint(); status.text = $"{Mathf.CeilToInt(time)} 秒 · 采集 {score} · 尾长 {length} · 碰撞 {collisions} · 第 {page} 页";
        }
        void Paint()
        {
            art.Clear(); foreach (var r in ads) art.Box(r.x, r.y, r.width, r.height, Red);
            for (int i = trail.Count - 1; i > 0; i--) art.Line(trail[i], trail[i - 1], 5, Purple);
            for (int i = 5; i < trail.Count; i += 5) art.Box(trail[i].x - 5, trail[i].y - 5, 10, 10, new Color(.8f, .61f, .17f));
            Color color = invincible > 0 ? Red : Blue;
            for (int i = -1; i <= 1; i++)
            {
                art.Line(head + new Vector2(-5, i * 4), head + new Vector2(-15, i * 12), 2, color);
                art.Line(head + new Vector2(5, i * 4), head + new Vector2(15, i * 12), 2, color);
            }
            art.Box(head.x - 7, head.y - 8, 14, 16, color); art.Refresh();
        }
    }

    sealed class XgActionDifferences : XgActionBase
    {
        readonly Rect[] positions = { new Rect(22, 29, 68, 62), new Rect(113, 26, 76, 64), new Rect(219, 26, 57, 65), new Rect(306, 31, 67, 60), new Rect(20, 172, 61, 69), new Rect(107, 165, 81, 78), new Rect(222, 172, 64, 70), new Rect(310, 170, 66, 74) };
        readonly string[] names = { "墙上的画", "时钟", "书本", "窗户", "台灯", "显示器", "水杯", "盆栽" };
        readonly string[] namesEn = { "painting", "clock", "books", "window", "lamp", "monitor", "cup", "plant" };
        string Name(int i) => XgUi.T(names[i], namesEn[i]);
        readonly int[] changes = new int[8];
        readonly bool[] found = new bool[8];
        readonly HashSet<int> suggested = new HashSet<int>();
        XgActionGraphic left, right;
        TMP_Text hint;
        int count, suggestion;
        public XgActionDifferences(XgMiniContext c) : base(c) { }
        public override void Build()
        {
            Header(XgUi.T("找不同 · 双图核验", "Spot the differences · compare both images"), XgUi.T("两幅书桌图共有五处不同：颜色、缺失、位移或大小。点任意一侧差异；紫框是" + C.Sim.AiDisplayName + "建议，也可能完全相同。", "Find five changes in color, presence, position or size. Click either image. Purple boxes are " + C.Sim.AiDisplayName + " suggestions and may be wrong."), 90);
            Array.Clear(changes, 0, 8); Array.Clear(found, 0, 8); suggested.Clear(); count = 0; suggestion = -1;
            var pool = new List<int>(); for (int i = 0; i < 8; i++) pool.Add(i);
            for (int i = 0; i < 5; i++) { int at = C.Random.Next(pool.Count); changes[pool[at]] = 1 + i % 4; pool.RemoveAt(at); }
            var a = C.Box("左侧图片", 25, 122, 400, 280, Color.white);
            var b = C.Box("右侧图片", 475, 122, 400, 280, Color.white);
            left = Art(25, 122, 400, 280); right = Art(475, 122, 400, 280);
            C.Pointer(a.rectTransform, p => Click(p, false)); C.Pointer(b.rectTransform, p => Click(p, true));
            hint = C.Text("差异建议", 25, 408, 420, 30, "", 15, Purple); hint.raycastTarget = false;
            C.Button("请求圈选", 464, 408, 120, 30, XgUi.T(C.Sim.AiDisplayName + "圈选", C.Sim.AiDisplayName + " pick"), Suggest, 15);
            C.Button("证实不同", 594, 408, 140, 30, "确有不同", () => Verify(true), 15);
            C.Button("否定圈选", 744, 408, 130, 30, "其实相同", () => Verify(false), 15);
            Paint(); Suggest();
        }
        Rect RightRect(int i)
        {
            Rect r = positions[i]; if (changes[i] == 3) r.x += 13;
            if (changes[i] == 4) { r.width *= .63f; r.height *= .65f; }
            return r;
        }
        void Click(Vector2 p, bool rightSide)
        {
            if (!playing) return;
            for (int i = 0; i < 8; i++)
            {
                Rect r = rightSide ? RightRect(i) : positions[i]; Rect old = positions[i];
                if (!r.Contains(p) && !old.Contains(p)) continue;
                if (found[i]) { feedback.text = "这里已经核验过，不重复计分。"; return; }
                if (changes[i] > 0) Mark(i); else Wrong("这里两边相同，扣时 3 秒。");
                return;
            }
            Wrong("未点中差异，扣时 3 秒。");
        }
        void Mark(int i)
        {
            if (found[i]) return; found[i] = true; count++;
            Result(true, names[i] + "：" + (changes[i] == 1 ? "颜色不同" : changes[i] == 2 ? "右图缺失" : changes[i] == 3 ? "位置移动" : "大小改变"));
            if (suggestion == i) suggestion = -1;
            Paint(); if (count == 5) End("已找齐五处差异");
        }
        void Wrong(string message) { Result(false, message); time = Mathf.Max(0, time - 3); if (time <= 0) End("时间耗尽"); }
        void Suggest()
        {
            if (!playing || suggestion >= 0) return;
            var real = new List<int>(); var decoy = new List<int>();
            for (int i = 0; i < 8; i++) if (!found[i] && !suggested.Contains(i)) { if (changes[i] > 0) real.Add(i); else decoy.Add(i); }
            var candidates = C.Random.NextDouble() < C.Skill ? real : decoy;
            if (candidates.Count == 0) candidates = real.Count > 0 ? real : decoy;
            if (candidates.Count == 0) { hint.text = "没有新的圈选，请自行寻找。"; return; }
            suggestion = candidates[C.Random.Next(candidates.Count)]; suggested.Add(suggestion);
            hint.text = XgUi.T(C.Sim.AiDisplayName + "觉得「" + Name(suggestion) + "」不同。请核验。", C.Sim.AiDisplayName + " thinks the " + Name(suggestion) + " differs. Check it."); Paint();
        }
        void Verify(bool different)
        {
            if (!playing || suggestion < 0) return; int i = suggestion; suggestion = -1;
            bool truth = changes[i] > 0;
            if (truth && different) Mark(i);
            else if (!truth && !different) Result(true, XgUi.T("纠正了" + C.Sim.AiDisplayName + "的误报：两侧「" + Name(i) + "」相同。", "Corrected " + C.Sim.AiDisplayName + ": the " + Name(i) + " matches on both sides."));
            else Wrong(truth ? XgUi.T("这处确有不同，请仔细比较。扣时 3 秒。", "This does differ. Compare carefully. Minus 3 seconds.") : XgUi.T("这处其实相同，" + C.Sim.AiDisplayName + "误报了。扣时 3 秒。", "These match; " + C.Sim.AiDisplayName + " was wrong. Minus 3 seconds."));
            hint.text = "可请求下一个圈选，或直接点击差异。"; Paint();
        }
        void Paint()
        {
            Scene(left, false); Scene(right, true); status.text = $"剩余 {Mathf.CeilToInt(time)} 秒   已找到 {count}/5   误判 {errors}";
        }
        void Scene(XgActionGraphic g, bool variant)
        {
            g.Clear(); g.Box(0, 0, 400, 280, new Color(.90f, .89f, .84f));
            g.Box(0, 101, 400, 9, new Color(.64f, .47f, .32f)); g.Box(0, 243, 400, 13, new Color(.54f, .37f, .25f));
            g.Box(34, 256, 12, 24, Blue); g.Box(354, 256, 12, 24, Blue);
            for (int i = 0; i < 8; i++)
            {
                Rect r = variant ? RightRect(i) : positions[i];
                if (!(variant && changes[i] == 2)) DrawObject(g, i, r, variant && changes[i] == 1 ? Red : Blue);
                if (found[i] || suggestion == i)
                {
                    // Mark the union, so moved and missing objects remain unambiguous.
                    Rect p = positions[i]; p.width += 14; Color color = found[i] ? Green : Purple;
                    g.Box(p.x - 3, p.y - 3, p.width + 6, 3, color); g.Box(p.x - 3, p.y + p.height, p.width + 6, 3, color);
                    g.Box(p.x - 3, p.y - 3, 3, p.height + 6, color); g.Box(p.x + p.width, p.y - 3, 3, p.height + 6, color);
                }
            }
            g.Refresh();
        }
        static void DrawObject(XgActionGraphic g, int i, Rect r, Color c)
        {
            float x = r.x, y = r.y, w = r.width, h = r.height;
            switch (i)
            {
                case 0: g.Box(x, y, w, h, c); g.Box(x + 5, y + 5, w - 10, h - 10, Color.white); g.Line(new Vector2(x + 7, y + h - 9), new Vector2(x + w * .5f, y + 14), 7, Green); g.Box(x + w - 23, y + 12, 13, 13, Color.yellow); break;
                case 1: g.Box(x, y, w, h, c); g.Box(x + 5, y + 5, w - 10, h - 10, Color.white); g.Line(r.center, r.center + new Vector2(0, -h * .3f), 4, c); g.Line(r.center, r.center + new Vector2(w * .25f, 9), 3, c); break;
                case 2: for (int b = 0; b < 4; b++) { g.Box(x + b * w / 4, y + b * 3, w / 4 - 2, h - b * 3, c); g.Box(x + b * w / 4 + 2, y + 10 + b * 3, w / 4 - 6, 3, Color.white); } break;
                case 3: g.Box(x, y, w, h, c); g.Box(x + 4, y + 4, w - 8, h - 8, new Color(.65f, .86f, .95f)); g.Box(x + w / 2, y, 4, h, c); g.Box(x, y + h / 2, w, 4, c); break;
                case 4: g.Box(x + w * .4f, y + h * .3f, w * .13f, h * .6f, c); g.Box(x + w * .1f, y + h * .86f, w * .8f, h * .12f, c); g.Box(x, y, w, h * .36f, c); break;
                case 5: g.Box(x, y, w, h * .7f, c); g.Box(x + 5, y + 5, w - 10, h * .7f - 10, new Color(.13f, .21f, .29f)); g.Box(x + w * .43f, y + h * .7f, w * .14f, h * .2f, c); g.Box(x + w * .2f, y + h * .9f, w * .6f, h * .08f, c); break;
                case 6: g.Box(x + 6, y + h * .25f, w * .7f, h * .72f, c); g.Box(x + w * .7f, y + h * .36f, w * .25f, h * .35f, c); g.Box(x + w * .7f, y + h * .43f, w * .14f, h * .18f, Color.white); break;
                default: g.Box(x + w * .3f, y + h * .58f, w * .5f, h * .4f, c); g.Box(x + w * .48f, y + 5, 5, h * .6f, Green); g.Line(new Vector2(x + w * .5f, y + h * .42f), new Vector2(x + 7, y + 10), 10, Green); g.Line(new Vector2(x + w * .5f, y + h * .33f), new Vector2(x + w - 6, y + 7), 10, Green); break;
            }
        }
        public override void Tick(float dt) { if (Clock(dt)) status.text = $"剩余 {Mathf.CeilToInt(time)} 秒   已找到 {count}/5   误判 {errors}"; }
    }
}
