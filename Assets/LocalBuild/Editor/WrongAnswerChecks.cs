using System;
using LingGuangV05.XingGuang;

// Pure simulation checks: run from Unity or Tests/WrongAnswerPenalty without the editor.
public static class WrongAnswerChecks
{
    sealed class Wallet : IXgHost
    {
        public double money = 1000;
        public double Money => money;
        public double Compute => 1;
        public double VramMB => 6144;
        public string Blocker => null;
        public bool Spend(double amount) { if (amount < 0 || amount > money) return false; money -= amount; return true; }
        public void Earn(double amount) { money += amount; }
        public void Train(double seconds) { }
    }

    static int count;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; }
    static bool Equal(double a, double b) => Math.Abs(a - b) < 1e-8;
    static XgSim Fixture(string desk, bool teaching, bool queued, out XgCard card)
    {
        var sim = new XgSim();
        sim.GoldChance = 0;
        sim.S.teachMode = teaching;
        card = sim.Card(desk);
        card.family = false; card.kind = ""; card.law = false;
        card.gold = false; card.bounty = false; card.timeLimit = 0; card.age = 0;
        card.truth = true;
        if (queued) sim.S.queue.Add(card);
        return sim;
    }

    public static void Run()
    {
        count = 0;
        // Paired fresh states compare actual correct/incorrect settlement, not the pricing helper.
        foreach (var desk in XgCatalog.Desks)
        foreach (bool teaching in new[] { false, true })
        foreach (bool queued in new[] { false, true })
        {
            var right = Fixture(desk.id, teaching, queued, out var goodCard);
            var wrong = Fixture(desk.id, teaching, queued, out var badCard);
            right.S.combo = wrong.S.combo = 7;
            right.S.payRaise = wrong.S.payRaise = 1;
            goodCard.level = badCard.level = 2;
            goodCard.gold = badCard.gold = true;
            goodCard.bounty = badCard.bounty = true;
            goodCard.trick = badCard.trick = true;
            var paid = new Wallet(); var fined = new Wallet();
            double samples = wrong.Labels(desk.id);
            double quote = wrong.AnswerPayFor(badCard, queued);
            Check(wrong.S.combo == 7 && wrong.S.handFines == 0 && Equal(wrong.Labels(desk.id), samples), "Quote does not settle the card");
            var win = queued ? right.AnswerQueued(goodCard.id, true, paid) : right.Answer(desk.id, true, paid);
            var loss = queued ? wrong.AnswerQueued(badCard.id, false, fined) : wrong.Answer(desk.id, false, fined);
            string label = desk.id + " teaching=" + teaching + " queued=" + queued;
            Check(win.correct && !loss.correct && win.pay > 0 && Equal(win.pay, loss.fine), "Symmetric reward/fine: " + label);
            Check(Equal(quote, win.pay), "Displayed reward equals settlement: " + label);
            Check(Equal(1000 - fined.Money, loss.fine) && Equal(wrong.S.handFines, loss.fine)
                && Equal(wrong.S.totalSpent, loss.fine) && Equal(wrong.DayIncome, -loss.fine), "Wallet and counters: " + label);
            Check(wrong.S.combo == 0 && loss.samples == 0 && Equal(wrong.Labels(desk.id), samples), "No rewards on wrong answer: " + label);
            if (queued) {
                Check(!wrong.AnswerQueued(badCard.id, false, fined).accepted && Equal(fined.Money, 1000 - loss.fine), "No duplicate queue charge");
            }
        }

        // Arithmetic: base 0.5 x hand 2.5 x next combo 1.05 = 1.3125, rather than the old half-base fine.
        var arithmetic = Fixture("arith", false, false, out var arithmeticCard);
        arithmeticCard.level = 1;
        Check(Equal(arithmetic.Answer("arith", false, new Wallet()).fine, 1.3125), "Arithmetic uses full potential reward");
        var bonus = Fixture("arith", false, false, out var bonusCard);
        bonusCard.level = 2; bonusCard.gold = true; bonus.S.combo = 9; bonus.S.payRaise = 1;
        // 0.5 x 2.5 x difficulty 1.5 x raise 2 x next combo 1.5 x gold 3 = 16.875.
        Check(Equal(bonus.AnswerPayFor(bonusCard), 16.875), "Difficulty, raise, next combo and gold quote");
        Check(Equal(bonus.Answer("arith", false, new Wallet()).fine, 16.875), "Bonuses are captured before combo resets");
        foreach (bool recheck in new[] { false, true }) {
            var review = Fixture("arith", false, true, out var reviewCard);
            reviewCard.level = 1; reviewCard.trick = false;
            reviewCard.hasJudgment = true; reviewCard.guess = false; reviewCard.recheck = recheck;
            review.S.unlocked.Add("label.hard");
            double expected = recheck ? 1.375 : 2.0625; // next combo +2, hard-case x1.5 only on new reviews
            Check(Equal(review.AnswerQueued(reviewCard.id, false, new Wallet()).fine, expected), "Review correction and hard-case bonus");
        }
        foreach (double balance in new[] { 0.0, .25, -3.0 }) {
            var sim = Fixture("arith", false, false, out _);
            var host = new Wallet { money = balance };
            var result = sim.Answer("arith", false, host);
            Check(Equal(result.fine, Math.Max(0, balance)) && Equal(host.money, Math.Min(0, balance)), "Fine capped to available balance");
        }
        var timeout = Fixture("arith", false, false, out var late);
        late.timeLimit = 1; late.age = 2;
        var lateResult = timeout.Answer("arith", true, new Wallet());
        Check(lateResult.timeout && lateResult.fine == 0, "Timeout is not a wrong-answer fine");
        var family = Fixture("spam", true, false, out var opinion);
        opinion.family = true; opinion.from = "妈";
        var familyResult = family.Answer("spam", false, new Wallet());
        Check(familyResult.subjective && familyResult.fine == 0 && family.S.handFines == 0, "Subjective choices are not fined");
        var chat = new XgSim(); var chatWallet = new Wallet();
        double chatPay = chat.ChatLabel("spam", true, new Wallet());
        double chatLoss = chat.ChatLabel("spam", false, chatWallet);
        Check(chatLoss < 0 && Equal(-chatLoss, chatPay) && Equal(1000 - chatWallet.money, chatPay), "Chat quiz reports full deduction");
        Console.WriteLine("WRONG_ANSWER_CHECKS_PASS " + count);
    }
}
