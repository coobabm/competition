using System.Collections.Generic;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using Michsky.DreamOS;
using UnityEngine;

namespace LingGuangV05.Desktop.Casino
{
    public sealed partial class CasinoPage
    {
        XgJuice juice;
        XgGlow winGlow;
        RectTransform board;
        WindowManager browserWindow;
        float glowUntil;
        XgCasinoRound pendingPayout;
        int pendingStreak;
        readonly List<CasinoCardMotion> cardMotions = new List<CasinoCardMotion>();
        bool CardsBusy { get { foreach (var m in cardMotions) if (m != null && m.Busy) return true; return false; } }
        bool PresentationBusy => Spinning || CardsBusy || juice != null && juice.IsHitStopped;
        bool PageVisible()
        {
            if (!isActiveAndEnabled || root == null || !root.gameObject.activeInHierarchy) return false;
            if (browserWindow == null) browserWindow = GetComponentInParent<WindowManager>(true);
            var group = browserWindow != null ? browserWindow.GetComponent<CanvasGroup>() : null;
            return browserWindow == null || browserWindow.isOn && (group == null || group.alpha > .02f);
        }
        void EnsureEffects()
        {
            if (juice != null || board == null || controller == null) return;
            juice = board.gameObject.AddComponent<XgJuice>();
            juice.Init(board, board, font, controller.gameObject);
            juice.LocalHitStop = true; juice.Visible = PageVisible;
            var halo = Rect("Win Glow", board, 16, 146, 736, 334);
            var image = PrologueDesk.Fill(halo, Color.clear, false);
            halo.SetSiblingIndex(arena.GetSiblingIndex());
            winGlow = halo.gameObject.AddComponent<XgGlow>(); winGlow.image = image;
            winGlow.color = new Color(Gold.r, Gold.g, Gold.b, .35f);
            juice.BringToFront(); SyncEffects();
        }
        void SyncEffects()
        {
            if (juice == null || bound == null) return;
            juice.Reduced = bound.S.reduceFx; juice.Muted = bound.S.mute;
            if (winGlow != null) { winGlow.on = Time.unscaledTime < glowUntil; winGlow.color.a = juice.Reduced ? .08f : .35f; }
        }
        void QueuePayout(XgCasinoRound round)
        {
            pendingPayout = round;
            pendingStreak = CasinoPresentation.WinStreak(bound.Casino.history);
        }
        void RevealPayout()
        {
            if (pendingPayout == null || PresentationBusy || !PageVisible()) return;
            var round = pendingPayout; pendingPayout = null;
            int tier = CasinoPresentation.WinTier(round);
            if (tier == 0 || juice == null) return;
            SyncEffects();
            bool big = tier == 2;
            var at = juice.At(arena);
            juice.Flash(Color.white, .09f, big ? .42f : .25f);
            juice.HitStop(big ? 65 : 45);
            juice.Shake(big ? 7 : 3, .28f);
            juice.Knock(arena, juice.Reduced ? .02f : .07f, new Vector2(big ? 12 : 6, -5), .32f, true);
            juice.Knock(wallet.rectTransform, .12f, Vector2.up * 4, .32f, true);
            foreach (var m in cardMotions) if (m != null) juice.Knock((RectTransform)m.transform, .09f, new Vector2(4, -5), .3f, true);
            juice.Float(at + Vector2.up * 35, (big ? T("大赢！ ", "BIG WIN! ") : "") + Signed(round.returned - round.stake), Gold, big ? 42 : 34, 85, 1.05f, 1.4f);
            juice.Burst(at, big ? 28 : 14, Gold, XgJuice.Shape.Yen, big ? 300 : 190, 440, .8f);
            if (big) juice.Burst(at, 16, Gold, XgJuice.Shape.Confetti, 260, 360, .8f);
            juice.Shockwave(at, Gold, big ? 250 : 170, .4f, big ? 10 : 6);
            juice.TransferCoins(at, juice.At(wallet.rectTransform), big ? 10 : 6);
            juice.Play(big ? XgJuice.Sfx.Id.Fanfare : XgJuice.Sfx.Id.Coin, Mathf.Min(1.35f, 1 + .04f * pendingStreak), .8f);
            if (pendingStreak >= 2)
            {
                juice.Float(at + Vector2.down * 30, T("连胜 ×", "WIN STREAK ×") + pendingStreak + (pendingStreak >= 8 ? "+" : ""), White, 24, 35, 1.2f);
                juice.Play(XgJuice.Sfx.Id.Tier, 1 + Mathf.Min(pendingStreak, 6) * .04f, .5f);
            }
            glowUntil = Time.unscaledTime + 1.3f;
        }
        void CancelPresentation()
        {
            pendingPayout = null; reveal = null; glowUntil = 0;
            foreach (var motion in cardMotions) if (motion != null) motion.Cancel();
            if (juice != null) juice.Clear();
            if (winGlow != null) winGlow.on = false;
        }
        void OnDisable() => CancelPresentation();
        CasinoCardMotion CardMotion(RectTransform card, RectTransform front, RectTransform back)
        {
            var motion = card.gameObject.AddComponent<CasinoCardMotion>();
            motion.Front = front; motion.Back = back; motion.Juice = juice;
            motion.Reduced = bound != null && bound.S.reduceFx;
            motion.CanDrag = () => !PresentationBusy;
            cardMotions.Add(motion); return motion;
        }
    }
}
