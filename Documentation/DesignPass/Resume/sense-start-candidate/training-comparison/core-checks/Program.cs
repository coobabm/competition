using System;
using System.Linq;
using System.Text.Json;
using LingGuangV05.XingGuang;

static class Program
{
    static int assertions;
    static readonly JsonSerializerOptions Json = new JsonSerializerOptions { IncludeFields = true, IgnoreReadOnlyProperties = true };
    static string Save(XgSim sim) => JsonSerializer.Serialize(sim.S, Json);
    static XgSim Reload(XgSim sim) => new XgSim(JsonSerializer.Deserialize<XgState>(Save(sim), Json));
    static void Check(bool ok, string why)
    {
        if (!ok) throw new Exception(why);
        assertions++;
        Console.WriteLine("PASS " + why);
    }
    static XgCheckup LatestFixed(XgSim sim, string dataset)
        => sim.S.checkups.Last(c => c.dataset == dataset && !c.taught);
    static bool Predict(XgSim sim, XgBoardCard card, string dataset)
        => sim.Board.Predict(card, sim.Knobs(sim.Run(XgCatalog.Dataset(dataset).track)), out _);
    static void TeachFour(XgSim sim, string dataset, Wallet wallet, bool generatedFixture = false)
    {
        sim.SelectDesk(dataset);
        for (int i = 0; sim.Labels(dataset) < 4 && i < 5; i++)
        {
            var card = generatedFixture ? sim.CreateLabelCard(dataset, false) : sim.Card(dataset);
            if (generatedFixture) { sim.S.cards.RemoveAll(c => c.dataset == dataset); sim.S.cards.Add(card); }
            if (!sim.TeachingGuess(card, out bool guess)
                || !sim.TryEvaluateTeachingGuess(dataset, card.id, guess, guess == card.truth, wallet, out var result)
                || !result.correct) throw new Exception("Natural teaching failed: " + dataset);
        }
        Check(sim.Labels(dataset) == 4, "Four real teaching judgments on " + (generatedFixture ? "production-generated fixture" : "natural") + " cards supply the training samples: " + dataset);
    }

