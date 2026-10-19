using System.Collections.Generic;
using LingGuangV05.Desktop.Zhongbao;
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
    /// </summary>
    public sealed partial class XgLabelPage
    {
        XgDeskGamePage deskGame;
        XgBtn gameToggle;
        XgSim gameOwner;
        /// <summary>Desks whose player picked the yes/no cards (kept for this session).</summary>
        readonly HashSet<string> classicDesks = new HashSet<string>();
        public bool DeskGameVisible => deskGame != null && deskGame.root != null && deskGame.root.gameObject.activeInHierarchy;

        void BuildDeskGame()
        {
            deskGame = new XgDeskGamePage(); deskGame.Init(host, ui);
            deskGame.ShowClassic = () => { if (Sim != null) classicDesks.Add(Desk); Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); };
            deskGame.BuyRaise = BuyRaise;
            deskGame.Answered = result => { AnswerCount++; if (result.pay > 0) Remember(HistoryMark.Right); else Remember(HistoryMark.Wrong); };
            deskGame.Build(root);
            deskGame.root.offsetMin = new Vector2(HallW + ColumnGap, 0);
            deskGame.root.gameObject.SetActive(false);

            // The way back from the yes/no cards: bottom left of the card area, where the writing view keeps its own.
            gameToggle = ZhongbaoSkin.Outlined(ui, body, "", () => { classicDesks.Remove(Desk); Fx.Play(XgJuice.Sfx.Id.Click); Refresh(); }, 13, ZhongbaoSkin.Line);
            var frame = ZhongbaoSkin.Frame(gameToggle);
            frame.anchorMin = frame.anchorMax = new Vector2(0, 0); frame.offsetMin = new Vector2(14, 8); frame.offsetMax = new Vector2(154, 34);
            if (DarkTeaching)
            {
                // A reserved footer cell cannot overlap the feedback or the multiline info text.
                frame.SetParent(infoBack, false);
                frame.anchorMin = frame.anchorMax = new Vector2(1, .5f);
                frame.offsetMin = new Vector2(-154, -14); frame.offsetMax = new Vector2(-10, 14);
                steps.rectTransform.offsetMax = new Vector2(-166, -2);
            }
            frame.name = "ToDeskGame";
            UiTip.Add(frame, "回到小游戏：这一桌的题改成玩游戏来答，答一题照样算一条样本。", "Back to the game: this desk's items are answered by playing, and each answer is still one sample.");
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
            if (show != deskGame.root.gameObject.activeSelf) { deskGame.root.gameObject.SetActive(show); if (show) deskGame.root.SetAsLastSibling(); }
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
    }
}
