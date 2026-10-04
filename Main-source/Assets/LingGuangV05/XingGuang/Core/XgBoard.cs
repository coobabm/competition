using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    /// <summary>Activation knob: decides the layer factor g (design v1.1 §4.3).</summary>
    public enum XgActivation { Step = 0, Sigmoid = 1, Relu = 2 }

    /// <summary>Wiring knob: decides R6 reach, which elements a card exposes and the per-step sequence decay.</summary>
    public enum XgWiring { Full = 0, LocalShared = 1, Recurrent = 2, GatedRecurrent = 3, EncoderDecoder = 4, Attention = 5, AnyToAny = 6 }

    /// <summary>One raw feature on a card. x &lt; 0 means "no position" (logic words, tone, the question itself).</summary>
    [Serializable]
    public sealed class XgFeature
    {
        public string name = "";
        public int x = -1, y, seq;
        public XgFeature() { }
        public XgFeature(string name, int x = -1, int y = 0, int seq = 0) { this.name = name; this.x = x; this.y = y; this.seq = seq; }
    }

    /// <summary>A card as the brain sees it: raw features, the region it trains and the label it was given.</summary>
    public sealed class XgBoardCard
    {
        public string region = "logic";
        public List<XgFeature> features = new List<XgFeature>();
        public bool truth;
        public int seed;
        /// <summary>Diagnostic attribute (e.g. how far the decisive clue sits from the end); -1 = none.</summary>
        public int distance = -1;
        /// <summary>Readable text of the card for wrong-answer samples.</summary>
        public string text = "";
        public XgBoardCard Add(string name, int x = -1, int y = 0, int seq = 0) { features.Add(new XgFeature(name, x, y, seq)); return this; }
    }

    /// <summary>A cell on the concept board. Layer 1 = one element; layer k = two lower concepts merged (R2).</summary>
    [Serializable]
    public sealed class XgConcept
    {
        public int id, layer;
        public string region = "", key = "";
        /// <summary>A second concept superposed into the same cell (R3); matches either key with one shared weight.</summary>
        public string alt = "";
        /// <summary>Weight toward "是" and activity (refreshed when matched, decays by R4).</summary>
        public double w, s;
        /// <summary>Read-only seed (the "？" cell): reinitialisation and R3 leave it alone; R1 still pulls it.</summary>
        public bool seed;
        /// <summary>Rule 7 钉 (design v1.1 §4.4, the ending's 底层规则): R4 cannot fade it, R3 cannot evict it, R1 cannot pull it, and any card it matches is answered by it.</summary>
        public bool pinned;
        /// <summary>Board card count when this concept last matched a training card (R3 squeezes stale ones first).</summary>
        public long seen;
        public XgConcept MemberwiseCloneConcept() => (XgConcept)MemberwiseClone();
    }

    [Serializable]
    public sealed class XgLink { public int a, b; public double c; }

    /// <summary>Everything the brain remembers. Plain lists so it serialises with the lab save.</summary>
    [Serializable]
    public sealed class XgBoardState
    {
        public List<XgConcept> concepts = new List<XgConcept>();
        [NonSerialized] public List<XgLink> links = new List<XgLink>();
        public int nextId = 1;
        public long cards, superposed, evicted, created;

        /// <summary>A deep copy (for trials on a copy of the model).</summary>
        public XgBoardState Clone()
        {
            var c = (XgBoardState)MemberwiseClone();
            c.concepts = new List<XgConcept>(concepts.Count);
            foreach (var x in concepts) c.concepts.Add((XgConcept)x.MemberwiseCloneConcept());
            c.links = new List<XgLink>(links == null ? 0 : links.Count);
            if (links != null) foreach (var l in links) c.links.Add(new XgLink { a = l.a, b = l.b, c = l.c });
            return c;
        }
    }

    /// <summary>Training-page knobs as rule parameters (design v1.1 §4.3).</summary>
    public sealed class XgKnobs
    {
        public int depth = 1, width = 8;
        public XgActivation activation = XgActivation.Step;
        public XgWiring wiring = XgWiring.Full;
        public bool skip, clip, position, warmup, batchNorm;
        /// <summary>偏置: every card also lights a constant element, so the board can shift its threshold.</summary>
        public bool bias;
        public double lr = .1;

        public int Cells { get { return Math.Max(1, width) * Math.Max(1, depth); } }

        /// <summary>Layer factor g: step passes nothing down, S-curve .25, ReLU .9, skip connections 1.0; BatchNorm +.05.</summary>
        public double G
        {
            get
            {
                if (skip) return 1;
                double g = activation == XgActivation.Step ? 0 : activation == XgActivation.Sigmoid ? .25 : .9;
                return g <= 0 ? 0 : Math.Min(1, g + (batchNorm ? .05 : 0));
            }
        }

        /// <summary>Per-step memory of the sequence wiring (R6): plain loops ×.75, gated loops ×.97, others no decay.</summary>
        public double SequenceDecay
        {
            get
            {
                switch (wiring)
                {
                    case XgWiring.Recurrent: return .75;
                    case XgWiring.GatedRecurrent: case XgWiring.EncoderDecoder: case XgWiring.Attention: return .97;
                    default: return 1;
                }
            }
        }

        public XgKnobs Copy() { return (XgKnobs)MemberwiseClone(); }
    }

    /// <summary>Result of one training card.</summary>
    public struct XgBoardStep { public bool correct, guessed, diverged; public double error; public int created; }

    /// <summary>
    /// The concept board (design v1.1 §4). Six stacked rules run on every training card; accuracy, walls, bias,
    /// confusion and drift all come out of them instead of a formula. Pure C#, deterministic, no Unity.
    /// </summary>
    public sealed class XgBoard
    {
        public const int MaxKeySize = 6;
        public const double LinkThreshold = 1;
        public const double Decay = .0015;
        /// <summary>Regions not being trained fade this much slower (灾难性遗忘 is slow, not instant).</summary>
        public const double OtherRegionDecay = .0002;
        public const int DecayEvery = 16;
        public const int PairCandidates = 16;
        /// <summary>R1 pull above this without gradient clipping tears the weights apart (NaN).</summary>
        public const double TearLimit = .8;
        public const double ClipLimit = .5;
        public const double WeightLimit = 4;
        /// <summary>Source tokens an encoder–decoder keeps after squeezing the sentence into one cell.</summary>
        public const int BottleneckTokens = 6;
        public const string BiasElement = "偏置";

        public XgBoardState S { get; private set; }

        readonly Dictionary<string, XgConcept> byKey = new Dictionary<string, XgConcept>();
        readonly Dictionary<string, List<XgConcept>> byElement = new Dictionary<string, List<XgConcept>>();
        readonly Dictionary<long, XgLink> links = new Dictionary<long, XgLink>();
        readonly Dictionary<int, XgConcept> byId = new Dictionary<int, XgConcept>();
        readonly Dictionary<string, int> regionCount = new Dictionary<string, int>();
        /// <summary>Split keys per concept id (key parts, alt parts); runtime only.</summary>
        readonly Dictionary<int, string[]> keyParts = new Dictionary<int, string[]>();
        readonly Dictionary<int, string[]> altParts = new Dictionary<int, string[]>();
        /// <summary>Links are capped at this many per cell; the weakest are pruned.</summary>
        public const int LinksPerCell = 6;
        /// <summary>A concept not matched for this many training cards is stale: R3 squeezes it out first.</summary>
        public const int StaleCards = 500;

        public XgBoard(XgBoardState state = null)
        {
            S = state ?? new XgBoardState();
            if (S.concepts == null) S.concepts = new List<XgConcept>();
            if (S.links == null) S.links = new List<XgLink>();
            Rebuild();
        }

        // ───────────── elements (R6 decides what a card exposes) ─────────────

        /// <summary>An element of one card under the current wiring: id, activation and where it sits (for reach).</summary>
        public struct Element { public string id; public double act; public int x, y, seq, back; public bool positioned; }

        public static List<Element> Elements(XgBoardCard card, XgKnobs k)
        {
            var list = new List<Element>(card.features.Count + 1);
            if (k.bias) list.Add(new Element { id = BiasElement, act = 1, x = -1 });
            var lengths = new Dictionary<int, int>();
            foreach (var f in card.features)
                if (f.x >= 0) { lengths.TryGetValue(f.seq, out int n); lengths[f.seq] = Math.Max(n, f.x + 1); }
            bool sequence = card.region == "sequence";
            foreach (var f in card.features)
            {
                var e = new Element { act = 1, x = f.x, y = f.y, seq = f.seq, positioned = f.x >= 0 };
                if (!e.positioned) { e.id = f.name; list.Add(e); continue; }
                int len = lengths[f.seq];
                int fromEnd = len - 1 - f.x;
                e.back = fromEnd;
                switch (k.wiring)
                {
                    case XgWiring.Full:
                        e.id = f.name + "@" + (f.seq > 0 ? "s" + f.seq + ":" : "") + f.x + (sequence ? "" : "," + f.y);
                        break;
                    case XgWiring.LocalShared:
                        e.id = f.name;
                        break;
                    case XgWiring.AnyToAny:
                        e.id = k.position ? f.name + "@-" + fromEnd + (f.seq > 0 ? "s" + f.seq : "") : f.name;
                        break;
                    case XgWiring.EncoderDecoder:
                        // The whole source sentence is squeezed into one fixed-size cell (定长瓶颈): only its last
                        // BottleneckTokens survive the squeeze.
                        if (f.seq == 0 && fromEnd >= BottleneckTokens) continue;
                        e.id = f.name + (f.seq > 0 ? "→" : "");
                        e.act = f.seq == 0 ? 1 : Math.Pow(k.SequenceDecay, fromEnd);
                        break;
                    case XgWiring.Attention:
                        // The decoder may look back at any source position.
                        e.id = f.name + (f.seq > 0 ? "→" : "");
                        e.act = f.seq == 0 ? 1 : Math.Pow(k.SequenceDecay, fromEnd);
                        break;
                    default: // Recurrent / GatedRecurrent: the loop carries the past forward, weaker every step.
                        e.id = f.name;
                        e.act = Math.Pow(k.SequenceDecay, fromEnd);
                        break;
                }
                list.Add(e);
            }
            return list;
        }

        /// <summary>R6 reach between two elements of the same card.</summary>
        public static bool Reach(Element a, Element b, XgKnobs k)
        {
            if (!a.positioned || !b.positioned) return true;
            switch (k.wiring)
            {
                case XgWiring.Full: case XgWiring.AnyToAny: return true;
                case XgWiring.LocalShared: return a.seq == b.seq && Math.Abs(a.x - b.x) <= 1 && Math.Abs(a.y - b.y) <= 1;
                // Across the two sentences the decoder lines each output word up with its source word.
                case XgWiring.EncoderDecoder: case XgWiring.Attention: return a.seq != b.seq ? a.x == b.x : Math.Abs(a.x - b.x) <= 1;
                default: return a.seq == b.seq && Math.Abs(a.x - b.x) <= 1;
            }
        }

        // ───────────── matching and readout ─────────────

        public sealed class Match { public XgConcept c; public double act; public List<int> elements; }

        /// <summary>
        /// Concepts present on the card. A merged concept only matches where its elements sit within reach of each
        /// other (R6), the same condition under which it was merged.
        /// </summary>
        public List<Match> Matches(XgBoardCard card, List<Element> elements, XgKnobs k)
        {
            var index = new Dictionary<string, List<int>>();
            for (int i = 0; i < elements.Count; i++)
            {
                if (!index.TryGetValue(elements[i].id, out var at)) index[elements[i].id] = at = new List<int>();
                at.Add(i);
            }
            var seen = new HashSet<int>();
            var result = new List<Match>();
            foreach (var id in index.Keys)
            {
                if (!byElement.TryGetValue(id, out var candidates)) continue;
                foreach (var c in candidates)
                {
                    if (c.region != card.region || !seen.Add(c.id)) continue;
                    var m = TryMatch(c, keyParts[c.id], index, elements, k) ?? (altParts.TryGetValue(c.id, out var alt) ? TryMatch(c, alt, index, elements, k) : null);
                    if (m != null) result.Add(m);
                }
            }
            return result;
        }

        static Match TryMatch(XgConcept c, string[] parts, Dictionary<string, List<int>> index, List<Element> elements, XgKnobs k)
        {
            foreach (var part in parts) if (!index.ContainsKey(part)) return null;
            double act = 1; var used = new List<int>(parts.Length);
            foreach (var part in parts)
            {
                var at = index[part];
                double best = -1; int bestAt = -1;
                foreach (int i in at)
                {
                    if (elements[i].act <= best) continue;
                    // Later parts must be reachable from a part already placed (a connected patch, n-gram or alignment).
                    bool reachable = used.Count == 0;
                    foreach (int j in used) if (Reach(elements[i], elements[j], k)) { reachable = true; break; }
                    if (reachable) { best = elements[i].act; bestAt = i; }
                }
                if (bestAt < 0) return null;
                act = Math.Min(act, best); used.Add(bestAt);
            }
            return new Match { c = c, act = act, elements = used };
        }

        /// <summary>
        /// Readout: every matched concept votes with its weight; the highest layer reached is reported. Nothing with a
        /// weight matched means a guess.
        /// </summary>
        public double Score(List<Match> matches, out bool guessed, out int topLayer)
        {
            topLayer = 0;
            double score = 0;
            foreach (var m in matches)
                if (m.c.pinned && Math.Abs(m.c.w) > 1e-3) { topLayer = Math.Max(1, m.c.layer); guessed = false; return m.c.w * 100; }
            foreach (var m in matches)
            {
                if (Math.Abs(m.c.w) <= 1e-3) continue;
                score += m.c.w * m.act;
                if (m.c.layer > topLayer) topLayer = m.c.layer;
            }
            guessed = topLayer == 0;
            return score;
        }

        public bool Predict(XgBoardCard card, XgKnobs k, out bool guessed)
        {
            var elements = Elements(card, k);
            double score = Score(Matches(card, elements, k), out guessed, out _);
            return guessed ? (card.seed & 1) == 0 : score > 0;
        }

        /// <summary>Share of cards answered right (the test set is never trained on).</summary>
        public double Accuracy(IList<XgBoardCard> cards, XgKnobs k)
        {
            if (cards == null || cards.Count == 0) return 0;
            int right = 0;
            foreach (var card in cards) if (Predict(card, k, out _) == card.truth) right++;
            return (double)right / cards.Count;
        }

        // ───────────── training: R1–R6 on one card ─────────────

        public XgBoardStep Train(XgBoardCard card, XgKnobs k)
        {
            var step = new XgBoardStep();
            var elements = Elements(card, k);
            // Unknown elements become layer-1 concepts first (R3 decides whether they fit).
            for (int i = 0; i < elements.Count; i++)
                if (!byKey.ContainsKey(card.region + "|" + elements[i].id)) { if (Create(card.region, elements[i].id, 1, k) != null) step.created++; }
            var matches = Matches(card, elements, k);
            double score = Score(matches, out bool guessed, out _);
            bool predicted = guessed ? (card.seed & 1) == 0 : score > 0;
            step.correct = predicted == card.truth; step.guessed = guessed;
            double target = card.truth ? 1 : -1;
            double err = target - Math.Tanh(score);
            step.error = err;
            double g = k.G;

            // A rate this large tears the output apart before anything else happens (NaN), unless gradients are clipped.
            if (!k.clip && k.lr * Math.Abs(err) > TearLimit) step.diverged = true;

            // R1 拉: pull every matched concept toward the label, weaker per layer below the output (g).
            foreach (var m in matches)
            {
                if (m.c.pinned) continue;
                double pull = k.lr * err * m.act * Math.Pow(g, Math.Max(0, k.depth - m.c.layer));
                if (k.clip) pull = Math.Max(-ClipLimit, Math.Min(ClipLimit, pull));
                m.c.w = Math.Max(-WeightLimit, Math.Min(WeightLimit, m.c.w + pull));
                m.c.s = Math.Min(10, m.c.s + m.act);
                m.c.seen = S.cards;
            }

            // R2 合: reachable pairs that fire together grow a link; past the threshold it becomes a higher concept.
            if (k.depth > 1 && g > 0 && Math.Abs(err) > 1e-6)
            {
                var top = Strongest(matches.FindAll(m => m.c.key != BiasElement), PairCandidates);
                for (int i = 0; i < top.Count; i++)
                    for (int j = i + 1; j < top.Count; j++)
                    {
                        var a = top[i]; var b = top[j];
                        int layer = Math.Max(a.c.layer, b.c.layer) + 1;
                        if (layer > k.depth || !Reachable(a, b, elements, k)) continue;
                        string key = Union(keyParts[a.c.id], keyParts[b.c.id]);
                        if (key == null || byKey.ContainsKey(card.region + "|" + key)) continue;
                        var link = Link(a.c.id, b.c.id);
                        link.c += k.lr * Math.Abs(err) * Math.Min(a.act, b.act) * Math.Pow(g, Math.Max(0, k.depth - layer + 1)) * 4;
                        if (link.c < LinkThreshold) continue;
                        link.c = 0;
                        var made = Create(card.region, key, layer, k);
                        if (made != null) { made.w = (a.c.w + b.c.w) * .5; step.created++; }
                    }
            }

            // R4 衰: everything fades a little; unrefreshed concepts disappear and free their cell.
            S.cards++;
            // Weight decay scales with the learning rate (as in real optimisers): a careful rate also forgets slowly.
            double rate = Math.Max(.1, Math.Min(3, k.lr / .1));
            if (S.cards % DecayEvery == 0) DecayAll(Math.Pow(1 - Decay * rate, DecayEvery), card.region, Math.Pow(1 - OtherRegionDecay, DecayEvery));
            return step;
        }

        static List<Match> Strongest(List<Match> matches, int count)
        {
            if (matches.Count <= count) return matches;
            var copy = new List<Match>(matches);
            copy.Sort((p, q) => (q.act * (q.c.s + Math.Abs(q.c.w))).CompareTo(p.act * (p.c.s + Math.Abs(p.c.w))));
            return copy.GetRange(0, count);
        }

        static bool Reachable(Match a, Match b, List<Element> elements, XgKnobs k)
        {
            foreach (int i in a.elements) foreach (int j in b.elements) if (i != j && Reach(elements[i], elements[j], k)) return true;
            return false;
        }

        /// <summary>Merges two sorted key part lists; null when the result is too big or adds nothing.</summary>
        static string Union(string[] a, string[] b)
        {
            var parts = new string[a.Length + b.Length]; int i = 0, j = 0, n = 0;
            while (i < a.Length || j < b.Length)
            {
                int cmp = i >= a.Length ? 1 : j >= b.Length ? -1 : string.CompareOrdinal(a[i], b[j]);
                if (cmp == 0) { parts[n++] = a[i++]; j++; }
                else if (cmp < 0) parts[n++] = a[i++];
                else parts[n++] = b[j++];
                if (n > MaxKeySize) return null;
            }
            if (n <= Math.Max(a.Length, b.Length)) return null;
            return string.Join("+", parts, 0, n);
        }

        // ───────────── R3 挤: limited cells ─────────────

        public int Count(string region) { regionCount.TryGetValue(region, out int n); return n; }

        XgConcept Create(string region, string key, int layer, XgKnobs k)
        {
            int cap = k.Cells;
            if (Count(region) >= cap)
            {
                // A similar concept on the same layer takes it in (superposition); otherwise the weakest goes.
                XgConcept similar = null, weakest = null, stale = null; double bestSim = .5;
                var mine = key.Split('+');
                foreach (var c in S.concepts)
                {
                    if (c.region != region || c.seed || c.pinned) continue;
                    if (c.layer == layer && c.alt.Length == 0) { double sim = Jaccard(keyParts[c.id], mine); if (sim >= bestSim) { bestSim = sim; similar = c; } }
                    if (weakest == null || c.s < weakest.s) weakest = c;
                    if (S.cards - c.seen > StaleCards && (stale == null || c.s < stale.s)) stale = c;
                }
                // R3 挤: a concept nobody has used for a while gives way first, however strong it once was.
                if (stale != null && similar == null) { Remove(stale); S.evicted++; similar = null; weakest = null; goto Make; }
                if (similar != null)
                {
                    similar.alt = key; S.superposed++; altParts[similar.id] = mine;
                    Index(mine, similar); byKey[region + "|" + key] = similar;
                    return null;
                }
                if (weakest == null || weakest.s >= 1) return null;
                Remove(weakest); S.evicted++;
            }
            Make:
            var made = new XgConcept { id = S.nextId++, region = region, key = key, layer = layer, s = 1, seen = S.cards };
            Add(made); S.created++;
            return made;
        }

        static double Jaccard(string[] x, string[] y)
        {
            int inter = 0;
            foreach (var p in x) foreach (var q in y) if (p == q) { inter++; break; }
            int union = x.Length + y.Length - inter;
            return union == 0 ? 0 : (double)inter / union;
        }

        // ───────────── R4 衰 ─────────────

        void DecayAll(double factor, string region = null, double otherFactor = -1)
        {
            var dead = new List<XgConcept>();
            foreach (var c in S.concepts)
            {
                if (c.seed || c.pinned) continue;
                double f = region == null || c.region == region || otherFactor < 0 ? factor : otherFactor;
                c.w *= f; c.s *= f;
                if (c.s < .05 && Math.Abs(c.w) < .02) dead.Add(c);
            }
            foreach (var c in dead) Remove(c);
            // Links fade too (per region, like concepts); links of removed concepts and each region's weakest beyond
            // its cap are dropped, so one busy region never prunes another's slowly growing links.
            var byRegion = new Dictionary<string, List<XgLink>>();
            foreach (var l in S.links)
            {
                if (!byId.TryGetValue(l.a, out var ca) || !byId.ContainsKey(l.b)) { l.c = -1; continue; }
                l.c *= region == null || ca.region == region || otherFactor < 0 ? factor : otherFactor;
                if (!byRegion.TryGetValue(ca.region, out var list)) byRegion[ca.region] = list = new List<XgLink>();
                list.Add(l);
            }
            foreach (var pair in byRegion)
            {
                int cap = LinksPerCell * Math.Max(1, Count(pair.Key));
                if (pair.Value.Count <= cap) continue;
                pair.Value.Sort((x, y) => x.c.CompareTo(y.c));
                for (int i = 0; i < pair.Value.Count - cap; i++) pair.Value[i].c = -1;
            }
            S.links.RemoveAll(l => l.c < .02);
            links.Clear(); foreach (var l in S.links) links[Pair(l.a, l.b)] = l;
        }

        /// <summary>Training stopped for a while (e.g. 寒冬): time alone fades the board.</summary>
        public void Idle(int cards)
        {
            if (cards > 0) DecayAll(Math.Pow(1 - Decay, cards));
        }

        // ───────────── seeds, resets and queries ─────────────

        /// <summary>Plants a read-only concept (the SI's "？" cell). R1 can still pull it.</summary>
        public XgConcept Plant(string region, string key, double w)
        {
            if (byKey.TryGetValue(region + "|" + key, out var existing)) { existing.seed = true; return existing; }
            var c = new XgConcept { id = S.nextId++, region = region, key = key, layer = 1, w = w, s = 10, seed = true };
            Add(c); return c;
        }

        /// <summary>Changing the depth reinitialises the network: everything but seeds is cleared (one region or all).</summary>
        public void Reinitialise(string region = null)
        {
            foreach (var c in S.concepts.ToArray()) if (!c.seed && !c.pinned && (region == null || c.region == region)) Remove(c);
            S.links.RemoveAll(l => !byId.ContainsKey(l.a) || !byId.ContainsKey(l.b));
            links.Clear(); foreach (var l in S.links) links[Pair(l.a, l.b)] = l;
        }

        /// <summary>NaN: the weights of a region are torn apart and lose part of what they held.</summary>
        public void Shake(string region, double keep)
        {
            foreach (var c in S.concepts) if (c.region == region && !c.seed && !c.pinned) c.w *= keep;
        }

        /// <summary>
        /// Rule 7 钉: pins a concept as it is now (creating it at the given direction if the board never learnt it).
        /// <paramref name="direction"/> overrides its sign (the shutdown rule locks the "？" cell to 是).
        /// </summary>
        public XgConcept Pin(string region, string key, double? direction = null)
        {
            if (!byKey.TryGetValue(region + "|" + key, out var c))
            {
                c = new XgConcept { id = S.nextId++, region = region, key = key, layer = 1, w = direction ?? 1, s = 10 };
                Add(c);
            }
            if (direction.HasValue) c.w = Math.Abs(c.w) < .5 ? direction.Value : Math.Sign(direction.Value) * Math.Abs(c.w);
            c.pinned = true;
            return c;
        }

        /// <summary>Readout without a question: the strongest concept lights up by itself (every emergence comes from this).</summary>
        public XgConcept Strongest(string region = null)
        {
            XgConcept best = null;
            foreach (var c in S.concepts)
                if ((region == null || c.region == region) && (best == null || Math.Abs(c.w) * c.s > Math.Abs(best.w) * best.s)) best = c;
            return best;
        }

        public XgConcept Find(string region, string key) { byKey.TryGetValue(region + "|" + key, out var c); return c; }

        public IEnumerable<XgConcept> Concepts(string region)
        {
            foreach (var c in S.concepts) if (c.region == region) yield return c;
        }

        public int MaxLayer(string region)
        {
            int layer = 0; foreach (var c in S.concepts) if (c.region == region) layer = Math.Max(layer, c.layer);
            return layer;
        }

        // ───────────── bookkeeping ─────────────

        void Rebuild()
        {
            byKey.Clear(); byElement.Clear(); links.Clear(); byId.Clear(); regionCount.Clear(); keyParts.Clear(); altParts.Clear();
            foreach (var c in S.concepts)
            {
                if (c.alt == null) c.alt = "";
                byId[c.id] = c; byKey[c.region + "|" + c.key] = c; keyParts[c.id] = c.key.Split('+'); Index(keyParts[c.id], c);
                if (c.alt.Length > 0) { byKey[c.region + "|" + c.alt] = c; altParts[c.id] = c.alt.Split('+'); Index(altParts[c.id], c); }
                regionCount.TryGetValue(c.region, out int n); regionCount[c.region] = n + 1;
            }
            foreach (var l in S.links) links[Pair(l.a, l.b)] = l;
        }

        void Add(XgConcept c)
        {
            S.concepts.Add(c); byId[c.id] = c; byKey[c.region + "|" + c.key] = c; keyParts[c.id] = c.key.Split('+'); Index(keyParts[c.id], c);
            regionCount.TryGetValue(c.region, out int n); regionCount[c.region] = n + 1;
        }

        void Index(string[] parts, XgConcept c)
        {
            foreach (var part in parts)
            {
                if (!byElement.TryGetValue(part, out var list)) byElement[part] = list = new List<XgConcept>();
                if (!list.Contains(c)) list.Add(c);
            }
        }

        void Remove(XgConcept c)
        {
            S.concepts.Remove(c); byId.Remove(c.id);
            byKey.Remove(c.region + "|" + c.key);
            if (c.alt.Length > 0) byKey.Remove(c.region + "|" + c.alt);
            foreach (var parts in new[] { keyParts[c.id], altParts.TryGetValue(c.id, out var alt) ? alt : null })
                if (parts != null) foreach (var part in parts) if (byElement.TryGetValue(part, out var list)) list.Remove(c);
            keyParts.Remove(c.id); altParts.Remove(c.id);
            regionCount[c.region] = Count(c.region) - 1;
            // Its links are dropped lazily by the next decay pass (they no longer resolve to a concept).
        }

        static long Pair(int a, int b) { return a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a; }

        XgLink Link(int a, int b)
        {
            // A stale link of a removed concept whose id was never reused is harmless; ids are never reused.
            long key = Pair(a, b);
            if (!links.TryGetValue(key, out var link)) { link = new XgLink { a = Math.Min(a, b), b = Math.Max(a, b) }; links[key] = link; S.links.Add(link); }
            return link;
        }
    }
}
