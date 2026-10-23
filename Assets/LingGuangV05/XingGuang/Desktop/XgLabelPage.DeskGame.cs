using System.Collections.Generic;
using LingGuangV05.Desktop.Zhongbao;
using TMPro;
using LingGuangV05.Desktop.XingGuang.MiniGames;
using LingGuangV05.XingGuang;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// A desk with a native game (XgMiniGames.ForDesk) opens it as its main view, over the workspace and the right column; the
    /// task hall stays on the left so the player can still change desk. A toggle switches to the classic yes/no cards and a
    /// button on the cards switches back. Same pattern as the writing view of 手写数字 (XgLabelPage.Handwriting.cs).
    /// The games book every answer through XgSim.SettleMiniAction, so a game labels its own desk's dataset.
    /// F10 (玩家反馈 2026-10-22): the game used to cover the right column, and with it 自动答题 and 去训练. A strip on top
    /// of the game (<see cref="RefreshDeskGameAuto"/>) now shows the desk's 自动答题 state, 「交给它 / 我来做」 and the jump to
    /// training on every view; the unified 工位卡 (v4 §3.1) replaces it later.
    /// </summary>
    public sealed partial class XgLabelPage
    {
        XgDeskGamePage deskGame;
        XgBtn gameToggle;
        XgSim gameOwner;
        /// <summary>Desks whose player picked the yes/no cards (kept for this session).</summary>
        readonly HashSet<string> classicDesks = new HashSet<string>();
        /// <summary>Height of the 自动答题 strip above a desk's game.</summary>
        const float AutoStripH = 36;
        RectTransform autoStrip;
        TMP_Text autoStripText;
        XgBtn autoStripToggle, autoStripTrain;
        float autoStripClock;
        public bool DeskGameVisible => deskGame != null && deskGame.root != null && deskGame.root.gameObject.activeInHierarchy;

        void BuildDeskGame()
        {
            deskGame = new XgDeskGamePage(); deskGame.Init(host, ui);
            deskGame.ShowClassic = () => { if (Sim != null) classicDesks.Add(Desk); Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); };
            deskGame.BuyRaise = BuyRaise;
            deskGame.Answered = result => { AnswerCount++; if (result.pay > 0) Remember(HistoryMark.Right); else Remember(HistoryMark.Wrong); };
            deskGame.Build(root);
            deskGame.root.offsetMin = new Vector2(HallW + ColumnGap, 0);
            deskGame.root.offsetMax = new Vector2(0, -AutoStripH);
            deskGame.root.gameObject.SetActive(false);
            BuildDeskGameAuto();

            // A dedicated footer row shares width with the streak, never the history or scrollable card content.
            gameToggle = ZhongbaoSkin.Outlined(ui, workspace, "", () => { classicDesks.Remove(Desk); Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 13, ZhongbaoSkin.Line);
            var frame = ZhongbaoSkin.Frame(gameToggle);
            frame.anchorMin = frame.anchorMax = new Vector2(1, 0);
            frame.offsetMin = new Vector2(-154, 39); frame.offsetMax = new Vector2(-10, 65);
            frame.name = "ToDeskGame";
            ShowFrame(gameToggle, false);
        }

        /// <summary>The desk's game can be on screen now (the player may still have picked the yes/no cards).</summary>
        bool DeskGameEligible(string gameId)
        {
            if (WorkContext || gameId == null || Sim == null || Mode != 0 || Sim.MiniGameBlocker(gameId) != null) return false;
            // Story observations and review judgments must remain actual answers to the displayed original card.
            if (Sim.CaptchaPending || Sim.CaptchaPauseLeft > 0 || Sim.QualityFrozen || Sim.ReviewCard(Desk) != null) return false;
            if (XgTeachingPacks.Find(gameId) != null) return true;
            if (!Sim.DeskOpen(Desk)) return false;
            var card = CurrentCard;
            return !(card.family || card.law || card.kind == "shutdown" || card.bottleneckPreview || card.kind == "combo");
        }

        /// <summary>Follows the desk: shows its game, or the way back to it. True while the game covers the cards. <paramref name="full"/> also refreshes the game's texts (the tick leaves that to the game's own clock).</summary>
        bool RefreshDeskGame(bool full)
        {
            if (deskGame == null || Sim == null) return false;
            if (!ReferenceEquals(gameOwner, Sim)) { gameOwner = Sim; classicDesks.Clear(); deskGame.Stop(); }
            string id = Sim.TeachingPackAvailable(Sim.S.teachingPack) && XgTeachingPacks.Find(Sim.S.teachingPack)?.dataset == Desk ? Sim.S.teachingPack : XgMiniGames.ForDesk(Desk);
            bool eligible = DeskGameEligible(id);
            bool show = eligible && !classicDesks.Contains(Desk);
            if (show != deskGame.root.gameObject.activeSelf) { deskGame.root.gameObject.SetActive(show); if (show) { deskGame.root.SetAsLastSibling(); autoStrip.SetAsLastSibling(); } }
            if (autoStrip.gameObject.activeSelf != show) autoStrip.gameObject.SetActive(show);
            if (show && (full || (autoStripClock -= UnityEngine.Time.unscaledDeltaTime) <= 0)) RefreshDeskGameAuto();
            bool toggleShow = eligible && !show;
            var toggleFrame = ZhongbaoSkin.Frame(gameToggle);
            bool toggleChanged = toggleFrame != null && toggleFrame.gameObject.activeSelf != toggleShow;
            ShowFrame(gameToggle, toggleShow);
            if (toggleShow && (full || toggleChanged))
                gameToggle.Set(T("← 改回小游戏", "← Back to the game"), true, DarkTeaching ? XgDark.Button : Color.white, DarkTeaching ? XgDark.Accent : ZhongbaoSkin.Blue);
            if (!show) return false;
            // Building/refreshing a hidden page is not a failed game launch or a player preference.
            // Leave it pending; the next visible refresh starts it. Focus still gates input, not rendering.
            if (!deskGame.Visible) { deskGame.Stop(); return false; }
            if (deskGame.SelectedGame != id && !deskGame.Open(id))
            {
                // It cannot start (the desk shut under us): the cards take over.
                classicDesks.Add(Desk); deskGame.root.gameObject.SetActive(false); ShowFrame(gameToggle, true);
                return false;
            }
            if (full) deskGame.Refresh();
            return true;
        }

        void BuildDeskGameAuto()
        {
            bool dark = ui.dark;
            autoStrip = Rect("DeskGameAuto", root, new Vector2(0, 1), Vector2.one, new Vector2(HallW + ColumnGap, -AutoStripH), Vector2.zero);
            Panel(autoStrip, dark ? XgDark.Card : Color.white);
            Panel(Rect("Rule", autoStrip, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 1)), dark ? XgDark.Line : ZhongbaoSkin.Line).raycastTarget = false;
            autoStripText = ui.Text(Rect("State", autoStrip, Vector2.zero, Vector2.one, new Vector2(14, 0), new Vector2(-260, 0)), "", 14, dark ? XgDark.Ink : XgPalette.Ink, TextAlignmentOptions.MidlineLeft);
            autoStripText.textWrappingMode = TextWrappingModes.NoWrap; autoStripText.overflowMode = TextOverflowModes.Ellipsis; autoStripText.richText = true;
            autoStripToggle = ui.Button(autoStrip, "", () =>
            {
                if (Sim == null || Sim.AutoLevel(Desk) <= 0 || Sim.AutoLabelHidden) return;
                Sim.AutoPaused = !Sim.AutoPaused; Fx.Play(XgJuice.Sfx.Id.Click); Refresh();
            }, 13);
            autoStripToggle.rt.name = "AutoHandOver";
            autoStripToggle.rt.anchorMin = autoStripToggle.rt.anchorMax = new Vector2(1, .5f);
            autoStripToggle.rt.offsetMin = new Vector2(-250, -13); autoStripToggle.rt.offsetMax = new Vector2(-136, 13);
            UiTip.Add(autoStripToggle.rt, () => Sim != null && Sim.AutoPaused
                ? T("交给它：它接着替你答题挣钱（所有题桌）。", "Hand it over: it goes back to answering for you (every desk).")
                : T("我来做：它先不替你答题，也不挣这份钱（所有题桌）；你自己答的不受影响。", "I'll do it: it stops answering for you and earns nothing from it (every desk); your own answers are not affected."));
            autoStripTrain = ui.Button(autoStrip, "", () =>
            {
                var d = Sim != null ? XgCatalog.Dataset(Desk) : null;
                if (d == null) return;
                Sim.SelectedTrack = d.track; Sim.SetDataset(Sim.SelectedTrack, Desk); host.ShowTab("train");
            }, 13);
            autoStripTrain.rt.name = "AutoToTrain";
            autoStripTrain.rt.anchorMin = autoStripTrain.rt.anchorMax = new Vector2(1, .5f);
            autoStripTrain.rt.offsetMin = new Vector2(-128, -13); autoStripTrain.rt.offsetMax = new Vector2(-10, 13);
            UiTip.Add(autoStripTrain.rt, "带着这张桌的数据去训练页。", "Go to the training page with this desk's data.");
            autoStrip.gameObject.SetActive(false);
        }

        /// <summary>The strip over the desk's game: what 自动答题 does on this desk, its 「交给它 / 我来做」 switch and 去训练.</summary>
        void RefreshDeskGameAuto()
        {
            autoStripClock = .5f;
            if (autoStrip == null || Sim == null) return;
            // The right column under the game re-sorts its boxes; the strip stays on top of the page.
            if (autoStrip.GetSiblingIndex() != autoStrip.parent.childCount - 1) autoStrip.SetAsLastSibling();
            bool dark = ui.dark;
            int level = Sim.AutoLevel(Desk);
            bool pausable = level > 0 && !Sim.AutoLabelHidden;
            XgAutoDeskPlan plan = null;
            if (pausable) foreach (var p in Sim.AutoDeskPlans(Host)) if (p.desk.id == Desk) { plan = p; break; }
            string good = dark ? "#5FE3B5" : "#2E8B57", bad = dark ? "#FF7A6E" : "#D63031", muted = dark ? "#97999E" : "#68748C";
            string text;
            if (Sim.AutoLabelHidden) text = "<color=" + muted + ">" + T("自动答题 · ？？？", "Auto-answer · ???") + "</color>";
            else if (level <= 0) text = "<color=" + muted + ">" + T("自动答题 · 还没开（切到「是非卡」在右栏购买）", "Auto-answer · not bought yet (switch to the cards to buy it on the right)") + "</color>";
            else if (plan == null) text = "<color=" + muted + ">" + T("自动答题 · 这个小游戏不接自动答题", "Auto-answer · not used for this game") + "</color>";
            else switch (plan.state)
            {
                case XgAutoDeskState.Running:
                case XgAutoDeskState.InboxFull:
                    text = "<color=" + good + ">● " + T("自动中", "Auto on") + "</color> · " + T("每秒 ", "") + N(plan.rate, "0.0") + T(" 张", " cards/s") + " · " + T("约 ¥", "≈ ¥") + N(plan.income, "0.00") + T("/秒", "/s")
                        + " · " + T("检查点 ", "checkpoint ") + XgSim.Pct(plan.accuracy);
                    break;
                case XgAutoDeskState.PausedByYou:
                    text = "<color=" + bad + ">Ⅱ " + T("你来做 · 自动答题已暂停", "You're on it · auto-answer paused") + "</color>";
                    break;
                case XgAutoDeskState.BelowBar:
                    text = "<color=" + bad + ">" + T("检查点 " + XgSim.Pct(plan.accuracy) + "，要 " + XgSim.Pct(XgCatalog.AutoMinAccuracy) + " 它才替你答这桌", "Checkpoint " + XgSim.Pct(plan.accuracy) + "; it answers this desk from " + XgSim.Pct(XgCatalog.AutoMinAccuracy)) + "</color>";
                    break;
                case XgAutoDeskState.NoCheckpoint:
                    text = "<color=" + bad + ">" + T("这桌还没有检查点：先去训练", "No checkpoint for this desk yet: train first") + "</color>";
                    break;
                case XgAutoDeskState.Held:
                    text = "<color=" + bad + ">" + T("自动答题暂停：", "Auto-answer paused: ") + (Sim.QualityFrozen ? T("账号冻结中", "account frozen")
                        : Sim.CaptchaPending ? T("等你输验证码", "waiting for the captcha") : T("验证没过，暂停 " + XgSim.FreezeClock(Sim.CaptchaPauseLeft), "captcha failed, paused " + XgSim.FreezeClock(Sim.CaptchaPauseLeft))) + "</color>";
                    break;
                case XgAutoDeskState.Project:
                    text = "<color=" + bad + ">" + T("自动答题暂停：研发占着 GPU", "Auto-answer paused: research has the GPU") + "</color>";
                    break;
                default:
                    text = "<color=" + bad + ">" + T("自动答题暂停：", "Auto-answer paused: ") + (Host != null && !string.IsNullOrEmpty(Host.Blocker) ? Host.Blocker : T("显卡没在算", "the GPU is not running")) + "</color>";
                    break;
            }
            autoStripText.text = text;
            autoStripToggle.rt.gameObject.SetActive(pausable);
            if (pausable)
                autoStripToggle.Set(Sim.AutoPaused ? T("交给它", "Hand it over") : T("我来做", "I'll do it"), true,
                    Sim.AutoPaused ? (dark ? XgDark.Accent : XgPalette.Good) : (Color?)null, Sim.AutoPaused ? Color.white : (Color?)null);
            var dataset = XgCatalog.Dataset(Desk);
            bool trainVisible = dataset != null && (host.Controller == null || host.Controller.FeatureVisible("train"));
            autoStripTrain.rt.gameObject.SetActive(trainVisible);
            if (trainVisible) autoStripTrain.Set(T("去训练 →", "Train →"), Sim.TrainingUnlocked(dataset.track));
        }
    }
}
