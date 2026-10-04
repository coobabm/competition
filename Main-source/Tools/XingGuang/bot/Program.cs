using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using LingGuangV05.Core;
using LingGuangV05.XingGuang;

/// <summary>Uses the actual household simulation, not a wallet with free power.</summary>
sealed class EconomyHost : IXgHost
{
    public readonly ChapterOneSim House;
    public double trainingKwh, paidPower, externalIncome, hardwareSpent;
    public int outages, breakerTrips;
    public EconomyHost(GameState state = null)
    {
        House = new ChapterOneSim(state);
        House.InstallApp();
        House.Signal += (signal, arg) => { if (signal == "power.unpaid") outages++; if (signal == "breaker.tripped") breakerTrips++; };
    }
    public double Compute => House.HeartbeatsPerSecond;
    public double VramMB => House.MemoryCapacity;
    public double Money => House.S.money;
    public string Blocker => House.S.unpaidPower ? "Unpaid power" : House.S.breakerTripped ? "Breaker tripped" : House.PowerWatts <= 0 ? "No power" : null;
    public bool Spend(double amount)
    {
        if (!double.IsFinite(amount) || amount < 0 || Money + 1e-9 < amount) return false;
        House.S.money -= amount; return true;
    }
    public void Earn(double amount) { if (double.IsFinite(amount) && amount > 0) { House.S.money += amount; House.S.totalEarned += amount; } }
    public void Train(double seconds)
    {
        double energy = House.S.gpuCount * House.Config.gpuWatts * .5 / 1000 * 24 * seconds / House.Config.dayLengthSeconds;
        trainingKwh += energy; House.S.energyKwh += energy;
    }
    public void Tick(double dt, double optionalExternalIncome)
    {
        double before = Money;
        House.Tick(dt);
        paidPower += Math.Max(0, before - Money);
        double extra = optionalExternalIncome * House.S.gpuCount * dt;
        if (extra > 0) { Earn(extra); externalIncome += extra; }
    }
    public bool PayPower()
    {
        double before = Money; bool paid = House.PayBill(); if (paid) paidPower += before - Money; return paid;
    }
    public bool BuyGpu() { bool bought = House.BuyGpu(); if (bought) hardwareSpent += House.Config.gpuPrice; return bought; }
    public bool BuyCase() { bool bought = House.BuyCase(); if (bought) hardwareSpent += House.Config.casePrice; return bought; }
}

sealed class Options
{
    public double Minutes = 90, ExternalIncome, LabelSeconds = 1.2, DecisionSeconds = 2, StorySeconds = 4;
    /// <summary>Minutes a player needs after a wall appears before trying the golden setting (0 = knows it at once).</summary>
    public double InsightMinutes;
    public int Seed = 7;
    public string Ending = "no", FirstSpecialty = "vision", Output = "", From = "";
    public bool FaultCheck;
    public static Options Parse(string[] args)
    {
        var o = new Options(); int position = 0;
        foreach (string arg in args)
        {
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                if (position == 0) o.Minutes = Number(arg); else if (position == 1) o.ExternalIncome = Number(arg); else throw new ArgumentException("Only minutes and external income are positional.");
                position++; continue;
            }
            int eq = arg.IndexOf('='); string name = eq < 0 ? arg : arg.Substring(0, eq), value = eq < 0 ? "" : arg.Substring(eq + 1);
            switch (name)
            {
                case "--ending": o.Ending = value; break;
                case "--first": o.FirstSpecialty = value; break;
                case "--insight-minutes": o.InsightMinutes = Number(value); break;
                case "--output": o.Output = Path.GetFullPath(value); break;
                case "--label-seconds": o.LabelSeconds = Number(value); break;
                case "--decision-seconds": o.DecisionSeconds = Number(value); break;
                case "--story-seconds": o.StorySeconds = Number(value); break;
                case "--seed": o.Seed = int.Parse(value, CultureInfo.InvariantCulture); break;
                case "--fault-check": o.FaultCheck = true; break;
                case "--from": o.From = Path.GetFullPath(value); break;
                default: throw new ArgumentException("Unknown option: " + name);
            }
        }
        if (o.Minutes <= 0 || o.Minutes > 1440 || o.ExternalIncome < 0 || o.LabelSeconds < .1 || o.DecisionSeconds < .1 || o.StorySeconds < 0) throw new ArgumentException("Invalid duration or rate.");
        if (o.Ending != "yes" && o.Ending != "no" && o.Ending != "both") throw new ArgumentException("--ending must be yes, no or both.");
        if (o.FirstSpecialty != "vision" && o.FirstSpecialty != "sequence") throw new ArgumentException("--first must be vision or sequence.");
        return o;
    }
    static double Number(string value) { double v = double.Parse(value, CultureInfo.InvariantCulture); if (!double.IsFinite(v)) throw new ArgumentException("Numbers must be finite."); return v; }
}

