from pathlib import Path
import hashlib
import json

root = Path.cwd()
dest = root / 'Documentation/DesignPass/Resume/CorrectionWriteCandidates'
paths = [
    'Assets/LingGuangV05/XingGuang/Core/XgBoard.cs',
    'Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs',
    'Assets/LingGuangV05/XingGuang/Core/XgSim.AnswerTrace.cs',
    'Assets/LingGuangV05/XingGuang/Core/XgSim.cs',
    'Assets/LocalBuild/Editor/AnswerTraceChecks.cs',
]
expected = [
    '6C787FAC61949D080C0D5994B129F2C509B8BD52E26297EDEA0F3CACDFA74B99',
    'B8C06FD6EE0047198E73C7CF595B497F30AD9B708EA6E6A7BA29126600071DA1',
    '7783F161DBD3EA12FC4F98386F498685B62C54F25E175968A57D711B880139F6',
    'CC5A252CE52C9E0AC6959C55DFFD25DA83B365AA65669FC19BFAAC1D5134B204',
    'D138E2E22AA14E7D02C4E9B78D214E80A940B314477C2C6213F27B5283494571',
]
texts = {}
manifest = []
for rel, digest in zip(paths, expected):
    raw = (root / rel).read_bytes()
    assert hashlib.sha256(raw).hexdigest().upper() == digest, rel + ' source changed'
    texts[Path(rel).name] = raw.decode('utf-8-sig').replace('\r\n', '\n')
    manifest.append({'path': rel, 'before': digest})

def replace(name, old, new):
    assert texts[name].count(old) == 1, (name, old)
    texts[name] = texts[name].replace(old, new)

replace('XgBoard.cs', '''        public XgBoardStep Train(XgBoardCard card, XgKnobs k)
        {
            var step = new XgBoardStep();''', '''        public XgBoardStep Train(XgBoardCard card, XgKnobs k) => Train(card, k, null);

        // Optional transient evidence of R1 writes and R2 creations, captured before R4 decay.
        internal XgBoardStep Train(XgBoardCard card, XgKnobs k, List<XgConceptPull> pulls)
        {
            var step = new XgBoardStep();
            HashSet<int> created = null;''')
replace('XgBoard.cs', '''                if (!byKey.ContainsKey(card.region + "|" + elements[i].id)) { if (Create(card.region, elements[i].id, 1, k) != null) step.created++; }''', '''                if (!byKey.ContainsKey(card.region + "|" + elements[i].id))
                {
                    var made = Create(card.region, elements[i].id, 1, k);
                    if (made == null) continue;
                    step.created++;
                    if (pulls != null)
                    {
                        if (created == null) created = new HashSet<int>();
                        created.Add(made.id);
                    }
                }''')
replace('XgBoard.cs', '''                if (k.dropout && Dropped(card.seed, m.c.id)) continue;
                double pull =''', '''                if (k.dropout && Dropped(card.seed, m.c.id)) continue;
                double beforeWeight = m.c.w, beforeStrength = m.c.s;
                double pull =''')
replace('XgBoard.cs', '''                m.c.s = Math.Min(10, m.c.s + m.act);
                m.c.seen''', '''                m.c.s = Math.Min(10, m.c.s + m.act);
                if (pulls != null && (m.c.w != beforeWeight || m.c.s != beforeStrength))
                    pulls.Add(new XgConceptPull(m.c, beforeWeight, beforeStrength, created != null && created.Contains(m.c.id)));
                m.c.seen''')
replace('XgBoard.cs', '''                        if (made != null) { made.w = (a.c.w + b.c.w) * .5; made.trainedCards = 1; step.created++; }''', '''                        if (made != null)
                        {
                            made.w = (a.c.w + b.c.w) * .5; made.trainedCards = 1; step.created++;
                            // R2 created this cell; zero is its absent-state baseline.
                            if (pulls != null) pulls.Add(new XgConceptPull(made, 0, 0, true));
                        }''')
replace('XgSim.Board.cs', 'void TeachBoard(XgCard card, bool label)', 'void TeachBoard(XgCard card, bool label, List<XgConceptPull> pulls = null)')
replace('XgSim.Board.cs', '            Board.Train(b, Knobs(run));', '            Board.Train(b, Knobs(run), pulls);')
replace('XgSim.cs', '''                    var beforeCorrection = traceReceiver != null && correcting ? CaptureBeforeCorrection(card.dataset) : null;
                    TeachBoard(card, card.truth);
                    if (beforeCorrection != null) correctionPulls = CaptureCorrectionPulls(card.dataset, beforeCorrection);''', '''                    if (traceReceiver != null && correcting) correctionPulls = new List<XgConceptPull>();
                    TeachBoard(card, card.truth, correctionPulls);''')
