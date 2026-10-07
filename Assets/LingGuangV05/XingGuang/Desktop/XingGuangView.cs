using System;
using System.Collections.Generic;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AppNames = LingGuangV05.Core.AppNames;
using static LingGuangV05.Desktop.XingGuang.XgUi;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The window a lab page lives in: 灵光.exe (<see cref="XingGuangView"/>) or 摆渡众包 (ZhongbaoView). Both run on
    /// the same <see cref="XingGuangController.Sim"/>; the host owns the HUD, the toasts and the effect layer.
    /// </summary>
    public interface IXgPageHost
    {
        XingGuangController Controller { get; }
        XgSim Sim { get; }
        IXgHost Host { get; }
        XgJuice Juice { get; }
        /// <summary>Shop buy amount: 1, 10 or int.MaxValue (as many as affordable).</summary>
        int BuyAmount { get; set; }
        bool Visible { get; }
        string Tab { get; }
        RectTransform ComboChip { get; }
        void Refresh(bool force);
        void ShowToast(string text, float seconds);
        /// <summary>Shows a page of this window, or opens the window that has it.</summary>
        void ShowTab(string id);
        /// <summary>Buys a tech-tree node (the public data packs on the labelling page) with the purchase effects.</summary>
        bool BuyNode(XgNode node, RectTransform from);
    }

    /// <summary>A page of 灵光.exe or 摆渡众包. Pages build their own layout and react to the sim through their host.</summary>
    public abstract class XgPage
    {
        protected IXgPageHost host;
        /// <summary>The 灵光.exe window; null for pages hosted by 摆渡众包.</summary>
        protected XingGuangView view => host as XingGuangView;
        protected XgUi ui;
        public RectTransform root;
        protected XgSim Sim => host.Sim;
        protected IXgHost Host => host.Host;
        protected XgJuice Fx => host.Juice;
        public void Init(IXgPageHost owner, XgUi kit) { host = owner; ui = kit; }
        public abstract void Build(RectTransform area);
        public abstract void Refresh();
        public virtual void Tick(float dt) { }
        public virtual void Shown() { }
    }

    /// <summary>
    /// 灵光.exe content, built in code inside the 灵光.exe window, in the dark scanner style of the approved redesign
    /// (Documentation/LingGuang-Incremental/lingguang-redesign/index.html): the top bar (the AI, the two bars of the main
    /// line, ¥, income, power and the clock; XingGuangView.Frame.cs), the grouped nav, the pages over a grid, toasts and
    /// the effect layer. 概览 is the landing page. The 标注台 and 订单 pages, and the combo, live in 摆渡众包; asking this
    /// window for them opens that app instead. Presentation only; rules live in XgSim.
    /// </summary>
    public sealed partial class XingGuangView : MonoBehaviour, IXgPageHost
    {
        /// <summary>Kept for the controller (window background).</summary>
        public static class Palette { public static readonly Color Page = XgDark.Page; }

        /// <summary>The top bar's height and the nav's width (the mockup's grid rows and columns).</summary>
        public const float TopBarHeight = 52, NavWidth = 148;

        XingGuangController controller;
        XgUi ui;
        RectTransform root;
        TMP_Text toast;
        readonly Dictionary<string, XgPage> pages = new Dictionary<string, XgPage>();
        string tab = "home";
        float refreshTimer, toastTimer;
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
        public int BuyAmount { get; set; } = 1;
        public XgHomePage Home { get; private set; }
        public XgTrainPage Train { get; private set; }
        public XgDataPage Data { get; private set; }
        public XgItemsPage Items { get; private set; }
        public XgTechTreePage Tree { get; private set; }
        public XgRepoPage Repo { get; private set; }
        public XgChatPage Chat { get; private set; }
        public XgAbilitiesPage Abilities { get; private set; }
        public XgFinalePage Finale { get; private set; }
        public XgCardsPage Cards { get; private set; }
        public XgWiringPage Wiring { get; private set; }
        /// <summary>The big card overlay (earned cards flip in; the album opens them).</summary>
        public XgHoloCard Holo { get; private set; }
        public bool Visible => controller != null && controller.Window != null && controller.Window.isOn && isActiveAndEnabled;
        public string Tab => tab;
        /// <summary>The combo lives in 摆渡众包; effects aimed at it here land on the main-line bars.</summary>
        public RectTransform ComboChip => barsRect;

        public void Build(XingGuangController owner, RectTransform content, TMP_FontAsset fontAsset)
        {
            controller = owner;
            ui = new XgUi(fontAsset, owner.Window, true);
            root = Rect("XingGuang", content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // Laid out at the normal window size; maximising scales every control up with the window.
            UiFitScale.Attach(root, owner.Window, new Vector2(1236, 693));
            Panel(root, XgDark.Page);
            Juice = gameObject.AddComponent<XgJuice>();
            // The scanner grid under the pages (the mockup's .page background).
            var grid = Rect("Grid", root, Vector2.zero, Vector2.one, new Vector2(NavWidth, 0), new Vector2(0, -TopBarHeight));
            grid.gameObject.AddComponent<CanvasRenderer>();
            var lines = grid.gameObject.AddComponent<XgGridGraphic>();
            lines.color = XgDark.Page; lines.raycastTarget = false;
            BuildTopBar();
            BuildNav();
            var area = Rect("Pages", root, Vector2.zero, Vector2.one, new Vector2(NavWidth + 12, 12), new Vector2(-12, -TopBarHeight - 12));
            Home = Add("home", new XgHomePage(), area);
            Train = Add("train", new XgTrainPage(), area);
            Data = Add("data", new XgDataPage(), area);
            Items = Add("items", new XgItemsPage(), area);
            Tree = Add("tree", new XgTechTreePage(), area);
            Repo = Add("repo", new XgRepoPage(), area);
            Wiring = Add("wiring", new XgWiringPage(), area);
            Abilities = Add("abilities", new XgAbilitiesPage(), area);
            Cards = Add("cards", new XgCardsPage(), area);
            Chat = Add("chat", new XgChatPage(), area);
            Finale = Add("final", new XgFinalePage(), area);
            Repo.SeenUpTo = owner.Sim.S.nextModelId - 1;
            var toastBox = Rect("Toast", root, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-330 + NavWidth / 2, 16), new Vector2(330 + NavWidth / 2, 52));
            Panel(toastBox, new Color32(14, 24, 35, 240)).raycastTarget = false;
            var rim = toastBox.gameObject.AddComponent<Outline>(); rim.effectColor = XgDark.Good; rim.effectDistance = new Vector2(1, -1);
            toast = ui.Text(Rect("Text", toastBox, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0)), "", 16, XgDark.Ink, TextAlignmentOptions.Center);
            toastBox.gameObject.SetActive(false);
            font = fontAsset;
            setup = new XgSetupOverlay(this, ui, fontAsset, root);
            Juice.Init(root, root, fontAsset);
            Juice.Visible = () => Visible;
            // Self-insight cards (闪卡) over the whole window.
            Holo = XgHoloCard.Install(this, root, fontAsset);
            // The one real training (XOR on a perceptron, stage one ending, recognising 0) in a corner panel.
            XgRealTrainPanel.Install(this, root, fontAsset);
            // An ability that emerges plays over the whole window (参数量与数据量主线).
            XgEmergenceCutscene.Install(this, root, fontAsset);
            Bind(owner);
            ShowTab(FeatureOpen(tab) ? tab : FirstOpenTab());
        }

        /// <summary>Whether a page is open now. The new pages (概览, 数据, 道具, 能力) follow the features they show.</summary>
        public bool FeatureOpen(string id)
        {
            if (controller == null || controller.Sim == null) return false;
            switch (id)
            {
                case "home": case "abilities": return true;
                case "data": return controller.FeatureVisible("train") || controller.Sim.TrainedSamples > 0;
                case "items": return controller.FeatureVisible("tree");
                default: return controller.FeatureVisible(id);
            }
        }

        /// <summary>The page shown when the current one is closed: 概览 (always open).</summary>
        string FirstOpenTab()
        {
            foreach (var id in NavIds) if (FeatureOpen(id)) return id;
            return "home";
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
            boundSim.StageAdvanced += OnStageAdvanced;
            boundSim.Assessed += OnAssessed;
            boundSim.NodeBought += OnNodeBought;
            boundSim.DeskOpened += OnDeskOpened;
            boundSim.ModelSaved += OnModelSaved;
            boundSim.WiringLogged += OnWiringLogged;
            Juice.Reduced = Sim.S.reduceFx;
            Juice.Muted = Sim.S.mute;
            SeedFeed();
            chatSeenAt = LastAiLineAt();
            Refresh(true);
        }

        void Unsubscribe(XgSim sim)
        {
            sim.EpochStarted -= OnEpochStarted; sim.BreakthroughDone -= OnBreakthrough; sim.Emerged -= OnEmerged; sim.StageAdvanced -= OnStageAdvanced;
            sim.Message -= OnMessage; sim.Diverged -= OnDiverged; sim.EpochDone -= OnEpoch; sim.Assessed -= OnAssessed;
            sim.NodeBought -= OnNodeBought; sim.DeskOpened -= OnDeskOpened; sim.ModelSaved -= OnModelSaved; sim.WiringLogged -= OnWiringLogged;
        }

        void OnDestroy() { if (boundSim != null) Unsubscribe(boundSim); }

        // ───────────── the recent-events feed (概览) ─────────────

        /// <summary>One line of the 概览 feed: the game time it arrived ("" for lines from before this session) and the text.</summary>
        public struct FeedLine { public string at, text; public int tone; }

        const int FeedLength = 30;
        readonly List<FeedLine> feed = new List<FeedLine>();
        /// <summary>Newest first: the sim's notices (the same lines as the toasts) and the 接线 log's warnings and voices.</summary>
        public IReadOnlyList<FeedLine> Feed => feed;

        void SeedFeed()
        {
            feed.Clear();
            var log = Sim.S.log;
            if (log == null) return;
            for (int i = log.Count - 1; i >= 0 && feed.Count < FeedLength; i--) feed.Add(new FeedLine { at = "", text = log[i] });
        }

        void AddFeed(string text, int tone)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (feed.Count > 0 && feed[0].text == text) return;
            feed.Insert(0, new FeedLine { at = ClockText(), text = text, tone = tone });
            if (feed.Count > FeedLength) feed.RemoveAt(feed.Count - 1);
        }

        /// <summary>The game clock as the feed and the top bar show it (HH:mm).</summary>
        public string ClockText()
        {
            var rt = controller != null ? controller.runtime : null;
            return rt != null && rt.Sim != null ? GameCalendar.Now(rt.Sim.S).ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture) : "";
        }

        void OnWiringLogged(XgWLog l) { if (l != null && l.tone >= 3) AddFeed(l.text, l.tone); }

        // ───────────── unread chat ─────────────

        double chatSeenAt;

        double LastAiLineAt()
        {
            var chat = Sim.S.chat;
            for (int i = chat.Count - 1; i >= 0; i--) if (chat[i].from == "ai") return chat[i].at;
            return 0;
        }

        /// <summary>Its lines since the 对话 page was last on screen.</summary>
        public int UnreadChat
        {
            get
            {
                if (Sim == null) return 0;
                int n = 0;
                var chat = Sim.S.chat;
                for (int i = chat.Count - 1; i >= 0; i--) { if (chat[i].from != "ai") continue; if (chat[i].at <= chatSeenAt) break; n++; }
                return n;
            }
        }

        // ───────────── events → feel ─────────────

        void OnMessage(string text) { AddFeed(text, 0); ShowToast(text, 3.5f); }

        void OnDiverged(int track)
        {
            Juice.Flash(XgDark.Bad, .25f, .55f);
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
            Juice.Float(Juice.At(root), line, XgDark.Gold, 22, 90, 3.5f, 1.1f);
            Refresh(true);
        }

        void OnStageAdvanced(int from)
        {
            Juice.Flash(Color.white, .2f, .8f);
            Juice.Burst(Juice.At(barsRect), 40, XgDark.Gold, XgJuice.Shape.Confetti, 420);
            Juice.Play(XgJuice.Sfx.Id.Fanfare);
            Refresh(true);
        }

        void OnEpoch(XgEpoch e) { Train.OnEpoch(e); }
        void OnAssessed(XgAssessment a) { Train.OnAssessed(a); }
        void OnNodeBought(XgNode n)
        {
            var target = tab == "tree" ? Tree.NodeTarget(n.id) : tab == "items" ? Items.NodeTarget(n.id) : null;
            if (target != null) Juice.TransferCoins(Juice.At(moneyRect), Juice.At(target));
            Tree.OnBought(n); Refresh(true);
        }
        // The desk tabs, card timeouts and 斗图 results belong to the 标注台, which 摆渡众包 hosts (ZhongbaoView).
        void OnDeskOpened(XgDesk d) { Juice.Play(XgJuice.Sfx.Id.Unlock); }

        void OnModelSaved(XgModelEntry m)
        {
            var tabRt = NavButton("repo");
            if (tabRt == null || !tabRt.gameObject.activeInHierarchy) return;
            Juice.Knock(tabRt, .12f);
            if (!m.record) Juice.Float(Juice.At(tabRt, new Vector2(60, 0)), Lang.T("已存入仓库"), XgDark.Good, 18);
            Juice.Play(XgJuice.Sfx.Id.Tick, .7f, .4f);
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
            TickTopBar(dt);
            if (controller.Offline != null && welcome == null) ShowWelcome(controller.Offline);
            refreshTimer -= dt;
            if (refreshTimer <= 0) Refresh(false);
        }

        /// <summary>Lab income per second: contracts plus auto-answer on every desk.</summary>
        public double IncomeRate
        {
            get { double r = Sim.IncomePerSecond; foreach (var l in Sim.S.autoLevels) r += Sim.AutoIncome(l.dataset, Host); return r; }
        }

        void ShowWelcome(XingGuangController.OfflineReport r)
        {
            welcome = Rect("Welcome", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(welcome, new Color(0, 0, 0, .6f));
            var card = ui.Card(welcome, "Card", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-230, -140), new Vector2(230, 140));
            ui.Text(Strip("Title", card, 14, 34, 20, 20), Lang.T("欢迎回来"), 22, XgDark.Good, TextAlignmentOptions.MidlineLeft);
            var t = TimeSpan.FromSeconds(Math.Min(r.seconds, 86400));
            string away = t.TotalHours >= 1 ? T((int)t.TotalHours + " 小时 " + t.Minutes + " 分", (int)t.TotalHours + " h " + t.Minutes + " min") : T(t.Minutes + " 分钟", t.Minutes + " min");
            var body = new System.Text.StringBuilder();
            body.Append(Lang.T("你离开了 ")).Append(away).Append(r.seconds > 86400 ? Lang.T("（最多算 24 小时）") : "").Append(T("。" + AppNames.AiZh + "没闲着：", ". " + AppNames.AiEn + " kept working:")).Append("\n\n");
            body.Append("<size=30><color=#FAC775><b>+¥").Append(Money(r.income)).Append("</b></color></size>\n");
            if (r.labels > 0) body.Append(Lang.T("自动答题标了 ")).Append(N(r.labels, "0")).Append(T(" 条", "")).Append("  ");
            if (r.epochs > 0) body.Append(Lang.T("后台训练 ")).Append(r.epochs).Append(T(" 轮", " epochs")).Append("  ");
            if (r.records > 0) body.Append(Lang.T("刷新纪录 ")).Append(r.records).Append(T(" 次", ""));
            ui.Text(Rect("Body", card, Vector2.zero, Vector2.one, new Vector2(20, 64), new Vector2(-20, -54)), body.ToString(), 16, XgDark.Ink, TextAlignmentOptions.TopLeft);
            var ok = ui.Button(card, Lang.T("收下"), () =>
            {
                Juice.Burst(Juice.At(moneyRect), 30, XgDark.Gold, XgJuice.Shape.Yen, 320);
                Juice.Knock(moneyRect, .2f);
                Juice.Play(XgJuice.Sfx.Id.Coin); Juice.Play(XgJuice.Sfx.Id.Fanfare, 1, .6f);
                controller.Offline = null;
                Destroy(welcome.gameObject); welcome = null;
            }, 18);
            ok.rt.anchorMin = ok.rt.anchorMax = new Vector2(.5f, 0); ok.rt.offsetMin = new Vector2(-90, 14); ok.rt.offsetMax = new Vector2(90, 54);
            ok.Set(Lang.T("收下"), true, XgDark.AccentSoft, XgDark.Good);
            welcome.SetAsLastSibling();
            Juice.BringToFront();
            Juice.Knock(card, .1f, Vector2.zero, .35f);
            Juice.Play(XgJuice.Sfx.Id.Whoosh);
        }

        public bool BuyNode(XgNode node, RectTransform from) => Tree != null && Tree.Buy(node, from);

        /// <summary>Window opened (from the icon, taskbar or a story notification); tab is optional.</summary>
        public void Open(string tabId)
        {
            if (tabId == "vision" || tabId == "sequence") { Sim.SelectedTrack = tabId == "vision" ? XgTrack.Vision : XgTrack.Sequence; tabId = "train"; }
            if (tabId == "research") tabId = "tree";
            if (IsCrowdPage(tabId)) { Refresh(true); controller.OpenCrowd(tabId); return; }
            if (!string.IsNullOrEmpty(tabId) && pages.ContainsKey(tabId)) ShowTab(tabId);
            else Refresh(true);
        }

        /// <summary>Pages that moved to 摆渡众包; asking 灵光 for them opens that app on the page.</summary>
        public static bool IsCrowdPage(string id) => id == "label" || id == "contracts";

        public void ShowTab(string id)
        {
            if (IsCrowdPage(id)) { controller.OpenCrowd(id); return; }
            // 科技 is a section of 道具: it opens with the 道具 page.
            if (!pages.ContainsKey(id) || !FeatureOpen(id == "tree" ? "items" : id)) return;
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
            if (tab == "chat") chatSeenAt = LastAiLineAt();
            if (!FeatureOpen(tab == "tree" ? "items" : tab)) { tab = FirstOpenTab(); foreach (var kv in pages) kv.Value.root.gameObject.SetActive(kv.Key == tab); }
            RefreshTopBar();
            RefreshNav();
            if (pages.TryGetValue(tab, out var page)) page.Refresh();
            setup?.Refresh(controller.runtime);
        }
    }
}
