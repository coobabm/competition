using System;
using System.Collections.Generic;
using LingGuangV05.Core;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.Zhongbao
{
    /// <summary>
    /// 摆渡众包 (Bodu Crowd), the in-world Baidu's crowd-labelling platform: one window with a status strip (余额, 信用,
    /// 等级, 今日热词 and the shared combo), a left nav and five pages. 标注台 and 企业订单 are the lab's own
    /// <see cref="XgLabelPage"/> and <see cref="XgContractsPage"/>, hosted here through <see cref="IXgPageHost"/> on the
    /// same <see cref="XingGuangController.Sim"/>; 分包, 结算 and 信用 read what the sim already has.
    /// It takes over the desktop's spare native Calculator window: until 灵光.exe is installed that window stays an
    /// ordinary calculator; from then on its icon, taskbar button, Start-menu row, title and size are 摆渡众包's, and
    /// they go back if a new game starts. Presentation only; every rule lives in XgSim.
    /// </summary>
    public sealed class ZhongbaoView : MonoBehaviour, IDesktopAppView, IXgPageHost
    {
        public const string NativeWindow = "Calculator";
        static readonly string[] NativeNames = { "Calculator", "计算器" };
        public static readonly string[] PageIds = { "label", "contracts", "subcontract", "ledger", "credit" };
        static readonly Vector2 Design = new Vector2(1266, 763);
        /// <summary>The window as big as the other app windows (the calculator is a small one).</summary>
        static readonly Vector2 WindowSizeDelta = new Vector2(-640, -200);
        public static readonly Color Blue = new Color32(41, 50, 225, 255);
        /// <summary>Header chips: the white-on-blue glass of the mockup (a faint white fill with a fainter rim).</summary>
        static readonly Color ChipFill = new Color32(67, 75, 229, 255), ChipRim = new Color32(110, 117, 236, 255);
        static readonly Color ChipInk = new Color32(240, 242, 255, 255), ChipDim = new Color32(190, 198, 255, 255), HudMoneyInk = new Color32(255, 226, 122, 255);
        /// <summary>The combo's tier colours on its white pill (index = tier + 1); the dark-header ones in XgPalette.Tiers would vanish on white.</summary>
        static readonly Color[] ComboInk = { ZhongbaoSkin.Blue, new Color32(47, 158, 68, 255), new Color32(242, 123, 29, 255), new Color32(227, 62, 51, 255), new Color32(214, 140, 0, 255) };
        /// <summary>The design root's insets (header 58, nav 132, status 24 plus a 12 margin): the area every page is built into.</summary>
        const float HeaderH = 58, NavW = 132, StatusH = 24;

        public static ZhongbaoView Instance { get; private set; }

        ZhongbaoHub hub;
        WindowManager window;
        RectTransform content;
        bool branded, applied;
        XgUi ui;
        TMP_FontAsset font;
        RectTransform root, comboChip, comboFill, gate, creditFill;
        TMP_Text appName, subtitle, hudMoney, hudCredit, hudCreditTier, hudLevel, hudTopic, comboNumber, comboMult, comboTier, toast, gateText, accountText, statusLeft, statusMid, statusRight;
        XgGlow comboGlow;
        XgBtn lingguangLink, gateButton;
        sealed class NavTab { public string id; public XgBtn btn; public Image bar; public TMP_Text badge; }
        readonly List<NavTab> tabs = new List<NavTab>();
        readonly Dictionary<string, XgPage> pages = new Dictionary<string, XgPage>();
        string page = "label";
        float refreshTimer, toastTimer, comboShownWindow = 1, comboSparkTimer;
        int comboShown = -1;
        double shownMoney = -1;
        XgSim bound;

        public WindowManager Window => window;
        public RectTransform Content => content;
        public XgLabelPage Label { get; private set; }
        public XgContractsPage Contracts { get; private set; }

        // ───────────── IXgPageHost ─────────────

        public XingGuangController Controller => hub != null ? hub.Controller : null;
        public XgSim Sim => Controller != null ? Controller.Sim : null;
        public IXgHost Host => Controller != null ? Controller.Host : null;
        public XgJuice Juice { get; private set; }
        public int BuyAmount { get; set; } = 1;
        public bool Visible => root != null && branded && window != null && window.isOn && isActiveAndEnabled;
        public string Tab => page;
        public RectTransform ComboChip => comboChip;

        static string T(string zh, string en) => GameText.T(zh, en);

        public static ZhongbaoView Install(ZhongbaoHub hub)
        {
            var window = Array.Find(FindObjectsByType<WindowManager>(FindObjectsInactive.Include, FindObjectsSortMode.None), w => w.name == NativeWindow);
            var holder = window != null && window.windowContainer != null ? window.windowContainer.Find("Content") as RectTransform : null;
            if (holder == null) return null;
            var view = holder.GetComponent<ZhongbaoView>() ?? holder.gameObject.AddComponent<ZhongbaoView>();
            view.hub = hub; view.window = window; view.content = holder;
            GameText.Changed -= view.OnLanguage; GameText.Changed += view.OnLanguage;
            Instance = view;
            return view;
        }

        void OnDestroy()
        {
            GameText.Changed -= OnLanguage;
            if (bound != null) Unsubscribe(bound);
            if (Instance == this) Instance = null;
        }

        void OnLanguage() { comboShown = -1; if (branded) Brand(); Refresh(true); }

        /// <summary>Called by the hub every frame: shows or hides the app and follows a reloaded lab.</summary>
        public void Tick(bool unlocked)
        {
            if (!applied || unlocked != branded)
            {
                // A window open across the switch would show the wrong content: close it (a calculator left open at
                // load stays open while the app is still locked).
                bool first = !applied;
                applied = true;
                if (window != null && window.isOn && (!first || unlocked)) window.CloseWindow();
                if (unlocked) Brand(); else Unbrand();
            }
            if (root != null && !ReferenceEquals(bound, Sim)) Bind(Sim);
        }

        // ───────────── IDesktopAppView ─────────────

        public bool EnsureReady()
        {
            if (!branded || Sim == null || window == null) return false;
            if (root == null) Build();
            return true;
        }

        public void OnOpened(string tab)
        {
            if (root == null) return;
            XingGuangController.EnsureShown(this, window);
            Open(tab);
        }

        /// <summary>Shows a page (label, contracts, subcontract, ledger, credit); unknown or closed pages keep the current one.</summary>
        public void Open(string tab)
        {
            if (tab == "orders") tab = "contracts";
            if (tab == "workers") tab = "subcontract";
            if (!string.IsNullOrEmpty(tab) && pages.ContainsKey(tab)) ShowPage(tab);
            else Refresh(true);
        }

        public void ShowTab(string id)
        {
            if (pages.ContainsKey(id)) { ShowPage(id); return; }
            // A 灵光 page (去训练 on the labelling and orders pages): open the lab on it.
            var router = hub != null ? hub.GetComponent<ChapterOneDesktopRouter>() ?? FindAnyObjectByType<ChapterOneDesktopRouter>() : null;
            if (router == null || !router.Open(XingGuangController.AppId, id)) Controller?.Open();
        }

        public bool BuyNode(XgNode node, RectTransform from)
        {
            if (node == null || Sim == null) return false;
            if (!Sim.BuyNode(node.id, Host)) { Juice.Play(XgJuice.Sfx.Id.Thud); return false; }
            Juice.Knock(from, .2f); Juice.Shockwave(Juice.At(from), XgPalette.Gold, 160, .35f, 8); Juice.Burst(Juice.At(from), 24, XgPalette.Gold, XgJuice.Shape.Yen, 300);
            return true;
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

        void ShowPage(string id)
        {
            if (!pages.ContainsKey(id) || !PageOpen(id)) return;
            page = id;
            foreach (var kv in pages) kv.Value.root.gameObject.SetActive(kv.Key == id);
            pages[id].Shown();
            Juice.BringToFront();
            Refresh(true);
        }

        /// <summary>企业订单 opens with the first checkpoint, as the 订单 tab did in 灵光; the other pages are always there.</summary>
        bool PageOpen(string id) => id != "contracts" || (Controller != null && Controller.FeatureVisible("contracts"));

        /// <summary>Rings a control for the to-do note: "" = the nav button, "name:Foo", "label:a|b" or "contract:id".</summary>
        public bool Highlight(string tab, string target)
        {
            if (root == null) return false;
            RectTransform rt = null;
            if (!string.IsNullOrEmpty(target) && pages.TryGetValue(tab ?? "", out var p) && p.root.gameObject.activeInHierarchy)
                rt = XgGuideHighlight.FindIn(p.root, target.StartsWith("contract:", StringComparison.Ordinal) ? "name:Row" + target.Substring(9) : target);
            if (rt == null) rt = tabs.Find(x => x.id == tab)?.btn?.rt;
            return XgGuideHighlight.Pulse(rt) != null;
        }

        // ───────────── the desktop entry ─────────────

        sealed class TextCache { public TMP_Text text; public string original; public bool status; }
        readonly List<TextCache> texts = new List<TextCache>();
        readonly List<(Behaviour c, bool enabled)> localizers = new List<(Behaviour, bool)>();
        readonly List<(Image img, Sprite sprite)> icons = new List<(Image, Sprite)>();
        readonly List<(GameObject go, bool active)> nativeChildren = new List<(GameObject, bool)>();
        readonly List<(ButtonManager bm, string text, Sprite icon)> buttons = new List<(ButtonManager, string, Sprite)>();
        (int index, string title, string keywords) startEntry = (-1, null, null);
        string taskTitle;
        Vector2 nativeSize, nativePosition;
        bool cached;

        GameObject DesktopIcon()
        {
            var icon = GameObject.Find("Desktop List/" + NativeWindow);
            if (icon != null) return icon;
            var list = GameObject.Find("Desktop List");
            var t = list != null ? list.transform.Find(NativeWindow) : null;
            return t != null ? t.gameObject : null;
        }

        static bool IsNativeName(string text) { if (string.IsNullOrEmpty(text)) return false; foreach (var n in NativeNames) if (text.Trim() == n) return true; return false; }

        /// <summary>Remembers the native entry once, so it can be put back exactly.</summary>
        void CacheNative()
        {
            if (cached || window == null) return;
            cached = true;
            var icon = DesktopIcon();
            var task = window.taskbarButton != null ? window.taskbarButton.gameObject : null;
            foreach (var entry in new[] { icon, task })
            {
                if (entry == null) continue;
                foreach (var c in entry.GetComponentsInChildren<Behaviour>(true))
                {
                    string type = c.GetType().Name;
                    if (type == "DesktopLocalizedText" || type == "AppElement" || type == "LocalizedObject") localizers.Add((c, c.enabled));
                }
                foreach (var img in entry.GetComponentsInChildren<Image>(true)) if (img.name == "Icon") icons.Add((img, img.sprite));
                foreach (var t in entry.GetComponentsInChildren<TMP_Text>(true))
                    if (t.name == "Title" || IsNativeName(t.text)) texts.Add(new TextCache { text = t, original = t.text });
                var bm = entry.GetComponent<ButtonManager>();
                if (bm != null) buttons.Add((bm, bm.buttonText, bm.buttonIcon));
            }
            if (window.taskbarButton != null)
            {
                taskTitle = window.taskbarButton.buttonTitle;
                var header = window.taskbarButton.headerButton;
                if (header != null) buttons.Add((header, header.buttonText, header.buttonIcon));
            }
            // Window chrome outside the content: the title bar and the status line.
            foreach (var t in window.GetComponentsInChildren<TMP_Text>(true))
            {
                if (content != null && t.transform.IsChildOf(content)) continue;
                bool title = t.name == "Aero Window Title" || IsNativeName(t.text), status = t.name == "Status";
                if (!title && !status) continue;
                texts.Add(new TextCache { text = t, original = t.text, status = status });
                foreach (var c in t.GetComponents<Behaviour>())
                {
                    string type = c.GetType().Name;
                    if (type == "DesktopLocalizedText" || type == "LocalizedObject") localizers.Add((c, c.enabled));
                }
            }
            if (content != null) foreach (Transform child in content) nativeChildren.Add((child.gameObject, child.gameObject.activeSelf));
            var container = window.windowContainer;
            nativeSize = container.sizeDelta; nativePosition = container.anchoredPosition;
            var menu = FindAnyObjectByType<HongmengOS.Aero2010.AeroStartMenu>(FindObjectsInactive.Include);
            if (menu != null && menu.programs != null)
                for (int i = 0; i < menu.programs.Length; i++)
                    if (menu.programs[i].window == window)
                    {
                        startEntry = (i, menu.programs[i].title, menu.programs[i].searchKeywords);
                        if (menu.programs[i].row != null)
                            foreach (var t in menu.programs[i].row.GetComponentsInChildren<TMP_Text>(true))
                                if (IsNativeName(t.text)) texts.Add(new TextCache { text = t, original = t.text });
                    }
        }

        string AppTitle => T("摆渡众包", "Bodu Crowd");

        /// <summary>The icon, taskbar button, Start-menu row and window become 摆渡众包's.</summary>
        void Brand()
        {
            CacheNative();
            branded = true;
            string name = AppTitle;
            var sprite = AppIcon();
            foreach (var (c, _) in localizers) if (c != null) c.enabled = false;
            foreach (var (img, _) in icons) if (img != null) img.sprite = sprite;
            foreach (var cache in texts) if (cache.text != null) cache.text.text = cache.status ? T("众包标注 · 企业接单", "Crowd labelling · Business orders") : name;
            foreach (var (bm, _, _) in buttons) if (bm != null) { bm.buttonText = name; bm.buttonIcon = sprite; bm.UpdateUI(); }
            if (window.taskbarButton != null) window.taskbarButton.buttonTitle = name;
            SetStartEntry(name, (startEntry.keywords ?? "") + " 摆渡众包 Bodu Crowd 众包 标注 订单");
            var container = window.windowContainer;
            if (container != null) container.sizeDelta = WindowSizeDelta;
            foreach (var (go, _) in nativeChildren) if (go != null && go != (root != null ? root.gameObject : null)) go.SetActive(false);
            if (root != null) root.gameObject.SetActive(true);
        }

        /// <summary>Before 灵光.exe is installed (or after a new game starts) the window is the plain calculator again.</summary>
        void Unbrand()
        {
            branded = false;
            if (!cached) return;
            foreach (var (c, enabled) in localizers) if (c != null) c.enabled = enabled;
            foreach (var (img, sprite) in icons) if (img != null) img.sprite = sprite;
            foreach (var cache in texts) if (cache.text != null) cache.text.text = cache.original;
            foreach (var (bm, text, icon) in buttons) if (bm != null) { bm.buttonText = text; bm.buttonIcon = icon; bm.UpdateUI(); }
            if (window.taskbarButton != null && taskTitle != null) window.taskbarButton.buttonTitle = taskTitle;
            SetStartEntry(startEntry.title, startEntry.keywords);
            var container = window.windowContainer;
            if (container != null) { container.sizeDelta = nativeSize; container.anchoredPosition = nativePosition; }
            foreach (var (go, active) in nativeChildren) if (go != null) go.SetActive(active);
            if (root != null) root.gameObject.SetActive(false);
        }

        void SetStartEntry(string title, string keywords)
        {
            if (startEntry.index < 0 || title == null) return;
            var menu = FindAnyObjectByType<HongmengOS.Aero2010.AeroStartMenu>(FindObjectsInactive.Include);
            if (menu == null || menu.programs == null || startEntry.index >= menu.programs.Length) return;
            var entry = menu.programs[startEntry.index];
            entry.title = title; entry.searchKeywords = keywords;
            menu.programs[startEntry.index] = entry;
        }

        static Sprite appIcon;

        /// <summary>A white paw on a Bodu-blue rounded square, drawn once.</summary>
        public static Sprite AppIcon()
        {
            if (appIcon != null) return appIcon;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(13 - x, x - (n - 14))), dy = Mathf.Max(0, Mathf.Max(13 - y, y - (n - 14)));
                    Color c = dx * dx + dy * dy <= 13 * 13 ? Blue : clear;
                    // Pad below, four toes above (y grows upwards in a texture).
                    bool pad = Ellipse(x, y, 32, 23, 12.5f, 10.5f);
                    bool toe = Ellipse(x, y, 16, 37, 5.5f, 6.5f) || Ellipse(x, y, 25.5f, 47, 5.5f, 6.5f) || Ellipse(x, y, 38.5f, 47, 5.5f, 6.5f) || Ellipse(x, y, 48, 37, 5.5f, 6.5f);
                    if (c.a > 0 && (pad || toe)) c = Color.white;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            appIcon = Sprite.Create(tex, new UnityEngine.Rect(0, 0, n, n), new Vector2(.5f, .5f));
            appIcon.hideFlags = HideFlags.DontSave;
            return appIcon;
        }

        static bool Ellipse(int x, int y, float cx, float cy, float rx, float ry) { float a = (x - cx) / rx, b = (y - cy) / ry; return a * a + b * b <= 1; }

        // ───────────── layout ─────────────

        void Build()
        {
            font = Controller != null && Controller.Font != null ? Controller.Font : PrologueDesk.CjkFont();
            ui = new XgUi(font, window);
            root = Rect("Zhongbao", content, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiFitScale.Attach(root, window, Design);
            Panel(root, ZhongbaoSkin.Page);
            root.gameObject.AddComponent<ChapterOneWindowFocus>().Window = window;
            Juice = gameObject.GetComponent<XgJuice>() ?? gameObject.AddComponent<XgJuice>();
            BuildHud();
            BuildNav();
            BuildStatus();
            // The pages' area: right of the nav, under the header, above the status strip, with a 12 margin (the contract the pages are built to).
            var area = Rect("Pages", root, Vector2.zero, Vector2.one, new Vector2(NavW + 12, StatusH + 12), new Vector2(-12, -(HeaderH + 12)));
            Label = Add("label", new XgLabelPage(), area);
            Contracts = Add("contracts", new XgContractsPage(), area);
            Add("subcontract", new ZhongbaoSubcontractPage(), area);
            Add("ledger", new ZhongbaoLedgerPage(hub), area);
            Add("credit", new ZhongbaoCreditPage(), area);
            var toastBox = Rect("Toast", root, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-330, StatusH + 14), new Vector2(330, StatusH + 50));
            Panel(toastBox, new Color(.1f, .12f, .2f, .9f)).raycastTarget = false;
            toast = ui.Text(Rect("Text", toastBox, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0)), "", 17, Color.white, TextAlignmentOptions.Center);
            toastBox.gameObject.SetActive(false);
            BuildGate();
            Juice.Init(root, root, font);
            Juice.Visible = () => Visible;
            Bind(Sim);
            ShowPage(page);
        }

        P Add<P>(string id, P p, RectTransform area) where P : XgPage
        {
            p.Init(this, ui);
            p.Build(area);
            p.root.gameObject.name = id;
            pages[id] = p;
            return p;
        }

        /// <summary>A header chip (faint glass on the platform blue), right-aligned: <paramref name="x"/> is the gap already used from the right edge and grows by the chip's width.</summary>
        RectTransform ChipBox(RectTransform hud, ref float x, float w, string name, float h = 26)
        {
            var r = Rect(name, hud, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-x - w, -h / 2), new Vector2(-x, h / 2));
            x += w + 8;
            Panel(r, ChipRim);
            Panel(Rect("Inner", r, Vector2.zero, Vector2.one, new Vector2(1, 1), new Vector2(-1, -1)), ChipFill).raycastTarget = false;
            return r;
        }

        TMP_Text Chip(RectTransform hud, ref float x, float w, string name)
        {
            var r = ChipBox(hud, ref x, w, name);
            var t = ui.Text(Rect("Text", r, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-8, 0)), "", 13, ChipInk, TextAlignmentOptions.MidlineLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;
            t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = 13;
            return t;
        }

        void BuildHud()
        {
            var hud = Rect("Hud", root, new Vector2(0, 1), Vector2.one, new Vector2(0, -HeaderH), Vector2.zero);
            Panel(hud, ZhongbaoSkin.Blue);
            // Logo: a white square with the character 摆 in the platform blue.
            var logo = Rect("Logo", hud, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(16, -17), new Vector2(50, 17));
            Panel(logo, Color.white).raycastTarget = false;
            var mark = ui.Text(Rect("Mark", logo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), "摆", 21, ZhongbaoSkin.Blue, TextAlignmentOptions.Center);
            mark.fontStyle = FontStyles.Bold; mark.textWrappingMode = TextWrappingModes.NoWrap;
            appName = ui.Text(Rect("AppName", hud, new Vector2(0, .45f), new Vector2(0, 1), new Vector2(60, 0), new Vector2(300, -5)), "", 20, Color.white, TextAlignmentOptions.BottomLeft);
            appName.fontStyle = FontStyles.Bold; appName.characterSpacing = 2; appName.textWrappingMode = TextWrappingModes.NoWrap;
            subtitle = ui.Text(Rect("Subtitle", hud, Vector2.zero, new Vector2(0, .45f), new Vector2(60, 6), new Vector2(300, 0)), "", 11, new Color(1, 1, 1, .75f), TextAlignmentOptions.TopLeft);
            subtitle.textWrappingMode = TextWrappingModes.NoWrap;
            float x = 16;

            // 打开灵光 ↗: an outlined link (a pale rim around a header-blue button).
            var linkRim = Rect("LingGuangLinkRim", hud, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-x - 104, -14), new Vector2(-x, 14));
            Panel(linkRim, new Color(1, 1, 1, .6f)).raycastTarget = false;
            x += 104 + 10;
            lingguangLink = ui.Button(linkRim, "", () => { Juice.Play(XgJuice.Sfx.Id.Click); ShowTab("train"); }, 13);
            lingguangLink.rt.gameObject.name = "LingGuangLink";
            lingguangLink.rt.anchorMin = Vector2.zero; lingguangLink.rt.anchorMax = Vector2.one; lingguangLink.rt.offsetMin = new Vector2(1, 1); lingguangLink.rt.offsetMax = new Vector2(-1, -1);
            UiTip.Add(lingguangLink.rt, "去灵光.exe 训练模型：检查点在那里练出来，签订单、开自动答题都靠它。", "Train the model in LingGuang.exe: checkpoints come from there, and orders and auto-answer need them.");

            // The shared combo, as on 灵光's HUD: a white pill with the number, the multiplier, the tier name and the decay bar.
            comboChip = Rect("Combo", hud, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-x - 184, -16), new Vector2(-x, 16));
            x += 184 + 8;
            comboGlow = Glow(comboChip, XgPalette.Gold, 5);
            Panel(comboChip, Color.white);
            comboNumber = ui.Text(Rect("Number", comboChip, Vector2.zero, new Vector2(0, 1), new Vector2(10, 3), new Vector2(84, 0)), "", 16, ZhongbaoSkin.Blue, TextAlignmentOptions.MidlineLeft);
            comboNumber.fontStyle = FontStyles.Bold; comboNumber.textWrappingMode = TextWrappingModes.NoWrap;
            comboMult = ui.Text(Rect("Mult", comboChip, Vector2.zero, new Vector2(0, 1), new Vector2(84, 3), new Vector2(128, 0)), "", 11, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
            comboMult.textWrappingMode = TextWrappingModes.NoWrap;
            comboTier = ui.Text(Rect("Tier", comboChip, new Vector2(0, 0), Vector2.one, new Vector2(128, 3), new Vector2(-8, 0)), "", 13, ZhongbaoSkin.Blue, TextAlignmentOptions.MidlineRight);
            comboTier.fontStyle = FontStyles.Bold; comboTier.textWrappingMode = TextWrappingModes.NoWrap;
            comboTier.enableAutoSizing = true; comboTier.fontSizeMin = 9; comboTier.fontSizeMax = 13;
            var bar = Rect("Window", comboChip, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 3));
            comboFill = Bar(bar, "Fill", new Color(0, 0, 0, 0), ZhongbaoSkin.Orange);

            hudTopic = Chip(hud, ref x, 150, "Topic");
            hudLevel = Chip(hud, ref x, 150, "Level");
            // 信用: label and score, a small gauge, the tier word.
            var creditBox = ChipBox(hud, ref x, 190, "Credit");
            hudCredit = ui.Text(Rect("Text", creditBox, new Vector2(0, 0), new Vector2(0, 1), new Vector2(10, 0), new Vector2(70, 0)), "", 13, ChipInk, TextAlignmentOptions.MidlineLeft);
            hudCredit.textWrappingMode = TextWrappingModes.NoWrap;
            var gauge = Rect("Gauge", creditBox, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(72, -3), new Vector2(126, 3));
            creditFill = Bar(gauge, "Fill", new Color(1, 1, 1, .22f), new Color32(124, 240, 160, 255));
            hudCreditTier = ui.Text(Rect("Tier", creditBox, Vector2.zero, Vector2.one, new Vector2(132, 0), new Vector2(-6, 0)), "", 12, ChipDim, TextAlignmentOptions.MidlineLeft);
            hudCreditTier.textWrappingMode = TextWrappingModes.NoWrap; hudCreditTier.enableAutoSizing = true; hudCreditTier.fontSizeMin = 9; hudCreditTier.fontSizeMax = 12;
            hudMoney = Chip(hud, ref x, 130, "Money");
            UiTip.Add(hudMoney.transform.parent, "余额：和家里共用的钱包。标注、订单进账；罚款、工资、电费花钱。", "Balance: the wallet shared with home. Labelling and orders pay in; fines, wages and power take out.");
            UiTip.Add(creditBox, () => Sim == null ? "" : Sim.QualityActive
                ? T("信用分 " + N(Sim.Credit, "0.#") + "（" + Sim.CreditTierName(Sim.CreditTier) + "）：决定抽检率和自动收入倍率。详情见「信用」页。", "Credit " + N(Sim.Credit, "0.#") + " (" + Sim.CreditTierName(Sim.CreditTier) + "): it sets the spot-check rate and the auto-income multiplier. See the Credit page.")
                : T("买下自动答题以后，平台才开始抽检、算信用。", "The platform starts spot-checking and scoring credit once you buy auto-answer."));
            UiTip.Add(hudLevel.transform.parent, "等级：加薪的头衔，每两级换一个。加薪在标注台右边的商店里。", "Level: your pay-raise title, a new one every two raises. Raises are in the shop beside the labelling desk.");
            UiTip.Add(hudTopic.transform.parent, "今日热词：弹幕、短信这些文字桌最近在聊什么。新的梗会让旧检查点答错（新题型）。", "Today's hot word: what the text desks are talking about. A new meme makes old checkpoints answer wrong (new card types).");
            UiTip.Add(comboChip, "连击：和灵光共用。连续答对、连续手动训练都会叠加。\n连击越高，标注报酬和手动训练效果越高。\n答错、超时或停太久会清零。", "Combo: shared with LingGuang. Builds with every right answer and every hand-pressed epoch.\nHigher combo pays more for labels and trains faster by hand.\nA wrong answer, a timeout or a long pause resets it.");
        }

        void BuildNav()
        {
            var nav = Rect("Nav", root, Vector2.zero, new Vector2(0, 1), new Vector2(0, StatusH), new Vector2(NavW, -HeaderH));
            Panel(nav, Color.white);
            Panel(Rect("Edge", nav, new Vector2(1, 0), Vector2.one, new Vector2(-1, 0), Vector2.zero), ZhongbaoSkin.Line).raycastTarget = false;
            const float top = 10, rowH = 40;
            for (int i = 0; i < PageIds.Length; i++)
            {
                string id = PageIds[i];
                var b = ui.Button(nav, "", () => { ShowPage(id); Juice.Play(XgJuice.Sfx.Id.Click); }, 14);
                b.rt.gameObject.name = "Tab " + id;
                b.rt.anchorMin = new Vector2(0, 1); b.rt.anchorMax = new Vector2(1, 1);
                b.rt.offsetMin = new Vector2(0, -top - (i + 1) * rowH); b.rt.offsetMax = new Vector2(-1, -top - i * rowH);
                b.label.alignment = TextAlignmentOptions.MidlineLeft;
                ((RectTransform)b.label.transform).offsetMin = new Vector2(18, 0);
                var barRect = Rect("Bar", b.rt, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(3, 0));
                var bar = Panel(barRect, ZhongbaoSkin.Blue); bar.raycastTarget = false;
                var badge = ZhongbaoSkin.Badge(ui, b.rt, "Badge", new Vector2(1, .5f), new Vector2(-24, 0), 18);
                tabs.Add(new NavTab { id = id, btn = b, bar = bar, badge = badge });
                UiTip.Add(b.rt, () => PageHelp(id));
            }
            float ruleY = top + PageIds.Length * rowH + 10;
            Panel(Rect("Rule", nav, new Vector2(0, 1), Vector2.one, new Vector2(14, -ruleY - 1), new Vector2(-14, -ruleY)), ZhongbaoSkin.Line).raycastTarget = false;
            accountText = ui.Text(Rect("Account", nav, new Vector2(0, 1), Vector2.one, new Vector2(18, -ruleY - 100), new Vector2(-8, -ruleY - 8)), "", 12, ZhongbaoSkin.Mute, TextAlignmentOptions.TopLeft);
            accountText.lineSpacing = 18;
        }

        /// <summary>The 24 px strip along the bottom: connection, the no-spot-check promise, and the rent with the next settlement.</summary>
        void BuildStatus()
        {
            var st = Rect("Status", root, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, StatusH));
            Panel(st, Color.white);
            Panel(Rect("Rule", st, new Vector2(0, 1), Vector2.one, new Vector2(0, -1), Vector2.zero), ZhongbaoSkin.Line).raycastTarget = false;
            statusLeft = ui.Text(Rect("Link", st, Vector2.zero, new Vector2(0, 1), new Vector2(12, 0), new Vector2(190, -1)), "", 11, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
            statusMid = ui.Text(Rect("Hand", st, Vector2.zero, new Vector2(0, 1), new Vector2(200, 0), new Vector2(460, -1)), "", 11, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineLeft);
            statusRight = ui.Text(Rect("Rent", st, new Vector2(1, 0), Vector2.one, new Vector2(-420, 0), new Vector2(-12, -1)), "", 11, ZhongbaoSkin.Mute, TextAlignmentOptions.MidlineRight);
            foreach (var t in new[] { statusLeft, statusMid, statusRight }) t.textWrappingMode = TextWrappingModes.NoWrap;
            UiTip.Add(st, "状态栏：手动标注从不抽检；右边是家里的房租，以及离下一次每日结算还有多久。", "Status strip: hand labels are never spot-checked; on the right, the home rent and the time to the next daily settlement.");
        }

        string PageHelp(string id)
        {
            switch (id)
            {
                case "label": return T("标注台：亲手给数据打标签。\n答对赚钱，也给模型攒训练数据。逻辑题最值钱。", "Labelling desk: label data by hand.\nRight answers pay and give the model training data. Logic cards pay best.");
                case "contracts": return PageOpen("contracts") ? T("企业订单：模型准确率达到要求就能签约，之后每秒自动给钱。", "Business orders: sign once the model is accurate enough; they pay every second.")
                    : T("企业订单：先在灵光训练出第一个检查点，才有企业找你。", "Business orders: train a first checkpoint in LingGuang and companies will come.");
                case "subcontract": return T("分包：网吧兄弟用你的账号帮你标，按分钟发工资。他们的错算在你头上。", "Subcontract: netbar friends label under your account for wages by the minute. Their mistakes count against you.");
                case "ledger": return T("结算：现在每秒进出多少钱，以及平台记下的罚款、尾款和工资。", "Ledger: money in and out per second now, and the fines, balances and wages the platform has recorded.");
                default: return T("信用：信用分、抽检和举报规则，以及什么会让它涨跌。", "Credit: the credit score, the spot-check and report rules, and what moves it.");
            }
        }

        /// <summary>While 灵光's first setup is unfinished, the platform waits for it.</summary>
        void BuildGate()
        {
            gate = Rect("SetupGate", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Panel(gate, new Color(.05f, .07f, .15f, .88f));
            var card = Rect("Card", gate, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-260, -110), new Vector2(260, 110));
            Panel(card, Color.white);
            gateText = ui.Text(Rect("Text", card, Vector2.zero, Vector2.one, new Vector2(24, 70), new Vector2(-24, -20)), "", 18, XgPalette.Ink, TextAlignmentOptions.Center);
            gateButton = ui.Button(card, "", () => Controller?.Open(), 17);
            gateButton.rt.anchorMin = gateButton.rt.anchorMax = new Vector2(.5f, 0); gateButton.rt.offsetMin = new Vector2(-110, 18); gateButton.rt.offsetMax = new Vector2(110, 60);
            gate.gameObject.SetActive(false);
        }

        // ───────────── sim events ─────────────

        void Bind(XgSim sim)
        {
            if (bound != null) Unsubscribe(bound);
            bound = sim;
            if (sim == null) return;
            sim.Message += OnMessage;
            sim.DeskOpened += OnDeskOpened;
            sim.CardTimedOut += OnCardTimedOut;
            sim.DuelDone += OnDuelDone;
            sim.ComboTier += OnComboTier;
            sim.ComboBroken += OnComboBroken;
            shownMoney = -1;
            Refresh(true);
        }

        void Unsubscribe(XgSim sim)
        {
            sim.Message -= OnMessage; sim.DeskOpened -= OnDeskOpened; sim.CardTimedOut -= OnCardTimedOut;
            sim.DuelDone -= OnDuelDone; sim.ComboTier -= OnComboTier; sim.ComboBroken -= OnComboBroken;
        }

        void OnMessage(string text) { ShowToast(text, 3.5f); }

        void OnDeskOpened(XgDesk d)
        {
            // 灵光 plays the unlock sound when it is the window in view.
            var lab = Controller != null ? Controller.View : null;
            if (lab == null || !lab.Visible) Juice.Play(XgJuice.Sfx.Id.Unlock);
            Label.RebuildTabs();
        }

        void OnCardTimedOut(string desk) { Label.OnTimeout(); }

        void OnDuelDone(bool won, double bonus)
        {
            if (!won) { Juice.Play(XgJuice.Sfx.Id.Thud); return; }
            Juice.Play(XgJuice.Sfx.Id.Fanfare);
            if (!Visible) return;
            Juice.Burst(Juice.At(Label.Paper), 30, Color.white, XgJuice.Shape.Confetti, 420);
            Juice.Float(Juice.At(Label.Paper, new Vector2(0, 40)), Lang.T("斗图胜利 +¥") + N(bonus, "0"), XgPalette.Gold, 30);
        }

        void OnComboTier(int tier)
        {
            if (!Visible) return;
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
            if (!Visible) return;
            Juice.Crumble(Juice.At(comboChip), Mathf.Min(30, 6 + was / 2), XgPalette.Tiers[Mathf.Min(4, XgSim.TierOf(was) + 1)]);
            Juice.Play(XgJuice.Sfx.Id.Break);
            if (was >= 10) Juice.Float(Juice.At(comboChip, new Vector2(-30, -46)), Lang.T("连击中断 ×") + was, XgPalette.Bad, 20);
        }

        // ───────────── frame ─────────────

        void Update()
        {
            if (root == null || Sim == null || !Visible) return;
            float dt = Time.unscaledDeltaTime;
            if (toastTimer > 0) { toastTimer -= dt; if (toastTimer <= 0) toast.transform.parent.gameObject.SetActive(false); }
            foreach (var p in pages.Values) if (p.root.gameObject.activeSelf) p.Tick(dt);
            UpdateCombo();
            UpdateMoney(dt);
            refreshTimer -= dt;
            if (refreshTimer <= 0) Refresh(false);
        }

        void UpdateMoney(float dt)
        {
            double money = Host.Money;
            if (shownMoney < 0 || Math.Abs(money - shownMoney) > Math.Max(1e5, money * 2)) shownMoney = money;
            double before = shownMoney;
            shownMoney += (money - shownMoney) * (1 - Math.Exp(-10 * dt));
            if (Math.Abs(money - shownMoney) < .005) shownMoney = money;
            string ink = money > before + .004 ? "FFF3B8" : "FFE27A";
            string text = T("余额 ", "Balance ") + "<b><color=#" + ink + ">¥" + Money(shownMoney) + "</color></b>";
            if (hudMoney.text != text) hudMoney.text = text;
        }

        void UpdateCombo()
        {
            int combo = Sim.S.combo;
            int tier = XgSim.TierOf(combo);
            if (combo >= 100)
            {
                comboSparkTimer += Time.unscaledDeltaTime;
                if (comboSparkTimer >= .4f) { comboSparkTimer = 0; Juice.Burst(Juice.At(comboChip), 2, XgPalette.Gold, XgJuice.Shape.Star, 70, 100, .8f); }
            }
            else comboSparkTimer = 0;
            if (combo != comboShown)
            {
                if (comboShown >= 0 && combo > comboShown) { Juice.Knock(comboChip, .06f + Mathf.Min(.1f, (combo - comboShown) * .02f)); comboShownWindow = (float)Math.Max(Sim.S.comboTimer, .1); }
                comboShown = combo;
                comboNumber.text = T("连击 ", "Combo ") + combo;
                comboMult.text = "×" + N(Sim.ComboMultiplier, "0.0#");
                comboTier.text = tier >= 0 ? T(XgCatalog.ComboTierNames[tier], XgCatalog.ComboTierNamesEn[tier]) : (Sim.S.bestCombo > 0 ? T("最高 ×", "Best ×") + Sim.S.bestCombo : "");
            }
            var color = ComboInk[tier + 1];
            if (tier >= 3) color = Color.Lerp(color, ComboInk[2], .5f + .5f * Mathf.Sin(Time.unscaledTime * 10));
            var shown = combo > 0 ? color : new Color(ComboInk[0].r, ComboInk[0].g, ComboInk[0].b, .55f);
            comboNumber.color = shown;
            comboTier.color = combo > 0 ? color : ZhongbaoSkin.Mute;
            SetBar(comboFill, combo > 0 ? (float)(Sim.S.comboTimer / Math.Max(.1, comboShownWindow)) : 0);
            comboGlow.on = tier >= 0;
            comboGlow.color = XgPalette.Tiers[tier + 1] * new Color(1, 1, 1, .6f);
        }

        /// <summary>The hot word: today's topic, else the newest meme drifting the text desks.</summary>
        string HotWord()
        {
            string topic = Sim.Topic;
            if (!string.IsNullOrEmpty(topic)) return topic;
            XgMemeDrift newest = null;
            foreach (var d in Sim.S.mkDrift) if (d.points > 0 && (newest == null || d.month > newest.month)) newest = d;
            return newest != null ? newest.meme : "—";
        }

        /// <summary>The household sim (rent, debt, the day clock) behind the wallet; null before it exists.</summary>
        ChapterOneSim House => hub != null && hub.runtime != null ? hub.runtime.Sim : null;

        /// <summary>
        /// 累计标注: everything submitted under the account, from the lab's own state: hand answers (right and wrong),
        /// the model's automatic ones, and the subcontract workers'.
        /// </summary>
        double TotalLabelled()
        {
            var s = Sim.S;
            double n = s.handCorrect + s.handWrong + s.autoCorrect + s.autoWrong;
            if (s.scWorkers != null) foreach (var w in s.scWorkers) if (w != null) n += w.labelsTotal;
            return n;
        }

        static string Clock(double seconds)
        {
            int total = Math.Max(0, (int)Math.Ceiling(seconds));
            return total / 60 + ":" + (total % 60).ToString("00");
        }

        void PlaceBadge(TMP_Text badge, string text)
        {
            ZhongbaoSkin.SetBadge(badge, text);
            if (string.IsNullOrEmpty(text)) return;
            float w = Mathf.Max(18, 9 + text.Length * 11);
            var r = (RectTransform)badge.transform.parent;
            r.sizeDelta = new Vector2(w, r.sizeDelta.y);
            r.anchoredPosition = new Vector2(-10 - w / 2, 0);
        }

        public void Refresh(bool force)
        {
            if (root == null || Sim == null) return;
            refreshTimer = .2f;
            if (!force && !Visible) return;
            Juice.Reduced = Sim.S.reduceFx;
            Juice.Muted = Sim.S.mute;
            appName.text = AppTitle;
            subtitle.text = T("众包标注 · 企业接单", "Crowd labelling · Business orders");
            if (shownMoney < 0) hudMoney.text = T("余额 ", "Balance ") + "<b><color=#FFE27A>¥" + Money(Host.Money) + "</color></b>";
            bool active = Sim.QualityActive;
            hudCredit.text = T("信用 ", "Credit ") + "<b>" + N(Math.Floor(Sim.Credit), "0") + "</b>";
            hudCreditTier.text = active ? Sim.CreditTierName(Sim.CreditTier) : T("未开通", "inactive");
            hudCreditTier.color = !active ? ChipDim : Sim.CreditTier == XgCreditTier.Gold ? new Color32(255, 214, 102, 255) : Sim.CreditTier == XgCreditTier.Watch ? new Color32(255, 170, 170, 255) : ChipInk;
            creditFill.GetComponent<Image>().color = Sim.CreditTier == XgCreditTier.Gold ? new Color32(255, 214, 102, 255) : Sim.CreditTier == XgCreditTier.Watch ? new Color32(255, 150, 150, 255) : new Color32(124, 240, 160, 255);
            SetBar(creditFill, active ? (float)(Sim.Credit / XgSim.QcCreditMax) : 0);
            hudLevel.text = T("等级 ", "Level ") + "<b>" + XgCatalog.RaiseTitle(Sim.RaiseLevel, En) + " Lv." + Sim.RaiseLevel + "</b>";
            hudTopic.text = T("今日热词 ", "Hot word ") + "<b>" + HotWord() + "</b>";

            if (page == "contracts" && !PageOpen("contracts")) { page = "label"; foreach (var kv in pages) kv.Value.root.gameObject.SetActive(kv.Key == page); }
            string[] names = { T("标注台", "Label desk"), T("企业订单", "Orders"), T("分包", "Subcontract"), T("结算", "Ledger"), T("信用", "Credit") };
            bool raiseReady = Sim.RaiseLevel < XgCatalog.RaiseMax && Host.Money >= Sim.NextRaiseCost;
            int signable = 0; foreach (var c in XgCatalog.Contracts) if (!Sim.Signed(c.id) && Sim.CanSign(c)) signable++;
            bool alarm = Sim.QualityFrozen || Sim.CaptchaPending || Sim.QualityWarning;
            var house = House;
            bool indebt = house != null && house.Debt > 0;
            string[] badges = { raiseReady ? T("加薪", "↑") : "", signable > 0 ? signable.ToString() : "", "", indebt ? T("欠", "Debt") : "", alarm ? "!" : "" };
            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                bool open = PageOpen(tab.id), on = tab.id == page;
                tab.btn.Set(names[i], open, on ? ZhongbaoSkin.Blue3 : Color.white, on ? ZhongbaoSkin.Blue : ZhongbaoSkin.Ink);
                if (!open) { tab.btn.image.color = Color.white; tab.btn.label.color = ZhongbaoSkin.Dim; }
                tab.btn.label.fontStyle = on ? FontStyles.Bold : FontStyles.Normal;
                tab.bar.enabled = on;
                PlaceBadge(tab.badge, open ? badges[i] : "");
            }
            lingguangLink.Set(T("打开灵光 ↗", "Open LingGuang ↗"), true, ZhongbaoSkin.Blue, Color.white);
            accountText.text = T("账号 kk_2016\n注册 2016-05-29\n累计标注 <b>" + N(TotalLabelled(), "N0") + "</b> 条", "Account kk_2016\nJoined 2016-05-29\nLabelled <b>" + N(TotalLabelled(), "N0") + "</b> so far");
            statusLeft.text = "<color=#2F9E44>●</color> " + T("已连接 摆渡云", "Connected to Bodu Cloud");
            statusMid.text = T("手动标注从不抽检", "Hand labels are never spot-checked");
            // Rent and the next daily settlement come from the household sim; before rent starts there is nothing to show.
            statusRight.text = house != null && house.EconomyActive
                ? T("房租 ¥" + Money(house.RentFor(GameCalendar.Now(house.S).Date)) + "/天 · " + Clock(house.Config.dayLengthSeconds - house.S.daySeconds) + " 后结算",
                    "Rent ¥" + Money(house.RentFor(GameCalendar.Now(house.S).Date)) + "/day · settles in " + Clock(house.Config.dayLengthSeconds - house.S.daySeconds))
                : "";

            var runtime = Controller != null ? Controller.runtime : null;
            bool waiting = runtime != null && runtime.Sim != null && runtime.Sim.InPrologue && !runtime.TestMode;
            if (gate.gameObject.activeSelf != waiting) gate.gameObject.SetActive(waiting);
            if (waiting)
            {
                gate.SetAsLastSibling();
                gateText.text = T("<b>欢迎来到摆渡众包</b>\n\n先在灵光.exe 里完成初次设置，\n这里的标注台就开张。", "<b>Welcome to Bodu Crowd</b>\n\nFinish LingGuang.exe's first setup\nand the labelling desk opens here.");
                gateButton.Set(T("打开灵光.exe", "Open LingGuang.exe"), true, Blue, Color.white);
            }
            if (pages.TryGetValue(page, out var p)) p.Refresh();
        }
    }
}