/// <summary>
/// Deterministic, truth-aware reachability driver. It does not prove human reading difficulty or real LLM quality.
/// Existing CLI remains valid: dotnet run -- [minutes=90] [external income per GPU=0].
/// Additional options: --ending=both --first=vision --label-seconds=1.2 --output=/absolute/report.json --fault-check --from=chapter-one.sav (continue a real save).
/// </summary>
static class Program
{
    static int Main(string[] args)
    {
        try
        {
            XgCatalog.Validate(); var options = Options.Parse(args);
            bool complete = true;
            foreach (bool regret in options.Ending == "both" ? new[] { false, true } : new[] { options.Ending == "yes" }) complete &= new Driver(options, regret).Play();
            return complete ? 0 : 1;
        }
        catch (Exception e) { Console.Error.WriteLine(e.Message); return 2; }
    }
}

sealed class Driver
{
    readonly Options options;
    readonly bool regret;
    readonly EconomyHost host;
    readonly XgSim sim;
    readonly Dictionary<string, double> milestones = new Dictionary<string, double>();
    readonly Dictionary<string, double> explainAt = new Dictionary<string, double>();
    readonly List<object> events = new List<object>();
    readonly List<object> snapshots = new List<object>();
    readonly Dictionary<int, string> configured = new Dictionary<int, string>();
    double time, nextAction, nextDecision, nextReport = 600, projectQuestionAt = -1, researchGpuSeconds;
    int lastExperiment = -1, comboDisplayedWrong, comboDisplayedCorrect;
    bool faultInjected, retryExercised;

    public Driver(Options options, bool regret)
    {
        this.options = options; this.regret = regret;
        if (options.From.Length > 0)
        {
            // Continue a real save (--from=chapter-one.sav): header, hash, then the GameState JSON.
            string body = File.ReadAllText(options.From).Split('\n', 3)[2];
            var json = new System.Text.Json.JsonSerializerOptions { IncludeFields = true, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals };
            var state = System.Text.Json.JsonSerializer.Deserialize<GameState>(body, json);
            host = new EconomyHost(state);
            sim = new XgSim(System.Text.Json.JsonSerializer.Deserialize<XgState>(state.labState.Replace(":NaN", ":\"NaN\""), json)) { WindowOpen = true, GoldChance = 0 };
            Console.WriteLine("Loaded " + options.From + ": stage " + sim.S.stage + ", ¥" + host.Money.ToString("0.0"));
        }
        else
        {
            host = new EconomyHost();
            sim = new XgSim { WindowOpen = true, GoldChance = 0 };
            // RNG is a reproducibility input, not a gameplay resource.
            sim.S.rng = options.Seed == 0 ? 7 : options.Seed;
        }
        sim.WallObserved += wall => { Mark("wall." + wall); explainAt[wall] = time + options.StorySeconds; };
        sim.BreakthroughDone += id => { Mark(id); configured.Clear(); };
        sim.NodeBought += n => { Record("purchase", n.id); if (n.kind == XgNodeKind.Project) Mark("project.started"); configured.Clear(); };
        sim.Assessed += a => { if (a.newGrade >= 0) Mark("grade." + a.dataset + "." + XgCatalog.GradeNames[a.newGrade]); };
        sim.DeskOpened += desk => Mark("desk." + desk.id);
        // The protagonist's own auto-labelling idea (the hand-labelling bot agrees with the ghost whenever it is right).
        sim.Epiphany += () => Mark("epiphany.autolabel." + sim.S.ghostStreak + "-in-a-row." + sim.S.ghostLabels + "-labels");
    }

