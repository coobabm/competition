using System.Collections.Generic;
using LingGuangV05.Desktop.Story;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LingGuangV05.Core;
namespace LingGuangV05.Desktop.Casino
{
    public sealed partial class CasinoPage
    {
        readonly List<Button> blackjackButtons = new List<Button>();
        XgBlackjackHand shownHand;
        int shownBlackjackRevision = -1, shownPlayers, shownDealer;
        bool shownHoleHidden;
        TMP_Text dealerScore, playerScore;
        static readonly Vector2 ShoePosition = new Vector2(682, -109);
        void AddBlackjackButtons(RectTransform slip)
        {
            string[] actions = { "hit", "stand", "double" };
            string[] labels = { Lang.T("要牌"), Lang.T("停牌"), Lang.T("加倍") };
            for (int i = 0; i < actions.Length; i++)
            {
                string action = actions[i];
                var b = Button("Blackjack " + action, slip, 18 + i * 101, 170, 96, 40, labels[i], () =>
                {
                    if (controller == null || !ReferenceEquals(controller.Sim, bound) || PresentationBusy) return;
                    int before = bound.Casino.rounds;
                    if (bound.PlayBlackjack(action, controller.Host)) PersistBlackjack(before);
                });
                b.GetComponentInChildren<TMP_Text>().fontSize = 16;
                blackjackButtons.Add(b);
            }
        }
        void PersistBlackjack(int previousRound)
        {
            if (bound.Casino.rounds > previousRound) QueuePayout(bound.Casino.history[bound.Casino.history.Count - 1]);
            feedback = null; BuildArena(); Refresh();
            controller.runtime.MarkDirty(); controller.SaveNow();
        }
        void RefreshBlackjackButtons()
        {
            var hand = bound.Casino.blackjack;
            for (int i = 0; i < blackjackButtons.Count; i++)
            {
                var b = blackjackButtons[i]; b.gameObject.SetActive(Games[game] == "blackjack");
                b.interactable = !PresentationBusy && bound.BlackjackActive && bound.Clock >= hand.nextActionAt
                    && (i != 2 || hand.player.Count == 2 && !hand.doubled && controller.Host.Money >= hand.stake);
            }
            if (Games[game] != "blackjack" || dealerScore == null || playerScore == null) return;
            dealerScore.text = Lang.T("庄家"); playerScore.text = T("你", "YOU");
            if (hand == null) return;
            if (CardsBusy) { dealerScore.text += "\n…"; playerScore.text += "\n…"; return; }
            dealerScore.text += hand.active ? Lang.T("\n暗牌未翻") : "\n" + BlackjackRules.Score(hand.dealer);
            playerScore.text += "\n" + BlackjackRules.Score(hand.player) + (BlackjackRules.Score(hand.player) > 21 ? Lang.T(" 爆牌") : "");
        }
        void BuildBlackjackTable()
        {
            var hand = bound?.Casino.blackjack;
            bool same = ReferenceEquals(shownHand, hand);
            int oldPlayers = same ? shownPlayers : 0, oldDealer = same ? shownDealer : 0;
            bool flipHole = hand != null && same && shownHoleHidden && !hand.active;
            float flipDelay = hand != null && same && hand.player.Count > oldPlayers ? .68f : 0;
            shownHand = hand; shownBlackjackRevision = hand?.revision ?? 0;
            shownPlayers = hand?.player.Count ?? 0; shownDealer = hand?.dealer.Count ?? 0; shownHoleHidden = hand != null && hand.active;
            Label("Blackjack Terms", arena, 20, 47, 680, 27, Lang.T("BLACKJACK 3:2    /    庄家软 17 停牌"), 17, Gold, TextAlignmentOptions.Center);
            dealerScore = Label("Dealer Score", arena, 18, 84, 132, 70, Lang.T("庄家"), 18, White, TextAlignmentOptions.Center);
            playerScore = Label("Player Score", arena, 18, 192, 132, 70, T("你", "YOU"), 18, White, TextAlignmentOptions.Center);
            for (int i = 2; i >= 0; i--)
            {
                var shoe = Rect("Card Shoe", arena, 659 + i * 2, 74 - i * 2, 46, 66);
                PrologueDesk.Fill(shoe, i == 0 ? Burgundy : Gold, false);
                if (i == 0) Label("Shoe Mark", shoe, 1, 1, 44, 64, "888\n<size=10>VIP</size>", 18, Gold, TextAlignmentOptions.Center);
            }
            if (hand == null)
                Label("Deal Prompt", arena, 160, 100, 470, 150, Lang.T("先选筹码，再发牌。\n不要超过 21 点。"), 26, White, TextAlignmentOptions.Center);
            else
            {
                DrawHand(hand.dealer, 82, false, hand.active, oldPlayers, oldDealer, flipHole, flipDelay);
                DrawHand(hand.player, 190, true, false, oldPlayers, oldDealer, false, flipDelay);
            }
            Label("Hand Status", arena, 20, 282, 680, 25, Lang.T("牌从牌堆发出 · 可轻拖牌面，松手归位 · 关页保留本手"), 13, White, TextAlignmentOptions.Center);
        }
        void DrawHand(List<int> cards, float y, bool player, bool hideHole, int oldPlayers, int oldDealer, bool flipHole, float flipDelay)
        {
            float step = cards.Count > 8 ? 43 : 60;
            int oldCount = player ? oldPlayers : oldDealer;
            for (int i = 0; i < cards.Count; i++)
            {
                bool hidden = hideHole && i == 1;
                var rt = Rect("Card " + y + " " + i, arena, 161 + i * step, y, 52, 76);
                PrologueDesk.Fill(rt, Gold);
                var front = Rect("Card Front", rt, 1, 1, 50, 74); PrologueDesk.Fill(front, White, false);
                var back = Rect("Card Back", rt, 1, 1, 50, 74); PrologueDesk.Fill(back, Burgundy, false);
                string rank = Face("dragon", BlackjackRules.Rank(cards[i]));
                string[] suit = { Lang.T("黑桃"), Lang.T("红桃"), Lang.T("梅花"), Lang.T("方片") };
                Color ink = cards[i] / 13 == 1 || cards[i] / 13 == 3 ? Burgundy : Background;
                Label("Card Value", front, 1, 2, 48, 70, "<size=12>" + suit[cards[i] / 13] + "</size>\n" + rank, 28, ink, TextAlignmentOptions.Center);
                Label(!player && i == 1 ? "Hidden Dealer Card" : "Card Back Value", back, 1, 2, 48, 70, "888", 20, Gold, TextAlignmentOptions.Center);
                var motion = CardMotion(rt, front, back);
                float scale = motion.Reduced ? .4f : 1;
                if (i >= oldCount)
                {
                    float afterFlip = flipHole ? flipDelay + .30f : 0;
                    motion.Deal(ShoePosition, CasinoPresentation.DealDelay(player, i, oldPlayers, oldDealer, afterFlip) * scale, !hidden);
                }
                else if (!player && i == 1 && flipHole) motion.Reveal(flipDelay * scale);
                else motion.Rest(!hidden);
            }
        }
    }
}
