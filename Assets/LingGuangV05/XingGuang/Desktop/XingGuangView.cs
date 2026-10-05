using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppNames = LingGuangV05.Core.AppNames;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>A page of 灵光.exe. Pages build their own layout and react to the sim through the view.</summary>
    public abstract class XgPage
    {
        protected XingGuangView view;
        protected XgUi ui;
        public RectTransform root;
        protected XgSim Sim => view.Sim;
        protected IXgHost Host => view.Host;
        protected XgJuice Fx => view.Juice;
        public void Init(XingGuangView owner, XgUi kit) { view = owner; ui = kit; }
        public abstract void Build(RectTransform area);
        public abstract void Refresh();
        public virtual void Tick(float dt) { }
        public virtual void Shown() { }
    }

    /// <summary>
    /// 灵光.exe content, built in code inside the 灵光.exe window: HUD (with the combo), nav, four pages
    /// (标注台, 训练, 技能树, 订单), toasts and the effect layer. Presentation only; rules live in XgSim.
    /// </summary>
    public sealed class XingGuangView : MonoBehaviour
    {
        /// <summary>Kept for the controller (window background).</summary>
        public static class Palette { public static readonly Color Page = XgPalette.Page; }

        XingGuangController controller;
        XgUi ui;
        RectTransform root, comboChip, comboFill;
        TMP_Text appName, hudMoney, hudIncome, hudCompute, hudStage, comboNumber, comboTier, toast;
        XgGlow comboGlow;
        readonly List<(string id, XgBtn btn)> tabs = new List<(string, XgBtn)>();
        readonly Dictionary<string, XgPage> pages = new Dictionary<string, XgPage>();
        XgBtn soundToggle, fxToggle;
        string tab = "label";
        float refreshTimer, toastTimer, comboShownWindow = 1, comboSparkTimer;
        int comboShown;
        double shownMoney = -1;
        RectTransform welcome;
        XgSim boundSim;
        XgSetupOverlay setup;
        TMP_FontAsset font;

        public XingGuangController Controller => controller;
        public XgSim Sim => controller.Sim;
        public IXgHost Host => controller.Host;
        public XgJuice Juice { get; private set; }
        /// <summary>Shop buy amount: 1, 10 or int.MaxValue (as many as affordable).</summary>
        public int BuyAmount = 1;
        public XgLabelPage Label { get; private set; }
        public XgTrainPage Train { get; private set; }
        public XgTreePage Tree { get; private set; }
        public XgContractsPage Contracts { get; private set; }
        public XgRepoPage Repo { get; private set; }
        public XgBoardPage Board { get; private set; }
        public XgWallPage Wall { get; private set; }
        public XgChatPage Chat { get; private set; }
        public XgFinalePage Finale { get; private set; }
        public bool Visible => controller != null && controller.Window != null && controller.Window.isOn && isActiveAndEnabled;
        public string Tab => tab;
        public RectTransform ComboChip => comboChip;

        public void Build(XingGuangController owner, RectTransform content, TMP_FontAsset fontAsset)
        {
            controller = owner;
            ui = new XgUi(fontAsset, owner.Window);
            root = Rect("XingGuang", content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // Laid out at the normal window size; maximising scales every control up with the window.
            UiFitScale.Attach(root, owner.Window, new Vector2(1236, 693));
            Panel(root, XgPalette.Page);
            Juice = gameObject.AddComponent<XgJuice>();
            BuildHud();
            BuildNav();
            var area = Rect("Pages", root, Vector2.zero, Vector2.one, new Vector2(150, 12), new Vector2(-12, -70));
            Label = Add("label", new XgLabelPage(), area);
            Train = Add("train", new XgTrainPage(), area);
            Tree = Add("tree", new XgTreePage(), area);
            Contracts = Add("contracts", new XgContractsPage(), area);
            Repo = Add("repo", new XgRepoPage(), area);
            Board = Add("board", new XgBoardPage(), area);
            Wall = Add("wall", new XgWallPage(), area);
            Chat = Add("chat", new XgChatPage(), area);
            Finale = Add("final", new XgFinalePage(), area);
            Repo.SeenUpTo = owner.Sim.S.nextModelId - 1;
            var toastBox = Rect("Toast", root, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-330, 16), new Vector2(330, 52));
            Panel(toastBox, new Color(.1f, .12f, .2f, .9f)).raycastTarget = false;
            toast = ui.Text(Rect("Text", toastBox, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0)), "", 17, Color.white, TextAlignmentOptions.Center);
            toastBox.gameObject.SetActive(false);
            font = fontAsset;
            setup = new XgSetupOverlay(this, ui, fontAsset, root);
            Juice.Init(root, root, fontAsset);
            Juice.Visible = () => Visible;
            // Self-insight cards (闪卡) over the whole window.
            XgHoloCard.Install(this, root, fontAsset);
            // The one real training (XOR wall, stage-one breakthrough, recognising 0) in a corner panel.
            XgRealTrainPanel.Install(this, root, fontAsset);
            Bind(owner);
            ShowTab(tab);
        }

        T Add<T>(string id, T page, RectTransform area) where T : XgPage
        {
            page.Init(this, ui);
            page.Build(area);
            page.root.gameObject.name = id;
            pages[id] = page;
            return page;
        }

        public void Bind(XingGuangController owner)
        {
            controller = owner;
            if (boundSim != null) Unsubscribe(boundSim);
            boundSim = owner.Sim;
            boundSim.Message += OnMessage;
            boundSim.Diverged += OnDiverged;
            boundSim.EpochDone += OnEpoch;
            boundSim.EpochStarted += OnEpochStarted;
            boundSim.BreakthroughDone += OnBreakthrough;
            boundSim.Emerged += OnEmerged;
            boundSim.InsightReached += OnInsight;
            boundSim.StageAdvanced += OnStageAdvanced;
            boundSim.Assessed += OnAssessed;
            boundSim.NodeBought += OnNodeBought;
            boundSim.ComboTier += OnComboTier;
            boundSim.ComboBroken += OnComboBroken;
            boundSim.DeskOpened += OnDeskOpened;
            boundSim.CardTimedOut += OnCardTimedOut;
            boundSim.DuelDone += OnDuelDone;
            boundSim.ModelSaved += OnModelSaved;
            Juice.Reduced = Sim.S.reduceFx;
            Juice.Muted = Sim.S.mute;
            Refresh(true);
        }

        void Unsubscribe(XgSim sim)
        {
            sim.EpochStarted -= OnEpochStarted; sim.BreakthroughDone -= OnBreakthrough; sim.Emerged -= OnEmerged; sim.InsightReached -= OnInsight; sim.StageAdvanced -= OnStageAdvanced;
            sim.Message -= OnMessage; sim.Diverged -= OnDiverged; sim.EpochDone -= OnEpoch; sim.Assessed -= OnAssessed;
            sim.NodeBought -= OnNodeBought; sim.ComboTier -= OnComboTier; sim.ComboBroken -= OnComboBroken; sim.DeskOpened -= OnDeskOpened;
            sim.CardTimedOut -= OnCardTimedOut; sim.DuelDone -= OnDuelDone; sim.ModelSaved -= OnModelSaved;
        }

        void OnDestroy() { if (boundSim != null) Unsubscribe(boundSim); }

        // ───────────── events → feel ─────────────

        void OnMessage(string text) { ShowToast(text, 3.5f); }

        void OnDiverged(int track)
        {
            Juice.Flash(XgPalette.Bad, .25f, .55f);
            if (Sim.LastEpochWasHand) { Juice.Shake(12, .4f); Juice.HitStop(200); }
            Juice.Play(XgJuice.Sfx.Id.Buzz);
            Train.Glitch();
        }

        void OnEpochStarted(XgEpoch e) { Train.OnEpochStarted(e); }
        void OnBreakthrough(string id) { if (id == "transformer") Tree.OnBreakthrough(id); Refresh(true); }
        /// <summary>An emergence moment (design v1.1 §5.4): the screen flashes and the line floats over the window.</summary>
        void OnEmerged(int stage, string line)
        {
            Juice.Flash(new Color32(255, 240, 200, 255), .25f, .9f);
            Juice.Play(XgJuice.Sfx.Id.Fanfare, .8f);
            Juice.Float(Juice.At(root), line, XgPalette.Gold, 22, 90, 3.5f, 1.1f);
            Refresh(true);
        }

        /// <summary>A wall worked out alone: the AI reacts, as far as its stage lets it speak (XgSim.InsightLine).</summary>
        void OnInsight(int stage, string route, string line)
        {
            Juice.Play(XgJuice.Sfx.Id.Unlock, 1.1f);
            Juice.Float(Juice.At(root), line, XgPalette.Gold, 22, 60, 3f, 1.1f);
        }

        void OnStageAdvanced(int from)
        {
            Juice.Flash(Color.white, .2f, .8f);
            Juice.Burst(Juice.At((RectTransform)hudStage.transform.parent), 40, XgPalette.Gold, XgJuice.Shape.Confetti, 420);
            Juice.Play(XgJuice.Sfx.Id.Fanfare);
            Refresh(true);
        }

        void OnEpoch(XgEpoch e) { Train.OnEpoch(e); }
        void OnAssessed(XgAssessment a) { Train.OnAssessed(a); }
        void OnNodeBought(XgNode n)
        {
            var target = Tree.NodeTarget(n.id);
            if (target != null && tab == "tree") Juice.TransferCoins(Juice.At((RectTransform)hudMoney.transform.parent), Juice.At(target));
            Tree.OnBought(n); Refresh(true);
        }
        void OnDeskOpened(XgDesk d) { Juice.Play(XgJuice.Sfx.Id.Unlock); Label.RebuildTabs(); }
        void OnCardTimedOut(string desk) { Label.OnTimeout(); }

        void OnDuelDone(bool won, double bonus)
        {
            if (!won) { Juice.Play(XgJuice.Sfx.Id.Thud); return; }
            Juice.Play(XgJuice.Sfx.Id.Fanfare);
            Juice.Burst(Juice.At(Label.Paper), 30, Color.white, XgJuice.Shape.Confetti, 420);
            Juice.Float(Juice.At(Label.Paper, new Vector2(0, 40)), T("斗图胜利 +¥", "Battle won +¥") + N(bonus, "0"), XgPalette.Gold, 30);
        }

        void OnModelSaved(XgModelEntry m)
        {
            var tabRt = tabs.Find(x => x.id == "repo").btn?.rt;
            if (tabRt == null) return;
            Juice.Knock(tabRt, .12f);
            if (!m.record) Juice.Float(Juice.At(tabRt, new Vector2(60, 0)), T("已存入仓库", "Saved"), XgPalette.Accent, 18);
            Juice.Play(XgJuice.Sfx.Id.Tick, .7f, .4f);
        }

        void OnComboTier(int tier)
        {
            string name = T(XgCatalog.ComboTierNames[tier], XgCatalog.ComboTierNamesEn[tier]);
            Juice.Flash(Color.white, .12f, .55f);
            Juice.Shake(6, .3f);
            Juice.Knock(comboChip, .25f, Vector2.zero, .35f);
            Juice.Float(Juice.At(comboChip, new Vector2(-40, -50)), name + "！", XgPalette.Tiers[tier + 1], 30 + tier * 4, 70, 1.1f, 1.6f);
            Juice.Burst(Juice.At(comboChip), 16 + tier * 10, XgPalette.Tiers[tier + 1], tier >= 3 ? XgJuice.Shape.Star : XgJuice.Shape.Spark, 300);
            Juice.Play(XgJuice.Sfx.Id.Tier, 1 + tier * .15f);
            if (tier >= 3) Juice.HitStop(120);
        }

        void OnComboBroken(int was)
        {
            Juice.Crumble(Juice.At(comboChip), Mathf.Min(30, 6 + was / 2), XgPalette.Tiers[Mathf.Min(4, XgSim.TierOf(was) + 1)]);
            Juice.Play(XgJuice.Sfx.Id.Break);
            if (was >= 10) Juice.Float(Juice.At(comboChip, new Vector2(-30, -46)), T("连击中断 ×", "Combo lost ×") + was, XgPalette.Bad, 20);
        }

        public void ShowToast(string text, float seconds)
        {
            if (toast == null || !Visible) return;
            toast.text = text;
            toast.transform.parent.gameObject.SetActive(true);
            toast.transform.parent.SetAsLastSibling();
            Juice.BringToFront();
            toastTimer = seconds;
        }

        // ───────────── frame ─────────────

        void Update()
        {
            if (controller == null || Sim == null) return;
            if (!Visible) return;
            float dt = Time.unscaledDeltaTime;
            if (toastTimer > 0) { toastTimer -= dt; if (toastTimer <= 0) toast.transform.parent.gameObject.SetActive(false); }
            foreach (var p in pages.Values) if (p.root.gameObject.activeSelf) p.Tick(dt);
            UpdateCombo();
            UpdateMoney(dt);
            if (controller.Offline != null && welcome == null) ShowWelcome(controller.Offline);
            refreshTimer -= dt;
            if (refreshTimer <= 0) Refresh(false);
        }

        /// <summary>The wallet counts up towards its real value instead of jumping.</summary>
        void UpdateMoney(float dt)
        {
            double money = Host.Money;
            if (shownMoney < 0 || Math.Abs(money - shownMoney) > Math.Max(1e5, money * 2)) shownMoney = money;
            double before = shownMoney;
            shownMoney += (money - shownMoney) * (1 - Math.Exp(-10 * dt));
            if (Math.Abs(money - shownMoney) < .005) shownMoney = money;
            hudMoney.text = "¥ " + Money(shownMoney);
            hudMoney.color = money > before + .004 ? new Color32(255, 220, 120, 255) : (Color)XgPalette.Money;
        }

        /// <summary>Lab income per second: contracts plus auto-answer on every desk.</summary>
        public double IncomeRate
        {
            get { double r = Sim.IncomePerSecond; foreach (var l in Sim.S.autoLevels) r += Sim.AutoIncome(l.dataset, Host); return r; }
        }

        void ShowWelcome(XingGuangController.OfflineReport r)
        {
            welcome = Rect("Welcome", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(welcome, new Color(.05f, .07f, .15f, .55f));
            var card = Rect("Card", welcome, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-230, -140), new Vector2(230, 140));
            Panel(card, Color.white);
            var head = Rect("Head", card, new Vector2(0, 1), Vector2.one, new Vector2(0, -56), Vector2.zero);
            Panel(head, XgPalette.Hud);
            ui.Text(Rect("Title", head, Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-18, 0)), T("欢迎回来", "Welcome back"), 24, Color.white, TextAlignmentOptions.MidlineLeft).fontStyle = FontStyles.Bold;
            var t = TimeSpan.FromSeconds(Math.Min(r.seconds, 86400));
            string away = t.TotalHours >= 1 ? T((int)t.TotalHours + " 小时 " + t.Minutes + " 分", (int)t.TotalHours + " h " + t.Minutes + " min") : T(t.Minutes + " 分钟", t.Minutes + " min");
            var body = new System.Text.StringBuilder();
            body.Append(T("你离开了 ", "You were away ")).Append(away).Append(r.seconds > 86400 ? T("（最多算 24 小时）", " (24 h max)") : "").Append(T("。" + AppNames.AiZh + "没闲着：", ". " + AppNames.AiEn + " kept working:")).Append("\n\n");
            body.Append("<size=30><color=#E86E14><b>+¥").Append(Money(r.income)).Append("</b></color></size>\n");
            if (r.labels > 0) body.Append(T("自动答题标了 ", "Auto-answer labelled ")).Append(N(r.labels, "0")).Append(T(" 条", "")).Append("  ");
            if (r.epochs > 0) body.Append(T("后台训练 ", "Trained ")).Append(r.epochs).Append(T(" 轮", " epochs")).Append("  ");
            if (r.records > 0) body.Append(T("刷新纪录 ", "Records ")).Append(r.records).Append(T(" 次", ""));
            ui.Text(Rect("Body", card, Vector2.zero, Vector2.one, new Vector2(20, 64), new Vector2(-20, -66)), body.ToString(), 16, XgPalette.Ink, TextAlignmentOptions.TopLeft);
            var ok = ui.Button(card, T("收下", "Collect"), () =>
            {
                Juice.Burst(Juice.At((RectTransform)hudMoney.transform.parent), 30, XgPalette.Gold, XgJuice.Shape.Yen, 320);
                Juice.Knock((RectTransform)hudMoney.transform.parent, .2f);
                Juice.Play(XgJuice.Sfx.Id.Coin); Juice.Play(XgJuice.Sfx.Id.Fanfare, 1, .6f);
                controller.Offline = null;
                Destroy(welcome.gameObject); welcome = null;
            }, 18);
            ok.rt.anchorMin = ok.rt.anchorMax = new Vector2(.5f, 0); ok.rt.offsetMin = new Vector2(-90, 14); ok.rt.offsetMax = new Vector2(90, 58);
            ok.Set(T("收下", "Collect"), true, XgPalette.Good, Color.white);
            welcome.SetAsLastSibling();
            Juice.BringToFront();
            Juice.Knock(card, .1f, Vector2.zero, .35f);
            Juice.Play(XgJuice.Sfx.Id.Whoosh);
        }

        void UpdateCombo()
        {
            int combo = Sim.S.combo;
            int tier = XgSim.TierOf(combo);
            if (combo >= 100 && Visible)
            {
                comboSparkTimer += Time.unscaledDeltaTime;
                if (comboSparkTimer >= .4f) { comboSparkTimer = 0; Juice.Burst(Juice.At(comboChip), 2, XgPalette.Gold, XgJuice.Shape.Star, 70, 100, .8f); }
            }
            else comboSparkTimer = 0;
            if (combo != comboShown)
            {
                if (combo > comboShown) { Juice.Knock(comboChip, .06f + Mathf.Min(.1f, (combo - comboShown) * .02f)); comboShownWindow = (float)Math.Max(Sim.S.comboTimer, .1); }
                comboShown = combo;
                comboNumber.text = combo > 0 ? "×" + combo : "×0";
                comboTier.text = tier >= 0 ? T(XgCatalog.ComboTierNames[tier], XgCatalog.ComboTierNamesEn[tier]) : (Sim.S.bestCombo > 0 ? T("最高 ×", "best ×") + Sim.S.bestCombo : T("连击", "combo"));
            }
            var color = XgPalette.Tiers[tier + 1];
            if (tier >= 3) color = Color.Lerp(color, Color.white, .5f + .5f * Mathf.Sin(Time.unscaledTime * 10));
            comboNumber.color = combo > 0 ? color : new Color(1, 1, 1, .45f);
            comboTier.color = combo > 0 ? color : new Color(1, 1, 1, .45f);
            SetBar(comboFill, combo > 0 ? (float)(Sim.S.comboTimer / Math.Max(.1, comboShownWindow)) : 0);
            comboGlow.on = tier >= 0;
            comboGlow.color = XgPalette.Tiers[tier + 1] * new Color(1, 1, 1, .6f);
        }

        // ───────────── layout ─────────────

        void BuildHud()
        {
            var hud = Rect("Hud", root, new Vector2(0, 1), Vector2.one, new Vector2(0, -58), Vector2.zero);
            Panel(hud, XgPalette.Hud);
            var logo = Rect("Logo", hud, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(14, -20), new Vector2(54, 20));
            logo.gameObject.AddComponent<XgIconGraphic>().raycastTarget = false;
            appName = ui.Text(Rect("AppName", hud, new Vector2(0, 0), new Vector2(0, 1), new Vector2(62, 0), new Vector2(300, 0)), "", 20, Color.white, TextAlignmentOptions.MidlineLeft);
            appName.fontStyle = FontStyles.Bold;
            hudMoney = Chip(hud, 0, 170, XgPalette.Money);
            hudIncome = Chip(hud, 1, 170, new Color32(255, 196, 140, 255));
            hudCompute = Chip(hud, 2, 210, new Color32(160, 186, 255, 255));
            hudStage = Chip(hud, 3, 170, XgPalette.Star);
            // Combo chip: number, tier name and the window bar.
            comboChip = Rect("Combo", hud, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-12 - 170, -25), new Vector2(-12, 25));
            comboGlow = Glow(comboChip, XgPalette.Gold, 5);
            Panel(comboChip, new Color32(60, 30, 90, 255));
            comboNumber = ui.Text(Rect("Number", comboChip, Vector2.zero, new Vector2(.55f, 1), new Vector2(8, 4), new Vector2(0, 0)), "×0", 30, Color.white, TextAlignmentOptions.MidlineLeft);
            comboNumber.fontStyle = FontStyles.Bold;
            comboTier = ui.Text(Rect("Tier", comboChip, new Vector2(.5f, 0), Vector2.one, new Vector2(0, 6), new Vector2(-8, 0)), "", 15, Color.white, TextAlignmentOptions.MidlineRight);
            var bar = Rect("Window", comboChip, Vector2.zero, new Vector2(1, 0), new Vector2(6, 4), new Vector2(-6, 8));
            comboFill = Bar(bar, "Fill", new Color(1, 1, 1, .12f), XgPalette.Gold);
            // Hover help for newcomers.
            UiTip.Add(hudStage.transform.parent, "实验室现在的阶段（共 6 个，每个约一个月）。\n练得够多就会「撞墙」：再练也不涨。\n用对新结构过了墙，才进入下一阶段。", "The lab's stage (6 in all, about a month each).\nTrain enough and you hit a wall: more training stops helping.\nPass it with the right new structure to reach the next stage.");
            UiTip.Add(hudCompute.transform.parent, "算力：每轮训练跑多快，显卡越多越快。\n显存：能放下多大的模型。层数、宽度开太大会超显存，就训不起来。", "GPU: how fast each epoch runs; more cards are faster.\nVRAM: how big a model fits. Too many layers or too much width will not fit and cannot train.");
            UiTip.Add(hudIncome.transform.parent, () => controller != null && controller.Sim != null && controller.Sim.AutoLabelHidden
                ? T("每秒自动进账 = 已签订单。\n去「订单」页：模型准确率够了就能签。", "Money per second = signed contracts.\nOpen Contracts: once a model is accurate enough you can sign.")
                : T("每秒自动进账 = 已签订单 + 自动答题。\n去「订单」页：模型准确率够了就能签。", "Money per second = signed contracts + auto-answering.\nOpen Contracts: once a model is accurate enough you can sign."));
            UiTip.Add(hudMoney.transform.parent, "经费。手动标注、订单、评级奖励会加钱；电费、显卡、技能树会花钱。", "Funds. Labelling, contracts and grade rewards add money; power, cards and the skill tree cost money.");
            UiTip.Add(comboChip, "连击：连续答对、连续手动训练都会叠加。\n连击越高，标注报酬和手动训练效果越高。\n答错、超时或停太久会清零。", "Combo: builds with every right answer and every hand-pressed epoch.\nHigher combo pays more for labels and trains faster by hand.\nA wrong answer, a timeout or a long pause resets it.");
        }

        float chipX = 12 + 170 + 8;

        TMP_Text Chip(RectTransform hud, int index, float w, Color ink)
        {
            if (index == 0) chipX = 12 + 170 + 8;
            float x1 = -chipX;
            chipX += w + 8;
            var r = Rect("Chip" + index, hud, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(x1 - w, -21), new Vector2(x1, 21));
            Panel(r, XgPalette.HudChip);
            return ui.Text(Rect("Text", r, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-8, 0)), "", 16, ink, TextAlignmentOptions.MidlineLeft);
        }

        void BuildNav()
        {
            var nav = Rect("Nav", root, Vector2.zero, new Vector2(0, 1), new Vector2(12, 12), new Vector2(138, -70));
            Panel(nav, XgPalette.Card);
            string[] ids = { "label", "train", "tree", "contracts", "repo", "board", "wall", "chat", "final" };
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                var b = ui.Button(nav, "", () => { ShowTab(id); Juice.Play(XgJuice.Sfx.Id.Click); }, 17);
                b.rt.anchorMin = new Vector2(0, 1); b.rt.anchorMax = new Vector2(1, 1);
                b.rt.offsetMin = new Vector2(8, -8 - (i + 1) * 58); b.rt.offsetMax = new Vector2(-8, -8 - i * 58 - 6);
                tabs.Add((id, b));
                UiTip.Add(b.rt, () => TabHelp(id));
            }
            soundToggle = ui.Button(nav, "", () => { Sim.S.mute = !Sim.S.mute; Juice.Muted = Sim.S.mute; Refresh(true); }, 14);
            PlaceBottom(soundToggle, 48, 32, 8, 8);
            fxToggle = ui.Button(nav, "", () => { Sim.S.reduceFx = !Sim.S.reduceFx; Juice.Reduced = Sim.S.reduceFx; Refresh(true); }, 14);
            PlaceBottom(fxToggle, 10, 32, 8, 8);
            UiTip.Add(soundToggle.rt, "打开或关闭音效。", "Turn sound effects on or off.");
            UiTip.Add(fxToggle.rt, "减少闪光、震动和粒子特效。", "Fewer flashes, shakes and particles.");
        }

        static string TabHelp(string id)
        {
            switch (id)
            {
                case "label": return T("标注台：亲手给数据打标签。\n答对赚钱，也给模型攒训练数据。逻辑题最值钱。", "Labelling: tag data by hand.\nRight answers earn money and give the model training data. Logic pays best.");
                case "train": return T("训练：选数据集和结构，按「训练一轮」让它学。\n隔几轮按「评估」看成绩；成绩好才能签订单。", "Train: pick a dataset and structure, press Train epoch.\nEvaluate every few epochs; good scores unlock contracts.");
                case "tree": return T("技能树：花钱解锁新结构、更深更宽的网络、数据包和自动化。\n按住节点 0.6 秒购买。", "Skill tree: buy new structures, deeper and wider nets, data packs and automation.\nHold a node for 0.6 s to buy.");
                case "contracts": return T("订单：模型准确率达到要求就能签约，之后每秒自动给钱。", "Contracts: sign once a model reaches the required accuracy; they pay every second.");
                case "repo": return T("模型仓库：每次刷新纪录都存一个检查点。", "Models: a checkpoint is saved for every record.");
                case "board": return T("大脑：它学到的概念，以及它以为的联系（有些是错的）。", "Brain: the concepts it has learned and the links it believes (some are wrong).");
                case "wall": return T("诊断：看它错在哪。错误的规律，就是该换什么结构的线索。\n训练图式：网络卡在哪一层、第几轮开始出问题。", "Diagnose: see where it goes wrong. The pattern of mistakes tells you what structure to try.\nTraining map: which layer is stuck and from which epoch.");
                case "chat": return T("对话：和它说话。阶段越高，它会说的越多。", "Talk: chat with it. The higher the stage, the more it can say.");
                default: return T("终章。", "Finale.");
            }
        }

        /// <summary>Window opened (from the icon, taskbar or a story notification); tab is optional.</summary>
        public void Open(string tabId)
        {
            if (tabId == "vision" || tabId == "sequence") { Sim.SelectedTrack = tabId == "vision" ? XgTrack.Vision : XgTrack.Sequence; tabId = "train"; }
            if (tabId == "research") tabId = "tree";
            if (!string.IsNullOrEmpty(tabId) && pages.ContainsKey(tabId)) ShowTab(tabId);
            else Refresh(true);
        }

        public void ShowTab(string id)
        {
            if (!pages.ContainsKey(id) || !controller.FeatureVisible(id)) return;
            tab = id;
            foreach (var kv in pages) kv.Value.root.gameObject.SetActive(kv.Key == id);
            pages[id].Shown();
            Juice.BringToFront();
            Refresh(true);
        }

        public void Refresh(bool force)
        {
            if (controller == null || Sim == null || root == null) return;
            refreshTimer = .2f;
            if (!force && !Visible) return;
            appName.text = T(AppNames.AppZh + " · 深度学习工作站", AppNames.AppEn + " · DL workstation");
            if (shownMoney < 0) hudMoney.text = "¥ " + Money(Host.Money);
            hudIncome.text = T("收入 ", "Income ") + "¥" + Money(IncomeRate) + T("/秒", "/s");
            double vramUse = Math.Max(XgSim.VramNeedMB(Sim.S.vision), XgSim.VramNeedMB(Sim.S.sequence));
            hudCompute.text = T("算力 ", "GPU ") + N(Host.Compute, "0.0") + T(" · 显存 ", " · VRAM ") + N(vramUse / 1024, "0.0") + "/" + N(Sim.Vram(Host) / 1024, "0") + "G";
            hudStage.text = T("阶段 ", "Stage ") + Sim.S.stage + "/6" + (Sim.Winter ? T(" · 寒冬", " · winter") : "");
            string[] names = { T("标注台", "Labelling"), T("训练", "Train"), T("技能树", "Skill tree"), T("订单", "Contracts"), T("模型仓库", "Models"), T("大脑", "Brain"), T("诊断", "Diagnose"), T("对话", "Talk"), T("终章", "Finale") };
            bool trainable = Sim.TrainingUnlocked(XgTrack.Vision) || Sim.TrainingUnlocked(XgTrack.Sequence);
            bool[] open = { controller.FeatureVisible("label"), controller.FeatureVisible("train"), controller.FeatureVisible("tree"), controller.FeatureVisible("contracts"), controller.FeatureVisible("repo"), controller.FeatureVisible("board"), controller.FeatureVisible("wall"), controller.FeatureVisible("chat"), controller.FeatureVisible("final") };
            int current = tabs.FindIndex(x => x.id == tab);
            if (current >= 0 && !open[current]) { tab = "label"; foreach (var kv in pages) kv.Value.root.gameObject.SetActive(kv.Key == tab); }
            int buyable = 0; foreach (var n in XgCatalog.Nodes) if (Sim.Status(n, Host) == XgSim.NodeStatus.Buyable) buyable++;
            bool raiseReady = Sim.RaiseLevel < XgCatalog.RaiseMax && Host.Money >= Sim.NextRaiseCost;
            int fresh = Sim.S.nextModelId - 1 - (Repo != null ? Repo.SeenUpTo : 0);
            string[] badges = { raiseReady ? "加薪" : "", "", buyable > 0 ? buyable.ToString() : "", "", fresh > 0 && tab != "repo" ? "+" + fresh : "", "", Sim.ActiveWall != null && tab != "wall" ? "!" : "", "", Sim.EndingOpen && tab != "final" ? "!" : "" };
            int visibleRow = 0;
            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].btn.rt.gameObject.SetActive(open[i]);
                if (!open[i]) continue;
                tabs[i].btn.rt.offsetMin = new Vector2(8, -8 - (visibleRow + 1) * 58);
                tabs[i].btn.rt.offsetMax = new Vector2(-8, -8 - visibleRow * 58 - 6);
                visibleRow++;
                bool on = tabs[i].id == tab;
                string badge = badges[i].Length > 0 ? "  <size=13><color=#" + (on ? "FFE08A" : "E08A00") + ">" + (i == 0 ? T(badges[i], "raise") : badges[i]) + "</color></size>" : "";
                tabs[i].btn.Set(names[i] + badge, open[i], on ? XgPalette.Accent : XgPalette.Button, on ? Color.white : XgPalette.Ink);
            }
            soundToggle.Set(Sim.S.mute ? T("音效：关", "Sound: off") : T("音效：开", "Sound: on"), true);
            fxToggle.Set(Sim.S.reduceFx ? T("减少特效：开", "Less FX: on") : T("减少特效：关", "Less FX: off"), true);
            if (pages.TryGetValue(tab, out var page)) page.Refresh();
            setup?.Refresh(controller.runtime);
        }

    }
}
