// Scoring and Fisher-Yates adapted from joaokucera/unity-blackjack (MIT), revision
// 0780e7c1873d2adbd6dfd57f896af6da6ceaafba. Copyright (c) 2018 João Kucera.
// See Assets/LingGuangV05/ThirdParty/Blackjack/{LICENSE.txt,NOTICE.md}.
using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    public static class BlackjackRules
    {
        public static int Rank(int card) => card % 13 + 1;
        public static int Score(IList<int> cards)
        {
            int total = 0, aces = 0;
            foreach (int card in cards)
            {
                int rank = Rank(card);
                total += rank == 1 ? 11 : Math.Min(rank, 10);
                if (rank == 1) aces++;
            }
            while (aces-- > 0 && total > 21) total -= 10;
            return total;
        }
        public static int[] Shuffle(Func<int, int> next)
        {
            var cards = new int[52];
            for (int i = 0; i < cards.Length; i++) cards[i] = i;
            for (int i = cards.Length - 1; i > 0; i--)
            {
                int card = cards[i], other = next(i + 1);
                cards[i] = cards[other]; cards[other] = card;
            }
            return cards;
        }
        public static bool Natural(IList<int> cards) => cards.Count == 2 && Score(cards) == 21;
        public static int Returned(IList<int> player, IList<int> dealer, int stake)
        {
            int p = Score(player), d = Score(dealer);
            if (p > 21) return 0;
            if (Natural(dealer)) return Natural(player) ? stake : 0;
            if (Natural(player)) return stake * 5 / 2;
            if (d > 21 || p > d) return stake * 2;
            return p == d ? stake : 0;
        }
    }
    [Serializable]
    public sealed class XgBlackjackHand
    {
        public bool active, doubled;
        public int stake, position, revision;
        public double nextActionAt;
        public int[] deck;
        public List<int> player = new List<int>(), dealer = new List<int>();
    }
    public sealed partial class XgSim
    {
        public bool BlackjackActive => Casino.blackjack != null && Casino.blackjack.active;
        public bool StartBlackjack(int stake, IXgHost host)
        {
            if (BlackjackActive || !CasinoRules.IsStake(stake) || !CasinoFunds(host, stake)
                || Clock < Casino.nextBetAt) return false;
            if (!host.Spend(stake)) return false;
            EnsureCasinoRng();
            var h = new XgBlackjackHand { active = true, stake = stake, deck = BlackjackRules.Shuffle(CasinoNext), nextActionAt = Clock + .4 };
            Casino.blackjack = h; Casino.wagered += stake; Casino.nextBetAt = Clock + CasinoRules.RoundSeconds;
            h.player.Add(h.deck[h.position++]); h.dealer.Add(h.deck[h.position++]);
            h.player.Add(h.deck[h.position++]); h.dealer.Add(h.deck[h.position++]); h.revision++;
            if (BlackjackRules.Natural(h.player) || BlackjackRules.Natural(h.dealer)) SettleBlackjack(host);
            return true;
        }
        public bool PlayBlackjack(string action, IXgHost host)
        {
            var h = Casino.blackjack;
            if (!BlackjackActive || host == null || !ValidCasinoClock || Clock < h.nextActionAt
                || h.deck == null || h.position < 4 || h.position >= h.deck.Length) return false;
            if (action != "hit" && action != "stand" && action != "double") return false;
            if (action == "double")
            {
                if (h.player.Count != 2 || h.doubled || !CasinoFunds(host, h.stake) || !host.Spend(h.stake)) return false;
                Casino.wagered += h.stake; h.stake *= 2; h.doubled = true;
            }
            h.nextActionAt = Clock + .4; h.revision++;
            if (action != "stand") h.player.Add(h.deck[h.position++]);
            int score = BlackjackRules.Score(h.player);
            if (score > 21) SettleBlackjack(host);
            else if (action != "hit" || score == 21)
            {
                while (BlackjackRules.Score(h.dealer) < 17 && h.position < h.deck.Length) h.dealer.Add(h.deck[h.position++]);
                SettleBlackjack(host);
            }
            return true;
        }
        void SettleBlackjack(IXgHost host)
        {
            var h = Casino.blackjack;
            if (!h.active) return;
            h.active = false; h.revision++;
            int returned = BlackjackRules.Returned(h.player, h.dealer, h.stake);
            Casino.returned += returned; Casino.nextBetAt = Clock + CasinoRules.RoundSeconds;
            if (Casino.history.Count >= 8) Casino.history.RemoveRange(0, Casino.history.Count - 7);
            Casino.history.Add(new XgCasinoRound { number = ++Casino.rounds, game = "blackjack", stake = h.stake, returned = returned, values = h.player.ToArray() });
            if (returned > 0) host.Earn(returned);
        }
    }
}
