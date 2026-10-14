using System;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    /// <summary>
    /// The main view of a labelling desk that has a native game (XgMiniGames.ForDesk). It hosts exactly one game, the one
    /// of the desk it sits on, and is not a tab: the label page (XgLabelPage.DeskGame.cs) decides when it shows and
    /// when the yes/no cards take over again. Every answered item is booked through XgSim.SettleMiniAction, so the pay and
    /// the sample always land on the desk's own dataset, by the one rule for all games.
    /// </summary>
    public sealed class XgDeskGamePage : XgPage
    {
        const float TopBar = 46, ChromeHeight = 92;
        RectTransform viewport, surface;
        TMP_Text heading, speech, status, payLine;
        XgBtn classic, raise;
        XgMiniGame game;
        XgMiniContext context;
        XgSim owner;
        string selected;
        long ticket;
        int sequence;
        float textClock;
        /// <summary>The player asked for the yes/no cards.</summary>
        public Action ShowClassic;
        /// <summary>The player wants the pay raise (the label page owns the purchase and its effects).</summary>
        public Action BuyRaise;
        /// <summary>An item was answered and settled.</summary>
        public Action<XgMiniResult> Answered;
        public string SelectedGame => selected;
        public bool Playing => game != null;
        public bool Visible => root != null && root.gameObject.activeInHierarchy && host.Visible;

        public override void Build(RectTransform area)
        {
            // The games are drawn on a light paper surface even inside the dark LingGuang shell.
            ui = new XgUi(ui.font, ui.window, false);
            root = XgUi.Rect("DeskGame", area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            XgUi.Panel(root, new Color32(238, 242, 248, 255));
            var life = root.gameObject.AddComponent<XgMiniLife>(); life.Page = this;
            heading = ui.Text(XgUi.Strip("Title", root, 8, 30, 16, 400), "", 20, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            heading.enableAutoSizing = true; heading.fontSizeMin = 13; heading.fontSizeMax = 20; heading.overflowMode = TextOverflowModes.Ellipsis;
            classic = ui.Button(root, "", () => ShowClassic?.Invoke(), 15); XgUi.PlaceTopRight(classic, 150, 7, 140, 32);
            raise = ui.Button(root, "", () => BuyRaise?.Invoke(), 15); XgUi.PlaceTopRight(raise, 310, 7, 150, 32);
            viewport = XgUi.Rect("GameViewport", root, Vector2.zero, Vector2.one, new Vector2(8, 92), new Vector2(-8, -46));
            viewport.gameObject.AddComponent<RectMask2D>();
            speech = ui.Text(XgUi.Rect("Companion", root, Vector2.zero, new Vector2(1, 0), new Vector2(14, 62), new Vector2(-14, 90)), "", 16, XgPalette.Accent, TextAlignmentOptions.MidlineLeft);
            speech.enableAutoSizing = true; speech.fontSizeMin = 11; speech.fontSizeMax = 16; speech.overflowMode = TextOverflowModes.Ellipsis;
            status = ui.Text(XgUi.Rect("Settlement", root, Vector2.zero, new Vector2(1, 0), new Vector2(14, 34), new Vector2(-14, 62)), "", 15, XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            status.enableAutoSizing = true; status.fontSizeMin = 11; status.fontSizeMax = 15; status.overflowMode = TextOverflowModes.Ellipsis;
            payLine = ui.Text(XgUi.Rect("PayRule", root, Vector2.zero, new Vector2(1, 0), new Vector2(14, 6), new Vector2(-14, 34)), "", 13, XgPalette.Muted, TextAlignmentOptions.MidlineLeft);
            payLine.enableAutoSizing = true; payLine.fontSizeMin = 10; payLine.fontSizeMax = 13; payLine.overflowMode = TextOverflowModes.Ellipsis;
        }

        public bool Focused
        {
            get
            {
                if (!Visible || LingGuangV05.Desktop.SaveSlotsPanel.IsOpen) return false;
                var window = ui.window;
                if (window == null || window.transform.parent == null) return true;
                var parent = window.transform.parent;
                for (int i = window.transform.GetSiblingIndex() + 1; i < parent.childCount; i++)
                {
                    var other = parent.GetChild(i).GetComponent<Michsky.DreamOS.WindowManager>();
                    if (other != null && other.isOn && other.gameObject.activeInHierarchy) return false;
                }
                return true;
            }
        }

        void NewSurface()
        {
            if (surface != null) { surface.gameObject.SetActive(false); XgMiniContext.DestroyObject(surface.gameObject); }
            surface = XgUi.Rect("Stage", viewport, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-450, -240), new Vector2(450, 240));
            Fit();
        }

        /// <summary>
        /// Scales the stage to the page and stacks the page top-down: title bar, stage, then the companion, settlement and pay
        /// lines right under it. Any spare height ends up as margin below the lines instead of a gap above the game.
        /// </summary>
        void Fit()
        {
            if (surface == null || viewport == null || root == null) return;
            float k = Mathf.Min((root.rect.width - 16) / XgMiniContext.Width, (root.rect.height - TopBar - ChromeHeight) / XgMiniContext.Height);
            if (k <= 0) return;
            surface.localScale = Vector3.one * k;
            float h = XgMiniContext.Height * k;
            viewport.anchorMin = new Vector2(0, 1); viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(8, -TopBar - h); viewport.offsetMax = new Vector2(-8, -TopBar);
            float y = TopBar + h + 4;
            foreach (var line in new[] { speech, status, payLine })
            {
                var rt = line.rectTransform;
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(14, -(y + 28)); rt.offsetMax = new Vector2(-14, -y);
                y += 28;
            }
        }

        public void Stop()
        {
            context?.Close(); game?.Dispose();
            if (owner != null && ticket != 0) owner.CancelMiniGame(ticket);
            game = null; context = null; ticket = 0; sequence = 0; selected = null;
            if (surface != null) { surface.gameObject.SetActive(false); XgMiniContext.DestroyObject(surface.gameObject); surface = null; }
        }

        public void Destroyed() { Stop(); }
        public void Suspended() { Stop(); }
        public override void Shown() { }

        /// <summary>Starts the game of a desk. False when it cannot start (the desk is shut or the stage not reached).</summary>
        public bool Open(string id)
        {
            if (!Visible || Sim == null) return false;
            var def = XgMiniGames.Find(id);
            if (def == null || def.Reward != XgMiniReward.Label || Sim.MiniGameBlocker(id) != null) return false;
            Stop(); owner = Sim;
            ticket = owner.BeginMiniGame(id, Host);
            if (ticket == 0) return false;
            selected = id; NewSurface();
            context = new XgMiniContext(surface, ui, owner, Host, id,
                () => Focused && ReferenceEquals(owner, Sim), () => owner.MiniGameSkill(id),
                text => { if (speech != null) speech.text = (text ?? "").Replace("\n", "  ·  "); }, Settle);
            game = XgActionGames.Create(id, context) ?? XgBoardGames.Create(id, context) ?? XgSocialGames.Create(id, context);
            if (game == null) { Stop(); return false; }
            speech.text = ""; status.text = XgUi.T("答一题，就是一条样本，钱直接进账。", "Every answer is one sample, and the pay goes straight to your balance.");
            game.Build(); Fit(); RefreshTexts();
            return true;
        }

        void Settle(bool correct, double seconds)
        {
            if (owner == null || !ReferenceEquals(owner, Sim) || !Focused) return;
            var result = owner.SubmitMiniGame(ticket, sequence + 1, correct, Host, seconds);
            if (!result.accepted) { status.text = result.note; return; }
            sequence++;
            status.text = (correct ? "<color=#2F9E44>" : "<color=#D63031>") + result.note + "</color>";
            host.Controller?.runtime?.MarkDirty();
            if (Fx != null)
            {
                Fx.Play(correct ? XgJuice.Sfx.Id.Tick : XgJuice.Sfx.Id.Clack);
                if (correct && result.pay > 0) Fx.Float(Fx.At(status.rectTransform, new Vector2(-120, 30)), "+¥" + result.pay.ToString("0.00"), XgPalette.Money, 22);
                else if (!correct && result.fine > 0) Fx.Float(Fx.At(status.rectTransform, new Vector2(-120, 30)), "-¥" + result.fine.ToString("0.00"), XgPalette.Bad, 22);
            }
            Answered?.Invoke(result);
            RefreshTexts();
        }

        /// <summary>Texts that follow the sim and the language: the title, the pay rule, the raise button.</summary>
        void RefreshTexts()
        {
            if (root == null || Sim == null) return;
            var def = XgMiniGames.Find(selected);
            var desk = def == null ? null : XgCatalog.Desk(def.Desk);
            heading.text = def == null ? "" : XgUi.T(def.Name, def.NameEn) + (desk == null ? "" : "  <size=70%><color=#7A7F8C>" + XgUi.T(desk.name, desk.nameEn) + "</color></size>");
            classic.Set(XgUi.T("改答是非题", "Yes/No cards"), true);
            if (def != null)
            {
                double basePay = Sim.MiniBasePay(def.Desk);
                double fine = Sim.HandFineFor(def.Desk, Sim.LevelOf(def.Desk));
                payLine.text = XgUi.T(
                    "每答一题 ¥" + basePay.ToString("0.00") + " 加一条样本；答得快或连对最多再加 100%；" + (fine > 0 ? "答错扣 ¥" + fine.ToString("0.00") : "答错不扣钱") + "。今天 " + Sim.DayDone + " 条 · ¥" + Sim.DayIncome.ToString("0.00"),
                    "Each answer ¥" + basePay.ToString("0.00") + " and one sample; a quick answer or a streak adds up to +100%; " + (fine > 0 ? "a wrong one costs ¥" + fine.ToString("0.00") : "a wrong one costs nothing") + ". Today " + Sim.DayDone + " · ¥" + Sim.DayIncome.ToString("0.00"));
            }
            bool maxed = Sim.RaiseLevel >= XgCatalog.RaiseMax;
            double cost = Sim.NextRaiseCost;
            bool can = !maxed && Host.Money + 1e-9 >= cost;
            raise.Set(maxed ? XgUi.T("加薪 已满", "Raise maxed") : XgUi.T("加薪 ¥", "Raise ¥") + XgUi.Money(cost), can);
        }

        void CheckOwner()
        {
            if (selected != null && (!ReferenceEquals(owner, Sim) || game != null && !owner.MiniGameTicketValid(ticket))) Stop();
            if (Visible && Sim != null) Sim.MiniGameSurfaceCheck = () => Visible;
        }

        public override void Refresh()
        {
            if (root == null) return;
            CheckOwner(); Fit(); RefreshTexts(); textClock = 0;
        }

        public override void Tick(float dt)
        {
            if (root == null) return;
            CheckOwner();
            // The texts follow money and the day; four times a second is plenty and keeps the frame free of string building.
            if ((textClock += dt) >= .25f) { textClock = 0; Fit(); RefreshTexts(); }
            if (Focused && game != null) game.Tick(Mathf.Clamp(dt, 0, .1f));
        }
    }

    /// <summary>Stops the game when its page is hidden (another tab, another window) so no ticket outlives its view.</summary>
    public sealed class XgMiniLife : MonoBehaviour
    {
        public XgDeskGamePage Page;
        void OnEnable() { Page?.Shown(); }
        void OnDisable() { Page?.Suspended(); }
        void OnDestroy() { Page?.Destroyed(); }
    }
}
