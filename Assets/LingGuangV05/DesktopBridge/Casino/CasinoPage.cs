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
        // Slot reels show a drawn symbol (Icons/casino/sym_*) over the text face; entries may be null when an icon is missing.
        readonly List<Image> slotIcons = new List<Image>();
        static readonly string[] SlotIconIds = { "sym_7", "sym_bar", "sym_star", "sym_bell", "sym_melon", "sym_gem" };
        readonly List<Image[]> dicePips = new List<Image[]>();
        static readonly int[] PipMasks = { 0, 64, 33, 97, 51, 115, 63 };
        readonly List<Button> tabs = new List<Button>(), chips = new List<Button>(), choices = new List<Button>();
        Button bet, halfStake, doubleStake;
        TMP_InputField stakeInput;
        XingGuangController controller;
        XgSim bound;
        XgCasinoRound reveal;
        int game, choice;
        long stake = 10; // any whole-yuan amount from 1 up to the wallet; there is no table limit
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

        /// <summary>One of the site's own icons (CasinoAssets); nothing when the file is missing.</summary>
        Image Icon(string name, Transform parent, float x, float y, float size)
        {
            var sprite = CasinoAssets.Icon(name);
            if (sprite == null) return null;
            var image = Rect(name + " Icon", parent, x, y, size, size).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }

        /// <summary>The site's own sound (CasinoAssets), or the lab's synthesized one when the file is missing.</summary>
        void Sound(string name, XgJuice.Sfx.Id fallback, float pitch = 1, float volume = 1)
        {
            if (juice == null) return;
            var clip = CasinoAssets.Sound(name);
            if (clip != null) juice.PlayClip(clip, pitch, volume); else juice.Play(fallback, pitch, volume);
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
            faces.Clear(); slotIcons.Clear(); dicePips.Clear(); tabs.Clear(); chips.Clear(); choices.Clear(); blackjackButtons.Clear(); stakeInput = null;
            root = PrologueDesk.Rect("888 VIP Casino", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            PrologueDesk.Fill(root, Background);
            var backdrop = LingGuangV05.Desktop.Media.DesktopMedia.Paint(root, "casino");
            if (backdrop != null) backdrop.color = new Color(1, 1, 1, .28f);
            var fit = root.gameObject.AddComponent<UiFitScale>(); fit.designSize = new Vector2(1120, 660); fit.minScale = .25f; fit.Apply(true);
            // Fixed centred artboard inside the fit root; extra aspect-ratio space remains dark.
            board = PrologueDesk.Centered("Casino Board", root, Vector2.zero, new Vector2(1120, 660));
            var header = Rect("Header", board, 0, 0, 1120, 86); PrologueDesk.Fill(header, new Color32(64, 11, 29, 255));
            if (Icon("logo", board, 22, 9, 68) != null) Label("Logo", board, 96, 6, 104, 68, "<b>888</b><size=14>.vip</size>", 38, Gold);
            else Label("Logo", board, 25, 6, 156, 68, "<b>888</b><size=16> .vip</size>", 57, Gold);
            Label("Brand", board, 204, 10, 360, 36, Lang.T("国际线上娱乐"), 26, White);
            Label("Year", board, 205, 47, 450, 25, Lang.T("2016 · 五大经典 · 即开即玩"), 14, Gold);
            wallet = Label("Wallet", board, 754, 16, 340, 30, "", 23, Gold, TextAlignmentOptions.MidlineRight);
            Label("Shared Wallet", board, 722, 48, 372, 22, Lang.T("游戏币与桌面钱包共用 · 1 币 = ¥1"), 13, Muted, TextAlignmentOptions.MidlineRight);
            for (int i = 0; i < Games.Length; i++)
            {
                int index = i;
                tabs.Add(Button("Game " + Games[i], board, 24 + i * 218, 98, 204, 42, GameName(Games[i]), () => { if (PresentationBusy) return; game = index; choice = 0; feedback = null; BuildArena(); }));
                Icon(Games[i], tabs[tabs.Count - 1].transform, 8, 4, 34);
            }
            arena = Rect("Arena", board, 24, 154, 720, 318);
            PrologueDesk.Fill(arena, Panel);
            var slip = Rect("Bet Slip", board, 762, 154, 334, 318); PrologueDesk.Fill(slip, Panel);
            Label("Ticket Label", slip, 20, 12, 294, 25, Lang.T("下注单 / BET SLIP"), 17, Gold);
            ticket = Label("Selected Bet", slip, 20, 42, 294, 24, "", 17, White);
            for (int i = 0; i < Stakes.Length; i++)
            {
                int value = Stakes[i];
                chips.Add(Button("Stake " + value, slip, 18 + i * 55, 70, 50, 40, value.ToString(), () => { if (PresentationBusy) return; SetStake(value); feedback = null; Sound("chip", XgJuice.Sfx.Id.Clack, 1, .7f); }));
                DecorateChip(chips[chips.Count - 1], "chip_" + value, 25, 20, 38, false); // the amount is printed on the drawn chip
            }
            // 全押: the whole (whole-yuan part of the) wallet. There is no maximum bet.
            var allInChip = Button("Stake All In", slip, 18 + Stakes.Length * 55, 70, 78, 40, Lang.T("全押"), () => { if (PresentationBusy) return; SetStake(Math.Max(1, CasinoRules.AllIn(controller != null ? controller.Host.Money : 0))); feedback = null; Sound("allin", XgJuice.Sfx.Id.Clack, 1, .8f); });
            allInChip.GetComponentInChildren<TMP_Text>().fontSize = 17;
            DecorateChip(allInChip, "chip_allin", 20, 20, 36, true);
            chips.Add(allInChip);
            BuildStakeInput(slip);
            Label("Choice Label", slip, 20, 156, 294, 20, Lang.T("选择押注"), 15, Muted);
            choices.Add(Button("Choice 0", slip, 18, 178, 144, 38, "", () => { if (!PresentationBusy) { choice = 0; feedback = null; } }));
            choices.Add(Button("Choice 1", slip, 174, 178, 142, 38, "", () => { if (!PresentationBusy) { choice = 1; feedback = null; } }));
            AddBlackjackButtons(slip);
            bet = Button("Place Bet", slip, 18, 224, 298, 48, "", PlaceBet);
            hint = Label("Bet Hint", slip, 18, 277, 298, 36, "", 13, Muted, TextAlignmentOptions.Center);
            result = Label("Settlement", board, 30, 480, 708, 32, "", 22, Gold);
            totals = Label("Totals", board, 765, 478, 334, 38, "", 15, Muted, TextAlignmentOptions.MidlineRight);
            rules = Label("Rules", board, 26, 522, 1070, 46, "", 15, White, TextAlignmentOptions.TopLeft);
            history = Label("Recent Results", board, 26, 575, 1070, 36, "", 14, Muted, TextAlignmentOptions.TopLeft);
            PrologueDesk.Fill(Rect("Footer Line", board, 24, 620, 1072, 1), Burgundy, false);
            Label("Notice", board, 24, 628, 1072, 25, Lang.T("虚构游戏支线 · 无充值提现 · 单局有输有赢，长期赔率仍略偏向庄家 · 别拿电费试手气 · 未满 18 岁请勿赌博"), 13, Muted, TextAlignmentOptions.Center);
            EnsureEffects(); BuildArena(); BuildScam();
        }

        bool Spinning => reveal != null && Time.unscaledTime < revealAt;

        void BuildArena()
        {
            cardMotions.Clear();
            for (int i = arena.childCount - 1; i >= 0; i--) { arena.GetChild(i).gameObject.SetActive(false); Destroy(arena.GetChild(i).gameObject); }
            faces.Clear(); slotIcons.Clear(); dicePips.Clear(); wheel = null;
            string id = Games[game];
            arena.GetComponent<Image>().color = id == "blackjack" ? new Color32(15, 57, 44, 255) : Panel;
            gameTitle = Label("Table Title", arena, 20, 12, 500, 32, GameName(id), 25, Gold);
            Label("Table Number", arena, 532, 14, 168, 28, "VIP / 0" + (game + 1), 16, Muted, TextAlignmentOptions.MidlineRight);
            if (id == "blackjack") { BuildBlackjackTable(); return; }
            string boast = id == "slots" ? Lang.T("两枚相同 2 倍 · 三枚相同 3 倍 · 三个 7 返还 20 倍")
                : id == "dice" ? Lang.T("三骰定大小 · 豹子退回一半")
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
                if (id == "slots")
                {
                    var symbol = LingGuangV05.Desktop.XingGuang.XgIcons.Place(inset, "casino", SlotIconIds[0], (width - 6) / 2, 71, 118);
                    if (symbol != null) symbol.gameObject.SetActive(false);
                    slotIcons.Add(symbol);
                }
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
                id == "dragon" ? Lang.T("龙  <size=16>VS</size>  虎     /     同点退回 3/4 本金") : Lang.T("请先选筹码，再按下方按钮下注"), 17, Muted, TextAlignmentOptions.Center);
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
            Label("Roulette Return", arena, 490, 126, 206, 90, Lang.T("押中颜色\n返还 2 倍\n零点退回一半"), 20, Gold, TextAlignmentOptions.Center);
        }

        void SetStake(long value)
        {
            stake = Math.Max(1, Math.Min(value, CasinoRules.StakeCeiling));
            if (stakeInput != null) stakeInput.SetTextWithoutNotify(stake.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Digits-only free amount box with half / double buttons; chips and 全押 sit above it. Bet size is never capped.</summary>
        void BuildStakeInput(RectTransform slip)
        {
            Label("Yen", slip, 18, 116, 20, 34, "¥", 19, Gold, TextAlignmentOptions.Center);
            var box = Rect("Stake Amount", slip, 38, 116, 132, 34);
            PrologueDesk.Fill(box, Background);
            var area = Rect("Text Area", box, 6, 2, 120, 30);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = Label("Placeholder", area, 0, 0, 120, 30, Lang.T("金额"), 17, Muted);
            var text = Label("Text", area, 0, 0, 120, 30, "", 19, White);
            text.richText = false; text.textWrappingMode = TextWrappingModes.NoWrap;
            stakeInput = box.gameObject.AddComponent<TMP_InputField>();
            stakeInput.textViewport = area; stakeInput.textComponent = text; stakeInput.placeholder = placeholder;
            stakeInput.fontAsset = font; stakeInput.pointSize = 19; stakeInput.richText = false;
            stakeInput.lineType = TMP_InputField.LineType.SingleLine;
            stakeInput.contentType = TMP_InputField.ContentType.Custom;
            stakeInput.characterValidation = TMP_InputField.CharacterValidation.Digit;
            stakeInput.characterLimit = 15;
            stakeInput.SetTextWithoutNotify(stake.ToString(CultureInfo.InvariantCulture));
            // Typing only records the number; PlaceBet and the wallet check decide whether it can be played.
            stakeInput.onValueChanged.AddListener(t =>
            {
                if (PresentationBusy) return;
                stake = long.TryParse(t, NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : 0;
                feedback = null;
            });
            stakeInput.onEndEdit.AddListener(t => { if (stake >= 1) SetStake(stake); });
            halfStake = Button("Stake Half", slip, 178, 116, 64, 34, "½", () => { if (!PresentationBusy) { SetStake(stake / 2); feedback = null; } });
            doubleStake = Button("Stake Double", slip, 248, 116, 68, 34, "×2", () =>
            {
                if (PresentationBusy) return;
                long room = Math.Max(1, CasinoRules.AllIn(controller != null ? controller.Host.Money : 0));
                SetStake(Math.Min(Math.Max(1, stake) * 2, room)); feedback = null;
            });
        }

        /// <summary>Slots only: the drawn symbol replaces the text face (value -1 = hidden reel); no icon keeps the text.</summary>
        void ShowSlotIcon(int index, int value)
        {
            if (index >= slotIcons.Count || slotIcons[index] == null) return;
            bool on = value >= 0 && value < SlotIconIds.Length && LingGuangV05.Desktop.XingGuang.XgIcons.Apply(slotIcons[index], "casino", SlotIconIds[value]);
            slotIcons[index].gameObject.SetActive(on);
            if (on) faces[index].text = "";
        }

        /// <summary>Puts a drawn chip on a stake button. With the amount on the chip the label is hidden; otherwise it stays, pushed right of the icon.</summary>
        void DecorateChip(Button button, string iconId, float cx, float cy, float size, bool keepLabel)
        {
            if (LingGuangV05.Desktop.XingGuang.XgIcons.Place(button.transform, "casino", iconId, cx, cy, size) == null) return;
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label == null) return;
            if (!keepLabel) { label.gameObject.SetActive(false); return; }
            label.rectTransform.offsetMin += new Vector2(size - 4, 0);
            label.textWrappingMode = TextWrappingModes.NoWrap; label.fontSize = 15;
        }

        void SetFace(int index, int value)
        {
            faces[index].text = Face(Games[game], value);
            ShowSlotIcon(index, value);
            if (game == 1 && index < dicePips.Count)
                for (int d = 0; d < 7; d++) dicePips[index][d].gameObject.SetActive((PipMasks[value] & (1 << d)) != 0);
        }

        void PlaceBet()
        {
            if (controller == null || !ReferenceEquals(controller.Sim, bound) || PresentationBusy || ScamBlocks) return;
            if (Games[game] == "blackjack")
            {
                int before = bound.Casino.rounds;
                if (!bound.StartBlackjack(stake, controller.Host)) { feedback = Lang.T("余额不足或上一手尚未结束"); return; }
                Sound("chip", XgJuice.Sfx.Id.Clack, .95f, .8f);
                PersistBlackjack(before); return;
            }
            if (!bound.TryCasinoBet(Games[game], choice, stake, controller.Host, out var round))
            { feedback = Lang.T("余额不足或上一局尚未结束"); return; }
            reveal = round; revealAt = Time.unscaledTime + 1.2f;
            // The spin is heard for the 1.2 s it takes (the clips are cut to it); 龙虎's cards bring their own.
            switch (round.game)
            {
                case "slots": Sound("reel_spin", XgJuice.Sfx.Id.Whoosh, 1, .7f); break;
                case "dice": Sound("dice", XgJuice.Sfx.Id.Clack, 1, .9f); break;
                case "roulette": Sound("roulette", XgJuice.Sfx.Id.Whoosh, 1, .9f); break;
                default: Sound("chip", XgJuice.Sfx.Id.Clack, 1, .7f); break;
            }
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
            if (!ReferenceEquals(bound, controller.Sim)) { CancelPresentation(); bound = controller.Sim; feedback = null; Build(); ShowLastResult(); }
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
            else if (reveal != null || shownRound != bound.Casino.rounds)
            {
                if (reveal != null && reveal.game == "slots") Sound("reel_stop", XgJuice.Sfx.Id.Thud, 1, .8f);
                reveal = null; ShowLastResult();
            }
            RevealPayout();
            if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .1f; Refresh(); }
            TickScam();
        }

        void Refresh()
        {
            if (bound == null) return;
            var c = bound.Casino;
            double money = controller.Host.Money - (pendingPayout != null ? pendingPayout.returned : 0);
            if (payoutRollAmount > 0) money -= payoutRollAmount * (1 - Mathf.SmoothStep(0, 1, (Time.unscaledTime - payoutRollStart) / 1.2f));
            wallet.text = Lang.T("余额  ") + "¥" + Money(money);
            totals.text = Lang.T("累计净输赢  ") + Signed(c.returned - c.wagered - (pendingPayout?.returned ?? 0))
                + (bound.S.casinoFrozen > 0 ? "\n<size=13>" + T("其中被冻结  ¥", "of which frozen  ¥") + Money(bound.S.casinoFrozen) + "</size>" : "");
            string id = Games[game];
            for (int i = 0; i < tabs.Count; i++) { tabs[i].interactable = !PresentationBusy; tabs[i].GetComponent<Image>().color = game == i ? Burgundy : Panel; }
            bool stakeLocked = PresentationBusy || bound.BlackjackActive;
            long allIn = CasinoRules.AllIn(money);
            for (int i = 0; i < chips.Count; i++)
            {
                chips[i].interactable = !stakeLocked;
                bool picked = i < Stakes.Length ? stake == Stakes[i] : allIn >= 1 && stake == allIn;
                chips[i].GetComponent<Image>().color = picked ? new Color32(151, 100, 37, 255) : Burgundy;
            }
            if (stakeInput != null) stakeInput.interactable = !stakeLocked;
            if (halfStake != null) halfStake.interactable = !stakeLocked;
            if (doubleStake != null) doubleStake.interactable = !stakeLocked;
            for (int i = 0; i < choices.Count; i++)
            {
                choices[i].gameObject.SetActive(id != "slots" && id != "blackjack"); choices[i].interactable = !PresentationBusy;
                choices[i].GetComponentInChildren<TMP_Text>().text = ChoiceName(id, i);
                choices[i].GetComponent<Image>().color = choice == i ? new Color32(151, 100, 37, 255) : Burgundy;
            }
            ticket.text = Lang.T("本局  ") + "¥" + Money(id == "blackjack" && bound.BlackjackActive ? c.blackjack.stake : stake) + "  /  " + (id == "blackjack" ? "BLACKJACK" : id == "slots" ? Lang.T("单次旋转") : ChoiceName(id, choice));
            bet.GetComponentInChildren<TMP_Text>().text = bound.BlackjackActive ? Lang.T("请先完成 21 点这一手") : Spinning ? Lang.T("开奖中…") : Lang.T("下注  ") + "¥" + Money(stake);
            bool affordable = CasinoRules.IsStake(stake) && money >= stake && !double.IsNaN(money) && !double.IsInfinity(money);
            bet.interactable = !bound.BlackjackActive && !PresentationBusy && !ScamBlocks && bound.Clock >= c.nextBetAt && affordable;
            hint.text = feedback ?? (bound.BlackjackActive ? Lang.T("关页不退注 · 回到 21 点可继续")
                : allIn < 1 ? Lang.T("余额不足，去标注台赚些零钱")
                : !CasinoRules.IsStake(stake) ? Lang.T("请输入下注金额，或点「全押」")
                : money < stake ? Lang.T("超出余额 · 最多可押 ¥") + Money(allIn)
                : Lang.T("点击即扣款 · 返还包含本金 · 下注不设上限"));
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
                if (same) SetFace(i, value); else { faces[i].text = "?"; ShowSlotIcon(i, -1); }
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
                case "slots": return Lang.T("规则：三轴各有 6 种等概率符号；任意两个相同返还 2 倍，三个相同返还 3 倍，三个 7 返还 20 倍，其余为 0。赢面 44%，理论返还率 99.5%。");
                case "dice": return Lang.T("规则：三枚六面骰。小为总点数 4–10，大为 11–17，押中返还 2 倍；三个相同（豹子）退回一半本金。赢面 49%，理论返还率 98.6%。");
                case "roulette": return Lang.T("规则：0–36 共 37 格等概率，18 红、18 黑，押中颜色返还 2 倍；0 为绿色，退回一半本金。赢面 49%，理论返还率 98.6%。");
                case "blackjack": return Lang.T("规则：单副牌每手洗牌，A 计 1 或 11，J/Q/K 计 10。庄家软 17 停牌；自然 21 点赢 3:2，普通胜局赢 1:1，平局退注。首两张可加倍，只补一张；不分牌、无保险。基本策略下理论返还率约 99.5%。");
                default: return Lang.T("规则：龙、虎各独立抽取 A–K（1–13），点数大的一方胜，押中返还 2 倍；同点退回 3/4 本金。赢面 46%，理论返还率 98.1%。");
            }
        }
    }
}
