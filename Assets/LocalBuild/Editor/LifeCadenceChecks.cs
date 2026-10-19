using System;
using System.Collections.Generic;
using System.Reflection;
using LingGuangV05.XingGuang;

/// <summary>Real simulation and wallet checks; callable from an isolated Editor wrapper or the standalone .NET runner.</summary>
public static class LifeCadenceChecks
{
    sealed class Wallet : IXgHost
    {
        public double money = 500, spent, earned;
        public int spendCalls, earnCalls;
        public bool rejectSpend;
        public double Money => money;
        public double Compute => 1;
        public double VramMB => 16000;
        public string Blocker => null;
        public bool Spend(double amount)
        {
            spendCalls++;
            if (rejectSpend || amount < 0 || amount > money) return false;
            money -= amount; spent += amount; return true;
        }
        public void Earn(double amount) { money += amount; earned += amount; earnCalls++; }
        public void Train(double seconds) { }
    }
    static int checks;
    static Func<XgState, XgState> clone;
    static readonly MethodInfo TickLife = typeof(XgSim).GetMethod("TickLifeEvents", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly string Fixture = @"{""events"":[
      {""id"":""e901"",""stage"":[1,6],""who"":""测试甲"",""whoEn"":""Fixture A"",""title"":""付款"",""titleEn"":""Payment"",""body"":""选择"",""bodyEn"":""Choose"",""choices"":[{""label"":""支付"",""labelEn"":""Pay"",""effects"":{""money"":-10},""result"":""已支付十元"",""resultEn"":""Paid ten yuan""},{""label"":""不付"",""labelEn"":""Decline"",""effects"":{},""result"":""未支付"",""resultEn"":""No payment""}]},
      {""id"":""e902"",""stage"":[1,6],""who"":""测试乙"",""whoEn"":""Fixture B"",""title"":""收入"",""titleEn"":""Income"",""body"":""选择"",""bodyEn"":""Choose"",""choices"":[{""label"":""收到"",""labelEn"":""Receive"",""effects"":{""money"":7},""result"":""已收到七元"",""resultEn"":""Received seven yuan""},{""label"":""不收"",""labelEn"":""Decline"",""effects"":{},""result"":""未收到"",""resultEn"":""No income""}]},
      {""id"":""e903"",""stage"":[2,6],""requires"":""ready"",""minDate"":20160701,""who"":""测试丙"",""whoEn"":""Fixture C"",""title"":""后续"",""titleEn"":""Follow-up"",""body"":""选择"",""bodyEn"":""Choose"",""choices"":[{""label"":""收到"",""labelEn"":""Receive"",""effects"":{},""result"":""已完成"",""resultEn"":""Completed""},{""label"":""离开"",""labelEn"":""Leave"",""effects"":{},""result"":""已离开"",""resultEn"":""Left""}]}
    ]}";

    public static int Run(string productionJson, Func<XgState, XgState> roundTrip)
    {
        checks = 0; clone = roundTrip;
        Need(TickLife != null && clone != null, "real life tick and state round-trip are available");
        try
        {
            XgSim.LoadLifeData(Fixture);
            FirstAndResultClock(); ModesAndHolds(); MigrationAndCycle(); WalletAndPersistence();
            XgSim.LoadLifeData(productionJson);
            ProductionDataSmoke();
            return checks;
        }
        finally { XgSim.LoadLifeData(productionJson); }
    }

    static XgSim Sim(bool curve = true)
    {
        var s = new XgSim();
        s.S.teachingFlow = false; s.S.openingFocus = false;
        s.S.stage = s.S.stageVision = s.S.stageSequence = 1;
        if (curve && !s.Has("train.curve")) s.S.unlocked.Add("train.curve");
        s.Today = 20160601;
        _ = s.LifeEventNext;
        return s;
    }
    static XgSim Reload(XgSim s)
    {
        var copy = new XgSim(clone(s.S)) { English = s.English, Today = s.Today };
        _ = copy.LifeEventNext; return copy;
    }
    static void Tick(XgSim s, Wallet w, double seconds)
    {
        TickLife.Invoke(s, new object[] { seconds, w });
    }
    static void Need(bool ok, string why) { if (!ok) throw new InvalidOperationException("Life cadence: " + why); checks++; }
    static void Equal(double actual, double expected, string why) => Need(Math.Abs(actual - expected) < 1e-7, why + " (" + actual + " vs " + expected + ")");
    static void Answer(XgSim s, Wallet w, int index = 1)
    {
        Need(s.AnswerLifeEvent(index, w), "answer commits once");
        Need(s.LifeEventResultPending && !s.LifeEventOpen, "answer opens saved result instead of another choice");
        Need(s.AcknowledgeLifeEventResult(), "first result acknowledgement succeeds");
    }

    static void FirstAndResultClock()
    {
        var opening = new XgSim { GoldChance = 0 }; var openingWallet = new Wallet();
        opening.BeginOpening();
        Need(opening.OpeningFocus && !opening.TeachingTreeOpen, "real BeginOpening starts before technology access");
        for (int i = 0; i < 12; i++) opening.Answer(XgMemes.SenseDesk, opening.Card(XgMemes.SenseDesk).truth, openingWallet);
        Need(opening.TeachingTreeOpen && opening.OpeningFocus, "twelve real answers open technology while opening focus remains");
        Need(opening.BuyNode("train.curve", openingWallet), "real opening flow buys training curve");
        Need(opening.OpeningFocus && opening.LifeEventsUnlocked, "owned curve starts events without requiring accuracy or next ability");
        Tick(opening, openingWallet, 479); Need(!opening.LifeEventOpen, "real opening first event waits full interval");
        Tick(opening, openingWallet, 1); Need(opening.LifeEventOpen, "real opening first event arrives after curve plus eight minutes");

        var s = Sim(false); var w = new Wallet();
        Tick(s, w, 900); Equal(s.LifeEventNext, 480, "no draw time before train.curve");
        Need(!s.LifeEventOpen && !s.S.lifeCadenceStarted, "training curve is the first-event gate");
        Need(s.BuyNode("train.curve", w), "actual training-curve purchase succeeds");
        Equal(s.LifeEventNext, 480, "purchase starts a full eight-minute interval");
        Tick(s, w, 479.5); Need(!s.LifeEventOpen, "first event waits until full interval");
        Equal(s.LifeEventNext, .5, "fractional remaining time retained");
        Tick(s, w, .5); Need(s.LifeEventOpen, "first event opens at 480 active seconds");
        long rng = s.S.lifeRng; var time = s.LifeEventNext;
        Tick(s, w, 900); Equal(s.LifeEventNext, time, "open question does not consume next interval");
        Need(s.S.lifeRng == rng, "waiting for an answer never rerolls");
        Need(s.AnswerLifeEvent(1, w), "choice produces result");
        Need(s.LifeEventResultPending && s.LifeEventResultView() != null, "result is available independently from event choices");
        Tick(s, w, 900); Equal(s.LifeEventNext, time, "result remains untimed until explicitly closed");
        Need(!s.OpenLifeEventNow("e901", w, true), "pending result cannot be overwritten by forced event");
        Need(s.AcknowledgeLifeEventResult(), "closing result succeeds");
        Equal(s.LifeEventNext, 480, "next eight minutes start at result close");
        Tick(s, w, 7); Need(!s.AcknowledgeLifeEventResult(), "duplicate close is rejected");
        Equal(s.LifeEventNext, 473, "duplicate close does not restart time");
        Tick(s, w, 472.9); Need(!s.LifeEventOpen, "next event waits after result close");
        Tick(s, w, .1); Need(s.LifeEventOpen, "next event arrives after full close-based interval");

        // At least one route exercises the public full simulation tick, rather than only its timer subsystem.
        s = Sim(); w = new Wallet(); s.WindowOpen = false;
        var life = s.S.lifeClock; var next = s.LifeEventNext;
        s.Tick(3, w);
        Equal(s.S.lifeClock, life + 3, "public simulation tick advances life clock");
        Equal(s.LifeEventNext, next - 3, "internal app closed does not pause cadence in public tick");
    }

    static void ModesAndHolds()
    {
        var s = Sim(); var w = new Wallet();
        s.LifeEventIntervalMode = 1;
        Equal(s.LifeEventNext, 900, "fifteen-minute setting starts full interval");
        Tick(s, w, 8); s = Reload(s);
        Need(s.LifeEventIntervalMode == 1, "fifteen-minute mode survives state round-trip");
        Equal(s.LifeEventNext, 892, "fifteen-minute remaining time survives reload");
        s.LifeEventIntervalMode = 1; Equal(s.LifeEventNext, 892, "same setting does not restart timer");
        s.LifeEventIntervalMode = 2; s = Reload(s); Tick(s, w, 3000);
        Need(s.LifeEventIntervalMode == 2 && !s.LifeEventOpen, "off setting survives reload and suppresses automatic draws");
        s.LifeEventIntervalMode = 0; Equal(s.LifeEventNext, 480, "re-enabling starts full selected interval");

        Tick(s, w, 10); var before = s.LifeEventNext; var clock = s.S.lifeClock;
        s.LifeEventActive = false; Tick(s, w, 50);
        Equal(s.LifeEventNext, before, "host pause or native minimization freezes only draw clock");
        Equal(s.S.lifeClock, clock + 50, "lifeClock remains independent of play-time gate");
        s.LifeEventActive = true; s.LifeEventConflict = true; Tick(s, w, 70);
        Equal(s.LifeEventNext, before, "script or reader conflict freezes remaining draw time");
        s.LifeEventConflict = false; Tick(s, w, 1);
        Equal(s.LifeEventNext, before - 1, "ordinary conflict resumes remaining time");
        s.S.lifeNext = 0; s.LifeEventConflict = true; Tick(s, w, 5);
        Need(s.S.lifeCadenceDeferred && !s.LifeEventOpen, "due conflict records deferred draw");
        s = Reload(s); Need(s.S.lifeCadenceDeferred, "deferred state survives reload");
        s.LifeEventActive = false; Tick(s, w, 50);
        Need(s.S.lifeCadenceDeferred, "inactive host does not start conflict grace");
        s.LifeEventActive = true; Tick(s, w, 1);
        Equal(s.LifeEventNext, 30, "conflict ends into a full thirty-second grace");
        s.LifeEventActive = false; Tick(s, w, 40); Equal(s.LifeEventNext, 30, "minimization does not consume grace");
        s.LifeEventActive = true; Tick(s, w, 29.9); Need(!s.LifeEventOpen, "grace has not ended early");
        Tick(s, w, .1); Need(s.LifeEventOpen, "event opens after full grace");
        Answer(s, w);
        s.LifeEventHold = () => true; before = s.LifeEventNext; Tick(s, w, 40);
        Equal(s.LifeEventNext, before, "existing presentation hold still pauses draw time");

        s = Sim(); s.S.lifeNext = 0; s.QueueStagePresentation(2);
        Need(s.StagePresentationHeld, "stage fixture represents Controller's skipped-Tick branch");
        clock = s.S.lifeClock; s.ObserveLifeEventConflict();
        Need(s.S.lifeCadenceDeferred && !s.LifeEventOpen, "observation detects due stage conflict without any Sim.Tick");
        Equal(s.S.lifeClock, clock, "conflict observation never advances economic clock");
        Equal(s.LifeEventNext, 0, "conflict observation never consumes or resets countdown");
        s = Reload(s); Need(s.S.lifeCadenceDeferred, "due stage conflict survives save/load before Tick resumes");
        s.CompleteStagePresentation(2); Tick(s, w, 1);
        Equal(s.LifeEventNext, 30, "stage skip-Tick recovery receives full thirty seconds");
        Tick(s, w, 30); Need(s.LifeEventOpen, "deferred stage event opens after grace");
    }

    static void MigrationAndCycle()
    {
        var s = Sim(); var w = new Wallet();
        s.S.lifeVersion = XgSim.LifeSchema; s.S.lifeCadenceVersion = 0; s.S.lifeRng = 987654321;
        s.S.lifeNext = 2; s.S.lifeDrawCount = 2; s.S.lifeDrawStage = 1;
        s.S.lifeDataSeen.Add("e901"); s.S.lifeFlags.Add("kept");
        s.S.lifeHistory.Add(new XgLifeRecord { id = "e901", choice = "1", at = 5 });
        s.S.lifeReturns.Add(new XgLifeTimer { id = "zhou", amount = 25, until = s.S.lifeClock + 10 });
        s = Reload(s);
        Need(s.LifeEventIntervalMode == 0 && s.S.lifeCadenceVersion == XgSim.LifeCadenceSchema, "legacy save migrates to enabled eight-minute cadence");
        Equal(s.LifeEventNext, 480, "obsolete stage/random countdown migrates once");
        Need(s.S.lifeRng == 987654321 && s.S.lifeFlags.Contains("kept") && s.S.lifeHistory.Count == 1,
            "migration preserves RNG, flags and history");
        Need(s.S.lifeReturns.Count == 1 && s.S.lifeDataSeen.Contains("e901") && s.S.lifeCycleSeen.Contains("e901"),
            "migration preserves repayments and permanent seen, initializes cycle");
        Tick(s, w, 5); long rng = s.S.lifeRng; var next = s.LifeEventNext;
        s.S.stage = 2; _ = s.LifeEventsEligible(w);
        Equal(s.LifeEventNext, next, "stage change does not reset remaining time");
        Need(s.S.lifeRng == rng, "stage change does not consume event RNG");
        Need(s.LifeEventsEligible(w).Count == 1 && s.LifeEventsEligible(w)[0] == "e902", "unseen eligible event precedes repeats despite legacy two-event count");
        Need(s.OpenLifeEventNow("e902", w), "third event of stage is allowed"); Answer(s, w);
        var pool = s.LifeEventsEligible(w);
        Need(pool.Contains("e901") && pool.Contains("e902") && !pool.Contains("e903"), "exhausted current eligible cycle reopens only currently valid events");
        Need(s.OpenLifeEventNow("e901", w), "next cycle can repeat eligible event"); Answer(s, w);
        Need(s.S.lifeDataSeen.Contains("e902"), "cycle reset never erases permanent seen");
        pool = s.LifeEventsEligible(w);
        Need(pool.Count == 1 && pool[0] == "e902", "already drawn current-cycle row cannot repeat early");
        s.S.lifeFlags.Add("ready"); Need(!s.LifeEventsEligible(w).Contains("e903"), "requires does not bypass minDate");
        s.Today = 20160701;
        pool = s.LifeEventsEligible(w);
        Need(pool.Count == 1 && pool[0] == "e903", "newly eligible unseen row takes priority over remaining repeat");
        Need(s.OpenLifeEventNow("e903", w), "newly eligible row opens"); Answer(s, w);
        s.S.stage = 1;
        Need(!s.LifeEventsEligible(w).Contains("e903"), "cycle never bypasses stage range");
        var cycleBefore = string.Join(",", s.S.lifeCycleSeen); s = Reload(s);
        Need(string.Join(",", s.S.lifeCycleSeen) == cycleBefore, "current cycle survives save round-trip");

        var a = Sim(); var b = Sim(); a.S.lifeRng = b.S.lifeRng = 1234567;
        Tick(a, new Wallet(), 480); Tick(b, new Wallet(), 480);
        Need(a.LifeEventId == b.LifeEventId && a.S.lifeRng == b.S.lifeRng, "same event seed produces deterministic draws");
    }

    static void WalletAndPersistence()
    {
        var s = Sim(); var w = new Wallet { money = 9 };
        Need(s.OpenLifeEventNow("e901", w), "payment fixture opens while poor");
        Need(!s.AnswerLifeEvent(0, w) && !s.LifeEventResultPending, "insufficient funds leave question pending without result");
        Equal(w.money, 9, "rejected choice never spends money");
        w.money = 10; w.rejectSpend = true;
        Need(!s.AnswerLifeEvent(0, w), "wallet rejecting Spend at commit is respected");
        Equal(w.money, 10, "failed spend is not partially applied");
        Need(s.S.lifeHistory.Count == 0 && !s.LifeEventResultPending, "failed commit writes no history or result");
        w.rejectSpend = false;
        Need(s.AnswerLifeEvent(0, w), "exact funds payment commits");
        Equal(w.money, 0, "exact funds reach zero without overdraft");
        Equal(w.spent, 10, "payment commits once");
        Need(s.S.lifeHistory.Count == 1, "one committed answer creates one history entry");
        Need(s.LifeEventResultView().body == "已支付十元", "Chinese result snapshot is saved");
        s.English = true; Need(s.LifeEventResultView().body == "Paid ten yuan", "English result uses saved same outcome");
        var seed = s.S.lifeRng; s = Reload(s);
        Need(s.LifeEventResultPending && s.LifeEventResultView().body == "Paid ten yuan", "pending result and language-independent outcome survive reload");
        Need(!s.AnswerLifeEvent(0, w), "reloaded result cannot replay selected effects");
        Need(s.S.lifeRng == seed && s.S.lifeHistory.Count == 1 && w.spent == 10, "viewing/reloading result never rerolls, duplicates history or charges");
        s.LifeEventIntervalMode = 2;
        Need(s.LifeEventResultPending, "turning events off keeps an already committed result");
        Need(s.AcknowledgeLifeEventResult() && !s.AcknowledgeLifeEventResult(), "result closes exactly once");

        s = Sim(); w = new Wallet(); Need(s.OpenLifeEventNow("e902", w), "income fixture opens");
        Need(s.AnswerLifeEvent(0, w), "income choice commits"); Equal(w.money, 507, "income added once");
        s = Reload(s); Tick(s, w, 600); Need(!s.AnswerLifeEvent(0, w), "pending result never reapplies income");
        Equal(w.money, 507, "reload leaves earned amount unchanged");

        s = Sim(); w = new Wallet(); s.LifeEventIntervalMode = 2; s.LifeEventActive = false; s.LifeEventConflict = true;
        s.S.lifeReturns.Add(new XgLifeTimer { id = "zhou", amount = 25, until = s.S.lifeClock + 10 });
        s.S.lifePauseUntil = s.S.lifeClock + 5; s.S.lifeBoostUntil = s.S.lifeClock + 5; s.S.lifeBoostFactor = 2;
        Tick(s, w, 10); Equal(w.money, 525, "returns continue while cadence is off, inactive and conflicted");
        Need(s.S.lifeReturns.Count == 0 && !s.LifeTrainingPaused && s.LifeTrainingBoost == 1, "return removed and temporary effects expire on lifeClock");
        s = Reload(s); Tick(s, w, 10); Equal(w.money, 525, "completed repayment is not repeated after reload");

        s = Sim(); w = new Wallet();
        Need(s.OpenLifeEventNow("landlord_meter", w, true), "native random-outcome event opens");
        Need(s.AnswerLifeEvent(0, w), "native outcome commits once");
        var rolled = s.S.lifeRng; double moneyAfter = w.money;
        string zh = s.LifeEventResultView().body;
        s.English = true; string en = s.LifeEventResultView().body;
        Need(zh.Length > 0 && en.Length > 0 && zh != en, "native random result snapshots both languages at one outcome");
        s = Reload(s); Need(s.LifeEventResultView().body == en && s.S.lifeRng == rolled,
            "native result reload preserves text and never rerolls its random branch");
        Need(!s.AnswerLifeEvent(0, w), "native resolved event cannot apply its fine again");
        Equal(w.money, moneyAfter, "native result has no duplicate wallet effect");

        s = Sim(); w = new Wallet(); Need(s.OpenLifeEventNow("zhou_loan", w, true), "real loan event opens");
        Need(s.AnswerLifeEvent(0, w), "real loan effect commits");
        Equal(w.money, 400, "real loan pays its stored principal once");
        s = Reload(s); s.LifeEventIntervalMode = 2; s.LifeEventActive = false;
        Tick(s, w, XgSim.LifeReturnSeconds);
        Equal(w.money, 500, "real outstanding loan returns even before result acknowledgement with draws disabled");
        Need(s.LifeEventResultPending && s.S.lifeReturns.Count == 0, "loan return does not dismiss pending result");
        s = Reload(s); Tick(s, w, XgSim.LifeReturnSeconds);
        Equal(w.money, 500, "real loan repayment does not repeat after another reload");
    }

    static void ProductionDataSmoke()
    {
        Need(XgSim.LifeDataCount >= 100, "actual production data parses without rewriting its text");
        var s = Sim(); var w = new Wallet();
        s.S.stage = 3; s.Today = 20160701;
        var pool = s.LifeEventsEligible(w);
        Need(pool.Count > 0, "real production pool contains eligible events");
        Need(s.OpenLifeEventNow(pool[0], w), "real production event opens");
        var view = s.LifeEventView(w);
        int pick = view.choices.FindIndex(c => c.enabled);
        Need(pick >= 0 && s.AnswerLifeEvent(pick, w), "real production enabled choice commits");
        Need(s.LifeEventResultPending && s.LifeEventResultView().body.Length > 0, "real production result remains visible for confirmation");
        s.English = true; Need(s.LifeEventResultView().body.Length > 0, "real production English result exists");
        s = Reload(s); Need(s.LifeEventResultPending && !s.AnswerLifeEvent(pick, w), "real production result reload cannot replay effects");
    }
}
