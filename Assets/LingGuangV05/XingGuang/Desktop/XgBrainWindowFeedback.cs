using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Global and per-region onset limits. Static acknowledgement is never rate-limited.</summary>
    public sealed class XgBrainReactionGate
    {
        readonly Queue<float> starts = new Queue<float>();
        readonly Dictionary<string, float> regionStarts = new Dictionary<string, float>();
        public bool TryStart(float now, string region)
        {
            while (starts.Count > 0 && now - starts.Peek() >= 1f) starts.Dequeue();
            region = region ?? "all";
            if (starts.Count >= 3 || regionStarts.TryGetValue(region, out float previous) && now - previous < 1f / 3f) return false;
            starts.Enqueue(now); regionStarts[region] = now; return true;
        }
        public void Clear() { starts.Clear(); regionStarts.Clear(); }
    }

    /// <summary>Bounded reactions to real events. Reduced motion redraws only static event state.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class XgBrainWindowFeedback : MaskableGraphic
    {
        public XgWallpaperConcepts concepts;
        public Func<bool> ReducedMotion;
        public bool Reduced => (ReducedMotion?.Invoke() ?? false) || GamePerformance.Low;
        public XgBrainReactionGate OnsetGate { get; set; } = new XgBrainReactionGate();
        readonly HashSet<string> dataRegions = new HashSet<string>();
        readonly HashSet<int> installedRegions = new HashSet<int>();
        float began = -10, nextDraw;
        Vector2 pulseUv = new Vector2(.5f, .5f), movingUv;
        Color ink, movingInk;
        bool animating, lastReduced;
        string lastReaction = "", animation = "";
        string signature = "";
        Color signatureInk;
        Vector2 signatureUv;
        bool signatureMoving;
        public int FeedbackCount { get; private set; }
        public int MeshBuilds { get; private set; }
        public int CorrectCount { get; private set; }
        public int WrongCount { get; private set; }
        public int ComboCount { get; private set; }
        public int EpochCount { get; private set; }
        public int DatasetCount { get; private set; }
        public int ConceptCount { get; private set; }
        public int AbilityCount { get; private set; }
        public int KnowledgeCount { get; private set; }
        public int AnimationStarts { get; private set; }
        public int MergedCount { get; private set; }
        public int StageCount { get; private set; }
        public bool Animating => animating && !Reduced;
        public string LastReaction => lastReaction;
        public string LastSignature => signature;
        static readonly Color Good = new Color32(95, 227, 181, 255), Bad = new Color32(255, 122, 110, 255);
        static readonly Color Edge = new Color32(110, 180, 255, 255), GoldInk = new Color32(255, 206, 90, 255);
        static int RegionIndex(string region) => region == "vision" ? 0 : region == "sequence" ? 1 : region == "tone" ? 2 : 3;
        Vector2 Location(string region)
        {
            int index = RegionIndex(region);
            // An event without a verified concept id lands at the region centre, never an arbitrary learned cell.
            return XgWallpaperConcepts.RegionCenter(index);
        }
        public void ResetFeedback()
        {
            dataRegions.Clear(); installedRegions.Clear(); OnsetGate.Clear();
            lastReaction = animation = signature = ""; began = -10; animating = signatureMoving = false;
            FeedbackCount = CorrectCount = WrongCount = ComboCount = EpochCount = DatasetCount = ConceptCount = AbilityCount = KnowledgeCount = AnimationStarts = MergedCount = StageCount = 0;
            SetVerticesDirty();
        }
        void React(string kind, string region, Color color, bool allowAnimation = true)
        {
            lastReaction = kind; pulseUv = region == null ? new Vector2(.5f, .5f) : Location(region); ink = color;
            signature = ""; signatureMoving = false;
            FeedbackCount++;
            if (Reduced) { animating = false; }
            else if (allowAnimation)
            {
                if (OnsetGate.TryStart(Time.unscaledTime, region))
                {
                    began = Time.unscaledTime; movingUv = pulseUv; movingInk = color; animation = kind;
                    animating = true; AnimationStarts++;
                }
                else MergedCount++;
            }
            SetVerticesDirty();
        }
        public void Region(string region, bool right) { if (right) CorrectCount++; else WrongCount++; React(right ? "correct" : "wrong", region, right ? Good : Bad); }
        public void Answer(string dataset, bool right)
        {
            if (right) CorrectCount++; else WrongCount++;
            DataReaction(right ? "correct" : "wrong", dataset, right ? Good : Bad, right);
        }
        void DataReaction(string kind, string dataset, Color color, bool showSignature = true)
        {
            int starts = AnimationStarts;
            string region = LingGuangV05.XingGuang.XgBoardData.DisplayRegion(dataset);
            React(kind, region, color);
            signature = showSignature ? SignatureKind(dataset) : "";
            signatureUv = pulseUv; signatureInk = RegionColor(region);
            signatureMoving = !Reduced && AnimationStarts > starts;
        }
        public static string SignatureKind(string dataset)
        {
            switch (dataset)
            {
                case "sense": case "spam": case "poems": case "translate": case "chess": case "mnist": case "go": return dataset;
                case "logic": case "arith": case "xor": return "logic";
                case "cifar": case "imagenet": case "meme": return "mnist";
                default: return "chain";
            }
        }
        static Color RegionColor(string region) => region == "logic" ? new Color32(165, 148, 255, 255)
            : region == "vision" ? new Color32(106, 184, 255, 255) : region == "tone" ? new Color32(255, 154, 60, 255) : new Color32(40, 230, 210, 255);
        // ConceptsChanged accompanies the settled answer/training event; do not start a second flash.
        public void Concept(string region) { ConceptCount++; React("concept", region, Good, false); }
        public void Knowledge(string region, bool ignored) { KnowledgeCount++; React("knowledge", region, Edge); }
        public void Gold() { ComboCount++; React("combo", null, GoldInk); }
        public void Sweep() { EpochCount++; React("epoch", null, Good); }
        public void Sweep(string region) { EpochCount++; React("epoch", region, Good); }
        public void TrainDataset(string dataset) { EpochCount++; DataReaction("epoch", dataset, Good); }
        public void Training(string region) { React("training", region, Edge, false); }
        public void Assessment(string region, bool record) { React(record ? "record" : "assessed", region, record ? Good : Edge); }
        public void Diverged(string region) { animating = false; React("nan", region, Bad, false); }
        public void Rebirth() { AbilityCount++; React("ability", null, Edge); }
        public void Stage(int stage) { StageCount++; React("stage", null, Edge); }
        public void NewRegion(string dataset)
        {
            if (string.IsNullOrEmpty(dataset)) return;
            string region = LingGuangV05.XingGuang.XgBoardData.DisplayRegion(dataset);
            if (dataRegions.Add(dataset)) installedRegions.Add(RegionIndex(region));
            DatasetCount++; DataReaction("dataset", dataset, Edge);
        }
        protected override void OnDisable() { base.OnDisable(); animating = signatureMoving = false; began = -10; }
        void Update()
        {
            bool reduced = Reduced;
            if (lastReduced != reduced) { lastReduced = reduced; animating = signatureMoving = false; SetVerticesDirty(); }
            if (reduced || !animating) return;
            float now = Time.unscaledTime;
            if (now < nextDraw) return;
            nextDraw = now + 1f / 30;
            if (now - began >= 1.2f) animating = signatureMoving = false;
            SetVerticesDirty();
        }
        static Color Alpha(Color color, float alpha) { color.a = alpha; return color; }
        float Pixel(float value) => value / Mathf.Max(.05f, Mathf.Abs(rectTransform.lossyScale.x));
        void Signature(VertexHelper vh, Rect r, string kind, Vector2 uv, Color tint, float progress, float opacity)
        {
            if (string.IsNullOrEmpty(kind)) return;
            var center = r.min + Vector2.Scale(r.size, uv);
            float unit = Mathf.Min(r.width / 1280, r.height / 832) * 1.5f;
            Vector2 At(float x, float y) => center + new Vector2(x, -y) * unit;
            bool Ready(int index, int total) => progress >= index / (float)total * .5f;
            void Dot(int i, int total, float x, float y, Color color, bool ring = false, bool square = false)
            {
                if (!Ready(i, total)) return;
                var p = At(x, y); color = Alpha(color, color.a * opacity);
                float radius = Pixel(1.3f);
                if (ring) XgDraw.Ring(vh, p, Pixel(2.4f), Pixel(.8f), color);
                else if (square) XgDraw.Box(vh, p - Vector2.one * radius, p + Vector2.one * radius, color);
                else XgDraw.Disc(vh, p, radius, color);
            }
            void Line(int i, int total, float x1, float y1, float x2, float y2, Color color)
            { if (Ready(i, total)) XgDraw.Seg(vh, At(x1, y1), At(x2, y2), Pixel(.7f), Alpha(color, color.a * opacity * .65f)); }
            var amber = new Color32(242, 196, 109, 255);
            switch (kind)
            {
                case "spam":
                    for (int side = -1; side <= 1; side += 2)
                    { Line(1, 3, side * 12, -20, side * 18, 0, Bad); Line(2, 3, side * 18, 0, side * 12, 20, Bad); }
                    Dot(2, 3, 0, 0, Bad, false, true); break;
                case "poems":
                    for (int i = 0; i < 10; i++) { int col = i % 5; float y = i < 5 ? -12 : 12;
                        Dot(i, 10, -40 + col * 20, y, i == 9 ? (Color)amber : tint);
                        if (col > 0) Line(i, 10, -60 + col * 20, y, -40 + col * 20, y, tint); }
                    break;
                case "translate":
                    for (int i = 0; i < 4; i++) { int other = i ^ 1;
                        Dot(i, 8, -36 + i * 24, -22, tint); Dot(i + 4, 8, -36 + i * 24, 22, Edge);
                        Line(i + 4, 8, -36 + i * 24, -22, -36 + other * 24, 22, Edge); }
                    break;
                case "mnist":
                    // A bounded twelve-pixel zero, matching the MNIST signature rather than inventing a recognised image.
                    for (int i = 0; i < 12; i++) { float angle = i * Mathf.PI * 2 / 12;
                        Dot(i, 12, Mathf.Round(Mathf.Cos(angle) * 2) * 12, Mathf.Round(Mathf.Sin(angle) * 2) * 12, tint, false, true); }
                    break;
                case "go":
                    for (int i = -2; i <= 2; i++) { Line(0, 1, -24, i * 12, 24, i * 12, tint); Line(0, 1, i * 12, -24, i * 12, 24, tint); }
                    Dot(0, 5, 0, 0, new Color32(226, 233, 255, 255));
                    for (int i = 0; i < 4; i++) { float a = i * Mathf.PI / 2; Dot(i + 1, 5, Mathf.Cos(a) * 12, Mathf.Sin(a) * 12, Good, true); }
                    break;
                case "chess":
                    Line(1, 4, -14, 26, -14, 4, amber); Line(2, 4, -14, 4, -14, -18, amber); Line(3, 4, -14, -18, 12, -18, amber);
                    Dot(0, 4, -14, 26, amber, true); Dot(3, 4, 12, -18, amber, true); break;
                case "logic":
                    Line(1, 4, 0, 34, 0, 0, tint); Line(2, 4, 0, 0, -26, -26, Good); Line(3, 4, 0, 0, 26, -26, Alpha(Bad, .4f));
                    Dot(2, 4, -26, -26, Good); Dot(3, 4, 26, -26, Bad, true); break;
                default:
                    for (int i = 0; i < 6; i++) { float x = -50 + i * 20;
                        Dot(i, 6, x, 10, i == 5 ? Good : tint); if (i > 0) Line(i, 6, x - 20, 10, x, 10, tint); }
                    break;
            }
        }
        static Vector2 Curve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        { float u = 1 - t; return u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d; }
        void Comets(VertexHelper vh, Rect r, Vector2 destination, Color tint, float age, int count)
        {
            Vector2 source = r.min + Vector2.Scale(r.size, new Vector2(.5f, .12f));
            Vector2 hub = r.min + Vector2.Scale(r.size, new Vector2(.52f, .29f));
            // At most four six-segment tails and two low-intensity branches; no particles or timers accumulate.
            for (int comet = 0; comet < count; comet++)
            {
                float travel = (age - comet * .045f) / .18f;
                if (travel < 0 || travel > 1.28f) continue;
                for (int tail = 0; tail < 6; tail++)
                {
                    float t = travel - tail * .045f; if (t < 0 || t > 1) continue;
                    Vector2 p = Curve(source, source + Vector2.up * r.height * .15f, hub, destination, t);
                    Vector2 previous = Curve(source, source + Vector2.up * r.height * .15f, hub, destination, Mathf.Max(0, t - .045f));
                    XgDraw.Seg(vh, previous, p, Pixel(tail == 0 ? 1.4f : .8f), Alpha(tint, .5f * (1 - tail / 6f)));
                    if (tail == 0) XgDraw.Disc(vh, p, Pixel(2), Alpha(tint, .65f));
                }
            }
            float branch = (age - .18f) / .1f;
            if (branch < 0 || branch > 1) return;
            for (int side = -1; side <= 1; side += 2)
            {
                var endpoint = destination + new Vector2(side * Pixel(20), Pixel(12));
                XgDraw.Seg(vh, destination, Vector2.Lerp(destination, endpoint, branch), Pixel(.8f), Alpha(tint, .22f * (1 - branch)));
            }
        }
        void DrawStatic(VertexHelper vh, Rect r)
        {
            foreach (int region in installedRegions)
                XgDraw.Ring(vh, r.min + Vector2.Scale(r.size, XgWallpaperConcepts.RegionCenter(region)), Pixel(7), Pixel(.8f), Alpha(Edge, .22f));
            if (string.IsNullOrEmpty(lastReaction)) return;
            XgDraw.Disc(vh, r.min + Vector2.Scale(r.size, pulseUv), Pixel(2.5f), Alpha(ink, .7f));
            XgDraw.Box(vh, r.min, new Vector2(r.xMax, r.yMin + Pixel(1.5f)), Alpha(ink, .65f));
            if (lastReaction == "nan")
            {
                var at = r.min + Vector2.Scale(r.size, pulseUv); float width = Pixel(7);
                XgDraw.Seg(vh, at - new Vector2(width, width), at + new Vector2(width, width), Pixel(1), ink);
                XgDraw.Seg(vh, at + new Vector2(-width, width), at + new Vector2(width, -width), Pixel(1), ink);
            }
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            MeshBuilds++; vh.Clear(); var r = rectTransform.rect;
            DrawStatic(vh, r);
            Signature(vh, r, signature, signatureUv, signatureInk, signatureMoving && !Reduced ? Mathf.Clamp01((Time.unscaledTime - began) / .7f) : 1, .6f);
            if (Reduced || !animating) return;
            float t = Mathf.Clamp01((Time.unscaledTime - began) / 1.2f), fade = Mathf.Sin(t * Mathf.PI);
            var at = r.min + Vector2.Scale(r.size, movingUv);
            if (animation == "correct" || animation == "wrong" || animation == "epoch" || animation == "dataset")
                Comets(vh, r, at, movingInk, Time.unscaledTime - began, animation == "epoch" ? 4 : 1);
            float radius = animation == "stage" || animation == "ability"
                ? Mathf.Lerp(Pixel(5), Mathf.Min(r.width, r.height) * .42f, t)
                : Pixel(animation == "wrong" ? Mathf.Lerp(14, 4, t) : Mathf.Lerp(4, 18, t));
            // A thin, low-opacity ring replaces the former full-window opaque white flash.
            XgDraw.Ring(vh, at, radius, Pixel(1.2f), Alpha(movingInk, fade * .38f));
            if (animation == "epoch")
                XgDraw.Seg(vh, at + new Vector2(-Pixel(18), Mathf.Lerp(Pixel(16), -Pixel(16), t)),
                    at + new Vector2(Pixel(18), Mathf.Lerp(Pixel(16), -Pixel(16), t)), Pixel(1), Alpha(movingInk, fade * .22f));
        }
    }
}