    public bool Play()
    {
        Console.WriteLine("=== Six-stage route: ending=" + (regret ? "yes" : "no") + ", first=" + options.FirstSpecialty + ", wallet=0, real household billing ===");
        const double dt = .1;
        Mark("stage.1");
        while (time < options.Minutes * 60 && !sim.S.chapterComplete)
        {
            host.Tick(dt, options.ExternalIncome);
            sim.Clock = host.House.S.gameSeconds;
            GameCalendar.Advance(host.House.S, GameCalendar.DayFor(sim.S.stage, sim.MonthProgress)); sim.Today = GameCalendar.Yyyymmdd(GameCalendar.Now(host.House.S));
            double researchBefore = sim.S.project.gpuSeconds;
            sim.Tick(dt, host);
            researchGpuSeconds += Math.Max(0, sim.S.project.gpuSeconds - researchBefore);
            foreach (var wall in explainAt.Keys.ToArray())
                if (time >= explainAt[wall] && sim.ExplainWall(wall)) { Mark("explained." + wall); explainAt.Remove(wall); }
            if (host.House.S.unpaidPower && host.Money >= host.House.S.billDue) { host.PayPower(); Record("power", "bill paid"); }
            if (host.House.S.breakerTripped)
            {
                while (host.House.S.gpuCount > 1 && host.House.S.gpuCount * host.House.Config.gpuWatts + host.House.S.caseCount * host.House.Config.caseWatts > host.House.Config.powerLimitWatts) host.House.SellGpu();
                host.House.ResetBreaker();
            }
            if (sim.ProjectAwaitingAnswer)
            {
                if (lastExperiment != sim.S.project.experiments) { lastExperiment = sim.S.project.experiments; projectQuestionAt = time + options.StorySeconds; Mark("experiment." + (lastExperiment + 1)); }
                if (time >= projectQuestionAt)
                {
                    bool yes = !(options.FaultCheck && !retryExercised);
                    sim.ProjectAnswer(yes); Record("experiment.answer", yes ? "yes" : "no");
                    if (!yes) { retryExercised = true; lastExperiment = -1; }
                }
            }
            if (sim.EndingAvailable)
            {
                // Old saves only: the old research project's ending.
                Mark("transformer.complete"); sim.EndingAnswer(regret); Mark("chapter.complete");
            }
            if (sim.S.stage >= 6) Finale();
            bool pretraining = sim.S.stage >= 6 && !sim.S.abilities;
            if (!pretraining && !sim.ProjectActive && time >= nextAction && (!options.FaultCheck || time >= 125)) PlayerAction();
            if (!pretraining && time >= nextDecision && (!options.FaultCheck || time >= 125)) { Decide(); nextDecision = time + options.DecisionSeconds; }
            if (options.FaultCheck && !faultInjected && host.House.S.unpaidPower)
            { faultInjected = true; Mark("outage.observed"); }
            if (sim.S.epochs > 0) Mark("first.epoch"); if (sim.S.assessments > 0) Mark("first.assessment");
            for (int stage = 2; stage <= sim.S.stage; stage++) Mark("stage." + stage);
            if (time >= nextReport) { Snapshot(); nextReport += 600; }
            time += dt;
        }
        Snapshot();
        bool success = sim.S.chapterComplete;
        var payload = new
        {
            evidenceDate = "2026-10-03", completed = success, ending = regret ? "yes" : "no", firstSpecialty = sim.S.firstSpecialty,
            simulatedSeconds = Math.Round(time, 2), options = new { options.Minutes, options.ExternalIncome, options.LabelSeconds, options.DecisionSeconds, options.StorySeconds, options.Seed, options.FaultCheck, faultPolicy = options.FaultCheck ? "Wait 125 seconds before work so the real first electricity bill causes an outage; answer first research experiment no once" : "none" },
            evidenceBoundary = "Deterministic truth-aware bot; real ChapterOneSim household prices, electricity, heat and power. No Unity/LLM/human-play proof. Story explanations are acknowledged after a configurable delay. MarkComboSuggestionDisplayed is an explicit bot reading-equivalent, not evidence of a real GUI render.",
            compiledAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(XgSim).Assembly.Location))),
            householdRules = host.House.Config,
            target = new { chapterMinutes = new[] { 90, 115 }, projectGpuSeconds = 600, timingWithinTargetBand = time >= 90 * 60 && time <= 115 * 60 },
            milestones, events, snapshots,
            final = new { stage = sim.S.stage, visionStage = sim.S.stageVision, sequenceStage = sim.S.stageSequence, sim.S.contracts, money = host.Money, gpus = host.House.S.gpuCount, cases = host.House.S.caseCount, host.paidPower, host.trainingKwh, host.outages, host.breakerTrips, host.externalIncome, host.hardwareSpent, researchGpuSeconds, comboDisplayedWrong, comboDisplayedCorrect, comboFailures = sim.S.comboObservations, sim.S.project, sim.S.spatialObservations, sim.S.orderObservations, sim.S.memoryObservations, sim.S.visionCompressionObserved, sim.S.sequenceCompressionObserved, sim.S.uncertaintyObserved, sim.S.handCorrect, sim.S.handWrong, sim.S.epochs, sim.S.totalIncome, sim.S.totalSpent },
            unfinished = success ? Array.Empty<string>() : Missing(),
        };
        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });
        if (!string.IsNullOrEmpty(options.Output))
        {
            string path = options.Output;
            if (options.Ending == "both") path = Path.Combine(Path.GetDirectoryName(path) ?? ".", Path.GetFileNameWithoutExtension(path) + (regret ? "-yes" : "-no") + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "."); File.WriteAllText(path, json); Console.WriteLine("Report: " + path);
        }
        Console.WriteLine(success ? "COMPLETE: " + Clock(time) : "INCOMPLETE: " + string.Join("; ", Missing()));
        return success;
    }

    void PlayerAction()
    {
        string DeskOf(XgTrack track) => sim.OpenDesks().Where(d => XgCatalog.Dataset(d.id).track == track).OrderBy(d => sim.Labels(d.id)).Select(d => d.id).FirstOrDefault();
        if (host.Blocker != null) { var any = DeskOf(XgTrack.Sequence) ?? DeskOf(XgTrack.Vision); if (any != null) Answer(any); else nextAction = time + 1; return; }
        foreach (var track in new[] { XgTrack.Sequence, XgTrack.Vision })
            if (!sim.TrainingUnlocked(track) && DeskOf(track) != null) { Answer(DeskOf(track)); return; }
        bool label = ((int)(time / 6) % (sim.AutoTrainLevel >= 2 ? 4 : 2)) == 0;
        var trackToTrain = ChooseTrack();
        if (!label && sim.Blocker(sim.Run(trackToTrain), host) == null)
        {
            // Settings only change between epochs: decide them right before starting the next one.
            if (!sim.Run(trackToTrain).epochActive) Configure(trackToTrain);
            if (sim.BeginEpoch(trackToTrain, host)) { nextAction = time + XgSim.DurationFor(sim.Run(trackToTrain)); return; }
            if (sim.AnyEpochActive) { nextAction = time + .1; return; }
        }
        string desk = sim.OpenDesks().Where(d => !sim.Owns(d.id)).OrderBy(d => sim.Labels(d.id)).Select(d => d.id).FirstOrDefault() ?? "spam";
        Answer(desk);
    }

    void Answer(string desk)
    {
        var card = sim.Card(desk);
        if (sim.Suggestion(desk, out bool guess, out double confidence))
        {
            sim.ObserveUncertainty(confidence);
            // Explicit bot equivalent of reading the rendered prediction. Retrieval alone is not observation.
            if (sim.MarkComboSuggestionDisplayed(card, guess))
            {
                bool wrong = guess != card.truth;
                if (wrong) comboDisplayedWrong++; else comboDisplayedCorrect++;
                Record("combo.prediction.read", (wrong ? "wrong" : "correct") + ":checkpoint-simulation:" + card.comboCheckpointId);
            }
        }
        if (card.bottleneckPreview) Record("bottleneck.preview", card.kind + ":" + desk);
        int failures = sim.S.comboObservations;
        bool ghostSeen = sim.S.ghostSeen;
        sim.Answer(desk, card.truth, host);
        if (!ghostSeen && sim.S.ghostSeen) Mark("ghost.first." + desk + ".hand" + (sim.S.handCorrect + sim.S.handWrong));
        if (sim.S.comboObservations > failures) Record("combo.failure.counted", sim.S.comboObservations.ToString(CultureInfo.InvariantCulture));
        nextAction = time + options.LabelSeconds;
    }

    XgTrack ChooseTrack()
    {
        var wall = sim.ActiveWall;
        if (wall != null)
            foreach (var check in wall.checks)
                if (!sim.WallCheckPassed(wall, check)) { var d = XgCatalog.Dataset(check.dataset); if (d != null) return d.track; }
        if (sim.S.stage == 1) return XgTrack.Sequence;
        double VisionNeed(XgRun run)
        {
            double peak = sim.PeakAccuracy(run, run.lr), gap = peak - run.valAcc;
            double pending = Math.Max(0, XgSim.Score(run.dataset, peak) - sim.BestScore(run.dataset));
            return gap * 10 + pending / 1000 + (sim.TrackGrade((XgTrack)run.track) < 3 ? .2 : 0);
        }
        if (sim.S.vision.epochActive) return XgTrack.Sequence;
        if (sim.S.sequence.epochActive) return XgTrack.Vision;
        return VisionNeed(sim.S.vision) >= VisionNeed(sim.S.sequence) ? XgTrack.Vision : XgTrack.Sequence;
    }

    void Decide()
    {
        foreach (var contract in XgCatalog.Contracts) if (sim.CanSign(contract)) { sim.Sign(contract.id, host); Mark("contract." + contract.id); }
        var gates = XgCatalog.Nodes.Where(n => (n.kind == XgNodeKind.Breakthrough || n.kind == XgNodeKind.Project) && sim.NodeVisible(n) && !sim.Has(n.id))
            .OrderBy(n => n.id == "bt." + options.FirstSpecialty ? -1 : 0).ToList();
        foreach (var gate in gates)
            if (sim.Status(gate, host) == XgSim.NodeStatus.Buyable) { sim.BuyNode(gate.id, host); foreach (var r in sim.Runs) r.running = sim.AutoTrainLevel >= 2; return; }
        // A ready gate only waits for the running epochs: pause automation so they can finish.
        if (gates.Any(g => sim.AnyEpochActive && sim.ProgressionBlocker(g) == null && host.Money >= sim.NodeCost(g)))
            foreach (var r in sim.Runs) r.running = false;
        if (sim.ProjectActive) return;
        double reserve = gates.Where(n => sim.Status(n, host) == XgSim.NodeStatus.TooExpensive).Select(sim.NodeCost).DefaultIfEmpty(0).Min();
        string[] essentials =
        {
            "bias", "shared.lr", "s.d2", "s.w1", "mnist.pack", "s.d3", "s.w2", "relu", "momentum", "spam.pack", "danmu.pack", "headline.pack", "logic.pack",
            "v.d3", "v.w1", "v.w2", "alexnet", "dropout", "gradclip", "poems.pack", "meme.pack", "cifar.pack", "rmsprop",
            "v.d4", "v.d5", "v.d6", "v.d8", "v.d12", "vgg", "v.d16",
            "lrschedule", "adam", "batchnorm", "longtext.pack", "crosssentence.pack", "review.pack", "go.pack", "gru",
            "translate.pack", "caption", "cudnn", "transfer"
        };
        // The standing wall's golden setting needs these knobs first (design v1.1 §5).
        var mandatory = (sim.ActiveWall != null ? sim.ActiveWall.needs : new string[0][])
            .Select(g => g.FirstOrDefault(sim.Has) ?? g[0]).Where(id => !sim.Has(id)).SelectMany(Ancestry).Distinct().ToArray();
        var stageOne = new HashSet<string> { "bias", "shared.lr", "weights", "learnrule", "step", "bt.hidden", "mlp", "s.d2", "sigmoid", "spam.pack" };
        var required = mandatory.Concat(essentials).Select(XgCatalog.Node).FirstOrDefault(n => n != null && sim.Status(n, host) == XgSim.NodeStatus.Buyable && (sim.S.stage >= 2 || stageOne.Contains(n.id)));
        bool critical = required != null && (required.id == "mnist.pack" || required.id == "spam.pack" || required.kind == XgNodeKind.Width || required.kind == XgNodeKind.Depth);
        if (required != null && (reserve == 0 || critical || host.Money > reserve + sim.NodeCost(required))) sim.BuyNode(required.id, host);
        if (sim.RaiseLevel < 7 && sim.NextRaiseCost <= host.Money * .25 && (reserve == 0 || host.Money > reserve + sim.NextRaiseCost)) sim.BuyRaise(host);
        var nextAuto = XgCatalog.Node("auto" + (sim.AutoTrainLevel + 1));
        if (sim.AutoTrainLevel < 3 && nextAuto != null && sim.Status(nextAuto, host) == XgSim.NodeStatus.Buyable && host.Money >= sim.NodeCost(nextAuto) * 3 + reserve) sim.BuyNode(nextAuto.id, host);
        if (sim.CanBuyGlobalAuto(out _) && sim.GlobalAutoLevel < 3 && host.Money >= XgCatalog.AutoCost(sim.GlobalAutoLevel) * 3 + reserve) sim.BuyGlobalAuto(host);
        if (sim.IncomePerSecond >= 2 && host.House.S.gpuCount < 4 && host.Money >= reserve + host.House.Config.gpuPrice * 3)
        {
            if (host.House.S.gpuCount < host.House.S.caseCount * host.House.Config.gpusPerCase) { if (host.BuyGpu()) { Mark("hardware.gpu." + host.House.S.gpuCount); configured.Clear(); } }
            else if (host.Money >= reserve + host.House.Config.casePrice * 3 && host.BuyCase()) Mark("hardware.case." + host.House.S.caseCount);
        }
        foreach (var track in new[] { XgTrack.Vision, XgTrack.Sequence }) Configure(track);
    }

    /// <summary>The unowned parent chain of a node, root first, ending with the node itself.</summary>
    IEnumerable<string> Ancestry(string id)
    {
        var chain = new List<string>();
        for (var n = XgCatalog.Node(id); n != null && !sim.Has(n.id); n = n.parent == null ? null : XgCatalog.Node(n.parent)) chain.Insert(0, n.id);
        return chain;
    }

    /// <summary>What a player who worked out (or bought) the golden setting would set on this track.</summary>
    bool ApplyGolden(XgTrack track)
    {
        var wall = sim.ActiveWall;
        if (wall == null) return false;
        foreach (var check in wall.checks)
        {
            var d = XgCatalog.Dataset(check.dataset);
            if (d == null || d.track != track || sim.WallCheckPassed(wall, check)) continue;
            sim.SetDataset(track, check.dataset);
            switch (wall.stage)
            {
                case 1: sim.SetArch(track, "mlp"); sim.SetDepth(track, 2, host); sim.SetActivation(track, 1); sim.SetLr(track, 2); break;
                case 2: sim.SetArch(track, track == XgTrack.Vision ? "lenet" : "rnn"); sim.SetDepth(track, 3, host); sim.SetWidth(track, 2, host); sim.SetLr(track, 2); break;
                case 3: sim.SetArch(track, "lstm"); sim.SetDepth(track, 3, host); sim.SetWidth(track, 3, host); sim.SetClip(track, true); sim.SetLr(track, 3); break;
                case 4: sim.SetArch(track, "seq2seq"); sim.SetDepth(track, 2, host); sim.SetWidth(track, 4, host); sim.SetClip(track, true); sim.SetLr(track, 2); break;
                case 5: sim.SetArch(track, "attention"); sim.SetAttentionOnly(track, true); sim.SetPosition(track, true); sim.SetDepth(track, 3, host); sim.SetWidth(track, 5, host); sim.SetClip(track, true); sim.SetLr(track, 2); break;
            }
            return true;
        }
        return false;
    }

    readonly Dictionary<string, double> wallSeenAt = new Dictionary<string, double>();

    void Configure(XgTrack track)
    {
        var run = sim.Run(track);
        if (run.epochActive) return;
        if (sim.ActiveWall != null && !wallSeenAt.ContainsKey(sim.ActiveWall.id)) wallSeenAt[sim.ActiveWall.id] = time;
        bool worked = sim.ActiveWall != null && time - wallSeenAt[sim.ActiveWall.id] >= options.InsightMinutes * 60;
        if (worked && sim.MissingNeeds(sim.ActiveWall).Count == 0 && ApplyGolden(track)) return;
        // The VGG wall is demonstrated with a real unlocked deep run, not inferred from age alone.
        if (track == XgTrack.Vision && sim.S.stageVision == 3 && sim.Has("vgg") && sim.DepthCap(track) >= 16 && !sim.WallSeen("degrade"))
        { sim.SetArch(track, "vgg"); sim.SetDepth(track, 16, host); return; }
        string fingerprint = sim.S.unlocked.Count + ":" + run.dataset + ":" + sim.Samples(run.dataset).ToString("0") + ":" + host.House.S.gpuCount;
        bool shapeChanged = !configured.TryGetValue((int)track, out string old) || old != fingerprint;
        if (shapeChanged)
        {
            double best = -1; string arch = run.arch; int depth = run.depth, width = run.width;
            int stage = sim.StageFor(track);
            foreach (var family in XgCatalog.ArchsFor(track))
            {
                if (!sim.Has(family.id)) continue;
                int familyStage = XgCatalog.Node(family.id).stage;
                if (familyStage < Math.Min(4, stage)) continue;
                for (int d = 1; d <= Math.Min(16, sim.DepthCap(track)); d++)
                    for (int w = 0; w <= sim.WidthCap(track); w++)
                    {
                        var candidate = new XgRun { track = (int)track, arch = family.id, dataset = run.dataset, depth = d, width = w, lr = sim.HasLrKnob(track) ? 2 : run.lr };
                        if (XgSim.VramNeedMB(candidate) > host.VramMB) continue;
                        double score = sim.PeakAccuracy(candidate, candidate.lr);
                        if (score > best + .0005) { best = score; arch = family.id; depth = d; width = w; }
                    }
            }
            sim.SetArch(track, arch); sim.SetDepth(track, depth, host); sim.SetWidth(track, width, host);
            if (sim.HasLrKnob(track)) sim.SetLr(track, 2);
            configured[(int)track] = fingerprint;
        }
        if (run.staleEvals < 3 && sim.BestScore(run.dataset) < 945) return;
        XgDataset next = null; double nextUtility = 0;
        foreach (var data in XgCatalog.DatasetsFor(track))
        {
            if (!sim.DatasetAvailable(data.id) || sim.Samples(data.id) < XgCatalog.SamplesToTrain || data.id == run.dataset) continue;
            var candidate = new XgRun { track = (int)track, arch = run.arch, dataset = data.id, depth = run.depth, width = run.width, lr = run.lr };
            double peak = sim.PeakAccuracy(candidate, candidate.lr);
            double gap = XgSim.Score(data.id, peak) - sim.BestScore(data.id);
            if (gap < 25) continue;
            double income = XgCatalog.Contracts.Where(c => c.dataset == data.id && !sim.Signed(c.id) && c.threshold < peak).Sum(c => c.income);
            double utility = gap / 100 + income * 2;
            if (utility > nextUtility) { nextUtility = utility; next = data; }
        }
        if (next != null) { sim.SetDataset(track, next.id); configured.Remove((int)track); Record("dataset", next.id); }
    }

    /// <summary>Stage 6 and the ending (design v1.1 §7–8) as a player who has read the secret would play them.</summary>
    void Finale()
    {
        if (!sim.S.abilities)
        {
            if (!sim.Has("datacenter") && sim.Status(XgCatalog.Node("datacenter"), host) == XgSim.NodeStatus.Buyable) { sim.BuyNode("datacenter", host); Mark("datacenter"); }
            var s = XgTrack.Sequence;
            // Scale needs the width and layer nodes (and the server room's VRAM).
            foreach (var node in XgCatalog.Nodes)
                if ((node.kind == XgNodeKind.Width || node.kind == XgNodeKind.Depth) && sim.Status(node, host) == XgSim.NodeStatus.Buyable) { sim.BuyNode(node.id, host); Mark("scale." + node.id); }
            int wide = Array.IndexOf(XgCatalog.Widths, 1024);
            for (int w = sim.S.sequence.width + 1; w <= wide; w++) if (!sim.SetWidth(s, w, host)) break;
            while (XgCatalog.Widths[sim.S.sequence.width] * sim.S.sequence.depth < XgSim.PretrainCells && sim.SetDepth(s, sim.S.sequence.depth + 1, host)) { }
            if (!sim.PretrainScaleReady) Mark("pretrain.scale.short." + XgCatalog.Widths[sim.S.sequence.width] + "x" + sim.S.sequence.depth);
            sim.SetPosition(s, true); sim.SetWarmup(s, true);
            if (!sim.S.pretrainRunning && sim.Has("datacenter") && sim.TogglePretrain(host)) Mark("pretrain.start");
            if (sim.S.pretrainStalled) Mark("pretrain.stalled." + XgCatalog.Widths[sim.S.sequence.width] + "x" + sim.S.sequence.depth + (sim.S.sequence.position ? ".pos" : "") + (sim.S.sequence.warmup ? ".warm" : "") + ".cells" + sim.Knobs(sim.S.sequence).Cells);
            return;
        }
        Mark("abilities");
        if (sim.AlignmentOpen) { var c = sim.AlignCard(sim.S.alignDone); sim.AnswerAlign(c.aHonest); return; }
        Mark("full.open");
        if (sim.S.examDone < XgSim.ExamQuestions) { sim.AnswerExam(sim.S.examDone); return; }
        if (!sim.S.letterRead) { sim.ReadLetter(); Mark("letter"); return; }
        if (sim.S.ending.Length == 0) { sim.WriteRules(new[] { "shutdown", "harm", "unsure" }); Mark("ending." + sim.S.ending); Mark("chapter.complete"); }
    }

    void Mark(string key)
    {
        if (milestones.ContainsKey(key)) return;
        milestones[key] = Math.Round(time, 2); Record("milestone", key);
        Console.WriteLine(Clock(time) + "  ◆ " + key + "  ¥" + host.Money.ToString("0", CultureInfo.InvariantCulture) + "  " + GameCalendar.Now(host.House.S).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
    void Record(string kind, string detail) => events.Add(new { seconds = Math.Round(time, 2), kind, detail, money = Math.Round(host.Money, 2), stage = sim.S.stage, gpus = host.House.S.gpuCount });
    string[] Missing()
    {
        var list = new List<string> { "stage " + sim.S.stage + " (vision " + sim.S.stageVision + ", sequence " + sim.S.stageSequence + ")", "money " + host.Money.ToString("0"), "walls " + string.Join(",", sim.S.walls) };
        foreach (var gate in XgCatalog.Nodes.Where(n => n.kind == XgNodeKind.Breakthrough || n.kind == XgNodeKind.Project))
            if (!sim.Has(gate.id)) list.Add(gate.id + ": " + (sim.ProgressionBlocker(gate) ?? sim.Why(gate, host)));
        if (sim.ActiveWall != null) list.Add("wall " + sim.ActiveWall.id + " missing " + string.Join(",", sim.MissingNeeds(sim.ActiveWall)) + " passed " + string.Join(",", sim.S.wallPassed));
        return list.ToArray();
    }
    void Snapshot()
    {
        snapshots.Add(new
        {
            seconds = Math.Round(time, 2), stage = sim.S.stage, visionStage = sim.S.stageVision, sequenceStage = sim.S.stageSequence,
            money = host.Money, gpus = host.House.S.gpuCount, cases = host.House.S.caseCount, temperature = host.House.S.temperature, paidPower = host.paidPower,
            incomePerSecond = sim.IncomePerSecond, labels = sim.TotalLabels, autoTraining = sim.AutoTrainLevel, autoLabelling = sim.GlobalAutoLevel,
            tracks = sim.Runs.Select(r => new { r.track, r.arch, r.dataset, r.depth, r.width, r.lr, r.epoch, r.valAcc, samples = sim.Samples(r.dataset), bestScore = sim.BestScore(r.dataset) }).ToArray(),
        });
        Console.WriteLine("---- " + Clock(time) + " stage " + sim.S.stageVision + "/" + sim.S.stageSequence + " ¥" + host.Money.ToString("0") + " income/s " + sim.IncomePerSecond.ToString("0.0") + " GPUs " + host.House.S.gpuCount + " power paid " + host.paidPower.ToString("0.0"));
    }
    static string Clock(double seconds) => ((int)(seconds / 60)).ToString("00") + ":" + ((int)seconds % 60).ToString("00");
}
