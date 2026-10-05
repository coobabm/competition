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
        /// <summary>A hand-made feature (特征工程 n-gram): used as it is, never merged with others by R2.</summary>
        public bool made;
        public XgFeature() { }
        public XgFeature(string name, int x = -1, int y = 0, int seq = 0) { this.name = name; this.x = x; this.y = y; this.seq = seq; }
    }

    /// <summary>A card as the brain sees it: raw features, the region it trains and the label it was given.</summary>
    public sealed partial class XgBoardCard
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
        /// <summary>Board card count when this concept last matched a training card.</summary>
        public long seen;
        /// <summary>Board card count when it was made (R3 spares it for <see cref="XgBoard.GraceCards"/> cards; 0 in older saves).</summary>
        public long born;
        public XgConcept MemberwiseCloneConcept() => (XgConcept)MemberwiseClone();
    }

    [Serializable]
    public sealed class XgLink
    {
        public int a, b; public double c;
        /// <summary>Seed of the card the link last grew on (Dropout: a pair must fire together on different cards).</summary>
        [NonSerialized] public int last;
    }

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
        /// <summary>
        /// Dropout: each training card leaves some concepts out (they are not pulled), and a combination only grows from
        /// pairs that fire together on different cards — the coincidences of a single card cannot build anything.
        /// </summary>
        public bool dropout;
        /// <summary>
        /// 单位初始化 (IRNN, Le, Jaitly &amp; Hinton 2015): a plain loop of ReLU units whose recurrent weights start as the
        /// identity passes its memory on unchanged by default, so it remembers far back without gates. Only with ReLU
        /// (an S-curve squashes it again), and the same open path lets a high rate blow the gradient up (clip it).
        /// </summary>
        public bool identityInit;
        /// <summary>特征工程 (the pre-deep-learning road): hand-made features, read per region (see <see cref="XgBoard.Elements"/>).</summary>
        public bool features;
        /// <summary>偏置: every card also lights a constant element, so the board can shift its threshold.</summary>
        public bool bias;
        public double lr = .1;
        /// <summary>The optimiser's tolerance for big steps (Adam 1.25, RMSProp 1.15, SGD 1).</summary>
        public double steadiness = 1;

        /// <summary>
        /// The step (rate × error) past which the weights tear (NaN). Deeper stacks tear sooner (each layer multiplies
        /// the step; less so behind shortcuts), loops sooner still (the same weights again every word, gradients
        /// explode) unless clipped; BatchNorm lets the rate go higher, clipping caps the step, adaptive optimisers help
        /// a little. Nothing makes a step of any size safe.
        /// </summary>
        public double TearAt
        {
            get
            {
                bool loop = wiring == XgWiring.Recurrent || wiring == XgWiring.GatedRecurrent || wiring == XgWiring.EncoderDecoder || wiring == XgWiring.Attention;
                double limit = XgBoard.TearLimit * (batchNorm ? 1.5 : 1) * (clip ? 2 : 1) * steadiness;
                limit /= 1 + .03 * Math.Max(0, depth - 1) * (skip ? .3 : 1);
                if (loop && !clip) limit *= .75;
                if (IdentityLoop && !clip) limit *= .5;
                return limit;
            }
        }

        public int Cells { get { return Math.Max(1, width) * Math.Max(1, depth); } }

        /// <summary>
        /// Layer factor g, the share of the error that gets one layer further down: a step passes nothing (its slope
        /// is zero), an S-curve at most .25 (its steepest slope), ReLU .98 (slope 1 where it is on); BatchNorm +.05.
        /// A skip connection carries the error past a layer whole, but only around a layer that has a slope at all.
        /// </summary>
        public double G
        {
            get
            {
                double g = activation == XgActivation.Step ? 0 : activation == XgActivation.Sigmoid ? .25 : .98;
                if (g <= 0) return 0;
                return skip ? 1 : Math.Min(1, g + (batchNorm ? .05 : 0));
            }
        }

        /// <summary>
        /// 传话 (the degradation of plain deep nets, He et al. 2015): a vote cast below the top has to be passed on by
        /// every layer above it, and a plain layer cannot learn to pass things on exactly unchanged (an identity map
        /// is hard to learn through stacked nonlinear layers). Each keeps this share of the vote as it was (ReLU .95,
        /// a step's yes/no .9, a saturating S-curve .8) and rewrites the rest (<see cref="RelayNoise"/>). BatchNorm
        /// helps a little — enough for about 20 layers, not for 30 — and a skip connection makes passing on the default.
        /// </summary>
        public double RelayKeep
        {
            get
            {
                if (skip) return 1;
                double keep = activation == XgActivation.Step ? .9 : activation == XgActivation.Sigmoid ? .8 : .95;
                return Math.Min(1, keep + (batchNorm ? .02 : 0));
            }
        }

        /// <summary>传话: how much each plain layer rewrites a vote it should pass on unchanged (relative to the vote); BatchNorm softens it, a skip removes it.</summary>
        public double RelayNoise => skip ? 0 : batchNorm ? .095 : .12;

        /// <summary>What is left of a vote cast <paramref name="layersAbove"/> layers below the answer, and how garbled it is.</summary>
        public double RelayLeft(int layersAbove) => layersAbove <= 0 ? 1 : Math.Pow(RelayKeep, layersAbove);
        public double RelayGarble(int layersAbove) => layersAbove <= 0 ? 0 : RelayNoise * Math.Sqrt(layersAbove);

        /// <summary>A plain ReLU loop started from the identity (IRNN): it hands its memory on unchanged by default.</summary>
        public bool IdentityLoop => identityInit && wiring == XgWiring.Recurrent && activation == XgActivation.Relu;

        /// <summary>Per-step memory of the sequence wiring (R6): plain loops ×.75 (×.97 as an IRNN), gated loops ×.97, others no decay.</summary>
        public double SequenceDecay
        {
            get
            {
                switch (wiring)
                {
                    case XgWiring.Recurrent: return IdentityLoop ? XgBoard.IdentityKeep : .75;
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
        /// <summary>R1 step (rate × error) above this tears the weights apart (NaN) for a shallow plain net; see <see cref="XgKnobs.TearAt"/>.</summary>
        public const double TearLimit = .8;
        public const double ClipLimit = .5;
        public const double WeightLimit = 4;
        /// <summary>Source tokens an encoder–decoder keeps sharp after squeezing the sentence into one vector; earlier ones fade.</summary>
        public const int BottleneckTokens = 4;
        public const double BottleneckFade = .75;
        /// <summary>How far apart two strokes may be for a hand-made (特征工程) descriptor to combine them.</summary>
        public const int FeatureReach = 2;
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
        /// <summary>A new concept cannot be squeezed out for this many training cards: it gets the chance to earn a weight.</summary>
        public const int GraceCards = 50;
        /// <summary>Weights this close to zero are dead: a new raw element may take their cell (R3).</summary>
        public const double DeadWeight = .05;
        /// <summary>Test hook: the balance bot switches 传话 off to measure one rule change at a time.</summary>
        internal static bool RelayOn = true;

        public XgBoard(XgBoardState state = null)
        {
            S = state ?? new XgBoardState();
            if (S.concepts == null) S.concepts = new List<XgConcept>();
            if (S.links == null) S.links = new List<XgLink>();
            Rebuild();
        }

        // ───────────── elements (R6 decides what a card exposes) ─────────────

        /// <summary>An element of one card under the current wiring: id, activation and where it sits (for reach).</summary>
        public struct Element { public string id; public double act; public int x, y, seq, back; public bool positioned, made; }

        public static List<Element> Elements(XgBoardCard card, XgKnobs k)
        {
            if (k.features) card = Engineered(card);
            var list = new List<Element>(card.features.Count + 1);
            if (k.bias) list.Add(new Element { id = BiasElement, act = 1, x = -1 });
            var lengths = new Dictionary<int, int>();
            foreach (var f in card.features)
                if (f.x >= 0) { lengths.TryGetValue(f.seq, out int n); lengths[f.seq] = Math.Max(n, f.x + 1); }
            bool sequence = card.region == "sequence";
            foreach (var f in card.features)
            {
                var e = new Element { act = 1, x = f.x, y = f.y, seq = f.seq, positioned = f.x >= 0, made = f.made };
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
                        // A convolution over text (TextCNN) reads word groups wherever they are; with position tags
                        // (as convolutional translators added them) it also knows where each word sits.
                        e.id = sequence && k.position ? f.name + "@-" + fromEnd : f.name;
                        break;
                    case XgWiring.AnyToAny:
                        // Two sentences (translation): every word may look at every word of both; with position tags
                        // the output word lines itself up with the source word in the same place (see Reach). One
                        // sentence: position tags tell the first 春 from a later one.
                        if (lengths.Count > 1) e.id = f.name + (f.seq > 0 ? "→" : "");
                        else e.id = k.position ? f.name + "@-" + fromEnd : f.name;
                        break;
                    case XgWiring.EncoderDecoder:
                        // The whole source sentence is squeezed into one fixed-size vector (定长瓶颈): the end of the
                        // sentence comes through clearly, the further back a word is the more it blurs.
                        e.id = f.name + (f.seq > 0 ? "→" : "");
                        e.act = f.seq == 0 ? Math.Pow(BottleneckFade, Math.Max(0, fromEnd - BottleneckTokens + 1)) : Math.Pow(k.SequenceDecay, fromEnd);
                        break;
                    case XgWiring.Attention:
                        // The decoder may look back at any source position.
                        e.id = f.name + (f.seq > 0 ? "→" : "");
                        e.act = f.seq == 0 ? 1 : Math.Pow(k.SequenceDecay, fromEnd);
                        break;
                    default: // Recurrent / GatedRecurrent: the loop carries the past forward, weaker every step.
                        e.id = f.name;
                        e.act = Math.Pow(k.SequenceDecay, fromEnd);
                        // A loop starts at the first word, so it knows which word opened the sentence.
                        if (f.x == 0 && f.seq == 0) list.Add(new Element { id = "^" + f.name, act = e.act, x = f.x, y = f.y, seq = f.seq, back = fromEnd, positioned = true });
                        break;
                }
                list.Add(e);
            }
            return list;
        }

        /// <summary>
        /// 特征工程: what a person would hand-make before networks could find it themselves. 逻辑 cards gain every pair of
        /// their elements as one new element (a feature cross: one layer can then answer 异或); 视觉 cards lose the stray
        /// dot and move to their top-left corner (denoise + centre); 序列 cards drop filler words and become fixed
        /// unigram and bigram features that R2 never merges (a linear n-gram reader: no word order beyond two).
        /// </summary>
        public static XgBoardCard Engineered(XgBoardCard card)
        {
            var made = new XgBoardCard { region = card.region, seed = card.seed, truth = card.truth };
            switch (card.region)
            {
                case "logic":
                    made.features.AddRange(card.features);
                    for (int i = 0; i < card.features.Count; i++)
                        for (int j = i + 1; j < card.features.Count; j++)
                        {
                            var a = card.features[i]; var b = card.features[j];
                            if (a.x >= 0 || b.x >= 0) continue;
                            made.features.Add(new XgFeature(string.CompareOrdinal(a.name, b.name) < 0 ? a.name + "&" + b.name : b.name + "&" + a.name));
                        }
                    break;
                case "vision":
                    // Denoise (drop the stray dot), then move the figure to the top-left corner.
                    int minX = int.MaxValue, minY = int.MaxValue;
                    foreach (var f in card.features) if (f.x >= 0 && f.name != XgBoardData.StrayDot) { minX = Math.Min(minX, f.x); minY = Math.Min(minY, f.y); }
                    foreach (var f in card.features)
                    {
                        if (f.x < 0) made.features.Add(f);
                        else if (f.name != XgBoardData.StrayDot) made.features.Add(new XgFeature(f.name, f.x - minX, f.y - minY, f.seq));
                    }
                    break;
                default:
                    // Unigrams and bigrams, each one fixed feature: a linear n-gram reader, the way text was classified
                    // before networks read in order.
                    XgFeature previous = null;
                    foreach (var f in card.features)
                    {
                        if (f.x < 0 || f.seq > 0) { made.features.Add(f); continue; }
                        if (XgBoardData.IsFiller(f.name)) continue;
                        made.features.Add(new XgFeature(f.name) { made = true });
                        if (previous != null) made.features.Add(new XgFeature(previous.name + f.name) { made = true });
                        previous = f;
                    }
                    break;
            }
            return made;
        }

        /// <summary>R6 reach between two elements of the same card.</summary>
        public static bool Reach(Element a, Element b, XgKnobs k)
        {
            if (a.made || b.made) return false;
            if (!a.positioned || !b.positioned) return true;
            // 特征工程 on a dense net: hand-made local descriptors only combine strokes near each other (positions stay bound).
            if (k.features && k.wiring == XgWiring.Full) return a.seq == b.seq && Math.Abs(a.x - b.x) <= FeatureReach && Math.Abs(a.y - b.y) <= FeatureReach;
            switch (k.wiring)
            {
                case XgWiring.Full: return true;
                // Self-attention reaches everything; position tags let it line two sentences up word for word.
                case XgWiring.AnyToAny: return a.seq == b.seq || !k.position || a.x == b.x;
                case XgWiring.LocalShared: return a.seq == b.seq && Math.Abs(a.x - b.x) <= 1 && Math.Abs(a.y - b.y) <= 1;
                // Across the two sentences the decoder lines each output word up with its source word (with an
                // encoder–decoder only as clearly as the squeezed sentence still holds that word; see Elements).
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
        public double Score(List<Match> matches, XgKnobs k, int seed, out bool guessed, out int topLayer)
        {
            topLayer = 0;
            double score = 0;
            foreach (var m in matches)
                if (m.c.pinned && Math.Abs(m.c.w) > 1e-3) { topLayer = Math.Max(1, m.c.layer); guessed = false; return m.c.w * 100; }
            int depth = k != null ? k.depth : 1;
            foreach (var m in matches)
            {
                if (Math.Abs(m.c.w) <= 1e-3) continue;
                double vote = m.c.w * m.act;
                // 传话: a vote cast below the top layer is relayed up through the layers above it, weaker and more
                // garbled each time. The garble belongs to the layer, not to the concept: everything one layer sends up
                // on a card is distorted the same way, so many votes cannot average it out (and the same card always
                // reads the same).
                int above = Math.Max(0, depth - Math.Min(depth, m.c.layer));
                if (above > 0 && RelayOn) vote = vote * k.RelayLeft(above) + Math.Abs(vote) * k.RelayGarble(above) * Jitter(seed, m.c.layer);
                score += vote;
                if (m.c.layer > topLayer) topLayer = m.c.layer;
            }
            guessed = topLayer == 0;
            return score;
        }

        /// <summary>Dropout's share of concepts left out of a training card.</summary>
        public const double DropoutRate = .2;

        /// <summary>Whether Dropout leaves this concept out of this card (fixed per pair, so a replay drops the same).</summary>
        static bool Dropped(int seed, int id) => (Jitter(seed ^ 0x6D2B79F5, id) + 1) * .5 < DropoutRate;

        /// <summary>A fixed number in [−1, 1] per (card, layer): 传话 garble.</summary>
        static double Jitter(int seed, int id)
        {
            unchecked
            {
                uint h = (uint)seed * 2654435761u ^ (uint)id * 2246822519u;
                h ^= h >> 15; h *= 0x2C1B3C6Du; h ^= h >> 12; h *= 0x297A2D39u; h ^= h >> 15;
                return (h & 0xFFFF) / 32767.5 - 1;
            }
        }

        /// <summary>What an identity-initialised ReLU loop (IRNN) keeps of every word per step: all words alike, unlike gates.</summary>
        public const double IdentityKeep = .96;

        /// <summary>Gated memory keeps a word it has learnt matters this much per step, and lets the rest go this fast.</summary>
        public const double GateKeep = .99, GateForget = .95, GateWeight = .3;

        /// <summary>
        /// LSTM / GRU gates decide what to keep: a word that takes part in a concept carrying weight (alone or in a
        /// combination) is remembered across the sentence (×.99 a step); the rest fades faster (×.95). Plain loops fade
        /// everything alike (×.75).
        /// </summary>
        void Gate(List<Element> elements, XgBoardCard card, XgKnobs k)
        {
            if (k.wiring != XgWiring.GatedRecurrent) return;
            for (int i = 0; i < elements.Count; i++)
            {
                var e = elements[i];
                if (!e.positioned || e.made) continue;
                bool matters = false;
                if (byElement.TryGetValue(e.id, out var uses))
                    foreach (var c in uses) if (c.region == card.region && Math.Abs(c.w) >= GateWeight) { matters = true; break; }
                e.act = Math.Pow(matters ? GateKeep : GateForget, e.back);
                elements[i] = e;
            }
        }

        public bool Predict(XgBoardCard card, XgKnobs k, out bool guessed)
        {
            var elements = Elements(card, k);
            Gate(elements, card, k);
            double score = Score(Matches(card, elements, k), k, card.seed, out guessed, out _);
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
            Gate(elements, card, k);
            // Unknown elements become layer-1 concepts first (R3 decides whether they fit).
            for (int i = 0; i < elements.Count; i++)
                if (!byKey.ContainsKey(card.region + "|" + elements[i].id)) { if (Create(card.region, elements[i].id, 1, k) != null) step.created++; }
            var matches = Matches(card, elements, k);
            double score = Score(matches, k, card.seed, out bool guessed, out _);
            bool predicted = guessed ? (card.seed & 1) == 0 : score > 0;
            step.correct = predicted == card.truth; step.guessed = guessed;
            double target = card.truth ? 1 : -1;
            double err = target - Math.Tanh(score);
            step.error = err;
            double g = k.G;

            // A step this large tears the weights apart before anything else happens (NaN).
            if (k.lr * Math.Abs(err) > k.TearAt) step.diverged = true;

            // R1 拉: pull every matched concept toward the label, weaker per layer below the output (g).
            foreach (var m in matches)
            {
                if (m.c.pinned) continue;
                if (k.dropout && Dropped(card.seed, m.c.id)) continue;
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
                        if (!byId.ContainsKey(a.c.id) || !byId.ContainsKey(b.c.id)) continue;
                        int layer = Math.Max(a.c.layer, b.c.layer) + 1;
                        if (layer > k.depth || !Reachable(a, b, elements, k)) continue;
                        string key = Union(keyParts[a.c.id], keyParts[b.c.id]);
                        if (key == null || byKey.ContainsKey(card.region + "|" + key)) continue;
                        var link = Link(a.c.id, b.c.id);
                        // Dropout: the same card again proves nothing new about this pair.
                        if (k.dropout && link.last == card.seed && link.c > 0) continue;
                        link.last = card.seed;
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
                // A similar concept on the same layer takes it in (superposition: one cell, two meanings, as real
                // neurons end up serving several features when there are too few of them). Otherwise R3 prunes by
                // magnitude: the concept whose weight is nearest zero, the one that adds least to any answer, gives way,
                // however often it fires. Concepts younger than GraceCards are spared so a newcomer can earn a weight.
                XgConcept similar = null, victim = null; double bestSim = .5;
                var mine = key.Split('+');
                foreach (var c in S.concepts)
                {
                    if (c.region != region || c.seed || c.pinned) continue;
                    if (c.layer == layer && c.alt.Length == 0) { double sim = Jaccard(keyParts[c.id], mine); if (sim >= bestSim) { bestSim = sim; similar = c; } }
                    // Spared: newcomers, and anything the card being trained right now is using (R1 just stamped it).
                    if (S.cards - c.born < GraceCards || c.seen == S.cards) continue;
                    double w = Math.Abs(c.w), vw = victim == null ? 0 : Math.Abs(victim.w);
                    if (victim == null || w < vw || w == vw && c.s < victim.s) victim = c;
                }
                if (similar != null)
                {
                    similar.alt = key; S.superposed++; altParts[similar.id] = mine;
                    Index(mine, similar); byKey[region + "|" + key] = similar;
                    return null;
                }
                if (victim == null) return null;
                // A raw element seen for the first time has earned nothing yet: it only takes a cell freed by a dead
                // weight. A merged concept has earned its place through repeated co-occurrence (R2) and may push out the
                // weakest weight, whatever it is.
                if (layer <= 1 && Math.Abs(victim.w) >= DeadWeight) return null;
                Remove(victim); S.evicted++;
            }
            var made = new XgConcept { id = S.nextId++, region = region, key = key, layer = layer, s = 1, seen = S.cards, born = S.cards };
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

        /// <summary>A copy of a region's learnt concepts (a checkpoint's weights; the "？" seed and pinned rules stay out).</summary>
        public List<XgConcept> SnapshotRegion(string region)
        {
            var list = new List<XgConcept>();
            foreach (var c in S.concepts) if (c.region == region && !c.seed && !c.pinned) list.Add(c.MemberwiseCloneConcept());
            return list;
        }

        /// <summary>Puts a checkpoint's weights back: the region is cleared and refilled (fresh ids; links regrow).</summary>
        public void RestoreRegion(string region, List<XgConcept> snapshot)
        {
            Reinitialise(region);
            if (snapshot != null)
                foreach (var saved in snapshot)
                {
                    if (saved.region != region || byKey.ContainsKey(region + "|" + saved.key)) continue;
                    var c = saved.MemberwiseCloneConcept();
                    c.id = S.nextId++; c.seen = S.cards; c.born = S.cards; c.seed = false; c.pinned = false;
                    S.concepts.Add(c);
                }
            Rebuild();
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
