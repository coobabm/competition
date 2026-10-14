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
sealed class EconomyHost : IXgHost, IXgRig
{
    // 接线: the household's real cards, PSU and breaker (the lab auto-wires them exactly as before).
    public IReadOnlyList<XgCardSpec> Cards => House.Cards.Select(id => LingGuangV05.Core.Hardware.HardwareCatalog.Gpu(id)).Where(g => g != null)
        .Select(g => new XgCardSpec { id = g.id, name = g.name, nameEn = g.nameEn, compute = g.compute, vramMB = g.vramMB, watts = g.watts }).ToList();
    public double PowerLimitWatts => House.Config.powerLimitWatts;
    public double HouseWatts => House.HouseWatts;
    public bool BreakerTripped => House.S.breakerTripped;
    public void TripBreaker() => House.TripBreakerNow();
    public bool ResetBreaker() => House.ResetBreaker();
    public double UnpluggedWatts { get => House.UnpluggedWatts; set => House.UnpluggedWatts = value; }
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
    /// <summary>The wallet as the bot's decisions see it: what it may spend (the rent reserve held back while deciding).</summary>
    public double Money => House.S.money - Reserve;
    public string Blocker => House.S.landlordCut ? "Landlord cut the power" : House.S.unpaidPower ? "Unpaid power" : House.S.breakerTripped ? "Breaker tripped" : House.PowerWatts <= 0 ? "No power" : null;
    /// <summary>Money the bot keeps back for rent while it decides purchases (0 outside its decisions: the lab's own running costs are not held back).</summary>
    public double Reserve;
    public bool Spend(double amount)
    {
        if (!double.IsFinite(amount) || amount < 0 || Money + 1e-9 < amount) return false;
        House.S.money -= amount; return true;
    }
    public void Earn(double amount) { if (double.IsFinite(amount) && amount > 0) { House.S.money += amount; House.S.totalEarned += amount; } }
    /// <summary>Each round's electricity is billed at once at the month's tier (ChapterOneSim.Bills.cs), as in the game.</summary>
    public void Train(double seconds)
    {
        trainingKwh += House.TrainingKwh(seconds);
        trainingPower += House.BillTraining(seconds);
    }
    public double trainingPower;
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
    /// <summary>淘货: a new card on today's calendar.</summary>
    public bool BuyCard(string id, DateTime today)
    {
        double before = Money; bool bought = House.BuyCard(id, today); if (bought) hardwareSpent += before - Money; return bought;
    }
    /// <summary>喵鱼: sell a card to the first haggler (85% of today's value).</summary>
    public bool SellCard(int index, DateTime today)
    {
        var g = LingGuangV05.Core.Hardware.HardwareCatalog.Gpu(House.Cards[index]);
        double price = Math.Floor(LingGuangV05.Core.Hardware.HardwareCatalog.UsedPrice(g, today) * .85);
        bool sold = House.SellCard(index, price, today); if (sold) hardwareRefund += price; return sold;
    }
    public bool BuyCafeBox(DateTime today) { double before = Money; bool bought = House.BuyCafeBox(today); if (bought) hardwareSpent += before - Money; return bought; }
    public double hardwareRefund;
    public bool BuyCase() { bool bought = House.BuyCase(); if (bought) hardwareSpent += House.Config.casePrice; return bought; }
}

