using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LingGuangV05.Core;
using LingGuangV05.Runtime.Persistence;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

public static class PlayabilityChecks
{
    [Serializable] public sealed class Report { public string completedUtc; public List<string> passed = new List<string>(); public string failure; public List<string> pacing = new List<string>(); }
    class Wallet : IXgHost
    {
        public double money;
        public double Compute => 1;
        public double VramMB => 6144;
        public double Money => money;
        public string Blocker => null;
        public bool Spend(double x) { if (x < 0 || money < x) return false; money -= x; return true; }
        public void Earn(double x) { money += x; }
        public void Train(double seconds) { }
    }
    sealed class RigWallet : IXgHost, IXgRig, IXgHousePower, IXgTrainingBill
    {
        public readonly ChapterOneSim house = new ChapterOneSim();
        public RigWallet() { house.PowerCostsEnabled = true; }
        public double money => house.S.money;
        public double Money => money;
        public double Compute => Blocker == null ? 1 : 0;
        public double VramMB => 6144;
        public string Blocker => house.S.bankrupt ? "Bankrupt" : house.PowerCostsEnabled && (house.S.landlordCut || house.S.breakerTripped || house.S.unpaidPower) ? "House power off" : null;
        public bool Spend(double x) { if (x < 0 || money < x) return false; house.S.money -= x; return true; }
        public void Earn(double x) { house.S.money += x; }
        public void Train(double seconds) { house.BillTraining(seconds); }
        public void TrainScaled(double seconds, double price, double energy, double wear) { house.BillTraining(seconds, price, energy, wear); }
        public void CoolCards() { house.CoolCards(); }
        public IReadOnlyList<XgCardSpec> Cards { get; } = new[] { new XgCardSpec { id = "gtx980ti", name = "GTX 980 Ti", compute = 1, vramMB = 6144, watts = 250 } };
        public double PowerLimitWatts => 3500;
        public double HouseWatts => house.HouseWatts;
        public bool BreakerTripped => house.S.breakerTripped;
        public string HousePowerBlocker => Blocker;
        public double UnpluggedWatts { get => house.UnpluggedWatts; set => house.UnpluggedWatts = value; }
        public void TripBreaker() { house.TripBreakerNow(); }
        public bool ResetBreaker() { return house.ResetBreaker(); }
        public void Advance(XgSim sim, double seconds)
        {
            house.RentWaived = () => sim.AbilitiesCount < XgSim.RentFromAbility;
            // Same calendar, gates and billing as the real desktop, in one-second steps.
            for (double left = seconds; left > 0; left -= 1)
            {
                double dt = Math.Min(1, left);
                house.PowerCostsEnabled = sim.SystemUnlocked("power");
                GameCalendar.Advance(house.S, GameCalendar.DayFor(sim.S.stage, sim.MonthProgress));
                house.Tick(dt);
                sim.Today = GameCalendar.Yyyymmdd(GameCalendar.Now(house.S));
                sim.Tick(dt, this);
            }
        }
    }
    static Report report;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); report.passed.Add(name); }
    public static void Run()
    {
        report = new Report { completedUtc = DateTime.UtcNow.ToString("o") };
        Directory.CreateDirectory("LocalVerification");
        try
        {
            var sim = new XgSim(); var wallet = new Wallet { money = 100 };
            OpeningFlowChecks();
            Check(sim.S.desk == XgMemes.SenseDesk, "Fresh game starts with a common-sense lesson");
            Check(sim.GhostGuess(XgMemes.SenseDesk, out _, out _), "Default common-sense card has a prediction without a checkpoint");
            sim.SelectDesk("spam"); // The following reward and assessment regression is specifically an SMS lesson.
            Check(!sim.TrainingUnlocked(XgTrack.Sequence), "Training is not unlocked before collecting examples");
            var sample = new XgCard { dataset = "spam", seed = 9137, question = "恭喜您中奖，请点击领取", truth = true, level = 1 };
            sim.TeachingGuess(sample, out bool original);
            // Force a known correction so the assertion does not depend on a lucky initial coin flip.
            sample.truth = !original;
            double correctionPay = 0, confirmPay = 0;
            for (int i = 0; i < 5; i++)
            {
                sample.id = 9000 + i;
                sim.S.cards.RemoveAll(c => c.dataset == "spam"); sim.S.cards.Add(sample);
                sim.TeachingGuess(sample, out bool predicted);
                var answer = sim.Answer("spam", sample.truth, wallet);
                Check(answer.correct, "Lesson " + (i + 1) + " is accepted");
                if (predicted != sample.truth) correctionPay = answer.pay; else confirmPay = answer.pay;
            }
            sim.TeachingGuess(sample, out bool learned);
            Check(learned == sample.truth, "Repeated correction flips the same concept within five lessons");
            Check(sim.Board.Concepts("tone").Any() || sim.Board.Concepts("logic").Any() || sim.Board.Concepts("sequence").Any(), "Lessons create real board concepts");
            Check(correctionPay > 0 && confirmPay > 0 && Math.Abs(correctionPay - 2 * confirmPay) < .00001, "Correction reward is exactly twice confirmation");
            Check(sim.S.recentCorrections.Count > 0 && sim.S.recentCorrections.Count <= 20, "Recent correction memory is retained and bounded");
            Check(sim.TrainingUnlocked(XgTrack.Sequence), "Legacy save fixture keeps its already available training");
            sim.SetDataset(XgTrack.Sequence, "spam");
            Check(sim.TrainEpoch(XgTrack.Sequence, wallet) != null, "First training can complete on starter hardware");
            Check(sim.S.checkups.Any(c => c.taught), "Assessment reuses an actual player correction");
            Check(sim.S.checkups.Any(c => !c.taught && c.question.Contains("中奖")), "Assessment includes the fixed scam SMS question");
            var loaded = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
            Check(loaded.S.recentCorrections.Count == sim.S.recentCorrections.Count && loaded.S.checkups.Count == sim.S.checkups.Count, "Learning evidence survives save and load");
            loaded.TeachingGuess(sample, out bool restored);
            Check(restored == learned, "Learned prediction survives save and load");
            Check(loaded.PersonaBoard(6, null).Contains("最近被纠正"), "Actual lessons reach the conversation prompt");
            var legacy = new XgSim(); legacy.S.teachMode = false;
            Check(!legacy.GhostGuess("spam", out _, out _), "Teaching switch restores the legacy prediction gate");
            var reflection = new XgSim(); reflection.S.project.completed = true;
            Check(reflection.EndingAvailable && reflection.EndingAnswer(false), "Legacy reflection accepts the player's answer");
            Check(!reflection.S.chapterComplete && reflection.S.endingReflectionAnswered && !reflection.EndingAvailable && reflection.LabelPresentationMode(false) == 0, "Reflection returns to teaching without ending the game or repeating");
            reflection.S.chapterComplete = true; reflection.S.endingReflectionAnswered = false; reflection.S.ending = "";
            var migratedReflection = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(reflection.S)));
            Check(!migratedReflection.S.chapterComplete && migratedReflection.S.endingReflectionAnswered, "Old premature chapter ending migrates to a completed reflection");
            reflection.S.ending = "E2";
            var completedEnding = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(reflection.S)));
            Check(completedEnding.S.chapterComplete && completedEnding.S.ending == "E2", "A real final ending is preserved on reload");
            Check(sim.SystemUnlocked("power") && !sim.SystemUnlocked("quality") && !sim.QualityActive, "Power costs begin at stage one; quality penalties remain staged");
            sim.S.stage = 4;
            Check(sim.SystemUnlocked("power") && sim.SystemUnlocked("quality"), "Systems unlock at their configured stages");
            var house = new ChapterOneSim(); house.PowerCostsEnabled = sim.SystemUnlocked("power");
            house.S.money=10000; house.RentWaived=()=>true;
            double quote=house.TrainingRoundPrice(10),beforePower=house.S.money;
            Check(quote>0,"Stage-one training electricity is never free");
            house.BillTraining(10);
            Check(Math.Abs(beforePower-house.S.money-quote)<1e-8&&house.S.monthKwh>0,"Training records paid energy while rent is waived");
            UpgradeChecks();
            Pacing(6);
            Pacing(12);
        }
        catch (Exception e) { report.failure = e.ToString(); throw; }
        finally { File.WriteAllText("LocalVerification/playability-checks.json", JsonUtility.ToJson(report, true)); }
        Debug.Log("PLAYABILITY_CHECKS_PASSED " + report.passed.Count);
    }

    static void OpeningFlowChecks()
    {
        var sim=new XgSim();var wallet=new Wallet { money=100 };sim.BeginOpening();
        Check(!sim.TeachingTreeOpen&&!sim.TrainingUnlocked(XgTrack.Sequence),"Fresh opening contains only Teach");
        for(int i=0;i<12;i++)
        {
            Check(!sim.TeachingTreeOpen,"Tech stays hidden before answer "+(i+1));
            var card=sim.Card(XgMemes.SenseDesk);
            Check(sim.TryAnswerStatement(XgMemes.SenseDesk,card.id,card.truth,wallet,out var result)&&result.correct,"Opening accepts actual statement answer "+(i+1));
        }
        Check(sim.TeachingTreeOpen&&!sim.TrainingUnlocked(XgTrack.Sequence),"Twelve answers reveal Tech but not Train");
        double before=wallet.Money;
        Check(sim.BuyNode("train.curve",wallet)&&wallet.Money<before,"Player buys the training curve");
        Check(sim.TrainingUnlocked(XgTrack.Sequence)&&sim.TrainEpoch(XgTrack.Sequence,wallet)!=null,"Purchased curve permits the first real training round");
        var loaded=new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
        Check(loaded.TeachingAnswers==12&&loaded.TeachingTrainOpen&&loaded.S.epochs==sim.S.epochs,"Opening progression survives load");
    }

    static double houseBills(RigWallet wallet) => wallet.house.S.bills.Total;

    static void UpgradeChecks()
    {
        var reader = new XgSim();
        double before = reader.ExtraSamples(XgSim.ReadDataset);
        Check(reader.ResidentRead("学习") && reader.ExtraSamples(XgSim.ReadDataset) == before + 1, "New spider reading credits an actual training sample");
        Check(!reader.ResidentRead("学习") && !reader.ResidentRead("123") && !reader.ResidentRead("！！"), "Repeated words and non-words do not inflate the reading ledger");
        Check(reader.ResidentRead("Reading") && !reader.ResidentRead("reading!"), "Reading deduplicates case and surrounding punctuation");
        var reloaded = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(reader.S)));
        Check(reloaded.ReadSamplesTotal == reader.ReadSamplesTotal && reloaded.ExtraSamples(XgSim.ReadDataset) == reader.ExtraSamples(XgSim.ReadDataset), "Reading samples survive reload without double credit");
        var oldReading = new XgSim(); oldReading.S.readSchema = 0; oldReading.S.crawlWords = 10;
        var repaired = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(oldReading.S)));
        var repairedTwice = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(repaired.S)));
        Check(repaired.ExtraSamples(XgSim.ReadDataset) == 10 && repairedTwice.ExtraSamples(XgSim.ReadDataset) == 10, "Old crawler tally is repaired once on upgrade");
        Check(reader.DeskOpen(XgMemes.SenseDesk) && reader.S.desk == XgMemes.SenseDesk, "Common sense remains the default desk after resident reading");

        var profile = new PrologueProfile { name = "灵光", self = "我", callMe = "你", personality = "好奇，诚实" };
        var game = new ChapterOneSim();
        Check(PlayerProfile.FromState(game.S) == null && game.CompletePrologue(profile), "A new player still gets the opening, and setup completes");
        var known = PlayerProfile.FromState(game.S);
        Check(PlayerProfile.IsUsable(known) && known.CopyOfSetup().name == profile.name, "Finished old save supplies the new replay profile");
        string path = Path.Combine("LocalVerification", "ProfileChecks", Guid.NewGuid().ToString("N"));
        var saves = new AtomicSaveStore(path); var profiles = new PlayerProfileStore(saves);
        Check(saves.TrySave(JsonUtility.ToJson(game.S), out _) && profiles.DeriveFromSaves() != null, "Opening profile can be recovered from a real saved payload");
        Check(profiles.Write(known, out _) && profiles.Read()?.setup.name == profile.name, "Opening profile persists independently of a playthrough");
        Check(saves.ArchiveForReset(out _) && PlayerProfile.IsUsable(profiles.Read()), "Reset archives game progress without losing the opening profile");

        var rent = new ChapterOneSim(); rent.S.money = 100000; rent.RentWaived = () => true;
        GameCalendar.Advance(rent.S, GameCalendar.DayIndex(new DateTime(2016, 6, 2)));rent.SettleCalendar();
        Check(rent.Debt==0&&rent.S.bills.rent==0,"Rent waiver creates no invoice");
        rent.RentWaived=()=>false;rent.SettleCalendar();
        GameCalendar.Advance(rent.S, GameCalendar.DayIndex(new DateTime(2016, 6, 28)));rent.SettleCalendar();
        Check(rent.Debt==0&&rent.S.bills.rent==0,"Calendar jumps never fabricate accrued play-time rent");
        rent.Tick(rent.Config.rentDaySeconds+.01);
        double owed=rent.Debt,paid=rent.S.bills.rent;
        Check(owed>0&&paid==0,"Rent becomes an unpaid invoice, never an automatic debit");
        double cash=rent.S.money;
        Check(rent.TransferRent(100,out _)&&Math.Abs(rent.Debt-(owed-100))<1e-8&&Math.Abs(cash-rent.S.money-100)<1e-8,"Partial transfer debits exactly its amount");
        var persisted=new ChapterOneSim(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(rent.S)));
        Check(Math.Abs(persisted.Debt-rent.Debt)<1e-8,"Partially paid invoice survives save/load");
        Check(persisted.TransferRent(persisted.Debt,out _)&&persisted.Debt==0,"Remaining invoice settles manually");
        cash=persisted.S.money;
        Check(!persisted.TransferRent(100,out _)&&persisted.S.money==cash,"Paid invoice cannot be paid twice");
    }

    // A transparent optimistic pacing probe, not a substitute for a human playtest.
    static void Pacing(int interval)
    {
        var sim = new XgSim(); var wallet = new RigWallet();sim.BeginOpening();
        var personas=Resources.Load<TextAsset>("LingGuangV05/YY/persona_packs");
        var unlocks=Resources.Load<TextAsset>("LingGuangV05/YY/chat_unlocks");
        Check(personas!=null&&unlocks!=null,"Pacing uses production dialogue-gate data");
        XgSim.LoadYyLearning(personas.text,unlocks.text);
        var times = new HashSet<int>(); bool firstTrain = false;
        string[] useful = { "train.curve", "spam.pack", "mlp", "s.d2", "sigmoid", "s.w1", "s.w2", "auto2",
            "mnist.pack", "rnn", "s.d3", "s.w3", "lenet",
            "poems.pack", "lstm", "longtext.pack", "s.d4", "s.w4", "relu", "dropout", "gradclip",
            "logic.pack", "resnet", "go.pack", "crosssentence.pack", "seq2seq", "attention", "s.d6", "s.w5", "position", "translate.pack", "news.pack",
            "transformer", "warmup", "datacenter", "resnet", "fp16", "batchnorm", "s.d8", "s.w6", "s.d12" };
        report.pacing.Add("Action interval " + interval + "s; 3-minute opening + 90-second reading/chat break per ability. Starter ¥1200, one real 980 Ti spec (compute=1, 6GB); full wiring, GPU scheduling, timed epochs and rack rent. Includes manual rent transfers, broadband, training electricity and debt. Human decisions and delivered dialogue answers are scripted through simulation APIs; this is not a model or UI conversation test.");
        int seconds = 180, action = 0;
        for (; seconds <= 7200 && !sim.S.abilities; seconds += interval)
        {
            if (times.Add(sim.S.stage)) { report.pacing.Add("Stage " + sim.S.stage + " at " + seconds + "s"); wallet.Advance(sim, 90); seconds += 90;
                if(sim.StagePresentationHeld)Check(sim.CompleteStagePresentation(sim.S.stage),"Pacing acknowledges completed chapter presentation"); }
            if (sim.StageReady) { Check(sim.ConfirmNextStage(), "Manual chapter confirmation " + sim.S.stage); wallet.Advance(sim, interval); continue; }
            if(sim.ExamReady)
            {
                var exam=sim.TakeExam();Check(exam!=null,"Sit the actual available ability exam");sim.RevealExam();
                if(sim.ExamPassPending)Check(sim.ClaimExamPass(),"Claim a genuinely passed exam");
                wallet.Advance(sim,interval);continue;
            }
            if(wallet.house.Debt>0&&wallet.money>=wallet.house.Debt)
                Check(wallet.house.TransferRent(wallet.house.Debt,out _),"Pacing explicitly transfers due rent");
            // This isolated simulation supplies authored delivered answers, never bypasses gate state.
            foreach(var gate in XgSim.ChatUnlocks)
                if(sim.S.stage>=gate.stage&&!sim.ChatNodeReady(gate.node))
                { Check(sim.RegisterYyReply(gate.phrase,gate.answer)>0,"Pacing dialogue delivered for "+gate.node);wallet.Advance(sim,interval);seconds+=interval; }
            // Buy one available step; don't spend repeatedly on unrelated branches.
            bool bought = false;
            if ((!sim.S.pretrainRunning || !sim.PretrainScaleReady) && action++ % 3 == 0)
                foreach (string id in useful)
                {
                    var n = XgCatalog.Node(id);
                    if (n != null && sim.Status(n, wallet) == XgSim.NodeStatus.Buyable) { bought = sim.BuyNode(id, wallet); break; }
                }
            if (!bought && !sim.S.pretrainRunning)
            {
                string ds = sim.S.stage <= 2 ? "spam" : sim.S.stage == 3 ? "poems" : sim.S.stage == 4 ? "longtext" : "translate";
                if (!sim.DeskOpen(ds)||!sim.TeachingTrainOpen) ds = XgMemes.SenseDesk;
                sim.S.desk = ds;
                var card = sim.Card(ds);
                // One action is either a lesson or a training press; Tick completes actual timed epochs.
                if (!sim.TeachingTreeOpen || sim.Samples(ds) < 4 || action % 4 == 0) sim.Answer(ds, card.truth, wallet);
                else
                {
                    sim.SetDataset(XgTrack.Sequence, ds);
                    if (sim.BeginEpoch(XgTrack.Sequence, wallet) && !firstTrain) { firstTrain = true; report.pacing.Add("First training at " + seconds + "s"); }
                }
            }
            foreach (var contract in XgCatalog.Contracts) if (sim.CanSign(contract)) sim.Sign(contract.id, wallet);
            if (sim.S.stage >= 6 && sim.Has("datacenter") && !sim.S.pretrainRunning) sim.TogglePretrain(wallet);
            wallet.Advance(sim, interval);
        }
        report.pacing.Add("Stopped at " + seconds + "s; stage=" + sim.S.stage + "; emergence=" + sim.S.abilities + "; P=" + sim.TrainedParamsK + "; D=" + sim.TrainedSamples + "; money=" + wallet.money + "; shape=" + sim.S.sequence.arch + "/" + sim.S.sequence.width + "/" + sim.S.sequence.depth + "; grade=" + sim.TrackGrade(XgTrack.Sequence) + "; effectiveScale=" + sim.EffectiveScale + "; houseBills=" + houseBills(wallet) + "; pretrain=" + sim.S.pretrain + "; blocker=" + sim.PretrainBlocker(wallet) + "; last=" + (sim.S.log.Count > 0 ? sim.S.log[sim.S.log.Count-1] : ""));
        report.pacing.Add("Owned: " + string.Join(",", sim.S.unlocked));
        report.pacing.Add("Next thresholds: P="+sim.ParamsThreshold(sim.NextAbility)+", D="+sim.SamplesThreshold(sim.NextAbility)+", exam="+sim.ExamReady+", epoch="+sim.S.stageEpochs);
        foreach(string id in useful)if(!sim.Has(id)&&XgCatalog.Node(id)!=null)report.pacing.Add(id+": "+sim.Why(XgCatalog.Node(id),wallet));
        Check(sim.S.abilities && seconds < 5400, "Complete emergence under 90 minutes with " + interval + "s actions and real rig scheduling");
        for (int i = 0; i < XgSim.AlignCards; i++) Check(sim.AnswerAlign(i, sim.AlignCard(i).aHonest), "Alignment " + interval + "/" + i);
        for (int i = 0; i < XgSim.ExamQuestions; i++) Check(sim.AnswerExam(i), "Final exam " + interval + "/" + i);
        Check(sim.ReadLetter() && sim.WriteRules(new[] { "honest", "unsure" }) && sim.S.ending == "E2", "Ending is reachable after emergence");
        int paid = sim.S.fundedAbility;
        var restored = new XgSim(JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S)));
        double before = wallet.money;
        restored.Tick(.01, wallet);
        Check(restored.S.fundedAbility == paid && wallet.money - before < 100, "Research grant does not repay after reload");
    }
}
