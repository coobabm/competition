using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using LingGuangV05.Core;
using LingGuangV05.Core.Hardware;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.Taohuo
{
    /// <summary>
    /// 淘货 (design v1.1 §3, §14.3): the 2016 Taobao parody, open from stage 2. It sells the year's graphics cards
    /// on the in-game calendar (GTX 1080 from 5-27, 1070, RX 480, 1060, TITAN X), a second PC case, the 950 Pro
    /// NVMe from September, and the 装机必备 freeware jokes. The GTX 1080 Ti is only ever a pre-order that never
    /// arrives (§10.3). It takes over the old 寻宝 window (router id "xunbao"), so the desktop icon, taskbar and every
    /// router.Open("xunbao") lead here. Secret settings are not sold here.
    /// </summary>
    public sealed partial class TaohuoView : ShopView, IDesktopAppView
    {
        public const string AppId = "xunbao";
        public const int OpenStage = 2;
        static readonly Color Orange = new Color32(255, 80, 0, 255), OrangeSoft = new Color32(255, 240, 230, 255), Page = new Color32(244, 244, 244, 255),
            Ink = new Color32(51, 51, 51, 255), Muted = new Color32(140, 140, 140, 255), Line = new Color32(232, 232, 232, 255), Grey = new Color32(200, 200, 200, 255);

        ChapterOneDesktopBridge binding;
        RectTransform holder, list, body, closed;
        TMP_Text wallet, rig, wangwang, closedText;
        TMP_InputField search;
        string tab = "gpu", signature = "";
        float nextRefresh, nextWang;
        int wangIndex;
        string confirmId = ""; float confirmUntil;
        readonly Dictionary<string, (Image fill, TMP_Text label)> tabs = new Dictionary<string, (Image, TMP_Text)>();
        readonly List<(Button button, TMP_Text label, Func<(bool ok, string text)> state)> buyButtons = new List<(Button, TMP_Text, Func<(bool, string)>)>();

        public static TaohuoView Instance { get; private set; }

        public static TaohuoView Install(ChapterOneDesktopRouter router)
        {
            var binding = router != null ? router.Get(AppId) : null;
            if (binding == null || binding.Window == null) return null;
            var window = binding.Window;
            XingGuangController.KeepOpenOnFirstStart(window);
            var content = (window.windowContainer != null ? window.windowContainer.Find("Content") as RectTransform : null) ?? binding.Content;
            if (content == null) return null;
            var view = content.GetComponent<TaohuoView>() ?? content.gameObject.AddComponent<TaohuoView>();
            view.binding = binding; view.window = window; view.holder = content;
            binding.UseCustomView(view);
            ShopNotices.Install(router.gameObject);
            GameText.Changed -= view.Relabel; GameText.Changed += view.Relabel;
            view.Relabel();
            Instance = view;
            return view;
        }

        void OnDestroy() { GameText.Changed -= Relabel; if (Instance == this) Instance = null; }

        void Relabel()
        {
            Rebrand(window, new[] { "寻宝", "Xunbao", "淘货", "Taohuo" }, T(AppNames.ShopZh, AppNames.ShopEn), null,
                T("亲，本店所有交易都在这台电脑里模拟完成，不连真的网", "Dear, every order is simulated on this computer, no real shopping"),
                binding != null && binding.DesktopShortcut != null ? binding.DesktopShortcut.gameObject : null,
                binding != null && binding.TaskbarShortcut != null ? binding.TaskbarShortcut.gameObject : null);
            signature = "";
        }

        // ───────────── IDesktopAppView ─────────────

        public bool EnsureReady() { Build(); return root != null; }

        public void OnOpened(string requested)
        {
            Build();
            XingGuangController.EnsureShown(this, window);
            if (requested == "data" || requested == "gpu" || requested == "pc" || requested == "disk" || requested == "soft" || requested == GiftTab) Show(requested);
            signature = "";
        }

        /// <summary>Opens 淘货 on a tab (gpu, pc, disk, soft, data).</summary>
        public void Open(string requested)
        {
            if (binding != null) binding.OpenApp(requested);
        }

        void Build()
        {
            if (root != null || holder == null) return;
            font = PrologueDesk.CjkFont();
            foreach (var old in holder.GetComponents<MonoBehaviour>())
                if (old is ChapterOneNativeAppView || old.GetType().Name == "ChapterOneApp") old.enabled = false;
            for (int i = 0; i < holder.childCount; i++) holder.GetChild(i).gameObject.SetActive(false);
            var bg = holder.GetComponent<Image>(); if (bg != null) bg.color = Page;

            root = PrologueDesk.Rect("Taohuo", holder, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(root, Page);
            Focus(root.gameObject);
            UiFitScale.Attach(root, window, new Vector2(1266, 763));

            var top = PrologueDesk.Rect("Top", root, new Vector2(0, 1), Vector2.one, new Vector2(0, -64), Vector2.zero);
            PrologueDesk.Fill(top, Orange);
            Label(top, "Logo", Vector2.zero, new Vector2(0, 1), new Vector2(20, 14), new Vector2(150, -4), "", 32, Color.white, TextAlignmentOptions.MidlineLeft).name = "Logo";
            Label(top, "Domain", Vector2.zero, new Vector2(0, 0), new Vector2(22, 4), new Vector2(160, 20), "taohuo.com", 12, new Color(1, 1, 1, .85f), TextAlignmentOptions.BottomLeft);
            var box = PrologueDesk.Rect("Search", top, Vector2.zero, Vector2.one, new Vector2(170, 13), new Vector2(-380, -13));
            PrologueDesk.Fill(box, Color.white);
            var area = PrologueDesk.Rect("Text Area", box, Vector2.zero, Vector2.one, new Vector2(12, 2), new Vector2(-12, -2));
            area.gameObject.AddComponent<RectMask2D>();
            var ph = Text(PrologueDesk.Rect("Placeholder", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 16, Muted, TextAlignmentOptions.MidlineLeft);
            var tx = Text(PrologueDesk.Rect("Text", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "", 16, Ink, TextAlignmentOptions.MidlineLeft);
            tx.richText = false; tx.textWrappingMode = TextWrappingModes.NoWrap;
            search = box.gameObject.AddComponent<TMP_InputField>();
            search.textViewport = area; search.textComponent = tx; search.placeholder = ph; search.fontAsset = font; search.pointSize = 16;
            search.characterLimit = 30; search.lineType = TMP_InputField.LineType.SingleLine; search.richText = false;
            search.onValueChanged.AddListener(_ => signature = "");
            Focus(box.gameObject);
            wallet = Label(top, "Wallet", new Vector2(1, 0), Vector2.one, new Vector2(-370, 0), new Vector2(-18, 0), "", 17, Color.white, TextAlignmentOptions.MidlineRight);

            var bar = PrologueDesk.Rect("Tabs", root, new Vector2(0, 1), Vector2.one, new Vector2(0, -106), new Vector2(0, -64));
            PrologueDesk.Fill(bar, Color.white);
            string[] ids = { "gpu", "pc", "disk", "data", GiftTab, "soft" };
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                var b = Btn(bar, "Tab " + id, new Vector2(0, 0), new Vector2(0, 1), new Vector2(16 + i * 128, 0), new Vector2(16 + i * 128 + 120, 0), Color.white, "", 17, Ink, () => Show(id), out var label);
                tabs[id] = (b.targetGraphic as Image, label);
                TipFor(b, TabTip(id));
            }

            body = PrologueDesk.Rect("Body", root, Vector2.zero, Vector2.one, new Vector2(0, 30), new Vector2(0, -106));
            var listArea = PrologueDesk.Rect("List", body, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-300, 0));
            list = Scroll(listArea, 14, 10);
            var side = PrologueDesk.Rect("Side", body, new Vector2(1, 0), Vector2.one, new Vector2(-292, 10), new Vector2(-10, -14));
            PrologueDesk.Fill(side, Color.white);
            Label(side, "RigTitle", new Vector2(0, 1), Vector2.one, new Vector2(16, -40), new Vector2(-12, -10), "", 18, Ink, TextAlignmentOptions.MidlineLeft).name = "RigTitle";
            rig = Label(side, "Rig", new Vector2(0, .38f), Vector2.one, new Vector2(16, 0), new Vector2(-12, -44), "", 15, Ink);
            UiTip.Add(rig, "每张卡的算力倍率相加就是总算力，显存和功耗也相加。一个机箱插两张卡。", "Every card's multiplier adds up to the compute; VRAM and watts add up too. A case holds two cards.");
            var wang = PrologueDesk.Rect("Wangwang", side, Vector2.zero, new Vector2(1, .38f), new Vector2(10, 10), new Vector2(-10, -6));
            PrologueDesk.Fill(wang, OrangeSoft);
            Label(wang, "WangTitle", new Vector2(0, 1), Vector2.one, new Vector2(12, -32), new Vector2(-8, -6), "", 15, Orange, TextAlignmentOptions.MidlineLeft).name = "WangTitle";
            wangwang = Label(wang, "WangText", Vector2.zero, Vector2.one, new Vector2(12, 8), new Vector2(-10, -34), "", 15, Ink);

            closed = PrologueDesk.Rect("Closed", body, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(closed, Page);
            closedText = Label(closed, "Text", Vector2.zero, Vector2.one, new Vector2(60, 60), new Vector2(-60, -60), "", 22, Ink, TextAlignmentOptions.Center);

            var foot = PrologueDesk.Rect("Status", root, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 30));
            PrologueDesk.Fill(foot, new Color32(250, 250, 250, 255));
            status = Label(foot, "Text", Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-16, 0), "", 14, Muted, TextAlignmentOptions.MidlineLeft);
            Show(tab);
        }

        static (string zh, string en) TabTip(string id)
        {
            switch (id)
            {
                case "gpu": return ("新显卡按日历上市。算力倍率越高训练越快，显存决定模型能多大。", "New cards come out on the calendar. A higher multiplier trains faster; VRAM decides how big a model fits.");
                case "pc": return ("多一个机箱就多两个显卡插槽，卡分开放也更凉快。", "Another case adds two card slots and keeps the cards cooler.");
                case "disk": return ("9 月上市的 NVMe 固态：数据读得快，所有卡训练快 10%。", "The NVMe drive from September: data loads faster, every card trains 10% faster.");
                case "data": return ("数据包：公开包、杂包、大包。便宜的脏，贵的干净。", "Data packs: public, bulk and big. Cheap ones are dirty, dear ones are clean.");
                case GiftTab: return GiftTabTip;
                default: return ("2016 年的装机必备。", "The 2016 must-have installers.");
            }
        }

        static UiTip TipFor(Component c, (string zh, string en) t) => UiTip.Add(c, t.zh, t.en);

        void Show(string id)
        {
            if (id == "data" && !HasData) id = "gpu";
            if (id == GiftTab && !HasGifts) id = "gpu";
            tab = id; signature = "";
        }

        // ───────────── refresh ─────────────

        void Update()
        {
            if (root == null || window == null || !window.isOn) return;
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .4f;
            var sim = Sim;
            var today = Today;
            bool open = Stage >= OpenStage;
            bool data = HasData;
            string cards = sim != null ? string.Join(",", sim.Cards) + "|" + sim.S.caseCount + "|" + sim.S.nvme : "";
            string now = tab + "|" + today.ToString("MMdd") + "|" + open + "|" + data + "|" + GameText.IsEnglish + "|" + cards + "|" + (search != null ? search.text : "") + "|" + (tab == "data" ? DataSignature() : "") + (tab == GiftTab ? GiftSignature() : "");
            if (now != signature) { signature = now; Redraw(sim, today, open, data); }
            UpdateLive(sim);
        }

        void Redraw(ChapterOneSim sim, DateTime today, bool open, bool data)
        {
            var logo = root.Find("Top/Logo")?.GetComponent<TMP_Text>(); if (logo != null) logo.text = "<b>" + T(AppNames.ShopZh, AppNames.ShopEn) + "</b>";
            var rigTitle = root.Find("Body/Side/RigTitle")?.GetComponent<TMP_Text>(); if (rigTitle != null) rigTitle.text = "<b>" + T("我的电脑", "My computer") + "</b>";
            var wangTitle = root.Find("Body/Side/Wangwang/WangTitle")?.GetComponent<TMP_Text>(); if (wangTitle != null) wangTitle.text = "<b>" + T("旺旺 · 亮影旗舰店客服", "WangWang · Liangying store support") + "</b>";
            ((TMP_Text)search.placeholder).text = T("搜索宝贝：1080、机箱、固态……", "Search: 1080, case, SSD…");
            foreach (var kv in tabs)
            {
                kv.Value.label.text = TabName(kv.Key);
                kv.Value.label.color = kv.Key == tab ? Orange : Ink;
                kv.Value.label.fontStyle = kv.Key == tab ? FontStyles.Bold : FontStyles.Normal;
                kv.Value.fill.color = kv.Key == tab ? OrangeSoft : Color.white;
                kv.Value.fill.transform.gameObject.SetActive((kv.Key != "data" || data) && (kv.Key != GiftTab || HasGifts));
            }
            closed.gameObject.SetActive(!open);
            closedText.text = T("<size=34><b>亲，淘货正在装修中～</b></size>\n\n第二阶段开张：显卡、机箱、固态硬盘、数据包都在这里买。\n本店<color=#FF5000>不卖</color>秘籍哦。\n\n先去标注台赚点钱吧，开张了旺旺叫您～",
                "<size=34><b>Dear, Taohuo is being renovated~</b></size>\n\nOpening at stage 2: graphics cards, cases, SSDs and data packs.\nWe do <color=#FF5000>not</color> sell secret settings.\n\nGo earn some money at the label desk first. We'll message you when we open~");
            buyButtons.Clear();
            Clear(list);
            if (!open) return;
            string q = search != null ? search.text.Trim() : "";
            switch (tab)
            {
                case "pc": BuildPc(sim, today); break;
                case "disk": BuildDisk(sim, today); break;
                case "soft": BuildSoft(); break;
                case "data": BuildData(q); break;
                case GiftTab: BuildGifts(today); break;
                default: BuildGpus(sim, today, q); break;
            }
        }

        static string TabName(string id)
        {
            switch (id)
            {
                case "gpu": return T("显卡", "GPUs");
                case "pc": return T("整机 · 机箱", "PCs · cases");
                case "disk": return T("存储", "Storage");
                case "data": return T("数据", "Data");
                case GiftTab: return T("送她", "For her");
                default: return T("装机必备", "Must-haves");
            }
        }

        void UpdateLive(ChapterOneSim sim)
        {
            wallet.text = sim == null ? "" : T("亲，欢迎来淘货！  钱包 ", "Welcome, dear!  Wallet ") + "<b>" + Money(sim.S.money) + "</b>";
            rig.text = RigSummary(sim);
            if (Time.unscaledTime >= nextWang) { nextWang = Time.unscaledTime + 7; wangIndex++; }
            wangwang.text = WangLine(wangIndex, sim);
            if (!StatusBusy) status.text = T("亲，所有宝贝包邮，下单即到，装好就能跑。", "Dear, free shipping on everything; it arrives at once and is installed for you.");
            foreach (var b in buyButtons)
            {
                var st = b.state();
                b.label.text = st.text;
                ((Image)b.button.targetGraphic).color = st.ok ? Orange : Grey;
            }
        }

        static readonly (string zh, string en)[] Wang =
        {
            ("亲，在的哦～有什么可以帮您？", "Hi dear, I'm here~ How can I help?"),
            ("亲，今天下单今天到，顺丰包邮哦～", "Dear, order today and it arrives today, free SF shipping~"),
            ("亲，给个五星好评返现 5 元哦～", "Dear, a five-star review gets you ¥5 back~"),
            ("亲，显卡都是全新未拆封的，假一赔十～", "Dear, all cards are brand new and sealed, ten back for a fake~"),
            ("亲，1080 Ti 的话厂家还没通知哦，可以先收藏～", "Dear, the factory hasn't announced the 1080 Ti yet, add it to favourites~"),
            ("亲，插槽满了可以把旧卡挂到喵鱼上回回血哦～", "Dear, if your slots are full, sell the old card on Miaoyu~"),
            ("亲，两张卡放一个机箱会热，再买个机箱更稳哦～", "Dear, two cards in one case run hot, another case keeps them cool~"),
        };

        static string WangLine(int i, ChapterOneSim sim)
        {
            var w = Wang[((i % Wang.Length) + Wang.Length) % Wang.Length];
            return "<color=#FF5000>" + T("亮影旗舰店", "Liangying store") + "</color>  " + T("说：", "says:") + "\n" + T(w.zh, w.en);
        }

        // ───────────── tabs ─────────────

        void BuildGpus(ChapterOneSim sim, DateTime today, string q)
        {
            if (today.Date == HardwareCatalog.SinglesDay)
            {
                var banner = Row(list, "SinglesDay", 54, Orange);
                Label(banner, "Text", Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-18, 0), T("<b>双 11 狂欢：全场显卡 8 折！只限今天！</b>", "<b>Singles' Day: every card 20% off, today only!</b>"), 22, Color.white, TextAlignmentOptions.MidlineLeft);
            }
            int shown = 0;
            foreach (var g in HardwareCatalog.Listings(today))
            {
                if (q.Length > 0 && (g.name + g.nameEn + g.title + g.titleEn).IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                GpuRow(g, today);
                shown++;
            }
            if (shown == 0) Note(q.Length > 0 ? T("没有找到「" + q + "」相关的宝贝，换个词试试～", "No items for \"" + q + "\". Try another word~") : T("新卡还没上市。", "No new cards yet."));
        }

        void GpuRow(GpuModel g, DateTime today)
        {
            var row = Row(list, g.id, 168, Color.white);
            CardPicture(row, g, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(14, -66), new Vector2(146, 66));
            var mid = PrologueDesk.Rect("Mid", row, Vector2.zero, Vector2.one, new Vector2(162, 10), new Vector2(-200, -10));
            Label(mid, "Title", new Vector2(0, 1), Vector2.one, new Vector2(0, -50), Vector2.zero, T(g.title, g.titleEn), 18, Ink);
            string badges = g.rumour
                ? "<color=#999999>" + T("[预约]  [到货时间待定]", "[Pre-order]  [Date unknown]") + "</color>"
                : "<color=#FF5000>" + T("[包邮]", "[Free shipping]") + "</color>  <color=#3C8CE7>" + T("[淘货自营]", "[Taohuo official]") + "</color>  <color=#999999>" + T("[7 天无理由]", "[7-day returns]") + "</color>";
            Label(mid, "Badges", new Vector2(0, 1), Vector2.one, new Vector2(0, -74), new Vector2(0, -52), badges, 14, Ink);
            Label(mid, "Stats", new Vector2(0, 1), Vector2.one, new Vector2(0, -98), new Vector2(0, -76), g.rumour ? T("传闻：算力 ×2.2 · 显存 11G", "Rumour: compute ×2.2 · VRAM 11G") : Stats(g) + "  ·  " + T(g.pitch, g.pitchEn), 15, g.rumour ? Muted : Ink);
            var review = Review(g, today);
            Label(mid, "Review", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 26), new Vector2(0, -102), review, 14, Muted);
            Label(mid, "Ask", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 24), Ask(g), 14, Muted);
            var right = PrologueDesk.Rect("Right", row, new Vector2(1, 0), Vector2.one, new Vector2(-190, 12), new Vector2(-14, -12));
            double price = HardwareCatalog.Price(g, today);
            Label(right, "Price", new Vector2(0, 1), Vector2.one, new Vector2(0, -44), Vector2.zero, g.rumour ? "<b>¥ ????</b>" : "<b>" + Money(price) + "</b>" + (price < g.price ? "  <s><size=14><color=#999999>" + Money(g.price) + "</color></size></s>" : ""), 28, Orange, TextAlignmentOptions.TopRight);
            int days = Math.Max(0, (today.Date - g.release.Date).Days);
            Label(right, "Sold", new Vector2(0, 1), Vector2.one, new Vector2(0, -70), new Vector2(0, -46), g.rumour ? T("已有 23333 人想要", "23,333 people want this") : T("月销 ", "") + (g.sold + days * 37).ToString("#,0", CultureInfo.InvariantCulture) + T(" 笔", " sold this month"), 14, Muted, TextAlignmentOptions.TopRight);
            string id = g.id;
            var b = Btn(right, "Buy", new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 0), new Vector2(0, 44), Orange, "", 18, Color.white, () => BuyGpu(id), out var label);
            if (g.rumour) buyButtons.Add((b, label, () => (true, T("到货通知我", "Notify me"))));
            else buyButtons.Add((b, label, () => GpuState(id)));
            TipFor(b, g.rumour ? ("厂家还没发布。", "Not announced yet.") : ("买下立刻装进机箱：算力、显存、功耗都加上去。", "Bought cards go straight into a slot: compute, VRAM and watts all add up."));
        }

        (bool, string) GpuState(string id)
        {
            var sim = Sim;
            var g = HardwareCatalog.Gpu(id);
            if (sim == null || g == null) return (false, "—");
            if (confirmId == id && Time.unscaledTime < confirmUntil) return (true, T("钱包警告：确定？", "Wallet warning: sure?"));
            if (!sim.SlotFree) return (false, T("插槽已满", "Slots full"));
            if (sim.S.money + 1e-9 < HardwareCatalog.Price(g, Today)) return (false, T("钱不够", "Not enough ¥"));
            return (true, T("立即购买", "Buy now"));
        }

        void BuyGpu(string id)
        {
            var g = HardwareCatalog.Gpu(id);
            if (g == null) return;
            if (g.rumour) { Say(T("亲，已登记，到货第一时间通知您～（厂家还没通知哦）", "Dear, you're on the list, we'll tell you the moment it arrives~ (the factory hasn't said when)")); return; }
            // §14.3: the TITAN X pops a wallet warning first.
            if (id == HardwareCatalog.TitanXp && !(confirmId == id && Time.unscaledTime < confirmUntil))
            {
                confirmId = id; confirmUntil = Time.unscaledTime + 5;
                Say(T("钱包警告：这张卡比整台电脑还贵。再点一次确认购买。", "Wallet warning: this card costs more than the whole computer. Click again to buy."));
                return;
            }
            confirmId = "";
            Run(sim => sim.BuyCard(id, Today), "Your " + g.nameEn + " is installed: compute ×" + g.compute.ToString("0.##", CultureInfo.InvariantCulture) + ", VRAM +" + (g.vramMB / 1024).ToString("0.#", CultureInfo.InvariantCulture) + "G.");
        }

        static readonly (string zh, string en)[] Reviews =
        {
            ("卖家发货超快，顺丰第二天就到了，鲁大师跑分 {0}%，好评！", "Shipped fast, here the next day by SF. Master Lu says {0}%. Five stars!"),
            ("包装很严实，就是旧机箱塞不下了，又买了个机箱。", "Well packed. My old case was full, so I bought another case."),
            ("炼丹神器，loss 降得飞快。", "Great for training, the loss drops fast."),
            ("客服小姐姐很耐心，亲来亲去的。", "Support was very patient, lots of 'dear'."),
            ("给好评返 5 元，五星。", "Five stars for the ¥5 cashback."),
            ("守望先锋 1080P 全特效稳定 144 帧。", "Overwatch at 1080p ultra holds 144 fps."),
        };

        static string Review(GpuModel g, DateTime today)
        {
            if (g.rumour) return T("还没有评价。", "No reviews yet.");
            string zh, en;
            if (g.id == HardwareCatalog.Rx480 && today.Day % 2 == 0) { zh = "装上开机一股焦味，换了个电源就好了。A 卡战未来！"; en = "Smelled burnt on first boot, a new PSU fixed it. AMD is the future!"; }
            else if (g.id == HardwareCatalog.TitanXp && today.Day % 2 == 0) { zh = "老婆以为是 999 买的，求别说破。"; en = "My wife thinks it cost 999. Please don't tell her."; }
            else
            {
                int i = Math.Abs((g.id.GetHashCode() & 0x7fff) + today.DayOfYear) % Reviews.Length;
                string pct = HardwareCatalog.LudashiPercent(g.id).ToString("0.#", CultureInfo.InvariantCulture);
                zh = string.Format(Reviews[i].zh, pct); en = string.Format(Reviews[i].en, pct);
            }
            string nick = new[] { "t***8", "小***猫", "炼***人", "z***9", "等***党" }[Math.Abs(g.id.Length + today.Day) % 5];
            return "<color=#FFAA00>★★★★★</color> " + T("「" + zh + "」", "\"" + en + "\"") + " — " + nick;
        }

        static string Ask(GpuModel g)
        {
            // §10.3: the 1080 Ti only ever comes up as a question.
            if (g.id == HardwareCatalog.Gtx1080 || g.rumour)
                return T("问大家：1080 Ti 什么时候出？  答：亲，厂家还没通知哦，等等党永远不亏～", "Q: When is the 1080 Ti coming out?  A: Dear, the factory hasn't said, waiting never hurts~");
            if (g.id == HardwareCatalog.Rx480)
                return T("问大家：能跑深度学习吗？  答：亲，A 卡的话……建议您咨询一下老周哦～", "Q: Can it do deep learning?  A: Dear, for AMD cards… please ask Lao Zhou~");
            return T("问大家：能跑深度学习吗？  答：亲，能的哦，CUDA 都支持～", "Q: Can it do deep learning?  A: Dear, yes, CUDA is fully supported~");
        }

        void BuildPc(ChapterOneSim sim, DateTime today)
        {
            var row = Row(list, "Case", 150, Color.white);
            var pic = PrologueDesk.Rect("Picture", row, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(14, -60), new Vector2(146, 60));
            PrologueDesk.Fill(pic, new Color32(30, 30, 30, 255), false);
            Label(pic, "Name", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "<b>PC</b>", 30, new Color32(120, 220, 255, 255), TextAlignmentOptions.Center);
            var mid = PrologueDesk.Rect("Mid", row, Vector2.zero, Vector2.one, new Vector2(162, 10), new Vector2(-200, -10));
            Label(mid, "Title", new Vector2(0, 1), Vector2.one, new Vector2(0, -50), Vector2.zero, T("【准系统】游戏悍将机箱 + Z170 主板 + 600W 电源 + 16G 内存（不含显卡）", "[Barebone] Gaming case + Z170 board + 600 W PSU + 16 GB RAM (no graphics card)"), 18, Ink);
            Label(mid, "Badges", new Vector2(0, 1), Vector2.one, new Vector2(0, -74), new Vector2(0, -52), "<color=#FF5000>" + T("[包邮]", "[Free shipping]") + "</color>  <color=#3C8CE7>" + T("[装好再发]", "[Assembled]") + "</color>", 14, Ink);
            Label(mid, "Stats", new Vector2(0, 0), Vector2.one, new Vector2(0, 0), new Vector2(0, -78),
                T("多 2 个显卡插槽。卡分开放，温度更低。机箱本身 " + (sim != null ? sim.Config.caseWatts.ToString("0") : "45") + "W。", "Two more card slots. Spread out, the cards run cooler. The case itself draws " + (sim != null ? sim.Config.caseWatts.ToString("0") : "45") + " W."), 15, Ink);
            var right = PrologueDesk.Rect("Right", row, new Vector2(1, 0), Vector2.one, new Vector2(-190, 12), new Vector2(-14, -12));
            double price = sim != null ? sim.Config.casePrice : 2000;
            Label(right, "Price", new Vector2(0, 1), Vector2.one, new Vector2(0, -44), Vector2.zero, "<b>" + Money(price) + "</b>", 28, Orange, TextAlignmentOptions.TopRight);
            Label(right, "Sold", new Vector2(0, 1), Vector2.one, new Vector2(0, -70), new Vector2(0, -46), T("月销 642 笔", "642 sold this month"), 14, Muted, TextAlignmentOptions.TopRight);
            var b = Btn(right, "Buy", new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 0), new Vector2(0, 44), Orange, "", 18, Color.white,
                () => Run(s => s.BuyCase(), "A new case is connected: two more slots."), out var label);
            buyButtons.Add((b, label, () =>
            {
                var s = Sim;
                if (s == null) return (false, "—");
                if (s.S.caseCount >= s.Config.maxCases) return (false, T("放不下了", "No room"));
                if (s.S.money + 1e-9 < s.Config.casePrice) return (false, T("钱不够", "Not enough ¥"));
                return (true, T("立即购买", "Buy now"));
            }));
            TipFor(b, ("一个机箱两个插槽。插槽满了，要么买机箱，要么去喵鱼卖旧卡。", "Each case has two slots. When they're full, buy a case or sell an old card on Miaoyu."));
            Note(T("想要便宜的？喵鱼上 10 月有网吧倒闭清仓的整机，自带一张 970。", "On a budget? In October Miaoyu has whole PCs from a closing net cafe, each with a 970 inside."));
        }

        void BuildDisk(ChapterOneSim sim, DateTime today)
        {
            var row = Row(list, "Nvme", 150, Color.white);
            var pic = PrologueDesk.Rect("Picture", row, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(14, -60), new Vector2(146, 60));
            PrologueDesk.Fill(pic, new Color32(20, 40, 90, 255), false);
            Label(pic, "Name", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "<b>950 PRO</b>\n<size=14>NVMe M.2</size>", 22, Color.white, TextAlignmentOptions.Center);
            bool released = today.Date >= HardwareCatalog.NvmeRelease;
            var mid = PrologueDesk.Rect("Mid", row, Vector2.zero, Vector2.one, new Vector2(162, 10), new Vector2(-200, -10));
            Label(mid, "Title", new Vector2(0, 1), Vector2.one, new Vector2(0, -50), Vector2.zero, T("三星 950 Pro 256G NVMe M.2 固态 读取 2500MB/s", "Samsung 950 Pro 256 GB NVMe M.2 SSD, reads 2,500 MB/s"), 18, Ink);
            Label(mid, "Badges", new Vector2(0, 1), Vector2.one, new Vector2(0, -74), new Vector2(0, -52), released ? "<color=#FF5000>" + T("[包邮]", "[Free shipping]") + "</color>  <color=#3C8CE7>" + T("[淘货自营]", "[Taohuo official]") + "</color>" : "<color=#999999>" + T("[9 月 1 日到货]", "[Arrives 1 September]") + "</color>", 14, Ink);
            Label(mid, "Stats", new Vector2(0, 0), Vector2.one, new Vector2(0, 0), new Vector2(0, -78),
                T("数据集从固态读，显卡不再干等：所有卡训练 ×" + HardwareCatalog.NvmeBoost.ToString("0.0#") + "。只能装一块。\n<color=#8C8C8C>「装了以后开机 8 秒，进系统前我都没来得及眨眼。」— 等***党</color>",
                  "Datasets load from the SSD, so the cards stop waiting: every card trains ×" + HardwareCatalog.NvmeBoost.ToString("0.0#") + ". One per computer.\n<color=#8C8C8C>\"Boots in 8 seconds. I didn't have time to blink.\" — w***r</color>"), 15, Ink);
            var right = PrologueDesk.Rect("Right", row, new Vector2(1, 0), Vector2.one, new Vector2(-190, 12), new Vector2(-14, -12));
            Label(right, "Price", new Vector2(0, 1), Vector2.one, new Vector2(0, -44), Vector2.zero, "<b>" + Money(HardwareCatalog.NvmePrice) + "</b>", 28, Orange, TextAlignmentOptions.TopRight);
            var b = Btn(right, "Buy", new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 0), new Vector2(0, 44), Orange, "", 18, Color.white,
                () => Run(s => s.BuyNvme(Today), "The 950 Pro is installed: every card trains 10% faster."), out var label);
            buyButtons.Add((b, label, () =>
            {
                var s = Sim;
                if (s == null) return (false, "—");
                if (s.S.nvme) return (false, T("已安装", "Installed"));
                if (Today.Date < HardwareCatalog.NvmeRelease) return (false, T("9 月到货", "September"));
                if (s.S.money + 1e-9 < HardwareCatalog.NvmePrice) return (false, T("钱不够", "Not enough ¥"));
                return (true, T("立即购买", "Buy now"));
            }));
            Note(T("本机自带：240G 固态（系统盘，已经快满了）。", "Already inside: a 240 GB SSD (the system drive, almost full)."));
        }

        static readonly (string name, string nameEn, string zh, string en)[] Soft =
        {
            ("驱动精灵 2016", "Driver Genius 2016", "驱动精灵：检测到 1 个驱动可升级……已顺手为您安装 3 款推荐软件。", "Driver Genius: 1 driver can be updated… and 3 recommended apps were installed for you."),
            ("2345 好压", "2345 HaoZip", "2345 好压安装完成。浏览器主页已为您锁定为 2345 网址导航。", "2345 HaoZip installed. Your browser home page is now locked to the 2345 portal."),
            ("大白菜 U 盘启动", "Dabaicai USB boot maker", "大白菜 U 盘启动盘制作成功。你没有 U 盘。", "Boot USB created. You don't have a USB stick."),
            ("一键 Ghost", "One-click Ghost", "一键 Ghost：C 盘已备份。还原点：它还不会说话的时候。", "One-click Ghost: drive C backed up. Restore point: before it could talk."),
            ("番茄花园 Win7 旗舰版", "Tomato Garden Win7 Ultimate", "番茄花园：此版本已停止维护。建议升级 Windows 10（7 月 29 日前免费）。", "Tomato Garden: this edition is no longer maintained. Upgrade to Windows 10 (free until 29 July)."),
        };

        void BuildSoft()
        {
            foreach (var s in Soft)
            {
                var row = Row(list, s.nameEn, 64, Color.white);
                Label(row, "Name", Vector2.zero, Vector2.one, new Vector2(18, 0), new Vector2(-200, 0), "<b>" + T(s.name, s.nameEn) + "</b>   <color=#8C8C8C>" + T("装机必备 · 免费", "Must-have · free") + "</color>", 17, Ink, TextAlignmentOptions.MidlineLeft);
                var line = (s.zh, s.en);
                Btn(row, "Get", new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-150, -20), new Vector2(-18, 20), new Color32(60, 140, 231, 255), T("免费下载", "Download"), 16, Color.white, () => Say(T(line.zh, line.en), 8), out _);
            }
            Note(T("提示：安装时记得取消勾选捆绑软件。", "Tip: untick the bundled extras when you install."));
        }

        void Note(string text)
        {
            var row = Row(list, "Note", 44, new Color(0, 0, 0, 0));
            Label(row, "Text", Vector2.zero, Vector2.one, new Vector2(8, 0), new Vector2(-8, 0), text, 15, Muted, TextAlignmentOptions.MidlineLeft);
        }
    }
}