start = texts['XgSim.AnswerTrace.cs'].index('        struct BeforePull')
end = texts['XgSim.AnswerTrace.cs'].index('        static XgAnswerTraceSource AnswerTraceSource', start)
texts['XgSim.AnswerTrace.cs'] = texts['XgSim.AnswerTrace.cs'][:start] + texts['XgSim.AnswerTrace.cs'][end:]
replace('XgSim.AnswerTrace.cs', '''    /// <summary>Values on either side of this lesson's actual training step, excluding untouched and pinned cells.</summary>''', '''    /// <summary>
    /// Values around an actual R1 write or R2 creation, captured before R4 decay, not the final board values.
    /// New R1 cells start at weight 0/strength 1; R2 creations use an absent-state baseline of 0/0.
    /// Pinned, Dropout-skipped and unchanged R1 cells contribute no record.
    /// </summary>''')
replace('AnswerTraceChecks.cs', '        CheckCorrectionPulls();', '        CheckCorrectionPulls();\n        CheckClampedCorrectionPulls();\n        CheckCreatedCorrectionPulls();')
replace('AnswerTraceChecks.cs', '            // The unrelated cell can fade but is not pulled by this card\'s lesson.', '            // This unrelated cell is not pulled by this card\'s lesson.')
replace('AnswerTraceChecks.cs', '            sim.S.board.cards = XgBoard.DecayEvery - 1;\n', '')
replace('AnswerTraceChecks.cs', 'pull.BeforeStrength == (existed ? old.s : 0)', 'pull.BeforeStrength == (existed ? old.s : after.layer == 1 ? 1 : 0)')
replace('AnswerTraceChecks.cs', 'Correction captures exact before/after values, including newly learned cells', 'Correction captures exact pre-decay write values, including initialized new R1 cells')
replace('AnswerTraceChecks.cs', '    static void Require(bool value, string message)', '''    static void CheckClampedCorrectionPulls()
    {
        double learnedWeight = 0, learnedStrength = 0;
        foreach (bool decay in new[] { false, true })
        {
            var template = new XgSim();
            var run = template.Run(XgCatalog.Dataset("spam").track);
            run.depth = 1; run.width = XgCatalog.Widths.Length - 1;
            var card = new XgCard { id = 89001, dataset = "spam", seed = 9139,
                question = "恭喜您中奖，请点击领取", truth = false, level = 1 };
            template.S.cards.RemoveAll(c => c.dataset == "spam"); template.S.cards.Add(card);
            var prepared = XgBoardData.FromCard(card);
            var knobs = template.Knobs(run);
            var elements = XgBoard.Elements(prepared, knobs);
            Require(!knobs.dropout && elements.Count > 2, "Clamp fixture has three actual features without Dropout");
            template.S.board = new XgBoardState { cards = XgBoard.DecayEvery - (decay ? 1 : 2), nextId = 901 };
            template.S.board.concepts.Add(new XgConcept { id = 17, region = prepared.region, key = elements[0].id,
                layer = 1, w = .8, s = 1, pinned = true, trainedCards = 7, trainingCountKnown = true });
            template.S.board.concepts.Add(new XgConcept { id = 18, region = prepared.region, key = elements[1].id,
                layer = 1, w = -XgBoard.WeightLimit, s = 10, trainedCards = 5, trainingCountKnown = true });
            template.S.board.concepts.Add(new XgConcept { id = 19, region = prepared.region, key = elements[2].id,
                layer = 1, w = .25, s = 1, trainedCards = long.MaxValue, trainingCountKnown = true });
            string saved = JsonUtility.ToJson(template.S);
            var sim = new XgSim(JsonUtility.FromJson<XgState>(saved));
            var control = new XgSim(JsonUtility.FromJson<XgState>(saved));
            Require(sim.TeachingGuess(sim.Card("spam"), out bool guess) && guess &&
                control.TeachingGuess(control.Card("spam"), out bool plainGuess) && plainGuess,
                "Both real teaching predictions are wrong before correction");
            XgAnswerTrace trace = null; int events = 0;
            sim.CardAnswered += value => { events++; trace = value; };
            var wallet = new Wallet(); var plainWallet = new Wallet();
            bool plainAccepted = control.TryEvaluateTeachingGuess("spam", card.id, guess, false, plainWallet, out var plain);
            bool accepted = sim.TryEvaluateTeachingGuess("spam", card.id, guess, false, wallet, out var result);
            Require(plainAccepted && accepted && result.corrected && events == 1,
                "The real teaching UI route settles one correction, with and without an observer");
            Require(JsonUtility.ToJson(plain) == JsonUtility.ToJson(result) && JsonUtility.ToJson(control.S) == JsonUtility.ToJson(sim.S)
                && wallet.Money == plainWallet.Money, "Write capture preserves outcome, state, RNG and payment across the decay boundary");
            var clamped = sim.S.board.concepts.Find(c => c.id == 18);
            var changed = sim.S.board.concepts.Find(c => c.id == 19);
            XgConceptPull learned = null;
            foreach (var pull in trace.CorrectionPulls)
            {
                Require(pull.Id != 17 && pull.Id != 18, "Pinned and fully clamped R1 cells never claim a write, even when R4 fades them");
                if (pull.Id == 19) learned = pull;
            }
            Require(clamped.trainedCards == 6 && changed.trainedCards == long.MaxValue,
                "Real R1 runs for the clamped cell and still writes when the lesson counter is saturated");
            Require(learned != null && !learned.Created && learned.BeforeWeight == .25 && learned.BeforeStrength == 1
                && learned.AfterWeight < .25 && learned.AfterStrength > 1, "An actual changed cell is captured with precise prior values");
            if (!decay)
            {
                Require(clamped.w == -XgBoard.WeightLimit && clamped.s == 10, "Clamping and saturation leave both parameters unchanged before decay");
                learnedWeight = learned.AfterWeight; learnedStrength = learned.AfterStrength;
                Require(changed.w == learnedWeight && changed.s == learnedStrength, "Without R4 the write snapshot equals the final board values");
            }
            else
            {
                Require(clamped.w > -XgBoard.WeightLimit && clamped.s < 10, "The clamped cell actually fades on the decay step");
                Require(learned.AfterWeight == learnedWeight && learned.AfterStrength == learnedStrength,
                    "The same actual write has the same snapshot on either side of the decay boundary");
                Require(changed.w != learned.AfterWeight && changed.s < learned.AfterStrength,
                    "The snapshot intentionally precedes R4, rather than claiming the decayed final values");
            }
        }
        Debug.Log("CORRECTION_WRITE_CHECKS_PASS: clamped decay-only cells excluded; actual writes, saturated counters and pre-decay values retained.");
    }

    static void CheckCreatedCorrectionPulls()
    {
        var initialWrites = new Dictionary<int, XgConceptPull>();
        foreach (bool decay in new[] { false, true })
        {
            var sim = Fixture(false, out var card);
            sim.S.unlocked.Add("relu");
            var run = sim.Run(XgCatalog.Dataset("spam").track);
            run.depth = 2; run.width = XgCatalog.Widths.Length - 1; run.lr = 1; run.act = 2;
            sim.S.board.concepts[0].pinned = true;
            sim.S.board.cards = XgBoard.DecayEvery - (decay ? 1 : 2);
            Require(sim.TeachingGuess(card, out bool guess) && guess, "R2 fixture uses a real wrong teaching prediction");
            XgAnswerTrace trace = null;
            sim.CardAnswered += value => trace = value;
            Require(sim.TryEvaluateTeachingGuess("spam", card.id, guess, false, new Wallet(), out var result) && result.corrected,
                "R2 fixture settles through the actual teaching route");
            int combinations = 0;
            foreach (var pull in trace.CorrectionPulls)
            {
                var cell = sim.S.board.concepts.Find(c => c.id == pull.Id);
                Require(cell != null && cell.id != 17 && pull.Created && pull.BeforeWeight == 0,
                    "Every captured cell in this fixture was actually created, excluding the pinned anchor");
                if (cell.layer > 1)
                {
                    combinations++;
                    Require(pull.BeforeStrength == 0 && pull.AfterStrength == 1 && cell.trainedCards == 1,
                        "Actual R2 creation captures an absent 0/0 baseline and the initialized combination before decay");
                }
                else Require(pull.BeforeStrength == 1, "R1 creation records its initialized strength before the actual pull");
                if (!decay)
                {
                    initialWrites.Add(pull.Id, pull);
                    Require(pull.AfterWeight == cell.w && pull.AfterStrength == cell.s, "Without R4 the creation/write snapshot equals board values");
                }
                else
                {
                    Require(initialWrites.TryGetValue(pull.Id, out var previous) && previous.AfterWeight == pull.AfterWeight
                        && previous.AfterStrength == pull.AfterStrength, "R1 and R2 capture the same writes before a later decay step");
                    Require(cell.s < pull.AfterStrength, "Created cells really decay after the immutable evidence was captured");
                }
            }
            Require(combinations > 0, "The real training rule must create at least one higher-layer combination");
        }
        Debug.Log("CORRECTION_CREATION_CHECKS_PASS: actual R1 writes and R2 creations captured before decay with distinct initial baselines.");
    }

    static void Require(bool value, string message)''')

for entry in manifest:
    path = dest / Path(entry['path']).name
    path.write_text(texts[path.name], encoding='utf-8', newline='\n')
    entry['candidate'] = hashlib.sha256(path.read_bytes()).hexdigest().upper()
(dest / 'hashes.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
print('Prepared five hash-guarded candidates outside Assets.')
