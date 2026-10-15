using System;
using System.Linq;
using System.Text.Json;
using LingGuangV05.XingGuang;

static class Program
{
    static int assertions;
    static readonly JsonSerializerOptions Json = new JsonSerializerOptions { IncludeFields = true, IgnoreReadOnlyProperties = true };
    static void Check(bool ok, string why)
    {
        if (!ok) throw new Exception(why);
        assertions++;
        Console.WriteLine("PASS " + why);
    }
    static XgSim Reload(XgSim sim) => new XgSim(JsonSerializer.Deserialize<XgState>(JsonSerializer.Serialize(sim.S, Json), Json));

    static void Main()
    {
        var wallet = new Wallet();
        var fresh = new XgSim();
        Check(fresh.S.stage == 1 && fresh.S.desk == XgMemes.SenseDesk && fresh.S.sequence.dataset == XgMemes.SenseDesk,
            "Fresh simulation chooses common sense for the desk and its sequence training data");
        Check(fresh.DeskOpen("sense") && fresh.DeskOpen("logic") && fresh.DeskOpen("spam")
            && !fresh.DeskOpen("arith") && !fresh.ProgressionDeskAvailable("arith"),
            "Only the established non-arithmetic stage-one desks open");
        fresh.SelectDesk("arith"); fresh.Tick(.1, wallet);
        Check(fresh.S.desk == "sense" && !fresh.DeskOpen("arith"), "Selection and normal Tick cannot bypass the arithmetic gate");
        fresh.English = true;
        Check(fresh.DeskConditionText(XgCatalog.Desk("arith")).StartsWith("Opens by itself", StringComparison.Ordinal),
            "Locked arithmetic guidance names its automatic ability unlock");
        var reloaded = Reload(fresh);
        Check(reloaded.S.desk == "sense" && !reloaded.DeskOpen("arith") && reloaded.S.stage == 1,
            "Repair after an in-memory save round trip cannot reopen arithmetic early");

        var natural = fresh.Card(fresh.S.desk);
        Check(natural.dataset == "sense" && XgMemes.SenseBank.Any(p => p.text == natural.question && p.textEn == natural.questionEn && p.yes == natural.truth),
            "Natural first card comes unchanged from the real bilingual common-sense bank");
        Check(fresh.TeachingGuess(natural, out bool guess), "Real default first card has an untrained prediction");
        bool truth = natural.truth; natural.truth = !truth;
        Check(fresh.TeachingGuess(natural, out bool paired) && paired == guess, "Common-sense guess does not read ground truth");
        natural.truth = truth;
        int taught = 0; fresh.LessonTaught += (_, _) => taught++;
        Check(fresh.TryEvaluateTeachingGuess("sense", natural.id, guess, guess == truth, wallet, out var answer)
            && answer.correct && taught == 1 && fresh.Labels("sense") == 1,
            "An accurate judgment on the natural first card teaches exactly once");

        var known = new XgSim();
        var phrase = XgMemes.Pick(XgMemes.SenseDesk, new Random(9137), 1, known.Today, known.Topic, false);
        Check(phrase.yes && phrase.text == "北京是中国的首都。" && phrase.textEn == "Beijing is the capital of China.",
            "Seed 9137 selects a real true common-sense phrase without changing its answer");
        var card = new XgCard { id = 91370001, dataset = "sense", track = (int)XgTrack.Sequence, seed = 9137,
            truth = phrase.yes, level = 1, question = phrase.text, questionEn = phrase.textEn,
            why = phrase.why, whyEn = phrase.whyEn, source = phrase.source };
        var features = XgBoardData.FromCard(card);
        Check(features.region == "sequence" && features.features.Count > 0 && features.features.All(f => card.question.Contains(f.name)),
            "English correction fixture uses genuine common-sense word features");
        Console.WriteLine("Sense features: " + string.Join(", ", features.features.Select(f => f.name)));
        known.S.cards.RemoveAll(c => c.dataset == "sense"); known.S.cards.Add(card);
        Check(known.S.desk == "sense" && known.TeachingGuess(card, out bool wrong) && !wrong,
            "The real untrained common-sense model predicts No on the true deterministic card");
        taught = 0; known.LessonTaught += (_, _) => taught++;
        Check(known.TryEvaluateTeachingGuess("sense", card.id, false, false, wallet, out answer)
            && answer.correct && answer.corrected && taught == 1 && known.S.teachingCorrections == 1 && known.Labels("sense") == 1,
            "Default common-sense correction is one real successful settlement");
        Check(known.S.recentCorrections.Count == 1 && known.S.recentCorrections[0].dataset == "sense"
            && known.S.recentCorrections[0].truth && known.Card("sense").id != card.id,
            "Common-sense learning preserves truth and advances the actual card");

        int opened = 0; reloaded.DeskOpened += desk => { if (desk.id == "arith") opened++; };
        reloaded.S.stage = 2; reloaded.Tick(.1, wallet);
        Check(reloaded.DeskOpen("arith") && reloaded.ProgressionDeskAvailable("arith") && opened == 1
            && !reloaded.Has("arith.pack") && !reloaded.Owns("arith"), "Arithmetic opens at stage two without purchasing a pack");
        reloaded.Tick(.1, wallet);
        Check(opened == 1 && reloaded.S.desk == "sense", "Unlock is announced once and does not redirect the player's selected desk");
        reloaded.SelectDesk("arith");
        Check(reloaded.S.desk == "arith" && reloaded.Card("arith").dataset == "arith", "Stage-two arithmetic is actually selectable");

        foreach (string selected in new[] { "spam", "arith" })
        {
            var legacy = new XgSim();
            legacy.S.desksOpen.Add("arith"); legacy.S.sequence.dataset = "spam"; legacy.SelectDesk(selected);
            legacy.S.labels.Add(new XgLabelCount { dataset = "arith", count = 7 });
            var existing = legacy.Card(selected);
            var loaded = Reload(legacy);
            Check(loaded.S.stage == 1 && loaded.S.desk == selected && loaded.S.sequence.dataset == "spam"
                && loaded.DeskOpen("arith") && loaded.ProgressionDeskAvailable("arith") && loaded.Labels("arith") == 7
                && loaded.Card(selected).id == existing.id && loaded.Card(selected).question == existing.question,
                "Existing stage-one save keeps desk, training selection, arithmetic unlock, labels and current card: " + selected);
            Check(loaded.DeskConditionText(XgCatalog.Desk("arith")) == "", "Grandfathered arithmetic is not described as locked: " + selected);
            loaded = Reload(loaded);
            Check(loaded.S.desk == selected && loaded.Labels("arith") == 7 && loaded.DeskOpen("arith"),
                "Repeated load preserves old progress without regranting or resetting it: " + selected);
        }
        Console.WriteLine("SENSE_START_CORE_CHECKS_PASS assertions=" + assertions);
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
