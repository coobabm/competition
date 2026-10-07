using System;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Core;
using LingGuangV05.Core.Hardware;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.Taohuo;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Miaoyu
{
    /// <summary>
    /// 喵鱼 (design v1.1 §3, §14.3): the 2016 second-hand market, open from stage 3. Sell old cards to get money back
    /// (every listing says 「自用 99 新 无拆无修 不议价」 and the buyers haggle anyway), buy other people's used cards,
    /// and from October buy the closing 网吧's machines, each a case with a GTX 970, for a small cluster (eight of
    /// them add ×3). Used prices follow the hardware timeline: every newer card that matches an old one pushes its
    /// price down. Lives in the desktop's spare native Commander window.
    /// </summary>
    public sealed class MiaoyuView : ShopView
    {
        public const string NativeWindow = "Commander";
        const string NavButtons = "Top Nav Buttons";
        public const int OpenStage = 3;
        public const string Description = "自用 99 新 无拆无修 不议价", DescriptionEn = "Personal use, 99% new, never opened or repaired, price is firm";
        static readonly Color Yellow = new Color32(255, 218, 68, 255), YellowSoft = new Color32(255, 248, 214, 255), Page = new Color32(245, 245, 245, 255),
            Ink = new Color32(40, 40, 40, 255), Muted = new Color32(140, 140, 140, 255), Grey = new Color32(205, 205, 205, 255), Accent = new Color32(255, 196, 0, 255);

        sealed class Buyer { public string nick, nickEn, line, lineEn; public double offer; }
        sealed class Posting
        {
            public string model;
            public Buyer buyer;
            public float nextBuyerAt;
            public int views, wants;
            public string reply = "", replyEn = "";
        }

        readonly Dictionary<int, Posting> postings = new Dictionary<int, Posting>();
        readonly System.Random rng = new System.Random();
        readonly List<(Button button, TMP_Text label, Func<(bool ok, string text)> state)> buyButtons = new List<(Button, TMP_Text, Func<(bool, string)>)>();
        readonly Dictionary<string, (Image fill, TMP_Text label)> tabs = new Dictionary<string, (Image, TMP_Text)>();
        RectTransform holder, list, closed;
        TMP_Text wallet, rig, closedText;
        string tab = "sell", signature = "";
        int version;
        float nextRefresh;

        public static MiaoyuView Instance { get; private set; }

        public static MiaoyuView Install()
        {
            var window = Array.Find(FindObjectsByType<WindowManager>(FindObjectsInactive.Include, FindObjectsSortMode.None), w => w.name == NativeWindow);
            var holder = window != null ? (window.windowContainer != null ? window.windowContainer.Find("Content") : null) ?? window.transform.Find("Container/Content") : null;
            if (holder == null) return null;
            var view = holder.gameObject.GetComponent<MiaoyuView>() ?? holder.gameObject.AddComponent<MiaoyuView>();
            view.window = window; view.holder = (RectTransform)holder;
            GameText.Changed -= view.Relabel; GameText.Changed += view.Relabel;
            view.Relabel();
            Instance = view;
            return view;
        }

        void OnDestroy() { GameText.Changed -= Relabel; if (Instance == this) Instance = null; }

        void Relabel()
        {
            if (window == null) return;
            Rebrand(window, new[] { "Commander", "文件管理器", "喵鱼", "Miaoyu" }, T(AppNames.UsedZh, AppNames.UsedEn), Icon(),
                Lang.T("闲置变现，喵～ 交易只在这台电脑里模拟"),
                GameObject.Find("Desktop List/" + NativeWindow));
            signature = "";
        }

        /// <summary>Opens 喵鱼 on a tab (sell, used, cafe).</summary>
        public void Open(string requested)
        {
            if (window != null && !window.isOn) window.OpenWindow();
            if (requested == "sell" || requested == "used" || requested == "cafe") tab = requested;
            signature = "";
        }

        static Sprite icon;
        /// <summary>A white fish on a yellow tile, drawn once.</summary>
        static Sprite Icon()
        {
            if (icon != null) return icon;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float cx = x - 31.5f, cy = y - 31.5f;
                    float r = 10, ax = Mathf.Max(0, Mathf.Abs(cx) - (29 - r)), ay = Mathf.Max(0, Mathf.Abs(cy) - (29 - r));
                    bool tile = ax * ax + ay * ay <= r * r;
                    bool body = (cx + 4) * (cx + 4) / (17f * 17f) + cy * cy / (10f * 10f) <= 1;
                    bool tail = cx > 10 && cx < 25 && Mathf.Abs(cy) < (cx - 10) * .8f;
                    bool eye = (cx + 13) * (cx + 13) + (cy - 2) * (cy - 2) <= 4.5f;
                    Color c = !tile ? new Color(0, 0, 0, 0) : eye ? new Color32(40, 40, 40, 255) : body || tail ? Color.white : (Color)new Color32(255, 200, 30, 255);
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            icon = Sprite.Create(tex, new UnityEngine.Rect(0, 0, n, n), new Vector2(.5f, .5f));
            icon.hideFlags = HideFlags.DontSave;
            return icon;
        }

        void Start()
        {
            font = PrologueDesk.CjkFont();
            // Commander keeps its minimise / maximise / close buttons inside the content: keep them, above the page.
            Transform nav = transform.Find(NavButtons);
            for (int i = 0; i < transform.childCount; i++) if (transform.GetChild(i) != nav) transform.GetChild(i).gameObject.SetActive(false);
            root = PrologueDesk.Rect("Miaoyu", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (nav != null) { nav.gameObject.SetActive(true); nav.SetAsLastSibling(); }
            PrologueDesk.Fill(root, Page);
            Focus(root.gameObject);
            UiFitScale.Attach(root, window, new Vector2(1266, 763));

            var top = PrologueDesk.Rect("Top", root, new Vector2(0, 1), Vector2.one, new Vector2(0, -64), Vector2.zero);
            PrologueDesk.Fill(top, Yellow);
            Label(top, "Logo", Vector2.zero, new Vector2(0, 1), new Vector2(20, 0), new Vector2(170, 0), "", 32, Ink, TextAlignmentOptions.MidlineLeft).name = "Logo";
            Label(top, "Slogan", Vector2.zero, new Vector2(1, 1), new Vector2(170, 0), new Vector2(-420, 0), "", 16, new Color32(90, 70, 0, 255), TextAlignmentOptions.MidlineLeft).name = "Slogan";
            wallet = Label(top, "Wallet", new Vector2(1, 0), Vector2.one, new Vector2(-400, 0), new Vector2(-18, 0), "", 17, Ink, TextAlignmentOptions.MidlineRight);

            var bar = PrologueDesk.Rect("Tabs", root, new Vector2(0, 1), Vector2.one, new Vector2(0, -106), new Vector2(0, -64));
            PrologueDesk.Fill(bar, Color.white);
            string[] ids = { "sell", "used", "cafe" };
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                var b = Btn(bar, "Tab " + id, Vector2.zero, new Vector2(0, 1), new Vector2(16 + i * 136, 0), new Vector2(16 + i * 136 + 128, 0), Color.white, "", 17, Ink, () => { tab = id; signature = ""; }, out var label);
                tabs[id] = (b.targetGraphic as Image, label);
                var tip = id == "sell" ? ("把旧卡挂出去回血。买家一定会砍价。", "List an old card to get money back. Buyers always haggle.")
                    : id == "used" ? ("别人的二手卡：便宜，算力也低。", "Other people's used cards: cheap, and slow.")
                    : ("网吧倒闭清仓：整机带一张 970，8 台组个小集群。", "A closing net cafe sells its PCs: each has a 970, eight make a small cluster.");
                UiTip.Add(b, tip.Item1, tip.Item2);
            }

            var body = PrologueDesk.Rect("Body", root, Vector2.zero, Vector2.one, new Vector2(0, 30), new Vector2(0, -106));
            var listArea = PrologueDesk.Rect("List", body, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-290, 0));
            list = Scroll(listArea, 14, 10);
            var side = PrologueDesk.Rect("Side", body, new Vector2(1, 0), Vector2.one, new Vector2(-282, 10), new Vector2(-10, -14));
            PrologueDesk.Fill(side, Color.white);
            Label(side, "RigTitle", new Vector2(0, 1), Vector2.one, new Vector2(16, -40), new Vector2(-12, -10), "", 18, Ink, TextAlignmentOptions.MidlineLeft).name = "RigTitle";
            rig = Label(side, "Rig", Vector2.zero, Vector2.one, new Vector2(16, 12), new Vector2(-12, -44), "", 15, Ink);

            closed = PrologueDesk.Rect("Closed", body, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(closed, Page);
            closedText = Label(closed, "Text", Vector2.zero, Vector2.one, new Vector2(60, 60), new Vector2(-60, -60), "", 22, Ink, TextAlignmentOptions.Center);

            var foot = PrologueDesk.Rect("Status", root, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 30));
            PrologueDesk.Fill(foot, new Color32(250, 250, 250, 255));
            status = Label(foot, "Text", Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-16, 0), "", 14, Muted, TextAlignmentOptions.MidlineLeft);
        }

        void Update()
        {
            if (root == null || window == null || !window.isOn) return;
            TickBuyers();
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .4f;
            var sim = Sim;
            var today = Today;
            bool open = Stage >= OpenStage;
            string cards = sim != null ? string.Join(",", sim.Cards) + "|" + sim.S.caseCount + "|" + sim.S.cafeBoxes : "";
            string now = tab + "|" + today.ToString("MMdd") + "|" + open + "|" + GameText.IsEnglish + "|" + cards + "|" + version;
            if (now != signature) { signature = now; Redraw(sim, today, open); }
            wallet.text = sim == null ? "" : Lang.T("钱包 ") + "<b>" + Money(sim.S.money) + "</b>";
            rig.text = RigSummary(sim);
            if (!StatusBusy) status.text = Lang.T("喵鱼提醒：交易请走平台，谨防「先付定金」。");
            foreach (var b in buyButtons)
            {
                var st = b.state();
                b.label.text = st.text;
                ((Image)b.button.targetGraphic).color = st.ok ? Accent : Grey;
            }
        }

        void Redraw(ChapterOneSim sim, DateTime today, bool open)
        {
            var logo = root.Find("Top/Logo")?.GetComponent<TMP_Text>(); if (logo != null) logo.text = "<b>" + T(AppNames.UsedZh, AppNames.UsedEn) + "</b>";
            var slogan = root.Find("Top/Slogan")?.GetComponent<TMP_Text>(); if (slogan != null) slogan.text = Lang.T("闲置变现，喵～");
            var rigTitle = root.Find("Body/Side/RigTitle")?.GetComponent<TMP_Text>(); if (rigTitle != null) rigTitle.text = "<b>" + T("我的电脑", "My computer") + "</b>";
            foreach (var kv in tabs)
            {
                kv.Value.label.text = kv.Key == "sell" ? Lang.T("卖闲置") : kv.Key == "used" ? Lang.T("淘二手") : Lang.T("网吧清仓");
                kv.Value.label.fontStyle = kv.Key == tab ? FontStyles.Bold : FontStyles.Normal;
                kv.Value.fill.color = kv.Key == tab ? YellowSoft : Color.white;
            }
            closed.gameObject.SetActive(!open);
            closedText.text = Lang.T("<size=34><b>喵鱼正在审核你的芝麻信用……</b></size>\n\n第三阶段开放：卖旧显卡回血，收别人的二手卡，\n10 月还有网吧倒闭清仓的整机，凑个小集群。");
            buyButtons.Clear();
            Clear(list);
            if (!open || sim == null) return;
            if (tab == "used") BuildUsed(sim, today);
            else if (tab == "cafe") BuildCafe(sim, today);
            else BuildSell(sim, today);
        }

        // ───────────── sell ─────────────

        void BuildSell(ChapterOneSim sim, DateTime today)
        {
            // Postings whose card moved or went are dropped.
            var stale = new List<int>();
            foreach (var kv in postings) if (kv.Key >= sim.Cards.Count || sim.Cards[kv.Key] != kv.Value.model) stale.Add(kv.Key);
            foreach (int k in stale) postings.Remove(k);
            for (int i = 0; i < sim.Cards.Count; i++)
            {
                var g = HardwareCatalog.Gpu(sim.Cards[i]);
                if (g == null) continue;
                int index = i;
                postings.TryGetValue(i, out var post);
                var row = Row(list, "Card " + i, post != null ? 176 : 132, Color.white);
                CardPicture(row, g, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -118), new Vector2(118, -14));
                double value = HardwareCatalog.UsedPrice(g, today);
                var mid = PrologueDesk.Rect("Mid", row, Vector2.zero, Vector2.one, new Vector2(134, 10), new Vector2(-190, -12));
                Label(mid, "Title", new Vector2(0, 1), Vector2.one, new Vector2(0, -28), Vector2.zero, "<b>" + T(g.name, g.nameEn) + "</b>" + (sim.S.cafeBoxes > 0 && g.id == HardwareCatalog.Gtx970 ? Lang.T("（网吧机里那张）") : ""), 19, Ink);
                Label(mid, "Desc", new Vector2(0, 1), Vector2.one, new Vector2(0, -54), new Vector2(0, -30), T(Description, DescriptionEn), 15, new Color32(90, 90, 90, 255));
                Label(mid, "Stats", new Vector2(0, 1), Vector2.one, new Vector2(0, -80), new Vector2(0, -56), Stats(g) + "   " + Lang.T("新卡上市越多越不值钱"), 14, Muted);
                var right = PrologueDesk.Rect("Right", row, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-176, -118), new Vector2(-14, -12));
                Label(right, "Price", new Vector2(0, 1), Vector2.one, new Vector2(0, -40), Vector2.zero, "<b>" + Money(value) + "</b>", 26, new Color32(255, 80, 0, 255), TextAlignmentOptions.TopRight);
                Label(right, "Hint", new Vector2(0, 1), Vector2.one, new Vector2(0, -62), new Vector2(0, -40), Lang.T("今日估价"), 13, Muted, TextAlignmentOptions.TopRight);
                if (post == null)
                {
                    var b = Btn(right, "Post", Vector2.zero, new Vector2(1, 0), new Vector2(16, 0), new Vector2(0, 42), Accent, "", 17, Ink, () => PostCard(index), out var label);
                    buyButtons.Add((b, label, () =>
                    {
                        var s = Sim;
                        if (s == null || index >= s.Cards.Count) return (false, "—");
                        if (s.S.gpuCount <= 1) return (false, Lang.T("最后一张"));
                        return (true, Lang.T("发布闲置"));
                    }));
                    UiTip.Add(b, "挂出去等买家。至少留一张卡。", "List it and wait for a buyer. Keep at least one card.");
                }
                else BuyerBlock(row, index, post, value);
            }
            if (sim.Cards.Count <= 1)
            {
                var note = Row(list, "Note", 44, new Color(0, 0, 0, 0));
                Label(note, "Text", Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0), Lang.T("只有一张卡可卖不了：先在淘货买张新的，再把旧的挂上来。"), 15, Muted, TextAlignmentOptions.MidlineLeft);
            }
        }

        void BuyerBlock(RectTransform row, int index, Posting post, double value)
        {
            var chat = PrologueDesk.Rect("Chat", row, Vector2.zero, new Vector2(1, 0), new Vector2(134, 10), new Vector2(-14, 52));
            PrologueDesk.Fill(chat, YellowSoft);
            string head = T("浏览 " + post.views + " · 想要 " + post.wants + "   ", post.views + " views · " + post.wants + " want it   ");
            if (post.buyer == null)
            {
                Label(chat, "Wait", Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0), head + (post.reply.Length > 0 ? "<color=#8C8C8C>" + T(post.reply, post.replyEn) + "</color>  " : "") + Lang.T("等买家……"), 15, Ink, TextAlignmentOptions.MidlineLeft);
                return;
            }
            var bu = post.buyer;
            Label(chat, "Line", Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-330, 0), head + "<b>" + T(bu.nick, bu.nickEn) + "</b>：" + T(bu.line, bu.lineEn), 15, Ink, TextAlignmentOptions.MidlineLeft);
            double offer = Math.Min(bu.offer, value);
            Btn(chat, "Deal", new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-322, -16), new Vector2(-170, 16), Accent, Lang.T("卖给他 ") + Money(offer), 15, Ink, () => Accept(index), out _);
            Btn(chat, "Firm", new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-162, -16), new Vector2(-10, 16), Color.white, Lang.T("不议价"), 15, Ink, () => Decline(index), out _);
        }

        void PostCard(int index)
        {
            var sim = Sim;
            if (sim == null || index >= sim.Cards.Count) return;
            if (sim.S.gpuCount <= 1) { Say(Lang.T("至少留一张卡，不然它就停了。")); return; }
            postings[index] = new Posting { model = sim.Cards[index], nextBuyerAt = Time.unscaledTime + 2.5f + (float)rng.NextDouble() * 3, views = 1 + rng.Next(5) };
            Say(T("已发布：「" + Description + "」", "Listed: \"" + DescriptionEn + "\""));
            version++;
        }

        static readonly (string zh, string en)[] Nicks =
        {
            ("学生党小王", "Student Wang"), ("华强北老李", "Old Li from Huaqiangbei"), ("吃鸡少年", "Gamer kid"), ("矿工阿明", "Miner Ming"),
            ("考研狗", "Grad-school hopeful"), ("网吧老板娘", "Net cafe owner"), ("同城面交", "Local, cash only"), ("炼丹萌新", "Deep-learning newbie"),
        };
        static readonly (string zh, string en)[] Haggles =
        {
            ("在吗？{0} 出不出", "Still there? {0}, deal?"),
            ("学生党，{0} 包邮行吗", "I'm a student. {0} with free shipping?"),
            ("能小刀吗？{0} 我马上拍", "A small cut? {0} and I buy right now"),
            ("同城面交 {0}，现金", "Meet locally, {0} cash"),
            ("不会是矿卡吧？{0} 最多了", "Not a mining card, right? {0} max"),
        };
        static readonly (string zh, string en)[] Lowballs =
        {
            ("{0} 卖不卖？不卖算了", "{0}, yes or no? Fine if not"),
            ("{0}，二手的嘛，你懂的", "{0}. It's used, you know"),
        };
        static readonly (string zh, string en)[] Firm =
        {
            ("不议价是吧，行，我拍了。今天能发吗？", "Price is firm? OK, bought. Can you ship today?"),
            ("爽快，原价拍了，别让我等。", "Fine, full price, don't keep me waiting."),
        };
        static readonly (string zh, string en)[] Leaves =
        {
            ("不议价还挂喵鱼？", "Firm price, on Miaoyu?"), ("那算了，祝你早日卖出。", "Never mind, good luck selling it."), ("……已拉黑。", "…blocked."),
        };

        static double Tens(double v) => Math.Max(10, Math.Floor(v / 10) * 10);

        Buyer NewBuyer(double value)
        {
            var nick = Nicks[rng.Next(Nicks.Length)];
            double r = rng.NextDouble();
            (string zh, string en) line; double offer;
            if (r < .25) { line = Firm[rng.Next(Firm.Length)]; offer = value; }
            else if (r < .4) { line = Lowballs[rng.Next(Lowballs.Length)]; offer = Tens(value * (.35 + rng.NextDouble() * .15)); }
            else { line = Haggles[rng.Next(Haggles.Length)]; offer = Tens(value * (.72 + rng.NextDouble() * .18)); }
            string p = Money(offer);
            return new Buyer { nick = nick.zh, nickEn = nick.en, line = string.Format(line.zh, p), lineEn = string.Format(line.en, p), offer = offer };
        }

        void TickBuyers()
        {
            var sim = Sim;
            if (sim == null) return;
            foreach (var kv in postings)
            {
                var post = kv.Value;
                if (post.buyer != null || Time.unscaledTime < post.nextBuyerAt) continue;
                if (kv.Key >= sim.Cards.Count) continue;
                double value = HardwareCatalog.UsedPrice(HardwareCatalog.Gpu(post.model), Today);
                post.buyer = NewBuyer(value);
                post.views += 3 + rng.Next(20); post.wants += 1;
                version++;
            }
        }

        void Accept(int index)
        {
            if (!postings.TryGetValue(index, out var post) || post.buyer == null) return;
            var sim = Sim;
            if (sim == null) return;
            var g = HardwareCatalog.Gpu(post.model);
            double offer = Math.Min(post.buyer.offer, HardwareCatalog.UsedPrice(g, Today));
            string en = "Sold the " + (g != null ? g.nameEn : "card") + " for " + Money(offer) + ".";
            if (!Run(s => s.SellCard(index, offer, Today), en)) return;
            postings.Remove(index);
            // Later cards moved up by one.
            var moved = new Dictionary<int, Posting>();
            foreach (var kv in postings) moved[kv.Key > index ? kv.Key - 1 : kv.Key] = kv.Value;
            postings.Clear();
            foreach (var kv in moved) postings[kv.Key] = kv.Value;
            version++;
        }

        void Decline(int index)
        {
            if (!postings.TryGetValue(index, out var post) || post.buyer == null) return;
            var leave = Leaves[rng.Next(Leaves.Length)];
            post.reply = post.buyer.nick + "：" + leave.zh; post.replyEn = post.buyer.nickEn + ": " + leave.en;
            post.buyer = null;
            post.nextBuyerAt = Time.unscaledTime + 4 + (float)rng.NextDouble() * 4;
            version++;
        }

        // ───────────── buy used ─────────────

        static readonly (string zh, string en)[] Places = { ("广东 深圳", "Shenzhen, Guangdong"), ("北京 海淀", "Haidian, Beijing"), ("四川 成都", "Chengdu, Sichuan"), ("湖北 武汉", "Wuhan, Hubei") };

        void BuildUsed(ChapterOneSim sim, DateTime today)
        {
            for (int i = 0; i < HardwareCatalog.UsedListings.Length; i++)
            {
                var g = HardwareCatalog.Gpu(HardwareCatalog.UsedListings[i]);
                if (g == null || !HardwareCatalog.Released(g, today)) continue;
                var row = Row(list, g.id, 132, Color.white);
                CardPicture(row, g, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(14, -52), new Vector2(118, 52));
                var mid = PrologueDesk.Rect("Mid", row, Vector2.zero, Vector2.one, new Vector2(134, 10), new Vector2(-190, -12));
                var nick = Nicks[(i * 3 + 1) % Nicks.Length]; var place = Places[i % Places.Length];
                Label(mid, "Title", new Vector2(0, 1), Vector2.one, new Vector2(0, -28), Vector2.zero, "<b>" + T(g.title, g.titleEn) + "</b>", 18, Ink);
                Label(mid, "Desc", new Vector2(0, 1), Vector2.one, new Vector2(0, -54), new Vector2(0, -30), T(Description, DescriptionEn), 15, new Color32(90, 90, 90, 255));
                Label(mid, "Who", new Vector2(0, 1), Vector2.one, new Vector2(0, -78), new Vector2(0, -56), T(nick.zh, nick.en) + " · " + T(place.zh, place.en) + "   " + Stats(g), 14, Muted);
                Label(mid, "Pitch", new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 26), T(g.pitch, g.pitchEn), 14, Muted);
                var right = PrologueDesk.Rect("Right", row, new Vector2(1, 0), Vector2.one, new Vector2(-176, 12), new Vector2(-14, -12));
                Label(right, "Price", new Vector2(0, 1), Vector2.one, new Vector2(0, -40), Vector2.zero, "<b>" + Money(HardwareCatalog.UsedPrice(g, today)) + "</b>", 26, new Color32(255, 80, 0, 255), TextAlignmentOptions.TopRight);
                string id = g.id;
                var b = Btn(right, "Buy", Vector2.zero, new Vector2(1, 0), new Vector2(16, 0), new Vector2(0, 42), Accent, "", 17, Ink,
                    () => Run(s => s.BuyUsedCard(id, Today), "Bought a used " + g.nameEn + ". 99% new, they say."), out var label);
                buyButtons.Add((b, label, () =>
                {
                    var s = Sim;
                    if (s == null) return (false, "—");
                    if (!s.SlotFree) return (false, Lang.T("插槽已满"));
                    if (s.S.money + 1e-9 < HardwareCatalog.UsedPrice(g, Today)) return (false, Lang.T("钱不够"));
                    return (true, Lang.T("我想要"));
                }));
                UiTip.Add(b, "便宜，但算力也低。占一个插槽。", "Cheap, but slow. Takes a slot.");
            }
        }

        // ───────────── 网吧 sale ─────────────

        void BuildCafe(ChapterOneSim sim, DateTime today)
        {
            var g = HardwareCatalog.Gpu(HardwareCatalog.Gtx970);
            bool open = today.Date >= HardwareCatalog.CafeRelease;
            var row = Row(list, "Cafe", 190, Color.white);
            var pic = PrologueDesk.Rect("Picture", row, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -134), new Vector2(134, -14));
            PrologueDesk.Fill(pic, new Color32(20, 20, 24, 255), false);
            if (!ProductIllustration(pic, "product_cafebox", Lang.T("网吧清仓 · 配图")))
                Label(pic, "Name", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "<b>" + Lang.T("网吧") + "</b>\n<size=14>×" + HardwareCatalog.CafeBoxLimit + "</size>", 26, new Color32(255, 90, 90, 255), TextAlignmentOptions.Center);
            var mid = PrologueDesk.Rect("Mid", row, Vector2.zero, Vector2.one, new Vector2(150, 10), new Vector2(-190, -12));
            Label(mid, "Title", new Vector2(0, 1), Vector2.one, new Vector2(0, -52), Vector2.zero, Lang.T("<b>【网吧倒闭清仓】E5-2670 + X79 寨板 + 16G + GTX 970 整机，共 8 台</b>"), 18, Ink);
            Label(mid, "Desc", new Vector2(0, 1), Vector2.one, new Vector2(0, -78), new Vector2(0, -54), T(Description, DescriptionEn) + Lang.T("（键盘有点油）"), 15, new Color32(90, 90, 90, 255));
            int left = HardwareCatalog.CafeBoxLimit - sim.S.cafeBoxes;
            Label(mid, "Stats", new Vector2(0, 0), Vector2.one, Vector2.zero, new Vector2(0, -82),
                T("每台自带机箱（2 个插槽）和一张 970：算力 ×" + g.compute.ToString("0.###") + "，功耗 " + g.watts.ToString("0") + "W + 机箱。\n8 台全收是个小集群：×" + (HardwareCatalog.CafeBoxLimit * g.compute).ToString("0.#") + "。已收 " + sim.S.cafeBoxes + " 台，剩 " + left + " 台。",
                  "Each has its own case (2 slots) and a 970: compute ×" + g.compute.ToString("0.###", CultureInfo.InvariantCulture) + ", " + g.watts.ToString("0") + " W plus the case.\nAll eight make a small cluster: ×" + (HardwareCatalog.CafeBoxLimit * g.compute).ToString("0.#", CultureInfo.InvariantCulture) + ". Bought " + sim.S.cafeBoxes + ", " + left + " left.")
                + (open ? "" : "\n<color=#D03030>" + Lang.T("老板：月底关门，10 月 1 号来拉货。") + "</color>"), 15, Ink);
            var right = PrologueDesk.Rect("Right", row, new Vector2(1, 0), Vector2.one, new Vector2(-176, 12), new Vector2(-14, -12));
            Label(right, "Price", new Vector2(0, 1), Vector2.one, new Vector2(0, -40), Vector2.zero, "<b>" + Money(HardwareCatalog.CafeBoxPrice) + "</b>", 26, new Color32(255, 80, 0, 255), TextAlignmentOptions.TopRight);
            Label(right, "Each", new Vector2(0, 1), Vector2.one, new Vector2(0, -62), new Vector2(0, -40), Lang.T("一台"), 13, Muted, TextAlignmentOptions.TopRight);
            var b = Btn(right, "Buy", Vector2.zero, new Vector2(1, 0), new Vector2(16, 0), new Vector2(0, 42), Accent, "", 17, Ink,
                () => Run(s => s.BuyCafeBox(Today), "A net cafe PC is connected. Cluster compute ×" + (((Sim != null ? Sim.S.cafeBoxes : 0) + 1) * g.compute).ToString("0.##", CultureInfo.InvariantCulture) + "."), out var label);
            buyButtons.Add((b, label, () =>
            {
                var s = Sim;
                if (s == null) return (false, "—");
                if (Today.Date < HardwareCatalog.CafeRelease) return (false, Lang.T("10 月清仓"));
                if (s.S.cafeBoxes >= HardwareCatalog.CafeBoxLimit) return (false, Lang.T("已收完"));
                if (s.S.caseCount >= s.Config.maxCases) return (false, Lang.T("放不下了"));
                if (s.S.money + 1e-9 < HardwareCatalog.CafeBoxPrice) return (false, Lang.T("钱不够"));
                return (true, Lang.T("来一台"));
            }));
            UiTip.Add(b, "一台 = 一个机箱 + 一张 970。插槽和电表也算上。", "One = a case and a 970. It counts towards slots and the power meter.");
        }
    }
}