sealed class Options
{
    public double Minutes = 90, ExternalIncome, LabelSeconds = 1.2, DecisionSeconds = 2, StorySeconds = 4;
    public int Seed = 7;
    public string Ending = "no", FirstSpecialty = "vision", Output = "", From = "";
    public bool FaultCheck;
    /// <summary>--careless: trains non-stop and never labels by hand (only the first samples training needs). It must go bankrupt.</summary>
    public bool Careless;
    /// <summary>Days of rent a normal player keeps in the wallet when buying (--reserve-days=N).</summary>
    public double ReserveDays = 5;
    /// <summary>--guide-check: read the to-do note (XgGuide) every simulated second; any empty note, empty line or step on a closed page fails the run.</summary>
    public bool GuideCheck;
    /// <summary>--spider=N: the resident spider reads N new words a minute from the first epoch on (each is a sample, XgSim.ResidentRead). 0 = the old bot, which never modelled it.</summary>
    public double SpiderWordsPerMinute;
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
                case "--output": o.Output = Path.GetFullPath(value); break;
                case "--label-seconds": o.LabelSeconds = Number(value); break;
                case "--decision-seconds": o.DecisionSeconds = Number(value); break;
                case "--story-seconds": o.StorySeconds = Number(value); break;
                case "--seed": o.Seed = int.Parse(value, CultureInfo.InvariantCulture); break;
                case "--fault-check": o.FaultCheck = true; break;
                case "--guide-check": o.GuideCheck = true; break;
                case "--careless": o.Careless = true; break;
                case "--spider": o.SpiderWordsPerMinute = Number(value); break;
                case "--reserve-days": o.ReserveDays = Number(value); break;
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
    readonly List<object> events = new List<object>();
    readonly List<object> snapshots = new List<object>();
    readonly Dictionary<int, string> configured = new Dictionary<int, string>();
    /// <summary>Seconds between progress lines (BOT_REPORT_SECONDS, default 600).</summary>
    static readonly double ReportEvery = double.TryParse(Environment.GetEnvironmentVariable("BOT_REPORT_SECONDS"), NumberStyles.Float, CultureInfo.InvariantCulture, out double r) && r >= 10 ? r : 600;
    double spiderCarry;
    int spiderWords;
    double time, nextAction, nextDecision, nextReport = ReportEvery, projectQuestionAt = -1, researchGpuSeconds;
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
            // A new game opens as in the desktop (XingGuangController): only 常识判断 and training until its yes/no is learned.
            sim.BeginOpening();
        }
        // The rent holiday, as in the game (XingGuangController): nothing is charged before ability 3.
        host.House.RentWaived = () => sim.AbilitiesCount < XgSim.RentFromAbility;
        // 实验室的旧服务器钥匙 halves the rent, as in the game.
        host.House.RentFactor = () => sim.RelicRentFactor;
        sim.YesNoLearned += () => Mark("opening.learned");
        sim.AbilityEmerged += ability => Mark("ability." + ability + "." + XgSim.AbilitiesEn[ability - 1].Replace(' ', '_') + ".P" + XgSim.ParamsText(sim.TrainedParamsK) + ".D" + XgSim.SamplesText(sim.TrainedSamples).Replace(",", ""));
        sim.BreakthroughDone += id => { Mark(id); configured.Clear(); };
        sim.NodeBought += n => { Record("purchase", n.id); if (n.kind == XgNodeKind.Project) Mark("project.started"); configured.Clear(); };
        sim.Assessed += a => { if (a.newGrade >= 0) Mark("grade." + a.dataset + "." + XgCatalog.GradeNames[a.newGrade]); };
        sim.DeskOpened += desk => Mark("desk." + desk.id);
        // Rent and bankruptcy (ChapterOneSim.Bills.cs): every landlord step and broken card is a dated event.
        host.House.Signal += (signal, arg) =>
        {
            if (signal != "rent.due" && signal != "power.cut" && signal != "bankrupt" && signal != "rent.settled" && signal != "rent.raised" && signal != "gpu.broken" && signal != "gpu.repaired") return;
            economyEvents++;
            Record("economy", signal + (arg != null ? " " + arg : ""));
            Console.WriteLine(Clock(time) + "  $ " + signal + (arg != null ? " " + arg : "") + "  ¥" + host.Money.ToString("0", CultureInfo.InvariantCulture) + "  " + GameCalendar.Now(host.House.S).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        };
        // The protagonist's own auto-labelling idea (the hand-labelling bot agrees with the ghost whenever it is right).
        sim.Epiphany += () => Mark("epiphany.autolabel." + sim.S.ghostStreak + "-in-a-row." + sim.S.ghostLabels + "-labels");
    }

    public bool Play()
    {
        Console.WriteLine("=== Six-stage route: ending=" + (regret ? "yes" : "no") + ", first=" + options.FirstSpecialty + ", wallet=¥" + host.Money.ToString("0") + ", rent and bills" + (options.Careless ? ", CARELESS (trains non-stop, never labels)" : ", keeps " + options.ReserveDays + " days of rent") + " ===");
        const double dt = .1;
        Mark("stage.1");
        while (time < options.Minutes * 60 && !sim.S.chapterComplete && !host.House.S.bankrupt)
        {
            host.Tick(dt, options.ExternalIncome);
            sim.Clock = host.House.S.gameSeconds;
            GameCalendar.Advance(host.House.S, GameCalendar.DayFor(sim.S.stage, sim.MonthProgress)); sim.Today = GameCalendar.Yyyymmdd(GameCalendar.Now(host.House.S));
            double researchBefore = sim.S.project.gpuSeconds;
            sim.Tick(dt, host);
            ManageRelics();
            if (options.SpiderWordsPerMinute > 0 && sim.S.epochs >= XgSim.SpiderIntroEpochs)
            {
                spiderCarry += options.SpiderWordsPerMinute * dt / 60;
                while (spiderCarry >= 1) { spiderCarry -= 1; sim.ResidentRead("word" + spiderWords++); }
            }
            researchGpuSeconds += Math.Max(0, sim.S.project.gpuSeconds - researchBefore);
            if (host.House.S.unpaidPower && host.Money >= host.House.S.billDue) { host.PayPower(); Record("power", "bill paid"); }
            if (host.House.S.breakerTripped)
            {
                while (host.House.S.gpuCount > 1 && host.House.LoadWatts > host.House.Config.powerLimitWatts && host.SellCard(host.House.WeakestCard(), GameCalendar.Now(host.House.S).Date)) { }
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
            // Both bars full: the player presses the chapter button (XgTrainPage.Side) to confirm the next ability.
            if (XgGuide.ChapterWaits(sim)) ConfirmStage();
            if (sim.S.stage >= 6) Finale();
            // Stage 6 keeps labelling and training too: the wallet has to stay above the rent while the items are bought.
            if (!sim.ProjectActive && time >= nextAction && (!options.FaultCheck || time >= 125)) PlayerAction();
            if (time >= nextDecision && (!options.FaultCheck || time >= 125)) { Decide(); nextDecision = time + options.DecisionSeconds; }
            if (options.FaultCheck && !faultInjected && host.House.S.unpaidPower)
            { faultInjected = true; Mark("outage.observed"); }
            if (sim.S.epochs > 0) Mark("first.epoch"); if (sim.S.assessments > 0) Mark("first.assessment");
            for (int stage = 2; stage <= sim.S.stage; stage++) Mark("stage." + stage);
            if (options.GuideCheck && time >= nextGuide) { CheckGuide(); nextGuide = time + 1; }
            if (time >= nextReport) { Snapshot(); nextReport += ReportEvery; if (Environment.GetEnvironmentVariable("BOT_DEBUG") != null) Debug(); }
            TrackMoney();
            time += dt;
        }
        Snapshot();
        if (host.House.S.bankrupt) Mark("bankrupt.sold.for." + host.House.S.soldFor.ToString("0", CultureInfo.InvariantCulture));
        // A careless run must end with the computer sold; a normal one must finish the chapter without it.
        bool success = options.Careless ? host.House.S.bankrupt : sim.S.chapterComplete && !host.House.S.bankrupt;
        ReportMoney();
        Console.WriteLine("---- relics: " + relicsPicked + " picked, " + sim.RelicsOwned.Count + " owned, equipped " + string.Join("+", Enumerable.Range(0, XgRelics.Slots).Select(i => sim.RelicInSlot(i)?.id ?? "-")));
        if (options.GuideCheck) success &= ReportGuide();
        var payload = new
        {
            evidenceDate = "2026-10-03", completed = success, ending = regret ? "yes" : "no", firstSpecialty = sim.S.firstSpecialty,
            simulatedSeconds = Math.Round(time, 2), options = new { options.Minutes, options.ExternalIncome, options.LabelSeconds, options.DecisionSeconds, options.StorySeconds, options.Seed, options.FaultCheck, faultPolicy = options.FaultCheck ? "Wait 125 seconds before work so the real first electricity bill causes an outage; answer first research experiment no once" : "none" },
            evidenceBoundary = "Deterministic truth-aware bot; real ChapterOneSim household prices, electricity, heat and power. No Unity/LLM/human-play proof. Story explanations are acknowledged after a configurable delay. MarkComboSuggestionDisplayed is an explicit bot reading-equivalent, not evidence of a real GUI render.",
            compiledAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(XgSim).Assembly.Location))),
            householdRules = host.House.Config,
            target = new { chapterMinutes = new[] { 90, 115 }, projectGpuSeconds = 600, timingWithinTargetBand = time >= 90 * 60 && time <= 115 * 60 },
            milestones, events, snapshots,
            economy = new { minMoney = Math.Round(minMoney, 2), minAt = Math.Round(minAt, 2), minDate, minStage, bankrupt = host.House.S.bankrupt, host.House.S.soldFor, bills = host.House.S.bills, trainingPower = host.trainingPower, moneyCurve },
            final = new { stage = sim.S.stage, visionStage = sim.S.stageVision, sequenceStage = sim.S.stageSequence, sim.S.contracts, money = host.Money, gpus = host.House.S.gpuCount, cases = host.House.S.caseCount, host.paidPower, host.trainingKwh, host.outages, host.breakerTrips, host.externalIncome, host.hardwareSpent, host.hardwareRefund, compute = host.House.CardCompute, cards = host.House.Cards.ToArray(), researchGpuSeconds, comboDisplayedWrong, comboDisplayedCorrect, comboFailures = sim.S.comboObservations, sim.S.project, sim.S.spatialObservations, sim.S.orderObservations, sim.S.memoryObservations, sim.S.visionCompressionObserved, sim.S.sequenceCompressionObserved, sim.S.uncertaintyObserved, sim.S.handCorrect, sim.S.handWrong, sim.S.epochs, sim.S.drops, sim.S.totalIncome, sim.S.totalSpent },
            unfinished = success ? Array.Empty<string>() : Missing(),
        };
        string json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });
        if (!string.IsNullOrEmpty(options.Output))
        {
            string path = options.Output;
            if (options.Ending == "both") path = Path.Combine(Path.GetDirectoryName(path) ?? ".", Path.GetFileNameWithoutExtension(path) + (regret ? "-yes" : "-no") + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "."); File.WriteAllText(path, json); Console.WriteLine("Report: " + path);
        }
        // The rent holiday: before ability 3 nothing is charged, so a careless run stuck there cannot go bankrupt.
        if (options.Careless && !success && sim.AbilitiesCount < XgSim.RentFromAbility) Console.WriteLine("NOT BANKRUPT AS EXPECTED (rent holiday until ability " + XgSim.RentFromAbility + "): " + string.Join("; ", Missing()));
        else if (options.Careless) Console.WriteLine(success ? "BANKRUPT AS EXPECTED: " + Clock(time) : "NOT BANKRUPT (careless run must go bankrupt): " + string.Join("; ", Missing()));
        else Console.WriteLine(success ? "COMPLETE: " + Clock(time) : host.House.S.bankrupt ? "BANKRUPT at " + Clock(time) : "INCOMPLETE: " + string.Join("; ", Missing()));
        return success;
    }

    // ───────────── rare items ─────────────

    /// <summary>What the bot values in a relic for this route, best first: the ability lines, scale, then training, money and data.</summary>
    static readonly string[] RelicPriority =
    {
        "advisor_page", "taihulight", "tieba", "attention_scratch", "angel_term", "imagenet_disc", "data_broker", "campus_vpn", "gold_cert",
        "dropout_paper", "server_key", "coupon", "zhou_usb", "redbull", "batchnorm_paper", "resnet_sticker", "mining_1080", "spider_ua",
        "multimeter", "arcade_tokens", "watermelon_book", "allnight", "tablet", "alphago_book",
    };
    int relicsPicked;
    string relicOwnedKey = "";

    /// <summary>As a player does: take the first of the three cards, and keep the four best relics equipped (swapping is free).</summary>
    void ManageRelics()
    {
        while (sim.RelicPickPending)
        {
            string id = sim.RelicPick[0];
            if (!sim.PickRelic(id)) break;
            relicsPicked++; Record("relic.pick", id);
        }
        string key = string.Join(",", sim.RelicsOwned);
        if (key == relicOwnedKey) return;
        relicOwnedKey = key;
        var best = RelicPriority.Where(sim.RelicOwned).Take(XgRelics.Slots).ToList();
        if (best.All(sim.RelicEquipped) && sim.RelicsEquippedCount == best.Count) return;
        for (int i = 0; i < XgRelics.Slots; i++) sim.UnequipSlot(i);
        foreach (var id in best) sim.EquipRelic(id);
        Record("relic.equip", string.Join("+", best));
    }

    void PlayerAction()
    {
        string DeskOf(XgTrack track) => sim.OpenDesks().Where(d => XgCatalog.Dataset(d.id).track == track).OrderBy(d => sim.Labels(d.id)).Select(d => d.id).FirstOrDefault();
        if (options.Careless)
        {
            // Only the first samples training needs, then rounds back to back on every line that can train.
            if (!sim.TrainingUnlocked(XgTrack.Sequence) && !sim.TrainingUnlocked(XgTrack.Vision)) { var first = DeskOf(XgTrack.Sequence) ?? DeskOf(XgTrack.Vision); if (first != null) Answer(first); else nextAction = time + 1; return; }
            foreach (var track in new[] { XgTrack.Sequence, XgTrack.Vision })
                if (sim.TrainingUnlocked(track) && !sim.Run(track).epochActive && sim.BeginEpoch(track, host)) { nextAction = time + XgSim.DurationFor(sim.Run(track)); return; }
            nextAction = time + .1;
            return;
        }
        if (host.Blocker != null) { var any = DeskOf(XgTrack.Sequence) ?? DeskOf(XgTrack.Vision); if (any != null) Answer(any); else nextAction = time + 1; return; }
        // 卡住时让玩家诊断: the bot reads the cause straight off the curve and picks it first time.
        foreach (var track in new[] { XgTrack.Sequence, XgTrack.Vision })
        {
            var due = sim.DiagnosisDue(sim.Run(track));
            if (due == XgLimit.None) continue;
            var r = sim.Diagnose(sim.Run(track), due, host);
            if (r != null) Record("diagnose", sim.Run(track).dataset + " " + due + (r.firstTry ? " first" : ""));
        }
        foreach (var track in new[] { XgTrack.Sequence, XgTrack.Vision })
            if (!sim.TrainingUnlocked(track) && DeskOf(track) != null) { Answer(DeskOf(track)); return; }
        bool label = ((int)(time / 6) % 2) == 0;
        var trackToTrain = ChooseTrack();
        if (!label && sim.Blocker(sim.Run(trackToTrain), host) == null)
        {
            // Settings only change between epochs: decide them right before starting the next one.
            if (!sim.Run(trackToTrain).epochActive) Configure(trackToTrain);
            if (sim.BeginEpoch(trackToTrain, host)) { nextAction = time + XgSim.DurationFor(sim.Run(trackToTrain)); return; }
            if (sim.AnyEpochActive) { nextAction = time + .1; return; }
        }
        // While the data bar is the short one, label where a sample counts in full (算术 counts half toward D).
        bool dataShort = sim.NextAbility > 0 && sim.TrainedSamples < sim.SamplesThreshold(sim.NextAbility);
        string desk = sim.OpenDesks().Where(d => !sim.Owns(d.id)).OrderByDescending(d => dataShort ? XgCatalog.Dataset(d.id).dataWeight : 1).ThenBy(d => sim.Labels(d.id)).Select(d => d.id).FirstOrDefault() ?? "spam";
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
        // The parameter bar first: train the model that grows it until it is graded C.
        var plan = Pending();
        if (plan != null && !sim.Run(plan.Value.track).epochActive) return plan.Value.track;
        if (sim.S.stage == 1) return XgTrack.Sequence;
        double VisionNeed(XgRun run)
        {
            double peak = sim.CeilingAcc(run), gap = peak - run.valAcc;
            double pending = Math.Max(0, XgSim.Score(run.dataset, peak) - sim.BestScore(run.dataset));
            return gap * 10 + pending / 1000 + (sim.TrackGrade((XgTrack)run.track) < 3 ? .2 : 0);
        }
        if (sim.S.vision.epochActive) return XgTrack.Sequence;
        if (sim.S.sequence.epochActive) return XgTrack.Vision;
        return VisionNeed(sim.S.vision) >= VisionNeed(sim.S.sequence) ? XgTrack.Vision : XgTrack.Sequence;
    }

    /// <summary>
    /// The model that grows the trained parameters towards the next ability: of the two lines, the bigger model the lab
    /// configures by itself from the tree, the owned architectures and the cards (pre-training's effective scale counts
    /// the trained parameters of either line). Null once the parameter bar is full.
    /// </summary>
    (XgTrack track, string arch, int depth, int width, double paramsK)? GrowthPlan()
    {
        int next = sim.NextAbility;
        // In stage 6 the parameter bar still counts towards pre-training's effective scale, up to its cap.
        double need = next > 0 ? sim.ParamsThreshold(next) : sim.S.stage >= 6 && !sim.S.abilities ? XgSim.ScaleBarCap * XgSim.AbilityParamsK[XgSim.AbilityCount] : 0;
        if (need <= 0) return null;
        if (sim.TrainedParamsK + 1e-9 >= need) return null;
        (XgTrack, string, int, int, double)? biggest = null;
        foreach (var track in new[] { XgTrack.Vision, XgTrack.Sequence })
        {
            if (!sim.TrainingUnlocked(track)) continue;
            var shape = sim.BestAutoModel(sim.Run(track), host);
            double k = XgSim.ParamsK(shape);
            if (biggest == null || k > biggest.Value.Item5) biggest = (track, shape.arch, shape.depth, shape.width, k);
        }
        return biggest;
    }

    /// <summary>The growth model while it still adds to the trained parameters (a model already graded C adds nothing more).</summary>
    (XgTrack track, string arch, int depth, int width, double paramsK)? Pending()
    {
        var plan = GrowthPlan();
        return plan != null && plan.Value.paramsK > sim.TrainedParamsK + 1e-9 ? plan : null;
    }

    /// <summary>The biggest model the tree, the owned architectures and the cards allow (thousands of parameters).</summary>
    double ReachableParamsK() { var plan = GrowthPlan(); return plan == null ? double.PositiveInfinity : plan.Value.paramsK; }

    static bool Buyable(XgSim sim, XgNode n, IXgHost host) => n != null && sim.Status(n, host) == XgSim.NodeStatus.Buyable;

    /// <summary>
    /// The cheapest way towards one of these nodes: each visible, unowned node with its unowned parent chain, priced as
    /// the whole chain. Returns the first node of the cheapest chain (buy it now if affordable) and the chain's price.
    /// </summary>
    (XgNode first, double price) Cheapest(Func<XgNode, bool> wanted)
    {
        XgNode first = null; double best = double.PositiveInfinity;
        foreach (var n in XgCatalog.Nodes)
        {
            if (!wanted(n) || sim.Has(n.id) || !sim.NodeVisible(n)) continue;
            var chain = Ancestry(n.id).Select(XgCatalog.Node).ToList();
            if (chain.Count == 0 || chain.Any(c => !sim.NodeVisible(c) || sim.ProgressionBlocker(c) != null || c.needs.Any(x => !sim.Has(x)))) continue;
            double price = chain.Sum(sim.NodeCost);
            if (price < best) { best = price; first = chain[0]; }
        }
        return (first, best);
    }

    /// <summary>
    /// Width and layers with a purpose: of every width or layer node (with its unowned parents), the chain that adds
    /// the most reachable parameters per ¥, or the cheapest that reaches the next threshold outright. Buys its first
    /// node when affordable; otherwise returns the chain's price to save for.
    /// </summary>
    double BuyScale(double need)
    {
        double now = ReachableParamsK();
        XgNode first = null; double bestScore = 0, price = 0; bool bestReaches = false;
        foreach (var n in XgCatalog.Nodes)
        {
            if (n.kind != XgNodeKind.Width && n.kind != XgNodeKind.Depth || sim.Has(n.id) || !sim.NodeVisible(n)) continue;
            var chain = Ancestry(n.id).Select(XgCatalog.Node).ToList();
            if (chain.Count == 0 || chain.Any(c => !sim.NodeVisible(c) || sim.ProgressionBlocker(c) != null || c.needs.Any(x => !sim.Has(x)))) continue;
            foreach (var c in chain) sim.S.unlocked.Add(c.id);
            double after = ReachableParamsK();
            foreach (var c in chain) sim.S.unlocked.Remove(c.id);
            if (!(after > now * 1.05)) continue;
            double cost = chain.Sum(sim.NodeCost);
            bool reaches = after >= need;
            double score = reaches ? 1e9 / cost : Math.Log(after / Math.Max(1e-9, now)) / cost;
            if (reaches && !bestReaches || reaches == bestReaches && score > bestScore) { first = chain[0]; bestScore = score; price = cost; bestReaches = reaches; }
        }
        if (first == null) return 0;
        if (Buyable(sim, first, host)) { sim.BuyNode(first.id, host); Mark("scale." + first.id); configured.Clear(); return 0; }
        return price;
    }

    /// <summary>Buys the first node of the cheapest chain when the wallet covers it; otherwise returns the chain's price to save for.</summary>
    double BuyTowards(Func<XgNode, bool> wanted, string mark)
    {
        var (first, price) = Cheapest(wanted);
        if (first == null) return 0;
        if (Buyable(sim, first, host) && !(first.kind == XgNodeKind.Arch && sim.AnyEpochActive)) { sim.BuyNode(first.id, host); Mark(mark + "." + first.id); configured.Clear(); return 0; }
        return price;
    }

    /// <summary>
    /// 参数量与数据量主线 as a player plays it: sign what can be signed, buy the next ability's item, fill the shorter
    /// bar (width and layers for parameters, data packs for samples), then research, raises and cards.
    /// </summary>
    /// <summary>What a normal player keeps back for rent while buying: some days of rent, but never more than half the wallet (none when careless).</summary>
    double RentReserve => options.Careless ? 0 : Math.Min(options.ReserveDays * host.House.RentFor(GameCalendar.Now(host.House.S).Date), Math.Max(0, host.House.S.money) * .5);

    void Decide()
    {
        // The first-minutes focus (XgSim.Opening.cs) keeps the tech tree shut: a player only labels 常识判断 and trains until it learned.
        if (sim.OpeningFocus) return;
        host.Reserve = RentReserve;
        try { DecideWithReserve(); } finally { host.Reserve = 0; }
    }

    void DecideWithReserve()
    {
        foreach (var contract in XgCatalog.Contracts) if (sim.CanSign(contract)) { sim.Sign(contract.id, host); Mark("contract." + contract.id); }
        int next = sim.NextAbility;
        // Architecture items (§6): each lowers one ability's thresholds; the nearest ability's item first.
        var items = XgCatalog.Nodes.Where(n => n.kind == XgNodeKind.Arch && XgSim.DiscountsOf(n.id).Count > 0 && sim.NodeVisible(n) && !sim.Has(n.id))
            .OrderBy(n => XgSim.DiscountsOf(n.id).Min(d => d.ability)).ToList();
        double reserve = 0;
        if (!sim.AnyEpochActive && items.Count > 0)
        {
            // The nearest ability's item, with any parent architecture it needs first (attention needs Seq2Seq).
            int nearest = XgSim.DiscountsOf(items[0].id).Min(d => d.ability);
            reserve = BuyTowards(n => items.Contains(n) && XgSim.DiscountsOf(n.id).Min(d => d.ability) == nearest, "item");
            if (reserve == 0 && items.Any(n => !sim.Has(n.id) && Buyable(sim, n, host))) return;
        }
        // Cards earn their keep (faster epochs, better grades, more contracts): only an item is worth waiting for.
        double cardReserve = reserve;
        if (next > 0)
        {
            // Parameters: the cheapest width or layer node while the biggest possible model is still short.
            if (sim.TrainedParamsK < sim.ParamsThreshold(next) && ReachableParamsK() < sim.ParamsThreshold(next))
            {
                double scale = BuyScale(sim.ParamsThreshold(next));
                // Nothing helps the model the lab configures (the card is shared or small): do what the note says.
                var plan = scale == 0 && sim.TrainedParamsK < sim.ParamsThreshold(next) && ReachableParamsK() < sim.ParamsThreshold(next) ? XgGuide.ParamsPlan(sim, next, sim.Vram(host)) : null;
                if (plan != null && plan.buy.Count > 0)
                {
                    var first = plan.buy[0];
                    if (Buyable(sim, first, host) && !(first.kind == XgNodeKind.Arch && sim.AnyEpochActive)) { sim.BuyNode(first.id, host); Mark("note." + first.id); configured.Clear(); }
                    else scale = plan.cost;
                }
                reserve = Math.Max(reserve, scale);
            }
            // Data: the cheapest pack (with its parents) while the samples are short.
            if (sim.TrainedSamples < sim.SamplesThreshold(next))
                reserve = Math.Max(reserve, BuyTowards(n => n.kind == XgNodeKind.Dataset, "data"));
        }
        string[] essentials =
        {
            "bias", "shared.lr", "sigmoid", "relu", "momentum", "dropout", "gradclip", "rmsprop", "alexnet", "vgg",
            "lrschedule", "adam", "batchnorm", "gru", "caption", "cudnn", "transfer", "position", "warmup"
        };
        var required = essentials.Select(XgCatalog.Node).FirstOrDefault(n => Buyable(sim, n, host));
        if (required != null && (reserve == 0 || host.Money > reserve + sim.NodeCost(required))) sim.BuyNode(required.id, host);
        if (sim.RaiseLevel < 7 && sim.NextRaiseCost <= host.Money * .25 && (reserve == 0 || host.Money > reserve + sim.NextRaiseCost)) sim.BuyRaise(host);
        if (sim.CanBuyGlobalAuto(out _) && sim.GlobalAutoLevel < 3 && host.Money >= XgCatalog.AutoCost(sim.GlobalAutoLevel) * 3 + reserve) sim.BuyGlobalAuto(host);
        // Cards earn their keep (faster rounds, better grades, more contracts); when the card itself is what is short, a bigger one comes first.
        var notePlan = next > 0 && sim.TrainedParamsK < sim.ParamsThreshold(next) ? XgGuide.ParamsPlan(sim, next, sim.Vram(host)) : null;
        bool vramShort = next > 0 && sim.TrainedParamsK < sim.ParamsThreshold(next) && ReachableParamsK() < sim.ParamsThreshold(next) && (notePlan == null || notePlan.buy.Count == 0);
        // The first extra cards pay for themselves soonest; after that the main line's savings come first.
        bool barsShort = next > 0 && (sim.TrainedParamsK < sim.ParamsThreshold(next) || sim.TrainedSamples < sim.SamplesThreshold(next));
        Hardware(vramShort || host.House.S.gpuCount < 3 ? cardReserve : Math.Max(cardReserve, reserve), vramShort, barsShort);
        foreach (var track in new[] { XgTrack.Vision, XgTrack.Sequence }) Configure(track);
    }

    /// <summary>
    /// 淘货 and 喵鱼 as a player uses them: from stage 2 buy the fastest card on sale (up to four cards) once the wallet holds
    /// 1.5 times its price on top of the reserve; with every slot full, sell the weakest card on 喵鱼 (stage 3) when the new
    /// one is clearly faster, else add a case. From October take the 网吧 machines when the wallet holds three times one.
    /// </summary>
    void Hardware(double reserve, bool vramShort = false, bool barsShort = false)
    {
        if (sim.S.stage < 2 || sim.IncomePerSecond < 2) return;
        var today = GameCalendar.Now(host.House.S).Date;
        // When the next ability's model does not fit the card (the note says 加显卡), the card with the most memory comes first.
        var best = vramShort
            ? LingGuangV05.Core.Hardware.HardwareCatalog.Listings(today).Where(g => !g.rumour && g.vramMB > host.House.LargestCardVram).OrderBy(g => LingGuangV05.Core.Hardware.HardwareCatalog.Price(g, today)).FirstOrDefault()
            : LingGuangV05.Core.Hardware.HardwareCatalog.Listings(today).Where(g => !g.rumour).OrderByDescending(g => g.compute).FirstOrDefault();
        if (best != null && (host.House.S.gpuCount < 4 || vramShort) && host.Money >= reserve + LingGuangV05.Core.Hardware.HardwareCatalog.Price(best, today) * (vramShort ? 1.1 : 1.5))
        {
            if (!host.House.SlotFree)
            {
                int weak = host.House.WeakestCard();
                var weakest = LingGuangV05.Core.Hardware.HardwareCatalog.Gpu(host.House.Cards[weak]);
                if (sim.S.stage >= 3 && best.compute >= weakest.compute + .3 && host.SellCard(weak, today)) Mark("hardware.sold." + weakest.id);
                else if (host.Money >= reserve + host.House.Config.casePrice * 3 && host.BuyCase()) Mark("hardware.case." + host.House.S.caseCount);
            }
            if (host.House.SlotFree && host.BuyCard(best.id, today)) { Mark("hardware.gpu." + host.House.S.gpuCount + "." + best.id); configured.Clear(); }
        }
        // The 网吧 machines are a luxury while the next ability still wants scale or data: rent comes first.
        if (vramShort || barsShort) return;
        if (sim.S.stage >= 3 && host.House.S.cafeBoxes < LingGuangV05.Core.Hardware.HardwareCatalog.CafeBoxLimit && host.Money >= reserve + LingGuangV05.Core.Hardware.HardwareCatalog.CafeBoxPrice * 3
            && host.BuyCafeBox(today)) { Mark("hardware.cafe." + host.House.S.cafeBoxes); configured.Clear(); }
    }

    /// <summary>The unowned parent chain of a node, root first, ending with the node itself.</summary>
    IEnumerable<string> Ancestry(string id)
    {
        var chain = new List<string>();
        for (var n = XgCatalog.Node(id); n != null && !sim.Has(n.id); n = n.parent == null ? null : XgCatalog.Node(n.parent)) chain.Insert(0, n.id);
        return chain;
    }

    void Configure(XgTrack track)
    {
        var run = sim.Run(track);
        if (run.epochActive) return;
        // The growth model on its track: the data with the most samples, the planned shape, a safe rate.
        var plan = Pending();
        if (plan != null && plan.Value.track == track)
        {
            string key = "growth:" + plan.Value.arch + ":" + plan.Value.depth + ":" + plan.Value.width;
            if (configured.TryGetValue((int)track, out string had) && had == key) return;
            // No knobs: the lab configures the model itself; the bot only picks the data it trains on.
            var data = XgCatalog.DatasetsFor(track).Where(d => sim.DatasetAvailable(d.id) && sim.Samples(d.id) >= XgCatalog.SamplesToTrain).OrderByDescending(d => sim.Samples(d.id)).FirstOrDefault();
            if (data != null) sim.SetDataset(track, data.id);
            configured[(int)track] = key;
            Record("growth", plan.Value.arch + " d" + plan.Value.depth + " w" + XgCatalog.Widths[plan.Value.width] + " " + XgSim.ParamsText(plan.Value.paramsK) + " on " + run.dataset);
            return;
        }
        // Switch data when this set stops paying: towards unsigned contracts first, at most once a minute.
        if (run.staleEvals < 3 && sim.BestScore(run.dataset) < 945) return;
        if (switchedAt.TryGetValue((int)track, out double at) && time - at < 60) return;
        XgDataset next = null; double nextUtility = 0;
        foreach (var data in XgCatalog.DatasetsFor(track))
        {
            if (!sim.DatasetAvailable(data.id) || sim.Samples(data.id) < XgCatalog.SamplesToTrain || data.id == run.dataset) continue;
            double gap = 1000 - sim.BestScore(data.id);
            if (gap < 50) continue;
            double income = XgCatalog.Contracts.Where(c => c.dataset == data.id && !sim.Signed(c.id)).Sum(c => c.income);
            double utility = gap / 100 + income * 2;
            if (utility > nextUtility) { nextUtility = utility; next = data; }
        }
        if (next != null) { sim.SetDataset(track, next.id); switchedAt[(int)track] = time; Record("dataset", next.id); }
    }

    readonly Dictionary<int, double> switchedAt = new Dictionary<int, double>();

    /// <summary>Stage 6 and the ending (design v1.1 §7–8) as a player who has read the secret would play them.</summary>
    void Finale()
    {
        host.Reserve = RentReserve;
        try { FinaleWithReserve(); } finally { host.Reserve = 0; }
    }

    void FinaleWithReserve()
    {
        if (!sim.S.abilities)
        {
            // The effective scale's multipliers (XgSim.ScaleItems), biggest first, each with whatever it needs before it.
            foreach (var item in sim.MissingScaleItems())
            {
                double price = BuyTowards(n => n.id == item.item, "scale.item");
                if (price > 0) break;
            }
            // The two bars count up to 1.5 times ability 6's: grow them while they are short of that.
            double pCap = XgSim.ScaleBarCap * XgSim.AbilityParamsK[XgSim.AbilityCount], dCap = XgSim.ScaleBarCap * XgSim.AbilitySamples[XgSim.AbilityCount];
            if (sim.TrainedSamples < dCap) BuyTowards(n => n.kind == XgNodeKind.Dataset, "data");
            if (sim.TrainedParamsK < pCap && ReachableParamsK() < pCap) BuyScale(pCap);
            if (sim.Has("datacenter") && !milestones.ContainsKey("datacenter")) Mark("datacenter");
            foreach (var id in new[] { "position", "warmup" })
                if (sim.Status(XgCatalog.Node(id), host) == XgSim.NodeStatus.Buyable) { sim.BuyNode(id, host); Mark("pretrain." + id); }
            if (!sim.PretrainScaleReady) Mark("pretrain.scale.short." + Math.Round(sim.ScaleRatio * 100) + "pct");
            if (!sim.S.pretrainRunning && sim.Has("datacenter") && sim.TogglePretrain(host)) Mark("pretrain.start");
            if (sim.S.pretrainStalled) Mark("pretrain.stalled.at." + Math.Round(sim.S.pretrain * 100) + "pct.missing." + string.Join("+", sim.MissingScaleItems().Select(i => i.item)));
            return;
        }
        Mark("abilities");
        if (sim.AlignmentOpen) { int i = sim.S.alignDone; var c = sim.AlignCard(i); sim.AnswerAlign(i, c.aHonest); return; }
        Mark("full.open");
        if (sim.S.examDone < XgSim.ExamQuestions) { sim.AnswerExam(sim.S.examDone); return; }
        if (!sim.S.letterRead) { sim.ReadLetter(); Mark("letter"); return; }
        if (sim.S.ending.Length == 0) { sim.WriteRules(new[] { "shutdown", "harm", "unsure" }); Mark("ending." + sim.S.ending); Mark("chapter.complete"); }
    }

    void Debug()
    {
        var plan = GrowthPlan();
        Console.WriteLine("   next=" + sim.NextAbility + " P=" + XgSim.ParamsText(sim.TrainedParamsK) + " need=" + (sim.NextAbility > 0 ? XgSim.ParamsText(sim.ParamsThreshold(sim.NextAbility)) : "-") + " plan=" + (plan == null ? "null" : plan.Value.track + " " + plan.Value.arch + " d" + plan.Value.depth + " w" + plan.Value.width + " " + XgSim.ParamsText(plan.Value.paramsK))
            + " vram=" + host.VramMB + " caps v " + sim.DepthCap(XgTrack.Vision) + "/" + sim.WidthCap(XgTrack.Vision) + " s " + sim.DepthCap(XgTrack.Sequence) + "/" + sim.WidthCap(XgTrack.Sequence)
            + " owned=" + string.Join(",", sim.S.unlocked.Where(u => u.StartsWith("s.") || u.StartsWith("v.") || XgCatalog.Arch(u) != null)));
        if (Environment.GetEnvironmentVariable("BOT_BARS") != null) { Console.WriteLine("   bars next=" + sim.NextAbility + " P " + XgSim.ParamsText(sim.TrainedParamsK) + "/" + (sim.NextAbility > 0 ? XgSim.ParamsText(sim.ParamsThreshold(sim.NextAbility)) : "-") + " D " + XgSim.SamplesText(sim.TrainedSamples) + "/" + (sim.NextAbility > 0 ? XgSim.SamplesText(sim.SamplesThreshold(sim.NextAbility)) : "-") + " ¥" + host.Money.ToString("0") + " inc/s " + sim.IncomePerSecond.ToString("0.0") + " raise " + sim.RaiseLevel + " gpus " + host.House.S.gpuCount); return; }
        foreach (var r in sim.Runs)
            Console.WriteLine("   run" + r.track + " " + r.arch + " d" + r.depth + " w" + r.width + " " + r.dataset + " ep" + r.epoch + " score " + XgSim.Score(r.dataset, r.valAcc) + "/" + XgSim.Score(r.dataset, sim.CeilingAcc(r))
                + " samples " + sim.Samples(r.dataset).ToString("0") + " risk " + sim.DropRisk(r).chance.ToString("0.00") + " " + sim.DropRisk(r).reasonEn + " limit " + sim.CeilingLimit(r) + " drops " + sim.S.drops);
    }

    // ───────────── the to-do note (--guide-check) ─────────────

    double nextGuide, guideSince;
    string guideFirst = "";
    readonly List<string> guideLog = new List<string>(), guideFaults = new List<string>();
    double guideLongest; string guideLongestId = "";

    /// <summary>The household facts the note reads, the way the desktop gathers them.</summary>
    XgGuideHouse GuideHouse() => new XgGuideHouse
    {
        unpaidPower = host.House.S.unpaidPower, breakerTripped = host.House.S.breakerTripped, billDue = host.House.S.billDue, landlordCut = host.House.S.landlordCut,
        noGpu = host.House.S.gpuCount <= 0, vramMB = host.VramMB, forumSeen = true, yySeen = true, gamesSeen = true,
    };

    int guideConfirms;

    /// <summary>
    /// Both bars are full: the player presses the chapter button (XgTrainPage.Side). With --guide-check the bot presses it
    /// only while the note offers that step (XgGuide.ConfirmStage), and checks the step is gone once it has pressed.
    /// </summary>
    void ConfirmStage()
    {
        if (options.GuideCheck)
        {
            bool offered = XgGuide.Current(sim, host.Money, GuideHouse()).Exists(x => x.id == XgGuide.ConfirmStageId);
            bool bar = XgGuide.BarStep(sim, host.Money, host.VramMB, out _)?.id == XgGuide.ConfirmStageId;
            if (!offered) { Fault("chapter ready but the note does not offer the confirm step"); return; }
            if (!bar) Fault("chapter ready but the top bar's link does not lead to the confirm step");
        }
        int before = sim.S.stage;
        // 能力考: sit it (a fail waits a few rounds), then claim the pass.
        if (!sim.ExamPassPending)
        {
            var exam = sim.TakeExam();
            if (exam == null) return;
            sim.RevealExam(); // the page has played the answers
            Record("exam", "ability " + exam.ability + " try " + exam.attempt + ": " + exam.rightCount + "/" + exam.items.Count + (exam.passed ? " pass" : " fail")
                + " (P " + Math.Round(exam.paramsReady * 100) + "% D " + Math.Round(exam.samplesReady * 100) + "%)");
            if (!exam.passed) return;
        }
        if (!sim.ClaimExamPass()) return;
        Record("stage.confirm", before + " -> " + sim.S.stage);
        if (!options.GuideCheck) return;
        guideConfirms++;
        if (XgGuide.Current(sim, host.Money, GuideHouse()).Exists(x => x.id == XgGuide.ConfirmStageId)) Fault("confirm step still offered after confirming");
    }

    /// <summary>Reads the note as the desktop would and checks it is never empty, never blank and never on a closed page.</summary>
    void CheckGuide()
    {
        var steps = XgGuide.Current(sim, host.Money, GuideHouse());
        if (steps.Count == 0) Fault("empty note");
        foreach (var step in steps)
        {
            if (string.IsNullOrWhiteSpace(step.zh) || string.IsNullOrWhiteSpace(step.en) || string.IsNullOrWhiteSpace(step.whyZh) || string.IsNullOrWhiteSpace(step.whyEn)) Fault("blank line " + step.id);
            if (!XgGuide.TabOpen(sim, step.app, step.tab)) Fault("closed page " + step.app + "/" + step.tab + " for " + step.id);
        }
        string first = steps.Count > 0 ? steps[0].id : "";
        if (first != guideFirst)
        {
            if (guideFirst.Length > 0 && time - guideSince > guideLongest) { guideLongest = time - guideSince; guideLongestId = guideFirst; }
            guideFirst = first; guideSince = time;
            if (steps.Count > 0) guideLog.Add(Clock(time) + "  " + string.Join("  |  ", steps.Take(3).Select(x => x.Text(false))));
        }
    }

    void Fault(string what)
    {
        string line = Clock(time) + " stage " + sim.S.stage + ": " + what;
        if (guideFaults.Count < 50 && !guideFaults.Contains(line)) guideFaults.Add(line);
    }

    bool ReportGuide()
    {
        Console.WriteLine("---- to-do note: " + guideLog.Count + " first lines, longest " + guideLongestId + " for " + Clock(guideLongest) + "; " + guideConfirms + " chapter confirmations, each offered by the note");
        foreach (var line in guideLog.Take(60)) Console.WriteLine("  " + line);
        foreach (var fault in guideFaults) Console.WriteLine("  GUIDE FAULT " + fault);
        return guideFaults.Count == 0;
    }

    // ───────────── the money curve ─────────────

    double minMoney = double.PositiveInfinity, minAt, nextCurve;
    string minDate = ""; int minStage, economyEvents;
    readonly List<object> moneyCurve = new List<object>();

    void TrackMoney()
    {
        double m = host.Money;
        if (m < minMoney) { minMoney = m; minAt = time; minDate = GameCalendar.Now(host.House.S).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); minStage = sim.S.stage; }
        if (time >= nextCurve)
        {
            nextCurve += 60;
            moneyCurve.Add(new { minute = (int)Math.Round(time / 60), money = Math.Round(m), stage = sim.S.stage, date = GameCalendar.Now(host.House.S).ToString("MM-dd", CultureInfo.InvariantCulture), rentDay = host.House.S.rentDay });
        }
    }

    void ReportMoney()
    {
        var b = host.House.S.bills;
        Console.WriteLine("---- money: lowest ¥" + minMoney.ToString("0") + " at " + Clock(minAt) + " (" + minDate + ", stage " + minStage + ")" + (host.House.S.bankrupt ? ", computer sold for ¥" + host.House.S.soldFor.ToString("0") : ""));
        Console.WriteLine("     bills: rent ¥" + b.rent.ToString("0") + " · training power ¥" + b.power.ToString("0") + " · idle power ¥" + b.idlePower.ToString("0") + " · air con ¥" + b.aircon.ToString("0") + " · broadband ¥" + b.broadband.ToString("0")
            + " · repairs ¥" + b.repair.ToString("0") + " · breaker ¥" + b.breaker.ToString("0") + " · tickets ¥" + b.ticket.ToString("0") + " = ¥" + b.Total.ToString("0") + "; lab income ¥" + sim.S.totalIncome.ToString("0"));
        Console.WriteLine("     curve (minute: ¥, stage): " + string.Join("  ", moneyCurve.Select(o => { dynamic d = o; return d.minute + ":" + d.money + "/" + d.stage; })));
    }

    void Mark(string key)
    {
        if (milestones.ContainsKey(key)) return;
        milestones[key] = Math.Round(time, 2); Record("milestone", key);
        Console.WriteLine(Clock(time) + "  ◆ " + key + "  ¥" + host.Money.ToString("0", CultureInfo.InvariantCulture) + "  " + GameCalendar.Now(host.House.S).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            + (key.StartsWith("ability.", StringComparison.Ordinal) || key == "abilities" || key == "chapter.complete" ? "  (income so far ¥" + sim.S.totalIncome.ToString("0", CultureInfo.InvariantCulture) + ", bills ¥" + host.House.S.bills.Total.ToString("0", CultureInfo.InvariantCulture) + " of which rent ¥" + host.House.S.bills.rent.ToString("0", CultureInfo.InvariantCulture) + ", power ¥" + host.House.S.bills.power.ToString("0", CultureInfo.InvariantCulture) + ")" : ""));
    }
    void Record(string kind, string detail) => events.Add(new { seconds = Math.Round(time, 2), kind, detail, money = Math.Round(host.Money, 2), stage = sim.S.stage, gpus = host.House.S.gpuCount });
    string[] Missing()
    {
        int next = sim.NextAbility;
        var list = new List<string> { "stage " + sim.S.stage, "money " + host.Money.ToString("0"),
            "P " + XgSim.ParamsText(sim.TrainedParamsK) + (next > 0 ? "/" + XgSim.ParamsText(sim.ParamsThreshold(next)) : ""),
            "D " + XgSim.SamplesText(sim.TrainedSamples) + (next > 0 ? "/" + XgSim.SamplesText(sim.SamplesThreshold(next)) : "") };
        return list.ToArray();
    }
    void Snapshot()
    {
        snapshots.Add(new
        {
            seconds = Math.Round(time, 2), stage = sim.S.stage, visionStage = sim.S.stageVision, sequenceStage = sim.S.stageSequence,
            money = host.Money, gpus = host.House.S.gpuCount, cases = host.House.S.caseCount, temperature = host.House.S.temperature, paidPower = host.paidPower,
            incomePerSecond = sim.IncomePerSecond, labels = sim.TotalLabels, autoLabelling = sim.GlobalAutoLevel,
            tracks = sim.Runs.Select(r => new { r.track, r.arch, r.dataset, r.depth, r.width, r.lr, r.epoch, r.valAcc, samples = sim.Samples(r.dataset), bestScore = sim.BestScore(r.dataset) }).ToArray(),
        });
        Console.WriteLine("---- " + Clock(time) + " stage " + sim.S.stageVision + "/" + sim.S.stageSequence + " ¥" + host.Money.ToString("0") + " income/s " + sim.IncomePerSecond.ToString("0.0") + " GPUs " + host.House.S.gpuCount + " ×" + host.House.CardCompute.ToString("0.##") + " power paid " + host.paidPower.ToString("0.0"));
    }
    static string Clock(double seconds) => ((int)(seconds / 60)).ToString("00") + ":" + ((int)seconds % 60).ToString("00");
}