    static int Main(string[] args)
    {
        try
        {
            // Exercise the failure path without changing game data or running any checks.
            if (args.Length == 1 && args[0] == "--verify-error-handling")
                throw new InvalidOperationException("Intentional check of the diagnostic failure handler.");
            RunChecks();
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("TRAINING_COMPARISON_CORE_CHECKS_FAIL");
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    static void RunChecks()
    {
        var wallet = new Wallet();
        var sim = new XgSim();
        string untouched = Save(sim);
        Check(sim.CheckupText(dataset: "sense").Contains("教它 4 张卡") && Save(sim) == untouched,
            "Reading an empty sense comparison gives guidance without creating records or changing state");
        var fixedSense = XgBoardData.Make("sense", 9137, 1, 20160524);
        Check(fixedSense.source != null && fixedSense.source.dataset == "sense"
            && XgMemes.SenseBank.Any(p => p.text == fixedSense.source.question && p.yes == fixedSense.truth),
            "Deterministic fixed sense sample comes from the real authored sense bank");
        Check(fixedSense.features.Count > 0 && fixedSense.features.All(f => fixedSense.source.question.Contains(f.name)),
            "Fixed sense question has its production word features");
        bool expected = Predict(sim, fixedSense, "sense");
        sim.TeachingGuess(sim.Card("sense"), out _);
        var baseline = LatestFixed(sim, "sense");
        Check(sim.S.checkups.Count == 1 && baseline.assessment == 0
            && baseline.question == fixedSense.source.question && baseline.answer == (expected ? "是" : "否")
            && baseline.correct == (expected == fixedSense.truth),
            "Default lesson stores its actual pre-training sense prediction with the matching question and truth check");
        string baselineText = sim.CheckupText(dataset: "sense");
        Check(baselineText.Contains(baseline.question) && baselineText.Contains(" → " + baseline.answer)
            && !baselineText.Contains("中奖"), "Default comparison renders the sense question and model answer without an SMS question");
        untouched = Save(sim);
        for (int i = 0; i < 5; i++) Check(sim.CheckupText(dataset: "sense") == baselineText, "Repeated comparison read is stable: " + i);
        Check(Save(sim) == untouched, "Repeated comparison reads do not mutate saved learning or history");
        sim.TeachingGuess(sim.Card("sense"), out _);
        Check(sim.S.checkups.Count == 1, "Repeated sense predictions do not duplicate the fixed baseline");
        bool truth = fixedSense.truth;
        fixedSense.truth = !truth;
        Check(Predict(sim, fixedSense, "sense") == expected, "The fixed sense model prediction does not read the answer key");
        fixedSense.truth = truth;
        Console.WriteLine("FIXED_SENSE question=" + baseline.question + " answer=" + baseline.answer + " truth=" + truth);

        TeachFour(sim, "sense", wallet);
        Check(sim.SetDataset(XgTrack.Sequence, "sense") || sim.S.sequence.dataset == "sense", "Training remains on sense after the natural lesson");
        Check(sim.TrainEpoch(XgTrack.Sequence, wallet) != null, "Real sense epoch completes and assesses its model");
        var assessed = LatestFixed(sim, "sense");
        Check(assessed.assessment == sim.S.assessments && assessed.assessment > 0 && assessed.question == baseline.question
            && assessed.answer == (Predict(sim, fixedSense, "sense") ? "是" : "否"),
            "Sense assessment reuses the exact baseline question with a fresh real model prediction");
        Check(sim.S.checkups.Where(c => c.assessment == sim.S.assessments).All(c => c.dataset == "sense"),
            "Sense assessment stores only matching fixed and taught questions");

        string senseBeforeSwitch = sim.CheckupText(dataset: "sense");
        sim.TeachingGuess(sim.CreateLabelCard("spam", false), out _);
        Check(LatestFixed(sim, "spam").question.Contains("中奖") && sim.CheckupText(dataset: "spam").Contains("中奖"),
            "Entering the SMS lesson preserves its established fixed scam-question regression");
        TeachFour(sim, "spam", wallet, true);
        Check(sim.SetDataset(XgTrack.Sequence, "spam"), "Select a different real training dataset");
        Check(sim.TrainEpoch(XgTrack.Sequence, wallet) != null, "Real SMS epoch completes after the common-sense epoch");
        var sms = LatestFixed(sim, "spam");
        Check(sms.assessment == sim.S.assessments && sms.question.Contains("中奖")
            && sim.S.checkups.Where(c => c.assessment == sms.assessment).All(c => c.dataset == "spam"),
            "SMS assessment retains the SMS fixed question and does not mix sense corrections");
        Check(sim.CheckupText(dataset: "sense") == senseBeforeSwitch && !sim.CheckupText(dataset: "sense").Contains("中奖"),
            "Returning to sense shows its own latest comparison even when another dataset was assessed later");
        Check(sim.CheckupText(dataset: "spam").Contains(sms.question)
            && !sim.CheckupText(dataset: "spam").Contains(baseline.question), "SMS comparison excludes the sense fixed question");
        string history = sim.CheckupText(true);
        Check(history.Contains(baseline.question) && history.Contains(sms.question), "Unfiltered board history still includes both real datasets");

        sim.S.stage = 2;
        sim.Today = 20170101;
        Check(sim.SetDataset(XgTrack.Sequence, "sense"), "Return the stage-two boundary fixture to sense");
        Check(sim.Assess(XgTrack.Sequence, wallet)?.dataset == "sense", "The actual assessed dataset remains sense across the stage boundary");
        Check(LatestFixed(sim, "sense").question == baseline.question && !sim.CheckupText(dataset: "sense").Contains("手写数字"),
            "Stage and calendar changes cannot replace the fixed sense question with the former stage-two digit question");
        sim.S.stage = 1;
        var restored = Reload(sim);
        Check(JsonSerializer.Serialize(restored.S.checkups, Json) == JsonSerializer.Serialize(sim.S.checkups, Json),
            "An in-memory save round trip preserves old and new checkup records without migration");
        Check(restored.CheckupText(dataset: "sense") == sim.CheckupText(dataset: "sense")
            && restored.CheckupText(true) == sim.CheckupText(true), "Reload preserves both the selected comparison and mixed-dataset history");

        var old = new XgSim();
        old.S.checkups.Add(new XgCheckup { dataset = "spam", question = "这条短信是诈骗吗：恭喜您中奖，请点击领取", answer = "否", assessment = 7 });
        old.S.assessments = 7;
        untouched = Save(old);
        Check(old.CheckupText(dataset: "sense").Contains("教它 4 张卡") && Save(old) == untouched,
            "An old save with only SMS history displays guidance for unseen sense without rewriting history");
        string oldHistory = JsonSerializer.Serialize(old.S.checkups[0], Json);
        old.TeachingGuess(old.Card("sense"), out _);
        Check(old.S.checkups.Count == 2 && LatestFixed(old, "sense").question == baseline.question
            && JsonSerializer.Serialize(old.S.checkups[0], Json) == oldHistory,
            "Teaching sense in an old save adds a genuine sense comparison while preserving its existing SMS record");
        untouched = Save(old);
        old.CheckupText(dataset: "sense"); old.CheckupText(dataset: "spam"); old.CheckupText(true);
        Check(Save(old) == untouched, "Switching comparison filters never changes saved state");

        var full = new XgSim();
        for (int i = 0; i < XgSim.CheckupMemory; i++)
            full.S.checkups.Add(new XgCheckup { dataset = "spam", question = "old SMS " + i, answer = "否", assessment = i });
        full.S.assessments = XgSim.CheckupMemory;
        full.TeachingGuess(full.Card("sense"), out _);
        Check(full.S.checkups.Count == XgSim.CheckupMemory && full.S.checkups[0].question == "old SMS 1"
            && LatestFixed(full, "sense").question == baseline.question,
            "Adding a first sense comparison to full history preserves the existing 160-record limit and newest history");

        var regression = new XgSim();
        var scam = new XgCard { dataset = "spam", seed = 9137, question = "恭喜您中奖，请点击领取", truth = true, level = 1 };
        regression.SelectDesk("spam");
        Check(regression.TeachingGuess(scam, out bool untrained) && !untrained,
            "Established real scam fixture starts with an incorrect model prediction and its unchanged true answer");
        for (int i = 0; i < 5; i++)
        {
            scam.id = 9000 + i;
            regression.S.cards.RemoveAll(c => c.dataset == "spam"); regression.S.cards.Add(scam);
            if (!regression.Answer("spam", scam.truth, wallet).correct) throw new Exception("SMS regression lesson failed");
        }
        Check(regression.TeachingGuess(scam, out bool learned) && learned && regression.S.recentCorrections.Count > 0,
            "Existing repeated SMS corrections still teach the actual concept");
        regression.SetDataset(XgTrack.Sequence, "spam");
        Check(regression.TrainEpoch(XgTrack.Sequence, wallet) != null
            && regression.S.checkups.Any(c => c.taught && c.dataset == "spam")
            && LatestFixed(regression, "spam").question.Contains("中奖"),
            "The established SMS epoch records both its learned correction and original fixed scam question");
        Console.WriteLine("TRAINING_COMPARISON_CORE_CHECKS_PASS assertions=" + assertions);
    }

    sealed class Wallet : IXgHost
    {
        public double Money { get; private set; } = 10000;
        public double Compute => 1;
        public double VramMB => 6144;
        public string Blocker => null;
        public bool Spend(double value) { if (value < 0 || Money < value) return false; Money -= value; return true; }
        public void Earn(double value) { Money += value; }
        public void Train(double seconds) { }
    }
}
