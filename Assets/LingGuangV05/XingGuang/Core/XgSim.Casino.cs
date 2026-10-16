using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    [Serializable]
    public sealed class XgCasinoRound
    {
        public int number, choice;
        // long: with no table limit a stake may be the whole wallet, and stake x 20 must not overflow.
        public long stake, returned;
        public string game;
        public int[] values;
    }

    [Serializable]
    public sealed class XgCasinoState
    {
        // Separate from the learning RNG: visiting the casino must not change training examples.
        public long rng;
        public int rounds;
        public double wagered, returned, nextBetAt;
        public List<XgCasinoRound> history = new List<XgCasinoRound>();
        public bool adVisible;
        public double adRemaining = 120;
        public XgBlackjackHand blackjack;
    }

    public sealed partial class XgState { public XgCasinoState casino; }

    /// <summary>
    /// Fictional local games, not a network service. Returns are gross (include the stake).
    ///
    /// There is NO table limit: any whole-yuan stake from ¥1 up to the player's whole wallet is accepted (the funds
    /// check is in XgSim.TryCasinoBet / StartBlackjack). <see cref="StakeCeiling"/> is only an arithmetic guard so that
    /// stake x 20 stays exact in double/long; it is far above anything the in-game economy can reach.
    ///
    /// Every game is close to fair. Theoretical return to player (RTP = expected gross return / stake), derived by
    /// exhaustive enumeration in XgCasinoTests.ExhaustiveReturnToPlayerMatchesTheDocumentedRtp and re-checked by a
    /// 200k-round seeded simulation in XgCasinoFairnessTests:
    ///   slots     3 reels x 6 equally likely symbols. Any pair 2x; three of a kind 3x; three 7s 20x.
    ///             (90*2 + 5*3 + 1*20) / 216 = 215/216 = 99.54 %. Wins 96/216 = 44.4 % of spins.
    ///   dice      3 dice, small 4-10 / big 11-17, win 2x. A triple (6/216) returns half the stake, not a loss.
    ///             (105*2 + 6*0.5) / 216 = 213/216 = 98.61 %. Wins 105/216 = 48.6 %.
    ///   roulette  European 0-36, red/black win 2x. Zero returns half the stake ("la partage" rule).
    ///             (18*2 + 0.5) / 37 = 98.65 %. Wins 18/37 = 48.6 %.
    ///   dragon    two cards A-K (1-13), higher wins 2x. A tie returns 3/4 of the stake.
    ///             (78*2 + 13*0.75) / 169 = 98.08 %. Wins 78/169 = 46.2 %.
    ///   blackjack fresh shuffled 52-card deck every hand, dealer stands on every 17, natural pays 3:2, double on the
    ///             first two cards, no split / insurance / surrender. About 99.5 % with basic strategy
    ///             (measured 99.56 %, 43.5 % of hands won, over 200k seeded hands).
    /// The draws come from one xorshift32 stream (CasinoNext) with no streak, balance or mood adjustment of any kind.
    /// </summary>
    public static class CasinoRules
    {
        public const double RoundSeconds = 1.5;
        public const long StakeCeiling = 100_000_000_000_000L;
        public static bool IsStake(long stake) => stake >= 1 && stake <= StakeCeiling;
        /// <summary>The largest stake the wallet can cover: the whole (whole-yuan part of the) balance, "all in".</summary>
        public static long AllIn(double money) => double.IsNaN(money) || money < 1 ? 0 : money >= StakeCeiling ? StakeCeiling : (long)Math.Floor(money);
        public static bool IsChoice(string game, int choice) => game == "slots" ? choice == 0
            : (game == "dice" || game == "roulette" || game == "dragon") && (choice == 0 || choice == 1);
        public static bool IsAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return false;
            string value = address.Trim();
            if (!value.Contains("://")) value = "https://" + value;
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == "https" || uri.Scheme == "http") && uri.UserInfo.Length == 0
                && uri.IsDefaultPort && (uri.Host == "888.vip" || uri.Host == "www.888.vip")
                && uri.AbsolutePath == "/" && uri.Query.Length == 0 && uri.Fragment.Length == 0;
        }

        public static bool Red(int n)
        {
            switch (n)
            {
                case 1: case 3: case 5: case 7: case 9: case 12: case 14: case 16: case 18:
                case 19: case 21: case 23: case 25: case 27: case 30: case 32: case 34: case 36: return true;
                default: return false;
            }
        }

        /// <summary>Gross return as a multiple of the stake, in quarters (so 0.5x = 2, 2x = 8, 20x = 80). Integer-exact.</summary>
        public static int ReturnQuarters(string game, int choice, int[] v)
        {
            if (!IsChoice(game, choice) || v == null) return 0;
            int count = game == "roulette" ? 1 : game == "dragon" ? 2 : 3;
            int min = game == "slots" || game == "roulette" ? 0 : 1;
            int max = game == "slots" ? 5 : game == "roulette" ? 36 : game == "dragon" ? 13 : 6;
            if (v.Length != count) return 0;
            foreach (int n in v) if (n < min || n > max) return 0;
            switch (game)
            {
                case "slots":
                    if (v[0] == v[1] && v[1] == v[2]) return v[0] == 0 ? 80 : 12; // three 7s 20x, other triples 3x
                    return v[0] == v[1] || v[1] == v[2] || v[0] == v[2] ? 8 : 0;  // any pair 2x
                case "dice":
                    if (v[0] == v[1] && v[1] == v[2]) return 2; // triple: half the stake back
                    return (v[0] + v[1] + v[2] <= 10 ? 0 : 1) == choice ? 8 : 0;
                case "roulette":
                    if (v[0] == 0) return 2; // zero: half the stake back
                    return (Red(v[0]) ? 0 : 1) == choice ? 8 : 0;
                case "dragon":
                    if (v[0] == v[1]) return 3; // tie: 3/4 of the stake back
                    return (v[0] > v[1] ? 0 : 1) == choice ? 8 : 0;
                default: return 0;
            }
        }

        public static double ReturnMultiplier(string game, int choice, int[] v) => ReturnQuarters(game, choice, v) / 4.0;
        /// <summary>Gross return in whole yuan, rounded down to the yuan (only odd stakes on half / quarter returns lose a fraction).</summary>
        public static long Payout(long stake, int quarters) => stake * quarters / 4;
    }

    public sealed partial class XgSim
    {
        public XgCasinoState Casino
        {
            get
            {
                if (S.casino == null) S.casino = new XgCasinoState();
                if (S.casino.history == null) S.casino.history = new List<XgCasinoRound>();
                return S.casino;
            }
        }

        /// <summary>Single synchronous settlement; animations never award money. O(1) time/space, 8-entry history.</summary>
        public bool TryCasinoBet(string game, int choice, long stake, IXgHost host, out XgCasinoRound round)
        {
            round = null;
            if (BlackjackActive || !CasinoRules.IsStake(stake) || !CasinoRules.IsChoice(game, choice)
                || !CasinoFunds(host, stake) || Clock < Casino.nextBetAt) return false;
            if (!host.Spend(stake)) return false;
            var c = Casino;
            c.nextBetAt = Clock + CasinoRules.RoundSeconds;
            EnsureCasinoRng();
            int count = game == "roulette" ? 1 : game == "dragon" ? 2 : 3;
            int[] values = new int[count];
            for (int i = 0; i < count; i++) values[i] = CasinoNext(game == "roulette" ? 37 : game == "dragon" ? 13 : 6) + (game == "dice" || game == "dragon" ? 1 : 0);
            long returned = CasinoRules.Payout(stake, CasinoRules.ReturnQuarters(game, choice, values));
            round = new XgCasinoRound { number = ++c.rounds, game = game, choice = choice, stake = stake, returned = returned, values = values };
            c.wagered += stake; c.returned += returned;
            if (c.history.Count >= 8) c.history.RemoveRange(0, c.history.Count - 7);
            c.history.Add(round);
            if (returned > 0) host.Earn(returned);
            CasinoSettled(round, false, host);
            return true;
        }

        int CasinoNext(int bound)
        {
            uint n = (uint)Casino.rng;
            n ^= n << 13; n ^= n >> 17; n ^= n << 5;
            Casino.rng = n;
            return (int)(n % (uint)bound);
        }

        bool ValidCasinoClock => !double.IsNaN(Clock) && !double.IsInfinity(Clock) && Clock >= 0;
        bool CasinoFunds(IXgHost host, long amount) => host != null && ValidCasinoClock && !double.IsNaN(host.Money)
            && !double.IsInfinity(host.Money) && host.Money >= amount;
        void EnsureCasinoRng()
        {
            if (Casino.rng != 0) return;
            Casino.rng = (uint)(S.rng ^ 0x6C8E9CF5L);
            if (Casino.rng == 0) Casino.rng = 0x6C8E9CF5L;
        }

        /// <summary>Only active eligible desktop time counts; offline catch-up never calls this.</summary>
        public void AdvanceCasinoAd(double seconds, bool held)
        {
            if (S.stage < 3 || S.chapterComplete || held || seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
            if (Casino.adVisible) return;
            Casino.adRemaining = Math.Max(0, Casino.adRemaining - seconds);
            if (Casino.adRemaining == 0) Casino.adVisible = true;
        }

        public void CloseCasinoAd()
        {
            if (!Casino.adVisible) return;
            Casino.adVisible = false;
            Casino.adRemaining = 300;
        }
    }
}
