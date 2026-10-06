using System;
using System.Collections.Generic;
using System.Globalization;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using LingGuangV05.Core;
namespace LingGuangV05.Desktop.Casino
{
    /// <summary>Five playable local games in a DreamOS tab; presentation never settles money.</summary>
    public sealed partial class CasinoPage : MonoBehaviour
    {
        public static readonly Color Gold = new Color32(236, 190, 96, 255);
        static readonly Color Background = new Color32(20, 10, 18, 255), Panel = new Color32(40, 20, 31, 255), Burgundy = new Color32(117, 23, 46, 255);
        static readonly Color Muted = new Color32(185, 158, 164, 255), White = new Color32(255, 240, 216, 255);
        static readonly string[] Games = { "slots", "dice", "roulette", "dragon", "blackjack" };
        static readonly int[] Stakes = { 10, 50, 100, 500 };
        static readonly string[] SymbolsZh = { "7", "BAR", "星", "铃", "瓜", "钻" }, SymbolsEn = { "7", "BAR", "STAR", "BELL", "MEL", "GEM" };
        RectTransform root, arena, wheel;
        TMP_FontAsset font;
        TMP_Text wallet, totals, result, history, rules, hint, gameTitle, ticket, odds;
        readonly List<TMP_Text> faces = new List<TMP_Text>();
        readonly List<Image[]> dicePips = new List<Image[]>();
        static readonly int[] PipMasks = { 0, 64, 33, 97, 51, 115, 63 };
        readonly List<Button> tabs = new List<Button>(), chips = new List<Button>(), choices = new List<Button>();
        Button bet;
        XingGuangController controller;
        XgSim bound;
        XgCasinoRound reveal;
        int game, stake = 10, choice;
        float revealAt, nextFrame, nextRefresh;
        int shownRound = -1;
        bool english;
        string feedback;

        static string T(string zh, string en) => GameText.T(zh, en);
        public static string GameName(string id)
        {
            switch (id)
            {
                case "slots": return Lang.T("幸运老虎机");
                case "dice": return Lang.T("骰子大小");
                case "roulette": return Lang.T("欧式轮盘");
                case "blackjack": return Lang.T("21 点");
                default: return Lang.T("龙虎斗");
            }
        }

        void Start()
        {
            font = PrologueDesk.CjkFont();
            Build();
        }

        RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            return PrologueDesk.Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y - h), new Vector2(x + w, -y));
        }

        TMP_Text Label(string name, Transform parent, float x, float y, float w, float h, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var rt = Rect(name, parent, x, y, w, h);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.fontSize = size; t.text = text; t.color = color; t.alignment = align;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        Button Button(string name, Transform parent, float x, float y, float w, float h, string text, Action action)
        {
            var rt = Rect(name, parent, x, y, w, h);
            var image = PrologueDesk.Fill(rt, Burgundy);
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = image;
            var colors = b.colors; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f); colors.disabledColor = new Color(.5f, .5f, .5f, .6f); b.colors = colors;
            b.onClick.AddListener(() => action());
            Label("Text", rt, 4, 1, w - 8, h - 2, text, 19, White, TextAlignmentOptions.Center);
            return b;
        }

        void Build()
        {
            CancelPresentation();
            english = GameText.IsEnglish;
            if (root != null) { root.gameObject.SetActive(false); Destroy(root.gameObject); }
            juice = null; winGlow = null; cardMotions.Clear(); shownHand = null;
            faces.Clear(); dicePips.Clear(); tabs.Clear(); chips.Clear(); choices.Clear(); blackjackButtons.Clear();
            root = PrologueDesk.Rect("888 VIP Casino", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(root, Background);
            var backdrop = LingGuangV05.Desktop.Media.DesktopMedia.Paint(root, "casino");
            if (backdrop != null) backdrop.color = new Color(1, 1, 1, .28f);
            var fit = root.gameObject.AddComponent<UiFitScale>(); fit.designSize = new Vector2(1120, 660); fit.minScale = .25f; fit.Apply(true);
            // Fixed centred artboard inside the fit root; extra aspect-ratio space remains dark.
            board = PrologueDesk.Centered("Casino Board", root, Vector2.zero, new Vector2(1120, 660));
            var header = Rect("Header", board, 0, 0, 1120, 86); PrologueDesk.Fill(header, new Color32(64, 11, 29, 255));
            Label("Logo", board, 25, 6, 156, 68, "<b>888</b><size=16> .vip</size>", 57, Gold);
            Label("Brand", board, 204, 10, 360, 36, Lang.T("国际线上娱乐"), 26, White);
            Label("Year", board, 205, 47, 450, 25, Lang.T("2016 · 五大经典 · 即开即玩"), 14, Gold);
            wallet = Label("Wallet", board, 754, 16, 340, 30, "", 23, Gold, TextAlignmentOptions.MidlineRight);
            Label("Shared Wallet", board, 722, 48, 372, 22, Lang.T("游戏币与桌面钱包共用 · 1 币 = ¥1"), 13, Muted, TextAlignmentOptions.MidlineRight);
            for (int i = 0; i < Games.Length; i++)
            {
                int index = i;
                tabs.Add(Button("Game " + Games[i], board, 24 + i * 218, 98, 204, 42, GameName(Games[i]), () => { if (PresentationBusy) return; game = index; choice = 0; feedback = null; BuildArena(); }));
            }
            arena = Rect("Arena", board, 24, 154, 720, 318);
            PrologueDesk.Fill(arena, Panel);
            var slip = Rect("Bet Slip", board, 762, 154, 334, 318); PrologueDesk.Fill(slip, Panel);
            Label("Ticket Label", slip, 20, 12, 294, 25, Lang.T("下注单 / BET SLIP"), 17, Gold);
            ticket = Label("Selected Bet", slip, 20, 42, 294, 24, "", 17, White);
            for (int i = 0; i < Stakes.Length; i++)
            {
                int value = Stakes[i];
                chips.Add(Button("Stake " + value, slip, 18 + i * 77, 80, 68, 46, value.ToString(), () => { if (PresentationBusy) return; stake = value; feedback = null; }));
            }
            Label("Choice Label", slip, 20, 139, 294, 23, Lang.T("选择押注"), 15, Muted);
            choices.Add(Button("Choice 0", slip, 18, 170, 144, 40, "", () => { if (!PresentationBusy) { choice = 0; feedback = null; } }));
            choices.Add(Button("Choice 1", slip, 174, 170, 142, 40, "", () => { if (!PresentationBusy) { choice = 1; feedback = null; } }));
            AddBlackjackButtons(slip);
            bet = Button("Place Bet", slip, 18, 225, 298, 50, "", PlaceBet);
            hint = Label("Bet Hint", slip, 18, 281, 298, 26, "", 13, Muted, TextAlignmentOptions.Center);
            result = Label("Settlement", board, 30, 480, 708, 32, "", 22, Gold);
            totals = Label("Totals", board, 765, 478, 334, 38, "", 15, Muted, TextAlignmentOptions.MidlineRight);
            rules = Label("Rules", board, 26, 522, 1070, 46, "", 15, White, TextAlignmentOptions.TopLeft);
            history = Label("Recent Results", board, 26, 575, 1070, 36, "", 14, Muted, TextAlignmentOptions.TopLeft);
            PrologueDesk.Fill(Rect("Footer Line", board, 24, 620, 1072, 1), Burgundy, false);
            Label("Notice", board, 24, 628, 1072, 25, Lang.T("虚构游戏支线 · 无充值提现 · 单局有输有赢，长期赔率偏向庄家 · 别拿电费试手气"), 13, Muted, TextAlignmentOptions.Center);
            EnsureEffects(); BuildArena();
        }

        bool Spinning => reveal != null && Time.unscaledTime < revealAt;

        void BuildArena()
        {
            cardMotions.Clear();
            for (int i = arena.childCount - 1; i >= 0; i--) { arena.GetChild(i).gameObject.SetActive(false); Destroy(arena.GetChild(i).gameObject); }
            faces.Clear(); dicePips.Clear(); wheel = null;
            string id = Games[game];
            arena.GetComponent<Image>().color = id == "blackjack" ? new Color32(15, 57, 44, 255) : Panel;
            gameTitle = Label("Table Title", arena, 20, 12, 500, 32, GameName(id), 25, Gold);
            Label("Table Number", arena, 532, 14, 168, 28, "VIP / 0" + (game + 1), 16, Muted, TextAlignmentOptions.MidlineRight);
            if (id == "blackjack") { BuildBlackjackTable(); return; }
            string boast = id == "slots" ? Lang.T("三枚相同 · 返还 20 倍")
                : id == "dice" ? Lang.T("三骰定大小 · 豹子通吃")
                : id == "roulette" ? Lang.T("红与黑之间 · 还有一个零")
                : Lang.T("一张定胜负 · A 最小 K 最大");
            odds = Label("Paytable Banner", arena, 20, 51, 680, 31, boast, 19, White, TextAlignmentOptions.Center);
            if (id == "roulette") { BuildWheel(); if (!Spinning) ShowLastResult(); return; }
            int count = id == "roulette" ? 1 : id == "dragon" ? 2 : 3;
            float width = id == "roulette" ? 194 : 150, gap = 18;
            float start = (720 - (width * count + gap * (count - 1))) / 2;
            for (int i = 0; i < count; i++)
            {
                var tile = Rect("Reel " + i, arena, start + i * (width + gap), 98, width, 148);
                PrologueDesk.Fill(tile, Gold);
                var inset = Rect("Face", tile, 3, 3, width - 6, 142);
                PrologueDesk.Fill(inset, new Color32(245, 229, 201, 255));
                if (id == "dice")
                {
                    var dots = new Image[7];
                    for (int d = 0; d < 7; d++)
                    {
                        float x = d == 6 ? 63 : d % 2 == 0 ? 29 : 97;
                        float y = d == 6 ? 51 : 17 + d / 2 * 34;
                        dots[d] = PrologueDesk.Fill(Rect("Pip " + d, inset, x, y, 18, 18), Burgundy, false);
                        dots[d].sprite = PrologueDesk.Circle(); dots[d].gameObject.SetActive(false);
                    }
                    dicePips.Add(dots);
                }
                faces.Add(Label("Value", inset, 2, id == "dice" ? 116 : 12, width - 10, id == "dice" ? 22 : 112, "?", id == "dice" ? 16 : 57, Burgundy, TextAlignmentOptions.Center));
                if (id == "dragon")
                {
                    var back = Rect("Card Back", tile, 3, 3, width - 6, 142); PrologueDesk.Fill(back, Burgundy, false);
                    Label("Back Mark", back, 0, 0, width - 6, 142, "888\n<size=16>VIP</size>", 34, Gold, TextAlignmentOptions.Center);
                    var motion = CardMotion(tile, inset, back);
                    if (reveal != null && reveal.game == "dragon") motion.Deal(new Vector2(672, -70), i * .2f, true);
                    else motion.Rest(true);
                }
            }
            Label("Table Caption", arena, 20, 265, 680, 31,
                id == "dragon" ? Lang.T("龙  <size=16>VS</size>  虎     /     同点庄家赢") : Lang.T("请先选筹码，再按下方按钮下注"), 17, Muted, TextAlignmentOptions.Center);
            if (!Spinning) ShowLastResult();
        }

        void BuildWheel()
        {
            var rim = Rect("Roulette Rim", arena, 258, 91, 204, 204);
            var rimImage = PrologueDesk.Fill(rim, Gold, false); rimImage.sprite = PrologueDesk.Circle();
            wheel = Rect("Roulette Wheel", rim, 4, 4, 196, 196);
            for (int n = 0; n <= 36; n++)
            {
                var pocket = Rect("Pocket " + n, wheel, 0, 0, 196, 196);
                var image = PrologueDesk.Fill(pocket, n == 0 ? new Color32(27, 138, 91, 255) : CasinoRules.Red(n) ? Burgundy : Background, false);
                image.sprite = PrologueDesk.Circle(); image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Radial360; image.fillOrigin = (int)Image.Origin360.Top;
                image.fillAmount = .96f / 37; pocket.localEulerAngles = new Vector3(0, 0, -n * 360f / 37);
            }
            var center = Rect("Wheel Center", rim, 46, 46, 112, 112);
            var fill = PrologueDesk.Fill(center, White, false); fill.sprite = PrologueDesk.Circle();
            faces.Add(Label("Value", center, 0, 0, 112, 112, "?", 48, Burgundy, TextAlignmentOptions.Center));
            PrologueDesk.Fill(Rect("Pointer", arena, 356, 87, 8, 22), Gold, false);
            Label("Red Pockets", arena, 24, 126, 210, 90, Lang.T("18 格红\n18 格黑\n1 格绿色的零"), 21, White, TextAlignmentOptions.Center);
            Label("Roulette Return", arena, 490, 126, 206, 90, Lang.T("押中颜色\n返还 2 倍\n零点两边都输"), 20, Gold, TextAlignmentOptions.Center);
        }

        void SetFace(int index, int value)
        {
            faces[index].text = Face(Games[game], value);
            if (game == 1 && index < dicePips.Count)
                for (int d = 0; d < 7; d++) dicePips[index][d].gameObject.SetActive((PipMasks[value] & (1 << d)) != 0);
        }

        void PlaceBet()
        {
            if (controller == null || !ReferenceEquals(controller.Sim, bound) || PresentationBusy) return;
            if (Games[game] == "blackjack")
            {
                int before = bound.Casino.rounds;
                if (!bound.StartBlackjack(stake, controller.Host)) { feedback = Lang.T("余额不足或上一手尚未结束"); return; }
                PersistBlackjack(before); return;
            }
            if (!bound.TryCasinoBet(Games[game], choice, stake, controller.Host, out var round))
            { feedback = Lang.T("余额不足或上一局尚未结束"); return; }
            reveal = round; revealAt = Time.unscaledTime + 1.2f;
            QueuePayout(round);
            if (round.game == "dragon") { revealAt = Time.unscaledTime; BuildArena(); }
            feedback = null;
            Refresh(); // lock tabs/chips/choices in the same callback, not on the next 10 Hz refresh
            controller.runtime.MarkDirty();
            controller.SaveNow(); // payout and RNG persist together, before the cosmetic animation
        }

        void Update()
        {
            if (root == null) return;
            if (controller == null) controller = FindAnyObjectByType<XingGuangController>();
            if (controller == null || controller.Sim == null) return;
            if (!ReferenceEquals(bound, controller.Sim)) { CancelPresentation(); bound = controller.Sim; feedback = null; ShowLastResult(); }
            EnsureEffects(); SyncEffects();
            if (!PageVisible()) { CancelPresentation(); return; }
            if (english != GameText.IsEnglish) Build();
            if (Games[game] == "blackjack" && (shownHand != bound.Casino.blackjack || shownBlackjackRevision != (bound.Casino.blackjack?.revision ?? 0))) BuildArena();
            if (Spinning)
            {
                if (Time.unscaledTime >= nextFrame)
                {
                    nextFrame = Time.unscaledTime + .075f;
                    for (int i = 0; i < faces.Count; i++)
                    {
                        int n = ((int)(Time.unscaledTime * 17) + i * 7) % (game == 2 ? 37 : game == 3 ? 13 : 6);
                        SetFace(i, n + (game == 1 || game == 3 ? 1 : 0));
                    }
                }
                if (wheel != null) wheel.localEulerAngles = new Vector3(0, 0, Time.unscaledTime * -420);
            }
            else if (reveal != null || shownRound != bound.Casino.rounds) { reveal = null; ShowLastResult(); }
            RevealPayout();
            if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .1f; Refresh(); }
        }

        void Refresh()
        {
            if (bound == null) return;
            var c = bound.Casino;
            double money = controller.Host.Money - (pendingPayout != null ? pendingPayout.returned : 0);
            wallet.text = Lang.T("余额  ") + "¥" + Money(money);
            totals.text = Lang.T("累计净输赢  ") + Signed(c.returned - c.wagered - (pendingPayout?.returned ?? 0));
            string id = Games[game];
            for (int i = 0; i < tabs.Count; i++) { tabs[i].interactable = !PresentationBusy; tabs[i].GetComponent<Image>().color = game == i ? Burgundy : Panel; }
            for (int i = 0; i < chips.Count; i++) { chips[i].interactable = !PresentationBusy && !bound.BlackjackActive; chips[i].GetComponent<Image>().color = stake == Stakes[i] ? new Color32(151, 100, 37, 255) : Burgundy; }
            for (int i = 0; i < choices.Count; i++)
            {
                choices[i].gameObject.SetActive(id != "slots" && id != "blackjack"); choices[i].interactable = !PresentationBusy;
                choices[i].GetComponentInChildren<TMP_Text>().text = ChoiceName(id, i);
                choices[i].GetComponent<Image>().color = choice == i ? new Color32(151, 100, 37, 255) : Burgundy;
            }
            ticket.text = Lang.T("本局  ") + "¥" + (id == "blackjack" && bound.BlackjackActive ? c.blackjack.stake : stake) + "  /  " + (id == "blackjack" ? "BLACKJACK" : id == "slots" ? Lang.T("单次旋转") : ChoiceName(id, choice));
            bet.GetComponentInChildren<TMP_Text>().text = bound.BlackjackActive ? Lang.T("请先完成 21 点这一手") : Spinning ? Lang.T("开奖中…") : Lang.T("下注  ") + "¥" + stake;
            bet.interactable = !bound.BlackjackActive && !PresentationBusy && bound.Clock >= c.nextBetAt && money >= stake && !double.IsNaN(money) && !double.IsInfinity(money);
            hint.text = feedback ?? (bound.BlackjackActive ? Lang.T("关页不退注 · 回到 21 点可继续") : money < stake ? Lang.T("余额不足，去标注台赚些零钱") : Lang.T("点击即扣款 · 返还包含本金"));
            RefreshBlackjackButtons();
            rules.text = RuleText(id);
            if (Spinning) result.text = Lang.T("正在开奖… 本局已经结算，关页不退注。");
            else
            {
                var last = c.history.Count > 0 ? c.history[c.history.Count - 1] : null;
                result.text = last == null ? Lang.T("欢迎光临。输赢都会计入游戏钱包。")
                    : "#" + last.number + "  " + GameName(last.game) + "   " + Lang.T("净输赢 ") + Signed(last.returned - last.stake) + "   <size=15>" + Lang.T("返还 ") + "¥" + last.returned + "</size>";
            }
            if (bound.BlackjackActive) result.text = Lang.T("21 点待续：本手已押 ¥") + c.blackjack.stake + Lang.T("，要牌或停牌后结算。");
            if (CardsBusy) result.text = Lang.T("发牌 / 翻牌中…");
            var items = new List<string>();
            for (int i = Math.Max(0, c.history.Count - 5); i < c.history.Count; i++)
            { var r = c.history[i]; if (pendingPayout == r) continue; items.Add("#" + r.number + " " + Signed(r.returned - r.stake)); }
            history.text = Lang.T("最近记录：") + (items.Count == 0 ? "—" : string.Join("    /    ", items));
        }

        void ShowLastResult()
        {
            shownRound = bound != null ? bound.Casino.rounds : -1;
            var list = bound != null ? bound.Casino.history : null;
            var last = list != null && list.Count > 0 ? list[list.Count - 1] : null;
            bool same = last != null && last.game == Games[game] && last.values != null && last.values.Length == faces.Count;
            for (int i = 0; i < faces.Count; i++)
            {
                int value = same ? last.values[i] : -1;
                if (same) SetFace(i, value); else faces[i].text = "?";
                if (!same && game == 1 && i < dicePips.Count) foreach (var dot in dicePips[i]) dot.gameObject.SetActive(false);
                faces[i].color = game == 2 && value == 0 ? new Color32(26, 117, 77, 255) : game == 2 && value > 0 && !CasinoRules.Red(value) ? Background : Burgundy;
            }
            if (wheel != null && same) wheel.localEulerAngles = new Vector3(0, 0, last.values[0] * 360f / 37 + 180f / 37);
        }

        static string Face(string id, int value)
        {
            if (id == "slots") return (GameText.IsEnglish ? SymbolsEn : SymbolsZh)[value];
            if (id == "dragon") return value == 1 ? "A" : value == 11 ? "J" : value == 12 ? "Q" : value == 13 ? "K" : value.ToString();
            return value.ToString();
        }

        static string ChoiceName(string id, int side) => id == "dice" ? (side == 0 ? Lang.T("小 4–10") : Lang.T("大 11–17"))
            : id == "roulette" ? (side == 0 ? Lang.T("红") : Lang.T("黑")) : side == 0 ? Lang.T("龙") : Lang.T("虎");
        static string Money(double value) => value.ToString("N0", CultureInfo.InvariantCulture);
        static string Signed(double value) => (value > 0 ? "+" : value < 0 ? "−" : "") + "¥" + Money(Math.Abs(value));
        static string RuleText(string id)
        {
            switch (id)
            {
                case "slots": return Lang.T("规则：三轴各有 6 种等概率符号；三个相同返还 20 倍，恰有两个相同返还本金，其余为 0。");
                case "dice": return Lang.T("规则：三枚六面骰。小为总点数 4–10，大为 11–17；三个相同（豹子）无论大小都输，押中返还 2 倍。");
                case "roulette": return Lang.T("规则：0–36 共 37 格等概率，18 红、18 黑；0 为绿色，两边都输，押中颜色返还 2 倍。");
                case "blackjack": return Lang.T("规则：单副牌每手洗牌，A 计 1 或 11，J/Q/K 计 10。庄家软 17 停牌；自然 21 点赢 3:2，普通胜局赢 1:1，平局退注。首两张可加倍，只补一张；不分牌、无保险。");
                default: return Lang.T("规则：龙、虎各独立抽取 A–K（1–13），点数大的一方胜；同点两边都输，押中返还 2 倍。");
            }
        }
    }
}
